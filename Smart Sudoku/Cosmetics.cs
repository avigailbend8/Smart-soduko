using System.Collections.Generic;
using System.Linq;

namespace Smart_Sudoku
{
    /// <summary>A mascot for the game screen: its own face for each mood.</summary>
    public sealed record MascotSet(string Id, string Name, int Price, Dictionary<string, string> Faces);

    /// <summary>A ring and a little badge around the profile picture.</summary>
    public sealed record AvatarFrame(string Id, string Name, int Price, uint[] Colors, string? Badge);

    /// <summary>
    /// Mascots (the face next to the timer) and profile picture frames, sold in the store.
    /// </summary>
    public static class Cosmetics
    {
        // The game asks for the classic face of a mood; each mascot swaps in its own
        private static Dictionary<string, string> Faces(string happy, string think, string party, string sad, string wow, string glad, string wink, string star) =>
            new Dictionary<string, string>
            {
                ["😊"] = happy, ["🤔"] = think, ["🥳"] = party, ["😢"] = sad,
                ["😮"] = wow, ["😄"] = glad, ["😉"] = wink, ["🤩"] = star,
            };

        public static readonly IReadOnlyList<MascotSet> Mascots = new[]
        {
            new MascotSet("classic", "סמיילי", 0, Faces("😊", "🤔", "🥳", "😢", "😮", "😄", "😉", "🤩")),
            new MascotSet("cat", "חתלתולה", 250, Faces("😺", "🐱", "😻", "😿", "🙀", "😸", "😼", "😻")),
            new MascotSet("bunny", "ארנבונת", 250, Faces("🐰", "🐇", "🥕", "🥺", "🐰", "🐰", "🐇", "🌟")),
            new MascotSet("unicorn", "חד-קרן", 400, Faces("🦄", "💭", "🌈", "☁️", "✨", "🦄", "💫", "🌈")),
            new MascotSet("bear", "דובי", 300, Faces("🐻", "🍯", "🎉", "🐻‍❄️", "😲", "🐻", "🐼", "⭐")),
            new MascotSet("heart", "לבבות", 200, Faces("💗", "💭", "💖", "💔", "💓", "💕", "💘", "💝")),
        };

        public static MascotSet CurrentMascot =>
            Mascots.FirstOrDefault(m => m.Id == GameData.GetExtra("mascot", "classic")) ?? Mascots[0];

        /// <summary>The current mascot's face for a mood (given as its classic smiley).</summary>
        public static string Face(string classic) =>
            CurrentMascot.Faces.TryGetValue(classic, out string? face) ? face : classic;

        public static readonly IReadOnlyList<AvatarFrame> AvatarFrames = new[]
        {
            new AvatarFrame("none", "פשוטה", 0, new uint[] { 0xFFE56B9A }, null),
            new AvatarFrame("gold", "זהב", 300, new uint[] { 0xFFFFD54F, 0xFFFFA000, 0xFFFFE082 }, "👑"),
            new AvatarFrame("rainbow", "קשת", 400, new uint[] { 0xFFFF6B6B, 0xFFFFD93D, 0xFF6BCB77, 0xFF4D96FF, 0xFFB86BFF }, "🌈"),
            new AvatarFrame("flowers", "פרחים", 250, new uint[] { 0xFFFFB3C6, 0xFFFF85A1 }, "🌸"),
            new AvatarFrame("stars", "כוכבים", 350, new uint[] { 0xFF7C4DFF, 0xFF40C4FF }, "⭐"),
            new AvatarFrame("diamond", "יהלום", 600, new uint[] { 0xFFB2EBF2, 0xFF4DD0E1, 0xFFE1F5FE }, "💎"),
        };

        public static AvatarFrame FrameFor(string? id) =>
            AvatarFrames.FirstOrDefault(f => f.Id == id) ?? AvatarFrames[0];

        public static string CurrentFrameId => GameData.GetExtra("avatarFrame", "none");
    }
}
