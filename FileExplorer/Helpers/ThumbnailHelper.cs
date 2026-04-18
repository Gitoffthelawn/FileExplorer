using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using FileExplorer.Common.Helper;
using FileExplorer.Properties;
using PhotoSauce.MagicScaler;

namespace FileExplorer.Helpers
{
    public class ThumbnailHelper
    {
        public static async Task<ImageSource> GetThumbnailImage(string path)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                try
                {
                    ProcessImageSettings settings = new ProcessImageSettings();
                    settings.Anchor = (CropAnchor)Settings.Default.ThumbnailAnchor;
                    settings.ResizeMode = (CropScaleMode)Settings.Default.ThumbnailMode;
                    settings.Width = Settings.Default.ThumbnailHeight;
                    settings.Height = Settings.Default.ThumbnailHeight;

                    ImageSource imageSource = await ImageCache.TryGetValue(path, settings);
                    if (imageSource == null)
                    {
                        using (FileStream fileStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                        {
                            imageSource = await ImageCache.GetOrAddValue(path, fileStream, settings);
                        }
                    }

                    return imageSource;
                }
                catch
                {
                    return null;
                }
            }
        }

        public static bool ThumbnailExists(string path)
        {
            return Regex.Match(path, SupportedImageFormats, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase).Success;
        }

        private const string SupportedImageFormats = @"^.+\.(?:(?:avif)|(?:bmp)|(?:dip)|(?:gif)|(?:heic)|(?:heif)|(?:jfif)|(?:jpe)|(?:jpe?g)|(?:jxr)|(?:png)|(?:rle)|(?:tiff?)|(?:wdp)|(?:webp))$";
    }
}
