using Tuning=StudentAge.CampusUno.MinigameTuning;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StudentAge.CampusUno
{
    public enum Suit { Red, Blue, Green, Yellow, Wild }
    public enum Face { Zero, One, Two, Three, Four, Five, Six, Seven, Eight, Nine, Skip, Reverse, DrawTwo, Wild, DrawFour }
    public enum Outcome { Playing, Win, Lose, Draw }

    public struct Card
    {
        public readonly Suit Suit;
        public readonly Face Face;
        public Card(Suit suit, Face face) { Suit = suit; Face = face; }
        public bool IsWild { get { return Face == Face.Wild || Face == Face.DrawFour; } }
        public int Points { get { return IsWild ? 50 : Face >= Face.Skip ? 20 : (int)Face; } }
        public string Mark { get { return Face == Face.Skip ? "停" : Face == Face.Reverse ? "返" : Face == Face.DrawTwo ? "+2" : Face == Face.DrawFour ? "+4" : Face == Face.Wild ? "换" : ((int)Face).ToString(); } }
        public string Title { get { return Face == Face.Skip ? "老师点名" : Face == Face.Reverse ? "课代表返场" : Face == Face.DrawTwo ? "加两页作业" : Face == Face.DrawFour ? "突击小测" : Face == Face.Wild ? "自由调课" : Suit == Suit.Red ? "语文笔记" : Suit == Suit.Blue ? "数学演算" : Suit == Suit.Green ? "生物观察" : "英语单词"; } }
        public override string ToString() { return Suit + ":" + Face; }
    }

    public struct MoveEvent { public bool Draw, Reshuffle; public int Seat, Index, DrawCountAfter; public Card Card; public MoveEvent(bool draw, int seat, int index, Card card, int drawCountAfter=-1) { Draw = draw; Reshuffle=false; Seat = seat; Index = index; Card = card; DrawCountAfter=drawCountAfter; } }

    // Engine has no Unity dependency. A single clock drives turn and match deadlines.
    public sealed class UnoGame
    {
        readonly Random random;
        readonly List<Card> deck = new List<Card>();
        readonly List<Card> discards = new List<Card>();
        readonly List<Card>[] hands;
        public readonly Queue<MoveEvent> Moves = new Queue<MoveEvent>();
        bool initialized;
        public int DrawCount=>deck.Count;public int DiscardCount=>discards.Count;
        public int PlayerCount { get { return hands.Length; } }
        public int Direction { get; private set; } = 1;
        public IReadOnlyList<Card> Hand(int seat) { return hands[seat]; }
        int Next(int seat, int steps = 1) { return (seat + Direction * steps + hands.Length * 2) % hands.Length; }
        public IReadOnlyList<Card> Player { get { return hands[0]; } }
        public IReadOnlyList<Card> Opponent { get { return hands[1]; } }
        public Card Top { get { return discards[discards.Count - 1]; } }
        public Suit Color { get; private set; }
        public int Turn { get; private set; }
        public Outcome Result { get; private set; }
        public float Remaining { get; private set; }
        public bool Timed { get; private set; }
        public float Elapsed { get; private set; }
        public float TurnRemaining { get; private set; }
        public string Message { get; private set; }
        public int Revision { get; private set; }
        public bool NeedsUno { get { return hands[0].Count == 1 && unoWindow > 0; } }
        public bool UnoArmed { get; private set; }
        public int PendingDrawIndex { get; private set; } = -1;
        float unoWindow;
        public const float Duration = 60f;
        public const float TurnDuration = 5f;

        public UnoGame(int seed, int players = 2, bool timed = true)
        {
            if (players < 2 || players > 4) throw new ArgumentOutOfRangeException("players");
            hands = Enumerable.Range(0, players).Select(_ => new List<Card>()).ToArray();
            Timed = timed; random = new Random(seed);
            for (int c = 0; c < 4; c++)
            {
                deck.Add(new Card((Suit)c, Face.Zero));
                for (int n = 1; n <= 12; n++)
                    for (int k = 0; k < 2; k++) deck.Add(new Card((Suit)c, (Face)n));
            }
            for (int i = 0; i < 4; i++) { deck.Add(new Card(Suit.Wild, Face.Wild)); deck.Add(new Card(Suit.Wild, Face.DrawFour)); }
            Shuffle(deck);
            for (int i = 0; i < Tuning.Int("UNO","InitialCards"); i++) for (int seat = 0; seat < players; seat++) Draw(seat, 1);
            int first = deck.FindIndex(c => c.Face <= Face.Nine);
            discards.Add(deck[first]); deck.RemoveAt(first); Color = Top.Suit;
            Remaining = Timed ? Duration : float.PositiveInfinity; Elapsed = 0; TurnRemaining = TurnDuration; Message = "下课铃响了，你先出！"; initialized = true;
        }

        void Shuffle(List<Card> cards)
        {
            for (int i = cards.Count - 1; i > 0; i--) { int j = random.Next(i + 1); Card c = cards[i]; cards[i] = cards[j]; cards[j] = c; }
        }

        void Draw(int player, int count)
        {
            for (int i = 0; i < count; i++)
            {
                RefillDeck();if(deck.Count==0)break;
                int end = deck.Count - 1; Card drawn = deck[end]; hands[player].Add(drawn); deck.RemoveAt(end);
                if (initialized) Moves.Enqueue(new MoveEvent(true, player, hands[player].Count - 1, drawn, deck.Count));
                RefillDeck();
            }
            if (player == 0 && hands[0].Count != 1) { unoWindow = 0; UnoArmed = false; }
        }

        void RefillDeck()
        {
            if(deck.Count!=0||discards.Count<=1)return;Card top=Top;discards.RemoveAt(discards.Count-1);deck.AddRange(discards);discards.Clear();discards.Add(top);Shuffle(deck);
            if(initialized){var move=new MoveEvent(false,-1,-1,top,deck.Count);move.Reshuffle=true;Moves.Enqueue(move);}
        }

        public bool CanPlay(int player, int index)
        {
            if (player < 0 || player >= hands.Length || player != Turn || Result != Outcome.Playing || index < 0 || index >= hands[player].Count) return false;
            if(player==0 && PendingDrawIndex>=0 && index!=PendingDrawIndex)return false;
            Card card = hands[player][index];
            if (card.Face == Face.DrawFour) return !hands[player].Any(c => !c.IsWild && c.Suit == Color);
            return card.IsWild || card.Suit == Color || card.Face == Top.Face;
        }

        public bool Play(int player, int index, Suit chosenColor)
        {
            if (!CanPlay(player, index)) return false;
            Card card = hands[player][index];
            if (card.IsWild && (int)chosenColor > 3 || (int)chosenColor < 0) return false;
            PendingDrawIndex=-1;
            Moves.Enqueue(new MoveEvent(false, player, index, card));
            hands[player].RemoveAt(index); discards.Add(card); Color = card.IsWild ? chosenColor : card.Suit;
            Message = (player == 0 ? "你：" : "同学 " + player + "：") + card.Title + " " + card.Mark;
            if (card.Face == Face.Reverse) Direction = -Direction;
            bool skip = card.Face == Face.Skip || (card.Face == Face.Reverse && hands.Length == 2) || card.Face == Face.DrawTwo || card.Face == Face.DrawFour;
            if (card.Face == Face.DrawTwo || card.Face == Face.DrawFour) Draw(Next(player), card.Face == Face.DrawTwo ? 2 : 4);
            if (hands[player].Count == 0) { Result = player == 0 ? Outcome.Win : Outcome.Lose; Message = player == 0 ? "交卷！你先出完了。" : "同桌先交卷了！"; }
            if (player == 0)
            {
                unoWindow = hands[0].Count == 1 && !UnoArmed ? 2f : 0;
                UnoArmed = false;
            }
            Turn = Next(player, skip ? 2 : 1); TurnRemaining = TurnDuration; Revision++;
            return true;
        }

        public bool CallUno()
        {
            if (Result != Outcome.Playing || hands[0].Count < 1 || hands[0].Count > 2) return false;
            if (hands[0].Count == 2 && Turn != 0) return false;
            UnoArmed = hands[0].Count == 2; unoWindow = 0;
            Message = "报到！只剩最后一张。"; Revision++; return true;
        }

        public Suit BestColor(int player)
        {
            return Enumerable.Range(0, 4).OrderByDescending(c => hands[player].Count(card => (int)card.Suit == c)).Select(c => (Suit)c).First();
        }

        public bool DrawForPlayer()
        {
            if(Turn!=0 || Result!=Outcome.Playing || PendingDrawIndex>=0)return false;
            int before=hands[0].Count;Draw(0,1);
            if(hands[0].Count>before && CanPlay(0,hands[0].Count-1)){
                PendingDrawIndex=hands[0].Count-1;Message="摸到一张牌。";Revision++;return true;
            }
            AdvanceAfterDraw();return true;
        }
        public bool PassDrawn()
        {
            if(Turn!=0 || Result!=Outcome.Playing || PendingDrawIndex<0)return false;
            AdvanceAfterDraw();return true;
        }
        void AdvanceAfterDraw(){PendingDrawIndex=-1;Turn=Next(0);TurnRemaining=TurnDuration;UnoArmed=false;Revision++;Message="你选择过牌。";}

        public bool DrawAndPass(int player)
        {
            if (Turn != player || Result != Outcome.Playing || PendingDrawIndex>=0) return false;
            int before = hands[player].Count; Draw(player, 1);
            if (hands[player].Count > before && CanPlay(player, hands[player].Count - 1))
                return Play(player, hands[player].Count - 1, BestColor(player));
            Message = (player == 0 ? "你" : "同桌") + "摸了一张，轮到下一位。";
            Turn = Next(player); TurnRemaining = TurnDuration; UnoArmed = false; Revision++; return true;
        }

        public void AutoMove(int player)
        {
            if (Turn != player || Result != Outcome.Playing) return;
            int choice = -1, best = -1;
            for (int i = 0; i < hands[player].Count; i++)
            {
                if (!CanPlay(player, i)) continue;
                Card c = hands[player][i];
                int score = c.Points + (c.Face == Face.Skip || c.Face == Face.Reverse ? 10 : 0) + (hands[Next(player)].Count <= 2 && c.Face == Face.DrawTwo ? 30 : 0);
                // Keep wilds as an escape unless they are the only legal play.
                if (c.IsWild) score -= 55;
                if (choice < 0 || score > best) { choice = i; best = score; }
            }
            if (player == 0 && hands[0].Count == 2) CallUno();
            if (choice >= 0) Play(player, choice, BestColor(player)); else DrawAndPass(player);
        }

        public void Tick(float seconds, bool allowAuto = true)
        {
            if (Result != Outcome.Playing || seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            float step = Timed ? Math.Min(seconds, Remaining) : seconds; Elapsed += step; if(Timed) Remaining = Math.Max(0, Remaining - step);
            TurnRemaining = Math.Max(0, TurnRemaining - step);
            if (unoWindow > 0)
            {
                unoWindow -= step;
                if (unoWindow <= 0 && hands[0].Count == 1) { Draw(0, 2); Message = "忘了报到，补两张！"; Revision++; }
            }
            if (Remaining <= 0)
            {
                var bestHand = hands.Skip(1).OrderBy(h => h.Count).ThenBy(h => h.Sum(c => c.Points)).First();
                int compare = hands[0].Count.CompareTo(bestHand.Count);
                if (compare == 0) compare = hands[0].Sum(c => c.Points).CompareTo(bestHand.Sum(c => c.Points));
                Result = compare < 0 ? Outcome.Win : compare > 0 ? Outcome.Lose : Outcome.Draw;
                Message = "上课铃响！先比剩余张数，再比点数（越少越好）。"; Revision++; return;
            }
            if (!allowAuto) return;
            if (Turn != 0 && TurnRemaining <= TurnDuration - Tuning.Get("UNO","AiDelay")) AutoMove(Turn);
            else if (Timed && Turn == 0 && TurnRemaining <= 0) DrawAndPass(0);
        }

        public void Surrender() { if (Result == Outcome.Playing) { Result = Outcome.Lose; Message = "退出牌局，本局认输。"; Revision++; } }
        internal int TotalCards { get { return deck.Count + discards.Count + hands.Sum(h => h.Count); } }
        internal void Fixture(IEnumerable<Card> mine, IEnumerable<Card> theirs, Card top, Suit color)
        {
            hands[0].Clear(); hands[0].AddRange(mine); hands[1].Clear(); hands[1].AddRange(theirs);
            PendingDrawIndex=-1; Moves.Clear(); Direction = 1; discards.Clear(); discards.Add(top); Color = color; Turn = 0; Remaining = Timed ? Duration : float.PositiveInfinity; Elapsed = 0; TurnRemaining = TurnDuration; Result = Outcome.Playing; unoWindow = 0; UnoArmed = false;
        }
    }
}
