using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;

namespace Smart_Sudoku
{
    /// <summary>
    /// Backups of the game's data in a folder the player chooses (outside the app), so nothing is lost
    /// if Windows reinstalls the app. Once a folder is chosen, a backup is made automatically every day.
    /// </summary>
    public static class Backup
    // אובייקט: Backup — גיבויים של המידע של המשחק בתיקייה שהשחקן בוחר, כדי ששום דבר לא יאבד. אחרי שבוחרים תיקייה — יש גיבוי אוטומטי כל יום.
    {
        private const string FolderToken = "backupFolder";
        // אובייקט: FolderToken — השם שתחתיו Windows זוכרת את תיקיית הגיבוי.
        private const string LastBackupKey = "backup.last";
        // אובייקט: LastBackupKey — השם שתחתיו נשמר התאריך של הגיבוי האחרון.
        private const int KeepBackups = 10;
        // אובייקט: KeepBackups — כמה גיבויים לשמור (10). ישנים יותר נמחקים.

        private static IDictionary<string, object> Settings => ApplicationData.Current.LocalSettings.Values;
        // אובייקט: Settings — ההגדרות המקומיות של האפליקציה במחשב.

        private static string Today => DateTime.Now.ToString("yyyy-MM-dd");
        // אובייקט: Today — התאריך של היום, כטקסט.

        public static bool HasFolder => StorageApplicationPermissions.FutureAccessList.ContainsItem(FolderToken);
        // אובייקט: HasFolder — אמת אם השחקן כבר בחר תיקיית גיבוי.

        public static string LastBackup => Settings.TryGetValue(LastBackupKey, out object? value) ? value as string ?? "" : "";
        // אובייקט: LastBackup — התאריך של הגיבוי האחרון (או ריק).

        private static async Task<StorageFolder?> FolderAsync()
        // פעולה: FolderAsync — מחזירה את תיקיית הגיבוי, או null אם אין תיקייה או שהיא נמחקה או הועברה.
        {
            if (!HasFolder)
                return null;
            try
            {
                return await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(FolderToken);
            }
            catch (Exception)
            {
                return null;   // the folder was moved or deleted
            }
        }

        /// <summary>Lets the player pick the backup folder. Returns its name, or null if cancelled.</summary>
        public static async Task<string?> ChooseFolderAsync()
        // פעולה: ChooseFolderAsync — פותחת חלון שבו השחקן בוחר תיקייה לגיבוי, ועושה מיד גיבוי ראשון. מחזירה את המיקום, או null אם ביטלו.
        {
            var picker = new FolderPicker { SuggestedStartLocation = PickerLocationId.DocumentsLibrary };
            // אובייקט: picker — חלון בחירת התיקייה של Windows.
            picker.FileTypeFilter.Add("*");
            var folder = await picker.PickSingleFolderAsync();
            // אובייקט: folder — התיקייה שהשחקן בחר.
            if (folder == null)
                return null;
            StorageApplicationPermissions.FutureAccessList.AddOrReplace(FolderToken, folder);
            await BackupNowAsync();
            return folder.Path;
        }

        public static async Task<string> FolderPathAsync() => (await FolderAsync())?.Path ?? "";
        // פעולה: FolderPathAsync — מחזירה את המיקום של תיקיית הגיבוי, כדי להציג אותו.

        /// <summary>Copies the database (and the guest's progress) into the backup folder. Returns false if no folder is set.</summary>
        public static async Task<bool> BackupNowAsync()
        // פעולה: BackupNowAsync — מעתיקה עכשיו את מסד הנתונים (ואת ההתקדמות של האורח) לתיקיית הגיבוי, ומוחקת גיבויים ישנים.
        {
            var folder = await FolderAsync();
            // אובייקט: folder — תיקיית הגיבוי.
            if (folder == null)
                return false;

            string stamp = DateTime.Now.ToString("yyyy-MM-dd-HHmm");
            // אובייקט: stamp — תאריך ושעה לשם הקובץ, כדי שכל גיבוי יקבל שם אחר.
            Database.CloseAll();
            var db = await StorageFile.GetFileFromPathAsync(Database.FilePath);
            // אובייקט: db — קובץ מסד הנתונים של המשחק.
            await db.CopyAsync(folder, $"smartsudoku-{stamp}.db", NameCollisionOption.ReplaceExisting);

            Settings[LastBackupKey] = Today;
            await DeleteOldAsync(folder);
            return true;
        }

        /// <summary>Called when the app starts: one backup a day, if a folder was chosen.</summary>
        public static async Task RunDailyAsync()
        // פעולה: RunDailyAsync — רצה כשהאפליקציה נפתחת: עושה גיבוי אחד ביום, אם נבחרה תיקייה.
        {
            try
            {
                if (HasFolder && LastBackup != Today)
                    await BackupNowAsync();
            }
            catch (Exception)
            {
                // A failed backup must never stop the game
            }
        }

        /// <summary>The backups in the folder, newest first (just the date-time part of the name).</summary>
        public static async Task<List<string>> ListAsync()
        // פעולה: ListAsync — מחזירה את רשימת הגיבויים שבתיקייה, מהחדש לישן.
        {
            var folder = await FolderAsync();
            // אובייקט: folder — תיקיית הגיבוי.
            if (folder == null)
                return new List<string>();
            var files = await folder.GetFilesAsync();
            // אובייקט: files — כל הקבצים שבתיקייה.
            return files.Where(f => f.Name.StartsWith("smartsudoku-") && f.Name.EndsWith(".db"))
                .Select(f => f.Name.Substring("smartsudoku-".Length, f.Name.Length - "smartsudoku-".Length - 3))
                .OrderByDescending(stamp => stamp)
                .ToList();
        }

        /// <summary>Puts a backup back in place of the current data.</summary>
        public static async Task<bool> RestoreAsync(string stamp)
        // פעולה: RestoreAsync — מחזירה גיבוי ישן במקום המידע הנוכחי (שחזור).
        {
            var folder = await FolderAsync();
            // אובייקט: folder — תיקיית הגיבוי.
            if (folder == null)
                return false;

            Database.CloseAll();
            var backupDb = await folder.GetFileAsync($"smartsudoku-{stamp}.db");
            // אובייקט: backupDb — קובץ מסד הנתונים מהגיבוי שנבחר.
            await backupDb.CopyAsync(ApplicationData.Current.LocalFolder, "smartsudoku.db", NameCollisionOption.ReplaceExisting);
            return true;
        }

        private static async Task DeleteOldAsync(StorageFolder folder)
        // פעולה: DeleteOldAsync — מוחקת גיבויים ישנים, כך שנשארים רק 10 האחרונים.
        {
            var stamps = await ListAsync();
            // אובייקט: stamps — התאריכים של כל הגיבויים, מהחדש לישן.
            foreach (string stamp in stamps.Skip(KeepBackups))
            {
                foreach (string name in new[] { $"smartsudoku-{stamp}.db", $"guest-{stamp}.json" })
                {
                    if (await folder.TryGetItemAsync(name) is IStorageItem old)
                        await old.DeleteAsync();
                }
            }
        }
    }
}
