using Com.Mayaminer;
using DocumentFormat.OpenXml.Office2010.Excel;
using iTextSharp.tool.xml.css;
using Newtonsoft.Json;
using NIS.Controllers;
using NIS.Data;
using NIS.Models;
using NIS.UtilTool;
using NIS.WebService;
using Oracle.ManagedDataAccess.Client;
using Org.BouncyCastle.Ocsp;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.DataVisualization.Charting;

namespace NIS.Controllers
{
    public class MedInfo
    {
        public string med_code { set; get; }
        public string med_time { set; get; }
        public string ud_seq { set; get; }
        public string flag { set; get; }
    }

    public class MedInfoB
    {
        public string med_code { set; get; }
        public string med_time { set; get; }
        public string ud_seq { set; get; }
        public string ud_seq_new { set; get; }
        public string flag { set; get; }
        public string DrugName { set; get; }
        public string GenericDrugs { set; get; }
        public string DrugEffects { set; get; }
        public string DrugSideEffects { set; get; }
        public string DrugPicPath { set; get; }
        public string DrugHref { set; get; }
        public string ud_dose { set; get; }
        public string ud_unit { set; get; }
        public string ud_path { set; get; }
        public string ud_type { set; get; }
        public string ud_cir { set; get; }
        public string ud_seq_o { set; get; }
        public string ud_dose_o { set; get; }
        public string ud_unit_o { set; get; }
        public string ud_path_o { set; get; }
        public string ud_type_o { set; get; }
        public string ud_cir_o { set; get; }
    }

    public class CommonMedicationController : BaseController
    {
        private CommData cd;    //常用資料Module
        private CommonMedication cm;
        private DBConnector link;
        private DataTable dt_udorder = new DataTable();
        private DataTable dt_stat = new DataTable();
        private DataTable dt_reg = new DataTable();
        private DataTable dt_prn = new DataTable();
        private DataTable dt_iv = new DataTable();
        private DataTable dt_all = new DataTable();

        private LogTool log;
        private string mode = MvcApplication.iniObj.NisSetting.ServerMode.ToString();

        public CommonMedicationController()
        {
            this.cd = new CommData();
            this.cm = new CommonMedication();
            this.link = new DBConnector();
            this.log = new LogTool();
        }

        public ActionResult Index(string url = "")
        {
            ViewBag.category = (userinfo != null ? userinfo.Category : null);
            ViewBag.url = url;
            return View();
        }

        #region Med_NurseExecute 執行一般給藥
        public ActionResult Med_NurseExecute(FormCollection form, string ExecFlag)
        {
            #region 給藥後醫師才DC在 NIS_EMRMS 押上 CANCEL Flag
            List<DBItem> updList = new List<DBItem>();
            string where = "CNCL_FLAG = 'I' and EMP_NO ='" + userinfo.EmployeesNo.ToString() + "' and ORDER_NO in (";
            where += "select UD_SEQPK from H_drug_execute where INVALID_DATE is not null and EXEC_ID ='" + userinfo.EmployeesNo.ToString() + "'";
            where += ")";
            updList.Add(new DBItem("CNCL_FLAG", "D", DBItem.DBDataType.String));
            int effRow = this.link.DBExecUpdate("NIS_EMRMS", updList, where);
            #endregion

            if (Session["PatInfo"] == null)
            { return Redirect("../VitalSign/VitalSignSingle"); }

            if (Check_Session(ExecFlag))
                return Redirect("../VitalSign/VitalSignSingle");
            else if (ExecFlag == "weberror")
                return View();

            //宣告病患_取得住院序號
            PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
            string feeno = ptInfo.FeeNo;
            string start = "";           //查詢_開始日期時間
            string end = "";             //查詢_結束日期時間
            string udodrgcode = "";      //藥包條碼
            ViewBag.feeno = feeno;
            ViewBag.chrno = ptInfo.ChartNo;

            #region 取得STAT,REG,PRN 用藥
            #region 前置處理
            if (ExecFlag == "" || ExecFlag == null || ExecFlag == "yes" || ExecFlag == "no")
            {
                ExecFlag = "first";
                start = DateTime.Now.AddHours(-8).ToString("yyyy/MM/dd HH:mm:ss");
                end = DateTime.Now.AddHours(+1).ToString("yyyy/MM/dd HH:mm:ss");
            }
            else
            {
                start = Convert.ToDateTime(form["start_date"] + " " + form["start_time"]).ToString("yyyy/MM/dd HH:mm:ss");    //查詢_開始日期時間
                end = Convert.ToDateTime(form["end_date"] + " " + form["end_time"]).ToString("yyyy/MM/dd HH:mm:ss");          //查詢_結束日期時間
                udodrgcode = form["udodrgcode"];     //藥包條碼
                ViewBag.start_date = Convert.ToDateTime(form["start_date"]).ToString("yyyy/MM/dd");
                ViewBag.start_time = Convert.ToDateTime(form["start_time"]).ToString("HH:mm");
                ViewBag.end_date = Convert.ToDateTime(form["end_date"]).ToString("yyyy/MM/dd");
                ViewBag.end_time = Convert.ToDateTime(form["end_time"]).ToString("HH:mm");
            }
            switch (ExecFlag)
            {
                case "quiry":
                    break;
                case "quiryAll":
                    break;
                case "yes":
                    ExecFlag = "quiry";
                    break;
                case "no":
                    ExecFlag = "quiry";
                    break;
                default:
                    break;
            }
            #endregion

            //PROCESS UD FORM WEB SERVICE
            var str1 = WebS_UdOrder(feeno, start, end, ExecFlag, "");
            if (str1 == "weberror")
                return Redirect("../CommonMedication/Med_NurseExecute?ExecFlag=weberror");

            #region 檢查注射部位是否有資料
            if (cm.GetSpecialDrug_Set(feeno) == false)
            {
                List<DBItem> insertList = new List<DBItem>();
                insertList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                insertList.Add(new DBItem("INDATE", DateTime.Now.ToString("yyyy/MM/dd hh:mm"), DBItem.DBDataType.String));
                insertList.Add(new DBItem("REVIEW", "EFGABC", DBItem.DBDataType.String));
                insertList.Add(new DBItem("INSDT", DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss"), DBItem.DBDataType.String));
                insertList.Add(new DBItem("INSOP", "SYSTEM", DBItem.DBDataType.String));
                insertList.Add(new DBItem("INSOPNAME", "SYSTEM", DBItem.DBDataType.String));
                this.link.DBExecInsert("SPECIALDRUG_SET", insertList);
            }
            string ls_position = cm.Get_Position(feeno);
            #endregion

            #region 取得STAT 用藥
            if (dt_stat.Rows.Count > 0)
            {
                dt_stat.Columns.Add("use_time");//使用時間
                dt_stat.Columns.Add("use_seq");//使用順序
                dt_stat.Columns.Add("use_date");
                foreach (DataRow dr in dt_stat.Rows)
                {
                    /*  if (dr["SEND_AMT"].ToString() != "String")
                      {
                          if ( decimal.Parse(dr["SEND_AMT"].ToString()) == decimal.Parse(dr["BACK_AMT"].ToString()))
                          { continue; }
                      }*/
                    DataTable dtt = cm.Get_DrugListTime(dr["UD_SEQ"].ToString(), "", start, end, ExecFlag);
                    if (dtt != null && dtt.Rows.Count > 0)
                    {
                        dr["use_seq"] = dtt.Rows[0]["use_seq"].ToString().TrimEnd(',');
                        dr["use_date"] = DateTime.Now.ToString("yyyy/MM/dd");
                        dr["use_time"] = DateTime.Now.ToString("HH:mm");
                    }
                    else
                    {
                        dr["use_time"] = "now";
                    }
                    // 20140422 新增胰島素判斷條件，藥品代碼第一碼需為 I
                    //if (dr["UD_PATH"].ToString().Trim() == "SC" || dr["DRUG_TYPE"].ToString().Trim() == "E")
                    if (dr["UD_PATH"].ToString().Trim() == "SC" || (dr["DRUG_TYPE"].ToString().Trim() == "E" && dr["MED_CODE"].ToString().Substring(0, 1) == "I" && dr["UD_PATH"].ToString().Trim() == "SC"))
                    {
                        dr["POSITION"] = ls_position;
                    }
                }
                dt_stat.DefaultView.RowFilter = "use_time <> 'now'";
                if (dt_stat.DefaultView.Count > 0)
                    ViewBag.dt_stat = dt_stat.DefaultView.ToTable();
            }
            #endregion
            #region 取得REG 用藥
            if (dt_reg.Rows.Count > 0)
            {
                dt_reg.Columns.Add("use_time");//使用時間
                dt_reg.Columns.Add("use_seq");//使用順序
                dt_reg.Columns.Add("use_date");
                dt_reg.Columns.Add("reason");
                dt_reg.Columns.Add("reasontype");
                foreach (DataRow dr in dt_reg.Rows)
                {
                    DataTable dtt = cm.Get_DrugListTime(dr["UD_SEQ"].ToString(), "", start, end, ExecFlag);
                    if (dtt != null && dtt.Rows.Count > 0)
                    {
                        dr["use_seq"] = dtt.Rows[0]["use_seq"].ToString().TrimEnd(',');
                        dr["use_time"] = dtt.Rows[0]["use_time"].ToString().TrimEnd(',');
                        dr["use_date"] = dtt.Rows[0]["use_date"].ToString().TrimEnd(',');
                        dr["reason"] = dtt.Rows[0]["reason1"].ToString();
                        dr["reasontype"] = dtt.Rows[0]["reasontype1"].ToString();
                    }
                    else
                    {
                        dr["use_time"] = "now";
                    }
                    // 20140422 新增胰島素判斷條件，藥品代碼第一碼需為 I
                    //if (dr["UD_PATH"].ToString().Trim() == "SC" || dr["DRUG_TYPE"].ToString().Trim() == "E")
                    if (dr["UD_PATH"].ToString().Trim() == "SC" || (dr["DRUG_TYPE"].ToString().Trim() == "E" && dr["MED_CODE"].ToString().Substring(0, 1) == "I" && dr["UD_PATH"].ToString().Trim() == "SC"))
                    {
                        //dr["POSITION"] = cm.Get_Position(feeno);
                        dr["POSITION"] = ls_position;
                    }
                }
                dt_reg.DefaultView.RowFilter = "use_time <> 'now'";
                if (dt_reg.DefaultView.Count > 0)
                    ViewBag.dt_reg = dt_reg.DefaultView.ToTable();
            }
            #endregion
            #region 取得PRN 用藥
            if (dt_prn.Rows.Count > 0)
            {
                dt_prn.Columns.Add("use_seq");//PKKEY
                dt_prn.Columns.Add("use_date"); //exec_date
                dt_prn.Columns.Add("use_time"); //exec_date
                dt_prn.Columns.Add("exec_date"); //exec_date
                dt_prn.Columns.Add("exec_count"); //exec_count
                dt_prn.Columns.Add("max_count"); //max_count
                foreach (DataRow dr in dt_prn.Rows)
                {
                    DataTable dtt = cm.Get_DrugListTime_PRN(dr["UD_SEQ"].ToString(), "P");
                    if (dtt != null && dtt.Rows.Count > 0)
                    {
                        dr["use_seq"] = dtt.Rows[0]["ud_seq"].ToString() + dtt.Rows[0]["MAXSEQ"].ToString();
                        dr["exec_date"] = Convert.ToDateTime(dtt.Rows[0]["DRUG_DATE"].ToString()).ToString("yyyy/MM/dd HH:mm");
                        dr["exec_count"] = dtt.Rows[0]["exec_count"].ToString();
                    }
                    else
                    {
                        dr["use_seq"] = dr["UD_SEQ"].ToString() + "001";
                        dr["exec_date"] = "";
                        dr["exec_count"] = "0";
                    }
                    dr["use_date"] = DateTime.Now.ToString("yyyy/MM/dd");
                    dr["use_time"] = DateTime.Now.ToString("HH:mm");
                    //20140416 因三院要求 PRN 不限次數
                    if (dr["ud_cir"].ToString().Trim() == "ASORDER" || dr["ud_cir"].ToString().Trim() == "PRN")
                    {
                        dr["max_count"] = "0";
                    }
                    else
                    {
                        dr["max_count"] = dr["DAY_CNT"].ToString().Trim();
                    }
                    // 20140422 新增胰島素判斷條件，藥品代碼第一碼需為 I
                    if (dr["UD_PATH"].ToString().Trim() == "SC" || (dr["DRUG_TYPE"].ToString().Trim() == "E" && dr["MED_CODE"].ToString().Substring(0, 1) == "I" && dr["UD_PATH"].ToString().Trim() == "SC"))
                    {
                        //dr["POSITION"] = cm.Get_Position(feeno);
                        dr["POSITION"] = ls_position;
                    }
                }
                if (dt_prn.Rows.Count > 0)
                    ViewBag.dt_prn = dt_prn;
            }
            #endregion
            #region 取得點滴 用藥
            if (dt_iv.Rows.Count > 0)
            {
                dt_iv.Columns.Add("use_seq");//PKKEY
                dt_iv.Columns.Add("use_date"); //exec_date
                dt_iv.Columns.Add("use_time"); //exec_date
                dt_iv.Columns.Add("exec_date"); //exec_date
                dt_iv.Columns.Add("exec_count"); //exec_count
                dt_iv.Columns.Add("max_count"); //max_count
                foreach (DataRow dr in dt_iv.Rows)
                {
                    DataTable dtt = cm.Get_DrugListTime_PRN(dr["UD_SEQ"].ToString(), "V");
                    if (dtt != null && dtt.Rows.Count > 0)
                    {
                        dr["use_seq"] = dtt.Rows[0]["ud_seq"].ToString() + dtt.Rows[0]["MAXSEQ"].ToString();
                        dr["exec_date"] = Convert.ToDateTime(dtt.Rows[0]["DRUG_DATE"].ToString()).ToString("yyyy/MM/dd HH:mm");
                        dr["exec_count"] = dtt.Rows[0]["exec_count"].ToString();
                    }
                    else
                    {
                        dr["use_seq"] = dr["UD_SEQ"].ToString() + "001";
                        dr["exec_date"] = "";
                        dr["exec_count"] = "0";
                    }
                    dr["use_date"] = DateTime.Now.ToString("yyyy/MM/dd");
                    dr["use_time"] = DateTime.Now.ToString("HH:mm");
                    dr["max_count"] = Convert.ToInt16(dr["DAY_CNT"].ToString()) * (int)Math.Ceiling(decimal.Parse(dr["UD_DOSE"].ToString()));
                }

                if (dt_iv.Rows.Count > 0)
                    ViewBag.dt_iv = dt_iv;

            }
            #endregion
            #endregion
            List<SelectListItem> position_list = new List<SelectListItem>();
            position_list.Add(new SelectListItem { Text = "請選擇", Value = "0" });
            position_list.Add(new SelectListItem { Text = "A (右上臂)", Value = "A" });
            position_list.Add(new SelectListItem { Text = "E (左上臂)", Value = "E" });
            position_list.Add(new SelectListItem { Text = "B (右腹部)", Value = "B" });
            position_list.Add(new SelectListItem { Text = "F (左腹部)", Value = "F" });
            position_list.Add(new SelectListItem { Text = "C (右大腿)", Value = "C" });
            position_list.Add(new SelectListItem { Text = "G (左大腿)", Value = "G" });
            //註：三院共識移除臀部部位
            //position_list.Add(new SelectListItem { Text = "H (左臀)", Value = "H" });
            //position_list.Add(new SelectListItem { Text = "D (右臀)", Value = "D" });
            ViewBag.position_list = position_list;
            //取得可注射部位
            string position_check = "EFGABC";
            position_check = cm.Get_Position_set(feeno);
            ViewBag.position_check = position_check;
            return View();
        }
        #endregion

        #region Med_NurseExecuteCheckList 執行一般給藥最後確認
        public ActionResult Med_NurseExecuteCheckList(FormCollection form, List<DRUG_EXECUTE> data, string ExecFlag)
        {

            List<DRUG_EXECUTE> drug_exec = new List<DRUG_EXECUTE>();
            if (data != null)
            {
                for (int i = 0; i < data.Count; i++)
                {
                    if (data[i].ORDSTATUS == "0" || data[i].ORDSTATUS == "2")
                    {
                        drug_exec.Add(data[i]);
                    }
                }
            }
            ViewData["drug_exec"] = drug_exec;
            return View();
        }
        [ValidateInput(false)]
        public ActionResult Med_NurseExecuteCheckList_New(FormCollection form, List<DRUG_EXECUTE> data, string ExecFlag)
        {            //判斷有無病人session
            if (Session["PatInfo"] != null)
            {
                PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
                if (Request["feeno"].ToString() != ptInfo.FeeNo)
                {
                    byte[] PatientInfoResultByte = webService.GetPatientInfo(Request["feeno"].ToString());
                    if (PatientInfoResultByte != null)
                    {
                        string PatientInfoResult = CompressTool.DecompressString(PatientInfoResultByte);
                        ptInfo = JsonConvert.DeserializeObject<PatientInfo>(PatientInfoResult);
                    }
                }
                bool success_dt = true;//預設存資料庫成功
                success_dt = updDrugExecute(data, ptInfo.FeeNo, ptInfo.CostCenterNo);
                //Response.Write("<script language='javascript'>window.close();</" + "script>");
                if (success_dt == true)
                {   //儲存成功
                    //return RedirectToAction("Med_NurseExecuteCheckList", new { @message = "儲存成功" });
                    Response.Write("<script>alert('儲存成功!');window.opener.location.href=window.opener.location.href;window.close();</script>");
                }
                else
                {   //儲存失敗
                    //return RedirectToAction("Med_NurseExecuteCheckList", new { @message = "儲存失敗" });
                    Response.Write("<script>alert('儲存失敗!');window.opener.location.href=window.opener.location.href;window.close();</script>");
                }

                return new EmptyResult();
            }
            else
            {
                Response.Write("<script>alert('請重新選擇病患');</script>");
                return new EmptyResult();
            }
        }
        #endregion

        #region Med_NurseExecute_BAK 執行一般給藥_備份
        public ActionResult Med_NurseExecute_BAK(FormCollection form, List<DRUG_EXECUTE> data, string ExecFlag)
        {
            if (Session["PatInfo"] == null)
            { return Redirect("../VitalSign/VitalSignSingle"); }

            if (Check_Session(ExecFlag))
                return Redirect("../VitalSign/VitalSignSingle");
            else if (ExecFlag == "weberror")
                return View();

            //宣告病患_取得住院序號
            PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
            string feeno = ptInfo.FeeNo;
            string start = "";           //查詢_開始日期時間
            string end = "";             //查詢_結束日期時間
            string udodrgcode = "";      //藥包條碼
            ViewBag.feeno = feeno;
            ViewBag.chrno = ptInfo.ChartNo;
            #region 更新資料
            if (ExecFlag == "save")
            {
                bool success_dt = true;//預設存資料庫成功
                success_dt = updDrugExecute(data, feeno, ptInfo.CostCenterNo);
                if (success_dt == true)
                {   //儲存成功
                    return RedirectToAction("Med_NurseExecute", new { @message = "儲存成功" });
                }
                else
                {   //儲存失敗
                    return RedirectToAction("Med_NurseExecute", new { @message = "儲存失敗" });
                    //return Redirect("../CommonMedication/Med_NurseExecute?ExecFlag=no");
                }
            }
            #endregion

            #region 取得STAT,REG,PRN 用藥
            #region 前置處理
            if (ExecFlag == "" || ExecFlag == null || ExecFlag == "yes" || ExecFlag == "no")
            {
                ExecFlag = "first";
                start = DateTime.Now.AddHours(-8).ToString("yyyy/MM/dd HH:mm:ss");
                end = DateTime.Now.AddHours(+1).ToString("yyyy/MM/dd HH:mm:ss");
            }
            else
            {
                start = Convert.ToDateTime(form["start_date"] + " " + form["start_time"]).ToString("yyyy/MM/dd HH:mm:ss");    //查詢_開始日期時間
                end = Convert.ToDateTime(form["end_date"] + " " + form["end_time"]).ToString("yyyy/MM/dd HH:mm:ss");          //查詢_結束日期時間
                udodrgcode = form["udodrgcode"];     //藥包條碼
                ViewBag.start_date = Convert.ToDateTime(form["start_date"]).ToString("yyyy/MM/dd");
                ViewBag.start_time = Convert.ToDateTime(form["start_time"]).ToString("HH:mm");
                ViewBag.end_date = Convert.ToDateTime(form["end_date"]).ToString("yyyy/MM/dd");
                ViewBag.end_time = Convert.ToDateTime(form["end_time"]).ToString("HH:mm");
            }
            switch (ExecFlag)
            {
                case "quiry":
                    break;
                case "quiryAll":
                    break;
                case "yes":
                    ExecFlag = "quiry";
                    break;
                case "no":
                    ExecFlag = "quiry";
                    break;
                default:
                    break;
            }
            #endregion

            //PROCESS UD FORM WEB SERVICE
            var str1 = WebS_UdOrder(feeno, start, end, ExecFlag, "");
            if (str1 == "weberror")
                return Redirect("../CommonMedication/Med_NurseExecute?ExecFlag=weberror");

            #region 檢查注射部位是否有資料
            if (cm.GetSpecialDrug_Set(feeno) == false)
            {
                List<DBItem> insertList = new List<DBItem>();
                insertList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                insertList.Add(new DBItem("INDATE", DateTime.Now.ToString("yyyy/MM/dd hh:mm"), DBItem.DBDataType.String));
                insertList.Add(new DBItem("REVIEW", "EFGABC", DBItem.DBDataType.String));
                insertList.Add(new DBItem("INSDT", DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss"), DBItem.DBDataType.String));
                insertList.Add(new DBItem("INSOP", "SYSTEM", DBItem.DBDataType.String));
                insertList.Add(new DBItem("INSOPNAME", "SYSTEM", DBItem.DBDataType.String));
                this.link.DBExecInsert("SPECIALDRUG_SET", insertList);
            }
            string ls_position = cm.Get_Position(feeno);
            #endregion

            #region 取得STAT 用藥
            if (dt_stat.Rows.Count > 0)
            {
                dt_stat.Columns.Add("use_time");//使用時間
                dt_stat.Columns.Add("use_seq");//使用順序
                dt_stat.Columns.Add("use_date");
                foreach (DataRow dr in dt_stat.Rows)
                {
                    DataTable dtt = cm.Get_DrugListTime(dr["UD_SEQ"].ToString(), "", start, end, ExecFlag);
                    if (dtt != null && dtt.Rows.Count > 0)
                    {
                        dr["use_seq"] = dtt.Rows[0]["use_seq"].ToString().TrimEnd(',');
                        dr["use_time"] = dtt.Rows[0]["use_time"].ToString().TrimEnd(',');
                        dr["use_date"] = dtt.Rows[0]["use_date"].ToString().TrimEnd(',');
                    }
                    else
                    {
                        dr["use_time"] = "now";
                    }
                    // 20140422 新增胰島素判斷條件，藥品代碼第一碼需為 I
                    //if (dr["UD_PATH"].ToString().Trim() == "SC" || dr["DRUG_TYPE"].ToString().Trim() == "E")
                    if (dr["UD_PATH"].ToString().Trim() == "SC" || (dr["DRUG_TYPE"].ToString().Trim() == "E" && dr["MED_CODE"].ToString().Substring(0, 1) == "I" && dr["UD_PATH"].ToString().Trim() == "SC"))
                    {
                        //dr["POSITION"] = cm.Get_Position(feeno);
                        dr["POSITION"] = ls_position;
                    }
                }
                dt_stat.DefaultView.RowFilter = "use_time <> 'now'";
                if (dt_stat.DefaultView.Count > 0)
                    ViewBag.dt_stat = dt_stat.DefaultView.ToTable();
            }
            #endregion
            #region 取得REG 用藥
            if (dt_reg.Rows.Count > 0)
            {
                dt_reg.Columns.Add("use_time");//使用時間
                dt_reg.Columns.Add("use_seq");//使用順序
                dt_reg.Columns.Add("use_date");
                dt_reg.Columns.Add("reason");
                dt_reg.Columns.Add("reasontype");
                foreach (DataRow dr in dt_reg.Rows)
                {
                    DataTable dtt = cm.Get_DrugListTime(dr["UD_SEQ"].ToString(), "", start, end, ExecFlag);
                    if (dtt != null && dtt.Rows.Count > 0)
                    {
                        dr["use_seq"] = dtt.Rows[0]["use_seq"].ToString().TrimEnd(',');
                        dr["use_time"] = dtt.Rows[0]["use_time"].ToString().TrimEnd(',');
                        dr["use_date"] = dtt.Rows[0]["use_date"].ToString().TrimEnd(',');
                        dr["reason"] = dtt.Rows[0]["reason1"].ToString();
                        dr["reasontype"] = dtt.Rows[0]["reasontype1"].ToString();
                    }
                    else
                    {
                        dr["use_time"] = "now";
                    }
                    // 20140422 新增胰島素判斷條件，藥品代碼第一碼需為 I
                    //if (dr["UD_PATH"].ToString().Trim() == "SC" || dr["DRUG_TYPE"].ToString().Trim() == "E")
                    if (dr["UD_PATH"].ToString().Trim() == "SC" || (dr["DRUG_TYPE"].ToString().Trim() == "E" && dr["MED_CODE"].ToString().Substring(0, 1) == "I" && dr["UD_PATH"].ToString().Trim() == "SC"))
                    {
                        //dr["POSITION"] = cm.Get_Position(feeno);
                        dr["POSITION"] = ls_position;
                    }
                }
                dt_reg.DefaultView.RowFilter = "use_time <> 'now'";
                if (dt_reg.DefaultView.Count > 0)
                    ViewBag.dt_reg = dt_reg.DefaultView.ToTable();
            }
            #endregion
            #region 取得PRN 用藥
            if (dt_prn.Rows.Count > 0)
            {
                dt_prn.Columns.Add("use_seq");//PKKEY
                dt_prn.Columns.Add("use_date"); //exec_date
                dt_prn.Columns.Add("use_time"); //exec_date
                dt_prn.Columns.Add("exec_date"); //exec_date
                dt_prn.Columns.Add("exec_count"); //exec_count
                dt_prn.Columns.Add("max_count"); //max_count
                foreach (DataRow dr in dt_prn.Rows)
                {
                    DataTable dtt = cm.Get_DrugListTime_PRN(dr["UD_SEQ"].ToString(), "P");
                    if (dtt != null && dtt.Rows.Count > 0)
                    {
                        dr["use_seq"] = dtt.Rows[0]["ud_seq"].ToString() + dtt.Rows[0]["MAXSEQ"].ToString();
                        dr["exec_date"] = Convert.ToDateTime(dtt.Rows[0]["exec_date"].ToString()).ToString("yyyy/MM/dd HH:mm");
                        dr["exec_count"] = dtt.Rows[0]["exec_count"].ToString();
                    }
                    else
                    {
                        dr["use_seq"] = dr["UD_SEQ"].ToString() + "001";
                        dr["exec_date"] = "";
                        dr["exec_count"] = "0";
                    }
                    dr["use_date"] = DateTime.Now.ToString("yyyy/MM/dd");
                    dr["use_time"] = DateTime.Now.ToString("HH:mm");
                    if (dr["ud_cir"].ToString().Trim() == "ASORDER")
                    {
                        dr["max_count"] = "0";
                    }
                    else
                    {
                        dr["max_count"] = dr["DAY_CNT"].ToString().Trim();
                    }
                    // 20140422 新增胰島素判斷條件，藥品代碼第一碼需為 I
                    //if (dr["UD_PATH"].ToString().Trim() == "SC" || dr["DRUG_TYPE"].ToString().Trim() == "E")
                    if (dr["UD_PATH"].ToString().Trim() == "SC" || (dr["DRUG_TYPE"].ToString().Trim() == "E" && dr["MED_CODE"].ToString().Substring(0, 1) == "I" && dr["UD_PATH"].ToString().Trim() == "SC"))
                    {
                        //dr["POSITION"] = cm.Get_Position(feeno);
                        dr["POSITION"] = ls_position;
                    }
                }
                if (dt_prn.Rows.Count > 0)
                    ViewBag.dt_prn = dt_prn;
            }
            #endregion
            #region 取得點滴 用藥
            if (dt_iv.Rows.Count > 0)
            {
                dt_iv.Columns.Add("use_seq");//PKKEY
                dt_iv.Columns.Add("use_date"); //exec_date
                dt_iv.Columns.Add("use_time"); //exec_date
                dt_iv.Columns.Add("exec_date"); //exec_date
                dt_iv.Columns.Add("exec_count"); //exec_count
                dt_iv.Columns.Add("max_count"); //max_count
                foreach (DataRow dr in dt_iv.Rows)
                {
                    DataTable dtt = cm.Get_DrugListTime_PRN(dr["UD_SEQ"].ToString(), "V");
                    if (dtt != null && dtt.Rows.Count > 0)
                    {
                        dr["use_seq"] = dtt.Rows[0]["ud_seq"].ToString() + dtt.Rows[0]["MAXSEQ"].ToString();
                        dr["exec_date"] = Convert.ToDateTime(dtt.Rows[0]["DRUG_DATE"].ToString()).ToString("yyyy/MM/dd HH:mm");
                        dr["exec_count"] = dtt.Rows[0]["exec_count"].ToString();
                    }
                    else
                    {
                        dr["use_seq"] = dr["UD_SEQ"].ToString() + "001";
                        dr["exec_date"] = "";
                        dr["exec_count"] = "0";
                    }
                    dr["use_date"] = DateTime.Now.ToString("yyyy/MM/dd");
                    dr["use_time"] = DateTime.Now.ToString("HH:mm");
                    dr["max_count"] = Convert.ToInt16(dr["DAY_CNT"].ToString()) * (int)Math.Ceiling(decimal.Parse(dr["UD_DOSE"].ToString()));
                }

                if (dt_iv.Rows.Count > 0)
                    ViewBag.dt_iv = dt_iv;

            }
            #endregion
            #endregion
            List<SelectListItem> position_list = new List<SelectListItem>();
            position_list.Add(new SelectListItem { Text = "請選擇", Value = "0" });
            position_list.Add(new SelectListItem { Text = "A (右上臂)", Value = "A" });
            position_list.Add(new SelectListItem { Text = "E (左上臂)", Value = "E" });
            position_list.Add(new SelectListItem { Text = "B (右腹部)", Value = "B" });
            position_list.Add(new SelectListItem { Text = "F (左腹部)", Value = "F" });
            position_list.Add(new SelectListItem { Text = "C (右大腿)", Value = "C" });
            position_list.Add(new SelectListItem { Text = "G (左大腿)", Value = "G" });
            //position_list.Add(new SelectListItem { Text = "H (左臀)", Value = "H" });
            //position_list.Add(new SelectListItem { Text = "D (右臀)", Value = "D" });
            ViewBag.position_list = position_list;
            return View();
        }
        #endregion

        #region Med_QueryExecLog 查詢給藥紀錄
        public ActionResult Med_QueryExecLog(FormCollection form, string ExecFlag, string TypeFlag, string feeno, string IsHisView)
        {//判斷有無病人session
            if (Session["PatInfo"] != null)
            {
                if (Check_Session(ExecFlag))
                    return Redirect("../VitalSign/VitalSignSingle");
                else if (ExecFlag == "weberror")
                    return View();

                //宣告病患_取得住院序號
                PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
                if (string.IsNullOrEmpty(feeno))
                    feeno = ptInfo.FeeNo;
                string start = "", end = "";
                ////if (ExecFlag == "" || ExecFlag == null)
                ////{
                ////    ExecFlag = "quiryAll";
                ////}

                if (ExecFlag == "quiryInterval")
                {
                    start = Convert.ToDateTime(form["start_date"] + " " + form["start_time"]).ToString("yyyy/MM/dd HH:mm:ss");    //查詢_開始日期時間
                    end = Convert.ToDateTime(form["end_date"] + " " + form["end_time"]).ToString("yyyy/MM/dd HH:mm:ss");          //查詢_結束日期時間
                    ViewBag.start_date = Convert.ToDateTime(form["start_date"]).ToString("yyyy/MM/dd");
                    ViewBag.start_time = Convert.ToDateTime(form["start_time"]).ToString("HH:mm");
                    ViewBag.end_date = Convert.ToDateTime(form["end_date"]).ToString("yyyy/MM/dd");
                    ViewBag.end_time = Convert.ToDateTime(form["end_time"]).ToString("HH:mm");
                }
                if (ExecFlag == "new")
                {
                    start = DateTime.Now.ToString("yyyy/MM/dd 00:00:00");    //查詢_開始日期時間
                    end = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");        //查詢_結束日期時間
                    ViewBag.start_date = DateTime.Now.ToString("yyyy/MM/dd");
                    ViewBag.end_date = DateTime.Now.ToString("yyyy/MM/dd");
                    ViewBag.start_time = "00:00";
                    ViewBag.end_time = DateTime.Now.ToString("HH:mm");
                    ExecFlag = "quiryInterval";

                }
                #region 取得用藥明細
                //PROCESS UD FORM WEB SERVICE
                var str1 = WebS_UdOrder(feeno, start, end, ExecFlag, "");
                if (str1 == "weberror")
                    return Redirect("../CommonMedication/Med_QueryExecLog?ExecFlag=weberror");
                //DataTable dt_all = new DataTable();
                //dt_all = cm.get_QueryExecLog(feeno, start, end, TypeFlag);

                if (dt_all.Rows.Count > 0)
                {
                    dt_all.Columns.Add("use_t");//使用時間
                    dt_all.Columns.Add("use_s");//使用順序
                    dt_all.Columns.Add("use_d");
                    dt_all.Columns.Add("use_execname");
                    dt_all.Columns.Add("use_execdate");
                    dt_all.Columns.Add("use_exectime");
                    dt_all.Columns.Add("use_reason");
                    //=============
                    dt_all.Columns.Add("check_execname"); //執行覆核給藥的人
                    dt_all.Columns.Add("check_execdate");   //執行覆核給藥的日期
                    dt_all.Columns.Add("check_exectime");   //執行覆核給藥的時間
                                                            //====
                    dt_all.Columns.Add("insulin");   //胰島素注射部位 
                    foreach (DataRow dr in dt_all.Rows)
                    {
                        DataTable dtt = cm.get_QueryExecLogTime(feeno, dr["UD_SEQ"].ToString(), start, end, TypeFlag, ExecFlag);
                        if (dtt != null && dtt.Rows.Count > 0)
                        {
                            dr["use_s"] = dtt.Rows[0]["use_s"].ToString().TrimEnd(',');
                            dr["use_t"] = dtt.Rows[0]["use_t"].ToString().TrimEnd(',');
                            dr["use_d"] = dtt.Rows[0]["use_d"].ToString().TrimEnd(',');
                            dr["use_execname"] = dtt.Rows[0]["use_execname"].ToString().TrimEnd(',');
                            dr["use_execdate"] = dtt.Rows[0]["use_execdate"].ToString().TrimEnd(',');
                            dr["use_exectime"] = dtt.Rows[0]["use_exectime"].ToString().TrimEnd(',');
                            dr["use_reason"] = dtt.Rows[0]["use_reason"].ToString().TrimEnd(',');
                            //===========
                            dr["check_execname"] = dtt.Rows[0]["check_execname"].ToString().TrimEnd(',');
                            dr["check_execdate"] = dtt.Rows[0]["check_execdate"].ToString().TrimEnd(',');
                            dr["check_exectime"] = dtt.Rows[0]["check_exectime"].ToString().TrimEnd(',');
                            //=========
                            dr["insulin"] = dtt.Rows[0]["insulin"].ToString().TrimEnd(',');
                            ////=========REASONTYPE
                            //if (string.IsNullOrEmpty(dtt.Rows[0]["USE_DOSE"].ToString()))
                            //{
                            //    dr["use_reason"] = "未給予原因:" + dtt.Rows[0]["REASONTYPE"].ToString().Trim().Replace("其他", "") + dtt.Rows[0]["NON_DRUG_OTHER"].ToString().TrimEnd(',');
                            //}
                            //else
                            //{
                            //    dr["use_reason"] = dtt.Rows[0]["use_reason"].ToString().TrimEnd(',');
                            //}

                        }
                    }
                    ViewBag.dt_all = dt_all;
                }
                #endregion
                ViewBag.IsHisView = IsHisView;
                ViewBag.TypeFlag = TypeFlag;
                ViewBag.feeno = feeno;
                return View();
            }
            else
            {
                Response.Write("<script>alert('請重新選擇病患');</script>");
                return new EmptyResult();
            }
        }

        public ActionResult Med_QueryExecLog_View(FormCollection form, string ExecFlag, string TypeFlag, string feeno, string IsHisView)
        {//判斷有無病人session
            if (Session["PatInfo"] != null)
            {
                if (Check_Session(ExecFlag))
                    return Redirect("../VitalSign/VitalSignSingle");
                else if (ExecFlag == "weberror")
                    return View();

                //宣告病患_取得住院序號
                PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
                if (string.IsNullOrEmpty(feeno))
                    feeno = ptInfo.FeeNo;
                string start = "", end = "";
                ////if (ExecFlag == "" || ExecFlag == null)
                ////{
                ////    ExecFlag = "quiryAll";
                ////}

                if (ExecFlag == "quiryInterval")
                {
                    start = Convert.ToDateTime(form["start_date"] + " " + form["start_time"]).ToString("yyyy/MM/dd HH:mm:ss");    //查詢_開始日期時間
                    end = Convert.ToDateTime(form["end_date"] + " " + form["end_time"]).ToString("yyyy/MM/dd HH:mm:ss");          //查詢_結束日期時間
                    ViewBag.start_date = Convert.ToDateTime(form["start_date"]).ToString("yyyy/MM/dd");
                    ViewBag.start_time = Convert.ToDateTime(form["start_time"]).ToString("HH:mm");
                    ViewBag.end_date = Convert.ToDateTime(form["end_date"]).ToString("yyyy/MM/dd");
                    ViewBag.end_time = Convert.ToDateTime(form["end_time"]).ToString("HH:mm");
                }
                if (ExecFlag == "new")
                {
                    start = DateTime.Now.ToString("yyyy/MM/dd 00:00:00");    //查詢_開始日期時間
                    end = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");        //查詢_結束日期時間
                    ViewBag.start_date = DateTime.Now.ToString("yyyy/MM/dd");
                    ViewBag.end_date = DateTime.Now.ToString("yyyy/MM/dd");
                    ViewBag.start_time = "00:00";
                    ViewBag.end_time = DateTime.Now.ToString("HH:mm");
                    ExecFlag = "quiryInterval";

                }
                #region 取得用藥明細
                //PROCESS UD FORM WEB SERVICE
                var str1 = WebS_UdOrder(feeno, start, end, ExecFlag, "");
                if (str1 == "weberror")
                    return Redirect("../CommonMedication/Med_QueryExecLog_View?ExecFlag=weberror");
                //DataTable dt_all = new DataTable();
                //dt_all = cm.get_QueryExecLog(feeno, start, end, TypeFlag);

                if (dt_all.Rows.Count > 0)
                {
                    dt_all.Columns.Add("use_t");//使用時間
                    dt_all.Columns.Add("use_s");//使用順序
                    dt_all.Columns.Add("use_d");
                    dt_all.Columns.Add("use_execname");
                    dt_all.Columns.Add("use_execdate");
                    dt_all.Columns.Add("use_exectime");
                    dt_all.Columns.Add("use_reason");
                    //=============
                    dt_all.Columns.Add("check_execname"); //執行覆核給藥的人
                    dt_all.Columns.Add("check_execdate");   //執行覆核給藥的日期
                    dt_all.Columns.Add("check_exectime");   //執行覆核給藥的時間
                                                            //====
                    dt_all.Columns.Add("insulin");   //胰島素注射部位 
                    foreach (DataRow dr in dt_all.Rows)
                    {
                        DataTable dtt = cm.get_QueryExecLogTime(feeno, dr["UD_SEQ"].ToString(), start, end, TypeFlag, ExecFlag);
                        if (dtt != null && dtt.Rows.Count > 0)
                        {
                            dr["use_s"] = dtt.Rows[0]["use_s"].ToString().TrimEnd(',');
                            dr["use_t"] = dtt.Rows[0]["use_t"].ToString().TrimEnd(',');
                            dr["use_d"] = dtt.Rows[0]["use_d"].ToString().TrimEnd(',');
                            dr["use_execname"] = dtt.Rows[0]["use_execname"].ToString().TrimEnd(',');
                            dr["use_execdate"] = dtt.Rows[0]["use_execdate"].ToString().TrimEnd(',');
                            dr["use_exectime"] = dtt.Rows[0]["use_exectime"].ToString().TrimEnd(',');
                            dr["use_reason"] = dtt.Rows[0]["use_reason"].ToString().TrimEnd(',');
                            //===========
                            dr["check_execname"] = dtt.Rows[0]["check_execname"].ToString().TrimEnd(',');
                            dr["check_execdate"] = dtt.Rows[0]["check_execdate"].ToString().TrimEnd(',');
                            dr["check_exectime"] = dtt.Rows[0]["check_exectime"].ToString().TrimEnd(',');
                            //=========
                            dr["insulin"] = dtt.Rows[0]["insulin"].ToString().TrimEnd(',');
                            ////=========REASONTYPE
                            //if (string.IsNullOrEmpty(dtt.Rows[0]["USE_DOSE"].ToString()))
                            //{
                            //    dr["use_reason"] = "未給予原因:" + dtt.Rows[0]["REASONTYPE"].ToString().Trim().Replace("其他", "") + dtt.Rows[0]["NON_DRUG_OTHER"].ToString().TrimEnd(',');
                            //}
                            //else
                            //{
                            //    dr["use_reason"] = dtt.Rows[0]["use_reason"].ToString().TrimEnd(',');
                            //}

                        }
                    }
                    ViewBag.dt_all = dt_all;
                }
                #endregion
                ViewBag.IsHisView = IsHisView;
                ViewBag.TypeFlag = TypeFlag;
                ViewBag.feeno = feeno;
                return View();
            }
            else
            {
                Response.Write("<script>alert('請重新選擇病患');</script>");
                return new EmptyResult();
            }
        }

        [HttpGet]
        public ActionResult Med_SearchList()
        {//判斷有無病人session
            if (Session["PatInfo"] != null)
            {
                ViewBag.FeeNo = base.ptinfo.FeeNo;
                ViewBag.RootDocument = GetSourceUrl();
                return View();
            }
            else
            {
                Response.Write("<script>alert('請重新選擇病患');</script>");
                return new EmptyResult();
            }
        }

        public ActionResult Med_SearchList_PDF(string Date, string FeeNo)
        {
            PatientInfo pinfo = new PatientInfo();
            byte[] ByteCode = webService.GetPatientInfo(FeeNo);
            //病人資訊
            if (ByteCode != null)
                pinfo = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(ByteCode));
            ViewData["ptinfo"] = pinfo;
            ViewBag.Date = Date;

            return View();
        }

        [HttpPost]
        public string Med_SearchList(string Date, string FeeNo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(FeeNo))
                {
                    if (Session["PatInfo"] != null)
                    {
                        FeeNo = base.ptinfo.FeeNo;
                    }
                }

                if (FeeNo != null)
                {
                    List<UdOrder> UdOrderList = new List<UdOrder>();
                    UdOrder Ud_Temp = null;
                    byte[] Temp_Byte = webService.GetUdOrder(FeeNo, "A");
                    if (Temp_Byte != null)
                        UdOrderList = JsonConvert.DeserializeObject<List<UdOrder>>(CompressTool.DecompressString(Temp_Byte));

                    Date = (string.IsNullOrWhiteSpace(Date)) ? DateTime.Now.ToString("yyyy/MM/dd 00:00:00") : Convert.ToDateTime(Date).ToString("yyyy/MM/dd 00:00:00");
                    string endDate = Convert.ToDateTime(Date).ToString("yyyy/MM/dd 23:59:59");
                    List<Dictionary<string, string>> Med_List = new List<Dictionary<string, string>>(), Dt = new List<Dictionary<string, string>>();
                    Dictionary<string, string> Temp = null;
                    string sql = string.Format("SELECT UD_SEQ, EXEC_DATE, EXEC_NAME, REASONTYPE, REASON,CHECKER,EARLY_REASON,"
                    + "DRUG_OTHER,NON_DRUG_OTHER,INSULIN_SITE,REMARK "
                    + "FROM H_DRUG_EXECUTE "
                    + "WHERE EXEC_DATE IS NOT NULL AND FEE_NO = '{0}' AND EXEC_DATE BETWEEN TO_DATE('{1}','yyyy/MM/dd hh24:mi:ss') "
                    + "AND TO_DATE('{2}','yyyy/MM/dd hh24:mi:ss')", FeeNo, Date, endDate);
                    //IDataReader reader = null;
                    DataTable Dtt = link.DBExecSQL(sql);
                    if (Dtt.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dtt.Rows.Count; i++)
                        {
                            Temp = new Dictionary<string, string>();
                            Temp["UD_SEQ"] = Dtt.Rows[i]["UD_SEQ"].ToString();
                            Temp["EXEC_DATE"] = Convert.ToDateTime(Dtt.Rows[i]["EXEC_DATE"].ToString()).ToString("yyyy/MM/dd HH:mm");
                            Temp["EXEC_NAME"] = Dtt.Rows[i]["EXEC_NAME"].ToString();
                            Temp["REASONTYPE"] = Dtt.Rows[i]["REASONTYPE"].ToString();
                            Temp["REASON"] = Dtt.Rows[i]["REASON"].ToString();

                            Temp["CHECKER"] = Dtt.Rows[i]["CHECKER"].ToString();
                            Temp["EARLY_REASON"] = Dtt.Rows[i]["EARLY_REASON"].ToString();
                            Temp["NON_DRUG_OTHER"] = Dtt.Rows[i]["NON_DRUG_OTHER"].ToString();
                            Temp["DRUG_OTHER"] = Dtt.Rows[i]["DRUG_OTHER"].ToString();
                            Temp["INSULIN_SITE"] = Dtt.Rows[i]["INSULIN_SITE"].ToString();
                            Temp["REMARK"] = Dtt.Rows[i]["REMARK"].ToString();
                            Med_List.Add(Temp);
                        }
                    }

                    if (Med_List.Count > 0)
                    {
                        List<string> SeqList = Med_List.Select(x => x["UD_SEQ"].ToString()).Distinct().ToList();
                        List<Dictionary<string, string>> Med_List_Temp = null;
                        foreach (string Seq in SeqList)
                        {
                            Temp = new Dictionary<string, string>();
                            string temp = "";
                            Ud_Temp = UdOrderList.Find(x => x.UD_SEQ == Seq);
                            Temp["MED_DESC"] = (Ud_Temp != null) ? Ud_Temp.MED_DESC.Trim() : "";
                            Temp["UD_DOSE"] = (Ud_Temp != null) ? Ud_Temp.UD_DOSE.Trim() + " " + Ud_Temp.UD_UNIT.Trim() : "";
                            Temp["UD_CIR"] = (Ud_Temp != null) ? Ud_Temp.UD_CIR.Trim() : "";
                            Temp["UD_PATH"] = (Ud_Temp != null) ? Ud_Temp.UD_PATH.Trim() : "";
                            Med_List_Temp = Med_List.FindAll(x => x["UD_SEQ"].ToString() == Seq);
                            foreach (var r in Med_List_Temp)
                            {
                                temp = string.Format("實際給藥時間：{0}，給藥人員姓名：{1}，",
                                        r["EXEC_DATE"], r["EXEC_NAME"]);

                                if (!string.IsNullOrWhiteSpace(r["CHECKER"].ToString()))
                                    temp += string.Format("覆核者姓名：{0}", r["CHECKER"] + "，");

                                if (!string.IsNullOrWhiteSpace(r["INSULIN_SITE"].ToString()) && r["INSULIN_SITE"].ToString().Trim() != "選擇部位")
                                    temp += string.Format("注射部位：{0}", r["INSULIN_SITE"] + "，");

                                if (!string.IsNullOrWhiteSpace(r["REASONTYPE"].ToString()))
                                    temp += string.Format("未給予：{0}" + "，", r["REASONTYPE"].ToString().Trim() != "其他" ? r["REASONTYPE"] : r["NON_DRUG_OTHER"]);
                                else if (!string.IsNullOrWhiteSpace(r["REASON"].ToString()))
                                    temp += string.Format("延遲給藥：{0}" + "，", r["REASON"].ToString().Trim() != "其他" ? r["REASON"] : r["DRUG_OTHER"]);
                                else if (!string.IsNullOrWhiteSpace(r["EARLY_REASON"].ToString()))
                                    temp += string.Format("提早給藥：{0}" + "，", r["EARLY_REASON"].ToString().Trim() != "其他" ? r["EARLY_REASON"] : r["DRUG_OTHER"]);

                                if (!string.IsNullOrWhiteSpace(r["REMARK"].ToString()))
                                    temp += string.Format("備註：{0}", r["REMARK"] + "，");

                                Temp["REMARK"] = temp.TrimEnd('，');

                                Dt.Add(Temp);
                                Temp = new Dictionary<string, string>();
                            }
                        }
                    }

                    return JsonConvert.SerializeObject(Dt);
                }
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return "";
            }
            return "";
        }
        #endregion

        #region Med_QueryExecLogByUD 查詢給藥紀錄_藥師,書記用
        public ActionResult Med_QueryExecLogByUD(FormCollection form, string feeno, string ExecFlag, string TypeFlag)
        {//判斷有無病人session
            if (!string.IsNullOrEmpty(feeno))
            {
                string start = "", end = "", medcode = "", str2 = "";
                if (ExecFlag == "" || ExecFlag == null)
                {
                    ExecFlag = "quiryIntervalByUD";
                }

                if (form["start_date"] == null)
                {
                    start = DateTime.Now.AddDays(-1).ToString("yyyy/MM/dd") + " 15:00:00";
                    end = DateTime.Now.ToString("yyyy/MM/dd") + " 15:00:00";
                    ViewBag.start_date = DateTime.Now.AddDays(-1).ToString("yyyy/MM/dd");
                    ViewBag.start_time = "15:00";
                    ViewBag.end_date = DateTime.Now.ToString("yyyy/MM/dd");
                    ViewBag.end_time = "15:00";
                }
                else
                {
                    start = Convert.ToDateTime(form["start_date"] + " " + form["start_time"]).ToString("yyyy/MM/dd HH:mm:ss");    //查詢_開始日期時間
                    end = Convert.ToDateTime(form["end_date"] + " " + form["end_time"]).ToString("yyyy/MM/dd HH:mm:ss");          //查詢_結束日期時間
                    ViewBag.start_date = Convert.ToDateTime(form["start_date"]).ToString("yyyy/MM/dd");
                    ViewBag.start_time = Convert.ToDateTime(form["start_time"]).ToString("HH:mm");
                    ViewBag.end_date = Convert.ToDateTime(form["end_date"]).ToString("yyyy/MM/dd");
                    ViewBag.end_time = Convert.ToDateTime(form["end_time"]).ToString("HH:mm");
                }
                if (ExecFlag == "quiryAllByUD")
                {
                    start = "";
                    end = "";
                }
                #region 取得用藥明細
                //PROCESS UD FORM WEB SERVICE
                var str1 = WebS_UdOrder(feeno, start, end, ExecFlag, medcode);
                if (dt_all.Rows.Count > 0)
                {
                    dt_all.Columns.Add("use_t");//使用時間
                    dt_all.Columns.Add("use_s");//使用順序
                    dt_all.Columns.Add("use_d");
                    dt_all.Columns.Add("use_execname");
                    dt_all.Columns.Add("use_execdate");
                    dt_all.Columns.Add("use_exectime");
                    dt_all.Columns.Add("use_reason");
                    if (medcode != "" && medcode != null)
                    {
                        dt_all.DefaultView.RowFilter = "med_code =" + medcode;
                    }
                    foreach (DataRow dr in dt_all.Rows)
                    {
                        DataTable dtt = cm.get_QueryExecLogTime(feeno, dr["UD_SEQ"].ToString(), start, end, TypeFlag, ExecFlag);
                        if (dtt != null && dtt.Rows.Count > 0)
                        {
                            dr["use_s"] = dtt.Rows[0]["use_s"].ToString().TrimEnd(',');
                            dr["use_t"] = dtt.Rows[0]["use_t"].ToString().TrimEnd(',');
                            dr["use_d"] = dtt.Rows[0]["use_d"].ToString().TrimEnd(',');
                            dr["use_execname"] = dtt.Rows[0]["use_execname"].ToString().TrimEnd(',');
                            dr["use_execdate"] = dtt.Rows[0]["use_execdate"].ToString().TrimEnd(',');
                            dr["use_exectime"] = dtt.Rows[0]["use_exectime"].ToString().TrimEnd(',');
                            dr["use_reason"] = dtt.Rows[0]["use_reason"].ToString().TrimEnd(',');
                            switch (dr["UD_STATUS"].ToString().Trim())
                            {
                                case "1":
                                    str2 = "待核";
                                    break;
                                case "2":
                                    str2 = "執行";
                                    break;
                                case "4":
                                    str2 = "出院帶藥";
                                    break;
                                case "6":
                                    str2 = "結束";
                                    break;
                            }
                            dr["UD_STATUS"] = str2;
                        }
                    }
                    dt_all.DefaultView.Sort = "UD_STATUS, MED_CODE, BEGIN_DATE DESC, BEGIN_TIME";
                    ViewBag.dt_all = dt_all.DefaultView.ToTable();
                }
                #endregion
                ViewBag.feeno = feeno;
                ViewBag.TypeFlag = TypeFlag;
                return View();
            }
            else
            {
                Response.Write("<script>alert('請重新選擇病患');</script>");
                return new EmptyResult();
            }
        }
        #endregion

        #region Med_QueryOrder 查詢病人用藥ORDER
        public ActionResult Med_QueryOrder()
        {
            //判斷有無病人session
            if (Session["PatInfo"] != null)
            {
                //宣告病患_取得住院序號
                PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
                string feeno = ptInfo.FeeNo;

                #region 取得用藥明細
                //PROCESS UD FORM WEB SERVICE
                var str1 = WebS_UdOrder(feeno, "", "", "QueryOrder", "");
                if (str1 == "weberror")
                    return RedirectToAction("Med_QueryOrder", new { @message = "Error" });
                //return Redirect("../CommonMedication/Med_QueryOrder?ExecFlag=weberror");

                if (dt_all.Rows.Count > 0)
                {
                    string str2 = "";
                    foreach (DataRow dr in dt_all.Rows)
                    {
                        switch (dr["UD_STATUS"].ToString().Trim())
                        {
                            case "1":
                                str2 = "待核";
                                break;
                            case "2":
                                str2 = "執行";
                                break;
                            case "4":
                                str2 = "出院帶藥";
                                break;
                            case "6":
                                str2 = "結束";
                                break;
                        }
                        dr["UD_STATUS"] = str2;
                        dr["BEGIN_DATE"] = Convert.ToInt32(dr["BEGIN_DATE"].ToString()).ToString("000/00/00");
                        dr["BEGIN_TIME"] = Convert.ToInt32(dr["BEGIN_TIME"].ToString()).ToString("00:00");
                        if (dr["DC_DATE"].ToString() != "")
                        {
                            dr["DC_DATE"] = Convert.ToInt32(dr["DC_DATE"].ToString()).ToString("000/00/00");
                            dr["DC_TIME"] = Convert.ToInt32(dr["DC_TIME"].ToString()).ToString("00:00");
                        }
                        if (dr["DC_DAY"].ToString() != "")
                        {
                            //將 DC_DAY 西元轉民國
                            System.Globalization.CultureInfo ToTWDay = new System.Globalization.CultureInfo("zh-TW");
                            ToTWDay.DateTimeFormat.Calendar = new System.Globalization.TaiwanCalendar();
                            DateTime DCDAYTmp = Convert.ToDateTime(dr["DC_DAY"].ToString());

                            dr["DC_DATE"] = DCDAYTmp.ToString("yyyy/MM/dd", ToTWDay);
                            dr["DC_TIME"] = DCDAYTmp.ToString("HH:mm", ToTWDay);
                        }
                        dr["UD_DOSE"] = dr["UD_DOSE"].ToString().Trim();
                        //Math.Round(Convert.ToDecimal(dr["UD_DOSE"].ToString().Trim()), 1).ToString("0.#");
                    }
                    dt_all.DefaultView.Sort = "MED_DESC, MED_CODE, BEGIN_DATE DESC, BEGIN_TIME DESC";
                    ViewBag.dt_all = dt_all.DefaultView.ToTable();
                }
                #endregion
                return View();

            }
            else
            {
                Response.Write("<script>alert('請重新選擇病患');</script>");
                return new EmptyResult();
            }
        }
        #endregion
        #region Med_QueryOrderPain 查詢病人用藥ORDER
        public ActionResult Med_QueryOrderPain()
        {
            DataTable dt_pain = new DataTable();

            //判斷有無病人session
            if (Session["PatInfo"] != null)
            {
                //宣告病患_取得住院序號
                PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
                string feeno = ptInfo.FeeNo;

                #region 取得用藥明細
                //PROCESS UD FORM WEB SERVICE
                var str1 = WebS_UdOrder(feeno, "", "", "QueryOrder", "");
                if (str1 == "weberror")
                    return RedirectToAction("Med_QueryOrder", new { @message = "Error" });
                //return Redirect("../CommonMedication/Med_QueryOrder?ExecFlag=weberror");

                if (dt_all.Rows.Count > 0)
                {
                    string str2 = "";
                    foreach (DataRow dr in dt_all.Rows)
                    {
                        switch (dr["UD_STATUS"].ToString().Trim())
                        {
                            case "1":
                                str2 = "待核";
                                break;
                            case "2":
                                str2 = "執行";
                                break;
                            case "4":
                                str2 = "出院帶藥";
                                break;
                            case "6":
                                str2 = "結束";
                                break;
                        }
                        dr["UD_STATUS"] = str2;
                        dr["BEGIN_DATE"] = Convert.ToInt32(dr["BEGIN_DATE"].ToString()).ToString("000/00/00");
                        dr["BEGIN_TIME"] = Convert.ToInt32(dr["BEGIN_TIME"].ToString()).ToString("00:00");
                        if (dr["DC_DATE"].ToString() != "")
                        {
                            dr["DC_DATE"] = Convert.ToInt32(dr["DC_DATE"].ToString()).ToString("000/00/00");
                            dr["DC_TIME"] = Convert.ToInt32(dr["DC_TIME"].ToString()).ToString("00:00");
                        }
                        if (dr["DC_DAY"].ToString() != "")
                        {
                            //將 DC_DAY 西元轉民國
                            System.Globalization.CultureInfo ToTWDay = new System.Globalization.CultureInfo("zh-TW");
                            ToTWDay.DateTimeFormat.Calendar = new System.Globalization.TaiwanCalendar();
                            DateTime DCDAYTmp = Convert.ToDateTime(dr["DC_DAY"].ToString());

                            dr["DC_DATE"] = DCDAYTmp.ToString("yyyy/MM/dd", ToTWDay);
                            dr["DC_TIME"] = DCDAYTmp.ToString("HH:mm", ToTWDay);
                        }
                        dr["UD_DOSE"] = dr["UD_DOSE"].ToString().Trim();
                        //Math.Round(Convert.ToDecimal(dr["UD_DOSE"].ToString().Trim()), 1).ToString("0.#");

                        if (dr["IsAnalgesics"].ToString() == "ture")
                        {
                            dt_pain.Rows.Add(dt_all.Rows);
                        }
                    }
                    if (dt_pain.Rows.Count > 0)
                    {
                        dt_pain.DefaultView.Sort = "MED_DESC, MED_CODE, BEGIN_DATE DESC, BEGIN_TIME DESC";
                    }
                    ViewBag.dt_all = dt_pain.DefaultView.ToTable();
                }
                #endregion
                return View();

            }
            else
            {
                Response.Write("<script>alert('請重新選擇病患');</script>");
                return new EmptyResult();
            }
        }
        #endregion
        #region Med_Info 餐包
        public ActionResult Med_Info(string id, bool flag)
        {//判斷有無病人session
            if (Session["PatInfo"] != null)
            {
                string strsql = "", strUdSeq = "", strMedCode = "", doJsonArr = "";
                byte[] doByteCode;
                DataTable dt_med = new DataTable();
                PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
                ViewBag.id = id;
                strsql = "SELECT DRUG_CODE AS MED_CODE, RIGHT_TIME AS DRUGTIME,B.UD_SEQ AS UD_SEQ,B.INVALID_DATE,B.EXEC_DATE,B.DRUG_DATE FROM BCMA_BCPRINT A, H_DRUG_EXECUTE B";
                strsql += " WHERE TRIM(A.UNCRCPPK) = B.UD_SEQPK AND A.BARCODE = '" + id + "' AND A.PCASENO ='" + ptInfo.FeeNo + "'";
                dt_med = cm.ExecSql(strsql);
                dt_med.Columns.Add("DrugName");
                dt_med.Columns.Add("GenericDrugs");
                dt_med.Columns.Add("DrugEffects");
                dt_med.Columns.Add("DrugSideEffects");
                dt_med.Columns.Add("DrugPicPath");
                dt_med.Columns.Add("DrugCode");
                dt_med.Columns.Add("UD_CIR");
                dt_med.Columns.Add("UD_DOSE");
                dt_med.Columns.Add("UD_UNIT");
                dt_med.Columns.Add("UD_PATH");
                dt_med.Columns.Add("ORDERDATE");
                dt_med.Columns.Add("UD_SEQ_O");
                dt_med.Columns.Add("UD_CIR_O");
                dt_med.Columns.Add("UD_DOSE_O");
                dt_med.Columns.Add("UD_UNIT_O");
                dt_med.Columns.Add("UD_PATH_O");
                dt_med.Columns.Add("FLAG");
                foreach (DataRow a in dt_med.Rows)
                {
                    strMedCode = a["MED_CODE"].ToString().Trim();
                    strUdSeq = a["UD_SEQ"].ToString().Trim();

                    doByteCode = webService.GetMedicalInfo(strMedCode);
                    doJsonArr = CompressTool.DecompressString(doByteCode);
                    List<MedicalInfo> MedList = JsonConvert.DeserializeObject<List<MedicalInfo>>(doJsonArr);
                    a["DrugName"] = MedList[0].DrugName.Trim();
                    a["GenericDrugs"] = MedList[0].GenericDrugs.Trim();
                    a["DrugEffects"] = MedList[0].DrugEffects.Trim();
                    a["DrugSideEffects"] = MedList[0].DrugSideEffects.Trim();
                    a["DrugPicPath"] = MedList[0].DrugPicPath.Trim();
                    a["DrugCode"] = MedList[0].DrugCode.Trim();
                    //if (flag == true || (a["INVALID_DATE"].ToString() == "" && a["EXEC_DATE"] == null))
                    if (flag == true || (a["INVALID_DATE"].ToString() == "" && a["EXEC_DATE"] == null))
                    {
                        #region 正常顯示
                        doByteCode = webService.GetUdOrder(strUdSeq, "B");
                        if (doByteCode != null)
                        {
                            doJsonArr = CompressTool.DecompressString(doByteCode);
                            List<UdOrder> UdOrderList = JsonConvert.DeserializeObject<List<UdOrder>>(doJsonArr);
                            a["UD_CIR_O"] = UdOrderList[0].UD_CIR.Trim();
                            a["UD_DOSE_O"] = UdOrderList[0].UD_DOSE.Trim();
                            a["UD_UNIT_O"] = UdOrderList[0].UD_UNIT.Trim();
                            a["UD_PATH_O"] = UdOrderList[0].UD_PATH.Trim();
                            a["FLAG"] = "N";
                        }
                        #endregion
                    }
                    else
                    {
                        doByteCode = null;
                        #region DC or OrderRenew
                        //找有沒被DC重開
                        if (doByteCode == null)
                        {
                            //沒有找到時...
                            doByteCode = webService.GetUdOrder(strUdSeq, "B");
                            if (doByteCode != null)
                            {
                                doJsonArr = CompressTool.DecompressString(doByteCode);
                                List<UdOrder> UdOrderList = JsonConvert.DeserializeObject<List<UdOrder>>(doJsonArr);
                                a["UD_CIR_O"] = UdOrderList[0].UD_CIR.Trim();
                                a["UD_DOSE_O"] = UdOrderList[0].UD_DOSE.Trim();
                                a["UD_UNIT_O"] = UdOrderList[0].UD_UNIT.Trim();
                                a["UD_PATH_O"] = UdOrderList[0].UD_PATH.Trim();
                            }
                            if (a["INVALID_DATE"].ToString() == "")
                                a["FLAG"] = "N";
                            else if (a["INVALID_DATE"].ToString() != "")
                                a["FLAG"] = "DC";
                            else
                                ViewBag.result = false;
                        }
                        else
                        {
                            doJsonArr = CompressTool.DecompressString(doByteCode);
                            List<MedOrderRenew> UdList = JsonConvert.DeserializeObject<List<MedOrderRenew>>(doJsonArr);
                            a["UD_SEQ"] = UdList[0].UDSEQ.Trim();
                            a["UD_CIR"] = UdList[0].UD_CIR.Trim();
                            a["UD_DOSE"] = UdList[0].UD_DOSE.Trim();
                            a["UD_UNIT"] = UdList[0].UD_UNIT.Trim();
                            a["UD_PATH"] = UdList[0].UD_PATH.Trim();
                            a["ORDERDATE"] = UdList[0].ORDERDATE.Trim();
                            a["UD_SEQ_O"] = UdList[0].UDSEQ_O.Trim();
                            a["UD_CIR_O"] = UdList[0].UD_CIR_O.Trim();
                            a["UD_DOSE_O"] = UdList[0].UD_DOSE_O.Trim();
                            a["UD_UNIT_O"] = UdList[0].UD_UNIT_O.Trim();
                            a["UD_PATH_O"] = UdList[0].UD_PATH_O.Trim();
                            if (UdList[0].UDSEQ.Trim() == "")
                                a["FLAG"] = "N";
                            else
                                a["FLAG"] = "Y";
                        }
                        #endregion
                    }
                    ViewBag.drug_date = a["DRUG_DATE"];
                }
                ViewBag.dt_med = dt_med;
                return View();

            }
            else
            {
                Response.Write("<script>alert('請重新選擇病患');</script>");
                return new EmptyResult();
            }
        }
        #endregion
        public static bool Check_insulin_in_drugEX_exstOrNot;
        #region Med_Cancel 取消給藥
        public ActionResult Med_Cancel(FormCollection form, List<DRUG_EXECUTE> data, string ExecFlag)
        {
            if (Session["PatInfo"] == null)
            { return Redirect("../VitalSign/VitalSignSingle"); }

            if (Check_Session(ExecFlag))
                return Redirect("../VitalSign/VitalSignSingle");
            else if (ExecFlag == "weberror")
                return View();

            PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];

            string feeno = ptInfo.FeeNo;
            string start = "";           //查詢_開始日期時間
            string end = "";             //查詢_結束日期時間
            ViewBag.feeno = feeno;
            ViewBag.user_id = userinfo.EmployeesNo;

            #region 更新資料
            if (ExecFlag == "save")
            {
                bool success_dt = true;//預設存資料庫成功
                success_dt = updDrugCancel(data, feeno, ptInfo.CostCenterNo);
                if (success_dt == true)
                {   //儲存成功
                    return RedirectToAction("Med_Cancel", new { @message = "儲存成功" });
                }
                else
                {   //儲存失敗
                    return RedirectToAction("Med_Cancel", new { @message = "儲存失敗" });
                }
            }
            #endregion

            #region 取得用藥清單
            #region 前置處理
            if (ExecFlag == "" || ExecFlag == null || ExecFlag == "yes" || ExecFlag == "no")
            {
                ExecFlag = "C_first";
                start = DateTime.Now.AddHours(-8).ToString("yyyy/MM/dd HH:mm:ss");
                end = DateTime.Now.AddHours(+1).ToString("yyyy/MM/dd HH:mm:ss");
            }
            else
            {
                start = Convert.ToDateTime(form["start_date"] + " " + form["start_time"]).ToString("yyyy/MM/dd HH:mm:ss");    //查詢_開始日期時間
                end = Convert.ToDateTime(form["end_date"] + " " + form["end_time"]).ToString("yyyy/MM/dd HH:mm:ss");          //查詢_結束日期時間
                ViewBag.start_date = Convert.ToDateTime(form["start_date"]).ToString("yyyy/MM/dd");
                ViewBag.start_time = Convert.ToDateTime(form["start_time"]).ToString("HH:mm");
                ViewBag.end_date = Convert.ToDateTime(form["end_date"]).ToString("yyyy/MM/dd");
                ViewBag.end_time = Convert.ToDateTime(form["end_time"]).ToString("HH:mm");
            }
            switch (ExecFlag)
            {
                case "C_quiry":
                    break;
                case "C_quiryAll":
                    break;
                case "yes":
                    ExecFlag = "C_quiry";
                    break;
                case "no":
                    ExecFlag = "C_quiry";
                    break;
                default:
                    break;
            }
            #endregion

            //PROCESS UD FORM WEB SERVICE
            var str1 = WebS_UdOrder(feeno, start, end, ExecFlag, "");
            if (str1 == "weberror")
                return Redirect("../CommonMedication/Med_Cancel?ExecFlag=weberror");

            #region 取得給藥時間
            if (dt_all.Rows.Count > 0)
            {
                dt_all.Columns.Add("use_time");//使用時間
                dt_all.Columns.Add("use_seq");//使用順序
                dt_all.Columns.Add("use_date");
                dt_all.Columns.Add("exec_id");
                dt_all.Columns.Add("exec_name");
                dt_all.Columns.Add("exec_time");
                dt_all.Columns.Add("exec_date");
                dt_all.Columns.Add("drug_date");
                dt_all.Columns.Add("upd_type");
                dt_all.Columns.Add("insulin_site");
                foreach (DataRow dr in dt_all.Rows)
                {
                    DataTable dtt = cm.Get_DrugListTime_Cancel(dr["UD_SEQ"].ToString(), "", start, end, ExecFlag, feeno);
                    if (dtt != null && dtt.Rows.Count > 0)
                    {
                        dr["use_seq"] = dtt.Rows[0]["use_seq"].ToString().TrimEnd(',');
                        dr["use_time"] = dtt.Rows[0]["use_time"].ToString().TrimEnd(',');
                        dr["use_date"] = dtt.Rows[0]["use_date"].ToString().TrimEnd(',');
                        dr["exec_time"] = dtt.Rows[0]["exec_time"].ToString().TrimEnd(',');
                        dr["exec_id"] = dtt.Rows[0]["exec_id"].ToString().TrimEnd(',');
                        dr["exec_name"] = dtt.Rows[0]["exec_name"].ToString().TrimEnd(',');
                        dr["drug_date"] = dtt.Rows[0]["drug_date"].ToString().TrimEnd(',');
                        dr["exec_date"] = dtt.Rows[0]["exec_date"].ToString().TrimEnd(',');
                        dr["insulin_site"] = dtt.Rows[0]["insulin_site"].ToString().TrimEnd(',');
                        /*if (dtt.Rows[0]["use_seq"].ToString().TrimEnd(',').Trim().Length > 16)
                            dr["upd_type"] ="U";
                        else
                            dr["upd_type"] = "I";*/
                        if (dtt.Rows[0]["insulin_site"].ToString() != null || dtt.Rows[0]["insulin_site"].ToString() != "")
                        {
                            Check_insulin_in_drugEX_exstOrNot = true;
                        }
                    }
                    else
                    {
                        dr["use_time"] = "now";
                    }
                }
                dt_all.DefaultView.RowFilter = "use_time <> 'now'";
                if (dt_all.DefaultView.Count > 0)
                    ViewBag.dt_all = dt_all.DefaultView;
            }
            #endregion

            #endregion
            return View();
        }
        #endregion

        #region Login 覆核
        [HttpGet]
        public ActionResult Login(string name, string id)
        {
            ViewBag.id = id;
            ViewBag.checkname = "Z";
            return View();
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Login(FormCollection form)
        {
            byte[] uData = webService.UserLogin(form["txtid"], form["txtpwd"]);
            if (uData != null)
            {
                string jsonstr = CompressTool.DecompressString(uData);
                UserInfo user = JsonConvert.DeserializeObject<UserInfo>(jsonstr);
                ViewBag.checkname = user.EmployeesName.ToString().Trim();
            }
            else
            {
                ViewBag.checkname = null;
                //帳號或密碼錯誤
            }
            ViewBag.id = form["id"];
            return View();
        }
        #endregion

        #region Med_NurseExecute INSERT,UPDATA

        public ActionResult Med_PDF(string feeno, string med, string path, string cir, string dose)
        {
            PatientInfo pinfo = new PatientInfo();
            byte[] ByteCode = webService.GetPatientInfo(feeno);
            //病人資訊
            if (ByteCode != null)
                pinfo = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(ByteCode));
            ViewData["ptinfo"] = pinfo;
            ViewBag.meddesc = med;
            ViewBag.path = path;
            ViewBag.cir = cir;
            ViewBag.dose = dose;
            return View();
        }

        public bool updDrugExecute(List<DRUG_EXECUTE> data, string feeno, string cost_code)
        {
            if (data != null) //判斷儲存勾選資料是否有無
            {
                int effRow = 0;
                for (int i = 0; i < data.Count; i++)
                {
                    if (data[i].ORDSTATUS == "0" || data[i].ORDSTATUS == "2")//有打勾的
                    {
                        effRow = 0;
                        string where = "";
                        #region 更新DRUG_EXECUTE
                        // if (data[i].UD_TYPE != "P" && (data[i].UD_TYPE != "R" || data[i].DRUG_TYPE != "V"))
                        if (data[i].UPD_TYPE == "U")
                        {
                            PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
                            where += " UD_SEQPK = '" + data[i].UD_SEQPK.ToString() + "' AND UD_SEQ = '" + data[i].UD_SEQ.ToString() + "' AND FEE_NO = '" + ptInfo.FeeNo + "' ";
                            List<DBItem> updList = new List<DBItem>();
                            updList.Add(new DBItem("EXEC_ID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                            updList.Add(new DBItem("EXEC_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));

                            if (data[i].ORDSTATUS != "2") //延遲執行
                            {
                                if (data[i].UD_CIR.ToString().Trim() == "STAT")
                                    updList.Add(new DBItem("EXEC_DATE", Convert.ToDateTime(data[i].DRUG_DATE).ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                                else
                                    updList.Add(new DBItem("EXEC_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                            }
                            if (data[i].REASONTYPE != null)
                                updList.Add(new DBItem("REASONTYPE", data[i].REASONTYPE.ToString(), DBItem.DBDataType.String));
                            if (data[i].REASON != null)
                                updList.Add(new DBItem("REASON", data[i].REASON.ToString(), DBItem.DBDataType.String));
                            if (data[i].CHECKER_DATE != null)
                                updList.Add(new DBItem("CHECKER_DATE", data[i].CHECKER_DATE.ToString(), DBItem.DBDataType.DataTime));
                            if (data[i].CHECKER_ID != null)
                                updList.Add(new DBItem("CHECKER_ID", data[i].CHECKER_ID.ToString(), DBItem.DBDataType.String));
                            if (data[i].CHECKER != null)
                                updList.Add(new DBItem("CHECKER", data[i].CHECKER.ToString(), DBItem.DBDataType.String));
                            if (data[i].BADREACTION != null)
                                updList.Add(new DBItem("BADREACTION", data[i].BADREACTION.ToString(), DBItem.DBDataType.String));

                            effRow += this.link.DBExecUpdate("DRUG_EXECUTE", updList, where);
                            //將紀錄回寫至 EMR Temp Table
                            if (data[i].ORDSTATUS != "2")
                            {
                                ////string sqlstr = "begin P_NIS_EMRMS('" + feeno + "','067','護理給藥記錄單(處方)','" + data[i].UD_SEQPK.ToString() + "','" + DateTime.Now.ToString("yyyy/MM/dd HH:mm") + "','" + userinfo.EmployeesNo + "','I');end;";
                                ////link.DBExec(sqlstr);
                                ////#region JAG 簽章
                                ////// 20150608 EMR
                                ////string strPath = @"C:\\wkhtmltopdf\\wkhtmltopdf.exe";
                                ////string filename = @"C:\inetpub\NIS\Images\" + data[i].UD_SEQPK.ToString() + ".pdf";

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

                                ////string url = sOut + "/CommonMedication/Med_PDF?feeno=" + feeno + "&med=" + data[i].MED_DESC.Trim() + "&path=" + data[i].UD_PATH.Trim() + "&cir=" + data[i].UD_CIR.Trim() + "&dose=" + data[i].USE_DOSE.Trim();

                                ////Process p = new Process();
                                ////p.StartInfo.FileName = strPath;
                                ////p.StartInfo.Arguments = "\""+url+"\"" + " " + filename;
                                ////p.StartInfo.UseShellExecute = true;
                                ////p.Start();
                                ////p.WaitForExit();

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
                                ////int result = emr_sign(data[i].UD_SEQPK.ToString(), feeno, "067", userinfo.EmployeesNo, emp_name, emp_id, dep_no, chr_no, pat_name, in_date, chagre_type, filename);
                                ////#endregion
                            }
                        }
                        else
                        {
                            List<DBItem> insertList = new List<DBItem>();
                            try
                            {
                                insertList.Add(new DBItem("UD_SEQPK", data[i].UD_SEQPK.ToString(), DBItem.DBDataType.String));
                                insertList.Add(new DBItem("UD_SEQ", data[i].UD_SEQ.ToString(), DBItem.DBDataType.String));
                                insertList.Add(new DBItem("DRUG_DATE", Convert.ToDateTime(data[i].DRUG_DATE).ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                                insertList.Add(new DBItem("EXEC_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                                insertList.Add(new DBItem("EXEC_ID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                                insertList.Add(new DBItem("EXEC_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                                insertList.Add(new DBItem("CREAT_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                                insertList.Add(new DBItem("MED_CODE", data[i].MED_CODE.ToString(), DBItem.DBDataType.String));
                                insertList.Add(new DBItem("FEE_NO", feeno, DBItem.DBDataType.String));
                                //if (data[@i].DRUG_TYPE == "V")
                                //{ insertList.Add(new DBItem("USE_DOSE", "1", DBItem.DBDataType.String)); }
                                //else
                                insertList.Add(new DBItem("USE_DOSE", data[i].USE_DOSE.ToString(), DBItem.DBDataType.String));
                                if (data[i].REASONTYPE != null)
                                    insertList.Add(new DBItem("REASONTYPE", data[i].REASONTYPE.ToString(), DBItem.DBDataType.String));
                                if (data[i].REASON != null)
                                    insertList.Add(new DBItem("REASON", data[i].REASON.ToString(), DBItem.DBDataType.String));
                                if (data[i].CHECKER_DATE != null)
                                    insertList.Add(new DBItem("CHECKER_DATE", data[i].CHECKER_DATE.ToString(), DBItem.DBDataType.DataTime));
                                if (data[i].CHECKER_ID != null)
                                    insertList.Add(new DBItem("CHECKER_ID", data[i].CHECKER_ID.ToString(), DBItem.DBDataType.String));
                                if (data[i].CHECKER != null)
                                    insertList.Add(new DBItem("CHECKER", data[i].CHECKER.ToString(), DBItem.DBDataType.String));
                                if (data[i].BADREACTION != null)
                                    insertList.Add(new DBItem("BADREACTION", data[i].BADREACTION.ToString(), DBItem.DBDataType.String));
                                effRow += this.link.DBExecInsert("DRUG_EXECUTE", insertList);

                                //將紀錄回寫至 EMR Temp Table 20141208 修正劑量為0不拋轉至nis_emrms
                                if (Convert.ToDouble(data[i].USE_DOSE.ToString()) > 0)
                                {
                                    ////string sqlstr = "begin P_NIS_EMRMS('" + feeno + "','067','護理給藥記錄單(處方)','" + data[i].UD_SEQPK.ToString() + "','" + DateTime.Now.ToString("yyyy/MM/dd HH:mm") + "','" + userinfo.EmployeesNo + "','I');end;";
                                    ////link.DBExec(sqlstr);
                                    #region JAG 簽章
                                    // 20150608 EMR
                                    string strPath = @"C:\\wkhtmltopdf\\wkhtmltopdf.exe";
                                    string filename = @"C:\inetpub\NIS\Images\" + data[i].UD_SEQPK.ToString() + ".pdf";

                                    string port = System.Web.HttpContext.Current.Request.ServerVariables["SERVER_PORT"];
                                    if (port == null || port == "80" || port == "443")
                                        port = "";
                                    else
                                        port = ":" + port;

                                    string protocol = System.Web.HttpContext.Current.Request.ServerVariables["SERVER_PORT_SECURE"];
                                    if (protocol == null || protocol == "0")
                                        protocol = "http://";
                                    else
                                        protocol = "https://";

                                    string sOut = protocol + System.Web.HttpContext.Current.Request.ServerVariables["SERVER_NAME"] + port + System.Web.HttpContext.Current.Request.ApplicationPath;

                                    if (sOut.EndsWith("/"))
                                    {
                                        sOut = sOut.Substring(0, sOut.Length - 1);
                                    }

                                    string url = sOut + "/CommonMedication/Med_PDF?feeno=" + feeno + "&med=" + data[i].MED_DESC.Trim() + "&path=" + data[i].UD_PATH.Trim() + "&cir=" + data[i].UD_CIR.Trim() + "&dose=" + data[i].USE_DOSE.Trim();

                                    Process p = new Process();
                                    p.StartInfo.FileName = strPath;
                                    p.StartInfo.Arguments = "\"" + url + "\"" + " " + filename;
                                    p.StartInfo.UseShellExecute = true;
                                    p.Start();
                                    p.WaitForExit();

                                    byte[] listByteCode = webService.UserName(userinfo.EmployeesNo);
                                    string listJsonArray = CompressTool.DecompressString(listByteCode);
                                    UserInfo user_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                                    string emp_id = (user_info.UserID != null) ? user_info.UserID.Trim() : "";
                                    string emp_name = (user_info.EmployeesName != null) ? user_info.EmployeesName.Trim() : "";
                                    string dep_no = ptinfo.DeptNo;
                                    string chr_no = ptinfo.ChartNo;
                                    string pat_name = ptinfo.PatientName;
                                    string in_date = ptinfo.InDate.ToString("yyyyMMdd");
                                    string chagre_type = (ptinfo.PayInfo == "健保") ? "001" : "000";
                                    int result = emr_sign(data[i].UD_SEQPK.ToString(), feeno, "067", userinfo.EmployeesNo, emp_name, emp_id, dep_no, chr_no, pat_name, in_date, chagre_type, filename);
                                    #endregion
                                }


                            }
                            catch
                            {
                                return false;
                            }
                        }
                        #endregion
                        #region DRUG_IV 點滴INSERT
                        if (data[@i].DRUG_TYPE == "V")
                        {
                            List<DBItem> insertList = new List<DBItem>();
                            insertList.Add(new DBItem("UD_SEQ", data[i].UD_SEQ.ToString(), DBItem.DBDataType.String));
                            insertList.Add(new DBItem("FEE_NO", feeno, DBItem.DBDataType.String));
                            insertList.Add(new DBItem("MED_CODE", data[i].MED_CODE.ToString(), DBItem.DBDataType.String));
                            insertList.Add(new DBItem("USE_DOSE", data[i].USE_DOSE.ToString(), DBItem.DBDataType.String));
                            insertList.Add(new DBItem("USE_UNIT", "Bot", DBItem.DBDataType.String));
                            insertList.Add(new DBItem("COST_CODE", cost_code, DBItem.DBDataType.String));
                            insertList.Add(new DBItem("DRUG_DATE", Convert.ToDateTime(data[i].DRUG_DATE).ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                            insertList.Add(new DBItem("UPD_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                            insertList.Add(new DBItem("CHR_NO", ptinfo.ChartNo, DBItem.DBDataType.String));
                            insertList.Add(new DBItem("BED_NO", ptinfo.BedNo, DBItem.DBDataType.String));
                            effRow += this.link.DBExecInsert("DRUG_IV", insertList);
                        }
                        #endregion
                        #region INSERT NIS_MED_INSULIN
                        //註：當註記為延遲給藥時，第一次不寫入NIS_MED_INSULIN，第二次執行在寫入
                        //未執行給藥也不寫入NIS_MED_INSULIN
                        //if (data[i].POSITION != "" && data[i].POSITION != null && data[i].DRUG_TYPE.ToString() == "E" && data[i].ORDSTATUS != "2" && data[i].REASONTYPE != "1")
                        // 20140423 胰島素新增藥品代碼第一碼為 I 之判斷條件
                        // 20141006 新增判斷藥品代碼為 OMIS 的自備胰島素
                        if (data[i].POSITION != "" && data[i].POSITION != null && data[i].DRUG_TYPE.ToString() == "E" && data[i].ORDSTATUS != "2" && data[i].REASONTYPE != "1" && (data[i].MED_CODE.Substring(0, 1).ToUpper() == "I" || data[i].MED_CODE.Substring(0, 4).ToUpper() == "OMIS"))
                        {
                            List<DBItem> insertList = new List<DBItem>();
                            insertList.Add(new DBItem("INID", data[i].UD_SEQPK.ToString(), DBItem.DBDataType.String));
                            insertList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));

                            // 20140423 將給藥時間紀錄為應給藥或選擇給藥時間
                            insertList.Add(new DBItem("INDATE", data[i].DRUG_DATE.ToString(), DBItem.DBDataType.String));
                            //if (data[i].UD_CIR.ToString().Trim() == "STAT")
                            //    insertList.Add(new DBItem("INDATE", Convert.ToDateTime(data[i].DRUG_DATE).ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.String));
                            //else if(data[i].UPD_TYPE == "U")
                            //    insertList.Add(new DBItem("INDATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.String));
                            //else
                            //    insertList.Add(new DBItem("INDATE", Convert.ToDateTime(data[i].DRUG_DATE).ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.String));
                            insertList.Add(new DBItem("IN_DRUGTYPE", data[i].MED_CODE.ToString(), DBItem.DBDataType.String));
                            insertList.Add(new DBItem("IN_DRUGNAME", data[i].MED_DESC.ToString().Trim(), DBItem.DBDataType.String));
                            insertList.Add(new DBItem("IN_DOSE", data[i].USE_DOSE.ToString(), DBItem.DBDataType.String));
                            insertList.Add(new DBItem("IN_ROUTE", data[i].UD_PATH.ToString().Trim(), DBItem.DBDataType.String));
                            insertList.Add(new DBItem("IN_DOSEUNIT", "unit", DBItem.DBDataType.String));
                            if (data[i].SS_DRUGNAME != null && data[i].SS_DRUGNAME != "請選擇")
                                insertList.Add(new DBItem("SS_DRUGNAME", data[i].SS_DRUGNAME.ToString(), DBItem.DBDataType.String));
                            if (data[i].SS_DOSE != null && data[i].SS_DRUGNAME != "請選擇")
                                insertList.Add(new DBItem("SS_DOSE", data[i].SS_DOSE.ToString(), DBItem.DBDataType.String));
                            insertList.Add(new DBItem("POSITION", data[i].POSITION.ToString().Substring(0, 1), DBItem.DBDataType.String));
                            if (data[i].REASON != null)
                                insertList.Add(new DBItem("REASON", data[i].REASON.ToString(), DBItem.DBDataType.String));
                            if (data[i].REASONTYPE != null)
                                insertList.Add(new DBItem("REASONTYPE", data[i].REASONTYPE.ToString(), DBItem.DBDataType.String));
                            insertList.Add(new DBItem("INJECTION", userinfo.EmployeesName, DBItem.DBDataType.String));
                            insertList.Add(new DBItem("STATUS", "new", DBItem.DBDataType.String));
                            if (data[i].CHECKER != null)
                                insertList.Add(new DBItem("REVIEW", data[i].CHECKER.ToString(), DBItem.DBDataType.String));
                            insertList.Add(new DBItem("INSDT", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                            insertList.Add(new DBItem("INSOP", userinfo.EmployeesNo, DBItem.DBDataType.String));
                            insertList.Add(new DBItem("INSOPNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                            insertList.Add(new DBItem("MODDT", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                            insertList.Add(new DBItem("MODOP", userinfo.EmployeesNo, DBItem.DBDataType.String));
                            insertList.Add(new DBItem("MODNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                            effRow += this.link.DBExecInsert("NIS_MED_INSULIN", insertList);
                        }
                        #endregion
                        #region INSERT SPECIALDRUG
                        if (data[i].POSITION != "" && data[i].POSITION != null && data[i].DRUG_TYPE.ToString() == "SC")
                        {
                            List<DBItem> insertList = new List<DBItem>();
                            insertList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                            insertList.Add(new DBItem("INDATE", data[i].DRUG_DATE.ToString(), DBItem.DBDataType.String));
                            insertList.Add(new DBItem("POSITION", data[i].POSITION.ToString().Substring(0, 1), DBItem.DBDataType.String));
                            insertList.Add(new DBItem("INSDT", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                            insertList.Add(new DBItem("INSOP", userinfo.EmployeesNo, DBItem.DBDataType.String));
                            insertList.Add(new DBItem("INSOPNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                            insertList.Add(new DBItem("DRUGNAME", data[i].ALISE_DESC.ToString().Trim(), DBItem.DBDataType.String));
                            //insertList.Add(new DBItem("DRUGTYPE", data[i].UD_TYPE.ToString(), DBItem.DBDataType.String));
                            insertList.Add(new DBItem("DOSE", data[i].USE_DOSE.ToString(), DBItem.DBDataType.String));
                            insertList.Add(new DBItem("ROUTE", data[i].UD_PATH.ToString().Trim(), DBItem.DBDataType.String));
                            // insertList.Add(new DBItem("DOSEUNIT", data[i].UD_UNIT.ToString(), DBItem.DBDataType.String));
                            insertList.Add(new DBItem("STATUS", "new", DBItem.DBDataType.String));
                            insertList.Add(new DBItem("SDID", data[i].UD_SEQPK.ToString(), DBItem.DBDataType.String));
                            if (data[i].REASON != null)
                                insertList.Add(new DBItem("REASON", data[i].REASON.ToString(), DBItem.DBDataType.String));
                            if (data[i].REASONTYPE != null)
                                insertList.Add(new DBItem("REASONTYPE", data[i].REASONTYPE.ToString(), DBItem.DBDataType.String));
                            insertList.Add(new DBItem("INJECTION", userinfo.EmployeesName, DBItem.DBDataType.String));
                            if (data[i].DRUG_TYPE.ToString() == "E")
                            {
                                insertList.Add(new DBItem("PAGE", "IN", DBItem.DBDataType.String));
                            }
                            else
                            {
                                insertList.Add(new DBItem("PAGE", "SC", DBItem.DBDataType.String));
                            }
                            effRow += this.link.DBExecInsert("SPECIALDRUG", insertList);
                        }
                        #endregion
                        #region 護理紀錄
                        string mag = "", strExec = "", title = "", dose = data[i].USE_DOSE;
                        string MED_DESC = data[i].MED_DESC.ToString().Trim();
                        string ALISE_DESC = "";
                        if (data[i].ALISE_DESC == null)
                        { ALISE_DESC = ""; }
                        else
                        { ALISE_DESC = data[i].ALISE_DESC.ToString().Trim(); }
                        Boolean save_f = false;
                        if (data[i].USE_DOSE != null && data[i].USE_DOSE != "")
                        {
                            dose = data[i].USE_DOSE;
                        }
                        //title = MED_DESC + " " + dose + " " + data[i].UD_PATH + "" + data[i].UD_CIR;
                        title = MED_DESC;
                        if (dose != "" && Convert.ToDouble(dose) != 0)
                        {
                            strExec = "執行內容:" + dose + " " + data[i].UD_UNIT + " " + data[i].UD_PATH + "" + data[i].UD_CIR + "。";
                        }

                        if (data[i].DRUG_TYPE.ToString() == "E" && data[i].SS_DOSE != null && data[i].SS_DRUGNAME.ToString().Trim() != "請選擇")
                        {
                            if (dose == "0")
                                strExec = "(Sliding Scale)" + data[i].SS_DRUGNAME.ToString().Trim() + " " + data[i].SS_DOSE + " " + data[i].UD_UNIT + " " + data[i].UD_PATH + "" + data[i].UD_CIR + "。";
                            else if (data[i].SS_DOSE != "0")
                                strExec += "(Sliding Scale)" + data[i].SS_DRUGNAME.ToString().Trim() + " " + data[i].SS_DOSE + " " + data[i].UD_UNIT + " " + data[i].UD_PATH + "" + data[i].UD_CIR + "。";
                        }

                        switch (data[i].REASONTYPE)
                        {
                            case "1"://未執行
                                mag = "病人因" + data[i].REASON + "未執行 " + MED_DESC + "(" + ALISE_DESC + ")。";
                                break;
                            /* 提早跟延遲不帶護理紀錄 
                             case "2"://提早執行
                                 mag = "病人因" + data[i].REASON + "提早執行 " + MED_DESC + "(" + ALISE_DESC + ")。";
                                 break;
                             case "3"://延遲執行
                                 mag = "病人因" + data[i].REASON + "延遲給予 " + MED_DESC + "(" + ALISE_DESC + ")。";
                                 break;*/
                            //過時給藥不帶護理紀錄 by 萬芳
                            //case "4"://拒絕
                            //    mag = "病人因" + data[i].REASON + "過時給藥" + MED_DESC + "(" + ALISE_DESC + ")。";
                            //    break;
                            case null:
                                break;
                            default:
                                break;
                        }
                        if (data[i].BADREACTION != "" && data[i].BADREACTION != null)
                        {
                            var str = Convert.ToInt16(DateTime.Now.ToString("yyyy")) - 1911 + "/" + DateTime.Now.ToString("MM/dd HH:mm:ss");
                            mag += "病人於" + str + "服用" + MED_DESC + "(" + ALISE_DESC + ") 產生 " + data[i].BADREACTION + " 不良反應情形。";
                        }

                        if (mag == "" || data[i].DRUG_TYPE.ToString() == "E")
                        {
                            mag = strExec + mag;
                        }
                        else
                        {
                            save_f = true;
                            title = ""; //註：當有註記時，寫入護理紀錄不用抬頭
                        }
                        //註：有填寫註記或頻次為STAT,ASORDER,PRN寫入護理記錄
                        if (data[i].UD_CIR.ToString().Trim() == "STAT" || data[i].UD_CIR.ToString().Trim() == "ASORDER" || data[i].UD_CIR.IndexOf("PRN", 0) > -1)
                        {
                            Insert_CareRecord(Convert.ToDateTime(data[i].DRUG_DATE).ToString("yyyy/MM/dd HH:mm:ss"), data[i].UD_SEQPK.ToString(), title, "", "", "", mag, "", "C_Medication");
                        }//20140617 新增藥品代碼為 I 的胰島素才寫入護理紀錄
                        else if (save_f == true || data[i].DRUG_TYPE.ToString() == "E" && data[i].ORDSTATUS == "0" && data[i].MED_CODE.Substring(0, 1).ToUpper() == "I")
                        {
                            Insert_CareRecord(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), data[i].UD_SEQPK.ToString(), title, "", "", "", mag, "", "C_Medication");
                        }
                        #endregion
                    }
                }
                if (effRow > 0)
                {   //儲存成功
                    return true;
                }
                else
                {   //儲存失敗
                    return false;
                }
            }
            return true;
        }
        #endregion

        #region Med_Cancel updDrugCancel
        public bool updDrugCancel(List<DRUG_EXECUTE> data, string feeno, string cost_code)
        {
            if (data != null) //判斷儲存勾選資料是否有無
            {
                int effRow = 0;
                string where = "";
                for (int i = 0; i < data.Count; i++)
                {
                    if (data[i].ORDSTATUS == "0")//有打勾的
                    {
                        #region DRUG_EXECUTE
                        //如果不等於prn或點滴的就做清空,反之則刪除
                        if (data[i].UD_SEQPK.Substring(0, 1).ToString() != "P" && data[i].UD_SEQPK.Substring(0, 1).ToString() != "V")
                        {
                            where = " UD_SEQPK = '" + data[i].UD_SEQPK.ToString() + "' AND FEE_NO = '" + feeno + "' ";
                            List<DBItem> updList = new List<DBItem>();
                            updList.Add(new DBItem("EXEC_ID", "", DBItem.DBDataType.String));
                            updList.Add(new DBItem("EXEC_NAME", "", DBItem.DBDataType.String));
                            updList.Add(new DBItem("EXEC_DATE", "", DBItem.DBDataType.DataTime));
                            updList.Add(new DBItem("REASONTYPE", "", DBItem.DBDataType.String));
                            updList.Add(new DBItem("REASON", "", DBItem.DBDataType.String));
                            updList.Add(new DBItem("CHECKER_DATE", "", DBItem.DBDataType.DataTime));
                            updList.Add(new DBItem("CHECKER_ID", "", DBItem.DBDataType.String));
                            updList.Add(new DBItem("CHECKER", "", DBItem.DBDataType.String));
                            updList.Add(new DBItem("BADREACTION", "", DBItem.DBDataType.String));
                            updList.Add(new DBItem("SIGNMODE", "A", DBItem.DBDataType.String));
                            effRow += this.link.DBExecUpdate("DRUG_EXECUTE", updList, where);
                            //將紀錄回寫至 EMR Temp Table
                            ////string sqlstr = "begin P_NIS_EMRMS('" + feeno + "','067','護理給藥記錄單(處方)','" + data[i].UD_SEQPK.ToString() + "','','" + userinfo.EmployeesNo + "','D');end;";
                            ////link.DBExec(sqlstr);
                            //int result = del_emr();
                        }
                        else
                        {
                            where = " UD_SEQPK = '" + data[i].UD_SEQPK.ToString() + "' AND FEE_NO = '" + feeno + "' ";
                            //  where = "UD_SEQPK = '" + data[i].UD_SEQPK + "'";
                            this.link.DBExecDelete("DRUG_EXECUTE", where);
                            //將紀錄回寫至 EMR Temp Table
                            ////string sqlstr = "begin P_NIS_EMRMS('" + feeno + "','067','護理給藥記錄單(處方)','" + data[i].UD_SEQPK.ToString() + "','','" + userinfo.EmployeesNo + "','D');end;";
                            ////link.DBExec(sqlstr);
                            //int result = del_emr();
                        }
                        #endregion
                        #region INSULIN
                        //INSULIN

                        //if (data[i].INSULIN_SITE !=null&&data[i].INSULIN_SITE.ToString().Trim() != "")
                        if (Check_insulin_in_drugEX_exstOrNot == true)
                        {
                            //where = "FEENO = '" + feeno + "' AND (INDATE = '" + Convert.ToDateTime(data[i].DRUG_DATE).ToString("yyyy/MM/dd HH:mm") + "'";
                            //where += " OR INID = '" + data[i].UD_SEQPK.ToString() +"')";

                            where = "FEENO = '" + feeno + "' AND INID = '" + data[i].UD_SEQPK.ToString() + "'";
                            this.link.DBExecDelete("NIS_MED_INSULIN", where);
                        }
                        #endregion
                        #region 點滴
                        //點滴
                        if (data[i].DRUG_TYPE == "V")
                        {
                            where = "FEE_NO ='" + feeno + "' AND UD_SEQ = '" + data[i].UD_SEQ + "' AND TO_CHAR(DRUG_DATE,'yyyy/MM/dd hh24:mi:ss')='" + data[i].DRUG_DATE + "'";
                            this.link.DBExecDelete("DRUG_IV", where);
                        }
                        #endregion
                        #region 護理記錄
                        /* if (data[i].UD_CIR.ToString().Trim() == "STAT" || data[i].UD_CIR.ToString().Trim() == "ASORDER" || data[i].UD_CIR.IndexOf("PRN", 0) > -1)
                        { }*/
                        where = "CARERECORD_ID ='" + data[i].UD_SEQPK + feeno + "'";
                        base.Del_CareRecord(data[i].UD_SEQPK + feeno, "drug_execute");

                        #endregion
                        #region 取消給藥EMR
                        try
                        {
                            int result = del_emr(data[i].UD_SEQPK + feeno, userinfo.EmployeesNo);
                        }
                        catch { }
                        #endregion
                    }
                }
            }
            return true;
        }
        #endregion

        #region GET WEBSERVICE

        //病人用藥
        public string WebS_UdOrder(string feeno, string start, string end, string ExecFlag, string medcode)
        {
            byte[] doByteCode = webService.GetUdOrder(feeno, "A");
            if (doByteCode == null)
            { return ("weberror"); }
            string doJsonArr = CompressTool.DecompressString(doByteCode);
            List<UdOrder> GetUdOrderList = JsonConvert.DeserializeObject<List<UdOrder>>(doJsonArr);

            dt_udorder = ConvertToDataTable(GetUdOrderList);
            DataRow[] dr_row;

            if (ExecFlag == "quiryAll" || ExecFlag == "quiryInterval")
            {   //給藥查詢
                dt_all = cm.get_drugtable();
                dt_udorder.DefaultView.Sort = "UD_SEQ, BEGIN_DATE";
                dt_udorder = dt_udorder.DefaultView.ToTable();


                dt_all = dt_udorder.Copy();
            }
            else if (ExecFlag == "quiryAllByUD" || ExecFlag == "quiryIntervalByUD")
            {   //給藥查詢
                dt_all = cm.get_drugtable();
                if (ExecFlag == "quiryAllByUD")
                { dt_all = dt_udorder.Copy(); }
                else
                {
                    dr_row = dt_udorder.Select(cm.Get_DrugListSql(feeno, "UD", start, end, ExecFlag));
                    if (dr_row.Count() > 0)
                    {
                        foreach (DataRow dr in dr_row)
                            dt_all.ImportRow(dr);

                    }
                    else
                    {
                        dt_all = new DataTable();
                    }

                }
            }
            else if (ExecFlag == "C_first" || ExecFlag == "C_quiryAll")
            {   //取消給藥
                dt_all = cm.get_drugtable();
                dr_row = dt_udorder.Select(cm.Get_DrugListSql(feeno, "C", start, end, ExecFlag));
                foreach (DataRow dr in dr_row)
                    dt_all.ImportRow(dr);
            }
            else if (ExecFlag == "QueryOrder")
            {   //用藥查詢
                dt_all = dt_udorder;
            }
            else
            {   //執行給藥
                dt_stat = cm.get_drugtable();
                dt_reg = dt_stat.Clone();
                dt_prn = dt_stat.Clone();
                dt_iv = dt_stat.Clone();

                dr_row = dt_udorder.Select(cm.Get_DrugListSql(feeno, "S", start, end, ExecFlag));
                foreach (DataRow dr in dr_row)
                    dt_stat.ImportRow(dr);

                dr_row = null;
                dr_row = dt_udorder.Select(cm.Get_DrugListSql(feeno, "R", start, end, ExecFlag));
                foreach (DataRow dr in dr_row)
                    dt_reg.ImportRow(dr);

                dr_row = null;
                dr_row = dt_udorder.Select(cm.Get_DrugListSql(feeno, "P", start, end, ExecFlag));
                foreach (DataRow dr in dr_row)
                    dt_prn.ImportRow(dr);

                dr_row = null;
                dr_row = dt_udorder.Select(cm.Get_DrugListSql(feeno, "V", start, end, ExecFlag));
                foreach (DataRow dr in dr_row)
                    dt_iv.ImportRow(dr);

                var sliding = from tmp in GetUdOrderList
                              where tmp.UD_STATUS == "2" && tmp.UD_PATH.Trim() == "SC" && (tmp.UD_TYPE == "P" || tmp.UD_TYPE == "S" || tmp.UD_CIR.Trim() == "ASORDER")
                              select new
                              {
                                  UD_SEQ = tmp.UD_SEQ,
                                  MED_DESC = tmp.MED_DESC.ToString().Trim(),
                                  UD_DOSE = decimal.Parse(tmp.UD_DOSE.ToString().Trim()).ToString("0")
                              };
                List<SelectListItem> sliding_list = new List<SelectListItem>();
                sliding_list.Add(new SelectListItem { Text = "請選擇", Value = "0" });
                if (sliding.Count() > 0)
                {
                    foreach (var tmp in sliding)
                    {
                        sliding_list.Add(new SelectListItem { Text = tmp.MED_DESC, Value = tmp.UD_DOSE });
                    }
                }
                ViewBag.sliding_list = sliding_list;
            }
            return ("OK");
        }

        public DataTable ConvertToDataTable<T>(IList<T> data)
        {
            System.ComponentModel.PropertyDescriptorCollection properties = System.ComponentModel.TypeDescriptor.GetProperties(typeof(T));
            DataTable table = new DataTable();
            foreach (System.ComponentModel.PropertyDescriptor prop in properties)
                table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            foreach (T item in data)
            {
                DataRow row = table.NewRow();
                foreach (System.ComponentModel.PropertyDescriptor prop in properties)
                    row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
                table.Rows.Add(row);
            }
            return table;

        }

        //線上藥典
        public ActionResult WebS_MedicinalInfo()
        {
            string Str_MedCode = string.Empty;
            if (Request["Str_MedCode"] != null)
            {
                try
                {
                    string[] s1 = Request["Str_MedCode"].ToString().Split('|');
                    Str_MedCode = string.Join("','", s1);
                }
                catch
                {
                    Str_MedCode = Request["Str_MedCode"].ToString().Trim();
                }
                byte[] doByteCode = webService.GetMedicalInfo(Str_MedCode);
                if (doByteCode == null)
                {
                    Response.Write("Error");
                }
                else
                {
                    string doJsonArr = CompressTool.DecompressString(doByteCode);
                    Response.Write(doJsonArr);
                }
            }
            return new EmptyResult();
        }

        public string WebS_MedicinalInfo2(string strMedCode)
        {
            byte[] doByteCode = webService.GetMedicalInfo(strMedCode);
            string doJsonArr = CompressTool.DecompressString(doByteCode);
            return (doJsonArr);
        }

        //藥品DC追蹤
        public ActionResult WebS_MedOrderRenew()
        {
            //string str = string.Empty;
            //string strBarcode = Request["str_Barcode"].ToString().Trim();
            //if (Request["str_Barcode"] != null)
            //{
            //    byte[] doByteCode = webService.GetMedOrderRenew(strBarcode, "D");
            //    if (doByteCode == null)
            //    { Response.Write("Error"); }
            //    else
            //    {
            //        string doJsonArr = CompressTool.DecompressString(doByteCode);
            //        Response.Write(doJsonArr);
            //    }
            //}
            return new EmptyResult();
        }

        public string WebS_MedOrderRenew2(string strBarcode)
        {
            //string str = string.Empty;
            //byte[] doByteCode;
            //if (strBarcode != null)
            //{
            //    doByteCode = webService.GetMedOrderRenew(strBarcode, "R");
            //    if (doByteCode == null)
            //    { Response.Write("Error"); }
            //    else
            //    {
            //        string doJsonArr = CompressTool.DecompressString(doByteCode);
            //        return (doJsonArr);
            //    }
            //}
            return "";
        }
        #endregion

        #region 雜物箱
        //餐包藥品條碼檢核
        public ActionResult Med_Bacrcode()
        {
            try
            {
                string strBarcode = string.Empty;
                string strFlag = string.Empty;
                string strsql = "", strUDSEQ = "", strMedCode = "";
                List<MedInfoB> med_list = new List<MedInfoB>();
                PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
                if (Request["str_Barcode"] != null)
                {
                    strBarcode = Request["str_Barcode"].ToString().Trim();
                    if (strBarcode.Length == 18)
                    {
                        strsql = "SELECT DRUG_CODE AS MED_CODE, RIGHT_TIME AS DRUGTIME,B.UD_SEQ AS UD_SEQ FROM BCMA_BCPRINT A, DRUG_EXECUTE B";
                        strsql += " WHERE TRIM(A.UNCRCPPK) = B.UD_SEQPK AND A.BARCODE = '" + strBarcode + "' AND A.PCASENO ='" + ptInfo.FeeNo + "'";
                    }
                    else
                    {
                        return new EmptyResult();
                    }
                    DataTable Dt = link.DBExecSQL(strsql);
                    if (Dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            byte[] doByteCode = null;
                            string ud_seq_new = "", doJsonArr = "";
                            //doByteCode = webService.GetMedOrderRenew(reader["UD_SEQ"].ToString().Trim(), "D");
                            if (doByteCode != null)
                            {
                                doJsonArr = CompressTool.DecompressString(doByteCode);
                                List<MedOrderRenew> UdList = JsonConvert.DeserializeObject<List<MedOrderRenew>>(doJsonArr);
                                ud_seq_new = UdList[0].UDSEQ.Trim();
                            }

                            med_list.Add(new MedInfoB()
                            {
                                med_code = Dt.Rows[i]["MED_CODE"].ToString().Trim(),
                                med_time = Dt.Rows[i]["DRUGTIME"].ToString().Trim(),
                                ud_seq = Dt.Rows[i]["UD_SEQ"].ToString().Trim(),
                                ud_seq_o = ud_seq_new,
                                flag = ""
                            });
                            strMedCode += Dt.Rows[i]["MED_CODE"].ToString().Trim() + "','";
                            strUDSEQ += Dt.Rows[i]["UD_SEQ"].ToString().Trim() + ",";
                        }
                    }

                    Response.Write(JsonConvert.SerializeObject(med_list));
                }

                return new EmptyResult();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return new EmptyResult();
            }
        }

        //Session 檢查
        public bool Check_Session(string success)
        {
            if (Session["PatInfo"] != null)
            {
                if (success == "yes")
                    Response.Write("<script>alert('儲存成功!')</script>");
                else if (success == "no")
                    Response.Write("<script>alert('儲存失敗!')</script>");
                else if (success == "ck")
                    Response.Write("<script>alert('未勾選給藥時間!')</script>");
                else if (success == "unselcet_pat")
                    Response.Write("<script>alert('尚未選擇病患!')</script>");
                else if (success == "overdue")
                    Response.Write("<script>alert('Session過期!')</script>");
                else if (success == "weberror")
                    Response.Write("<script>alert('此病人尚未開立任何藥囑!')</script>");
                return false;
            }
            else
            {
                return true;
            }
        }

        //點滴標籤列印
        public string IV_Label_Print()
        {
            int effRow = 0;
            string UD_SEQ = Request["str_Barcode"];
            string use_dose = string.Empty;
            //20140718 新增單位判斷針劑或點滴
            if (Request["use_unit"].ToString().Trim() == "bag" || Request["use_unit"].ToString().Trim() == "bot")
            {
                use_dose = "1";
            }
            else
            {
                use_dose = Request["use_dose"].ToString().Trim();
            }

            List<DBItem> insertList = new List<DBItem>();
            insertList.Add(new DBItem("UD_SEQ", Request["ud_seq"].ToString().Trim(), DBItem.DBDataType.String));
            insertList.Add(new DBItem("FEE_NO", Request["fee_no"].ToString().Trim(), DBItem.DBDataType.String));
            insertList.Add(new DBItem("MED_CODE", Request["med_code"].ToString().Trim(), DBItem.DBDataType.String));
            //insertList.Add(new DBItem("USE_DOSE", Request["use_dose"].ToString().Trim(), DBItem.DBDataType.String));
            //insertList.Add(new DBItem("USE_DOSE", "1", DBItem.DBDataType.String));
            insertList.Add(new DBItem("USE_DOSE", use_dose, DBItem.DBDataType.String));
            insertList.Add(new DBItem("USE_UNIT", Request["use_unit"].ToString().Trim(), DBItem.DBDataType.String));
            insertList.Add(new DBItem("COST_CODE", Request["cost_code"].ToString().Trim(), DBItem.DBDataType.String));
            insertList.Add(new DBItem("PRINT_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
            insertList.Add(new DBItem("PRINT_USER", userinfo.EmployeesName.ToString().Trim(), DBItem.DBDataType.String));
            insertList.Add(new DBItem("FLOW_SPEED", Request["flow_speed"].ToString().Trim(), DBItem.DBDataType.String));
            insertList.Add(new DBItem("BIRTH_DATE", NIS.UtilTool.ConvertTool.toRocDate(ptinfo.Birthday).Replace("-", "/"), DBItem.DBDataType.String));
            insertList.Add(new DBItem("PAT_NAME", ptinfo.PatientName, DBItem.DBDataType.String));
            insertList.Add(new DBItem("BED_NO", ptinfo.BedNo, DBItem.DBDataType.String));
            effRow += this.link.DBExecInsert("DRUG_IV_LABEL", insertList);
            if (effRow > 0)
                return "ok";
            else
                return "error";
        }

        //雙人覆核LOGIN
        public string DoubleLogin(string id, string pw)
        {
            string checkname;
            if (userinfo.EmployeesNo == id)
                return "iderr";
            byte[] uData = webService.UserLogin(id, pw);
            if (uData != null)
            {
                string jsonstr = CompressTool.DecompressString(uData);
                UserInfo user = JsonConvert.DeserializeObject<UserInfo>(jsonstr);
                checkname = user.EmployeesName.ToString().Trim();
            }
            else
            {
                checkname = "error";
            }
            return checkname;
        }
        #endregion

        #region  給藥作業(恩主公)

        public ActionResult Medication_NurseExecute()
        {
            if (Session["PatInfo"] != null)
            {
                //宣告病患_取得住院日期
                PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
                ViewBag.InDate = Convert.ToDateTime(ptInfo.InDate).ToString("yyyy/MM/dd");
                this.SetMedicationTime(DateTime.Now.ToString("yyyy/M/d"));
                return View();
            }
            else
            {
                Response.Write("<script>alert('請重新選擇病患');</script>");
                return new EmptyResult();
            }
        }
        public ActionResult Medication_NurseExecutePain()
        {
            if (Session["PatInfo"] != null)
            {
                //宣告病患_取得住院日期
                PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
                ViewBag.InDate = Convert.ToDateTime(ptInfo.InDate).ToString("yyyy/MM/dd");
                this.SetMedicationTimePain(DateTime.Now.ToString("yyyy/M/d"));
                return View();
            }
            else
            {
                Response.Write("<script>alert('請重新選擇病患');</script>");
                return new EmptyResult();
            }
        }
        private void SetMedicationTime(string pStartDate)
        {

            List<UdOrder> GetUdOrderList = new List<UdOrder>();
            DataTable MedDt = new DataTable();
            List<UdOrder> LongList = new List<UdOrder>();
            List<UdOrder> PRNList = new List<UdOrder>();
            List<UdOrder> StatList = new List<UdOrder>();
            List<UdOrder> IVList = new List<UdOrder>();

            DataTable LongDt = new DataTable();
            DataTable PRNDt = new DataTable();
            DataTable StatDt = new DataTable();
            DataTable IVDt = new DataTable();

            Stopwatch sw = new Stopwatch();
            sw.Reset();
            sw.Start();
            byte[] doByteCode = webService.GetUdOrder(ptinfo.FeeNo, "A");
            sw.Stop();
            double WsTimer = sw.ElapsedMilliseconds;
            this.log.saveLogMsg("使用者：" + userinfo.EmployeesNo + "，病床號" + ptinfo.BedNo + "，病人批價序號：" + ptinfo.FeeNo + "，花費時間：" + WsTimer.ToString() + " ms", "GetUdOrderTimer");

            string doJsonArr = "";
            if (doByteCode != null)
            {
                doJsonArr = CompressTool.DecompressString(doByteCode);
                string TmpBeginDate = (Convert.ToDateTime(pStartDate).Year - 1911).ToString() + Convert.ToDateTime(pStartDate).Date.ToString("MMdd");
                GetUdOrderList = JsonConvert.DeserializeObject<List<UdOrder>>(doJsonArr);
                /*2016/07/27*/
                string TmpInDate = ptinfo.InDate.AddYears(-1911).ToString("yyyyMMdd");
                //var temp = GetUdOrderList.Where(x => Convert.ToInt64(x.BEGIN_DATE) >= Convert.ToInt64(TmpInDate) &&
                // Convert.ToInt64(trans_tcdate(x.BEGIN_DATE, x.BEGIN_TIME, -1)) <= Convert.ToInt64(trans_tcdate(TmpBeginDate, "", 0)) &&
                // (Convert.ToInt64(x.END_DATE) == 0 ||
                // (Convert.ToInt64(x.END_DATE) != 0 && Convert.ToInt64(x.END_DATE + x.END_TIME) >= Convert.ToInt64(TmpBeginDate))
                // )).ToArray();
                string[] UD_SEQ_ARRAY = GetUdOrderList.Where(x => Convert.ToInt64(x.BEGIN_DATE) >= Convert.ToInt64(TmpInDate) &&
                 Convert.ToInt64(trans_tcdate(x.BEGIN_DATE, x.BEGIN_TIME, -1)) <= Convert.ToInt64(trans_tcdate(TmpBeginDate, "", 0)) &&
                 (Convert.ToInt64(x.END_DATE) == 0 ||
                 (Convert.ToInt64(x.END_DATE) != 0 && Convert.ToInt64(x.END_DATE) >= Convert.ToInt64(TmpBeginDate))
                 )).Select(x => x.UD_SEQ).ToArray();
                //「醫囑開始日期 >= 入院日期」且「醫囑開始日期 <= 查詢日期」且(「醫囑結束日期 == 0」或「醫囑結束日期 >= 查詢日期」)
                //20160728 mod by yungchen
                MedDt = this.cm.CurrentMedData(string.Join("','", UD_SEQ_ARRAY), pStartDate, ptinfo.FeeNo);

                //已交集的給藥檔
                string[] CrossUdSeq = MedDt.AsEnumerable().Select(x => x.Field<string>("UD_SEQ")).ToArray();
                //PRN //IV=點滴 增加DC後給藥 該藥物的顯示
                PRNList = GetUdOrderList.Where(x => (x.UD_TYPE == "P" && (Convert.ToInt64(x.BEGIN_DATE) <= Convert.ToInt64(TmpBeginDate) && (Convert.ToInt64(x.END_DATE) >= Convert.ToInt64(TmpBeginDate) || Convert.ToInt64(x.END_DATE) == 0) || CrossUdSeq.Contains(x.UD_SEQ)))).OrderBy(x => x.BEGIN_DATE).ToList();
                IVList = GetUdOrderList.Where(x => (x.UD_TYPE == "IV" && (Convert.ToInt64(x.BEGIN_DATE) <= Convert.ToInt64(TmpBeginDate) && (Convert.ToInt64(x.END_DATE) >= Convert.ToInt64(TmpBeginDate) || Convert.ToInt64(x.END_DATE) == 0) || CrossUdSeq.Contains(x.UD_SEQ)))).OrderBy(x => x.DRUG_TYPE).ThenBy(x => x.BEGIN_DATE).ToList();

                PRNDt = this.cm.CurrentMedData(string.Join("','", PRNList.Select(x => x.UD_SEQ).ToArray()), pStartDate, ptinfo.FeeNo);
                IVDt = this.cm.CurrentMedData(string.Join("','", IVList.Select(x => x.UD_SEQ).ToArray()), pStartDate, ptinfo.FeeNo);
                if (MedDt != null && MedDt.Rows.Count > 0)
                {//LONG=長期  //Start=臨時
                    LongList = GetUdOrderList.Where(x => x.UD_TYPE == "R" && CrossUdSeq.Contains(x.UD_SEQ)).OrderBy(x => x.BEGIN_DATE).ToList();
                    StatList = GetUdOrderList.Where(x => x.UD_TYPE == "S" && CrossUdSeq.Contains(x.UD_SEQ)).OrderBy(x => x.BEGIN_DATE).ToList();
                    // IVList = GetUdOrderList.Where(x => x.UD_TYPE == "IV" && CrossUdSeq.Contains(x.UD_SEQ)).OrderBy(x => x.DRUG_TYPE).ThenBy(x => x.BEGIN_DATE).ToList();

                    LongDt = this.cm.CurrentMedData(string.Join("','", LongList.Select(x => x.UD_SEQ).ToArray()), pStartDate, ptinfo.FeeNo);
                    StatDt = this.cm.CurrentMedData(string.Join("','", StatList.Select(x => x.UD_SEQ).ToArray()), pStartDate, ptinfo.FeeNo);
                }
            }

            ViewBag.LongList = LongList.OrderBy(x => x.UD_PATH).OrderBy(x => x.UD_CIR).OrderBy(x => x.DC_FLAG).ToList();
            ViewBag.PRNList = PRNList.OrderBy(x => x.UD_PATH).OrderBy(x => x.UD_CIR).OrderBy(x => x.DC_FLAG).ToList();
            //ViewBag.PRNList = PRNList.OrderBy(x => x.DC_FLAG).OrderBy(x => x.UD_CIR).OrderBy(x => x.UD_PATH).ToList();
            ViewBag.StatList = StatList.OrderBy(x => x.UD_PATH).OrderBy(x => x.UD_CIR).OrderBy(x => x.DC_FLAG).ToList();
            ViewBag.IVList = IVList.OrderBy(x => x.UD_PATH).OrderBy(x => x.UD_CIR).OrderBy(x => x.DC_FLAG).ToList();

            ViewBag.LongDt = LongDt;
            ViewBag.PRNDt = PRNDt;
            ViewBag.StatDt = StatDt;
            ViewBag.IVDt = IVDt;
            ViewData["late_reason"] = this.cd.getSelectItem("common_medication", "late_reason");

            //ViewData["insulin_section"] = this.cd.getSelectItem("common_medication", "insulin_section");
            //胰島素 排除禁打部位
            List<SelectListItem> InsulinDdl = this.cd.getSelectItem("common_medication", "insulin_section");
            DataTable InsulinBanDB = this.cm.InsulinBanOrLastPosition(ptinfo.FeeNo, "SPECIALDRUG_SET");
            string[] BanPositionList = null;
            if (InsulinBanDB != null && InsulinBanDB.Rows.Count > 0)
            {
                BanPositionList = InsulinBanDB.Rows[0]["BAN"].ToString().Split(',');
                foreach (string r in BanPositionList)
                {
                    for (int i = 0; i <= InsulinDdl.Count - 1; i++)
                    {
                        if (InsulinDdl[i].Value == r)
                        {
                            InsulinDdl.RemoveAt(i);
                            continue;
                        }
                    }
                }
            }
            ViewData["insulin_section"] = InsulinDdl;
            //胰島素 最後施打部位
            DataTable InsulinLastPositionDB = this.cm.InsulinBanOrLastPosition(ptinfo.FeeNo, "NIS_MED_INSULIN");
            string InsulinLastPosition = "";
            if (InsulinLastPositionDB != null && InsulinLastPositionDB.Rows.Count > 0)
            {
                InsulinLastPosition = InsulinLastPositionDB.Rows[0]["POSITION"].ToString();
            }

            ViewBag.InsulinLastPosition = InsulinLastPosition;
            ViewBag.pStartDate = pStartDate;
        }
        private void SetMedicationTimePain(string pStartDate)
        {

            List<UdOrder> GetUdOrderList = new List<UdOrder>();
            DataTable MedDt = new DataTable();
            List<UdOrder> LongList = new List<UdOrder>();
            List<UdOrder> PRNList = new List<UdOrder>();
            List<UdOrder> StatList = new List<UdOrder>();
            List<UdOrder> IVList = new List<UdOrder>();

            DataTable LongDt = new DataTable();
            DataTable PRNDt = new DataTable();
            DataTable StatDt = new DataTable();
            DataTable IVDt = new DataTable();

            Stopwatch sw = new Stopwatch();
            sw.Reset();
            sw.Start();
            byte[] doByteCode = webService.GetUdOrder(ptinfo.FeeNo, "A");
            sw.Stop();
            double WsTimer = sw.ElapsedMilliseconds;
            this.log.saveLogMsg("使用者：" + userinfo.EmployeesNo + "，病床號" + ptinfo.BedNo + "，病人批價序號：" + ptinfo.FeeNo + "，花費時間：" + WsTimer.ToString() + " ms", "GetUdOrderTimer");

            string doJsonArr = "";
            if (doByteCode != null)
            {
                doJsonArr = CompressTool.DecompressString(doByteCode);
                string TmpBeginDate = (Convert.ToDateTime(pStartDate).Year - 1911).ToString() + Convert.ToDateTime(pStartDate).Date.ToString("MMdd");
                GetUdOrderList = JsonConvert.DeserializeObject<List<UdOrder>>(doJsonArr);
                /*2016/07/27*/
                string TmpInDate = ptinfo.InDate.AddYears(-1911).ToString("yyyyMMdd");
                //var temp = GetUdOrderList.Where(x => Convert.ToInt64(x.BEGIN_DATE) >= Convert.ToInt64(TmpInDate) &&
                // Convert.ToInt64(trans_tcdate(x.BEGIN_DATE, x.BEGIN_TIME, -1)) <= Convert.ToInt64(trans_tcdate(TmpBeginDate, "", 0)) &&
                // (Convert.ToInt64(x.END_DATE) == 0 ||
                // (Convert.ToInt64(x.END_DATE) != 0 && Convert.ToInt64(x.END_DATE + x.END_TIME) >= Convert.ToInt64(TmpBeginDate))
                // )).ToArray();
                string[] UD_SEQ_ARRAY = GetUdOrderList.Where(x => Convert.ToInt64(x.BEGIN_DATE) >= Convert.ToInt64(TmpInDate) &&
                 Convert.ToInt64(trans_tcdate(x.BEGIN_DATE, x.BEGIN_TIME, -1)) <= Convert.ToInt64(trans_tcdate(TmpBeginDate, "", 0)) &&
                 (Convert.ToInt64(x.END_DATE) == 0 ||
                 (Convert.ToInt64(x.END_DATE) != 0 && Convert.ToInt64(x.END_DATE) >= Convert.ToInt64(TmpBeginDate))
                 )).Select(x => x.UD_SEQ).ToArray();
                //「醫囑開始日期 >= 入院日期」且「醫囑開始日期 <= 查詢日期」且(「醫囑結束日期 == 0」或「醫囑結束日期 >= 查詢日期」)
                //20160728 mod by yungchen
                MedDt = this.cm.CurrentMedData(string.Join("','", UD_SEQ_ARRAY), pStartDate, ptinfo.FeeNo);

                //已交集的給藥檔
                string[] CrossUdSeq = MedDt.AsEnumerable().Select(x => x.Field<string>("UD_SEQ")).ToArray();
                //PRN //IV=點滴 增加DC後給藥 該藥物的顯示
                PRNList = GetUdOrderList.Where(x => (x.UD_TYPE == "P" && x.IsAnalgesics == true && (Convert.ToInt64(x.BEGIN_DATE) <= Convert.ToInt64(TmpBeginDate) && (Convert.ToInt64(x.END_DATE) >= Convert.ToInt64(TmpBeginDate) || Convert.ToInt64(x.END_DATE) == 0) || CrossUdSeq.Contains(x.UD_SEQ)))).OrderBy(x => x.BEGIN_DATE).ToList();
                IVList = GetUdOrderList.Where(x => (x.UD_TYPE == "IV" && x.IsAnalgesics == true && (Convert.ToInt64(x.BEGIN_DATE) <= Convert.ToInt64(TmpBeginDate) && (Convert.ToInt64(x.END_DATE) >= Convert.ToInt64(TmpBeginDate) || Convert.ToInt64(x.END_DATE) == 0) || CrossUdSeq.Contains(x.UD_SEQ)))).OrderBy(x => x.DRUG_TYPE).ThenBy(x => x.BEGIN_DATE).ToList();

                PRNDt = this.cm.CurrentMedData(string.Join("','", PRNList.Select(x => x.UD_SEQ).ToArray()), pStartDate, ptinfo.FeeNo);
                IVDt = this.cm.CurrentMedData(string.Join("','", IVList.Select(x => x.UD_SEQ).ToArray()), pStartDate, ptinfo.FeeNo);
                if (MedDt != null && MedDt.Rows.Count > 0)
                {//LONG=長期  //Start=臨時
                    LongList = GetUdOrderList.Where(x => x.UD_TYPE == "R" && x.IsAnalgesics == true && CrossUdSeq.Contains(x.UD_SEQ)).OrderBy(x => x.BEGIN_DATE).ToList();
                    StatList = GetUdOrderList.Where(x => x.UD_TYPE == "S" && x.IsAnalgesics == true && CrossUdSeq.Contains(x.UD_SEQ)).OrderBy(x => x.BEGIN_DATE).ToList();
                    // IVList = GetUdOrderList.Where(x => x.UD_TYPE == "IV" && CrossUdSeq.Contains(x.UD_SEQ)).OrderBy(x => x.DRUG_TYPE).ThenBy(x => x.BEGIN_DATE).ToList();

                    LongDt = this.cm.CurrentMedData(string.Join("','", LongList.Select(x => x.UD_SEQ).ToArray()), pStartDate, ptinfo.FeeNo);
                    StatDt = this.cm.CurrentMedData(string.Join("','", StatList.Select(x => x.UD_SEQ).ToArray()), pStartDate, ptinfo.FeeNo);
                }
            }

            ViewBag.LongList = LongList.OrderBy(x => x.UD_PATH).OrderBy(x => x.UD_CIR).OrderBy(x => x.DC_FLAG).ToList();
            ViewBag.PRNList = PRNList.OrderBy(x => x.UD_PATH).OrderBy(x => x.UD_CIR).OrderBy(x => x.DC_FLAG).ToList();
            //ViewBag.PRNList = PRNList.OrderBy(x => x.DC_FLAG).OrderBy(x => x.UD_CIR).OrderBy(x => x.UD_PATH).ToList();
            ViewBag.StatList = StatList.OrderBy(x => x.UD_PATH).OrderBy(x => x.UD_CIR).OrderBy(x => x.DC_FLAG).ToList();
            ViewBag.IVList = IVList.OrderBy(x => x.UD_PATH).OrderBy(x => x.UD_CIR).OrderBy(x => x.DC_FLAG).ToList();

            ViewBag.LongDt = LongDt;
            ViewBag.PRNDt = PRNDt;
            ViewBag.StatDt = StatDt;
            ViewBag.IVDt = IVDt;
            ViewData["late_reason"] = this.cd.getSelectItem("common_medication", "late_reason");

            //ViewData["insulin_section"] = this.cd.getSelectItem("common_medication", "insulin_section");
            //胰島素 排除禁打部位
            List<SelectListItem> InsulinDdl = this.cd.getSelectItem("common_medication", "insulin_section");
            DataTable InsulinBanDB = this.cm.InsulinBanOrLastPosition(ptinfo.FeeNo, "SPECIALDRUG_SET");
            string[] BanPositionList = null;
            if (InsulinBanDB != null && InsulinBanDB.Rows.Count > 0)
            {
                BanPositionList = InsulinBanDB.Rows[0]["BAN"].ToString().Split(',');
                foreach (string r in BanPositionList)
                {
                    for (int i = 0; i <= InsulinDdl.Count - 1; i++)
                    {
                        if (InsulinDdl[i].Value == r)
                        {
                            InsulinDdl.RemoveAt(i);
                            continue;
                        }
                    }
                }
            }
            ViewData["insulin_section"] = InsulinDdl;
            //胰島素 最後施打部位
            DataTable InsulinLastPositionDB = this.cm.InsulinBanOrLastPosition(ptinfo.FeeNo, "NIS_MED_INSULIN");
            string InsulinLastPosition = "";
            if (InsulinLastPositionDB != null && InsulinLastPositionDB.Rows.Count > 0)
            {
                InsulinLastPosition = InsulinLastPositionDB.Rows[0]["POSITION"].ToString();
            }

            ViewBag.InsulinLastPosition = InsulinLastPosition;
            ViewBag.pStartDate = pStartDate;
        }
        [HttpPost]
        public ActionResult QueryMedData(string pStartDate, string type = "")
        {
            pStartDate = (pStartDate + "").Trim();
            try
            {
                //有傳值進來就嘗試轉轉看
                if (!String.IsNullOrEmpty(pStartDate))
                {
                    pStartDate = Convert.ToDateTime(pStartDate).ToString("yyyy/M/d");
                }
            }
            catch (Exception e)
            {
                //Do nothing
            }

            //若沒有傳值進來或轉失敗則時間預設為今天的日期
            if (String.IsNullOrEmpty(pStartDate))
            {
                pStartDate = DateTime.Now.ToString("yyyy/M/d");
            }

            //pStartDate = !String.IsNullOrEmpty(pStartDate) ? Convert.ToDateTime(pStartDate).ToString("yyyy/M/d") : DateTime.Now.ToString("yyyy/M/d");
            if (type == "pain")
            {
                this.SetMedicationTimePain(pStartDate);
            }
            else
            {
                this.SetMedicationTime(pStartDate);
            }
            return PartialView("Medication_Result");
        }

        [HttpPost]
        [ValidateInput(false)]
        public JsonResult MedInsert(FormCollection pForm, string StartDate, string TxtReplenishDate, string TxtReplenishTime, Boolean Replenish)
        //TxtReplenishDate 補登日期,  TxtReplenishTime 補登時間, Replenish 是否勾選補登
        {
            string CareRecordID = "";
            string InsulinID = "";
            string SeqArray = pForm["HfMedData"] ?? "";
            if (SeqArray == "") return Json("N");
            string[] KeyCollection = SeqArray.Split('|');
            string[] TemPK = SeqArray.Split('|');
            List<DBItem> updList = new List<DBItem>();
            List<DBItem> insulinList = new List<DBItem>();
            int effRow = 0;
            /*             
             *非PRN中：'Hf_' + UD_SEQPK => 存的是{給藥日期|給藥時間|醫囑備註|給予劑量|未給藥原因代碼|延遲、提早原因代碼|備註|是否帶護理紀錄(是:1，否:0)|是否有填註記(是:1，否:0)|醫囑囑型(R:長期;P:PRN;S:臨時;IV:點滴)|未給藥原因其他|延遲、提早原因其他|給藥類型(提早:WhiteEarlyBox、延遲:RedBox)}
             *         'Hf_Care_' + UD_SEQPK => 存的是{備註|給予劑量|途徑|頻次|未給藥原因中文|藥名|延遲原因中文|藥劑單位}
             *         'Hf_Insulin_' + UD_SEQPK => 存的是{藥品分類|胰島素施打部位}
             *PRN中：'Hf_' + UD_SEQPK => 存的是{備註|預計給藥時間的hour|是否有填註記(是:1，否:0)|醫囑囑型(R:長期;P:PRN;S:臨時;IV:點滴)|給藥類型(提早:WhiteEarlyBox、延遲:RedBox)}
             *       'Hf_Care_' + UD_SEQPK => 存的是{備註|劑量|途徑|頻次|藥名|醫囑備註}
             *       'Hf_Insulin_' + UD_SEQPK => 存的是{藥品分類(R:長期;P:PRN;S:臨時;Y:胰島素;SC:皮下注射)|胰島素施打部位}
             */
            try
            {
                for (int i = 0; i < KeyCollection.Length; i++)
                {
                    string Key = pForm["Hf_" + KeyCollection[i]];
                    string[] AnsArray = Key.Split('|');
                    string TmpIV = pForm["Hf_Insulin_" + KeyCollection[i]] ?? "";
                    string[] TmpInsulinData = TmpIV.Split('|');
                    string TmpData = pForm["Hf_Care_" + KeyCollection[i]] ?? "";
                    string[] CareCollection = TmpData.Split('|');
                    string TmpDoubleCheck = pForm["Hf_DoubleCheck_" + KeyCollection[i]] ?? "";
                    string[] TempCheck = TmpDoubleCheck.Split('|');
                    bool IsRemark = false;
                    bool IsPRN = false;
                    string EXEC_DATE = "";//實際給藥時間

                    string DrugType = AnsArray[AnsArray.Length - 1];
                    // if (AnsArray.Length <= 5 || (TmpInsulinData != null && TmpInsulinData[0] == "P")) IsPRN = true;                    
                    if (AnsArray != null && AnsArray.Length > 0)
                    {
                        //20160929 mod by yungchen 修改判斷prn的條件
                        if (AnsArray[3] == "P" || (TmpInsulinData != null && TmpInsulinData[0] == "P")) IsPRN = true;

                        if ((IsPRN && AnsArray[2] == "1") || (!IsPRN && AnsArray[8] == "1"))
                            IsRemark = true;
                        else if (IsPRN && AnsArray[8] == "1" && AnsArray[9] == "IV")
                        {  //IV 因原本帶的字串就是非 PRN ，故就算是 PRN 也走非 PRN 的儲存字串。
                            IsRemark = true;
                            IsPRN = false;  //雖然是 PRN ，但因字串組成的關係，必需走非 PRN 的程式
                        }
                    }
                    else
                        return Json("N");

                    if (IsRemark)
                    {
                        //有填註記
                        if (!IsPRN)
                        {
                            EXEC_DATE = AnsArray[0] + " " + AnsArray[1] + ":00";
                            updList.Add(new DBItem("USE_DOSE", AnsArray[3], DBItem.DBDataType.String));
                            updList.Add(new DBItem("EXEC_DATE", AnsArray[0] + " " + AnsArray[1] + ":00", DBItem.DBDataType.DataTime));
                            updList.Add(new DBItem("REASONTYPE", AnsArray[4], DBItem.DBDataType.String));
                            updList.Add(new DBItem("NON_DRUG_OTHER", AnsArray[10], DBItem.DBDataType.String));
                            if (DrugType == "RedBox")
                                updList.Add(new DBItem("REASON", AnsArray[5], DBItem.DBDataType.String));
                            else if (DrugType == "WhiteEarlyBox")
                                updList.Add(new DBItem("EARLY_REASON", AnsArray[5], DBItem.DBDataType.String));
                            updList.Add(new DBItem("DRUG_OTHER", AnsArray[11], DBItem.DBDataType.String));
                            updList.Add(new DBItem("DESCRIPTION", AnsArray[2], DBItem.DBDataType.String));
                            updList.Add(new DBItem("REMARK", AnsArray[6], DBItem.DBDataType.String));
                        }
                        else
                        {
                            EXEC_DATE = AnsArray[4] + " " + AnsArray[5] + ":00";
                            updList.Add(new DBItem("USE_DOSE", CareCollection[1], DBItem.DBDataType.String));
                            updList.Add(new DBItem("EXEC_DATE", AnsArray[4] + " " + AnsArray[5] + ":00", DBItem.DBDataType.DataTime));//20160929 mod by yungchen 應該不是抓現在時間
                                                                                                                                      // updList.Add(new DBItem("EXEC_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                            updList.Add(new DBItem("REMARK", AnsArray[0], DBItem.DBDataType.String));
                        }
                        updList.Add(new DBItem("EXEC_ID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                        updList.Add(new DBItem("EXEC_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    }
                    else
                    {
                        //沒填註記
                        EXEC_DATE = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                        updList.Add(new DBItem("EXEC_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                        //沒填註記時間是now沒錯 by yungchen mod 2016/10/17
                        // updList.Add(new DBItem("EXEC_DATE", AnsArray[4] + " " + AnsArray[5] + ":00", DBItem.DBDataType.DataTime));//20160929 mod by yungchen 應該不是抓現在時間
                        updList.Add(new DBItem("EXEC_ID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                        updList.Add(new DBItem("EXEC_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    }
                    string Id = "";
                    if (!IsPRN || KeyCollection[i].IndexOf("TmpPRN") < 0)
                    {//非 PRN
                        if (KeyCollection[i].IndexOf("TmpIV") < 0)
                            Id = KeyCollection[i];
                        else
                            Id = KeyCollection[i].Split('_')[1];  //IV 和 PRN 給藥模式一樣，故 id 取值方式相同
                    }
                    else
                        Id = KeyCollection[i].Split('_')[1];
                    //注射部位存檔====
                    if (TmpInsulinData != null && TmpInsulinData[0] == "Y")//20160929 mod by yungchen 增加存檔注射部位
                    {
                        updList.Add(new DBItem("INSULIN_SITE", TmpInsulinData[2], DBItem.DBDataType.String));//注射部位
                    }
                    //雙人覆核存檔=======
                    if (TempCheck[0] == "Y" && TempCheck[1] != "" && TempCheck[2] != "")
                    {
                        updList.Add(new DBItem("CHECKER_ID", TempCheck[1], DBItem.DBDataType.String));
                        updList.Add(new DBItem("CHECKER", TempCheck[2], DBItem.DBDataType.String));
                        updList.Add(new DBItem("CHECKER_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                    }


                    updList.Add(new DBItem("BED_NO", ptinfo.BedNo, DBItem.DBDataType.String));
                    updList.Add(new DBItem("RECORD_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));//現在時間
                    //雙人覆核存檔=======
                    if (KeyCollection[i].IndexOf("TmpPRN") >= 0 || KeyCollection[i].IndexOf("TmpIV") >= 0)
                    {
                        //藥品代碼
                        string HidMedInfo = pForm["Hid_MedInfo_" + KeyCollection[i]];
                        string[] MedInfoArray = HidMedInfo.Split('|');

                        //寫入一筆給藥紀錄                        
                        string MedPk = "";
                        string DrugDateTmp = "";
                        if (KeyCollection[i].IndexOf("TmpPRN") >= 0)
                        {//PRN
                            //20161007 mod by yungchen 修改24點的時候 紀錄錯天的問題
                            //string DrugTimeConvert = AnsArray[1] != "24" ? AnsArray[1] : "0";
                            //DrugDateTmp = Convert.ToDateTime(StartDate + " " + DrugTimeConvert + ":00:00").ToString("yyyy/M/d tt hh:mm:ss");

                            string DrugTimeConvert = AnsArray[1] != "24" ? AnsArray[1] : "0";
                            DrugDateTmp = Convert.ToDateTime(StartDate + " " + DrugTimeConvert + ":00:00").ToString("yyyy/M/d tt hh:mm:ss");
                            if (AnsArray[1] == "24")
                            { DrugDateTmp = Convert.ToDateTime(StartDate + " " + DrugTimeConvert + ":00:00").AddDays(1).ToString("yyyy/M/d tt hh:mm:ss"); }
                            MedPk = "P" + Id;
                        }
                        else
                        {//IV
                            string DrugTimeConvert = KeyCollection[i].Split('_')[2] != "24" ? KeyCollection[i].Split('_')[2] : "0";
                            DrugDateTmp = Convert.ToDateTime(StartDate + " " + DrugTimeConvert + ":00:00").ToString("yyyy/M/d tt hh:mm:ss");
                            if (AnsArray[2] == "24")
                            { DrugDateTmp = Convert.ToDateTime(StartDate + " " + DrugTimeConvert + ":00:00").AddDays(1).ToString("yyyy/M/d tt hh:mm:ss"); }
                            MedPk = "V" + Id;
                        }

                        MedPk += DateTime.Now.ToString("MMddHHmmss") + i;
                        updList.Add(new DBItem("FEE_NO", ptinfo.FeeNo, DBItem.DBDataType.String));
                        updList.Add(new DBItem("UD_SEQPK", MedPk, DBItem.DBDataType.String));
                        updList.Add(new DBItem("UD_SEQ", Id, DBItem.DBDataType.String));
                        updList.Add(new DBItem("DRUG_DATE", DrugDateTmp, DBItem.DBDataType.String));
                        updList.Add(new DBItem("MED_CODE", MedInfoArray[0], DBItem.DBDataType.String));
                        TemPK[i] = MedPk;//將 PRN 跟 點滴 的PK換成資料庫裏面的
                        if (TempCheck[0] == "Y" && TempCheck[1] != "" && TempCheck[2] != "")
                        {
                            log.saveLogMsg("====DBExecInsert====，Hf_DoubleCheck_" + KeyCollection[i] + "：" + pForm["Hf_DoubleCheck_" + KeyCollection[i]] + "，updlist_json：" + JsonConvert.SerializeObject(updList) + "，where：" + string.Format("UD_SEQPK='{0}' and fee_no ='{1}'", Id, ptinfo.FeeNo), "MedInsert_checkid");
                        }
                        else
                        {
                            log.saveLogMsg("====DBExecInsert====，Hf_DoubleCheck_" + KeyCollection[i] + "：" + pForm["Hf_DoubleCheck_" + KeyCollection[i]] + "，updlist_json：" + JsonConvert.SerializeObject(updList) + "，where：" + string.Format("UD_SEQPK='{0}' and fee_no ='{1}'", Id, ptinfo.FeeNo), "MedInsert_checkid_null");
                        }

                        effRow += this.link.DBExecInsert("DRUG_EXECUTE", updList);

                        CareRecordID = MedPk + ptinfo.FeeNo;
                        InsulinID = MedPk;

                    }
                    else
                    {//2017/09/19 給藥儲存錯誤新增Where = Feeno的判斷條件
                        if (TempCheck[0] == "Y" && TempCheck[1] != "" && TempCheck[2] != "")
                        {
                            log.saveLogMsg("====DBExecUpdate====，Hf_DoubleCheck_" + KeyCollection[i] + "：" + pForm["Hf_DoubleCheck_" + KeyCollection[i]] + "，updlist_json：" + JsonConvert.SerializeObject(updList) + "，where：" + string.Format("UD_SEQPK='{0}' and fee_no ='{1}'", Id, ptinfo.FeeNo), "MedInsert_checkid");
                        }
                        else
                        {
                            log.saveLogMsg("====DBExecUpdate====，Hf_DoubleCheck_" + KeyCollection[i] + "：" + pForm["Hf_DoubleCheck_" + KeyCollection[i]] + "，updlist_json：" + JsonConvert.SerializeObject(updList) + "，where：" + string.Format("UD_SEQPK='{0}' and fee_no ='{1}'", Id, ptinfo.FeeNo), "MedInsert_checkid_null");
                        }

                        effRow += this.link.DBExecUpdate("DRUG_EXECUTE", updList, string.Format("UD_SEQPK='{0}' and fee_no ='{1}'", Id, ptinfo.FeeNo));

                        CareRecordID = Id + ptinfo.FeeNo;
                        InsulinID = Id;
                    }

                    //胰島素有給藥才寫入胰島素注射表
                    if (CareCollection[1] != null && CareCollection[1] != "")
                        if (TmpInsulinData != null && TmpInsulinData[0] == "Y")
                        {
                            //寫入胰島素注射部位部位
                            insulinList.Add(new DBItem("INID", InsulinID, DBItem.DBDataType.String));// 避免重複自己抓自己的ID
                            insulinList.Add(new DBItem("FEENO", ptinfo.FeeNo, DBItem.DBDataType.String));
                            if (Replenish)
                            {
                                //TxtReplenishDate 補登日期,  TxtReplenishTime 補登時間, Replenish 是否勾選補登
                                insulinList.Add(new DBItem("INDATE", TxtReplenishDate + " " + TxtReplenishTime, DBItem.DBDataType.String));
                            }
                            else
                            {
                                insulinList.Add(new DBItem("INDATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.String));
                            }
                            insulinList.Add(new DBItem("POSITION", TmpInsulinData[1], DBItem.DBDataType.String));
                            insulinList.Add(new DBItem("INJECTION", userinfo.EmployeesName, DBItem.DBDataType.String));
                            insulinList.Add(new DBItem("INSDT", AnsArray[4] + " " + AnsArray[5] + ":00", DBItem.DBDataType.String));
                            insulinList.Add(new DBItem("INSOP", userinfo.EmployeesNo, DBItem.DBDataType.String));
                            insulinList.Add(new DBItem("INSOPNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                            insulinList.Add(new DBItem("MODDT", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                            insulinList.Add(new DBItem("MODOP", userinfo.EmployeesNo, DBItem.DBDataType.String));
                            insulinList.Add(new DBItem("MODNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                            if (!IsPRN)
                            {
                                insulinList.Add(new DBItem("IN_DRUGNAME", CareCollection[5], DBItem.DBDataType.String));
                                insulinList.Add(new DBItem("IN_DOSE", AnsArray[3], DBItem.DBDataType.String));
                                insulinList.Add(new DBItem("IN_ROUTE", CareCollection[2], DBItem.DBDataType.String));
                                if (DrugType == "RedBox")
                                    insulinList.Add(new DBItem("REASON", AnsArray[5], DBItem.DBDataType.String));
                                else if (DrugType == "WhiteEarlyBox")
                                    insulinList.Add(new DBItem("EARLY_REASON", AnsArray[5], DBItem.DBDataType.String));
                                insulinList.Add(new DBItem("REASONTYPE", AnsArray[4], DBItem.DBDataType.String));
                                insulinList.Add(new DBItem("NON_DRUG_OTHER", AnsArray[10], DBItem.DBDataType.String));
                                insulinList.Add(new DBItem("DRUG_OTHER", AnsArray[11], DBItem.DBDataType.String));
                            }
                            else
                            {
                                insulinList.Add(new DBItem("IN_DRUGNAME", CareCollection[4], DBItem.DBDataType.String));
                                insulinList.Add(new DBItem("IN_DOSE", CareCollection[1], DBItem.DBDataType.String));
                                insulinList.Add(new DBItem("IN_ROUTE", CareCollection[2], DBItem.DBDataType.String));
                            }
                            effRow += this.link.DBExecInsert("NIS_MED_INSULIN", insulinList);
                            insulinList.Clear();
                        }

                    if (effRow > 0)
                    {
                        if (IsPRN || (!IsPRN && AnsArray[7] == "1" && (AnsArray[4] != "" || AnsArray[9] != "R")))
                        {//20160930 mod by yungchen 囑型(AnsArray[9])不等於R(長期醫囑) 判斷有沒有要帶護理紀錄
                         //拋轉護理紀錄            

                            if (TmpData != null && TmpData != "" && TmpData.Substring(TmpData.ToString().Length - 1).ToString() == "Y")
                            {
                                string RecordContent = "";
                                string Title = "";
                                string tmp = string.Empty;
                                if (CareCollection[4] == "其他")
                                {
                                    //2021/10/27 ECK提出當原因為其他時，護理紀錄不要呈現"其他"字樣
                                    /*tmp += CareCollection[4];
                                    tmp += "("+AnsArray[10]+")";*/
                                    tmp += AnsArray[10];
                                }
                                else
                                {
                                    tmp = CareCollection[4];
                                }
                                if (IsPRN)
                                {
                                    RecordContent = string.Format("病人因{0}，依醫囑給予左列藥物{1} {2} {3} {4}，持續監測病人反應及有無產生不良反應。", CareCollection[0], CareCollection[1], CareCollection[6], CareCollection[2], CareCollection[3]);
                                    // Title = "PRN " + CareCollection[4];
                                    Title = CareCollection[4];
                                    base.Insert_CareRecord(AnsArray[4] + " " + AnsArray[5], CareRecordID, Title, "", "", "", RecordContent, "", "drug_execute");
                                }
                                else if (TmpData[0].ToString() != "")
                                {
                                    if (AnsArray[4] != "")
                                    {
                                        RecordContent = string.Format("病人因{0}，{1} {2} {3} {4} 未執行。", tmp, CareCollection[5], CareCollection[1], CareCollection[2], CareCollection[3]);
                                        Title = "未給藥";
                                    }
                                    else //if (AnsArray[9] == "S")//--20160930有打勾的都帶護理紀錄
                                    {
                                        RecordContent = string.Format("病人因{0}，依醫囑給予左列藥物{1} {2} {3} {4}，持續監測病人反應及有無產生不良反應。", CareCollection[0], CareCollection[1], CareCollection[7], CareCollection[2], CareCollection[3]);
                                        // Title = "STAT " + CareCollection[5];
                                        Title = CareCollection[5];
                                    }
                                }
                                else
                                {
                                    if (AnsArray[4] != "")
                                    {
                                        RecordContent = string.Format("病人因{0}，{1} {2} {3} {4} 未執行。", tmp, CareCollection[5], CareCollection[1], CareCollection[2], CareCollection[3]);
                                        Title = "未給藥";
                                    }
                                    else //if (AnsArray[9] == "S")//--20160930有打勾的都帶護理紀錄
                                    {
                                        RecordContent = string.Format("依醫囑給予左列藥物 {0} {1} {2} {3}，持續監測病人反應及有無產生不良反應。", CareCollection[1], CareCollection[7], CareCollection[2], CareCollection[3]);
                                        // Title = "STAT " + CareCollection[5];
                                        Title = CareCollection[5];
                                    }
                                }
                                //base.Insert_CareRecord(DateTime.Now.ToString("yyyy/MM/dd HH:mm"), CareRecordID, Title, "", "", "", RecordContent, "", "drug_execute");
                                if (!IsPRN)
                                    base.Insert_CareRecord(AnsArray[0] + " " + AnsArray[1], CareRecordID, Title, "", "", "", RecordContent, "", "drug_execute");

                            }
                        }
                    }
                    else
                        return Json("N");

                    Key = null;
                    AnsArray = null;
                    TmpIV = null;
                    TmpInsulinData = null;
                    TmpData = null;
                    CareCollection = null;
                    Id = null;
                    updList.Clear();
                }//end for

                //給藥簽章
                #region --簽章--
                byte[] allergen = webService.GetAllergyList(ptinfo.FeeNo);
                string ptJsonArr = string.Empty;
                string allergyDesc = string.Empty;
                DateTime NowTime = DateTime.Now;
                if (allergen != null)
                {
                    ptJsonArr = CompressTool.DecompressString(allergen);
                }
                List<NIS.Data.PatientInfo> patList = JsonConvert.DeserializeObject<List<NIS.Data.PatientInfo>>(ptJsonArr);

                if (allergen != null)
                {
                    allergyDesc = patList[0].AllergyDesc;
                }
                DataTable dt;
                DRUG_EXECUTE DE = new DRUG_EXECUTE();
                List<UdOrder> GetUdOrderList = new List<UdOrder>();
                string SQLstr = "Select * from DRUG_EXECUTE where fee_no='" + ptinfo.FeeNo + "' and UD_SEQPK in ('" + string.Join("','", TemPK) + "')";
                dt = cm.getDt(SQLstr);
                dt.Columns.Add("DR_NAME");
                dt.Columns.Add("MED_CIR");
                dt.Columns.Add("MED_PATH");
                dt.Columns.Add("NOTE");
                byte[] doByteCode = webService.GetUdOrder(ptinfo.FeeNo, "A");
                string doJsonArr = "";
                if (doByteCode != null)
                {
                    doJsonArr = CompressTool.DecompressString(doByteCode);
                    GetUdOrderList = JsonConvert.DeserializeObject<List<UdOrder>>(doJsonArr);
                }
                foreach (DataRow r in dt.Rows)
                {
                    r["DR_NAME"] = GetUdOrderList.Where(x => x.UD_SEQ.ToString().Trim() == r["UD_SEQ"].ToString().Trim()).First().MED_DESC.ToString();
                    r["MED_CIR"] = GetUdOrderList.Where(x => x.UD_SEQ.ToString().Trim() == r["UD_SEQ"].ToString().Trim()).First().UD_CIR.ToString();
                    r["MED_PATH"] = GetUdOrderList.Where(x => x.UD_SEQ.ToString().Trim() == r["UD_SEQ"].ToString().Trim()).First().UD_PATH.ToString();
                    string temp = "";

                    if (!string.IsNullOrWhiteSpace(r["CHECKER"].ToString()))
                        temp = string.Format("覆核者姓名：{0}", r["CHECKER"] + "，");

                    if (!string.IsNullOrWhiteSpace(r["INSULIN_SITE"].ToString()))
                        temp += string.Format("注射部位：{0}", r["INSULIN_SITE"] + "，");

                    if (!string.IsNullOrWhiteSpace(r["REASONTYPE"].ToString()))
                        temp += string.Format("未給予：{0}" + "，", r["REASONTYPE"].ToString().Trim() != "其他" ? r["REASONTYPE"] : r["NON_DRUG_OTHER"]);
                    else if (!string.IsNullOrWhiteSpace(r["REASON"].ToString()))
                        temp += string.Format("延遲給藥：{0}" + "，", r["REASON"].ToString().Trim() != "其他" ? r["REASON"] : r["DRUG_OTHER"]);
                    else if (!string.IsNullOrWhiteSpace(r["EARLY_REASON"].ToString()))
                        temp += string.Format("提早給藥：{0}" + "，", r["EARLY_REASON"].ToString().Trim() != "其他" ? r["EARLY_REASON"] : r["DRUG_OTHER"]);

                    if (!string.IsNullOrWhiteSpace(r["REMARK"].ToString()))
                        temp += string.Format("備註：{0}", r["REMARK"] + "，");

                    r["NOTE"] = temp.TrimEnd('，');

                    string xml = cm.Med_Get_xml(ptinfo.PatientName, ptinfo.ChartNo,
                    ptinfo.PatientGender, (ptinfo.Age).ToString(), ptinfo.BedNo, ptinfo.InDate.ToString("yyyyMMdd"),
                    ptinfo.InDate.ToString("HHmm"), allergyDesc, userinfo.EmployeesName, r["DRUG_DATE"].ToString(), Convert.ToDateTime(r["EXEC_DATE"].ToString()).ToString("yyyy/MM/dd HH:mm"),
                    r["DR_NAME"].ToString(), r["USE_DOSE"].ToString(), r["MED_CIR"].ToString(), r["MED_PATH"].ToString(), r["NOTE"].ToString());

                    #region --EMR--
                    //取得應簽章人員
                    byte[] listByteCode = webService.UserName(userinfo.Guider);
                    string listJsonArray = CompressTool.DecompressString(listByteCode);
                    UserInfo user_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                    string RecordTime = NowTime.ToString("yyyy/MM/dd HH:mm:ss");
                    string Temp_NowTime_Str = Convert.ToDateTime(RecordTime).ToString("yyyyMMddHHmmss");//時間採統一變數
                    string EmrXmlString = this.get_xml(
                        NowTime.ToString("yyyyMMddHHmmss.fffffff"), Temp_NowTime_Str + GetMd5Hash(r["UD_SEQPK"].ToString() + ptinfo.FeeNo), "A000018", Convert.ToDateTime(NowTime).ToString("yyyyMMdd"),
                        GetMd5Hash(r["UD_SEQPK"].ToString() + ptinfo.FeeNo), Convert.ToDateTime(NowTime).ToString("yyyyMMdd"), "", "",
                        user_info.EmployeesNo, user_info.EmployeesName, user_info.UserID, ptinfo.ChartNo, ptinfo.PatientName,
                        ptinfo.PatientID, ptinfo.PayInfo,
                        "C:\\EMR\\", "A000018" + GetMd5Hash(r["UD_SEQPK"].ToString() + ptinfo.FeeNo) + Temp_NowTime_Str + ".xml", listJsonArray, "MedInsert"
                        );
                    SaveEMRLogData(r["UD_SEQPK"].ToString() + ptinfo.FeeNo, GetMd5Hash(r["UD_SEQPK"].ToString() + ptinfo.FeeNo), "EMR", RecordTime, "A000018" + GetMd5Hash(r["UD_SEQPK"].ToString() + ptinfo.FeeNo) + Temp_NowTime_Str, xml);
                    SaveEMRLogData(r["UD_SEQPK"].ToString() + ptinfo.FeeNo, GetMd5Hash(r["UD_SEQPK"].ToString() + ptinfo.FeeNo), "Temp", RecordTime, Temp_NowTime_Str + "-" + GetMd5Hash(r["UD_SEQPK"].ToString() + ptinfo.FeeNo), EmrXmlString);

                    #endregion
                }
                #endregion
            }
            catch (Exception ex)
            {//寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return Json("N");
            }
            return Json("Y");
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult RenderChecksRights(FormCollection pForm)
        {
            List<DRUG_EXECUTE> DRUG_EXECUTE = new List<DRUG_EXECUTE>();
            string SeqArray = pForm["HfMedData"] ?? "";
            string[] KeyCollection = SeqArray.Split('|');
            for (int i = 0; i < KeyCollection.Length; i++)
            {
                string Key = pForm["Hf_" + KeyCollection[i]];
                string[] AnsArray = Key.Split('|');
                string TmpData = pForm["Hf_Care_" + KeyCollection[i]];
                string[] CareCollection = TmpData.Split('|');
                string TmpDoubleCheck = pForm["Hf_DoubleCheck_" + KeyCollection[i]] ?? "";
                string[] TempCheck = TmpDoubleCheck.Split('|');
                string MedCode = pForm["Hid_MedInfo_" + KeyCollection[i]] ?? "";

                bool IsRemark = false;
                bool IsPRN = true;
                bool IsIV = false;

                if (AnsArray.Count() > 7)
                {
                    if (AnsArray[9] == "IV")
                    {
                        IsIV = true;
                    }
                }

                DRUG_EXECUTE DE = new DRUG_EXECUTE();
                // if (AnsArray.Length > 5) IsPRN = false;  //4 WAWA              
                if (AnsArray != null && AnsArray.Length > 0)
                {
                    if (AnsArray[3] != "P") IsPRN = false;  //mod by yungchenm
                    if ((IsPRN && AnsArray[2] == "1") || (!IsPRN && AnsArray[8] == "1"))
                        IsRemark = true;
                }
                else
                    return Json("N");

                if (IsRemark)
                {
                    //有填註記
                    if (!IsPRN)
                    {
                        //DE.DESCRIPTION = AnsArray[2]; //舊版醫囑=藥名
                        DE.MED_DESC = CareCollection[5];
                        DE.USE_DOSE = AnsArray[3].ToString().Trim() + AnsArray[3].ToString().Trim() != "" ? AnsArray[3].ToString().Trim() + " " + CareCollection[7].ToString().Trim() : "";
                        DE.EXEC_DATE = AnsArray[0] + " " + AnsArray[1] + ":00";
                        DE.REASONTYPE = CareCollection[4];
                        DE.REASON = CareCollection[6];
                        DE.REMARK = AnsArray[6];
                        DE.MED_CODE = MedCode;

                        double result = 0;
                        int count = 0;
                        if (AnsArray[3].ToString().Trim() != "")
                        {
                            result = double.Parse(AnsArray[3].ToString().Trim());
                            count = (int)Math.Ceiling(result);
                        }
                       
                        DE.DOSE_COUNT = count.ToString().Trim();

                    }
                    else
                    {
                        //DE.DESCRIPTION = CareCollection[5]; //舊版醫囑=藥名
                        DE.MED_DESC = CareCollection[4];
                        DE.USE_DOSE = CareCollection[1];
                        DE.EXEC_DATE = AnsArray[4] + " " + AnsArray[5] + ":00";
                        //DE.REASONTYPE = CareCollection[4];  //PRN 沒有未給予或延遲、提早原因
                        DE.REMARK = AnsArray[0];
                        DE.MED_CODE = MedCode;

                    }
                }
                else
                {
                    //沒填註記
                    DE.EXEC_DATE = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                    if (!IsPRN)
                    {
                        DE.USE_DOSE = AnsArray[3].ToString().Trim() + AnsArray[3].ToString().Trim() != "" ? AnsArray[3].ToString().Trim() + " " + CareCollection[7].ToString().Trim() : "";
                        DE.MED_DESC = CareCollection[5];
                    }
                    else
                    {
                        DE.USE_DOSE = CareCollection[1];
                        DE.MED_DESC = CareCollection[4];
                    }
                }
                DE.UD_PATH = CareCollection[2];
                DE.UD_CIR = CareCollection[3];
                DE.UD_SEQPK = KeyCollection[i];
                if(IsIV)
                {
                    DE.DRUG_TYPE = "V";
                }

                if (TempCheck[0] == "Y" && TempCheck[1] != "" && TempCheck[2] != "")
                {
                    DE.CHECKER_ID = TempCheck[1];
                    DE.CHECKER = TempCheck[2];
                    DE.CHECKER_DATE = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                }

                DRUG_EXECUTE.Add(DE);

                Key = null;
                AnsArray = null;
                TmpData = null;
                CareCollection = null;
                DE = null;
            }
            ViewData["drug_exec"] = DRUG_EXECUTE;
            return PartialView("Medication_3Checks5Rights");
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult RenderChecksRightsPain(FormCollection pForm)
        {
            List<DRUG_EXECUTE> DRUG_EXECUTE = new List<DRUG_EXECUTE>();
            string SeqArray = pForm["HfMedData"] ?? "";
            string[] KeyCollection = SeqArray.Split('|');
            for (int i = 0; i < KeyCollection.Length; i++)
            {
                string Key = pForm["Hf_" + KeyCollection[i]];
                string[] AnsArray = Key.Split('|');
                string TmpData = pForm["Hf_Care_" + KeyCollection[i]];
                string[] CareCollection = TmpData.Split('|');
                string TmpDoubleCheck = pForm["Hf_DoubleCheck_" + KeyCollection[i]] ?? "";
                string[] TempCheck = TmpDoubleCheck.Split('|');
                string MedCode = pForm["Hid_MedInfo_" + KeyCollection[i]] ?? "";

                bool IsRemark = false;
                bool IsPRN = true;
                bool IsIV = false;

                if(AnsArray.Count() > 7)
                {
                    if (AnsArray[9] == "IV")
                    {
                        IsIV = true;
                    }
                }



                DRUG_EXECUTE DE = new DRUG_EXECUTE();
                // if (AnsArray.Length > 5) IsPRN = false;  //4 WAWA              
                if (AnsArray != null && AnsArray.Length > 0)
                {
                    if (AnsArray[3] != "P") IsPRN = false;  //mod by yungchenm
                    if ((IsPRN && AnsArray[2] == "1") || (!IsPRN && AnsArray[8] == "1"))
                        IsRemark = true;
                }
                else
                    return Json("N");

                if (IsRemark)
                {
                    //有填註記
                    if (!IsPRN)
                    {
                        //DE.DESCRIPTION = AnsArray[2]; //舊版醫囑=藥名
                        DE.MED_DESC = CareCollection[5];
                        DE.USE_DOSE = AnsArray[3].ToString().Trim() + AnsArray[3].ToString().Trim() != "" ? AnsArray[3].ToString().Trim() + " " + CareCollection[7].ToString().Trim() : "";
                        DE.EXEC_DATE = AnsArray[0] + " " + AnsArray[1] + ":00";
                        DE.REASONTYPE = CareCollection[4];
                        DE.REASON = CareCollection[6];
                        DE.REMARK = AnsArray[6];
                        DE.MED_CODE = MedCode;

                        double result = 0;
                        int count = 0;
                        if (AnsArray[3].ToString().Trim() != "")
                        {
                            result = double.Parse(AnsArray[3].ToString().Trim());
                            count = (int)Math.Ceiling(result);
                        }
                       
                        DE.DOSE_COUNT = count.ToString().Trim();

                    }
                    else
                    {
                        //DE.DESCRIPTION = CareCollection[5]; //舊版醫囑=藥名
                        DE.MED_DESC = CareCollection[4];
                        DE.USE_DOSE = CareCollection[1];
                        DE.EXEC_DATE = AnsArray[4] + " " + AnsArray[5] + ":00";
                        //DE.REASONTYPE = CareCollection[4];  //PRN 沒有未給予或延遲、提早原因
                        DE.REMARK = AnsArray[0];
                        DE.MED_CODE = MedCode;

                    }
                }
                else
                {
                    //沒填註記
                    DE.EXEC_DATE = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                    if (!IsPRN)
                    {
                        DE.USE_DOSE = AnsArray[3].ToString().Trim() + AnsArray[3].ToString().Trim() != "" ? AnsArray[3].ToString().Trim() + " " + CareCollection[7].ToString().Trim() : "";
                        DE.MED_DESC = CareCollection[5];
                    }
                    else
                    {
                        DE.USE_DOSE = CareCollection[1];
                        DE.MED_DESC = CareCollection[4];
                    }
                }
                DE.UD_PATH = CareCollection[2];
                DE.UD_CIR = CareCollection[3];
                DE.UD_SEQPK = KeyCollection[i];
                if (IsIV)
                {
                    DE.DRUG_TYPE = "V";
                }

                if (TempCheck[0] == "Y" && TempCheck[1] != "" && TempCheck[2] != "")
                {
                    DE.CHECKER_ID = TempCheck[1];
                    DE.CHECKER = TempCheck[2];
                    DE.CHECKER_DATE = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                }

                DRUG_EXECUTE.Add(DE);

                Key = null;
                AnsArray = null;
                TmpData = null;
                CareCollection = null;
                DE = null;
            }
            ViewData["drug_exec"] = DRUG_EXECUTE;
            return PartialView("Medication_3Checks5RightsPain");
        }


        public ActionResult RenderFrequency()
        {
            List<Fre> GetFreList = new List<Fre>();
            byte[] doByteCode = webService.GetFre();
            string doJsonArr = "";
            if (doByteCode != null)
            {
                doJsonArr = CompressTool.DecompressString(doByteCode);
                GetFreList = JsonConvert.DeserializeObject<List<Fre>>(doJsonArr);
            }
            ViewBag.Fre = GetFreList;
            return PartialView("Medication_Frequency");
        }

        public ActionResult Medication_ExeRecord()
        {
            //給藥作業
            return View();
        }

        public ActionResult Med_Pharmacopoeia(string MedCode)
        {
            //藥典頁面            
            List<MedicalInfo> MedList = new List<MedicalInfo>();
            try
            {
                byte[] doByteCode = webService.GetMedicalInfo(MedCode);
                if (doByteCode != null)
                {
                    string doJsonArr = CompressTool.DecompressString(doByteCode);
                    MedList = JsonConvert.DeserializeObject<List<MedicalInfo>>(doJsonArr);
                }
            }
            catch (Exception e)
            {
                //Do nothing
            }

            ////假資料
            //MedList.Add(new MedicalInfo());
            //MedList[0].DrugCode = "藥品代碼";
            //MedList[0].DrugName = "藥品商品名";
            //MedList[0].GenericDrugs = "藥品學名";
            //MedList[0].DrugEffects = "藥品作用";
            //MedList[0].DrugSideEffects = "藥品副作用";
            //MedList[0].DrugPicPath = "藥品圖片路徑";
            //MedList[0].DrugHref = "藥品網頁路徑";

            ViewBag.MedPharmInfo = MedList;

            return View();
        }

        #endregion
        /// <summary>
        /// 轉西元年
        /// </summary>
        /// <param name="content">內容</param>
        public string trans_tcdate(string date, string time, int h)
        {
            string tctime = " 23:59";
            if (time != "")
                tctime = " " + time.Substring(0, 2) + ":" + time.Substring(2, 2);

            string tcdate = date.Substring(0, 3) + "/" + date.Substring(3, 2) + "/" + date.Substring(5, 2) + tctime;

            System.Globalization.CultureInfo tc = new System.Globalization.CultureInfo("zh-TW");
            tc.DateTimeFormat.Calendar = new System.Globalization.TaiwanCalendar();
            string stringdate = DateTime.Parse(tcdate, tc).AddHours(h).ToString("yyyyMMddHHmm");
            return stringdate;
        }

        public string getInsulinMed()
        {
            List<string> result = new List<string>();

            string sql = "SELECT DRUG_CODE FROM PHO.FORMULARYITEM WHERE DRUG_CLASS_CODE ='M02060' OR DRUG_CLASS_CODE_1 ='M02060' OR DRUG_CLASS_CODE_2 ='M02060'";
            DataTable dt = new DataTable();
            string resultStr = "";
            link.DBExecSQL(sql, ref dt);

            if (dt.Rows.Count > 0)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    string drugCode = dr["DRUG_CODE"].ToString();
                    if (drugCode != "")
                    {
                        result.Add(drugCode.Trim());
                    }
                }
            }
            resultStr = String.Join("|", result);
            return resultStr;
        }

        //取得用戶端的Client IP
        private string GetClientIp()
        {
            string ip = System.Web.HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if (string.IsNullOrEmpty(ip))
            {
                ip = System.Web.HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
            }

            return ip;
        }

        public ActionResult Med_Print(string content)
        {
            string sql = $"SELECT * FROM H_DRUG_EXECUTE WHERE UD_SEQPK = '{content.Trim()}'";
            DataTable dt = new DataTable();
            link.DBExecSQL(sql, ref dt);
            if (dt.Rows.Count > 0)
            {
                var data = dt.Rows[0];
                string feedno = data["FEE_NO"].ToString().Trim();
                byte[] doByteCode = webService.GetUdOrder(feedno, "A");
                if (doByteCode == null)
                {
                    Response.StatusCode = 400;
                    return Json(new { message = "weberror" }, JsonRequestBehavior.DenyGet);
                }
                else if (string.IsNullOrWhiteSpace(data["MED_CODE"].ToString().Trim()))
                {
                    Response.StatusCode = 400;
                    return Json(new { message = "" }, JsonRequestBehavior.DenyGet);
                }
                string doJsonArr = CompressTool.DecompressString(doByteCode);
                List<UdOrder> GetUdOrderList = JsonConvert.DeserializeObject<List<UdOrder>>(doJsonArr);
                UdOrder udOrder = GetUdOrderList.Where(w => w.MED_CODE == data["MED_CODE"].ToString().Trim() && w.UD_SEQ == data["UD_SEQ"].ToString().Trim()).FirstOrDefault();
                if (udOrder == null)
                {
                    Response.StatusCode = 400;
                    return Json(new { message = "" }, JsonRequestBehavior.DenyGet);
                }

                // 取整數做列印次數
                var Count = (int)Math.Ceiling(decimal.Parse(data["USE_DOSE"].ToString().Trim()));

                string result = PrintIVF(udOrder.MED_CODE, udOrder.UD_PATH, udOrder.MED_DESC, $"{data["USE_DOSE"].ToString().Trim()} {udOrder.UD_UNIT}", Count.ToString(), Convert.ToDateTime(data["EXEC_DATE"]).ToString("yyyy/MM/dd HH:mm:ss").Trim());
                if(result == "Y") 
                {
                    return Json(new { message = "OK" }, JsonRequestBehavior.DenyGet);
                }
            }
            Response.StatusCode = 400;
            return Json(new { message = "" }, JsonRequestBehavior.DenyGet);
        }

        //列印點滴卡
        public string PrintIVF(string code, string way, string desc, string dose, string count, string execTime)
        {
            //檢核Med_Code
            if (string.IsNullOrWhiteSpace(code.Trim()))
                return "N";

            // 此 DB 中紀錄的 ip 才可進行點滴列印
            string ip = GetClientIp();
            string sql = $"SELECT * FROM CANPRINTIVF WHERE IPADDRESS = '{ip}'";
            DataTable dt = new DataTable();
            link.DBExecSQL(sql, ref dt);
            if (dt.Rows.Count == 0)
                return "N";

            #if DEBUG
                sql = $"select DOSE_UNIT_CONVERT from PRICE where code = '{code.Trim()}' order by EFFECTIVE_DATE desc";
            #else
                sql = $"select DOSE_UNIT_CONVERT from MAST.PRICE where code = '{code.Trim()}' order by EFFECTIVE_DATE desc";
            #endif

            dt = new DataTable();
            link.DBExecSQL(sql, ref dt);
            if (dt.Rows.Count != 0)
            {
                int divisor = Convert.ToInt32(dt.Rows[0][0]);
                count = Math.Ceiling(double.Parse(count) / divisor).ToString();//無條件進位
            }

            // 預防機制
            if (int.Parse(count) >= 5)
            {
                count = "5";
            }

            string strPath = @"C:\\wkhtmltopdf\\wkhtmltopdf.exe";

            int times = int.Parse(count);

            for (int i = 0; i < times; i++)
            {
                try
                {
                    string UrlTmp = Request.Url.AbsoluteUri.Replace(Request.Url.PathAndQuery, "");
                    string AdditionalValue = "";
                    string tempPath = "";

                    //檔案暫存位置
                    tempPath = "C:\\IVF\\";
                    //檢查資料夾是否存在
                    if (Directory.Exists(tempPath + @"pdfFiles"))
                    {
                        //資料夾存在
                    }
                    else
                    {
                        //新增資料夾
                        Directory.CreateDirectory(tempPath + @"pdfFiles");
                    }

                    desc = desc.Replace('"',' ');
                    AdditionalValue += "--enable-javascript --page-width \"80mm\" --page-height \"50mm\"  --margin-top \"2mm\" --margin-bottom \"0mm\" --margin-left \"0mm\" --margin-right \"0mm\" ";
                    if (mode == "Maya")
                    {
                        UrlTmp += "/CommonMedication/PrintIVFsticker?code=" + code.Trim() + "&way=" + way + "&feeno=" + ptinfo.FeeNo + "&desc=" + desc + "&dose=" + dose + "&userName=" + userinfo.EmployeesName.ToString().Trim() + "&execTime=" + execTime;
                    }
                    else
                    {
                        UrlTmp += "/NIS/CommonMedication/PrintIVFsticker?code=" + code.Trim() + "&way=" + way + "&feeno=" + ptinfo.FeeNo + "&desc=" + desc + "&dose=" + dose + "&userName=" + userinfo.EmployeesName.ToString().Trim() + "&execTime=" + execTime;
                    }
                    tempPath += "IVF_" + ptinfo.FeeNo + "_" + DateTime.Now.ToString("yyyyMMddhhmmss") + ".pdf";
                    Process p = new Process();
                    p.StartInfo.FileName = strPath;
                    p.StartInfo.Arguments = AdditionalValue + "\"" + UrlTmp + "\" \"" + tempPath + "\" ";
                    p.StartInfo.UseShellExecute = true;
                    p.Start();
                    p.WaitForExit();

               

                    ////寫入DB LOG

                    List<DBItem> insertDataList = new List<DBItem>();
                    string serial = creatid("IVF", userinfo.EmployeesNo.ToString().Trim(), ptinfo.FeeNo, "0");

                    insertDataList.Clear();
                    insertDataList.Add(new DBItem("SERIAL", serial, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATE_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("COSTCODE", ptinfo.CostCenterCode, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));

                    int erow = link.DBExecInsert("NIS_IVF_PRINT_LOG", insertDataList);

                    //寫入PDF
                    if (erow > 0)
                    {
                        link.DBCmd.CommandText = "";
                        link.DBCmd.Parameters.Clear();
                        link.DBCmd.CommandText = "UPDATE NIS_IVF_PRINT_LOG SET  IVF_PDF = :IVF_PDF " + " WHERE SERIAL = '" + serial + "' ";

                        string filePath = tempPath;
                        byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
                        link.DBCmd.Parameters.Add(":IVF_PDF", OracleDbType.Blob).Value = fileBytes;

                        link.DBOpen();
                        link.DBCmd.ExecuteNonQuery();
                        link.DBClose();
                    }
                }
                catch (Exception ex)
                {
                    return "N";
                }
            }        
            return "Y";
        }

        //列印用畫面 點滴卡
        public ActionResult PrintIVFsticker(string code, string way, string feeno, string desc, string dose, string userName, string execTime)
        {
            string chartNO = ""; string bedno = ""; string name = "";

            //取得病人資訊
            byte[] doByteCode = webService.GetPatientInfo(feeno);
            if (doByteCode != null)
            {
                string ptinfoJosnArr = CompressTool.DecompressString(doByteCode);
                PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                ViewBag.pi = pi;
                chartNO = pi.ChartNo;
                bedno = pi.BedNo;
                name = pi.PatientName;
            }

            //產生QR Code 圖片並轉 Base64 丟到前端
            //var url = bedno + "|" + name + "|" + chartNO + "|" + desc + "|" + code;
            // 調整 QR Code 掃描資訊為病歷號 + 院內碼 + 給藥時間
            string execTimeToNumber = new string(execTime.Where(char.IsDigit).ToArray());  //排除空格與符號

            var url = chartNO + "|" + code + "|" + execTimeToNumber;
            QRCodeGenerator qRCodeGenerator = new QRCodeGenerator();
            QRCodeData data = qRCodeGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            PngByteQRCode qRCode = new PngByteQRCode(data);
            byte[] image = qRCode.GetGraphic(4);
            string qrcode = Convert.ToBase64String(image);

            // 去識別化後的病人姓名
            var maskName = "";
            //判斷病人姓名是否為中文
            if (cm.IsChinese(name))
            {
                maskName = cm.MaskChineseName(name);
            }
            else
            {
                maskName = cm.MaskEnglishName(name);
            }

            ViewBag.code = code;
            ViewBag.way = way;
            ViewBag.desc = desc;
            ViewBag.dose = dose;
            ViewBag.userName = userName;
            ViewBag.qrcode = qrcode;
            // 去識別化後的病人姓名
            ViewBag.maskName = maskName;
            ViewBag.execTime = execTime;

            return View();
        }

        //取得該藥物是否為第一次施打
        public bool isGrugFirstTime(string feeno, string drugcode)
        {
            bool result = true;

            string sql = "SELECT MED_CODE FROM DRUG_EXECUTE WHERE FEE_NO = '" + feeno.Trim() + "' AND MED_CODE ='" + drugcode.Trim() + "'";
            DataTable dt = new DataTable();
            link.DBExecSQL(sql, ref dt);

            // 因判斷的時候已經寫入DB，所以要判斷大於1才是第2次以上施打
            if (dt!= null && dt.Rows.Count > 1)
            {
                result = false;
            }
            
            return result;
        }
    }
}