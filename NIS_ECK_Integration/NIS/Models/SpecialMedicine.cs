
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NIS.Controllers;
using NIS.Models;
namespace NIS.Models
{

    public class SpecialMedicine : DBAction
    {
         private DBConnector DB;
        private BaseController baseC = new BaseController();
        public SpecialMedicine()
         {
            this.DB = new DBConnector();
        }
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
        /// 取得新增項目內容
        /// </summary>
        /// <param name="type">血糖項目名稱</param>
        /// <param name="MyList">清單記憶體位置</param>
        public List<SelectListItem> getTypeItem(string type, string defaultValue = "")
        {
            List<SelectListItem> MyList = new List<SelectListItem>();
            try
            {
                string sqlStatment = string.Empty;
                string sql = string.Empty;

                sql = " SELECT * FROM BLOODSUGAR_SYMPTOMS ";
                sql += " WHERE TYPE='" + type + "' ";
                
                DataTable Dt = this.DB.DBExecSQL(sql);
                if (Dt.Rows.Count > 0)
                {
                    for (int d = 0; d < Dt.Rows.Count; d++)
                    {
                        MyList.Add(new SelectListItem()
                        {
                            Text = Dt.Rows[d]["ITEM"].ToString().Trim(),
                            Value = Dt.Rows[d]["VALUE"].ToString().Trim(),
                            Selected = false
                        });

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
            return MyList;
        }


        /// <summary>
        /// 取得SPECIALDRUG(特殊藥物注射資料表)
        /// </summary>
        /// <param name="FEENO">依據批價序號號</param>
        /// <param name="PAGE">頁面</param>
        /// <param name="SDID">流水號</param>
        /// <param name="status">狀態</param>
        /// <param name="firstday">開始日期</param>
        /// <param name="lastday">結束日期</param>
        /// <returns>回傳table</returns>
        public DataTable sql_SDtable(string FEENO,string PAGE, string SDID, string status, string firstday, string lastday)
        {
            string DataSheet = "SPECIALDRUG";
 
            string sql = "SELECT * FROM " + DataSheet + " WHERE FEENO = '" + FEENO + "' ";
            if (PAGE != "")
                sql += "AND PAGE = '" + PAGE + "' ";
            if (SDID != "")
                sql += "AND SDID = '" + SDID + "' ";
           if (status != "")
                sql += "AND STATUS <> '" + status + "' ";
            if (firstday != "")
                sql += "AND INDATE > '" + firstday + "' ";
            if (lastday != "")
                sql += "AND INDATE < '" + lastday + "' ";
            sql += " order by INDATE DESC";
         

            return base.gettable(sql);
        }
        /// <summary>
        /// 取得SPECIALDRUG_SET(禁止&拒絕注射部位 資料表)
        /// </summary>
        /// <param name="FEENO">依據批價序號號</param>
        /// <returns>回傳table</returns>
        public DataTable sql_DtSet(string FEENO)
        {
            string DataSheet = "SPECIALDRUG_SET";

            string sql = "select a.* from (SELECT * FROM " + DataSheet + " WHERE FEENO = '" + FEENO + "' ";
            sql += " order by INSDT DESC)a where rownum<=1 ";
            return base.gettable(sql);
        }

        /// <summary>
        /// 取得get_itemname
        /// </summary>
        /// <param name="item">輸入項目</param>
        /// <param name="val">輸入數值</param>
        /// <returns>回傳值</returns>
        public string get_itemname(string item,string val)
        {
            string getstring = "";
            
            DataTable dt_item = new DataTable();
            if (val != "")
            {
                string[] vArray = val.Split(',');
                foreach (string i in vArray)
                {
                    
                    dt_item = sql_symptoms(item,i.ToString());
                    getstring += dt_item.Rows[0][0].ToString() + ";";
                }
                return getstring;
            }
            return "";
        }

        /// <summary>
        /// 取得sql_symptoms
        /// </summary>
        /// <param name="item">輸入項目</param>
        /// <param name="val">輸入數值</param>
        /// <returns>回傳值</returns>
        public DataTable sql_symptoms(string item, string val)
        {
            string DataSheet = "BLOODSUGAR_SYMPTOMS";
            string sql = "SELECT ITEM FROM " + DataSheet + " WHERE VALUE = '" + val + "' ";
            return base.gettable(sql);
        }
        /// <summary>
        /// 取得已注射部位
        /// </summary>
        /// <param name="feeno">批價序號</param>
        /// <returns>回傳值</returns>
        public string get_position(string feeno)
        {
            string DataSheet = "SPECIALDRUG";
            string sql = "SELECT POSITION FROM " + DataSheet + " WHERE FEENO = '" + feeno + "' order by indate DESC ";
            string positionList = "";

            DataTable dt_position= base.gettable(sql);
            foreach (DataRow dr_p in dt_position.Rows)
            {
                if (dr_p["POSITION"].ToString() != "")
                {
                    positionList += dr_p["POSITION"].ToString() + ",";
                }
            }
             return positionList;
        }

    }
}