using System;
using System.Text;

namespace AsyncStateMachine.Graphs
{
    /// <summary>
    /// Generates a <see href="https://mermaid-js.github.io/mermaid/#/">mermaid</see> compliant state graph.
    /// </summary>
    public static class MermaidStateGraph
    {
        /// <summary>
        /// Plots a mermaid state graph.
        /// </summary>
        /// <typeparam name="TState">The type of state.</typeparam>
        /// <typeparam name="TTrigger">The type of trigger.</typeparam>
        /// <param name="configuration">An instance of a <see cref="StateConfiguration{TTrigger, TState}"/>.</param>
        /// <returns>A mermaid graph.</returns>
        public static string Format<TState, TTrigger>(StateMachineConfiguration<TTrigger, TState> configuration)
            where TState : struct
            where TTrigger : struct
        {
            _ = configuration ?? throw new ArgumentNullException(nameof(configuration));

            var sb = new StringBuilder();

            sb.AppendLine("stateDiagram-v2");

            var indentation = new string(' ', 4);

            foreach (var transition in configuration.Transitions)
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