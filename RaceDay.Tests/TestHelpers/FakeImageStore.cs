using Microsoft.AspNetCore.Http;
using RaceDay.API.Helpers;

namespace RaceDay.Tests.TestHelpers
{
    // Stands in for Azure Blob Storage so controller tests can check the saved URL.
    public class FakeImageStore : IImageStore
    {
        public Task<ImageSaveResult> SaveAsync(IFormFile file)
        {
            string name = string.IsNullOrWhiteSpace(file.FileName) ? "image.png" : file.FileName;
            return Task.FromResult(ImageSaveResult.Saved("https://storage.example/raceday-images/" + name));
        }

        public Task<ImageContent?> OpenAsync(string storedUrl)
        {
            return Task.FromResult<ImageContent?>(null);
        }
    }
}
