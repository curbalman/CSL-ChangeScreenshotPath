using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChangeScreenshotPath.API
{
    public interface IScreenshotManager
    {
        void CaptureScreenshot();
        void CaptureHiresScreenshot();
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
