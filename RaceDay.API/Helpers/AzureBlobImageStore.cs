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
            try
            {
                var container = GetContainer();
                if (container == null)
                {
                    return ImageSaveResult.Failed("Azure Blob Storage is not configured.");
                }

                await container.CreateIfNotExistsAsync(PublicAccessType.None);
                await container.SetAccessPolicyAsync(PublicAccessType.None);

                string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                string blobName = Guid.NewGuid().ToString("N") + extension;
                var blob = container.GetBlobClient(blobName);

                await using var stream = file.OpenReadStream();
                string contentType = extension == ".png" ? "image/png" : "image/jpeg";
                await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType });

                string? publicBase = _configuration["AzureBlob:PublicBaseUrl"];
                string url = string.IsNullOrWhiteSpace(publicBase)
                    ? blob.Uri.ToString()
                    : publicBase.TrimEnd('/') + "/" + container.Name + "/" + blobName;

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
                var container = GetContainer();
                if (container == null)
                {
                    return null;
                }

                string blobName = Uri.UnescapeDataString(uri.Segments[^1].Trim('/'));
                string? publicBase = _configuration["AzureBlob:PublicBaseUrl"];
                string expectedUrl = string.IsNullOrWhiteSpace(publicBase)
                    ? container.GetBlobClient(blobName).Uri.AbsoluteUri
                    : publicBase.TrimEnd('/') + "/" + container.Name + "/" + Uri.EscapeDataString(blobName);
                if (string.IsNullOrWhiteSpace(blobName) || blobName.Contains('/') || blobName.Contains('\\') ||
                    !Uri.TryCreate(expectedUrl, UriKind.Absolute, out Uri? expected) || uri != expected)
                {
                    return null;
                }
                string extension = Path.GetExtension(blobName).ToLowerInvariant();
                if (extension != ".png" && extension != ".jpg" && extension != ".jpeg")
                {
                    return null;
                }

                var blob = container.GetBlobClient(blobName);
                if (!await blob.ExistsAsync())
                {
                    return null;
                }

                var download = await blob.DownloadStreamingAsync();
                string contentType = extension == ".png" ? "image/png" : "image/jpeg";
                return new ImageContent { Content = download.Value.Content, ContentType = contentType };
            }
            catch (Exception)
            {
                return null;
            }
        }

        // Reading an image must not create containers or change their access policy.
        private BlobContainerClient? GetContainer()
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

            return new BlobServiceClient(connectionString).GetBlobContainerClient(containerName);
        }
    }
}
