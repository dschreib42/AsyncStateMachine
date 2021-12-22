using AsyncStateMachine.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace AsyncStateMachine.Graphs
{
    /// <summary>
    /// Plots a <see href="http://www.graphviz.org/">Graphviz</see> compliant graph.
    /// </summary>
    public static class DotGraph
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
            var indentation = new string(' ', 4);
            var startNode = "START";

            sb.AppendLine("digraph G {");
            sb.AppendLine($"{indentation}rankdir = LR;");
            sb.AppendLine($"{indentation}size = \"8,5\";");

            foreach (var transition in transitions)
            {
                if (transition.Source is null)
                {
                    sb.AppendLine($"{indentation}\"{startNode}\" [shape = point];");
                    sb.AppendLine($"{indentation}\"{startNode}\" -> \"{transition.Destination}\";");
                }
                else
                {
                    sb.AppendLine($"{indentation}\"{transition.Source}\" -> \"{transition.Destination}\" [label = \"{transition.Trigger}\"];");
                }
            }

            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}