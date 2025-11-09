using ClosedXML.Excel;
using NuGet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;

namespace NIS.Models
{
    public class Obstetrics : DBConnector
    {
        #region 產程監測
        /// <summary>
        /// 產程監測
        /// </summary>
        /// <param name="feeno"></param>
        /// <param name="userno"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public DataTable sel_Instantly_Be_Birth(string feeno, string userno, string id, string UC_E = "")
        {
            string sql = "SELECT * FROM OBS_BTHPRC WHERE 0 = 0 ";
            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if (userno != "")
                sql += "AND CREATNO = '" + userno + "' ";
            if (id != "")
                sql += "AND IID IN ('" + id + "') ";
            if (UC_E != "")
                sql += "AND UC_E = '" + UC_E + "' ";

            sql += " AND DELETED IS NULL ORDER BY RECORDTIME DESC";

            return base.DBExecSQL(sql);
        }
        #endregion

        #region 處置片語
        /// <summary>
        /// 處置片語
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        public DataTable sel_SHARED_VITALSIGN_OPTION(string title)
        {
            //產程監測之護理處置
            //產程監測之醫療處置
            //新生兒出生紀錄之急救片語
            string sql = " SELECT * FROM SHARED_VITALSIGN_OPTION where TITLE = '" + title + "' ";
            return base.DBExecSQL(sql);
        }
        #endregion 

        #region 產程監測-用藥紀錄
        /// <summary>
        /// 產程監測-用藥紀錄
        /// </summary>
        /// <param name="feeno"></param>
        /// <param name="id"></param>
        /// <param name="snom"></param>
        /// <returns></returns>
        public DataTable sel_bp_disp(string feeno, string id, string snom = "", bool? status = null)
        {
            string sql = "SELECT * FROM OBS_BPDISP WHERE 0 = 0 ";
            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if (id != "")
                sql += "AND IID IN ('" + id + "') ";
            if (snom != "")
                sql += "AND SNOM = '" + snom + "' ";
            if (status != null)
            {
                var r = status == true ? "0" : "1";
                sql += "AND TM_MED_USE_STAT = '" + r + "' ";
            }

            sql += " ORDER BY CREATTIME DESC";

            return base.DBExecSQL(sql);
        }
        #endregion

        #region 產程監測-用藥紀錄-序號
        /// <summary>
        /// 產程監測-用藥紀錄-序號
        /// </summary>
        /// <param name="feeno"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public DataTable sel_bp_disp_seq(string feeno, string id)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT* FROM(SELECT* FROM OBS_BPDISP ORDER BY CREATTIME desc) WHERE 0 = 0 ";
            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if (id != "")
                sql += "AND IID IN ('" + id + "') ";

            sql += " AND ROWNUM = 1";
            return base.DBExecSQL(sql);
        }
        #endregion

        #region 產程監測-用藥紀錄-秒速
        /// <summary>
        /// 產程監測-用藥紀錄-秒速
        /// </summary>
        /// <param name="feeno"></param>
        /// <param name="id"></param>
        /// <param name="snom"></param>
        /// <returns></returns>
        public DataTable sel_bp_disp_detail(string feeno, string id, string snom)
        {
            DataTable dt = new DataTable();

            string sql = "SELECT* FROM(SELECT* FROM OBS_BPDISP_DETAIL ORDER BY SNOD desc) WHERE 0 = 0 ";
            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if (id != "")
                sql += "AND IID IN ('" + id + "') ";
            if (snom != "")
                sql += "AND SNOM = '" + snom + "' ";

            //sql += " AND ROWNUM = 1";

            return base.DBExecSQL(sql);
        }
        #endregion

        #region 產程監測-用藥紀錄-秒速-序號
        /// <summary>
        /// 產程監測-用藥紀錄-秒速-序號
        /// </summary>
        /// <param name="feeno"></param>
        /// <param name="id"></param>
        /// <param name="snom"></param>
        /// <returns></returns>
        public DataTable sel_bp_disp_detail_seq(string feeno, string id, string snom)
        {
            DataTable dt = new DataTable();

            string sql = "SELECT* FROM(SELECT* FROM OBS_BPDISP_DETAIL ORDER BY CREATTIME desc) WHERE 0 = 0 ";
            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if (id != "")
                sql += "AND IID IN ('" + id + "') ";
            if (snom != "")
                sql += "AND SNOM = '" + snom + "' ";

            sql += " AND ROWNUM = 1";

            return base.DBExecSQL(sql);
        }
        #endregion

        #region 生產史
        /// <summary>
        /// 生產史
        /// </summary>
        /// <param name="feeno"></param>
        /// <returns></returns>
        public DataTable sel_bthhis(string feeno, string iid = "")
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM OBS_BTHHIS WHERE 0 = 0 ";
            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if (iid != "")
                sql += "AND IID = '" + iid + "' ";

            sql += "AND DELETED IS NULL";

            return base.DBExecSQL(sql);
        }
        #endregion

        #region 二、三產程
        /// <summary>
        /// 二、三產程
        /// </summary>
        /// <param name="feeno"></param>
        /// <returns></returns>
        public DataTable sel_bthsta(string feeno = "", DateTime? sDT = null, DateTime? eDT = null, string id = "")
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM OBS_BTHSTA WHERE 0 = 0 ";
            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if (id != "")
                sql += "AND IID = '" + id + "' ";
            if (sDT != null && eDT != null)
                sql += $"AND BIRTH BETWEEN TO_DATE('{sDT.Value.ToString("yyyy-MM-dd HH:mm:ss")}', 'YYYY-MM-DD HH24:MI:SS') AND TO_DATE('{eDT.Value.ToString("yyyy-MM-dd HH:mm:ss")}', 'YYYY-MM-DD HH24:MI:SS') ";

            sql += "AND DELETED IS NULL ORDER BY FEENO";

            return base.DBExecSQL(sql);
        }
        #endregion

        #region C/S原因
        /// <summary>
        /// C/S原因
        /// </summary>
        /// <param name="IID"></param>
        /// <returns></returns>
        public DataTable sel_csreason(string IID = "")
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM OBS_CS WHERE 0 = 0 ";
            if (IID != "" && IID != null)
                sql += "AND IID IN ('" + IID + "') ";

            sql += "AND DELETED IS NULL ORDER BY CSRS_SEQ";

            return base.DBExecSQL(sql);
        }
        #endregion

        #region 腳印單
        /// <summary>
        /// 腳印單
        /// </summary>
        /// <param name="feeno"></param>
        /// <returns></returns>
        public DataTable sel_nbfoot(string feeno, string babyseq = "")
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM OBS_NBFOOT WHERE 0 = 0 ";
            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if (babyseq != "")
                sql += "AND BABY_SEQ = '" + babyseq + "' ";

            sql += "AND DELETED IS NULL ORDER BY BABY_SEQ";

            return base.DBExecSQL(sql);
        }
        #endregion

        #region 陰道塞紗
        /// <summary>
        /// 陰道塞紗
        /// </summary>
        /// <param name="IID"></param>
        /// <returns></returns>
        public DataTable sel_vagyarn(string feeno, string IID = "")
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM OBS_YAGYARN WHERE 0 = 0 ";
            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if (IID != "" && IID != null)
                sql += "AND IID IN ('" + IID + "') ";

            sql += "AND DELETED IS NULL ORDER BY CREATTIME DESC";

            return base.DBExecSQL(sql);
        }
        #endregion

        #region 執行檢查輸入
        /// <summary>
        /// 執行檢查輸入
        /// </summary>
        /// <param name="feeno">批價序號</param>
        /// <param name="chartno">病歷號</param>
        /// <param name="idno">PK_IID</param>
        /// <param name="sDT">開始時間</param>
        /// <param name="eDT">結束時間</param>
        /// <returns></returns>
        public DataTable sel_exechk(string feeno = "", string chartno = "", string idno = "", DateTime? sDT = null, DateTime? eDT = null, string iid = "")
        {
            DataTable dt = new DataTable();
            string sql = "SELECT TO_CHAR(R.RECORDTIME, 'YYYY-MM-DD') as RECORDDAY, R.* FROM OBS_EXECHK R WHERE 0 = 0 ";
            if (feeno != "" && feeno != null)
                sql += "AND R.FEENO = '" + feeno + "' ";
            if (iid != "" && iid != null)
                sql += "AND R.IID = '" + iid + "' ";
            if (chartno != "" && chartno != null)
                sql += "AND R.PP_CHARTNO = '" + chartno + "' ";
            if (idno != "" && idno != null)
                sql += "AND R.PP_ID LIKE '" + idno + "' ";
            if (sDT != null && eDT != null)
                sql += $"AND R.CHECKST BETWEEN TO_DATE('{sDT.Value.ToString("yyyy-MM-dd HH:mm:ss")}', 'YYYY-MM-DD HH24:MI:SS') AND TO_DATE('{eDT.Value.ToString("yyyy-MM-dd HH:mm:ss")}', 'YYYY-MM-DD HH24:MI:SS') ";

            sql += "AND R.DELETED IS NULL ORDER BY R.CHECKST";

            return base.DBExecSQL(sql);
        }
        #endregion

        #region 新生兒出生紀錄
        /// <summary>
        /// 新生兒出生紀錄
        /// </summary>
        /// <param name="feeno"></param>
        /// <returns></returns>
        public DataTable sel_nb(string feeno, string chartno = "", string IID = "", string TempNo = "")
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM OBS_NB WHERE 0 = 0 ";
            if (feeno != "" && feeno != null)
                sql += "AND FEENO = '" + feeno + "' ";
            if (chartno != "" && chartno != null)
                sql += "AND NB_CHARTNO = '" + chartno + "' ";
            if (IID != "" && IID != null)
                sql += "AND IID = '" + IID + "' ";
            if (TempNo != "" && TempNo != null)
                sql += "AND NB_TEMPNO = '" + TempNo + "' ";

            sql += "AND DELETED IS NULL ORDER BY NB_TEMPNO";

            return base.DBExecSQL(sql);
        }
        #endregion

        #region 新生兒出生紀錄-緊急處置
        /// <summary>
        /// 新生兒出生紀錄-緊急處置
        /// </summary>
        /// <param name="feeno"></param>
        /// <param name="chartno"></param>
        /// <param name="IID"></param>
        /// <param name="TempNo"></param>
        /// <returns></returns>
        public DataTable sel_nb_emr(string feeno, string IID = "", string snom = "")
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM OBS_NB_EMR WHERE 0 = 0 ";
            if (feeno != "" && feeno != null)
                sql += "AND FEENO = '" + feeno + "' ";
            if (IID != "" && IID != null)
                sql += "AND IID = '" + IID + "' ";
            if (snom != "" && snom != null)
                sql += "AND SNOM = '" + snom + "' ";

            sql += "AND DELETED IS NULL ORDER BY CREATTIME DESC";

            return base.DBExecSQL(sql);
        }
        #endregion

        #region 新生兒出生紀錄-緊急處置-序號
        /// <summary>
        /// 新生兒出生紀錄-緊急處置-序號
        /// </summary>
        /// <param name="feeno"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public DataTable sel_nb_emr_seq(string feeno, string id)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT* FROM(SELECT* FROM OBS_NB_EMR ORDER BY CREATTIME desc) WHERE 0 = 0 ";
            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if (id != "")
                sql += "AND IID IN ('" + id + "') ";

            sql += " AND ROWNUM = 1";

            return base.DBExecSQL(sql);
        }
        #endregion

        #region 新生兒溝通表
        /// <summary>
        /// 新生兒溝通表
        /// </summary>
        /// <param name="childFeeNo"></param>
        /// <param name="childChartno"></param>
        /// <param name="mother_feeno"></param>
        /// <returns></returns>
        public DataTable sel_nbcha(string childFeeNo, string childChartno = "", string mother_feeno = "", string BABY_SEQ = "")
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM OBS_BABYLINK_DATA WHERE 0 = 0 ";
            if (childFeeNo != "" && childFeeNo != null)
                sql += "AND BABY_FEE_NO = '" + childFeeNo + "' ";
            if (childChartno != "" && childChartno != null)
                sql += "AND BABY_CHART_NO = '" + childChartno + "' ";
            if (mother_feeno != "" && mother_feeno != null)
                sql += "AND MOM_FEE_NO = '" + mother_feeno + "' ";
            if (BABY_SEQ != "" && BABY_SEQ != null)
                sql += "AND BABY_SEQ = '" + BABY_SEQ + "' ";

            sql += " ORDER BY BABY_SEQ";

            return base.DBExecSQL(sql);
        }
        #endregion

        #region 產後護理
        /// <summary>
        /// 產後護理
        /// </summary>
        /// <param name="feeno"></param>
        /// <param name="userno"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public DataTable sel_aft_bth(string feeno, string id, string userno = "")
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM OBS_AFTBTH WHERE 0 = 0 ";
            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if (id != "")
                sql += "AND IID IN ('" + id + "') ";
            if (userno != "")
                sql += "AND UPDNO = '" + userno + "' ";

            sql += " AND DELETED IS NULL ORDER BY RECORDTIME DESC";

            return base.DBExecSQL(sql);
        }
        #endregion

        #region 產後護理-用藥紀錄
        /// <summary>
        /// 產後護理-用藥紀錄
        /// </summary>
        /// <param name="feeno"></param>
        /// <param name="id"></param>
        /// <param name="snom"></param>
        /// <returns></returns>
        public DataTable sel_ab_disp(string feeno, string id, string snom = "", bool? status = null)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM OBS_ABDISP WHERE 0 = 0 ";
            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if (id != "")
                sql += "AND IID IN ('" + id + "') ";
            if (snom != "")
                sql += "AND SNOM = '" + snom + "' ";
            if (status != null)
            {
                var r = status == true ? "0" : "1";
                sql += "AND TM_MED_USE_STAT = '" + r + "' ";
            }

            return base.DBExecSQL(sql);
        }
        #endregion

        #region 產後護理-用藥紀錄-序號
        /// <summary>
        /// 產後護理-用藥紀錄-序號
        /// </summary>
        /// <param name="feeno"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public DataTable sel_ab_disp_seq(string feeno, string id)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT* FROM(SELECT* FROM OBS_ABDISP ORDER BY CREATTIME desc, SNOM desc) WHERE 0 = 0 ";
            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if (id != "")
                sql += "AND IID IN ('" + id + "') ";

            sql += " AND ROWNUM = 1";

            return base.DBExecSQL(sql);
        }
        #endregion

        #region 產後護理-用藥紀錄-秒速
        /// <summary>
        /// 產後護理-用藥紀錄-秒速
        /// </summary>
        /// <param name="feeno"></param>
        /// <param name="id"></param>
        /// <param name="snom"></param>
        /// <returns></returns>
        public DataTable sel_ab_disp_detail(string feeno, string id, string snom)
        {
            DataTable dt = new DataTable();

            string sql = "SELECT* FROM(SELECT* FROM OBS_ABDISP_DETAIL ORDER BY SNOD desc) WHERE 0 = 0 ";
            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if (id != "")
                sql += "AND IID IN ('" + id + "') ";
            if (snom != "")
                sql += "AND SNOM = '" + snom + "' ";

            sql += " AND ROWNUM = 1";

            return base.DBExecSQL(sql);
        }
        #endregion

        #region 產後護理-用藥紀錄-秒速-序號
        /// <summary>
        /// 產後護理-用藥紀錄-秒速-序號
        /// </summary>
        /// <param name="feeno"></param>
        /// <param name="id"></param>
        /// <param name="snom"></param>
        /// <returns></returns>
        public DataTable sel_ab_disp_detail_seq(string feeno, string id, string snom)
        {
            DataTable dt = new DataTable();

            string sql = "SELECT* FROM(SELECT* FROM OBS_ABDISP_DETAIL ORDER BY CREATTIME desc, SNOM desc) WHERE 0 = 0 ";
            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if (id != "")
                sql += "AND IID IN ('" + id + "') ";
            if (snom != "")
                sql += "AND SNOM = '" + snom + "' ";

            sql += " AND ROWNUM = 1";

            return base.DBExecSQL(sql);
        }
        #endregion

        #region 母嬰護理

        /// <summary>
        /// 母嬰護理
        /// </summary>
        /// <param name="feeno"></param>
        /// <param name="userno"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public DataTable sel_Breast_Fedding(string feeno, string userno, string id, string baby_feeno = "", string seq = "")
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM OBS_PATNB WHERE 0 = 0 ";
            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if (baby_feeno != "")
                sql += "AND NB_SEQ_FEENO LIKE '%" + baby_feeno + "%' ";
            if (seq != "")
                sql += "AND NB_SEQ_FEENO LIKE '%" + seq + "%' ";
            if (userno != "")
                sql += "AND CREATNO = '" + userno + "' ";
            if (id != "")
                sql += "AND IID = '" + id + "' ";

            sql += "AND DELETED IS NULL ORDER BY RECORDTIME DESC";

            return base.DBExecSQL(sql);
        }
        #endregion

        #region 配方奶
        /// <summary>
        /// 配方奶
        /// </summary>
        /// <param name="IID"></param>
        /// <returns></returns>
        public DataTable sel_nb_formula(string IID = "")
        {
            string sql = "SELECT * FROM OBS_NBFORMULA WHERE 0 = 0 ";
            if (IID != "" && IID != null)
                sql += "AND IID IN ('" + IID + "') ";

            sql += "AND DELETED IS NULL ORDER BY START_D DESC";

            return base.DBExecSQL(sql);
        }
        #endregion

        #region 配方奶開窗
        /// <summary>
        /// 配方奶開窗
        /// </summary>
        /// <param name="IID"></param>
        /// <returns></returns>
        public DataTable sel_nb_formula_choose(string IID = "")
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM OBS_NBFORMULA WHERE 0 = 0 ";
            if (IID != "" && IID != null)
                sql += "AND IID IN ('" + IID + "') ";

            sql += "AND DELETED IS NULL AND END_D IS NULL ORDER BY START_D DESC";

            return base.DBExecSQL(sql);
        }
        #endregion

        public class milk_INFO
        {
            public string ID { get; set; }
            public string SEQ { get; set; }
            public string HO_ID { get; set; }
            public string HO_NAME { get; set; }
            public string NAME { get; set; }
            public string UNIT { get; set; }
            public string CAPATITY { get; set; }
        }

        #region 配方奶開窗(計價維護版本)_
        /// <summary>
        /// 配方奶開窗
        /// </summary>
        /// <param name="IID"></param>
        /// <returns></returns>
        public DataTable sel_nb_formula_choose_NEW(string IID = "")
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM OBS_NBFORMULA WHERE 0 = 0 ";
            if (IID != "" && IID != null)
                sql += "AND IID IN ('" + IID + "') ";

            sql += "AND DELETED IS NULL AND END_D IS NULL ORDER BY START_D DESC";

            return base.DBExecSQL(sql);
        }
        #endregion

        #region 母嬰同室
        /// <summary>
        /// 母嬰同室
        /// </summary>
        /// <param name="feeno"></param>
        /// <param name="userno"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public DataTable sel_roomin(string userno, string id, string momchartno = "", List<string> babychartno = null, string feeno = "", bool? notEnd = null, string babyfeeno="")
        {
            var CheckDate = DateTime.Now.AddMonths(-6).ToString("yyyy/MM/dd HH:mm:ss");
            string sql = "SELECT * FROM OBS_ROOMIN WHERE 0 = 0  AND (1=1) ";
            List<string> dtlSql = new List<string>();
            if (momchartno != "")
            {
                dtlSql.Add($"PAT_FEENO = '{momchartno}'");
            }
            if (babychartno != null)
            {
                dtlSql.Add($"NB_CHARTNO IN ('" + String.Join("','", babychartno) + "')");
                dtlSql.Add($"NB_CHARTNO_E IN ('" + String.Join("','", babychartno) + "')");
            }

            //if (feeno != "")
            //    sql += "AND FEENO = '" + feeno + "' ";
            if(feeno != "" && babyfeeno != "")
            {
                sql += "AND( FEENO = '" + feeno + "' OR FEENO = '" + babyfeeno + "')";
            }
            else if (feeno!="")
            {
                sql += "AND FEENO = '" + feeno + "' ";
            }
            if (userno != "")
                sql += "AND CREATNO = '" + userno + "' ";
            if (id != "")
                sql += "AND IID = '" + id + "' ";

            if (notEnd == true)
            {
                sql += "AND END_TIME IS NULL ";
                sql += "AND START_TIME > to_date('" + CheckDate + "','yyyy/MM/dd HH24:MI:SS') ";
            }

            sql += "AND DELETED IS NULL ORDER BY START_TIME DESC";

            if (dtlSql.Count > 0)
                sql = sql.Replace("1=1", String.Join(" or ", dtlSql));

            return base.DBExecSQL(sql);
        }
        #endregion

        #region 皮膚接觸
        /// <summary>
        /// 皮膚接觸
        /// </summary>
        /// <param name="feeno"></param>
        /// <param name="userno"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public DataTable sel_sktsk(string userno, string id, string momchartno = "", List<string> babychartno = null, string feeno = "", bool? notEnd = null)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM OBS_SKTSK WHERE 0 = 0  AND (1=1) ";

            List<string> dtlSql = new List<string>();
            if (momchartno != "")
            {
                dtlSql.Add($"PAT_FEENO = '{momchartno}'");
            }
            if (babychartno != null)
            {
                dtlSql.Add($"NB_CHARTNO IN ('" + String.Join("','", babychartno) + "')");
            }

            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";

            if (userno != "")
                sql += "AND CREATNO = '" + userno + "' ";
            if (id != "")
                sql += "AND IID = '" + id + "' ";

            if (notEnd == true)
            {
                sql += "AND END_TIME IS NULL ";
            }

            sql += " AND DELETED IS NULL ORDER BY START_TIME DESC";

            if (dtlSql.Count > 0)
                sql = sql.Replace("1=1", String.Join(" or ", dtlSql));

            return base.DBExecSQL(sql);
        }
        #endregion

        #region 哺餵母乳轉介
        /// <summary>
        /// 哺餵母乳轉介
        /// </summary>
        /// <param name="feeno"></param>
        /// <param name="userno"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public DataTable sel_bredtran(string feeno, string userno, string id, DateTime? sDT = null, DateTime? eDT = null)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM OBS_BREDTRAN WHERE 0 = 0 ";
            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if (userno != "")
                sql += "AND CREATNO = '" + userno + "' ";
            if (id != "")
                sql += "AND IID = '" + id + "' ";
            if (sDT != null && eDT != null)
                sql += $" AND REF_DATE BETWEEN TO_DATE('{sDT.Value.ToString("yyyy-MM-dd HH:mm:ss")}', 'YYYY-MM-DD HH24:MI:SS') AND TO_DATE('{eDT.Value.ToString("yyyy-MM-dd HH:mm:ss")}', 'YYYY-MM-DD HH24:MI:SS') ";
            sql += "AND DELETED IS NULL ORDER BY FEENO, CREATTIME DESC";

            return base.DBExecSQL(sql);
        }
        #endregion

        #region 哺餵母乳轉介-新生兒
        /// <summary>
        /// 哺餵母乳轉介-新生兒
        /// </summary>
        /// <param name="feeno"></param>
        /// <param name="userno"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public DataTable sel_btnb(string feeno, string userno, string id)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM OBS_BTNB WHERE 0 = 0 ";
            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if (userno != "")
                sql += "AND CREATNO = '" + userno + "' ";
            if (id != "")
                sql += "AND IID = '" + id + "' ";

            sql += "AND DELETED IS NULL AND (NB_STA_OTH != '死亡' OR NB_STA_OTH IS NULL ) ORDER BY SQ_OF_NB";

            return base.DBExecSQL(sql);
        }
        #endregion

        #region 愛丁堡產後憂鬱症評估
        /// <summary>
        /// 愛丁堡產後憂鬱症評估
        /// </summary>
        /// <param name="feeno"></param>
        /// <param name="userno"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public DataTable sel_epds(string feeno, string userno, string id)
        {
            string sql = "SELECT * FROM OBS_EPDS WHERE 0 = 0 ";
            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if (userno != "")
                sql += "AND CREATNO = '" + userno + "' ";
            if (id != "")
                sql += "AND IID = '" + id + "' ";

            sql += "AND DELETED IS NULL ORDER BY CREATTIME DESC";

            return base.DBExecSQL(sql);
        }
        #endregion

        #region 新生兒出生7小時觀察記錄
        /// <summary>
        /// 新生兒出生7小時觀察記錄
        /// </summary>
        /// <param name="feeno"></param>
        /// <param name="userno"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public DataTable sel_nb7hr(string feeno, string userno, string id, string sno)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM OBS_NB7HR WHERE 0 = 0 ";
            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if (userno != "")
                sql += "AND CREATNO = '" + userno + "' ";
            if (id != "")
                sql += "AND IID = '" + id + "' ";
            if (sno != "")
                sql += "AND SNO = '" + sno + "' ";

            sql += "AND DELETED IS NULL ORDER BY CREATTIME DESC";

            return base.DBExecSQL(sql);
        }

        #endregion

        #region 嬰兒呼吸暫停記錄
        /// <summary>
        /// 嬰兒呼吸暫停記錄
        /// </summary>
        /// <param name="feeno"></param>
        /// <param name="userno"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public DataTable sel_pibs(string feeno, string userno, string id)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM OBS_PIBS WHERE 0 = 0 ";
            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if (userno != "")
                sql += "AND CREATNO = '" + userno + "' ";
            if (id != "")
                sql += "AND IID = '" + id + "' ";

            sql += "AND DELETED IS NULL ORDER BY CREATTIME DESC";

            return base.DBExecSQL(sql);
        }

        #endregion

        #region 新生兒戒斷系統評估
        /// <summary>
        /// 新生兒戒斷系統評估
        /// </summary>
        /// <param name="feeno"></param>
        /// <param name="userno"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public DataTable sel_nass(string feeno, string userno, string id)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM OBS_NASS WHERE 0 = 0 ";
            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if (userno != "")
                sql += "AND CREATNO = '" + userno + "' ";
            if (id != "")
                sql += "AND IID = '" + id + "' ";

            sql += "AND DELETED IS NULL ORDER BY CREATTIME DESC";

            return base.DBExecSQL(sql);
        }
        #endregion

        #region 新生兒入評
        /// <summary>
        /// 新生兒入評
        /// </summary>
        /// <param name="feeno"></param>
        /// <param name="userno"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public DataTable sel_nbentr(string feeno, string userno, string id)
        {
            string sql = "SELECT * FROM OBS_NBENTR WHERE 0 = 0 ";
            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if (userno != "")
                sql += "AND CREATNO = '" + userno + "' ";
            if (id != "")
                sql += "AND IID = '" + id + "' ";

            sql += "AND DELETED IS NULL ORDER BY CREATTIME DESC";

            return base.DBExecSQL(sql);
        }
        #endregion

        #region 護理紀錄
        /// <summary>
        /// 護理紀錄
        /// </summary>
        /// <param name="feeno"></param>
        /// <param name="userno"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public DataTable sel_carerecord(string feeno, string title, string iid = "")
        {
            string sql = "SELECT * FROM CARERECORD_DATA WHERE 0 = 0 ";

            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";

            if (title != "")
                sql += "AND TITLE = '" + title + "' ";

            if (iid != "")
                sql += "AND CARERECORD_ID = '" + iid + "' ";

            sql += "AND DELETED IS NULL ORDER BY CREATTIME DESC";

            return base.DBExecSQL(sql);
        }
        #endregion

        #region 嬰幼兒入院護理評估單-聯絡人
        /// <summary>
        /// 嬰幼兒入院護理評估單-聯絡人
        /// </summary>
        /// <param name="feeno"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public DataTable sel_babyentr_ct(string feeno, string id = "", string userno = "")
        {
            string sql = "SELECT * FROM OBS_BABYENTR_CT WHERE 0 = 0 ";
            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if (userno != "")
                sql += "AND CREATNO = '" + userno + "' ";
            if (id != "")
                sql += "AND IID = '" + id + "' ";

            sql += "AND DELETED IS NULL ORDER BY CREATTIME DESC";

            return base.DBExecSQL(sql);
        }
        #endregion

        #region 嬰幼兒入院護理評估單-住院經驗
        /// <summary>
        /// 嬰幼兒入院護理評估單-住院經驗
        /// </summary>
        /// <param name="feeno"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public DataTable sel_babyentr_ho(string feeno, string id = "", string userno = "")
        {
            string sql = "SELECT * FROM OBS_BABYENTR_HO WHERE 0 = 0 ";
            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if (userno != "")
                sql += "AND CREATNO = '" + userno + "' ";
            if (id != "")
                sql += "AND IID = '" + id + "' ";

            sql += "AND DELETED IS NULL ORDER BY CREATTIME DESC";

            return base.DBExecSQL(sql);
        }
        #endregion

        #region 嬰幼兒入院護理評估單-手術經驗
        /// <summary>
        /// 嬰幼兒入院護理評估單-手術經驗
        /// </summary>
        /// <param name="feeno"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public DataTable sel_babyentr_sg(string feeno, string id = "", string userno = "")
        {
            string sql = "SELECT * FROM OBS_BABYENTR_SG WHERE 0 = 0 ";
            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if (userno != "")
                sql += "AND CREATNO = '" + userno + "' ";
            if (id != "")
                sql += "AND IID = '" + id + "' ";

            sql += "AND DELETED IS NULL ORDER BY CREATTIME DESC";

            return base.DBExecSQL(sql);
        }
        #endregion

        #region 嬰幼兒入院護理評估單
        /// <summary>
        /// 嬰幼兒入院護理評估單
        /// </summary>
        /// <param name="feeno"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public DataTable sel_babyentr(string feeno, string id = "", string userno = "")
        {
            string sql = "SELECT * FROM OBS_BABYENTR WHERE 0 = 0 ";
            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if (userno != "")
                sql += "AND CREATNO = '" + userno + "' ";
            if (id != "")
                sql += "AND IID = '" + id + "' ";

            sql += "AND DELETED IS NULL ORDER BY CREATTIME DESC";

            return base.DBExecSQL(sql);
        }
        #endregion

        #region 嬰幼兒入院護理評估單-序號
        /// <summary>
        /// 嬰幼兒入院護理評估單-序號
        /// </summary>
        /// <param name="feeno"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public DataTable sel_babyentr_seq(string feeno, string id = "")
        {
            DataTable dt = new DataTable();
            string sql = "SELECT* FROM(SELECT* FROM OBS_BABYENTR ORDER BY CREATTIME desc) WHERE 0 = 0 ";
            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if (id != "")
                sql += "AND IID IN ('" + id + "') ";

            sql += " AND ROWNUM = 1";

            sql += "AND DELETED IS NULL";

            return base.DBExecSQL(sql);
        }

        #endregion

        #region 嬰幼兒/新生兒入院護理評估單-頭部異常
        /// <summary>
        /// 嬰幼兒/新生兒入院護理評估單-頭部異常
        /// </summary>
        /// <param name="feeno"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public DataTable sel_ass_1_1f(string feeno, string type, string id = "", string userno = "")
        {
            string sql = "SELECT * FROM OBS_ASS_1_1F WHERE 0 = 0 ";
            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if (type != "")
                sql += "AND TYPE = '" + type + "' ";
            if (userno != "")
                sql += "AND CREATNO = '" + userno + "' ";
            if (id != "")
                sql += "AND IID = '" + id + "' ";

            sql += "AND DELETED IS NULL ORDER BY SNO";

            return base.DBExecSQL(sql);
        }
        #endregion

        #region 產科核對清單
        /// <summary>
        /// 產科核對清單
        /// </summary>
        /// <param name="feeno"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public DataTable sel_obschk(string feeno, string id = "")
        {
            string sql = "SELECT * FROM OBS_OBSCHK WHERE 0 = 0 ";
            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if (id != "")
                sql += "AND IID = '" + id + "' ";

            sql += "AND DELETED IS NULL ORDER BY CREATTIME DESC";

            return base.DBExecSQL(sql);
        }
        #endregion

        #region 兒科核對清單
        /// <summary>
        /// 兒科核對清單
        /// </summary>
        /// <param name="feeno"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public DataTable sel_pedchk(string feeno, string id = "")
        {
            string sql = "SELECT * FROM OBS_PEDCHK WHERE 0 = 0 ";
            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if (id != "")
                sql += "AND IID = '" + id + "' ";

            sql += "AND DELETED IS NULL ORDER BY CREATTIME DESC";

            return base.DBExecSQL(sql);
        }
        #endregion

        #region 地址開窗
        /// <summary>
        /// 地址開窗
        /// </summary>
        /// <param name="IID"></param>
        /// <returns></returns>
        public DataTable sel_v_area(string AREA_NAME = "", string AREA_NO = "")
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM V_AREA WHERE 0 = 0 ";
            if (AREA_NAME != "" && AREA_NAME != null)
                sql += "AND AREA_NAME LIKE '%" + AREA_NAME + "%'";

            if (AREA_NO != "" && AREA_NO != null)
                sql += "AND AREA_CODE LIKE '%" + AREA_NO + "%'";

            return base.DBExecSQL(sql);
        }
        #endregion

        /*------------------------------------------------------------------------------------*/

        public string sel_Instantly_Time(string feeno, string tabname)
        {
            DataTable dt = new DataTable();

            string sql = "SELECT * FROM ( SELECT " + tabname + " FROM NIS_INSTANTLY_BE_BIRTH WHERE ";
            sql += "FEENO = '" + feeno + "' AND " + tabname + " IS NOT NULL AND DELETED IS NULL ";
            sql += "ORDER BY RECORDTIME DESC) WHERE rownum <= 1 ";

            base.DBExecSQL(sql, ref dt);

            if (dt != null && dt.Rows.Count > 0)
                return Convert.ToDateTime(dt.Rows[0][tabname]).ToString("yyyy/MM/dd HH:mm");
            else
                return "";
        }

        public DataTable sel_Child_Brith(string feeno, string userno, string id)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM NIS_CHILD_BRITH WHERE 0 = 0 ";
            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if (userno != "")
                sql += "AND CREATNO = '" + userno + "' ";
            if (id != "")
                sql += "AND IID = '" + id + "' ";

            sql += "AND DELETED IS NULL ";

            base.DBExecSQL(sql, ref dt);

            return dt;
        }
    }

    #region 報表
    public static class ObstetricsHelper
    {
        /// <summary>
        /// 產生 excel
        /// </summary>
        /// <typeparam name="T">傳入的物件型別</typeparam>
        /// <param name="data">物件資料集</param>
        /// <param name="speficReportName">特別指定標題，會在第一列使用跨欄置中呈現   20200331 add</param>
        /// <returns></returns>
        public static void Export<T>(List<T> data, string ReportName, string speficReportName = "")
        {
            //建立 excel 物件
            XLWorkbook workbook = new XLWorkbook();
            //加入 excel 工作表名
            var sheet = workbook.Worksheets.Add(ReportName);
            //欄位起啟位置
            int colIdx = 1;
            int columnNameRowIndex = -1; // 20200331 add  欄位名稱起始列數
            if (speficReportName == "")
            {
                //沒指定，從第一列就是欄位名稱
                columnNameRowIndex = 1;
            }
            else
                //指定，第二列是欄位名稱，第一列是標題
                columnNameRowIndex = 2;

            //使用 reflection 將物件屬性取出當作工作表欄位名稱
            foreach (var item in typeof(T).GetProperties())
            {
                #region - 可以使用 DescriptionAttribute 設定，找不到 DescriptionAttribute 時改用屬性名稱 -
                DescriptionAttribute description = item.GetCustomAttribute(typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (description != null)
                {
                    //sheet.Cell(1, colIdx++).Value = description.Description;
                    sheet.Cell(columnNameRowIndex, colIdx++).Value = description.Description;
                    continue;
                }
                //sheet.Cell(1, colIdx++).Value = item.Name;
                sheet.Cell(columnNameRowIndex, colIdx++).Value = item.Name;
                #endregion
            }

            //20200331 加上跨欄置中標題
            if (speficReportName != "")
            {
                sheet.Cell(1, 1).Value = speficReportName;
                sheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                sheet.Range(1, 1, 1, colIdx - 1).Row(1).Merge();
            }

            //資料起始列位置
            //int rowIdx = 2;
            int rowIdx = columnNameRowIndex + 1;
            foreach (var item in data)
            {
                //每筆資料欄位起始位置
                int conlumnIndex = 1;
                foreach (var jtem in item.GetType().GetProperties())
                {
                    //將資料內容加上 "'" 避免受到 excel 預設格式影響，並依 row 及 column 填入
                    sheet.Cell(rowIdx, conlumnIndex).Value = string.Concat("'", Convert.ToString(jtem.GetValue(item, null)));
                    conlumnIndex++;
                }
                rowIdx++;
            }

            // sheet.Columns().AdjustToContents();  // Adjust column width
            // sheet.Rows().AdjustToContents();     // Adjust row heights

            //存檔至指定位置
            System.Web.HttpContext.Current.Response.Clear();
            System.Web.HttpContext.Current.Response.Buffer = true;
            System.Web.HttpContext.Current.Response.Charset = "";
            System.Web.HttpContext.Current.Response.HeaderEncoding = System.Text.Encoding.Default;
            System.Web.HttpContext.Current.Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            System.Web.HttpContext.Current.Response.AddHeader("content-disposition", $@"attachment;filename={ReportName}_{DateTime.Now.ToString("yyyyMMdd")}.xlsx");
            using (MemoryStream MyMemoryStream = new MemoryStream())
            {
                workbook.SaveAs(MyMemoryStream);
                MyMemoryStream.WriteTo(System.Web.HttpContext.Current.Response.OutputStream);
            }
            System.Web.HttpContext.Current.Response.Flush();
            System.Web.HttpContext.Current.Response.End();
        }
    }

    public class ProductionExport
    {
        [Description("病歷號")]
        public string ChartNo { set; get; }
        [Description("姓名")]
        public string Name { set; get; }
        [Description("開始時間")]
        public string StartTime { set; get; }
        [Description("結束時間")]
        public string EndTime { set; get; }
        [Description("來源別")]
        public string From { set; get; }
        [Description("檢查項目")]
        public string Item { set; get; }
        [Description("檢查結果")]
        public string Result { set; get; }
        [Description("檢查結果2")]
        public string Result2 { set; get; }
        [Description("動向")]
        public string To { set; get; }
        [Description("建立者")]
        public string Creater { set; get; }
    }

    public class FeedingBreastReferralExport
    {
        [Description("轉介日期")]
        public string TransferDate { set; get; }

        [Description("母親病歷號")]
        public string MomChartNo { set; get; }
        [Description("母親姓名")]
        public string MomName { set; get; }

        [Description("生產日期")]
        public string BirthDate { set; get; }
        [Description("胎數")]
        public int BornCnt { set; get; }


        [Description("新生兒病歷號")]
        public string BabyChartNo { set; get; }
        [Description("新生兒姓名")]
        public string BabyName { set; get; }
        [Description("生產方式")]
        public string BirthType { set; get; }
        [Description("轉介原因")]
        public string Reason { set; get; }
        [Description("產次")]
        public string P { set; get; }
        [Description("回覆日期")]
        public string ReplyDate { set; get; }
        [Description("回覆方式")]
        public string ProcessMethod { set; get; }
        [Description("回覆結果")]
        public string ReplyResult { set; get; }
    }

    public class RoomInExport
    {
        [Description("母親病歷號")]
        public string MomChartNo { set; get; }
        [Description("母親姓名")]
        public string MomName { set; get; }
        [Description("生產週數")]
        public string Gest { set; get; }
        [Description("生產方式")]
        public string BirthType { set; get; }
        [Description("生產時間")]
        public string Birthday { set; get; }
        [Description("親子同室")]
        public string RoomIn { set; get; }
        [Description("結束原因")]
        public string EndResult { set; get; }
        [Description("排除原因")]
        public string ExtraResult { set; get; }
    }

    public class RoomInDetailExport
    {
        [Description("親子同室")]
        public string RoomIn { set; get; }
        [Description("開始時間")]
        public string StartTime { set; get; }
        [Description("結束時間")]
        public string EndTime { set; get; }
        [Description("同室時間")]
        public string RoomInTime { set; get; }
        [Description("母親病歷號")]
        public string MomChartNo { set; get; }
        [Description("母親姓名")]
        public string MomName { set; get; }
        [Description("生產方式")]
        public string BirthType { set; get; }
        [Description("生產時間")]
        public string Birthday { set; get; }
        [Description("新生兒病歷號")]
        public string BabyChartNo { set; get; }
        [Description("新生兒姓名")]
        public string BabyName { set; get; }
        [Description("胎序")]
        public string Seq { set; get; }
        [Description("原因類別")]
        public string ReasonType { set; get; }
        [Description("結束原因")]
        public string EndResult { set; get; }
        [Description("排除原因")]
        public string ExtraResult { set; get; }
        [Description("胎次")]
        public string P { set; get; }
    }

    public class SkinTouchExport
    {
        [Description("新生兒病歷號")]
        public string BabyChartNo { set; get; }
        [Description("新生兒姓名")]
        public string BabyName { set; get; }
        [Description("胎序")]
        public string Seq { set; get; }
        [Description("出生日期時間")]
        public string Birth { set; get; }
        [Description("開始時間")]
        public string StartTime { set; get; }
        [Description("結束時間")]
        public string EndTime { set; get; }
        [Description("接觸時間")]
        public string TouchTime { set; get; }
        [Description("原因類別")]
        public string ReasonType { set; get; }
        [Description("結束原因")]
        public string EndResult { set; get; }
        [Description("母親病歷號")]
        public string MomChartNo { set; get; }
        [Description("母親姓名")]
        public string MomName { set; get; }
        [Description("排除原因")]
        public string ExtraResult { set; get; }
        [Description("新生兒動向")]
        public string BabySent { set; get; }
        [Description("出生方式")]
        public string BirthType { set; get; }
        [Description("活/死產")]
        public string BabyStatus { set; get; }

        [Description("符合")]
        public string Match { set; get; }
    }

    public class SkinTouchDetailExport
    {
        [Description("開始時間")]
        public string StartTime { set; get; }
        [Description("結束時間")]
        public string EndTime { set; get; }
        [Description("接觸時間")]
        public string TouchTime { set; get; }
        [Description("原因類別")]
        public string ReasonType { set; get; }
        [Description("結束原因")]
        public string EndResult { set; get; }
        [Description("母親病歷號")]
        public string MomChartNo { set; get; }
        [Description("母親姓名")]
        public string MomName { set; get; }
        [Description("新生兒病歷號")]
        public string BabyChartNo { set; get; }
        [Description("新生兒姓名")]
        public string BabyName { set; get; }
        [Description("胎序")]
        public string Seq { set; get; }
        [Description("地點")]
        public string Location { set; get; }
        [Description("接觸對象")]
        public string Person { set; get; }
        [Description("活產扣除")]
        public string Exclude { set; get; }
        [Description("產次")]
        public string P { set; get; }
        [Description("資料來源")]
        public string Source { set; get; }
    }

    public class NBExport
    {
        [Description("病歷號(小孩)")]
        public string BabyChartNo { set; get; }
        [Description("姓名(小孩)")]
        public string BabyName { set; get; }
        [Description("母親病歷號")]
        public string MomChartNo { set; get; }
        [Description("母親姓名")]
        public string MomName { set; get; }
        [Description("出生日期")]
        public string Birth { set; get; }
        [Description("出生方式")]
        public string BirthType { set; get; }
        [Description("出生週數")]
        public string GEST { set; get; }
        [Description("體重")]
        public string Weight { set; get; }
        [Description("身高")]
        public string Height { set; get; }
        [Description("胎位")]
        public string FETAL { set; get; }
        [Description("性別")]
        public string Gender { set; get; }
        [Description("活/死產")]
        public string NB_STAT { set; get; }
        [Description("動向")]
        public string CHILD_SENT { set; get; }
        [Description("A/S(一分)")]
        public string RB_AS_1_NUM { set; get; }
        [Description("A/S(五分)")]
        public string RB_AS_5_NUM { set; get; }
        [Description("A/S(十分)")]
        public string RB_AS_10_NUM { set; get; }
        [Description("胎便染色")]
        public string MECONIUM_COLOR { set; get; }
        [Description("急救")]
        public string EMR { set; get; }
        [Description("親子同室狀態")]
        public string RoominStat { set; get; }
        [Description("肌膚接觸")]
        public string SkinTouch { set; get; }
        [Description("哺乳種類")]
        public string Feed { set; get; }
        [Description("出院動向")]
        public string Method { set; get; }
    }

    public class ChildWeightExport
    {
        [Description("病歷號(小孩)")]
        public string BabyChartNo { set; get; }
        [Description("姓名(小孩)")]
        public string BabyName { set; get; }
        [Description("來源")]
        public string Source { set; get; }
        [Description("母親病歷號")]
        public string MomChartNo { set; get; }
        [Description("母親姓名")]
        public string MomName { set; get; }
        [Description("出生日期")]
        public string Birth { set; get; }
        [Description("出生方式")]
        public string BirthType { set; get; }
        [Description("出生週數")]
        public string GEST { set; get; }
        [Description("體重")]
        public string Weight { set; get; }
        [Description("身高")]
        public string Height { set; get; }
        [Description("性別")]
        public string Gender { set; get; }
        [Description("電話")]
        public string Phone { set; get; }
        [Description("出院日期")]
        public string OutTime { set; get; }
        [Description("出院動向")]
        public string Method { set; get; }
        [Description("出院體重")]
        public string OutWeight { set; get; }
    }

    public class SDMExport
    {
        [Description("母親病歷號")]
        public string MomChartNo { set; get; }
        [Description("母親姓名")]
        public string MomName { set; get; }
        [Description("產次")]
        public string P { set; get; }

        [Description("決定實施親子同室的方式(住院)")]
        public string ROOMIN { set; get; }
        [Description("新生兒餵食的選擇(住院)")]
        public string FEED { set; get; }
        [Description("決定實施親子同室的方式(SDM)")]
        public string ROOMIN_SDM { set; get; }
        [Description("新生兒餵食的選擇(SDM)")]
        public string FEED_SDM { set; get; }
    }

    public class MomDetailExport
    {
        [Description("母親病歷號")]
        public string MomChartNo { set; get; }
        [Description("母親姓名")]
        public string MomName { set; get; }
        [Description("高危妊娠")]
        public string GPREG { set; get; }
        [Description("預產期")]
        public string EDC { set; get; }
        [Description("懷孕週數")]
        public string GEST { set; get; }
        [Description("胎次")]
        public string P { set; get; }
        [Description("生產日期")]
        public string Birth { set; get; }
        //add by chih,20200511
        [Description("生產時間")]
        public string BirthTime { set; get; }
        //end add by chih,20200511
        [Description("生產方式")]
        public string BirthType { set; get; }
        [Description("Vacuum")]
        public string Vacuum { set; get; }
        [Description("剖腹產註記")]
        public string VacuumNote { set; get; }
        [Description("嘗試VBAC")]
        public string VBAC { set; get; }
        [Description("妊娠併發症")]
        public string PREG { set; get; }
        [Description("新生兒數")]
        public string NBNumber { set; get; }
        [Description("產傷兒數")]
        public int NBHurtNumber { set; get; }
        [Description("留臍帶血")]
        public string CORDBLOOD { set; get; }
        [Description("無痛分娩")]
        public string EPIDURAL { set; get; }
        [Description("先生陪產")]
        public string PATERNITY { set; get; }
        [Description("先生斷臍")]
        public string CORDCLAMP { set; get; }
        [Description("會陰情形")]
        public string PERINEUM { set; get; }
        [Description("裂傷程度")]
        public string PERI_LAC { set; get; }
        [Description("腹部傷口")]
        public string ABD_WND { set; get; }
        [Description("類固醇施打")]
        public string Steroid { set; get; }
        [Description("SDM哺乳種類")]
        public string SDMFEED { set; get; }
        [Description("住院哺乳種類")]
        public string FEED { set; get; }
        [Description("SDM親子同室")]
        public string SDMROOM_IN { set; get; }
        [Description("住院親子同室")]
        public string ROOM_IN { set; get; }
        [Description("親子同室狀態")]
        public string ROOM_IN_STAT { set; get; }
        [Description("產婦區域")]
        public string Zone { set; get; }
        [Description("產婦入院日期")]
        public string AdmissionDate { set; get; }
        [Description("產婦入院時間")]
        public string AdmissionTime { set; get; }
        [Description("陣痛開始日期")]
        public string PAIN_STDate { set; get; }
        [Description("陣痛開始時間")]
        public string PAIN_STTime { set; get; }
        [Description("宮口全開日期")]
        public string CERFO_Date { set; get; }
        [Description("宮口全開時間")]
        public string CERFO_TIME { set; get; }
        [Description("總破水時間")]
        public string GRuptureTime { set; get; }
        [Description("總時間")]
        public string TotalTime { set; get; }
        [Description("終止妊娠")]
        public string TOP_Reason { set; get; }
    }

    public class FEEDExport
    {
        [Description("出生日期")]
        public string Birth { set; get; }
        [Description("母親病歷號")]
        public string MomChartNo { set; get; }
        [Description("母親姓名")]
        public string MomName { set; get; }
        [Description("病歷號(小孩)")]
        public string BabyChartNo { set; get; }
        [Description("姓名(小孩)")]
        public string BabyName { set; get; }
        [Description("胎序")]
        public string Seq { set; get; }
        [Description("母乳類別")]
        public string Feed { set; get; }
        [Description("扣除原因分析")]
        public string Reason { set; get; }
        //add by chih,20200511
        [Description("小孩類別")]
        public string BrPicu { set; get; }
        // end add by chih,20200511
    }

    public class ListExport
    {
        [Description("序號")]
        public string Seq { set; get; }
        [Description("類別")]
        public string Type { set; get; }
        [Description("公式")]
        public string Formula { set; get; }
        [Description("分子")]
        public int Molecule { set; get; }
        [Description("分母")]
        public int Denominator { set; get; }
        [Description("計算結果")]
        public string Result { set; get; }
    }
    #endregion
}
