using System;
using ChangeScreenshotPath;
using ChangeScreenshotPath.Utils;

namespace ChangeScreenshotPath.API
{
    public abstract class ChangeScreenshotPathAPI
    {
        public static void CaptureScreenshot()
        {
            OnGUIPatch.iSCaptureRequestedFromAPI = true;
        }
        public static void CaptureHiresScreenshot()
        {
            // TODO
        }
        public virtual void OnAfterCaptureScreenshot(ScreenshotEventArgs status) 
        {
            BLog.Debug("OnAfterCaptureScreenshot called at base class");
        }

        public virtual void OnAfterCaptureHiresScreenshot(ScreenshotEventArgs status)
        {
            BLog.Debug("OnAfterHiresCaptureScreenshot called at base class");
        }

        public ChangeScreenshotPathAPI()
        {
            // WTF 对象可能不会实例化
            BLog.Debug("ChangeScreenshotPathAPI(base class) constructor called");
            OnGUIPatch.AfterCaptureScreenshot += OnAfterCaptureScreenshot;
        }
    }

    public class ScreenshotEventArgs : EventArgs
    {
        public string Path;
        public bool Success;
        public ScreenshotEventArgs(bool success, string path)
        {
            Success = success;
            Path = path;
        }
    }

}


