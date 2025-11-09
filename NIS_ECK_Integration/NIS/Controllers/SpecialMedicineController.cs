using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NIS.Controllers;
using NIS.Models;
using NIS.Data;

namespace NIS.Controllers
{
    public class SpecialMedicineController : BaseController
    {    
        // GET: /SepicalMedicine/
         private CommData cd;    //常用資料Module
         private SpecialMedicine sd;
       // private PatientInfo ptInfo;

        public SpecialMedicineController()
        {
            this.cd = new CommData();
            this.sd = new SpecialMedicine();
            // this.ptInfo = (PatientInfo)HttpContext.Session["PatInfo"];
            // PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
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
            public string DOSEUNIT { get; set;}
            public string ROUTE { get; set; }
            public string STATUS { get; set; }
            public string REASON { get; set; }
            public string REASONTYPE { get; set; }
            public string INJECTION { get; set; }
            public string PAGE { get; set; }
            public string SDID { get; set; }
        }


        [HttpGet]
        public ActionResult List(string success)
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
            listitem2.Add(new SelectListItem { Text = "Clexane(60 mg)", Value = "1" });
            listitem2.Add(new SelectListItem { Text = "Recormon(2000 iu)", Value = "2" });
            listitem2.Add(new SelectListItem { Text = "Recormon(5000 iu)", Value = "3" });
            listitem2.Add(new SelectListItem { Text = "Nesp(20 mcg)", Value = "4" });
            listitem2.Add(new SelectListItem { Text = "Mircera(50mcg)", Value = "5" });
            listitem2.Add(new SelectListItem { Text = "其他", Value = "o" });
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
                get_p= sd.get_position(feeno).ToString().Trim();
                string status = "", REVIEW = "";
                string page = "SP";
                status = "del";
             
                DataTable dt = new DataTable();
                DataTable dt_s = new DataTable();
                dt = sd.sql_SDtable(feeno, page,"", status, "", "");
                dt_s = sd.sql_DtSet(feeno);
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
                ViewBag.dt_s=dt_s;
                ViewBag.text = "";
                     
                return View();

            }
            return Redirect("../VitalSign/VitalSign_Single");

        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult List(FormCollection form, string SDID, string ck, List<newitem> data)
        {

            if (Session["PatInfo"] != null)
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
                listitem2.Add(new SelectListItem { Text = "Clexane(60 mg)", Value = "1" });
                listitem2.Add(new SelectListItem { Text = "Recormon(2000 iu)", Value = "2" });
                listitem2.Add(new SelectListItem { Text = "Recormon(5000 iu)", Value = "3" });
                listitem2.Add(new SelectListItem { Text = "Nesp(20 mcg)", Value = "4" });
                listitem2.Add(new SelectListItem { Text = "Mircera(50mcg)", Value = "5" });
                listitem2.Add(new SelectListItem { Text = "其他", Value = "o" });
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

                string status = "", REVIEW="";
                string page = "SP";
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

                dt = sd.sql_SDtable(feeno, page, "", status, start, end);
                dt_s = sd.sql_DtSet(feeno);
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
                ViewBag.dt_s = dt_s;
                ViewBag.text = "";
                #endregion
                #region SAVE
                bool success_dt = true, data_up = false;
                if (data[0].SDID != null && data[0].SDID != "")//如果有資料的話
                {
                    DataTable dt_up = new DataTable();
             
                    if (data[0].STATUS == "del"){
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
                    else if (data[0].STATUS == "upd") { 
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
                        //dt_up.Columns.Add("SDID");
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
                            dr_up["PAGE"] = "SP";
                            
                            dr_up["where"] = where;
                            dt_up.Rows.Add(dr_up);
                       
                    }
                   

                    if (data_up)
                    {
                        //確認是否有存資料 及有無成功
                        int erow = sd.upd("SPECIALDRUG", dt_up);
                        if (erow < 1)
                            success_dt = false;
                        //儲存成功
                        if (success_dt)
                        {
                            return Redirect("../SpecialMedicine/List?success=yes");
                        }
                        else
                        {                       //儲存失敗
                            return Redirect("../SpecialMedicine/List?success=no");
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
                                                         "String", "String", "String", "String", "String",
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
                        dr["DRUGTYPE"] = data[0].DRUGNAME;
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
                            int erow = sd.insert("SPECIALDRUG", dt_bs);
                            if (erow < 1)
                                success_dt = false;
                            //儲存成功
                            if (success_dt)
                            {
                                return Redirect("../SpecialMedicine/List?success=yes");
                            }
                            else
                            {                       //儲存失敗
                                return Redirect("../SpecialMedicine/List?success=no");
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
        public ActionResult Insert(string SDID,string feeno)
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
            listitem2.Add(new SelectListItem { Text = "Clexane(60 mg)", Value = "" });
            listitem2.Add(new SelectListItem { Text = "Recormon(2000 iu)", Value = "" });
            listitem2.Add(new SelectListItem { Text = "Recormon(5000 iu)" ,Value = "" });
            listitem2.Add(new SelectListItem { Text = "Nesp(20 mcg)", Value = "" });
            listitem2.Add(new SelectListItem { Text = "Mircera(50mcg)", Value = "" });
            listitem2.Add(new SelectListItem { Text = "其他", Value = "" });
            ViewData["List2"] = listitem2;
            List<SelectListItem> listitem3 = new List<SelectListItem>();
            listitem3.Add(new SelectListItem { Text = "拒絕", Value = "" });
            listitem3.Add(new SelectListItem { Text = "禁食", Value = "" });
            listitem3.Add(new SelectListItem { Text = "血糖偏低", Value = "" });
            listitem3.Add(new SelectListItem { Text = "其他", Value = "" });
            ViewData["List3"] = listitem3;
            #endregion

            string status = "";
            string page = "SP";
           // status = "del";
            DataTable dt = new DataTable();
            dt = sd.sql_SDtable(feeno, page, SDID, status, "", "");
            ViewBag.table = dt;
            //if (SDID != null && dt.Rows.Count > 0)
            //{
            //    DataRow dr = dt.Rows[0];
            // //   ViewBag.dr = dr;
            //    ViewBag.dt = dt;
            //}
            //else
            //{
            //    ViewBag.dr = null;
            //  //  DataRow dr = DataRow(dt);
            //}

            return View();
        }

        [HttpGet]
        public ActionResult Set_reject()
        {
            PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
            string feeno = ptInfo.FeeNo;
            DataTable dt = new DataTable();               
            dt = sd.sql_DtSet(feeno);
            ViewBag.feeno = feeno;
            ViewBag.table = dt;
            string col = "";
            if (dt != null && dt.Rows.Count > 0)
            {
                if (dt.Rows[0]["BAN"].ToString().Trim() != "")
                { col = "red"; }
                else
                { col = "purple"; }
            //item=dt.Rows[0]["BAN"].ToString().Trim() + dt.Rows[0]["REFUSE"].ToString().Trim();
            ViewBag.ban = dt.Rows[0]["BAN"].ToString().Trim();
            ViewBag.refuse = dt.Rows[0]["REFUSE"].ToString().Trim();
            }
          
            ViewBag.col = col;        
          //  ViewBag.item = item;

            return View();
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Set_reject(FormCollection form, string[] ckin, string feeno, string reason, string rbtype, string refuse,string ban)
        {
            if (ckin != null)
            {//有無勾選

                //if (data[0].STATUS == "new")
                //{
                DataTable dt_s = new DataTable();
                DataTable dt = new DataTable();
                dt = sd.sql_DtSet(feeno);
                ViewBag.feeno = feeno;
                ViewBag.table = dt;

                string list ="AEBFCGHD";
                string[] id = ban.Split(',');
                for (int i = 0; i < id.Length; i++)
                {
                    if (id[i] != null && id[i] != "")
                    { list = list.Replace(id[i], ""); }
                }

                #region 存檔20160817
                List<DBItem> insertDataList = new List<DBItem>();
            
                insertDataList.Add(new DBItem("FEENO", ptinfo.FeeNo, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("INDATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("REASON", reason, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("REVIEW", list.Replace(",", ""), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("INSDT", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("INSOP", userinfo.EmployeesNo, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("INSOPNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("BAN", ban, DBItem.DBDataType.String));

                int erow = sd.DBExecInsert("SPECIALDRUG_SET", insertDataList);
                if (erow > 0)
                {
                   
                    Response.Write("<script>alert('新增成功');window.close();</script>");
                }
                else
                    Response.Write("<script>alert('新增失敗');window.close();</script>");
                #endregion


                #region 宣告資料表


               //// string[] UpcrcpTime = { "FEENO", "INDATE", "BAN", "REVIEW", 
               ////                                     "INSDT","INSOP" ,"INSOPNAME",
               ////                                     "MODDT","MODOP","MODNAME","REASON","REFUSE"};
               //// string[] datatype_upcrcp = {  "String", "String", "String","String",
               ////                                          "String", "String", "String","String",
               ////                                          "String", "String", "String", "String",
               ////                                          "String", "String", "String", "String"};
               //// set_dt_column(dt_s, UpcrcpTime);
               //// DataRow dr = dt_s.NewRow();
               //// for (int i = 0; i < dt_s.Columns.Count; i++)
               //// {
               ////     dr[i] = datatype_upcrcp[i];

               //// }
               //// dt_s.Rows.Add(dr);
               //// //塞入datatype
               //// dr = dt_s.NewRow();

               //// dr["FEENO"] = feeno;
               //// dr["INDATE"] = (DateTime.Now.ToString("yyyy/MM/dd HH:mm"));
               //// dr["REVIEW"] = list.Replace(",", "");//list.TrimEnd(',');
               //// dr["INSDT"] = (DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
               //// dr["INSOP"] = userinfo.EmployeesNo;
               //// dr["INSOPNAME"] = userinfo.EmployeesName;
               //// //if (position != null) 
               //// //{
               //// //    if (rbtype == "ban")//存禁忌部位
               //// //    {
               //// //        dr["BAN"] = position.TrimEnd(',');
               //// //        dr["REASON"] = reason;
               //// //    }
               //// //    else
               //// //    {
               //// //        dr["REFUSE"] = position.TrimEnd(',');
               //// //    }
               //// //}
               //// //  if (ban != "")
               //// { dr["BAN"] = ban; }
               //// //   if (refuse != "")
               ////// { dr["REFUSE"] = refuse; }


               //// dt_s.Rows.Add(dr);




               //// if (data_s)
               //// {
               ////     //確認是否有存資料 及有無成功
               ////     int erow = sd.insert("SPECIALDRUG_SET", dt_s);
               ////     if (erow < 1)
               ////         success_dt = false;
               ////     //儲存成功
               ////     if (success_dt)
               ////     {
               ////        // Response.Write("<script>alert('儲存成功!')</script>");
               ////         Response.Write("<script>alert('儲存成功');window.close();</script>");
               ////         return View();
               ////         //return Redirect("../SpecialMedicine/List?success=yes");
               ////     }
               ////     else
               ////     {
               ////      //   Response.Write("<script>alert('儲存失敗!')</script>");
               ////         Response.Write("<script>alert('儲存失敗');window.close();</script>");
               ////         return View();
               ////         //儲存失敗
               ////         //return Redirect("../SpecialMedicine/List?success=no");
               ////     }
               //// }

               //// // }
                #endregion

            }
            else//將原有的拒打部位取消
            {
                DataTable dt_s = new DataTable();
                DataTable dt = new DataTable();

                #region 存檔20160817


                List<DBItem> insertDataList = new List<DBItem>();

                insertDataList.Add(new DBItem("FEENO", ptinfo.FeeNo, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("INDATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("REASON", reason, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("REVIEW", "AEBFCGHD", DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("INSDT", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("INSOP", userinfo.EmployeesNo, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("INSOPNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("BAN", "", DBItem.DBDataType.String));

                int erow = sd.DBExecInsert("SPECIALDRUG_SET", insertDataList);
                if (erow > 0)
                {

                    Response.Write("<script>alert('新增成功');window.close();</script>");
                }
                else
                    Response.Write("<script>alert('新增失敗');window.close();</script>");
                #endregion

                #region 宣告資料表

              ////  string[] UpcrcpTime = { "FEENO", "INDATE", "BAN", "REVIEW", 
              ////                                      "INSDT","INSOP" ,"INSOPNAME",
              ////                                      "MODDT","MODOP","MODNAME","REASON","REFUSE"};
              ////  string[] datatype_upcrcp = {  "String", "String", "String","String",
              ////                                           "String", "String", "String","String",
              ////                                           "String", "String", "String", "String",
              ////                                           "String", "String", "String", "String"};
              ////  set_dt_column(dt_s, UpcrcpTime);
              ////  DataRow dr = dt_s.NewRow();
              ////  for (int i = 0; i < dt_s.Columns.Count; i++)
              ////  {
              ////      dr[i] = datatype_upcrcp[i];

              ////  }
              ////  dt_s.Rows.Add(dr);
              ////  //塞入datatype
              ////  dr = dt_s.NewRow();

              ////  dr["FEENO"] = feeno;
              ////  dr["INDATE"] = (DateTime.Now.ToString("yyyy/MM/dd HH:mm"));
              ////  dr["REVIEW"] = "AEBFCGHD";
              ////  dr["INSDT"] = (DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
              ////  dr["INSOP"] = userinfo.EmployeesNo;
              ////  dr["INSOPNAME"] = userinfo.EmployeesName;
              
              ////  { dr["BAN"] = ""; }
            
              //////  { dr["REFUSE"] = ""; }


              ////  dt_s.Rows.Add(dr);




              ////  if (data_s)
              ////  {
              ////      //確認是否有存資料 及有無成功
              ////      int erow = sd.insert("SPECIALDRUG_SET", dt_s);
              ////      if (erow < 1)
              ////          success_dt = false;
              ////      //儲存成功
              ////      if (success_dt)
              ////      {
              ////          Response.Write("<script>alert('儲存成功!')</script>");
              ////          return View();
              ////          //return Redirect("../SpecialMedicine/List?success=yes");
              ////      }
              ////      else
              ////      {
              ////          Response.Write("<script>alert('儲存失敗!')</script>");
              ////          return View();
              ////          //儲存失敗
              ////          //return Redirect("../SpecialMedicine/List?success=no");
              ////      }
              ////  }

              ////  // }
                #endregion
            }
            return View();
        }
        
    }
}
