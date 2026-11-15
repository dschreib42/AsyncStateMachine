using AsyncStateMachine.Callbacks;
using AsyncStateMachine.Contracts;
using NeoSmart.AsyncLock;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncStateMachine
{
    /// <summary>
    /// Main StateMachine implementation.
    /// </summary>
    /// <typeparam name="TTrigger">Type of trigger.</typeparam>
    /// <typeparam name="TState">Type of state.</typeparam>
    public sealed class StateMachine<TTrigger, TState> : IStateMachine<TTrigger, TState>
        where TTrigger : struct
        where TState : struct
    {
        #region Fields

        private readonly ISubject<Transition<TTrigger, TState>> _subject;
        private readonly ICallbackFilter _filter;
        private readonly ICallbackExecutor _executor;
        private readonly StateMachineConfiguration<TTrigger, TState> _configuration;
        private readonly AsyncLock _asyncLock;

        private TState? _currentState;
        private int _disposed;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of a <see cref="StateMachine{Trigger, State}"/> class.
        /// </summary>
        /// <param name="configuration">The state machine configuration instance.</param>
        public StateMachine(StateMachineConfiguration<TTrigger, TState> configuration)
            : this(configuration,
                   new Subject<Transition<TTrigger, TState>>(),
                   new CallbackFilter(),
                   new CallbackExecutor())
        {
        }

        /// <summary>
        /// Initializes a new instance of a <see cref="StateMachine{Trigger, State}"/> class.
        /// </summary>
        /// <param name="subject">The subject used as observable implementation.</param>
        /// <param name="executor">The callback executor implementation.</param>
        /// <param name="filter">The callback filter implementation.</param>
        /// <param name="initialState">The initial state.</param>
        internal StateMachine(StateMachineConfiguration<TTrigger, TState> configuration,
                              ISubject<Transition<TTrigger, TState>> subject,
                              ICallbackFilter filter,
                              ICallbackExecutor executor)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _subject = subject ?? throw new ArgumentNullException(nameof(subject));
            _filter = filter ?? throw new ArgumentNullException(nameof(filter));
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
            _asyncLock = new AsyncLock();
        }

        #endregion

        #region Properties

        /// <inheritdoc/>
        public IObservable<Transition<TTrigger, TState>> Observable => _subject;

        /// <inheritdoc/>
        public TState? CurrentState => _currentState;

        #endregion

        #region IStateMachine implementation

        /// <inheritdoc/>
        public Task InitializeAsync(CancellationToken cancellationToken = default)
            => InitializeAsync(_configuration.InitialState, cancellationToken);

        /// <inheritdoc/>
        public async Task InitializeAsync(TState state, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            EnsureStateConfigured(state);

            using (await _asyncLock.LockAsync(cancellationToken))
            {
                ThrowIfDisposed();

                if (_currentState.HasValue)
                    throw new InvalidOperationException("StateMachine already initialized. Use ResetAsync to restart it.");

                try
                {
                    await EnterStateAsync(state);
                }
                catch
                {
                    // initialization failed, so the machine must not count as initialized
                    _currentState = null;
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public async Task ResetAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var initialState = _configuration.InitialState;
            EnsureStateConfigured(initialState);

            // exit and re-enter under a single lock, so the reset is atomic and
            // cancellation can only happen before anything has been changed
            using (await _asyncLock.LockAsync(cancellationToken))
            {
                ThrowIfDisposed();

                if (_currentState.HasValue)
                {
                    // call exit action for current state
                    await OnExitAsync(_configuration.GetStateConfiguration(_currentState.Value), PredicateWithoutParam);
                }

                await EnterStateAsync(initialState);
            }
        }

        /// <inheritdoc/>
        public Task<bool> InStateAsync(TState state, CancellationToken cancellationToken = default)
            => InStateAsync(state, 5, cancellationToken);

        /// <inheritdoc/>
        public async Task<bool> InStateAsync(TState state, ushort maxDepth, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            using (await _asyncLock.LockAsync(cancellationToken))
            {
                ThrowIfDisposed();

                if (!_currentState.HasValue)
                    throw new InvalidOperationException("StateMachine not yet initialized");

                if (state.Equals(_currentState))
                    return true;

                var nextState = _currentState.Value;
                for (var i = 0; i < maxDepth; i++)
                {
                    var parentState = _configuration.GetStateConfiguration(nextState).ParentState;
                    if (parentState.HasValue)
                    {
                        if (state.Equals(parentState))
                            return true;

                        nextState = parentState.Value;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public async Task<bool> CanFireAsync(TTrigger trigger, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            try
            {
                using (await _asyncLock.LockAsync(cancellationToken))
                {
                    ThrowIfDisposed();

                    if (!_currentState.HasValue || !_configuration.HasState(_currentState.Value))
                        return false;

                    var result = await _configuration.GetStateConfiguration(_currentState.Value).CanFireAsync(trigger);

                    return result.Item1;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (ObjectDisposedException)
            {
                throw;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task FireAsync(TTrigger trigger, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            using (await _asyncLock.LockAsync(cancellationToken))
            {
                ThrowIfDisposed();

                if (!_currentState.HasValue)
                    throw new InvalidOperationException("StateMachine not yet initialized");

                await FireAsync(trigger, _currentState.Value);
            }
        }

        /// <inheritdoc/>
        public async Task FireAsync<TParam>(TTrigger trigger, TParam parameter, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            using (await _asyncLock.LockAsync(cancellationToken))
            {
                ThrowIfDisposed();

                if (!_currentState.HasValue)
                    throw new InvalidOperationException("StateMachine not yet initialized");

                await FireAsync(trigger, _currentState.Value, parameter);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            // tell observers that no more transitions will follow
            try
            {
                _subject.OnCompleted();
            }
            catch (Exception)
            {
                // an observer failing must not make Dispose throw
            }

            if (_subject is IDisposable x)
            {
                x.Dispose();
            }

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Helper methods

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
                throw new ObjectDisposedException(GetType().Name);
        }

        private void EnsureStateConfigured(TState state)
        {
            if (!_configuration.HasState(state))
                throw new ArgumentException($"State '{state}' is not configured", nameof(state));

            _configuration.Validate();
        }

        /// <summary>
        /// Sets the given state as current state, publishes it and runs its entry actions.
        /// Must be called while holding the lock.
        /// </summary>
        private async Task EnterStateAsync(TState state)
        {
            var configuration = _configuration.GetStateConfiguration(state);
            var entryCallbacks = _filter.Filter(configuration.OnEntryCallbacks, PredicateWithoutParam);

            _currentState = state;

            await PublishAndEnterAsync(new Transition<TTrigger, TState>(null, null, state), entryCallbacks);
        }

        private Task<TState> FireAsync(TTrigger trigger, TState previous)
            => PerformTransition(previous, trigger, PredicateWithoutParam);

        private Task<TState> FireAsync<TParam>(TTrigger trigger, TState previous, TParam param)
            => PerformTransition(previous, trigger, PredicateWithParam<TParam>, param, ParamCallbacksGuard);

        private async Task<TState> PerformTransition(TState previous,
                                                     TTrigger trigger,
                                                     Func<ICallback, bool> predicate,
                                                     object parameter = null,
                                                     Action<IReadOnlyCollection<ICallback>> guard = null)
        {
            var configuration = _configuration.GetStateConfiguration(previous);

            var (canBeFired, nextState) = await configuration.CanFireAsync(trigger);
            if (!canBeFired)
            {
                return nextState ?? throw new InvalidOperationException("Failed to find a matching trigger");
            }

            var next = nextState.Value;

            // resolve (and validate) the entry callbacks before any side effect happens,
            // so a failing guard leaves the machine and its observers untouched
            var entryCallbacks = _filter.Filter(_configuration.GetStateConfiguration(next).OnEntryCallbacks, predicate, guard);

            // call exit action for current state
            await OnExitAsync(_configuration.GetStateConfiguration(previous), PredicateWithoutParam);

            _currentState = next;

            await PublishAndEnterAsync(new Transition<TTrigger, TState>(previous, trigger, next), entryCallbacks, parameter);

            return next;
        }

        /// <summary>
        /// Publishes the transition and runs the entry callbacks. The state has already changed at
        /// this point, so the entry callbacks run even if an observer throws; the observer's
        /// exception is rethrown afterwards.
        /// </summary>
        private async Task PublishAndEnterAsync(Transition<TTrigger, TState> transition,
                                                IEnumerable<ICallback> entryCallbacks,
                                                object parameter = null)
        {
            Exception publishError = null;
            try
            {
                _subject.OnNext(transition);
            }
            catch (Exception ex)
            {
                publishError = ex;
            }

            await _executor.ExecuteAsync(entryCallbacks, parameter);

            if (publishError != null)
                ExceptionDispatchInfo.Capture(publishError).Throw();
        }

        private Task OnExitAsync(StateConfiguration<TTrigger, TState> configuration,
                                 Func<ICallback, bool> predicate,
                                 object parameter = null,
                                 Action<IReadOnlyCollection<ICallback>> guard = null)
        {
            var callbacks = _filter.Filter(configuration.OnExitCallbacks, predicate, guard);

            return _executor.ExecuteAsync(callbacks, parameter);
        }

        private static bool PredicateWithoutParam(ICallback callback)
            => callback is FuncCallback || callback is ActionCallback;

        private static bool PredicateWithParam<TParam>(ICallback callback)
            => callback is FuncWithParamCallback<TParam> || callback is ActionWithParamCallback<TParam>;

        private static void ParamCallbacksGuard(IReadOnlyCollection<ICallback> callbacks)
        {
            if (callbacks == null || callbacks.Count < 1)
                throw new ArgumentException("No matching callback with parameter was found", "parameter");
        }

        #endregion
    }
}