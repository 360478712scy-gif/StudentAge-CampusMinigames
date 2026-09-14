using System;
using System.Collections.Generic;
namespace StudentAge.PlayingCards
{
    public enum CardSuit { Clubs,Diamonds,Hearts,Spades,Joker }
    public readonly struct PlayingCard : IEquatable<PlayingCard>
    {
        public readonly CardSuit Suit;public readonly int Rank;
        public PlayingCard(CardSuit suit,int rank){if((int)suit<0||(int)suit>4||rank<1||rank>(suit==CardSuit.Joker?2:13))throw new ArgumentOutOfRangeException();Suit=suit;Rank=rank;}
        public int Id=>Suit==CardSuit.Joker?51+Rank:(int)Suit*13+Rank-1;
        public string AssetName=>Suit==CardSuit.Joker?(Rank==1?"black_joker":"red_joker"):(Rank==1?"ace":Rank==11?"jack":Rank==12?"queen":Rank==13?"king":Rank.ToString())+"_of_"+Suit.ToString().ToLowerInvariant();
        public bool Equals(PlayingCard c)=>Suit==c.Suit&&Rank==c.Rank;
        public override bool Equals(object o)=>o is PlayingCard&&Equals((PlayingCard)o);
        public override int GetHashCode()=>Id;
    }
    // Game-independent identity, shuffle, draw, hands and discard. No UNO or landlord rules here.
    public sealed class PlayingDeck
    {
        readonly List<PlayingCard> stock=new List<PlayingCard>(),discard=new List<PlayingCard>();
        public int Remaining=>stock.Count;public IReadOnlyList<PlayingCard> Discarded=>discard;
        public PlayingDeck(int seed,bool jokers=true){for(int suit=0;suit<4;suit++)for(int rank=1;rank<=13;rank++)stock.Add(new PlayingCard((CardSuit)suit,rank));if(jokers){stock.Add(new PlayingCard(CardSuit.Joker,1));stock.Add(new PlayingCard(CardSuit.Joker,2));}Shuffle(stock,new Random(seed));}
        public static void Shuffle<T>(IList<T> cards,Random rng){for(int i=cards.Count-1;i>0;i--){int j=rng.Next(i+1);T t=cards[i];cards[i]=cards[j];cards[j]=t;}}
        public PlayingCard Draw(){if(stock.Count==0)throw new InvalidOperationException("Deck is empty");var c=stock[stock.Count-1];stock.RemoveAt(stock.Count-1);return c;}
        public PlayingCard DrawWhere(Func<PlayingCard,bool> match){int index=stock.FindIndex(c=>match(c));if(index<0)throw new InvalidOperationException("No matching card remains");var card=stock[index];stock.RemoveAt(index);return card;}
        public List<PlayingCard>[] Deal(int players,int perHand){if(players<1||perHand<0||players*perHand>Remaining)throw new ArgumentOutOfRangeException();var hands=new List<PlayingCard>[players];for(int n=0;n<players;n++)hands[n]=new List<PlayingCard>();for(int n=0;n<perHand;n++)for(int p=0;p<players;p++)hands[p].Add(Draw());return hands;}
        public void DiscardFrom(IList<PlayingCard> hand,int index){if(index<0||index>=hand.Count)throw new ArgumentOutOfRangeException();discard.Add(hand[index]);hand.RemoveAt(index);}
    }
}
