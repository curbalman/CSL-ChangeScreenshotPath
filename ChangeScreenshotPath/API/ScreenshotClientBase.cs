using ColossalFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BenCSCommons.Cities1;

namespace ChangeScreenshotPath.API
{
    public abstract class ScreenshotClientBase : IScreenshotClient
    {
        public static IScreenshotManager manager => Singleton<ScreenshotWrapper>.instance;
        public virtual void OnAfterCaptureScreenshot(ScreenshotEventArgs status)
        {
            BLog.Rem();
            BLog.Info(status, nameof(status));
        }
        public virtual void OnAfterCaptureHiresScreenshot(ScreenshotEventArgs status)
        {
            BLog.Rem();
            BLog.Info(status, nameof(status));
        }
        public virtual void OnReleased()
        {
            BLog.Rem();
        }
        public virtual void OnCreated()
        {
            BLog.Rem();
        }
    }
}
