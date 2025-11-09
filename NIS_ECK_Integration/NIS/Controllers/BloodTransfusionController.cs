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
using static NIS.Models.BloodTransfusion;
using System.Web.UI.DataVisualization.Charting;
using System.Web.UI.WebControls;

namespace NIS.Controllers
{
    public class BloodTransfusionController : BaseController
    {
        LogTool log = new LogTool();
        private DBConnector link;
        private BloodProducts bloodProducts = new BloodProducts();
        private string mode = MvcApplication.iniObj.NisSetting.ServerMode.ToString();
        private BloodTransfusion bloodTransfusionModel;

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

        public BloodTransfusionController()
        {
            this.link = new DBConnector();
            this.bloodTransfusionModel = new BloodTransfusion();
        }

        #region 清單列表
        /// <summary>
        /// 清單列表
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            if (Session["PatInfo"] == null)
            {
                Response.Write("<script>alert('請選擇病患!');</script>");
                return new EmptyResult();
            }

            return View();
        }

        /// <summary>
        /// 清單列表資料
        /// </summary>
        /// <param name="feeno">批價號</param>
        /// <param name="start">開始時間</param>
        /// <param name="end">結束時間</param>
        public ActionResult TransfusionListData(string feeno, string start = "", string end = "")
        {
            if (!string.IsNullOrEmpty(start) && !string.IsNullOrEmpty(end))
            {
                ViewBag.BloodTransfusionList = bloodTransfusionModel.QueryBloodTransfusionListData(ptinfo.FeeNo, start, end);
            }
            else
            {
                ViewBag.BloodTransfusionList = bloodTransfusionModel.QueryBloodTransfusionListData(ptinfo.FeeNo);
            }
            ViewBag.VerificationList = bloodTransfusionModel.QueryBloodVerificationDataByFeeNo(ptinfo.FeeNo);
            ViewBag.BloodVitalSignData = bloodTransfusionModel.QueryBloodVitalSignDataByFeeNo(ptinfo.FeeNo);
            ViewBag.TransfusionReactionList = bloodTransfusionModel.QueryTransfusionReactionDataByFeeNo(ptinfo.FeeNo);
            return View();
        }

        #region 輸血紀錄單(刪除)
        /// <summary>
        /// 輸血紀錄單(刪除)
        /// </summary>
        /// <param name="Serial">輸血紀錄單Serial</param>
        /// <returns></returns>
        public ActionResult TransfusionDelete(string Serial)
        {
            try
            {
                RESPONSE_MSG json_result = new RESPONSE_MSG();
                if (string.IsNullOrEmpty(Serial))
                {
                    json_result.status = RESPONSE_STATUS.ERROR;
                    json_result.message = "刪除失敗!";
                    return Content(JsonConvert.SerializeObject(json_result), "application/json");
                }
                int erow = 0;
                List<DBItem> insertDataList = new List<DBItem>();

                insertDataList = new List<DBItem>();
                insertDataList.Add(new DBItem("DELETE_TIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("DELETE_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EDIT_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EDIT_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("STATUS", "D", DBItem.DBDataType.String));

                // 刪除輸血紀錄單相關表單(核血作業、生命徵象、輸血反應)
                string where = "BLOODTARNSFUSION_SERIAL='" + Serial + "' AND STATUS ='Y'";
                erow = link.DBExecUpdate("BLOODVERIFICATION_DATA", insertDataList, where);
                erow = link.DBExecUpdate("BLOODVITALSIGN_DATA", insertDataList, where);
                erow = link.DBExecUpdate("BLOODTRANSREACTION_DATA", insertDataList, where);

                // 刪除輸血記錄單主檔
                where = "SERIAL='" + Serial + "' AND STATUS ='Y'";
                erow = link.DBExecUpdate("BLOODTRANSFUSION_DATA", insertDataList, where);

                if (erow == 0)
                {
                    json_result.status = RESPONSE_STATUS.ERROR;
                    json_result.message = "刪除失敗!";
                    return Content(JsonConvert.SerializeObject(json_result), "application/json");
                }
                else
                {
                    json_result.status = RESPONSE_STATUS.SUCCESS;
                    json_result.message = "刪除成功!";
                    return Content(JsonConvert.SerializeObject(json_result), "application/json");
                }
            }
            catch (Exception ex)
            {

            }
            return new EmptyResult();
        }
        #endregion
        #endregion

        #region 新增介面
        public ActionResult Insert(string BloodTransfusionSerial)
        {
            if (BloodTransfusionSerial != null)
            {
                ViewBag.BloodTransfusionSerial = BloodTransfusionSerial;
                ViewBag.BloodVerification = bloodTransfusionModel.QueryBloodVerificationDataBySerial(BloodTransfusionSerial);
                ViewBag.BloodVitalSignData = bloodTransfusionModel.QueryBloodVitalSignDataBySerial(BloodTransfusionSerial, "");
            }

            return View();
        }
        #endregion

        #region 核血作業
        /// <summary>
        /// 核血作業
        /// </summary>
        /// <param name="BloodTransfusionSerial">輸血紀錄單Serial</param>
        public ActionResult BloodVerification(string BloodTransfusionSerial)
        {
            if (BloodTransfusionSerial != null)
            {
                ViewBag.BloodTransfusionSerial = BloodTransfusionSerial;
                ViewBag.BloodVerification = bloodTransfusionModel.QueryBloodVerificationDataBySerial(BloodTransfusionSerial);
                ViewBag.BloodVitalSignData = bloodTransfusionModel.QueryBloodVitalSignDataBySerial(BloodTransfusionSerial, "Start");

                // 從檢驗資料中取得血型
                var ABOTyping = "";
                var RH_D = "";
                byte[] labByte = webService.GetLabbyDate(ptinfo.FeeNo, ptinfo.InDate.ToString("yyyy/MM/dd HH:mm"), DateTime.Now.ToString("yyyy/MM/dd HH:mm"));
                string labJson = "[]";
                if (labByte != null)
                {
                    labJson = CompressTool.DecompressString(labByte);
                    List<Lab> labList = JsonConvert.DeserializeObject<List<Lab>>(labJson);
                    if (labList != null)
                    {
                        var queryABOTyping = labList.OrderByDescending(x => x.LabDate).FirstOrDefault(x => x.LabCode == "6311001");
                        if (queryABOTyping != null)
                        {
                            ABOTyping = queryABOTyping.LabValue.Trim();
                        }
                        var queryRH_D = labList.OrderByDescending(x => x.LabDate).FirstOrDefault(x => x.LabCode == "6311003");
                        if (queryRH_D != null)
                        {
                            RH_D = queryRH_D.LabValue.Trim();
                        }
                    }
                }
                ViewBag.ABOTyping = ABOTyping;
                ViewBag.RH_D = RH_D;
            }

            return View();
        }

        #region 血品帶入資訊
        /// <summary>
        /// 血品帶入資訊
        /// </summary>
        /// <param name="starttime">開始時間</param>
        /// <param name="endtime">結束時間</param>
        /// <param name="chartNo">病歷號</param>
        /// <param name="feeno">批價號</param>
        /// <param name="BloodTransfusionSerial">輸血紀錄單Serial</param>
        public ActionResult Blood_Interfacing(string starttime, string endtime, string chartNo, string feeno, string BloodTransfusionSerial)
        {

            string start = DateTime.Now.AddDays(-1).ToString("yyyy/MM/dd 00:00");
            string end = DateTime.Now.ToString("yyyy/MM/dd 23:59");
            if (starttime != null && endtime != null)
            {
                start = starttime;
                end = endtime;
            }

            var returnData = new List<BloodTransfusion.BloodImport>();

            DataTable queryBloodProduts = bloodProducts.Query(chartNo, starttime, endtime);
            if (queryBloodProduts.Rows.Count > 0)
            {
                var tempData = new BloodTransfusion.BloodImport();
                for (var i = 0; i < queryBloodProduts.Rows.Count; i++)
                {
                    tempData = new BloodTransfusion.BloodImport();
                    tempData.Blood_ID = queryBloodProduts.Rows[i]["BLOOD_NO"].ToString().Trim();
                    var outDate = queryBloodProduts.Rows[i]["OUT_DATE"].ToString().Trim();
                    var outTime = queryBloodProduts.Rows[i]["OUT_TIME"].ToString().Trim();
                    var bloodTime = ConvertROCToGregorian(outDate).ToString("yyyy/MM/dd") + " " + outTime.Substring(0, 2) + ":" + outTime.Substring(2, 2);
                    tempData.Blood_Time = bloodTime;
                    tempData.Blood_Name = queryBloodProduts.Rows[i]["BLOOD_NAME"].ToString().Trim();
                    tempData.Blood_Number = queryBloodProduts.Rows[i]["BLOOD_NO"].ToString().Trim();
                    tempData.Blood_Type = queryBloodProduts.Rows[i]["BLOOD_TYPE"].ToString().Trim() + queryBloodProduts.Rows[i]["RH"].ToString().Trim();
                    tempData.Blood_Unit = "";
                    var effectiveDate = queryBloodProduts.Rows[i]["EFFECTIVE_DATE"].ToString().Trim();
                    var bloodExp = ConvertROCToGregorian(effectiveDate).ToString("yyyy/MM/dd");
                    tempData.Blood_Exp = bloodExp;
                    returnData.Add(tempData);
                }
            }

            // 取得已帶入過的血品
            var BloodVerificationList = bloodTransfusionModel.QueryBloodVerificationDataByFeeNo(feeno).AsEnumerable().OrderBy(x => x["RECORD_TIME"]).ToList();
            var blood_idList = new List<Dictionary<string, string>>();
            if (BloodVerificationList.Count > 0)
            {
                for (var i = 0; i < BloodVerificationList.Count; i++)
                {
                    Dictionary<string, string> TempData = new Dictionary<string, string>();
                    TempData.Add("blood_id", BloodVerificationList[i]["BLOOD_ID"].ToString());
                    TempData.Add("record_time", BloodVerificationList[i]["RECORD_TIME"].ToString());
                    blood_idList.Add(TempData);
                }
            }
            ViewBag.BLOODID_List = blood_idList;

            ViewData["result"] = returnData;

            return View();
        }
        #endregion

        #region 民國轉換西元
        /// <summary>
        /// 民國轉換西元
        /// </summary>
        /// <param name="rocDate">民國時間</param>
        /// <returns>西元時間</returns>
        static DateTime ConvertROCToGregorian(string rocDate)
        {
            if (rocDate.Length != 7)
                return new DateTime();

            int rocYear = int.Parse(rocDate.Substring(0, 3));
            int month = int.Parse(rocDate.Substring(3, 2));
            int day = int.Parse(rocDate.Substring(5, 2));
            int gregorianYear = rocYear + 1911;

            return new DateTime(gregorianYear, month, day);
        }
        #endregion

        #region (新增,編輯)核血作業
        //[HttpPost]
        //[ValidateInput(false)]
        //public ActionResult BloodVerificationSave(FormCollection form)
        //{
        //    try
        //    {
        //        RESPONSE_MSG json_result = new RESPONSE_MSG();
        //        var BloodTransfusionSerial = form["BloodTransfusionSerial"].ToString();
        //        var Serial = form["Serial"].ToString();
        //        int erow = 0;
        //        List<DBItem> insertDataList = new List<DBItem>();
        //        string start_time = form["Blood_StartDay"] + " " + form["Blood_StartTime"];
        //        var DateTimeNow = DateTime.Now;
        //        string NewTableID = "BLOODVERIFICATION_DATA_" + DateTime.Now.ToString("yyyyMMddHHmmssfff");

        //        //判斷新增或編輯
        //        if (string.IsNullOrEmpty(Serial)) //新增
        //        {
        //            //檢查輸血紀錄單Serial是否存在
        //            string sql = "SELECT SERIAL FROM BLOODTRANSFUSION_DATA \n";
        //            sql += "WHERE SERIAL = '" + BloodTransfusionSerial + "' \n";
        //            DataTable dt = link.DBExecSQL(sql);
        //            if (dt.Rows.Count == 0)
        //            {
        //                insertDataList = new List<DBItem>();
        //                insertDataList.Add(new DBItem("SERIAL", BloodTransfusionSerial, DBItem.DBDataType.String));
        //                insertDataList.Add(new DBItem("FEENO", ptinfo.FeeNo, DBItem.DBDataType.String));
        //                insertDataList.Add(new DBItem("CHARTNO", ptinfo.ChartNo, DBItem.DBDataType.String));
        //                insertDataList.Add(new DBItem("CREATE_TIME", DateTimeNow.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
        //                insertDataList.Add(new DBItem("CREATE_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
        //                insertDataList.Add(new DBItem("CREATE_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
        //                insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
        //                insertDataList.Add(new DBItem("RECORD_TIME", DateTimeNow.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
        //                insertDataList.Add(new DBItem("EDIT_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
        //                insertDataList.Add(new DBItem("EDIT_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
        //                erow = link.DBExecInsert("BLOODTRANSFUSION_DATA", insertDataList);
        //                if (erow == 0)
        //                {
        //                    json_result.status = RESPONSE_STATUS.ERROR;
        //                    json_result.message = "新增輸血記錄單失敗!";
        //                    return Content(JsonConvert.SerializeObject(json_result), "application/json");
        //                }
        //            }

        //            Serial = creatid("BLOODVERIFICATION_DATA", userinfo.EmployeesNo, ptinfo.FeeNo, "0");
        //            insertDataList = new List<DBItem>();
        //            insertDataList.Add(new DBItem("TABLE_ID", NewTableID, DBItem.DBDataType.String));
        //            insertDataList.Add(new DBItem("SERIAL", Serial, DBItem.DBDataType.String));
        //            insertDataList.Add(new DBItem("BLOODTARNSFUSION_SERIAL", BloodTransfusionSerial, DBItem.DBDataType.String));
        //            insertDataList.Add(new DBItem("FEENO", ptinfo.FeeNo, DBItem.DBDataType.String));
        //            insertDataList.Add(new DBItem("CHARTNO", ptinfo.ChartNo, DBItem.DBDataType.String));
        //            insertDataList.Add(new DBItem("CREATE_TIME", DateTimeNow.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
        //            insertDataList.Add(new DBItem("CREATE_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
        //            insertDataList.Add(new DBItem("CREATE_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
        //            insertDataList.Add(new DBItem("EDIT_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
        //            insertDataList.Add(new DBItem("EDIT_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
        //            insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
        //            insertDataList.Add(new DBItem("RECORD_TIME", DateTimeNow.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
        //            insertDataList.Add(new DBItem("START_TIME", start_time, DBItem.DBDataType.DataTime));
        //            insertDataList.Add(new DBItem("BLOOD_NAME", form["Blood_Name"], DBItem.DBDataType.String));
        //            insertDataList.Add(new DBItem("BLOOD_NUMBER", form["Blood_Number"], DBItem.DBDataType.String));
        //            insertDataList.Add(new DBItem("BLOOD_TYPE", form["Blood_Type"], DBItem.DBDataType.String));
        //            insertDataList.Add(new DBItem("BLOOD_UNIT", form["Blood_Unit"], DBItem.DBDataType.String));
        //            insertDataList.Add(new DBItem("BLOOD_EXP", form["Blood_Exp"], DBItem.DBDataType.DataTime));
        //            insertDataList.Add(new DBItem("BLOOD_VERIFYUSER1_ID", form["Blood_VerifyUser1_Id"], DBItem.DBDataType.String));
        //            insertDataList.Add(new DBItem("BLOOD_VERIFYUSER1_NAME", form["Blood_VerifyUser1_Name"], DBItem.DBDataType.String));
        //            insertDataList.Add(new DBItem("BLOOD_ID", form["Blood_ID"], DBItem.DBDataType.String));
        //            erow = link.DBExecInsert("BLOODVERIFICATION_DATA", insertDataList);
        //            if (erow == 0)
        //            {
        //                json_result.status = RESPONSE_STATUS.ERROR;
        //                json_result.message = "新增紀錄失敗!";
        //                return Content(JsonConvert.SerializeObject(json_result), "application/json");
        //            }
        //        }
        //        else
        //        {
        //            insertDataList = new List<DBItem>();
        //            insertDataList.Add(new DBItem("MODIFY_TIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
        //            insertDataList.Add(new DBItem("MODIFY_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
        //            insertDataList.Add(new DBItem("MODIFY_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
        //            insertDataList.Add(new DBItem("EDIT_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
        //            insertDataList.Add(new DBItem("EDIT_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
        //            insertDataList.Add(new DBItem("STATUS", "M", DBItem.DBDataType.String));
        //            string where = "SERIAL='" + Serial + "' AND STATUS ='Y'";
        //            erow = link.DBExecUpdate("BLOODVERIFICATION_DATA", insertDataList, where);
        //            insertDataList = new List<DBItem>();
        //            insertDataList.Add(new DBItem("TABLE_ID", NewTableID, DBItem.DBDataType.String));
        //            insertDataList.Add(new DBItem("SERIAL", Serial, DBItem.DBDataType.String));
        //            insertDataList.Add(new DBItem("BLOODTARNSFUSION_SERIAL", BloodTransfusionSerial, DBItem.DBDataType.String));
        //            insertDataList.Add(new DBItem("FEENO", ptinfo.FeeNo, DBItem.DBDataType.String));
        //            insertDataList.Add(new DBItem("CHARTNO", ptinfo.ChartNo, DBItem.DBDataType.String));
        //            insertDataList.Add(new DBItem("CREATE_TIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
        //            insertDataList.Add(new DBItem("CREATE_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
        //            insertDataList.Add(new DBItem("CREATE_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
        //            insertDataList.Add(new DBItem("EDIT_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
        //            insertDataList.Add(new DBItem("EDIT_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
        //            insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
        //            insertDataList.Add(new DBItem("RECORD_TIME", form["Record_Time"], DBItem.DBDataType.DataTime));
        //            insertDataList.Add(new DBItem("START_TIME", start_time, DBItem.DBDataType.DataTime));
        //            insertDataList.Add(new DBItem("BLOOD_NAME", form["Blood_Name"], DBItem.DBDataType.String));
        //            insertDataList.Add(new DBItem("BLOOD_NUMBER", form["Blood_Number"], DBItem.DBDataType.String));
        //            insertDataList.Add(new DBItem("BLOOD_TYPE", form["Blood_Type"], DBItem.DBDataType.String));
        //            insertDataList.Add(new DBItem("BLOOD_UNIT", form["Blood_Unit"], DBItem.DBDataType.String));
        //            insertDataList.Add(new DBItem("BLOOD_EXP", form["Blood_Exp"], DBItem.DBDataType.DataTime));
        //            insertDataList.Add(new DBItem("BLOOD_VERIFYUSER1_ID", form["Blood_VerifyUser1_Id"], DBItem.DBDataType.String));
        //            insertDataList.Add(new DBItem("BLOOD_VERIFYUSER1_NAME", form["Blood_VerifyUser1_Name"], DBItem.DBDataType.String));
        //            insertDataList.Add(new DBItem("BLOOD_ID", form["Blood_ID"], DBItem.DBDataType.String));
        //            erow = link.DBExecInsert("BLOODVERIFICATION_DATA", insertDataList);
        //            if (erow == 0)
        //            {
        //                json_result.status = RESPONSE_STATUS.ERROR;
        //                json_result.message = "修改紀錄失敗!";
        //                return Content(JsonConvert.SerializeObject(json_result), "application/json");
        //            }
        //        }

        //        json_result.status = RESPONSE_STATUS.SUCCESS;
        //        json_result.message = "儲存成功!";
        //        return Content(JsonConvert.SerializeObject(json_result), "application/json");
        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //    return new EmptyResult();
        //}
        #endregion

        #region (新增,編輯)核血作業 New
        /// <summary>
        /// (新增,編輯)核血作業 New
        /// </summary>
        /// <param name="form">表單內容</param>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult BloodVerificationSave(BloodTransfusion.BloodVerification_Save form)
        {
            RESPONSE_MSG json_result = new RESPONSE_MSG();
            try
            {
                var BloodTransfusionSerial = form.BloodTransfusionSerial;
                var BloodList = form.BloodList;
                if (BloodList.Length == 0)
                {
                    json_result.status = RESPONSE_STATUS.ERROR;
                    json_result.message = "無帶入任何血品!";
                    return Content(JsonConvert.SerializeObject(json_result), "application/json");
                }
                int erow = 0;
                List<DBItem> insertDataList = new List<DBItem>();
                string start_time = form.Blood_StartDay + " " + form.Blood_StartTime;
                var DateTimeNow = DateTime.Now;
                string NewTableID = "BLOODVERIFICATION_DATA_" + DateTime.Now.ToString("yyyyMMddHHmmssfff");

                string sql = "SELECT SERIAL FROM BLOODTRANSFUSION_DATA \n";
                sql += "WHERE SERIAL = '" + BloodTransfusionSerial + "' \n";
                DataTable dt = link.DBExecSQL(sql);
                if (dt.Rows.Count == 0)
                {
                    insertDataList = new List<DBItem>();
                    insertDataList.Add(new DBItem("SERIAL", BloodTransfusionSerial, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("FEENO", ptinfo.FeeNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CHARTNO", ptinfo.ChartNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATE_TIME", DateTimeNow.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("CREATE_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATE_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("RECORD_TIME", DateTimeNow.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("EDIT_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("EDIT_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("VERIFICATION_TIME", start_time, DBItem.DBDataType.DataTime));
                    erow = link.DBExecInsert("BLOODTRANSFUSION_DATA", insertDataList);
                    if (erow == 0)
                    {
                        json_result.status = RESPONSE_STATUS.ERROR;
                        json_result.message = "新增輸血記錄單失敗!";
                        return Content(JsonConvert.SerializeObject(json_result), "application/json");
                    }
                }

                // 取得已儲存血品資料
                var savedBloodList = bloodTransfusionModel.QueryBloodVerificationDataBySerial(BloodTransfusionSerial).AsEnumerable().ToList();

                // 註記舊有血品資料
                if (savedBloodList.Count > 0)
                {
                    insertDataList = new List<DBItem>();
                    insertDataList.Add(new DBItem("MODIFY_TIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("MODIFY_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("MODIFY_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("EDIT_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("EDIT_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("STATUS", "M", DBItem.DBDataType.String));
                    string where = "BLOODTARNSFUSION_SERIAL='" + BloodTransfusionSerial + "' AND STATUS ='Y'";
                    erow = link.DBExecUpdate("BLOODVERIFICATION_DATA", insertDataList, where);
                }

                foreach (var blood in BloodList)
                {
                    try
                    {
                        var matchSavedBlood = savedBloodList.Where(x => x["BLOOD_ID"].ToString() == blood.Blood_ID).FirstOrDefault();
                        var Serial = creatid("BLOODVERIFICATION_DATA", userinfo.EmployeesNo, ptinfo.FeeNo, "0");
                        if (matchSavedBlood != null)
                        {
                            Serial = matchSavedBlood["SERIAL"].ToString();
                        }
                        insertDataList = new List<DBItem>();
                        insertDataList.Add(new DBItem("TABLE_ID", "BLOODVERIFICATION_DATA_" + DateTime.Now.ToString("yyyyMMddHHmmssfff"), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("SERIAL", Serial, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("BLOODTARNSFUSION_SERIAL", BloodTransfusionSerial, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("FEENO", ptinfo.FeeNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CHARTNO", ptinfo.ChartNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CREATE_TIME", DateTimeNow.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("CREATE_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CREATE_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("EDIT_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("EDIT_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("RECORD_TIME", DateTimeNow.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("START_TIME", start_time, DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("BLOOD_NAME", blood.Blood_Name, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("BLOOD_NUMBER", blood.Blood_Number, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("BLOOD_TYPE", blood.Blood_Type, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("BLOOD_UNIT", blood.Blood_Unit, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("BLOOD_EXP", blood.Blood_Exp, DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("BLOOD_VERIFYUSER1_ID", form.Blood_VerifyUser1_Id, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("BLOOD_VERIFYUSER1_NAME", form.Blood_VerifyUser1_Name, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("BLOOD_ID", blood.Blood_ID, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("BLOOD_TIME", blood.Blood_Time, DBItem.DBDataType.DataTime));
                        erow = link.DBExecInsert("BLOODVERIFICATION_DATA", insertDataList);
                        if (erow == 0)
                        {
                            this.log.saveLogMsg($"[BloodVerificationSaveError] BloodTransfusionSerial:{form.BloodTransfusionSerial} UserId:{userinfo.EmployeesNo} BloodID:{blood.Blood_ID}", "BloodTransfusion");
                        }

                    }
                    catch (Exception ex)
                    {
                        this.log.saveLogMsg($"[BloodVerificationSaveError] BloodTransfusionSerial:{form.BloodTransfusionSerial} UserId:{userinfo.EmployeesNo} BloodID:{blood.Blood_ID} Error:" + ex.Message.ToString(), "BloodTransfusion");
                    }
                }
                //新增護理紀錄
                string careRecordStr = form.CareRecordStr;
                if (Upd_CareRecord(DateTimeNow.ToString("yyyy/MM/dd HH:mm:ss"), BloodTransfusionSerial, "核血", careRecordStr, "", "", "", "", "BLOODVERIFICATION") == 0)
                {
                    base.Insert_CareRecord_Black(DateTimeNow.ToString("yyyy/MM/dd HH:mm:ss"), BloodTransfusionSerial, "核血", careRecordStr, "", "", "", "", "BLOODVERIFICATION");
                }

                // 更新主表核血時間
                insertDataList = new List<DBItem>();
                insertDataList.Add(new DBItem("VERIFICATION_TIME", start_time, DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("MODIFY_TIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("MODIFY_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MODIFY_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EDIT_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EDIT_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                string bloodTransfusionwhere = "SERIAL='" + BloodTransfusionSerial + "' AND STATUS ='Y'";
                erow = link.DBExecUpdate("BLOODTRANSFUSION_DATA", insertDataList, bloodTransfusionwhere);
            }
            catch (Exception ex)
            {
                this.log.saveLogMsg($"[BloodVerificationSaveError] BloodTransfusionSerial:{form.BloodTransfusionSerial} UserId:{userinfo.EmployeesNo} Error:" + ex.Message.ToString(), "BloodTransfusion");
                json_result.status = RESPONSE_STATUS.ERROR;
                json_result.message = "儲存失敗!";
                return Content(JsonConvert.SerializeObject(json_result), "application/json");
            }

            json_result.status = RESPONSE_STATUS.SUCCESS;
            json_result.message = "儲存成功!";
            return Content(JsonConvert.SerializeObject(json_result), "application/json");
        }
        #endregion

        #region 雙人覆核(新增)
        /// <summary>
        /// 雙人覆核(新增)
        /// </summary>
        /// <param name="form">人員資訊</param>
        /// <returns></returns>
        public string InsDoubleChk(FormCollection form)
        {
            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;

            var type = form["CHKTYPE"];

            var chkuser = "";
            var chkupass = "";

            if (type == "VerifyUser1")
            {
                chkuser = form["Blood_VerifyUser1_Id"];
                chkupass = form["Blood_VerifyUser1_Password"];
            }

            if (chkuser == userno)
                return "執行覆核者不得與建立者同一人!";

            byte[] listByteCode = webService.UserLogin(chkuser, chkupass);
            if (listByteCode == null)
                return "帳號密碼錯誤!";

            return "覆核成功!";
        }
        #endregion

        #region UserName取得員工姓名
        /// <summary>
        /// 呼叫WebService取得員工姓名
        /// </summary>
        /// <param name="userno">員工編號</param>
        /// <returns></returns>
        public string Get_Employee_Name(string userno)
        {
            byte[] listByteCode = webService.UserName(userno);
            if (listByteCode == null)
                return "";
            string listJsonArray = CompressTool.DecompressString(listByteCode);
            UserInfo user = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
            return user.EmployeesName;
        }
        #endregion
        #endregion

        #region 生命徵象               
        /// <summary>
        /// 編輯生命徵象畫面
        /// </summary>
        /// <param name="BloodTransfusionSerial">輸血紀錄單Serial</param>
        /// <param name="Period">時間點(輸血前15分鐘'15minBefore'/輸血後15分鐘'15minAfter'/輸血結束'End')</param>
        public ActionResult VitalSign(string BloodTransfusionSerial, string Period)
        {
            if (BloodTransfusionSerial != null)
            {
                ViewBag.Period = Period;
                ViewBag.BloodTransfusionSerial = BloodTransfusionSerial;
                ViewBag.BloodVitalSignData = bloodTransfusionModel.QueryBloodVitalSignDataBySerial(BloodTransfusionSerial, Period);
            }

            return View();
        }

        #region 新增、編輯生命徵象
        /// <summary>
        /// 新增、編輯生命徵象
        /// </summary>
        /// <param name="form">表單內容</param>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult VitalSignSave(FormCollection form)
        {
            RESPONSE_MSG json_result = new RESPONSE_MSG();
            try
            {
                var BloodTransfusionSerial = form["BloodTransfusionSerial"].ToString();
                var Serial = form["VS_Serial"].ToString();
                var Period = form["VS_Period"].ToString();
                int erow = 0;
                List<DBItem> insertDataList = new List<DBItem>();
                var DateTimeNow = DateTime.Now;
                string NewTableID = "BLOODVITALSIGN_DATA_" + DateTime.Now.ToString("yyyyMMddHHmmssfff");

                //判斷新增或編輯
                if (string.IsNullOrEmpty(Serial)) //新增
                {
                    //檢查輸血紀錄單Serial是否存在
                    string sql = "SELECT SERIAL FROM BLOODTRANSFUSION_DATA \n";
                    sql += "WHERE SERIAL = '" + BloodTransfusionSerial + "' \n";
                    DataTable dt = link.DBExecSQL(sql);
                    if (dt.Rows.Count == 0)
                    {
                        insertDataList = new List<DBItem>();
                        insertDataList.Add(new DBItem("SERIAL", BloodTransfusionSerial, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("FEENO", ptinfo.FeeNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CHARTNO", ptinfo.ChartNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CREATE_TIME", DateTimeNow.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("CREATE_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CREATE_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("EDIT_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("EDIT_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("RECORD_TIME", DateTimeNow.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                        erow = link.DBExecInsert("BLOODTRANSFUSION_DATA", insertDataList);
                        if (erow == 0)
                        {
                            json_result.status = RESPONSE_STATUS.ERROR;
                            json_result.message = "新增輸血記錄單失敗!";
                            return Content(JsonConvert.SerializeObject(json_result), "application/json");
                        }
                    }
                    Serial = creatid("BLOODVITALSIGN_DATA", userinfo.EmployeesNo, ptinfo.FeeNo, "0");
                    string recordTime = DateTimeNow.ToString("yyyy/MM/dd HH:mm");
                    insertDataList = new List<DBItem>();
                    insertDataList.Add(new DBItem("TABLE_ID", NewTableID, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("SERIAL", Serial, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("BLOODTARNSFUSION_SERIAL", BloodTransfusionSerial, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("FEENO", ptinfo.FeeNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CHARTNO", ptinfo.ChartNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATE_TIME", DateTimeNow.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("CREATE_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATE_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("EDIT_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("EDIT_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("RECORD_TIME", recordTime, DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("PERIOD", Period, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("BT", form["BT"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("HR", form["HR"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("RESPIRATORY", form["Respiratory"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("BP_SYSTOLIC", form["BP_Systolic"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("BP_DIASTOLIC", form["BP_Diastolic"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("SPO2", form["SPO2"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASSESS_TIME", form["Assess_Time"], DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("VS_ID", form["VS_Id"].ToString(), DBItem.DBDataType.String));
                    if (Period != "15minBefore")
                    {
                        insertDataList.Add(new DBItem("IS_ALLERGIC", form["IsAllergic"].ToString(), DBItem.DBDataType.String));
                    }
                    erow = link.DBExecInsert("BLOODVITALSIGN_DATA", insertDataList);
                    if (erow == 0)
                    {
                        this.log.saveLogMsg($"[VitalSignSaveError][新增] BloodTransfusionSerial:{form["BloodTransfusionSerial"].ToString()} UserId:{userinfo.EmployeesNo} VSID:{form["VS_Id"].ToString()}", "BloodTransfusion");
                        json_result.status = RESPONSE_STATUS.ERROR;
                        json_result.message = "新增紀錄失敗!";
                        return Content(JsonConvert.SerializeObject(json_result), "application/json");
                    }
                    else
                    {
                        //新增護理紀錄
                        string content = form["CareRecordContent"];
                        string title = form["CareRecordTitle"];

                        base.Insert_CareRecord_Black(recordTime, Serial, title, content, "", "", "", "", "BLOODVITALSIGN");

                    }
                }
                else
                {
                    string recordTime = DateTimeNow.ToString("yyyy/MM/dd HH:mm");
                    insertDataList = new List<DBItem>();
                    insertDataList.Add(new DBItem("MODIFY_TIME", DateTimeNow.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("MODIFY_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("MODIFY_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("EDIT_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("EDIT_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("STATUS", "M", DBItem.DBDataType.String));
                    string where = "SERIAL='" + Serial + "' AND STATUS ='Y'";
                    erow = link.DBExecUpdate("BLOODVITALSIGN_DATA", insertDataList, where);
                    insertDataList = new List<DBItem>();
                    insertDataList.Add(new DBItem("TABLE_ID", NewTableID, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("SERIAL", Serial, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("BLOODTARNSFUSION_SERIAL", BloodTransfusionSerial, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("FEENO", ptinfo.FeeNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CHARTNO", ptinfo.ChartNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATE_TIME", DateTimeNow.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("CREATE_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATE_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("EDIT_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("EDIT_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("RECORD_TIME", recordTime, DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("PERIOD", Period, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("BT", form["BT"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("HR", form["HR"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("RESPIRATORY", form["Respiratory"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("BP_SYSTOLIC", form["BP_Systolic"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("BP_DIASTOLIC", form["BP_Diastolic"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("SPO2", form["SPO2"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASSESS_TIME", form["Assess_Time"], DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("VS_ID", form["VS_Id"].ToString(), DBItem.DBDataType.String));
                    if (Period != "15minBefore")
                    {
                        insertDataList.Add(new DBItem("IS_ALLERGIC", form["IsAllergic"].ToString(), DBItem.DBDataType.String));
                    }
                    erow = link.DBExecInsert("BLOODVITALSIGN_DATA", insertDataList);
                    if (erow == 0)
                    {
                        this.log.saveLogMsg($"[VitalSignSaveError][修改] BloodTransfusionSerial:{form["BloodTransfusionSerial"].ToString()} UserId:{userinfo.EmployeesNo} VSID:{form["VS_Id"].ToString()}", "BloodTransfusion");
                        json_result.status = RESPONSE_STATUS.ERROR;
                        json_result.message = "修改紀錄失敗!";
                        return Content(JsonConvert.SerializeObject(json_result), "application/json");
                    }
                    else
                    {
                        //更新護理紀錄
                        string content = form["CareRecordContent"];
                        string title = form["CareRecordTitle"];

                        base.Upd_CareRecord(recordTime, Serial, title, content, "", "", "", "", "BLOODVITALSIGN");

                    }
                }
            }
            catch (Exception ex)
            {
                this.log.saveLogMsg($"[VitalSignSaveError] BloodTransfusionSerial:{form["BloodTransfusionSerial"].ToString()} UserId:{userinfo.EmployeesNo} VSID:{form["VS_Id"].ToString()} Error:" + ex.Message.ToString(), "BloodTransfusion");
            }
            json_result.status = RESPONSE_STATUS.SUCCESS;
            json_result.message = "儲存成功!";
            return Content(JsonConvert.SerializeObject(json_result), "application/json");
        }
        #endregion

        #region 生命徵象帶入資料
        public ActionResult VitalSign_Interfacing(string starttime, string endtime, string feeno, string BloodTransfusionSerial)
        {

            string start = DateTime.Now.ToString("yyyy/MM/dd 00:00");
            string end = DateTime.Now.AddMinutes(2).ToString("yyyy/MM/dd HH:mm");
            if (starttime != null && endtime != null)
            {
                start = starttime;
                end = endtime;
            }

            List<string[]> vsId = new List<string[]>();
            List<BloodTransfusion.VitalSignImport> vsList = new List<BloodTransfusion.VitalSignImport>();
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

            // 開始處理資料
            for (int i = 0; i <= vsId.Count - 1; i++)
            {
                //初始化資料
                BloodTransfusion.VitalSignImport vsdl = new BloodTransfusion.VitalSignImport();

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

            // 取得已帶入過的生命徵象vs_id,record_time
            var BloodVitalSignDataList = bloodTransfusionModel.QueryBloodVitalSignDataBySerial(BloodTransfusionSerial, "").AsEnumerable().OrderBy(x => x["RECORD_TIME"]).ToList();
            var vs_idList = new List<Dictionary<string, string>>();
            if (BloodVitalSignDataList.Count > 0)
            {
                for (var i = 0; i < BloodVitalSignDataList.Count; i++)
                {
                    var matchBloodID = vs_idList.FirstOrDefault(x => x["vs_id"].ToString() == BloodVitalSignDataList[i]["vs_id"].ToString());
                    if (matchBloodID != null)
                    {
                        if (DateTime.Parse(BloodVitalSignDataList[i]["RECORD_TIME"].ToString()) < DateTime.Parse(matchBloodID["record_time"].ToString()))
                        {
                            continue;
                        }
                    }
                    Dictionary<string, string> TempData = new Dictionary<string, string>();
                    TempData.Add("vs_id", BloodVitalSignDataList[i]["vs_id"].ToString());
                    TempData.Add("record_time", BloodVitalSignDataList[i]["RECORD_TIME"].ToString());
                    vs_idList.Add(TempData);
                }
            }
            ViewBag.VSID_List = vs_idList;


            ViewData["result"] = vsList;

            return View();
        }
        #endregion

        #endregion

        #region 輸血反應     
        public ActionResult TransfusionReactionListData(string BloodTransfusionSerial)
        {
            ViewBag.BloodTransfusionSerial = BloodTransfusionSerial;
            ViewBag.TransfusionReactionList = bloodTransfusionModel.QueryTransfusionReactionDataBySerial(BloodTransfusionSerial, "");
            return View();
        }

        public ActionResult TransfusionReaction(string BloodTransfusionSerial, string Serial)
        {
            if (!string.IsNullOrEmpty(BloodTransfusionSerial))
            {
                ViewBag.BloodTransfusionSerial = BloodTransfusionSerial;
            }

            if (!string.IsNullOrEmpty(Serial))
            {
                ViewBag.TransfusionReaction = bloodTransfusionModel.QueryTransfusionReactionDataBySerial(BloodTransfusionSerial, Serial);
            }

            return View();
        }

        #region 輸血反應(新增，修改)
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult TransfusionReactionSave(BloodTransfusion.TransfusionReaction_Save form)
        {
            RESPONSE_MSG json_result = new RESPONSE_MSG();
            try
            {
                var BloodTransfusionSerial = form.BloodTransfusionSerial;
                var Serial = form.Serial;
                int erow = 0;
                List<DBItem> insertDataList = new List<DBItem>();
                var DateTimeNow = DateTime.Now;
                string NewTableID = "BLOODTRANSREACTION_DATA_" + DateTime.Now.ToString("yyyyMMddHHmmssfff");

                var TransfusionReact = form.TransfusionReact;
                var REACT_SYMPTOM_STR = "";
                if (!string.IsNullOrEmpty(form.ReactSymptom) && form.ReactSymptom.Length > 0)
                {
                    REACT_SYMPTOM_STR = string.Join(",", form.ReactSymptom);
                }
                //判斷新增或編輯
                if (string.IsNullOrEmpty(Serial)) //新增
                {
                    //檢查輸血紀錄單Serial是否存在
                    string sql = "SELECT SERIAL FROM BLOODTRANSFUSION_DATA \n";
                    sql += "WHERE SERIAL = '" + BloodTransfusionSerial + "' \n";
                    DataTable dt = link.DBExecSQL(sql);
                    if (dt.Rows.Count == 0)
                    {
                        insertDataList = new List<DBItem>();
                        insertDataList.Add(new DBItem("SERIAL", BloodTransfusionSerial, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("FEENO", ptinfo.FeeNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CHARTNO", ptinfo.ChartNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CREATE_TIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("CREATE_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CREATE_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("EDIT_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("EDIT_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("RECORD_TIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                        erow = link.DBExecInsert("BLOODTRANSFUSION_DATA", insertDataList);
                        if (erow == 0)
                        {
                            json_result.status = RESPONSE_STATUS.ERROR;
                            json_result.message = "新增輸血記錄單失敗!";
                            Content(JsonConvert.SerializeObject(json_result), "application/json");
                        }
                    }
                    string recordTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm");
                    Serial = creatid("BLOODTRANSREACTION_DATA", userinfo.EmployeesNo, ptinfo.FeeNo, "0");
                    insertDataList = new List<DBItem>();
                    insertDataList.Add(new DBItem("TABLE_ID", NewTableID, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("SERIAL", Serial, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("BLOODTARNSFUSION_SERIAL", BloodTransfusionSerial, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("FEENO", ptinfo.FeeNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CHARTNO", ptinfo.ChartNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATE_TIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("CREATE_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATE_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("EDIT_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("EDIT_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("RECORD_TIME", recordTime, DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("TRANSFUSION_REACT", form.TransfusionReact, DBItem.DBDataType.String));
                    if (TransfusionReact == "有")
                    {
                        //insertDataList.Add(new DBItem("REACT_TIME", form.ReactDay + " " + form.ReactTime, DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("REACT_SYMPTOM", REACT_SYMPTOM_STR, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("TXT_REACT_SYMPTOM_OTHER", form.txt_ReactSymptom_Other, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("REACT_PROCEDURE", form.ReactProcedure, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("VS_ID", form.VS_Id, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("ASSESS_TIME", form.Assess_Time, DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("BT", form.BT, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("HR", form.HR, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("RESPIRATORY", form.Respiratory, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("BP_SYSTOLIC", form.BP_Systolic, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("BP_DIASTOLIC", form.BP_Diastolic, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("SPO2", form.SPO2, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("MD_TRA", form.MD_TRA, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("DC_BLOODTRANSFUSION", form.DC_BloodTransfusion, DBItem.DBDataType.String));
                    }
                    erow = link.DBExecInsert("BLOODTRANSREACTION_DATA", insertDataList);
                    if (erow == 0)
                    {
                        this.log.saveLogMsg($"[TransfusionReactionSaveError][新增] BloodTransfusionSerial:{form.BloodTransfusionSerial} UserId:{userinfo.EmployeesNo} VS_Id:{form.VS_Id}", "BloodTransfusion");
                        json_result.status = RESPONSE_STATUS.ERROR;
                        json_result.message = "新增紀錄失敗!";
                        return Content(JsonConvert.SerializeObject(json_result), "application/json");
                    }
                    else
                    {
                        //新增護理紀錄
                        string content = form.CareRecordStr;
                        string title = "輸血不適症狀";

                        base.Insert_CareRecord_Black(recordTime, Serial, title, content, "", "", "", "", "BLOODTRANSREACTION");

                    }

                }
                else
                {
                    // 編輯時保留原始RecordTime(以第一筆為準)
                    DataTable BloodTransferReacttionNow = bloodTransfusionModel.QueryTransfusionReactionDataBySerial(BloodTransfusionSerial, Serial);
                    var RecordTimeNow = "";
                    if (BloodTransferReacttionNow.Rows.Count > 0)
                    {
                        DateTime dateTime;
                        if (DateTime.TryParse(BloodTransferReacttionNow.Rows[0]["RECORD_TIME"].ToString(), out dateTime))
                        {
                            RecordTimeNow = dateTime.ToString("yyyy/MM/dd HH:mm");
                        }
                    }

                    insertDataList = new List<DBItem>();
                    insertDataList.Add(new DBItem("MODIFY_TIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("MODIFY_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("MODIFY_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("EDIT_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("EDIT_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("STATUS", "M", DBItem.DBDataType.String));
                    string where = "SERIAL='" + Serial + "' AND STATUS ='Y'";
                    erow = link.DBExecUpdate("BLOODTRANSREACTION_DATA", insertDataList, where);

                    insertDataList = new List<DBItem>();
                    insertDataList.Add(new DBItem("TABLE_ID", NewTableID, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("SERIAL", Serial, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("BLOODTARNSFUSION_SERIAL", BloodTransfusionSerial, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("FEENO", ptinfo.FeeNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CHARTNO", ptinfo.ChartNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATE_TIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("CREATE_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATE_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("EDIT_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("EDIT_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("RECORD_TIME", RecordTimeNow, DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("TRANSFUSION_REACT", form.TransfusionReact, DBItem.DBDataType.String));
                    if (TransfusionReact == "有")
                    {
                        //insertDataList.Add(new DBItem("REACT_TIME", form.ReactDay + " " + form.ReactTime, DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("REACT_SYMPTOM", REACT_SYMPTOM_STR, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("TXT_REACT_SYMPTOM_OTHER", form.txt_ReactSymptom_Other, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("REACT_PROCEDURE", form.ReactProcedure, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("VS_ID", form.VS_Id, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("ASSESS_TIME", form.Assess_Time, DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("BT", form.BT, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("HR", form.HR, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("RESPIRATORY", form.Respiratory, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("BP_SYSTOLIC", form.BP_Systolic, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("BP_DIASTOLIC", form.BP_Diastolic, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("SPO2", form.SPO2, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("MD_TRA", form.MD_TRA, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("DC_BLOODTRANSFUSION", form.DC_BloodTransfusion, DBItem.DBDataType.String));
                    }
                    erow = link.DBExecInsert("BLOODTRANSREACTION_DATA", insertDataList);
                    if (erow == 0)
                    {
                        this.log.saveLogMsg($"[TransfusionReactionSaveError][修改] BloodTransfusionSerial:{form.BloodTransfusionSerial} UserId:{userinfo.EmployeesNo} VS_Id:{form.VS_Id}", "BloodTransfusion");
                        json_result.status = RESPONSE_STATUS.ERROR;
                        json_result.message = "修改紀錄失敗!";
                        return Content(JsonConvert.SerializeObject(json_result), "application/json");
                    }
                    else
                    {
                        //更新護理紀錄
                        string content = form.CareRecordStr;
                        string title = "輸血不適症狀";

                        base.Upd_CareRecord(RecordTimeNow, Serial, title, content, "", "", "", "", "BLOODTRANSREACTION");

                    }
                }

                // 若停止輸血為是且生命徵象(輸血結束)未有紀錄，新增一筆相同生命徵象的紀錄
                if (form.DC_BloodTransfusion == "是")
                {
                    DataTable vitalSignEnd = bloodTransfusionModel.QueryBloodVitalSignDataBySerial(BloodTransfusionSerial, "End");
                    if (vitalSignEnd.Rows.Count == 0)
                    {
                        insertDataList = new List<DBItem>();
                        insertDataList.Add(new DBItem("TABLE_ID", NewTableID, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("SERIAL", creatid("BLOODVITALSIGN_DATA", userinfo.EmployeesNo, ptinfo.FeeNo, "0"), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("BLOODTARNSFUSION_SERIAL", BloodTransfusionSerial, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("FEENO", ptinfo.FeeNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CHARTNO", ptinfo.ChartNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CREATE_TIME", DateTimeNow.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("CREATE_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CREATE_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("EDIT_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("EDIT_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("RECORD_TIME", DateTimeNow.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("PERIOD", "End", DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("BT", form.BT, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("HR", form.HR, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("RESPIRATORY", form.Respiratory, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("BP_SYSTOLIC", form.BP_Systolic, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("BP_DIASTOLIC", form.BP_Diastolic, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("SPO2", form.SPO2, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("ASSESS_TIME", form.Assess_Time, DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("VS_ID", form.VS_Id.ToString(), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("IS_ALLERGIC", "是", DBItem.DBDataType.String));
                        erow = link.DBExecInsert("BLOODVITALSIGN_DATA", insertDataList);
                        if (erow == 0)
                        {
                            this.log.saveLogMsg($"[VitalSignSaveError][新增] BloodTransfusionSerial:{form.BloodTransfusionSerial.ToString()} UserId:{userinfo.EmployeesNo} VSID:{form.VS_Id.ToString()}", "BloodTransfusion");
                            json_result.status = RESPONSE_STATUS.ERROR;
                            json_result.message = "新增紀錄失敗!";
                            return Content(JsonConvert.SerializeObject(json_result), "application/json");
                        }
 
                    }
                }

                json_result.status = RESPONSE_STATUS.SUCCESS;
                json_result.message = "儲存成功!";
                return Content(JsonConvert.SerializeObject(json_result), "application/json");
            }
            catch (Exception ex)
            {
                this.log.saveLogMsg($"[TransfusionReactionSaveError][修改] BloodTransfusionSerial:{form.BloodTransfusionSerial} UserId:{userinfo.EmployeesNo} VS_Id:{form.VS_Id} Error:" + ex.Message.ToString(), "BloodTransfusion");
            }
            return new EmptyResult();
        }
        #endregion

        #region 輸血反應(刪除)
        /// <summary>
        /// 輸血反應(刪除)
        /// </summary>
        /// <param name="Serial">輸血反應Serial</param>
        public ActionResult TransfusionReactionDelete(string Serial)
        {
            try
            {
                RESPONSE_MSG json_result = new RESPONSE_MSG();
                if (string.IsNullOrEmpty(Serial))
                {
                    json_result.status = RESPONSE_STATUS.ERROR;
                    json_result.message = "刪除失敗!";
                    return Content(JsonConvert.SerializeObject(json_result), "application/json");
                }
                int erow = 0;
                List<DBItem> insertDataList = new List<DBItem>();

                insertDataList = new List<DBItem>();
                insertDataList.Add(new DBItem("DELETE_TIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("DELETE_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EDIT_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EDIT_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("STATUS", "D", DBItem.DBDataType.String));
                string where = "SERIAL='" + Serial + "' AND STATUS ='Y'";
                erow = link.DBExecUpdate("BLOODTRANSREACTION_DATA", insertDataList, where);
                if (erow == 0)
                {
                    json_result.status = RESPONSE_STATUS.ERROR;
                    json_result.message = "刪除失敗!";
                    return Content(JsonConvert.SerializeObject(json_result), "application/json");
                }
                else
                {
                    json_result.status = RESPONSE_STATUS.SUCCESS;
                    json_result.message = "刪除成功!";
                    base.Del_CareRecord(Serial, "BLOODTRANSREACTION");

                    return Content(JsonConvert.SerializeObject(json_result), "application/json");
                }
            }
            catch (Exception ex)
            {

            }
            return new EmptyResult();
        }
        #endregion

        #region 生命徵象帶入資料(輸血反應)
        /// <summary>
        /// 生命徵象帶入資料(輸血反應)
        /// </summary>
        /// <param name="starttime">開始時間</param>
        /// <param name="endtime">結束時間</param>
        /// <param name="feeno">批價號</param>
        /// <param name="BloodTransfusionSerial">輸血紀錄單Serial</param>
        public ActionResult TransfusionReaction_VitalSign_Interfacing(string starttime, string endtime, string feeno, string BloodTransfusionSerial)
        {

            string start = DateTime.Now.ToString("yyyy/MM/dd 00:00");
            string end = DateTime.Now.AddMinutes(2).ToString("yyyy/MM/dd HH:mm");
            if (starttime != null && endtime != null)
            {
                start = starttime;
                end = endtime;
            }

            List<string[]> vsId = new List<string[]>();
            List<BloodTransfusion.VitalSignImport> vsList = new List<BloodTransfusion.VitalSignImport>();
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

            // 開始處理資料
            for (int i = 0; i <= vsId.Count - 1; i++)
            {
                //初始化資料
                BloodTransfusion.VitalSignImport vsdl = new BloodTransfusion.VitalSignImport();

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

            // 取得已帶入過的生命徵象vs_id,record_time
            var BloodTransReactionDataList = bloodTransfusionModel.QueryTransfusionReactionDataBySerial(BloodTransfusionSerial, "").AsEnumerable().OrderBy(x => x["RECORD_TIME"]).ToList();
            var vs_idList = new List<Dictionary<string, string>>();
            if (BloodTransReactionDataList.Count > 0)
            {
                for (var i = 0; i < BloodTransReactionDataList.Count; i++)
                {
                    var matchBloodID = vs_idList.FirstOrDefault(x => x["vs_id"].ToString() == BloodTransReactionDataList[i]["vs_id"].ToString());
                    if (matchBloodID != null)
                    {
                        if (DateTime.Parse(BloodTransReactionDataList[i]["RECORD_TIME"].ToString()) < DateTime.Parse(matchBloodID["record_time"].ToString()))
                        {
                            continue;
                        }
                    }
                    Dictionary<string, string> TempData = new Dictionary<string, string>();
                    TempData.Add("vs_id", BloodTransReactionDataList[i]["vs_id"].ToString());
                    TempData.Add("record_time", BloodTransReactionDataList[i]["RECORD_TIME"].ToString());
                    vs_idList.Add(TempData);
                }
            }
            ViewBag.VSID_List = vs_idList;
            ViewData["result"] = vsList;

            return View();
        }
        #endregion

        #endregion
    }
}