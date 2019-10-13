using System.Windows;

namespace MarukoLib.UI
{

    public static class PointUtils
    {

        public static Point Add(this Point a, Point b) => new Point(a.X + b.X, a.Y + b.Y);

        public static Point Sub(this Point a, Point b) => new Point(a.X - b.X, a.Y - b.Y);

        public static Point Multiply(this Point p, double multiplier) => new Point(p.X * multiplier, p.Y * multiplier);

        public static Point Divide(this Point p, double divider) => new Point(p.X / divider, p.Y / divider);

        public static double Distance(this Point a, Point b) => DistanceToOrigin(Sub(a, b));

        public static double ManhattanDistance(this Point a, Point b) => ManhattanToOrigin(Sub(a, b));

        public static double DistanceToOrigin(this Point a) => System.Math.Sqrt(a.X * a.X + a.Y * a.Y);

        public static double ManhattanToOrigin(this Point a) => System.Math.Abs(a.X) + System.Math.Abs(a.Y);

        public static System.Drawing.Point RoundToSdPoint(this Point p) => 
            new System.Drawing.Point((int)System.Math.Round(p.X), (int)System.Math.Round(p.Y));

        public static System.Drawing.Point FloorToSdPoint(this Point p) => 
            new System.Drawing.Point((int)System.Math.Floor(p.X), (int)System.Math.Floor(p.Y));

        public static System.Drawing.Point CeilingToSdPoint(this Point p) => 
            new System.Drawing.Point((int)System.Math.Ceiling(p.X), (int)System.Math.Ceiling(p.Y));

    }

}
