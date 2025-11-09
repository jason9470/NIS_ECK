using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NIS.UtilTool
{
    public class ConvertTool
    {
        /// <summary>
        /// 西元年轉民國年
        /// </summary>
        /// <param name="inDate">西元年日期(日期)</param>
        /// <returns></returns>
        public static string toRocDate(DateTime inDate)
        {
            int year = int.Parse(inDate.Year.ToString()) - 1911;
            string outDate = year.ToString() + inDate.ToString("-MM-dd");
            return outDate;
        }

        /// <summary>
        /// 民國年轉西元日期
        /// </summary>
        /// <param name="inDate">民國年日期(文字)</param>
        /// <returns></returns>
        public static DateTime toDCDate(string inDate) {
            DateTime outDate = DateTime.MinValue;
            try
            {
                inDate = (int.Parse(inDate) + 19110000).ToString();
                inDate = inDate.Substring(0, 4) + "-" + inDate.Substring(4, 2) + "-" + inDate.Substring(6, 2);
                outDate = DateTime.Parse(inDate);
            }
            catch (Exception)
            {
                outDate = DateTime.MinValue;
            }
            return outDate;
        }

    }
}