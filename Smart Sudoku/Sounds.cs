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
    // אובייקט: Sounds — מנגן את הצלילים והמוזיקה של המשחק (מהתיקייה Assets/Sounds), רק אם הצלילים פועלים.
    {
        private static readonly Dictionary<string, MediaPlayer> Players = new Dictionary<string, MediaPlayer>();
        // אובייקט: Players — "נגנים" מוכנים לכל צליל, כדי שלא ניצור נגן חדש בכל פעם.

        // ---------- Background music ----------

        private static MediaPlayer? music;
        // אובייקט: music — הנגן של מוזיקת הרקע.
        private static string? musicTrack;
        // אובייקט: musicTrack — שם השיר שמתנגן עכשיו (או null כשאין מוזיקה).

        /// <summary>Plays a looping music track (Assets/Sounds/music-NAME.wav), or stops the music with null.</summary>
        public static void Music(string? track)
        // פעולה: Music — מנגנת מוזיקת רקע בלופ (חוזרת שוב ושוב). כשמקבלת null — מפסיקה את המוזיקה.
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
                // אובייקט: music (החדש) — נגן שמנגן את השיר שוב ושוב, בעוצמה נמוכה (35%).
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
        // פעולה: ApplyMusicSetting — מפעילה את המוזיקה הרגועה אם השחקן הפעיל מוזיקה, ואחרת מכבה אותה.

        public static void Play(string name)
        // פעולה: Play — משמיעה צליל קצר לפי השם שלו (למשל "coin"). אם הצליל חסר — המשחק ממשיך כרגיל.
        {
            if (!GameData.SoundOn)
                return;

            try
            {
                if (!Players.TryGetValue(name, out var player))
                {
                    player = new MediaPlayer
                    // אובייקט: player — נגן חדש לצליל הזה, שנשמר לפעם הבאה.
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
