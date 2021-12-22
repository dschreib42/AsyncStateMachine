using AsyncStateMachine.Callbacks;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AsyncStateMachine.UnitTests.Callbacks
{
    public class ActionCallbackTests
    {
        [Fact]
        public void Constructor_NullAction_Throws()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ActionCallback(null));
        }

        [Fact]
        public async Task ExecuteAsync_WasCalled()
        {
            // Arrange
            var called = 0;
            var instance = new ActionCallback(() => called++);

            // Act
            await instance.ExecuteAsync("foo");

            // Assert
            Assert.Equal(1, called);
        }
    }
}