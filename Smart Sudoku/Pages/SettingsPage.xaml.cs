using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Smart_Sudoku.Pages
{
    /// <summary>
    /// Every setting in one place: sound, music, night mode, mistakes, profile picture,
    /// password and secret word, and backups.
    /// </summary>
    public sealed partial class SettingsPage : BasePage
    {
        public SettingsPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Refresh();
        }

        private static TextBlock Section(string text) => Ui.Text(text, 22, "RoseBrush", bold: true);

        private async void Refresh()
        {
            SubtitleText.Text = Session.PlayerName != null ? $"ההגדרות של {Session.PlayerName}" : "ההגדרות של האורח";
            ContentPanel.Children.Clear();

            // Game
            ContentPanel.Children.Add(Section("🎮 משחק"));
            var game = new StackPanel { Spacing = 6 };
            game.Children.Add(Switch("🔊 צלילים", GameData.SoundOn, on =>
            {
                GameData.SetSound(on);
                Sounds.ApplyMusicSetting();
            }));
            game.Children.Add(Switch("🎵 מוזיקת רקע", GameData.MusicOn, on =>
            {
                GameData.MusicOn = on;
                Sounds.ApplyMusicSetting();
            }));
            game.Children.Add(Switch("🌙 מצב לילה", GameData.NightMode, on =>
            {
                GameData.NightMode = on;
                Themes.Apply(GameData.ThemeId);
                if (on)
                    Achievements.Unlock("night");
                Refresh();
            }));
            game.Children.Add(Switch("❌ להראות טעויות באדום (ולהוריד לב)", GameData.ShowMistakes, on => GameData.ShowMistakes = on));
            ContentPanel.Children.Add(Ui.Card(game));

            if (Session.UserId is int userId)
            {
                // Profile picture
                ContentPanel.Children.Add(Section("📷 תמונת פרופיל"));
                var picture = await Avatars.LoadAsync(userId);
                var choose = new Button { Content = "לבחור תמונה", FontSize = 16 };
                choose.Click += async (s, e) =>
                {
                    try
                    {
                        if (await Avatars.PickAndSaveAsync(userId))
                        {
                            App.ShowToast("📷 תמונת הפרופיל עודכנה!");
                            Refresh();
                        }
                    }
                    catch (Exception)
                    {
                        App.ShowToast("לא הצלחנו לפתוח את התמונה. נסו תמונה אחרת (JPG או PNG).");
                    }
                };
                var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { choose } };
                if (picture != null)
                {
                    var remove = new Button { Content = "להסיר", FontSize = 16 };
                    remove.Click += (s, e) =>
                    {
                        Avatars.Remove(userId);
                        Refresh();
                    };
                    buttons.Children.Add(remove);
                }
                ContentPanel.Children.Add(Ui.Card(Ui.Row(
                    Ui.Text("מסגרות מיוחדות לתמונה יש בחנות 👑", 15, "MutedTextBrush"),
                    buttons,
                    Avatars.Circle(picture, Session.PlayerName ?? "", 64, Cosmetics.CurrentFrameId))));

                // Password and secret word
                ContentPanel.Children.Add(Section("🔒 סיסמה"));
                ContentPanel.Children.Add(Ui.Card(PasswordCard(userId)));
            }

            // Backups
            ContentPanel.Children.Add(Section("💾 גיבוי"));
            ContentPanel.Children.Add(Ui.Card(await BackupCard()));
        }

        private static Grid Switch(string label, bool value, Action<bool> changed)
        {
            var toggle = new ToggleSwitch { IsOn = value, OnContent = "פועל", OffContent = "כבוי", MinWidth = 0 };
            toggle.Toggled += (s, e) => changed(toggle.IsOn);
            return Ui.Row(Ui.Text(label, 17), toggle);
        }

        private UIElement PasswordCard(int userId)
        {
            var oldBox = new PasswordBox { Header = "הסיסמה הנוכחית", FontSize = 16, CornerRadius = new CornerRadius(12) };
            var newBox = new PasswordBox { Header = "סיסמה חדשה", FontSize = 16, CornerRadius = new CornerRadius(12) };
            var result = Ui.Text("", 15, "BerryBrush", bold: true);
            var change = new Button { Content = "לשנות סיסמה", FontSize = 16, HorizontalAlignment = HorizontalAlignment.Stretch };
            change.Click += (s, e) =>
            {
                if (newBox.Password.Length < 4)
                    result.Text = "הסיסמה החדשה צריכה לפחות 4 תווים";
                else if (Database.ChangePassword(userId, oldBox.Password, newBox.Password))
                    result.Text = "✅ הסיסמה שונתה!";
                else
                    result.Text = "הסיסמה הנוכחית לא נכונה";
                oldBox.Password = newBox.Password = "";
            };

            var wordBox = new PasswordBox { Header = "מילת סוד (לשחזור אם שוכחים סיסמה)", FontSize = 16, CornerRadius = new CornerRadius(12) };
            var wordResult = Ui.Text(Database.HasRecoveryWord(userId) ? "✓ כבר נבחרה מילת סוד" : "עוד לא נבחרה מילת סוד", 14, "MutedTextBrush");
            var saveWord = new Button { Content = "לשמור מילת סוד", FontSize = 16, HorizontalAlignment = HorizontalAlignment.Stretch };
            saveWord.Click += (s, e) =>
            {
                if (wordBox.Password.Trim().Length < 2)
                {
                    wordResult.Text = "מילת הסוד צריכה לפחות 2 תווים";
                    return;
                }
                Database.SetRecoveryWord(userId, wordBox.Password);
                wordBox.Password = "";
                wordResult.Text = "✅ מילת הסוד נשמרה. אם תשכחו סיסמה — \"שכחתי סיסמה\" במסך ההתחברות.";
            };

            return new StackPanel { Spacing = 10, Children = { oldBox, newBox, change, result, wordBox, saveWord, wordResult } };
        }

        private async System.Threading.Tasks.Task<UIElement> BackupCard()
        {
            var panel = new StackPanel { Spacing = 10 };
            var status = Ui.Text("", 15, "MutedTextBrush");
            panel.Children.Add(Ui.Text("הגיבוי שומר את כל המשתמשים וההתקדמות בתיקייה שבוחרים, ומתעדכן לבד פעם ביום.", 15, "MutedTextBrush"));

            string folder = await Backup.FolderPathAsync();
            status.Text = folder.Length == 0
                ? "עוד לא נבחרה תיקיית גיבוי"
                : $"📁 {folder}\nגיבוי אחרון: {(Backup.LastBackup.Length > 0 ? Backup.LastBackup : "עוד לא")}";
            panel.Children.Add(status);

            var choose = new Button { Content = folder.Length == 0 ? "📁 לבחור תיקיית גיבוי" : "📁 לשנות תיקייה", FontSize = 16 };
            choose.Click += async (s, e) =>
            {
                if (await Backup.ChooseFolderAsync() != null)
                {
                    App.ShowToast("💾 הגיבוי הראשון נשמר!");
                    Refresh();
                }
            };
            var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { choose } };

            if (folder.Length > 0)
            {
                var now = new Button { Content = "💾 לגבות עכשיו", FontSize = 16 };
                now.Click += async (s, e) =>
                {
                    if (await Backup.BackupNowAsync())
                    {
                        App.ShowToast("💾 הגיבוי נשמר!");
                        Refresh();
                    }
                };
                buttons.Children.Add(now);
            }
            panel.Children.Add(buttons);

            // Restoring: pick one of the saved backups
            var backups = await Backup.ListAsync();
            if (backups.Count > 0)
            {
                var list = new ComboBox { PlaceholderText = "לבחור גיבוי לשחזור", FontSize = 16, MinWidth = 220 };
                foreach (string stamp in backups)
                    list.Items.Add(stamp);
                var restore = new Button { Content = "♻️ לשחזר", FontSize = 16 };
                restore.Click += async (s, e) =>
                {
                    if (list.SelectedItem is not string stamp)
                        return;
                    var confirm = new ContentDialog
                    {
                        Title = "לשחזר את הגיבוי?",
                        Content = $"כל ההתקדמות תחזור למצב של {stamp}. מה שקרה אחרי זה יימחק.",
                        PrimaryButtonText = "לשחזר",
                        CloseButtonText = "ביטול",
                        FlowDirection = FlowDirection.RightToLeft,
                    };
                    if (await confirm.ShowAsync() != ContentDialogResult.Primary)
                        return;
                    if (await Backup.RestoreAsync(stamp))
                    {
                        App.ShowToast("♻️ הגיבוי שוחזר!");
                        // Reload the player's data from the restored files
                        if (Session.UserId is int id)
                            GameData.LoadUser(id);
                        else
                            GameData.LoadGuest();
                        Themes.Apply(GameData.ThemeId);
                        Refresh();
                    }
                };
                panel.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { list, restore } });
            }
            return panel;
        }
    }
}
