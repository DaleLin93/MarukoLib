using System;
using System.Drawing;
using JetBrains.Annotations;
using MarukoLib.Lang;
using G = System.Drawing.Graphics;

namespace MarukoLib.Graphics
{

    public enum ScaleMode
    {
        /// <summary>
        /// Scales the content to fit the size of itself by changing the aspect ratio of the content if necessary.
        /// </summary>
        ScaleToFill,

        /// <summary>
        /// Scales the content to fit the size of the view by maintaining the aspect ratio.
        /// Any remaining area of the view’s bounds is transparent.
        /// </summary>
        ScaleAspectFit,

        /// <summary>
        /// Scales the content to fill the size of the view. Some portion of the content may be clipped to fill the view’s bounds.
        /// </summary>
        ScaleAspectFill
    }

    public static class ScaleFunctions
    {

        private const float SnapTolerance = 0.4F;

        public delegate void ScaleFunction([NotNull] Image image, [NotNull] G g, RectangleF target, float position = 0);

        public static ScaleFunction GetScaleFunction(this ScaleMode mode)
        {
            switch (mode)
            {
                case ScaleMode.ScaleToFill:
                    return delegate(Image image, G g, RectangleF target, float position)
                    {
                        g.DrawImage(image, target, new RectangleF(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);
                    };
                case ScaleMode.ScaleAspectFit:
                    return delegate(Image image, G g, RectangleF target, float position)
                    {
                        GetAspectFitSize(image.Size, target.Width, target.Height, position, out _, out var dst);
                        dst.X += target.X;
                        dst.Y += target.Y;
                        g.DrawImage(image, dst, new RectangleF(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);
                    };
                case ScaleMode.ScaleAspectFill:
                    return delegate(Image image, G g, RectangleF target, float position)
                    {
                        GetAspectFillSize(image.Size, target.Width, target.Height, position, out var src, out _);
                        g.DrawImage(image, target, src, GraphicsUnit.Pixel);
                    };
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }

        public static SizeF GetAspectFitSize(SizeF originalSize, float maxWidth, float maxHeight, float position,
            out RectangleF src, out RectangleF dst)
        {
            if (maxWidth <= 0 && maxHeight <= 0)
            {
                src = dst = new RectangleF(0, 0, originalSize.Width, originalSize.Height);
                return originalSize;
            }
            float width, height;
            if (maxWidth <= 0)
            {
                width = maxHeight / originalSize.Height * originalSize.Width;
                height = maxHeight;
            }
            else if (maxHeight <= 0)
            {
                width = maxWidth;
                height = maxWidth / originalSize.Width * originalSize.Height;
            }
            else
            {
                var wScale = maxWidth / originalSize.Width;
                var hScale = maxHeight / originalSize.Height;
                if (wScale < hScale)
                {
                    width = maxWidth;
                    height = originalSize.Height * wScale;
                }
                else
                {
                    width = originalSize.Width * hScale;
                    height = maxHeight;
                }
            }
            position = (position + 1) / 2;
            src = new RectangleF(0, 0, width, height);
            dst = new RectangleF((maxWidth - width) * position, (maxHeight - height) * position, width, height);
            return new SizeF(width, height);
        }

        public static SizeF GetAspectFillSize(SizeF originalSize, float minWidth, float minHeight, float position,
            out RectangleF src, out RectangleF dst)
        {
            if (minWidth <= 0 && minHeight <= 0)
            {
                src = dst = new RectangleF(0, 0, originalSize.Width, originalSize.Height);
                return originalSize;
            }
            float scale;
            if (minWidth <= 0)
                scale = minHeight / originalSize.Height;
            else if (minHeight <= 0)
                scale = minWidth / originalSize.Width;
            else
            {
                var wScale = minWidth / originalSize.Width;
                var hScale = minHeight / originalSize.Height;
                scale = wScale > hScale ? wScale : hScale;
            }
            var width = (originalSize.Width * scale).Snap(minWidth, SnapTolerance);
            var height = (originalSize.Height * scale).Snap(minHeight, SnapTolerance);

            position = (position + 1) / 2;
            var srcSize = new SizeF(
                (minWidth / scale).Snap(originalSize.Width, SnapTolerance),
                (minHeight / scale).Snap(originalSize.Height, SnapTolerance)
            );
            var srcPos = new PointF(
                (originalSize.Width - srcSize.Width) * position, 
                (originalSize.Height - srcSize.Height) * position
            );
            src = new RectangleF(srcPos, srcSize);
            dst = new RectangleF(0, 0, minWidth, minHeight);
            return new SizeF(width, height);
        }

    }

}