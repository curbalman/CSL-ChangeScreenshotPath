using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using ColossalFramework;
using ColossalFramework.IO;
using UnityEngine;

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
        internal static string fileName = "Screenshot.png";
        internal static string fileNameHires = "HiresScreenshot.png";

        private static MainMenu mainMenu = UnityEngine.Object.FindObjectOfType<MainMenu>();

        // Reflect private fields.
        private static SavedInputKey m_Screenshot = (SavedInputKey)typeof(MainMenu).GetField("m_Screenshot", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(mainMenu);
        private static SavedInputKey m_HiresScreenshot = (SavedInputKey)typeof(MainMenu).GetField("m_HiresScreenshot", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(mainMenu);

        private static bool captureNextFrame = false; 
        public static bool Prefix()
        {
            if (m_Screenshot.IsPressed(Event.current))
            {
                Application.CaptureScreenshot(PathUtils.MakeUniquePath(Path.Combine(screenShotPath(), fileName)), 1);
            }
            else if (m_HiresScreenshot.IsPressed(Event.current) || captureNextFrame)
            {
                // If anti-aliasing is on, turn it off and capture in next frame.
                if (CheckAntiAliasing())
                {
                    captureNextFrame = true;
                    return false;
                }
                else
                {
                    Application.CaptureScreenshot(PathUtils.MakeUniquePath(Path.Combine(useDifferentPathForHiresScreenshot ? hiresScreenshotPath : ScreenshotPathPatch.screenshotPath, fileNameHires)), superSize);
                    if (captureNextFrame)
                    {
                        // Restore anti-aliasing.
                        GameObject.Find("AntiAliasing").GetComponent<ColossalFramework.UI.UIDropDown>().selectedIndex = 1;
                        captureNextFrame = false;
                    }
                }
            }

            return false; // Skip the original method.
        }


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

        private static bool CheckAntiAliasing()
        {
            if (GameObject.Find("AntiAliasing").GetComponent<ColossalFramework.UI.UIDropDown>().selectedIndex == 1)
            {
                GameObject.Find("AntiAliasing").GetComponent<ColossalFramework.UI.UIDropDown>().selectedIndex = 0;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}