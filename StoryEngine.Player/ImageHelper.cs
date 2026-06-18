using System.Drawing;
using System.IO;
using StoryEngine.Repository;

namespace StoryEngine.Player
{
    /// <summary>
    /// Utility pentru încărcarea imaginilor din ZIP în Player.
    /// System.Drawing există doar în proiectul Player, nu în Core.
    /// </summary>
    internal static class ImageHelper
    {
        /// <summary>
        /// Încarcă o imagine din ZIP ca Bitmap în memorie.
        /// Returnează null dacă fișierul nu există sau apare o eroare.
        /// Nu lasă niciun file handle deschis.
        /// </summary>
        public static Image LoadFromZip(StoryRepository repo, string zipPath, string imageFilename)
        {
            if (string.IsNullOrEmpty(imageFilename)) return null;

            byte[] bytes = repo.LoadImageBytesFromZip(zipPath, imageFilename);
            if (bytes == null || bytes.Length == 0) return null;

            using var ms = new MemoryStream(bytes);
            return new Bitmap(ms);   // Bitmap copiază datele intern — stream poate fi închis
        }

        /// <summary>
        /// Derivă numele fișierului iconiței dintr-o cheie de proprietate item.
        /// Ex: "item.lantern" → "lantern.png"
        /// Folosit ca fallback când HudIcon nu e setat în JSON.
        /// </summary>
        public static string IconFileFromItemKey(string propertyKey)
        {
            const string prefix = "item.";
            if (propertyKey != null && propertyKey.StartsWith(prefix))
                return propertyKey.Substring(prefix.Length) + ".png";
            return null;
        }
    }
}
