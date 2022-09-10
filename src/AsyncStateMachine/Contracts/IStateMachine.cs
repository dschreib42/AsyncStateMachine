using System;
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
        /// Gets the current state of the state machine.
        /// </summary>
        TState? CurrentState { get; }

        /// <summary>
        /// Initializes the state machine.
        /// </summary>
        /// <param name="state">
        /// If not null the desired state to restore or the initial state defined in the
        /// <see cref="StateConfiguration{TTrigger, TState}"/>.
        /// </param>
        /// <returns>A task that completes, when the state machine is initialized.</returns>
        Task InitializeAsync(TState? state = null);

        /// <summary>
        /// Checks if the state machine is in the given state.
        /// </summary>
        /// <remarks>The given state can be the sub-state or the primary state.</remarks>
        /// <param name="state">The state to test.</param>
        /// <param name="maxDepth">Max. depth of hierarchy.</param>
        /// <returns><c>True</c> if state is active, otherwise <c>False</c>.</returns>
        Task<bool> InStateAsync(TState state, ushort maxDepth = 5);

        /// <summary>
        /// Validates if the specified trigger can be fired.
        /// </summary>
        /// <param name="trigger">The trigger to validate.</param>
        /// <returns><c>True</c>, if the trigger can be fired, otherwise <c>False</c>.</returns>
        Task<bool> CanFireAsync(TTrigger trigger);

        /// <summary>
        /// Triggers a state change.
        /// </summary>
        /// <param name="trigger">Trigger to use.</param>
        /// <returns>A task that completes, when the state machine has changed state.</returns>
        Task FireAsync(TTrigger trigger);

        /// <summary>
        /// Triggers a state change with a parameter.
        /// </summary>
        /// <param name="trigger">Trigger to use.</param>
        /// <param name="parameter">The parameter to use.</param>
        /// <returns>A task that completes, when the state machine has changed state.</returns>
        Task FireAsync<TParam>(TTrigger trigger, TParam parameter);

        /// <summary>
        /// Resets the state machine to the initial state.
        /// </summary>
        /// <returns>A task that completes, when the state machine has been reseted.</returns>
        Task ResetAsync();
    }
}