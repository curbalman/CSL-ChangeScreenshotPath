using System.Reflection;
using ColossalFramework.Plugins;

namespace ChangeScreenshotPath;

public static class RenderItCompatibility
{
    public static Assembly RenderItAssembly;

    public static PluginManager.PluginInfo RenderItMod = null;

    public static bool IsRenderItExist = false;
    public static bool IsRenderItEnabled => RenderItMod?.isEnabled ?? false;

    public static PropertyInfo RenderItAAOption;
    public static MethodInfo RenderItUpdateAntialiasingMethod;
    public static object ValueOfActiveProfile;
    public static object RenderItModManager;
    
    
    public static void Initialize()
    {
        foreach (var plugin in PluginManager.instance.GetPluginsInfo())
        {
            foreach (var assembly in plugin.GetAssemblies())
            {
                if (assembly.GetName().Name == "RenderIt")
                {
                    IsRenderItExist = true;
                    RenderItAssembly ??= assembly;
                    RenderItMod ??= plugin;
                }
            }
        }
    }
}