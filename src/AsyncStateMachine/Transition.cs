namespace AsyncStateMachine
{
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