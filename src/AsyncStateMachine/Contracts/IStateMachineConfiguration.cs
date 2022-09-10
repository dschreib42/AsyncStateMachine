using System.Collections.Generic;

namespace AsyncStateMachine.Contracts
{
    /// <summary>
    /// Operations exposed by a state machine configuration.
    /// </summary>
    /// <typeparam name="TTrigger">Type of trigger</typeparam>
    /// <typeparam name="TState">Type of state.</typeparam>
    public interface IStateMachineConfiguration<TTrigger, TState>
        where TTrigger : struct
        where TState : struct
    {
        /// <summary>
        /// The initial state of the state machine.
        /// </summary>
        TState InitialState { get; }

        /// <summary>
        /// Retrieve all configured transitions describing the state machine.
        /// </summary>
        IEnumerable<Transition<TTrigger, TState>> Transitions { get; }

        /// <summary>
        /// Configures a state and its triggers/transitions.
        /// </summary>
        /// <param name="state">State to configure.</param>
        /// <returns>An instance of a <see cref="IStateConfiguration{TTrigger, TState}"/>.</returns>
        IStateConfiguration<TTrigger, TState> Configure(TState state);

        /// <summary>
        /// Is configuration valid?
        /// </summary>
        /// <remarks>Throws an exception on error.</remarks>
        void Validate();
    }
}