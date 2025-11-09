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
using System.Data.OleDb;

namespace NIS.Controllers
{
    public class CatheterFlushVolumeController : BaseController
    {
        private CommData cd;
        private DBConnector link;
        private FlushCatheter fc;

        public CatheterFlushVolumeController()
        {
            this.cd = new CommData();
            this.link = new DBConnector();
            this.fc = new FlushCatheter();
        }


        /// <summary>
        /// //連續性沖洗尿管出入量-首頁
        /// </summary>
        /// <param name="flag">傳入帶入i/o的勾勾(帶=Y;沒帶=Null)</param>
        /// <returns></returns>
        public ActionResult Index(bool flag = false)
        {
            try
            {
                if (Session["PatInfo"] != null)
                {
                    List<Dictionary<string, string>> Dt = new List<Dictionary<string, string>>();
                    Dictionary<string, string> Temp = null;
                    //白班：0701-1500
                    //小夜：1501-2300
                    //大夜：2301-0700
                    //今日大夜與明日大夜之間：0701-0700
                    DateTime now = DateTime.Now;
                    string searchDateTime = "";
                    if (int.Parse(now.ToString("HHmm")) <= 700)
                        searchDateTime = now.AddDays(-1).ToString("yyyy/MM/dd 23:01:00");
                    else if (int.Parse(now.ToString("HHmm")) <= 1500)
                        searchDateTime = now.ToString("yyyy/MM/dd 07:01:00");
                    else if (int.Parse(now.ToString("HHmm")) <= 2300)
                        searchDateTime = now.ToString("yyyy/MM/dd 15:01:00");
                    else
                        searchDateTime = now.ToString("yyyy/MM/dd 23:01:00");

                    string sql = "SELECT FC.RECORD_TIME, FC.RECORD_CLASS, FC.AMOUNT, FC.COLORID, FC.COLOROTHER, FC.BIT_SURPLUS, FC.UPDNAME, FC.POST_OP "
                    + ",(SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'catheter_flush' AND P_GROUP = 'urine_color' AND P_VALUE = FC.COLORID)COLORNAME "
                    + "FROM FLUSH_CATHETER_DATA FC "
                    + "WHERE DELETED IS NULL "
                    + "AND FEENO = '" + base.ptinfo.FeeNo + "' "
                    + "AND RECORD_TIME BETWEEN TO_DATE('" + searchDateTime + "','yyyy/mm/dd hh24:mi:ss') "
                    + "AND TO_DATE('" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/mm/dd hh24:mi:ss') "
                    + "ORDER BY RECORD_TIME, UPDTIME ";
                    DataTable Dtt = link.DBExecSQL(sql);
                    if (Dtt.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dtt.Rows.Count; i++)
                        {
                            Temp = new Dictionary<string, string>();
                            Temp["RECORD_TIME"] = Convert.ToDateTime(Dtt.Rows[i]["RECORD_TIME"].ToString()).ToString("yyyy/MM/dd HH:mm");
                            Temp["RECORD_CLASS"] = Dtt.Rows[i]["RECORD_CLASS"].ToString();
                            Temp["AMOUNT"] = Dtt.Rows[i]["AMOUNT"].ToString();
                            Temp["COLORNAME"] = (Dtt.Rows[i]["COLORID"].ToString() == "99") ? Dtt.Rows[i]["COLOROTHER"].ToString() : Dtt.Rows[i]["COLORNAME"].ToString();
                            Temp["BIT_SURPLUS"] = Dtt.Rows[i]["BIT_SURPLUS"].ToString();
                            Temp["UPDNAME"] = Dtt.Rows[i]["UPDNAME"].ToString();
                            Temp["POST_OP"] = Dtt.Rows[i]["POST_OP"].ToString();
                            Dt.Add(Temp);
                        }
                    }
                    ViewBag.flag = flag;
                    ViewData["Dt"] = Dt;
                    ViewData["urine_color"] = this.cd.getSelectItem("catheter_flush", "urine_color", "");
                    return View();
                }

                Response.Write("<script>alert('登入逾時');</script>");
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                Response.Write("<script>alert('網路異常，請重新操作！');</script>");
                return new EmptyResult();
            }
        }

        #region --新增--
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Insert(FormCollection form)
        {
            if(Session["PatInfo"] != null)
            {
                int erow = 0;
                string recordDate = form["insert_day"] + " " + form["insert_time"];
                string remark = form["txt_remark"].Trim();
                bool flag = false;
                List<DBItem> baseDataList = new List<DBItem>(), insertDataList = null;
                string ID = string.Empty;
                //畫面上還沒有此項目，先備留
                baseDataList.Add(new DBItem("AMOUNT_UNIT", "1", DBItem.DBDataType.String));
                baseDataList.Add(new DBItem("RECORD_TIME", recordDate, DBItem.DBDataType.DataTime));
                baseDataList.Add(new DBItem("FEENO", base.ptinfo.FeeNo, DBItem.DBDataType.String));
                baseDataList.Add(new DBItem("REMARK", remark, DBItem.DBDataType.String));
                baseDataList.Add(new DBItem("CREATNO", base.userinfo.EmployeesNo, DBItem.DBDataType.String));
                baseDataList.Add(new DBItem("CREATNAME", base.userinfo.EmployeesName, DBItem.DBDataType.String));
                baseDataList.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                baseDataList.Add(new DBItem("UPDNO", base.userinfo.EmployeesNo, DBItem.DBDataType.String));
                baseDataList.Add(new DBItem("UPDNAME", base.userinfo.EmployeesName, DBItem.DBDataType.String));
                baseDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                baseDataList.Add(new DBItem("TAKE_IO", (form["cb_into_io"] == "Y") ? "1" : "0", DBItem.DBDataType.String));
                //沖入
                if(!string.IsNullOrWhiteSpace(form["txt_i_amount"]))
                {
                    insertDataList = new List<DBItem>(baseDataList);
                    ID = base.creatid("FLUSH_CATHETER_DATA", base.userinfo.EmployeesNo, base.ptinfo.FeeNo, "I");
                    insertDataList.Add(new DBItem("RECORD_ID", ID, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("RECORD_CLASS", "0", DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("BIT_SURPLUS", (!string.IsNullOrWhiteSpace(form["txt_bit_last"])) ? form["txt_bit_last"] : "", DBItem.DBDataType.Number));
                    insertDataList.Add(new DBItem("AMOUNT", form["txt_i_amount"], DBItem.DBDataType.Number));
                    if(!string.IsNullOrWhiteSpace(form["PostOp"]) && form["PostOp"] == "Y")
                        insertDataList.Add(new DBItem("POST_OP", form["PostOp"], DBItem.DBDataType.String));
                    erow = this.link.DBExecInsert("FLUSH_CATHETER_DATA", insertDataList);

                    #region --帶入IO--
                    if(!string.IsNullOrWhiteSpace(form["cb_into_io"]) && form["cb_into_io"] == "Y")
                    {
                        try
                        {
                            base.Insert_IO_DATA(recordDate, ID, "5", "IO_ITEM_09277__20160607161229994_0", form["txt_i_amount"], "1", ID, "FLUSH_CATHETER_DATA");
                        }
                        catch { }
                    }
                    #endregion

                    #region --帶入護理記錄--
                    if(!string.IsNullOrWhiteSpace(form["cb_into_care"]) && form["cb_into_care"] == "Y")
                    {
                        //沖入
                        //一般：依醫囑予沖入0.9% Normal Saline【量】ml，班內沖洗液剩餘量：【餘量】，【備註】。
                        string Str = "依醫囑予沖入0.9% Normal Saline "
                            + form["txt_i_amount"] + "ml"
                            + ((!string.IsNullOrWhiteSpace(form["txt_bit_last"])) ? "，班內沖洗液剩餘量：" + form["txt_bit_last"] : "")
                            + "ml"
                            + ((!string.IsNullOrWhiteSpace(remark)) ? "，" + remark : "") + "。";
                        if(!string.IsNullOrWhiteSpace(form["PostOp"]) && form["PostOp"] == "Y")
                        {
                            Str += "(POST OP)。";//先這樣判斷，等模版到，再行修改。add by jarvis 201611181748
                        }
                        try
                        {
                            base.Insert_CareRecord(recordDate, ID, "連續性沖洗尿管出入量紀錄", "", "", Str, "", "", "FLUSH_CATHETER_DATA");
                        }
                        catch { }
                    }
                    #endregion
                }
                //沖出
                if(!string.IsNullOrWhiteSpace(form["txt_o_amount"]))
                {
                    insertDataList = new List<DBItem>(baseDataList);
                    ID = base.creatid("FLUSH_CATHETER_DATA", base.userinfo.EmployeesNo, base.ptinfo.FeeNo, "O");
                    insertDataList.Add(new DBItem("RECORD_ID", ID, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("RECORD_CLASS", "1", DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("COLORID", form["urine_color"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("COLOROTHER", (form["urine_color"] == "99" && !string.IsNullOrWhiteSpace(form["color_other"])) ? form["color_other"].Trim() : "", DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("AMOUNT", form["txt_o_amount"], DBItem.DBDataType.Number));
                    erow += this.link.DBExecInsert("FLUSH_CATHETER_DATA", insertDataList);

                    #region --帶入IO--
                    if(!string.IsNullOrWhiteSpace(form["cb_into_io"]) && form["cb_into_io"] == "Y")
                    {
                        try
                        {
                            base.Insert_IO_DATA(recordDate, ID, "10", "IO_ITEM_09277__20160607161631171_0", form["txt_o_amount"], "1", ID, "FLUSH_CATHETER_DATA");
                            base.Insert_IO_Additional(ID, form["urine_color"], (form["urine_color"] == "99" && !string.IsNullOrWhiteSpace(form["color_other"])) ? form["color_other"].Trim() : "", "", "", "", "");
                        }
                        catch { }
                    }
                    #endregion

                    #region --帶入護理記錄--
                    if(!string.IsNullOrWhiteSpace(form["cb_into_care"]) && form["cb_into_care"] == "Y")
                    {
                        //沖出
                        //一般：沖出【量】ml尿液呈【顏色】，【備註】。
                        string Str = "沖出 " + form["txt_o_amount"] + "ml，尿液呈";
                        if(form["urine_color"] == "99")
                            Str += form["color_other"].Trim();
                        else
                        {
                            DataTable DTcolor = this.link.DBExecSQL("SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'catheter_flush' AND P_GROUP = 'urine_color' AND P_VALUE='" + form["urine_color"] + "'");
                            if(DTcolor != null && DTcolor.Rows.Count > 0)
                            {
                                Str += DTcolor.Rows[0][0].ToString() + " ";
                            }
                        }
                        Str += ((!string.IsNullOrWhiteSpace(remark)) ? "，" + remark : "") + "。";
                        try
                        {
                            base.Insert_CareRecord(recordDate, ID, "連續性沖洗尿管出入量紀錄", "", "", Str, "", "", "FLUSH_CATHETER_DATA");
                        }
                        catch { }
                    }
                    #endregion
                }

                flag = (form["cb_into_io"] == "Y") ? true : false;

                if(erow > 0)
                    Response.Write("<script>alert('儲存成功！');window.location.href='Index?flag=" + flag + "';</script>");
                else
                    Response.Write("<script>alert('儲存失敗！')window.location.href='Index?flag=" + flag + "';</script>");

                return new EmptyResult();
            }
            Response.Write("<script>alert('登入逾時');</script>");
            return new EmptyResult();
        }
        #endregion

        #region --修改頁面--
        [HttpGet]
        public ActionResult Edit(string row, string date)
        {
            if(Session["PatInfo"] != null)
            {
                string feeno = ptinfo.FeeNo;
                DataTable dt = fc.QueryReCordData(feeno, row);
                ViewData["urine_color"] = this.cd.getSelectItem("catheter_flush", "urine_color", "");
                ViewBag.date = date;
                ViewBag.dt = dt;
                DataTable CareDt = this.link.DBExecSQL("SELECT * FROM CARERECORD_DATA WHERE 0=0 AND FEENO='" + feeno + "' AND DELETED IS NULL AND CARERECORD_ID='" + row + "'");
                ViewBag.CareDt = CareDt;
                return View();
            }
            Response.Write("<script>alert('登入逾時');window.close();</script>");
            return new EmptyResult();
        }

        //修改儲存
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Edit(FormCollection form)
        {
            if(Session["PatInfo"] != null)
            {
                int erow = 0;
                string Record_Class = form["record_class"];
                string userno = userinfo.EmployeesNo;
                string feeno = ptinfo.FeeNo;
                string id = form["record_id"];
                string date = form["insert_day"] + " " + form["insert_time"];
                string now = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                string amount = (Record_Class == "0") ? form["txt_i_amount"] : form["txt_o_amount"];
                string post_str_temp = string.Empty;
                List<DBItem> insertDataList = new List<DBItem>();
                insertDataList.Add(new DBItem("RECORD_TIME", date, DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("AMOUNT", amount, DBItem.DBDataType.Number));
                insertDataList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDTIME", now, DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("REMARK", form["txt_remark"], DBItem.DBDataType.String));
                //為了取原資料，是否有勾POST OP
                string SqlStr = "SELECT * FROM FLUSH_CATHETER_DATA WHERE FEENO='" + feeno + "' ";
                SqlStr += "AND RECORD_ID='" + id + "' AND RECORD_CLASS='0' AND DELETED IS NULL ";
                DataTable Dt = this.link.DBExecSQL(SqlStr);
                if(Dt != null && Dt.Rows.Count > 0)
                {
                    post_str_temp = Dt.Rows[0]["POST_OP"].ToString();
                }

                if(Record_Class == "0")
                {
                    if(form["txt_bit_last"] != "")
                    {
                        insertDataList.Add(new DBItem("BIT_SURPLUS", form["txt_bit_last"], DBItem.DBDataType.Number));
                    }
                    else
                    {//如需空值換成存""，再實行以下
                        insertDataList.Add(new DBItem("BIT_SURPLUS", "NULL", DBItem.DBDataType.Number));
                    }
                    #region --帶入護理記錄--沖入
                    if(form["hid_into_care"] == "1")
                    {//有備註的沖入RECORD****依醫囑予沖入0.9% Normal Saline【量】ml，【備註】。
                        if(form["hid_carerecordhas"] != "1")
                        {//為0就新增
                            //沖入
                            //一般：依醫囑予沖入0.9% Normal Saline【量】ml，班內沖洗液剩餘量：【餘量】，【備註】。
                            string Str = "依醫囑予沖入0.9% Normal Saline "
                                + amount + "ml"
                                + ((!string.IsNullOrWhiteSpace(form["txt_bit_last"])) ? "，班內沖洗液剩餘量：" + form["txt_bit_last"] : "")
                                + "ml"
                                + ((!string.IsNullOrWhiteSpace(form["txt_remark"])) ? "，" + form["txt_remark"] : "") + "。";
                            if(!string.IsNullOrEmpty(post_str_temp))
                            {
                                Str += "(POST OP)。";//先這樣判斷，等模版到，再行修改。add by jarvis 201611181748
                            }
                            try
                            {
                                base.Insert_CareRecord(date, id, "連續性沖洗尿管出入量紀錄", "", "", Str, "", "", "FLUSH_CATHETER_DATA");
                            }
                            catch { }
                        }
                        else
                        {//為1就修改
                            //沖入
                            //一般：依醫囑予沖入0.9% Normal Saline【量】ml，班內沖洗液剩餘量：【餘量】，【備註】。
                            string Str = "依醫囑予沖入0.9% Normal Saline "
                                + amount + "ml"
                                + ((!string.IsNullOrWhiteSpace(form["txt_bit_last"])) ? "，班內沖洗液剩餘量：" + form["txt_bit_last"] : "")
                                + "ml"
                                + ((!string.IsNullOrWhiteSpace(form["txt_remark"])) ? "，" + form["txt_remark"] : "") + "。";
                            if(!string.IsNullOrEmpty(post_str_temp))
                            {
                                Str += "(POST OP)。";//先這樣判斷，等模版到，再行修改。add by jarvis 201611181748
                            }
                            try
                            {
                                base.Upd_CareRecord(date, id, "連續性沖洗尿管出入量紀錄", "", "", Str, "", "", "FLUSH_CATHETER_DATA");
                            }
                            catch { }
                        }
                    }
                    else
                    {//如果未打勾，且有資料，就執行刪除
                        if(form["hid_carerecordhas"] == "1")
                        {  //真刪
                            //DBConnector dbconnector = new DBConnector();
                            try
                            {
                                link.DBExecDelete("CARERECORD_DATA", "CARERECORD_ID ='" + id + "' AND SELF ='FLUSH_CATHETER_DATA'");
                            }
                            catch { }
                        }
                        else
                        {

                        }
                    }
                    #endregion

                    #region 連動修改帶入IO-沖入
                    try
                    {
                        base.Update_IO_DATA(date, id, "5", "IO_ITEM_09277__20160607161229994_0", form["txt_i_amount"], "1", id, "FLUSH_CATHETER_DATA");
                    }
                    catch { }
                    #endregion
                }
                else
                {
                    insertDataList.Add(new DBItem("COLORID", form["urine_color"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("COLOROTHER", (form["color_other"] == null) ? "" : form["color_other"].Trim(), DBItem.DBDataType.String));
                    #region--帶入護理記錄--沖出
                    //未完成
                    if(form["hid_into_care"] == "1")
                    {//有備註的沖入RECORD****依醫囑予沖入0.9% Normal Saline【量】ml，【備註】。
                        if(form["hid_carerecordhas"] != "1")
                        {//為0就新增
                            //沖出
                            //一般：沖出【量】ml尿液呈【顏色】，【備註】。
                            string Str = "沖出 " + form["txt_o_amount"] + "ml，尿液呈";
                            if(form["urine_color"] == "99")
                                Str += form["color_other"].Trim();
                            else
                            {
                                DataTable DTcolor = this.link.DBExecSQL("SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'catheter_flush' AND P_GROUP = 'urine_color' AND P_VALUE='" + form["urine_color"] + "'");
                                if(DTcolor != null && DTcolor.Rows.Count > 0)
                                {
                                    Str += DTcolor.Rows[0][0].ToString() + " ";
                                }
                            }
                            Str += ((!string.IsNullOrWhiteSpace(form["txt_remark"])) ? "，" + form["txt_remark"] : "") + "。";
                            try
                            {
                                base.Insert_CareRecord(date, id, "連續性沖洗尿管出入量紀錄", "", "", Str, "", "", "FLUSH_CATHETER_DATA");
                            }
                            catch { }
                        }
                        else
                        {//1修改
                            //沖出
                            //一般：沖出【量】ml尿液呈【顏色】，【備註】。
                            string Str = "沖出 " + form["txt_o_amount"] + "ml，尿液呈";
                            if(form["urine_color"] == "99")
                                Str += form["color_other"].Trim();
                            else
                            {
                                DataTable DTcolor = this.link.DBExecSQL("SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'catheter_flush' AND P_GROUP = 'urine_color' AND P_VALUE='" + form["urine_color"] + "'");
                                if(DTcolor != null && DTcolor.Rows.Count > 0)
                                {
                                    Str += DTcolor.Rows[0][0].ToString() + " ";
                                }
                            }
                            Str += ((!string.IsNullOrWhiteSpace(form["txt_remark"])) ? "，" + form["txt_remark"] : "") + "。";
                            try
                            {
                                base.Upd_CareRecord(date, id, "連續性沖洗尿管出入量紀錄", "", "", Str, "", "", "FLUSH_CATHETER_DATA");
                            }
                            catch { }
                        }
                    }
                    else
                    {//如果未打勾，且有資料，就執行刪除
                        if(form["hid_carerecordhas"] == "1")
                        {  //真刪
                            //DBConnector dbconnector = new DBConnector();
                            erow += link.DBExecDelete("CARERECORD_DATA", "CARERECORD_ID ='" + id + "' AND SELF ='FLUSH_CATHETER_DATA'");
                        }
                    }
                    #endregion

                    #region 連動修改帶入IO-沖出
                    try
                    {
                        base.Update_IO_DATA(date, id, "10", "IO_ITEM_09277__20160607161631171_0", form["txt_o_amount"], "1", id, "FLUSH_CATHETER_DATA");
                        base.Update_IO_Additional(id, form["urine_color"], (form["urine_color"] == "99" && !string.IsNullOrWhiteSpace(form["color_other"])) ? form["color_other"].Trim() : "", "", "", "", "");
                    }
                    catch { }
                    #endregion
                }
                erow += this.link.DBExecUpdate("FLUSH_CATHETER_DATA", insertDataList, "RECORD_ID = '" + form["record_id"] + "' AND FEENO = '" + feeno + "' AND RECORD_CLASS='" + Record_Class + "'");
                if(erow > 0)
                {
                    Response.Write("<script>alert('更新成功!');window.location.href='Detail?date=" + form["date"] + "';</script>");//window.opener.location.reload();
                }
                else
                {
                    Response.Write("<script>alert('更新失敗!');window.location.href='Detail?date=" + form["date"] + "';</script>");
                }
            }
            else
                Response.Write("<script>alert('登入逾時');window.close();</script>");
            return new EmptyResult();
        }

        //刪除
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Data_Delete(FormCollection form)
        {
            if(Session["PatInfo"] != null)
            {
                int erow = 0;
                string userno = userinfo.EmployeesNo;
                string feeno = ptinfo.FeeNo;
                List<DBItem> insertDataList = new List<DBItem>();
                insertDataList.Add(new DBItem("DELETED", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                erow = this.link.DBExecUpdate("FLUSH_CATHETER_DATA", insertDataList, "RECORD_ID = '" + form["record_id"] + "' AND FEENO = '" + feeno + "' ");

                erow += base.Del_CareRecord(form["record_id"], "FLUSH_CATHETER_DATA");
                try
                {
                    base.Delete_IO_DATA(form["record_id"], "FLUSH_CATHETER_DATA");
                }
                catch { }
                insertDataList = null;
                if(erow > 0)
                {
                    Response.Write("<script>alert('刪除成功!');window.location.href='Detail?date=" + form["date"] + "';</script>");
                }
                else
                {
                    Response.Write("<script>alert('刪除失敗!');window.location.href='Detail?date=" + form["date"] + "';</script>");
                }
            }
            else
                Response.Write("<script>alert('登入逾時');window.close();</script>");
            return new EmptyResult();
        }
        #endregion


        /// <summary>
        ///目前沒有再用這個view 
        /// </summary>
        /// <returns></returns>
        public ActionResult Inquire()
        {
            return View();
        }

        #region --範圍查詢頁面--

        #region --範圍查詢頁面--
        [HttpGet]
        public ActionResult Range_Index(string StartDate = "", string EndDate = "")
        {
            if(Session["PatInfo"] != null)
            {
                if(StartDate == "")
                {
                    StartDate = DateTime.Now.ToString("yyyy/MM/dd");
                }
                if(EndDate == "")
                {
                    EndDate = DateTime.Now.ToString("yyyy/MM/dd");
                }
                string feeno = ptinfo.FeeNo;
                DataTable dt_data = fc.sel_flush_catheter_data_bydate(feeno, StartDate, EndDate);
                List<NIS.Models.FlushCatheter.flush_catheter_data> fc_List = new List<NIS.Models.FlushCatheter.flush_catheter_data>();
                if(dt_data != null && dt_data.Rows.Count > 0)
                {
                    for(int i = 0; i < dt_data.Rows.Count; i++)
                    {
                        fc_List.Add(new NIS.Models.FlushCatheter.flush_catheter_data(
                              dt_data.Rows[i]["RECORD_ID"].ToString(), Convert.ToDateTime(dt_data.Rows[i]["RECORD_TIME"].ToString())
                            , dt_data.Rows[i]["RECORD_CLASS"].ToString(), dt_data.Rows[i]["AMOUNT"].ToString()
                            , dt_data.Rows[i]["COLORID"].ToString(), dt_data.Rows[i]["COLOROTHER"].ToString()
                            , dt_data.Rows[i]["COLORNAME"].ToString(), dt_data.Rows[i]["BIT_SURPLUS"].ToString()
                            , dt_data.Rows[i]["POST_OP"].ToString()
                        ));
                    }
                }
                DataTable dt_time_table = new DataTable();
                dt_time_table.Columns.Add("morning");
                dt_time_table.Columns.Add("night");
                dt_time_table.Columns.Add("bignight");
                string[] classhourRangeStart = { "07:01", "15:01", "23:01" };
                string[] classhourRangeEnd = { "15:00", "23:00", "07:00" };
                for(DateTime x = Convert.ToDateTime(StartDate); x <= Convert.ToDateTime(EndDate); x = x.AddDays(1))
                {
                    DataRow row = dt_time_table.NewRow();
                    if(x <= Convert.ToDateTime(EndDate))
                    {
                        for(int y = 0; y < 3; y++)
                        {
                            //DateTime TempX = (y != 2) ? x : x.AddDays(-1);
                            DateTime TempX = (y != 2) ? x : x.AddDays(1);
                            //DataTable dt = fc.min_max_recordtime(feeno, TempX.ToString("yyyy/MM/dd ") + classhourRangeStart[y], x.ToString("yyyy/MM/dd ") + classhourRangeEnd[y]);
                            DataTable dt = fc.min_max_recordtime(feeno, x.ToString("yyyy/MM/dd ") + classhourRangeStart[y], TempX.ToString("yyyy/MM/dd ") + classhourRangeEnd[y]);
                            //if(dt != null && dt.Rows.Count > 0 && dt.Rows[0]["MAX"].ToString() != "")
                            if(dt != null && dt.Rows.Count > 0 && dt.Rows[0]["RECORD_TIME"].ToString() != "")
                            {
                                //修正為dt有值就跟目前班別結束時間相減，沒值就是帶出8小時 by jarvis lu 20160912
                                TimeSpan Total = Convert.ToDateTime(TempX.ToString("yyyy/MM/dd ") + classhourRangeEnd[y]).Subtract(Convert.ToDateTime(dt.Rows[0]["RECORD_TIME"].ToString()));
                                string minutes = Math.Round(((decimal)(Total.Minutes) / 60), 1).ToString();
                                double time_difference = Convert.ToDouble(minutes) + Total.Hours;
                                switch(y)
                                {
                                    case 0:
                                        row["morning"] = time_difference;
                                        break;
                                    case 1:
                                        row["night"] = time_difference;
                                        break;
                                    case 2:
                                        row["bignight"] = time_difference;
                                        break;
                                }
                            }
                            else
                            {
                                switch(y)
                                {
                                    case 0:
                                        row["morning"] = "8";
                                        break;
                                    case 1:
                                        row["night"] = "8";
                                        break;
                                    case 2:
                                        row["bignight"] = "8";
                                        break;
                                }
                            }
                        }
                    }
                    dt_time_table.Rows.Add(row);
                }
                dt_time_table.AcceptChanges();
                ViewData["fc_List"] = fc_List;
                ViewBag.dt_time_table = dt_time_table;
                ViewBag.start_date = StartDate;
                ViewBag.end_date = EndDate;
                ViewBag.FeeNo = feeno;
                ViewBag.RootDocument = GetSourceUrl();
                return View();
            }
            Response.Write("<script>alert('登入逾時');</script>");
            return new EmptyResult();
        }
        #endregion

        #region --範圍查詢頁面--回傳POST查詢日期跳轉///目前省略用此跳轉 by.20161018 jarvis
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Range_Index(FormCollection form)
        {
            if(Session["PatInfo"] != null)
            {
                Response.Write("<script>window.location.href='../CatheterFlushVolume/Range_Index?StartDate=" + form["start_date"] + "&EndDate=" + form["end_date"] + "';</script>");
            }
            Response.Write("<script>alert('登入逾時');</script>");
            return new EmptyResult();
        }

        //帶護理記錄，按照班別
        public string TakeCareRecord()
        {
            string[] shiftName = { "日班", "小夜班", "大夜班" };
            int shift = Convert.ToInt32(Request["Pclass"].ToString());

            string PAll_i = Request["PAll_i"].ToString();
            string PAll_o = Request["PAll_o"].ToString();
            string PAll_sub = Request["PAll_sub"].ToString();

            string PInAmount = Request["PInAmount"].ToString();
            string POutAmount = Request["POutAmount"].ToString();
            string PSubAmount = Request["PSubAmount"].ToString();
            string PTemp_Color = Request["PTemp_Color"].ToString();
            string PBitAmount = Request["PBitAmount"].ToString();
            string dateNow = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");//現在時間
            string feeno = ptinfo.FeeNo;
            string userno = userinfo.EmployeesNo;
            string id = base.creatid("FLUSH_CATHETER_DATA", userno, feeno, "0");
            string Str = "";
            if(shift != 2)
            {   //白班、小夜班小計
                //治療/處置：連續性沖洗尿管出入量紀錄
                //一般：班內連續性沖洗尿管出入量統計：【總沖入量】/【總沖出量】，尿量【尿量】，呈【顏色】。班內點滴餘量【點滴餘量】。
                Str = "班內連續性沖洗尿管出入量統計：" + PInAmount + "/" + POutAmount + "，尿量" + PSubAmount;
                if(PTemp_Color != "")
                {
                    Str += "，呈" + PTemp_Color;
                }
                Str += "。班內點滴餘量" + PBitAmount + "。";
            }
            else
            {   //大夜班小技
                //治療/處置：連續性沖洗尿管出入量紀錄
                //一般：班內連續性沖洗尿管出入量統計：【總沖入量】/【總沖出量】，尿量【尿量】，呈【顏色】。24小時總沖出入量：【總沖入量】/【總沖入量】，總尿量【總尿量/小時】。
                Str = "班內連續性沖吸尿管出入量統計：" + PInAmount + "/" + POutAmount + "，尿量" + PSubAmount;
                if(PTemp_Color != "")
                {
                    Str += "，呈" + PTemp_Color;
                }
                Str += "。24小時總沖出入量：" + PAll_i + "/" + PAll_o + "，總尿量 ";
                if(Convert.ToInt32(PAll_sub) > 0)
                {
                    Str += "+";
                }
                Str += PAll_sub + " ml / 24hr。";
            }
            int erow = base.Insert_CareRecord_Black(dateNow, id, "連續性沖洗尿管出入量紀錄", "", "", Str, "", "");

            if(erow > 0)
            {
                return "Y";
            }
            else
            {
                return "N";
            }
        }


        #endregion

        #region--明細--
        [HttpGet]
        public ActionResult Detail(string date)
        {
            string feeno = ptinfo.FeeNo;
            string userno = userinfo.EmployeesNo;
            DataTable dt_data = fc.sel_flush_catheter_data_bydate(feeno, date, date);
            ViewBag.date = date;
            ViewBag.dt_data = dt_data;
            ViewBag.userno = userno;
            return View();
        }
        #endregion

        #region--列印--
        public ActionResult PrintList(string feeno, string StartDate = "", string EndDate = "")
        {
            PatientInfo pinfo = new PatientInfo();
            byte[] ByteCode = webService.GetPatientInfo(feeno);
            //病人資訊
            if(ByteCode != null)
                pinfo = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(ByteCode));
            ViewData["ptinfo"] = pinfo;
            DataTable dt_data = fc.sel_flush_catheter_data_bydate(feeno, StartDate, EndDate);
            List<NIS.Models.FlushCatheter.flush_catheter_data> fc_List = new List<NIS.Models.FlushCatheter.flush_catheter_data>();
            if(dt_data != null && dt_data.Rows.Count > 0)
            {
                for(int i = 0; i < dt_data.Rows.Count; i++)
                {
                    fc_List.Add(new NIS.Models.FlushCatheter.flush_catheter_data(
                          dt_data.Rows[i]["RECORD_ID"].ToString(), Convert.ToDateTime(dt_data.Rows[i]["RECORD_TIME"].ToString())
                        , dt_data.Rows[i]["RECORD_CLASS"].ToString(), dt_data.Rows[i]["AMOUNT"].ToString()
                        , dt_data.Rows[i]["COLORID"].ToString(), dt_data.Rows[i]["COLOROTHER"].ToString()
                        , dt_data.Rows[i]["COLORNAME"].ToString(), dt_data.Rows[i]["BIT_SURPLUS"].ToString()
                        , dt_data.Rows[i]["POST_OP"].ToString()
                    ));
                }
            }
            DataTable dt_time_table = new DataTable();
            dt_time_table.Columns.Add("morning");
            dt_time_table.Columns.Add("night");
            dt_time_table.Columns.Add("bignight");
            string[] classhourRangeStart = { "07:01", "15:01", "23:01" };
            string[] classhourRangeEnd = { "15:00", "23:00", "07:00" };
            for(DateTime x = Convert.ToDateTime(StartDate); x <= Convert.ToDateTime(EndDate); x = x.AddDays(1))
            {
                DataRow row = dt_time_table.NewRow();
                if(x <= Convert.ToDateTime(EndDate))
                {
                    for(int y = 0; y < 3; y++)
                    {
                        //DateTime TempX = (y != 2) ? x : x.AddDays(-1);
                        DateTime TempX = (y != 2) ? x : x.AddDays(1);
                        //DataTable dt = fc.min_max_recordtime(feeno, TempX.ToString("yyyy/MM/dd ") + classhourRangeStart[y], x.ToString("yyyy/MM/dd ") + classhourRangeEnd[y]);
                        DataTable dt = fc.min_max_recordtime(feeno, x.ToString("yyyy/MM/dd ") + classhourRangeStart[y], TempX.ToString("yyyy/MM/dd ") + classhourRangeEnd[y]);
                        //if(dt != null && dt.Rows.Count > 0 && dt.Rows[0]["MAX"].ToString() != "")
                        if(dt != null && dt.Rows.Count > 0 && dt.Rows[0]["RECORD_TIME"].ToString() != "")
                        {
                            //修正為dt有值就跟目前班別結束時間相減，沒值就是帶出8小時 by jarvis lu 20160912
                            TimeSpan Total = Convert.ToDateTime(TempX.ToString("yyyy/MM/dd ") + classhourRangeEnd[y]).Subtract(Convert.ToDateTime(dt.Rows[0]["RECORD_TIME"].ToString()));
                            string minutes = Math.Round(((decimal)(Total.Minutes) / 60), 1).ToString();
                            double time_difference = Convert.ToDouble(minutes) + Total.Hours;
                            switch(y)
                            {
                                case 0:
                                    row["morning"] = time_difference;
                                    break;
                                case 1:
                                    row["night"] = time_difference;
                                    break;
                                case 2:
                                    row["bignight"] = time_difference;
                                    break;
                            }
                        }
                        else
                        {
                            switch(y)
                            {
                                case 0:
                                    row["morning"] = "8";
                                    break;
                                case 1:
                                    row["night"] = "8";
                                    break;
                                case 2:
                                    row["bignight"] = "8";
                                    break;
                            }
                        }
                    }
                }
                dt_time_table.Rows.Add(row);
            }
            dt_time_table.AcceptChanges();
            ViewData["fc_List"] = fc_List;
            ViewBag.dt_time_table = dt_time_table;

            ViewBag.StartDate = StartDate;
            ViewBag.EndDate = EndDate;
            ViewBag.FeeNo = feeno;
            ViewBag.RootDocument = GetSourceUrl();
            return View();
        }
        #endregion
        #endregion
    }
}