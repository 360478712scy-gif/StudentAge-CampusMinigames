using Tuning=StudentAge.CampusUno.MinigameTuning;
using System;using System.Collections.Generic;using System.Linq;using StudentAge.PlayingCards;
namespace StudentAge.CampusPuzzles
{
    public sealed class LandlordGame
    {
        public readonly int Level;readonly Random rng;PlayingDeck deck;int bids,highestSeat=-1,first,passes;
        public List<PlayingCard>[] Hands{get;private set;}public PlayingCard[] Bottom{get;private set;}public int Turn{get;private set;}public bool Bidding{get;private set;}public int HighBid{get;private set;}public int Landlord{get;private set;}=-1;public int Winner{get;private set;}=-1;public bool HumanWon=>Winner>=0&&(Winner==Landlord?Landlord==0:Landlord!=0);public int Deals{get;private set;}public int Revision{get;private set;}public LandlordPattern Previous{get;private set;}public int LastPlayer{get;private set;}=-1;public PlayingCard[] LastCards{get;private set;}=new PlayingCard[0];public int DiscardCount=>deck.Discarded.Count;public string LastAction{get;private set;}
        public LandlordGame(int level,int seed){Level=Math.Max(1,Math.Min(5,level));rng=new Random(seed);first=rng.Next(3);Deal();}
        void Deal(){deck=new PlayingDeck(rng.Next());Hands=deck.Deal(3,17);Bottom=new[]{deck.Draw(),deck.Draw(),deck.Draw()};foreach(var h in Hands)Sort(h);Bidding=true;HighBid=0;highestSeat=-1;bids=passes=0;Landlord=Winner=-1;Previous=default;LastCards=new PlayingCard[0];LastPlayer=-1;Turn=first;first=(first+1)%3;Deals++;Revision++;LastAction="重新发牌";}
        static void Sort(List<PlayingCard> hand)=>hand.Sort((a,b)=>{int c=LandlordRules.Rank(b).CompareTo(LandlordRules.Rank(a));return c!=0?c:b.Id.CompareTo(a.Id);});
        public bool Bid(int seat,int score){if(!Bidding||seat!=Turn||score<0||score>3||score>0&&score<=HighBid)return false;LastAction=score==0?"不叫":"叫 "+score+" 分";if(score>0){HighBid=score;highestSeat=seat;}bids++;Revision++;if(score==3||bids==3){if(highestSeat<0){Deal();return true;}Landlord=highestSeat;Hands[Landlord].AddRange(Bottom);Sort(Hands[Landlord]);Bidding=false;Turn=Landlord;LastPlayer=Landlord;LastAction="地主先出";}else Turn=(Turn+1)%3;return true;}
        public bool CanPass=>!Bidding&&Winner<0&&Previous.Valid&&LastPlayer!=Turn;
        public bool Pass(int seat){if(seat!=Turn||!CanPass)return false;LastAction="不出";passes++;Turn=(Turn+1)%3;if(passes==2){Turn=LastPlayer;Previous=default;LastCards=new PlayingCard[0];passes=0;}Revision++;return true;}
        public bool Play(int seat,IEnumerable<PlayingCard> selection){if(Bidding||Winner>=0||seat!=Turn)return false;var chosen=selection.ToArray();if(chosen.Length==0||chosen.Select(c=>c.Id).Distinct().Count()!=chosen.Length||chosen.Any(c=>!Hands[seat].Contains(c)))return false;var pattern=LandlordRules.Classify(chosen);if(!pattern.Beats(Previous))return false;foreach(int index in chosen.Select(c=>Hands[seat].IndexOf(c)).OrderByDescending(i=>i))deck.DiscardFrom(Hands[seat],index);Previous=pattern;LastCards=chosen;LastPlayer=seat;passes=0;LastAction=LandlordRules.Name(pattern.Kind);if(Hands[seat].Count==0)Winner=seat;else Turn=(Turn+1)%3;Revision++;return true;}
        public int AiBid(int seat){if(!Bidding||seat!=Turn)return 0;var counts=Hands[seat].GroupBy(LandlordRules.Rank);int strength=counts.Sum(g=>g.Key>=16?3:g.Key==15?2:g.Key==14?1:0)+counts.Count(g=>g.Count()==4)*5;int want=strength>=12?3:strength>=8?2:strength>=5||Deals>1?1:0;return want>HighBid?want:0;}
        public PlayingCard[] Choose(int seat,int overrideLevel=0)
        {
            if(Bidding||Winner>=0||seat!=Turn)return null;int skill=overrideLevel>0?overrideLevel:Tuning.Int("Landlord","AiLevel",Level);var moves=LandlordRules.Moves(Hands[seat],Previous);if(moves.Count==0)return null;var finish=moves.FirstOrDefault(m=>m.Length==Hands[seat].Count);if(finish!=null)return finish;
            bool teammate=seat!=Landlord&&LastPlayer!=Landlord&&LastPlayer!=seat;
            if(Previous.Valid&&teammate&&skill>=2)return null;
            if(skill==1){var simple=moves.Where(m=>m.Length<=2&&LandlordRules.Classify(m).Kind!=LandlordKind.Rocket).OrderBy(m=>LandlordRules.Classify(m).High).Take(3).ToArray();if(simple.Length>0)return simple[rng.Next(simple.Length)];return moves.OrderBy(m=>m.Length).ThenBy(m=>LandlordRules.Classify(m).High).First();}
            // Uses own cards plus public roles/counts only; no opponent hand inspection.
            bool danger=Enumerable.Range(0,3).Any(p=>p!=seat&&(seat==Landlord||p==Landlord)&&Hands[p].Count<=2);
            var groups=Hands[seat].GroupBy(LandlordRules.Rank).ToDictionary(g=>g.Key,g=>g.Count());
            return moves.OrderByDescending(m=>{var p=LandlordRules.Classify(m);var picked=m.GroupBy(LandlordRules.Rank).ToDictionary(g=>g.Key,g=>g.Count());int splits=groups.Count(g=>picked.ContainsKey(g.Key)&&picked[g.Key]<g.Value);float value=m.Length*(skill>=4?13:9)-p.High*.55f-splits*(skill>=3?7:2);if(p.Kind==LandlordKind.Bomb||p.Kind==LandlordKind.Rocket)value-=Previous.Valid?16:28;if(skill>=4&&danger&&Previous.Valid)value+=p.High*2.2f;if(skill>=3){int loose=groups.Count(g=>g.Value-(picked.ContainsKey(g.Key)?picked[g.Key]:0)==1);value-=loose*2;}return value;}).First();
        }
    }
}
