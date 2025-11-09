using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NIS.Models;
using System.Data;

namespace NIS.Models
{

    public class CommonMedicine : DBAction
    {

        public class Confirm
        {
            public string Ordseq { get; set; }
            public string Remark { get; set; }
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

        //=============上面先用不到===============
        /// <summary>
        /// 取得一般給藥table
        /// </summary>
        /// <returns>回傳table</returns>
        public DataTable get_drugtable(string use_condition)
        {
            DataTable dt = new DataTable();
            if (use_condition == "new")
            {
                string[] UpcrcpTime = { "sheetno","dose","ordstatus","doseunit","feeno","givetime",
                                       "givedate","category","orderstartdate","orderenddate","drugname",
                                       "genericdrugs","route","ratel","rateh","ratememo","status",
                                       "dcflag","giveserial","costtime","feq","doublecheck","note","med_code","ud_status", "seq"};

                string[] datatype_upcrcp = {"String", "String", "String", "String", "String",
                                        "String", "String", "String", "String", "String","String",
                                        "String", "String", "String", "String", "String","String",
                                        "String", "String", "String", "String", "String", "String","String","String","String"};


                dt = set_dt_column(dt, UpcrcpTime);
                DataRow dr = dt.NewRow();
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    dr[i] = datatype_upcrcp[i];
                }
                dt.Rows.Add(dr);
            }
            if (use_condition == "save")
            {
                string[] UpcrcpTime = {  "reasontype", "reason", "insdt","badreaction","ordstatus",
                                              "checker","insname","moddt","modname","moddose","status"};
                string[] datatype_upcrcp = { "String", "String", "String", "String", "String", "String",
                                                   "String", "String", "String", "String", "String"};

                dt = set_dt_column(dt, UpcrcpTime);
                DataRow dr = dt.NewRow();
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    dr[i] = datatype_upcrcp[i];
                }
                dt.Rows.Add(dr);
            }
            //      UpcrcpTime = new string[] { "" };
            return dt;
        }
        /// <summary>
        /// 取得DRUG_SHOW藥物列
        /// </summary>
        /// <param name="page">依據傳入頁面</param>
        /// <param name="FEENO">依據批價序號號</param>
        /// <param name="SHEETNO">依據醫囑序號</param>
        /// <param name="UDODRGCODE">依據藥包條碼</param>
        /// <param name="CATEGORY">依據用藥類別</param>
        /// <param name="status">狀態</param>
        /// <param name="firstday">開始日期</param>
        /// <param name="lastday">結束日期</param>
        /// <returns>回傳table</returns>
        public DataTable get_drug_row(string page, string FEENO, string SHEETNO, string UDODRGCODE, string CATEGORY, string status, string firstday, string lastday, string flag1)
        {
            string DataSheet = "";
            if (page != "")
            {
                DataSheet = "DRUG_SHOW";
                //if (page == "row") DataSheet = "DRUG_SHOW";
                //if ((page == "Cancel") || (page == "Search_Print")) DataSheet = "UNCRCPTIME";
            }
            string sql = "";
            //"SELECT * FROM " + DataSheet + " WHERE FEENO = '" + FEENO + "' AND SHEETNO ='"+ SHEETNO + "' ";

            sql = "select distinct sheetno,dose,ordstatus,doseunit,feeno,givedate, ";
            sql += "category,orderstartdate,Replace (orderenddate,'9999/12/31 23:59:59','') as orderenddate,drugname, ";
            sql += "genericdrugs,route,ratel,rateh,ratememo, ";
            sql += "dcflag,costtime,feq,doublecheck,note,Replace (status,'需給藥','') status ,med_code,ud_status";
            sql += "from " + DataSheet + " WHERE FEENO = '" + FEENO + "' AND dcflag='N' ";
            if (CATEGORY == "S")//R常規 P,B暫時放常規
            {
                sql += "AND CATEGORY = '" + CATEGORY + "' ";
            }
            else
            { sql += "AND CATEGORY <> 'S' "; }
            if (page == "row") sql += "AND STATUS = '需給藥' ";
            if (page == "Cancel_row") sql += "AND STATUS = '需給藥' ";

            if (firstday == "" && lastday == "")
            {
                switch (flag1)
                {
                    case "1":
                        //未到給藥時間
                        sql += "AND  (givedate|| '_'|| givetime)  < to_char(sysdate+1/24,'yyyy/MM/dd_hh24:mm:ss') ";
                        sql += "AND  (givedate|| '_'|| givetime)  > to_char(sysdate,'yyyy/MM/dd_hh24:mm:ss') ";
                        break;
                    case "2":
                        //已過給藥時間
                        sql += "AND  (givedate|| '_'|| givetime)  < to_char(sysdate,'yyyy/MM/dd_hh24:mm:ss') ";
                        sql += "AND  INSDT IS NULL ";
                        break;
                    case "3":
                        //取消給藥用
                        sql += "AND  (givedate|| '_'|| givetime)  < to_char(sysdate+1/24,'yyyy/MM/dd_hh24:mm:ss') ";
                        break;
                    case "quiry":
                        //依時間查詢給藥用
                        //sql += "AND (givedate|| '_'|| givetime)  BETWEEN '" + start + "'  AND '" + end + "' ";
                        break;
                    default:
                        break;
                }

            }
            else
            {
                string start = Convert.ToDateTime(firstday).ToString("yyyy/MM/dd_HH:mm:ss");
                string end = Convert.ToDateTime(lastday).ToString("yyyy/MM/dd_HH:mm:ss");
                sql += "AND (givedate|| '_'|| givetime)  BETWEEN '" + start + "'  AND '" + end + "' ";

            }

            return base.gettable(sql);

        }

        public DataTable get_drug_show(string page, string FEENO, string SHEETNO, string UDODRGCODE, string CATEGORY, string status, string firstday, string lastday, string get_flag)
        {
            string DataSheet = page;
            string sql = "";
            sql = "select distinct sheetno,dose,ordstatus,doseunit,feeno,SUBSTR(ORDERSTARTDATE,1,10) givedate, ";
            sql += "category,orderstartdate,Replace (orderenddate,'9999/12/31 23:59:59','') as orderenddate,drugname, ";
            sql += "genericdrugs,route,ratel,rateh,ratememo, ";
            sql += "dcflag,costtime,feq,doublecheck,note,Replace (status,'需給藥','') status ,med_code,'' ud_status ";
            //依參數取得用藥 S:STAT, P:PRN, R:REG, B:退藥
            if (get_flag == "A1")
            {   //取消給藥
                sql += "from " + DataSheet + " WHERE FEENO = '" + FEENO + "' AND STATUS = '已給藥' AND ORDSTATUS = '0' ";
            }
            else
            {   //執行給藥
                sql += "from " + DataSheet + " WHERE FEENO = '" + FEENO + "' AND STATUS = '需給藥' AND ORDSTATUS = '1' ";
            }
            switch (CATEGORY)
            {
                case "S":  //STAT
                    sql += "AND CATEGORY = '" + CATEGORY + "' AND FEQ <> 'ASORDER' ";
                    break;
                case "R":  //REG
                    sql += "AND ((CATEGORY = '" + CATEGORY + "' AND FEQ <> 'ASORDER') OR (CATEGORY = 'P' AND FEQ NOT LIKE '%PRN%')) AND dcflag='N' ";
                    break;
                case "P": //PRN
                    sql += "AND ((CATEGORY = 'P' AND FEQ LIKE '%PRN%') OR (CATEGORY = 'R' AND FEQ='ASORDER')) AND INSDT IS NULL AND dcflag='N' ";
                    break;
                default:
                    break;
            }

            //當category=="P" 時，不用時間區間
            if (CATEGORY == "P")
            {
                return base.gettable(sql);
            }

            //依執行項目抓取時間
            if (firstday == "" && lastday == "")
            {
                switch (get_flag)
                {
                    case "1":
                        //未到給藥時間 (VIEW：執行給藥)
                        sql += "AND  (givedate|| '_'|| givetime)  < to_char(sysdate+1/24,'yyyy/MM/dd_hh24:mm:ss') ";
                        sql += "AND  (givedate|| '_'|| givetime)  > to_char(sysdate-8/24,'yyyy/MM/dd_hh24:mm:ss') "; 
                        break;
                    case "2":
                        //已過給藥時間 (VIEW：未給藥)
                        sql += "AND  (givedate|| '_'|| givetime)  < to_char(sysdate,'yyyy/MM/dd_hh24:mm:ss') ";
                        sql += "AND  INSDT IS NULL ";
                        break;
                    case "3":
                        //取消給藥用 (VIEW：取消用藥)
                        sql += "AND  (givedate|| '_'|| givetime)  < to_char(sysdate+1/24,'yyyy/MM/dd_hh24:mm:ss') ";
                        break;
                    case "quiry":
                        //依時間查詢給藥用
                        //sql += "AND (givedate|| '_'|| givetime)  BETWEEN '" + start + "'  AND '" + end + "' ";
                        break;
                    default:
                        break;
                }
            }
            else
            {
                string start = Convert.ToDateTime(firstday).ToString("yyyy/MM/dd_HH:mm:ss");
                string end = Convert.ToDateTime(lastday).ToString("yyyy/MM/dd_HH:mm:ss");
                sql += "AND (givedate|| '_'|| givetime)  BETWEEN '" + start + "'  AND '" + end + "' ";
            }
            return base.gettable(sql + " order by ROUTE");
        }

        public DataTable get_drug_time(string DataSheet, string SHEETNO, string givedate, string firstday, string lastday, string get_flag)
        {
            DataTable dt = new DataTable();
            string sql = "";
            sql = "select count(0) from " + DataSheet + " where SHEETNO='" + SHEETNO + "' ";
            dt = base.gettable(sql);
            if (dt.Rows[0][0].ToString() == "0")
            {
                return null;
            }
            else
            {
                sql = "select * from " + DataSheet + " where SHEETNO='" + SHEETNO + "' AND ORDSTATUS <>'0' ";
                if (givedate != null && givedate != "")
                {
                    switch (get_flag)
                    {
                        case "1":
                            //未到給藥時間
                            sql += "AND  (givedate|| '_'|| givetime)  < to_char(sysdate+1/24,'yyyy/MM/dd_hh24:mm:ss') ";
                            sql += "AND  (givedate|| '_'|| givetime)  > to_char(sysdate-8/24,'yyyy/MM/dd_hh24:mm:ss') ";
                            break;
                        case "2":
                            //已過給藥時間
                            sql += "AND  (givedate|| '_'|| givetime)  < to_char(sysdate,'yyyy/MM/dd_hh24:mm:ss') ";
                            sql += "AND  INSDT IS NULL ";
                            break;
                        case "3":
                            //取消給藥用
                            sql += "AND  (givedate|| '_'|| givetime)  < to_char(sysdate+1/24,'yyyy/MM/dd_hh24:mm:ss') ";
                            break;
                        case "quiry":
                            //依時間查詢給藥用
                            string start = Convert.ToDateTime(firstday).ToString("yyyy/MM/dd_HH:mm:ss");
                            string end = Convert.ToDateTime(lastday).ToString("yyyy/MM/dd_HH:mm:ss");
                            sql += "AND (givedate|| '_'|| givetime)  BETWEEN '" + start + "'  AND '" + end + "' ";
                            break;
                        default:
                            break;
                    }
                }
                dt = base.gettable(sql);
                dt.Columns.Add("use_t");//使用時間
                dt.Columns.Add("use_s");//使用順序
                dt.Columns.Add("use_d");
                foreach (DataRow dr in dt.Rows)
                {
                    if (dr["GIVESERIAL"].ToString() != "")
                    {
                        if (dr["GIVETIME"].ToString().Trim() == "")
                        { dt.Rows[0]["use_t"] += DateTime.Now.ToString("HH:mm:ss") + ","; }
                        else
                        { dt.Rows[0]["use_t"] += dr["GIVETIME"].ToString() + ","; }

                    }
                    dt.Rows[0]["use_s"] += dr["GIVESERIAL"].ToString() + ",";
                    dt.Rows[0]["use_d"] += dr["GIVEDATE"].ToString() + " " + ",";
                }
                return dt;
            }

        }

        //查詢給藥記錄
        public DataTable get_QueryExecLog(string page, string feeno, string firstday, string lastday, string get_flag)
        {
            string DataSheet = page;
            string sql = "";
            sql = "SELECT DISTINCT SHEETNO,MED_CODE,DRUGNAME,GENERICDRUGS,DOSE,DOSEUNIT,ROUTE,FEQ,ORDERSTARTDATE,NOTE, '' UD_STATUS, ";
            sql += "REPLACE (ORDERENDDATE,'9999/12/31 23:59:59','') AS ORDERENDDATE ";
            sql += "FROM " + DataSheet + " WHERE FEENO = '" + feeno + "' ";
            switch (get_flag)
            {
                case "all":
                    //全部用藥
                    break;
                case "ok":
                    //已給藥
                    sql += "AND ORDSTATUS = '0' AND INSDT IS NOT NULL AND (REASONTYPE IN ('2','3') OR REASONTYPE IS NULL) ";
                    break;
                case "not":
                    //未給藥
                    sql += "AND ORDSTATUS = '1' ";
                    break;
                case "cancel":
                    //取消給藥
                    sql += "AND REASONTYPE IN ('1','4','5') AND ORDSTATUS = '0' AND INSDT IS NOT NULL ";
                    break;
                default:
                    break;
            }
            

            //依執行項目抓取時間
            if (firstday != "" && lastday != "")
            {
                string start = Convert.ToDateTime(firstday).ToString("yyyy/MM/dd_HH:mm:ss");
                string end = Convert.ToDateTime(lastday).ToString("yyyy/MM/dd_HH:mm:ss");
                sql += "AND (givedate|| '_'|| givetime)  BETWEEN '" + start + "' AND '" + end + "' ";
            }
            sql += " ORDER BY DRUGNAME";
            return base.gettable(sql);
        }

        public DataTable get_QueryExecLogTime(string page, string SHEETNO, string firstday, string lastday, string get_flag)
        {
            DataTable dt = new DataTable();
            string DataSheet = page;

            string sql = "";
            sql = "select * from DRUG_SHOW where SHEETNO='" + SHEETNO + "'";
            if (get_flag == "quiryInterval")
            {
                //區間時間
                string start = Convert.ToDateTime(firstday).ToString("yyyy/MM/dd_HH:mm:ss");
                string end = Convert.ToDateTime(lastday).ToString("yyyy/MM/dd_HH:mm:ss");
                sql += "AND (givedate|| '_'|| givetime)  BETWEEN '" + start + "'  AND '" + end + "' ";
            }

            switch (lastday)
            {
                  case "all":
                    //全部用藥
                    break;
                case "ok":
                    //已給藥
                    sql += "AND ORDSTATUS = '0' AND INSDT IS NOT NULL AND (REASONTYPE IN ('2','3') OR REASONTYPE IS NULL) ";
                    break;
                case "not":
                    //未給藥
                    sql += "AND ORDSTATUS = '1' ";
                    break;
                case "cancel":
                    //取消給藥
                    sql += "AND REASONTYPE IN ('1','4','5') AND ORDSTATUS = '0' AND INSDT IS NOT NULL ";
                    break;
                default:
                    break;
            }

            dt = base.gettable(sql);
            dt.Columns.Add("use_t");//使用時間
            dt.Columns.Add("use_s");//使用順序
            dt.Columns.Add("use_d");
            dt.Columns.Add("use_insname"); //執行給藥的人
            dt.Columns.Add("use_insdt");   //執行給藥的時間
            dt.Columns.Add("use_reason");
            foreach (DataRow dr in dt.Rows)
            {
                if (dr["GIVESERIAL"].ToString() != "")
                {
                    dt.Rows[0]["use_t"] += dr["GIVETIME"].ToString() + " " + ",";
                    dt.Rows[0]["use_insname"] += dr["INSNAME"].ToString() + " ,";
                    dt.Rows[0]["use_reason"] += dr["REASON"].ToString() + " ,";
                    if (dr["INSDT"].ToString() != null && dr["INSDT"].ToString() != "")
                    { dt.Rows[0]["use_insdt"] += Convert.ToDateTime(dr["INSDT"]).ToString("HH:mm") + " ,"; }
                    else
                    { dt.Rows[0]["use_insdt"] += dr["INSDT"] + ","; }
                }
                dt.Rows[0]["use_s"] += dr["GIVESERIAL"].ToString() + ",";
                dt.Rows[0]["use_d"] += dr["GIVEDATE"].ToString() + ",";
            }
            return dt;
        }
       
        /// <summary>
        /// 取得查詢DRUG_SHOW藥物列
        /// </summary>
        /// <param name="page">依據傳入頁面</param>
        /// <param name="FEENO">依據批價序號號</param>
        /// <param name="SHEETNO">依據藥碼</param>
        /// <param name="drugname">依據藥名</param>
        /// <param name="route">依據途徑</param>
        /// <param name="CATEGORY">依據用藥類別</param>
        /// <param name="status">狀態</param>
        /// <param name="firstday">開始日期</param>
        /// <param name="lastday">結束日期</param>
        /// <returns>回傳table</returns>
        public DataTable get_drug_quiry(string page, string FEENO, string SHEETNO, string drugname, string route, string CATEGORY, string status, string firstday, string lastday)
        {
            string DataSheet = "DRUG_SHOW";
            string sql = "";

            sql = "select distinct sheetno,dose,ordstatus,doseunit,feeno,givedate, ";
            sql += "category,orderstartdate,Replace (orderenddate,'9999/12/31 23:59:59','') as orderenddate,drugname, ";
            sql += "genericdrugs,route,ratel,rateh,ratememo, ";
            sql += "dcflag,costtime,feq,doublecheck,note,Replace (status,'已給藥','需給藥') status,med_code,us_status ";
            sql += "from " + DataSheet + " WHERE FEENO = '" + FEENO + "' AND dcflag='N' ";
            //20130826
            if (page == "search")
            {
                sql = "select distinct sheetno,dose,ordstatus,doseunit,feeno,givedate,insdt, ";
                sql += "category,orderstartdate,Replace (orderenddate,'9999/12/31 23:59:59','') as orderenddate,drugname, ";
                sql += "genericdrugs,route,ratel,rateh,ratememo, ";
                sql += "dcflag,costtime,feq,doublecheck,note,Replace (status,'已給藥','需給藥') status ,med_code,ud_status";
                sql += "from " + DataSheet + " WHERE FEENO = '" + FEENO + "' AND dcflag='N' ";
            }
            //20130826 mod 
            if (CATEGORY == "S")//R常規 P,B暫時放常規
            {
                sql += "AND CATEGORY = '" + CATEGORY + "' ";
            }
            else
            { sql += "AND CATEGORY <> 'S' "; }
            if (SHEETNO != "")
            {
                sql += "AND SHEETNO = '" + SHEETNO + "' ";
            }
            if (route != "")
            {
                sql += "AND route = '" + route + "' ";
            }
            if (drugname != "")
            {
                sql += "AND drugname like '%" + drugname + "%' ";
            }
            if (status != "")
            {
                if (status == "使用中")
                {
                    sql += "AND status <> '取消' AND orderenddate > to_char(sysdate,'yyyy/MM/dd hh24:mm:ss')  ";
                }
                if (status == "取消")
                {
                    sql += "AND status = '取消' AND orderenddate > to_char(sysdate,'yyyy/MM/dd hh24:mm:ss')  ";
                }
                if (status == "結束")
                {
                    sql += "AND orderenddate < to_char(sysdate,'yyyy/MM/dd hh24:mm:ss')  ";
                }
            }
            else
            {
                //sql += "AND orderenddate > to_char(sysdate,'yyyy/MM/dd hh24:mm:ss')  ";
                //sql += "UNION ";
                //sql += "select distinct sheetno,dose,ordstatus,doseunit,feeno,givedate, ";
                //sql += "category,orderstartdate,Replace (orderenddate,'9999/12/31 23:59:59','') as orderenddate,drugname, ";
                //sql += "genericdrugs,route,ratel,rateh,ratememo, ";
                //sql += "dcflag,costtime,feq,doublecheck,note,'結束' as status ";
                //sql += "from " + DataSheet + " WHERE FEENO = '" + FEENO + "' AND dcflag='N' ";
                //sql += "AND orderenddate < to_char(sysdate,'yyyy/MM/dd hh24:mm:ss')  ";
            }


            //if (page == "row") sql += "AND STATUS = '需給藥' ";
            //if (page == "Cancel_row") sql += "AND STATUS = '需給藥' ";
            if (firstday == "" && lastday == "")
            {
                //   sql += "AND  (givedate|| '_'|| givetime)  < to_char(sysdate+1/24,'yyyy/MM/dd_hh24:mm:ss') ";
            }
            else
            {
                string start = Convert.ToDateTime(firstday).ToString("yyyy/MM/dd_HH:mm:ss");
                string end = Convert.ToDateTime(lastday).ToString("yyyy/MM/dd_HH:mm:ss");
                sql += "AND (givedate|| '_'|| givetime)  BETWEEN '" + start + "'  AND '" + end + "' ";
            }
            return base.gettable(sql);
        }

        /// <summary> 檢查PK
        /// </summary>
        public Boolean ck_pk(string FEENO, string SHEETNO, string GIVESERIAL, string GIVEDATE, string dc, string page)
        {   //檢查是否已有資料，如有資料則離開，沒有資料則INSERT
            bool t = false;
            if (dc == "Y")
            {
                ck_dc(FEENO, SHEETNO, GIVESERIAL, GIVEDATE);
            }
            else
            {
                string sql = "";
                sql = "SELECT count(0) FROM DRUG_SHOW WHERE ";
                sql += "SHEETNO = '" + SHEETNO + "' AND FEENO = '" + FEENO + "' AND GIVESERIAL = '" + GIVESERIAL + "' ";

                if (GIVEDATE != null && GIVEDATE != "")
                {
                    sql += "AND GIVEDATE = '" + GIVEDATE + "' ";
                }

                DataTable dt = base.gettable(sql);
                if (dt.Rows[0][0].ToString() == "0")
                { //該筆資料沒有
                    t = true;
                }
            }
            return t;
        }

        /// <summary> 檢查DC
        /// </summary>
        public Boolean ck_dc(string FEENO, string SHEETNO, string GIVESERIAL, string GIVEDATE)
        {
            bool t = false;
            string sql = "";
            string DataSheet = "DRUG_SHOW";

            sql = "SELECT DCFLAG from " + DataSheet + " WHERE ";
            sql += "SHEETNO = '" + SHEETNO + "' AND FEENO = '" + FEENO + "' ";
            sql += "AND GIVEDATE = '" + GIVEDATE + "' ";
            sql += "AND GIVESERIAL = '" + GIVESERIAL + "' ";


            DataTable dt = base.gettable(sql);
            //if(dt.Rows[0][0].ToString() == "N")
            if ( dt.Rows.Count > 0)
            { //尚未DC 需更新資料
                DataTable dt_c = new DataTable();
                dt_c.Columns.Add("DCFLAG");
                dt_c.Columns.Add("where");
                DataRow dr_c = dt_c.NewRow();
                //塞入datatype

                dr_c = dt_c.NewRow();

                string where = "SHEETNO = '" + SHEETNO + "' AND FEENO = '" + FEENO + "' ";
                where += "AND GIVEDATE = '" + GIVEDATE + "' AND GIVESERIAL = '" + GIVESERIAL + "' ";
                where += "AND ORDSTATUS = '1' AND INSDT IS NULL AND  ";
                dr_c["DCFLAG"] = "Y";
                dr_c["where"] = where;
                dt_c.Rows.Add(dr_c);
                //確認是否有存資料 及有無成功
                int erow = upd("DRUG_SHOW", dt_c);
                if (erow >= 1)
                    t = true;
            }
            return t;
        }
        /// <summary>
        /// 設定table的colum
        /// </summary>
        /// <param name="dt">DataTable</param>
        /// <param name="clumn">string[]</param>
        protected DataTable set_dt_column(DataTable dt, string[] clumn)
        {
            for (int i = 0; i < clumn.Length; i++)
            {
                dt.Columns.Add(clumn[i]);
            }
            return dt;
        }

    }
}
