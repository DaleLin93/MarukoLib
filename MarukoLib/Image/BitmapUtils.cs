using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using JetBrains.Annotations;

namespace MarukoLib.Image
{

    public static class BitmapUtils
    {

        public static Bitmap TakeScreenshot(this Screen screen, Bitmap reusableBitmap = null) => TakeScreenshot(screen, PixelFormat.Format32bppArgb, reusableBitmap);

        public static Bitmap TakeScreenshot(this Screen screen, PixelFormat pixelFormat, Bitmap reusableBitmap = null)
        {
            var screenBounds = screen.Bounds;
            Bitmap snapshot;
            if (reusableBitmap != null && reusableBitmap.PixelFormat == pixelFormat && screenBounds.Width == reusableBitmap.Width && screenBounds.Height == reusableBitmap.Height)
                snapshot = reusableBitmap;
            else
                snapshot = new Bitmap(screenBounds.Width, screenBounds.Height);
            using (var g = Graphics.FromImage(snapshot))
                g.CopyFromScreen(0, 0, 0, 0, screen.Bounds.Size, CopyPixelOperation.SourceCopy);
            return snapshot;
        }

        public static Bitmap Copy(this Bitmap bitmap)
        {
            int width= bitmap.Width, height = bitmap.Height;
            var output = new Bitmap(width, height, bitmap.PixelFormat);
            using (var g = Graphics.FromImage(output))
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.DrawImage(bitmap, 0, 0, width, height);
            }
            return output;
        }

        public static Bitmap ScaleToSize(this Bitmap bitmap, Size size) => bitmap.ScaleToSize(size.Width, size.Height);

        public static Bitmap ScaleToSize(this Bitmap bitmap, float ratio) => bitmap.ScaleToSize((int)(bitmap.Width * ratio), (int)(bitmap.Height * ratio));

        public static Bitmap ScaleToSize(this Bitmap bitmap, int width, int height) => bitmap.Width == width && bitmap.Height == height ? bitmap : ScaleToSize(bitmap, width, height, null);

        public static Bitmap ScaleToSize(this Bitmap bitmap, [NotNull] Bitmap reusable) => ScaleToSize(bitmap, reusable.Width, reusable.Height, reusable);

        public static Bitmap ScaleToSize(this Bitmap bitmap, int width, int height, [CanBeNull] Bitmap reusable)
        {
            if (reusable == null || reusable.Width != width || reusable.Height != height || bitmap.PixelFormat != reusable.PixelFormat) reusable = new Bitmap(width, height, bitmap.PixelFormat);
            using (var g = Graphics.FromImage(reusable))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
//                g.DrawImage(bitmap, new Rectangle(0, 0, width, height), new Rectangle(0, 0, bitmap.Width, bitmap.Height), GraphicsUnit.Pixel);
                g.DrawImage(bitmap, 0, 0, width, height);
            }
            return reusable;
        }

        /// <summary>
        /// 按给定长度/宽度等比例缩放
        /// </summary>
        /// <param name="bitmap">原图</param>
        /// <param name="width">新图片宽度</param>
        /// <param name="height">新图片高度</param>
        /// <returns>新图片</returns>
        public static Bitmap ScaleProportional(this Bitmap bitmap, int width, int height)
        {
            float proportionalWidth, proportionalHeight;
            if (width.Equals(0))
            {
                proportionalWidth = (float)height / bitmap.Size.Height * bitmap.Width;
                proportionalHeight = height;
            }
            else if (height.Equals(0))
            {
                proportionalWidth = width;
                proportionalHeight = ((float)width) / bitmap.Size.Width * bitmap.Height;
            }
            else if ((float)width / bitmap.Size.Width * bitmap.Size.Height <= height)
            {
                proportionalWidth = width;
                proportionalHeight = (float)width / bitmap.Size.Width * bitmap.Height;
            }
            else
            {
                proportionalWidth = (float)height / bitmap.Size.Height * bitmap.Width;
                proportionalHeight = height;
            }
            return bitmap.ScaleToSize((int)proportionalWidth, (int)proportionalHeight);
        }

        /// <summary>
        /// 按给定长度/宽度缩放,同时可以设置背景色
        /// </summary>
        /// <param name="bitmap">original image</param>
        /// <param name="backgroundColor">background color</param>
        /// <param name="width">Width of new image</param>
        /// <param name="height">Height of new image</param>
        /// <returns></returns>
        public static Bitmap ScaleToSize(this Bitmap bitmap, Color backgroundColor, int width, int height)
        {
            var scaledBitmap = new Bitmap(width, height);
            using (var g = Graphics.FromImage(scaledBitmap))
            {
                g.Clear(backgroundColor);
                var proportionalBitmap = bitmap.ScaleProportional(width, height);
                var imagePosition = new Point((int)((width - proportionalBitmap.Width) / 2m), (int)((height - proportionalBitmap.Height) / 2m));
                g.DrawImage(proportionalBitmap, imagePosition);
            }
            return scaledBitmap;
        }
    }

}
