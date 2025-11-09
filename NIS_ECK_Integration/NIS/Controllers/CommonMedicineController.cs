using Newtonsoft.Json;
using NIS.Controllers;
using NIS.Data;
using NIS.Models;
using NIS.UtilTool;
using NIS.WebService;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Web;
using System.Web.Mvc;


namespace NIS.Controllers
{
    public class CommonMedicineController : BaseController
    {
        private CommData cd;    //常用資料Module
        private CommonMedicine cm;
        private DBConnector link;

        public CommonMedicineController()
        {
            this.cd = new CommData();
            this.cm = new CommonMedicine();
            this.link = new DBConnector();
        }
        public class Confirm
        {
            public string Ordseq { get; set; }
            public string Remark { get; set; }
            public string ORDPROCED { get; set; }
            public string DOSE { get; set; }
            public string DOSEUNIT { get; set; }
            public string ROUTE { get; set; }
            public string FEQ { get; set; }
            public string ORDBGNDTTM { get; set; }
            public string ORDENDDTTM { get; set; }
            public string ORDSTATUS { get; set; }
            public string INSURANCE { get; set; }
            public string UDOTYPFREQN { get; set; }
            public string RECORDSTIME { get; set; }
            public string INSDT { get; set; }
            public string DRUGNAME { get; set; }
            public string COSTTIME { get; set; }
            public string RATEMEMO { get; set; }
            public string RATEL { get; set; }
            public string RATEH { get; set; }
            public string NOTE { get; set; }
            public string UDDDGNMATERIAL { get; set; }
        }
        public class drugtime
        {
            public string key { get; set; }
            public string sheetno { get; set; }
            public string givedate { get; set; }
            public string givetime { get; set; }
            public string reasontype { get; set; }
            public string reason { get; set; }
            public string insdt { get; set; }
            public string moddose { get; set; }
            public string ordstatus { get; set; }
            public string checker { get; set; }
            public string giveserial { get; set; }
            public string badreaction { get; set; }
            public string grugname { get; set; }
            public string genericdrugs { get; set; }
            public string route { get; set; }
            public string dose { get; set; }
            public string feq { get; set; }
            public string med_code { get; set; }
            public string ud_status { get; set; }
            public string use_t { get; set; }
            public string use_s { get; set; }
            public string use_d { get; set; }
        }

        // GET: /CommonMedicine/

        public ActionResult Index() {
            return View();
        }

        #region Login
        [HttpGet]
        public ActionResult Login(string name)
        {
            return View();
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Login(FormCollection form)
        {
            string checkname = "";

            byte[] uData = webService.UserLogin(form["txtid"], form["txtpwd"]);
            if (uData != null)
            {
                string jsonstr = CompressTool.DecompressString(uData);
                UserInfo user = JsonConvert.DeserializeObject<UserInfo>(jsonstr);
                checkname = user.EmployeesName.ToString().Trim();
            }
            else
            {
                ViewBag.checkname = null;
                //帳號或密碼錯誤
            }
            ViewBag.checkname = checkname;

            //  Response.Write("../CommonMedicine/Execute?name="+aa+ "");
            //Response.Write("<script>opener.fatherHello('" + aa + "');</script>");
            //return Redirect("href='javascript:opener.fatherHello('')'");

            return View();
        }
        #endregion

        #region Med_Execute 執行給藥
        [HttpGet]
        public ActionResult Med_Execute(string success)
        {
            if (Session["PatInfo"] == null)
            { return Redirect("../VitalSign/VitalSign_Single"); }

            if (Check_Session(success))
                return Redirect("../VitalSign/VitalSign_Single");

            //宣告病患_取得住院序號
            PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
            string feeno = ptInfo.FeeNo;

            //PROCESS UD FORM WEB SERVICE
            WebS_DrugOrder();

            DataTable dt_stat = new DataTable();
            DataTable dt_reg = new DataTable();
            DataTable dt_prn = new DataTable();

            DataTable dt_drug = new DataTable();
            /*    dt_drug = cm.get_drug_show("DRUG_SHOW", feeno, "", "", "A", "", "", "", "1");

                dt_stat = dt_drug;
                dt_stat.DefaultView.RowFilter = "CATEGORY='S'";
                dt_reg = dt_drug;
                dt_reg.DefaultView.RowFilter = "((CATEGORY = 'R' AND FEQ <> 'ASORDER') OR (CATEGORY = 'P' AND FEQ NOT LIKE '%PRN%'))";
                dt_prn = dt_drug;
                dt_prn.DefaultView.RowFilter = "((CATEGORY = 'P' AND FEQ LIKE '%PRN%') OR (CATEGORY = 'R' AND FEQ='ASORDER')) AND INSDT IS NULL";*/
            
            #region 取得STAT,REG,PRN 用藥
            dt_stat = cm.get_drug_show("DRUG_SHOW", feeno, "", "", "S", "", "", "", "1");
            dt_reg = cm.get_drug_show("DRUG_SHOW", feeno, "", "", "R", "", "", "", "1");
            dt_prn = cm.get_drug_show("DRUG_SHOW", feeno, "", "", "P", "", "", "", "1");

            if (dt_stat.Rows.Count > 0)
            {
                dt_stat.Columns.Add("use_t");//使用時間
                dt_stat.Columns.Add("use_s");//使用順序
                dt_stat.Columns.Add("use_d");
                foreach (DataRow dr in dt_stat.Rows)
                {
                    DataTable dtt = cm.get_drug_time("DRUG_SHOW", dr["SHEETNO"].ToString(), dr["GIVEDATE"].ToString().Trim(), "", "", "1");
                    if (dtt != null && dtt.Rows.Count > 0)
                    {
                        dr["use_s"] = dtt.Rows[0]["use_s"].ToString().TrimEnd(',');
                        dr["use_t"] = dtt.Rows[0]["use_t"].ToString().TrimEnd(',');
                        dr["use_d"] = dtt.Rows[0]["use_d"].ToString().TrimEnd(',');
                    }
                    else
                    {
                        dr["use_t"] = "now";
                        dr["use_s"] = 1;
                    }
                }
                ViewBag.dt_stat = dt_stat;
            }

            if (dt_reg.Rows.Count > 0)
            {
                dt_reg.Columns.Add("use_t");//使用時間
                dt_reg.Columns.Add("use_s");//使用順序
                dt_reg.Columns.Add("use_d");
                foreach (DataRow dr in dt_reg.Rows)
                {
                    DataTable dtt = cm.get_drug_time("DRUG_SHOW", dr["SHEETNO"].ToString(), dr["GIVEDATE"].ToString().Trim(), "", "", "1");
                    if (dtt != null && dtt.Rows.Count > 0)
                    {
                        dr["use_s"] = dtt.Rows[0]["use_s"].ToString().TrimEnd(',');
                        dr["use_t"] = dtt.Rows[0]["use_t"].ToString().TrimEnd(',');
                        dr["use_d"] = dtt.Rows[0]["use_d"].ToString().TrimEnd(',');
                    }
                    else
                    {
                        dr["use_t"] = "now";
                        dr["use_s"] = 1;
                    }
                }
                ViewBag.dt_reg = dt_reg;
            }

            if (dt_prn.Rows.Count > 0)
            {
                dt_prn.Columns.Add("use_t");//使用時間
                dt_prn.Columns.Add("use_s");//使用順序
                dt_prn.Columns.Add("use_d");
                foreach (DataRow dr in dt_prn.Rows)
                {
                    DataTable dtt = cm.get_drug_time("DRUG_SHOW", dr["SHEETNO"].ToString(), dr["GIVEDATE"].ToString().Trim(), "", "", "");
                    if (dtt != null && dtt.Rows.Count > 0)
                    {
                        dr["use_s"] = dtt.Rows[0]["use_s"].ToString().TrimEnd(',');
                        dr["use_t"] = dtt.Rows[0]["use_t"].ToString().TrimEnd(',');
                        dr["use_d"] = dtt.Rows[0]["use_d"].ToString().TrimEnd(',');
                    }
                    else
                    {
                        dr["use_t"] = "now";
                        dr["use_s"] = 1;
                    }
                }
                ViewBag.dt_prn = dt_prn;
            }
            #endregion
            return View();
            
            
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Med_Execute(FormCollection form, String val, String but, List<drugtime> data, string cker)
        {
            if (Session["PatInfo"] == null)
            {    //session過期
                return Redirect("../CommonMedicine/Med_Execute?success=overdue");
            }

            //宣告病患_取得住院序號
            PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
            string feeno = ptInfo.FeeNo;
            string start = Convert.ToDateTime(form["start_date"] + " " + form["start_time"]).ToString("yyyy/MM/dd HH:mm:ss");    //查詢_開始日期時間
            string end = Convert.ToDateTime(form["end_date"] + " " + form["end_time"]).ToString("yyyy/MM/dd HH:mm:ss");          //查詢_結束日期時間
            string udodrgcode = form["udodrgcode"];     //藥包條碼
            string get_flag;
            ViewBag.start_date = Convert.ToDateTime(form["start_date"]).ToString("yyyy/MM/dd");
            ViewBag.start_time = Convert.ToDateTime(form["start_time"]).ToString("HH:mm");
            ViewBag.end_date = Convert.ToDateTime(form["end_date"]).ToString("yyyy/MM/dd");
            ViewBag.end_time = Convert.ToDateTime(form["end_time"]).ToString("HH:mm");
            ViewBag.udodrgcode = udodrgcode;

            if (but == "quiryAll" || but == "OK")
            {
                start = "";
                end = "";
                udodrgcode = "";
            }

            #region 更新資料
            if (but != "quiry" && but != "quiryAll")
            {
                bool success_dt = true, data_ordseq = false;//預設存資料庫成功
                if (data != null) //判斷儲存勾選資料是否有無
                {
                    /*****判別是否有資料 有的話就要做更新資料庫的動作*****/

                    DataTable dt_save = new DataTable();
                    //   dt_save = cm.get_drugtable("save");
                    DataRow dr = dt_save.NewRow();
                    dt_save.Columns.Add("insdt");
                    dt_save.Columns.Add("insname");
                    dt_save.Columns.Add("reasontype");
                    dt_save.Columns.Add("reason");
                    dt_save.Columns.Add("badreaction");
                    dt_save.Columns.Add("ordstatus");
                    dt_save.Columns.Add("status");
                    dt_save.Columns.Add("checker");
                    dt_save.Columns.Add("moddose");
                    dt_save.Columns.Add("where");
                    //   dt_save.Columns.Add("givedate");
                    //   dt_save.Columns.Add("givetime");
                    //塞入datatype
                    for (int i = 0; i < data.Count; i++)
                    {
                        if (data[i].ordstatus == "0")//有打勾的
                        {
                            data_ordseq = true;
                            dr = dt_save.NewRow();
                            string where = " SHEETNO = '" + data[i].sheetno.ToString() + "' ";
                            where += "AND feeno = '" + feeno + "' ";
                            where += "AND giveserial = '" + data[i].giveserial + "' ";
                           // where += "AND givedate = '" + data[i].givedate.ToString().Trim() + "' ";
                            if (data[i].givedate != null && data[i].givedate.ToString().Trim() != "")
                            {
                                where += "AND givedate = '" + data[i].givedate.ToString().Trim() + "' ";
                            }

                            /*   ud_status = data[i].ud_status;
                               ud_cir = data[i].feq;
                               if (data[i].category == 'P' && ud_cir.IndexOf("PRN"))
                               { dr["insdt"] = Convert.ToDateTime(form["start_date"] + " " + form["start_time"]).ToString("yyyy/MM/dd HH:mm:ss"); }*/
                            dr["insdt"] = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                            dr["insname"] = userinfo.EmployeesName;
                            dr["reasontype"] = data[i].reasontype;
                            dr["reason"] = data[i].reason;
                            dr["badreaction"] = data[i].badreaction;
                            dr["ordstatus"] = data[i].ordstatus;
                            dr["checker"] = cker;
                            dr["moddose"] = data[i].moddose;
                            dr["status"] = "已給藥";
                            dr["where"] = where;
                            //       string datetime = Convert.ToDateTime(data[i].INSDT.ToString()).ToString("yyyy/MM/dd hh:mm:ss");

                            dt_save.Rows.Add(dr);
                            //===============
                            string mag = "", title = "", dose = data[i].dose;
                            if (data[i].moddose != null && data[i].moddose != "")
                            {
                                dose = data[i].moddose;
                            }
                            title = data[i].grugname + "(" + data[i].genericdrugs + ") " + dose + " " + data[i].route + "" + data[i].feq;
                            switch (data[i].reasontype)
                            {
                                case "1"://未執行
                                    mag = "病人因" + data[i].reason + "未執行" + data[i].grugname + "(" + data[i].genericdrugs + ")";
                                    break;
                                case "2"://提早執行
                                    mag = "病人因" + data[i].reason + "提早執行" + data[i].grugname + "(" + data[i].genericdrugs + ")";
                                    break;
                                case "3"://延遲執行
                                    mag = "病人因" + data[i].reason + "延遲給予" + data[i].grugname + "(" + data[i].genericdrugs + ")";
                                    break;
                                case "4"://拒絕
                                    mag = "病人因拒絕" + data[i].grugname + "(" + data[i].genericdrugs + ")";
                                    break;
                                case "5"://暫停
                                    mag = "病人因" + data[i].reason + "暫停" + data[i].grugname + "(" + data[i].genericdrugs + ")。";
                                    break;
                                case null:
                                    break;
                                default:
                                    break;

                            }
                            if (data[i].badreaction != null && data[i].badreaction != "")
                            {
                                mag += "病人於" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + "服用" + data[i].grugname + "(" + data[i].genericdrugs + ") 產生" + data[i].badreaction + "情形";
                            }
                        }
                    }

                    if (data_ordseq)
                    {
                        //確認是否有存資料 及有無成功
                        int erow = cm.upd("DRUG_SHOW", dt_save);
                        if (erow < 1)
                            success_dt = false;

                        if (success_dt)
                        {   //儲存成功
                            return Redirect("../CommonMedicine/Med_Execute?success=yes");
                        }
                        else
                        {   //儲存失敗
                            return Redirect("../CommonMedicine/Med_Execute?success=no");
                        }
                    }
                }
            }
            #endregion

            #region 取得給藥紀錄資料
            //依傳入參數決定sql
            switch (but)
            {
                case "quiry":
                    //依輸入時間查詢
                    get_flag = but;
                    break;
                case "quiryAll":
                    //查詢全部用藥
                    get_flag = but;
                    break;
                default:
                    get_flag = "1";
                    break;
            }

            DataTable dt_stat = new DataTable();
            DataTable dt_reg = new DataTable();
            DataTable dt_prn = new DataTable();
            dt_stat = cm.get_drug_show("DRUG_SHOW", feeno, "", "", "S", "", start, end, get_flag);
            dt_reg = cm.get_drug_show("DRUG_SHOW", feeno, "", "", "R", "", start, end, get_flag);
            dt_prn = cm.get_drug_show("DRUG_SHOW", feeno, "", "", "P", "", "", "", get_flag);

            #region 取得STAT給藥紀錄資料
            if (dt_stat.Rows.Count > 0)
            {
                dt_stat.Columns.Add("use_t");//使用時間
                dt_stat.Columns.Add("use_s");//使用順序
                dt_stat.Columns.Add("use_d");
                foreach (DataRow dr in dt_stat.Rows)
                {
                    DataTable dtt = cm.get_drug_time("DRUG_SHOW", dr["SHEETNO"].ToString(), dr["GIVEDATE"].ToString().Trim(), start, end, get_flag);
                    if (dtt != null && dtt.Rows.Count > 0)
                    {
                        dr["use_s"] = dtt.Rows[0]["use_s"].ToString().TrimEnd(',');
                        dr["use_t"] = dtt.Rows[0]["use_t"].ToString().TrimEnd(',');
                        dr["use_d"] = dtt.Rows[0]["use_d"].ToString().TrimEnd(',');
                    }
                }
                ViewBag.dt_stat = dt_stat;
            }
            #endregion

            #region 取得REG給藥紀錄資料
            if (dt_reg.Rows.Count > 0)
            {
                dt_reg.Columns.Add("use_t");//使用時間
                dt_reg.Columns.Add("use_s");//使用順序
                dt_reg.Columns.Add("use_d");
                foreach (DataRow dr in dt_reg.Rows)
                {
                    DataTable dtt = cm.get_drug_time("DRUG_SHOW", dr["SHEETNO"].ToString(), dr["GIVEDATE"].ToString().Trim(), start, end, get_flag);
                    if (dtt != null && dtt.Rows.Count > 0)
                    {
                        dr["use_s"] = dtt.Rows[0]["use_s"].ToString().TrimEnd(',');
                        dr["use_t"] = dtt.Rows[0]["use_t"].ToString().TrimEnd(',');
                        dr["use_d"] = dtt.Rows[0]["use_d"].ToString().TrimEnd(',');
                    }
                }
                ViewBag.dt_reg = dt_reg;
            }
            #endregion

            #region 取得PRN給藥紀錄資料
            if (dt_prn.Rows.Count > 0)
            {
                dt_prn.Columns.Add("use_t");//使用時間
                dt_prn.Columns.Add("use_s");//使用順序
                dt_prn.Columns.Add("use_d");
                foreach (DataRow dr in dt_prn.Rows)
                {
                    DataTable dtt = cm.get_drug_time("DRUG_SHOW", dr["SHEETNO"].ToString(), dr["GIVEDATE"].ToString().Trim(), start, end, get_flag);
                    if (dtt != null && dtt.Rows.Count > 0)
                    {
                        dr["use_s"] = dtt.Rows[0]["use_s"].ToString().TrimEnd(',');
                        dr["use_t"] = dtt.Rows[0]["use_t"].ToString().TrimEnd(',');
                        dr["use_d"] = dtt.Rows[0]["use_d"].ToString().TrimEnd(',');
                    }
                }
                ViewBag.dt_prn = dt_prn;
            }

            #endregion
            #endregion

            return View();
        }

        #endregion

        #region Med_QueryExecLog 查詢給藥紀錄
        [HttpGet]
        public ActionResult Med_QueryExecLog(String id)
        {
            if (Check_Session(id))
                return Redirect("../VitalSign/VitalSign_Single");

            //宣告病患_取得住院序號
            PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
            string feeno = ptInfo.FeeNo;
  
            DataTable dt_all = new DataTable();

            #region 取得全部用藥
            //dt_all = cm.get_QueryExecLog("DRUG_SHOW", feeno, "", "", "quiryAll");
            dt_all = cm.get_QueryExecLog("DRUG_SHOW", feeno, "", "", id);

            if (dt_all.Rows.Count > 0)
            {
                dt_all.Columns.Add("use_t");//使用時間
                dt_all.Columns.Add("use_s");//使用順序
                dt_all.Columns.Add("use_d");
                dt_all.Columns.Add("use_insname");
                dt_all.Columns.Add("use_insdt");
                dt_all.Columns.Add("use_reason");
                foreach (DataRow dr in dt_all.Rows)
                {
                    DataTable dtt = cm.get_QueryExecLogTime("DRUG_SHOW", dr["SHEETNO"].ToString(), "", id, "quiryAll");
                    if (dtt != null && dtt.Rows.Count > 0)
                    {
                        dr["use_s"] = dtt.Rows[0]["use_s"].ToString().TrimEnd(',');
                        dr["use_t"] = dtt.Rows[0]["use_t"].ToString().TrimEnd(',');
                        dr["use_d"] = dtt.Rows[0]["use_d"].ToString().TrimEnd(',');
                        dr["use_insname"] = dtt.Rows[0]["use_insname"].ToString().TrimEnd(',');
                        dr["use_insdt"] = dtt.Rows[0]["use_insdt"].ToString().TrimEnd(',');
                        dr["use_reason"] = dtt.Rows[0]["use_reason"].ToString().TrimEnd(',');
                    }
                }
                ViewBag.dt_all = dt_all;
            }
            #endregion
            return View();
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Med_QueryExecLog(FormCollection form, String success, String trans)
        {
            if (Check_Session(success))
                return Redirect("../VitalSign/VitalSign_Single");

            //宣告病患_取得住院序號
            PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
            string feeno = ptInfo.FeeNo;
            string start = Convert.ToDateTime(form["start_date"] + " " + form["start_time"]).ToString("yyyy/MM/dd HH:mm:ss");    //查詢_開始日期時間
            string end = Convert.ToDateTime(form["end_date"] + " " + form["end_time"]).ToString("yyyy/MM/dd HH:mm:ss");          //查詢_結束日期時間
            ViewBag.start_date = Convert.ToDateTime(form["start_date"]).ToString("yyyy/MM/dd");
            ViewBag.start_time = Convert.ToDateTime(form["start_time"]).ToString("HH:mm");
            ViewBag.end_date = Convert.ToDateTime(form["end_date"]).ToString("yyyy/MM/dd");
            ViewBag.end_time = Convert.ToDateTime(form["end_time"]).ToString("HH:mm");
            DataTable dt_all = new DataTable();

            #region 取得全部用藥
            if (trans == "quiryAll")
            { dt_all = cm.get_QueryExecLog("DRUG_SHOW", feeno, "", "", "quiryAll"); }
            else
            { dt_all = cm.get_QueryExecLog("DRUG_SHOW", feeno, start, end, "quiryInterval"); }

            if (dt_all.Rows.Count > 0)
            {
                dt_all.Columns.Add("use_t");//使用時間
                dt_all.Columns.Add("use_s");//使用順序
                dt_all.Columns.Add("use_d");
                dt_all.Columns.Add("use_insname");
                dt_all.Columns.Add("use_insdt");
                dt_all.Columns.Add("use_reason");
                foreach (DataRow dr in dt_all.Rows)
                {
                    DataTable dtt = cm.get_QueryExecLogTime("DRUG_SHOW", dr["SHEETNO"].ToString(), start, end, trans);
                    if (dtt != null && dtt.Rows.Count > 0)
                    {
                        dr["use_s"] = dtt.Rows[0]["use_s"].ToString().TrimEnd(',');
                        dr["use_t"] = dtt.Rows[0]["use_t"].ToString().TrimEnd(',');
                        dr["use_d"] = dtt.Rows[0]["use_d"].ToString().TrimEnd(',');
                        dr["use_insname"] = dtt.Rows[0]["use_insname"].ToString().TrimEnd(',');
                        dr["use_insdt"] = dtt.Rows[0]["use_insdt"].ToString().TrimEnd(',');
                        dr["use_reason"] = dtt.Rows[0]["use_reason"].ToString().TrimEnd(',');
                    }
                }
                ViewBag.dt_all = dt_all;
            }
            #endregion
            return View();
        }
        #endregion

        #region Med_ExecList 暫時沒使用
        public ActionResult Med_ExecList(string success)
        {
            if (Session["PatInfo"] != null)
            {
                // Response.Write("<script>alert('Session測試!')</script>");
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

                //宣告病患_取得住院序號
                PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
                string feeno = ptInfo.FeeNo;

                //PROCESS UD FORM WEB SERVICE
                WebS_DrugOrder();

                DataTable dt_stat = new DataTable();
                DataTable dt_reg = new DataTable();
                DataTable dt_prn = new DataTable();

                #region 取得STAT,REG,PRN 用藥
                dt_stat = cm.get_drug_show("DRUG_SHOW", feeno, "", "", "S", "", "", "", "1");
                dt_reg = cm.get_drug_show("DRUG_SHOW", feeno, "", "", "R", "", "", "", "1");
                dt_prn = cm.get_drug_show("DRUG_SHOW", feeno, "", "", "P", "", "", "", "");

                if (dt_stat.Rows.Count > 0)
                {
                    dt_stat.Columns.Add("use_t");//使用時間
                    dt_stat.Columns.Add("use_s");//使用順序
                    dt_stat.Columns.Add("use_d");
                    foreach (DataRow dr in dt_stat.Rows)
                    {
                        DataTable dtt = cm.get_drug_time("time", dr["SHEETNO"].ToString(), dr["GIVEDATE"].ToString().Trim(), "", "", "1");
                        if (dtt != null && dtt.Rows.Count > 0)
                        {
                            dr["use_s"] = dtt.Rows[0]["use_s"].ToString().TrimEnd(',');
                            dr["use_t"] = dtt.Rows[0]["use_t"].ToString().TrimEnd(',');
                            dr["use_d"] = dtt.Rows[0]["use_d"].ToString().TrimEnd(',');
                        }
                        else
                        {
                            dr["use_t"] = "now";
                            dr["use_s"] = 1;
                        }
                    }
                    ViewBag.dt_stat = dt_stat;
                }

                if (dt_reg.Rows.Count > 0)
                {
                    dt_reg.Columns.Add("use_t");//使用時間
                    dt_reg.Columns.Add("use_s");//使用順序
                    dt_reg.Columns.Add("use_d");
                    foreach (DataRow dr in dt_reg.Rows)
                    {
                        DataTable dtt = cm.get_drug_time("time", dr["SHEETNO"].ToString(), dr["GIVEDATE"].ToString().Trim(), "", "", "1");
                        if (dtt != null && dtt.Rows.Count > 0)
                        {
                            dr["use_s"] = dtt.Rows[0]["use_s"].ToString().TrimEnd(',');
                            dr["use_t"] = dtt.Rows[0]["use_t"].ToString().TrimEnd(',');
                            dr["use_d"] = dtt.Rows[0]["use_d"].ToString().TrimEnd(',');
                        }
                        else
                        {
                            dr["use_t"] = "now";
                            dr["use_s"] = 1;
                        }
                    }
                    ViewBag.dt_reg = dt_reg;
                }

                if (dt_prn.Rows.Count > 0)
                {
                    dt_prn.Columns.Add("use_t");//使用時間
                    dt_prn.Columns.Add("use_s");//使用順序
                    dt_prn.Columns.Add("use_d");
                    foreach (DataRow dr in dt_prn.Rows)
                    {
                        DataTable dtt = cm.get_drug_time("time", dr["SHEETNO"].ToString(), dr["GIVEDATE"].ToString().Trim(), "", "", "1");
                        if (dtt != null && dtt.Rows.Count > 0)
                        {
                            dr["use_s"] = dtt.Rows[0]["use_s"].ToString().TrimEnd(',');
                            dr["use_t"] = dtt.Rows[0]["use_t"].ToString().TrimEnd(',');
                            dr["use_d"] = dtt.Rows[0]["use_d"].ToString().TrimEnd(',');
                        }
                        else
                        {
                            dr["use_t"] = "now";
                            dr["use_s"] = 1;
                        }
                    }
                    ViewBag.dt_prn = dt_prn;
                }
                #endregion
                return View();
            }
            return Redirect("../VitalSign/VitalSign_Single");
        }

        #endregion

        #region GET WEBSERVICE MED DATA

        //病人用藥
        public void WebS_DrugOrder()
        {
            try
            {
                if (ptinfo.FeeNo != null)
                {
                    byte[] doByteCode = webService.GetDrugOrder(ptinfo.FeeNo);
                    string doJsonArr = CompressTool.DecompressString(doByteCode);
                    List<DrugOrder> GetDrugOrderList = JsonConvert.DeserializeObject<List<DrugOrder>>(doJsonArr);
                    ViewData["GetDrugOrderList"] = GetDrugOrderList;

                    bool data_ok = false;
                    DataTable dt_drug = new DataTable();
                    dt_drug = cm.get_drugtable("new");
                    DataRow dr_d = dt_drug.NewRow();

                    string sheetno_check = "";
                    int li_seq = 0;
                    for (int k = 0; k < GetDrugOrderList.Count; k++)
                    {
                        if (GetDrugOrderList[k].SheetNo != "" && GetDrugOrderList[k].GiveSerial != "")
                        {
                            if (cm.ck_pk(ptinfo.FeeNo, GetDrugOrderList[k].SheetNo, GetDrugOrderList[k].GiveSerial, GetDrugOrderList[k].GiveDate, GetDrugOrderList[k].DcFlag, "load"))
                            {
                                data_ok = true;
                                dr_d = dt_drug.NewRow();
                                dr_d["feeno"] = ptinfo.FeeNo;
                                dr_d["sheetno"] = GetDrugOrderList[k].SheetNo;
                                dr_d["giveserial"] = GetDrugOrderList[k].GiveSerial;
                                dr_d["dose"] = GetDrugOrderList[k].Dose;
                                dr_d["ordstatus"] = "1";
                                dr_d["doseunit"] = GetDrugOrderList[k].DoseUnit; ;
                                dr_d["category"] = GetDrugOrderList[k].Category;
                                dr_d["orderstartdate"] = GetDrugOrderList[k].OrderStartDate.ToString("yyyy/MM/dd HH:mm:ss");
                                dr_d["orderenddate"] = GetDrugOrderList[k].OrderEndDate.ToString("yyyy/MM/dd HH:mm:ss");
                                dr_d["drugname"] = GetDrugOrderList[k].DrugName;
                                dr_d["genericdrugs"] = GetDrugOrderList[k].GenericDrugs;
                                dr_d["route"] = GetDrugOrderList[k].Route;
                                dr_d["ratel"] = GetDrugOrderList[k].RateL;
                                dr_d["rateh"] = GetDrugOrderList[k].RateH;
                                dr_d["ratememo"] = GetDrugOrderList[k].RateMemo;
                                dr_d["dcflag"] = GetDrugOrderList[k].DcFlag;
                                dr_d["costtime"] = GetDrugOrderList[k].CostTime;
                                dr_d["feq"] = GetDrugOrderList[k].Feq;
                                dr_d["doublecheck"] = GetDrugOrderList[k].DoubleCheck;
                                dr_d["note"] = GetDrugOrderList[k].Note;
                                dr_d["status"] = "需給藥";
                                dr_d["med_code"] = GetDrugOrderList[k].Med_code;
                                dr_d["ud_status"] = GetDrugOrderList[k].Ud_status;
                                if (GetDrugOrderList[k].Category == "S")
                                {
                                    dr_d["givedate"] = GetDrugOrderList[k].OrderStartDate.ToString("yyyy/MM/dd");
                                    dr_d["givetime"] = GetDrugOrderList[k].OrderEndDate.ToString("HH:mm:ss");
                                }
                                else
                                {
                                    dr_d["givedate"] = GetDrugOrderList[k].GiveDate;
                                    dr_d["givetime"] = GetDrugOrderList[k].GiveTime;
                                }
                                if (sheetno_check == GetDrugOrderList[k].SheetNo)
                                {
                                    li_seq++;
                                }
                                else
                                {
                                    string sql = "";
                                    sql = "SELECT NVL(MAX(SEQ),0) SEQ FROM DRUG_SHOW WHERE FEENO = '" + ptinfo.FeeNo + "' AND SHEETNO = '" + GetDrugOrderList[k].SheetNo + "'";
                                    DataTable Dt = link.DBExecSQL(sql);
                                    if (Dt.Rows.Count > 0)
                                    {
                                        for (int i = 0; i < Dt.Rows.Count; i++)
                                        {
                                            li_seq = Convert.ToInt16(Dt.Rows[i]["SEQ"].ToString());
                                            li_seq++;
                                        }
                                    }
                                    sheetno_check = GetDrugOrderList[k].SheetNo;
                                }
                                dr_d["seq"] = Convert.ToString(li_seq).PadLeft(5, '0');
                                dt_drug.Rows.Add(dr_d);
                            }
                        }
                    }
                    if (data_ok)
                    {
                        int erow = cm.insert("DRUG_SHOW", dt_drug);
                        //   data_ordseq = false;
                    }
                    GetDrugOrderList = null;
                }
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString());
            }
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
                string doJsonArr = CompressTool.DecompressString(doByteCode);
                Response.Write(doJsonArr);
            }
            return new EmptyResult();
        }
        #endregion

        #region 雜物箱
        //藥品條碼
        public ActionResult Med_Bacrcode()
        {
            try
            {
                string strBarcode = string.Empty;
                string strFlag = string.Empty;
                string sql = "";
                List<MedInfo> med_list = new List<MedInfo>();

                if (Request["str_Barcode"] != null)
                {
                    strBarcode = Request["str_Barcode"].ToString().Trim();

                    if (strBarcode.Length == 18)
                    { //餐包條碼
                        sql = "SELECT DRUG_CODE AS MED_CODE, RIGHT_TIME AS GIVETIME FROM BCMA_BCPRINT WHERE BARCODE = '" + strBarcode + "'";
                    }
                    else
                    { //種包條碼
                        sql = "SELECT DISTINCT MED_CODE,'0' AS GIVETIME FROM DRUG_SHOW WHERE SHEETNO ='" + strBarcode + "'";
                    }
                    //IDataReader reader = null;
                    DataTable Dt = link.DBExecSQL(sql);
                    if (Dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            med_list.Add(new MedInfo()
                            {
                                med_code = Dt.Rows[i]["MED_CODE"].ToString().Trim(),
                                med_time = Dt.Rows[i]["GIVETIME"].ToString().Trim()
                            });
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
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString());
                return new EmptyResult();
            }
        }

        public ActionResult Bad_Reaction()
        {
            return View();
        }

        public ActionResult Check()
        {
            return View();
        }

        protected DataTable set_dt_column(DataTable dt, string[] clumn)
        {
            for (int i = 0; i < clumn.Length; i++)
            {
                dt.Columns.Add(clumn[i]);
            }
            return dt;
        }

        public ActionResult Execute2()//test
        {
            return View();
        }

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
                return false;
            }
            else
            {
                return true;
            }
        }
        #endregion
    }
}