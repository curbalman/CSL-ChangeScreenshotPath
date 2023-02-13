using System;
using System.IO;
using System.Xml.Serialization;

namespace ChangeScreenshotPath
{
    // a serialization class based on code by algernon
    // https://github.com/algernon-A/ACME/blob/master/Code/Settings/ModSettings.cs

    /// <summary>
    /// Global mod settings.
    /// </summary>
    [XmlRoot("ChangeScreenshotPath")]
    public class ModSettings
    {
        // Settings file name.
        [XmlIgnore]
        private static readonly string SettingsFilePath = Path.Combine(ColossalFramework.IO.DataLocation.localApplicationData, "ChangeScreenshotPath.xml");

        // File version.
        [XmlAttribute("Version")]
        public int version = 0;

        [XmlElement("ScreenshotPath")]
        public string ScreenshotPath { get => ScreenshotPathPatch.screenshotPath; set => ScreenshotPathPatch.screenshotPath = value; }

        [XmlElement("UseDifferentPathForHiresScreenshot")]
        public bool UseDifferentPathForHiresScreenshot { get => OnGUIPatch.useDifferentPathForHiresScreenshot; set => OnGUIPatch.useDifferentPathForHiresScreenshot = value; }

        [XmlElement("HiresScreenshotPath")]
        public string HiresScreenshotPath { get => OnGUIPatch.hiresScreenshotPath; set => OnGUIPatch.hiresScreenshotPath = value; }

        [XmlElement("HiresScreenshotSuperSize")]
        public OnGUIPatch.HiresScreenshotSuperSize HiresScreenshotSuperSize { get => OnGUIPatch.hiresScreenshotSuperSize; set => OnGUIPatch.hiresScreenshotSuperSize = value; }

        [XmlElement("ScreenshotNaming")]
        public OnGUIPatch.ScreenshotNaming ScreenshotNaming { get => OnGUIPatch.screenshotNaming; set => OnGUIPatch.screenshotNaming = value; }
        
        [XmlElement("UseLocationStamp")]
        public bool UseLocationStamp { get => OnGUIPatch.useLocationStamp; set => OnGUIPatch.useLocationStamp = value; }
        
        [XmlElement("DisableAutoOffAAFeature")]
        public bool DisableAutoOffAAFeature { get => OnGUIPatch.disableAutoOffAAFeature; set => OnGUIPatch.disableAutoOffAAFeature = value; }

        internal static void Load()
        {
            try
            {
                // Check to see if configuration file exists.
                if (File.Exists(SettingsFilePath))
                {
                    // Read it.
                    using (StreamReader reader = new StreamReader(SettingsFilePath))
                    {
                        XmlSerializer xmlSerializer = new XmlSerializer(typeof(ModSettings));
                        if (!(xmlSerializer.Deserialize(reader) is ModSettings settingsFile))
                        {
                            UnityEngine.Debug.Log("ChangeScreenshotPath: couldn't deserialize settings file");
                        }
                    }
                }
                else
                {
                    UnityEngine.Debug.Log("ChangeScreenshotPath: no settings file found");
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        internal static void Save()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(SettingsFilePath))
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(ModSettings));
                    xmlSerializer.Serialize(writer, new ModSettings());
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }
    }


}
