using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using JetBrains.Annotations;
using G = System.Drawing.Graphics;

namespace MarukoLib.Graphics
{
    
    public static class BitmapUtils
    {

        [NotNull]
        public static Bitmap TakeScreenshot([NotNull] this Screen screen, [CanBeNull] Bitmap reusable = null) 
            => TakeScreenshot(screen.Bounds, PixelFormat.Format32bppArgb, reusable);

        [NotNull]
        public static Bitmap TakeScreenshot([NotNull] this Screen screen, PixelFormat format, [CanBeNull] Bitmap reusable = null) 
            => TakeScreenshot(screen.Bounds, format, reusable);

        [NotNull]
        public static Bitmap TakeScreenshot(Rectangle rectangle, PixelFormat format, [CanBeNull] Bitmap reusable = null)
        {
            var bounds = rectangle;
            if (reusable == null || reusable.PixelFormat != format || reusable.Size != bounds.Size)
                reusable = new Bitmap(bounds.Width, bounds.Height, format);
            using (var g = G.FromImage(reusable))
                g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
            return reusable;
        }

        [NotNull]
        public static Bitmap Copy([NotNull] this Image image)
        {
            int width= image.Width, height = image.Height;
            var output = new Bitmap(width, height, image.PixelFormat);
            using (var g = G.FromImage(output))
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.DrawImage(image, 0, 0, width, height);
            }
            return output;
        }

        [NotNull]
        public static Bitmap Scale([NotNull] this Image image, float ratio)
            => Scale(image, new Size((int)(image.Width * ratio), (int)(image.Height * ratio)), ScaleMode.ScaleToFill);

        [NotNull]
        public static Bitmap Scale([NotNull] this Image image, Size size, ScaleMode scaleMode, float position = 0)
            => image.Size == size && image is Bitmap bitmap ? bitmap : Scale(image, new BitmapFormat(size, image.PixelFormat), scaleMode, position);

        [NotNull]
        public static Bitmap Scale([NotNull] this Image image, [NotNull] Bitmap reusable, ScaleMode scaleMode, float position = 0)
            => Scale(image, new BitmapFormat(reusable), scaleMode, position, reusable);

        [NotNull]
        public static Bitmap Scale([NotNull] this Image image, BitmapFormat format, ScaleMode scaleMode, float position = 0,
            [CanBeNull] Bitmap reusable = null, [CanBeNull] Action<G> gConfigurator = null)
        {
            reusable = format.Validate(reusable);
            using (var g = G.FromImage(reusable))
            {
                gConfigurator?.Invoke(g);
                scaleMode.GetScaleFunction()(image, g, g.VisibleClipBounds, position);
            }
            return reusable;
        }

        /// <summary>
        /// Scale bitmap and keep the ratio of width to height.
        /// </summary>
        [NotNull]
        public static Bitmap ScaleProportionally([NotNull] this Image image, int maxWidth, int maxHeight)
            => Scale(image, ScaleFunctions.GetAspectFitSize(image.Size, maxWidth, maxHeight, 0, out _, out _).ToSize(), ScaleMode.ScaleToFill);

    }

}
