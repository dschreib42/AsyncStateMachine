using System;
using System.Threading.Tasks;

namespace AsyncStateMachine.Callbacks
{
    /// <summary>
    /// Implementation of a <see cref="ICallback"/> for parameterless actions.
    /// </summary>
    internal sealed class ActionCallback : ICallback
    {
        private readonly Action _action;

        /// <summary>
        /// Initializes a instance of a <see cref="ActionCallback"/> class.
        /// </summary>
        /// <param name="action">The action.</param>
        public ActionCallback(Action action)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        /// <inheritdoc/>
        public Task ExecuteAsync(object _)
        {
            _action();

            return Task.CompletedTask;
        }
    }
}