using System;
using System.Threading.Tasks;
using Xunit;

namespace AsyncStateMachine.UnitTests.Callbacks
{
    public class FuncWithParamCallbackTests
    {
        [Fact]
        public void Constructor_NullAction_Throws()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new FuncWithParamCallback<string>(null));
        }

        [Fact]
        public async Task ExecuteAsync_WasCalled()
        {
            // Arrange
            var called = 0;
            var instance = new FuncWithParamCallback<int>(_ => { called++; return Task.CompletedTask; });

            // Act
            await instance.ExecuteAsync(42);

            // Assert
            Assert.Equal(1, called);
        }

        [Fact]
        public async Task ExecuteAsync_WithArgument_ArgumentWasPassed()
        {
            // Arrange
            var sentArg = 42;
            var receivedArg = 0;
            var instance = new FuncWithParamCallback<int>(i => { receivedArg = i; return Task.CompletedTask; });

            // Act
            await instance.ExecuteAsync(sentArg);

            // Assert
            Assert.Equal(sentArg, receivedArg);
        }
    }
}