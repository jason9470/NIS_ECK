using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

namespace NIS.Models
{
    public class BloodProducts : DBConnector
    {
        private string mode = NIS.MvcApplication.iniObj.NisSetting.ServerMode.ToString();

        public DataTable Query(string chartNo, string startTime, string endTime)
        {
            DataTable dt = new DataTable();

            if (string.IsNullOrEmpty(chartNo))
            {
                return dt;
            }

            var BCLASS_Table = "BBK.BCLASS";
            var BOUT_Table = "BBK.BOUT";
            var BREADY_Table = "BBK.BREADY";
            var BINV_Table = "BBK.BINV";

            string sql = "";

            if (mode == "Maya")
            {
                sql = "SELECT * FROM BLOODPRODUCTS \n";
                //sql += "WHERE CHART_NO = '" + chartNo + "'";
                sql += "WHERE TO_DATE(TO_CHAR(TO_NUMBER(SUBSTR(OUT_DATE, 1, 3)) + 1911) || SUBSTR(OUT_DATE, 4, 4) || LPAD(OUT_TIME, 6, '0'),'YYYYMMDDHH24MISS') ";
                sql += "BETWEEN TO_DATE('" + startTime + "', 'YYYY-MM-DD HH24:MI:SS') AND TO_DATE('" + endTime + "', 'YYYY-MM-DD HH24:MI:SS') \n";
                sql += "ORDER BY OUT_DATE, OUT_TIME DESC";
                base.DBExecSQL(sql, ref dt);
                return dt;
            }

            sql = "SELECT BO.CHART_NO,BO.OUT_DATE ,BO.OUT_TIME, BO.BLOOD_CLASS, \n";
            sql += "(SELECT BLOOD_CLASS_NAME FROM " + BCLASS_Table + " WHERE BLOOD_CLASS = BO.BLOOD_CLASS ) BLOOD_NAME, \n";
            sql += "(SELECT BLOOD_SHORT_NAME FROM " + BCLASS_Table + " WHERE BLOOD_CLASS = BO.BLOOD_CLASS ) BLOOD_SHORT_NAME, \n";
            sql += "BO.BLOOD_NO, BA.BLOOD_TYPE, BA.RH, \n";
            sql += "BI.EFFECTIVE_DATE \n";
            sql += "FROM " + BOUT_Table + " BO," + BREADY_Table + " BA," + BINV_Table + " BI \n";
            sql += "WHERE BO.READY_NO = BA.READY_NO \n";
            sql += "AND BI.BLOOD_NO = BO.BLOOD_NO AND BI.BLOOD_CLASS = BO.BLOOD_CLASS \n";
            sql += "AND BA.CHART_NO = '" + chartNo + "' \n";
            sql += "AND TO_DATE(TO_CHAR(TO_NUMBER(SUBSTR(BO.OUT_DATE, 1, 3)) + 1911) || SUBSTR(BO.OUT_DATE, 4, 4) || LPAD(BO.OUT_TIME, 6, '0'),'YYYYMMDDHH24MISS') ";
            sql += "BETWEEN TO_DATE('" + startTime + "', 'YYYY-MM-DD HH24:MI:SS') AND TO_DATE('" + endTime + "', 'YYYY-MM-DD HH24:MI:SS') \n";
            sql += "ORDER BY BO.OUT_DATE ,BO.OUT_TIME DESC";
            base.DBExecSQL(sql, ref dt);
            return dt;
        }
    }
}