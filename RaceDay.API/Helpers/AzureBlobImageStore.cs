using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.API.Helpers
{
    public class ImageSaveResult
    {
        public bool Success { get; init; }
        public string? Url { get; init; }
        public string? Error { get; init; }

        public static ImageSaveResult Saved(string url) => new() { Success = true, Url = url };
        public static ImageSaveResult Failed(string error) => new() { Success = false, Error = error };
    }

    public interface IImageStore
    {
        Task<ImageSaveResult> SaveAsync(IFormFile file);
    }

    /*
     * Uploads an image with the Azure Blob Storage SDK.
     * Local runs use Azurite. A live Azure Storage account is a separate connection string
     * and is not the same thing as the emulator.
     */
    public class AzureBlobImageStore : IImageStore
    {
        private readonly IConfiguration _configuration;

        public AzureBlobImageStore(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<ImageSaveResult> SaveAsync(IFormFile file)
        {
            string? connectionString = _configuration["AzureBlob:ConnectionString"];
            string containerName = _configuration["AzureBlob:ContainerName"] ?? "raceday-images";
            if (string.IsNullOrWhiteSpace(containerName))
            {
                containerName = "raceday-images";
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return ImageSaveResult.Failed("Azure Blob Storage is not configured.");
            }

            try
            {
                var service = new BlobServiceClient(connectionString);
                var container = service.GetBlobContainerClient(containerName);
                await container.CreateIfNotExistsAsync(PublicAccessType.Blob);

                string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                string blobName = Guid.NewGuid().ToString("N") + extension;
                var blob = container.GetBlobClient(blobName);

                await using var stream = file.OpenReadStream();
                string contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;
                await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType });

                string? publicBase = _configuration["AzureBlob:PublicBaseUrl"];
                string url = string.IsNullOrWhiteSpace(publicBase)
                    ? blob.Uri.ToString()
                    : publicBase.TrimEnd('/') + "/" + containerName + "/" + blobName;

                return ImageSaveResult.Saved(url);
            }
            catch (Exception)
            {
                return ImageSaveResult.Failed("The image could not be stored. Check the blob storage connection.");
            }
        }
    }
}
