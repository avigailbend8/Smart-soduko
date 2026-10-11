using System;
using System.Collections.Generic;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace Smart_Sudoku
{
    /// <summary>
    /// Short sound effects from Assets/Sounds. Respects the player's sound setting.
    /// </summary>
    public static class Sounds
    {
        private static readonly Dictionary<string, MediaPlayer> Players = new Dictionary<string, MediaPlayer>();

        public static void Play(string name)
        {
            if (!GameData.SoundOn)
                return;

            try
            {
                if (!Players.TryGetValue(name, out var player))
                {
                    player = new MediaPlayer
                    {
                        Source = MediaSource.CreateFromUri(new Uri($"ms-appx:///Assets/Sounds/{name}.wav")),
                        AudioCategory = MediaPlayerAudioCategory.GameEffects,
                    };
                    Players[name] = player;
                }

                player.PlaybackSession.Position = TimeSpan.Zero;
                player.Play();
            }
            catch (Exception)
            {
                // A missing sound should never break the game
            }
        }
    }
}
