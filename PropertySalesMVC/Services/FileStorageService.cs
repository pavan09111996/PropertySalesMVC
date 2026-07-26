using SkiaSharp;

namespace PropertySalesMVC.Services
{
    public class FileStorageService : IFileStorageService
    {
        // Uploaded photos arrive in wildly inconsistent shapes and resolutions
        // (phone portraits, huge DSLR originals, old low-res scans...). Every
        // display spot on the site (cards, gallery, admin grids) already
        // crops to a fixed aspect ratio via CSS object-fit, but that can only
        // crop — it can't fix an oversized file or a format that varies from
        // upload to upload. Normalizing here (cap the resolution, always
        // re-encode to JPEG) makes every stored photo behave the same way
        // downstream, and keeps a stray 12MP original from bloating storage.
        private const int MaxDimension = 1600;
        private const int JpegQuality = 85;

        private readonly IWebHostEnvironment _env;

        public FileStorageService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> SavePropertyImageAsync(int propertyId, IFormFile file)
        {
            string uploadRoot = Path.Combine(
                _env.WebRootPath, "uploads", "properties", propertyId.ToString());

            Directory.CreateDirectory(uploadRoot);

            string fileName = Guid.NewGuid() + ".jpg";
            string fullPath = Path.Combine(uploadRoot, fileName);

            await using (var uploadStream = file.OpenReadStream())
            using (var original = SKBitmap.Decode(uploadStream))
            {
                if (original == null)
                    throw new InvalidOperationException("The uploaded file is not a readable image.");

                // Only allocate a resized copy when actually shrinking — resizing()
                // returns a distinct bitmap that we alone own and must dispose;
                // "original" is always disposed by the outer using regardless.
                SKBitmap? resized = original.Width > MaxDimension || original.Height > MaxDimension
                    ? Resize(original)
                    : null;

                using (resized)
                {
                    SKBitmap toEncode = resized ?? original;
                    using SKImage image = SKImage.FromBitmap(toEncode);
                    using SKData jpeg = image.Encode(SKEncodedImageFormat.Jpeg, JpegQuality);
                    await using var outStream = new FileStream(fullPath, FileMode.Create);
                    jpeg.SaveTo(outStream);
                }
            }

            return $"/uploads/properties/{propertyId}/{fileName}";
        }

        private static SKBitmap Resize(SKBitmap original)
        {
            double scale = Math.Min((double)MaxDimension / original.Width, (double)MaxDimension / original.Height);
            var targetInfo = new SKImageInfo(
                (int)Math.Round(original.Width * scale),
                (int)Math.Round(original.Height * scale));

            return original.Resize(targetInfo, SKSamplingOptions.Default)
                ?? throw new InvalidOperationException("Failed to resize the uploaded image.");
        }

        public async Task<string> SavePropertyVideoAsync(int propertyId, IFormFile file)
        {
            string videoDir = Path.Combine(
                _env.WebRootPath, "uploads", "properties", propertyId.ToString(), "video");

            Directory.CreateDirectory(videoDir);

            string videoName = "property-video" + Path.GetExtension(file.FileName);
            string fullPath = Path.Combine(videoDir, videoName);

            await using (var fs = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(fs);
            }

            return $"/uploads/properties/{propertyId}/video/{videoName}";
        }

        public void DeleteFile(string webRelativePath)
        {
            string fullPath = Path.Combine(
                _env.WebRootPath,
                webRelativePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));

            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }

        public void DeletePropertyFolder(int propertyId)
        {
            string propertyFolder = Path.Combine(
                _env.WebRootPath, "uploads", "properties", propertyId.ToString());

            if (Directory.Exists(propertyFolder))
                Directory.Delete(propertyFolder, recursive: true);
        }
    }
}
