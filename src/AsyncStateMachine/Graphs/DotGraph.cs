using AsyncStateMachine.Contracts;
using AsyncStateMachine.Graphs.Formatters;
using System;
using System.Collections.Generic;
using System.Linq;
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
        /// <param name="configuration">An instance of a <see cref="StateConfiguration{TTrigger, TState}"/>.</param>
        /// <returns>A mermaid graph.</returns>
        /// <typeparam name="TState">The type of state.</typeparam>
        /// <typeparam name="TTrigger">The type of trigger.</typeparam>
        /// <param name="options">Graph generating options.</param>
        public static string Format<TState, TTrigger>(StateMachineConfiguration<TTrigger, TState> configuration,
                                                      GraphOptions options = GraphOptions.None)
            where TState : struct
            where TTrigger : struct
        {
            _ = configuration ?? throw new ArgumentNullException(nameof(configuration));

            var sb = new StringBuilder();
            var indentation = new string(' ', 4);
            var startNode = "START";

            var formatter = options.HasFlag(GraphOptions.CamelCaseFormatting)
                ? (INameFormatter)new CamelCaseNameFormatter()
                : (INameFormatter)new TransparentNameFormatter();

            sb.AppendLine("digraph G {");
            sb.AppendLine($"{indentation}rankdir = LR;");
            sb.AppendLine($"{indentation}size = \"8,5\";");

            foreach (var transition in configuration.Transitions)
            {
                if (transition.Source is null)
                {
                    if (options.HasFlag(GraphOptions.CreateStartTransition))
                    {
                        var source = startNode;
                        var destination = formatter.FormatName(transition.Destination.ToString());

                        sb.AppendLine($"{indentation}\"{source}\" [shape = point];");
                        sb.AppendLine($"{indentation}\"{source}\" -> \"{destination}\";");
                    }
                }
                else
                {
                    var source = transition.Source.HasValue ? formatter.FormatName(transition.Source.Value.ToString()) : null;
                    var destination = formatter.FormatName(transition.Destination.ToString());
                    var trigger = formatter.FormatName(transition.Trigger.ToString());

                    sb.AppendLine($"{indentation}\"{source}\" -> \"{destination}\" [label = \"{trigger}\"];");
                }
            }

            if (options.HasFlag(GraphOptions.MarkTerminationNodes))
            {
                var sourceNodes = new HashSet<TState>(configuration.Transitions.Where(x => x.Source.HasValue).Select(x => x.Source.Value));
                var destNodes = new HashSet<TState>(configuration.Transitions.Select(x => x.Destination));

                foreach (var endNode in destNodes.Where(x => !sourceNodes.Contains(x)))
                {
                    sb.AppendLine($"{indentation}\"{endNode}\" [peripheries = 2];");
                }
            }

            sb.AppendLine("}");

            return sb.ToString();
        }
    }

    [Flags]
    public enum GraphOptions
    {
        /// <summary>
        /// No options selected.
        /// </summary>
        None = 0,

        /// <summary>
        /// CamelCase to word formatting applied.
        /// </summary>
        CamelCaseFormatting = 1,

        /// <summary>
        /// Creates a transition to the start node.
        /// </summary>
        CreateStartTransition = 2,

        /// <summary>
        /// Marks termination nodes.
        /// </summary>
        MarkTerminationNodes = 4,
    }
}