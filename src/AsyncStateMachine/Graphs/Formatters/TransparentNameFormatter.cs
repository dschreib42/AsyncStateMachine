using AsyncStateMachine.Contracts;

namespace AsyncStateMachine.Graphs.Formatters
{
    /// <summary>
    /// Implements a <see cref="INameFormatter"/> doing nothing.
    /// </summary>
    internal class TransparentNameFormatter : INameFormatter
    {
        /// <inheritdoc/>
        public string FormatName(string name) => name;
    }
}