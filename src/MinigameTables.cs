using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json.Linq;

namespace StudentAge.CampusUno
{
    /// <summary>
    /// 读取 Cfgs/zh-cn 下本插件自定义的 JSON 表（GomokuCfg.json、SanguoshaCfg.json……）。
    ///
    /// 表的格式与原版 FightPlayerCfg.json 一样：对象，键是 id，值是一行：
    /// <code>
    /// { "910201": { "id": 910201, "game": 9102, "level": 1, "name": "第1关", "parms": { "Win": 0.9 } } }
    /// </code>
    /// 多个 Mod 提供同名表时，按 <see cref="ModCfgLocator"/> 的顺序合并：同 id 的行里 parms 按键覆盖，
    /// 其他字段整体替换，所以作者只需把要改的行和键放进自己 Mod 的同名文件里。
    /// </summary>
    public static class MinigameTables
    {
        static readonly Dictionary<string, Dictionary<string, JObject>> cache =
            new Dictionary<string, Dictionary<string, JObject>>(StringComparer.OrdinalIgnoreCase);
        static readonly List<string> errors = new List<string>();

        public static IReadOnlyList<string> Errors => errors;

        /// <summary>清空缓存，下次访问重新读文件。</summary>
        public static void Reset() { cache.Clear(); errors.Clear(); ModCfgLocator.Reset(); }

        /// <summary>合并后的整张表；不存在时返回空表。</summary>
        public static Dictionary<string, JObject> Load(string tableName)
        {
            if (cache.TryGetValue(tableName, out var rows)) return rows;
            rows = new Dictionary<string, JObject>(StringComparer.OrdinalIgnoreCase);
            foreach (string dir in ModCfgLocator.CfgDirectories())
            {
                string file = Path.Combine(dir, tableName + ".json");
                if (!File.Exists(file)) continue;
                try
                {
                    var root = JObject.Parse(File.ReadAllText(file));
                    foreach (var pair in root)
                    {
                        if (!(pair.Value is JObject row)) continue;
                        if (rows.TryGetValue(pair.Key, out var existing)) MergeRow(existing, row);
                        else rows[pair.Key] = (JObject)row.DeepClone();
                    }
                }
                catch (Exception e)
                {
                    errors.Add(file + ": " + e.Message);
                    UnityEngine.Debug.LogWarning("[CampusMinigames] 读取 " + file + " 失败：" + e.Message);
                }
            }
            cache[tableName] = rows;
            return rows;
        }

        // 同 id 的行：普通字段整体替换，parms 按键合并，作者只需写要改的键。
        static void MergeRow(JObject target, JObject source)
        {
            foreach (var prop in source.Properties())
            {
                if (prop.Name == "parms" && prop.Value is JObject sourceParms && target["parms"] is JObject targetParms)
                {
                    foreach (var parm in sourceParms.Properties()) targetParms[parm.Name] = parm.Value.DeepClone();
                }
                else target[prop.Name] = prop.Value.DeepClone();
            }
        }

        public static JObject Row(string tableName, string id)
        {
            return Load(tableName).TryGetValue(id, out var row) ? row : null;
        }

        public static JObject Row(string tableName, int id) => Row(tableName, id.ToString(CultureInfo.InvariantCulture));

        /// <summary>读取某行 parms 里的一个数值；缺失或不是数字时返回 false。</summary>
        public static bool TryParm(JObject row, string key, out float value)
        {
            value = 0;
            if (row == null) return false;
            var parms = row["parms"] as JObject;
            JToken token = parms?[key];
            if (token == null) return false;
            switch (token.Type)
            {
                case JTokenType.Integer:
                case JTokenType.Float:
                    value = token.Value<float>(); return true;
                case JTokenType.Boolean:
                    value = token.Value<bool>() ? 1 : 0; return true;
                case JTokenType.String:
                    return float.TryParse(token.Value<string>(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
                default:
                    return false;
            }
        }

        public static string[] StringArray(JObject row, string key)
        {
            if (row == null || !(row[key] is JArray array)) return null;
            var list = new List<string>();
            foreach (var item in array) if (item.Type == JTokenType.String) list.Add(item.Value<string>());
            return list.ToArray();
        }
    }

    /// <summary>
    /// 把 JSON 表映射到 <see cref="MinigameTuning"/> 的键。
    /// 带关卡号的键（Gomoku.Win3）读 GomokuCfg.json 里 id=910203 那行的 parms.Win；
    /// 不带关卡号的键读该游戏 id=9102 的全局行；NDS/Audio/Social 读 CampusMinigameCfg.json 里以分组名为 id 的行。
    /// </summary>
    public static class MinigameLevelConfig
    {
        public const string CommonTable = "CampusMinigameCfg";

        public static readonly Dictionary<string, int> GameIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            {"UNO", 9101}, {"Gomoku", 9102}, {"Bubble", 9103}, {"Box", 9104}, {"TwentyFour", 9105}, {"Tetris", 9106},
            {"Landlord", 9107}, {"Snake", 9108}, {"Pacman", 9109}, {"Mario", 9110}, {"Contra", 9111}, {"Mahjong", 9112},
        };

        public static string TableFor(string section) => GameIds.ContainsKey(section) ? section + "Cfg" : CommonTable;

        /// <summary>拆出键末尾的关卡号：Win3 → (Win, 3)；Price → (Price, 0)。</summary>
        public static string SplitLevel(string key, out int level)
        {
            int i = key.Length;
            while (i > 0 && char.IsDigit(key[i - 1])) i--;
            level = i < key.Length ? int.Parse(key.Substring(i), CultureInfo.InvariantCulture) : 0;
            return key.Substring(0, i);
        }

        public static bool TryRead(string section, string baseKey, int level, out float value)
        {
            value = 0;
            JObject row;
            if (GameIds.TryGetValue(section, out int gameId))
                row = MinigameTables.Row(TableFor(section), level > 0 ? gameId * 100 + level : gameId);
            else
                row = MinigameTables.Row(CommonTable, section);
            return MinigameTables.TryParm(row, baseKey, out value);
        }

        /// <summary>启动时把全部表读进 MinigameTuning；表里没有的项保持代码默认值。</summary>
        public static void Apply()
        {
            MinigameTuning.Configure(s =>
            {
                string baseKey = SplitLevel(s.Key, out int level);
                return TryRead(s.Section, baseKey, level, out float v) ? v : s.Default;
            });
            MinigameTuning.Fallback = (section, key, level, setting) =>
            {
                if (!TryRead(section, key, level, out float v)) return null;
                return setting != null ? setting.Clamp(v) : v;
            };
        }
    }
}
