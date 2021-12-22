using AsyncStateMachine.Callbacks;
using System;
using System.Threading.Tasks;

namespace AsyncStateMachine
{
    /// <summary>
    /// Implementation of a <see cref="ICallback"/> for async functions with a single argument.
    /// </summary>
    internal class FuncWithParamCallback<TParam> : ICallback
    {
        private readonly Func<TParam, Task> _callback;

        /// <summary>
        /// Initializes a instance of a <see cref="FuncWithParamCallback{TParam}"/> class.
        /// </summary>
        /// <param name="callback">The callback function.</param>
        public FuncWithParamCallback(Func<TParam, Task> callback)
        {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        /// <inheritdoc/>
        public Task ExecuteAsync(object p)
        {
            return _callback((TParam)p);
        }
    }
}