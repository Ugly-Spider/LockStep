# 定点数

浮点数由于设备的不同，会产生差异，不能在帧同步的逻辑中使用，所以，我们需要实现一个定点数。
(这里贴一篇文章，分析的很到位，关于浮点数在不同平台产生不同结果的一个原因：[https://coolshell.cn/articles/11235.html])

如果使用int32来实现，其中1位符号，15位整数部分，16位小数部分，那么能表示的最大值是32767(2^15-1)，最小单位是0.0000152587891(0.5^16)，范围明显不够，而且，计算过程中如果出现了小于最小单位的数，结果就会是0，还可能导致其他的一些问题。那么只能使用int64来实现了，当然，缺点是要多占用一倍的内存。

内容：
- 常量的定义
- 四则运算
- 绝对值
- 幂运算
- 求平方根
- 三角函数
- 与int、float互转

### 定义一些基本的常量
```c#
        public const int BIT_NUM = 64;
        public const int FRACTION_BIT_NUM = 32;
        private const long FRACTION_MASK = 0x00000000FFFFFFFF;

        private const long NAN = long.MinValue;
        private const long POSITIVE_INFINITE = long.MaxValue;
        private const long NEGATIVE_INFINITE = long.MinValue + 1;
        private const long MAX_VALUE = long.MaxValue - 1;
        private const long MIN_VALUE = long.MinValue + 2;
        private const long ONE = 1L << FRACTION_BIT_NUM;
        private const double PI = 3.1415926;

        public static readonly FP Zero = new FP(0);
        public static readonly FP One = new FP(ONE);
        public static readonly FP Half = new FP(ONE / 2);
        public static readonly FP Pi = PI;
        public static readonly FP NaN = new FP(NAN);
        public static readonly FP PositiveInfinite = new FP(POSITIVE_INFINITE);
        public static readonly FP NegativeInfinite = new FP(NEGATIVE_INFINITE);
        public static readonly FP MaxValue = new FP(MAX_VALUE);
        public static readonly FP MinValue = new FP(MIN_VALUE);
        public static readonly FP Rad2Deg = 57.29578f;
        public static readonly FP Deg2Rad = 0.01745f;
        public static readonly FP Precision = new FP(1);
```

### 加减法
加法减法比较简单，不过需要注意一些特殊情况，NaN加减任何数都是NaN，正无穷+负无穷=NaN
```c#
        public static FP operator +(FP a, FP b)
        {
            //if a or b is NaN, the result is NaN
            if (a._rawValue == NAN || b._rawValue == NAN) return NaN;
            
            //positive infinite + positive infinite = positive infinite
            //negative infinite + negative infinite = negative infinite
            //positive infinite + negative infinite = NaN
            //negative infinite + positive infinite = NaN
            if (a._rawValue == POSITIVE_INFINITE)
            {
                return b._rawValue == NEGATIVE_INFINITE ? NaN : PositiveInfinite;
            }
            if(a._rawValue == NEGATIVE_INFINITE)
            {
                return b._rawValue == POSITIVE_INFINITE ? NaN : NegativeInfinite;
            }

            //faster then: new FP(a._rawValue - b._rawValue);
            FP r;
            r._rawValue = a._rawValue + b._rawValue;
            return r;
        }

        public static FP operator -(FP a, FP b)
        {
            //if a or b is NaN, the result is NaN
            if (a._rawValue == NAN || b._rawValue == NAN) return NaN;
            
            //positive infinite - positive infinite = NaN
            //negative infinite - negative infinite = NaN
            //positive infinite - negative infinite = positive infinite
            //negative infinite - positive infinite = negative infinite
            if (a._rawValue == POSITIVE_INFINITE)
            {
                return b._rawValue == POSITIVE_INFINITE ? NaN : PositiveInfinite;
            }

            if (a._rawValue == NEGATIVE_INFINITE)
            {
                return b._rawValue == NEGATIVE_INFINITE ? NaN : NegativeInfinite;
            }
            
            FP r;
            r._rawValue = a._rawValue - b._rawValue;
            return r;
        }
```

这里发现 `FP r; r._rawValue = 10;` 比 `FP r = new FP(10);` 这种写法更快，对比了它们的汇编后发现，前者少了一次构造函数的调用。

### 乘法
乘法，直接使用rawValue相乘再除系数并不可行，因为rawValue相乘时会溢出；rawValue除系数后再乘也不可行，如果被除数小于1，结果就是0了。</br>
我们可以拆分一下`a * b = (ia + fa) * (ib + fb)//ia代表a的整数部分，fa代表a的小数部分`</br>
如果a, b都是正数:`1.2 * 2.4 = (1 + 0.2) * (2 + 0.4)`</br>
如果a是正数，b是负数:`1.2 * (-2.4) = (1 + 0.2) * (-3 + 0.6)`</br>
而无论是正数还是负数的取整操作都可以通过`_rawValue >> FRACTION_BIT_NUM`来实现，取小数的操作都可以通过`_rawValue & FRACTION_MASK`来实现。</br>
这里用8位来举例(1位符号位，3位整数，4位小数)：
* `+1.75` 的补码是 `0001 1100` `0001 1100 >> 4 = 0000 0001(+1)` `0001 1100 & 0000 1111 = 0000 1100(0.75)`
* `-1.75` 的补码是 `1110 0100` `1110 0100 >> 4 = 0000 1110(-2)` `1110 0100 & 0000 1111 = 0000 0100(0.25)`

`fa`和`fb`需要转换为ulong，否则当`fa * fb >= 0.5`时会溢出</br>

这里又发现了一个比较有意思的问题，`0*∞=?`，我的第一反应是0，因为0乘任何数都等于0，但是我用float测试了一下，发现结果是NaN，思考了一下，应该是这样的`1/∞=0, 2/∞=0...`所以反推`0*∞=1, 0*∞=2...`，可能的值有无数种，所以无法判断，就等于NaN了。
```c#
        public static FP operator *(FP a, FP b)
        {
            if (a._rawValue == NAN || b._rawValue == NAN) return NaN;
            if (a._rawValue == POSITIVE_INFINITE)
            {
                if (b._rawValue == 0) return NaN;
                return b._rawValue > 0 ? PositiveInfinite : NegativeInfinite;
            }

            if (a._rawValue == NEGATIVE_INFINITE)
            {
                if (b._rawValue == 0) return NaN;
                return b._rawValue > 0 ? NegativeInfinite : PositiveInfinite;
            }
            
            var fa = (ulong)(a._rawValue & FRACTION_MASK);
            var ia = a._rawValue >> FRACTION_BIT_NUM;
            var fb = (ulong)(b._rawValue & FRACTION_MASK);
            var ib = b._rawValue >> FRACTION_BIT_NUM;

            FP r;
            r._rawValue = (long)((fa * fb) >> FRACTION_BIT_NUM) + (long)fa * ib + (long)fb * ia + ((ia * ib) << FRACTION_BIT_NUM);
            return r;
        }
```

### 除法
这里计算除法的思路和手工计算除法的思路相同
```c#
        public static FP operator /(FP a, FP b)
        {
            if (a._rawValue == NAN || b._rawValue == NAN || (a._rawValue == 0 && b._rawValue == 0)) return NaN;
            var sign = ((a._rawValue ^ b._rawValue) & long.MinValue) == 0;
            if (b._rawValue == 0) return sign ? PositiveInfinite : NegativeInfinite;
            
            var ra = (ulong) a._rawValue;
            var rb = (ulong) b._rawValue;
            if ((ra & SIGN_MASK) != 0) //negative
            {
                ra = ulong.MaxValue - (ra + 1);
            }
            if ((rb & SIGN_MASK) != 0) //negative
            {
                rb = ulong.MaxValue - (rb + 1);
            }
            var c = 0ul;
            var rr = 0ul;
            for (int i = 0; i < BIT_NUM + FRACTION_BIT_NUM; ++i)
            {
                var t = ra & SIGN_MASK;
                ra <<= 1;
                c <<= 1;
                if (t != 0)
                {
                    c |= 0x1;
                }
            
                rr <<= 1;
                if (c >= rb)
                {
                    rr |= 0x1;
                    c -= rb;
                }
            }

            FP r;
            r._rawValue = sign ? (long)rr : -((long)rr);
            return r;
        }
```


### 绝对值
```c#
        public static FP Abs(FP x)
        {
            //faster then: r._rawValue = x._rawValue > 0 ? x._rawValue : -x._rawValue;
            var mask = x._rawValue >> 63;
            FP r;
            r._rawValue = (mask + x._rawValue) ^ mask;
            return r;
        }
```

### 求平方根
这里使用牛顿迭代法`x' = x - f(x) / f'(x) `
```c#
        public static FP Sqrt(FP x)
        {
            if (x._rawValue == 0) return Zero;
            if(x._rawValue < 0) throw new Exception("Input number can't be negative!");
            if (x._rawValue == NAN) return NaN;
            
            var r = x * FP.Half;
            int t = 10;
            for (int i = 0; i < t; ++i)
            {
                r -= (r * r - x) / (2 * r);
            }

            return r;
        }
```

### 幂运算
```c#
        public static FP Pow(FP x, int p)
        {
            if(p < 0) throw new Exception("P can't be negative!");
            if (x._rawValue == NAN) return NaN;
            return InternalPow(x, p);
        }

        private static FP InternalPow(FP x, int p)
        {
            if (p == 0) return 1;
            if (p == 1) return x;

            if (p % 2 == 0) return InternalPow(x, p / 2) * InternalPow(x, p / 2);
            else return InternalPow(x, p / 2) * InternalPow(x, p / 2) * x;
        }
```

### 三角函数
三角函数可以通过查表法，提前生成一个三角函数表，比如每隔1度计算一次sin值，然后计算时取最接近的两个角度的插值，这样做牺牲一丢丢的内存，不过效率很高。
为了得到更多的练习，这里我们打算自己计算，这里通过泰勒级数累加10次近似计算。
(这里有一个通俗易懂的关于如何理解泰勒级数的帖子[https://blog.csdn.net/qq_33414271/article/details/78783935])
```c#
        public static FP Cos(FP radiant)
        {
            if (radiant._rawValue == NAN) return NaN;
            
            var r = FP.One;
            var t = 10;
            var xs = radiant;
            var factorial = 1;
            for (int i = 1; i < t + 1; ++i)
            {
                factorial *= i;
             
                FP c = 0;
                if (i % 4 == 0) c = 1;
                else if (i % 4 == 2) c = -1;
                r += c * xs / factorial;
                xs *= radiant;
            }

            return r;
        }
        
        
        public static FP Sin(FP radiant)
        {
            if (radiant._rawValue == NAN) return NaN;
            
            var r = FP.Zero;
            var t = 10;
            var xs = radiant;
            var factorial = 1;
            for (int i = 1; i < t + 1; ++i)
            {
                factorial *= i;
             
                FP c = 0;
                if (i % 4 == 1) c = 1;
                else if (i % 4 == 3) c = -1;
                r += c * xs / factorial;
                xs *= radiant;
            }

            return r;
        }

        public static FP Tan(FP radiant)
        {
            return Sin(radiant) / Cos(radiant);
        }
```

参考：PhotonTrueSync/Fix64
