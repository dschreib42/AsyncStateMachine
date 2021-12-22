using System;

namespace AsyncStateMachine.Behaviours
{
    /// <summary>
    /// Implementation of the Ignored triggers behavior.
    /// </summary>
    /// <typeparam name="TTrigger">Type of trigger.</typeparam>
    /// <typeparam name="TState">Type of state.</typeparam>
    internal sealed class IgnoredTriggerBehaviour<TTrigger, TState> : BaseTriggerBehaviour<TTrigger, TState>
        where TTrigger : struct
        where TState : struct
    {
        /// <summary>
        /// Initializes a new instance of a <see cref="IgnoredTriggerBehaviour{TTrigger,TState}"/> class.
        /// </summary>
        /// <param name="source">The source state.</param>
        /// <param name="trigger">The trigger.</param>
        /// <param name="target">The destination state.</param>
        public IgnoredTriggerBehaviour(TState source, TTrigger trigger, TState target)
            : base(source, trigger, target)
        {
            if (!source.Equals(target))
                throw new ArgumentException("Source and target do not match", nameof(target));
        }
    }
}