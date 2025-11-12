using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NIS.Models;
using System.Data;
using NIS.Data;
using System.Data.OleDb;
using NIS.UtilTool;
using NIS.WebService;
using Newtonsoft.Json;
using static NIS.Models.IOManager;
/*註解測試*/

namespace NIS.Controllers
{
    public class IOManageController : BaseController
    {
        private CommData cd;    //常用資料Module
        private IOManager iom;
        private TubeManager tubem;
        private HISViewController his;
        private DBConnector link = new DBConnector();
        DateTime? nullDateTime = null;

        public IOManageController()
        {
            this.cd = new CommData();
            this.iom = new IOManager();
            this.tubem = new TubeManager();
            this.his = new HISViewController();
        }

        #region Index

        [HttpGet]
        public ActionResult Index()
        {
            if (Session["PatInfo"] != null)
            {
                ViewBag.dt_i = iom.sel_io_data_byClass(ptinfo.FeeNo, DateTime.Now.ToString("yyyy/MM/dd"), "1", "intaketype");
                ViewBag.dt_o = iom.sel_io_data_byClass(ptinfo.FeeNo, DateTime.Now.ToString("yyyy/MM/dd"), "1", "outputtype");
                ViewBag.date = DateTime.Now.ToString("yyyy/MM/dd");
                return View();
            }

            Response.Write("<script>alert('登入逾時');</script>");
            return new EmptyResult();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Index(FormCollection form)
        {
            if (Session["PatInfo"] != null)
            {
                string date = form["date"];
                ViewBag.dt_i = iom.sel_io_data_byClass(ptinfo.FeeNo, date, form["unit"], "intaketype");
                ViewBag.dt_o = iom.sel_io_data_byClass(ptinfo.FeeNo, date, form["unit"], "outputtype");
                ViewBag.seleunit = form["unit"];
                ViewBag.date = date;
                return View();
            }

            Response.Write("<script>alert('登入逾時');</script>");
            return new EmptyResult();
        }

        [HttpGet]
        public ActionResult Old_Index()
        {
            if (Session["PatInfo"] != null)
            {
                string feeno = ptinfo.FeeNo;
                DateTime now = DateTime.Now;
                DataTable dt_i_item = iom.sel_sys_params_kind("iotype", "intaketype");
                DataTable dt_o_item = iom.sel_sys_params_kind("iotype", "outputtype");
                DataTable dt_io_data = iom.sel_io_data("", feeno, "", now.ToString("yyyy/MM/dd 07:01:00"), now.AddDays(1).ToString("yyyy/MM/dd 07:00:59"), "1");
                DataTable dt = new DataTable();

                string[] column = { "DATE", "P_VALUE", "AMOUNT", "CLORIE", "REASON", "TYPE" };
                iom.set_dt_column(dt, column);
                iom.set_new_list(dt_io_data, dt_i_item, dt_o_item, dt, now, now);

                ViewBag.dt = dt;
                ViewBag.dt_i_item = dt_i_item;
                ViewBag.dt_o_item = dt_o_item;

                return View();
            }

            Response.Write("<script>alert('登入逾時');</script>");
            return new EmptyResult();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Old_Index(FormCollection form)
        {
            if (Session["PatInfo"] != null)
            {
                string feeno = ptinfo.FeeNo;

                DateTime start = Convert.ToDateTime(form["start_date"]);
                DateTime end = Convert.ToDateTime(form["end_date"]);
                TimeSpan Total = end.Subtract(start);
                ViewBag.key = Total.Days;
                ViewBag.start_date = start;
                ViewBag.end_date = end;

                DataTable dt_i_item = iom.sel_sys_params_kind("iotype", "intaketype");
                DataTable dt_o_item = iom.sel_sys_params_kind("iotype", "outputtype");
                DataTable dt_io_data = iom.sel_io_data("", feeno, "", start.ToString("yyyy/MM/dd 07:01:00"), end.AddDays(1).ToString("yyyy/MM/dd 07:00:59"), form["unit"]);
                DataTable dt = new DataTable();

                string[] column = { "DATE", "P_VALUE", "AMOUNT", "CLORIE", "REASON", "TYPE" };
                iom.set_dt_column(dt, column);
                iom.set_new_list(dt_io_data, dt_i_item, dt_o_item, dt, start, end);

                ViewBag.dt = dt;
                ViewBag.dt_i_item = dt_i_item;
                ViewBag.dt_o_item = dt_o_item;
                ViewBag.seleunit = form["unit"];

                return View();
            }

            Response.Write("<script>alert('登入逾時');</script>");
            return new EmptyResult();
        }

        #endregion

        #region Insert

        [HttpGet]
        public ActionResult Insert(string date, string IO)
        {
            if (Session["PatInfo"] != null)
            {
                string feeno = ptinfo.FeeNo;
                string userno = userinfo.EmployeesNo;

                ViewBag.dt_intake = iom.sel_sys_params_kind("iotype", "intaketype");
                ViewBag.dt_output = iom.sel_sys_params_kind("iotype", "outputtype");
                ViewBag.dt_kind_maintain = iom.sel_io_item("");
                DateTime now = DateTime.Now;
                if (DateTime.Now.Hour < 7)
                    ViewBag.dt_io_data = iom.sel_io_data("", feeno, "", now.AddDays(-1).ToString("yyyy/MM/dd 23:01:00"), now.ToString("yyyy/MM/dd HH:mm:ss"), "1");
                else if (DateTime.Now.Hour < 15)
                    ViewBag.dt_io_data = iom.sel_io_data("", feeno, "", now.ToString("yyyy/MM/dd 07:01:00"), now.ToString("yyyy/MM/dd HH:mm:ss"), "1");
                else if (DateTime.Now.Hour < 23)
                    ViewBag.dt_io_data = iom.sel_io_data("", feeno, "", now.ToString("yyyy/MM/dd 15:01:00"), now.ToString("yyyy/MM/dd HH:mm:ss"), "1");
                else
                    ViewBag.dt_io_data = iom.sel_io_data("", feeno, "", now.ToString("yyyy/MM/dd 23:01:00"), now.ToString("yyyy/MM/dd HH:mm:ss"), "1");

                ViewBag.dt_tubekind = tubem.sel_tube(feeno, "", "", "");
                if (date != null)
                    ViewBag.date = date;
                if (IO != null)
                    ViewBag.IO = IO;

                ViewBag.tube_name = new Func<string, string>(sel_item_name);
                ViewData["color_drainage"] = this.cd.getSelectItem("io", "outputcolor_Drainage", "");
                ViewData["taste_drainage"] = this.cd.getSelectItem("io", "outputtaste_Drainage", "");
                ViewData["nature_drainage"] = this.cd.getSelectItem("io", "outputnature_Drainage", "");

                return View();
            }

            Response.Write("<script>alert('登入逾時');</script>");
            return new EmptyResult();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Insert(FormCollection form)
        {
            if (Session["PatInfo"] != null)
            {
                string feeno = ptinfo.FeeNo;
                string userno = userinfo.EmployeesNo;
                string id = base.creatid("IO_DATA", userno, feeno, "0");

                //取得IO資料
                string date = form["creat_day"] + " " + form["creat_time"];
                string typeid = form["typeid"];
                string itemid = form["itemid"];
                string amount = (form["txt_amount"] == null) ? "" : form["txt_amount"];
                string calories = (form["txt_carloe"] == null) ? "" : form["txt_carloe"];
                string amount_unit = form["unit_select"];

                List<DBItem> insertDataList = new List<DBItem>();
                insertDataList.Add(new DBItem("IO_ROW", "IO_DATA_SEQUENCE.NEXTVAL", DBItem.DBDataType.Number));
                insertDataList.Add(new DBItem("IO_ID", id, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATTIME", date, DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("CREANO", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("TYPEID", typeid, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ITEMID", itemid, DBItem.DBDataType.String));

                if (amount != "" && calories != "")
                {
                    insertDataList.Add(new DBItem("AMOUNT", amount, DBItem.DBDataType.Number));
                    insertDataList.Add(new DBItem("CALORIES", calories, DBItem.DBDataType.Number));
                }
                else
                    insertDataList.Add(new DBItem("REASON", "Loss", DBItem.DBDataType.String));

                insertDataList.Add(new DBItem("AMOUNT_UNIT", amount_unit, DBItem.DBDataType.String));

                int erow = iom.DBExecInsert("IO_DATA", insertDataList);

                //儲存成功
                if (erow > 0)
                {
                    //新增IO性狀
                    insertDataList.Clear();
                    insertDataList.Add(new DBItem("FEATUREID", id, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("COLORID", form["color_drainage"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("COLOROTHER", (form["color_other"] == null) ? "" : form["color_other"].Trim(), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("NATUREID", form["nature_drainage"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("NATUREOTHER", (form["nature_other"] == null) ? "" : form["nature_other"].Trim(), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("TASTEID", form["taste_drainage"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("TASTEOTHER", (form["taste_other"] == null) ? "" : form["taste_other"].Trim(), DBItem.DBDataType.String));
                    iom.DBExecInsert("TUBE_FEATURE", insertDataList);

                    string io = (int.Parse(typeid) < 6) ? "I" : "O";
                    Response.Write("<script>window.location.href='Insert?date=" + date + "&IO=" + io + "';</script>");
                }
                else
                    Response.Write("<script>window.location.href='Insert';</script>");

                return new EmptyResult();
            }

            Response.Write("<script>alert('登入逾時');</script>");
            return new EmptyResult();
        }

        [HttpGet]
        public ActionResult New_Insert(string date, string IO)
        {
            if (Session["PatInfo"] != null)
            {
                string feeno = ptinfo.FeeNo;
                string userno = userinfo.EmployeesNo;
                DataTable dt_i_item = iom.sel_sys_params_kind("iotype", "intaketype");
                DataTable dt_o_item = iom.sel_sys_params_kind("iotype", "outputtype");
                DateTime now = DateTime.Now;

                if (DateTime.Now.Hour < 8)
                    ViewBag.dt_io_data = iom.sel_io_data("", feeno, "", now.ToString("yyyy/MM/dd 00:00:00"), now.ToString("yyyy/MM/dd HH:mm:ss"), "1");
                else if (DateTime.Now.Hour < 16)
                    ViewBag.dt_io_data = iom.sel_io_data("", feeno, "", now.ToString("yyyy/MM/dd 08:00:00"), now.ToString("yyyy/MM/dd HH:mm:ss"), "1");
                else if (DateTime.Now.Hour < 24)
                    ViewBag.dt_io_data = iom.sel_io_data("", feeno, "", now.ToString("yyyy/MM/dd 16:00:00"), now.ToString("yyyy/MM/dd HH:mm:ss"), "1");

                DataTable dt_io_data = iom.sel_io_data("", feeno, "", now.AddDays(-1).ToString("yyyy/MM/dd 23:59:59"), now.AddDays(1).ToString("yyyy/MM/dd 00:00:00"), "1");
                DataTable dt = new DataTable();
                string[] column = { "DATE", "P_VALUE", "AMOUNT", "CLORIE", "REASON", "TYPE" };
                iom.set_dt_column(dt, column);
                iom.set_new_list(dt_io_data, dt_i_item, dt_o_item, dt, now, now);

                ViewBag.dt_tubekind = tubem.sel_tube(feeno, "", "", "");
                ViewBag.dt_intake = dt_i_item;
                ViewBag.dt_output = dt_o_item;
                ViewBag.dt_kind_maintain = iom.sel_io_item("");
                ViewBag.dt = dt;

                if (date != null)
                    ViewBag.date = date;
                if (IO != null)
                    ViewBag.IO = IO;

                ViewBag.tube_name = new Func<string, string>(sel_item_name);

                return View();
            }

            Response.Write("<script>alert('登入逾時');</script>");
            return new EmptyResult();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult New_Insert(FormCollection form)
        {
            if (Session["PatInfo"] != null)
            {
                string feeno = ptinfo.FeeNo;
                string userno = userinfo.EmployeesNo;

                //取得IO資料
                string date = form["creat_day"] + " " + form["creat_time"];
                string typeid = form["typeid"];
                string itemid = form["itemid"];
                string amount = (form["txt_amount"] == null) ? "" : form["txt_amount"];
                string calories = (form["txt_carloe"] == null) ? "" : form["txt_carloe"];
                string amount_unit = form["unit_select"];

                List<DBItem> insertDataList = new List<DBItem>();
                insertDataList.Add(new DBItem("IO_ROW", "IO_DATA_SEQUENCE.NEXTVAL", DBItem.DBDataType.Number));
                insertDataList.Add(new DBItem("IO_ID", base.creatid("IO_DATA", userno, feeno, "0"), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATTIME", date, DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("CREANO", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("TYPEID", typeid, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ITEMID", itemid, DBItem.DBDataType.String));

                if (amount != "" && calories != "")
                {
                    insertDataList.Add(new DBItem("AMOUNT", amount, DBItem.DBDataType.Number));
                    insertDataList.Add(new DBItem("CALORIES", calories, DBItem.DBDataType.Number));
                }
                else
                    insertDataList.Add(new DBItem("REASON", "Loss", DBItem.DBDataType.String));

                insertDataList.Add(new DBItem("AMOUNT_UNIT", amount_unit, DBItem.DBDataType.String));

                int erow = iom.DBExecInsert("IO_DATA", insertDataList);

                //儲存成功
                if (erow > 0)
                {
                    string io = "";
                    if (int.Parse(typeid) < 6)
                        io = "I";
                    else
                        io = "O";
                    Response.Write("<script>window.location.href='Insert?date=" + date + "&IO=" + io + "';</script>");
                }
                else
                    Response.Write("<script>window.location.href='Insert';</script>");

                return new EmptyResult();
            }

            Response.Write("<script>alert('登入逾時');</script>");
            return new EmptyResult();
        }

        //新增護理處置
        public ActionResult Insert_Record(string Com, string I)
        {
            if (I.Trim() != "")
            {
                int erow = base.Insert_CareRecord(DateTime.Now.ToString("yyyy/MM/dd HH:mm"), base.creatid("IO", userinfo.EmployeesNo, ptinfo.FeeNo, "0"), "", Com.Trim(), "", "", I.Trim(), "", "IO");
                if (erow > 0)
                    Response.Write("新增成功");
                else
                    Response.Write("新增失敗");
            }
            return new EmptyResult();
        }

        #endregion

        #region Detail

        [HttpGet]
        public ActionResult Detail(string date, string day, string from_func = "", string itemid = "", string show_by = "",string feeno = "" ,string hisview = "")
        {
            string userno = string.Empty;
            if (hisview == "T"|| Session["PatInfo"] == null)
            {
                his.ControllerContext = ControllerContext;
                if (!string.IsNullOrEmpty(feeno))
                {
                    his.getSession(feeno);
                }
                if (ptinfo == null)
                {
                    ptinfo = (PatientInfo)Session["PatInfo"];
                }
            }
            if (userinfo != null)
            {
                userno = userinfo.EmployeesNo;
            }
            if (Session["PatInfo"] != null)
            {
                feeno = ptinfo.FeeNo;
                DateTime now = Convert.ToDateTime(day);
                DataTable dt = new DataTable();
                DateTime yesterday = now.AddDays(-1);
                string tube_typeID = "";
                if (from_func != "")
                {
                    tube_typeID = (from_func.ToUpper() == "IO_TUBE") ? "9" : "";
                }

                if (show_by != "tube") // show_by:  class|tube
                    itemid = "";



                if (date == "N")
                    dt = iom.sel_io_data("", feeno, "", yesterday.ToString("yyyy/MM/dd 23:01:00"), now.ToString("yyyy/MM/dd 07:00:59"), "", tube_typeID, itemid);
                else if (date == "D")
                    dt = iom.sel_io_data("", feeno, "", now.ToString("yyyy/MM/dd 07:01:00"), now.ToString("yyyy/MM/dd 15:00:59"), "", tube_typeID, itemid);
                else if (date == "E")
                    dt = iom.sel_io_data("", feeno, "", now.ToString("yyyy/MM/dd 15:01:00"), now.ToString("yyyy/MM/dd 23:00:59"), "", tube_typeID, itemid);
                else
                    dt = iom.sel_io_data("", feeno, "", now.ToString("yyyy/MM/dd 07:01:00"), now.AddDays(1).ToString("yyyy/MM/dd 07:00:59"), "", tube_typeID, itemid);

                ViewBag.dt_io_data = set_dt(dt);
                ViewBag.date = date;
                ViewBag.day = day;
                ViewBag.userno = userno;
                ViewBag.from_func = from_func;
                ViewBag.show_by = show_by;
                ViewBag.itemid = itemid;
                ViewBag.hisview = hisview;
                ViewBag.tube_name = new Func<string, string>(sel_item_name);
                return View();

            }
            Response.Write("<script>alert('登入逾時');window.close();</script>");
            return new EmptyResult();
        }

        [HttpGet]
        public ActionResult IO_Maintain(string row, string date, string day, string itemid = "", string from_func = "", string show_by = "")
        {
            if (Session["PatInfo"] != null)
            {
                string tube_typeID = "";
                if (from_func != null)
                {
                    tube_typeID = (from_func.ToUpper() == "IO_TUBE") ? "9" : "";
                }
                ViewBag.dt_intake = iom.sel_sys_params_kind("iotype", "intaketype");
                ViewBag.dt_output = iom.sel_sys_params_kind("iotype", "outputtype");
                ViewBag.dt_kind_maintain = iom.sel_io_item("");
                ViewBag.dt = iom.sel_io_data("", "", row, "", "", "", tube_typeID, itemid);
                ViewBag.date = date;
                ViewBag.day = day;
                ViewBag.from_func = from_func;
                ViewBag.show_by = show_by;
                string feeno = ptinfo.FeeNo;
                ViewBag.dt_tubekind = tubem.sel_tube(ptinfo.FeeNo, "", "", "");
                ViewData["color_drainage"] = this.cd.getSelectItem("io", "outputcolor_Drainage", "");
                ViewData["taste_drainage"] = this.cd.getSelectItem("io", "outputtaste_Drainage", "");
                ViewData["nature_drainage"] = this.cd.getSelectItem("io", "outputnature_Drainage", "");
                DataTable IOdataDt = this.iom.DBExecSQL("SELECT * FROM IO_DATA WHERE 0=0 AND IO_ROW='" + row + "' AND FEENO='" + feeno + "' AND DELETED IS NULL");
                if (IOdataDt != null && IOdataDt.Rows.Count > 0)
                {
                    string IO_NUM = IOdataDt.Rows[0]["IO_ID"].ToString();
                    DataTable CareDt = this.iom.DBExecSQL("SELECT * FROM CARERECORD_DATA WHERE 0=0 AND FEENO='" + feeno + "' AND DELETED IS NULL AND CARERECORD_ID='" + IO_NUM + "'");
                    ViewBag.CareDt = CareDt;
                }
                return View();
            }

            Response.Write("<script>alert('登入逾時');window.close();</script>");
            return new EmptyResult();
        }

        //更新
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult IO_Maintain_Upd(FormCollection form)
        {
            if (Session["PatInfo"] != null)
            {
                string userno = userinfo.EmployeesNo;
                string feeno = ptinfo.FeeNo;
                string[] unitStr = { "mL", "g", "mg", "分鐘", "次" };
                string type_tube_name = form["type_tube_name"];
                string id = form["io_id"];
                string date = form["creat_day"] + " " + form["creat_time"];
                string amount = (form["txt_amount"] == null) ? "" : form["txt_amount"];
                string calories = (form["txt_carloe"] == null) ? "" : form["txt_carloe"];
                string amount_unit = form["unit_select"];
                string now = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                //IO引流管統計
                string from_func = form["from_func"];
                string itemid = form["itemid"];
                string itemid2 = form["itemid2"];
                string show_by = form["show_by"];
                string reason = "";  //使用parameter Update 時, 變數不得為null
                if (itemid == "" && itemid2 != "")
                {
                    form["itemid"] = itemid2;
                    itemid = itemid2;
                }


                List<DBItem> insertDataList = new List<DBItem>();
                insertDataList.Add(new DBItem("CREATTIME", date, DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDTIME", now, DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("TYPEID", form["typeid"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ITEMID", form["itemid"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EXPLANATION_ITEM", form["txt_explanation_item"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("REMARK", form["txta_remark"], DBItem.DBDataType.String));
                if (amount != "" && calories != "")
                {
                    insertDataList.Add(new DBItem("AMOUNT", amount, DBItem.DBDataType.Number));
                    insertDataList.Add(new DBItem("CALORIES", calories, DBItem.DBDataType.Number));
                    insertDataList.Add(new DBItem("REASON", reason, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("AMOUNT", null, DBItem.DBDataType.Number));
                    insertDataList.Add(new DBItem("CALORIES", null, DBItem.DBDataType.Number));
                    insertDataList.Add(new DBItem("REASON", "Loss", DBItem.DBDataType.String));
                }
                insertDataList.Add(new DBItem("AMOUNT_UNIT", form["unit_select"], DBItem.DBDataType.String));
                int erow = iom.DBExecUpdate("IO_DATA", insertDataList, "IO_ROW = '" + form["row"] + "' AND FEENO = '" + feeno + "' ");
                #region 帶入護理記錄
                if (form["hid_care_record"] == "1")
                {//勾選帶入護理記錄
                    //判斷是否為修改
                    if (form["hid_carerecordhas"] != "1")
                    {//進入為新增
                        if (Convert.ToInt32(form["typeid"]) >= 1 && Convert.ToInt32(form["typeid"]) <= 5 || Convert.ToInt32(form["typeid"]) == 11)
                        {
                            //是輸入--【類別】【項目】【細項說明】【量】ml，【備註】。
                            string SqlStr = " SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'iotype' AND P_GROUP = 'intaketype' AND P_VALUE='" + form["typeid"] + "'";
                            DataTable DTTYPE = iom.DBExecSQL(SqlStr);
                            string Str = DTTYPE.Rows[0]["P_NAME"] + " ";
                            if (form["typeid"] != "3")
                            {
                                Str += type_tube_name + " ";
                            }
                            else
                            {
                                Str += type_tube_name + " ";
                            }
                            Str += form["txt_explanation_item"].Trim() + " ";
                            if (amount != "")
                            {
                                Str += amount + unitStr[Convert.ToInt32(amount_unit) - 1] + ", ";
                            }
                            else
                            {
                                Str += "Loss, ";
                            }
                            if (form["txta_remark"].Trim() != "")
                            {
                                Str += form["txta_remark"].Trim() + "。";
                            }
                            else
                            {
                                Str = Str.Trim();
                                if ((Str.Substring(Str.Length - 1, 1) == ",") || (Str.Substring(Str.Length - 1, 1) == "，"))
                                    Str = Str.Substring(0, Str.Length - 1);
                                Str += "。";
                            }
                            erow += base.Insert_CareRecord(date, id, "Intake", "", "", Str, "", "", "IO_DATA");// form["txtarea_record"].ToString().Trim()
                        }
                        else if (Convert.ToInt32(form["typeid"]) >= 6 && Convert.ToInt32(form["typeid"]) <= 10 || Convert.ToInt32(form["typeid"]) == 12 || Convert.ToInt32(form["typeid"]) == 13)
                        {
                            //是輸出----【類別】【項目】【細項說明】【量】ml【顏色】【性狀】【氣味】，【備註】。
                            string SqlStr = " SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'iotype' AND P_GROUP = 'outputtype' AND P_VALUE='" + form["typeid"] + "'";
                            DataTable DTTYPE = iom.DBExecSQL(SqlStr);
                            string Str = DTTYPE.Rows[0]["P_NAME"] + " ";
                            switch (form["typeid"])
                            {
                                case "7":
                                    break;
                                default:
                                    Str += type_tube_name + " ";
                                    break;
                            }
                            Str += form["txt_explanation_item"].Trim() + " ";
                            if (amount != "")
                            {
                                Str += amount + unitStr[Convert.ToInt32(amount_unit) - 1] + ", ";
                            }
                            else
                            {
                                Str += "Loss, ";
                            }
                            DataTable DTcolor = iom.DBExecSQL("SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'io' AND P_GROUP = 'outputcolor_Drainage' AND P_VALUE='" + form["color_drainage"] + "'");
                            switch (form["color_drainage"])
                            {
                                case "-1":
                                    break;
                                case "99":
                                    Str += form["color_other"].Trim() + " ";
                                    break;
                                default:
                                    Str += DTcolor.Rows[0][0].ToString() + " ";
                                    break;
                            }
                            DataTable DTnature = iom.DBExecSQL("SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'io' AND P_GROUP = 'outputnature_Drainage' AND P_VALUE='" + form["nature_drainage"] + "'");
                            switch (form["nature_drainage"])
                            {
                                case "-1":
                                    break;
                                case "99":
                                    Str += form["nature_other"].Trim() + " ";
                                    break;
                                default:
                                    Str += DTnature.Rows[0][0].ToString() + " ";
                                    break;
                            }
                            DataTable DTtaste = iom.DBExecSQL("SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'io' AND P_GROUP = 'outputtaste_Drainage' AND P_VALUE='" + form["taste_drainage"] + "'");
                            switch (form["taste_drainage"])
                            {
                                case "-1":
                                    break;
                                case "99":
                                    Str += form["taste_other"].Trim() + " ";
                                    break;
                                default:
                                    Str += DTtaste.Rows[0][0].ToString() + " ";
                                    break;
                            }
                            if (form["txta_remark"].Trim() != "")
                            {
                                Str += form["txta_remark"].Trim() + "。";
                            }
                            else
                            {
                                Str = Str.Trim();
                                if ((Str.Substring(Str.Length - 1, 1) == ",") || (Str.Substring(Str.Length - 1, 1) == "，"))
                                    Str = Str.Substring(0, Str.Length - 1);
                                Str += "。";
                            }
                            erow += base.Insert_CareRecord(date, id, "Output", "", "", Str, "", "", "IO_DATA");//form["txtarea_record"].ToString().Trim()
                        }
                    }
                    else
                    {//為修改
                        if (Convert.ToInt32(form["typeid"]) >= 1 && Convert.ToInt32(form["typeid"]) <= 5 || Convert.ToInt32(form["typeid"]) == 11)
                        {
                            //是輸入--【類別】【項目】【細項說明】【量】ml，【備註】。
                            string SqlStr = " SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'iotype' AND P_GROUP = 'intaketype' AND P_VALUE='" + form["typeid"] + "'";
                            DataTable DTTYPE = iom.DBExecSQL(SqlStr);
                            string Str = DTTYPE.Rows[0]["P_NAME"] + " ";
                            if (form["typeid"] != "3")
                            {
                                Str += type_tube_name + " ";
                            }
                            else
                            {
                                Str += type_tube_name + " ";
                            }
                            Str += form["txt_explanation_item"].Trim() + " ";
                            if (amount != "")
                            {
                                Str += amount + unitStr[Convert.ToInt32(amount_unit) - 1] + ", ";
                            }
                            else
                            {
                                Str += "Loss, ";
                            }
                            if (form["txta_remark"].Trim() != "")
                            {
                                Str += form["txta_remark"].Trim() + "。";
                            }
                            else
                            {
                                Str = Str.Trim();
                                if ((Str.Substring(Str.Length - 1, 1) == ",") || (Str.Substring(Str.Length - 1, 1) == "，"))
                                    Str = Str.Substring(0, Str.Length - 1);
                                Str += "。";
                            }
                            erow += base.Upd_CareRecord(date, id, "Intake", "", "", Str, "", "", "IO_DATA");
                        }
                        else if (Convert.ToInt32(form["typeid"]) >= 6 && Convert.ToInt32(form["typeid"]) <= 10 || Convert.ToInt32(form["typeid"]) == 12 || Convert.ToInt32(form["typeid"]) == 13)
                        {
                            //是輸出----【類別】【項目】【細項說明】【量】ml【顏色】【性狀】【氣味】，【備註】。
                            string SqlStr = " SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'iotype' AND P_GROUP = 'outputtype' AND P_VALUE='" + form["typeid"] + "'";
                            DataTable DTTYPE = iom.DBExecSQL(SqlStr);
                            string Str = DTTYPE.Rows[0]["P_NAME"] + " ";
                            switch (form["typeid"])
                            {
                                case "7":
                                    break;
                                default:
                                    Str += type_tube_name + " ";
                                    break;
                            }
                            Str += form["txt_explanation_item"].Trim() + " ";
                            if (amount != "")
                            {
                                Str += amount + unitStr[Convert.ToInt32(amount_unit) - 1] + ", ";
                            }
                            else
                            {
                                Str += "Loss, ";
                            }
                            DataTable DTcolor = iom.DBExecSQL("SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'io' AND P_GROUP = 'outputcolor_Drainage' AND P_VALUE='" + form["color_drainage"] + "'");
                            switch (form["color_drainage"])
                            {
                                case "-1":
                                    break;
                                case "99":
                                    Str += form["color_other"].Trim() + " ";
                                    break;
                                default:
                                    Str += DTcolor.Rows[0][0].ToString() + " ";
                                    break;
                            }
                            DataTable DTnature = iom.DBExecSQL("SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'io' AND P_GROUP = 'outputnature_Drainage' AND P_VALUE='" + form["nature_drainage"] + "'");
                            switch (form["nature_drainage"])
                            {
                                case "-1":
                                    break;
                                case "99":
                                    Str += form["nature_other"].Trim() + " ";
                                    break;
                                default:
                                    Str += DTnature.Rows[0][0].ToString() + " ";
                                    break;
                            }
                            DataTable DTtaste = iom.DBExecSQL("SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'io' AND P_GROUP = 'outputtaste_Drainage' AND P_VALUE='" + form["taste_drainage"] + "'");
                            switch (form["taste_drainage"])
                            {
                                case "-1":
                                    break;
                                case "99":
                                    Str += form["taste_other"].Trim() + " ";
                                    break;
                                default:
                                    Str += DTtaste.Rows[0][0].ToString() + " ";
                                    break;
                            }
                            if (form["txta_remark"].Trim() != "")
                            {
                                Str += form["txta_remark"].Trim() + "。";
                            }
                            else
                            {
                                Str = Str.Trim();
                                if ((Str.Substring(Str.Length - 1, 1) == ",") || (Str.Substring(Str.Length - 1, 1) == "，"))
                                    Str = Str.Substring(0, Str.Length - 1);

                                Str += "。";
                            }
                            erow += base.Upd_CareRecord(date, id, "Output", "", "", Str, "", "", "IO_DATA");
                        }
                    }
                }
                else
                {//是否刪除
                    if (form["hid_carerecordhas"] == "1")
                    {  //真刪
                        //erow += link.DBExecDelete("CARERECORD_DATA", "CARERECORD_ID ='" + id + "' AND SELF ='IO_DATA'");
                        erow += base.Del_CareRecord(id, "IO_DATA" );

                        if (erow > 0)
                        {
                            //將紀錄回寫至 EMR Temp Table                          
                            try
                            {
                                //int result = del_emr(id + "IO_DATA", userinfo.EmployeesNo);
                            }
                            catch { }
                        }

                    }
                }
                #endregion

                if (erow > 0)
                { //新增IO性狀
                    insertDataList.Clear();
                    insertDataList.Add(new DBItem("COLORID", form["color_drainage"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("COLOROTHER", (form["color_other"] == null) ? "" : form["color_other"].Trim(), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("NATUREID", form["nature_drainage"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("NATUREOTHER", (form["nature_other"] == null) ? "" : form["nature_other"].Trim(), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("TASTEID", form["taste_drainage"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("TASTEOTHER", (form["taste_other"] == null) ? "" : form["taste_other"].Trim(), DBItem.DBDataType.String));
                    iom.DBExecUpdate("TUBE_FEATURE", insertDataList, "FEATUREID = '" + form["io_id"] + "'");
                    Response.Write("<script>alert('更新成功!');window.location.href='Detail?show_by=" + show_by + "&itemid=" + itemid + "&from_func=" + form["from_func"] + "&date=" + form["date"] + "&day=" + form["day"] + "';</script>");//window.opener.location.reload();
                }
                else
                    Response.Write("<script>alert('更新失敗!');window.location.href='Detail?show_by=" + show_by + "&itemid=" + itemid + "&from_func=" + form["from_func"] + "&date=" + form["date"] + "&day=" + form["day"] + "';</script>");
            }
            else
                Response.Write("<script>alert('登入逾時');window.close();</script>");

            return new EmptyResult();
        }

        //刪除
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult IO_Maintain_Delete(FormCollection form)
        {
            if (Session["PatInfo"] != null)
            {
                string userno = userinfo.EmployeesNo;
                string feeno = ptinfo.FeeNo;
                DataTable IOdataDt = this.iom.DBExecSQL("SELECT * FROM IO_DATA WHERE 0=0 AND IO_ROW='" + form["row"] + "' AND FEENO='" + feeno + "' AND DELETED IS NULL");
                List<DBItem> insertDataList = new List<DBItem>();
                insertDataList.Add(new DBItem("DELETED", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                int erow = iom.DBExecUpdate("IO_DATA", insertDataList, "IO_ROW = '" + form["row"] + "' AND FEENO = '" + feeno + "' ");
                if (IOdataDt != null && IOdataDt.Rows.Count > 0)
                {
                    string IO_NUM = IOdataDt.Rows[0]["IO_ID"].ToString();
                    DataTable CareDt = this.iom.DBExecSQL("SELECT * FROM CARERECORD_DATA WHERE 0=0 AND FEENO='" + feeno + "' AND DELETED IS NULL AND CARERECORD_ID='" + IO_NUM + "'");
                    ViewBag.CareDt = CareDt;
                    if (CareDt != null && CareDt.Rows.Count > 0)
                    {
                        erow += base.Del_CareRecord(IO_NUM, "IO_DATA");
                    }
                }
                insertDataList = null;
                if (erow > 0)
                    Response.Write("<script>alert('刪除成功!');window.location.href='Detail?date=" + form["date"] + "&day=" + form["day"] + "';</script>");//window.opener.location.reload();
                else
                    Response.Write("<script>alert('刪除失敗!');window.location.href='Detail?date=" + form["date"] + "&day=" + form["day"] + "';</script>");
            }
            else
                Response.Write("<script>alert('登入逾時');window.close();</script>");

            return new EmptyResult();
        }

        #endregion

        #region IO維護

        //IO維護首頁
        [HttpGet]
        public ActionResult IO_Function_Maintain(string mode)
        {
            string userno = userinfo.EmployeesNo;
            ViewData["intake_tube"] = this.cd.getSelectItem("iotype", "intaketype", "");
            ViewData["output_tube"] = this.cd.getSelectItem("iotype", "outputtype", "");
            ViewBag.mode = mode;
            DataTable dt = iom.sel_io_item("");
            ViewBag.dt = dt;

            return View();
        }

        //新增
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult IO_Function_Maintain_Insert(FormCollection form)
        {
            string userno = userinfo.EmployeesNo;
            string[] typeid = form["io_typeid[]"].Split(',');
            string[] name = form["txt_tube[]"].Split(',');
            string[] claries = form["txt_calories[]"].Split(',');
            int erow = 0;

            for (int i = 0; i < typeid.Length; i++)
            {
                List<DBItem> insertDataList = new List<DBItem>();
                insertDataList.Add(new DBItem("ITEMID", base.creatid("IO_ITEM", userno, "", i.ToString()), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREANO", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("TYPEID", typeid[i], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("NAME", name[i], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CALORIES", (claries[i].ToString() != "") ? claries[i] : "0", DBItem.DBDataType.Number));
                insertDataList.Add(new DBItem("SEQUENCE", "99", DBItem.DBDataType.Number));

                erow += iom.DBExecInsert("IO_ITEM", insertDataList);
            }

            if (erow > 0)
                Response.Write("<script>alert('儲存成功!');window.location.href='IO_Function_Maintain?mode=insert';</script>");
            else
                Response.Write("<script>alert('儲存失敗!');window.location.href='IO_Function_Maintain?mode=insert';</script>");

            return new EmptyResult();
        }

        //更新
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult IO_Function_Maintain_Update(FormCollection form)
        {
            string[] id = form["id[]"].Split(',');
            string[] typeid = form["io_typeid[]"].Split(',');
            string[] name = form["txt_tube[]"].Split(',');
            string[] clarie = form["txt_calories[]"].Split(',');
            string[] squence = form["txt_squence[]"].Split(',');
            int erow = 0;

            for (int i = 0; i < id.Length; i++)
            {
                List<DBItem> insertDataList = new List<DBItem>();
                insertDataList.Add(new DBItem("TYPEID", typeid[i], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("NAME", name[i], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CALORIES", (clarie[i].ToString() != "") ? clarie[i] : "0", DBItem.DBDataType.Number));
                insertDataList.Add(new DBItem("SEQUENCE", (!string.IsNullOrEmpty(squence[i])) ? squence[i] : "99", DBItem.DBDataType.Number));
                erow += iom.DBExecUpdate("IO_ITEM", insertDataList, " ITEMID = '" + id[i] + "' ");
            }

            if (erow > 0)
                Response.Write("<script>alert('儲存成功!');window.location.href='IO_Function_Maintain?mode=update';</script>");
            else
                Response.Write("<script>alert('儲存失敗!');window.location.href='IO_Function_Maintain?mode=update';</script>");

            return new EmptyResult();
        }

        //刪除
        [HttpPost]
        public ActionResult IO_Function_Maintain_Delete(string kindid)
        {
            //刪除tube
            string where = " ITEMID = '" + kindid + "' AND CREANO <> 'sys' ";
            int erow = iom.DBExecDelete("IO_ITEM", where);

            if (erow > 0)
                Response.Write("<script>alert('儲存成功!');window.location.href='IO_Function_Maintain?mode=update';</script>");
            else
                Response.Write("<script>alert('儲存失敗!');window.location.href='IO_Function_Maintain?mode=update';</script>");

            return new EmptyResult();
        }

        #endregion

        public ActionResult IO(string id)
        {
            if (Session["PatInfo"] != null)
            {
                string feeno = ptinfo.FeeNo;
                DataTable dt_io_data = iom.sel_io_data_byGroup(feeno);
                ViewBag.dt = dt_io_data;
                ViewBag.id = id;
                return View();
            }

            Response.Write("<script>alert('登入逾時');window.close();</script>");
            return new EmptyResult();
        }

        #region other_function

        private DataTable set_dt(DataTable dt)
        {
            if (dt.Rows.Count > 0)
            {
                dt.Columns.Add("username");

                string userno = dt.Rows[0]["CREANO"].ToString();
                byte[] listByteCode = webService.UserName(userno);
                string listJsonArray = CompressTool.DecompressString(listByteCode);
                UserInfo user_name = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                foreach (DataRow r in dt.Rows)
                {
                    if (userno != r["CREANO"].ToString())
                    {
                        userno = r["CREANO"].ToString();
                        listByteCode = webService.UserName(userno);
                        listJsonArray = CompressTool.DecompressString(listByteCode);
                        user_name = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                    }
                    r["username"] = user_name.EmployeesName;
                }
            }
            return dt;
        }

        public string sel_item_name(string item_id)
        {
            if (item_id != "")
            {
                DataTable dt = tubem.sel_tube("", "", item_id, "", "N");
                string content = "";
                if (dt.Rows.Count > 0)
                {
                    content = dt.Rows[0]["TYPE_NAME"].ToString() + dt.Rows[0]["POSITION"].ToString() + dt.Rows[0]["LOCATION_NAME"].ToString();
                    if (dt.Rows[0]["NUMBERID"].ToString() != "99")
                        content += "#" + dt.Rows[0]["NUBER_NAME"].ToString();
                    else
                        content += "#" + dt.Rows[0]["NUMBEROTHER"].ToString();
                }

                return content;
            }
            else
                return "";
        }

        #endregion

        public void insert_IVF(string itemid,string userno, string typeid,string name, string sequence)
        {
            List<DBItem> insertIVF = new List<DBItem>();
            insertIVF.Add(new DBItem("ITEMID", itemid, DBItem.DBDataType.String));
            insertIVF.Add(new DBItem("CREANO", userno, DBItem.DBDataType.String));
            insertIVF.Add(new DBItem("TYPEID", typeid, DBItem.DBDataType.Number));
            insertIVF.Add(new DBItem("NAME", name, DBItem.DBDataType.String));
            insertIVF.Add(new DBItem("CALORIES", "0", DBItem.DBDataType.Number));
            insertIVF.Add(new DBItem("SEQUENCE", sequence, DBItem.DBDataType.String));
            
            int erow = iom.DBExecInsert("IO_ITEM", insertIVF);
        }

        #region --IO_Insert_新版 2016/03/10_eck--
        [HttpGet]
        public ActionResult IO_Insert_List(string date, string IO)
        {
            List<DBItem> insertDataList = new List<DBItem>();
            List<IOItem> IVFLlist = new List<IOItem>();
            //塞入dataColumns   
            DataTable IO_dt = new DataTable();
            IO_dt.Columns.Add("ITEMID");
            IO_dt.Columns.Add("CREANO");
            IO_dt.Columns.Add("TYPEID");
            IO_dt.Columns.Add("NAME");
            IO_dt.Columns.Add("CALORIES");
            IO_dt.Columns.Add("SEQUENCE");
            IO_dt.Columns.Add("CHINESE_NAME");
            DataRow dr = IO_dt.NewRow();

            if (Session["PatInfo"] != null)
            {
                UserInfo ui = (UserInfo) Session["UserInfo"];
                string feeno = ptinfo.FeeNo;
                ViewBag.dt_intake = iom.sel_sys_params_kind("iotype", "intaketype");
                ViewBag.dt_output = iom.sel_sys_params_kind("iotype", "outputtype");
                Obstetrics obs_m = new Obstetrics();
                ViewBag.milk = obs_m.sel_nb_formula_choose();

                //點滴改從WebService取值
                int cnt = 0;
                //iom.DELETE_IO_IVF();  //刪掉維護作業的點滴類
                IO_dt = iom.sel_io_item("","1");  // 取維護作業中的 [非點滴類] IO項目(1)

                //點滴改從 webService.GetIVFList  取值, 其餘不變![點滴類]  IO項目(2)              
                byte[] TempByte = webService.GetIVFList();
                if (TempByte != null)
                    IVFLlist = JsonConvert.DeserializeObject<List<IOItem>>(CompressTool.DecompressString(TempByte));

                string  DB_itemid ="", tmp_Name="";
                string save_time = "";

                //IO項目(1) + IO項目(2)  塞入 dataTable
                foreach (IOItem IVF in IVFLlist.OrderBy(x => x.SORT))
                {
                    cnt += 1;
                    tmp_Name = IVF.NAME.Trim().Replace("'", "＇");
                    DB_itemid = iom.sel_io_item(1, tmp_Name);
                    save_time = DateTime.Now.ToString("yyyyMMddHHmmssff");
                    if (DB_itemid == "")
                    {
                        DB_itemid = "IO_ITEM_13278__" + save_time + "_" + cnt.ToString();
                        insert_IVF(DB_itemid, ui.EmployeesNo, "1", tmp_Name, IVF.SORT.Trim());
                    }
                        dr = IO_dt.NewRow();
                        dr["ITEMID"] = DB_itemid;
                        dr["CREANO"] = "SYS";
                        dr["TYPEID"] = "1";
                        dr["NAME"] = tmp_Name;
                        dr["CALORIES"] = "0";
                        dr["SEQUENCE"] = (IVF.SORT == null) ? "99" : IVF.SORT;
                        IO_dt.Rows.Add(dr);                   
                }
                // IVF清單固定要有 其他藥物 與 其他點滴(3)
                //  IO項目(1) + WS(IO項目(2)  + 特殊點滴(3) 塞入 dataTable
                IO_dt = iom.insert_SPECIAL_IVF(IO_dt);                

                ViewBag.dt_kind_maintain = IO_dt;
                DateTime now = DateTime.Now;
                //-----2016/06/23 Vanda 管路時間算法
                //大夜：2301-700
                //白班：0701-1500
                //小夜：1501-2300
                //今日大夜與明日大夜之間：2301-2359
                if (Convert.ToInt32(DateTime.Now.ToString("HHmm")) <= 700)
                {
                    DataTable NewDT = iom.sel_io_data("", feeno, "", now.AddDays(-1).ToString("yyyy/MM/dd 23:01:00"), now.ToString("yyyy/MM/dd HH:mm:ss"), "");// "1"
                    ViewBag.dt_io_data = NewDT;
                }
                else if (Convert.ToInt32(DateTime.Now.ToString("HHmm")) <= 1500)
                    ViewBag.dt_io_data = iom.sel_io_data("", feeno, "", now.ToString("yyyy/MM/dd 07:01:00"), now.ToString("yyyy/MM/dd HH:mm:ss"), "");
                else if (Convert.ToInt32(DateTime.Now.ToString("HHmm")) <= 2300)
                    ViewBag.dt_io_data = iom.sel_io_data("", feeno, "", now.ToString("yyyy/MM/dd 15:01:00"), now.ToString("yyyy/MM/dd HH:mm:ss"), "");
                else
                    ViewBag.dt_io_data = iom.sel_io_data("", feeno, "", now.ToString("yyyy/MM/dd 23:01:00"), now.ToString("yyyy/MM/dd HH:mm:ss"), "");

                ViewBag.dt_tubekind = tubem.sel_tube(feeno, "", "", "");
                if (date != null)
                    ViewBag.date = date;
                if (IO != null)
                    ViewBag.IO = IO;
                ViewBag.tube_name = new Func<string, string>(sel_item_name);
                ViewData["color_drainage"] = this.cd.getSelectItem("io", "outputcolor_Drainage", "");//
                ViewData["taste_drainage"] = this.cd.getSelectItem("io", "outputtaste_Drainage", "");//
                ViewData["nature_drainage"] = this.cd.getSelectItem("io", "outputnature_Drainage", "");//
                return View();//
            }

            Response.Write("<script>alert('登入逾時');</script>");
            return new EmptyResult();
        }

        #region --新增IO--
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult IO_Insert(FormCollection form)
        {
            if (Session["PatInfo"] != null)
            {
                string PcheckboxValue = form["txt_checkbox_temp"];
                //string PtxtareaRecord = form["txtarea_record"];
                string feeno = ptinfo.FeeNo;
                string userno = userinfo.EmployeesNo;
                string id = base.creatid("IO_DATA", userno, feeno, "0");
                string[] unitStr = { "mL", "g", "mg", "分鐘", "次" };
                //取得IO資料
                string date = form["creat_day"] + " " + form["creat_time"];
                string typeid = form["typeid"];
                string itemid = form["itemid"];
                string amount = (form["txt_amount"] == null) ? "" : form["txt_amount"];
                string B_milk_time = (form["B_milk_time"] == null) ? "" : form["B_milk_time"];
                string Times = (form["times"] == null) ? "" : form["times"];
                //string calories = (form["txt_carloe"] == null) ? "" : form["txt_carloe"];
                string amount_unit = form["unit_select"];
                if (typeid =="11" || typeid=="12"|| typeid == "13")
                {
                    switch (typeid)
                    {
                        case "11":
                            amount_unit = "4";break;
                        case "12":
                        case "13":
                            amount_unit = "5"; break;
                        default:
                            amount_unit = form["unit_select"];
                            break;
                    }
                }
                string explanation_item = form["txt_explanation_item"];
                string remark = form["txta_remark"];
                string type_tube_name = form["type_tube_name"];
                List<DBItem> insertDataList = new List<DBItem>();
                insertDataList.Add(new DBItem("IO_ROW", "IO_DATA_SEQUENCE.NEXTVAL", DBItem.DBDataType.Number));
                insertDataList.Add(new DBItem("IO_ID", id, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("TYPEID", typeid, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ITEMID", itemid, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EXPLANATION_ITEM", explanation_item, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("REMARK", remark, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREANO", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREANAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATTIME", date, DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDTIME", date, DBItem.DBDataType.DataTime));
                if (B_milk_time != "")//有含乳時間
                {
                    insertDataList.Add(new DBItem("B_MILK_TIME", B_milk_time, DBItem.DBDataType.Number));
                }
                if (Times != "")//有次數 (排尿排便用)
                {
                    insertDataList.Add(new DBItem("TIMES", Times, DBItem.DBDataType.Number));
                }
                if (amount != "")// && calories != ""
                {
                    insertDataList.Add(new DBItem("AMOUNT", amount, DBItem.DBDataType.Number));
                    //insertDataList.Add(new DBItem("CALORIES", calories, DBItem.DBDataType.Number));
                }
                else
                {
                    insertDataList.Add(new DBItem("REASON", "Loss", DBItem.DBDataType.String));
                }
                insertDataList.Add(new DBItem("AMOUNT_UNIT", amount_unit, DBItem.DBDataType.String));
                int erow = iom.DBExecInsert("IO_DATA", insertDataList);
                #region --帶入護理記錄--
                if (PcheckboxValue == "1")//表示有選取帶入護理紀錄的checkbox
                {
                    if (Convert.ToInt32(typeid) >= 1 && Convert.ToInt32(typeid) <= 5 || Convert.ToInt32(typeid) == 11)
                    {
                        //是輸入--【類別】【項目】【細項說明】【量】mL，【備註】。
                        string SqlStr = " SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'iotype' AND P_GROUP = 'intaketype' AND P_VALUE='" + typeid + "'";
                        DataTable DTTYPE = iom.DBExecSQL(SqlStr);
                        string Str = DTTYPE.Rows[0]["P_NAME"] + " ";
                        Str += type_tube_name + " ";

                        Str += explanation_item.Trim() + " ";

                        //if (B_milk_time != "")
                        //{
                        //    Str +="含乳" + B_milk_time + "分鐘, ";
                        //}
                        if (amount != "")
                        {
                            Str += amount + unitStr[Convert.ToInt32(amount_unit) - 1] + ", ";
                        }
                        else
                        {
                            Str += "Loss, ";
                        }
                        if (remark.Trim() != "")
                        {
                            Str += remark.Trim() + "。";
                        }
                        else
                        {
                            Str = Str.Trim();
                            if ((Str.Substring(Str.Length - 1, 1) == ",") || (Str.Substring(Str.Length - 1, 1) == "，"))
                                Str = Str.Substring(0, Str.Length - 1);
                            Str += "。";
                        }
                        erow += base.Insert_CareRecord(date, id, "Intake", "", "", Str, "", "", "IO_DATA");// form["txtarea_record"].ToString().Trim()
                    }
                    else if (Convert.ToInt32(typeid) >= 6 && Convert.ToInt32(typeid) <= 10 || Convert.ToInt32(typeid) == 12 || Convert.ToInt32(typeid) == 13)
                    {
                        //是輸出----【類別】【項目】【細項說明】【量】mL【顏色】【性狀】【氣味】，【備註】。
                        string SqlStr = " SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'iotype' AND P_GROUP = 'outputtype' AND P_VALUE='" + typeid + "'";
                        DataTable DTTYPE = iom.DBExecSQL(SqlStr);
                        string Str = DTTYPE.Rows[0]["P_NAME"] + " ";
                        switch (typeid)
                        {
                            case "7":
                                break;
                            default:
                                Str += type_tube_name + " ";
                                break;
                        }
                        Str += explanation_item.Trim() + " ";
                        //if (Times != "")
                        //{
                        //    Str += Times + "次, ";
                        //}
                        if (amount != "")
                        {
                            Str += amount + unitStr[Convert.ToInt32(amount_unit) - 1] + ", ";
                        }
                        else
                        {
                            Str += "Loss, ";
                        }

                        DataTable DTcolor = iom.DBExecSQL("SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'io' AND P_GROUP = 'outputcolor_Drainage' AND P_VALUE='" + form["color_drainage"] + "'");
                        switch (form["color_drainage"])
                        {
                            case "-1":
                                break;
                            case "99":
                                Str += form["color_other"].Trim() + " ";
                                break;
                            default:
                                Str += DTcolor.Rows[0][0].ToString() + " ";
                                break;
                        }
                        DataTable DTnature = iom.DBExecSQL("SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'io' AND P_GROUP = 'outputnature_Drainage' AND P_VALUE='" + form["nature_drainage"] + "'");
                        switch (form["nature_drainage"])
                        {
                            case "-1":
                                break;
                            case "99":
                                Str += form["nature_other"].Trim() + " ";
                                break;
                            default:
                                Str += DTnature.Rows[0][0].ToString() + " ";
                                break;
                        }
                        DataTable DTtaste = iom.DBExecSQL("SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'io' AND P_GROUP = 'outputtaste_Drainage' AND P_VALUE='" + form["taste_drainage"] + "'");
                        switch (form["taste_drainage"])
                        {
                            case "-1":
                                break;
                            case "99":
                                Str += form["taste_other"].Trim() + " ";
                                break;
                            default:
                                Str += DTtaste.Rows[0][0].ToString() + " ";
                                break;
                        }
                        if (remark.Trim() != "")
                        {
                            Str += remark.Trim() + "。";
                        }
                        else
                        {
                            Str = Str.Trim();
                            if ((Str.Substring(Str.Length - 1, 1) == ",") || (Str.Substring(Str.Length - 1, 1) == "，"))
                                Str = Str.Substring(0, Str.Length - 1);
                            Str += "。";
                        }
                        erow += base.Insert_CareRecord(date, id, "Output", "", "", Str, "", "", "IO_DATA");//form["txtarea_record"].ToString().Trim()
                    }
                }
                #endregion

                if(typeid == "14")
                {
                    string milkID = itemid;
                    //配方奶計價
                    if (milkID != "" && milkID != null)
                    {
                        int DefaultCapacity = 0;
                        int capacity = 0;
                        int UsageCapacity = 0;
                        string startTime = DateTime.Now.ToString("yyyy/MM/dd 00:00:00");
                        string endTime = DateTime.Now.ToString("yyyy/MM/dd 23:59:59");


                        string sql = "SELECT * FROM NIS_MILK_LOG WHERE MILK_ID = '" + milkID + "' AND FEENO = '" + feeno + "' AND STATUS = 'Y' AND RECORD_TIME BETWEEN TO_DATE('" + startTime + "','yyyy/MM/dd HH24:mi:ss') AND TO_DATE('" + endTime + "','yyyy/MM/dd HH24:mi:ss')";
                        DataTable dt = new DataTable();
                        link.DBExecSQL(sql, ref dt);

                        if (amount.ToString() != "")
                        {
                            UsageCapacity = int.Parse(amount);
                        }
                        //取得已使用的配方奶紀錄
                        if (dt.Rows.Count > 0)
                        {
                            capacity = int.Parse(dt.Rows[0]["MILK_CAPACITY"].ToString());
                        }
                        //取得配方奶對應計價碼
                        string ho_id = "";
                        string unit = "";
                        sql = "SELECT * FROM OBS_NBFORMULA WHERE IID = '" + milkID + "' AND DELETED IS NULL";
                        DataTable dtMilk = new DataTable();
                        link.DBExecSQL(sql, ref dtMilk);
                        if (dtMilk.Rows.Count > 0)
                        {
                            ho_id = dtMilk.Rows[0]["HO_ID"].ToString();
                            unit = dtMilk.Rows[0]["UNIT"].ToString();
                            if (unit == "瓶")
                            {
                                DefaultCapacity = int.Parse(dtMilk.Rows[0]["CAPACITY"].ToString());
                            }
                        }
                        if (unit == "瓶")
                        {
                            //如果相減小於0 或是沒有紀錄 計一瓶
                            if (capacity - UsageCapacity < 0 || dt.Rows.Count <= 0)
                            {
                                List<Bill_RECORD> billDataList = new List<Bill_RECORD>();
                                Bill_RECORD billData = new Bill_RECORD();
                                billData.HO_ID = ho_id;
                                billData.COUNT = "1";
                                billDataList.Add(billData);

                                SaveBillingRecord(billDataList);
                            }
                        }
                        else if (unit == "天")
                        {
                            //如果相減小於0 或是沒有紀錄 計一瓶
                            if (dt.Rows.Count <= 0)
                            {
                                List<Bill_RECORD> billDataList = new List<Bill_RECORD>();
                                Bill_RECORD billData = new Bill_RECORD();
                                billData.HO_ID = ho_id;
                                billData.COUNT = "1";
                                billDataList.Add(billData);

                                SaveBillingRecord(billDataList);
                            }
                        }

                        //配方奶寫log
                        int RemainingCapacity = capacity - UsageCapacity;
                        int newCapacity = 0;
                        string serial = creatid("MILK_LOG", userno, feeno, "0");


                        if (RemainingCapacity < 0)
                        {
                            newCapacity = DefaultCapacity + RemainingCapacity;
                        }
                        else
                        {
                            newCapacity = RemainingCapacity;
                        }

                        insertDataList.Clear();
                        insertDataList.Add(new DBItem("STATUS", "D", DBItem.DBDataType.String));
                        erow = link.DBExecUpdate("NIS_MILK_LOG", insertDataList, "MILK_ID = '" + milkID + "' AND FEENO = '" + feeno + "'");



                        insertDataList.Clear();
                        insertDataList.Add(new DBItem("SERIAL", serial, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CHARTNO", ptinfo.ChartNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("FEENO", ptinfo.FeeNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CREATE_NO", userno, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("RECORD_TIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("MILK_ID", milkID, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("MILK_NAME", "".ToString(), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("MILK_CAPACITY", newCapacity.ToString(), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));

                        erow = link.DBExecInsert("NIS_MILK_LOG", insertDataList);
                    }
                }
             
                //儲存成功
                if (erow > 0)
                {
                    //新增IO性狀
                    insertDataList.Clear();
                    insertDataList.Add(new DBItem("FEATUREID", id, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("COLORID", form["color_drainage"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("COLOROTHER", (form["color_other"] == null) ? "" : form["color_other"].Trim(), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("NATUREID", form["nature_drainage"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("NATUREOTHER", (form["nature_other"] == null) ? "" : form["nature_other"].Trim(), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("TASTEID", form["taste_drainage"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("TASTEOTHER", (form["taste_other"] == null) ? "" : form["taste_other"].Trim(), DBItem.DBDataType.String));
                    iom.DBExecInsert("TUBE_FEATURE", insertDataList);
                    string io = (int.Parse(typeid) < 6) ? "I" : "O";
                    Response.Write("<script>alert('新增成功!');window.location.href='IO_Insert_List?date=" + date + "&IO=" + io + "';</script>");
                }
                else
                    Response.Write("<script>alert('新增失敗!');window.location.href='IO_Insert_List';</script>");
                return new EmptyResult();
            }
            Response.Write("<script>alert('登入逾時');</script>");
            return new EmptyResult();
        }
        #endregion

        #region --查詢--
        [HttpGet]
        public ActionResult Inquire(string date = "", string hour = "", string unit = "",string hisview = "",string feeno = "")
        {
            if (hisview == "T" || Session["PatInfo"] == null)
            {
                his.ControllerContext = ControllerContext;
                his.getSession(feeno);
                if (ptinfo == null)
                {
                    ptinfo = (PatientInfo)Session["PatInfo"];
                }
            }

            if (Session["PatInfo"] != null)
            {
                int LossCount = 0;
                string sqlstr = "SELECT * FROM SYS_PARAMS  WHERE 0 = 0 AND P_GROUP IN ('intaketype','outputtype') AND P_MODEL = 'iotype'";
                sqlstr += " AND P_VALUE <= 10";
                sqlstr += " ORDER BY P_SORT";
                DataTable dtType = iom.DBExecSQL(sqlstr);
                ViewBag.dtType = dtType;
                if (hour == "")
                {////此區為設定初始值
                    hour = "2";
                }
                if (date == "")
                {////此區為設定初始值
                    date = DateTime.Now.ToString("yyyy/MM/dd");
                }
                if (unit == "")
                {////此區為設定初始值
                    unit = "1";
                }
                DataTable dt_io = iom.sel_io_data_byClassbyHour(ptinfo.FeeNo, date, hour);
                string SqlStr = "Select To_Char(CREATTIME,'yyyy/MM/dd HH24:MI') CREATETIME ,\"TYPEID\",SUM(CASE WHEN AMOUNT_UNIT<>'3' THEN AMOUNT ELSE AMOUNT*0.001 END) as AMOUNT,count(REASON) as C_REASON";
                SqlStr += " From IO_DATA where  DELETED IS NULL AND FEENO = '" + ptinfo.FeeNo + "' AND CREATTIME BETWEEN";
                SqlStr += " TO_DATE('" + date + " 07:01:00', 'yyyy/mm/dd hh24:mi:ss') AND";
                SqlStr += " TO_DATE('" + Convert.ToDateTime(date).AddDays(1).ToString("yyyy/MM/dd") + " 07:00:59', 'yyyy/mm/dd hh24:mi:ss')";
                SqlStr += " Group by To_Char(CREATTIME,'yyyy/MM/dd HH24:MI'),\"TYPEID\",REASON";
                SqlStr += " Order by To_Char(CREATTIME,'yyyy/MM/dd HH24:MI'),TO_NUMBER(TYPEID)";
                DataTable dt_new_io_day_query = iom.DBExecSQL(SqlStr);/////撈取依時間與TIDYPE的表,已計算完單位by jarvis.2016/07/13
                DataTable dt_io_data = iom.sel_io_data("", ptinfo.FeeNo, "", Convert.ToDateTime(date).ToString("yyyy/MM/dd 07:01:00"), Convert.ToDateTime(date).AddDays(1).ToString("yyyy/MM/dd 07:00:59"), "");
                #region --計算總計--
                double amount_i = 0.000, amount_o = 0.000, loss_i = 0, loss_o = 0;
                string totle = "";
                foreach (DataRow r in dt_io_data.Rows)
                {
                    if (r["P_GROUP"].ToString() == "intaketype")
                    {
                        if (r["AMOUNT"].ToString().Trim() != "")
                        {
                            double dou_amount_i = (r["AMOUNT_UNIT"].ToString() == "3") ? (Convert.ToDouble(r["AMOUNT"]) * 0.001) : Convert.ToDouble(r["AMOUNT"]);
                            //string str_amount_i = dou_amount_i.ToString("F2");
                            //amount_i += Convert.ToDouble(str_amount_i);
                            string tmp_unit = r["AMOUNT_UNIT"].ToString();
                            if (tmp_unit != "4" && tmp_unit != "5") { 
                                amount_i += Convert.ToDouble(dou_amount_i.ToString("F3"));
                            }
                        }
                        if (r["REASON"].ToString().Trim() != "")
                        {
                            loss_i++;
                        }
                    }
                    else if (r["P_GROUP"].ToString() == "outputtype")
                    {
                        if (r["AMOUNT"].ToString().Trim() != "")
                        {
                            double dou_amount_o = (r["AMOUNT_UNIT"].ToString() == "3") ? (Convert.ToDouble(r["AMOUNT"]) * 0.001) : Convert.ToDouble(r["AMOUNT"]);
                            //string str_amount_o = dou_amount_o.ToString("F2");
                            //amount_o += Convert.ToDouble(str_amount_o);
                            string tmp_unit = r["AMOUNT_UNIT"].ToString();
                            if (tmp_unit != "4" && tmp_unit != "5") { 
                                amount_o += Convert.ToDouble(dou_amount_o.ToString("F3"));
                            }
                        }
                        if (r["REASON"].ToString().Trim() != "")
                        {
                            loss_o++;
                        }
                    }
                }
                totle = amount_i + "mL";
                if (loss_i != 0)
                {
                    totle += "+Loss";
                }
                totle += "/" + amount_o + "mL";
                if (loss_o != 0)
                {
                    totle += "+Loss";
                }
                totle += " (";
                //if(((Convert.ToInt32(amount_i) - Convert.ToInt32(amount_o))) > 0)
                //    totle += " +";
                //totle += (Convert.ToInt32(amount_i) - Convert.ToInt32(amount_o)).ToString() + " mL/ 24hr) ";//一般
                if ((amount_i - amount_o) > 0)
                    totle += " +";
                totle += Math.Round((amount_i - amount_o), 3).ToString() + " mL/ 24hr) ";
                #endregion 計算總計-舊方法-end

                ViewBag.dt_new_io_day_query = dt_new_io_day_query;////新SQL組成的dataTable
                ViewBag.LossCount = LossCount;
                ViewBag.date = date;
                ViewBag.hour = hour;
                ViewBag.unit = unit;
                ViewBag.totle = totle;
                ViewBag.hisview = hisview;
                return View();
            }
            Response.Write("<script>alert('登入逾時');</script>");
            return new EmptyResult();
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Inquire_obs(FormCollection form)
        {
            return Inquire_obs(form["txtdate"], form["ddl_total_interval"], form["unit"], form["hisview"]);
        }
        [HttpGet]
        public ActionResult Inquire_obs(string date = "", string hour = "", string unit = "", string hisview = "", string feeno="")
        {
            if (hisview == "T" || Session["PatInfo"] == null)
            {
                his.ControllerContext = ControllerContext;
                his.getSession(feeno);
                if (ptinfo == null)
                {
                    ptinfo = (PatientInfo)Session["PatInfo"];
                }
            }

            if (Session["PatInfo"] != null)
            {
                int LossCount = 0;
                DataTable dtType = iom.DBExecSQL("SELECT * FROM SYS_PARAMS  WHERE 0 = 0 AND P_GROUP IN ('intaketype','outputtype') AND P_MODEL = 'iotype' ORDER BY TO_NUMBER(P_VALUE)");
                ViewBag.dtType = dtType;
                if (hour == "")
                {////此區為設定初始值
                    hour = "2";
                }
                if (date == "")
                {////此區為設定初始值
                    date = DateTime.Now.ToString("yyyy/MM/dd");
                }
                if (unit == "")
                {////此區為設定初始值
                    unit = "1";
                }
                DataTable dt_io = iom.sel_io_data_byClassbyHour(ptinfo.FeeNo, date, hour);
                string SqlStr = "Select To_Char(CREATTIME,'yyyy/MM/dd HH24:MI') CREATETIME ,\"TYPEID\",SUM(CASE WHEN AMOUNT_UNIT<>'3' THEN AMOUNT ELSE AMOUNT*0.001 END) as AMOUNT,count(REASON) as C_REASON";
                SqlStr += " From IO_DATA where  DELETED IS NULL AND FEENO = '" + ptinfo.FeeNo + "' AND CREATTIME BETWEEN";
                SqlStr += " TO_DATE('" + date + " 07:01:00', 'yyyy/mm/dd hh24:mi:ss') AND";
                SqlStr += " TO_DATE('" + Convert.ToDateTime(date).AddDays(1).ToString("yyyy/MM/dd") + " 07:00:59', 'yyyy/mm/dd hh24:mi:ss')";
                SqlStr += " Group by To_Char(CREATTIME,'yyyy/MM/dd HH24:MI'),\"TYPEID\",REASON";
                SqlStr += " Order by To_Char(CREATTIME,'yyyy/MM/dd HH24:MI'),TO_NUMBER(TYPEID)";
                DataTable dt_new_io_day_query = iom.DBExecSQL(SqlStr);/////撈取依時間與TIDYPE的表,已計算完單位by jarvis.2016/07/13
                DataTable dt_io_data = iom.sel_io_data("", ptinfo.FeeNo, "", Convert.ToDateTime(date).ToString("yyyy/MM/dd 07:01:00"), Convert.ToDateTime(date).AddDays(1).ToString("yyyy/MM/dd 07:00:59"), "");
                #region --計算總計--
                double amount_i = 0.000, amount_o = 0.000, loss_i = 0, loss_o = 0;
                string totle = "";
                foreach (DataRow r in dt_io_data.Rows)
                {
                    if (r["P_GROUP"].ToString() == "intaketype")
                    {
                        if (r["AMOUNT"].ToString().Trim() != "")
                        {
                            double dou_amount_i = (r["AMOUNT_UNIT"].ToString() == "3") ? (Convert.ToDouble(r["AMOUNT"]) * 0.001) : Convert.ToDouble(r["AMOUNT"]);
                            //string str_amount_i = dou_amount_i.ToString("F2");
                            //amount_i += Convert.ToDouble(str_amount_i);
                            string tmp_unit = r["AMOUNT_UNIT"].ToString();
                            if (tmp_unit != "4" && tmp_unit != "5")
                            {
                                amount_i += Convert.ToDouble(dou_amount_i.ToString("F3"));
                            }
                        }
                        if (r["REASON"].ToString().Trim() != "")
                        {
                            loss_i++;
                        }
                    }
                    else if (r["P_GROUP"].ToString() == "outputtype")
                    {
                        if (r["AMOUNT"].ToString().Trim() != "")
                        {
                            double dou_amount_o = (r["AMOUNT_UNIT"].ToString() == "3") ? (Convert.ToDouble(r["AMOUNT"]) * 0.001) : Convert.ToDouble(r["AMOUNT"]);
                            //string str_amount_o = dou_amount_o.ToString("F2");
                            //amount_o += Convert.ToDouble(str_amount_o);
                            string tmp_unit = r["AMOUNT_UNIT"].ToString();
                            if (tmp_unit != "4" && tmp_unit != "5")
                            {
                                amount_o += Convert.ToDouble(dou_amount_o.ToString("F3"));
                            }
                        }
                        if (r["REASON"].ToString().Trim() != "")
                        {
                            loss_o++;
                        }
                    }
                }
                totle = amount_i + "mL";
                if (loss_i != 0)
                {
                    totle += "+Loss";
                }
                totle += "/" + amount_o + "mL";
                if (loss_o != 0)
                {
                    totle += "+Loss";
                }
                totle += " (";
                //if(((Convert.ToInt32(amount_i) - Convert.ToInt32(amount_o))) > 0)
                //    totle += " +";
                //totle += (Convert.ToInt32(amount_i) - Convert.ToInt32(amount_o)).ToString() + " mL/ 24hr) ";//一般
                if ((amount_i - amount_o) > 0)
                    totle += " +";
                totle += Math.Round((amount_i - amount_o), 3).ToString() + " mL/ 24hr) ";
                #endregion 計算總計-舊方法-end

                ViewBag.dt_new_io_day_query = dt_new_io_day_query;////新SQL組成的dataTable
                ViewBag.LossCount = LossCount;
                ViewBag.date = date;
                ViewBag.hour = hour;
                ViewBag.unit = unit;
                ViewBag.totle = totle;
                ViewBag.hisview = hisview;

                return View();
            }
            Response.Write("<script>alert('登入逾時');</script>");
            return new EmptyResult();
        }
        [HttpGet]
        public ActionResult Inquire_tube(string date = "", string hour = "", string unit = "", string qry_date = "", string obs ="", string hisview = "", string feeno = "")
        {

            if (qry_date == null)
            {
                qry_date = Request.QueryString["qry_date"];
            }
            if (obs == null)
            {
                obs = Request.QueryString["obs"];
            }
            if (hisview == "T" || Session["PatInfo"] == null)
            {
                his.ControllerContext = ControllerContext;
                his.getSession(feeno);
                if (ptinfo == null)
                {
                    ptinfo = (PatientInfo)Session["PatInfo"];
                }
            }


            if (Session["PatInfo"] != null)
            {
                //宣告病患_取得住院日期
                PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
                ViewBag.InDate = Convert.ToDateTime(ptInfo.InDate).ToString("yyyy/MM/dd");

                int LossCount = 0;
                if (hour == "")
                {////此區為設定初始值
                    hour = "2";
                }
                if (qry_date == "")
                {////此區為設定初始值
                    date = DateTime.Now.ToString("yyyy/MM/dd");
                }
                else
                {
                    date = qry_date;
                }
                if (unit == "")
                {////此區為設定初始值
                    unit = "1";
                }

                string SqlStr = "";
                SqlStr = " SELECT IO_ID,IO_ITEMID  as ITEMID, MAIN.TYPEID,IO_CREATETIME as CREATETIME,IO_AMOUNT as AMOUNT,AMOUNT_UNIT,C_REASON as REASON,TUBEROW,TUBEID,FEENO,TYPE_NAME,POSITION,LOCATION_NAME,NUBER_NAME,NUMBEROTHER ";
                SqlStr += " ,CASE NUMBERID WHEN '99' THEN TYPE_NAME||POSITION||LOCATION_NAME||'#'||NUMBEROTHER ELSE TYPE_NAME||POSITION||LOCATION_NAME||'#'||NUBER_NAME END AS TUBE_CONTENT ";
                SqlStr += " FROM ( ";
                SqlStr += " Select IO_ID,ITEMID as IO_ITEMID, To_Char(CREATTIME,'yyyy/MM/dd hh24:mi:ss') IO_CREATETIME ,\"TYPEID\",SUM(CASE WHEN AMOUNT_UNIT<>'3' THEN AMOUNT ELSE AMOUNT*0.001 END) as IO_AMOUNT,AMOUNT_UNIT,count(REASON) as C_REASON";
                SqlStr += " From IO_DATA where TYPEID ='9' AND DELETED IS NULL AND FEENO = '" + ptinfo.FeeNo + "' ";
                SqlStr += " AND CREATTIME BETWEEN TO_DATE('" + date + " 07:01:00', 'yyyy/mm/dd hh24:mi:ss') ";
                SqlStr += " AND TO_DATE('" + Convert.ToDateTime(date).AddDays(1).ToString("yyyy/MM/dd") + " 07:00:59', 'yyyy/mm/dd hh24:mi:ss')";
                SqlStr += " Group by IO_ID,ITEMID,AMOUNT_UNIT, To_Char(CREATTIME,'yyyy/MM/dd hh24:mi:ss'),TYPEID,REASON";
                SqlStr += " Order by To_Char(CREATTIME,'yyyy/MM/dd hh24:mi:ss'),TO_NUMBER(TYPEID) ) MAIN ";
                SqlStr += "  LEFT OUTER JOIN ( ";
                SqlStr += " SELECT  TUBE.*,";
                SqlStr += " TUBE_FEATURE.NUMBERID,TUBE_FEATURE.NUMBEROTHER,TUBE_FEATURE.MATERIALID,TUBE_FEATURE.MATERIALOTHER, ";
                SqlStr += " (SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'tube' AND P_GROUP = 'tubePosition' AND P_VALUE = TUBE.LOCATION ) LOCATION_NAME, ";
                SqlStr += " (SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'tube' AND P_GROUP = 'tubeNumber' AND P_VALUE = TUBE_FEATURE.NUMBERID ) NUBER_NAME, ";
                SqlStr += "  (SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'tube' AND P_GROUP = 'tubeMaterial' AND P_VALUE = TUBE_FEATURE.MATERIALID ) MATERIAL_NAME, ";
                SqlStr += " (SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'tube' AND P_GROUP = 'tubeLengthUnit' AND P_VALUE = TUBE.LENGTHUNIT ) LENGTHUNIT_NAME, ";
                SqlStr += " (SELECT KINDNAME FROM TUBE_KIND WHERE KINDID = TUBE.TYPEID ) TYPE_NAME, ";
                SqlStr += " (SELECT TUBE_GROUP FROM TUBE_KIND WHERE KINDID = TUBE.TYPEID ) TYPE_GROUP, ";
                SqlStr += " (SELECT ASSESS_TYPE FROM TUBE_KIND WHERE KINDID = TUBE.TYPEID ) ASSESS_TYPE ";
                SqlStr += " FROM TUBE INNER JOIN TUBE_FEATURE ON TUBE.TUBEID = TUBE_FEATURE.FEATUREID  ORDER BY STARTTIME ASC ) DETAIL ";

                SqlStr += "    ON MAIN.IO_ITEMID = DETAIL.TUBEROW ";
                DataTable dt_io_tube = iom.DBExecSQL(SqlStr);
                //List<IO_Inquire> IOList = new List<IO_Inquire>();
                //IOList = (List<IO_Inquire>)dt_io_tube.ToList<IO_Inquire>();
                //string IOjson = JsonConvert.SerializeObject(IOList);
                DataTable dtType = iom.DBExecSQL("select distinct TUBE_CONTENT,TUBEROW,NUMBEROTHER from (" + SqlStr + " ) order by NUMBEROTHER");
                ViewBag.dtType = dtType;

                //#region --計算總計--
                double amount_o = 0.000, loss_o = 0;
                string totle = "Record day total 引流管輸出量：";
                if (dt_io_tube.Rows.Count > 0)
                {

                    //}
                    foreach (DataRow r in dt_io_tube.Rows)
                    {
                        if (r["AMOUNT"].ToString().Trim() != "")
                        {
                            double dou_amount_o = (r["AMOUNT_UNIT"].ToString() == "3") ? (Convert.ToDouble(r["AMOUNT"]) * 0.001) : Convert.ToDouble(r["AMOUNT"]);
                            amount_o += Convert.ToDouble(dou_amount_o.ToString("F3"));
                        }
                        if (r["REASON"].ToString().Trim() != "0")
                        {
                            loss_o++;
                            LossCount++;
                        }
                    }
                    totle += " ";

                    totle += Math.Round((amount_o), 3).ToString() + " mL ";
                    if (loss_o != 0)
                    {
                        totle += "+Loss " + loss_o + "次";
                    }
                    totle += " / 24hr ";
                    #endregion 計算總計-舊方法-end

                    ViewBag.LossCount = LossCount;
                }
                else
                {
                    ViewBag.LossCount = 0;
                }
                ViewBag.dt_new_io_day_query = dt_io_tube;////新SQL組成的dataTable
                ViewBag.date = date;
                string beforeday = Convert.ToDateTime(date).AddDays(-1).ToString("yyyy/MM/dd");
                string nextday = Convert.ToDateTime(date).AddDays(1).ToString("yyyy/MM/dd");
                ViewBag.beforeday = beforeday;
                ViewBag.nextday = nextday;
                ViewBag.hour = hour;
                ViewBag.unit = unit;
                ViewBag.totle = totle;
                ViewBag.obs = obs;
                ViewBag.hisview = hisview;
                return View();
            }
            Response.Write("<script>alert('登入逾時');</script>");
            return new EmptyResult();
        }


        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Inquire(FormCollection form)
        {
            return Inquire(form["txtdate"], form["ddl_total_interval"], form["unit"], form["hisview"]);
        }

        //引流管統計
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Inquire_tube(FormCollection form)
        {
            return Inquire_tube(form["txtdate"], form["ddl_total_interval"], form["unit"], form["qry_date"],  form["obs"], form["hisview"]);
        }

        [HttpPost]
        //引流管統計 for IO_Tube (引流管統計 Ajax )
        public ActionResult Inquire_tube_new(string date, string qry_date)
        {
            RESPONSE_MSG json_result = new RESPONSE_MSG();

            if (Session["PatInfo"] != null)
            {
                //宣告病患_取得住院日期
                PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
                ViewBag.InDate = Convert.ToDateTime(ptInfo.InDate).ToString("yyyy/MM/dd");

                if (qry_date == "")
                {////此區為設定初始值
                    date = DateTime.Now.ToString("yyyy/MM/dd");
                }
                else
                {
                    date = qry_date;
                }

                string SqlStr = "";
                SqlStr = " SELECT IO_ID,IO_ITEMID  as ITEMID, MAIN.TYPEID,IO_CREATETIME as CREATETIME,IO_AMOUNT as AMOUNT,AMOUNT_UNIT,C_REASON as REASON,TUBEROW,TUBEID,FEENO,TYPE_NAME,POSITION,LOCATION_NAME,NUBER_NAME,NUMBEROTHER ";
                SqlStr += " ,CASE NUMBERID WHEN '99' THEN TYPE_NAME||POSITION||LOCATION_NAME||'#'||NUMBEROTHER ELSE TYPE_NAME||POSITION||LOCATION_NAME||'#'||NUBER_NAME END AS TUBE_CONTENT ";
                SqlStr += " FROM ( ";
                SqlStr += " Select IO_ID,ITEMID as IO_ITEMID, To_Char(CREATTIME,'yyyy/MM/dd hh24:mi:ss') IO_CREATETIME ,\"TYPEID\",SUM(CASE WHEN AMOUNT_UNIT<>'3' THEN AMOUNT ELSE AMOUNT*0.001 END) as IO_AMOUNT,AMOUNT_UNIT,count(REASON) as C_REASON";
                SqlStr += " From IO_DATA where TYPEID ='9' AND DELETED IS NULL AND FEENO = '" + ptinfo.FeeNo + "' ";
                SqlStr += " AND CREATTIME BETWEEN TO_DATE('" + date + " 07:01:00', 'yyyy/mm/dd hh24:mi:ss') ";
                SqlStr += " AND TO_DATE('" + Convert.ToDateTime(date).AddDays(1).ToString("yyyy/MM/dd") + " 07:00:59', 'yyyy/mm/dd hh24:mi:ss')";
                SqlStr += " Group by IO_ID,ITEMID,AMOUNT_UNIT, To_Char(CREATTIME,'yyyy/MM/dd hh24:mi:ss'),TYPEID,REASON";
                SqlStr += " Order by To_Char(CREATTIME,'yyyy/MM/dd hh24:mi:ss'),TO_NUMBER(TYPEID) ) MAIN ";
                SqlStr += "  LEFT OUTER JOIN ( ";
                SqlStr += " SELECT  TUBE.*,";
                SqlStr += " TUBE_FEATURE.NUMBERID,TUBE_FEATURE.NUMBEROTHER,TUBE_FEATURE.MATERIALID,TUBE_FEATURE.MATERIALOTHER, ";
                SqlStr += " (SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'tube' AND P_GROUP = 'tubePosition' AND P_VALUE = TUBE.LOCATION ) LOCATION_NAME, ";
                SqlStr += " (SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'tube' AND P_GROUP = 'tubeNumber' AND P_VALUE = TUBE_FEATURE.NUMBERID ) NUBER_NAME, ";
                SqlStr += "  (SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'tube' AND P_GROUP = 'tubeMaterial' AND P_VALUE = TUBE_FEATURE.MATERIALID ) MATERIAL_NAME, ";
                SqlStr += " (SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'tube' AND P_GROUP = 'tubeLengthUnit' AND P_VALUE = TUBE.LENGTHUNIT ) LENGTHUNIT_NAME, ";
                SqlStr += " (SELECT KINDNAME FROM TUBE_KIND WHERE KINDID = TUBE.TYPEID ) TYPE_NAME, ";
                SqlStr += " (SELECT TUBE_GROUP FROM TUBE_KIND WHERE KINDID = TUBE.TYPEID ) TYPE_GROUP, ";
                SqlStr += " (SELECT ASSESS_TYPE FROM TUBE_KIND WHERE KINDID = TUBE.TYPEID ) ASSESS_TYPE ";
                SqlStr += " FROM TUBE INNER JOIN TUBE_FEATURE ON TUBE.TUBEID = TUBE_FEATURE.FEATUREID  ORDER BY STARTTIME ASC ) DETAIL ";

                SqlStr += "    ON MAIN.IO_ITEMID = DETAIL.TUBEROW ";
                DataTable dt_io_tube = iom.DBExecSQL(SqlStr);

                List<IO_Inquire> IOList = new List<IO_Inquire>();
                IOList = (List<IO_Inquire>)dt_io_tube.ToList<IO_Inquire>();
                string tubejson = JsonConvert.SerializeObject(IOList);

                json_result.attachment = tubejson;


                ViewBag.tubeData = json_result;
                //return Content(JsonConvert.SerializeObject(json_result), "application/json");
                return Content(JsonConvert.SerializeObject(ViewBag), "application/json");
                //return View();
            }
            else
            {
                Response.Write("<script>alert('登入逾時');</script>");
                ViewBag.tubeData = "";
                return new EmptyResult();
            }

        }
        /// <summary>
        /// 範圍查詢頁面
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult IO_Range_Index(string StartDate = "", string EndDate = "", string unit = "", string obs = "")
        {
            if (Session["PatInfo"] != null)
            {
                if (unit == "")
                {
                    unit = "1";
                }
                if (StartDate == "")
                {
                    StartDate = DateTime.Now.ToString("yyyy/MM/dd");
                }
                if (EndDate == "")
                {
                    EndDate = DateTime.Now.ToString("yyyy/MM/dd");
                }
                if (string.IsNullOrEmpty(obs))
                {
                    obs = "F"; ;
                }
                string feeno = ptinfo.FeeNo;
                DataTable dtType = iom.DBExecSQL("SELECT * FROM SYS_PARAMS  WHERE 0 = 0 AND P_GROUP IN ('intaketype','outputtype') AND P_MODEL = 'iotype' ORDER BY P_GROUP,P_SORT");
                DataTable dt_io_data = iom.sel_io_data_byRange(feeno, StartDate, EndDate);
                List<NIS.Models.IOManager.IOManagerUnit> io_List = new List<NIS.Models.IOManager.IOManagerUnit>();
                if (dt_io_data != null && dt_io_data.Rows.Count > 0)
                {
                    for (int i = 0; i < dt_io_data.Rows.Count; i++)
                    {
                        io_List.Add(new NIS.Models.IOManager.IOManagerUnit(
                              dt_io_data.Rows[i]["IO_ROW"].ToString(), dt_io_data.Rows[i]["IO_ID"].ToString()
                            , Convert.ToDateTime(dt_io_data.Rows[i]["CREATTIME"]), dt_io_data.Rows[i]["CREANO"].ToString()
                            , dt_io_data.Rows[i]["TYPEID"].ToString(), dt_io_data.Rows[i]["ITEMID"].ToString()
                            , dt_io_data.Rows[i]["AMOUNT_UNIT"].ToString() == "3" ? (Convert.ToDouble(dt_io_data.Rows[i]["AMOUNT"].ToString().Trim()) * 0.001).ToString() : dt_io_data.Rows[i]["AMOUNT"].ToString()
                            , dt_io_data.Rows[i]["AMOUNT_UNIT"].ToString() //2016/06/23 Vanda mL=g=1000mg
                            , dt_io_data.Rows[i]["POSITION"].ToString(), dt_io_data.Rows[i]["REASON"].ToString()
                            , dt_io_data.Rows[i]["EXPLANATION_ITEM"].ToString(), dt_io_data.Rows[i]["REMARK"].ToString()
                            , dt_io_data.Rows[i]["CREANAME"].ToString(), dt_io_data.Rows[i]["P_ID"].ToString()
                            , dt_io_data.Rows[i]["P_VALUE"].ToString(), dt_io_data.Rows[i]["P_SORT"].ToString()
                        ));
                    }
                }
                ViewBag.dtType = dtType;
                ViewData["io_List"] = io_List;
                ViewBag.start_date = StartDate;
                ViewBag.end_date = EndDate;
                ViewBag.seleunit = unit;
                ViewBag.obs = obs;

                return View();
            }
            Response.Write("<script>alert('登入逾時');</script>");
            return new EmptyResult();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult IO_Range_Index(FormCollection form)
        {
            return IO_Range_Index(form["start_date"], form["end_date"], form["unit"], form["obs"]);
        }

        /// <summary>
        /// 帶入護理記錄
        /// </summary>
        /// <returns>json</returns>
        [HttpPost]
        public string TakeCareRecord()
        {
            string[] unitStr = { "mL", "g", "mg", "分鐘", "次" };
            int erow = 0; string StrO = ""; string title = "";
            string dateNow = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");//現在時間
            string date = Request["Pdate"].ToString();//紀錄的時間
            string feeno = ptinfo.FeeNo;
            string userno = userinfo.EmployeesNo;
            string id = base.creatid("IO_DATA", userno, feeno, "0");
            string type = Request["Ptypei"].ToString();//如果是1，處理"每班"；如果是2，處理"總量"。
            string Pclass = Request["Pclass"].ToString();
            string Punit = "1"; //Request["Punit"].ToString(); //2016/06/23 Vanda 總量一律為ml
            string Func_from =  Request["Func_from"].ToString();
            if (type == "1") //每班
            {
                //I:【攝入排出-異常處置措施】。
                //(一般)O:"班內 Record I/O：【攝入總量】/【排出總量】 ml ( 【攝入總量－排出總量】/ 8hrs)+【Loss】。(LOSS有發生才SHOW，不論發生幾次都指SHOW ""LOSS"")"
                string PIValue = Request["PIValue"].ToString();
                string POValue = Request["POValue"].ToString();
                string PBMValue = string.Empty;
                if (!string.IsNullOrEmpty(Request["PBMValue"]))
                {
                    PBMValue = Request["PBMValue"].ToString();
                }
                string PBTValue = string.Empty;
                if (!string.IsNullOrEmpty(Request["PBTValue"]))
                {
                    PBTValue = Request["PBTValue"].ToString();
                }

                string PclassName = Request["PclassName"].ToString();
                string PlossI = Request["PclasslossI"].ToString();
                string PlossO = Request["PclasslossO"].ToString();
                string StrI = Request["Pdealwith"].ToString().Trim();//處置措施
                if (Func_from == "Inquire")
                {
                    title = "I/O";
                    StrO = date + " " + PclassName + " 班內 Record I/O：" + PIValue + unitStr[Convert.ToInt32(Punit) - 1];
                    if (PlossI != "0")
                    {
                        StrO += "+loss";
                    }
                    StrO += "/" + POValue + unitStr[Convert.ToInt32(Punit) - 1];
                    if (PlossO != "0")
                    {
                        StrO += "+loss";
                    }
                    string plus = "";
                    Decimal NumValue_str = Convert.ToDecimal(PIValue) - Convert.ToDecimal(POValue);
                    if (NumValue_str >= 0)
                    {
                        plus = "+";
                    }
                    StrO += " ( " + plus + "" + (NumValue_str).ToString() + "mL/ 8hrs)";//一般
                    if (!string.IsNullOrEmpty(PBMValue) )
                    {
                        StrO += " ，含乳時間：" + PBMValue + unitStr[3];
                    }
                    if (!string.IsNullOrEmpty(PBTValue))
                    {
                        StrO += " ，排便及排尿次數：" + PBTValue + unitStr[4];
                    }

                }
                else
                {
                    title = " I/O 引流管統計";
                    StrO = date + title + Request["SubtotalO"].TrimEnd('；')+ "。";//處置措施
                }

               // StrO = StrO.TrimEnd()
                erow = base.Insert_CareRecord_Black(dateNow, id, title, "", "", StrO, StrI, "");
            }
            else //總量
            {
                string StrI = Request["PdealwithALL"];//處置措施
                if (Func_from == "Inquire")
                {
                    title = "Record I/O";
                    StrO = date + " Day total Record I/O " + Request["AllTotal"];//處置措施
                }
                else
                {
                    title = "I/O 引流管統計";  
                    StrO = date + " " + Request["AllTotalO"].ToString().Trim().TrimEnd('；') + "。"; //引流管總計                                                            
                }               
                            
                erow = base.Insert_CareRecord_Black(dateNow, id, title, "", "", StrO, StrI, ""); //20181112 護理部要求改成全部黑字(IO總量/引流管統計)

            }
            if (erow > 0)
            {
                return "Y";
            }
            else
            {
                return "N";
            }
        }
        #endregion
    }
}