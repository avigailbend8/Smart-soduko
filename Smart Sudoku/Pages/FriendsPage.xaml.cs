using System;
using System.Collections.Generic;
using System.Linq;
using Smart_Sudoku.Duel;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Smart_Sudoku.Pages
{
    /// <summary>
    /// The player's friends: add by user name, see who is online in the duel server, invite to a duel, send coins.
    /// </summary>
    public sealed partial class FriendsPage : BasePage
    // אובייקט: FriendsPage — עמוד החברים: מוסיפים חבר לפי שם המשתמש, רואים מי מחובר לשרת הדו-קרב, מזמינים לדו-קרב ושולחים מטבעות במתנה.
    {
        public const int GiftCoins = 50;
        // אובייקט: GiftCoins — כמה מטבעות יש במתנה אחת לחבר (50).

        // Who is connected to the duel server right now (by name), when we're connected too
        private Dictionary<string, OnlinePlayer> online = new Dictionary<string, OnlinePlayer>(StringComparer.OrdinalIgnoreCase);
        // אובייקט: online — מי מחובר עכשיו לשרת הדו-קרב (לפי השם), כשגם אנחנו מחוברים.

        private TextBox? nameBox;
        // אובייקט: nameBox — תיבת הטקסט שבה מקלידים את השם של החבר החדש.
        private TextBlock? messageText;
        // אובייקט: messageText — שורת ההודעה שמופיעה מתחת לתיבה (למשל "ההזמנה נשלחה").

        public FriendsPage()
        // פעולה: FriendsPage (בנאי) — טוענת את העיצוב של העמוד מקובץ ה-XAML.
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        // פעולה: OnNavigatedTo — רצה כשנכנסים לעמוד: בונה את הרשימה, ואם מחוברים לשרת — שואלת מי מחובר עכשיו.
        {
            base.OnNavigatedTo(e);
            Refresh();

            if (Client.Current is Client client && client.IsConnected)
            // אובייקט: client — החיבור לשרת הדו-קרב.
            {
                client.OnlineReceived += OnOnlineReceived;
                try
                {
                    await client.GetOnlineAsync();
                }
                catch (Exception)
                {
                    // Not online after all; the list just won't show who's connected
                }
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        // פעולה: OnNavigatedFrom — רצה כשיוצאים מהעמוד: מפסיקה להקשיב לרשימת המחוברים.
        {
            base.OnNavigatedFrom(e);
            if (Client.Current is Client client)
                client.OnlineReceived -= OnOnlineReceived;
        }

        private async void OnOnlineReceived(List<OnlinePlayer> players) =>
        // פעולה: OnOnlineReceived — רצה כשהשרת שולח את רשימת המחוברים: שומרת אותה ובונה את העמוד מחדש.
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                // Name -> player (if the same name shows up twice, the first one is kept)
                online = new Dictionary<string, OnlinePlayer>(StringComparer.OrdinalIgnoreCase);
                foreach (var player in players)
                {
                    if (!online.ContainsKey(player.Name))
                        online[player.Name] = player;
                }
                Refresh();
            });

        private async void Refresh()
        // פעולה: Refresh — בונה מחדש את כל העמוד: תיבה להוספת חבר, ואז כרטיס לכל חבר עם תמונה, מצב (מחובר או לא) וכפתורים:
        // להזמין לדו-קרב, לשלוח מתנה ולהסיר מהרשימה. חברים מחוברים מופיעים ראשונים.
        {
            ContentPanel.Children.Clear();
            if (Session.UserId is not int me)
            {
                SubtitleText.Text = "חברים זמינים רק למשתמשים רשומים. נרשמים בתפריט הראשי ואז מוסיפים חברים! 💌";
                return;
            }

            bool connected = Client.Current?.IsConnected == true;
            // אובייקט: connected — אמת אם אנחנו מחוברים לשרת הדו-קרב.
            SubtitleText.Text = connected
                ? "מחוברים לשרת הדו-קרב: רואים מי מחובר ואפשר להזמין לדו-קרב ⚔️"
                : "כדי לראות מי מחובר ולהזמין לדו-קרב, צריך קודם להיכנס לחדר בדו-קרב ברשת.";

            // Add a friend
            nameBox = new TextBox { PlaceholderText = "שם המשתמש של החבר/ה", FontSize = 17, CornerRadius = new CornerRadius(12) };
            nameBox.KeyDown += (s, e) =>
            {
                if (e.Key == VirtualKey.Enter)
                    AddFriend();
            };
            var add = new Button { Content = "➕ להוסיף", FontSize = 16, MinWidth = 110 };
            // אובייקט: add — הכפתור "להוסיף".
            add.Click += (s, e) => AddFriend();
            messageText = Ui.Text("", 15, "BerryBrush", bold: true);
            ContentPanel.Children.Add(Ui.Card(new StackPanel { Spacing = 10, Children = { Ui.Row(nameBox, add), messageText } }));

            var friends = Database.GetFriends(me);
            // אובייקט: friends — רשימת החברים מתוך מסד הנתונים.
            if (friends.Count == 0)
            {
                ContentPanel.Children.Add(Ui.Text("עוד אין חברים ברשימה. מוסיפים לפי שם המשתמש שלהם 💕", 16, "MutedTextBrush"));
                return;
            }

            // Online friends first
            foreach (string friend in friends.OrderByDescending(f => online.ContainsKey(f)))
            {
                // A friend registered on this computer has a picture and a frame
                int? friendId = Database.FindUser(friend);
                // אובייקט: friendId — המספר של החבר אם הוא רשום במחשב הזה (או null).
                Windows.UI.Xaml.Media.ImageSource? picture = null;
                // אובייקט: picture — תמונת הפרופיל של החבר.
                string? frame = null;
                // אובייקט: frame — המסגרת של תמונת הפרופיל של החבר.
                if (friendId != null)
                {
                    picture = await Avatars.LoadAsync(friendId.Value);
                    frame = Database.GetSetting(friendId.Value, "avatarFrame");
                }
                var avatar = Avatars.Circle(picture, friend, 52, frame);
                // אובייקט: avatar — העיגול עם התמונה של החבר.

                string status = FriendStatus(friend, connected);
                // אובייקט: status — מה ידוע על החבר בשרת: מחובר, באמצע משחק, או לא מחובר.

                var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                // אובייקט: buttons — שורת הכפתורים של החבר.
                if (online.ContainsKey(friend))
                {
                    var invite = new Button { Content = "⚔️ להזמין", FontSize = 15 };
                    // אובייקט: invite — הכפתור "להזמין" לדו-קרב (רק לחבר מחובר).
                    invite.Click += async (s, e) =>
                    {
                        if (Client.Current is Client client)
                        {
                            await client.InviteAsync(friend);
                            Say($"ההזמנה נשלחה ל{friend}! 💌");
                        }
                    };
                    buttons.Children.Add(invite);
                }
                if (friendId != null)
                {
                    var gift = new Button { Content = $"🎁 {GiftCoins} 🪙", FontSize = 15 };
                    // אובייקט: gift — הכפתור "מתנה" (רק לחבר שרשום במחשב הזה).
                    gift.Click += (s, e) => SendGift(friend);
                    buttons.Children.Add(gift);
                }
                var remove = new Button { Content = "✖", FontSize = 15 };
                // אובייקט: remove — הכפתור ✖ שמסיר את החבר מהרשימה.
                ToolTipService.SetToolTip(remove, "להסיר מהרשימה");
                remove.Click += (s, e) =>
                {
                    Database.RemoveFriend(me, friend);
                    Refresh();
                };
                buttons.Children.Add(remove);

                var info = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
                // אובייקט: info — השם והמצב של החבר, זה מתחת לזה.
                info.Children.Add(Ui.Text(friend, 19, bold: true));
                if (status.Length > 0)
                    info.Children.Add(Ui.Text(status, 14, "MutedTextBrush"));
                ContentPanel.Children.Add(Ui.Card(Ui.Row(info, buttons, avatar), highlight: online.ContainsKey(friend)));
            }
        }

        // What we know about the friend on the duel server (empty when we aren't connected)
        private string FriendStatus(string friend, bool connected)
        // פעולה: FriendStatus — מחזירה טקסט על מצב החבר בשרת: מחובר, באמצע משחק, או לא מחובר (ריק אם אנחנו לא מחוברים).
        {
            if (online.TryGetValue(friend, out var player))
            {
                if (player.Status == PlayerStatus.Playing)
                    return "🟠 באמצע משחק";
                if (player.Room.Length > 0)
                    return $"🟢 מחובר/ת · חדר {player.Room}";
                return "🟢 מחובר/ת";
            }
            if (connected)
                return "⚪ לא מחובר/ת";
            return "";
        }

        private void AddFriend()
        // פעולה: AddFriend — מוסיפה את השם שהוקלד לרשימת החברים (אבל לא את עצמך!), ופותחת את ההישג "חברה טובה".
        {
            if (Session.UserId is not int me || nameBox == null)
                return;
            string name = nameBox.Text.Trim();
            // אובייקט: name — השם שהוקלד.
            if (name.Length == 0)
                return;
            if (string.Equals(name, Session.PlayerName, StringComparison.OrdinalIgnoreCase))
            {
                Say("זה השם שלך 😄");
                return;
            }

            Database.AddFriend(me, name);
            Achievements.Unlock("friend");
            Sounds.Play("coin");
            Refresh();
            Say(Database.FindUser(name) == null
                ? $"{name} נוסף/ה! (לא רשום/ה במחשב הזה, אז אפשר רק להזמין לדו-קרב ברשת)"
                : $"{name} נוסף/ה לחברים! 💕");
        }

        private void SendGift(string friend)
        // פעולה: SendGift — שולחת לחבר מתנה של 50 מטבעות. אם החבר לא נמצא — מחזירה את המטבעות.
        {
            if (!GameData.Spend(GiftCoins))
            {
                Say("אין מספיק מטבעות למתנה 😅");
                return;
            }
            if (!Database.GiveCoins(friend, GiftCoins))
            {
                GameData.AddCoins(GiftCoins);   // give them back
                Say("לא מצאנו את המשתמש הזה במחשב");
                return;
            }
            Sounds.Play("coin");
            Say($"שלחת ל{friend} מתנה של {GiftCoins} 🪙! 🎁");
        }

        private void Say(string text)
        // פעולה: Say — מציגה הודעה בשורת ההודעות.
        {
            if (messageText != null)
                messageText.Text = text;
        }
    }
}
