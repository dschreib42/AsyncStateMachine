using System;
using System.Threading;
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
        /// <param name="cancellationToken">The cancellation token</param>
        Task InitializeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if the state machine is in the given state.
        /// </summary>
        /// <remarks>The given state can be the sub-state or the primary state.</remarks>
        /// <param name="state">The state to test.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns><c>True</c> if state is active, otherwise <c>False</c>.</returns>
        Task<bool> InStateAsync(TState state, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if the state machine is in the given state.
        /// </summary>
        /// <remarks>The given state can be the sub-state or the primary state.</remarks>
        /// <param name="state">The state to test.</param>
        /// <param name="maxDepth">Max. depth of hierarchy.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns><c>True</c> if state is active, otherwise <c>False</c>.</returns>
        Task<bool> InStateAsync(TState state, ushort maxDepth, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates if the specified trigger can be fired.
        /// </summary>
        /// <param name="trigger">The trigger to validate.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns><c>True</c>, if the trigger can be fired, otherwise <c>False</c>.</returns>
        Task<bool> CanFireAsync(TTrigger trigger, CancellationToken cancellationToken = default);

        /// <summary>
        /// Triggers a state change.
        /// </summary>
        /// <param name="trigger">Trigger to use.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        Task FireAsync(TTrigger trigger, CancellationToken cancellationToken = default);

        /// <summary>
        /// Triggers a state change with a parameter.
        /// </summary>
        /// <param name="trigger">Trigger to use.</param>
        /// <param name="parameter">The parameter to use.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        Task FireAsync<TParam>(TTrigger trigger, TParam parameter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resets the state machine to the initial state.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token</param>
        Task ResetAsync(CancellationToken cancellationToken = default);
    }
}