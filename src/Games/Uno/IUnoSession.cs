namespace StudentAge.CampusUno
{
    public interface IPracticeSession {}
    // A gameplay view never owns UP charges, NPC progress or rewards.
    public interface IUnoSession
    {
        bool IsExternal { get; }
        float Cost { get; }
        bool Begin();
        void Finish(Outcome outcome);
        void Cancel();
    }
}
