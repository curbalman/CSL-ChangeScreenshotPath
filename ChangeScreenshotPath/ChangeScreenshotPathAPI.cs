using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChangeScreenshotPath
{
    public static class ChangeScreenshotPathAPI
    {
        public static void CaptureScreenshot()
        {
            OnGUIPatch.iSCaptureRequestedFromAPI = true;
        }
    }
}
