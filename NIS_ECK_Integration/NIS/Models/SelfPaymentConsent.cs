using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

namespace NIS.Models
{
    public class SelfPaymentConsent : DBConnector
    {
        private DBConnector link = new DBConnector();

        /// <summary>
        /// 恩主公醫院建立自費同意書API Request Model
        /// http://172.20.110.185:81/api/NisCreateSign
        /// </summary>
        public class NisCreateSign
        {
            /// <summary>病歷號</summary>
            public string CHART_NO { get; set; }

            /// <summary>病人身分證</summary>
            public string ID_NO { get; set; }

            /// <summary>開單醫師員編</summary>
            public string DOCTOR_NO { get; set; }

            /// <summary>開單醫師科別</summary>
            public string DOCTOR_DIV_NO { get; set; }

            /// <summary>就醫日期, 診間號,午別</summary>
            public string OPD_DAT_MRLOC_SHIFT { get; set; }

            /// <summary>來源別</summary>
            public string ORIGIN_TYPE { get; set; }

            /// <summary>同意書編號</summary>
            public string DOC_TYPE { get; set; }

            /// <summary>病人姓名</summary>
            public string PT_NAME { get; set; }

            /// <summary>建立同意書員編</summary>
            public string CREATE_CLERKID { get; set; }

            /// <summary>門診就醫序號 or住院編號</summary>
            public string FEENO_CLINIC_ID { get; set; }

            /// <summary>計價項目清單</summary>
            public List<OptionalPriceItem> OptionalPriceItems { get; set; } = new List<OptionalPriceItem>();

        }

        /// <summary>
        /// 恩主公醫院建立自費同意書API 計價項目
        /// http://172.20.110.185:81/api/NisCreateSign
        /// </summary>
        public class OptionalPriceItem
        {
            /// <summary>計價品項院內碼</summary>
            public string CODE { get; set; }

            /// <summary>使用數量</summary>
            public int USE_AMOUNT { get; set; }

            /// <summary>來源類別</summary>
            public string ORIGIN_TYPE { get; set; }
        }

        /// <summary>
        /// 恩主公醫院建立自費同意書API Response Model
        /// http://172.20.110.185:81/api/NisCreateSign
        /// </summary>
        public class NisCreateSignResponse
        {
            /// <summary>是否成功</summary>
            public Boolean isSuccess { get; set; }

            /// <summary>結果</summary>
            public NisCreateSignResponseModel model { get; set; }

            /// <summary>錯誤訊息</summary>
            public string errorMsg { get; set; }

            /// <summary>
            /// 結果物件
            /// </summary>
            public class NisCreateSignResponseModel
            {
                /// <summary>自費同意書url</summary>
                public string docurl { get; set; }

                /// <summary>自費同意書唯一號</summary>
                public string jobid { get; set; }

                /// <summary></summary>
                public string docid { get; set; }
            }
        }

        /// <summary>
        /// 恩主公醫院NIS系統查詢同意書API Request Model
        /// http://172.20.110.185:81/api/NisQuerySign
        /// </summary>
        public class NisQuerySignRequest
        {
            public string ChartNo { get; set; }
            public string DocumentNo { get; set; }
            public string StartDate { get; set; }  // 如果你希望用 DateTime，請參考下方備註
            public string EndDate { get; set; }
        }

        /// <summary>
        /// 恩主公醫院NIS系統查詢同意書API Response Model
        /// http://172.20.110.185:81/api/NisQuerySign
        /// </summary>
        public class NisQuerySignResponse
        {
            public bool result { get; set; }
            public string message { get; set; }
            public string desc { get; set; }
            public int code { get; set; }
            public int return_count { get; set; }
            public int no_limit_count { get; set; }
            public List<Job> jobs { get; set; } = new List<Job>();

            public class Job
            {
                public JobInfo jobinfo { get; set; } = new JobInfo();
                public List<JobSignHist> jobsignhists { get; set; } = new List<JobSignHist>();
            }

            public class JobInfo
            {
                public string JOB_ID { get; set; }
                public string ATTACH { get; set; }
                public string TEMP_ID { get; set; }
                public string TEMP_NAME { get; set; }
                public string JOB_DOC_ID { get; set; }
                public string JOB_DESC { get; set; }
                public string JOB_STATUS { get; set; }
                public string CREATE_TIME { get; set; }
                public string CORP_NO { get; set; }
                public string BIZ_TYPE { get; set; }
                public string JOB_TYPE { get; set; }
                public string JOB_ATTR1 { get; set; }
                public string JOB_ATTR2 { get; set; }
                public string JOB_ATTR3 { get; set; }
                public string JOB_ATTR4 { get; set; }
                public string JOB_ATTR5 { get; set; }
                public string JOB_ATTR6 { get; set; }
                public string JOB_ATTR7 { get; set; }
                public string JOB_ATTR8 { get; set; }
                public string JOB_ATTR9 { get; set; }
                public string JOB_ATTR10 { get; set; }
                public string TEMP_VER { get; set; }
                public string REMARK { get; set; }
                public string CREATE_USER { get; set; }
                public string UPDATE_USER { get; set; }
                public string UPDATE_TIME { get; set; }
            }

            public class JobSignHist
            {
                public int SIGN_SEQ { get; set; }
                public string SIGNER_ID { get; set; }
                public string SIGNER_NAME { get; set; }
                public int SIGNBLOCK_ID { get; set; }
                public int SIGN_OPTION { get; set; }
                public string SIGN_STATUS { get; set; }
                public string UPDATE_TIME { get; set; }
            }
        }

        /// <summary>
        /// 計價項目
        /// </summary>
        public class BillingItem
        {
            /// <summary>計價明細表唯一值</summary>
            public string serial_d { get; set; }

            /// <summary>院內碼</summary>
            public string ho_id { get; set; }

            /// <summary>名稱</summary>
            public string itemName { get; set; }

            /// <summary>總價</summary>
            public string itemPrice { get; set; }

            /// <summary>數量</summary>
            public string count { get; set; }

            /// <summary>自費價格</summary>
            public string selfPrice { get; set; }

            /// <summary>紀錄時間</summary>
            public string recordTime { get; set; }

            /// <summary>品項建立使用者ID</summary>
            public string itemCreateId { get; set; }

            /// <summary>品項建立使用者姓名</summary>
            public string itemCreateName { get; set; }
        }

        /// <summary>
        /// 編輯同意書註記儲存用
        /// </summary>
        public class SaveNote
        {

            /// <summary>同意書ID</summary>
            public string consentId { get; set; }

            /// <summary>註記內容</summary>
            public string note { get; set; }
        }

        /// <summary>
        /// 查詢計價確認後的明細
        /// </summary>
        /// <param name="feeno">批價號</param>
        /// <param name="start">開始時間</param>
        /// <param name="end">結束時間</param>
        /// <param name="selfPayConsent">是否已產生自費同意書(Y)</param>
        public DataTable queryComfirmDetail(string feeno = "", string start = "", string end = "", string selfPayConsent = "")
        {
            DataTable dt = new DataTable();
            // 稽核傳入參數是否皆為空
            if (feeno == "" && start == "" && start == "" && end == "" && selfPayConsent == "")
            {
                return dt;
            }
            string sql = "SELECT * FROM DATA_BILLING_CONFIRM_DETAIL \n";
            sql += "WHERE 0 = 0 \n";
            sql += "AND ITEM_IDENTITY = '自費' \n";
            sql += "AND STATUS = 'Y' \n";
            if (feeno != "")
            {
                sql += "AND FEENO = '" + feeno + "' \n";
            }
            if (start != "" && end != "")
            {
                sql += "AND RECORD_DATE BETWEEN TO_DATE('" + start + "','yyyy-MM-dd HH24:mi:ss') \n";
                sql += "AND TO_DATE('" + end + "','yyyy-MM-dd HH24:mi:ss') \n";
            }
            if (selfPayConsent == "Y")
            {
                sql += "AND SELF_PAY_CONSENT = 'Y' \n";
            }
            sql += "ORDER BY RECORD_DATE DESC \n";

            link.DBExecSQL(sql, ref dt);
            this.link.DBClose();

            return dt;
        }

        /// <summary>
        /// 查詢計價確認後的明細
        /// 連結自費同意書簽屬狀態及網址
        /// </summary>
        /// <param name="feeno">批價號</param>
        /// <param name="start">開始時間</param>
        /// <param name="end">結束時間</param>
        /// <param name="selfPayConsent">是否已產生自費同意書(Y)</param>
        public DataTable queryComfirmDetailJoinSelfPay(string feeno = "", string start = "", string end = "", string selfPayConsent = "")
        {
            DataTable dt = new DataTable();
            // 稽核傳入參數是否皆為空
            if (feeno == "" && start == "" && start == "" && end == "" && selfPayConsent == "")
            {
                return dt;
            }
            string sql = "SELECT D.*, M.JOB_STATUS, M.CONSENT_URL FROM \n";
            sql += "(SELECT * FROM DATA_BILLING_CONFIRM_DETAIL \n";
            sql += "WHERE 0 = 0 \n";
            sql += "AND ITEM_IDENTITY = '自費' \n";
            sql += "AND STATUS = 'Y' \n";
            if (feeno != "")
            {
                sql += "AND FEENO = '" + feeno + "' \n";
            }
            if (start != "" && end != "")
            {
                sql += "AND RECORD_DATE BETWEEN TO_DATE('" + start + "','yyyy-MM-dd HH24:mi:ss') \n";
                sql += "AND TO_DATE('" + end + "','yyyy-MM-dd HH24:mi:ss') \n";
            }
            if (selfPayConsent == "Y")
            {
                sql += "AND SELF_PAY_CONSENT = 'Y' \n";
            }
            sql += "ORDER BY RECORD_DATE DESC) D \n";
            sql += "LEFT JOIN (SELECT CONSENT_ID,JOB_STATUS,CONSENT_URL FROM SELFPAYCONSENT_MASTER WHERE STATUS = 'Y') M \n";
            sql += "ON D.SELF_PAY_CONSENT_ID = M.CONSENT_ID \n";

            link.DBExecSQL(sql, ref dt);
            this.link.DBClose();

            return dt;
        }

        /// <summary>
        /// 查詢自費同意書主表
        /// </summary>
        /// <param name="feeno">批價號</param>
        /// <param name="start">開始時間</param>
        /// <param name="end">結束時間</param>
        /// <param name="jobStatus">同意書簽核狀態</param>
        public DataTable queryConsentMaster(string feeno = "", string start = "", string end = "", string jobStatus = "", string consentId = "")
        {
            DataTable dt = new DataTable();
            // 稽核傳入參數是否皆為空
            if (feeno == "" && start == "" && start == "" && end == "" && jobStatus == "" && consentId == "")
            {
                return dt;
            }
            string sql = "SELECT * FROM SELFPAYCONSENT_MASTER \n";
            sql += "WHERE 0 = 0 \n";
            sql += "AND STATUS = 'Y' \n";
            if (feeno != "")
            {
                sql += "AND FEENO = '" + feeno + "' \n";
            }
            if (start != "" && end != "")
            {
                sql += "AND GENERATED_TIME BETWEEN TO_DATE('" + start + "','yyyy-MM-dd HH24:mi:ss') \n";
                sql += "AND TO_DATE('" + end + "','yyyy-MM-dd HH24:mi:ss')";
            }
            if (jobStatus != "")
            {
                if (jobStatus == "NOTED")
                {
                    sql += "AND NOTE_USER_ID IS NOT NULL \n";
                }
            }
            if (consentId != "")
            {
                sql += "AND CONSENT_ID = '" + consentId + "' \n";
            }
            sql += "ORDER BY GENERATED_TIME DESC \n";

            link.DBExecSQL(sql, ref dt);
            this.link.DBClose();

            return dt;
        }

        /// <summary>
        /// 查詢自費同意書明細表
        /// </summary>
        /// <param name="feeno">批價號</param>
        /// <param name="start">開始時間</param>
        /// <param name="end">結束時間</param>
        /// <param name="jobStatus">同意書簽核狀態</param>
        /// <param name="consentId">同意書ID</param>
        public DataTable queryConsentDetail(string feeno = "", string start = "", string end = "", string jobStatus = "", string consentId = "")
        {
            DataTable dt = new DataTable();
            // 稽核傳入參數是否皆為空
            if (feeno == "" && start == "" && start == "" && end == "" && jobStatus == "" && consentId == "")
            {
                return dt;
            }

            string sql = "SELECT * FROM SELFPAYCONSENT_DETAIL WHERE CONSENT_ID IN \n";
            sql += "(SELECT CONSENT_ID FROM SELFPAYCONSENT_MASTER \n";
            sql += "WHERE 0 = 0 \n";
            sql += "AND STATUS = 'Y' \n";
            if (feeno != "")
            {
                sql += "AND FEENO = '" + feeno + "' \n";
            }
            if (start != "" && end != "")
            {
                sql += "AND GENERATED_TIME BETWEEN TO_DATE('" + start + "','yyyy-MM-dd HH24:mi:ss') \n";
                sql += "AND TO_DATE('" + end + "','yyyy-MM-dd HH24:mi:ss')";
            }
            if (jobStatus != "")
            {
                if (jobStatus == "NOTED")
                {
                    sql += "AND NOTE IS NOT NULL \n";
                }
            }
            if (consentId != "")
            {
                sql += "AND CONSENT_ID = '" + consentId + "' \n";
            }
            sql += ") AND STATUS = 'Y'";

            link.DBExecSQL(sql, ref dt);
            this.link.DBClose();

            return dt;
        }
    }
}