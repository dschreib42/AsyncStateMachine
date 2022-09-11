using AsyncStateMachine.Graphs;
using System;
using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace AsyncStateMachine.UnitTests.Graphs
{
    [UsesVerify]
    public class MermaidFlowChartGraphTests
    {
        private readonly StateMachineConfiguration<Trigger, State> _configuration;

        private enum State
        {
            Open,
            Assigned,
            Deferred,
            Closed
        }

        private enum Trigger
        {
            assign,
            defer,
            close
        }

        /// <summary>
        /// Constructor for the <see cref="MermaidFlowChartGraphTests"/> class.
        /// </summary>
        public MermaidFlowChartGraphTests()
        {
            _configuration = new StateMachineConfiguration<Trigger, State>(State.Open);

            _configuration.Configure(State.Open)
                .Permit(Trigger.assign, State.Assigned);
            _configuration.Configure(State.Assigned)
                .PermitReentry(Trigger.assign)
                .Permit(Trigger.close, State.Closed)
                .Permit(Trigger.defer, State.Deferred);
            _configuration.Configure(State.Deferred)
                .Permit(Trigger.assign, State.Assigned);
            _configuration.Configure(State.Closed);
        }

        [Fact]
        public void Format_NullConfiguration_Throws()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => MermaidFlowChartGraph.Format<Trigger, State>(null));
        }

        [Fact]
        public Task Format_Graph_Correct()
        {
            // Act
            var result = MermaidFlowChartGraph.Format(_configuration);

            // Assert
            return Verifier.Verify(result);
        }
    }
}