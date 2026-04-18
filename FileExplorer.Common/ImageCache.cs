using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AsyncKeyedLock;
using PhotoSauce.MagicScaler;

namespace FileExplorer.Common.Helper
{
    public class ImageCache
    {
        public static Task<ImageSource> TryGetValue(string filePath, int width = 960)
        {
            return TryGetValue(filePath, new ProcessImageSettings { Width = width, ResizeMode = CropScaleMode.Max });
        }

        public static async Task<ImageSource> TryGetValue(string filePath, ProcessImageSettings settings)
        {
            string cacheKey = GetCacheKey(filePath, settings);
            if (!Cache.Storage.Exists(cacheKey))
                return null;

            try
            {
                using (MemoryStream outputStream = new MemoryStream())
                {
                    await Task.Run(() =>
                    {
                        Cache.Storage.Download(cacheKey, outputStream);
                        outputStream.Position = 0;
                    });

                    return StreamToImage(outputStream);
                }
            }
            catch
            {
                return null;
            }
        }

        public static Task<ImageSource> GetOrAddValue(string filePath, Stream inputStream, int width = 960)
        {
            return GetOrAddValue(filePath, inputStream, new ProcessImageSettings { Width = width, ResizeMode = CropScaleMode.Max });
        }

        public static async Task<ImageSource> GetOrAddValue(string filePath, Stream inputStream, ProcessImageSettings settings)
        {
            ImageSource image = await TryGetValue(filePath, settings);
            if (image != null)
                return image;
            
            try
            {
                using (MemoryStream outputStream = new MemoryStream())
                {
                    await Task.Run(() =>
                    {
                        MagicImageProcessor.ProcessImage(inputStream, outputStream, settings);
                        outputStream.Position = 0;

                        string cacheKey = GetCacheKey(filePath, settings);                        
                        using (ImageKeyLockProvider.Lock(cacheKey))
                        {
                            LiteDB.BsonDocument metadata = new LiteDB.BsonDocument();
                            if (settings.Width > 0)
                                metadata["Width"] = settings.Width;
                            if (settings.Height > 0)
                                metadata["Height"] = settings.Height;

                            Cache.Storage.Upload(cacheKey, filePath, outputStream, metadata);
                            outputStream.Position = 0;
                        }
                    });

                    return StreamToImage(outputStream);
                }
            }
            catch
            {
                return null;
            }
        }

        private static string GetCacheKey(string filePath, ProcessImageSettings settings)
        {
            string json = JsonSerializer.Serialize(settings);
            string cacheKey = $"{filePath}_{json}";

            using (SHA256 sha256 = SHA256.Create())
            {
                StringBuilder stringBuilder = new StringBuilder();
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(cacheKey.ToLowerInvariant()));
                for (int i = 0; i < bytes.Length; i++)
                    stringBuilder.Append(bytes[i].ToString("x2"));

                return stringBuilder.ToString();
            }
        }

        private static ImageSource StreamToImage(Stream stream)
        {
            BitmapImage bitmapImage = new BitmapImage();

            bitmapImage.BeginInit();
            bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = stream;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            return bitmapImage;
        }

        private static readonly AsyncKeyedLocker<string> ImageKeyLockProvider = new AsyncKeyedLocker<string>();
    }
}
