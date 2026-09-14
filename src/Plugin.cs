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
        internal static Plugin Instance;
        internal ConfigEntry<int> GameId;
        internal ConfigEntry<float> TrustCost;
        internal ConfigEntry<int> Relation;
        internal UnoTable Table;
        internal bool Pending;
        internal int Generation;
        internal int ErrorCount;
        public static bool UpPresent { get { return AppDomain.CurrentDomain.GetAssemblies().Any(a=>a.GetType("EC2BUnofficialPatch.Features.Mechanics.Minigames.ICustomMinigame",false)!=null); } }
        static Action externalAbort;
        public static bool IsBusy { get { return Busy; } }
        public static bool AcquireExternal(Action abort) { if(Instance==null || Busy)return false;externalAbort=abort;return true; }
        public static void ReleaseExternal() { externalAbort=null; }
        public static void InvalidateExternal(IUnoSession session) { if(Instance!=null&&Instance.Table!=null&&ReferenceEquals(Instance.Table.Session,session))Instance.Table.Invalidate(); }
        public static bool OpenExternal(string npcName,IUnoSession session)=>OpenExternal(npcName,session,false);
        public static bool OpenExternal(string npcName,IUnoSession session,bool autoBegin) {
            if(Instance==null||Busy)return false;
            try{Instance.Table=Instance.gameObject.AddComponent<UnoTable>();Instance.Table.Open(npcName,session,autoBegin);return true;}
            catch{Instance.Abort();throw;}
        }
        internal static bool Busy { get { return Instance != null && (Instance.Pending || Instance.Table != null || externalAbort != null); } }
        Harmony harmony;CampusAutoUpdate updater;
        float nextRegister;
        bool warned;
        void Awake()
        {
            Instance = this;
            MinigameConfig.Load();
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
            GameId = Config.Bind("Social", "GameId", 9101, new ConfigDescription("PersonGrowCfg.minigame 的绑定编号；须避开其他模组占用。", new AcceptableValueRange<int>(1000, 10000000)));
            TrustCost = Config.Bind("Social", "TrustCost", 4f, new ConfigDescription("默认每局消耗信任；模组的首阶段配置优先。", new AcceptableValueRange<float>(0, 100)));
            Relation = Config.Bind("Social", "NeedRelation", 3, new ConfigDescription("默认关系等级；模组的首阶段配置优先。", new AcceptableValueRange<int>(0, 6)));
            // Retire the legacy option as well as its keyboard handler.
            Config.Bind("Practice", "EnableF10", false);
            Config.Remove(new ConfigDefinition("Practice", "EnableF10"));
            harmony = new Harmony("studio.studentage.campusuno");
            harmony.PatchAll(typeof(Plugin).Assembly);
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
            if (Time.unscaledTime >= nextRegister) { nextRegister = Time.unscaledTime + 1f; EnsureConfigs(); }
        }
        internal void EnsureConfigs()
        {
            if (Cfg.MinigameCfgMap == null || Cfg.MinigameActionCfgMap == null) return;
            if(UpPresent){ExternalMetadata.Register();NativeSkin.Load();return;}
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
            if (Busy) return false;
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
            var abort=externalAbort;externalAbort=null;if(abort!=null)abort();
            if (Table != null) Table.Abort();
        }
        internal void LogError(Exception e) { ErrorCount++; Logger.LogError(e); }
        internal void Log(string message) { Logger.LogInfo(message); }
        void OnDestroy() { Logger.LogInfo("UNO lifecycle Destroy"); updater?.Stop(); Abort(); if (harmony != null) harmony.UnpatchSelf(); if (Instance == this) Instance = null; }
    }

    internal sealed class SocialSession : IUnoSession, ICardSeatSession
    {
        readonly Plugin plugin;
        readonly TheEntity.Role player, npc;
        readonly int gameId, generation;
        readonly MinigameActionCfg cfg;
        readonly MinigameCfg gameCfg;
        bool charged, completed;
        public int NpcId { get { return npc.id; } }
        public string Name { get { return npc.Name; } }
        public int StartTalk { get { return cfg.startTalk; } }
        public float Cost { get { return cfg.cost; } }
        public SocialSession(Plugin owner, int npcId)
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
