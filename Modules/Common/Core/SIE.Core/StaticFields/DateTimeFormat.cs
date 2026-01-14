using System;

namespace SIE.Core
{
    /// <summary>
    /// 日期-字符串 格式字段
    /// </summary>
    public static class DateTimeFormat
    {
        /// <summary>
        /// yyyy/MM/dd HH:mm:ss
        /// </summary>
        public static readonly string LongDateString1 = "yyyy/MM/dd HH:mm:ss";

        /// <summary>
        /// yyyy-MM-dd HH:mm:ss
        /// </summary>
        public static readonly string LongDateString2 = "yyyy-MM-dd HH:mm:ss";

        /// <summary>
        /// yyyy-MM-dd hh:mm
        /// </summary>
        public static readonly string LongDateString3 = "yyyy-MM-dd hh:mm";

        /// <summary>
        /// yyyyMMddHHmmss
        /// </summary>
        public static readonly string LongDateString4 = "yyyyMMddHHmmss";

        /// <summary>
        /// yyyy/MM/dd
        /// </summary>
        public static readonly string YYYMMdd1 = "yyyy/MM/dd";

        /// <summary>
        /// yyyy-MM-dd
        /// </summary>
        public static readonly string YYYMMdd2 = "yyyy-MM-dd";

        /// <summary>
        /// yyyy-MM
        /// </summary>
        public static readonly string YYYYMM = "yyyy-MM";

        /// <summary>
        /// MM/dd
        /// </summary>
        public static readonly string MMdd1 = "MM/dd";

        /// <summary>
        /// MM:mm
        /// </summary>
        public static readonly string HHmm = "HH:mm";

        /// <summary>
        /// 日期转字符串 yyyy/MM/dd HH:mm:ss
        /// </summary>
        /// <param name="dt">日期对象</param>
        /// <returns>返回日期转字符串</returns>
        public static string ToLongFormat1(DateTime dt)
        {
            return dt.ToString(DateTimeFormat.LongDateString1);
        }

        /// <summary>
        /// 日期转字符串 yyyy/MM/dd HH:mm:ss
        /// </summary>
        /// <param name="dt">日期对象</param>
        /// <returns>返回日期转字符串</returns>
        public static string ToLongFormat1(DateTime? dt)
        {
            if (dt.HasValue)
            {
                return dt.Value.ToString(DateTimeFormat.LongDateString1);
            }

            return string.Empty;
        }

        /// <summary>
        /// MM-dd HH:mm
        /// </summary>
        public static readonly string MMddHHmm = "MM-dd HH:mm";

        /// <summary>
        /// 日期转字符串 yyyy-MM-dd HH:mm:ss
        /// </summary>
        /// <param name="dt">日期对象</param>
        /// <returns>返回日期转字符串</returns>
        public static string ToLongFormat2(DateTime dt)
        {
            return dt.ToString(DateTimeFormat.LongDateString2);
        }

        /// <summary>
        /// 日期转字符串 yyyy-MM-dd HH:mm:ss
        /// </summary>
        /// <param name="dt">日期对象</param>
        /// <returns>返回日期转字符串</returns>
        public static string ToLongFormat2(DateTime? dt)
        {
            if (dt.HasValue)
            {
                return dt.Value.ToString(DateTimeFormat.LongDateString2);
            }

            return string.Empty;
        }

        /// <summary>
        /// 日期转字符串 yyyy-MM-dd HH:mm:ss
        /// </summary>
        /// <param name="dt">日期对象</param>
        /// <returns>返回日期转字符串</returns>
        public static string ToLongFormat4(DateTime? dt)
        {
            if (dt.HasValue)
            {
                return dt.Value.ToString(DateTimeFormat.LongDateString4);
            }

            return string.Empty;
        }

        /// <summary>
        /// 转化时间类型为字符串类型   yyyy/MM/dd 
        /// </summary>
        /// <param name="dt">日期对象</param>
        /// <returns>返回日期转字符串</returns>
        public static string ToShortFormat1(DateTime dt)
        {
            return dt.ToString(DateTimeFormat.YYYMMdd1);
        }

        /// <summary>
        /// 转化时间类型为字符串类型   yyyy/MM/dd 
        /// </summary>
        /// <param name="dt">日期对象</param>
        /// <returns>返回日期转字符串</returns>
        public static string ToShortFormat1(DateTime? dt)
        {
            if (dt.HasValue)
            {
                return dt.Value.ToString(DateTimeFormat.YYYMMdd1);
            }

            return string.Empty;
        }

        /// <summary>
        /// 转化时间类型为字符串类型   yyyy-MM-dd 
        /// </summary>
        /// <param name="dt">日期对象</param>
        /// <returns>返回日期转字符串</returns>
        public static string ToShortFormat2(this DateTime dt)
        {
            return dt.ToString(DateTimeFormat.YYYMMdd2);
        }

        /// <summary>
        /// 转化时间类型为字符串类型   yyyy-MM-dd 
        /// </summary>
        /// <param name="dt">日期对象</param>
        /// <returns>返回日期转字符串</returns>
        public static string ToShortFormat2(this DateTime? dt)
        {
            if (dt.HasValue)
            {
                return dt.Value.ToString(DateTimeFormat.YYYMMdd2);
            }

            return string.Empty;
        }

        /// <summary>
        /// 转化时间类型为字符串类型   yyyyMMddHHmmssfff
        /// </summary>
        /// <param name="dt">日期对象</param>
        /// <returns>返回日期转字符串</returns>
        public static string ToLongCodeFormat(DateTime dt)
        {
            return dt.ToString("yyyyMMddHHmmssfff");
        }
    }
}