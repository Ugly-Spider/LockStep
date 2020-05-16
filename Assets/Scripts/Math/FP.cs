using System;
using System.Globalization;

namespace LockStep
{
    /// <summary>
    /// 64bit fixed point number 31.32
    /// </summary>
    [Serializable]
    public struct FP
    {
        public const int BIT_NUM = 64;
        public const int FRACTION_BIT_NUM = 32;
        private const long FRACTION_MASK = 0x00000000FFFFFFFF;
        private const ulong SIGN_MASK = 0x8000000000000000;

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
        
        public long RawValue => _rawValue;
        
        [UnityEngine.SerializeField]
        private long _rawValue;

        private FP(long rawValue)
        {
            _rawValue = rawValue;
        }

        public float ToFloat()
        {
            if (_rawValue == NAN) return float.NaN;
            
            return (float)_rawValue / ONE;
        }

        public int ToInt()
        {
            if(_rawValue == NAN) throw new Exception("NaN can't convert to int");
            
            return (int)(_rawValue / ONE);
        }

        public int RoundToInt()
        {
            var i = ToInt();
            var f = _rawValue & FRACTION_MASK;
            if (_rawValue < 0) f = (~(f - 1)) & FRACTION_MASK;
            if (f >= Half._rawValue)
            {
                if (_rawValue >= 0)
                {
                    ++i;
                }
                else
                {
                    --i;
                }
            }
            return i;
        }

        public override string ToString()
        {
            return ToFloat().ToString(CultureInfo.InvariantCulture);
        }

        public static FP Abs(FP x)
        {
            if (x._rawValue == NAN) return NaN;
            
            //faster then: r._rawValue = x._rawValue > 0 ? x._rawValue : -x._rawValue;
            var mask = x._rawValue >> 63;
            FP r;
            r._rawValue = (mask + x._rawValue) ^ mask;
            return r;
        }

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

        public static int RoundToInt(FP x)
        {
            return x.RoundToInt();
        }

        public static bool operator >(FP a, FP b)
        {
            if (a._rawValue == NAN || b._rawValue == NAN) return false;
            
            return a._rawValue > b._rawValue;
        }
        
        public static bool operator <(FP a, FP b)
        {
            if (a._rawValue == NAN || b._rawValue == NAN) return false;
            
            return a._rawValue < b._rawValue;
        }
        
        public static bool operator >=(FP a, FP b)
        {
            if (a._rawValue == NAN || b._rawValue == NAN) return false;
            
            return a._rawValue >= b._rawValue;
        }
        
        public static bool operator <=(FP a, FP b)
        {
            if (a._rawValue == NAN || b._rawValue == NAN) return false;
            
            return a._rawValue <= b._rawValue;
        }
        
        public static bool operator ==(FP a, FP b)
        {
            if (a._rawValue == NAN || b._rawValue == NAN) return false;
            
            return a._rawValue == b._rawValue;
        }
        
        public static bool operator !=(FP a, FP b)
        {
            return !(a == b);
        }

        public static FP operator -(FP x)
        {
            if (x._rawValue == NAN) return NaN;
            
            FP r;
            r._rawValue = -x._rawValue;
            return r;
        }

        public static FP operator %(FP a, FP b)
        {
            if (a._rawValue == NAN || b._rawValue == NAN) return NaN;
            
            FP r;
            r._rawValue = a._rawValue % b._rawValue;
            return r;
        }

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
            
            //a * b / c = (ia + fa) * (ib + fb) / c 
            var fa = (ulong)(a._rawValue & FRACTION_MASK);//Convert to ulong, ensure fa * fb will not overflow
            var ia = a._rawValue >> FRACTION_BIT_NUM;
            var fb = (ulong)(b._rawValue & FRACTION_MASK);//Convert to ulong, ensure fa * fb will not overflow
            var ib = b._rawValue >> FRACTION_BIT_NUM;
            
            FP r;
            r._rawValue = (long)((fa * fb) >> FRACTION_BIT_NUM) + (long)fa * ib + (long)fb * ia + ((ia * ib) << FRACTION_BIT_NUM);
            return r;
        }

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

        public static implicit operator FP(double x)
        {
            FP r;
            r._rawValue = (long)(x * ONE);
            return r;
        }

        public static implicit operator FP(int x)
        {
            FP r;
            r._rawValue = x * ONE;
            return r;
        }

        public static FP FromRawValue(long x)
        {
            FP r;
            r._rawValue = x;
            return r;
        }
    }

}

