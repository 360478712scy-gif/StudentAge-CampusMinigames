using System;
using System.IO;
using System.Reflection;
using Config;
using EC2BUnofficialPatch.Features.Mechanics.Minigames;
using Sdk;
using StudentAge.CampusUno;
using UnityEngine;

namespace StudentAge.CampusMinigames
{
    public sealed class UnoEntry : ICustomMinigame
    {
        public void Open(CustomMinigameContext context){var session=new UpSession(context);if(!Plugin.OpenExternal(session.Name,session))throw new InvalidOperationException("Another minigame is active or CampusUno plugin is missing");session.OnInvalidated(()=>Plugin.InvalidateExternal(session));}
    }
    public sealed class GomokuEntry : ICustomMinigame
    {
        public void Open(CustomMinigameContext context){var session=new UpSession(context);GomokuView.Open(session,UpLevel.Resolve(context,9102,5));}
    }
    /// <summary>
    /// 决定这一局玩第几关。优先级：
    /// 1. TalkCfg/OptionCfg 的 miniGame[1]：关卡号（3）或完整阶段 id（910203）——剧情里亲自打开时用；
    /// 2. 社交阶段：ActionCfgId - 游戏编号×100；
    /// 3. 都没有时第 1 关。超出游戏内置关数时按最高一关处理并记录日志。
    /// </summary>
    public static class UpLevel
    {
        public static int Resolve(CustomMinigameContext context,int gameId,int stageCount)
        {
            int level=0;string source="default";
            if(context.LaunchParameters!=null&&context.LaunchParameters.Count>0){int p=(int)Math.Round(context.LaunchParameters[0]);level=p>=100?p%100:p;source="miniGame["+p+"]";}
            if(level<=0&&context.ActionCfgId>0){int logical=context.GameId>0?context.GameId:gameId;level=context.ActionCfgId-logical*100;source="stage "+context.ActionCfgId;}
            if(level<=0)level=1;
            if(level>stageCount){Debug.LogWarning("Campus UP | game "+gameId+" | "+source+" 超出内置关数 "+stageCount+"，按最高一关处理");level=stageCount;}
            Debug.Log("Campus UP Open | game "+gameId+" | level "+level+" | from "+context.LaunchFrom+" ("+source+")");
            return level;
        }
    }
    public sealed class UpSession : IGameSession
    {
        readonly CustomMinigameContext context;bool ended,began;IDisposable tuning;
        public string Name{get;private set;}public string Root{get;private set;}public int NpcId{get{return context.NpcId;}}
        public bool IsExternal{get{return true;}}public float Cost{get{return 0;}}
        // All public UP callbacks are invoked on the Unity main thread.
        public UpSession(CustomMinigameContext request){context=request??throw new ArgumentNullException(nameof(request));var role=Singleton<RoleMgr>.Ins.GetRole(context.NpcId);Name=role==null?"同桌":role.Name;Root=Path.GetDirectoryName(context.SourceFile);
            if(context.Parameters.TryGetValue("tuning",out string json)){
                var values=Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string,float>>(json);
                if(values!=null&&values.Count>0)tuning=MinigameTuning.Push(values);
            }
            context.Invalidated+=ReleaseTuning;}
        void ReleaseTuning(){tuning?.Dispose();tuning=null;}
        public bool IsActive{get{return !ended&&context.IsActive;}}
        public bool Begin(){if(!IsActive)return false;if(began)return true;began=context.Begin();return began;}
        public void Finish(Outcome outcome){if(ended)return;ended=true;Debug.Log("Campus UP Complete | game "+context.GameId+" | stage "+context.ActionCfgId+" | outcome "+outcome);ReleaseTuning();context.Complete(outcome==Outcome.Win);}
        public void Cancel(){if(ended)return;ended=true;ReleaseTuning();context.Cancel();}
        public void OnInvalidated(Action callback){context.Invalidated+=()=>{ended=true;callback();};}
    }
}
