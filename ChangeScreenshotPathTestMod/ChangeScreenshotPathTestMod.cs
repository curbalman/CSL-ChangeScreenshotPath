using System;
using ICities;
using BenCSCommons.Cities1;
using ChangeScreenshotPath;
using ChangeScreenshotPath.API;
using UnityEngine;
using ColossalFramework;


namespace ChangeScreenshotPathTestMod
{
    public class ChangeScreenshotPathTestMod : BenCSCommons.Cities1.UserModBase
    {
        public ChangeScreenshotPathTestMod() :
            base("Change Screenshot Path Test Mod", "A test mod to change the screenshot path.")
        {
        }
        public override void OnSettingsUI(UIHelperBase helper)
        {
            base.OnSettingsUI(helper);
            helper.AddButton("Capture Screenshot", () =>
            {
                BLog.Info("Capture Screenshot button clicked.");
                ScreenshotClient.manager.CaptureScreenshot();
            });
            helper.AddButton("Capture 高清 Screenshot", () =>
            {
                BLog.Info("Capture HiRes Screenshot button clicked.");
                ScreenshotClient.manager.CaptureHiresScreenshot();
            });
            helper.AddButton("重启游戏", () =>
            {
                String path = Application.dataPath;
                path += "/../Cities.exe";
                System.Diagnostics.Process.Start(path);
                Singleton<LoadingManager>.instance.QuitApplication();
            });
        }
    }
    public class ScreenshotClient : ScreenshotClientBase
    {

    }
}
