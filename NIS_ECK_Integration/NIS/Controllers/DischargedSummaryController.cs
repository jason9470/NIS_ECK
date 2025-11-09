using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using NIS.Models;

namespace NIS.Controllers
{
    public class DischargedSummaryController : BaseController
    {
        private CareRecord care_record_m;
        private TubeManager tubem;
        private Education edu_m;
        private DBConnector link;
        private CommData cd;
        private NurseCarePlanController ncp;

        public DischargedSummaryController()
        {
            this.care_record_m = new CareRecord();
            this.tubem = new TubeManager();
            this.edu_m = new Education();
            this.link = new DBConnector();
            this.cd = new CommData();
            this.ncp = new NurseCarePlanController();
        }

        /// <summary>
        /// 新出院護理摘要
        /// </summary>
        /// <remarks>2016/05/04 Vanda Add</remarks>
        [HttpGet]
        public ActionResult DischargedSummary(string pState)
        {
            //判斷有無病人session
            if (Session["PatInfo"] != null)
            {
                //判斷是否可填出院護理摘要 
                int Cnt = 0;
                if (pState != null && pState == "Y")
                {
                    Cnt = 1;
                    ViewBag.State = "Y";
                }
                else
                {
                    DataTable DtCnt = this.link.DBExecSQL(string.Format("SELECT COUNT(TYPE_ID) FROM NIS_SPECIAL_EVENT_DATA WHERE FEENO='{0}' AND TYPE_ID='6'", ptinfo.FeeNo));
                    if (DtCnt != null && DtCnt.Rows.Count > 0) Cnt = int.Parse(DtCnt.Rows[0][0].ToString().Trim());
                    ViewBag.State = Cnt > 0 ? "N" : "F";
                }
                ViewBag.DischargedCnt = Cnt;
                if (Cnt == 0)
                {
                    //入院日期
                    ViewBag.IpdDate = ptinfo.InDate;

                    //讀取未拔管管路資料           
                    ViewBag.DtTube = tubem.sel_tube(ptinfo.FeeNo, "", "", "", "");

                    //讀取未評值目標項目        
                    ViewBag.Dt_Master = ncp.Sel_User_Master(ptinfo.FeeNo);
                    ViewData["Dt_Goal"] = ncp.Sel_User_Goal(ptinfo.FeeNo);

                    //讀取未評值護理指導
                    ViewBag.DtGuide = edu_m.sel_health_education(ptinfo.FeeNo, "", "N");

                    //讀取指導下拉選單
                    ViewData["division"] = this.cd.getSelectItem("health_education", "division");
                    ViewData["branch_division"] = this.cd.getSelectItem("health_education", "branch_division");
                }
            }
            else
            {
                Response.Write("<script>alert('請重新選擇病患');</script>");
                return new EmptyResult();
            }
            return View();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult DischargeBulletin(FormCollection pForm)
        {
            int erow = 0;
            string TxtHospital = pForm["TxtHospital"] ?? "";
            string TxtAADHospital = pForm["TxtAADHospital"] ?? "";
            string RblAAD = pForm["RblAAD"] ?? "";
            string RblWay = pForm["RblWay"] ?? "";
            string date = pForm["txt_day"] + " " + pForm["txt_time"];
            string time = pForm["txt_time"];
            string ID = base.creatid("EVENT", userinfo.EmployeesNo, ptinfo.FeeNo, "0");
            List<DBItem> insertDataList = new List<DBItem>();
            List<DBItem> updateDataList = new List<DBItem>();

            //回寫護理計畫
            if (pForm["HfTargetPK"] != null && pForm["HfTargetPK"] != "")
            {
                string[] HfTargetPK = pForm["HfTargetPK"].Split(',');
                foreach (string RblName in HfTargetPK)
                {
                    string TargetVal = pForm[RblName];
                    string Record = pForm[RblName.Replace("CPRESULT", "Record")];
                    string TargetReason = TargetVal == "N" ? pForm[RblName.Replace("CPRESULT", "c_options")] : "";
                    string TargetReason2 = TargetVal == "N" ? "-" + pForm[RblName.Replace("CPRESULT", "c_options")] : "";
                    string CareRecordContent = "評值 " + Record.Split('|').GetValue(1).ToString() + "，目標" + Record.Split('|').GetValue(2).ToString();


                    if (RblWay == "死亡" || RblWay == "潛離")
                    {
                        TargetVal = "N";
                        TargetReason = RblWay;
                        TargetReason2 = "-" + RblWay;
                    }
                    CareRecordContent += TargetReason2 + "，結束原建立之照護計畫。 ";

                    string[] PK = RblName.Replace("CPRESULT", "|").Split('|');
                    updateDataList.Add(new DBItem("TARGETENDDATE", Convert.ToDateTime(date).ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                    updateDataList.Add(new DBItem("TARGETSTATUS", TargetVal, DBItem.DBDataType.String));
                    updateDataList.Add(new DBItem("ASSESS_ID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    updateDataList.Add(new DBItem("ASSESS_DATE", Convert.ToDateTime(date).ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                    updateDataList.Add(new DBItem("REASON", TargetReason.Trim(), DBItem.DBDataType.String));
                    erow += this.link.DBExecUpdate("CPTARGETDTL", updateDataList, string.Format("RECORDID='{0}' AND TARGETID='{1}'", PK[0], PK[1]));
                    this.link.DBExec(string.Format("UPDATE CAREPLANMASTER SET PLANENDDATE= to_date('{0}','yyyy/MM/dd hh24:mi:ss') WHERE FEENO='{1}' AND PLANENDDATE IS NULL", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), ptinfo.FeeNo));
                    //-----2016/06/21 將所有項目加上結束日期
                    this.link.DBExec(string.Format("UPDATE CPFEATUREDTL SET FEATUREENDDATE=to_date('{0}','yyyy/MM/dd hh24:mi:ss') WHERE RECORDID='{1}' AND FEATUREENDDATE IS NULL", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), PK[0]));
                    this.link.DBExec(string.Format("UPDATE CPRFDTL SET RELATEDFACTORSENDDATE= to_date('{0}','yyyy/MM/dd hh24:mi:ss') WHERE RECORDID='{1}' AND RELATEDFACTORSENDDATE IS NULL", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), PK[0]));
                    //this.link.DBExec(string.Format("UPDATE CPTARGETDTL SET TARGETENDDATE='{0}' WHERE RECORDID='{1}' AND TARGETENDDATE IS NULL", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), PK[0]));
                    this.link.DBExec(string.Format("UPDATE CPMEASUREDTL SET MEASUREENDDATE= to_date('{0}','yyyy/MM/dd hh24:mi:ss') WHERE RECORDID='{1}' AND MEASUREENDDATE IS NULL", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), PK[0]));
                    //-----
                    TargetVal = null;
                    TargetReason = null;
                    PK = null;
                    updateDataList.Clear();

                    //==帶護理紀錄
                    Insert_CareRecord(Convert.ToDateTime(date).ToString("yyyy/MM/dd HH:mm:58"), RblName + DateTime.Now.ToString("yyyyMMddHHmmss"), Record.Split('|').GetValue(0).ToString(), CareRecordContent, "", "", "", "", "CAREPLANMASTER");
                }
            }

            //回寫護理指導
            string HfGuideCnt = pForm["HfGuideCnt"] ?? "";
            if (HfGuideCnt != "" && int.Parse(HfGuideCnt) > 0)
            {
                int Cnt = int.Parse(HfGuideCnt);
                for (int y = 0; y < Cnt; y++)
                {
                    string Score = pForm["RblGuide_" + y.ToString()] ?? "";
                    if (RblWay == "死亡" || RblWay == "潛離") Score = RblWay;
                    string PK = pForm["HfGuidePK_" + y.ToString()] ?? "";
                    string Reason = pForm["TxtGuide_" + y.ToString()] ?? "";
                    string content = pForm["edutxt_" + y.ToString()] ?? "";
                    Score = Reason.Trim() != "" ? Score + "|" + Reason.Trim() : Score;
                    updateDataList.Add(new DBItem("SCORE_RESULT", Score, DBItem.DBDataType.String));
                    updateDataList.Add(new DBItem("SCORE_NAME", ptinfo.PatientName, DBItem.DBDataType.String));
                    updateDataList.Add(new DBItem("SCORENO", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    updateDataList.Add(new DBItem("SCORE_TIME", date, DBItem.DBDataType.DataTime));
                    erow += this.link.DBExecUpdate("HEALTH_EDUCATION_DATA", updateDataList, string.Format("EDU_ID='{0}' AND FEENO='{1}'", PK, ptinfo.FeeNo));
                    //==帶護理紀錄
                    if (RblWay != "死亡" && RblWay != "潛離")
                        this.Insert_CareRecord(Convert.ToDateTime(date).ToString("yyyy/MM/dd HH:mm:58"), PK + "_R", "護理指導評值", "", "", "", "", content + Score + "。", "Education");
                    Score = null;
                    PK = null;
                    Reason = null;
                    updateDataList.Clear();
                }
                updateDataList = null;
            }

            //拋轉特殊事件
            string Words = RblWay;
            if (RblWay == "死亡")
                Words = "Expired";
            else if (RblWay == "潛離")
                Words = "Escape";
            else if (RblWay == "轉院至")
                Words = RblWay + TxtHospital.Trim() + "醫院";

            insertDataList.Add(new DBItem("EVENT_ID", ID, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FEENO", ptinfo.FeeNo, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATNO", userinfo.EmployeesNo, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATTIME", date, DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("TYPE_ID", "6", DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CONTENT", Words + " at " + time, DBItem.DBDataType.String));
            erow += care_record_m.DBExecInsert("NIS_SPECIAL_EVENT_DATA", insertDataList);

            //拋轉護理紀錄
            base.Insert_CareRecord_Black(Convert.ToDateTime(date).ToString("yyyy/MM/dd HH:mm:59"), ID, (RblAAD + RblWay + (TxtHospital.Trim() != "" ? (TxtHospital.Trim() + "醫院") : "")), pForm["TxtSummary"], "", "", "", "");

            if (erow > 0)
                Response.Write("<script>alert('新增成功');location.href='DischargedSummary?pState=Y';</script>");
            else
                Response.Write("<script>alert('新增失敗');location.href='DischargedSummary?pState=N';</script>");

            return new EmptyResult();
        }

        //衛教項目列表
        public ActionResult PartialItemList(string mode, string column, string key)
        {
            Function func_m = new Function();
            ViewBag.mode = mode;
            ViewData["division"] = this.cd.getSelectItem("health_education", "division");
            if (column != null && key != null)
            {
                if (key != "")
                {
                    if (column == "CATEGORY_ID")
                        ViewBag.dt = func_m.sel_health_education_item(column, "", key);
                    else
                        ViewBag.dt = func_m.sel_health_education_item("", column, key);
                }
            }
            return View();
        }
    }
}
