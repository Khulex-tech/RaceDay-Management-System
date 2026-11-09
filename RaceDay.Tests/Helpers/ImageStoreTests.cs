using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using RaceDay.API.Helpers;

namespace RaceDay.Tests.Helpers
{
    public class ImageStoreTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("not a URL")]
        [InlineData("https://private.example/image.png")]
        public async Task OpenAsync_WithoutStorage_ReturnsMissing(string url)
        {
            var store = new AzureBlobImageStore(new ConfigurationBuilder().Build());
            Assert.Null(await store.OpenAsync(url));
        }

        [Fact]
        public async Task SaveAsync_WithoutAConnectionString_DoesNotThrow()
        {
            var store = new AzureBlobImageStore(new ConfigurationBuilder().Build());
            await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            IFormFile file = new FormFile(stream, 0, 3, "file", "runner.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };

            ImageSaveResult result = await store.SaveAsync(file);

            Assert.False(result.Success);
            Assert.Equal("Azure Blob Storage is not configured.", result.Error);
        }
    }
}
