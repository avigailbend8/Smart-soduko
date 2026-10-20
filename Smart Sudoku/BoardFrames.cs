using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml.Media;

namespace Smart_Sudoku
{
    /// <param name="Colors">Gradient colors for the border, or null for the theme's accent color.</param>
    /// <param name="Corner">An emoji in each corner of the board, or null.</param>
    public sealed record BoardFrame(string Id, string Name, int Price, double Thickness, uint[]? Colors, string? Corner);

    /// <summary>Frames around the Sudoku board, bought in the store.</summary>
    public static class BoardFrames
    {
        public static readonly IReadOnlyList<BoardFrame> All = new[]
        {
            new BoardFrame("classic", "קלאסית", 0, 3, null, null),
            new BoardFrame("gold", "זהב ✨", 100, 6, new[] { 0xFFF6D365u, 0xFFE0A526u, 0xFFFFE9A8u, 0xFFC98B16u }, "✨"),
            new BoardFrame("flowers", "פרחים 🌸", 100, 5, new[] { 0xFFF7A8C8u, 0xFFB7E4A8u }, "🌸"),
            new BoardFrame("hearts", "לבבות 💖", 100, 5, new[] { 0xFFFF7EB3u, 0xFFFF4F8Bu }, "💖"),
            new BoardFrame("rainbow", "קשת 🌈", 150, 6, new[] { 0xFFFF6B6Bu, 0xFFFFB86Bu, 0xFFFFE66Du, 0xFF6BD490u, 0xFF6BB8FFu, 0xFFB38BFFu }, "🌈"),
        };

        public static BoardFrame Get(string id) => All.FirstOrDefault(f => f.Id == id) ?? All[0];

        public static Brush Gradient(uint[] colors)
        {
            var brush = new LinearGradientBrush { StartPoint = new Windows.Foundation.Point(0, 0), EndPoint = new Windows.Foundation.Point(1, 1) };
            foreach (var stop in Themes.MakeStops(colors))
                brush.GradientStops.Add(stop);
            return brush;
        }
    }
}
