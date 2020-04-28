using System;
using WP = System.Windows.Point;
using DP = System.Drawing.Point;
using DPF = System.Drawing.PointF;
using WS = System.Windows.Size;
using DS = System.Drawing.Size;
using DSF = System.Drawing.SizeF;

namespace MarukoLib.Graphics
{

    public static class GeometryUtils
    {

        #region Point

        public static DP Add(this DP a, DP b) => new DP(a.X + b.X, a.Y + b.Y);

        public static DP Sub(this DP a, DP b) => new DP(a.X - b.X, a.Y - b.Y);

        public static DPF Add(this DPF a, DPF b) => new DPF(a.X + b.X, a.Y + b.Y);

        public static DPF Sub(this DPF a, DPF b) => new DPF(a.X - b.X, a.Y - b.Y);

        public static WP Add(this WP a, WP b) => new WP(a.X + b.X, a.Y + b.Y);

        public static WP Sub(this WP a, WP b) => new WP(a.X - b.X, a.Y - b.Y);

        public static DP Mul(this DP p, int multiplier) => new DP(p.X * multiplier, p.Y * multiplier);

        public static DP Div(this DP p, int divider) => new DP(p.X / divider, p.Y / divider);

        public static DPF Mul(this DPF p, float multiplier) => new DPF(p.X * multiplier, p.Y * multiplier);

        public static DPF Div(this DPF p, float divider) => new DPF(p.X / divider, p.Y / divider);

        public static WP Mul(this WP p, double multiplier) => new WP(p.X * multiplier, p.Y * multiplier);

        public static WP Div(this WP p, double divider) => new WP(p.X / divider, p.Y / divider);

        public static double Distance(this DP a, DP b) => DistanceToOrigin(Sub(a, b));

        public static double Distance(this DPF a, DPF b) => DistanceToOrigin(Sub(a, b));

        public static double Distance(this WP a, WP b) => DistanceToOrigin(Sub(a, b));

        public static int ManhattanDistance(this DP a, DP b) => ManhattanToOrigin(Sub(a, b));

        public static float ManhattanDistance(this DPF a, DPF b) => ManhattanToOrigin(Sub(a, b));

        public static double ManhattanDistance(this WP a, WP b) => ManhattanToOrigin(Sub(a, b));

        public static double DistanceToOrigin(this DP a) => Math.Sqrt(a.X * a.X + a.Y * a.Y);

        public static double DistanceToOrigin(this DPF a) => Math.Sqrt(a.X * a.X + a.Y * a.Y);

        public static double DistanceToOrigin(this WP a) => Math.Sqrt(a.X * a.X + a.Y * a.Y);

        public static int ManhattanToOrigin(this DP a) => Math.Abs(a.X) + Math.Abs(a.Y);

        public static float ManhattanToOrigin(this DPF a) => Math.Abs(a.X) + Math.Abs(a.Y);

        public static double ManhattanToOrigin(this WP a) => Math.Abs(a.X) + Math.Abs(a.Y);

        public static WP ToSwPoint(this DP p) => new WP(p.X, p.Y);

        public static WP ToSwPoint(this DPF p) => new WP(p.X, p.Y);

        public static DP RoundToSdPoint(this WP p) => new DP((int)Math.Round(p.X), (int)Math.Round(p.Y));

        public static DP FloorToSdPoint(this WP p) => new DP((int)Math.Floor(p.X), (int)Math.Floor(p.Y));

        public static DP CeilingToSdPoint(this WP p) => new DP((int)Math.Ceiling(p.X), (int)Math.Ceiling(p.Y));

        public static DPF ToSdPointF(this WP p) => new DPF((float)p.X, (float)p.Y);

        #endregion

        #region Size

        public static DS Add(this DS a, DS b) => new DS(a.Width + b.Width, a.Height + b.Height);

        public static DS Sub(this DS a, DS b) => new DS(a.Width - b.Width, a.Height - b.Height);

        public static DSF Add(this DSF a, DSF b) => new DSF(a.Width + b.Width, a.Height + b.Height);

        public static DSF Sub(this DSF a, DSF b) => new DSF(a.Width - b.Width, a.Height - b.Height);

        public static WS Add(this WS a, WS b) => new WS(a.Width + b.Width, a.Height + b.Height);

        public static WS Sub(this WS a, WS b) => new WS(a.Width - b.Width, a.Height - b.Height);

        public static DS Mul(this DS s, int multiplier) => new DS(s.Width * multiplier, s.Height * multiplier);

        public static DS Div(this DS s, int divider) => new DS(s.Width / divider, s.Height / divider);

        public static DSF Mul(this DSF s, float multiplier) => new DSF(s.Width * multiplier, s.Height * multiplier);

        public static DSF Div(this DSF s, float divider) => new DSF(s.Width / divider, s.Height / divider);

        public static WS Mul(this WS s, double multiplier) => new WS(s.Width * multiplier, s.Height * multiplier);

        public static WS Div(this WS s, double divider) => new WS(s.Width / divider, s.Height/ divider);

        public static int GetArea(this DS s) => Math.Abs(s.Width * s.Height);

        public static float GetArea(this DSF s) => Math.Abs(s.Width * s.Height);

        public static double GetArea(this WS s) => Math.Abs(s.Width * s.Height);

        public static int GetSignedArea(this DS s) => s.Width * s.Height;

        public static float GetSignedArea(this DSF s) => s.Width * s.Height;

        public static double GetSignedArea(this WS s) => s.Width * s.Height;

        public static WS ToSwSize(this DS s) => new WS(s.Width, s.Height);

        public static WS ToSwSize(this DSF s) => new WS(s.Width, s.Height);

        public static DS RoundToSwSize(this WS s) => new DS((int)Math.Round(s.Width), (int)Math.Round(s.Height));

        public static DS FloorToSwSize(this WS s) => new DS((int)Math.Floor(s.Width), (int)Math.Floor(s.Height));

        public static DS CeilingToSwSize(this WS s) => new DS((int)Math.Ceiling(s.Width), (int)Math.Ceiling(s.Height));

        public static DSF ToSwSizeF(this WS s) => new DSF((float)s.Width, (float)s.Height);

        #endregion


    }

}
