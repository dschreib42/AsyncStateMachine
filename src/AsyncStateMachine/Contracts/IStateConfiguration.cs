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
        /// <inheritdoc/>
        IStateConfiguration<TTrigger, TState> Permit(TTrigger trigger, TState targetState);

        /// <inheritdoc/>
        IStateConfiguration<TTrigger, TState> PermitIf(TTrigger trigger, TState targetState, Func<bool> condition);

        /// <inheritdoc/>
        IStateConfiguration<TTrigger, TState> PermitIf(TTrigger trigger, TState targetState, Func<Task<bool>> condition);

        /// <inheritdoc/>
        IStateConfiguration<TTrigger, TState> Ignore(TTrigger trigger);

        /// <inheritdoc/>
        IStateConfiguration<TTrigger, TState> OnEntry(Action action);

        /// <inheritdoc/>
        IStateConfiguration<TTrigger, TState> OnEntry(Func<Task> func);

        /// <inheritdoc/>
        IStateConfiguration<TTrigger, TState> OnEntry<TParam>(Action<TParam> action);

        /// <inheritdoc/>
        IStateConfiguration<TTrigger, TState> OnEntry<TParam>(Func<TParam, Task> onEntryB);

        /// <inheritdoc/>
        IStateConfiguration<TTrigger, TState> OnExit(Action action);

        /// <inheritdoc/>
        IStateConfiguration<TTrigger, TState> OnExit(Func<Task> func);
    }
}