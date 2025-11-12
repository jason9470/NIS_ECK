using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.Text;
using System.Data;
using System.Collections;
using System.Diagnostics;
using NIS.Data;
using NIS.Models;
using NIS.UtilTool;
using NIS.WebService;
using Newtonsoft.Json;
using System.Configuration;
using Com.Mayaminer;
using static NIS.Models.EmergencyTreatment;
using static NIS.Controllers.CareRecordController;
using Oracle.ManagedDataAccess.Client;
using static NIS.Controllers.ConstraintsAssessmentController;
using System.Windows.Controls;

namespace NIS.Controllers
{
    public class EmergencyTreatmentController : BaseController
    {
        LogTool log = new LogTool();
        private DBConnector link;
        private EmergencyTreatment EmergencyTreatmentModel;
        private string mode = MvcApplication.iniObj.NisSetting.ServerMode.ToString();

        public enum RESPONSE_STATUS
        {
            SUCCESS = 0,
            ERROR = 1,
            EXCEPTION = 2,
            LOGOUT = 3
        }

        /// <summary> 回傳值 </summary>
        public class RESPONSE_MSG
        {
            /// <summary> 處理狀態 </summary>
            public RESPONSE_STATUS status { set; get; }

            /// <summary> 傳回訊息或內容 </summary>
            public string message { set; get; }

            /// <summary> 附帶物件 </summary>
            public object attachment { set; get; }

            /// <summary> 取得序列化結果 </summary>
            public string get_json()
            {
                return JsonConvert.SerializeObject(this);
            }
        }

        public EmergencyTreatmentController()
        {
            this.EmergencyTreatmentModel = new EmergencyTreatment();
            this.link = new DBConnector();
        }

        #region 首頁
        // 首頁
        public ActionResult Index()
        {
            if (Session["PatInfo"] == null)
            {
                Response.Write("<script>alert('請重新選擇病患');</script>");
                return new EmptyResult();
            }

            return View();
        }

        // 急救紀錄資料列表
        public ActionResult EmergencyTreatmentListData()
        {
            ViewBag.EmergencyTreatmentList = EmergencyTreatmentModel.QueryMaster(ptinfo.FeeNo);
            DataTable dt = EmergencyTreatmentModel.QueryTemplate(ptinfo.FeeNo);
            dt.Columns.AddRange(new DataColumn[]
            {
                new DataColumn("DoctorArrivalDate", typeof(string)),
                new DataColumn("DoctorArrivalTime", typeof(string)),
                new DataColumn("CPCRDate", typeof(string)),
                new DataColumn("CPCRTime", typeof(string)),
                new DataColumn("DefibrillationDate", typeof(string)),
                new DataColumn("DefibrillationTime", typeof(string)),
            });
            foreach (DataRow row in dt.Rows)
            {
                DataTable detail = EmergencyTreatmentModel.QueryDetail("", row["ID"].ToString());
                foreach (DataRow item in detail.Rows)
                {
                    if(item["ITEM_ID"].ToString() == "DoctorArrivalDate")
                    {
                        row["DoctorArrivalDate"] = item["ITEM_VALUE"].ToString();
                    }
                    else if (item["ITEM_ID"].ToString() == "DoctorArrivalTime")
                    {
                        row["DoctorArrivalTime"] = item["ITEM_VALUE"].ToString();
                    }
                    else if (item["ITEM_ID"].ToString() == "CPCRDate")
                    {
                        row["CPCRDate"] = item["ITEM_VALUE"].ToString();
                    }
                    else if (item["ITEM_ID"].ToString() == "CPCRTime")
                    {
                        row["CPCRTime"] = item["ITEM_VALUE"].ToString();
                    }
                    else if (item["ITEM_ID"].ToString() == "DefibrillationDate")
                    {
                        row["DefibrillationDate"] = item["ITEM_VALUE"].ToString();
                    }
                    else if (item["ITEM_ID"].ToString() == "DefibrillationTime")
                    {
                        row["DefibrillationTime"] = item["ITEM_VALUE"].ToString();
                    }
                }
            }
            ViewBag.TemplateList = dt;
            return View();
        }
        #endregion

        #region 藥物使用清單
        // 藥物使用清單
        public ActionResult NoteList(string MasterID)
        {
            ViewBag.MasterID = MasterID;
            return View();
        }

        // 藥物使用清單資料
        public ActionResult NoteListData(string MasterID)
        {
            DataTable TemplateNote = EmergencyTreatmentModel.QueryTemplate("", MasterID, "", "Note");
            ViewBag.TemplateNote = TemplateNote;
            ViewBag.DetailNote = EmergencyTreatmentModel.QueryDetail(MasterID, "");

            return View();
        }
        #endregion

        #region 新增介面
        // 新增介面
        public ActionResult Insert(string MasterID)
        {
            ViewBag.MasterID = MasterID;
            var TemplateBeforeID = "";
            var TemplateAfterID = "";
            DataTable TemplateBefore = EmergencyTreatmentModel.QueryTemplate("", MasterID, "", "Before");
            if (TemplateBefore.Rows.Count > 0)
            {
                TemplateBeforeID = TemplateBefore.Rows[0]["ID"].ToString();
            }
            DataTable TemplateAfter = EmergencyTreatmentModel.QueryTemplate("", MasterID, "", "After");
            if (TemplateAfter.Rows.Count > 0)
            {
                TemplateAfterID = TemplateAfter.Rows[0]["ID"].ToString();
            }

            ViewBag.TemplateBeforeID = TemplateBeforeID;
            ViewBag.TemplateAfterID = TemplateAfterID;

            ViewBag.TemplateBefore = TemplateBefore;
            ViewBag.TemplateAfter = TemplateAfter;

            ViewBag.DetailBefore = EmergencyTreatmentModel.QueryDetail("", TemplateBeforeID);
            ViewBag.DetailAfter = EmergencyTreatmentModel.QueryDetail("", TemplateAfterID);

            ViewBag.DetailNote = EmergencyTreatmentModel.QueryDetail(MasterID, "");

            return View();
        }

        // 急救前編輯介面
        public ActionResult Edit_Information_Before(string MasterID, string TemplateID)
        {
            ViewBag.MasterID = MasterID;
            ViewBag.TemplateID = TemplateID;
            ViewBag.TemplateBefore = EmergencyTreatmentModel.QueryTemplate("", MasterID, TemplateID, "Before"); ;
            ViewBag.DetailBefore = EmergencyTreatmentModel.QueryDetail("", TemplateID);
            return View();
        }

        // 急救後編輯介面
        public ActionResult Edit_Information_After(string MasterID, string TemplateID)
        {
            ViewBag.MasterID = MasterID;
            ViewBag.TemplateID = TemplateID;
            ViewBag.TemplateAfter = EmergencyTreatmentModel.QueryTemplate("", MasterID, TemplateID, "After"); ;
            ViewBag.DetailAfter = EmergencyTreatmentModel.QueryDetail("", TemplateID);
            return View();
        }

        // 急救Note編輯介面
        public ActionResult Edit_Information_Note(string MasterID, string TemplateID)
        {
            ViewBag.MasterID = MasterID;
            ViewBag.TemplateID = TemplateID;
            ViewBag.TemplateNote = EmergencyTreatmentModel.QueryTemplate("", MasterID, TemplateID, "Note"); ;
            ViewBag.DetailNote = EmergencyTreatmentModel.QueryDetail("", TemplateID);
            return View();
        }

        // 帶入VitalSign介面
        public ActionResult VitalSign(string MasterID, string TemplateID)
        {
            ViewBag.MasterID = MasterID;
            return View();
        }

        //VitalSign拋轉介接
        public ActionResult VitalSign_Interfacing(string starttime, string endtime, string feeno, string MasterID, string TemplateID)
        {

            string start = DateTime.Now.ToString("yyyy/MM/dd 00:00");
            string end = DateTime.Now.AddMinutes(2).ToString("yyyy/MM/dd HH:mm");
            if (starttime != null && endtime != null)
            {
                start = starttime;
                end = endtime;
            }

            List<string[]> vsId = new List<string[]>();
            //取得vs_id
            /*string*/
            var sqlstr = " select CREATE_DATE,vs_id from data_vitalsign where fee_no = '" + ptinfo.FeeNo + "' ";
            sqlstr += "and create_date between to_date('" + start + "','yyyy/MM/dd hh24:mi:ss') and to_date('" + end + "','yyyy/MM/dd hh24:mi:ss') AND DEL is null ";
            sqlstr += "group by CREATE_DATE,vs_id order by CREATE_DATE";

            DataTable Dt = link.DBExecSQL(sqlstr);
            if (Dt.Rows.Count > 0)
            {
                for (int i = 0; i < Dt.Rows.Count; i++)
                {
                    vsId.Add(new string[] { Dt.Rows[i]["vs_id"].ToString().Trim(), Dt.Rows[i]["CREATE_DATE"].ToString() });
                }
            }

            List<EmergencyTreatment_VitalSignImport> vsList = new List<EmergencyTreatment_VitalSignImport>();

            // 開始處理資料
            for (int i = 0; i <= vsId.Count - 1; i++)
            {
                //初始化資料
                EmergencyTreatment_VitalSignImport vsdl = new EmergencyTreatment_VitalSignImport();

                sqlstr = " select vsd.*, to_char(CREATE_DATE,'yyyy/MM/dd hh24:mi:ss') as m_date  from data_vitalsign vsd ";
                sqlstr += " where fee_no ='" + ptinfo.FeeNo + "' and vs_id = '" + vsId[i][0] + "' ";
                sqlstr += " and create_date = to_date('" + Convert.ToDateTime(vsId[i][1]).ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/MM/dd hh24:mi:ss')";
                //sqlstr += " and vsd.vs_record is not null ";
                vsdl.vs_id = vsId[i][0];
                Dt = this.link.DBExecSQL(sqlstr);
                if (Dt.Rows.Count > 0)
                {
                    for (int j = 0; j < Dt.Rows.Count; j++)
                    {
                        var vs_item = Dt.Rows[j]["vs_item"].ToString();
                        var vs_record = Dt.Rows[j]["vs_record"].ToString();
                        var vs_reason = Dt.Rows[j]["vs_reason"].ToString();
                        switch (vs_item)
                        {
                            case "bt":
                                if (vs_reason == "測不到")
                                {
                                    vsdl.vs_bt = "測不到";
                                }
                                else
                                {
                                    vsdl.vs_bt = vs_record;
                                }
                                break;
                            case "mp":
                                if (vs_reason == "測不到")
                                {
                                    vsdl.vs_mp = "測不到";
                                }
                                else
                                {
                                    vsdl.vs_mp = vs_record;
                                }
                                break;
                            case "bf":
                                if (vs_reason == "測不到")
                                {
                                    vsdl.vs_bf = "測不到";
                                }
                                else
                                {
                                    vsdl.vs_bf = vs_record;
                                }
                                break;
                            case "bp":
                                var splitBP = vs_record.Split('|');
                                if (vs_reason == "測不到")
                                {
                                    vsdl.vs_bp_sys = "測不到";
                                    vsdl.vs_bp_dia = "測不到";
                                }
                                else
                                {
                                    if (splitBP.Length > 0)
                                    {
                                        for (var z = 0; z < splitBP.Length; z++)
                                        {
                                            var value = splitBP[z];
                                            switch (z)
                                            {
                                                case 0:
                                                    vsdl.vs_bp_sys = value;
                                                    break;
                                                case 1:
                                                    vsdl.vs_bp_dia = value;
                                                    break;
                                    }
                                }                                
                                    }
                                }
                                break;
                            case "sp":
                                if (vs_reason == "測不到")
                                {
                                    vsdl.vs_sp = "測不到";
                                }
                                else
                                {
                                    vsdl.vs_sp = vs_record;
                                }
                                break;
                        }
                    }
                    vsdl.create_date = Dt.Rows[0]["CREATE_DATE"].ToString();
                    vsdl.modify_date = Dt.Rows[0]["MODIFY_DATE"].ToString();
                }
                vsList.Add(vsdl);
            }
            ViewData["result"] = vsList;

            // 取得已帶入過的生命徵象vs_id,record_time
            var templateList = EmergencyTreatmentModel.QueryTemplate("", MasterID, "", "").AsEnumerable().OrderBy(x => x["RECORD_TIME"]).ToList();
            var vs_idList = new List<Dictionary<string, string>>();
            if (templateList.Count > 0)
            {
                for (var i = 0; i < templateList.Count; i++)
                {
                    var matchBloodID = vs_idList.FirstOrDefault(x => x["vs_id"].ToString() == templateList[i]["vs_id"].ToString());
                    if (matchBloodID != null)
                    {
                        if (DateTime.Parse(templateList[i]["RECORD_TIME"].ToString()) < DateTime.Parse(matchBloodID["record_time"].ToString()))
                        {
                            continue;
                        }
                    }
                    Dictionary<string, string> TempData = new Dictionary<string, string>();
                    TempData.Add("vs_id", templateList[i]["vs_id"].ToString());
                    TempData.Add("record_time", templateList[i]["RECORD_TIME"].ToString());
                    vs_idList.Add(TempData);
                }
            }
            ViewBag.VSID_List = vs_idList;

            return View();
        }

        //EKG清單
        public ActionResult EKGList()
        {
            string chartNO = ptinfo.ChartNo.ToString().Trim();
            DataTable dt = EmergencyTreatmentModel.get_EKG(chartNO);
            ViewBag.dt = dt;

            return View();
        }

        // 下載EKG圖片轉為Base64
        public ActionResult DownloadEKGPicture(string url)
        {
            RESPONSE_MSG result = new RESPONSE_MSG();
            if (mode == "Maya")
            {
                result.status = RESPONSE_STATUS.SUCCESS;
                result.message = "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";
                return Content(JsonConvert.SerializeObject(result), "application/json");
            }

            if (string.IsNullOrEmpty(url))
            {
                result.status = RESPONSE_STATUS.ERROR;
                result.message = "url為空!";
                return Content(JsonConvert.SerializeObject(result), "application/json");
            }

            string EKGBase64 = "";
            byte[] arr = null;
            using (var webClient = new MyWebClient())
            {
                arr = webClient.DownloadData(url);
            }
            EKGBase64 = Convert.ToBase64String(arr);
            if (string.IsNullOrEmpty(EKGBase64))
            {
                result.status = RESPONSE_STATUS.ERROR;
                result.message = "EKG圖片下載失敗!";
                return Content(JsonConvert.SerializeObject(result), "application/json");
            }

            result.status = RESPONSE_STATUS.SUCCESS;
            result.message = EKGBase64;
            return Content(JsonConvert.SerializeObject(result), "application/json");
        }

        // 取得使用者姓名
        public ActionResult GetUserName(string UserID)
        {
            RESPONSE_MSG result = new RESPONSE_MSG();
            if (string.IsNullOrEmpty(UserID))
            {
                result.status = RESPONSE_STATUS.ERROR;
                result.message = "員工編號為空!";
                return Content(JsonConvert.SerializeObject(result), "application/json");
            }

            byte[] listByteCode = webService.UserName(UserID);
            if (listByteCode == null)
            {
                result.status = RESPONSE_STATUS.ERROR;
                result.message = "查無此員工!";
                return Content(JsonConvert.SerializeObject(result), "application/json");
            }
            string listJsonArray = CompressTool.DecompressString(listByteCode);
            UserInfo user = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
            result.status = RESPONSE_STATUS.SUCCESS;
            result.message = user.EmployeesName;
            return Content(JsonConvert.SerializeObject(result), "application/json");
        }

        #endregion

        #region 新增、修改、刪除
        // 新增、修改
        public ActionResult Save(FormCollection form)
        {
            RESPONSE_MSG result = new RESPONSE_MSG();
            Boolean billStatus = false;

            if (Session["PatInfo"] == null)
            {
                result.status = RESPONSE_STATUS.ERROR;
                result.message = "載入病人資訊失敗!";
                return Content(JsonConvert.SerializeObject(result), "application/json");
            }

            var MasterID = form["MasterID"].ToString();
            var TemplateID = form["TemplateID"].ToString();
            var Type = form["Type"].ToString();

            List<DBItem> insertDataList = new List<DBItem>();
            var DateTimeNow = DateTime.Now;
            var RecordTime = DateTimeNow.ToString("yyyy/MM/dd HH:mm");

            switch (Type)
            {
                case "Before":
                    RecordTime = form["IncidentDate"].ToString() + " " + form["IncidentTime"].ToString();
                    break;
                case "After":
                    RecordTime = form["EndEmergencyTreatmentDate"].ToString() + " " + form["EndEmergencyTreatmentTime"].ToString();
                    break;
                case "Note":
                    RecordTime = form["RecordDate"].ToString() + " " + form["RecordTime"].ToString();
                    break;
            }


            ////電擊器計價 新增默許
            //if (form["InitialTreatment"] != null || form["InitialTreatment"].ToString().Contains("電擊"))
            //{
            //    List<Bill_RECORD> billDataList = new List<Bill_RECORD>();

            //    Bill_RECORD billData = new Bill_RECORD();

            //    billData.HO_ID = "8447028";
            //    billData.COUNT = "1";

            //    billDataList.Add(billData);

            //    SaveBillingRecord(billDataList);
            //}

            //急救時間計價
            //檢查是否有起訖時間
            string masterSql = "SELECT * FROM EMERGENCYTREATMENT_MASTER WHERE ID = '" + MasterID + "' AND status ='Y' ";
            DataTable dtMaster = new DataTable();

            link.DBExecSQL(masterSql, ref dtMaster);
            if (dtMaster !=null && dtMaster.Rows.Count > 0)
            {
                var rowMaster = dtMaster.Rows[0];
                if (!string.IsNullOrWhiteSpace(rowMaster["START_TIME"].ToString()))
                {
                    // 尚未計價時才需要計價
                    if (rowMaster["BILL_STATUS"].ToString() != "Y")
                    {
                        // 找出是否有CPR或電擊
                        string billCheckSql = "SELECT * FROM EMERGENCYTREATMENT_DETAIL WHERE MASTER_ID = '" + MasterID + "' AND  ITEM_ID = 'InitialTreatment' AND STATUS = 'Y'  ";
                        DataTable dtBillCheck = new DataTable();

                        link.DBExecSQL(billCheckSql, ref dtBillCheck);
                        if (dtBillCheck != null && dtBillCheck.Rows.Count > 0)
                        {
                            double TimeDifference = 0;

                            TimeDifference = (DateTime.Parse(RecordTime) - (DateTime)rowMaster["START_TIME"]).TotalMinutes;

                            int count = 0;

                            double trans = 0;

                            trans = TimeDifference / 10;

                            // 10分鐘計算1次，最多6次
                            if (trans < 1)
                            {
                                trans = 1;
                            }
                            else
                            {
                                trans = Math.Ceiling(trans);
                            }

                            count = (int)trans;
                            if (trans > 5)
                            {
                                count = 6;
                            }

                            // 新增默許
                            List<Bill_RECORD> billDataList = new List<Bill_RECORD>();
                            Bill_RECORD billData = new Bill_RECORD();

                            if (dtBillCheck.Rows[0]["ITEM_VALUE"].ToString().Contains("心外按摩"))
                            {
                                billData.HO_ID = "8447029";
                                billData.COUNT = count.ToString();

                                billDataList.Add(billData);
                            }

                            //if (dtBillCheck.Rows[0]["ITEM_VALUE"].ToString().Contains("電擊"))
                            //{
                            //    billData = new Bill_RECORD();

                            //    billData.HO_ID = "8447028";
                            //    billData.COUNT = "1";

                            //    billDataList.Add(billData);
                            //}


                            SaveBillingRecord(billDataList, (DateTime)rowMaster["START_TIME"]);

                            billStatus = true;

                        }
                    }
                }
            }






            //------------------------------------------------------------------------------------------------------------------

            //string startRecordTime = ""; string endRecordTime = "";
            //if (form["IncidentDate"] != null && form["IncidentTime"] != null)
            //{
            //    startRecordTime = form["IncidentDate"].ToString() + " " + form["IncidentTime"].ToString();
            //}
            //if (form["EndEmergencyTreatmentDate"] != null && form["EndEmergencyTreatmentTime"] != null)
            //{
            //    endRecordTime = form["EndEmergencyTreatmentDate"].ToString() + " " + form["EndEmergencyTreatmentTime"].ToString();
            //}

            //if ((startRecordTime != "" || endRecordTime != "") && MasterID != "")
            //{
            //    string sql = "SELECT * FROM EMERGENCYTREATMENT_DETAIL WHERE MASTER_ID = '" + MasterID + "'";
            //    if (startRecordTime != "")
            //    {
            //        sql += " AND (ITEM_ID = 'EndEmergencyTreatmentDate' OR ITEM_ID = 'EndEmergencyTreatmentTime')";
            //    }
            //    else
            //    {
            //        sql += " AND (ITEM_ID = 'IncidentDate' OR ITEM_ID = 'IncidentTime')";

            //    }
            //    sql += " AND STATUS = 'Y'";

            //    DataTable dt = new DataTable();
            //    link.DBExecSQL(sql, ref dt);
            //    string dataTime = "";
            //    string tempDate = "";
            //    string tempTime = "";
            //    if (dt.Rows.Count > 0)
            //    {
            //        foreach (DataRow dr in dt.Rows)
            //        {
            //            if (dr["ITEM_ID"].ToString() == "IncidentDate" || dr["ITEM_ID"].ToString() == "EndEmergencyTreatmentDate")
            //            {
            //                tempDate = dr["ITEM_VALUE"].ToString();
            //            }
            //            else
            //            {
            //                tempTime = dr["ITEM_VALUE"].ToString();
            //            }
            //        }

            //    }
            //    //檢查是否有記過價

            //    string checkSql = "SELECT * FROM EMERGENCYTREATMENT_MASTER WHERE ID = '"+ MasterID + "' AND  BILL_STATUS = 'Y'";
            //    DataTable dtCk = new DataTable();

            //    link.DBExecSQL(checkSql, ref dtCk);
            //    if(dtCk.Rows.Count > 0)
            //    {

            //    }
            //    else
            //    {
            //        // 找出是否有CPR或電擊
            //        string billCheckSql = "SELECT * FROM EMERGENCYTREATMENT_DETAIL WHERE MASTER_ID = '" + MasterID + "' AND  ITEM_ID = 'InitialTreatment' AND STATUS = 'Y'  ";
            //        DataTable dtBillCheck = new DataTable();

            //        link.DBExecSQL(billCheckSql, ref dtBillCheck);
            //        if (dtBillCheck != null && dtBillCheck.Rows.Count > 0)
            //        {
            //            double TimeDifference = 0;

            //            if (tempDate != "" && tempTime != "")
            //            {
            //                dataTime = tempDate + " " + tempTime;
            //            }

            //            DateTime recordTime = new DateTime();
            //            if (dataTime != "")
            //            {
            //                recordTime = DateTime.Parse(dataTime);

            //                DateTime saveTime = DateTime.Parse(RecordTime);

            //                if (startRecordTime != "")
            //                {
            //                    TimeDifference = (recordTime - saveTime).TotalMinutes;
            //                }
            //                else
            //                {
            //                    TimeDifference = (saveTime - recordTime).TotalMinutes;
            //                }

            //                int count = 0;

            //                double trans = 0;

            //                trans = TimeDifference / 10;

            //                // 10分鐘計算1次，最多6次
            //                if (trans < 1)
            //                {
            //                    trans = 1;
            //                }
            //                else
            //                {
            //                    trans = Math.Ceiling(trans);
            //                }

            //                count = (int)trans;
            //                if (trans > 5)
            //                {
            //                    count = 6;
            //                }

            //                // 新增默許
            //                List<Bill_RECORD> billDataList = new List<Bill_RECORD>();
            //                Bill_RECORD billData = new Bill_RECORD();

            //                if (dtBillCheck.Rows[0]["ITEM_VALUE"].ToString().Contains("心外按摩"))
            //                {
            //                    billData.HO_ID = "8447029";
            //                    billData.COUNT = count.ToString();

            //                    billDataList.Add(billData);
            //                }

            //                //if (dtBillCheck.Rows[0]["ITEM_VALUE"].ToString().Contains("電擊"))
            //                //{
            //                //    billData = new Bill_RECORD();

            //                //    billData.HO_ID = "8447028";
            //                //    billData.COUNT = "1";

            //                //    billDataList.Add(billData);
            //                //}


            //                SaveBillingRecord(billDataList, saveTime);

            //                billStatus = true;

            //            }

            //        }

            //        //------------------------------------------------------------------------------------------------

            //        //double TimeDifference = 0;

            //        //if (tempDate != "" && tempTime != "")
            //        //{
            //        //    dataTime = tempDate + " " + tempTime;
            //        //}

            //        //DateTime recordTime = new DateTime();
            //        //if (dataTime != "")
            //        //{
            //        //    recordTime = DateTime.Parse(dataTime);

            //        //    DateTime saveTime = DateTime.Parse(RecordTime);

            //        //    if (startRecordTime != "")
            //        //    {
            //        //        TimeDifference = (recordTime - saveTime).TotalMinutes;
            //        //    }
            //        //    else
            //        //    {
            //        //        TimeDifference = (saveTime - recordTime).TotalMinutes;
            //        //    }

            //        //    int count = 0;

            //        //    double trans = 0;

            //        //    trans = TimeDifference / 10;

            //        //    // 10分鐘計算1次，最多6次
            //        //    if (trans < 1)
            //        //    {
            //        //        trans = 1;
            //        //    }
            //        //    else
            //        //    {
            //        //        trans = Math.Ceiling(trans);
            //        //    }

            //        //    count = (int)trans;
            //        //    if (trans > 5)
            //        //    {
            //        //        count = 6;
            //        //    }

            //        //    // 新增默許
            //        //    List<Bill_RECORD> billDataList = new List<Bill_RECORD>();

            //        //    // CPR
            //        //    Bill_RECORD billData = new Bill_RECORD();

            //        //    billData.HO_ID = "8447029";
            //        //    billData.COUNT = count.ToString();

            //        //    billDataList.Add(billData);

            //        //    //電擊器計價 新增默許
            //        //    if (form["InitialTreatment"] != null || form["InitialTreatment"].ToString().Contains("電擊"))
            //        //    {
            //        //        billData = new Bill_RECORD();

            //        //        billData.HO_ID = "8447028";
            //        //        billData.COUNT = "1";

            //        //        billDataList.Add(billData);

            //        //        SaveBillingRecord(billDataList);
            //        //    }

            //        //    SaveBillingRecord(billDataList, saveTime);

            //        //    billStatus = true;

            //        //}
            //    }
            //}
            
            // 急救前和急救後才有
            // 儲存EKG圖片
            string EKG = "";
            byte[] DeCodeEKG = new byte[0];
            if (Type == "Before" || Type == "After")
            {
                EKG = form["EKG_Picture_Base64"].ToString();
                DeCodeEKG = Convert.FromBase64String(EKG);
            }

            int erow = 0;
            // 儲存用Item(不需儲存Detail)
            var propertiesList = new List<string>() { "MasterID", "TemplateID", "Type", "EKG_Picture_Base64" };
            try
            {
                //新增
                if (string.IsNullOrEmpty(TemplateID))
                {
                    //檢查是否已有主檔
                    TemplateID = base.creatid("EMERGENCYTREATMENT_TEMPLATE", userinfo.EmployeesNo, ptinfo.FeeNo, "0");
                    DataTable queryMaster = EmergencyTreatmentModel.QueryMaster("", MasterID);
                    //沒有主檔需新增
                    if (queryMaster.Rows.Count == 0)
                    {
                        insertDataList.Clear();
                        insertDataList.Add(new DBItem("SERIAL", "EMERGENCYTREATMENT_MASTER_" + DateTimeNow.ToString("yyyyMMddHHmmssfff"), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("ID", MasterID, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("FEENO", ptinfo.FeeNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CHARTNO", ptinfo.ChartNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CREATE_TIME", DateTimeNow.ToString("yyyy-MM-dd HH:mm"), DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("CREATE_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CREATE_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("RECORD_TIME", DateTimeNow.ToString("yyyy-MM-dd HH:mm"), DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("EDIT_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("EDIT_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                        //若為急救前資訊，需額外於主檔中START_TIME欄位寫入開始時間
                        if (Type == "Before")
                        {
                            insertDataList.Add(new DBItem("START_TIME", RecordTime, DBItem.DBDataType.DataTime));
                        }
                        //若為急救後資訊，需額外於主檔中END_TIME欄位寫入結束時間
                        if (Type == "After")
                        {
                            insertDataList.Add(new DBItem("END_TIME", RecordTime, DBItem.DBDataType.DataTime));
                        }
                        if(billStatus)
                        {
                            insertDataList.Add(new DBItem("BILL_STATUS", "Y", DBItem.DBDataType.String));
                        }
                        erow = link.DBExecInsert("EMERGENCYTREATMENT_MASTER", insertDataList);
                        if (erow == 0)
                        {
                            result.status = RESPONSE_STATUS.ERROR;
                            result.message = "新增主表失敗!";
                            return Content(JsonConvert.SerializeObject(result), "application/json");
                        }
                    }
                    else
                    {
                        //有主檔需更新修改資訊
                        insertDataList.Clear();
                        insertDataList.Add(new DBItem("MODIFY_TIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("MODIFY_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("MODIFY_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("EDIT_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("EDIT_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                        //若為急救前資訊，需額外於主檔中START_TIME欄位寫入開始時間
                        if (Type == "Before")
                        {
                            insertDataList.Add(new DBItem("START_TIME", RecordTime, DBItem.DBDataType.DataTime));
                        }
                        //若為急救後資訊，需額外於主檔中END_TIME欄位寫入結束時間
                        if (Type == "After")
                        {
                            insertDataList.Add(new DBItem("END_TIME", RecordTime, DBItem.DBDataType.DataTime));
                        }
                        if (billStatus)
                        {
                            insertDataList.Add(new DBItem("BILL_STATUS", "Y", DBItem.DBDataType.String));
                        }
                        string where = "ID='" + MasterID + "' AND STATUS ='Y'";
                        erow = link.DBExecUpdate("EMERGENCYTREATMENT_MASTER", insertDataList, where);
                    }

                    //新增TEMPLATE
                    insertDataList.Clear();
                    insertDataList.Add(new DBItem("SERIAL", "EMERGENCYTREATMENT_TEMPLATE_" + DateTimeNow.ToString("yyyyMMddHHmmssfff"), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("MASTER_ID", MasterID, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ID", TemplateID, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("FEENO", ptinfo.FeeNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CHARTNO", ptinfo.ChartNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATE_TIME", DateTimeNow.ToString("yyyy-MM-dd HH:mm"), DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("CREATE_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATE_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("RECORD_TIME", RecordTime, DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("EDIT_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("EDIT_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("TYPE", Type, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("VERSION", "1", DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("VS_ID", form["VitalSign_VS_ID"], DBItem.DBDataType.String));
                    erow = link.DBExecInsert("EMERGENCYTREATMENT_TEMPLATE", insertDataList);
                    if (erow == 0)
                    {
                        result.status = RESPONSE_STATUS.ERROR;
                        result.message = "新增模板失敗!";
                        return Content(JsonConvert.SerializeObject(result), "application/json");
                    }
                    else
                    {
                        //寫入EKG圖片
                        if (Type != "Note" && DeCodeEKG.Length > 0)
                        {
                            link.DBCmd.CommandText = "UPDATE EMERGENCYTREATMENT_TEMPLATE SET EKG = :EKG "
                                        + " WHERE FEENO = '" + base.ptinfo.FeeNo + "' AND ID = '" + TemplateID + "' AND STATUS='Y'";

                            link.DBCmd.Parameters.Add(":EKG", OracleDbType.Blob).Value = DeCodeEKG;

                            link.DBOpen();
                            link.DBCmd.ExecuteNonQuery();
                            link.DBClose();
                        }
                    }

                    //新增Detail
                    string[] ItemIDList = form.AllKeys;
                    for (var i = 0; i < ItemIDList.Length; i++)
                    {
                        try
                        {
                            var ItemID = ItemIDList[i].ToString();
                            var ItemValue = form[ItemID].ToString();
                            if (propertiesList.Contains(ItemID) || string.IsNullOrEmpty(ItemValue))
                            {
                                continue;
                            }

                            insertDataList.Clear();
                            insertDataList.Add(new DBItem("SERIAL", "EMERGENCYTREATMENT_DETAIL_" + DateTime.Now.ToString("yyyyMMddHHmmssfff"), DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("MASTER_ID", MasterID, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("TEMPLATE_ID", TemplateID, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("ITEM_ID", ItemID, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("ITEM_VALUE", ItemValue, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("CREATE_TIME", DateTimeNow.ToString("yyyy-MM-dd HH:mm"), DBItem.DBDataType.DataTime));
                            insertDataList.Add(new DBItem("VERSION", "1", DBItem.DBDataType.String));
                            erow = link.DBExecInsert("EMERGENCYTREATMENT_DETAIL", insertDataList);
                        }
                        catch (Exception ex)
                        {
                        }
                    }

                    #region 新增護理紀錄
                    string careRecordStr = form["CareRecordStr"];
                    switch (Type)
                    {
                        case "Before":
                            if (Upd_CareRecord(RecordTime, TemplateID, "急救前", careRecordStr, "", "", "", "", "EmergencyTreatment") == 0)
                            {
                                Insert_CareRecord_Black(RecordTime, TemplateID, "急救前", careRecordStr, "", "", "", "", "EmergencyTreatment");
                            }
                            break;
                        case "After":
                            if (Upd_CareRecord(RecordTime, TemplateID, "急救後", careRecordStr, "", "", "", "", "EmergencyTreatment") == 0)
                            {
                                Insert_CareRecord_Black(RecordTime, TemplateID, "急救後", careRecordStr, "", "", "", "", "EmergencyTreatment");
                            }
                            break;
                        case "Note":
                            if (Upd_CareRecord(RecordTime, TemplateID, "急救過程暨用藥", careRecordStr, "", "", "", "", "EmergencyTreatment") == 0)
                            {
                                Insert_CareRecord_Black(RecordTime, TemplateID, "急救過程暨用藥", careRecordStr, "", "", "", "", "EmergencyTreatment");
                            }
                            break;
                    }
                    #endregion
                }
                //修改
                else
                {
                    // 註記Master(無版次僅記錄最後修改資訊)
                    insertDataList.Clear();
                    insertDataList.Add(new DBItem("MODIFY_TIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("MODIFY_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("MODIFY_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("EDIT_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("EDIT_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    if (Type == "Before")
                    {
                        insertDataList.Add(new DBItem("START_TIME", RecordTime, DBItem.DBDataType.DataTime));
                    }
                    if (Type == "After")
                    {
                        insertDataList.Add(new DBItem("END_TIME", RecordTime, DBItem.DBDataType.DataTime));
                    }
                    if (billStatus)
                    {
                        insertDataList.Add(new DBItem("BILL_STATUS", "Y", DBItem.DBDataType.String));
                    }
                    string where = "ID='" + MasterID + "' AND STATUS ='Y'";
                    erow = link.DBExecUpdate("EMERGENCYTREATMENT_MASTER", insertDataList, where);

                    // 查詢現行Template
                    DataTable TemplateNow = EmergencyTreatmentModel.QueryTemplate("", MasterID, TemplateID, Type);
                    if (TemplateNow.Rows.Count != 1)
                    {
                        result.status = RESPONSE_STATUS.ERROR;
                        result.message = "無符合條件Template紀錄!";
                        return Content(JsonConvert.SerializeObject(result), "application/json");
                    }
                    var VersionNow = TemplateNow.Rows[0]["VERSION"].ToString().Trim();
                    var NewVersion = (int.Parse(VersionNow) + 1).ToString().Trim();

                    // 註記舊Template
                    insertDataList.Clear();
                    insertDataList.Add(new DBItem("MODIFY_TIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("MODIFY_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("MODIFY_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("EDIT_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("EDIT_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("STATUS", "M", DBItem.DBDataType.String));
                    where = "ID='" + TemplateID + "' AND STATUS ='Y'";
                    erow = link.DBExecUpdate("EMERGENCYTREATMENT_TEMPLATE", insertDataList, where);

                    // 新增新Template
                    insertDataList.Clear();
                    insertDataList.Add(new DBItem("SERIAL", "EMERGENCYTREATMENT_TEMPLATE_" + DateTimeNow.ToString("yyyyMMddHHmmssfff"), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("MASTER_ID", MasterID, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ID", TemplateID, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("FEENO", ptinfo.FeeNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CHARTNO", ptinfo.ChartNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATE_TIME", DateTimeNow.ToString("yyyy-MM-dd HH:mm"), DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("CREATE_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATE_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("RECORD_TIME", RecordTime, DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("EDIT_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("EDIT_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("TYPE", Type, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("VERSION", NewVersion, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("VS_ID", form["VitalSign_VS_ID"], DBItem.DBDataType.String));
                    erow = link.DBExecInsert("EMERGENCYTREATMENT_TEMPLATE", insertDataList);
                    if (erow == 0)
                    {
                        result.status = RESPONSE_STATUS.ERROR;
                        result.message = "新增模板失敗!";
                        return Content(JsonConvert.SerializeObject(result), "application/json");
                    }
                    else
                    {
                        if (Type != "Note" && DeCodeEKG.Length > 0)
                        {
                            link.DBCmd.CommandText = "UPDATE EMERGENCYTREATMENT_TEMPLATE SET EKG = :EKG "
                                        + " WHERE FEENO = '" + base.ptinfo.FeeNo + "' AND ID = '" + TemplateID + "' AND STATUS='Y'";

                            link.DBCmd.Parameters.Add(":EKG", OracleDbType.Blob).Value = DeCodeEKG;

                            link.DBOpen();
                            link.DBCmd.ExecuteNonQuery();
                            link.DBClose();
                        }
                    }

                    // 註記舊Detail
                    insertDataList.Clear();
                    insertDataList.Add(new DBItem("MODIFY_TIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("STATUS", "M", DBItem.DBDataType.String));
                    where = "MASTER_ID='" + MasterID + "' AND TEMPLATE_ID='" + TemplateID + "' AND STATUS ='Y'";
                    erow = link.DBExecUpdate("EMERGENCYTREATMENT_DETAIL", insertDataList, where);
                    if (erow == 0)
                    {
                        result.status = RESPONSE_STATUS.ERROR;
                        result.message = "註記舊Detail失敗!";
                        return Content(JsonConvert.SerializeObject(result), "application/json");
                    }

                    // 新增新Detail
                    string[] ItemIDList = form.AllKeys;
                    for (var i = 0; i < ItemIDList.Length; i++)
                    {
                        try
                        {
                            var ItemID = ItemIDList[i].ToString();
                            var ItemValue = form[ItemID].ToString();
                            if (propertiesList.Contains(ItemID) || string.IsNullOrEmpty(ItemValue))
                            {
                                continue;
                            }

                            insertDataList.Clear();
                            insertDataList.Add(new DBItem("SERIAL", "EMERGENCYTREATMENT_DETAIL_" + DateTime.Now.ToString("yyyyMMddHHmmssfff"), DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("MASTER_ID", MasterID, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("TEMPLATE_ID", TemplateID, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("ITEM_ID", ItemID, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("ITEM_VALUE", ItemValue, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("CREATE_TIME", DateTimeNow.ToString("yyyy-MM-dd HH:mm"), DBItem.DBDataType.DataTime));
                            insertDataList.Add(new DBItem("VERSION", NewVersion, DBItem.DBDataType.String));
                            erow = link.DBExecInsert("EMERGENCYTREATMENT_DETAIL", insertDataList);
                        }
                        catch (Exception ex)
                        {
                        }
                    }

                    #region 新增護理紀錄
                    string careRecordStr = form["CareRecordStr"];
                    string executionStr = form["ExecutionStr"];
                    switch (Type)
                    {
                        case "Before":
                            if (Upd_CareRecord(RecordTime, TemplateID, "急救前", careRecordStr, "", "", "", "", "EmergencyTreatment") == 0)
                            {
                                Insert_CareRecord_Black(RecordTime, TemplateID, "急救前", careRecordStr, "", "", "", "", "EmergencyTreatment");
                            }
                            break;
                        case "After":
                            if (Upd_CareRecord(RecordTime, TemplateID, "急救後", careRecordStr, "", "", "", "", "EmergencyTreatment") == 0)
                            {
                                Insert_CareRecord_Black(RecordTime, TemplateID, "急救後", careRecordStr, "", "", "", "", "EmergencyTreatment");
                            }
                            if (Upd_CareRecord(RecordTime, TemplateID + "1", "急救處置", executionStr, "", "", "", "", "EmergencyTreatment") == 0)
                            {
                                Insert_CareRecord_Black(RecordTime, TemplateID + "1", "急救處置", executionStr, "", "", "", "", "EmergencyTreatment");
                            }
                            break;
                        case "Note":
                            if (Upd_CareRecord(RecordTime, TemplateID, "急救過程暨用藥", careRecordStr, "", "", "", "", "EmergencyTreatment") == 0)
                            {
                                Insert_CareRecord_Black(RecordTime, TemplateID, "急救過程暨用藥", careRecordStr, "", "", "", "", "EmergencyTreatment");
                            }
                            break;
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                result.status = RESPONSE_STATUS.ERROR;
                result.message = "儲存失敗!";
                return Content(JsonConvert.SerializeObject(result), "application/json");
            }

            result.status = RESPONSE_STATUS.SUCCESS;
            result.message = "儲存成功!";
            return Content(JsonConvert.SerializeObject(result), "application/json");
        }

        // 急救紀錄單刪除
        public ActionResult DeleteEmergencyTreatment(string MasterID)
        {
            RESPONSE_MSG json_result = new RESPONSE_MSG();
            try
            {
                DateTime DateTimeNow = DateTime.Now;
                if (string.IsNullOrEmpty(MasterID))
                {
                    json_result.status = RESPONSE_STATUS.ERROR;
                    json_result.message = "刪除失敗!";
                    return Content(JsonConvert.SerializeObject(json_result), "application/json");
                }
                int erow = 0;

                List<DBItem> insertDataList_MASTER = new List<DBItem>();
                insertDataList_MASTER.Add(new DBItem("DELETE_TIME", DateTimeNow.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                insertDataList_MASTER.Add(new DBItem("DELETE_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                insertDataList_MASTER.Add(new DBItem("EDIT_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                insertDataList_MASTER.Add(new DBItem("EDIT_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList_MASTER.Add(new DBItem("STATUS", "D", DBItem.DBDataType.String));
                string where_MASTER = "ID='" + MasterID + "' AND STATUS ='Y'";
                erow = link.DBExecUpdate("EMERGENCYTREATMENT_MASTER", insertDataList_MASTER, where_MASTER);
                if (erow == 0)
                {
                    json_result.status = RESPONSE_STATUS.ERROR;
                    json_result.message = "MASTER刪除失敗!";
                    return Content(JsonConvert.SerializeObject(json_result), "application/json");
                }

                //取出要刪除的所有TEMPLATE_ID(刪除護理紀錄用)
                DataTable QueryTemplate = EmergencyTreatmentModel.QueryTemplate("", MasterID, "", "");

                List<DBItem> insertDataList_TEMPLATE = new List<DBItem>();
                insertDataList_TEMPLATE.Add(new DBItem("DELETE_TIME", DateTimeNow.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                insertDataList_TEMPLATE.Add(new DBItem("DELETE_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                insertDataList_TEMPLATE.Add(new DBItem("EDIT_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                insertDataList_TEMPLATE.Add(new DBItem("EDIT_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList_TEMPLATE.Add(new DBItem("STATUS", "D", DBItem.DBDataType.String));
                string where_TEMPLATE = "MASTER_ID='" + MasterID + "' AND STATUS ='Y'";
                erow = link.DBExecUpdate("EMERGENCYTREATMENT_TEMPLATE", insertDataList_TEMPLATE, where_TEMPLATE);
                if (erow == 0)
                {
                    json_result.status = RESPONSE_STATUS.ERROR;
                    json_result.message = "TEMPLATE刪除失敗!";
                    return Content(JsonConvert.SerializeObject(json_result), "application/json");
                }

                List<DBItem> insertDataList_DETAIL = new List<DBItem>();
                insertDataList_DETAIL.Add(new DBItem("DELETE_TIME", DateTimeNow.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                insertDataList_DETAIL.Add(new DBItem("STATUS", "D", DBItem.DBDataType.String));
                string where_DETAIL = "MASTER_ID='" + MasterID + "' AND STATUS ='Y'";
                erow = link.DBExecUpdate("EMERGENCYTREATMENT_DETAIL", insertDataList_DETAIL, where_DETAIL);
                if (erow == 0)
                {
                    json_result.status = RESPONSE_STATUS.ERROR;
                    json_result.message = "DETAIL刪除失敗!";
                    return Content(JsonConvert.SerializeObject(json_result), "application/json");
                }

                // 刪除護理紀錄
                if (QueryTemplate.Rows.Count > 0)
                {
                    for (var i = 0; i < QueryTemplate.Rows.Count; i++)
                    {
                        Del_CareRecord(QueryTemplate.Rows[i]["ID"].ToString(), "EmergencyTreatment");
                    }
                }
            }
            catch (Exception ex)
            {
                json_result.status = RESPONSE_STATUS.ERROR;
                json_result.message = "刪除失敗!";
                return Content(JsonConvert.SerializeObject(json_result), "application/json");
            }
            json_result.status = RESPONSE_STATUS.SUCCESS;
            json_result.message = "刪除成功!";
            return Content(JsonConvert.SerializeObject(json_result), "application/json");
        }

        // 急救前資訊，生命徵象暨急救藥物使用紀錄，急救後記錄刪除
        public ActionResult DeleteInformation(string TemplateID, string Type, string MasterID)
        {
            RESPONSE_MSG json_result = new RESPONSE_MSG();
            // 刪除護理紀錄成功Flag
            var delCareRecord = true;
            try
            {
                DateTime DateTimeNow = DateTime.Now;
                if (string.IsNullOrEmpty(TemplateID) || string.IsNullOrEmpty(Type))
                {
                    json_result.status = RESPONSE_STATUS.ERROR;
                    json_result.message = "刪除失敗!";
                    return Content(JsonConvert.SerializeObject(json_result), "application/json");
                }
                int erow = 0;
                List<DBItem> insertDataList = new List<DBItem>();

                // 註記Master(無版次僅記錄最後修改資訊)
                insertDataList.Clear();
                insertDataList.Add(new DBItem("MODIFY_TIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("MODIFY_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MODIFY_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EDIT_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EDIT_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                if (Type == "Before")
                {
                    insertDataList.Add(new DBItem("START_TIME", "", DBItem.DBDataType.DataTime));
                }
                if (Type == "After")
                {
                    insertDataList.Add(new DBItem("END_TIME", "", DBItem.DBDataType.DataTime));
                }
                string where_MASTER = "ID='" + MasterID + "' AND STATUS ='Y'";
                erow = link.DBExecUpdate("EMERGENCYTREATMENT_MASTER", insertDataList, where_MASTER);

                insertDataList = new List<DBItem>();
                insertDataList.Add(new DBItem("DELETE_TIME", DateTimeNow.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("DELETE_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EDIT_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EDIT_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("STATUS", "D", DBItem.DBDataType.String));
                string where = "ID='" + TemplateID + "' AND TYPE = '" + Type + "' AND STATUS ='Y'";
                erow = link.DBExecUpdate("EMERGENCYTREATMENT_TEMPLATE", insertDataList, where);
                if (erow == 0)
                {
                    json_result.status = RESPONSE_STATUS.ERROR;
                    json_result.message = "TEMPLATE刪除失敗!";
                    return Content(JsonConvert.SerializeObject(json_result), "application/json");
                }

                List<DBItem> insertDataList_DETAIL = new List<DBItem>();
                insertDataList_DETAIL.Add(new DBItem("DELETE_TIME", DateTimeNow.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                insertDataList_DETAIL.Add(new DBItem("STATUS", "D", DBItem.DBDataType.String));
                string where_DETAIL = "TEMPLATE_ID='" + TemplateID + "' AND STATUS ='Y'";
                erow = link.DBExecUpdate("EMERGENCYTREATMENT_DETAIL", insertDataList_DETAIL, where_DETAIL);
                if (erow == 0)
                {
                    json_result.status = RESPONSE_STATUS.ERROR;
                    json_result.message = "DETAIL刪除失敗!";
                    return Content(JsonConvert.SerializeObject(json_result), "application/json");
                }

                // 刪除護理紀錄
                erow = Del_CareRecord(TemplateID, "EmergencyTreatment");
                if (erow == 0)
                {
                    delCareRecord = false;
                }
            }
            catch (Exception ex)
            {
                json_result.status = RESPONSE_STATUS.ERROR;
                json_result.message = "刪除失敗!";
                return Content(JsonConvert.SerializeObject(json_result), "application/json");
            }

            json_result.status = RESPONSE_STATUS.SUCCESS;
            json_result.message = "刪除成功!";
            if (delCareRecord)
            {
                json_result.message += "\n護理紀錄刪除成功";
            }
            else
            {
                json_result.message += "\n護理紀錄刪除失敗";
            }
            return Content(JsonConvert.SerializeObject(json_result), "application/json");
        }

        // 同紀錄單生命徵象暨急救藥物使用紀錄全刪除
        public ActionResult DeleteAllNote(string MasterID)
        {
            RESPONSE_MSG json_result = new RESPONSE_MSG();
            try
            {
                DateTime DateTimeNow = DateTime.Now;
                if (string.IsNullOrEmpty(MasterID))
                {
                    json_result.status = RESPONSE_STATUS.ERROR;
                    json_result.message = "刪除失敗!";
                    return Content(JsonConvert.SerializeObject(json_result), "application/json");
    }
                int erow = 0;
                List<DBItem> insertDataList = new List<DBItem>();

                //查詢所有同紀錄單生命徵象暨急救藥物使用紀錄
                DataTable QueryTemplate = EmergencyTreatmentModel.QueryTemplate("", MasterID, "", "Note");
                if (QueryTemplate.Rows.Count == 0)
                {
                    json_result.status = RESPONSE_STATUS.ERROR;
                    json_result.message = "無紀錄需刪除!";
                    return Content(JsonConvert.SerializeObject(json_result), "application/json");
                }

                insertDataList = new List<DBItem>();
                insertDataList.Add(new DBItem("DELETE_TIME", DateTimeNow.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("DELETE_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EDIT_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EDIT_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("STATUS", "D", DBItem.DBDataType.String));
                string where = "MASTER_ID='" + MasterID + "' AND TYPE = 'Note' AND STATUS ='Y'";
                erow = link.DBExecUpdate("EMERGENCYTREATMENT_TEMPLATE", insertDataList, where);
                if (erow == 0)
                {
                    json_result.status = RESPONSE_STATUS.ERROR;
                    json_result.message = "TEMPLATE刪除失敗!";
                    return Content(JsonConvert.SerializeObject(json_result), "application/json");
                }

                List<DBItem> insertDataList_DETAIL = new List<DBItem>();
                insertDataList_DETAIL.Add(new DBItem("DELETE_TIME", DateTimeNow.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                insertDataList_DETAIL.Add(new DBItem("STATUS", "D", DBItem.DBDataType.String));
                string where_DETAIL = "MASTER_ID='" + MasterID + "' AND STATUS ='Y' ";
                where_DETAIL += "AND TEMPLATE_ID IN (''";
                for (var i = 0; i < QueryTemplate.Rows.Count; i++)
                {
                    where_DETAIL += ", '" + QueryTemplate.Rows[i]["ID"].ToString() + "'";
                }
                where_DETAIL += ")";
                erow = link.DBExecUpdate("EMERGENCYTREATMENT_DETAIL", insertDataList_DETAIL, where_DETAIL);
                if (erow == 0)
                {
                    json_result.status = RESPONSE_STATUS.ERROR;
                    json_result.message = "DETAIL刪除失敗!";
                    return Content(JsonConvert.SerializeObject(json_result), "application/json");
                }

                //刪除護理紀錄
                for (var i = 0; i < QueryTemplate.Rows.Count; i++)
                {
                    Del_CareRecord(QueryTemplate.Rows[i]["ID"].ToString(), "EmergencyTreatment");
                }
            }
            catch (Exception ex)
            {
                json_result.status = RESPONSE_STATUS.ERROR;
                json_result.message = "刪除失敗!";
                return Content(JsonConvert.SerializeObject(json_result), "application/json");
            }

            json_result.status = RESPONSE_STATUS.SUCCESS;
            json_result.message = "刪除成功!";
            return Content(JsonConvert.SerializeObject(json_result), "application/json");
        }

        #endregion
    }
}