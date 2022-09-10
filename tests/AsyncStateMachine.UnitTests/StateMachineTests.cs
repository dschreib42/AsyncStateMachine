using AsyncStateMachine.Callbacks;
using AsyncStateMachine.Contracts;
using Moq;
using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Xunit;

namespace AsyncStateMachine.UnitTests
{
    public class StateMachineTests
    {
        private readonly Mock<ISubject<Transition<Trigger, State>>> _subject;
        private readonly Mock<ICallbackExecutor> _executor;
        private readonly Mock<ICallbackFilter> _filter;
        private readonly StateMachineConfiguration<Trigger, State> _config;
        private readonly StateMachine<Trigger, State> _sm;

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
        /// Initializes a new instance of a <see cref="StateMachineTests"/> class.
        /// </summary>
        public StateMachineTests()
        {
            _subject = new Mock<ISubject<Transition<Trigger, State>>>();
            _executor = new Mock<ICallbackExecutor>();
            _filter = new Mock<ICallbackFilter>();
            _config = new StateMachineConfiguration<Trigger, State>(State.A);

            _sm = new StateMachine<Trigger, State>(_config, _subject.Object, _filter.Object, _executor.Object);
        }

        [Fact]
        public void Constructor_NullConfiguration_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new StateMachine<Trigger, State>(null,
                                                                                        Mock.Of<ISubject<Transition<Trigger, State>>>(),
                                                                                        Mock.Of<ICallbackFilter>(),
                                                                                        Mock.Of<ICallbackExecutor>()));
        }

        [Fact]
        public void Constructor_NullSubject_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new StateMachine<Trigger, State>(_config,
                                                                                        null,
                                                                                        Mock.Of<ICallbackFilter>(),
                                                                                        Mock.Of<ICallbackExecutor>()));
        }

        [Fact]
        public void Constructor_NullFilter_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new StateMachine<Trigger, State>(_config,
                                                                                        Mock.Of<ISubject<Transition<Trigger, State>>>(),
                                                                                        null,
                                                                                        Mock.Of<ICallbackExecutor>()));
        }

        [Fact]
        public void Constructor_NullExecutor_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new StateMachine<Trigger, State>(_config,
                                                                                        Mock.Of<ISubject<Transition<Trigger, State>>>(),
                                                                                        Mock.Of<ICallbackFilter>(),
                                                                                        null));
        }

        [Fact]
        public async Task InitializeAsync_InitialState_Applied()
        {
            // Arrange
            _config.Configure(State.A);
            var sm = new StateMachine<Trigger, State>(_config,
                                                      Mock.Of<ISubject<Transition<Trigger, State>>>(),
                                                      Mock.Of<ICallbackFilter>(),
                                                      Mock.Of<ICallbackExecutor>());

            // Act
            await sm.InitializeAsync();

            // Assert
            Assert.Equal(_config.InitialState, sm.CurrentState);
        }

        [Theory]
        [InlineData(State.A)]
        [InlineData(State.B)]
        public async Task InitializeAsync_ValidState_CorrectState(State expectedState)
        {
            // Arrange
            _config.Configure(State.A);
            _config.Configure(State.B);

            // Act
            await _sm.InitializeAsync(expectedState);

            // Assert
            Assert.Equal(expectedState, _sm.CurrentState);
        }

        [Fact]
        public async Task CanFireAsync_InvalidTrigger_False()
        {
            // Arrange
            _config.Configure(State.A)
                .PermitReentry(Trigger.a);
            await _sm.InitializeAsync();

            // Act
            var result = await _sm.CanFireAsync(Trigger.b);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CanFireAsync_ValidTrigger_True()
        {
            // Arrange
            _config.Configure(State.A)
                .PermitReentry(Trigger.a);
            await _sm.InitializeAsync();

            // Act
            var result = await _sm.CanFireAsync(Trigger.a);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public Task FireAsync_InvalidTrigger_Throws()
        {
            // Arrange
            var result = _config.Configure(State.A);

            // Act & Assert
            return Assert.ThrowsAsync<InvalidOperationException>(() => _sm.FireAsync(Trigger.a));
        }

        [Fact]
        public async Task FireAsync_Reentry_NoStateChange()
        {
            // Arrange
            _config.Configure(State.A)
                .PermitReentry(Trigger.a);
            await _sm.InitializeAsync();

            // Act
            await _sm.FireAsync(Trigger.a);

            // Assert
            Assert.Equal(State.A, _sm.CurrentState);
        }

        [Fact]
        public async Task FireAsync_Reentry_ObservableTriggered()
        {
            // Arrange
            _config.Configure(State.A)
                .PermitReentry(Trigger.a);
            await _sm.InitializeAsync();
            _subject.Invocations.Clear();

            // Act
            await _sm.FireAsync(Trigger.a);

            // Assert
            _subject.Verify(m => m.OnNext(It.IsAny<Transition<Trigger, State>>()), Times.Once);
        }

        [Fact]
        public async Task FireAsync_Ignore_ObservableNotTriggered()
        {
            // Arrange
            _config.Configure(State.A)
                .Ignore(Trigger.a);
            await _sm.InitializeAsync();
            _subject.Invocations.Clear();

            // Act
            await _sm.FireAsync(Trigger.a);

            // Assert
            _subject.Verify(m => m.OnNext(It.IsAny<Transition<Trigger, State>>()), Times.Never);
        }

        [Fact]
        public async Task FireAsync_Ignore_NoStateChange()
        {
            // Arrange
            _config.Configure(State.A)
                .Ignore(Trigger.a);
            await _sm.InitializeAsync();

            // Act
            await _sm.FireAsync(Trigger.a);

            // Assert
            Assert.Equal(State.A, _sm.CurrentState);
        }

        [Fact]
        public Task FireAsync_Uninitialized_Throws()
        {
            // Arrange
            var sm = new StateMachine<Trigger, State>(_config);

            // Act & Assert
            return Assert.ThrowsAsync<InvalidOperationException>(() => sm.FireAsync(Trigger.a));
        }

        [Fact]
        public async Task FireAsync_Reentry_ExecuteCalled()
        {
            // Arrange
            _config.Configure(State.A)
                .PermitReentry(Trigger.a);
            await _sm.InitializeAsync();
            _executor.Invocations.Clear();

            // Act
            await _sm.FireAsync(Trigger.a);

            // Assert
            _executor.Verify(m => m.ExecuteAsync(It.IsAny<IEnumerable<ICallback>>(),
                                                 It.IsAny<object>()),
                                                 Times.Exactly(2));
        }

        [Fact]
        public async Task FireAsync_Ignored_ExecuteNotCalled()
        {
            // Arrange
            _config.Configure(State.A)
                .Ignore(Trigger.a);
            await _sm.InitializeAsync();
            _executor.Invocations.Clear();

            // Act
            await _sm.FireAsync(Trigger.a);

            // Assert
            _executor.Verify(m => m.ExecuteAsync(It.IsAny<IEnumerable<ICallback>>(),
                                                 It.IsAny<object>()),
                                                 Times.Never);
        }

        [Fact]
        public async Task FireAsync_Permit_ExecuteCalled()
        {
            // Arrange
            _config.Configure(State.A)
                .Permit(Trigger.b, State.B);
            _config.Configure(State.B);
            await _sm.InitializeAsync();
            _executor.Invocations.Clear();

            // Act
            await _sm.FireAsync(Trigger.b);

            // Assert
            _executor.Verify(m => m.ExecuteAsync(It.IsAny<IEnumerable<ICallback>>(),
                                                 It.IsAny<object>()),
                                                 Times.Exactly(2));
        }

        [Fact]
        public async Task FireAsync_Permit_ObservableTriggered()
        {
            // Arrange
            _config.Configure(State.A)
                .Permit(Trigger.b, State.B);
            _config.Configure(State.B);
            await _sm.InitializeAsync();
            _subject.Invocations.Clear();

            // Act
            await _sm.FireAsync(Trigger.b);

            // Assert
            _subject.Verify(m => m.OnNext(It.IsAny<Transition<Trigger, State>>()), Times.Once);
        }

        [Fact]
        public async Task FireAsync_PermitIf_ExecuteCalled()
        {
            // Arrange
            _config.Configure(State.A)
                .PermitIf(Trigger.b, State.B, () => true);
            _config.Configure(State.B);
            await _sm.InitializeAsync();
            _executor.Invocations.Clear();

            // Act
            await _sm.FireAsync(Trigger.b);

            // Assert
            _executor.Verify(m => m.ExecuteAsync(It.IsAny<IEnumerable<ICallback>>(),
                                                 It.IsAny<object>()),
                                                 Times.Exactly(2));
        }

        [Fact]
        public async Task FireAsync_PermitIf_ObservableTriggered()
        {
            // Arrange
            _config.Configure(State.A)
                .PermitIf(Trigger.b, State.B, () => true);
            _config.Configure(State.B);
            await _sm.InitializeAsync();
            _subject.Invocations.Clear();

            // Act
            await _sm.FireAsync(Trigger.b);

            // Assert
            _subject.Verify(m => m.OnNext(It.IsAny<Transition<Trigger, State>>()), Times.Once);
        }

        [Theory]
        [InlineData("foo")]
        public async Task FireAsync_Permit_ParameterPassedToExecute(string parameter)
        {
            // Arrange
            _config.Configure(State.A)
                .Permit(Trigger.b, State.B);
            _config.Configure(State.B)
                .OnEntry<string>(s => { });
            await _sm.InitializeAsync();
            _executor.Invocations.Clear();

            // Act
            await _sm.FireAsync(Trigger.b, parameter);

            // Assert
            _executor.Verify(m => m.ExecuteAsync(It.IsAny<IEnumerable<ICallback>>(), parameter),
                                                 Times.Once);
        }

        [Theory]
        [InlineData("foo")]
        public async Task FireAsync_PermitIf_ParameterPassedToExecute(string parameter)
        {
            // Arrange
            _config.Configure(State.A)
                .PermitIf(Trigger.b, State.B, () => true);
            _config.Configure(State.B)
                .OnEntry<string>(s => { });
            await _sm.InitializeAsync();
            _executor.Invocations.Clear();

            // Act
            await _sm.FireAsync(Trigger.b, parameter);

            // Assert
            _executor.Verify(m => m.ExecuteAsync(It.IsAny<IEnumerable<ICallback>>(), parameter), Times.Once);
        }

        [Fact]
        public async Task FireAsync_PermitIf_Throws()
        {
            // Arrange
            _config.Configure(State.A)
                .PermitIf(Trigger.b, State.B, () => false);
            _config.Configure(State.B)
                .OnEntry<string>(s => { });
            await _sm.InitializeAsync();
            _executor.Invocations.Clear();

            // Act
            var exception = await Record.ExceptionAsync(() => _sm.FireAsync(Trigger.b, "foo"));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidOperationException>(exception);
        }

        [Fact]
        public async Task ResetAsync_InitialState_Restored()
        {
            // Arrange
            _config.Configure(State.A)
                .Permit(Trigger.b, State.B);
            _config.Configure(State.B);
            await _sm.InitializeAsync();
            await _sm.FireAsync(Trigger.b);

            // Act
            await _sm.ResetAsync();

            // Assert
            Assert.Equal(_config.InitialState, _sm.CurrentState);
        }

        [Theory]
        [InlineData(State.A, State.A, true)]
        [InlineData(State.A, State.B, false)]
        [InlineData(State.A, State.C, false)]
        [InlineData(State.B, State.A, false)]
        [InlineData(State.B, State.B, true)]
        [InlineData(State.B, State.C, false)]
        [InlineData(State.C, State.A, false)]
        [InlineData(State.C, State.B, true)]
        [InlineData(State.C, State.C, true)]
        public async Task InStateAsync_StateCombinations_Works(State current, State test, bool expected)
        {
            // Assert
            _config.Configure(State.A);
            _config.Configure(State.B);
            _config.Configure(State.C)
                .SubstateOf(State.B);
            await _sm.InitializeAsync(current);

            // Act
            var result = await _sm.InStateAsync(test);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(State.A, State.A, true)]
        [InlineData(State.A, State.B, false)]
        [InlineData(State.A, State.C, false)]
        [InlineData(State.B, State.A, true)]
        [InlineData(State.B, State.B, true)]
        [InlineData(State.B, State.C, false)]
        [InlineData(State.C, State.A, true)]
        [InlineData(State.C, State.B, true)]
        [InlineData(State.C, State.C, true)]
        public async Task InStateAsync_StateHierarchie_Works(State current, State test, bool expected)
        {
            // Assert
            _config.Configure(State.A);
            _config.Configure(State.B)
                .SubstateOf(State.A);
            _config.Configure(State.C)
                .SubstateOf(State.B);
            await _sm.InitializeAsync(current);

            // Act
            var result = await _sm.InStateAsync(test);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(State.A, State.A, true)]
        [InlineData(State.A, State.B, false)]
        [InlineData(State.A, State.C, false)]
        [InlineData(State.A, State.D, false)]
        [InlineData(State.B, State.A, true)]
        [InlineData(State.B, State.B, true)]
        [InlineData(State.B, State.C, false)]
        [InlineData(State.B, State.D, false)]
        [InlineData(State.C, State.A, false)]
        [InlineData(State.C, State.B, false)]
        [InlineData(State.C, State.C, true)]
        [InlineData(State.C, State.D, false)]
        [InlineData(State.D, State.A, false)]
        [InlineData(State.D, State.B, false)]
        [InlineData(State.D, State.C, true)]
        [InlineData(State.D, State.D, true)]
        public async Task InStateAsync_TwoStateHierarchies_Works(State current, State test, bool expected)
        {
            // Assert
            _config.Configure(State.A);
            _config.Configure(State.B)
                .SubstateOf(State.A);
            _config.Configure(State.C);
            _config.Configure(State.D)
                .SubstateOf(State.C);

            await _sm.InitializeAsync(current);

            // Act
            var result = await _sm.InStateAsync(test);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(State.A, State.A, true)]
        [InlineData(State.A, State.B, true)]
        [InlineData(State.B, State.A, true)]
        [InlineData(State.B, State.B, true)]
        [InlineData(State.A, State.C, false)]
        [InlineData(State.A, State.D, false)]
        public async Task InStateAsync_CircularHierarchie_Works(State current, State test, bool expected)
        {
            // Assert
            _config.Configure(State.A)
                .SubstateOf(State.B);
            _config.Configure(State.B)
                .SubstateOf(State.A);

            await _sm.InitializeAsync(current);

            // Act
            var result = await _sm.InStateAsync(test);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Dispose_DoesNot_Throw()
        {
            // Act
            var exception = Record.Exception(() => _sm.Dispose());

            // Assert
            Assert.Null(exception);
        }
    }
}