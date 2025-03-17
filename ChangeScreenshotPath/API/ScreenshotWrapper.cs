using System;
using System.Collections.Generic;
using ChangeScreenshotPath;
using ColossalFramework;
using ColossalFramework.Plugins;
using BenCSCommons.Cities1;
using System.Configuration;

namespace ChangeScreenshotPath.API
{
    public class ScreenshotWrapper: Singleton<ScreenshotWrapper>, IScreenshotManager
    {
        private List<IScreenshotClient> clients = [];

        public void CaptureScreenshot()
        {
            BLog.Rem("Capture screenshot requested");
            OnGUIPatch.iSCaptureRequestedFromClient = true;
        }

        public void CaptureHiresScreenshot()
        {
            BLog.Rem("Capture hires screenshot requested");
            OnGUIPatch.isHiresRequestedFromClient = true;
        }

        internal void OnAfterScreenshotRelay(ScreenshotEventArgs status)
        {
            BLog.Rem("OnAfterScreenshotRelay called", status);
            Singleton<ScreenshotWrapper>.instance.getImplementations();
            foreach (var client in clients)
            {
                client?.OnAfterCaptureScreenshot(status);
            }
        }


        internal void OnAfterHiresScreenshotRelay(ScreenshotEventArgs status)
        {
            BLog.Rem("OnAfterHiresScreenshotRelay called", status);
            Singleton<ScreenshotWrapper>.instance.getImplementations();
            foreach (var client in clients)
            {
                client?.OnAfterCaptureScreenshot(status);
            }
        }

        private void getImplementations()
        {
            OnClientReleased();
            clients = Singleton<PluginManager>.instance.GetImplementations<IScreenshotClient>();
            OnClientCreated();
            BLog.Rem();
            foreach (IScreenshotClient client in clients)
            {
                BLog.Info($"Client type name: {client.GetType().FullName}");
            }
        }

        internal void Release()
        {
            BLog.Rem();
            Singleton<PluginManager>.instance.eventPluginsChanged -= getImplementations;
            Singleton<PluginManager>.instance.eventPluginsStateChanged -= getImplementations;
            OnClientReleased();
        }

        public ScreenshotWrapper()
        {
            Singleton<PluginManager>.instance.eventPluginsChanged += getImplementations;
            Singleton<PluginManager>.instance.eventPluginsStateChanged += getImplementations;
        }

        private void OnClientReleased()
        {
            BLog.Rem();
            for (int i = 0; i < clients.Count; i++)
            {
                try
                {
                    clients[i]?.OnReleased();
                }
                catch (Exception ex)
                {
                    Type type = clients[i].GetType();
                    PluginManager.PluginInfo pluginInfo = Singleton<PluginManager>.instance.FindPluginInfo(type.Assembly);
                    if (pluginInfo != null)
                    {
                        BLog.Error("The Mod " + pluginInfo.ToString() + " has caused an error");
                    }
                    else
                    {
                        BLog.Error("A Mod caused an error");
                    }
                }
            }
        }

        private void OnClientCreated()
        {
            BLog.Rem();
            for (int i = 0; i < clients.Count; i++)
            {
                try
                {
                    clients[i]?.OnCreated();
                }
                catch (Exception ex)
                {
                    Type type = clients[i].GetType();
                    PluginManager.PluginInfo pluginInfo = Singleton<PluginManager>.instance.FindPluginInfo(type.Assembly);
                    if (pluginInfo != null)
                    {
                        BLog.Error("The Mod " + pluginInfo.ToString() + " has caused an error");
                    }
                    else
                    {
                        BLog.Error("A Mod caused an error");
                    }
                }
            }
        }
    }



}


