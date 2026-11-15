using AsyncStateMachine.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AsyncStateMachine.ComponentTests
{
    /// <summary>
    /// Tests for cancellation, reset, initialization errors, hierarchy depth and concurrency.
    /// </summary>
    public sealed class StateMachineEdgeCaseTests : IDisposable
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

        private readonly StateMachineConfiguration<Trigger, State> _config;
        private readonly StateMachine<Trigger, State> _sm;

        public enum State
        {
            A,
            B,
            C,
        }

        public enum Trigger
        {
            a,
            b,
        }

        public StateMachineEdgeCaseTests()
        {
            _config = new StateMachineConfiguration<Trigger, State>(State.A);
            _sm = new StateMachine<Trigger, State>(_config);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _sm.Dispose();
        }

        #region Cancellation

        public static IEnumerable<object[]> AllOperations()
            => new[]
            {
                "InitializeAsync",
                "InitializeAsync(state)",
                "ResetAsync",
                "InStateAsync",
                "InStateAsync(maxDepth)",
                "CanFireAsync",
                "FireAsync",
                "FireAsync(param)",
            }.Select(x => new object[] { x });

        private static Task Invoke(IStateMachine<Trigger, State> sm, string operation, CancellationToken token)
            => operation switch
            {
                "InitializeAsync" => sm.InitializeAsync(token),
                "InitializeAsync(state)" => sm.InitializeAsync(State.B, token),
                "ResetAsync" => sm.ResetAsync(token),
                "InStateAsync" => sm.InStateAsync(State.A, token),
                "InStateAsync(maxDepth)" => sm.InStateAsync(State.A, 1, token),
                "CanFireAsync" => sm.CanFireAsync(Trigger.a, token),
                "FireAsync" => sm.FireAsync(Trigger.a, token),
                "FireAsync(param)" => sm.FireAsync(Trigger.b, "foo", token),
                _ => throw new ArgumentOutOfRangeException(nameof(operation)),
            };

        private IStateConfiguration<Trigger, State> ConfigureForAllOperations(Action onExitA = null)
        {
            _config.Configure(State.A)
                .Permit(Trigger.a, State.B)
                .Permit(Trigger.b, State.C)
                .OnExit(() => onExitA?.Invoke());
            var b = _config.Configure(State.B);
            _config.Configure(State.C)
                .OnEntry<string>(_ => { });
            return b;
        }

        [Theory]
        [MemberData(nameof(AllOperations))]
        public async Task Operation_CancelledToken_ThrowsAndStateUnchanged(string operation)
        {
            // Arrange
            var exitCalls = 0;
            ConfigureForAllOperations(() => exitCalls++);
            await _sm.InitializeAsync();
            var transitions = new List<Transition<Trigger, State>>();
            using var _ = _sm.Observable.Subscribe(transitions.Add);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Invoke(_sm, operation, cts.Token));
            Assert.Equal(State.A, _sm.CurrentState);
            Assert.Empty(transitions);
            Assert.Equal(0, exitCalls);
        }

        [Theory]
        [MemberData(nameof(AllOperations))]
        public async Task Operation_CancelledWhileWaitingForLock_Throws(string operation)
        {
            // Arrange
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            ConfigureForAllOperations()
                .Permit(Trigger.a, State.A)
                .OnEntry(async () =>
                {
                    entered.TrySetResult();
                    await release.Task;
                });
            await _sm.InitializeAsync();

            // occupy the lock: transition A -> B blocks inside B's entry action
            var blocking = _sm.FireAsync(Trigger.a);
            await entered.Task.WaitAsync(Timeout);

            using var cts = new CancellationTokenSource();
            var waiting = Invoke(_sm, operation, cts.Token);

            // Act
            cts.Cancel();

            // Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting.WaitAsync(Timeout));

            release.SetResult();
            await blocking.WaitAsync(Timeout);
            Assert.Equal(State.B, _sm.CurrentState);
        }

        [Fact]
        public async Task CanFireAsync_CancelledToken_DoesNotReturnFalse()
        {
            // Arrange
            _config.Configure(State.A)
                .Permit(Trigger.a, State.B);
            _config.Configure(State.B);
            await _sm.InitializeAsync();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            var exception = await Record.ExceptionAsync(() => _sm.CanFireAsync(Trigger.a, cts.Token));

            // Assert
            Assert.IsAssignableFrom<OperationCanceledException>(exception);
        }

        [Fact]
        public async Task Operation_NotCancelledToken_Works()
        {
            // Arrange
            _config.Configure(State.A)
                .Permit(Trigger.a, State.B);
            _config.Configure(State.B);
            using var cts = new CancellationTokenSource();

            // Act
            await _sm.InitializeAsync(cts.Token);
            var canFire = await _sm.CanFireAsync(Trigger.a, cts.Token);
            await _sm.FireAsync(Trigger.a, cts.Token);
            var inB = await _sm.InStateAsync(State.B, cts.Token);
            await _sm.ResetAsync(cts.Token);

            // Assert
            Assert.True(canFire);
            Assert.True(inB);
            Assert.Equal(State.A, _sm.CurrentState);
        }

        #endregion

        #region ResetAsync

        [Fact]
        public async Task ResetAsync_Cancelled_ExitNotCalledAndStateKept()
        {
            // Arrange
            var exitCalls = 0;
            _config.Configure(State.A)
                .Permit(Trigger.a, State.B);
            _config.Configure(State.B)
                .OnExit(() => exitCalls++);
            await _sm.InitializeAsync();
            await _sm.FireAsync(Trigger.a);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _sm.ResetAsync(cts.Token));

            // Assert
            Assert.Equal(0, exitCalls);
            Assert.Equal(State.B, _sm.CurrentState);

            // a later transition must not run B's exit action twice
            await _sm.ResetAsync();
            Assert.Equal(1, exitCalls);
        }

        [Fact]
        public async Task ResetAsync_CancelledDuringExit_CompletesReset()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            var visited = new List<string>();
            _config.Configure(State.A)
                .Permit(Trigger.a, State.B)
                .OnEntry(() => visited.Add("<A"));
            _config.Configure(State.B)
                .OnExit(() =>
                {
                    visited.Add("B>");
                    cts.Cancel();
                });
            await _sm.InitializeAsync();
            await _sm.FireAsync(Trigger.a);
            visited.Clear();

            // Act: once the exit actions ran, the reset must not be abandoned half-way
            await _sm.ResetAsync(cts.Token);

            // Assert
            Assert.Equal(State.A, _sm.CurrentState);
            Assert.Equal(new[] { "B>", "<A" }, visited);
        }

        [Fact]
        public async Task ResetAsync_Uninitialized_EntersInitialState()
        {
            // Arrange
            var visited = new List<string>();
            _config.Configure(State.A)
                .OnEntry(() => visited.Add("<A"))
                .OnExit(() => visited.Add("A>"));

            // Act
            await _sm.ResetAsync();

            // Assert
            Assert.Equal(State.A, _sm.CurrentState);
            Assert.Equal(new[] { "<A" }, visited);
        }

        [Fact]
        public async Task ResetAsync_InInitialState_ExitThenEntry()
        {
            // Arrange
            var visited = new List<string>();
            _config.Configure(State.A)
                .OnEntry(() => visited.Add("<A"))
                .OnExit(() => visited.Add("A>"));
            await _sm.InitializeAsync();

            // Act
            await _sm.ResetAsync();

            // Assert
            Assert.Equal(new[] { "<A", "A>", "<A" }, visited);
        }

        [Fact]
        public async Task ResetAsync_FromOtherState_ExitThenInitialEntry()
        {
            // Arrange
            var visited = new List<string>();
            _config.Configure(State.A)
                .Permit(Trigger.a, State.B)
                .OnEntry(() => visited.Add("<A"));
            _config.Configure(State.B)
                .OnExit(() => visited.Add("B>"));
            await _sm.InitializeAsync();
            await _sm.FireAsync(Trigger.a);
            visited.Clear();

            // Act
            await _sm.ResetAsync();

            // Assert
            Assert.Equal(new[] { "B>", "<A" }, visited);
        }

        [Fact]
        public async Task ResetAsync_PublishesInitialTransition()
        {
            // Arrange
            _config.Configure(State.A)
                .Permit(Trigger.a, State.B);
            _config.Configure(State.B);
            await _sm.InitializeAsync();
            await _sm.FireAsync(Trigger.a);
            var transitions = new List<Transition<Trigger, State>>();
            using var _ = _sm.Observable.Subscribe(transitions.Add);

            // Act
            await _sm.ResetAsync();

            // Assert
            var transition = Assert.Single(transitions);
            Assert.Null(transition.Source);
            Assert.Null(transition.Trigger);
            Assert.Equal(State.A, transition.Destination);
        }

        [Fact]
        public async Task ResetAsync_ConcurrentFire_RunsAfterReset()
        {
            // Arrange
            var visited = new List<string>();
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _config.Configure(State.A)
                .Permit(Trigger.a, State.B)
                .OnEntry(() => visited.Add("<A"))
                .OnExit(() => visited.Add("A>"));
            _config.Configure(State.B)
                .Permit(Trigger.b, State.A)
                .OnExit(async () =>
                {
                    visited.Add("B>");
                    entered.TrySetResult();
                    await release.Task;
                })
                .OnEntry(() => visited.Add("<B"));
            await _sm.InitializeAsync();
            await _sm.FireAsync(Trigger.a);
            visited.Clear();

            // reset blocks inside B's exit action
            var reset = _sm.ResetAsync();
            await entered.Task.WaitAsync(Timeout);

            // Act
            var fire = _sm.FireAsync(Trigger.a);
            await Task.Delay(50);
            release.SetResult();
            await Task.WhenAll(reset, fire).WaitAsync(Timeout);

            // Assert: the fire could not slip in between exit and re-entry
            Assert.Equal(new[] { "B>", "<A", "A>", "<B" }, visited);
            Assert.Equal(State.B, _sm.CurrentState);
        }

        #endregion

        #region InitializeAsync

        [Fact]
        public async Task InitializeAsync_UnconfiguredState_ThrowsArgumentException()
        {
            // Arrange
            _config.Configure(State.A);

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => _sm.InitializeAsync(State.C));

            // Assert
            Assert.Equal("state", exception.ParamName);
            Assert.Contains("C", exception.Message);
            Assert.Null(_sm.CurrentState);
        }

        [Fact]
        public async Task InitializeAsync_InitialStateNotConfigured_Throws()
        {
            // Arrange
            _config.Configure(State.B);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _sm.InitializeAsync());
            Assert.Null(_sm.CurrentState);
        }

        [Fact]
        public async Task InitializeAsync_ViaInterfaceWithState_Applied()
        {
            // Arrange
            _config.Configure(State.A);
            _config.Configure(State.B);
            IStateMachine<Trigger, State> sm = _sm;

            // Act
            await sm.InitializeAsync(State.B);

            // Assert
            Assert.Equal(State.B, sm.CurrentState);
        }

        [Fact]
        public async Task InitializeAsync_WithState_EntryActionCalled()
        {
            // Arrange
            var calls = 0;
            _config.Configure(State.A);
            _config.Configure(State.B)
                .OnEntry(() => calls++);

            // Act
            await _sm.InitializeAsync(State.B);

            // Assert
            Assert.Equal(1, calls);
        }

        #endregion

        #region InStateAsync

        [Fact]
        public async Task InStateAsync_Uninitialized_Throws()
        {
            // Arrange
            _config.Configure(State.A);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _sm.InStateAsync(State.A));
            await Assert.ThrowsAsync<InvalidOperationException>(() => _sm.InStateAsync(State.A, 1));
        }

        [Theory]
        [InlineData(State.C, 0, true)]
        [InlineData(State.B, 0, false)]
        [InlineData(State.B, 1, true)]
        [InlineData(State.A, 0, false)]
        [InlineData(State.A, 1, false)]
        [InlineData(State.A, 2, true)]
        [InlineData(State.A, 5, true)]
        public async Task InStateAsync_MaxDepth_LimitsHierarchySearch(State test, ushort maxDepth, bool expected)
        {
            // Arrange: C is a substate of B, which is a substate of A
            _config.Configure(State.A);
            _config.Configure(State.B)
                .SubstateOf(State.A);
            _config.Configure(State.C)
                .SubstateOf(State.B);
            await _sm.InitializeAsync(State.C);

            // Act
            var result = await _sm.InStateAsync(test, maxDepth);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region CanFireAsync

        [Fact]
        public async Task CanFireAsync_Uninitialized_False()
        {
            // Arrange
            _config.Configure(State.A)
                .Permit(Trigger.a, State.B);
            _config.Configure(State.B);

            // Act
            var result = await _sm.CanFireAsync(Trigger.a);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CanFireAsync_GuardThrows_False()
        {
            // Arrange
            _config.Configure(State.A)
                .PermitIf(Trigger.a, State.B, new Func<bool>(() => throw new TimeoutException()));
            _config.Configure(State.B);
            await _sm.InitializeAsync();

            // Act
            var result = await _sm.CanFireAsync(Trigger.a);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Concurrency

        [Fact]
        public async Task FireAsync_Parallel_CallbacksNeverOverlap()
        {
            // Arrange
            const int count = 50;
            var active = 0;
            var maxActive = 0;
            var entries = 0;

            async Task Callback()
            {
                var now = Interlocked.Increment(ref active);
                InterlockedMax(ref maxActive, now);
                await Task.Delay(1);
                Interlocked.Decrement(ref active);
            }

            _config.Configure(State.A)
                .PermitReentry(Trigger.a)
                .OnEntry(async () =>
                {
                    Interlocked.Increment(ref entries);
                    await Callback();
                })
                .OnExit(Callback);
            await _sm.InitializeAsync();
            entries = 0;

            // Act
            await Task.WhenAll(Enumerable.Range(0, count).Select(_ => Task.Run(() => _sm.FireAsync(Trigger.a))))
                      .WaitAsync(Timeout);

            // Assert
            Assert.Equal(1, maxActive);
            Assert.Equal(count, entries);
        }

        [Fact]
        public async Task FireAsync_Parallel_TransitionsPublishedInOrder()
        {
            // Arrange
            const int count = 50;
            var transitions = new List<Transition<Trigger, State>>();
            _config.Configure(State.A)
                .Permit(Trigger.a, State.B);
            _config.Configure(State.B)
                .Permit(Trigger.a, State.A);
            await _sm.InitializeAsync();
            using var _ = _sm.Observable.Subscribe(t => { lock (transitions) transitions.Add(t); });

            // Act
            await Task.WhenAll(Enumerable.Range(0, count).Select(_ => Task.Run(() => _sm.FireAsync(Trigger.a))))
                      .WaitAsync(Timeout);

            // Assert: every transition starts where the previous one ended
            Assert.Equal(count, transitions.Count);
            var expectedSource = State.A;
            foreach (var t in transitions)
            {
                Assert.Equal(expectedSource, t.Source);
                expectedSource = t.Destination;
            }
            Assert.Equal(count % 2 == 0 ? State.A : State.B, _sm.CurrentState);
        }

        #endregion

        #region Reentrancy

        // The machine's lock must be reentrant: a callback running under the lock
        // may call back into the same machine without deadlocking.

        [Fact]
        public async Task FireAsync_FromEntryCallback_DoesNotDeadlock()
        {
            // Arrange
            _config.Configure(State.A)
                .Permit(Trigger.a, State.B);
            _config.Configure(State.B)
                .Permit(Trigger.a, State.C)
                .OnEntry(async () => await _sm.FireAsync(Trigger.a));
            _config.Configure(State.C);
            await _sm.InitializeAsync();

            // Act
            await _sm.FireAsync(Trigger.a).WaitAsync(Timeout);

            // Assert
            Assert.Equal(State.C, _sm.CurrentState);
        }

        [Fact]
        public async Task FireAsync_FromExitCallback_DoesNotDeadlock()
        {
            // Arrange
            var inState = false;
            _config.Configure(State.A)
                .Permit(Trigger.a, State.B)
                .OnExit(async () => inState = await _sm.InStateAsync(State.A));
            _config.Configure(State.B);
            await _sm.InitializeAsync();

            // Act
            await _sm.FireAsync(Trigger.a).WaitAsync(Timeout);

            // Assert
            Assert.True(inState);
            Assert.Equal(State.B, _sm.CurrentState);
        }

        [Fact]
        public async Task CanFireAsync_FromEntryCallback_DoesNotDeadlock()
        {
            // Arrange
            var canFire = false;
            _config.Configure(State.A)
                .Permit(Trigger.a, State.B);
            _config.Configure(State.B)
                .Permit(Trigger.a, State.C)
                .OnEntry(async () => canFire = await _sm.CanFireAsync(Trigger.a));
            _config.Configure(State.C);
            await _sm.InitializeAsync();

            // Act
            await _sm.FireAsync(Trigger.a).WaitAsync(Timeout);

            // Assert
            Assert.True(canFire);
        }

        #endregion

        #region Transition consistency

        [Fact]
        public async Task FireAsync_WithParamWithoutMatchingCallback_HasNoSideEffects()
        {
            // Arrange
            var exitCalled = false;
            _config.Configure(State.A)
                .Permit(Trigger.a, State.B)
                .OnExit(() => exitCalled = true);
            _config.Configure(State.B)
                .OnEntry(() => { });
            await _sm.InitializeAsync();
            var transitions = new List<Transition<Trigger, State>>();
            using var _ = _sm.Observable.Subscribe(transitions.Add);

            // Act
            var exception = await Record.ExceptionAsync(() => _sm.FireAsync(Trigger.a, 42));

            // Assert: the failure happens before exit action, state change and publication
            var ex = Assert.IsType<ArgumentException>(exception);
            Assert.Equal("parameter", ex.ParamName);
            Assert.False(exitCalled);
            Assert.Empty(transitions);
            Assert.Equal(State.A, _sm.CurrentState);
        }

        [Fact]
        public async Task FireAsync_WithParam_ArgumentExceptionFromCallback_PropagatesUnchanged()
        {
            // Arrange
            var original = new ArgumentException("boom", "other");
            _config.Configure(State.A)
                .Permit(Trigger.a, State.B);
            _config.Configure(State.B)
                .OnEntry<int>(_ => throw original);
            await _sm.InitializeAsync();

            // Act
            var exception = await Record.ExceptionAsync(() => _sm.FireAsync(Trigger.a, 42));

            // Assert
            Assert.Same(original, exception);
        }

        [Fact]
        public async Task FireAsync_ObserverThrows_EntryActionStillRunsAndErrorPropagates()
        {
            // Arrange
            var entered = false;
            _config.Configure(State.A)
                .Permit(Trigger.a, State.B);
            _config.Configure(State.B)
                .OnEntry(() => entered = true);
            await _sm.InitializeAsync();
            using var _ = _sm.Observable.Subscribe(t =>
            {
                if (t.Destination == State.B)
                    throw new InvalidOperationException("observer failed");
            });

            // Act
            var exception = await Record.ExceptionAsync(() => _sm.FireAsync(Trigger.a));

            // Assert: state and entry side effects stay consistent
            Assert.IsType<InvalidOperationException>(exception);
            Assert.Equal("observer failed", exception.Message);
            Assert.True(entered);
            Assert.Equal(State.B, _sm.CurrentState);
        }

        [Fact]
        public async Task FireAsync_MultipleAsyncCallbacks_RunInRegistrationOrder()
        {
            // Arrange
            var log = new List<string>();
            _config.Configure(State.A)
                .PermitReentry(Trigger.a)
                .OnEntry(async () =>
                {
                    log.Add("first:start");
                    await Task.Delay(50);
                    log.Add("first:end");
                })
                .OnEntry(() => { log.Add("second"); return Task.CompletedTask; });
            await _sm.InitializeAsync();
            log.Clear();

            // Act
            await _sm.FireAsync(Trigger.a).WaitAsync(Timeout);

            // Assert
            Assert.Equal(new[] { "first:start", "first:end", "second" }, log);
        }

        #endregion

        #region Double initialization

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task InitializeAsync_AlreadyInitialized_ThrowsAndChangesNothing(bool withState)
        {
            // Arrange
            var entered = 0;
            var exited = 0;
            _config.Configure(State.A)
                .OnEntry(() => entered++)
                .OnExit(() => exited++);
            _config.Configure(State.B)
                .OnEntry(() => entered++);
            await _sm.InitializeAsync();
            var transitions = new List<Transition<Trigger, State>>();
            using var _ = _sm.Observable.Subscribe(transitions.Add);

            // Act
            var exception = await Record.ExceptionAsync(
                () => withState ? _sm.InitializeAsync(State.B) : _sm.InitializeAsync());

            // Assert
            Assert.IsType<InvalidOperationException>(exception);
            Assert.Equal(State.A, _sm.CurrentState);
            Assert.Equal(1, entered);
            Assert.Equal(0, exited);
            Assert.Empty(transitions);
        }

        [Fact]
        public async Task ResetAsync_AfterInitialize_StillWorks()
        {
            // Arrange
            _config.Configure(State.A);
            await _sm.InitializeAsync();

            // Act
            await _sm.ResetAsync();

            // Assert
            Assert.Equal(State.A, _sm.CurrentState);
        }

        [Fact]
        public async Task InitializeAsync_Parallel_ExactlyOneSucceeds()
        {
            // Arrange
            const int count = 20;
            var entered = 0;
            _config.Configure(State.A).OnEntry(() => Interlocked.Increment(ref entered));

            // Act
            var results = await Task.WhenAll(Enumerable.Range(0, count)
                .Select(_ => Task.Run(() => Record.ExceptionAsync(() => _sm.InitializeAsync()))))
                .WaitAsync(Timeout);

            // Assert
            Assert.Equal(1, results.Count(x => x == null));
            Assert.Equal(count - 1, results.Count(x => x is InvalidOperationException));
            Assert.Equal(1, entered);
        }

        #endregion

        #region Disposal

        [Theory]
        [MemberData(nameof(AllOperations))]
        public async Task Operation_AfterDispose_ThrowsObjectDisposed(string operation)
        {
            // Arrange
            var exited = false;
            ConfigureForAllOperations(() => exited = true);
            await _sm.InitializeAsync();
            _sm.Dispose();

            // Act
            var exception = await Record.ExceptionAsync(() => Invoke(_sm, operation, CancellationToken.None));

            // Assert: nothing happens on a disposed machine
            Assert.IsType<ObjectDisposedException>(exception);
            Assert.False(exited);
            Assert.Equal(State.A, _sm.CurrentState);
        }

        [Fact]
        public async Task Dispose_WhileFireIsWaitingForLock_WaitingCallThrowsObjectDisposed()
        {
            // Arrange
            var block = false;
            var entryRuns = 0;
            var entered = new TaskCompletionSource();
            var release = new TaskCompletionSource();
            _config.Configure(State.A)
                .PermitReentry(Trigger.a)
                .OnEntry(async () =>
                {
                    Interlocked.Increment(ref entryRuns);
                    if (!block)
                        return;

                    entered.TrySetResult();
                    await release.Task;
                });
            await _sm.InitializeAsync();
            block = true;
            entryRuns = 0;

            var running = _sm.FireAsync(Trigger.a);
            await entered.Task.WaitAsync(Timeout);
            var waiting = _sm.FireAsync(Trigger.a);

            // Act
            _sm.Dispose();
            release.SetResult();

            // Assert: the running call finishes, the one that was waiting for the lock is rejected
            await running.WaitAsync(Timeout);
            await Assert.ThrowsAsync<ObjectDisposedException>(() => waiting.WaitAsync(Timeout));
            Assert.Equal(1, entryRuns);
        }

        #endregion

        #region Failed initialization

        [Fact]
        public async Task InitializeAsync_EntryCallbackThrows_MachineStaysUninitializedAndCanRetry()
        {
            // Arrange
            var fail = true;
            _config.Configure(State.A)
                .OnEntry(() =>
                {
                    if (fail)
                        throw new InvalidOperationException("entry failed");
                });

            // Act
            var first = await Record.ExceptionAsync(() => _sm.InitializeAsync());

            // Assert
            Assert.Equal("entry failed", first.Message);
            Assert.Null(_sm.CurrentState);
            await Assert.ThrowsAsync<InvalidOperationException>(() => _sm.FireAsync(Trigger.a));

            // Act: retry succeeds once the cause is gone
            fail = false;
            await _sm.InitializeAsync();

            // Assert
            Assert.Equal(State.A, _sm.CurrentState);
        }

        #endregion

        #region Helpers

        private static void InterlockedMax(ref int target, int value)
        {
            int current;
            while ((current = Volatile.Read(ref target)) < value)
            {
                if (Interlocked.CompareExchange(ref target, value, current) == current)
                    break;
            }
        }

        #endregion
    }
}
