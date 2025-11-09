using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using NIS.Controllers;
using System.Data.OleDb;
using NIS.Models.DBModel;
using Com.Mayaminer;

namespace NIS.Models
{
    public class VitalSign : DBConnector
    {
        private BaseController baseC = new BaseController();

        /// <summary> 取得VS_ID </summary>
        /// <param name="feeno">批價序號</param>
        /// <param name="start">開始日</param>
        /// <param name="end">結束日</param>
        public string sel_vital_sign_id(string feeno, string start, string end, string str_HISVIEW= "")
        {
            string work_VITALSIGN = "";
            string VITALSIGN = "VS";

            if (str_HISVIEW == "HISVIEW")
            {
                work_VITALSIGN = " H_DATA_VITALSIGN";
                VITALSIGN = "VS";
            }
            else
            {
                work_VITALSIGN = " DATA_VITALSIGN";
                VITALSIGN = "VS";
            }

            string sql = "SELECT CREATE_DATE, VS_ID FROM " + work_VITALSIGN + " WHERE 0 = 0 ";
            if (feeno != "")
                sql += "AND FEE_NO = '" + feeno + "' ";
            if (VITALSIGN == "VS")
                sql += " AND DEL IS NULL ";
            if (start != "" && end != "")
                sql += "AND  TO_CHAR(CREATE_DATE,'yyyy/MM/dd HH:mi:ss') BETWEEN '" + start + "' AND '" + end + "' ";
            sql += "GROUP BY CREATE_DATE,VS_ID ORDER BY CREATE_DATE";

            return sql;
        }

        /// <summary> 取得VS_RECORD </summary>
        /// <param name="id">VS_ID</param>
        /// <param name="feeno">批價序號</param>
        /// <param name="vs_item">項目</param>
        /// <param name="record_time">評估時間</param>
        public string sel_vital_sign_data(string id, string feeno, string vs_item, string record_time, string  str_HISVIEW)
        {
            string work_VITALSIGN = "";
            string VITALSIGN = "VS";
            if (str_HISVIEW == "HISVIEW")
            {
                work_VITALSIGN = " H_DATA_VITALSIGN";
                VITALSIGN = "VS";
            }
            else
            {
                work_VITALSIGN = " DATA_VITALSIGN";
                VITALSIGN = "VS";
            }
             
            string sql = " SELECT * FROM "+ work_VITALSIGN + " WHERE 0 = 0 ";
            if (id != "")
                sql += "AND VS_ID = '" + id + "' ";
            if (feeno != "")
                sql += "AND FEE_NO = '" + feeno + "' ";
            if (id != "")
                sql += "AND VS_ID = '" + id + "' ";
            if (vs_item != "")
                sql += "AND VS_ITEM IN ('" + vs_item + "') ";
            if (VITALSIGN == "VS")
                sql += " AND DEL IS NULL ";
            if (record_time != "")
                // sql += "AND  to_date(CREATE_DATE,'yyyy/MM/dd HH24:mi:ss') = to_date('" + record_time + "','yyyy/MM/dd HH24:mi:ss') ";
                sql += "AND TO_DATE(TO_CHAR(CREATE_DATE,'yyyy/MM/dd HH24:mi:ss'),'yyyy/MM/dd HH24:mi:ss') = TO_DATE('" + record_time + "','yyyy/MM/dd HH24:mi:ss') ";
            return sql;
        }

        /// <summary> 取得批次量測 </summary>
        /// <param name="feeno">批價序號</param>
        /// <param name="date">日期</param>
        public string sel_mul_vital_sign_data(string feeno, DateTime date)
        {
            string sqlstr = "SELECT * FROM DATA_VITALSIGN WHERE VS_TYPE = 'M' ";
            sqlstr += "AND FEE_NO = '" + feeno + "' ";
            sqlstr += "AND RECORD_TIME BETWEEN '" + date.ToString("yyyy/MM/dd 00:00:00") + "' ";
            sqlstr += "AND '" + date.ToString("yyyy/MM/dd 23:59:59") + "' ";
            sqlstr += "AND DEL IS NULL ORDER BY RECORD_TIME ";
            return sqlstr;
        }

        /// <summary> 取得生命徵象項目設定 </summary>
        /// <param name="feeno">批價序號</param>
        public string sel_user_vs_option(string feeno)
        {
            return "SELECT * FROM NIS_SYS_USER_VSOPTIION WHERE EMP_NO = '" + feeno + "' ";
        }

        /// <summary> 取得異常值檢查表 </summary>
        public DataTable Get_Check_Abnormal_dt()
        {
            DataTable dt = new DataTable();
            base.DBExecSQL("SELECT * FROM NIS_SYS_VITALSIGN_OPTION ", ref dt);

            return dt;
        }

        public DataTable get_event(string feeno, string type ="", string no_type = "", string start = "", string end = "")
        {
            DataTable dt = new DataTable();
            string sqlstr = "SELECT * FROM NIS_SPECIAL_EVENT_DATA where feeno='" + feeno + "'";
            if (!string.IsNullOrEmpty(type))
            {
                sqlstr += " AND type_id in(" + type + ")";
            }
            if (!string.IsNullOrEmpty(no_type))
            {
                sqlstr += " AND type_id not in(" + no_type + ")";
            }
            if (!string.IsNullOrEmpty(start) && !string.IsNullOrEmpty(end))
            {
                DateTime start_time = Convert.ToDateTime(start);
                DateTime end_time = Convert.ToDateTime(end);
                sqlstr += "AND  TO_CHAR(CREATTIME,'yyyy/MM/dd HH:mi:ss') BETWEEN '" + start_time.ToString("yyyy/MM/dd HH:mm:ss") + "' AND '" + end_time.ToString("yyyy/MM/dd HH:mm:ss") + "' ";
            }
            sqlstr += " order by creattime asc,type_id";
            base.DBExecSQL(sqlstr, ref dt);
            return dt;
        }

        public List<VitalSignDataList> sel_vital(string feeno, DateTime start, DateTime end, string vs_item,string str_HISVIEW="")
        {
            List<VitalSignDataList> vs_data_list = new List<VitalSignDataList>();
            try
            {
                string sql = "";
                sql = sel_vital_sign_id(feeno, start.ToString("yyyy/MM/dd HH:mm:ss"), end.ToString("yyyy/MM/dd HH:mm:ss"), str_HISVIEW);
                if (base.DBExecSQL(sql, false))
                {
                    List<string[]> vs_id_list = new List<string[]>();
                    VitalSignDataList temp = null;
                    VitalSignData temp_data = null;
                    DataTable Dt = base.DBExecSQL(sql);
                    if (Dt.Rows.Count > 0)
                    {
                        for (int d = 0; d < Dt.Rows.Count; d++)
                        {
                            vs_id_list.Add(new string[] { Dt.Rows[d]["VS_ID"].ToString(), Convert.ToDateTime(Dt.Rows[d]["CREATE_DATE"].ToString()).ToString("yyyy/MM/dd HH:mm:ss") });
                        }
                        //初始化資料
                        for (int i = 0; i <= vs_id_list.Count - 1; i++)
                        {
                            sql = sel_vital_sign_data(vs_id_list[i][0], feeno, vs_item.Replace(",", "','"), vs_id_list[i][1], str_HISVIEW);                            
                            DataTable Dtt = DBExecSQL(sql);
                            if (Dtt.Rows.Count > 0)
                            {
                                temp = new VitalSignDataList();
                                temp.vsid = vs_id_list[i][0];
                                temp.recordtime = vs_id_list[i][1];
                                for (int d = 0; d < Dtt.Rows.Count; d++)
                                {
                                    temp_data = new VitalSignData();
                                    temp_data.vs_item = Dtt.Rows[d]["vs_item"].ToString();
                                    temp_data.vs_part = Dtt.Rows[d]["vs_part"].ToString();
                                    temp_data.vs_record = Dtt.Rows[d]["vs_record"].ToString();
                                    temp_data.vs_reason = Dtt.Rows[d]["vs_reason"].ToString();
                                    temp_data.vs_memo = Dtt.Rows[d]["vs_memo"].ToString();
                                    temp_data.vs_other_memo = Dtt.Rows[d]["vs_other_memo"].ToString();
                                    temp_data.create_user = Dtt.Rows[d]["create_user"].ToString();
                                    temp_data.create_date = Dtt.Rows[d]["create_date"].ToString();
                                    temp_data.modify_user = Dtt.Rows[d]["modify_user"].ToString();
                                    temp_data.modify_date = Dtt.Rows[d]["modify_date"].ToString();
                                    temp_data.vs_type = Dtt.Rows[d]["vs_type"].ToString();
                                    //temp_data.record_time = Dtt.Rows[d]["record_time"].ToString();
                                    temp.DataList.Add(temp_data);
                                }
                                vs_data_list.Add(temp);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = baseC.ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = baseC.ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, baseC.userinfo.EmployeesNo, tmp_action, ex.ToString(),ex);
            }

            return vs_data_list;
        }
        
        public class DataVitalSign {
            /// <summary>住院序號</summary>
            public string FEE_NO { set; get; }
            /// <summary>VS資料ID</summary>
            public string VS_ID { set; get; }
            /// <summary>項目</summary>
            public string VS_ITEM { set; get; }
            /// <summary>部位</summary>
            public string VS_PART { set; get; }
            /// <summary>紀錄值</summary>
            public string VS_RECORD { set; get; }
            /// <summary>不可測原因</summary>
            public string VS_REASON { set; get; }
            /// <summary>護理紀錄</summary>
            public string VS_MEMO { set; get; }
            /// <summary>建立人員代碼</summary>
            public string CREATE_USER { set; get; }
            /// <summary>建立時間(檢驗時間)</summary>
            public string CREATE_DATE { set; get; }
            /// <summary>修改人員代碼</summary>
            public string MODIFY_USER { set; get; }
            /// <summary>修改時間(DB新增、更新時間)</summary>
            public string MODIFY_DATE { set; get; }
            /// <summary>護理處置</summary>
            public string VS_OTHER_MEMO { set; get; }
            /// <summary>因前次發燒記錄(或著在帶入時當來源使用)</summary>
            public string VS_TYPE { set; get; }
            /// <summary>是否有轉護理紀錄(Y:是、N:否、checked:人員勾選)</summary>
            public string PLAN { set; get; }
        }

        public class DB_NIS_VITALSIGN
        {
            /// <summary>住院序號</summary>
            public string FEE_NO { get; set; } = "";
            /// <summary>VS資料ID</summary>
            public string VS_ID { get; set; } = "";
            /// <summary>項目</summary>
            public string VS_ITEM { get; set; } = "";
            /// <summary>部位</summary>
            public string VS_PART { get; set; } = "";
            /// <summary>紀錄值</summary>
            public string VS_RECORD { get; set; } = "";
            /// <summary>不可測原因</summary>
            public string VS_REASON { get; set; } = "";
            /// <summary>護理紀錄</summary>
            public string VS_MEMO { get; set; } = "";
            /// <summary>建立人員</summary>
            public string CREATE_USER { set; get; }
            /// <summary>建立時間</summary>
            public DateTime CREATE_DATE { get; set; }
            /// <summary>修改人員</summary>
            public string MODIFY_USER { set; get; }
            /// <summary>修改時間</summary>
            public DateTime MODIFY_DATE { get; set; }
            /// <summary>護理處置</summary>
            public string VS_OTHER_MEMO { get; set; } = "";
            /// <summary>因前次發燒記錄</summary>
            public string VS_TYPE { get; set; } = "";
            /// <summary>是否有轉護理紀錄(Y:是、N:否、checked:人員勾選)</summary>
            public string PLAN { set; get; }
        }

        /// <summary>
        /// 特殊事件註記(NIS_SPECIAL_EVENT_DATA)
        /// </summary>
        public class DB_NIS_SPECIALEVENT
        {
            /// <summary>PK序號</summary>
            public string EVENT_ID { get; set; } = "";
            /// <summary>批價序號</summary>
            public string FEENO { get; set; } = "";
            /// <summary>紀錄人員</summary>
            public string CREATNO { get; set; } = "";
            /// <summary>記錄時間</summary>
            public DateTime CREATTIME { get; set; }
            /// <summary>類型</summary>
            public string TYPE_ID { get; set; } = "";
            /// <summary>特殊事件內容</summary>
            public string CONTENT { get; set; } = "";
            /// <summary>編輯內容</summary>
            public string EDIT_ITEM { get; set; } = "";
        }

        /// <summary>
        /// 洗腎註記
        /// </summary>
        public class DB_NIS_Dehydration
        {
            /// <summary>PK序號</summary>
            public string EVENT_ID { get; set; } = "";
            /// <summary>批價序號</summary>
            public string FEENO { get; set; } = "";
            /// <summary>紀錄人員</summary>
            public string CREATNO { get; set; } = "";
            /// <summary>記錄時間</summary>
            public DateTime CREATTIME { get; set; }
            /// <summary>類型</summary>
            public string TYPE_ID { get; set; } = "";
            /// <summary>特殊事件內容</summary>
            public string CONTENT { get; set; } = "";
            /// <summary>編輯內容</summary>
            public string EDIT_ITEM { get; set; } = "";
        }
        /// <summary>
        /// Tpr資料
        /// </summary>
        public class ClassData
        {
            /// <summary>樣式Tag</summary>
            public string class_tag { get; set; }
            /// <summary>群組名稱</summary>
            public string group_name { get; set; }
            /// <summary>項目時間</summary>
            public string item_time { get; set; }
            /// <summary>項目部位</summary>
            public string item_part { get; set; }
            /// <summary>項目名稱</summary>
            public string item_name { get; set; }
            /// <summary>項目值</summary>
            public string item_value { get; set; }
            /// <summary>數值狀態(異常值)</summary>
            public string value_status { get; set; }

        }
        /// <summary>
        /// Tpr資料
        /// </summary>
        public class NIS_SYS_VITALSIGN_OPTION
        {
            /// <summary>模組ID</summary>
            public string MODEL_ID { get; set; }
            /// <summary>顯示名稱</summary>
            public string TITLE { get; set; }
            /// <summary>限值</summary>
            public string VALUE_LIMIT { get; set; }
            /// <summary>判斷</summary>
            public string DECIDE { get; set; }
            /// <summary>項目</summary>
            public string ITEM { get; set; }
            /// <summary>附加項目</summary>
            public string OTHER_ITEM { get; set; }
            /// <summary>附加標題</summary>
            public string OTHER_TITLE { get; set; }
            /// <summary>備註</summary>
            public string REMARK { get; set; }

        }
        /// <summary>
        /// 前端Partial Tpr
        /// </summary>
        public class ViewTpr
        {
            /// <summary>批價序號</summary>
            public string feeno { get; set; }
            /// <summary>出生日期</summary>
            public string birthday { get; set; }
            /// <summary>生產天數 (生產週數天數換算為生產天數)</summary>
            public string gest_d { get; set; }
            /// <summary>住院日期</summary>
            public string in_date { get; set; }
            /// <summary>資料起</summary>
            public string start_date { get; set; }
            /// <summary>資料迄</summary>
            public string end_date { get; set; }
            /// <summary>繪製天數</summary>
            public int draw_day { get; set; }
            /// <summary>Tpr input IO項目</summary>
            public List<DB_NIS_SYS_PARAMS> i_item_list = new List<DB_NIS_SYS_PARAMS>();
            /// <summary>Tpr output IO項目</summary>
            public List<DB_NIS_SYS_PARAMS> o_item_list = new List<DB_NIS_SYS_PARAMS>();
            /// <summary>Tpr資料</summary>
            public List<TprData> tpr_data = new List<TprData>();
        }
        /// <summary>
        /// Tpr Data
        /// </summary>
        public class TprData
        {
            /// <summary>資料日期</summary>
            public string data_date { get; set; }
            /// <summary>資料清單</summary>
            public List<ClassData> data_list { get; set; }
        }
        public class IO_Inquire //護理計畫總表
        {
            /// <summary>批價序號</summary>
            public string IO_ID { get; set; }
            /// <summary>健康問題 ID</summary>
            public string ITEMID { get; set; }
            /// <summary>編號</summary>
            public string TYPEID { get; set; }
            /// <summary>健康問題描述</summary>
            public string CREATETIME { get; set; }
            /// <summary>計畫起始日</summary>
            public string AMOUNT { get; set; }
            /// <summary>計畫結束日</summary>
            public string AMOUNT_UNIT { get; set; }
            /// <summary>紀錄者員編</summary>
            public string REASON { get; set; }
            /// <summary>紀錄者姓名</summary>
            public string TUBEROW { get; set; }
            /// <summary>紀錄者單位</summary>
            public string TUBEID { get; set; }
            /// <summary>最後修改者</summary>
            public string FEENO { get; set; }
            /// <summary>最後修改者姓名</summary>
            public string TYPE_NAME { get; set; }
            /// <summary>最後修改者單位</summary>
            public string POSITION { get; set; }
            /// <summary>最後修改時間</summary>
            public string LOCATION_NAME { get; set; }
            /// <summary>是否有修改</summary>
            public string NUBER_NAME { get; set; }
            /// <summary>最後評值者</summary>
            public string NUMBEROTHER { get; set; }
            /// <summary>最後評值者</summary>
            public string TUBE_CONTENT { get; set; }
        }

    }
}