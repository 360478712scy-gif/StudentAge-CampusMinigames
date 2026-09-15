using System;
using System.IO;
using System.Linq;

namespace StudentAge.CampusUno
{
    public static class CampusResources
    {
        public static string Root
        {
            get
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "CampusMinigames.UP");
                if (assembly != null && !string.IsNullOrEmpty(assembly.Location)) return Path.GetDirectoryName(assembly.Location);
                // Before the extension loads, support both a flat Workshop package and the legacy manual layout.
                string own = Path.GetDirectoryName(typeof(Plugin).Assembly.Location);
                if (File.Exists(Path.Combine(own, "CampusMinigames.UP.dll"))) return own;
                return Path.Combine(BepInEx.Paths.PluginPath, "StudentAgeCampusMinigames");
            }
        }
        public static bool IsManualInstall => string.Equals(Path.GetFullPath(typeof(Plugin).Assembly.Location),
            Path.GetFullPath(Path.Combine(BepInEx.Paths.PluginPath, "CampusUno", "CampusUno.dll")), StringComparison.OrdinalIgnoreCase);
    }
}
