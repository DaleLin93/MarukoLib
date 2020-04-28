using System.Drawing;
using System.Drawing.Imaging;
using JetBrains.Annotations;

namespace MarukoLib.Graphics
{

    public struct BitmapFormat
    {

        public Size Size;

        public PixelFormat PixelFormat;

        public BitmapFormat(Size size, PixelFormat pixelFormat)
        {
            Size = size;
            PixelFormat = pixelFormat;
        }

        public BitmapFormat([NotNull] Image image) : this(image.Size, image.PixelFormat) { }

        public bool IsMatch([CanBeNull] Image image) => image != null && Size == image.Size && PixelFormat == image.PixelFormat;

        public Bitmap CreateBitmap() => new Bitmap(Size.Width, Size.Height, PixelFormat);

        public Bitmap Validate([CanBeNull] Bitmap bitmap) 
            => IsMatch(bitmap) ? bitmap : new Bitmap(Size.Width, Size.Height, PixelFormat);

    }

}
