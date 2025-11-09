using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NIS.Models
{
    public class Care_Plan_Item
    {
        /// <summary> 護理診斷代碼 </summary>
        public string DIAGNOSIS_CODE { set; get; }

        /// <summary> 護理診斷中文名稱</summary>
        public string DIAGNOSIS_NAME { set; get; }

        /// <summary> 護理診斷領域代碼 </summary>
        public string DIAGNOSIS_DOMAIN_CODE { set; get; }

        /// <summary> 護理診斷領域中文名稱 </summary>
        public string DIAGNOSIS_DOMAIN_DESC { set; get; }

        /// <summary> 定義性特徵代碼 </summary>
        public string FEATURE_CODE { set; get; }

        /// <summary> 定義性特徵中文名稱 </summary>
        public string FEATURE_DESC { set; get; }

        /// <summary> 相關因素代碼 </summary>
        public string INDUCEMENTS_CODE { set; get; }

        /// <summary> 相關因素中文名稱 </summary>
        public string INDUCEMENTS_DESC { set; get; }

        /// <summary> 目標代碼 </summary>
        public string TARGET_CODE { set; get; }

        /// <summary> 目標中文名稱 </summary>
        public string TARGET_DESC { set; get; }
        
        /// <summary> 措施代碼 </summary>
        public string MEASURE_CODE { set; get; }

        /// <summary> 措施中文名稱 </summary>
        public string MEASURE_DESC { set; get; }

        /// <summary> 停用日期 </summary>
        public string DISABLE_DATE { set; get; }
    }

    public class User_Care_Plan_Item
    {
        /// <summary> 護理計劃PK </summary>
        public string M_ID { set; get; }

        /// <summary> 護理問題PK </summary>
        public string F_ID { set; get; }

        /// <summary> 定義性特徵PK </summary>
        public string D_ID { set; get; }

        /// <summary> 相關因素PK </summary>
        public string A_ID { set; get; }

        /// <summary> 目標PK </summary>
        public string G_PK_ID { set; get; }

        /// <summary> 活動PK </summary>
        public string I_ID { set; get; }

        /// <summary> 可否修改</summary>
        public string Modify { set; get; }
        /// <summary> 時間</summary>
        public string RecordTime { set; get; }

        /// <summary> 內容</summary>
        public string Item { set; get; }

        /// <summary> 內容</summary>
        public string Content { set; get; }

        /// <summary> 是否為自定義</summary>
        public string Custom { set; get; }

        /// <summary> 是否評值</summary>
        public string Score { set; get; }

        /// <summary> 評值狀態</summary>
        public string Score_Status { set; get; }

        /// <summary> 不適用原因</summary>
        public string Reason { set; get; }

        /// <summary> 評值時間</summary>
        public string ScoreTime { set; get; }

        /// <summary> 停止時間</summary>
        public string StopDate { set; get; }

    }
}