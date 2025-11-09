using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

namespace NIS.Models
{
    public class FlushCatheter : DBConnector
    {

        public DataTable sel_flush_catheter_data_bydate(string feeno, string Startdate, string Enddate)
        {
            //一天的日期區間改成 0701~隔天07:00
            //string dateSubOneDay = (Convert.ToDateTime(Startdate + " 23:00:00").AddDays(-1)).ToString("yyyy/MM/dd HH:mm:ss");
            string start_str = (Convert.ToDateTime(Startdate + " 07:01:00")).ToString("yyyy/MM/dd HH:mm:ss");
            string end_str = (Convert.ToDateTime(Enddate + " 07:00:59").AddDays(1)).ToString("yyyy/MM/dd HH:mm:ss");
            DataTable dt = new DataTable();
            string sql = "SELECT A1.*,(SELECT P_NAME FROM SYS_PARAMS S1 WHERE P_MODEL = 'catheter_flush' AND P_GROUP = 'urine_color' AND S1.P_VALUE = A1.COLORID)COLORNAME ";
            sql += "FROM (SELECT * FROM FLUSH_CATHETER_DATA FC ";
            sql += "WHERE 0=0 AND DELETED IS NULL AND FEENO='" + feeno + "' AND AMOUNT_UNIT='1' AND RECORD_CLASS IN ('0','1') ";
            sql += "AND RECORD_TIME BETWEEN TO_DATE('" + start_str + "','yyyy/mm/dd hh24:mi:ss') ";
            sql += "AND TO_DATE('" + end_str + "','yyyy/mm/dd hh24:mi:ss'))A1 ";
            sql += "ORDER BY RECORD_TIME,RECORD_CLASS ";
            dt = base.DBExecSQL(sql);
            return dt;
        }

        public class flush_catheter_data
        {
            public string RECORD_ID { get; set; }
            public DateTime RECORD_TIME { get; set; }
            public string RECORD_CLASS { get; set; }
            public string AMOUNT { get; set; }
            public string COLORID { get; set; }
            public string COLOROTHER { get; set; }
            public string COLORNAME { get; set; }
            public string BIT_SURPLUS { get; set; }
            public string POST_OP { get; set; }
            //public string REMARK { get; set; }

            public flush_catheter_data(string i_RECORD_ID, DateTime i_RECORD_TIME, string i_RECORD_CLASS, string i_AMOUNT,
                string i_COLORID, string i_COLOROTHER, string i_COLORNAME, string i_BIT_SURPLUS, string i_POST_OP
                )
            {
                this.RECORD_ID = i_RECORD_ID;
                this.RECORD_TIME = i_RECORD_TIME;
                this.RECORD_CLASS = i_RECORD_CLASS;
                this.AMOUNT = i_AMOUNT;
                this.COLORID = i_COLORID;
                this.COLOROTHER = i_COLOROTHER;
                this.COLORNAME = i_COLORNAME;
                this.BIT_SURPLUS = i_BIT_SURPLUS;
                this.POST_OP = i_POST_OP;
            }
        }


        //取得班別內輸入的第一筆及最後一筆時間，供計算---原寫法(暫停使用)
        //取得每班裡的有POST OP的狀態及時間
        public DataTable min_max_recordtime(string feeno, string starttime, string endtime)
        {
            DataTable dt = new DataTable();
            //string sql = "SELECT MIN(RECORD_TIME)as MIN,MAX(RECORD_TIME)as MAX from FLUSH_CATHETER_DATA ";
            //sql += "WHERE 0=0 AND DELETED IS NULL AND FEENO='" + feeno + "' AND AMOUNT_UNIT='1' AND RECORD_CLASS IN ('0') ";
            //sql += "AND RECORD_TIME BETWEEN TO_DATE('" + starttime + "','yyyy/mm/dd hh24:mi:ss') ";
            //sql += "AND TO_DATE('" + endtime + "','yyyy/mm/dd hh24:mi:ss')";
            string sql = "SELECT RECORD_TIME from FLUSH_CATHETER_DATA ";
            sql += "WHERE 0=0 AND DELETED IS NULL AND FEENO='" + feeno + "' ";
            sql += "AND AMOUNT_UNIT='1' AND RECORD_CLASS IN ('0') AND POST_OP='Y' ";
            sql += "AND RECORD_TIME BETWEEN TO_DATE('" + starttime + "','yyyy/mm/dd hh24:mi:ss') ";
            sql += "AND TO_DATE('" + endtime + "','yyyy/mm/dd hh24:mi:ss') ORDER BY RECORD_TIME DESC";
            dt = base.DBExecSQL(sql);
            return dt;
        }

        //修改頁面Load舊資料
        public DataTable QueryReCordData(string feeno, string id)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM FLUSH_CATHETER_DATA WHERE 0=0 ";
            sql += "AND DELETED IS NULL AND FEENO='" + feeno + "' AND AMOUNT_UNIT='1' ";
            sql += "AND RECORD_ID='" + id + "'";
            dt = base.DBExecSQL(sql);
            return dt;
        }
    }
}