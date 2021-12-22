using System;
using System.Threading.Tasks;

namespace AsyncStateMachine.Callbacks
{
    /// <summary>
    /// Implementation of a <see cref="ICallback"/> for async functions without arguments.
    /// </summary>
    internal class FuncCallback : ICallback
    {
        private readonly Func<Task> _callback;

        /// <summary>
        /// Initializes a instance of a <see cref="FuncCallback"/> class.
        /// </summary>
        /// <param name="callback">The callback function.</param>
        public FuncCallback(Func<Task> callback)
        {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        /// <inheritdoc/>
        public Task ExecuteAsync(object _)
        {
            return _callback();
        }
    }
}