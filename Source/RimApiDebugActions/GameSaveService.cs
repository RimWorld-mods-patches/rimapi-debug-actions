using System;
using System.IO;
using Verse;

namespace RimApiDebugActions
{
    /// <summary>Saves the running game under a plain save name, for test setup such as creating a reset save.</summary>
    internal sealed class GameSaveService
    {
        public object Save(string name)
        {
            name = (name ?? string.Empty).Trim();
            if (name.Length == 0 || name != GenFile.SanitizedFileName(name) || name != Path.GetFileName(name))
                return new { success = false, error = "The save name must be a plain file name without extension.", name };
            if (Current.ProgramState != ProgramState.Playing || Current.Game == null)
                return new { success = false, error = "No game is being played.", name };
            if (GameDataSaveLoader.SavingIsTemporarilyDisabled)
                return new { success = false, error = "Saving is temporarily disabled.", name };

            var path = GenFilePaths.FilePathForSavedGame(name);
            var before = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
            // Vanilla logs and swallows save exceptions; the written file is the evidence.
            GameDataSaveLoader.SaveGame(name);
            var saved = File.Exists(path) && File.GetLastWriteTimeUtc(path) >= before && new FileInfo(path).Length > 0;
            return saved
                ? (object)new { success = true, name, path, bytes = new FileInfo(path).Length, tick = Find.TickManager.TicksGame }
                : new { success = false, error = "The game did not write the save; see the game log.", name, path };
        }
    }
}
