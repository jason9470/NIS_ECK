using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using NIS.Models;
using NIS.Data;

namespace NIS.Controllers
{
    public class DischargePlanningServicesController : BaseController
    {
        private CommData cd;
        private DischargedCare dc;
        private DBConnector link;

        public DischargePlanningServicesController()
        {
            this.cd = new CommData();
            this.dc = new DischargedCare();
            this.link = new DBConnector();
        }

        public ActionResult DischargePlanningServices()
        {//判斷有無病人session
            if (Session["PatInfo"] != null)
            {
                ViewBag.dt = null;
                ViewBag.edit = 0;
                ViewBag.DisDt = this.dc.GetDischargedCareQueryDt(ptinfo.FeeNo);
                ViewBag.userno = userinfo.EmployeesNo;
                ViewBag.CaseSate = this.dc.GetCaseState(ptinfo.FeeNo);
            }
            else
            {
                Response.Write("<script>alert('請重新選擇病患');</script>");
                return new EmptyResult();
            }
            return View();
        }

        [HttpPost]
        public ActionResult QueryData(string pStart, string pEnd)
        {
            ViewBag.DisDt = this.dc.GetDischargedCareQueryDt(ptinfo.FeeNo, pStart, pEnd);
            ViewBag.userno = userinfo.EmployeesNo;
            return PartialView("ResultDischargedServices");
        }

        [HttpPost]
        public ActionResult EditData(int pType, string pId)
        {
            DataTable Dt = this.dc.GetDischargedCareEditDt(pId);
            ViewBag.dt = Dt;
            ViewBag.edit = pType;
            return PartialView("EditDischargedServices");
        }

        [HttpPost]
        public JsonResult DelData(string pId)
        {
            this.dc.DBExecDelete("DISCHARGED_CARE", string.Format("dc_id='{0}'", pId));
            base.Del_CareRecord(pId, "");
            return Json("Y");
        }

        [HttpPost]
        [ValidateInput(false)]
        public JsonResult Insert(FormCollection pForm, string pFilter)
        {
            int ERow = 0;
            int Edit = int.Parse(pForm["HfEdit"]);
            string Data = "";

            List<DBItem> DBItemDataList = new List<DBItem>();

            DBItemDataList.Add(new DBItem("assess_date", pForm["TxtAssessDate"], DBItem.DBDataType.String));
            DBItemDataList.Add(new DBItem("assess_time", pForm["TxtAssessTime"], DBItem.DBDataType.String));
            DBItemDataList.Add(new DBItem("filter_state", pForm["RblFilterState"] ?? "", DBItem.DBDataType.String));
            DBItemDataList.Add(new DBItem("filter_other", pForm["TxtFilterOther"] ?? "", DBItem.DBDataType.String));
            DBItemDataList.Add(new DBItem("op_date", pForm["TxtOpDate"] ?? "", DBItem.DBDataType.String));
            DBItemDataList.Add(new DBItem("daily_life", pForm["RblDailyLife"], DBItem.DBDataType.String));
            DBItemDataList.Add(new DBItem("skin_state", pForm["RblSkinState"], DBItem.DBDataType.String));
            DBItemDataList.Add(new DBItem("skin_wound", pForm["CblSkinWound"] ?? "", DBItem.DBDataType.String));
            DBItemDataList.Add(new DBItem("social_support", pForm["RblSocialSupport"], DBItem.DBDataType.String));
            DBItemDataList.Add(new DBItem("tube_request", pForm["RblTubeRequest"] ?? "", DBItem.DBDataType.String));
            DBItemDataList.Add(new DBItem("tube_type", pForm["CblTubeType"] ?? "", DBItem.DBDataType.String));
            DBItemDataList.Add(new DBItem("tube_other", pForm["TxtTubeOther"] ?? "", DBItem.DBDataType.String));
            DBItemDataList.Add(new DBItem("continence", pForm["RblContinence"] ?? "", DBItem.DBDataType.String));
            DBItemDataList.Add(new DBItem("usage_ventilator", pForm["RblUsageVentilator"] ?? "", DBItem.DBDataType.String));
            DBItemDataList.Add(new DBItem("usage_oxygen", pForm["RblUsageOxygen"] ?? "", DBItem.DBDataType.String));
            DBItemDataList.Add(new DBItem("momentus", pForm["RblMomentus"] ?? "", DBItem.DBDataType.String));
            DBItemDataList.Add(new DBItem("ltc_type", pForm["RblLtcType"] ?? "", DBItem.DBDataType.String));
            DBItemDataList.Add(new DBItem("total", pForm["TxtTotal"], DBItem.DBDataType.String));
            string ID = base.creatid("DISCHARGED_CARE", userinfo.EmployeesNo, ptinfo.FeeNo, "0");
            if (Edit == 0)
            {
                DBItemDataList.Add(new DBItem("fee_no", ptinfo.FeeNo, DBItem.DBDataType.String));
                DBItemDataList.Add(new DBItem("dc_id", ID, DBItem.DBDataType.String));
                DBItemDataList.Add(new DBItem("create_user", userinfo.EmployeesNo, DBItem.DBDataType.String));
                DBItemDataList.Add(new DBItem("create_user_name", userinfo.EmployeesName, DBItem.DBDataType.String));
                DBItemDataList.Add(new DBItem("create_date", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.String));
                ERow = this.link.DBExecInsert("DISCHARGED_CARE", DBItemDataList);
                ERow += base.Insert_CareRecord(pForm["TxtAssessDate"] + " " + pForm["TxtAssessTime"], ID, "出院準備服務篩選評估", "病人因" + pFilter + "，予出院準備篩選評估，結果為" + pForm["TxtTotal"] + "分，續追蹤出院準備小組收案情形。", "", "", "", "", "DISCHARGED_CARE");
            }
            else
            {
                DBItemDataList.Add(new DBItem("modify_user", userinfo.EmployeesNo, DBItem.DBDataType.String));
                DBItemDataList.Add(new DBItem("modify_user_name", userinfo.EmployeesName, DBItem.DBDataType.String));
                DBItemDataList.Add(new DBItem("modify_date", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.String));
                ERow = this.link.DBExecUpdate("DISCHARGED_CARE", DBItemDataList, string.Format("fee_no='{0}' AND dc_id='{1}'", ptinfo.FeeNo, pForm["HfDcId"]));
                ERow += base.Upd_CareRecord(pForm["TxtAssessDate"] + " " + pForm["TxtAssessTime"], pForm["HfDcId"], "出院準備服務篩選評估", "病人因" + pFilter + "，予出院準備篩選評估，結果為" + pForm["TxtTotal"] + "分，續追蹤出院準備小組收案情形。", "", "", "", "", "DISCHARGED_CARE");
            }

            if (ERow > 0)
            {
                Data = "Y|";
                if (Edit == 0) Data += ID;
            }
            else
                Data = "N|";

            return Json(Data);
        }

    }
}
