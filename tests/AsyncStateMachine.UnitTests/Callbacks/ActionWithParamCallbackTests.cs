using AsyncStateMachine.Callbacks;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AsyncStateMachine.UnitTests.Callbacks
{
    public class ActionWithParamCallbackTests
    {
        [Fact]
        public void Constructor_NullAction_Throws()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ActionWithParamCallback<string>(null));
        }

        [Fact]
        public async Task ExecuteAsync_WasCalled()
        {
            // Arrange
            var called = 0;
            var instance = new ActionWithParamCallback<int>(_ => called++);

            // Act
            await instance.ExecuteAsync(42);

            // Assert
            Assert.Equal(1, called);
        }

        [Fact]
        public async Task ExecuteAsync_WithArgument_ArgumentWasPassed()
        {
            // Arrange
            const int sentArg = 42;
            var receivedArg = 0;
            var instance = new ActionWithParamCallback<int>(i => receivedArg = i);

            // Act
            await instance.ExecuteAsync(sentArg);

            // Assert
            Assert.Equal(sentArg, receivedArg);
        }
    }
}