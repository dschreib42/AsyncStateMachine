using System;
using System.Threading.Tasks;

namespace AsyncStateMachine.Behaviours
{
    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="TTrigger"></typeparam>
    /// <typeparam name="TState"></typeparam>
    internal sealed class PermitIfTriggerBehaviour<TTrigger, TState> : BaseTriggerBehaviour<TTrigger, TState>
        where TTrigger : struct
        where TState : struct
    {
        private readonly Func<Task<bool>> _condition;

        /// <summary>
        /// Initializes a new instance of a <see cref="PermitIfTriggerBehaviour{TTrigger, TState}"/> class.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="trigger"></param>
        /// <param name="target"></param>
        /// <param name="condition"></param>
        public PermitIfTriggerBehaviour(TState source,
                                        TTrigger trigger,
                                        TState target,
                                        Func<bool> condition)
            : base(source, trigger, target)
        {
            _condition = () => Task.FromResult(condition());
        }

        /// <summary>
        /// Initializes a new instance of a <see cref="PermitIfTriggerBehaviour{TTrigger, TState}"/> class.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="trigger"></param>
        /// <param name="target"></param>
        /// <param name="condition"></param>
        public PermitIfTriggerBehaviour(TState source,
                                        TTrigger trigger,
                                        TState target,
                                        Func<Task<bool>> condition)
            : base(source, trigger, target)
        {
            _condition = condition;
        }

        /// <inheritdoc/>
        public override Task<bool> Condition() => _condition();
    }
}