using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NIS.Data;
using NIS.Models;
using NIS.UtilTool;
using NIS.WebService;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Data.OleDb;
using NIS.Models.DBModel;

namespace NIS.Controllers
{
    public class ConstraintsAssessmentController : BaseController
    {
        //
        // GET: /ConstraintsAssessment/
        private CommData cd;    //常用資料Module
        private ConstraintsAssessment ca;
        private DBConnector link;
        public ConstraintsAssessmentController()
        {
            this.link = new DBConnector();
            this.cd = new CommData();
            this.ca = new ConstraintsAssessment();
        }

        public class binddata
        {
            public string ASSESS { get; set; }
            public string ASSESSDT { get; set; }
            public string REASON { get; set; }
            public string CONSCIOUS { get; set; }
            public string REACTION { get; set; }
            public string POSITION { get; set; }
            public string TOOL { get; set; }
            public string CYCLE { get; set; }
            public string PAUSE { get; set; }
            public string ENDING { get; set; }
            public string HARM { get; set; }
            public string HARM1 { get; set; }
            public string HARM2 { get; set; }
            public string EXPLAIN { get; set; }
            public string RECORDS { get; set; }
            public string OTHER { get; set; }
        }

        public class binding_site_Data
        {
            public string P_MODEL { get; set; }
            public string P_GROUP { get; set; }
            public string P_NAME { get; set; }
            public string P_VALUE { get; set; }
            public string P_SORT { get; set; }
        }

        [HttpGet]
        public ActionResult ListN()
        {
            PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
            string feeno = ptInfo.FeeNo;
            ViewBag.dt_a = ca.get_table("BINDTABLE", feeno, "");
            ViewBag.dt = ca.get_table("BINDTABLESAVE", feeno, "");
            ViewBag.login = userinfo.EmployeesNo;
            return View();
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult ListN(FormCollection form, string but, string id, string bindid, string saveid)
        {
            PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
            string feeno = ptInfo.FeeNo;
            bool data_ok = false;
            bool data_save = false;
            ViewBag.login = userinfo.EmployeesNo;
            DataTable dt_d = new DataTable();
            DataRow dr = dt_d.NewRow();
            switch (but)
            {
                case "delend":
                    #region 更新
                    //更新 刪除結束日期                                           
                    dt_d.Columns.Add("ENDDT");
                    dt_d.Columns.Add("MODDT");
                    dt_d.Columns.Add("MODID");
                    dt_d.Columns.Add("MODNAME");
                    dt_d.Columns.Add("STATUS");
                    dt_d.Columns.Add("where");
                    if (id != null && id != "")//更新之流水號不為空
                    {
                        data_ok = true;
                        dr = dt_d.NewRow();
                        string where = " ID = '" + id.ToString() + "' ";
                        dr["ENDDT"] = string.Empty;
                        dr["MODDT"] = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString();
                        dr["MODID"] = userinfo.EmployeesNo;
                        dr["MODNAME"] = userinfo.EmployeesName;
                        dr["STATUS"] = "刪除結束註記";
                        dr["where"] = where;
                        dt_d.Rows.Add(dr);
                    }
                    #endregion//更新
                    break;
                case "del":
                    #region 更新
                    //更新 刪除結束日期                                           
                    dt_d.Columns.Add("MODDT");
                    dt_d.Columns.Add("MODID");
                    dt_d.Columns.Add("MODNAME");
                    dt_d.Columns.Add("STATUS");
                    dt_d.Columns.Add("where");
                    if (id != null && id != "")//更新之流水號不為空
                    {
                        data_ok = true;
                        dr = dt_d.NewRow();
                        string where = " ID = '" + id.ToString() + "' ";
                        dr["MODDT"] = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString();
                        dr["MODID"] = userinfo.EmployeesNo;
                        dr["MODNAME"] = userinfo.EmployeesName;
                        dr["STATUS"] = "del";
                        dr["where"] = where;
                        dt_d.Rows.Add(dr);
                    }
                    #endregion//更新
                    break;
                case "delFeq"://刪除評估記錄
                    #region 刪除評估記錄
                    //更新 刪除結束日期                                           
                    dt_d.Columns.Add("MODDT");
                    dt_d.Columns.Add("MODID");
                    dt_d.Columns.Add("MODNAME");
                    dt_d.Columns.Add("STATUS");
                    dt_d.Columns.Add("where");
                    if (saveid != null && saveid != "" && bindid != null && bindid != "")//更新之流水號不為空
                    {
                        data_save = true;
                        dr = dt_d.NewRow();
                        string where = " ID = '" + saveid.ToString() + "' and bindid= '" + bindid.ToString() + "'";
                        dr["MODDT"] = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString();
                        dr["MODID"] = userinfo.EmployeesNo;
                        dr["MODNAME"] = userinfo.EmployeesName;
                        dr["STATUS"] = "del";
                        dr["where"] = where;
                        dt_d.Rows.Add(dr);
                    }
                    #endregion//更新
                    break;
            }
            if (data_ok)
            {
                //確認是否有存資料 及有無成功
                int erow = ca.upd("BINDTABLE", dt_d);
            }
            if (data_save)
            {
                //確認是否有存資料 及有無成功
                int erow = ca.upd("BINDTABLESAVE", dt_d);
            }
            ViewBag.dt_a = ca.get_table("BINDTABLE", feeno, "");
            ViewBag.dt = ca.get_table("BINDTABLESAVE", feeno, "");
            return View();
        }
        [HttpGet]
        public ActionResult InsertDate(string ver, string start, string id)
        {
            #region LOAD
            //宣告病患_取得住院序號
            PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
            string feeno = ptInfo.FeeNo;
            if (ver == "start")
            {
                ViewBag.title = "新增約束";
                ViewBag.content_title = "約束開始日期時間";
                ViewBag.table_title = "開始日期時間";
                if (id == null || id == "")
                {
                    id = DateTime.Now.ToString("yyyyMMddHHmmss").ToString() + feeno;
                }
            }
            else
            {
                ViewBag.title = "結束約束";
                ViewBag.content_title = "約束結束日期時間";
                ViewBag.table_title = "結束日期時間";

            }
            ViewBag.start = start;
            ViewBag.ver = ver;
            ViewBag.id = id;
            //  ViewBag.dt_a = ca.get_table("BINDTABLE", feeno, "");
            #endregion

            return View();
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult InsertDate(FormCollection form, string ver, string id)
        {
            //宣告病患_取得住院序號
            PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
            string feeno = ptInfo.FeeNo;
            #region LOAD
            if (ver == "start")
            {
                ViewBag.title = "新增約束";
                ViewBag.content_title = "約束開始日期時間";
                ViewBag.table_title = "開始日期時間";
                ViewBag.ver = ver;
                #region SAVE

                string start = Convert.ToDateTime(form["start_day"]).ToString("yyyy/MM/dd ");
                start += Convert.ToDateTime(form["start_time"]).ToString("HH:mm:ss");
                //  if (feq != null)
                {
                    DataTable dt_n = new DataTable();
                    dt_n = ca.get_newtable("BINDTABLE");
                    DataRow dr = dt_n.NewRow();
                    bool data_ok = false;

                    dr = dt_n.NewRow();
                    data_ok = true;

                    dr["FEENO"] = feeno;
                    dr["ID"] = id;
                    dr["BOUT"] = int.Parse(ca.get_id(feeno)) + 1;//抓最大次數+1
                    dr["STARTDT"] = start;
                    dr["ENDDT"] = "";
                    dr["ASSESS"] = "0";
                    //dr["FEQ"]=feq;
                    dr["STATUS"] = "開始約束";
                    dr["INSDT"] = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString();
                    dr["INSID"] = userinfo.EmployeesNo;
                    dr["INSNAME"] = userinfo.EmployeesName;

                    dt_n.Rows.Add(dr);
                    if (data_ok)
                    {
                        int erow = ca.insert("BINDTABLE", dt_n);
                        //   data_ordseq = false;
                        #region SAVE 跑馬燈
                        List<DBItem> insertDataList = new List<DBItem>();
                        DateTime STARTTIME = DateTime.Parse(start).AddDays(1);
                        insertDataList.Add(new DBItem("NT_ID", id, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("STARTTIME", STARTTIME.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("FEE_NO", feeno, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("MEMO", "此病人已約束24小時", DBItem.DBDataType.String));
                        //   insertDataList.Add(new DBItem("TIMEOUT", "", DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("ACTIONLINK", "", DBItem.DBDataType.String));
                        int erow2 = link.DBExecInsert("DATA_NOTICE", insertDataList);


                        #endregion
                    }

                }
                #endregion
            }
            else
            {
                ViewBag.title = "結束約束";
                ViewBag.content_title = "約束結束日期時間";
                ViewBag.table_title = "結束日期時間";
                ViewBag.ver = ver;
                #region 更新
                //更新 結束日期
                bool data_ok = false;
                // string id = "";
                string end = Convert.ToDateTime(form["start_day"]).ToString("yyyy/MM/dd ");
                end += Convert.ToDateTime(form["start_time"]).ToString("HH:mm:ss");
                DataTable dt_d = new DataTable();
                DataRow dr = dt_d.NewRow();
                dt_d.Columns.Add("ENDDT");
                dt_d.Columns.Add("MODDT");
                dt_d.Columns.Add("MODID");
                dt_d.Columns.Add("MODNAME");
                dt_d.Columns.Add("STATUS");

                dt_d.Columns.Add("where");
                if (id != null && id != "")//更新之流水號不為空
                {
                    data_ok = true;
                    dr = dt_d.NewRow();
                    string where = " ID = '" + id.ToString() + "' ";
                    dr["ENDDT"] = end;
                    dr["MODDT"] = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString();
                    dr["MODID"] = userinfo.EmployeesNo;
                    dr["MODNAME"] = userinfo.EmployeesName;
                    dr["STATUS"] = "結束";
                    dr["where"] = where;
                    dt_d.Rows.Add(dr);
                }

                if (data_ok)
                {
                    //確認是否有存資料 及有無成功
                    int erow = ca.upd("BINDTABLE", dt_d);
                    //護理紀錄 記錄約束結束
                    string mag = "病人於" + end + "結束約束";
                    Insert_CareRecord(end, "end" + id, "", "", "", "", mag, "", "ASS_N");
                    #region SAVE 跑馬燈結束註記
                    List<DBItem> insertDataList = new List<DBItem>();
                    insertDataList.Add(new DBItem("TIMEOUT", end, DBItem.DBDataType.DataTime));
                    int erow2 = link.DBExecUpdate("DATA_NOTICE", insertDataList, "NT_ID = '" + id.ToString() + "' ");
                    #endregion
                }
                #endregion//更新
            }
            //  ViewBag.dt_a = ca.get_table("BINDTABLE", feeno, "");
            #endregion //Load
            ViewBag.id = id;
            return View();
        }

        [HttpGet]//---ECK 列表，按下主表之後下方子表的切頁
        public ActionResult ConstraintsList(string id)
        {
            PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
            string feeno = ptInfo.FeeNo;
            ViewBag.Category = userinfo.Category.ToString();
            //DataTable dt_a = ca.get_table("BINDTABLE", feeno, "");
            //ViewBag.dt_a = dt_a;
            DataTable dt_a1 = ca.get_table("BINDTABLE_ADD_REASON", feeno, "");
            DataView view = dt_a1.DefaultView;
            view.Sort = "BOUTDESC DESC";
            DataTable dt_a = view.ToTable();
            ViewBag.dt_a = dt_a;
            if (id == "" && dt_a.Rows.Count > 0)
            {
                //id = dt_a.Rows[dt_a.Rows.Count - 1]["ID"].ToString().Trim();
                id = dt_a.Rows[0]["ID"].ToString().Trim();
            }
            //DataTable dt = ca.get_table("BINDTABLESAVE", feeno, id);
            //ViewBag.dt = dt;
            DataTable dt_x = ca.get_table("BINDTABLESAVE", feeno, id);
            DataView view_x = dt_x.DefaultView;
            if (dt_x.Rows.Count > 0)
            {
                view_x.Sort = "ASSESSDT ASC";
            }
            DataTable dt = view_x.ToTable();
            ViewBag.dt = dt;
            if (dt_a.Rows.Count > 0)
            {   //此為舊版本
                //ViewBag.BINDTABLE_id = dt_a.Rows[dt_a.Rows.Count - 1]["ID"].ToString().Trim();
                //ViewBag.lastStartDt = dt_a.Rows[dt_a.Rows.Count - 1]["STARTDT"].ToString().Trim();
                ////ViewBag.feq = dt_a.Rows[dt_a.Rows.Count - 1]["feq"].ToString().Trim();
                //ViewBag.feqdt = dt_a.Rows[dt_a.Rows.Count - 1]["feqdt"].ToString().Trim();
                //ViewBag.sfeqdt = dt_a.Rows[dt_a.Rows.Count - 1]["sfeqdt"].ToString().Trim();
                //ViewBag.lastmodid = dt_a.Rows[dt_a.Rows.Count - 1]["modid"].ToString().Trim();
                //ViewBag.lastmodname = dt_a.Rows[dt_a.Rows.Count - 1]["modname"].ToString().Trim();

                //jarvis修改-----↓
                //以下版本，將取主表的第一行的相關資訊，給view在新增評估時使用
                ViewBag.BINDTABLE_id = dt_a.Rows[0]["ID"].ToString().Trim();
                ViewBag.lastStartDt = dt_a.Rows[0]["STARTDT"].ToString().Trim();
                //ViewBag.feq = dt_a.Rows[dt_a.Rows.Count - 1]["feq"].ToString().Trim();
                ViewBag.feqdt = dt_a.Rows[0]["feqdt"].ToString().Trim();
                ViewBag.sfeqdt = dt_a.Rows[0]["sfeqdt"].ToString().Trim();
                ViewBag.lastmodid = dt_a.Rows[0]["modid"].ToString().Trim();
                ViewBag.lastmodname = dt_a.Rows[0]["modname"].ToString().Trim();
                //jarvis修改-----↑
                for (int j = 0; j < dt_a.Rows.Count; j++)
                {
                    string num = ca.CheckAssess(feeno, dt_a.Rows[j]["ID"].ToString().Trim()).ToString();
                    if (num == "0")
                    { dt_a.Rows[j]["ASSESS"] = "due"; }
                    else
                    { dt_a.Rows[j]["ASSESS"] = ""; }
                }
            }
            if (dt.Rows.Count > 0)
            {
                ViewBag.lastStartDt = dt.Rows[dt.Rows.Count - 1]["ASSESSDT"].ToString().Trim();
                ViewBag.feq = dt.Rows[dt.Rows.Count - 1]["feq"].ToString().Trim();
            }
            ViewBag.login = userinfo.EmployeesNo;
            ViewBag.feeno = feeno;
            return View();
        }

        [HttpGet]
        public ActionResult AssessN(string id, int feq, string start, string bindid)
        {
            PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
            string feeno = ptInfo.FeeNo;
            DataTable dt = new DataTable();

            #region 展開評估時間
            if (feq != 0 && feq != 1)
            {
                DataTable dtr = ca.get_newrow(id);
                DataTable dtr1 = ca.get_newrow1(id);
                ViewBag.dtr = dtr1;
                if (dtr != null && dtr.Rows.Count > 0)
                {
                    if (dtr.Rows[0]["ASSESSDT"].ToString() != null && dtr.Rows[0]["ASSESSDT"].ToString() != "")
                    { start = dtr.Rows[0]["ASSESSDT"].ToString(); }
                }
                TimeSpan sum = DateTime.Now - DateTime.Parse(start);
                int nowM = DateTime.Parse(start).Minute;
                int m = sum.Minutes;
                dt = ca.get_newtable("BINDTABLADD");
                DateTime m0 = DateTime.Parse(DateTime.Parse(start).ToString("yyyy/MM/dd HH:00:ss").ToString());
                //   int fornum = 0;
                switch (feq)
                {
                    case 15:
                        //     fornum = 8;
                        if (nowM <= 15) { m0 = m0.AddMinutes(0); }
                        else if (nowM <= 30) { m0 = m0.AddMinutes(15); }
                        else if (nowM <= 45) { m0 = m0.AddMinutes(30); }
                        else { m0 = m0.AddMinutes(45); }
                        break;
                    case 30:
                        //       fornum = 4;
                        if (nowM > 30) { m0 = m0.AddMinutes(30); }
                        break;
                    case 60:
                        //      fornum = 2;
                        break;

                }
                DataRow dr = dt.NewRow();
                List<String> TList = new List<String>();
                // List<DBItem> TList = new List<DBItem>();
                int i = 0;
                while (m0.AddMinutes((i + 1) * feq) < DateTime.Now)
                {

                    TList.Add(m0.AddMinutes((i + 1) * feq).ToString("yyyy/MM/dd HH:mm:ss").ToString());
                    i++;
                }
                if (i == 0)
                { //如果沒有展開評估
                    Response.Write("<script>alert('未至需評估時間');window.location.href='ListN';</script>");
                }

                ViewData["time"] = TList;
                ViewBag.mod = "0";
            }
            else if (feq == 1)
            {// 修改狀態
                if (bindid != null && bindid != "")
                {
                    dt = ca.get_table("BINDTABLESAVEMOD", feeno, bindid);
                }
            }
            else
            {
                // start = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString();
                // DataTable dtr = ca.get_table("BINDTABLE2", feeno,"");
                //if (dtr != null && dtr.Rows.Count > 0)
                //{
                //    if (dtr.Rows[0]["STARTDT"].ToString() != null && dtr.Rows[0]["STARTDT"].ToString() != "")
                //    { start = dtr.Rows[0]["STARTDT"].ToString(); }
                //}
                ViewBag.assessdt = start;
            }

            #endregion //展開評估時間

            ViewBag.dt = dt;
            ViewBag.feq = feq;
            ViewBag.id = id;
            return View();
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult AssessN(FormCollection form, List<binddata> data, int feq)
        {
            PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
            string feeno = ptInfo.FeeNo;
            bool data_ok = false;
            bool data_UP = false;

            if (feq == 0)
            {//頻次沒有選 表示為首次評估=>需寫入護理紀錄
                #region 新增評估
                DataTable dt_n = new DataTable();
                DataRow dr = dt_n.NewRow();
                dt_n = ca.get_newtable("BINDTABLESAVE");

                dr = dt_n.NewRow();
                data_ok = true;
                string assessdt = form["assessdt"];
                dr["FEENO"] = feeno;
                dr["ID"] = form["ID"];//DateTime.Now.ToString("yyyyMMddHHmmss").ToString();}
                dr["BINDID"] = feeno + DateTime.Now.ToString("yyyyMMddHHmmss").ToString();
                dr["BOUT"] = form["BOUT"];//int.Parse(ca.get_id("BINDTABLE", feeno)) + 1;//抓最大次數+1
                //r["STARTDT"] = form["STARTDT"];//start;
                //r["ENDDT"] = "";             
                dr["FEQ"] = feq;
                dr["STATUS"] = "約束評估";

                dr["INSDT"] = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString();
                dr["INSID"] = userinfo.EmployeesNo;
                dr["INSNAME"] = userinfo.EmployeesName;
                //dr["RECORDS"] = data[i].RECORDS;

                dr["BINDID"] = feeno + DateTime.Parse(assessdt).ToString("yyyyMMddHHmm").ToString();

                dr["ASSESSDT"] = form["assessdt"];
                dr["REASON"] = form["REASON"];
                dr["CONSCIOUS"] = form["CONSCIOUS"];
                dr["REACTION"] = form["REACTION"];
                dr["POSITION"] = form["POSITION"];
                dr["TOOL"] = form["TOOL"];
                dr["CYCLE"] = form["CYCLE"];
                dr["PAUSE"] = form["PAUSE"];
                dr["ENDING"] = form["ENDING"];
                dr["HARM"] = form["HARM"];

                dr["EXPLAIN"] = form["EXPLAIN"];
                dr["OTHER"] = form["OTHER"];
                dr["HARM1"] = form["HARM1"];
                dr["HARM2"] = form["HARM2"];
                dt_n.Rows.Add(dr);

                if (data_ok)
                {
                    int erow = ca.insert("BINDTABLESAVE", dt_n);
                    //   data_ordseq = false;
                }

                #endregion
                #region 傳入護理紀錄
                string mag = "", title = "";
                Boolean save_f = false;
                if (form["HARM"] != null && form["HARM"] != "")
                {
                    if (form["HARM"] == "無")
                    {
                        title = "無約束傷害";
                        mag = "病人因" + form["REASON"];
                        mag += "以" + form["TOOL"] + "約束" + form["POSITION"];
                        mag += "，病人意識" + form["CONSCIOUS"];
                        mag += "，" + form["REACTION"];
                        mag += "，約束部位循環" + form["CYCLE"];
                        mag += "，予短暫解除約束" + form["PAUSE"] + "分鐘，";
                        mag += "協助" + form["ENDING"];
                        mag += "，" + form["HARM"] + "約束傷害。";
                    }
                    else
                    {
                        title = "有約束傷害";
                        mag = "病人因" + form["REASON"];
                        mag += "以" + form["TOOL"] + "約束" + form["POSITION"];
                        mag += "，病人意識" + form["CONSCIOUS"];
                        mag += "，" + form["REACTION"];
                        mag += "，約束部位循環" + form["CYCLE"];
                        mag += "，予短暫解除約束" + form["PAUSE"] + "分鐘，";
                        mag += "協助" + form["ENDING"];
                        mag += "，有" + form["HARM"] + "之約束傷害。";
                    }
                }
                else
                {
                    title = "病人於" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString() + "開始約束";
                    mag = "病人因" + form["REASON"];
                    mag += "以" + form["TOOL"] + "約束" + form["POSITION"];
                    mag += "，病人意識" + form["CONSCIOUS"];
                    mag += "，" + form["REACTION"];
                }
                if (mag != null && mag != "")
                    save_f = true;
                if (save_f)
                {
                    Insert_CareRecord(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), form["ID"], "", mag, "", "", "", "", "ASS_N");
                }
                #endregion
                #region 修改評估主表

                // string id = "";
                DataTable dt_d = new DataTable();
                DataRow dr2 = dt_d.NewRow();
                // dt_d.Columns.Add("ENDDT");
                dt_d.Columns.Add("MODDT");
                dt_d.Columns.Add("MODID");
                dt_d.Columns.Add("MODNAME");
                dt_d.Columns.Add("ASSESS");
                dt_d.Columns.Add("STATUS");

                dt_d.Columns.Add("where");
                if (form["ID"] != null && form["ID"] != "")//更新之流水號不為空
                {
                    data_UP = true;
                    dr2 = dt_d.NewRow();
                    string where = " ID = '" + form["ID"].ToString() + "' ";
                    //   dr2["ENDDT"] = end;
                    dr2["MODDT"] = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString();
                    dr2["MODID"] = userinfo.EmployeesNo;
                    dr2["MODNAME"] = userinfo.EmployeesName;
                    dr2["ASSESS"] = "1";
                    dr2["STATUS"] = "已新增評估";
                    dr2["where"] = where;
                    dt_d.Rows.Add(dr2);
                }

                if (data_UP)
                {
                    //確認是否有存資料 及有無成功
                    int erow = ca.upd("BINDTABLE", dt_d);
                }
                #endregion
            }
            else if (feq != 1)
            {//新增多筆展開頻次之評估
                string timelist = form["assdt[]"];//展開所有需評估時間

                #region 新增評估
                string[] times = timelist.Split(',');
                DataTable dt_n = new DataTable();
                dt_n = ca.get_newtable("BINDTABLESAVE");
                DataRow dr = dt_n.NewRow();
                for (int i = 0; i < times.Length; i++)
                {
                    dr = dt_n.NewRow();

                    data_ok = true;
                    string assessdt = times[i].ToString();
                    dr["FEENO"] = feeno;
                    dr["ID"] = form["ID"];
                    //  dr["BINDID"] = feeno + DateTime.Now.ToString("yyyyMMddHHmmss").ToString();
                    dr["BOUT"] = form["BOUT"];//int.Parse(ca.get_id("BINDTABLE", feeno)) + 1;//抓最大次數+1
                    dr["ASSESSDT"] = assessdt;
                    dr["BINDID"] = feeno + DateTime.Parse(assessdt).ToString("yyyyMMddHHmm").ToString();

                    dr["FEQ"] = feq;
                    dr["STATUS"] = "批次新增約束";
                    dr["INSDT"] = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString();
                    dr["INSID"] = userinfo.EmployeesNo;
                    dr["INSNAME"] = userinfo.EmployeesName;

                    dr["REASON"] = form["REASON"];
                    dr["CONSCIOUS"] = form["CONSCIOUS"];
                    dr["REACTION"] = form["REACTION"];
                    dr["POSITION"] = form["POSITION"];
                    dr["TOOL"] = form["TOOL"];
                    dr["CYCLE"] = form["CYCLE"];
                    dr["PAUSE"] = form["PAUSE"];
                    dr["ENDING"] = form["ENDING"];
                    dr["HARM"] = form["HARM"];
                    dr["EXPLAIN"] = form["EXPLAIN"];
                    dr["OTHER"] = form["OTHER"];
                    dr["HARM1"] = form["HARM1"];
                    dr["HARM2"] = form["HARM2"];
                    dt_n.Rows.Add(dr);

                }
                if (data_ok)
                {
                    int erow = ca.insert("BINDTABLESAVE", dt_n);
                }
                #endregion
                #region 傳入護理紀錄
                string mag = "", title = "";
                Boolean save_f = false;
                if (form["HARM"] != null && form["HARM"] != "")
                {
                    if (form["HARM"] == "無")
                    {
                        title = "無約束傷害";
                        mag = "病人因" + form["REASON"];
                        mag += "以" + form["TOOL"] + "約束" + form["POSITION"];
                        mag += "，病人意識" + form["CONSCIOUS"];
                        mag += "，" + form["REACTION"];
                        mag += "，約束部位循環" + form["CYCLE"];
                        mag += "，予短暫解除約束" + form["PAUSE"] + "分鐘，";
                        mag += "協助" + form["ENDING"];
                        mag += "，" + form["HARM"] + "約束傷害。";
                    }
                    else
                    {
                        title = "有約束傷害";
                        mag = "病人因" + form["REASON"];
                        mag += "以" + form["TOOL"] + "約束" + form["POSITION"];
                        mag += "，病人意識" + form["CONSCIOUS"];
                        mag += "，" + form["REACTION"];
                        mag += "，約束部位循環" + form["CYCLE"];
                        mag += "，予短暫解除約束" + form["PAUSE"] + "分鐘，";
                        mag += "協助" + form["ENDING"];
                        mag += "，有" + form["HARM"] + "之約束傷害。";
                    }
                }
                else
                {
                    title = "病人於" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString() + "開始約束";
                    mag = "病人因" + form["REASON"];
                    mag += "以" + form["TOOL"] + "約束" + form["POSITION"];
                    mag += "，病人意識" + form["CONSCIOUS"];
                    mag += "，" + form["REACTION"];
                    //mag += "，約束部位循環" + form["CYCLE"];
                    //mag += "，予短暫解除約束" + form["PAUSE"];
                    //mag += "協助" + form["ENDING"];
                    //mag += "，" + form["HARM"] + "約束傷害。";
                }



                if (mag != null && mag != "") { save_f = true; }
                if (save_f)
                {
                    Insert_CareRecord(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), form["ID"], title, "", "", "", mag, "", "ASS_N");
                }
                #endregion
                #region 修改評估主表

                // string id = "";
                DataTable dt_d = new DataTable();
                DataRow dr2 = dt_d.NewRow();
                // dt_d.Columns.Add("ENDDT");
                dt_d.Columns.Add("MODDT");
                dt_d.Columns.Add("MODID");
                dt_d.Columns.Add("MODNAME");
                dt_d.Columns.Add("ASSESS");
                dt_d.Columns.Add("STATUS");

                dt_d.Columns.Add("where");
                if (form["ID"] != null && form["ID"] != "")//更新之流水號不為空
                {
                    data_UP = true;
                    dr2 = dt_d.NewRow();
                    string where = " ID = '" + form["ID"].ToString() + "' ";
                    //   dr2["ENDDT"] = end;
                    dr2["MODDT"] = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString();
                    dr2["MODID"] = userinfo.EmployeesNo;
                    dr2["MODNAME"] = userinfo.EmployeesName;
                    dr2["ASSESS"] = "1";
                    dr2["STATUS"] = "已新增評估";
                    dr2["where"] = where;
                    dt_d.Rows.Add(dr2);
                }

                if (data_UP)
                {
                    //確認是否有存資料 及有無成功
                    int erow = ca.upd("BINDTABLE", dt_d);
                }
                #endregion
            }
            else
            {
                #region 更新評估

                DataTable dt = new DataTable();
                DataRow dr = dt.NewRow();


                data_ok = true;
                dt.Columns.Add("STATUS");
                dt.Columns.Add("MODDT");
                dt.Columns.Add("MODID");
                dt.Columns.Add("MODNAME");
                dt.Columns.Add("ASSESS");

                dt.Columns.Add("REASON");
                dt.Columns.Add("CONSCIOUS");
                dt.Columns.Add("REACTION");
                dt.Columns.Add("POSITION");
                dt.Columns.Add("TOOL");
                dt.Columns.Add("CYCLE");
                dt.Columns.Add("PAUSE");
                dt.Columns.Add("ENDING");
                dt.Columns.Add("HARM");
                dt.Columns.Add("EXPLAIN");
                dt.Columns.Add("OTHER");
                dt.Columns.Add("HARM1");
                dt.Columns.Add("HARM2");

                dt.Columns.Add("where");
                if (form["BINDID"] != null && form["BINDID"] != "")//更新之流水號不為空
                {
                    dr = dt.NewRow();
                    string where = " BINDID = '" + form["BINDID"].ToString() + "' ";
                    dr["STATUS"] = "更新";

                    dr["MODDT"] = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString();
                    dr["MODID"] = userinfo.EmployeesNo;
                    dr["MODNAME"] = userinfo.EmployeesName;

                    dr["REASON"] = form["REASON"];
                    dr["CONSCIOUS"] = form["CONSCIOUS"];
                    dr["REACTION"] = form["REACTION"];
                    dr["POSITION"] = form["POSITION"];
                    dr["TOOL"] = form["TOOL"];
                    dr["CYCLE"] = form["CYCLE"];
                    dr["PAUSE"] = form["PAUSE"];
                    dr["ENDING"] = form["ENDING"];
                    dr["HARM"] = form["HARM"];

                    dr["EXPLAIN"] = form["EXPLAIN"];
                    dr["OTHER"] = form["OTHER"];
                    dr["HARM1"] = form["HARM1"];
                    dr["HARM2"] = form["HARM2"];
                    dr["where"] = where;
                    dt.Rows.Add(dr);
                }
                if (data_ok)
                {
                    int erow = ca.upd("BINDTABLESAVE", dt);
                    //   data_ordseq = false;
                }

                #endregion
                #region 修改評估主表

                // string id = "";
                DataTable dt_d = new DataTable();
                DataRow dr2 = dt_d.NewRow();
                // dt_d.Columns.Add("ENDDT");
                dt_d.Columns.Add("MODDT");
                dt_d.Columns.Add("MODID");
                dt_d.Columns.Add("MODNAME");
                dt_d.Columns.Add("ASSESS");
                dt_d.Columns.Add("STATUS");

                dt_d.Columns.Add("where");
                if (form["BINDID"] != null && form["BINDID"] != "")//更新之流水號不為空
                {
                    data_UP = true;
                    dr2 = dt_d.NewRow();
                    string where = " ID = '" + form["BINDID"].ToString() + "' ";
                    //   dr2["ENDDT"] = end;
                    dr2["MODDT"] = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString();
                    dr2["MODID"] = userinfo.EmployeesNo;
                    dr2["MODNAME"] = userinfo.EmployeesName;
                    dr2["ASSESS"] = "1";
                    dr2["STATUS"] = "已新增評估";
                    dr2["where"] = where;
                    dt_d.Rows.Add(dr2);
                }

                if (data_UP)
                {
                    //確認是否有存資料 及有無成功
                    int erow = ca.upd("BINDTABLE", dt_d);
                }
                #endregion
            }

            if (form["mod"] == "0")
            {
                #region 新增評估 註解了
                /* 
                DataTable dt_n = new DataTable();
                DataRow dr = dt_n.NewRow();
                dt_n = ca.get_newtable("BINDTABLESAVE");
                if (data != null)
                {
                    for (int i = 0; i < data.Count; i++)
                    {
                        //   if (data[i].ASSESS != "0")
                        {

                            dr = dt_n.NewRow();
                            data_ok = true;

                            dr["FEENO"] = feeno;
                            dr["ID"] = form["ID"];//DateTime.Now.ToString("yyyyMMddHHmmss").ToString();
                            dr["BOUT"] = form["BOUT"];//int.Parse(ca.get_id("BINDTABLE", feeno)) + 1;//抓最大次數+1
                            dr["STARTDT"] = form["STARTDT"];//start;
                            dr["ENDDT"] = "";
                            dr["ASSESS"] = data[i].ASSESS;
                            dr["FEQ"] = form["FEQ"];
                            dr["STATUS"] = "約束評估";

                            dr["INSDT"] = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString();
                            dr["INSID"] = userinfo.EmployeesNo;
                            dr["INSNAME"] = userinfo.EmployeesName;
                            dr["EXPLAIN"] = data[i].EXPLAIN;
                            dr["RECORDS"] = data[i].RECORDS;
                            dr["OTHER"] = data[i].OTHER;
                            dr["BINDID"] = DateTime.Now.ToString("yyyyMMddHHmmss").ToString() +
                             DateTime.Parse(data[i].ASSESSDT).ToString("yyyyMMddHHmm").ToString();
                            dr["ASSESSDT"] = data[i].ASSESSDT;
                            dr["REASON"] = data[i].REASON;
                            dr["CONSCIOUS"] = data[i].CONSCIOUS;
                            dr["REACTION"] = data[i].REACTION;
                            dr["POSITION"] = data[i].POSITION;
                            dr["TOOL"] = data[i].TOOL;
                            dr["CYCLE"] = data[i].CYCLE;
                            dr["PAUSE"] = data[i].PAUSE;
                            dr["ENDING"] = data[i].ENDING;
                            dr["HARM"] = data[i].HARM;
                            dr["HARM1"] = data[i].HARM1;
                            dr["HARM2"] = data[i].HARM2;
                            dt_n.Rows.Add(dr);

                        }
                    }
                    if (data_ok)
                    {
                        int erow = ca.insert("BINDTABLESAVE", dt_n);
                        //   data_ordseq = false;
                    }
                }
                #endregion
                #region 修改評估主表

                // string id = "";
                DataTable dt_d = new DataTable();
                DataRow dr2 = dt_d.NewRow();
                // dt_d.Columns.Add("ENDDT");
                dt_d.Columns.Add("MODDT");
                dt_d.Columns.Add("MODID");
                dt_d.Columns.Add("MODNAME");
                dt_d.Columns.Add("ASSESS");
                dt_d.Columns.Add("STATUS");

                dt_d.Columns.Add("where");
                if (form["ID"] != null && form["ID"] != "")//更新之流水號不為空
                {
                    data_UP = true;
                    dr2 = dt_d.NewRow();
                    string where = " ID = '" + form["ID"].ToString() + "' ";
                    //   dr2["ENDDT"] = end;
                    dr2["MODDT"] = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString();
                    dr2["MODID"] = userinfo.EmployeesNo;
                    dr2["MODNAME"] = userinfo.EmployeesName;
                    dr2["ASSESS"] = "1";
                    dr2["STATUS"] = "已新增評估";
                    dr2["where"] = where;
                    dt_d.Rows.Add(dr2);
                }

                if (data_UP)
                {
                    //確認是否有存資料 及有無成功
                    int erow = ca.upd("BINDTABLE", dt_d);
                }*/
                #endregion

            }//如果是第一次新增評估
            else
            {
                #region 修改評估 註解了
                /*
                data_UPn = true;
                DataTable dt_u = new DataTable();
                DataRow dr3 = dt_u.NewRow();
                dr3 = dt_u.NewRow();
                dt_u.Columns.Add("EXPLAIN");
                dt_u.Columns.Add("RECORDS");
                dt_u.Columns.Add("OTHER");
                dt_u.Columns.Add("REASON");
                dt_u.Columns.Add("CONSCIOUS");
                dt_u.Columns.Add("REACTION");
                dt_u.Columns.Add("POSITION");
                dt_u.Columns.Add("TOOL");
                dt_u.Columns.Add("CYCLE");
                dt_u.Columns.Add("PAUSE");
                dt_u.Columns.Add("ENDING");
                dt_u.Columns.Add("HARM");
                dt_u.Columns.Add("HARM1");
                dt_u.Columns.Add("HARM2");

                dt_u.Columns.Add("MODDT");
                dt_u.Columns.Add("MODID");
                dt_u.Columns.Add("MODNAME");
                dt_u.Columns.Add("ASSESS");
                dt_u.Columns.Add("STATUS");

                dt_u.Columns.Add("where");

                for (int i = 0; i < data.Count; i++)
                {
                    if (data[i].ASSESS.ToString().Trim() == "1")
                    {
                        dr3 = dt_u.NewRow();
                        string where = " ID = '" + form["ID"].ToString() + "' and assessdt ='" + data[i].ASSESSDT.ToString() + "' ";

                        dr3["ASSESS"] = data[i].ASSESS;
                        dr3["EXPLAIN"] = data[i].EXPLAIN;
                        dr3["RECORDS"] = data[i].RECORDS;
                        dr3["OTHER"] = data[i].OTHER;
                        dr3["REASON"] = data[i].REASON;
                        dr3["CONSCIOUS"] = data[i].CONSCIOUS;
                        dr3["REACTION"] = data[i].REACTION;
                        dr3["POSITION"] = data[i].POSITION;
                        dr3["TOOL"] = data[i].TOOL;
                        dr3["CYCLE"] = data[i].CYCLE;
                        dr3["PAUSE"] = data[i].PAUSE;
                        dr3["ENDING"] = data[i].ENDING;
                        dr3["HARM"] = data[i].HARM;
                        dr3["HARM1"] = data[i].HARM1;
                        dr3["HARM2"] = data[i].HARM2;


                        dr3["MODDT"] = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString();
                        dr3["MODID"] = userinfo.EmployeesNo;
                        dr3["MODNAME"] = userinfo.EmployeesName;
                        dr3["STATUS"] = "已新增評估";

                        dr3["where"] = where;
                        dt_u.Rows.Add(dr3);
                    }
                }
                if (data_UPn)
                {
                    //確認是否有存資料 及有無成功
                    int erow = ca.upd("BINDTABLESAVE", dt_u);
                }
                 */
                #endregion
            }
            // return View();
            ViewBag.id = form["ID"];
            Response.Write("<script>alert('儲存成功');window.location.href='ListN';</script>");
            return View("ListN");
        }
        [HttpGet]
        public ActionResult Assess_mod(string bindid)
        {
            PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
            string feeno = ptInfo.FeeNo;
            DataTable dt = new DataTable();

            dt = ca.get_table("BINDTABLESAVEROW", feeno, bindid);
            ViewBag.dt = dt;

            return View();
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Assess_mod(FormCollection form, List<binddata> data)
        {
            PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
            string feeno = ptInfo.FeeNo;
            bool data_ok = false;
            bool data_UP = false;
            bool data_UPn = false;
            if (form["mod"] == "0")
            {
                #region 新增評估
                DataTable dt_n = new DataTable();
                DataRow dr = dt_n.NewRow();
                dt_n = ca.get_newtable("BINDTABLESAVE");
                if (data != null)
                {
                    for (int i = 0; i < data.Count; i++)
                    {
                        //   if (data[i].ASSESS != "0")
                        {

                            dr = dt_n.NewRow();
                            data_ok = true;

                            dr["FEENO"] = feeno;
                            dr["ID"] = form["ID"];//DateTime.Now.ToString("yyyyMMddHHmmss").ToString();
                            dr["BOUT"] = form["BOUT"];//int.Parse(ca.get_id("BINDTABLE", feeno)) + 1;//抓最大次數+1
                            dr["STARTDT"] = form["STARTDT"];//start;
                            dr["ENDDT"] = "";
                            dr["ASSESS"] = data[i].ASSESS;
                            dr["FEQ"] = form["FEQ"];
                            dr["STATUS"] = "約束評估";

                            dr["INSDT"] = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString();
                            dr["INSID"] = userinfo.EmployeesNo;
                            dr["INSNAME"] = userinfo.EmployeesName;
                            dr["EXPLAIN"] = data[i].EXPLAIN;
                            dr["RECORDS"] = data[i].RECORDS;
                            dr["OTHER"] = data[i].OTHER;
                            dr["BINDID"] = DateTime.Now.ToString("yyyyMMddHHmmss").ToString() +
                            DateTime.Parse(data[i].ASSESSDT).ToString("yyyyMMddHHmm").ToString();
                            dr["ASSESSDT"] = data[i].ASSESSDT;
                            dr["REASON"] = data[i].REASON;
                            dr["CONSCIOUS"] = data[i].CONSCIOUS;
                            dr["REACTION"] = data[i].REACTION;
                            dr["POSITION"] = data[i].POSITION;
                            dr["TOOL"] = data[i].TOOL;
                            dr["CYCLE"] = data[i].CYCLE;
                            dr["PAUSE"] = data[i].PAUSE;
                            dr["ENDING"] = data[i].ENDING;
                            dr["HARM"] = data[i].HARM;
                            dr["HARM1"] = data[i].HARM1;
                            dr["HARM2"] = data[i].HARM2;
                            dt_n.Rows.Add(dr);
                        }
                    }
                    if (data_ok)
                    {
                        int erow = ca.insert("BINDTABLESAVE", dt_n);
                        //   data_ordseq = false;
                    }
                }
                #endregion
                #region 修改評估主表

                // string id = "";
                DataTable dt_d = new DataTable();
                DataRow dr2 = dt_d.NewRow();
                // dt_d.Columns.Add("ENDDT");
                dt_d.Columns.Add("MODDT");
                dt_d.Columns.Add("MODID");
                dt_d.Columns.Add("MODNAME");
                dt_d.Columns.Add("ASSESS");
                dt_d.Columns.Add("STATUS");

                dt_d.Columns.Add("where");
                if (form["ID"] != null && form["ID"] != "")//更新之流水號不為空
                {
                    data_UP = true;
                    dr2 = dt_d.NewRow();
                    string where = " ID = '" + form["ID"].ToString() + "' ";
                    //   dr2["ENDDT"] = end;
                    dr2["MODDT"] = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString();
                    dr2["MODID"] = userinfo.EmployeesNo;
                    dr2["MODNAME"] = userinfo.EmployeesName;
                    dr2["ASSESS"] = "1";
                    dr2["STATUS"] = "已新增評估";
                    dr2["where"] = where;
                    dt_d.Rows.Add(dr2);
                }

                if (data_UP)
                {
                    //確認是否有存資料 及有無成功
                    int erow = ca.upd("BINDTABLE", dt_d);
                }
                #endregion
            }//如果是第一次新增評估
            else
            {
                #region 修改評估
                data_UPn = true;
                DataTable dt_u = new DataTable();
                DataRow dr3 = dt_u.NewRow();
                dr3 = dt_u.NewRow();
                dt_u.Columns.Add("EXPLAIN");
                dt_u.Columns.Add("RECORDS");
                dt_u.Columns.Add("OTHER");
                dt_u.Columns.Add("REASON");
                dt_u.Columns.Add("CONSCIOUS");
                dt_u.Columns.Add("REACTION");
                dt_u.Columns.Add("POSITION");
                dt_u.Columns.Add("TOOL");
                dt_u.Columns.Add("CYCLE");
                dt_u.Columns.Add("PAUSE");
                dt_u.Columns.Add("ENDING");
                dt_u.Columns.Add("HARM");
                dt_u.Columns.Add("HARM1");
                dt_u.Columns.Add("HARM2");

                dt_u.Columns.Add("MODDT");
                dt_u.Columns.Add("MODID");
                dt_u.Columns.Add("MODNAME");
                dt_u.Columns.Add("ASSESS");
                dt_u.Columns.Add("STATUS");

                dt_u.Columns.Add("where");

                for (int i = 0; i < data.Count; i++)
                {
                    if (data[i].ASSESS.ToString().Trim() == "1")
                    {
                        dr3 = dt_u.NewRow();
                        string where = " ID = '" + form["ID"].ToString() + "' and assessdt ='" + data[i].ASSESSDT.ToString() + "' ";

                        dr3["ASSESS"] = data[i].ASSESS;
                        dr3["EXPLAIN"] = data[i].EXPLAIN;
                        dr3["RECORDS"] = data[i].RECORDS;
                        dr3["OTHER"] = data[i].OTHER;
                        dr3["REASON"] = data[i].REASON;
                        dr3["CONSCIOUS"] = data[i].CONSCIOUS;
                        dr3["REACTION"] = data[i].REACTION;
                        dr3["POSITION"] = data[i].POSITION;
                        dr3["TOOL"] = data[i].TOOL;
                        dr3["CYCLE"] = data[i].CYCLE;
                        dr3["PAUSE"] = data[i].PAUSE;
                        dr3["ENDING"] = data[i].ENDING;
                        dr3["HARM"] = data[i].HARM;
                        dr3["HARM1"] = data[i].HARM1;
                        dr3["HARM2"] = data[i].HARM2;

                        dr3["MODDT"] = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString();
                        dr3["MODID"] = userinfo.EmployeesNo;
                        dr3["MODNAME"] = userinfo.EmployeesName;
                        dr3["STATUS"] = "已新增評估";

                        dr3["where"] = where;
                        dt_u.Rows.Add(dr3);
                    }
                }
                if (data_UPn)
                {
                    //確認是否有存資料 及有無成功
                    int erow = ca.upd("BINDTABLESAVE", dt_u);
                }
                #endregion
            }
            // return View();
            return View("List");
        }

        public ActionResult Print()
        {
            return View();
        }
        #region 舊版暫時不用
        [HttpGet]//舊版暫時不用
        public ActionResult List()
        {
            PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
            string feeno = ptInfo.FeeNo;
            ViewBag.dt_a = ca.get_table("BINDTABLE", feeno, "");
            ViewBag.dt = ca.get_table("BINDTABLESAVE", feeno, "");
            return View();
        }
        [HttpPost]//舊版暫時不用
        [ValidateInput(false)]
        public ActionResult List(FormCollection form, string but, string id)
        {
            PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
            string feeno = ptInfo.FeeNo;
            bool data_ok = false;
            DataTable dt_d = new DataTable();
            DataRow dr = dt_d.NewRow();
            switch (but)
            {
                case "delend":
                    #region 更新
                    //更新 刪除結束日期                                           
                    dt_d.Columns.Add("ENDDT");
                    dt_d.Columns.Add("MODDT");
                    dt_d.Columns.Add("MODID");
                    dt_d.Columns.Add("MODNAME");
                    dt_d.Columns.Add("STATUS");
                    dt_d.Columns.Add("where");
                    if (id != null && id != "")//更新之流水號不為空
                    {
                        data_ok = true;
                        dr = dt_d.NewRow();
                        string where = " ID = '" + id.ToString() + "' ";
                        dr["ENDDT"] = string.Empty;
                        dr["MODDT"] = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString();
                        dr["MODID"] = userinfo.EmployeesNo;
                        dr["MODNAME"] = userinfo.EmployeesName;
                        dr["STATUS"] = "刪除結束註記";
                        dr["where"] = where;
                        dt_d.Rows.Add(dr);
                    }
                    #endregion//更新
                    break;
                case "del":
                    #region 更新
                    //更新 刪除結束日期                                           
                    dt_d.Columns.Add("MODDT");
                    dt_d.Columns.Add("MODID");
                    dt_d.Columns.Add("MODNAME");
                    dt_d.Columns.Add("STATUS");
                    dt_d.Columns.Add("where");
                    if (id != null && id != "")//更新之流水號不為空
                    {
                        data_ok = true;
                        dr = dt_d.NewRow();
                        string where = " ID = '" + id.ToString() + "' ";
                        dr["MODDT"] = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString();
                        dr["MODID"] = userinfo.EmployeesNo;
                        dr["MODNAME"] = userinfo.EmployeesName;
                        dr["STATUS"] = "del";
                        dr["where"] = where;
                        dt_d.Rows.Add(dr);
                    }
                    #endregion//更新
                    break;
            }
            if (data_ok)
            {
                //確認是否有存資料 及有無成功
                int erow = ca.upd("BINDTABLE", dt_d);
            }

            ViewBag.dt_a = ca.get_table("BINDTABLE", feeno, "");
            ViewBag.dt = ca.get_table("BINDTABLESAVE", feeno, "");
            return View();
        }
        [HttpGet]
        public ActionResult Assess(string id, string assess, string start, int bout = 0, int feq = 0)
        {
            PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
            string feeno = ptInfo.FeeNo;
            DataTable dt = new DataTable();
            #region 展開評估時間
            if (assess == "0")
            {
                TimeSpan sum = DateTime.Now - DateTime.Parse(start);
                int nowM = DateTime.Parse(start).Minute;
                int m = sum.Minutes;
                dt = ca.get_newtable("BINDTABLADD");
                DateTime m0 = DateTime.Parse(DateTime.Parse(start).ToString("yyyy/MM/dd HH:00:ss").ToString());
                int fornum = 0;
                switch (feq)
                {
                    case 15:
                        fornum = 8;
                        if (nowM <= 15) { m0 = m0.AddMinutes(0); }
                        else if (nowM <= 30) { m0 = m0.AddMinutes(15); }
                        else if (nowM <= 45) { m0 = m0.AddMinutes(30); }
                        else { m0 = m0.AddMinutes(45); }
                        break;
                    case 30:
                        fornum = 4;
                        if (nowM > 30) { m0 = m0.AddMinutes(30); }
                        break;
                    case 60:
                        fornum = 2;
                        break;

                }
                DataRow dr = dt.NewRow();
                for (int i = 0; i < fornum; i++)
                {
                    dt.Rows.Add(dr);
                    dr["ASSESSDT"] = m0.AddMinutes((i + 1) * feq).ToString("yyyy/MM/dd HH:mm:ss").ToString();
                    dr["ROW"] = i;
                    dr["FEENO"] = feeno;
                    dr["ID"] = id;
                    dr["BOUT"] = bout;
                    dr["STARTDT"] = start;
                    dr["ASSESS"] = "0";
                    dr["FEQ"] = feq;
                    dr["STATUS"] = "建立評估";
                    dr["INSDT"] = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString();
                    dr["INSID"] = userinfo.EmployeesNo;
                    dr["INSNAME"] = userinfo.EmployeesName;

                    dr = dt.NewRow();
                }
                ViewBag.mod = "0";
            }//if(assess == "0")如果還未評估過
            else
            {
                dt = ca.get_table("BINDTABLESAVEMOD", feeno, id);
                ViewBag.mod = "1";
            }
            #endregion //展開評估時間

            ViewBag.dt = dt;

            return View();
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Assess(FormCollection form, List<binddata> data)
        {
            PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
            string feeno = ptInfo.FeeNo;
            bool data_ok = false;
            bool data_UP = false;
            bool data_UPn = false;
            if (form["mod"] == "0")
            {
                #region 新增評估
                DataTable dt_n = new DataTable();
                DataRow dr = dt_n.NewRow();
                dt_n = ca.get_newtable("BINDTABLESAVE");
                if (data != null)
                {
                    for (int i = 0; i < data.Count; i++)
                    {
                        //   if (data[i].ASSESS != "0")
                        {

                            dr = dt_n.NewRow();
                            data_ok = true;

                            dr["FEENO"] = feeno;
                            dr["ID"] = form["ID"];//DateTime.Now.ToString("yyyyMMddHHmmss").ToString();
                            dr["BOUT"] = form["BOUT"];//int.Parse(ca.get_id("BINDTABLE", feeno)) + 1;//抓最大次數+1
                            dr["STARTDT"] = form["STARTDT"];//start;
                            dr["ENDDT"] = "";
                            dr["ASSESS"] = data[i].ASSESS;
                            dr["FEQ"] = form["FEQ"];
                            dr["STATUS"] = "約束評估";

                            dr["INSDT"] = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString();
                            dr["INSID"] = userinfo.EmployeesNo;
                            dr["INSNAME"] = userinfo.EmployeesName;
                            dr["EXPLAIN"] = data[i].EXPLAIN;
                            dr["RECORDS"] = data[i].RECORDS;
                            dr["OTHER"] = data[i].OTHER;
                            dr["BINDID"] = DateTime.Now.ToString("yyyyMMddHHmmss").ToString() +
                             DateTime.Parse(data[i].ASSESSDT).ToString("yyyyMMddHHmm").ToString();
                            dr["ASSESSDT"] = data[i].ASSESSDT;
                            dr["REASON"] = data[i].REASON;
                            dr["CONSCIOUS"] = data[i].CONSCIOUS;
                            dr["REACTION"] = data[i].REACTION;
                            dr["POSITION"] = data[i].POSITION;
                            dr["TOOL"] = data[i].TOOL;
                            dr["CYCLE"] = data[i].CYCLE;
                            dr["PAUSE"] = data[i].PAUSE;
                            dr["ENDING"] = data[i].ENDING;
                            dr["HARM"] = data[i].HARM;
                            dr["HARM1"] = data[i].HARM1;
                            dr["HARM2"] = data[i].HARM2;
                            dt_n.Rows.Add(dr);

                        }
                    }
                    if (data_ok)
                    {
                        int erow = ca.insert("BINDTABLESAVE", dt_n);
                        //   data_ordseq = false;
                    }
                }
                #endregion
                #region 修改評估主表

                // string id = "";
                DataTable dt_d = new DataTable();
                DataRow dr2 = dt_d.NewRow();
                // dt_d.Columns.Add("ENDDT");
                dt_d.Columns.Add("MODDT");
                dt_d.Columns.Add("MODID");
                dt_d.Columns.Add("MODNAME");
                dt_d.Columns.Add("ASSESS");
                dt_d.Columns.Add("STATUS");

                dt_d.Columns.Add("where");
                if (form["ID"] != null && form["ID"] != "")//更新之流水號不為空
                {
                    data_UP = true;
                    dr2 = dt_d.NewRow();
                    string where = " ID = '" + form["ID"].ToString() + "' ";
                    //   dr2["ENDDT"] = end;
                    dr2["MODDT"] = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString();
                    dr2["MODID"] = userinfo.EmployeesNo;
                    dr2["MODNAME"] = userinfo.EmployeesName;
                    dr2["ASSESS"] = "1";
                    dr2["STATUS"] = "已新增評估";
                    dr2["where"] = where;
                    dt_d.Rows.Add(dr2);
                }

                if (data_UP)
                {
                    //確認是否有存資料 及有無成功
                    int erow = ca.upd("BINDTABLE", dt_d);
                }
                #endregion
            }//如果是第一次新增評估
            else
            {
                #region 修改評估
                data_UPn = true;
                DataTable dt_u = new DataTable();
                DataRow dr3 = dt_u.NewRow();
                dr3 = dt_u.NewRow();
                dt_u.Columns.Add("EXPLAIN");
                dt_u.Columns.Add("RECORDS");
                dt_u.Columns.Add("OTHER");
                dt_u.Columns.Add("REASON");
                dt_u.Columns.Add("CONSCIOUS");
                dt_u.Columns.Add("REACTION");
                dt_u.Columns.Add("POSITION");
                dt_u.Columns.Add("TOOL");
                dt_u.Columns.Add("CYCLE");
                dt_u.Columns.Add("PAUSE");
                dt_u.Columns.Add("ENDING");
                dt_u.Columns.Add("HARM");
                dt_u.Columns.Add("HARM1");
                dt_u.Columns.Add("HARM2");

                dt_u.Columns.Add("MODDT");
                dt_u.Columns.Add("MODID");
                dt_u.Columns.Add("MODNAME");
                dt_u.Columns.Add("ASSESS");
                dt_u.Columns.Add("STATUS");

                dt_u.Columns.Add("where");

                for (int i = 0; i < data.Count; i++)
                {
                    if (data[i].ASSESS.ToString().Trim() == "1")
                    {
                        dr3 = dt_u.NewRow();
                        string where = " ID = '" + form["ID"].ToString() + "' and assessdt ='" + data[i].ASSESSDT.ToString() + "' ";

                        dr3["ASSESS"] = data[i].ASSESS;
                        dr3["EXPLAIN"] = data[i].EXPLAIN;
                        dr3["RECORDS"] = data[i].RECORDS;
                        dr3["OTHER"] = data[i].OTHER;
                        dr3["REASON"] = data[i].REASON;
                        dr3["CONSCIOUS"] = data[i].CONSCIOUS;
                        dr3["REACTION"] = data[i].REACTION;
                        dr3["POSITION"] = data[i].POSITION;
                        dr3["TOOL"] = data[i].TOOL;
                        dr3["CYCLE"] = data[i].CYCLE;
                        dr3["PAUSE"] = data[i].PAUSE;
                        dr3["ENDING"] = data[i].ENDING;
                        dr3["HARM"] = data[i].HARM;
                        dr3["HARM1"] = data[i].HARM1;
                        dr3["HARM2"] = data[i].HARM2;


                        dr3["MODDT"] = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString();
                        dr3["MODID"] = userinfo.EmployeesNo;
                        dr3["MODNAME"] = userinfo.EmployeesName;
                        dr3["STATUS"] = "已新增評估";

                        dr3["where"] = where;
                        dt_u.Rows.Add(dr3);
                    }
                }
                if (data_UPn)
                {
                    //確認是否有存資料 及有無成功
                    int erow = ca.upd("BINDTABLESAVE", dt_u);
                }
                #endregion
            }
            // return View();
            return View("List");
        }
        #endregion

        #region print_PDF
        public ActionResult Print_List_PDF(string feeno)
        {

            PatientInfo pinfo = new PatientInfo();
            byte[] ByteCode = webService.GetPatientInfo(feeno);
            //病人資訊
            if (ByteCode != null)
                pinfo = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(ByteCode));
            ViewData["ptinfo"] = pinfo;
            DataTable dt_a = ca.get_table("BINDTABLE", feeno, "");
            ViewBag.dt_a = dt_a;
            ViewBag.dt = ca.get_table("BINDTABLESAVE", feeno, "");
            return View();
        }
        //轉PDF頁面

        public ActionResult Html_To_Pdf(string url)
        {
            string strPath = @"C:\\wkhtmltopdf\\wkhtmltopdf.exe";
            string filename = DateTime.Now.ToString("yyyyMMddHHmmssfff") + url.Substring(url.IndexOf("feeno=") + 6, url.Length - url.IndexOf("feeno=") - 6) + ".pdf";
            string tempPath = "C:\\inetpub\\NIS\\Images\\" + filename;
            //string tempPath = "D:\\Dropbox\\NIS\\NIS\\NIS\\Images\\" + filename;
            string pdfpath = "../Images/" + filename;
            Process p = new Process();
            p.StartInfo.FileName = strPath;
            p.StartInfo.Arguments = url + " " + tempPath;
            p.StartInfo.UseShellExecute = true;
            p.Start();
            p.WaitForExit();
            Response.Write("<script>window.open('Download_Pdf?filename=" + filename + "');window.location.href='ListNew';</script>");

            return new EmptyResult();
        }

        public ActionResult Download_Pdf(string filename)
        {
            string tempPath = "C:\\inetpub\\NIS\\Images\\" + filename;

            FileInfo fileInfo = new FileInfo(tempPath);
            Response.Clear();
            Response.ClearContent();
            Response.ClearHeaders();
            Response.AddHeader("Content-Disposition", "attachment;filename=Report.pdf");
            Response.AddHeader("Content-Length", fileInfo.Length.ToString());
            Response.AddHeader("Content-Transfer-Encoding", "binary");
            Response.ContentType = "application/vnd.ms-excel";
            Response.ContentEncoding = Encoding.UTF8;
            Response.WriteFile(fileInfo.FullName);
            Response.Flush();
            Response.End();
            fileInfo.Delete();

            return new EmptyResult();
        }


        #endregion//print_PDF


        #region 約束主畫面清單  --ListNew---ECK
        //[HttpGet]
        public ActionResult ListNew(string id = "")
        {//判斷有無病人session
            if (Session["PatInfo"] != null)
            {
                ViewBag.RootDocument = GetSourceUrl();
                PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
                string feeno = ptInfo.FeeNo;
                ViewBag.Category = userinfo.Category.ToString();
                // DataTable dt_a1 = ca.get_table("BINDTABLE", feeno, "");
                DataTable dt_a1 = ca.get_table("BINDTABLE_ADD_REASON", feeno, "");
                DataView view = dt_a1.DefaultView;
                if (dt_a1.Rows.Count > 0)
                    view.Sort = "BOUTDESC DESC";
                DataTable dt_a = view.ToTable();
                ViewBag.dt_a = dt_a;
                if (id == "" && dt_a.Rows.Count > 0)
                {//初始取最新的一筆評估的主檔ID給下方撈職時使用
                 //id = dt_a.Rows[dt_a.Rows.Count - 1]["ID"].ToString().Trim();
                    id = dt_a.Rows[0]["ID"].ToString().Trim();
                }
                DataTable dt_x = ca.get_table("BINDTABLESAVE", feeno, id);
                DataView view_x = dt_x.DefaultView;
                if (dt_x.Rows.Count > 0)
                {
                    view_x.Sort = "ASSESSDT ASC";
                }
                DataTable dt = view_x.ToTable();
                ViewBag.dt = dt;
                if (dt_a.Rows.Count > 0)
                {
                    //此為舊版本
                    //ViewBag.BINDTABLE_id = dt_a.Rows[dt_a.Rows.Count - 1]["ID"].ToString().Trim();
                    //ViewBag.lastStartDt = dt_a.Rows[dt_a.Rows.Count - 1]["STARTDT"].ToString().Trim();
                    ////ViewBag.feq = dt_a.Rows[dt_a.Rows.Count - 1]["feq"].ToString().Trim();
                    //ViewBag.feqdt = dt_a.Rows[dt_a.Rows.Count - 1]["feqdt"].ToString().Trim();
                    //ViewBag.sfeqdt = dt_a.Rows[dt_a.Rows.Count - 1]["sfeqdt"].ToString().Trim();
                    //ViewBag.lastmodid = dt_a.Rows[dt_a.Rows.Count - 1]["modid"].ToString().Trim();
                    //ViewBag.lastmodname = dt_a.Rows[dt_a.Rows.Count - 1]["modname"].ToString().Trim();

                    //jarvis修改-----↓
                    //以下版本，將取主表的第一行的相關資訊，給view在新增評估時使用
                    ViewBag.BINDTABLE_id = dt_a.Rows[0]["ID"].ToString().Trim();
                    ViewBag.lastStartDt = dt_a.Rows[0]["STARTDT"].ToString().Trim();
                    //ViewBag.feq = dt_a.Rows[dt_a.Rows.Count - 1]["feq"].ToString().Trim();
                    ViewBag.feqdt = dt_a.Rows[0]["feqdt"].ToString().Trim();
                    ViewBag.sfeqdt = dt_a.Rows[0]["sfeqdt"].ToString().Trim();
                    ViewBag.lastmodid = dt_a.Rows[0]["modid"].ToString().Trim();
                    ViewBag.lastmodname = dt_a.Rows[0]["modname"].ToString().Trim();
                    //jarvis修改-----↑
                    for (int j = 0; j < dt_a.Rows.Count; j++)
                    {
                        string num = ca.CheckAssess(feeno, dt_a.Rows[j]["ID"].ToString().Trim()).ToString();
                        if (num == "0")
                        { dt_a.Rows[j]["ASSESS"] = "due"; }
                        else
                        { dt_a.Rows[j]["ASSESS"] = ""; }
                    }
                }
                if (dt.Rows.Count > 0)
                {
                    ViewBag.lastStartDt = dt.Rows[dt.Rows.Count - 1]["ASSESSDT"].ToString().Trim();
                    ViewBag.feq = dt.Rows[dt.Rows.Count - 1]["feq"].ToString().Trim();
                    //jarvis修改-----↓
                    //ViewBag.lastStartDt = dt.Rows[0]["ASSESSDT"].ToString().Trim();
                    //ViewBag.feq = dt.Rows[0]["feq"].ToString().Trim();
                    //jarvis修改-----↑

                }
                //jarvis修改-----↓//初始值為30分的頻率
                if (dt.Rows.Count == 1)
                {
                    ViewBag.feq = "30";
                }
                //jarvis修改-----↑
                ViewBag.login = userinfo.EmployeesNo;
                ViewBag.feeno = feeno;
            }
            else
            {
                Response.Write("<script>alert('請重新選擇病患');</script>");
                return new EmptyResult();
            }
            return View();
        }

        //[HttpPost]
        //[ValidateInput(false)]
        //public ActionResult ListNew(FormCollection form, string id = "")
        //{
        //    PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
        //    ViewBag.Category = userinfo.Category.ToString();
        //    string feeno = ptInfo.FeeNo;
        //    DataTable dt_a = ca.get_table("BINDTABLE", feeno, "");
        //    ViewBag.dt_a = dt_a;
        //    if(id == "" && dt_a.Rows.Count > 0)
        //    {
        //        //id = dt_a.Rows[dt_a.Rows.Count - 1]["ID"].ToString().Trim();
        //        //jarvis修改-----↓
        //        id = dt_a.Rows[0]["ID"].ToString().Trim();
        //        //jarvis修改-----↑
        //    }
        //    ViewBag.dt = ca.get_table("BINDTABLESAVE", feeno, id);
        //    if(dt_a.Rows.Count > 0)
        //    {
        //        //ViewBag.BINDTABLE_id = dt_a.Rows[dt_a.Rows.Count - 1]["ID"].ToString().Trim();
        //        //ViewBag.lastStartDt = dt_a.Rows[dt_a.Rows.Count - 1]["STARTDT"].ToString().Trim();
        //        //ViewBag.feq = dt_a.Rows[dt_a.Rows.Count - 1]["feq"].ToString().Trim();
        //        //ViewBag.feqdt = dt_a.Rows[dt_a.Rows.Count - 1]["feqdt"].ToString().Trim();
        //        //ViewBag.sfeqdt = dt_a.Rows[dt_a.Rows.Count - 1]["sfeqdt"].ToString().Trim();
        //        //ViewBag.lastmodid = dt_a.Rows[dt_a.Rows.Count - 1]["modid"].ToString().Trim();
        //        //ViewBag.lastmodname = dt_a.Rows[dt_a.Rows.Count - 1]["modname"].ToString().Trim();
        //        //jarvis修改-----↓
        //        ViewBag.BINDTABLE_id = dt_a.Rows[0]["ID"].ToString().Trim();
        //        ViewBag.lastStartDt = dt_a.Rows[0]["STARTDT"].ToString().Trim();
        //        ViewBag.feq = dt_a.Rows[0]["feq"].ToString().Trim();
        //        ViewBag.feqdt = dt_a.Rows[0]["feqdt"].ToString().Trim();
        //        ViewBag.sfeqdt = dt_a.Rows[0]["sfeqdt"].ToString().Trim();
        //        ViewBag.lastmodid = dt_a.Rows[0]["modid"].ToString().Trim();
        //        ViewBag.lastmodname = dt_a.Rows[0]["modname"].ToString().Trim();
        //        //jarvis修改-----↑
        //    }
        //    ViewBag.login = userinfo.EmployeesNo;
        //    ViewBag.feeno = feeno;
        //    return View();
        //}
        #endregion

        #region 設定開始日期
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult StartDate(FormCollection form)
        {
            //宣告病患_取得住院序號
            PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
            string feeno = ptInfo.FeeNo;
            #region LOAD

            ViewBag.title = "新增約束";
            ViewBag.content_title = "約束開始日期時間";
            ViewBag.table_title = "開始日期時間";
            #region SAVE
            string id = DateTime.Now.ToString("yyyyMMddHHmmss").ToString() + "_" + feeno;
            int bout = int.Parse(ca.get_id(feeno)) + 1;
            string start = Convert.ToDateTime(form["start_day"]).ToString("yyyy/MM/dd ");
            start += Convert.ToDateTime(form["start_time"]).ToString("HH:mm:ss");
            //  if (feq != null)

            //DataTable dt_n = new DataTable();
            //dt_n = ca.get_newtable("BINDTABLE");
            //DataRow dr = dt_n.NewRow();
            bool data_ok = false;
            data_ok = true;
            List<DBItem> insertDataListS = new List<DBItem>();
            insertDataListS.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
            insertDataListS.Add(new DBItem("ID", id, DBItem.DBDataType.String));
            insertDataListS.Add(new DBItem("BOUT", bout.ToString(), DBItem.DBDataType.Number));//抓最大次數+1
            insertDataListS.Add(new DBItem("STARTDT", start, DBItem.DBDataType.String));
            insertDataListS.Add(new DBItem("ASSESS", "0", DBItem.DBDataType.String));
            insertDataListS.Add(new DBItem("STATUS", "開始約束", DBItem.DBDataType.String));
            insertDataListS.Add(new DBItem("INSDT", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString(), DBItem.DBDataType.String));
            insertDataListS.Add(new DBItem("INSID", userinfo.EmployeesNo, DBItem.DBDataType.String));
            insertDataListS.Add(new DBItem("INSNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
            insertDataListS.Add(new DBItem("FEQDT", start, DBItem.DBDataType.String));
            insertDataListS.Add(new DBItem("SFEQDT", start, DBItem.DBDataType.String)); // by iven

            if (data_ok)
            {
                int erow = link.DBExecInsert("BINDTABLE", insertDataListS);
                ////   data_ordseq = false;
                #region SAVE 跑馬燈
                List<DBItem> insertDataList = new List<DBItem>();
                DateTime STARTTIME = DateTime.Parse(start).AddDays(1);
                insertDataList.Add(new DBItem("NT_ID", id, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("STARTTIME", STARTTIME.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("FEE_NO", feeno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MEMO", "此病人已約束24小時", DBItem.DBDataType.String));
                //   insertDataList.Add(new DBItem("TIMEOUT", "", DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ACTIONLINK", "", DBItem.DBDataType.String));
                int erow2 = link.DBExecInsert("DATA_NOTICE", insertDataList);
                #endregion
            }

            #endregion

            ViewBag.dt = null;

            #endregion //Loa
            // Response.Write("<script>if(confirm('新增成功!是否開啟簽署同意書?')){@Java_funcion.pup_window('../Images/BMI/Young.pdf');}</script>}else{location.href='../ConstraintsAssessment/AssessNew?id=" + id + "&start=" + start + "&bout=" + bout + "&feq=0';'};</script>");

            Response.Write("<script>location.href='../ConstraintsAssessment/AssessNew?id=" + id + "&start=" + start + "&bout=" + bout + "&feq=0';</script>");
            return new EmptyResult();
        }
        #endregion

        #region 設定結束日期
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult EndDate(FormCollection form)
        {
            //宣告病患_取得住院序號
            PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
            string feeno = ptInfo.FeeNo;
            #region LOAD
            #region 更新
            //更新 結束日期
            bool data_ok = false;
            string reason = "", reason_oth = "";
            string couscious = "", couscious_oth = "";
            //組護理記錄-結束用
            DataTable dt = new DataTable();

            // string id = "";
            List<DBItem> insertDataList2 = new List<DBItem>();
            string end = Convert.ToDateTime(form["end_day"]).ToString("yyyy/MM/dd ");
            end += Convert.ToDateTime(form["end_time"]).ToString("HH:mm");
            string id = form["bind_id"];
            if (id != null && id != "")
            {
                data_ok = true;
                insertDataList2.Add(new DBItem("ENDDT", end, DBItem.DBDataType.String));
                insertDataList2.Add(new DBItem("MODDT", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString(), DBItem.DBDataType.String));
                insertDataList2.Add(new DBItem("MODNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList2.Add(new DBItem("MODID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                insertDataList2.Add(new DBItem("STATUS", "結束評估", DBItem.DBDataType.String));
                dt = ca.get_table("BINDTABLESAVE", feeno, id);
                if (dt != null && dt.Rows.Count > 0)
                {
                    reason = dt.Rows[0]["REASON"].ToString();
                    reason_oth = dt.Rows[0]["REASON_OTHER"].ToString();
                    couscious = dt.Rows[0]["CONSCIOUS"].ToString();
                    couscious_oth = dt.Rows[0]["CONSCIOUS_OTHER"].ToString();
                }
            }
            if (data_ok)
            {
                //確認是否有存資料 及有無成功
                int erow = link.DBExecUpdate("BINDTABLE", insertDataList2, "ID = '" + id.ToString() + "' ");
                //護理紀錄 記錄約束結束 ///---【結束日期】【結束時間】予結束病人【約束部位】【約束方式】。
                string mag = end + " 予結束病人";
                mag += " " + couscious;
                if (couscious_oth != "")
                {
                    mag += ":" + couscious_oth;
                }
                mag += " " + reason;
                if (reason_oth != "")
                {
                    mag += ":" + reason_oth;
                }
                mag += "。";
                Insert_CareRecord(end, "end" + id, "解除約束", mag, "", "", "", "", "ASS_N");
                #region SAVE 跑馬燈結束註記
                List<DBItem> insertDataList = new List<DBItem>();
                insertDataList.Add(new DBItem("TIMEOUT", end, DBItem.DBDataType.DataTime));
                int erow2 = link.DBExecUpdate("DATA_NOTICE", insertDataList, "NT_ID = '" + id.ToString() + "' ");
                //
                string sqlstr = "SELECT * FROM BINDTABLE WHERE ID = '" + id + "'";
                DataTable dtTime = new DataTable();
                string startTime = "";
                string endTime = "";

                link.DBExecSQL(sqlstr, ref dtTime);

                if(dtTime.Rows.Count > 0)
                {
                    startTime = dtTime.Rows[0]["STARTDT"].ToString();
                    endTime = dtTime.Rows[0]["ENDDT"].ToString();
                }
                if(startTime != "" && endTime != "")
                {
                    DateTime startTemp = DateTime.Parse(startTime);
                    DateTime endTemp = DateTime.Parse(endTime);

                    List<Bill_RECORD> billDataList = new List<Bill_RECORD>();
                    Bill_RECORD billData = new Bill_RECORD();

                    var hours = (endTemp - startTemp).TotalHours;
                    string billSet = "8447093";
                    int count = 0;
                    if(hours < 8 )
                    {

                    }
                    else if (hours >= 8 && hours <=24)
                    {
                        count = 1;
                    }
                    else if (hours > 24 && hours <= 48)
                    {
                        count = 2;

                    }
                    else if (hours > 48 && hours <= 72)
                    {
                        count = 3;

                    }
                    else if (hours > 72)
                    {
                        count = 4;

                    }

                    if(count > 0 )
                    {
                        billData.HO_ID = billSet;
                        billData.COUNT = count.ToString();
                        billDataList.Add(billData);

                        SaveBillingRecord(billDataList);
                    }

                }



                #endregion
            }
            #endregion//更新
            #endregion //Load

            Response.Write("<script>location.href='../ConstraintsAssessment/ListNew';</script>");
            return new EmptyResult();

            #region 前台處理結束約束展開評估 現在用不到了
            //string feq = form["feq"];
            //if ((feq != null && feq != "") && Convert.ToDateTime(form["lastStart"]).AddMinutes(int.Parse(feq)) <= Convert.ToDateTime(end))
            //{
            //    string lastmodid = form["endlastmodid"];
            //    string lastmodname = form["endlastmodname"];
            //    //string feqDT = Convert.ToDateTime(form["endfeq_day"]).ToString("yyyy/MM/dd ");
            //    //feqDT += Convert.ToDateTime(form["endfeq_time"]).ToString("HH:mm:ss");
            //    string start = Convert.ToDateTime(form["endfeqdt"]).ToString("yyyy/MM/dd ");
            //    start += Convert.ToDateTime(form["endfeqdt"]).ToString("HH:mm:ss");
            //    int bout = int.Parse(ca.get_id("BINDTABLE", feeno));
            //    Response.Write("<script>location.href='../ConstraintsAssessment/AssessNew?id=" + id + "&lastmodid=" + lastmodid + "&lastmodname=" + lastmodname + "&start=" + start + "&bout=" + bout + "&feqDT=" + end + "&feq=" + feq + "&status=end';</script>");
            //    return new EmptyResult();
            //}
            //else
            //{
            //    Response.Write("<script>location.href='../ConstraintsAssessment/ListNew';</script>");
            //    return new EmptyResult();
            //}
            #endregion //結束約束展開評估
        }
        #endregion

        #region 設定約束頻次及預展時間
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult AddFeq(FormCollection form, string endstart = "")
        {
            //宣告病患_取得住院序號
            PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
            string feeno = ptInfo.FeeNo;
            #region LOAD
            #region 更新 BindTable
            //更新 結束日期
            List<DBItem> insertDataList2 = new List<DBItem>();
            string feqDT = Convert.ToDateTime(form["feq_day"]).ToString("yyyy/MM/dd ");
            feqDT += Convert.ToDateTime(form["feq_time"]).ToString("HH:mm:ss");

            string start = Convert.ToDateTime(form["feqdt"]).ToString("yyyy/MM/dd ");
            start += Convert.ToDateTime(form["feqdt"]).ToString("HH:mm:ss");

            //最後展開時間 by iven
            string sfeqdt = "";
            if (form["sfeqdt"] == "")
                sfeqdt = start;
            else
                sfeqdt = Convert.ToDateTime(form["sfeqdt"]).ToString("yyyy/MM/dd HH:mm:ss");

            if (endstart != "")
            { start = endstart; }

            int bout = int.Parse(ca.get_id(feeno));

            string id = form["bind_id"];
            string feq = form["frequency"];
            string lastmodid = form["lastmodid"];
            string lastmodname = form["lastmodname"];

            if (lastmodid != userinfo.EmployeesNo && (Convert.ToDateTime(feqDT) > Convert.ToDateTime(sfeqdt)))
            {
                Response.Write("<script>location.href='../ConstraintsAssessment/AssessNew?id=" + id + "&lastmodid=" + lastmodid + "&lastmodname=" + lastmodname + "&start=" + sfeqdt + "&bout=" + bout + "&feqDT=" + feqDT + "&feq=" + feq + "&status=batch';</script>");
            }
            if ((lastmodid == userinfo.EmployeesNo && (Convert.ToDateTime(start) <= Convert.ToDateTime(feqDT))))
            {
                /* if (id != null && id != "")
                 {
                     data_ok = true;
                     insertDataList2.Add(new DBItem("FEQDT", feqDT, DBItem.DBDataType.String));
                     insertDataList2.Add(new DBItem("FEQ", feq, DBItem.DBDataType.String));
                     insertDataList2.Add(new DBItem("MODDT", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString(), DBItem.DBDataType.String));
                     insertDataList2.Add(new DBItem("MODNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                     insertDataList2.Add(new DBItem("MODID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                     insertDataList2.Add(new DBItem("STATUS", "預展評估", DBItem.DBDataType.String));

                 }
                 if (data_ok)
                 {
                     //確認是否有存資料 及有無成功
                     int erow = link.DBExecUpdate("BINDTABLE", insertDataList2, "ID = '" + id.ToString() + "' ");
                     //護理紀錄 記錄約束結束
                     ////string mag = "病人於" + end + "結束約束";
                     ////Insert_CareRecord(end, "end" + id, "", "", "", "", mag, "", "ASS_N");
                     ////#region SAVE 跑馬燈結束註記
                     ////List<DBItem> insertDataList = new List<DBItem>();
                     ////insertDataList.Add(new DBItem("TIMEOUT", end, DBItem.DBDataType.DataTime));
                     ////int erow2 = link.DBExecUpdate("DATA_NOTICE", insertDataList, "NT_ID = '" + id.ToString() + "' ");
                     ////#endregion
                 }*/
                Response.Write("<script>location.href='../ConstraintsAssessment/AssessNew?id=" + id + "&lastmodid=" + lastmodid + "&lastmodname=" + lastmodname + "&start=" + start + "&bout=" + bout + "&feqDT=" + feqDT + "&feq=" + feq + "&status=batch';</script>");
            }
            else
            {
                Response.Write("<script>alert('選取範圍內時間以有其他人員展開,請選至" + sfeqdt + "之後的時間+');location.href='../ConstraintsAssessment/ListNew';</script>");
            }
            #endregion//更新
            #endregion //Load
            return new EmptyResult();

        }
        #endregion

        #region 設定批次修改時間
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult QuiryDate(FormCollection form, string endstart = "")
        {
            //宣告病患_取得住院序號
            PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
            string feeno = ptInfo.FeeNo;
            string st = form["stDate"] + " " + form["stTime"];
            string ed = form["edDate"] + " " + form["edTime"];

            int count = Convert.ToInt16(ca.get_table("BINDTABLESAVECOUNT", feeno, st + "|" + ed).Rows[0][0]);
            if (count != 0)
            { Response.Write("<script>location.href='../ConstraintsAssessment/AssessNew_batch?start=" + st + "&end=" + ed + "&feeno=" + feeno + "&status=batch';</script>"); }
            else
            { Response.Write("<script>location.href='../ConstraintsAssessment/ListNew';alert('範圍區間內，無未評估清單');</script>"); }
            return new EmptyResult();

        }
        [HttpGet]
        public ActionResult AssessNew_batch(string start = "", string End = "", string feeno = "")
        {
            start = Request["start"];
            ViewBag.restraint_dt = GetBindingSite("constraint", "restraint");
            ViewBag.BindingSite_dt = GetBindingSite("constraint", "binding_site");
            ViewBag.integrity_dt = GetBindingSite("constraint", "integrity");

            DataTable dt = ca.get_table("BINDTABLESAVE_MODLIST", feeno, start + "|" + End);
            List<String> TList = new List<String>();
            foreach (DataRow r in dt.Rows)
            {
                TList.Add(Convert.ToDateTime(r["ASSESSDT"].ToString()).ToString("yyyy/MM/dd HH:mm"));
            }
            //if(dt != null && dt.Rows.Count > 0)
            //{
            //    DataTable dt_b = ca.get_table("BINDTABLESAVE", feeno, dt.Rows[0]["ID"].ToString());
            //    ViewBag.dt_b = dt_b;
            //}
            ViewData["time"] = TList;
            ViewBag.dt_list = dt;
            return View();
        }

        #endregion
        #region  AssessNew_batch [HttpPost] 新增存檔,取消新增 ---ECK
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult AssessNew_batch(FormCollection form, List<binddata> data)
        {
            PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
            string feeno = ptInfo.FeeNo;
            string LastAssessTime = "";
            bool data_UP = false;

            #region 新增評估資料
            {
                //新增多筆展開頻次之評估
                string assdt = form["assdt[]"];//展開有需評估時間  
                string assdtAll = form["assdtAll[]"];//展開所有評估時間  

                #region 新增評估
                for (int t = 0; t < assdtAll.Split(',').Length; t++)
                {
                    if (assdt != null && assdt.IndexOf(assdtAll.Split(',').GetValue(t).ToString()) >= 0)
                    {
                        //有打勾 t為第幾筆 從0開始算                            
                        string bindid = "";
                        for (int n = 0; n < assdt.Split(',').Length; n++)
                        {
                            if (assdt.Split(',').GetValue(n).ToString().IndexOf(assdtAll.Split(',').GetValue(t).ToString()) >= 0)
                            {
                                string assdttime = assdt.Split(',').GetValue(n).ToString().Split('|').GetValue(0).ToString();
                                bindid = assdt.Split(',').GetValue(n).ToString().Split('|').GetValue(1).ToString();
                            }
                        }
                        #region 更新評估
                        data_UP = true;
                        Boolean save_f = false;
                        List<String> TList = new List<String>();
                        ViewData["time"] = TList;
                        List<DBItem> insertDataList = new List<DBItem>();
                        insertDataList.Add(new DBItem("STATUS", "更新", DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("MODDT", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString(), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("MODID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("MODNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("ASSESS", "1", DBItem.DBDataType.String));//已經評估  
                        //insertDataList.Add(new DBItem("ASSESSDT", form["ASSESSDT"], DBItem.DBDataType.String));  <=修改時約束日期不更新
                        LastAssessTime = DateTime.Parse(assdtAll.Split(',').GetValue(t).ToString()).ToString("yyyy/MM/dd HH:mm:ss");

                        if (form["ck_restraint" + t] != null)
                        {
                            insertDataList.Add(new DBItem("REASON", form["ck_restraint" + t], DBItem.DBDataType.String));
                        }
                        string Temp_txt1 = (form["txt1" + t] == null) ? "" : form["txt1" + t];
                        insertDataList.Add(new DBItem("REASON_OTHER", Temp_txt1, DBItem.DBDataType.String));//
                        if (form["ck_bindingsite" + t] != null)
                        {
                            insertDataList.Add(new DBItem("CONSCIOUS", form["ck_bindingsite" + t], DBItem.DBDataType.String));
                        }//
                        string Temp_txt2 = (form["txt2" + t] == null) ? "" : form["txt2" + t];
                        insertDataList.Add(new DBItem("CONSCIOUS_OTHER", Temp_txt2, DBItem.DBDataType.String));//

                        if (form["rb_pulse" + t] != null)
                            insertDataList.Add(new DBItem("PULSE", form["rb_pulse" + t], DBItem.DBDataType.String));
                        if (form["rb_tightness" + t] != null)
                            insertDataList.Add(new DBItem("TIGHTNESS", form["rb_tightness" + t], DBItem.DBDataType.String));
                        if (form["rb_temperature" + t] != null)
                            insertDataList.Add(new DBItem("TEMPERATURE", form["rb_temperature" + t], DBItem.DBDataType.String));
                        if (form["rb_colour" + t] != null)
                            insertDataList.Add(new DBItem("COLOUR", form["rb_colour" + t], DBItem.DBDataType.String));
                        if (form["ck_integrity" + t] != null)
                            insertDataList.Add(new DBItem("INTEGRITY", form["ck_integrity" + t], DBItem.DBDataType.String));
                        if (form["rb_feeling" + t] != null)
                            insertDataList.Add(new DBItem("FEELING", form["rb_feeling" + t], DBItem.DBDataType.String));
                        if (form["rb_remove" + t] != null)
                            insertDataList.Add(new DBItem("REMOVE", form["rb_remove" + t], DBItem.DBDataType.String));
                        string Temp_txt10 = (form["txt10" + t] == null) ? "" : form["txt10" + t];
                        insertDataList.Add(new DBItem("REMARK", Temp_txt10, DBItem.DBDataType.String));//備註

                        #region 批次儲存
                        if (data_UP)
                        {
                            int erow = link.DBExecUpdate("BINDTABLESAVE", insertDataList, " FEENO = '" + feeno + "' AND BINDID = '" + bindid + "' ");
                            if (erow > 0)
                                save_f = true;
                        }
                        #endregion //批次儲存
                        #region 護理紀錄組字--///by jarvis 20160908
                        string mag = "";
                        if (save_f == true)
                        {
                            mag = "予病人 " + form["ck_bindingsite" + t];
                            if (form["txt2" + t] != null)
                            {
                                mag += ":" + form["txt2" + t];
                            }
                            mag += " " + form["ck_restraint" + t];
                            if (form["txt1" + t] != null)
                            {
                                mag += ":" + form["txt1" + t];
                            }
                            mag += "，約束鬆緊度" + form["rb_tightness" + t];
                            mag += "，約束部位周圍皮膚" + form["ck_integrity" + t];
                            mag += "，病人脈搏" + form["rb_pulse" + t];
                            mag += "，肢體末端" + form["rb_colour" + t] + form["rb_temperature" + t];
                            mag += "，感覺" + form["rb_feeling" + t];
                            if (form["rb_remove" + t] == "是")
                            {
                                mag += "，" + "鬆除約束協助進行翻身、清潔及肢體活動";
                            }
                            mag += "。";
                        }
                        #endregion//護理紀錄組字-批次新增-按照舊規則只有新增第一筆的一筆護理記錄
                        #region 新增護理紀錄--///by jarvis 20160908
                        if (mag != null && mag != "") { save_f = true; } else { save_f = false; }
                        if (save_f)
                        {
                            //string id_temp = "consrtaint_" + form["ID"] + "_" + DateTime.Parse(assdtAll.Split(',').GetValue(t).ToString()).ToString("MMddHHmmss");//consrtaint_20160819151351_I0332966_0819122800
                            string id_temp = "consrtaint_" + bindid;
                            string time_temp = DateTime.Parse(assdtAll.Split(',').GetValue(t).ToString()).ToString("yyyy/MM/dd HH:mm:ss");//2016/08/19 12:28:00
                            Insert_CareRecord(time_temp, id_temp, "約束評估", mag, "", "", "", "", "ASS_N");
                            //Insert_CareRecord_Black(time_temp, id_temp, "約束評估", "", "", mag, "", "");/*----2016/08/22 11:52--by jarvis 多筆新增這部分會與單筆存紅字需多增加判斷，先remark.學姐說先註解，等反映再來修正、改善。
                        }
                        #endregion //新增護理紀錄
                        #endregion
                    }
                }
                #endregion
            }
            ViewBag.id = form["ID"];
            Response.Write("<script>alert('儲存成功');window.location.href='ListNew';</script>");
            return View("ListNew");
        }

        #endregion//設定批次修改時間
        #endregion

        #region 評估表展開 AssessNew ---ECK
        #region  AssessNew [HttpGet]
        [HttpGet]
        public ActionResult AssessNew(string id, string start, string bindid, string status, string feqDT, string lastmodid, string lastmodname, string bout, int feq = 0)
        {
            //start 第一筆時為開始時間;status=batch時為批次新增評估 此時的start為上一次feqdt時間
            DataTable dt = new DataTable();
            PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
            string feeno = ptInfo.FeeNo;
            ViewBag.Restraint_dt = GetBindingSite("constraint", "restraint");
            ViewBag.BindingSite_dt = GetBindingSite("constraint", "binding_site");
            ViewBag.Integrity_dt = GetBindingSite("constraint", "integrity");
            if (bindid != null && bindid != "")//修改狀態
            {
                dt = ca.get_table("BINDTABLESAVEMOD", feeno, bindid);
                DataTable dt2 = ca.get_table("BINDTABLESAVE", feeno, id);
                DataTable NewDT = FiltData(dt2, "BINDID='" + bindid + "'", "");
                ViewBag.dt2 = NewDT;
                List<String> TList = new List<String>();
                ViewData["time"] = TList;
            }
            else//新增狀態
            {
                #region 判別頻次
                if (feq == 0)//頻次等於1或0為第一次評估 只有一筆
                {
                    #region 第一筆評估
                    if (feq == 0)
                    {
                        ViewBag.assessdt = start;
                    }
                    #endregion //第一筆評估
                    List<String> TList = new List<String>();
                    ViewData["time"] = TList;
                }
                else //展開多筆評估
                {
                    #region 展開多筆評估
                    DataTable dt_a = ca.get_table("BINDTABLE", feeno, id);
                    DataTable dt_b = ca.get_table("BINDTABLESAVE", feeno, id);
                    string assessStime = "";
                    string feqdt = "";
                    if (dt_a.Rows.Count > 0)
                    {
                        if (dt_b.Rows.Count > 0)
                        {
                            ViewBag.dt_b = dt_b;
                            //如果有評估資料 最後一筆記錄時間 繼續展
                            //assessStime = dt_b.Rows[dt_b.Rows.Count - 1]["ASSESSDT"].ToString().Trim();

                            //抓取約束最後一筆時間 by iven
                            if (lastmodid == userinfo.EmployeesNo)
                            {
                                //增修同人抓有評估的最後一筆時間
                                assessStime = dt_b.AsEnumerable()
                                    .Where(row => row["ASSESS"].ToString() == "1")
                                    .Max(row => row["ASSESSDT"]).ToString();
                            }
                            else
                            {
                                //增修不同人抓展開的最後一筆時間
                                assessStime = dt_b.AsEnumerable()
                                    .Max(row => row["ASSESSDT"]).ToString();
                            }
                        }
                        else
                        {
                            assessStime = dt_a.Rows[dt_a.Rows.Count - 1]["STARTDT"].ToString().Trim();
                        }

                        //feq = int.Parse(dt_a.Rows[dt_a.Rows.Count - 1]["feq"].ToString().Trim());//最後頻次
                        if (status != "end")
                        {
                            feqdt = dt_a.Rows[dt_a.Rows.Count - 1]["feqdt"].ToString().Trim();//最後設定時間  
                        }
                        else
                        { feqdt = feqDT; }

                        if (lastmodid == userinfo.EmployeesNo)
                        {  //修改者跟後面修改者為同人 未至需評估時間不需寫入資料庫
                            #region 增修同人
                            DateTime m0 = DateTime.Parse(DateTime.Parse(assessStime).ToString("yyyy/MM/dd HH:mm:ss").ToString());
                            //展開開始時間
                            // DateTime mEnd = DateTime.Now;
                            //if (DateTime.Parse(DateTime.Parse(feqdt).ToString("yyyy/MM/dd HH:mm:ss").ToString()) < DateTime.Now)
                            // {
                            // mEnd = DateTime.Parse(DateTime.Parse(feqdt).ToString("yyyy/MM/dd HH:mm:ss").ToString());
                            //如果預展時間小於現在 展至選取時間
                            // }

                            //直接取結束時間 by iven
                            DateTime mEnd = DateTime.Parse(DateTime.Parse(feqDT).ToString("yyyy/MM/dd HH:mm:ss").ToString());
                            List<String> TList = new List<String>();
                            int i = 0;
                            while (m0.AddMinutes((i + 1) * feq) <= mEnd)
                            {
                                TList.Add(m0.AddMinutes((i + 1) * feq).ToString("yyyy/MM/dd HH:mm").ToString());
                                i++;
                            }
                            if (i == 0)
                            { //如果沒有展開評估
                                if (status != "end")
                                {
                                    Response.Write("<script>alert('選取範圍內時間評估皆已列出');window.location.href='ListNew';</script>");
                                }
                                else
                                { Response.Write("<script>window.location.href='ListNew';</script>"); }

                            }
                            ViewData["time"] = TList;
                            ViewBag.dt_b = dt_b;//約束列表 按順序排 遞增

                            #endregion //增修同人
                        }
                        else
                        {
                            #region 增修不同人
                            DateTime m0 = DateTime.Parse(DateTime.Parse(assessStime).ToString("yyyy/MM/dd HH:mm:ss").ToString());
                            DateTime mEnd = DateTime.Parse(DateTime.Parse(feqDT).ToString("yyyy/MM/dd HH:mm:ss").ToString());
                            int i = 0;
                            string assessTime = "";
                            while (m0.AddMinutes((i + 1) * feq) <= mEnd)
                            {
                                assessTime = m0.AddMinutes((i + 1) * feq).ToString("yyyy/MM/dd HH:mm:ss").ToString();
                                i++;
                            }
                            if ((m0.AddMinutes((i + 1) * feq) <= mEnd) != false)//assess會為空
                            {
                                m0 = DateTime.Parse(DateTime.Parse(assessTime).ToString("yyyy/MM/dd HH:mm:ss").ToString());
                            }
                            List<String> TList = new List<String>();

                            i = 0;
                            while (m0.AddMinutes((i + 1) * feq) <= mEnd)
                            {
                                TList.Add(m0.AddMinutes((i + 1) * feq).ToString("yyyy/MM/dd HH:mm:ss").ToString());
                                i++;
                            }
                            if (i == 0)
                            { //如果沒有展開評估
                                if (status != "end")
                                {
                                    Response.Write("<script>alert('選取範圍內時間評估皆已列出');window.location.href='ListNew';</script>");
                                }
                                else
                                { Response.Write("<script>window.location.href='ListNew';</script>"); }
                            }
                            ViewData["time"] = TList;
                            #endregion //增修不同人
                        }
                    }
                    #endregion //展開多筆評估
                }
                #endregion //判別頻次
                #region 展開評估時間 內容註解
                /*
                if (feq != 0 && feq != 1)
                {
                    DataTable dtr = ca.get_newrow(id);
                    DataTable dtr1 = ca.get_newrow1(id);
                    ViewBag.dtr = dtr1;
                    if (dtr != null && dtr.Rows.Count > 0)
                    {
                        if (dtr.Rows[0]["ASSESSDT"].ToString() != null && dtr.Rows[0]["ASSESSDT"].ToString() != "")
                        { start = dtr.Rows[0]["ASSESSDT"].ToString(); }
                    }
                    TimeSpan sum = DateTime.Now - DateTime.Parse(start);
                    int nowM = DateTime.Parse(start).Minute;
                    int m = sum.Minutes;
                    dt = ca.get_newtable("BINDTABLADD");
                    DateTime m0 = DateTime.Parse(DateTime.Parse(start).ToString("yyyy/MM/dd HH:00:ss").ToString());
                    //   int fornum = 0;
                    switch (feq)
                    {
                        case 15:
                            //     fornum = 8;
                            if (nowM <= 15) { m0 = m0.AddMinutes(0); }
                            else if (nowM <= 30) { m0 = m0.AddMinutes(15); }
                            else if (nowM <= 45) { m0 = m0.AddMinutes(30); }
                            else { m0 = m0.AddMinutes(45); }
                            break;
                        case 30:
                            //       fornum = 4;
                            if (nowM > 30) { m0 = m0.AddMinutes(30); }
                            break;
                        case 60:
                            //      fornum = 2;
                            break;

                    }
                    DataRow dr = dt.NewRow();
                    List<String> TList = new List<String>();
                    // List<DBItem> TList = new List<DBItem>();
                    int i = 0;
                    while (m0.AddMinutes((i + 1) * feq) < DateTime.Now)
                    {

                        TList.Add(m0.AddMinutes((i + 1) * feq).ToString("yyyy/MM/dd HH:mm:ss").ToString());
                        i++;
                    }
                    if (i == 0)
                    { //如果沒有展開評估
                        Response.Write("<script>alert('未至需評估時間');window.location.href='ListN';</script>");
                    }

                    ViewData["time"] = TList;
                    ViewBag.mod = "0";
                }
                else if (feq == 1)
                {// 修改狀態
                    if (bindid != null && bindid != "")
                    {
                        dt = ca.get_table("BINDTABLESAVEMOD", feeno, bindid);
                    }
                }
                else
                {
                    start = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString();
                    DataTable dtr = ca.get_table("BINDTABLE2", feeno, "");
                    if (dtr != null && dtr.Rows.Count > 0)
                    {
                        if (dtr.Rows[0]["STARTDT"].ToString() != null && dtr.Rows[0]["STARTDT"].ToString() != "")
                        { start = dtr.Rows[0]["STARTDT"].ToString(); }
                    }
                    ViewBag.assessdt = start;
                }
                */
                #endregion //展開評估時間
            }

            ViewBag.BOUT = bout;
            ViewBag.dt = dt;
            ViewBag.feq = feq;
            ViewBag.id = id;
            return View();
        }

        /// <summary>---ECK 
        /// SYS_PARAMS 取得欄位
        /// </summary>
        /// <param name="pModel"></param>
        /// <param name="pGroup"></param>
        /// <returns>回傳一個dt 有詳細的欄位資訊</returns>
        protected internal DataTable GetBindingSite(string pModel, string pGroup)
        {
            string StrSql = "SELECT * FROM SYS_PARAMS WHERE P_MODEL='" + pModel + "' AND P_GROUP='" + pGroup + "' ORDER BY P_SORT";
            DataTable dt = this.link.DBExecSQL(StrSql);
            return dt;
        }
        #endregion// AssessNew [HttpGet]

        #region  AssessNew [HttpPost] 新增存檔,取消新增---ECK
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult AssessNew(FormCollection form, List<binddata> data, int feq)
        {
            try
            {
                PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
                string feeno = ptInfo.FeeNo;
                string LastAssessTime = "", LastAssessTime2 = "";
                bool data_ok = false;
                bool data_UP = false;

                if (form["BINDID"] != null && form["BINDID"] != "")//更新之流水號不為空
                {
                    //BINDID有值時為修改 更新資料
                    #region 更新評估主檔
                    //判斷更新評估是否小於記錄時間 by iven
                    string assessdt = form["ASSESSDT"];
                    string strsql = "SELECT COUNT(*) CC FROM BINDTABLE WHERE FEENO = '" + feeno + "' AND ID = '" + form["ID"] + "'";
                    strsql += " AND FEQDT < '" + assessdt + "'";
                    DataTable Dt = link.DBExecSQL(strsql);
                    if (Dt.Rows.Count > 0)
                    {
                        strsql = "UPDATE BINDTABLE SET FEQDT = '" + assessdt + "' WHERE FEENO = '" + feeno + "' AND ID = '" + form["ID"] + "'";
                        link.DBExecSQL(strsql, false);
                    }

                    #endregion
                    #region 更新評估
                    data_UP = true;
                    List<String> TList = new List<String>();
                    ViewData["time"] = TList;
                    List<DBItem> insertDataList = new List<DBItem>();
                    insertDataList.Add(new DBItem("STATUS", "更新", DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("MODDT", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString(), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("MODID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("MODNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASSESS", "1", DBItem.DBDataType.String));//已經評估
                    insertDataList.Add(new DBItem("REASON", form["ck_restraint"], DBItem.DBDataType.String));//
                    string Temp_txt1 = (form["txt1"] == null) ? "" : form["txt1"];
                    insertDataList.Add(new DBItem("REASON_OTHER", Temp_txt1, DBItem.DBDataType.String));//
                    insertDataList.Add(new DBItem("CONSCIOUS", form["ck_bindingsite"], DBItem.DBDataType.String));//
                    string Temp_txt2 = (form["txt2"] == null) ? "" : form["txt2"];
                    insertDataList.Add(new DBItem("CONSCIOUS_OTHER", Temp_txt2, DBItem.DBDataType.String));//
                    #region 更新評估(完整資料時需更新項目)
                    if (feq != 0)//頻次0為第一筆資料
                    {
                        insertDataList.Add(new DBItem("PULSE", form["rb_pulse"], DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("TIGHTNESS", form["rb_tightness"], DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("TEMPERATURE", form["rb_temperature"], DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("COLOUR", form["rb_colour"], DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("INTEGRITY", form["ck_integrity"], DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("FEELING", form["rb_feeling"], DBItem.DBDataType.String));
                        if (form["rb_remove"] != null)
                        {
                            insertDataList.Add(new DBItem("REMOVE", form["rb_remove"], DBItem.DBDataType.String));
                        }
                        string Temp_txt10 = (form["txt10"] == null) ? "" : form["txt10"];
                        insertDataList.Add(new DBItem("REMARK", Temp_txt10, DBItem.DBDataType.String));//備註

                    }
                    #endregion//更新評估(完整資料)
                    #region 護理紀錄組字--///by jarvis 20160727--暫時註記，資料內容與舊版些微不同
                    #region 舊版本
                    //if (feq == 0)
                    //{
                    //    string[] other = form["OTHER"].ToString().Split(',');
                    //    if (form["HARM"] != null && form["HARM"] != "")
                    //    {
                    //        save_f = true;

                    //        if (form["HARM"] == "無")
                    //        {
                    //            title = "無約束傷害";
                    //            mag = "病人因" + form["REASON"] + other[0];
                    //            mag += "以" + form["TOOL"] + other[4] + "約束" + form["POSITION"] + other[3];
                    //            mag += "，病人意識" + form["CONSCIOUS"] + other[1];
                    //            mag += "，" + form["REACTION"] + other[2];
                    //            mag += "，約束部位循環" + form["CYCLE"] + other[5];
                    //            mag += "，予短暫解除約束" + form["PAUSE"] + "分鐘，";
                    //            mag += "協助" + form["ENDING"] + other[6];
                    //            mag += "，" + form["HARM"] + other[7] + "約束傷害。";
                    //        }
                    //        else
                    //        {
                    //            title = "有約束傷害";
                    //            mag = "病人因" + form["REASON"];
                    //            mag += "以" + form["TOOL"] + "約束" + form["POSITION"] + other[3];
                    //            mag += "，病人意識" + form["CONSCIOUS"] + other[1];
                    //            mag += "，" + form["REACTION"] + other[2];
                    //            mag += "，約束部位循環" + form["CYCLE"] + other[5];
                    //            mag += "，予短暫解除約束" + form["PAUSE"] + "分鐘，";
                    //            mag += "協助" + form["ENDING"] + other[6];
                    //            mag += "，有" + form["HARM"] + other[7] + "之約束傷害。";
                    //        }
                    //    }
                    //    else
                    //    {
                    //        title = "病人於" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString() + "開始約束";

                    //        mag = "病人因" + form["REASON"];
                    //        mag += "以" + form["TOOL"] + "約束" + form["POSITION"] + other[3];
                    //        mag += "，病人意識" + form["CONSCIOUS"] + other[1];
                    //        mag += "，" + form["REACTION"] + other[2] + "。";
                    //    }
                    //}
                    //↓ new by.jarvis.20160816 add  
                    //----病人因【約束原因】予【約束方式】，約束鬆緊度【約束鬆緊程度】，約束部位周圍皮膚【皮膚完整姓】，病人脈搏【脈搏】，肢體末端 【顏色】【肢體末端溫度】，感覺【感覺度】，鬆除約束協助進行翻身、清潔及肢體活動。
                    //--註：『鬆除約束協助進行翻身、清潔及肢體活動』只有在『鬆除約束、翻身、肢體活動、清潔』選『有』的時後才呈現。
                    #endregion
                    string mag = "";
                    Boolean save_f = false;
                    if (feq == 0)
                    {//應該不會進來這裡//目前第一次的不予修改
                        save_f = true;
                        //mag += "病人因" + form["ck_restraint"];
                        //if(Temp_txt1 != "")
                        //{
                        //    mag += ":" + Temp_txt1;
                        //}
                        //mag += "予" + form["ck_bindingsite"];
                        //if(Temp_txt2 != "")
                        //{
                        //    mag += ":" + Temp_txt2;
                        //}
                        //mag += "。";
                        //【開始日期】【開始時間】予病人【約束部位】【約束方式】。
                        mag = form["ASSESSDT"] + " 予病人 " + form["ck_bindingsite"];
                        if (Temp_txt2 != "")
                        {
                            mag += ":" + Temp_txt2;
                        }
                        mag += " " + form["ck_restraint"];
                        if (Temp_txt1 != "")
                        {
                            mag += ":" + Temp_txt1;
                        }
                        mag += "。";
                        #region 更新護理紀錄--///by jarvis 20160908
                        if (mag != null && mag != "") { save_f = true; }
                        if (save_f)
                        {
                            string id_Temp = "start" + form["ID"];
                            int erow = Upd_CareRecord(Convert.ToDateTime(form["ASSESSDT"].ToString().Trim()).ToString("yyyy/MM/dd HH:mm:ss"), id_Temp, "新增評估", mag, "", "", "", "", "ASS_N");
                        }
                        #endregion //新增護理紀錄
                    }
                    else
                    {
                        //予病人【約束部位】【約束方式】，約束鬆緊度【約束鬆緊程度】，約束部位周圍皮膚【皮膚完整姓】，
                        //病人脈搏【脈搏】，肢體末端 【顏色】【肢體末端溫度】，感覺【感覺度】，鬆除約束協助進行翻身、清潔及肢體活動。
                        //註：『鬆除約束協助進行翻身、清潔及肢體活動』只有在『鬆除約束、翻身、肢體活動、清潔』選『有』的時後才呈現。

                        save_f = true;
                        //mag += "病人因" + form["ck_restraint"];
                        //if(Temp_txt1 != "")
                        //{
                        //    mag += ":" + Temp_txt1;
                        //}
                        //mag += "予" + form["ck_bindingsite"];
                        //if(Temp_txt2 != "")
                        //{
                        //    mag += ":" + Temp_txt2;
                        //}
                        mag = "予病人 " + form["ck_bindingsite"];
                        if (Temp_txt2 != "")
                        {
                            mag += ":" + Temp_txt2;
                        }
                        mag += " " + form["ck_restraint"];
                        if (Temp_txt1 != "")
                        {
                            mag += ":" + Temp_txt1;
                        }
                        mag += "，約束鬆緊度" + form["rb_tightness"];
                        mag += "，約束部位周圍皮膚" + form["ck_integrity"];
                        mag += "，病人脈搏" + form["rb_pulse"];
                        mag += "，肢體末端" + form["rb_colour"] + form["rb_temperature"];
                        mag += "，感覺" + form["rb_feeling"];
                        if (form["rb_remove"] == "是")
                        {
                            mag += "，" + "鬆除約束協助進行翻身、清潔及肢體活動";
                        }
                        mag += "。";
                        #region 更新護理紀錄--///by jarvis 20160908--單筆修改
                        if (mag != null && mag != "") { save_f = true; }
                        if (save_f)
                        {
                            //string id_Temp = "consrtaint_" + form["ID"];
                            string id_Temp = "consrtaint_" + form["BINDID"];
                            DataTable checkbindidAreUse = this.link.DBExecSQL("SELECT * FROM CARERECORD_DATA WHERE  FEENO = '" + feeno + "' AND CARERECORD_ID = '" + id_Temp + "' AND SELF = 'ASS_N' AND DELETED IS NULL");
                            if (checkbindidAreUse.Rows.Count > 0 && checkbindidAreUse != null)
                            {//已評估過的要修改
                                int erow = Upd_CareRecord(Convert.ToDateTime(form["ASSESSDT"].ToString().Trim()).ToString("yyyy/MM/dd HH:mm:ss"), id_Temp, "約束評估", mag, "", "", "", "", "ASS_N");
                            }
                            else
                            {//紅色background 的未評估的要新增
                                int erow = Insert_CareRecord(Convert.ToDateTime(form["ASSESSDT"].ToString().Trim()).ToString("yyyy/MM/dd HH:mm:ss"), id_Temp, "約束評估", mag, "", "", "", "", "ASS_N");

                            }
                        }
                        #endregion //新增護理紀錄
                    }
                    #endregion//護理紀錄組字


                    if (data_UP)
                    {   //WHERE條件多加FEENO by iven
                        int erow = link.DBExecUpdate("BINDTABLESAVE", insertDataList, " FEENO = '" + feeno + "' AND BINDID = '" + form["BINDID"] + "' ");
                        if (erow > 0)
                        {
                            //更新成功需UPDATA BINDTABLE.FEQDT by iven
                            string strtmp = AssessNew_Cancel(form["ID"]);
                            Response.Write("<script>alert('更新成功');window.location.href='ListNew';</script>");
                        }
                        else
                        { Response.Write("<script>alert('更新失敗');window.location.href='ListNew';</script>"); }
                    }


                    #endregion//更新評估
                }
                else //新增評估
                {
                    #region 新增評估資料
                    if (feq == 0)
                    {//頻次沒有選 表示為首次評估=>需寫入護理紀錄
                        #region 新增評估
                        //DataTable dt_n = new DataTable();
                        //DataRow dr = dt_n.NewRow();
                        //dt_n = ca.get_newtable("BINDTABLESAVE");
                        data_ok = true;
                        List<DBItem> insertDataList = new List<DBItem>();
                        insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("ID", form["ID"], DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("BINDID", "bindid_" + form["ID"] + "_" + DateTime.Now.ToString("yyyyMMddHHmmss").ToString(), DBItem.DBDataType.String));//抓最大次數+1
                        insertDataList.Add(new DBItem("BOUT", form["BOUT"], DBItem.DBDataType.String));
                        //insertDataList.Add(new DBItem("STARTDT", "", DBItem.DBDataType.String));
                        //insertDataList.Add(new DBItem("ENDDT", "", DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("ASSESS", "1", DBItem.DBDataType.String));//已經評估
                        insertDataList.Add(new DBItem("FEQ", "0", DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("STATUS", "開始約束", DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("INSDT", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString(), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("INSID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("INSNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("MODDT", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString(), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("MODID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("MODNAME", userinfo.EmployeesName, DBItem.DBDataType.String));

                        insertDataList.Add(new DBItem("ASSESSDT", form["ASSESSDT"], DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("REASON", form["ck_restraint"], DBItem.DBDataType.String));//
                        string Temp_txt1 = (form["txt1"] == null) ? "" : form["txt1"];
                        insertDataList.Add(new DBItem("REASON_OTHER", Temp_txt1, DBItem.DBDataType.String));//
                        insertDataList.Add(new DBItem("CONSCIOUS", form["ck_bindingsite"], DBItem.DBDataType.String));//
                        string Temp_txt2 = (form["txt2"] == null) ? "" : form["txt2"];
                        insertDataList.Add(new DBItem("CONSCIOUS_OTHER", Temp_txt2, DBItem.DBDataType.String));//
                        //                               insertDataList.Add(new DBItem("REACTION", form["REACTION"], DBItem.DBDataType.String));
                        //                               insertDataList.Add(new DBItem("POSITION", form["POSITION"], DBItem.DBDataType.String));
                        //                               insertDataList.Add(new DBItem("TOOL", form["TOOL"], DBItem.DBDataType.String));
                        //                               insertDataList.Add(new DBItem("OTHER", form["OTHER"], DBItem.DBDataType.String));
                        //第一次約束 不需要顯示
                        //insertDataList.Add(new DBItem("CYCLE", form["CYCLE"], DBItem.DBDataType.String));
                        //insertDataList.Add(new DBItem("PAUSE", form["PAUSE"], DBItem.DBDataType.String));
                        //insertDataList.Add(new DBItem("ENDING", form["ENDING"], DBItem.DBDataType.String));
                        //insertDataList.Add(new DBItem("HARM", form["HARM"], DBItem.DBDataType.String));

                        //insertDataList.Add(new DBItem("EXPLAIN", form["EXPLAIN"], DBItem.DBDataType.String));

                        //insertDataList.Add(new DBItem("HARM1", form["HARM1"], DBItem.DBDataType.String));
                        //insertDataList.Add(new DBItem("HARM2", form["HARM2"], DBItem.DBDataType.String));   

                        if (data_ok)
                        {
                            int erow = link.DBExecInsert("BINDTABLESAVE", insertDataList);
                            //   data_ordseq = false;
                        }
                        #endregion
                        #region 護理紀錄組字--///by jarvis 20160908--新增單筆
                        string mag = "";
                        Boolean save_f = false;
                        if (feq == 0)
                        {
                            save_f = true;
                            //【開始日期】【開始時間】予病人【約束部位】【約束方式】。
                            mag = form["ASSESSDT"] + " 予病人 " + form["ck_bindingsite"];
                            if (Temp_txt2 != "")
                            {
                                mag += ":" + Temp_txt2;
                            }
                            mag += " " + form["ck_restraint"];
                            if (Temp_txt1 != "")
                            {
                                mag += ":" + Temp_txt1;
                            }
                            mag += "。";
                            #region 新增護理紀錄--///by jarvis 20160908
                            if (mag != null && mag != "") { save_f = true; }
                            if (save_f)
                            {
                                Insert_CareRecord(Convert.ToDateTime(form["ASSESSDT"].ToString().Trim()).ToString("yyyy/MM/dd HH:mm:ss"), "start_" + form["ID"], "新增約束", mag, "", "", "", "", "ASS_N");
                            }
                            #endregion //第一次的約束評估
                        }
                        else
                        {//應該不會跳進來
                            save_f = true;
                            //予病人【約束部位】【約束方式】，約束鬆緊度【約束鬆緊程度】，約束部位周圍皮膚【皮膚完整姓】，
                            //病人脈搏【脈搏】，肢體末端 【顏色】【肢體末端溫度】，感覺【感覺度】，鬆除約束協助進行翻身、清潔及肢體活動。
                            //註：『鬆除約束協助進行翻身、清潔及肢體活動』只有在『鬆除約束、翻身、肢體活動、清潔』選『有』的時後才呈現。

                            mag = "予病人 " + form["ck_bindingsite"];
                            if (Temp_txt2 != "")
                            {
                                mag += ":" + Temp_txt2;
                            }
                            mag += " " + form["ck_restraint"];
                            if (Temp_txt1 != "")
                            {
                                mag += ":" + Temp_txt1;
                            }
                            mag += "，約束鬆緊度" + form["rb_tightness"];
                            mag += "，約束部位周圍皮膚" + form["ck_integrity"];
                            mag += "，病人脈搏" + form["rb_pulse"];
                            mag += "，肢體末端" + form["rb_colour"] + form["rb_temperature"];
                            mag += "，感覺" + form["rb_feeling"];
                            if (form["rb_remove"] == "是")
                            {
                                mag += "，" + "鬆除約束協助進行翻身、清潔及肢體活動";
                            }
                            mag += "。";
                            #region 新增護理紀錄--///by jarvis 20160908
                            if (mag != null && mag != "") { save_f = true; }
                            if (save_f)
                            {
                                Insert_CareRecord(Convert.ToDateTime(form["ASSESSDT"].ToString().Trim()).ToString("yyyy/MM/dd HH:mm:ss"), "consrtaint_" + form["ID"], "約束評估", mag, "", "", "", "", "ASS_N");
                            }
                            #endregion //新增護理紀錄
                        }

                        #endregion//護理紀錄組字

                        #region 修改評估主表--
                        data_UP = true;
                        List<DBItem> insertDataList2 = new List<DBItem>();
                        insertDataList2.Add(new DBItem("ASSESS", "1", DBItem.DBDataType.String));
                        //insertDataList2.Add(new DBItem("FEQ", feq.ToString(), DBItem.DBDataType.String));
                        insertDataList2.Add(new DBItem("MODDT", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString(), DBItem.DBDataType.String));
                        insertDataList2.Add(new DBItem("MODNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                        insertDataList2.Add(new DBItem("MODID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                        insertDataList2.Add(new DBItem("STATUS", "已新增評估", DBItem.DBDataType.String));

                        if (data_UP)
                        {                        //確認是否有存資料 及有無成功
                                                 // int erow = ca.upd("BINDTABLE", dt_d);
                            int erow = link.DBExecUpdate("BINDTABLE", insertDataList2, "ID = '" + form["ID"] + "' ");
                        }
                        #endregion
                    }
                    else //if (feq != 1)
                    {
                        //新增多筆展開頻次之評估
                        string assdt = form["assdt[]"];//展開有需評估時間  
                        string assdtAll = form["assdtAll[]"];//展開所有評估時間  

                        #region 新增評估
                        //string[] times = assdt.Split(',');//第幾筆_時間                                   
                        string record = "";
                        for (int t = 0; t < assdtAll.Split(',').Length; t++)
                        {
                            if (assdt == null)
                            {   //當沒有勾選任何評估時間時 by iven
                                #region 展開不儲存內容 只存時間
                                List<DBItem> insertDataList = new List<DBItem>();
                                insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                                insertDataList.Add(new DBItem("ID", form["ID"], DBItem.DBDataType.String));
                                insertDataList.Add(new DBItem("BINDID", "bindid_" + form["ID"] + "_" + DateTime.Parse(assdtAll.Split(',').GetValue(t).ToString()).ToString("MMddHHmm") + "_" + userinfo.EmployeesNo, DBItem.DBDataType.String));//抓最大次數+1
                                insertDataList.Add(new DBItem("BOUT", form["BOUT"], DBItem.DBDataType.String));
                                insertDataList.Add(new DBItem("ASSESSDT", DateTime.Parse(assdtAll.Split(',').GetValue(t).ToString()).ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));//抓最大次數+1
                                insertDataList.Add(new DBItem("FEQ", feq.ToString(), DBItem.DBDataType.String));
                                insertDataList.Add(new DBItem("STATUS", "批次展開約束", DBItem.DBDataType.String));
                                insertDataList.Add(new DBItem("INSDT", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString(), DBItem.DBDataType.String));
                                insertDataList.Add(new DBItem("INSID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                                //展開沒填內容不要INSERT NAME by iven
                                //insertDataList.Add(new DBItem("INSNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                                insertDataList.Add(new DBItem("ASSESS", "0", DBItem.DBDataType.String));//0展出時間 未做評估
                                                                                                        //加上儲存約束原因及方式 //by jarvis 20160908
                                if (form["ck_restraint" + t] != null)
                                    insertDataList.Add(new DBItem("REASON", form["ck_restraint" + t], DBItem.DBDataType.String));
                                if (form["ck_bindingsite" + t] != null)
                                    insertDataList.Add(new DBItem("CONSCIOUS", form["ck_bindingsite" + t], DBItem.DBDataType.String));
                                if (form["txt1" + t] != null)
                                {
                                    insertDataList.Add(new DBItem("REASON_OTHER", form["txt1" + t], DBItem.DBDataType.String));
                                }
                                if (form["txt2" + t] != null)
                                {
                                    insertDataList.Add(new DBItem("CONSCIOUS_OTHER", form["txt2" + t], DBItem.DBDataType.String));
                                }
                                link.DBExecInsert("BINDTABLESAVE", insertDataList);

                                #endregion //展開不儲存內容 只存時間
                            }
                            else if (assdt.IndexOf(assdtAll.Split(',').GetValue(t).ToString()) >= 0)
                            {//有打勾 t為第幾筆 從0開始算 
                             //string aa = form["rb_reasonT" + t];
                             //string bb = form["ck_reactionT" + t];
                             //int xx = assdt.IndexOf(assdtAll.Split(',').GetValue(t).ToString());///在字串中搜尋找到的位置，等於無意義的位子，只能證明找到
                                #region 批次儲存
                                string bind_id = "bindid_" + form["ID"] + "_" + DateTime.Parse(assdtAll.Split(',').GetValue(t).ToString()).ToString("MMddHHmm") + "_" + userinfo.EmployeesNo;//修改bind_idd統一跟護理記錄id同
                                List<DBItem> insertDataList = new List<DBItem>();
                                insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                                insertDataList.Add(new DBItem("ID", form["ID"], DBItem.DBDataType.String));
                                insertDataList.Add(new DBItem("BINDID", bind_id, DBItem.DBDataType.String));//抓最大次數+1
                                insertDataList.Add(new DBItem("BOUT", form["BOUT"], DBItem.DBDataType.Number));
                                //insertDataList.Add(new DBItem("STARTDT", "", DBItem.DBDataType.String));
                                //insertDataList.Add(new DBItem("ENDDT", "", DBItem.DBDataType.String));
                                insertDataList.Add(new DBItem("ASSESS", "1", DBItem.DBDataType.String));//1為有評估內容
                                insertDataList.Add(new DBItem("FEQ", feq.ToString(), DBItem.DBDataType.String));
                                insertDataList.Add(new DBItem("STATUS", "批次儲存約束", DBItem.DBDataType.String));
                                insertDataList.Add(new DBItem("INSDT", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString(), DBItem.DBDataType.String));
                                insertDataList.Add(new DBItem("INSID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                                insertDataList.Add(new DBItem("INSNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                                insertDataList.Add(new DBItem("MODDT", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString(), DBItem.DBDataType.String));
                                insertDataList.Add(new DBItem("MODID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                                insertDataList.Add(new DBItem("MODNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                                insertDataList.Add(new DBItem("ASSESSDT", DateTime.Parse(assdtAll.Split(',').GetValue(t).ToString()).ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));//抓最大次數+1

                                LastAssessTime = DateTime.Parse(assdtAll.Split(',').GetValue(t).ToString()).ToString("yyyy/MM/dd HH:mm:ss");
                                if (form["ck_restraint" + t] != null)
                                    insertDataList.Add(new DBItem("REASON", form["ck_restraint" + t], DBItem.DBDataType.String));
                                if (form["ck_bindingsite" + t] != null)
                                    insertDataList.Add(new DBItem("CONSCIOUS", form["ck_bindingsite" + t], DBItem.DBDataType.String));
                                //if (form["ck_reaction" + t] != null)
                                //    insertDataList.Add(new DBItem("REACTION", form["ck_reaction" + t], DBItem.DBDataType.String));
                                //if (form["ck_position" + t] != null)
                                //    insertDataList.Add(new DBItem("POSITION", form["ck_position" + t], DBItem.DBDataType.String));
                                //if (form["ck_tool" + t] != null)
                                //    insertDataList.Add(new DBItem("TOOL", form["ck_tool" + t], DBItem.DBDataType.String));
                                //if (form["OTHER" + t] != null)
                                //    insertDataList.Add(new DBItem("OTHER", form["OTHER" + t], DBItem.DBDataType.String));

                                if (form["txt1" + t] != null)
                                {
                                    insertDataList.Add(new DBItem("REASON_OTHER", form["txt1" + t], DBItem.DBDataType.String));
                                }
                                if (form["txt2" + t] != null)
                                {
                                    insertDataList.Add(new DBItem("CONSCIOUS_OTHER", form["txt2" + t], DBItem.DBDataType.String));
                                }
                                if (form["rb_pulse" + t] != null)
                                {
                                    insertDataList.Add(new DBItem("PULSE", form["rb_pulse" + t], DBItem.DBDataType.String));
                                }
                                if (form["rb_tightness" + t] != null)
                                {
                                    insertDataList.Add(new DBItem("TIGHTNESS", form["rb_tightness" + t], DBItem.DBDataType.String));
                                }

                                if (form["rb_temperature" + t] != null)
                                {
                                    insertDataList.Add(new DBItem("TEMPERATURE", form["rb_temperature" + t], DBItem.DBDataType.String));
                                }
                                if (form["rb_colour" + t] != null)
                                {
                                    insertDataList.Add(new DBItem("COLOUR", form["rb_colour" + t], DBItem.DBDataType.String));
                                }
                                if (form["ck_integrity" + t] != null)
                                {
                                    insertDataList.Add(new DBItem("INTEGRITY", form["ck_integrity" + t], DBItem.DBDataType.String));
                                }
                                if (form["rb_feeling" + t] != null)
                                {
                                    insertDataList.Add(new DBItem("FEELING", form["rb_feeling" + t], DBItem.DBDataType.String));
                                }

                                if (form["rb_remove" + t] != null)
                                {
                                    insertDataList.Add(new DBItem("REMOVE", form["rb_remove" + t], DBItem.DBDataType.String));
                                }
                                //string Temp_txt10 = (form["txt10" + t] == null) ? "" : form["txt10" + t];
                                //insertDataList.Add(new DBItem("REMARK", Temp_txt10, DBItem.DBDataType.String));//備註
                                if (form["txt10" + t] != null)
                                {
                                    insertDataList.Add(new DBItem("REMARK", form["txt10" + t], DBItem.DBDataType.String));
                                }
                                link.DBExecInsert("BINDTABLESAVE", insertDataList);
                                if (record == "")
                                {
                                    #region 護理紀錄組字--///by jarvis 20160908
                                    string mag = "";
                                    Boolean save_f = false;
                                    if (feq == 0)
                                    {//應該不會跳進來
                                        save_f = true;
                                        //【開始日期】【開始時間】予病人【約束部位】【約束方式】。
                                        mag = DateTime.Parse(assdtAll.Split(',').GetValue(t).ToString()).ToString("yyyy/MM/dd HH:mm:ss");
                                        mag += " 予病人 " + form["ck_bindingsite" + t];
                                        if (form["txt2" + t] != null)
                                        {
                                            mag += ":" + form["txt2" + t];
                                        }
                                        mag += " " + form["ck_restraint" + t];
                                        if (form["txt1" + t] != null)
                                        {
                                            mag += ":" + form["txt1" + t];
                                        }
                                        mag += "。";
                                        #region 新增護理紀錄--///by jarvis 20160908
                                        if (mag != null && mag != "") { save_f = true; }
                                        if (save_f)
                                        {
                                            Insert_CareRecord(Convert.ToDateTime(form["ASSESSDT"].ToString().Trim()).ToString("yyyy/MM/dd HH:mm:ss"), "start" + form["ID"], "新增約束", mag, "", "", "", "", "ASS_N");
                                        }
                                        #endregion
                                    }
                                    else
                                    {///
                                        save_f = true;
                                        mag = "予病人 " + form["ck_bindingsite" + t];
                                        if (form["txt2" + t] != null)
                                        {
                                            mag += ":" + form["txt2" + t];
                                        }
                                        mag += " " + form["ck_restraint" + t];
                                        if (form["txt1" + t] != null)
                                        {
                                            mag += ":" + form["txt1" + t];
                                        }
                                        mag += "，約束鬆緊度" + form["rb_tightness" + t];
                                        mag += "，約束部位周圍皮膚" + form["ck_integrity" + t];
                                        mag += "，病人脈搏" + form["rb_pulse" + t];
                                        mag += "，肢體末端" + form["rb_colour" + t] + form["rb_temperature" + t];
                                        mag += "，感覺" + form["rb_feeling" + t];
                                        if (form["rb_remove" + t] == "是")
                                        {
                                            mag += "，" + "鬆除約束協助進行翻身、清潔及肢體活動";
                                        }
                                        mag += "。";
                                    }
                                    #endregion//護理紀錄組字-批次新增-按照舊規則只有新增第一筆的一筆護理記錄
                                    #region 新增護理紀錄--///by jarvis 20160908
                                    if (mag != null && mag != "") { save_f = true; }
                                    if (save_f)
                                    {
                                        //string id_temp = "consrtaint_" + form["ID"] + "_" + DateTime.Parse(assdtAll.Split(',').GetValue(t).ToString()).ToString("MMddHHmmss");//consrtaint_20160819151351_I0332966_0819122800
                                        string id_temp = "consrtaint_" + bind_id;
                                        string time_temp = DateTime.Parse(assdtAll.Split(',').GetValue(t).ToString()).ToString("yyyy/MM/dd HH:mm:ss");//2016/08/19 12:28:00
                                        Insert_CareRecord(time_temp, id_temp, "約束評估", mag, "", "", "", "", "ASS_N");
                                        //Insert_CareRecord_Black(time_temp, id_temp, "約束評估", "", "", mag, "", "");/*----2016/08/22 11:52--by jarvis 多筆新增這部分會與單筆存紅字需多增加判斷，先remark.學姐說先註解，等反映再來修正、改善。
                                    }
                                    #endregion //新增護理紀錄
                                    //record = "Y";
                                }

                                //  h += DateTime.Parse(times[b].Split('_').GetValue(1).ToString()).ToString("yyyy/MM/dd HH:mm:ss") + "</ br>";
                                #endregion //批次儲存
                            }
                            else
                            { //沒有打勾的項目
                                #region 展開不儲存內容 只存時間
                                List<DBItem> insertDataList = new List<DBItem>();
                                insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                                insertDataList.Add(new DBItem("ID", form["ID"], DBItem.DBDataType.String));
                                insertDataList.Add(new DBItem("BINDID", "bindid_" + form["ID"] + "_" + DateTime.Parse(assdtAll.Split(',').GetValue(t).ToString()).ToString("MMddHHmm") + "_" + userinfo.EmployeesNo, DBItem.DBDataType.String));//抓最大次數+1
                                insertDataList.Add(new DBItem("BOUT", form["BOUT"], DBItem.DBDataType.String));
                                insertDataList.Add(new DBItem("ASSESSDT", DateTime.Parse(assdtAll.Split(',').GetValue(t).ToString()).ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));//抓最大次數+1
                                insertDataList.Add(new DBItem("FEQ", feq.ToString(), DBItem.DBDataType.String));
                                insertDataList.Add(new DBItem("STATUS", "批次展開約束", DBItem.DBDataType.String));
                                insertDataList.Add(new DBItem("INSDT", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString(), DBItem.DBDataType.String));
                                insertDataList.Add(new DBItem("INSID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                                //展開沒填內容不要INSERT NAME by iven
                                //insertDataList.Add(new DBItem("INSNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                                insertDataList.Add(new DBItem("ASSESS", "0", DBItem.DBDataType.String));//0展出時間 未做評估
                                                                                                        //加上儲存約束原因及方式 by jarvis 20160908
                                if (form["ck_restraint" + t] != null)
                                    insertDataList.Add(new DBItem("REASON", form["ck_restraint" + t], DBItem.DBDataType.String));
                                if (form["ck_bindingsite" + t] != null)
                                    insertDataList.Add(new DBItem("CONSCIOUS", form["ck_bindingsite" + t], DBItem.DBDataType.String));
                                if (form["txt1" + t] != null)
                                {
                                    insertDataList.Add(new DBItem("REASON_OTHER", form["txt1" + t], DBItem.DBDataType.String));
                                }
                                if (form["txt2" + t] != null)
                                {
                                    insertDataList.Add(new DBItem("CONSCIOUS_OTHER", form["txt2" + t], DBItem.DBDataType.String));
                                }
                                link.DBExecInsert("BINDTABLESAVE", insertDataList);

                                #endregion //展開不儲存內容 只存時間
                            }
                            if (t == assdtAll.Split(',').Length - 1)
                                LastAssessTime2 = DateTime.Parse(assdtAll.Split(',').GetValue(t).ToString()).ToString("yyyy/MM/dd HH:mm:ss");
                        }

                        #endregion
                        #region 修改評估主表
                        //data_UP = true;
                        List<DBItem> insertDataList2 = new List<DBItem>();
                        insertDataList2.Add(new DBItem("ASSESS", "1", DBItem.DBDataType.String));
                        // insertDataList2.Add(new DBItem("FEQ", feq, DBItem.DBDataType.String));
                        insertDataList2.Add(new DBItem("MODDT", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString(), DBItem.DBDataType.String));
                        insertDataList2.Add(new DBItem("MODNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                        insertDataList2.Add(new DBItem("MODID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                        insertDataList2.Add(new DBItem("STATUS", "已新增評估", DBItem.DBDataType.String));
                        //修改最後評估時間 by iven
                        if (LastAssessTime != "")
                            insertDataList2.Add(new DBItem("FEQDT", LastAssessTime, DBItem.DBDataType.String));
                        insertDataList2.Add(new DBItem("SFEQDT", LastAssessTime2, DBItem.DBDataType.String));
                        int erow = link.DBExecUpdate("BINDTABLE", insertDataList2, "ID = '" + form["ID"] + "' ");
                        #endregion

                        //20150330 預展後 換登入者，未帶上次資訊 會刪掉最後幾筆資料 新增後再做TEST
                        //新增評估後，要清掉重覆的時間
                        string strsql = "DELETE BINDTABLESAVE WHERE FEENO='" + feeno + "' AND ID='" + form["ID"].ToString() + "'";
                        strsql += " AND INSID = '" + userinfo.EmployeesNo + "' AND ASSESSDT > (SELECT MAX(ASSESSDT) FROM BINDTABLESAVE";
                        strsql += " WHERE FEENO='" + feeno + "' AND ID='" + form["ID"].ToString() + "' AND ASSESS <> '0')";

                        link.DBExecSQL(strsql, false);
                    }
                    #endregion//新增評估資料
                }

                ViewBag.id = form["ID"];
                Response.Write("<script>alert('儲存成功');window.location.href='ListNew';</script>");
                return View("ListNew");
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                Response.Write("<script>alert('儲存失敗');window.location.href='ListNew';</script>");
                return View("ListNew");
            }
        }

        public string AssessNew_Cancel(string id)
        {
            try
            {
                string feq = Request["Pfeq"];
                if (feq != "Y")
                {
                    //在新增時,點放棄目前編輯資料要把FEQDT的時間修改 (放最後一筆評估時間) by iven
                    string sql = "UPDATE BINDTABLE SET FEQDT = (SELECT MAX(ASSESSDT) FROM BINDTABLESAVE WHERE ID='" + id + "' AND ASSESS <> '0')";
                    sql += " WHERE ID='" + id + "'";
                    link.DBExecSQL(sql, false);

                }
                else
                {//刪除剛建立的主表，並只有在第一筆準備要建立時，按放棄鈕的時候執行
                    string where = "ID='" + id + "' AND FEENO='" + ptinfo.FeeNo + "' ";
                    int erow = this.link.DBExecDelete("BINDTABLE", where);
                }
                return "OK";
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return "NG";
            }
        }
        #endregion// AssessNew [HttpPost]

        #endregion// 評估表展開
        #region 刪除列表
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult DelListNew(FormCollection form, string but, string id, string bindid, string saveid)
        {
            PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
            string feeno = ptInfo.FeeNo;
            bool data_ok = false;
            bool data_save = false;
            ViewBag.login = userinfo.EmployeesNo;
            DataTable dt_d = new DataTable();
            DataRow dr = dt_d.NewRow();
            switch (but)
            {
                case "delend":
                    #region 更新
                    //更新 刪除結束日期                                           
                    dt_d.Columns.Add("ENDDT");
                    dt_d.Columns.Add("MODDT");
                    dt_d.Columns.Add("MODID");
                    dt_d.Columns.Add("MODNAME");
                    dt_d.Columns.Add("STATUS");
                    dt_d.Columns.Add("where");
                    if (id != null && id != "")//更新之流水號不為空
                    {
                        data_ok = true;
                        dr = dt_d.NewRow();
                        string where = " ID = '" + id.ToString() + "' ";
                        dr["ENDDT"] = string.Empty;
                        dr["MODDT"] = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString();
                        dr["MODID"] = userinfo.EmployeesNo;
                        dr["MODNAME"] = userinfo.EmployeesName;
                        dr["STATUS"] = "刪除結束註記";
                        dr["where"] = where;
                        dt_d.Rows.Add(dr);
                    }
                    #endregion//更新
                    #region 刪除 結束約束的護理紀錄
                    // Del_CareRecord("end" + id, "ASS_N");
                    base.Del_CareRecord("end" + id, "ASS_N");
                    link.DBExecDelete("CARERECORD_DATA", "CARERECORD_ID = 'end" + id + "' AND SELF = 'ASS_N' ");
                    #endregion//刪除 結束約束的護理紀錄
                    break;
                case "del":
                    #region 更新
                    //更新 刪除列表日期                                           
                    dt_d.Columns.Add("MODDT");
                    dt_d.Columns.Add("MODID");
                    dt_d.Columns.Add("MODNAME");
                    dt_d.Columns.Add("STATUS");
                    dt_d.Columns.Add("where");
                    id = saveid;
                    if (id != null && id != "")//更新之流水號不為空
                    {
                        data_ok = true;
                        data_save = true;
                        dr = dt_d.NewRow();
                        string where = " ID = '" + id.ToString() + "' ";
                        dr["MODDT"] = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString();
                        dr["MODID"] = userinfo.EmployeesNo;
                        dr["MODNAME"] = userinfo.EmployeesName;
                        dr["STATUS"] = "del";
                        dr["where"] = where;
                        dt_d.Rows.Add(dr);
                    }
                    #endregion//更新
                    #region SAVE 跑馬燈結束註記
                    List<DBItem> insertDataList = new List<DBItem>();
                    insertDataList.Add(new DBItem("TIMEOUT", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString(), DBItem.DBDataType.DataTime));
                    int erow2 = link.DBExecUpdate("DATA_NOTICE", insertDataList, "NT_ID = '" + id.ToString() + "' ");
                    string bug_where = " NT_ID IN (SELECT NT_ID FROM DATA_NOTICE "
                        + " WHERE MEMO ='此病人已約束24小時' AND TIMEOUT = TO_DATE('9999-12-31','YYYY-MM-DD HH24:MI:SS') "
                        + " AND NT_ID IN  (SELECT DISTINCT ID FROM BINDTABLE WHERE STATUS='del')  UNION SELECT NT_ID FROM DATA_NOTICE"
                        + " WHERE MEMO ='此病人已約束24小時' AND TIMEOUT = TO_DATE('9999-12-31','YYYY-MM-DD HH24:MI:SS') "
                        + " AND FEE_NO NOT IN (SELECT DISTINCT FEENO FROM BINDTABLE  ))";
                    int erow3 = link.DBExecDelete("DATA_NOTICE", bug_where);
                    #endregion
                    break;
                case "delFeq"://刪除評估記錄
                    #region 刪除評估記錄
                    //更新 刪除評估記錄                                           
                    dt_d.Columns.Add("MODDT");
                    dt_d.Columns.Add("MODID");
                    dt_d.Columns.Add("MODNAME");
                    dt_d.Columns.Add("STATUS");
                    /*------------以下為修改的新項目-----------*/
                    dt_d.Columns.Add("PULSE");
                    dt_d.Columns.Add("TIGHTNESS");
                    dt_d.Columns.Add("TEMPERATURE");
                    dt_d.Columns.Add("COLOUR");
                    dt_d.Columns.Add("INTEGRITY");
                    dt_d.Columns.Add("FEELING");
                    dt_d.Columns.Add("REMOVE");
                    dt_d.Columns.Add("REMARK");
                    dt_d.Columns.Add("INSNAME");
                    dt_d.Columns.Add("ASSESS");

                    dt_d.Columns.Add("where");
                    if (saveid != null && saveid != "" && bindid != null && bindid != "")//更新之流水號不為空
                    {
                        data_save = true;
                        dr = dt_d.NewRow();
                        string where = " ID = '" + saveid.ToString().Trim() + "' and bindid= '" + bindid.ToString().Trim() + "'";
                        dr["MODDT"] = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").ToString();
                        dr["MODID"] = userinfo.EmployeesNo;
                        dr["MODNAME"] = "";// userinfo.EmployeesName;//新增與修改人名稱都要改為空
                        dr["STATUS"] = "D";//"del";//恩主公希望能看到刪除的資料，所以多個D的狀態
                        /*------------以下為修改的新項目-----------*/
                        dr["PULSE"] = "";
                        dr["TIGHTNESS"] = "";
                        dr["TEMPERATURE"] = "";
                        dr["COLOUR"] = "";
                        dr["INTEGRITY"] = "";
                        dr["FEELING"] = "";
                        dr["REMOVE"] = "";
                        dr["REMARK"] = "";
                        dr["INSNAME"] = "";
                        dr["ASSESS"] = "0";

                        dr["where"] = where;
                        dt_d.Rows.Add(dr);
                    }
                    #endregion//更新
                    break;
            }
            if (data_ok)
            {
                //確認是否有存資料 及有無成功
                int erow = ca.upd("BINDTABLE", dt_d);
                if (erow > 0)
                {
                    Del_CareRecord("start_" + bindid.ToString().Trim(), "ASS_N");
                    Del_CareRecord("end" + bindid.ToString().Trim(), "ASS_N");
                }
            }
            if (data_save)
            {
                //確認是否有存資料 及有無成功
                int erow = ca.upd("BINDTABLESAVE", dt_d);
                if (erow > 0)
                {
                    Del_CareRecord("consrtaint_" + bindid.ToString().Trim(), "ASS_N");
                }
            }
            //將以下兩段註解掉，目前沒發現問題
            // ViewBag.dt_a = ca.get_table("BINDTABLE", feeno, "");
            // ViewBag.dt = ca.get_table("BINDTABLESAVE", feeno, "");
            Response.Write("<script>location.href='../ConstraintsAssessment/ListNew';</script>");
            return new EmptyResult();
            //return View("ListNew");
        }
        #endregion //刪除列表
        //test約束


        #region --列印--by.jarvis .2016/08/16 edit---ECK
        public ActionResult PrintList(string feeno)
        {
            ViewBag.RootDocument = GetSourceUrl();
            //DataTable dt_a = ca.get_table("BINDTABLE", feeno, "");

            DataTable dt_a1 = ca.get_table("BINDTABLE_ADD_REASON", feeno, "");
            DataView view = dt_a1.DefaultView;
            view.Sort = "BOUTDESC DESC";
            DataTable dt_a = view.ToTable();

            DataTable dt = ca.get_table("BINDTABLESAVE", feeno, "");
            ViewBag.dt_a = dt_a;
            ViewBag.dt = dt;
            ViewBag.FeeNo = feeno;

            PatientInfo pinfo = new PatientInfo();
            byte[] ByteCode = webService.GetPatientInfo(feeno);
            //病人資訊
            if (ByteCode != null)
                pinfo = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(ByteCode));
            ViewData["ptinfo"] = pinfo;
            return View();
        }
        #endregion


        #region other function---ECK
        /// <summary>要篩選的資料表</summary>  
        /// <param name="RowFilterStr">篩選條件(ex: actid= 'id123' AND datatype ='1')</param>
        /// <param name="SortStr">要排序的欄位名稱</param>  
        /// <returns>篩選過後的資料表</returns>
        public static DataTable FiltData(DataTable DT, string RowFilterStr, string SortStr = "")
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
        #endregion other function -end


        /// <summary>
        /// 取得語音歷程紀錄
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public ActionResult VoiceHistory(string startDate, string endDate, string source)
        {
            if (Session["PatInfo"] == null)
            {
                Response.Write("<script>alert('登入逾時');</script>");
                return new EmptyResult();
            }

            PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
            string feeNo = ptInfo.FeeNo;
            string userNo = userinfo.EmployeesNo;

            startDate = (startDate == null) ? DateTime.Now.ToString("yyyy/MM/dd") : startDate;
            endDate = (endDate == null) ? DateTime.Now.ToString("yyyy/MM/dd") : endDate;

            DataTable dt = ca.getVoiceHistory(
                feeNo,
                userNo,
                startDate,
                endDate
            );
            List<TALK_TABLE> voiceHistoryList = (List<TALK_TABLE>)dt.ToList<TALK_TABLE>();
            string voiceJsonStr = JsonConvert.SerializeObject(voiceHistoryList);
            ViewData["voiceHistoryList"] = voiceHistoryList;
            ViewBag.voiceJsonStr = voiceJsonStr;
            ViewBag.source = source;
            return View();

        }
    }
}
