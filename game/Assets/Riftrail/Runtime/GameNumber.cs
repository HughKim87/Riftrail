using System;
using System.Globalization;
namespace Riftrail
{
    public static class GameNumber
    {
        public static string Format(long value)
        {
            if(value<0) return "-"+FormatPositive(-(double)value);
            return FormatPositive(value);
        }
        public static string Precise(double value)=>Math.Abs(value)<1000?value.ToString("0.##",CultureInfo.InvariantCulture):Format((long)Math.Round(value));
        private static string FormatPositive(double value)
        {
            var units=new[]{"","K","M","G","T","P","E"};var unit=0;
            while(value>=1000 && unit<units.Length-1) {value/=1000;unit++;}
            if(unit==0) return value.ToString("0",CultureInfo.InvariantCulture);
            var decimals=value>=100?0:value>=10?1:2;
            var rounded=Math.Round(value,decimals,MidpointRounding.AwayFromZero);
            if(rounded>=1000 && unit<units.Length-1) {rounded/=1000;unit++;decimals=2;}
            return rounded.ToString(decimals==0?"0":decimals==1?"0.#":"0.##",CultureInfo.InvariantCulture)+units[unit];
        }
    }
}
