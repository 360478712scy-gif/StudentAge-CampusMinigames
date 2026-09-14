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
        public void Open(CustomMinigameContext context){var session=new UpSession(context);int level=context.ActionCfgId-context.GameId*100;if(level<1||level>5)throw new InvalidOperationException("Gomoku requires social stage 1–5");GomokuView.Open(session,level);}
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
