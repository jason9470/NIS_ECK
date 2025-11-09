using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NIS.Data;
using NIS.Models;
using Newtonsoft.Json;
using NIS.UtilTool;
using NIS.Models.DBModel;
using Com.Mayaminer;

namespace NIS.Controllers
{
    public class NurseCarePlanController : BaseController
    {
        private CareRecord care_record_m;
        private LogTool log = new LogTool();
        private DBConnector link;

        public NurseCarePlanController()
        {
            care_record_m = new CareRecord();
            this.link = new DBConnector();
        }
        //
        // GET: /NurseCarePlan/
        /// <summary> 護理問題 </summary>
        public List<Care_Plan_Item> Sel_Topic(string f_id)
        {
            // 調整護理計畫時，新的編碼不可以與舊的編碼重複，且舊的編碼不可刪除
            List<Care_Plan_Item> temp = null;
            Care_Plan_Item temp_data = null;
            string sql = "select a.*,(select b.DIAGNOSIS_DOMAIN_DESC from NIS_SYS_DIAGNOSIS_DOMAIN b where b.DIAGNOSIS_DOMAIN_CODE = a.DIAGNOSIS_DOMAIN_CODE) as DIAGNOSIS_DOMAIN_DESC from nis_sys_diagnosis a where diagnosis_code = '" + f_id + "'";
            DataTable Dt = this.link.DBExecSQL(sql);
            if (Dt != null && Dt.Rows.Count > 0)
            {
                temp = new List<Care_Plan_Item>();
                foreach (DataRow read in Dt.Rows)
                {
                    temp_data = new Care_Plan_Item();
                    temp_data.DIAGNOSIS_CODE = read["DIAGNOSIS_CODE"].ToString();
                    temp_data.DIAGNOSIS_NAME = read["DIAGNOSIS_NAME"].ToString();
                    temp_data.DIAGNOSIS_DOMAIN_CODE = read["DIAGNOSIS_DOMAIN_CODE"].ToString();
                    temp_data.DIAGNOSIS_DOMAIN_DESC = read["DIAGNOSIS_DOMAIN_DESC"].ToString();
                    temp.Add(temp_data);
                }
            }
            return temp;
        }

        /// <summary> 定義性特徵 </summary>
        public List<Care_Plan_Item> Sel_Defin(string f_id)
        {
            List<Care_Plan_Item> temp = null;
            Care_Plan_Item temp_data = null;
            string sql = "select * from nis_sys_diagnosis_feature where diagnosis_code ='" + f_id + "' and disable_date is null ";

            DataTable Dt = this.link.DBExecSQL(sql);
            if (Dt != null && Dt.Rows.Count > 0)
            {
                temp = new List<Care_Plan_Item>();
                foreach (DataRow read in Dt.Rows)
                {
                    temp_data = new Care_Plan_Item();
                    temp_data.DIAGNOSIS_CODE = read["DIAGNOSIS_CODE"].ToString();
                    temp_data.FEATURE_CODE = read["FEATURE_CODE"].ToString();
                    temp_data.FEATURE_DESC = read["FEATURE_DESC"].ToString();
                    temp.Add(temp_data);
                }
            }

            return temp;
        }

        /// <summary> 相關因素 </summary>
        public List<Care_Plan_Item> Sel_About(string f_id)
        {
            List<Care_Plan_Item> temp = null;
            Care_Plan_Item temp_data = null;
            string sql = "select * from nis_sys_diagnosis_inducements where diagnosis_code ='" + f_id + "' and disable_date is null ";
            DataTable Dt = this.link.DBExecSQL(sql);
            if (Dt != null && Dt.Rows.Count > 0)
            {
                temp = new List<Care_Plan_Item>();
                foreach (DataRow read in Dt.Rows)
                {
                    temp_data = new Care_Plan_Item();
                    temp_data.DIAGNOSIS_CODE = read["DIAGNOSIS_CODE"].ToString();
                    temp_data.INDUCEMENTS_CODE = read["INDUCEMENTS_CODE"].ToString();
                    temp_data.INDUCEMENTS_DESC = read["INDUCEMENTS_DESC"].ToString();
                    temp.Add(temp_data);
                }
            }


            return temp;
        }

        /// <summary> 目標 </summary>
        public List<Care_Plan_Item> Sel_Goal(string f_id)
        {
            List<Care_Plan_Item> temp = null;
            Care_Plan_Item temp_data = null;
            string sql = "select * from nis_sys_diagnosis_target where diagnosis_code ='" + f_id + "' and disable_date is null ";

            DataTable Dt = this.link.DBExecSQL(sql);
            if (Dt != null && Dt.Rows.Count > 0)
            {
                temp = new List<Care_Plan_Item>();
                foreach (DataRow read in Dt.Rows)
                {
                    temp_data = new Care_Plan_Item();
                    temp_data.DIAGNOSIS_CODE = read["DIAGNOSIS_CODE"].ToString();
                    temp_data.TARGET_CODE = read["TARGET_CODE"].ToString();
                    temp_data.TARGET_DESC = read["TARGET_DESC"].ToString();
                    temp.Add(temp_data);
                }
            }

            return temp;
        }

        /// <summary> 目標-活動 </summary>
        public List<Care_Plan_Item> Sel_Active(string f_id)
        {
            List<Care_Plan_Item> temp = null;
            Care_Plan_Item temp_data = null;
            string sql = "select * from nis_sys_diagnosis_measure where diagnosis_code ='" + f_id + "' and disable_date is null ";

            DataTable Dt = this.link.DBExecSQL(sql);
            if (Dt != null && Dt.Rows.Count > 0)
            {
                temp = new List<Care_Plan_Item>();
                foreach (DataRow read in Dt.Rows)
                {
                    temp_data = new Care_Plan_Item();
                    //temp_data.S_ID = read["s_id"].ToString();
                    temp_data.DIAGNOSIS_CODE = read["DIAGNOSIS_CODE"].ToString();
                    temp_data.MEASURE_CODE = read["MEASURE_CODE"].ToString();
                    temp_data.MEASURE_DESC = read["MEASURE_DESC"].ToString();
                    temp.Add(temp_data);
                }
            }
            return temp;
        }
        public ActionResult AllList(string rb_mode = "rb_diagnosis")
        {
            if (Session["PatInfo"] != null)
            {
                string feeno = ptinfo.FeeNo;
                ViewBag.RootDocument = GetSourceUrl();
                ViewBag.feeno = feeno;
                ViewBag.rb_mode = rb_mode;
                ViewBag.userno = userinfo.EmployeesNo;
                ViewBag.Category = userinfo.Category.ToString();
                ViewBag.ck_list = get_cklist(feeno);

                //DateTime start = Convert.ToDateTime(form["start_date"] + " " + form["start_time"]);
                //DateTime end = Convert.ToDateTime(form["end_date"] + " " + form["end_time"]);
                //ViewBag.start_date = start;
                //ViewBag.end_date = end;

                return View();
            }

            Response.Write("<script>alert('請重新選擇病患');</script>");
            return new EmptyResult();
        }
        public ActionResult AllList_PDF(string feeno, string rb_mode = "rb_diagnosis", string s_datetime = "", string e_datetime = "", string t_diagnosis = "")
        {
            ViewBag.RootDocument = GetSourceUrl();
            ViewBag.feeno = feeno;
            ViewBag.rb_mode = rb_mode;
                ViewBag.s_datetime = s_datetime;
                ViewBag.e_datetime = e_datetime;
                ViewBag.t_diagnosis = t_diagnosis;
            PatientInfo pinfo = new PatientInfo();
            byte[] ByteCode = webService.GetPatientInfo(feeno);
            //病人資訊
            if (ByteCode != null)
                pinfo = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(ByteCode));

            ViewData["ptinfo"] = pinfo;

            //DateTime start = Convert.ToDateTime(form["start_date"] + " " + form["start_time"]);
            //DateTime end = Convert.ToDateTime(form["end_date"] + " " + form["end_time"]);
            //ViewBag.start_date = start;
            //ViewBag.end_date = end;

            return View();

        }
        public DataTable get_cklist(string feeno)
        {
            DataTable dt = new DataTable();
            string sql = " select DISTINCT TOPICDESC from CAREPLANMASTER WHERE feeno = '" + feeno + "' ORDER BY TOPICDESC ";
            link.DBExecSQL(sql, ref dt);

            return dt;
        }
        public JsonResult AllList_Search(string feeno = "",string rb_mode = "rb_diagnosis", string t_diagnosis = "", string s_datetime = "", string e_datetime = "")
        {
            if (Session["PatInfo"] != null)
            {
                feeno = ptinfo.FeeNo;
            }
            DataTable dt = get_cp_record(feeno, rb_mode, t_diagnosis, s_datetime, e_datetime);
                List<DBModel.CAREPLAN> CarePlanList = new List<DBModel.CAREPLAN>();
                if (dt != null && dt.Rows.Count > 0)
                {
                    CarePlanList = (List<DBModel.CAREPLAN>)dt.ToList<DBModel.CAREPLAN>();
                    foreach (DBModel.CAREPLAN dnv in CarePlanList)
                    {
                        UserInfo USERINFO_LIST = null;
                        string RECORDER_UNIT = "";
                        string RECORDER_NAME = "";
                        string MODIFY_UNIT = "";
                        string MODIFY_NAME = "";
                        string ASSESS_UNIT = "";
                        string ASSESS_NAME = "";
                        byte[] listByteCode = null;
                        if (!string.IsNullOrEmpty(dnv.RECORDER))
                        {
                            listByteCode = webService.UserName(dnv.RECORDER);
                            if (listByteCode != null)
                            {
                                string listJsonArray = CompressTool.DecompressString(listByteCode);
                                USERINFO_LIST = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                                RECORDER_UNIT = USERINFO_LIST.CostCenterName.Trim();
                                RECORDER_NAME = USERINFO_LIST.EmployeesName.Trim();
                            }
                        }
                        if (!string.IsNullOrEmpty(dnv.MODIFY_ID))
                        {
                            listByteCode = webService.UserName(dnv.MODIFY_ID);
                            if (listByteCode != null)
                            {
                                string listJsonArray = CompressTool.DecompressString(listByteCode);
                                USERINFO_LIST = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                                MODIFY_UNIT = USERINFO_LIST.CostCenterName.Trim();
                                MODIFY_NAME = USERINFO_LIST.EmployeesName.Trim();
                            }
                        }
                        if (!string.IsNullOrEmpty(dnv.ASSESS_ID))
                        {
                            listByteCode = webService.UserName(dnv.ASSESS_ID);
                            if (listByteCode != null)
                            {
                                string listJsonArray = CompressTool.DecompressString(listByteCode);
                                USERINFO_LIST = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                                ASSESS_UNIT = USERINFO_LIST.CostCenterName.Trim();
                                ASSESS_NAME = USERINFO_LIST.EmployeesName.Trim();
                            }
                        }

                        dnv.PLANSTARTDATE = (!string.IsNullOrEmpty(dnv.PLANSTARTDATE)) ? Convert.ToDateTime(dnv.PLANSTARTDATE).ToString("yyyy/MM/dd") : "";
                        dnv.PLANSTARTDATE = (!string.IsNullOrEmpty(dnv.PLANSTARTDATE)) ? Convert.ToDateTime(dnv.PLANSTARTDATE).ToString("yyyy/MM/dd") : "";
                        dnv.PLANENDDATE = (!string.IsNullOrEmpty(dnv.PLANENDDATE)) ? Convert.ToDateTime(dnv.PLANENDDATE).ToString("yyyy/MM/dd") : "";
                        if (!string.IsNullOrEmpty(dnv.MODIFT_DATE))
                        {
                            dnv.MODIFT_DATE = Convert.ToDateTime(dnv.MODIFT_DATE).ToString("yyyy/MM/dd");
                            dnv.MODIFT_bool = true;
                        }
                        else
                        {
                            dnv.MODIFT_DATE ="";
                            dnv.MODIFT_bool = false;
                        }
                        if (!string.IsNullOrEmpty(dnv.ASSESS_DATE))
                        {
                            dnv.ASSESS_DATE = Convert.ToDateTime(dnv.ASSESS_DATE).ToString("yyyy/MM/dd");
                            dnv.ASSESS_bool = true;
                        }
                        else
                        {
                            dnv.ASSESS_DATE = "";
                            dnv.ASSESS_bool = false;
                        }
                        dnv.RECORDER_UNIT = RECORDER_UNIT;
                        dnv.RECORDER_NAME = RECORDER_NAME;
                        dnv.MODIFY_UNIT = MODIFY_UNIT;
                        dnv.MODIFY_NAME = MODIFY_NAME;
                        dnv.ASSESS_UNIT = ASSESS_UNIT;
                        dnv.ASSESS_NAME = ASSESS_NAME;
                    }
                }

                //dt = this.care_record_m.getRecorderName(dt);

                return new JsonResult { Data = CarePlanList };
            

        }
        public DataTable get_cp_record(string feeno, string rb_mode = "", string t_diagnosis = "", string s_datetime = "", string e_datetime = "")
        {
            DataTable dt = new DataTable();
            string sql = "select * from CAREPLANMASTER WHERE feeno = '" + feeno + "' ";
            switch (rb_mode)
            {
                case "rb_diagnosis":
                    sql += (!string.IsNullOrEmpty(t_diagnosis) && t_diagnosis !="all") ? " AND TOPICDESC='" + t_diagnosis + "' " : "";
                    break;
                case "rb_timerange":
                    sql += " AND PLANSTARTDATE BETWEEN to_date( '" + s_datetime + "00:00:00', 'yyyy/mm/dd hh24:mi:ss' )"; //2018/08/29 07:01:00
                    sql += " AND to_date('" + e_datetime + "23:59:59', 'yyyy/mm/dd hh24:mi:ss' ) ";
                    break;
                case "rb_unfinished":
                    sql += " AND PLANENDDATE is null ";
                    break;
                default:
                    break;
            }
            sql += " order by PNO asc";
            link.DBExecSQL(sql, ref dt);
            return dt;
        }

        public ActionResult List(string rb_mode = "timerange", string s_day = "", string s_time = "", string e_day = "", string e_time = "")
        {
            if (Session["PatInfo"] != null)
            {
                string feeno = ptinfo.FeeNo;
                DateTime now = DateTime.Now;
                string s_datetime = "";
                string e_datetime = "";
                if (string.IsNullOrEmpty(s_day) && string.IsNullOrEmpty(e_day))
                {
                    s_datetime = now.AddDays(-6).ToString("yyyy/MM/dd 00:00:00");
                    e_datetime = now.ToString("yyyy/MM/dd HH:mm:ss");
                }
                else
                {
                    s_datetime = s_day + " " + s_time;
                    e_datetime = e_day + " " + e_time;
                }
                DataTable dt = care_record_m.get_cp_record(feeno, rb_mode, s_datetime, e_datetime);
                dt = this.care_record_m.getRecorderName(dt);
                ViewBag.dt = dt;
                ViewBag.RootDocument = GetSourceUrl();
                ViewBag.feeno = feeno;
                ViewBag.rb_mode = rb_mode;
                ViewBag.userno = userinfo.EmployeesNo;
                ViewBag.Category = userinfo.Category.ToString();

                ViewBag.start_date = s_datetime;
                ViewBag.end_date = e_datetime;

                return View();
            }

            Response.Write("<script>alert('請重新選擇病患');</script>");
            return new EmptyResult();
        }
        //列印
        public ActionResult List_PDF(string feeno, string rb_mode = "", string s_day = "", string s_time = "", string e_day = "", string e_time = "")
        {
            PatientInfo pinfo = new PatientInfo();
            byte[] ByteCode = webService.GetPatientInfo(feeno);
            string s_datetime = s_day + " " + s_time;
            string e_datetime = e_day + " " + e_time;

            //病人資訊
            if (ByteCode != null)
                pinfo = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(ByteCode));
            ViewData["ptinfo"] = pinfo;
            DataTable dt = care_record_m.get_cp_record(feeno, rb_mode, s_datetime, e_datetime);
            dt = this.care_record_m.getRecorderName(dt);
            ViewBag.dt = dt;

            return View();
        }

        #region --新版護理計畫--


        public ActionResult Index()
        {//判斷有無病人session
            if (Session["PatInfo"] != null)
            {
                PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
                string feeno = ptInfo.FeeNo;
                Set_Index(feeno);
            }
            else
            {
                Response.Write("<script>alert('請重新選擇病患');</script>");
                return new EmptyResult();
            }
            return View();
        }

        public bool Discharged(string feeno)
        {
            string sqlstr = "";
            sqlstr += "SELECT * FROM NIS_SPECIAL_EVENT_DATA WHERE TYPE_ID ='6' AND FEENO='" + feeno + "'";
            DataTable Dt = link.DBExecSQL(sqlstr);
            if (Dt.Rows.Count > 0)
            {
                    return true;
            }
            return false;
        }      
        //顯示所有護理計畫
        public ActionResult AllCarePlan()
        {
            PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
            string feeno = ptInfo.FeeNo;
            Get_AllPlan(feeno);
            ViewBag.start = (DateTime.Now.AddDays(-4) < ptinfo.InDate) ? ptinfo.InDate : DateTime.Now.AddDays(-1);
            ViewBag.end = DateTime.Now.ToString("yyyy/MM/dd");
            ViewBag.Discharged = Discharged(feeno);//是否出院
            return View();
        }
        public JsonResult HistoryData(string recordid,string targetid, string start_date = "", string end_date = "")
        {
            string LoginUserID = userinfo.EmployeesNo;
            string sqlstr = string.Format("select * FROM CAREPLAN_HISTORY WHERE RECORDID = '{0}' AND TARGETID = '{1}' AND ASSESS_DATE "
                + "BETWEEN TO_DATE( '{2} 00:00:00', 'yyyy/mm/dd hh24:mi:ss' ) AND TO_DATE('{3} 23:59:59', 'yyyy/mm/dd hh24:mi:ss') AND DELETED is null ORDER BY ASSESS_DATE DESC"
                , recordid
                , targetid
                , start_date
                , end_date);
            List<CarePlan_History> CPHistory = new List<CarePlan_History>();
            bool Dt_bool = false;
            DataTable Dt = link.DBExecSQL(sqlstr);
            if (Dt.Rows.Count > 0)
            {
                Dt_bool = true;
                CPHistory = (List<CarePlan_History>)Dt.ToList<CarePlan_History>();
            }
            return Json(new { LoginUserID, Dt_bool, CPHistory });
        }

        public JsonResult HistoryEdit(string His_id, string recordid = "", string targetid = "", string reason = "")
        {
            List<CarePlan_History> CPHistory_List = new List<CarePlan_History>();
            CarePlan_History CPHistory = new CarePlan_History();
            ReturnJsonData ReturnList = new ReturnJsonData();
            DateTime Nowdate = DateTime.Now;
            int erow = 0;
            string CARERECORDID = string.Empty;
            string PK_ID = string.Empty;
            string Where = string.Empty;
            string sqlstr = string.Format("select * FROM CAREPLAN_HISTORY WHERE PK_ID = '{0}' "
                        , His_id);
            DataTable Dt = link.DBExecSQL(sqlstr);
            if (Dt.Rows.Count > 0)
            {
                foreach (DataRow read in Dt.Rows)
                {
                    CPHistory = new CarePlan_History();
                    CPHistory.PK_ID = read["PK_ID"].ToString();
                    CPHistory.RECORDID = read["RECORDID"].ToString();
                    CPHistory.TARGETSTATUS = read["TARGETSTATUS"].ToString();
                    CPHistory.REASON = read["REASON"].ToString();
                    CPHistory.ASSESS_ID = read["ASSESS_ID"].ToString();
                    CPHistory.ASSESS_NAME = read["ASSESS_NAME"].ToString();
                    CPHistory.ASSESS_DATE = Convert.ToDateTime(read["ASSESS_DATE"].ToString());
                    CPHistory.CPFEATUREID_OBJ = read["CPFEATUREID_OBJ"].ToString();
                    CPHistory.CPRF_OBJ = read["CPRF_OBJ"].ToString();
                    CPHistory.CPMEASURE_OBJ = read["CPMEASURE_OBJ"].ToString();
                    CPHistory.CARERECORDID = read["CARERECORDID"].ToString();
                    CPHistory.TARGETID = read["TARGETID"].ToString();
                    CPHistory.DELETED = read["DELETED"].ToString();
                }
            }
            CARERECORDID = CPHistory.CARERECORDID;
            PK_ID = CPHistory.PK_ID;

            sqlstr = string.Format("SELECT PK_ID FROM (SELECT * FROM CAREPLAN_HISTORY WHERE RECORDID ='{0}' AND TARGETID ='{1}' ORDER BY ASSESS_DATE DESC) WHERE ROWNUM =1"
                , recordid
                , targetid);
            Dt = link.DBExecSQL(sqlstr);
            string First_PKID = ""; //最新一筆資料
            foreach (DataRow read in Dt.Rows)
            {
                First_PKID = read["PK_ID"].ToString();
            }

            sqlstr = "select TARGETDESC from cptargetdtl where RECORDID='"+ recordid + "' AND TARGETID ='"+ targetid + "'";
            Dt = link.DBExecSQL(sqlstr);
            string TARGETDESC = "";
            foreach (DataRow read in Dt.Rows)
            {
                TARGETDESC = read["TARGETDESC"].ToString();
            }
           

            List<DBItem> UpdList = new List<DBItem>();
            UpdList.Add(new DBItem("REASON", reason, DBItem.DBDataType.String));

            if (First_PKID == PK_ID)
            {
                Where = " recordid = '" + recordid + "' and TARGETID = '" + targetid + "' ";
                erow = this.link.DBExecUpdate("CPTARGETDTL", UpdList, Where);
            }

            UpdList.Add(new DBItem("TARGETID", targetid, DBItem.DBDataType.String));
            UpdList.Add(new DBItem("CARERECORDID", CARERECORDID, DBItem.DBDataType.String));

            Where = " PK_ID = '" + CPHistory.PK_ID + "' ";
            erow = this.link.DBExecUpdate("CAREPLAN_HISTORY", UpdList, Where);

            string Date = CPHistory.ASSESS_DATE.ToString("yyyy/MM/dd HH:mm:ss");
            string Content = "評值 " + TARGETDESC + "，目標未達成-" + reason + "，繼續執行原建立之照護計畫。";
            erow = Upd_CareRecord(Date, CARERECORDID, "", "", "", "", "", Content, "CAREPLANMASTER","", "HistoryEdit");

            return new JsonResult { Data = CPHistory };
        }

        private void Set_Index(string feeno)//CaseNO改成id
        {
            ViewBag.Dt_Master = this.Sel_User_Master(feeno);
            ViewData["Dt_Deftin"] = this.Sel_User_Defin(feeno);
            ViewData["Dt_About"] = this.Sel_User_About(feeno);
            ViewData["Dt_Goal"] = this.Sel_User_Goal(feeno);
            ViewData["Dt_Active"] = this.Sel_User_Active(feeno);
            ViewBag.ReplaceFirst = new Func<string, string, string, string>(ReplaceFirst);
        }

        private void Get_AllPlan(string feeno)//CaseNO改成id
        {
            ViewBag.Dt_Master = this.Sel_User_Master(feeno);
            ViewData["Dt_Deftin"] = this.Get_All_Defin(feeno);
            ViewData["Dt_About"] = this.Get_All_About(feeno);
            ViewData["Dt_Goal"] = this.Get_All_Goal(feeno);
            ViewData["Dt_Active"] = this.Get_All_Active(feeno);
            ViewBag.ReplaceFirst = new Func<string, string, string, string>(ReplaceFirst);
        }
        /// <summary> 取代第一個 </summary>
        private string ReplaceFirst(string AllWord, string OldWord, string NewWord)
        {
            if (AllWord.Length > 0)
            {
                int Index = AllWord.IndexOf(OldWord) + OldWord.Length;
                AllWord = AllWord.Substring(0, Index).Replace(OldWord, NewWord) + AllWord.Substring(Index, AllWord.Length - Index);
            }
            return AllWord;
        }

        #region --目標評值--
        //目標評值
        public JsonResult Score_Goal(FormCollection form, string recordid, string targetid ,string type="")
        {

            string Date = form["CreateDate_" + recordid] + " " + form["CreateTime_" + recordid] + ":" + DateTime.Now.ToUniversalTime().AddHours(8).ToString("ss")
                , Score = form[recordid + "CPRESULT" + targetid].ToString()
                , Content = "評值 " + form[recordid + "TARGET" + targetid] + "，";
            string reason = string.Empty;
            if (form[recordid + "CPRESULT" + targetid].ToString() == "N")
            {
                reason = form[recordid + "c_options" + targetid].ToString();
            }

            ReturnJsonData ReturnList = new ReturnJsonData();
            List<DBItem> UpdList = new List<DBItem>();
            UpdList.Add(new DBItem("TARGETSTATUS", Score, DBItem.DBDataType.String));
            UpdList.Add(new DBItem("REASON", reason, DBItem.DBDataType.String));
            UpdList.Add(new DBItem("ASSESS_DATE", Date, DBItem.DBDataType.DataTime));
            UpdList.Add(new DBItem("ASSESS_ID", userinfo.EmployeesNo, DBItem.DBDataType.String));
            if (Score != "N")
            {
                UpdList.Add(new DBItem("TARGETENDDATE", Date, DBItem.DBDataType.DataTime));
            }
            else
            {
                if (reason != "修改護理措施" && reason != "繼續執行")
                {
                    UpdList.Add(new DBItem("TARGETENDDATE", Date, DBItem.DBDataType.DataTime));
                }
                else
                {
                    UpdList.Add(new DBItem("TARGETENDDATE", "", DBItem.DBDataType.DataTime));
                }
            }
            string Where = " recordid = '" + recordid + "' and TARGETID = '" + targetid + "' ";
            int erow = this.link.DBExecUpdate("CPTARGETDTL", UpdList, Where);
            if (erow > 0)
            {
                ReturnList.Status = "評值成功";
                if (Score == "Y")
                    Content += "目標已達成結束原建立之照護計畫。";
                else if (Score == "C")
                    Content += "目標因不適用結束原建立之照護計畫。";
                else if (Score == "N")
                {
                    if (reason == "修改護理措施" || reason == "繼續執行")
                        Content += "目標未達成-" + reason + "，繼續執行原建立之照護計畫。";
                    else
                        Content += "目標未達成-" + reason + "，結束原建立之照護計畫。";
                }
                //新增至歷程
                UpdList.Clear();
                DateTime Nowdate = DateTime.Now;
                string PK_ID = "History_" + Nowdate.ToString("yyyyMMddHHmmssfff");
                UpdList.Add(new DBItem("RECORDID", recordid, DBItem.DBDataType.String));
                UpdList.Add(new DBItem("TARGETSTATUS", Score, DBItem.DBDataType.String));
                UpdList.Add(new DBItem("REASON", reason, DBItem.DBDataType.String));
                UpdList.Add(new DBItem("ASSESS_ID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                UpdList.Add(new DBItem("ASSESS_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                if (string.IsNullOrEmpty(type))
                {
                    UpdList.Add(new DBItem("ASSESS_DATE", Date, DBItem.DBDataType.DataTime));
                }

                string id_sql = "SELECT SERIAL,'CPFEATUREDTL' SOURCE_TAG FROM CPFEATUREDTL WHERE RECORDID='" + recordid + "' and FEATUREENDDATE is null union ";
                id_sql += "SELECT SERIAL,'CPRFDTL' SOURCE_TAG FROM CPRFDTL WHERE RECORDID='" + recordid + "' and RELATEDFACTORSENDDATE is null union ";
                id_sql += "SELECT SERIAL,'CPMEASUREDTL' SOURCE_TAG FROM CPMEASUREDTL WHERE RECORDID='" + recordid + "' and MEASUREENDDATE is null";
                DataTable id_Dt = this.link.DBExecSQL(id_sql);
                List<ID_List> dttolist = new List<ID_List>();
                string jsonCPF = ""; string jsonCPR = ""; string jsonCPM = "";
                if (id_Dt.Rows.Count > 0)
                {
                    dttolist = (List<ID_List>)id_Dt.ToList<ID_List>();
                    dttolist = dttolist.FindAll(x => x.SOURCE_TAG == "CPFEATUREDTL").ToList();
                    List<string> JsonList = new List<string>();
                    foreach (var item in dttolist)
                    {
                        JsonList.Add(item.SERIAL);
                    }
                    jsonCPF = JsonConvert.SerializeObject(JsonList);

                    dttolist = (List<ID_List>)id_Dt.ToList<ID_List>();
                    dttolist = dttolist.FindAll(x => x.SOURCE_TAG == "CPRFDTL").ToList();
                    JsonList.Clear();
                    foreach (var item in dttolist)
                    {
                        JsonList.Add(item.SERIAL);
                    }
                    jsonCPR = JsonConvert.SerializeObject(JsonList);

                    dttolist = (List<ID_List>)id_Dt.ToList<ID_List>();
                    dttolist = dttolist.FindAll(x => x.SOURCE_TAG == "CPMEASUREDTL").ToList();
                    JsonList.Clear();
                    foreach (var item in dttolist)
                    {
                        JsonList.Add(item.SERIAL);
                    }
                    jsonCPM = JsonConvert.SerializeObject(JsonList);

                }
                if (type!= "Restart")
                {
                    UpdList.Add(new DBItem("CPFEATUREID_OBJ", jsonCPF, DBItem.DBDataType.String));
                    UpdList.Add(new DBItem("CPRF_OBJ", jsonCPR, DBItem.DBDataType.String));
                    UpdList.Add(new DBItem("CPMEASURE_OBJ", jsonCPM, DBItem.DBDataType.String));
                }
                UpdList.Add(new DBItem("TARGETID", targetid, DBItem.DBDataType.String));

                string CARERECORDID = "", RECORDID = "", TARGETID = "";
                List<string> CPFEATUREID_OBJ = new List<string>();
                List<string> CPRF_OBJ = new List<string>();
                List<string> CPMEASURE_OBJ = new List<string>();
                if (!string.IsNullOrEmpty(type))
                {
                    string sqlstr = "SELECT * FROM (SELECT * FROM CAREPLAN_HISTORY WHERE RECORDID ='"
                        + recordid + "' AND TARGETID ='" + targetid + "' ORDER BY ASSESS_DATE DESC) WHERE ROWNUM =1";
                    DataTable PKID_DT = this.link.DBExecSQL(sqlstr);
                    if (PKID_DT != null && PKID_DT.Rows.Count > 0)
                    {
                        foreach (DataRow read in PKID_DT.Rows)
                        {
                            CARERECORDID = read["CARERECORDID"].ToString();
                            Date = String.Format("{0:yyyy/MM/dd HH:mm:ss}", read["ASSESS_DATE"]);
                            PK_ID = read["PK_ID"].ToString();
                            CPFEATUREID_OBJ = JsonConvert.DeserializeObject<List<string>>(read["CPFEATUREID_OBJ"].ToString());
                            CPRF_OBJ = JsonConvert.DeserializeObject<List<string>>(read["CPRF_OBJ"].ToString());
                            CPMEASURE_OBJ = JsonConvert.DeserializeObject<List<string>>(read["CPMEASURE_OBJ"].ToString());
                            RECORDID = read["RECORDID"].ToString();
                            TARGETID = read["TARGETID"].ToString();
                        }
                    }
                }
                else
                {
                    CARERECORDID = "AssessCP" + recordid + Nowdate.ToString("yyyyMMddHHmmssfff"); //護理紀錄ID
                }
                UpdList.Add(new DBItem("CARERECORDID", CARERECORDID, DBItem.DBDataType.String));
                ReturnList.ReturnData = "History_" + Nowdate.ToString("yyyyMMddHHmmssfff");

                if (!string.IsNullOrEmpty(type))
                {
                    Where = " PK_ID = '" + PK_ID + "' ";
                    erow = this.link.DBExecUpdate("CAREPLAN_HISTORY", UpdList, Where);
                    Upd_CareRecord(Date, CARERECORDID, "#" + form["PNO"] + " " + form["Topic"], "", "", "", "", Content, "CAREPLANMASTER", ReturnList.ReturnData);

                }
                else
                {
                    UpdList.Add(new DBItem("PK_ID", PK_ID, DBItem.DBDataType.String));
                    erow = this.link.DBExecInsert("CAREPLAN_HISTORY", UpdList);
                    Insert_CareRecord(Date, CARERECORDID, "#" + form["PNO"] + " " + form["Topic"], "", "", "", "", Content, "CAREPLANMASTER", ReturnList.ReturnData);
                }


                //Insert_CareRecord(Date, "AssessCP" + recordid + DateTime.Now.ToString("yyyyMMddHHmmssfff"), "#" + form["PNO"] + " " + form["Topic"], "", "", "", "", Content, "CAREPLANMASTER");
                string sql = "SELECT * FROM cptargetdtl where recordid ='" + recordid + "' and TARGETENDDATE is null";
                DataTable Dt = this.link.DBExecSQL(sql);
                string Date_EndDate = "";
                if (Dt.Rows.Count == 0)
                {
                    Date_EndDate = Date;
                }
                string CPMASTER_Where = "",CPFEATUREDTL_Where = "",CPRFDTL_Where = "",CPMEASUREDTL_Where = "";
                if (!string.IsNullOrEmpty(type))
                {
                    if (Score == "N"&&(reason == "修改護理措施" || reason == "繼續執行"))
                    {
                        Date_EndDate = "";
                    }

                    CPMASTER_Where = " recordid = '" + recordid + "' ";
                    if (CPFEATUREID_OBJ != null)
                    {
                        CPFEATUREDTL_Where = " SERIAL in ('" + string.Join("','", CPFEATUREID_OBJ) + "') ";
                    }
                    if (CPRF_OBJ!=null)
                    {
                        CPRFDTL_Where = " SERIAL in ('" + string.Join("','", CPRF_OBJ) + "') ";
                    }
                        //CPTARGETDTL_Where = " recordid = '" + recordid + "' and TARGETID = '" + TARGETID + "'";
                    if (CPMEASURE_OBJ != null)
                    {
                        CPMEASUREDTL_Where = " SERIAL in ('" + string.Join("','", CPMEASURE_OBJ) + "')";
                    }
                }
                else
                {
                    CPMASTER_Where = " recordid = '" + recordid + "' ";
                    CPFEATUREDTL_Where = " recordid = '" + recordid + "' and FEATUREENDDATE is null";
                    CPRFDTL_Where = " recordid = '" + recordid + "' and RELATEDFACTORSENDDATE is null";
                    //CPTARGETDTL_Where = " recordid = '" + recordid + "' and TARGETID = '" + TARGETID + "' and TARGETENDDATE is null";
                    CPMEASUREDTL_Where = " recordid = '" + recordid + "' and MEASUREENDDATE is null";
                }

                UpdList.Clear();
                UpdList.Add(new DBItem("PLANENDDATE", Date_EndDate, DBItem.DBDataType.DataTime));
                UpdList.Add(new DBItem("ASSESS_DATE", Date, DBItem.DBDataType.DataTime));
                UpdList.Add(new DBItem("ASSESS_ID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                //UpdList.Add(new DBItem("MODIFY_ID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                //UpdList.Add(new DBItem("MODIFT_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                erow = this.link.DBExecUpdate("CAREPLANMASTER", UpdList, CPMASTER_Where);
                //20160621 將所有項目加上結束日期

                if (!string.IsNullOrEmpty(CPFEATUREDTL_Where))
                {
                    UpdList.Clear();
                    UpdList.Add(new DBItem("FEATUREENDDATE", Date_EndDate, DBItem.DBDataType.DataTime));
                    erow = this.link.DBExecUpdate("CPFEATUREDTL", UpdList, CPFEATUREDTL_Where);
                }
                if (!string.IsNullOrEmpty(CPRFDTL_Where))
                {
                    UpdList.Clear();
                    UpdList.Add(new DBItem("RELATEDFACTORSENDDATE", Date_EndDate, DBItem.DBDataType.DataTime));
                    erow = this.link.DBExecUpdate("CPRFDTL", UpdList, CPRFDTL_Where);
                }
                //if (!string.IsNullOrEmpty(CPTARGETDTL_Where))
                //{
                //    UpdList.Clear();
                //    UpdList.Add(new DBItem("TARGETENDDATE", Date_EndDate, DBItem.DBDataType.DataTime));
                //    erow = this.link.DBExecUpdate("CPTARGETDTL", UpdList, CPTARGETDTL_Where);
                //}
                if (!string.IsNullOrEmpty(CPMEASUREDTL_Where))
                {
                    UpdList.Clear();
                    UpdList.Add(new DBItem("MEASUREENDDATE", Date_EndDate, DBItem.DBDataType.DataTime));
                    erow = this.link.DBExecUpdate("CPMEASUREDTL", UpdList, CPMEASUREDTL_Where);
                }

            }
            else
            {
                ReturnList.Status = "儲存失敗";
            }

            return new JsonResult { Data = ReturnList.Status };
        }

        //執行活動
        public EmptyResult Execute_Act(FormCollection form, string recordid)
        {
            //string Content = "依 " + form["Topic"] + " 健康問題之照護計畫予 ";
            string Content = string.Empty;
            foreach (string item in form["activity" + recordid].Split(','))
                Content += item + "、";
            Content = Content.Substring(0, Content.Length - 1);
            string Date = form["CreateDate_" + recordid] + " " + form["CreateTime_" + recordid] + ":" + DateTime.Now.ToUniversalTime().AddHours(8).ToString("ss");

            int erow = Insert_CareRecord_Black(Date, "ExecuteMEA" + recordid + DateTime.Now.ToString("yyyyMMddHHmmss"), "#" + form["PNO"] + " " + form["Topic"], "", "", "", Content + "。", "", "CAREPLANMASTER");
            if (erow > 0)
                Response.Write("執行成功");
            else
                Response.Write("儲存失敗");

            return new EmptyResult();
        }
        #endregion

        #endregion


        #region --目標評值刪除--
        //目標評值
        public JsonResult Score_Del(FormCollection form, string recordid, string targetid)
        {
            string ReturnString = "", json_log="";
            string id_sql = "SELECT * FROM (SELECT * FROM CAREPLAN_HISTORY WHERE RECORDID='" + recordid + "' AND TARGETID ='" + targetid + "' AND DELETED is null ORDER BY ASSESS_DATE DESC) WHERE ROWNUM = 1";
            DataTable id_Dt = this.link.DBExecSQL(id_sql);
            CarePlan_History CPHistory = new CarePlan_History();
            if (id_Dt != null && id_Dt.Rows.Count > 0)
            {
                foreach (DataRow read in id_Dt.Rows)
                {
                    CPHistory.PK_ID = read["PK_ID"].ToString();
                    CPHistory.RECORDID = read["RECORDID"].ToString();
                    CPHistory.TARGETSTATUS = read["TARGETSTATUS"].ToString();
                    CPHistory.REASON = read["REASON"].ToString();
                    CPHistory.ASSESS_ID = read["ASSESS_ID"].ToString();
                    CPHistory.ASSESS_NAME = read["ASSESS_NAME"].ToString();
                    CPHistory.ASSESS_DATE = Convert.ToDateTime(read["ASSESS_DATE"].ToString());
                    CPHistory.CPFEATUREID_OBJ = read["CPFEATUREID_OBJ"].ToString();
                    CPHistory.CPRF_OBJ = read["CPRF_OBJ"].ToString();
                    CPHistory.CPMEASURE_OBJ = read["CPMEASURE_OBJ"].ToString();
                    CPHistory.CARERECORDID = read["CARERECORDID"].ToString();
                    CPHistory.TARGETID = read["TARGETID"].ToString();
                    CPHistory.DELETED = read["DELETED"].ToString();
                }
                json_log = JsonConvert.SerializeObject(CPHistory);
                this.log.saveLogMsg(json_log + "[刪除最新HISTORY ]", "Score_Del"); //log存檔

            }
            else
            {
                ReturnString = "該筆資料為舊資料，無法做刪除動作。";
                return new JsonResult { Data = ReturnString };

            }
            List<DBItem> dbItemList = new List<DBItem>();
            dbItemList.Add(new DBItem("DELETED", userinfo.EmployeesNo, DBItem.DBDataType.String));
            link.DBExecUpdate("CAREPLAN_HISTORY", dbItemList, "PK_ID = '" + CPHistory.PK_ID + "'");
            id_sql = "SELECT * FROM (SELECT * FROM CAREPLAN_HISTORY WHERE RECORDID='" + recordid + "' AND TARGETID ='" + targetid + "' AND DELETED is null ORDER BY ASSESS_DATE DESC) WHERE ROWNUM = 1";
            id_Dt = this.link.DBExecSQL(id_sql);
            CarePlan_History RetureHistoryData = new CarePlan_History();
            string ASSESS_DATE = "";
            if (id_Dt != null && id_Dt.Rows.Count > 0)
            {
                foreach (DataRow read in id_Dt.Rows)
                {
                    RetureHistoryData.PK_ID = read["PK_ID"].ToString();
                    RetureHistoryData.RECORDID = read["RECORDID"].ToString();
                    RetureHistoryData.TARGETSTATUS = read["TARGETSTATUS"].ToString();
                    RetureHistoryData.REASON = read["REASON"].ToString();
                    RetureHistoryData.ASSESS_ID = read["ASSESS_ID"].ToString();
                    RetureHistoryData.ASSESS_NAME = read["ASSESS_NAME"].ToString();
                    RetureHistoryData.ASSESS_DATE = Convert.ToDateTime(read["ASSESS_DATE"].ToString());
                    RetureHistoryData.CPFEATUREID_OBJ = read["CPFEATUREID_OBJ"].ToString();
                    RetureHistoryData.CPRF_OBJ = read["CPRF_OBJ"].ToString();
                    RetureHistoryData.CPMEASURE_OBJ = read["CPMEASURE_OBJ"].ToString();
                    RetureHistoryData.CARERECORDID = read["CARERECORDID"].ToString();
                    RetureHistoryData.TARGETID = read["TARGETID"].ToString();
                    RetureHistoryData.DELETED = read["DELETED"].ToString();
                }

                ASSESS_DATE = RetureHistoryData.ASSESS_DATE.ToString("yyyy/MM/dd HH:mm:ss");
                json_log = JsonConvert.SerializeObject(RetureHistoryData);
                this.log.saveLogMsg(json_log + "[還原HISTORY ]", "Score_Del"); //log存檔

            }
            else
            {
                RetureHistoryData.TARGETSTATUS = "";
                RetureHistoryData.ASSESS_ID = "";
                RetureHistoryData.REASON = "";

                ASSESS_DATE = "";
            }


            string sql = "SELECT * FROM cptargetdtl where recordid ='" + recordid + "' and TARGETENDDATE is null";
            DataTable Dt = this.link.DBExecSQL(sql);

            dbItemList.Clear();
            dbItemList.Add(new DBItem("TARGETENDDATE", "", DBItem.DBDataType.DataTime));
            dbItemList.Add(new DBItem("TARGETSTATUS", RetureHistoryData.TARGETSTATUS, DBItem.DBDataType.String));
            dbItemList.Add(new DBItem("ASSESS_ID", RetureHistoryData.ASSESS_ID, DBItem.DBDataType.String));
            dbItemList.Add(new DBItem("ASSESS_DATE", ASSESS_DATE, DBItem.DBDataType.DataTime));
            dbItemList.Add(new DBItem("REASON", RetureHistoryData.REASON, DBItem.DBDataType.String));
            json_log = JsonConvert.SerializeObject(dbItemList);
            this.log.saveLogMsg(json_log + "[更新CPTARGETDTL ]", "Score_Del"); //log存檔

            if (link.DBExecUpdateTns("CPTARGETDTL", dbItemList, " recordid = '" + CPHistory.RECORDID.ToString() + "' and TARGETID = '" + CPHistory.TARGETID + "' ") > 0)
            {
                ReturnString = "刪除成功";
            }
            if (Dt.Rows.Count == 0)
            {
                List<string> CAREPLAN_obj = JsonConvert.DeserializeObject<List<string>>(CPHistory.CPFEATUREID_OBJ.ToString());
                string where_CPF = string.Join("','",  CAREPLAN_obj );
                CAREPLAN_obj = JsonConvert.DeserializeObject<List<string>>(CPHistory.CPMEASURE_OBJ.ToString());
                string where_CPM = string.Join("','",  CAREPLAN_obj );
                CAREPLAN_obj = JsonConvert.DeserializeObject<List<string>>(CPHistory.CPRF_OBJ.ToString());
                string where_CPRF = string.Join("','",  CAREPLAN_obj);

                dbItemList.Clear();
                dbItemList.Add(new DBItem("PLANENDDATE", "", DBItem.DBDataType.DataTime));
                dbItemList.Add(new DBItem("ASSESS_ID", RetureHistoryData.ASSESS_ID, DBItem.DBDataType.String));
                dbItemList.Add(new DBItem("ASSESS_DATE", ASSESS_DATE, DBItem.DBDataType.DataTime));
                //dbItemList.Add(new DBItem("MODIFY_ID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                //dbItemList.Add(new DBItem("MODIFT_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                json_log = JsonConvert.SerializeObject(dbItemList);
                this.log.saveLogMsg(json_log + "[更新CAREPLANMASTER ]", "Score_Del"); //log存檔

                if (this.link.DBExecUpdateTns("CAREPLANMASTER", dbItemList, " RECORDID = '" + CPHistory.RECORDID.ToString() + "'") > 0)
                {
                    dbItemList.Clear();
                    dbItemList.Add(new DBItem("FEATUREENDDATE", "", DBItem.DBDataType.DataTime));
                    link.DBExecUpdateTns("CPFEATUREDTL", dbItemList, " SERIAL in ('" + where_CPF + "')");
                    dbItemList.Clear();
                    dbItemList.Add(new DBItem("RELATEDFACTORSENDDATE", "", DBItem.DBDataType.DataTime));
                    link.DBExecUpdateTns("CPRFDTL", dbItemList, " SERIAL in ('" + where_CPRF + "')");
                    dbItemList.Clear();
                    dbItemList.Add(new DBItem("MEASUREENDDATE", "", DBItem.DBDataType.DataTime));
                    link.DBExecUpdateTns("CPMEASUREDTL", dbItemList, " SERIAL in ('" + where_CPM + "')");
                    ReturnString = "刪除成功";
                }
                else
                {
                    ReturnString = "刪除失敗";
                }
            }
            else
            {
                dbItemList.Clear();
                dbItemList.Add(new DBItem("ASSESS_ID", RetureHistoryData.ASSESS_ID, DBItem.DBDataType.String));
                dbItemList.Add(new DBItem("ASSESS_DATE", ASSESS_DATE, DBItem.DBDataType.DataTime));
                //dbItemList.Add(new DBItem("MODIFY_ID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                //dbItemList.Add(new DBItem("MODIFT_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                json_log = JsonConvert.SerializeObject(dbItemList);
                this.log.saveLogMsg(json_log + "[更新CAREPLANMASTER ]", "Score_Del"); //log存檔

                if (this.link.DBExecUpdateTns("CAREPLANMASTER", dbItemList, " RECORDID = '" + CPHistory.RECORDID.ToString() + "'") > 0)
                {
                    ReturnString = "刪除成功";
                }
                else
                {
                    ReturnString = "刪除失敗";
                }

            }

            if (!string.IsNullOrEmpty(CPHistory.CARERECORDID))
            {
                //Del_CareRecordTns(CPHistory.CARERECORDID.ToString(), "CAREPLANMASTER", ref link);
                base.Del_CareRecord(CPHistory.CARERECORDID.ToString(), "CAREPLANMASTER");


            }

            link.DBCommit();

            return new JsonResult { Data = ReturnString };

        }
        #endregion

        #region 新增護理計劃
        //新增--護理問題查詢
        public ActionResult NewCarePlan(string feeno)
        {
            List<SelectListItem> typeList = new List<SelectListItem>();
            string sql = "SELECT * FROM NIS_SYS_DIAGNOSIS_DOMAIN WHERE DISABLE_DATE IS NULL";
            DataTable Dt = this.link.DBExecSQL(sql);
            if (Dt.Rows.Count > 0)
            {
                for (int i = 0; i < Dt.Rows.Count; i++)
                {
                    typeList.Add(new SelectListItem { Text = Dt.Rows[i]["DIAGNOSIS_DOMAIN_DESC"].ToString(), Value = Dt.Rows[i]["DIAGNOSIS_DOMAIN_CODE"].ToString() });
                }
            }
            ViewData["typeList"] = typeList;
            ViewBag.feeno = feeno;
            return View();
        }

        //護理問題查詢
        public PartialViewResult NewCarePlanSearch(string rd, string text, string sub)
        {
            ViewBag.dtsearch = this.SearchCarePlanTopic(rd, text, sub, ptinfo.FeeNo.ToString());
            return PartialView();
        }

        public ActionResult Get_Result(string item, string startdate, string enddate)
        {
            if (item == "TPR")
            {
                DataTable dt = new DataTable();
                string sql = "select * from data_vitalsign where fee_no ='" + ptinfo.FeeNo + "' and create_date between to_date('" + startdate + "','yyyy/MM/dd') and to_date('" + enddate + "','yyyy/MM/dd') and vs_item in ('bt','bp','bf','sp','mp') order by create_date,vs_item";
                dt = this.link.DBExecSQL(sql);
                ViewBag.result = dt;
            }
            if (item == "LAB")
            {
                byte[] LabbyDateByteCode = webService.GetLabbyDate(ptinfo.FeeNo, startdate, enddate);

                if (LabbyDateByteCode != null)
                {
                    string LabbyDateListJosnArr = NIS.UtilTool.CompressTool.DecompressString(LabbyDateByteCode);
                    List<Lab> LabbyDateList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Lab>>(LabbyDateListJosnArr);
                    ViewData["result"] = LabbyDateList;
                }
            }
            ViewBag.Item = item;
            return View();
        }

        public DataTable SearchCarePlanTopic(string rd, string text, string sub, string feeno)
        {
            DataTable dt = new DataTable();
            string sql = "select a.diagnosis_code, a.diagnosis_name, b.diagnosis_domain_desc,(select count(*) from CAREPLANMASTER where feeno ='" + feeno + "' and PLANENDDATE is null and topicid = a.diagnosis_code) as USE_NUM ";
            sql += "from nis_sys_diagnosis a, nis_sys_diagnosis_domain b ";
            sql += "where a.diagnosis_domain_code = b.diagnosis_domain_code ";
            sql += "and a.disable_date is null and b.disable_date is null ";
            if (rd == "SUBJECT")//by科別
                sql += "and b.diagnosis_domain_code = '" + sub + "' ";
            else if (rd != "" && text.Trim() != "")//by關鍵字
                sql += "AND a.diagnosis_name LIKE '%" + text + "%' ";
            sql += "ORDER BY a.diagnosis_name ASC";
            this.link.DBExecSQL(sql, ref dt);
            return dt;
        }

        //新增護理計劃
        public ActionResult NewCarePlanItem(string f_id)
        {
            ViewData["dt_Topic"] = this.Sel_Topic(f_id);
            ViewData["dt_Defin"] = this.Sel_Defin(f_id);
            ViewData["dt_About"] = this.Sel_About(f_id);
            ViewData["dt_Goal"] = this.Sel_Goal(f_id);

            ViewData["dt_Active"] = this.Sel_Active(f_id);
            return View();
        }
        #endregion

        #region 護理計畫存檔
        [HttpPost]
        public ActionResult CarePlanSave()
        {
            string RECORDID = userinfo.EmployeesNo + ptinfo.FeeNo + DateTime.Now.ToString("yyyyMMddHHmmss");
            string CareRecordContent = string.Empty;
            string CarePlanCount = string.Empty;
            //string sql = "SELECT count(*)+1 AS PNO FROM CAREPLANMASTER where feeno='" + ptinfo.FeeNo + "'";
            string sql = "SELECT MAX(PNO)+1 AS PNO FROM CAREPLANMASTER where feeno='" + ptinfo.FeeNo + "'";
            string ALLFEATUREDESC = string.Empty;
            DataTable Dt = this.link.DBExecSQL(sql);
            if (!string.IsNullOrEmpty(Dt.Rows[0]["PNO"].ToString()))
            {
                CarePlanCount = Dt.Rows[0]["PNO"].ToString();
            }
            else
            {
                CarePlanCount = "1";
            }
            //儲存護理診斷
            if (Request.Form["f_id"] != null)
            {
                List<DBItem> InsertList = new List<DBItem>();
                InsertList.Add(new DBItem("RECORDID", RECORDID, DBItem.DBDataType.String));
                InsertList.Add(new DBItem("PLANSTARTDATE", Request.Form["CreateDate"] + " " + Request.Form["CreateTime"], DBItem.DBDataType.DataTime));
                InsertList.Add(new DBItem("TOPICID", Request.Form["f_id"].ToString(), DBItem.DBDataType.String));
                InsertList.Add(new DBItem("TOPICDESC", Request.Form["f_item"].ToString(), DBItem.DBDataType.String));
                InsertList.Add(new DBItem("FEENO", ptinfo.FeeNo, DBItem.DBDataType.String));
                InsertList.Add(new DBItem("RECORDER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                InsertList.Add(new DBItem("PNO", CarePlanCount, DBItem.DBDataType.Number));
                InsertList.Add(new DBItem("S", Request.Form["S"], DBItem.DBDataType.String));
                int erow = this.link.DBExecInsert("CAREPLANMASTER", InsertList);
                //CareRecordContent += Request.Form["f_item"].ToString() + "/O：";
                if (Request.Form["S"] != "")
                {
                    CareRecordContent += "S：" + Request.Form["S"] + "。";
                }

            }
            //儲存定義性特徵
            if (Request.Form["FEATURE"] != null)
            {

                for (int j = 0; j < Request.Form["FEATURE"].ToString().Split(',').Length; j++)
                {
                    string FEATUREID = Request.Form["FEATURE"].ToString().Split(',').GetValue(j).ToString();
                    string Serial = base.creatid("CPFeatureDTL", userinfo.EmployeesNo.ToString().Trim(), ptinfo.FeeNo.ToString().Trim(), j.ToString());
                    List<DBItem> InsertList = new List<DBItem>();
                    InsertList.Add(new DBItem("SERIAL", Serial, DBItem.DBDataType.String));
                    InsertList.Add(new DBItem("RECORDID", RECORDID, DBItem.DBDataType.String));
                    InsertList.Add(new DBItem("FEATURESTARTDATE", Request.Form["CreateDate"] + " " + Request.Form["CreateTime"], DBItem.DBDataType.DataTime));
                    InsertList.Add(new DBItem("FEATUREID", FEATUREID, DBItem.DBDataType.String));
                    InsertList.Add(new DBItem("FEATUREDESC", Request.Form["FEATURE" + FEATUREID].ToString(), DBItem.DBDataType.String));
                    int erow = this.link.DBExecInsert("CPFEATUREDTL", InsertList);
                    ALLFEATUREDESC += Request.Form["FEATURE" + FEATUREID].ToString() + ",";
                }

            }
            if (Request.Form["add_Def"] != null)
            {
                string custom_feature = string.Empty;
                for (int j = 1; j < Request.Form["add_Def"].ToString().Split(',').Length; j++)
                {
                    string FEATUREID = Request.Form["add_Def"].ToString().Split(',').GetValue(j).ToString();
                    string Serial = base.creatid("CPFeatureCDTL", userinfo.EmployeesNo.ToString().Trim(), ptinfo.FeeNo.ToString().Trim(), j.ToString());
                    List<DBItem> InsertList = new List<DBItem>();
                    InsertList.Add(new DBItem("SERIAL", Serial, DBItem.DBDataType.String));
                    InsertList.Add(new DBItem("RECORDID", RECORDID, DBItem.DBDataType.String));
                    InsertList.Add(new DBItem("FEATURESTARTDATE", Request.Form["CreateDate"] + " " + Request.Form["CreateTime"], DBItem.DBDataType.DataTime));
                    InsertList.Add(new DBItem("FEATUREID", "C" + j, DBItem.DBDataType.String));
                    InsertList.Add(new DBItem("FEATUREDESC", Request.Form["add_Def"].ToString().Split(',').GetValue(j).ToString(), DBItem.DBDataType.String));
                    InsertList.Add(new DBItem("CUSTOM", "Y", DBItem.DBDataType.String));
                    int erow = this.link.DBExecInsert("CPFEATUREDTL", InsertList);
                    ALLFEATUREDESC += Request.Form["add_Def"].ToString().Split(',').GetValue(j).ToString() + ",";
                }
            }
            //20160620 mod by yungchen
            if (ALLFEATUREDESC.Length > 0)
                ALLFEATUREDESC = ALLFEATUREDESC.Substring(0, ALLFEATUREDESC.Length - 1);
            CareRecordContent += "O：評估病人出現" + ALLFEATUREDESC.Replace(",", "、") + "。";

            //儲存相關因素
            if (Request.Form["INDUCEMENTS"] != null)
            {
                string ALLRelatedFactorsDesc = string.Empty;
                for (int j = 0; j < Request.Form["INDUCEMENTS"].ToString().Split(',').Length; j++)
                {
                    string INDUCEMENTS = Request.Form["INDUCEMENTS"].ToString().Split(',').GetValue(j).ToString();
                    string Serial = base.creatid("CPRFDTL", userinfo.EmployeesNo.ToString().Trim(), ptinfo.FeeNo.ToString().Trim(), j.ToString());
                    List<DBItem> InsertList = new List<DBItem>();
                    InsertList.Add(new DBItem("SERIAL", Serial, DBItem.DBDataType.String));
                    InsertList.Add(new DBItem("RECORDID", RECORDID, DBItem.DBDataType.String));
                    InsertList.Add(new DBItem("RELATEDFACTORSSTARTDATE", Request.Form["CreateDate"] + " " + Request.Form["CreateTime"], DBItem.DBDataType.DataTime));
                    InsertList.Add(new DBItem("RELATEDFACTORSID", INDUCEMENTS, DBItem.DBDataType.String));
                    InsertList.Add(new DBItem("RELATEDFACTORSDESC", Request.Form["INDUCEMENTS" + INDUCEMENTS].ToString(), DBItem.DBDataType.String));
                    int erow = this.link.DBExecInsert("CPRFDTL", InsertList);
                    ALLRelatedFactorsDesc += Request.Form["INDUCEMENTS" + INDUCEMENTS].ToString() + ",";
                }
                ALLRelatedFactorsDesc = ALLRelatedFactorsDesc.Substring(0, ALLRelatedFactorsDesc.Length - 1);
                CareRecordContent += "A：評估病人具" + ALLRelatedFactorsDesc.Replace(",", "、") + "之危險因子。";
            }
            if (Request.Form["FEATURE"] != null || Request.Form["INDUCEMENTS"] != null)
            {
                //CareRecordContent += "A：建立" + Request.Form["f_item"].ToString() + "健康問題之照護計畫。";
            }
            //儲存目標
            if (Request.Form["TARGET"] != null)
            {
                string ALLTARGET = string.Empty;
                for (int j = 0; j < Request.Form["TARGET"].ToString().Split(',').Length; j++)
                {
                    string TARGETID = Request.Form["TARGET"].ToString().Split(',').GetValue(j).ToString();
                    string TARGETCONTENT = string.Empty;
                    //string ALLTARGETCONTENT = string.Empty;
                    string Serial = base.creatid("CPTARGETDTL", userinfo.EmployeesNo.ToString().Trim(), ptinfo.FeeNo.ToString().Trim(), j.ToString());



                    List<DBItem> InsertList = new List<DBItem>();
                    InsertList.Add(new DBItem("SERIAL", Serial, DBItem.DBDataType.String));
                    InsertList.Add(new DBItem("RECORDID", RECORDID, DBItem.DBDataType.String));
                    InsertList.Add(new DBItem("TARGETSTARTDATE", Request.Form["CreateDate"] + " " + Request.Form["CreateTime"], DBItem.DBDataType.DataTime));
                    InsertList.Add(new DBItem("TARGETID", TARGETID, DBItem.DBDataType.String));
                    InsertList.Add(new DBItem("TARGETDESC", Request.Form["TARGET" + TARGETID].ToString(), DBItem.DBDataType.String));
                    int erow = this.link.DBExecInsert("CPTARGETDTL", InsertList);
                    ALLTARGET += Request.Form["TARGET" + TARGETID].ToString() + "、";
                }
                ALLTARGET = ALLTARGET.Substring(0, ALLTARGET.Length - 1);
                CareRecordContent += "P:擬定";
            }
            //儲存護理措施
            if (Request.Form["MEASURE"] != null)
            {
                string ALLMEASURE = string.Empty;
                for (int j = 0; j < Request.Form["MEASURE"].ToString().Split(',').Length; j++)
                {
                    string MEASUREID = Request.Form["MEASURE"].ToString().Split(',').GetValue(j).ToString();
                    string Serial = base.creatid("CPMEASUREDTL", userinfo.EmployeesNo.ToString().Trim(), ptinfo.FeeNo.ToString().Trim(), j.ToString());
                    List<DBItem> InsertList = new List<DBItem>();
                    InsertList.Add(new DBItem("SERIAL", Serial, DBItem.DBDataType.String));
                    InsertList.Add(new DBItem("RECORDID", RECORDID, DBItem.DBDataType.String));
                    InsertList.Add(new DBItem("MEASUREID", MEASUREID, DBItem.DBDataType.String));
                    InsertList.Add(new DBItem("MEASUREDESC", Request.Form["MEASURE" + MEASUREID].ToString(), DBItem.DBDataType.String));
                    InsertList.Add(new DBItem("MEASURESTARTDATE", Request.Form["CreateDate"] + " " + Request.Form["CreateTime"], DBItem.DBDataType.DataTime));
                    int erow = this.link.DBExecInsert("CPMEASUREDTL", InsertList);
                    ALLMEASURE += Request.Form["MEASURE" + MEASUREID].ToString() + "、";
                }
                ALLMEASURE = ALLMEASURE.Substring(0, ALLMEASURE.Length - 1);
                CareRecordContent += ALLMEASURE + "之護理措施。";
            }
            //轉帶護理紀錄
            Insert_CareRecord(Request.Form["CreateDate"] + " " + Request.Form["CreateTime"] + ":" + DateTime.Now.ToString("ss"), RECORDID + DateTime.Now.ToString("yyyyMMddHHmmss"), "#" + CarePlanCount + " " + Request.Form["f_item"].ToString(), CareRecordContent, "", "", "", "", "CAREPLANMASTER");
            Response.Write("<script>alert('儲存成功!');location.href='../NurseCarePlan/Index';</script>");
            return new EmptyResult();
        }
        #endregion

        #region 護理計畫刪除
        public string CarePlanDel(string Cid = "")
        {
            if (!Bool_Delete(Cid))
            {
                return "Fail";
            }
            string where = "";
            string id = "";
            string sql = "";
            where = "CARERECORD_ID LIKE '%" + Cid + "%' AND FEENO ='" + ptinfo.FeeNo.ToString().Trim() + "' AND SELF ='CAREPLANMASTER' AND DELETED is null ";//id為必要值
            sql = "SELECT CARERECORD_ID FROM CARERECORD_DATA WHERE " + where;
            DataTable Dt = link.DBExecSQL(sql);
            if (Dt.Rows.Count > 0)
             {    
                 for (int d = 0; d < Dt.Rows.Count; d++)
                    {
                        id = Dt.Rows[d]["CARERECORD_ID"].ToString();
                    }
            }
            try
            {
                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();//引用stopwatch物件
                sw.Reset();//碼表歸零
                sw.Start();//碼表開始計時
                
                //Del_CareRecordTns(Cid, "CAREPLANMASTER", ref link, id);
                Del_CareRecord(Cid, "CAREPLANMASTER");

                link.DBExecDeleteTns("CAREPLANMASTER", "recordid ='" + Cid + "'");
                    link.DBExecDeleteTns("CPFEATUREDTL", "recordid ='" + Cid + "'");
                    link.DBExecDeleteTns("CPRFDTL", "recordid ='" + Cid + "'");
                    link.DBExecDeleteTns("CPTARGETDTL", "recordid ='" + Cid + "'");
                    link.DBExecDeleteTns("CPMEASUREDTL", "recordid ='" + Cid + "'");
                link.DBCommit();

                if (!string.IsNullOrEmpty(id))
                {
                    try
                    {
                            //int result = del_emr(id + "CAREPLANMASTER", userinfo.EmployeesNo);
                    }
                    catch (Exception ex)
                    {
                        //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                        string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                        string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                        write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(),ex);
                    }
                }

                sw.Stop();//碼錶停止

                //印出所花費的總毫秒數

                string result1 = sw.Elapsed.TotalMilliseconds.ToString();


                return "Y";

            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(),ex);

                return "N";
            }

        }
        public bool Bool_Delete(string Cid)//是否可以刪除
        {
            int rowct = 0;
            try
            {
                string sqlstr = "";
                string where = "CARERECORD_ID LIKE '%" + Cid + "%' AND SELF ='CAREPLANMASTER' AND FEENO ='" + ptinfo.FeeNo.ToString().Trim() + "' AND DELETED is null ";//id為必要值
                sqlstr += "SELECT COUNT(*)as cnt FROM CARERECORD_DATA WHERE " + where;
                DataTable Dt = link.DBExecSQL(sqlstr);
                if (Dt.Rows.Count > 0)
                {
                    for (int d = 0; d < Dt.Rows.Count; d++)
                    {
                        rowct = int.Parse(Dt.Rows[d]["cnt"].ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(),ex);
            }

            if (rowct > 1 )
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        #endregion

        #region 取得護理計畫資料集
        public DataTable Sel_User_Master(string feeno)
        {
            DataTable dt = new DataTable();
            string sql = @"SELECT * FROM CAREPLANMASTER where feeno='" + feeno + "' order by pno asc";
            this.link.DBExecSQL(sql, ref dt);
            return dt;
        }

        public List<User_Care_Plan_Item> Sel_User_Defin(string feeno)
        {
            List<User_Care_Plan_Item> temp = null;
            User_Care_Plan_Item temp_data = null;
            string sql = "select * from CPFEATUREDTL where recordid in (select recordid from CAREPLANMASTER where feeno='" + feeno + "') and FEATUREENDDATE is null ";
            DataTable Dt = this.link.DBExecSQL(sql);
            if (Dt != null && Dt.Rows.Count > 0)
            {
                temp = new List<User_Care_Plan_Item>();
                foreach (DataRow read in Dt.Rows)
                {
                    temp_data = new User_Care_Plan_Item();
                    temp_data.M_ID = read["recordid"].ToString();
                    temp_data.D_ID = read["featureid"].ToString();
                    temp_data.Item = read["featuredesc"].ToString();
                    temp_data.Custom = read["custom"].ToString();
                    temp_data.RecordTime = read["featurestartdate"].ToString();
                    temp_data.StopDate = read["FEATUREENDDATE"].ToString();
                    temp.Add(temp_data);
                }
            }
            return temp;
        }

        public List<User_Care_Plan_Item> Get_All_Defin(string feeno)
        {
            List<User_Care_Plan_Item> temp = null;
            User_Care_Plan_Item temp_data = null;
            string sql = "select * from CPFEATUREDTL where recordid in (select recordid from CAREPLANMASTER where feeno='" + feeno + "') ";
            DataTable Dt = this.link.DBExecSQL(sql);
            if (Dt != null && Dt.Rows.Count > 0)
            {
                temp = new List<User_Care_Plan_Item>();
                foreach (DataRow read in Dt.Rows)
                {
                    temp_data = new User_Care_Plan_Item();
                    temp_data.M_ID = read["recordid"].ToString();
                    temp_data.D_ID = read["featureid"].ToString();
                    temp_data.Item = read["featuredesc"].ToString();
                    temp_data.Custom = read["custom"].ToString();
                    temp_data.RecordTime = read["featurestartdate"].ToString();
                    temp_data.StopDate = read["FEATUREENDDATE"].ToString();
                    temp.Add(temp_data);
                }
            }
            return temp;
        }

        public List<User_Care_Plan_Item> Sel_User_About(string feeno)
        {
            List<User_Care_Plan_Item> temp = null;
            User_Care_Plan_Item temp_data = null;
            string sql = "select * from CPRFDTL where recordid in (select recordid from CAREPLANMASTER where feeno='" + feeno + "') and RELATEDFACTORSENDDATE is null ";
            DataTable Dt = this.link.DBExecSQL(sql);
            if (Dt != null && Dt.Rows.Count > 0)
            {
                temp = new List<User_Care_Plan_Item>();
                foreach (DataRow read in Dt.Rows)
                {
                    temp_data = new User_Care_Plan_Item();
                    temp_data.M_ID = read["recordid"].ToString();
                    temp_data.A_ID = read["relatedfactorsid"].ToString();
                    temp_data.Item = read["relatedfactorsdesc"].ToString();
                    temp_data.Custom = read["custom"].ToString();
                    temp_data.RecordTime = read["relatedfactorsstartdate"].ToString();
                    temp_data.StopDate = read["RELATEDFACTORSENDDATE"].ToString();
                    temp.Add(temp_data);
                }
            }
            
            return temp;
        }

        public List<User_Care_Plan_Item> Get_All_About(string feeno)
        {
            List<User_Care_Plan_Item> temp = null;
            User_Care_Plan_Item temp_data = null;
            string sql = "select * from CPRFDTL where recordid in (select recordid from CAREPLANMASTER where feeno='" + feeno + "') ";
            DataTable Dt = this.link.DBExecSQL(sql);
            if (Dt != null && Dt.Rows.Count > 0)
            {
                temp = new List<User_Care_Plan_Item>();
                foreach (DataRow read in Dt.Rows)
                {
                    temp_data = new User_Care_Plan_Item();
                    temp_data.M_ID = read["recordid"].ToString();
                    temp_data.A_ID = read["relatedfactorsid"].ToString();
                    temp_data.Item = read["relatedfactorsdesc"].ToString();
                    temp_data.Custom = read["custom"].ToString();
                    temp_data.RecordTime = read["relatedfactorsstartdate"].ToString();
                    temp_data.StopDate = read["RELATEDFACTORSENDDATE"].ToString();
                    temp.Add(temp_data);
                }
            }
            
            return temp;
        }

        public List<User_Care_Plan_Item> Sel_User_Goal(string feeno)
        {
            List<User_Care_Plan_Item> temp = null;
            User_Care_Plan_Item temp_data = null;
            string sql = "select * from CPTARGETDTL where ";
            //sql += "TARGETENDDATE is null and ";
            sql += "recordid in (select recordid from CAREPLANMASTER where feeno='" + feeno + "')";
            DataTable Dt = this.link.DBExecSQL(sql);
            if (Dt != null && Dt.Rows.Count > 0)
            {
                temp = new List<User_Care_Plan_Item>();
                foreach (DataRow read in Dt.Rows)
                {
                    temp_data = new User_Care_Plan_Item();
                    temp_data.M_ID = read["recordid"].ToString();
                    temp_data.G_PK_ID = read["targetid"].ToString();
                    temp_data.Item = read["targetdesc"].ToString();
                    temp_data.Content = read["targetdesc"].ToString();
                    temp_data.Custom = read["custom"].ToString();
                    temp_data.ScoreTime = read["targetstartdate"].ToString();
                    temp_data.StopDate = read["TARGETENDDATE"].ToString();
                    temp.Add(temp_data);
                }
            }


            return temp;
        }

        public List<User_Care_Plan_Item> Get_All_Goal(string feeno)
        {
            List<User_Care_Plan_Item> temp = null;
            User_Care_Plan_Item temp_data = null;
            string sql = "select * from CPTARGETDTL where recordid in (select recordid from CAREPLANMASTER where feeno='" + feeno + "')";
            DataTable Dt = this.link.DBExecSQL(sql);
            if (Dt != null && Dt.Rows.Count > 0)
            {
                string result = string.Empty;
                temp = new List<User_Care_Plan_Item>();
                foreach (DataRow read in Dt.Rows)
                {
                    if (read["TARGETSTATUS"].ToString() == "Y")
                    {
                        result = "已達成";
                    }
                    if (read["TARGETSTATUS"].ToString() == "C")
                    {
                        result = "不適用";
                    }
                    if (read["TARGETSTATUS"].ToString() == "")
                    {
                        result = "未評值";
                    }
                    if (read["TARGETSTATUS"].ToString() == "N")
                    {
                        result = "未達成，" + read["REASON"].ToString();
                    }
                    temp_data = new User_Care_Plan_Item();
                    temp_data.M_ID = read["recordid"].ToString();
                    temp_data.G_PK_ID = read["targetid"].ToString();
                    temp_data.Item = read["targetdesc"].ToString();
                    temp_data.Modify = (read["Assess_ID"].ToString() == userinfo.EmployeesNo) ? "Y" : "F";
                    temp_data.Content = read["targetdesc"].ToString();
                    temp_data.Custom = read["custom"].ToString();
                    temp_data.ScoreTime = read["ASSESS_DATE"].ToString();
                    temp_data.Score = result;
                    temp_data.Score_Status = read["TARGETSTATUS"].ToString();
                    temp_data.StopDate = read["TARGETENDDATE"].ToString();
                    temp.Add(temp_data);
                }
            }


            return temp;
        }

        public List<User_Care_Plan_Item> Sel_User_Active(string feeno)
        {
            List<User_Care_Plan_Item> temp = null;
            User_Care_Plan_Item temp_data = null;
            string sql = "select * from CPMEASUREDTL where MEASUREENDDATE is null and recordid in (select recordid from CAREPLANMASTER where feeno='" + feeno + "')";
            DataTable Dt = this.link.DBExecSQL(sql);
            if (Dt != null && Dt.Rows.Count > 0)
            {
                temp = new List<User_Care_Plan_Item>();
                foreach (DataRow read in Dt.Rows)
                {
                    temp_data = new User_Care_Plan_Item();
                    temp_data.M_ID = read["recordid"].ToString();
                    temp_data.I_ID = read["measureid"].ToString();
                    temp_data.Item = read["measuredesc"].ToString();
                    temp_data.Custom = read["custom"].ToString();
                    temp.Add(temp_data);
                }
            }

            return temp;
        }

        public List<User_Care_Plan_Item> Get_All_Active(string feeno)
        {
            List<User_Care_Plan_Item> temp = null;
            User_Care_Plan_Item temp_data = null;
            string sql = "select * from CPMEASUREDTL where recordid in (select recordid from CAREPLANMASTER where feeno='" + feeno + "')";
            DataTable Dt = this.link.DBExecSQL(sql);
            if (Dt != null && Dt.Rows.Count > 0)
            {
                temp = new List<User_Care_Plan_Item>();
                foreach (DataRow read in Dt.Rows)
                {
                    temp_data = new User_Care_Plan_Item();
                    temp_data.M_ID = read["recordid"].ToString();
                    temp_data.I_ID = read["measureid"].ToString();
                    temp_data.Item = read["measuredesc"].ToString();
                    temp_data.Custom = read["custom"].ToString();
                    temp_data.RecordTime = read["MEASURESTARTDATE"].ToString();
                    temp_data.StopDate = read["MEASUREENDDATE"].ToString();
                    temp.Add(temp_data);
                }
            }

            return temp;
        }

        #endregion



        #region 編輯護理計劃
        //新增護理計劃
        public ActionResult EditCarePlan(string R, string recordid, string HistoryID,string source= "")
        {
            string sql = "select topicid,s from CAREPLANMASTER where recordid ='" + recordid + "'";
            DataTable dt = this.link.DBExecSQL(sql);
            if (dt != null && dt.Rows.Count > 0)
            {
                //取得護理計畫內容
                ViewBag.S = dt.Rows[0][1].ToString();
                ViewData["dt_Topic"] = this.Sel_Topic(dt.Rows[0][0].ToString());
                ViewData["dt_Defin"] = this.Sel_Defin(dt.Rows[0][0].ToString());
                ViewData["dt_About"] = this.Sel_About(dt.Rows[0][0].ToString());
                ViewData["dt_Goal"] = this.Sel_Goal(dt.Rows[0][0].ToString());
                ViewData["dt_Active"] = this.Sel_Active(dt.Rows[0][0].ToString());
                //取得使用者填寫內容
                ViewBag.S = dt.Rows[0]["S"].ToString();
                sql = "select * from CPFEATUREDTL where recordid ='" + recordid + "' and FEATUREENDDATE is null";
                DataTable Dt = this.link.DBExecSQL(sql);
                ViewBag.UserFeature = Dt;
                sql = "select * from CPRFDTL where recordid ='" + recordid + "' and RELATEDFACTORSENDDATE is null";
                Dt = this.link.DBExecSQL(sql);
                ViewBag.UserInducements = Dt;
                sql = "select * from CPTARGETDTL where recordid ='" + recordid + "' and TARGETENDDATE is null";
                Dt = this.link.DBExecSQL(sql);
                ViewBag.UserTarget = Dt;
                sql = "select * from CPMEASUREDTL where recordid ='" + recordid + "' and MEASUREENDDATE is null";
                Dt = this.link.DBExecSQL(sql);
                ViewBag.UserMeasure = Dt;
                ViewBag.R = R;
                ViewBag.HistoryID = HistoryID;
                ViewBag.ReturnSource = source;
            }

            return View();
        }

        public ActionResult CarePlanEdit()
        {
            string RECORDID = Request.Form["recordid"].ToString();
            string HistoryID = Request.Form["HistoryID"].ToString();
            string CareRecordContent = string.Empty;
            string sql = "SELECT PNO AS PNO FROM CAREPLANMASTER where RECORDID ='" + RECORDID + "'";
            DataTable Dt = this.link.DBExecSQL(sql);
            string CarePlanCount = Dt.Rows[0]["PNO"].ToString();
            //儲存護理診斷
            List<DBItem> UpdList = new List<DBItem>();
            UpdList.Add(new DBItem("MODIFT_DATE", Request.Form["CreateDate"] + " " + Request.Form["CreateTime"], DBItem.DBDataType.DataTime));
            UpdList.Add(new DBItem("MODIFY_ID", userinfo.EmployeesNo, DBItem.DBDataType.String));
            UpdList.Add(new DBItem("S", Request.Form["S"], DBItem.DBDataType.String));
            string Where = " recordid = '" + RECORDID + "'";
            this.link.DBExecUpdate("CAREPLANMASTER", UpdList, Where);
            //CareRecordContent += Request.Form["f_item"].ToString() + "/O：";
            if (Request.Form["S"] != "")
            {
                CareRecordContent += "S：" + Request.Form["S"] + "。";
            }

            string ALLFEATUREDESC = string.Empty;

            //儲存定義性特徵
            if (Request.Form["FEATURE"] != null)
            {
                string[] OldFeatureid = { };
                OldFeatureid = Request.Form["OldFeatureid"].ToString().Split(',');
                string[] NewFeatureid = { };
                NewFeatureid = Request.Form["FEATURE"].ToString().Split(',');


                for (int j = 0; j < Request.Form["FEATURE"].ToString().Split(',').Length; j++)
                {
                    string FEATUREID = Request.Form["FEATURE"].ToString().Split(',').GetValue(j).ToString();
                    //如果是新的定義性特徵就新增
                    if (!Array.Exists(OldFeatureid, element => element == FEATUREID))
                    {
                        string Serial = base.creatid("CPFeatureDTL", userinfo.EmployeesNo.ToString().Trim(), ptinfo.FeeNo.ToString().Trim(), j.ToString());
                        List<DBItem> InsertList = new List<DBItem>();
                        InsertList.Add(new DBItem("SERIAL", Serial, DBItem.DBDataType.String));
                        InsertList.Add(new DBItem("RECORDID", RECORDID, DBItem.DBDataType.String));
                        InsertList.Add(new DBItem("FEATURESTARTDATE", Request.Form["CreateDate"] + " " + Request.Form["CreateTime"], DBItem.DBDataType.DataTime));
                        InsertList.Add(new DBItem("FEATUREID", FEATUREID, DBItem.DBDataType.String));
                        InsertList.Add(new DBItem("FEATUREDESC", Request.Form["FEATURE" + FEATUREID].ToString(), DBItem.DBDataType.String));
                        int erow = this.link.DBExecInsert("CPFEATUREDTL", InsertList);
                    }
                    ALLFEATUREDESC += Request.Form["FEATURE" + FEATUREID].ToString() + ",";
                }


                //如果是舊的定義性特徵被取消就押上停止日期
                for (int k = 0; k < OldFeatureid.Length; k++)
                {
                    if (!Array.Exists(NewFeatureid, element => element == OldFeatureid[k]))
                    {
                        string where = " RECORDID = '" + RECORDID + "' and FEATUREID =  '" + OldFeatureid[k] + "'";
                        List<DBItem> updList = new List<DBItem>();
                        updList.Add(new DBItem("FEATUREENDDATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                        int effRow = this.link.DBExecUpdate("CPFeatureDTL", updList, where);
                    }
                }

            }

            if (Request.Form["add_Def"] != null)
            {
                string[] OldCFeatureid = { };
                OldCFeatureid = Request.Form["OldCFeatureid"].ToString().Split('|');
                string[] NewCFeatureid = { };
                NewCFeatureid = Request.Form["add_Def"].ToString().Split(',');

                string custom_feature = string.Empty;
                for (int j = 1; j < Request.Form["add_Def"].ToString().Split(',').Length; j++)
                {
                    string FEATUREID = Request.Form["add_Def"].ToString().Split(',').GetValue(j).ToString();
                    if (!Array.Exists(OldCFeatureid, element => element == FEATUREID))
                    {
                        string Serial = base.creatid("CPFeatureCDTL", userinfo.EmployeesNo.ToString().Trim(), ptinfo.FeeNo.ToString().Trim(), j.ToString());
                        List<DBItem> InsertList = new List<DBItem>();
                        InsertList.Add(new DBItem("SERIAL", Serial, DBItem.DBDataType.String));
                        InsertList.Add(new DBItem("RECORDID", RECORDID, DBItem.DBDataType.String));
                        InsertList.Add(new DBItem("FEATURESTARTDATE", Request.Form["CreateDate"] + " " + Request.Form["CreateTime"], DBItem.DBDataType.DataTime));
                        InsertList.Add(new DBItem("FEATUREID", "C" + j, DBItem.DBDataType.String));
                        InsertList.Add(new DBItem("FEATUREDESC", Request.Form["add_Def"].ToString().Split(',').GetValue(j).ToString(), DBItem.DBDataType.String));
                        InsertList.Add(new DBItem("CUSTOM", "Y", DBItem.DBDataType.String));
                        int erow = this.link.DBExecInsert("CPFEATUREDTL", InsertList);
                    }

                    ALLFEATUREDESC += Request.Form["add_Def"].ToString().Split(',').GetValue(j).ToString() + ",";
                }

                //如果是舊的定義性特徵被取消就押上停止日期
                for (int k = 0; k < OldCFeatureid.Length; k++)
                {
                    if (!Array.Exists(NewCFeatureid, element => element == OldCFeatureid[k]))
                    {
                        string where = " RECORDID = '" + RECORDID + "' and FEATUREDESC =  '" + OldCFeatureid[k] + "'";
                        List<DBItem> updList = new List<DBItem>();
                        updList.Add(new DBItem("FEATUREENDDATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                        int effRow = this.link.DBExecUpdate("CPFeatureDTL", updList, where);
                    }
                }

            }
            //20160620 mod by yungchen
            if (ALLFEATUREDESC.Length > 0)
                ALLFEATUREDESC = ALLFEATUREDESC.Substring(0, ALLFEATUREDESC.Length - 1);
            CareRecordContent += "O：評估病人出現" + ALLFEATUREDESC.Replace(",", "、") + "。";
            //儲存相關因素
            if (Request.Form["INDUCEMENTS"] != null)
            {
                string[] OldRelatedfactorsid = { };
                OldRelatedfactorsid = Request.Form["OldRelatedfactorsid"].ToString().Split(',');
                string[] NewRelatedfactorsid = { };
                NewRelatedfactorsid = Request.Form["INDUCEMENTS"].ToString().Split(',');

                string ALLRelatedFactorsDesc = string.Empty;
                for (int j = 0; j < Request.Form["INDUCEMENTS"].ToString().Split(',').Length; j++)
                {
                    string INDUCEMENTS = Request.Form["INDUCEMENTS"].ToString().Split(',').GetValue(j).ToString();
                    //如果是新的相關因素就新增
                    if (!Array.Exists(OldRelatedfactorsid, element => element == INDUCEMENTS))
                    {
                        string Serial = base.creatid("CPRFDTL", userinfo.EmployeesNo.ToString().Trim(), ptinfo.FeeNo.ToString().Trim(), j.ToString());
                        List<DBItem> InsertList = new List<DBItem>();
                        InsertList.Add(new DBItem("SERIAL", Serial, DBItem.DBDataType.String));
                        InsertList.Add(new DBItem("RECORDID", RECORDID, DBItem.DBDataType.String));
                        InsertList.Add(new DBItem("RELATEDFACTORSSTARTDATE", Request.Form["CreateDate"] + " " + Request.Form["CreateTime"], DBItem.DBDataType.DataTime));
                        InsertList.Add(new DBItem("RELATEDFACTORSID", INDUCEMENTS, DBItem.DBDataType.String));
                        InsertList.Add(new DBItem("RELATEDFACTORSDESC", Request.Form["INDUCEMENTS" + INDUCEMENTS].ToString(), DBItem.DBDataType.String));
                        int erow = this.link.DBExecInsert("CPRFDTL", InsertList);
                    }
                    ALLRelatedFactorsDesc += Request.Form["INDUCEMENTS" + INDUCEMENTS].ToString() + ",";
                }
                ALLRelatedFactorsDesc = ALLRelatedFactorsDesc.Substring(0, ALLRelatedFactorsDesc.Length - 1);
                CareRecordContent += "A：評估病人具" + ALLRelatedFactorsDesc.Replace(",", "、") + "之危險因子。";

                //如果是舊的相關因素被取消就押上停止日期
                for (int k = 0; k < OldRelatedfactorsid.Length; k++)
                {
                    if (!Array.Exists(NewRelatedfactorsid, element => element == OldRelatedfactorsid[k]))
                    {
                        string where = " RECORDID = '" + RECORDID + "' and RELATEDFACTORSID =  '" + OldRelatedfactorsid[k] + "'";
                        List<DBItem> updList = new List<DBItem>();
                        updList.Add(new DBItem("RELATEDFACTORSENDDATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                        int effRow = this.link.DBExecUpdate("CPRFDTL", updList, where);
                    }
                }

            }
            if (Request.Form["FEATURE"] != null || Request.Form["INDUCEMENTS"] != null)
            {
                //CareRecordContent += "A：建立" + Request.Form["f_item"].ToString() + "健康問題之照護計畫。";
            }
            //儲存目標
            if (Request.Form["TARGET"] != null)
            {
                //先把舊有目標刪除
                //sql = "delete from CPTARGETDTL where recordid ='" + RECORDID + "'";
                //this.link.DBExecSQL(sql);
                //新增新的目標
                string[] OldTarget = { };
                OldTarget = Request.Form["OldTarget"].ToString().Split(',');
                string[] NewTarget = { };
                NewTarget = Request.Form["TARGET"].ToString().Split(',');

                string ALLTARGET = string.Empty;
                for (int j = 0; j < Request.Form["TARGET"].ToString().Split(',').Length; j++)
                {
                    string TARGETID = Request.Form["TARGET"].ToString().Split(',').GetValue(j).ToString();
                    //如果是新的目標就新增
                    if (!Array.Exists(OldTarget, element => element == TARGETID))
                    {
                        string TARGETCONTENT = string.Empty;
                        string ALLTARGETCONTENT = string.Empty;
                        string Serial = base.creatid("CPTARGETDTL", userinfo.EmployeesNo.ToString().Trim(), ptinfo.FeeNo.ToString().Trim(), j.ToString());
                        
                        List<DBItem> InsertList = new List<DBItem>();
                        InsertList.Add(new DBItem("SERIAL", Serial, DBItem.DBDataType.String));
                        InsertList.Add(new DBItem("RECORDID", RECORDID, DBItem.DBDataType.String));
                        InsertList.Add(new DBItem("TARGETSTARTDATE", Request.Form["CreateDate"] + " " + Request.Form["CreateTime"], DBItem.DBDataType.DataTime));
                        InsertList.Add(new DBItem("TARGETID", TARGETID, DBItem.DBDataType.String));
                        InsertList.Add(new DBItem("TARGETDESC", Request.Form["TARGET" + TARGETID].ToString(), DBItem.DBDataType.String));
                        int erow = this.link.DBExecInsert("CPTARGETDTL", InsertList);
                        ALLTARGET += Request.Form["TARGET" + TARGETID].ToString() + "、";
                    }
                    else
                    {
                        ALLTARGET += Request.Form["TARGET" + TARGETID].ToString() + "、";
                    }
                }
                //如果是舊的目標被取消就押上停止日期
                for (int k = 0; k < OldTarget.Length; k++)
                {
                    if (!Array.Exists(NewTarget, element => element == OldTarget[k]))
                    {
                        string where = " RECORDID = '" + RECORDID + "' and TARGETID =  '" + OldTarget[k] + "'";
                        List<DBItem> updList = new List<DBItem>();
                        updList.Add(new DBItem("TARGETENDDATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                        int effRow = this.link.DBExecUpdate("CPTARGETDTL", updList, where);
                    }
                }

                ALLTARGET = ALLTARGET.Substring(0, ALLTARGET.Length - 1);
                CareRecordContent += "P:擬定";
            }
            //儲存護理措施
            if (Request.Form["MEASURE"] != null)
            {
                string[] OldMEASURE = { };
                OldMEASURE = Request.Form["OldMEASURE"].ToString().Split(',');
                string[] NewMEASURE = { };
                NewMEASURE = Request.Form["MEASURE"].ToString().Split(',');

                //先把舊有措施刪除(更改為將舊有措施加上結束日期
                //sql = "delete from CPMEASUREDTL where recordid ='" + RECORDID + "'";
                //this.link.DBExecSQL(sql);

                string ALLMEASURE = string.Empty;
                for (int j = 0; j < Request.Form["MEASURE"].ToString().Split(',').Length; j++)
                {
                    string MEASUREID = Request.Form["MEASURE"].ToString().Split(',').GetValue(j).ToString();
                    //如果是新的措施就新增
                    if (!Array.Exists(OldMEASURE, element => element == MEASUREID))
                    {
                        string Serial = base.creatid("CPMEASUREDTL", userinfo.EmployeesNo.ToString().Trim(), ptinfo.FeeNo.ToString().Trim(), j.ToString());
                        List<DBItem> InsertList = new List<DBItem>();
                        InsertList.Add(new DBItem("SERIAL", Serial, DBItem.DBDataType.String));
                        InsertList.Add(new DBItem("RECORDID", RECORDID, DBItem.DBDataType.String));
                        InsertList.Add(new DBItem("MEASUREID", MEASUREID, DBItem.DBDataType.String));
                        InsertList.Add(new DBItem("MEASUREDESC", Request.Form["MEASURE" + MEASUREID].ToString(), DBItem.DBDataType.String));
                        InsertList.Add(new DBItem("MEASURESTARTDATE", Request.Form["CreateDate"] + " " + Request.Form["CreateTime"], DBItem.DBDataType.DataTime));
                        int erow = this.link.DBExecInsert("CPMEASUREDTL", InsertList);
                        ALLMEASURE += Request.Form["MEASURE" + MEASUREID].ToString() + "、";
                    }
                    else
                    {
                        string where = " RECORDID = '" + RECORDID + "' and MEASUREID =  '" + MEASUREID + "'";
                        List<DBItem> updList = new List<DBItem>();
                        updList.Add(new DBItem("MEASUREDESC", Request.Form["MEASURE" + MEASUREID].ToString(), DBItem.DBDataType.String));
                        int effRow = this.link.DBExecUpdate("CPMEASUREDTL", updList, where);
                        ALLMEASURE += Request.Form["MEASURE" + MEASUREID].ToString() + "、";
                    }
                }
                //如果是舊的措施被取消就押上停止日期
                for (int k = 0; k < OldMEASURE.Length; k++)
                {
                    if (!Array.Exists(NewMEASURE, element => element == OldMEASURE[k]))
                    {
                        string where = " RECORDID = '" + RECORDID + "' and MEASUREID =  '" + OldMEASURE[k] + "'";
                        List<DBItem> updList = new List<DBItem>();
                        updList.Add(new DBItem("MEASUREENDDATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                        int effRow = this.link.DBExecUpdate("CPMEASUREDTL", updList, where);
                    }
                }


                ALLMEASURE = ALLMEASURE.Substring(0, ALLMEASURE.Length - 1);
                CareRecordContent += ALLMEASURE + "之護理措施。";
            }
            //轉帶護理紀錄
            if (Request.Form["R"] == "Y")
            {
                Insert_CareRecord(Request.Form["CreateDate"] + " " + Request.Form["CreateTime"] + ":" + DateTime.Now.ToString("ss"), RECORDID + DateTime.Now.ToString("yyyyMMddHHmmss"), "#" + CarePlanCount + " " + Request.Form["f_item"], CareRecordContent, "", "", "", "", "CAREPLANMASTER", HistoryID);
            }
            else
            {
                Insert_CareRecord(Request.Form["CreateDate"] + " " + Request.Form["CreateTime"] + ":" + DateTime.Now.ToString("ss"), RECORDID + DateTime.Now.ToString("yyyyMMddHHmmss"), "重整 #" + CarePlanCount + " " + Request.Form["f_item"], CareRecordContent, "", "", "", "", "CAREPLANMASTER");
            }

            Response.Write("<script>alert('重整成功!');location.href='../NurseCarePlan/Index';</script>");
            return new EmptyResult();
        }
        #endregion
    }
}
