using System;
using System.IO;
using System.Reflection;

namespace StudentAge.CampusUno
{
    /// <summary>
    /// 统一的路径解析。支持两种安装布局：
    ///
    /// 工坊 / 官方 Mod 目录（lfw 示范结构）：
    ///   Mod根/plugins/CampusMinigames.dll
    ///   Mod根/EC2BUnofficialPatch/Minigame/CustomMinigamecfg.json + 全部资源目录
    ///   Mod根/Cfgs/zh-cn/*.json
    ///
    /// 手动安装：
    ///   BepInEx/plugins/CampusMinigames/CampusMinigames.dll
    ///   BepInEx/plugins/CampusMinigames/CustomMinigamecfg.json + 资源目录 + Cfgs/zh-cn
    /// </summary>
    public static class CampusResources
    {
        public const string RegistryFileName = "CustomMinigamecfg.json";
        public const string ContentFolder = "EC2BUnofficialPatch/Minigame";

        static string pluginDirectory, modRoot, contentRoot;

        /// <summary>CampusMinigames.dll 所在目录。</summary>
        public static string PluginDirectory
        {
            get
            {
                if (pluginDirectory == null)
                {
                    string location = typeof(CampusResources).Assembly.Location;
                    pluginDirectory = string.IsNullOrEmpty(location) ? BepInEx.Paths.PluginPath : Path.GetDirectoryName(location);
                }
                return pluginDirectory;
            }
        }

        /// <summary>小游戏资源目录：包含 CustomMinigamecfg.json 以及 Music/Nds/Retro/Sanguosha 等子目录。</summary>
        public static string ContentRoot { get { Resolve(); return contentRoot; } }

        /// <summary>Mod 根目录：Cfgs/ 与 EC2BUnofficialPatch/ 的父目录。手动安装时等于插件目录。</summary>
        public static string ModRoot { get { Resolve(); return modRoot; } }

        /// <summary>兼容旧调用：等同 ContentRoot。</summary>
        public static string Root => ContentRoot;

        /// <summary>Cfgs/zh-cn 所在目录（Mod 根优先，其次资源目录内）。</summary>
        public static string CfgRoot
        {
            get
            {
                string primary = Path.Combine(ModRoot, "Cfgs", "zh-cn");
                if (Directory.Exists(primary)) return primary;
                return Path.Combine(ContentRoot, "Cfgs", "zh-cn");
            }
        }

        /// <summary>插件被直接放在游戏 BepInEx/plugins 下（手动安装），而不是由官方 Mod/工坊目录加载。</summary>
        public static bool IsManualInstall
        {
            get
            {
                try
                {
                    string plugins = Path.GetFullPath(BepInEx.Paths.PluginPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                    return Path.GetFullPath(PluginDirectory).StartsWith(plugins, StringComparison.OrdinalIgnoreCase);
                }
                catch { return false; }
            }
        }

        static void Resolve()
        {
            if (contentRoot != null) return;
            string dir = PluginDirectory;
            string registry = Path.Combine(dir, RegistryFileName);
            if (File.Exists(registry))
            {
                // 手动安装：一切都在插件目录里。
                contentRoot = dir; modRoot = dir; return;
            }

            // 工坊布局：plugins/ 的上一级是 Mod 根；再向上找几层以兼容嵌套目录。
            string probe = dir;
            for (int depth = 0; depth < 4 && !string.IsNullOrEmpty(probe); depth++)
            {
                string candidate = Path.Combine(probe, "EC2BUnofficialPatch", "Minigame");
                if (File.Exists(Path.Combine(candidate, RegistryFileName)))
                {
                    contentRoot = candidate; modRoot = probe; return;
                }
                probe = Path.GetDirectoryName(probe);
            }

            // 找不到注册文件时退回插件目录，至少让日志能说明问题。
            contentRoot = dir; modRoot = dir;
        }
    }
}
