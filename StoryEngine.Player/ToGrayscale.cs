using System.Drawing;
using System.Drawing.Imaging;

namespace StoryEngine.Player
{
    internal static class ImageEffects
    {
        /// <summary>
        /// Convertește o imagine color în grayscale (folosit pentru iconițele
        /// de pe butoanele de decizie dezactivate).
        /// </summary>
        public static Image ToGrayscale(Image original)
        {
            var bmp = new Bitmap(original.Width, original.Height);

            // Matrice standard de conversie luminanță (NTSC weights)
            var colorMatrix = new ColorMatrix(new float[][]
            {
                new float[] {0.299f, 0.299f, 0.299f, 0, 0},
                new float[] {0.587f, 0.587f, 0.587f, 0, 0},
                new float[] {0.114f, 0.114f, 0.114f, 0, 0},
                new float[] {0,      0,      0,      1, 0},
                new float[] {0,      0,      0,      0, 1}
            });

            using var attributes = new ImageAttributes();
            attributes.SetColorMatrix(colorMatrix);

            using var g = Graphics.FromImage(bmp);
            g.DrawImage(original,
                new Rectangle(0, 0, original.Width, original.Height),
                0, 0, original.Width, original.Height,
                GraphicsUnit.Pixel,
                attributes);

            return bmp;
        }
    }
}