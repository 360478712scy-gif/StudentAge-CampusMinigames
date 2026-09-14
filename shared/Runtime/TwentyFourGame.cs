using Tuning=StudentAge.CampusUno.MinigameTuning;
using System;
using System.Collections.Generic;
using System.Linq;
using StudentAge.PlayingCards;
namespace StudentAge.CampusPuzzles
{
    public readonly struct Fraction : IEquatable<Fraction>
    {
        public readonly long N,D;
        public Fraction(long n,long d=1){if(d==0)throw new DivideByZeroException();if(d<0){n=-n;d=-d;}long a=Math.Abs(n),b=d;while(b!=0){long t=a%b;a=b;b=t;}N=n/a;D=d/a;}
        public static Fraction Apply(Fraction a,Fraction b,char op){switch(op){case '+':return new Fraction(a.N*b.D+b.N*a.D,a.D*b.D);case '-':return new Fraction(a.N*b.D-b.N*a.D,a.D*b.D);case '*':return new Fraction(a.N*b.N,a.D*b.D);case '/':return new Fraction(a.N*b.D,a.D*b.N);default:throw new ArgumentException();}}
        public bool Equals(Fraction b)=>N==b.N&&D==b.D;
        public override string ToString()=>D==1?N.ToString():N+"/"+D;
    }
    public sealed class CalcToken
    {
        public readonly Fraction Value;public readonly string Expression;public readonly int Mask;
        public CalcToken(Fraction v,string e,int mask){Value=v;Expression=e;Mask=mask;}
    }
    public sealed class TwentyFourGame
    {
        public readonly int Level;public readonly PlayingCard[] Cards=new PlayingCard[4];
        public readonly List<CalcToken> Tokens=new List<CalcToken>();readonly Stack<CalcToken[]> history=new Stack<CalcToken[]>();readonly Random rng;bool submitted;readonly HashSet<int> asked=new HashSet<int>();public PlayingDeck Deck{get;private set;}
        public int RequiredAnswers=>Tuning.Int("TwentyFour","Answers",Level);public float TimeLimit=>Tuning.Get("TwentyFour","Seconds",Level);
        public int Solved{get;private set;}public int Mistakes{get;private set;}public bool Won=>Solved>=RequiredAnswers;public float Remaining{get;private set;}=120;public bool Lost=>Remaining<=0&&!Won;public void Tick(float seconds){if(!Won&&!Lost&&!submitted&&!QuestionSolved)Remaining=Math.Max(0,Remaining-Math.Max(0,seconds));}public string LastExpression{get;private set;}public bool QuestionSolved=>Tokens.Count==1&&Tokens[0].Value.Equals(new Fraction(24));
        static readonly int[][][] Questions={
            new[]{new[]{1,7,8,10},new[]{2,8,9,9},new[]{3,8,9,10},new[]{4,8,10,10},new[]{2,6,10,10},new[]{1,5,10,10}},
            new[]{new[]{1,2,3,4},new[]{2,3,4,6},new[]{3,3,4,4},new[]{1,2,4,8},new[]{2,2,4,8},new[]{1,3,5,6}},
            new[]{new[]{2,3,5,9},new[]{1,4,5,6},new[]{2,4,6,8},new[]{2,3,7,8},new[]{1,3,7,9},new[]{2,5,6,8}},
            new[]{new[]{2,3,5,7},new[]{2,2,5,9},new[]{1,4,6,8},new[]{2,3,6,9},new[]{3,4,6,8},new[]{1,2,7,10}},
            new[]{new[]{1,3,4,6},new[]{2,3,5,7},new[]{2,2,5,9},new[]{2,5,7,10},new[]{3,4,5,6},new[]{1,4,7,9}}};
        public TwentyFourGame(int level,int seed){Level=Math.Max(1,Math.Min(5,level));rng=new Random(seed);NewQuestion();}
        public void NewQuestion(){if(Won||Lost)return;submitted=false;Remaining=TimeLimit;var bank=Questions[Level-1];if(asked.Count>=bank.Length)asked.Clear();int question;do{question=rng.Next(bank.Length);}while(!asked.Add(question));var ranks=(int[])bank[question].Clone();PlayingDeck.Shuffle(ranks,rng);Deck=new PlayingDeck(rng.Next(),false);for(int i=0;i<4;i++){int rank=ranks[i];Cards[i]=Deck.DrawWhere(c=>c.Rank==rank);}Reset();}
        public void Reset(){if(submitted||Won||Lost)return;Tokens.Clear();history.Clear();for(int i=0;i<4;i++)Tokens.Add(new CalcToken(new Fraction(Cards[i].Rank),Cards[i].Rank.ToString(),1<<i));}
        public bool Combine(int first,int second,char op){if(Won||Lost||QuestionSolved||first<0||second<0||first>=Tokens.Count||second>=Tokens.Count||first==second)return false;var a=Tokens[first];var b=Tokens[second];if(op=='/'&&b.Value.N==0)return false;Fraction result=Fraction.Apply(a.Value,b.Value,op);history.Push(Tokens.ToArray());int at=Math.Min(first,second);Tokens.RemoveAt(Math.Max(first,second));Tokens.RemoveAt(at);Tokens.Insert(at,new CalcToken(result,"("+a.Expression+(op=='*'?"×":op=='/'?"÷":op.ToString())+b.Expression+")",a.Mask|b.Mask));return true;}
        public bool Submit(){if(Won||Lost||submitted||!QuestionSolved)return false;LastExpression=Tokens[0].Expression;submitted=true;Solved++;return true;}
        public void Undo(){if(history.Count==0||submitted||Won||Lost)return;Tokens.Clear();Tokens.AddRange(history.Pop());}
        public static string Solve(IEnumerable<int> numbers){return Search(numbers.Select((n,i)=>new CalcToken(new Fraction(n),n.ToString(),1<<i)).ToList());}
        static string Search(List<CalcToken> list){if(list.Count==1)return list[0].Value.Equals(new Fraction(24))?list[0].Expression:null;for(int i=0;i<list.Count;i++)for(int j=0;j<list.Count;j++){if(i==j)continue;foreach(char op in "+-*/"){if((op=='+'||op=='*')&&i>j||op=='/'&&list[j].Value.N==0)continue;var next=list.Where((x,n)=>n!=i&&n!=j).ToList();next.Add(new CalcToken(Fraction.Apply(list[i].Value,list[j].Value,op),"("+list[i].Expression+op+list[j].Expression+")",0));var solution=Search(next);if(solution!=null)return solution;}}return null;}
    }
}
