using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace bilicomic_tool
{
   public static class Utils
    {
        
        public static int ToInt32(this object obj)
        {

            if (int.TryParse(obj.ToString(), out var value))
            {
                return value;
            }
            else
            {
                return 0;
            }
        }
        /// <summary>
         /// 生成时间戳/秒
         /// </summary>
         /// <returns></returns>
        public static long GetTimestampS()
        {
            return Convert.ToInt64((DateTime.Now - new DateTime(1970, 1, 1, 8, 0, 0, 0)).TotalSeconds);
        }
        /// <summary>
        /// 生成时间戳/豪秒
        /// </summary>
        /// <returns></returns>
        public static long GetTimestampMS()
        {
            return Convert.ToInt64((DateTime.Now - new DateTime(1970, 1, 1, 8, 0, 0, 0)).TotalMilliseconds);
        }
        public static string ToMD5(string input)
        {
            MD5 md5 = MD5.Create();
            var comBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            var output = "";
            foreach (var item in comBytes)
            {
                output += item.ToString("x2");
            }
            return output;
        }

    }
}
