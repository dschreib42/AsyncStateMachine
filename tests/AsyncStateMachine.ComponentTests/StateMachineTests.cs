using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace AsyncStateMachine.ComponentTests
{
    public sealed class StateMachineTests : IDisposable
    {
        private readonly StateMachineConfiguration<Trigger, State> _config;
        private readonly StateMachine<Trigger, State> _sm;

        private enum State
        {
            A,
            B,
            C,
        }

        private enum Trigger
        {
            a,
            b,
            c,
        }

        /// <summary>
        /// Initializes a new instance of a <see cref="StateMachineTests"/> class.
        /// </summary>
        public StateMachineTests()
        {
            _config = new StateMachineConfiguration<Trigger, State>(State.A);
            _sm = new StateMachine<Trigger, State>(_config);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _sm.Dispose();
        }

        [Theory]
        [InlineData("A")]
        [InlineData("B")]
        [InlineData("C")]
        [InlineData(null)]
        public async Task InitializeAsync_ReportedTransition_AsDefined(string state)
        {
            // Arrange
            Transition<Trigger, State> transition = null;
            _config.Configure(State.A);
            _config.Configure(State.B);
            _config.Configure(State.C);
            using var _ = _sm.Observable.Subscribe(t => transition = t);

            State expectedState = state switch
            {
                "A" => State.A,
                "B" => State.B,
                "C" => State.C,
                _ => _config.InitialState,
            };

            // Act
            await _sm.InitializeAsync(expectedState);

            // Assert
            Assert.Equal(expectedState, transition.Destination);
        }

        [Fact]
        public async Task InitializeAsync_ReportedTransition_DefaultState()
        {
            // Arrange
            Transition<Trigger, State> transition = null;
            _config.Configure(State.A);
            _config.Configure(State.B);
            _config.Configure(State.C);
            using var _ = _sm.Observable.Subscribe(t => transition = t);

            // Act
            await _sm.InitializeAsync();

            // Assert
            Assert.Equal(_config.InitialState, transition.Destination);
        }

        [Fact]
        public async Task FireAsync_InvalidTrigger_Throws()
        {
            // Arrange
            var result = _config.Configure(State.A);
            await _sm.InitializeAsync(State.A);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _sm.FireAsync(Trigger.a));
        }

        [Fact]
        public async Task FireAsync_Reentry_NoStateChange()
        {
            // Arrange
            _config.Configure(State.A)
                .PermitReentry(Trigger.a);
            await _sm.InitializeAsync(State.A);

            // Act
            await _sm.FireAsync(Trigger.a);

            // Assert
            Assert.Equal(State.A, _sm.CurrentState);
        }

        [Fact]
        public async Task FireAsync_Ignore_NoStateChange()
        {
            // Arrange
            _config.Configure(State.A)
                .Ignore(Trigger.a);
            await _sm.InitializeAsync(State.A);

            // Act
            await _sm.FireAsync(Trigger.a);

            // Assert
            Assert.Equal(State.A, _sm.CurrentState);
        }

        [Fact]
        public async Task CanFireAsync_InvalidTrigger_False()
        {
            // Arrange
            _config.Configure(State.A)
                .PermitReentry(Trigger.a);
            await _sm.InitializeAsync();

            // Act
            var result = await _sm.CanFireAsync(Trigger.b);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CanFireAsync_ValidTrigger_True()
        {
            // Arrange
            _config.Configure(State.A)
                .PermitReentry(Trigger.a);
            await _sm.InitializeAsync();

            // Act
            var result = await _sm.CanFireAsync(Trigger.a);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task FireAsync_Reentry_OnEntryCalled()
        {
            // Arrange
            var syncCalled = -1;
            var asyncCalled = -1;
            _config.Configure(State.A)
                .PermitReentry(Trigger.a)
                .OnEntry(() => syncCalled++)
                .OnEntry(() => { asyncCalled++; return Task.CompletedTask; });
            await _sm.InitializeAsync();

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
            _config.Configure(State.A)
                .PermitReentry(Trigger.a)
                .OnExit(() => syncCalled++)
                .OnExit(() => { asyncCalled++; return Task.CompletedTask; });
            await _sm.InitializeAsync();

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
            var syncCalled = -1;
            var asyncCalled = -1;
            _config.Configure(State.A)
                .Ignore(Trigger.a)
                .OnEntry(() => syncCalled++)
                .OnEntry(() => { asyncCalled++; return Task.CompletedTask; });
            await _sm.InitializeAsync();

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
            _config.Configure(State.A)
                .Ignore(Trigger.a)
                .OnExit(() => syncCalled++)
                .OnExit(() => { syncCalled++; return Task.CompletedTask; });
            await _sm.InitializeAsync();

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
            _config.Configure(State.A)
                .Permit(Trigger.a, State.B)
                .OnExit(() => syncCalled++)
                .OnExit(() => { asyncCalled++; return Task.CompletedTask; });
            _config.Configure(State.B);
            await _sm.InitializeAsync();

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
            _config.Configure(State.A)
                .Permit(Trigger.a, State.B);
            _config.Configure(State.B)
                .OnEntry(() => syncCalled++)
                .OnEntry(() => { asyncCalled++; return Task.CompletedTask; });
            await _sm.InitializeAsync();

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
            _config.Configure(State.A)
                .PermitIf(Trigger.a, State.B, () => true);
            _config.Configure(State.B)
                .OnEntry(() => syncCalled++)
                .OnEntry(() => { asyncCalled++; return Task.CompletedTask; });
            await _sm.InitializeAsync();

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
            _config.Configure(State.A)
                .PermitIf(Trigger.a, State.B, () => false);
            _config.Configure(State.B);
            await _sm.InitializeAsync();

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
            _config.Configure(State.A)
                .Permit(Trigger.a, State.B);
            _config.Configure(State.B)
                .OnEntry<string>(str => received = str);
            await _sm.InitializeAsync();

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
            _config.Configure(State.A)
                .Permit(Trigger.a, State.B);
            _config.Configure(State.B)
                .OnEntry<string>(str => { received = str; return Task.CompletedTask; });
            await _sm.InitializeAsync();

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
            _config.Configure(State.A)
                .Permit(Trigger.a, State.B);
            _config.Configure(State.B)
                .OnEntry<int>(number => received = number);
            await _sm.InitializeAsync();

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
            _config.Configure(State.A)
                .Permit(Trigger.a, State.B);
            _config.Configure(State.B)
                .OnEntry<int>(number => { received = number; return Task.CompletedTask; });
            await _sm.InitializeAsync();

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
            _config.Configure(State.A)
                .Permit(Trigger.a, State.B);
            _config.Configure(State.B)
                .OnEntry<int>(number => { });
            await _sm.InitializeAsync();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _sm.FireAsync(Trigger.a, "wrong type"));
        }

        [Fact]
        public async Task FireAsync_PermitWithWrongTypeArgAsync_Throws()
        {
            // Arrange
            _config.Configure(State.A)
                .Permit(Trigger.a, State.B);
            _config.Configure(State.B)
                .OnEntry<int>(number => Task.CompletedTask);
            await _sm.InitializeAsync();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _sm.FireAsync(Trigger.a, "wrong type"));
        }

        [Fact]
        public async Task FireAsync_PermitWithNoHandler_Throws()
        {
            // Arrange
            _config.Configure(State.A)
                .Permit(Trigger.a, State.B);
            _config.Configure(State.B);
            await _sm.InitializeAsync();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _sm.FireAsync(Trigger.a, "no handler"));
        }

        [Fact]
        public async Task FireAsync_OnEntry_FireAgain()
        {
            // Arrange
            _config.Configure(State.A)
                .Permit(Trigger.b, State.B);
            _config.Configure(State.B)
                .Permit(Trigger.a, State.A)
                .OnEntry(() => _sm.FireAsync(Trigger.a));
            await _sm.InitializeAsync();

            // Act
            await _sm.FireAsync(Trigger.b);

            // Assert
            Assert.Equal(State.A, _sm.CurrentState);
        }

        [Fact]
        public async Task FireAsync_OnEntryThrows_CallerCatchesException()
        {
            // Arrange
            _config.Configure(State.A)
                .Permit(Trigger.a, State.B);
            _config.Configure(State.B)
                .OnEntry(() => throw new TimeoutException());
            await _sm.InitializeAsync();

            // Act & Assert
            await Assert.ThrowsAsync<TimeoutException>(() => _sm.FireAsync(Trigger.a));
        }

        [Fact]
        public async Task FireAsync_OnExitThrows_CallerCatchesException()
        {
            // Arrange
            _config.Configure(State.A)
                .Permit(Trigger.a, State.B)
                .OnExit(() => throw new TimeoutException());
            _config.Configure(State.B);
            await _sm.InitializeAsync();

            // Act & Assert
            await Assert.ThrowsAsync<TimeoutException>(() => _sm.FireAsync(Trigger.a));
        }

        [Fact]
        public async Task FireAsync_RecursiveFire_TransitionsInCorrectOrder()
        {
            // Arrange
            var transitions = new List<string>();
            using var subscription = _sm.Observable.Subscribe(t => transitions.Add($"{t.Source}{t.Trigger}{t.Destination}"));
            _config.Configure(State.A)
                .Permit(Trigger.b, State.B)
                .Permit(Trigger.c, State.C);
            _config.Configure(State.B)
                .Permit(Trigger.c, State.C)
                .OnEntry(() => _sm.FireAsync(Trigger.c));
            _config.Configure(State.C);
            await _sm.InitializeAsync();

            // Act
            await _sm.FireAsync(Trigger.b);

            // Assert
            Assert.Equal(new[] { "A", "AbB", "BcC" }, transitions);
        }

        [Fact]
        public async Task FireAsync_RecursiveFire_NodesVisitedInCorrectOrder()
        {
            // Arrange
            var visited = new List<string>();
            _config.Configure(State.A)
                .Permit(Trigger.b, State.B)
                .Permit(Trigger.c, State.C)
                .OnEntry(() => visited.Add("<A"))
                .OnExit(() => visited.Add("A>"));
            _config.Configure(State.B)
                .Permit(Trigger.c, State.C)
                .OnEntry(() => visited.Add("<B"))
                .OnEntry(() => _sm.FireAsync(Trigger.c))
                .OnExit(() => visited.Add("B>"));
            _config.Configure(State.C)
                .OnEntry(() => visited.Add("<C"));
            await _sm.InitializeAsync();

            // Act
            await _sm.FireAsync(Trigger.b);

            // Assert
            Assert.Equal(new[] { "<A", "A>", "<B", "B>", "<C" }, visited);
        }

        [Fact]
        public async Task ResetAsync_OnExit_Called()
        {
            // Arrange
            var calls = 0;
            _config.Configure(State.A)
                .Permit(Trigger.b, State.B);
            _config.Configure(State.B)
                .OnExit(() => calls++);
            await _sm.InitializeAsync();
            await _sm.FireAsync(Trigger.b);

            // Act
            await _sm.ResetAsync();

            // Assert
            Assert.Equal(1, calls);
        }
    }
}