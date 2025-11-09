using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Mvc;
using NIS.Models;

namespace NIS.Controllers
{
    public class FallAssessController : BaseController
    {
        Assess ass_m = new Assess();

        public ActionResult ListIndex(string modeval = "")
        {
            if (Session["PatInfo"] != null)
            {
                //自動切換
                if (string.IsNullOrWhiteSpace(modeval))
                {
                    //兒童
                    if (ptinfo.Age < 17)
                        return RedirectToAction("List_Child");
                    //成人
                    else
                        return RedirectToAction("List");
                }
                return View();
            }
            else
            {
                Response.Write("<script>alert('登入逾時');</script>");
                return new EmptyResult();
            }
        }

        #region 成人跌倒

        //首頁
        public ActionResult List()
        {
            ViewBag.dt = ass_m.sel_fall_assess_data(base.ptinfo.FeeNo, "", "NIS_FALL_ASSESS_DATA");
            ViewBag.userno = base.userinfo.EmployeesNo;
            return View();
        }

        public ActionResult Detail(string Startdate = "", string Enddate = "")
        {
            if (string.IsNullOrWhiteSpace(Startdate))
                Startdate = DateTime.Now.AddDays(-3).ToString("yyyy/MM/dd");
            if (string.IsNullOrWhiteSpace(Enddate))
                Enddate = DateTime.Now.ToString("yyyy/MM/dd");

            ViewBag.StartDate = Startdate;
            ViewBag.EndDate = Enddate;
            ViewBag.dt = ass_m.sel_fall_assess_data_date(ptinfo.FeeNo, "", "NIS_FALL_ASSESS_DATA", Startdate + " 00:00:00", Enddate + " 23:59:59");

            return View();
        }

        //新增_更新頁面
        public ActionResult Insert(string id, string num)
        {
            Function func_m = new Function();
            //修改頁面
            if (id != null)
                ViewBag.dt = ass_m.sel_fall_assess_data(base.ptinfo.FeeNo, id, "NIS_FALL_ASSESS_DATA");
            //新增_帶舊資料
            else
                ViewBag.dt_new = ass_m.sel_fall_assess_data(base.ptinfo.FeeNo, "", "NIS_FALL_ASSESS_DATA");
            ViewBag.age = base.ptinfo.Age;
            ViewBag.num = num;
            ViewBag.FRIDs = (func_m.sel_FRIDs(ptinfo.FeeNo.Trim()) == true) ? "藥" : "";
            ViewData["MinDate"] = base.GetMinDate();
            ViewData["FallAssessMeasure"] = base.GetAdultFallAssessMeasure();
            return View();
        }

        //新增跌倒
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Insert_FallAssess(FormCollection form)
        {
            string date = form["txt_day"] + " " + form["txt_time"];
            string id = creatid("FALL", base.userinfo.EmployeesNo, base.ptinfo.FeeNo, "0");

            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("FALL_ID", id, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FEENO", base.ptinfo.FeeNo, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATNO", base.userinfo.EmployeesNo, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATNAME", base.userinfo.EmployeesName, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("RECORDTIME", date, DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("NUM", form["num"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("AGE", form["rb_age"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("ACTIVITY", form["rb_activity"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("COMMUNICATION", form["rb_communication"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CONSCIOUSNESS", form["rb_consciousness"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("DIZZINESS", form["rb_dizziness"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("EXCRETION", form["rb_excretion"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FALL", form["rb_fall"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("DRUG", form["rb_drug"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("TOTAL", form["total"], DBItem.DBDataType.Number));
            insertDataList.Add(new DBItem("REASON", form["rb_reason"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("PRECAUTION", form["cb_precaution"], DBItem.DBDataType.String));

            if (ass_m.DBExecInsert("NIS_FALL_ASSESS_DATA", insertDataList) > 0)
            {
                ass_m.DBExecDelete("DATA_NOTICE", " NT_ID = 'FALL_ADULT_" + base.ptinfo.FeeNo + "' AND FEE_NO = '" + base.ptinfo.FeeNo + "' ");
                string notice_day = ass_m.sel_last_fall_assess_time_And_total(base.ptinfo.FeeNo, "NIS_FALL_ASSESS_DATA");
                if (notice_day != "")
                    Do_FallNotice(base.ptinfo.FeeNo, "ADULT", notice_day, "9999/12/31 00:00:00");

                string content = string.Empty;
                if (int.Parse(form["total"]) > 2)
                {
                    content = "評估原因："+ form["rb_reason"] + "，評估病人跌倒危險因子分數為" + form["total"] + "，給予" + form["cb_precaution"] + "等護理措施，並持續追蹤病人狀況。";

                    string item_id = ass_m.sel_health_education_item(base.ptinfo.FeeNo, "FALL");
                    if (item_id != "")
                    {
                        insertDataList.Clear();
                        insertDataList.Add(new DBItem("EDU_ID", base.creatid("EDUCATION_DATA", base.userinfo.EmployeesNo, base.ptinfo.FeeNo, "0"), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("FEENO", base.ptinfo.FeeNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CREATNO", base.userinfo.EmployeesNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("RECORDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("ITEMID", item_id, DBItem.DBDataType.String));
                        ass_m.DBExecInsert("HEALTH_EDUCATION_DATA", insertDataList);
                    }
                }
                else
                    content = "評估原因：" + form["rb_reason"] + "，評估病人跌倒危險因子分數為" + form["total"] + "分，持續追蹤病人狀況。";

                base.Insert_CareRecord(date, id, "跌倒危險性評估", "", "", content, "", "", "FALL_ASSESS");

                Response.Write("<script>alert('新增成功!');window.location.href='List';</script>");
            }
            else
                Response.Write("<script>alert('新增失敗!');window.location.href='List';</script>");

            return new EmptyResult();
        }

        //更新跌倒
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Upd_FallAssess(FormCollection form)
        {
            string date = form["txt_day"] + " " + form["txt_time"];
            string id = form["id"];

            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("UPDNO", base.userinfo.EmployeesNo, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("UPDNAME", base.userinfo.EmployeesName, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("RECORDTIME", date, DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("AGE", form["rb_age"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("ACTIVITY", form["rb_activity"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("COMMUNICATION", form["rb_communication"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CONSCIOUSNESS", form["rb_consciousness"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("DIZZINESS", form["rb_dizziness"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("EXCRETION", form["rb_excretion"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FALL", form["rb_fall"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("DRUG", form["rb_drug"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("TOTAL", form["total"], DBItem.DBDataType.Number));
            insertDataList.Add(new DBItem("REASON", form["rb_reason"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("PRECAUTION", form["cb_precaution"], DBItem.DBDataType.String));

            if (ass_m.DBExecUpdate("NIS_FALL_ASSESS_DATA", insertDataList, " FEENO = '" + base.ptinfo.FeeNo + "' AND FALL_ID = '" + id + "'") > 0)
            {
                ass_m.DBExecDelete("DATA_NOTICE", " NT_ID = 'FALL_ADULT_" + base.ptinfo.FeeNo + "' AND FEE_NO = '" + base.ptinfo.FeeNo + "' ");
                string notice_day = ass_m.sel_last_fall_assess_time_And_total(base.ptinfo.FeeNo, "NIS_FALL_ASSESS_DATA");
                if (notice_day != "")
                    Do_FallNotice(base.ptinfo.FeeNo, "ADULT", notice_day, "9999/12/31 00:00:00");

                string content = "";
                if (int.Parse(form["total"]) > 2)
                    content = "評估病人跌倒危險因子分數為" + form["total"] + "，給予" + form["cb_precaution"] + "等護理措施，並持續追蹤病人狀況。";
                else
                    content = "評估病人跌倒危險因子分數為" + form["total"] + "分，持續追蹤病人狀況。";

                base.Upd_CareRecord(date, id, "跌倒危險性評估", "", "", content, "", "", "FALL_ASSESS");

                Response.Write("<script>alert('更新成功!');window.location.href='List';</script>");
            }
            else
                Response.Write("<script>alert('更新失敗!');window.location.href='List';</script>");

            return new EmptyResult();
        }

        //刪除跌倒
        [HttpPost]
        public string Del_FallAssess(string id)
        {
            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("UPDNO", base.userinfo.EmployeesNo, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("UPDNAME", base.userinfo.EmployeesName, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("DELETED", base.userinfo.EmployeesNo, DBItem.DBDataType.String));
            if (ass_m.DBExecUpdate("NIS_FALL_ASSESS_DATA", insertDataList, " FEENO = '" + base.ptinfo.FeeNo + "' AND FALL_ID = '" + id + "' ") > 0)
            {
                ass_m.DBExecDelete("DATA_NOTICE", " NT_ID = 'FALL_" + base.ptinfo.FeeNo + "' AND FEE_NO = '" + base.ptinfo.FeeNo + "' ");
                string notice_day = ass_m.sel_last_fall_assess_time_And_total(base.ptinfo.FeeNo, "NIS_FALL_ASSESS_DATA");
                if (notice_day != "")
                    Do_FallNotice(base.ptinfo.FeeNo, "ADULT", notice_day, "9999/12/31 00:00:00");
                base.Del_CareRecord(id, "FALL_ASSESS");
                return "刪除成功！";
            }
            else
                return "刪除失敗！";
        }

        #endregion

        #region 兒童評估

        //首頁
        public ActionResult List_Child()
        {
            ViewBag.dt = ass_m.sel_fall_assess_data(base.ptinfo.FeeNo, "", "NIS_FALL_ASSESS_DATA_CHILD");
            ViewBag.userno = base.userinfo.EmployeesNo;
            return View();
        }

        public ActionResult Deatil_Child(string Startdate = "", string Enddate = "")
        {
            if (string.IsNullOrWhiteSpace(Startdate))
                Startdate = DateTime.Now.AddDays(-3).ToString("yyyy/MM/dd");
            if (string.IsNullOrWhiteSpace(Enddate))
                Enddate = DateTime.Now.ToString("yyyy/MM/dd");

            ViewBag.StartDate = Startdate;
            ViewBag.EndDate = Enddate;
            ViewBag.dt = ass_m.sel_fall_assess_data_date(ptinfo.FeeNo, "", "NIS_FALL_ASSESS_DATA_CHILD", Startdate + " 00:00:00", Enddate + " 23:59:59");

            return View();
        }

        //新增_更新頁面
        public ActionResult Insert_Child(string id, string num)
        {
            //修改頁面
            if (id != null)
                ViewBag.dt = ass_m.sel_fall_assess_data(base.ptinfo.FeeNo, id, "NIS_FALL_ASSESS_DATA_CHILD");
            //新增_帶舊資料
            else
                ViewBag.dt_new = ass_m.sel_fall_assess_data(base.ptinfo.FeeNo, "", "NIS_FALL_ASSESS_DATA_CHILD");
            ViewBag.age = base.ptinfo.Age;
            ViewBag.PatientGender = ptinfo.PatientGender;
            ViewBag.num = num;
            ViewData["MinDate"] = base.GetMinDate();
            ViewData["FallAssessMeasure"] = base.GetChildFallAssessMeasure();
            return View();
        }
        //新增跌倒-兒童儲存
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Insert_FallAssess_Child(FormCollection form)
        {
            string userno = userinfo.EmployeesNo;
            string username = userinfo.EmployeesName;
            string feeno = ptinfo.FeeNo;
            string date = form["txt_day"] + " " + form["txt_time"];
            string id = creatid("FALL", userno, feeno, "0");
            int total = int.Parse(form["total"]);
            string Reason = form["rb_reason"];
            string Precaution = form["cb_precaution"];
            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("FALL_ID", id, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATNAME", username, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("RECORDTIME", date, DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("NUM", "", DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("AGE", form["rb_age"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("GENDER", form["rb_gender"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FITNESS_FALL", form["rb_fitness_fall"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FALL_HISTORY", form["rb_fall_history"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("ACTIVITY", form["rb_activity"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("DRUG", form["rb_drug"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("DISEASE", form["rb_disease"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("TOTAL", total.ToString(), DBItem.DBDataType.Number));
            insertDataList.Add(new DBItem("REASON", Reason, DBItem.DBDataType.String));
            if(Precaution != null)
            {
                insertDataList.Add(new DBItem("PRECAUTION", Precaution, DBItem.DBDataType.String));
            }
            int erow = ass_m.DBExecInsert("NIS_FALL_ASSESS_DATA_CHILD", insertDataList);
            if(erow > 0)
            {
                ass_m.DBExecDelete("DATA_NOTICE", " NT_ID = 'FALL_CHILD_" + feeno + "' AND FEE_NO = '" + feeno + "' ");
                string notice_day = ass_m.sel_last_fall_assess_time_And_total(feeno, "NIS_FALL_ASSESS_DATA_CHILD");
                if(notice_day != "")
                {
                    Do_FallNotice(feeno, "CHILD", notice_day, "9999/12/31 00:00:00");
                }
                string content = "";
                if(total >= 3)
                {
                    content = "評估原因："+ form["rb_reason"] + "，評估病人跌倒危險因子分數為" + total.ToString() + "，給予" + Precaution + "等護理措施，並持續追蹤病人狀況。";
                    string item_id = ass_m.sel_health_education_item(feeno, "FALL");
                    if(item_id != "")
                    {
                        string health_education_id = base.creatid("EDUCATION_DATA", userno, feeno, "0");
                        string health_education_date = DateTime.Now.ToString("yyyy/MM/dd HH:mm");
                        insertDataList.Clear();
                        insertDataList.Add(new DBItem("EDU_ID", health_education_id, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CREATTIME", health_education_date, DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("RECORDTIME", health_education_date, DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("ITEMID", item_id, DBItem.DBDataType.String));
                        ass_m.DBExecInsert("HEALTH_EDUCATION_DATA", insertDataList);
                    }
                }
                else
                {
                    content = "評估原因：" + form["rb_reason"] + "，評估病人跌倒危險因子分數為" + total.ToString() + "分，持續追蹤病人狀況。";
                }
                base.Insert_CareRecord(date, id, "跌倒危險性評估", "", "", content, "", "", "FALL_ASSESS");
                Response.Write("<script>alert('新增成功!');window.location.href='List_Child';</script>");
            }
            else
                Response.Write("<script>alert('新增失敗!');window.location.href='List_Child';</script>");
            return new EmptyResult();
        }

        //更新跌倒
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Upd_FallAssess_Child(FormCollection form)
        {
            string userno = userinfo.EmployeesNo;
            string username = userinfo.EmployeesName;
            string feeno = ptinfo.FeeNo;
            string date = form["txt_day"] + " " + form["txt_time"];
            string id = form["id"];
            int total = int.Parse(form["total"]);
            string Precaution = form["cb_precaution"];
            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("UPDNAME", username, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("RECORDTIME", date, DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("REASON", form["rb_reason"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("AGE", form["rb_age"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("GENDER", form["rb_gender"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FITNESS_FALL", form["rb_fitness_fall"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FALL_HISTORY", form["rb_fall_history"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("ACTIVITY", form["rb_activity"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("DRUG", form["rb_drug"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("DISEASE", form["rb_disease"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("TOTAL", total.ToString(), DBItem.DBDataType.Number));
            insertDataList.Add(new DBItem("PRECAUTION", Precaution, DBItem.DBDataType.String));
            string where = "FALL_ID = '" + id + "'";
            int erow = ass_m.DBExecUpdate("NIS_FALL_ASSESS_DATA_CHILD", insertDataList, where);
            if(erow > 0)
            {
                ass_m.DBExecDelete("DATA_NOTICE", " NT_ID = 'FALL_CHILD_" + feeno + "' AND FEE_NO = '" + feeno + "' ");
                string notice_day = ass_m.sel_last_fall_assess_time_And_total(feeno, "NIS_FALL_ASSESS_DATA_CHILD");
                if(notice_day != "")
                {
                    Do_FallNotice(feeno, "CHILD", notice_day, "9999/12/31 00:00:00");
                }
                string content = "";
                if(total >= 3)
                    content = "評估病人跌倒危險因子分數為" + total.ToString() + "，給予" + Precaution + "等護理措施，並持續追蹤病人狀況。";
                else
                    content = "評估病人跌倒危險因子分數為" + total.ToString() + "分，持續追蹤病人狀況。";
                base.Upd_CareRecord(date, id, "", "", "", content, "", "", "FALL_ASSESS");
                Response.Write("<script>alert('更新成功!');window.location.href='List_Child';</script>");
            }
            else
                Response.Write("<script>alert('更新失敗!');window.location.href='List_Child';</script>");
            return new EmptyResult();
        }

        //刪除跌倒
        [HttpPost]
        public ActionResult Del_FallAssess_Child(string id)
        {
            string userno = userinfo.EmployeesNo;
            string username = userinfo.EmployeesName;
            string feeno = ptinfo.FeeNo;
            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("DELETED", userno, DBItem.DBDataType.String));
            string where = "FALL_ID = '" + id + "' ";
            int erow = ass_m.DBExecUpdate("NIS_FALL_ASSESS_DATA_CHILD", insertDataList, where);
            if(erow > 0)
            {
                ass_m.DBExecDelete("DATA_NOTICE", " NT_ID = 'FALL_CHILD_" + feeno + "' AND FEE_NO = '" + feeno + "' ");
                string notice_day = ass_m.sel_last_fall_assess_time_And_total(feeno, "NIS_FALL_ASSESS_DATA_CHILD");
                if(notice_day != "")
                {
                    Do_FallNotice(feeno, "CHILD", notice_day, "9999/12/31 00:00:00");
                }
                base.Del_CareRecord(id, "FALL_ASSESS");
                Response.Write("<script>alert('刪除成功!');window.location.href='List_Child';</script>");
            }
            else
                Response.Write("<script>alert('刪除失敗!');window.location.href='List_Child';</script>");
            return new EmptyResult();
        }

        #endregion
        
        /// <summary> 
        /// 新增跑馬燈 
        /// 1.大於等於3分，每日進行危險性評估
        /// 2.小於3分，每7天進行危險性評估(範例:10/3 評估， 10/9再進行第二次評估)
        /// </summary>
        /// <param name="feeno"></param>
        /// <param name="AgeType">成人(ADULT)或兒童(CHILD)</param>
        /// <param name="startday">開始時間</param>
        /// <param name="endday">結束時間</param>
        /// <param name="ExecType">儲存的狀態(Insert)or(Update改不使用)</param>
        /// <returns></returns>
        public int Do_FallNotice(string feeno, string AgeType, string startday, string endday)
        {
            int erow = 0;
            if (!string.IsNullOrWhiteSpace(feeno) && !string.IsNullOrWhiteSpace(AgeType) && !string.IsNullOrWhiteSpace(startday) && !string.IsNullOrEmpty(endday))
            {
                if (startday.Split('|').Length > 1)
                {
                    List<DBItem> insertDataList = new List<DBItem>();
                    string memo = string.Empty;

                    if (int.Parse(startday.Split('|')[1]) >= 3)
                    {
                        memo = "患者跌倒評估大於3分，需每日進行跌倒危險性評估";
                        startday = Convert.ToDateTime(startday.Split('|')[0]).AddDays(1).ToString("yyyy/MM/dd 00:00:00");
                    }
                    else
                    {
                        memo = "患者跌倒評估已大於七日，請重新評估";
                        startday = Convert.ToDateTime(startday.Split('|')[0]).AddDays(7).ToString("yyyy/MM/dd 00:00:00");
                    }

                    insertDataList.Add(new DBItem("NT_ID", "FALL_" + AgeType + "_" + feeno, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("FEE_NO", feeno, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("MEMO", memo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("TIMEOUT", endday, DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("STARTTIME", startday, DBItem.DBDataType.DataTime));
                    erow = ass_m.DBExecInsert("DATA_NOTICE", insertDataList);
                }
            }
            return erow;
        }

    }
}
