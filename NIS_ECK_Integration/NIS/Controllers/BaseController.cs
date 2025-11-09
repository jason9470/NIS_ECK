using Com.Mayaminer;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Presentation;
using Newtonsoft.Json;
using NIS.Data;
using NIS.Models;
using NIS.Models.DBModel;
using NIS.PutJTEMRWsService_WS;
using NIS.UtilTool;
using NIS.WebService;
using NIS.WebService_RCS;
using Org.BouncyCastle.Bcpg.OpenPgp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web.Mvc;

namespace NIS.Controllers
{

    public class BaseController : Controller //,  IDisposable
    {
        DBModel DBModels = new DBModel();

        /// <summary>WebService位置</summary>
        public static string webServiceUrl;

        /// <summary>NIS-WS</summary>
        public Nis webService = new Nis();
        public PutJTEMRWsService _EMRnis = new PutJTEMRWsService();
        public WebServiceRCS ws_rcs = new WebServiceRCS();

        /// <summary>語系檔名稱</summary>
        public static string cultureName;

        /// <summary> 是否拋轉 </summary>
        public string switchAssessmentInto;

        /// <summary> 病人資料 </summary>
        public PatientInfo ptinfo;

        /// <summary> 使用者資料 </summary>
        public UserInfo userinfo;

        /// <summary> 登入使用者清單資料 </summary>
        public List<UserData> user_list;

        /// <summary> 補輸清單 </summary>
        public Complement_List complement_List;

        /// <summary> 是否為除錯模式 </summary>
        public static bool debug_mode;

        /// <summary> 發佈時間戳記 </summary>
        public static string build_date_str;

        private DBConnector link;
        //private PrintController PrintC = new PrintController();
        private bool disposed = false;

        /// <summary>
        /// 建構式
        /// </summary>
        public BaseController()
        {
            this.link = new DBConnector();
            // 讀取服務位置
            webServiceUrl = MvcApplication.iniObj.NisSetting.Connection.WebServiceUrl;
            // 讀取語系
            cultureName = MvcApplication.iniObj.NisSystem.Language;
            //是否拋轉
            switchAssessmentInto = MvcApplication.iniObj.NisSystem.SwitchAssessmentInto;
            //發佈時間戳記
            build_date_str = GetBuildDateStr();
            // 讀取服務位置
            this.webService.Url = MvcApplication.iniObj.NisSetting.Connection.WebServiceUrl;

#if DEBUG
            debug_mode = true;
#else
                debug_mode = false;
#endif
        }

        /// <summary>
        /// 取得發佈時間戳記
        /// </summary>
        /// <returns>string</returns>
        public string GetBuildDateStr()
        {
            Assembly entryAssembly = Assembly.GetExecutingAssembly();
            var fileInfo = new System.IO.FileInfo(entryAssembly.Location);
            DateTime buildDate = fileInfo.LastWriteTime;
            return buildDate.ToString("yyyyMMddHHmmss");
        }

        /// <summary>
        /// 執行前先讀取資料
        /// </summary>
        /// <param name="filterContext"></param>
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            //取得病人資料
            if (Session["PatInfo"] != null)
                this.ptinfo = (PatientInfo)Session["PatInfo"];
            //取得使用者資料
            if (Session["UserInfo"] != null)
                this.userinfo = (UserInfo)Session["UserInfo"];
            //取得補輸清單
            if (Session["Complement_List"] != null)
                this.complement_List = (Complement_List)Session["Complement_List"];
            if (Session["UserDataList"] != null)
                this.user_list = (List<UserData>)Session["UserDataList"];
            base.OnActionExecuting(filterContext);
        }

        /// <summary>
        /// Action執行完畢後執行語系設定
        /// </summary>
        /// <param name="filterContext"></param>
        protected override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            //設定語系
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(cultureName);
            Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentCulture;
            base.OnActionExecuted(filterContext);
        }

        /// <summary>
        /// 產生ID
        /// </summary>
        /// <param name="tablename">資料表名稱</param>
        /// <param name="username">登入者</param>
        /// <param name="feeno">住院號</param>
        /// <param name="variable">變數</param>
        public string creatid(string tablename, string username, string feeno, string variable)
        {
            string id = tablename + "_" + username + "_" + feeno + "_" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + "_" + variable;
            return id;
        }

        /// <summary> 取得生命徵象異常年紀代號 </summary>
        public string get_check_type(PatientInfo ptinfo)
        {
            string type = "a";
            if (ptinfo.Age < 19)
                type = "y";
            if (ptinfo.Age < 12)
                type = "c";
            if (ptinfo.Age < 5)
            {
                type = "t";
                DateTime Birthday = ptinfo.Birthday;
                int totalMonth = DateTime.Now.Year * 12 + DateTime.Now.Month - Birthday.Year * 12 - Birthday.Month;
                if (totalMonth < 18)
                    type = "b";
                if (totalMonth < 1)
                    type = "n";
            }
            return type;
        }

        //VitalSign_取得異常值檢查表
        public DataTable Get_Check_Abnormal_dt()
        {
            DataTable dt = new DataTable();
            try
            {
                link.DBExecSQL("SELECT * FROM NIS_SYS_VITALSIGN_OPTION ", ref dt);
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }
            finally
            {
                this.link.DBClose();
            }

            return dt;
        }

        //VitalSign_儲存時檢查異常值 
        public string Check_Num_Abnormal(string lfn, string hfn, string cvalue, DataTable dt_check)
        {
            string must = "";
            try
            {
                double check_val = 0;
                if (double.TryParse(cvalue.Replace("#", ""), out check_val))
                {
                    foreach (DataRow r in dt_check.Rows)
                    {
                        if (r["MODEL_ID"].ToString() == lfn || r["MODEL_ID"].ToString() == hfn)
                        {
                            if (r["DECIDE"].ToString() == ">")
                            {
                                if (check_val > double.Parse(r["VALUE_LIMIT"].ToString()))
                                    must = "Y";
                            }
                            else if (r["DECIDE"].ToString() == "<")
                            {
                                if (check_val < double.Parse(r["VALUE_LIMIT"].ToString()))
                                    must = "Y";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }
            return must;
        }

        //異常值取顏色
        public string check_abnormal_color(DataTable dt_check, string item, string value)
        {
            string color = "black", tmp_MODEL_ID = "";
            try
            {
                string low_suf = "_l,l,l_a,l_b,l_c,l_c1,l_c11,l_c1-2,l_c3-5,l_d,l_e,l_f,l_low,l_n,l_r,l_s,l_y,ld_a,ld_c17,ld_n,ld_n2,ls_a,ls_c17,ls_n,ls_n2";
                string high_suf = "_h,h,h_a,h_b,h_c,h_c1,h_c11,h_c1-2,h_c3-5,h_d,h_e,h_f,h_high,h_r,h_s,h_y,hd_a,hd_c17,hd_n,hd_n2,hs_a,hs_c17,hs_n,hs_n2";
                string[] lowitems_suffixs = low_suf.Split(','), highitems_suffixs = high_suf.Split(',');
                Boolean isLowItem = false, isHighItem = false;
                if (dt_check != null && dt_check.Rows.Count > 0)
                {
                    foreach (DataRow r in dt_check.Rows)
                    {
                        isLowItem = false; isHighItem = false;
                        tmp_MODEL_ID = r["MODEL_ID"].ToString();

                        foreach (string txt in lowitems_suffixs)
                        {
                            if (tmp_MODEL_ID == item + txt)
                                isLowItem = true;
                        }
                        foreach (string txt in highitems_suffixs)
                        {
                            if (tmp_MODEL_ID == item + txt)
                                isHighItem = true;
                        }

                        if (isLowItem || isHighItem)
                        {
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                if (r["DECIDE"].ToString() == ">=")
                                {
                                    if (double.Parse(value) > double.Parse(r["VALUE_LIMIT"].ToString()))
                                    { color = "red"; }
                                }
                                else if (r["DECIDE"].ToString() == ">")
                                {
                                    if (double.Parse(value) < double.Parse(r["VALUE_LIMIT"].ToString()))
                                    { color = "red"; }
                                }
                                else if (r["DECIDE"].ToString() == "<")
                                {
                                    if (double.Parse(value) < double.Parse(r["VALUE_LIMIT"].ToString()))
                                    { color = "blue"; }
                                }
                                else if (r["DECIDE"].ToString() == "<=")
                                {
                                    if (double.Parse(value) < double.Parse(r["VALUE_LIMIT"].ToString()))
                                    { color = "blue"; }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }

            return color;
        }


        #region 線上/歷史區


        /// <summary>
        /// 一次撈出 CHART_NO 的所有 PROFILE 資料( FEENO, DATA_ZONE )
        /// </summary>
        /// <param name="CHART_NO"></param>
        /// <returns>Patient_DataZones</returns>
        public Dictionary<string, string> get_DataZone(string CHART_NO)
        {
            List<Dictionary<string, string>> Patient_DataZones = new List<Dictionary<string, string>>();
            Dictionary<string, string> DataZones = null;
            DataTable Dt = new DataTable();
            string v_FEENO = "", v_DATA_ZONE = "";
            try
            {
                string sqlstr = "SELECT * FROM CS.PATIENT_PROFILE where CHART_NO ='" + CHART_NO + "' ";
                link.DBExecSQL(sqlstr, ref Dt);
                if (Dt.Rows.Count > 0)
                {
                    DataZones = new Dictionary<string, string>();
                    for (int j = 0; j < Dt.Rows.Count; j++)
                    {
                        v_FEENO = Dt.Rows[j]["FEENO"].ToString();
                        v_DATA_ZONE = Dt.Rows[j]["DATA_ZONE"].ToString();

                        DataZones[v_FEENO] = v_DATA_ZONE;

                        //Patient_DataZones.Add(DataZones);
                    }

                }


            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }
            finally
            {
                this.link.DBClose();
            }
            return DataZones;
        }


        /// <summary>
        /// 將 WEBSERVICE 的出院病人資料塞入 PATIENT_PROFILE
        /// </summary>
        /// <param name="FEENO"></param>
        /// <param name="MBD_DATE"></param>
        public void INSERT_MBD_PROFILE(string FEENO, DateTime MBD_DATE, string CHART_NO = "")
        {
            DataTable Dt = new DataTable();
            string sqlstr = "";
            string _MBD_DATE = Convert.ToDateTime(MBD_DATE).ToString("yyyyMMdd").ToString();
            string _LOG_DATE = DateTime.Now.ToString("yyyyMMdd HH:mm:ss");
            string _ARRANGE_DATE = Convert.ToDateTime(MBD_DATE).AddMonths(1).ToString("yyyyMM").ToString() + "01";
            try
            {
                sqlstr = "SELECT * FROM CS.PATIENT_PROFILE where FEENO ='" + FEENO + "' ";
                link.DBExecSQL(sqlstr, ref Dt);

                if (Dt.Rows.Count > 0)
                {
                    for (int j = 0; j < Dt.Rows.Count; j++)
                    {
                        //strDataZone = Dt.Rows[j]["DATA_ZONE"].ToString();
                    }
                }
                else
                {
                    sqlstr = " INSERT INTO CS.PATIENT_PROFILE VALUES('";
                    sqlstr += FEENO + "' , '" + _MBD_DATE + "' , '" + _ARRANGE_DATE + "' ";
                    sqlstr += " , '" + _LOG_DATE + "', '" + userinfo.EmployeesNo + "', NULL , NULL ";
                    sqlstr += " , 'N' , 'CS' ,'from WS', '" + CHART_NO + "' ) ";
                    link.DBExecSQL(sqlstr);
                }

            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }
            finally
            {
                this.link.DBClose();
            }

        }

        /// <summary>
        /// 申請修改 歷史區資料 : MOVED_STATUS 改成 R ，並且註記申請人　與　FEENO
        /// </summary>
        /// <param name="FEENO"></param>
        public void HISTORY_APPLY_RECOVER(string FEENO)
        {
            string sqlstr = "";
            DataTable Dt_tables = new DataTable();
            DataTable Dt = new DataTable();
            string _LOG_DATE = DateTime.Now.ToString("yyyyMMdd HH:mm:ss");
            String _LOG_USER = userinfo.EmployeesNo;
            Boolean bl_EXESQL = false;
            try
            {
                sqlstr = " UPDATE PATIENT_PROFILE SET MOVED_STATUS ='R' , LOG_USER ='" + _LOG_USER + "', MEMO = '" + _LOG_USER + " 申請 RECOVER ! ', LOG_DATE = '" + _LOG_DATE + "' ";
                sqlstr += " WHERE  MOVED_STATUS ='Y' AND  FEENO IN ('" + FEENO + "')  ";
                bl_EXESQL = link.DBExecSQL(sqlstr, false);

            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }
            finally
            {
                this.link.DBClose();
            }
        }

        #endregion

        /// <summary> 取得查詢日期之轉床資料 </summary>
        /// <param name="date">查詢日期</param>
        public BedTransList get_costcenter(string feeno, DateTime date)
        {
            BedTransList bedtranslist = new BedTransList();
            byte[] BedTransListByteCode = webService.GetBedTransList(feeno);

            if (BedTransListByteCode != null)
            {
                string BedTransListJosnArr = NIS.UtilTool.CompressTool.DecompressString(BedTransListByteCode);
                List<BedTransList> BedTransList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<BedTransList>>(BedTransListJosnArr);
                BedTransList.Sort((x, y) => { return -y.TransDate.CompareTo(x.TransDate); });
                for (int i = 0; i < BedTransList.Count; i++)
                {
                    if (date < Convert.ToDateTime(BedTransList[i].TransDate))
                    {
                        if (BedTransList[i].CostCode != null && BedTransList[i].CostCode != "")
                            bedtranslist = BedTransList[i];
                        i = BedTransList.Count;
                    }
                }
            }
            bedtranslist.BedNo = (bedtranslist.BedNo == null) ? "" : bedtranslist.BedNo.Trim();
            bedtranslist.CostCode = (bedtranslist.CostCode == null) ? "" : bedtranslist.CostCode.Trim();
            bedtranslist.CostDesc = (bedtranslist.CostDesc == null) ? "" : bedtranslist.CostDesc.Trim();
            return bedtranslist;
        }

        /// <summary> 取得最後之轉床資料 </summary>
        /// <param name="date">查詢日期</param>
        public BedTransList get_costcenter(string feeno)
        {
            BedTransList bedtranslist = new BedTransList();
            byte[] BedTransListByteCode = webService.GetBedTransList(feeno);

            if (BedTransListByteCode != null)
            {
                string BedTransListJosnArr = NIS.UtilTool.CompressTool.DecompressString(BedTransListByteCode);
                List<BedTransList> BedTransList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<BedTransList>>(BedTransListJosnArr);
                BedTransList.Sort((x, y) => { return -y.TransDate.CompareTo(x.TransDate); });
                if (BedTransList.Count > 0)
                {
                    if (BedTransList.Count > 1)
                        bedtranslist = BedTransList[BedTransList.Count - 2];
                    else
                        bedtranslist = BedTransList[0];
                }
            }
            bedtranslist.BedNo = (bedtranslist.BedNo == null) ? "" : bedtranslist.BedNo.Trim();
            bedtranslist.CostCode = (bedtranslist.CostCode == null) ? "" : bedtranslist.CostCode.Trim();
            bedtranslist.CostDesc = (bedtranslist.CostDesc == null) ? "" : bedtranslist.CostDesc.Trim();
            return bedtranslist;
        }

        /// <summary> 取得轉床歷程_單一成本中心 </summary>
        public Dictionary<string, List<BED_TRANS>> get_Trans_ByOneStation(string Station, string start_date, string end_date)
        {
            DateTime start = Convert.ToDateTime(start_date);
            DateTime end = Convert.ToDateTime(end_date);
            Dictionary<string, List<BED_TRANS>> MyDic_TDL = new Dictionary<string, List<BED_TRANS>>();
            List<BED_TRANS> TLD = new List<BED_TRANS>(), TLD_temp = null;
            byte[] TransListDetailByteCode = webService.GetBed_Trans(Station, start.ToString("yyyy/MM/dd"), end.ToString("yyyy/MM/dd"));
            if (TransListDetailByteCode != null)
            {
                string TransListDetailJosnArr = NIS.UtilTool.CompressTool.DecompressString(TransListDetailByteCode);
                TLD = Newtonsoft.Json.JsonConvert.DeserializeObject<List<BED_TRANS>>(TransListDetailJosnArr);
                var TLD_Group = TLD.GroupBy((x => x.FEENO)).Select(x => x.First());
                foreach (var item in TLD_Group)
                {
                    TLD_temp = new List<BED_TRANS>();
                    for (int i = 0; i < TLD.Count; i++)
                    {
                        if (item.FEENO.Trim() == TLD[i].FEENO.Trim())
                            TLD_temp.Add(TLD[i]);
                    }
                    MyDic_TDL.Add(item.FEENO.Trim(), TLD_temp);
                }
            }

            return MyDic_TDL;
        }

        public string get_discharge_time(string feeno)
        {
            string time = "", sql = "";
            try
            {
                sql = "SELECT * FROM (";
                sql += "SELECT CREATTIME FROM NIS_SPECIAL_EVENT_DATA WHERE 0 = 0 ";
                if (feeno != "")
                    sql += "AND FEENO = '" + feeno + "'";
                sql += "AND TYPE_ID = '6' ORDER BY CREATTIME DESC ) WHERE rownum <= 1";  //20180731 小安說出院 = 6

                DataTable dt = new DataTable();
                link.DBExecSQL(sql, ref dt);
                if (dt.Rows.Count > 0)
                    time = dt.Rows[0][0].ToString();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), "DBExecSQL", sql, ex);
            }
            finally
            {
                this.link.DBClose();
            }

            return time;
        }
        //取得CVP
        public DataTable Sel_CVP(string feeno, string date)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM (SELECT create_date,VS_ITEM, VS_RECORD FROM H_DATA_VITALSIGN WHERE VS_RECORD IS NOT NULL ";
            sql += "AND FEE_NO = '" + feeno + "' AND CREATE_DATE BETWEEN ";
            sql += "to_date('" + Convert.ToDateTime(date).AddDays(-1).ToString("yyyy/MM/dd 23:59:59") + "','yyyy/mm/dd hh24:mi:ss') ";
            sql += "AND to_date('" + Convert.ToDateTime(date).AddDays(1).ToString("yyyy/MM/dd 00:00:00") + "','yyyy/mm/dd hh24:mi:ss') ";
            // sql += "AND (VS_ITEM = 'cv1' OR VS_ITEM = 'cv2') ORDER BY CREATE_DATE) WHERE rownum <= 3 ";
            sql += "AND (VS_ITEM = 'cv1' OR VS_ITEM = 'cv2') ORDER BY CREATE_DATE) ";
            //DBConnector dbconnector = new DBConnector();
            link.DBExecSQL(sql, ref dt);
            return dt;
        }
        public DataTable Sel_ICP(string feeno, string date)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM (SELECT create_date,VS_ITEM, VS_RECORD FROM H_DATA_VITALSIGN WHERE VS_RECORD IS NOT NULL ";
            sql += "AND FEE_NO = '" + feeno + "' AND CREATE_DATE BETWEEN ";
            sql += "to_date('" + Convert.ToDateTime(date).AddDays(-1).ToString("yyyy/MM/dd 23:59:59") + "','yyyy/mm/dd hh24:mi:ss') ";
            sql += "AND to_date('" + Convert.ToDateTime(date).AddDays(1).ToString("yyyy/MM/dd 00:00:00") + "','yyyy/mm/dd hh24:mi:ss') ";
            // sql += "AND (VS_ITEM = 'ic1' or VS_ITEM = 'ic2') ORDER BY CREATE_DATE) WHERE rownum <= 3 ";
            sql += "AND (VS_ITEM = 'ic1' or VS_ITEM = 'ic2') ORDER BY CREATE_DATE) ";
            //DBConnector dbconnector = new DBConnector();
            link.DBExecSQL(sql, ref dt);
            return dt;
        }
        public DataTable Sel_CPP(string feeno, string date)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM (SELECT create_date,VS_ITEM, VS_RECORD FROM H_DATA_VITALSIGN WHERE VS_RECORD IS NOT NULL ";
            sql += "AND FEE_NO = '" + feeno + "' AND CREATE_DATE BETWEEN ";
            sql += "to_date('" + Convert.ToDateTime(date).AddDays(-1).ToString("yyyy/MM/dd 23:59:59") + "','yyyy/mm/dd hh24:mi:ss') ";
            sql += "AND to_date('" + Convert.ToDateTime(date).AddDays(1).ToString("yyyy/MM/dd 00:00:00") + "','yyyy/mm/dd hh24:mi:ss') ";

            sql += "AND (VS_ITEM = 'cpp') ORDER BY CREATE_DATE) ";
            //DBConnector dbconnector = new DBConnector();
            link.DBExecSQL(sql, ref dt);
            return dt;
        }
        public DataTable Sel_ABP(string feeno, string date)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM (SELECT create_date,VS_ITEM, VS_RECORD FROM H_DATA_VITALSIGN WHERE VS_RECORD IS NOT NULL ";
            sql += "AND FEE_NO = '" + feeno + "' AND CREATE_DATE BETWEEN ";
            sql += "to_date('" + Convert.ToDateTime(date).AddDays(-1).ToString("yyyy/MM/dd 23:59:59") + "','yyyy/mm/dd hh24:mi:ss') ";
            sql += "AND to_date('" + Convert.ToDateTime(date).AddDays(1).ToString("yyyy/MM/dd 00:00:00") + "','yyyy/mm/dd hh24:mi:ss') ";

            sql += "AND (VS_ITEM = 'abp') ORDER BY CREATE_DATE) ";
            //DBConnector dbconnector = new DBConnector();
            link.DBExecSQL(sql, ref dt);
            return dt;
        }
        public DataTable Sel_PCWP(string feeno, string date)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM (SELECT create_date,VS_ITEM, VS_RECORD FROM H_DATA_VITALSIGN WHERE VS_RECORD IS NOT NULL ";
            sql += "AND FEE_NO = '" + feeno + "' AND CREATE_DATE BETWEEN ";
            sql += "to_date('" + Convert.ToDateTime(date).AddDays(-1).ToString("yyyy/MM/dd 23:59:59") + "','yyyy/mm/dd hh24:mi:ss') ";
            sql += "AND to_date('" + Convert.ToDateTime(date).AddDays(1).ToString("yyyy/MM/dd 00:00:00") + "','yyyy/mm/dd hh24:mi:ss') ";

            sql += "AND (VS_ITEM = 'pcwp') ORDER BY CREATE_DATE) ";
            //DBConnector dbconnector = new DBConnector();
            link.DBExecSQL(sql, ref dt);
            return dt;
        }
        public DataTable Sel_ETCO2(string feeno, string date)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM (SELECT create_date,VS_ITEM, VS_RECORD FROM H_DATA_VITALSIGN WHERE VS_RECORD IS NOT NULL ";
            sql += "AND FEE_NO = '" + feeno + "' AND CREATE_DATE BETWEEN ";
            sql += "to_date('" + Convert.ToDateTime(date).AddDays(-1).ToString("yyyy/MM/dd 23:59:59") + "','yyyy/mm/dd hh24:mi:ss') ";
            sql += "AND to_date('" + Convert.ToDateTime(date).AddDays(1).ToString("yyyy/MM/dd 00:00:00") + "','yyyy/mm/dd hh24:mi:ss') ";

            sql += "AND (VS_ITEM = 'etco') ORDER BY CREATE_DATE) ";
            //DBConnector dbconnector = new DBConnector();
            link.DBExecSQL(sql, ref dt);
            return dt;
        }
        public DataTable Sel_GI(string feeno, string date)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM (SELECT create_date,VS_ITEM, VS_RECORD FROM H_DATA_VITALSIGN WHERE VS_RECORD IS NOT NULL ";
            sql += "AND FEE_NO = '" + feeno + "' AND CREATE_DATE BETWEEN ";
            sql += "to_date('" + Convert.ToDateTime(date).AddDays(-1).ToString("yyyy/MM/dd 23:59:59") + "','yyyy/mm/dd hh24:mi:ss') ";
            sql += "AND to_date('" + Convert.ToDateTime(date).AddDays(1).ToString("yyyy/MM/dd 00:00:00") + "','yyyy/mm/dd hh24:mi:ss') ";

            sql += "AND (VS_ITEM = 'gi') ORDER BY CREATE_DATE) ";
            //DBConnector dbconnector = new DBConnector();
            link.DBExecSQL(sql, ref dt);
            return dt;
        }
        public DataTable Sel_GI_J(string feeno, string date)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM (SELECT create_date,VS_ITEM, VS_RECORD FROM H_DATA_VITALSIGN WHERE VS_RECORD IS NOT NULL ";
            sql += "AND FEE_NO = '" + feeno + "' AND CREATE_DATE BETWEEN ";
            sql += "to_date('" + Convert.ToDateTime(date).AddDays(-1).ToString("yyyy/MM/dd 23:59:59") + "','yyyy/mm/dd hh24:mi:ss') ";
            sql += "AND to_date('" + Convert.ToDateTime(date).AddDays(1).ToString("yyyy/MM/dd 00:00:00") + "','yyyy/mm/dd hh24:mi:ss') ";

            sql += "AND (VS_ITEM = 'gi_j') ORDER BY CREATE_DATE) ";
            //DBConnector dbconnector = new DBConnector();
            link.DBExecSQL(sql, ref dt);
            return dt;
        }
        public DataTable Sel_GI_C(string feeno, string date)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM (SELECT create_date,VS_ITEM, VS_RECORD FROM H_DATA_VITALSIGN WHERE VS_RECORD IS NOT NULL ";
            sql += "AND FEE_NO = '" + feeno + "' AND CREATE_DATE BETWEEN ";
            sql += "to_date('" + Convert.ToDateTime(date).AddDays(-1).ToString("yyyy/MM/dd 23:59:59") + "','yyyy/mm/dd hh24:mi:ss') ";
            sql += "AND to_date('" + Convert.ToDateTime(date).AddDays(1).ToString("yyyy/MM/dd 00:00:00") + "','yyyy/mm/dd hh24:mi:ss') ";

            sql += "AND (VS_ITEM = 'gi_c') ORDER BY CREATE_DATE) ";
            //DBConnector dbconnector = new DBConnector();
            link.DBExecSQL(sql, ref dt);
            return dt;
        }
        public DataTable Sel_GI_U(string feeno, string date)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM (SELECT create_date,VS_ITEM, VS_RECORD FROM H_DATA_VITALSIGN WHERE VS_RECORD IS NOT NULL ";
            sql += "AND FEE_NO = '" + feeno + "' AND CREATE_DATE BETWEEN ";
            sql += "to_date('" + Convert.ToDateTime(date).AddDays(-1).ToString("yyyy/MM/dd 23:59:59") + "','yyyy/mm/dd hh24:mi:ss') ";
            sql += "AND to_date('" + Convert.ToDateTime(date).AddDays(1).ToString("yyyy/MM/dd 00:00:00") + "','yyyy/mm/dd hh24:mi:ss') ";

            sql += "AND (VS_ITEM = 'gi_u') ORDER BY CREATE_DATE) ";
            //DBConnector dbconnector = new DBConnector();
            link.DBExecSQL(sql, ref dt);
            return dt;
        }
        public DataTable Sel_SI_N(string feeno, string date)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM (SELECT create_date,VS_ITEM, VS_RECORD FROM H_DATA_VITALSIGN WHERE VS_RECORD IS NOT NULL ";
            sql += "AND FEE_NO = '" + feeno + "' AND CREATE_DATE BETWEEN ";
            sql += "to_date('" + Convert.ToDateTime(date).AddDays(-1).ToString("yyyy/MM/dd 23:59:59") + "','yyyy/mm/dd hh24:mi:ss') ";
            sql += "AND to_date('" + Convert.ToDateTime(date).AddDays(1).ToString("yyyy/MM/dd 00:00:00") + "','yyyy/mm/dd hh24:mi:ss') ";

            sql += "AND (VS_ITEM = 'si_n') ORDER BY CREATE_DATE) ";
            //DBConnector dbconnector = new DBConnector();
            link.DBExecSQL(sql, ref dt);
            return dt;
        }
        public DataTable Sel_SI_B(string feeno, string date)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM (SELECT create_date,VS_ITEM, VS_RECORD FROM H_DATA_VITALSIGN WHERE VS_RECORD IS NOT NULL ";
            sql += "AND FEE_NO = '" + feeno + "' AND CREATE_DATE BETWEEN ";
            sql += "to_date('" + Convert.ToDateTime(date).AddDays(-1).ToString("yyyy/MM/dd 23:59:59") + "','yyyy/mm/dd hh24:mi:ss') ";
            sql += "AND to_date('" + Convert.ToDateTime(date).AddDays(1).ToString("yyyy/MM/dd 00:00:00") + "','yyyy/mm/dd hh24:mi:ss') ";

            sql += "AND (VS_ITEM = 'si_b') ORDER BY CREATE_DATE) ";
            //DBConnector dbconnector = new DBConnector();
            link.DBExecSQL(sql, ref dt);
            return dt;
        }
        public DataTable Sel_SI_R(string feeno, string date)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM (SELECT create_date,VS_ITEM, VS_RECORD FROM H_DATA_VITALSIGN WHERE VS_RECORD IS NOT NULL ";
            sql += "AND FEE_NO = '" + feeno + "' AND CREATE_DATE BETWEEN ";
            sql += "to_date('" + Convert.ToDateTime(date).AddDays(-1).ToString("yyyy/MM/dd 23:59:59") + "','yyyy/mm/dd hh24:mi:ss') ";
            sql += "AND to_date('" + Convert.ToDateTime(date).AddDays(1).ToString("yyyy/MM/dd 00:00:00") + "','yyyy/mm/dd hh24:mi:ss') ";

            sql += "AND (VS_ITEM = 'si_r') ORDER BY CREATE_DATE) ";
            //DBConnector dbconnector = new DBConnector();
            link.DBExecSQL(sql, ref dt);
            return dt;
        }
        public DataTable Sel_SI_S(string feeno, string date)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM (SELECT create_date,VS_ITEM, VS_RECORD FROM H_DATA_VITALSIGN WHERE VS_RECORD IS NOT NULL ";
            sql += "AND FEE_NO = '" + feeno + "' AND CREATE_DATE BETWEEN ";
            sql += "to_date('" + Convert.ToDateTime(date).AddDays(-1).ToString("yyyy/MM/dd 23:59:59") + "','yyyy/mm/dd hh24:mi:ss') ";
            sql += "AND to_date('" + Convert.ToDateTime(date).AddDays(1).ToString("yyyy/MM/dd 00:00:00") + "','yyyy/mm/dd hh24:mi:ss') ";

            sql += "AND (VS_ITEM = 'si_s') ORDER BY CREATE_DATE) ";
            //DBConnector dbconnector = new DBConnector();
            link.DBExecSQL(sql, ref dt);
            return dt;
        }
        public DataTable Sel_SI_O(string feeno, string date)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM (SELECT create_date,VS_ITEM, VS_RECORD FROM H_DATA_VITALSIGN WHERE VS_RECORD IS NOT NULL ";
            sql += "AND FEE_NO = '" + feeno + "' AND CREATE_DATE BETWEEN ";
            sql += "to_date('" + Convert.ToDateTime(date).AddDays(-1).ToString("yyyy/MM/dd 23:59:59") + "','yyyy/mm/dd hh24:mi:ss') ";
            sql += "AND to_date('" + Convert.ToDateTime(date).AddDays(1).ToString("yyyy/MM/dd 00:00:00") + "','yyyy/mm/dd hh24:mi:ss') ";

            sql += "AND (VS_ITEM = 'si_o') ORDER BY CREATE_DATE) ";
            //DBConnector dbconnector = new DBConnector();
            link.DBExecSQL(sql, ref dt);
            return dt;
        }

        /// <summary> 取得轉床歷程_全部成本中心 </summary>
        public Dictionary<string, List<BED_TRANS>> get_Trans_ByAllStation(string Station, string start_date, string end_date)
        {
            DateTime start = Convert.ToDateTime(start_date);
            DateTime end = Convert.ToDateTime(end_date);
            Dictionary<string, List<BED_TRANS>> MyDic_TDL = new Dictionary<string, List<BED_TRANS>>();
            List<BED_TRANS> TLD = new List<BED_TRANS>(), TLD_temp = null;
            byte[] TransListDetailByteCode = webService.GetBed_TransList(Station, start.ToString("yyyy/MM/dd"), end.ToString("yyyy/MM/dd"));
            if (TransListDetailByteCode != null)
            {
                string TransListDetailJosnArr = NIS.UtilTool.CompressTool.DecompressString(TransListDetailByteCode);
                TLD = Newtonsoft.Json.JsonConvert.DeserializeObject<List<BED_TRANS>>(TransListDetailJosnArr);
                var TLD_Group = TLD.GroupBy((x => x.FEENO)).Select(x => x.First());
                foreach (var item in TLD_Group)
                {
                    TLD_temp = new List<BED_TRANS>();
                    for (int i = 0; i < TLD.Count; i++)
                    {
                        if (item.FEENO.Trim() == TLD[i].FEENO.Trim())
                            TLD_temp.Add(TLD[i]);
                    }
                    MyDic_TDL.Add(item.FEENO.Trim(), TLD_temp);
                }
            }

            return MyDic_TDL;
        }

        #region 護理紀錄


        /// <summary>
        /// 新增護理紀錄_紅字
        /// </summary>
        /// <param name="time">紀錄時間</param>
        /// <param name="id">P_KEY</param>
        /// <param name="title">標題(可為空值)</param>
        /// <param name="C">內容:一般(可為空值)</param>
        /// <param name="S">內容:主觀(可為空值)</param>
        /// <param name="O">內容:客觀(可為空值)</param>
        /// <param name="I">內容:執行(可為空值)</param>
        /// <param name="E">內容:評值(可為空值)</param>
        /// <param name="self">此筆記錄可於護理紀錄修改傳入(CARERECORD) 不可修改傳入 table name</param>
        public int Insert_CareRecord(string time, string id, string title, string C, string S, string O, string I, string E, string self, string historyID = "")
        {
            int erow = 0;
            if (Session["PatInfo"] != null)
            {
                string userno = userinfo.EmployeesNo;
                string feeno = ptinfo.FeeNo;
                DateTime NowTime = DateTime.Now;
                LogTool lt = new LogTool();
                CareRecord care_record_m = new CareRecord();
                string sign_userno = care_record_m.sel_guide_userno(userno, Convert.ToDateTime(time), Convert.ToDateTime(time).Hour);
                try
                {
                    List<DBItem> insertDataList = new List<DBItem>();
                    insertDataList.Add(new DBItem("CARERECORD_ID", id, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("GUIDE_NO", sign_userno, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATTIME", NowTime.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("RECORDTIME", time, DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("TITLE", trans_date(title), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("C_OTHER", trans_date(C), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("S_OTHER", trans_date(S), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("O_OTHER", trans_date(O), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("I_OTHER", trans_date(I), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("E_OTHER", trans_date(E), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("SELF", self, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("SIGN", "N", DBItem.DBDataType.String));
                    if (!string.IsNullOrEmpty(historyID))
                    {
                        insertDataList.Add(new DBItem("CP_HISTORYID", historyID, DBItem.DBDataType.String));
                    }
                    erow = link.DBExecInsert("CARERECORD_DATA", insertDataList);
                }
                catch (Exception ex)
                {
                    string detail_Str = "\r\n CARERECORD_ID:" + ValReplaceNullToStr(id) + "\r\n CREATNO:" + ValReplaceNullToStr(userno) + "\r\n GUIDE_NO:" + ValReplaceNullToStr(sign_userno) + " \r\n ";
                    detail_Str += "CREATNAME:" + ValReplaceNullToStr(userinfo.EmployeesName) + "\r\n CREATTIME:" + ValReplaceNullToStr(NowTime.ToString("yyyy/MM/dd HH:mm")) + "\r\n RECORDTIME:" + ValReplaceNullToStr(time);
                    detail_Str += "\r\n FEENO:" + ValReplaceNullToStr(feeno) + "\r\n TITLE:" + ValReplaceNullToStr(title) + "\r\n C:" + ValReplaceNullToStr(C) + "\r\n S:" + ValReplaceNullToStr(S) + "\r\n O:" + ValReplaceNullToStr(O);
                    detail_Str += "\r\n I:" + ValReplaceNullToStr(I) + "\r\n E:" + ValReplaceNullToStr(E);
                    lt.saveLogMsg(ex.Message + detail_Str, "CareRecord_Log_Red");
                }
                if (erow > 0)
                {
                    #region --EMR--
                    try
                    {
                        string msg = "";
                        if (trans_date(C) != "")
                            msg += trans_date(C) + "\n";// + trans_date(C)
                        if (trans_date(S) != "")
                            msg += "S:" + trans_date(S) + "\n";// + trans_date(S) 
                        if (trans_date(O) != "")
                            msg += "O:" + trans_date(O) + "\n";//+ trans_date(O) 
                        if (trans_date(I) != "")
                            msg += "I:" + trans_date(I) + "\n";// + trans_date(I) 
                        if (trans_date(E) != "")
                            msg += "E:" + trans_date(E) + "\n";// + trans_date(E)


                        erow = EMR_Sign(time, id, msg, title, self, "");
                        //批次
                        //Assess ass_m = new Assess();
                        //var shift = "";
                        //string sqlstr = "";
                        //DateTime record = Convert.ToDateTime(time);
                        //int strtime = int.Parse(record.ToString("HHmm"));
                        //if (strtime >= 0 && strtime <= 759)
                        //    shift = "N";
                        //else if (strtime >= 800 && strtime <= 1559)
                        //    shift = "D";
                        //else if (strtime >= 1600 && strtime <= 2359)
                        //    shift = "E";

                        //List<DBItem> insertDataList = new List<DBItem>();

                        //查詢今日的包

                        // sqlstr += "SELECT * FROM NIS_EMR_PACKAGE_MST WHERE SHIFT = '" + shift + "' AND CREATE_DTM BETWEEN TO_DATE('" + record.ToString("yyyy/MM/dd 00:00:00") + "', 'yyyy/MM/dd hh24:mi:ss') AND TO_DATE('" + record.ToString("yyyy/MM/dd 23:59:59") + "', 'yyyy/MM/dd hh24:mi:ss') AND FEENO = '" + feeno + "'";

                        //DataTable DtP = link.DBExecSQL(sqlstr);

                        //有包則新增明細
                        //if (DtP.Rows.Count > 0)
                        //{
                        //    var P_ID = DtP.Rows[0]["PAC_ID"].ToString().Trim();
                        //    var PD_ID = "PD_" + feeno + "_" + NowTime.ToString("yyyyMMddHHmmfff");

                        //    insertDataList.Add(new DBItem("PACD_ID", PD_ID, DBItem.DBDataType.String));
                        //    insertDataList.Add(new DBItem("PAC_ID", P_ID, DBItem.DBDataType.String));
                        //    insertDataList.Add(new DBItem("RECORD_KEY", id, DBItem.DBDataType.String));
                        //    insertDataList.Add(new DBItem("RECORD_DATE", time, DBItem.DBDataType.DataTime));
                        //    insertDataList.Add(new DBItem("FOCUS", trans_date(title), DBItem.DBDataType.String));
                        //    insertDataList.Add(new DBItem("CONTENT", msg, DBItem.DBDataType.String));
                        //    insertDataList.Add(new DBItem("HAS_EKG", "N", DBItem.DBDataType.String));
                        //    insertDataList.Add(new DBItem("CREATE_ID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                        //    insertDataList.Add(new DBItem("CREATE_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                        //    insertDataList.Add(new DBItem("CREATE_DATE", NowTime.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                        //    insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                        //    erow = link.DBExecInsert("NIS_EMR_PACKAGE_DTL", insertDataList);
                        //    insertDataList.Clear();
                        //    insertDataList.Add(new DBItem("SIGN_STATUS", "U", DBItem.DBDataType.String));

                        //    erow = ass_m.DBExecUpdate("NIS_EMR_PACKAGE_MST", insertDataList, " PAC_ID = '" + P_ID + "' ");
                        //}
                        //無包新增主檔
                        //else
                        //{
                        //MST
                        //var P_ID = "P_" + feeno + "_" + NowTime.ToString("yyyyMMddHHmmfff");
                        //insertDataList.Add(new DBItem("PAC_ID", P_ID, DBItem.DBDataType.String));
                        //insertDataList.Add(new DBItem("CHART_NO", this.ptinfo.ChartNo, DBItem.DBDataType.String));
                        //insertDataList.Add(new DBItem("FEENO", this.ptinfo.FeeNo, DBItem.DBDataType.String));
                        //insertDataList.Add(new DBItem("SHIFT", shift, DBItem.DBDataType.String));
                        //insertDataList.Add(new DBItem("CREATE_DTM", time, DBItem.DBDataType.DataTime));
                        //insertDataList.Add(new DBItem("CAREGIVER_ID", "", DBItem.DBDataType.String));
                        //insertDataList.Add(new DBItem("CAREGIVER_NAME", userinfo.EmployeesNo, DBItem.DBDataType.String));
                        //insertDataList.Add(new DBItem("GUIDE_ID", userinfo.Guider, DBItem.DBDataType.String));
                        //insertDataList.Add(new DBItem("GUIDE_NAME", NowTime.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                        //insertDataList.Add(new DBItem("REP_ID", userinfo.EmployeesName, DBItem.DBDataType.String));
                        //insertDataList.Add(new DBItem("REP_NAME", NowTime.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                        //    insertDataList.Add(new DBItem("SIGNER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                        //    insertDataList.Add(new DBItem("SIGNER_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                        //    insertDataList.Add(new DBItem("SIGN_STATUS", "U", DBItem.DBDataType.String));
                        //    int pkerow = link.DBExecInsert("NIS_EMR_PACKAGE_MST", insertDataList);

                        //    if(pkerow > 0)
                        //    {
                        //        //DTL
                        //        insertDataList.Clear();
                        //        var PD_ID = "PD_" + feeno + "_" + NowTime.ToString("yyyyMMddHHmmfff");
                        //        insertDataList.Add(new DBItem("PACD_ID", PD_ID, DBItem.DBDataType.String));
                        //        insertDataList.Add(new DBItem("PAC_ID", P_ID, DBItem.DBDataType.String));
                        //        insertDataList.Add(new DBItem("RECORD_KEY", id, DBItem.DBDataType.String));
                        //        insertDataList.Add(new DBItem("RECORD_DATE", time, DBItem.DBDataType.DataTime));
                        //        insertDataList.Add(new DBItem("FOCUS", trans_date(title), DBItem.DBDataType.String));
                        //        insertDataList.Add(new DBItem("CONTENT", msg, DBItem.DBDataType.String));
                        //        insertDataList.Add(new DBItem("HAS_EKG", "N", DBItem.DBDataType.String));
                        //        insertDataList.Add(new DBItem("CREATE_ID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                        //        insertDataList.Add(new DBItem("CREATE_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                        //        insertDataList.Add(new DBItem("CREATE_DATE", NowTime.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                        //        insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                        //        erow = link.DBExecInsert("NIS_EMR_PACKAGE_DTL", insertDataList);
                        //    }

                        //}              
                        //SaveEMRLogData(id + self, GetMd5Hash(id + self), "EMR", RecordTime, "A000040" + GetMd5Hash(id + self) + Temp_NowTime_Str, xml);
                        //SaveEMRLogData(id + self, GetMd5Hash(id + self), "Temp", RecordTime, Temp_NowTime_Str + "-" + GetMd5Hash(id + self), EmrXmlString);
                    }
                    catch (Exception ex)
                    {
                        lt.saveLogMsg(ex.Message, "EMR_Pre_Operation_Log");
                    }
                    #endregion
                }
                //20160223 簽章先拿掉 方便測試
                //////if (erow > 0)
                //////{
                //////    //將紀錄回寫至 EMR Temp Table
                //////    string sqlstr = "begin P_NIS_EMRMS('" + feeno + "','014','護理紀錄單','" + id + self + "','" + time + "','" + sign_userno + "','I');end;";
                //////    dbconnector.DBExec(sqlstr);
                #region JAG 簽章
                //////    // 20150608 EMR
                //////    string strPath = @"C:\\wkhtmltopdf\\wkhtmltopdf.exe";
                //////    string filename = @"C:\inetpub\NIS\Images\" + id + self+".pdf";

                //////    string port = System.Web.HttpContext.Current.Request.ServerVariables["SERVER_PORT"];
                //////    if (port == null || port == "80" || port == "443")
                //////        port = "";
                //////    else
                //////        port = ":" + port;

                //////    string protocol = System.Web.HttpContext.Current.Request.ServerVariables["SERVER_PORT_SECURE"];
                //////    if (protocol == null || protocol == "0")
                //////        protocol = "http://";
                //////    else
                //////        protocol = "https://";

                //////    string sOut = protocol + System.Web.HttpContext.Current.Request.ServerVariables["SERVER_NAME"] + port + System.Web.HttpContext.Current.Request.ApplicationPath;

                //////    if (sOut.EndsWith("/"))
                //////    {
                //////        sOut = sOut.Substring(0, sOut.Length - 1);
                //////    }

                //////    string url = sOut + "/CareRecord/List_PDF?id=" + id + self + "&feeno=" + feeno;
                //////    Process p = new Process();
                //////    p.StartInfo.FileName = strPath;
                //////    p.StartInfo.Arguments = url + " " + filename;
                //////    p.StartInfo.UseShellExecute = true;
                //////    p.Start();
                //////    p.WaitForExit();

                //////    NIS.WebService.Nis nis = new WebService.Nis();
                //////    byte[] listByteCode = nis.UserName(userinfo.EmployeesNo);
                //////    string listJsonArray = CompressTool.DecompressString(listByteCode);
                //////    UserInfo user_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                //////    string emp_id = (user_info.UserID != null) ? user_info.UserID.Trim() : "";
                //////    string emp_name = (user_info.EmployeesName != null) ? user_info.EmployeesName.Trim() : "";
                //////    string dep_no = ptinfo.DeptNo;
                //////    string chr_no = ptinfo.ChartNo;
                //////    string pat_name = ptinfo.PatientName;
                //////    string in_date = ptinfo.InDate.ToString("yyyyMMdd");
                //////    string chagre_type = (ptinfo.PayInfo == "健保") ? "001" : "000";
                //////    int result = emr_sign(id + self, feeno, "014", userno, emp_name, emp_id, dep_no, chr_no, pat_name, in_date, chagre_type, filename);
                #endregion
                //////}
            }

            return erow;
        }

        public int Insert_CareRecordTns(string time, string id, string title, string C, string S, string O, string I, string E, string self, ref DBConnector link)
        {
            int erow = 0; string EMRid = "";
            try
            {
                if (Session["PatInfo"] != null)
                {
                    CareRecord care_record_m = new CareRecord();
                    string sign_userno = care_record_m.sel_guide_userno(this.userinfo.EmployeesNo, Convert.ToDateTime(time), Convert.ToDateTime(time).Hour);
                    DateTime NowTime = DateTime.Now;
                    string feeno = ptinfo.FeeNo;
                    LogTool lt = new LogTool();
                    List<DBItem> insertDataList = new List<DBItem>();
                    insertDataList.Add(new DBItem("CARERECORD_ID", id, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATNO", this.userinfo.EmployeesNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("GUIDE_NO", sign_userno, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATTIME", NowTime.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("RECORDTIME", time, DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("FEENO", this.ptinfo.FeeNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("TITLE", trans_date(title), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("C_OTHER", trans_date(C), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("S_OTHER", trans_date(S), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("O_OTHER", trans_date(O), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("I_OTHER", trans_date(I), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("E_OTHER", trans_date(E), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("SELF", self, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("SIGN", "N", DBItem.DBDataType.String));
                    erow = link.DBExecInsertTns("CARERECORD_DATA", insertDataList);
                    #region --EMR--
                    //try
                    //{

                    //    byte[] tempByte = webService.GetAllergyList(this.ptinfo.FeeNo);
                    //    string allergyDesc = string.Empty, msg = string.Empty;
                    //    string RecordTime = NowTime.ToString("yyyy/MM/dd HH:mm:ss");
                    //    string Temp_NowTime_Str = Convert.ToDateTime(RecordTime).ToString("yyyyMMddHHmmss");//時間採統一變數
                    //    if (tempByte != null)
                    //    {
                    //        List<NIS.Data.PatientInfo> patList = JsonConvert.DeserializeObject<List<NIS.Data.PatientInfo>>(CompressTool.DecompressString(tempByte));
                    //        allergyDesc = patList[0].AllergyDesc;
                    //    }

                    //    if (trans_date(C) != "")
                    //        msg += trans_date(C) + "\n";
                    //    if (trans_date(S) != "")
                    //        msg += "S:" + trans_date(S) + "\n";
                    //    if (trans_date(O) != "")
                    //        msg += "O:" + trans_date(O) + "\n";
                    //    if (trans_date(I) != "")
                    //        msg += "I:" + trans_date(I) + "\n";
                    //    if (trans_date(E) != "")
                    //        msg += "E:" + trans_date(E) + "\n";

                    //    string xml = care_record_m.care_Record_Get_xml(this.ptinfo.PatientName, this.ptinfo.ChartNo,
                    //    this.ptinfo.PatientGender, (this.ptinfo.Age).ToString(), this.ptinfo.BedNo, this.ptinfo.InDate.ToString("yyyyMMdd"),
                    //    this.ptinfo.InDate.ToString("HHmm"), allergyDesc, Convert.ToDateTime(time).ToString("yyyyMMdd"),
                    //    Convert.ToDateTime(time).ToString("HHmm"), userinfo.EmployeesName, trans_date(title), msg);
                    //    SaveEMRLogData(id + self, GetMd5Hash(id + self), "EMR", RecordTime, "A000040" + GetMd5Hash(id + self) + Temp_NowTime_Str, xml);

                    //    //取得應簽章人員
                    //    tempByte = webService.UserName(userinfo.Guider);
                    //    if (tempByte != null)
                    //    {
                    //        string listJsonArray = CompressTool.DecompressString(tempByte);
                    //        UserInfo user_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                    //        string EmrXmlString = this.get_xml(
                    //            NowTime.ToString("yyyyMMddHHmmss.fffffff"), Temp_NowTime_Str + GetMd5Hash(id + self), "A000040", Convert.ToDateTime(time).ToString("yyyyMMdd"),
                    //            GetMd5Hash(id + self), Convert.ToDateTime(time).ToString("yyyyMMdd"), "", "",
                    //            user_info.EmployeesNo, user_info.EmployeesName, user_info.UserID, ptinfo.ChartNo, ptinfo.PatientName,
                    //            ptinfo.PatientID, ptinfo.PayInfo,
                    //            "C:\\EMR\\", "A000040" + GetMd5Hash(id + self) + Temp_NowTime_Str + ".xml", listJsonArray, "Insert_CareRecordTns"
                    //            );
                    //        SaveEMRLogData(id + self, GetMd5Hash(id + self), "Temp", RecordTime, Temp_NowTime_Str + "-" + GetMd5Hash(id + self), EmrXmlString);
                    //        EMRid = id + self;
                    //    }
                    //    else
                    //    {
                    //        lt.saveLogMsg(userinfo.EmployeesNo + "_" + userinfo.Guider + "_" + id + self + "_" + GetMd5Hash(id + self), "EMRLogTns");
                    //    }
                    //}
                    //catch (Exception ex)
                    //{
                    //    lt.saveLogMsg("EMRid= \t" + EMRid + "\t" + ex.Message.ToString(), "EMRLog");

                    //}
                    try
                    {

                        string msg = "";

                        if (trans_date(C) != "")
                            msg += trans_date(C) + "\n";// + trans_date(C)
                        if (trans_date(S) != "")
                            msg += "S:" + trans_date(S) + "\n";// + trans_date(S) 
                        if (trans_date(O) != "")
                            msg += "O:" + trans_date(O) + "\n";//+ trans_date(O) 
                        if (trans_date(I) != "")
                            msg += "I:" + trans_date(I) + "\n";// + trans_date(I) 
                        if (trans_date(E) != "")
                            msg += "E:" + trans_date(E) + "\n";// + trans_date(E)

                        erow = EMR_Sign(time, id, msg, title, self, "");

                        //SaveEMRLogData(id + "CARERECORD", GetMd5Hash(id + "CARERECORD"), "EMR", RecordTime, "A000040" + GetMd5Hash(id + "CARERECORD") + Temp_NowTime_Str, xml);
                        //SaveEMRLogData(id + "CARERECORD", GetMd5Hash(id + "CARERECORD"), "Temp", RecordTime, Temp_NowTime_Str + "-" + GetMd5Hash(id + "CARERECORD"), EmrXmlString);
                    }
                    catch (Exception ex)
                    {
                        lt.saveLogMsg(ex.Message, "EMR_Pre_Operation_Log");
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), "EMRLogTns", EMRid, ex);
            }

            return erow;
        }

        #region 寫ErrorLog (write_logMsg)
        //寫入 write_logMsg
        public static void write_logMsg(string controllerName, string loginID, string actionName, string err_msg, string err_folder = "DBExecSQL", string strSQL = "", Exception ex = null)
        {
            string tmp_msg = "", tmpfolder = "";
            if (err_folder != "")
                tmpfolder = err_folder;
            else
                tmpfolder = "DBExecSQL";
            tmp_msg = "loginID: " + loginID + ",\tcontrollerName: " + controllerName + "\t,ActionName: " + actionName + "\t行號: #" + GetLineNumber(ex).ToString() + "\n";
            LogTool log = new LogTool();
            if (strSQL != "")
            {
                log.saveLogMsg(tmp_msg + ", err_msg= \t" + err_msg, tmpfolder);
                log.saveLogMsg(tmp_msg + ", SQL= \t" + strSQL, tmpfolder);
            }
            else
                log.saveLogMsg(tmp_msg + ", err_msg= \t" + err_msg, tmpfolder);
        }
        public static void write_logMsg(string controllerName, string loginID, string actionName, string err_msg)
        {
            string tmp_msg = "", tmpfolder = "DBExecSQL", strSQL = "", err_folder = "";
            if (err_folder != "")
                tmpfolder = err_folder;
            else
                tmpfolder = "DBExecSQL";
            tmp_msg = "loginID: " + loginID + ",\tcontrollerName: " + controllerName + "\t,ActionName: " + actionName + "\n";
            LogTool log = new LogTool();
            if (strSQL != "")
            {
                log.saveLogMsg(tmp_msg + ", err_msg= \t" + err_msg, tmpfolder);
                log.saveLogMsg(tmp_msg + ", SQL= \t" + strSQL, tmpfolder);
            }
            else
                log.saveLogMsg(tmp_msg + ", err_msg= \t" + err_msg, tmpfolder);
        }

        public static void write_logMsg(string controllerName, string loginID, string actionName, string err_msg, Exception ex = null)
        {
            string tmp_msg = "", tmpfolder = "DBExecSQL", strSQL = "", err_folder = "";
            if (err_folder != "")
                tmpfolder = err_folder;
            else
                tmpfolder = "DBExecSQL";
            tmp_msg = "loginID: " + loginID + ",\tcontrollerName: " + controllerName + "\t,ActionName: " + actionName + "\t行號: #" + GetLineNumber(ex).ToString() + "\n";
            LogTool log = new LogTool();
            if (strSQL != "")
            {
                log.saveLogMsg(tmp_msg + ", err_msg= \t" + err_msg, tmpfolder);
                log.saveLogMsg(tmp_msg + ", SQL= \t" + strSQL, tmpfolder);
            }
            else
                log.saveLogMsg(tmp_msg + ", err_msg= \t" + err_msg, tmpfolder);
        }

        //回傳error行號
        public static int GetLineNumber(Exception ex)
        {
            var lineNumber = 0;
            const string lineSearch = ":line ";
            var index = ex.StackTrace.LastIndexOf(lineSearch);
            if (index != -1)
            {
                var lineNumberText = ex.StackTrace.Substring(index + lineSearch.Length);
                if (int.TryParse(lineNumberText, out lineNumber))
                {
                }
            }
            return lineNumber;
        }
        #endregion


        /// <summary>
        /// 新增護理紀錄_黑字
        /// </summary>
        /// <param name="time">紀錄時間</param>
        /// <param name="id">P_KEY</param>
        /// <param name="title">標題(可為空值)</param>
        /// <param name="C">內容:一般(可為空值)</param>
        /// <param name="S">內容:主觀(可為空值)</param>
        /// <param name="O">內容:客觀(可為空值)</param>
        /// <param name="I">內容:執行(可為空值)</param>
        /// <param name="E">內容:評值(可為空值)</param>
        /// <param name="self">此筆記錄可於護理紀錄修改傳入(CARERECORD) 不可修改傳入 table name</param>
        public int Insert_CareRecord_Black(string time, string id, string title, string C, string S, string O, string I, string E, string self = "")
        {
            int erow = 0;
            if (Session["PatInfo"] != null)
            {
                CareRecord care_record_m = new CareRecord();
                string userno = userinfo.EmployeesNo;
                string feeno = ptinfo.FeeNo;
                DateTime NowTime = DateTime.Now;
                LogTool lt = new LogTool();
                string sign_userno = care_record_m.sel_guide_userno(userno, Convert.ToDateTime(time), Convert.ToDateTime(time).Hour);
                //DBConnector dbconnector = new DBConnector();
                try
                {
                    List<DBItem> insertDataList = new List<DBItem>();
                    insertDataList.Add(new DBItem("CARERECORD_ID", id, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("GUIDE_NO", sign_userno, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATTIME", NowTime.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("RECORDTIME", time, DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("TITLE", trans_date(title), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("C", trans_date(C), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("S", trans_date(S), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("O", trans_date(O), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("I", trans_date(I), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("E", trans_date(E), DBItem.DBDataType.String));
                    if (string.IsNullOrEmpty(self))
                    {
                        insertDataList.Add(new DBItem("SELF", "CARERECORD", DBItem.DBDataType.String));
                    }
                    else
                    {
                        insertDataList.Add(new DBItem("SELF", self, DBItem.DBDataType.String));
                    }
                    insertDataList.Add(new DBItem("SIGN", "N", DBItem.DBDataType.String));
                    erow = link.DBExecInsert("CARERECORD_DATA", insertDataList);
                }
                catch (Exception ex)
                {
                    string detail_Str = "\r\n CARERECORD_ID:" + ValReplaceNullToStr(id) + "\r\n CREATNO:" + ValReplaceNullToStr(userno) + "\r\n GUIDE_NO:" + ValReplaceNullToStr(sign_userno) + " \r\n ";
                    detail_Str += "CREATNAME:" + ValReplaceNullToStr(userinfo.EmployeesName) + "\r\n CREATTIME:" + ValReplaceNullToStr(NowTime.ToString("yyyy/MM/dd HH:mm")) + "\r\n RECORDTIME:" + ValReplaceNullToStr(time);
                    detail_Str += "\r\n FEENO:" + ValReplaceNullToStr(feeno) + "\r\n TITLE:" + ValReplaceNullToStr(title) + "\r\n C:" + ValReplaceNullToStr(C) + "\r\n S:" + ValReplaceNullToStr(S) + "\r\n O:" + ValReplaceNullToStr(O);
                    detail_Str += "\r\n I:" + ValReplaceNullToStr(I) + "\r\n E:" + ValReplaceNullToStr(E);
                    lt.saveLogMsg(ex.Message + detail_Str, "CareRecord_Log_Black");
                }
                if (erow > 0)
                {
                    #region --EMR--
                    try
                    {
                        //byte[] allergen = webService.GetAllergyList(feeno);
                        //string ptJsonArr = string.Empty;
                        //string allergyDesc = string.Empty;
                        //if (allergen != null) {
                        //    ptJsonArr = CompressTool.DecompressString(allergen);
                        //}

                        //List<NIS.Data.PatientInfo> patList = JsonConvert.DeserializeObject<List<NIS.Data.PatientInfo>>(ptJsonArr);
                        string msg = "";

                        if (trans_date(C) != "")
                            msg += trans_date(C) + "\n";// + trans_date(C)
                        if (trans_date(S) != "")
                            msg += "S:" + trans_date(S) + "\n";// + trans_date(S) 
                        if (trans_date(O) != "")
                            msg += "O:" + trans_date(O) + "\n";//+ trans_date(O) 
                        if (trans_date(I) != "")
                            msg += "I:" + trans_date(I) + "\n";// + trans_date(I) 
                        if (trans_date(E) != "")
                            msg += "E:" + trans_date(E) + "\n";// + trans_date(E)

                        //if (allergen != null) {
                        //    allergyDesc = patList[0].AllergyDesc;
                        //}

                        //string xml = care_record_m.care_Record_Get_xml(ptinfo.PatientName, ptinfo.ChartNo,
                        //ptinfo.PatientGender, (ptinfo.Age).ToString(), ptinfo.BedNo, ptinfo.InDate.ToString("yyyyMMdd"),
                        //ptinfo.InDate.ToString("HHmm"), allergyDesc, Convert.ToDateTime(time).ToString("yyyyMMdd"),
                        //Convert.ToDateTime(time).ToString("HHmm"), userinfo.EmployeesName, trans_date(title), msg);

                        //取得應簽章人員
                        //byte[] listByteCode = webService.UserName(userinfo.Guider);
                        //string listJsonArray = CompressTool.DecompressString(listByteCode);
                        //UserInfo user_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                        //string RecordTime = NowTime.ToString("yyyy/MM/dd HH:mm:ss");
                        //string Temp_NowTime_Str = Convert.ToDateTime(RecordTime).ToString("yyyyMMddHHmmss");//時間採統一變數
                        //string EmrXmlString = this.get_xml(
                        //    NowTime.ToString("yyyyMMddHHmmss.fffffff"), Temp_NowTime_Str + GetMd5Hash(id + "CARERECORD"), "A000040", Convert.ToDateTime(time).ToString("yyyyMMdd"),
                        //    GetMd5Hash(id + "CARERECORD"), Convert.ToDateTime(time).ToString("yyyyMMdd"), "", "",
                        //    user_info.EmployeesNo, user_info.EmployeesName, user_info.UserID, ptinfo.ChartNo, ptinfo.PatientName,
                        //    ptinfo.PatientID, ptinfo.PayInfo,
                        //    "C:\\EMR\\", "A000040" + GetMd5Hash(id + "CARERECORD") + Temp_NowTime_Str + ".xml", listJsonArray , "Insert_CareRecord_Black"
                        //    );
                        erow = EMR_Sign(time, id, msg, title, self, "");

                        //SaveEMRLogData(id + "CARERECORD", GetMd5Hash(id + "CARERECORD"), "EMR", RecordTime, "A000040" + GetMd5Hash(id + "CARERECORD") + Temp_NowTime_Str, xml);
                        //SaveEMRLogData(id + "CARERECORD", GetMd5Hash(id + "CARERECORD"), "Temp", RecordTime, Temp_NowTime_Str + "-" + GetMd5Hash(id + "CARERECORD"), EmrXmlString);
                    }
                    catch (Exception ex)
                    {
                        lt.saveLogMsg(ex.Message, "EMR_Pre_Operation_Log");
                    }
                    #endregion
                    //將紀錄回寫至 EMR Temp Table
                    ////string sqlstr = "begin P_NIS_EMRMS('" + feeno + "','014','護理紀錄單','" + id + "CARERECORD','" + time + "','" + sign_userno + "','I');end;";
                    ////dbconnector.DBExec(sqlstr);
                    #region JAG 簽章
                    // 20150608 EMR
                    //20160223 簽章先拿掉 方便測試
                    ////string strPath = @"C:\\wkhtmltopdf\\wkhtmltopdf.exe";
                    ////string filename = @"C:\inetpub\NIS\Images\" + id + "CARERECORD.pdf";

                    ////string port = System.Web.HttpContext.Current.Request.ServerVariables["SERVER_PORT"];
                    ////if (port == null || port == "80" || port == "443")
                    ////    port = "";
                    ////else
                    ////    port = ":" + port;

                    ////string protocol = System.Web.HttpContext.Current.Request.ServerVariables["SERVER_PORT_SECURE"];
                    ////if (protocol == null || protocol == "0")
                    ////    protocol = "http://";
                    ////else
                    ////    protocol = "https://";

                    ////string sOut = protocol + System.Web.HttpContext.Current.Request.ServerVariables["SERVER_NAME"] + port + System.Web.HttpContext.Current.Request.ApplicationPath;

                    ////if (sOut.EndsWith("/"))
                    ////{
                    ////    sOut = sOut.Substring(0, sOut.Length - 1);
                    ////}

                    ////string url = sOut + "/CareRecord/List_PDF?id=" + id + "CARERECORD&feeno=" + feeno;
                    ////Process p = new Process();
                    ////p.StartInfo.FileName = strPath;
                    ////p.StartInfo.Arguments = url + " " + filename;
                    ////p.StartInfo.UseShellExecute = true;
                    ////p.Start();
                    ////p.WaitForExit();

                    ////NIS.WebService.Nis nis = new WebService.Nis();
                    ////byte[] listByteCode = nis.UserName(userinfo.EmployeesNo);
                    ////string listJsonArray = CompressTool.DecompressString(listByteCode);
                    ////UserInfo user_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                    ////string emp_id = (user_info.UserID != null) ? user_info.UserID.Trim() : "";
                    ////string emp_name = (user_info.EmployeesName != null) ? user_info.EmployeesName.Trim() : "";
                    ////string dep_no = ptinfo.DeptNo;
                    ////string chr_no = ptinfo.ChartNo;
                    ////string pat_name = ptinfo.PatientName;
                    ////string in_date = ptinfo.InDate.ToString("yyyyMMdd");
                    ////string chagre_type = (ptinfo.PayInfo == "健保") ? "001" : "000";
                    ////int result = emr_sign(id + "CARERECORD", feeno, "014", userno, emp_name, emp_id, dep_no, chr_no, pat_name, in_date, chagre_type, filename);
                    #endregion
                }
            }

            return erow;
        }

        /// <summary>
        /// 新增護理紀錄_黑字
        /// </summary>
        /// <param name="time">紀錄時間</param>
        /// <param name="id">P_KEY</param>
        /// <param name="title">標題(可為空值)</param>
        /// <param name="C">內容:一般(可為空值)</param>
        /// <param name="S">內容:主觀(可為空值)</param>
        /// <param name="O">內容:客觀(可為空值)</param>
        /// <param name="I">內容:執行(可為空值)</param>
        /// <param name="E">內容:評值(可為空值)</param>
        /// <param name="self">此筆記錄可於護理紀錄修改傳入(CARERECORD) 不可修改傳入 table name</param>
        public int Insert_CareRecord_BlackTns(string time, string id, string title, string C, string S, string O, string I, string E, ref DBConnector link, string self = "")
        {
            int erow = 0;
            if (Session["PatInfo"] != null)
            {
                CareRecord care_record_m = new CareRecord();
                string feeno = ptinfo.FeeNo;
                string sign_userno = care_record_m.sel_guide_userno(this.userinfo.EmployeesNo, Convert.ToDateTime(time), Convert.ToDateTime(time).Hour);
                DateTime NowTime = DateTime.Now;
                LogTool lt = new LogTool();
                List<DBItem> insertDataList = new List<DBItem>();
                insertDataList.Add(new DBItem("CARERECORD_ID", id, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATNO", this.userinfo.EmployeesNo, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("GUIDE_NO", sign_userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATNAME", this.userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATTIME", NowTime.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("RECORDTIME", time, DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("FEENO", this.ptinfo.FeeNo, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("TITLE", trans_date(title), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("C", trans_date(C), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("S", trans_date(S), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("O", trans_date(O), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("I", trans_date(I), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("E", trans_date(E), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("SELF", "CARERECORD", DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("SIGN", "N", DBItem.DBDataType.String));

                //DBConnector dbconnector = new DBConnector();
                erow = link.DBExecInsertTns("CARERECORD_DATA", insertDataList);
                if (erow > 0)
                {
                    #region --EMR--
                    //try {
                    //    byte[] TempByte = webService.GetAllergyList(this.ptinfo.FeeNo);
                    //    string allergyDesc = string.Empty, msg = string.Empty;
                    //    string RecordTime = NowTime.ToString("yyyy/MM/dd HH:mm:ss");
                    //    string Temp_NowTime_Str = Convert.ToDateTime(RecordTime).ToString("yyyyMMddHHmmss");//時間採統一變數
                    //    if (TempByte != null) {
                    //        List<NIS.Data.PatientInfo> patList = JsonConvert.DeserializeObject<List<NIS.Data.PatientInfo>>(CompressTool.DecompressString(TempByte));
                    //        allergyDesc = patList[0].AllergyDesc;
                    //    }

                    //    if (trans_date(C) != "")
                    //        msg += trans_date(C) + "\n";
                    //    if (trans_date(S) != "")
                    //        msg += "S:" + trans_date(S) + "\n";
                    //    if (trans_date(O) != "")
                    //        msg += "O:" + trans_date(O) + "\n";
                    //    if (trans_date(I) != "")
                    //        msg += "I:" + trans_date(I) + "\n";
                    //    if (trans_date(E) != "")
                    //        msg += "E:" + trans_date(E) + "\n";

                    //    string xml = care_record_m.care_Record_Get_xml(this.ptinfo.PatientName, this.ptinfo.ChartNo,
                    //    this.ptinfo.PatientGender, (this.ptinfo.Age).ToString(), this.ptinfo.BedNo, this.ptinfo.InDate.ToString("yyyyMMdd"),
                    //    this.ptinfo.InDate.ToString("HHmm"), allergyDesc, Convert.ToDateTime(time).ToString("yyyyMMdd"),
                    //    Convert.ToDateTime(time).ToString("HHmm"), this.userinfo.EmployeesName, trans_date(title), msg);
                    //    SaveEMRLogData(id + "CARERECORD", GetMd5Hash(id + "CARERECORD"), "EMR", RecordTime, "A000040" + GetMd5Hash(id + "CARERECORD") + Temp_NowTime_Str, xml);
                    //    //取得應簽章人員
                    //    TempByte = webService.UserName(this.userinfo.Guider);
                    //    if (TempByte != null) {
                    //        string listJsonArray = CompressTool.DecompressString(TempByte);
                    //        UserInfo user_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);

                    //        string EmrXmlString = this.get_xml(
                    //            NowTime.ToString("yyyyMMddHHmmss.fffffff"), Temp_NowTime_Str + GetMd5Hash(id + "CARERECORD"), "A000040", Convert.ToDateTime(time).ToString("yyyyMMdd"),
                    //            GetMd5Hash(id + "CARERECORD"), Convert.ToDateTime(time).ToString("yyyyMMdd"), "", "",
                    //            user_info.EmployeesNo, user_info.EmployeesName, user_info.UserID, this.ptinfo.ChartNo, this.ptinfo.PatientName,
                    //            this.ptinfo.PatientID, this.ptinfo.PayInfo,
                    //            "C:\\EMR\\", "A000040" + GetMd5Hash(id + "CARERECORD") + Temp_NowTime_Str + ".xml", listJsonArray , "Insert_CareRecord_BlackTns"
                    //            );
                    //        SaveEMRLogData(id + "CARERECORD", GetMd5Hash(id + "CARERECORD"), "Temp", RecordTime, Temp_NowTime_Str + "-" + GetMd5Hash(id + "CARERECORD"), EmrXmlString);
                    //    } else {
                    //        lt.saveLogMsg(userinfo.EmployeesNo + "_" + userinfo.Guider + "_" + id + "CARERECORD_" + GetMd5Hash(id + "CARERECORD"), "EMRLogTns");
                    //    }
                    //} catch { }
                    try
                    {
                        //byte[] allergen = webService.GetAllergyList(feeno);
                        //string ptJsonArr = string.Empty;
                        //string allergyDesc = string.Empty;
                        //if (allergen != null)
                        //{
                        //    ptJsonArr = CompressTool.DecompressString(allergen);
                        //}

                        //List<NIS.Data.PatientInfo> patList = JsonConvert.DeserializeObject<List<NIS.Data.PatientInfo>>(ptJsonArr);
                        string msg = "";

                        if (trans_date(C) != "")
                            msg += trans_date(C) + "\n";// + trans_date(C)
                        if (trans_date(S) != "")
                            msg += "S:" + trans_date(S) + "\n";// + trans_date(S) 
                        if (trans_date(O) != "")
                            msg += "O:" + trans_date(O) + "\n";//+ trans_date(O) 
                        if (trans_date(I) != "")
                            msg += "I:" + trans_date(I) + "\n";// + trans_date(I) 
                        if (trans_date(E) != "")
                            msg += "E:" + trans_date(E) + "\n";// + trans_date(E)

                        //if (allergen != null)
                        //{
                        //    allergyDesc = patList[0].AllergyDesc;
                        //}

                        //string xml = care_record_m.care_Record_Get_xml(ptinfo.PatientName, ptinfo.ChartNo,
                        //ptinfo.PatientGender, (ptinfo.Age).ToString(), ptinfo.BedNo, ptinfo.InDate.ToString("yyyyMMdd"),
                        //ptinfo.InDate.ToString("HHmm"), allergyDesc, Convert.ToDateTime(time).ToString("yyyyMMdd"),
                        //Convert.ToDateTime(time).ToString("HHmm"), userinfo.EmployeesName, trans_date(title), msg);

                        //取得應簽章人員
                        //byte[] listByteCode = webService.UserName(userinfo.Guider);
                        //string listJsonArray = CompressTool.DecompressString(listByteCode);
                        //UserInfo user_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                        //string RecordTime = NowTime.ToString("yyyy/MM/dd HH:mm:ss");
                        //string Temp_NowTime_Str = Convert.ToDateTime(RecordTime).ToString("yyyyMMddHHmmss");//時間採統一變數
                        //string EmrXmlString = this.get_xml(
                        //    NowTime.ToString("yyyyMMddHHmmss.fffffff"), Temp_NowTime_Str + GetMd5Hash(id + "CARERECORD"), "A000040", Convert.ToDateTime(time).ToString("yyyyMMdd"),
                        //    GetMd5Hash(id + "CARERECORD"), Convert.ToDateTime(time).ToString("yyyyMMdd"), "", "",
                        //    user_info.EmployeesNo, user_info.EmployeesName, user_info.UserID, ptinfo.ChartNo, ptinfo.PatientName,
                        //    ptinfo.PatientID, ptinfo.PayInfo,
                        //    "C:\\EMR\\", "A000040" + GetMd5Hash(id + "CARERECORD") + Temp_NowTime_Str + ".xml", listJsonArray, "Insert_CareRecord_Black"
                        //    );
                        erow = EMR_Sign(time, id, msg, title, self, "");

                        //SaveEMRLogData(id + "CARERECORD", GetMd5Hash(id + "CARERECORD"), "EMR", RecordTime, "A000040" + GetMd5Hash(id + "CARERECORD") + Temp_NowTime_Str, xml);
                        //SaveEMRLogData(id + "CARERECORD", GetMd5Hash(id + "CARERECORD"), "Temp", RecordTime, Temp_NowTime_Str + "-" + GetMd5Hash(id + "CARERECORD"), EmrXmlString);
                    }
                    catch (Exception ex)
                    {
                        lt.saveLogMsg(ex.Message, "EMR_Pre_Operation_Log");
                    }
                    #endregion
                }
            }

            return erow;
        }

        /// <summary>
        /// 更新護理紀錄
        /// </summary>
        /// <param name="time">紀錄時間</param>
        /// <param name="id">P_KEY</param>
        /// <param name="title">標題(可為空值)</param>
        /// <param name="C">內容:一般(可為空值)</param>
        /// <param name="S">內容:主觀(可為空值)</param>
        /// <param name="O">內容:客觀(可為空值)</param>
        /// <param name="I">內容:執行(可為空值)</param>
        /// <param name="E">內容:評值(可為空值)</param>
        public int Upd_CareRecord(string time, string id, string title, string C, string S, string O, string I, string E, string self, string historyID = "", string HistoryEdit = "")
        {
            int erow = 0;
            if (Session["PatInfo"] != null)
            {
                CareRecord care_record_m = new CareRecord();
                string userno = userinfo.EmployeesNo;
                string feeno = ptinfo.FeeNo;
                try
                {
                    string sign_userno = care_record_m.sel_guide_userno(userno, Convert.ToDateTime(time), Convert.ToDateTime(time).Hour);
                    DateTime NowTime = DateTime.Now;
                    List<DBItem> insertDataList = new List<DBItem>();
                    LogTool lt = new LogTool();
                    insertDataList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("GUIDE_NO", sign_userno, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("UPDTIME", NowTime.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("RECORDTIME", time, DBItem.DBDataType.DataTime));
                    if (string.IsNullOrEmpty(HistoryEdit))
                    {
                        insertDataList.Add(new DBItem("TITLE", trans_date(title), DBItem.DBDataType.String));
                    }
                    insertDataList.Add(new DBItem("C_OTHER", trans_date(C), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("S_OTHER", trans_date(S), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("O_OTHER", trans_date(O), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("I_OTHER", trans_date(I), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("E_OTHER", trans_date(E), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("SIGN", "N", DBItem.DBDataType.String));
                    string where = "CARERECORD_ID = '" + id + "' AND SELF = '" + self + "' AND DELETED IS NULL ";

                    erow = link.DBExecUpdate("CARERECORD_DATA", insertDataList, where);
                    if (erow > 0 && id != "")
                    {
                        insertDataList.Clear();
                        insertDataList.Add(new DBItem("STATUS", "M", DBItem.DBDataType.String));
                        string EMRwhere = " RECORD_KEY  = '" + id + "' AND SELF = '" + self + "'";
                        erow = care_record_m.DBExecUpdate("NIS_EMR_PACKAGE_DTL", insertDataList, EMRwhere);

                    }
                    if (erow > 0)
                    {
                        //將紀錄回寫至 EMR Temp Table
                        ////string sqlstr = "begin P_NIS_EMRMS('" + ptinfo.FeeNo + "','014','護理紀錄單','" + id + self  + "','" + time + "','" + sign_userno + "','I');end;";
                        ////care_record_m.DBExec(sqlstr);

                        #region JAG 簽章
                        // 20150608 EMR
                        //20160223 簽章先拿掉 方便測試
                        ////string strPath = @"C:\\wkhtmltopdf\\wkhtmltopdf.exe";
                        ////string filename = @"C:\inetpub\NIS\Images\" + id + self+".pdf";

                        ////string port = System.Web.HttpContext.Current.Request.ServerVariables["SERVER_PORT"];
                        ////if (port == null || port == "80" || port == "443")
                        ////    port = "";
                        ////else
                        ////    port = ":" + port;

                        ////string protocol = System.Web.HttpContext.Current.Request.ServerVariables["SERVER_PORT_SECURE"];
                        ////if (protocol == null || protocol == "0")
                        ////    protocol = "http://";
                        ////else
                        ////    protocol = "https://";

                        ////string sOut = protocol + System.Web.HttpContext.Current.Request.ServerVariables["SERVER_NAME"] + port + System.Web.HttpContext.Current.Request.ApplicationPath;

                        ////if (sOut.EndsWith("/"))
                        ////{
                        ////    sOut = sOut.Substring(0, sOut.Length - 1);
                        ////}

                        ////string url = sOut + "/CareRecord/List_PDF?id=" + id + self + "&feeno=" + ptinfo.FeeNo;
                        ////Process p = new Process();
                        ////p.StartInfo.FileName = strPath;
                        ////p.StartInfo.Arguments = url + " " + filename;
                        ////p.StartInfo.UseShellExecute = true;
                        ////p.Start();
                        ////p.WaitForExit();

                        ////NIS.WebService.Nis nis = new WebService.Nis();
                        ////byte[] listByteCode = nis.UserName(userinfo.EmployeesNo);
                        ////string listJsonArray = CompressTool.DecompressString(listByteCode);
                        ////UserInfo user_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                        ////string emp_id = (user_info.UserID != null) ? user_info.UserID.Trim() : "";
                        ////string emp_name = (user_info.EmployeesName != null) ? user_info.EmployeesName.Trim() : "";
                        ////string dep_no = ptinfo.DeptNo;
                        ////string chr_no = ptinfo.ChartNo;
                        ////string pat_name = ptinfo.PatientName;
                        ////string in_date = ptinfo.InDate.ToString("yyyyMMdd");
                        ////string chagre_type = (ptinfo.PayInfo == "健保") ? "001" : "000";
                        ////int result = emr_sign(id + self, ptinfo.FeeNo, "014", userno, emp_name, emp_id, dep_no, chr_no, pat_name, in_date, chagre_type, filename);
                        #endregion


                        #region --EMR--

                        try
                        {
                            //byte[] allergen = webService.GetAllergyList(feeno);
                            //string ptJsonArr = string.Empty;
                            //string allergyDesc = string.Empty;

                            //if (allergen != null)
                            //{
                            //    ptJsonArr = CompressTool.DecompressString(allergen);
                            //}

                            //List<NIS.Data.PatientInfo> patList = JsonConvert.DeserializeObject<List<NIS.Data.PatientInfo>>(ptJsonArr);
                            string msg = "";

                            if (trans_date(C) != "")
                                msg += trans_date(C) + "\n";// + trans_date(C)
                            if (trans_date(S) != "")
                                msg += "S:" + trans_date(S) + "\n";// + trans_date(S) 
                            if (trans_date(O) != "")
                                msg += "O:" + trans_date(O) + "\n";//+ trans_date(O) 
                            if (trans_date(I) != "")
                                msg += "I:" + trans_date(I) + "\n";// + trans_date(I) 
                            if (trans_date(E) != "")
                                msg += "E:" + trans_date(E) + "\n";// + trans_date(E)

                            //if (allergen != null)
                            //{
                            //    allergyDesc = patList[0].AllergyDesc;
                            //}

                            //string xml = care_record_m.care_Record_Get_xml(ptinfo.PatientName, ptinfo.ChartNo,
                            //ptinfo.PatientGender, (ptinfo.Age).ToString(), ptinfo.BedNo, ptinfo.InDate.ToString("yyyyMMdd"),
                            //ptinfo.InDate.ToString("HHmm"), allergyDesc, Convert.ToDateTime(time).ToString("yyyyMMdd"),
                            //Convert.ToDateTime(time).ToString("HHmm"), userinfo.EmployeesName, trans_date(title), msg);

                            //取得應簽章人員
                            //byte[] listByteCode = webService.UserName(userinfo.Guider);
                            //string listJsonArray = CompressTool.DecompressString(listByteCode);
                            //UserInfo user_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);

                            //string RecordTime = NowTime.ToString("yyyy/MM/dd HH:mm:ss");
                            //string Temp_NowTime_Str = Convert.ToDateTime(RecordTime).ToString("yyyyMMddHHmmss");//時間採統一變數
                            //string EmrXmlString = this.get_xml(
                            //    NowTime.ToString("yyyyMMddHHmmss.fffffff"), Temp_NowTime_Str + GetMd5Hash(id + self), "A000040", Convert.ToDateTime(time).ToString("yyyyMMdd"),
                            //    GetMd5Hash(id + self), Convert.ToDateTime(time).ToString("yyyyMMdd"), "", "",
                            //    user_info.EmployeesNo, user_info.EmployeesName, user_info.UserID, ptinfo.ChartNo, ptinfo.PatientName,
                            //    ptinfo.PatientID, ptinfo.PayInfo,
                            //    "C:\\EMR\\", "A000040" + GetMd5Hash(id + self) + Temp_NowTime_Str + ".xml", listJsonArray, "Insert_CareRecord"
                            //    );
                            erow = EMR_Sign(time, id, msg, title, self, "");
                        }
                        catch (Exception ex)
                        {
                            lt.saveLogMsg(ex.Message, "EMR_Pre_Operation_Log");
                        }
                    }
                }
                catch (Exception ex)
                {
                    //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                    string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                    string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                    write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                }
                finally
                {
                    care_record_m.DBClose();
                    this.link.DBClose();
                }
                #endregion
            }

            return erow;
        }

        /// <summary>
        /// 更新護理紀錄
        /// </summary>
        /// <param name="time">紀錄時間</param>
        /// <param name="id">P_KEY</param>
        /// <param name="title">標題(可為空值)</param>
        /// <param name="C">內容:一般(可為空值)</param>
        /// <param name="S">內容:主觀(可為空值)</param>
        /// <param name="O">內容:客觀(可為空值)</param>
        /// <param name="I">內容:執行(可為空值)</param>
        /// <param name="E">內容:評值(可為空值)</param>
        public int Upd_CareRecordTns(string time, string id, string title, string C, string S, string O, string I, string E, string self, ref DBConnector link)
        {
            int erow = 0;
            if (Session["PatInfo"] != null)
            {
                string feeno = ptinfo.FeeNo;
                CareRecord care_record_m = new CareRecord();
                string sign_userno = care_record_m.sel_guide_userno(this.userinfo.EmployeesNo, Convert.ToDateTime(time), Convert.ToDateTime(time).Hour);
                DateTime NowTime = DateTime.Now;
                LogTool lt = new LogTool();

                List<DBItem> insertDataList = new List<DBItem>();
                insertDataList.Add(new DBItem("UPDNO", this.userinfo.EmployeesNo, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("GUIDE_NO", sign_userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDTIME", NowTime.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("RECORDTIME", time, DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("TITLE", trans_date(title), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("C_OTHER", trans_date(C), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("S_OTHER", trans_date(S), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("O_OTHER", trans_date(O), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("I_OTHER", trans_date(I), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("E_OTHER", trans_date(E), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("SIGN", "N", DBItem.DBDataType.String));

                erow = link.DBExecUpdateTns("CARERECORD_DATA", insertDataList, "CARERECORD_ID = '" + id + "' AND SELF = '" + self + "' ");
                #region --EMR--
                //try {
                //    byte[] tempByte = webService.GetAllergyList(this.ptinfo.FeeNo);
                //    string allergyDesc = string.Empty, msg = string.Empty;
                //    string RecordTime = NowTime.ToString("yyyy/MM/dd HH:mm:ss");
                //    string Temp_NowTime_Str = Convert.ToDateTime(RecordTime).ToString("yyyyMMddHHmmss");//時間採統一變數
                //    if (tempByte != null) {
                //        List<NIS.Data.PatientInfo> patList = JsonConvert.DeserializeObject<List<NIS.Data.PatientInfo>>(CompressTool.DecompressString(tempByte));
                //        allergyDesc = patList[0].AllergyDesc;
                //    }

                //    if (trans_date(C) != "")
                //        msg += trans_date(C) + "\n";
                //    if (trans_date(S) != "")
                //        msg += "S:" + trans_date(S) + "\n";
                //    if (trans_date(O) != "")
                //        msg += "O:" + trans_date(O) + "\n";
                //    if (trans_date(I) != "")
                //        msg += "I:" + trans_date(I) + "\n";
                //    if (trans_date(E) != "")
                //        msg += "E:" + trans_date(E) + "\n";

                //    string xml = care_record_m.care_Record_Get_xml(this.ptinfo.PatientName, this.ptinfo.ChartNo,
                //       this.ptinfo.PatientGender, (this.ptinfo.Age).ToString(), this.ptinfo.BedNo, this.ptinfo.InDate.ToString("yyyyMMdd"),
                //       this.ptinfo.InDate.ToString("HHmm"), allergyDesc, Convert.ToDateTime(time).ToString("yyyyMMdd"),
                //       Convert.ToDateTime(time).ToString("HHmm"), userinfo.EmployeesName, trans_date(title), msg);
                //    SaveEMRLogData(id + self, GetMd5Hash(id + self), "EMR", RecordTime, "A000040" + GetMd5Hash(id + self) + Temp_NowTime_Str, xml);

                //    //取得應簽章人員
                //    tempByte = webService.UserName(userinfo.Guider);
                //    if (tempByte != null) {
                //        string listJsonArray = CompressTool.DecompressString(tempByte);
                //        UserInfo user_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                //        string EmrXmlString = this.get_xml(
                //            NowTime.ToString("yyyyMMddHHmmss.fffffff"), Temp_NowTime_Str + GetMd5Hash(id + self), "A000040", Convert.ToDateTime(time).ToString("yyyyMMdd"),
                //             GetMd5Hash(id + self), Convert.ToDateTime(time).ToString("yyyyMMdd"), "", "",
                //             user_info.EmployeesNo, user_info.EmployeesName, user_info.UserID, ptinfo.ChartNo, ptinfo.PatientName,
                //            ptinfo.PatientID, ptinfo.PayInfo,
                //            "C:\\EMR\\", "A000040" + GetMd5Hash(id + self) + Temp_NowTime_Str + ".xml", listJsonArray , "Upd_CareRecordTns"
                //            );
                //        SaveEMRLogData(id + self, GetMd5Hash(id + self), "Temp", RecordTime, Temp_NowTime_Str + "-" + GetMd5Hash(id + self), EmrXmlString);
                //    }
                //} catch { }
                #endregion

                try
                {
                    //byte[] allergen = webService.GetAllergyList(feeno);
                    //string ptJsonArr = string.Empty;
                    //string allergyDesc = string.Empty;

                    //if (allergen != null)
                    //{
                    //    ptJsonArr = CompressTool.DecompressString(allergen);
                    //}

                    //List<NIS.Data.PatientInfo> patList = JsonConvert.DeserializeObject<List<NIS.Data.PatientInfo>>(ptJsonArr);
                    string msg = "";

                    if (trans_date(C) != "")
                        msg += trans_date(C) + "\n";// + trans_date(C)
                    if (trans_date(S) != "")
                        msg += "S:" + trans_date(S) + "\n";// + trans_date(S) 
                    if (trans_date(O) != "")
                        msg += "O:" + trans_date(O) + "\n";//+ trans_date(O) 
                    if (trans_date(I) != "")
                        msg += "I:" + trans_date(I) + "\n";// + trans_date(I) 
                    if (trans_date(E) != "")
                        msg += "E:" + trans_date(E) + "\n";// + trans_date(E)

                    //if (allergen != null)
                    //{
                    //    allergyDesc = patList[0].AllergyDesc;
                    //}

                    //string xml = care_record_m.care_Record_Get_xml(ptinfo.PatientName, ptinfo.ChartNo,
                    //ptinfo.PatientGender, (ptinfo.Age).ToString(), ptinfo.BedNo, ptinfo.InDate.ToString("yyyyMMdd"),
                    //ptinfo.InDate.ToString("HHmm"), allergyDesc, Convert.ToDateTime(time).ToString("yyyyMMdd"),
                    //Convert.ToDateTime(time).ToString("HHmm"), userinfo.EmployeesName, trans_date(title), msg);

                    //取得應簽章人員
                    //byte[] listByteCode = webService.UserName(userinfo.Guider);
                    //string listJsonArray = CompressTool.DecompressString(listByteCode);
                    //UserInfo user_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);

                    //string RecordTime = NowTime.ToString("yyyy/MM/dd HH:mm:ss");
                    //string Temp_NowTime_Str = Convert.ToDateTime(RecordTime).ToString("yyyyMMddHHmmss");//時間採統一變數
                    //string EmrXmlString = this.get_xml(
                    //    NowTime.ToString("yyyyMMddHHmmss.fffffff"), Temp_NowTime_Str + GetMd5Hash(id + self), "A000040", Convert.ToDateTime(time).ToString("yyyyMMdd"),
                    //    GetMd5Hash(id + self), Convert.ToDateTime(time).ToString("yyyyMMdd"), "", "",
                    //    user_info.EmployeesNo, user_info.EmployeesName, user_info.UserID, ptinfo.ChartNo, ptinfo.PatientName,
                    //    ptinfo.PatientID, ptinfo.PayInfo,
                    //    "C:\\EMR\\", "A000040" + GetMd5Hash(id + self) + Temp_NowTime_Str + ".xml", listJsonArray, "Insert_CareRecord"
                    //    );
                    erow = EMR_Sign(time, id, msg, title, self, "");
                }
                catch (Exception ex)
                {
                    lt.saveLogMsg(ex.Message, "EMR_Pre_Operation_Log");
                }
            }

            return erow;
        }

        /// <summary>
        /// 刪除護理紀錄
        /// </summary>
        /// <param name="id">P_KEY</param>
        public int Del_CareRecord(string id, string self, bool del_emrfile = true)
        {
            CareRecord care_record_m = new CareRecord();

            int erow = 0; string sql = "";
            try
            {
                if (Session["PatInfo"] != null)
                {
                    if (self == "C_Medication")
                    {
                        erow = link.DBExecDelete("CARERECORD_DATA", "CARERECORD_ID ='" + id + "' AND SELF ='" + self + "'");
                    }
                    else
                    {
                        List<DBItem> insertDataList = new List<DBItem>();
                        insertDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("DELETED", userinfo.EmployeesNo, DBItem.DBDataType.String));
                        string where = "";
                        if (self == "CAREPLANMASTER")
                        {
                            where = "CARERECORD_ID LIKE '%" + id + "%' AND SELF ='" + self + "' AND DELETED is null ";//id為必要值
                            sql = "SELECT CARERECORD_ID FROM CARERECORD_DATA WHERE " + where;
                            DataTable Dt = link.DBExecSQL(sql);
                            if (Dt.Rows.Count > 0)
                            {
                                for (int i = 0; i < Dt.Rows.Count; i++)
                                {
                                    id = Dt.Rows[i]["CARERECORD_ID"].ToString();
                                }
                            }
                        }
                        else
                        {
                            if (id != "Wound_Record")//使傷口紀錄-傷口護理能夠連動護理紀錄
                            {
                                where = "CARERECORD_ID = '" + id + "' ";//id為必要值
                            }
                            if (self != "")
                            {
                                where += "AND SELF = '" + self + "' ";
                            }
                        }
                        erow = link.DBExecUpdate("CARERECORD_DATA", insertDataList, where);
                    }
                    if (erow > 0)
                    {
                        try
                        {
                            //20230901 KEN 調整EMR為批次
                            if (erow > 0 && id != "")
                            {
                                string PAC_ID = "";
                                string delstr = "SELECT * FROM NIS_EMR_PACKAGE_DTL WHERE  RECORD_KEY  = '" + id + "' AND STATUS = 'Y'";
                                if (self != "")
                                {
                                    delstr += " AND SELF = '" + self + "' ";
                                }
                                DataTable Dt = link.DBExecSQL(delstr);
                                if (Dt.Rows.Count > 0)
                                {
                                    PAC_ID = Dt.Rows[0]["PAC_ID"].ToString();
                                }
                                List<DBItem> insertDataList = new List<DBItem>();
                                insertDataList.Clear();
                                insertDataList.Add(new DBItem("STATUS", "M", DBItem.DBDataType.String));
                                string EMRwhere = " RECORD_KEY  = '" + id + "'";
                                if (self != "")
                                {
                                    EMRwhere += " AND SELF = '" + self + "' ";
                                }
                                erow = care_record_m.DBExecUpdate("NIS_EMR_PACKAGE_DTL", insertDataList, EMRwhere);

                                if (erow > 0)
                                {
                                    insertDataList.Clear();
                                    insertDataList.Add(new DBItem("SIGN_STATUS", "U", DBItem.DBDataType.String));
                                    EMRwhere = " PAC_ID  = '" + PAC_ID + "'";
                                    erow = care_record_m.DBExecUpdate("NIS_EMR_PACKAGE_MST", insertDataList, EMRwhere);
                                }
                                //20231213 ken 調整批次簽章上線後，舊資料刪除方式
                                else
                                {
                                    string sqlcheck = "SELECT * FROM CARERECORD_DATA WHERE carerecord_id = '" + id + "' AND SELF = '" + self + "' AND CREATTIME <to_date('2023-11-21 12:00:00','yyyy-MM-dd HH24:mi:ss')";
                                    DataTable Dtck = link.DBExecSQL(sqlcheck);
                                    if (Dtck.Rows.Count > 0)
                                    {
                                        int result = del_emr(id + self, userinfo.EmployeesNo);
                                    }
                                }
                            }
                            //if (del_emrfile)
                            //{
                            //}
                            //int delEMR = del_emr_pack(id, userinfo.EmployeesNo);
                        }
                        catch (Exception ex)
                        {
                            //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                            string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                            string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                            write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, id + self.ToString(), "DBExecSQL", sql, ex);
                        }
                    }

                }

                return erow;
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), "DBExecSQL", sql, ex);
                return erow;
            }
            finally
            {
                this.link.DBClose();
            }
        }
        /// <summary>
        /// 刪除護理紀錄
        /// </summary>
        /// <param name="id">P_KEY</param>
        public int Del_CareRecordEKG(string id, string self, bool del_emrfile = true, string userNO = "")
        {
            CareRecord care_record_m = new CareRecord();
            List<DBItem> insertDataList = new List<DBItem>();

            int erow = 0; string sql = "";
            try
            {
                if (userNO != "")
                {
                    insertDataList = new List<DBItem>();
                    insertDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("DELETED", userNO, DBItem.DBDataType.String));
                    string where = "";
                    if (self != "")
                    {
                        where = "CARERECORD_ID LIKE '%" + id + "%' AND SELF ='" + self + "' AND DELETED is null ";//id為必要值
                    }
                    erow = link.DBExecUpdate("CARERECORD_DATA", insertDataList, where);

                    if (erow > 0)
                    {
                        try
                        {
                            //20230901 KEN 調整EMR為批次
                            if (erow > 0 && id != "")
                            {
                                string PAC_ID = "";
                                string delstr = "SELECT * FROM NIS_EMR_PACKAGE_DTL WHERE  RECORD_KEY  = '" + id + "' AND STATUS = 'Y'";
                                if (self != "")
                                {
                                    delstr += " AND SELF = '" + self + "' ";
                                }
                                DataTable Dt = link.DBExecSQL(delstr);
                                if (Dt.Rows.Count > 0)
                                {
                                    PAC_ID = Dt.Rows[0]["PAC_ID"].ToString();
                                }
                                insertDataList = new List<DBItem>();
                                insertDataList.Clear();
                                insertDataList.Add(new DBItem("STATUS", "M", DBItem.DBDataType.String));
                                string EMRwhere = " RECORD_KEY  = '" + id + "'";
                                if (self != "")
                                {
                                    EMRwhere += " AND SELF = '" + self + "' ";
                                }
                                erow = care_record_m.DBExecUpdate("NIS_EMR_PACKAGE_DTL", insertDataList, EMRwhere);

                                if (erow > 0)
                                {
                                    insertDataList.Clear();
                                    insertDataList.Add(new DBItem("SIGN_STATUS", "U", DBItem.DBDataType.String));
                                    EMRwhere = " PAC_ID  = '" + PAC_ID + "'";
                                    erow = care_record_m.DBExecUpdate("NIS_EMR_PACKAGE_MST", insertDataList, EMRwhere);
                                }
                                //20231213 ken 調整批次簽章上線後，舊資料刪除方式
                                else
                                {
                                    string sqlcheck = "SELECT * FROM CARERECORD_DATA WHERE carerecord_id = '" + id + "' AND SELF = '" + self + "' AND CREATTIME <to_date('2023-11-21 12:00:00','yyyy-MM-dd HH24:mi:ss')";
                                    DataTable Dtck = link.DBExecSQL(sqlcheck);
                                    if (Dtck.Rows.Count > 0)
                                    {
                                        int result = del_emr(id + self, userNO);
                                    }
                                }
                            }
                            //if (del_emrfile)
                            //{
                            //}
                            //int delEMR = del_emr_pack(id, userinfo.EmployeesNo);
                        }
                        catch (Exception ex)
                        {
                            //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                            string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                            string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                            write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, id + self.ToString(), "DBExecSQL", sql, ex);
                        }
                    }

                }

                return erow;
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), "DBExecSQL", sql, ex);
                return erow;
            }
            finally
            {
                this.link.DBClose();
            }
        }
        /// <summary>
        /// 刪除護理紀錄
        /// </summary>
        /// <param name="id">P_KEY</param>
        public int Del_CareRecordTns(string id, string self, ref DBConnector link, string NCid = "")
        {
            int erow = 0; string where = "";
            try
            {
                if (Session["PatInfo"] != null && (!string.IsNullOrWhiteSpace(id) || !string.IsNullOrWhiteSpace(self)))
                {
                    List<DBItem> insertDataList = new List<DBItem>();
                    insertDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("DELETED", userinfo.EmployeesNo, DBItem.DBDataType.String));

                    id = (string.IsNullOrEmpty(NCid)) ? id : NCid;
                    where = "0 = 0 "
                        + ((!string.IsNullOrWhiteSpace(id)) ? "AND CARERECORD_ID = '" + id + "' " : "")
                        + ((!string.IsNullOrWhiteSpace(self)) ? "AND UPPER(SELF) = '" + self.ToUpper() + "' " : "");

                    erow = link.DBExecUpdateTns("CARERECORD_DATA", insertDataList, where);
                    if (self != "CAREPLANMASTER")
                    {
                        if (erow > 0)
                        {
                            //del_emr(id + self, userinfo.EmployeesNo);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), "DBExecSQL", where, ex);
            }
            finally
            {
                //Tns不關連線
            }
            return erow;
        }

        #endregion

        #region I/O
        /// <summary>
        /// 帶入IO
        /// </summary>
        /// <param name="time">紀錄時間</param>
        /// <param name="id">ID</param>
        /// <param name="typeid">種類序號</param>
        /// <param name="itemid">項目序號</param>
        /// <param name="amount">數量</param>
        /// <param name="amount_unit">單位(1:mL  2:g 3:mg)</param>
        public int Insert_IO_DATA(string time, string id, string typeid, string itemid, string amount, string amount_unit, string source_id, string source_tablename, string explanation_item = "")
        {
            int erow = 0;
            if (Session["PatInfo"] != null)
            {
                //DBConnector dbconnector = new DBConnector();
                string userno = userinfo.EmployeesNo;
                string feeno = ptinfo.FeeNo;
                string DATA_SOURCE = "NIS";
                if (id.Substring(0, 3) == "OBS")
                    DATA_SOURCE = "OBS";
                else
                    DATA_SOURCE = "NIS";
                List<DBItem> insertDataList = new List<DBItem>();
                insertDataList.Add(new DBItem("IO_ROW", "IO_DATA_SEQUENCE.NEXTVAL", DBItem.DBDataType.Number));
                insertDataList.Add(new DBItem("IO_ID", id, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREANO", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREANAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATTIME", time, DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("TYPEID", typeid, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ITEMID", itemid, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("AMOUNT", amount, DBItem.DBDataType.Number));
                insertDataList.Add(new DBItem("AMOUNT_UNIT", amount_unit, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EXPLANATION_ITEM", explanation_item, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("SOURCE_ID", source_id, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("SOURCE", source_tablename, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("DATA_SOURCE", DATA_SOURCE, DBItem.DBDataType.String));

                erow = link.DBExecInsert("IO_DATA", insertDataList);
            }
            return erow;
        }

        #region 連動修改I/O
        /// <summary>
        /// 修改 IO
        /// </summary>
        /// <param name="time">紀錄時間</param>
        /// <param name="id">ID</param>
        /// <param name="typeid">種類序號</param>
        /// <param name="itemid">項目序號</param>
        /// <param name="amount">數量</param>
        /// <param name="amount_unit">單位(1:mL  2:g 3:mg)</param>
        public int Update_IO_DATA(string time, string id, string typeid, string itemid, string amount, string amount_unit, string source_id, string source_tablename, string explanation_item = "")
        {
            int erow = 0;
            if (Session["PatInfo"] != null)
            {
                //DBConnector dbconnector = new DBConnector();
                string userno = userinfo.EmployeesNo;
                string feeno = ptinfo.FeeNo;
                List<DBItem> insertDataList = new List<DBItem>();
                //insertDataList.Add(new DBItem("CREANO", userno, DBItem.DBDataType.String));
                //insertDataList.Add(new DBItem("CREANAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATTIME", time, DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("TYPEID", typeid, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ITEMID", itemid, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("AMOUNT", amount, DBItem.DBDataType.Number));
                insertDataList.Add(new DBItem("AMOUNT_UNIT", amount_unit, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EXPLANATION_ITEM", explanation_item, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                //insertDataList.Add(new DBItem("SOURCE_ID", source_id, DBItem.DBDataType.String));
                //insertDataList.Add(new DBItem("SOURCE", source_tablename, DBItem.DBDataType.String));
                string where = "FEENO = '" + feeno + "' AND SOURCE_ID = '" + source_id + "' AND SOURCE = '" + source_tablename + "' ";
                erow = link.DBExecUpdate("IO_DATA", insertDataList, where);
            }
            return erow;
        }
        #endregion

        /// <summary>
        /// 刪除IO
        /// </summary>
        /// <param name="time">紀錄時間</param>
        /// <param name="typeid">種類序號</param>
        /// <param name="itemid">項目序號</param>
        /// <param name="amount">數量</param>
        /// <param name="amount_unit">單位(1:mL  2:g 3:mg)</param>
        public int Delete_IO_DATA(string source_id, string source_tablename)
        {
            int erow = 0;
            if (Session["PatInfo"] != null)
            {
                //DBConnector dbconnector = new DBConnector();
                string userno = userinfo.EmployeesNo;
                string feeno = ptinfo.FeeNo;

                List<DBItem> insertDataList = new List<DBItem>();
                insertDataList.Add(new DBItem("DELETED", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));

                string where = "FEENO = '" + ptinfo.FeeNo + "' AND SOURCE_ID = '" + source_id + "' AND SOURCE = '" + source_tablename + "' ";
                erow = link.DBExecUpdate("IO_DATA", insertDataList, where);
            }
            return erow;
        }


        #region --IO，顏色、氣味、形狀--
        /// <summary>
        /// 新增IO，副加形容(顏色、氣味、形狀)
        /// </summary>
        /// <param name="id">IO的ID</param>
        /// <param name="color_drainage">顏色</param>
        /// <param name="color_other">顏色的其他</param>
        /// <param name="nature_drainage">性狀</param>
        /// <param name="nature_other">性狀的其他</param>
        /// <param name="taste_drainage">氣味</param>
        /// <param name="taste_other">氣味的其他</param>
        /// <returns></returns>
        public int Insert_IO_Additional(string id, string color_drainage, string color_other, string nature_drainage, string nature_other, string taste_drainage, string taste_other)
        {
            int erow = 0;
            if (Session["PatInfo"] != null && id != "")
            {
                //DBConnector dbconnector = new DBConnector();
                List<DBItem> insertDataList = new List<DBItem>();
                insertDataList.Clear();
                insertDataList.Add(new DBItem("FEATUREID", id, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("COLORID", color_drainage, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("COLOROTHER", (color_other == null) ? "" : color_other.Trim(), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("NATUREID", nature_drainage, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("NATUREOTHER", (nature_other == null) ? "" : nature_other.Trim(), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("TASTEID", taste_drainage, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("TASTEOTHER", (taste_other == null) ? "" : taste_other.Trim(), DBItem.DBDataType.String));
                erow = link.DBExecInsert("TUBE_FEATURE", insertDataList);
                insertDataList.Clear();
            }
            return erow;
        }



        /// <summary>
        /// 更新IO，副加形容(顏色、氣味、形狀)
        /// </summary>
        /// <param name="id"></param>
        /// <param name="color_drainage"></param>
        /// <param name="color_other"></param>
        /// <param name="nature_drainage"></param>
        /// <param name="nature_other"></param>
        /// <param name="taste_drainage"></param>
        /// <param name="taste_other"></param>
        /// <returns></returns>
        public int Update_IO_Additional(string id, string color_drainage, string color_other, string nature_drainage, string nature_other, string taste_drainage, string taste_other)
        {
            int erow = 0;
            if (Session["PatInfo"] != null && id != "")
            {
                //DBConnector dbconnector = new DBConnector();
                List<DBItem> insertDataList = new List<DBItem>();
                insertDataList.Clear();
                //insertDataList.Add(new DBItem("FEATUREID", id, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("COLORID", color_drainage, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("COLOROTHER", (color_other == null) ? "" : color_other.Trim(), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("NATUREID", nature_drainage, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("NATUREOTHER", (nature_other == null) ? "" : nature_other.Trim(), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("TASTEID", taste_drainage, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("TASTEOTHER", (taste_other == null) ? "" : taste_other.Trim(), DBItem.DBDataType.String));
                erow = link.DBExecUpdate("TUBE_FEATURE", insertDataList, "FEATUREID='" + id + "'");
                insertDataList.Clear();
            }
            return erow;
        }
        #endregion


        #endregion

        /// <summary>
        /// 轉民國年
        /// </summary>
        /// <param name="content">內容</param>
        public string trans_date(string content)
        {
            //int start = 0;
            //if (content != null)
            //{
            //    while (start < content.Length)
            //    {
            //        int first = content.IndexOf('/', start);
            //        try
            //        {
            //            if (first > -1 && (content.IndexOf('/', first + 1, 2) > -1 || content.IndexOf('/', first + 1, 3) > -1))
            //            {
            //                try
            //                {
            //                    try
            //                    {
            //                        string date_first = content.Substring(first - 4, 10);
            //                        DateTime new_date = Convert.ToDateTime(date_first);
            //                        int year = new_date.AddYears(-1911).Year;
            //                        string new_day = year.ToString() + new_date.ToString("/MM/dd");
            //                        content = content.Replace(date_first.Trim(), new_day);
            //                        start = first - 4 + 10;
            //                    }
            //                    catch
            //                    {
            //                        try
            //                        {
            //                            string date_second = content.Substring(first - 4, 9);
            //                            DateTime new_date = Convert.ToDateTime(date_second);
            //                            int year = new_date.AddYears(-1911).Year;
            //                            string new_day = year.ToString() + new_date.ToString("/MM/dd");
            //                            content = content.Replace(date_second.Trim(), new_day);
            //                            start = first - 4 + 9;
            //                        }
            //                        catch
            //                        {
            //                            try
            //                            {
            //                                string date_third = content.Substring(first - 4, 8);
            //                                DateTime new_date = Convert.ToDateTime(date_third);
            //                                int year = new_date.AddYears(-1911).Year;
            //                                string new_day = year.ToString() + new_date.ToString("/MM/dd");
            //                                content = content.Replace(date_third.Trim(), new_day);
            //                                start = first - 4 + 8;
            //                            }
            //                            catch
            //                            {
            //                                start = first - 4 + 8;
            //                            }
            //                        }
            //                    }
            //                }
            //                catch
            //                {
            //                    start = content.Length;
            //                }
            //            }
            //            else
            //            {
            //                start = content.Length;
            //            }
            //        }
            //        catch
            //        {
            //            start = content.Length;
            //        }
            //    }
            //}
            return content;
        }
        public string GetSourceUrl()
        {
            string Port = System.Web.HttpContext.Current.Request.ServerVariables["SERVER_PORT"];
            if (Port == null || Port == "80" || Port == "443")
                Port = "";
            else
                Port = ":" + Port;

            string Protocol = System.Web.HttpContext.Current.Request.ServerVariables["SERVER_PORT_SECURE"];
            if (Protocol == null || Protocol == "0")
                Protocol = "http://";
            else
                Protocol = "https://";

            string SourceUrl = Protocol + System.Web.HttpContext.Current.Request.ServerVariables["SERVER_NAME"] + Port + System.Web.HttpContext.Current.Request.ApplicationPath;

            if (SourceUrl.EndsWith("/"))
            {
                SourceUrl = SourceUrl.Substring(0, SourceUrl.Length - 1);
            }
            return SourceUrl;
        }



        #region 取得動態入評內容

        /// <summary> 取得護理評估內容 </summary>
        public string getNaInfo(string na_type, string na_item_name)
        {
            string sql = string.Empty;
            try
            {
                string retStr = string.Empty;

                sql = " SELECT DTL_VALUE FROM NIS_DATA_NARECORD_DETAIL  ";
                sql += " WHERE DTL_SNO = (SELECT DTL_SNO FROM NIS_DATA_NARECORD_MASTER WHERE FEE_NO = '" + ptinfo.FeeNo + "' AND NA_TYPE='" + na_type + "'";
                sql += " AND RECORD_DATE = (SELECT MAX(RECORD_DATE) FROM NIS_DATA_NARECORD_MASTER WHERE FEE_NO = '" + ptinfo.FeeNo + "' AND NA_TYPE = '" + na_type + "')) ";
                sql += " AND DTL_ID IN ( SELECT DTL_ID FROM SYS_NADTL WHERE DTL_PARENT_ID is null and NA_ID =  ";
                sql += " (SELECT Distinct NA_ID FROM NIS_DATA_NARECORD_MASTER WHERE FEE_NO = '" + ptinfo.FeeNo + "' AND NA_TYPE='" + na_type + "') ";
                sql += " AND DTL_TITLE = '" + na_item_name + "') ";

                DataTable Dt = link.DBExecSQL(sql);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        retStr = Dt.Rows[i]["DTL_VALUE"].ToString().Trim();
                    }
                }

                return retStr;
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), "DBExecSQL", sql, ex);
                return "";
            }
            finally
            {
                this.link.DBClose();
            }
        }

        /// <summary> 取得護理評估子選項內容 </summary>
        public string getNaInfo_sub(string na_type, string na_item_name)
        {
            string sql = string.Empty, sub_sql = "";
            try
            {
                string retStr = string.Empty;
                string dtl_idno = string.Empty;
                //DBConnector link = new DBConnector();

                sql = " SELECT DTL_ID FROM NIS_DATA_NARECORD_DETAIL  ";
                sql += " WHERE DTL_SNO = (SELECT DTL_SNO FROM NIS_DATA_NARECORD_MASTER WHERE FEE_NO = '" + ptinfo.FeeNo + "' AND NA_TYPE='" + na_type + "'";
                sql += " AND RECORD_DATE = (SELECT MAX(RECORD_DATE) FROM NIS_DATA_NARECORD_MASTER WHERE FEE_NO = '" + ptinfo.FeeNo + "' AND NA_TYPE = '" + na_type + "')) ";
                sql += " AND DTL_ID IN ( SELECT DTL_ID FROM SYS_NADTL WHERE DTL_PARENT_ID is null and NA_ID =  ";
                sql += " (SELECT Distinct NA_ID FROM NIS_DATA_NARECORD_MASTER WHERE FEE_NO = '" + ptinfo.FeeNo + "' AND NA_TYPE='" + na_type + "') ";
                sql += " AND DTL_TITLE = '" + na_item_name + "') ";
                DataTable Dt = link.DBExecSQL(sql);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        dtl_idno = Dt.Rows[i]["DTL_ID"].ToString().Trim();//.Substring(1);
                    }
                }
                //取子選項
                sub_sql = " SELECT DTL_VALUE FROM NIS_DATA_NARECORD_DETAIL  ";
                sub_sql += "where DTL_ID = ";
                sub_sql += "(select DTL_ID from SYS_NADTL where DTL_PARENT_ID ='" + dtl_idno + "' and DTL_TYPE = 'C') and ";
                sub_sql += "DTL_SNO = (SELECT DTL_SNO FROM NIS_DATA_NARECORD_MASTER WHERE FEE_NO = '" + ptinfo.FeeNo + "' AND NA_TYPE='" + na_type + "')  ";

                Dt = link.DBExecSQL(sub_sql);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        retStr = Dt.Rows[i]["DTL_VALUE"].ToString().Trim();
                    }
                }
                return retStr;
            }
            catch (Exception ex)
            {   //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), "DBExecSQL", sql, ex);
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), "DBExecSQL", sub_sql, ex);
                return "";
            }
            finally
            {
                this.link.DBClose();
            }
        }

        /// <summary> 取得護理評估子選項內容 </summary>
        public string getNaInfo_sub_title(string na_type, string na_item_name)
        {
            string sql = string.Empty;
            try
            {
                string retStr = string.Empty;
                string dtl_idno = string.Empty;
                //DBConnector link = new DBConnector();


                sql = " SELECT DTL_VALUE FROM NIS_DATA_NARECORD_DETAIL  ";
                sql += " WHERE DTL_SNO = (SELECT DTL_SNO FROM NIS_DATA_NARECORD_MASTER WHERE FEE_NO = '" + ptinfo.FeeNo + "' AND NA_TYPE='" + na_type + "'";
                sql += " AND RECORD_DATE = (SELECT MAX(RECORD_DATE) FROM NIS_DATA_NARECORD_MASTER WHERE FEE_NO = '" + ptinfo.FeeNo + "' AND NA_TYPE = '" + na_type + "')) ";
                sql += " AND DTL_ID IN ( SELECT DTL_ID FROM SYS_NADTL WHERE NA_ID =  ";
                sql += " (SELECT DISTINCT(NA_ID) FROM NIS_DATA_NARECORD_MASTER WHERE FEE_NO = '" + ptinfo.FeeNo + "' AND NA_TYPE='" + na_type + "') ";
                sql += " AND DTL_TITLE = '" + na_item_name + "') ";

                DataTable Dt = link.DBExecSQL(sql);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        retStr = Dt.Rows[i]["DTL_VALUE"].ToString().Trim();
                    }
                }
                return retStr;
            }
            catch (Exception ex)
            {//寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), "DBExecSQL", sql, ex);
                return "";
            }
            finally
            {
                this.link.DBClose();
            }
        }

        #endregion

        #region 取得靜態入評內容

        public DataTable sel_assess_data(string feeno, string natype, string[] item_id = null)
        {
            string sql = "";
            DataTable dt = new DataTable();
            try
            {
                //DBConnector link = new DBConnector();
                sql = "SELECT * FROM ASSESSMENTDETAIL WHERE TABLEID = ( ";
                sql += "SELECT TABLEID FROM (SELECT * FROM ASSESSMENTMASTER WHERE 0 = 0 ";
                if (feeno != "")
                    sql += "AND FEENO = '" + feeno + "' ";
                if (natype != "")
                    sql += "AND NATYPE = '" + natype + "' ";
                sql += "AND STATUS IN('insert','update') ORDER BY MODIFYTIME DESC) WHERE rownum <= 1) ";
                if (item_id != null)
                    sql += "AND ITEMID  IN ('" + String.Join("','", item_id) + "') ";

                link.DBExecSQL(sql, ref dt);
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), "DBExecSQL", sql, ex);
            }
            finally
            {
                this.link.DBClose();
            }

            return dt;
        }

        /// <summary>
        /// 取得評估項目的值
        /// </summary>
        /// <param name="id">欲搜尋ID</param>
        public string sel_data(DataTable dt, string id)
        {
            string value = "";
            foreach (DataRow r in dt.Rows)
            {
                if (r["ITEMID"].ToString() == id)
                    value = r["ITEMVALUE"].ToString();
            }
            return trans_special_code(value);
        }

        public string sel_data(DataTable dt, string id, string columnName)
        {
            string ColName = "";
            string value = "";
            ColName = columnName;
            foreach (DataRow r in dt.Rows)
            {
                if (r[ColName].ToString() == id)
                    value = r[ColName].ToString();
            }
            return trans_special_code(value);
        }
        private string trans_special_code(string content)
        {
            if (content != null)
            {
                content = content.Trim();
                content = content.Replace("&", "&amp;");
                content = content.Replace("<", "&lt;");
                content = content.Replace(">", "&gt;");
                content = content.Replace("'", "&apos;");
                content = content.Replace("\"", "&quot;");

                content = content.Replace("\u0001", "");
                content = content.Replace("\u000B", "");
                content = content.Replace("\r\n", "&#xD;&#xA;");
                content = content.Replace("\n", "&#xA;");
                return content;
            }
            else
                return "";
        }
        #endregion

        #region 簽章轉捷格
        /// <summary>
        /// 簽章轉捷格
        /// </summary>
        /// <param name="tableid">文件id</param>
        /// <param name="feeno">批價序號</param>
        /// <param name="signature_no">簽章單張</param>
        /// <param name="signature_name">簽章名稱</param>
        /// <param name="userno">員編</param>
        /// <param name="content">內容</param>
        public int emr_sign(string pkey, string fee_no, string doc_type, string emp_no, string emp_name, string emp_id, string dep_no, string chr_no, string pat_name, string in_date, string charge_type, string filename)
        {
            try
            {
                JagEMRReference.Service1 jag = new JagEMRReference.Service1();
                jag.Url = ConfigurationManager.AppSettings["JagEMRReference"].ToString();

                string orderstr = string.Empty;
                string base64str = string.Empty;

                orderstr = get_xml_str(emp_no, emp_name, emp_id, dep_no, pkey, doc_type, fee_no, chr_no, pat_name, in_date, charge_type);

                byte[] pdfbyte = System.IO.File.ReadAllBytes(@"" + filename);
                base64str = Convert.ToBase64String(pdfbyte);
                //int result = jag.UploadEMRFile(orderstr, base64str);
                System.IO.File.Delete(@"" + filename);
                //return result;
                return 4;

            }
            catch (Exception)
            {
                return 4;
            }

        }

        public string emr_delete(string pk, string empno)
        {
            string result = string.Empty;
            NIS.EMR_D.Service1 emr = new NIS.EMR_D.Service1();
            result = emr.remark_request_DocParent("D", "報告已重新登打送簽", GetMd5Hash(pk), empno);
            return result;

        }

        public int del_emr(string pk, string empno)
        {
            try
            {
                if (empno == "")
                {
                    System.IO.File.WriteAllLines(@"C:\EMR\Delete\" + userinfo.EmployeesNo + "_" + GetMd5Hash(pk) + ".txt", new string[] { });
                }
                else
                {
                    System.IO.File.WriteAllLines(@"C:\EMR\Delete\" + empno + "_" + GetMd5Hash(pk) + ".txt", new string[] { });

                }
            }
            catch (System.IO.DirectoryNotFoundException e)
            {
                Console.Write(e);
            }
            return 0;
        }

        //20230626 新增批次EMR刪除功能
        public int del_emr_pack(string pk, string empno)
        {
            try
            {
                string sql = "";
                Assess ass_m = new Assess();
                sql = "SELECT * FROM NIS_EMR_PACKAGE_DTL WHERE RECORD_KEY = '" + pk + "' AND STATUS = 'Y'";
                DataTable DtP = link.DBExecSQL(sql);

                if (DtP.Rows.Count > 0)
                {
                    List<DBItem> insertDataList = new List<DBItem>();
                    insertDataList.Add(new DBItem("STATUS", "D", DBItem.DBDataType.String));
                    int erow = ass_m.DBExecUpdate("NIS_EMR_PACKAGE_DTL", insertDataList, " RECORD_KEY = '" + pk + "' ");
                }
            }
            catch (System.IO.DirectoryNotFoundException e)
            {
                Console.Write(e);
            }
            return 0;
        }

        public string get_xml_str(string emp_no, string emp_name, string emp_id, string dep_no, string pkey, string doc_type, string fee_no, string chr_no, string pat_name, string in_date, string charge_type)
        {
            string xml = string.Empty;
            xml += "<RequestDate>" + DateTime.Now.ToString("yyyyMMdd") + "</RequestDate>";
            xml += "<RequestTime>" + DateTime.Now.ToString("HHmmss") + "</RequestTime>";
            xml += "<RequestUser>" + emp_no.ToString() + "</RequestUser>";
            xml += "<RequestUserName>" + emp_name.ToString() + "</RequestUserName>";
            xml += "<UserIDNO>" + emp_id + "</UserIDNO>";
            xml += "<RequestDivision>" + dep_no.Trim() + "</RequestDivision>";
            xml += "<FileName>" + pkey + ".pdf</FileName>";
            xml += "<SignSystem>NIS</SignSystem>";
            xml += "<RequestDocType>" + doc_type + "</RequestDocType>";
            xml += "<RequestDocDate>" + DateTime.Now.ToString("yyyyMMdd") + "</RequestDocDate>";
            xml += "<RequestDocTime>" + DateTime.Now.ToString("HHmmss") + "</RequestDocTime>";
            xml += "<RequestDocRoot>" + fee_no + "</RequestDocRoot>";
            xml += "<RequestDocParent>" + fee_no + "</RequestDocParent>";
            xml += "<RequestDocNo>" + pkey + "</RequestDocNo>";
            xml += "<RequestPatientID>" + chr_no + "</RequestPatientID>";
            xml += "<RequestPatinetName>" + pat_name + "</RequestPatinetName>";
            xml += "<VisitDate>" + in_date + "</VisitDate>";
            xml += "<Category>I</Category>";
            xml += "<DocCharge>" + charge_type + "</DocCharge>";
            xml += "<InHospital>1</InHospital>";
            xml += "<DischargeDate>        </DischargeDate>";

            //xml += "<RequestDate>20100303</RequestDate>";
            //xml += "<RequestTime>075614</RequestTime>";
            //xml += "<RequestUser>A1001</RequestUser>";
            //xml += "<RequestUserName>林小明醫師</RequestUserName>";
            //xml += "<UserIDNO>B001790309</UserIDNO>";
            //xml += "<RequestDivision>11</RequestDivision>";
            //xml += "<FileName>IN201003031533001234.pdf</FileName>";
            //xml += "<SignSystem>IPD</SignSystem>";
            //xml += "<RequestDocType>18842-5</RequestDocType>";
            //xml += "<RequestDocDate>20100303</RequestDocDate>";
            //xml += "<RequestDocTime>082838</RequestDocTime>";
            //xml += "<RequestDocRoot>IN1206120034</RequestDocRoot>";
            //xml += "<RequestDocParent>IN1206120034</RequestDocParent>";
            //xml += "<RequestDocNo>IN1206120034L18842501</RequestDocNo>";
            //xml += "<RequestPatientID>0012345</RequestPatientID>";
            //xml += "<RequestPatinetName>王明雄</RequestPatinetName>";
            //xml += "<VisitDate>20100302</VisitDate>";
            //xml += "<Category>I</Category>";
            //xml += "<DocCharge>000</DocCharge>";
            //xml += "<InHospital>1</InHospital>";
            //xml += "<DischargeDate>20100303</DischargeDate>";

            return xml;
        }

        /// <summary>
        /// 電子簽驗章程式是由參數檔之內容來進行建立電子病歷簽驗章索引及儲存相關資料
        /// </summary>
        /// <param name="TranID">醫療相關系統呼叫電子簽驗章程式之交易碼</param>
        /// <param name="RDate">醫療相關系統呼叫電子簽驗章程式之日期(HIS系統日期)</param>
        /// <param name="RTime">醫療相關系統呼叫電子簽驗章程式之時間(HIS系統時間)</param>
        /// <param name="RNo">此病歷文件序號</param>
        /// <param name="RDocType">病歷文件類別代碼</param>
        /// <param name="RDocDate">此病歷文件之初次產生日期(就診日期)</param>
        /// <param name="RDocRoot">此病歷文件之就醫序號</param>
        /// <param name="RDocParent">此病歷文件之就診帳號</param>
        /// <param name="RDocOrderDate">此病歷文件之開單日期</param>
        /// <param name="RDocOrderDiv">此病歷文件之開單科別</param>
        /// <param name="RDocPerformDate">此病歷文件之採檢日期(非檢驗類報告可不填)</param>
        /// <param name="RDocReportDate">此病歷文件之完成日期(非檢驗類報告可不填)</param>
        /// <param name="RDivision">簽屬此病歷文件之醫護人員所屬科別代號</param>
        /// <param name="RUser">簽屬此病歷文件之醫護人員編號ID(醫師或其他醫護人員)</param>
        /// <param name="RUserName">簽屬此病歷文件之醫護人員姓名(醫師或其他醫護人員)</param>
        /// <param name="UserIDNO">簽屬此病歷文件之醫護人員身分證字號(醫師或其他醫護人員)</param>
        /// <param name="RPatientID">此病歷文件之病歷號</param>
        /// <param name="RPatinetName">此病歷文件之病患姓名</param>
        /// <param name="CitizenID">此病歷文件之病患身份證字號</param>
        /// <param name="RIOEType">就診身份此病歷文件之診別(I:住院  O:門診  E:急診  H:健檢)</param>
        /// <param name="RChargeType">此病歷文件之病患就診身份</param>
        /// <param name="RType">參數種類</param>
        /// <param name="SignSystem">產生此病歷文件之醫療相關系統代碼</param>
        /// <param name="FileMove">是否上傳病歷文件檔</param>
        /// <param name="FilePath">此病歷文件檔資料夾路徑</param>
        /// <param name="FileName">此病歷文件檔名稱</param>
        /// <returns>xml文字</returns>
        public string get_xml(string TranID, string RNo, string RDocType, string RDocDate, string RDocParent, string RDocOrderDate,
            string RDocPerformDate, string RDocReportDate, string RUser, string RUserName, string UserIDNO, string RPatientID, string RPatinetName, string CitizenID,
            string RChargeType, string FilePath, string FileName, string listJsonArray = "", string xml_type = "")
        {
            string xml = string.Empty;
            xml += "<TransactionID>" + TranID + "</TransactionID>";
            xml += "<RequestDate>" + DateTime.Now.ToString("yyyyMMdd") + "</RequestDate>";
            xml += "<RequestTime>" + DateTime.Now.ToString("HHmmss") + "</RequestTime>";
            xml += "<RequestNo>" + RNo + "</RequestNo>";
            xml += "<RequestDocType>" + RDocType + "</RequestDocType>";
            xml += "<RequestDocDate>" + ptinfo.InDate.ToString("yyyyMMdd") + "</RequestDocDate>";
            xml += "<RequestDocRoot>" + ptinfo.FeeNo + "</RequestDocRoot>";
            xml += "<RequestDocParent>" + RDocParent + "</RequestDocParent>";
            xml += "<RequestDocOrderDate>" + RDocOrderDate + "</RequestDocOrderDate>";
            xml += "<RequestOrderDiv>" + userinfo.CostCenterCode + "</RequestOrderDiv>";
            xml += "<RequestDocPerformDate>" + RDocPerformDate + "</RequestDocPerformDate>";
            xml += "<RequestDocReportDate>" + RDocReportDate + "</RequestDocReportDate>";
            xml += "<RequestDivision>" + userinfo.Guider_CCCode + "</RequestDivision>";
            xml += "<RequestUser>" + RUser.ToString() + "</RequestUser>";
            xml += "<RequestUserName>" + RUserName.ToString() + "</RequestUserName>";
            xml += "<UserIDNO>" + UserIDNO + "</UserIDNO>";
            xml += "<RequestPatientID>" + RPatientID + "</RequestPatientID>";
            xml += "<RequestPatinetName>" + RPatinetName + "</RequestPatinetName>";
            xml += "<CitizenID>" + CitizenID + "</CitizenID>";
            xml += "<RequestIOEType>" + "I" + "</RequestIOEType>";
            xml += "<RequestChargeType>" + RChargeType + "</RequestChargeType>";
            xml += "<RequestType>" + "Copy" + "</RequestType>";
            xml += "<SignSystem>" + "NIS" + "</SignSystem>";
            xml += "<FileMove>" + "Y" + "</FileMove>";
            xml += "<FilePath>" + FilePath + "</FilePath>";//C:\\EMR\\IPD\\ 
            xml += "<FileName>" + FileName + "</FileName>";// DISN- D001213013 20120313165503.xml
            //DBConnector dbconnector = new DBConnector();
            List<DBItem> ListDBItem = new List<DBItem>();
            LogTool lt = new LogTool();
            string US_JSON = JsonConvert.SerializeObject(userinfo);

            try
            {
                ListDBItem.Add(new DBItem("ORIGINAL_ID", listJsonArray, DBItem.DBDataType.String));
                ListDBItem.Add(new DBItem("HASH_ID", "代簽人：" + userinfo.Guider, DBItem.DBDataType.String));
                ListDBItem.Add(new DBItem("FOLDER_SORT", xml_type, DBItem.DBDataType.String));
                ListDBItem.Add(new DBItem("XML_NAME", xml, DBItem.DBDataType.String));
                ListDBItem.Add(new DBItem("RECORD_TIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                ListDBItem.Add(new DBItem("UPLOAD_STATUS", (string.IsNullOrEmpty(userinfo.Guider_CCCode)) ? "N" : "Y", DBItem.DBDataType.String));
                ListDBItem.Add(new DBItem("CREATE_USER", "資料建立人：" + userinfo.EmployeesNo, DBItem.DBDataType.String));
                ListDBItem.Add(new DBItem("FILE_EXIST", userinfo.Guider_CCCode, DBItem.DBDataType.String));
                link.DBExecInsert("EMR_LOG_TABLE", ListDBItem);

            }
            catch (Exception ex)
            {//寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                lt.saveLogMsg(US_JSON + "\n xml:" + xml, "EMRLogFail_get_xml");
            }
            return xml;
        }

        /// <summary>
        /// 電子簽驗章程式是由參數檔之內容來進行建立電子病歷簽驗章索引及儲存相關資料批次
        /// </summary>
        /// <param name="TranID">醫療相關系統呼叫電子簽驗章程式之交易碼</param>
        /// <param name="RDate">醫療相關系統呼叫電子簽驗章程式之日期(HIS系統日期)</param>
        /// <param name="RTime">醫療相關系統呼叫電子簽驗章程式之時間(HIS系統時間)</param>
        /// <param name="RNo">此病歷文件序號</param>
        /// <param name="RDocType">病歷文件類別代碼</param>
        /// <param name="RDocDate">此病歷文件之初次產生日期(就診日期)</param>
        /// <param name="RDocRoot">此病歷文件之就醫序號</param>
        /// <param name="RDocParent">此病歷文件之就診帳號</param>
        /// <param name="RDocOrderDate">此病歷文件之開單日期</param>
        /// <param name="RDocOrderDiv">此病歷文件之開單科別</param>
        /// <param name="RDocPerformDate">此病歷文件之採檢日期(非檢驗類報告可不填)</param>
        /// <param name="RDocReportDate">此病歷文件之完成日期(非檢驗類報告可不填)</param>
        /// <param name="RDivision">簽屬此病歷文件之醫護人員所屬科別代號</param>
        /// <param name="RUser">簽屬此病歷文件之醫護人員編號ID(醫師或其他醫護人員)</param>
        /// <param name="RUserName">簽屬此病歷文件之醫護人員姓名(醫師或其他醫護人員)</param>
        /// <param name="UserIDNO">簽屬此病歷文件之醫護人員身分證字號(醫師或其他醫護人員)</param>
        /// <param name="RPatientID">此病歷文件之病歷號</param>
        /// <param name="RPatinetName">此病歷文件之病患姓名</param>
        /// <param name="CitizenID">此病歷文件之病患身份證字號</param>
        /// <param name="RIOEType">就診身份此病歷文件之診別(I:住院  O:門診  E:急診  H:健檢)</param>
        /// <param name="RChargeType">此病歷文件之病患就診身份</param>
        /// <param name="RType">參數種類</param>
        /// <param name="SignSystem">產生此病歷文件之醫療相關系統代碼</param>
        /// <param name="FileMove">是否上傳病歷文件檔</param>
        /// <param name="FilePath">此病歷文件檔資料夾路徑</param>
        /// <param name="FileName">此病歷文件檔名稱</param>
        /// <returns>xml文字</returns>
        public string get_xml_new(string TranID, string RNo, string RDocType, string RDocDate, string RDocParent, string RDocOrderDate,
            string RDocPerformDate, string RDocReportDate, string RUser, string RUserName, string UserIDNO, string RPatientID, string RPatinetName, string CitizenID,
            string RChargeType, string FilePath, string FileName, string listJsonArray = "", string xml_type = "", string InDate = "", string FeeNo = "")
        {
            string US_JSON = "";
            //取得應簽章人員
            UserInfo user_info = new UserInfo();
            byte[] listByteCode = webService.UserName(RUser);
            if (listByteCode != null)
            {
                string listJsonArrayPT = CompressTool.DecompressString(listByteCode);
                user_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArrayPT);
                US_JSON = JsonConvert.SerializeObject(user_info);

            }


            string xml = string.Empty;
            xml += "<TransactionID>" + TranID + "</TransactionID>";
            xml += "<RequestDate>" + DateTime.Now.ToString("yyyyMMdd") + "</RequestDate>";
            xml += "<RequestTime>" + DateTime.Now.ToString("HHmmss") + "</RequestTime>";
            xml += "<RequestNo>" + RNo + "</RequestNo>";
            xml += "<RequestDocType>" + RDocType + "</RequestDocType>";
            xml += "<RequestDocDate>" + InDate + "</RequestDocDate>";
            xml += "<RequestDocRoot>" + FeeNo + "</RequestDocRoot>";
            xml += "<RequestDocParent>" + RDocParent + "</RequestDocParent>";
            xml += "<RequestDocOrderDate>" + RDocOrderDate + "</RequestDocOrderDate>";
            xml += "<RequestOrderDiv>" + user_info.CostCenterCode + "</RequestOrderDiv>";
            xml += "<RequestDocPerformDate>" + RDocPerformDate + "</RequestDocPerformDate>";
            xml += "<RequestDocReportDate>" + RDocReportDate + "</RequestDocReportDate>";
            xml += "<RequestDivision>" + user_info.CostCenterCode + "</RequestDivision>";
            xml += "<RequestUser>" + RUser.ToString() + "</RequestUser>";
            xml += "<RequestUserName>" + RUserName.ToString() + "</RequestUserName>";
            xml += "<UserIDNO>" + UserIDNO + "</UserIDNO>";
            xml += "<RequestPatientID>" + RPatientID + "</RequestPatientID>";
            xml += "<RequestPatinetName>" + RPatinetName + "</RequestPatinetName>";
            xml += "<CitizenID>" + CitizenID + "</CitizenID>";
            xml += "<RequestIOEType>" + "I" + "</RequestIOEType>";
            xml += "<RequestChargeType>" + RChargeType + "</RequestChargeType>";
            xml += "<RequestType>" + "Copy" + "</RequestType>";
            xml += "<SignSystem>" + "NIS" + "</SignSystem>";
            xml += "<FileMove>" + "Y" + "</FileMove>";
            xml += "<FilePath>" + FilePath + "</FilePath>";//C:\\EMR\\IPD\\ 
            xml += "<FileName>" + FileName + "</FileName>";// DISN- D001213013 20120313165503.xml
            //DBConnector dbconnector = new DBConnector();
            List<DBItem> ListDBItem = new List<DBItem>();
            LogTool lt = new LogTool();

            try
            {
                ListDBItem.Add(new DBItem("ORIGINAL_ID", listJsonArray, DBItem.DBDataType.String));
                ListDBItem.Add(new DBItem("HASH_ID", "代簽人：" + user_info.Guider, DBItem.DBDataType.String));
                ListDBItem.Add(new DBItem("FOLDER_SORT", xml_type, DBItem.DBDataType.String));
                ListDBItem.Add(new DBItem("XML_NAME", xml, DBItem.DBDataType.String));
                ListDBItem.Add(new DBItem("RECORD_TIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                ListDBItem.Add(new DBItem("UPLOAD_STATUS", (string.IsNullOrEmpty(user_info.Guider_CCCode)) ? "N" : "Y", DBItem.DBDataType.String));
                ListDBItem.Add(new DBItem("CREATE_USER", "資料建立人：" + user_info.EmployeesNo, DBItem.DBDataType.String));
                ListDBItem.Add(new DBItem("FILE_EXIST", user_info.Guider_CCCode, DBItem.DBDataType.String));
                link.DBExecInsert("EMR_LOG_TABLE", ListDBItem);

            }
            catch (Exception ex)
            {//寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, user_info.EmployeesNo, tmp_action, ex.ToString(), ex);
                lt.saveLogMsg(US_JSON + "\n xml:" + xml, "EMRLogFail_get_xml");
            }
            return xml;
        }

        public string xml_trigger(string TranID, string RNo, string RDocType, string RDocDate, string RDocParent, string RDocOrderDate,
            string RDocPerformDate, string RDocReportDate, string RUser, string RUserName, string UserIDNO, string UserCCCode, string RUserCCCode, string RPatientFeeNo, string RPatientID, string RPatinetName, string CitizenID,
            string RChargeType, string FilePath, string FileName, string listJsonArray = "", string xml_type = "")
        {
            string xml = string.Empty;
            PatientInfo pinfo = new PatientInfo();
            string JosnArr = string.Empty;
            //byte[] ByteCode = webService.GetPatientInfo(RPatientFeeNo);
            ////病人資訊
            //if (ByteCode != null)
            //{
            //    JosnArr = CompressTool.DecompressString(ByteCode);
            //}
            JosnArr = RecursiveWebservice("GetPatientInfo", RPatientFeeNo);
            pinfo = JsonConvert.DeserializeObject<PatientInfo>(JosnArr);

            xml += "<TransactionID>" + TranID + "</TransactionID>";
            xml += "<RequestDate>" + DateTime.Now.ToString("yyyyMMdd") + "</RequestDate>";
            xml += "<RequestTime>" + DateTime.Now.ToString("HHmmss") + "</RequestTime>";
            xml += "<RequestNo>" + RNo + "</RequestNo>";
            xml += "<RequestDocType>" + RDocType + "</RequestDocType>";
            xml += "<RequestDocDate>" + pinfo.InDate.ToString("yyyyMMdd") + "</RequestDocDate>";
            xml += "<RequestDocRoot>" + RPatientFeeNo + "</RequestDocRoot>";
            xml += "<RequestDocParent>" + RDocParent + "</RequestDocParent>";
            xml += "<RequestDocOrderDate>" + RDocOrderDate + "</RequestDocOrderDate>";
            xml += "<RequestOrderDiv>" + UserCCCode + "</RequestOrderDiv>";
            xml += "<RequestDocPerformDate>" + RDocPerformDate + "</RequestDocPerformDate>";
            xml += "<RequestDocReportDate>" + RDocReportDate + "</RequestDocReportDate>";
            xml += "<RequestDivision>" + RUserCCCode + "</RequestDivision>";
            xml += "<RequestUser>" + RUser.ToString() + "</RequestUser>";
            xml += "<RequestUserName>" + RUserName.ToString() + "</RequestUserName>";
            xml += "<UserIDNO>" + UserIDNO + "</UserIDNO>";
            xml += "<RequestPatientID>" + RPatientID + "</RequestPatientID>";
            xml += "<RequestPatinetName>" + RPatinetName + "</RequestPatinetName>";
            xml += "<CitizenID>" + CitizenID + "</CitizenID>";
            xml += "<RequestIOEType>" + "I" + "</RequestIOEType>";
            xml += "<RequestChargeType>" + RChargeType + "</RequestChargeType>";
            xml += "<RequestType>" + "Copy" + "</RequestType>";
            xml += "<SignSystem>" + "NIS" + "</SignSystem>";
            xml += "<FileMove>" + "Y" + "</FileMove>";
            xml += "<FilePath>" + FilePath + "</FilePath>";//C:\\EMR\\IPD\\ 
            xml += "<FileName>" + FileName + "</FileName>";// DISN- D001213013 20120313165503.xml
            //DBConnector dbconnector = new DBConnector();
            List<DBItem> ListDBItem = new List<DBItem>();
            LogTool lt = new LogTool();
            try
            {
                ListDBItem.Add(new DBItem("ORIGINAL_ID", listJsonArray, DBItem.DBDataType.String));
                ListDBItem.Add(new DBItem("HASH_ID", "代簽人：" + RUserName, DBItem.DBDataType.String));
                ListDBItem.Add(new DBItem("FOLDER_SORT", xml_type, DBItem.DBDataType.String));
                ListDBItem.Add(new DBItem("XML_NAME", xml, DBItem.DBDataType.String));
                ListDBItem.Add(new DBItem("RECORD_TIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                ListDBItem.Add(new DBItem("UPLOAD_STATUS", (string.IsNullOrEmpty(RUserCCCode)) ? "N" : "Y", DBItem.DBDataType.String));
                ListDBItem.Add(new DBItem("CREATE_USER", "資料建立人：VIP無此資料", DBItem.DBDataType.String));
                ListDBItem.Add(new DBItem("FILE_EXIST", RUserCCCode, DBItem.DBDataType.String));
                link.DBExecInsert("EMR_LOG_TABLE", ListDBItem);
            }
            catch (Exception ex)
            {//寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, "VIP無此資料", tmp_action, ex.ToString(), ex);
                lt.saveLogMsg(listJsonArray + "\n xml:" + xml, "EMRLogFail_xml_trigger");
            }
            return xml;
        }

        /// <summary>
        /// 電子簽驗章程式是由參數檔之內容來進行建立電子病歷簽驗章索引及儲存相關資料
        /// </summary>
        /// <param name="RDate">醫療相關系統呼叫電子簽驗章程式之日期(HIS系統日期)</param>
        /// <param name="RTime">醫療相關系統呼叫電子簽驗章程式之時間(HIS系統時間)</param>
        /// <param name="RNo">此病歷文件序號</param>
        /// <param name="RDocType">病歷文件類別代碼</param>
        /// <param name="RDocDate">此病歷文件之初次產生日期(就診日期)</param>
        /// <param name="RDocRoot">此病歷文件之就醫序號</param>
        /// <param name="RDocParent">此病歷文件之就診帳號</param>
        /// <param name="RDocOrderDate">此病歷文件之開單日期</param>
        /// <param name="RDocOrderDiv">此病歷文件之開單科別</param>
        /// <param name="RDocPerformDate">此病歷文件之採檢日期(非檢驗類報告可不填)</param>
        /// <param name="RDocReportDate">此病歷文件之完成日期(非檢驗類報告可不填)</param>
        /// <param name="RDivision">簽屬此病歷文件之醫護人員所屬科別代號</param>
        /// <param name="RUser">簽屬此病歷文件之醫護人員編號ID(醫師或其他醫護人員)</param>
        /// <param name="RUserName">簽屬此病歷文件之醫護人員姓名(醫師或其他醫護人員)</param>
        /// <param name="UserIDNO">簽屬此病歷文件之醫護人員身分證字號(醫師或其他醫護人員)</param>
        /// <param name="RPatientID">此病歷文件之病歷號</param>
        /// <param name="RPatinetName">此病歷文件之病患姓名</param>
        /// <param name="CitizenID">此病歷文件之病患身份證字號</param>
        /// <param name="RIOEType">就診身份此病歷文件之診別(I:住院  O:門診  E:急診  H:健檢)</param>
        /// <param name="RChargeType">此病歷文件之病患就診身份</param>
        /// <param name="SignSystem">產生此病歷文件之醫療相關系統代碼</param>
        /// <param name="FileName">此病歷文件檔名稱</param>
        /// <returns>xml文字</returns>
        public string getPDF_xml(DateTime Now, string SourCode, string PKID, string InDate,
            string feeno, string RequestDocOrderDate, string CostCenterCode, string Guider_CCCode,
            string RUser, string RUserName, string UserIDNO, string RPatientID, string RPatinetName, string PatientID,
            string PayInfo, string FilePath, string FileName, string listJsonArray = "", string xml_type = "")
        {
            string xml = string.Empty, md5_pkid = string.Empty;
            if (string.IsNullOrEmpty(PatientID.Trim()))
            {
                PatientID = RPatientID;
            }
            if (!string.IsNullOrEmpty(PKID))
            {
                md5_pkid = GetMd5Hash(PKID);
            }
            xml += "<RequestDate>" + Now.ToString("yyyyMMdd") + "</RequestDate>";
            xml += "<RequestTime>" + Now.ToString("HHmmss") + "</RequestTime>";
            xml += "<RequestNo>" + Now.ToString("yyyyMMddHHmmss") + md5_pkid + "</RequestNo>";
            xml += "<RequestDocType>" + SourCode + "</RequestDocType>";
            xml += "<RequestDocDate>" + InDate + "</RequestDocDate>";
            xml += "<RequestDocRoot>" + feeno + "</RequestDocRoot>";
            xml += "<RequestDocParent>" + md5_pkid + "</RequestDocParent>";
            xml += "<RequestDocOrderDate>" + RequestDocOrderDate + "</RequestDocOrderDate>";
            xml += "<RequestOrderDiv>" + CostCenterCode.Trim() + "</RequestOrderDiv>";
            xml += "<RequestDocPerformDate>" + "" + "</RequestDocPerformDate>";
            xml += "<RequestDocReportDate>" + "" + "</RequestDocReportDate>";
            xml += "<RequestDivision>" + Guider_CCCode.Trim() + "</RequestDivision>";
            xml += "<RequestUser>" + RUser.Trim().ToString() + "</RequestUser>";
            xml += "<RequestUserName>" + RUserName.Trim().ToString() + "</RequestUserName>";
            xml += "<UserIDNO>" + UserIDNO + "</UserIDNO>";
            xml += "<RequestPatientID>" + RPatientID + "</RequestPatientID>";
            xml += "<RequestPatinetName>" + RPatinetName + "</RequestPatinetName>";
            xml += "<CitizenID>" + PatientID + "</CitizenID>";
            xml += "<RequestIOEType>" + "I" + "</RequestIOEType>";
            xml += "<RequestChargeType>" + PayInfo + "</RequestChargeType>";
            xml += "<SignSystem>" + "NIS" + "</SignSystem>";
            xml += "<FilePath>" + FilePath + "</FilePath>";//C:\\EMR\\IPD\\ 
            xml += "<FileName>" + FileName + ".pdf" + "</FileName>";// DISN- D001213013 20120313165503.xml
            List<DBItem> ListDBItem = new List<DBItem>();
            LogTool lt = new LogTool();

            try
            {
                ListDBItem.Add(new DBItem("ORIGINAL_ID", PKID, DBItem.DBDataType.String));
                ListDBItem.Add(new DBItem("HASH_ID", "代簽人：" + RUserName.Trim().ToString(), DBItem.DBDataType.String));
                ListDBItem.Add(new DBItem("FOLDER_SORT", xml_type, DBItem.DBDataType.String));
                ListDBItem.Add(new DBItem("XML_NAME", xml, DBItem.DBDataType.String));
                ListDBItem.Add(new DBItem("RECORD_TIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                ListDBItem.Add(new DBItem("UPLOAD_STATUS", (string.IsNullOrEmpty(Guider_CCCode.Trim())) ? "N" : "Y", DBItem.DBDataType.String));
                ListDBItem.Add(new DBItem("CREATE_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                ListDBItem.Add(new DBItem("FILE_EXIST", Guider_CCCode.Trim(), DBItem.DBDataType.String));
                link.DBExecInsert("EMR_LOG_TABLE", ListDBItem);

            }
            catch (Exception ex)
            {//寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                Assembly entryAssembly = Assembly.GetExecutingAssembly();
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, RUserName.Trim().ToString(), tmp_action, ex.ToString(), ex);
                lt.saveLogMsg(listJsonArray + "\n xml:" + xml, "EMRLogFail_get_xml");
            }
            return xml;
        }
        #endregion

        //20230629 ken 寫入EMR暫存表
        public int EMR_Sign(string time, string id, string msg, string title, string self = "", string feeno = "", string CREATNO = "")
        {
            int erow = 0;
            LogTool lt = new LogTool();
            var C = "";
            var S = "";
            var O = "";
            var I = "";
            var E = "";
            string chartNo = "";
            try
            {
                string emrSql = "SELECT * FROM CARERECORD_DATA WHERE CARERECORD_ID ='" + id + "' ";
                if (self != "")
                {
                    emrSql += "AND SELF = '" + self + "'";
                }
                DataTable Dt = link.DBExecSQL(emrSql);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        C = Dt.Rows[i]["C_DEL"].ToString() + Dt.Rows[i]["C_OTHER"].ToString() + Dt.Rows[i]["C"].ToString(); ;
                        S = Dt.Rows[i]["S"].ToString() + Dt.Rows[i]["S_OTHER"].ToString();
                        O = Dt.Rows[i]["O"].ToString() + Dt.Rows[i]["O_OTHER"].ToString();
                        I = Dt.Rows[i]["I"].ToString() + Dt.Rows[i]["I_OTHER"].ToString();
                        E = Dt.Rows[i]["E"].ToString() + Dt.Rows[i]["E_OTHER"].ToString() + Dt.Rows[i]["E_OTHER_DEL"].ToString();
                    }
                    msg = "";
                }

                if (trans_date(trans_date(C)) != "")
                    msg += trans_date(trans_date(C)) + "\n";// + trans_date(C)

                if (trans_date(trans_date(S)) != "")
                    msg += "S:" + trans_date(trans_date(S)) + "\n";// + trans_date(S) 

                if (trans_date(trans_date(O)) != "")
                    msg += "O:" + trans_date(trans_date(O)) + "\n";//+ trans_date(O) 

                if (trans_date(trans_date(I)) != "")
                    msg += "I:" + trans_date(trans_date(I)) + "\n";// + trans_date(I) 

                if (trans_date(trans_date(E)) != "")
                    msg += "E:" + trans_date(trans_date(E)) + "\n";// + trans_date(E)

                if (feeno == "")
                {
                    feeno = ptinfo.FeeNo;
                    chartNo = ptinfo.ChartNo;
                }
                else
                {
                    byte[] ptinfoByteCode = this.webService.GetPatientInfo(feeno);
                    string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                    PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                    chartNo = pi.ChartNo.Trim();
                }
                DateTime NowTime = DateTime.Now;

                //批次
                Assess ass_m = new Assess();
                var shift = "";
                string sqlstr = "";
                DateTime record = Convert.ToDateTime(time);
                int strtime = int.Parse(record.ToString("HHmm"));
                if (strtime >= 0 && strtime <= 759)
                    shift = "N";
                else if (strtime >= 800 && strtime <= 1559)
                    shift = "D";
                else if (strtime >= 1600 && strtime <= 2359)
                    shift = "E";

                List<DBItem> insertDataList = new List<DBItem>();

                string EmployeesNo = "";
                string EmployeesName = "";
                string Guider = "";
                string repID = "";

                //取得應簽章人員
                if (CREATNO == "")
                {
                    EmployeesNo = userinfo.EmployeesNo.Trim();
                    EmployeesName = userinfo.EmployeesName.Trim();
                    Guider = userinfo.Guider.Trim();
                }
                else
                {
                    byte[] listByteCode = webService.UserName(CREATNO);
                    string listJsonArray = CompressTool.DecompressString(listByteCode);
                    UserInfo user_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                    EmployeesNo = user_info.EmployeesNo.Trim();
                    EmployeesName = user_info.EmployeesName.Trim();
                    string tempstr = RequestName(user_info.EmployeesNo, user_info.EmployeesNo, user_info.Category);
                    if (string.IsNullOrEmpty(tempstr))
                    {
                        Guider = "";
                    }
                    else
                    {
                        Guider = tempstr.Trim();
                    }
                }
                repID = Guider;

                //查詢今日的包
                sqlstr += "SELECT * FROM NIS_EMR_PACKAGE_MST WHERE  CREATE_DTM BETWEEN TO_DATE('" + record.ToString("yyyy/MM/dd 00:00:00") + "', 'yyyy/MM/dd hh24:mi:ss') AND TO_DATE('" + record.ToString("yyyy/MM/dd 23:59:59") + "', 'yyyy/MM/dd hh24:mi:ss') AND FEENO = '" + feeno.Trim() + "' AND SIGNER = '" + EmployeesNo.Trim() + "' AND (STATUS ='Y' OR STATUS IS NULL) ";

                DataTable DtP = link.DBExecSQL(sqlstr);

                //有包則新增明細
                if (DtP.Rows.Count > 0)
                {
                    var P_ID = DtP.Rows[0]["PAC_ID"].ToString().Trim();
                    var PD_ID = "PD_" + feeno + "_" + NowTime.ToString("yyyyMMddHHmmfff");
                    string hasEKG = "";
                    if (self == "EKG")
                    {
                        hasEKG = "EKG";
                    }
                    insertDataList.Add(new DBItem("PACD_ID", PD_ID.Trim(), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAC_ID", P_ID.Trim(), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("RECORD_KEY", id, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("RECORD_DATE", time, DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("FOCUS", trans_date(title), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CONTENT", msg, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("HAS_EKG", hasEKG, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATE_ID", EmployeesNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATE_NAME", EmployeesName, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATE_DATE", NowTime.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("SELF", self, DBItem.DBDataType.String));
                    erow = link.DBExecInsert("NIS_EMR_PACKAGE_DTL", insertDataList);
                    insertDataList.Clear();
                    insertDataList.Add(new DBItem("SIGN_STATUS", "U", DBItem.DBDataType.String));
                    erow = ass_m.DBExecUpdate("NIS_EMR_PACKAGE_MST", insertDataList, " PAC_ID = '" + P_ID + "' ");
                }
                //無包新增主檔
                else
                {
                    //MST
                    var P_ID = "P_" + feeno + "_" + NowTime.ToString("yyyyMMddHHmmfff");
                    insertDataList.Add(new DBItem("PAC_ID", P_ID.Trim(), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CHART_NO", chartNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("SHIFT", shift, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATE_DTM", time, DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("CAREGIVER_ID", "", DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("GUIDE_ID", Guider, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("REP_ID", repID, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("SIGNER", EmployeesNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("SIGNER_NAME", EmployeesName, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("SIGN_STATUS", "U", DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("VERSION", "0", DBItem.DBDataType.String));
                    int pkerow = link.DBExecInsert("NIS_EMR_PACKAGE_MST", insertDataList);

                    if (pkerow > 0)
                    {
                        //DTL
                        insertDataList.Clear();
                        var PD_ID = "PD_" + feeno + "_" + NowTime.ToString("yyyyMMddHHmmfff");
                        string hasEKG = "";
                        if (self == "EKG")
                        {
                            hasEKG = "EKG";
                        }
                        insertDataList.Add(new DBItem("PACD_ID", PD_ID.Trim(), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("PAC_ID", P_ID.Trim(), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("RECORD_KEY", id, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("RECORD_DATE", time, DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("FOCUS", trans_date(title), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CONTENT", msg, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("HAS_EKG", hasEKG, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CREATE_ID", EmployeesNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CREATE_NAME", EmployeesName, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CREATE_DATE", NowTime.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("SELF", self, DBItem.DBDataType.String));
                        erow = link.DBExecInsert("NIS_EMR_PACKAGE_DTL", insertDataList);
                    }
                }
            }
            catch (Exception ex)
            {
                lt.saveLogMsg(ex.Message.ToString(), "EMRLog");
                erow = 0;
            }
            return erow;
        }

        //20230629 ken 批次簽章處理
        public int SignPush(string feenoSingle = "")
        {
            int errow = 0;
            var PackNo = "";
            string feenoLog = "";
            Assess ass_m = new Assess();
            CareRecord care_record_m = new CareRecord();
            LogTool lt = new LogTool();
            try
            {
                string userNo = userinfo.EmployeesNo.ToString().Trim();
                string sqlstr = "";
                sqlstr = " SELECT * FROM NIS_EMR_PACKAGE_MST WHERE (SIGNER = '" + userNo + "' OR REP_ID = '" + userNo + "' OR GUIDE_ID = '" + userNo + "') AND SIGN_STATUS = 'U' AND (STATUS = 'Y' OR STATUS IS NULL)";

                if (feenoSingle != "")
                {
                    feenoSingle = feenoSingle.Trim();
                    sqlstr += " AND FEENO = '" + feenoSingle + "'";
                }
                DataTable DtCK = this.link.DBExecSQL(sqlstr);
                if (DtCK.Rows.Count > 0)
                {
                    for (int i = 0; i < DtCK.Rows.Count; i++)
                    {
                        try
                        {
                            //通知主檔
                            DateTime NowTime = DateTime.Now;
                            var version = DtCK.Rows[i]["VERSION"].ToString().Trim();
                            int addVersion = int.Parse(version) + 1;
                            var id = DtCK.Rows[i]["PAC_ID"].ToString().Trim();
                            var RecordTime = DtCK.Rows[i]["CREATE_DTM"].ToString().Trim();
                            var time = DtCK.Rows[i]["CREATE_DTM"].ToString().Trim();
                            string Temp_NowTime_Str = Convert.ToDateTime(RecordTime).ToString("yyyyMMddHHmmss");//時間採統一變數
                            string self = "CARERECORD";
                            var feeno = DtCK.Rows[i]["FEENO"].ToString().Trim();
                            feenoLog = feeno;

                            //取得應簽章人員
                            byte[] listByteCode = webService.UserName(userinfo.Guider);
                            string listJsonArray = CompressTool.DecompressString(listByteCode);
                            UserInfo user_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);

                            //加入刪除退掛功能，若feeno抓不到資料，即進行刪除
                            byte[] ptinfoByteCode = this.webService.GetPatientInfo(feeno);
                            if (ptinfoByteCode == null)
                            {
                                //無資料進行刪除
                                int deleteResult = SignDelete(feeno);
                                continue;
                            }
                            string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                            PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);

                            string EmrXmlString = this.get_xml_new(
                                 NowTime.ToString("yyyyMMddHHmmss.fffffff"), NowTime.ToString("yyyyMMddHHmmss") + GetMd5Hash(id + self), "A000040", Convert.ToDateTime(time).ToString("yyyyMMdd"),
                                 GetMd5Hash(id + self), Convert.ToDateTime(time).ToString("yyyyMMdd"), "", "",
                                 user_info.EmployeesNo, user_info.EmployeesName, user_info.UserID, pi.ChartNo, pi.PatientName,
                                 pi.PatientID, pi.PayInfo,
                                 "C:\\EMR\\", "A000040" + GetMd5Hash(id + self) + Temp_NowTime_Str + "_" + addVersion.ToString() + ".xml", listJsonArray, "Insert_CareRecord", pi.InDate.ToString("yyyyMMdd"), pi.FeeNo
                                 );
                            PackNo = "";
                            sqlstr = "";
                            PackNo = DtCK.Rows[i]["PAC_ID"].ToString().Trim();

                            if (PackNo != "")
                            {
                                sqlstr = " SELECT * FROM NIS_EMR_PACKAGE_DTL WHERE PAC_ID = '" + PackNo + "' AND STATUS = 'Y' ORDER BY RECORD_DATE ASC";
                                DataTable DtPD = this.link.DBExecSQL(sqlstr);
                                if (DtPD.Rows.Count > 0)
                                {
                                    List<SignListDtl> signlist = new List<SignListDtl>();
                                    for (int j = 0; j < DtPD.Rows.Count; j++)
                                    {
                                        DateTime record = Convert.ToDateTime(DtPD.Rows[j]["RECORD_DATE"]);
                                        string hasEKG = DtPD.Rows[j]["HAS_EKG"].ToString().Trim();
                                        string EKG = "";
                                        if (hasEKG == "EKG")
                                        {
                                            string ekgSql = "SELECT * FROM CARERECORD_DATA WHERE CARERECORD_ID ='" + DtPD.Rows[j]["RECORD_KEY"].ToString().Trim() + "' ";
                                            DataTable Dtekg = link.DBExecSQL(ekgSql);
                                            if (Dtekg.Rows.Count > 0)
                                            {
                                                byte[] arr = null;
                                                if (Dtekg.Rows[0]["EKG"].ToString() != "" && Dtekg.Rows[0]["EKG"] != null)
                                                {
                                                    arr = (byte[])Dtekg.Rows[0]["EKG"];
                                                    EKG = Convert.ToBase64String(arr);
                                                }

                                            }
                                            else
                                            {
                                                EKG = "";
                                            }
                                        }

                                        signlist.Add(new SignListDtl()
                                        {
                                            PACD_ID = DtPD.Rows[j]["PACD_ID"].ToString().Trim(),
                                            PAC_ID = DtPD.Rows[j]["PAC_ID"].ToString().Trim(),
                                            RECORD_KEY = DtPD.Rows[j]["RECORD_KEY"].ToString().Trim(),
                                            RECORD_DATE = record.ToString("yyyyMMdd").Trim(),
                                            RECORD_TIME = record.ToString("HHmmss").Trim(),
                                            FOCUS = DtPD.Rows[j]["FOCUS"].ToString().Trim(),
                                            CONTENT = DtPD.Rows[j]["CONTENT"].ToString().Trim(),
                                            HAS_EKG = DtPD.Rows[j]["HAS_EKG"].ToString().Trim(),
                                            CREATE_ID = DtPD.Rows[j]["CREATE_ID"].ToString().Trim(),
                                            CREATE_NAME = DtPD.Rows[j]["CREATE_NAME"].ToString().Trim(),
                                            CREATE_DATE = DtPD.Rows[j]["CREATE_DATE"].ToString().Trim(),
                                            SELF = DtPD.Rows[j]["SELF"].ToString().Trim(),
                                            EKG = EKG
                                        });
                                    }
                                    byte[] allergen = webService.GetAllergyList(feeno);
                                    string ptJsonArr = string.Empty;
                                    string allergyDesc = string.Empty;

                                    if (allergen != null)
                                    {
                                        ptJsonArr = CompressTool.DecompressString(allergen);
                                    }
                                    List<NIS.Data.PatientInfo> patList = JsonConvert.DeserializeObject<List<NIS.Data.PatientInfo>>(ptJsonArr);

                                    if (allergen != null)
                                    {
                                        allergyDesc = patList[0].AllergyDesc;
                                    }
                                    string xml = care_record_m.care_Record_Get_xml_new(signlist, allergyDesc, pi);
                                    SaveEMRLogData(id + self, GetMd5Hash(id + self), "EMR", RecordTime, "A000040" + GetMd5Hash(id + self) + Temp_NowTime_Str + "_" + addVersion.ToString(), xml);
                                    //SaveEMRLogData(id + self, GetMd5Hash(id + self), "EMRBK", RecordTime, "A000040" + GetMd5Hash(id + self) + Temp_NowTime_Str + "_" + addVersion.ToString(), xml);
                                    SaveEMRLogData(id + self, GetMd5Hash(id + self), "Temp", RecordTime, Temp_NowTime_Str + "-" + GetMd5Hash(id + self) + "_" + addVersion.ToString(), EmrXmlString);
                                }
                                else
                                {
                                    //若無明細刪除該包
                                    if (version != "0")
                                    {
                                        del_emr(id + self, userNo);
                                    }
                                }
                            }
                            List<DBItem> insertDataList = new List<DBItem>();
                            insertDataList.Clear();
                            insertDataList.Add(new DBItem("SIGN_STATUS", "S", DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("ERR", "N", DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("VERSION", addVersion.ToString(), DBItem.DBDataType.String));
                            errow = ass_m.DBExecUpdate("NIS_EMR_PACKAGE_MST", insertDataList, " PAC_ID = '" + PackNo + "' ");

                            //LOG
                            errow = EMRLogSave(PackNo, "User", userinfo.EmployeesNo.ToString().Trim(), feeno, "S", addVersion.ToString());
                        }
                        catch (Exception ex)
                        {
                            lt.saveLogMsg(ex.Message.ToString() + PackNo, "EMRLog");
                            List<DBItem> insertDataList = new List<DBItem>();
                            insertDataList.Clear();
                            insertDataList.Add(new DBItem("ERR", "E", DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("SIGN_STATUS", "U", DBItem.DBDataType.String));
                            errow = ass_m.DBExecUpdate("NIS_EMR_PACKAGE_MST", insertDataList, " PAC_ID = '" + PackNo + "' ");
                            //LOG
                            errow = EMRLogSave(PackNo, "User", userinfo.EmployeesNo.ToString().Trim(), feenoLog, "E");
                            errow = 0;
                        }
                    }
                }
                else
                {
                    //無資料
                    errow = 2;
                }
            }
            catch (Exception ex)
            {
                lt.saveLogMsg(ex.Message.ToString() + PackNo, "EMRLog");
                List<DBItem> insertDataList = new List<DBItem>();
                insertDataList.Clear();
                insertDataList.Add(new DBItem("ERR", "E", DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("SIGN_STATUS", "U", DBItem.DBDataType.String));
                errow = ass_m.DBExecUpdate("NIS_EMR_PACKAGE_MST", insertDataList, " PAC_ID = '" + PackNo + "' ");
                //Log
                errow = EMRLogSave(PackNo, "User", userinfo.EmployeesNo.ToString().Trim(), feenoLog, "E");
                errow = 0;
            }
            return errow;
        }

        //20230629 ken 批次簽章處理 全部送簽名
        public int SignPushAll(string feenoSingle = "", string userNo = "", string startTime = "", string endTime = "")
        {
            LogTool lt = new LogTool();
            int errow = 0;
            string pac_id = "";
            var PackNo = "";
            string feenoLog = "";
            Assess ass_m = new Assess();
            try
            {
                string sqlstr = "";
                sqlstr = "SELECT * FROM NIS_EMR_PACKAGE_MST";
                if (!string.IsNullOrEmpty(feenoSingle) && !string.IsNullOrEmpty(startTime) && !string.IsNullOrEmpty(endTime))
                {
                    sqlstr += " WHERE FEENO = '" + feenoSingle + "'";
                    sqlstr += " AND CREATE_DTM >= to_date('" + startTime + " 00:00:00" + "','yyyy/mm/dd hh24:mi:ss') AND CREATE_DTM <= to_date('" + endTime + " 23:59:59" + "','yyyy/mm/dd hh24:mi:ss')";
                    if (!string.IsNullOrEmpty(userNo))
                    {
                        sqlstr += " AND (SIGNER = '" + userNo + "' OR REP_ID = '" + userNo + "' OR GUIDE_ID = '" + userNo + "')";
                    }
                }
                else
                {
                    sqlstr += " WHERE SIGN_STATUS = 'U' AND (STATUS = 'Y' OR STATUS IS NULL)";
                }
                DataTable DtCK = this.link.DBExecSQL(sqlstr);
                CareRecord care_record_m = new CareRecord();
                if (DtCK.Rows.Count > 0)
                {
                    for (int i = 0; i < DtCK.Rows.Count; i++)
                    {
                        pac_id = "";
                        pac_id = DtCK.Rows[i]["PAC_ID"].ToString().Trim();
                        try
                        {
                            //通知主檔
                            DateTime NowTime = DateTime.Now;
                            var version = DtCK.Rows[i]["VERSION"].ToString().Trim();
                            int addVersion = int.Parse(version) + 1;
                            var id = DtCK.Rows[i]["PAC_ID"].ToString().Trim();
                            var RecordTime = DtCK.Rows[i]["CREATE_DTM"].ToString().Trim();
                            var time = DtCK.Rows[i]["CREATE_DTM"].ToString().Trim();
                            var feeno = DtCK.Rows[i]["FEENO"].ToString().Trim();
                            feenoLog = feeno;
                            string Temp_NowTime_Str = Convert.ToDateTime(RecordTime).ToString("yyyyMMddHHmmss");//時間採統一變數
                            string self = "CARERECORD";
                            string employee = "";
                            employee = DtCK.Rows[i]["GUIDE_ID"].ToString().Trim();
                            if (string.IsNullOrEmpty(employee))
                            {
                                employee = DtCK.Rows[i]["SIGNER"].ToString().Trim();
                            }
                            //取得應簽章人員
                            byte[] listByteCode = webService.UserName(employee);
                            string listJsonArray = CompressTool.DecompressString(listByteCode);
                            UserInfo user_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);

                            //加入刪除退掛功能，若feeno抓不到資料，即進行刪除
                            byte[] ptinfoByteCode = this.webService.GetPatientInfo(feeno);
                            if (ptinfoByteCode == null)
                            {
                                //無資料進行刪除
                                int deleteResult = SignDelete(feeno);
                                continue;
                            }
                            string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                            PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);

                            string EmrXmlString = this.get_xml_new(
                                 NowTime.ToString("yyyyMMddHHmmss.fffffff"), NowTime.ToString("yyyyMMddHHmmss") + GetMd5Hash(id + self), "A000040", Convert.ToDateTime(time).ToString("yyyyMMdd"),
                                 GetMd5Hash(id + self), Convert.ToDateTime(time).ToString("yyyyMMdd"), "", "",
                                 user_info.EmployeesNo, user_info.EmployeesName, user_info.UserID, pi.ChartNo, pi.PatientName,
                                 pi.PatientID, pi.PayInfo,
                                 "C:\\EMR\\", "A000040" + GetMd5Hash(id + self) + Temp_NowTime_Str + "_" + addVersion.ToString() + ".xml", listJsonArray, "Insert_CareRecord", pi.InDate.ToString("yyyyMMdd"), pi.FeeNo
                                 );
                            PackNo = "";
                            sqlstr = "";
                            PackNo = DtCK.Rows[i]["PAC_ID"].ToString().Trim();

                            if (PackNo != "")
                            {
                                sqlstr = " SELECT * FROM NIS_EMR_PACKAGE_DTL WHERE PAC_ID = '" + PackNo + "' AND STATUS = 'Y' ORDER BY RECORD_DATE ASC";
                                DataTable DtPD = this.link.DBExecSQL(sqlstr);
                                if (DtPD.Rows.Count > 0)
                                {
                                    List<SignListDtl> signlist = new List<SignListDtl>();
                                    for (int j = 0; j < DtPD.Rows.Count; j++)
                                    {
                                        DateTime record = Convert.ToDateTime(DtPD.Rows[j]["RECORD_DATE"]);
                                        string hasEKG = DtPD.Rows[j]["HAS_EKG"].ToString().Trim();
                                        string EKG = "";
                                        if (hasEKG == "EKG")
                                        {
                                            string ekgSql = "SELECT * FROM CARERECORD_DATA WHERE CARERECORD_ID ='" + DtPD.Rows[j]["RECORD_KEY"].ToString().Trim() + "' ";
                                            DataTable Dtekg = link.DBExecSQL(ekgSql);
                                            if (Dtekg.Rows.Count > 0)
                                            {
                                                byte[] arr = null;
                                                arr = (byte[])Dtekg.Rows[0]["EKG"];
                                                EKG = Convert.ToBase64String(arr);
                                            }
                                            else
                                            {
                                                EKG = "";
                                            }
                                        }
                                        signlist.Add(new SignListDtl()
                                        {
                                            PACD_ID = DtPD.Rows[j]["PACD_ID"].ToString().Trim(),
                                            PAC_ID = DtPD.Rows[j]["PAC_ID"].ToString().Trim(),
                                            RECORD_KEY = DtPD.Rows[j]["RECORD_KEY"].ToString().Trim(),
                                            RECORD_DATE = record.ToString("yyyyMMdd").Trim(),
                                            RECORD_TIME = record.ToString("HHmmss").Trim(),
                                            FOCUS = DtPD.Rows[j]["FOCUS"].ToString().Trim(),
                                            CONTENT = DtPD.Rows[j]["CONTENT"].ToString().Trim(),
                                            HAS_EKG = DtPD.Rows[j]["HAS_EKG"].ToString().Trim(),
                                            CREATE_ID = DtPD.Rows[j]["CREATE_ID"].ToString().Trim(),
                                            CREATE_NAME = DtPD.Rows[j]["CREATE_NAME"].ToString().Trim(),
                                            CREATE_DATE = DtPD.Rows[j]["CREATE_DATE"].ToString().Trim(),
                                            SELF = DtPD.Rows[j]["SELF"].ToString().Trim(),
                                            EKG = EKG
                                        });
                                    }
                                    byte[] allergen = webService.GetAllergyList(feeno);
                                    string ptJsonArr = string.Empty;
                                    string allergyDesc = string.Empty;

                                    if (allergen != null)
                                    {
                                        ptJsonArr = CompressTool.DecompressString(allergen);
                                    }

                                    List<NIS.Data.PatientInfo> patList = JsonConvert.DeserializeObject<List<NIS.Data.PatientInfo>>(ptJsonArr);

                                    if (allergen != null)
                                    {
                                        allergyDesc = patList[0].AllergyDesc;
                                    }
                                    string xml = care_record_m.care_Record_Get_xml_new(signlist, allergyDesc, pi);
                                    SaveEMRLogData(id + self, GetMd5Hash(id + self), "EMR", RecordTime, "A000040" + GetMd5Hash(id + self) + Temp_NowTime_Str + "_" + addVersion.ToString(), xml, employee);
                                    //SaveEMRLogData(id + self, GetMd5Hash(id + self), "EMRBK", RecordTime, "A000040" + GetMd5Hash(id + self) + Temp_NowTime_Str + "_" + addVersion.ToString(), xml, employee);
                                    SaveEMRLogData(id + self, GetMd5Hash(id + self), "Temp", RecordTime, Temp_NowTime_Str + "-" + GetMd5Hash(id + self) + "_" + addVersion.ToString(), EmrXmlString, employee);
                                }
                                else
                                {
                                    //無資料進行刪除
                                    if (version != "0")
                                    {
                                        del_emr(id + self, employee);
                                    }
                                }
                            }
                            List<DBItem> insertDataList = new List<DBItem>();
                            insertDataList.Clear();
                            insertDataList.Add(new DBItem("SIGN_STATUS", "S", DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("ERR", "N", DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("VERSION", addVersion.ToString(), DBItem.DBDataType.String));
                            errow = ass_m.DBExecUpdate("NIS_EMR_PACKAGE_MST", insertDataList, " PAC_ID = '" + PackNo + "' ");

                            //LOG
                            errow = EMRLogSave(PackNo, "SYSTEM", "SYSTEM", feeno, "S", addVersion.ToString());
                        }
                        catch (Exception ex)
                        {
                            lt.saveLogMsg(pac_id + "_" + ex.Message.ToString(), "EMRLog");
                            List<DBItem> insertDataList = new List<DBItem>();
                            insertDataList.Clear();
                            insertDataList.Add(new DBItem("ERR", "E", DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("SIGN_STATUS", "U", DBItem.DBDataType.String));
                            errow = ass_m.DBExecUpdate("NIS_EMR_PACKAGE_MST", insertDataList, " PAC_ID = '" + PackNo + "' ");

                            //LOG
                            errow = EMRLogSave(PackNo, "SYSTEM", "SYSTEM", feenoLog, "E");
                            errow = 0;
                        }
                        Thread.Sleep(2000);
                    }
                }
                else
                {
                    //無資料
                    errow = 2;
                }
            }
            catch (Exception ex)
            {
                lt.saveLogMsg(ex.Message.ToString(), "EMRLog");
                List<DBItem> insertDataList = new List<DBItem>();
                insertDataList.Clear();
                insertDataList.Add(new DBItem("ERR", "E", DBItem.DBDataType.String));
                errow = ass_m.DBExecUpdate("NIS_EMR_PACKAGE_MST", insertDataList, " PAC_ID = '" + PackNo + "' ");
                errow = 0;
            }
            return errow;
        }

        //病人退掛刪除作業
        public int SignDelete(string feeno = "")
        {
            int errow = 0;
            string PacID = "";
            string self = "CARERECORD";
            string sqlstr = "SELECT * FROM NIS_EMR_PACKAGE_MST WHERE FEENO = '" + feeno + "'";
            List<DBItem> insertDataList = new List<DBItem>();
            LogTool lt = new LogTool();
            Assess ass_m = new Assess();
            try
            {
                DataTable Dt = this.link.DBExecSQL(sqlstr);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        string pac_id = Dt.Rows[i]["PAC_ID"].ToString().Trim();
                        PacID = pac_id;
                        string employee = Dt.Rows[i]["GUIDE_ID"].ToString().Trim();
                        string sign_status = Dt.Rows[i]["SIGN_STATUS"].ToString().Trim();

                        //MST主檔刪除
                        insertDataList.Clear();
                        insertDataList.Add(new DBItem("STATUS", "D", DBItem.DBDataType.String));
                        errow = ass_m.DBExecUpdate("NIS_EMR_PACKAGE_MST", insertDataList, " PAC_ID = '" + pac_id + "' ");
                        if (errow > 0)
                        {
                            if (sign_status == "S")
                            {
                                //刪除已送簽過簽章
                                del_emr(pac_id + self, employee);
                            }
                            //Log
                            errow = EMRLogSave(pac_id, "SYSTEM", "SYSTEM", feeno, "D");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                lt.saveLogMsg(ex.Message.ToString() + PacID, "EMRLog");
                errow = 0;
            }
            return errow;
        }

        public int EMRLogSave(string PacID, string type, string employee, string feeno, string status, string version = "")
        {
            int errow = 0;
            List<DBItem> insertDataList = new List<DBItem>();
            LogTool lt = new LogTool();

            try
            {
                insertDataList.Clear();
                if (version != "")
                {
                    insertDataList.Add(new DBItem("SERIAL", PacID + "_" + version, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("SERIAL", PacID + "_" + DateTime.Now.ToString("yyyyMMddHHmm"), DBItem.DBDataType.String));
                }
                insertDataList.Add(new DBItem("PAC_ID", PacID, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("SIGN_TYPE", type, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("SIGN_ID", employee, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("SIGN_TIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("STATUS", status, DBItem.DBDataType.String));
                errow = link.DBExecInsert("NIS_EMR_PACKAGE_LOG", insertDataList);
            }
            catch (Exception ex)
            {
                lt.saveLogMsg(ex.Message.ToString() + PacID, "EMRLogSave");
                errow = 0;
            }
            return errow;
        }

        SetDDL TmpSetDdl = null;
        public SetDDL SetDropDownList
        {
            get
            {
                if (TmpSetDdl == null)
                {
                    TmpSetDdl = new SetDDL();
                }

                return TmpSetDdl;
            }
        }
        public string get_EKG_Carerecord(string id)
        {
            DataTable dt = new DataTable();
            string start = DateTime.Now.AddDays(-365).ToString("yyyy/MM/dd");
            string end = DateTime.Now.ToString("yyyy/MM/dd");

            string sql = "SELECT * FROM CARERECORD_DATA WHERE CARERECORD_ID = '" + id + "'";

            link.DBExecSQL(sql, ref dt);
            string EKG = "";

            if (dt.Rows.Count > 0)
            {
                EKG = dt.Rows[0]["IMGFILENAME"].ToString();
            }

            return EKG;
        }
        public string GetMd5Hash(string input)
        {
            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] data = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));

            System.Text.StringBuilder sBuilder = new System.Text.StringBuilder();
            for (int i = 0; i < data.Length; i++)
                sBuilder.Append(data[i].ToString("x2"));

            return sBuilder.ToString();
        }

        /// <summary>
        /// 設定所有項目的TYPE
        /// </summary>
        /// <param name="all_key">所有項目</param>
        /// <param name="all_type">所有項目的TYPE</param>
        public Hashtable set_hashtable(string[] all_key, string[] all_type)
        {
            Hashtable Input_Type = new Hashtable();
            for (int i = 0; i < all_key.Length; i++)
                Input_Type.Add(all_key[i], all_type[i]);

            return Input_Type;
        }

        public DateTime GetMinDate()
        {
            DateTime mindate = DateTime.Now;
            if (this.complement_List.Status)
                mindate = this.ptinfo.InDate;
            else
            {
                if (DateTime.Now.Hour < 24)//20171030 改規則 AlanHuang
                {
                    mindate = Convert.ToDateTime(DateTime.Now.AddDays(-1).ToString("yyyy/MM/dd 00:00"));
                }
                else if (DateTime.Now.Day == 1 && DateTime.Now.Hour < 24)
                {
                    if (((DateTime.Now.Year % 4 == 0) && (DateTime.Now.Year % 100 != 0)) || (DateTime.Now.Year % 400 == 0))//判斷閏年
                    {
                        if (DateTime.Now.Month == 2)
                        {
                            mindate = Convert.ToDateTime(DateTime.Now.AddMonths(-1).AddDays(28).ToString("yyyy/MM/dd 00:00"));
                        }
                    }
                    if (DateTime.Now.Month == 1 || DateTime.Now.Month == 3 || DateTime.Now.Month == 5 || DateTime.Now.Month == 7 || DateTime.Now.Month == 8 || DateTime.Now.Month == 10 || DateTime.Now.Month == 12)
                    {
                        mindate = Convert.ToDateTime(DateTime.Now.AddMonths(-1).AddDays(30).ToString("yyyy/MM/dd 00:00"));
                    }
                    else
                    {
                        if (DateTime.Now.Month == 2) { mindate = Convert.ToDateTime(DateTime.Now.AddMonths(-1).AddDays(27).ToString("yyyy/MM/dd 00:00")); }
                        else { mindate = Convert.ToDateTime(DateTime.Now.AddMonths(-1).AddDays(29).ToString("yyyy/MM/dd 00:00")); }
                    }
                }
                else
                { mindate = Convert.ToDateTime(DateTime.Now.ToString("yyyy/MM/dd 00:00")); }
                if (this.ptinfo.InDate > mindate)
                    mindate = this.ptinfo.InDate;
            }
            return mindate;
        }

        public List<FallAssessMeasure> GetAdultFallAssessMeasure()
        {
            List<FallAssessMeasure> dt = new List<FallAssessMeasure>();
            dt.Add(new FallAssessMeasure("無", 0));
            dt.Add(new FallAssessMeasure("病人床頭懸掛預防跌倒標示牌", 0));
            dt.Add(new FallAssessMeasure("提供防跌衛教單張並說明內容", 0));
            dt.Add(new FallAssessMeasure("床旁置紅燈鈴(護理人員至病房實際操作並解說)", 0));
            dt.Add(new FallAssessMeasure("將病床降到最低高度，床輪固定，病床靠牆", 0));
            dt.Add(new FallAssessMeasure("尿壺、便盆或便盆椅倒空並放置適當位置", 0));
            dt.Add(new FallAssessMeasure("輔具使用(如床欄、夜燈、呼叫鈴、便盆椅、輪椅、拐杖)", 0));
            dt.Add(new FallAssessMeasure("視情況約束，並提供合適約束工具(乒乓拍手套、腕約、胸約)", 0));
            dt.Add(new FallAssessMeasure("教導病人及家屬服用特殊藥物注意事項", 0));
            dt.Add(new FallAssessMeasure("虛弱或術後病患首次下床時，教導先坐床緣5分鐘後再下床", 0));
            dt.Add(new FallAssessMeasure("工作人員不定期巡視浴廁地板，保持地面乾燥", 0));
            dt.Add(new FallAssessMeasure("家屬或看護陪伴病人在旁，應清楚欲外出時，必須先告知醫護人員，以能夠加強探視", 0));
            dt.Add(new FallAssessMeasure("提醒睡前如廁及夜間少喝水，減少半夜下床如廁次數", 0));
            dt.Add(new FallAssessMeasure("步態不穩者，提供適當的輔具", 0));
            dt.Add(new FallAssessMeasure("指導家屬正確執行上下床移位技巧", 0));
            dt.Add(new FallAssessMeasure("醫師及專科護理師針對高危險群病患提醒及預防跌倒宣導", 0));
            return dt;
        }

        public List<FallAssessMeasure> GetChildFallAssessMeasure()
        {
            List<FallAssessMeasure> dt = new List<FallAssessMeasure>();
            dt.Add(new FallAssessMeasure("無", 0));
            dt.Add(new FallAssessMeasure("病人床頭懸掛預防跌倒標示牌，並提供防跌衛教單張說明內容", 0));
            dt.Add(new FallAssessMeasure("小於2歲(含)幼兒，應提供兒童床使用；2歲以上小於5歲之幼兒(含)，可使用成人床，床欄若有縫隙，應提供長枕將床欄缺口防堵", 0));
            dt.Add(new FallAssessMeasure("雙側床欄應隨時拉起並拉高，床欄卡隼需卡好", 0));
            dt.Add(new FallAssessMeasure("家屬能隨時陪伴於病童身旁且禁止病童站立於床上跳躍", 0));
            dt.Add(new FallAssessMeasure("病床床輪要固定，並將床降到最低高度", 0));
            dt.Add(new FallAssessMeasure("勿讓病童站於活動式點滴架架上推送行走，並整理病童身上的管路，避免因垂落的管路而絆倒，勿在走廊上奔跑", 0));
            dt.Add(new FallAssessMeasure("病童照顧者如有更換，護理人員應重新指導新照顧者防跌措施", 0));
            dt.Add(new FallAssessMeasure("維持病室內燈光明亮及維持地板乾燥無潮濕、無障礙物", 0));
            return dt;
        }

        // 儲存EMRLOG
        public void SaveEMRLogData(string O_ID, string H_ID, string F_Sort, string R_Time, string XML_Name, string XML_Content, string userno = "")
        {
            //DBConnector dbconnector = new DBConnector();
            List<DBItem> ListDBItem = new List<DBItem>();
            LogTool lt = new LogTool();
            string SaveStatus = "N";//有無寫入資料的狀態
            string File_Exist = "X";
            userno = (string.IsNullOrEmpty(userno)) ? userinfo.EmployeesNo : userno;
            try
            {
                if (F_Sort == "EMR")
                {//System.IO.File.WriteAllLines(@"C:\EMR\A000040" + GetMd5Hash(id + self) + Temp_NowTime_Str + ".xml", new string[] { xml });
                    System.IO.File.WriteAllLines(@"C:\EMR\" + XML_Name + ".xml", new string[] { XML_Content });
                }
                else if (F_Sort == "EMRBK")
                {
                    System.IO.File.WriteAllLines(@"C:\EMR\Backup\" + XML_Name + ".xml", new string[] { XML_Content });
                }
                else
                { //System.IO.File.WriteAllLines(@"C:\Temp\" + Temp_NowTime_Str + "-" + GetMd5Hash(id + self) + ".xml", new string[] { EmrXmlString }, Encoding.GetEncoding("BIG5"));
                    System.IO.File.WriteAllLines(@"C:\Temp\" + XML_Name + ".xml", new string[] { XML_Content }, Encoding.GetEncoding("BIG5"));
                }
                SaveStatus = "Y";
                File_Exist = (System.IO.File.Exists(@"C:\\" + F_Sort + "\\" + XML_Name + ".xml")) ? "Y" : "N";
            }
            catch (Exception ex)
            {
                lt.saveLogMsg(ex.Message.ToString(), "EMRLog");
                SaveStatus = "N";
            }
            finally
            {
                try
                {
                    ListDBItem.Add(new DBItem("ORIGINAL_ID", O_ID, DBItem.DBDataType.String));
                    ListDBItem.Add(new DBItem("HASH_ID", H_ID, DBItem.DBDataType.String));
                    ListDBItem.Add(new DBItem("FOLDER_SORT", F_Sort, DBItem.DBDataType.String));
                    ListDBItem.Add(new DBItem("XML_NAME", XML_Name, DBItem.DBDataType.String));
                    ListDBItem.Add(new DBItem("RECORD_TIME", Convert.ToDateTime(R_Time).ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                    ListDBItem.Add(new DBItem("UPLOAD_STATUS", SaveStatus, DBItem.DBDataType.String));
                    ListDBItem.Add(new DBItem("CREATE_USER", userno, DBItem.DBDataType.String));
                    ListDBItem.Add(new DBItem("FILE_EXIST", File_Exist, DBItem.DBDataType.String));
                    link.DBExecInsert("EMR_LOG_TABLE", ListDBItem);
                }
                catch (Exception ex)
                {
                    //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                    write_logMsg("BASE", userinfo.EmployeesNo, "SaveEMRLogData", ex.ToString(), ex);
                    lt.saveLogMsg("O_ID：" + O_ID + "HASH_ID：" + H_ID + "FOLDER_SORT：" + F_Sort + "XML_NAME：" + XML_Name + "RECORD_TIME：" + R_Time + "UPLOAD_STATUS：" + SaveStatus + "CREATE_USER：" + userno + "FILE_EXIST：" + File_Exist, "EMRLogFail_SaveEMRLogData");
                }

            }
        }
        // VIP儀器資料帶入護理紀錄簽章
        public int Insert_VIP_CareRecord()
        {
            string time = ""; string id = ""; string title = ""; string self = ""; string sqlstr = "";
            List<DBModel.CARERECORD_DATA> VIP_CareRecord = new List<DBModel.CARERECORD_DATA>();
            List<string> update_list = new List<string>();
            DataTable dt = new DataTable();
            string listJsonArray = string.Empty;
            byte[] listByteCode = null;
            int erow = 0;
            try
            {
                sqlstr = "SELECT aa.*FROM CARERECORD_DATA aa ,VIPBPTBL VIP where ";
                sqlstr += "aa.VIP_ID = VIP.ID AND VIP.STATUS = '1' order by VIP.DATA_TIME";
                dt = link.DBExecSQL(sqlstr);
                if (dt != null && dt.Rows.Count > 0)
                {
                    VIP_CareRecord = (List<DBModel.CARERECORD_DATA>)dt.ToList<DBModel.CARERECORD_DATA>();
                }
                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (var item in VIP_CareRecord)
                    {
                        update_list.Add("ID = '" + item.VIP_ID + "'");  //增加 WHERE ID條件
                    }

                    #region 更新TempVIP資料表狀態 狀態4 xml產生中
                    List<DBItem> updateDataList = new List<DBItem>();
                    updateDataList.Add(new DBItem("STATUS", "4", DBItem.DBDataType.String));
                    update_list = update_list.Distinct().ToList(); //去除重複值
                    string where = string.Join(" OR ", update_list);
                    //DBConnector dbconnector = new DBConnector();
                    erow = link.DBExecUpdate("VIPBPTBL", updateDataList, where);
                    #endregion

                    CareRecord care_record_m = new CareRecord();
                    LogTool lt = new LogTool();
                    foreach (var item in VIP_CareRecord)
                    {
                        time = Convert.ToDateTime(item.CREATTIME).ToString("yyyy/MM/dd HH:mm:ss");
                        id = item.CARERECORD_ID;
                        self = item.SELF;
                        string userno = item.CREATNO;
                        string feeno = item.FEENO;
                        string allergyDesc = string.Empty;
                        DateTime NowTime = DateTime.Now; //XML生成時間
                        //
                        #region --EMR--
                        try
                        {
                            listByteCode = null;
                            listJsonArray = string.Empty;
                            //listByteCode = webService.GetAllergyList(feeno);
                            //listJsonArray = string.Empty;
                            //if (listByteCode != null)
                            //{
                            //    listJsonArray = CompressTool.DecompressString(listByteCode);
                            //}
                            listJsonArray = RecursiveWebservice("GetAllergyList", feeno);

                            List<PatientInfo> patList = JsonConvert.DeserializeObject<List<PatientInfo>>(listJsonArray);
                            string msg = "";

                            if (trans_date(item.C_OTHER) != "")
                                msg += trans_date(item.C_OTHER) + "\n";// + trans_date(C)
                            if (trans_date(item.S_OTHER) != "")
                                msg += "S:" + trans_date(item.S_OTHER) + "\n";// + trans_date(S) 
                            if (trans_date(item.O_OTHER) != "")
                                msg += "O:" + trans_date(item.O_OTHER) + "\n";//+ trans_date(O) 
                            if (trans_date(item.I_OTHER) != "")
                                msg += "I:" + trans_date(item.I_OTHER) + "\n";// + trans_date(I) 
                            if (trans_date(item.E_OTHER) != "")
                                msg += "E:" + trans_date(item.E_OTHER) + "\n";// + trans_date(E)

                            if (patList != null)
                            {
                                allergyDesc = patList[0].AllergyDesc;
                            }

                            listByteCode = null;
                            listJsonArray = string.Empty;
                            //listByteCode = webService.GetPatientInfo(feeno);
                            //if (listByteCode != null)
                            //{
                            //    listJsonArray = CompressTool.DecompressString(listByteCode);
                            //}
                            listJsonArray = RecursiveWebservice("GetPatientInfo", feeno);
                            PatientInfo pt_info = JsonConvert.DeserializeObject<PatientInfo>(listJsonArray);

                            string xml = care_record_m.care_Record_Get_xml(pt_info.PatientName, pt_info.ChartNo,
                            pt_info.PatientGender, (pt_info.Age).ToString(), pt_info.BedNo, Convert.ToDateTime(pt_info.InDate).ToString("yyyyMMdd"),
                             Convert.ToDateTime(pt_info.InDate).ToString("HHmm"), allergyDesc, Convert.ToDateTime(time).ToString("yyyyMMdd"),
                            Convert.ToDateTime(time).ToString("HHmm"), item.CREATNAME, trans_date(title), msg);
                            //資料輸入人員
                            listByteCode = null;
                            listJsonArray = string.Empty;
                            //listByteCode = webService.UserName(item.CREATNO);
                            //if (listByteCode != null)
                            //{
                            //    listJsonArray = CompressTool.DecompressString(listByteCode);
                            //}

                            listJsonArray = RecursiveWebservice("UserName", item.CREATNO);
                            UserInfo user_info1 = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                            ///
                            //取得應簽章人員
                            string CREATNO = RequestName(item.CREATNO);//抓不到代簽人ID則以自己簽 由ECK自行處理錯誤資料
                            if (string.IsNullOrEmpty(CREATNO))
                            {
                                CREATNO = item.CREATNO;
                            }

                            listByteCode = null;
                            listJsonArray = string.Empty;
                            //listByteCode = webService.UserName(CREATNO);//代簽人ID
                            //if (listByteCode != null)
                            //{
                            //    listJsonArray = CompressTool.DecompressString(listByteCode);
                            //}

                            listJsonArray = RecursiveWebservice("UserName", CREATNO);
                            UserInfo user_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                            ///                        
                            string RecordTime = NowTime.ToString("yyyy/MM/dd HH:mm:ss");
                            string Temp_NowTime_Str = Convert.ToDateTime(RecordTime).ToString("yyyyMMddHHmmss");//時間採統一變數
                            string EmrXmlString = this.xml_trigger(
                                NowTime.ToString("yyyyMMddHHmmss.fffffff"), Temp_NowTime_Str + GetMd5Hash(id + self), "A000040", Convert.ToDateTime(time).ToString("yyyyMMdd"),
                                GetMd5Hash(id + self), Convert.ToDateTime(time).ToString("yyyyMMdd"), "", "",
                                user_info.EmployeesNo, user_info.EmployeesName, user_info.UserID, user_info1.CostCenterCode, user_info.CostCenterCode, pt_info.FeeNo, pt_info.ChartNo, pt_info.PatientName,
                                pt_info.PatientID, pt_info.PayInfo,
                                "C:\\EMR\\", "A000040" + GetMd5Hash(id + self) + Temp_NowTime_Str + ".xml", listJsonArray, "In_VIP_CareRecord"
                                );
                            erow = EMR_Sign(time, id, msg, title, self, feeno, CREATNO);
                            //SaveEMRLogData(id + self, GetMd5Hash(id + self), "EMR", RecordTime, "A000040" + GetMd5Hash(id + self) + Temp_NowTime_Str, xml, userno);
                            //SaveEMRLogData(id + self, GetMd5Hash(id + self), "Temp", RecordTime, Temp_NowTime_Str + "-" + GetMd5Hash(id + self), EmrXmlString, userno);
                        }
                        catch (Exception ex)
                        {
                            lt.saveLogMsg(ex.Message, "EMR_Pre_Operation_Log");
                        }
                        #endregion
                    }

                    #region 更新TempVIP資料表狀態 狀態5 xml產生完成
                    updateDataList = new List<DBItem>();
                    updateDataList.Add(new DBItem("STATUS", "5", DBItem.DBDataType.String));
                    update_list = update_list.Distinct().ToList(); //去除重複值
                    where = string.Join(" OR ", update_list);
                    erow = link.DBExecUpdate("VIPBPTBL", updateDataList, where);
                    #endregion
                }
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), "DBExecSQL", sqlstr, ex);
                Response.Write("<script>alert('新增失敗!');window.location.href='List';</script>");
            }
            finally
            {
                this.link.DBClose();
            }

            return erow;
        }

        /// <summary>
        /// 因ws定時斷線(ECK設定每天早上七點斷線)，重試WebService直到搜尋資料出現
        /// </summary>
        /// <param name="wsFun">ws方法</param>
        /// <param name="search_1">搜尋條件1</param>
        public string RecursiveWebservice(string wsFun, string search_1 = "")
        {
            LogTool log = new LogTool();
            string listJsonArray = string.Empty;
            bool abnomalData = false;
            byte[] listByteCode = null;
            int maxTimes = 20;
            int i = 0;
            for (i = 0; i < maxTimes; i++)
            {
                try
                {
                    if (wsFun == "GetAllergyList")
                    {
                        listByteCode = webService.GetAllergyList(search_1);//feeno
                        if (listByteCode != null)
                        {
                            listJsonArray = CompressTool.DecompressString(listByteCode);
                        }
                        break;
                    }
                    else if (wsFun == "GetPatientInfo")
                    {
                        listByteCode = webService.GetPatientInfo(search_1);//feeno
                        if (listByteCode != null)
                        {
                            listJsonArray = CompressTool.DecompressString(listByteCode);
                        }
                        else
                        {
                            abnomalData = true;
                        }
                        break;
                    }
                    else if (wsFun == "UserName")
                    {
                        listByteCode = webService.UserName(search_1);//CREATNO
                        if (listByteCode != null)
                        {
                            listJsonArray = CompressTool.DecompressString(listByteCode);
                        }
                        else
                        {
                            abnomalData = true;
                        }
                        break;
                    }
                }
                catch (Exception ex)
                {
                    if (i > 0)
                    {
                        Thread.Sleep(5000);
                    }
                }
            }
            if (i >= maxTimes || abnomalData)
            {
                log.saveLogMsg("wsFun：" + wsFun + "，search_1：" + search_1, "RecursiveWebservice");
            }
            return listJsonArray;
        }

        public string RequestName(string CREATNO, string USERNO = "", string Category = "")
        {
            DateTime NowTime = DateTime.Now;
            CultureInfo culture = new CultureInfo("zh-TW");
            culture.DateTimeFormat.Calendar = new TaiwanCalendar();

            var taiwanCalender = new System.Globalization.TaiwanCalendar();
            var datetime = NowTime.ToString("yyyMMdd", culture);

            string WRITE_ID = "";
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM REQUESTLIST  WHERE  WRITE_ID=trim('" + CREATNO + "') AND SET_DATE ='" + datetime + "' ";
            //DBConnector dbconnector = new DBConnector();
            link.DBExecSQL(sql, ref dt);
            if (dt.Rows.Count == 0)
            {
                if (Category != "SN") //非SN無代簽者
                {
                    return CREATNO;
                }
            }
            else //有代簽人資料且不代簽人不等於自己
            {
                WRITE_ID = dt.Rows[0]["WRITE_ID"].ToString();
                CREATNO = dt.Rows[0]["REQUEST_ID"].ToString();
                if (USERNO != CREATNO)
                {
                    return (RequestName(CREATNO));
                }
            }

            //簽章代理人記log
            write_StackOverflow_log(CREATNO, WRITE_ID, sql, Category);

            return ""; //回傳空值(VIP資料遇到空值會將輸入人放入代簽人, NIS系統會將跳轉回登入畫面並出現提示訊息)
        }

        //簽章代理人記log
        private static void write_StackOverflow_log(string REQUEST_ID, string WRITE_ID, string sql, string Category)
        {
            try
            {
                throw new StackOverflowException("BaseController.RequestName()出現無窮迴圈! 系統啟動自我保護機制(強迫User登出)");
            }
            catch (StackOverflowException stack_ex)
            {
                LogTool log = new LogTool();
                log.saveLogMsg(stack_ex.Message.ToString(), "DBExecSQL");
                log.saveLogMsg(Category + " 代簽護理師資料有誤(REQUEST_ID= " + REQUEST_ID + ", WRITE_ID= " + WRITE_ID + ")，電子簽章可能有誤! 請檢查此人代簽人資訊!，" + sql, "DBExecSQL");
            }
        }

        #region Class_Dispose

        public new void Dispose() // Implement IDisposable
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~BaseController() // the finalizer
        {
            Dispose(false);
        }

        #endregion
        /// <summary>
        /// 將傳入值判斷如果是null就顯示成"NULL"字串
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public string ValReplaceNullToStr(string values)
        {
            string TempStr = "";
            if (values == null)
            {
                TempStr = " NULL ";
            }
            else
            {
                TempStr = values;
            }
            return TempStr;
        }

        public static string[] GetVerifyStr()
        {
            // 院方提供的加密參數
            string GM_UserAccount = "admin";
            string GM_PW = "M@Y@NIS";

            // 組出金鑰
            string[] verifyStr = new string[5];
            verifyStr[0] = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            verifyStr[1] = "MAYA.NIS";
            verifyStr[2] = GM_UserAccount;
            verifyStr[3] = GetPW1(verifyStr[2]);
            verifyStr[4] = GetPW2(verifyStr[2], verifyStr[0], GM_PW);

            return verifyStr;
        }
        private static string GetPW1(string UseAccount)
        {
            string nonceStr = UseAccount;
            byte[] nonceByte = System.Text.Encoding.Default.GetBytes(nonceStr);
            string nonceBase64 = Convert.ToBase64String(nonceByte);

            return nonceBase64;
        }

        private static string GetPW2(string UseAccount, string DateTimeNow, string UserPW)
        {
            SHA512 WK_SHA512 = new SHA512CryptoServiceProvider();
            string combinStr = $"{UseAccount}1131090019{DateTimeNow}MAYA{UserPW}";
            byte[] combinByte = System.Text.Encoding.Default.GetBytes(combinStr);
            byte[] WK_SHA512_Cryptp = WK_SHA512.ComputeHash(combinByte);
            string WK_PW_Base64_SHA512 = Convert.ToBase64String(WK_SHA512_Cryptp);

            return WK_PW_Base64_SHA512;
        }

        /// <summary>
        /// 儀器上傳自動新增紀錄用(因應無使用者登入及選擇病人情境)
        /// </summary>
        /// <param name="time"></param>
        /// <param name="id"></param>
        /// <param name="title"></param>
        /// <param name="C"></param>
        /// <param name="S"></param>
        /// <param name="O"></param>
        /// <param name="I"></param>
        /// <param name="E"></param>
        /// <param name="self"></param>
        /// <param name="userno"></param>
        /// <param name="userName"></param>
        /// <param name="feeno"></param>
        /// <param name="historyID"></param>
        /// <returns></returns>
        public int Insert_CareRecord_MachineUpload(string time, string id, string title, string C, string S, string O, string I, string E, string self, string userno, string userName, string feeno, string historyID = "")
        {
            int erow = 0;
            DateTime NowTime = DateTime.Now;
            LogTool lt = new LogTool();
            CareRecord care_record_m = new CareRecord();
            string sign_userno = care_record_m.sel_guide_userno(userno, Convert.ToDateTime(time), Convert.ToDateTime(time).Hour);
            try
            {
                List<DBItem> insertDataList = new List<DBItem>();
                insertDataList.Add(new DBItem("CARERECORD_ID", id, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("GUIDE_NO", sign_userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATNAME", userName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATTIME", NowTime.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("RECORDTIME", time, DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("TITLE", trans_date(title), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("C_OTHER", trans_date(C), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("S_OTHER", trans_date(S), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("O_OTHER", trans_date(O), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("I_OTHER", trans_date(I), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("E_OTHER", trans_date(E), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("SELF", self, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("SIGN", "N", DBItem.DBDataType.String));
                if (!string.IsNullOrEmpty(historyID))
                {
                    insertDataList.Add(new DBItem("CP_HISTORYID", historyID, DBItem.DBDataType.String));
                }
                erow = link.DBExecInsert("CARERECORD_DATA", insertDataList);
            }
            catch (Exception ex)
            {
                string detail_Str = "\r\n CARERECORD_ID:" + ValReplaceNullToStr(id) + "\r\n CREATNO:" + ValReplaceNullToStr(userno) + "\r\n GUIDE_NO:" + ValReplaceNullToStr(sign_userno) + " \r\n ";
                detail_Str += "CREATNAME:" + ValReplaceNullToStr(userName) + "\r\n CREATTIME:" + ValReplaceNullToStr(NowTime.ToString("yyyy/MM/dd HH:mm")) + "\r\n RECORDTIME:" + ValReplaceNullToStr(time);
                detail_Str += "\r\n FEENO:" + ValReplaceNullToStr(feeno) + "\r\n TITLE:" + ValReplaceNullToStr(title) + "\r\n C:" + ValReplaceNullToStr(C) + "\r\n S:" + ValReplaceNullToStr(S) + "\r\n O:" + ValReplaceNullToStr(O);
                detail_Str += "\r\n I:" + ValReplaceNullToStr(I) + "\r\n E:" + ValReplaceNullToStr(E);
                lt.saveLogMsg(ex.Message + detail_Str, "CareRecord_Log_Red");
            }
            if (erow > 0)
            {
                #region --EMR--
                try
                {
                    string msg = "";
                    if (trans_date(C) != "")
                        msg += trans_date(C) + "\n";// + trans_date(C)
                    if (trans_date(S) != "")
                        msg += "S:" + trans_date(S) + "\n";// + trans_date(S) 
                    if (trans_date(O) != "")
                        msg += "O:" + trans_date(O) + "\n";//+ trans_date(O) 
                    if (trans_date(I) != "")
                        msg += "I:" + trans_date(I) + "\n";// + trans_date(I) 
                    if (trans_date(E) != "")
                        msg += "E:" + trans_date(E) + "\n";// + trans_date(E)


                    erow = EMR_Sign(time, id, msg, title, self, feeno, userno);
                }
                catch (Exception ex)
                {
                    lt.saveLogMsg(ex.Message, "EMR_Pre_Operation_Log");
                }
                #endregion
            }


            return erow;
        }

        //相關紀錄連默許計價
        public ActionResult SaveBillingRecord(List<Bill_RECORD> data, DateTime? recordDate = null)
        {
            RESPONSE_MSG jsonResult = new RESPONSE_MSG();
            List<DBItem> insertDataList = new List<DBItem>();
            int erow = 0;
            UserInfo ui = (UserInfo)Session["UserInfo"];
            NIS.Data.PatientInfo pf = (NIS.Data.PatientInfo)Session["PatInfo"];

            string userno = ui.EmployeesNo;
            string feeno = pf.FeeNo;
            string now = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

            // 有指定時間時
            if (recordDate != null)
            {
                now = recordDate.Value.ToString("yyyy/MM/dd HH:mm:ss");
            }

            string date = now;
            string serial = creatid("BILL_DATA", userno, feeno, "0");

            // 已出院不做計價動作
            if (Convert.ToDateTime(pf.OutDate).ToString("yyyy").ToString() != "0001" && pf.OutDate < DateTime.Now)
            {
                jsonResult.status = RESPONSE_STATUS.SUCCESS;
                return Content(JsonConvert.SerializeObject(jsonResult), "application/json");
            }

            try
            {
                //Master
                insertDataList.Clear();
                insertDataList.Add(new DBItem("SERIAL_M", serial, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATE_DATE", date, DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("CREATE_ID", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("RECORD_DATE", date, DBItem.DBDataType.DataTime));
                erow = this.link.DBExecInsert("DATA_BILLING_TEMP_MASTER", insertDataList);

                if (erow > 0)
                {
                    //Deatil
                    for (int i = 0; i < data.Count(); i++)
                    {
                        string ho_id = data[i].HO_ID;
                        //查詢價格資訊
                        string sqlstr = "SELECT * FROM SYS_BILL_PRICE WHERE CODE = '" + ho_id + "'";
                        DataTable dtPrice = this.link.DBExecSQL(sqlstr);

                        string itemName = "";
                        string itemType = "";
                        string itemPrice = "";
                        string selfPrice = "";
                        string nhCode = "";

                        if (dtPrice.Rows.Count > 0)
                        {
                            itemName = dtPrice.Rows[0]["CHINESE_NAME"].ToString();

                            var nh_type = dtPrice.Rows[0]["NH_ORDER_TYPE"].ToString();
                            if (nh_type == "1")
                            {
                                nh_type = "藥物";
                            }
                            else if (nh_type == "2")
                            {
                                nh_type = "處置";
                            }
                            else if (nh_type == "3")
                            {
                                nh_type = "衛材";
                            }

                            itemType = nh_type;
                            itemPrice = dtPrice.Rows[0]["NH_PRICE"].ToString();
                            selfPrice = dtPrice.Rows[0]["SELF_PRICE"].ToString();
                            nhCode = dtPrice.Rows[0]["NH_CODE"].ToString();
                        }



                        string serialD = creatid("BILL_DATA_D", userno, feeno, "0");
                        string cover = "N";
                        DateTime record = DateTime.Parse(date);
                        DateTime nowTime = DateTime.Parse(DateTime.Now.ToString("yyyy/MM/dd 00:00:00"));
                        int price = int.Parse(itemPrice) * int.Parse(data[i].COUNT);

                        if (record < nowTime)
                        {
                            cover = "Y";
                        }

                        insertDataList.Clear();
                        insertDataList.Add(new DBItem("SERIAL_M", serial, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("SERIAL_D", serialD, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("HO_ID", ho_id, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("ITEM_NAME", itemName, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("ITEM_TYPE", itemType, DBItem.DBDataType.String));
                        //insertDataList.Add(new DBItem("ITEM_IDENTITY", "健保", DBItem.DBDataType.String));

                        var IDENTITY = data[i].IDENTITY;
                        if (string.IsNullOrWhiteSpace(IDENTITY))
                        {
                            IDENTITY = "健保";
                        }

                        insertDataList.Add(new DBItem("ITEM_IDENTITY", IDENTITY, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("ITEM_PRICE", price.ToString(), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("COUNT", data[i].COUNT, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("COSTCODE", userinfo.CostCenterCode.Trim(), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CREATE_ID", userno, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("RECORD_DATE", date, DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("SELF_PRICE", selfPrice, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("NH_PRICE", itemPrice, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("COVER", cover, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("SET_NAME", "", DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CHARTNO", ptinfo.ChartNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("BEDNO", ptinfo.BedNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("PT_COSTCODE", ptinfo.CostCenterNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("DOCTOR", ptinfo.DocNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("NH_CODE", nhCode, DBItem.DBDataType.String));
                        erow = this.link.DBExecInsert("DATA_BILLING_TEMP_DETAIL", insertDataList);
                    
                    }
                }
            }
            catch (Exception ex)
            {
                jsonResult.status = RESPONSE_STATUS.ERROR;
                jsonResult.message = ex.ToString();

            }
            if (erow > 0)
            {
                jsonResult.status = RESPONSE_STATUS.SUCCESS;
            }
            else
            {
                jsonResult.status = RESPONSE_STATUS.ERROR;
            }

            return Content(JsonConvert.SerializeObject(jsonResult), "application/json");
        }

        //相關紀錄連默許計價
        public ActionResult SaveBillingRecordWound(List<Bill_RECORD> data, string woundValue = "", string woundPosition = "")
        {
            RESPONSE_MSG jsonResult = new RESPONSE_MSG();
            List<DBItem> insertDataList = new List<DBItem>();
            int erow = 0;
            UserInfo ui = (UserInfo)Session["UserInfo"];
            NIS.Data.PatientInfo pf = (NIS.Data.PatientInfo)Session["PatInfo"];

            string userno = ui.EmployeesNo;
            string feeno = pf.FeeNo;
            string now = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            //邏輯調整，不以每小時加總，以當下時間紀錄
            string date = now;
            
            string serial = creatid("BILL_DATA", userno, feeno, "0");

            // 已出院不做計價動作
            if (Convert.ToDateTime(pf.OutDate).ToString("yyyy").ToString() != "0001" && pf.OutDate < DateTime.Now)
            {
                jsonResult.status = RESPONSE_STATUS.SUCCESS;
                return Content(JsonConvert.SerializeObject(jsonResult), "application/json");
            }

            try
            {
                //Master
                insertDataList.Clear();
                insertDataList.Add(new DBItem("SERIAL_M", serial, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATE_DATE", date, DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("CREATE_ID", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("RECORD_DATE", date, DBItem.DBDataType.DataTime));
                erow = this.link.DBExecInsert("DATA_BILLING_TEMP_MASTER", insertDataList);

                if (erow > 0)
                {
                    //Deatil
                    for (int i = 0; i < data.Count(); i++)
                    {
                        string ho_id = data[i].HO_ID;
                        //查詢價格資訊
                        string sqlstr = "SELECT * FROM SYS_BILL_PRICE WHERE CODE = '" + ho_id + "'";
                        DataTable dtPrice = this.link.DBExecSQL(sqlstr);

                        string itemName = "";
                        string itemType = "";
                        string itemPrice = "";
                        string selfPrice = "";
                        string nhCode = "";

                        if (dtPrice.Rows.Count > 0)
                        {
                            itemName = dtPrice.Rows[0]["CHINESE_NAME"].ToString();

                            var nh_type = dtPrice.Rows[0]["NH_ORDER_TYPE"].ToString();
                            if (nh_type == "1")
                            {
                                nh_type = "藥物";
                            }
                            else if (nh_type == "2")
                            {
                                nh_type = "處置";
                            }
                            else if (nh_type == "3")
                            {
                                nh_type = "衛材";
                            }

                            itemType = nh_type;
                            itemPrice = dtPrice.Rows[0]["NH_PRICE"].ToString();
                            selfPrice = dtPrice.Rows[0]["SELF_PRICE"].ToString();
                            nhCode = dtPrice.Rows[0]["NH_CODE"].ToString();
                        }

                        string serialD = creatid("BILL_DATA_D", userno, feeno, "0");

                        //寫傷口log

                        string serialWound = creatid("BILL_WOUND", userno, feeno, "0");
                        insertDataList.Clear();
                        insertDataList.Add(new DBItem("SERIAL", serialWound, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CREATE_DATE", now, DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("CREATE_ID", userno, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("BILL_ID", serialD, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("WOUND_VALUE", woundValue, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("WOUND_POSITION", woundPosition, DBItem.DBDataType.String));
                        erow = this.link.DBExecInsert("NIS_WOUND_LOG", insertDataList);


                        string cover = "N";
                        DateTime record = DateTime.Parse(date);
                        DateTime nowTime = DateTime.Parse(DateTime.Now.ToString("yyyy/MM/dd 00:00:00"));

                        if (record < nowTime)
                        {
                            cover = "Y";
                        }

                        insertDataList.Clear();
                        insertDataList.Add(new DBItem("SERIAL_M", serial, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("SERIAL_D", serialD, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("HO_ID", ho_id, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("ITEM_NAME", itemName, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("ITEM_TYPE", itemType, DBItem.DBDataType.String));
                        //insertDataList.Add(new DBItem("ITEM_IDENTITY", "健保", DBItem.DBDataType.String));

                        var IDENTITY = data[i].IDENTITY;
                        if (string.IsNullOrWhiteSpace(IDENTITY))
                        {
                            IDENTITY = "健保";
                        }

                        insertDataList.Add(new DBItem("ITEM_IDENTITY", IDENTITY, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("ITEM_PRICE", itemPrice, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("COUNT", data[i].COUNT, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("COSTCODE", userinfo.CostCenterCode.Trim(), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CREATE_ID", userno, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("RECORD_DATE", date, DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("SELF_PRICE", selfPrice, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("NH_PRICE", itemPrice, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("COVER", cover, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("SET_NAME", "", DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CHARTNO", ptinfo.ChartNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("BEDNO", ptinfo.BedNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("PT_COSTCODE", ptinfo.CostCenterNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("DOCTOR", ptinfo.DocNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("NH_CODE", nhCode, DBItem.DBDataType.String));
                        erow = this.link.DBExecInsert("DATA_BILLING_TEMP_DETAIL", insertDataList);
                    }
                }
            }
            catch (Exception ex)
            {
                jsonResult.status = RESPONSE_STATUS.ERROR;
                jsonResult.message = ex.ToString();

            }
            if (erow > 0)
            {
                jsonResult.status = RESPONSE_STATUS.SUCCESS;
            }
            else
            {
                jsonResult.status = RESPONSE_STATUS.ERROR;
            }

            return Content(JsonConvert.SerializeObject(jsonResult), "application/json");
        }

        /// <summary>
        /// 新增護理紀錄對應表
        /// </summary>
        /// <param name="time">紀錄時間</param>
        /// <param name="id">TABLE_ID</param>
        /// <param name="pid">CARERECORD_ID</param>
        /// <param name="self">此筆記錄可於護理紀錄修改傳入(CARERECORD) 不可修改傳入 table name</param>
        public int Insert_CareRecordMapper(string time, string id, string pid, string self)
        {
            int erow = 0;
            if (Session["PatInfo"] != null)
            {
                string userno = userinfo.EmployeesNo;
                string feeno = ptinfo.FeeNo;
                DateTime NowTime = DateTime.Now;
                LogTool lt = new LogTool();
                CareRecord care_record_m = new CareRecord();
                string sign_userno = care_record_m.sel_guide_userno(userno, Convert.ToDateTime(time), Convert.ToDateTime(time).Hour);
                try
                {
                    List<DBItem> insertDataList = new List<DBItem>();
                    insertDataList.Add(new DBItem("CHILD_ID", id, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CARERECORD_ID", pid, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("SELF", self, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATTIME", time, DBItem.DBDataType.DataTime));
                    erow = link.DBExecInsert("ASSESSMENTMAPPER", insertDataList);
                }
                catch (Exception ex)
                {
                    string detail_Str = "\r\n CARERECORD_ID:" + ValReplaceNullToStr(pid) + "\r\n CREATNO:" + ValReplaceNullToStr(userno) + "\r\n GUIDE_NO:" + ValReplaceNullToStr(sign_userno) + " \r\n ";
                    detail_Str += "CREATNAME:" + ValReplaceNullToStr(userinfo.EmployeesName) + "\r\n CREATTIME:" + ValReplaceNullToStr(NowTime.ToString("yyyy/MM/dd HH:mm")) + "\r\n RECORDTIME:" + ValReplaceNullToStr(time);
                    detail_Str += "\r\n FEENO:" + ValReplaceNullToStr(feeno) + "\r\n id:" + ValReplaceNullToStr(id) + "\r\n pid:" + ValReplaceNullToStr(pid) + "\r\n self:" + ValReplaceNullToStr(self);
                    lt.saveLogMsg(ex.Message + detail_Str, "CareRecord_Log_Red");
                }
            }

            return erow;
        }

        /// <summary>
        /// 讀取護理紀錄對應表_子KEY搜尋
        /// </summary>
        /// <param name="id">TABLE_ID</param>
        public string Get_CareRecordMapper_Id(string id)
        {
            if (Session["PatInfo"] != null)
            {
                string userno = userinfo.EmployeesNo;
                string feeno = ptinfo.FeeNo;
                DateTime NowTime = DateTime.Now;
                LogTool lt = new LogTool();
                try
                {
                    DataTable dt = new DataTable();
                    string sql = $"SELECT * FROM ASSESSMENTMAPPER WHERE CHILD_ID = '{id}'";
                    link.DBExecSQL(sql, ref dt);
                    if (dt.Rows.Count != 0)
                    {
                        return dt.Rows[0]["CARERECORD_ID"].ToString();
                         
                    }
                }
                catch (Exception ex)
                {
                    string detail_Str = "\r\n TABLE_ID:" + ValReplaceNullToStr(id) + "\r\n CREATNO:" + ValReplaceNullToStr(userno) + "\r\n GUIDE_NO:";
                    detail_Str += "CREATNAME:" + ValReplaceNullToStr(userinfo.EmployeesName) + "\r\n CREATTIME:" + ValReplaceNullToStr(NowTime.ToString("yyyy/MM/dd HH:mm"));
                    detail_Str += "\r\n FEENO:" + ValReplaceNullToStr(feeno) + "\r\n id:" + ValReplaceNullToStr(id);
                    lt.saveLogMsg(ex.Message + detail_Str, "CareRecord_Log_Red");
                }
            }
            return null;
        }

        /// <summary>
        /// 讀取護理紀錄對應表_母KEY搜尋
        /// </summary>
        /// <param name="pid">CARERECORD_ID</param>
        /// <param name="self">此筆記錄可於護理紀錄修改傳入(CARERECORD) 不可修改傳入 table name</param>
        public List<string> Get_CareRecordMapper_Pid(string pid, string self = "")
        {
            List<string> childList = new List<string>();
            if (Session["PatInfo"] != null)
            {
                string userno = userinfo.EmployeesNo;
                string feeno = ptinfo.FeeNo;
                DateTime NowTime = DateTime.Now;
                LogTool lt = new LogTool();
                try
                {
                    DataTable dt = new DataTable();
                    string sql = $@"SELECT M.CHILD_ID FROM ASSESSMENTMAPPER M 
LEFT JOIN CARERECORD_DATA CR ON CR.CARERECORD_ID = M.CHILD_ID AND CR.DELETED IS NULL 
LEFT JOIN ASSESSMENTMASTER AM ON AM.TABLEID = M.CARERECORD_ID AND AM.DELETED IS NULL 
WHERE M.CARERECORD_ID = '{pid}'";
                    if (!string.IsNullOrEmpty(self))
                    {
                        sql += $" AND CR.SELF = '{self}'";
                    }
                    link.DBExecSQL(sql, ref dt);
                    if (dt.Rows.Count != 0)
                    {
                        foreach (DataRow row in dt.Rows)
                        {
                            childList.Add(row["CHILD_ID"].ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    string detail_Str = "\r\n CARERECORD_ID:" + ValReplaceNullToStr(pid) + "\r\n CREATNO:" + ValReplaceNullToStr(userno) + "\r\n GUIDE_NO:";
                    detail_Str += "CREATNAME:" + ValReplaceNullToStr(userinfo.EmployeesName) + "\r\n CREATTIME:" + ValReplaceNullToStr(NowTime.ToString("yyyy/MM/dd HH:mm"));
                    detail_Str += "\r\n FEENO:" + ValReplaceNullToStr(feeno) + "\r\n pid:" + ValReplaceNullToStr(pid) + "\r\n self:" + ValReplaceNullToStr(self);
                    lt.saveLogMsg(ex.Message + detail_Str, "CareRecord_Log_Red");
                }
            }

            return childList;
        }

        /// <summary>
        /// 刪除護理紀錄對應表_子KEY比對
        /// </summary>
        /// <param name="id">TABLE_ID</param>
        public void Delete_CareRecordMapper_Id(string id)
        {
            if (Session["PatInfo"] != null)
            {
                string userno = userinfo.EmployeesNo;
                string feeno = ptinfo.FeeNo;
                DateTime NowTime = DateTime.Now;
                LogTool lt = new LogTool();
                try
                {
                    DataTable dt = new DataTable();
                    string sql = $@"SELECT M.CARERECORD_ID FROM ASSESSMENTMAPPER M
LEFT JOIN CARERECORD_DATA CR ON CR.CARERECORD_ID = M.CHILD_ID AND CR.DELETED IS NULL
LEFT JOIN ASSESSMENTMASTER AM ON AM.TABLEID = M.CARERECORD_ID AND AM.DELETED IS NULL
WHERE M.CHILD_ID = '{id}'";
                    link.DBExecSQL(sql);
                }
                catch (Exception ex)
                {
                    string detail_Str = "\r\n TABLE_ID:" + ValReplaceNullToStr(id) + "\r\n CREATNO:" + ValReplaceNullToStr(userno) + "\r\n GUIDE_NO:";
                    detail_Str += "CREATNAME:" + ValReplaceNullToStr(userinfo.EmployeesName) + "\r\n CREATTIME:" + ValReplaceNullToStr(NowTime.ToString("yyyy/MM/dd HH:mm"));
                    detail_Str += "\r\n FEENO:" + ValReplaceNullToStr(feeno) + "\r\n id:" + ValReplaceNullToStr(id);
                    lt.saveLogMsg(ex.Message + detail_Str, "CareRecord_Log_Red");
                }
            }
        }

        /// <summary>
        /// 刪除護理紀錄對應表_母KEY比對
        /// </summary>
        /// <param name="pid">CARERECORD_ID</param>
        public void Delete_CareRecordMapper_Pid(string pid)
        {
            if (Session["PatInfo"] != null)
            {
                string userno = userinfo.EmployeesNo;
                string feeno = ptinfo.FeeNo;
                DateTime NowTime = DateTime.Now;
                LogTool lt = new LogTool();
                try
                {
                    DataTable dt = new DataTable();
                    string sql = $"DELETE FROM ASSESSMENTMAPPER WHERE CARERECORD_ID = '{pid}'";
                    link.DBExecSQL(sql);
                }
                catch (Exception ex)
                {
                    string detail_Str = "\r\n CARERECORD_ID:" + ValReplaceNullToStr(pid) + "\r\n CREATNO:" + ValReplaceNullToStr(userno) + "\r\n GUIDE_NO:";
                    detail_Str += "CREATNAME:" + ValReplaceNullToStr(userinfo.EmployeesName) + "\r\n CREATTIME:" + ValReplaceNullToStr(NowTime.ToString("yyyy/MM/dd HH:mm"));
                    detail_Str += "\r\n FEENO:" + ValReplaceNullToStr(feeno) + "\r\n pid:" + ValReplaceNullToStr(pid);
                    lt.saveLogMsg(ex.Message + detail_Str, "CareRecord_Log_Red");
                }
            }
        }
    }
}