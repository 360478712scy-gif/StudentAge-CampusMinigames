using System;
using System.Linq;
using Config;
using UnityEngine;
using StudentAge.CampusUno;
using StudentAge.Sanguosha;
namespace StudentAge.CampusMinigames {
 public sealed partial class SanguoshaView {
  bool testDirectory;Vector2 testScroll;
  bool HasGeneral(string id)=>Session?.TestMode==true||SanguoshaIntegration.Has(id);
  public static void OpenTestDirectory(){
   if(!NdsIntegration.IsTitleScreen||Current!=null||Plugin.IsBusy)return;
   var obj=new GameObject("SanguoshaTitleTest");var view=obj.AddComponent<SanguoshaView>();
   if(!Plugin.AcquireExternal(Invalidate)){Destroy(obj);return;}
   Current=view;view.testDirectory=true;DontDestroyOnLoad(obj);
   try{view.Build();}catch(Exception e){Debug.LogException(e);view.Close();}
  }
  void TestPeople(){
   Frame(new Rect(240,70,1120,760));Text("三国杀测试 · 选择人物和关卡",280,90,1040,60,title);
   Text("全部武将可选 · 不限回合 · 不修改存档",290,154,800,40,small);
   if(Btn("关闭",1190,150,120,40))Close();
   if(Cfg.PersonCfgMap==null)return;
   var people=Cfg.PersonCfgMap.Values.Where(p=>p.init!=null&&p.init.Count>0&&p.init[0]>1).OrderBy(p=>p.id).ToArray();
   testScroll=GUI.BeginScrollView(new Rect(275,215,1050,570),testScroll,new Rect(0,0,1020,Math.Max(560,people.Length*70)));
   for(int i=0;i<people.Length;i++){
    var person=people[i];float y=i*70;Text(person.name+"  ["+person.id+"]",15,y,270,54);
    var opponents=Encounters.For(person.id);
    for(int stage=0;stage<3;stage++)if(Btn((stage+1)+" · "+Catalog.Get(opponents[stage]).Name,295+stage*235,y+6,220,46)){
     Session=new SanguoshaSession(person.id,stage,person.name);testDirectory=false;selected="zhaoyun";page=0;
    }
   }
   GUI.EndScrollView();
  }
 }
}
