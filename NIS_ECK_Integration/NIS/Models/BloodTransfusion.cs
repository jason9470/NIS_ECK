using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;

namespace NIS.Models
{
    public class BloodTransfusion : DBConnector
    {
        private DBConnector link;
        public BloodTransfusion()
        {
            this.link = new DBConnector();
        }

        /// <summary>
        /// 生命徵象帶入
        /// </summary>
        public class VitalSignImport
        {
            /// <summary>
            /// 生命徵象唯一碼
            /// </summary>
            public string vs_id { set; get; }

            /// <summary>
            /// 體溫
            /// </summary>
            public string vs_bt { set; get; }

            /// <summary>
            /// 脈搏
            /// </summary>
            public string vs_mp { set; get; }

            /// <summary>
            /// 呼吸速率
            /// </summary>
            public string vs_bf { set; get; }

            /// <summary>
            /// 血壓(收縮壓)
            /// </summary>
            public string vs_bp_sys { set; get; }

            /// <summary>
            /// 血壓(舒張壓)
            /// </summary>
            public string vs_bp_dia { set; get; }

            /// <summary>
            /// SPO2
            /// </summary>
            public string vs_sp { set; get; }

            /// <summary>
            /// 建立者ID
            /// </summary>
            public string create_user { set; get; }

            /// <summary>
            /// 建立時間
            /// </summary>
            public string create_date { set; get; }

            /// <summary>
            /// 修改者ID
            /// </summary>
            public string modify_user { set; get; }

            /// <summary>
            /// 修改時間
            /// </summary>
            public string modify_date { set; get; }
        }

        /// <summary>
        /// 血品
        /// </summary>
        public class BloodImport
        {
            /// <summary>
            /// 血品ID
            /// </summary>
            public string Blood_ID { set; get; }

            /// <summary>
            /// 血品時間
            /// </summary>
            public string Blood_Time { set; get; }

            /// <summary>
            /// 血品名稱
            /// </summary>
            public string Blood_Name { set; get; }

            /// <summary>
            /// 血品號碼
            /// </summary>
            public string Blood_Number { set; get; }

            /// <summary>
            /// 血品血型
            /// </summary>
            public string Blood_Type { set; get; }

            /// <summary>
            /// 血品單位
            /// </summary>
            public string Blood_Unit { set; get; }

            /// <summary>
            /// 血品到期日
            /// </summary>
            public string Blood_Exp { set; get; }
        }

        /// <summary>
        /// 血品資訊表單儲存用
        /// </summary>
        public class BloodVerification_Save
        {
            /// <summary>
            /// 是否已進行雙人覆核
            /// </summary>
            public string CHKTYPE { set; get; }

            /// <summary>
            /// 輸血紀錄Serial
            /// </summary>
            public string BloodTransfusionSerial { set; get; }

            /// <summary>
            /// 輸血開始日期
            /// </summary>
            public string Blood_StartDay { set; get; }

            /// <summary>
            /// 輸血開始時間
            /// </summary>
            public string Blood_StartTime { set; get; }

            /// <summary>
            /// 覆核人員ID
            /// </summary>
            public string Blood_VerifyUser1_Id { set; get; }

            /// <summary>
            /// 覆核人員姓名
            /// </summary>
            public string Blood_VerifyUser1_Name { set; get; }

            /// <summary>
            /// 血品資訊列表
            /// </summary>
            public BloodImport[] BloodList { set; get; }

            /// <summary>
            /// 護理紀錄內容
            /// </summary>
            public string CareRecordStr { set; get; }

        }

        /// <summary>
        /// 輸血反應表單儲存用
        /// </summary>
        public class TransfusionReaction_Save
        {
            /// <summary>
            /// 輸血紀錄Serial
            /// </summary>
            public string BloodTransfusionSerial { set; get; }

            /// <summary>
            /// 輸血反應Serial
            /// </summary>
            public string Serial { set; get; }

            /// <summary>
            /// 是否有輸血反應
            /// </summary>
            public string TransfusionReact { set; get; }

            /// <summary>
            /// 輸血反應日期
            /// </summary>
            public string ReactDay { set; get; }

            /// <summary>
            /// 輸血反應時間
            /// </summary>
            public string ReactTime { set; get; }

            /// <summary>
            /// 輸血反應症狀
            /// </summary>
            public string ReactSymptom { set; get; }

            /// <summary>
            /// 輸血反應症狀其他內容
            /// </summary>
            public string txt_ReactSymptom_Other { set; get; }

            /// <summary>
            /// 輸血反應處置
            /// </summary>
            public string ReactProcedure { set; get; }

            /// <summary>
            /// 生命徵象唯一碼
            /// </summary>
            public string VS_Id { set; get; }

            /// <summary>
            /// 生命徵象紀錄時間
            /// </summary>
            public string Assess_Time { set; get; }

            /// <summary>
            /// 體溫
            /// </summary>
            public string BT { set; get; }

            /// <summary>
            /// 脈搏
            /// </summary>
            public string HR { set; get; }

            /// <summary>
            /// 呼吸速率
            /// </summary>
            public string Respiratory { set; get; }

            /// <summary>
            /// 血壓(舒張壓)
            /// </summary>
            public string BP_Systolic { set; get; }

            /// <summary>
            /// 血壓(舒張壓)
            /// </summary>
            public string BP_Diastolic { set; get; }

            /// <summary>
            /// SPO2
            /// </summary>
            public string SPO2 { set; get; }

            /// <summary>
            /// 醫療科輸血反應評估
            /// </summary>
            public string MD_TRA { set; get; }

            /// <summary>
            /// 停止輸血
            /// </summary>
            public string DC_BloodTransfusion { set; get; }

            /// <summary>
            /// 護理紀錄內容
            /// </summary>
            public string CareRecordStr { get; set; }

        }

        /// <summary>
        /// 查詢輸血紀錄
        /// </summary>
        /// <param name="feeno">批價號</param>
        /// <param name="start">開始時間</param>
        /// <param name="end">結束時間</param>
        public DataTable QueryBloodTransfusionListData(string feeno, string start = "", string end = "")
        {
            string sql = "SELECT * FROM BLOODTRANSFUSION_DATA \n";
            sql += "WHERE FEENO = '" + feeno + "'\n";
            sql += "AND STATUS = 'Y' \n";
            if (!string.IsNullOrEmpty(start) && !string.IsNullOrEmpty(end))
            {
                sql += $"AND (VERIFICATION_TIME BETWEEN TO_DATE('{start} 00:00:00', 'YYYY-MM-DD HH24:MI:SS') AND TO_DATE('{end} 23:59:59', 'YYYY-MM-DD HH24:MI:SS') \n";
                sql += $"OR VERIFICATION_TIME IS NULL) \n";
            }
            sql += "ORDER BY VERIFICATION_TIME DESC";
            return link.DBExecSQL(sql);
        }

        /// <summary>
        /// 查詢血品資訊(Serial)
        /// </summary>
        /// <param name="BloodTransfusionSerial">輸血紀錄Serial</param>
        public DataTable QueryBloodVerificationDataBySerial(string BloodTransfusionSerial)
        {
            string sql = "SELECT * FROM BLOODVERIFICATION_DATA \n";
            sql += "WHERE BLOODTARNSFUSION_SERIAL = '" + BloodTransfusionSerial + "'\n";
            sql += "AND STATUS = 'Y' \n";
            sql += "ORDER BY RECORD_TIME DESC";
            return link.DBExecSQL(sql);
        }

        /// <summary>
        /// 查詢血品資訊(feeno)
        /// </summary>
        /// <param name="feeno">批價號</param>
        public DataTable QueryBloodVerificationDataByFeeNo(string feeno)
        {
            string sql = "SELECT * FROM BLOODVERIFICATION_DATA \n";
            sql += "WHERE FEENO = '" + feeno + "'\n";
            sql += "AND STATUS = 'Y' \n";
            sql += "ORDER BY RECORD_TIME DESC";
            return link.DBExecSQL(sql);
        }

        /// <summary>
        /// 查詢血品資訊(chartNo)
        /// </summary>
        /// <param name="chartNo">病歷號</param>
        public DataTable QueryBloodVerificationDataByChartNo(string chartNo)
        {
            string sql = "SELECT * FROM BLOODVERIFICATION_DATA \n";
            sql += "WHERE CHARTNO = '" + chartNo + "'\n";
            sql += "AND STATUS = 'Y' \n";
            sql += "ORDER BY RECORD_TIME DESC";
            return link.DBExecSQL(sql);
        }

        /// <summary>
        /// 查詢生命徵象(Serial)
        /// </summary>
        /// <param name="BloodTransfusionSerial">輸血紀錄Serial</param>
        /// <param name="Period">時間點(輸血前15分鐘'15minBefore'/輸血後15分鐘'15minAfter'/輸血結束'End')</param>
        public DataTable QueryBloodVitalSignDataBySerial(string BloodTransfusionSerial, string Period)
        {
            string sql = "SELECT * FROM BLOODVITALSIGN_DATA \n";
            sql += "WHERE BLOODTARNSFUSION_SERIAL = '" + BloodTransfusionSerial + "'\n";
            if (Period != "")
            {
                sql += "AND PERIOD = '" + Period + "' \n";
            }
            sql += "AND STATUS = 'Y' \n";
            sql += "ORDER BY RECORD_TIME DESC";
            return link.DBExecSQL(sql);
        }

        /// <summary>
        /// 查詢生命徵象(feeno)
        /// </summary>
        /// <param name="feeno">批價號</param>
        public DataTable QueryBloodVitalSignDataByFeeNo(string feeno)
        {
            string sql = "SELECT * FROM BLOODVITALSIGN_DATA \n";
            sql += "WHERE FEENO = '" + feeno + "'\n";
            sql += "AND STATUS = 'Y' \n";
            sql += "ORDER BY RECORD_TIME DESC";
            return link.DBExecSQL(sql);
        }

        /// <summary>
        /// 查詢輸血反應(Serial)
        /// </summary>
        /// <param name="BloodTransfusionSerial">輸血紀錄Serial</param>
        /// <param name="Serial">輸血反應Serial</param>
        public DataTable QueryTransfusionReactionDataBySerial(string BloodTransfusionSerial, string Serial)
        {
            string sql = "SELECT * FROM BLOODTRANSREACTION_DATA \n";
            sql += "WHERE BLOODTARNSFUSION_SERIAL = '" + BloodTransfusionSerial + "'\n";
            if (!string.IsNullOrEmpty(Serial))
            {
                sql += "AND SERIAL = '" + Serial + "' \n";
            }
            sql += "AND STATUS = 'Y' \n";
            sql += "ORDER BY RECORD_TIME DESC";
            return link.DBExecSQL(sql);
        }

        /// <summary>
        /// 查詢輸血反應(feeno)
        /// </summary>
        /// <param name="feeno">批價號</param>
        public DataTable QueryTransfusionReactionDataByFeeNo(string feeno)
        {
            string sql = "SELECT * FROM BLOODTRANSREACTION_DATA \n";
            sql += "WHERE FEENO = '" + feeno + "'\n";
            sql += "AND STATUS = 'Y' \n";
            sql += "ORDER BY RECORD_TIME DESC";
            return link.DBExecSQL(sql);
        }
    }
}