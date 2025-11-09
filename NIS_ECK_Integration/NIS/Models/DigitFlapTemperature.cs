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
    public class DigitFlapTemperature : DBConnector
    {    

        public DataTable sel_temperature_data(string feeno, string id, string row)
        {
            DataTable dt = new DataTable();

           // string sql = "SELECT * FROM DIGI_FLAP_DATA WHERE DELETED='N' AND ENDTIME IS NULL ";

            string sql = "SELECT * FROM DIGI_FLAP_DATA WHERE DELETED='N'";

            if (feeno != "")
                sql += "AND FEENO='" + feeno + "' ";
            if (id != "")//抓取單筆時使用id
                sql += "AND DFTEMP_ID='" + id + "' ";
            if (row != "")
            {//記錄的時候使用row記錄
                string[] row_ = row.Split(',');
                for (int i = 0; i < row_.Length; i++)
                {
                    if (i == 0)
                        sql += "AND ( DFTEMP_ROW = " + row_[i] + " ";
                    else
                        sql += "OR DFTEMP_ROW = " + row_[i] + " ";
                }
                sql += " )";
            }

            sql += "ORDER BY DFTEMP_NUM ASC ";

            base.DBExecSQL(sql, ref dt);

            return dt;
        }

        public DataTable sel_temperature_data_row(string feeno, string St = "", string Ed = "")
        {
            //只有 feeno 代表抓 num ；有日期為查詢，故加上 DELETED 和 ENDTIME
            DataTable dt = new DataTable();

            string sql = "SELECT * FROM DIGI_FLAP_DATA WHERE 1=1 ";

            if (feeno != "")
                sql += "AND FEENO='" + feeno + "' ";
            if (St != "" && Ed != "")
            {
                sql += "AND (DFTEMP_CREATE_DTM BETWEEN to_date('" + Convert.ToDateTime(St).ToString("yyyy/MM/dd 00:00:00") + "','yyyy/mm/dd hh24:mi:ss') ";
                sql += "AND to_date('" + Convert.ToDateTime(Ed).ToString("yyyy/MM/dd 23:59:59") + "','yyyy/mm/dd hh24:mi:ss') ) ";
                sql += "AND DELETED='N' ";//AND ENDTIME IS NULL ";
            }

            sql += "ORDER BY DFTEMP_NUM ASC ";

            base.DBExecSQL(sql, ref dt);

            return dt;
        }

        public DataTable sel_temperature_record(string feeno, string DFTempID, string St = "", string Ed = "")
        {//各部位之評估 最新一筆列表
            DataTable dt = new DataTable();

            string sql = "";

            if (DFTempID == "")
                sql += "SELECT C.* FROM ( ";

            sql += " SELECT A.DFTEMP_ID, A.FEENO, A.DFTEMP_NUM, A.DFTEMP_TYPE, A.DFTEMP_POSITION, A.DFTEMP_CTRL_LIMB, A.DFTEMP_POSITION_CONT, A.DFTEMP_CREATE_DTM, ";
            sql += " B.DFRECORD_ID, B.RECORDTIME, B.DFRECORD_TEMPERATURE_ROOM, B.DFRECORD_TEMPERATURE, B.DFRECORD_COLOR, B.DFRECORD_COLOR_CONT, B.DFRECORD_PLUMPNESS, B.DFRECORD_MEASURE_YN, ";
            sql += " B.DFRECORD_MEASURE_CONT, B.DFRECORD_PIC, B.DFRECORD_REMARK, B.DFRECORD_CARE_RECORD, B.DELETED, B.CREANO, ";
            sql += " ROW_NUMBER() OVER(PARTITION BY B.DFTEMP_ID ORDER BY B.RECORDTIME DESC) AS TOPROW ";
            sql += " FROM DIGI_FLAP_RECORD B LEFT JOIN DIGI_FLAP_DATA A ON A.DFTEMP_ID=B.DFTEMP_ID WHERE A.DELETED='N' AND B.DELETED='N' ";// AND A.ENDTIME IS NULL

            if (feeno != "")
                sql += " AND A.FEENO='" + feeno + "' ";
            if (DFTempID != "")
                sql += " AND A.DFTEMP_ID='" + DFTempID + "' ";

            if (DFTempID == "")
                sql += ") C  WHERE C.TOPROW='1' ";

            if (St != "" && Ed != "")
            {
                sql += "AND (C.DFTEMP_CREATE_DTM BETWEEN to_date('" + Convert.ToDateTime(St).ToString("yyyy/MM/dd 00:00:00") + "','yyyy/mm/dd hh24:mi:ss') ";
                sql += "AND to_date('" + Convert.ToDateTime(Ed).ToString("yyyy/MM/dd 23:59:59") + "','yyyy/mm/dd hh24:mi:ss') ) ";
            }

            base.DBExecSQL(sql, ref dt);

            return dt;
        }

        public DataTable sel_temperature_record_by_date(string feeno, string St = "", string Ed = "")
        {//各部位之評估 最新一筆列表
            DataTable dt = new DataTable();

            string sql = "";

            sql += " SELECT A.DFTEMP_ID, A.FEENO, A.DFTEMP_NUM, A.DFTEMP_TYPE, A.DFTEMP_POSITION, A.DFTEMP_CTRL_LIMB, A.DFTEMP_POSITION_CONT, A.DFTEMP_CREATE_DTM, ";
            sql += " B.DFRECORD_ID, B.RECORDTIME, B.DFRECORD_TEMPERATURE_ROOM, B.DFRECORD_TEMPERATURE, B.DFRECORD_COLOR, B.DFRECORD_COLOR_CONT, B.DFRECORD_PLUMPNESS, B.DFRECORD_MEASURE_YN, ";
            sql += " B.DFRECORD_MEASURE_CONT, B.DFRECORD_PIC, B.DFRECORD_REMARK, B.DFRECORD_CARE_RECORD, B.DELETED, B.CREANO ";
            sql += " FROM DIGI_FLAP_RECORD B LEFT JOIN DIGI_FLAP_DATA A ON A.DFTEMP_ID=B.DFTEMP_ID WHERE A.DELETED='N' AND B.DELETED='N' ";// AND A.ENDTIME IS NULL

            if (feeno != "")
                sql += " AND A.FEENO='" + feeno + "' ";
            if (St != "" && Ed != "")
            {
                sql += "AND (A.DFTEMP_CREATE_DTM BETWEEN to_date('" + Convert.ToDateTime(St).ToString("yyyy/MM/dd 00:00:00") + "','yyyy/mm/dd hh24:mi:ss') ";
                sql += "AND to_date('" + Convert.ToDateTime(Ed).ToString("yyyy/MM/dd 23:59:59") + "','yyyy/mm/dd hh24:mi:ss') ) ";
            }

            base.DBExecSQL(sql, ref dt);

            return dt;
        }

        #region  原本延用NIS的寫法
        //public DataTable sel_temperature_record(string userno, string feeno, string dfrecord_id, string dftemp_id)
        //{
        //    DataTable dt = new DataTable();

        //    string sql = "SELECT DIGI_FLAP_RECORD.*, ";
        //    sql += "(SELECT DFTEMP_NUM FROM DIGI_FLAP_DATA WHERE DIGI_FLAP_DATA.DFTemp_ID = DIGI_FLAP_RECORD.DFTemp_ID AND DELETED='N' )DFTEMP_NUM, ";
        //    sql += "(SELECT ENDTIME FROM DIGI_FLAP_DATA WHERE DIGI_FLAP_DATA.DFTemp_ID = DIGI_FLAP_RECORD.DFTemp_ID AND DELETED='N' )ENDTIME, ";
        //    sql += "(SELECT DELETED FROM DIGI_FLAP_DATA WHERE DIGI_FLAP_DATA.DFTemp_ID = DIGI_FLAP_RECORD.DFTemp_ID )P_DELETED ";
        //    sql += "FROM DIGI_FLAP_RECORD WHERE 0 = 0  AND DELETED is null ";
        //    if (userno != "")
        //        sql += "AND CREANO = '" + userno + "' ";
        //    if (feeno != "")
        //        sql += "AND FEENO = '" + feeno + "' ";
        //    if (dfrecord_id != "")
        //        sql += "AND DFRECORD_ID = '" + dfrecord_id + "' ";
        //    if (dftemp_id != "")
        //        sql += "AND DFTemp_ID = '" + dftemp_id + "' ";

        //    sql += "ORDER BY RECORDTIME DESC";

        //    base.DBExecSQL(sql, ref dt);

        //    return dt;
        //}

        //public DataTable sel_temperature_record_byPosition(DataTable dt, string feeno, string dftemp_id)
        //{
        //    string sql = "SELECT * FROM (SELECT * FROM DIGI_FLAP_RECORD WHERE 0 = 0 AND DELETED='N' ";

        //    if (feeno != "")
        //        sql += "AND FEENO = '" + feeno + "' ";
        //    if (dftemp_id != "")
        //        sql += "AND DFTEMP_ID = '" + dftemp_id + "' ";

        //    sql += "ORDER BY RECORDTIME DESC ) where rownum <= 1 ";

        //    base.DBExecSQL(sql, ref dt);

        //    return dt;
        //}
        #endregion  原本延用NIS的寫法

        public DataTable sel_temperature_record_byLastInfo(string ModeVal, DataTable dt, string dftemp_id)
        {
            string sql = "SELECT A.DFTEMP_ID, A.DFTEMP_NUM, A.DFTEMP_ROW, A.DFTEMP_CREATE_DTM, A.DFTEMP_TYPE, A.DFTEMP_POSITION, A.DFTEMP_CTRL_LIMB, A.DFTEMP_POSITION_CONT, A.ENDTIME, B.* ";
            sql += "FROM DIGI_FLAP_DATA A LEFT JOIN ";

            if(dftemp_id != "" && ModeVal == "New")  //部位ID
                sql += "(SELECT * FROM DIGI_FLAP_RECORD WHERE DFTEMP_ID='" + dftemp_id + "' ORDER BY RECORDTIME DESC) B ";
            else if(dftemp_id != "" && ModeVal == "Edit")  //評估ID
                sql += "(SELECT * FROM DIGI_FLAP_RECORD WHERE DFRECORD_ID='" + dftemp_id + "' ORDER BY RECORDTIME DESC) B ";

            sql += "ON A.DFTEMP_ID=B.DFTEMP_ID WHERE 1=1 ";

            if(dftemp_id != "" && ModeVal == "New")  //部位ID
                sql += "AND A.DFTEMP_ID = '" + dftemp_id + "' ";
            else if(dftemp_id != "" && ModeVal == "Edit")  //評估ID
                sql += "AND B.DFRECORD_ID = '" + dftemp_id + "' ";

            sql += "AND rownum <= 1 ORDER BY A.DFTEMP_NUM ";

            base.DBExecSQL(sql, ref dt);

            return dt;
        }

        public DataTable sel_category_list(string PGroup, string PModel)
        {//抓取 部位、顏色 下拉式選單資訊
            DataTable dt = new DataTable();

            string sql = string.Format("SELECT A.P_NAME, A.P_VALUE FROM SYS_PARAMS A WHERE A.P_GROUP={0} AND A.P_MODEL={1} ORDER BY A.P_SORT ", SQLString(PGroup), SQLString(PModel));

            base.DBExecSQL(sql, ref dt);

            return dt;
        }

        public DataTable sel_ctrl_limb_list(string feeno)
        {//抓取 已建立對照肢資訊
            DataTable dt = new DataTable();

            string sql = "SELECT DISTINCT B.P_NAME, CASE WHEN TO_NCHAR(A.DFTEMP_POSITION) = TO_NCHAR('99') THEN A.DFTEMP_POSITION_Cont ELSE TO_NCHAR(A.DFTEMP_POSITION) END AS DFTEMP_POSITION";
            sql += ", A.DFTEMP_ID, B.P_VALUE FROM DIGI_FLAP_DATA A LEFT JOIN SYS_PARAMS B ON A.DFTEMP_POSITION=B.P_VALUE ";
            sql += "AND B.P_MODEL='digitflaptemperature' AND (B.P_GROUP='flap_position' OR B.P_GROUP='digit_position') WHERE A.DELETED='N' ";// AND A.ENDTIME IS NULL ";
            sql += "AND A.FEENO = '" + feeno + "' AND A.DFTEMP_TYPE = '1' ";
            sql += "ORDER BY B.P_VALUE ASC "; 

            base.DBExecSQL(sql, ref dt);

            return dt;
        }

        public DataTable sel_record_temperature_info(string feeno, string DFRecordID)
        {
            //抓取溫度  (這支目前因畫面問題無法實現公式，暫時無使用到) by 2016/4/29 tag wawa edit
            DataTable dt = new DataTable();

            string sql = "SELECT A.*, B.* ";
            sql += "FROM DIGI_FLAP_DATA A LEFT JOIN DIGI_FLAP_RECORD B ON A.DFTEMP_ID=B.DFTEMP_ID ";
            sql += "WHERE A.DELETED='N' ";// AND A.ENDTIME IS NULL ";

            if (feeno != "")
                sql += "AND A.FEENO = '" + feeno;
            if (DFRecordID != "")
                sql += "AND B.DFRECORD_ID = '" + DFRecordID;

            sql += "AND rownum <= 1 ORDER BY B.RECORDTIME ";

            base.DBExecSQL(sql, ref dt);

            return dt;
        }

        public static String SQLString(String ColString, bool IsRegex = false, bool Num = false)
        {
            if (ColString != null)
            {
                if (IsRegex)
                {
                    //   \ to \\
                    ColString = Regex.Replace(ColString, "[^\\w\\~～ 。@!！$＄%％^︿&＆*＊(（）)'＼／’]", "");
                }

                int _find = ColString.IndexOf("'");
                if (_find >= 0)
                {
                    ColString = ColString.Replace("'", "''");

                }
                return "'" + ColString.ToString().Trim() + "'";
            }
            else
            {
                if (Num) return "0";
                return "null";
            }
        }

        //將DataTable轉成JsonArray
        public static string DatatableToJsonArray(DataTable pDt)
        {
            return JsonConvert.SerializeObject(pDt, Formatting.Indented);
        }

    }
}
