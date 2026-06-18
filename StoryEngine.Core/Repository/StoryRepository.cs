using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using StoryEngine.Models;

namespace StoryEngine.Repository
{
    public class StoryRepository
    {
        private const string StoryEntryName = "story.json";

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        // ── Story (ZIP) ───────────────────────────────────────────────────

        public void SaveStory(StoryDefinition story, string zipPath)
        {
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

        // ── Game state ────────────────────────────────────────────────────

        public void SaveGameState(GameState state, string savePath)
        {
            File.WriteAllText(savePath, JsonSerializer.Serialize(state, JsonOptions), Encoding.UTF8);
        }

        public GameState LoadGameState(string savePath)
        {
            string json = File.ReadAllText(savePath, Encoding.UTF8);
            return JsonSerializer.Deserialize<GameState>(json, JsonOptions)
                ?? throw new InvalidDataException("Failed to deserialize game state.");
        }

        // ── Image loading ─────────────────────────────────────────────────

        /// <summary>
        /// Loads an image entry from the ZIP as raw bytes.
        /// Returns null if the entry does not exist.
        /// Returning byte[] keeps this library UI-agnostic (no System.Drawing dependency).
        /// The caller (Player) constructs a Bitmap from the bytes.
        /// </summary>
        public byte[] LoadImageBytesFromZip(string zipPath, string imageFilename)
        {
            if (string.IsNullOrEmpty(zipPath) || string.IsNullOrEmpty(imageFilename))
                return null;

            try
            {
                using var zip = ZipFile.OpenRead(zipPath);
                var entry = zip.GetEntry($"images/{imageFilename}");
                if (entry == null) return null;

                using var zipStream = entry.Open();
                using var ms = new MemoryStream();
                zipStream.CopyTo(ms);
                return ms.ToArray();
            }
            catch
            {
                return null;
            }
        }
    }
}
