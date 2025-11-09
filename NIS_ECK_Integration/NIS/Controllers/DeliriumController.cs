using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using NIS.Data;
using NIS.Models;
using NIS.UtilTool;
using NIS.WebService;
using System.Data;
using System;
using System.Collections.Generic;

namespace NIS.Controllers
{
    public class DeliriumController : BaseController
    {
        // GET: /Delirium/
        private DBConnector link;

        //患者譫妄評估
        public DeliriumController()
        {
            this.link = new DBConnector();
        }

        #region --查詢列表--
        public ActionResult index()
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
            string StrSql = "SELECT * FROM DELIRIUM_DATA WHERE FEE_NO='" + feeno + "' ";
            StrSql += "AND ASSESS_DT BETWEEN TO_DATE('" + Starttime + " 00:00:00','yyyy/mm/dd hh24:mi:ss') ";
            StrSql += "AND  TO_DATE('" + endtime + " 23:59:59','yyyy/mm/dd hh24:mi:ss') ";
            StrSql += "AND DEL_USER IS NULL AND STATUS = 'Y' ORDER BY  ASSESS_DT DESC, CREATE_DATE DESC";
            DataTable dt = this.link.DBExecSQL(StrSql);
            return dt;
        }
        #endregion

        #region --新增--
        public ActionResult Insert(string id)
        {
            if (id != null)
            {
                string feeno = ptinfo.FeeNo;
                DataTable dt = this.link.DBExecSQL("SELECT * FROM DELIRIUM_DATA WHERE FEE_NO='" + feeno + "' AND ASSESS_ID='" + id + "'");
                ViewBag.dt = dt;
            }
            return View();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Delirium_Save(FormCollection form)
        {
            string[] StrMoodScore = { "是", "否", "無法評估" };
            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;
            string date = form["txt_day"] + " " + form["txt_time"];
            string id = creatid("DELIRIUM_DATA", userno, feeno, "0");
            string type = "新增";
            int erow = '0';
            string reasonOther = "";
            string msg = "";
            if (form["id"] != "" && form["id"] != null)
                id = form["id"];
            #region 新增
            #region 護理紀錄
            reasonOther += form["rb_assessment_reason_other"];

            #endregion //護理紀錄
            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("ASSESS_DT", date, DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("BED_NO", ptinfo.BedNo, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("SCORE_1A", form["rb_acute_attack_1a"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("SCORE_1B", form["rb_acute_attack_1b"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("SCORE_2", form["rb_attention"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("SCORE_3", form["rb_ponder"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("SCORE_4", form["rb_consciousness"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("TOTAL_SCORE", form["hid_score"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("RESULT", form["hid_score_remark"], DBItem.DBDataType.String));

            //2023.07.19 Ryan
            string hid_score_remark = form["hid_score_remark"];
            bool isContain = hid_score_remark.Contains("有譫妄");
            if (isContain)
            {
                insertDataList.Add(new DBItem("DELIRIUM_RESULT", "Y", DBItem.DBDataType.String));
            }
            else if (form["hid_score_remark"] == "無法評估")
            {
                insertDataList.Add(new DBItem("DELIRIUM_RESULT", "E", DBItem.DBDataType.String));
            }
            else
            {
                insertDataList.Add(new DBItem("DELIRIUM_RESULT", "N", DBItem.DBDataType.String));
            }

            insertDataList.Add(new DBItem("DEL_USER", "", DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("ASSESSMENT_REASON", form["rb_assessment_reason"], DBItem.DBDataType.String));
            if (form["rb_assessment_reason"] == "其他") 
            {
                insertDataList.Add(new DBItem("REASON_OTHER", form["rb_assessment_reason_other"], DBItem.DBDataType.String));
            }
            #endregion //新增
            if (form["id"] != "" && form["id"] != null)
            { //修改
                type = "修改";
                var careRecordId = "";
                DataTable dt = new DataTable();
                var sql = "SELECT * FROM DELIRIUM_DATA WHERE ASSESS_ID='" + id + "' AND STATUS = 'Y'";
                this.link.DBExecSQL(sql, ref dt);
                if (dt.Rows.Count > 0)
                {
                    careRecordId = dt.Rows[0]["CARERECORD_ID"].ToString();
                }
                insertDataList.Add(new DBItem("MODIFY_USER", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MODIFY_USER_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MODIFY_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("STATUS", "M", DBItem.DBDataType.String));
                string where = "ASSESS_ID='" + id + "'";
                erow = this.link.DBExecUpdate("DELIRIUM_DATA", insertDataList, where);
                var reason = "";
                if (form["rb_assessment_reason"] != null)
                {
                    reason = form["rb_assessment_reason"];
                }
                if (reason == "其他")
                {
                    msg += "病人評核原因：" + reasonOther + "，評估項目：1.急性發作且病程波動：";
                }
                else
                {
                    msg += "病人評核原因：" + reason + "，評估項目：1.急性發作且病程波動：";
                }
                //1a
                msg += "1a.與平常相比較，有任何證據顯示病人精神狀態產生急性變化：";
                if (form["rb_acute_attack_1a"] != null && form["rb_acute_attack_1a"] == "0")
                {
                    msg += "是";
                }
                else if (form["rb_acute_attack_1a"] != null && form["rb_acute_attack_1a"] == "1")
                {
                    msg += "否";
                }
                else
                {
                    msg += "無法評估";
                }
                msg += "，";

                //1b
                msg += "1b.這些不正常的行為在一天中呈現波動狀態：";
                if (form["rb_acute_attack_1b"] != null && form["rb_acute_attack_1b"] == "0")
                {
                    msg += "是";
                }
                else if (form["rb_acute_attack_1b"] != null && form["rb_acute_attack_1b"] == "1")
                {
                    msg += "否";
                }
                else
                {
                    msg += "無法評估";
                }
                msg += "、";

                //2
                msg += "2.注意力不集中：";
                if (form["rb_attention"] != null && form["rb_attention"] == "0")
                {
                    msg += "是";
                }
                else if (form["rb_attention"] != null && form["rb_attention"] == "1")
                {
                    msg += "否";
                }
                else
                {
                    msg += "無法評估";
                }
                msg += "、";

                //3
                msg += "3.思考缺乏組織：";
                if (form["rb_ponder"] != null && form["rb_ponder"] == "0")
                {
                    msg += "是";
                }
                else if (form["rb_ponder"] != null && form["rb_ponder"] == "1")
                {
                    msg += "否";
                }
                else
                {
                    msg += "無法評估";
                }
                msg += "、";

                //4
                msg += "4.意識狀態改變：";
                if (form["rb_consciousness"] != null && form["rb_consciousness"] == "0")
                {
                    msg += "是";
                }
                else if (form["rb_consciousness"] != null && form["rb_consciousness"] == "1")
                {
                    msg += "否";
                }
                else
                {
                    msg += "無法評估";
                }
                msg += "。";

                //result
                msg += "評估結果：";
                if (form["hid_score_remark"] != null)
                {
                    msg += form["hid_score_remark"];
                }
                else if (form["hid_score_remark"] != null && form["hid_score_remark"] == "1")
                {
                    msg += "否";
                }
                else
                {
                    msg += "無法評估";
                }
                msg += "。";

                base.Upd_CareRecord(date, careRecordId, "譫妄評估", msg, "", "", "", "", "Delirium");

                id = creatid("DELIRIUM_DATA", userno, feeno, "0");
                insertDataList.Clear();
                insertDataList.Add(new DBItem("ASSESS_DT", date, DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("CARERECORD_ID", careRecordId, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("SCORE_1A", form["rb_acute_attack_1a"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("SCORE_1B", form["rb_acute_attack_1b"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("SCORE_2", form["rb_attention"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("SCORE_3", form["rb_ponder"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("SCORE_4", form["rb_consciousness"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("TOTAL_SCORE", form["hid_score"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("RESULT", form["hid_score_remark"], DBItem.DBDataType.String));

                //2023.07.19 Ryan
                if (isContain)
                {
                    insertDataList.Add(new DBItem("DELIRIUM_RESULT", "Y", DBItem.DBDataType.String));
                }
                else if (form["hid_score_remark"] == "無法評估")
                {
                    insertDataList.Add(new DBItem("DELIRIUM_RESULT", "E", DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("DELIRIUM_RESULT", "N", DBItem.DBDataType.String));
                }

                insertDataList.Add(new DBItem("DEL_USER", "", DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASSESSMENT_REASON", form["rb_assessment_reason"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("REASON_OTHER", reasonOther, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASSESS_ID", id, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FEE_NO", feeno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATE_USER", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATE_USER_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATE_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MODIFY_USER", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MODIFY_USER_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MODIFY_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                erow = this.link.DBExecInsert("DELIRIUM_DATA", insertDataList);


            }
            else
            {  //新增              
                insertDataList.Add(new DBItem("ASSESS_ID", id, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CARERECORD_ID", id, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FEE_NO", feeno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATE_USER", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATE_USER_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATE_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MODIFY_USER", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MODIFY_USER_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MODIFY_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                erow = this.link.DBExecInsert("DELIRIUM_DATA", insertDataList);
                var reason = "";
                if (form["rb_assessment_reason"] != null)
                {
                    reason = form["rb_assessment_reason"];
                }
                if(reason == "其他")
                {
                    msg += "病人評核原因：" + reasonOther + "，評估項目：1.急性發作且病程波動：";
                }
                else
                {
                    msg += "病人評核原因：" + reason + "，評估項目：1.急性發作且病程波動：";
                }
                //1a
                msg += "1a.與平常相比較，有任何證據顯示病人精神狀態產生急性變化：";
                if (form["rb_acute_attack_1a"] != null && form["rb_acute_attack_1a"] == "0")
                {
                    msg += "是";
                }
                else if (form["rb_acute_attack_1a"] != null && form["rb_acute_attack_1a"] == "1")
                {
                    msg += "否";
                }
                else
                {
                    msg += "無法評估";
                }
                msg += "，";

                //1b
                msg += "1b.這些不正常的行為在一天中呈現波動狀態：";
                if (form["rb_acute_attack_1b"] != null && form["rb_acute_attack_1b"] == "0")
                {
                    msg += "是";
                }
                else if (form["rb_acute_attack_1b"] != null && form["rb_acute_attack_1b"] == "1")
                {
                    msg += "否";
                }
                else
                {
                    msg += "無法評估";
                }
                msg += "、";

                //2
                msg += "2.注意力不集中：";
                if (form["rb_attention"] != null && form["rb_attention"] == "0")
                {
                    msg += "是";
                }
                else if (form["rb_attention"] != null && form["rb_attention"] == "1")
                {
                    msg += "否";
                }
                else
                {
                    msg += "無法評估";
                }
                msg += "、";

                //3
                msg += "3.思考缺乏組織：";
                if (form["rb_ponder"] != null && form["rb_ponder"] == "0")
                {
                    msg += "是";
                }
                else if (form["rb_ponder"] != null && form["rb_ponder"] == "1")
                {
                    msg += "否";
                }
                else
                {
                    msg += "無法評估";
                }
                msg += "、";

                //4
                msg += "4.意識狀態改變：";
                if (form["rb_consciousness"] != null && form["rb_consciousness"] == "0")
                {
                    msg += "是";
                }
                else if (form["rb_consciousness"] != null && form["rb_consciousness"] == "1")
                {
                    msg += "否";
                }
                else
                {
                    msg += "無法評估";
                }
                msg += "。";

                //result
                msg += "評估結果：";
                if (form["hid_score_remark"] != null)
                {
                    msg += form["hid_score_remark"];
                }
                else
                {
                    msg += "";
                }
                msg += "。";

                base.Insert_CareRecord(date, id, "譫妄評估", msg, "", "", "", "", "Delirium");
            }
            if (erow > 0)
                Response.Write("<script>window.location.href='index';</script>");
            else
                Response.Write("<script>alert('" + type + "失敗!');window.location.href='index';</script>");
            return new EmptyResult();
        }
        #endregion

        #region --刪除--
        [HttpPost]
        public JsonResult Del_MoodData(string id)
        {
            var careRecordId = "";
            DataTable dt = new DataTable();
            var sql = "SELECT * FROM DELIRIUM_DATA WHERE ASSESS_ID='" + id + "' AND STATUS = 'Y'";
            this.link.DBExecSQL(sql, ref dt);
            if (dt.Rows.Count > 0)
            {
                careRecordId = dt.Rows[0]["CARERECORD_ID"].ToString();
            }
            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;
            string where = " ASSESS_ID = '" + id + "' AND FEE_NO = '" + feeno + "' ";
            List<DBItem> DelDataList = new List<DBItem>();
            DelDataList.Add(new DBItem("DEL_USER", userno, DBItem.DBDataType.String));
            DelDataList.Add(new DBItem("MODIFY_USER", userno, DBItem.DBDataType.String));
            DelDataList.Add(new DBItem("MODIFY_USER_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
            DelDataList.Add(new DBItem("MODIFY_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
            DelDataList.Add(new DBItem("STATUS", "D", DBItem.DBDataType.String));
            int erow = this.link.DBExecUpdate("DELIRIUM_DATA", DelDataList, where);
            erow += base.Del_CareRecord(careRecordId, "Delirium");
            if (erow > 0)
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
            if (ByteCode != null)
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
