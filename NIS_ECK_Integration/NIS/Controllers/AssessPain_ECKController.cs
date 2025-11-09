using Newtonsoft.Json;
using NIS.Data;
using NIS.Models;
using NIS.UtilTool;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;

namespace NIS.Controllers
{
    public class AssessPain_ECKController : BaseController
    {
        private DBConnector link = new DBConnector();

        //建構子
        public AssessPain_ECKController()
        {
            this.link = new DBConnector();
        }

        public ActionResult Index(string Msg)
        {//判斷有無病人session
            if (Session["PatInfo"] != null)
            {
                ViewBag.Msg = Msg;
                ViewBag.userno = base.userinfo.EmployeesNo;
                return View();
            }
            else
            {
                Response.Write("<script>alert('請重新選擇病患');</script>");
                return new EmptyResult();
            }
        }

        #region 初始疼痛部位

        /// <summary>
        /// 搜尋 NIS_DATA_PAIN_POSITION
        /// </summary>
        /// <param name="PainCode"></param>
        /// <returns></returns>
        public string SelectPainPosition(string PainCode, string FeeNo, string HISVIEW)
        {
            try
            {
                DateTime before_day = DateTime.Now;
                List<InHistory> IpdList = null;
                if (string.IsNullOrWhiteSpace(FeeNo))
                {
                    if (Session["PatInfo"] != null)
                    {
                        FeeNo = "'" + base.ptinfo.FeeNo + "'";
                    }
                }
                else
                {
                    #region 初始疼痛HISVIEW
                    if (!string.IsNullOrEmpty(HISVIEW))
                    {
                        //優化需求: 7日內的急診檢傷的疼痛及疼痛評估資料加入到住院的疼痛評估的LIST
                        string feeno_group = "";
                        PatientInfo pi = null;
                        byte[] doByteCode = webService.GetPatientInfo(FeeNo.Trim());
                        if (doByteCode != null)
                        {
                            string ptinfoJosnArr = CompressTool.DecompressString(doByteCode);
                            pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                        }
                        string Charno = (pi == null) ? "" : pi.ChartNo;
                        doByteCode = webService.GetInHistory(Charno);
                        if (doByteCode != null)
                        {
                            string doJsonArr = CompressTool.DecompressString(doByteCode);
                            //DateTime before_day = DateTime.Now.AddDays(-7);
                            before_day = Convert.ToDateTime(pi.InDate).AddDays(-7);

                            IpdList = JsonConvert.DeserializeObject<List<InHistory>>(doJsonArr);
                            IpdList = IpdList.FindAll(x => x.outdate >= before_day);
                            if (IpdList.Count > 0)
                            {
                                feeno_group = "''";
                                foreach (var item in IpdList)
                                {
                                    feeno_group += ",'" + item.FeeNo + "'";
                                }
                                FeeNo = String.Join(",", feeno_group.Split(','));
                            }
                        }
                    }
                    #endregion
                }

                //確認是否有病人資料(有選取病人)
                if (FeeNo != null)
                {
                    List<Dictionary<string, string>> dt = new List<Dictionary<string, string>>();
                    Dictionary<string, string> Temp = null;

                    string sql = "SELECT FEE_NO, PAIN_CODE, RECORD_DATE, PAIN_POSITION, RECORD_JSON_DATA, UPDATE_USERNO, UPDATE_USERNAME, END_DATE "
                        + "FROM NIS_DATA_PAIN_POSITION "
                        + "WHERE FEE_NO in ( " + FeeNo + ") ";
                    if (!string.IsNullOrWhiteSpace(PainCode))
                    {
                        sql += "AND PAIN_CODE = '" + PainCode + "' ";
                    }
                    #region 初始疼痛HISVIEW
                    if (!string.IsNullOrEmpty(HISVIEW))
                    {
                        sql += "AND RECORD_DATE > '" + before_day.ToString("yyyy/MM/dd HH:mm:ss") + "'";
                    }
                    #endregion

                    sql += "ORDER BY RECORD_DATE DESC ";

                    DataTable Dt = link.DBExecSQL(sql);
                    if (Dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            Temp = new Dictionary<string, string>();
                            if (Dt.Rows[i]["FEE_NO"].ToString().Length > 15) //住院急診經由Feeno長度判斷
                            {
                                Temp["InType"] = "急";
                            }
                            else
                            {
                                Temp["InType"] = "住";
                            }
                            Temp["WorkType"] = "疼痛評估";
                            Temp["PainCode"] = Dt.Rows[i]["PAIN_CODE"].ToString();
                            Temp["RecordDate"] = Convert.ToDateTime(Dt.Rows[i]["RECORD_DATE"].ToString()).ToString("yyyy/MM/dd HH:mm");
                            Temp["PainPosition"] = Dt.Rows[i]["PAIN_POSITION"].ToString();
                            Temp["RecordData"] = Dt.Rows[i]["RECORD_JSON_DATA"].ToString();
                            Temp["UserName"] = Dt.Rows[i]["UPDATE_USERNAME"].ToString();
                            if (HISVIEW != "true")//HisView 無法修改
                            {
                                Temp["EditFlag"] = (Dt.Rows[i]["UPDATE_USERNO"].ToString() == base.userinfo.EmployeesNo) ? "Y" : "N";
                            }
                            Temp["EndDate"] = Dt.Rows[i]["END_DATE"].ToString();
                            Temp["UserNo"] = Dt.Rows[i]["UPDATE_USERNO"].ToString();
                            dt.Add(Temp);
                        }
                    }
                    #region 初始疼痛HISVIEW-生命徵象
                    if (!string.IsNullOrEmpty(HISVIEW))
                    {
                        string before7_day = before_day.ToString("yyyy-MM-dd HH:mm");
                        sql = "SELECT * FROM DATA_VITALSIGN ";
                        sql += " WHERE FEE_NO in ( " + FeeNo + ") AND VS_ITEM = 'ps'";
                        sql += " AND CREATE_DATE > to_date('" + before7_day + "','yyyy/MM/dd hh24:mi:ss')";

                        Dt = link.DBExecSQL(sql);
                        if (Dt.Rows.Count > 0)
                        {
                            for (int i = 0; i < Dt.Rows.Count; i++)
                            {
                                Temp = new Dictionary<string, string>();
                                if (Dt.Rows[i]["FEE_NO"].ToString().Length > 15) //住院急診經由Feeno長度判斷
                                {
                                    Temp["InType"] = "急";
                                }
                                else
                                {
                                    Temp["InType"] = "住";
                                }
                                Temp["WorkType"] = "生命徵象";
                                Temp["PainCode"] = Dt.Rows[i]["VS_ID"].ToString();
                                Temp["RecordDate"] = Convert.ToDateTime(Dt.Rows[i]["CREATE_DATE"].ToString()).ToString("yyyy/MM/dd HH:mm");
                                Temp["PainPosition"] = "";
                                Temp["RecordData"] = "";
                                Temp["UserName"] = "";
                                Temp["EditFlag"] = "N";
                                Temp["EndDate"] = "";
                                Temp["UserNo"] = "";
                                dt.Add(Temp);
                            }
                        }
                    }
                    #endregion
                    #region 初始疼痛HISVIEW-急診檢傷
                    if (!string.IsNullOrEmpty(HISVIEW))
                    {
                        string before7_day = before_day.ToString("yyyy-MM-dd HH:mm");
                        foreach (var item in IpdList)
                        {
                            byte[] TriageInfoByteCode = webService.GetTriageInfo(item.FeeNo);
                            if (TriageInfoByteCode != null)
                            {
                                string TriageInfoJosnArr = NIS.UtilTool.CompressTool.DecompressString(TriageInfoByteCode);
                                List<TriageInfo> TriageInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<List<TriageInfo>>(TriageInfoJosnArr);
                                foreach (var items in TriageInfo)
                                {
                                    Temp = new Dictionary<string, string>();
                                    if (item.FeeNo.Length > 15) //住院急診經由Feeno長度判斷
                                    {
                                        Temp["InType"] = "急";
                                    }
                                    else
                                    {
                                        Temp["InType"] = "住";
                                    }
                                    Temp["WorkType"] = "檢傷";
                                    Temp["PainCode"] = items.FeeNo.ToString();
                                    Temp["RecordDate"] = items.ADMIT_DATE + " " + items.ADMIT_TIME;
                                    Temp["PainPosition"] = "";
                                    Temp["RecordData"] = "";
                                    Temp["UserName"] = "";
                                    Temp["EditFlag"] = "N";
                                    Temp["EndDate"] = "";
                                    Temp["UserNo"] = "";
                                    dt.Add(Temp);
                                }
                            }
                        }
                    }
                    #endregion
                    dt = dt.OrderByDescending(d => d["RecordDate"]).ToList();
                    return JsonConvert.SerializeObject(dt);
                }
                else
                    return "";
            }
            catch (Exception ex)
            {

                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return "";
            }
        }

        /// <summary>
        /// 疼痛部位結案
        /// </summary>
        /// <param name="PainCode">代碼</param>
        /// <param name="EndDate">結案時間</param>
        /// <param name="EndReason">結案原因</param>
        public string SaveEndData(string PainCode, string EndDate, string EndReason)
        {
            bool Success = false;
            //確認是否有病人資料
            if(Session["PatInfo"] != null)
            {
                try
                {
                    List<DBItem> dbItem = new List<DBItem>();
                    dbItem.Add(new DBItem("END_DATE", EndDate, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("END_REASON", EndReason, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("UPDATE_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("UPDATE_USERNO", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("UPDATE_USERNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    if(this.link.DBExecUpdate("NIS_DATA_PAIN_POSITION", dbItem, "PAIN_CODE = '" + PainCode + "' AND FEE_NO = '" + base.ptinfo.FeeNo + "' ") == 1)
                        Success = true;
                }
                catch
                {
                    Success = false;
                }
            }
            if(Success)
                return "Y";
            else
                return "N";
        }

        //疼痛部位表單頁面
        public ActionResult PainPosition()
        {
            ViewData["ptinfo"] = base.ptinfo;
            ViewData["PositionList"] = SelectSysParams("PainPosition", ((base.ptinfo.Age < 18) ? "Child" : "Adult"));
            return View();
        }

        /// <summary>
        /// 儲存 NIS_DATA_PAIN_POSITION
        /// </summary>
        /// <param name="PainCode">編輯 PK</param>
        /// <param name="RecordTime">記錄時間</param>
        /// <param name="PainPosition">疼痛部位</param>
        /// <param name="Data">JSON OBJ</param>
        public string SavePainPosition(string PainCode, string RecordTime, string PainPosition, string Data)
        {
            bool Success = false;
            //確認是否有病人資料
            if(Session["PatInfo"] != null)
            {
                try
                {
                    List<DBItem> dbItem = new List<DBItem>();
                    dbItem.Add(new DBItem("RECORD_DATE", Convert.ToDateTime(RecordTime).ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("PAIN_POSITION", PainPosition, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("RECORD_JSON_DATA", Data, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("UPDATE_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("UPDATE_USERNO", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("UPDATE_USERNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    if(string.IsNullOrWhiteSpace(PainCode))
                    {
                        PainCode = creatid("NIS_DATA_PAIN_POSITION", base.userinfo.EmployeesNo, base.ptinfo.FeeNo, "0");
                        dbItem.Add(new DBItem("PAIN_CODE", PainCode, DBItem.DBDataType.String));
                        dbItem.Add(new DBItem("FEE_NO", base.ptinfo.FeeNo, DBItem.DBDataType.String));
                        dbItem.Add(new DBItem("CREATE_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                        dbItem.Add(new DBItem("CREATE_USERNO", userinfo.EmployeesNo, DBItem.DBDataType.String));
                        dbItem.Add(new DBItem("CREATE_USERNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                        if(this.link.DBExecInsert("NIS_DATA_PAIN_POSITION", dbItem) == 1)
                            Success = true;
                    }
                    else
                    {
                        if(this.link.DBExecUpdate("NIS_DATA_PAIN_POSITION", dbItem, "PAIN_CODE = '" + PainCode + "' AND FEE_NO = '" + base.ptinfo.FeeNo + "' ") == 1)
                            Success = true;
                    }
                }
                catch
                {
                    Success = false;
                }
            }
            if(Success)
            {
                List<Dictionary<string, string>> DataList = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(Data);
                string AssessType = DataList.Find(x => x["Name"] == "PainLevel")["Value"];

                if(AssessType.Equals("數字量表") || AssessType.Equals("臉譜量表"))
                {
                    string Word = PainPosition;
                    if(DataList.Exists(x => x["Name"] == "PainLevel_Number"))
                        Word += "疼痛強度" + DataList.Find(x => x["Name"] == "PainLevel_Number")["Value"] + "分";
                    else
                        Word += "疼痛強度" + DataList.Find(x => x["Name"] == "PainLevel_Face")["Value"] + "分";

                    Word += "之" + DataList.Find(x => x["Name"] == "PainNature")["Value"];
                    if(DataList.Exists(x => x["Name"] == "PainNature_Other"))
                        Word = Word.Substring(0, Word.Length - 2) + DataList.Find(x => x["Name"] == "PainNature_Other")["Value"];

                    if(DataList.Find(x => x["Name"] == "PainExtend")["Value"].Equals("是"))
                        Word += "，並延伸至" + DataList.Find(x => x["Name"] == "ExtendPositionItem")["Value"];

                    Word += "。病人對疼痛之反應為" + DataList.Find(x => x["Name"] == "PainReaction")["Value"];
                    if(DataList.Exists(x => x["Name"] == "PainReaction_Other"))
                        Word = Word.Substring(0, Word.Length - 2) + DataList.Find(x => x["Name"] == "PainReaction_Other")["Value"];

                    if(DataList.Find(x => x["Name"] == "PainStart")["Value"].Equals("清楚"))
                    {
                        Word += "，主訴疼痛從";
                        if(DataList.Find(x => x["Name"] == "PainStart_TimeUnit")["Value"] != "手術後")
                        {
                            Word += DataList.Find(x => x["Name"] == "PainStart_TimeNum")["Value"];
                        }
                        Word += DataList.Find(x => x["Name"] == "PainStart_TimeUnit")["Value"] + "開始";
                    }
                    else
                        Word += "，主訴疼痛從" + DataList.Find(x => x["Name"] == "PainStart")["Value"] + "開始";

                    Word += "，發生頻率：" + DataList.Find(x => x["Name"] == "PainFreq")["Value"];
                    if(DataList.Exists(x => x["Name"] == "PainFreq_Other"))
                        Word = Word.Substring(0, Word.Length - 2) + DataList.Find(x => x["Name"] == "PainFreq_Other")["Value"];

                    if(DataList.Find(x => x["Name"] == "PainDuration")["Value"].Equals("固定"))
                        Word += "，每次疼痛持續時間" + DataList.Find(x => x["Name"] == "PainDuration_TimeNum")["Value"] + DataList.Find(x => x["Name"] == "PainDuration_TimeUnit")["Value"];
                    else
                        Word += "，每次疼痛持續時間" + DataList.Find(x => x["Name"] == "PainDuration")["Value"];

                    Word += "，一天中最疼痛的時刻為" + DataList.Find(x => x["Name"] == "PainMustTime")["Value"];
                    if(DataList.Exists(x => x["Name"] == "PainMustTime_Other"))
                        Word = Word.Substring(0, Word.Length - 2) + DataList.Find(x => x["Name"] == "PainMustTime_Other")["Value"];

                    Word += "，疼痛已持續" + DataList.Find(x => x["Name"] == "PainContinue")["Value"] + "。"
                        + DataList.Find(x => x["Name"] == "PainRelieve")["Value"];
                    if(DataList.Exists(x => x["Name"] == "PainRelieve_Other"))
                    {
                        //Word = Word.Substring(0, Word.Length - 3);//下面一行 edis by jarvis 20160923
                        Word = Word.Substring(0, Word.Length - 4) + DataList.Find(x => x["Name"] == "PainRelieve_Other")["Value"];
                    }
                    Word += "可緩解疼痛";

                    Word += "，疼痛會伴隨" + DataList.Find(x => x["Name"] == "PainSymptomBy")["Value"];
                    if(DataList.Exists(x => x["Name"] == "PainSymptomBy_Other"))
                        Word = Word.Substring(0, Word.Length - 2) + DataList.Find(x => x["Name"] == "PainSymptomBy_Other")["Value"];
                    Word += "症狀";

                    Word += "，且因疼痛造成" + DataList.Find(x => x["Name"] == "PainEffect_Normal")["Value"]
                        + "及易" + DataList.Find(x => x["Name"] == "PainEffect_Relationship")["Value"];
                    if(DataList.Exists(x => x["Name"] == "PainEffect_Relationship_Other"))
                        Word = Word.Substring(0, Word.Length - 2) + DataList.Find(x => x["Name"] == "PainEffect_Relationship_Other")["Value"];
                    Word += "之影響。";

                    if(Upd_CareRecord(Convert.ToDateTime(RecordTime).ToString("yyyy/MM/dd HH:mm:00")
                        , PainCode, "初始疼痛-一般評估", "", Word, "", "", "", "PAIN_POSITION") == 0)
                    {
                        Insert_CareRecord(Convert.ToDateTime(RecordTime).ToString("yyyy/MM/dd HH:mm:00")
                            , PainCode, "初始疼痛-一般評估", "", Word, "", "", "", "PAIN_POSITION");
                    }
                }
                else
                {
                    string Word = "病人無法表達疼痛，意識" + DataList.Find(x => x["Name"] == "ConsciosLevel")["Value"];
                    Word += "，GCS：E" + DataList.Find(x => x["Name"] == "GCS_E")["Value"];
                    Word += "V" + DataList.Find(x => x["Name"] == "GCS_V")["Value"];
                    Word += "M" + DataList.Find(x => x["Name"] == "GCS_M")["Value"];

                    int ps_value = 0;
                    switch(AssessType)
                    {
                        case "困難評估(成人)":
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_Adult_Breath")["Value"].Substring(1, 1));
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_Adult_NoLG")["Value"].Substring(1, 1));
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_Adult_Face")["Value"].Substring(1, 1));
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_Adult_Body")["Value"].Substring(1, 1));
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_Adult_Appease")["Value"].Substring(1, 1));
                            break;
                        case "困難評估(兒童)":
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_Child_Face")["Value"].Substring(1, 1));
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_Child_Legs")["Value"].Substring(1, 1));
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_Child_Activity")["Value"].Substring(1, 1));
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_Child_Crying")["Value"].Substring(1, 1));
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_Child_Consolability")["Value"].Substring(1, 1));
                            break;
                        case "困難評估(新生兒)":
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_Baby_Crying")["Value"].Substring(1, 1));
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_Baby_FiO2")["Value"].Substring(1, 1));
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_Baby_VT")["Value"].Substring(1, 1));
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_Baby_Expression")["Value"].Substring(1, 1));
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_Baby_Sleepless")["Value"].Substring(1, 1));
                            break;
                        case "CPOT評估(加護單位)":
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_CPOT_Face")["Value"].Substring(1, 1));
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_CPOT_Body")["Value"].Substring(1, 1));
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_CPOT_Muscle")["Value"].Substring(1, 1));
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_CPOT_Breath")["Value"].Substring(1, 1));
                            break;
                    }
                    if (AssessType.Equals("CPOT評估(加護單位)"))
                    {
                        Word += "，依疼痛CPOT評估方法評估病人疼痛" + ps_value.ToString() + "分";

                        if (Upd_CareRecord(Convert.ToDateTime(RecordTime).ToString("yyyy/MM/dd HH:mm:00")
                            , PainCode, "初始疼痛-CPOT評估", "", "", Word, "", "", "PAIN_POSITION") == 0)
                        {
                            Insert_CareRecord(Convert.ToDateTime(RecordTime).ToString("yyyy/MM/dd HH:mm:00")
                                , PainCode, "初始疼痛-CPOT評估", "", "", Word, "", "", "PAIN_POSITION");
                        }
                    }
                    else
                    {
                        Word += "，依疼痛困難評估方法評估病人疼痛" + ps_value.ToString() + "分";

                        if (Upd_CareRecord(Convert.ToDateTime(RecordTime).ToString("yyyy/MM/dd HH:mm:00")
                            , PainCode, "初始疼痛-困難評估", "", "", Word, "", "", "PAIN_POSITION") == 0)
                        {
                            Insert_CareRecord(Convert.ToDateTime(RecordTime).ToString("yyyy/MM/dd HH:mm:00")
                                , PainCode, "初始疼痛-困難評估", "", "", Word, "", "", "PAIN_POSITION");
                        }
                    }
                }
                return "Y";
            }
            else
                return "N";
        }

        //刪除傷口
        [HttpPost]
        public string DeletePainPosition(string PainCode)
        {
            DataTable dt = new DataTable();
            string where = "FEE_NO = '" + base.ptinfo.FeeNo + "' AND PAIN_CODE = '" + PainCode + "' ";
            int erow = this.link.DBExecDelete("NIS_DATA_PAIN_POSITION", where);
            string sql = "SELECT * FROM NIS_DATA_PAIN_RECORD WHERE FEE_NO = '" + base.ptinfo.FeeNo + "' AND PAIN_CODE = '" + PainCode + "'  ";
            this.link.DBExecSQL(sql, ref dt);

            if(erow > 0)
            {
                where = "FEE_NO = '" + base.ptinfo.FeeNo + "' AND PAIN_CODE = '" + PainCode + "' ";
                erow = this.link.DBExecDelete("NIS_DATA_PAIN_RECORD", where);

                if(erow >= 0)
                {
                    try
                    {
                        base.Del_CareRecord(PainCode, "PAIN_POSITION");
                        //base.Del_CareRecord("", PainCode); 不知何用
                        if (dt.Rows.Count > 0)
                        {
                            for(int i = 0; i< dt.Rows.Count; i++)
                            {
                                string recordID = dt.Rows[i]["PAIN_RECORD_CODE"].ToString();
                                base.Del_CareRecord(recordID, PainCode);
                            }
                        }
                    }
                    catch { }
                }
                return "Y";
            }
            else
                return "N";
        }

        #endregion

        #region 持續疼痛

        /// <summary>
        /// 搜尋 NIS_DATA_PAIN_RECORD
        /// </summary>
        /// <param name="PainCode">NIS_DATA_PAIN_POSITION PK代碼</param>
        /// <param name="PainRecordCode">NIS_DATA_PAIN_RECORD PK代碼</param>
        public string SelectPainPositionRecord(string PainCode, string PainRecordCode, string FeeNo, string SourceType ="")
        {
            try {
                string feeno_group = "";
                DateTime before_day = DateTime.Now;
                List<InHistory> IpdList = null;
                if (string.IsNullOrWhiteSpace(FeeNo))
                {
                    if (Session["PatInfo"] != null)
                    {
                        FeeNo = "'" + base.ptinfo.FeeNo + "'";
                    }
                }
                else
                {
                    #region 持續疼痛HISVIEW
                    if (!string.IsNullOrEmpty(SourceType))
                    {
                        //優化需求: 7日內的急診檢傷的疼痛及疼痛評估資料加入到住院的疼痛評估的LIST
                        PatientInfo pi = null;
                        byte[] doByteCode = webService.GetPatientInfo(FeeNo.Trim());
                        if (doByteCode != null)
                        {
                            string ptinfoJosnArr = CompressTool.DecompressString(doByteCode);
                            pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                        }
                        string Charno = (pi == null) ? "" : pi.ChartNo;
                        doByteCode = webService.GetInHistory(Charno);
                        if (doByteCode != null)
                        {
                            string doJsonArr = CompressTool.DecompressString(doByteCode);
                            //DateTime before_day = DateTime.Now.AddDays(-7);
                            before_day = Convert.ToDateTime(pi.InDate).AddDays(-7);

                            IpdList = JsonConvert.DeserializeObject<List<InHistory>>(doJsonArr);
                            IpdList = IpdList.FindAll(x => x.outdate >= before_day);
                            if (IpdList.Count > 0)
                            {
                                feeno_group = "''";
                                foreach (var item in IpdList)
                                {
                                    feeno_group += ",'" + item.FeeNo + "'";
                                }
                                FeeNo = String.Join(",", feeno_group.Split(','));
                            }
                        }
                    }
                    #endregion
                }

                //確認是否有病人資料(有選取病人)
                if (FeeNo != null)
                {
                    List<Dictionary<string, string>> dt = new List<Dictionary<string, string>>();
                    Dictionary<string, string> Temp = null;

                    string sql = "SELECT PAIN_CODE, PAIN_RECORD_CODE, RECORD_DATE, RECORD_JSON_DATA, UPDATE_USERNAME, CREATE_USERNO "
                        + ", (SELECT PAIN_POSITION FROM NIS_DATA_PAIN_POSITION WHERE PAIN_CODE = A.PAIN_CODE) PAIN_POSITION "
                        + "FROM NIS_DATA_PAIN_RECORD A "
                        + "WHERE FEE_NO in ( " + FeeNo + ") ";

                    if (!string.IsNullOrWhiteSpace(PainRecordCode))
                    {
                        sql += "AND PAIN_RECORD_CODE = '" + PainRecordCode + "' ";
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(PainCode))
                        {
                            sql += "AND PAIN_CODE = '" + PainCode + "' AND RECORD_DATE = (SELECT MAX(RECORD_DATE) FROM NIS_DATA_PAIN_RECORD "
                            + "WHERE FEE_NO in ( " + FeeNo + ") AND PAIN_CODE = '" + PainCode + "') ";
                        }
                        else
                        {
                            sql += "AND PAIN_CODE IN (SELECT PAIN_CODE FROM NIS_DATA_PAIN_POSITION "
                            + "WHERE FEE_NO in ( " + FeeNo + ") ";
                            #region 持續疼痛HISVIEW
                            if (!string.IsNullOrEmpty(SourceType))
                            {
                                sql += "AND RECORD_DATE > '" + before_day.ToString("yyyy/MM/dd HH:mm:ss") + "'";
                            }
                            #endregion
                            sql += ")";
                        }
                    }
                    sql += "ORDER BY RECORD_DATE DESC ";
                    
                    DataTable Dt = link.DBExecSQL(sql);
                    if (Dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            Temp = new Dictionary<string, string>();
                            Temp["PainCode"] = Dt.Rows[i]["PAIN_CODE"].ToString();
                            Temp["PainRecordCode"] = Dt.Rows[i]["PAIN_RECORD_CODE"].ToString();
                            Temp["RecordDate"] = Convert.ToDateTime(Dt.Rows[i]["RECORD_DATE"].ToString()).ToString("yyyy/MM/dd HH:mm");
                            Temp["PainPosition"] = Dt.Rows[i]["PAIN_POSITION"].ToString();
                            Temp["RecordData"] = Dt.Rows[i]["RECORD_JSON_DATA"].ToString();
                            Temp["UserName"] = Dt.Rows[i]["UPDATE_USERNAME"].ToString();
                            Temp["UserNO"] = Dt.Rows[i]["CREATE_USERNO"].ToString();
                            dt.Add(Temp);
                        }
                    }
                    //reader.Close();
                    //reader.Dispose();
                    #region 持續疼痛HISVIEW-生命徵象
                    if (!string.IsNullOrEmpty(SourceType))
                    {
                        string before7_day = before_day.ToString("yyyy-MM-dd HH:mm");
                        sql = "SELECT * FROM DATA_VITALSIGN ";
                        sql += " WHERE FEE_NO in ( " + FeeNo + ") AND VS_ITEM = 'ps'";
                        sql += " AND CREATE_DATE > to_date('" + before7_day + "','yyyy/MM/dd hh24:mi:ss')";

                        Dt = link.DBExecSQL(sql);
                        if (Dt.Rows.Count > 0)
                        {
                            for (int i = 0; i < Dt.Rows.Count; i++)
                            {
                                List<PainItem> Data = new List<PainItem>();
                                Data.Add(new PainItem("PainLevel", Dt.Rows[i]["VS_PART"].ToString()));
                                Regex rgx = new Regex(@"^([(]\d+[)])|^(\d+)");
                                int ps_value = 0;
                                foreach (string ps in Dt.Rows[i]["VS_RECORD"].ToString().Split('|'))
                                {
                                    if (ps != "")
                                    {
                                        ps_value += int.Parse(rgx.Match(ps).ToString().Replace("(", "").Replace(")", ""));
                                    }
                                }
                                Data.Add(new PainItem("PainLevel_Number", ps_value.ToString()));
                                string RecordData = JsonConvert.SerializeObject(Data);
                                Temp = new Dictionary<string, string>();
                                Temp["PainCode"] = Dt.Rows[i]["VS_ID"].ToString();
                                Temp["PainRecordCode"] = "";
                                Temp["RecordDate"] = Convert.ToDateTime(Dt.Rows[i]["CREATE_DATE"].ToString()).ToString("yyyy/MM/dd HH:mm");
                                Temp["PainPosition"] = "";
                                Temp["RecordData"] = RecordData;
                                Temp["UserName"] = "";
                                Temp["UserNO"] = "";
                                dt.Add(Temp);
                            }
                        }
                    }
                    #endregion
                    #region 初始疼痛HISVIEW-急診檢傷
                    if (!string.IsNullOrEmpty(SourceType))
                    {
                        string before7_day = before_day.ToString("yyyy-MM-dd HH:mm");
                        foreach (var item in IpdList)
                        {
                            byte[] TriageInfoByteCode = webService.GetTriageInfo(item.FeeNo);
                            if (TriageInfoByteCode != null)
                            {
                                string TriageInfoJosnArr = NIS.UtilTool.CompressTool.DecompressString(TriageInfoByteCode);
                                List<TriageInfo> TriageInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<List<TriageInfo>>(TriageInfoJosnArr);
                                foreach (var items in TriageInfo)
                                {
                                    var VAS = items.VAS.Split(' ');
                                    List<PainItem> Data = new List<PainItem>();
                                    Data.Add(new PainItem("PainLevel", VAS[0]));
                                    Data.Add(new PainItem("PainLevel_Number", VAS[1]));
                                    string RecordData = JsonConvert.SerializeObject(Data);
                                    Temp = new Dictionary<string, string>();
                                    if (item.FeeNo.Length > 15) //住院急診經由Feeno長度判斷
                                    {
                                        Temp["InType"] = "急";
                                    }
                                    else
                                    {
                                        Temp["InType"] = "住";
                                    }
                                    Temp["WorkType"] = "檢傷";
                                    Temp["PainCode"] = items.FeeNo.ToString();
                                    Temp["PainRecordCode"] = "";
                                    Temp["RecordDate"] = items.ADMIT_DATE + " " + items.ADMIT_TIME;
                                    Temp["PainPosition"] = "";
                                    Temp["RecordData"] = RecordData;
                                    Temp["UserName"] = "";
                                    Temp["UserNO"] = "";
                                    dt.Add(Temp);
                                }
                            }
                        }
                    }
                    #endregion
                    dt = dt.OrderByDescending(d => d["RecordDate"]).ToList();
                    return JsonConvert.SerializeObject(dt);
                }
                else
                    return "";
            }
            catch (Exception ex)
            {

                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return "";
            }
}

        public Comparison<Dictionary<string, string>> compare(Dictionary<string, string> dic)
        {
          
            return null;
        }

        //持續疼痛表單頁面
        public ActionResult PainPositionRecord(string PainPosition)
        {
            try
            {
                List<SelectListItem> PainAbnormalList = new List<SelectListItem>();
                string[] Temp = null;
                string sql = "SELECT ITEM FROM NIS_SYS_VITALSIGN_OPTION "
                    + "WHERE MODEL_ID = 'pain_abnormal_not_med' ";
                DataTable Dt = link.DBExecSQL(sql);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        Temp = Dt.Rows[i]["ITEM"].ToString().Split('|');
                        foreach (string word in Temp)
                        {
                            PainAbnormalList.Add(new SelectListItem
                            {
                                Text = word,
                                Value = word
                            });
                        }
                    }
                }
                ViewData["ptinfo"] = base.ptinfo;
                ViewData["PainAbnormalList"] = PainAbnormalList;
                ViewData["RelievePainMed"] = SelectSysParams("RelievePain", "Med");
                ViewData["RelievePainSideEffect"] = SelectSysParams("RelievePain", "SideEffect");
                ViewBag.PainPosition = PainPosition;
            }
            catch (Exception ex)
            {

                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }
            return View();
        }

        /// <summary>
        /// 儲存 NIS_DATA_PAIN_RECORD
        /// </summary>
        /// <param name="PainCode">父層 PK</param>
        /// <param name="PainRecordCode">編輯 PK</param>
        /// <param name="RecordTime">記錄時間</param>
        /// <param name="PainPosition">疼痛部位</param>
        /// <param name="Data">JSON OBJ</param>
        public string SavePainPositionRecord(string PainCode, string PainRecordCode, string RecordTime, string PainPosition, string Data)
        {
            bool Success = false;
            //確認是否有病人資料
            if(Session["PatInfo"] != null)
            {
                try
                {
                    List<DBItem> dbItem = new List<DBItem>();
                    dbItem.Add(new DBItem("RECORD_DATE", Convert.ToDateTime(RecordTime).ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("PAIN_POSITION", PainPosition, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("RECORD_JSON_DATA", Data, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("UPDATE_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("UPDATE_USERNO", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("UPDATE_USERNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    if(string.IsNullOrWhiteSpace(PainRecordCode))
                    {
                        PainRecordCode = creatid("NIS_DATA_PAIN_RECORD", base.userinfo.EmployeesNo, base.ptinfo.FeeNo, "0");
                        dbItem.Add(new DBItem("PAIN_CODE", PainCode, DBItem.DBDataType.String));
                        dbItem.Add(new DBItem("PAIN_RECORD_CODE", PainRecordCode, DBItem.DBDataType.String));
                        dbItem.Add(new DBItem("FEE_NO", base.ptinfo.FeeNo, DBItem.DBDataType.String));
                        dbItem.Add(new DBItem("CREATE_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                        dbItem.Add(new DBItem("CREATE_USERNO", userinfo.EmployeesNo, DBItem.DBDataType.String));
                        dbItem.Add(new DBItem("CREATE_USERNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                        if(this.link.DBExecInsert("NIS_DATA_PAIN_RECORD", dbItem) == 1)
                            Success = true;
                    }
                    else
                    {
                        if(this.link.DBExecUpdate("NIS_DATA_PAIN_RECORD", dbItem, "PAIN_CODE = '" + PainCode + "' AND PAIN_RECORD_CODE = '" + PainRecordCode + "' AND FEE_NO = '" + base.ptinfo.FeeNo + "' ") == 1)
                            Success = true;
                    }
                }
                catch
                {
                    Success = false;
                }
            }
            if(Success)
            {

                List<Dictionary<string, string>> DataList = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(Data);
                string AssessType = DataList.Find(x => x["Name"] == "PainLevel")["Value"];

                if(AssessType.Equals("數字量表") || AssessType.Equals("臉譜量表"))
                {
                    string Word_S = PainPosition;
                    if(DataList.Exists(x => x["Name"] == "PainLevel_Number"))
                        Word_S += "疼痛強度" + DataList.Find(x => x["Name"] == "PainLevel_Number")["Value"] + "分";
                    else
                        Word_S += "疼痛強度" + DataList.Find(x => x["Name"] == "PainLevel_Face")["Value"] + "分";

                    Word_S += "之" + DataList.Find(x => x["Name"] == "PainNature")["Value"];
                    if(DataList.Exists(x => x["Name"] == "PainNature_Other"))
                        Word_S = Word_S.Substring(0, Word_S.Length - 2) + DataList.Find(x => x["Name"] == "PainNature_Other")["Value"];
                    Word_S += "。";

                    string Word_I = string.Empty;
                    if((DataList.Exists(x => x["Name"] == "PainMedItem") && DataList.Find(x => x["Name"] == "PainMedItem")["Value"] != "") || (DataList.Exists(x => x["Name"] == "Txt_RelievePainMedItem_other") && DataList.Find(x => x["Name"] == "Txt_RelievePainMedItem_other")["Value"] != ""))
                    {
                        Word_I += "依醫囑給予";
                    }
                    if(DataList.Exists(x => x["Name"] == "PainMedItem") && DataList.Find(x => x["Name"] == "PainMedItem")["Value"] != "")
                    {
                        //string med = DataList.Find(x => x["Name"] == "PainMedItem")["Value"];
                        //if (med != "")
                        //{
                        //    var medArr = med.Split('|');
                        //    Word_I += medArr[1] + " ";
                        //    Word_I += medArr[2] + " ";
                        //    Word_I += medArr[3] + " ";
                        //}


                        var tempArr = new List<string>();
                        var medTempArr = new List<string>();

                        foreach (var temp in DataList)
                        {
                            if (temp["Name"] == "PainMedItem")
                            {
                                tempArr.Add(temp["Value"]);
                            }
                        }
                        for(int i = 0; i < tempArr.Count; i++)
                        {
                            string med = tempArr[i];
                            string tempmedInfo = "";
                            if (med != "")
                            {
                                var medArr = med.Split('|');
                                tempmedInfo += medArr[1] + " ";
                                tempmedInfo += medArr[2] + " ";
                                tempmedInfo += medArr[3] + " ";
                                medTempArr.Add(tempmedInfo);
                            }
                        }
                        if(medTempArr.Count > 0)
                        {
                            Word_I += String.Join("、", medTempArr);
                        }
                    }
                    if (DataList.Exists(x => x["Name"] == "Txt_RelievePainMedItem_other") && DataList.Find(x => x["Name"] == "Txt_RelievePainMedItem_other")["Value"] != "")
                    {
                        Word_I += "其他：" + DataList.Find(x => x["Name"] == "Txt_RelievePainMedItem_other")["Value"] + " ";
                    }
                    if((DataList.Exists(x => x["Name"] == "RelievePainMedItem") && DataList.Find(x => x["Name"] == "RelievePainMedItem")["Value"] != "") || (DataList.Exists(x => x["Name"] == "Txt_RelievePainMedItem_other") && DataList.Find(x => x["Name"] == "Txt_RelievePainMedItem_other")["Value"] != ""))
                    {
                        Word_I = Word_I.Substring(0, Word_I.Length - 1) + "使用，並監測藥物之作用及副作用。";
                    }
                    //Word_I += "給予" + DataList.Find(x => x["Name"] == "PainAbnormalItem")["Value"];
                    if(DataList.Exists(x => x["Name"] == "PainAbnormalItem_Other"))
                        Word_I = Word_I.Substring(0, Word_I.Length - 2) + DataList.Find(x => x["Name"] == "PainAbnormalItem_Other")["Value"];
                    Word_I += "等措施協助緩解病患疼痛問題。";

                    string Word_E = "病人現意識狀態為" + DataList.Find(x => x["Name"] == "ConsciosLevel")["Value"];
                    Word_E += "，GCS：E" + DataList.Find(x => x["Name"] == "GCS_E")["Value"];
                    Word_E += "V" + DataList.Find(x => x["Name"] == "GCS_V")["Value"];
                    Word_E += "M" + DataList.Find(x => x["Name"] == "GCS_M")["Value"];

                    if(DataList.Exists(x => x["Name"] == "RelievePainSideEffectItem"))
                    {
                        Word_E += "，病人出現" + DataList.Find(x => x["Name"] == "RelievePainSideEffectItem")["Value"];
                        if(DataList.Exists(x => x["Name"] == "RelievePainSideEffectItem_Other"))
                            Word_E = Word_E.Substring(0, Word_E.Length - 2) + DataList.Find(x => x["Name"] == "RelievePainSideEffectItem_Other")["Value"];
                        Word_E += "等治療副作用。";
                    }
                    else
                        Word_E += "。";

                    if(DataList.Find(x => x["Name"] == "SpiritualityAssess")["Value"].Equals("需要"))
                    {
                        Word_E += "靈性方面：病人 " + DataList.Find(x => x["Name"] == "Mood_Thermometer_Q1")["Value"] + " 感覺緊張不安";
                        Word_E += "，" + DataList.Find(x => x["Name"] == "Mood_Thermometer_Q2")["Value"] + " 感覺憂鬱、心情低落";
                        Word_E += "，" + DataList.Find(x => x["Name"] == "Mood_Thermometer_Q3")["Value"] + " 覺得容易苦惱或動怒";
                        Word_E += "，" + DataList.Find(x => x["Name"] == "Mood_Thermometer_Q4")["Value"] + " 覺得比不上別人";
                        Word_E += "，" + DataList.Find(x => x["Name"] == "Mood_Thermometer_Q5")["Value"] + " 睡眠困難。譬如難以入睡、易睡或早醒";

                        string Social = string.Empty , Spirituality = string.Empty;
                        if (DataList.Exists(x => x["Name"] == "Social_Issues_Patient_Y"))
                        {
                            Social = DataList.Find(x => x["Name"] == "Social_Issues_Patient_Y")["Value"];
                            if(DataList.Exists(x => x["Name"] == "Social_Issues_Patient_Y_Other"))
                                Social = DataList.Find(x => x["Name"] == "Social_Issues_Patient_Y_Other")["Value"];


                        }
                        if (DataList.Exists(x => x["Name"] == "Spirituality_eligion_Patient_Y"))
                        {
                            Spirituality = DataList.Find(x => x["Name"] == "Spirituality_eligion_Patient_Y")["Value"];
                            if (DataList.Exists(x => x["Name"] == "Spirituality_eligion_Patient_Y_Other"))
                                Spirituality = DataList.Find(x => x["Name"] == "Spirituality_eligion_Patient_Y_Other")["Value"];
                        }
                        if (!string.IsNullOrWhiteSpace(Social) && !string.IsNullOrWhiteSpace(Spirituality))
                            Word_E += "，且有" + Social + "及" + Spirituality + "之情況。";
                        else if (!string.IsNullOrWhiteSpace(Social))
                            Word_E += "，且有" + Social + "之情況。";
                        else if (!string.IsNullOrWhiteSpace(Spirituality))
                            Word_E += "，且有" + Spirituality + "之情況。";

                        Social = string.Empty;
                        Spirituality = string.Empty;
                        if (DataList.Exists(x => x["Name"] == "Social_Issues_Family_Y"))
                        {
                            Social = DataList.Find(x => x["Name"] == "Social_Issues_Family_Y")["Value"];
                            if(DataList.Exists(x => x["Name"] == "Social_Issues_Family_Other"))
                                Social = DataList.Find(x => x["Name"] == "Social_Issues_Family_Other")["Value"];


                        }
                        if (DataList.Exists(x => x["Name"] == "Spirituality_eligion_Family_Y"))
                        {
                            Spirituality = DataList.Find(x => x["Name"] == "Spirituality_eligion_Family_Y")["Value"];
                            if (DataList.Exists(x => x["Name"] == "Spirituality_eligion_Family_Other"))
                                Spirituality = DataList.Find(x => x["Name"] == "Spirituality_eligion_Family_Other")["Value"];

                        }

                        if (!string.IsNullOrWhiteSpace(Social) && !string.IsNullOrWhiteSpace(Spirituality))
                            Word_E += "家屬亦有" + Social + "及" + Spirituality + "之情況。";
                        else if (!string.IsNullOrWhiteSpace(Social))
                            Word_E += "家屬亦有" + Social + "之情況。";
                        else if (!string.IsNullOrWhiteSpace(Spirituality))
                            Word_E += "家屬亦有" + Spirituality + "之情況。";

                    }

                    if (Upd_CareRecord(Convert.ToDateTime(RecordTime).ToString("yyyy/MM/dd HH:mm:59")
                        , PainRecordCode, "持續疼痛-一般評估", "", Word_S, "", Word_I, Word_E, PainCode) == 0)
                    {
                        Insert_CareRecord(Convert.ToDateTime(RecordTime).ToString("yyyy/MM/dd HH:mm:59")
                            , PainRecordCode, "持續疼痛-一般評估", "", Word_S, "", Word_I, Word_E, PainCode);
                    }
                }
                else
                {
                    int ps_value = 0;
                    switch(AssessType)
                    {
                        case "困難評估(成人)":
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_Adult_Breath")["Value"].Substring(1, 1));
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_Adult_NoLG")["Value"].Substring(1, 1));
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_Adult_Face")["Value"].Substring(1, 1));
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_Adult_Body")["Value"].Substring(1, 1));
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_Adult_Appease")["Value"].Substring(1, 1));
                            break;
                        case "困難評估(兒童)":
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_Child_Face")["Value"].Substring(1, 1));
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_Child_Legs")["Value"].Substring(1, 1));
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_Child_Activity")["Value"].Substring(1, 1));
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_Child_Crying")["Value"].Substring(1, 1));
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_Child_Consolability")["Value"].Substring(1, 1));
                            break;
                        case "困難評估(新生兒)":
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_Baby_Crying")["Value"].Substring(1, 1));
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_Baby_FiO2")["Value"].Substring(1, 1));
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_Baby_VT")["Value"].Substring(1, 1));
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_Baby_Expression")["Value"].Substring(1, 1));
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_Baby_Sleepless")["Value"].Substring(1, 1));
                            break;
                        case "CPOT評估(加護單位)":
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_CPOT_Face")["Value"].Substring(1, 1));
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_CPOT_Body")["Value"].Substring(1, 1));
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_CPOT_Muscle")["Value"].Substring(1, 1));
                            ps_value += int.Parse(DataList.Find(x => x["Name"] == "PainLevel_CPOT_Breath")["Value"].Substring(1, 1));
                            break;
                    }
                    string Word_I = string.Empty;
                    if((DataList.Exists(x => x["Name"] == "RelievePainMedItem") && DataList.Find(x => x["Name"] == "RelievePainMedItem")["Value"] != "") || (DataList.Exists(x => x["Name"] == "Txt_RelievePainMedItem_other") && DataList.Find(x => x["Name"] == "Txt_RelievePainMedItem_other")["Value"] != ""))
                    {
                        Word_I += "依醫囑給予";
                    }
                    if(DataList.Exists(x => x["Name"] == "RelievePainMedItem") && DataList.Find(x => x["Name"] == "RelievePainMedItem")["Value"] != "")
                    {
                        //Word_I += "依醫囑給予";
                        foreach(string MedID in DataList.Find(x => x["Name"] == "RelievePainMedItem")["Value"].Split(','))
                        {
                            Word_I += DataList.Find(x => x["Name"] == "RelievePainMedItem_Name_" + MedID)["Value"] + " ";
                            Word_I += DataList.Find(x => x["Name"] == "RelievePainMedItem_Amount_" + MedID)["Value"] + " ";
                            Word_I += DataList.Find(x => x["Name"] == "RelievePainMedItem_Way_" + MedID)["Value"] + "、";
                        }
                        //Word_I = Word_I.Substring(0, Word_I.Length - 1) + "使用，並監測藥物之作用及副作用。";
                    }
                    if(DataList.Exists(x => x["Name"] == "Txt_RelievePainMedItem_other") && DataList.Find(x => x["Name"] == "Txt_RelievePainMedItem_other")["Value"] != "")
                    {
                        Word_I += "其他：" + DataList.Find(x => x["Name"] == "Txt_RelievePainMedItem_other")["Value"] + " ";
                    }
                    if((DataList.Exists(x => x["Name"] == "RelievePainMedItem") && DataList.Find(x => x["Name"] == "RelievePainMedItem")["Value"] != "") || (DataList.Exists(x => x["Name"] == "Txt_RelievePainMedItem_other") && DataList.Find(x => x["Name"] == "Txt_RelievePainMedItem_other")["Value"] != ""))
                    {
                        Word_I = Word_I.Substring(0, Word_I.Length - 1) + "使用，並監測藥物之作用及副作用。";
                    }

                    Word_I += "給予" + DataList.Find(x => x["Name"] == "PainAbnormalItem")["Value"];
                    if(DataList.Exists(x => x["Name"] == "PainAbnormalItem_Other"))
                        Word_I = Word_I.Substring(0, Word_I.Length - 2) + DataList.Find(x => x["Name"] == "PainAbnormalItem_Other")["Value"];
                    Word_I += "等措施協助緩解病患疼痛問題。";

                    string Word_E = "病人現意識狀態為" + DataList.Find(x => x["Name"] == "ConsciosLevel")["Value"];
                    Word_E += "，GCS：E" + DataList.Find(x => x["Name"] == "GCS_E")["Value"];
                    Word_E += "V" + DataList.Find(x => x["Name"] == "GCS_V")["Value"];
                    Word_E += "M" + DataList.Find(x => x["Name"] == "GCS_M")["Value"];

                    if(DataList.Exists(x => x["Name"] == "RelievePainSideEffectItem"))
                    {
                        Word_E += "，病人出現" + DataList.Find(x => x["Name"] == "RelievePainSideEffectItem")["Value"];
                        if(DataList.Exists(x => x["Name"] == "RelievePainSideEffectItem_Other"))
                            Word_E = Word_E.Substring(0, Word_E.Length - 2) + DataList.Find(x => x["Name"] == "RelievePainSideEffectItem_Other")["Value"];
                        Word_E += "等治療副作用。";
                    }
                    else
                        Word_E += "。";

                    if (AssessType.Equals("CPOT評估(加護單位)"))
                    {
                        string Word_O = "病人無法表達疼痛，依疼痛CPOT評估方法評估病人疼痛" + ps_value.ToString() + "分。";
                        if (Upd_CareRecord(Convert.ToDateTime(RecordTime).ToString("yyyy/MM/dd HH:mm:59")
                            , PainRecordCode, "持續疼痛-CPOT評估", "", "", Word_O, Word_I, Word_E, PainCode) == 0)
                        {
                            Insert_CareRecord(Convert.ToDateTime(RecordTime).ToString("yyyy/MM/dd HH:mm:59")
                                , PainRecordCode, "持續疼痛-CPOT評估", "", "", Word_O, Word_I, Word_E, PainCode);
                        }
                    }
                    else
                    {
                        string Word_O = "病人無法表達疼痛，依疼痛困難評估方法評估病人疼痛" + ps_value.ToString() + "分。";
                        if (Upd_CareRecord(Convert.ToDateTime(RecordTime).ToString("yyyy/MM/dd HH:mm:59")
                            , PainRecordCode, "持續疼痛-困難評估", "", "", Word_O, Word_I, Word_E, PainCode) == 0)
                        {
                            Insert_CareRecord(Convert.ToDateTime(RecordTime).ToString("yyyy/MM/dd HH:mm:59")
                                , PainRecordCode, "持續疼痛-困難評估", "", "", Word_O, Word_I, Word_E, PainCode);
                        }
                    }
                }
                return "Y";
            }
            else
                return "N";
        }

        #endregion

        /// <summary>
        /// 取得系統部位維護黨
        /// </summary>
        /// <returns></returns>
        public List<SysParamItem> SelectSysParams(string PModel, string PGroup)
        {
            List<SysParamItem> PositionList = new List<SysParamItem>();
            try
            {
                
                SysParamItem Temp = null;
                string sql = "SELECT P_ID, P_NAME, P_VALUE, P_MEMO FROM SYS_PARAMS "
                    + "WHERE P_MODEL = '" + PModel + "' AND P_GROUP = '" + PGroup + "' "
                    + "AND P_LANG = 'zh-TW' ORDER BY P_SORT ";

                DataTable Dt = link.DBExecSQL(sql);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        Temp = new SysParamItem();
                        Temp.p_id = Dt.Rows[i]["P_ID"].ToString();
                        Temp.p_name = Dt.Rows[i]["P_NAME"].ToString();
                        Temp.p_value = Dt.Rows[i]["P_VALUE"].ToString();
                        Temp.p_memo = Dt.Rows[i]["P_MEMO"].ToString();
                        PositionList.Add(Temp);
                    }
                }
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }
            return PositionList;
        }

        /// <summary>
        /// 取得VitalSing > GCS
        /// </summary>
        /// <returns>Json字串</returns>
        public string SelectGCS()
        {
            try
            {
                //確認是否有病人資料(有選取病人)
                if (Session["PatInfo"] != null)
                {
                    //宣告必須要使用到的變數
                    List<Dictionary<string, string>> vsList = new List<Dictionary<string, string>>();
                    Dictionary<string, string> vsdl = null;
                    List<Dictionary<string, string>> vsId = new List<Dictionary<string, string>>();
                    string[] TempWord = null;

                    //取得vs_id
                    string sql = "SELECT CREATE_DATE, VS_ID "
                    + "FROM DATA_VITALSIGN "
                    + "WHERE FEE_NO = '" + base.ptinfo.FeeNo + "' AND VS_ITEM = 'gc' "
                    + "GROUP BY CREATE_DATE, VS_ID ORDER BY CREATE_DATE ";

                    DataTable Dt = link.DBExecSQL(sql);
                    if (Dt.Rows.Count > 0)
                    {
                        Dictionary<string, string> vsId_Item = null;
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            vsId_Item = new Dictionary<string, string>();
                            vsId_Item["VS_ID"] = Dt.Rows[i]["VS_ID"].ToString().Trim();
                            vsId_Item["CREATE_DATE"] = Convert.ToDateTime(Dt.Rows[i]["CREATE_DATE"].ToString().Trim()).ToString("yyyy/MM/dd HH:mm:ss");
                            vsId.Add(vsId_Item);
                        }
                    }
                    // 開始處理資料
                    foreach (var item in vsId)
                    {
                        vsdl = new Dictionary<string, string>();
                        vsdl["Date"] = Convert.ToDateTime(item["CREATE_DATE"]).ToString("yyyy/MM/dd HH:mm");

                        sql = "SELECT VS_RECORD FROM DATA_VITALSIGN "
                        + "WHERE FEE_NO = '" + base.ptinfo.FeeNo + "' AND VS_ID = '" + item["VS_ID"] + "' AND VS_ITEM = 'gc' "
                        + "AND CREATE_DATE = TO_DATE('" + item["CREATE_DATE"] + "', 'yyyy/MM/dd hh24:mi:ss')";

                        Dt = link.DBExecSQL(sql);
                        if (Dt.Rows.Count > 0)
                        {
                            for (int i = 0; i < Dt.Rows.Count; i++)
                            {
                                TempWord = Dt.Rows[i]["VS_RECORD"].ToString().Split('|');
                                vsdl["Value"] = "E：" + TempWord[0] + "，V：" + TempWord[1] + "，M：" + TempWord[2];
                                vsdl["E"] = TempWord[0].Substring(1, 1);
                                vsdl["V"] = TempWord[1].Substring(1, 1);
                                vsdl["M"] = TempWord[2].Substring(1, 1);
                            }
                        }
                        vsList.Add(vsdl);
                    }

                    return JsonConvert.SerializeObject(vsList);
                }
                else
                    return "";
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return "";
            }
        }
    }
}
