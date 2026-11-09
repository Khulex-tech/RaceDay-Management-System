namespace RaceDay.API.Helpers
{
    /*
     * Checks profile pictures and event banners before they are sent to Azure Blob Storage.
     * The MVC site never stores the file itself.
     */
    public static class ImageUploadHelper
    {
        private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png" };

        // Returns an error message if the file is not allowed, otherwise null
        public static string? Validate(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return "Please choose a file to upload.";
            }

            if (file.Length > MaxFileSize)
            {
                return "File is too large. The maximum size is 5 MB.";
            }

            string extension = Path.GetExtension(file.FileName).ToLower();
            if (!AllowedExtensions.Contains(extension))
            {
                return "Only .jpg and .png files are allowed.";
            }

            return null;
        }
    }
}
