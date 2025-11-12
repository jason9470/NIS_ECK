using Com.Mayaminer;
using Newtonsoft.Json;
using NIS.Data;
using NIS.Models;
using NIS.UtilTool;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static NIS.Models.SelfPaymentConsent;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using static NIS.Models.SelfPaymentConsent.NisCreateSignResponse;
using System.Runtime.Remoting.Messaging;
using System.Security.Policy;
using DocumentFormat.OpenXml.Drawing.Charts;
using DataTable = System.Data.DataTable;

namespace NIS.Controllers
{
    public class SelfPaymentConsentController : BaseController
    {
        LogTool log = new LogTool();
        private DBConnector link;
        private string mode = MvcApplication.iniObj.NisSetting.ServerMode.ToString();
        SelfPaymentConsent SelfPaymentConsentM = new SelfPaymentConsent();

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

        public SelfPaymentConsentController()
        {
            this.link = new DBConnector();
        }

        // 首頁
        public ActionResult Index(string type = "item")
        {
            if (Session["PatInfo"] == null)
            {
                Response.Write("<script>alert('請重新選擇病患');</script>");
                return new EmptyResult();
            }
            ViewBag.type = type;

            return View();
        }

        // 自費品項資料列表
        public ActionResult ItemList()
        {
            return View();
        }

        // 自費品項資料列表
        public ActionResult ItemListData(string feeno, string start, string end, string consentStatus = "all")
        {
            DataTable ComfirmDetail = SelfPaymentConsentM.queryComfirmDetail(feeno, start, end);
            if (ComfirmDetail.Rows.Count > 0)
            {
                switch (consentStatus)
                {
                    //判斷篩選條件(是否已產生同意書)
                    case "generated":
                        var filterData = ComfirmDetail.AsEnumerable().Where(x => x["SELF_PAY_CONSENT"].ToString() == "Y");
                        ComfirmDetail = filterData.Any() ? filterData.CopyToDataTable() : ComfirmDetail.Clone();
                        break;
                    case "notGenerated":
                        filterData = ComfirmDetail.AsEnumerable().Where(x => x["SELF_PAY_CONSENT"].ToString() == "");
                        ComfirmDetail = filterData.Any() ? filterData.CopyToDataTable() : ComfirmDetail.Clone();
                        break;
                }

                ComfirmDetail.Columns.Add("CREATE_NAME", typeof(string));
                ComfirmDetail.Columns["CREATE_NAME"].ReadOnly = false;
                //檢查是否有URL欄位，若沒有新增此欄位
                if (!ComfirmDetail.Columns.Contains("SELF_PAY_CONSENT_LINK"))
                {
                    ComfirmDetail.Columns.Add("SELF_PAY_CONSENT_LINK", typeof(string));
                }
                ComfirmDetail.Columns["SELF_PAY_CONSENT_LINK"].ReadOnly = false;
                Dictionary<string, string> userNameDict = new Dictionary<string, string>();
                foreach (DataRow row in ComfirmDetail.Rows)
                {
                    var userId = row["CREATE_ID"].ToString().Trim();
                    string userName = "";
                    if (userNameDict.TryGetValue(userId, out userName))
                    {
                        row["CREATE_NAME"] = userName;
                    }
                    else
                    {
                        byte[] listByteCode = webService.UserName(userId);
                        if (listByteCode != null)
                        {
                            string listJsonArray = CompressTool.DecompressString(listByteCode);
                            UserInfo user_name = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                            row["CREATE_NAME"] = user_name.EmployeesName.ToString().Trim();
                            userNameDict.Add(userId, user_name.EmployeesName.ToString().Trim());
                        }
                    }

                    // 取得主檔存的自費同意書URL
                    //DataTable currentMaster = SelfPaymentConsentM.queryConsentMaster(ptinfo.FeeNo.Trim(), "", "", "", row["SELF_PAY_CONSENT_ID"].ToString());

                    //if (currentMaster !=null && currentMaster.Rows.Count > 0)
                    //{
                    //    row["SELF_PAY_CONSENT_LINK"] = currentMaster.Rows[0]["CONSENT_URL"].ToString();
                    //}

                    //取得最新的自費同意書URL
                    if (!string.IsNullOrEmpty(row["SELF_PAY_CONSENT_JOB_ID"].ToString()))
                    {
                        HttpClient client = new HttpClient();
                        string url = $"http://172.20.110.185:81/api/GetSignDocUrl?jobid={row["SELF_PAY_CONSENT_JOB_ID"]}&amp;readMode=Y";
                        try
                        {
                            HttpResponseMessage response = client.GetAsync(url).Result;

                            if (response.IsSuccessStatusCode)
                            {
                                string responseBody = response.Content.ReadAsStringAsync().Result;
                                row["SELF_PAY_CONSENT_LINK"] = responseBody;
                            }
                            else
                            {
                                this.log.saveLogMsg($"[ItemListData][GetSignDocUrl][取得同意書URL API][Error][jobId:{row["JOB_ID"]}\nresponse:{JsonConvert.SerializeObject(response)}", "SelfPaymentConsent");
                            }
                        }
                        catch (HttpRequestException e)
                        {
                            this.log.saveLogMsg($"[ItemListData][GetSignDocUrl][取得同意書URL API][Error][jobId:{row["JOB_ID"]}\nError:{e.Message.ToString()}", "SelfPaymentConsent");
                        }
                    }
                }
            }
            ViewBag.ComfirmDetail = ComfirmDetail;
            return View();
        }

        // 自費品同意書列表
        public ActionResult ConsentList()
        {
            return View();
        }

        // 自費品同意書列表
        public ActionResult ConsentListData(string feeno, string start, string end, string jobStatus = "")
        {
            DataTable ConsentMaster = SelfPaymentConsentM.queryConsentMaster(feeno, start, end, jobStatus);
            if (ConsentMaster.Rows.Count > 0)
            {
                //檢查是否有URL欄位，若沒有新增此欄位
                if (!ConsentMaster.Columns.Contains("CONSENT_URL"))
                {
                    ConsentMaster.Columns.Add("CONSENT_URL", typeof(string));
                }
                ConsentMaster.Columns["CONSENT_URL"].ReadOnly = false;
                //檢查是否有簽核狀態欄位，若沒有新增此欄位
                if (!ConsentMaster.Columns.Contains("JOB_STATUS"))
                {
                    ConsentMaster.Columns.Add("JOB_STATUS", typeof(string));
                }
                ConsentMaster.Columns["JOB_STATUS"].ReadOnly = false;


                //取得時間區間內自費同意書最新簽屬狀態
                DateTime dateEnd = DateTime.TryParse(end, out dateEnd) ? dateEnd : DateTime.Now;
                DateTime dateTimeStart = new DateTime();
                NisQuerySignRequest nisQuerySignRequest = new NisQuerySignRequest
                {
                    //ChartNo = ConsentMaster.Rows[i]["CHARTNO"].ToString(),
                    ChartNo = "9999999999",
                    DocumentNo = "BAK9000-005",
                    StartDate = DateTime.TryParse(start, out dateTimeStart) ? dateTimeStart.AddDays(-1).ToString("yyyy-MM-dd") : dateEnd.AddDays(-7).ToString("yyyy-MM-dd"),
                    EndDate = dateEnd.AddDays(1).ToString("yyyy-MM-dd")
                };
                HttpClient client = new HttpClient();
                NisQuerySignResponse nisQuerySignResponse = new NisQuerySignResponse();
                string url = "http://172.20.110.185:81/api/NisQuerySign";
                var jsonData = JsonConvert.SerializeObject(nisQuerySignRequest);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                try
                {
                    HttpResponseMessage response = client.PostAsync(url, content).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = response.Content.ReadAsStringAsync().Result;
                        nisQuerySignResponse = JsonConvert.DeserializeObject<NisQuerySignResponse>(responseBody);
                    }
                    else
                    {
                        this.log.saveLogMsg($"[ConsentListData][GetSignDocUrl][取得同意書URL API][Error][request:{jsonData}\nresponse:{JsonConvert.SerializeObject(response)}", "SelfPaymentConsent");
                    }
                }
                catch (HttpRequestException e)
                {
                    this.log.saveLogMsg($"[ConsentListData][GetSignDocUrl][取得同意書URL API][Error][request:{jsonData}\nError:{e.Message.ToString()}", "SelfPaymentConsent");
                }
                for (var i = 0; i < ConsentMaster.Rows.Count; i++)
                {
                    //取得最新的自費同意書URL                 
                    url = $"http://172.20.110.185:81/api/GetSignDocUrl?jobid={ConsentMaster.Rows[i]["JOB_ID"]}&amp;readMode=Y";
                    try
                    {
                        HttpResponseMessage response = client.GetAsync(url).Result;

                        if (response.IsSuccessStatusCode)
                        {
                            string responseBody = response.Content.ReadAsStringAsync().Result;
                            ConsentMaster.Rows[i]["CONSENT_URL"] = responseBody;
                        }
                        else
                        {
                            this.log.saveLogMsg($"[ConsentListData][GetSignDocUrl][取得同意書URL API][Error][jobId:{ConsentMaster.Rows[i]["JOB_ID"]}\nresponse:{JsonConvert.SerializeObject(response)}", "SelfPaymentConsent");
                        }
                    }
                    catch (HttpRequestException e)
                    {
                        this.log.saveLogMsg($"[ConsentListData][GetSignDocUrl][取得同意書URL API][Error][jobId:{ConsentMaster.Rows[i]["JOB_ID"]}\nError:{e.Message.ToString()}", "SelfPaymentConsent");
                    }

                    //取得簽屬狀態(已簽屬、未簽屬)
                    if (nisQuerySignResponse != null && nisQuerySignResponse.jobs.Count > 0)
                    {
                        var filterJobs = nisQuerySignResponse.jobs.Where(x => x.jobinfo.JOB_ID == ConsentMaster.Rows[i]["JOB_ID"].ToString()).FirstOrDefault();
                        if (filterJobs != null)
                        {
                            ConsentMaster.Rows[i]["JOB_STATUS"] = filterJobs.jobinfo.JOB_STATUS;
                        }
                    }
                }
                //篩選簽屬狀態(已簽屬、未簽屬)
                if (jobStatus == "COMPLETE")
                {
                    var filterRows = ConsentMaster.AsEnumerable().Where(x => x["JOB_STATUS"].ToString() == "COMPLETE");
                    ConsentMaster = filterRows.Any() ? filterRows.CopyToDataTable() : ConsentMaster.Clone();
                }
                else if (jobStatus == "ONGOING")
                {
                    var filterRows = ConsentMaster.AsEnumerable().Where(x => x["JOB_STATUS"].ToString() != "COMPLETE");
                    ConsentMaster = filterRows.Any() ? filterRows.CopyToDataTable() : ConsentMaster.Clone();
                }
            }

            ViewBag.ConsentMaster = ConsentMaster;
            ViewBag.ConsentDetail = SelfPaymentConsentM.queryConsentDetail(feeno, start, end, jobStatus);
            return View();
        }

        // 建立自費同意書
        public ActionResult CreateConsentForm(string feeno, string start, string end)
        {
            DataTable ComfirmDetail = SelfPaymentConsentM.queryComfirmDetail(feeno, start, end);
            if (ComfirmDetail.Rows.Count > 0)
            {
                var filterData = ComfirmDetail.AsEnumerable().Where(x => x["SELF_PAY_CONSENT"].ToString() == "");
                ComfirmDetail = filterData.Any() ? filterData.CopyToDataTable() : ComfirmDetail.Clone();

                ComfirmDetail.Columns.Add("CREATE_NAME", typeof(string));
                Dictionary<string, string> userNameDict = new Dictionary<string, string>();
                foreach (DataRow row in ComfirmDetail.Rows)
                {
                    var userId = row["CREATE_ID"].ToString().Trim();
                    string userName = "";
                    if (userNameDict.TryGetValue(userId, out userName))
                    {
                        row["CREATE_NAME"] = userName;
                    }
                    else
                    {
                        byte[] listByteCode = webService.UserName(userId);
                        if (listByteCode != null)
                        {
                            string listJsonArray = CompressTool.DecompressString(listByteCode);
                            UserInfo user_name = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                            row["CREATE_NAME"] = user_name.EmployeesName.ToString().Trim();
                            userNameDict.Add(userId, user_name.EmployeesName.ToString().Trim());
                        }
                    }
                }
            }
            ViewBag.ComfirmDetail = ComfirmDetail;
            return View();
        }

        /// <summary>
        /// 產生自費同意書
        /// </summary>
        /// <param name="billingItems">自費項目列表</param>
        public ActionResult GenerateConsent(BillingItem[] billingItems)
        {
            RESPONSE_MSG result = new RESPONSE_MSG();
            try
            {
                // 檢查自費項目是否以產生自費同意書
                // 計價確認表中自費項目已產生自費同意書的項目
                DataTable ComfirmDetail = SelfPaymentConsentM.queryComfirmDetail(ptinfo.FeeNo, "", "", "Y");
                if (ComfirmDetail.Rows.Count > 0)
                {
                    int existCount = 0;
                    string returnMessage = "";
                    foreach (var item in billingItems)
                    {
                        var isExist = ComfirmDetail.AsEnumerable().FirstOrDefault(x => x["SERIAL_D"].ToString() == item.serial_d);
                        if (isExist != null)
                        {
                            existCount += 1;
                            returnMessage += $"{existCount}. 名稱:{item.itemName} 院內碼:{item.ho_id}\n";
                        }
                    }
                    if (existCount > 0)
                    {
                        result.status = RESPONSE_STATUS.ERROR;
                        result.message = returnMessage;
                        return Content(JsonConvert.SerializeObject(result), "application/json");
                    }
                }

                // 呼叫恩主公醫院建立自費同意書API
                // 建立request
                NisCreateSign request = new NisCreateSign()
                {
                    //CHART_NO = ptinfo.ChartNo.Trim(),
                    //FEENO_CLINIC_ID= ptinfo.FeeNo.Trim(),
                    //ID_NO = ptinfo.PatientID.Trim(),
                    //PT_NAME = ptinfo.PatientName.Trim(),
                    //因自費同意書API無測試站台,使用測試病歷號
                    CHART_NO = "9999999999",
                    FEENO_CLINIC_ID = "1140306001",
                    ID_NO = "A123456789",
                    PT_NAME = "測試",

                    DOCTOR_NO = ptinfo.DocNo.Trim(),
                    DOCTOR_DIV_NO = "",
                    OPD_DAT_MRLOC_SHIFT = ptinfo.CostCenterCode?.Trim(), //公司測試環境無資料
                    ORIGIN_TYPE = "I",
                    DOC_TYPE = "BAK9000-005",
                    CREATE_CLERKID = userinfo.EmployeesNo.Trim(),
                };
                foreach (var item in billingItems)
                {
                    int intNum = 0;
                    request.OptionalPriceItems.Add(new OptionalPriceItem()
                    {
                        CODE = item.ho_id.Trim(),
                        USE_AMOUNT = int.TryParse(item.count.Trim(), out intNum) ? intNum : 0,
                        ORIGIN_TYPE = "MAST_PRICE"
                    });
                }

                NisCreateSignResponse responseResult = new NisCreateSignResponse();
                if (mode == "Maya")
                {
                    responseResult.isSuccess = true;
                    responseResult.model = new NisCreateSignResponseModel()
                    {
                        docurl = "http://10.168.30.23/NIS_ECK",
                        jobid = "job_" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                        docid = "doc_" + DateTime.Now.ToString("yyyyMMddHHmmss")
                    };
                }
                else
                {
                    HttpClient client = new HttpClient();
                    string url = "http://172.20.110.185:81/api/NisCreateSign";
                    var jsonData = JsonConvert.SerializeObject(request);
                    var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                    try
                    {
                        HttpResponseMessage response = client.PostAsync(url, content).Result;

                        if (response.IsSuccessStatusCode)
                        {
                            string responseBody = response.Content.ReadAsStringAsync().Result;
                            responseResult = JsonConvert.DeserializeObject<NisCreateSignResponse>(responseBody);
                        }
                        else
                        {
                            this.log.saveLogMsg($"[GenerateConsent][Error][同意書建立API][連線][feeNo:{ptinfo.FeeNo.Trim()}]\nrequest:{JsonConvert.SerializeObject(request)}\nresponse:{JsonConvert.SerializeObject(response)}", "SelfPaymentConsent");
                            result.status = RESPONSE_STATUS.ERROR;
                            result.message = "同意書系統連線失敗";
                            return Content(JsonConvert.SerializeObject(result), "application/json");
                        }
                    }
                    catch (HttpRequestException e)
                    {
                        this.log.saveLogMsg($"[GenerateConsent][Error][同意書建立API][連線][feeNo:{ptinfo.FeeNo.Trim()}]\nrequest:{JsonConvert.SerializeObject(request)}\nerrorMessage:{e.Message}", "SelfPaymentConsent");
                        result.status = RESPONSE_STATUS.ERROR;
                        result.message = "同意書系統連線失敗";
                        return Content(JsonConvert.SerializeObject(result), "application/json");
                    }
                }

                if (responseResult.model == null || string.IsNullOrEmpty(responseResult.model.jobid))
                {
                    this.log.saveLogMsg($"[GenerateConsent][同意書建立API][Error][建立][feeNo:{ptinfo.FeeNo.Trim()}]\nrequest:{JsonConvert.SerializeObject(request)}\nresponse:{JsonConvert.SerializeObject(responseResult)}", "SelfPaymentConsent");
                    result.status = RESPONSE_STATUS.ERROR;
                    result.message = "同意書建立失敗";
                    return Content(JsonConvert.SerializeObject(result), "application/json");
                }

                var erow = 0;
                string consentId = base.creatid("SELFPAYCONSENT_MASTER", userinfo.EmployeesNo, ptinfo.FeeNo, "0");
                List<DBItem> insertDataList = new List<DBItem>();
                // 新增主檔
                insertDataList.Add(new DBItem("CONSENT_ID", consentId, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("GENERATED_TIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                //insertDataList.Add(new DBItem("JOB_STATUS", "ONGOING", DBItem.DBDataType.String));
                //insertDataList.Add(new DBItem("CONSENT_URL", responseResult.model.docurl, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EDIT_USER_ID", userinfo.EmployeesNo.Trim(), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EDIT_USER_NAME", userinfo.EmployeesName.Trim(), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATE_TIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("CREATE_ID", userinfo.EmployeesNo.Trim(), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATE_NAME", userinfo.EmployeesName.Trim(), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CHARTNO", ptinfo.ChartNo.Trim(), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FEENO", ptinfo.FeeNo.Trim(), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("JOB_ID", responseResult.model.jobid.Trim(), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("DOC_ID", responseResult.model.docid.Trim(), DBItem.DBDataType.String));
                erow = link.DBExecInsert("SELFPAYCONSENT_MASTER", insertDataList);
                if (erow == 0)
                {
                    this.log.saveLogMsg($"[GenerateConsent][SELFPAYCONSENT_MASTER][Error][feeNo:{ptinfo.FeeNo.Trim()}]\ninsertDataList:{JsonConvert.SerializeObject(insertDataList)}", "SelfPaymentConsent");
                    result.status = RESPONSE_STATUS.ERROR;
                    result.message = "同意書建立失敗";
                    return Content(JsonConvert.SerializeObject(result), "application/json");
                }

                // 新增明細
                erow = 0;
                int billingItemsIndex = 0;
                foreach (var item in billingItems)
                {
                    try
                    {
                        insertDataList.Clear();
                        insertDataList.Add(new DBItem("DETAIL_ID", base.creatid("SELFPAYCONSENT_DETAIL", userinfo.EmployeesNo, ptinfo.FeeNo, billingItemsIndex.ToString()), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CONSENT_ID", consentId, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("ITEM_SERIAL_D", item.serial_d, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("HO_ID", item.ho_id, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("ITEM_NAME", item.itemName, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("ITEM_PRICE", item.itemPrice, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("COUNT", item.count, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("SELF_PRICE", item.selfPrice, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("RECORD_TIME", item.recordTime, DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("ITEM_CREATE_ID", item.itemCreateId, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("ITEM_CREATE_NAME", item.itemCreateName, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CREATE_TIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("CREATE_ID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CREATE_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                        erow = link.DBExecInsert("SELFPAYCONSENT_DETAIL", insertDataList);
                        // 新增明細成功後壓註記DATA_BILLING_CONFIRM_DETAIL
                        if (erow == 1)
                        {
                            insertDataList.Clear();
                            insertDataList.Add(new DBItem("SELF_PAY_CONSENT", "Y", DBItem.DBDataType.String));
                            erow = link.DBExecUpdate("DATA_BILLING_CONFIRM_DETAIL", insertDataList, "SERIAL_D = '" + item.serial_d + "' AND STATUS = 'Y'");
                            insertDataList.Add(new DBItem("SELF_PAY_CONSENT_ID", consentId, DBItem.DBDataType.String));
                            //insertDataList.Add(new DBItem("SELF_PAY_CONSENT_LINK", responseResult.model.docurl, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("SELF_PAY_CONSENT_JOB_ID", responseResult.model.jobid.Trim(), DBItem.DBDataType.String));
                            erow = link.DBExecUpdate("DATA_BILLING_CONFIRM_DETAIL", insertDataList, "SERIAL_D = '" + item.serial_d + "' AND STATUS = 'Y'");
                            if (erow == 0)
                            {
                                this.log.saveLogMsg($"[GenerateConsent][DATA_BILLING_CONFIRM_DETAIL][Error][feeNo:{ptinfo.FeeNo.Trim()}] SERIAL_D:{item.serial_d}\ninsertDataList:{JsonConvert.SerializeObject(insertDataList)}", "SelfPaymentConsent");
                            }
                        }
                        else
                        {
                            this.log.saveLogMsg($"[GenerateConsent][SELFPAYCONSENT_DETAIL][Error][feeNo:{ptinfo.FeeNo.Trim()}]\ninsertDataList:{JsonConvert.SerializeObject(insertDataList)}", "SelfPaymentConsent");
                        }
                    }
                    catch (Exception ex)
                    {
                        this.log.saveLogMsg($"[GenerateConsent][SELFPAYCONSENT_DETAIL][Error][feeNo:{ptinfo.FeeNo.Trim()}]\ninsertDataList:{JsonConvert.SerializeObject(insertDataList)}\nerrorMessage:{ex.Message.ToString()}", "SelfPaymentConsent");
                    }
                    billingItemsIndex++;
                }

            }
            catch (Exception ex)
            {
                this.log.saveLogMsg($"[GenerateConsent][Error][feeNo:{ptinfo.FeeNo.Trim()}] errorMessage:{ex.Message.ToString()}", "SelfPaymentConsent");
                result.status = RESPONSE_STATUS.ERROR;
                result.message = "同意書建立失敗";
            }

            result.status = RESPONSE_STATUS.SUCCESS;
            result.message = "同意書建立成功";
            return Content(JsonConvert.SerializeObject(result), "application/json");
        }

        // 編輯自費同意書註記畫面
        public ActionResult ConsentNoteForm(string consentId)
        {
            ViewBag.ConsentId = consentId;
            ViewBag.ConsentMaster = SelfPaymentConsentM.queryConsentMaster(ptinfo.FeeNo.Trim(), "", "", "", consentId);
            ViewBag.ConsentDetail = SelfPaymentConsentM.queryConsentDetail(ptinfo.FeeNo.Trim(), "", "", "", consentId);
            return View();
        }

        // 編輯自費同意書註記畫面
        public ActionResult SaveNote(SaveNote data)
        {
            RESPONSE_MSG result = new RESPONSE_MSG();
            try
            {
                int erow = 0;
                DataTable currentMaster = SelfPaymentConsentM.queryConsentMaster(ptinfo.FeeNo.Trim(), "", "", "", data.consentId);
                if (currentMaster.Rows.Count > 0)
                {

                    //註記舊紀錄
                    List<DBItem> insertDataList = new List<DBItem>();
                    insertDataList.Add(new DBItem("MODIFY_TIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("MODIFY_ID", userinfo.EmployeesNo.Trim(), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("MODIFY_NAME", userinfo.EmployeesName.Trim(), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("STATUS", "M", DBItem.DBDataType.String));
                    erow = link.DBExecUpdate("SELFPAYCONSENT_MASTER", insertDataList, "CONSENT_ID = '" + data.consentId + "' AND STATUS = 'Y'");
                    if (erow == 1)
                    {
                        insertDataList.Clear();
                        insertDataList.Add(new DBItem("CONSENT_ID", currentMaster.Rows[0]["CONSENT_ID"].ToString(), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("GENERATED_TIME", DateTime.Parse(currentMaster.Rows[0]["GENERATED_TIME"].ToString()).ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                        //insertDataList.Add(new DBItem("JOB_STATUS", currentMaster.Rows[0]["JOB_STATUS"].ToString(), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("NOTE", data.note, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("NOTE_USER_ID", userinfo.EmployeesNo.Trim(), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("NOTE_USER_NAME", userinfo.EmployeesName.Trim(), DBItem.DBDataType.String));
                        //insertDataList.Add(new DBItem("CONSENT_URL", currentMaster.Rows[0]["CONSENT_URL"].ToString(), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("EDIT_USER_ID", currentMaster.Rows[0]["EDIT_USER_ID"].ToString(), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("EDIT_USER_NAME", currentMaster.Rows[0]["EDIT_USER_NAME"].ToString(), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CREATE_TIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("CREATE_ID", userinfo.EmployeesNo.Trim(), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CREATE_NAME", userinfo.EmployeesName.Trim(), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CHARTNO", ptinfo.ChartNo.Trim(), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("FEENO", ptinfo.FeeNo.Trim(), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("JOB_ID", currentMaster.Rows[0]["JOB_ID"].ToString(), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("DOC_ID", currentMaster.Rows[0]["DOC_ID"].ToString(), DBItem.DBDataType.String));
                        erow = link.DBExecInsert("SELFPAYCONSENT_MASTER", insertDataList);
                        if (erow == 0)
                        {
                            this.log.saveLogMsg($"[SaveNote][Error][主表新增][feeNo:{ptinfo.FeeNo.Trim()}] data:{JsonConvert.SerializeObject(data)}", "SelfPaymentConsent");
                            result.status = RESPONSE_STATUS.ERROR;
                            result.message = "儲存失敗";
                            return Content(JsonConvert.SerializeObject(result), "application/json");
                        }
                    }
                    else
                    {
                        this.log.saveLogMsg($"[SaveNote][Error][主表註記舊資料][feeNo:{ptinfo.FeeNo.Trim()}] data:{JsonConvert.SerializeObject(data)}", "SelfPaymentConsent");
                        result.status = RESPONSE_STATUS.ERROR;
                        result.message = "儲存失敗";
                        return Content(JsonConvert.SerializeObject(result), "application/json");
                    }
                }
            }
            catch (Exception ex)
            {
                this.log.saveLogMsg($"[SaveNote][Error][feeNo:{ptinfo.FeeNo.Trim()}] data:{JsonConvert.SerializeObject(data)} errorMessage:{ex.Message.ToString()}", "SelfPaymentConsent");
                result.status = RESPONSE_STATUS.ERROR;
                result.message = "儲存失敗";
                return Content(JsonConvert.SerializeObject(result), "application/json");
            }

            result.status = RESPONSE_STATUS.SUCCESS;
            result.message = "儲存成功";
            return Content(JsonConvert.SerializeObject(result), "application/json");
        }
    }
}