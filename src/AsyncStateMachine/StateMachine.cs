using AsyncStateMachine.Callbacks;
using AsyncStateMachine.Contracts;
using NeoSmart.AsyncLock;
using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
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
        private bool _disposed;

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
        public async Task InitializeAsync(TState? state = null)
        {
            if (state.HasValue && !_configuration.HasState(state.Value))
                throw new ArgumentException("State not configured");

            _configuration.Validate();

            using (await _asyncLock.LockAsync())
            {
                var targetState = state ?? _configuration.InitialState;
                var configuration = _configuration.GetStateConfiguration(targetState);

                _currentState = targetState;

                // publish state changed
                _subject.OnNext(new Transition<TTrigger, TState>(null, null, targetState));

                // call entry action for current state
                await OnEnterAsync(configuration, PredicateWithoutParam);
            }
        }

        /// <inheritdoc/>
        public async Task ResetAsync()
        {
            using (await _asyncLock.LockAsync())
            {
                if (_currentState.HasValue)
                {
                    // call exit action for current state
                    await OnExitAsync(_configuration.GetStateConfiguration(_currentState.Value), PredicateWithoutParam);
                }
            }

            await InitializeAsync();
        }

        /// <inheritdoc/>
        public async Task<bool> InStateAsync(TState state, ushort maxDepth = 5)
        {
            using (await _asyncLock.LockAsync())
            {
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
        public async Task<bool> CanFireAsync(TTrigger trigger)
        {
            try
            {
                using (await _asyncLock.LockAsync())
                {
                    if (!_currentState.HasValue || !_configuration.HasState(_currentState.Value))
                        return false;

                    var result = await _configuration.GetStateConfiguration(_currentState.Value).CanFireAsync(trigger);

                    return result.Item1;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task FireAsync(TTrigger trigger)
        {
            using (await _asyncLock.LockAsync())
            {
                if (!_currentState.HasValue)
                    throw new InvalidOperationException("StateMachine not yet initialized");

                await FireAsync(trigger, _currentState.Value);
            }
        }

        /// <inheritdoc/>
        public async Task FireAsync<TParam>(TTrigger trigger, TParam parameter)
        {
            using (await _asyncLock.LockAsync())
            {
                try
                {
                    if (!_currentState.HasValue)
                        throw new InvalidOperationException("StateMachine not yet initialized");

                    await FireAsync(trigger, _currentState.Value, parameter);
                }
                catch (ArgumentException ex)
                {
                    throw new ArgumentException(ex.Message, nameof(parameter));
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed)
                return;

            if (_subject is IDisposable x)
            {
                x.Dispose();
            }

            _disposed = true;

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Helper methods

        private Task<TState> FireAsync(TTrigger trigger, TState previous)
            => PerformTransition(previous, trigger, PredicateWithoutParam);

        private Task<TState> FireAsync<TParam>(TTrigger trigger, TState previous, TParam param)
            => PerformTransition(previous, trigger, PredicateWithParam<TParam>, param, MinCallbacksGuard);

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

            // call exit action for current state
            await OnExitAsync(_configuration.GetStateConfiguration(previous), PredicateWithoutParam);

            _currentState = next;

            // publish state changed
            _subject.OnNext(new Transition<TTrigger, TState>(previous, trigger, next));

            // call entry action for new state
            await OnEnterAsync(_configuration.GetStateConfiguration(next), predicate, parameter, guard);

            return next;
        }

        private Task OnEnterAsync(StateConfiguration<TTrigger, TState> configuration,
                                  Func<ICallback, bool> predicate,
                                  object parameter = null,
                                  Action<IReadOnlyCollection<ICallback>> guard = null)
        {
            var callbacks = _filter.Filter(configuration.OnEntryCallbacks, predicate, guard);

            return _executor.ExecuteAsync(callbacks, parameter);
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

        private static void MinCallbacksGuard(IReadOnlyCollection<ICallback> callbacks)
        {
            if (callbacks == null || callbacks.Count < 1)
                throw new ArgumentException("No matching callback with parameter was found");
        }

        #endregion
    }
}