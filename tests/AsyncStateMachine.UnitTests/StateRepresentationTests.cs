using System.Threading.Tasks;
using Xunit;

namespace AsyncStateMachine.UnitTests
{
    public class StateRepresentationTests
    {
        private readonly StateRepresentation<Trigger, State> _sr;

        public enum State
        {
            A,
            B,
        };

        public enum Trigger
        {
            a,
            b,
        };

        /// <summary>
        /// Initializes a new instance of a <see cref="StateRepresentationTests" class.
        /// </summary>
        public StateRepresentationTests()
        {
            _sr = new StateRepresentation<Trigger, State>(State.A);
        }

        [Fact]
        public async Task CanFireAsync_InvalidTrigger_Throws()
        {
            // Act
            var exception = await Record.ExceptionAsync(
                () => _sr.CanFireAsync(Trigger.a));

            // Assert
            Assert.NotNull(exception);
        }

        [Fact]
        public async Task CanFireAsync_IgnoredTrigger_False()
        {
            // Arrange
            _sr.Ignore(Trigger.a);

            // Act
            var result = await _sr.CanFireAsync(Trigger.a);

            // Assert
            Assert.False(result.Item1);
        }

        [Fact]
        public async Task CanFireAsync_PermitedTrigger_True()
        {
            // Arrange
            _sr.Permit(Trigger.a, State.B);

            // Act
            var result = await _sr.CanFireAsync(Trigger.a);

            // Assert
            Assert.True(result.Item1);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public async Task CanFireAsync_PermitedIfTrigger_Works(bool contition, bool expected)
        {
            // Arrange
            _sr.PermitIf(Trigger.a, State.B, () => contition);

            // Act
            var result = await _sr.CanFireAsync(Trigger.a);

            // Assert
            Assert.Equal(expected, result.Item1);
        }

        [Theory]
        [InlineData(true, State.A)]
        [InlineData(false, State.B)]
        public async Task CanFireAsync_PermitedIfTrigger_CorrectState(bool condition, State expectedState)
        {
            // Arrange
            _sr.PermitIf(Trigger.a, State.A, () => condition);
            _sr.PermitIf(Trigger.a, State.B, () => !condition);

            // Act
            var result = await _sr.CanFireAsync(Trigger.a);

            // Assert
            Assert.True(result.Item1);
            Assert.Equal(expectedState, result.Item2.Value);
        }

        [Fact]
        public async Task CanFire_PermitedIfTriggerAllFalse_NoState()
        {
            // Arrange
            _sr.PermitIf(Trigger.a, State.A, () => false);
            _sr.PermitIf(Trigger.a, State.B, () => false);

            // Act
            var result = await _sr.CanFireAsync(Trigger.a);

            // Assert
            Assert.False(result.Item1);
            Assert.Null(result.Item2);
        }

        [Fact]
        public void Transitions_Initial_Empty()
        {
            // Act
            var transitions = _sr.Transitions;

            // Assert
            Assert.Empty(transitions);
        }

        [Fact]
        public void Transitions_Ignore_NotEmpty()
        {
            // Arrange
            _sr.Ignore(Trigger.a);

            // Act
            var transitions = _sr.Transitions;

            // Assert
            Assert.NotEmpty(transitions);
        }

        [Fact]
        public void Transitions_Permit_NotEmpty()
        {
            // Arrange
            _sr.Permit(Trigger.a, State.B);

            // Act
            var transitions = _sr.Transitions;

            // Assert
            Assert.NotEmpty(transitions);
        }

        [Fact]
        public void OnEntryCallbacks_Initial_Empty()
        {
            // Act
            var transitions = _sr.OnEntryCallbacks;

            // Assert
            Assert.Empty(transitions);
        }

        [Fact]
        public void OnExitCallbacks_Initial_Empty()
        {
            // Act
            var transitions = _sr.OnExitCallbacks;

            // Assert
            Assert.Empty(transitions);
        }

        [Fact]
        public void OnEntryCallbacks_OnEntry_NotEmpty()
        {
            // Arrange
            _sr.OnEntry(() => { });

            // Act
            var callbacks = _sr.OnEntryCallbacks;

            // Assert
            Assert.NotEmpty(callbacks);
        }

        [Fact]
        public void OnEntryCallbacks_OnEntryAsync_NotEmpty()
        {
            // Arrange
            _sr.OnEntry(() => Task.CompletedTask);

            // Act
            var callbacks = _sr.OnEntryCallbacks;

            // Assert
            Assert.NotEmpty(callbacks);
        }

        [Fact]
        public void OnExitCallbacks_OnExit_NotEmpty()
        {
            // Arrange
            _sr.OnExit(() => { });

            // Act
            var callbacks = _sr.OnExitCallbacks;

            // Assert
            Assert.NotEmpty(callbacks);
        }

        [Fact]
        public void OnExitCallbacks_OnExitAsync_NotEmpty()
        {
            // Arrange
            _sr.OnExit(() => Task.CompletedTask);

            // Act
            var callbacks = _sr.OnExitCallbacks;

            // Assert
            Assert.NotEmpty(callbacks);
        }

        [Fact]
        public void Permit_Permit_Throws()
        {
            // Arrange
            _sr.Permit(Trigger.a, State.B);

            // Act
            var exception = Record.Exception(() => _sr.Permit(Trigger.a, State.B));

            // Assert
            Assert.NotNull(exception);
        }

        [Fact]
        public void Permit_PermitIf_Throws()
        {
            // Arrange
            _sr.Permit(Trigger.a, State.B);

            // Act
            var exception = Record.Exception(() => _sr.PermitIf(Trigger.a, State.B, () => true));

            // Assert
            Assert.NotNull(exception);
        }

        [Fact]
        public void PermitIf_PermitIf_DoesNotThrow()
        {
            // Arrange
            _sr.PermitIf(Trigger.a, State.B, () => false);

            // Act
            var exception = Record.Exception(() => _sr.PermitIf(Trigger.a, State.A, () => true));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void DuplicatePermitIf_Throws()
        {
            // Arrange
            _sr.PermitIf(Trigger.a, State.B, () => false);

            // Act
            var exception = Record.Exception(() => _sr.PermitIf(Trigger.a, State.B, () => false));

            // Assert
            Assert.NotNull(exception);
        }
    }
}