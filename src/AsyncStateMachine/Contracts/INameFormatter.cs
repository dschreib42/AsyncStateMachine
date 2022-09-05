namespace AsyncStateMachine.Contracts
{
    /// <summary>
    /// Operations exposed by a node/transition name formatter.
    /// </summary>
    internal interface INameFormatter
    {
        /// <summary>
        /// Formats a given name.
        /// </summary>
        /// <param name="name">The input name to format.</param>
        /// <returns>The formatted instance of a <see cref="string"/>.</returns>
        string FormatName(string name);
    }
}