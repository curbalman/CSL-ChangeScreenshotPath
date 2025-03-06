using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using ColossalFramework;
using ColossalFramework.IO;
using ColossalFramework.UI;
using UnityEngine;
using ChangeScreenshotPath.Utils;

namespace ChangeScreenshotPath
{
    public static class Patcher
    {
        private const string HarmonyId = "neinnew.CSL-ChangeScreenshotPath";

        private static bool patched = false;

        public static void PatchAll()
        {
            if (patched) return;
            if (!CitiesHarmony.API.HarmonyHelper.IsHarmonyInstalled) 
            {
                UnityEngine.Debug.LogError("Change Screenshot Path: Harmony is not installed.");
                return;
            }

            UnityEngine.Debug.Log("Change Screenshot Path: Patching...");

            patched = true;

            var harmony = new Harmony(HarmonyId);

            // Patch all patches manually.
            var ScreenshotPathPatchPostfix = typeof(ScreenshotPathPatch).GetMethod(nameof(ScreenshotPathPatch.Postfix));
            var OnGUIPatchPrefix = typeof(OnGUIPatch).GetMethod(nameof(OnGUIPatch.Prefix));

            harmony.Patch(
                original: typeof(MainMenu).GetProperty("screenShotPath").GetGetMethod(),
                postfix: new HarmonyMethod(ScreenshotPathPatchPostfix)
                );
            harmony.Patch(
                original: typeof(ToolController).GetProperty("screenShotPath").GetGetMethod(),
                postfix: new HarmonyMethod(ScreenshotPathPatchPostfix)
                );
            harmony.Patch(
                original: AccessTools.Method(typeof(MainMenu), "OnGUI"),
                prefix: new HarmonyMethod(OnGUIPatchPrefix)
                );
            harmony.Patch(
                original: AccessTools.Method(typeof(ToolController), "OnGUI"),
                prefix: new HarmonyMethod(OnGUIPatchPrefix)
                );

            /* // Not sure why PatchAll causes errors..
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            */
        }

        public static void UnpatchAll()
        {
            if (!patched) return;

            var harmony = new Harmony(HarmonyId);
            harmony.UnpatchAll(HarmonyId);

            patched = false;

            UnityEngine.Debug.Log("Change Screenshot Path: Reverted...");
        }
    }

    [HarmonyPatch(typeof(ToolController), "screenShotPath", MethodType.Getter)]
    [HarmonyPatch(typeof(MainMenu), "screenShotPath", MethodType.Getter)]
    public static class ScreenshotPathPatch
    {
        internal static string screenshotPath;

        // = Path.Combine(ColossalFramework.IO.DataLocation.localApplicationData, "Screenshots");
        public static string defaultPath;

        static ScreenshotPathPatch()
        {
            // Invoke screenShotPath to perform the defaultPath initialization statement in Postfix.
            typeof(MainMenu).GetProperty("screenShotPath").GetGetMethod().Invoke(UnityEngine.Object.FindObjectOfType<MainMenu>(), null);
            
            screenshotPath = defaultPath;
        }

        public static void Postfix(ref string __result)
        {
            // Result of the original method.
            defaultPath = __result;

            // patched result
            __result = screenshotPath;
        }
    }

    [HarmonyPatch(typeof(ToolController), "OnGUI")]
    [HarmonyPatch(typeof(MainMenu), "OnGUI")]
    public static class OnGUIPatch
    {
        public enum ScreenshotNaming
        {
            Sequential, // Screenshot N
            DateTime // yyyy-MM-DD_HH-MM-SS N
        }

        public enum HiresScreenshotSuperSize { x2, x3, x4, x6, x8, x12, x16 }
        public static readonly int[] HiresScreenshotSuperSizeValues = { 2, 3, 4, 6, 8, 12, 16 };

        internal static bool useDifferentPathForHiresScreenshot = false;
        internal static string hiresScreenshotPath = Path.Combine(ScreenshotPathPatch.defaultPath, "HiresScreenshots");

        internal static HiresScreenshotSuperSize hiresScreenshotSuperSize = HiresScreenshotSuperSize.x4;
        internal static int superSize = HiresScreenshotSuperSizeValues[(int)hiresScreenshotSuperSize];

        internal static ScreenshotNaming screenshotNaming = ScreenshotNaming.Sequential;
        internal static bool useLocationStamp = true;

        internal static bool disableAutoOffAAFeature = false;

        private static MainMenu mainMenu = UnityEngine.Object.FindObjectOfType<MainMenu>();

        // Reflect private fields.
        private static SavedInputKey m_Screenshot = (SavedInputKey)typeof(MainMenu).GetField("m_Screenshot", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(mainMenu);
        private static SavedInputKey m_HiresScreenshot = (SavedInputKey)typeof(MainMenu).GetField("m_HiresScreenshot", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(mainMenu);

        private static bool captureHiresInNextFrame = false;
        private static bool captureHiresInNextFrameForRenderIt = false;
        private static bool restoreRenderItAntialiasing = false;
        private static int restoreRenderItAntialiasingTimer;
        
        private static int renderItAntialiasingTechniqueNumber;
        internal static bool iSCaptureRequestedFromAPI = false;

        public static bool Prefix()
        {
            // Screenshot
            if (m_Screenshot.IsPressed(Event.current) || iSCaptureRequestedFromAPI)
            {
                BLog.Debug($"Start capturing screenshot, iSCaptureRequestedFromAPI = {iSCaptureRequestedFromAPI}");
                string fileName = FileName(isHires: false);
                Application.CaptureScreenshot(PathUtils.MakeUniquePath(Path.Combine(screenShotPath(), fileName)), 1);
                iSCaptureRequestedFromAPI = false;
                BLog.Info($"Captured screenshot: {fileName}");
            }
            // HiresScreenshot
            else if (m_HiresScreenshot.IsPressed(Event.current))
            {
                Debug.LogWarning("01");
                
                // Just capture if disabled the auto off AA feature.
                if (disableAutoOffAAFeature)
                {
                    CaptureHiresScreenshot();
                    return false;
                }
                
                // Check Render It.
                if (RenderItCompatibility.IsRenderItExist && RenderItCompatibility.IsRenderItEnabled)
                {
                    // If anti-aliasing is on, turn it off and schedule capture in next frame.
                    if (CheckRenderItAntialiasing())
                    {
                        captureHiresInNextFrameForRenderIt = true;
                    }
                    else
                    {
                        // No anti-aliasing enabled. capture now.
                        CaptureHiresScreenshot();
                    }
                }
                // If anti-aliasing is on, turn it off and schedule capture in next frame.
                else if (CheckAntialiasing())
                {
                    captureHiresInNextFrame = true;
                }
                else
                {
                    // No anti-aliasing enabled. capture now.
                    CaptureHiresScreenshot();
                }
            }
            else if (captureHiresInNextFrame)
            {
                captureHiresInNextFrame = false;

                // Capture hires screenshot while disabled anti-aliasing.
                CaptureHiresScreenshot();
                
                // Restore vanilla anti-aliasing.
                GameObject.Find("AntiAliasing").GetComponent<UIDropDown>().selectedIndex = 1;
            }
            else if (captureHiresInNextFrameForRenderIt)
            {
                captureHiresInNextFrameForRenderIt = false;
                Debug.LogWarning("13 -");
                
                // Capture hires screenshot while disabled anti-aliasing.
                CaptureHiresScreenshot();
                
                Debug.LogWarning(RenderItCompatibility.RenderItAAOption == null);
                Debug.LogWarning(RenderItCompatibility.RenderItUpdateAntialiasingMethod == null);

                restoreRenderItAntialiasing = true;
            }
            else if (restoreRenderItAntialiasing)
            {
                // Wait with antialiasing turned off (otherwise it won't take with it turned off).
                restoreRenderItAntialiasingTimer++;
                if (restoreRenderItAntialiasingTimer > 100)
                {
                    restoreRenderItAntialiasingTimer = 0;
                    restoreRenderItAntialiasing = false;
                
                    // Restore Render It anti-aliasing.
                    RenderItCompatibility.RenderItAAOption.GetSetMethod()
                        .Invoke(RenderItCompatibility.ValueOfActiveProfile, new object[] { renderItAntialiasingTechniqueNumber });
                    RenderItCompatibility.RenderItUpdateAntialiasingMethod.Invoke(RenderItCompatibility.RenderItModManager, null);
                }
            }
            
            return false; // Skip the original method.
        }


        private static string FileName(bool isHires)
        {
                string fileName = screenshotNaming switch
                {
                    ScreenshotNaming.Sequential => isHires ? "HiresScreenshot" : "Screenshot",
                    ScreenshotNaming.DateTime => DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"),
                    _ => throw new ArgumentOutOfRangeException()
                };
                if (useLocationStamp)
                {
                    if (Singleton<SimulationManager>.instance.m_metaData != null)
                    {
                        fileName += $" [{Singleton<SimulationManager>.instance.m_metaData.m_CityName}]";
                    }

                    if (Controller != null)
                    {
                        Vector3 pos = Controller.m_currentPosition;
                        Vector3 angle = Controller.m_currentAngle;
                        fileName += $"[{pos.x}, {pos.y}, {pos.z}, {angle.x}, {angle.y}]";
                    }              
                }
                fileName += ".png";

                return fileName;
        }

        private static CameraController Controller => controller ??= UnityEngine.Object.FindObjectOfType<CameraController>();
        private static CameraController controller;
        
        /// <summary>
        /// Get screenShotPath that currently exists.
        /// </summary>
        private static string screenShotPath()
        {
            var mainMenu = UnityEngine.Object.FindObjectOfType<MainMenu>();
            var toolController = UnityEngine.Object.FindObjectOfType<ToolController>();

            if (mainMenu != null)
            {
                return mainMenu.screenShotPath;
            }
            else if (toolController != null)
            {
                return toolController.screenShotPath;
            }

            return null;
        }


        private static void CaptureHiresScreenshot() => Application.CaptureScreenshot(
            PathUtils.MakeUniquePath(Path.Combine(
                useDifferentPathForHiresScreenshot ? hiresScreenshotPath : ScreenshotPathPatch.screenshotPath,
                FileName(isHires: true))), superSize);

        private static bool CheckAntialiasing()
        {
            UIDropDown antialiasingDropdown = GameObject.Find("AntiAliasing").GetComponent<UIDropDown>();
            
            if (antialiasingDropdown.selectedIndex == 1)
            {
                antialiasingDropdown.selectedIndex = 0;
                return true;
            }
            
            return false;
        }

        private static bool CheckRenderItAntialiasing()
        {
            Type typeOfProfileManager = RenderItCompatibility.RenderItAssembly.GetType("RenderIt.Managers.ProfileManager");
            Type typeOfProfile = RenderItCompatibility.RenderItAssembly.GetType("RenderIt.Profile");

            // Type: RenderIt.Managers.ProfileManager
            // Name: RenderIt.Managers.ProfileManager.Instance_get
            MethodInfo methodOfInstanceGet = typeOfProfileManager
                .GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)
                ?.GetGetMethod();
            // ProfileManager.Instance..
            var valueOfInstance = methodOfInstanceGet?.Invoke(null, null);
            
            // Type: RenderIt.Profile
            // Name: RenderIt.Managers.ProfileManager.ActiveProfile
            PropertyInfo propertyOfActiveProfile = typeOfProfileManager.GetProperty("ActiveProfile", BindingFlags.Public | BindingFlags.Instance);
            // ProfileManager.Instance.ActiveProfile..
            RenderItCompatibility.ValueOfActiveProfile = propertyOfActiveProfile?.GetGetMethod()
                .Invoke(valueOfInstance, null);

            // Type: int
            // Name: RenderIt.Profile.AntialiasingTechnique
            RenderItCompatibility.RenderItAAOption = typeOfProfile.GetProperty("AntialiasingTechnique", BindingFlags.Public | BindingFlags.Instance);
            // ProfileManager.Instance.ActiveProfile.AntialiasingTechnique..
            var valueOfAntialiasingTechnique = RenderItCompatibility.RenderItAAOption?.GetGetMethod()
                .Invoke(RenderItCompatibility.ValueOfActiveProfile, null);

            if (valueOfAntialiasingTechnique is int antialiasingNumber)
            {
                renderItAntialiasingTechniqueNumber = antialiasingNumber;
                
                // If anti-aliasing is on, turn it off and capture in next frame.
                if (antialiasingNumber is 1 /* Default */ or 2 /* FXAA */ or 3 /* TAA */)
                {
                    RenderItCompatibility.RenderItAAOption.GetSetMethod()
                        .Invoke(RenderItCompatibility.ValueOfActiveProfile, new object[] { 0 /* None */ });
                    
                    Type typeOfModManager = RenderItCompatibility.RenderItAssembly.GetType("RenderIt.ModManager");
                    RenderItCompatibility.RenderItModManager = UnityEngine.Object.FindObjectOfType(typeOfModManager);
                    
                    // It seems(and considered) not in-game.
                    if (RenderItCompatibility.RenderItModManager == null) return false;

                    RenderItCompatibility.RenderItUpdateAntialiasingMethod ??= typeOfModManager.GetMethod("UpdateAntialiasing", BindingFlags.NonPublic | BindingFlags.Instance);
                    RenderItCompatibility.RenderItUpdateAntialiasingMethod?
                        .Invoke(RenderItCompatibility.RenderItModManager, null);

                    return true;
                }
            }

            return false;
        }
    }
}