using Newtonsoft.Json;
using NIS.Data;
using NIS.Models;
using NIS.UtilTool;
using NIS.WebService;
using System;
using System.IO;
using Com.Mayaminer;
using System.Data;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using System.Web;
using System.Text;
using System.Web.Mvc;
using ClosedXML.Excel;
using System.Runtime.InteropServices;

namespace NIS.Controllers
{
    public class FunctionController : BaseController
    {
        private CommData cd;
        private DBConnector link;
        private DBConnector2 link2;
        private LogTool log;
        private Function func_m;
        private Assess ass_m;
        private TubeManager tubem;
        private ConstraintsAssessment ca;
        private Wound wound;

        public FunctionController()
        {
            this.cd = new CommData();
            this.link = new DBConnector();
            this.link2 = new DBConnector2();
            this.log = new LogTool();
            this.func_m = new Function();
            this.tubem = new TubeManager();
            this.ca = new ConstraintsAssessment();
            this.ass_m = new Assess();
            this.wound = new Wound();
        }

        public ActionResult Index()
        {
            return View();
        }

        #region 急診權限
        [HttpGet]
        public ActionResult ER_Permissions()
        {
            string sql = "SELECT ";
            sql += "  case when a.MO_ID  is NULL then 'N' else 'Y' end as CHECKED, a.MO_ID, b.BUTTON_NAME ";
            sql += " from SYS_USER_MODELS a , (SELECT MO_ID,BUTTON_NAME from SYS_MODELS where MO_ID <> 'E01' ) b ";
            sql += " where a.EMP_NO = 'ER_Administrator' and a.MO_ID = b.MO_ID  and a.MO_ID <> 'E01'  ";
            ViewBag.function_list = this.link.DBExecSQL(sql);
            return View();
        }
        [HttpPost]
        public ActionResult ER_Permissions(string mo_id)
        {
            List<DBItem> insertDataList = new List<DBItem>();
            this.link.DBExec("DELETE FROM SYS_USER_MODELS where EMP_NO = 'ER_Administrator' and MO_ID <> 'E01' ");
            for (int i = 0; i < mo_id.Split(',').Length; i++)
            {
                insertDataList.Clear();
                insertDataList.Add(new DBItem("MO_ID", mo_id.Split(',').GetValue(i).ToString(), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EMP_NO", "ER_Administrator", DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("SET_DEFAULT_PAGE", "N", DBItem.DBDataType.String));
                link.DBExecInsert("SYS_USER_MODELS", insertDataList);
            }
            return new EmptyResult();
        }
        #endregion

        #region 報表

        //報表首頁 
        public ActionResult MonthStatments()
        {
            List<CostCenterList> costlist = new List<CostCenterList>();
            //護理站列表
            byte[] listByteCode = webService.GetCostCenterList();
            if (listByteCode != null)
                costlist = JsonConvert.DeserializeObject<List<CostCenterList>>(CompressTool.DecompressString(listByteCode));

            ViewData["costlist"] = costlist;
            return View();
        }

        [HttpPost]
        public ActionResult AssessAbstainDetail(string OptionList)
        {
            List<Dictionary<string, string>> dt_ = new List<Dictionary<string, string>>();
            Dictionary<string, string> temp = null;
            Dictionary<string, string> Option = JsonConvert.DeserializeObject<Dictionary<string, string>>(OptionList);
            List<string> CostCode = JsonConvert.DeserializeObject<List<string>>(Option["CostCode"]);
            List<UserInfo> CostCodeUserList = new List<UserInfo>();
            byte[] TempByte = null;
            bool op1 = Option["AssessAbstainSpecial"].Contains("抽菸"), op2 = Option["AssessAbstainSpecial"].Contains("檳榔");

            try
            {
                foreach (string CostCode_ in CostCode)
                {
                    TempByte = webService.GetUserList(CostCode_);
                    if (TempByte != null)
                        CostCodeUserList.AddRange(JsonConvert.DeserializeObject<List<UserInfo>>(CompressTool.DecompressString(TempByte)));
                }
                if (CostCodeUserList.Count > 0)
                {
                    List<Dictionary<string, string>> tempDt = new List<Dictionary<string, string>>();

                    string sql = "SELECT * FROM ASSESSMENTMASTER WHERE MODIFYUSER IN('09277'";
                    foreach (UserInfo userNo in CostCodeUserList)
                        sql += ",'" + userNo.EmployeesNo.Trim() + "' ";
                    sql += ") " + "AND MODIFYTIME >= to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') "
                        + "AND MODIFYTIME <= to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') "
                        + "AND STATUS NOT IN ('temporary', 'delete') AND DELETED IS NULL AND NATYPE = 'A' "
                        + "ORDER BY CREATETIME ";

                    DataTable Dt = ass_m.DBExecSQL(sql);
                    if (Dt.Rows.Count > 0)
                    {
                        PatientInfo pi = new PatientInfo();
                        string feeno = "";
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            if (feeno != Dt.Rows[i]["FEENO"].ToString())
                            {
                                feeno = Dt.Rows[i]["FEENO"].ToString();
                                TempByte = webService.GetPatientInfo(feeno);
                                if (TempByte != null)
                                    pi = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(TempByte));
                            }
                            temp = new Dictionary<string, string>();
                            temp["costCode"] = pi.CostCenterName;
                            temp["ptName"] = pi.PatientName;
                            temp["ChartNo"] = pi.ChartNo;
                            temp["age"] = pi.Age.ToString();
                            temp["deptName"] = pi.DeptName;
                            temp["icd"] = pi.ICD9_code1;
                            temp["TABLEID"] = Dt.Rows[i]["TABLEID"].ToString();
                            tempDt.Add(temp);
                        }
                    }
                    if (tempDt.Count > 0)
                    {
                        List<Dictionary<string, string>> tempDt_;

                        foreach (var item in tempDt)
                        {
                            tempDt_ = new List<Dictionary<string, string>>();
                            sql = "SELECT * FROM ASSESSMENTDETAIL WHERE TABLEID = '" + item["TABLEID"] + "' "
                                + "AND ITEMID IN ('param_cigarette', 'param_cigarette_tutor', 'param_areca', 'param_areca_tutor') ";

                            Dt = ass_m.DBExecSQL(sql);
                            if (Dt.Rows.Count > 0)
                            {
                                for (int i = 0; i < Dt.Rows.Count; i++)
                                {
                                    temp = new Dictionary<string, string>();
                                    temp["ITEMID"] = Dt.Rows[i]["ITEMID"].ToString();
                                    temp["ITEMVALUE"] = Dt.Rows[i]["ITEMVALUE"].ToString();
                                    tempDt_.Add(temp);
                                }
                            }
                            temp = new Dictionary<string, string>();
                            temp["costCode"] = item["costCode"];
                            temp["ptName"] = item["ptName"];
                            temp["ChartNo"] = item["ChartNo"];
                            temp["age"] = item["age"];
                            temp["deptName"] = item["deptName"];
                            temp["icd"] = item["icd"];
                            if (op1)
                            {
                                temp["param_cigarette"] = "";
                                temp["param_cigarette_tutor"] = "";
                                if (tempDt_.Exists(x => x["ITEMID"] == "param_cigarette"))
                                {
                                    temp["param_cigarette"] = tempDt_.Find(x => x["ITEMID"] == "param_cigarette")["ITEMVALUE"]
                                        .Replace("1", "不抽")
                                        .Replace("3", "已戒菸")
                                        .Replace("4", "偶爾")
                                        .Replace("2", "抽");
                                    if ((tempDt_.Find(x => x["ITEMID"] == "param_cigarette")["ITEMVALUE"] == "4" || tempDt_.Find(x => x["ITEMID"] == "param_cigarette")["ITEMVALUE"] == "2") && tempDt_.Exists(x => x["ITEMID"] == "param_cigarette_tutor"))
                                        temp["param_cigarette_tutor"] = tempDt_.Find(x => x["ITEMID"] == "param_cigarette_tutor")["ITEMVALUE"];
                                }
                            }
                            if (op2)
                            {
                                temp["param_areca"] = "";
                                temp["param_areca_tutor"] = "";
                                if (tempDt_.Exists(x => x["ITEMID"] == "param_areca"))
                                {
                                    temp["param_areca"] = tempDt_.Find(x => x["ITEMID"] == "param_areca")["ITEMVALUE"]
                                        .Replace("1", "不吃")
                                        .Replace("3", "已戒")
                                        .Replace("4", "偶爾")
                                        .Replace("2", "有");

                                    if ((tempDt_.Find(x => x["ITEMID"] == "param_areca")["ITEMVALUE"] == "4" || tempDt_.Find(x => x["ITEMID"] == "param_areca")["ITEMVALUE"] == "2") && tempDt_.Exists(x => x["ITEMID"] == "param_areca_tutor"))
                                        temp["param_areca_tutor"] = tempDt_.Find(x => x["ITEMID"] == "param_areca_tutor")["ITEMVALUE"];
                                }
                            }
                            dt_.Add(temp);
                        }
                    }
                }

                List<string> Title = new List<string> { "單位", "病人姓名", "病歷號", "年齡", "科別", "診斷" };
                if (op1)
                {
                    Title.Add("抽菸情形");
                    Title.Add("勸戒輔導");
                }
                if (op2)
                {
                    Title.Add("檳榔情形");
                    Title.Add("勸戒輔導");
                }
                List<Dictionary<string, string>> Search_Title = new List<Dictionary<string, string>>();
                temp = new Dictionary<string, string>();
                temp["Name"] = "搜尋區間";
                temp["Value"] = Option["StartDate"] + "-" + Option["EndDate"];
                Search_Title.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Name"] = "單位";
                temp["Value"] = string.Join("、", JsonConvert.DeserializeObject<List<string>>(Option["CostName"]).ToArray());
                Search_Title.Add(temp);

                response(Title, Search_Title, dt_, "健康促進勸戒明細表");
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }

            return new EmptyResult();
        }

        [HttpPost]
        public ActionResult AssessAbstainStatistics(string OptionList)
        {
            try
            {
                Dictionary<string, string> Option = JsonConvert.DeserializeObject<Dictionary<string, string>>(OptionList);
                List<string> CostCode = JsonConvert.DeserializeObject<List<string>>(Option["CostCode"]);
                List<UserInfo> CostCodeUserList = new List<UserInfo>();
                byte[] TempByte = null;
                bool op1 = Option["AssessAbstainSpecial"].Contains("抽菸"), op2 = Option["AssessAbstainSpecial"].Contains("檳榔");

                List<Dictionary<string, string>> dt_ = new List<Dictionary<string, string>>();
                Dictionary<string, string> temp = null;
                temp = new Dictionary<string, string>();
                temp["Title"] = "勸戒人數";
                temp["Num_cigarette"] = "0";
                temp["Num_areca"] = "0";
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Title"] = "使用人數";
                temp["Num_cigarette"] = "0";
                temp["Num_areca"] = "0";
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Title"] = "勸戒率(%)";
                temp["Num_cigarette"] = "0";
                temp["Num_areca"] = "0";
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Title"] = "備註";
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Title"] = "勸戒人數定義:有執行勸戒人數";
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Title"] = "使用人數定義:偶爾人數+有使用人數";
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Title"] = "勸戒率定義:(有執行勸戒人數/(偶爾人數+有使用人數))*100";
                dt_.Add(temp);

                foreach (string CostCode_ in CostCode)
                {
                    TempByte = webService.GetUserList(CostCode_);
                    if (TempByte != null)
                        CostCodeUserList.AddRange(JsonConvert.DeserializeObject<List<UserInfo>>(CompressTool.DecompressString(TempByte)));
                }
                if (CostCodeUserList.Count > 0)
                {

                    List<Dictionary<string, string>> tempDt = new List<Dictionary<string, string>>();

                    string sql = "SELECT * FROM ASSESSMENTMASTER WHERE MODIFYUSER IN(''";
                    foreach (UserInfo userNo in CostCodeUserList)
                        sql += ",'" + userNo.EmployeesNo.Trim() + "' ";
                    sql += ") " + "AND MODIFYTIME >= to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') "
                        + "AND MODIFYTIME <= to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') "
                        + "AND STATUS NOT IN ('temporary', 'delete') AND DELETED IS NULL AND NATYPE = 'A' "
                        + "ORDER BY CREATETIME ";
                    DataTable Dt = ass_m.DBExecSQL(sql);

                    if (Dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            temp = new Dictionary<string, string>();
                            temp["TABLEID"] = Dt.Rows[i]["TABLEID"].ToString();
                            temp["param_cigarette"] = "";
                            temp["param_cigarette_tutor"] = "";
                            temp["param_areca"] = "";
                            temp["param_areca_tutor"] = "";
                            tempDt.Add(temp);
                        }
                    }
                    if (tempDt.Count > 0)
                    {
                        foreach (var item in tempDt)
                        {
                            sql = "SELECT * FROM ASSESSMENTDETAIL WHERE TABLEID = '" + item["TABLEID"] + "' "
                                + "AND ITEMID IN ('param_cigarette', 'param_cigarette_tutor', 'param_areca', 'param_areca_tutor') ";
                            Dt = ass_m.DBExecSQL(sql);
                            if (Dt.Rows.Count > 0)
                            {
                                for (int i = 0; i < Dt.Rows.Count; i++)
                                {
                                    item[Dt.Rows[i]["ITEMID"].ToString()] = Dt.Rows[i]["ITEMVALUE"].ToString();
                                }
                            }
                            if (!string.IsNullOrWhiteSpace(item["param_cigarette"]) && (item["param_cigarette"] == "4" || item["param_cigarette"] == "2"))
                            {
                                dt_[1]["Num_cigarette"] = (int.Parse(dt_[1]["Num_cigarette"]) + 1).ToString();
                                if (!string.IsNullOrWhiteSpace(item["param_cigarette_tutor"]) && item["param_cigarette_tutor"] == "有")
                                    dt_[0]["Num_cigarette"] = (int.Parse(dt_[0]["Num_cigarette"]) + 1).ToString();
                            }
                            if (!string.IsNullOrWhiteSpace(item["param_areca"]) && (item["param_areca"] == "4" || item["param_areca"] == "2"))
                            {
                                dt_[1]["Num_areca"] = (int.Parse(dt_[1]["Num_areca"]) + 1).ToString();
                                if (!string.IsNullOrWhiteSpace(item["param_areca_tutor"]) && item["param_areca_tutor"] == "有")
                                    dt_[0]["Num_areca"] = (int.Parse(dt_[0]["Num_areca"]) + 1).ToString();
                            }
                        }
                        if (dt_[1]["Num_cigarette"] != "0")
                            dt_[2]["Num_cigarette"] = (Math.Round((float.Parse(dt_[0]["Num_cigarette"]) / int.Parse(dt_[1]["Num_cigarette"])), 2) * 100).ToString() + "%";
                        if (dt_[1]["Num_areca"] != "0")
                            dt_[2]["Num_areca"] = (Math.Round((float.Parse(dt_[0]["Num_areca"]) / int.Parse(dt_[1]["Num_areca"])), 2) * 100).ToString() + "%";

                    }
                    foreach (var item in dt_)
                    {
                        if (!op1 && item.ContainsKey("Num_cigarette"))
                            item.Remove("Num_cigarette");
                        if (!op2 && item.ContainsKey("Num_areca"))
                            item.Remove("Num_areca");
                    }
                }

                List<string> Title = new List<string>();
                Title.Add("");
                if (op1)
                    Title.Add("抽菸");
                if (op2)
                    Title.Add("檳榔");
                List<Dictionary<string, string>> Search_Title = new List<Dictionary<string, string>>();
                temp = new Dictionary<string, string>();
                temp["Name"] = "搜尋區間";
                temp["Value"] = Option["StartDate"] + "-" + Option["EndDate"];
                Search_Title.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Name"] = "單位";
                temp["Value"] = string.Join("、", JsonConvert.DeserializeObject<List<string>>(Option["CostName"]).ToArray());
                Search_Title.Add(temp);

                response(Title, Search_Title, dt_, "健康促進勸戒統計表");
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return new EmptyResult();
            }
        }

        [HttpPost]
        public ActionResult AssessLifeDetail(string OptionList)
        {
            try
            {
                List<Dictionary<string, string>> dt_ = new List<Dictionary<string, string>>();
                Dictionary<string, string> temp = null;
                Dictionary<string, string> Option = JsonConvert.DeserializeObject<Dictionary<string, string>>(OptionList);
                List<string> CostCode = JsonConvert.DeserializeObject<List<string>>(Option["CostCode"]);
                List<UserInfo> CostCodeUserList = new List<UserInfo>();
                byte[] TempByte = null;
                bool op1 = Option["AssessLifeSpecial"].Contains("抽菸")
                    , op2 = Option["AssessLifeSpecial"].Contains("喝酒")
                    , op3 = Option["AssessLifeSpecial"].Contains("檳榔");

                foreach (string CostCode_ in CostCode)
                {
                    TempByte = webService.GetUserList(CostCode_);
                    if (TempByte != null)
                        CostCodeUserList.AddRange(JsonConvert.DeserializeObject<List<UserInfo>>(CompressTool.DecompressString(TempByte)));
                }
                if (CostCodeUserList.Count > 0)
                {
                    List<Dictionary<string, string>> tempDt = new List<Dictionary<string, string>>();

                    string sql = "SELECT * FROM ASSESSMENTMASTER WHERE MODIFYUSER IN(''";
                    foreach (UserInfo userNo in CostCodeUserList)
                        sql += ",'" + userNo.EmployeesNo.Trim() + "' ";
                    sql += ") " + "AND MODIFYTIME >= to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') "
                        + "AND MODIFYTIME <= to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') "
                        + "AND STATUS NOT IN ('temporary', 'delete') AND DELETED IS NULL AND NATYPE = 'A' "
                        + "ORDER BY CREATETIME ";


                    DataTable Dt = ass_m.DBExecSQL(sql);

                    if (Dt.Rows.Count > 0)
                    {
                        PatientInfo pi = new PatientInfo();
                        string feeno = "";
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            if (feeno != Dt.Rows[i]["FEENO"].ToString())
                            {
                                feeno = Dt.Rows[i]["FEENO"].ToString();
                                TempByte = webService.GetPatientInfo(feeno);
                                if (TempByte != null)
                                    pi = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(TempByte));
                            }
                            temp = new Dictionary<string, string>();
                            temp["costCode"] = pi.CostCenterName;
                            temp["ptName"] = pi.PatientName;
                            temp["ChartNo"] = pi.ChartNo;
                            temp["age"] = pi.Age.ToString();
                            temp["deptName"] = pi.DeptName;
                            temp["icd"] = pi.ICD9_code1;
                            temp["TABLEID"] = Dt.Rows[i]["TABLEID"].ToString();
                            tempDt.Add(temp);
                        }
                    }
                    if (tempDt.Count > 0)
                    {
                        List<Dictionary<string, string>> tempDt_;

                        foreach (var item in tempDt)
                        {
                            tempDt_ = new List<Dictionary<string, string>>();
                            sql = "SELECT * FROM ASSESSMENTDETAIL WHERE TABLEID = '" + item["TABLEID"] + "' "
                                + "AND ITEMID IN ('param_cigarette', 'param_drink', 'param_areca') ";
                            Dt = ass_m.DBExecSQL(sql);
                            if (Dt.Rows.Count > 0)
                            {
                                for (int i = 0; i < Dt.Rows.Count; i++)
                                {
                                    temp = new Dictionary<string, string>();
                                    temp["ITEMID"] = Dt.Rows[i]["ITEMID"].ToString();
                                    temp["ITEMVALUE"] = Dt.Rows[i]["ITEMVALUE"].ToString();
                                    tempDt_.Add(temp);
                                }
                            }
                            temp = new Dictionary<string, string>();
                            temp["costCode"] = item["costCode"];
                            temp["ptName"] = item["ptName"];
                            temp["ChartNo"] = item["ChartNo"];
                            temp["age"] = item["age"];
                            temp["deptName"] = item["deptName"];
                            temp["icd"] = item["icd"];
                            if (op1 && tempDt_.Exists(x => x["ITEMID"] == "param_cigarette"))
                            {
                                temp["param_cigarette"] = tempDt_.Find(x => x["ITEMID"] == "param_cigarette")["ITEMVALUE"]
                                    .Replace("1", "不抽")
                                    .Replace("3", "已戒菸")
                                    .Replace("4", "偶爾")
                                    .Replace("2", "抽");
                            }
                            if (op2 && tempDt_.Exists(x => x["ITEMID"] == "param_drink"))
                            {
                                temp["param_drink"] = tempDt_.Find(x => x["ITEMID"] == "param_drink")["ITEMVALUE"]
                                    .Replace("1", "不喝")
                                    .Replace("3", "已戒酒")
                                    .Replace("4", "偶爾")
                                    .Replace("2", "大量");
                            }
                            if (op3 && tempDt_.Exists(x => x["ITEMID"] == "param_areca"))
                            {
                                temp["param_areca"] = tempDt_.Find(x => x["ITEMID"] == "param_areca")["ITEMVALUE"]
                                    .Replace("1", "不吃")
                                    .Replace("3", "已戒")
                                    .Replace("4", "偶爾")
                                    .Replace("2", "有");
                            }
                            dt_.Add(temp);
                        }
                    }
                }

                List<string> Title = new List<string>() { "單位", "病人姓名", "病歷號", "年齡", "科別", "診斷" };
                if (op1)
                    Title.Add("抽菸情形");
                if (op2)
                    Title.Add("喝酒情形");
                if (op3)
                    Title.Add("檳榔情形");
                List<Dictionary<string, string>> Search_Title = new List<Dictionary<string, string>>();
                temp = new Dictionary<string, string>();
                temp["Name"] = "搜尋區間";
                temp["Value"] = Option["StartDate"] + "-" + Option["EndDate"];
                Search_Title.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Name"] = "單位";
                temp["Value"] = string.Join("、", JsonConvert.DeserializeObject<List<string>>(Option["CostName"]).ToArray());
                Search_Title.Add(temp);

                response(Title, Search_Title, dt_, "病人生活習慣明細表");
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return new EmptyResult();
            }
        }

        [HttpPost]
        public ActionResult CarePlanDetail(string OptionList)
        {
            try
            {
                List<Dictionary<string, string>> dt_ = new List<Dictionary<string, string>>();
                Dictionary<string, string> temp = null;
                Dictionary<string, string> Option = JsonConvert.DeserializeObject<Dictionary<string, string>>(OptionList);
                List<string> CostCode = JsonConvert.DeserializeObject<List<string>>(Option["CostCode"]);
                List<UserInfo> CostCodeUserList = new List<UserInfo>();
                byte[] TempByte = null;

                //foreach (string CostCode_ in CostCode)
                //{
                //    TempByte = webService.GetUserList(CostCode_);
                //    if (TempByte != null)
                //        CostCodeUserList.AddRange(JsonConvert.DeserializeObject<List<UserInfo>>(CompressTool.DecompressString(TempByte)));
                //}
                //   if (CostCodeUserList.Count > 0)
                //{
                //string sql = "SELECT * FROM CAREPLANMASTER WHERE RECORDER IN(''";
                //foreach (UserInfo userNo in CostCodeUserList)
                //    sql += ",'" + userNo.EmployeesNo.Trim() + "' ";
                //sql += ") " + "AND PLANSTARTDATE >= to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') "
                //    + "AND PLANSTARTDATE <= to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') "
                //    + "ORDER BY PLANSTARTDATE ";

                string sql = "SELECT * FROM CAREPLANMASTER,v_clerk_data WHERE TRIM(RECORDER) =TRIM(v_clerk_data.CLERK_ID) AND v_clerk_data.DEPT_NO IN(''";
                foreach (string CostCode_ in CostCode)
                    sql += ",'" + CostCode_.Trim() + "' ";
                sql += ") " + "AND PLANSTARTDATE >= to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') "
                    + "AND PLANSTARTDATE <= to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') "
                    + "ORDER BY PLANSTARTDATE ";
                DataTable Dt = ass_m.DBExecSQL(sql);
                if (Dt.Rows.Count > 0)
                {
                    PatientInfo pi = new PatientInfo();
                    string feeno = "";
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        if (feeno != Dt.Rows[i]["FEENO"].ToString())
                        //if (feeno != reader["FEENO"].ToString())
                        {
                            //feeno = reader["FEENO"].ToString();
                            feeno = Dt.Rows[i]["FEENO"].ToString();
                            TempByte = webService.GetPatientInfo(feeno);
                            if (TempByte != null)
                                pi = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(TempByte));
                        }
                        temp = new Dictionary<string, string>();
                        //temp["costCode"] = reader["DEPT_NAME"].ToString();//pi.CostCenterName;
                        temp["costCode"] = Dt.Rows[i]["DEPT_NAME"].ToString();//pi.CostCenterName;
                        temp["ptName"] = pi.PatientName;
                        temp["ChartNo"] = pi.ChartNo;
                        temp["age"] = pi.Age.ToString();
                        temp["deptName"] = pi.DeptName;
                        temp["icd"] = pi.ICD9_code1;
                        //temp["TOPICDESC"] = reader["TOPICDESC"].ToString();
                        temp["TOPICDESC"] = Dt.Rows[i]["TOPICDESC"].ToString();
                        dt_.Add(temp);
                    }
                }

                //}

                List<string> Title = new List<string> { "單位", "病人姓名", "病歷號", "年齡", "科別", "診斷"
                                 , "護理診斷名稱" };
                List<Dictionary<string, string>> Search_Title = new List<Dictionary<string, string>>();
                temp = new Dictionary<string, string>();
                temp["Name"] = "搜尋區間";
                temp["Value"] = Option["StartDate"] + "-" + Option["EndDate"];
                Search_Title.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Name"] = "單位";
                temp["Value"] = string.Join("、", JsonConvert.DeserializeObject<List<string>>(Option["CostName"]).ToArray());
                Search_Title.Add(temp);

                response(Title, Search_Title, dt_, "護理診斷明細表");
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return new EmptyResult();
            }
        }

        [HttpPost]
        public ActionResult CarePlanStatistics(string OptionList)
        {
            try
            {
                List<Dictionary<string, string>> dt_ = new List<Dictionary<string, string>>();
                Dictionary<string, string> temp = null;
                Dictionary<string, string> Option = JsonConvert.DeserializeObject<Dictionary<string, string>>(OptionList);
                List<string> CostCode = JsonConvert.DeserializeObject<List<string>>(Option["CostCode"]);
                List<UserInfo> CostCodeUserList = new List<UserInfo>();
                byte[] TempByte = null;

                foreach (string CostCode_ in CostCode)
                {
                    TempByte = webService.GetUserList(CostCode_);
                    if (TempByte != null)
                        CostCodeUserList.AddRange(JsonConvert.DeserializeObject<List<UserInfo>>(CompressTool.DecompressString(TempByte)));
                }
                if (CostCodeUserList.Count > 0)
                {
                    //string sql = "SELECT DISTINCT(TOPICID), TOPICDESC, COUNT(*) AS NUM "
                    //    + "FROM CAREPLANMASTER "
                    //    + "WHERE RECORDER IN(''";
                    //foreach (UserInfo userNo in CostCodeUserList)
                    //    sql += ",'" + userNo.EmployeesNo.Trim() + "' ";
                    //sql += ") " + "AND PLANSTARTDATE >= to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') "
                    //    + "AND PLANSTARTDATE <= to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') "
                    //    + "GROUP BY (TOPICID, TOPICDESC) ORDER BY NUM DESC ";

                    string sql = "SELECT DISTINCT(TOPICID), TOPICDESC, COUNT(*) AS NUM "
                   + "FROM CAREPLANMASTER,v_clerk_data "
                   + "WHERE  TRIM(RECORDER) =TRIM(v_clerk_data.CLERK_ID) AND v_clerk_data.DEPT_NO IN(''";
                    foreach (string CostCode_ in CostCode)
                        sql += ",'" + CostCode_.Trim() + "' ";
                    sql += ") " + "AND PLANSTARTDATE >= to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') "
                        + "AND PLANSTARTDATE <= to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') "
                        + "GROUP BY (TOPICID, TOPICDESC) ORDER BY NUM DESC ";
                    DataTable Dt = ass_m.DBExecSQL(sql);

                    if (Dt.Rows.Count > 0)
                    {
                        PatientInfo pi = new PatientInfo();
                        string Row = "";
                        int Row_ = 0;
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            if (Row != Dt.Rows[i]["NUM"].ToString())
                            {
                                Row = Dt.Rows[i]["NUM"].ToString();
                                Row_++;
                            }
                            temp = new Dictionary<string, string>();
                            temp["ROW"] = Row_.ToString();
                            //temp["TOPICDESC"] = reader["TOPICDESC"].ToString();
                            //temp["NUM"] = reader["NUM"].ToString();
                            temp["TOPICDESC"] = Dt.Rows[i]["TOPICDESC"].ToString();
                            temp["NUM"] = Dt.Rows[i]["NUM"].ToString();
                            dt_.Add(temp);
                        }
                    }

                }

                List<string> Title = new List<string> { "排名", "護理診斷名稱", "使用次數" };
                List<Dictionary<string, string>> Search_Title = new List<Dictionary<string, string>>();
                temp = new Dictionary<string, string>();
                temp["Name"] = "搜尋區間";
                temp["Value"] = Option["StartDate"] + "-" + Option["EndDate"];
                Search_Title.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Name"] = "單位";
                temp["Value"] = string.Join("、", JsonConvert.DeserializeObject<List<string>>(Option["CostName"]).ToArray());
                Search_Title.Add(temp);

                response(Title, Search_Title, dt_, "護理診斷統計表");
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return new EmptyResult();
            }
        }

        [HttpPost]
        public ActionResult CarePlan_Detail(string OptionList)
        {
            try
            {
                List<Dictionary<string, string>> dt_ = new List<Dictionary<string, string>>();
                Dictionary<string, string> temp = null;
                Dictionary<string, string> Option = JsonConvert.DeserializeObject<Dictionary<string, string>>(OptionList);
                List<string> CostCode = JsonConvert.DeserializeObject<List<string>>(Option["CostCode"]);
                List<UserInfo> CostCodeUserList = new List<UserInfo>();
                byte[] TempByte = null;
                bool op1 = Option["CarePlan_Special"].Contains("定義性特徵")
                    , op2 = Option["CarePlan_Special"].Contains("相關因素")
                    , op3 = Option["CarePlan_Special"].Contains("目標")
                    , op4 = Option["CarePlan_Special"].Contains("措施");

                foreach (string CostCode_ in CostCode)
                {
                    TempByte = webService.GetUserList(CostCode_);
                    if (TempByte != null)
                        CostCodeUserList.AddRange(JsonConvert.DeserializeObject<List<UserInfo>>(CompressTool.DecompressString(TempByte)));
                }
                if (CostCodeUserList.Count > 0)
                {
                    //                string where = "RECORDER IN(''";
                    //                foreach (UserInfo userNo in CostCodeUserList)
                    //                    where += ",'" + userNo.EmployeesNo.Trim() + "' ";
                    //                where += ") " + "AND PLANSTARTDATE >= to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') "
                    //                    + "AND PLANSTARTDATE <= to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') "
                    //                    + @"AND TOPICID IN (SELECT DIAGNOSIS_CODE FROM (
                    //                    SELECT DIAGNOSIS_CODE
                    //                    ,(SELECT COUNT(*) FROM CAREPLANMASTER WHERE PLANENDDATE IS NULL AND TOPICID = DIAGNOSIS_CODE) AS USE_NUM 
                    //                    FROM NIS_SYS_DIAGNOSIS
                    //                    WHERE DISABLE_DATE IS NULL
                    //                    ORDER BY USE_NUM DESC
                    //                    ) WHERE rownum <=10 )";

                    string where = " TRIM(RECORDER) =TRIM(v_clerk_data.CLERK_ID) AND v_clerk_data.DEPT_NO IN(''";
                    foreach (string CostCode_ in CostCode)
                        where += ",'" + CostCode_.Trim() + "' ";
                    where += ") " + "AND PLANSTARTDATE >= to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') "
                        + "AND PLANSTARTDATE <= to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') "
                        + @"AND TOPICID IN (SELECT DIAGNOSIS_CODE FROM (
                        SELECT DIAGNOSIS_CODE
                        ,(SELECT COUNT(*) FROM CAREPLANMASTER WHERE PLANENDDATE IS NULL AND TOPICID = DIAGNOSIS_CODE) AS USE_NUM 
                        FROM NIS_SYS_DIAGNOSIS
                        WHERE DISABLE_DATE IS NULL
                        ORDER BY USE_NUM DESC
                        ) WHERE rownum <=10 )";

                    List<Dictionary<string, string>> TempDt = new List<Dictionary<string, string>>();
                    DataTable Dt = ass_m.DBExecSQL("SELECT * FROM CAREPLANMASTER,v_clerk_data WHERE " + where + "ORDER BY PLANSTARTDATE ");
                    if (Dt.Rows.Count > 0)
                    {
                        PatientInfo pi = new PatientInfo();
                        string feeno = "";
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            if (feeno != Dt.Rows[i]["FEENO"].ToString())
                            {
                                feeno = Dt.Rows[i]["FEENO"].ToString();
                                TempByte = webService.GetPatientInfo(feeno);
                                if (TempByte != null)
                                    pi = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(TempByte));
                            }
                            temp = new Dictionary<string, string>();
                            temp["costCode"] = Dt.Rows[i]["DEPT_NAME"].ToString();//pi.CostCenterName;
                            temp["ptName"] = pi.PatientName;
                            temp["ChartNo"] = pi.ChartNo;
                            temp["age"] = pi.Age.ToString();
                            temp["deptName"] = pi.DeptName;
                            temp["icd"] = pi.ICD9_code1;
                            temp["RECORDID"] = Dt.Rows[i]["RECORDID"].ToString();
                            temp["TOPICDESC"] = Dt.Rows[i]["TOPICDESC"].ToString();
                            TempDt.Add(temp);
                        }
                    }
                    if (TempDt.Count > 0)
                    {
                        List<User_Care_Plan_Item> User_Defin = new List<User_Care_Plan_Item>();
                        List<User_Care_Plan_Item> User_About = new List<User_Care_Plan_Item>();
                        List<User_Care_Plan_Item> User_Goal = new List<User_Care_Plan_Item>();
                        List<User_Care_Plan_Item> User_Active = new List<User_Care_Plan_Item>();
                        User_Care_Plan_Item User_Item_Temp;
                        List<User_Care_Plan_Item> User_Temp;

                        string sql = "";

                        if (op1)
                        {
                            sql = "SELECT * FROM CPFEATUREDTL WHERE RECORDID IN ("
                                + "SELECT RECORDID FROM CAREPLANMASTER,v_clerk_data WHERE " + where + ") ";
                            Dt = ass_m.DBExecSQL(sql);
                            if (Dt.Rows.Count > 0)
                            {
                                for (int i = 0; i < Dt.Rows.Count; i++)
                                {
                                    User_Item_Temp = new User_Care_Plan_Item();
                                    User_Item_Temp.M_ID = Dt.Rows[i]["recordid"].ToString();
                                    User_Item_Temp.D_ID = Dt.Rows[i]["featureid"].ToString();
                                    User_Item_Temp.Item = Dt.Rows[i]["featuredesc"].ToString();
                                    User_Item_Temp.Custom = Dt.Rows[i]["custom"].ToString();
                                    User_Item_Temp.RecordTime = Dt.Rows[i]["featurestartdate"].ToString();
                                    User_Item_Temp.StopDate = Dt.Rows[i]["FEATUREENDDATE"].ToString();
                                    User_Defin.Add(User_Item_Temp);
                                }
                            }
                        }

                        if (op2)
                        {
                            sql = "SELECT * FROM CPRFDTL WHERE RECORDID IN ("
                                 + "SELECT RECORDID FROM CAREPLANMASTER,v_clerk_data WHERE " + where + ") ";
                            Dt = ass_m.DBExecSQL(sql);
                            if (Dt.Rows.Count > 0)
                            {
                                for (int i = 0; i < Dt.Rows.Count; i++)
                                {
                                    User_Item_Temp = new User_Care_Plan_Item();
                                    User_Item_Temp.M_ID = Dt.Rows[i]["recordid"].ToString();
                                    User_Item_Temp.A_ID = Dt.Rows[i]["relatedfactorsid"].ToString();
                                    User_Item_Temp.Item = Dt.Rows[i]["relatedfactorsdesc"].ToString();
                                    User_Item_Temp.Custom = Dt.Rows[i]["custom"].ToString();
                                    User_Item_Temp.RecordTime = Dt.Rows[i]["relatedfactorsstartdate"].ToString();
                                    User_Item_Temp.StopDate = Dt.Rows[i]["RELATEDFACTORSENDDATE"].ToString();
                                    User_About.Add(User_Item_Temp);
                                }
                            }
                        }

                        if (op3)
                        {
                            sql = "SELECT * FROM CPTARGETDTL WHERE RECORDID IN ("
                                 + "SELECT RECORDID FROM CAREPLANMASTER,v_clerk_data WHERE " + where + ") ";
                            Dt = ass_m.DBExecSQL(sql);

                            if (Dt.Rows.Count > 0)
                            {
                                for (int i = 0; i < Dt.Rows.Count; i++)
                                {
                                    User_Item_Temp = new User_Care_Plan_Item();
                                    User_Item_Temp.M_ID = Dt.Rows[i]["recordid"].ToString();
                                    User_Item_Temp.G_PK_ID = Dt.Rows[i]["targetid"].ToString();
                                    User_Item_Temp.Item = Dt.Rows[i]["targetdesc"].ToString();
                                    User_Item_Temp.Content = Dt.Rows[i]["targetdesc"].ToString();
                                    User_Item_Temp.Custom = Dt.Rows[i]["custom"].ToString();
                                    User_Item_Temp.ScoreTime = Dt.Rows[i]["targetstartdate"].ToString();
                                    User_Goal.Add(User_Item_Temp);
                                }
                            }
                        }

                        if (op4)
                        {
                            sql = "SELECT * FROM CPMEASUREDTL WHERE RECORDID IN ("
                                 + "SELECT RECORDID FROM CAREPLANMASTER,v_clerk_data WHERE " + where + ") ";
                            Dt = ass_m.DBExecSQL(sql);
                            if (Dt.Rows.Count > 0)
                            {
                                for (int i = 0; i < Dt.Rows.Count; i++)
                                {
                                    User_Item_Temp = new User_Care_Plan_Item();
                                    User_Item_Temp.M_ID = Dt.Rows[i]["recordid"].ToString();
                                    User_Item_Temp.I_ID = Dt.Rows[i]["measureid"].ToString();
                                    User_Item_Temp.Item = Dt.Rows[i]["measuredesc"].ToString();
                                    User_Item_Temp.Custom = Dt.Rows[i]["custom"].ToString();
                                    User_Active.Add(User_Item_Temp);
                                }
                            }
                        }

                        foreach (var item in TempDt)
                        {
                            temp = new Dictionary<string, string>();
                            temp["costCode"] = item["costCode"];
                            temp["ptName"] = item["ptName"];
                            temp["ChartNo"] = item["ChartNo"];
                            temp["age"] = item["age"];
                            temp["deptName"] = item["deptName"];
                            temp["icd"] = item["icd"];
                            temp["TOPICDESC"] = item["TOPICDESC"];
                            if (op1)
                            {
                                temp["User_Defin"] = "";
                                User_Temp = User_Defin.FindAll(x => x.M_ID == item["RECORDID"]);
                                if (User_Temp != null && User_Temp.Count > 0)
                                    foreach (User_Care_Plan_Item planItem in User_Temp)
                                        temp["User_Defin"] += planItem.Item.Replace("<", "＜").Replace(">", " ＞") + "、";
                            }
                            if (op2)
                            {
                                temp["User_About"] = "";
                                User_Temp = User_About.FindAll(x => x.M_ID == item["RECORDID"]);
                                if (User_Temp != null && User_Temp.Count > 0)
                                    foreach (User_Care_Plan_Item planItem in User_Temp)
                                        temp["User_About"] += planItem.Item.Replace("<", "＜").Replace(">", " ＞") + "、";
                            }
                            if (op3)
                            {
                                temp["User_Goal"] = "";
                                User_Temp = User_Goal.FindAll(x => x.M_ID == item["RECORDID"]);
                                if (User_Temp != null && User_Temp.Count > 0)
                                    foreach (User_Care_Plan_Item planItem in User_Temp)
                                        temp["User_Goal"] += planItem.Item.Replace("<", "＜").Replace(">", " ＞") + "、";
                            }
                            if (op4)
                            {
                                temp["User_Active"] = "";
                                User_Temp = User_Active.FindAll(x => x.M_ID == item["RECORDID"]);
                                if (User_Temp != null && User_Temp.Count > 0)
                                    foreach (User_Care_Plan_Item planItem in User_Temp)
                                        temp["User_Active"] += planItem.Item.Replace("<", "＜").Replace(">", " ＞") + "、";
                            }
                            dt_.Add(temp);
                        }
                    }
                }

                List<string> Title = new List<string>() { "單位", "病人姓名", "病歷號", "年齡", "科別", "診斷"
                , "護理診斷名稱" };
                if (op1)
                    Title.Add("定義性特徵");
                if (op2)
                    Title.Add("相關因素");
                if (op3)
                    Title.Add("目標");
                if (op4)
                    Title.Add("護理措施");
                List<Dictionary<string, string>> Search_Title = new List<Dictionary<string, string>>();
                temp = new Dictionary<string, string>();
                temp["Name"] = "搜尋區間";
                temp["Value"] = Option["StartDate"] + "-" + Option["EndDate"];
                Search_Title.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Name"] = "單位";
                temp["Value"] = string.Join("、", JsonConvert.DeserializeObject<List<string>>(Option["CostName"]).ToArray());
                Search_Title.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Name"] = "類別";
                if (op1)
                    temp["Value"] = "定義性特徵";
                if (op2)
                    temp["Value"] = "相關因素";
                if (op3)
                    temp["Value"] = "目標";
                if (op4)
                    temp["Value"] = "護理措施";
                Search_Title.Add(temp);

                response(Title, Search_Title, dt_, "護理診斷對應相關明細表");
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return new EmptyResult();
            }
        }

        [HttpPost]
        public ActionResult EducationItemDetail(string OptionList)
        {
            try
            {
                List<Dictionary<string, string>> dt_ = new List<Dictionary<string, string>>();
                Dictionary<string, string> temp = null;
                Dictionary<string, string> Option = JsonConvert.DeserializeObject<Dictionary<string, string>>(OptionList);
                List<string> CostCode = JsonConvert.DeserializeObject<List<string>>(Option["CostCode"]);
                List<UserInfo> CostCodeUserList = new List<UserInfo>();
                byte[] TempByte = null;

                foreach (string CostCode_ in CostCode)
                {
                    TempByte = webService.GetUserList(CostCode_);
                    if (TempByte != null)
                        CostCodeUserList.AddRange(JsonConvert.DeserializeObject<List<UserInfo>>(CompressTool.DecompressString(TempByte)));
                }
                if (CostCodeUserList.Count > 0)
                {

                    string sql = "SELECT HEALTH_EDUCATION_DATA.*, v_clerk_data.*,"
                        + "(SELECT NAME FROM HEALTH_EDUCATION_ITEM_DATA WHERE HEALTH_EDUCATION_ITEM_DATA.ITEM_ID = HEALTH_EDUCATION_DATA.ITEMID)ITEM_NAME "
                    //    + "FROM HEALTH_EDUCATION_DATA WHERE CREATNO IN(''";
                    //foreach (UserInfo userNo in CostCodeUserList)
                    //    sql += ",'" + userNo.EmployeesNo.Trim() + "' ";

                    + "FROM  HEALTH_EDUCATION_DATA,v_clerk_data  WHERE TRIM(CREATNO) =TRIM(v_clerk_data.CLERK_ID) AND v_clerk_data.DEPT_NO IN (''";
                    foreach (string CostCode_ in CostCode)
                        sql += ",'" + CostCode_.Trim() + "' ";

                    sql += ") " + "AND RECORDTIME >= to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') "
                        + "AND RECORDTIME <= to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') "
                        + "AND DELETED IS NULL "
                        + "ORDER BY RECORDTIME ";
                    DataTable Dt = ass_m.DBExecSQL(sql);
                    if (Dt.Rows.Count > 0)
                    {
                        PatientInfo pi = new PatientInfo();
                        string feeno = "";
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            if (feeno != Dt.Rows[i]["FEENO"].ToString())
                            {
                                feeno = Dt.Rows[i]["FEENO"].ToString();
                                TempByte = webService.GetPatientInfo(feeno);
                                if (TempByte != null)
                                    pi = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(TempByte));
                            }
                            temp = new Dictionary<string, string>();
                            temp["costCode"] = Dt.Rows[i]["DEPT_NAME"].ToString();//pi.CostCenterName;
                            temp["ptName"] = pi.PatientName;
                            temp["ChartNo"] = pi.ChartNo;
                            temp["age"] = pi.Age.ToString();
                            temp["deptName"] = pi.DeptName;
                            temp["icd"] = pi.ICD9_code1;
                            temp["ITEM_NAME"] = Dt.Rows[i]["ITEM_NAME"].ToString();
                            dt_.Add(temp);
                        }
                    }
                }

                List<string> Title = new List<string> { "單位", "病人姓名", "病歷號", "年齡", "科別", "診斷"
                                 , "護理指導名稱" };
                List<Dictionary<string, string>> Search_Title = new List<Dictionary<string, string>>();
                temp = new Dictionary<string, string>();
                temp["Name"] = "搜尋區間";
                temp["Value"] = Option["StartDate"] + "-" + Option["EndDate"];
                Search_Title.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Name"] = "單位";
                temp["Value"] = string.Join("、", JsonConvert.DeserializeObject<List<string>>(Option["CostName"]).ToArray());
                Search_Title.Add(temp);

                response(Title, Search_Title, dt_, "護理指導明細表");
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return new EmptyResult();
            }
        }

        [HttpPost]
        public ActionResult EducationItemStatistics(string OptionList)
        {
            try
            {
                List<Dictionary<string, string>> dt_ = new List<Dictionary<string, string>>();
                Dictionary<string, string> temp = null;
                Dictionary<string, string> Option = JsonConvert.DeserializeObject<Dictionary<string, string>>(OptionList);
                List<string> CostCode = JsonConvert.DeserializeObject<List<string>>(Option["CostCode"]);
                List<UserInfo> CostCodeUserList = new List<UserInfo>();
                byte[] TempByte = null;

                foreach (string CostCode_ in CostCode)
                {
                    TempByte = webService.GetUserList(CostCode_);
                    if (TempByte != null)
                        CostCodeUserList.AddRange(JsonConvert.DeserializeObject<List<UserInfo>>(CompressTool.DecompressString(TempByte)));
                }
                if (CostCodeUserList.Count > 0)
                {
                    string sql = "SELECT DISTINCT(A.ITEMID), COUNT(*) AS NUM "
                        + ",(SELECT NAME FROM HEALTH_EDUCATION_ITEM_DATA WHERE ITEM_ID = A.ITEMID)ITEM_NAME  "
                      //+ "FROM HEALTH_EDUCATION_DATA A "
                      //    + "WHERE A.CREATNO IN(''";
                      //foreach (UserInfo userNo in CostCodeUserList)
                      //    sql += ",'" + userNo.EmployeesNo.Trim() + "' ";

                      + "FROM  HEALTH_EDUCATION_DATA A,v_clerk_data  WHERE TRIM(A.CREATNO) =TRIM(v_clerk_data.CLERK_ID) AND v_clerk_data.DEPT_NO IN (''";
                    foreach (string CostCode_ in CostCode)
                        sql += ",'" + CostCode_.Trim() + "' ";

                    sql += ") " + "AND A.RECORDTIME >= to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') "
                        + "AND A.RECORDTIME <= to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') "
                        + "AND A.DELETED IS NULL "
                        + "GROUP BY (A.ITEMID) ORDER BY NUM DESC ";
                    DataTable Dt = ass_m.DBExecSQL(sql);
                    if (Dt.Rows.Count > 0)
                    {
                        PatientInfo pi = new PatientInfo();
                        string Row = "";
                        int Row_ = 0;
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            if (Row != Dt.Rows[i]["NUM"].ToString())
                            {
                                Row = Dt.Rows[i]["NUM"].ToString();
                                Row_++;
                            }
                            temp = new Dictionary<string, string>();
                            temp["ROW"] = Row_.ToString();
                            temp["ITEM_NAME"] = Dt.Rows[i]["ITEM_NAME"].ToString();
                            temp["NUM"] = Dt.Rows[i]["NUM"].ToString();
                            //temp["ITEM_NAME"] = reader["ITEM_NAME"].ToString();
                            //temp["NUM"] = reader["NUM"].ToString();
                            dt_.Add(temp);
                        }
                    }
                }

                List<string> Title = new List<string> { "排名", "護理指導名稱", "使用次數" };
                List<Dictionary<string, string>> Search_Title = new List<Dictionary<string, string>>();
                temp = new Dictionary<string, string>();
                temp["Name"] = "搜尋區間";
                temp["Value"] = Option["StartDate"] + "-" + Option["EndDate"];
                Search_Title.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Name"] = "單位";
                temp["Value"] = string.Join("、", JsonConvert.DeserializeObject<List<string>>(Option["CostName"]).ToArray());
                Search_Title.Add(temp);

                response(Title, Search_Title, dt_, "護理指導統計表");
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return new EmptyResult();
            }
        }

        [HttpPost]
        public ActionResult Education_WayDetail(string OptionList)
        {
            try
            {
                List<Dictionary<string, string>> dt_ = new List<Dictionary<string, string>>();
                Dictionary<string, string> temp = null;
                Dictionary<string, string> Option = JsonConvert.DeserializeObject<Dictionary<string, string>>(OptionList);
                List<string> CostCode = JsonConvert.DeserializeObject<List<string>>(Option["CostCode"]);
                List<UserInfo> CostCodeUserList = new List<UserInfo>();
                byte[] TempByte = null;

                foreach (string CostCode_ in CostCode)
                {
                    TempByte = webService.GetUserList(CostCode_);
                    if (TempByte != null)
                        CostCodeUserList.AddRange(JsonConvert.DeserializeObject<List<UserInfo>>(CompressTool.DecompressString(TempByte)));
                }
                if (CostCodeUserList.Count > 0)
                {

                    string sql = "SELECT HEALTH_EDUCATION_DATA.*, v_clerk_data.*, "
                        + "(SELECT NAME FROM HEALTH_EDUCATION_ITEM_DATA WHERE HEALTH_EDUCATION_ITEM_DATA.ITEM_ID = HEALTH_EDUCATION_DATA.ITEMID)ITEM_NAME "
                    //    + "FROM HEALTH_EDUCATION_DATA WHERE CREATNO IN('09277'";
                    //foreach (UserInfo userNo in CostCodeUserList)
                    //    sql += ",'" + userNo.EmployeesNo.Trim() + "' ";
                      + "FROM  HEALTH_EDUCATION_DATA,v_clerk_data  WHERE TRIM(HEALTH_EDUCATION_DATA.CREATNO) =TRIM(v_clerk_data.CLERK_ID) AND v_clerk_data.DEPT_NO IN (''";
                    foreach (string CostCode_ in CostCode)
                        sql += ",'" + CostCode_.Trim() + "' ";

                    sql += ") " + "AND RECORDTIME >= to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') "
                        + "AND RECORDTIME <= to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') "
                        + "AND DELETED IS NULL "
                        + "ORDER BY RECORDTIME ";
                    DataTable Dt = ass_m.DBExecSQL(sql);

                    if (Dt.Rows.Count > 0)
                    {
                        PatientInfo pi = new PatientInfo();
                        string feeno = "";
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            if (feeno != Dt.Rows[i]["FEENO"].ToString())
                            {
                                feeno = Dt.Rows[i]["FEENO"].ToString();
                                TempByte = webService.GetPatientInfo(feeno);
                                if (TempByte != null)
                                    pi = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(TempByte));
                            }
                            foreach (string item in Dt.Rows[i]["DESCRIPTION"].ToString().Replace("|99", "").Split('|'))
                            {
                                temp = new Dictionary<string, string>();
                                temp["costCode"] = Dt.Rows[i]["DEPT_NAME"].ToString();//pi.CostCenterName;
                                temp["ptName"] = pi.PatientName;
                                temp["ChartNo"] = pi.ChartNo;
                                temp["age"] = pi.Age.ToString();
                                temp["deptName"] = pi.DeptName;
                                temp["icd"] = pi.ICD9_code1;
                                temp["ITEM_NAME"] = Dt.Rows[i]["ITEM_NAME"].ToString();
                                temp["DESCRIPTION"] = item;
                                dt_.Add(temp);
                            }
                        }
                    }
                }

                List<string> Title = new List<string> { "單位", "病人姓名", "病歷號", "年齡", "科別", "診斷"
                                 , "護理指導名稱", "指導方式" };
                List<Dictionary<string, string>> Search_Title = new List<Dictionary<string, string>>();
                temp = new Dictionary<string, string>();
                temp["Name"] = "搜尋區間";
                temp["Value"] = Option["StartDate"] + "-" + Option["EndDate"];
                Search_Title.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Name"] = "單位";
                temp["Value"] = string.Join("、", JsonConvert.DeserializeObject<List<string>>(Option["CostName"]).ToArray());
                Search_Title.Add(temp);

                response(Title, Search_Title, dt_, "護理指導指導方式明細表");
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return new EmptyResult();
            }
        }

        [HttpPost]
        public ActionResult Education_WayStatistics(string OptionList)
        {
            try
            {
                List<string> WayList = new List<string> { "說明與討論", "提供衛教單張", "影片/衛教看板", "示範" };
                List<Dictionary<string, string>> dt_ = new List<Dictionary<string, string>>(), Tempdt = new List<Dictionary<string, string>>();
                Dictionary<string, string> temp = null;
                Dictionary<string, string> Option = JsonConvert.DeserializeObject<Dictionary<string, string>>(OptionList);
                List<string> CostCode = JsonConvert.DeserializeObject<List<string>>(Option["CostCode"]);
                List<UserInfo> CostCodeUserList = new List<UserInfo>();
                byte[] TempByte = null;

                foreach (string CostCode_ in CostCode)
                {
                    TempByte = webService.GetUserList(CostCode_);
                    if (TempByte != null)
                        CostCodeUserList.AddRange(JsonConvert.DeserializeObject<List<UserInfo>>(CompressTool.DecompressString(TempByte)));
                }
                if (CostCodeUserList.Count > 0)
                {
                    string sql = "SELECT HEALTH_EDUCATION_DATA.*, v_clerk_data.*, "
                        + "(SELECT NAME FROM HEALTH_EDUCATION_ITEM_DATA WHERE HEALTH_EDUCATION_ITEM_DATA.ITEM_ID = HEALTH_EDUCATION_DATA.ITEMID)ITEM_NAME "
                    //    + "FROM HEALTH_EDUCATION_DATA WHERE CREATNO IN('09277'";
                    //foreach (UserInfo userNo in CostCodeUserList)
                    //    sql += ",'" + userNo.EmployeesNo.Trim() + "' ";

                    + "FROM  HEALTH_EDUCATION_DATA,v_clerk_data  WHERE TRIM(HEALTH_EDUCATION_DATA.CREATNO) =TRIM(v_clerk_data.CLERK_ID) AND v_clerk_data.DEPT_NO IN (''";
                    foreach (string CostCode_ in CostCode)
                        sql += ",'" + CostCode_.Trim() + "' ";

                    sql += ") " + "AND RECORDTIME >= to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') "
                        + "AND RECORDTIME <= to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') "
                        + "AND DELETED IS NULL "
                        + "ORDER BY RECORDTIME ";
                    DataTable Dt = ass_m.DBExecSQL(sql);
                    if (Dt.Rows.Count > 0)
                    {
                        PatientInfo pi = new PatientInfo();
                        string feeno = "";

                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            if (feeno != Dt.Rows[i]["FEENO"].ToString())
                            {
                                feeno = Dt.Rows[i]["FEENO"].ToString();
                                TempByte = webService.GetPatientInfo(feeno);
                                if (TempByte != null)
                                    pi = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(TempByte));
                            }
                            temp = new Dictionary<string, string>();
                            temp["costCode"] = Dt.Rows[i]["DEPT_NAME"].ToString();//pi.CostCenterName;
                            temp["ptName"] = pi.PatientName;
                            temp["ChartNo"] = pi.ChartNo;
                            temp["age"] = pi.Age.ToString();
                            temp["deptName"] = pi.DeptName;
                            temp["icd"] = pi.ICD9_code1;
                            temp["ITEMID"] = Dt.Rows[i]["ITEMID"].ToString();
                            temp["ITEM_NAME"] = Dt.Rows[i]["ITEM_NAME"].ToString();
                            temp["DESCRIPTION"] = Dt.Rows[i]["DESCRIPTION"].ToString();
                            Tempdt.Add(temp);
                        }
                    }
                }
                if (Tempdt.Count > 0)
                {
                    List<Dictionary<string, string>> ItemTemp;
                    int Count = 0, tempCount = 0;
                    List<string> ItemList = Tempdt.Select(x => x["ITEMID"]).Distinct().ToList();
                    foreach (string item in ItemList)
                    {
                        Count = 0; tempCount = 0;
                        ItemTemp = Tempdt.FindAll(x => x["ITEMID"] == item);
                        temp = new Dictionary<string, string>();
                        temp["ITEM_NAME"] = ItemTemp[0]["ITEM_NAME"];
                        foreach (string Way in WayList)
                        {
                            tempCount = ItemTemp.FindAll(x => x["DESCRIPTION"].Contains(Way)).Count;
                            Count += tempCount;
                            temp[Way] = tempCount.ToString();
                        }
                        tempCount = ItemTemp.FindAll(x => x["DESCRIPTION"].Replace(WayList[0], "")
                            .Replace(WayList[1], "")
                            .Replace(WayList[2], "")
                            .Replace(WayList[3], "")
                            .Replace("|", "") != "").Count;
                        temp["其他"] = tempCount.ToString();
                        Count += tempCount;
                        temp["Count"] = Count.ToString();
                        dt_.Add(temp);
                    }
                }

                List<string> Title = new List<string> { "護理指導名稱" };
                foreach (string Way in WayList)
                {
                    Title.Add(Way);
                }
                Title.Add("其他");
                Title.Add("合計(單位:人次)");
                List<Dictionary<string, string>> Search_Title = new List<Dictionary<string, string>>();
                temp = new Dictionary<string, string>();
                temp["Name"] = "搜尋區間";
                temp["Value"] = Option["StartDate"] + "-" + Option["EndDate"];
                Search_Title.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Name"] = "單位";
                temp["Value"] = string.Join("、", JsonConvert.DeserializeObject<List<string>>(Option["CostName"]).ToArray());
                Search_Title.Add(temp);

                response(Title, Search_Title, dt_, "護理指導指導方式統計表");
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return new EmptyResult();
            }
        }

        [HttpPost]
        public ActionResult EducationDetail(string OptionList)
        {
            try
            {
                List<Dictionary<string, string>> dt_ = new List<Dictionary<string, string>>();
                Dictionary<string, string> temp = null;
                Dictionary<string, string> Option = JsonConvert.DeserializeObject<Dictionary<string, string>>(OptionList);
                List<string> CostCode = JsonConvert.DeserializeObject<List<string>>(Option["CostCode"]);
                List<UserInfo> CostCodeUserList = new List<UserInfo>();
                byte[] TempByte = null;
                bool op1 = Option["EducationSpecial"].Contains("指導對象"), op2 = Option["EducationSpecial"].Contains("指導方式")
                    , op3 = Option["EducationSpecial"].Contains("指導細項"), op4 = Option["EducationSpecial"].Contains("評值結果");

                foreach (string CostCode_ in CostCode)
                {
                    TempByte = webService.GetUserList(CostCode_);
                    if (TempByte != null)
                        CostCodeUserList.AddRange(JsonConvert.DeserializeObject<List<UserInfo>>(CompressTool.DecompressString(TempByte)));
                }
                if (CostCodeUserList.Count > 0)
                {

                    string sql = "SELECT HEALTH_EDUCATION_DATA.*, v_clerk_data.*, "
                        + "(SELECT NAME FROM HEALTH_EDUCATION_ITEM_DATA WHERE HEALTH_EDUCATION_ITEM_DATA.ITEM_ID = HEALTH_EDUCATION_DATA.ITEMID)ITEM_NAME "
                    //    + "FROM HEALTH_EDUCATION_DATA WHERE CREATNO IN('09277'";
                    //foreach (UserInfo userNo in CostCodeUserList)
                    //    sql += ",'" + userNo.EmployeesNo.Trim() + "' ";

                    + "FROM  HEALTH_EDUCATION_DATA,v_clerk_data  WHERE TRIM(HEALTH_EDUCATION_DATA.CREATNO) =TRIM(v_clerk_data.CLERK_ID) AND v_clerk_data.DEPT_NO IN (''";
                    foreach (string CostCode_ in CostCode)
                        sql += ",'" + CostCode_.Trim() + "' ";
                    sql += ") " + "AND RECORDTIME >= to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') "
                        + "AND RECORDTIME <= to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') "
                        + "AND DELETED IS NULL "
                        + "ORDER BY RECORDTIME ";
                    DataTable Dt = ass_m.DBExecSQL(sql);

                    if (Dt.Rows.Count > 0)
                    {
                        PatientInfo pi = new PatientInfo();
                        string feeno = "", tempString = "";
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            if (feeno != Dt.Rows[i]["FEENO"].ToString())
                            {
                                feeno = Dt.Rows[i]["FEENO"].ToString();
                                TempByte = webService.GetPatientInfo(feeno);
                                if (TempByte != null)
                                    pi = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(TempByte));
                            }

                            if (op1)
                                tempString = Dt.Rows[i]["OBJECT"].ToString().Replace("99|", "");
                            if (op2)
                                tempString = Dt.Rows[i]["DESCRIPTION"].ToString();
                            if (op3)
                                tempString = Dt.Rows[i]["INSTRUCT"].ToString();
                            if (op4)
                                tempString = Dt.Rows[i]["SCORE_RESULT"].ToString();

                            foreach (string item in tempString.Split('|'))
                            {
                                temp = new Dictionary<string, string>();
                                temp["costCode"] = Dt.Rows[i]["DEPT_NAME"].ToString();//pi.CostCenterName;
                                temp["ptName"] = pi.PatientName;
                                temp["ChartNo"] = pi.ChartNo;
                                temp["age"] = pi.Age.ToString();
                                temp["deptName"] = pi.DeptName;
                                temp["icd"] = pi.ICD9_code1;
                                temp["ITEM_NAME"] = Dt.Rows[i]["ITEM_NAME"].ToString();
                                temp["ITEM"] = item;
                                dt_.Add(temp);
                            }
                        }
                    }
                }

                List<string> Title = new List<string> { "單位", "病人姓名", "病歷號", "年齡", "科別", "診斷"
                                 , "護理指導", "項目內容" };
                List<Dictionary<string, string>> Search_Title = new List<Dictionary<string, string>>();
                temp = new Dictionary<string, string>();
                temp["Name"] = "搜尋區間";
                temp["Value"] = Option["StartDate"] + "-" + Option["EndDate"];
                Search_Title.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Name"] = "單位";
                temp["Value"] = string.Join("、", JsonConvert.DeserializeObject<List<string>>(Option["CostName"]).ToArray());
                Search_Title.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Name"] = "項目類別";
                temp["Value"] = Option["EducationSpecial"];
                Search_Title.Add(temp);

                response(Title, Search_Title, dt_, "護理指導指導內容明細表");
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return new EmptyResult();
            }
        }

        [HttpPost]
        public ActionResult FallAssess_AdultDetail(string OptionList)
        {
            try
            {
                List<Dictionary<string, string>> dt_ = new List<Dictionary<string, string>>();
                Dictionary<string, string> temp = null;
                Dictionary<string, string> Option = JsonConvert.DeserializeObject<Dictionary<string, string>>(OptionList);
                List<string> CostCode = JsonConvert.DeserializeObject<List<string>>(Option["CostCode"]);
                List<UserInfo> CostCodeUserList = new List<UserInfo>();
                byte[] TempByte = null;

                foreach (string CostCode_ in CostCode)
                {
                    TempByte = webService.GetUserList(CostCode_);
                    if (TempByte != null)
                        CostCodeUserList.AddRange(JsonConvert.DeserializeObject<List<UserInfo>>(CompressTool.DecompressString(TempByte)));
                }
                if (CostCodeUserList.Count > 0)
                {
                    string sql = "SELECT NIS_FALL_ASSESS_DATA.*, v_clerk_data.* "
                       //foreach (UserInfo userNo in CostCodeUserList)
                       //  sql += ",'" + userNo.EmployeesNo.Trim() + "' ";
                       + "FROM  NIS_FALL_ASSESS_DATA,v_clerk_data  WHERE TRIM(NIS_FALL_ASSESS_DATA.CREATNO) =TRIM(v_clerk_data.CLERK_ID) AND v_clerk_data.DEPT_NO IN (''";
                    foreach (string CostCode_ in CostCode)
                        sql += ",'" + CostCode_.Trim() + "' ";

                    sql += ") " + "AND RECORDTIME >= to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') "
                        + "AND RECORDTIME <= to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') "
                        + "AND DELETED IS NULL "
                        + "ORDER BY RECORDTIME ";
                    DataTable Dt = ass_m.DBExecSQL(sql);

                    if (Dt.Rows.Count > 0)
                    {
                        PatientInfo pi = new PatientInfo();
                        string feeno = "";
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            if (feeno != Dt.Rows[i]["FEENO"].ToString())
                            {
                                feeno = Dt.Rows[i]["FEENO"].ToString();
                                TempByte = webService.GetPatientInfo(feeno);
                                if (TempByte != null)
                                    pi = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(TempByte));
                            }
                            temp = new Dictionary<string, string>();
                            temp["costCode"] = pi.CostCenterName;
                            temp["RECORDTIME_Date"] = Convert.ToDateTime(Dt.Rows[i]["RECORDTIME"].ToString()).ToString("yyyy/MM/dd");
                            temp["RECORDTIME_Time"] = Convert.ToDateTime(Dt.Rows[i]["RECORDTIME"].ToString()).ToString("HH:mm");
                            temp["ptName"] = pi.PatientName;
                            temp["ChartNo"] = pi.ChartNo;
                            temp["age"] = pi.Age.ToString();
                            temp["deptName"] = pi.DeptName;
                            temp["icd"] = pi.ICD9_code1;
                            temp["REASON"] = Dt.Rows[i]["REASON"].ToString();
                            temp["AssAGE"] = Dt.Rows[i]["AGE"].ToString();
                            temp["CONSCIOUSNESS"] = Dt.Rows[i]["CONSCIOUSNESS"].ToString();
                            temp["DIZZINESS"] = Dt.Rows[i]["DIZZINESS"].ToString();
                            temp["DRUG"] = Dt.Rows[i]["DRUG"].ToString();
                            temp["EXCRETION"] = Dt.Rows[i]["EXCRETION"].ToString();
                            temp["FALL"] = Dt.Rows[i]["FALL"].ToString();
                            temp["ACTIVITY"] = Dt.Rows[i]["ACTIVITY"].ToString();
                            temp["COMMUNICATION"] = Dt.Rows[i]["COMMUNICATION"].ToString();
                            temp["TOTAL"] = Dt.Rows[i]["TOTAL"].ToString();
                            temp["Level"] = (int.Parse(Dt.Rows[i]["TOTAL"].ToString()) >= 3) ? "高危險" : "無";
                            dt_.Add(temp);
                        }
                    }
                }

                List<string> Title = new List<string> { "單位", "日期", "時間", "病人姓名", "病歷號", "年齡", "科別", "診斷"
                                 , "跌倒評估原因", "年齡", "意識狀態", "頭暈/眩暈/虛弱感/視力異常", "使用特殊藥物易致跌倒", "排泄情形", "跌倒史", "輔助使用", "執意下床", "總分", "跌倒危險程度" };
                List<Dictionary<string, string>> Search_Title = new List<Dictionary<string, string>>();
                temp = new Dictionary<string, string>();
                temp["Name"] = "搜尋區間";
                temp["Value"] = Option["StartDate"] + "-" + Option["EndDate"];
                Search_Title.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Name"] = "單位";
                temp["Value"] = string.Join("、", JsonConvert.DeserializeObject<List<string>>(Option["CostName"]).ToArray());
                Search_Title.Add(temp);

                response(Title, Search_Title, dt_, "成人跌倒危險性評估明細表");
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return new EmptyResult();
            }
        }

        [HttpPost]
        public ActionResult FallAssess_AdultStatistics(string OptionList)
        {
            try
            {
                List<Dictionary<string, string>> dt_ = new List<Dictionary<string, string>>();
                Dictionary<string, string> temp = null;
                Dictionary<string, string> Option = JsonConvert.DeserializeObject<Dictionary<string, string>>(OptionList);
                List<string> CostCode = JsonConvert.DeserializeObject<List<string>>(Option["CostCode"]);
                List<UserInfo> CostCodeUserList = new List<UserInfo>();
                byte[] TempByte = null;

                temp = new Dictionary<string, string>();
                temp["Title"] = "高危險";
                temp["Num"] = "0";
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Title"] = "無";
                temp["Num"] = "0";
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Title"] = "跌倒危險比率(%)";
                temp["Num"] = "0";
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Title"] = "備註";
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Title"] = "高危險定義:跌倒危險性評估總分≧3分者";
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Title"] = "無:跌倒危險性評估總分<3分者";
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Title"] = "跌倒危險比率=(高危人數/(高危人數+無人數))*100";
                dt_.Add(temp);

                foreach (string CostCode_ in CostCode)
                {
                    TempByte = webService.GetUserList(CostCode_);
                    if (TempByte != null)
                        CostCodeUserList.AddRange(JsonConvert.DeserializeObject<List<UserInfo>>(CompressTool.DecompressString(TempByte)));
                }
                if (CostCodeUserList.Count > 0)
                {

                    string sql = "SELECT NIS_FALL_ASSESS_DATA.*, v_clerk_data.* "
                       //foreach (UserInfo userNo in CostCodeUserList)
                       //  sql += ",'" + userNo.EmployeesNo.Trim() + "' ";
                       + "FROM  NIS_FALL_ASSESS_DATA,v_clerk_data  WHERE TRIM(NIS_FALL_ASSESS_DATA.CREATNO) =TRIM(v_clerk_data.CLERK_ID) AND v_clerk_data.DEPT_NO IN (''";
                    foreach (string CostCode_ in CostCode)
                        sql += ",'" + CostCode_.Trim() + "' ";
                    sql += ") " + "AND RECORDTIME >= to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') "
                        + "AND RECORDTIME <= to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') "
                        + "AND DELETED IS NULL "
                        + "ORDER BY RECORDTIME ";
                    DataTable Dt = ass_m.DBExecSQL(sql);
                    if (Dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            if (int.Parse(Dt.Rows[i]["TOTAL"].ToString()) >= 3)
                                dt_[0]["Num"] = (int.Parse(dt_[0]["Num"]) + 1).ToString();
                            else
                                dt_[1]["Num"] = (int.Parse(dt_[1]["Num"]) + 1).ToString();
                        }
                        dt_[2]["Num"] = (Math.Round((float.Parse(dt_[0]["Num"]) / (int.Parse(dt_[0]["Num"]) + int.Parse(dt_[1]["Num"]))), 2) * 100).ToString() + "%";
                    }
                }

                List<string> Title = new List<string> { "跌倒危險程度", "人數" };
                List<Dictionary<string, string>> Search_Title = new List<Dictionary<string, string>>();
                temp = new Dictionary<string, string>();
                temp["Name"] = "搜尋區間";
                temp["Value"] = Option["StartDate"] + "-" + Option["EndDate"];
                Search_Title.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Name"] = "單位";
                temp["Value"] = string.Join("、", JsonConvert.DeserializeObject<List<string>>(Option["CostName"]).ToArray());
                Search_Title.Add(temp);

                response(Title, Search_Title, dt_, "成人跌倒危險性評估統計表");
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return new EmptyResult();
            }
        }

        [HttpPost]
        public ActionResult FallAssessMeasure_AdultDetail(string OptionList)
        {
            try
            {
                List<Dictionary<string, string>> dt_ = new List<Dictionary<string, string>>();
                Dictionary<string, string> temp = null;
                Dictionary<string, string> Option = JsonConvert.DeserializeObject<Dictionary<string, string>>(OptionList);
                List<string> CostCode = JsonConvert.DeserializeObject<List<string>>(Option["CostCode"]);
                List<UserInfo> CostCodeUserList = new List<UserInfo>();
                byte[] TempByte = null;
                List<FallAssessMeasure> fallAssessMeasure = base.GetAdultFallAssessMeasure();
                fallAssessMeasure.RemoveAt(0);

                foreach (string CostCode_ in CostCode)
                {
                    TempByte = webService.GetUserList(CostCode_);
                    if (TempByte != null)
                        CostCodeUserList.AddRange(JsonConvert.DeserializeObject<List<UserInfo>>(CompressTool.DecompressString(TempByte)));
                }
                if (CostCodeUserList.Count > 0)
                {
                    string sql = "SELECT NIS_FALL_ASSESS_DATA.*, v_clerk_data.* "
                       //foreach (UserInfo userNo in CostCodeUserList)
                       //  sql += ",'" + userNo.EmployeesNo.Trim() + "' ";
                       + "FROM  NIS_FALL_ASSESS_DATA,v_clerk_data  WHERE TRIM(NIS_FALL_ASSESS_DATA.CREATNO) =TRIM(v_clerk_data.CLERK_ID) AND v_clerk_data.DEPT_NO IN (''";
                    foreach (string CostCode_ in CostCode)
                        sql += ",'" + CostCode_.Trim() + "' ";
                    sql += ") " + "AND RECORDTIME >= to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') "
                        + "AND RECORDTIME <= to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') "
                        + "AND DELETED IS NULL "
                        + "ORDER BY RECORDTIME ";
                    DataTable Dt = ass_m.DBExecSQL(sql);

                    if (Dt.Rows.Count > 0)
                    {
                        PatientInfo pi = new PatientInfo();
                        string feeno = "";
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            if (feeno != Dt.Rows[i]["FEENO"].ToString())
                            {
                                feeno = Dt.Rows[i]["FEENO"].ToString();
                                TempByte = webService.GetPatientInfo(feeno);
                                if (TempByte != null)
                                    pi = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(TempByte));
                            }
                            temp = new Dictionary<string, string>();
                            temp["costCode"] = pi.CostCenterName;
                            temp["RECORDTIME_Date"] = Convert.ToDateTime(Dt.Rows[i]["RECORDTIME"].ToString()).ToString("yyyy/MM/dd");
                            temp["RECORDTIME_Time"] = Convert.ToDateTime(Dt.Rows[i]["RECORDTIME"].ToString()).ToString("HH:mm");
                            temp["ptName"] = pi.PatientName;
                            temp["ChartNo"] = pi.ChartNo;
                            temp["age"] = pi.Age.ToString();
                            temp["deptName"] = pi.DeptName;
                            temp["icd"] = pi.ICD9_code1;
                            foreach (FallAssessMeasure item in fallAssessMeasure)
                            {
                                if (!string.IsNullOrWhiteSpace(Dt.Rows[i]["PRECAUTION"].ToString()))
                                    temp[item.Name] = (Dt.Rows[i]["PRECAUTION"].ToString().IndexOf(item.Name) > -1) ? "是" : "否";
                            }
                            dt_.Add(temp);
                        }
                    }
                }

                List<string> Title = new List<string> { "單位", "日期", "時間", "病人姓名", "病歷號", "年齡", "科別", "診斷"
                                 , fallAssessMeasure[0].Name, fallAssessMeasure[1].Name, fallAssessMeasure[2].Name, fallAssessMeasure[3].Name
                                 , fallAssessMeasure[4].Name, fallAssessMeasure[5].Name, fallAssessMeasure[6].Name, fallAssessMeasure[7].Name
                                 , fallAssessMeasure[8].Name, fallAssessMeasure[9].Name, fallAssessMeasure[10].Name, fallAssessMeasure[11].Name
                                 , fallAssessMeasure[12].Name, fallAssessMeasure[13].Name, fallAssessMeasure[14].Name };
                List<Dictionary<string, string>> Search_Title = new List<Dictionary<string, string>>();
                temp = new Dictionary<string, string>();
                temp["Name"] = "搜尋區間";
                temp["Value"] = Option["StartDate"] + "-" + Option["EndDate"];
                Search_Title.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Name"] = "單位";
                temp["Value"] = string.Join("、", JsonConvert.DeserializeObject<List<string>>(Option["CostName"]).ToArray());
                Search_Title.Add(temp);

                response(Title, Search_Title, dt_, "成人跌倒危險性措施使用明細表");
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return new EmptyResult();
            }
        }

        [HttpPost]
        public ActionResult FallAssessMeasure_AdultStatistics(string OptionList)
        {
            try
            {
                List<Dictionary<string, string>> dt_ = new List<Dictionary<string, string>>();
                Dictionary<string, string> temp;
                Dictionary<string, string> Option = JsonConvert.DeserializeObject<Dictionary<string, string>>(OptionList);
                List<string> CostCode = JsonConvert.DeserializeObject<List<string>>(Option["CostCode"]);
                List<UserInfo> CostCodeUserList = new List<UserInfo>();
                byte[] TempByte = null;
                List<FallAssessMeasure> fallAssessMeasure = base.GetAdultFallAssessMeasure();

                foreach (string CostCode_ in CostCode)
                {
                    TempByte = webService.GetUserList(CostCode_);
                    if (TempByte != null)
                        CostCodeUserList.AddRange(JsonConvert.DeserializeObject<List<UserInfo>>(CompressTool.DecompressString(TempByte)));
                }
                if (CostCodeUserList.Count > 0)
                {
                    string sql = "SELECT NIS_FALL_ASSESS_DATA.*, v_clerk_data.* "
                       //foreach (UserInfo userNo in CostCodeUserList)
                       //  sql += ",'" + userNo.EmployeesNo.Trim() + "' ";
                       + "FROM  NIS_FALL_ASSESS_DATA,v_clerk_data  WHERE TRIM(NIS_FALL_ASSESS_DATA.CREATNO) =TRIM(v_clerk_data.CLERK_ID) AND v_clerk_data.DEPT_NO IN (''";
                    foreach (string CostCode_ in CostCode)
                        sql += ",'" + CostCode_.Trim() + "' ";
                    sql += ") " + "AND RECORDTIME >= to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') "
                        + "AND RECORDTIME <= to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') "
                        + "AND DELETED IS NULL "
                        + "ORDER BY RECORDTIME ";
                    DataTable Dt = ass_m.DBExecSQL(sql);
                    if (Dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            if (!string.IsNullOrWhiteSpace(Dt.Rows[i]["PRECAUTION"].ToString()))
                            {
                                foreach (string item in Dt.Rows[i]["PRECAUTION"].ToString().Split(','))
                                {
                                    if (fallAssessMeasure.Exists(x => x.Name == item))
                                        fallAssessMeasure.Find(x => x.Name == item).Num++;
                                }
                            }
                        }
                    }
                    fallAssessMeasure.Sort((x, y) => { return -x.Num.CompareTo(y.Num); });

                    string Row = "";
                    int Row_ = 0;
                    int total = fallAssessMeasure.Sum(x => x.Num);
                    foreach (FallAssessMeasure item in fallAssessMeasure)
                    {
                        if (Row != item.Num.ToString())
                        {
                            Row = item.Num.ToString();
                            Row_++;
                        }
                        temp = new Dictionary<string, string>();
                        temp["level"] = "第 " + Row_.ToString() + " 名";
                        temp["Name"] = item.Name;
                        temp["Num"] = item.Num.ToString();
                        if (total > 0)
                            temp["percent"] = (Math.Round((float.Parse(item.Num.ToString()) / total), 3) * 100).ToString();
                        else
                            temp["percent"] = "0";
                        dt_.Add(temp);
                    }
                    temp = new Dictionary<string, string>();
                    temp["level"] = "";
                    temp["Name"] = "總計";
                    temp["Num"] = total.ToString();
                    temp["percent"] = "";
                    dt_.Add(temp);
                    temp = new Dictionary<string, string>();
                    dt_.Add(temp);
                    temp = new Dictionary<string, string>();
                    temp["level"] = "備註";
                    dt_.Add(temp);
                    temp = new Dictionary<string, string>();
                    temp["level"] = "比率：(單措施內容合計/總計)*100%";
                    dt_.Add(temp);
                }

                List<string> Title = new List<string> { "排名", "措施內容", "合計", "比率(%)" };
                List<Dictionary<string, string>> Search_Title = new List<Dictionary<string, string>>();
                temp = new Dictionary<string, string>();
                temp["Name"] = "搜尋區間";
                temp["Value"] = Option["StartDate"] + "-" + Option["EndDate"];
                Search_Title.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Name"] = "單位";
                temp["Value"] = string.Join("、", JsonConvert.DeserializeObject<List<string>>(Option["CostName"]).ToArray());
                Search_Title.Add(temp);

                response(Title, Search_Title, dt_, "成人跌倒危險性措施使用統計表");
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return new EmptyResult();
            }
        }

        [HttpPost]
        public ActionResult FallAssess_ChildDetail(string OptionList)
        {
            try
            {
                List<Dictionary<string, string>> dt_ = new List<Dictionary<string, string>>();
                Dictionary<string, string> temp = null;
                Dictionary<string, string> Option = JsonConvert.DeserializeObject<Dictionary<string, string>>(OptionList);
                List<string> CostCode = JsonConvert.DeserializeObject<List<string>>(Option["CostCode"]);
                List<UserInfo> CostCodeUserList = new List<UserInfo>();
                byte[] TempByte = null;

                foreach (string CostCode_ in CostCode)
                {
                    TempByte = webService.GetUserList(CostCode_);
                    if (TempByte != null)
                        CostCodeUserList.AddRange(JsonConvert.DeserializeObject<List<UserInfo>>(CompressTool.DecompressString(TempByte)));
                }
                if (CostCodeUserList.Count > 0)
                {

                    string sql = "SELECT NIS_FALL_ASSESS_DATA_CHILD.*, v_clerk_data.* "
                       //foreach (UserInfo userNo in CostCodeUserList)
                       //  sql += ",'" + userNo.EmployeesNo.Trim() + "' ";
                       + "FROM  NIS_FALL_ASSESS_DATA_CHILD,v_clerk_data  WHERE TRIM(NIS_FALL_ASSESS_DATA_CHILD.CREATNO) =TRIM(v_clerk_data.CLERK_ID) AND v_clerk_data.DEPT_NO IN (''";
                    foreach (string CostCode_ in CostCode)
                        sql += ",'" + CostCode_.Trim() + "' ";
                    sql += ") " + "AND RECORDTIME >= to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') "
                        + "AND RECORDTIME <= to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') "
                        + "AND DELETED IS NULL "
                        + "ORDER BY RECORDTIME ";
                    DataTable Dt = ass_m.DBExecSQL(sql);
                    if (Dt.Rows.Count > 0)
                    {
                        PatientInfo pi = new PatientInfo();
                        string feeno = "";
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            if (feeno != Dt.Rows[i]["FEENO"].ToString())
                            {
                                feeno = Dt.Rows[i]["FEENO"].ToString();
                                TempByte = webService.GetPatientInfo(feeno);
                                if (TempByte != null)
                                    pi = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(TempByte));
                            }
                            temp = new Dictionary<string, string>();
                            temp["costCode"] = pi.CostCenterName;
                            temp["RECORDTIME_Date"] = Convert.ToDateTime(Dt.Rows[i]["RECORDTIME"].ToString()).ToString("yyyy/MM/dd");
                            temp["RECORDTIME_Time"] = Convert.ToDateTime(Dt.Rows[i]["RECORDTIME"].ToString()).ToString("HH:mm");
                            temp["ptName"] = pi.PatientName;
                            temp["ChartNo"] = pi.ChartNo;
                            temp["age"] = pi.Age.ToString();
                            temp["deptName"] = pi.DeptName;
                            temp["icd"] = pi.ICD9_code1;
                            temp["REASON"] = Dt.Rows[i]["REASON"].ToString();
                            temp["AssAGE"] = Dt.Rows[i]["AGE"].ToString();
                            temp["GENDER"] = Dt.Rows[i]["GENDER"].ToString();
                            temp["FITNESS_FALL"] = Dt.Rows[i]["FITNESS_FALL"].ToString();
                            temp["FALL_HISTORY"] = Dt.Rows[i]["FALL_HISTORY"].ToString();
                            temp["ACTIVITY"] = Dt.Rows[i]["ACTIVITY"].ToString();
                            temp["DRUG"] = Dt.Rows[i]["DRUG"].ToString();
                            temp["TOTAL"] = Dt.Rows[i]["TOTAL"].ToString();
                            temp["Level"] = (int.Parse(Dt.Rows[i]["TOTAL"].ToString()) >= 3) ? "高危險" : "無";
                            dt_.Add(temp);
                        }
                    }
                }

                List<string> Title = new List<string> { "單位", "日期", "時間", "病人姓名", "病歷號", "年齡", "科別", "診斷"
                                 , "跌倒評估原因", "年齡", "性別", "體能狀況", "跌倒史", "活動力", "藥物(抗組織胺類、抗癲癇藥物)", "總分", "跌倒危險程度" };
                List<Dictionary<string, string>> Search_Title = new List<Dictionary<string, string>>();
                temp = new Dictionary<string, string>();
                temp["Name"] = "搜尋區間";
                temp["Value"] = Option["StartDate"] + "-" + Option["EndDate"];
                Search_Title.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Name"] = "單位";
                temp["Value"] = string.Join("、", JsonConvert.DeserializeObject<List<string>>(Option["CostName"]).ToArray());
                Search_Title.Add(temp);

                response(Title, Search_Title, dt_, "兒童跌倒危險性評估明細表");
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return new EmptyResult();
            }
        }

        [HttpPost]
        public ActionResult FallAssess_ChildStatistics(string OptionList)
        {
            try
            {
                List<Dictionary<string, string>> dt_ = new List<Dictionary<string, string>>();
                Dictionary<string, string> temp = null;
                Dictionary<string, string> Option = JsonConvert.DeserializeObject<Dictionary<string, string>>(OptionList);
                List<string> CostCode = JsonConvert.DeserializeObject<List<string>>(Option["CostCode"]);
                List<UserInfo> CostCodeUserList = new List<UserInfo>();
                byte[] TempByte = null;

                temp = new Dictionary<string, string>();
                temp["Title"] = "高危險";
                temp["Num"] = "0";
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Title"] = "無";
                temp["Num"] = "0";
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Title"] = "跌倒危險比率(%)";
                temp["Num"] = "0";
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Title"] = "備註";
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Title"] = "高危險定義:跌倒危險性評估總分≧3分者";
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Title"] = "無:跌倒危險性評估總分<3分者";
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Title"] = "跌倒危險比率=(高危人數/(高危人數+無人數))*100";
                dt_.Add(temp);

                foreach (string CostCode_ in CostCode)
                {
                    TempByte = webService.GetUserList(CostCode_);
                    if (TempByte != null)
                        CostCodeUserList.AddRange(JsonConvert.DeserializeObject<List<UserInfo>>(CompressTool.DecompressString(TempByte)));
                }
                if (CostCodeUserList.Count > 0)
                {
                    string sql = "SELECT NIS_FALL_ASSESS_DATA_CHILD.*, v_clerk_data.* "
                       //foreach (UserInfo userNo in CostCodeUserList)
                       //  sql += ",'" + userNo.EmployeesNo.Trim() + "' ";
                       + "FROM  NIS_FALL_ASSESS_DATA_CHILD,v_clerk_data  WHERE TRIM(NIS_FALL_ASSESS_DATA_CHILD.CREATNO) =TRIM(v_clerk_data.CLERK_ID) AND v_clerk_data.DEPT_NO IN (''";
                    foreach (string CostCode_ in CostCode)
                        sql += ",'" + CostCode_.Trim() + "' ";
                    sql += ") " + "AND RECORDTIME >= to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') "
                        + "AND RECORDTIME <= to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') "
                        + "AND DELETED IS NULL "
                        + "ORDER BY RECORDTIME ";
                    DataTable Dt = ass_m.DBExecSQL(sql);
                    if (Dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            if (int.Parse(Dt.Rows[i]["TOTAL"].ToString()) >= 3)
                                dt_[0]["Num"] = (int.Parse(dt_[0]["Num"]) + 1).ToString();
                            else
                                dt_[1]["Num"] = (int.Parse(dt_[1]["Num"]) + 1).ToString();
                        }
                        dt_[2]["Num"] = (Math.Round((float.Parse(dt_[0]["Num"]) / (int.Parse(dt_[0]["Num"]) + int.Parse(dt_[1]["Num"]))), 2) * 100).ToString() + "%";
                    }
                }

                List<string> Title = new List<string> { "跌倒危險程度", "人數" };
                List<Dictionary<string, string>> Search_Title = new List<Dictionary<string, string>>();
                temp = new Dictionary<string, string>();
                temp["Name"] = "搜尋區間";
                temp["Value"] = Option["StartDate"] + "-" + Option["EndDate"];
                Search_Title.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Name"] = "單位";
                temp["Value"] = string.Join("、", JsonConvert.DeserializeObject<List<string>>(Option["CostName"]).ToArray());
                Search_Title.Add(temp);

                response(Title, Search_Title, dt_, "兒童跌倒危險性評估統計表");
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return new EmptyResult();
            }
        }

        [HttpPost]
        public ActionResult FallAssessMeasure_ChildDetail(string OptionList)
        {
            try
            {
                List<Dictionary<string, string>> dt_ = new List<Dictionary<string, string>>();
                Dictionary<string, string> temp = null;
                Dictionary<string, string> Option = JsonConvert.DeserializeObject<Dictionary<string, string>>(OptionList);
                List<string> CostCode = JsonConvert.DeserializeObject<List<string>>(Option["CostCode"]);
                List<UserInfo> CostCodeUserList = new List<UserInfo>();
                byte[] TempByte = null;
                List<FallAssessMeasure> fallAssessMeasure = base.GetChildFallAssessMeasure();
                fallAssessMeasure.RemoveAt(0);

                foreach (string CostCode_ in CostCode)
                {
                    TempByte = webService.GetUserList(CostCode_);
                    if (TempByte != null)
                        CostCodeUserList.AddRange(JsonConvert.DeserializeObject<List<UserInfo>>(CompressTool.DecompressString(TempByte)));
                }
                if (CostCodeUserList.Count > 0)
                {
                    string sql = "SELECT NIS_FALL_ASSESS_DATA_CHILD.*, v_clerk_data.* "
                       //foreach (UserInfo userNo in CostCodeUserList)
                       //  sql += ",'" + userNo.EmployeesNo.Trim() + "' ";
                       + "FROM  NIS_FALL_ASSESS_DATA_CHILD,v_clerk_data  WHERE TRIM(NIS_FALL_ASSESS_DATA_CHILD.CREATNO) =TRIM(v_clerk_data.CLERK_ID) AND v_clerk_data.DEPT_NO IN (''";
                    foreach (string CostCode_ in CostCode)
                        sql += ",'" + CostCode_.Trim() + "' ";
                    sql += ") " + "AND RECORDTIME >= to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') "
                        + "AND RECORDTIME <= to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') "
                        + "AND DELETED IS NULL "
                        + "ORDER BY RECORDTIME ";
                    DataTable Dt = ass_m.DBExecSQL(sql);
                    if (Dt.Rows.Count > 0)
                    {
                        PatientInfo pi = new PatientInfo();
                        string feeno = "";
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            if (feeno != Dt.Rows[i]["FEENO"].ToString())
                            {
                                feeno = Dt.Rows[i]["FEENO"].ToString();
                                TempByte = webService.GetPatientInfo(feeno);
                                if (TempByte != null)
                                    pi = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(TempByte));
                            }
                            temp = new Dictionary<string, string>();
                            temp["costCode"] = pi.CostCenterName;
                            temp["RECORDTIME_Date"] = Convert.ToDateTime(Dt.Rows[i]["RECORDTIME"].ToString()).ToString("yyyy/MM/dd");
                            temp["RECORDTIME_Time"] = Convert.ToDateTime(Dt.Rows[i]["RECORDTIME"].ToString()).ToString("HH:mm");
                            temp["ptName"] = pi.PatientName;
                            temp["ChartNo"] = pi.ChartNo;
                            temp["age"] = pi.Age.ToString();
                            temp["deptName"] = pi.DeptName;
                            temp["icd"] = pi.ICD9_code1;
                            foreach (FallAssessMeasure item in fallAssessMeasure)
                            {
                                if (!string.IsNullOrWhiteSpace(Dt.Rows[i]["PRECAUTION"].ToString()))
                                    temp[item.Name] = (Dt.Rows[i]["PRECAUTION"].ToString().IndexOf(item.Name) > -1) ? "是" : "否";
                            }
                            dt_.Add(temp);
                        }
                    }
                }

                List<string> Title = new List<string> { "單位", "日期", "時間", "病人姓名", "病歷號", "年齡", "科別", "診斷"
                                 , fallAssessMeasure[0].Name, fallAssessMeasure[1].Name, fallAssessMeasure[2].Name, fallAssessMeasure[3].Name
                                 , fallAssessMeasure[4].Name, fallAssessMeasure[5].Name, fallAssessMeasure[6].Name, fallAssessMeasure[7].Name };
                List<Dictionary<string, string>> Search_Title = new List<Dictionary<string, string>>();
                temp = new Dictionary<string, string>();
                temp["Name"] = "搜尋區間";
                temp["Value"] = Option["StartDate"] + "-" + Option["EndDate"];
                Search_Title.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Name"] = "單位";
                temp["Value"] = string.Join("、", JsonConvert.DeserializeObject<List<string>>(Option["CostName"]).ToArray());
                Search_Title.Add(temp);

                response(Title, Search_Title, dt_, "兒童跌倒危險性措施使用明細表");
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return new EmptyResult();
            }
        }

        [HttpPost]
        public ActionResult FallAssessMeasure_ChildStatistics(string OptionList)
        {
            try
            {
                List<Dictionary<string, string>> dt_ = new List<Dictionary<string, string>>();
                Dictionary<string, string> temp;
                Dictionary<string, string> Option = JsonConvert.DeserializeObject<Dictionary<string, string>>(OptionList);
                List<string> CostCode = JsonConvert.DeserializeObject<List<string>>(Option["CostCode"]);
                List<UserInfo> CostCodeUserList = new List<UserInfo>();
                byte[] TempByte = null;
                List<FallAssessMeasure> fallAssessMeasure = base.GetChildFallAssessMeasure();

                foreach (string CostCode_ in CostCode)
                {
                    TempByte = webService.GetUserList(CostCode_);
                    if (TempByte != null)
                        CostCodeUserList.AddRange(JsonConvert.DeserializeObject<List<UserInfo>>(CompressTool.DecompressString(TempByte)));
                }
                if (CostCodeUserList.Count > 0)
                {
                    string sql = "SELECT NIS_FALL_ASSESS_DATA_CHILD.*, v_clerk_data.* "
                       //foreach (UserInfo userNo in CostCodeUserList)
                       //  sql += ",'" + userNo.EmployeesNo.Trim() + "' ";
                       + "FROM  NIS_FALL_ASSESS_DATA_CHILD,v_clerk_data  WHERE TRIM(NIS_FALL_ASSESS_DATA_CHILD.CREATNO) =TRIM(v_clerk_data.CLERK_ID) AND v_clerk_data.DEPT_NO IN (''";
                    foreach (string CostCode_ in CostCode)
                        sql += ",'" + CostCode_.Trim() + "' ";
                    sql += ") " + "AND RECORDTIME >= to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') "
                        + "AND RECORDTIME <= to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') "
                        + "AND DELETED IS NULL "
                        + "ORDER BY RECORDTIME ";

                    DataTable Dt = ass_m.DBExecSQL(sql);
                    if (Dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            if (!string.IsNullOrWhiteSpace(Dt.Rows[i]["PRECAUTION"].ToString()))
                            {
                                foreach (string item in Dt.Rows[i]["PRECAUTION"].ToString().Split(','))
                                {
                                    if (fallAssessMeasure.Exists(x => x.Name == item))
                                        fallAssessMeasure.Find(x => x.Name == item).Num++;
                                }
                            }
                        }
                    }
                    fallAssessMeasure.Sort((x, y) => { return -x.Num.CompareTo(y.Num); });

                    string Row = "";
                    int Row_ = 0;
                    int total = fallAssessMeasure.Sum(x => x.Num);
                    foreach (FallAssessMeasure item in fallAssessMeasure)
                    {
                        if (Row != item.Num.ToString())
                        {
                            Row = item.Num.ToString();
                            Row_++;
                        }
                        temp = new Dictionary<string, string>();
                        temp["level"] = "第 " + Row_.ToString() + " 名";
                        temp["Name"] = item.Name;
                        temp["Num"] = item.Num.ToString();
                        if (total > 0)
                            temp["percent"] = (Math.Round((float.Parse(item.Num.ToString()) / total), 3) * 100).ToString();
                        else
                            temp["percent"] = "0";
                        dt_.Add(temp);
                    }
                    temp = new Dictionary<string, string>();
                    temp["level"] = "";
                    temp["Name"] = "總計";
                    temp["Num"] = total.ToString();
                    temp["percent"] = "";
                    dt_.Add(temp);
                    temp = new Dictionary<string, string>();
                    dt_.Add(temp);
                    temp = new Dictionary<string, string>();
                    temp["level"] = "備註";
                    dt_.Add(temp);
                    temp = new Dictionary<string, string>();
                    temp["level"] = "比率：(單措施內容合計/總計)*100%";
                    dt_.Add(temp);
                }

                List<string> Title = new List<string> { "排名", "措施內容", "合計", "比率(%)" };
                List<Dictionary<string, string>> Search_Title = new List<Dictionary<string, string>>();
                temp = new Dictionary<string, string>();
                temp["Name"] = "搜尋區間";
                temp["Value"] = Option["StartDate"] + "-" + Option["EndDate"];
                Search_Title.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Name"] = "單位";
                temp["Value"] = string.Join("、", JsonConvert.DeserializeObject<List<string>>(Option["CostName"]).ToArray());
                Search_Title.Add(temp);

                response(Title, Search_Title, dt_, "兒童跌倒危險性措施使用統計表");
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return new EmptyResult();
            }
        }

        [HttpPost]
        public ActionResult NutritionalAssessDetail(string OptionList)
        {
            try
            {
                List<Dictionary<string, string>> dt_ = new List<Dictionary<string, string>>();
                Dictionary<string, string> temp = null;
                Dictionary<string, string> Option = JsonConvert.DeserializeObject<Dictionary<string, string>>(OptionList);
                List<string> CostCode = JsonConvert.DeserializeObject<List<string>>(Option["CostCode"]);
                List<UserInfo> CostCodeUserList = new List<UserInfo>();
                byte[] TempByte = null;

                foreach (string CostCode_ in CostCode)
                {
                    TempByte = webService.GetUserList(CostCode_);
                    if (TempByte != null)
                        CostCodeUserList.AddRange(JsonConvert.DeserializeObject<List<UserInfo>>(CompressTool.DecompressString(TempByte)));
                }
                if (CostCodeUserList.Count > 0)
                {
                    string sql = "SELECT NUTRITIONAL_ASSESSMENT.*, v_clerk_data.* "
                       //foreach (UserInfo userNo in CostCodeUserList)
                       //  sql += ",'" + userNo.EmployeesNo.Trim() + "' ";
                       + "FROM  NUTRITIONAL_ASSESSMENT,v_clerk_data  WHERE TRIM(NUTRITIONAL_ASSESSMENT.CREANO) =TRIM(v_clerk_data.CLERK_ID) AND v_clerk_data.DEPT_NO IN (''";
                    foreach (string CostCode_ in CostCode)
                        sql += ",'" + CostCode_.Trim() + "' ";
                    sql += ") " + "AND NUTA_ASSESSMENT_DTM >= to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') "
                        + "AND NUTA_ASSESSMENT_DTM <= to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') "
                        + " AND NUTA_TYPE = 'adult' AND DELETED = 'N' "
                        + "ORDER BY NUTA_ASSESSMENT_DTM ";
                    DataTable Dt = ass_m.DBExecSQL(sql);
                    if (Dt.Rows.Count > 0)
                    {
                        PatientInfo pi = new PatientInfo();
                        string feeno = "";
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            if (feeno != Dt.Rows[i]["FEENO"].ToString())
                            {
                                feeno = Dt.Rows[i]["FEENO"].ToString();
                                TempByte = webService.GetPatientInfo(feeno);
                                if (TempByte != null)
                                    pi = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(TempByte));
                            }
                            temp = new Dictionary<string, string>();
                            temp["costCode"] = pi.CostCenterName;
                            temp["RECORDTIME_Date"] = Convert.ToDateTime(Dt.Rows[i]["NUTA_ASSESSMENT_DTM"].ToString()).ToString("yyyy/MM/dd");
                            temp["RECORDTIME_Time"] = Convert.ToDateTime(Dt.Rows[i]["NUTA_ASSESSMENT_DTM"].ToString()).ToString("HH:mm");
                            temp["ptName"] = pi.PatientName;
                            temp["ChartNo"] = pi.ChartNo;
                            temp["age"] = pi.Age.ToString();
                            temp["deptName"] = pi.DeptName;
                            temp["icd"] = pi.ICD9_code1;
                            temp["NUTA_HEIGHT"] = Dt.Rows[i]["NUTA_HEIGHT"].ToString();
                            temp["NUTA_WEIGHT"] = Dt.Rows[i]["NUTA_WEIGHT"].ToString();
                            temp["NUTA_BMI"] = Dt.Rows[i]["NUTA_BMI"].ToString();
                            temp["NUTA_ASSESSMENT_ROW1"] = Dt.Rows[i]["NUTA_ASSESSMENT_ROW1"].ToString();
                            temp["NUTA_ASSESSMENT_ROW2"] = Dt.Rows[i]["NUTA_ASSESSMENT_ROW2"].ToString();
                            temp["NUTA_ASSESSMENT_ROW3"] = Dt.Rows[i]["NUTA_ASSESSMENT_ROW3"].ToString();
                            temp["NUTA_ASSESSMENT_ROW4"] = Dt.Rows[i]["NUTA_ASSESSMENT_ROW4"].ToString();
                            temp["level"] = "";
                            if (!string.IsNullOrWhiteSpace(Dt.Rows[i]["NUTA_ASSESSMENT_ROW4"].ToString()))
                            {
                                if (int.Parse(Dt.Rows[i]["NUTA_ASSESSMENT_ROW4"].ToString()) >= 3)
                                    temp["level"] = "MUST≧3分";
                                else
                                    temp["level"] = "MUST<3分";
                            }
                            dt_.Add(temp);
                        }
                    }
                }

                List<string> Title = new List<string> { "單位", "日期", "時間", "病人姓名", "病歷號", "年齡", "科別", "診斷"
                                , "入院身高", "體重", "BMI", "身體質量指數", "三到六個月體重減輕", "急性疾病狀態幾乎無法進食或禁食", "MUST總分", "篩選結果"};
                List<Dictionary<string, string>> Search_Title = new List<Dictionary<string, string>>();
                temp = new Dictionary<string, string>();
                temp["Name"] = "搜尋區間";
                temp["Value"] = Option["StartDate"] + "-" + Option["EndDate"];
                Search_Title.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Name"] = "單位";
                temp["Value"] = string.Join("、", JsonConvert.DeserializeObject<List<string>>(Option["CostName"]).ToArray());
                Search_Title.Add(temp);

                response(Title, Search_Title, dt_, "營養評估明細表");
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return new EmptyResult();
            }
        }

        [HttpPost]
        public ActionResult NutritionalAssessStatistics(string OptionList)
        {
            try
            {
                List<Dictionary<string, string>> dt_ = new List<Dictionary<string, string>>();
                Dictionary<string, string> temp;
                Dictionary<string, string> Option = JsonConvert.DeserializeObject<Dictionary<string, string>>(OptionList);
                List<string> CostCode = JsonConvert.DeserializeObject<List<string>>(Option["CostCode"]);
                List<UserInfo> CostCodeUserList = new List<UserInfo>();
                byte[] TempByte = null;

                temp = new Dictionary<string, string>();
                temp["title"] = "MUST<3分";
                temp["num"] = "0";
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                temp["title"] = "MUST≧3分";
                temp["num"] = "0";
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                temp["title"] = "≧3分比率(%)";
                temp["num"] = "0";
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                temp["title"] = "備註";
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                temp["title"] = "MUST≧3分比率=(MUST≧3分人數/(MUST<3分+MUST≧3分))*100%";
                dt_.Add(temp);

                foreach (string CostCode_ in CostCode)
                {
                    TempByte = webService.GetUserList(CostCode_);
                    if (TempByte != null)
                        CostCodeUserList.AddRange(JsonConvert.DeserializeObject<List<UserInfo>>(CompressTool.DecompressString(TempByte)));
                }
                if (CostCodeUserList.Count > 0)
                {
                    string sql = "SELECT NUTRITIONAL_ASSESSMENT.*, v_clerk_data.* "
                       //foreach (UserInfo userNo in CostCodeUserList)
                       //  sql += ",'" + userNo.EmployeesNo.Trim() + "' ";
                       + "FROM  NUTRITIONAL_ASSESSMENT,v_clerk_data  WHERE TRIM(NUTRITIONAL_ASSESSMENT.CREANO) =TRIM(v_clerk_data.CLERK_ID) AND v_clerk_data.DEPT_NO IN (''";
                    foreach (string CostCode_ in CostCode)
                        sql += ",'" + CostCode_.Trim() + "' ";
                    sql += ") " + "AND NUTA_ASSESSMENT_DTM >= to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') "
                        + "AND NUTA_ASSESSMENT_DTM <= to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') "
                        + " AND NUTA_TYPE = 'adult' AND DELETED = 'N' "
                        + "ORDER BY NUTA_ASSESSMENT_DTM ";
                    DataTable Dt = ass_m.DBExecSQL(sql);
                    if (Dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            if (!string.IsNullOrWhiteSpace(Dt.Rows[i]["NUTA_ASSESSMENT_ROW4"].ToString()))
                            {
                                if (int.Parse(Dt.Rows[i]["NUTA_ASSESSMENT_ROW4"].ToString()) >= 3)
                                    dt_[1]["num"] = (int.Parse(dt_[1]["num"]) + 1).ToString();
                                else
                                    dt_[0]["num"] = (int.Parse(dt_[0]["num"]) + 1).ToString();
                            }
                        }
                    }
                    if (dt_[0]["num"] != "0" && dt_[1]["num"] != "0")
                        dt_[2]["num"] = (Math.Round((float.Parse(dt_[1]["num"]) / (int.Parse(dt_[0]["num"]) + int.Parse(dt_[1]["num"]))), 3) * 100).ToString() + "%";

                }

                List<string> Title = new List<string> { "營養評估", "人數" };
                List<Dictionary<string, string>> Search_Title = new List<Dictionary<string, string>>();
                temp = new Dictionary<string, string>();
                temp["Name"] = "搜尋區間";
                temp["Value"] = Option["StartDate"] + "-" + Option["EndDate"];
                Search_Title.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Name"] = "單位";
                temp["Value"] = string.Join("、", JsonConvert.DeserializeObject<List<string>>(Option["CostName"]).ToArray());
                Search_Title.Add(temp);

                response(Title, Search_Title, dt_, "營養評估統計表");
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return new EmptyResult();
            }
        }

        [HttpPost]
        public ActionResult PressureSoreDetail(string OptionList)
        {
            try
            {
                List<Dictionary<string, string>> dt_ = new List<Dictionary<string, string>>();
                Dictionary<string, string> temp = null;
                Dictionary<string, string> Option = JsonConvert.DeserializeObject<Dictionary<string, string>>(OptionList);
                List<string> CostCode = JsonConvert.DeserializeObject<List<string>>(Option["CostCode"]);
                List<UserInfo> CostCodeUserList = new List<UserInfo>();
                byte[] TempByte = null;

                foreach (string CostCode_ in CostCode)
                {
                    TempByte = webService.GetUserList(CostCode_);
                    if (TempByte != null)
                        CostCodeUserList.AddRange(JsonConvert.DeserializeObject<List<UserInfo>>(CompressTool.DecompressString(TempByte)));
                }
                if (CostCodeUserList.Count > 0)
                {
                    string sql = "SELECT NIS_PRESSURE_SORE_DATA.*, v_clerk_data.* "
                       //foreach (UserInfo userNo in CostCodeUserList)
                       //  sql += ",'" + userNo.EmployeesNo.Trim() + "' ";
                       + "FROM  NIS_PRESSURE_SORE_DATA,v_clerk_data  WHERE TRIM(NIS_PRESSURE_SORE_DATA.CREATNO) =TRIM(v_clerk_data.CLERK_ID) AND v_clerk_data.DEPT_NO IN (''";
                    foreach (string CostCode_ in CostCode)
                        sql += ",'" + CostCode_.Trim() + "' ";
                    sql += ") " + "AND RECORDTIME >= to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') "
                        + "AND RECORDTIME <= to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') "
                        + "ORDER BY RECORDTIME ";

                    DataTable Dt = ass_m.DBExecSQL(sql);
                    if (Dt.Rows.Count > 0)
                    {
                        PatientInfo pi = new PatientInfo();
                        string feeno = "";
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            if (feeno != Dt.Rows[i]["FEENO"].ToString())
                            {
                                feeno = Dt.Rows[i]["FEENO"].ToString();
                                TempByte = webService.GetPatientInfo(feeno);
                                if (TempByte != null)
                                    pi = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(TempByte));
                            }
                            temp = new Dictionary<string, string>();
                            temp["costCode"] = pi.CostCenterName;
                            temp["ptName"] = pi.PatientName;
                            temp["ChartNo"] = pi.ChartNo;
                            temp["age"] = pi.Age.ToString();
                            temp["deptName"] = pi.DeptName;
                            temp["icd"] = pi.ICD9_code1;
                            temp["NUTRITION"] = Dt.Rows[i]["NUTRITION"].ToString();
                            temp["PERCEPTION"] = Dt.Rows[i]["PERCEPTION"].ToString();
                            temp["DAMP"] = Dt.Rows[i]["DAMP"].ToString();
                            temp["ACTIVITY"] = Dt.Rows[i]["ACTIVITY"].ToString();
                            temp["MOVING"] = Dt.Rows[i]["MOVING"].ToString();
                            temp["FRICTION"] = Dt.Rows[i]["FRICTION"].ToString();
                            temp["TOTAL"] = Dt.Rows[i]["TOTAL"].ToString();
                            if (int.Parse(Dt.Rows[i]["TOTAL"].ToString()) < 19)
                                temp["Level"] = "輕度危險性";
                            if (int.Parse(Dt.Rows[i]["TOTAL"].ToString()) < 15)
                                temp["Level"] = "中度危險性";
                            if (int.Parse(Dt.Rows[i]["TOTAL"].ToString()) < 13)
                                temp["Level"] = "高度危險性";
                            if (int.Parse(Dt.Rows[i]["TOTAL"].ToString()) <= 9)
                                temp["Level"] = "極高度危險性";
                            else
                                temp["Level"] = "無危險性";
                            temp["HAVE"] = Dt.Rows[i]["HAVE"].ToString();
                            dt_.Add(temp);
                        }
                    }
                }

                List<string> Title = new List<string> { "單位", "病人姓名", "病歷號", "年齡", "科別", "診斷"
                                 , "營養狀態", "感覺知覺", "潮濕程度", "活動力", "移動力", "摩擦力和剪力", "總分", "壓傷危險等級", "有無壓傷" };
                List<Dictionary<string, string>> Search_Title = new List<Dictionary<string, string>>();
                temp = new Dictionary<string, string>();
                temp["Name"] = "搜尋區間";
                temp["Value"] = Option["StartDate"] + "-" + Option["EndDate"];
                Search_Title.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Name"] = "單位";
                temp["Value"] = string.Join("、", JsonConvert.DeserializeObject<List<string>>(Option["CostName"]).ToArray());
                Search_Title.Add(temp);

                response(Title, Search_Title, dt_, "壓傷危險因子明細表");
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return new EmptyResult();
            }
        }

        [HttpPost]
        public ActionResult PressureSoreStatistics(string OptionList)
        {
            try
            {
                List<Dictionary<string, string>> dt_ = new List<Dictionary<string, string>>();
                Dictionary<string, string> temp = null;
                Dictionary<string, string> Option = JsonConvert.DeserializeObject<Dictionary<string, string>>(OptionList);
                List<string> CostCode = JsonConvert.DeserializeObject<List<string>>(Option["CostCode"]);
                List<UserInfo> CostCodeUserList = new List<UserInfo>();
                byte[] TempByte = null;

                temp = new Dictionary<string, string>();
                temp["Title"] = "19-23分:無危險性";
                temp["Num"] = "0";
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Title"] = "15-18分:輕度危險性";
                temp["Num"] = "0";
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Title"] = "13-14分:中度危險性";
                temp["Num"] = "0";
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Title"] = "10-12分:高度危險性";
                temp["Num"] = "0";
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Title"] = "≦9分:極高度危險性";
                temp["Num"] = "0";
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Title"] = "合計";
                temp["Num"] = "0";
                dt_.Add(temp);

                foreach (string CostCode_ in CostCode)
                {
                    TempByte = webService.GetUserList(CostCode_);
                    if (TempByte != null)
                        CostCodeUserList.AddRange(JsonConvert.DeserializeObject<List<UserInfo>>(CompressTool.DecompressString(TempByte)));
                }
                if (CostCodeUserList.Count > 0)
                {
                    string sql = "SELECT NIS_PRESSURE_SORE_DATA.*, v_clerk_data.* "
                       //foreach (UserInfo userNo in CostCodeUserList)
                       //  sql += ",'" + userNo.EmployeesNo.Trim() + "' ";
                       + "FROM  NIS_PRESSURE_SORE_DATA,v_clerk_data  WHERE TRIM(NIS_PRESSURE_SORE_DATA.CREATNO) =TRIM(v_clerk_data.CLERK_ID) AND v_clerk_data.DEPT_NO IN (''";
                    foreach (string CostCode_ in CostCode)
                        sql += ",'" + CostCode_.Trim() + "' ";
                    sql += ") " + "AND RECORDTIME >= to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') "
                        + "AND RECORDTIME <= to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') "
                        + "ORDER BY RECORDTIME ";

                    DataTable Dt = ass_m.DBExecSQL(sql);
                    if (Dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            if (int.Parse(Dt.Rows[i]["TOTAL"].ToString()) <= 9)
                                dt_[4]["Num"] = (int.Parse(dt_[4]["Num"]) + 1).ToString();
                            else if (int.Parse(Dt.Rows[i]["TOTAL"].ToString()) < 13)
                                dt_[3]["Num"] = (int.Parse(dt_[3]["Num"]) + 1).ToString();
                            else if (int.Parse(Dt.Rows[i]["TOTAL"].ToString()) < 15)
                                dt_[2]["Num"] = (int.Parse(dt_[2]["Num"]) + 1).ToString();
                            else if (int.Parse(Dt.Rows[i]["TOTAL"].ToString()) < 19)
                                dt_[1]["Num"] = (int.Parse(dt_[1]["Num"]) + 1).ToString();
                            else
                                dt_[0]["Num"] = (int.Parse(dt_[0]["Num"]) + 1).ToString();
                        }
                        dt_[5]["Num"] = (int.Parse(dt_[0]["Num"]) + int.Parse(dt_[1]["Num"])
                           + int.Parse(dt_[2]["Num"]) + int.Parse(dt_[3]["Num"])
                           + int.Parse(dt_[4]["Num"])).ToString();
                    }
                }

                List<string> Title = new List<string> { "壓傷危險等級", "人數" };
                List<Dictionary<string, string>> Search_Title = new List<Dictionary<string, string>>();
                temp = new Dictionary<string, string>();
                temp["Name"] = "搜尋區間";
                temp["Value"] = Option["StartDate"] + "-" + Option["EndDate"];
                Search_Title.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Name"] = "單位";
                temp["Value"] = string.Join("、", JsonConvert.DeserializeObject<List<string>>(Option["CostName"]).ToArray());
                Search_Title.Add(temp);

                response(Title, Search_Title, dt_, "壓傷危險因子統計表");
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return new EmptyResult();
            }
        }

        [HttpPost]
        public ActionResult DischargedCareDetail(string OptionList)
        {
            try
            {
                List<Dictionary<string, string>> dt_ = new List<Dictionary<string, string>>();
                Dictionary<string, string> temp = null;
                Dictionary<string, string> Option = JsonConvert.DeserializeObject<Dictionary<string, string>>(OptionList);
                List<string> CostCode = JsonConvert.DeserializeObject<List<string>>(Option["CostCode"]);
                List<UserInfo> CostCodeUserList = new List<UserInfo>();
                byte[] TempByte = null;

                foreach (string CostCode_ in CostCode)
                {
                    TempByte = webService.GetUserList(CostCode_);
                    if (TempByte != null)
                        CostCodeUserList.AddRange(JsonConvert.DeserializeObject<List<UserInfo>>(CompressTool.DecompressString(TempByte)));
                }
                if (CostCodeUserList.Count > 0)
                {
                    string sql = "SELECT DISCHARGED_CARE.*, v_clerk_data.* "
                       //foreach (UserInfo userNo in CostCodeUserList)
                       //  sql += ",'" + userNo.EmployeesNo.Trim() + "' ";
                       + "FROM  DISCHARGED_CARE,v_clerk_data  WHERE TRIM(DISCHARGED_CARE.CREATE_USER) =TRIM(v_clerk_data.CLERK_ID) AND v_clerk_data.DEPT_NO IN (''";
                    foreach (string CostCode_ in CostCode)
                        sql += ",'" + CostCode_.Trim() + "' ";
                    sql += ") " + "AND ASSESS_DATE >= '" + Option["StartDate"] + "' "
                        + "AND ASSESS_DATE <= '" + Option["EndDate"] + "' "
                        + "ORDER BY ASSESS_DATE || FILTER_STATE ";
                    DataTable Dt = ass_m.DBExecSQL(sql);
                    if (Dt.Rows.Count > 0)
                    {
                        PatientInfo pi = new PatientInfo();
                        string feeno = "";
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            if (feeno != Dt.Rows[i]["FEE_NO"].ToString())
                            {
                                feeno = Dt.Rows[i]["FEE_NO"].ToString();
                                TempByte = webService.GetPatientInfo(feeno);
                                if (TempByte != null)
                                    pi = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(TempByte));
                            }
                            temp = new Dictionary<string, string>();
                            temp["costCode"] = pi.CostCenterName;
                            temp["ASSESS_DATE"] = Dt.Rows[i]["ASSESS_DATE"].ToString();
                            temp["ASSESS_TIME"] = Dt.Rows[i]["ASSESS_TIME"].ToString();
                            temp["BedNo"] = pi.BedNo;
                            temp["ptName"] = pi.PatientName;
                            temp["ChartNo"] = pi.ChartNo;
                            temp["age"] = pi.Age.ToString();
                            temp["deptName"] = pi.DeptName;
                            temp["icd"] = pi.ICD9_code1;
                            temp["FILTER_STATE"] = (!string.IsNullOrWhiteSpace(Dt.Rows[i]["FILTER_STATE"].ToString())) ?
                                Dt.Rows[i]["FILTER_STATE"].ToString().Replace("0", "新病人")
                                .Replace("1", "手術")
                                .Replace("2", "ICU轉入")
                                .Replace("3", "病情變化") : "";
                            temp["DAILY_LIFE"] = (!string.IsNullOrWhiteSpace(Dt.Rows[i]["DAILY_LIFE"].ToString())) ?
                                Dt.Rows[i]["DAILY_LIFE"].ToString().Replace("0", "完全自理")
                                .Replace("1", "部分依賴他人")
                                .Replace("2", "完全依賴他人轉入") : "";
                            if (Dt.Rows[i]["SKIN_STATE"].ToString() == "0")
                                temp["SKIN_STATE"] = "無傷口";
                            else if (Dt.Rows[i]["SKIN_STATE"].ToString() == "1")
                            {
                                temp["SKIN_STATE"] = "一般傷口-" + Dt.Rows[i]["SKIN_WOUND"].ToString().Replace("0", "傷口").Replace("1", "人工造口");
                            }
                            else if (Dt.Rows[i]["SKIN_STATE"].ToString() == "2")
                                temp["SKIN_STATE"] = "壓傷";
                            else
                                temp["SKIN_STATE"] = "";
                            temp["SOCIAL_SUPPORT"] = (!string.IsNullOrWhiteSpace(Dt.Rows[i]["SOCIAL_SUPPORT"].ToString())) ?
                                Dt.Rows[i]["SOCIAL_SUPPORT"].ToString().Replace("0", "可自我照顧，家屬(外傭)有能力照顧病人")
                                .Replace("1", "有家屬但缺乏照顧人力(包括看護工)")
                                .Replace("2", "無家屬") : "";
                            if (Dt.Rows[i]["TUBE_REQUEST"].ToString() == "0")
                                temp["TUBE_REQUEST"] = "無";
                            else if (Dt.Rows[i]["TUBE_REQUEST"].ToString() == "1")
                            {
                                temp["TUBE_REQUEST"] = "有-" + Dt.Rows[i]["TUBE_TYPE"].ToString().Replace("0", "鼻胃管").Replace("1", "導尿管").Replace("2", "氣切管").Replace("99", Dt.Rows[i]["TUBE_OTHER"].ToString());
                            }
                            else
                                temp["TUBE_REQUEST"] = "";
                            temp["CONTINENCE"] = (!string.IsNullOrWhiteSpace(Dt.Rows[i]["CONTINENCE"].ToString())) ?
                                Dt.Rows[i]["CONTINENCE"].ToString().Replace("0", "可自行控制")
                                .Replace("1", "失禁一個月內")
                                .Replace("2", "失禁一個月以上") : "";
                            temp["USAGE_VENTILATOR"] = (!string.IsNullOrWhiteSpace(Dt.Rows[i]["USAGE_VENTILATOR"].ToString())) ?
                                Dt.Rows[i]["USAGE_VENTILATOR"].ToString().Replace("0", "不需要")
                                .Replace("1", "持續使用一個月內")
                                .Replace("2", "持續使用一個月以上") : "";
                            temp["USAGE_OXYGEN"] = (!string.IsNullOrWhiteSpace(Dt.Rows[i]["USAGE_OXYGEN"].ToString())) ?
                                Dt.Rows[i]["USAGE_OXYGEN"].ToString().Replace("0", "不需要")
                                .Replace("1", "PRN使用")
                                .Replace("2", "持續使用") : "";
                            temp["MOMENTUS"] = (!string.IsNullOrWhiteSpace(Dt.Rows[i]["MOMENTUS"].ToString())) ?
                                Dt.Rows[i]["MOMENTUS"].ToString().Replace("0", "門診追蹤")
                                .Replace("1", "居家護理")
                                .Replace("2", "長期照護")
                                .Replace("3", "IDS-4") : "";
                            temp["TOTAL"] = Dt.Rows[i]["TOTAL"].ToString();
                            if (Dt.Rows[i]["CASE_STATE"].ToString().Equals("Y"))
                                temp["Dischareged"] = "收案";
                            else if (Dt.Rows[i]["CASE_STATE"].ToString().Equals("N"))
                                temp["Dischareged"] = "不收案";
                            else
                                temp["Dischareged"] = "";//2019/3/4修改, 原:待收案
                            dt_.Add(temp);
                        }
                    }
                }

                List<string> Title = new List<string> { "單位", "日期", "時間", "床號", "病人姓名", "病歷號", "年齡", "科別", "診斷"
                                 , "篩選狀態", "日常生活", "皮膚狀況", "社會支持", "居家導管照護需求", "大小便控制", "使用呼吸器", "使用氧氣", "病人動向", "總分", "收案狀態" };
                List<Dictionary<string, string>> Search_Title = new List<Dictionary<string, string>>();
                temp = new Dictionary<string, string>();
                temp["Name"] = "搜尋區間";
                temp["Value"] = Option["StartDate"] + "-" + Option["EndDate"];
                Search_Title.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Name"] = "單位";
                temp["Value"] = string.Join("、", JsonConvert.DeserializeObject<List<string>>(Option["CostName"]).ToArray());
                Search_Title.Add(temp);

                response(Title, Search_Title, dt_, "完成出院準備服務篩選明細表");
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return new EmptyResult();
            }
        }

        [HttpPost]
        public ActionResult InHospWoundPressureStatistics(string OptionList)
        {
            try
            {
                List<Dictionary<string, string>> dt_ = new List<Dictionary<string, string>>();
                Dictionary<string, string> temp = null;
                Dictionary<string, string> Option = JsonConvert.DeserializeObject<Dictionary<string, string>>(OptionList);
                List<string> CostCode = JsonConvert.DeserializeObject<List<string>>(Option["CostCode"]);
                List<string> CostName = JsonConvert.DeserializeObject<List<string>>(Option["CostName"]);
                List<UserInfo> CostCodeUserList = new List<UserInfo>();
                byte[] TempByte = null;

                foreach (string Name in CostName)
                {
                    temp = new Dictionary<string, string>();
                    temp["CostName"] = Name;
                    temp["薦骨"] = "0";
                    temp["坐骨"] = "0";
                    temp["股骨粗隆"] = "0";
                    temp["跟骨"] = "0";
                    temp["足踝"] = "0";
                    temp["肩胛骨"] = "0";
                    temp["枕骨"] = "0";
                    temp["顏面壓傷"] = "0";
                    temp["其他"] = "0";
                    dt_.Add(temp);
                }

                foreach (string CostCode_ in CostCode)
                {
                    TempByte = webService.GetUserList(CostCode_);
                    if (TempByte != null)
                        CostCodeUserList.AddRange(JsonConvert.DeserializeObject<List<UserInfo>>(CompressTool.DecompressString(TempByte)));
                }
                if (CostCodeUserList.Count > 0)
                {
                    string sql = "SELECT WOUND_DATA.*, v_clerk_data.* "
                       //foreach (UserInfo userNo in CostCodeUserList)
                       //  sql += ",'" + userNo.EmployeesNo.Trim() + "' ";
                       + "FROM  WOUND_DATA,v_clerk_data  WHERE TRIM(WOUND_DATA.CREANO) =TRIM(v_clerk_data.CLERK_ID) AND v_clerk_data.DEPT_NO IN (''";
                    foreach (string CostCode_ in CostCode)
                        sql += ",'" + CostCode_.Trim() + "' ";
                    sql += ") " + "AND CREATTIME IS NOT NULL AND TYPE IN ('壓瘡', '壓傷') "
                        + "AND LOCATION IN('" + base.userinfo.CostCenterName + "' ";
                    foreach (string location in JsonConvert.DeserializeObject<List<string>>(Option["CostName"]).ToArray())
                        sql += ",'" + location + "' ";
                    sql += ") " + "AND CREATTIME >= to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') "
                        + "AND CREATTIME <= to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') "
                        + "AND DELETED IS NULL "
                        + "ORDER BY CREATTIME ";

                    DataTable Dt = ass_m.DBExecSQL(sql);
                    if (Dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            temp = dt_.Find(x => x["CostName"] == Dt.Rows[i]["LOCATION"].ToString());
                            if (temp != null)
                            {
                                switch (Dt.Rows[i]["POSITION"].ToString())
                                {
                                    case "右腸骨瘠":
                                    case "左腸骨瘠":
                                    case "右膝":
                                    case "左膝":
                                    case "右手肘":
                                    case "左手肘":
                                    case "腹部":
                                    case "胸部":
                                    case "背部":
                                    case "腰部":
                                    case "右臀":
                                    case "左臀":
                                    case "會陰":
                                    default:
                                        temp["其他"] = (int.Parse(temp["其他"]) + 1).ToString();
                                        break;
                                    case "右大粗隆":
                                    case "左大粗隆":
                                        temp["股骨粗隆"] = (int.Parse(temp["股骨粗隆"]) + 1).ToString();
                                        break;
                                    case "枕骨":
                                        temp["枕骨"] = (int.Parse(temp["枕骨"]) + 1).ToString();
                                        break;
                                    case "右肩胛骨":
                                    case "左肩胛骨":
                                        temp["肩胛骨"] = (int.Parse(temp["肩胛骨"]) + 1).ToString();
                                        break;
                                    case "薦骨":
                                        temp["薦骨"] = (int.Parse(temp["薦骨"]) + 1).ToString();
                                        break;
                                    case "右坐骨粗隆":
                                    case "左坐骨粗隆":
                                        temp["坐骨"] = (int.Parse(temp["坐骨"]) + 1).ToString();
                                        break;
                                    case "右踝骨外側":
                                    case "左踝骨外側":
                                        temp["足踝"] = (int.Parse(temp["足踝"]) + 1).ToString();
                                        break;
                                    case "右足跟":
                                    case "左足跟":
                                        temp["跟骨"] = (int.Parse(temp["跟骨"]) + 1).ToString();
                                        break;
                                    case "右臉":
                                    case "左臉":
                                        if (Dt.Rows[i]["REASON"].ToString().IndexOf("醫療裝置壓迫") > -1)
                                            temp["顏面壓傷"] = (int.Parse(temp["顏面壓傷"]) + 1).ToString();
                                        else
                                            temp["其他"] = (int.Parse(temp["其他"]) + 1).ToString();
                                        break;
                                }
                            }
                        }
                    }
                }
                temp = new Dictionary<string, string>();
                temp["CostName"] = "合計";
                temp["薦骨"] = dt_.Sum(x => int.Parse(x["薦骨"])).ToString();
                temp["坐骨"] = dt_.Sum(x => int.Parse(x["坐骨"])).ToString();
                temp["股骨粗隆"] = dt_.Sum(x => int.Parse(x["股骨粗隆"])).ToString();
                temp["跟骨"] = dt_.Sum(x => int.Parse(x["跟骨"])).ToString();
                temp["足踝"] = dt_.Sum(x => int.Parse(x["足踝"])).ToString();
                temp["肩胛骨"] = dt_.Sum(x => int.Parse(x["肩胛骨"])).ToString();
                temp["枕骨"] = dt_.Sum(x => int.Parse(x["枕骨"])).ToString();
                temp["顏面壓傷"] = dt_.Sum(x => int.Parse(x["顏面壓傷"])).ToString();
                temp["其他"] = dt_.Sum(x => int.Parse(x["其他"])).ToString();
                dt_.Add(temp);

                List<string> Title = new List<string> { "單位", "薦骨", "坐骨", "股骨粗隆", "跟骨", "足踝", "肩胛骨", "枕骨", "因醫療裝置壓迫引起的顏面壓傷", "其他" };
                List<Dictionary<string, string>> Search_Title = new List<Dictionary<string, string>>();
                temp = new Dictionary<string, string>();
                temp["Name"] = "搜尋區間";
                temp["Value"] = Option["StartDate"] + "-" + Option["EndDate"];
                Search_Title.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Name"] = "單位";
                temp["Value"] = string.Join("、", JsonConvert.DeserializeObject<List<string>>(Option["CostName"]).ToArray());
                Search_Title.Add(temp);

                response(Title, Search_Title, dt_, "院內發生壓傷部位統計表");
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return new EmptyResult();
            }
        }

        [HttpPost]
        public ActionResult WoundPressureDetail(string OptionList)
        {
            try
            {
                List<Dictionary<string, string>> dt_ = new List<Dictionary<string, string>>();
                Dictionary<string, string> temp = null;
                Dictionary<string, string> Option = JsonConvert.DeserializeObject<Dictionary<string, string>>(OptionList);
                List<string> CostCode = JsonConvert.DeserializeObject<List<string>>(Option["CostCode"]);
                List<UserInfo> CostCodeUserList = new List<UserInfo>();
                byte[] TempByte = null;

                foreach (string CostCode_ in CostCode)
                {
                    TempByte = webService.GetUserList(CostCode_);
                    if (TempByte != null)
                        CostCodeUserList.AddRange(JsonConvert.DeserializeObject<List<UserInfo>>(CompressTool.DecompressString(TempByte)));
                }
                if (CostCodeUserList.Count > 0)
                {
                    List<Dictionary<string, string>> tempDt = new List<Dictionary<string, string>>();

                    string sql = "SELECT WOUND_DATA.*, v_clerk_data.* "
                       + "FROM  WOUND_DATA,v_clerk_data  WHERE TRIM(WOUND_DATA.CREANO) =TRIM(v_clerk_data.CLERK_ID) AND v_clerk_data.DEPT_NO IN (''";
                    foreach (string CostCode_ in CostCode)
                        sql += ",'" + CostCode_.Trim() + "' ";
                    sql += ") AND CREATTIME IS NOT NULL AND TYPE IN ('壓瘡', '壓傷') "
                        + "AND LOCATION IN('" + base.userinfo.CostCenterName + "' ";
                    foreach (string location in JsonConvert.DeserializeObject<List<string>>(Option["CostName"]).ToArray())
                        sql += ",'" + location + "' ";
                    sql += ") " + "AND CREATTIME >= to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') "
                        + "AND CREATTIME <= to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') "
                        + "AND DELETED IS NULL "
                        + "ORDER BY CREATTIME ";

                    DataTable Dt = ass_m.DBExecSQL(sql);
                    if (Dt.Rows.Count > 0)
                    {
                        PatientInfo pi = new PatientInfo();
                        string feeno = "";
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            if (feeno != Dt.Rows[i]["FEENO"].ToString())
                            {
                                feeno = Dt.Rows[i]["FEENO"].ToString();
                                TempByte = webService.GetPatientInfo(feeno);
                                if (TempByte != null)
                                    pi = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(TempByte));
                            }

                            temp = new Dictionary<string, string>();
                            temp["InDate"] = pi.InDate.ToString("yyyy/MM/dd HH:mm");
                            temp["CREATTIME"] = Convert.ToDateTime(Dt.Rows[i]["CREATTIME"].ToString()).ToString("yyyy/MM/dd");
                            temp["LOCATION"] = Dt.Rows[i]["LOCATION"].ToString();
                            temp["OutDate"] = pi.OutDate.ToString("yyyy/MM/dd HH:mm");
                            temp["PatientName"] = pi.PatientName;
                            temp["ChartNo"] = pi.ChartNo;
                            temp["POSITION"] = Dt.Rows[i]["POSITION"].ToString();
                            temp["REASON"] = Dt.Rows[i]["REASON"].ToString();
                            temp["WOUND_ID"] = Dt.Rows[i]["WOUND_ID"].ToString();
                            tempDt.Add(temp);
                        }
                    }

                    if (tempDt.Count > 0)
                    {
                        UserInfo tempUser;
                        sql = "SELECT WOUND_RECORD.*, v_clerk_data.* "
                            + " FROM  WOUND_RECORD,v_clerk_data  WHERE TRIM(WOUND_RECORD.CREANO) =TRIM(v_clerk_data.CLERK_ID) AND v_clerk_data.DEPT_NO IN (''";
                        foreach (string CostCode_ in CostCode)
                            sql += ",'" + CostCode_.Trim() + "' ";
                        sql += ") AND WOUND_ID = '{0}' "
                            + "AND RECORDTIME >= to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') "
                            + "AND RECORDTIME <= to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') "
                            + "AND DELETED IS NULL "
                            + "ORDER BY RECORDTIME ";
                        foreach (var item in tempDt)
                        {
                            Dt = ass_m.DBExecSQL(sql);
                            if (Dt.Rows.Count > 0)
                            {
                                for (int i = 0; i < Dt.Rows.Count; i++)
                                {
                                    temp = new Dictionary<string, string>();
                                    temp["InDate"] = item["InDate"];
                                    temp["CREATTIME"] = item["CREATTIME"];
                                    temp["LOCATION"] = item["LOCATION"];
                                    temp["OutDate"] = item["OutDate"];
                                    temp["PatientName"] = item["PatientName"];
                                    temp["ChartNo"] = item["ChartNo"];
                                    temp["POSITION"] = item["POSITION"];
                                    temp["REASON"] = item["REASON"];
                                    temp["GRADE"] = Dt.Rows[i]["GRADE"].ToString();
                                    if (Dt.Rows[i]["RANGE_YN"].ToString() == "是")
                                    {
                                        temp["RANGE"] = "{0}x{1}x{2}";
                                        temp["RANGE"] = string.Format(temp["RANGE"]
                                            , ((!string.IsNullOrWhiteSpace(Dt.Rows[i]["RANGE_HEIGHT"].ToString())) ? Dt.Rows[i]["RANGE_HEIGHT"].ToString() : "")
                                            , ((!string.IsNullOrWhiteSpace(Dt.Rows[i]["RANGE_WIDTH"].ToString())) ? Dt.Rows[i]["RANGE_WIDTH"].ToString() : "")
                                            , ((!string.IsNullOrWhiteSpace(Dt.Rows[i]["RANGE_DEPTH"].ToString())) ? Dt.Rows[i]["RANGE_DEPTH"].ToString() : ""));
                                    }
                                    else
                                        temp["RANGE"] = "";
                                    temp["EXTERIOR"] = Dt.Rows[i]["EXTERIOR"].ToString().Replace("其他", Dt.Rows[i]["EXTERIOR_OTHER"].ToString()).Replace("|", ",");
                                    temp["CREANO"] = Dt.Rows[i]["CREANO"].ToString();
                                    tempUser = CostCodeUserList.Find(x => x.EmployeesNo == Dt.Rows[i]["CREANO"].ToString());
                                    if (tempUser == null && Dt.Rows[i]["CREANO"].ToString() == base.userinfo.EmployeesNo)
                                    {
                                        temp["CostCenterName"] = base.userinfo.CostCenterName;
                                        temp["EmployeesName"] = base.userinfo.EmployeesName;
                                    }
                                    else
                                    {
                                        temp["CostCenterName"] = tempUser.CostCenterName;
                                        temp["EmployeesName"] = tempUser.EmployeesName;
                                    }

                                    dt_.Add(temp);
                                }
                            }
                        }
                    }
                }

                List<string> Title = new List<string> { "住院日期", "發生日期", "發生地點", "出院日期", "病人姓名", "病歷號"
                                 , "發生部位", "發生原因", "壓傷分級", "範圍", "外觀", "員工編號", "紀錄單位", "紀錄者" };
                List<Dictionary<string, string>> Search_Title = new List<Dictionary<string, string>>();
                temp = new Dictionary<string, string>();
                temp["Name"] = "搜尋區間";
                temp["Value"] = Option["StartDate"] + "-" + Option["EndDate"];
                Search_Title.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Name"] = "單位";
                temp["Value"] = string.Join("、", JsonConvert.DeserializeObject<List<string>>(Option["CostName"]).ToArray());
                Search_Title.Add(temp);

                response(Title, Search_Title, dt_, "壓傷明細表");
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return new EmptyResult();
            }
        }

        [HttpPost]
        public ActionResult WoundPressureStatistics(string OptionList)
        {
            try
            {
                List<Dictionary<string, string>> dt_ = new List<Dictionary<string, string>>();
                Dictionary<string, string> temp = null;
                Dictionary<string, string> Option = JsonConvert.DeserializeObject<Dictionary<string, string>>(OptionList);
                List<string> CostCode = JsonConvert.DeserializeObject<List<string>>(Option["CostCode"]);
                List<string> CostCodeName = JsonConvert.DeserializeObject<List<string>>(Option["CostName"]);
                List<UserInfo> CostCodeUserList = new List<UserInfo>();
                byte[] TempByte = null;
                int IN = 0, TRAN = 0;

                foreach (string CostName_ in CostCodeName)
                {
                    temp = new Dictionary<string, string>();
                    temp["CostName"] = CostName_;
                    temp["IN"] = "0";
                    temp["TRAN"] = "0";
                    dt_.Add(temp);
                }

                foreach (string CostCode_ in CostCode)
                {
                    TempByte = webService.GetUserList(CostCode_);
                    if (TempByte != null)
                        CostCodeUserList.AddRange(JsonConvert.DeserializeObject<List<UserInfo>>(CompressTool.DecompressString(TempByte)));
                }
                if (CostCodeUserList.Count > 0)
                {
                    List<Dictionary<string, string>> tempDt = new List<Dictionary<string, string>>();

                    string sql = "SELECT WOUND_DATA.*, v_clerk_data.* "
                       + "FROM  WOUND_DATA,v_clerk_data  WHERE TRIM(WOUND_DATA.CREANO) =TRIM(v_clerk_data.CLERK_ID) AND v_clerk_data.DEPT_NO IN (''";
                    foreach (string CostCode_ in CostCode)
                        sql += ",'" + CostCode_.Trim() + "' ";
                    sql += ") AND CREATTIME IS NOT NULL AND TYPE IN ('壓瘡', '壓傷') "
                        + "AND LOCATION IN('" + base.userinfo.CostCenterName + "' ";
                    foreach (string location in JsonConvert.DeserializeObject<List<string>>(Option["CostName"]).ToArray())
                        sql += ",'" + location + "' ";
                    sql += ") " + "AND CREATTIME >= to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') "
                        + "AND CREATTIME <= to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') "
                        + "AND DELETED IS NULL "
                        + "ORDER BY CREATTIME ";

                    DataTable Dt = ass_m.DBExecSQL(sql);
                    if (Dt.Rows.Count > 0)
                    {
                        PatientInfo pi = new PatientInfo();
                        string feeno = "";
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            if (feeno != Dt.Rows[i]["FEENO"].ToString())
                            {
                                feeno = Dt.Rows[i]["FEENO"].ToString();
                                TempByte = webService.GetPatientInfo(feeno);
                                if (TempByte != null)
                                    pi = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(TempByte));
                            }
                            temp = new Dictionary<string, string>();
                            temp["InDate"] = pi.InDate.ToString("yyyy/MM/dd HH:mm");
                            temp["CREATTIME"] = Convert.ToDateTime(Dt.Rows[i]["CREATTIME"].ToString()).ToString("yyyy/MM/dd");
                            temp["LOCATION"] = Dt.Rows[i]["LOCATION"].ToString();
                            temp["OutDate"] = pi.OutDate.ToString("yyyy/MM/dd HH:mm");
                            temp["PatientName"] = pi.PatientName;
                            temp["ChartNo"] = pi.ChartNo;
                            temp["POSITION"] = Dt.Rows[i]["POSITION"].ToString();
                            temp["REASON"] = Dt.Rows[i]["REASON"].ToString();
                            temp["WOUND_ID"] = Dt.Rows[i]["WOUND_ID"].ToString();
                            tempDt.Add(temp);
                        }
                    }

                    if (tempDt.Count > 0)
                    {
                        UserInfo tempUser;
                        sql = "SELECT WOUND_RECORD.*, v_clerk_data.* "
                            + " FROM  WOUND_RECORD,v_clerk_data  WHERE TRIM(WOUND_RECORD.CREANO) =TRIM(v_clerk_data.CLERK_ID) AND v_clerk_data.DEPT_NO IN (''";
                        foreach (string CostCode_ in CostCode)
                            sql += ",'" + CostCode_.Trim() + "' ";
                        sql += ") AND WOUND_ID = '{0}' "
                            + "AND RECORDTIME >= to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') "
                            + "AND RECORDTIME <= to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') "
                            + "AND DELETED IS NULL "
                            + "ORDER BY RECORDTIME ";
                        foreach (var item in tempDt)
                        {
                            Dt = ass_m.DBExecSQL(sql);
                            if (Dt.Rows.Count > 0)
                            {
                                for (int i = 0; i < Dt.Rows.Count; i++)
                                {
                                    temp = dt_.Find(x => x["CostName"] == item["LOCATION"]);
                                    temp["IN"] = (int.Parse(temp["IN"]) + 1).ToString();
                                    IN++;

                                    tempUser = CostCodeUserList.Find(x => x.EmployeesNo == Dt.Rows[i]["CREANO"].ToString());
                                    if (tempUser == null && Dt.Rows[i]["CREANO"].ToString() == base.userinfo.EmployeesNo)
                                    //tempUser = CostCodeUserList.Find(x => x.EmployeesNo == reader["CREANO"].ToString());
                                    //if (tempUser == null && reader["CREANO"].ToString() == base.userinfo.EmployeesNo)
                                    {
                                        base.userinfo.CostCenterName = item["LOCATION"];
                                        if (item["LOCATION"] != base.userinfo.CostCenterName)
                                        {
                                            temp["TRAN"] = (int.Parse(temp["TRAN"]) + 1).ToString();
                                            TRAN++;
                                        }
                                    }
                                    else
                                    {
                                        if (item["LOCATION"] != tempUser.CostCenterName)
                                        {
                                            temp["TRAN"] = (int.Parse(temp["TRAN"]) + 1).ToString();
                                            TRAN++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    temp = new Dictionary<string, string>();
                    temp["CostName"] = "合計(人次)";
                    temp["IN"] = IN.ToString();
                    temp["TRAN"] = TRAN.ToString();
                    dt_.Add(temp);
                }

                List<string> Title = new List<string> { "單位", "院內發生人次", "轉入壓傷人次" };
                List<Dictionary<string, string>> Search_Title = new List<Dictionary<string, string>>();
                temp = new Dictionary<string, string>();
                temp["Name"] = "搜尋區間";
                temp["Value"] = Option["StartDate"] + "-" + Option["EndDate"];
                Search_Title.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Name"] = "單位";
                temp["Value"] = string.Join("、", CostCodeName);
                Search_Title.Add(temp);

                response(Title, Search_Title, dt_, "壓傷統計表");
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return new EmptyResult();
            }
        }


        [HttpPost]
        public ActionResult MedDetail(string OptionList)
        {
            try
            {
                List<Dictionary<string, string>> dt_ = new List<Dictionary<string, string>>();
                Dictionary<string, string> temp = null;
                Dictionary<string, string> temp_bed = null;
                Dictionary<string, string> Option = JsonConvert.DeserializeObject<Dictionary<string, string>>(OptionList);
                List<string> CostCode = JsonConvert.DeserializeObject<List<string>>(Option["CostCode"]);
                List<UserInfo> CostCodeUserList = new List<UserInfo>();
                List<UserInfo> Employee_List = new List<UserInfo>();
                List<PatientInfo> pi_List = new List<PatientInfo>();
                List<BedItem> BedList = new List<BedItem>();
                byte[] TempByte = null;
                string sql_bed = "";


                foreach (string CostCode_ in CostCode)
                {
                    TempByte = webService.GetBedList(CostCode_);
                    if (TempByte != null)
                        BedList.AddRange(JsonConvert.DeserializeObject<List<BedItem>>(CompressTool.DecompressString(TempByte)));
                }
                if (BedList.Count > 0)
                {
                    string sql = "SELECT A.* "
                        + ", (SELECT B.MEDCHINESE FROM MEDINFO B WHERE B.MEDCODE = A.MED_CODE) AS MEDCHINESE "
                        + "FROM DRUG_EXECUTE A WHERE  A.BED_NO IN ( ";

                    temp_bed = new Dictionary<string, string>();
                    foreach (BedItem item in BedList)
                    {
                        sql_bed += "'" + item.BedNo.Trim() + "',";
                        temp_bed[item.BedNo.Trim()] = item.CostCenterName.Trim();
                    }

                    sql_bed = sql_bed.TrimEnd(',');
                    sql += sql_bed + " )";

                    //原來寫法會有因不同伺服器時間格式而不相容的問題:"AM"或"上午"  
                    sql += "AND TO_DATE(SUBSTR(A.DRUG_DATE, 0 , LENGTH(A.DRUG_DATE) - 12),'yyyy-mm-dd AM hh:mi:ss') BETWEEN "  //18AUG17 元DB存的格式有包含時分秒，但其實頁面條件只要月日即可，因此用SUBSTR去切割後面hh:mi:ss只留yyyy/mm/dd
                        + "TO_DATE('" + Convert.ToDateTime(Option["StartDate"]).ToString("yyyy-MM-dd") + "','yyyy/mm/dd AM hh:mi:ss') "
                        + "AND TO_DATE('" + Convert.ToDateTime(Option["EndDate"]).ToString("yyyy-MM-dd 下午 11:59:59") + "','yyyy/mm/dd AM hh:mi:ss') "
                        + " AND ( REASON IS NOT NULL OR REASONTYPE IS NOT NULL OR  EXEC_ID  IS NULL OR EXEC_DATE  IS NULL )  ORDER BY A.DRUG_DATE ";

                    DataTable Dt = ass_m.DBExecSQL(sql);
                    if (Dt.Rows.Count > 0)
                    {
                        PatientInfo pi = new PatientInfo();
                        string feeno = "", userno = "";
                        string flag_notDo = "";
                        UserInfo tempUser = new UserInfo();
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            flag_notDo = "";
                            if (Dt.Rows[i]["UD_SEQPK"].ToString().StartsWith("U") || Dt.Rows[i]["UD_SEQPK"].ToString().StartsWith("N") || Dt.Rows[i]["UD_SEQPK"].ToString().StartsWith("S"))
                            {
                                if (feeno != Dt.Rows[i]["FEE_NO"].ToString())
                                {
                                    feeno = Dt.Rows[i]["FEE_NO"].ToString();
                                    bool IsPiExists = pi_List.Exists(x => x.FeeNo == feeno); //只要出現過就不再進webservice取值
                                    if (!IsPiExists)
                                    {
                                        TempByte = webService.GetPatientInfo(feeno);
                                        if (TempByte != null)
                                            pi = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(TempByte));
                                        pi_List.Add(pi);
                                    }

                                    pi = pi_List.Find(x => x.FeeNo == feeno); //病人明細改從pi_List取

                                    if (!string.IsNullOrWhiteSpace(Dt.Rows[i]["EXEC_ID"].ToString()))
                                    {
                                        userno = Dt.Rows[i]["EXEC_ID"].ToString();
                                        bool IsEmployeeExists = Employee_List.Exists(x => x.EmployeesNo == userno); //只要出現過就不再進webservice取值
                                        if (!IsEmployeeExists)
                                        {
                                            tempUser.EmployeesNo = Dt.Rows[i]["EXEC_ID"].ToString();
                                            tempUser.EmployeesName = Dt.Rows[i]["EXEC_NAME"].ToString();
                                            Employee_List.Add(tempUser);
                                        }

                                        tempUser = Employee_List.Find(x => x.EmployeesNo == userno); //給藥護理師改從 Employee_List取

                                    }
                                    else
                                    {
                                        tempUser.EmployeesNo = ""; tempUser.EmployeesName = ""; //未執行則為空白
                                    }
                                }

                                temp = new Dictionary<string, string>();
                                temp["costCode"] = temp_bed[Dt.Rows[i]["BED_NO"].ToString()]; //20181017 ECK要求以床號的病房名稱為主(因為病人會轉房間)
                                temp["date"] = Convert.ToDateTime(Dt.Rows[i]["DRUG_DATE"].ToString()).ToString("yyyy/MM/dd");
                                temp["time"] = Convert.ToDateTime(Dt.Rows[i]["DRUG_DATE"].ToString()).ToString("HH:mm");
                                temp["ptName"] = pi.PatientName;
                                temp["ChartNo"] = pi.ChartNo;
                                temp["age"] = pi.Age.ToString();
                                temp["deptName"] = pi.DeptName;
                                temp["icd"] = pi.ICD9_code1;
                                temp["MEDCHINESE"] = Dt.Rows[i]["MEDCHINESE"].ToString();
                                temp["WHY"] = "";
                                temp["REASON"] = "";
                                temp["EXEC_ID"] = "";
                                if (!string.IsNullOrWhiteSpace(Dt.Rows[i]["REASONTYPE"].ToString()))
                                {
                                    temp["WHY"] = "未給";
                                    temp["REASON"] = Dt.Rows[i]["REASONTYPE"].ToString();
                                }
                                else if (!string.IsNullOrWhiteSpace(Dt.Rows[i]["REASON"].ToString()))
                                {
                                    temp["WHY"] = "延遲";
                                    temp["REASON"] = Dt.Rows[i]["REASON"].ToString();
                                }
                                else if (string.IsNullOrWhiteSpace(Dt.Rows[i]["EXEC_DATE"].ToString()) || string.IsNullOrWhiteSpace(Dt.Rows[i]["EXEC_ID"].ToString()))
                                {
                                    temp["WHY"] = "未執行";
                                    flag_notDo = "未執行";
                                }
                                if (flag_notDo == "未執行")
                                    temp["EXEC_ID"] = "";
                                else
                                    temp["EXEC_ID"] = tempUser.EmployeesName;
                                dt_.Add(temp);
                            }
                        }
                    }
                }

                List<string> Title = new List<string> { "單位", "日期", "時間", "病人姓名", "病歷號", "年齡", "科別", "診斷"
                                 , "藥物名稱", "原因", "說明", "執行者" };
                List<Dictionary<string, string>> Search_Title = new List<Dictionary<string, string>>();
                temp = new Dictionary<string, string>();
                temp["Name"] = "搜尋區間";
                temp["Value"] = Option["StartDate"] + "-" + Option["EndDate"];
                Search_Title.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Name"] = "單位";
                temp["Value"] = string.Join("、", JsonConvert.DeserializeObject<List<string>>(Option["CostName"]).ToArray());
                Search_Title.Add(temp);

                response(Title, Search_Title, dt_, "給藥特殊註記明細表");
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return new EmptyResult();
            }
        }

        [HttpPost]
        public ActionResult MedStatistics(string OptionList)
        {
            try
            {
                List<Dictionary<string, string>> dt_ = new List<Dictionary<string, string>>();
                Dictionary<string, string> temp = null;
                Dictionary<string, string> Option = JsonConvert.DeserializeObject<Dictionary<string, string>>(OptionList);
                List<string> CostCode = JsonConvert.DeserializeObject<List<string>>(Option["CostCode"]);
                List<UserInfo> CostCodeUserList = new List<UserInfo>();

                List<BedItem> BedList = new List<BedItem>();
                byte[] TempByte = null;
                string sql_bed = "";

                temp = new Dictionary<string, string>();
                temp["Title"] = "次數";
                temp["No"] = "0";
                temp["Delay"] = "0";
                temp["None"] = "0";
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Title"] = "總次數";
                temp["No"] = "0";
                temp["Delay"] = "0";
                temp["None"] = "0";
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Title"] = "比率(%)";
                temp["No"] = "0";
                temp["Delay"] = "0";
                temp["None"] = "0";
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Title"] = "備註";
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Title"] = "1.次數定義:有註記未給、延遲次數";
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Title"] = "2.總次數:病人應給藥總次數(排除prn及點滴)";
                dt_.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Title"] = "3.比率:(次數/總次數)*100%";
                dt_.Add(temp);

                foreach (string CostCode_ in CostCode)
                {
                    TempByte = webService.GetBedList(CostCode_);
                    if (TempByte != null)
                        BedList.AddRange(JsonConvert.DeserializeObject<List<BedItem>>(CompressTool.DecompressString(TempByte)));
                }
                if (BedList.Count > 0)
                {
                    string sql = "SELECT A.* "
                        + ", (SELECT B.MEDCHINESE FROM MEDINFO B WHERE B.MEDCODE = A.MED_CODE) AS MEDCHINESE "
                        + "FROM DRUG_EXECUTE A WHERE  A.BED_NO IN ( ";
                    foreach (BedItem item in BedList)
                        sql_bed += "'" + item.BedNo.Trim() + "',";
                    sql_bed = sql_bed.TrimEnd(',');
                    sql += sql_bed + " )";

                    //string sql = "SELECT A.* "
                    //    + ", (SELECT B.MEDCHINESE FROM MEDINFO B WHERE B.MEDCODE = A.MED_CODE) AS MEDCHINESE "
                    //    + "FROM DRUG_EXECUTE A WHERE (A.EXEC_ID IN(''";
                    //foreach (UserInfo userNo in CostCodeUserList)
                    //    sql += ",'" + userNo.EmployeesNo.Trim() + "' ";
                    //  sql += ") OR A.EXEC_ID IS NULL ) ";                   
                    //區間時間
                    sql += "AND TO_DATE(A.DRUG_DATE,'yyyy-mm-dd AM hh:mi:ss') BETWEEN "
                    + "TO_DATE('" + Convert.ToDateTime(Option["StartDate"] + " 00:00:00").ToString("yyyy-MM-dd tt hh:mm:ss") + "','yyyy/mm/dd AM hh:mi:ss') "
                    + "AND TO_DATE('" + Convert.ToDateTime(Option["EndDate"] + " 23:59:59").ToString("yyyy-MM-dd tt hh:mm:ss") + "','yyyy/mm/dd AM hh:mi:ss') "
                    + " ORDER BY A.DRUG_DATE ";

                    DataTable Dt = ass_m.DBExecSQL(sql);
                    if (Dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            if (Dt.Rows[i]["UD_SEQPK"].ToString().StartsWith("U") || Dt.Rows[i]["UD_SEQPK"].ToString().StartsWith("N") || Dt.Rows[i]["UD_SEQPK"].ToString().StartsWith("S"))
                            {
                                dt_[1]["No"] = (int.Parse(dt_[1]["No"]) + 1).ToString();
                                dt_[1]["Delay"] = (int.Parse(dt_[1]["Delay"]) + 1).ToString();
                                dt_[1]["None"] = (int.Parse(dt_[1]["None"]) + 1).ToString();

                                if (!string.IsNullOrWhiteSpace(Dt.Rows[i]["REASONTYPE"].ToString()))
                                {
                                    dt_[0]["No"] = (int.Parse(dt_[0]["No"]) + 1).ToString();
                                }
                                else if (!string.IsNullOrWhiteSpace(Dt.Rows[i]["REASON"].ToString()))
                                {
                                    dt_[0]["Delay"] = (int.Parse(dt_[0]["Delay"]) + 1).ToString();
                                }
                                else if (string.IsNullOrWhiteSpace(Dt.Rows[i]["EXEC_DATE"].ToString()) || (string.IsNullOrWhiteSpace(Dt.Rows[i]["EXEC_ID"].ToString()) && string.IsNullOrWhiteSpace(Dt.Rows[i]["EXEC_NAME"].ToString())))
                                {
                                    dt_[0]["None"] = (int.Parse(dt_[0]["None"]) + 1).ToString();
                                }
                            }

                        }
                        dt_[2]["No"] = (Math.Round(float.Parse(dt_[0]["No"]) / int.Parse(dt_[1]["No"]), 2) * 100).ToString() + "%";
                        dt_[2]["Delay"] = (Math.Round(float.Parse(dt_[0]["Delay"]) / int.Parse(dt_[1]["Delay"]), 2) * 100).ToString() + "%";
                        dt_[2]["None"] = (Math.Round(float.Parse(dt_[0]["None"]) / int.Parse(dt_[1]["None"]), 2) * 100).ToString() + "%";
                    }
                }

                List<string> Title = new List<string> { "原因類別", "未給", "延遲", "未執行" };
                List<Dictionary<string, string>> Search_Title = new List<Dictionary<string, string>>();
                temp = new Dictionary<string, string>();
                temp["Name"] = "搜尋區間";
                temp["Value"] = Option["StartDate"] + "-" + Option["EndDate"];
                Search_Title.Add(temp);
                temp = new Dictionary<string, string>();
                temp["Name"] = "單位";
                temp["Value"] = string.Join("、", JsonConvert.DeserializeObject<List<string>>(Option["CostName"]).ToArray());
                Search_Title.Add(temp);

                response(Title, Search_Title, dt_, "給藥特殊註記統計表");
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return new EmptyResult();
            }
        }

        [HttpPost]
        public ActionResult DeliriumAndCFSStatistics(string OptionList)
        {
            Dictionary<string, string> Option = JsonConvert.DeserializeObject<Dictionary<string, string>>(OptionList);
            List<string> CostCode = JsonConvert.DeserializeObject<List<string>>(Option["CostCode"]);
            List<UserInfo> CostCodeUserList = new List<UserInfo>();
            List<Dictionary<string, string>> dt_ = new List<Dictionary<string, string>>();
            List<Dictionary<string, string>> Search_Title = new List<Dictionary<string, string>>();
            Dictionary<string, string> temp = null;
            List<Dictionary<string, string>> tempDt = new List<Dictionary<string, string>>();
            List<BedItem> BedList = new List<BedItem>();
            byte[] TempByte = null;
            temp = new Dictionary<string, string>();
            temp["Name"] = "搜尋區間";
            temp["Value"] = Option["StartDate"] + "-" + Option["EndDate"];
            Search_Title.Add(temp);
            temp = new Dictionary<string, string>();
            temp["Name"] = "單位";
            temp["Value"] = string.Join("、", JsonConvert.DeserializeObject<List<string>>(Option["CostName"]).ToArray());
            Search_Title.Add(temp);
            temp = new Dictionary<string, string>();
            bool op1 = Option["DeliriumAndCFS"].Contains("CfsStatisticsALL"), op2 = Option["DeliriumAndCFS"].Contains("CfsStatistics65EarlyPeriodCFS"),
                op3 = Option["DeliriumAndCFS"].Contains("CfsStatistics65MiniCog"), op4 = Option["DeliriumAndCFS"].Contains("CfsStatistics65EarlyPeriodDNR"),
                op5 = Option["DeliriumAndCFS"].Contains("CfsStatistics65EarlyPeriodUTI"), op6 = Option["DeliriumAndCFS"].Contains("DeliriumStatisticsALL"),
                op7 = Option["DeliriumAndCFS"].Contains("DeliriumStatistics65");
            try
            {
                foreach (string CostCode_ in CostCode)
                {
                    TempByte = webService.GetUserList(CostCode_);
                    if (TempByte != null)
                        CostCodeUserList.AddRange(JsonConvert.DeserializeObject<List<UserInfo>>(CompressTool.DecompressString(TempByte)));
                }
                //foreach (string CostCode_ in CostCode)
                //{
                //    TempByte = webService.GetBedList(CostCode_);
                //    if (TempByte != null)
                //        BedList.AddRange(JsonConvert.DeserializeObject<List<BedItem>>(CompressTool.DecompressString(TempByte)));
                //}

                if (CostCodeUserList.Count > 0)
                {
                    //CFS
                    string cfs_filter_sql = "SELECT FEE_NO, CFS_SCORE, MC_SCORE, MODIFY_USER, MODIFY_DATE_CFS, ROW_NUMBER() OVER(PARTITION BY FEE_NO ORDER BY MODIFY_DATE_CFS DESC) AS SN \n";
                    cfs_filter_sql += "FROM (SELECT FEE_NO, CFS_SCORE, MC_SCORE, MODIFY_USER, BED_NO,CASE WHEN MODIFY_DATE IS NOT NULL THEN MODIFY_DATE ELSE CREATE_DATE END AS MODIFY_DATE_CFS FROM CFS_DATA WHERE STATUS = 'Y') \n";
                    cfs_filter_sql += "WHERE TRIM(MODIFY_USER) IN('08661','09227','MAYA'";
                    foreach (UserInfo userNo in CostCodeUserList)
                        cfs_filter_sql += ",'" + userNo.EmployeesNo.Trim() + "' ";
                    cfs_filter_sql += ") \n";
                    cfs_filter_sql += "AND to_date(MODIFY_DATE_CFS,'yyyy/mm/dd hh24:mi:ss') BETWEEN to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') AND to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') ";

                    string cfs_sql = "WITH cfs_filter AS ( ";
                    cfs_sql += cfs_filter_sql + "), \n";
                    cfs_sql += "cfs AS ( ";
                    cfs_sql += "SELECT FEE_NO, CFS_SCORE, MC_SCORE, MODIFY_USER, MODIFY_DATE_CFS, SN \n";
                    cfs_sql += "FROM cfs_filter \n";
                    cfs_sql += "WHERE SN ='1') ";

                    //223.224才能用
                    cfs_sql += ", cfs_unit AS ( \n";
                    cfs_sql += "SELECT A.*, B.DEPT_NAME AS UNIT \n";
                    cfs_sql += "FROM cfs A LEFT JOIN V_CLERK_DATA B \n";
                    cfs_sql += "ON TRIM(A.MODIFY_USER) = TRIM(B.CLERK_ID) ) ";

                    string fee_no_sql_cfs = cfs_sql + "SELECT FEE_NO FROM cfs_unit";
                    DataTable fee_no_cfs_Dt = ass_m.DBExecSQL(fee_no_sql_cfs);
                    List<string> sel_fee_no_cfs = new List<string> { };
                    if (fee_no_cfs_Dt.Rows.Count > 0)
                    {
                        for (var i = 0; i < fee_no_cfs_Dt.Rows.Count; i++)
                        {
                            sel_fee_no_cfs.Add(fee_no_cfs_Dt.Rows[i]["FEE_NO"].ToString());
                        }
                    }

                    string pat_filter_cfs = "SELECT IX.ADMIT_NO Fee_No,IX.CHART_NO ChartNo,PT.BED_NO BedNo,CASE CT.SEX WHEN 'M' THEN '1' WHEN 'F' THEN '2' END PatientGender,CT.PT_NAME PatientName, \n" +
                        "TO_CHAR(SUBSTR(CT.BIRTH_DATE, 2, 7)) Birthday,DR.DOCTOR_NAME DocName, PT.VS_NO DocNo, IX.ADMIT_DATE InDate, IX.ADMIT_TIME InTime, \n" +
                        "IX.DISCHARGE_DATE OutDate, \n" +
                        "IX.DISCHARGE_TIME OutTime, \n" +
                        "(SELECT DISTINCT MAX(DIAGNOSIS_CODE) FROM IPD.ORDAIPD2 WHERE ADMIT_NO = IX.ADMIT_NO AND DIAGNOSIS_TYPE IN('11', '21')) ICD9_code1, \n" +
                        "(SELECT DISTINCT max(DIAGNOSIS_CHINESE_NAME) FROM IPD.ORDAIPD2 WHERE ADMIT_NO = IX.ADMIT_NO AND DIAGNOSIS_CODE IN(SELECT DISTINCT MAX(DIAGNOSIS_CODE) FROM IPD.ORDAIPD2 WHERE ADMIT_NO = IX.ADMIT_NO AND DIAGNOSIS_TYPE IN('11', '21'))) mdiag_desc1, \n" +
                        "PT.DIV_NO DeptNo,(SELECT DIV_SHORT_NAME FROM MAST.DIV WHERE DIV_NO = PT.DIV_NO) DeptName, \n" +
                        "CT.ID_NO PatientID, (CASE PT.PT_TYPE WHEN '02' THEN '健保' WHEN '01' THEN '自費' ELSE '自費'END) PayInfo, \n" +
                        "(SELECT CASE WHEN TRIM(DNR_FLAG) IS NOT NULL THEN 'Y' ELSE 'N' END from CHART.PTDNRDATA where ID_NO =CT.ID_NO) DNR \n" +
                        "FROM IPD.IPDINDEX IX, CS.V_ALL_PTIPD PT, CHART.CHART CT, MAST.DOCTOR DR \n" +
                        "WHERE IX.ADMIT_NO IN ('' ";
                    foreach (var FEENO in sel_fee_no_cfs)
                    {
                        pat_filter_cfs += ",'" + FEENO.Trim() + "' ";
                    }
                    pat_filter_cfs += ") \n" +
                    "AND IX.ADMIT_NO = PT.ADMIT_NO \n" +
                    "AND IX.CUT_NO = PT.CUT_NO \n" +
                    "AND IX.CHART_NO = CT.CHART_NO \n" +
                    "AND PT.VS_NO = DR.DOCTOR_NO \n" +
                    "AND IX.CUT_NO IN(SELECT MAX(CUT_NO) FROM IPD.IPDINDEX WHERE ADMIT_NO = IX.ADMIT_NO)";

                    cfs_sql += ",pat AS ( \n";
                    cfs_sql += pat_filter_cfs + "), \n";


                    cfs_sql += "cfs_pat AS ( \n";
                    cfs_sql += "SELECT A.*, B.BEDNO, B.PATIENTNAME, B.BIRTHDAY, TRUNC((sysdate - to_date(cast( B.BIRTHDAY +19110000 as varchar(8)),'yyyyMMdd'))/365.25) AS AGE, \n";
                    cfs_sql += "B.INDATE, B.OUTDATE, B.CHARTNO, B.DEPTNAME, B.MDIAG_DESC1, B.DEPTNO, B.DNR \n";
                    cfs_sql += "FROM cfs_unit A LEFT JOIN pat B \n";
                    cfs_sql += "ON A.FEE_NO = B.FEE_NO ), \n";
                    //......................

                    cfs_sql += "cfs_pat_tube_filter AS ( \n";
                    if (op5)
                    {
                        cfs_sql += "SELECT A.FEE_NO, SUM(CASE WHEN A.OUTDATE IS NOT NULL AND to_date(cast( A.OUTDATE +19110000 as varchar(8)),'yyyy/mm/dd') >= B.STARTTIME AND (to_date( cast( A.OUTDATE +19110000 as varchar(8)),'yyyy/mm/dd') <= B.ENDTIME OR B.ENDTIME IS NULL ) THEN 1 ELSE 0 END) AS TUBE_SUM \n";
                    }
                    else
                    {
                        cfs_sql += "SELECT A.FEE_NO, SUM(CASE WHEN to_date(A.MODIFY_DATE_CFS,'yyyy/mm/dd hh24:mi:ss') >= B.STARTTIME AND (to_date( A.MODIFY_DATE_CFS,'yyyy/mm/dd hh24:mi:ss') <= B.ENDTIME OR B.ENDTIME IS NULL ) THEN 1 ELSE 0 END) AS TUBE_SUM \n";
                    }
                    cfs_sql += "FROM cfs_pat A LEFT JOIN TUBE B \n";
                    cfs_sql += "ON A.FEE_NO = B.FEENO \n";
                    cfs_sql += "WHERE (TYPEID = 'TK00000001' OR TYPEID = 'TK00000002') \n";
                    if (op5)
                    {
                        cfs_sql += "AND A.OUTDATE IS NOT NULL AND A.OUTDATE != '0000000' \n";
                    }
                    cfs_sql += "GROUP BY A.FEE_NO),  \n";

                    cfs_sql += "cfs_pat_tube AS ( \n";
                    cfs_sql += "SELECT A.*, CASE WHEN B.TUBE_SUM > 0 THEN 'Y' ELSE 'N' END AS TUBE \n";
                    cfs_sql += "FROM cfs_pat A LEFT JOIN cfs_pat_tube_filter B \n";
                    cfs_sql += "ON A.FEE_NO = B.FEE_NO) ";

                    //DEL
                    string del_filter_sql = "SELECT FEE_NO, DELIRIUM_RESULT, MODIFY_USER, MODIFY_DATE_DEL, ROW_NUMBER() OVER(PARTITION BY FEE_NO ORDER BY MODIFY_DATE_DEL DESC) AS SN \n";
                    del_filter_sql += "FROM (SELECT FEE_NO, DELIRIUM_RESULT, MODIFY_USER, CASE WHEN MODIFY_DATE IS NOT NULL THEN MODIFY_DATE ELSE CREATE_DATE END AS MODIFY_DATE_DEL FROM DELIRIUM_DATA WHERE STATUS = 'Y') \n";
                    del_filter_sql += "WHERE TRIM(MODIFY_USER) IN('08661','09227','MAYA'";
                    foreach (UserInfo userNo in CostCodeUserList)
                        del_filter_sql += ",'" + userNo.EmployeesNo.Trim() + "' ";
                    del_filter_sql += ") \n";
                    del_filter_sql += "AND to_date(MODIFY_DATE_DEL,'yyyy/mm/dd hh24:mi:ss') BETWEEN to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') AND to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') ";

                    string del_sql = "WITH del_filter AS ( \n";
                    del_sql += del_filter_sql + "), \n";
                    del_sql += "del AS ( \n";
                    del_sql += "SELECT FEE_NO, DELIRIUM_RESULT, MODIFY_USER, MODIFY_DATE_DEL, SN \n";
                    del_sql += "FROM del_filter \n";
                    del_sql += "WHERE SN ='1') \n";

                    //223.224才能用
                    del_sql += ", del_unit AS ( \n";
                    del_sql += "SELECT A.*, B.DEPT_NAME AS UNIT \n";
                    del_sql += "FROM del A LEFT JOIN V_CLERK_DATA B \n";
                    del_sql += "ON TRIM(A.MODIFY_USER) = TRIM(B.CLERK_ID) ) \n";

                    string fee_no_sql_del = del_sql + "SELECT FEE_NO FROM del_unit";
                    DataTable fee_no_del_Dt = ass_m.DBExecSQL(fee_no_sql_del);
                    List<string> sel_fee_no_del = new List<string> { };
                    if (fee_no_del_Dt.Rows.Count > 0)
                    {
                        for (var i = 0; i < fee_no_del_Dt.Rows.Count; i++)
                        {
                            sel_fee_no_del.Add(fee_no_del_Dt.Rows[i]["FEE_NO"].ToString());
                        }
                    }

                    string pat_filter_del = "SELECT IX.ADMIT_NO Fee_No,IX.CHART_NO ChartNo,PT.BED_NO BedNo,CASE CT.SEX WHEN 'M' THEN '1' WHEN 'F' THEN '2' END PatientGender,CT.PT_NAME PatientName, \n" +
                        "TO_CHAR(SUBSTR(CT.BIRTH_DATE, 2, 7)) Birthday,DR.DOCTOR_NAME DocName, PT.VS_NO DocNo, IX.ADMIT_DATE InDate, IX.ADMIT_TIME InTime, \n" +
                        "IX.DISCHARGE_DATE OutDate, \n" +
                        "IX.DISCHARGE_TIME OutTime, \n" +
                        "(SELECT DISTINCT MAX(DIAGNOSIS_CODE) FROM IPD.ORDAIPD2 WHERE ADMIT_NO = IX.ADMIT_NO AND DIAGNOSIS_TYPE IN('11', '21')) ICD9_code1, \n" +
                        "(SELECT DISTINCT max(DIAGNOSIS_CHINESE_NAME) FROM IPD.ORDAIPD2 WHERE ADMIT_NO = IX.ADMIT_NO AND DIAGNOSIS_CODE IN(SELECT DISTINCT MAX(DIAGNOSIS_CODE) FROM IPD.ORDAIPD2 WHERE ADMIT_NO = IX.ADMIT_NO AND DIAGNOSIS_TYPE IN('11', '21'))) mdiag_desc1, \n" +
                        "PT.DIV_NO DeptNo,(SELECT DIV_SHORT_NAME FROM MAST.DIV WHERE DIV_NO = PT.DIV_NO) DeptName, \n" +
                        "CT.ID_NO PatientID, (CASE PT.PT_TYPE WHEN '02' THEN '健保' WHEN '01' THEN '自費' ELSE '自費'END) PayInfo, \n" +
                        "(SELECT CASE WHEN TRIM(DNR_FLAG) IS NOT NULL THEN 'Y' ELSE 'N' END from CHART.PTDNRDATA where ID_NO =CT.ID_NO) DNR \n" +
                        "FROM IPD.IPDINDEX IX, CS.V_ALL_PTIPD PT, CHART.CHART CT, MAST.DOCTOR DR \n" +
                        "WHERE IX.ADMIT_NO IN ('' ";
                    foreach (var FEENO in sel_fee_no_del)
                    {
                        pat_filter_del += ",'" + FEENO.Trim() + "' ";
                    }
                    pat_filter_del += ") \n" +
                    "AND IX.ADMIT_NO = PT.ADMIT_NO \n" +
                    "AND IX.CUT_NO = PT.CUT_NO \n" +
                    "AND IX.CHART_NO = CT.CHART_NO \n" +
                    "AND PT.VS_NO = DR.DOCTOR_NO \n" +
                    "AND IX.CUT_NO IN(SELECT MAX(CUT_NO) FROM IPD.IPDINDEX WHERE ADMIT_NO = IX.ADMIT_NO)";

                    del_sql += ",pat AS ( \n";
                    del_sql += pat_filter_del + "), \n";


                    del_sql += "del_pat AS ( \n";
                    del_sql += "SELECT A.*, B.BEDNO, B.PATIENTNAME, B.BIRTHDAY, TRUNC((sysdate - to_date(cast( B.BIRTHDAY +19110000 as varchar(8)),'yyyyMMdd'))/365.25) AS AGE, \n";
                    del_sql += "B.INDATE, B.OUTDATE, B.CHARTNO, B.DEPTNAME, B.MDIAG_DESC1, B.DEPTNO, B.DNR \n";
                    del_sql += "FROM del_unit A LEFT JOIN pat B \n";
                    del_sql += "ON A.FEE_NO = B.FEE_NO ) \n";
                    //......................

                    //測試用
                    //cfs_sql += "SELECT A.* FROM cfs_pat_tube A";
                    //del_sql += "SELECT A.* FROM del_pat A";
                    //DataTable test_cfs = ass_m.DBExecSQL(cfs_sql);
                    //DataTable test_del = ass_m.DBExecSQL(del_sql);
                    if (op1)
                    {
                        cfs_sql += "SELECT SUM(CASE WHEN A.AGE >= 65 THEN 1 ELSE 0 END) AS SUM_65, \n" +
                            "SUM (CASE WHEN A.CFS_SCORE >= 1 AND A.CFS_SCORE <= 3 THEN 1 ELSE 0 END) AS CFS_1_3, \n" +
                            "SUM (CASE WHEN A.CFS_SCORE >= 4 AND A.CFS_SCORE <= 6 then 1 ELSE 0 END) AS CFS_4_6, \n" +
                            "SUM (CASE WHEN A.CFS_SCORE >= 7 AND A.CFS_SCORE <= 9 then 1 ELSE 0 END) AS CFS_7_9, \n" +
                            "SUM (CASE WHEN A.TUBE = 'Y' THEN 1 ELSE 0 END) AS TUBE_SUM, \n" +
                            "SUM (CASE WHEN A.DNR = 'Y' THEN 1 ELSE 0 END) AS DNR_SUM, \n" +
                            "A.UNIT ";

                        cfs_sql += "FROM cfs_pat_tube A \n";
                        cfs_sql += "GROUP BY A.UNIT";
                        DataTable cfs_pat_tube_Dt = ass_m.DBExecSQL(cfs_sql);

                        if (cfs_pat_tube_Dt.Rows.Count > 0)
                        {
                            var total_65 = 0;
                            var total_CFS_1_3 = 0;
                            var total_CFS_4_6 = 0;
                            var total_CFS_7_9 = 0;
                            var tatal_TUBE_SUM = 0;
                            var total_DNR_SUM = 0;

                            tempDt = new List<Dictionary<string, string>>();
                            for (int i = 0; i < cfs_pat_tube_Dt.Rows.Count; i++)
                            {
                                temp = new Dictionary<string, string>();
                                temp["NUM"] = (i + 1).ToString();
                                temp["UNIT"] = cfs_pat_tube_Dt.Rows[i]["UNIT"].ToString();
                                temp["SUM_65"] = cfs_pat_tube_Dt.Rows[i]["SUM_65"].ToString();
                                temp["CFS_1_3"] = cfs_pat_tube_Dt.Rows[i]["CFS_1_3"].ToString();
                                temp["CFS_4_6"] = cfs_pat_tube_Dt.Rows[i]["CFS_4_6"].ToString();
                                temp["CFS_7_9"] = cfs_pat_tube_Dt.Rows[i]["CFS_7_9"].ToString();
                                temp["TUBE_SUM"] = cfs_pat_tube_Dt.Rows[i]["TUBE_SUM"].ToString();
                                temp["DNR_SUM"] = cfs_pat_tube_Dt.Rows[i]["DNR_SUM"].ToString();

                                total_65 += Int32.Parse(cfs_pat_tube_Dt.Rows[i]["SUM_65"].ToString());
                                total_CFS_1_3 += Int32.Parse(cfs_pat_tube_Dt.Rows[i]["CFS_1_3"].ToString());
                                total_CFS_4_6 += Int32.Parse(cfs_pat_tube_Dt.Rows[i]["CFS_4_6"].ToString());
                                total_CFS_7_9 += Int32.Parse(cfs_pat_tube_Dt.Rows[i]["CFS_7_9"].ToString());
                                tatal_TUBE_SUM += Int32.Parse(cfs_pat_tube_Dt.Rows[i]["TUBE_SUM"].ToString());
                                total_DNR_SUM += Int32.Parse(cfs_pat_tube_Dt.Rows[i]["DNR_SUM"].ToString());
                                tempDt.Add(temp);
                            }
                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "";
                            temp["UNIT"] = "總計";
                            temp["SUM_65"] = total_65.ToString();
                            temp["CFS_1_3"] = total_CFS_1_3.ToString();
                            temp["CFS_4_6"] = total_CFS_4_6.ToString();
                            temp["CFS_7_9"] = total_CFS_7_9.ToString();
                            temp["TUBE_SUM"] = tatal_TUBE_SUM.ToString();
                            temp["DNR_SUM"] = total_DNR_SUM.ToString();
                            tempDt.Add(temp);
                            dt_ = tempDt;
                        }

                        List<string> Title = new List<string> { "序號", "單位", "65歲(含)以上(人次)", "CFS_1-3(人次)", "CFS_4-6(人次)", "CFS_7-9(人次)", "尿管使用(人次)", "DNR(人次)" };
                        response(Title, Search_Title, dt_, "衰弱評估統計表");
                    }
                    else if (op2)
                    {
                        cfs_sql += "SELECT SUM(CASE WHEN AGE >= 65 AND CFS_SCORE >= 4 AND CFS_SCORE <= 6 THEN 1 ELSE 0 END) AS SUM_65_CFS_4_6, " +
                            "SUM(CASE WHEN AGE >= 65 AND CFS_SCORE >= 1 AND CFS_SCORE <= 9 THEN 1 ELSE 0 END) AS SUM_65_CFS_1_9, " +
                            "UNIT ";
                        cfs_sql += "FROM cfs_pat_tube ";
                        cfs_sql += "GROUP BY UNIT";
                        DataTable cfs_pat_tube_Dt = ass_m.DBExecSQL(cfs_sql);

                        if (cfs_pat_tube_Dt.Rows.Count > 0)
                        {
                            float rate_65_CFS = 0;
                            float avg_SUM_65_CFS_4_6 = 0;
                            float avg_SUM_65_CFS_1_9 = 0;
                            float avg_all_rate_65_CFS = 0;

                            tempDt = new List<Dictionary<string, string>>();
                            for (int i = 0; i < cfs_pat_tube_Dt.Rows.Count; i++)
                            {
                                temp = new Dictionary<string, string>();
                                temp["NUM"] = (i + 1).ToString();
                                temp["UNIT"] = cfs_pat_tube_Dt.Rows[i]["UNIT"].ToString();
                                temp["SUM_65_CFS_4_6"] = cfs_pat_tube_Dt.Rows[i]["SUM_65_CFS_4_6"].ToString();
                                temp["SUM_65_CFS_1_9"] = cfs_pat_tube_Dt.Rows[i]["SUM_65_CFS_1_9"].ToString();

                                if (temp["SUM_65_CFS_1_9"] != "0")
                                {
                                    rate_65_CFS = (float.Parse(cfs_pat_tube_Dt.Rows[i]["SUM_65_CFS_4_6"].ToString()) / float.Parse(cfs_pat_tube_Dt.Rows[i]["SUM_65_CFS_1_9"].ToString())) * 100;
                                }
                                else
                                {
                                    rate_65_CFS = 0;
                                }
                                temp["RATE_65_CFS"] = rate_65_CFS.ToString("#0.00") + "%";
                                avg_SUM_65_CFS_4_6 += Int32.Parse(cfs_pat_tube_Dt.Rows[i]["SUM_65_CFS_4_6"].ToString());
                                avg_SUM_65_CFS_1_9 += Int32.Parse(cfs_pat_tube_Dt.Rows[i]["SUM_65_CFS_1_9"].ToString());
                                avg_all_rate_65_CFS += rate_65_CFS;
                                tempDt.Add(temp);
                            }

                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "";
                            temp["UNIT"] = "總計/平均";
                            temp["SUM_65_CFS_4_6"] = ((float)avg_SUM_65_CFS_4_6 / (float)cfs_pat_tube_Dt.Rows.Count).ToString();
                            temp["SUM_65_CFS_1_9"] = ((float)avg_SUM_65_CFS_1_9 / (float)cfs_pat_tube_Dt.Rows.Count).ToString();
                            if (float.Parse(temp["SUM_65_CFS_1_9"]) != 0)
                            {
                                temp["RATE_65_CFS"] = ((float.Parse(temp["SUM_65_CFS_4_6"]) / float.Parse(temp["SUM_65_CFS_1_9"])) * 100).ToString("#0.00") + "%";

                            }
                            else
                            {
                                temp["RATE_65_CFS"] = "0%";
                            }

                            tempDt.Add(temp);
                            temp = new Dictionary<string, string>();
                            tempDt.Add(temp);
                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "定義說明： ";
                            tempDt.Add(temp);
                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "1.65歲(含)以上衰弱前期比率：65歲(含)以上衰弱前期比率：(65歲(含)以上 CFS_4-6 人數 /CFS 1-9 總人數)*100%";
                            tempDt.Add(temp);
                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "2.NA表示未進行CFS評估";
                            tempDt.Add(temp);
                            dt_ = tempDt;
                        }
                        List<string> Title = new List<string> { "序號", "單位", "65歲(含)以上CFS_4-6 人次數", "65歲(含)以上CFS_1-9 總人次數", "65歲(含)以上衰弱前期比率" };
                        response(Title, Search_Title, dt_, "65歲(含)以上衰弱前期統計表");
                    }
                    else if (op3)
                    {
                        cfs_sql += "SELECT SUM(CASE WHEN AGE >= 65 AND MC_SCORE > 0 THEN 1 ELSE 0 END) AS SUM_65_MC, " +
                            "SUM(CASE WHEN AGE >= 65 AND CFS_SCORE >= 4 AND CFS_SCORE <= 6 THEN 1 ELSE 0 END) AS SUM_65_CFS, " +
                            "UNIT ";
                        cfs_sql += "FROM cfs_pat_tube ";
                        cfs_sql += "GROUP BY UNIT";
                        DataTable cfs_pat_tube_Dt = ass_m.DBExecSQL(cfs_sql);
                        if (cfs_pat_tube_Dt.Rows.Count > 0)
                        {
                            float rate_65_MC = 0;
                            float avg_SUM_65_CFS = 0;
                            float avg_SUM_MC = 0;
                            float avg_all_rate_65_MC = 0;

                            tempDt = new List<Dictionary<string, string>>();
                            for (int i = 0; i < cfs_pat_tube_Dt.Rows.Count; i++)
                            {
                                temp = new Dictionary<string, string>();
                                temp["NUM"] = (i + 1).ToString();
                                temp["UNIT"] = cfs_pat_tube_Dt.Rows[i]["UNIT"].ToString();
                                temp["SUM_65_MC"] = cfs_pat_tube_Dt.Rows[i]["SUM_65_MC"].ToString();
                                temp["SUM_65_CFS"] = cfs_pat_tube_Dt.Rows[i]["SUM_65_CFS"].ToString();

                                if (temp["SUM_65_CFS"] != "0")
                                {
                                    rate_65_MC = (float.Parse(cfs_pat_tube_Dt.Rows[i]["SUM_65_MC"].ToString()) / float.Parse(cfs_pat_tube_Dt.Rows[i]["SUM_65_CFS"].ToString())) * 100;
                                }
                                else
                                {
                                    rate_65_MC = 0;
                                }
                                temp["RATE_65_MC"] = rate_65_MC.ToString("#0.00") + "%";
                                avg_SUM_65_CFS += Int32.Parse(cfs_pat_tube_Dt.Rows[i]["SUM_65_CFS"].ToString());
                                avg_SUM_MC += Int32.Parse(cfs_pat_tube_Dt.Rows[i]["SUM_65_MC"].ToString());
                                avg_all_rate_65_MC += rate_65_MC;
                                tempDt.Add(temp);
                            }

                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "";
                            temp["UNIT"] = "總計/平均";
                            temp["SUM_65_MC"] = ((float)avg_SUM_MC / (float)cfs_pat_tube_Dt.Rows.Count).ToString();
                            temp["SUM_65_CFS"] = ((float)avg_SUM_65_CFS / (float)cfs_pat_tube_Dt.Rows.Count).ToString();
                            if (float.Parse(temp["SUM_65_CFS"]) != 0)
                            {
                                temp["RATE_65_MC"] = ((float.Parse(temp["SUM_65_MC"]) / float.Parse(temp["SUM_65_CFS"])) * 100).ToString("#0.00") + "%";
                            }
                            else
                            {
                                temp["RATE_65_MC"] = "0%";
                            }
                            tempDt.Add(temp);
                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "定義說明： ";
                            tempDt.Add(temp);
                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "1.65歲(含)以上mini-cog比率：(65歲(含)以上mini-cog人次數 / 65歲(含)以上CFS_4-6人次數)*100%";
                            tempDt.Add(temp);
                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "2.NA表示未進行CFS評估";
                            tempDt.Add(temp);
                            dt_ = tempDt;
                        }
                        List<string> Title = new List<string> { "序號", "單位", "65歲(含)以上mini-cog人次數", "65歲(含)以上CFS_4-6人次數", "65歲(含)以上mini-cog比率" };
                        response(Title, Search_Title, dt_, "65歲(含)以上mini-cog評估統計表");
                    }
                    else if (op4)
                    {
                        cfs_sql += "SELECT SUM(CASE WHEN AGE >= 65 AND CFS_SCORE >= 4 AND CFS_SCORE <= 6 AND DNR = 'Y' THEN 1 ELSE 0 END) AS SUM_65_CFS_46_DNR, " +
                            "SUM(CASE WHEN AGE >= 65 AND CFS_SCORE >= 1 AND CFS_SCORE <= 9 AND DNR = 'Y' THEN 1 ELSE 0 END) AS SUM_65_CFS_19_DNR, " +
                            "UNIT ";
                        cfs_sql += "FROM cfs_pat_tube ";
                        cfs_sql += "GROUP BY UNIT";
                        DataTable cfs_pat_tube_Dt = ass_m.DBExecSQL(cfs_sql);
                        if (cfs_pat_tube_Dt.Rows.Count > 0)
                        {
                            float rate_65_CFS_DNR = 0;
                            float avg_SUM_65_CFS_46_DNR = 0;
                            float avg_SUM_65_CFS_19_DNR = 0;
                            float avg_rate_65_CFS_DNR = 0;

                            tempDt = new List<Dictionary<string, string>>();
                            for (int i = 0; i < cfs_pat_tube_Dt.Rows.Count; i++)
                            {
                                temp = new Dictionary<string, string>();
                                temp["NUM"] = (i + 1).ToString();
                                temp["UNIT"] = cfs_pat_tube_Dt.Rows[i]["UNIT"].ToString();
                                temp["SUM_65_CFS_46_DNR"] = cfs_pat_tube_Dt.Rows[i]["SUM_65_CFS_46_DNR"].ToString();
                                temp["SUM_65_CFS_19_DNR"] = cfs_pat_tube_Dt.Rows[i]["SUM_65_CFS_19_DNR"].ToString();

                                if (temp["SUM_65_CFS_19_DNR"] != "0")
                                {
                                    rate_65_CFS_DNR = (float.Parse(cfs_pat_tube_Dt.Rows[i]["SUM_65_CFS_46_DNR"].ToString()) / float.Parse(cfs_pat_tube_Dt.Rows[i]["SUM_65_CFS_19_DNR"].ToString())) * 100;
                                }
                                else
                                {
                                    rate_65_CFS_DNR = 0;
                                }
                                temp["RATE_65_CFS_DNR"] = rate_65_CFS_DNR.ToString("#0.00") + "%";
                                avg_SUM_65_CFS_46_DNR += Int32.Parse(cfs_pat_tube_Dt.Rows[i]["SUM_65_CFS_46_DNR"].ToString());
                                avg_SUM_65_CFS_19_DNR += Int32.Parse(cfs_pat_tube_Dt.Rows[i]["SUM_65_CFS_19_DNR"].ToString());
                                avg_rate_65_CFS_DNR += rate_65_CFS_DNR;
                                tempDt.Add(temp);
                            }

                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "";
                            temp["UNIT"] = "總計/平均";
                            temp["SUM_65_CFS_46_DNR"] = ((float)avg_SUM_65_CFS_46_DNR / (float)cfs_pat_tube_Dt.Rows.Count).ToString();
                            temp["SUM_65_CFS_19_DNR"] = ((float)avg_SUM_65_CFS_19_DNR / (float)cfs_pat_tube_Dt.Rows.Count).ToString();
                            temp["RATE_65_CFS_DNR"] = ((float.Parse(temp["SUM_65_CFS_46_DNR"]) / float.Parse(temp["SUM_65_CFS_19_DNR"])) * 100).ToString("#0.00") + "%";
                            tempDt.Add(temp);
                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "定義說明： ";
                            tempDt.Add(temp);
                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "1.65歲(含)以上衰弱前期DNR簽署比率：(65歲(含)以上CFS_4-6 DNR簽署人次數/65歲(含)以上CFS_1-9DNR簽署人次數)*100%";
                            tempDt.Add(temp);
                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "2.NA表示未進行CFS評估";
                            tempDt.Add(temp);
                            dt_ = tempDt;
                        }
                        List<string> Title = new List<string> { "序號", "單位", "65歲(含)以上CFS_4-6 DNR簽署人次數", "65歲(含)以上CFS_1-9 DNR簽署人次數", "65歲(含)以上衰弱前期DNR簽署比率" };
                        response(Title, Search_Title, dt_, "65歲(含)以上衰弱前期DNR簽署統計表");
                    }
                    else if (op5)
                    {
                        cfs_sql += "SELECT SUM(CASE WHEN AGE >= 65 AND CFS_SCORE >= 4 AND CFS_SCORE <= 6 AND TUBE = 'Y' THEN 1 ELSE 0 END) AS SUM_46_TUBE, " +
                            "SUM(CASE WHEN AGE >= 65 AND CFS_SCORE >= 1 AND CFS_SCORE <= 9 AND TUBE = 'Y' THEN 1 ELSE 0 END) AS SUM_19_TUBE, " +
                            "UNIT ";
                        cfs_sql += "FROM cfs_pat_tube ";
                        cfs_sql += "GROUP BY UNIT";
                        DataTable cfs_pat_tube_Dt = ass_m.DBExecSQL(cfs_sql);
                        if (cfs_pat_tube_Dt.Rows.Count > 0)
                        {
                            float rate_TUBE = 0;
                            float avg_SUM_46_TUBE = 0;
                            float avg_SUM_19_TUBE = 0;
                            float avg_rate_TUBE = 0;

                            tempDt = new List<Dictionary<string, string>>();
                            for (int i = 0; i < cfs_pat_tube_Dt.Rows.Count; i++)
                            {
                                temp = new Dictionary<string, string>();
                                temp["NUM"] = (i + 1).ToString();
                                temp["UNIT"] = cfs_pat_tube_Dt.Rows[i]["UNIT"].ToString();
                                temp["SUM_46_TUBE"] = cfs_pat_tube_Dt.Rows[i]["SUM_46_TUBE"].ToString();
                                temp["SUM_19_TUBE"] = cfs_pat_tube_Dt.Rows[i]["SUM_19_TUBE"].ToString();

                                if (temp["SUM_19_TUBE"] != "0")
                                {
                                    rate_TUBE = (float.Parse(cfs_pat_tube_Dt.Rows[i]["SUM_46_TUBE"].ToString()) / float.Parse(cfs_pat_tube_Dt.Rows[i]["SUM_19_TUBE"].ToString())) * 100;
                                }
                                else
                                {
                                    rate_TUBE = 0;
                                }
                                temp["RATE_TUBE"] = rate_TUBE.ToString("#0.00") + "%";
                                avg_SUM_46_TUBE += Int32.Parse(cfs_pat_tube_Dt.Rows[i]["SUM_46_TUBE"].ToString());
                                avg_SUM_19_TUBE += Int32.Parse(cfs_pat_tube_Dt.Rows[i]["SUM_19_TUBE"].ToString());
                                avg_rate_TUBE += rate_TUBE;
                                tempDt.Add(temp);
                            }

                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "";
                            temp["UNIT"] = "總計/平均";
                            temp["SUM_46_TUBE"] = ((float)avg_SUM_46_TUBE / (float)cfs_pat_tube_Dt.Rows.Count).ToString();
                            temp["SUM_19_TUBE"] = ((float)avg_SUM_19_TUBE / (float)cfs_pat_tube_Dt.Rows.Count).ToString();
                            if (float.Parse(temp["SUM_46_TUBE"]) != 0)
                            {
                                temp["RATE_TUBE"] = ((float.Parse(temp["SUM_46_TUBE"]) / float.Parse(temp["SUM_19_TUBE"])) * 100).ToString("#0.00") + "%";
                            }
                            else
                            {
                                temp["RATE_TUBE"] = "0%";
                            }
                            tempDt.Add(temp);
                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "定義說明： ";
                            tempDt.Add(temp);
                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "1.65歲(含)以上衰弱前期出院尿管存留率：(65歲(含)以上CFS_4-6 出院時尿管使用人次數/CFS_1-9出院時尿管使用總人次數)*100%";
                            tempDt.Add(temp);
                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "2.NA表示未進行CFS評估";
                            tempDt.Add(temp);
                            dt_ = tempDt;
                        }
                        List<string> Title = new List<string> { "序號", "單位", "CFS_4-6 出院時尿管使用人次數", "CFS_1-9出院時尿管使用總人次數", "衰弱前期出院尿管存留率" };
                        response(Title, Search_Title, dt_, "65歲(含)以上衰弱前期出院尿管存留統計表");
                    }
                    else if (op6)
                    {
                        del_sql += "SELECT SUM(CASE WHEN DELIRIUM_RESULT = 'Y' THEN 1 ELSE 0 END) AS SUM_DEL_Y, " +
                            "SUM(CASE WHEN DELIRIUM_RESULT = 'Y' OR DELIRIUM_RESULT = 'N' THEN 1 ELSE 0 END) AS SUM_DEL, " +
                            "UNIT ";
                        del_sql += "FROM del_pat ";
                        del_sql += "GROUP BY UNIT ";
                        DataTable del_pat_Dt = ass_m.DBExecSQL(del_sql);
                        if (del_pat_Dt.Rows.Count > 0)
                        {
                            float rate_DEL = 0;
                            float avg_SUM_DEL_Y = 0;
                            float avg_SUM_DEL = 0;
                            float avg_rate_DEL = 0;

                            tempDt = new List<Dictionary<string, string>>();
                            for (int i = 0; i < del_pat_Dt.Rows.Count; i++)
                            {
                                temp = new Dictionary<string, string>();
                                temp["NUM"] = (i + 1).ToString();
                                temp["UNIT"] = del_pat_Dt.Rows[i]["UNIT"].ToString();
                                temp["SUM_DEL_Y"] = del_pat_Dt.Rows[i]["SUM_DEL_Y"].ToString();
                                temp["SUM_DEL"] = del_pat_Dt.Rows[i]["SUM_DEL"].ToString();

                                if (temp["SUM_DEL"] != "0")
                                {
                                    rate_DEL = (float.Parse(del_pat_Dt.Rows[i]["SUM_DEL_Y"].ToString()) / float.Parse(del_pat_Dt.Rows[i]["SUM_DEL"].ToString())) * 100;
                                }
                                else
                                {
                                    rate_DEL = 0;
                                }
                                temp["RATE_DEL"] = rate_DEL.ToString("#0.00") + "%";
                                avg_SUM_DEL_Y += Int32.Parse(del_pat_Dt.Rows[i]["SUM_DEL_Y"].ToString());
                                avg_SUM_DEL += Int32.Parse(del_pat_Dt.Rows[i]["SUM_DEL"].ToString());
                                avg_rate_DEL += rate_DEL;
                                tempDt.Add(temp);
                            }

                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "";
                            temp["UNIT"] = "總計/平均";
                            temp["SUM_DEL_Y"] = ((float)avg_SUM_DEL_Y / (float)del_pat_Dt.Rows.Count).ToString();
                            temp["SUM_DEL"] = ((float)avg_SUM_DEL / (float)del_pat_Dt.Rows.Count).ToString();
                            temp["RATE_DEL"] = ((float.Parse(temp["SUM_DEL_Y"]) / float.Parse(temp["SUM_DEL"])) * 100).ToString("#0.00") + "%";
                            tempDt.Add(temp);
                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "定義說明： ";
                            tempDt.Add(temp);
                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "1.譫妄比率：(譫妄人次數 /譫妄評估總人次數)*100%";
                            tempDt.Add(temp);
                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "2.譫妄人次數：以瞻妄評估作業為依據，曾被評估有譫妄分子即計算1次，並以當次住院流水編號計算，至多僅計算一次";
                            tempDt.Add(temp);
                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "3.譫妄評估總人次數：以瞻妄評估作業為依據，有進行瞻妄評估分母即計算1次，並以當次住院流水編號計算，至多僅計算一次";
                            tempDt.Add(temp);
                            dt_ = tempDt;
                        }
                        List<string> Title = new List<string> { "序號", "單位", "譫妄人次數", "譫妄評估總人次數", "譫妄比率" };
                        response(Title, Search_Title, dt_, "譫妄評估統計表");
                    }
                    else if (op7)
                    {
                        del_sql += "SELECT SUM(CASE WHEN AGE >= 65 AND DELIRIUM_RESULT = 'Y' THEN 1 ELSE 0 END) AS SUM_65_DEL_Y, " +
                            "SUM(CASE WHEN AGE >= 65 THEN 1 ELSE 0 END) AS SUM_65_DEL, " +
                            "UNIT ";
                        del_sql += "FROM del_pat ";
                        del_sql += "GROUP BY UNIT ";
                        DataTable del_pat_Dt = ass_m.DBExecSQL(del_sql);
                        if (del_pat_Dt.Rows.Count > 0)
                        {
                            float rate_65_DEL = 0;
                            float avg_SUM_65_DEL_Y = 0;
                            float avg_SUM_65_DEL = 0;
                            float avg_rate_65_DEL = 0;

                            tempDt = new List<Dictionary<string, string>>();
                            for (int i = 0; i < del_pat_Dt.Rows.Count; i++)
                            {
                                temp = new Dictionary<string, string>();
                                temp["NUM"] = (i + 1).ToString();
                                temp["UNIT"] = del_pat_Dt.Rows[i]["UNIT"].ToString();
                                temp["SUM_65_DEL_Y"] = del_pat_Dt.Rows[i]["SUM_65_DEL_Y"].ToString();
                                temp["SUM_65_DEL"] = del_pat_Dt.Rows[i]["SUM_65_DEL"].ToString();

                                if (temp["SUM_65_DEL"] != "0")
                                {
                                    rate_65_DEL = (float.Parse(del_pat_Dt.Rows[i]["SUM_65_DEL_Y"].ToString()) / float.Parse(del_pat_Dt.Rows[i]["SUM_65_DEL"].ToString())) * 100;
                                }
                                else
                                {
                                    rate_65_DEL = 0;
                                }
                                temp["RATE_65_DEL"] = rate_65_DEL.ToString("#0.00") + "%";
                                avg_SUM_65_DEL_Y += Int32.Parse(del_pat_Dt.Rows[i]["SUM_65_DEL_Y"].ToString());
                                avg_SUM_65_DEL += Int32.Parse(del_pat_Dt.Rows[i]["SUM_65_DEL"].ToString());
                                avg_rate_65_DEL += rate_65_DEL;
                                tempDt.Add(temp);
                            }

                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "";
                            temp["UNIT"] = "總計/平均";
                            temp["SUM_65_DEL_Y"] = ((float)avg_SUM_65_DEL_Y / (float)del_pat_Dt.Rows.Count).ToString();
                            temp["SUM_65_DEL"] = ((float)avg_SUM_65_DEL / (float)del_pat_Dt.Rows.Count).ToString();
                            if (float.Parse(temp["SUM_65_DEL"]) != 0)
                            {
                                temp["RATE_65_DEL"] = ((float.Parse(temp["SUM_65_DEL_Y"]) / float.Parse(temp["SUM_65_DEL"])) * 100).ToString("#0.00") + "%";

                            }
                            else
                            {
                                temp["RATE_65_DEL"] = "0%";

                            }
                            tempDt.Add(temp);
                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "定義說明： ";
                            tempDt.Add(temp);
                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "1.65歲(含)以上長者譫妄發生率：(65歲(含)以上長者發生譫妄人次數 /65歲(含)以上長者譫妄評估總人次數)*100%";
                            tempDt.Add(temp);
                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "2.65歲(含)以上長者發生譫妄人次數：以瞻妄評估作業為依據，曾被評估有譫妄分子即計算1次，並以當次住院流水編號計算，至多僅計算一次";
                            tempDt.Add(temp);
                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "3.65歲(含)以上長者譫妄評估總人次數：以瞻妄評估作業為依據，有進行瞻妄評估分母即計算1次，並以當次住院流水編號計算，至多僅計算一次";
                            tempDt.Add(temp);
                            dt_ = tempDt;
                        }
                        List<string> Title = new List<string> { "序號", "單位", "65歲(含)以上長者發生譫妄人次數", "65歲(含)以上長者譫妄總人次數", "譫妄比率" };
                        response(Title, Search_Title, dt_, "65歲(含)以上長者譫妄統計表");
                    }

                }
                return new EmptyResult();
            }

            catch (Exception ex)
            {
                return new EmptyResult();
            }

        }

        [HttpPost]
        public ActionResult DeliriumAndCFSDetail(string OptionList)
        {
            Dictionary<string, string> Option = JsonConvert.DeserializeObject<Dictionary<string, string>>(OptionList);
            List<string> CostCode = JsonConvert.DeserializeObject<List<string>>(Option["CostCode"]);
            List<UserInfo> CostCodeUserList = new List<UserInfo>();
            List<Dictionary<string, string>> dt_ = new List<Dictionary<string, string>>();
            List<Dictionary<string, string>> Search_Title = new List<Dictionary<string, string>>();
            Dictionary<string, string> temp = null;
            List<string> Title = new List<string> { };
            byte[] TempByte = null;
            bool op1 = Option["DeliriumAndCFS"].Contains("DeliriumDetail"), op2 = Option["DeliriumAndCFS"].Contains("CFSDetail");
            if (op1)
            {
                Title = new List<string> { "序號", "單位", "病歷號碼","入院日期","譫妄評估日期","床號","出院日期","病人姓名",
                    "年齡","65歲(含)以上","科別","診斷","譫妄","尿管","DNR" };
            }
            else if (op2)
            {
                Title = new List<string> { "序號", "單位", "病歷號碼","入院日期","衰弱評估日期","床號","出院日期","病人姓名",
                    "年齡","65歲(含)以上","科別","診斷","CFS評估分數","mini-cog評估分數","尿管","DNR" };
            }
            temp = new Dictionary<string, string>();
            temp["Name"] = "搜尋區間";
            temp["Value"] = Option["StartDate"] + "-" + Option["EndDate"];
            Search_Title.Add(temp);
            temp = new Dictionary<string, string>();
            temp["Name"] = "單位";
            temp["Value"] = string.Join("、", JsonConvert.DeserializeObject<List<string>>(Option["CostName"]).ToArray());
            Search_Title.Add(temp);

            try
            {
                foreach (string CostCode_ in CostCode)
                {
                    TempByte = webService.GetUserList(CostCode_);
                    if (TempByte != null)
                        CostCodeUserList.AddRange(JsonConvert.DeserializeObject<List<UserInfo>>(CompressTool.DecompressString(TempByte)));
                }
                if (CostCodeUserList.Count > 0)
                {
                    List<Dictionary<string, string>> tempDt = new List<Dictionary<string, string>>();
                    List<Dictionary<string, string>> del_cfs_pat_tempDt = new List<Dictionary<string, string>>();
                    List<Dictionary<string, string>> tube_tempDt = new List<Dictionary<string, string>>();

                    string del_filter_sql = "SELECT FEE_NO, DELIRIUM_RESULT, MODIFY_USER, MODIFY_DATE_DEL ";
                    del_filter_sql += "FROM (SELECT FEE_NO, DELIRIUM_RESULT, MODIFY_USER, CASE WHEN ASSESS_DT IS NOT NULL THEN ASSESS_DT ELSE ASSESS_DT END AS MODIFY_DATE_DEL FROM DELIRIUM_DATA WHERE STATUS = 'Y' ) ";
                    del_filter_sql += "WHERE TRIM(MODIFY_USER) IN('08661','09227','MAYA'";
                    foreach (UserInfo userNo in CostCodeUserList)
                        del_filter_sql += ",'" + userNo.EmployeesNo.Trim() + "' ";
                    del_filter_sql += ") ";
                    del_filter_sql += "AND MODIFY_DATE_DEL BETWEEN to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') AND to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') ";

                    string cfs_filter_sql = "SELECT FEE_NO, CFS_SCORE, MC_SCORE, MODIFY_USER, MODIFY_DATE_CFS, COG_RESULT ";
                    cfs_filter_sql += "FROM (SELECT FEE_NO, CFS_SCORE, MC_SCORE, MODIFY_USER, COG_RESULT, CASE WHEN ASSESS_DT IS NOT NULL THEN ASSESS_DT ELSE ASSESS_DT END AS MODIFY_DATE_CFS FROM CFS_DATA WHERE STATUS = 'Y') ";
                    cfs_filter_sql += "WHERE TRIM(MODIFY_USER) IN('08661', '09227','MAYA'";
                    foreach (UserInfo userNo in CostCodeUserList)
                        cfs_filter_sql += ",'" + userNo.EmployeesNo.Trim() + "' ";
                    cfs_filter_sql += ") ";
                    cfs_filter_sql += "AND MODIFY_DATE_CFS BETWEEN to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') AND to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') ";

                    string sql = "";
                    string fee_no_sql = "";

                    if (op1)
                    {
                        sql = "WITH del_filter AS ( ";
                        sql += del_filter_sql + "), ";
                        sql += "del AS ( ";
                        sql += "SELECT FEE_NO, DELIRIUM_RESULT, MODIFY_USER, MODIFY_DATE_DEL ";
                        sql += "FROM del_filter) ";

                        //223.224才能用
                        sql += ", del_station AS (";
                        sql += "SELECT A.*, B.DEPT_NAME AS UNIT ";
                        sql += "FROM del A LEFT JOIN V_CLERK_DATA B ";
                        sql += "ON TRIM(A.MODIFY_USER) = TRIM(B.CLERK_ID) ) ";

                        fee_no_sql = sql + "SELECT DISTINCT FEE_NO FROM del_station";
                    }
                    else if (op2)
                    {
                        sql = "WITH cfs_filter AS ( ";
                        sql += cfs_filter_sql + "), ";
                        sql += "cfs AS ( ";
                        sql += "SELECT FEE_NO, CFS_SCORE, MC_SCORE, MODIFY_USER, MODIFY_DATE_CFS, COG_RESULT ";
                        sql += "FROM cfs_filter) ";

                        //223.224才能用
                        sql += ", cfs_station AS (";
                        sql += "SELECT A.*, B.DEPT_NAME AS UNIT ";
                        sql += "FROM cfs A LEFT JOIN V_CLERK_DATA B ";
                        sql += "ON TRIM(A.MODIFY_USER) = TRIM(B.CLERK_ID) ) ";

                        fee_no_sql = sql + "SELECT DISTINCT FEE_NO FROM cfs_station";
                    }

                    //sql += "del_cfs_filter AS ( ";
                    //sql += "SELECT FEE_NO, DELIRIUM_RESULT, CFS_SCORE, MC_SCORE, MODIFY_USER, MODIFY_DATE_DEL, MODIFY_DATE_CFS, COG_RESULT FROM ( ";
                    //sql += "SELECT A.FEE_NO, A.DELIRIUM_RESULT, A.MODIFY_DATE_DEL, B.MODIFY_DATE_CFS, B.CFS_SCORE, B.MC_SCORE, B.COG_RESULT, A.MODIFY_USER ";
                    //sql += "FROM del A LEFT JOIN cfs B ON A.FEE_NO = B.FEE_NO ";
                    //sql += "UNION ";
                    //sql += "SELECT B.FEE_NO, A.DELIRIUM_RESULT, A.MODIFY_DATE_DEL, B.MODIFY_DATE_CFS, B.CFS_SCORE, B.MC_SCORE, B.COG_RESULT, B.MODIFY_USER ";
                    //sql += "FROM del A RIGHT JOIN cfs B ON A.FEE_NO = B.FEE_NO WHERE A.FEE_NO IS NULL )) ";


                    DataTable fee_no_Dt = ass_m.DBExecSQL(fee_no_sql);
                    List<string> sel_fee_no = new List<string> { };
                    if (fee_no_Dt.Rows.Count > 0)
                    {
                        for (var i = 0; i < fee_no_Dt.Rows.Count; i++)
                        {
                            sel_fee_no.Add(fee_no_Dt.Rows[i]["FEE_NO"].ToString());
                        }
                    }

                    string pat_filter = "SELECT IX.ADMIT_NO FeeNo,IX.CHART_NO ChartNo,PT.BED_NO BedNo,CASE CT.SEX WHEN 'M' THEN '1' WHEN 'F' THEN '2' END PatientGender,CT.PT_NAME PatientName," +
                        "TO_CHAR(SUBSTR(CT.BIRTH_DATE, 2, 7)) Birthday,DR.DOCTOR_NAME DocName, PT.VS_NO DocNo, to_date(cast( IX.ADMIT_DATE +19110000 as varchar(8)),'yyyy/mm/dd') InDate, IX.ADMIT_TIME InTime," +
                        "IX.DISCHARGE_DATE OutDate," +
                        "IX.DISCHARGE_TIME OutTime," +
                        "(SELECT DISTINCT MAX(DIAGNOSIS_CODE) FROM IPD.ORDAIPD2 WHERE ADMIT_NO = IX.ADMIT_NO AND DIAGNOSIS_TYPE IN('11', '21')) ICD9_code1," +
                        "(SELECT DISTINCT max(DIAGNOSIS_CHINESE_NAME) FROM IPD.ORDAIPD2 WHERE ADMIT_NO = IX.ADMIT_NO AND DIAGNOSIS_CODE IN(SELECT DISTINCT MAX(DIAGNOSIS_CODE) FROM IPD.ORDAIPD2 WHERE ADMIT_NO = IX.ADMIT_NO AND DIAGNOSIS_TYPE IN('11', '21'))) mdiag_desc1," +
                        "PT.DIV_NO DeptNo,(SELECT DIV_SHORT_NAME FROM MAST.DIV WHERE DIV_NO = PT.DIV_NO) DeptName," +
                        "CT.ID_NO PatientID, (CASE PT.PT_TYPE WHEN '02' THEN '健保' WHEN '01' THEN '自費' ELSE '自費'END) PayInfo," +
                        "(SELECT CASE WHEN TRIM(DNR_FLAG) IS NOT NULL THEN 'Y' ELSE 'N' END from CHART.PTDNRDATA where ID_NO =CT.ID_NO) DNR " +
                        "FROM IPD.IPDINDEX IX, CS.V_ALL_PTIPD PT, CHART.CHART CT, MAST.DOCTOR DR " +
                        "WHERE IX.ADMIT_NO IN ('' ";
                    foreach (var FEENO in sel_fee_no)
                    {
                        pat_filter += ",'" + FEENO.Trim() + "' ";
                    }
                    pat_filter += ") " +
                    "AND IX.ADMIT_NO = PT.ADMIT_NO " +
                    "AND IX.CUT_NO = PT.CUT_NO " +
                    "AND IX.CHART_NO = CT.CHART_NO " +
                    "AND PT.VS_NO = DR.DOCTOR_NO " +
                    "AND IX.CUT_NO IN(SELECT MAX(CUT_NO) FROM IPD.IPDINDEX WHERE ADMIT_NO = IX.ADMIT_NO)";

                    sql += ",pat AS ( ";
                    sql += pat_filter + "), ";
                    //......................

                    sql += "del_cfs_pat AS ( ";
                    sql += "SELECT A.*, B.BEDNO, B.PATIENTNAME, B.BIRTHDAY, TRUNC((sysdate - to_date(cast( B.BIRTHDAY +19110000 as varchar(8)),'yyyyMMdd'))/365.25) AS AGE, ";
                    sql += "B.INDATE, B.OUTDATE, B.CHARTNO, B.DEPTNAME, B.MDIAG_DESC1, B.DEPTNO, B.DNR ";
                    if (op1)
                    {
                        sql += "FROM del_station A LEFT JOIN pat B ";
                    }
                    else if (op2)
                    {
                        sql += "FROM cfs_station A LEFT JOIN pat B ";
                    }
                    sql += "ON A.FEE_NO = B.FeeNo ), ";

                    sql += "del_cfs_pat_tube_filter AS ( ";
                    if (op1)
                    {
                        sql += "SELECT A.FEE_NO, " +
                        "SUM(CASE WHEN A.MODIFY_DATE_DEL >= B.STARTTIME AND ( A.MODIFY_DATE_DEL <= B.ENDTIME OR B.ENDTIME IS NULL ) THEN 1 ELSE 0 END) AS TUBE_SUM ";
                    }
                    else if (op2)
                    {
                        sql += "SELECT A.FEE_NO, " +
                            "SUM(CASE WHEN A.MODIFY_DATE_CFS >= B.STARTTIME AND (A.MODIFY_DATE_CFS <= B.ENDTIME OR B.ENDTIME IS NULL ) " +
                            " THEN 1 ELSE 0 END) AS TUBE_SUM ";
                    }
                    sql += "FROM del_cfs_pat A LEFT JOIN TUBE B ";
                    sql += "ON A.FEE_NO = B.FEENO ";
                    sql += "WHERE TYPEID = 'TK00000001' OR TYPEID = 'TK00000002' ";
                    sql += "GROUP BY A.FEE_NO),  ";

                    sql += "del_cfs_pat_tube AS ( ";
                    sql += "SELECT A.*, CASE WHEN B.TUBE_SUM > 0 THEN 'Y' ELSE 'N' END AS TUBE ";
                    sql += "FROM del_cfs_pat A LEFT JOIN del_cfs_pat_tube_filter B ";
                    sql += "ON A.FEE_NO = B.FEE_NO) ";

                    sql += "SELECT * FROM del_cfs_pat_tube ORDER BY UNIT";

                    string tube_sql = "SELECT FEENO, STARTTIME, ENDTIME, TYPEID ";
                    tube_sql += "FROM TUBE ";
                    tube_sql += "WHERE TYPEID = 'TK00000001' OR TYPEID = 'TK00000002'";

                    DataTable del_cfs_pat_tube_Dt = ass_m.DBExecSQL(sql);
                    DataTable tube_Dt = ass_m.DBExecSQL(tube_sql);

                    if (del_cfs_pat_tube_Dt.Rows.Count > 0)
                    {
                        tempDt = new List<Dictionary<string, string>>();
                        for (int i = 0; i < del_cfs_pat_tube_Dt.Rows.Count; i++)
                        {
                            string feeno = del_cfs_pat_tube_Dt.Rows[i]["FEE_NO"].ToString();
                            byte[] listByteCodePt = webService.GetPatientInfo(feeno);
                            string listJsonArrayPt = CompressTool.DecompressString(listByteCodePt);
                            PatientInfo pt_info = JsonConvert.DeserializeObject<PatientInfo>(listJsonArrayPt);
                            string ptcost = pt_info.CostCenterName.ToString();
                            string ptbed = pt_info.BedNo.ToString();

                            temp = new Dictionary<string, string>();
                            //temp["CREATTIME"] = Convert.ToDateTime(Dt.Rows[i]["CREATTIME"].ToString()).ToString("yyyy/MM/dd");
                            temp["SN"] = (i + 1).ToString();
                            temp["UNIT"] = ptcost;
                            temp["CHARTNO"] = del_cfs_pat_tube_Dt.Rows[i]["CHARTNO"].ToString();


                            temp["INDATE"] = del_cfs_pat_tube_Dt.Rows[i]["INDATE"].ToString();
                            if (op1)
                            {
                                if (del_cfs_pat_tube_Dt.Rows[i]["MODIFY_DATE_DEL"].ToString() != "" && del_cfs_pat_tube_Dt.Rows[i]["MODIFY_DATE_DEL"].ToString() != null)
                                {
                                    temp["MODIFY_DATE_DEL"] = del_cfs_pat_tube_Dt.Rows[i]["MODIFY_DATE_DEL"].ToString();
                                }
                                else
                                {
                                    temp["MODIFY_DATE_DEL"] = "";
                                }
                            }
                            else if (op2)
                            {
                                if (del_cfs_pat_tube_Dt.Rows[i]["MODIFY_DATE_CFS"].ToString() != "" && del_cfs_pat_tube_Dt.Rows[i]["MODIFY_DATE_CFS"].ToString() != null)
                                {
                                    temp["MODIFY_DATE_CFS"] = del_cfs_pat_tube_Dt.Rows[i]["MODIFY_DATE_CFS"].ToString();
                                }
                                else
                                {
                                    temp["MODIFY_DATE_CFS"] = "";
                                }
                            }
                            temp["BEDNO"] = ptbed;

                            if (int.Parse(del_cfs_pat_tube_Dt.Rows[i]["OUTDATE"].ToString()) == 0)
                            {
                                temp["OUTDATE"] = "";
                            }
                            else
                            {
                                temp["OUTDATE"] = del_cfs_pat_tube_Dt.Rows[i]["OUTDATE"].ToString();
                            }
                            temp["PATIENTNAME"] = del_cfs_pat_tube_Dt.Rows[i]["PATIENTNAME"].ToString();
                            temp["AGE"] = del_cfs_pat_tube_Dt.Rows[i]["AGE"].ToString();
                            if (int.Parse(temp["AGE"]) >= 65)
                            {
                                temp["AGE_65"] = "V";
                            }
                            else
                            {
                                temp["AGE_65"] = "";
                            }
                            temp["DEPTNAME"] = del_cfs_pat_tube_Dt.Rows[i]["DEPTNAME"].ToString();
                            temp["MDIAG_DESC1"] = del_cfs_pat_tube_Dt.Rows[i]["MDIAG_DESC1"].ToString();
                            if (op1)
                            {
                                if (del_cfs_pat_tube_Dt.Rows[i]["DELIRIUM_RESULT"].ToString() != "" && del_cfs_pat_tube_Dt.Rows[i]["DELIRIUM_RESULT"].ToString() != null)
                                {
                                    var result = del_cfs_pat_tube_Dt.Rows[i]["DELIRIUM_RESULT"].ToString();
                                    switch (result)
                                    {
                                        case "Y":
                                            temp["DELIRIUM_RESULT"] = "有譫妄";
                                            break;
                                        case "E":
                                            temp["DELIRIUM_RESULT"] = "無法評估";
                                            break;
                                        case "U":
                                            temp["DELIRIUM_RESULT"] = "無法評估";
                                            break;
                                        case "N":
                                            temp["DELIRIUM_RESULT"] = "無譫妄";
                                            break;
                                        default:
                                            temp["DELIRIUM_RESULT"] = "無評估";
                                            break;
                                    }
                                }
                                else
                                {
                                    temp["DELIRIUM_RESULT"] = "無評估";
                                }
                                //temp["DELIRIUM_RESULT"] = del_cfs_pat_tube_Dt.Rows[i]["DELIRIUM_RESULT"].ToString();
                            }
                            else if (op2)
                            {
                                if (del_cfs_pat_tube_Dt.Rows[i]["CFS_SCORE"].ToString() != "" && del_cfs_pat_tube_Dt.Rows[i]["CFS_SCORE"].ToString() != null)
                                {
                                    temp["CFS_SCORE"] = del_cfs_pat_tube_Dt.Rows[i]["CFS_SCORE"].ToString();
                                }
                                else
                                {
                                    temp["CFS_SCORE"] = "無需評估";
                                }
                                if (del_cfs_pat_tube_Dt.Rows[i]["MC_SCORE"].ToString() != "" && del_cfs_pat_tube_Dt.Rows[i]["MC_SCORE"].ToString() != null)
                                {
                                    temp["MC_SCORE"] = del_cfs_pat_tube_Dt.Rows[i]["MC_SCORE"].ToString();
                                }
                                else
                                {
                                    if (del_cfs_pat_tube_Dt.Rows[i]["COG_RESULT"].ToString() == "")
                                    {
                                        temp["MC_SCORE"] = "無需評估";

                                    }
                                    else
                                    {
                                        temp["MC_SCORE"] = "無法評估，" + del_cfs_pat_tube_Dt.Rows[i]["COG_RESULT"].ToString();

                                    }
                                }
                            }
                            temp["TUBE"] = del_cfs_pat_tube_Dt.Rows[i]["TUBE"].ToString();
                            if (del_cfs_pat_tube_Dt.Rows[i]["DNR"].ToString() != null && del_cfs_pat_tube_Dt.Rows[i]["DNR"].ToString() != "")
                            {
                                temp["DNR"] = del_cfs_pat_tube_Dt.Rows[i]["DNR"].ToString();
                            }
                            else
                            {
                                temp["DNR"] = "N";
                            }
                            tempDt.Add(temp);
                        }
                        dt_ = tempDt;
                    }
                    //if (tube_Dt.Rows.Count > 0)
                    //{
                    //    tempDt = new List<Dictionary<string, string>>();
                    //    for (int i = 0; i < tube_Dt.Rows.Count; i++)
                    //    {
                    //        temp = new Dictionary<string, string>();
                    //        temp["FEE_NO"] = tube_Dt.Rows[i]["FEENO"].ToString();
                    //        temp["STARTTIME"] = Convert.ToDateTime(tube_Dt.Rows[i]["STARTTIME"].ToString()).ToString("yyyy/MM/dd HH:mm:ss");
                    //        var test = tube_Dt.Rows[i]["ENDTIME"].ToString();
                    //        if (tube_Dt.Rows[i]["ENDTIME"].ToString() != "" && tube_Dt.Rows[i]["ENDTIME"].ToString() != null)
                    //        {
                    //            temp["ENDTIME"] = Convert.ToDateTime(tube_Dt.Rows[i]["ENDTIME"].ToString()).ToString("yyyy/MM/dd HH:mm:ss");
                    //        }
                    //        else
                    //        {
                    //            temp["ENDTIME"] = "";
                    //        }
                    //        temp["TYPEID"] = tube_Dt.Rows[i]["TYPEID"].ToString();
                    //        tempDt.Add(temp);
                    //    }
                    //    tube_tempDt = tempDt;
                    //}

                    //foreach (var del_cfs_pat in del_cfs_pat_tempDt)
                    //{
                    //    foreach (var tube in tube_tempDt)
                    //    {
                    //        var del_date = del_cfs_pat["MODIFY_DATE_DEL"];
                    //        var cfs_date = del_cfs_pat["MODIFY_DATE_CFS"];
                    //        var tube_str = tube["STARTTIME"];
                    //        var tube_end = tube["ENDTIME"];
                    //        if (del_cfs_pat["FEE_NO"] == tube["FEE_NO"])
                    //        {
                    //            bool in_flag = false;
                    //            if (del_date != "" && del_date != null)
                    //            {
                    //                if (Convert.ToDateTime(del_date) >= Convert.ToDateTime(tube_str))
                    //                {
                    //                    if (tube_end == "" || tube_end == null)
                    //                    {
                    //                        in_flag = true;
                    //                    }
                    //                    else if (Convert.ToDateTime(del_date) <= Convert.ToDateTime(tube_end))
                    //                    {
                    //                        in_flag = true;
                    //                    }
                    //                }
                    //            }
                    //            if (cfs_date != "" && cfs_date != null)
                    //            {
                    //                if (Convert.ToDateTime(cfs_date) >= Convert.ToDateTime(tube_str))
                    //                {
                    //                    if (tube_end == "" || tube_end == null)
                    //                    {
                    //                        in_flag = true;
                    //                    }
                    //                    else if (Convert.ToDateTime(cfs_date) <= Convert.ToDateTime(tube_end))
                    //                    {
                    //                        in_flag = true;
                    //                    }
                    //                }
                    //            }
                    //            if (in_flag)
                    //            {
                    //                del_cfs_pat["TUBE"] = "Y";
                    //            }
                    //            else
                    //            {
                    //                del_cfs_pat["TUBE"] = "N";
                    //            }
                    //        }
                    //        else
                    //        {
                    //            del_cfs_pat["TUBE"] = "N";
                    //        }
                    //    }
                    //}

                    //for (int i = 0; i < del_cfs_pat_tempDt.Count; i++)
                    //{
                    //    var age = 0;
                    //    temp = new Dictionary<string, string>();
                    //    temp["NUM"] = (i + 1).ToString();
                    //    temp["UNIT"] = "";
                    //    temp["BEDNO"] = del_cfs_pat_tempDt[i]["BEDNO"].ToString();
                    //    temp["INDATE"] = del_cfs_pat_tempDt[i]["INDATE"].ToString();
                    //    temp["ACESSDATE"] = "";
                    //    temp["OUTDATE"] = del_cfs_pat_tempDt[i]["OUTDATE"].ToString();
                    //    temp["CHARTNO"] = del_cfs_pat_tempDt[i]["CHARTNO"].ToString();
                    //    temp["PATIENTNAME"] = del_cfs_pat_tempDt[i]["PATIENTNAME"].ToString();
                    //    if (del_cfs_pat_tempDt[i]["BIRTHDAY"].ToString() != "" && del_cfs_pat_tempDt[i]["BIRTHDAY"].ToString() != null)
                    //    {
                    //        DateTime birth = DateTime.ParseExact(del_cfs_pat_tempDt[i]["BIRTHDAY"].ToString(), "yyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
                    //        var bir_year = birth.Year;
                    //        bir_year = bir_year + 1911;
                    //        var bir_mon = birth.Month;
                    //        var bir_day = birth.Day;
                    //        DateTime date_now = DateTime.Now;
                    //        var year_now = date_now.Year;
                    //        var month_now = date_now.Month;
                    //        var day_now = date_now.Day;
                    //        if (bir_mon == month_now)
                    //        {
                    //            if (bir_day <= day_now)
                    //            {
                    //                age = year_now - bir_year;
                    //            }
                    //            else
                    //            {
                    //                age = year_now - bir_year - 1;
                    //            }
                    //        }
                    //        else if (bir_mon >= month_now)
                    //        {
                    //            age = year_now - bir_year - 1;
                    //        }
                    //        else
                    //        {
                    //            age = year_now - bir_year;
                    //        }
                    //        if (age <= 0)
                    //        {
                    //            temp["AGE"] = "0";
                    //        }
                    //        else
                    //        {
                    //            temp["AGE"] = age.ToString();
                    //        }
                    //    }
                    //    else
                    //    {
                    //        temp["AGE"] = "";
                    //    }
                    //    if (age >= 65)
                    //    {
                    //        temp["65_Y_N"] = "V";
                    //    }
                    //    else
                    //    {
                    //        temp["65_Y_N"] = "";
                    //    }
                    //    temp["DEPTNAME"] = del_cfs_pat_tempDt[i]["DEPTNAME"].ToString();
                    //    temp["MDIAG_DESC1"] = del_cfs_pat_tempDt[i]["MDIAG_DESC1"].ToString();
                    //    if (del_cfs_pat_tempDt[i]["DELIRIUM_RESULT"].ToString() != "" && del_cfs_pat_tempDt[i]["DELIRIUM_RESULT"].ToString() != null)
                    //    {
                    //        temp["DELIRIUM_RESULT"] = del_cfs_pat_tempDt[i]["DELIRIUM_RESULT"].ToString();
                    //    }
                    //    else
                    //    {
                    //        temp["DELIRIUM_RESULT"] = "無評估";
                    //    }
                    //    if (del_cfs_pat_tempDt[i]["CFS_SCORE"].ToString() != "" && del_cfs_pat_tempDt[i]["CFS_SCORE"].ToString() != null)
                    //    {
                    //        temp["CFS_SCORE"] = del_cfs_pat_tempDt[i]["CFS_SCORE"].ToString();
                    //    }
                    //    else
                    //    {
                    //        temp["CFS_SCORE"] = "無評估";
                    //    }
                    //    temp["MC_SCORE"] = del_cfs_pat_tempDt[i]["MC_SCORE"].ToString();
                    //    temp["TUBE"] = del_cfs_pat_tempDt[i]["TUBE"].ToString();
                    //    dt_.Add(temp);
                    //}
                    if (op1)
                    {
                        response(Title, Search_Title, dt_, "譫妄明細表");
                    }
                    else if (op2)
                    {
                        response(Title, Search_Title, dt_, "衰弱明細表");
                    }
                }
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                return new EmptyResult();
            }

        }

        [HttpPost]
        public ActionResult BundleCareDetail(string OptionList)
        {
            Dictionary<string, string> Option = JsonConvert.DeserializeObject<Dictionary<string, string>>(OptionList);
            List<string> CostCode = JsonConvert.DeserializeObject<List<string>>(Option["CostCode"]);
            List<UserInfo> CostCodeUserList = new List<UserInfo>();
            List<Dictionary<string, string>> dt_ = new List<Dictionary<string, string>>();
            List<Dictionary<string, string>> Search_Title = new List<Dictionary<string, string>>();
            Dictionary<string, string> temp = null;
            List<BedItem> BedList = new List<BedItem>();
            byte[] TempByte = null;
            temp = new Dictionary<string, string>();
            temp["Name"] = "搜尋區間";
            temp["Value"] = Option["StartDate"] + "-" + Option["EndDate"];
            Search_Title.Add(temp);
            temp = new Dictionary<string, string>();
            temp["Name"] = "單位";
            temp["Value"] = string.Join("、", JsonConvert.DeserializeObject<List<string>>(Option["CostName"]).ToArray());
            Search_Title.Add(temp);
            bool op1 = Option["BundleCare"].Contains("UTIBundleCareDetail"), op2 = Option["BundleCare"].Contains("VAPBundleCareDetail"),
                op3 = Option["BundleCare"].Contains("CVCBundleCareDetail");
            DateTime startOption = DateTime.Parse(Option["StartDate"].ToString());
            string date_ROC = startOption.ToString("yyyyMMdd");
            try
            {
                foreach (string CostCode_ in CostCode)
                {
                    TempByte = webService.GetBedList(CostCode_);
                    if (TempByte != null)
                        BedList.AddRange(JsonConvert.DeserializeObject<List<BedItem>>(CompressTool.DecompressString(TempByte)));
                }
                if (BedList.Count > 0)
                {
                    //string bundle_tube_sql = "SELECT BC.*, VC.DEPT_NAME, TB.STARTTIME, TB.ENDTIME, TB.POSITION,(SELECT TB_K.KINDNAME FROM TUBE_KIND TB_K WHERE TB_K.KINDID=TB.TYPEID) TUBE_NAME \n";
                    //bundle_tube_sql += "FROM(SELECT A.FEENO, A.BC_TYPE, A.TUBEID, A.TABLE_ID, \n";
                    //bundle_tube_sql += "CASE WHEN A.RECORDTIME IS NOT NULL THEN A.RECORDTIME ELSE A.CREATE_TIME END MODIFY_DATE, B.ITEM_ID, B.ITEM_VALUE, \n";
                    //bundle_tube_sql += "CASE WHEN A.UPDATE_ID IS NOT NULL THEN A.UPDATE_ID ELSE A.CREATE_ID END MODIFY_ID \n";
                    //if (op1)
                    //{
                    //    bundle_tube_sql += "FROM(SELECT * FROM NIS_TUBE_ASSESS_BUNDLE_MASTER WHERE BC_TYPE = 'UTI' AND DELETE_TIME IS NULL) A \n";
                    //}
                    //else if (op2)
                    //{
                    //    bundle_tube_sql += "FROM(SELECT * FROM NIS_TUBE_ASSESS_BUNDLE_MASTER WHERE BC_TYPE = 'VAP' AND DELETE_TIME IS NULL) A \n";
                    //}
                    //else if (op3)
                    //{
                    //    bundle_tube_sql += "FROM(SELECT * FROM NIS_TUBE_ASSESS_BUNDLE_MASTER WHERE BC_TYPE = 'BSI' AND DELETE_TIME IS NULL) A \n";
                    //}
                    //bundle_tube_sql += "INNER JOIN(SELECT ITEM_ID, TABLE_ID, LISTAGG(ITEM_VALUE, ', ') WITHIN GROUP(ORDER BY ITEM_VALUE) ITEM_VALUE \n";
                    //bundle_tube_sql += "INNER JOIN(SELECT TABLE_ID,ITEM_ID,rtrim (xmlagg (xmlelement(e,ITEM_VALUE||' ')).extract ('//text()'), ' ') AS ITEM_VALUE \n";
                    //bundle_tube_sql += "FROM NIS_TUBE_ASSESS_BUNDLE_DETAIL \n";
                    //bundle_tube_sql += "GROUP BY TABLE_ID, ITEM_ID) B \n";
                    //bundle_tube_sql += "ON A.TABLE_ID = B.TABLE_ID) BC \n";
                    //bundle_tube_sql += "INNER JOIN TUBE TB \n";
                    //bundle_tube_sql += "ON BC.TUBEID = TB.TUBEID \n";
                    //bundle_tube_sql += "LEFT JOIN V_CLERK_DATA VC \n";
                    //bundle_tube_sql += "ON TRIM(BC.MODIFY_ID) = TRIM(VC.CLERK_ID) \n";
                    //bundle_tube_sql += "WHERE TRIM(MODIFY_ID) IN('08661','09227','MAYA'";
                    //foreach (UserInfo userNo in CostCodeUserList)
                    //    bundle_tube_sql += ",'" + userNo.EmployeesNo.Trim() + "' ";
                    //bundle_tube_sql += ") \n";
                    //bundle_tube_sql += "AND MODIFY_DATE BETWEEN to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') AND to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') \n";
                    //bundle_tube_sql += "ORDER BY BC.TABLE_ID, BC.ITEM_ID \n";

                    //string fee_no_sql = "WITH BUNDLE_TUBE AS ( " + bundle_tube_sql + ") SELECT DISTINCT FEENO FROM BUNDLE_TUBE";
                    //DataTable fee_no_Dt = ass_m.DBExecSQL(fee_no_sql);
                    //List<string> sel_fee_no = new List<string> { };
                    //if (fee_no_Dt.Rows.Count > 0)
                    //{
                    //    for (var i = 0; i < fee_no_Dt.Rows.Count; i++)
                    //    {
                    //        sel_fee_no.Add(fee_no_Dt.Rows[i]["FEENO"].ToString());
                    //    }
                    //}

                    //string pat_filter = "SELECT IX.ADMIT_NO FeeNo_,IX.CHART_NO ChartNo,PT.BED_NO BedNo,CASE CT.SEX WHEN 'M' THEN '1' WHEN 'F' THEN '2' END PatientGender,CT.PT_NAME PatientName, \n" +
                    //    "TO_CHAR(SUBSTR(CT.BIRTH_DATE, 2, 7)) Birthday,DR.DOCTOR_NAME DocName, PT.VS_NO DocNo, IX.ADMIT_DATE InDate, IX.ADMIT_TIME InTime, \n" +
                    //    "IX.DISCHARGE_DATE OutDate, \n" +
                    //    "IX.DISCHARGE_TIME OutTime, \n" +
                    //    "(SELECT DISTINCT MAX(DIAGNOSIS_CODE) FROM IPD.ORDAIPD2 WHERE ADMIT_NO = IX.ADMIT_NO AND DIAGNOSIS_TYPE IN('11', '21')) ICD9_code1, \n" +
                    //    "(SELECT DISTINCT max(DIAGNOSIS_CHINESE_NAME) FROM IPD.ORDAIPD2 WHERE ADMIT_NO = IX.ADMIT_NO AND DIAGNOSIS_CODE IN(SELECT DISTINCT MAX(DIAGNOSIS_CODE) FROM IPD.ORDAIPD2 WHERE ADMIT_NO = IX.ADMIT_NO AND DIAGNOSIS_TYPE IN('11', '21'))) mdiag_desc1, \n" +
                    //    "PT.DIV_NO DeptNo,(SELECT DIV_SHORT_NAME FROM MAST.DIV WHERE DIV_NO = PT.DIV_NO) DeptName, \n" +
                    //    "CT.ID_NO PatientID, (CASE PT.PT_TYPE WHEN '02' THEN '健保' WHEN '01' THEN '自費' ELSE '自費'END) PayInfo, \n" +
                    //    "(SELECT CASE WHEN TRIM(DNR_FLAG) IS NOT NULL THEN 'Y' ELSE 'N' END from CHART.PTDNRDATA where ID_NO =CT.ID_NO) DNR \n" +
                    //    "FROM IPD.IPDINDEX IX, CS.V_ALL_PTIPD PT, CHART.CHART CT, MAST.DOCTOR DR \n" +
                    //    "WHERE IX.ADMIT_NO IN ('' ";
                    //foreach (var FEENO in sel_fee_no)
                    //{
                    //    pat_filter += ",'" + FEENO.Trim() + "' ";
                    //}
                    //pat_filter += ") \n" +
                    //"AND IX.ADMIT_NO = PT.ADMIT_NO \n" +
                    //"AND IX.CUT_NO = PT.CUT_NO \n" +
                    //"AND IX.CHART_NO = CT.CHART_NO \n" +
                    //"AND PT.VS_NO = DR.DOCTOR_NO \n" +
                    //"AND IX.CUT_NO IN(SELECT MAX(CUT_NO) FROM IPD.IPDINDEX WHERE ADMIT_NO = IX.ADMIT_NO)";

                    //string sql = "WITH BUNDLE_TUBE AS ( ";
                    //sql += bundle_tube_sql + "), \n";
                    //sql += "PAT AS (" + pat_filter + ") \n";
                    //sql += "SELECT * FROM BUNDLE_TUBE A LEFT JOIN PAT B \n";
                    //sql += "ON A.FEENO = B.FEENO_ \n";

                    //string sql = "WITH BUNDLE_TUBE AS ( ";
                    //sql += bundle_tube_sql + "), \n";
                    //sql += "SELECT * FROM BUNDLE_TUBE";

                    string ipd_ipdindex_sql = "SELECT * FROM \n";
                    ipd_ipdindex_sql += "(SELECT A.*, row_number() OVER(PARTITION BY admit_no ORDER BY cut_no DESC) AS sn \n";
                    ipd_ipdindex_sql += "FROM ipd.ipdindex A) \n";
                    ipd_ipdindex_sql += "WHERE sn = 1";

                    string cs_v_all_ptipd_sql = "SELECT * FROM (\n";
                    cs_v_all_ptipd_sql += "SELECT PT.ADMIT_NO,PT.CHART_NO,PT.BED_NO,ROW_NUMBER() OVER (PARTITION BY PT.ADMIT_NO ORDER BY PT.CUT_NO DESC) sn \n";
                    cs_v_all_ptipd_sql += "FROM CS.V_ALL_PTIPD PT )\n";
                    cs_v_all_ptipd_sql += "WHERE sn = 1";

                    string sql = "WITH BUNDLE_TUBE AS (SELECT * FROM  TUBE TM LEFT JOIN TUBE_KIND TK ON TM.TYPEID = TK.KINDID), \n";
                    sql += "COMBIN AS( \n";
                    sql += "SELECT BM.CREATE_TIME,BM.RECORDTIME, BM.FEENO, BT.KINDNAME,BT.STARTTIME,BT.ENDTIME, PT.BED_NO, D.* FROM  NIS_TUBE_ASSESS_BUNDLE_MASTER BM \n";
                    sql += "LEFT JOIN BUNDLE_TUBE BT ON BM.TUBEID = BT.TUBEID \n";
                    sql += "INNER JOIN(SELECT TABLE_ID,ITEM_ID,rtrim (xmlagg (xmlelement(e,ITEM_VALUE||' ')).extract ('//text()'), ' ') AS ITEM_VALUE \n";
                    sql += "FROM NIS_TUBE_ASSESS_BUNDLE_DETAIL \n";
                    sql += "GROUP BY TABLE_ID, ITEM_ID) D \n";
                    sql += "ON BM.TABLE_ID = D.TABLE_ID \n";
                    sql += "LEFT JOIN (" + cs_v_all_ptipd_sql + ") PT ON BM.FEENO = PT.ADMIT_NO \n";
                    sql += " LEFT JOIN (" + ipd_ipdindex_sql + ") IX  ON BM.FEENO = IX.ADMIT_NO \n";
                    if (op1)
                    {
                        sql += " WHERE BC_TYPE = 'UTI' AND DELETE_TIME IS NULL \n";
                    }
                    else if (op2)
                    {
                        sql += " WHERE BC_TYPE = 'VAP' AND DELETE_TIME IS NULL \n";
                    }
                    else if (op3)
                    {
                        sql += " WHERE BC_TYPE = 'BSI' AND DELETE_TIME IS NULL \n";
                    }

                    sql += "AND to_date(to_char(BM.CREATE_TIME,'yyyy-MM-dd'),'yyyy-MM-dd') < to_date(to_char(BT.ENDTIME,'yyyy-MM-dd'),'yyyy-MM-dd')";//拔管當天不算
                    sql += "AND BM.CREATE_TIME BETWEEN to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') AND to_date('" + Option["EndDate"] + "23:59:59','yyyy/mm/dd hh24:mi:ss')";
                    sql += "AND (ix.discharge_date +19110000 > " + date_ROC + " OR ix.discharge_date IS NULL OR ix.discharge_date = 00000000) \n";//出院當天不算
                    sql += "AND TRIM(PT.BED_NO) IN(' '";
                    foreach (BedItem bedNo in BedList)
                        sql += ",'" + bedNo.BedNo.Trim() + "' ";
                    sql += ")) \n";
                    sql += "SELECT * FROM COMBIN \n";

                    //DataTable bundle_tube_test = ass_m.DBExecSQL(sql);
                    if (op1)
                    {
                        //string sql = "WITH BUNDLE_TUBE AS ( ";
                        //sql += bundle_tube_sql + ") \n";
                        //sql += "SELECT * FROM BUNDLE_TUBE \n";
                        sql += "PIVOT(MAX(ITEM_VALUE) FOR ITEM_ID IN('bundle_uti_reason','bundle_uti_reason_other', 'bundle_uti_hand', 'bundle_uti_position','bundle_uti_position_reason', 'bundle_uti_tube', 'bundle_uti_bag', 'bundle_uti_clean_condition', 'bundle_uti_clean_method', 'bundle_uti_clean_method_other'))";
                        DataTable bundle_tube = ass_m.DBExecSQL(sql);

                        if (bundle_tube.Rows.Count > 0)
                        {
                            for (int i = 0; i < bundle_tube.Rows.Count; i++)
                            {
                                string feeno = bundle_tube.Rows[i]["FEENO"].ToString();
                                byte[] listByteCodePt = webService.GetPatientInfo(feeno);
                                string listJsonArrayPt = CompressTool.DecompressString(listByteCodePt);
                                PatientInfo pt_info = JsonConvert.DeserializeObject<PatientInfo>(listJsonArrayPt);
                                string ptcost = pt_info.CostCenterName.ToString();
                                string ptbed = pt_info.BedNo.ToString();

                                temp = new Dictionary<string, string>();
                                temp["SN"] = (i + 1).ToString();
                                temp["UNIT"] = ptcost;
                                temp["BEDNO"] = ptbed;
                                temp["RECORDTIME"] = bundle_tube.Rows[i]["CREATE_TIME"].ToString();
                                temp["PATNAME"] = pt_info.PatientName;
                                temp["CHARTNO"] = pt_info.ChartNo;
                                temp["KINDNAME"] = bundle_tube.Rows[i]["KINDNAME"].ToString();
                                temp["STARTTIME"] = bundle_tube.Rows[i]["STARTTIME"].ToString();
                                temp["ENDTIME"] = bundle_tube.Rows[i]["ENDTIME"].ToString();

                                string reason = bundle_tube.Rows[i]["'bundle_uti_reason'"].ToString();
                                string reason_other = bundle_tube.Rows[i]["'bundle_uti_reason_other'"].ToString();
                                reason = reason.Replace("其他", reason_other);
                                reason = reason.Replace(" ", ", ");
                                temp["bundle_uti_reason"] = reason;

                                temp["bundle_uti_hand"] = bundle_tube.Rows[i]["'bundle_uti_hand'"].ToString();
                                temp["bundle_uti_position"] = bundle_tube.Rows[i]["'bundle_uti_position'"].ToString();
                                if (bundle_tube.Rows[i]["'bundle_uti_position_reason'"].ToString() != "")
                                {
                                    temp["bundle_uti_position_reason"] = bundle_tube.Rows[i]["'bundle_uti_position_reason'"].ToString();
                                }
                                else
                                {
                                    temp["bundle_uti_position_reason"] = "NA";
                                }
                                temp["bundle_uti_tube"] = bundle_tube.Rows[i]["'bundle_uti_tube'"].ToString();
                                temp["bundle_uti_bag"] = bundle_tube.Rows[i]["'bundle_uti_bag'"].ToString();
                                temp["bundle_uti_clean_condition"] = bundle_tube.Rows[i]["'bundle_uti_clean_condition'"].ToString();

                                string clean_method = bundle_tube.Rows[i]["'bundle_uti_clean_method'"].ToString();
                                string clean_method_other = bundle_tube.Rows[i]["'bundle_uti_clean_method_other"].ToString();
                                clean_method = clean_method.Replace("其他", clean_method_other);
                                temp["bundle_uti_clean_method"] = clean_method;
                                float sum_col = 0;
                                string[] count_col = new string[] { "bundle_uti_hand", "bundle_uti_position", "bundle_uti_tube", "bundle_uti_bag", "bundle_uti_clean_condition", "bundle_uti_clean_method" };
                                foreach (string col in count_col)
                                {
                                    if (temp[col] != "" && temp[col] != "NA")
                                    {
                                        sum_col += 1;
                                    }
                                }
                                temp["assess_rate"] = ((sum_col / 6) * 100).ToString("#0.00") + "%";

                                dt_.Add(temp);
                            }
                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "";
                            dt_.Add(temp);
                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "定義說明：(明細表要呈現說明內)";
                            dt_.Add(temp);
                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "1.UTI Bundle care 導管類別：(依bundle_care維護註記呈現)";
                            dt_.Add(temp);
                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "2.導管拔除日期，排除呈現及計算 ";
                            dt_.Add(temp);
                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "3.評估完整率：(分子：以1.2-1.3~6.完成勾選項目數)/(分母：以1.2-1.3~6.應勾選項目數)*100%";
                            dt_.Add(temp);
                        }
                        List<string> Title = new List<string> { "序號", "單位", "床號", "評估日期", "病人姓名", "病歷號", "導管種類", "導管置入日期", "導管拔除日期",
                        "導管需要留存原因", "1.執行導尿管照護前、後進行手部衛生", "2-1.導尿管固定位置", "2-2.男性無法放置下腹部原因",
                        "3.密閉、無菌且通暢的引流系統，避免管路扭曲或壓折", "4.集尿袋不可超過8分滿", "5.尿道口清潔", "6.尿道口清潔方式", "評估完整率"};
                        response(Title, Search_Title, dt_, "UTI_Bundle_care每日照護明細表(分子)");

                    }
                    else if (op2)
                    {
                        //string sql = "WITH BUNDLE_TUBE AS ( ";
                        //sql += bundle_tube_sql + ") \n";
                        //sql += "SELECT * FROM BUNDLE_TUBE \n";
                        sql += "PIVOT(MAX(ITEM_VALUE) FOR ITEM_ID IN('bundle_vap_reason','bundle_vap_reason_other', 'bundle_vap_mouth', 'bundle_vap_bed_height','bundle_vap_bed_height_none', 'bundle_vap_bed_height_other', 'bundle_vap_effusion'))";
                        DataTable bundle_tube = ass_m.DBExecSQL(sql);

                        if (bundle_tube.Rows.Count > 0)
                        {
                            for (int i = 0; i < bundle_tube.Rows.Count; i++)
                            {
                                string feeno = bundle_tube.Rows[i]["FEENO"].ToString();
                                byte[] listByteCodePt = webService.GetPatientInfo(feeno);
                                string listJsonArrayPt = CompressTool.DecompressString(listByteCodePt);
                                PatientInfo pt_info = JsonConvert.DeserializeObject<PatientInfo>(listJsonArrayPt);
                                string ptcost = pt_info.CostCenterName.ToString();
                                string ptbed = pt_info.BedNo.ToString();

                                temp = new Dictionary<string, string>();
                                temp["SN"] = (i + 1).ToString();
                                temp["UNIT"] = ptcost;
                                temp["BEDNO"] = ptbed;
                                temp["RECORDTIME"] = bundle_tube.Rows[i]["RECORDTIME"].ToString();
                                temp["PATNAME"] = pt_info.PatientName;
                                temp["CHARTNO"] = pt_info.ChartNo;
                                temp["KINDNAME"] = bundle_tube.Rows[i]["KINDNAME"].ToString();
                                temp["STARTTIME"] = bundle_tube.Rows[i]["STARTTIME"].ToString();
                                temp["ENDTIME"] = bundle_tube.Rows[i]["ENDTIME"].ToString();

                                string reason = bundle_tube.Rows[i]["'bundle_vap_reason'"].ToString();
                                string reason_other = bundle_tube.Rows[i]["'bundle_vap_reason_other'"].ToString();
                                reason = reason.Replace("其他", reason_other);
                                reason = reason.Replace(" ", ", ");
                                temp["bundle_vap_reason"] = reason;

                                temp["bundle_vap_mouth"] = bundle_tube.Rows[i]["'bundle_vap_mouth'"].ToString();
                                temp["bundle_vap_bed_height"] = bundle_tube.Rows[i]["'bundle_vap_bed_height'"].ToString();

                                string bed_height_none = bundle_tube.Rows[i]["'bundle_vap_bed_height_none'"].ToString();
                                string bed_height_other = bundle_tube.Rows[i]["'bundle_vap_bed_height_other'"].ToString();
                                bed_height_none = bed_height_none.Replace("其他", bed_height_other);
                                temp["bundle_vap_bed_height_none"] = bed_height_none;
                                if (bed_height_none == "")
                                {
                                    temp["bundle_vap_bed_height_none"] = "NA";
                                }

                                temp["bundle_vap_effusion"] = bundle_tube.Rows[i]["'bundle_vap_effusion'"].ToString();

                                float sum_col = 0;
                                string[] count_col = new string[] { "bundle_vap_mouth", "bundle_vap_bed_height", "bundle_vap_effusion" };
                                foreach (string col in count_col)
                                {
                                    if (temp[col] != "" && temp[col] != "NA")
                                    {
                                        sum_col += 1;
                                    }
                                }
                                temp["assess_rate"] = ((sum_col / 3) * 100).ToString("#0.00") + "%";

                                dt_.Add(temp);
                            }
                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "";
                            dt_.Add(temp);
                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "定義說明：(明細表要呈現說明內)";
                            dt_.Add(temp);
                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "1.VAP Bundle care 導管類別：(依bundle_care維護註記呈現)";
                            dt_.Add(temp);
                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "2.導管拔除日期，排除呈現及計算 ";
                            dt_.Add(temp);
                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "3.評估完整率：(分子：以1.2-1.3.完成勾選項目數)/(分母：以1.2-1.3.應勾選項目數)*100%";
                            dt_.Add(temp);
                        }
                        List<string> Title = new List<string> { "序號", "單位", "床號", "評估日期", "病人姓名", "病歷號", "導管種類", "導管置入日期", "導管拔除日期",
                        "導管需要留存原因", "1.口腔照護", "2-1.床頭抬高", "2-2未床頭抬高原因", "3.排空積水", "評估完整率"};
                        response(Title, Search_Title, dt_, "VAP_Bundle_care每日照護明細表(分子)");
                    }
                    else if (op3)
                    {
                        //string sql = "WITH BUNDLE_TUBE AS ( ";
                        //sql += bundle_tube_sql + ") \n";
                        //sql += "SELECT * FROM BUNDLE_TUBE \n";
                        sql += "PIVOT(MAX(ITEM_VALUE) FOR ITEM_ID IN('bundle_bsi_hand','bundle_bsi_copied_type', 'bundle_bsi_date_start_date', 'bundle_bsi_date_end_date','bundle_bsi_position', 'bundle_bsi_skin_disinfect'))";
                        DataTable bundle_tube = ass_m.DBExecSQL(sql);

                        if (bundle_tube.Rows.Count > 0)
                        {
                            for (int i = 0; i < bundle_tube.Rows.Count; i++)
                            {
                                string feeno = bundle_tube.Rows[i]["FEENO"].ToString();
                                byte[] listByteCodePt = webService.GetPatientInfo(feeno);
                                string listJsonArrayPt = CompressTool.DecompressString(listByteCodePt);
                                PatientInfo pt_info = JsonConvert.DeserializeObject<PatientInfo>(listJsonArrayPt);
                                string ptcost = pt_info.CostCenterName.ToString();
                                string ptbed = pt_info.BedNo.ToString();

                                temp = new Dictionary<string, string>();
                                temp["SN"] = (i + 1).ToString();
                                temp["UNIT"] = ptcost;
                                temp["BEDNO"] = ptbed;
                                temp["RECORDTIME"] = bundle_tube.Rows[i]["RECORDTIME"].ToString();
                                temp["PATNAME"] = pt_info.PatientName;
                                temp["CHARTNO"] = pt_info.ChartNo;
                                temp["KINDNAME"] = bundle_tube.Rows[i]["KINDNAME"].ToString();
                                temp["STARTTIME"] = bundle_tube.Rows[i]["STARTTIME"].ToString();
                                temp["ENDTIME"] = bundle_tube.Rows[i]["ENDTIME"].ToString();
                                temp["bundle_bsi_reason"] = "";

                                temp["bundle_bsi_hand"] = bundle_tube.Rows[i]["'bundle_bsi_hand'"].ToString();
                                temp["bundle_bsi_copied_type"] = bundle_tube.Rows[i]["'bundle_bsi_copied_type'"].ToString();
                                temp["bundle_bsi_date"] = bundle_tube.Rows[i]["'bundle_bsi_date_start_date'"].ToString() + " - " + bundle_tube.Rows[i]["'bundle_bsi_date_end_date'"].ToString();
                                temp["bundle_bsi_position"] = bundle_tube.Rows[i]["'bundle_bsi_position'"].ToString();
                                temp["bundle_bsi_skin_disinfect"] = bundle_tube.Rows[i]["'bundle_bsi_skin_disinfect'"].ToString();

                                float sum_col = 0;
                                string[] count_col = new string[] { "bundle_bsi_hand", "bundle_bsi_copied_type", "bundle_bsi_date", "bundle_bsi_position", "bundle_bsi_skin_disinfect" };
                                foreach (string col in count_col)
                                {
                                    if (temp[col] != "" && temp[col] != "NA")
                                    {
                                        sum_col += 1;
                                    }
                                }
                                temp["assess_rate"] = ((sum_col / 5) * 100).ToString("#0.00") + "%";

                                dt_.Add(temp);
                            }
                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "";
                            dt_.Add(temp);
                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "定義說明：(明細表要呈現說明內)";
                            dt_.Add(temp);
                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "1.BSI Bundle care 導管類別：(依bundle_care維護註記呈現)";
                            dt_.Add(temp);
                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "2.導管拔除日期，排除呈現及計算 ";
                            dt_.Add(temp);
                            temp = new Dictionary<string, string>();
                            temp["NUM"] = "3.評估完整率：(分子：以1.2-1.3~4.完成勾選項目數)/(分母：以1.2-1.3~4.應勾選項目數)*100%";
                            dt_.Add(temp);
                        }
                        List<string> Title = new List<string> { "序號", "單位", "床號", "評估日期", "病人姓名", "病歷號", "導管種類", "導管置入日期", "導管拔除日期",
                        "評估中心導管需要留存原因", "1.照護中心導管前確實執行手部衛生", "2-1敷料種類", "2-2敷料有效日期", "3.評估中心導管置放部位無紅、腫、熱、痛等情形", "4.更換敷料前消毒皮膚方式","評估完整率"};
                        response(Title, Search_Title, dt_, "BSI_Bundle_care每日照護明細表(分子)");
                    }
                }
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                return new EmptyResult();
            }
        }
        public class CostCodeObj
        {
            public string CostCode { set; get; }
            public string CostName { set; get; }

        }
        [HttpPost]
        public ActionResult BundleCareStatistics_old(string OptionList) //BundleCare統計表(舊版)
        {
            Dictionary<string, string> Option = JsonConvert.DeserializeObject<Dictionary<string, string>>(OptionList);
            List<string> CostName = JsonConvert.DeserializeObject<List<string>>(Option["CostName"]);
            List<UserInfo> CostCodeUserList = new List<UserInfo>();
            List<Dictionary<string, string>> dt_ = new List<Dictionary<string, string>>();
            List<Dictionary<string, string>> Search_Title = new List<Dictionary<string, string>>();
            Dictionary<string, string> temp = null;
            CareRecord care_record_m = new CareRecord();
            List<DBItem> insertDataList = new List<DBItem>();
            string where = "";
            int erow = 0;
            temp = new Dictionary<string, string>();
            temp["Name"] = "搜尋區間";
            temp["Value"] = Option["StartDate"] + "-" + Option["EndDate"];
            Search_Title.Add(temp);
            temp = new Dictionary<string, string>();
            temp["Name"] = "單位";
            temp["Value"] = string.Join("、", JsonConvert.DeserializeObject<List<string>>(Option["CostName"]).ToArray());
            Search_Title.Add(temp);
            bool op1 = Option["BundleCare"].Contains("UTIBundleCareStatistics"), op2 = Option["BundleCare"].Contains("VAPBundleCareStatistics"),
                op3 = Option["BundleCare"].Contains("CVCBundleCareStatistics");
            DateTime startOption = DateTime.Parse(Option["StartDate"].ToString());
            var startOP = startOption.ToString("yyyyMMdd");
            try
            {
                string sql = "SELECT TUBE.*, IX.DISCHARGE_DATE FROM (SELECT * FROM TUBE ";
                if (op1)
                {
                    sql += "WHERE TYPEID IN ('TK00000001','TK00000002') \n";
                }
                else if (op2)
                {
                    sql += "WHERE TYPEID IN ('TK00000023','TK00000028') \n";
                }
                else if (op3)
                {
                    sql += "WHERE TYPEID IN ('TK00000031','TK00000002','TK00000032','TK00000034','TK00000040','TK00000042','TK00000043','TK00000026','TK00000022','TK00000021','TK00000027') \n";
                }
                sql += " AND DELETED IS NULL \n";
                sql += " AND (ENDTIME BETWEEN to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') AND to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') OR ENDTIME IS NULL) ) TUBE \n";
                sql += " LEFT JOIN IPD.IPDINDEX IX  ON TUBE.FEENO = IX.ADMIT_NO ";
                sql += " WHERE ix.discharge_date +19110000 > " + startOP + " OR ix.discharge_date IS NULL OR ix.discharge_date = 00000000";

                DataTable tube_bundle = ass_m.DBExecSQL(sql);

                if (tube_bundle.Rows.Count > 0)
                {
                    for (int i = 0; i < tube_bundle.Rows.Count; i++)
                    {
                        var hasEnd = false;
                        var ptcost = "";
                        var emp = tube_bundle.Rows[i]["CREATNO"].ToString();
                        var row = tube_bundle.Rows[i]["TUBEROW"].ToString();
                        var id = tube_bundle.Rows[i]["TUBEID"].ToString();
                        var feeno = tube_bundle.Rows[i]["FEENO"].ToString();
                        var start = tube_bundle.Rows[i]["STARTTIME"].ToString();
                        var end = tube_bundle.Rows[i]["ENDTIME"].ToString();
                        var outDate = tube_bundle.Rows[i]["DISCHARGE_DATE"].ToString();

                        PatientInfo pt_info = new PatientInfo();
                        byte[] listByteCodePt = webService.GetPatientInfo(feeno);
                        if (listByteCodePt != null)
                        {
                            string listJsonArrayPt = CompressTool.DecompressString(listByteCodePt);
                            pt_info = JsonConvert.DeserializeObject<PatientInfo>(listJsonArrayPt);
                            ptcost = pt_info.CostCenterName.ToString();
                            outDate = pt_info.OutDate.ToString("yyyyMMdd");
                        }
                        if (start != "")
                        {
                            DateTime startTRAN = DateTime.Parse(start);
                            startOption = DateTime.Parse(Option["StartDate"].ToString());
                            if (startTRAN < startOption)
                            {
                                start = Option["StartDate"].ToString();
                            }
                            else
                            {
                                start = startTRAN.ToString("yyyy/MM/dd 00:00:00");
                            }
                        }
                        else
                        {
                            start = Option["StartDate"].ToString();
                        }
                        if (end == "")
                        {
                            startOption = DateTime.Parse(Option["StartDate"].ToString());
                            DateTime outTRAN = new DateTime();

                            if (outDate != "" && outDate != "0000000" && outDate != "00010101")
                            {
                                outTRAN = DateTime.ParseExact(outDate, "yyyyMMdd", System.Globalization.CultureInfo.CurrentCulture);
                                if (outTRAN < startOption)
                                {
                                    continue;
                                }
                                else
                                {
                                    end = outTRAN.ToString("yyyy/MM/dd 00:00:00");
                                    hasEnd = true;

                                }
                            }
                            else
                            {
                                end = DateTime.Now.ToString("yyyy/MM/dd 00:00:00");
                            }
                        }
                        else
                        {
                            DateTime endTRAN = DateTime.Parse(end);
                            end = endTRAN.ToString("yyyy/MM/dd 00:00:00");
                            hasEnd = true;
                        }

                        if (start != "" && end != "")
                        {
                            DateTime startDate = DateTime.Parse(start);
                            DateTime endDate = DateTime.Parse(end);

                            int day = endDate.Subtract(DateTime.Now).Days;

                            if (day > 0)
                            {
                                endDate = DateTime.Now;
                            }

                            day = endDate.Subtract(startDate).Days;
                            if (hasEnd == false)
                            {
                                day = day + 1;
                            }
                            insertDataList = new List<DBItem>();
                            insertDataList.Clear();
                            insertDataList.Add(new DBItem("STATION", ptcost.ToString(), DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("TIMECOUNT", day.ToString(), DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("BUNDLECOUNT", "0", DBItem.DBDataType.String));
                            where = " TUBEROW  = '" + row + "' AND TUBEID = '" + id + "'";
                            erow = care_record_m.DBExecUpdate("TUBE", insertDataList, where);
                        }
                    }
                    var type = "";
                    if (op1)
                    {
                        type = "UTI";
                    }
                    else if (op2)
                    {
                        type = "VAP";
                    }
                    else if (op3)
                    {
                        type = "BSI";
                    }
                    string bundleSql = "SELECT * FROM NIS_TUBE_ASSESS_BUNDLE_MASTER WHERE BC_TYPE = '" + type + "' ";
                    bundleSql += "AND CREATE_TIME BETWEEN to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') AND to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') \n";
                    DataTable bundleData = ass_m.DBExecSQL(bundleSql);
                    if (bundleData.Rows.Count > 0)
                    {
                        for (int b = 0; b < bundleData.Rows.Count; b++)
                        {
                            var daycount = "";
                            int sumtime = 0;
                            var row = bundleData.Rows[b]["TUBEROW"].ToString();
                            var id = bundleData.Rows[b]["TUBEID"].ToString();
                            var tbid = bundleData.Rows[b]["TABLE_ID"].ToString();
                            var recordTime = bundleData.Rows[b]["RECORDTIME"].ToString();
                            DateTime rcDate = DateTime.Parse(recordTime);
                            var recordTimedS = rcDate.ToString("yyyy/MM/dd");
                            string checkone = "SELECT * FROM NIS_TUBE_ASSESS_BUNDLE_MASTER WHERE TUBEROW = '" + row + "' AND COUNTTIME IS NOT NULL AND RECORDTIME BETWEEN to_date('" + recordTimedS + " 00:00:00" + "','yyyy/mm/dd hh24:mi:ss') AND to_date('" + recordTimedS + " 23:59:59" + "','yyyy/mm/dd hh24:mi:ss')";
                            DataTable bundleCheck = ass_m.DBExecSQL(checkone);
                            var emp = bundleData.Rows[b]["CREATE_ID"].ToString();

                            if (bundleCheck.Rows.Count > 0)
                            {
                                insertDataList.Clear();
                                where = " TUBEROW  = '" + row + "' AND TUBEID = '" + id + "'";
                                erow = care_record_m.DBExecUpdate("TUBE", insertDataList, where);
                            }
                            else
                            {
                                string timeselect = "SELECT * FROM TUBE WHERE TUBEROW = '" + row + "' AND TUBEID = '" + id + "'";
                                DataTable bundleTimeData = ass_m.DBExecSQL(timeselect);
                                if (bundleTimeData.Rows.Count > 0)
                                {
                                    daycount = bundleTimeData.Rows[0]["BUNDLECOUNT"].ToString();
                                }
                                if (daycount != "")
                                {
                                    sumtime = int.Parse(daycount) + 1;
                                }
                                insertDataList.Clear();
                                insertDataList.Add(new DBItem("BUNDLECOUNT", sumtime.ToString(), DBItem.DBDataType.String));
                                where = " TUBEROW  = '" + row + "' AND TUBEID = '" + id + "'";
                                erow = care_record_m.DBExecUpdate("TUBE", insertDataList, where);

                                insertDataList.Clear();
                                insertDataList.Add(new DBItem("COUNTTIME", "Y", DBItem.DBDataType.String));
                                where = " TUBEROW  = '" + row + "' AND TABLE_ID = '" + tbid + "'";
                                erow = care_record_m.DBExecUpdate("NIS_TUBE_ASSESS_BUNDLE_MASTER", insertDataList, where);
                            }
                        }
                        insertDataList.Clear();
                        insertDataList.Add(new DBItem("COUNTTIME", "", DBItem.DBDataType.String));
                        where = "DELETE_ID IS NULL";
                        erow = care_record_m.DBExecUpdate("NIS_TUBE_ASSESS_BUNDLE_MASTER", insertDataList, where);
                    }
                }
                if (CostName.Count > 0)
                {
                    float SUM_BUNDLE = 0;
                    float SUM_TUBE = 0;
                    for (int j = 0; j < CostName.Count; j++)
                    {
                        string csql = "SELECT * FROM TUBE WHERE STATION = '" + CostName[j] + "' AND (ENDTIME BETWEEN to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') AND to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') OR ENDTIME IS NULL)";
                        if (op1)
                        {
                            csql += " AND TYPEID IN ('TK00000001','TK00000002') \n";
                        }
                        else if (op2)
                        {
                            csql += " AND TYPEID IN ('TK00000023','TK00000028') \n";
                        }
                        else if (op3)
                        {
                            csql += " AND  TYPEID IN ('TK00000031','TK00000002','TK00000032','TK00000034','TK00000040','TK00000042','TK00000043','TK00000026','TK00000022','TK00000021','TK00000027') \n";
                        }
                        csql += " AND DELETED IS NULL \n";
                        DataTable bundlesta = ass_m.DBExecSQL(csql);
                        if (bundlesta.Rows.Count > 0)
                        {
                            var sumtube = 0;
                            var sum_bundle = 0;

                            for (int tube = 0; tube < bundlesta.Rows.Count; tube++)
                            {
                                sumtube = sumtube + int.Parse(bundlesta.Rows[tube]["TIMECOUNT"].ToString());
                                sum_bundle = sum_bundle + int.Parse(bundlesta.Rows[tube]["BUNDLECOUNT"].ToString());
                            }
                            float BUNDLE = 0;
                            float TUBE = 0;
                            string[] col_array = new string[] { "SUM_BUNDLE", "SUM_TUBE" };
                            temp = new Dictionary<string, string>();
                            temp["SN"] = (j + 1).ToString();
                            temp["UNIT"] = CostName[j].ToString();
                            temp["SUM_BUNDLE"] = sum_bundle.ToString();
                            temp["SUM_TUBE"] = sumtube.ToString();
                            if (temp["SUM_BUNDLE"] == "" || temp["SUM_BUNDLE"] == null)
                            {
                                temp["RATE"] = "0%";
                            }
                            else
                            {
                                TUBE = float.Parse(sumtube.ToString());
                                BUNDLE = float.Parse(sum_bundle.ToString());
                                float rate = 0;
                                if (TUBE != 0)
                                {
                                    rate = (BUNDLE / TUBE);
                                }
                                else
                                {
                                    rate = 0;
                                }
                                temp["RATE"] = (rate * 100).ToString("#0.00") + "%";
                            }
                            foreach (string col in col_array)
                            {
                                if (temp[col] == "" || temp[col] == null)
                                {
                                    temp[col] = "0";
                                }
                            }
                            SUM_BUNDLE += float.Parse(temp["SUM_BUNDLE"]);
                            SUM_TUBE += float.Parse(temp["SUM_TUBE"]);
                            dt_.Add(temp);
                        }
                        else
                        {
                            temp = new Dictionary<string, string>();
                            temp["SN"] = (j + 1).ToString();
                            temp["UNIT"] = CostName[j].ToString();
                            temp["SUM_BUNDLE"] = "0";
                            temp["SUM_TUBE"] = "0";
                            temp["RATE"] = "0.00%";
                            dt_.Add(temp);
                        }
                    }
                    temp = new Dictionary<string, string>();
                    temp["SN"] = "";
                    temp["UNIT"] = "總計";
                    temp["SUM_BUNDLE"] = SUM_BUNDLE.ToString();
                    temp["SUM_TUBE"] = SUM_TUBE.ToString();
                    if (SUM_TUBE != 0)
                    {
                        temp["RATE"] = ((SUM_BUNDLE / SUM_TUBE) * 100).ToString("#0.00") + "%";
                    }
                    else
                    {
                        temp["RATE"] = "0.00%";
                    }
                    dt_.Add(temp);
                }
                if (op1)
                {
                    temp = new Dictionary<string, string>();
                    temp["NUM"] = "";
                    dt_.Add(temp);
                    temp = new Dictionary<string, string>();
                    temp["NUM"] = "定義說明：(統計表要呈現說明內)";
                    dt_.Add(temp);
                    temp = new Dictionary<string, string>();
                    temp["NUM"] = "1.UTI Bundle care每日照護完成比率：(分子：UTI Bundle care每日照護完成人次)/(分母：UTI Bundle care 每日應照護人次)*100%";
                    dt_.Add(temp);
                    List<string> Title = new List<string> { "序號", "單位", "UTI Bundle care每日照護完成人次", "UTI Bundle care 每日應照護人次", "UTI Bundle care每日照護完成比率(%)" };
                    response(Title, Search_Title, dt_, "UTI_Bundle_care每日照護統計表");
                }
                else if (op2)
                {
                    temp = new Dictionary<string, string>();
                    temp["NUM"] = "";
                    dt_.Add(temp);
                    temp = new Dictionary<string, string>();
                    temp["NUM"] = "定義說明：(統計表要呈現說明內)";
                    dt_.Add(temp);
                    temp = new Dictionary<string, string>();
                    temp["NUM"] = "1.VAP Bundle care每日照護完成比率：(分子：VAP Bundle care每日照護完成人次)/(分母：VAP Bundle care 每日應照護人次)*100%";
                    dt_.Add(temp);
                    List<string> Title = new List<string> { "序號", "單位", "VAP Bundle care每日照護完成人次", "VAP Bundle care 每日應照護人次", "VAP Bundle care每日照護完成比率(%)" };
                    response(Title, Search_Title, dt_, "VAP_Bundle_care每日照護統計表");
                }
                else if (op3)
                {
                    temp = new Dictionary<string, string>();
                    temp["NUM"] = "";
                    dt_.Add(temp);
                    temp = new Dictionary<string, string>();
                    temp["NUM"] = "定義說明：(統計表要呈現說明內)";
                    dt_.Add(temp);
                    temp = new Dictionary<string, string>();
                    temp["NUM"] = "1.BSI Bundle care每日照護完成比率：(分子：BSI Bundle care每日照護完成人次)/(分母：BSI Bundle care 每日應照護人次)*100%";
                    dt_.Add(temp);
                    List<string> Title = new List<string> { "序號", "單位", "BSI Bundle care每日照護完成人次", "BSI Bundle care 每日應照護人次", "BSI Bundle care每日照護完成比率(%)" };
                    response(Title, Search_Title, dt_, "BSI_Bundle_care每日照護統計表");
                }
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                return new EmptyResult();
            }
        }

        public class CostCodeStatistics
        {
            public string CostCode { get; set; }
            public string CostName { set; get; }
            public string SUM_BUNDLE { set; get; }
            public string SUM_TUBE { set; get; }
            public string RATE { set; get; }
        }

        [HttpPost]
        public ActionResult BundleCareStatistics(string OptionList) //優化
        {
            Dictionary<string, string> Option = JsonConvert.DeserializeObject<Dictionary<string, string>>(OptionList);
            List<string> CostName = JsonConvert.DeserializeObject<List<string>>(Option["CostName"]);
            List<string> CostCode = JsonConvert.DeserializeObject<List<string>>(Option["CostCode"]);
            List<UserInfo> CostCodeUserList = new List<UserInfo>();
            List<BedItem> BedList = new List<BedItem>();
            List<string> BedNoList = new List<string>();
            List<Dictionary<string, string>> dt_ = new List<Dictionary<string, string>>();
            List<Dictionary<string, string>> Search_Title = new List<Dictionary<string, string>>();
            Dictionary<string, string> temp = new Dictionary<string, string>();
            byte[] TempByte = null;
            List<CostCodeStatistics> CostObjList = new List<CostCodeStatistics>();
            temp["Name"] = "搜尋區間";
            temp["Value"] = Option["StartDate"] + "-" + Option["EndDate"];
            Search_Title.Add(temp);
            temp = new Dictionary<string, string>();
            temp["Name"] = "單位";
            temp["Value"] = string.Join("、", JsonConvert.DeserializeObject<List<string>>(Option["CostName"]).ToArray());
            Search_Title.Add(temp);
            bool op1 = Option["BundleCare"].Contains("UTIBundleCareStatistics"), op2 = Option["BundleCare"].Contains("VAPBundleCareStatistics"),
                op3 = Option["BundleCare"].Contains("CVCBundleCareStatistics");
            DateTime startOption = DateTime.Parse(Option["StartDate"].ToString());
            DateTime endOption = DateTime.Parse(Option["EndDate"].ToString());

            List<string> dateList = new List<string>();
            for (DateTime i = startOption; i <= endOption; i = i.AddDays(1))
            {
                dateList.Add(i.ToString("yyyy/MM/dd"));
            }
            try
            {
                string date_ROC = startOption.ToString("yyyyMMdd");
                var serial = 0;
                var CostCode_Now = "";
                var CostName_Now = "";
                foreach (string CostCode_ in CostCode)
                {
                    BedList = new List<BedItem>();
                    //TempByte = webService.GetUserList(CostCode_);
                    TempByte = webService.GetBedList(CostCode_);
                    if (TempByte != null)
                    {
                        BedList.AddRange(JsonConvert.DeserializeObject<List<BedItem>>(CompressTool.DecompressString(TempByte)));
                    }

                    if (BedList.Count > 0)
                    {
                        CostCode_Now = BedList[0].CostCenterCode.ToString();
                        CostName_Now = BedList[0].CostCenterName.ToString();
                        BedNoList = new List<string>();
                        foreach (var item in BedList)
                        {
                            if (!BedNoList.Contains(item.BedNo.Trim()))
                            {
                                BedNoList.Add(item.BedNo.Trim());
                            }
                        }
                        //處理同一FEENO會有不同床號的問題(取CUT_NO最大那一筆)
                        string ipd_ipdindex_sql = "SELECT * FROM \n";
                        ipd_ipdindex_sql += "(SELECT A.*, row_number() OVER(PARTITION BY admit_no ORDER BY cut_no DESC) AS sn \n";
                        ipd_ipdindex_sql += "FROM ipd.ipdindex A) \n";
                        ipd_ipdindex_sql += "WHERE sn = 1";

                        string cs_v_all_ptipd_sql = "SELECT * FROM (\n";
                        cs_v_all_ptipd_sql += "SELECT PT.ADMIT_NO Fee_No,PT.CHART_NO ChartNo,PT.BED_NO BedNo,ROW_NUMBER() OVER (PARTITION BY PT.ADMIT_NO ORDER BY PT.CUT_NO DESC) sn \n";
                        cs_v_all_ptipd_sql += "FROM CS.V_ALL_PTIPD PT )\n";
                        cs_v_all_ptipd_sql += "WHERE sn = 1";

                        string tube_sql = "SELECT TUBE.*, IX.DISCHARGE_DATE, PT.BED_NO  FROM (SELECT * FROM TUBE \n";
                        if (op1)
                        {
                            tube_sql += "WHERE TYPEID IN ('TK00000001','TK00000002') \n";
                        }
                        else if (op2)
                        {
                            tube_sql += "WHERE TYPEID IN ('TK00000023','TK00000028') \n";
                        }
                        else if (op3)
                        {
                            tube_sql += "WHERE TYPEID IN ('TK00000031','TK00000002','TK00000032','TK00000034','TK00000040','TK00000042','TK00000043','TK00000026','TK00000022','TK00000021','TK00000027') \n";
                        }
                        tube_sql += " AND DELETED IS NULL \n";
                        tube_sql += " AND CREATTIME <= to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') \n";
                        tube_sql += " AND (ENDTIME >= to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') OR ENDTIME IS NULL) ) TUBE \n";
                        tube_sql += " LEFT JOIN (" + ipd_ipdindex_sql + ") IX  ON TUBE.FEENO = IX.ADMIT_NO \n";
                        tube_sql += " LEFT JOIN CS.V_ALL_PTIPD PT  ON IX.ADMIT_NO = PT.ADMIT_NO AND IX.CUT_NO = PT.CUT_NO \n";
                        tube_sql += " WHERE (ix.discharge_date +19110000 > " + date_ROC + " OR ix.discharge_date IS NULL OR ix.discharge_date = 0000000) \n";
                        tube_sql += "AND PT.BED_NO IN (''";
                        foreach (var BedNo in BedNoList)
                            tube_sql += ",'" + BedNo + "'";
                        tube_sql += ")";
                        DataTable tube_dt = ass_m.DBExecSQL(tube_sql);
                        var days_gap = 0;
                        if (tube_dt.Rows.Count > 0)
                        {
                            for (var i = 0; i < tube_dt.Rows.Count; i++)
                            {
                                var startdate = DateTime.Parse(DateTime.Parse(tube_dt.Rows[i]["CREATTIME"].ToString()).ToString("yyyy/MM/dd"));
                                var enddate_str = !string.IsNullOrEmpty(tube_dt.Rows[i]["ENDTIME"].ToString()) ? tube_dt.Rows[i]["ENDTIME"].ToString() : "";
                                var test = tube_dt.Rows[i]["DISCHARGE_DATE"].ToString();
                                var discharge_date_str = (!string.IsNullOrEmpty(tube_dt.Rows[i]["DISCHARGE_DATE"].ToString())) && tube_dt.Rows[i]["DISCHARGE_DATE"].ToString() != "0000000" ? (int.Parse(tube_dt.Rows[i]["DISCHARGE_DATE"].ToString()) + 19110000).ToString() : "";
                                var start = new DateTime();
                                var end = new DateTime();

                                if (startdate >= startOption)
                                {
                                    start = startdate;
                                }
                                else
                                {
                                    start = startOption;
                                }
                                if (enddate_str != "" || discharge_date_str != "")
                                {
                                    var enddate = new DateTime();
                                    var discharge_date = new DateTime();
                                    if (enddate_str != "")
                                    {
                                        enddate = DateTime.Parse(DateTime.Parse(enddate_str).ToString("yyyy/MM/dd"));
                                    }
                                    if (discharge_date_str != "")
                                    {
                                        var year = discharge_date_str.Substring(0, 4);
                                        var mon = discharge_date_str.Substring(4, 2);
                                        var day = discharge_date_str.Substring(6, 2);
                                        discharge_date = DateTime.Parse(year + "/" + mon + "/" + day);
                                    }
                                    if (enddate_str != "" && discharge_date_str != "")
                                    {
                                        if (discharge_date <= enddate)
                                        {
                                            if (discharge_date <= endOption)
                                            {
                                                end = discharge_date.AddDays(-1);
                                            }
                                            else
                                            {
                                                end = endOption;
                                            }
                                        }
                                        else
                                        {
                                            if (enddate <= endOption)
                                            {
                                                end = enddate.AddDays(-1);
                                            }
                                            else
                                            {
                                                end = endOption;
                                            }
                                        }
                                    }
                                    else if (enddate_str != "" && discharge_date_str == "")
                                    {
                                        if (enddate <= endOption)
                                        {
                                            end = enddate.AddDays(-1);
                                        }
                                        else
                                        {
                                            end = endOption;
                                        }
                                    }
                                    else if (enddate_str == "" && discharge_date_str != "")
                                    {
                                        if (discharge_date <= endOption)
                                        {
                                            end = discharge_date.AddDays(-1);
                                        }
                                        else
                                        {
                                            end = endOption;
                                        }
                                    }
                                }
                                else
                                {
                                    end = endOption;
                                }
                                days_gap += ((end - start).Days + 1);
                            }
                        }
                        temp = new Dictionary<string, string>();
                        var type = "";
                        if (op1)
                        {
                            type = "UTI";
                        }
                        else if (op2)
                        {
                            type = "VAP";
                        }
                        else if (op3)
                        {
                            type = "BSI";
                        }
                        string bundleSql = "SELECT SUM(count) SUM FROM( \n";
                        bundleSql += "SELECT COUNT(DISTINCT(TUBEID)) count FROM ( \n";
                        bundleSql += "SELECT TUBE.*,DENSE_RANK() OVER(ORDER BY to_date(to_char(TUBE.CREATE_TIME,'yyyy-MM-dd'),'yyyy-MM-dd')) sn FROM ( \n";
                        bundleSql += "SELECT TUBE.*,PT.BedNo FROM NIS_TUBE_ASSESS_BUNDLE_MASTER TUBE \n";
                        bundleSql += "LEFT JOIN (" + cs_v_all_ptipd_sql + ") PT ON TUBE.FEENO = PT.FEE_NO \n";
                        bundleSql += " LEFT JOIN (" + ipd_ipdindex_sql + ") IX  ON TUBE.FEENO = IX.ADMIT_NO \n";
                        bundleSql += "LEFT JOIN TUBE BT ON TUBE.TUBEID = BT.TUBEID \n";
                        bundleSql += "WHERE BC_TYPE = '" + type + "' \n";
                        bundleSql += "AND to_date(to_char(CREATE_TIME,'yyyy-MM-dd'),'yyyy-MM-dd') < to_date(to_char(BT.ENDTIME,'yyyy-MM-dd'),'yyyy-MM-dd') \n";//拔管當天不算
                        bundleSql += "AND CREATE_TIME BETWEEN to_date('" + Option["StartDate"] + " 00:00:00','yyyy/mm/dd hh24:mi:ss') AND to_date('" + Option["EndDate"] + " 23:59:59','yyyy/mm/dd hh24:mi:ss') \n";
                        bundleSql += "AND (ix.discharge_date +19110000 > " + date_ROC + " OR ix.discharge_date IS NULL OR ix.discharge_date = 00000000) \n";//出院當天不算
                        bundleSql += "AND DELETE_TIME IS NULL \n";
                        bundleSql += "AND PT.BedNo IN (''";
                        foreach (var BedNo in BedNoList)
                            bundleSql += ",'" + BedNo + "'";
                        bundleSql += ") \n";
                        bundleSql += ") TUBE \n";
                        bundleSql += ") GROUP BY sn \n";
                        bundleSql += ")";
                        DataTable bundleData = ass_m.DBExecSQL(bundleSql);
                        if (bundleData.Rows.Count > 0)
                        {
                            var sum_bundle_Now = !string.IsNullOrEmpty(bundleData.Rows[0]["SUM"].ToString()) ? bundleData.Rows[0]["SUM"].ToString() : "0";
                            var sum_tube_Now = days_gap.ToString();
                            List<string> costCodeList = new List<string>();
                            foreach (var cost in CostObjList)
                            {
                                costCodeList.Add(cost.CostCode);
                            }
                            if (costCodeList.Contains(CostCode_Now))
                            {
                                CostObjList.ForEach(data =>
                                {
                                    if (data.CostCode == CostCode_Now)
                                    {
                                        float float_sum_bundle_Now = float.Parse(sum_bundle_Now);
                                        float float_sum_bundle = float.Parse(data.SUM_BUNDLE);
                                        float float_sum_tube_Now = float.Parse(sum_tube_Now);
                                        float float_sum_tube = float.Parse(data.SUM_TUBE);
                                        data.SUM_BUNDLE = (float_sum_bundle + float_sum_bundle_Now).ToString("#0");
                                        data.SUM_TUBE = (float_sum_tube + float_sum_tube_Now).ToString("#0");
                                        if (data.SUM_TUBE != "0")
                                        {
                                            data.RATE = (float.Parse(data.SUM_BUNDLE) / float.Parse(data.SUM_TUBE)).ToString();
                                        }
                                        else
                                        {
                                            data.RATE = "0.00";
                                        }
                                    }
                                });
                            }
                            else
                            {
                                CostCodeStatistics newCost = new CostCodeStatistics();
                                newCost.CostCode = CostCode_Now;
                                newCost.CostName = CostName_Now;
                                newCost.SUM_BUNDLE = sum_bundle_Now;
                                newCost.SUM_TUBE = sum_tube_Now;
                                if (sum_tube_Now != "0")
                                {
                                    newCost.RATE = (float.Parse(sum_bundle_Now) / float.Parse(sum_tube_Now)).ToString();
                                }
                                else
                                {
                                    newCost.RATE = "0.00";
                                }
                                CostObjList.Add(newCost);
                            }
                        }
                    }
                }

                if (CostObjList.Count > 0)
                {
                    var bundle_total = 0;
                    var tube_total = 0;
                    float total_rate = 0;
                    foreach (var data in CostObjList)
                    {
                        serial += 1;
                        temp = new Dictionary<string, string>();
                        temp["SN"] = serial.ToString();
                        temp["UNIT"] = data.CostName;
                        temp["SUM_BUNDLE"] = data.SUM_BUNDLE;
                        temp["SUM_TUBE"] = data.SUM_TUBE;
                        temp["RATE"] = (float.Parse(data.RATE) * 100).ToString("#0.00") + "%";
                        dt_.Add(temp);
                        bundle_total += int.Parse(data.SUM_BUNDLE);
                        tube_total += int.Parse(data.SUM_TUBE);
                    }
                    total_rate = (float)bundle_total / (float)tube_total;
                    temp = new Dictionary<string, string>();
                    temp["SN"] = "";
                    temp["UNIT"] = "總計";
                    temp["SUM_BUNDLE"] = bundle_total.ToString();
                    temp["SUM_TUBE"] = tube_total.ToString();
                    if (tube_total.ToString() != "")
                    {
                        temp["RATE"] = (total_rate * 100).ToString("#0.00") + "%";
                    }
                    else
                    {
                        temp["RATE"] = "0%";
                    }
                    dt_.Add(temp);
                }
                if (op1)
                {
                    temp = new Dictionary<string, string>();
                    temp["NUM"] = "";
                    dt_.Add(temp);
                    temp = new Dictionary<string, string>();
                    temp["NUM"] = "定義說明：(統計表要呈現說明內)";
                    dt_.Add(temp);
                    temp = new Dictionary<string, string>();
                    temp["NUM"] = "1.UTI Bundle care每日照護完成比率：(分子：UTI Bundle care每日照護完成人次)/(分母：UTI Bundle care 每日應照護人次)*100%";
                    dt_.Add(temp);
                    List<string> Title = new List<string> { "序號", "單位", "UTI Bundle care每日照護完成人次", "UTI Bundle care 每日應照護人次", "UTI Bundle care每日照護完成比率(%)" };
                    response(Title, Search_Title, dt_, "UTI_Bundle_care每日照護統計表");
                }
                else if (op2)
                {
                    temp = new Dictionary<string, string>();
                    temp["NUM"] = "";
                    dt_.Add(temp);
                    temp = new Dictionary<string, string>();
                    temp["NUM"] = "定義說明：(統計表要呈現說明內)";
                    dt_.Add(temp);
                    temp = new Dictionary<string, string>();
                    temp["NUM"] = "1.VAP Bundle care每日照護完成比率：(分子：VAP Bundle care每日照護完成人次)/(分母：VAP Bundle care 每日應照護人次)*100%";
                    dt_.Add(temp);
                    List<string> Title = new List<string> { "序號", "單位", "VAP Bundle care每日照護完成人次", "VAP Bundle care 每日應照護人次", "VAP Bundle care每日照護完成比率(%)" };
                    response(Title, Search_Title, dt_, "VAP_Bundle_care每日照護統計表");
                }
                else if (op3)
                {
                    temp = new Dictionary<string, string>();
                    temp["NUM"] = "";
                    dt_.Add(temp);
                    temp = new Dictionary<string, string>();
                    temp["NUM"] = "定義說明：(統計表要呈現說明內)";
                    dt_.Add(temp);
                    temp = new Dictionary<string, string>();
                    temp["NUM"] = "1.BSI Bundle care每日照護完成比率：(分子：BSI Bundle care每日照護完成人次)/(分母：BSI Bundle care 每日應照護人次)*100%";
                    dt_.Add(temp);
                    List<string> Title = new List<string> { "序號", "單位", "BSI Bundle care每日照護完成人次", "BSI Bundle care 每日應照護人次", "BSI Bundle care每日照護完成比率(%)" };
                    response(Title, Search_Title, dt_, "BSI_Bundle_care每日照護統計表");
                }
            }
            catch (Exception ex)
            {

            }
            return new EmptyResult();
        }

        [HttpPost]
        public ActionResult BundleCareNeedBundle(string OptionList) //BundleCare驗證明細(列出應做人數明細)
        {
            Dictionary<string, string> Option = JsonConvert.DeserializeObject<Dictionary<string, string>>(OptionList);
            List<string> CostName = JsonConvert.DeserializeObject<List<string>>(Option["CostName"]);
            List<string> CostCode = JsonConvert.DeserializeObject<List<string>>(Option["CostCode"]);
            List<UserInfo> CostCodeUserList = new List<UserInfo>();
            List<BedItem> BedList = new List<BedItem>();
            List<string> BedNoList = new List<string>();
            List<Dictionary<string, string>> dt_ = new List<Dictionary<string, string>>();
            List<Dictionary<string, string>> Search_Title = new List<Dictionary<string, string>>();
            Dictionary<string, string> temp = new Dictionary<string, string>();
            byte[] TempByte = null;
            List<CostCodeStatistics> CostObjList = new List<CostCodeStatistics>();
            temp["Name"] = "搜尋區間";
            temp["Value"] = Option["StartDate"] + "-" + Option["EndDate"];
            Search_Title.Add(temp);
            temp = new Dictionary<string, string>();
            temp["Name"] = "單位";
            temp["Value"] = string.Join("、", JsonConvert.DeserializeObject<List<string>>(Option["CostName"]).ToArray());
            Search_Title.Add(temp);
            bool op1 = Option["BundleCare_NeedBundle"].Contains("UTIBundleCare_NeedBundleDetail"), op2 = Option["BundleCare_NeedBundle"].Contains("VAPBundleCare_NeedBundleDetail"),
                op3 = Option["BundleCare_NeedBundle"].Contains("CVCBundleCare_NeedBundleDetail");
            DateTime startOption = DateTime.Parse(Option["StartDate"].ToString());
            DateTime endOption = DateTime.Parse(Option["EndDate"].ToString());
            List<string> dateList = new List<string>();
            for (DateTime i = startOption; i <= endOption; i = i.AddDays(1))
            {
                dateList.Add(i.ToString("yyyy/MM/dd"));
            }
            try
            {
                DateTime date_DateTime = new DateTime();
                string date_ROC = "";
                var serial = 0;
                var CostCode_Now = "";
                var CostName_Now = "";
                foreach (string date in dateList)
                {
                    foreach (string CostCode_ in CostCode)
                    {
                        BedList = new List<BedItem>();
                        TempByte = webService.GetBedList(CostCode_);
                        if (TempByte != null)
                        {
                            BedList.AddRange(JsonConvert.DeserializeObject<List<BedItem>>(CompressTool.DecompressString(TempByte)));
                        }

                        if (BedList.Count > 0)
                        {
                            CostCode_Now = BedList[0].CostCenterCode.ToString();
                            CostName_Now = BedList[0].CostCenterName.ToString();
                            BedNoList = new List<string>();
                            foreach (var item in BedList)
                            {
                                if (!BedNoList.Contains(item.BedNo.Trim()))
                                {
                                    BedNoList.Add(item.BedNo.Trim());
                                }
                            }
                            date_DateTime = DateTime.Parse(date);
                            date_ROC = date_DateTime.ToString("yyyyMMdd");
                            //處理同一FEENO會有不同床號的問題(取CUT_NO最大那一筆)
                            string ipd_ipdindex_sql = "SELECT * FROM \n";
                            ipd_ipdindex_sql += "(SELECT A.*, row_number() OVER(PARTITION BY admit_no ORDER BY cut_no DESC) AS sn \n";
                            ipd_ipdindex_sql += "FROM ipd.ipdindex A) \n";
                            ipd_ipdindex_sql += "WHERE sn = 1";

                            string cs_v_all_ptipd_sql = "SELECT * FROM (\n";
                            cs_v_all_ptipd_sql += "SELECT PT.ADMIT_NO Fee_No,PT.CHART_NO ChartNo,PT.BED_NO BedNo,ROW_NUMBER() OVER (PARTITION BY PT.ADMIT_NO ORDER BY PT.CUT_NO DESC) sn \n";
                            cs_v_all_ptipd_sql += "FROM CS.V_ALL_PTIPD PT )\n";
                            cs_v_all_ptipd_sql += "WHERE sn = 1";

                            string tube_sql = "SELECT TUBE.*, TK.KINDNAME, IX.DISCHARGE_DATE, IX.CHART_NO, PT.BED_NO, CT.PT_NAME  FROM (SELECT * FROM TUBE \n";
                            if (op1)
                            {
                                tube_sql += "WHERE TYPEID IN ('TK00000001','TK00000002') \n";
                            }
                            else if (op2)
                            {
                                tube_sql += "WHERE TYPEID IN ('TK00000023','TK00000028') \n";
                            }
                            else if (op3)
                            {
                                tube_sql += "WHERE TYPEID IN ('TK00000031','TK00000002','TK00000032','TK00000034','TK00000040','TK00000042','TK00000043','TK00000026','TK00000022','TK00000021','TK00000027') \n";
                            }
                            tube_sql += " AND DELETED IS NULL \n";
                            tube_sql += " AND CREATTIME <= to_date('" + date + " 23:59:59','yyyy/mm/dd hh24:mi:ss') \n";
                            tube_sql += " AND (ENDTIME > to_date('" + date + " 23:59:59','yyyy/mm/dd hh24:mi:ss') OR ENDTIME IS NULL) ) TUBE \n";
                            tube_sql += " LEFT JOIN (" + ipd_ipdindex_sql + ") IX  ON TUBE.FEENO = IX.ADMIT_NO \n";
                            tube_sql += " LEFT JOIN CS.V_ALL_PTIPD PT  ON IX.ADMIT_NO = PT.ADMIT_NO AND IX.CUT_NO = PT.CUT_NO \n";
                            tube_sql += "LEFT JOIN CHART.CHART CT ON IX.CHART_NO = CT.CHART_NO \n";
                            tube_sql += "LEFT JOIN TUBE_KIND TK ON TUBE.TYPEID = TK.KINDID \n";
                            tube_sql += " WHERE (ix.discharge_date +19110000 > " + date_ROC + " OR ix.discharge_date IS NULL OR ix.discharge_date = 00000000) \n";
                            tube_sql += "AND PT.BED_NO IN (''";
                            foreach (var BedNo in BedNoList)
                                tube_sql += ",'" + BedNo + "'";
                            tube_sql += ") \n";
                            DataTable tube_dt = ass_m.DBExecSQL(tube_sql);

                            if (tube_dt.Rows.Count > 0)
                            {
                                for (var i = 0; i < tube_dt.Rows.Count; i++)
                                {
                                    serial += 1;
                                    temp = new Dictionary<string, string>();
                                    temp["SN"] = serial.ToString();
                                    temp["UNIT"] = CostName_Now;
                                    temp["BEDNO"] = tube_dt.Rows[i]["BED_NO"].ToString();
                                    temp["PATNAME"] = tube_dt.Rows[i]["PT_NAME"].ToString();
                                    temp["CHARTNO"] = tube_dt.Rows[i]["CHART_NO"].ToString();
                                    temp["KINDNAME"] = tube_dt.Rows[i]["KINDNAME"].ToString();
                                    temp["STARTTIME"] = tube_dt.Rows[i]["CREATTIME"].ToString();
                                    temp["ENDTIME"] = tube_dt.Rows[i]["ENDTIME"].ToString();
                                    dt_.Add(temp);
                                }
                            }

                            //var type = "";
                            //if (op1)
                            //{
                            //    type = "UTI";
                            //}
                            //else if (op2)
                            //{
                            //    type = "VAP";
                            //}
                            //else if (op3)
                            //{
                            //    type = "BSI";
                            //}

                            //string bundleSql = "SELECT DISTINCT(TUBEID) FROM ( \n";
                            //bundleSql += "SELECT TUBE.*,PT.BedNo FROM NIS_TUBE_ASSESS_BUNDLE_MASTER TUBE \n";
                            //bundleSql += "LEFT JOIN (" + cs_v_all_ptipd_sql + ") PT ON TUBE.FEENO = PT.FEE_NO \n";
                            //bundleSql += "WHERE BC_TYPE = '" + type + "' ";
                            //bundleSql += "AND CREATE_TIME BETWEEN to_date('" + date + " 00:00:00','yyyy/mm/dd hh24:mi:ss') AND to_date('" + date + " 23:59:59','yyyy/mm/dd hh24:mi:ss') \n";
                            //bundleSql += "AND DELETE_TIME IS NULL \n";
                            //bundleSql += "AND PT.BedNo IN (''";
                            //foreach (var BedNo in BedNoList)
                            //    bundleSql += ",'" + BedNo + "'";
                            //bundleSql += ") \n";
                            //bundleSql += ") \n";
                            //DataTable bundleData = ass_m.DBExecSQL(bundleSql);
                            //if (bundleData.Rows.Count > 0)
                            //{

                            //}
                        }
                    }
                }
                if (op1)
                {
                    List<string> Title = new List<string> { "序號", "單位", "床號", "病人姓名", "病歷號", "導管種類", "導管置入日期", "導管拔除日期" };
                    response(Title, Search_Title, dt_, "UTI應執行BundleCare明細表(分母)");
                }
                else if (op2)
                {
                    List<string> Title = new List<string> { "序號", "單位", "床號", "病人姓名", "病歷號", "導管種類", "導管置入日期", "導管拔除日期" };
                    response(Title, Search_Title, dt_, "VAP應執行BundleCare明細表(分母)");
                }
                else if (op3)
                {
                    List<string> Title = new List<string> { "序號", "單位", "床號", "病人姓名", "病歷號", "導管種類", "導管置入日期", "導管拔除日期" };
                    response(Title, Search_Title, dt_, "BSI應執行BundleCare明細表(分母)");
                }
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                return new EmptyResult();
            }
        }

        /// <summary> 產生excel </summary>
        /// <param name="sql">sql字串</param>
        /// <param name="Search_title">查詢資訊(單位別,區間)</param>
        /// <param name="title">標題列表</param>
        /// <param name="dt">DataTable</param>
        /// <param name="filename">報表名稱</param>
        private void response(List<string> title, List<Dictionary<string, string>> Search_title, List<Dictionary<string, string>> dt, string filename)
        {
            int row = 1;
            XLWorkbook workbook = new XLWorkbook();
            IXLWorksheets sheets = workbook.Worksheets;
            IXLWorksheet sheet = sheets.Add("sheet_name");
            // fill excel
            IXLRange range;
            range = sheet.Range(row, 1, row, title.Count).Merge();
            row++;
            range.Value = filename;
            range.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            IXLCell cell;
            foreach (var item in Search_title)
            {
                cell = sheet.Cell(row, 1);
                cell.Value = item["Name"];

                range = sheet.Range(row, 2, row, title.Count).Merge();
                range.Value = item["Value"];
                row++;
            }
            for (int i = 0; i < title.Count; i++)
            {
                cell = sheet.Cell(row, i + 1);
                cell.Value = title[i];
                cell.Style.Font.FontColor = XLColor.Blue;
            }
            row++;

            foreach (var item in dt)
            {
                int i = 1;
                foreach (var item_ in item)
                {
                    cell = sheet.Cell(row, i);
                    cell.Value = item_.Value;
                    i++;
                }
                row++;
            }

            Response.Clear();
            Response.ClearContent();
            Response.ClearHeaders();
            Response.ContentType = "application/vnd.ms-excel";
            Response.AddHeader("Content-Disposition", "attachment;filename=" + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + HttpUtility.UrlEncode(filename, System.Text.Encoding.UTF8) + ".xls");

            using (var memoryStream = new MemoryStream())
            {
                workbook.SaveAs(memoryStream);
                memoryStream.WriteTo(Response.OutputStream);
                Response.Flush();
                Response.End();
            }
            foreach (IXLWorksheets ws in sheets)
            {
                Marshal.ReleaseComObject(sheet);
                //ws.Dispose();
            }
            Marshal.ReleaseComObject(sheets);
            workbook.Dispose();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        #endregion

        private void response_trans(List<string> title, List<Dictionary<string, string>> Search_title, List<Dictionary<string, string>> dt, string filename)
        {
            int row = 1;
            XLWorkbook workbook = new XLWorkbook();
            IXLWorksheets sheets = workbook.Worksheets;
            IXLWorksheet sheet = sheets.Add("sheet_name");
            // fill excel
            IXLRange range;
            range = sheet.Range(row, 1, row, title.Count).Merge();
            row++;
            range.Value = filename;
            range.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            IXLCell cell;
            foreach (var item in Search_title)
            {
                cell = sheet.Cell(row, 1);
                cell.Value = item["Name"];

                range = sheet.Range(row, 2, row, title.Count).Merge();
                range.Value = item["Value"];
                row++;
            }
            for (int i = 0; i < title.Count; i++)
            {
                cell = sheet.Cell(row, i + 1);
                cell.Value = title[i];
                cell.Style.Font.FontColor = XLColor.Blue;
            }
            row++;

            foreach (var item in dt)
            {
                int i = 1;
                foreach (var item_ in item)
                {
                    cell = sheet.Cell(row, i);
                    cell.Value = item_.Value;
                    i++;
                }
                row++;
            }

            string[] testarray = { "定義說明", "65歲(含)以上衰弱前期比率" };

            row++;
            foreach (var item in testarray)
            {
                int i = 1;
                cell = sheet.Cell(row, 1);
                cell.Value = item;
                i++;
            }

            Response.Clear();
            Response.ClearContent();
            Response.ClearHeaders();
            Response.ContentType = "application/vnd.ms-excel";
            Response.AddHeader("Content-Disposition", "attachment;filename=" + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + HttpUtility.UrlEncode(filename, System.Text.Encoding.UTF8) + ".xls");

            using (var memoryStream = new MemoryStream())
            {
                workbook.SaveAs(memoryStream);
                memoryStream.WriteTo(Response.OutputStream);
                Response.Flush();
                Response.End();
            }
            foreach (IXLWorksheets ws in sheets)
            {
                Marshal.ReleaseComObject(sheet);
                //ws.Dispose();
            }
            Marshal.ReleaseComObject(sheets);
            workbook.Dispose();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        #region 報表範本

        public EmptyResult getReport()
        {
            try
            {
                string sqlstr = "select * from sys_params";
                //開始建立xml檔案
                XMLtoExcelReport xReport = new XMLtoExcelReport();
                xReport.AddNewXml();
                DataTable Dt = link2.DBExecSQL(sqlstr);
                link2.DBExecSQL(sqlstr, ref Dt);

                List<string> title = new List<string> { "序號", "模組名稱", "群組名稱", "名稱", "值", "語系", "排序", "備註" };
                xReport.AddWorkSheet("sheet_name", Dt, null, title);
                String fn = xReport.SaveFile();

                //輸出檔案
                FileInfo fileInfo = new FileInfo(fn);
                Response.Clear();
                Response.ClearContent();
                Response.ClearHeaders();
                Response.AddHeader("Content-Disposition", "attachment;filename=Report" + xReport.outFileExt);
                Response.AddHeader("Content-Length", fileInfo.Length.ToString());
                Response.AddHeader("Content-Transfer-Encoding", "binary");
                Response.ContentType = "application/vnd.ms-excel";
                Response.ContentEncoding = Encoding.UTF8;
                Response.WriteFile(fileInfo.FullName);
                Response.Flush();
                Response.End();
                xReport = null;

            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }
            finally
            {
                this.link2.DBClose();
            }

            return new EmptyResult();
        }

        #endregion

        #region 右方Menu功能維護

        public ActionResult MenuModify()
        {

            byte[] cclistByteCode = webService.GetCostCenterList();
            if (cclistByteCode != null)
            {
                string ccListJsonArr = CompressTool.DecompressString(cclistByteCode);
                List<CostCenterList> ccList = JsonConvert.DeserializeObject<List<CostCenterList>>(ccListJsonArr);
                List<SelectListItem> cList = new List<SelectListItem>();

                cList.Add(new SelectListItem()
                {
                    Text = "全部",
                    Value = "%",
                    Selected = true
                });

                for (int i = 0; i <= ccList.Count - 1; i++)
                {
                    cList.Add(new SelectListItem()
                    {
                        Text = ccList[i].CCCDescription,
                        Value = ccList[i].CostCenterCode,
                        Selected = false
                    });
                }
                ViewData["costlist"] = cList;
            }
            return View();
        }

        public ActionResult CostCenterUserList()
        {
            if (Request["costcenter"] != null)
            {
                byte[] userlistByteCode = webService.GetUserList(Request["costcenter"].ToString().Trim());
                if (userlistByteCode != null)
                {
                    string userlistJsonArr = CompressTool.DecompressString(userlistByteCode);
                    List<UserInfo> userlist = JsonConvert.DeserializeObject<List<UserInfo>>(userlistJsonArr);
                    ViewData["userlist"] = userlist;
                }
            }
            return View();
        }

        /// <summary> 刪除權限 </summary>
        public EmptyResult FunctionDelete()
        {
            try
            {
                string condition = "emp_no='" + Request["del_emp_no"].ToString().Trim() + "'";
                int effRow = this.link.DBExecDelete("sys_user_models", condition);
                switch (effRow)
                {
                    case 0:
                        Response.Write("已無任何權限可以刪除");
                        break;
                    default:
                        Response.Write("刪除成功");
                        break;
                }
            }
            catch (Exception ex)
            {
                Response.Write("刪除失敗，請聯絡系統負責人[" + ex.Message.ToString() + "]");
            }

            return new EmptyResult();
        }

        public EmptyResult FunctionSave()
        {
            try
            {
                string default_page = Request["default_page"].ToString().Trim();
                string[] model_list = Request["model_list"].ToString().Split(',');
                string emp_no = Request["emp_no"].ToString().Trim();

                //先清除原先權限
                this.link.DBExecDelete("sys_user_models", "emp_no='" + emp_no.Trim() + "'");

                int effRow = 0;
                for (int i = 0; i <= model_list.Length - 1; i++)
                {
                    List<DBItem> modIns = new List<DBItem>();
                    modIns.Add(new DBItem("emp_no", emp_no, DBItem.DBDataType.String));
                    modIns.Add(new DBItem("mo_id", model_list[i].Trim(), DBItem.DBDataType.String));
                    if (model_list[i].Trim() == default_page)
                        modIns.Add(new DBItem("set_default_page", "Y", DBItem.DBDataType.String));
                    else
                        modIns.Add(new DBItem("set_default_page", "N", DBItem.DBDataType.String));
                    effRow += this.link.DBExecInsert("sys_user_models", modIns);
                }
                if (effRow == model_list.Length)
                    Response.Write("新增成功");
                else
                    Response.Write("新增失敗，請聯絡系統負責人");

            }
            catch (Exception ex)
            {
                Response.Write("新增失敗，請聯絡系統負責人[" + ex.Message.ToString() + "]");
            }
            finally
            {
                this.link.DBClose();
            }
            return new EmptyResult();
        }

        public ActionResult FunctionList()
        {
            string usercondition = string.Empty;
            List<ModelItem> modellist = new List<ModelItem>();
            try
            {
                string sqlstr = " SELECT  SM.* ,(SELECT SET_DEFAULT_PAGE FROM SYS_USER_MODELS ";
                sqlstr += " WHERE EMP_NO = '" + Request["emp_no"].ToString().Trim() + "' AND MO_ID = SM.MO_ID) as SET_DEFAULT_PAGE, ";
                sqlstr += " CASE WHEN (SELECT TRIM(MO_ID) FROM SYS_USER_MODELS WHERE EMP_NO = '" + Request["emp_no"].ToString().Trim() + "' AND MO_ID = SM.MO_ID) IS NOT NULL THEN 'Y' ELSE 'N' END AS HAS_MODEL ";
                sqlstr += " FROM SYS_MODELS SM ORDER BY SM.MODEL_SORT, SM.FUNC_SORT ";

                DataTable Dt = ass_m.DBExecSQL(sqlstr);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        string set_default_page = "N";
                        if (Dt.Rows[i]["SET_DEFAULT_PAGE"].ToString().Trim() != "")
                        {
                            set_default_page = Dt.Rows[i]["SET_DEFAULT_PAGE"].ToString().Trim();
                        }

                        modellist.Add(new ModelItem()
                        {
                            mo_id = Dt.Rows[i]["mo_id"].ToString().Trim(),
                            button_name = Dt.Rows[i]["button_name"].ToString().Trim(),
                            classname = Dt.Rows[i]["class_name"].ToString().Trim(),
                            set_default_page = set_default_page,
                            has_model = Dt.Rows[i]["has_model"].ToString().Trim()
                        });
                    }
                    ViewData["modellist"] = modellist;
                }
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }
            finally
            {
                this.ass_m.DBClose();
            }

            return View();
        }

        #endregion

        #region 班表

        /// <summary> 護理人員班表 </summary>
        public ActionResult ShiftArrangement()
        {
            return View();
        }

        #endregion

        #region 派班作業

        /// <summary> 派班作業 </summary>
        public ActionResult ShiftAsign()
        {
            try
            {
                byte[] cccListByteCode = webService.GetCostCenterList();
                if (cccListByteCode != null)
                {
                    string cccListJsonArr = CompressTool.DecompressString(cccListByteCode);
                    List<CostCenterList> cccList = JsonConvert.DeserializeObject<List<CostCenterList>>(cccListJsonArr);
                    Dictionary<string, string> cList = new Dictionary<string, string>();

                    for (int i = 0; i <= cccList.Count - 1; i++)
                        cList.Add(cccList[i].CCCDescription.ToString().Trim(), cccList[i].CostCenterCode.ToString().Trim());
                    ViewData["ccc_list"] = cList;
                }
                if (Request["saveinfo"] != null)
                    ViewData["saveinfo"] = Request["saveinfo"].ToString().Trim();

                if (int.Parse(DateTime.Now.ToString("HHmm")) > 800 && int.Parse(DateTime.Now.ToString("HHmm")) < 1559)
                    ViewBag.cs = "D";
                else if (int.Parse(DateTime.Now.ToString("HHmm")) > 1600 && int.Parse(DateTime.Now.ToString("HHmm")) < 2359)
                    ViewBag.cs = "E";
                else
                    ViewBag.cs = "N";

                ViewData["user_ccc"] = userinfo.CostCenterCode.Trim();
                ViewData["shift_category"] = this.cd.getSelectList("vitalsign", "shiftCategory");

            }
            catch (Exception ex)
            {
                try
                {
                    //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                    string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                    string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                    write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                }
                catch (Exception e)
                {
                    //Do nothing
                }
            }
            finally
            {
                this.link.DBClose();
            }
            return View();
        }

        /// <summary> 護理站床位清單(與病人清單相同) </summary>
        public ActionResult ShiftStationList()
        {
            try
            {
                // 先取得病人清單
                if (Request["costcode"] != null)
                {
                    byte[] cclistByteCode = webService.GetPatientList(Request["costcode"].ToString().Trim());
                    if (cclistByteCode != null)
                    {
                        string cclistJsonArr = CompressTool.DecompressString(cclistByteCode);
                        List<PatientList> ptlist = JsonConvert.DeserializeObject<List<PatientList>>(cclistJsonArr);
                        ptlist.Sort((x, y) => { return -x.BedNo.CompareTo(y.BedNo); });
                        ptlist.Reverse();
                        string shift_cat = (Request["shiftcat"] == null) ? "" : Request["shiftcat"].ToString().Trim();
                        string shift_date = (Request["shiftday"] == null) ? "" : Request["shiftday"].ToString().Trim();

                        List<ShiftList> sd = new List<ShiftList>();
                        for (int i = 0; i < ptlist.Count; i++)
                        {
                            sd.Add(new ShiftList()
                            {
                                bedno = ptlist[i].BedNo,
                                PatientName = (Convert.ToDateTime(shift_date + " 23:59:59") < DateTime.Now) ? "" : ptlist[i].PatientName,
                                leader_empno = "N",
                                response_empno = "",
                                response_name = "",
                                combine_empno = "",
                                combine_name = ""
                            });
                        }

                        //取得現有交班資料
                        string costCode = Request["costcode"].ToString().Trim();
                        List<string[]> shiftinfolist = new List<string[]>();
                        string sqlstr = " SELECT BED_NO, LEADER_USER, RESPONSIBLE_USER, RESPONSIBLE_NAME, GUIDE_USER, GUIDE_NAME ";
                        sqlstr += " FROM DATA_DISPATCHING WHERE SHIFT_CATE = '" + shift_cat + "' ";
                        sqlstr += " AND SHIFT_DATE = TO_DATE('" + shift_date + "','yyyy/MM/dd') ";

                        //ken 20240328 新增costcode判斷
                        sqlstr += " AND COST_CODE = '"+ costCode + "' ";
                        DataTable Dt = ass_m.DBExecSQL(sqlstr);
                        if (Dt.Rows.Count > 0)
                        {
                            for (int i = 0; i < Dt.Rows.Count; i++)
                            {
                                shiftinfolist.Add(new string[] {
                                Dt.Rows[i]["BED_NO"].ToString().Trim(),
                                Dt.Rows[i]["LEADER_USER"].ToString().Trim(),
                                Dt.Rows[i]["RESPONSIBLE_USER"].ToString().Trim(),
                                Dt.Rows[i]["RESPONSIBLE_NAME"].ToString().Trim(),
                                Dt.Rows[i]["GUIDE_USER"].ToString().Trim(),
                                Dt.Rows[i]["GUIDE_NAME"].ToString().Trim()
                            });
                            }
                        }
                        for (int i = 0; i < sd.Count; i++)
                        {
                            for (int j = 0; j < shiftinfolist.Count; j++)
                            {
                                //相同床號
                                if (sd[i].bedno.Trim() == shiftinfolist[j][0])
                                {
                                    sd[i].leader_empno = shiftinfolist[j][1];
                                    sd[i].response_empno = shiftinfolist[j][2];
                                    sd[i].response_name = shiftinfolist[j][3];
                                    sd[i].combine_empno = shiftinfolist[j][4];
                                    sd[i].combine_name = shiftinfolist[j][5];
                                }
                            }
                        }
                        ViewData["shiftData"] = sd;

                    }
                }

                return View();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return View();
            }
        }

        /// <summary> 護理人員排班清單(包含未排班人員) </summary>
        public ActionResult ShiftMemberList()
        {
            string costcode = "%";
            string shiftday = "%";
            string shiftcat = "%";
            if (Request["costcode"] != "")
                costcode = Request["costcode"].ToString().Trim();
            if (Request["shiftday"] != "")
                shiftday = Request["shiftday"].ToString().Trim().Replace("/", "");
            if (Request["shiftcat"] != "")
                shiftcat = Request["shiftcat"].ToString().Trim();
            //2014/03/19 改 換GetShiftData 因web還未改好先不用
            // byte[] shiftByteCode = { };
            string shiftJson = "";
            string SelfStatus = string.Empty;
            if (costcode.ToString().Trim() != "%" && shiftday != "%" && shiftcat != "%")
            {
                byte[] shiftByteCode = webService.GetShiftData(shiftday, shiftcat, costcode);
                if (shiftByteCode != null)
                {
                    shiftJson = CompressTool.DecompressString(shiftByteCode);
                    List<ShiftData> shdata = JsonConvert.DeserializeObject<List<ShiftData>>(shiftJson);
                    for (int i = 0; i < shdata.Count; i++)
                    {
                        ViewData["shdata"] = shdata;
                        ViewBag.name = shdata[i].employe_name;// userinfo.EmployeesName;
                        ViewBag.no = shdata[i].employe_no;//userinfo.EmployeesNo;
                        ViewBag.job = shdata[i].employe_title;//userinfo.JobGrade;
                    }
                }
            }
            else
            {
                byte[] shiftByteCode = webService.GetUserList(costcode);
                if (shiftByteCode != null)
                {
                    shiftJson = CompressTool.DecompressString(shiftByteCode);
                    List<UserInfo> shdata = JsonConvert.DeserializeObject<List<UserInfo>>(shiftJson);
                    ViewData["shdata"] = shdata;
                    ViewBag.name = userinfo.EmployeesName;
                    ViewBag.no = userinfo.EmployeesNo;
                    ViewBag.job = userinfo.JobGrade;
                }
            }
            ViewData["costcode"] = costcode;
            ViewData["shiftday"] = shiftday;
            return View();
        }

        /// <summary> 儲存班表 </summary>
        public ActionResult ShiftSave()
        {
            try
            {
                string[] bedList = null;
                if (Request["BedNo"] != null)
                {
                    string shift_cate = Request["shiftcategory"].ToString().Trim();
                    string day = Request["shift_day"].ToString().Trim();

                    bedList = Request["BedNo"].ToString().Split(',');
                    for (int i = 0; i <= bedList.Length - 1; i++)
                    {
                        // 取得所有資訊
                        string CostCode = Request["ccc_list"].ToString().Trim();
                        string BedNo = bedList[i].ToString().Trim();
                        string LeaderEmpno = Request["LeaderEmpno" + i.ToString()].ToString().Trim();
                        string RespEmpno = Request["RespEmpno" + i.ToString()].ToString().Trim();
                        string RespName = Request["RespName" + i.ToString()].ToString().Trim();
                        string CombEmpno = Request["CombEmpno" + i.ToString()].ToString().Trim();
                        string CombName = Request["CombName" + i.ToString()].ToString().Trim();
                        int ct = 0;


                        string wherecondition = " SHIFT_CATE='" + shift_cate + "' AND BED_NO='" + BedNo + "' AND COST_CODE = '"+ CostCode + "'";
                        wherecondition += " AND SHIFT_DATE = TO_DATE('" + day + "','yyyy/MM/dd hh24:mi:ss')";
                        string sqlstr = " SELECT COUNT(*) AS CT FROM DATA_DISPATCHING WHERE" + wherecondition;

                        DataTable Dt = ass_m.DBExecSQL(sqlstr);
                        if (Dt.Rows.Count > 0)
                        {
                            for (int j = 0; j < Dt.Rows.Count; j++)
                            {
                                ct = int.Parse(Dt.Rows[j]["ct"].ToString());
                            }
                        }

                        List<DBItem> dbList = new List<DBItem>();
                        dbList.Add(new DBItem("shift_cate", shift_cate, DBItem.DBDataType.String));
                        dbList.Add(new DBItem("cost_code", CostCode, DBItem.DBDataType.String));
                        dbList.Add(new DBItem("bed_no", BedNo, DBItem.DBDataType.String));
                        dbList.Add(new DBItem("shift_date", day, DBItem.DBDataType.DataTime));
                        dbList.Add(new DBItem("leader_user", LeaderEmpno, DBItem.DBDataType.String));
                        dbList.Add(new DBItem("responsible_user", RespEmpno, DBItem.DBDataType.String));
                        dbList.Add(new DBItem("responsible_name", RespName, DBItem.DBDataType.String));
                        dbList.Add(new DBItem("guide_user", CombEmpno, DBItem.DBDataType.String));
                        dbList.Add(new DBItem("guide_name", CombName, DBItem.DBDataType.String));
                        dbList.Add(new DBItem("modify_user", userinfo.EmployeesNo, DBItem.DBDataType.String));
                        dbList.Add(new DBItem("modify_date", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                        if (ct > 0)
                        {   //有資料處理流程
                            this.link.DBExecUpdate("DATA_DISPATCHING", dbList, wherecondition);
                        }
                        else
                        {   //無資料處理流程
                            dbList.Add(new DBItem("create_user", userinfo.EmployeesNo, DBItem.DBDataType.String));
                            dbList.Add(new DBItem("create_date", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                            this.link.DBExecInsert("DATA_DISPATCHING", dbList);
                        }
                    }
                }
                return RedirectToAction("ShiftAsign", new { @saveinfo = "儲存成功" });
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);

                return RedirectToAction("ShiftAsign", new { @saveinfo = ex.Message.ToString() });
            }

        }

        /// <summary> 班表列印 </summary>
        public ActionResult ShiftPrint()
        {
            string shift_cat = Request["shiftcat"].ToString().Trim();
            string shift_date = Request["shiftday"].ToString().Trim();
            string costcode = Request["costcode"].ToString().Trim();
            DataTable dt = new DataTable();
            ViewBag.dt = func_m.sql_shiftPrint(costcode, shift_date, ref dt);

            return View();
        }

        #endregion

        #region 下拉選單設定
        public ActionResult DataSelector()
        {
            try
            {
                string sqlstr = " SELECT P_GROUP, P_MEMO FROM SYS_PARAMS ";
                sqlstr += " WHERE P_MODEL = '" + Request["model"].ToString().Trim() + "' AND P_GROUP NOT IN('na_cate','na_dtl_type','vs_item')";
                sqlstr += " GROUP BY P_GROUP, P_MEMO ORDER BY P_MEMO";
                List<SelectListItem> modelList = new List<SelectListItem>();
                DataTable Dt = ass_m.DBExecSQL(sqlstr);
                modelList.Add(new SelectListItem
                {
                    Value = "",
                    Text = "====未選擇模組===="
                });
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        modelList.Add(new SelectListItem
                        {
                            Value = Dt.Rows[i]["P_GROUP"].ToString().Trim(),
                            Text = Dt.Rows[i]["P_MEMO"].ToString().Trim()
                        });
                    }

                }
                if (Request["group"] != null)
                    ViewData["group"] = Request["group"].ToString().Trim();
                ViewData["model"] = Request["model"].ToString();
                ViewData["modelname"] = Request["name"].ToString();
                ViewData["modellist"] = modelList;
                return View();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return View();
            }
        }

        /// <summary>
        /// 取得資料集和
        /// </summary>
        public ActionResult DataItemList()
        {
            try
            {
                string sqlstr = " select * from sys_params ";
                sqlstr += " where p_model='" + Request["model"].ToString().Trim() + "' and p_group='" + Request["group"].ToString().Trim() + "' ";
                sqlstr += " order by p_sort asc";
                List<SysParamItem> paramlist = new List<SysParamItem>();
                DataTable Dt = ass_m.DBExecSQL(sqlstr);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        paramlist.Add(new SysParamItem()
                        {
                            p_id = Dt.Rows[i]["p_id"].ToString().Trim(),
                            p_model = Dt.Rows[i]["p_model"].ToString().Trim(),
                            p_group = Dt.Rows[i]["p_group"].ToString().Trim(),
                            p_name = Dt.Rows[i]["p_name"].ToString().Trim(),
                            p_value = Dt.Rows[i]["p_value"].ToString().Trim(),
                            p_lang = Dt.Rows[i]["p_lang"].ToString().Trim(),
                            p_sort = Dt.Rows[i]["p_sort"].ToString().Trim(),
                            p_memo = Dt.Rows[i]["p_memo"].ToString().Trim()
                        });
                    }
                }
                ViewData["SysParamList"] = paramlist;
                ViewData["group"] = Request["group"].ToString().Trim();
                return View();
            }
            catch (Exception ex)
            {
                this.log.saveLogMsg(ex.Message.ToString(), "DataItemList");

                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return View();
            }
        }

        /// <summary>
        /// 項目編輯
        /// </summary>
        public ActionResult DataItemProcess()
        {
            string message = string.Empty;
            try
            {
                string model = Request["model"].ToString().Trim();
                string group = Request["group"].ToString().Trim();

                switch (Request["p_type"].ToString().Trim())
                {
                    case "save":
                        string[] id_list = Request["p_id"].ToString().Split(',');
                        string[] value_list = Request["p_value"].ToString().Split(',');
                        string[] name_list = Request["p_name"].ToString().Split(',');
                        string[] memo_list = Request["p_memo"].ToString().Split(',');
                        string[] lang_list = Request["p_lang"].ToString().Split(',');
                        int updRec = 0;
                        string newId = string.Empty;
                        for (int i = 0; i <= id_list.Length - 1; i++)
                        {

                            List<DBItem> dbitem = new List<DBItem>();


                            switch (id_list[i].ToString())
                            {
                                case "N":
                                    // 先取得序號
                                    DataTable Dt = ass_m.DBExecSQL("SELECT 'P'||LPAD(TO_CHAR(TO_NUMBER(REPLACE(MAX(P_ID),'P','')) + 1),9,'0') AS MID FROM SYS_PARAMS");
                                    if (Dt.Rows.Count > 0)
                                    {
                                        for (int j = 0; j < Dt.Rows.Count; j++)
                                        //while (reader.Read())
                                        {
                                            newId = Dt.Rows[i]["MID"].ToString();
                                            //newId = reader["MID"].ToString();
                                        }
                                    }

                                    dbitem.Add(new DBItem("p_id", newId, DBItem.DBDataType.String));
                                    dbitem.Add(new DBItem("p_model", model, DBItem.DBDataType.String));
                                    dbitem.Add(new DBItem("p_group", group, DBItem.DBDataType.String));
                                    dbitem.Add(new DBItem("p_name", name_list[i], DBItem.DBDataType.String));
                                    dbitem.Add(new DBItem("p_value", value_list[i], DBItem.DBDataType.String));
                                    dbitem.Add(new DBItem("p_lang", lang_list[i], DBItem.DBDataType.String));
                                    dbitem.Add(new DBItem("p_sort", "0", DBItem.DBDataType.Number));
                                    dbitem.Add(new DBItem("p_memo", memo_list[i], DBItem.DBDataType.String));

                                    updRec += this.link.DBExecInsert("sys_params", dbitem);
                                    break;
                                default:
                                    dbitem.Add(new DBItem("p_name", name_list[i], DBItem.DBDataType.String));
                                    dbitem.Add(new DBItem("p_value", value_list[i], DBItem.DBDataType.String));
                                    updRec += this.link.DBExecUpdate("sys_params", dbitem, " p_id = '" + id_list[i].Trim() + "' ");
                                    break;
                            }
                            if (updRec == 0)
                                message = "無任何資料被儲存";
                            else if (updRec > 1)
                                message = "儲存完成";
                        }

                        break;
                    case "del":
                        string[] del_id_list = Request["edit_id"].ToString().Split(',');
                        for (int i = 0; i <= del_id_list.Length - 1; i++)
                        {
                            int ct = 0;
                            string sqlstr = " select count(*) as ct from sys_params where p_model = '" + model + "' and p_group = '" + group + "' ";
                            DataTable Dt = link.DBExecSQL(sqlstr);
                            if (Dt.Rows.Count > 0)
                                ct = int.Parse(Dt.Rows[0]["ct"].ToString());
                            if (ct <= 1)
                            {
                                List<DBItem> data = new List<DBItem>();
                                data.Add(new DBItem("p_name", "未設定", DBItem.DBDataType.String));
                                data.Add(new DBItem("p_value", "未設定", DBItem.DBDataType.String));
                                this.link.DBExecUpdate("sys_params", data, "p_id = '" + del_id_list[i] + "' ");
                            }
                            else
                            {
                                this.link.DBExecDelete("sys_params", "p_id = '" + del_id_list[i] + "' ");
                            }

                        }
                        message = "刪除完畢";
                        break;
                }
            }
            catch (Exception ex)
            {
                message = ex.Message.ToString();
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }
            return RedirectToAction("DataSelector", new
            {
                @model = Request["model"].ToString().Trim(),
                @name = Request["name"].ToString().Trim(),
                @group = Request["group"].ToString().Trim(),
                @message = message
            });
        }

        #endregion

        #region 輔助查詢

        [HttpGet]
        public ActionResult Ais_Inquiry()
        {
            ViewBag.dt = func_m.sel_Ais_Inquiry("");
            return View();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Ais_Inquiry(FormCollection form)
        {
            string[] mode = form["mode"].Split(',');

            #region 宣告dt,變數
            DataTable dt_insert = new DataTable(); DataTable dt_upd = new DataTable();
            string[] dt_insert_column = { "DATA_ID", "CREANO", "DATA_TYPE", "DATA_QUEUE", "DATA_NAME", "DATA_ROUTE", "PARAMETER" };
            string[] dt_upd_column = { "DATA_TYPE", "DATA_QUEUE", "DATA_NAME", "DATA_ROUTE", "PARAMETER", "WHERE" };
            string[] dt_insert_type = { "String", "String", "String", "Number", "String", "String", "String" };
            string[] dt_upd_type = { "String", "Number", "String", "String", "String", "NONE" };
            set_dt_column(dt_insert, dt_insert_column);
            set_dt_column(dt_upd, dt_upd_column);
            set_dt_datatype(dt_insert, dt_insert_type);
            set_dt_datatype(dt_upd, dt_upd_type);

            string userno = userinfo.EmployeesNo;
            string[] id = form["id"].Split(',');
            string[] type = form["type"].Split(',');
            string[] name = form["name"].Split(',');
            string[] route = form["route"].Split(',');
            string[] queue = form["queue"].Split(',');
            #endregion

            for (int i = 1; i < mode.Length; i++)
            {
                if (mode[i] == "Insert")
                {
                    DataRow insert_row = dt_insert.NewRow();
                    insert_row["DATA_ID"] = base.creatid("AIS_INQUIRY_DATA", userno, "", i.ToString());
                    insert_row["CREANO"] = userno;
                    insert_row["DATA_TYPE"] = type[i];
                    insert_row["DATA_QUEUE"] = queue[i];
                    insert_row["DATA_NAME"] = name[i];
                    insert_row["DATA_ROUTE"] = route[i];
                    insert_row["PARAMETER"] = "-1";
                    dt_insert.Rows.Add(insert_row);
                }
                else if (mode[i] == "Update")
                {
                    DataRow upd_row = dt_upd.NewRow();
                    upd_row["DATA_TYPE"] = type[i];
                    upd_row["DATA_QUEUE"] = queue[i];
                    upd_row["DATA_NAME"] = name[i];
                    upd_row["DATA_ROUTE"] = route[i];
                    upd_row["PARAMETER"] = "-1";
                    upd_row["WHERE"] = " DATA_ID = '" + id[i] + "' ";

                    dt_upd.Rows.Add(upd_row);
                }
            }
            if (dt_insert.Rows.Count > 1 || dt_upd.Rows.Count > 1)
            {
                int erow = 0;
                if (dt_insert.Rows.Count > 1)
                    erow += func_m.insert("AIS_INQUIRY_DATA", dt_insert);
                if (dt_upd.Rows.Count > 1)
                    erow += func_m.upd("AIS_INQUIRY_DATA", dt_upd);
                if (erow > 0)
                    Response.Write("<script>alert('儲存成功');window.location.href='Ais_Inquiry';</script>");
                else
                    Response.Write("<script>alert('儲存失敗');window.location.href='Ais_Inquiry';</script>");
            }

            return new EmptyResult();
        }

        [HttpPost]
        public ActionResult del_Ais_Inquiry()
        {
            string[] id = Request["del_ID"].ToString().Split(',');
            string where = " DATA_ID IN ('" + String.Join("','", id) + "')";
            int erow = func_m.del("AIS_INQUIRY_DATA", where);
            if (erow > 0)
                Response.Write("<script>alert('刪除成功');window.location.href='Ais_Inquiry';</script>");
            else
                Response.Write("<script>alert('刪除失敗');window.location.href='Ais_Inquiry';</script>");
            return new EmptyResult();
        }

        public ActionResult Ais_Inquiry_Index()
        {

            DataTable dt = func_m.sel_Ais_Inquiry("");
            foreach (DataRow r in dt.Rows)
            {

                if (r["DATA_TYPE"].ToString() == "url")
                    r["DATA_ROUTE"] = "http://" + r["DATA_ROUTE"].ToString().Replace("#userno#", userinfo.EmployeesNo).Replace("#username#", userinfo.EmployeesName).Replace("#pwd#", userinfo.Pwd).Replace("#usercode#", userinfo.CostCenterCode).Replace("#date#", DateTime.Now.ToString("yyyy/MM/dd"));
                else if (r["DATA_TYPE"].ToString() == "program")
                    r["DATA_ROUTE"] = r["DATA_ROUTE"].ToString().Replace("#userno#", userinfo.EmployeesNo).Replace("#username#", userinfo.EmployeesName).Replace("#pwd#", userinfo.Pwd).Replace("#usercode#", userinfo.CostCenterCode).Replace("#date#", DateTime.Now.ToString("yyyy/MM/dd"));

                if (Session["PatInfo"] != null)
                    r["DATA_ROUTE"] = r["DATA_ROUTE"].ToString().Replace("#feeno#", ptinfo.FeeNo).Replace("#chartno#", ptinfo.ChartNo).Replace("#cccode#", ptinfo.CostCenterNo);
            }
            ViewBag.dt = dt;
            return View();
        }

        #endregion

        #region 衛教維護

        [HttpGet]
        public ActionResult Health_Education_Maintain(string mode)
        {
            ViewBag.mode = mode;
            ViewData["division"] = this.cd.getSelectItem("health_education", "division");
            ViewData["branch_division"] = this.cd.getSelectItem("health_education", "branch_division");
            ViewBag.dt = func_m.sel_type("health_education", "division");
            ViewBag.dt_branch_division = func_m.sel_type("health_education", "branch_division");
            ViewBag.dt_explanation = func_m.sel_type("health_education", "explanation");
            return View();
        }

        public ActionResult Partial_Health_Education_Item_List(string mode, string column, string key)
        {
            ViewBag.mode = mode;
            ViewData["division"] = this.cd.getSelectItem("health_education", "division");
            ViewData["branch_division"] = this.cd.getSelectItem("health_education", "branch_division");
            ViewData["explanation"] = this.cd.getSelectItem("health_education", "explanation");
            if (column != null && key != null)
            {
                if (column == "CATEGORY_ID")
                    ViewBag.dt = func_m.sel_health_education_item(column, "", key);
                else
                    ViewBag.dt = func_m.sel_health_education_item("", column, key);
            }
            else
                ViewBag.dt = null;

            return View();
        }

        //新增項目
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Health_Education_Item_Insert(FormCollection form)
        {
            #region 宣告Table,Post資料
            DataTable dt = new DataTable();
            string[] dt_column = { "ITEM_ID", "NAME", "ITEM_REFERENCE_URL", "ICD9_ID", "CATEGORY_ID", "SECOND_CATEGORY_ID", "EXPLANATION_ID", "OTHER", "ITEM_CATEGORY", "SHOW" , "CREATE_DATE" };
            string[] dt_type = { "Number", "String", "String", "String", "String", "String", "String", "String", "String", "String" , "DataTime" };
            set_dt_column(dt, dt_column);
            set_dt_datatype(dt, dt_type);
            string[] name = form["item_name"].ToString().Split(',');
            string[] route = form["route"].ToString().Split(',');
            string[] icd = form["icd"].ToString().Split(',');
            string[] item = form["item"].ToString().Split(',');
            string[] category = form["division"].ToString().Split(',');
            //  string[] second_category = form["branch_division"].ToString().Split(',');
            string[] explanation = form["explanation"].ToString().Split(',');
            #endregion

            for (int i = 1; i < name.Length; i++)
            {
                DataRow r = dt.NewRow();
                r["ITEM_ID"] = "(SELECT 'HE'||LPAD(TO_CHAR(TO_NUMBER(REPLACE(NVL(MAX(ITEM_ID),'HE0000000'),'HE','')) + 1),8,'0') FROM HEALTH_EDUCATION_ITEM_DATA WHERE ITEM_ID LIKE 'HE%' AND LENGTH(ITEM_ID) < 11)";
                r["NAME"] = name[i];
                r["ITEM_REFERENCE_URL"] = route[i];
                r["ICD9_ID"] = icd[i];
                r["CATEGORY_ID"] = category[i];
                r["SECOND_CATEGORY_ID"] = "P000000313";//second_category[i];  
                r["EXPLANATION_ID"] = explanation[i];
                r["OTHER"] = "0";
                r["ITEM_CATEGORY"] = item[i];
                r["SHOW"] = "Y";
                r["CREATE_DATE"] = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                dt.Rows.Add(r);
            }

            int erow = func_m.insert("HEALTH_EDUCATION_ITEM_DATA", dt);
            if (erow > 0)
                Response.Write("<script>alert('儲存成功!');window.location.href='Health_Education_Maintain?mode=insert';</script>");
            else
                Response.Write("<script>alert('儲存失敗!');window.location.href='Health_Education_Maintain?mode=insert';</script>");

            return new EmptyResult();
        }

        //更新項目
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Health_Education_Item_Upd(FormCollection form)
        {
            #region 宣告Table,Post資料
            DataTable dt = new DataTable();
            string[] dt_column = { "NAME", "ITEM_REFERENCE_URL", "ICD9_ID", "CATEGORY_ID", "EXPLANATION_ID", "ITEM_CATEGORY", "SHOW", "WHERE" };
            string[] dt_type = { "String", "String", "String", "String", "String", "String", "String", "NONE" };
            set_dt_column(dt, dt_column);
            set_dt_datatype(dt, dt_type);
            string[] id = form["item_id"].ToString().Split(',');
            string[] name = form["item_name"].ToString().Split(',');
            string[] route = form["route"].ToString().Split(',');
            string[] icd = form["icd"].ToString().Split(',');
            string[] item = form["item"].ToString().Split(',');
            string[] category = form["division"].ToString().Split(',');
            string[] explanation = form["explanation"].ToString().Split(',');
            #endregion

            for (int i = 0; i < name.Length; i++)
            {
                DataRow r = dt.NewRow();
                r["NAME"] = name[i];
                r["ITEM_REFERENCE_URL"] = route[i];
                r["ICD9_ID"] = icd[i];
                r["CATEGORY_ID"] = category[i];
                r["EXPLANATION_ID"] = explanation[i];
                r["ITEM_CATEGORY"] = item[i];
                r["SHOW"] = form["del_ID_" + id[i]].ToString();
                r["WHERE"] = " ITEM_ID = '" + id[i] + "' ";
                dt.Rows.Add(r);
            }

            int erow = func_m.upd("HEALTH_EDUCATION_ITEM_DATA", dt);
            if (erow > 0)
                Response.Write("<script>alert('儲存成功!');window.location.href='Health_Education_Maintain?mode=edit';</script>");
            else
                Response.Write("<script>alert('儲存失敗!');window.location.href='Health_Education_Maintain?mode=edit';</script>");

            return new EmptyResult();
        }

        //新增科別
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Divisioin_Insert(FormCollection form)
        {
            #region 宣告Table,Post資料
            DataTable dt_insert = new DataTable(); DataTable dt_update = new DataTable();
            string[] dt_insert_column = { "P_ID", "P_MODEL", "P_GROUP", "P_NAME", "P_VALUE", "P_LANG", "P_SORT", "P_MEMO" };
            string[] dt_insert_type = { "Number", "String", "String", "String", "Number", "String", "Number", "String" };
            set_dt_column(dt_insert, dt_insert_column);
            set_dt_datatype(dt_insert, dt_insert_type);
            string[] dt_upd_cloumn = { "P_NAME", "P_SORT", "WHERE" };
            string[] dt_upd_type = { "String", "Number", "NONE" };
            set_dt_column(dt_update, dt_upd_cloumn);
            set_dt_datatype(dt_update, dt_upd_type);

            string[] name = form["division_name"].ToString().Split(',');
            string[] mode = form["division_mode"].ToString().Split(',');
            string[] id = form["division_id"].ToString().Split(',');
            #endregion

            for (int i = 1; i < name.Length; i++)
            {
                if (mode[i] == "Insert")
                {
                    DataRow insert_r = dt_insert.NewRow();
                    insert_r["P_ID"] = "(SELECT 'P'||LPAD(TO_CHAR(TO_NUMBER(REPLACE(NVL(MAX(P_ID),'P000000000'),'P','')) + 1),9,'0') FROM SYS_PARAMS)";
                    insert_r["P_MODEL"] = "health_education";
                    insert_r["P_GROUP"] = "division";
                    insert_r["P_NAME"] = name[i];
                    insert_r["P_VALUE"] = "(SELECT 'P'||LPAD(TO_CHAR(TO_NUMBER(REPLACE(NVL(MAX(P_ID),'P000000000'),'P','')) + 1),9,'0') FROM SYS_PARAMS)";
                    insert_r["P_LANG"] = "zh-TW";
                    insert_r["P_SORT"] = i;
                    insert_r["P_MEMO"] = "衛教科別";
                    dt_insert.Rows.Add(insert_r);
                }
                else if (mode[i] == "Update")
                {
                    DataRow upd_r = dt_update.NewRow();
                    upd_r["P_NAME"] = name[i];
                    upd_r["P_SORT"] = i;
                    upd_r["WHERE"] = " P_ID = '" + id[i] + "' ";
                    dt_update.Rows.Add(upd_r);
                }
            }

            if (dt_insert.Rows.Count > 1 || dt_update.Rows.Count > 1)
            {
                int erow = 0;
                if (dt_insert.Rows.Count > 1)
                    erow += func_m.insert("SYS_PARAMS", dt_insert);
                if (dt_update.Rows.Count > 1)
                    erow += func_m.upd("SYS_PARAMS", dt_update);

                if (erow > 0)
                    Response.Write("<script>alert('儲存成功!');window.location.href='Health_Education_Maintain?mode=" + form["mode"] + "';</script>");
                else
                    Response.Write("<script>alert('儲存失敗!');window.location.href='Health_Education_Maintain?mode=" + form["mode"] + "';</script>");
            }
            return new EmptyResult();
        }

        //刪除科別
        [HttpPost]
        public ActionResult Del_PARAMS()
        {
            string[] id = Request["del_ID"].ToString().Split(',');
            string where = " P_ID IN ('" + String.Join("','", id) + "')";
            int erow = func_m.del("SYS_PARAMS", where);
            if (erow > 0)
                Response.Write("<script>alert('刪除成功');window.location.href='Health_Education_Maintain?mode=" + Request["mode"].ToString() + "';</script>");
            else
                Response.Write("<script>alert('刪除失敗');window.location.href='Health_Education_Maintain?mode=" + Request["mode"].ToString() + "';</script>");
            return new EmptyResult();
        }

        //新增次科別
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Branch_Division_Insert(FormCollection form)
        {
            #region 宣告Table,Post資料
            DataTable dt_insert = new DataTable(); DataTable dt_update = new DataTable();
            string[] dt_insert_column = { "P_ID", "P_MODEL", "P_GROUP", "P_NAME", "P_VALUE", "P_LANG", "P_SORT", "P_MEMO" };
            string[] dt_insert_type = { "Number", "String", "String", "String", "Number", "String", "Number", "String" };
            set_dt_column(dt_insert, dt_insert_column);
            set_dt_datatype(dt_insert, dt_insert_type);
            string[] dt_upd_cloumn = { "P_NAME", "P_SORT", "WHERE" };
            string[] dt_upd_type = { "String", "Number", "NONE" };
            set_dt_column(dt_update, dt_upd_cloumn);
            set_dt_datatype(dt_update, dt_upd_type);

            string[] name = form["branch_division_name"].ToString().Split(',');
            string[] mode = form["branch_division_mode"].ToString().Split(',');
            string[] id = form["branch_division_id"].ToString().Split(',');
            #endregion

            for (int i = 1; i < name.Length; i++)
            {
                if (mode[i] == "Insert")
                {
                    DataRow insert_r = dt_insert.NewRow();
                    insert_r["P_ID"] = "(SELECT 'P'||LPAD(TO_CHAR(TO_NUMBER(REPLACE(NVL(MAX(P_ID),'P000000000'),'P','')) + 1),9,'0') FROM SYS_PARAMS)";
                    insert_r["P_MODEL"] = "health_education";
                    insert_r["P_GROUP"] = "branch_division";
                    insert_r["P_NAME"] = name[i];
                    insert_r["P_VALUE"] = "(SELECT 'P'||LPAD(TO_CHAR(TO_NUMBER(REPLACE(NVL(MAX(P_ID),'P000000000'),'P','')) + 1),9,'0') FROM SYS_PARAMS)";
                    insert_r["P_LANG"] = "zh-TW";
                    insert_r["P_SORT"] = i;
                    insert_r["P_MEMO"] = "衛教次科別";
                    dt_insert.Rows.Add(insert_r);
                }
                else if (mode[i] == "Update")
                {
                    DataRow upd_r = dt_update.NewRow();
                    upd_r["P_NAME"] = name[i];
                    upd_r["P_SORT"] = i;
                    upd_r["WHERE"] = " P_ID = '" + id[i] + "' ";
                    dt_update.Rows.Add(upd_r);
                }
            }

            if (dt_insert.Rows.Count > 1 || dt_update.Rows.Count > 1)
            {
                int erow = 0;
                if (dt_insert.Rows.Count > 1)
                    erow += func_m.insert("SYS_PARAMS", dt_insert);
                if (dt_update.Rows.Count > 1)
                    erow += func_m.upd("SYS_PARAMS", dt_update);

                if (erow > 0)
                    Response.Write("<script>alert('儲存成功!');window.location.href='Health_Education_Maintain?mode=" + form["mode"] + "';</script>");
                else
                    Response.Write("<script>alert('儲存失敗!');window.location.href='Health_Education_Maintain?mode=" + form["mode"] + "';</script>");
            }
            return new EmptyResult();
        }

        //新增說明類別
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Explanation_Insert(FormCollection form)
        {
            #region 宣告Table,Post資料
            DataTable dt_insert = new DataTable(); DataTable dt_update = new DataTable();
            string[] dt_insert_column = { "P_ID", "P_MODEL", "P_GROUP", "P_NAME", "P_VALUE", "P_LANG", "P_SORT", "P_MEMO" };
            string[] dt_insert_type = { "Number", "String", "String", "String", "Number", "String", "Number", "String" };
            set_dt_column(dt_insert, dt_insert_column);
            set_dt_datatype(dt_insert, dt_insert_type);
            string[] dt_upd_cloumn = { "P_NAME", "P_SORT", "WHERE" };
            string[] dt_upd_type = { "String", "Number", "NONE" };
            set_dt_column(dt_update, dt_upd_cloumn);
            set_dt_datatype(dt_update, dt_upd_type);

            string[] name = form["explanation_name"].ToString().Split(',');
            string[] mode = form["explanation_mode"].ToString().Split(',');
            string[] id = form["explanation_id"].ToString().Split(',');
            #endregion

            for (int i = 1; i < name.Length; i++)
            {
                if (mode[i] == "Insert")
                {
                    DataRow insert_r = dt_insert.NewRow();
                    insert_r["P_ID"] = "(SELECT 'P'||LPAD(TO_CHAR(TO_NUMBER(REPLACE(NVL(MAX(P_ID),'P000000000'),'P','')) + 1),9,'0') FROM SYS_PARAMS)";
                    insert_r["P_MODEL"] = "health_education";
                    insert_r["P_GROUP"] = "explanation";
                    insert_r["P_NAME"] = name[i];
                    insert_r["P_VALUE"] = "(SELECT 'P'||LPAD(TO_CHAR(TO_NUMBER(REPLACE(NVL(MAX(P_ID),'P000000000'),'P','')) + 1),9,'0') FROM SYS_PARAMS)";
                    insert_r["P_LANG"] = "zh-TW";
                    insert_r["P_SORT"] = i;
                    insert_r["P_MEMO"] = "衛教說明類別";
                    dt_insert.Rows.Add(insert_r);
                }
                else if (mode[i] == "Update")
                {
                    DataRow upd_r = dt_update.NewRow();
                    upd_r["P_NAME"] = name[i];
                    upd_r["P_SORT"] = i;
                    upd_r["WHERE"] = " P_ID = '" + id[i] + "' ";
                    dt_update.Rows.Add(upd_r);
                }
            }

            if (dt_insert.Rows.Count > 1 || dt_update.Rows.Count > 1)
            {
                int erow = 0;
                if (dt_insert.Rows.Count > 1)
                    erow += func_m.insert("SYS_PARAMS", dt_insert);
                if (dt_update.Rows.Count > 1)
                    erow += func_m.upd("SYS_PARAMS", dt_update);

                if (erow > 0)
                    Response.Write("<script>alert('儲存成功!');window.location.href='Health_Education_Maintain?mode=" + form["mode"] + "';</script>");
                else
                    Response.Write("<script>alert('儲存失敗!');window.location.href='Health_Education_Maintain?mode=" + form["mode"] + "';</script>");
            }
            return new EmptyResult();
        }

        #endregion

        #region 護理評估_項目

        /// <summary> 儲存護理評估項目_檢視 </summary>
        public ActionResult Na_Item()
        {
            try
            {
                string sqlstr = " SELECT * FROM SYS_NAITEM ORDER BY NA_CATE DESC, NA_SORT ASC";
                List<NursingAssessmentItem> item_info = new List<NursingAssessmentItem>();
                DataTable Dt = ass_m.DBExecSQL(sqlstr);

                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        item_info.Add(new NursingAssessmentItem()
                        {
                            na_cate = Dt.Rows[i]["na_cate"].ToString().Trim(),
                            na_name = Dt.Rows[i]["na_name"].ToString().Trim(),
                            na_type = Dt.Rows[i]["na_type"].ToString().Trim(),
                            na_sort = int.Parse(Dt.Rows[i]["na_sort"].ToString().Trim())
                            //na_cate = reader["na_cate"].ToString().Trim(),
                            //na_name = reader["na_name"].ToString().Trim(),
                            //na_type = reader["na_type"].ToString().Trim(),
                            //na_sort = int.Parse(reader["na_sort"].ToString().Trim())
                        });
                    }
                }

                ViewData["item_info"] = item_info;
                Dictionary<string, string> na_cate_list = this.cd.getSelectList("system", "na_cate");
                ViewData["na_cate_list"] = na_cate_list;
                return View();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);

                return RedirectToAction("Na_Index", new { @message = ex.Message.ToString() });
            }
        }

        /// <summary> 儲存護理評估項目_新增項目 </summary>
        public ActionResult Na_Item_Add()
        {
            try
            {
                List<DBItem> insList = new List<DBItem>();
                insList.Add(new DBItem("na_cate", Request["na_cate"].ToString().Trim(), DBItem.DBDataType.String));
                insList.Add(new DBItem("na_name", Request["na_name"].ToString().Trim(), DBItem.DBDataType.String));
                insList.Add(new DBItem("na_type", Request["na_type"].ToString().Trim(), DBItem.DBDataType.String));
                insList.Add(new DBItem("na_sort", Request["na_sort"].ToString().Trim(), DBItem.DBDataType.Number));
                int effRow = this.link.DBExecInsert("sys_naitem", insList);
                if (effRow != 1)
                    Response.Write("N");
                else
                    Response.Write("Y");
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                Response.Write(ex.Message.ToString());
                return new EmptyResult();
            }
        }

        /// <summary> 儲存護理評估項目_儲存 </summary>
        public ActionResult Na_Item_Save()
        {
            string message = string.Empty;
            try
            {
                int effRow = 0;
                string[] na_type = Request["na_type"].ToString().Trim().Split(',');
                for (int i = 0; i <= na_type.Length - 1; i++)
                {
                    List<DBItem> updlist = new List<DBItem>();
                    updlist.Add(new DBItem("na_cate", Request[na_type[i] + "_na_cate"].ToString().Trim(), DBItem.DBDataType.String));
                    updlist.Add(new DBItem("na_name", Request[na_type[i] + "_na_name"].ToString().Trim(), DBItem.DBDataType.String));
                    updlist.Add(new DBItem("na_type", na_type[i], DBItem.DBDataType.String));
                    updlist.Add(new DBItem("na_sort", Request[na_type[i] + "_na_sort".ToString().Trim()], DBItem.DBDataType.Number));
                    effRow += this.link.DBExecUpdate("sys_naitem", updlist, "na_type='" + na_type[i] + "'");
                }
                if (effRow == na_type.Length)
                    message = "更新成功";
                else
                    message = "更新失敗";
            }
            catch (Exception ex)
            {
                message = ex.Message.ToString();
            }
            return RedirectToAction("Na_Item", new { @message = message });
        }

        #endregion

        #region 護理評估_主檔

        /// <summary> 儲存護理評估_主功能頁面 </summary>
        public ActionResult Na_Index()
        {
            try
            {
                string sqlstr = string.Empty;
                string lang = NIS.MvcApplication.iniObj.NisSystem.Language;
                List<SelectListItem> na_list = new List<SelectListItem>();

                sqlstr = " SELECT * FROM SYS_NAITEM ORDER BY NA_CATE DESC, NA_SORT ASC ";

                DataTable Dt = ass_m.DBExecSQL(sqlstr);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        if (Request["na_type"] != null)
                        {
                            if (Dt.Rows[i]["na_type"].ToString().Trim() == Request["na_type"].ToString())
                            {
                                na_list.Add(new SelectListItem()
                                {
                                    Text = Dt.Rows[i]["na_name"].ToString().Trim(),
                                    Value = Dt.Rows[i]["na_type"].ToString().Trim(),
                                    Selected = true
                                });
                            }
                            else
                            {
                                na_list.Add(new SelectListItem()
                                {
                                    Text = Dt.Rows[i]["na_name"].ToString().Trim(),
                                    Value = Dt.Rows[i]["na_type"].ToString().Trim()
                                });
                            }
                        }
                        else
                        {
                            na_list.Add(new SelectListItem()
                            {
                                Text = Dt.Rows[i]["na_name"].ToString().Trim(),
                                Value = Dt.Rows[i]["na_type"].ToString().Trim()
                            });
                        }
                    }
                }
                ViewData["na_list"] = na_list;
                // 下拉選單資料
                Dictionary<string, string> na_cate_list = this.cd.getSelectList("system", "na_cate");
                ViewData["na_cate_list"] = na_cate_list;
                return View();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return View();
            }
        }

        /// <summary> 儲存護理評估主檔_檢視 </summary>
        public ActionResult Na_Main()
        {
            try
            {
                if (Request["na_type"] != null)
                {
                    string sqlstr = string.Empty;
                    string lang = NIS.MvcApplication.iniObj.NisSystem.Language;
                    List<NursingAssessmentMain> na_main_list = new List<NursingAssessmentMain>();

                    sqlstr = " SELECT NM.*, (SELECT NA_NAME FROM SYS_NAITEM WHERE NA_TYPE= NM.NA_TYPE) AS NA_NAME ";
                    sqlstr += " FROM SYS_NAMAIN NM WHERE NA_TYPE='" + Request["na_type"].ToString() + "' ORDER BY NA_VERSION DESC ";

                    DataTable Dt = ass_m.DBExecSQL(sqlstr);
                    if (Dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            NursingAssessment_Status na_status;
                            switch (Dt.Rows[i]["na_status"].ToString().Trim())
                            {
                                case "C":
                                    na_status = NursingAssessment_Status.C;
                                    break;
                                case "O":
                                    na_status = NursingAssessment_Status.O;
                                    break;
                                default:
                                    na_status = NursingAssessment_Status.T;
                                    break;
                            }

                            na_main_list.Add(new NursingAssessmentMain()
                            {
                                na_id = Dt.Rows[i]["na_id"].ToString().Trim(),
                                na_name = Dt.Rows[i]["na_name"].ToString().Trim(),
                                na_iso = Dt.Rows[i]["na_iso"].ToString().Trim(),
                                na_status = na_status,
                                na_type = Dt.Rows[i]["na_type"].ToString().Trim(),
                                na_version = Dt.Rows[i]["na_version"].ToString().Trim(),
                                na_desc = Dt.Rows[i]["na_desc"].ToString().Trim()
                            });
                        }
                    }

                    ViewData["na_main_list"] = na_main_list;
                }
                return View();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return View();
            }
        }

        /// <summary> 儲存護理評估主檔_新增版本 </summary>
        public ActionResult Na_Main_Add()
        {

            try
            {
                List<DBItem> insList = new List<DBItem>();
                insList.Add(new DBItem("na_id", Request["na_id"].ToString().Trim(), DBItem.DBDataType.String));
                insList.Add(new DBItem("na_iso", Request["na_iso"].ToString().Trim(), DBItem.DBDataType.String));
                insList.Add(new DBItem("na_desc", Request["na_desc"].ToString().Trim(), DBItem.DBDataType.String));
                insList.Add(new DBItem("na_status", Request["na_status"].ToString().Trim(), DBItem.DBDataType.String));
                insList.Add(new DBItem("na_version", Request["na_version"].ToString().Trim(), DBItem.DBDataType.String));
                insList.Add(new DBItem("na_type", Request["na_type"].ToString().Trim(), DBItem.DBDataType.String));
                insList.Add(new DBItem("create_user", userinfo.EmployeesNo, DBItem.DBDataType.String));
                insList.Add(new DBItem("create_date", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                insList.Add(new DBItem("modify_user", userinfo.EmployeesNo, DBItem.DBDataType.String));
                insList.Add(new DBItem("modify_date", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                int effRow = this.link.DBExecInsert("sys_namain", insList);
                if (effRow == 1)
                {
                    Response.Write("Y");
                }
            }
            catch (Exception ex)
            {
                Response.Write(ex.Message.ToString());
            }

            return new EmptyResult();
        }

        /// <summary> 儲存護理評估主檔_紀錄 </summary>
        public ActionResult NaMain_Save()
        {
            try
            {
                if (Request["na_type"] != null)
                {
                    string na_type = "A";
                    na_type = Request["na_type"].ToString();

                    string[] id_list = Request["na_id"].ToString().Trim().Split(',');

                    for (int i = 0; i <= id_list.Length - 1; i++)
                    {
                        List<DBItem> dbitem = new List<DBItem>();
                        dbitem.Add(new DBItem("na_type", Request["na_type"].ToString(), DBItem.DBDataType.String));
                        dbitem.Add(new DBItem("na_desc", Request[id_list[i] + "_na_desc"].ToString().Trim(), DBItem.DBDataType.String));
                        dbitem.Add(new DBItem("na_iso", Request[id_list[i] + "_na_iso"].ToString().Trim(), DBItem.DBDataType.String));
                        dbitem.Add(new DBItem("na_version", Request[id_list[i] + "_na_version"].ToString().Trim(), DBItem.DBDataType.String));
                        dbitem.Add(new DBItem("modify_user", userinfo.EmployeesNo, DBItem.DBDataType.String));
                        dbitem.Add(new DBItem("modify_date", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                        this.link.DBExecUpdate("SYS_NAMAIN", dbitem, "na_id='" + id_list[i].ToString() + "' ");
                    }
                    return RedirectToAction("Na_Index", new { @message = "儲存成功", @na_type = na_type });
                }
                else
                {
                    return RedirectToAction("Na_Index", new { @message = "儲存失敗，請聯絡資訊室" });
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("Na_Index", new { @message = ex.Message.ToString() });
            }

        }

        /// <summary> 儲存護理評估主檔_設定為正式評估 </summary>
        public ActionResult NaMain_Set()
        {
            try
            {
                if (Request["na_id"] != null)
                {
                    string na_id = Request["na_id"].ToString().Trim();
                    string na_type = na_id.Substring(0, 1);

                    List<DBItem> upddbitem = new List<DBItem>();
                    upddbitem.Add(new DBItem("na_status", "C", DBItem.DBDataType.String));
                    int effrow = this.link.DBExecUpdate("SYS_NAMAIN", upddbitem, "na_id='" + na_id + "'");
                    if (effrow == 1)
                    {
                        //成功後將其他的版本設定為舊版本
                        upddbitem.Clear();
                        upddbitem.Add(new DBItem("na_status", "O", DBItem.DBDataType.String));
                        this.link.DBExecUpdate("sys_namain", upddbitem, "na_id <> '" + na_id + "' and na_type='" + na_type + "' ");

                        Response.Write("Y");
                    }
                    else
                    {
                        Response.Write("N");
                    }
                }
            }
            catch (Exception ex)
            {
                Response.Write(ex.Message.ToString());
            }
            return new EmptyResult();
        }

        #endregion

        #region 護理評估_標籤

        /// <summary> 儲存護理評估標籤_檢視 </summary>
        public ActionResult Na_Tag()
        {
            try
            {
                if (Request["na_id"] != null)
                {
                    string na_id = Request["na_id"].ToString();
                    ViewData["na_id"] = na_id;
                    ViewData["na_type"] = na_id.Substring(0, 1);

                    List<NursingAssessmentTag> taglist = new List<NursingAssessmentTag>();

                    string sqlstr = " select * from sys_natag where na_id='" + na_id + "' order by tag_sort asc ";

                    DataTable Dt = ass_m.DBExecSQL(sqlstr);
                    if (Dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            int tag_sort = 0;
                            if (Dt.Rows[i]["tag_sort"].ToString() != "")
                                tag_sort = int.Parse(Dt.Rows[i]["tag_sort"].ToString().Trim());
                            taglist.Add(new NursingAssessmentTag()
                            {
                                tag_id = Dt.Rows[i]["tag_id"].ToString().Trim(),
                                tag_name = Dt.Rows[i]["tag_name"].ToString().Trim(),
                                tag_help = Dt.Rows[i]["tag_help"].ToString().Trim(),
                                tag_sort = tag_sort
                            });
                        }
                    }

                    ViewData["tag_info"] = taglist;

                    sqlstr = " SELECT SN.*,(SELECT NA_NAME FROM SYS_NAITEM WHERE NA_TYPE=SN.NA_TYPE) AS NA_NAME ";
                    sqlstr += " FROM SYS_NAMAIN SN WHERE NA_ID='" + na_id + "' ";
                    NursingAssessmentMain namain_info = new NursingAssessmentMain();
                    Dt = ass_m.DBExecSQL(sqlstr);
                    if (Dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            namain_info.na_id = Dt.Rows[i]["na_id"].ToString().Trim();
                            namain_info.na_name = Dt.Rows[i]["na_name"].ToString().Trim();
                            namain_info.na_desc = Dt.Rows[i]["na_desc"].ToString().Trim();
                            namain_info.na_version = Dt.Rows[i]["na_version"].ToString().Trim();
                        }
                    }

                    ViewData["namain_info"] = namain_info;
                    //取得舊版本資訊
                    List<NursingAssessmentMain> oldna_info = new List<NursingAssessmentMain>();
                    sqlstr = " SELECT SN.*,(SELECT NA_NAME FROM SYS_NAITEM WHERE NA_TYPE=SN.NA_TYPE) AS NA_NAME ";
                    sqlstr += " FROM SYS_NAMAIN SN WHERE NA_TYPE = '" + na_id.Substring(0, 1) + "' AND NA_STATUS IN ('C','O') ORDER BY NA_VERSION DESC ";

                    Dt = ass_m.DBExecSQL(sqlstr);
                    int idx = 0;
                    if (Dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            if (idx >= 7)
                                break;
                            oldna_info.Add(new NursingAssessmentMain()
                            {
                                na_id = Dt.Rows[i]["na_id"].ToString().Trim(),
                                na_name = Dt.Rows[i]["na_name"].ToString().Trim(),
                                na_version = Dt.Rows[i]["na_version"].ToString().Trim(),
                                na_desc = Dt.Rows[i]["na_desc"].ToString().Trim(),
                                na_iso = Dt.Rows[i]["na_iso"].ToString().Trim()
                            });
                            idx++;
                        }
                    }

                    ViewData["oldna_info"] = oldna_info;
                    return View();
                }
                else
                {
                    return RedirectToAction("Na_Index");
                }
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);

                return RedirectToAction("Na_Index");
            }
        }

        /// <summary> 儲存護理評估標籤_新增標籤 </summary>
        public ActionResult Na_Inc_Tag()
        {
            if (Request["tag_name"] != null)
            {
                List<DBItem> tag = new List<DBItem>();
                tag.Add(new DBItem("na_id", Request["na_id"].ToString().Trim(), DBItem.DBDataType.String));
                tag.Add(new DBItem("tag_id", getTagId(), DBItem.DBDataType.String));
                tag.Add(new DBItem("tag_name", Request["tag_name"].ToString().Trim(), DBItem.DBDataType.String));
                tag.Add(new DBItem("tag_sort", Request["tag_sort"].ToString().Trim(), DBItem.DBDataType.Number));
                tag.Add(new DBItem("tag_help", Request["tag_help"].ToString().Trim(), DBItem.DBDataType.String));
                int effRow = this.link.DBExecInsert("sys_natag", tag);
                if (effRow != 1)
                    Response.Write("N");
                else
                    Response.Write("Y");
            }
            return new EmptyResult();
        }


        /// <summary> 重新整理細項的親子關係 </summary>
        public void repair_parent_id(ref List<string[]> relationship, string new_na_id)
        {
            try
            {
                string sqlstr = " SELECT /*+INDEX(SYS_NADTL NA_DTL_PK)*/* FROM SYS_NADTL WHERE NA_ID='" + new_na_id + "' AND DTL_PARENT_ID IS NOT NULL ";

                DataTable Dt = ass_m.DBExecSQL(sqlstr);
                if (Dt.Rows.Count > 0)
                {
                    for (int j = 0; j < Dt.Rows.Count; j++)
                    {
                        string new_parent_id = string.Empty;
                        for (int i = 0; i <= relationship.Count - 1; i++)
                        {
                            if (relationship[i][0] != Dt.Rows[j]["dtl_parent_id"].ToString())
                            {
                                continue;
                            }
                            else
                            {
                                List<DBItem> updinfo = new List<DBItem>();
                                updinfo.Add(new DBItem("dtl_parent_id", relationship[i][1], DBItem.DBDataType.String));
                                this.link.DBExecUpdate("sys_nadtl", updinfo, " dtl_id='" + Dt.Rows[j]["dtl_id"].ToString().Trim() + "' ");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);

                throw ex;
            }
        }

        /// <summary> 複製細項 </summary>
        public void copy_dtl(string copy_na_id, string new_na_id, string copy_tag_id, string new_tag_id, ref List<string[]> dtlRefList)
        {
            try
            {

                // 複製明細
                string sqlstr = "select * from sys_nadtl where na_id='" + copy_na_id + "' and tag_id='" + copy_tag_id + "' ";

                List<NursingAssessmentDtl> newDtl = new List<NursingAssessmentDtl>();
                DataTable Dt = ass_m.DBExecSQL(sqlstr);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        newDtl.Add(new NursingAssessmentDtl()
                        {
                            dtl_id = Dt.Rows[i]["dtl_id"].ToString().Trim(),
                            dtl_title = Dt.Rows[i]["dtl_title"].ToString().Trim(),
                            dtl_child_hide = Dt.Rows[i]["dtl_child_hide"].ToString().Trim(),
                            dtl_default_value = Dt.Rows[i]["dtl_default_value"].ToString().Trim(),
                            dtl_help = Dt.Rows[i]["dtl_help"].ToString().Trim(),
                            dtl_length = int.Parse(Dt.Rows[i]["dtl_length"].ToString().Trim()),
                            dtl_parent_id = Dt.Rows[i]["dtl_parent_id"].ToString().Trim(),
                            dtl_rear_word = Dt.Rows[i]["dtl_rear_word"].ToString().Trim(),
                            dtl_show_value = Dt.Rows[i]["dtl_show_value"].ToString().Trim(),
                            dtl_sort = int.Parse(Dt.Rows[i]["dtl_sort"].ToString().Trim()),
                            dtl_type = Dt.Rows[i]["dtl_type"].ToString().Trim(),
                            dtl_value = Dt.Rows[i]["dtl_value"].ToString().Trim(),
                            dtl_must = Dt.Rows[i]["dtl_must"].ToString().Trim(),
                        });
                    }
                }

                for (int i = 0; i <= newDtl.Count - 1; i++)
                {
                    string new_dtl_id = getDtlId();
                    dtlRefList.Add(new string[] { newDtl[i].dtl_id, new_dtl_id });
                    List<DBItem> insDtlList = new List<DBItem>();
                    insDtlList.Add(new DBItem("na_id", new_na_id, DBItem.DBDataType.String));
                    insDtlList.Add(new DBItem("tag_id", new_tag_id, DBItem.DBDataType.String));
                    insDtlList.Add(new DBItem("dtl_id", new_dtl_id, DBItem.DBDataType.String));
                    insDtlList.Add(new DBItem("dtl_title", newDtl[i].dtl_title, DBItem.DBDataType.String));
                    insDtlList.Add(new DBItem("dtl_child_hide", newDtl[i].dtl_child_hide, DBItem.DBDataType.String));
                    insDtlList.Add(new DBItem("dtl_default_value", newDtl[i].dtl_default_value, DBItem.DBDataType.String));
                    insDtlList.Add(new DBItem("dtl_help", newDtl[i].dtl_help, DBItem.DBDataType.String));
                    insDtlList.Add(new DBItem("dtl_length", newDtl[i].dtl_length.ToString(), DBItem.DBDataType.Number));
                    insDtlList.Add(new DBItem("dtl_parent_id", newDtl[i].dtl_parent_id, DBItem.DBDataType.String));
                    insDtlList.Add(new DBItem("dtl_rear_word", newDtl[i].dtl_rear_word, DBItem.DBDataType.String));
                    insDtlList.Add(new DBItem("dtl_show_value", newDtl[i].dtl_show_value, DBItem.DBDataType.String));
                    insDtlList.Add(new DBItem("dtl_sort", newDtl[i].dtl_sort.ToString(), DBItem.DBDataType.Number));
                    insDtlList.Add(new DBItem("dtl_type", newDtl[i].dtl_type, DBItem.DBDataType.String));
                    insDtlList.Add(new DBItem("dtl_value", newDtl[i].dtl_value, DBItem.DBDataType.String));
                    insDtlList.Add(new DBItem("dtl_must", newDtl[i].dtl_must, DBItem.DBDataType.String));
                    link.DBExecInsert("sys_nadtl", insDtlList);
                    insDtlList.Clear();
                    insDtlList = null;
                }
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);

                throw ex;
            }
        }

        /// <summary> 儲存護理評估標籤_存檔 </summary>
        public ActionResult Na_Tag_Save()
        {
            string na_id = string.Empty;
            string message = string.Empty;
            if (Request["na_id"] != null)
            {
                na_id = Request["na_id"].ToString().Trim();
                string[] tag_id = Request["tag_id"].ToString().Trim().Split(',');

                for (int i = 0; i <= tag_id.Length - 1; i++)
                {
                    List<DBItem> updList = new List<DBItem>();
                    updList.Add(new DBItem("tag_name", Request[tag_id[i] + "_tag_name"], DBItem.DBDataType.String));
                    updList.Add(new DBItem("tag_sort", Request[tag_id[i] + "_tag_sort"], DBItem.DBDataType.Number));
                    updList.Add(new DBItem("tag_help", Request[tag_id[i] + "_tag_help"], DBItem.DBDataType.String));
                    this.link.DBExecUpdate("sys_natag", updList, "na_id='" + na_id + "' and tag_id='" + tag_id[i].ToString() + "'");
                }
                message = "儲存成功";
            }
            return RedirectToAction("Na_Tag", new { @na_id = na_id, @message = message });
        }

        /// <summary> 儲存護理評估標籤_刪除 </summary>
        public ActionResult Na_Tag_Del()
        {
            string whereCondition = string.Empty;
            //刪除標籤主檔
            whereCondition = "na_id='" + Request["na_id"].ToString().Trim() + "' and tag_id='" + Request["tag_id"].ToString().Trim() + "' ";
            this.link.DBExecDelete("sys_natag", whereCondition);
            //刪除明細
            this.link.DBExecDelete("sys_nadtl", whereCondition);
            Response.Write("Y");
            return new EmptyResult();
        }

        /// <summary> 儲存護理評估標籤_取得標籤流水號 </summary>
        public string getTagId()
        {
            string sqlstr = " SELECT 'T'||LPAD(TO_CHAR(TO_NUMBER(REPLACE(TAG_ID, 'T', '')) + 1) , 9, '0') AS NEWID ";
            sqlstr += " FROM SYS_NATAG WHERE TAG_ID = (SELECT MAX(TAG_ID) FROM SYS_NATAG) ";
            DataTable Dt = link.DBExecSQL(sqlstr);
            if (Dt.Rows.Count > 0)
            {
                for (int d = 0; d < Dt.Rows.Count; d++)
                {
                    sqlstr = Dt.Rows[d]["newid"].ToString();
                }
            }
            else
            {
                sqlstr = "T000000001";
            }
            return sqlstr;
        }

        #endregion

        #region 護理評估_細項

        /// <summary> 儲存護理評估細項_檢視 </summary>
        public ActionResult Na_Dtl()
        {
            try
            {
                if (Request["na_id"] != null)
                {
                    string na_id = Request["na_id"].ToString().Trim();
                    string tag_id = Request["tag_id"].ToString().Trim();
                    ViewData["na_id"] = na_id;
                    ViewData["na_type"] = na_id.Substring(0, 1);
                    ViewData["tag_id"] = tag_id;

                    string sqlstr = string.Empty;

                    // 主檔
                    NursingAssessmentMain namain_info = new NursingAssessmentMain();
                    sqlstr = "  SELECT SN.*, (SELECT NA_NAME FROM SYS_NAITEM WHERE NA_TYPE= SN.NA_TYPE) AS NA_NAME ";
                    sqlstr += " FROM SYS_NAMAIN SN WHERE NA_ID='" + na_id + "' ";

                    DataTable Dt = ass_m.DBExecSQL(sqlstr);
                    if (Dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            namain_info.na_id = Dt.Rows[i]["na_id"].ToString().Trim();
                            namain_info.na_name = Dt.Rows[i]["na_name"].ToString().Trim();
                            namain_info.na_iso = Dt.Rows[i]["na_iso"].ToString().Trim();
                            namain_info.na_version = Dt.Rows[i]["na_version"].ToString().Trim();
                            namain_info.na_desc = Dt.Rows[i]["na_desc"].ToString().Trim();
                        }
                    }

                    ViewData["namain_info"] = namain_info;
                    NursingAssessmentTag natag_info = new NursingAssessmentTag();
                    sqlstr = " select * from sys_natag where na_id='" + na_id + "' ";
                    sqlstr += " and tag_id='" + tag_id + "' ";

                    Dt = ass_m.DBExecSQL(sqlstr);
                    if (Dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            natag_info.tag_id = Dt.Rows[i]["tag_id"].ToString().Trim();
                            natag_info.tag_name = Dt.Rows[i]["tag_name"].ToString().Trim();
                            natag_info.tag_sort = int.Parse(Dt.Rows[i]["tag_sort"].ToString().Trim());
                            natag_info.tag_help = Dt.Rows[i]["tag_help"].ToString().Trim();
                        }
                    }

                    ViewData["natag_info"] = natag_info;

                    //明細資料
                    Dictionary<string, string> dtl_id = new Dictionary<string, string>();
                    dtl_id.Add("", "頂層");

                    List<NursingAssessmentDtl> nadtl_info = new List<NursingAssessmentDtl>();
                    sqlstr = "select * from sys_nadtl where na_id='" + na_id + "' and tag_id='" + tag_id + "' order by dtl_sort asc";

                    Dt = ass_m.DBExecSQL(sqlstr);
                    if (Dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            nadtl_info.Add(new NursingAssessmentDtl()
                            {
                                na_id = Dt.Rows[i]["na_id"].ToString().Trim(),
                                tag_id = Dt.Rows[i]["tag_id"].ToString().Trim(),
                                dtl_id = Dt.Rows[i]["dtl_id"].ToString().Trim(),
                                dtl_type = Dt.Rows[i]["dtl_type"].ToString().Trim(),
                                dtl_title = Dt.Rows[i]["dtl_title"].ToString().Trim(),
                                dtl_child_hide = Dt.Rows[i]["dtl_child_hide"].ToString().Trim(),
                                dtl_show_value = Dt.Rows[i]["dtl_show_value"].ToString().Trim(),
                                dtl_parent_id = Dt.Rows[i]["dtl_parent_id"].ToString().Trim(),
                                dtl_sort = int.Parse(Dt.Rows[i]["dtl_sort"].ToString().Trim()),
                                dtl_value = Dt.Rows[i]["dtl_value"].ToString().Trim(),
                                dtl_default_value = Dt.Rows[i]["dtl_default_value"].ToString().Trim(),
                                dtl_help = Dt.Rows[i]["dtl_help"].ToString().Trim(),
                                dtl_length = int.Parse(Dt.Rows[i]["dtl_length"].ToString().Trim()),
                                dtl_rear_word = Dt.Rows[i]["dtl_rear_word"].ToString().Trim(),
                                dtl_must = Dt.Rows[i]["dtl_must"].ToString().Trim(),

                            });
                        }
                    }


                    //取得父皆清單
                    getDtlList(ref nadtl_info, ref dtl_id, "");

                    // 將nadtl_info整理成UI需要的Obj
                    List<NursingAssessmentDtlObj> nadtl_obj = this.getDtlObj(ref nadtl_info, "");
                    ViewData["nadtl_obj"] = nadtl_obj;
                    ViewData["parent_list"] = dtl_id;
                    ViewData["dtl_type_list"] = this.cd.getSelectList("system", "na_dtl_type");

                }
                return View();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return View();
            }
        }

        /// <summary> 儲存護理評估細項_新增 </summary>
        public ActionResult Na_Dtl_Add()
        {
            try
            {
                List<DBItem> insList = new List<DBItem>();
                insList.Add(new DBItem("na_id", Request["na_id"].ToString().Trim(), DBItem.DBDataType.String));
                insList.Add(new DBItem("tag_id", Request["tag_id"].ToString().Trim(), DBItem.DBDataType.String));
                insList.Add(new DBItem("dtl_id", getDtlId(), DBItem.DBDataType.String));
                insList.Add(new DBItem("dtl_type", Request["dtl_type"].ToString().Trim(), DBItem.DBDataType.String));
                insList.Add(new DBItem("dtl_title", Request["dtl_title"].ToString().Trim(), DBItem.DBDataType.String));
                insList.Add(new DBItem("dtl_child_hide", Request["dtl_child_hide"].ToString().Trim(), DBItem.DBDataType.String));
                insList.Add(new DBItem("dtl_show_value", Request["dtl_show_value"].ToString().Trim(), DBItem.DBDataType.String));
                insList.Add(new DBItem("dtl_parent_id", Request["dtl_parent_id"].ToString().Trim(), DBItem.DBDataType.String));
                insList.Add(new DBItem("dtl_sort", Request["dtl_sort"].ToString().Trim(), DBItem.DBDataType.Number));
                insList.Add(new DBItem("dtl_value", Request["dtl_value"].ToString().Trim(), DBItem.DBDataType.String));
                insList.Add(new DBItem("dtl_default_value", Request["dtl_default_value"].ToString().Trim(), DBItem.DBDataType.String));
                insList.Add(new DBItem("dtl_length", Request["dtl_length"].ToString().Trim(), DBItem.DBDataType.Number));
                insList.Add(new DBItem("dtl_help", Request["dtl_help"].ToString().Trim(), DBItem.DBDataType.String));
                insList.Add(new DBItem("dtl_rear_word", Request["dtl_rear_word"].ToString().Trim(), DBItem.DBDataType.String));
                insList.Add(new DBItem("dtl_must", Request["dtl_must"].ToString().Trim(), DBItem.DBDataType.String));

                int effRow = this.link.DBExecInsert("sys_nadtl", insList);

                if (effRow == 1)
                    Response.Write("Y");
                else
                    Response.Write("新增失敗：請聯絡負責人");
            }
            catch (Exception ex)
            {
                Response.Write("新增失敗：" + ex.Message.ToString());
            }

            return new EmptyResult();
        }

        /// <summary> 儲存護理評估細項_儲存 </summary>
        public ActionResult Na_Dtl_Save()
        {
            string message = string.Empty;
            string na_id = string.Empty;
            string tag_id = string.Empty;
            if (Request["dtl_id"] != null)
            {
                na_id = Request["na_id"].ToString().Trim();
                tag_id = Request["tag_id"].ToString().Trim();
                string whereCondition = " na_id = '" + na_id + "' and tag_id='" + tag_id + "' ";

                string[] dtl_id_list = Request["dtl_id"].ToString().Split(',');
                for (int i = 0; i <= dtl_id_list.Length - 1; i++)
                {
                    string wcAddition = string.Empty;
                    List<DBItem> updList = new List<DBItem>();
                    updList.Add(new DBItem("dtl_type", Request[dtl_id_list[i] + "_dtl_type"].ToString().Trim(), DBItem.DBDataType.String));
                    updList.Add(new DBItem("dtl_title", Request[dtl_id_list[i] + "_dtl_title"].ToString().Trim(), DBItem.DBDataType.String));
                    if (Request[dtl_id_list[i] + "_dtl_child_hide"] != null)
                        updList.Add(new DBItem("dtl_child_hide", "Y", DBItem.DBDataType.String));
                    else
                        updList.Add(new DBItem("dtl_child_hide", "N", DBItem.DBDataType.String));
                    updList.Add(new DBItem("dtl_show_value", Request[dtl_id_list[i] + "_dtl_show_value"].ToString().Trim(), DBItem.DBDataType.String));
                    updList.Add(new DBItem("dtl_parent_id", Request[dtl_id_list[i] + "_dtl_parent_id"].ToString().Trim(), DBItem.DBDataType.String));
                    updList.Add(new DBItem("dtl_sort", Request[dtl_id_list[i] + "_dtl_sort"].ToString().Trim(), DBItem.DBDataType.Number));
                    updList.Add(new DBItem("dtl_value", Request[dtl_id_list[i] + "_dtl_value"].ToString().Trim(), DBItem.DBDataType.String));
                    updList.Add(new DBItem("dtl_default_value", Request[dtl_id_list[i] + "_dtl_default_value"].ToString().Trim(), DBItem.DBDataType.String));
                    updList.Add(new DBItem("dtl_length", Request[dtl_id_list[i] + "_dtl_length"].ToString().Trim(), DBItem.DBDataType.Number));
                    updList.Add(new DBItem("dtl_help", Request[dtl_id_list[i] + "_dtl_help"].ToString().Trim(), DBItem.DBDataType.String));
                    updList.Add(new DBItem("dtl_rear_word", Request[dtl_id_list[i] + "_dtl_rear_word"].ToString().Trim(), DBItem.DBDataType.String));
                    if (Request[dtl_id_list[i] + "_dtl_must"] != null)
                        updList.Add(new DBItem("dtl_must", "Y", DBItem.DBDataType.String));
                    else
                        updList.Add(new DBItem("dtl_must", "N", DBItem.DBDataType.String));
                    wcAddition = " and dtl_id='" + dtl_id_list[i] + "'";
                    this.link.DBExecUpdate("sys_nadtl", updList, whereCondition + wcAddition);
                }
                message = "儲存成功";
            }
            else
            {
                message = "沒有資料被更新";
            }
            return RedirectToAction("Na_Dtl", new { @na_id = na_id, @tag_id = tag_id, @message = message });
        }

        /// <summary> 儲存護理評估細項_刪除細項 </summary>
        public ActionResult Na_Dtl_Delete()
        {
            try
            {
                string[] dellist = Request["del_id"].ToString().Split(',');
                string whereCondition = " dtl_id in ('" + string.Join("','", dellist) + "') ";
                this.link.DBExecDelete("sys_nadtl", whereCondition);
                //清除掉沒有爸爸的節點
                string sqlstr = "DELETE SYS_NADTL WHERE DTL_ID IN ( SELECT /*+INDEX(SN SYS_NADTL_IDX1)*/ DTL_ID FROM SYS_NADTL SN WHERE DTL_PARENT_ID IS NOT NULL ";
                sqlstr += " AND (SELECT DTL_ID FROM SYS_NADTL WHERE SN.DTL_PARENT_ID = DTL_ID) IS NULL) ";
                this.link.DBExec(sqlstr);
                Response.Write("Y");
            }
            catch (Exception ex)
            {
                Response.Write(ex.Message.ToString());
            }
            return new EmptyResult();
        }

        /// <summary> 儲存護理評估細項_取得細項樹狀物件 </summary>
        private List<NursingAssessmentDtlObj> getDtlObj(ref List<NursingAssessmentDtl> inObj, string refId = "")
        {
            List<NursingAssessmentDtlObj> retObj = new List<NursingAssessmentDtlObj>();
            for (int j = 0; j <= inObj.Count - 1; j++)
            {
                if (inObj[j].dtl_parent_id != refId)
                    continue;

                List<NursingAssessmentDtlObj> subObj = this.getDtlObj(ref inObj, inObj[j].dtl_id);
                if (subObj.Count == 0)
                    subObj = null;
                retObj.Add(new NursingAssessmentDtlObj()
                {
                    na_id = inObj[j].na_id,
                    tag_id = inObj[j].tag_id,
                    dtl_id = inObj[j].dtl_id,
                    dtl_title = inObj[j].dtl_title,
                    dtl_child_hide = inObj[j].dtl_child_hide,
                    dtl_default_value = inObj[j].dtl_default_value,
                    dtl_help = inObj[j].dtl_help,
                    dtl_length = inObj[j].dtl_length,
                    dtl_rear_word = inObj[j].dtl_rear_word,
                    dtl_show_value = inObj[j].dtl_show_value,
                    dtl_sort = inObj[j].dtl_sort,
                    dtl_value = inObj[j].dtl_value,
                    dtl_type = inObj[j].dtl_type,
                    dtl_parent_id = inObj[j].dtl_parent_id,
                    dtl_must = inObj[j].dtl_must,
                    child_obj = subObj
                });
            }
            return retObj;
        }

        /// <summary> 儲存護理評估細項_取得細項ID </summary>
        private string getDtlId()
        {
            string sqlstr = " SELECT 'D'||LPAD(TO_CHAR(TO_NUMBER(REPLACE(DTL_ID,'D','')) + 1),19,'0') AS NEW_DTL_ID FROM SYS_NADTL SN ";
            sqlstr += " WHERE SN.DTL_ID = (SELECT MAX(DTL_ID) FROM SYS_NADTL) ";
            DataTable Dt = link.DBExecSQL(sqlstr);
            if (Dt.Rows.Count > 0)
            {
                for (int d = 0; d < Dt.Rows.Count; d++)
                {
                    sqlstr = Dt.Rows[d]["new_dtl_id"].ToString().Trim();
                }
            }
            else
            {
                sqlstr = "D0000000000000000001";
            }
            return sqlstr;
        }

        /// <summary> 儲存護理評估細項_取得下拉選單 </summary>
        public void getDtlList(ref List<NursingAssessmentDtl> inObj, ref Dictionary<string, string> dtlList, string refId = "", int level_id = 0)
        {
            List<NursingAssessmentDtlObj> retObj = new List<NursingAssessmentDtlObj>();
            for (int j = 0; j <= inObj.Count - 1; j++)
            {
                if (inObj[j].dtl_parent_id != refId)
                    continue;
                string fontSymbol = ">";
                string fontStr = string.Empty;
                for (int c = level_id; c > 0; c--)
                {
                    fontStr += fontSymbol;
                }
                dtlList.Add(inObj[j].dtl_id, fontStr + inObj[j].dtl_title);
                int sub_level_id = level_id + 1;
                this.getDtlList(ref inObj, ref dtlList, inObj[j].dtl_id, sub_level_id);
            }
        }

        #endregion

        #region other_function

        /// <summary>
        /// 設定table的colum
        /// </summary>
        /// <param name="dt">資料表</param>
        /// <param name="clumn">欄位_陣列</param>
        protected DataTable set_dt_column(DataTable dt, string[] clumn)
        {
            for (int i = 0; i < clumn.Length; i++)
            {
                dt.Columns.Add(clumn[i]);
            }
            return dt;
        }

        /// <summary>
        /// 設定欄位的型態
        /// </summary>
        /// <param name="dt">資料表</param>
        /// <param name="datatype">型態_陣列</param>
        protected DataTable set_dt_datatype(DataTable dt, string[] datatype)
        {
            DataRow row_type = dt.NewRow();
            for (int i = 0; i < dt.Columns.Count; i++)
                row_type[i] = datatype[i];

            dt.Rows.Add(row_type);

            return dt;
        }

        #endregion

        #region 公告維護

        /// <summary> 公告維護 </summary>
        public ActionResult Announcement()
        {
            try
            {
                List<AnnouncmentItem> sa_list = new List<AnnouncmentItem>();
                string sqlstr = "select * from nis_sys_announcement where to_date(sa_dateline,'YYYY-MM-DD HH24:MI:SS') > sysdate order by sa_sort asc";
                DataTable Dt = ass_m.DBExecSQL(sqlstr);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        sa_list.Add(new AnnouncmentItem(
                            int.Parse(Dt.Rows[i]["sa_id"].ToString().Trim()),
                            Dt.Rows[i]["sa_desc"].ToString().Trim(),
                            DateTime.Parse(Dt.Rows[i]["sa_dateline"].ToString().Trim()),
                            int.Parse(Dt.Rows[i]["sa_sort"].ToString().Trim())
                            ));
                    }
                }

                ViewData["sa_list"] = sa_list;
                return View();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return View();
            }
        }

        /// <summary> 新增公告 </summary>
        public ActionResult Announcement_Add()
        {
            int effRow = 0;
            try
            {
                DateTime sa_date = Convert.ToDateTime(Request["sa_dateline"].ToString());
                List<DBItem> insList = new List<DBItem>();
                insList.Add(new DBItem("sa_id", getSaId(), DBItem.DBDataType.Number));
                insList.Add(new DBItem("sa_desc", Request["sa_desc"].ToString().Trim(), DBItem.DBDataType.String));
                insList.Add(new DBItem("sa_dateline", sa_date.ToString("yyyy-MM-dd") + " 23:59:59", DBItem.DBDataType.String));
                insList.Add(new DBItem("sa_sort", Request["sa_sort"].ToString().Trim(), DBItem.DBDataType.Number));
                effRow = this.link.DBExecInsert("nis_sys_announcement", insList);
                if (effRow == 1)
                    Response.Write("Y");
            }
            catch (Exception ex)
            {
                Response.Write(ex.Message.ToString());
            }
            return new EmptyResult();
        }

        /// <summary> 刪除公告 </summary>
        public ActionResult Announcement_Del()
        {
            try
            {
                string sa_id = Request["sa_id"];
                int effRow = this.link.DBExecDelete("nis_sys_announcement", "sa_id='" + sa_id + "'");
                if (effRow == 1)
                    Response.Write("Y");
            }
            catch (Exception ex)
            {
                Response.Write(ex.Message.ToString());
            }
            return new EmptyResult();
        }

        /// <summary> 儲存公告 </summary>
        public ActionResult Announcement_Save()
        {
            string message = string.Empty;
            try
            {
                if (Request["sa_id"] != null)
                {
                    string[] saidList = Request["sa_id"].ToString().Split(',');
                    int effRow = 0;
                    for (int i = 0; i <= saidList.Length - 1; i++)
                    {
                        List<DBItem> updList = new List<DBItem>();
                        DateTime sa_date = Convert.ToDateTime(Request[saidList[i] + "_sa_dateline"].ToString());

                        updList.Add(new DBItem("sa_desc", Request[saidList[i] + "_sa_desc"].ToString().Trim(), DBItem.DBDataType.String));
                        updList.Add(new DBItem("sa_dateline", sa_date.ToString("yyyy-MM-dd") + " 23:59:59", DBItem.DBDataType.String));
                        updList.Add(new DBItem("sa_sort", Request[saidList[i] + "_sa_sort"].ToString().Trim(), DBItem.DBDataType.Number));
                        effRow += this.link.DBExecUpdate("nis_sys_announcement", updList, " sa_id = " + saidList[i].ToString());
                    }
                    if (effRow == saidList.Length)
                    {
                        message = "更新成功";
                    }
                    else
                    {
                        message = "更新失敗，請聯絡系統負責人";
                    }
                }
            }
            catch (Exception ex)
            {
                message = ex.Message.ToString();
            }
            return RedirectToAction("Announcement", new { @message = message });
        }

        /// <summary> 取得公告ID </summary>
        private string getSaId()
        {
            try
            {
                string retId = "0";

                DataTable Dt = ass_m.DBExecSQL("SELECT MAX(SA_ID) +1 AS NEW_ID FROM NIS_SYS_ANNOUNCEMENT");
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        if (Dt.Rows[i]["new_id"].ToString().Trim() != "")
                            retId = Dt.Rows[i]["new_id"].ToString().Trim();
                    }
                }
                return retId;
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);

                return ex.ToString();
            }
        }
        #endregion
    }
}
