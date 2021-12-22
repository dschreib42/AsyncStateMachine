using System;
using System.Threading.Tasks;
using Xunit;

namespace AsyncStateMachine.ComponentTests
{
    public class StateMachineTests
    {
        private readonly StateMachine<Trigger, State> _sm;

        private enum State
        {
            A,
            B,
        }

        private enum Trigger
        {
            a,
            b,
        }

        /// <summary>
        /// Initializes a new instance of a <see cref="StateMachineTests"/> class.
        /// </summary>
        public StateMachineTests()
        {
            _sm = new StateMachine<Trigger, State>(State.A);
        }

        [Fact]
        public void Configure_Returns_NotNull()
        {
            // Act
            var result = _sm.Configure(State.A);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void Configure_DuplicatePermit_Throws()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(
                () => _sm.Configure(State.A).Permit(Trigger.a, State.B).Permit(Trigger.a, State.B));
        }

        [Fact]
        public void Configure_PermitAndPermitIf_Throws()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(
                () => _sm.Configure(State.A).Permit(Trigger.a, State.B).PermitIf(Trigger.a, State.B, () => true));
        }

        [Fact]
        public void Configure_DuplicatePermitIf_Throws()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(
                () => _sm.Configure(State.A).PermitIf(Trigger.a, State.B, () => false).PermitIf(Trigger.a, State.B, () => true));
        }

        [Fact]
        public Task FireAsync_InvalidTrigger_Throws()
        {
            // Arrange
            var result = _sm.Configure(State.A);

            // Act & Assert
            return Assert.ThrowsAsync<InvalidOperationException>(() => _sm.FireAsync(Trigger.a));
        }

        [Fact]
        public async Task FireAsync_Reentry_NoStateChange()
        {
            // Arrange
            _sm.Configure(State.A)
                .Permit(Trigger.a, State.A);

            // Act
            await _sm.FireAsync(Trigger.a);

            // Assert
            Assert.Equal(State.A, _sm.CurrentState);
        }

        [Fact]
        public async Task FireAsync_Ignore_NoStateChange()
        {
            // Arrange
            _sm.Configure(State.A)
                .Ignore(Trigger.a);

            // Act
            await _sm.FireAsync(Trigger.a);

            // Assert
            Assert.Equal(State.A, _sm.CurrentState);
        }

        [Fact]
        public async Task InitializeAsync_ValidState_CorrectState()
        {
            // Arrange
            _sm.Configure(State.A);
            _sm.Configure(State.B);

            // Act
            await _sm.InitializeAsync(State.B);

            // Assert
            Assert.Equal(State.B, _sm.CurrentState);
        }

        [Fact]
        public async Task CanFireAsync_InvalidTrigger_False()
        {
            // Arrange
            _sm.Configure(State.A)
                .Permit(Trigger.a, State.A);

            // Act
            var result = await _sm.CanFireAsync(Trigger.b);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CanFireAsync_ValidTrigger_True()
        {
            // Arrange
            _sm.Configure(State.A)
                .Permit(Trigger.a, State.A);

            // Act
            var result = await _sm.CanFireAsync(Trigger.a);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task FireAsync_Reentry_OnEntryCalled()
        {
            // Arrange
            var syncCalled = 0;
            var asyncCalled = 0;
            _sm.Configure(State.A)
                .Permit(Trigger.a, State.A)
                .OnEntry(() => syncCalled++)
                .OnEntry(() => { asyncCalled++; return Task.CompletedTask; });

            // Act
            await _sm.FireAsync(Trigger.a);

            // Assert
            Assert.Equal(1, syncCalled);
            Assert.Equal(1, asyncCalled);
        }

        [Fact]
        public async Task FireAsync_Reentry_OnExitCalled()
        {
            // Arrange
            var syncCalled = 0;
            var asyncCalled = 0;
            _sm.Configure(State.A)
                .Permit(Trigger.a, State.A)
                .OnExit(() => syncCalled++)
                .OnExit(() => { asyncCalled++; return Task.CompletedTask; });

            // Act
            await _sm.FireAsync(Trigger.a);

            // Assert
            Assert.Equal(1, syncCalled);
            Assert.Equal(1, asyncCalled);
        }

        [Fact]
        public async Task FireAsync_Ignore_OnEntryNotCalled()
        {
            // Arrange
            var syncCalled = 0;
            var asyncCalled = 0;
            _sm.Configure(State.A)
                .Ignore(Trigger.a)
                .OnEntry(() => syncCalled++)
                .OnEntry(() => { asyncCalled++; return Task.CompletedTask; });

            // Act
            await _sm.FireAsync(Trigger.a);

            // Assert
            Assert.Equal(0, syncCalled);
            Assert.Equal(0, asyncCalled);
        }

        [Fact]
        public async Task FireAsync_Igored_OnExitNotCalled()
        {
            // Arrange
            var syncCalled = 0;
            var asyncCalled = 0;
            _sm.Configure(State.A)
                .Ignore(Trigger.a)
                .OnExit(() => syncCalled++)
                .OnExit(() => { syncCalled++; return Task.CompletedTask; });

            // Act
            await _sm.FireAsync(Trigger.a);

            // Assert
            Assert.Equal(0, syncCalled);
            Assert.Equal(0, asyncCalled);
        }

        [Fact]
        public async Task FireAsync_Permit_OnExitCalled()
        {
            // Arrange
            var syncCalled = 0;
            var asyncCalled = 0;
            _sm.Configure(State.A)
                .Permit(Trigger.a, State.B)
                .OnExit(() => syncCalled++)
                .OnExit(() => { asyncCalled++; return Task.CompletedTask; });
            _sm.Configure(State.B);

            // Act
            await _sm.FireAsync(Trigger.a);

            // Assert
            Assert.Equal(1, syncCalled);
            Assert.Equal(1, asyncCalled);
            Assert.Equal(State.B, _sm.CurrentState);
        }

        [Fact]
        public async Task FireAsync_Permit_OnEntryCalled()
        {
            // Arrange
            var syncCalled = 0;
            var asyncCalled = 0;
            _sm.Configure(State.A)
                .Permit(Trigger.a, State.B);
            _sm.Configure(State.B)
                .OnEntry(() => syncCalled++)
                .OnEntry(() => { asyncCalled++; return Task.CompletedTask; });

            // Act
            await _sm.FireAsync(Trigger.a);

            // Assert
            Assert.Equal(1, syncCalled);
            Assert.Equal(1, asyncCalled);
            Assert.Equal(State.B, _sm.CurrentState);
        }

        [Fact]
        public async Task FireAsync_PermitIfTrue_OnEntryCalled()
        {
            // Arrange
            var syncCalled = 0;
            var asyncCalled = 0;
            _sm.Configure(State.A)
                .PermitIf(Trigger.a, State.B, () => true);
            _sm.Configure(State.B)
                .OnEntry(() => syncCalled++)
                .OnEntry(() => { asyncCalled++; return Task.CompletedTask; });

            // Act
            await _sm.FireAsync(Trigger.a);

            // Assert
            Assert.Equal(1, syncCalled);
            Assert.Equal(1, asyncCalled);
            Assert.Equal(State.B, _sm.CurrentState);
        }

        [Fact]
        public async Task FireAsync_PermitIfFalse_Throws()
        {
            // Arrange
            _sm.Configure(State.A)
                .PermitIf(Trigger.a, State.B, () => false);
            _sm.Configure(State.B);

            // Act
            var exception = await Record.ExceptionAsync(() => _sm.FireAsync(Trigger.a));

            // Assert
            Assert.NotNull(exception);
        }

        [Fact]
        public async Task FireAsync_PermitWithStringArg_SyncOnEntryCalled()
        {
            // Arrange
            var sent = Guid.NewGuid().ToString();
            var received = string.Empty;
            _sm.Configure(State.A)
                .Permit(Trigger.a, State.B);
            _sm.Configure(State.B)
                .OnEntry<string>(str => received = str);

            // Act
            await _sm.FireAsync(Trigger.a, sent);

            // Assert
            Assert.Equal(sent, received);
            Assert.Equal(State.B, _sm.CurrentState);
        }

        [Fact]
        public async Task FireAsync_PermitWithStringArg_AsyncOnEntryCalled()
        {
            // Arrange
            var sent = Guid.NewGuid().ToString();
            var received = string.Empty;
            _sm.Configure(State.A)
                .Permit(Trigger.a, State.B);
            _sm.Configure(State.B)
                .OnEntry<string>(str => { received = str; return Task.CompletedTask; });

            // Act
            await _sm.FireAsync(Trigger.a, sent);

            // Assert
            Assert.Equal(sent, received);
            Assert.Equal(State.B, _sm.CurrentState);
        }

        [Fact]
        public async Task FireAsync_PermitWithIntArg_SyncOnEntryCalled()
        {
            // Arrange
            const int sent = 42;
            var received = 0;
            _sm.Configure(State.A)
                .Permit(Trigger.a, State.B);
            _sm.Configure(State.B)
                .OnEntry<int>(number => received = number);

            // Act
            await _sm.FireAsync(Trigger.a, sent);

            // Assert
            Assert.Equal(sent, received);
            Assert.Equal(State.B, _sm.CurrentState);
        }

        [Fact]
        public async Task FireAsync_PermitWithIntArg_AsyncOnEntryCalled()
        {
            // Arrange
            const int sent = 42;
            var received = 0;
            _sm.Configure(State.A)
                .Permit(Trigger.a, State.B);
            _sm.Configure(State.B)
                .OnEntry<int>(number => { received = number; return Task.CompletedTask; });

            // Act
            await _sm.FireAsync(Trigger.a, sent);

            // Assert
            Assert.Equal(sent, received);
            Assert.Equal(State.B, _sm.CurrentState);
        }

        [Fact]
        public async Task FireAsync_PermitWithWrongTypeArgSync_Throws()
        {
            // Arrange
            _sm.Configure(State.A)
                .Permit(Trigger.a, State.B);
            _sm.Configure(State.B)
                .OnEntry<int>(number => { });

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _sm.FireAsync(Trigger.a, "wrong type"));
        }

        [Fact]
        public async Task FireAsync_PermitWithWrongTypeArgAsync_Throws()
        {
            // Arrange
            _sm.Configure(State.A)
                .Permit(Trigger.a, State.B);
            _sm.Configure(State.B)
                .OnEntry<int>(number => Task.CompletedTask);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _sm.FireAsync(Trigger.a, "wrong type"));
        }

        [Fact]
        public async Task FireAsync_PermitWithNoHandler_Throws()
        {
            // Arrange
            _sm.Configure(State.A)
                .Permit(Trigger.a, State.B);
            _sm.Configure(State.B);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _sm.FireAsync(Trigger.a, "no handler"));
        }
    }
}