using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Smart_Sudoku
{
    /// <summary>
    /// Profile pictures: choosing a photo, making it a small square, saving it in the database,
    /// and drawing it as a circle.
    /// </summary>
    public static class Avatars
    {
        /// <summary>Saved pictures are 256×256, so the database stays small.</summary>
        private const uint Size = 256;

        // Pictures already loaded during this run of the app (user id -> picture)
        private static readonly Dictionary<int, ImageSource?> Cache = new Dictionary<int, ImageSource?>();

        /// <summary>Lets the player pick a photo, then saves it as their profile picture. Returns false if they cancelled.</summary>
        public static async Task<bool> PickAndSaveAsync(int userId)
        {
            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            };
            foreach (string type in new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" })
                picker.FileTypeFilter.Add(type);

            var file = await picker.PickSingleFileAsync();
            if (file == null)
                return false;

            using var input = await file.OpenReadAsync();
            byte[] image = await MakeSquareAsync(input);
            Database.SetAvatar(userId, image);
            Cache.Remove(userId);
            return true;
        }

        public static void Remove(int userId)
        {
            Database.RemoveAvatar(userId);
            Cache.Remove(userId);
        }

        /// <summary>Cuts the middle square out of the photo and shrinks it to 256×256 (PNG).</summary>
        private static async Task<byte[]> MakeSquareAsync(IRandomAccessStream input)
        {
            var decoder = await BitmapDecoder.CreateAsync(input);
            double width = decoder.OrientedPixelWidth, height = decoder.OrientedPixelHeight;

            // Scale so the short side is 256, then keep the centered 256×256 part
            double scale = Size / Math.Min(width, height);
            uint scaledWidth = (uint)Math.Max(Size, Math.Round(width * scale));
            uint scaledHeight = (uint)Math.Max(Size, Math.Round(height * scale));
            var transform = new BitmapTransform
            {
                ScaledWidth = scaledWidth,
                ScaledHeight = scaledHeight,
                InterpolationMode = BitmapInterpolationMode.Fant,
                Bounds = new BitmapBounds
                {
                    X = (scaledWidth - Size) / 2,
                    Y = (scaledHeight - Size) / 2,
                    Width = Size,
                    Height = Size,
                },
            };
            var pixels = await decoder.GetPixelDataAsync(
                BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, transform,
                ExifOrientationMode.RespectExifOrientation, ColorManagementMode.DoNotColorManage);

            using var output = new InMemoryRandomAccessStream();
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, output);
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, Size, Size, 96, 96, pixels.DetachPixelData());
            await encoder.FlushAsync();

            var bytes = new byte[output.Size];
            output.Seek(0);
            await output.ReadAsync(bytes.AsBuffer(), (uint)bytes.Length, InputStreamOptions.None);
            return bytes;
        }

        /// <summary>A user's picture ready to show, or null if they have none.</summary>
        public static async Task<ImageSource?> LoadAsync(int userId)
        {
            if (Cache.TryGetValue(userId, out var cached))
                return cached;

            ImageSource? source = null;
            if (Database.GetAvatar(userId) is byte[] image)
            {
                using var stream = new InMemoryRandomAccessStream();
                await stream.WriteAsync(image.AsBuffer());
                stream.Seek(0);
                var bitmap = new BitmapImage();
                await bitmap.SetSourceAsync(stream);
                source = bitmap;
            }
            Cache[userId] = source;
            return source;
        }

        /// <summary>A round picture, or the name's first letter in a circle when there's no picture.</summary>
        public static Border Circle(ImageSource? picture, string name, double size)
        {
            var circle = new Border
            {
                Width = size,
                Height = size,
                CornerRadius = new CornerRadius(size / 2),
                BorderBrush = (Brush)Application.Current.Resources["RoseBrush"],
                BorderThickness = new Thickness(size > 60 ? 3 : 2),
            };

            if (picture != null)
            {
                circle.Background = new ImageBrush { ImageSource = picture, Stretch = Stretch.UniformToFill };
            }
            else
            {
                circle.Background = (Brush)Application.Current.Resources["SelectedCellBrush"];
                circle.Child = new TextBlock
                {
                    Text = name.Length > 0 ? name.Substring(0, 1) : "🙂",
                    FontSize = size * 0.45,
                    FontWeight = FontWeights.Bold,
                    Foreground = (Brush)Application.Current.Resources["BerryBrush"],
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
            }
            return circle;
        }
    }
}
