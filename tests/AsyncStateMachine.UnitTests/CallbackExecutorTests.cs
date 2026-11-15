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

        [Fact]
        public async Task ExecuteAsync_Callbacks_RunSequentiallyInOrder()
        {
            // Arrange
            var log = new System.Collections.Generic.List<int>();
            var callbacks = Enumerable.Range(0, 3).Select(i =>
            {
                var m = new Mock<ICallback>();
                m.Setup(x => x.ExecuteAsync(It.IsAny<object>())).Returns(async () =>
                {
                    log.Add(i * 10);
                    await Task.Delay(20 - i * 5);
                    log.Add(i * 10 + 1);
                });
                return m.Object;
            }).ToList();

            // Act
            await _exec.ExecuteAsync(callbacks);

            // Assert
            Assert.Equal(new[] { 0, 1, 10, 11, 20, 21 }, log);
        }

        [Fact]
        public async Task ExecuteAsync_CallbackThrowsSynchronously_FailsTaskAndSkipsRest()
        {
            // Arrange
            var first = new Mock<ICallback>();
            first.Setup(x => x.ExecuteAsync(It.IsAny<object>())).Throws<InvalidOperationException>();
            var second = new Mock<ICallback>();

            // Act
            var task = _exec.ExecuteAsync(new[] { first.Object, second.Object });

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => task);
            second.Verify(m => m.ExecuteAsync(It.IsAny<object>()), Times.Never());
        }
    }
}