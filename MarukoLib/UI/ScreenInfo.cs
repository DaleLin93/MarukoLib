using System.Linq;
using System.Windows;
using System.Windows.Forms;
using MarukoLib.Lang;
using Newtonsoft.Json;

namespace MarukoLib.UI
{

    public class ScreenInfo : IDescribable
    {

        public readonly int Index;

        public readonly string DeviceName;

        public readonly int BitsPerPixel;

        public readonly bool Primary;

        public readonly Point Position;
        
        public readonly Size Size;

        public readonly double ScaleFactor;

        [JsonConstructor]
        public ScreenInfo(
            [JsonProperty(nameof(Index))]int index, [JsonProperty(nameof(DeviceName))]string deviceName,
            [JsonProperty(nameof(BitsPerPixel))]int bitsPerPixel, [JsonProperty(nameof(Primary))]bool primary,
            [JsonProperty(nameof(Position))]Point position, [JsonProperty(nameof(Size))]Size size, 
            [JsonProperty(nameof(ScaleFactor))]double scaleFactor)
        {
            Index = index;
            DeviceName = deviceName;
            BitsPerPixel = bitsPerPixel;
            Primary = primary;
            Position = position;
            Size = size;
            ScaleFactor = scaleFactor;
        }

        private ScreenInfo(int index, Screen screen, double scaleFactor)
        {
            Index = index;
            var bounds = screen.Bounds;
            DeviceName = screen.DeviceName;
            BitsPerPixel = screen.BitsPerPixel;
            Primary = screen.Primary;
            Position = new Point(bounds.X, bounds.Y);
            Size = new Size(bounds.Width, bounds.Height);
            ScaleFactor = scaleFactor;
        }

        public static ScreenInfo[] All
        {
            get
            {
                var screens = Screen.AllScreens;
                var scaleFactor = DpiUtils.Scale;
                var output = new ScreenInfo[screens.Length];
                for (var i = 0; i < screens.Length; i++) output[i] = new ScreenInfo(i, screens[i], scaleFactor);
                return output;
            }
        }

        public static ScreenInfo FindByPoint(Point point) => All.FirstOrDefault(screen => screen.Contains(point));

        [JsonIgnore] public Point Center => new Point(Position.X + Size.Width / 2, Position.Y + Size.Height / 2);

        public bool Contains(Point point) => point.X >= Position.X && point.X < Position.X + Size.Width
                                             && point.Y >= Position.Y && point.Y < Position.Y + Size.Height;

        public string Describe(PreferredDescriptionType type) => $"Screen {Index}: {Size.Width}x{Size.Height}, At: {Position.X}, {Position.Y}";

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((ScreenInfo) obj);
        }

        public override int GetHashCode() => DeviceName != null ? DeviceName.GetHashCode() : 0;

        protected bool Equals(ScreenInfo other) => string.Equals(DeviceName, other.DeviceName);

    }

}