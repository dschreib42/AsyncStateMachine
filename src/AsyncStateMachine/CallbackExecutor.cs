using AsyncStateMachine.Callbacks;
using AsyncStateMachine.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AsyncStateMachine
{
    /// <summary>
    /// Implementation of a <see cref="ICallbackExecutor"/>.
    /// </summary>
    internal class CallbackExecutor : ICallbackExecutor
    {
        /// <inheritdoc/>
        public Task ExecuteAsync(IEnumerable<ICallback> callbacks,
                                 object parameter = null)
        {
            _ = callbacks ?? throw new ArgumentNullException(nameof(callbacks));

            return Task.WhenAll(callbacks.Select(x => x.ExecuteAsync(parameter)));
        }
    }
}