using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace Smart_Sudoku
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default <see cref="Application"/> class.
    /// </summary>
    public sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object. This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();

            // Open tall enough to show the whole Sudoku board and number pad
            ApplicationView.PreferredLaunchViewSize = new Size(900, 860);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            Suspending += OnSuspending;

            // Keep a record of crashes in the app's local folder (crash-log.txt), to help fix them
            UnhandledException += (s, e) =>
            {
                try
                {
                    string path = System.IO.Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "crash-log.txt");
                    System.IO.File.AppendAllText(path, $"{DateTime.Now:o}\n{e.Exception}\n\n");
                }
                catch (Exception)
                {
                }
            };
        }

        private const double DecorWidth = 1600, DecorHeight = 1000;

        private static Frame? rootFrame;
        private static Image? patternImage;
        private static Canvas? decorCanvas;
        private static Border? toast;
        private static DispatcherTimer? toastTimer;

        /// <summary>A short message that slides in at the top of any page (achievements, rewards).</summary>
        public static void ShowToast(string message)
        {
            if (toast == null)
                return;

            ((TextBlock)toast.Child).Text = message;
            toast.Visibility = Visibility.Visible;
            var slide = new Windows.UI.Xaml.Media.Animation.Storyboard();
            var move = new Windows.UI.Xaml.Media.Animation.DoubleAnimation
            {
                From = -80,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(350),
                EasingFunction = new Windows.UI.Xaml.Media.Animation.BackEase { EasingMode = Windows.UI.Xaml.Media.Animation.EasingMode.EaseOut },
            };
            Windows.UI.Xaml.Media.Animation.Storyboard.SetTarget(move, toast.RenderTransform);
            Windows.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(move, "Y");
            slide.Children.Add(move);
            slide.Begin();

            toastTimer?.Stop();
            toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3.5) };
            toastTimer.Tick += (s, e) =>
            {
                toastTimer.Stop();
                toast.Visibility = Visibility.Collapsed;
            };
            toastTimer.Start();
        }

        /// <summary>
        /// Shows a theme's picture (from Assets/Patterns) and scattered emoji behind every page.
        /// </summary>
        public static void SetThemeBackground(string? patternFile, string[]? decorations, string[]? pictures = null, string motion = "float")
        {
            if (patternImage == null || decorCanvas == null)
                return;

            patternImage.Source = patternFile == null
                ? null
                : new BitmapImage(new Uri($"ms-appx:///Assets/Patterns/{patternFile}"));

            decorCanvas.Children.Clear();

            // Storybook pictures, framed like photos along the left and right edges
            if (pictures != null)
            {
                var spots = new[] { (40.0, 120.0, -6.0), (1290.0, 160.0, 5.0), (60.0, 600.0, 4.0), (1270.0, 620.0, -5.0) };
                for (int i = 0; i < spots.Length; i++)
                {
                    var (left, top, angle) = spots[i];
                    var frame = new Border
                    {
                        Background = new SolidColorBrush(Colors.White),
                        Padding = new Thickness(10, 10, 10, 26),
                        CornerRadius = new CornerRadius(6),
                        Opacity = 0.85,
                        RenderTransform = new RotateTransform { Angle = angle },
                        Child = new Image
                        {
                            Source = new BitmapImage(new Uri($"ms-appx:///Assets/Characters/{pictures[i % pictures.Length]}")),
                            Width = 240,
                            Height = 240,
                            Stretch = Stretch.UniformToFill,
                        },
                    };
                    Canvas.SetLeft(frame, left);
                    Canvas.SetTop(frame, top);
                    decorCanvas.Children.Add(frame);
                }
            }

            if (decorations == null)
                return;

            // Same seed every time, so the decorations don't jump around between pages
            var random = new Random(42);
            bool animate = new Windows.UI.ViewManagement.UISettings().AnimationsEnabled;
            for (int i = 0; i < 46; i++)
            {
                double opacity = 0.22 + random.NextDouble() * 0.16;
                var transform = new CompositeTransform { Rotation = random.Next(-25, 25) };
                var piece = new TextBlock
                {
                    Text = decorations[i % decorations.Length],
                    FontSize = random.Next(30, 64),
                    Opacity = opacity,
                    RenderTransform = transform,
                };
                Canvas.SetLeft(piece, random.NextDouble() * DecorWidth);
                Canvas.SetTop(piece, random.NextDouble() * DecorHeight);
                decorCanvas.Children.Add(piece);

                // Respect the Windows "show animations" setting
                if (animate)
                    Move(piece, transform, opacity, motion, random);
            }
        }

        /// <summary>Gives a background decoration its theme's motion, forever.</summary>
        private static void Move(TextBlock piece, CompositeTransform transform, double opacity, string motion, Random random)
        {
            var storyboard = new Windows.UI.Xaml.Media.Animation.Storyboard
            {
                RepeatBehavior = Windows.UI.Xaml.Media.Animation.RepeatBehavior.Forever,
                BeginTime = TimeSpan.FromSeconds(random.NextDouble() * 4),
            };

            Windows.UI.Xaml.Media.Animation.DoubleAnimation animation;
            switch (motion)
            {
                case "snow":
                case "bubbles":
                    // Drift all the way down (snow) or up (bubbles), then start again
                    double top = Canvas.GetTop(piece);
                    double distance = motion == "snow" ? DecorHeight + 80 - top : -(top + 80);
                    animation = new Windows.UI.Xaml.Media.Animation.DoubleAnimation
                    {
                        From = 0,
                        To = distance,
                        Duration = TimeSpan.FromSeconds(Math.Abs(distance) / (25 + random.NextDouble() * 25)),
                    };
                    Windows.UI.Xaml.Media.Animation.Storyboard.SetTarget(animation, transform);
                    Windows.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(animation, "TranslateY");
                    break;
                case "twinkle":
                    animation = new Windows.UI.Xaml.Media.Animation.DoubleAnimation
                    {
                        From = opacity,
                        To = 0.05,
                        Duration = TimeSpan.FromSeconds(1.5 + random.NextDouble() * 2),
                        AutoReverse = true,
                    };
                    Windows.UI.Xaml.Media.Animation.Storyboard.SetTarget(animation, piece);
                    Windows.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(animation, "Opacity");
                    break;
                default:
                    // A gentle float up and down
                    animation = new Windows.UI.Xaml.Media.Animation.DoubleAnimation
                    {
                        From = 0,
                        To = -14,
                        Duration = TimeSpan.FromSeconds(2 + random.NextDouble() * 2),
                        AutoReverse = true,
                        EasingFunction = new Windows.UI.Xaml.Media.Animation.SineEase { EasingMode = Windows.UI.Xaml.Media.Animation.EasingMode.EaseInOut },
                    };
                    Windows.UI.Xaml.Media.Animation.Storyboard.SetTarget(animation, transform);
                    Windows.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(animation, "TranslateY");
                    break;
            }

            storyboard.Children.Add(animation);
            storyboard.Begin();
        }

        /// <inheritdoc/>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active.
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // TODO: Load state from previously suspended application
                }

                // The theme background and picture sit behind the frame, so every page shares them
                patternImage = new Image { Stretch = Stretch.UniformToFill, IsHitTestVisible = false };
                decorCanvas = new Canvas { Width = DecorWidth, Height = DecorHeight };
                var decorLayer = new Viewbox { Stretch = Stretch.UniformToFill, Child = decorCanvas, IsHitTestVisible = false };
                var root = new Grid { Background = (Brush)Resources["PageBackgroundBrush"] };
                root.Children.Add(patternImage);
                root.Children.Add(decorLayer);
                root.Children.Add(rootFrame);

                // Smooth transitions between pages
                rootFrame.ContentTransitions = new Windows.UI.Xaml.Media.Animation.TransitionCollection
                {
                    new Windows.UI.Xaml.Media.Animation.NavigationThemeTransition(),
                };

                toast = new Border
                {
                    Background = (Brush)Resources["ButtonHoverBrush"],
                    BorderBrush = (Brush)Resources["RoseBrush"],
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(18),
                    Padding = new Thickness(22, 12, 22, 12),
                    Margin = new Thickness(0, 18, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top,
                    Visibility = Visibility.Collapsed,
                    IsHitTestVisible = false,
                    RenderTransform = new TranslateTransform(),
                    Child = new TextBlock
                    {
                        FontSize = 20,
                        FontWeight = Windows.UI.Text.FontWeights.SemiBold,
                        Foreground = (Brush)Resources["BerryBrush"],
                        FlowDirection = FlowDirection.RightToLeft,
                    },
                };
                root.Children.Add(toast);

                Database.Init();

                // Place the frame in the current Window
                Window.Current.Content = root;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page, configuring
                    // the new page by passing required information as a navigation parameter.
                    rootFrame.Navigate(typeof(Pages.MenuPage), e.Arguments);
                }

                // Ensure the current window is active
                Window.Current.Activate();
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails.
        /// </summary>
        /// <param name="sender">The Frame which failed navigation.</param>
        /// <param name="e">Details about the navigation failure.</param>
        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception($"Failed to load page '{e.SourcePageType.FullName}'.");
        }

        /// <summary>
        /// Invoked when application execution is being suspended. Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            SuspendingDeferral deferral = e.SuspendingOperation.GetDeferral();

            // TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}
