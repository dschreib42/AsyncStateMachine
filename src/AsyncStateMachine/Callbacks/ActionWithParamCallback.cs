using System;
using System.Threading.Tasks;

namespace AsyncStateMachine.Callbacks
{
    /// <summary>
    /// Implementation of a <see cref="ICallback"/> for actions with a single argument.
    /// </summary>
    internal class ActionWithParamCallback<TParam> : ICallback
    {
        private readonly Action<TParam> _action;

        /// <summary>
        /// Initializes a instance of a <see cref="ActionWithParamCallback"/> class.
        /// </summary>
        /// <param name="action">The action.</param>
        public ActionWithParamCallback(Action<TParam> action)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        /// <inheritdoc/>
        public Task ExecuteAsync(object p)
        {
            _action((TParam)p);

            return Task.CompletedTask;
        }
    }
}