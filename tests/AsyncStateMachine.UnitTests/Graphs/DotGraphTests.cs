using AsyncStateMachine.Graphs;
using System;
using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace AsyncStateMachine.UnitTests.Graphs
{
    [UsesVerify]
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
        public Task Format_GraphOptionAll_Correct()
        {
            // Arrange
            var options = GraphOptions.All;

            // Act
            var result = DotGraph.Format(_configuration, options);

            // Assert
            return Verifier.Verify(result);
        }
    }
}