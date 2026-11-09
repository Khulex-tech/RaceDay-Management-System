using RaceDay.Web.Helpers;

namespace RaceDay.Web.Tests
{
    public class FinishPositionTests
    {
        [Fact]
        public void SparseCapturedResults_KeepTheRecordedPlaceSeparateFromTheSavedCount()
        {
            string text = FinishPosition.Format(47, 3);

            Assert.Equal("47th recorded. 3 results on file.", text);
            Assert.DoesNotContain("out of", text);
        }

        [Fact]
        public void OneSavedResult_IsNotCalledAField()
        {
            string text = FinishPosition.Format(1, 1);

            Assert.Equal("1st recorded. 1 result on file.", text);
            Assert.DoesNotContain("finisher", text);
        }

        [Theory]
        [InlineData(11, "11th recorded. 2 results on file.")]
        [InlineData(12, "12th recorded. 2 results on file.")]
        [InlineData(13, "13th recorded. 2 results on file.")]
        public void Teens_UseTh(int position, string expected)
        {
            Assert.Equal(expected, FinishPosition.Format(position, 2));
        }
    }
}
