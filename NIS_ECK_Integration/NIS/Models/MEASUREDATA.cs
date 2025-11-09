using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NIS.Models
{
    public class MEASUREDATA
    {
        /// <summary> 病歷號 </summary>
        public string PATIENTID { set; get; }

        /// <summary> 護理診斷中文名稱</summary>
        public string DATADATE { set; get; }

        /// <summary> 護理診斷領域代碼 </summary>
        public string OBSERVATIONID { set; get; }

        /// <summary> 數值 </summary>
        public string VALUE { set; get; }

        /// <summary> 單位 </summary>
        public string UNIT { set; get; }

        /// <summary> 位置 </summary>
        public string LOCATION { set; get; }

        /// <summary> 狀態 </summary>
        public string STATUS { set; get; }
    }

}