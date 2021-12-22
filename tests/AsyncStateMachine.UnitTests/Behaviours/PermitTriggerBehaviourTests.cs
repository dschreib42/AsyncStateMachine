using AsyncStateMachine.Behaviours;
using System.Threading.Tasks;
using Xunit;

namespace AsyncStateMachine.UnitTests.Behaviours
{
    public class PermitTriggerBehaviourTests
    {
        private readonly PermitTriggerBehaviour<Trigger, State> _behaviour;

        public enum State
        {
            A,
            B
        }

        public enum Trigger
        {
            a,
            b
        }

        /// <summary>
        /// Initializes a new instance of a <see cref="PermitTriggerBehaviourTests"/> class.
        /// </summary>
        public PermitTriggerBehaviourTests()
        {
            _behaviour = new PermitTriggerBehaviour<Trigger, State>(State.A, Trigger.b, State.B);
        }

        [Fact]
        public async Task Condition_AlwaysValid_True()
        {
            // Act
            var result = await _behaviour.Condition();

            // Assert
            Assert.True(result);
        }
    }
}