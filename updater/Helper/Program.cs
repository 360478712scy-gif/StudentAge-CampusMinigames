using System;using System.Diagnostics;using System.IO;using System.Threading;using StudentAge.CampusUpdates;
internal static class Program {
 static int Main(string[] args){string log=null;try{if(args.Length!=1)return 2;string planPath=Path.GetFullPath(args[0]);log=Path.Combine(Path.GetDirectoryName(planPath),"install.log");var p=UpdatePackage.Read<InstallPlan>(File.ReadAllBytes(planPath));
  if(p.pid<=0||p.processStarted<=0||string.IsNullOrEmpty(p.root)||!Directory.Exists(p.root)||!Path.IsPathRooted(p.package)||!Path.IsPathRooted(p.backup))throw new InvalidDataException("Invalid install plan");
  // Keep an exclusive transaction lock for the entire wait and install; a second launch cannot race.
  using(var gate=new FileStream(Path.Combine(Path.GetDirectoryName(planPath),"install.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)){
   Log(log,"Waiting for game exit");try{using(var parent=Process.GetProcessById(p.pid)){if(parent.StartTime.ToUniversalTime().Ticks!=p.processStarted)throw new InvalidOperationException("Process identity changed");parent.WaitForExit();}}catch(ArgumentException){}
   Thread.Sleep(1500);string staging=Path.Combine(Path.GetDirectoryName(planPath),"verified-"+Guid.NewGuid().ToString("N"));var payload=UpdatePackage.Extract(p.package,p.sha256,p.version,staging);UpdatePackage.Apply(p.root,staging,payload,p.backup);Directory.Delete(staging,true);File.WriteAllText(Path.Combine(Path.GetDirectoryName(planPath),"installed.txt"),p.version);Log(log,"Update installed: "+p.version);
  }return 0;
 }catch(Exception e){if(log!=null)try{Log(log,"FAILED: "+e);}catch{}return 1;}}
 static void Log(string path,string message){File.AppendAllText(path,DateTime.UtcNow.ToString("O")+" "+message+Environment.NewLine);}
}
