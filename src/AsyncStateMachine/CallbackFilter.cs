using AsyncStateMachine.Callbacks;
using AsyncStateMachine.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AsyncStateMachine
{
    /// <summary>
    /// Implementation of a <see cref="ICallbackFilter"/>.
    /// </summary>
    internal sealed class CallbackFilter : ICallbackFilter
    {
        /// <inheritdoc/>
        public IEnumerable<ICallback> Filter(IEnumerable<ICallback> available,
                                             Func<ICallback, bool> predicate,
                                             Action<IReadOnlyCollection<ICallback>> guard = null)
        {
            _ = available ?? throw new ArgumentNullException(nameof(available));
            _ = predicate ?? throw new ArgumentNullException(nameof(predicate));

            // filter callbacks depending on predicate
            var callbacks = available.Where(predicate).ToArray();

            // verify callbacks and throw exception depending on guard function
            guard?.Invoke(callbacks);

            return callbacks.ToArray();
        }
    }
}