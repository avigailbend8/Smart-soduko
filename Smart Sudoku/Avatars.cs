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
    // אובייקט: Avatars — אחראי על תמונות הפרופיל: בחירת תמונה, חיתוך לריבוע קטן, שמירה במסד הנתונים, והצגה בעיגול.
    {
        /// <summary>Saved pictures are 256×256, so the database stays small.</summary>
        private const uint Size = 256;
        // אובייקט: Size — גודל התמונה שנשמרת (256×256 נקודות), כדי שמסד הנתונים לא יהיה כבד.

        // Pictures already loaded during this run of the app (user id -> picture)
        private static readonly Dictionary<int, ImageSource?> Cache = new Dictionary<int, ImageSource?>();
        // אובייקט: Cache — תמונות שכבר נטענו בזמן שהאפליקציה פתוחה, כדי לא לטעון אותן שוב ושוב.

        /// <summary>Lets the player pick a photo, then saves it as their profile picture. Returns false if they cancelled.</summary>
        public static async Task<bool> PickAndSaveAsync(int userId)
        // פעולה: PickAndSaveAsync — פותחת חלון לבחירת תמונה, מקטינה אותה לריבוע ושומרת אותה כתמונת הפרופיל. מחזירה false אם ביטלו.
        {
            var picker = new FileOpenPicker
            // אובייקט: picker — חלון בחירת הקבצים של Windows, שמציג רק תמונות.
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            };
            foreach (string type in new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" })
                picker.FileTypeFilter.Add(type);

            var file = await picker.PickSingleFileAsync();
            // אובייקט: file — קובץ התמונה שהשחקן בחר.
            if (file == null)
                return false;

            using var input = await file.OpenReadAsync();
            // אובייקט: input — "צינור" לקריאת התוכן של קובץ התמונה.
            byte[] image = await MakeSquareAsync(input);
            // אובייקט: image — התמונה אחרי החיתוך וההקטנה, כרשימת בייטים שאפשר לשמור.
            Database.SetAvatar(userId, image);
            Cache.Remove(userId);
            return true;
        }

        public static void Remove(int userId)
        // פעולה: Remove — מוחקת את תמונת הפרופיל של המשתמש.
        {
            Database.RemoveAvatar(userId);
            Cache.Remove(userId);
        }

        /// <summary>Cuts the middle square out of the photo and shrinks it to 256×256 (PNG).</summary>
        private static async Task<byte[]> MakeSquareAsync(IRandomAccessStream input)
        // פעולה: MakeSquareAsync — חותכת את הריבוע שבאמצע התמונה ומקטינה אותו ל-256×256 (קובץ PNG).
        {
            var decoder = await BitmapDecoder.CreateAsync(input);
            // אובייקט: decoder — "מפענח" שקורא את התמונה המקורית.
            double width = decoder.OrientedPixelWidth, height = decoder.OrientedPixelHeight;

            // Scale so the short side is 256, then keep the centered 256×256 part
            double scale = Size / Math.Min(width, height);
            // אובייקט: scale — פי כמה להקטין, כך שהצלע הקצרה תהיה 256.
            uint scaledWidth = (uint)Math.Max(Size, Math.Round(width * scale));
            uint scaledHeight = (uint)Math.Max(Size, Math.Round(height * scale));
            var transform = new BitmapTransform
            // אובייקט: transform — ההוראות לשינוי התמונה: לאיזה גודל להקטין, ואיזה חלק לחתוך מהאמצע.
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
            // אובייקט: pixels — הנקודות (פיקסלים) של התמונה אחרי ההקטנה והחיתוך.
                BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, transform,
                ExifOrientationMode.RespectExifOrientation, ColorManagementMode.DoNotColorManage);

            using var output = new InMemoryRandomAccessStream();
            // אובייקט: output — מקום בזיכרון שאליו נכתבת התמונה החדשה.
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, output);
            // אובייקט: encoder — "מקודד" שהופך את הנקודות לקובץ PNG.
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, Size, Size, 96, 96, pixels.DetachPixelData());
            await encoder.FlushAsync();

            var bytes = new byte[output.Size];
            // אובייקט: bytes — התמונה הסופית כרשימת בייטים.
            output.Seek(0);
            await output.ReadAsync(bytes.AsBuffer(), (uint)bytes.Length, InputStreamOptions.None);
            return bytes;
        }

        /// <summary>A user's picture ready to show, or null if they have none.</summary>
        public static async Task<ImageSource?> LoadAsync(int userId)
        // פעולה: LoadAsync — טוענת את תמונת הפרופיל של משתמש כדי להציג אותה (או null אם אין לו תמונה).
        {
            if (Cache.TryGetValue(userId, out var cached))
                return cached;

            ImageSource? source = null;
            // אובייקט: source — התמונה המוכנה להצגה.
            if (Database.GetAvatar(userId) is byte[] image)
            {
                using var stream = new InMemoryRandomAccessStream();
                // אובייקט: stream — מקום בזיכרון שאליו מעתיקים את הבייטים של התמונה.
                await stream.WriteAsync(image.AsBuffer());
                stream.Seek(0);
                var bitmap = new BitmapImage();
                // אובייקט: bitmap — התמונה שאפשר להציג על המסך.
                await bitmap.SetSourceAsync(stream);
                source = bitmap;
            }
            Cache[userId] = source;
            return source;
        }

        /// <summary>
        /// A round picture, or the name's first letter in a circle when there's no picture.
        /// A frame from the store (see Cosmetics) adds a colored ring and a badge.
        /// </summary>
        public static FrameworkElement Circle(ImageSource? picture, string name, double size, string? frameId = null)
        // פעולה: Circle — בונה את העיגול של תמונת הפרופיל. אם אין תמונה — מציגה את האות הראשונה של השם.
        // אם השחקן קנה מסגרת — מוסיפה טבעת צבעונית ותג קטן.
        {
            var frame = Cosmetics.FrameFor(frameId);
            // אובייקט: frame — המסגרת שהשחקן בחר.
            bool fancy = frame.Id != "none";
            // אובייקט: fancy — אמת אם יש מסגרת מיוחדת (לא הפשוטה).
            var circle = new Border
            // אובייקט: circle — המסגרת העגולה (Border) שבתוכה תוצג התמונה.
            {
                Width = size,
                Height = size,
                CornerRadius = new CornerRadius(size / 2),
                BorderBrush = fancy ? BoardFrames.Gradient(frame.Colors) : (Brush)Application.Current.Resources["RoseBrush"],
                BorderThickness = new Thickness(fancy ? Math.Max(3, size / 14) : size > 60 ? 3 : 2),
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

            if (frame.Badge == null)
                return circle;

            // The badge sits on the top corner of the ring
            var holder = new Grid { Width = size, Height = size };
            // אובייקט: holder — "מגש" (Grid) שמחזיק את העיגול ואת התג שבפינה, זה מעל זה.
            holder.Children.Add(circle);
            holder.Children.Add(new TextBlock
            {
                Text = frame.Badge,
                FontSize = Math.Max(14, size * 0.3),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, -size * 0.12, -size * 0.08, 0),
                IsHitTestVisible = false,
            });
            return holder;
        }
    }
}
