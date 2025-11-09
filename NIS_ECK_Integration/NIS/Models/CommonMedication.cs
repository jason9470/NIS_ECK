using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NIS.Models;
using System.Data;
using System.Text.RegularExpressions;

namespace NIS.Models
{
    public class CommonMedication : DBAction
    {
        public class Confirm
        {
            public string Ordseq { get; set; }
            public string Remark { get; set; }
        }

        public string Get_DrugListSql(string FEE_NO, string UD_TYPE, string BEGIN_DATE, string DC_DATE, string GET_FLAG)
        {
            string sql = "";
            switch (UD_TYPE)
            {
                case "S":  //STAT
                    sql += "UD_TYPE = 'S' AND UD_CIR <> 'ASORDER' AND ";
                    break;
                case "R":  //REG
                    sql += " ((UD_TYPE = 'R' AND UD_CIR <> 'ASORDER') OR (UD_TYPE = 'P' AND UD_CIR NOT LIKE '%PRN%')) AND DRUG_TYPE <> 'V' AND ";
                    break;
                case "P": //PRN
                    sql += " ((UD_TYPE = 'P' AND UD_CIR LIKE '%PRN%') OR UD_CIR = 'ASORDER') AND UD_STATUS IN ('1','2') AND ";
                    //return (sql);
                    break;
                case "V": //PRN
                    sql += " DRUG_TYPE = 'V' AND UD_TYPE = 'R' AND UD_CIR <> 'ASORDER' AND";
                    sql += " (DC_DATE ='' OR DC_DATE IS NULL OR DC_DAY >= '" + DC_DATE + "')";
                    return (sql);
                default:
                    break;
            }

            switch (GET_FLAG)
            {
                case "first":
                    //第一次進入給藥畫面,固定時間區間
                    sql += " (DC_DATE ='' OR DC_DATE IS NULL OR BEGIN_DAY <= '" + DC_DATE + "')";
                    break;
                case "Zquiry":
                    //依輸入的時間查詢
                    sql += " (DC_DATE ='' OR DC_DATE IS NULL OR BEGIN_DAY <= '" + DC_DATE + "')";
                    break;
                case "ZquiryAll":
                    //查詢全部時間
                    sql += " UD_STATUS IN ('1','2','6') ";
                    break;
                //case "C_first":  //取消
                //    sql += " (DC_DATE ='' OR DC_DATE IS NULL OR BEGIN_DAY <= '" + DC_DATE + "' AND DC_DAY >= '" + BEGIN_DATE + "') ";
                //    break;
                //case "C_quiryAll":  //取消
                //    sql += " (DC_DATE ='' OR DC_DATE IS NULL OR BEGIN_DAY <= '" + DC_DATE + "' AND DC_DAY >= '" + BEGIN_DATE + "') ";
                //    break;
                case "quiryAllByUD": //藥師書記用
                    if (BEGIN_DATE != "" || DC_DATE != "")
                        sql = " DC_DATE ='' OR DC_DATE IS NULL OR BEGIN_DAY <= '" + DC_DATE + "' AND DC_DAY >= '" + BEGIN_DATE + "'";
                    break;
                case "quiryIntervalByUD": //藥師書記用
                    sql = " DC_DATE ='' OR DC_DATE IS NULL OR BEGIN_DAY <= '" + DC_DATE + "' AND DC_DAY >= '" + BEGIN_DATE + "'";
                    break;
                default:
                    break;
            }

            return (sql);
        }


        public DataTable Get_DrugListTime(string UD_SEQ, string EXEC_DATE, string BEGIN_DATE, string DC_DATE, string GET_FLAG)
        {
            DataTable dt_drug = new DataTable();
            string sql = "";
            sql += "SELECT * FROM H_DRUG_EXECUTE WHERE UD_SEQ = '" + UD_SEQ + "' AND EXEC_DATE IS NULL AND INVALID_DATE IS NULL ";
            //sql += "SELECT * FROM DRUG_EXECUTE WHERE UD_SEQ = '" + UD_SEQ + "' AND EXEC_DATE IS NULL AND ";
            //sql += "to_date(INVALID_DATE) < to_date('" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + "','YYYY/MM/DD HH24:MI:ss') ";
            switch (GET_FLAG)
            {
                case "first":
                    //未到給藥時間 (VIEW：執行給藥)
                    //sql += "AND DRUG_DATE BETWEEN (sysdate - 8/24) AND (sysdate + 1/24) ";
                    sql += "AND DRUG_DATE <= (sysdate + 1/24) ";
                    break;
                case "Zquiry":
                    //依輸入的時間查詢
                    sql += "AND DRUG_DATE BETWEEN TO_DATE('" + BEGIN_DATE + "','yyyy/mm/dd hh24:mi:ss') AND TO_DATE('" + DC_DATE + "','yyyy/mm/dd hh24:mi:ss') ";
                    break;
                case "ZquiryAll":
                    //查詢全部時間
                    // sql += "UD_STATUS IN ('1','2') ";
                    //sql += "AND  (givedate|| '_'|| givetime)  < to_char(sysdate+1/24,'yyyy/MM/dd_hh24:mm:ss') ";
                    break;
                default:
                    break;
            }

            dt_drug = base.gettable(sql + " ORDER BY DRUG_DATE");
            dt_drug.Columns.Add("use_seq");//使用順序
            dt_drug.Columns.Add("use_date");
            dt_drug.Columns.Add("use_time");//使用時間
            dt_drug.Columns.Add("reason1");
            dt_drug.Columns.Add("reasontype1");
            foreach (DataRow dr in dt_drug.Rows)
            {
                dt_drug.Rows[0]["use_seq"] += dr["UD_SEQPK"].ToString() + ",";
                dt_drug.Rows[0]["use_date"] += DateTime.Parse(dr["DRUG_DATE"].ToString()).ToString("yyyy/MM/dd") + ",";
                dt_drug.Rows[0]["use_time"] += DateTime.Parse(dr["DRUG_DATE"].ToString()).ToString("HH:mm") + ",";
                dt_drug.Rows[0]["reason1"] += dr["REASON"].ToString() + ",";
                dt_drug.Rows[0]["reasontype1"] += dr["REASONTYPE"].ToString() + ",";
            }
            return dt_drug;
        }

        public DataTable Get_DrugListTime_PRN(string UD_SEQ, string type)
        {
            //type : P=PRN,V=點滴
            DataTable dt_drug = new DataTable();
            string sql = "";
            sql += "SELECT A.UD_SEQPK,A.UD_SEQ,A.DRUG_DATE,LPAD(SUBSTR(A.UD_SEQPK,LENGTH(A.UD_SEQPK)-2,3)+1,3,'0') MAXSEQ,";
            if (type == "P")
            {
                sql += "(SELECT COUNT(*) FROM H_DRUG_EXECUTE WHERE UD_SEQ = '" + UD_SEQ + "' AND TO_CHAR(EXEC_DATE,'YYYY/MM/DD') = TO_CHAR(SYSDATE,'YYYY/MM/DD')) EXEC_COUNT ";
            }
            else
            {
                sql += "(SELECT NVL(SUM(USE_DOSE),0) FROM H_DRUG_EXECUTE";
                sql += " WHERE UD_SEQ = '" + UD_SEQ + "' AND TO_CHAR(DRUG_DATE,'YYYY/MM/DD') = TO_CHAR(SYSDATE,'YYYY/MM/DD')) EXEC_COUNT ";
            }
            sql += "FROM H_DRUG_EXECUTE A WHERE UD_SEQ = '" + UD_SEQ + "' AND UD_SEQPK = ";
            sql += "(SELECT MAX(UD_SEQPK) FROM H_DRUG_EXECUTE WHERE UD_SEQ = '" + UD_SEQ + "')";

            dt_drug = base.gettable(sql);
            return dt_drug;
        }

        public DataTable Get_DrugListTime_Cancel(string UD_SEQ, string EXEC_DATE, string BEGIN_DATE, string DC_DATE, string GET_FLAG, string feeno)
        {
            DataTable dt_drug = new DataTable();
            string sql = "", sql2 = "";

            sql2 = "SELECT EXEC_DATE FROM H_DRUG_EXECUTE WHERE FEE_NO='" + feeno + "' AND UD_SEQ = '" + UD_SEQ + "'";
            switch (GET_FLAG)
            {
                case "C_first":
                    sql2 += " AND EXEC_DATE BETWEEN SYSDATE - 8/24 AND SYSDATE + 1/24";
                    break;
                case "C_quiryAll":
                    //依輸入的時間查詢
                    //sql2 += " AND DRUG_DATE BETWEEN '" + BEGIN_DATE + "' AND '" + DC_DATE + "'";
                    sql2 += " AND EXEC_DATE BETWEEN TO_DATE('" + BEGIN_DATE + "','yyyy/mm/dd hh24:mi:ss') AND TO_DATE('" + Convert.ToDateTime(DC_DATE).ToString("yyyy/MM/dd 23:59:59") + "','yyyy/mm/dd hh24:mi:ss') ";
                    break;
                default:
                    break;
            }
            sql = "SELECT * FROM H_DRUG_EXECUTE WHERE FEE_NO='" + feeno + "' AND UD_SEQ = '" + UD_SEQ + "' AND EXEC_DATE IN (" + sql2 + " AND EXEC_DATE IS NOT NULL ) ORDER BY DRUG_DATE";

            dt_drug = base.gettable(sql);
            dt_drug.Columns.Add("use_seq");
            dt_drug.Columns.Add("use_date");
            dt_drug.Columns.Add("use_time");
            dt_drug.Columns.Add("exec_time");
            dt_drug.Columns.Add("exec_id");
            dt_drug.Columns.Add("exec_name");
            dt_drug.Columns.Add("drug_date");
            foreach (DataRow dr in dt_drug.Rows)
            {
                dt_drug.Rows[0]["use_seq"] += dr["UD_SEQPK"].ToString() + ",";
                dt_drug.Rows[0]["drug_date"] += DateTime.Parse(dr["DRUG_DATE"].ToString()).ToString("yyyy/MM/dd HH:mm:ss") + ",";
                dt_drug.Rows[0]["use_date"] += DateTime.Parse(dr["DRUG_DATE"].ToString()).ToString("yyyy/MM/dd") + ",";// 於取消給藥時間的顯示日期 DRUG_DATE 改為EXEC_DATE
                dt_drug.Rows[0]["use_time"] += DateTime.Parse(dr["DRUG_DATE"].ToString()).ToString("HHmm") + ",";
                dt_drug.Rows[0]["exec_time"] += DateTime.Parse(dr["EXEC_DATE"].ToString()).ToString("HHmm") + ",";
                dt_drug.Rows[0]["exec_id"] += dr["EXEC_ID"].ToString() + ",";
                dt_drug.Rows[0]["exec_name"] += dr["EXEC_NAME"].ToString() + ",";
            }
            return dt_drug;
        }

        public string Get_Position(string FeeNo)
        {
            DataTable dt_drug = new DataTable();
            string strName = "";
            string sql = "", strPosition = "('A','E','B','F')";
            string strSelect = "REPLACE(REPLACE(REVIEW,'C',''),'G','')||SUBSTR(REVIEW,1,1)";
            if (Convert.ToInt16(DateTime.Now.ToString("HH")) >= 21 || Convert.ToInt16(DateTime.Now.ToString("HH")) < 6)
            {
                sql = "SELECT COUNT(*) AS COUNT FROM SPECIALDRUG_SET WHERE FEENO = '" + FeeNo + "' AND (INSTR(REVIEW,'C') > 0 OR INSTR(REVIEW,'G') > 0) ";
                sql += " AND INSDT = (SELECT MAX(INSDT) FROM SPECIALDRUG_SET WHERE FEENO = '" + FeeNo + "')";
                dt_drug = base.gettable(sql);
                if (Convert.ToInt16(dt_drug.Rows[0]["COUNT"].ToString()) > 0)
                {   //當21:00後施打部份都未被設拒打時
                    strPosition = "('C','G')";
                    strSelect = "SUBSTR(REVIEW,INSTR(REVIEW,'C'),1)||SUBSTR(REVIEW,INSTR(REVIEW,'G'),1)||SUBSTR(REVIEW,INSTR(REVIEW,'C'),1)||SUBSTR(REVIEW,INSTR(REVIEW,'G'),1)";
                }
            }
            sql = "SELECT SUBSTR(A.REVIEW,INSTR(A.REVIEW,B.PO)+1,1) POSITION FROM ";
            sql += "(SELECT " + strSelect + " REVIEW FROM SPECIALDRUG_SET WHERE FEENO = '" + FeeNo + "' AND INSDT = ";
            sql += "(SELECT MAX(INSDT) FROM SPECIALDRUG_SET WHERE FEENO = '" + FeeNo + "')) A,";
            sql += "(SELECT NVL((SELECT SUBSTR(POSITION,1,1) FROM NIS_MED_INSULIN WHERE FEENO = '" + FeeNo + "' AND SUBSTR(POSITION,1,1) IN " + strPosition + " AND INSDT = ";
            sql += "(SELECT MAX(INSDT) FROM NIS_MED_INSULIN WHERE FEENO = '" + FeeNo + "' AND SUBSTR(POSITION,1,1) IN " + strPosition + ") AND rownum=1),0) PO FROM DUAL) B";
            dt_drug = base.gettable(sql);
            if (dt_drug.Rows.Count == 0)
            {
                if (Convert.ToInt16(DateTime.Now.ToString("HH")) >= 21 || Convert.ToInt16(DateTime.Now.ToString("HH")) < 6)
                    return "G";
                else
                    return "E";
            }
            else
            {
                strName = dt_drug.Rows[0]["POSITION"].ToString().Trim();
            }
            return strName;
        }
        public string Get_Position_set(string FeeNo)
        {
            DataTable dt = new DataTable();
            string strName = "", sql = "";
            sql += "select REVIEW from SPECIALDRUG_SET where FEENO = '" + FeeNo + "' order by indate desc";
            dt = base.gettable(sql);
            if (dt.Rows.Count > 0)
                strName = dt.Rows[0]["REVIEW"].ToString().Trim();
            return strName;
        }
        public DataTable get_QueryExecLog(string feeno, string BEGIN_DATE, string DC_DATE, string ExecFlag)
        {
            string sql = "SELECT * FROM UD_ORDER WHERE FEE_NO = '" + feeno + "' AND UD_TYPE IN ('S','R','P') AND UD_STATUS <> '4' ";

            //依執行項目抓取時間
            if (BEGIN_DATE != "" && DC_DATE != "")
            {
                sql += "AND (DC_DATE IS NULL OR ";
                sql += "TO_DATE(TO_CHAR(TO_NUMBER(DC_DATE)+19110000)||DC_TIME, 'yyyy/MM/dd hh24:mi:ss') < TO_DATE('" + DC_DATE + "','yyyy/mm/dd hh24:mi:ss') ";
                sql += "AND TO_DATE(TO_CHAR(TO_NUMBER(DC_DATE)+19110000)||DC_TIME, 'yyyy/MM/dd hh24:mi:ss') >  TO_DATE('" + BEGIN_DATE + "','yyyy/mm/dd hh24:mi:ss')) ";
            }
            sql += " ORDER BY MED_CODE";
            return base.gettable(sql);
        }

        public DataTable get_QueryExecLogTime(string feeno, string UD_SEQ, string BEGIN_DATE, string DC_DATE, string TypeFlag, string ExecFlag)
        {
            DataTable dt = new DataTable();
            string sql = "";
            //   sql = "SELECT * FROM DRUG_EXECUTE WHERE UD_SEQ = '" + UD_SEQ + "' AND INVALID_DATE IS NULL ";
            //20160727 by yungchen 修改成抓feeno
            //sql = "SELECT * FROM DRUG_EXECUTE WHERE FEE_NO = '" + feeno + "' AND UD_SEQ = '" + UD_SEQ + "'  AND trim(INVALID_DATE) = '0'  ";
            //20160906 mod by yungchen 不要過濾已DC資料
            sql = "SELECT A.*, B.P_NAME FROM H_DRUG_EXECUTE A LEFT JOIN (SELECT * FROM SYS_PARAMS WHERE P_MODEL='common_medication' AND P_GROUP='late_reason') B ON A.REASON=B.P_VALUE WHERE A.FEE_NO = '" + feeno + "' AND A.UD_SEQ = '" + UD_SEQ + "' ";
            if (ExecFlag == "quiryInterval" || ExecFlag == "quiryIntervalByUD")
            {
                //區間時間
                sql += "AND TO_DATE(A.DRUG_DATE,'yyyy-mm-dd AM hh:mi:ss') BETWEEN TO_DATE('" + Convert.ToDateTime(BEGIN_DATE).ToString("yyyy-MM-dd tt hh:mm:ss") + "','yyyy/mm/dd AM hh:mi:ss') AND TO_DATE('" + Convert.ToDateTime(DC_DATE).ToString("yyyy-MM-dd tt hh:mm:ss") + "','yyyy/mm/dd AM hh:mi:ss') ";
            }
            switch (TypeFlag)
            {
                case "all":
                    //全部用藥
                    break;
                case "ok":
                    //已給藥
                    sql += " AND A.EXEC_DATE IS NOT NULL AND (A.REASONTYPE IN ('2','4') OR A.REASONTYPE IS NULL) ";
                    break;
                case "not":
                    //未給藥
                    sql += " AND A.EXEC_DATE IS NULL ";
                    break;
                case "cancel":
                    //取消給藥
                    sql += " AND A.EXEC_DATE IS NOT NULL AND A.REASONTYPE = '1' ";
                    break;
                default:
                    break;
            }
            dt = base.gettable(sql + " ORDER BY TO_DATE(A.DRUG_DATE,'yyyy-mm-dd AM hh:mi:ss')");  //要轉成時間，不然判斷會判成文字，下午會在上午之前
            dt.Columns.Add("use_t");//使用時間
            dt.Columns.Add("use_s");//使用順序
            dt.Columns.Add("use_d");
            dt.Columns.Add("use_execname"); //執行給藥的人
            dt.Columns.Add("use_execdate");   //執行給藥的日期
            dt.Columns.Add("use_exectime");   //執行給藥的時間
            dt.Columns.Add("use_reason");
            //===20140509 覆核人員 by yungchen
            dt.Columns.Add("check_execname"); //執行覆核給藥的人
            dt.Columns.Add("check_execdate");   //執行覆核給藥的日期
            dt.Columns.Add("check_exectime");   //執行覆核給藥的時間
            //====
            dt.Columns.Add("insulin");   //執行覆核給藥的時間

            foreach (DataRow dr in dt.Rows)
            {
                dt.Rows[0]["use_t"] += Convert.ToDateTime(dr["DRUG_DATE"]).ToString("HH:mm") + ",";
                dt.Rows[0]["use_execname"] += dr["EXEC_NAME"].ToString() + " ,";
                string ReasonStr = "";


                if (dr["EXEC_DATE"].ToString() != null && dr["EXEC_DATE"].ToString() != "")
                {
                    dt.Rows[0]["use_execdate"] += Convert.ToDateTime(dr["EXEC_DATE"]).ToString("yyyy/MM/dd") + " ,";
                    dt.Rows[0]["use_exectime"] += Convert.ToDateTime(dr["EXEC_DATE"]).ToString("HH:mm") + " ,";
                }
                else
                {
                    dt.Rows[0]["use_execdate"] += " ,";
                    dt.Rows[0]["use_exectime"] += " ,";
                }
                //=========雙人覆核
                dt.Rows[0]["check_execname"] += dr["CHECKER"].ToString() + " ,";
                if (dr["CHECKER_DATE"].ToString() != null && dr["CHECKER_DATE"].ToString() != "")
                {
                    dt.Rows[0]["check_execdate"] += Convert.ToDateTime(dr["CHECKER_DATE"]).ToString("yyyy/MM/dd") + " ,";
                    dt.Rows[0]["check_exectime"] += Convert.ToDateTime(dr["CHECKER_DATE"]).ToString("HH:mm") + " ,";
                }
                else
                {
                    dt.Rows[0]["check_execdate"] += " ,";
                    dt.Rows[0]["check_exectime"] += " ,";
                }
                //======

                if (string.IsNullOrEmpty(dr["USE_DOSE"].ToString()))//未給藥原因
                {
                    ReasonStr = dr["REASONTYPE"].ToString();
                    dt.Rows[0]["use_reason"] += ReasonStr != "其他" ? "未給予:" + ReasonStr + " ," : "未給予:" + ReasonStr + "：" + dr["NON_DRUG_OTHER"].ToString() + " ,";
                }
                else
                {
                    ReasonStr = dr["REASON"].ToString() != "" ? dr["P_NAME"].ToString() : dr["EARLY_REASON"].ToString();
                    dt.Rows[0]["use_reason"] += ReasonStr != "其他" ? ReasonStr + " ," : ReasonStr + "：" + dr["DRUG_OTHER"].ToString() + " ,";

                }
                //=========胰島素部位
                if (dr["INSULIN_SITE"].ToString() != null && dr["INSULIN_SITE"].ToString() != "" && !string.IsNullOrEmpty(dr["USE_DOSE"].ToString()))
                {
                    dt.Rows[0]["insulin"] += dr["INSULIN_SITE"].ToString() + " ,";
                }
                else
                {
                    dt.Rows[0]["insulin"] += " ,";
                }

                dt.Rows[0]["use_s"] += dr["UD_SEQPK"].ToString() + " ,";
                dt.Rows[0]["use_d"] += Convert.ToDateTime(dr["DRUG_DATE"]).ToString("yyyy/MM/dd") + " ,";
            }
            return dt;
        }

        public bool GetSpecialDrug_Set(string feeno)
        {
            string sql = "";
            sql = "SELECT COUNT(*) CNT FROM SPECIALDRUG_SET WHERE FEENO = '" + feeno + "'";
            DataTable dt = base.gettable(sql);
            if (Convert.ToInt16(dt.Rows[0]["CNT"].ToString()) > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public DataTable get_drugtable()
        {
            DataTable dt = new DataTable();
            string[] UdOrder = { "UD_SEQ","UD_SEQ_OLD","UD_TYPE","UD_STATUS","FEE_NO","CHR_NO","MED_CODE","COST_CODE","BED_NO","PAT_NAME",
                                 "MED_DESC","ALISE_DESC","UD_DOSE","UD_UNIT","UD_CIR","UD_PATH","UD_LIMIT","UD_QTY","PAY_FLAG","PROG_FLAG",
                                 "BEGIN_DATE","BEGIN_TIME","DC_DATE","DC_TIME","UD_CMD","DOC_CODE","SEND_AMT","BACK_AMT","FEE_DATE","FEE_TIME",
                                 "UD_DOSE_TOTAL","BEGIN_DAY","DC_DAY","DoubleCheck","DAY_CNT","DRUG_TYPE","FLOW_SPEED","POSITION"};

            string[] UdOrder_datatype = {"String","String","String","String","String","String","String","String","String","String",
                                        "String","String","String","String","String","String","String","String","String","String",
                                        "String","String","String","String","String","String","String","String","String","String",
                                        "String","String","String","String","String","String","String","String","String"};


            dt = set_dt_column(dt, UdOrder);
            DataRow dr = dt.NewRow();
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                dr[i] = UdOrder_datatype[i];
            }
            dt.Rows.Add(dr);
            return dt;
        }

        protected DataTable set_dt_column(DataTable dt, string[] clumn)
        {
            for (int i = 0; i < clumn.Length; i++)
            {
                dt.Columns.Add(clumn[i]);
            }
            return dt;
        }

        public DataTable ExecSql(string strsql)
        {
            return base.gettable(strsql);
        }

        public DataTable CurrentMedData(string pId, string pDate, string pFeeNo)
        {//目前 ddl 值咏蓁已改為文字，故 TYPE_DESC 目前暫不使用  By wawa
            //因為給藥時間為1~24，當日的0點應該標示在前一日的24點，故從當日01:00開始至隔日的00:59為間隔  by wawa
            //註:20171011 因為瑪雅server不認識AM PM 格式的關係，因此本機跑此SQL會失敗，但恩主公那邊可以運作正常 #歷史包袱 
            //DRUG_EXECUTE TABLE 的DRUG_DATE欄位不得出現PM AM數值  需為上午下午  否則會出錯
            string pDateNext = Convert.ToDateTime(pDate).AddDays(+1).ToString("yyyy/MM/dd");
            //string _str = string.Format("SELECT a.*,CASE a.REASONTYPE WHEN '1' THEN '未執行' WHEN '2' THEN '提早執行' WHEN '3' THEN '延遲執行' END TYPE_DESC,b.P_NAME,CASE NVL(a.REASONTYPE,'emp') WHEN 'emp' THEN a.EXEC_DATE ELSE TO_DATE(a.DRUG_DATE,'yyyy-mm-dd AM hh:mi:ss') END ORDERBY,c.POSITION FROM DRUG_EXECUTE a LEFT JOIN (SELECT * FROM SYS_PARAMS WHERE P_MODEL='common_medication' AND P_GROUP='late_reason') b ON a.REASON=b.P_VALUE LEFT JOIN NIS_MED_INSULIN c ON a.UD_SEQPK = c.INID WHERE TO_DATE(a.DRUG_DATE,'yyyy-mm-dd AM hh:mi:ss') >= TO_DATE('{0} 00:00','yyyy-mm-dd hh24:mi:ss') AND TO_DATE(a.DRUG_DATE,'yyyy-mm-dd AM hh:mi:ss') <= TO_DATE('{0} 23:59','yyyy-mm-dd hh24:mi:ss') AND a.UD_SEQ IN ('{1}') AND a.FEE_NO='{2}' ORDER BY a.UD_SEQ,ORDERBY", pDate, pId, pFeeNo);
            string _str = "";
            _str = "SELECT D.*, E.P_NAME AS INSULIN_POSITION FROM ";
            _str += " (";
            _str += " SELECT TO_CHAR(a.EXEC_DATE,'yyyy-mm-dd AM hh:mi:ss') EXECDTM_CHAR, a.*,CASE a.REASONTYPE WHEN '1' THEN '未執行' WHEN '2' THEN '提早執行' WHEN '3' THEN '延遲執行' END TYPE_DESC,b.P_NAME,";
            _str += " CASE NVL(a.REASONTYPE,'emp') WHEN 'emp' THEN a.EXEC_DATE ELSE TO_DATE(a.DRUG_DATE,'yyyy-mm-dd AM hh:mi:ss') END ORDERBY,c.POSITION";
            _str += " FROM H_DRUG_EXECUTE a LEFT JOIN (SELECT * FROM SYS_PARAMS WHERE P_MODEL='common_medication' AND P_GROUP='late_reason') b ON a.REASON=b.P_VALUE ";
            _str += " LEFT JOIN NIS_MED_INSULIN c ON a.UD_SEQPK = c.INID ";
            _str += " ) D LEFT JOIN (SELECT * FROM SYS_PARAMS WHERE P_MODEL='common_medication' AND P_GROUP='insulin_section') E ON D.POSITION=E.P_VALUE ";
            _str += string.Format(" WHERE ((TO_DATE(D.DRUG_DATE,'yyyy-mm-dd AM hh:mi:ss') >= TO_DATE('{0} 01:00','yyyy-mm-dd hh24:mi:ss') AND TO_DATE(D.DRUG_DATE,'yyyy-mm-dd AM hh:mi:ss') <= TO_DATE('{1} 00:59','yyyy-mm-dd hh24:mi:ss')) ", pDate, pDateNext);
            _str += string.Format(" OR (D.EXECDTM_CHAR IS NOT NULL AND TO_DATE(D.EXECDTM_CHAR,'yyyy-mm-dd AM hh:mi:ss') >= TO_DATE('{0} 01:00','yyyy-mm-dd hh24:mi:ss') AND TO_DATE(D.EXECDTM_CHAR,'yyyy-mm-dd AM hh:mi:ss') <= TO_DATE('{1} 00:59','yyyy-mm-dd hh24:mi:ss'))) ", pDate, pDateNext);
            _str += string.Format(" AND D.UD_SEQ IN ('{0}') AND D.FEE_NO='{1}' ORDER BY D.UD_SEQ,ORDERBY", pId, pFeeNo);

            DataTable MedDt = base.DBExecSQL(_str);
            return MedDt;
        }

        /// <summary>要篩選的資料表</summary>  
        /// <param name="RowFilterStr">篩選條件(ex: actid= 'id123' AND datatype ='1')</param>
        /// <param name="SortStr">要排序的欄位名稱</param>  
        /// <returns>篩選過後的資料表</returns>
        public DataTable FiltData(DataTable DT, string RowFilterStr, string SortStr = "")
        {
            DataTable RTDT = null;
            try
            {
                if (DT != null && DT.Rows.Count > 0)
                {
                    DataView DV = new DataView();
                    DV = DT.DefaultView;
                    DV.RowFilter = RowFilterStr;
                    if (!string.IsNullOrEmpty(SortStr))
                    {
                        DV.Sort = SortStr;
                    }
                    if (DV.Table != null && DV.Table.Rows.Count > 0)
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

        public DataTable InsulinBanOrLastPosition(string pFeeNo, string SelTableName)
        {//胰島素禁打部位(SelTableName=SPECIALDRUG_SET) 或 胰島素最後施打部位(SelTableName=NIS_MED_INSULIN)     
            string _str = string.Format("SELECT * FROM {1} WHERE FEENO='{0}' ORDER BY INDATE DESC", pFeeNo, SelTableName);  //不使用 ROWNUN ，因為 ROWNUN 在禁打部位 NULL 時會抓取錯誤列

            DataTable MedDt = base.DBExecSQL(_str);
            return MedDt;
        }
        public DataTable getDt(string SelTableName)
        {
            DataTable MedDt = base.DBExecSQL(SelTableName);
            return MedDt;
        }

        /// <summary>
        /// 給藥紀錄存檔,組xml文字
        /// </summary>
        /// <param name="pt_name">姓名</param>
        /// <param name="chart_no">病歷號</param>
        /// <param name="sex">性別 (M-男，F-女)</param>
        /// <param name="age">年齡</param>
        /// <param name="bed_no">床號</param>
        /// <param name="admit_date">入院日期(西元年，EX: 20130502)</param>
        /// <param name="admit_time">入院時間(EX:202551)</param>
        /// <param name="NKDA">過敏( WEB_SERVICE : GetAllergyList 取得)</param>
        /// <param name="record_date">給藥日期日期(西元年，EX:2013/05/02 23:00:00)</param>   
        /// <param name="duty_nurse">護理人員(EX:陳琬娸)</param>
        /// <param name="Med_name">給藥名稱</param>
        /// <param name="Med_content">內容</param>
        /// <returns>xml文字</returns>
        public string Med_Get_xml(string pt_name, string chart_no, string sex, string age, string bed_no, string admit_date, string admit_time
            , string NKDA, string duty_nurse, string drug_date, string record_date, string Med_name, string use_dose, string cir,
           string path, string Med_content
            )
        {
            string xml = "<?xml version='1.0' encoding='utf-8'?>";
            xml += "<Document>";
            xml += "<MED_PT_NAME>" + pt_name + "</MED_PT_NAME>";
            xml += "<MED_CHART_NO>" + chart_no + "</MED_CHART_NO>";
            xml += "<MED_SEX>" + sex + "</MED_SEX>";
            xml += "<MED_AGE>" + age + "</MED_AGE>";
            xml += "<MED_BED_NO>" + bed_no + "</MED_BED_NO>";
            xml += "<MED_ADMIT_DATE>" + admit_date + "</MED_ADMIT_DATE>";
            xml += "<MED_ADMIT_TIME>" + admit_time + "</MED_ADMIT_TIME>";
            xml += "<MED_ALLERGEN>" + NKDA + "</MED_ALLERGEN>";

            xml += "<MED_DUTY_NURSE>" + duty_nurse + "</MED_DUTY_NURSE>";//給藥護理人員
            xml += "<MED_GIVE_DATE>" + drug_date + "</MED_GIVE_DATE>";//給藥日期=>應給藥日期
            xml += "<MED_GIVE_TIME>" + record_date + "</MED_GIVE_TIME>";//給藥時間=>實際給藥日期

            xml += "<MED_NAME>" + Med_name + "</MED_NAME>"; //藥名         
            xml += "<MED_GIVE_DOSC>" + use_dose + "</MED_GIVE_DOSC>";//劑量
            xml += "<MED_CIR>" + cir + "</MED_CIR>";//頻次
            xml += "<MED_PATH>" + path + "</MED_PATH>"; //途徑         
            xml += "<MED_CMD>" + Med_content + "</MED_CMD>"; //註記         
            xml += "</Document>";
            return xml;
        }

        /// <summary>
        /// 判斷字串是否主要由中文組成。
        /// </summary>
        /// <param name="text">輸入的字串。</param>
        /// <returns>如果主要由中文組成則返回 true，否則返回 false。</returns>
        public bool IsChinese(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }
            // 匹配大部分常用中文漢字的 Unicode 範圍 ([\u4E00-\u9FFF])
            // 這裡簡單判斷如果字串中有任何一個漢字，就視為中文。
            // 更嚴格的判斷可能需要計算漢字比例。
            return Regex.IsMatch(text, @"[\u4E00-\u9FFF]");
        }

        /// <summary>
        /// 中文姓名去識別化：保留姓氏和名字的最後一個字，中間用星號遮蔽。
        /// 例如：「王小明」 -> 「王O明」，「李華」 -> 「李O」。
        /// </summary>
        /// <param name="name">中文姓名。</param>
        /// <returns>去識別化後的中文姓名。</returns>
        public string MaskChineseName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            if (name.Length <= 2)
            {
                // 兩個字的姓名，例如「李明」，保留姓，遮蔽名字
                return name[0] + new string('O', name.Length - 1);
            }
            else
            {
                // 三個字或更長的姓名，例如「王小明」 -> 「王*明」，「歐陽修文」 -> 「歐**文」
                char firstChar = name[0];
                char lastChar = name[name.Length - 1];
                string middleStars = new string('O', name.Length - 2);
                return $"{firstChar}{middleStars}{lastChar}";
            }
        }

        /// <summary>
        /// 英文姓名去識別化：每個單字保留第一個字母及最後一個字母，其餘部分用星號遮蔽。
        /// 例如：「John Doe」 -> 「J**n D*e」。
        /// </summary>
        /// <param name="name">英文姓名。</param>
        /// <returns>去識別化後的英文姓名。</returns>
        public string MaskEnglishName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            var words = name.Split(' ');
            var maskedWords = words.Select(word =>
            {
                if (word.Length <= 1)
                {
                    return word; // 單個字母的單字不遮蔽，例如 "A"
                }
                return word[0] + new string('*', word.Length - 2) + word[word.Length - 1];
            });
            return string.Join(" ", maskedWords);
        }
    }
}

