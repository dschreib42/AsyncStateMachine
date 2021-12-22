using AsyncStateMachine.Callbacks;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace AsyncStateMachine.UnitTests
{
    public class CallbackExecutorTests
    {
        private readonly CallbackExecutor _exec;

        /// <summary>
        /// Initializes a new instance of a <see cref="CallbackExecutorTests"/> class.
        /// </summary>
        public CallbackExecutorTests()
        {
            _exec = new CallbackExecutor();
        }

        [Fact]
        public Task ExecuteAsync_NullCallbacks_Throws()
        {
            // Act & Assert
            return Assert.ThrowsAsync<ArgumentNullException>(
                () => _exec.ExecuteAsync(null, 42));
        }

        [Theory]
        [InlineData(1, null)]
        [InlineData(2, "foo")]
        [InlineData(10, 42)]
        public async Task ExecuteAsync_Callbacks_AllInvoked(ushort numCallbacks, object parameter)
        {
            // Arrange
            var callbacks = Enumerable
                .Range(0, numCallbacks)
                .Select(_ => new Mock<ICallback>())
                .ToList();

            // Act
            await _exec.ExecuteAsync(callbacks.Select(x => x.Object), parameter);

            // Assert
            foreach (var callback in callbacks)
            {
                callback.Verify(m => m.ExecuteAsync(parameter), Times.Once());
            }
        }
    }
}