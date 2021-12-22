using AsyncStateMachine.Graphs;
using System;
using Xunit;

namespace AsyncStateMachine.UnitTests.Graphs
{
    public class MermaidStateGraphTests
    {
        public enum Trigger
        {
            a
        }

        public enum State
        {
            A
        }

        [Fact]
        public void Plot_NullTransitions_Throws()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => MermaidStateGraph.Format<Trigger, State>(null));
        }
    }
}