using System.Threading.Tasks;

namespace AsyncStateMachine.Behaviours
{
    /// <summary>
    /// Base interface for all common operations exposed by a triggers behavior.
    /// </summary>
    /// <typeparam name="TTrigger">Type of trigger.</typeparam>
    /// <typeparam name="TState">Type of state.</typeparam>
    internal interface ITriggerBehaviour<TTrigger, TState>
        where TTrigger : struct
        where TState : struct
    {
        /// <summary>
        /// The source state of the transition.
        /// </summary>
        TState SourceState { get; }

        /// <summary>
        /// The destination state of the transition.
        /// </summary>
        TState TargetState { get; }

        /// <summary>
        /// The trigger.
        /// </summary>
        TTrigger Trigger { get; }

        /// <summary>
        /// Condition if trigger can be applied.
        /// </summary>
        /// <returns><c>True</c> if applicable, otherwise <c>False</c>.</returns>
        Task<bool> Condition();
    }
}