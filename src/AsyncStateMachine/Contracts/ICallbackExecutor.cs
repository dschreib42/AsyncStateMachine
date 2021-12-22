using AsyncStateMachine.Callbacks;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsyncStateMachine.Contracts
{
    /// <summary>
    /// Abstraction of a callback executor.
    /// </summary>
    internal interface ICallbackExecutor
    {
        /// <summary>
        /// Executes the given enumeration of callbacks.
        /// </summary>
        /// <param name="callbacks">Callbacks to execute.</param>
        /// <param name="parameter">Argument to pass into the each callback.</param>
        /// <returns>A task that completes, when all callbacks are invoked.</returns>
        Task ExecuteAsync(IEnumerable<ICallback> callbacks,
                          object parameter = null);
    }
}