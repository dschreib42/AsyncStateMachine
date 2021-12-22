using System.Threading.Tasks;

namespace AsyncStateMachine.Callbacks
{
    /// <summary>
    /// The abstract interface for definition the base execution method for
    /// sync/async executions with or without argument.
    /// </summary>
    internal interface ICallback
    {
        /// <summary>
        /// The executes the callback with the given argument.
        /// </summary>
        /// <param name="param">The argument to be passed into the callback.</param>
        /// <returns>A task that completes, when the callback was executed.</returns>
        Task ExecuteAsync(object param);
    }
}