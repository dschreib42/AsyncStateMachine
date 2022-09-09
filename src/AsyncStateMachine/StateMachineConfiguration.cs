using AsyncStateMachine.Contracts;
using System;
using System.Collections.Generic;

namespace AsyncStateMachine
{
    /// <summary>
    /// Main StateMachine configuration implementation.
    /// </summary>
    /// <typeparam name="TTrigger">Type of trigger.</typeparam>
    /// <typeparam name="TState">Type of state.</typeparam>
    public sealed class StateMachineConfiguration<TTrigger, TState> : IStateMachineConfiguration<TTrigger, TState>
        where TTrigger : struct
        where TState : struct
    {
        private readonly Dictionary<TState, StateConfiguration<TTrigger, TState>> _states;
        private readonly TState _initialState;

        /// <summary>
        /// Initializes a new instance of a <see cref="StateMachineConfiguration{Trigger, State}"/> class.
        /// </summary>
        /// <param name="initialState">The initial state.</param>
        public StateMachineConfiguration(TState initialState)
        {
            _states = new Dictionary<TState, StateConfiguration<TTrigger, TState>>();
            _initialState = initialState;
        }

        /// <inheritdoc/>
        public TState InitialState => _initialState;

        /// <inheritdoc/>
        public IStateConfiguration<TTrigger, TState> Configure(TState state)
        {
            var configuration = new StateConfiguration<TTrigger, TState>(state);

            _states.Add(state, configuration);

            return configuration;
        }

        /// <inheritdoc/>
        public IEnumerable<Transition<TTrigger, TState>> Transitions
        {
            get
            {
                yield return new Transition<TTrigger, TState>(null, null, _initialState);

                foreach (var configuration in _states.Values)
                {
                    foreach (var transition in configuration.Transitions)
                    {
                        yield return transition;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void Validate()
        {
            if (!HasState(_initialState))
                throw new Exception($"The initial state '{InitialState} is not configured");

            foreach (var transition in Transitions)
            {
                if (transition.Source.HasValue && !HasState(transition.Source.Value))
                    throw new Exception($"Trigger '{transition.Trigger}' references unknown source state '{transition.Source}'");

                if (!HasState(transition.Destination))
                    throw new Exception($"Trigger '{transition.Trigger}' references unknown target state '{transition.Destination}'");
            }
        }

        /// <summary>
        /// Does a state exist in the configuration?
        /// </summary>
        /// <param name="state">State instance.</param>
        /// <returns><c>True</c> on success, otherwise <c>False</c>.</returns>
        internal bool HasState(TState state) => _states.ContainsKey(state);

        /// <summary>
        /// Returns the configuration for a given state.
        /// </summary>
        /// <param name="state">The state of the configuration request.</param>
        /// <returns>An instance of a <see cref="StateConfiguration{TTrigger, TState}"/>.</returns>
        internal StateConfiguration<TTrigger, TState> GetStateConfiguration(TState state)
        {
            return _states.TryGetValue(state, out var configuration)
                ? configuration
                : throw new ArgumentException($"State not found: '{state}'", nameof(state));
        }
    }
}