using Tuning=StudentAge.CampusUno.MinigameTuning;
using System;
using System.Collections.Generic;
namespace StudentAge.CampusPuzzles
{
    public sealed class BoxGame
    {
        readonly Random rng;public readonly int Level;public readonly int[] Items={0,1,2,3};public readonly List<int[]> Swaps=new List<int[]>();
        public float CloseDuration=>Tuning.Get("Box","CloseSeconds",Level);public float SwapDuration=>Tuning.Get("Box","SwapSeconds",Level);public float MemorizeDuration=>Tuning.Get("Box","MemorySeconds",Level);public float SwapGap=>Tuning.Get("Box","GapSeconds",Level);public int SwapCount=>Tuning.Int("Box","Swaps",Level);
        public int TotalRounds=>Tuning.Int("Box","Rounds");public int RequiredWins=>Math.Min(TotalRounds,Tuning.Int("Box","Wins"));
        public int Target{get;private set;}public int Round{get;private set;}public int Correct{get;private set;}public bool AwaitingPick{get;private set;}public bool Finished=>Round==TotalRounds&&!AwaitingPick;public bool Won=>Finished&&Correct>=RequiredWins;
        public BoxGame(int level,int seed){Level=Math.Max(1,Math.Min(5,level));rng=new Random(seed);}
        public void NextRound(){if(AwaitingPick||Round>=TotalRounds)throw new InvalidOperationException();Round++;Target=rng.Next(4);Swaps.Clear();int prevA=-1,prevB=-1;for(int n=0;n<SwapCount;n++){int a,b;do{a=rng.Next(4);b=rng.Next(4);}while(a==b||a==prevA&&b==prevB||a==prevB&&b==prevA);Swaps.Add(new[]{a,b});prevA=a;prevB=b;}AwaitingPick=true;}
        public void Swap(int a,int b){int t=Items[a];Items[a]=Items[b];Items[b]=t;}
        public bool Pick(int slot){if(!AwaitingPick||slot<0||slot>=4)throw new InvalidOperationException();AwaitingPick=false;bool ok=Items[slot]==Target;if(ok)Correct++;return ok;}
    }
}
