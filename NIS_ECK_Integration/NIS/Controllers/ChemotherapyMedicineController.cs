using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using NIS.Controllers;
using NIS.Data;
using NIS.Models;
using NIS.UtilTool;
using NIS.WebService;

namespace NIS.Controllers
{
    public class ChemotherapyMedicineController : BaseController
    {
        private CommData cd;    //常用資料Module
        private CommonMedicine cm;
        private ChemotherapyMedicine chemo;
        private DBConnector link;

        public ChemotherapyMedicineController()
        {
            this.link = new DBConnector();
            this.cd = new CommData();
            this.cm = new CommonMedicine();
            this.chemo = new ChemotherapyMedicine();
        }

          public class chemolist
        {
            public string ck { get; set; }
            public string ORDSEQ { get; set; }
            public string SHEETNO { get; set; }
            public string SEGUENCE { get; set; }
            public string REASONTYPE { get; set; }
            public string REASON { get; set; }
            public string INSDT { get; set; }
            public string DOSE { get; set; }
            public string DOSEUNIT { get; set; }
            public string STATUS { get; set; }
            public string ROUTE { get; set; }
            public string FEQ { get; set; }
            public string ORDERSTARTDATE { get; set; }
            public string ORDERENDDATE { get; set; }          
            public string MEMO { get; set; }
            public string GENERICDRUGS { get; set; }
            public string TOTAL { get; set; }          
            public string DRUGNAME { get; set; }
            public string RATE {get;set;}
            public string CHECK1 { get; set; }
            public string CHECK2 { get; set; }
            public string BADREACTION { get; set; }
            public string MODDOSE { get; set; }
        }

      
        [HttpGet]
        public ActionResult Execute()
        {//判斷有無病人session
            if (Session["PatInfo"] != null)
            {
                //宣告病患_取得住院序號
                PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
                string feeno = ptInfo.FeeNo;
                //UserInfo ui = (UserInfo)Session["UserInfo"];

                if (webService.GetChemotherapy(feeno) != null)
                {

                    byte[] ctdByteCode0 = webService.GetChemotherapy(feeno);
                    string ctdJsonArr0 = CompressTool.DecompressString(ctdByteCode0);
                    List<ChemotherapyStatus> ChemotherapyStatusList = JsonConvert.DeserializeObject<List<ChemotherapyStatus>>(ctdJsonArr0);
                    ViewData["ChemotherapyStatusList"] = ChemotherapyStatusList;
                    ChemotherapyStatusList = null;


                    byte[] ctdByteCode = webService.GetChemotherapyDrugOrder(feeno);
                    string ctdJsonArr = CompressTool.DecompressString(ctdByteCode);
                    List<ChemotherapyDrugOrder> chemoOrderList = JsonConvert.DeserializeObject<List<ChemotherapyDrugOrder>>(ctdJsonArr);
                    ViewData["chemoOrderList"] = chemoOrderList;

                    #region 宣告UpcrcpTime 儲存資料

                    DataTable dt_ck = new DataTable();
                    string[] UpcrcpTime = { "ORDSEQ", "FEENO","SHEETNO","TOTAL", "RATE","INSDT","MEMO",
                                                "SEGUENCE","DOSE","DOSEUNIT","ROUTE" ,"FEQ","DRUGNAME",
                                                "ORDERSTARTDATE","ORDERENDDATE","STATUS","GENERICDRUGS",
                                                "DUR_TIME","INFUSIONSOLUTION"
                                                };
                    string[] datatype_upcrcp = { "String", "String", "String", "String","String","String",
                                                     "String", "String", "String", "String", "String","String",
                                                     "String", "String", "String", "String", "String",
                                                     "String", "String"
                                           };
                    set_dt_column(dt_ck, UpcrcpTime);
                    DataRow _dr = dt_ck.NewRow();
                    bool data_ordseq = false;
                    for (int i = 0; i < dt_ck.Columns.Count; i++)
                    {
                        _dr[i] = datatype_upcrcp[i];
                    }
                    dt_ck.Rows.Add(_dr);
                    //塞入datatype
                    for (int i = 0; i < chemoOrderList.Count; i++)
                    {
                        if (chemoOrderList[i] != null)
                        {
                            if (cm.ck_sheet(feeno, chemoOrderList[i].SheetNo, "chemo"))
                            {
                                data_ordseq = true;
                                _dr = dt_ck.NewRow();

                                //string[] item = data[i].Ordseq.ToString().Split('_');
                                //string[] reason = data[i].Remark.ToString().Split(':');
                                _dr["FEENO"] = feeno;
                                _dr["ORDSEQ"] = chemoOrderList[i].OrderNo;
                                _dr["SHEETNO"] = chemoOrderList[i].SheetNo;
                                _dr["SEGUENCE"] = chemoOrderList[i].Sequence;
                                _dr["INSDT"] = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
                                _dr["DOSE"] = chemoOrderList[i].Dose.ToString();
                                _dr["DOSEUNIT"] = chemoOrderList[i].DoseUnit.ToString();
                                _dr["ORDERSTARTDATE"] = chemoOrderList[i].OrderStartDate.ToString("yyyy/MM/dd HH:mm:ss");
                                _dr["ORDERENDDATE"] = chemoOrderList[i].OrderEndDate.ToString("yyyy/MM/dd HH:mm:ss");

                                _dr["ROUTE"] = chemoOrderList[i].Route.ToString();
                                _dr["FEQ"] = chemoOrderList[i].Feq.ToString();
                                _dr["MEMO"] = chemoOrderList[i].Memo.ToString();

                                _dr["DRUGNAME"] = chemoOrderList[i].DrugName;
                                _dr["GENERICDRUGS"] = chemoOrderList[i].GenericDrugs.ToString();

                                _dr["TOTAL"] = chemoOrderList[i].Total.ToString();
                                _dr["RATE"] = chemoOrderList[i].Rate.ToString();

                                _dr["DUR_TIME"] = chemoOrderList[i].dur_time.ToString();
                                if (chemoOrderList[i].InfusionSolution != null) { _dr["INFUSIONSOLUTION"] = chemoOrderList[i].InfusionSolution.ToString(); }

                                _dr["STATUS"] = "未給藥";

                                dt_ck.Rows.Add(_dr);
                            }

                        }
                    }

                    //ViewBag.table_check = dt_ck;
                    if (data_ordseq)
                    {
                        int erow = cm.insert("ChemotherapyDrugOrder", dt_ck);
                        data_ordseq = false;
                    }

                    chemoOrderList = null;
                    #endregion
                }
                #region LOAD
                DataTable dt_all = new DataTable();
                dt_all = chemo.sql_udorderinfo("Execute", feeno, "", "", "all", "", "", "", "", "");
                DataTable dt = new DataTable();
                dt = chemo.sql_udorderinfo("Execute", feeno, "", "", "", "", "", "結束", "", "");
                dt_all.Columns.Add("day_time");
                dt.Columns.Add("ck_disabled");
                dt.Columns.Add("day_time");

                foreach (DataRow dr_all in dt_all.Rows)
                {
                    dr_all["day_time"] = (DateTime.Parse(dr_all["ORDERENDDATE"].ToString()) - DateTime.Parse(dr_all["ORDERSTARTDATE"].ToString())).TotalDays;
                }
                foreach (DataRow dr in dt.Rows)
                {
                    dr["day_time"] = (DateTime.Parse(dr["ORDERENDDATE"].ToString()) - DateTime.Parse(dr["ORDERSTARTDATE"].ToString())).TotalDays;
                }
                ViewBag.name1 = userinfo.EmployeesName;
                ViewBag.name2 = "";
                if (dt.Rows.Count > 0 && (dt.Rows[0]["CHECK2"].ToString() != null || dt.Rows[0]["CHECK2"].ToString() != ""))
                { ViewBag.name2 += dt.Rows[0]["CHECK2"]; }
                else { ViewBag.name2 = ""; }
                ViewBag.dt_all = dt_all;
                ViewBag.dt = dt;
                #endregion


                return View();
            }
            else
            {
                Response.Write("<script>alert('請重新選擇病患');</script>");
                return new EmptyResult();
            }
        }
       
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Execute(FormCollection form,List<chemolist> data,string but,string checker)
        {//判斷有無病人session
            if (Session["PatInfo"] != null)
            {
                //宣告病患_取得住院序號
                PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
                string feeno = ptInfo.FeeNo;


                #region SAVE

                if (but == "OK")
                {
                    bool success_dt = true, data_ordseq = false, data_s = false;//預設存資料庫成功
                    if (data != null) //判斷儲存勾選資料是否有無
                    {
                        /*****判別是否有資料 有的話就要做存入資料庫的動作*****/
                        #region 宣告UpcrcpTime
                        DataTable dt_ck = new DataTable();
                        DataTable dt_up = new DataTable();
                        string[] UpcrcpTime = { "ORDSEQ", "FEENO","SHEETNO","TOTAL", "RATE","INSDT","MEMO",
                                                "SEGUENCE","DOSE","DOSEUNIT","ROUTE" ,"FEQ","DRUGNAME",
                                                "ORDERSTARTDATE","ORDERENDDATE","STATUS","GENERICDRUGS",
                                                "CHECK1","CHECK2","REASONTYPE","REASON","BADREACTION","MODDOSE"
                                                };
                        string[] datatype_upcrcp = { "String", "String", "String", "String","String","String",
                                                     "String", "String", "String", "String", "String","String",
                                                     "String", "String", "String", "String", "String" ,
                                                     "String", "String" , "String", "String", "String", "String" };

                        set_dt_column(dt_ck, UpcrcpTime);
                        DataRow dr = dt_ck.NewRow();
                        DataRow dr_up = dt_up.NewRow();
                        for (int i = 0; i < dt_ck.Columns.Count; i++)
                        {
                            dr[i] = datatype_upcrcp[i];
                        }
                        dt_ck.Rows.Add(dr);
                        //塞入datatype 
                        for (int i = 0; i < data.Count; i++)
                        {
                            if (data[i].ck == "0")
                            {
                                if (data[i].CHECK1 == null)
                                {
                                    data_ordseq = true;
                                    dr = dt_ck.NewRow();

                                    dr["FEENO"] = feeno;
                                    dr["ORDSEQ"] = data[i].ORDSEQ;
                                    dr["SHEETNO"] = data[i].SHEETNO;
                                    dr["SEGUENCE"] = data[i].SEGUENCE;
                                    dr["INSDT"] = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
                                    dr["DOSE"] = data[i].DOSE.ToString();
                                    dr["DOSEUNIT"] = data[i].DOSEUNIT;
                                    dr["ORDERSTARTDATE"] = data[i].ORDERSTARTDATE;
                                    dr["ORDERENDDATE"] = data[i].ORDERENDDATE;
                                    dr["ROUTE"] = data[i].ROUTE;
                                    dr["FEQ"] = data[i].FEQ;
                                    dr["MEMO"] = data[i].MEMO;
                                    dr["DRUGNAME"] = data[i].DRUGNAME;
                                    dr["GENERICDRUGS"] = data[i].GENERICDRUGS;
                                    dr["TOTAL"] = data[i].TOTAL;
                                    dr["RATE"] = data[i].RATE;
                                    dr["REASONTYPE"] = data[i].REASONTYPE;
                                    dr["REASON"] = data[i].REASON;
                                    dr["BADREACTION"] = data[i].BADREACTION;
                                    dr["MODDOSE"] = data[i].MODDOSE;

                                    //if (checker != "")
                                    //{
                                    //    dr["STATUS"] = "已覆核";
                                    //    dr["CHECK2"] = checker;
                                    //    if (data[i].CHECK1 == null)
                                    //    {
                                    //        dr["CHECK1"] = userinfo.EmployeesName;
                                    //        dt_up.Columns.Add("CHECK1");
                                    //    }
                                    //    dt_up.Columns.Add("CHECK2");
                                    //}
                                    //else
                                    {
                                        dr["STATUS"] = "待覆核";
                                        dr["CHECK1"] = userinfo.EmployeesName;
                                        dt_up.Columns.Add("CHECK1");
                                    }
                                    dt_ck.Rows.Add(dr);

                                    dt_up.Columns.Add("STATUS");
                                    dt_up.Columns.Add("where");
                                    dr_up = dt_up.NewRow();

                                    //if (checker != "")
                                    //{
                                    //    dr_up["STATUS"] = "已覆核";
                                    //    dr_up["CHECK2"] = checker;
                                    //    if (data[i].CHECK1 == null)
                                    //    {
                                    //        dr_up["CHECK1"] = userinfo.EmployeesName;
                                    //    }
                                    //}
                                    //else
                                    {
                                        dr_up["STATUS"] = "待覆核";
                                        dr_up["CHECK1"] = userinfo.EmployeesName;
                                    }
                                    string where = " FEENO = '" + feeno + "' AND SHEETNO = '" + data[i].SHEETNO + "'";
                                    dr_up["where"] = where;
                                    dt_up.Rows.Add(dr_up);

                                }
                                //else
                                //{ //check有值 先完成 待覆核
                                //    data_ordseq = true;
                                //    data_s = true;
                                //    string where = " FEENO = '" + feeno + "' AND SHEETNO = '" + data[i].SHEETNO + "'";

                                //    dt_ck.Columns.Add("CHECK2");
                                //    dt_ck.Columns.Add("STATUS");
                                //    dt_ck.Columns.Add("where");
                                //    dr = dt_ck.NewRow();
                                //    dr["STATUS"] = "已覆核";
                                //    dr["CHECK2"] = checker;
                                //    dr["where"] = where;
                                //    dt_ck.Rows.Add(dr);

                                //    dt_up.Columns.Add("CHECK2");
                                //    dt_up.Columns.Add("STATUS");
                                //    dt_up.Columns.Add("where");
                                //    dr_up = dt_up.NewRow();

                                //    dr_up["STATUS"] = "已覆核";
                                //    dr_up["CHECK2"] = checker;
                                //    dr_up["where"] = where;
                                //    dt_up.Rows.Add(dr_up);
                                //}


                            }
                            ViewBag.table_check = dt_ck;

                        }

                        if (data_ordseq)
                        {
                            //確認是否有存資料 及有無成功
                            int erow = 0;
                            if (data_s) { erow = chemo.insert("Chemotherapy", dt_ck); }
                            else { erow = chemo.upd("Chemotherapy", dt_ck); }
                            chemo.upd("ChemotherapyDrugOrder", dt_up);
                            if (erow < 1)
                                success_dt = false;
                            //儲存成功
                            if (success_dt)
                            {
                                return Redirect("../ChemotherapyMedicine/Execute?success=yes");
                            }
                            else
                            {                       //儲存失敗
                                return Redirect("../ChemotherapyMedicine/Execute?success=no");
                            }
                        }
                        //  return Redirect("../CommonMedicine/Execute?success=ck");
                    }
                    //沒有新增
                    //   return Redirect("../CommonMedicine/Execute");
                    //return View();
                    #endregion//tb
                }
                if (but == "ckin")
                {
                    string checkname = "";

                    byte[] uData = webService.UserLogin(form["txtid"], form["txtpwd"]);
                    if (uData != null)
                    {
                        string jsonstr = CompressTool.DecompressString(uData);
                        UserInfo user = JsonConvert.DeserializeObject<UserInfo>(jsonstr);
                        checkname = user.EmployeesName.ToString().Trim();
                        ViewBag.name2 = checkname;
                        checker = ViewBag.name2;
                    }
                    else
                    {
                        ViewBag.checkname = null;
                        ViewBag.name2 = null;
                        //帳號或密碼錯誤
                    }


                    //form["name2"];
                    //ViewBag.name2 = "覆核者";
                    // checker = ViewBag.name2;
                    //以上要取得覆核者資料
                    bool success_dt = true, data_ordseq = false, data_s = false;//預設存資料庫成功
                    if (data != null) //判斷儲存勾選資料是否有無
                    {
                        /*****判別是否有資料 有的話就要做存入資料庫的動作*****/
                        #region 宣告UpcrcpTime
                        DataTable dt_ck = new DataTable();
                        DataTable dt_up = new DataTable();
                        DataTable dt_c = new DataTable();
                        string[] UpcrcpTime = { "ORDSEQ", "FEENO","SHEETNO","TOTAL", "RATE","INSDT","MEMO",
                                                "SEGUENCE","DOSE","DOSEUNIT","ROUTE" ,"FEQ","DRUGNAME",
                                                "ORDERSTARTDATE","ORDERENDDATE","STATUS","GENERICDRUGS",
                                                "CHECK1","CHECK2"
                                                };
                        string[] datatype_upcrcp = { "String", "String", "String", "String","String","String",
                                                     "String", "String", "String", "String", "String","String",
                                                     "String", "String", "String", "String", "String" ,
                                                     "String", "String" };

                        set_dt_column(dt_ck, UpcrcpTime);
                        DataRow dr = dt_ck.NewRow();
                        DataRow dr_up = dt_up.NewRow();
                        for (int i = 0; i < dt_ck.Columns.Count; i++)
                        {
                            dr[i] = datatype_upcrcp[i];
                        }
                        dt_ck.Rows.Add(dr);
                        //塞入datatype 
                        for (int i = 0; i < data.Count; i++)
                        {
                            if (data[i].ck == "true")
                            {
                                if (data[i].CHECK1 == null)
                                {
                                    data_ordseq = true;
                                    dr = dt_ck.NewRow();

                                    dr["FEENO"] = feeno;
                                    dr["ORDSEQ"] = data[i].ORDSEQ;
                                    dr["SHEETNO"] = data[i].SHEETNO;
                                    dr["SEGUENCE"] = data[i].SEGUENCE;
                                    dr["INSDT"] = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
                                    dr["DOSE"] = data[i].DOSE.ToString();
                                    dr["DOSEUNIT"] = data[i].DOSEUNIT;
                                    dr["ORDERSTARTDATE"] = data[i].ORDERSTARTDATE;
                                    dr["ORDERENDDATE"] = data[i].ORDERENDDATE;
                                    dr["ROUTE"] = data[i].ROUTE;
                                    dr["FEQ"] = data[i].FEQ;
                                    dr["MEMO"] = data[i].MEMO;
                                    dr["DRUGNAME"] = data[i].DRUGNAME;
                                    dr["GENERICDRUGS"] = data[i].GENERICDRUGS;
                                    dr["TOTAL"] = data[i].TOTAL;
                                    dr["RATE"] = data[i].RATE;

                                    if (checker != "")
                                    {
                                        dr["STATUS"] = "已覆核";
                                        dr["CHECK2"] = checker;
                                        if (data[i].CHECK1 == null)
                                        {
                                            dr["CHECK1"] = userinfo.EmployeesName;
                                            dt_up.Columns.Add("CHECK1");
                                        }
                                        dt_up.Columns.Add("CHECK2");
                                    }

                                    dt_ck.Rows.Add(dr);

                                    dt_up.Columns.Add("STATUS");
                                    dt_up.Columns.Add("where");
                                    dr_up = dt_up.NewRow();

                                    if (checker != "")
                                    {
                                        dr_up["STATUS"] = "已覆核";
                                        dr_up["CHECK2"] = checker;
                                        if (data[i].CHECK1 == null)
                                        {
                                            dr_up["CHECK1"] = userinfo.EmployeesName;
                                        }
                                    }

                                    string where = " FEENO = '" + feeno + "' AND SHEETNO = '" + data[i].SHEETNO + "'";
                                    dr_up["where"] = where;
                                    dt_up.Rows.Add(dr_up);

                                }
                                else
                                { //check有值 先完成 待覆核
                                    data_ordseq = true;
                                    data_s = true;
                                    string where = " FEENO = '" + feeno + "' AND SHEETNO = '" + data[i].SHEETNO + "'";

                                    //dt_c.Columns.Add("CHECK2");
                                    //dt_c.Columns.Add("STATUS");
                                    //dt_c.Columns.Add("where");
                                    //dr = dt_c.NewRow();
                                    //dr["STATUS"] = "已覆核";
                                    //dr["CHECK2"] = checker;
                                    //dr["where"] = where;
                                    //dt_ck.Rows.Add(dr);

                                    dt_up.Columns.Add("CHECK2");
                                    dt_up.Columns.Add("STATUS");
                                    dt_up.Columns.Add("where");
                                    dr_up = dt_up.NewRow();

                                    dr_up["STATUS"] = "已覆核";
                                    dr_up["CHECK2"] = checker;
                                    dr_up["where"] = where;
                                    dt_up.Rows.Add(dr_up);
                                }


                            }
                            ViewBag.table_check = dt_ck;

                        }

                        if (data_ordseq)
                        {
                            //確認是否有存資料 及有無成功
                            int erow = 0;
                            if (data_s == false) { erow = chemo.insert("Chemotherapy", dt_ck); }
                            else { erow = chemo.upd("Chemotherapy", dt_up); }
                            chemo.upd("ChemotherapyDrugOrder", dt_up);
                            if (erow < 1)
                                success_dt = false;
                            //儲存成功
                            if (success_dt)
                            {
                                return Redirect("../ChemotherapyMedicine/Execute?success=yes");
                            }
                            else
                            {                       //儲存失敗
                                return Redirect("../ChemotherapyMedicine/Execute?success=no");
                            }
                        }
                        //  return Redirect("../CommonMedicine/Execute?success=ck");
                    }
                    //沒有新增
                    //   return Redirect("../CommonMedicine/Execute");
                    //return View();
                    #endregion//tb

                }
                if (but == "over")
                {
                    //備註結束給藥
                    bool success_dt = true, data_ordseq = false;//預設存資料庫成功
                    if (data != null) //判斷儲存勾選資料是否有無
                    {
                        /*****判別是否有資料 有的話就要做存入資料庫的動作*****/
                        #region 宣告UpcrcpTime
                        DataTable dt_ck = new DataTable();
                        DataTable dt_up = new DataTable();
                        DataTable dt_c = new DataTable();
                        DataRow dr_up = dt_up.NewRow();

                        //塞入datatype 
                        for (int i = 0; i < data.Count; i++)
                        {
                            if (data[i].ck == "true")
                            {
                                { //check有值 先完成 待覆核
                                    data_ordseq = true;
                                    //  data_s = true;
                                    string where = " FEENO = '" + feeno + "' AND SHEETNO = '" + data[i].SHEETNO + "'";

                                    dt_up.Columns.Add("STATUS");
                                    dt_up.Columns.Add("where");
                                    dr_up = dt_up.NewRow();

                                    dr_up["STATUS"] = "結束";
                                    dr_up["where"] = where;
                                    dt_up.Rows.Add(dr_up);
                                }
                            }
                            ViewBag.table_check = dt_ck;
                        }
                        if (data_ordseq)
                        {
                            //確認是否有存資料 及有無成功
                            int erow = 0;
                            erow = chemo.upd("Chemotherapy", dt_up);
                            erow += chemo.upd("ChemotherapyDrugOrder", dt_up);
                            if (erow < 2)
                                success_dt = false;
                            //儲存成功
                            if (success_dt)
                            {
                                return Redirect("../ChemotherapyMedicine/Execute?success=yes");
                            }
                            else
                            {                       //儲存失敗
                                return Redirect("../ChemotherapyMedicine/Execute?success=no");
                            }
                        }
                        //  return Redirect("../CommonMedicine/Execute?success=ck");
                    }
                    //沒有新增
                    //   return Redirect("../CommonMedicine/Execute");
                    //return View();
                    #endregion//tb

                }
                #endregion
                #region LOAD
                DataTable dt_all = new DataTable();
                dt_all = chemo.sql_udorderinfo("Execute", feeno, "", "", "", "", "", "", "", "");
                DataTable dt = new DataTable();
                dt = chemo.sql_udorderinfo("Execute", feeno, "", "", "", "", "", "結束", "", "");
                dt_all.Columns.Add("day_time");
                dt.Columns.Add("ck_disabled");
                dt.Columns.Add("day_time");

                foreach (DataRow dr_all in dt_all.Rows)
                {
                    dr_all["day_time"] = (DateTime.Parse(dr_all["ORDERENDDATE"].ToString()) - DateTime.Parse(dr_all["ORDERSTARTDATE"].ToString())).TotalDays;
                }
                foreach (DataRow dr in dt.Rows)
                {
                    dr["day_time"] = (DateTime.Parse(dr["ORDERENDDATE"].ToString()) - DateTime.Parse(dr["ORDERSTARTDATE"].ToString())).TotalDays;
                }

                ViewBag.dt_all = dt_all;
                ViewBag.dt = dt;
                #endregion
                ViewBag.name1 = userinfo.EmployeesName;
                ViewBag.name2 = checker;
                //  ViewBag.name = form["name"];
                //登入帳號 覆核

                return View();
            }
            else
            {
                Response.Write("<script>alert('請重新選擇病患');</script>");
                return new EmptyResult();
            }
        }

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
            return View();
        }

        #region 其他
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
        
        public ActionResult UnExcuteReason()
        {
            return View();
        }
       
        public ActionResult Print()
        {
            List<SelectListItem> listitem = new List<SelectListItem>();
            listitem.Add(new SelectListItem { Text = "使用中", Value = "" });
            listitem.Add(new SelectListItem { Text = "待覆核", Value = "" });
            listitem.Add(new SelectListItem { Text = "已覆核", Value = "" });
            listitem.Add(new SelectListItem { Text = "結束", Value = "" });
            ViewData["List"] = listitem;

            return View();
        }

        #endregion

    }
}
