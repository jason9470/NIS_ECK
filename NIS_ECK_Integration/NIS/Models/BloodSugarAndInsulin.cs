
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
using Com.Mayaminer;

namespace NIS.Models
{

    public class BloodSugarAndInsulin : DBAction
    {
        private DBConnector DB;
        private BaseController baseC = new BaseController();
        private string mode = NIS.MvcApplication.iniObj.NisSetting.ServerMode.ToString();
        public BloodSugarAndInsulin()
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

                DataTable Dt = this.DBExecSQL(sql);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        MyList.Add(new SelectListItem()
                        {
                            Text = Dt.Rows[i]["ITEM"].ToString().Trim(),
                            Value = Dt.Rows[i]["VALUE"].ToString().Trim(),
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
                write_logMsg(tmp_controller, baseC.userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }

            return MyList;
        }



        /// <summary>
        /// 取得BLOODSUGAR(血糖監測資料表)
        /// </summary>
        /// <param name="FEENO">依據批價序號號</param>
        /// <param name="BSID">x流水號</param>
        /// <param name="status">狀態</param>
        /// <param name="firstday">開始日期</param>
        /// <param name="lastday">結束日期</param>
        /// <returns>回傳table</returns>
        public DataTable sql_BStable(string FEENO, string BSID, string status, string firstday, string lastday)
        {
            string DataSheet = "BLOODSUGAR";

            string sql = "SELECT * FROM " + DataSheet + " WHERE FEENO = '" + FEENO + "' ";
            if (BSID != "")
                sql += "AND BSID = '" + BSID + "' ";
            if (status != "")
                sql += "AND ((STATUS <> '" + status + "')  OR (STATUS is NULL)) ";
            if (firstday != "")
                sql += "AND INDATE > '" + firstday + "' ";
            if (lastday != "")
                sql += "AND INDATE < '" + lastday + "' ";
            sql += " order by INDATE DESC";


            return base.gettable(sql);
        }



        /// <summary>
        /// 取得get_itemname
        /// </summary>
        /// <param name="item">輸入項目</param>
        /// <param name="val">輸入數值</param>
        /// <returns>回傳值</returns>
        public string get_itemname(string item, string val)
        {
            string getstring = "";

            DataTable dt_item = new DataTable();
            if (val != "")
            {
                string[] vArray = val.Split(',');
                foreach (string i in vArray)
                {

                    dt_item = sql_symptoms(item, i.ToString());
                    getstring += dt_item.Rows[0][0].ToString() + ";";
                }
                return getstring;
            }
            return "";
        }

        public DataTable GetBSDS()
        {
            string sql = "SELECT * FROM BLOODSUGAR_SYMPTOMS";
            return base.gettable(sql);
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
        /// 取得SPECIALDRUG(特殊藥物注射資料表&& 胰島素注射藥物)
        /// </summary>
        /// <param name="FEENO">依據批價序號號</param>
        /// <param name="PAGE">頁面</param>
        /// <param name="SDID">流水號</param>
        /// <param name="status">狀態</param>
        /// <param name="firstday">開始日期</param>
        /// <param name="lastday">結束日期</param>
        /// <returns>回傳table</returns>
        public DataTable sql_SDtable(string FEENO, string PAGE, string SDID, string status, string firstday, string lastday)
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
            sql += " order by INDATE DESC)a where rownum<=1 ";
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

            DataTable dt_position = base.gettable(sql);
            foreach (DataRow dr_p in dt_position.Rows)
            {
                if (dr_p["POSITION"].ToString() != "")
                {
                    positionList += dr_p["POSITION"].ToString() + ",";
                }
            }
            return positionList;
        }
        public DataTable get_BSugarInsulin(string feeno)
        {
            string strsql = "";
            strsql = "SELECT M.BSI_ID,M.FEENO,M.BSID,M.INID,TO_CHAR(M.CREATE_DATE,'yyyy/mm/dd hh24:mi:ss') CREATE_DATE,B.BLOODSUGAR,B.INSOPNAME,B.MEAL_STATUS,";
            strsql += "B.INDATE B_INDATE,I.INDATE I_INDATE,IN_DRUGNAME,I.IN_DOSE,I.IN_DOSEUNIT,I.POSITION,I.INJECTION,I.SS_DRUGNAME,I.SS_DOSE";
            strsql += " FROM BSUGARINSULIN M ";
            strsql += " LEFT JOIN BLOODSUGAR B ON M.FEENO = B.FEENO AND M.BSID = B.BSID ";
            strsql += " LEFT JOIN NIS_MED_INSULIN I ON M.FEENO = I.FEENO AND M.INID = I.INID WHERE M.FEENO = '" + feeno + "'";
            return base.gettable(strsql);
        }

        public DataTable get_BSugarInsulin_list(string feeno, string flag, string start, string end)
        {
            string strsql = "";
            if (flag == "I")
            {
                strsql += "SELECT I.INDATE I_INDATE,IN_DRUGNAME,NVL(I.IN_DOSE, '0') IN_DOSE,I.IN_DOSEUNIT,(select p_name from sys_params where p_model='common_medication' and p_group='insulin_section'";
                strsql += " and I.POSITION=sys_params.p_value) POSITION,I.INJECTION,NVL(I.SS_DRUGNAME,' ') SS_DRUGNAME,NVL(I.SS_DOSE,'0') SS_DOSE";
                strsql += " FROM NIS_MED_INSULIN I WHERE FEENO='" + feeno + "' and I.IN_DOSE <> '0'  ORDER BY I.INDATE";
                //"AND TO_DATE(I.INDATE,'YYYY/MM/DD HH24:MI:SS')";
                //strsql += " BETWEEN TO_DATE('" + start + "','YYYY/MM/DD HH24:MI:SS') AND TO_DATE('" + end + "','YYYY/MM/DD HH24:MI:SS') ORDER BY I.INDATE";
            }
            else if (flag == "B")
            {
                strsql += "SELECT B.BLOODSUGAR,B.INSOPNAME,B.INDATE B_INDATE,B.MEAL_STATUS ";
                strsql += " FROM BLOODSUGAR B WHERE FEENO='" + feeno + "' AND B.BLOODSUGAR IS NOT NULL AND STATUS <> 'del' ORDER BY B_INDATE";
                // strsql += " AND TO_DATE(B.INDATE,'YYYY/MM/DD HH24:MI:SS') BETWEEN TO_DATE('" + start + "','YYYY/MM/DD HH24:MI:SS') ";
                // strsql += " AND TO_DATE('" + end + "','YYYY/MM/DD HH24:MI:SS') ORDER BY B_INDATE";
            }
            else
            {
                strsql += "SELECT B.BLOODSUGAR,B.INSOPNAME,B.INDATE B_INDATE,I.INDATE I_INDATE,IN_DRUGNAME,";
                strsql += "I.IN_DOSE,I.IN_DOSEUNIT,I.POSITION,I.INJECTION,I.SS_DRUGNAME,I.SS_DOSE";
                strsql += " FROM (SELECT * FROM NIS_MED_INSULIN WHERE FEENO='" + feeno + "') I";
                strsql += " FULL Outer Join (SELECT * FROM BLOODSUGAR WHERE FEENO='" + feeno + "' AND BLOODSUGAR IS NOT NULL AND STATUS <> 'del') B";
                strsql += " ON SUBSTR(I.INDATE,1,13) BETWEEN SUBSTR(B.INDATE, 1, 13) AND SUBSTR(TO_CHAR(TO_DATE(B.INDATE,'YYYY/MM/DD HH24:MI') + 1/24,'YYYY/MM/DD HH24:MI'),1,13)";
                strsql += " WHERE B.BLOODSUGAR IS NOT NULL ORDER BY B.INDATE DESC,I.INDATE";
                //strsql += " ON SUBSTR(I.INDATE,1,13) = SUBSTR(B.INDATE,1,13) ORDER BY B.INDATE,I.INDATE";
            }
            return base.gettable(strsql);
        }

        public string Get_DrugListSql()
        {
            string sql = "";
            sql = " ((UD_TYPE = 'R' AND UD_CIR <> 'ASORDER') OR (UD_TYPE = 'P' AND UD_CIR NOT LIKE '%PRN%')) AND DRUG_TYPE <> 'V' AND ";
            sql += " (DC_DATE ='' OR DC_DATE IS NULL OR BEGIN_DAY <= ' + DC_DATE + ')";
            return sql;
        }

        public DataTable Get_BloodSugar_CostCodeList()
        {
            string sql = "SELECT P_VALUE FROM SYS_PARAMS \n";
            sql += "WHERE P_MODEL = 'BloodSugar' AND P_GROUP = 'CostCode' \n";
            sql += "ORDER BY P_SORT";

            return base.gettable(sql);
        }

        public DataTable Get_Unsynced_TEMPREADING(string StartTime, string EndTime, string UserID)
        {
            var TEMPREADING_Table = "POCT.TEMPREADING";
            var TEMPREADINGUP_Table = "POCT.TEMPREADINGUP";
            if (mode == "Maya")
            {
                TEMPREADING_Table = "TEMPREADING";
                TEMPREADINGUP_Table = "TEMPREADINGUP";
            }
            string sql = "SELECT A.*, B.UP_SYSTEM, B.UP_DATETIME FROM \n";
            sql += "(SELECT * FROM " + TEMPREADING_Table + " \n";
            sql += "WHERE DATETIME BETWEEN TO_DATE('" + StartTime + "','YYYY-MM-DD HH24:MI') AND TO_DATE('" + EndTime + "','YYYY-MM-DD HH24:MI') \n";
            if (!string.IsNullOrEmpty(UserID))
            {
                sql += "AND OPERATORID = '" + UserID + "' \n";
            }
            sql += "AND TYPE = '1') A \n";
            sql += "LEFT JOIN " + TEMPREADINGUP_Table + " B \n";
            sql += "ON A.ID = B.ID \n";
            sql += "AND B.UP_SYSTEM = 'NIS' \n";
            sql += "WHERE B.UP_DATETIME is null";

            return base.gettable(sql);
        }
    }
}