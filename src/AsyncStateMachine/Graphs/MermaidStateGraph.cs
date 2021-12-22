using System;
using System.Collections.Generic;
using System.Text;

namespace AsyncStateMachine.Graphs
{
    /// <summary>
    /// Plots a <see href="https://mermaid-js.github.io/mermaid/#/">mermaid</see> compliant state graph.
    /// </summary>
    public static class MermaidStateGraph
    {
        /// <summary>
        /// Plots a mermaid state graph.
        /// </summary>
        /// <param name="transitions">The enumeration of transitions.</param>
        /// <returns>A mermaid graph.</returns>
        /// <typeparam name="TState">The type of state.</typeparam>
        /// <typeparam name="TTrigger">The type of trigger.</typeparam>
        public static string Format<TState, TTrigger>(IEnumerable<Transition<TTrigger, TState>> transitions)
            where TState : struct
            where TTrigger : struct
        {
            _ = transitions ?? throw new ArgumentNullException(nameof(transitions));

            var sb = new StringBuilder();

            sb.AppendLine("stateDiagram-v2");

            var indentation = new string(' ', 4);

            foreach (var transition in transitions)
            {
                var startState = transition.Source.HasValue
                    ? transition.Source.Value.ToString()
                    : "[*]";

                sb.AppendLine($"{indentation}{startState} --> {transition.Destination}");
            }

            return sb.ToString();
        }
    }
}