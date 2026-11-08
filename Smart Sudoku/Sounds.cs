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

        // ---------- Background music ----------

        private static MediaPlayer? music;
        private static string? musicTrack;

        /// <summary>Plays a looping music track (Assets/Sounds/music-NAME.wav), or stops the music with null.</summary>
        public static void Music(string? track)
        {
            if (track == musicTrack)
                return;
            music?.Pause();
            music = null;
            musicTrack = track;
            if (track == null)
                return;

            try
            {
                music = new MediaPlayer
                {
                    Source = MediaSource.CreateFromUri(new Uri($"ms-appx:///Assets/Sounds/music-{track}.wav")),
                    IsLoopingEnabled = true,
                    Volume = 0.35,
                    AudioCategory = MediaPlayerAudioCategory.GameMedia,
                };
                music.Play();
            }
            catch (Exception)
            {
                music = null;
            }
        }

        /// <summary>The calm background music, if the player turned music on.</summary>
        public static void ApplyMusicSetting() => Music(GameData.MusicOn && GameData.SoundOn ? "calm" : null);

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
