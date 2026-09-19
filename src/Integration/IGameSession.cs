using System;using StudentAge.CampusUno;
namespace StudentAge.CampusMinigames{
public interface IGameSession:IUnoSession,ICardSeatSession {string Name{get;}string Root{get;}bool IsActive{get;}void OnInvalidated(Action callback);}
}
