using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using PixelFormat = SharpDX.Direct2D1.PixelFormat;

namespace MarukoLib.DirectX
{

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class SharpDXUtils
    {

        public static System.Drawing.Color ToSd(this SharpDX.Color color) => System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);

        public static System.Windows.Media.Color ToSwm(this SharpDX.Color color) => System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);

        public static SharpDX.Color ToSdx(this System.Windows.Media.Color color) => new SharpDX.Color(color.ScR, color.ScG, color.ScB, color.ScA);

        public static SharpDX.Color ToSdx(this System.Drawing.Color color)
        {
            const float n = 255f;
            return new SharpDX.Color(color.R / n, color.G / n, color.B / n, color.A / n);
        }

        public static RawColor4 ToRaw4(this SharpDX.Color color)
        {
            const float n = 255f;
            return new RawColor4(color.R / n, color.G / n, color.B / n, color.A / n);
        }

        public static RawVector2 Multiply(this RawVector2 vec, float scale) => new RawVector2(vec.X * scale, vec.Y * scale);

        public static RawRectangleF ToRawRectangleF(this System.Drawing.Rectangle rect) => new RawRectangleF(rect.Left, rect.Top, rect.Right, rect.Bottom);

        public static RawRectangleF GetBounds(this IEnumerable<RawRectangleF> rectangles)
        {
            float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
            foreach (var rectangle in rectangles)
            {
                if (minX > rectangle.Left) minX = rectangle.Left;
                if (maxX < rectangle.Right) maxX = rectangle.Right;
                if (minY > rectangle.Top) minY = rectangle.Top;
                if (maxY < rectangle.Bottom) maxY = rectangle.Bottom;
            }
            return new RawRectangleF(minX, minY, maxX, maxY);
        }

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        public static RawRectangleF Scale(this RawRectangleF rect, float scale) => 
            scale == 1 ? rect : new RawRectangleF(rect.Left * scale, rect.Top * scale, rect.Right * scale, rect.Bottom * scale);

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        public static RawRectangleF SizeScale(this RawRectangleF rect, float scale) =>
            scale == 1 ? rect : new RawRectangleF(rect.Left, rect.Top, rect.Left + (rect.Right - rect.Left) * scale, rect.Top + (rect.Bottom - rect.Top) * scale);

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        public static RawRectangleF CenteredSizeScale(this RawRectangleF rect, float scale)
        {
            if (scale == 1) return rect;
            var halfDeltaWidth = rect.Width() * (scale - 1) / 2;
            var halfDeltaHeight = rect.Height() * (scale - 1) / 2;
            return new RawRectangleF(rect.Left - halfDeltaWidth, rect.Top - halfDeltaHeight, rect.Right + halfDeltaWidth, rect.Bottom + halfDeltaHeight);
        }

        public static bool Contains(this RawRectangleF rect, RawVector2 point) => Contains(rect, point.X, point.Y);

        public static bool Contains(this RawRectangleF rect, double x, double y) => x > rect.Left && x < rect.Right && y > rect.Top && y < rect.Bottom;

        public static RawRectangleF Centered(this RawRectangleF rect)
        {
            float halfWidth = (rect.Right - rect.Left) / 2, halfHeight = (rect.Bottom - rect.Top) / 2;
            return new RawRectangleF
            {
                Left = rect.Left - halfWidth,
                Right = rect.Left + halfWidth,
                Top = rect.Top - halfHeight,
                Bottom = rect.Top + halfHeight
            };
        }

        public static RawRectangleF CenteredRect(RawVector2 center, float size) => CenteredRect(center.X, center.Y, size);

        public static RawRectangleF CenteredRect(RawVector2 center, RawVector2 size) => CenteredRect(center.X, center.Y, size.X, size.Y);

        public static RawRectangleF CenteredRect(float cX, float cY, float size) => CenteredRect(cX, cY, size, size);

        public static RawRectangleF CenteredRect(float cX, float cY, float width, float height)
        {
            float halfWidth = width / 2, halfHeight = height / 2;
            return new RawRectangleF
            {
                Left = cX - halfWidth,
                Right = cX + halfWidth,
                Top = cY - halfHeight,
                Bottom = cY + halfHeight
            };
        }

        public static bool Intersected(RawRectangleF a, RawRectangleF b) => !(a.Right < b.Left || a.Left > b.Right || a.Top > b.Bottom || a.Bottom < b.Top);

        public static bool IntersectedWith(this RawRectangleF self, RawRectangleF that) => Intersected(self, that);

        public static float Width(this RawRectangleF self) => self.Right - self.Left;

        public static float Height(this RawRectangleF self) => self.Bottom - self.Top;

        public static RawVector2 Center(this RawRectangleF self) => new RawVector2((self.Left + self.Right) / 2, (self.Top + self.Bottom) / 2);

        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        public static RawRectangleF Shrink(this RawRectangleF self, float margin) => margin == 0 ? self : Shrink(self, margin, margin, margin, margin);

        public static RawRectangleF Shrink(this RawRectangleF self, float left, float top, float right, float bottom) => new RawRectangleF
        {
            Left = self.Left + left,
            Top = self.Top + top,
            Right = self.Right - right,
            Bottom = self.Bottom - bottom,
        };

        public static RawMatrix3x2 Translate(this RawMatrix3x2 matrix, float v) => Translate(matrix, v, v);

        public static RawMatrix3x2 Translate(this RawMatrix3x2 matrix, RawVector2 vec) => Translate(matrix, vec.X, vec.Y);

        public static RawMatrix3x2 Translate(this RawMatrix3x2 matrix, float x, float y) =>
            SharpDX.Matrix3x2.Multiply(SharpDX.Matrix3x2.Translation(x, y), matrix);

        public static RawMatrix3x2 Scale(this RawMatrix3x2 matrix, float v) => Scale(matrix, v, v);

        public static RawMatrix3x2 Scale(this RawMatrix3x2 matrix, RawVector2 vec) => Scale(matrix, vec.X, vec.Y);

        public static RawMatrix3x2 Scale(this RawMatrix3x2 matrix, float x, float y) =>
            SharpDX.Matrix3x2.Multiply(SharpDX.Matrix3x2.Scaling(x, y), matrix);

        public static RawMatrix3x2 Rotate(this RawMatrix3x2 matrix, float angle) =>
            SharpDX.Matrix3x2.Multiply(SharpDX.Matrix3x2.Rotation(angle), matrix);

        public static Bitmap ToD2D1Bitmap(this System.Drawing.Bitmap bitmap, RenderTarget renderTarget) => ToD2D1Bitmap(bitmap, renderTarget, renderTarget.PixelFormat);

        public static Bitmap ToD2D1Bitmap(this System.Drawing.Bitmap bitmap, RenderTarget renderTarget, PixelFormat pixelFormat)
        {
            var sourceArea = new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var bitmapProperties = new BitmapProperties(pixelFormat);
            var size = new SharpDX.Size2(bitmap.Width, bitmap.Height);

            // Transform pixels from BGRA to RGBA.
            var stride = bitmap.Width * sizeof(int);
            using (var tempStream = new SharpDX.DataStream(bitmap.Height * stride, true, true))
            {
                // Lock System.Drawing.Bitmap
                var bitmapData = bitmap.LockBits(sourceArea, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

                // Convert all pixels.
                for (var y = 0; y < bitmap.Height; y++)
                {
                    var offset = bitmapData.Stride * y;
                    for (var x = 0; x < bitmap.Width; x++)
                    {
                        // Not optimized.
                        var B = Marshal.ReadByte(bitmapData.Scan0, offset++);
                        var G = Marshal.ReadByte(bitmapData.Scan0, offset++);
                        var R = Marshal.ReadByte(bitmapData.Scan0, offset++);
                        var A = Marshal.ReadByte(bitmapData.Scan0, offset++);
                        tempStream.Write(R | (G << 8) | (B << 16) | (A << 24));
                    }
                }
                bitmap.UnlockBits(bitmapData);
                tempStream.Position = 0;
                return new Bitmap(renderTarget, size, tempStream, stride, bitmapProperties);
            }
        }

        public static void CopyToD2D1Bitmap(this System.Drawing.Bitmap bitmap, Bitmap targetBitmap)
        {
            var sourceArea = new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height);

            // Transform pixels from BGRA to RGBA.
            var stride = bitmap.Width * sizeof(int);
            var length = bitmap.Height * stride;
            using (var tempStream = new SharpDX.DataStream(length, true, true))
            {
                // Lock System.Drawing.Bitmap
                var bitmapData = bitmap.LockBits(sourceArea, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

                // Convert all pixels.
                for (var y = 0; y < bitmap.Height; y++)
                {
                    var offset = bitmapData.Stride * y;
                    for (var x = 0; x < bitmap.Width; x++)
                    {
                        // Not optimized.
                        var B = Marshal.ReadByte(bitmapData.Scan0, offset++);
                        var G = Marshal.ReadByte(bitmapData.Scan0, offset++);
                        var R = Marshal.ReadByte(bitmapData.Scan0, offset++);
                        var A = Marshal.ReadByte(bitmapData.Scan0, offset++);
                        tempStream.Write(R | (G << 8) | (B << 16) | (A << 24));
                    }
                }
                bitmap.UnlockBits(bitmapData);
                tempStream.Position = 0;
                targetBitmap.CopyFromStream(tempStream, stride, length);
            }
        }

    }
}