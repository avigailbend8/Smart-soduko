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
    {
        private const string FolderToken = "backupFolder";
        private const string LastBackupKey = "backup.last";
        private const int KeepBackups = 10;

        private static IDictionary<string, object> Settings => ApplicationData.Current.LocalSettings.Values;

        private static string Today => DateTime.Now.ToString("yyyy-MM-dd");

        public static bool HasFolder => StorageApplicationPermissions.FutureAccessList.ContainsItem(FolderToken);

        public static string LastBackup => Settings.TryGetValue(LastBackupKey, out object? value) ? value as string ?? "" : "";

        private static async Task<StorageFolder?> FolderAsync()
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
        {
            var picker = new FolderPicker { SuggestedStartLocation = PickerLocationId.DocumentsLibrary };
            picker.FileTypeFilter.Add("*");
            var folder = await picker.PickSingleFolderAsync();
            if (folder == null)
                return null;
            StorageApplicationPermissions.FutureAccessList.AddOrReplace(FolderToken, folder);
            await BackupNowAsync();
            return folder.Path;
        }

        public static async Task<string> FolderPathAsync() => (await FolderAsync())?.Path ?? "";

        /// <summary>Copies the database (and the guest's progress) into the backup folder. Returns false if no folder is set.</summary>
        public static async Task<bool> BackupNowAsync()
        {
            var folder = await FolderAsync();
            if (folder == null)
                return false;

            string stamp = DateTime.Now.ToString("yyyy-MM-dd-HHmm");
            Database.CloseAll();
            var db = await StorageFile.GetFileFromPathAsync(Database.FilePath);
            await db.CopyAsync(folder, $"smartsudoku-{stamp}.db", NameCollisionOption.ReplaceExisting);

            var guestFile = await folder.CreateFileAsync($"guest-{stamp}.json", CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(guestFile, JsonSerializer.Serialize(GuestStore.Load(), AppJson.Default.PlayerProgress));

            Settings[LastBackupKey] = Today;
            await DeleteOldAsync(folder);
            return true;
        }

        /// <summary>Called when the app starts: one backup a day, if a folder was chosen.</summary>
        public static async Task RunDailyAsync()
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
        {
            var folder = await FolderAsync();
            if (folder == null)
                return new List<string>();
            var files = await folder.GetFilesAsync();
            return files.Where(f => f.Name.StartsWith("smartsudoku-") && f.Name.EndsWith(".db"))
                .Select(f => f.Name.Substring("smartsudoku-".Length, f.Name.Length - "smartsudoku-".Length - 3))
                .OrderByDescending(stamp => stamp)
                .ToList();
        }

        /// <summary>Puts a backup back in place of the current data.</summary>
        public static async Task<bool> RestoreAsync(string stamp)
        {
            var folder = await FolderAsync();
            if (folder == null)
                return false;

            Database.CloseAll();
            var backupDb = await folder.GetFileAsync($"smartsudoku-{stamp}.db");
            await backupDb.CopyAsync(ApplicationData.Current.LocalFolder, "smartsudoku.db", NameCollisionOption.ReplaceExisting);

            if (await folder.TryGetItemAsync($"guest-{stamp}.json") is StorageFile guestFile)
            {
                var guest = JsonSerializer.Deserialize(await FileIO.ReadTextAsync(guestFile), AppJson.Default.PlayerProgress);
                if (guest != null)
                    GuestStore.Save(guest);
            }
            return true;
        }

        private static async Task DeleteOldAsync(StorageFolder folder)
        {
            var stamps = await ListAsync();
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
