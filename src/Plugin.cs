using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using Config;
using HarmonyLib;
using Sdk;
using UnityEngine;

namespace StudentAge.CampusUno
{
    [BepInPlugin("studio.studentage.campusuno", "课间 UNO · 学生时代", CampusVersion.Value)]
    [BepInProcess("StudentAge.exe")]
    [BepInDependency("sa.EC2B.UnofficialPatch", BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class Plugin : BaseUnityPlugin
    {
        internal static CampusRuntime Instance;
        public static bool UpPresent { get { return AppDomain.CurrentDomain.GetAssemblies().Any(a=>a.GetType("EC2BUnofficialPatch.Features.Mechanics.Minigames.ICustomMinigame",false)!=null); } }
        static Action externalAbort;
        public static bool IsBusy { get { return Busy; } }
        public static bool AcquireExternal(Action abort) { if(Instance==null || Busy)return false;externalAbort=abort;return true; }
        public static void ReleaseExternal() { externalAbort=null; }
        internal static void AbortExternal() { var abort=externalAbort;externalAbort=null;abort?.Invoke(); }
        public static void InvalidateExternal(IUnoSession session) { if(Instance!=null&&Instance.Table!=null&&ReferenceEquals(Instance.Table.Session,session))Instance.Table.Invalidate(); }
        public static bool OpenExternal(string npcName,IUnoSession session)=>OpenExternal(npcName,session,false);
        public static bool OpenExternal(string npcName,IUnoSession session,bool autoBegin) {
            if(Instance==null||Busy)return false;
            try{Instance.Table=Instance.gameObject.AddComponent<UnoTable>();Instance.Table.Open(npcName,session,autoBegin);return true;}
            catch{Instance.Abort();throw;}
        }
        internal static bool Busy { get { return Instance != null && (Instance.Pending || Instance.Table != null || externalAbort != null); } }
        void Awake()
        {
            if (Instance != null) return;
            var host = new GameObject("CampusMinigames_RuntimeHost") { hideFlags = HideFlags.HideAndDontSave };
            UnityEngine.Object.DontDestroyOnLoad(host);
            var runtime = host.AddComponent<CampusRuntime>();
            try { runtime.Initialize(Config, Logger); }
            catch { UnityEngine.Object.Destroy(host); throw; }
        }
    }

    /// <summary>从 CampusMinigameCfg.json 某行 parms 读取的只读数值；表里没有时用代码默认值。</summary>
    internal sealed class TableValue<T>
    {
        readonly string row, key; readonly T fallback;
        internal TableValue(string row, string key, T fallback) { this.row = row; this.key = key; this.fallback = fallback; }
        internal T Value
        {
            get
            {
                if (!MinigameTables.TryParm(MinigameTables.Row(MinigameLevelConfig.CommonTable, row), key, out float v)) return fallback;
                return (T)Convert.ChangeType(v, typeof(T));
            }
        }
    }

    internal sealed class CampusRuntime : MonoBehaviour
    {
        // 无 UP 时的社交绑定默认值，来自 Cfgs/zh-cn/CampusMinigameCfg.json 的 Social 行。
        internal readonly TableValue<int> GameId = new TableValue<int>("Social", "GameId", 9101);
        internal readonly TableValue<float> TrustCost = new TableValue<float>("Social", "TrustCost", 4f);
        internal readonly TableValue<int> Relation = new TableValue<int>("Social", "NeedRelation", 3);
        internal UnoTable Table;
        internal bool Pending;
        internal int Generation;
        internal int ErrorCount;
        ConfigFile Config;
        BepInEx.Logging.ManualLogSource Logger;
        Harmony harmony;CampusAutoUpdate updater;
        float nextRegister;
        bool warned;
        internal void Initialize(ConfigFile config, BepInEx.Logging.ManualLogSource logger)
        {
            Config = config; Logger = logger; Plugin.Instance = this;
            MinigameLevelConfig.Apply();{var encounters=SanguoshaTables.LoadEncounters(out string[] fallbackGenerals);StudentAge.Sanguosha.Encounters.Configure(encounters,fallbackGenerals);}
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
            // Retire the legacy option as well as its keyboard handler.
            Config.Bind("Practice", "EnableF10", false);
            Config.Remove(new ConfigDefinition("Practice", "EnableF10"));
            harmony = new Harmony("studio.studentage.campusuno");
            HarmonyScope.PatchNamespace(harmony, typeof(Plugin).Assembly, "StudentAge.CampusUno");
            Logger.LogInfo("Campus UNO "+CampusVersion.Value+" loaded | untimed quick rounds | social ID " + GameId.Value);
            Logger.LogInfo("Game assembly: " + typeof(FuncMgr).Assembly.ManifestModule.ModuleVersionId);
            bool smoke = Environment.GetCommandLineArgs().Contains("--campus-uno-smoke") || Config.Bind("Diagnostics", "SmokeTest", false, "仅供隔离副本自动验收；普通使用保持 false。").Value;
            Logger.LogInfo("UNO lifecycle Awake | active " + gameObject.activeInHierarchy + " | enabled " + enabled + " | smoke " + smoke);
            if (smoke) gameObject.AddComponent<SmokeHarness>();
            updater=new CampusAutoUpdate(Config,Logger);updater.Start();
        }
        void Start() { Logger.LogInfo("UNO lifecycle Start"); }
        void Update()
        {
            if (Time.unscaledTime >= nextRegister) { nextRegister = Time.unscaledTime + 1f; SavePathGuard.Tick(Logger); EnsureConfigs(); }
        }
        internal void EnsureConfigs()
        {
            if (Cfg.MinigameCfgMap == null || Cfg.MinigameActionCfgMap == null) return;
            if(Plugin.UpPresent){ExternalMetadata.Register();NativeSkin.Load();return;}
            int id = GameId.Value, stage = id * 100 + 1;
            if (!Cfg.MinigameCfgMap.ContainsKey(id))
                Cfg.MinigameCfgMap[id] = new MinigameCfg { id = id, name = "课间 UNO", tips = "同色、同数字或同功能出牌。率先出完手牌获胜！", bgm = 8 };
            if (!Cfg.MinigameActionCfgMap.ContainsKey(stage))
                Cfg.MinigameActionCfgMap[stage] = new MinigameActionCfg { id = stage, cost = TrustCost.Value, needRelation = Relation.Value, effect = new List<List<float>>(), parms = new List<float>() };
            NativeSkin.Load();
            if (!warned) { warned = true; Logger.LogInfo("Social metadata ready: game " + id + ", repeatable stage " + stage); }
        }
        internal bool IsBound(int npcId)
        {
            PersonGrowCfg person;
            return Cfg.PersonGrowCfgMap != null && Cfg.PersonGrowCfgMap.TryGetValue(npcId, out person) && person.minigame == GameId.Value;
        }
        internal bool OpenSocial(int npcId, int bgId)
        {
            if (Plugin.Busy) return false;
            EnsureConfigs();
            SocialSession session;
            try { session = new SocialSession(this, npcId); if (!session.Validate()) return false; }
            catch (Exception e) { LogError(e); return false; }
            int token = Generation;
            Action open = () => {
                if (token != Generation) return;
                Pending = false;
                if (!session.Validate()) return;
                try { Table = gameObject.AddComponent<UnoTable>(); Table.Open(session.Name, session); }
                catch (Exception e) { LogError(e); Abort(); }
            };
            try
            {
                if (session.StartTalk != 0)
                {
                    if (Cfg.TalkCfgMap == null || !Cfg.TalkCfgMap.ContainsKey(session.StartTalk)) { Logger.LogWarning("UNO startTalk missing: " + session.StartTalk); return false; }
                    Pending = true; Singleton<CommonEvtMgr>.Ins.ShowTalk(session.StartTalk, open, bgId);
                }
                else open();
                return true;
            }
            catch (Exception e) { Pending = false; LogError(e); return false; }
        }
        internal void Abort()
        {
            Generation++; Pending = false;
            Plugin.AbortExternal();
            if (Table != null) Table.Abort();
        }
        internal void LogError(Exception e) { ErrorCount++; Logger.LogError(e); }
        internal void Log(string message) { Logger.LogInfo(message); }
        void OnDestroy() { Logger.LogInfo("UNO lifecycle Destroy"); updater?.Stop(); Abort(); if (harmony != null) harmony.UnpatchSelf(); if (Plugin.Instance == this) Plugin.Instance = null; }
    }

    internal sealed class SocialSession : IUnoSession, ICardSeatSession
    {
        readonly CampusRuntime plugin;
        readonly TheEntity.Role player, npc;
        readonly int gameId, generation;
        readonly MinigameActionCfg cfg;
        readonly MinigameCfg gameCfg;
        bool charged, completed;
        public int NpcId { get { return npc.id; } }
        public string Name { get { return npc.Name; } }
        public int StartTalk { get { return cfg.startTalk; } }
        public float Cost { get { return cfg.cost; } }
        public SocialSession(CampusRuntime owner, int npcId)
        {
            plugin = owner; generation = owner.Generation; gameId = owner.GameId.Value;
            player = Singleton<RoleMgr>.Ins.GetRole(); npc = Singleton<RoleMgr>.Ins.GetRole(npcId);
            cfg = Cfg.MinigameActionCfgMap[gameId * 100 + 1]; gameCfg = Cfg.MinigameCfgMap[gameId];
        }
        bool SameWorld()
        {
            return plugin.Generation == generation && ReferenceEquals(player, Singleton<RoleMgr>.Ins.GetRole()) && ReferenceEquals(npc, Singleton<RoleMgr>.Ins.GetRole(npc.id));
        }
        public bool Validate()
        {
            return npc != null && player != null && SameWorld() && plugin.IsBound(npc.id) && !float.IsNaN(cfg.cost) && !float.IsInfinity(cfg.cost) && cfg.cost >= 0 && npc.Relation >= cfg.needRelation && !Singleton<RoundMgr>.Ins.IsHoliday() && Singleton<RoleMgr>.Ins.HasEnoughCost(3, cfg.cost)
                && (cfg.winTalk == 0 || Cfg.TalkCfgMap.ContainsKey(cfg.winTalk)) && (cfg.loseTalk == 0 || Cfg.TalkCfgMap.ContainsKey(cfg.loseTalk));
        }
        public bool IsExternal { get { return false; } }
        public void Cancel() {}
        public bool Begin()
        {
            if (charged || completed || !Validate()) return false;
            player.UpdateAttr(3, -cfg.cost, 1f, DescCtrl.GetFromTag(gameCfg.name)); charged = true;
            plugin.Log("UNO begin | NPC " + npc.id + " | trust " + cfg.cost); return true;
        }
        public void Finish(Outcome outcome)
        {
            if (!charged || completed) return;
            completed = true;
            if (!SameWorld()) { plugin.Log("UNO settlement skipped: game context changed"); return; }
            bool win = outcome == Outcome.Win;
            float actualFavor = 0;
            try
            {
                Singleton<RoleMgr>.Ins.GetNeedsData().DoSocial(DescCtrl.GetFromTag(gameCfg.name));
                if (win)
                {
                    var effector = CommonEvtMgr.GenEffector(cfg.effect);
                    if (effector != null) { effector.SetTag(DescCtrl.GetFromTag(gameCfg.name)); effector.Run(1f + player.IncCtrl.GetValue(RoleIncType.OtherAttrMul, 220)); }
                    actualFavor = npc.UpdateFavor(2f, 1f, DescCtrl.GetFromTag(gameCfg.name));
                }
                EventMgr.Send(1603);
                int talk = win ? cfg.winTalk : outcome == Outcome.Lose ? cfg.loseTalk : 0;
                if (talk != 0) Singleton<CommonEvtMgr>.Ins.ShowTalk(talk);
                plugin.Log("UNO finished | NPC " + npc.id + " | " + outcome + " | actual favor " + actualFavor);
            }
            catch (Exception e) { plugin.LogError(e); }
        }
    }

    [HarmonyPatch(typeof(FuncMgr), "GetMiniGameData")]
    static class RegisterPatch { static void Prefix() { Plugin.Instance.EnsureConfigs(); } }

    [HarmonyPatch(typeof(MiniGameData), "GetGameByNpc")]
    static class NpcGamePatch
    {
        static bool Prefix(int _npcId, ref MiniGameSubData __result)
        {
            if (Plugin.UpPresent || !Plugin.Instance.IsBound(_npcId)) return true;
            Plugin.Instance.EnsureConfigs();
            // Repeatable rounds use fresh stage-one metadata; never touch vanilla's shared game dictionary.
            __result = new MiniGameSubData { id = Plugin.Instance.GameId.Value, npcId = _npcId }; return false;
        }
    }
    [HarmonyPatch(typeof(MiniGameData), "SocialGame")]
    static class SocialPatch
    {
        static bool Prefix(int _npcId, int _bgId, ref bool __result)
        {
            if (Plugin.UpPresent || !Plugin.Instance.IsBound(_npcId)) return true;
            __result = Plugin.Instance.OpenSocial(_npcId, _bgId); return false;
        }
    }
    [HarmonyPatch(typeof(FuncMgr), "OpenMiniGame")]
    static class OpenPatch
    {
        static bool Prefix(int _gameId, MiniGameFromType _type)
        {
            if (Plugin.UpPresent || _gameId != Plugin.Instance.GameId.Value) return true;
            // Social sessions are opened by SocialPatch, which captures the correct NPC.
            // Other story entry modes require dedicated continuation adapters and are not advertised in v0.1.
            Plugin.Instance.Log("UNO direct OpenMiniGame ignored (" + _type + "). Use role social entry or the NDS console.");
            return false;
        }
    }
    [HarmonyPatch(typeof(Game), "LoadGame")]
    static class LoadPatch { static void Prefix() { Plugin.Instance.Abort(); } }
    [HarmonyPatch(typeof(Game), "NewGame")]
    static class NewPatch { static void Prefix() { Plugin.Instance.Abort(); } }
    [HarmonyPatch(typeof(Game), "BackToMain")]
    static class BackPatch { static void Prefix() { Plugin.Instance.Abort(); } }
    [HarmonyPatch(typeof(Game), "QuickSaveGame")]
    static class QuickSavePatch { static bool Prefix() { return !Plugin.Busy; } }
}
