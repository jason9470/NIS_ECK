using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NIS.Models;
using System.Data;

namespace NIS.Models
{

    public class ConstraintsAssessment : DBAction
    {

        /// <summary>
        /// 新增
        /// </summary>
        /// <returns>傳回成功筆數</returns>
        public int insert(string TableName, DataTable dt)
        {
            int eftrow = 0;
            List<DBItem> insertDataList = new List<DBItem>();
            for (int i = 1; i < dt.Rows.Count; i++)
            {

                for (int j = 0; j < dt.Columns.Count; j++)
                {
                    if (dt.Rows[0][j].ToString() == "String")
                        insertDataList.Add(new DBItem(dt.Columns[j].ToString(), dt.Rows[i][j].ToString(), DBItem.DBDataType.String));
                    else if (dt.Rows[0][j].ToString() == "DataTime")
                        insertDataList.Add(new DBItem(dt.Columns[j].ToString(), dt.Rows[i][j].ToString(), DBItem.DBDataType.DataTime));
                    else if (dt.Rows[0][j].ToString() == "Number")
                        insertDataList.Add(new DBItem(dt.Columns[j].ToString(), dt.Rows[i][j].ToString(), DBItem.DBDataType.Number));

                }
                eftrow = eftrow + base.DBExecInsert(TableName, insertDataList);
                insertDataList.Clear();
            }
            return eftrow;
        }
        /// <summary>
        /// 更新
        /// </summary>
        public int upd(string TableName, DataTable dt)
        {
            int eftrow = 0;
            List<DBItem> insertDataList = new List<DBItem>();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                for (int j = 0; j < dt.Columns.Count - 1; j++)
                {
                    insertDataList.Add(new DBItem(dt.Columns[j].ToString(), dt.Rows[i][j].ToString(), DBItem.DBDataType.String));
                }
                eftrow = eftrow + base.DBExecUpdate(TableName, insertDataList, dt.Rows[i][dt.Columns.Count - 1].ToString());
                insertDataList.Clear();
            }
            return eftrow;
        }
        /// <summary>
        /// 取得 新增tableITEM欄位
        /// </summary>
        /// <returns>回傳table</returns>
        public DataTable get_tablelist(string page)
        {

            List<SelectListItem> tablelist = new List<SelectListItem>();
            DataTable dt = new DataTable();

            string DataSheet = "", item = "", s = "", f = "", not = "";
            switch (page)
            {
                case "careitem_factor":
                    DataSheet = "careitem_factor";
                    item = "F";
                    f = ",NANDA,SUBJECT,DIAGNOSIS";
                    not = "and f_id not in (select factor from caretable) ";
                    break;
                case "careitem_about":
                    DataSheet = "careitem_about";
                    item = "A";
                    break;
                case "careitem_goal":
                    DataSheet = "careitem_goal";
                    item = "G";
                    break;
                case "careitem_step":
                    DataSheet = "careitem_step";
                    item = "S";
                    s = ",TYPE";
                    break;
            }
            string sql = "select " + item + "_ID idtype,ITEM text " + s + f + " from " + DataSheet;
            sql += " where DC = '1' ";
            sql += "" + not;
            sql += " order by " + item + "_ID ";
            sql += "" + s;


            return base.gettable(sql);// tablelist;// base.gettable(sql);
        }

        /// <summary>
        /// 取得 新增table欄位
        /// </summary>
        /// <returns>回傳table</returns>
        public DataTable get_table(string item, string feeno, string other)
        {
            DataTable dt = new DataTable();
            string DataSheet = item;
            string sql = "";
            switch (item)
            {
                case "BINDTABLE": //約束評估 日期,次數列表
                    sql = "select * from " + DataSheet + " WHERE 1=1 ";
                    sql += " and feeno ='" + feeno + "' and status <> 'del' ";
                    if (other != null && other != "")
                    { sql += " and id ='" + other + "'"; }
                    sql += " order by  LPAD(bout,3,'0'),INSDT";
                    break;
                case "BINDTABLE2": //約束評估 日期,次數列表
                    DataSheet = "BINDTABLE";
                    sql = "select * from " + DataSheet + " WHERE 1=1 ";
                    sql += " and feeno ='" + feeno + "' and status <> 'del' ";
                    sql += " order by LPAD(bout,3,'0'),INSDT desc";
                    break;
                case "BINDTABLESAVE"://約束評估 以儲存評估項目
                    sql = "select * from " + DataSheet + " WHERE 1=1 ";
                    sql += " and feeno ='" + feeno + "' and status <> 'del' ";
                    if(other != null && other != "")
                    { sql += " and id ='" + other + "'"; }
                    sql += " order by LPAD(bout,3,'0'),assessdt";
                    break;
                case "BINDTABLESAVEMOD":
                    sql = "select * from BINDTABLESAVE WHERE 1=1 ";
                    sql += " and feeno ='" + feeno + "' and status <> 'del' ";
                    sql += "and bindid ='" + other + "' ";
                    sql += " order by assessdt";
                    break;
                case "BINDTABLESAVEROW":
                    sql = "select * from BINDTABLESAVE WHERE 1=1 ";
                    sql += " and feeno ='" + feeno + "' and assess <> '1' ";
                    sql += "and binid ='" + other + "' and status <> 'del' ";
                    sql += " order by assessdt";
                    break;
                case "BINDTABLESAVECOUNT":
                    string st = "", end = "";
                    if (other != "")
                    {
                        st = other.Split('|').GetValue(0).ToString();
                        end = other.Split('|').GetValue(1).ToString();
                    }
                    sql = "select count(*) from bindtablesave where status <> 'del' and assess ='0'";
                    sql += " and feeno ='" + feeno + "' ";
                    sql += " and ASSESSDT Between '" + st + "' and '" + end + "'";
                    break;
                case "BINDTABLESAVE_MODLIST":
                     st = "";
                     end = "";
                    if (other != "")
                    {
                        st = other.Split('|').GetValue(0).ToString();
                        end = other.Split('|').GetValue(1).ToString();
                    }
                    sql = "select * from bindtablesave where  status <> 'del' and assess ='0'";
                    sql += " and feeno ='" + feeno + "' ";
                    sql += " and ASSESSDT Between '" + st + "' and '" + end + "'";
                    break;
                case "BINDTABLE_ADD_REASON": //約束評估 日期,次數列表//主表串出約束原因
                    //sql = "select * from " + DataSheet + " WHERE 1=1 ";
                    //sql += " and feeno ='" + feeno + "' and status <> 'del' ";
                    //if(other != null && other != "")
                    //{ sql += " and id ='" + other + "'"; }
                    //sql += " order by  LPAD(bout,3,'0'),INSDT";
                    sql = "SELECT A.*,LPAD(A.bout,3,'0') boutdesc";
                    sql += ",(select  count(*)  FROM BINDTABLESAVE where id in a.id and feq<>'0' and status <>'del' ) countrow";
                    sql += ",NVL((SELECT B.REASON FROM BINDTABLESAVE B WHERE B.FEENO='" + feeno + "' AND B.STATUS<>'del' AND rownum = 1";
                    //if(other != null && other != "")
                    //{
                    sql += " AND A.ID=B.ID ";
                    //}
                    sql += "),'') REASON ";
                    sql += ",NVL((SELECT B.REASON_OTHER FROM BINDTABLESAVE B WHERE B.FEENO='" + feeno + "' AND B.STATUS<>'del' AND rownum = 1";
                    // if(other != null && other != "")
                    // {
                    sql += " AND A.ID=B.ID ";
                    //}
                    sql += "),'') REASON_OTHER ";
                    sql += " FROM BINDTABLE A WHERE 1=1  AND A.FEENO ='" + feeno + "' AND A.STATUS <> 'del'";
                    if(other != null && other != "")
                    { sql += " and id ='" + other + "'"; }
                    sql += " ORDER BY LPAD(A.bout,3,'0'),INSDT ";
                    break;
            }


            return base.gettable(sql);
        }
        /// <summary>
        /// 取得 table欄位
        /// </summary>
        /// <returns>回傳table</returns>
        public DataTable get_newrow(string id)
        {
            DataTable dt = new DataTable();
            string DataSheet = "BINDTABLESAVE";
            string sql = "select * from  " + DataSheet + " where  status <> 'del' and  ID='" + id + "' ";
            sql += "order by assessdt desc ";
            return base.gettable(sql);
        }

        /// <summary>
        /// 取得 table欄位
        /// </summary>
        /// <returns>回傳table</returns>
        public DataTable get_newrow1(string id)
        {
            DataTable dt = new DataTable();
            string DataSheet = "BINDTABLESAVE";
            string sql = "select * from  " + DataSheet + " where  status <> 'del' and  ID='" + id + "' and feq = '0' ";
            sql += "order by assessdt desc ";
            return base.gettable(sql);
        }

        /// <summary>
        /// 取得 新增table欄位
        /// </summary>
        /// <returns>回傳table</returns>
        public DataTable get_newtable(string item)
        {
            DataTable dt = new DataTable();
            string[] UpcrcpTime = { };
            string[] datatype_upcrcp = { };
            switch (item)
            {
                case "BINDTABLE":
                    UpcrcpTime = new string[] { "FEENO", "ID", "BOUT", "STARTDT", "ENDDT", "ASSESS",
                                                     "FEQ","STATUS","INSDT","INSNAME","MODDT","MODNAME",
                                                     "INSID","MODID" };
                    datatype_upcrcp = new string[] { "String", "String", "Number", "String", "String", "String",
                                                          "String", "String", "String", "String", "String", "String",
                                                          "String", "String"  };
                    break;
                case "BINDTABLADD":
                    UpcrcpTime = new string[] {      "FEENO", "ID", "BOUT", "STARTDT", "ENDDT", "ASSESS",
                                                     "FEQ","STATUS","INSDT","INSNAME","MODDT","MODNAME",
                                                     "INSID","MODID","ASSESSDT","ROW"};
                    datatype_upcrcp = null;
                    break;
                case "BINDTABLESAVE":
                    UpcrcpTime = new string[] { "FEENO", "ID", "BOUT", "STARTDT", "ENDDT", "ASSESS",
                                                     "FEQ","STATUS","INSDT","INSNAME","MODDT","MODNAME",
                                                     "INSID","MODID","EXPLAIN","RECORDS","OTHER","BINDID",
                                                     "ASSESSDT","REASON","CONSCIOUS","REACTION","POSITION",
                                                     "TOOL","CYCLE","PAUSE","ENDING","HARM","HARM1","HARM2"
                                                     
                    };
                    datatype_upcrcp = new string[] { "String", "String", "Number", "String", "String", "String",
                                                          "String", "String", "String", "String", "String", "String",
                                                          "String", "String" , "String", "String", "String","String",
                                                           "String", "String" , "String", "String", "String",
                                                           "String", "String" , "String", "String", "String",
                                                           "String", "String" };
                    break;

            }
            

            for (int i = 0; i < UpcrcpTime.Length; i++)
            {
                dt.Columns.Add(UpcrcpTime[i]);
            }

            DataRow dr = dt.NewRow();
            if (datatype_upcrcp != null)
            {
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    dr[i] = datatype_upcrcp[i];
                }
                dt.Rows.Add(dr);
            }
            return dt;
        }

        public string get_id(string feeno)
        {
            DataTable dt = base.gettable("select max(to_number(bout)) from BINDTABLE where feeno ='" + feeno + "' and  status <> 'del' ");
            if (dt != null && dt.Rows.Count > 0 && !string.IsNullOrWhiteSpace(dt.Rows[0][0].ToString()))
                return dt.Rows[0][0].ToString();
            else
                return "0";
        }

        /// <summary>
        /// 約束評估顯示語音歷程
        /// </summary>
        /// <param name="feeNo">feeNo</param>
        /// <returns>語音歷程資料</returns>
        public DataTable getVoiceHistory(string feeNo,string userNo, string startDate, string endDate)
        {
            string strSQL = "SELECT * FROM TALK_TABLE WHERE FEE_NO = '" + feeNo + "' AND USERID ='" + userNo + "' ";
            strSQL += " AND CREATE_TIME >= to_date ('" + startDate + " 00:00:00','yyyy/mm/dd hh24:mi:ss') AND CREATE_TIME <= to_date ('" + endDate + " 23:59:59' ,'yyyy/mm/dd hh24:mi:ss')";
            strSQL += " AND STATUS = 'Y' AND CONTROLLER = 'ConstraintsAssessment/ListNew'";
            strSQL += " ORDER BY CREATE_TIME DESC";
            DataTable dt = base.gettable(
                strSQL
            );

            return dt;
        }
       
        /// <summary>
        /// 檢查約束評估是否還有未完的 記錄到主表
        /// </summary>
        /// <param name="feeno">feeno</param>
        /// <param name="id">id</param>
        internal object CheckAssess(string feeno, string id)
        {
            int num = 0;
            DataTable dt = new DataTable();
            string sql = "select sum(assess) totle ,count(assess) count ";
            sql += " from BINDTABLESAVE t where  STATUS <> 'del' and feeno='" + feeno + "' and  id='" + id + "'";
            dt = base.gettable(sql);
            if (dt.Rows[0]["totle"] != null && (dt.Rows[0]["count"].ToString().Trim() == dt.Rows[0]["totle"].ToString().Trim() ||
               dt.Rows[0]["count"].ToString().Trim() == "0"))
            { num = 1; }
            //0表示尚有未完成
            //1表示已完成
            return num;
        }

    }
}
