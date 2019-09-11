using SD = System.Drawing;
using SWM = System.Windows.Media;

namespace MarukoLib.UI
{
    public static class ColorUtils
    {

        public static SWM.Color SetAlpha(this SWM.Color color, byte alpha) => new SWM.Color { A = alpha, R = color.R, G = color.G, B = color.B };

        public static SD.Color ToSdColor(this uint color) => SD.Color.FromArgb((byte) (color >> 24 & 0xFF), (byte) (color >> 16 & 0xFF), (byte) (color >> 8 & 0xFF), (byte) (color & 0xFF));

        public static uint ToUIntArgb(this SD.Color color) => ((uint) color.A << 24) | ((uint) color.R << 16) | ((uint) color.G << 8) | color.B;

        public static SWM.Color ToSwmColor(this SD.Color color) => color.ToUIntArgb().ToSwmColor();

        public static SD.Color Inverted(this SD.Color color) => SD.Color.FromArgb(color.A, 255 - color.R, 255 - color.G, 255 - color.B);

        public static SWM.Color ToSwmColor(this uint color) => SWM.Color.FromArgb((byte) (color >> 24 & 0xFF), (byte) (color >> 16 & 0xFF), (byte) (color >> 8 & 0xFF), (byte) (color & 0xFF));

        public static uint ToUIntArgb(this SWM.Color color) => ((uint)color.A << 24) | ((uint)color.R << 16) | ((uint)color.G << 8) | color.B;

        public static SD.Color ToSdColor(this SWM.Color color) => color.ToUIntArgb().ToSdColor();

        public static SWM.Color Inverted(this SWM.Color color) => SWM.Color.FromScRgb(color.ScA, 1 - color.ScR, 1 - color.ScG, 1 - color.ScB);

    }
}
