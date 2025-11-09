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
    public class PatientsMoodAssessController : BaseController
    {
        // GET: /PatientsMoodAssess/
        private DBConnector link;

        //患者心情評估
        public PatientsMoodAssessController()
        {
            this.link = new DBConnector();
        }

        #region --查詢列表--
        public ActionResult List()
        {//判斷有無病人session
            if (Session["PatInfo"] != null)
            {

                ViewBag.RootDocument = GetSourceUrl();
                ViewBag.feeno = ptinfo.FeeNo;
            }
            else
            {
                Response.Write("<script>alert('請重新選擇病患');</script>");
                return new EmptyResult();
            }
            return View();
        }

        public ActionResult ListData()
        {
            string feeno = ptinfo.FeeNo;
            ViewBag.dt = QueryData(feeno, Request["Pstart"], Request["Pend"]);
            return View();
        }

        private DataTable QueryData(string feeno, string Starttime, string endtime)
        {
            string StrSql = "SELECT * FROM MOOD_ASSESSMENT_DATA WHERE FEE_NO='" + feeno + "' ";
            StrSql += "AND ASSESS_DT BETWEEN TO_DATE('" + Starttime + " 00:00:00','yyyy/mm/dd hh24:mi:ss') ";
            StrSql += "AND  TO_DATE('" + endtime + " 23:59:59','yyyy/mm/dd hh24:mi:ss') ";
            StrSql += "AND DEL_USER IS NULL ORDER BY  ASSESS_DT";
            DataTable dt = this.link.DBExecSQL(StrSql);
            return dt;
        }
        #endregion

        #region --新增--
        public ActionResult Insert(string id)
        {
            if(id != null)
            {
                string feeno = ptinfo.FeeNo;
                DataTable dt = this.link.DBExecSQL("SELECT * FROM MOOD_ASSESSMENT_DATA WHERE FEE_NO='" + feeno + "' AND ASSESS_ID='" + id + "'");
                ViewBag.dt = dt;
            }
            return View();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Mood_Save(FormCollection form)
        {
            string[] StrMoodScore = { "完全沒有(0)", "輕微(1)", "中等程度(2)", "厲害(3)", "非常厲害(4)" };
            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;
            string date = form["txt_day"] + " " + form["txt_time"];
            string id = creatid("MOOD_ASSESSMENT_DATA", userno, feeno, "0");
            string Spirituality = "", TempSpirituality1 = "", TempSpirituality2 = "", TempSpirituality3 = "", TempSpirituality4 = "", type = "新增";
            int erow = '0';
            string msg = "";
            if(form["id"] != "" && form["id"] != null)
                id = form["id"];
            #region 新增
            #region 靈性評估
            string i_p = form["rb_social_issues_patient"];
            string i_f = form["rb_social_issues_family_member"];
            string r_p = form["rb_spirituality_religion_patient"];
            string r_f = form["rb_spirituality_religion_family_member"];
            Spirituality = i_p + "|" + i_f + "|" + r_p + "|" + r_f;

            if(form["cb_social_issues_patient_has"] != null)
                TempSpirituality1 = (form["cb_social_issues_patient_has"].IndexOf("其他") < 0) ? form["cb_social_issues_patient_has"]
                : form["cb_social_issues_patient_has"] + "|" + form["txt_social_issues_patient_has"];

            if(form["cb_social_issues_family_member_has"] != null)
                TempSpirituality2 = (form["cb_social_issues_family_member_has"].IndexOf("其他") < 0) ? form["cb_social_issues_family_member_has"]
                : form["cb_social_issues_family_member_has"] + "|" + form["txt_social_issues_family_member_has"];

            if(form["cb_spirituality_religion_patient_has"] != null)
                TempSpirituality3 = (form["cb_spirituality_religion_patient_has"].IndexOf("其他") < 0) ? form["cb_spirituality_religion_patient_has"]
                : form["cb_spirituality_religion_patient_has"] + "|" + form["txt_spirituality_religion_patient_has"];

            if(form["cb_spirituality_religion_family_member_has"] != null)
                TempSpirituality4 = (form["cb_spirituality_religion_family_member_has"].IndexOf("其他") < 0) ? form["cb_spirituality_religion_family_member_has"]
                : form["cb_spirituality_religion_family_member_has"] + "|" + form["txt_spirituality_religion_family_member_has"];

            #endregion//靈性評估
            #region 護理紀錄
            msg += "住院病友心情評估總分：" + form["hid_score"] + "分，" + form["hid_score_remark"];
            if(form["rb_suicidal_thoughts"] != "" && form["rb_suicidal_thoughts"] != null)
                msg += "自殺想法：" + StrMoodScore[Convert.ToInt32(form["rb_suicidal_thoughts"])] + "。";
            if(i_p == "無" && r_p == "無")
            {
                msg += "病人無社會及靈性宗教問題。";
            }
            else
            {
                if(i_p == "有" && r_p == "無")
                    msg += "病人有" + TempSpirituality1.Replace("|", ":") + "等社會問題，無靈性宗教問題。";
                if(i_p == "無" && r_p == "有")
                    msg += "病人有" + TempSpirituality3.Replace("|", ":") + "等靈性宗教問題，無社會問題。";
                if(i_p == "有" && r_p == "有")
                    msg += "病人有" + TempSpirituality1.Replace("|", ":") + "等社會問題，靈性宗教有" + TempSpirituality3.Replace("|", ":") + "問題。";
            }

            if(i_p == "無法評估" && r_p == "無法評估")
            {
                msg += "無法評估病人社會及靈性宗教問題。";
            }
            else
            {
                if(i_p == "無法評估" && r_p == "無")
                    msg += "無法評估病人是否有社會問題，靈性宗教無問題。";
                if(i_p == "無法評估" && r_p == "有")
                    msg += "無法評估病人是否有社會問題，靈性宗教有" + TempSpirituality3.Replace("|", ":") + "等問題。";
                if(i_p == "無" && r_p == "無法評估")
                    msg += "病人無社會問題，靈性宗教問題無法評估。";
                if(i_p == "有" && r_p == "無法評估")
                    msg += "病人有" + TempSpirituality1.Replace("|", ":") + "等社會問題，靈性宗教問題無法評估。";
            }

            if(i_f == "無" && r_f == "無")
            {
                msg += "家屬無社會及靈性宗教問題。";
            }
            else
            {
                if(i_f == "有" && r_f == "無")
                    msg += "家屬有" + TempSpirituality2.Replace("|", ":") + "等社會問題，無靈性宗教問題。";
                if(i_f == "無" && r_f == "有")
                    msg += "家屬有" + TempSpirituality4.Replace("|", ":") + "等靈性宗教問題，無社會問題。";
                if(i_f == "有" && r_f == "有")
                    msg += "家屬有" + TempSpirituality2.Replace("|", ":") + "等社會問題，靈性宗教有" + TempSpirituality4.Replace("|", ":") + "問題。";
            }

            if(i_f == "無法評估" && r_f == "無法評估")
            {
                msg += "無法評估家屬社會及靈性宗教問題。";
            }
            else
            {
                if(i_f == "無法評估" && r_f == "無")
                    msg += "無法評估家屬是否有社會問題，靈性宗教無問題。";
                if(i_f == "無法評估" && r_f == "有")
                    msg += "無法評估家屬是否有社會問題，靈性宗教有" + TempSpirituality4.Replace("|", ":") + "等問題。";
                if(i_f == "無" && r_f == "無法評估")
                    msg += "家屬無社會問題，靈性宗教問題無法評估。";
                if(i_f == "有" && r_f == "無法評估")
                    msg += "家屬有" + TempSpirituality2.Replace("|", ":") + "等社會問題，靈性宗教問題無法評估。";
            }
            if(!string.IsNullOrEmpty(form["label_remark_0"].Trim()))
            {
                msg += "備註：" + form["label_remark_0"] + "。";
            }
            #endregion //護理紀錄
            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("ASSESS_DT", date, DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("SCORE_1", form["rb_jittery"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("SCORE_2", form["rb_distress_flare"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("SCORE_3", form["rb_gloomy"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("SCORE_4", form["rb_feeling_failure"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("SCORE_5", form["rb_difficulty_sleeping"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("SUICIDAL_THOUGHTS_SCORE", form["rb_suicidal_thoughts"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("TOTAL_SCORE", form["hid_score"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("SPIRITUALITY", Spirituality, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("SOCIETY_PT", TempSpirituality1, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("SOCIETY_FM", TempSpirituality2, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("RELIGION_PT", TempSpirituality3, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("RELIGION_FM", TempSpirituality4, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("MEASURE", form["ta_measure"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("REMARK", form["label_remark_0"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("DEL_USER", "", DBItem.DBDataType.String));
            #endregion //新增
            if(form["id"] != "" && form["id"] != null)
            { //修改
                type = "修改";
                insertDataList.Add(new DBItem("MODIFY_USER", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MODIFY_USER_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MODIFY_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.String));
                string where = "ASSESS_ID='" + id + "'";
                erow = this.link.DBExecUpdate("MOOD_ASSESSMENT_DATA", insertDataList, where);
                base.Upd_CareRecord(date, id, "病友心情評估", msg, "", "", form["ta_measure"], "", "Mood");
            }
            else
            {  //新增              
                insertDataList.Add(new DBItem("ASSESS_ID", id, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FEE_NO", feeno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATE_USER", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATE_USER_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATE_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MODIFY_USER", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MODIFY_USER_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MODIFY_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.String));
                erow = this.link.DBExecInsert("MOOD_ASSESSMENT_DATA", insertDataList);
                base.Insert_CareRecord(date, id, "病友心情評估", msg, "", "", form["ta_measure"], "", "Mood");
            }
            if(erow > 0)
                Response.Write("<script>alert('" + type + "成功!');window.location.href='List';</script>");
            else
                Response.Write("<script>alert('" + type + "失敗!');window.location.href='List';</script>");
            return new EmptyResult();
        }
        #endregion

        #region --刪除--
        [HttpPost]
        public JsonResult Del_MoodData(string id)
        {
            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;
            string where = " ASSESS_ID = '" + id + "' AND FEE_NO = '" + feeno + "' ";
            List<DBItem> DelDataList = new List<DBItem>();
            DelDataList.Add(new DBItem("DEL_USER", userno, DBItem.DBDataType.String));
            DelDataList.Add(new DBItem("MODIFY_USER", userno, DBItem.DBDataType.String));
            DelDataList.Add(new DBItem("MODIFY_USER_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
            DelDataList.Add(new DBItem("MODIFY_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.String));
            int erow = this.link.DBExecUpdate("MOOD_ASSESSMENT_DATA", DelDataList, where);
            erow += base.Del_CareRecord(id, "Mood");
            if(erow > 0)
                return Json(1);
            else
                return Json(0);
        }
        #endregion

        #region --列印--
        public ActionResult PrintList(string feeno, string StartDate = "", string EndDate = "")
        {
            PatientInfo pinfo = new PatientInfo();
            byte[] ByteCode = webService.GetPatientInfo(feeno);
            //病人資訊
            if(ByteCode != null)
                pinfo = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(ByteCode));
            ViewData["ptinfo"] = pinfo;
            ViewBag.StartDate = StartDate;
            ViewBag.EndDate = EndDate;
            ViewBag.FeeNo = feeno;
            ViewBag.RootDocument = GetSourceUrl();
            DataTable dt = QueryData(feeno, StartDate, EndDate);
            ViewBag.dt = dt;
            return View();
        }
        #endregion
    }
}
