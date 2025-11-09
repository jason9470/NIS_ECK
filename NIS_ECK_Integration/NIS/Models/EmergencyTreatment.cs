using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

namespace NIS.Models
{
    public class EmergencyTreatment : DBConnector
    {
        public EmergencyTreatment()
        {
        }

        /// <summary>
        /// 生命徵象帶入
        /// </summary>
        public class EmergencyTreatment_VitalSignImport
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
        /// 取得EKG
        /// </summary>
        /// <param name="chartNO">病歷號</param>
        public DataTable get_EKG(string chartNO)
        {
            DataTable dt = new DataTable();
            string start = DateTime.Now.AddDays(-365).ToString("yyyy/MM/dd");
            string end = DateTime.Now.AddDays(1).ToString("yyyy/MM/dd");

            string sql = "SELECT * FROM MAYA_EKG WHERE HISNUM = '" + chartNO + "' AND imgdatetime BETWEEN TO_DATE('" + start + "', 'YYYY/MM/DD')  AND TO_DATE('" + end + "', 'YYYY/MM/DD')";

            base.DBExecSQL(sql, ref dt);

            return dt;

        }

        /// <summary>
        /// 查詢急救主表
        /// </summary>
        /// <param name="FeeNo">批價號</param>
        /// <param name="MasterID">急救紀錄單ID</param>
        public DataTable QueryMaster(string FeeNo = "", string MasterID = "")
        {
            DataTable dt = new DataTable();
            if (string.IsNullOrEmpty(FeeNo) && string.IsNullOrEmpty(MasterID))
            {
                return dt;
            }
            string sql = "SELECT * FROM EMERGENCYTREATMENT_MASTER \n";
            sql += "WHERE STATUS = 'Y' \n";
            if (!string.IsNullOrEmpty(FeeNo))
            {
                sql += "AND FEENO ='" + FeeNo + "' \n";
            }
            if (!string.IsNullOrEmpty(MasterID))
            {
                sql += "AND ID ='" + MasterID + "' \n";
            }
            sql += "ORDER BY START_TIME DESC \n";
            base.DBExecSQL(sql, ref dt);
            return dt;
        }

        /// <summary>
        /// 查詢急救模板(急救前/急救後/藥物紀錄)
        /// </summary>
        /// <param name="FeeNo">批價號</param>
        /// <param name="MasterID">急救紀錄單主表ID</param>
        /// <param name="TemplateID">急救紀錄單模板ID</param>
        /// <param name="Type">急救模板類型(急救前Brfore/急救後After/藥物紀錄Note)</param>
        public DataTable QueryTemplate(string FeeNo = "", string MasterID = "", string TemplateID = "", string Type = "")
        {
            DataTable dt = new DataTable();
            if (string.IsNullOrEmpty(FeeNo) && string.IsNullOrEmpty(MasterID) && string.IsNullOrEmpty(TemplateID))
            {
                return dt;
            }
            string sql = "SELECT * FROM EMERGENCYTREATMENT_TEMPLATE \n";
            sql += "WHERE STATUS = 'Y' \n";
            if (!string.IsNullOrEmpty(FeeNo))
            {
                sql += "AND FEENO ='" + FeeNo + "' \n";
            }
            if (!string.IsNullOrEmpty(MasterID))
            {
                sql += "AND MASTER_ID ='" + MasterID + "' \n";
            }
            if (!string.IsNullOrEmpty(TemplateID))
            {
                sql += "AND ID ='" + TemplateID + "' \n";
            }
            if (!string.IsNullOrEmpty(Type))
            {
                sql += "AND TYPE ='" + Type + "' \n";
            }
            sql += "ORDER BY RECORD_TIME DESC \n";
            base.DBExecSQL(sql, ref dt);
            return dt;
        }

        /// <summary>
        /// 查詢急救明細
        /// </summary>
        /// <param name="MasterID">急救紀錄單主表ID</param>
        /// <param name="TemplateID">急救紀錄單模板ID</param>
        public DataTable QueryDetail(string MasterID = "", string TemplateID = "")
        {
            DataTable dt = new DataTable();
            if (string.IsNullOrEmpty(MasterID) && string.IsNullOrEmpty(TemplateID))
            {
                return dt;
            }
            string sql = "SELECT * FROM EMERGENCYTREATMENT_DETAIL \n";
            sql += "WHERE STATUS = 'Y' \n";
            if (!string.IsNullOrEmpty(MasterID))
            {
                sql += "AND MASTER_ID ='" + MasterID + "' \n";
            }
            if (!string.IsNullOrEmpty(TemplateID))
            {
                sql += "AND TEMPLATE_ID ='" + TemplateID + "' \n";
            }
            base.DBExecSQL(sql, ref dt);
            return dt;
        }
    }
}