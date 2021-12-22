using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsyncStateMachine.Contracts
{
    /// <summary>
    /// Operations exposed by the StateMachine.
    /// </summary>
    /// <typeparam name="TTrigger">Type of trigger</typeparam>
    /// <typeparam name="TState">Type of state.</typeparam>
    public interface IStateMachine<TTrigger, TState> : IDisposable
        where TTrigger : struct
        where TState : struct
    {
        /// <summary>
        /// Observable to consume all state changes.
        /// </summary>
        IObservable<Transition<TTrigger, TState>> Observable { get; }

        /// <summary>
        /// Retrieve all configured transitions describing the state machine.
        /// </summary>
        IEnumerable<Transition<TTrigger, TState>> Treansitions { get; }

        /// <summary>
        /// Configures a state and its triggers/transitions.
        /// </summary>
        /// <param name="state">State to configure.</param>
        /// <returns>An instance of a <see cref="IStateConfiguration{TTrigger, TState}"/>.</returns>
        IStateConfiguration<TTrigger, TState> Configure(TState state);

        /// <summary>
        /// Triggers a state change.
        /// </summary>
        /// <param name="trigger">Trigger to use.</param>
        /// <returns>A task that completes, when the state machine has changed state.</returns>
        Task FireAsync(TTrigger trigger);

        /// <summary>
        /// Initializes the state machine,
        /// </summary>
        /// <param name="initialState">The initial state of the state machine.</param>
        /// <returns>A task that completes, when the state machine is initialized.</returns>
        Task InitializeAsync(TState initialState);

        /// <summary>
        /// Resets the state machine to the initial state.
        /// </summary>
        /// <returns></returns>
        Task ResetAsync();
    }

    /// <summary>
    /// Describing the transition from a source state to a destination state.
    /// </summary>
    /// <typeparam name="TTrigger">Type of trigger</typeparam>
    /// <typeparam name="TState">Type of state.</typeparam>
    public class Transition<TTrigger, TState>
        where TTrigger : struct
        where TState : struct
    {
        /// <summary>
        /// Initializes a new instance of a <see cref="Transition{TTrigger, TState}"/> class.
        /// </summary>
        /// <param name="source">The start state.</param>
        /// <param name="trigger">The trigger that initiates the transition.</param>
        /// <param name="destination">The final state of the transition.</param>
        public Transition(TState? source, TTrigger? trigger, TState destination)
        {
            Source = source;
            Trigger = trigger;
            Destination = destination;
        }

        /// <summary>
        /// The source of the transition.
        /// </summary>
        public TState? Source { get; }

        /// <summary>
        /// The trigger of the transition.
        /// </summary>
        public TTrigger? Trigger { get; }

        /// <summary>
        /// The destination of the transition.
        /// </summary>
        public TState Destination { get; }

        /// <summary>
        /// Is start transition?
        /// </summary>
        public bool IsStartTransition => Source is null && Trigger is null;
    }
}