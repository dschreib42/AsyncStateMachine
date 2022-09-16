using System;
using System.Linq;
using System.Text;

namespace AsyncStateMachine.Graphs
{
    /// <summary>
    /// Generates a <see href="https://mermaid-js.github.io/mermaid/#/">mermaid</see> compliant flow-chart graph.
    /// </summary>
    public static class MermaidFlowChartGraph
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

            sb.AppendLine("graph TD");

            var indentation = new string(' ', 4);

            foreach (var transition in configuration.Transitions.Where(x => !x.IsStartTransition))
            {
                sb.AppendLine($"{indentation}{transition.Source} -->|{transition.Trigger}| {transition.Destination}");
            }

            return sb.ToString();
        }
    }
}