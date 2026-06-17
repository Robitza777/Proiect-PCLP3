using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using StoryEngine.Models;

namespace StoryEngine.Repository
{
    /// <summary>
    /// Handles loading and saving StoryDefinition (ZIP + JSON) and GameState (JSON save files).
    /// The ZIP contains:
    ///   story.json  — the StoryDefinition
    ///   images/     — optional background images referenced by blocks
    /// </summary>
    public class StoryRepository
    {
        private const string StoryEntryName = "story.json";
        private const string SaveEntryName  = "save.json";

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        // ------------------------------------------------------------------ //
        //  Story (ZIP)
        // ------------------------------------------------------------------ //

        public void SaveStory(StoryDefinition story, string zipPath)
        {
            // Overwrite existing file
            if (File.Exists(zipPath)) File.Delete(zipPath);

            using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
            var entry = zip.CreateEntry(StoryEntryName);
            using var stream = entry.Open();
            using var writer = new StreamWriter(stream, Encoding.UTF8);
            writer.Write(JsonSerializer.Serialize(story, JsonOptions));
        }

        public StoryDefinition LoadStory(string zipPath)
        {
            using var zip = ZipFile.OpenRead(zipPath);
            var entry = zip.GetEntry(StoryEntryName)
                ?? throw new FileNotFoundException($"'{StoryEntryName}' not found inside ZIP.");

            using var stream = entry.Open();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            string json = reader.ReadToEnd();
            return JsonSerializer.Deserialize<StoryDefinition>(json, JsonOptions)
                ?? throw new InvalidDataException("Failed to deserialize story.");
        }

        // ------------------------------------------------------------------ //
        //  Game state (save / load)
        // ------------------------------------------------------------------ //

        public void SaveGameState(GameState state, string savePath)
        {
            string json = JsonSerializer.Serialize(state, JsonOptions);
            File.WriteAllText(savePath, json, Encoding.UTF8);
        }

        public GameState LoadGameState(string savePath)
        {
            string json = File.ReadAllText(savePath, Encoding.UTF8);
            return JsonSerializer.Deserialize<GameState>(json, JsonOptions)
                ?? throw new InvalidDataException("Failed to deserialize game state.");
        }

        // ------------------------------------------------------------------ //
        //  Embedded image extraction helper (used by the Player)
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Extracts an image from the story ZIP into a temp file and returns the path.
        /// Returns null if the image entry doesn't exist.
        /// </summary>
        public string ExtractImage(string zipPath, string imageFilename)
        {
            using var zip = ZipFile.OpenRead(zipPath);
            string entryName = $"images/{imageFilename}";
            var entry = zip.GetEntry(entryName);
            if (entry == null) return null;

            string tempPath = Path.Combine(Path.GetTempPath(), imageFilename);
            entry.ExtractToFile(tempPath, overwrite: true);
            return tempPath;
        }
    }
}
