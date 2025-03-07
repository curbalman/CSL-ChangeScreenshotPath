using System;
using CitiesHarmony.API;
using ICities;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;
using System.Runtime.Remoting.Lifetime;
using ChangeScreenshotPath.API;

namespace ChangeScreenshotPath
{
    public class Mod : IUserMod
    {
        public string Name => "Change Screenshot Path";

        public string Description => "change screenshot path";

        public void OnEnabled()
        {
            HarmonyHelper.DoOnHarmonyReady(() => Patcher.PatchAll());
            RenderItCompatibility.Initialize();
            Singleton<ScreenshotWrapper>.Ensure();
        }

        public void OnDisabled()
        {
            Singleton<ScreenshotWrapper>.instance.Release();
            if (HarmonyHelper.IsHarmonyInstalled) Patcher.UnpatchAll();
        }


        private UITextField screenshotPath;
        private UIButton screenshotPath_ShowInFileExplorer;
        private UIButton screenshotPath_ResetToDefault;

        private UICheckBox useDifferentPathForHiresScreenshot;

        private UITextField hiresScreenshotPath;
        private UIButton hiresScreenshotPath_ShowInFileExplorer;
        private UIButton hiresScreenshotPath_ResetToDefault;

        private UIDropDown hiresScreenshotSupersize;
        private UIDropDown fileNaming;
        private UICheckBox useLocationStamp;
        private UICheckBox disableAutoOffAAFeature;

        public void OnSettingsUI(UIHelperBase helper)
        {
            // Didn't put this on OnEnabled(). Otherwise static classes that reference to MainMenu are constructed before MainMenu created. 
            ModSettings.Load();

            UIHelperBase group = helper.AddGroup(this.Name);

            screenshotPath = (UITextField)group.AddTextfield("Screenshot Path", ScreenshotPathPatch.screenshotPath, _ => { }, (value) =>
            {
                try
                {
                    System.IO.Directory.CreateDirectory(value);

                    ScreenshotPathPatch.screenshotPath = value;
                    ModSettings.Save();
                    HideInValidLabel(screenshotPath);
                    ControlIsEnabled(screenshotPath_ShowInFileExplorer, true);
                }
                catch (Exception e)
                {
                    ShowInValidLabel(screenshotPath, e);
                    ControlIsEnabled(screenshotPath_ShowInFileExplorer, false);
                }
                ControlIsEnabled(screenshotPath_ResetToDefault, value != ScreenshotPathPatch.defaultPath);
            });
            screenshotPath.width = screenshotPath.parent.parent.width - 30;
            var parent = screenshotPath.parent as UIPanel;
            parent.height += 30;
            parent.padding.top = 10;

            screenshotPath_ShowInFileExplorer = helper.AddButton("Show in File Explorer", () => {
                System.Diagnostics.Process.Start(ScreenshotPathPatch.screenshotPath);
            }) as UIButton;

            screenshotPath_ResetToDefault = helper.AddButton("Reset to Default", () => {
                ScreenshotPathPatch.screenshotPath = ScreenshotPathPatch.defaultPath;
                screenshotPath.text = ScreenshotPathPatch.defaultPath;
                HideInValidLabel(screenshotPath);
                ControlIsEnabled(screenshotPath_ResetToDefault, false);
                ControlIsEnabled(screenshotPath_ShowInFileExplorer, true);
                ModSettings.Save();
            }) as UIButton;

            StylingButtons(screenshotPath_ResetToDefault, screenshotPath_ShowInFileExplorer);
            AttachButtons(screenshotPath, screenshotPath_ShowInFileExplorer, screenshotPath_ResetToDefault);



            useDifferentPathForHiresScreenshot = (UICheckBox)group.AddCheckbox("Use different path for hires screenshot", OnGUIPatch.useDifferentPathForHiresScreenshot, isChecked =>
            {
                OnGUIPatch.useDifferentPathForHiresScreenshot = isChecked;
                hiresScreenshotPath.parent.isVisible = isChecked;
                ModSettings.Save();
            });
            useDifferentPathForHiresScreenshot.height += 30;



            hiresScreenshotPath = (UITextField)group.AddTextfield("HiresScreenshot Path", OnGUIPatch.hiresScreenshotPath, _ => { }, (value) =>
            {
                try
                {
                    System.IO.Directory.CreateDirectory(value);

                    OnGUIPatch.hiresScreenshotPath = value;
                    ModSettings.Save();
                    HideInValidLabel(hiresScreenshotPath);
                    ControlIsEnabled(hiresScreenshotPath_ShowInFileExplorer, true);
                }
                catch (Exception e)
                {
                    ShowInValidLabel(hiresScreenshotPath, e);
                    ControlIsEnabled(hiresScreenshotPath_ShowInFileExplorer, false);
                }
                ControlIsEnabled(hiresScreenshotPath_ResetToDefault, value != System.IO.Path.Combine(ScreenshotPathPatch.defaultPath, "HiresScreenshots"));
            });
            hiresScreenshotPath.width = hiresScreenshotPath.parent.parent.width - 30;
            parent = hiresScreenshotPath.parent as UIPanel;
            parent.height += 30;
            parent.padding.top = 10;
            
            hiresScreenshotPath_ShowInFileExplorer = helper.AddButton("Show in File Explorer", () => {
                System.Diagnostics.Process.Start(OnGUIPatch.hiresScreenshotPath);
            }) as UIButton;

            hiresScreenshotPath_ResetToDefault = helper.AddButton("Reset to Default", () => {
                OnGUIPatch.hiresScreenshotPath = System.IO.Path.Combine(ScreenshotPathPatch.defaultPath, "HiresScreenshots"); ;
                hiresScreenshotPath.text = OnGUIPatch.hiresScreenshotPath;
                HideInValidLabel(hiresScreenshotPath);
                ControlIsEnabled(hiresScreenshotPath_ResetToDefault, false);
                ControlIsEnabled(hiresScreenshotPath_ShowInFileExplorer, true);
                ModSettings.Save();
            }) as UIButton;

            StylingButtons(hiresScreenshotPath_ResetToDefault, hiresScreenshotPath_ShowInFileExplorer);
            AttachButtons(hiresScreenshotPath, hiresScreenshotPath_ShowInFileExplorer, hiresScreenshotPath_ResetToDefault);



            UILabel resolutionSizeLabel;
            hiresScreenshotSupersize = (UIDropDown)group.AddDropdown("HiresScreenshot Supersize", Enum.GetNames(typeof(OnGUIPatch.HiresScreenshotSuperSize)), (int)OnGUIPatch.hiresScreenshotSuperSize, value =>
            {
                OnGUIPatch.superSize = OnGUIPatch.HiresScreenshotSuperSizeValues[value];
                OnGUIPatch.hiresScreenshotSuperSize = (OnGUIPatch.HiresScreenshotSuperSize)value;
                ModSettings.Save();

                resolutionSizeLabel = hiresScreenshotSupersize.GetComponentInChildren<UILabel>();
                if (resolutionSizeLabel != null)
                {
                    resolutionSizeLabel.text = "= " + UIView.GetAView().uiCamera.pixelWidth * OnGUIPatch.superSize + "x" + UIView.GetAView().uiCamera.pixelHeight * OnGUIPatch.superSize;
                }
            });
            {
                resolutionSizeLabel = hiresScreenshotPath.GetComponentInChildren<UILabel>();
                if (resolutionSizeLabel == null) resolutionSizeLabel = hiresScreenshotSupersize.AddUIComponent<UILabel>();

                resolutionSizeLabel.backgroundSprite = "GenericPanelWhite";
                resolutionSizeLabel.color = new Color32(131, 131, 131, 255);
                resolutionSizeLabel.textColor = new Color32(185, 221, 254, 255);
                resolutionSizeLabel.verticalAlignment = UIVerticalAlignment.Middle;
                resolutionSizeLabel.textAlignment = UIHorizontalAlignment.Center;
                resolutionSizeLabel.autoSize = false;
                resolutionSizeLabel.size = new Vector2(200f, 30f);
                resolutionSizeLabel.relativePosition = new Vector3(250f, 4f);
                resolutionSizeLabel.text = "= " + UIView.GetAView().uiCamera.pixelWidth * OnGUIPatch.superSize + "x" + UIView.GetAView().uiCamera.pixelHeight * OnGUIPatch.superSize;
            }
            



            fileNaming = (UIDropDown)group.AddDropdown("File Naming", Enum.GetNames(typeof(OnGUIPatch.ScreenshotNaming)), (int)OnGUIPatch.screenshotNaming, value =>
            {
                OnGUIPatch.screenshotNaming = (OnGUIPatch.ScreenshotNaming)value;
                ModSettings.Save();
            });

            useLocationStamp = (UICheckBox)group.AddCheckbox("Use location stamp", OnGUIPatch.useLocationStamp, sel =>
            {
                OnGUIPatch.useLocationStamp = sel;
                ModSettings.Save();
            });
            
            disableAutoOffAAFeature = (UICheckBox)group.AddCheckbox("Disable the feature that automatically turns off anti-aliasing while taking high-resolution screenshot", OnGUIPatch.disableAutoOffAAFeature, sel =>
            {
                OnGUIPatch.disableAutoOffAAFeature = sel;
                ModSettings.Save();
            });


            // ensuring
            {
                hiresScreenshotPath.parent.isVisible = useDifferentPathForHiresScreenshot.isChecked;
                ControlIsEnabled(screenshotPath_ResetToDefault, ScreenshotPathPatch.screenshotPath != ScreenshotPathPatch.defaultPath);
                ControlIsEnabled(hiresScreenshotPath_ResetToDefault, OnGUIPatch.hiresScreenshotPath != System.IO.Path.Combine(ScreenshotPathPatch.defaultPath, "HiresScreenshots"));
            }
        }

        /// <summary>
        /// Displays a label if an exception occurs on the entered path.
        /// </summary>
        void ShowInValidLabel(UIComponent uic, Exception e)
        {
            var label = uic.GetComponentInChildren<UILabel>();

            if(label == null)
            {
                label = uic.AddUIComponent<UILabel>();
                label.relativePosition = new Vector3(0f, 30f);
            }

            label.text = "Invalid Path: " + e.Message;
            label.textColor = new Color32(255, 0, 0, 255);
            uic.color = new Color32(255, 0, 0, 255);
            label.enabled = true;
        }

        void HideInValidLabel(UIComponent uic)
        {
            var label = uic.GetComponentInChildren<UILabel>();
            if (label != null)
            {
                label.enabled = false;
            }
            uic.color = new Color32(255, 255, 255, 255);
        }

        void ControlIsEnabled(UIComponent uic, bool b)
        {
            if (uic == null) return;

            uic.isEnabled = b;
        }

        /// <summary>
        /// Attach the button to the top right.
        /// </summary>
        void AttachButtons(UIComponent uic, params UIButton[] buttons)
        {
            var parent = uic.parent as UIPanel;
            var panel = parent.AddUIComponent<UIPanel>();
            panel.width = parent.parent.width - 30f;
            panel.height = 12f;
            panel.autoLayout = true;
            panel.autoLayoutStart = LayoutStart.BottomRight;
            
            var panel2 = panel.AddUIComponent<UIPanel>();
            panel2.autoFitChildrenHorizontally = true;
            panel2.height = 80f;
            panel2.autoLayout = true;
            panel2.isInteractive = false;
            panel2.autoLayoutPadding.left = 3;

            foreach (var button in buttons)
            {
                panel2.AttachUIComponent(button.gameObject);
            }
        }

        void StylingButtons(params UIButton[] buttons)
        {
            foreach (var button in buttons)
            {
                button.textScale = 0.9f;
                button.normalBgSprite = "ButtonWhite";
                button.color = new Color32(100, 128, 150, 255);
                button.focusedColor = new Color32(100, 128, 150, 255);
                button.hoveredColor = new Color32(94, 195, 255, 255);
                button.pressedColor = new Color32(212, 237, 255, 255);
                button.disabledColor = new Color32(51, 65, 77, 255);
                button.hoveredTextColor = new Color32(255, 255, 255, 255);
                button.disabledTextColor = new Color32(100, 100, 100, 255);
            }
        }

        
    }
}
