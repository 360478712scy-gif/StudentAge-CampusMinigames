using System.IO;using BepInEx;using BepInEx.Configuration;
namespace StudentAge.CampusUno {
internal static class MinigameConfig {
 public static void Load(){var file=new ConfigFile(Path.Combine(Paths.ConfigPath,"studio.studentage.minigames.cfg"),false);file.SaveOnConfigSet=false;
 MinigameTuning.Configure(s=>{var desc=new ConfigDescription(s.Description+" 修改后重启游戏生效。",s.Integer?(AcceptableValueBase)new AcceptableValueRange<int>((int)s.Min,(int)s.Max):new AcceptableValueRange<float>(s.Min,s.Max));
 if(s.Integer){var e=file.Bind(s.Section,s.Key,(int)s.Default,desc);e.Value=(int)s.Clamp(e.Value);return e.Value;}
 var f=file.Bind(s.Section,s.Key,s.Default,desc);f.Value=s.Clamp(f.Value);return f.Value;});file.Save();}
}
}
