using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NIS.Models;
using System.Data;

namespace NIS.Controllers
{
    public class PressureSoreAssessController : BaseController
    {
        DBConnector link = new DBConnector();

        public ActionResult List()
        {//判斷有無病人session
            if (Session["PatInfo"] != null)
            {
                ViewBag.dt = this.sel_pressure_data(base.ptinfo.FeeNo, "", "");
            ViewBag.userno = base.userinfo.EmployeesNo;

            }
            else
            {
                Response.Write("<script>alert('請重新選擇病患');</script>");
                return new EmptyResult();
            }
            return View();
        }

        public ActionResult Insert(string id)
        {
            if (!string.IsNullOrWhiteSpace(id))
                ViewBag.dt = this.sel_pressure_data(ptinfo.FeeNo, "", id);
            else
            {
                //舊資料帶入
                ViewBag.dt_new = this.sel_pressure_data(ptinfo.FeeNo, "", "");
            }
            ViewData["MinDate"] = base.GetMinDate();
            return View();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Insert_Pressure(FormCollection form)
        {
            bool success = false;

            try
            {
                string date = form["txt_day"] + " " + form["txt_time"];
                string id = creatid("PRESSURE_SORE_DATA", base.userinfo.EmployeesNo, base.ptinfo.FeeNo, "0");

                List<DBItem> insertDataList = new List<DBItem>();
                insertDataList.Add(new DBItem("PRESSURE_ID", id, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FEENO", base.ptinfo.FeeNo, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATNO", base.userinfo.EmployeesNo, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("RECORDTIME", date, DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("NUTRITION", form["rb_nutrition"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PERCEPTION", form["rb_perception"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("DAMP", form["rb_damp"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ACTIVITY", form["rb_activity"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MOVING", form["rb_moving"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FRICTION", form["rb_friction"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("HAVE", form["rb_have"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("TOTAL", form["total"], DBItem.DBDataType.Number));

                if (this.link.DBExecInsertTns("NIS_PRESSURE_SORE_DATA", insertDataList) > 0)
                {
                    string msg = "病人皮膚感覺知覺為" + form["rb_perception"].Substring(0, form["rb_perception"].Length - 3) + "，"
                    + "潮溼程度為" + form["rb_damp"].Substring(0, form["rb_damp"].Length - 3) + "，"
                    + "活動力為" + form["rb_activity"].Substring(0, form["rb_activity"].Length - 3) + "，"
                    + "移動力為" + form["rb_moving"].Substring(0, form["rb_moving"].Length - 3) + "，"
                    + "營養狀態為" + form["rb_nutrition"].Substring(0, form["rb_nutrition"].Length - 3) + "，"
                    + "摩擦力/剪力為" + form["rb_friction"].Substring(0, form["rb_friction"].Length - 3) + "，"
                    + form["carerecord"] + "，"
                    + "目前" + form["rb_have"].ToString() + "壓傷。";

                    if (base.Insert_CareRecordTns(date, id, "壓傷危險評估", "", "", msg, "", "", "PRESSURE_SORE_DATA", ref link) > 0)
                    {
                        success = true;
                        if (form["rb_have"].ToString() == "有")
                            Response.Write("<script>if(confirm('新增成功!是否繼續填寫傷口護理紀錄表?')){window.location.href='../Wound/listTrauma';}else{window.location.href='List'};</script>");
                        else
                            Response.Write("<script>alert('新增成功!');window.location.href='List';</script>");
                    }
                    else
                    {
                        success = false;
                        Response.Write("<script>alert('新增失敗!');window.location.href='List';</script>");
                    }
                }
                else
                    Response.Write("<script>alert('新增失敗!');window.location.href='List';</script>");

                if (success)
                {
                    this.link.DBCommit();
                }
                else
                {
                    this.link.DBRollBack();
                }
            }
            catch (Exception ex)
            {
                if (ex.ToString() != "DBCommit Fail")
                {
                    this.link.DBRollBack();
                }

                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(),ex); Response.Write("<script>alert('新增失敗!');window.location.href='List';</script>");
            }
            finally
            {
                this.link.DBClose();
            }
            return new EmptyResult();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Upd_Pressure(FormCollection form)
        {
            bool success = false;

            try
            {
                string date = form["txt_day"] + " " + form["txt_time"];

                List<DBItem> insertDataList = new List<DBItem>();
                insertDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("RECORDTIME", date, DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("NUTRITION", form["rb_nutrition"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PERCEPTION", form["rb_perception"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("DAMP", form["rb_damp"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ACTIVITY", form["rb_activity"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MOVING", form["rb_moving"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FRICTION", form["rb_friction"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("HAVE", form["rb_have"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("TOTAL", form["total"], DBItem.DBDataType.Number));

                if (this.link.DBExecUpdateTns("NIS_PRESSURE_SORE_DATA", insertDataList, " PRESSURE_ID = '" + form["id"] + "' AND FEENO = '" + base.ptinfo.FeeNo + "' ") > 0)
                {
                    string msg = "病人皮膚感覺知覺為" + form["rb_perception"].Substring(0, form["rb_perception"].Length - 3) + "，"
                    + "潮溼程度為" + form["rb_damp"].Substring(0, form["rb_damp"].Length - 3) + "，"
                    + "活動力為" + form["rb_activity"].Substring(0, form["rb_activity"].Length - 3) + "，"
                    + "移動力為" + form["rb_moving"].Substring(0, form["rb_moving"].Length - 3) + "，"
                    + "營養狀態為" + form["rb_nutrition"].Substring(0, form["rb_nutrition"].Length - 3) + "，"
                    + "摩擦力/剪力為" + form["rb_friction"].Substring(0, form["rb_friction"].Length - 3) + "，"
                    + form["carerecord"] + "，"
                    + "目前" + form["rb_have"].ToString() + "壓傷。";

                    string[] id_type = form["id"].ToString().Split('_');
                    //if (id_type[2]== "ASSESS") //第一次入評資料 壓傷ID  TAG
                    //{
                    //    success = true;
                    //    Response.Write("<script>alert('更新成功!');window.location.href='List';</script>");
                    //}
                    //else
                    //{
                        if (base.Upd_CareRecord(date, form["id"], "壓傷危險評估", "", "", msg, "", "", "PRESSURE_SORE_DATA") > 0)
                        {
                            success = true;
                            Response.Write("<script>alert('更新成功!');window.location.href='List';</script>");
                        }
                        else
                        {
                            success = false;
                            Response.Write("<script>alert('更新失敗!');window.location.href='List';</script>");
                        }
                    //}                    
                }
                else
                    Response.Write("<script>alert('更新失敗!');window.location.href='List';</script>");

                if (success)
                {
                    this.link.DBCommit();
                }
                else
                {
                    this.link.DBRollBack();
                }
            }
            catch (Exception ex)
            {
                if (ex.ToString() != "DBCommit Fail")
                {
                    this.link.DBRollBack();
                }

                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(),ex); Response.Write("<script>alert('新增失敗!');window.location.href='List';</script>");
            }
            finally
            {
                this.link.DBClose();
            }

            return new EmptyResult();
        }

        [HttpPost]
        public string Del_Pressure(string PressureID)
        {
            string Msg = string.Empty;
            bool success = false;

            try
            {
                List<DBItem> dbItemList = new List<DBItem>();
                dbItemList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                dbItemList.Add(new DBItem("DELETED", base.userinfo.EmployeesNo, DBItem.DBDataType.String));

                if (this.link.DBExecUpdateTns("NIS_PRESSURE_SORE_DATA", dbItemList, " PRESSURE_ID = '" + PressureID + "' AND FEENO = '" + base.ptinfo.FeeNo + "' ") > 0)
                {
                    //if (base.Del_CareRecordTns(PressureID, "PRESSURE_SORE_DATA", ref link) > 0)
                    if (base.Del_CareRecord(PressureID, "PRESSURE_SORE_DATA") > 0)

                            success = true;
                }

                if (success)
                {
                    this.link.DBCommit();
                    Msg = "刪除成功！";
                }
                else
                {
                    this.link.DBRollBack();
                    Msg = "刪除失敗！";
                }

            }
            catch (Exception ex)
            {
                Msg = "刪除失敗！";
                if (ex.ToString() != "DBCommit Fail")
                {
                    this.link.DBRollBack();
                }
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(),ex);
                
            }
            finally
            {                
                link.DBClose();
            }

            return Msg;
        }
        
        private DataTable sel_pressure_data(string feeno, string userno, string id)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM NIS_PRESSURE_SORE_DATA WHERE 0 = 0 ";
            if (id != "")
                sql += "AND PRESSURE_ID = '" + id + "' ";
            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if (userno != "")
                sql += "AND CREATNO = '" + userno + "' ";

            sql += "AND DELETED IS NULL ORDER BY RECORDTIME DESC";

            this.link.DBExecSQL(sql, ref dt);
            return dt;
        }

    }
}