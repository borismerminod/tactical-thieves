namespace TacticalThievesServer.Services
{
    public class ThiefStateService
    {
        public enum eThiefState
        {
            Idle = 0,
            Move = 1,
            Stealth = 2
        }

        private eThiefState currentState;
        public eThiefState CurrentState { get => currentState; set => currentState = value; }

        public ThiefStateService()
        {
            Idle();
        }

        public void Idle()
        {
            CurrentState = eThiefState.Idle;
        }

        public void Move()
        {
            CurrentState = eThiefState.Move;
        }

        public void Stealth()
        {
            CurrentState = eThiefState.Stealth;
        }

    }
}
