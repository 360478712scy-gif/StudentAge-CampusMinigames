using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace StudentAge.CampusUno
{
    /// <summary>
    /// SanguoshaCfg.json：每个 npc 一行，generals 是三关对手（同时是胜利奖励武将）。
    /// id 为 0 的行给没有单独配置的角色用。
    /// <code>{ "101": { "id": 101, "name": "……", "generals": ["liubei","sunshangxiang","huatuo"] } }</code>
    /// </summary>
    public static class SanguoshaTables
    {
        public const string Table = "SanguoshaCfg";

        public static Dictionary<int, string[]> LoadEncounters() => LoadEncounters(out _);

        public static Dictionary<int, string[]> LoadEncounters(out string[] fallback)
        {
            fallback = null;
            var result = new Dictionary<int, string[]>();
            foreach (var pair in MinigameTables.Load(Table))
            {
                if (!int.TryParse(pair.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int npc)) continue;
                string[] generals = MinigameTables.StringArray(pair.Value, "generals");
                if (generals == null || generals.Length < 3)
                {
                    UnityEngine.Debug.LogWarning("[CampusMinigames] SanguoshaCfg.json 第 " + pair.Key + " 行需要 3 个 generals，已忽略");
                    continue;
                }
                if (npc == 0) fallback = generals; else result[npc] = generals;
            }
            return result;
        }
    }
}
