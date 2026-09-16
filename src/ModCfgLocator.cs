using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;

namespace StudentAge.CampusUno
{
    /// <summary>
    /// 找出所有可能提供 Cfgs/zh-cn 自定义表的目录。
    ///
    /// 顺序：本 Mod 自带的表（默认值）→ 官方启用的 Mod 目录（1.94 MultiFolderLoader）
    /// 或 Steam 创意工坊目录 → BepInEx/plugins 下的手动安装目录。
    /// 后面的表按 id 覆盖前面的行，所以角色 Mod 作者把 GomokuCfg.json 之类复制到
    /// 自己的 Mod 里修改即可覆盖默认关卡。
    /// </summary>
    public static class ModCfgLocator
    {
        const string WorkshopAppId = "1991040";
        static string[] snapshot;

        /// <summary>所有存在的 Cfgs/zh-cn 目录，去重，本 Mod 的在最前。</summary>
        public static string[] CfgDirectories()
        {
            if (snapshot != null) return snapshot;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<string>();
            void Add(string modRoot)
            {
                if (string.IsNullOrEmpty(modRoot)) return;
                string cfg;
                try { cfg = Path.GetFullPath(Path.Combine(modRoot, "Cfgs", "zh-cn")); } catch { return; }
                if (!Directory.Exists(cfg) || !seen.Add(cfg)) return;
                result.Add(cfg);
            }

            Add(CampusResources.ModRoot);
            foreach (string root in ModRoots()) Add(root);
            snapshot = result.ToArray();
            return snapshot;
        }

        /// <summary>F9 热重载等场景可调用，下次读取重新扫描。</summary>
        public static void Reset() { snapshot = null; }

        static IEnumerable<string> ModRoots()
        {
            var roots = new List<string>();
            try
            {
                if (!OfficialModRoots(roots))
                {
                    string workshop = WorkshopDirectory();
                    if (Directory.Exists(workshop))
                        roots.AddRange(Directory.GetDirectories(workshop).OrderBy(p => Path.GetFileName(p), StringComparer.OrdinalIgnoreCase));
                }
            }
            catch (Exception e) { UnityEngine.Debug.LogWarning("[CampusMinigames] 扫描 Mod 目录失败：" + e.Message); }
            try
            {
                if (Directory.Exists(Paths.PluginPath))
                    roots.AddRange(Directory.GetDirectories(Paths.PluginPath).OrderBy(p => Path.GetFileName(p), StringComparer.OrdinalIgnoreCase));
            }
            catch (Exception e) { UnityEngine.Debug.LogWarning("[CampusMinigames] 扫描 plugins 目录失败：" + e.Message); }
            return roots;
        }

        static string WorkshopDirectory()
        {
            var steamApps = new DirectoryInfo(Paths.GameRootPath).Parent?.Parent;
            return steamApps == null ? string.Empty : Path.Combine(steamApps.FullName, "workshop", "content", WorkshopAppId);
        }

        // 官方 1.94：BepInEx.MultiFolderLoader 按 doorstop_config.ini 里的启用名单加载 Mod。
        static bool OfficialModRoots(List<string> result)
        {
            if (!AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "BepInEx.MultiFolderLoader")) return false;
            string ini = Path.Combine(Paths.GameRootPath, "doorstop_config.ini");
            if (!File.Exists(ini)) return false;
            var sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> section = null;
            foreach (string raw in File.ReadAllLines(ini))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";")) continue;
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    section = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    sections[line.Substring(1, line.Length - 2)] = section;
                }
                else if (section != null)
                {
                    int split = line.IndexOf('=');
                    if (split > 0) section[line.Substring(0, split).Trim()] = line.Substring(split + 1).Trim();
                }
            }
            if (!sections.TryGetValue("MultiFolderLoader", out var main)) return false;
            var found = new List<string>();
            AddSection(main, found);
            if (main.TryGetValue("enableAdditionalDirectories", out string extra) && extra.Equals("true", StringComparison.OrdinalIgnoreCase))
                foreach (var entry in sections.Where(e => e.Key.StartsWith("MultiFolderLoader_", StringComparison.OrdinalIgnoreCase)))
                    AddSection(entry.Value, found);
            result.AddRange(found.OrderBy(p => p, StringComparer.OrdinalIgnoreCase));
            return true;
        }

        static void AddSection(Dictionary<string, string> section, List<string> result)
        {
            if (!section.TryGetValue("baseDir", out string value)) return;
            string path = Resolve(value);
            if (!Directory.Exists(path)) return;
            HashSet<string> enabled = section.TryGetValue("enabledModsListPath", out value) ? ReadList(value) : null;
            HashSet<string> disabled = section.TryGetValue("disabledModsListPath", out value) ? ReadList(value) : null;
            foreach (string dir in Directory.GetDirectories(path))
            {
                string id = Path.GetFileName(dir);
                if ((enabled == null || enabled.Contains(id)) && (disabled == null || !disabled.Contains(id))) result.Add(Path.GetFullPath(dir));
            }
        }

        static string Resolve(string value)
        {
            value = Environment.ExpandEnvironmentVariables(value);
            return Path.GetFullPath(Path.IsPathRooted(value) ? value : Path.Combine(Paths.GameRootPath, value));
        }

        static HashSet<string> ReadList(string value)
        {
            string path = Resolve(value);
            IEnumerable<string> lines = File.Exists(path) ? File.ReadAllLines(path).Select(s => s.Trim()).Where(s => s.Length > 0) : Enumerable.Empty<string>();
            return new HashSet<string>(lines, StringComparer.OrdinalIgnoreCase);
        }
    }
}
