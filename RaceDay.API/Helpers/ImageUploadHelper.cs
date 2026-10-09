namespace RaceDay.API.Helpers
{
    /*
     * Saves uploaded images (profile pictures and event banners) to wwwroot/uploads
     * and returns the URL path. Part 3 will swap this for Azure Blob Storage.
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

        // Saves the file with a unique name and returns its URL, e.g. /uploads/abc123.png
        public static async Task<string> SaveAsync(IFormFile file, string webRootPath)
        {
            string uploadFolder = Path.Combine(webRootPath, "uploads");
            Directory.CreateDirectory(uploadFolder);

            string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName).ToLower();
            string fullPath = Path.Combine(uploadFolder, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return "/uploads/" + fileName;
        }
    }
}
