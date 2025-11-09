using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

namespace NIS.Models
{
    public class CVVH_Data
    {
        private DBConnector link;
        public CVVH_Data()
        {
            this.link = new DBConnector();
        }

        /// <summary>
        /// CVVH主頁列表的資料
        /// </summary>
        /// <param name="feeno">批價序號</param>
        /// <param name="StartDate">開始時間</param>
        /// <param name="EndDate">結束時間</param>
        /// <returns></returns>
        public DataTable CVVH_Main_List_Data(string feeno, string StartDate = "", string EndDate = "")//計算CVVH的I/O總計的SQL扣
        {
            DataTable dt = new DataTable();
            string tomorrow = Convert.ToDateTime(EndDate).AddDays(1).ToString("yyyy/MM/dd");
            string SqlStr = "SELECT A.*,NVL((SELECT SUM(B.IO_TOTAL) FROM CVVH_DTL_DATA B WHERE A.RECORD_ID=B.RECORD_ID AND DELETED IS NULL),'0')TOTALNUM ";
            SqlStr += "FROM CVVH_MASTER A WHERE 0=0 AND  FEENO='" + feeno + "' AND DELETED IS NULL ";
            SqlStr += "AND RECORD_TIME BETWEEN TO_DATE('" + StartDate + "','yyyy/mm/dd') ";
            SqlStr += "AND TO_DATE('" + tomorrow + "','yyyy/mm/dd') ";
            SqlStr += "ORDER BY RECORD_TIME,UPDTIME ";
            dt = this.link.DBExecSQL(SqlStr);
            return dt;
        }

        /// <summary>
        /// CVVH記錄的LIST DATA
        /// </summary>
        /// <param name="id">主表ID</param>
        /// <returns></returns>
        public DataTable CVVH_Record_Data(string id)
        {
            DataTable dt = new DataTable();
            string SqlStr = "SELECT ROWNUM,B.* FROM(SELECT A.* FROM CVVH_DTL_DATA A WHERE 0=0 AND RECORD_ID='" + id + "'  AND DELETED IS NULL ";
            SqlStr += "ORDER BY DATA_TIME,UPDTIME)B";
            dt = this.link.DBExecSQL(SqlStr);
            return dt;
        }

        /// <summary>
        /// 統計三班的值
        /// </summary>
        /// <param name="feeno">批價序號</param>
        /// <param name="dtldate">日期</param>
        /// <returns></returns>
        public DataTable CVVH_total(string feeno, string dtldate)
        {
            DataTable dt = new DataTable();
            string tomorrow = Convert.ToDateTime(dtldate).AddDays(1).ToString("yyyy/MM/dd");
            string yesterday = Convert.ToDateTime(dtldate).AddDays(-1).ToString("yyyy/MM/dd");
            string tomorrow_3 = Convert.ToDateTime(dtldate).AddDays(1).ToString("yyyy-MM-dd");
            string dtldate_2 = Convert.ToDateTime(dtldate).ToString("yyyy-MM-dd");
            string yesterday_1 = Convert.ToDateTime(dtldate).AddDays(-1).ToString("yyyy-MM-dd");
            string SqlTotal = "SELECT NVL((SELECT SUM(io_total) FROM CVVH_DTL_DATA WHERE DELETED IS NULL AND  RECORD_ID IN (SELECT RECORD_ID ";
            SqlTotal += "FROM CVVH_MASTER WHERE FEENO = '" + feeno + "' AND DELETED IS NULL AND TO_CHAR(RECORD_TIME, 'YYYY-MM-DD') IN (";
            SqlTotal += "'" + yesterday_1 + "' ,'" + dtldate_2 + "' ,'" + tomorrow_3 + "')) ";
            SqlTotal += "AND DATA_TIME BETWEEN TO_DATE('" + Convert.ToDateTime(dtldate).ToString("yyyy/MM/dd") + " 07:01:00', 'yyyy/mm/dd hh24:mi:ss') AND TO_DATE('" + Convert.ToDateTime(dtldate).ToString("yyyy/MM/dd") + " 15:00:59', 'yyyy/mm/dd hh24:mi:ss')";
            SqlTotal += "), 0) morning";
            SqlTotal += ",NVL((SELECT SUM(io_total) FROM CVVH_DTL_DATA WHERE DELETED IS NULL  AND  RECORD_ID IN (SELECT RECORD_ID ";
            SqlTotal += "FROM CVVH_MASTER WHERE FEENO = '" + feeno + "' AND DELETED IS NULL AND TO_CHAR(RECORD_TIME, 'YYYY-MM-DD') IN (";
            SqlTotal += "'" + yesterday_1 + "' ,'" + dtldate_2 + "' ,'" + tomorrow_3 + "')) ";
            SqlTotal += "AND DATA_TIME BETWEEN TO_DATE('" + Convert.ToDateTime(dtldate).ToString("yyyy/MM/dd") + " 15:01:00', 'yyyy/mm/dd hh24:mi:ss') AND TO_DATE('" + Convert.ToDateTime(dtldate).ToString("yyyy/MM/dd") + " 23:00:59', 'yyyy/mm/dd hh24:mi:ss')";
            SqlTotal += "), 0) after";
            SqlTotal += ",NVL((SELECT SUM(io_total) FROM CVVH_DTL_DATA WHERE DELETED IS NULL AND  RECORD_ID IN (SELECT RECORD_ID ";
            SqlTotal += "FROM CVVH_MASTER WHERE FEENO = '" + feeno + "' AND DELETED IS NULL AND TO_CHAR(RECORD_TIME, 'YYYY-MM-DD') IN (";
            SqlTotal += "'" + yesterday_1 + "','" + dtldate_2 + "','" + tomorrow_3 + "')) ";
            SqlTotal += "AND DATA_TIME BETWEEN TO_DATE('" + Convert.ToDateTime(dtldate).ToString("yyyy/MM/dd") + " 23:01:00', 'yyyy/mm/dd hh24:mi:ss') AND TO_DATE('" + Convert.ToDateTime(tomorrow).ToString("yyyy/MM/dd") + " 07:00:59', 'yyyy/mm/dd hh24:mi:ss')";
            SqlTotal += "), 0) night";
            SqlTotal += ",NVL((SELECT RECORD_ID FROM CVVH_MASTER t WHERE DELETED IS NULL AND TO_CHAR(RECORD_TIME, 'YYYY-MM-DD') = '" + yesterday_1 + "'";
            SqlTotal += "AND FEENO = '" + feeno + "'), 0) beforeday";
            SqlTotal += ",NVL((SELECT RECORD_ID FROM CVVH_MASTER t WHERE DELETED IS NULL AND TO_CHAR(RECORD_TIME, 'YYYY-MM-DD') = '" + tomorrow_3 + "'";
            SqlTotal += "AND FEENO = '" + feeno + "'), 0) nextday	FROM dual";
            dt = this.link.DBExecSQL(SqlTotal);
            return dt;
        }

        #region CVVH TPR 資料
        /// <summary>
        /// 建構CVVH_TPR的資料
        /// </summary>
        public class CVVH_TPR_Data
        {
            public string DataDate { get; set; }
            public string IO_Total { get; set; }
            public CVVH_TPR_Data(string P_Date, string P_i_io_total)
            {
                this.DataDate = P_Date;
                this.IO_Total = P_i_io_total;
            }
        }

        /// <summary>
        /// 到資料庫去撈一天的一個DataTable，裡面包含一個yyyy-MM-dd日期及 IOtotal總量
        /// </summary>
        /// <param name="feeno">批價序號</param>
        /// <param name="start">一天時刻的開始日期yyy</param>
        /// <param name="end">一天的結束日期</param>
        /// <returns></returns>
        public DataTable get_cvvh_total_data(string feeno, string start, string end)
        {
            //this.link.DBClose();//找不到原因。//已經開啟一個與這個 command 相關的 datareader 必須先將它關閉。//但下這一行就好了。
            DataTable dt = new DataTable();
            string Str = "SELECT '" + start + "' as TIME,SUM(io_total) as TOTAL FROM CVVH_DTL_DATA WHERE DELETED IS NULL";
            Str += " AND RECORD_ID IN (SELECT RECORD_ID FROM CVVH_MASTER WHERE FEENO = '" + feeno + "'";
            Str += " AND DELETED IS NULL AND TO_CHAR(RECORD_TIME, 'YYYY-MM-DD') IN (";
            Str += "'" + start + "','" + end + "'))";
            Str += " AND DATA_TIME BETWEEN TO_DATE('" + start + " 07:01:00', 'yyyy-mm-dd hh24:mi:ss')";
            Str += " AND TO_DATE('" + end + " 07:00:59', 'yyyy-mm-dd hh24:mi:ss')";
            dt = this.link.DBExecSQL(Str);
            return dt;
        }

        /// <summary>
        /// 組成一個裝有CVVH_TPR_Data型態的List
        /// </summary>
        /// <param name="feeno">批價序號</param>
        /// <param name="start">搜尋的開始日期</param>
        /// <param name="end">搜尋的結束日期</param>
        /// <returns></returns>
        public List<CVVH_TPR_Data> get_cvvh_data_list(string feeno, DateTime Start, DateTime End)
        {
            List<CVVH_TPR_Data> vs_data_list = new List<CVVH_TPR_Data>();
            for(DateTime s = Start;s <= End;s = s.AddDays(1))
            {
                DateTime NextS = s.AddDays(1);
                DataTable dt = get_cvvh_total_data(feeno, s.ToString("yyyy-MM-dd"), NextS.ToString("yyyy-MM-dd"));
                if(dt != null && dt.Rows.Count > 0)
                    vs_data_list.Add(new CVVH_TPR_Data(dt.Rows[0]["TIME"].ToString().Trim(), dt.Rows[0]["TOTAL"].ToString().Trim()));
                else
                    vs_data_list.Add(new CVVH_TPR_Data(s.ToString("yyyy-MM-dd"), ""));
            }
            return vs_data_list;
        }
        #endregion
    }
}