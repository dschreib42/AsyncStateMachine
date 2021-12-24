using System;
using System.Threading.Tasks;

namespace AsyncStateMachine.Behaviours
{
    /// <summary>
    /// Implementation of the PermitIf triggers behavior.
    /// </summary>
    /// <typeparam name="TTrigger">Type of trigger.</typeparam>
    /// <typeparam name="TState">Type of state.</typeparam>
    internal sealed class PermitIfTriggerBehaviour<TTrigger, TState> : BaseTriggerBehaviour<TTrigger, TState>
        where TTrigger : struct
        where TState : struct
    {
        private readonly Func<Task<bool>> _condition;

        /// <summary>
        /// Initializes a new instance of a <see cref="PermitIfTriggerBehaviour{TTrigger, TState}"/> class.
        /// </summary>
        /// <param name="source">The source state.</param>
        /// <param name="trigger">The trigger.</param>
        /// <param name="target">The destination state.</param>
        /// <param name="condition">Synchronous function to check the condition of the transition.</param>
        public PermitIfTriggerBehaviour(TState source,
                                        TTrigger trigger,
                                        TState target,
                                        Func<bool> condition)
            : base(source, trigger, target)
        {
            _ = condition ?? throw new ArgumentNullException(nameof(condition));

            _condition = () => Task.FromResult(condition());
        }

        /// <summary>
        /// Initializes a new instance of a <see cref="PermitIfTriggerBehaviour{TTrigger, TState}"/> class.
        /// </summary>
        /// <param name="source">The source state.</param>
        /// <param name="trigger">The trigger.</param>
        /// <param name="target">The destination state.</param>
        /// <param name="condition">Asynchronous function to check the condition of the transition.</param>
        public PermitIfTriggerBehaviour(TState source,
                                        TTrigger trigger,
                                        TState target,
                                        Func<Task<bool>> condition)
            : base(source, trigger, target)
        {
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }

        /// <inheritdoc/>
        public override Task<bool> Condition() => _condition();
    }
}