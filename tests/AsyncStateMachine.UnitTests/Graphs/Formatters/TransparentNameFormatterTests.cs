using AsyncStateMachine.Contracts;
using AsyncStateMachine.Graphs.Formatters;
using Xunit;

namespace AsyncStateMachine.UnitTests.Graphs.Formatters
{
    public class TransparentNameFormatterTests
    {
        private readonly INameFormatter _formatter;

        /// <summary>
        /// Initializes a new instance of a <see cref="TransparentNameFormatterTests"/> class.
        /// </summary>
        public TransparentNameFormatterTests()
        {
            _formatter = new TransparentNameFormatter();
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData("ABigBrownFoxJumpsOverTheFence", "ABigBrownFoxJumpsOverTheFence")]
        [InlineData("ABuzz_Word", "ABuzz_Word")]
        public void FormatName_Input_OutputCorrect(string input, string expected)
        {
            // Act
            var output = _formatter.FormatName(input);

            // Assert
            Assert.Equal(expected, output);
        }
    }
}