using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NIS.Models;
using System.Data;

namespace NIS.Models
{
    public class IOManager : DBConnector
    {
        public DataTable sel_io_data(string userno, string feeno, string row, string starttime, string endtime, string unit, string type = "",string itemid="",string  str_HISVIEW="")
        {
            DataTable dt = new DataTable();
            string work_IOtable = "IO_DATA";

            //if (str_HISVIEW == "HISVIEW")
            //{
            //    work_IOtable = "H_IO_DATA_history";
            //}
            //else
            //{
            //    work_IOtable = "H_IO_DATA";
            //}
            string sql = "SELECT IO.* , TUBE.COLORID, TUBE.COLOROTHER, TUBE.NATUREID, TUBE.NATUREOTHER, TUBE.TASTEID, TUBE.TASTEOTHER ";
            //20160801 修改tpr帶出所有單位
            sql += ",CASE WHEN AMOUNT_UNIT <> '3' THEN AMOUNT ELSE AMOUNT*0.001 END AMOUNT_ALL ";
            sql += ",(SELECT P_GROUP FROM SYS_PARAMS WHERE P_MODEL = 'iotype' AND P_VALUE = IO.TYPEID)P_GROUP ";
            sql += ",(SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'iotype' AND P_VALUE = IO.TYPEID)P_NAME ";
            sql += ",(SELECT NAME FROM IO_ITEM WHERE IO_ITEM.ITEMID = IO.ITEMID)NAME ";
            sql += ",(SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'io' AND P_GROUP = 'outputcolor_Drainage' AND P_VALUE = TUBE.COLORID)COLORNAME ";
            sql += ",(SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'io' AND P_GROUP = 'outputnature_Drainage' AND P_VALUE = TUBE.NATUREID)NATURENAME ";
            sql += ",(SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'io' AND P_GROUP = 'outputtaste_Drainage' AND P_VALUE = TUBE.TASTEID)TASTENAME ";
            sql += "FROM "+ work_IOtable  + " IO LEFT OUTER JOIN TUBE_FEATURE TUBE ON IO.IO_ID = TUBE.FEATUREID WHERE 0 = 0 ";
            if (userno != "")
                sql += "AND CREANO = '" + userno + "' ";
            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if (row != "")
                sql += "AND IO_ROW = '" + row + "' ";
            if (itemid != "")
                sql += "AND ITEMID = '" + itemid + "' ";
            //if (unit != "")
            //    sql += "AND AMOUNT_UNIT = '" + unit + "' ";
            if (starttime != "")
                sql += "AND CREATTIME BETWEEN to_date('" + starttime + "','yyyy/mm/dd hh24:mi:ss') AND to_date('" + endtime + "','yyyy/mm/dd hh24:mi:ss') ";
            if (type != "")
                sql += "AND TYPEID = '" + type + "' ";

            sql += "AND DELETED IS NULL ORDER BY CREATTIME,IO_ROW";

            base.DBExecSQL(sql, ref dt);

            return dt;
        }

        public DataTable sel_io_data_byClass(string feeno, string date, string unit, string io_type)
        {
            DataTable dt = new DataTable();
            string[] class_list = { "D", "N", "E" };
            string sql = "SELECT TEMP.*,(D+N+E)TOTAL,(D_REASON+N_REASON+E_REASON)TOTAL_REASON FROM (SELECT P_VALUE,P_NAME ";
            for (int i = 0; i < class_list.Length; i++)
            {
                sql += ",NVL((SELECT SUM(AMOUNT) FROM IO_DATA WHERE 0 = 0 AND DELETED IS NULL AND TYPEID = P_VALUE ";
                if (unit != "")
                    sql += "AND AMOUNT_UNIT = '" + unit + "' ";
                if (feeno != "")
                    sql += "AND FEENO = '" + feeno + "' ";
                //早班 = "D";小夜 = "N"; 大夜 = "E"
                if (class_list[i] == "D")
                    sql += "AND CREATTIME BETWEEN to_date('" + date + " 07:01:00','yyyy/mm/dd hh24:mi:ss') AND to_date('" + date + " 15:00:59','yyyy/mm/dd hh24:mi:ss') ), 0)D ";
                else if (class_list[i] == "N")
                    sql += "AND CREATTIME BETWEEN to_date('" + date + " 15:01:00','yyyy/mm/dd hh24:mi:ss') AND to_date('" + date + " 23:00:59','yyyy/mm/dd hh24:mi:ss') ), 0)N ";
                else if (class_list[i] == "E")
                    sql += "AND CREATTIME BETWEEN to_date('" + date + " 23:01:00','yyyy/mm/dd hh24:mi:ss') AND to_date('" + Convert.ToDateTime(date).AddDays(+1).ToString("yyyy/MM/dd") + " 07:00:59','yyyy/mm/dd hh24:mi:ss') ), 0)E ";

                sql += ",NVL((SELECT COUNT(REASON) FROM IO_DATA WHERE 0 = 0 AND DELETED IS NULL AND TYPEID = P_VALUE ";
                if (unit != "")
                    sql += "AND AMOUNT_UNIT = '" + unit + "' ";
                if (feeno != "")
                    sql += "AND FEENO = '" + feeno + "' ";
                if (class_list[i] == "D")
                    sql += "AND CREATTIME BETWEEN to_date('" + date + " 07:01:00','yyyy/mm/dd hh24:mi:ss') AND to_date('" + date + " 15:00:59','yyyy/mm/dd hh24:mi:ss') ), 0)D_REASON ";
                else if (class_list[i] == "N")
                    sql += "AND CREATTIME BETWEEN to_date('" + date + " 15:01:00','yyyy/mm/dd hh24:mi:ss') AND to_date('" + date + " 23:00:59','yyyy/mm/dd hh24:mi:ss') ), 0)N_REASON ";
                else if (class_list[i] == "E")
                    sql += "AND CREATTIME BETWEEN to_date('" + date + " 23:01:00','yyyy/mm/dd hh24:mi:ss') AND to_date('" + Convert.ToDateTime(date).AddDays(+1).ToString("yyyy/MM/dd") + " 07:00:59','yyyy/mm/dd hh24:mi:ss') ), 0)E_REASON ";
            }
            sql += "FROM SYS_PARAMS WHERE 0 = 0 ";
            if (io_type != "")
                sql += "AND P_GROUP = '" + io_type + "' ";
            sql += "AND P_MODEL = 'iotype' ORDER BY P_SORT)TEMP ";

            base.DBExecSQL(sql, ref dt);

            return dt;
        }

        public DataTable sel_io_data_byGroup(string feeno)
        {
            DataTable dt = new DataTable();
            //20140125 新增規則 AND FEENO = '" + feeno + "' 
            string sql = "SELECT * FROM (SELECT TEMP.*, ";
            sql += "(SELECT SUM(AMOUNT) FROM IO_DATA WHERE TYPEID = '1' AND AMOUNT_UNIT = '1' AND FEENO = '" + feeno + "' AND CREATTIME = TEMP.CREATTIME AND DELETED IS NULL ) || 'mL', ";
            sql += "(SELECT SUM(AMOUNT) FROM IO_DATA WHERE TYPEID = '2' AND AMOUNT_UNIT = '1' AND FEENO = '" + feeno + "' AND CREATTIME = TEMP.CREATTIME AND DELETED IS NULL ) || 'mL', ";
            sql += "(SELECT SUM(AMOUNT) FROM IO_DATA WHERE TYPEID = '3' AND AMOUNT_UNIT = '1' AND FEENO = '" + feeno + "' AND CREATTIME = TEMP.CREATTIME AND DELETED IS NULL ) || 'mL', ";
            sql += "(SELECT SUM(AMOUNT) FROM IO_DATA WHERE TYPEID = '4' AND AMOUNT_UNIT = '1' AND FEENO = '" + feeno + "' AND CREATTIME = TEMP.CREATTIME AND DELETED IS NULL ) || 'mL', ";
            sql += "(SELECT SUM(AMOUNT) FROM IO_DATA WHERE TYPEID = '5' AND AMOUNT_UNIT = '1' AND FEENO = '" + feeno + "' AND CREATTIME = TEMP.CREATTIME AND DELETED IS NULL ) || 'mL', ";
            sql += "(SELECT SUM(AMOUNT) FROM IO_DATA WHERE TYPEID = '6' AND AMOUNT_UNIT = '1' AND FEENO = '" + feeno + "' AND CREATTIME = TEMP.CREATTIME AND DELETED IS NULL ) || 'mL', ";
            sql += "(SELECT SUM(AMOUNT) FROM IO_DATA WHERE TYPEID = '7' AND AMOUNT_UNIT = '1' AND FEENO = '" + feeno + "' AND CREATTIME = TEMP.CREATTIME AND DELETED IS NULL ) || 'mL', ";
            sql += "(SELECT SUM(AMOUNT) FROM IO_DATA WHERE TYPEID = '8' AND AMOUNT_UNIT = '1' AND FEENO = '" + feeno + "' AND CREATTIME = TEMP.CREATTIME AND DELETED IS NULL ) || 'mL', ";
            sql += "(SELECT SUM(AMOUNT) FROM IO_DATA WHERE TYPEID = '9' AND AMOUNT_UNIT = '1' AND FEENO = '" + feeno + "' AND CREATTIME = TEMP.CREATTIME AND DELETED IS NULL ) || 'mL', ";
            sql += "(SELECT SUM(AMOUNT) FROM IO_DATA WHERE TYPEID = '10' AND AMOUNT_UNIT = '1' AND FEENO = '" + feeno + "' AND CREATTIME = TEMP.CREATTIME AND DELETED IS NULL ) || 'mL' ";
            sql += "FROM ";
            sql += "(SELECT CREATTIME FROM IO_DATA WHERE FEENO = '" + feeno + "' AND AMOUNT_UNIT = '1' GROUP BY CREATTIME ORDER BY CREATTIME) TEMP ";
            sql += "UNION ";
            sql += "SELECT TEMP.*, ";
            sql += "(SELECT SUM(AMOUNT) FROM IO_DATA WHERE TYPEID = '1' AND AMOUNT_UNIT = '2' AND FEENO = '" + feeno + "' AND CREATTIME = TEMP.CREATTIME AND DELETED IS NULL ) || '次', ";
            sql += "(SELECT SUM(AMOUNT) FROM IO_DATA WHERE TYPEID = '2' AND AMOUNT_UNIT = '2' AND FEENO = '" + feeno + "' AND CREATTIME = TEMP.CREATTIME AND DELETED IS NULL ) || '次', ";
            sql += "(SELECT SUM(AMOUNT) FROM IO_DATA WHERE TYPEID = '3' AND AMOUNT_UNIT = '2' AND FEENO = '" + feeno + "' AND CREATTIME = TEMP.CREATTIME AND DELETED IS NULL ) || '次', ";
            sql += "(SELECT SUM(AMOUNT) FROM IO_DATA WHERE TYPEID = '4' AND AMOUNT_UNIT = '2' AND FEENO = '" + feeno + "' AND CREATTIME = TEMP.CREATTIME AND DELETED IS NULL ) || '次', ";
            sql += "(SELECT SUM(AMOUNT) FROM IO_DATA WHERE TYPEID = '5' AND AMOUNT_UNIT = '2' AND FEENO = '" + feeno + "' AND CREATTIME = TEMP.CREATTIME AND DELETED IS NULL ) || '次', ";
            sql += "(SELECT SUM(AMOUNT) FROM IO_DATA WHERE TYPEID = '6' AND AMOUNT_UNIT = '2' AND FEENO = '" + feeno + "' AND CREATTIME = TEMP.CREATTIME AND DELETED IS NULL ) || '次', ";
            sql += "(SELECT SUM(AMOUNT) FROM IO_DATA WHERE TYPEID = '7' AND AMOUNT_UNIT = '2' AND FEENO = '" + feeno + "' AND CREATTIME = TEMP.CREATTIME AND DELETED IS NULL ) || '次', ";
            sql += "(SELECT SUM(AMOUNT) FROM IO_DATA WHERE TYPEID = '8' AND AMOUNT_UNIT = '2' AND FEENO = '" + feeno + "' AND CREATTIME = TEMP.CREATTIME AND DELETED IS NULL ) || '次', ";
            sql += "(SELECT SUM(AMOUNT) FROM IO_DATA WHERE TYPEID = '9' AND AMOUNT_UNIT = '2' AND FEENO = '" + feeno + "' AND CREATTIME = TEMP.CREATTIME AND DELETED IS NULL ) || '次', ";
            sql += "(SELECT SUM(AMOUNT) FROM IO_DATA WHERE TYPEID = '10' AND AMOUNT_UNIT = '2' AND FEENO = '" + feeno + "' AND CREATTIME = TEMP.CREATTIME AND DELETED IS NULL ) || '次' ";
            sql += "FROM ";
            sql += "(SELECT CREATTIME FROM IO_DATA WHERE FEENO = '" + feeno + "' AND AMOUNT_UNIT = '2' GROUP BY CREATTIME ORDER BY CREATTIME) TEMP) ORDER BY CREATTIME DESC ";

            base.DBExecSQL(sql, ref dt);

            return dt;
        }

        /// <summary>
        /// 取得IO種類 (改為SYS_PARAMS內容通用)
        /// </summary>
        /// <param name="type">輸入/輸出</param>
        public DataTable sel_sys_params_kind(string p_model = "" , string p_group = "")
        {
            DataTable dt = new DataTable();
            List<string> where_list = new List<string>();
            string sql = "SELECT * FROM SYS_PARAMS";
            if (!string.IsNullOrEmpty(p_model))
            {
                where_list.Add(" P_MODEL='" + p_model + "' ");
            }
            if (!string.IsNullOrEmpty(p_group))
            {
                where_list.Add(" P_GROUP='" + p_group + "' ");
            }
            if (where_list.Count > 0)
            {
                sql += " WHERE " + string.Join("AND", where_list);
            }
            sql += "order by P_SORT ASC ";
            base.DBExecSQL(sql, ref dt);

            return dt;
        }

        /// <summary>
        /// 取得IO自訂項目
        /// </summary>
        /// <returns></returns>
        public DataTable sel_io_item(string userno, string typeid="")
        {
            DataTable dt = new DataTable();

            string sql = "SELECT * FROM IO_ITEM WHERE 0 = 0 ";
            if (userno != "")
                sql += " AND CREANO = '" + userno + "' OR CREANO = 'sys' ";
            if (typeid != "")
                sql += " AND TYPEID != " + typeid ;
            sql += " order by typeid,sequence,name asc";
            base.DBExecSQL(sql, ref dt);

            return dt;
        }

        /// <summary>
        /// IVF清單固定要有 其他藥物 與 其他點滴
        /// </summary>
        /// <param name="IO_dt"></param>
        /// <returns></returns>
        public DataTable insert_SPECIAL_IVF(DataTable IO_dt)
        {
            DataRow dr = IO_dt.NewRow();

            dr = IO_dt.NewRow();
            dr["ITEMID"] = "IO_ITEM_14386__20160702082147795_3";
            dr["CREANO"] = "09277";
            dr["TYPEID"] = "1";
            dr["NAME"] = "其他藥物";
            dr["CALORIES"] = "0";
            dr["SEQUENCE"] = "99";
            IO_dt.Rows.Add(dr);
            dr = IO_dt.NewRow();
            dr["ITEMID"] = "IO_ITEM_14386__20160702082147795_4";
            dr["CREANO"] = "09277";
            dr["TYPEID"] = "1";
            dr["NAME"] = "其他點滴";
            dr["CALORIES"] = "0";
            dr["SEQUENCE"] = "99";
            IO_dt.Rows.Add(dr);
            return IO_dt;
        }
        /// <summary>
        /// 取得IO自訂項目
        /// </summary>
        /// <param name="item_name">ITEM_NAME</param>
        /// <returns>回傳ITEMID</returns>
        public string sel_io_item(int type,string item_name)
        {
            DataTable dt = new DataTable();
            var tmp_itemid = "";
            string sql = "SELECT * FROM IO_ITEM WHERE typeid= " +type;
            if (item_name != "")
                sql += " and name = '"+item_name +"' and rownum =1 ";

            sql += " order by typeid,sequence,name asc";
            base.DBExecSQL(sql, ref dt);
            if (dt.Rows.Count >0 )
            {
                tmp_itemid = dt.Rows[0]["ITEMID"].ToString();
            }else
            {
                tmp_itemid = "";
            }

            return tmp_itemid;
        }
        /// <summary>
        /// 設定table的colum
        /// </summary>
        /// <param name="dt">資料表</param>
        /// <param name="clumn">欄位_陣列</param>
        public DataTable set_dt_column(DataTable dt, string[] clumn)
        {
            for (int i = 0; i < clumn.Length; i++)
            {
                dt.Columns.Add(clumn[i]);
            }
            return dt;
        }

        /// <summary>
        /// 將撈出資料重新組合
        /// </summary>
        public void set_new_list(DataTable old_dt, DataTable dt_i_item, DataTable dt_o_item, DataTable new_dt, DateTime firstday, DateTime lastday)
        {
            for (DateTime date = firstday; date <= lastday; date = date.AddDays(1))
            {
                foreach (DataRow r_i_item in dt_i_item.Rows)
                {
                    DataRow row = new_dt.NewRow();
                    double amount = 0, clorie = 0;
                    string reason = "";
                    foreach (DataRow r in old_dt.Rows)
                    {
                        if (Convert.ToDateTime(r["CREATTIME"]) >= Convert.ToDateTime(date.ToString("yyyy/MM/dd 07:00:01")) && Convert.ToDateTime(r["CREATTIME"]) <= Convert.ToDateTime(date.AddDays(1).ToString("yyyy/MM/dd 07:00:00")))
                        {
                            if (r["TYPEID"].ToString() == r_i_item["P_VALUE"].ToString())
                            {
                                if (r["AMOUNT_ALL"].ToString() != "")
                                {
                                    amount += Convert.ToDouble(r["AMOUNT_ALL"].ToString());
                                    string TempCalories = "0";
                                    if(r["CALORIES"].ToString() != "")
                                    {
                                        TempCalories = r["CALORIES"].ToString();
                                    }
                                    clorie += Convert.ToDouble(TempCalories);
                                }
                                else
                                    reason = "+Loss";
                            }
                        }
                    }
                    row["DATE"] = date;
                    row["P_VALUE"] = r_i_item["P_VALUE"].ToString();
                    row["AMOUNT"] = amount;
                    row["CLORIE"] = clorie;
                    row["REASON"] = reason;
                    row["TYPE"] = r_i_item["P_GROUP"].ToString();
                    new_dt.Rows.Add(row);
                }
                foreach (DataRow r_o_item in dt_o_item.Rows)
                {
                    DataRow row = new_dt.NewRow();
                    double amount = 0, clorie = 0;
                    string reason = "";
                    foreach (DataRow r in old_dt.Rows)
                    {
                        if (Convert.ToDateTime(r["CREATTIME"]) >= Convert.ToDateTime(date.ToString("yyyy/MM/dd 07:00:01")) && Convert.ToDateTime(r["CREATTIME"]) <= Convert.ToDateTime(date.AddDays(1).ToString("yyyy/MM/dd 07:00:00")))
                        {
                            if (r["TYPEID"].ToString() == r_o_item["P_VALUE"].ToString())
                            {
                                if (r["AMOUNT_ALL"].ToString() != "")
                                {
                                    amount += Convert.ToDouble(r["AMOUNT_ALL"].ToString());
                                    string TempCalories = "0";
                                    if(r["CALORIES"].ToString() != "")
                                    {
                                        TempCalories = r["CALORIES"].ToString();
                                    }
                                    clorie += Convert.ToDouble(TempCalories);
                                    
                                }
                                else
                                    reason = "+Loss";
                            }
                        }
                    }
                    row["DATE"] = date;
                    row["P_VALUE"] = r_o_item["P_VALUE"].ToString();
                    row["AMOUNT"] = amount;
                    row["CLORIE"] = clorie;
                    row["REASON"] = reason;
                    row["TYPE"] = r_o_item["P_GROUP"].ToString();
                    new_dt.Rows.Add(row);
                }
            }
        }
        /*---------------------新增查詢列表------增加時間判斷-----------------------*/
        //public DataTable sel_io_data_byClassbyHour(string feeno, string date, string unit, string hour) //2016/06/23 Vanda mark，查詢條件不用篩選單位
        public DataTable sel_io_data_byClassbyHour(string feeno, string date, string hour)
        {
            DataTable dt = new DataTable();
            #region --舊方法--捨去
            //string[] hour_list = { "D7", "D8", "D9", "D10", "D11", "D12", "D13", "D14"
            //                          ,"N15","N16","N17","N18","N19","N20", "N21", "N22"
            //                          , "E23", "E0", "E2", "E3" , "E4" , "E5" , "E6" };
            //string[] class_list = { "D", "N", "E"};
            //string sql = "SELECT TEMP.*,(";
            //for(int i = 0; i < hour_list.Length; i++) 
            //{
            //    sql += hour_list[i]+"+";
            //}
            //sql = sql.Substring(0, sql.Length - 1);
            //    sql += ")TOTAL,(D_REASON+N_REASON+E_REASON)TOTAL_REASON FROM (SELECT P_VALUE,P_NAME ";
            //for(int i = 0; i < class_list.Length; i++)
            //{
            //    if(class_list[i].ToString() == "D"){
            //        DateTime TempStartHour =Convert.ToDateTime(date+" 07:01:00");
            //        for(DateTime x = TempStartHour; x < TempStartHour.AddHours(8); x =x.AddHours(int.Parse(hour)))
            //        {
            //            string TempStartX = x.ToString("yyyy/MM/dd HH:mm:ss");
            //            string TempEndX = x.AddHours(int.Parse(hour)).ToString("yyyy/MM/dd HH");
            //            sql += ",NVL((SELECT SUM(AMOUNT) FROM IO_DATA WHERE 0 = 0 AND DELETED IS NULL AND TYPEID = P_VALUE ";
            //            if(unit != "")
            //                sql += "AND AMOUNT_UNIT = '" + unit + "' ";
            //            if(feeno != "")
            //                sql += "AND FEENO = '" + feeno + "' ";
            //            sql += "AND CREATTIME BETWEEN to_date('"+TempStartX+"','yyyy/mm/dd hh24:mi:ss') AND to_date('"+TempEndX+":00:59','yyyy/mm/dd hh24:mi:ss') ), 0)D"+x.Hour+" ";
            //        }
            //    }
            //    else if(class_list[i] == "N"){
            //       DateTime TempStartHour =Convert.ToDateTime(date+" 15:01:00");
            //        for(DateTime x = TempStartHour; x < TempStartHour.AddHours(8); x =x.AddHours(int.Parse(hour)))
            //        {
            //            string TempStartX = x.ToString("yyyy/MM/dd HH:mm:ss");
            //            string TempEndX = x.AddHours(int.Parse(hour)).ToString("yyyy/MM/dd HH");
            //            sql += ",NVL((SELECT SUM(AMOUNT) FROM IO_DATA WHERE 0 = 0 AND DELETED IS NULL AND TYPEID = P_VALUE ";
            //            if(unit != "")
            //                sql += "AND AMOUNT_UNIT = '" + unit + "' ";
            //            if(feeno != "")
            //                sql += "AND FEENO = '" + feeno + "' ";
            //            sql += "AND CREATTIME BETWEEN to_date('" + TempStartX + "','yyyy/mm/dd hh24:mi:ss') AND to_date('" + TempEndX + ":00:59','yyyy/mm/dd hh24:mi:ss') ), 0)N" + x.Hour + " ";
            //        }
            //    }
            //    else if(class_list[i] == "E") {
            //         DateTime TempStartHour =Convert.ToDateTime(date+" 23:01:00");
            //        for(DateTime x = TempStartHour; x < TempStartHour.AddHours(8); x =x.AddHours(int.Parse(hour)))
            //        {
            //            string TempStartX = x.ToString("yyyy/MM/dd HH:mm:ss");
            //            string TempEndX = x.AddHours(int.Parse(hour)).ToString("yyyy/MM/dd HH");
            //            sql += ",NVL((SELECT SUM(AMOUNT) FROM IO_DATA WHERE 0 = 0 AND DELETED IS NULL AND TYPEID = P_VALUE ";
            //            if(unit != "")
            //                sql += "AND AMOUNT_UNIT = '" + unit + "' ";
            //            if(feeno != "")
            //                sql += "AND FEENO = '" + feeno + "' ";
            //            sql += "AND CREATTIME BETWEEN to_date('" + TempStartX + "','yyyy/mm/dd hh24:mi:ss') AND to_date('" + TempEndX + ":00:59','yyyy/mm/dd hh24:mi:ss') ), 0)E" + x.Hour + " ";
            //        }
            //    }
            //    sql += ",NVL((SELECT COUNT(REASON) FROM IO_DATA WHERE 0 = 0 AND DELETED IS NULL AND TYPEID = P_VALUE ";
            //    if(unit != "")
            //        sql += "AND AMOUNT_UNIT = '" + unit + "' ";
            //    if(feeno != "")
            //        sql += "AND FEENO = '" + feeno + "' ";
            //    if(class_list[i] == "D")
            //        sql += "AND CREATTIME BETWEEN to_date('" + date + " 07:01:00','yyyy/mm/dd hh24:mi:ss') AND to_date('" + date + " 15:00:59','yyyy/mm/dd hh24:mi:ss') ), 0)D_REASON ";
            //    else if(class_list[i] == "N")
            //        sql += "AND CREATTIME BETWEEN to_date('" + date + " 15:01:00','yyyy/mm/dd hh24:mi:ss') AND to_date('" + date + " 23:00:59','yyyy/mm/dd hh24:mi:ss') ), 0)N_REASON ";
            //    else if(class_list[i] == "E")
            //        sql += "AND CREATTIME BETWEEN to_date('" + date + " 23:01:00','yyyy/mm/dd hh24:mi:ss') AND to_date('" + Convert.ToDateTime(date).AddDays(+1).ToString("yyyy/MM/dd") + " 07:00:59','yyyy/mm/dd hh24:mi:ss') ), 0)E_REASON ";
            //}
            //sql += "FROM SYS_PARAMS WHERE 0 = 0 ";
            //if(io_type != "")
            //    sql += "AND P_GROUP = '" + io_type + "' ";
            //sql += "AND P_MODEL = 'iotype' ORDER BY P_SORT)TEMP ";
            ////sql += "  AS p";
            #endregion
            string dateSubOneDay = (Convert.ToDateTime(date + " 23:01:00").AddDays(-1)).ToString("yyyy/MM/dd HH:mm:ss");
            string sql = "SELECT * FROM (SELECT * FROM IO_DATA  WHERE  0 = 0 AND DELETED IS NULL ";
            //-----2016/06/23 Vanda mark，查詢條件不用篩選單位
            //if(unit != "")
            //{
            //    sql += " AND AMOUNT_UNIT = '" + unit + "' ";
            //}
            //-----
            if(feeno != "")
            {
                sql += " AND FEENO='" + feeno + "' ";
            }
           // sql += "AND CREATTIME BETWEEN TO_DATE('" + dateSubOneDay + "','yyyy/mm/dd hh24:mi:ss') AND TO_DATE('" + date + " 22:59:59','yyyy/mm/dd hh24:mi:ss'))A1 ";
            sql += "AND CREATTIME BETWEEN TO_DATE('" + dateSubOneDay + "','yyyy/mm/dd hh24:mi:ss') AND TO_DATE('" + date + " 23:00:00','yyyy/mm/dd hh24:mi:ss'))A1 ";
            sql += "INNER JOIN ";
            sql += "(SELECT * FROM SYS_PARAMS  WHERE 0 = 0 ";
            sql += "AND P_GROUP IN ('intaketype','outputtype') ";
            sql += "AND P_MODEL = 'iotype' ORDER BY P_GROUP,P_SORT) A2 on TYPEID = P_VALUE ";
            dt = base.DBExecSQL(sql);
            return dt;
        }


        /*---------------------範圍查詢列表------by日期區間查詢-----------------------*/
        //public DataTable sel_io_data_byRange(string feeno, string Startdate, string Enddate, string unit) //2016/06/23 Vanda mark，查詢條件不用篩選單位
        public DataTable sel_io_data_byRange(string feeno, string Startdate, string Enddate)
        {
            DataTable dt = new DataTable();
           // string dateSubOneDay = (Convert.ToDateTime(Startdate + " 23:00:00").AddDays(-1)).ToString("yyyy/MM/dd HH:mm:ss");
            string dateSubOneDay = (Convert.ToDateTime(Startdate + " 07:01:00")).ToString("yyyy/MM/dd HH:mm:ss");//.AddDays(-1)

            string sql = "SELECT * FROM (SELECT * FROM IO_DATA  WHERE  0 = 0 AND DELETED IS NULL ";
            //-----2016/06/23 Vanda mark，查詢條件不用篩選單位
            //if(unit != "")
            //{
            //    sql += " AND AMOUNT_UNIT = '" + unit + "' ";
            //}
            //-----
            if(feeno != "")
            {
                sql += " AND FEENO='" + feeno + "' ";
            }
            //sql += "AND CREATTIME BETWEEN TO_DATE('" + dateSubOneDay + "','yyyy/mm/dd hh24:mi:ss') AND TO_DATE('" + Enddate + " 22:59:59','yyyy/mm/dd hh24:mi:ss'))A1 ";
            sql += "AND CREATTIME BETWEEN TO_DATE('" + dateSubOneDay + "','yyyy/mm/dd hh24:mi:ss') AND TO_DATE('" + Convert.ToDateTime(Enddate).AddDays(1).ToString("yyyy/MM/dd") + " 07:00:00','yyyy/mm/dd hh24:mi:ss'))A1 ";

            sql += "INNER JOIN ";
            sql += "(SELECT * FROM SYS_PARAMS  WHERE 0 = 0 ";
            sql += "AND P_GROUP IN ('intaketype','outputtype') ";
            sql += "AND P_MODEL = 'iotype' ORDER BY P_GROUP,P_SORT) A2 on TYPEID = P_VALUE ";
            dt = base.DBExecSQL(sql);
            return dt;
        }

        #region WebService for 點滴
        /// <summary>
        /// 點滴清單改從WebService(GetIVFList)取值
        /// </summary>
        public void DELETE_IO_IVF()
        {
            string sql = " DELETE FROM IO_ITEM WHERE  typeid = '1' ";
            base.DBExecSQL(sql);
        }

        /// <summary>IO ITEM明細</summary>
        public class IOItem
        {
            /// <summary>IO明細PK</summary>
            public string ITEMID { get; set; }
            /// <summary>建立者( sys 或 員編)</summary>
            public string CREANO { get; set; }
            /// <summary>IO編號(點滴=1)</summary>
            public string TYPEID { get; set; }
            /// <summary>名稱</summary>
            public string NAME { get; set; }
            /// <summary>卡路里</summary>
            public string CALORIES { get; set; }
            /// <summary>排序</summary>
            public string SORT { get; set; }

        }

     

        #endregion
        public class IOManagerUnit
        {
            public string IO_ROW { set; get; }//PK
            public string IO_ID { set; get; }//ID(可重複)
            public DateTime CREATTIME { set; get; }//紀錄時間

            public string CREANO { set; get; }//新增者
            public string TYPEID { set; get; }//種類序號
            public string ITEMID { set; get; }//項目序號

            public string AMOUNT { set; get; }//數量
            public string AMOUNT_UNIT { set; get; }//單位(1:mL  2:次)
            public string POSITION { set; get; }

            public string REASON { set; get; }
            public string EXPLANATION_ITEM { set; get; }//說明細項
            public string REMARK { set; get; }//備註
            public string CREANAME { set; get; }//新增者姓名

            public string P_ID { set; get; }//種類唯一值
            public string P_VALUE { set; get; }//輸入輸出種類的值
            public string P_SORT { set; get; }//輸入輸出排序值


            public IOManagerUnit(string i_IO_ROW, string i_IO_ID, DateTime i_CREATTIME
                , string i_CREANO, string i_TYPEID, string i_ITEMID
                 , string i_AMOUNT, string i_AMOUNT_UNIT, string i_POSITION
                , string i_REASON, string i_EXPLANATION_ITEM, string i_REMARK, string i_CREANAME
                , string i_P_ID, string i_P_VALUE, string i_P_SORT
                )
            {
                this.IO_ROW = i_IO_ROW;
                this.IO_ID = i_IO_ID;
                this.CREATTIME = i_CREATTIME;
                this.CREANO = i_CREANO;
                this.TYPEID = i_TYPEID;
                this.ITEMID = i_ITEMID;

                this.AMOUNT = i_AMOUNT;
                this.AMOUNT_UNIT = i_AMOUNT_UNIT;
                this.POSITION = i_POSITION;

                this.REASON = i_REASON;
                this.EXPLANATION_ITEM = i_EXPLANATION_ITEM;
                this.REMARK = i_REMARK;
                this.CREANAME = i_CREANAME;

                this.P_ID = i_P_ID;
                this.P_VALUE = i_P_VALUE;
                this.P_SORT = i_P_SORT;
            }
        }


        /// <summary>要篩選的資料表</summary>  
        /// <param name="RowFilterStr">篩選條件(ex: actid= 'id123' AND datatype ='1')</param>
        /// <param name="SortStr">要排序的欄位名稱</param>  
        /// <returns>篩選過後的資料表</returns>
        public static DataTable FiltData(DataTable DT, string RowFilterStr, string SortStr = "")
        {
            DataTable RTDT = null;
            try
            {
                if(DT != null && DT.Rows.Count > 0)
                {
                    DataView DV = new DataView();
                    DV = DT.DefaultView;
                    DV.RowFilter = RowFilterStr;
                    if(!string.IsNullOrEmpty(SortStr))
                    {
                        DV.Sort = SortStr;
                    }
                    if(DV.Table != null && DV.Table.Rows.Count > 0)
                    {
                        RTDT = DV.ToTable();
                    }
                    DV.RowFilter = "";
                }
                return RTDT;
            }
            catch
            {
                return null;
            }
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
