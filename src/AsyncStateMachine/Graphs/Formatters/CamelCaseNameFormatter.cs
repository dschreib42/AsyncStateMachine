using AsyncStateMachine.Contracts;
using System.Text.RegularExpressions;

namespace AsyncStateMachine.Graphs.Formatters
{
    /// <summary>
    /// Implements a <see cref="INameFormatter"/> supporting Camel-Case notation.
    /// </summary>
    internal sealed class CamelCaseNameFormatter : INameFormatter
    {
        private readonly Regex _regex;

        /// <summary>
        /// Initializes a new instance of a <see cref="CamelCaseNameFormatter"/> class.
        /// </summary>
        public CamelCaseNameFormatter()
        {
            _regex = new Regex("([a-z|A-Z][A-Z])");
        }

        /// <inheritdoc/>
        public string FormatName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            while (true)
            {
                var match = _regex.Match(name);
                if (!match.Success)
                    break;

                name = name.Replace(match.Value, match.Value.Insert(1, " "));
            }

            return name.Replace('_', '-');
        }
    }
}