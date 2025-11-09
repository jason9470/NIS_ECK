using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NIS.Models;
using System.Data;

namespace NIS.Models
{

    public class ChemotherapyMedicine : DBAction
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
        /// 取得UDORDERINFO
        /// </summary>
        /// <param name="FEENO">依據批價序號號</param>
        /// <param name="ORDSEQ">依據醫囑序號</param>
        /// <returns>回傳table</returns>
        public Boolean ck_sheet(string FEENO, string ORDSEQ, string page)
        {

            string DataSheet = "UDORDERINFO";
            if (page != "")
            {
                if (page == "chemo") DataSheet = "ChemotherapyDrugOrder";
            }
            bool t = false;
            string sql = "SELECT count(0) from " + DataSheet + " WHERE ORDSEQ = '" + ORDSEQ + "' AND FEENO = '" + FEENO + "' ";
            if (page != "")
            {
                sql = "SELECT count(0) from " + DataSheet + " WHERE SHEETNO = '" + ORDSEQ + "' AND FEENO = '" + FEENO + "' ";
            }

            DataTable dt = base.gettable(sql);
            if (dt.Rows[0][0].ToString() == "0") { t = true; }
            return t;
        }




        /// <summary>
        /// 取得UDORDERINFO
        /// </summary>
        /// <param name="page">依據傳入頁面</param>
        /// <param name="FEENO">依據批價序號號</param>
        /// <param name="ORDSEQ">依據醫囑序號</param>
        /// <param name="ITEM">依據項目</param>
        /// <param name="OTHER">依據其他</param>
        /// <param name="UDODRGCODE">依據藥包條碼</param>
        /// <param name="UDOTYPFREQN">依據用藥類別</param>
        /// <param name="status">狀態</param>
        /// <param name="firstday">開始日期</param>
        /// <param name="lastday">結束日期</param>
        /// <returns>回傳table</returns>
        public DataTable sql_udorderinfo(string page, string FEENO, string ORDSEQ, string ITEM, string OTHER, string UDODRGCODE, string UDOTYPFREQN, string status, string firstday, string lastday)
        {
            string DataSheet = "";
            if (page != "")
            {
                if (page == "Execute") DataSheet = "CHEMOTHERAPYDRUGORDER";
                if ((page == "Cancel") || (page == "Search_Print")) DataSheet = "UNCRCPTIME";
            }
            string sql = "SELECT *  FROM " + DataSheet + " WHERE FEENO = '" + FEENO + "' ";

            if (OTHER != "all") sql += "AND ORDSEQ not in (select SHEETNO from CHEMOTHERAPY where ORDSEQ=CHEMOTHERAPYDRUGORDER.SHEETNO) ";
       
            if (ORDSEQ != "")
                sql += "AND ORDSEQ = '" + ORDSEQ + "' ";
            if (UDODRGCODE != null && UDODRGCODE != "")
                sql += "AND UDODRGCODE = '" + UDODRGCODE + "' ";
            if (UDOTYPFREQN != "")
                sql += "AND UDOTYPFREQN = '" + UDOTYPFREQN + "' ";
            if (status != "")
                sql += "AND STATUS <> '" + status + "' ";
            if (firstday != "")
                sql += "AND ORDBGNDTTM > '" + firstday + "' ";
            if (lastday != "")
                sql += "AND ORDENDDTTM < '" + lastday + "' ";
            if (OTHER != "" && ITEM != "")
                sql += "AND " + ITEM + " = '" + OTHER + "' ";
            if (page == "Cancel")
            {
                sql += "AND STATUS <> '結束' ";
            }
            sql += " order by SEGUENCE";


            return base.gettable(sql);
        }

        /// <summary>
        /// 取得USE_FREQUENCE
        /// </summary>
        /// <param name="FEQ">依據藥物頻次</param>
        /// <returns>回傳table</returns>
        public DataTable sql_USE_FREQUENCE(string FEQ)
        {
            string sql = "SELECT * FROM USE_FREQUENCE WHERE 0 = 0 ";
            if (FEQ != "")
                sql += "AND RTRIM(FEQ) = '" + FEQ + "' ";
            else
                sql += "AND 1 <> 1 ";

            sql += "and (USE_TIME > (SELECT (to_char(sysdate,'hh24mi'))today from dual) OR nvl(use_time,'emp') ='emp' ) ";
            return base.gettable(sql);
        }
        /////// <summary>
        /////// 取得USE_FREQUENCE_add
        /////// </summary>
        /////// <param name="FEQ">依據藥物頻次</param>
        /////// <returns>回傳值</returns>
        ////public string sql_USE_FREQUENCE_add(string FEQ)
        ////{        
        ////    string Frequency = "";
        ////    DataTable dt_b = new DataTable();
        ////    dt_b = sql_USE_FREQUENCE(FEQ.ToString());
        ////    foreach (DataRow dr_b in dt_b.Rows)
        ////    {
        ////        if (dr_b["USE_TIME"].ToString() != "")
        ////        {
        ////            Frequency += dr_b["USE_TIME"].ToString() + ",";
        ////        }
        ////        else
        ////        {
        ////            Frequency = dr_b["FEQ"].ToString().Trim() + ",";
        ////        }
        ////    }
        ////    return Frequency;
        ////}
        /////// <summary>
        /////// 取得USE_FREQUENCE_add 時間頻率列
        /////// </summary>
        /////// <param name="FEQ">依據藥物頻次</param>
        /////// <returns>回傳值</returns>
        ////public string sql_USE_FREQUENCE_gettext(string FEQ,string ORDSEQ)
        ////{
        ////    string text_name = "";          
        ////    DataTable dt_b = new DataTable();
        ////    dt_b = sql_USE_FREQUENCE(FEQ.ToString());
        ////    foreach (DataRow dr_b in dt_b.Rows)
        ////    {
        ////        if (dr_b["USE_TIME"].ToString() != "")                            
        ////            text_name += ORDSEQ +"_"+ dr_b["USE_TIME"].ToString() + ",";                
        ////        else
        ////            //text_name = ORDSEQ + "_" + dr_b["USE_TIME"].ToString() + ",";
        ////            text_name = ORDSEQ + "_" + dr_b["FEQ"].ToString().Trim() + ",";
        ////    }
        ////    return text_name;
        ////}
        /// <summary>
        /// 取得sql_ch_add 日期轉換
        /// </summary>
        /// <param name="FEQ">存檔日期</param>
        /// <returns>回傳值</returns>
        public string sql_ch_add(DateTime savetime)
        {
            string newdate = "";
            newdate = savetime.ToString("yyyy/dd/mm");

            return newdate;
        }


    }
   
}
