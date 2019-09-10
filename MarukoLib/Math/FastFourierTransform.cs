using System;

namespace MarukoLib.Math
{
    public static class FastFourierTransform
    {

        public static Complex[] Transform(Complex[] sourceData, int n)
        {
            //2的r次幂为N，求出r.r能代表fft算法的迭代次数
            var r = Convert.ToInt32(System.Math.Log(n, 2));

            //分别存储蝶形运算过程中左右两列的结果
            var interVar1 = new Complex[n];
            var interVar2 = new Complex[n];
            interVar1 = (Complex[])sourceData.Clone();

            //w代表旋转因子
            var w = new Complex[n / 2];
            //为旋转因子赋值。（在蝶形运算中使用的旋转因子是已经确定的，提前求出以便调用）
            //旋转因子公式 \  /\  /k __
            //              \/  \/N  --  exp(-j*2πk/N)
            //这里还用到了欧拉公式
            for (var i = 0; i < n / 2; i++)
            {
                var angle = -i * System.Math.PI * 2 / n;
                w[i] = new Complex(System.Math.Cos(angle), System.Math.Sin(angle));
            }

            //蝶形运算
            for (var i = 0; i < r; i++)
            {
                //i代表当前的迭代次数，r代表总共的迭代次数.
                //i记录着迭代的重要信息.通过i可以算出当前迭代共有几个分组，每个分组的长度

                //interval记录当前有几个组
                // <<是左移操作符，左移一位相当于*2
                //多使用位运算符可以人为提高算法速率^_^
                int interval = 1 << i;

                //halfN记录当前循环每个组的长度N
                int halfN = 1 << (r - i);

                //循环，依次对每个组进行蝶形运算
                for (int j = 0; j < interval; j++)
                {
                    //j代表第j个组

                    //gap=j*每组长度，代表着当前第j组的首元素的下标索引
                    int gap = j * halfN;

                    //进行蝶形运算
                    for (int k = 0; k < halfN / 2; k++)
                    {
                        interVar2[k + gap] = interVar1[k + gap] + interVar1[k + gap + halfN / 2];
                        interVar2[k + halfN / 2 + gap] = (interVar1[k + gap] - interVar1[k + gap + halfN / 2]) * w[k * interval];
                    }
                }

                //将结果拷贝到输入端，为下次迭代做好准备
                interVar1 = (Complex[])interVar2.Clone();
            }

            //将输出码位倒置
            for (uint j = 0; j < n; j++)
            {
                //j代表自然顺序的数组元素的下标索引

                //用rev记录j码位倒置后的结果
                uint rev = 0;
                //num作为中间变量
                uint num = j;

                //码位倒置（通过将j的最右端一位最先放入rev右端，然后左移，然后将j的次右端一位放入rev右端，然后左移...）
                //由于2的r次幂=N，所以任何j可由r位二进制数组表示，循环r次即可
                for (int i = 0; i < r; i++)
                {
                    rev <<= 1;
                    rev |= num & 1;
                    num >>= 1;
                }
                interVar2[rev] = interVar1[j];
            }
            return interVar2;
        }

    }
}
