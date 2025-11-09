using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NIS.Models;
using System.Data;
using System.Data.OleDb;

namespace NIS.Models
{
    public class Wound : DBConnector
    {
        /// <summary>
        /// 更新
        /// </summary>
        public int upd(string TableName, DataTable dt)
        {
            int eftrow = 0;
            List<DBItem> insertDataList = new List<DBItem>();
            for(int i = 1; i < dt.Rows.Count; i++)
            {
                for(int j = 0; j < dt.Columns.Count - 1; j++)
                {
                    if(dt.Rows[0][j].ToString() == "String")
                        insertDataList.Add(new DBItem(dt.Columns[j].ToString(), dt.Rows[i][j].ToString(), DBItem.DBDataType.String));
                    else if(dt.Rows[0][j].ToString() == "DataTime")
                        insertDataList.Add(new DBItem(dt.Columns[j].ToString(), dt.Rows[i][j].ToString(), DBItem.DBDataType.DataTime));
                    else if(dt.Rows[0][j].ToString() == "Number")
                        insertDataList.Add(new DBItem(dt.Columns[j].ToString(), dt.Rows[i][j].ToString(), DBItem.DBDataType.Number));
                }
                eftrow = eftrow + base.DBExecUpdate(TableName, insertDataList, dt.Rows[i][dt.Columns.Count - 1].ToString());
                insertDataList.Clear();
            }
            return eftrow;
        }

        /// <summary>
        /// 刪除
        /// </summary>
        public int del(string TableName, string where)
        {
            int eftrow = base.DBExecDelete(TableName, where);
            return eftrow;
        }

        /// <summary>
        /// 搜尋壓傷紀錄_報表用20150330
        /// </summary>
        public DataTable sel_wound_t(string starttime, string endtime)
        {
            DataTable dt = null;

            //string sql = "select a.*, b.shape, b.class, b.dressing, b.measure from WOUND_DATA a left join WOUND_RECORD b on a.wound_id = b.wound_id where a.type='壓瘡' ";
            string sql = "select a.*, b.shape, b.class, b.dressing, b.measure from WOUND_DATA a left join WOUND_RECORD b on a.wound_id = b.wound_id where a.type IN ('壓瘡', '壓傷') ";

            if(starttime != "")
            {
                sql += "AND a.creattime <= to_date('" + Convert.ToDateTime(endtime).AddDays(1).ToString("yyyy/MM/dd 00:00:00") + "','yyyy/mm/dd hh24:mi:ss') ";
                sql += "AND (a.ENDTIME >= to_date('" + Convert.ToDateTime(starttime).ToString("yyyy/MM/dd 00:00:00") + "','yyyy/mm/dd hh24:mi:ss') OR ENDTIME IS NULL) ";
            }
            sql += "and b.RECORDTIME = (select max(RECORDTIME) from WOUND_RECORD where wound_id = a.wound_id) ";
            sql += "AND a.DELETED is null order by a.creattime ASC ";

            base.DBExecSQL(sql, ref dt);

            return dt;
        }
        public DataTable sel_wound_(string starttime, string endtime)
        {
            DataTable dt = null;

            //string sql = "select feeno,wound_id,creattime ,endtime,location,position,reason from WOUND_DATA where type='壓瘡' ";
            string sql = "select feeno,wound_id,creattime ,endtime,location,position,reason from WOUND_DATA where type IN ('壓瘡', '壓傷') ";

            if(starttime != "")
            {
                sql += "AND creattime <= to_date('" + Convert.ToDateTime(endtime).AddDays(1).ToString("yyyy/MM/dd 00:00:00") + "','yyyy/mm/dd hh24:mi:ss') ";
                sql += "AND (ENDTIME >= to_date('" + Convert.ToDateTime(starttime).ToString("yyyy/MM/dd 00:00:00") + "','yyyy/mm/dd hh24:mi:ss') OR ENDTIME IS NULL) ";
            }

            sql += "AND DELETED is null order by creattime ASC ";

            base.DBExecSQL(sql, ref dt);

            return dt;
        }
        public DataTable sel_wound_d(string id)
        {
            DataTable dt = null;

            string sql = "select shape,class,dressing, measure from WOUND_RECORD  where 0=0 ";

            if(id != "")
            {
                sql += "AND wound_id = '" + id + "' ";
            }

            sql += "AND DELETED is null order by recordtime desc ";

            base.DBExecSQL(sql, ref dt);

            return dt;
        }
        public DataTable sel_wound_data(string feeno, string idList)
        {
            DataTable dt = new DataTable();

            string sql = "SELECT WD.* ";
            sql += " ,(CASE WHEN (SELECT COUNT(*) FROM WOUND_RECORD WHERE WOUND_ID = WD.WOUND_ID AND DELETED IS NULL ) > 0 THEN 'F' ELSE 'T' END)DELETED_BOOL";
            sql += " FROM WOUND_DATA WD WHERE 0 = 0 ";
            if (feeno != "")
                sql += "AND WD.FEENO = '" + feeno + "' ";
            if(idList != "")
            {
                string[] id = idList.Split(',');
                for(int i = 0; i < id.Length; i++)
                {
                    if(i == 0)
                        sql += "AND ( WD.WOUND_ID = '" + id[i] + "' ";
                    else
                        sql += "OR WD.WOUND_ID = '" + id[i] + "' ";
                }
                sql += " )";
            }
            sql += "ORDER BY NUM ASC ";

            base.DBExecSQL(sql, ref dt);

            return dt;
        }

        public DataTable sel_wound_record(string userno, string feeno, string record_id)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM WOUND_RECORD WHERE 0 = 0 ";
            if(userno != "")
                sql += "AND CREANO = '" + userno + "' ";
            if(feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if(record_id != "")
                sql += "AND RECORD_ID = '" + record_id + "' ";
            base.DBExecSQL(sql, ref dt);

            return dt;
        }

        public DataTable sel_wound_record(string userno, string feeno, string record_id, string wound_id)
        {
            DataTable dt = new DataTable();

            string sql = "SELECT WOUND_RECORD.*, ";
            sql += "(SELECT NUM FROM WOUND_DATA WHERE WOUND_DATA.WOUND_ID = WOUND_RECORD.WOUND_ID AND DELETED is null )NUM, ";
            sql += "(SELECT ENDTIME FROM WOUND_DATA WHERE WOUND_DATA.WOUND_ID = WOUND_RECORD.WOUND_ID AND DELETED is null )ENDTIME, ";
            sql += "(SELECT DELETED FROM WOUND_DATA WHERE WOUND_DATA.WOUND_ID = WOUND_RECORD.WOUND_ID )WOUND_DELETED ";
            sql += "FROM WOUND_RECORD WHERE 0 = 0  AND DELETED is null ";
            if(userno != "")
                sql += "AND CREANO = '" + userno + "' ";
            if(feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if(record_id != "")
                sql += "AND RECORD_ID = '" + record_id + "' ";
            if(wound_id != "")
                sql += "AND WOUND_ID = '" + wound_id + "' ";

            sql += "ORDER BY RECORDTIME DESC";

            base.DBExecSQL(sql, ref dt);

            return dt;
        }

        public DataTable sel_wound_record_byWound(DataTable dt, string userno, string feeno, string wound_id)
        {
            string sql = "SELECT * FROM (SELECT * FROM WOUND_RECORD WHERE 0 = 0 AND DELETED is null ";
            //if (userno != "")
            //    sql += "AND CREANO = '" + userno + "' ";
            if(feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if(wound_id != "")
                sql += "AND WOUND_ID = '" + wound_id + "' ";

            sql += "ORDER BY RECORDTIME DESC ) where rownum <= 1 ";

            base.DBExecSQL(sql, ref dt);

            return dt;
        }

        public DataTable sel_group_wound_record(string userno, string feeno)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM ( SELECT DISTINCT WOUND_ID,";
            sql += "(SELECT ENDTIME FROM WOUND_DATA WHERE WOUND_RECORD.WOUND_ID = WOUND_DATA.WOUND_ID)ENDTIME,";
            sql += "(SELECT DELETED FROM WOUND_DATA WHERE WOUND_RECORD.WOUND_ID = WOUND_DATA.WOUND_ID)WOUND_DELETED ";
            sql += "FROM WOUND_RECORD WHERE 0 = 0 ";
            if(userno != "")
                sql += "AND CREANO = '" + userno + "' ";
            if(feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            sql += ") WHERE ENDTIME is null AND WOUND_DELETED is null ";
            base.DBExecSQL(sql, ref dt);
            sql = "";

            foreach(DataRow r in dt.Rows)
            {
                sql += " SELECT * FROM (SELECT WOUND_RECORD.*,";
                sql += "(SELECT NUM FROM WOUND_DATA WHERE WOUND_DATA.WOUND_ID = WOUND_RECORD.WOUND_ID)NUM,";
                sql += "(SELECT POSITION FROM WOUND_DATA WHERE WOUND_DATA.WOUND_ID = WOUND_RECORD.WOUND_ID)POSITION,";
                sql += "('" + r["ENDTIME"] + "')ENDTIME,";
                sql += "('" + r["WOUND_DELETED"] + "')WOUND_DELETED ";
                sql += "FROM WOUND_RECORD WHERE 0 = 0 AND DELETED is null ";
                if(userno != "")
                    sql += "AND CREANO = '" + userno + "' ";
                if(feeno != "")
                    sql += "AND FEENO = '" + feeno + "' ";
                sql += "AND WOUND_ID = '" + r["WOUND_ID"] + "' ORDER BY RECORDTIME DESC) ";
                sql += "WHERE rownum <= 1 union";
            }
            if(sql != "")
            {
                sql = "SELECT * FROM (" + sql.Substring(0, sql.Length - 5) + ")ORDER BY NUM ASC ";
                dt.Reset();
                base.DBExecSQL(sql, ref dt);
            }

            return dt;
        }

        public string sel_wound_sysparams_data(string p_model, string p_group)
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

        public DataTable sel_wound_record_data(string feeno, string WOUNDID, string SrchType = "")
        {//各部位之評估 最新一筆列表 
            DataTable dt = new DataTable();

            string sql = "";

            if((WOUNDID == null || WOUNDID == "") && SrchType == "")
                sql += "SELECT C.* FROM ( ";

            sql += " SELECT A.WOUND_ID, A.FEENO, A.NUM, A.TYPE, A.POSITION, A.LOCATION, A.REASON, A.ENDTIME, A.ENDT_REASON, A.DELETED, A.DEL_REASON, A.CREATTIME, ";
            sql += " B.RECORD_ID, B.RECORDTIME, B.range_YN, B.backout_YN, B.range_other,B.exudate_YN, B.EXUDATE_COLOR, B.EXUDATE_NATURE, ";
            sql += " B.EXUDATE_COLOR_OTHER,B.EXUDATE_NATURE_OTHER,B.EXTERIOR,B.EXTERIOR_OTHER,B.SCALD_RANGE,B.WOUND_LEVEL,B.EXUDATE_AMOUNT, ";//by jarvis add 20160914
            sql += " B.sutures_YN, B.stitchesDate, B.pin, B.handle_YN, B.handle_item, B.handle_other, B.grade, B.plan_YN, B.RANGE_WIDTH, B.RANGE_HEIGHT, B.RANGE_DEPTH, B.CREANO, B.WOUND_IMG_ID, ";
            sql += " B.PERINEUM_RANGE,B.PERINEUM_RED,B.PERINEUM_SWOLLEN,B.SWOLLEN_RANGE,B.PERINEUM_BRUISE,B.PERINEUM_SECRETION, ";
            sql += " ROW_NUMBER() OVER(PARTITION BY B.WOUND_ID ORDER BY B.RECORDTIME DESC) AS TOPROW ";
            sql += " FROM WOUND_RECORD B LEFT JOIN WOUND_DATA A ON A.WOUND_ID=B.WOUND_ID WHERE A.DELETED IS NULL AND B.DELETED IS NULL";

            if(feeno != null && feeno != "")
                sql += " AND A.FEENO='" + feeno + "' ";
            if(WOUNDID != null && WOUNDID != "")
                sql += " AND A.WOUND_ID='" + WOUNDID + "' ";

            if((WOUNDID == null || WOUNDID == "") && SrchType == "")
                sql += ") C  WHERE C.TOPROW='1' ";

            base.DBExecSQL(sql, ref dt);

            return dt;
        }
        public DataTable sel_wound_record_img(string feeno, string record_id)
        {//各部位之評估 最新一筆列表 
            DataTable dt = new DataTable();

            string sql = " SELECT WOUND_IMG_DATA";
            sql += " FROM WOUND_RECORD WHERE ";

            if (!string.IsNullOrEmpty(feeno))
            {
                sql += " FEENO='" + feeno + "' ";
            }
            if (!string.IsNullOrEmpty(record_id))
            {
                sql += " AND RECORD_ID='" + record_id + "' ";
            }

            base.DBExecSQL(sql, ref dt);

            return dt;
        }
        public DataTable sel_wound_record_byLastInfo(string ModeVal, DataTable dt, string wound_id)
        {
            string sql = "SELECT A.WOUND_ID, A.NUM, A.WOUND_ROW, A.CREATTIME, A.TYPE, A.POSITION, A.POSITION_TYPE, A.LOCATION, A.ENDTIME, B.* ";
            sql += "FROM WOUND_DATA A LEFT JOIN ";

            if(wound_id != "" && ModeVal == "Insert_Record")  //傷口ID
                sql += "(SELECT * FROM WOUND_RECORD WHERE WOUND_ID='" + wound_id + "' ORDER BY RECORDTIME DESC) B ";
            else if(wound_id != "" && ModeVal == "Update_Record")  //評估ID
                sql += "(SELECT * FROM WOUND_RECORD WHERE RECORD_ID='" + wound_id + "' ORDER BY RECORDTIME DESC) B ";

            sql += "ON A.WOUND_ID=B.WOUND_ID WHERE 1=1 ";

            if(wound_id != "" && ModeVal == "Insert_Record")  //傷口ID
                sql += "AND A.WOUND_ID = '" + wound_id + "' ";
            else if(wound_id != "" && ModeVal == "Update_Record")  //評估ID
                sql += "AND B.RECORD_ID = '" + wound_id + "' ";

            sql += "AND rownum <= 1 ORDER BY A.NUM ";

            base.DBExecSQL(sql, ref dt);

            return dt;
        }

    }
}
