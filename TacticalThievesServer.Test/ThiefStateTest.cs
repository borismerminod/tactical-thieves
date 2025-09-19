using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TacticalThievesServer.Services;

namespace TacticalThievesServer.Test
{
    public class ThiefStateTest
    {
        [Fact]
        public void InitialState_ShouldbeIdle()
        {
            ThiefStateService thiefState = new ThiefStateService();

            Assert.Equal(thiefState.CurrentState, ThiefStateService.eThiefState.Idle);
        }

        [Fact]
        public void Move_ShouldChangeStateToMove()
        {
            ThiefStateService thiefState = new ThiefStateService();
            thiefState.Move();
            Assert.Equal(thiefState.CurrentState, ThiefStateService.eThiefState.Move);
        }

        [Fact]
        public void Stealth_ShouldChangeStateToStealth()
        {
            ThiefStateService thiefState = new ThiefStateService();
            thiefState.Stealth();
            Assert.Equal(thiefState.CurrentState, ThiefStateService.eThiefState.Stealth);
        }
    }
}
