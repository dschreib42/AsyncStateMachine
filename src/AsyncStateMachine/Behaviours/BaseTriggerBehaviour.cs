using System.Threading.Tasks;

namespace AsyncStateMachine.Behaviours
{
    /// <summary>
    /// Base class for all triggers behaviors.
    /// </summary>
    /// <typeparam name="TTrigger">Type of trigger.</typeparam>
    /// <typeparam name="TState">Type of state.</typeparam>
    internal abstract class BaseTriggerBehaviour<TTrigger, TState> : ITriggerBehaviour<TTrigger, TState>
        where TTrigger : struct
        where TState : struct
    {
        /// <summary>
        /// Initializes a new instance of a <see cref="BaseTriggerBehaviour{TTrigger, TState}"/> class.
        /// </summary>
        /// <param name="source">The source state.</param>
        /// <param name="trigger">The trigger.</param>
        /// <param name="target">The destination state.</param>
        public BaseTriggerBehaviour(TState source, TTrigger trigger, TState target)
        {
            Trigger = trigger;
            SourceState = source;
            TargetState = target;
        }

        /// <inheritdoc/>
        public TState SourceState { get; }

        /// <inheritdoc/>
        public TState TargetState { get; }

        /// <inheritdoc/>
        public TTrigger Trigger { get; }

        /// <inheritdoc/>
        public virtual Task<bool> Condition() => Task.FromResult(true);
    }
}