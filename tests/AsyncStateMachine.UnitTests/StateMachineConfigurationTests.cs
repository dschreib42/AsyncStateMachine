using System;
using Xunit;

namespace AsyncStateMachine.UnitTests
{
    public sealed class StateMachineConfigurationTests
    {
        private readonly StateMachineConfiguration<Trigger, State> _config;

        // Must be `public`, otherwise it's not possible to create a subject mock.
        public enum State
        {
            A,
            B,
            C,
            D,
        }

        // Must be `public`, otherwise it's not possible to create a subject mock.
        public enum Trigger
        {
            a,
            b,
        }

        /// <summary>
        /// Initializes a new instance of a <see cref="StateMachineConfigurationTests"/> class.
        /// </summary>
        public StateMachineConfigurationTests()
        {
            _config = new StateMachineConfiguration<Trigger, State>(State.A);
        }

        [Fact]
        public void Configure_Returns_NotNull()
        {
            // Act
            var result = _config.Configure(State.A);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void Configure_DuplicateState_Throws()
        {
            // Arrange
            _config.Configure(State.B);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _config.Configure(State.B));
        }

        [Fact]
        public void Configure_DuplicateSubstateOf_Throws()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(
                () => _config.Configure(State.A).SubstateOf(State.B).SubstateOf(State.B));
        }

        [Fact]
        public void Configure_DuplicatePermit_Throws()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(
                () => _config.Configure(State.A).Permit(Trigger.a, State.B).Permit(Trigger.a, State.B));
        }

        [Fact]
        public void Configure_PermitAndPermitIf_Throws()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(
                () => _config.Configure(State.A).Permit(Trigger.a, State.B).PermitIf(Trigger.a, State.B, () => true));
        }

        [Fact]
        public void Configure_DuplicatePermitIf_Throws()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(
                () => _config.Configure(State.A).PermitIf(Trigger.a, State.B, () => false).PermitIf(Trigger.a, State.B, () => true));
        }

        [Fact]
        public void Validate_InitialStateNotConfigured_Throws()
        {
            // Act & Assert
            Assert.Throws<Exception>(() => _config.Validate());
        }
    }
}