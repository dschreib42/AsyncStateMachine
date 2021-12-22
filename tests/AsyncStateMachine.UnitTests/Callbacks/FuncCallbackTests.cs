using AsyncStateMachine.Callbacks;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AsyncStateMachine.UnitTests.Callbacks
{
    public class FuncCallbackTests
    {
        [Fact]
        public void Constructor_NullAction_Throws()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new FuncCallback(null));
        }

        [Fact]
        public async Task ExecuteAsync_WasCalled()
        {
            // Arrange
            var called = 0;
            var instance = new FuncCallback(() => { called++; return Task.CompletedTask; });

            // Act
            await instance.ExecuteAsync("foo");

            // Assert
            Assert.Equal(1, called);
        }
    }
}