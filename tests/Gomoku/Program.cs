using StudentAge.CampusMinigames;
int checks=0;void Check(bool ok,string text){if(!ok)throw new Exception(text);checks++;}
foreach(var dir in new[]{(1,0),(0,1),(1,1),(1,-1)}){var b=new int[225];for(int n=0;n<4;n++)b[(8+n*dir.Item2)*15+4+n*dir.Item1]=1;foreach(int p in new[]{1,2}){int i=GomokuAI.Choose(b,p,5,rng:()=>0);Check(b[i]==0,"legal");b[i]=1;Check(GomokuGame.FindLine(b,i)!=null,"win/block direction");b[i]=0;}}
var broken=new int[225];foreach(int x in new[]{3,4,6,7})broken[105+x]=1;
int[] expected={65,72,79,85,90};for(int level=1;level<=5;level++){int defended=0;for(int n=0;n<100;n++){bool first=true;int trial=n;double Rng(){if(first){first=false;return (trial+.5)/100;}return .5;}if(GomokuAI.Choose(broken,2,level,rng:Rng)==110)defended++;}Check(defended==expected[level-1],"graded missed-defense rate "+level);}
var g=new GomokuGame();for(int n=0;n<4;n++){g.Play(n);g.Play(30+n);}Check(g.Play(4)&&g.Winner==1,"win");Check(!g.Play(99),"terminal rejects move");g.Undo();Check(g.Winner==0&&g.Turn==1,"undo restores turn");Check(!g.Play(-1)&&!g.Play(225)&&!g.Play(1),"invalid moves");var source=new int[225];source[112]=1;var copy=source.ToArray();for(int level=1;level<=5;level++){Check(source[GomokuAI.Choose(source,2,level,budgetOverride:25)]==0,"search legal");Check(source.SequenceEqual(copy),"immutable");}
var cancelled=new CancellationToken(true);try{GomokuAI.Choose(source,2,5,cancelled);throw new Exception("cancellation ignored");}catch(OperationCanceledException){checks++;}
// A missed defense must continue a local plan, never wander to an isolated corner.
for(int level=1;level<=5;level++){
 var b=new int[225];foreach(int x in new[]{3,4,6,7})b[105+x]=1;b[8*15+8]=b[9*15+8]=2;
 var copyB=b.ToArray();int missed=GomokuAI.Choose(b,2,level,rng:()=>.999);
 Check(missed!=110,"lapse actually overlooks urgent block "+level);
 Check(Enumerable.Range(0,225).Any(i=>b[i]==2&&Math.Abs(i%15-missed%15)<=2&&Math.Abs(i/15-missed/15)<=2),"miss still develops own nearby line "+level);
 Check(b.SequenceEqual(copyB),"lapse leaves source unchanged "+level);
 int[] winning=new int[225];for(int x=4;x<8;x++)winning[105+x]=2;
 int found=GomokuAI.Choose(winning,2,level,rng:()=>0);winning[found]=2;
 Check(GomokuGame.FindLine(winning,found)!=null,"all stages can complete own five "+level);
}
Console.WriteLine("GOMOKU_NATIVE_CHECKS_OK "+checks);
