using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace StoryEngine.Editor
{
    /// <summary>
    /// StoryRepository.SaveStory/LoadStory (din Core) lucrează DOAR cu story.json — nu
    /// ambalează și folderul images/ în arhivă. Clasa asta completează exact partea
    /// care lipsea: ține imaginile într-un folder temporar de lucru pe durata editării,
    /// iar la salvare le adaugă în zip-ul deja creat de StoryRepository.
    ///
    /// Nu modifică nimic din StoryRepository — doar "completează" zip-ul după ce
    /// StoryRepository.SaveStory a scris deja story.json în el.
    /// </summary>
    public class ImageWorkspace
    {
        private const string ImagesFolderName = "images";
        public static readonly string ImageFileFilter = "Imagini (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif";

        /// <summary>Creează un folder temporar gol de lucru (pentru o poveste nouă).</summary>
        public string CreateNewWorkspace()
        {
            string dir = Path.Combine(Path.GetTempPath(), "StoryEditor_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            Directory.CreateDirectory(Path.Combine(dir, ImagesFolderName));
            return dir;
        }

        /// <summary>Extrage doar intrările din images/ ale unui zip existent într-un folder temporar nou.</summary>
        public string ExtractImagesFromZip(string zipPath)
        {
            string dir = CreateNewWorkspace();
            string imagesDir = GetImagesDir(dir);

            using var zip = ZipFile.OpenRead(zipPath);
            foreach (var entry in zip.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name)) continue; // intrare de folder, nimic de extras
                if (!entry.FullName.Replace('\\', '/').StartsWith(ImagesFolderName + "/", StringComparison.OrdinalIgnoreCase))
                    continue;

                string destPath = Path.Combine(imagesDir, entry.Name);
                entry.ExtractToFile(destPath, overwrite: true);
            }

            return dir;
        }

        /// <summary>Șterge folderul temporar de lucru (best-effort, nu aruncă excepție).</summary>
        public void CleanupWorkspace(string workspaceDir)
        {
            if (string.IsNullOrEmpty(workspaceDir)) return;
            try { if (Directory.Exists(workspaceDir)) Directory.Delete(workspaceDir, recursive: true); }
            catch { /* cleanup, nu e critic */ }
        }

        public string GetImagesDir(string workspaceDir)
        {
            string dir = Path.Combine(workspaceDir, ImagesFolderName);
            Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>
        /// Copiază un fișier extern în folderul de lucru (cu rename la coliziune) și
        /// returnează numele de fișier care trebuie reținut în story.json.
        /// </summary>
        public string ImportImage(string workspaceDir, string sourceFilePath)
        {
            string imagesDir = GetImagesDir(workspaceDir);
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

        /// <summary>
        /// Apelat DUPĂ _repo.SaveStory(story, zipPath) — reia zip-ul deja creat (care
        /// conține doar story.json) și adaugă în el toate fișierele din folderul de
        /// lucru, sub images/. Așa rezultă o arhivă completă, exact ca cea cerută în temă.
        /// </summary>
        public void AppendImagesToZip(string workspaceDir, string zipPath)
        {
            string imagesDir = GetImagesDir(workspaceDir);
            var files = Directory.GetFiles(imagesDir);
            if (files.Length == 0) return;

            using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Update);

            // Scoatem orice intrări images/ vechi rămase dintr-o salvare anterioară pe
            // același fișier, ca să nu rămână imagini orfane în arhivă.
            foreach (var oldEntry in zip.Entries.Where(en => en.FullName.Replace('\\', '/')
                         .StartsWith(ImagesFolderName + "/", StringComparison.OrdinalIgnoreCase)).ToList())
            {
                oldEntry.Delete();
            }

            foreach (var file in files)
            {
                string entryName = ImagesFolderName + "/" + Path.GetFileName(file);
                zip.CreateEntryFromFile(file, entryName);
            }
        }

        public string[] ListImages(string workspaceDir) => Directory.GetFiles(GetImagesDir(workspaceDir));

        private static bool PathsPointToSameFile(string a, string b)
        {
            try { return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase); }
            catch { return false; }
        }
    }
}
