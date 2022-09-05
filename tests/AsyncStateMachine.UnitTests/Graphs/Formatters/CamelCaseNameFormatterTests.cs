using AsyncStateMachine.Contracts;
using AsyncStateMachine.Graphs.Formatters;
using Xunit;

namespace AsyncStateMachine.UnitTests.Graphs.Formatters
{
    public class CamelCaseNameFormatterTests
    {
        private readonly INameFormatter _formatter;

        /// <summary>
        /// Initializes a new instance of a <see cref="CamelCaseNameFormatterTests"/> class.
        /// </summary>
        public CamelCaseNameFormatterTests()
        {
            _formatter = new CamelCaseNameFormatter();
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData("ABigBrownFoxJumpsOverTheFence", "A Big Brown Fox Jumps Over The Fence")]
        [InlineData("ABuzz_Word", "A Buzz-Word")]
        public void FormatName_Input_OutputCorrect(string input, string expected)
        {
            // Act
            var output = _formatter.FormatName(input);

            // Assert
            Assert.Equal(expected, output);
        }
    }
}