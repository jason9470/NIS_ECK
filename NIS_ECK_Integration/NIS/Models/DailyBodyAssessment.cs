using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NIS.Models;
using System.Data;
using System.Data.OleDb;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace NIS.Models
{
    public class DailyAssessment
    {
        public string DBAM_ID { get; set; }
        public string FEENO { get; set; }
        public string DBAM_TYPE { get; set; }
        public string DBAM_CARE_RECORD { get; set; }
        public string DELETED { get; set; }
        public string CREANO { get; set; }
        public DateTime CREATTIME { get; set; }
        public object UPDNO { get; set; }
        public object UPDTIME { get; set; }
        public object DBAM_VER { get; set; }
        public DateTime DBAM_DTM { get; set; }
        public string DBAM_TEMP_TYPE { get; set; }
        public string DBAD_ID { get; set; }
        public string DBAM_ID1 { get; set; }
        public string DBAD_ITEMID { get; set; }
        public string DBAD_ITEMVALUE { get; set; }
        public string DBAD_ITEMTYPE { get; set; }
        public string ASSDATE { get; set; }
        public string ASSTIME { get; set; }
    }
    public class DailyBodyAssessment : DBConnector
    {        
        public DataTable sel_dailybodyassess_data(string feeno, string id, string dbamtype, string St = "", string Ed = "")
        {//橫式列表及單筆使用
            DataTable dt = new DataTable();

            string sql = "SELECT A.*, (TO_CHAR(A.DBAM_DTM,'yyyy/mm/dd')) AS ASSDATE, (TO_CHAR(A.DBAM_DTM,'hh24:mi')) AS ASSTIME ";
            if(id != "")
                sql += ", B.* ";
            sql += "FROM DAILY_BODY_ASSESSMENT_MASTER A ";
            if(id != "")
                sql += "LEFT JOIN DAILY_BODY_ASSESSMENT_DETAIL B ON A.DBAM_ID = B.DBAM_ID ";
            sql += "WHERE DELETED='N' ";

            if(feeno != "")
                sql += "AND A.FEENO='" + feeno + "' ";
            if(id != "")  //抓取單筆時使用id
                sql += "AND A.DBAM_ID='" + id + "' ";
            if(dbamtype != "")  //判斷 成人/兒童 之評估
                sql += "AND A.DBAM_TYPE='" + dbamtype + "' ";
            if(St != "" && Ed != "")
            {
                sql += "AND (A.DBAM_DTM BETWEEN to_date('" + Convert.ToDateTime(St).ToString("yyyy/MM/dd HH:mm") + "','yyyy/mm/dd hh24:mi') ";
                sql += "AND to_date('" + Convert.ToDateTime(Ed).ToString("yyyy/MM/dd HH:mm") + "','yyyy/mm/dd hh24:mi') ) ";
            }
            sql += " ORDER BY DBAD_ID ASC ";
            //sql += "ORDER BY DBAD_ITEMTYPE,A.DBAM_DTM,CREATTIME,UPDTIME ASC ";舊方法會遇到bug

            base.DBExecSQL(sql, ref dt);

            return dt;
        }

        public DataTable sel_dailybodyassess_col_data(string feeno, string dbamtype, string St = "", string Ed = "")
        {//直式列表
            DataTable dt = new DataTable();

            string sql = "SELECT A.*, B.ListItemCont, (TO_CHAR(A.DBAM_DTM,'yyyy/mm/dd')) AS ASSDATE, (TO_CHAR(A.DBAM_DTM,'hh24:mi')) AS ASSTIME ";
            sql += "FROM DAILY_BODY_ASSESSMENT_MASTER A LEFT JOIN ";
            sql += "( ";
            sql += "SELECT DISTINCT DBAM_ID,LISTITEMCONT FROM ";
            sql += "( ";
            sql += "SELECT DBAM_ID,sys.stragg(C.DBAD_ITEMID || '|' || C.DBAD_ITEMVALUE || '|' || C.DBAD_ITEMTYPE || '※') OVER(partition by C.DBAM_ID order by C.DBAM_ID) AS LISTITEMCONT ";
            sql += "FROM DAILY_BODY_ASSESSMENT_DETAIL C ";
            sql += ") D ";
            //sql += "SELECT DBAM_ID, wmsys.wm_concat(C.DBAD_ITEMID || '|' || C.DBAD_ITEMVALUE || '|' || C.DBAD_ITEMTYPE || '※') ListItemCont ";
            //sql += "FROM DAILY_BODY_ASSESSMENT_DETAIL C GROUP BY DBAM_ID ";
            sql += ") B";
            sql += " ON A.DBAM_ID=B.DBAM_ID WHERE DELETED='N' ";

            if(feeno != "")
                sql += "AND A.FEENO='" + feeno + "' ";
            if(dbamtype != "")  //判斷 成人/兒童 之評估
                sql += "AND A.DBAM_TYPE='" + dbamtype + "' ";
            if(St != "" && Ed != "")
            {
                sql += "AND (A.DBAM_DTM BETWEEN to_date('" + Convert.ToDateTime(St).ToString("yyyy/MM/dd HH:mm") + "','yyyy/mm/dd hh24:mi') ";
                sql += "AND to_date('" + Convert.ToDateTime(Ed).ToString("yyyy/MM/dd HH:mm") + "','yyyy/mm/dd hh24:mi') ) ";
            }

            sql += "ORDER BY A.DBAM_DTM,CREATTIME,UPDTIME ASC ";

            base.DBExecSQL(sql, ref dt);

            return dt;
        }

        public DataTable sel_dailybodyassess_last_info(string feeno, string dbamtype)
        {//直式列表
            DataTable dt = new DataTable();

            string sql = "SELECT A.*,B.*,(TO_CHAR(A.DBAM_DTM,'yyyy/mm/dd')) AS ASSDATE, (TO_CHAR(A.DBAM_DTM,'hh24:mi')) AS ASSTIME FROM ( ";
            sql += "SELECT * FROM (SELECT * FROM DAILY_BODY_ASSESSMENT_MASTER ";
            sql += " WHERE DELETED='N' ";

            if(feeno != "")
                sql += "AND FEENO='" + feeno + "' ";
            if(dbamtype != "")  //判斷 成人/兒童 之評估
                sql += "AND DBAM_TYPE='" + dbamtype + "' ";

            sql += "ORDER BY CREATTIME DESC) WHERE ROWNUM < 2";
            sql += ") A LEFT JOIN DAILY_BODY_ASSESSMENT_DETAIL B ON A.DBAM_ID=B.DBAM_ID ";

            base.DBExecSQL(sql, ref dt);

            return dt;
        }

        public string sel_daly_body_sysparams_data(string p_model, string p_group)
        {
            DataTable dt = new DataTable();

            string sql = "SELECT REPLACE(MAX(P_VALUE),',','|') P_VALUE ";
            sql += "FROM (SELECT wmsys.wm_concat(A.P_VALUE) OVER(ORDER BY A.P_ID) P_VALUE FROM SYS_PARAMS A WHERE 1=1 ";

            if(p_model != "")
                sql += "AND A.P_MODEL='" + p_model + "' ";
            if(p_group != "")
                sql += "AND A.P_GROUP='" + p_group + "' ";

            sql += ") B ";


            base.DBExecSQL(sql, ref dt);

            return dt.Rows[0]["P_VALUE"].ToString();
        }

        public string sel_daly_body_sysparams_data_sorting(string p_model, string p_group)
        {
            DataTable dt = new DataTable();

            string sql = "SELECT REPLACE(MAX(P_VALUE),',','|') P_VALUE ";
            sql += "FROM (SELECT wmsys.wm_concat(A.P_VALUE) OVER(ORDER BY A.P_SORT) P_VALUE FROM SYS_PARAMS A WHERE 1=1 ";

            if (p_model != "")
                sql += "AND A.P_MODEL='" + p_model + "' ";
            if (p_group != "")
                sql += "AND A.P_GROUP='" + p_group + "' ";

            sql += "AND A.P_NAME NOT IN ('沉澱物','混濁')) B ";


            base.DBExecSQL(sql, ref dt);

            return dt.Rows[0]["P_VALUE"].ToString();
        }

        public static String SQLString(String ColString, bool IsRegex = false, bool Num = false)
        {
            if(ColString != null)
            {
                if(IsRegex)
                {
                    //   \ to \\
                    ColString = Regex.Replace(ColString, "[^\\w\\~～ 。@!！$＄%％^︿&＆*＊(（）)'＼／’]", "");
                }

                int _find = ColString.IndexOf("'");
                if(_find >= 0)
                {
                    ColString = ColString.Replace("'", "''");

                }
                return "'" + ColString.ToString().Trim() + "'";
            }
            else
            {
                if(Num) return "0";
                return "null";
            }
        }

        //將DataTable轉成JsonArray
        public static string DatatableToJsonArray(DataTable pDt)
        {
            return JsonConvert.SerializeObject(pDt, Formatting.Indented);
        }




        #region 20161019 每日身體評估列表改撈值的sql方式，與資料組成 ，撈取『主』表，在時間範圍內有幾筆有幾筆
        public DataTable sel_dailybodyassess_data_M(string feeno, string dbamtype, string St = "", string Ed = "")
        {//橫式列表及單筆使用
            //主表的select DBAM_ID from DAILY_BODY_ASSESSMENT_MASTER t where
            //feeno='I0332966' and dbam_type='adult'  
            //AND (DBAM_DTM BETWEEN to_date('2016/1/12 16:37','yyyy/mm/dd hh24:mi') AND to_date('2016/10/19 16:37','yyyy/mm/dd hh24:mi') )
            //ORDER BY DBAM_DTM,CREATTIME,UPDTIME ASC
            DataTable dt = new DataTable();
            string sql = "SELECT DBAM_ID,DBAM_DTM,CREANO,DBAM_TEMP_TYPE from DAILY_BODY_ASSESSMENT_MASTER t WHERE DELETED='N' ";
            if(feeno != "")
            {
                sql += "AND FEENO='" + feeno + "' ";
            }
            if(dbamtype != "")
            {//判斷 成人/兒童 之評估
                sql += "AND DBAM_TYPE='" + dbamtype + "' ";
            }
            if(St != "" && Ed != "")
            {
                sql += "AND (DBAM_DTM BETWEEN to_date('" + Convert.ToDateTime(St).ToString("yyyy/MM/dd HH:mm") + "','yyyy/mm/dd hh24:mi') AND to_date('" + Convert.ToDateTime(Ed).ToString("yyyy/MM/dd HH:mm") + "','yyyy/mm/dd hh24:mi')) ";
            }
            sql += "ORDER BY DBAM_DTM DESC,CREATTIME,UPDTIME ASC";
            base.DBExecSQL(sql, ref dt);
            return dt;
        }
        #endregion

        #region 20161019 每日身體評估列表改撈值的sql方式，與資料組成 ，撈取『子』表，在時間範圍內所有的資料
        public DataTable sel_dailybodyassess_data_D(string feeno, string dbamtype, string St = "", string Ed = "")
        {//橫式列表及單筆使用
            //主表的select DBAM_ID from DAILY_BODY_ASSESSMENT_MASTER t where
            //feeno='I0332966' and dbam_type='adult'  
            //AND (DBAM_DTM BETWEEN to_date('2016/1/12 16:37','yyyy/mm/dd hh24:mi') AND to_date('2016/10/19 16:37','yyyy/mm/dd hh24:mi') )
            //ORDER BY DBAM_DTM,CREATTIME,UPDTIME ASC
            DataTable dt = new DataTable();
            string sql = "SELECT DBAD_ID,DBAM_ID,(DBAD_ITEMID || '|' || DBAD_ITEMVALUE || '|' || DBAD_ITEMTYPE ) a FROM DAILY_BODY_ASSESSMENT_DETAIL t ";
            sql += "WHERE DBAM_ID IN (SELECT DBAM_ID FROM DAILY_BODY_ASSESSMENT_MASTER t WHERE DELETED='N' ";
            if(feeno != "")
            {
                sql += "AND FEENO='" + feeno + "' ";
            }
            if(dbamtype != "")
            {//判斷 成人/兒童 之評估
                sql += "AND dbam_type='" + dbamtype + "' ";
            }
            if(St != "" && Ed != "")
            {
                sql += "AND t.DBAM_DTM BETWEEN to_date('" + Convert.ToDateTime(St).ToString("yyyy/MM/dd HH:mm") + "','yyyy/mm/dd hh24:mi') ";
                sql += "AND to_date('" + Convert.ToDateTime(Ed).ToString("yyyy/MM/dd HH:mm") + "','yyyy/mm/dd hh24:mi')) ";
            }
            sql += "Order by DBAM_ID,DBAD_ID";
            base.DBExecSQL(sql, ref dt);
            return dt;
        }
        #endregion


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

        /// <summary>
        /// 轉換特殊字元
        /// </summary>
        /// <param name="content">內容傳入</param>
        /// <returns></returns>
        public string trans_special_code_with_Daily_Body(string content)
        {
            if(content != null)
            {
                content = content.Trim();
                content = content.Replace(",", "，");
                content = content.Replace("|", " ");
                return content;
            }
            else
                return "";
        }
    }
}
