using System;
using System.Threading.Tasks;

namespace AsyncStateMachine.Contracts
{
    /// <summary>
    /// Operations exposed to configure a state.
    /// </summary>
    /// <typeparam name="TTrigger">Type of trigger.</typeparam>
    /// <typeparam name="TState">Type of state.</typeparam>
    public interface IStateConfiguration<TTrigger, TState>
        where TTrigger : struct
        where TState : struct
    {
        /// <summary>
        /// Defines a transition from the current state to the given targetState when the given trigger is invoked.
        /// </summary>
        /// <param name="trigger">The trigger causing the transition.</param>
        /// <param name="targetState">The new state when the trigger was invoked.</param>
        /// <returns>An instance of a <see cref="IStateConfiguration{TTrigger, TState}"/>.</returns>
        IStateConfiguration<TTrigger, TState> Permit(TTrigger trigger, TState targetState);

        /// <summary>
        /// Defines a conditional transition from the current state to the given targetState when the given trigger is
        /// invoked and the condition is fulfilled.
        /// </summary>
        /// <param name="trigger">The trigger causing the transition.</param>
        /// <param name="targetState">The new state when the trigger was invoked.</param>
        /// <param name="condition">On <c>True</c> the transition can be applied, otherwise <c>False</c>.</param>
        /// <returns>An instance of a <see cref="IStateConfiguration{TTrigger, TState}"/>.</returns>
        IStateConfiguration<TTrigger, TState> PermitIf(TTrigger trigger, TState targetState, Func<bool> condition);

        /// <summary>
        /// Defines a conditional transition from the current state to the given targetState when the given trigger is
        /// invoked and the asynchronous condition is fulfilled.
        /// </summary>
        /// <param name="trigger">The trigger causing the transition.</param>
        /// <param name="targetState">The new state when the trigger was invoked.</param>
        /// <param name="condition">On <c>True</c> the transition can be applied, otherwise <c>False</c>.</param>
        /// <returns>An instance of a <see cref="IStateConfiguration{TTrigger, TState}"/>.</returns>
        IStateConfiguration<TTrigger, TState> PermitIf(TTrigger trigger, TState targetState, Func<Task<bool>> condition);

        /// <summary>
        /// Defines a transition from the current state to the same state with invoking the entry/exit handlers.
        /// </summary>
        /// <remarks>No state change will be applied.</remarks>
        /// <param name="trigger">The trigger causing the transition.</param>
        /// <returns>An instance of a <see cref="IStateConfiguration{TTrigger, TState}"/>.</returns>
        IStateConfiguration<TTrigger, TState> PermitReentry(TTrigger trigger);

        /// <summary>
        /// Defines a transition from the current state to the same state without invoking the entry/exit handlers.
        /// </summary>
        /// <remarks>No state change will be applied.</remarks>
        /// <param name="trigger">The trigger causing the transition.</param>
        /// <returns>An instance of a <see cref="IStateConfiguration{TTrigger, TState}"/>.</returns>
        IStateConfiguration<TTrigger, TState> Ignore(TTrigger trigger);

        /// <summary>
        /// Registers an action to be called, whenever the state is reached.
        /// </summary>
        /// <param name="action">The action to invoke when the state was reached.</param>
        /// <returns>An instance of a <see cref="IStateConfiguration{TTrigger, TState}"/>.</returns>
        IStateConfiguration<TTrigger, TState> OnEntry(Action action);

        /// <summary>
        /// Registers a asynchronous function to be called, whenever the state is reached.
        /// </summary>
        /// <param name="func">The function to invoke when the state was reached.</param>
        /// <returns>An instance of a <see cref="IStateConfiguration{TTrigger, TState}"/>.</returns>
        IStateConfiguration<TTrigger, TState> OnEntry(Func<Task> func);

        /// <summary>
        /// Registers a parameterized action to be called, whenever the state is reached.
        /// </summary>
        /// <param name="action">The action to invoke when the state was reached.</param>
        /// <returns>An instance of a <see cref="IStateConfiguration{TTrigger, TState}"/>.</returns>
        IStateConfiguration<TTrigger, TState> OnEntry<TParam>(Action<TParam> action);

        /// <summary>
        /// Registers a parameterized function to be called, whenever the state is reached.
        /// </summary>
        /// <param name="func">The function to invoke when the state was reached.</param>
        /// <returns>An instance of a <see cref="IStateConfiguration{TTrigger, TState}"/>.</returns>
        IStateConfiguration<TTrigger, TState> OnEntry<TParam>(Func<TParam, Task> func);

        /// <summary>
        /// Registers an action to be called, whenever the state is changed.
        /// </summary>
        /// <param name="action">The action to invoke when the state is changed.</param>
        /// <returns>An instance of a <see cref="IStateConfiguration{TTrigger, TState}"/>.</returns>
        IStateConfiguration<TTrigger, TState> OnExit(Action action);

        /// <summary>
        /// Registers an asynchronous function to be called, whenever the state is changed.
        /// </summary>
        /// <param name="func">The function to invoke when the state is changed.</param>
        /// <returns>An instance of a <see cref="IStateConfiguration{TTrigger, TState}"/>.</returns>
        IStateConfiguration<TTrigger, TState> OnExit(Func<Task> func);

        /// <summary>
        /// Marks the current state as substate of the given parentState.
        /// </summary>
        /// <param name="parentState">The parent state of the current state</param>
        /// <returns>An instance of a <see cref="IStateConfiguration{TTrigger, TState}"/>.</returns>
        IStateConfiguration<TTrigger, TState> SubstateOf(TState parentState);
    }
}
