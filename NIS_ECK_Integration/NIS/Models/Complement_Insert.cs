using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NIS.Models;
using System.Data;


namespace NIS.Models
{
    public class Complement_Insert : DBConnector
    {
        public DataTable sel_func_list(string userno)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT EXAM_STATUS,END_TIME FROM NIS_COMPLEMENT_INSERT WHERE EXAM_STATUS <> 'N' ";
            sql += "AND (END_TIME IS NULL OR to_date('" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/mm/dd hh24:mi:ss') <= END_TIME) ";
            sql += "AND APL_NO = '" + userno + "' ";

            base.DBExecSQL(sql, ref dt);

            return dt;
        }

        public DataTable sel_complement_list(string userno, string status, string ck_endtime = "")
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM NIS_COMPLEMENT_INSERT WHERE 0 = 0 ";
            if (userno != "")
                sql += "AND APL_NO = '" + userno + "' ";
            if (status != "")
                sql += "AND EXAM_STATUS = '" + status + "' ";
            if (ck_endtime != "")
                sql += "AND EXAM_STATUS <> 'N' AND (to_date('" + DateTime.Now.ToString("yyyy/MM/dd 00:00:00") + "','yyyy/mm/dd hh24:mi:ss') <= END_TIME OR  END_TIME IS NULL) ";
            sql += "ORDER BY APL_TIME DESC";

            base.DBExecSQL(sql, ref dt);

            return dt;
        }

    }
}
