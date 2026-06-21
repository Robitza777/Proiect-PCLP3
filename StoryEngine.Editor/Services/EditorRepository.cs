using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using StoryEngine.Models;

namespace StoryEngine.Editor.Services
{
    /// <summary>
    /// Handles all disk I/O for the editor.
    ///
    /// The editor works on an extracted "project folder" (story.json + images/),
    /// per the recommendation in the spec (open ZIP -> temp folder -> edit -> rebuild ZIP).
    ///
    /// IMPORTANT: JsonOptions below must mirror StoryEngine.Repository.StoryRepository's
    /// settings exactly, so files produced here open correctly in the Player.
    /// </summary>
    public class EditorRepository
    {
        private const string StoryEntryName = "story.json";
        private const string ImagesFolderName = "images";

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        public static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };

        // ── Working folder management ──────────────────────────────────

        /// <summary>Creates a brand-new, empty working folder for a story created from scratch.</summary>
        public string CreateNewProjectFolder()
        {
            string dir = Path.Combine(Path.GetTempPath(), "StoryEditor_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            Directory.CreateDirectory(Path.Combine(dir, ImagesFolderName));
            return dir;
        }

        /// <summary>Extracts an existing .zip story into a fresh temp working folder.</summary>
        public string ExtractToProjectFolder(string zipPath)
        {
            string dir = CreateNewProjectFolder();
            using var zip = ZipFile.OpenRead(zipPath);
            foreach (var entry in zip.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name)) continue; // directory entry, nothing to extract
                string destPath = Path.Combine(dir, entry.FullName.Replace('/', Path.DirectorySeparatorChar));
                string? destDir = Path.GetDirectoryName(destPath);
                if (!string.IsNullOrEmpty(destDir)) Directory.CreateDirectory(destDir);
                entry.ExtractToFile(destPath, overwrite: true);
            }
            Directory.CreateDirectory(Path.Combine(dir, ImagesFolderName));
            return dir;
        }

        /// <summary>Deletes the temp working folder (call when opening another story or closing).</summary>
        public void CleanupProjectFolder(string? projectDir)
        {
            if (string.IsNullOrEmpty(projectDir)) return;
            try { if (Directory.Exists(projectDir)) Directory.Delete(projectDir, recursive: true); }
            catch { /* best effort cleanup, non-critical */ }
        }

        // ── story.json ──────────────────────────────────────────────────

        public StoryDefinition LoadStory(string projectDir)
        {
            string jsonPath = Path.Combine(projectDir, StoryEntryName);
            if (!File.Exists(jsonPath))
                throw new FileNotFoundException("Arhiva nu conține un fișier 'story.json' valid.");

            string json = File.ReadAllText(jsonPath, Encoding.UTF8);
            return JsonSerializer.Deserialize<StoryDefinition>(json, JsonOptions)
                ?? throw new InvalidDataException("Fișierul 'story.json' nu a putut fi interpretat.");
        }

        public void SaveStoryJson(StoryDefinition story, string projectDir)
        {
            string jsonPath = Path.Combine(projectDir, StoryEntryName);
            File.WriteAllText(jsonPath, JsonSerializer.Serialize(story, JsonOptions), Encoding.UTF8);
        }

        // ── ZIP export ───────────────────────────────────────────────────

        /// <summary>Packs story.json + images/ from the working folder into the final .zip package.</summary>
        public void ExportZip(string projectDir, string zipPath)
        {
            string jsonPath = Path.Combine(projectDir, StoryEntryName);
            if (!File.Exists(jsonPath))
                throw new FileNotFoundException("Nu există story.json în folderul de lucru.");

            string tempZip = zipPath + ".tmp";
            if (File.Exists(tempZip)) File.Delete(tempZip);

            using (var zip = ZipFile.Open(tempZip, ZipArchiveMode.Create))
            {
                zip.CreateEntryFromFile(jsonPath, StoryEntryName);

                string imagesDir = GetImagesDir(projectDir);
                foreach (var file in Directory.GetFiles(imagesDir))
                {
                    string entryName = ImagesFolderName + "/" + Path.GetFileName(file);
                    zip.CreateEntryFromFile(file, entryName);
                }
            }

            if (File.Exists(zipPath)) File.Delete(zipPath);
            File.Move(tempZip, zipPath);
        }

        // ── images/ ──────────────────────────────────────────────────────

        public string GetImagesDir(string projectDir)
        {
            string dir = Path.Combine(projectDir, ImagesFolderName);
            Directory.CreateDirectory(dir);
            return dir;
        }

        public List<string> ListImages(string projectDir)
        {
            string dir = GetImagesDir(projectDir);
            return Directory.GetFiles(dir)
                .Select(Path.GetFileName)
                .Where(n => !string.IsNullOrEmpty(n))
                .Select(n => n!)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Copies an external image file into the project's images/ folder (renaming on
        /// collision) and returns the filename that should be stored in the JSON.
        /// If the file is already inside the project's images folder, it is reused as-is.
        /// </summary>
        public string ImportImage(string projectDir, string sourceFilePath)
        {
            string imagesDir = GetImagesDir(projectDir);
            string fileName = Path.GetFileName(sourceFilePath);
            string destPath = Path.Combine(imagesDir, fileName);

            if (PathsPointToSameFile(sourceFilePath, destPath))
                return fileName;

            if (File.Exists(destPath))
            {
                string nameOnly = Path.GetFileNameWithoutExtension(fileName);
                string ext = Path.GetExtension(fileName);
                int i = 1;
                do
                {
                    fileName = $"{nameOnly}_{i}{ext}";
                    destPath = Path.Combine(imagesDir, fileName);
                    i++;
                } while (File.Exists(destPath));
            }

            File.Copy(sourceFilePath, destPath, overwrite: true);
            return fileName;
        }

        private static bool PathsPointToSameFile(string a, string b)
        {
            try { return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase); }
            catch { return false; }
        }
    }
}
