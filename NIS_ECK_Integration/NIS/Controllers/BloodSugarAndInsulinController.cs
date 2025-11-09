using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NIS.Controllers;
using NIS.Models;
using NIS.Data;
using NIS.WebService;
using NIS.UtilTool;
using Newtonsoft.Json;
using Com.Mayaminer;

namespace NIS.Controllers
{
    public class BloodSugarAndInsulinController : BaseController
    {
        private CommData cd;    //常用資料Module
        private BloodSugarAndInsulin bai;
        // private PatientInfo ptInfo;
        DBConnector link = new DBConnector();
        LogTool log = new LogTool();
        private string mode = MvcApplication.iniObj.NisSetting.ServerMode.ToString();

        public BloodSugarAndInsulinController()
        {
            this.cd = new CommData();
            this.bai = new BloodSugarAndInsulin();
            // this.ptInfo = (PatientInfo)HttpContext.Session["PatInfo"];
            // PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
        }
        public class BloodSugarInquiry
        {
            public string BSID { get; set; }
            public string FEENO { get; set; }
            public string INDATE { get; set; }
            public string BLOODSUGAR { get; set; }
            public string NOTE { get; set; }
            public string SYMPTOM { get; set; }
            public string DEALWITH { get; set; }
            public string MONITOR { get; set; }
            public string DRUGNAME { get; set; }
            public string DOSE { get; set; }
            public string ROUTE { get; set; }
            public string DOSEUNIT { get; set; }
            public string STATUS { get; set; }

        }
        public class newitem
        {
            public string Ordseq { get; set; }
            public string FEENO { get; set; }
            public string INDATE { get; set; }
            public string POSITION { get; set; }
            public string REVIEW { get; set; }
            public string DRUGNAME { get; set; }
            public string DRUGTYPE { get; set; }
            public string DOSE { get; set; }
            public string DOSEUNIT { get; set; }
            public string ROUTE { get; set; }
            public string STATUS { get; set; }
            public string REASON { get; set; }
            public string REASONTYPE { get; set; }
            public string INJECTION { get; set; }
            public string PAGE { get; set; }
            public string SDID { get; set; }
        }

        public enum RESPONSE_STATUS
        {
            SUCCESS = 0,
            ERROR = 1,
            EXCEPTION = 2
        }

        public class RESPONSE_MSG
        {
            /// <summary> 處理狀態 </summary>
            public RESPONSE_STATUS status { set; get; }

            /// <summary> 傳回訊息或內容 </summary>
            public string message { set; get; }

            /// <summary> 附帶物件 </summary>
            public object attachment { set; get; }

            /// <summary> 取得序列化結果 </summary>
            public string get_json()
            {
                return JsonConvert.SerializeObject(this);
            }
        }


        #region 舊版
        //
        // GET: /BloodSugarAndInsulin/
        [HttpPost]
        public ActionResult test(string val)
        {
            string aa = "this is test";
            ViewBag.test = aa;
            return PartialView();
        }

        //舊版血糖列表
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult BloodSugar_List_bak(FormCollection form, List<BloodSugarInquiry> data, string ck)
        {
            #region 下拉式選單設定
            List<SelectListItem> listitem = new List<SelectListItem>();
            listitem.Add(new SelectListItem { Text = "一般", Value = "Normal" });
            listitem.Add(new SelectListItem { Text = "S", Value = "S" });
            listitem.Add(new SelectListItem { Text = "O", Value = "O" });
            listitem.Add(new SelectListItem { Text = "I", Value = "I" });
            listitem.Add(new SelectListItem { Text = "E", Value = "E" });
            ViewData["List"] = listitem;
            List<SelectListItem> listitem2 = new List<SelectListItem>();
            listitem2.Add(new SelectListItem { Text = "拒絕", Value = "拒絕" });
            listitem2.Add(new SelectListItem { Text = "禁食", Value = "禁食" });
            listitem2.Add(new SelectListItem { Text = "血糖偏低", Value = "血糖偏低" });
            listitem2.Add(new SelectListItem { Text = "其他", Value = "其他" });
            ViewData["List2"] = listitem2;
            List<SelectListItem> listitem3 = new List<SelectListItem>();
            listitem3.Add(new SelectListItem { Text = "MIXTARD 30HM★1000IU/10ml 需冷藏(30%Insulin Human Regular +70%Insulin HumanIsophane(NPH))", Value = "1" });
            listitem3.Add(new SelectListItem { Text = "HUMULIN N★ 1000IU/10ml 需冷藏(NPH )", Value = "2" });
            listitem3.Add(new SelectListItem { Text = "HUMULIN R★1000IU/10ml 需冷藏 (Insulin Human Regular)", Value = "3" });
            listitem3.Add(new SelectListItem { Text = "penfill Novorapid★300IU/3ml 需冷藏(Insulin aspart)", Value = "4" });
            listitem3.Add(new SelectListItem { Text = "penfill Insulatard HM★300IU/3ml 需冷藏(NPH)", Value = "5" });
            listitem3.Add(new SelectListItem { Text = "Lantus★1000IU/10ml 需冷藏(Insulin glargine)", Value = "6" });
            listitem3.Add(new SelectListItem { Text = "penfill Novomix 30★300IU/3ml 需冷藏(30% Insulin aspart + 70% protamine Insulin aspart )", Value = "7" });
            listitem3.Add(new SelectListItem { Text = "其它", Value = "o" });
            ViewData["List3"] = listitem3;
            List<SelectListItem> listitem4 = new List<SelectListItem>();
            listitem4.Add(new SelectListItem { Text = "IV", Value = "0" });
            listitem4.Add(new SelectListItem { Text = "IVD", Value = "1" });
            listitem4.Add(new SelectListItem { Text = "SC", Value = "2" });
            ViewData["List4"] = listitem4;
            List<SelectListItem> listitem5 = new List<SelectListItem>();
            listitem5.Add(new SelectListItem { Text = "unit", Value = "0" });
            ViewData["List5"] = listitem5;
            #endregion
            #region get高低血壓項目
            ViewData["Lsymptoms"] = this.bai.getTypeItem("Lsymptoms");
            ViewData["Ldealwith"] = this.bai.getTypeItem("Ldealwith");
            ViewData["Hsymptoms"] = this.bai.getTypeItem("Hsymptoms");
            ViewData["Hdealwith"] = this.bai.getTypeItem("Hdealwith");
            #endregion
            if (Session["PatInfo"] != null)
            {
                #region LOAD
                //宣告病患_取得住院序號
                PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
                string feeno = ptInfo.FeeNo;
                string status = "";
                if (ck == null || ck == "false")
                {
                    status = "del";
                }
                //this.cm.sql_udorderinfo();
                string start = Convert.ToDateTime(form["start_day"]).ToString("yyyy/MM/dd") + Convert.ToDateTime(form["start_time"]).ToString(" HH:mm:ss");
                string end = Convert.ToDateTime(form["end_day"]).ToString("yyyy/MM/dd") + Convert.ToDateTime(form["end_time"]).ToString(" HH:mm:ss");
                ViewBag.start_date = start;
                ViewBag.end_date = end;

                DataTable dt = new DataTable();
                dt = bai.sql_BStable(feeno, "", status, start, end);
                ViewBag.table = dt;
                ViewBag.text = "";
                dt.Columns.Add("SYval");
                dt.Columns.Add("DWval");
                DataTable dt_b = new DataTable();
                //string Frequency="";
                foreach (DataRow dr in dt.Rows)
                {
                    if (dr["BLOODSUGAR"].ToString() == "")
                    {
                        dr["BLOODSUGAR"] = " ";
                    }
                    dr["SYval"] = dr["SYMPTOM"];
                    dr["DWval"] = dr["DEALWITH"];
                    dr["SYMPTOM"] = bai.get_itemname("SYMPTOM", dr["SYMPTOM"].ToString());
                    dr["DEALWITH"] = bai.get_itemname("DEALWITH", dr["DEALWITH"].ToString());
                }
                #endregion
                #region SAVE
                bool success_dt = true, data_bs = false, data_up = false;
                if (data != null)//如果有資料的話
                {
                    if (data[0].STATUS == "new" || data[0].STATUS == " ")
                    {
                        DataTable dt_bs = new DataTable();
                        string id = DateTime.Now.ToString("yyyyMMddHHmmss");
                        data_bs = true;
                        #region 宣告UpcrcpTime

                        string[] UpcrcpTime = { "FEENO", "INDATE", "BLOODSUGAR", "NOTE", "SYMPTOM",
                                                    "DEALWITH","MONITOR","INSDT","INSOP" ,"INSOPNAME",
                                                    "MODDT","MODOP","MODNAME","DRUGNAME","DOSE",
                                                    "ROUTE","DOSEUNIT","STATUS","BSID"};
                        string[] datatype_upcrcp = { "String", "String", "String", "String", "String",
                                                         "String", "String", "String", "String", "String",
                                                         "String", "String", "String", "String", "String",
                                                         "String", "String", "String", "String"};
                        set_dt_column(dt_bs, UpcrcpTime);
                        DataRow dr = dt_bs.NewRow();
                        for (int i = 0; i < dt_bs.Columns.Count; i++)
                        {
                            dr[i] = datatype_upcrcp[i];

                        }
                        dt_bs.Rows.Add(dr);
                        //塞入datatype
                        dr = dt_bs.NewRow();

                        dr["FEENO"] = feeno;
                        dr["INDATE"] = data[0].INDATE;
                        dr["BLOODSUGAR"] = data[0].BLOODSUGAR;
                        dr["NOTE"] = data[0].NOTE;
                        if (data[0].SYMPTOM != null) { dr["SYMPTOM"] = data[0].SYMPTOM.TrimEnd(','); }
                        if (data[0].DEALWITH != null) { dr["DEALWITH"] = data[0].DEALWITH.TrimEnd(','); }
                        if (data[0].MONITOR != null) { dr["MONITOR"] = data[0].MONITOR; }//無法注射原因
                        dr["INSDT"] = (DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        dr["INSOP"] = userinfo.EmployeesNo;
                        dr["INSOPNAME"] = userinfo.EmployeesName;
                        if (data[0].DRUGNAME != null) { dr["DRUGNAME"] = data[0].DRUGNAME.ToString(); }
                        if (data[0].DOSE != null) { dr["DOSE"] = data[0].DOSE.ToString(); }
                        if (data[0].ROUTE != null) { dr["ROUTE"] = data[0].ROUTE.ToString(); }
                        if (data[0].DOSEUNIT != null) { dr["DOSEUNIT"] = data[0].DOSEUNIT.ToString(); }
                        dr["STATUS"] = data[0].STATUS.ToString();
                        dr["BSID"] = id;
                        //Hex(Now.Ticks());

                        dt_bs.Rows.Add(dr);

                        #endregion
                        #region 傳入護理紀錄
                        string mag = "", title = "";
                        Boolean save_f = false;
                        if (data[0].BLOODSUGAR != null && data[0].BLOODSUGAR != "")
                        {
                            if (int.Parse(data[0].BLOODSUGAR) < 70 || int.Parse(data[0].BLOODSUGAR) > 110)
                            {
                                title = "血糖值異常";
                                mag = "病人於" + data[0].INDATE;
                                mag += "測量血糖值為" + data[0].BLOODSUGAR;
                                if (int.Parse(data[0].BLOODSUGAR) < 70)
                                { mag += ",血糖過低"; }
                                if (int.Parse(data[0].BLOODSUGAR) > 110)
                                { mag += ",血糖過高"; }
                            }
                        }

                        if (mag != null && mag != "") { save_f = true; }
                        if (save_f)
                        {
                            Insert_CareRecord(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), id, title, "", "", mag, "", "", "BloodSugar");
                        }
                        #endregion
                        if (data_bs)
                        {
                            //確認是否有存資料 及有無成功
                            int erow = bai.insert("BLOODSUGAR", dt_bs);
                            if (erow < 1)
                                success_dt = false;
                            //儲存成功
                            if (success_dt)
                            {
                                return Redirect("../BloodSugarAndInsulin/BloodSugar_List?success=yes");
                            }
                            else
                            {                       //儲存失敗
                                return Redirect("../BloodSugarAndInsulin/BloodSugar_List?success=no");
                            }
                        }
                    }
                    else if (data[0].BSID != null)
                    {
                        DataTable dt_up = new DataTable();
                        if (data[0].STATUS == "del")//刪除
                        {

                            dt_up.Columns.Add("STATUS");
                            dt_up.Columns.Add("where");
                            DataRow dr_up = dt_up.NewRow();
                            data_up = true;
                            if (data[0].BSID != null)
                            {
                                dr_up = dt_up.NewRow();
                                string where = " BSID = '" + data[0].BSID.ToString() + "' ";
                                dr_up["STATUS"] = "del";
                                dr_up["where"] = where;
                                dt_up.Rows.Add(dr_up);
                            }
                        }
                        else if (data[0].STATUS == "upd")//更新
                        {

                            dt_up.Columns.Add("STATUS");
                            dt_up.Columns.Add("INDATE");
                            dt_up.Columns.Add("BLOODSUGAR");
                            dt_up.Columns.Add("NOTE");
                            dt_up.Columns.Add("SYMPTOM");
                            dt_up.Columns.Add("DEALWITH");
                            dt_up.Columns.Add("MONITOR");
                            dt_up.Columns.Add("MODDT");
                            dt_up.Columns.Add("MODOP");
                            dt_up.Columns.Add("MODNAME");
                            dt_up.Columns.Add("DRUGNAME");
                            dt_up.Columns.Add("DOSE");
                            dt_up.Columns.Add("ROUTE");
                            dt_up.Columns.Add("DOSEUNIT");

                            dt_up.Columns.Add("where");
                            DataRow dr_up = dt_up.NewRow();
                            data_up = true;
                            if (data[0].BSID != null)
                            {
                                dr_up = dt_up.NewRow();
                                string where = " BSID = '" + data[0].BSID.ToString() + "' ";
                                dr_up["INDATE"] = data[0].INDATE;
                                dr_up["BLOODSUGAR"] = data[0].BLOODSUGAR;
                                dr_up["NOTE"] = data[0].NOTE;
                                if (data[0].SYMPTOM != null) { dr_up["SYMPTOM"] = data[0].SYMPTOM.TrimEnd(','); }
                                if (data[0].DEALWITH != null) { dr_up["DEALWITH"] = data[0].DEALWITH.TrimEnd(','); }
                                //  dr_up["MONITOR"] = data[0].MONITOR;
                                dr_up["MODDT"] = (DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss"));
                                dr_up["MODOP"] = userinfo.EmployeesNo;
                                dr_up["MODNAME"] = userinfo.EmployeesName;
                                if (data[0].MONITOR != null) { dr_up["MODNAME"] = ""; }
                                if (data[0].DRUGNAME != null) { dr_up["DRUGNAME"] = data[0].DRUGNAME.ToString(); }
                                if (data[0].DOSE != null) { dr_up["DOSE"] = data[0].DOSE.ToString(); }
                                if (data[0].ROUTE != null) { dr_up["ROUTE"] = data[0].ROUTE.ToString(); }
                                if (data[0].DOSEUNIT != null) { dr_up["DOSEUNIT"] = data[0].DOSEUNIT.ToString(); }
                                dr_up["STATUS"] = "upd";
                                dr_up["where"] = where;
                                dt_up.Rows.Add(dr_up);
                            }
                        }

                        if (data_up)
                        {
                            //確認是否有存資料 及有無成功
                            int erow = bai.upd("BLOODSUGAR", dt_up);
                            if (erow < 1)
                                success_dt = false;
                            //儲存成功
                            if (success_dt)
                            {
                                return Redirect("../BloodSugarAndInsulin/BloodSugar_List?success=yes");
                            }
                            else
                            {                       //儲存失敗
                                return Redirect("../BloodSugarAndInsulin/BloodSugar_List?success=no");
                            }
                        }
                    }
                }

                #endregion
                #region SAVE 補輸
                if (data[0].INDATE != null && data[0].INDATE.ToString().Trim() != "")
                {
                    string aa = data[0].INDATE;
                    if (DateTime.Parse(data[0].INDATE.ToString().Trim()) < DateTime.Now)
                    {//如果輸入時間小於現在時間

                    }

                }
                #endregion

                return View();

            }
            return Redirect("../VitalSign/VitalSign_Single");

        }

        [HttpGet]
        public ActionResult BloodSugar_List(string success, string qs, string qe, string getfeeno)
        {
            string tmp_item = "", tmp_value = "", tmp_color = "";
            //判斷有無病人session
            if (Session["PatInfo"] != null)
            {
                #region 下拉式選單設定
                List<SelectListItem> listitem = new List<SelectListItem>();
                listitem.Add(new SelectListItem { Text = "一般", Value = "Normal" });
                listitem.Add(new SelectListItem { Text = "S", Value = "S" });
                listitem.Add(new SelectListItem { Text = "O", Value = "O" });
                listitem.Add(new SelectListItem { Text = "I", Value = "I" });
                listitem.Add(new SelectListItem { Text = "E", Value = "E" });
                ViewData["List"] = listitem;
                List<SelectListItem> listitem2 = new List<SelectListItem>();
                listitem2.Add(new SelectListItem { Text = "拒絕", Value = "拒絕" });
                listitem2.Add(new SelectListItem { Text = "測不到", Value = "測不到" });
                ViewData["List2"] = listitem2;
                List<SelectListItem> listitem3 = new List<SelectListItem>();
                listitem3.Add(new SelectListItem { Text = "MIXTARD 30HM★1000IU/10ml 需冷藏(30%Insulin Human Regular +70%Insulin HumanIsophane(NPH))", Value = "1" });
                listitem3.Add(new SelectListItem { Text = "HUMULIN N★ 1000IU/10ml 需冷藏(NPH )", Value = "2" });
                listitem3.Add(new SelectListItem { Text = "HUMULIN R★1000IU/10ml 需冷藏 (Insulin Human Regular)", Value = "3" });
                listitem3.Add(new SelectListItem { Text = "penfill Novorapid★300IU/3ml 需冷藏(Insulin aspart)", Value = "4" });
                listitem3.Add(new SelectListItem { Text = "penfill Insulatard HM★300IU/3ml 需冷藏(NPH)", Value = "5" });
                listitem3.Add(new SelectListItem { Text = "Lantus★1000IU/10ml 需冷藏(Insulin glargine)", Value = "6" });
                listitem3.Add(new SelectListItem { Text = "penfill Novomix 30★300IU/3ml 需冷藏(30% Insulin aspart + 70% protamine Insulin aspart )", Value = "7" });
                listitem3.Add(new SelectListItem { Text = "其它", Value = "o" });
                ViewData["List3"] = listitem3;
                List<SelectListItem> listitem4 = new List<SelectListItem>();
                listitem4.Add(new SelectListItem { Text = "IV", Value = "0" });
                listitem4.Add(new SelectListItem { Text = "IVD", Value = "1" });
                listitem4.Add(new SelectListItem { Text = "SC", Value = "2" });
                ViewData["List4"] = listitem4;
                List<SelectListItem> listitem5 = new List<SelectListItem>();
                listitem5.Add(new SelectListItem { Text = "unit", Value = "0" });
                ViewData["List5"] = listitem5;
                #endregion
                #region get高低血壓項目
                ViewData["Lsymptoms"] = this.bai.getTypeItem("Lsymptoms");
                ViewData["Ldealwith"] = this.bai.getTypeItem("Ldealwith");
                ViewData["Hsymptoms"] = this.bai.getTypeItem("Hsymptoms");
                ViewData["Hdealwith"] = this.bai.getTypeItem("Hdealwith");
                #endregion

                if (success == "yes")
                    Response.Write("<script>alert('儲存成功!')</script>");
                else if (success == "no")
                    Response.Write("<script>alert('儲存失敗!')</script>");
                else if (success == "upd")
                    Response.Write("<script>alert('更新成功!')</script>");
                else if (success == "ck")
                    Response.Write("<script>alert('未勾選給藥時間!')</script>");
                else if (success == "unselcet_pat")
                    Response.Write("<script>alert('尚未選擇病患!')</script>");
                else if (success == "overdue")
                    Response.Write("<script>alert('Session過期!')</script>");

                #region LOAD
                //宣告病患_取得住院序號
                Response.Write("");
                PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
                string feeno = string.Empty;
                if (getfeeno != null)
                {
                    feeno = getfeeno;
                    ViewBag.getfeeno = getfeeno;
                    ViewBag.loginempno = "";
                }
                else
                {
                    feeno = ptInfo.FeeNo;
                    ViewBag.loginempno = userinfo.EmployeesNo;
                }

                DataTable dt = new DataTable();
                DateTime q_start = DateTime.Now.AddDays(-7);
                DateTime q_end = DateTime.Now;
                if (qs != null && qe != null)
                {
                    q_start = Convert.ToDateTime(qs);
                    q_end = Convert.ToDateTime(qe);
                    dt = bai.sql_BStable(feeno, "", "del", q_start.ToString("yyyy/MM/dd HH:mm"), q_end.ToString("yyyy/MM/dd HH:mm"));
                }
                else
                {
                    dt = bai.sql_BStable(feeno, "", "del", "", "");
                }
                ViewBag.start = q_start;
                ViewBag.end = q_end;

                //取得異常查檢表
                DataTable dt_check = base.Get_Check_Abnormal_dt();

                ViewBag.text = "";
                dt.Columns.Add("SYval");
                dt.Columns.Add("DWval");
                dt.Columns.Add("Color");
                DataTable dt_b = new DataTable();

                foreach (DataRow dr in dt.Rows)
                {
                    tmp_item = tmp_value = tmp_color = "";

                    tmp_value = dr["BLOODSUGAR"].ToString();
                    //tmp_item = set_name(tmp);
                    tmp_color = base.check_abnormal_color(dt_check, "bs", tmp_value);

                    if (dr["BLOODSUGAR"].ToString() == "")
                    {
                        dr["BLOODSUGAR"] = " ";
                    }
                    dr["SYval"] = dr["SYMPTOM"];
                    dr["DWval"] = dr["DEALWITH"];
                    dr["Color"] = tmp_color;
                    dr["SYMPTOM"] = bai.get_itemname("SYMPTOM", dr["SYMPTOM"].ToString());
                    dr["DEALWITH"] = bai.get_itemname("DEALWITH", dr["DEALWITH"].ToString());
                }
                ViewBag.table = dt;

                #endregion

                return View();
            }
            else
            {
                Response.Write("<script>alert('請先選擇病人');</script>");
                return new EmptyResult();
            }


        }

        //20131228 Edit By James 修改血糖列表儲存方式
        [HttpPost]
        public ActionResult BloodSugar_List()
        {
            if (Request.Form["QueryStatus"].ToString() == "True")//查詢
            {
                string qs = Request.Form["start_day"].ToString() + " " + Request.Form["start_time"].ToString();
                string qe = Request.Form["end_day"].ToString() + " " + Request.Form["end_time"].ToString();
                if (Request.Form["getfeeno"].ToString() != "")
                {
                    return Redirect("../BloodSugarAndInsulin/BloodSugar_List?getfeeno=" + Request.Form["getfeeno"].ToString() + "&qs=" + qs + "&qe=" + qe);
                }
                else
                {
                    return Redirect("../BloodSugarAndInsulin/BloodSugar_List?qs=" + qs + "&qe=" + qe);
                }
            }

            string SYMPTOM = string.Empty;
            string DEALWITH = string.Empty;
            if (!string.IsNullOrWhiteSpace(Request.Form["SymptomContent"]))
            {
                SYMPTOM = Request.Form["SymptomContent"].ToString();
                SYMPTOM = SYMPTOM.Substring(0, SYMPTOM.Length - 1);
            }
            if (!string.IsNullOrWhiteSpace(Request.Form["HandleContent"]))
            {
                DEALWITH = Request.Form["HandleContent"].ToString();
                DEALWITH = DEALWITH.Substring(0, DEALWITH.Length - 1);
            }

            string SymptomRecord = string.Empty;
            string DealwithRecord = string.Empty;

            DataTable SD = this.bai.GetBSDS();
            string[] AllSDItem = new string[SD.Rows.Count + 1];
            string[] AllSDValue = new string[SD.Rows.Count + 1];
            //將症狀及處置中文說明放到陣列中
            for (int sdc = 0; sdc < SD.Rows.Count; sdc++)
            {
                AllSDItem[sdc] = SD.Rows[sdc]["ITEM"].ToString();
                AllSDValue[sdc] = SD.Rows[sdc]["VALUE"].ToString();
            }

            //如果儲存之值符合則轉換成中文說明
            for (int sdc = 0; sdc < SD.Rows.Count; sdc++)
            {
                for (int src = 0; src < SYMPTOM.Split(',').Length; src++)
                {
                    if (SYMPTOM.Split(',').GetValue(src).ToString() == AllSDValue[sdc])
                    {
                        SymptomRecord += AllSDItem[sdc] + "，";
                    }
                }
                for (int drc = 0; drc < DEALWITH.Split(',').Length; drc++)
                {
                    if (DEALWITH.Split(',').GetValue(drc).ToString() == AllSDValue[sdc])
                    {
                        DealwithRecord += AllSDItem[sdc] + "，";
                    }
                }
            }
            if (SymptomRecord != null && SymptomRecord != "")
                SymptomRecord = "，症狀：" + SymptomRecord.Substring(0, SymptomRecord.Length - 1) + "。";

            if (DealwithRecord != null && DealwithRecord != "")
                DealwithRecord = "處置：" + DealwithRecord.Substring(0, DealwithRecord.Length - 1) + "。";

            //20140131 修改護理紀錄內容
            string INDATE = Request.Form["now_day"].ToString() + " " + Request.Form["now_time"].ToString();
            string INSDT = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            string FeeNo = ptinfo.FeeNo.ToString().Trim();
            string BLOODSUGAR = Request.Form["value"].ToString();
            if (Request.Form["low"] != null)
            {
                if (Request.Form["low"].ToString() == "0")
                {
                    BLOODSUGAR = "1";
                }
                else if (Request.Form["low"].ToString() == "1")
                {
                    BLOODSUGAR = "999";
                }
            }
            string BSStatus = string.Empty;
            string ReturnMessage = string.Empty;
            string tmp = "";
            string[] ItemIDList = Request.Form.AllKeys;

            if (BLOODSUGAR != null && BLOODSUGAR.Trim() != "")
            {
                tmp = BLOODSUGAR;
                if (Convert.ToInt32(BLOODSUGAR.Replace("#", "")) < 2)
                { BLOODSUGAR = "Low"; }
                else if (Convert.ToInt32(BLOODSUGAR.Replace("#", "")) >= 998)
                { BLOODSUGAR = "High"; }
            }

            if (BLOODSUGAR.Trim() == "")
            {
                if (Request.Form["SubmitStatus"].ToString() != "del" && Request.Form["low"] != null && (Request.Form["List2"].ToString() != "無法測量原因" && Request.Form["List2"].ToString() != ""))
                {
                    if (Request.Form["low"].ToString() == "0")
                    {
                        BSStatus = "Low";
                    }
                    else if (Request.Form["low"].ToString() == "1")
                    {
                        BSStatus = "High";
                    }
                }
            }

            string Note = Request.Form["note"].ToString();
            string MONITOR = string.Empty;
            if (Request.Form["List2"].ToString() != "無法測量原因" && Request.Form["List2"].ToString() != "")
            {
                MONITOR = Request.Form["List2"].ToString();
            }
            //如果沒有 BSID 則新增，若有則修改
            if (Request.Form["BSID"].ToString() == "" && Request.Form["SubmitStatus"].ToString() == "")
            {
                string status = (BSStatus == "") ? "new" : BSStatus;

                string BSID = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                List<DBItem> insertDataList = new List<DBItem>();
                insertDataList.Add(new DBItem("FEENO", FeeNo, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("INDATE", INDATE, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("BLOODSUGAR", tmp, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("NOTE", Note, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("SYMPTOM", SYMPTOM, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("DEALWITH", DEALWITH, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MONITOR", MONITOR, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("INSDT", INSDT, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("INSOP", userinfo.EmployeesNo, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("INSOPNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("STATUS", status, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("BSID", BSID, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("COST_CODE", ptinfo.CostCenterNo.ToString().Trim(), DBItem.DBDataType.String));
                if (!string.IsNullOrWhiteSpace(Request.Form["rbn_meal_status"]))
                    insertDataList.Add(new DBItem("MEAL_STATUS", Request.Form["rbn_meal_status"].ToString(), DBItem.DBDataType.String));
                int erow = link.DBExecInsert("BLOODSUGAR", insertDataList);
                ReturnMessage = "資料已新增";

                string CareRecord_String = string.Format("{0} {1}，血糖值：{2}{3}{4}。"
                             , Convert.ToDateTime(INDATE).ToString("yyyy/MM/dd HH:mm:ss")
                             , "測量種類：" + Request.Form["rbn_meal_status"].ToString().Replace("C", "不清楚").Replace("B", "飯前").Replace("A", "飯後")
                             , (status == "Low" || status == "High") ? status : BLOODSUGAR + "mg/dl"
                             , (!string.IsNullOrWhiteSpace(SYMPTOM)) ? SymptomRecord : ""
                             , (!string.IsNullOrWhiteSpace(Note)) ? "，註記： " + Note + "。" : "");

                if (Request.Form["List2"].ToString() != "無法測量原因" && Request.Form["List2"].ToString() != "")
                {
                    string message = INDATE + " 病人無法測量血糖，因 " + Request.Form["List2"].ToString() + "。";
                    if (Insert_CareRecord(Convert.ToDateTime(INDATE).ToString("yyyy/MM/dd HH:mm:ss"), BSID, "", "", "", message, "", "", "BloodSugar") == 0)
                        Insert_CareRecord(Convert.ToDateTime(INDATE).ToString("yyyy/MM/dd HH:mm:ss"), BSID, "", "", "", message, "", "", "BloodSugar");
                }
                else
                {
                    if (Insert_CareRecord(Convert.ToDateTime(INDATE).ToString("yyyy/MM/dd HH:mm:ss"), BSID, "", "", "", CareRecord_String, "" + DealwithRecord, "", "BloodSugar") == 0)
                        Insert_CareRecord(Convert.ToDateTime(INDATE).ToString("yyyy/MM/dd HH:mm:ss"), BSID, "", "", "", CareRecord_String, DealwithRecord, "", "BloodSugar");
                }
                string mealStatus = Request.Form["rbn_meal_status"].ToString();
                string setID = "";

                List<Bill_RECORD> billDataList = new List<Bill_RECORD>();

                Bill_RECORD billData = new Bill_RECORD();

                // 新增默許
                if (mealStatus == "B")
                {
                    billData.HO_ID = "6309904";
                    billData.COUNT = "1";

                }
                else
                {
                    billData.HO_ID = "6309905";
                    billData.COUNT = "1";

                }
                billDataList.Add(billData);

                if (ptinfo.Age >= 1)
                {
                    billData = new Bill_RECORD();
                    billData.HO_ID = "9500324";
                    billData.COUNT = "1";

                    billDataList.Add(billData);
                }
                else
                {
                    billData = new Bill_RECORD();
                    billData.HO_ID = "9500314";
                    billData.COUNT = "1";
                    billData.IDENTITY = "吸收";

                    billDataList.Add(billData);
                }

                SaveBillingRecord(billDataList);

            }
            else
            {
                //標記為刪除
                if (Request.Form["SubmitStatus"].ToString() == "del")
                {
                    string where = " BSID = '" + Request.Form["BSID"].ToString() + "' ";
                    List<DBItem> updList = new List<DBItem>();
                    updList.Add(new DBItem("status", "del", DBItem.DBDataType.String));
                    int effRow = this.link.DBExecUpdate("BLOODSUGAR", updList, where);
                    ReturnMessage = "資料已刪除";
                    Del_CareRecord(Request.Form["BSID"].ToString(), "BloodSugar");
                }
                else
                {//TODO: 把INDATE 改成 存表單時間
                    string status = (BSStatus == "") ? "upd" : BSStatus;
                    string where = " BSID = '" + Request.Form["BSID"].ToString() + "' ";
                    List<DBItem> updList = new List<DBItem>();//血糖監測(修改)SQL UPDATE
                    updList.Add(new DBItem("INDATE", INDATE, DBItem.DBDataType.String));//20171026 add by AlanHuang
                    updList.Add(new DBItem("bloodsugar", tmp, DBItem.DBDataType.String));//20140724 mod yungchen
                    updList.Add(new DBItem("note", Note, DBItem.DBDataType.String));
                    updList.Add(new DBItem("symptom", SYMPTOM, DBItem.DBDataType.String));
                    updList.Add(new DBItem("dealwith", DEALWITH, DBItem.DBDataType.String));
                    updList.Add(new DBItem("monitor", MONITOR, DBItem.DBDataType.String));
                    updList.Add(new DBItem("moddt", INSDT, DBItem.DBDataType.String));//修改日期目前存DateTimeNow，待與USER確認
                    updList.Add(new DBItem("modop", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    updList.Add(new DBItem("modname", userinfo.EmployeesName, DBItem.DBDataType.String));
                    updList.Add(new DBItem("status", status, DBItem.DBDataType.String));
                    //add MEAL_STATUS 加入飯前飯後 2016/6/15
                    if (!string.IsNullOrWhiteSpace(Request.Form["rbn_meal_status"]))
                        updList.Add(new DBItem("MEAL_STATUS", Request.Form["rbn_meal_status"].ToString(), DBItem.DBDataType.String));

                    int effRow = this.link.DBExecUpdate("BLOODSUGAR", updList, where);
                    ReturnMessage = "資料已更新";
                    string CareRecord_String = string.Format("{0} {1}，血糖值：{2}{3}{4}。"
                            , Convert.ToDateTime(INDATE).ToString("yyyy/MM/dd HH:mm:ss")
                            , "測量種類：" + Request.Form["rbn_meal_status"].ToString().Replace("C", "不清楚").Replace("B", "飯前").Replace("A", "飯後")
                            , (status == "Low" || status == "High") ? status : BLOODSUGAR + "mg/dl"
                            , (!string.IsNullOrWhiteSpace(SYMPTOM)) ? SymptomRecord : ""
                            , (!string.IsNullOrWhiteSpace(Note)) ? "，註記： " + Note + "。" : "");

                    if (Request.Form["List2"].ToString() != "無法測量原因" && Request.Form["List2"].ToString() != "")
                    {
                        string message = INDATE + " 病人無法測量血糖，因 " + Request.Form["List2"].ToString() + "。";
                        if (Upd_CareRecord(Convert.ToDateTime(INDATE).ToString("yyyy/MM/dd HH:mm:ss"), Request.Form["BSID"].ToString(), "", "", "", message, "", "", "BloodSugar") == 0)
                            Insert_CareRecord(Convert.ToDateTime(INDATE).ToString("yyyy/MM/dd HH:mm:ss"), Request.Form["BSID"].ToString(), "", "", "", message, "", "", "BloodSugar");
                    }
                    else
                    {
                        if (Upd_CareRecord(Convert.ToDateTime(INDATE).ToString("yyyy/MM/dd HH:mm:ss"), Request.Form["BSID"].ToString(), "", "", "", CareRecord_String, "" + DealwithRecord, "", "BloodSugar") == 0)
                            Insert_CareRecord(Convert.ToDateTime(INDATE).ToString("yyyy/MM/dd HH:mm:ss"), Request.Form["BSID"].ToString(), "", "", "", CareRecord_String, DealwithRecord, "", "BloodSugar");
                    }
                }
            }
            return RedirectToAction("BloodSugar_List", new { @message = "" + ReturnMessage + "" });
        }

        /// <summary>
        /// 設定table的colum
        /// </summary>
        /// <param name="dt">DataTable</param>
        /// <param name="clumn">string[]</param>
        protected DataTable set_dt_column(DataTable dt, string[] clumn)
        {
            for (int i = 0; i < clumn.Length; i++)
            {
                dt.Columns.Add(clumn[i]);
            }
            return dt;
        }
        public ActionResult BloodSugar_Insert(string bsid)
        {
            #region 下拉式選單設定
            List<SelectListItem> listitem = new List<SelectListItem>();
            listitem.Add(new SelectListItem { Text = "一般", Value = "" });
            listitem.Add(new SelectListItem { Text = "S", Value = "" });
            listitem.Add(new SelectListItem { Text = "O", Value = "" });
            listitem.Add(new SelectListItem { Text = "I", Value = "" });
            listitem.Add(new SelectListItem { Text = "E", Value = "" });
            ViewData["List"] = listitem;
            List<SelectListItem> listitem2 = new List<SelectListItem>();
            listitem2.Add(new SelectListItem { Text = "測不到", Value = "" });
            listitem2.Add(new SelectListItem { Text = "拒絕", Value = "" });
            listitem2.Add(new SelectListItem { Text = "其他", Value = "" });
            ViewData["List2"] = listitem2;
            List<SelectListItem> listitem3 = new List<SelectListItem>();
            listitem3.Add(new SelectListItem { Text = "50% G/W 20ML/AMP", Value = "" });
            listitem3.Add(new SelectListItem { Text = "其它", Value = "" });
            ViewData["List3"] = listitem3;
            List<SelectListItem> listitem4 = new List<SelectListItem>();
            listitem4.Add(new SelectListItem { Text = "IV", Value = "" });
            listitem4.Add(new SelectListItem { Text = "IVD", Value = "" });
            ViewData["List4"] = listitem4;
            List<SelectListItem> listitem5 = new List<SelectListItem>();
            listitem5.Add(new SelectListItem { Text = "AMP", Value = "" });
            ViewData["List5"] = listitem5;
            #endregion
            #region get高低血壓項目
            ViewData["Lsymptoms"] = this.bai.getTypeItem("Lsymptoms");
            ViewData["Ldealwith"] = this.bai.getTypeItem("Ldealwith");
            ViewData["Hsymptoms"] = this.bai.getTypeItem("Hsymptoms");
            ViewData["Hdealwith"] = this.bai.getTypeItem("Hdealwith");
            #endregion

            if (bsid != null)
            {
                //宣告病患_取得住院序號
                if (Session["PatInfo"] != null)
                {
                    PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
                    string feeno = ptInfo.FeeNo;

                    //this.cm.sql_udorderinfo();
                    DataTable dt = new DataTable();
                    dt = bai.sql_BStable(feeno, bsid, "", "", "");



                    DataRow dr = dt.Rows[0];
                    //   dt = bai.sql_BStable(feeno, bsid, "", "", "").Rows[0];

                    ViewBag.dr = dr;
                    //ViewBag.text = "";

                    //DataTable dt_b = new DataTable();
                    ////string Frequency="";
                    //foreach (DataRow dr in dt.Rows)
                    //{
                    //    if (dr["BLOODSUGAR"].ToString() == "")
                    //    {
                    //        dr["BLOODSUGAR"] = " ";
                    //    }
                    //    dr["SYMPTOM"] = bai.get_itemname("SYMPTOM", dr["SYMPTOM"].ToString());
                    //    dr["DEALWITH"] = bai.get_itemname("DEALWITH", dr["DEALWITH"].ToString());
                    //}
                }
            }

            return View();

        }

        public ActionResult BloodSugar_low_symptoms()
        {
            return View();
        }

        public ActionResult BloodSugar_low_disposal()
        {
            return View();
        }

        public ActionResult BloodSugar_high_symptoms()
        {
            return View();
        }


        public ActionResult Print_Single()
        {
            return View();
        }
        [HttpGet]
        public ActionResult Insulin_List(string success)
        {
            #region 下拉式選單設定
            List<SelectListItem> listitem = new List<SelectListItem>();
            listitem.Add(new SelectListItem { Text = "一般", Value = "" });
            listitem.Add(new SelectListItem { Text = "S", Value = "" });
            listitem.Add(new SelectListItem { Text = "O", Value = "" });
            listitem.Add(new SelectListItem { Text = "I", Value = "" });
            listitem.Add(new SelectListItem { Text = "E", Value = "" });
            ViewData["List"] = listitem;
            List<SelectListItem> listitem2 = new List<SelectListItem>();
            listitem2.Add(new SelectListItem { Text = "MIXTARD 30HM", Value = "1" });
            listitem2.Add(new SelectListItem { Text = "HUMULIN N", Value = "2" });
            listitem2.Add(new SelectListItem { Text = "HUMULIN R", Value = "3" });
            listitem2.Add(new SelectListItem { Text = "penfill Navorapid", Value = "4" });
            listitem2.Add(new SelectListItem { Text = "penfill Insulatard HM", Value = "5" });
            listitem2.Add(new SelectListItem { Text = "Lantus", Value = "6" });
            listitem2.Add(new SelectListItem { Text = "novomix 30 penfill", Value = "7" });
            listitem2.Add(new SelectListItem { Text = "其他", Value = "8" });
            ViewData["List2"] = listitem2;
            List<SelectListItem> listitem3 = new List<SelectListItem>();
            listitem3.Add(new SelectListItem { Text = "拒絕", Value = "" });
            listitem3.Add(new SelectListItem { Text = "禁食", Value = "" });
            listitem3.Add(new SelectListItem { Text = "血糖偏低", Value = "" });
            listitem3.Add(new SelectListItem { Text = "其他", Value = "" });
            ViewData["List3"] = listitem3;
            #endregion
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
                string get_p = "";
                get_p = bai.get_position(feeno).ToString().Trim();
                string status = "", REVIEW = "";
                string page = "IN";
                status = "del";
                DataTable dt_s = new DataTable();
                DataTable dt = new DataTable();
                dt_s = bai.sql_DtSet(feeno);
                dt = bai.sql_SDtable(feeno, page, "", status, "", "");
                string OK = "A,E,B,F,C,G,H,D,";
                if (dt_s.Rows.Count > 0)
                {
                    REVIEW = dt_s.Rows[0]["REVIEW"].ToString().Trim();
                    foreach (char i in REVIEW)
                    {
                        OK = OK.Replace(i + ",", "");
                    }
                    if (OK != "") { ViewBag.OK = OK; }
                }

                ViewBag.get_p = get_p;
                ViewBag.table = dt;
                ViewBag.dt_s = dt_s;
                ViewBag.text = "";

                return View();

            }
            return Redirect("../VitalSign/VitalSign_Single");

        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Insulin_List(FormCollection form, string SDID, string ck, List<newitem> data)
        {

            if (Session["PatInfo"] != null)
            {
                #region 下拉式選單設定
                List<SelectListItem> listitem = new List<SelectListItem>();
                listitem.Add(new SelectListItem { Text = "一般", Value = "N" });
                listitem.Add(new SelectListItem { Text = "S", Value = "S" });
                listitem.Add(new SelectListItem { Text = "O", Value = "O" });
                listitem.Add(new SelectListItem { Text = "I", Value = "I" });
                listitem.Add(new SelectListItem { Text = "E", Value = "E" });
                ViewData["List"] = listitem;
                List<SelectListItem> listitem2 = new List<SelectListItem>();
                listitem2.Add(new SelectListItem { Text = "MIXTARD 30HM", Value = "1" });
                listitem2.Add(new SelectListItem { Text = "HUMULIN N", Value = "2" });
                listitem2.Add(new SelectListItem { Text = "HUMULIN R", Value = "3" });
                listitem2.Add(new SelectListItem { Text = "penfill Navorapid", Value = "4" });
                listitem2.Add(new SelectListItem { Text = "penfill Insulatard HM", Value = "5" });
                listitem2.Add(new SelectListItem { Text = "Lantus", Value = "6" });
                listitem2.Add(new SelectListItem { Text = "novomix 30 penfill", Value = "7" });
                listitem2.Add(new SelectListItem { Text = "其他", Value = "8" });
                ViewData["List2"] = listitem2;
                List<SelectListItem> listitem3 = new List<SelectListItem>();
                listitem3.Add(new SelectListItem { Text = "拒絕", Value = "" });
                listitem3.Add(new SelectListItem { Text = "禁食", Value = "" });
                listitem3.Add(new SelectListItem { Text = "血糖偏低", Value = "" });
                listitem3.Add(new SelectListItem { Text = "其他", Value = "" });
                ViewData["List3"] = listitem3;
                #endregion
                #region LOAD
                //宣告病患_取得住院序號
                PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
                string feeno = ptInfo.FeeNo;

                string status = "", REVIEW = "";
                string page = "IN";
                if (ck == null || ck == "false")
                {
                    status = "del";
                }
                DataTable dt_s = new DataTable();
                DataTable dt = new DataTable();
                string start = Convert.ToDateTime(form["start_day"]).ToString("yyyy/MM/dd hh:mm:ss");
                string end = Convert.ToDateTime(form["end_day"]).ToString("yyyy/MM/dd hh:mm:ss");
                ViewBag.start_date = start;
                ViewBag.end_date = end;
                dt_s = bai.sql_DtSet(feeno);
                dt = bai.sql_SDtable(feeno, page, "", status, start, end);
                ViewBag.table = dt;
                ViewBag.dt_s = dt_s;
                string OK = "A,E,B,F,C,G,H,D,";
                if (dt_s.Rows.Count > 0)
                {
                    REVIEW = dt_s.Rows[0]["REVIEW"].ToString().Trim();
                    foreach (char i in REVIEW)
                    {
                        OK = OK.Replace(i + ",", "");
                    }
                    if (OK != "") { ViewBag.OK = OK; }
                }
                ViewBag.table = dt;
                ViewBag.text = "";
                #endregion
                #region SAVE
                //rbn_meal_status
                bool success_dt = true, data_up = false;
                if (data[0].SDID != null && data[0].SDID != "")//如果有資料的話
                {
                    DataTable dt_up = new DataTable();

                    if (data[0].STATUS == "del")
                    {
                        dt_up.Columns.Add("STATUS");
                        dt_up.Columns.Add("where");
                        DataRow dr_up = dt_up.NewRow();
                        data_up = true;
                        if (data[0].SDID != null)
                        {
                            dr_up = dt_up.NewRow();
                            string where = " SDID = '" + data[0].SDID + "' ";
                            dr_up["STATUS"] = "del";
                            dr_up["where"] = where;
                            dt_up.Rows.Add(dr_up);
                        }
                    }
                    else if (data[0].STATUS == "upd")
                    {
                        //更新資料

                        dt_up.Columns.Add("INDATE");
                        dt_up.Columns.Add("POSITION");
                        dt_up.Columns.Add("REVIEW");
                        dt_up.Columns.Add("MODDT");
                        dt_up.Columns.Add("MODOP");
                        dt_up.Columns.Add("MODNAME");
                        dt_up.Columns.Add("DRUGNAME");
                        dt_up.Columns.Add("DRUGTYPE");
                        dt_up.Columns.Add("DOSE");
                        dt_up.Columns.Add("ROUTE");
                        dt_up.Columns.Add("DOSEUNIT");
                        dt_up.Columns.Add("STATUS");
                        //  dt_up.Columns.Add("SDID");
                        dt_up.Columns.Add("REASON");
                        dt_up.Columns.Add("REASONTYPE");
                        dt_up.Columns.Add("INJECTION");
                        dt_up.Columns.Add("PAGE");


                        dt_up.Columns.Add("where");
                        DataRow dr_up = dt_up.NewRow();
                        data_up = true;

                        dr_up = dt_up.NewRow();
                        string where = " SDID = '" + data[0].SDID + "' ";
                        dr_up["INDATE"] = data[0].INDATE;
                        dr_up["POSITION"] = data[0].POSITION;
                        dr_up["REVIEW"] = data[0].REVIEW;
                        dr_up["MODDT"] = (DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss"));
                        dr_up["MODOP"] = userinfo.EmployeesNo;
                        dr_up["MODNAME"] = userinfo.EmployeesName;
                        dr_up["DRUGNAME"] = data[0].DRUGNAME;
                        dr_up["DRUGTYPE"] = data[0].DRUGTYPE;
                        dr_up["DOSE"] = data[0].DOSE;
                        dr_up["ROUTE"] = data[0].ROUTE;
                        dr_up["DOSEUNIT"] = data[0].DOSEUNIT;
                        dr_up["STATUS"] = "upd";
                        dr_up["REASON"] = data[0].REASON;
                        dr_up["REASONTYPE"] = data[0].REASONTYPE;
                        dr_up["INJECTION"] = userinfo.EmployeesName;
                        dr_up["PAGE"] = "IN";

                        dr_up["where"] = where;
                        dt_up.Rows.Add(dr_up);

                    }


                    if (data_up)
                    {
                        //確認是否有存資料 及有無成功
                        int erow = bai.upd("SPECIALDRUG", dt_up);
                        if (erow < 1)
                            success_dt = false;
                        //儲存成功
                        if (success_dt)
                        {
                            return Redirect("../BloodSugarAndInsulin/Insulin_List?success=yes");
                        }
                        else
                        {                       //儲存失敗
                            return Redirect("../BloodSugarAndInsulin/Insulin_List?success=no");
                        }
                    }
                }
                if (data[0].SDID == null || data[0].SDID == "")
                {//新增或查詢
                    if (data[0].STATUS == "new")
                    {
                        DataTable dt_bs = new DataTable();
                        bool data_bs = true;
                        #region 宣告UpcrcpTime

                        string[] UpcrcpTime = { "FEENO", "INDATE", "POSITION", "REVIEW",
                                                    "INSDT","INSOP" ,"INSOPNAME","DOSE",
                                                    "MODDT","MODOP","MODNAME","DRUGNAME","DRUGTYPE",
                                                    "ROUTE","DOSEUNIT","STATUS","SDID",
                                                    "REASON","REASONTYPE","INJECTION","PAGE"};
                        string[] datatype_upcrcp = { "String", "String", "String", "String",
                                                         "String", "String", "String","String",
                                                         "String", "String", "String","String","String",
                                                         "String", "String", "String", "String",
                                                         "String", "String", "String", "String"};
                        set_dt_column(dt_bs, UpcrcpTime);
                        DataRow dr = dt_bs.NewRow();
                        for (int i = 0; i < dt_bs.Columns.Count; i++)
                        {
                            dr[i] = datatype_upcrcp[i];

                        }
                        dt_bs.Rows.Add(dr);
                        //塞入datatype
                        dr = dt_bs.NewRow();

                        dr["FEENO"] = feeno;
                        dr["INDATE"] = data[0].INDATE;
                        dr["POSITION"] = data[0].POSITION;
                        dr["REVIEW"] = data[0].REVIEW;
                        //if (data[0].SYMPTOM != null) { dr["SYMPTOM"] = data[0].SYMPTOM.TrimEnd(','); }
                        //if (data[0].DEALWITH != null) { dr["DEALWITH"] = data[0].DEALWITH.TrimEnd(','); }
                        //dr["MONITOR"] = data[0].MONITOR;
                        dr["INSDT"] = (DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss"));
                        dr["INSOP"] = userinfo.EmployeesNo;
                        dr["INSOPNAME"] = userinfo.EmployeesName;
                        dr["DRUGNAME"] = data[0].DRUGNAME;
                        dr["DRUGTYPE"] = data[0].DRUGTYPE;
                        dr["DOSE"] = data[0].DOSE;
                        dr["ROUTE"] = data[0].ROUTE;
                        dr["DOSEUNIT"] = data[0].DOSEUNIT;
                        dr["STATUS"] = data[0].STATUS;
                        dr["SDID"] = (DateTime.Now.ToString("yyyyMMddhhmmss"));
                        dr["REASON"] = data[0].REASON;
                        dr["REASONTYPE"] = data[0].REASONTYPE;
                        dr["INJECTION"] = userinfo.EmployeesName;
                        dr["PAGE"] = data[0].PAGE;

                        dt_bs.Rows.Add(dr);




                        if (data_bs)
                        {
                            //確認是否有存資料 及有無成功
                            int erow = bai.insert("SPECIALDRUG", dt_bs);
                            if (erow < 1)
                                success_dt = false;
                            //儲存成功
                            if (success_dt)
                            {
                                return Redirect("../BloodSugarAndInsulin/Insulin_List?success=yes");
                            }
                            else
                            {                       //儲存失敗
                                return Redirect("../BloodSugarAndInsulin/Insulin_List?success=no");
                            }
                        }

                    }
                    #endregion
                }
                #endregion
                return View();

            }
            return Redirect("../VitalSign/VitalSign_Single");

        }
        public ActionResult Insulin_Insert()
        {
            List<SelectListItem> listitem = new List<SelectListItem>();
            listitem.Add(new SelectListItem { Text = "一般", Value = "N" });
            listitem.Add(new SelectListItem { Text = "S", Value = "S" });
            listitem.Add(new SelectListItem { Text = "O", Value = "O" });
            listitem.Add(new SelectListItem { Text = "I", Value = "I" });
            listitem.Add(new SelectListItem { Text = "E", Value = "E" });
            ViewData["List"] = listitem;
            List<SelectListItem> listitem2 = new List<SelectListItem>();
            listitem2.Add(new SelectListItem { Text = "MIXTARD 30HM", Value = "1" });
            listitem2.Add(new SelectListItem { Text = "HUMULIN N", Value = "2" });
            listitem2.Add(new SelectListItem { Text = "HUMULIN R", Value = "3" });
            listitem2.Add(new SelectListItem { Text = "penfill Navorapid", Value = "4" });
            listitem2.Add(new SelectListItem { Text = "penfill Insulatard HM", Value = "5" });
            listitem2.Add(new SelectListItem { Text = "Lantus", Value = "6" });
            listitem2.Add(new SelectListItem { Text = "novomix 30 penfill", Value = "7" });
            listitem2.Add(new SelectListItem { Text = "其他", Value = "8" });
            ViewData["List2"] = listitem2;
            List<SelectListItem> listitem3 = new List<SelectListItem>();
            listitem3.Add(new SelectListItem { Text = "拒絕", Value = "" });
            listitem3.Add(new SelectListItem { Text = "禁食", Value = "" });
            listitem3.Add(new SelectListItem { Text = "血糖偏低", Value = "" });
            listitem3.Add(new SelectListItem { Text = "其他", Value = "" });
            ViewData["List3"] = listitem3;

            return View();
        }

        public ActionResult Insulin_Insert2()//新畫面
        {
            List<SelectListItem> listitem = new List<SelectListItem>();

            listitem.Add(new SelectListItem { Text = "01", Value = "" });
            listitem.Add(new SelectListItem { Text = "02", Value = "" });
            listitem.Add(new SelectListItem { Text = "03", Value = "" });
            listitem.Add(new SelectListItem { Text = "04", Value = "" });
            listitem.Add(new SelectListItem { Text = "05", Value = "" });
            listitem.Add(new SelectListItem { Text = "06", Value = "" });
            listitem.Add(new SelectListItem { Text = "07", Value = "" });
            listitem.Add(new SelectListItem { Text = "08", Value = "" });
            listitem.Add(new SelectListItem { Text = "09", Value = "" });
            listitem.Add(new SelectListItem { Text = "10", Value = "" });
            listitem.Add(new SelectListItem { Text = "11", Value = "" });
            listitem.Add(new SelectListItem { Text = "12", Value = "" });
            listitem.Add(new SelectListItem { Text = "13", Value = "" });
            listitem.Add(new SelectListItem { Text = "14", Value = "" });
            listitem.Add(new SelectListItem { Text = "15", Value = "" });
            listitem.Add(new SelectListItem { Text = "16", Value = "" });
            listitem.Add(new SelectListItem { Text = "17", Value = "" });
            listitem.Add(new SelectListItem { Text = "18", Value = "" });
            listitem.Add(new SelectListItem { Text = "19", Value = "" });
            listitem.Add(new SelectListItem { Text = "20", Value = "" });
            listitem.Add(new SelectListItem { Text = "21", Value = "" });
            listitem.Add(new SelectListItem { Text = "22", Value = "" });
            listitem.Add(new SelectListItem { Text = "23", Value = "" });
            listitem.Add(new SelectListItem { Text = "24", Value = "" });
            listitem.Add(new SelectListItem { Text = "25", Value = "" });
            listitem.Add(new SelectListItem { Text = "26", Value = "" });
            listitem.Add(new SelectListItem { Text = "27", Value = "" });
            listitem.Add(new SelectListItem { Text = "28", Value = "" });
            listitem.Add(new SelectListItem { Text = "29", Value = "" });
            listitem.Add(new SelectListItem { Text = "30", Value = "" });
            listitem.Add(new SelectListItem { Text = "31", Value = "" });
            listitem.Add(new SelectListItem { Text = "32", Value = "" });
            listitem.Add(new SelectListItem { Text = "33", Value = "" });
            listitem.Add(new SelectListItem { Text = "34", Value = "" });
            listitem.Add(new SelectListItem { Text = "35", Value = "" });
            listitem.Add(new SelectListItem { Text = "36", Value = "" });
            listitem.Add(new SelectListItem { Text = "37", Value = "" });
            listitem.Add(new SelectListItem { Text = "38", Value = "" });
            listitem.Add(new SelectListItem { Text = "39", Value = "" });
            listitem.Add(new SelectListItem { Text = "40", Value = "" });
            listitem.Add(new SelectListItem { Text = "41", Value = "" });
            listitem.Add(new SelectListItem { Text = "42", Value = "" });
            listitem.Add(new SelectListItem { Text = "43", Value = "" });
            listitem.Add(new SelectListItem { Text = "44", Value = "" });
            listitem.Add(new SelectListItem { Text = "45", Value = "" });
            listitem.Add(new SelectListItem { Text = "46", Value = "" });
            listitem.Add(new SelectListItem { Text = "47", Value = "" });
            listitem.Add(new SelectListItem { Text = "48", Value = "" });
            listitem.Add(new SelectListItem { Text = "49", Value = "" });
            listitem.Add(new SelectListItem { Text = "50", Value = "" });
            listitem.Add(new SelectListItem { Text = "51", Value = "" });
            listitem.Add(new SelectListItem { Text = "52", Value = "" });
            listitem.Add(new SelectListItem { Text = "53", Value = "" });
            listitem.Add(new SelectListItem { Text = "54", Value = "" });
            listitem.Add(new SelectListItem { Text = "55", Value = "" });
            listitem.Add(new SelectListItem { Text = "56", Value = "" });
            listitem.Add(new SelectListItem { Text = "57", Value = "" });
            listitem.Add(new SelectListItem { Text = "58", Value = "" });
            listitem.Add(new SelectListItem { Text = "59", Value = "" });

            ViewData["List"] = listitem;
            List<SelectListItem> listitem2 = new List<SelectListItem>();
            listitem2.Add(new SelectListItem { Text = "MIXTARD 30HM", Value = "" });
            listitem2.Add(new SelectListItem { Text = "HUMULIN N", Value = "" });
            listitem2.Add(new SelectListItem { Text = "HUMULIN R", Value = "" });
            listitem2.Add(new SelectListItem { Text = "penfill Navorapid", Value = "" });
            listitem2.Add(new SelectListItem { Text = "penfill Insulatard HM", Value = "" });
            listitem2.Add(new SelectListItem { Text = "Lantus", Value = "" });
            listitem2.Add(new SelectListItem { Text = "novomix 30 penfill", Value = "" });
            listitem2.Add(new SelectListItem { Text = "其他", Value = "" });
            ViewData["List2"] = listitem2;
            List<SelectListItem> listitem3 = new List<SelectListItem>();
            listitem3.Add(new SelectListItem { Text = "拒絕", Value = "" });
            listitem3.Add(new SelectListItem { Text = "禁食", Value = "" });
            listitem3.Add(new SelectListItem { Text = "血糖偏低", Value = "" });
            listitem3.Add(new SelectListItem { Text = "其他", Value = "" });
            ViewData["List3"] = listitem3;

            return View();
        }
        //public ActionResult Insulin_List()
        //{
        //    return View();
        //}

        public ActionResult Insulin_Set_reject()
        {
            return View();
        }
        #endregion

        #region 新版

        #region 清單 BSugarInsulin_List
        public ActionResult BSugarInsulin_List()
        {//判斷有無病人session
            if (Session["PatInfo"] != null)
            {
                //宣告病患_取得住院序號
                PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
                string feeno = ptInfo.FeeNo;
                ViewBag.feeno = feeno;

                DataTable dt_bsi = bai.get_BSugarInsulin_list(feeno, "", "", "");
                if (dt_bsi.Rows.Count == 0)
                    ViewBag.dt_bsi = null;
                else
                    ViewBag.dt_bsi = dt_bsi;
                /*DataRow dr = dt_bsi.NewRow();

                foreach (DataRow drr in dt_bsi.Rows)
                {
                    if (drr["B_INDATE"].ToString() == "")
                    {
                        dr = drr;
                    }
                }
                dt_bsi.Rows.InsertAt(dr, 1);


                if (li_bsugar  >= li_insulin)
                {
                    foreach (DataRow dr in dt_bsugar.Rows)
                    {
                        dr["BLOODSUGAR"] = "";
                    }
                }
                else
                { 

                }
                */
                return View();
            }
            else
            {
                Response.Write("<script>alert('請重新選擇病患');</script>");
                return new EmptyResult();
            }
        }
        #endregion

        #region 清單 BSugarInsulin_List2
        public ActionResult BSugarInsulin_List2()
        {//判斷有無病人session
            if (Session["PatInfo"] != null)
            {
                //宣告病患_取得住院序號
                PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
                string feeno = ptInfo.FeeNo;
                bool lb_use = false;
                ViewBag.feeno = feeno;

                DataTable dt_BSugar = bai.get_BSugarInsulin_list(feeno, "B", "", "");
                DataTable dt_Insulin = bai.get_BSugarInsulin_list(feeno, "I", "", "");
                List<BloodSugarInsulin_List> BloodSugar_List = new List<BloodSugarInsulin_List>();
                List<BloodSugarInsulin_List> Insulin_List = new List<BloodSugarInsulin_List>();

                BloodSugar_List = (from a in dt_BSugar.AsEnumerable()
                                   select new BloodSugarInsulin_List()
                                   {
                                       BLOODSUGAR = a.Field<string>("BLOODSUGAR"),
                                       INSOPNAME = a.Field<string>("INSOPNAME"),
                                       B_INDATE = a.Field<string>("B_INDATE"),
                                       MEAL_STATUS = a.Field<string>("MEAL_STATUS"),
                                       CHECK_FLAG = "N"
                                   }).ToList<BloodSugarInsulin_List>();

                Insulin_List = (from a in dt_Insulin.AsEnumerable()
                                select new BloodSugarInsulin_List()
                                {
                                    I_INDATE = a.Field<string>("I_INDATE"),
                                    IN_DRUGNAME = a.Field<string>("IN_DRUGNAME"),
                                    IN_DOSE = a.Field<decimal>("IN_DOSE").ToString("0.#"),
                                    IN_DOSEUNIT = a.Field<string>("IN_DOSEUNIT"),
                                    POSITION = a.Field<string>("POSITION"),
                                    INJECTION = a.Field<string>("INJECTION"),
                                    SS_DRUGNAME = a.Field<string>("SS_DRUGNAME"),
                                    SS_DOSE = a.Field<decimal>("SS_DOSE").ToString("#.#"),
                                    CHECK_FLAG = "N"
                                }).ToList<BloodSugarInsulin_List>();
                List<BloodSugarInsulin_List> BSugarInsulin_List = new List<BloodSugarInsulin_List>();
                int tmpcount = 0;
                for (int i = 0; i <= BloodSugar_List.Count - 1; i++)
                {
                    if (BloodSugar_List[i].BLOODSUGAR.ToString().IndexOf("#") == -1)
                        tmpcount++;

                    DateTime Date_B = new DateTime();
                    //DateTime.TryParse(BloodSugar_List[i].B_INDATE, out Date_B);
                    Date_B = Convert.ToDateTime(BloodSugar_List[i].B_INDATE.ToString());

                    DateTime Date_B2 = Date_B;
                    if (i + 1 < BloodSugar_List.Count)
                        //DateTime.TryParse(BloodSugar_List[i+1].B_INDATE, out Date_B2);
                        Date_B2 = Convert.ToDateTime(BloodSugar_List[i + 1].B_INDATE.ToString());

                    for (int j = 0; j <= Insulin_List.Count - 1; j++)
                    {
                        DateTime Date_I = new DateTime();
                        //DateTime.TryParse(BloodSugar_List[j].B_INDATE, out Date_I);
                        Date_I = Convert.ToDateTime(Insulin_List[j].I_INDATE.ToString());
                        int a = DateTime.Compare(Date_B, Date_I);
                        if (DateTime.Compare(Date_B, Date_I) <= 0 && DateTime.Compare(Date_B2, Date_I) == 1 && Insulin_List[j].CHECK_FLAG.ToString() != "Y" ||
                            (i == 0 && DateTime.Compare(Date_B, Date_I) >= 0) || (i == BloodSugar_List.Count - 1 && DateTime.Compare(Date_B, Date_I) == -1))
                        {
                            BSugarInsulin_List.Insert(0, new BloodSugarInsulin_List()
                            {
                                BLOODSUGAR = BloodSugar_List[i].BLOODSUGAR.ToString(),
                                INSOPNAME = BloodSugar_List[i].INSOPNAME.ToString(),
                                B_INDATE = BloodSugar_List[i].B_INDATE.ToString(),
                                //B_INDATE = string.IsNullOrEmpty(BloodSugar_List[i].B_INDATE) ? "": BloodSugar_List[i].B_INDATE.ToString(),
                                I_INDATE = Insulin_List[j].I_INDATE.ToString(),
                                IN_DRUGNAME = Insulin_List[j].IN_DRUGNAME.ToString(),
                                IN_DOSE = Insulin_List[j].IN_DOSE.ToString(),
                                IN_DOSEUNIT = Insulin_List[j].IN_DOSEUNIT,
                                POSITION = Insulin_List[j].POSITION,
                                INJECTION = Insulin_List[j].INJECTION.ToString(),
                                SS_DRUGNAME = Insulin_List[j].SS_DRUGNAME.ToString(),
                                SS_DOSE = Insulin_List[j].SS_DOSE.ToString(),
                                MEAL_STATUS = BloodSugar_List[i].MEAL_STATUS.ToString()
                            });
                            Insulin_List[j].CHECK_FLAG = "Y";
                            BloodSugar_List[i].CHECK_FLAG = "Y";
                            lb_use = true;
                        }
                    }
                    if (lb_use == false || BloodSugar_List[i].CHECK_FLAG == "N")
                    {
                        BSugarInsulin_List.Insert(0, new BloodSugarInsulin_List()
                        {
                            BLOODSUGAR = BloodSugar_List[i].BLOODSUGAR.ToString(),
                            INSOPNAME = BloodSugar_List[i].INSOPNAME.ToString(),
                            B_INDATE = BloodSugar_List[i].B_INDATE.ToString(),
                            //B_INDATE = string.IsNullOrEmpty(BloodSugar_List[i].B_INDATE) ? "" : BloodSugar_List[i].B_INDATE.ToString(),
                            MEAL_STATUS = BloodSugar_List[i].MEAL_STATUS,
                            I_INDATE = "",
                            IN_DRUGNAME = "",
                            IN_DOSE = "",
                            IN_DOSEUNIT = "",
                            POSITION = "",
                            INJECTION = "",
                            SS_DRUGNAME = "",
                            SS_DOSE = "",
                        });
                        BloodSugar_List[i].CHECK_FLAG = "Y";
                    }
                }

                for (int j = Insulin_List.Count - 1; j >= 0; j--)
                {
                    if (Insulin_List[j].CHECK_FLAG.ToString() != "Y")
                    {
                        // if (BSugarInsulin_List.Count > 0)
                        {
                            BSugarInsulin_List.Insert(0, new BloodSugarInsulin_List()
                            {
                                BLOODSUGAR = "",
                                INSOPNAME = "",
                                B_INDATE = "",
                                I_INDATE = Insulin_List[j].I_INDATE.ToString(),
                                IN_DRUGNAME = Insulin_List[j].IN_DRUGNAME.ToString(),
                                IN_DOSE = Insulin_List[j].IN_DOSE.ToString(),
                                IN_DOSEUNIT = Insulin_List[j].IN_DOSEUNIT,
                                POSITION = Insulin_List[j].POSITION,
                                INJECTION = Insulin_List[j].INJECTION.ToString(),
                                SS_DRUGNAME = Insulin_List[j].SS_DRUGNAME.ToString(),
                                SS_DOSE = Insulin_List[j].SS_DOSE.ToString()
                            });
                            Insulin_List[j].CHECK_FLAG = "Y";
                        }

                    }
                }
                for (int i = BloodSugar_List.Count - 1; i >= 0; i--)
                {
                    if (BloodSugar_List[i].CHECK_FLAG.ToString() != "Y")
                    {
                        if (BSugarInsulin_List.Count > 0)
                            BSugarInsulin_List.Insert(0, new BloodSugarInsulin_List()
                            {
                                BLOODSUGAR = BloodSugar_List[i].BLOODSUGAR.ToString(),
                                INSOPNAME = BloodSugar_List[i].INSOPNAME.ToString(),
                                B_INDATE = BloodSugar_List[i].B_INDATE.ToString(),
                                //B_INDATE = string.IsNullOrEmpty(BloodSugar_List[i].B_INDATE) ? "" : BloodSugar_List[i].B_INDATE.ToString(),
                                MEAL_STATUS = BloodSugar_List[i].MEAL_STATUS,
                                I_INDATE = "",
                                IN_DRUGNAME = "",
                                IN_DOSE = "",
                                IN_DOSEUNIT = "",
                                POSITION = "",
                                INJECTION = "",
                                SS_DRUGNAME = "",
                                SS_DOSE = "",
                            });
                    }
                }
                if (BSugarInsulin_List.Count == 0)
                    ViewData["BSugarInsulin_List"] = null;
                else
                    ViewData["BSugarInsulin_List"] = BSugarInsulin_List;

                ViewBag.b_count = "血糖監測次數：" + BloodSugar_List.Count + "(含手動輸入：" + tmpcount + ")，";
                ViewBag.i_count = "胰島素注射次數：" + Insulin_List.Count;
                return View();
            }
            else
            {
                Response.Write("<script>alert('請重新選擇病患');</script>");
                return new EmptyResult();
            }
        }
        #endregion

        #region 主頁 index
        public ActionResult index()
        {//判斷有無病人session
            if (Session["PatInfo"] != null)
            {
                //宣告病患_取得住院序號
                PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
                string feeno = ptInfo.FeeNo;
                ViewBag.feeno = feeno;

                DataTable dt_bsi = bai.get_BSugarInsulin(feeno);
                ViewBag.dt_bsi = dt_bsi;
                return View();
            }
            else
            {
                Response.Write("<script>alert('請重新選擇病患');</script>");
                return new EmptyResult();
            }
        }
        #endregion

        #region 新增 BSugarInsulin_Add
        public ActionResult BSugarInsulin_Add(string feeno)
        {
            ViewBag.feeno = feeno;
            string Today_datetime = DateTime.Now.AddHours(+1).ToString("yyyy/MM/dd HH:mm:ss");
            DataTable dt_udorder = new DataTable();
            DataTable dt_med = new DataTable();

            byte[] doByteCode = webService.GetUdOrder(feeno, "A");
            if (doByteCode == null)
            {
                Response.Write("Error");
                return View();
            }

            string doJsonArr = CompressTool.DecompressString(doByteCode);
            List<UdOrder> GetUdOrderList = JsonConvert.DeserializeObject<List<UdOrder>>(doJsonArr);
            //dt_udorder = ConvertToDataTable(GetUdOrderList);
            var insulin_med = from a in GetUdOrderList
                              where a.DRUG_TYPE == "E" && a.UD_STATUS == "2"
                              select new
                              {
                                  MED_CODE = a.MED_CODE.Trim(),
                                  UD_DOSE = a.UD_DOSE,
                                  UD_CIR = a.UD_CIR,
                                  UD_UNIT = a.UD_UNIT,
                                  MED_DESC = a.MED_DESC.Trim(),
                                  UD_TYPE = a.UD_TYPE,
                                  UD_SEQ = a.UD_SEQ
                              };
            List<SelectListItem> Insulit_list = new List<SelectListItem>();
            Insulit_list.Add(new SelectListItem { Text = "請選擇", Value = "0" });
            if (insulin_med.Count() > 0)
            {
                string str_tmp;

                foreach (var tmp in insulin_med)
                {
                    if (tmp.UD_TYPE == "R")
                    {
                        str_tmp = "常規 => " + tmp.MED_DESC;
                    }
                    else
                    {
                        str_tmp = "PRN => " + tmp.MED_DESC;
                    }
                    Insulit_list.Add(new SelectListItem { Text = str_tmp, Value = tmp.UD_SEQ });
                }
            }
            ViewBag.insulin_med = insulin_med;
            ViewBag.Insulit_list = Insulit_list;
            return View();
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
        #endregion

        #region 修改 BSugarInsulin_Modify
        public ActionResult BSugarInsulin_Modify(string id)
        {
            ViewBag.id = id;
            return View();
        }
        #endregion

        #region 儲存 BSugarInsulin_Save
        public ActionResult BSugarInsulin_Save(string id)
        {

            return RedirectToAction("index", new { @message = "儲存成功" });
        }
        #endregion

        #endregion

        #region 儀器上傳自動產生血糖紀錄
        public ActionResult BSugarInsulin_AutoGenerate(string UserID = "", string StartTime = "", string EndTime = "")
        {
            RESPONSE_MSG result = new RESPONSE_MSG();
            if (string.IsNullOrEmpty(StartTime) || string.IsNullOrEmpty(EndTime))
            {
                StartTime = DateTime.Now.AddDays(-3).ToString("yyyy/MM/dd 00:00");
                EndTime = DateTime.Now.AddDays(1).ToString("yyyy/MM/dd 00:00");
            }
            var TEMPREADINGUP_Table = "POCT.TEMPREADINGUP";

            if (mode == "Maya")
            {
                StartTime = "2025-03-07 00:00";
                EndTime = "2025-03-19 00:00";
                TEMPREADINGUP_Table = "TEMPREADINGUP";
            }

            // 取得未同步資料
            DataTable UnsyncedData = bai.Get_Unsynced_TEMPREADING(StartTime, EndTime, UserID);
            if (UnsyncedData.Rows.Count == 0)
            {
                result.status = RESPONSE_STATUS.SUCCESS;
                result.message = "無未同步資料";
                return Content(JsonConvert.SerializeObject(result), "application/json");
            }

            // 取得篩選用部門代碼列表
            DataTable CostCodeTable = bai.Get_BloodSugar_CostCodeList();
            List<string> CostCodeList = new List<string>();
            if (CostCodeTable.Rows.Count == 0)
            {
                result.status = RESPONSE_STATUS.ERROR;
                result.message = "無部門代碼列表";
                return Content(JsonConvert.SerializeObject(result), "application/json");
            }
            CostCodeList = CostCodeTable.AsEnumerable().Select(x => x["P_VALUE"].ToString()).ToList();

            InHistory Ipd = new InHistory();
            UserInfo userInfo = new UserInfo();

            for (var i = 0; i < UnsyncedData.Rows.Count; i++)
            {
                try
                {
                    // 若資料的部門代碼不符合就跳過
                    if (!CostCodeList.Contains(UnsyncedData.Rows[i]["OPERATORDEPT"].ToString().Trim()))
                    {
                        continue;
                    }

                    // 若有血糖紀錄就跳過
                    var DataIsExistSQL = "SELECT * FROM BLOODSUGAR WHERE MACHINE_UPLOAD_ID = '" + UnsyncedData.Rows[i]["ID"].ToString().Trim() + "'";
                    DataTable DataIsExist = link.DBExecSQL(DataIsExistSQL);
                    if (DataIsExist.Rows.Count > 0)
                    {
                        continue;
                    }

                    var ChartNo = UnsyncedData.Rows[i]["PID"].ToString().Trim();
                    var FeeNo = "";
                    var CostCode = "";
                    if (string.IsNullOrEmpty(Ipd.ChrNo) || Ipd.ChrNo.Trim() != ChartNo)
                    {
                        Ipd = new InHistory();
                        byte[] doByteCode = webService.GetInHistory(ChartNo);
                        if (doByteCode != null)
                        {
                            string doJsonArr = CompressTool.DecompressString(doByteCode);
                            List<InHistory> IpdList = JsonConvert.DeserializeObject<List<InHistory>>(doJsonArr);
                            Ipd = IpdList.OrderByDescending(x => x.indate).FirstOrDefault();
                        }
                        else
                        {
                            continue;
                        }
                    }
                    FeeNo = string.IsNullOrEmpty(Ipd.FeeNo) ? "" : Ipd.FeeNo.Trim();
                    CostCode = string.IsNullOrEmpty(Ipd.CostCode) ? "" : Ipd.CostCode.Trim();
                    DateTime DataTime = new DateTime();
                    DateTime.TryParse(UnsyncedData.Rows[i]["DATETIME"].ToString(), out DataTime);
                    var userID = UnsyncedData.Rows[i]["OPERATORID"].ToString().Trim();
                    var userName = "";
                    if (string.IsNullOrEmpty(userInfo.UserID) || userInfo.UserID.Trim() != userID)
                    {
                        byte[] doByteCode = webService.UserName(userID);
                        if (doByteCode != null)
                        {
                            string doJsonArr = CompressTool.DecompressString(doByteCode);
                            userInfo = JsonConvert.DeserializeObject<UserInfo>(doJsonArr);
                        }
                    }
                    userName = string.IsNullOrEmpty(userInfo.EmployeesName) ? "" : userInfo.EmployeesName.Trim();
                    var mealStatus = "";
                    switch (UnsyncedData.Rows[i]["MEDICALORDER"].ToString().Trim())
                    {
                        case "AC":
                            mealStatus = "B";
                            break;
                        case "PC":
                            mealStatus = "A";
                            break;
                        case "ST":
                            mealStatus = "C";
                            break;
                    }
                    var ID = UnsyncedData.Rows[i]["ID"].ToString().Trim();
                    var DateTimeNow = DateTime.Now;
                    var OriginalReading = UnsyncedData.Rows[i]["READING"].ToString().Trim();
                    var Reading = "";
                    var CareRecordReading = "";
                    switch (OriginalReading)
                    {
                        case "<20":
                            Reading = "1";
                            CareRecordReading = "Low";
                            break;
                        case ">600":
                            Reading = "999";
                            CareRecordReading = "High";
                            break;
                        default:
                            Reading = OriginalReading;
                            CareRecordReading = OriginalReading;
                            break;
                    }

                    List<DBItem> insertDataList = new List<DBItem>();
                    insertDataList.Add(new DBItem("FEENO", FeeNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("INDATE", DataTime.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("BLOODSUGAR", Reading.Trim(), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("NOTE", "", DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("INSDT", DateTimeNow.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("INSOP", userID, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("INSOPNAME", userName, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("STATUS", "new", DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("BSID", DateTimeNow.ToString("yyyyMMddHHmmssfff"), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("COST_CODE", CostCode, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("MEAL_STATUS", mealStatus, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("MACHINE_UPLOAD_ID", ID, DBItem.DBDataType.String));
                    int erow = link.DBExecInsert("BLOODSUGAR", insertDataList);

                    string CareRecord_String = string.Format("{0} {1}，血糖值：{2}。"
                            , DataTime.ToString("yyyy/MM/dd HH:mm:ss")
                            , "測量種類：" + mealStatus.Replace("C", "不清楚").Replace("B", "飯前").Replace("A", "飯後")
                    , CareRecordReading + "mg/dl");

                    if (erow > 0)
                    {
                        erow = Insert_CareRecord_MachineUpload(DataTime.ToString("yyyy/MM/dd HH:mm:ss"), DateTimeNow.ToString("yyyyMMddHHmmssfff"), "", "", "", CareRecord_String, "", "", "BloodSugar", userID, userName, FeeNo);
                    }
                    else
                    {
                        this.log.saveLogMsg($"[新增血糖紀錄失敗] 儀器上傳ID:{UnsyncedData.Rows[i]["ID"].ToString().Trim()}", "BSugarInsulin_AutoGenerate");
                    }

                    if (erow > 0)
                    {
                        // 成功新增紀錄後於TEMPREADINGUP表中新增註記
                        insertDataList = new List<DBItem>();
                        insertDataList.Add(new DBItem("ID", ID, DBItem.DBDataType.Number));
                        insertDataList.Add(new DBItem("UP_SYSTEM", "NIS", DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("UP_DATETIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("UP_STATUS", "Y", DBItem.DBDataType.String));
                        erow = link.DBExecInsert(TEMPREADINGUP_Table, insertDataList);
                        if (erow > 0)
                        {
                            if (string.IsNullOrEmpty(result.message))
                            {
                                result.message += "成功上傳ID:";
                            }
                            result.message += $"{UnsyncedData.Rows[i]["ID"].ToString().Trim()},";
                        }
                        else
                        {
                            this.log.saveLogMsg($"[血糖資料註記失敗] 儀器上傳ID:{UnsyncedData.Rows[i]["ID"].ToString().Trim()}", "BSugarInsulin_AutoGenerate");
                        }
                    }
                    else
                    {
                        var whereSQL = "MACHINE_UPLOAD_ID = '" + UnsyncedData.Rows[i]["ID"].ToString().Trim() + "'";
                        link.DBExecDelete("BLOODSUGAR", whereSQL);
                        whereSQL = "CARERECORD_ID = '" + "BloodSugar_MachineUpload_" + ID + "_" + DateTimeNow.ToString("yyyyMMddHHmmssfff") + "'";
                        link.DBExecDelete("CARERECORD_DATA", whereSQL);
                        this.log.saveLogMsg($"[新增血糖護理紀錄失敗] 儀器上傳ID:{UnsyncedData.Rows[i]["ID"].ToString().Trim()} CARERECORD_ID:{"BloodSugar_MachineUpload_" + ID + "_" + DateTimeNow.ToString("yyyyMMddHHmmssfff")}", "BSugarInsulin_AutoGenerate");
                    }
                }
                catch (Exception ex)
                {
                    this.log.saveLogMsg($"[新增血糖紀錄失敗] 儀器上傳ID:{UnsyncedData.Rows[i]["ID"].ToString().Trim()} Error:{ex.Message}", "BSugarInsulin_AutoGenerate");
                }
            }

            result.status = RESPONSE_STATUS.SUCCESS;
            return Content(JsonConvert.SerializeObject(result), "application/json");
        }
        #endregion
    }
}
