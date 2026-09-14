using System;using System.Collections.Generic;using System.IO;using System.IO.Compression;using System.Linq;using System.Runtime.Serialization;using System.Runtime.Serialization.Json;using System.Security.Cryptography;using System.Text;
namespace StudentAge.CampusUpdates {
[DataContract] public sealed class Feed {
 [DataMember] public int schema;
 [DataMember] public string version,layout,url,sha256;
 [DataMember] public long size;
}
[DataContract] public sealed class Payload {
 [DataMember] public int schema;
 [DataMember] public string version;
 [DataMember] public List<PayloadFile> files;
}
[DataContract] public sealed class PayloadFile {
 [DataMember] public string path,sha256;
 [DataMember] public long size;
}
[DataContract] public sealed class InstallPlan {
 [DataMember] public int pid;
 [DataMember] public long processStarted;
 [DataMember] public string root,package,sha256,version,backup;
}
public static class UpdatePackage {
 public const long MaxZip=256L*1024*1024,MaxExpanded=512L*1024*1024;
 public const string Layout="campus-minigames-v1";
 public static T Read<T>(byte[] bytes){using(var s=new MemoryStream(bytes))return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(s);}
 public static byte[] Write<T>(T value){using(var s=new MemoryStream()){new DataContractJsonSerializer(typeof(T)).WriteObject(s,value);return s.ToArray();}}
 public static string Hash(string path){using(var s=File.OpenRead(path))using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(s)).Replace("-","").ToLowerInvariant();}
 public static bool IsHash(string value)=>value!=null&&value.Length==64&&value.All(c=>c>='0'&&c<='9'||c>='a'&&c<='f');
 public static bool Newer(string candidate,string current){Version a,b;if(!Version.TryParse(candidate,out a)||!Version.TryParse(current,out b))throw new InvalidDataException("Invalid version");return a>b;}
 public static void ValidateFeed(Feed f,string repository){Uri u;if(f==null||f.schema!=1||f.layout!=Layout||!IsHash(f.sha256)||f.size<=0||f.size>MaxZip||!Uri.TryCreate(f.url,UriKind.Absolute,out u)||u.Scheme!="https"||u.Host!="github.com"||!u.IsDefaultPort||u.UserInfo.Length!=0||!u.AbsolutePath.StartsWith("/"+repository+"/releases/download/",StringComparison.Ordinal)||u.Query.Length!=0||u.Fragment.Length!=0)throw new InvalidDataException("Unsupported update feed");Newer(f.version,"0.0.0");}
 public static void ValidatePath(string relative){
  if(string.IsNullOrEmpty(relative)||relative.Length>220||relative.Contains("\\")||relative.StartsWith("/",StringComparison.Ordinal)||!(relative.StartsWith("CampusUno/",StringComparison.Ordinal)||relative.StartsWith("StudentAgeCampusMinigames/",StringComparison.Ordinal)))throw new InvalidDataException("Path outside plugin directories");
  foreach(string part in relative.Split('/')){if(part==""||part=="."||part==".."||part.EndsWith(".",StringComparison.Ordinal)||part.EndsWith(" ",StringComparison.Ordinal)||part.Any(c=>c<32||"<>:\"|?*".Contains(c)))throw new InvalidDataException("Invalid package path");string stem=part.Split('.')[0].ToUpperInvariant();if(new[]{"CON","PRN","AUX","NUL"}.Contains(stem)||Enumerable.Range(1,9).Any(n=>stem=="COM"+n||stem=="LPT"+n))throw new InvalidDataException("Reserved filename");}
 }
 public static string Under(string root,string relative){ValidatePath(relative);string full=Path.GetFullPath(Path.Combine(root,relative.Replace('/',Path.DirectorySeparatorChar))),prefix=Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;if(!full.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Path escaped installation");return full;}
 static void CheckParents(string root,string path){string stop=Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);for(string p=path;!string.IsNullOrEmpty(p);p=Path.GetDirectoryName(p)){if((File.Exists(p)||Directory.Exists(p))&&(File.GetAttributes(p)&FileAttributes.ReparsePoint)!=0)throw new IOException("Linked update target is not supported");if(string.Equals(p,stop,StringComparison.OrdinalIgnoreCase))return;}throw new IOException("Target outside root");}
 public static Payload Extract(string zip,string expectedHash,string version,string staging){
  if(!IsHash(expectedHash)||new FileInfo(zip).Length>MaxZip||Hash(zip)!=expectedHash)throw new InvalidDataException("Package checksum mismatch");Directory.CreateDirectory(staging);
  using(var archive=ZipFile.OpenRead(zip)){
   if(archive.Entries.Count>4097)throw new InvalidDataException("Too many files");var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);foreach(var entry in archive.Entries)if(!names.Add(entry.FullName))throw new InvalidDataException("Duplicate archive path");
   var metadata=archive.GetEntry("package.json");if(metadata==null||metadata.Length>1024*1024)throw new InvalidDataException("Missing package manifest");Payload payload;using(var ms=new MemoryStream()){using(var s=metadata.Open())s.CopyTo(ms);payload=Read<Payload>(ms.ToArray());}
   if(payload==null||payload.schema!=1||payload.version!=version||payload.files==null||payload.files.Count==0||payload.files.Count+1!=archive.Entries.Count)throw new InvalidDataException("Invalid package manifest");
   names.Clear();long total=0;foreach(var f in payload.files){ValidatePath(f.path);if(!names.Add(f.path)||!IsHash(f.sha256)||f.size<0||f.size>128L*1024*1024||(total+=f.size)>MaxExpanded)throw new InvalidDataException("Invalid manifest entry");var e=archive.GetEntry(f.path);if(e==null||e.Length!=f.size)throw new InvalidDataException("Missing package entry");string target=Under(staging,f.path);Directory.CreateDirectory(Path.GetDirectoryName(target));using(var input=e.Open())using(var output=new FileStream(target,FileMode.CreateNew,FileAccess.Write)){byte[] b=new byte[65536];int n;long count=0;while((n=input.Read(b,0,b.Length))>0){count+=n;if(count>f.size)throw new InvalidDataException("Expanded data exceeds declared size");output.Write(b,0,n);}if(count!=f.size)throw new InvalidDataException("Incomplete package entry");}if(Hash(target)!=f.sha256)throw new InvalidDataException("Entry checksum mismatch");}
   if(!names.Contains("CampusUno/CampusUno.dll")||!names.Contains("StudentAgeCampusMinigames/CampusMinigames.UP.dll"))throw new InvalidDataException("Update must contain both plugins");return payload;
  }
 }
 // Every target is backed up before the first replacement. Journal survives partial installs.
 public static void Apply(string root,string staging,Payload payload,string backup,Action<int> afterWrite=null){
  root=Path.GetFullPath(root);Directory.CreateDirectory(backup);var originals=new List<string>();var written=new List<string>();
  foreach(var f in payload.files){string target=Under(root,f.path);CheckParents(root,target);string source=Under(staging,f.path);if(!File.Exists(source)||Hash(source)!=f.sha256)throw new InvalidDataException("Staging changed after verification");if(File.Exists(target)){string copy=Under(backup,f.path);Directory.CreateDirectory(Path.GetDirectoryName(copy));File.Copy(target,copy,false);originals.Add(f.path);}}
  File.WriteAllBytes(Path.Combine(backup,"package.json"),Write(payload));File.WriteAllLines(Path.Combine(backup,"originals.txt"),originals);File.WriteAllText(Path.Combine(backup,"status.txt"),"applying");
  try{foreach(var f in payload.files){string target=Under(root,f.path);CheckParents(root,target);Directory.CreateDirectory(Path.GetDirectoryName(target));written.Add(f.path);Replace(Under(staging,f.path),target);afterWrite?.Invoke(written.Count);}File.WriteAllText(Path.Combine(backup,"status.txt"),"complete");}
  catch{var failures=new List<Exception>();foreach(string path in written.AsEnumerable().Reverse()){try{string target=Under(root,path);CheckParents(root,target);if(originals.Contains(path))Replace(Under(backup,path),target);else if(File.Exists(target))File.Delete(target);}catch(Exception e){failures.Add(e);}}File.WriteAllText(Path.Combine(backup,"status.txt"),failures.Count==0?"rolled-back":"rollback-needs-attention");if(failures.Count>0)throw new AggregateException("Rollback incomplete; originals retained",failures);throw;}
 }
 static void Replace(string source,string target){string temp=target+".campus-update-new";try{File.Copy(source,temp,true);if(File.Exists(target))File.Replace(temp,target,null);else File.Move(temp,target);}finally{if(File.Exists(temp))File.Delete(temp);}}
}
}
