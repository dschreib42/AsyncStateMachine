using AsyncStateMachine.Graphs;
using System;
using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace AsyncStateMachine.UnitTests.Graphs
{
    public class DotGraphTests
    {
        private readonly StateMachineConfiguration<Trigger, State> _configuration;

        private enum State
        {
            NewBug,
            Rejected,
            Assigned,
            Deferred,
            Closed
        }

        private enum Trigger
        {
            assign,
            reject,
            defer,
            close
        }

        /// <summary>
        /// Constructor for the <see cref="DotGraphTests"/> class.
        /// </summary>
        public DotGraphTests()
        {
            _configuration = new StateMachineConfiguration<Trigger, State>(State.NewBug);

            _configuration.Configure(State.NewBug)
                .Permit(Trigger.assign, State.Assigned)
                .Permit(Trigger.reject, State.Rejected);
            _configuration.Configure(State.Rejected);
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
            Assert.Throws<ArgumentNullException>(() => DotGraph.Format<Trigger, State>(null));
        }

        [Fact]
        public void Format_CamelCaseAndTerminationNodes_TerminationNodeUsesFormattedName()
        {
            // Arrange
            var configuration = new StateMachineConfiguration<Trigger, State>(State.NewBug);
            configuration.Configure(State.NewBug).Permit(Trigger.reject, State.Rejected);
            configuration.Configure(State.Rejected);

            // Act
            var dot = DotGraph.Format(configuration, GraphOptions.CamelCaseFormatting | GraphOptions.MarkTerminationNodes);

            // Assert
            Assert.Contains("\"Rejected\" [peripheries = 2];", dot);

            configuration = new StateMachineConfiguration<Trigger, State>(State.Assigned);
            configuration.Configure(State.Assigned).Permit(Trigger.close, State.NewBug);
            configuration.Configure(State.NewBug);
            dot = DotGraph.Format(configuration, GraphOptions.CamelCaseFormatting | GraphOptions.MarkTerminationNodes);
            Assert.Contains("\"New Bug\" [peripheries = 2];", dot);
            Assert.DoesNotContain("\"NewBug\"", dot);
        }

        [Fact]
        public Task Format_GraphOptionNone_Correct()
        {
            // Arrange
            var options = GraphOptions.CreateStartTransition | GraphOptions.MarkTerminationNodes;

            // Act
            var result = DotGraph.Format(_configuration, options);

            // Assert
            return Verifier.Verify(result);
        }

        [Fact]
        public Task Format_GraphOptionCreateStartTransistions_Correct()
        {
            // Arrange
            var options = GraphOptions.CreateStartTransition;

            // Act
            var result = DotGraph.Format(_configuration, options);

            // Assert
            return Verifier.Verify(result);
        }

        [Fact]
        public Task Format_GraphOptionMarkTerminationNodes_Correct()
        {
            // Arrange
            var options = GraphOptions.MarkTerminationNodes;

            // Act
            var result = DotGraph.Format(_configuration, options);

            // Assert
            return Verifier.Verify(result);
        }

        [Fact]
        public Task Format_GraphOptionCamelCaseFormatting_Correct()
        {
            // Arrange
            var options = GraphOptions.CamelCaseFormatting;

            // Act
            var result = DotGraph.Format(_configuration, options);

            // Assert
            return Verifier.Verify(result);
        }

        [Fact]
        public Task Format_GraphOptionDefault_Correct()
        {
            // Arrange
            var options = GraphOptions.Default;

            // Act
            var result = DotGraph.Format(_configuration, options);

            // Assert
            return Verifier.Verify(result);
        }
    }
}