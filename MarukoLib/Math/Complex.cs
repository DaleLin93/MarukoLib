using System;

namespace MarukoLib.Math
{

    public class Complex : IEquatable<Complex>
    {

        #region 属性

        /// <summary>
        /// 获取或设置复数的实部
        /// </summary>
        public double Real { get; set; }

        /// <summary>
        /// 获取或设置复数的虚部
        /// </summary>
        public double Imaginary { get; set; }

        #endregion

        #region 构造函数

        /// <summary>
        /// 默认构造函数，得到的复数为0
        /// </summary>
        public Complex() : this(0, 0)
        {
        }

        /// <summary>
        /// 只给实部赋值的构造函数，虚部将取0
        /// </summary>
        /// <param name="dbReal">实部</param>
        public Complex(double dbReal) : this(dbReal, 0)
        {
        }

        /// <summary>
        /// 一般形式的构造函数
        /// </summary>
        /// <param name="dbReal">实部</param>
        /// <param name="dbImage">虚部</param>
        public Complex(double dbReal, double dbImage)
        {
            Real = dbReal;
            Imaginary = dbImage;
        }

        /// <summary>
        /// 以拷贝另一个复数的形式赋值的构造函数
        /// </summary>
        /// <param name="other">复数</param>
        public Complex(Complex other)
        {
            Real = other.Real;
            Imaginary = other.Imaginary;
        }

        #endregion

        #region 重载

        //加法的重载
        public static Complex operator +(Complex comp1, Complex comp2) => comp1.Add(comp2);

        //减法的重载
        public static Complex operator -(Complex comp1, Complex comp2) => comp1.Substract(comp2);

        //乘法的重载
        public static Complex operator *(Complex comp1, Complex comp2) => comp1.Multiply(comp2);

        //==的重载
        public static bool operator ==(Complex z1, Complex z2)
        {
            return ((z1.Real == z2.Real) && (z1.Imaginary == z2.Imaginary));
        }

        //!=的重载
        public static bool operator !=(Complex z1, Complex z2)
        {
            if (z1.Real == z2.Real)
            {
                return (z1.Imaginary != z2.Imaginary);
            }
            return true;
        }

        /// <summary>
        /// 重载ToString方法,打印复数字符串
        /// </summary>
        /// <returns>打印字符串</returns>
        public override string ToString()
        {
            if (Real == 0 && Imaginary == 0)
            {
                return string.Format("{0}", 0);
            }
            if (Real == 0 && (Imaginary != 1 && Imaginary != -1))
            {
                return string.Format("{0} i", Imaginary);
            }
            if (Imaginary == 0)
            {
                return string.Format("{0}", Real);
            }
            if (Imaginary == 1)
            {
                return string.Format("i");
            }
            if (Imaginary == -1)
            {
                return string.Format("- i");
            }
            if (Imaginary < 0)
            {
                return string.Format("{0} - {1} i", Real, -Imaginary);
            }
            return string.Format("{0} + {1} i", Real, Imaginary);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 复数加法
        /// </summary>
        /// <param name="comp">待加复数</param>
        /// <returns>返回相加后的复数</returns>
        public Complex Add(Complex comp)
        {
            double x = Real + comp.Real;
            double y = Imaginary + comp.Imaginary;

            return new Complex(x, y);
        }

        /// <summary>
        /// 复数减法
        /// </summary>
        /// <param name="comp">待减复数</param>
        /// <returns>返回相减后的复数</returns>
        public Complex Substract(Complex comp)
        {
            double x = Real - comp.Real;
            double y = Imaginary - comp.Imaginary;

            return new Complex(x, y);
        }

        /// <summary>
        /// 复数乘法
        /// </summary>
        /// <param name="comp">待乘复数</param>
        /// <returns>返回相乘后的复数</returns>
        public Complex Multiply(Complex comp)
        {
            double x = Real * comp.Real - Imaginary * comp.Imaginary;
            double y = Real * comp.Imaginary + Imaginary * comp.Real;

            return new Complex(x, y);
        }

        /// <summary>
        /// 获取复数的模/幅度
        /// </summary>
        /// <returns>返回复数的模</returns>
        public double GetModul() => System.Math.Sqrt(Real * Real + Imaginary * Imaginary);

        /// <summary>
        /// 获取复数的相位角，取值范围（-π，π]
        /// </summary>
        /// <returns>返回复数的相角</returns>
        public double GetAngle() => System.Math.Atan2(Imaginary, Real);

        /// <summary>
        /// 获取复数的共轭复数
        /// </summary>
        /// <returns>返回共轭复数</returns>
        public Complex Conjugate() => new Complex(this.Real, -this.Imaginary);

        public override bool Equals(object obj) => Equals(obj as Complex);

        public bool Equals(Complex other)
        {
            return other != null &&
                   Real == other.Real &&
                   Imaginary == other.Imaginary;
        }

        public override int GetHashCode()
        {
            var hashCode = -1613305685;
            hashCode = hashCode * -1521134295 + Real.GetHashCode();
            hashCode = hashCode * -1521134295 + Imaginary.GetHashCode();
            return hashCode;
        }

        #endregion
    }

}
