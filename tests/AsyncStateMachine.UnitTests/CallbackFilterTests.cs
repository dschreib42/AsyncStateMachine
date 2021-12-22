using AsyncStateMachine.Callbacks;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace AsyncStateMachine.UnitTests
{
    public class CallbackFilterTests
    {
        private readonly CallbackFilter _filter;

        /// <summary>
        /// Initializes a new instance of a <see cref="CallbackFilterTests"/> class.
        /// </summary>
        public CallbackFilterTests()
        {
            _filter = new CallbackFilter();
        }

        [Fact]
        public void Filter_NullCallbacks_Throws()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _filter.Filter(null, _ => true));
        }

        [Fact]
        public void Filter_NullFilter_Throws()
        {
            // Arrange
            var mocks = RandomCallbacks(10);
            var callbacks = mocks.Select(x => x.Object).ToList();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _filter.Filter(callbacks, null));
        }

        [Fact]
        public void Filter_NoFilter_AllReturned()
        {
            // Arrange
            var mocks = RandomCallbacks(10);
            var callbacks = mocks.Select(x => x.Object).ToList();

            // Act
            var result = _filter.Filter(callbacks, _ => true);

            // Assert
            Assert.Equal(callbacks, result);
        }

        [Theory]
        [InlineData(5, 10)]
        public void Filter_SomeFiltered_SomeReturned(ushort sizeSubset, ushort numCallbacks)
        {
            // Arrange
            var mocks = RandomCallbacks(numCallbacks);
            var callbacks = mocks.Select(x => x.Object).ToList();
            var subset = callbacks.Take(sizeSubset).ToList();
            bool filter(ICallback x) => subset.Contains(x);

            // Act
            var result = _filter.Filter(callbacks, filter).ToArray();

            // Assert
            Assert.Equal(sizeSubset, result.Length);
            Assert.True(result.Length < callbacks.Count);
        }

        [Fact]
        public void Filter_AllFiltered_AllReturned()
        {
            // Arrange
            var mocks = RandomCallbacks(10);
            var callbacks = mocks.Select(x => x.Object).ToList();

            // Act
            var result = _filter.Filter(callbacks, _ => false);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void Filter_GuardThrows_Throws()
        {
            // Arrange
            var mocks = RandomCallbacks(10);
            var callbacks = mocks.Select(x => x.Object).ToList();

            // Act
            var exception = Record.Exception(
                () => _filter.Filter(callbacks,
                                     _ => true,
                                     _ => throw new Exception()));

            // Assert
            Assert.NotNull(exception);
        }

        [Fact]
        public void Filter_AllFiltered_GuardThrows()
        {
            // Arrange
            void EmptyGuard(IEnumerable<ICallback> callbacks)
            {
                if (!callbacks.Any())
                    throw new Exception("no callbacks");
            };

            // Act
            var exception = Record.Exception(
                () => _filter.Filter(Array.Empty<ICallback>(), _ => false, EmptyGuard));

            // Assert
            Assert.NotNull(exception);
        }

        private static IList<Mock<ICallback>> RandomCallbacks(ushort num)
        {
            return Enumerable
                .Range(0, num)
                .Select(_ => new Mock<ICallback>())
                .ToList();
        }
    }
}