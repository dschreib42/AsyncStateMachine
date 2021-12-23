using AsyncStateMachine.Callbacks;
using AsyncStateMachine.Contracts;
using NeoSmart.AsyncLock;
using System;
using System.Collections.Generic;
using System.Linq;
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

        private readonly Dictionary<TState, StateRepresentation<TTrigger, TState>> _states;
        private readonly ISubject<Transition<TTrigger, TState>> _subject;
        private readonly ICallbackFilter _filter;
        private readonly ICallbackExecutor _executor;
        private readonly AsyncLock _asyncLock;

        private TState? _initialState;
        private TState? _currentState;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of a <see cref="StateMachine{Trigger, State}"/> class.
        /// </summary>
        /// <param name="initialState">The initial state.</param>
        public StateMachine(TState? initialState = null)
            : this(new Subject<Transition<TTrigger, TState>>(),
                   new CallbackFilter(),
                   new CallbackExecutor(),
                   initialState)
        {
        }

        /// <summary>
        /// Initializes a new instance of a <see cref="StateMachine{Trigger, State}"/> class.
        /// </summary>
        /// <param name="subject">The subject used as observable implementation.</param>
        /// <param name="executor">The callback executor implementation.</param>
        /// <param name="filter">The callback filter implementation.</param>
        /// <param name="initialState">The initial state.</param>
        internal StateMachine(ISubject<Transition<TTrigger, TState>> subject,
                              ICallbackFilter filter,
                              ICallbackExecutor executor,
                              TState? initialState)
        {
            _subject = subject ?? throw new ArgumentNullException(nameof(subject));
            _filter = filter ?? throw new ArgumentNullException(nameof(filter));
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
            _states = new Dictionary<TState, StateRepresentation<TTrigger, TState>>();
            _asyncLock = new AsyncLock();

            _initialState = initialState;
            _currentState = initialState;
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
        public async Task InitializeAsync(TState initialState)
        {
            using (await _asyncLock.LockAsync())
            {
                Validate();

                var representation = GetStateRepresentation(initialState);

                _initialState = initialState;
                _currentState = initialState;

                await OnEnterAsync(representation, PredicateWithoutParam);

                // publish state changed
                _subject.OnNext(new Transition<TTrigger, TState>(null, null, _initialState.Value));
            }
        }

        /// <inheritdoc/>
        public Task ResetAsync()
        {
            return !_initialState.HasValue
                ? throw new InvalidOperationException("StateMachine not yet initialized!")
                : InitializeAsync(_initialState.Value);
        }

        /// <inheritdoc/>
        public async Task<bool> InStateAsync(TState state)
        {
            using (await _asyncLock.LockAsync())
            {
                if (!_currentState.HasValue)
                    throw new InvalidOperationException("StateMachine not yet initialized");

                if (state.Equals(_currentState))
                    return true;

                var representation = GetStateRepresentation(_currentState.Value);
                var superState = representation.ParentState;

                if (superState.HasValue && state.Equals(superState))
                    return true;
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
                    if (!_currentState.HasValue || !KnowsState(_currentState.Value))
                        return false;

                    var result = await GetStateRepresentation(_currentState.Value).CanFireAsync(trigger);

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
        public IStateConfiguration<TTrigger, TState> Configure(TState state)
        {
            var representation = new StateRepresentation<TTrigger, TState>(state);

            _states.Add(state, representation);

            return representation;
        }

        /// <inheritdoc/>
        public IEnumerable<Transition<TTrigger, TState>> Transitions
        {
            get
            {
                if (_initialState.HasValue)
                    yield return new Transition<TTrigger, TState>(null, null, _initialState.Value);

                foreach (var representation in _states.Values)
                {
                    foreach (var transition in representation.Transitions)
                    {
                        yield return transition;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            var disposables = new object[]
            {
                _subject,
                _asyncLock
            };

            foreach (var disposable in disposables.Cast<IDisposable>())
            {
                disposable.Dispose();
            }
        }

        #endregion

        #region Helper methods

        private Task<TState> FireAsync(TTrigger trigger, TState previous)
        {
            return PerformTransition(previous, trigger, PredicateWithoutParam);
        }

        private Task<TState> FireAsync<TParam>(TTrigger trigger, TState previous, TParam param)
        {
            return PerformTransition(previous, trigger, PredicateWithParam<TParam>, param, MinCallbacksGuard);
        }

        private async Task<TState> PerformTransition(TState previous,
                                                     TTrigger trigger,
                                                     Func<ICallback, bool> predicate,
                                                     object parameter = null,
                                                     Action<IList<ICallback>> guard = null)
        {
            var representation = GetStateRepresentation(previous);

            var (canBeFired, nextState) = await representation.CanFireAsync(trigger);
            if (!canBeFired)
            {
                return nextState ?? throw new InvalidOperationException("Failed to find a matching trigger");
            }

            var next = nextState.Value;

            // call exit action for current state
            await OnExitAsync(GetStateRepresentation(previous), PredicateWithoutParam);

            _currentState = next;

            // call entry action for new state
            await OnEnterAsync(GetStateRepresentation(next), predicate, parameter, guard);

            // publish state changed
            _subject.OnNext(new Transition<TTrigger, TState>(previous, trigger, next));

            return next;
        }

        private bool KnowsState(TState state) => _states.ContainsKey(state);

        private StateRepresentation<TTrigger, TState> GetStateRepresentation(TState state)
        {
            return _states.TryGetValue(state, out var representation)
                ? representation
                : throw new InvalidOperationException($"State not found: '{state}'");
        }

        private void Validate()
        {
            foreach (var transition in Transitions)
            {
                if (transition.Source.HasValue && !KnowsState(transition.Source.Value))
                    throw new Exception($"Trigger '{transition.Trigger}' references unknown source state '{transition.Source}'");

                if (!KnowsState(transition.Destination))
                    throw new Exception($"Trigger '{transition.Trigger}' references unknown target state '{transition.Destination}'");
            }
        }

        private Task OnEnterAsync(StateRepresentation<TTrigger, TState> representation,
                                   Func<ICallback, bool> predicate,
                                   object parameter = null,
                                   Action<IList<ICallback>> guard = null)
        {
            var callbacks = _filter.Filter(representation.OnEntryCallbacks, predicate, guard);

            return _executor.ExecuteAsync(callbacks, parameter);
        }

        private Task OnExitAsync(StateRepresentation<TTrigger, TState> representation,
                                  Func<ICallback, bool> predicate,
                                  object parameter = null,
                                  Action<IList<ICallback>> guard = null)
        {
            var callbacks = _filter.Filter(representation.OnExitCallbacks, predicate, guard);

            return _executor.ExecuteAsync(callbacks, parameter);
        }

        private static bool PredicateWithoutParam(ICallback callback)
            => callback is FuncCallback || callback is ActionCallback;

        private static bool PredicateWithParam<TParam>(ICallback callback)
            => callback is FuncWithParamCallback<TParam> || callback is ActionWithParamCallback<TParam>;

        private static void MinCallbacksGuard(IList<ICallback> callbacks)
        {
            if (callbacks == null || callbacks.Count < 1)
                throw new ArgumentException("No matching callback with parameter was found");
        }

        #endregion
    }
}