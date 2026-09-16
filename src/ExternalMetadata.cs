using System;
using System.Collections.Generic;
using System.IO;
using Config;
using Newtonsoft.Json;
namespace StudentAge.CampusUno
{
    internal static class ExternalMetadata
    {
        static Dictionary<int,MinigameCfg> games;
        static Dictionary<int,MinigameActionCfg> actions;
        static string lastError;
        internal static void Register()
        {
            if (Cfg.MinigameCfgMap == null || Cfg.MinigameActionCfgMap == null) return;
            string root=CampusResources.CfgRoot;
            if(games==null)
            {
                try
                {
                    var loadedGames=Read<MinigameCfg>(Path.Combine(root,"MinigameCfg.json"),g=>g.id);
                    var loadedActions=Read<MinigameActionCfg>(Path.Combine(root,"MinigameActionCfg.json"),g=>g.id);
                    // Publish only after both files validate; a partial installation can recover.
                    games=loadedGames;actions=loadedActions;lastError=null;
                }
                catch(Exception e) when(e is IOException || e is InvalidDataException || e is UnauthorizedAccessException || e is JsonException)
                {
                    if(lastError!=e.Message)UnityEngine.Debug.LogWarning("Campus minigame metadata unavailable: "+e.Message);
                    lastError=e.Message;return;
                }
            }
            // Author-provided native Mod rows always take precedence; never write a CFG file.
            foreach(var row in games)if(!Cfg.MinigameCfgMap.ContainsKey(row.Key))Cfg.MinigameCfgMap.Add(row.Key,row.Value);
            foreach(var row in actions)if(!Cfg.MinigameActionCfgMap.ContainsKey(row.Key))Cfg.MinigameActionCfgMap.Add(row.Key,row.Value);
        }
        static Dictionary<int,T> Read<T>(string path,Func<T,int> id) where T:class
        {
            var rows=JsonConvert.DeserializeObject<Dictionary<int,T>>(File.ReadAllText(path));
            if(rows==null)throw new InvalidDataException("Empty minigame metadata: "+path);
            foreach(var row in rows)if(row.Value==null || row.Key<=0 || row.Key!=id(row.Value))throw new InvalidDataException("Invalid minigame metadata ID: "+path);
            return rows;
        }
    }
}
