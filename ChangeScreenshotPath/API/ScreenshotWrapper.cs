using System;
using System.Collections.Generic;
using ChangeScreenshotPath;
using ChangeScreenshotPath.Utils;
using ColossalFramework;
using ColossalFramework.Plugins;
using ColossalFramework.UI;

namespace ChangeScreenshotPath.API
{


    public class ScreenshotWrapper: Singleton<ScreenshotWrapper>, IScreenshotManager
    {
        private List<IScreenshotClient> clients = new List<IScreenshotClient>();
        public void CaptureScreenshot()
        {
            OnGUIPatch.iSCaptureRequestedFromClient = true;
        }
        public void CaptureHiresScreenshot()
        {
            // TODO
        }
        internal void OnAfterCaptureScreenshotRelay(ScreenshotEventArgs status)
        {
            foreach (var client in clients)
            {
                client?.OnAfterCaptureScreenshot(status);
            }
        }
        public ScreenshotWrapper()
        {
            getImplementations();
            Singleton<PluginManager>.instance.eventPluginsChanged += getImplementations;
            Singleton<PluginManager>.instance.eventPluginsStateChanged += getImplementations;
        }

        private void getImplementations()
        {
            OnClientReleased();
            clients = Singleton<PluginManager>.instance.GetImplementations<IScreenshotClient>();
            OnClientCreated();
        }

        public void Release()
        {
            Singleton<PluginManager>.instance.eventPluginsChanged -= getImplementations;
            Singleton<PluginManager>.instance.eventPluginsStateChanged -= getImplementations;
            OnClientReleased();
        }

        public void OnClientReleased()
        {
            for (int i = 0; i < clients.Count; i++)
            {
                try
                {
                    clients[i].OnReleased();
                }
                catch (Exception ex)
                {
                    Type type = clients[i].GetType();
                    PluginManager.PluginInfo pluginInfo = Singleton<PluginManager>.instance.FindPluginInfo(type.Assembly);
                    if (pluginInfo != null)
                    {
                        BLog.Error("The Mod " + pluginInfo.ToString() + " has caused an error", ex);
                    }
                    else
                    {
                        BLog.Error("A Mod caused an error", ex);
                    }
                }
            }
        }
        public void OnClientCreated()
        {
            for (int i = 0; i < clients.Count; i++)
            {
                try
                {
                    clients[i].OnCreated();
                }
                catch (Exception ex)
                {
                    Type type = clients[i].GetType();
                    PluginManager.PluginInfo pluginInfo = Singleton<PluginManager>.instance.FindPluginInfo(type.Assembly);
                    if (pluginInfo != null)
                    {
                        BLog.Error("The Mod " + pluginInfo.ToString() + " has caused an error", ex);
                    }
                    else
                    {
                        BLog.Error("A Mod caused an error", ex);
                    }
                }
            }
        }
    }



}


