using AsyncStateMachine.Callbacks;
using System;
using System.Collections.Generic;

namespace AsyncStateMachine.Contracts
{
    /// <summary>
    /// Abstraction of a callback filter.
    /// </summary>
    internal interface ICallbackFilter
    {
        /// <summary>
        /// Executes the given available callbacks.
        /// </summary>
        /// <param name="available">Available callbacks.</param>
        /// <param name="predicate">Predicate filtering the available callbacks.</param>
        /// <param name="guard">The guard function to validate the callbacks to be executed.</param>
        /// <returns>A list of callbacks to be executed.</returns>
        IReadOnlyCollection<ICallback> Filter(IReadOnlyCollection<ICallback> available,
                                              Func<ICallback, bool> predicate,
                                              Action<IReadOnlyCollection<ICallback>> guard = null);
    }
}