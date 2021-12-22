using AsyncStateMachine.Behaviours;
using AsyncStateMachine.Callbacks;
using AsyncStateMachine.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AsyncStateMachine
{
    /// <summary>
    /// Manages all state transitions and all callbacks to be called on state entry/exit.
    /// </summary>
    /// <typeparam name="TTrigger">Type of trigger.</typeparam>
    /// <typeparam name="TState">Type of state.</typeparam>
    internal class StateRepresentation<TTrigger, TState> : IStateConfiguration<TTrigger, TState>
        where TTrigger : struct
        where TState : struct
    {
        #region Fields

        private readonly TState _state;
        private readonly IDictionary<TTrigger, IList<ITriggerBehaviour<TTrigger, TState>>> _triggerBehaviours;

        private readonly IList<ICallback> _onEntry;
        private readonly IList<ICallback> _onExit;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of a <see cref="StateRepresentation{Trigger, State}"/> class.
        /// </summary>
        /// <param name="sourceState">The state.</param>
        public StateRepresentation(TState sourceState)
        {
            _state = sourceState;
            _triggerBehaviours = new Dictionary<TTrigger, IList<ITriggerBehaviour<TTrigger, TState>>>();
            _onEntry = new List<ICallback>();
            _onExit = new List<ICallback>();
        }

        #endregion

        #region IStateConfiguration implementation

        /// <inheritdoc/>
        public IStateConfiguration<TTrigger, TState> Permit(TTrigger trigger, TState targetState)
        {
            AddTriggerBehaviour(new PermitTriggerBehaviour<TTrigger, TState>(_state, trigger, targetState));
            return this;
        }

        /// <inheritdoc/>
        public IStateConfiguration<TTrigger, TState> PermitIf(TTrigger trigger, TState targetState, Func<bool> condition)
        {
            AddTriggerBehaviour(new PermitIfTriggerBehaviour<TTrigger, TState>(_state, trigger, targetState, condition));

            return this;
        }

        /// <inheritdoc/>
        public IStateConfiguration<TTrigger, TState> PermitIf(TTrigger trigger, TState targetState, Func<Task<bool>> condition)
        {
            AddTriggerBehaviour(new PermitIfTriggerBehaviour<TTrigger, TState>(_state, trigger, targetState, condition));

            return this;
        }

        /// <inheritdoc/>
        public IStateConfiguration<TTrigger, TState> Ignore(TTrigger trigger)
        {
            if (!_triggerBehaviours.ContainsKey(trigger))
                _triggerBehaviours.Add(trigger, new List<ITriggerBehaviour<TTrigger, TState>>());
            else if (_triggerBehaviours[trigger].Any(x => x is PermitTriggerBehaviour<TTrigger, TState> && _state.Equals(x.TargetState)))
                throw new ArgumentException("A Permit() was already created for the same trigger", nameof(trigger));
            else if (_triggerBehaviours[trigger].Any(x => x is PermitIfTriggerBehaviour<TTrigger, TState> && _state.Equals(x.TargetState)))
                throw new ArgumentException("A PermitIf() was already created for the same target state", nameof(trigger));
            else if (_triggerBehaviours[trigger].Any(x => x is IgnoredTriggerBehaviour<TTrigger, TState> && _state.Equals(x.TargetState)))
                throw new ArgumentException($"The ignored trigger is already configured", nameof(trigger));

            _triggerBehaviours[trigger].Add(new IgnoredTriggerBehaviour<TTrigger, TState>(_state, trigger, _state));

            return this;
        }

        /// <inheritdoc/>
        public IStateConfiguration<TTrigger, TState> OnEntry(Func<Task> func)
        {
            _ = func ?? throw new ArgumentNullException(nameof(func));
            _onEntry.Add(new FuncCallback(func));
            return this;
        }

        /// <inheritdoc/>
        public IStateConfiguration<TTrigger, TState> OnEntry(Action action)
        {
            _ = action ?? throw new ArgumentNullException(nameof(action));
            _onEntry.Add(new ActionCallback(action));
            return this;
        }

        /// <inheritdoc/>
        public IStateConfiguration<TTrigger, TState> OnExit(Action action)
        {
            _ = action ?? throw new ArgumentNullException(nameof(action));
            _onExit.Add(new ActionCallback(action));
            return this;
        }

        /// <inheritdoc/>
        public IStateConfiguration<TTrigger, TState> OnEntry<TParam>(Action<TParam> action)
        {
            _ = action ?? throw new ArgumentNullException(nameof(action));
            _onEntry.Add(new ActionWithParamCallback<TParam>(action));
            return this;
        }

        /// <inheritdoc/>
        public IStateConfiguration<TTrigger, TState> OnEntry<TParam>(Func<TParam, Task> func)
        {
            _ = func ?? throw new ArgumentNullException(nameof(func));
            _onEntry.Add(new FuncWithParamCallback<TParam>(func));
            return this;
        }

        /// <inheritdoc/>
        public IStateConfiguration<TTrigger, TState> OnExit(Func<Task> func)
        {
            _ = func ?? throw new ArgumentNullException(nameof(func));
            _onExit.Add(new FuncCallback(func));
            return this;
        }

        /// <inheritdoc/>
        internal IEnumerable<Transition<TTrigger, TState>> Transitions
        {
            get
            {
                foreach (var behaviour in _triggerBehaviours)
                {
                    foreach (var entry in behaviour.Value)
                    {
                        yield return new Transition<TTrigger, TState>(entry.SourceState,
                                                                      entry.Trigger,
                                                                      entry.TargetState);
                    }
                }
            }
        }

        #endregion

        #region Internal methods

        /// <summary>
        /// Can the current
        /// </summary>
        /// <param name="trigger"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        internal async Task<(bool, TState?)> CanFireAsync(TTrigger trigger)
        {
            if (!_triggerBehaviours.TryGetValue(trigger, out var behaviours))
                throw new InvalidOperationException($"Invalid trigger '{trigger}' in state: '{_state}'");

            foreach (var behaviour in behaviours)
            {
                if (await behaviour.Condition())
                {
                    var fire = !(behaviour is IgnoredTriggerBehaviour<TTrigger, TState>);
                    var state = behaviour.TargetState;

                    return (fire, state);
                }
            }

            return (false, null);
        }

        /// <summary>
        /// Provides all callbacks to be executed on state entry.
        /// </summary>
        internal IEnumerable<ICallback> OnEntryCallbacks => _onEntry.ToList().AsReadOnly();

        /// <summary>
        /// Provides all callbacks to be executed on state exit.
        /// </summary>
        internal IEnumerable<ICallback> OnExitCallbacks => _onExit.ToList().AsReadOnly();

        #endregion

        #region Helper methods

        private void AddTriggerBehaviour(ITriggerBehaviour<TTrigger, TState> triggerBehaviour)
        {
            var trigger = triggerBehaviour.Trigger;
            var targetState = triggerBehaviour.TargetState;

            if (!_triggerBehaviours.ContainsKey(trigger))
                _triggerBehaviours.Add(trigger, new List<ITriggerBehaviour<TTrigger, TState>>());
            else if (_triggerBehaviours[trigger].Any(x => x is PermitTriggerBehaviour<TTrigger, TState> && targetState.Equals(x.TargetState)))
                throw new ArgumentException("A Permit() was already created for the same trigger", nameof(targetState));
            else if (_triggerBehaviours[trigger].Any(x => x is PermitIfTriggerBehaviour<TTrigger, TState> && targetState.Equals(x.TargetState)))
                throw new ArgumentException("A PermitIf() was already created for the same target state", nameof(targetState));
            else if (_triggerBehaviours[trigger].Any(x => x is IgnoredTriggerBehaviour<TTrigger, TState> && targetState.Equals(x.TargetState)))
                throw new ArgumentException($"The ignored trigger is already configured", nameof(trigger));

            _triggerBehaviours[trigger].Add(triggerBehaviour);
        }

        #endregion
    }
}