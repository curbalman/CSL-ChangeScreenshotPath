using ChangeScreenshotPath;
using ColossalFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BenCSCommons.Cities1;

namespace ChangeScreenshotPath.API
{
    public interface IScreenshotClient
    {
        void OnAfterCaptureScreenshot(ScreenshotEventArgs status);
        void OnAfterCaptureHiresScreenshot(ScreenshotEventArgs status);
        void OnReleased();
        void OnCreated();
    }
}
