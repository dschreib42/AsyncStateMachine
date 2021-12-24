using AsyncStateMachine.Behaviours;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AsyncStateMachine.UnitTests.Behaviours
{
    public class PermitIfTriggerBehaviourTests
    {
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

        [Fact]
        public void Constructor_NullSyncCondition_Throws()
        {
            // Arrange
            Func<bool> condition = null;

            // Act && Assert
            Assert.ThrowsAny<ArgumentNullException>(() => new PermitIfTriggerBehaviour<Trigger, State>(State.A, Trigger.a, State.B, condition));
        }

        [Fact]
        public void Constructor_NullAsyncCondition_Throws()
        {
            // Arrange
            Func<Task<bool>> condition = null;

            // Act && Assert
            Assert.ThrowsAny<ArgumentNullException>(() => new PermitIfTriggerBehaviour<Trigger, State>(State.A, Trigger.a, State.B, condition));
        }

        [Fact]
        public async Task Condition_NotValid_False()
        {
            // Arrange
            var behaviour = new PermitIfTriggerBehaviour<Trigger, State>(
                State.A, Trigger.b, State.B,
                () => false);

            // Act
            var result = await behaviour.Condition();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task Condition_Valid_True()
        {
            // Arrange
            var behaviour = new PermitIfTriggerBehaviour<Trigger, State>(
                State.A, Trigger.b, State.B,
                () => Task.FromResult(true));

            // Act
            var result = await behaviour.Condition();

            // Assert
            Assert.True(result);
        }
    }
}