using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using NIS.Data;
using NIS.Models;
using NIS.UtilTool;
using NIS.WebService;

namespace NIS.Controllers
{
    public class BladderTrainingController : BaseController
    {
        private DBConnector link;

        public BladderTrainingController()
        {
            this.link = new DBConnector();
        }

        //列表查詢
        public ActionResult List(string StartDate, string EndDate)
        {
            if(Session["PatInfo"] != null)
            {
                if(string.IsNullOrWhiteSpace(StartDate))
                    StartDate = DateTime.Now.ToString("yyyy/MM/dd");
                if(string.IsNullOrWhiteSpace(EndDate))
                    EndDate = DateTime.Now.ToString("yyyy/MM/dd");

                string StartYesterday = Convert.ToDateTime(StartDate).AddDays(-1).ToString("yyyy/MM/dd 23:00:00");
                string SqlStr = "SELECT * FROM BLADDER_DATA "
                + "WHERE FEENO = '" + base.ptinfo.FeeNo + "' AND DELETED IS NULL "
                + "AND RECORD_TIME BETWEEN TO_DATE('" + StartYesterday + "','yyyy/mm/dd hh24:mi:ss') "
                + "AND TO_DATE('" + EndDate + " 22:59:59','yyyy/mm/dd hh24:mi:ss') "
                + "ORDER BY RECORD_TIME, UPDTIME";
                DataTable dt = this.link.DBExecSQL(SqlStr);

                ViewBag.dt = dt;
                ViewBag.StartDate = StartDate;
                ViewBag.EndDate = EndDate;
                ViewBag.FeeNo = base.ptinfo.FeeNo;
                ViewBag.RootDocument = GetSourceUrl();
                return View();
            }

            Response.Write("<script>alert('請重新選擇病患');</script>");
            return new EmptyResult();
        }

        //刪除
        public string DelData(string ID)
        {
            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("DELETED", base.userinfo.EmployeesNo, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("UPDNO", base.userinfo.EmployeesNo, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("UPDNAME", base.userinfo.EmployeesName, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
            int erow = this.link.DBExecUpdate("BLADDER_DATA", insertDataList, "RECORD_ID = '" + ID + "' AND FEENO = '" + base.ptinfo.FeeNo + "' ");
            base.Delete_IO_DATA(ID, "BLADDER_DATA");
            base.Del_CareRecord(ID, "BLADDER_DATA");

            if(erow > 0)
                return "Y";
            else
                return "N";
        }

        //新增
        [HttpGet]
        public ActionResult Insert(string StartDate, string EndDate, string id)
        {
            if(!string.IsNullOrWhiteSpace(id))
            {
                string feeno = ptinfo.FeeNo;
                DataTable dt = this.link.DBExecSQL("SELECT * FROM BLADDER_DATA WHERE FEENO = '" + feeno + "' "
                + "AND DELETED IS NULL AND RECORD_ID = '" + id + "'");
                ViewBag.dt = dt;

                DataTable dt_io = this.link.DBExecSQL("SELECT * FROM IO_DATA WHERE FEENO='" + feeno + "' "
               + "AND SOURCE_ID='" + id + "'"
               + "AND SOURCE='BLADDER_DATA' AND DELETED IS NULL ");
                ViewBag.chkbtn_io = (dt_io.Rows.Count > 0) ? "Y" : "N";
            }
            //如需切換回LIST時使用
            ViewBag.StartDate = StartDate;
            ViewBag.EndDate = EndDate;
            return View();
        }

        //儲存
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Insert(FormCollection form)
        {
            if(Session["PatInfo"] != null)
            {
                string date = form["txt_day"] + " " + form["txt_time"];
                string feeno = ptinfo.FeeNo;
                string username = userinfo.EmployeesName;
                string userno = userinfo.EmployeesNo;
                string id = base.creatid("BLADDER_DATA", userno, feeno, "0");
                string nowtime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                List<DBItem> insertDataList = new List<DBItem>();
                insertDataList.Add(new DBItem("CATHETER_SORT", form["rb_catheter"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("TRAINING_STATUS", form["rb_training_status"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("URINE_FEEL", form["rb_urine_feel"], DBItem.DBDataType.String));

                if(!string.IsNullOrEmpty(form["txt_assimilate_quantity"].Trim()))
                    insertDataList.Add(new DBItem("INTAKE_WATER", form["txt_assimilate_quantity"].Trim(), DBItem.DBDataType.Number));

                if(!string.IsNullOrEmpty(form["txt_urine_last"].Trim()))
                    insertDataList.Add(new DBItem("RESIDUAL_UNINE", form["txt_urine_last"].Trim(), DBItem.DBDataType.Number));

                if(form["rb_catheter"] == "1" && !string.IsNullOrEmpty(form["txt_self_solve"].Trim()))
                    insertDataList.Add(new DBItem("SELF_SITUATION", form["txt_self_solve"].Trim(), DBItem.DBDataType.Number));

                insertDataList.Add(new DBItem("REMARK", form["txt_remark_0"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("RECORD_ID", id, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("RECORD_TIME", date, DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATNAME", username, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATTIME", nowtime, DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDNAME", username, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDTIME", nowtime, DBItem.DBDataType.DataTime));
                int erow = this.link.DBExecInsert("BLADDER_DATA", insertDataList);

                #region 帶入I/O
                if(!string.IsNullOrWhiteSpace(form["cb_into_io"]) && form["cb_into_io"] == "Y")
                {
                    if(form["rb_catheter"] == "1")
                    {
                        //膀胱造廔，自解排尿量→輸出，尿液，自解量
                        string SelfSituation = (string.IsNullOrEmpty(form["txt_self_solve"].Trim())) ? "" : form["txt_self_solve"].Trim();
                        if(SelfSituation != "")
                        {
                            base.Insert_IO_DATA(date, id + "_0", "6", "IO_ITEM_09277__20160607161414341_0", SelfSituation, "1", id, "BLADDER_DATA");
                        }
                        //膀胱造廔，餘尿量→輸出，尿液，單次導尿管
                        string ResidualUnine = (string.IsNullOrEmpty(form["txt_urine_last"].Trim())) ? "" : form["txt_urine_last"].Trim();
                        if(ResidualUnine != "")
                        {
                            base.Insert_IO_DATA(date, id + "_1", "6", "IO_ITEM_09277__20160607161414444_2", ResidualUnine, "1", id, "BLADDER_DATA");
                        }
                    }
                    else
                    {
                        //尿管，餘尿量→輸出，尿液，留置導尿管
                        string ResidualUnine = (string.IsNullOrEmpty(form["txt_urine_last"].Trim())) ? "" : form["txt_urine_last"].Trim();
                        if(ResidualUnine != "")
                        {
                            base.Insert_IO_DATA(date, id + "_2", "6", "IO_ITEM_09277__20160607161414411_1", ResidualUnine, "1", id, "BLADDER_DATA");
                        }
                    }
                    //攝水量→輸入，由口進食，水
                    string Intake_water = (form["txt_assimilate_quantity"].Trim() == "") ? "" : form["txt_assimilate_quantity"].Trim();
                    if(Intake_water != "")
                    {
                        base.Insert_IO_DATA(date, id + "_3", "2", "IO_ITEM_14391__20160705090857867_0", Intake_water, "1", id, "BLADDER_DATA");
                    }
                }
                #endregion

                #region 帶入護理記錄
                //【時間】【尿管類別】【訓練-狀態】，【尿液感】尿液感，自解【自解排尿量】ml，攝水量【攝水量】ml，餘尿量【餘尿量】ml，【備註】，續觀。
                //註：自解、攝水量、餘尿量、備註沒資料時不呈現。
                //2016/08/02 問題清單-膀胱訓練-1050809 改為必定帶入護理記錄
                string TempStrStatus = ((form["rb_training_status"] == "0") ? "關閉 " : "開啟 ");
                string Str = date + " "
                    + ((form["rb_catheter"] == "0") ? "尿管 " : "膀胱造廔 ") + TempStrStatus + "，"
                    + ((form["rb_urine_feel"] == "0") ? "無 尿液感，" : "有 尿液感，")
                    + ((form["rb_catheter"] == "1" && form["txt_self_solve"].Trim() != "") ? "自解 " + form["txt_self_solve"].Trim() + " ml，" : "")
                    + ((form["txt_assimilate_quantity"].Trim() != "") ? "攝水量 " + form["txt_assimilate_quantity"].Trim() + " ml，" : "")
                    + ((form["txt_urine_last"].Trim() != "") ? "餘尿量 " + form["txt_urine_last"].Trim() + " ml，" : "")
                    + ((form["txt_remark_0"].Trim() != "") ? " " + form["txt_remark_0"].Trim() + " ，" : "") + "續觀。";

                base.Insert_CareRecord(date, id, "膀胱訓練 訓練-" + TempStrStatus, "", "", Str, "", "", "BLADDER_DATA");
                #endregion

                if(erow > 0)
                {
                    Response.Write("<script>alert('新增成功');window.location.href = '../BladderTraining/List?StartDate=" + form["StartDate"] + "&EndDate=" + form["EndDate"] + "';</script>");
                }
                else
                {
                    Response.Write("<script>alert('修改失敗');window.location.href = '../BladderTraining/Insert?StartDate=" + form["StartDate"] + "&EndDate=" + form["EndDate"] + "&id=" + form["id"] + "';</script>");
                }
            }
            else
                Response.Write("<script>alert('登入逾時');</script>");

            return new EmptyResult();
        }

        //修改
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Edit(FormCollection form)
        {
            if(Session["PatInfo"] != null)
            {
                string date = form["txt_day"] + " " + form["txt_time"];

                List<DBItem> UpDataList = new List<DBItem>();
                UpDataList.Add(new DBItem("CATHETER_SORT", form["rb_catheter"], DBItem.DBDataType.String));
                UpDataList.Add(new DBItem("TRAINING_STATUS", form["rb_training_status"], DBItem.DBDataType.String));
                UpDataList.Add(new DBItem("URINE_FEEL", form["rb_urine_feel"], DBItem.DBDataType.String));

                if(!string.IsNullOrEmpty(form["txt_assimilate_quantity"].Trim()))
                    UpDataList.Add(new DBItem("INTAKE_WATER", form["txt_assimilate_quantity"].Trim(), DBItem.DBDataType.Number));
                else
                    UpDataList.Add(new DBItem("INTAKE_WATER", "Null", DBItem.DBDataType.Number));

                if(!string.IsNullOrEmpty(form["txt_urine_last"].Trim()))
                    UpDataList.Add(new DBItem("RESIDUAL_UNINE", form["txt_urine_last"].Trim(), DBItem.DBDataType.Number));
                else
                    UpDataList.Add(new DBItem("RESIDUAL_UNINE", "Null", DBItem.DBDataType.Number));

                if(form["rb_catheter"] == "1" && !string.IsNullOrEmpty(form["txt_self_solve"].Trim()))
                    UpDataList.Add(new DBItem("SELF_SITUATION", form["txt_self_solve"].Trim(), DBItem.DBDataType.Number));
                else
                    UpDataList.Add(new DBItem("SELF_SITUATION", "Null", DBItem.DBDataType.Number));

                UpDataList.Add(new DBItem("REMARK", form["txt_remark_0"], DBItem.DBDataType.String));
                UpDataList.Add(new DBItem("RECORD_TIME", date, DBItem.DBDataType.DataTime));
                UpDataList.Add(new DBItem("UPDNO", userinfo.EmployeesNo, DBItem.DBDataType.String));
                UpDataList.Add(new DBItem("UPDNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                UpDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));

                string where = "RECORD_ID = '" + form["id"] + "' AND DELETED IS NULL AND FEENO = '" + ptinfo.FeeNo + "' ";
                int erow = this.link.DBExecUpdate("BLADDER_DATA", UpDataList, where);

                #region 修改I/O
                if(this.link.DBExecDelete("IO_DATA", "FEENO = '" + ptinfo.FeeNo + "' AND SOURCE_ID = '" + form["id"] + "' AND SOURCE = 'BLADDER_DATA' ") > 0)
                {
                    if(form["rb_catheter"] == "1")
                    {
                        //膀胱造廔，自解排尿量→輸出，尿液，自解量
                        string SelfSituation = (string.IsNullOrEmpty(form["txt_self_solve"].Trim())) ? "" : form["txt_self_solve"].Trim();
                        if(SelfSituation != "")
                        {
                            base.Insert_IO_DATA(date, form["id"] + "_0", "6", "IO_ITEM_09277__20160607161414341_0", SelfSituation, "1", form["id"], "BLADDER_DATA");
                        }
                        //膀胱造廔，餘尿量→輸出，尿液，單次導尿管
                        string ResidualUnine = (string.IsNullOrEmpty(form["txt_urine_last"].Trim())) ? "" : form["txt_urine_last"].Trim();
                        if(ResidualUnine != "")
                        {
                            base.Insert_IO_DATA(date, form["id"] + "_1", "6", "IO_ITEM_09277__20160607161414444_2", ResidualUnine, "1", form["id"], "BLADDER_DATA");
                        }
                    }
                    else
                    {
                        //尿管，餘尿量→輸出，尿液，留置導尿管
                        string ResidualUnine = (string.IsNullOrEmpty(form["txt_urine_last"].Trim())) ? "" : form["txt_urine_last"].Trim();
                        if(ResidualUnine != "")
                        {
                            base.Insert_IO_DATA(date, form["id"] + "_2", "6", "IO_ITEM_09277__20160607161414411_1", ResidualUnine, "1", form["id"], "BLADDER_DATA");
                        }
                    }
                    //攝水量→輸入，由口進食，水
                    string Intake_water = (form["txt_assimilate_quantity"].Trim() == "") ? "" : form["txt_assimilate_quantity"].Trim();
                    if(Intake_water != "")
                    {
                        base.Insert_IO_DATA(date, form["id"] + "_3", "2", "IO_ITEM_14391__20160705090857867_0", Intake_water, "1", form["id"], "BLADDER_DATA");
                    }
                }
                #endregion

                #region 修改護理記錄
                //【時間】【尿管類別】【訓練-狀態】，【尿液感】尿液感，自解【自解排尿量】ml，攝水量【攝水量】，餘尿量【餘尿量】ml，【備註】，續觀。
                //註：自解、攝水量、餘尿量、備註沒資料時不呈現。
                //2016/08/02 問題清單-膀胱訓練-1050809 改為必定帶入護理記錄
                string TempStrStatus = ((form["rb_training_status"] == "0") ? "關閉 " : "開啟 ");
                string Str = date + " "
                    + ((form["rb_catheter"] == "0") ? "尿管 " : "膀胱造廔 ") + TempStrStatus + "，"
                    + ((form["rb_urine_feel"] == "0") ? "無 尿液感，" : "有 尿液感，")
                    + ((form["rb_catheter"] == "1" && form["txt_self_solve"].Trim() != "") ? "自解 " + form["txt_self_solve"].Trim() + " ml，" : "")
                    + ((form["txt_assimilate_quantity"].Trim() != "") ? "攝水量 " + form["txt_assimilate_quantity"].Trim() + " ml，" : "")
                    + ((form["txt_urine_last"].Trim() != "") ? "餘尿量 " + form["txt_urine_last"].Trim() + " ml，" : "")
                    + ((form["txt_remark_0"].Trim() != "") ? " " + form["txt_remark_0"].Trim() + " ，" : "") + "續觀。";

                base.Upd_CareRecord(date, form["id"], "膀胱訓練 訓練-" + TempStrStatus, "", "", Str, "", "", "BLADDER_DATA");
                #endregion

                if(erow > 0)
                {
                    Response.Write("<script>alert('修改成功');window.location.href = '../BladderTraining/List?StartDate=" + form["StartDate"] + "&EndDate=" + form["EndDate"] + "';</script>");
                }
                else
                {
                    Response.Write("<script>alert('修改失敗');window.location.href = '../BladderTraining/Insert?StartDate=" + form["StartDate"] + "&EndDate=" + form["EndDate"] + "&id=" + form["id"] + "';</script>");
                }
            }
            else
                Response.Write("<script>alert('登入逾時');</script>");

            return new EmptyResult();
        }

        public ActionResult Print_List(string feeno, string StartDate = "", string EndDate = "")
        {
            PatientInfo pinfo = new PatientInfo();
            byte[] ByteCode = webService.GetPatientInfo(feeno);
            //病人資訊
            if(ByteCode != null)
                pinfo = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(ByteCode));
            ViewData["ptinfo"] = pinfo;
            if(StartDate == "")
            {
                StartDate = DateTime.Now.ToString("yyyy/MM/dd");
            }
            if(EndDate == "")
            {
                EndDate = DateTime.Now.ToString("yyyy/MM/dd");
            }
            //string feeno = ptinfo.FeeNo;
            string StartYesterday = Convert.ToDateTime(StartDate).AddDays(-1).ToString("yyyy/MM/dd 23:00:00");
            string SqlStr = "SELECT * FROM BLADDER_DATA WHERE 0=0 AND FEENO='" + feeno + "' AND DELETED IS NULL ";
            SqlStr += " AND RECORD_TIME BETWEEN TO_DATE('" + StartYesterday + "','yyyy/mm/dd hh24:mi:ss') ";
            SqlStr += " AND TO_DATE('" + EndDate + " 22:59:59','yyyy/mm/dd hh24:mi:ss')";
            SqlStr += "ORDER BY RECORD_TIME,UPDTIME";
            DataTable dt = this.link.DBExecSQL(SqlStr);
            ViewBag.dt = dt;
            ViewBag.StartDate = StartDate;
            ViewBag.EndDate = EndDate;
            return View();
        }

    }
}
