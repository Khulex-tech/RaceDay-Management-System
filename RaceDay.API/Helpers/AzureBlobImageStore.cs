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

    public class ImageContent
    {
        public Stream Content { get; init; } = Stream.Null;
        public string ContentType { get; init; } = "application/octet-stream";
    }

    public interface IImageStore
    {
        Task<ImageSaveResult> SaveAsync(IFormFile file);

        // Reads a stored blob with the account key. The container does not need to be public.
        Task<ImageContent?> OpenAsync(string storedUrl);
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
                var container = await OpenContainerAsync();
                if (container == null)
                {
                    return ImageSaveResult.Failed("Azure Blob Storage is not configured.");
                }

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

        public async Task<ImageContent?> OpenAsync(string storedUrl)
        {
            if (string.IsNullOrWhiteSpace(storedUrl) || !Uri.TryCreate(storedUrl, UriKind.Absolute, out Uri? uri))
            {
                return null;
            }

            try
            {
                var container = await OpenContainerAsync();
                if (container == null)
                {
                    return null;
                }

                string blobName = Uri.UnescapeDataString(uri.Segments[^1].Trim('/'));
                var blob = container.GetBlobClient(blobName);
                if (!await blob.ExistsAsync())
                {
                    return null;
                }

                var download = await blob.DownloadStreamingAsync();
                string contentType = string.IsNullOrWhiteSpace(download.Value.Details.ContentType)
                    ? "application/octet-stream"
                    : download.Value.Details.ContentType;
                return new ImageContent { Content = download.Value.Content, ContentType = contentType };
            }
            catch (Exception)
            {
                return null;
            }
        }

        // Creates a private container. Pages do not read the blob anonymously; the API streams it.
        private async Task<BlobContainerClient?> OpenContainerAsync()
        {
            string? connectionString = _configuration["AzureBlob:ConnectionString"];
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return null;
            }

            string containerName = _configuration["AzureBlob:ContainerName"] ?? "raceday-images";
            if (string.IsNullOrWhiteSpace(containerName))
            {
                containerName = "raceday-images";
            }

            var container = new BlobServiceClient(connectionString).GetBlobContainerClient(containerName);
            await container.CreateIfNotExistsAsync(PublicAccessType.None);
            try
            {
                await container.SetAccessPolicyAsync(PublicAccessType.None);
            }
            catch (Exception)
            {
                // A locked-down account may refuse the access change. The key can still read the blob.
            }

            return container;
        }
    }
}
