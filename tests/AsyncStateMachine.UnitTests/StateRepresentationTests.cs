using System.Threading.Tasks;
using Xunit;

namespace AsyncStateMachine.UnitTests
{
    public sealed class StateConfigurationTests
    {
        private readonly StateConfiguration<Trigger, State> _sr;

        public enum State
        {
            A,
            B,
        };

        private enum Trigger
        {
            a,
            b,
        };

        /// <summary>
        /// Initializes a new instance of a <see cref="StateConfigurationTests"/> class.
        /// </summary>
        public StateConfigurationTests()
        {
            _sr = new StateConfiguration<Trigger, State>(State.A);
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
        public async Task CanFireAsync_PermittedTrigger_True()
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
        public async Task CanFireAsync_PermittedIfTrigger_Works(bool condition, bool expected)
        {
            // Arrange
            _sr.PermitIf(Trigger.a, State.B, () => condition);

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
            var (canBeFired, newState) = await _sr.CanFireAsync(Trigger.a);

            // Assert
            Assert.True(canBeFired);
            Assert.Equal(expectedState, newState.Value);
        }

        [Fact]
        public async Task CanFire_PermittedIfTriggerAllFalse_NoState()
        {
            // Arrange
            _sr.PermitIf(Trigger.a, State.A, () => false);
            _sr.PermitIf(Trigger.a, State.B, () => false);

            // Act
            var (canBeFired, newState) = await _sr.CanFireAsync(Trigger.a);

            // Assert
            Assert.False(canBeFired);
            Assert.Null(newState);
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
        public void Permit_SameState_Throws()
        {
            // Act
            var exception = Record.Exception(() => _sr.Permit(Trigger.a, State.A));

            // Assert
            Assert.NotNull(exception);
        }

        [Fact]
        public void PermitReentry_PermitReentry_Throws()
        {
            // Arrange
            _sr.PermitReentry(Trigger.a);

            // Act
            var exception = Record.Exception(() => _sr.PermitReentry(Trigger.a));

            // Assert
            Assert.NotNull(exception);
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

        [Fact]
        public void ParentState_SubstateOf_True()
        {
            // Arrange
            _sr.SubstateOf(State.B);

            // Act & Assert
            Assert.Equal(State.B, _sr.ParentState);
        }

        [Fact]
        public void ParentState_NoSubstateOf_Null()
        {
            // Act & Assert
            Assert.Null(_sr.ParentState);
        }
    }
}