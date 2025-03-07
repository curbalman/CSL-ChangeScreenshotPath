using ChangeScreenshotPath.Utils;
using ColossalFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChangeScreenshotPath.API
{
    public interface IScreenshotClient
    {
        void OnAfterCaptureScreenshot(ScreenshotEventArgs status);
        void OnAfterCaptureHiresScreenshot(ScreenshotEventArgs status);
        void OnReleased();
        void OnCreated();
    }

    public abstract class ScreenshotClientBase : IScreenshotClient
    {
        public IScreenshotManager manager => Singleton<ScreenshotWrapper>.instance;
        public virtual void OnAfterCaptureScreenshot(ScreenshotEventArgs status)
        {
            BLog.Debug("OnAfterCaptureScreenshot called at base class");
        }
        public virtual void OnAfterCaptureHiresScreenshot(ScreenshotEventArgs status)
        {
            BLog.Debug("OnAfterHiresCaptureScreenshot called at base class");
        }
        public virtual void OnReleased()
        {
            BLog.Debug("OnReleased called at base class");
        }
        public virtual void OnCreated()
        {
            BLog.Debug("OnCreated called at base class");
        }
    }
}
