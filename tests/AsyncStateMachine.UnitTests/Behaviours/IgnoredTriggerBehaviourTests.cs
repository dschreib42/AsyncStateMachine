using AsyncStateMachine.Behaviours;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AsyncStateMachine.UnitTests.Behaviours
{
    public class IgnoredTriggerBehaviourTests
    {
        private readonly IgnoredTriggerBehaviour<Trigger, State> _behaviour;

        private enum State
        {
            A,
            B
        }

        private enum Trigger
        {
            a,
            b
        }

        /// <summary>
        /// Initializes a new instance of a <see cref="PermitTriggerBehaviourTests"/> class.
        /// </summary>
        public IgnoredTriggerBehaviourTests()
        {
            _behaviour = new IgnoredTriggerBehaviour<Trigger, State>(State.A, Trigger.b, State.A);
        }

        [Fact]
        public void Constructor_SourceDiffersFromTarget_Throws()
        {
            // Act & Assert
            Assert.ThrowsAny<ArgumentException>(
                () => new IgnoredTriggerBehaviour<Trigger, State>(State.A, Trigger.b, State.B));
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