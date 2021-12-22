using AsyncStateMachine.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AsyncStateMachine.Graphs
{
    /// <summary>
    /// Plots a <see cref="https://mermaid-js.github.io/mermaid/#/">mermaid</see> compliant flow-chart graph.
    /// </summary>
    public static class MermaidFlowChartGraph
    {
        /// <summary>
        /// Plots a mermaid state graph.
        /// </summary>
        /// <param name="transitions">The enumeration of transitions.</param>
        /// <returns>A mermaid graph.</returns>
        /// <typeparam name="State">The type of state.</typeparam>
        /// <typeparam name="Trigger">The type of trigger.</typeparam>
        public static string Format<TState, TTrigger>(IEnumerable<Transition<TTrigger, TState>> transitions)
            where TState : struct
            where TTrigger : struct
        {
            _ = transitions ?? throw new ArgumentNullException(nameof(transitions));

            var sb = new StringBuilder();

            sb.AppendLine("graph TD");

            var indentation = new string(' ', 4);

            foreach (var transition in transitions.Where(x => !x.IsStartTransition))
            {
                sb.AppendLine($"{indentation}{transition.Source} -->|{transition.Trigger}| {transition.Destination}");
            }

            return sb.ToString();
        }
    }
}