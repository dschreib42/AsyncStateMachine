using AsyncStateMachine.Callbacks;
using AsyncStateMachine.Contracts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsyncStateMachine
{
    /// <summary>
    /// Implementation of a <see cref="ICallbackExecutor"/>.
    /// </summary>
    internal sealed class CallbackExecutor : ICallbackExecutor
    {
        /// <inheritdoc/>
        public async Task ExecuteAsync(IEnumerable<ICallback> callbacks,
                                       object parameter = null)
        {
            _ = callbacks ?? throw new ArgumentNullException(nameof(callbacks));

            // run one after the other in registration order; the first failure stops the chain
            foreach (var callback in callbacks)
            {
                await callback.ExecuteAsync(parameter);
            }
        }
    }
}