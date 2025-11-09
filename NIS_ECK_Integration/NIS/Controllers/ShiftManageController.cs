using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NIS.Models;
using System.Data.OleDb;
using Newtonsoft.Json;
using NIS.Data;
using NIS.UtilTool;
using NIS.WebService;
using Com.Mayaminer;

namespace NIS.Controllers
{
    public class ShiftManageController : BaseController
    {
        private DBConnector link;
        private TransferDutyController tdc;
        private MainController mc;
        private LogTool log;


        public ShiftManageController()
        {
            this.link = new DBConnector();
            this.tdc = new TransferDutyController();
            this.mc = new MainController();
            this.log = new LogTool();

        }

        //031交班單
        public ActionResult Kardex()
        {
            List<GetExpectedItem> ExpectedItemList = new List<GetExpectedItem>();
            List<GetExpectedItem> ExpectedItemList_lab = new List<GetExpectedItem>();
            List<GetExpectedItem> lab_group = new List<GetExpectedItem>();
            string temp = "";
            string[] itemName = null;
            byte[] TempByte = null;

            try
            {
                //交班單預計項目數值
                TempByte = this.webService.GetExpectedItem(base.ptinfo.FeeNo);
                if (TempByte != null)
                {
                    ExpectedItemList = JsonConvert.DeserializeObject<List<GetExpectedItem>>(CompressTool.DecompressString(TempByte));
                    ExpectedItemList_lab = ExpectedItemList.FindAll(x => x.ItemType == "lab");
                    lab_group = ExpectedItemList_lab.GroupBy(x => new { x.LabNo, x.UseDate, x.ItemType, x.ComplyDate }).Select(y => new GetExpectedItem()
                    {
                        UseDate = y.Key.UseDate,
                        ItemType = y.Key.ItemType,
                        ComplyDate = y.Key.ComplyDate,
                        LabNo = y.Key.LabNo
                    }).ToList();
                }
                foreach (var item in lab_group)
                {
                    System.Text.StringBuilder sBuilder = new System.Text.StringBuilder();
                    foreach (var labs in ExpectedItemList_lab.FindAll(x => x.LabNo == item.LabNo))
                    {
                        sBuilder.Append(labs.ItemName + "|");
                    }
                    itemName = sBuilder.ToString().Split('|');
                    temp = string.Join(",", itemName);
                    item.ItemName = temp;
                }
            }
            catch (Exception)
            {
                //Do nothing
            }
            ViewData["ExpectedItemList"] = ExpectedItemList;
            ViewData["lab_group"] = lab_group;

            List<Dictionary<string, string>> BloodInfoList = new List<Dictionary<string, string>>();
            try
            {
                //輸血
                TempByte = this.webService.GetBloodInfo(base.ptinfo.FeeNo);
                if (TempByte != null)
                {
                    BloodInfoList = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(CompressTool.DecompressString(TempByte));
                }
            }
            catch (Exception)
            {
                //Do nothing
            }

            ViewData["BloodInfoList"] = BloodInfoList;
            ViewBag.shiftcate = Request["shiftcate"].ToString().Trim();
            ViewBag.type = Request["type"].ToString().Trim();
            ViewBag.paramsdata = Request["paramsdata"].ToString().Trim();
            ViewBag.RootDocument = GetSourceUrl();
            ViewBag.Assessment = (base.ptinfo != null? base.ptinfo.Assessment: null);
            ViewBag.feeno = (base.ptinfo != null? base.ptinfo.FeeNo.Trim(): null);
            ViewBag.BedNo = (base.ptinfo != null? base.ptinfo.BedNo.Trim(): null);
            return View();
        }

        public ActionResult Kardex_Detail_PDF(string feeno)
        {
            byte[] TempByte = webService.GetExpectedItem(feeno);
            PatientInfo pi = null;
            List<GetExpectedItem> ExpectedItemList = new List<GetExpectedItem>();
            List<GetExpectedItem> ExpectedItemList_lab = new List<GetExpectedItem>();
            List<GetExpectedItem> lab_group = new List<GetExpectedItem>();
            string temp = "";
            string[] itemName = null;

            if (TempByte != null)
            {
                ExpectedItemList = JsonConvert.DeserializeObject<List<GetExpectedItem>>(CompressTool.DecompressString(TempByte));
                ExpectedItemList_lab = ExpectedItemList.FindAll(x => x.ItemType == "lab");
                lab_group = ExpectedItemList_lab.GroupBy(x => new { x.LabNo, x.UseDate, x.ItemType, x.ComplyDate }).Select(y => new GetExpectedItem()
                {
                    UseDate = y.Key.UseDate,
                    ItemType = y.Key.ItemType,
                    ComplyDate = y.Key.ComplyDate,
                    LabNo = y.Key.LabNo
                }).ToList();
            }
            foreach (var item in lab_group)
            {
                System.Text.StringBuilder sBuilder = new System.Text.StringBuilder();
                foreach (var labs in ExpectedItemList_lab.FindAll(x => x.LabNo == item.LabNo))
                {
                    sBuilder.Append(labs.ItemName + "|");
                }
                itemName = sBuilder.ToString().Split('|');
                temp = string.Join(",", itemName);
                item.ItemName = temp;
            }
            ViewData["ExpectedItemList"] = ExpectedItemList;
            ViewData["lab_group"] = lab_group;
            //輸血
            TempByte = webService.GetBloodInfo(feeno);
            List<Dictionary<string, string>> BloodInfoList = new List<Dictionary<string, string>>();
            if (TempByte != null)
            {
                BloodInfoList = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(CompressTool.DecompressString(TempByte));
            }
            if (feeno != null && feeno != "")
            {//取病人資料
                byte[] ptinfoByteCode = webService.GetPatientInfo(feeno.Trim());
                if (ptinfoByteCode != null)
                {
                    string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                    pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                }
            }
            ViewData["ExpectedItemList"] = ExpectedItemList;
            ViewData["BloodInfoList"] = BloodInfoList;
            ViewBag.shiftcate = Request["shiftcate"].ToString().Trim();
            ViewBag.RootDocument = GetSourceUrl();
            ViewBag.Assessment = pi.Assessment.Trim();
            ViewBag.feeno = feeno;
            ViewBag.BedNo = pi.BedNo.Trim();
            ViewData["pi"] = pi;
            return View();
        }

        //取資料
        public string SelcetPtShit()
        {
            string feeno = (base.ptinfo != null) ? base.ptinfo.FeeNo : Request["feeno"];
            List<Dictionary<string, string>> Temp = new List<Dictionary<string, string>>();

            try
            {
                string sql = "SELECT RECORD_JSON_DATA FROM NIS_DATA_PT_SHIFT "
                + "WHERE FEE_NO = '" + feeno + "' AND RECORD_DATE = ( "
                + "SELECT MAX(RECORD_DATE) FROM NIS_DATA_PT_SHIFT WHERE FEE_NO = '" + feeno + "' ) ";

                DataTable Dt = this.link.DBExecSQL(sql);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        Temp = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(Dt.Rows[i]["RECORD_JSON_DATA"].ToString());
                    }
                }

                return JsonConvert.SerializeObject(Temp);
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(),ex);
                return JsonConvert.SerializeObject(Temp);
            }
        }

        [HttpPost]
        public string GetHisVIewUrl(string Url, string fName, string type = "")
        {
            Url = Url.Substring(0, Url.LastIndexOf("/"));
            Url = Url.Substring(0, Url.LastIndexOf("/"));
            return Url + "/HisView/" + fName + "?feeno=" + base.ptinfo.FeeNo + "&type=" + type;
        }

        //儲存
        public string SaveKardex(string Data, string shiftcate, string bedno, string successorNo)
        {
            bool Success = false;
            DateTime now = DateTime.Now;

            //確認是否有病人資料
            if (Session["PatInfo"] != null)
            {
                //儲存Kardex相關的Data
                try
                {
                    List<DBItem> dbItem = new List<DBItem>();
                    dbItem.Add(new DBItem("SHIFT_CODE", base.creatid("NIS_DATA_PT_SHIFT", base.userinfo.EmployeesNo, base.ptinfo.FeeNo, "0"), DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("FEE_NO", base.ptinfo.FeeNo, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("RECORD_DATE", now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("RECORD_JSON_DATA", Data, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("RECORD_USERNO", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("RECORD_USERNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    if (this.link.DBExecInsert("NIS_DATA_PT_SHIFT", dbItem) == 1)
                        Success = true;
                }
                catch
                {
                    Success = false;
                }
            }

            if (Success)
            {
                //儲存交班人ID到派班Table            
                int erow = 0;
                string where = "";
                if (!string.IsNullOrEmpty(successorNo))
                {
                    //確認是否有使用者資料
                    if (Session["UserInfo"] != null)
                    {
                        try
                        {
                            string date = "";
                            if (Int32.Parse(now.ToString("HH")) < 3)
                            {
                                date = now.AddDays(-1).ToString("yyyy/MM/dd");
                            }
                            else
                            {
                                date = now.ToString("yyyy/MM/dd");
                            }
                            where = "RESPONSIBLE_USER = '" + userinfo.EmployeesNo + "' AND TO_CHAR(SHIFT_DATE, 'yyyy/MM/dd') = '" + date + "' AND BED_NO='" + bedno + "' AND SHIFT_CATE='" + shiftcate + "'";
                            List<DBItem> dbItem = new List<DBItem>();
                            dbItem.Add(new DBItem("MODIFY_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                            dbItem.Add(new DBItem("MODIFY_DATE", now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                            dbItem.Add(new DBItem("SUCCESSOR", successorNo, DBItem.DBDataType.String));
                            erow = this.link.DBExecUpdate("DATA_DISPATCHING", dbItem, where);
                        }
                        catch (Exception ex)
                        {
                            log.saveLogMsg(ex.Message.ToString() + "，SUCCESSOR：" + successorNo + "，BEDNO：" + bedno + "，SHIFTCATE：" + shiftcate + "，where：" + where, "SaveSucceNoLog");
                        }
                    }
                }
                if (erow == 0)
                {
                    log.saveLogMsg("SUCCESSOR：" + successorNo + "，BEDNO：" + bedno + "，SHIFTCATE：" + shiftcate + "，where：" + where, "SaveSucceNoLog");
                }

                //列印交班表頭
                string baseURL = "";
                string printURL = "";
                string queryString = "";
                string fullURL = "";

                //網站的基礎URL
                baseURL = GetSourceUrl();

                //列印頁的URL
                printURL = baseURL + "/Print/GetPDF";

                //列印交班表頭
                queryString = "url=" + baseURL + "/ShiftManage/TransferDuty_PDF?";
                queryString += "shift=" + shiftcate;
                queryString += "&bedno=" + bedno;
                queryString += "&userNo=" + userinfo.EmployeesNo.Trim();
                queryString += "&SuccessorNo=" + successorNo;
                queryString += "&filename=" + this.ptinfo.FeeNo + "-" + now.ToString("yyyyMMddHHmmss");
                queryString += "&DelFile=false";

                fullURL = printURL + "?" + queryString + "";
                //出表使用非同步Request 避免User等待太久         
                ActionResult printMasterResult = pseudoHttpRequestUtil.generatePseudoRequest(fullURL, "GetPDF",
                    (PrintController pctlr) =>
                    {
                        ActionResult result = pctlr.GetPDF();
                        return result;
                    }, true);

                //列印交班明細
                queryString = "url=" + baseURL + "/ShiftManage/Kardex_Detail_PDF?";
                queryString += "feeno=" + this.ptinfo.FeeNo;
                queryString += "&shiftcate=" + shiftcate;
                queryString += "&filename=" + this.ptinfo.FeeNo + "-" + now.ToString("yyyyMMddHHmmss") + "_detail";
                queryString += "&DelFile=false";

                fullURL = printURL + "?" + queryString + "";
                //出表使用非同步Request 避免User等待太久
                ActionResult printDetailResult = pseudoHttpRequestUtil.generatePseudoRequest(fullURL, "GetPDF",
                    (PrintController pctlr) =>
                    {
                        ActionResult result = pctlr.GetPDF();
                        return result;
                    }, false);

                //EMR
                int signResult = base.SignPush(this.ptinfo.FeeNo);
                //若成功則回傳Flag Y
                return "Y";
            }

            //失敗則回傳Flag N
            return "N";
        }

        #region 撈自動帶資料

        private string error_str(string value, string message, ref List<string>alert_list,string aa_type = ""){
            if (string.IsNullOrEmpty(value))
            {
                value =  "<font color=\"red\">資料異常</font>";
                alert_list.Add(message);
            }else
            {
                switch (aa_type.ToUpper())
                {
                    case "GCS":
                        value=value.Substring(1,1);
                        break;
                    default:
                        break;
                }
            }
            return value;
            }
       
        [HttpPost]
        public string SelectDt()//交班單  交班單列印都會用到此方法
        {
            try
            {
                if (ptinfo == null)
                {
                    string feeno = Request["feeno"].ToString();
                    if (feeno != null && feeno != "")
                    {//取病人資料
                        byte[] ptinfoByteCode = webService.GetPatientInfo(feeno.Trim());
                        if (ptinfoByteCode != null)
                        {
                            string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                            ptinfo = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                        }
                    }
                }
                int age = (base.ptinfo != null) ? base.ptinfo.Age : Int32.Parse(Request["age"].ToString());
                string type =  base.get_check_type(ptinfo); //取得生命徵象異常年紀代號
                var shiftcate = Request["SHIFTCATE"].ToString();
                List<string> alertstr = new List<string>();
                if (Session["PatInfo"] != null || Request["feeno"].ToString() != null)//feeno的這個回傳參數，是為了，給列印明細頁面讀取時用的
                {
                    List<Dictionary<string, string>> Temp = new List<Dictionary<string, string>>();
                    Dictionary<string, string> temp = null, Dt = new Dictionary<string, string>();
                    string sql = string.Empty;
                    string Temp_String = string.Empty;
                    string[] ListWord = null;
                    string feeno = (base.ptinfo != null) ? base.ptinfo.FeeNo : Request["feeno"].ToString();
                    string Blood_Type = (base.ptinfo != null) ? base.ptinfo.Blood_Type : Request["Blood_Type"].ToString();
                    string Consultation = (base.ptinfo != null) ? base.ptinfo.Consultation : Request["Consultation"].ToString();// Dt["Condition"]看起來這格是要取Condition才對，先暫時取這個

                    #region 入院護理評估_成人
                    sql = "SELECT * FROM (SELECT TABLEID, NATYPE FROM ASSESSMENTMASTER "
                    + "WHERE FEENO = '" + feeno + "' AND NATYPE = 'A' AND DELETED IS NULL "
                    + "AND STATUS NOT IN ('TEMPORARY','DELETE') "
                    + "ORDER BY CREATETIME DESC, MODIFYTIME DESC) WHERE ROWNUM <= 1 ";
                    string TableId = string.Empty, NaType = string.Empty;

                    DataTable Dt2 = link.DBExecSQL(sql); //上面已有Dt變數
                    if (Dt2.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt2.Rows.Count; i++)

                        {
                            TableId = Dt2.Rows[i]["TABLEID"].ToString();
                            NaType = Dt2.Rows[i]["NATYPE"].ToString();

                        }
                    }


                    if (TableId != string.Empty && NaType != string.Empty)
                    {
                        Temp = new List<Dictionary<string, string>>();
                        //取得所有入評值
                        sql = "SELECT * FROM ASSESSMENTDETAIL WHERE TABLEID = '" + TableId + "' ";

                        Dt2 = link.DBExecSQL(sql);
                        if (Dt2.Rows.Count > 0)
                        {
                            for (int i = 0; i < Dt2.Rows.Count; i++)

                            {
                                temp = new Dictionary<string, string>();
                                temp["Name"] = Dt2.Rows[i]["ITEMID"].ToString();
                                temp["Value"] = Dt2.Rows[i]["ITEMVALUE"].ToString();
                                Temp.Add(temp);
                        }
                    }

                    if (Temp.Count > 0)
                    {
                        //血型
                        Dt["A_pt_blood"] = Blood_Type;
                        //過敏史食物
                        if (Temp.Exists(x => x["Name"] == "param_allergy_food_other"))
                        {
                            ListWord = Temp.Find(x => x["Name"] == "param_allergy_food_other")["Value"].Split(',');
                            if (ListWord.Length > 0)
                            {
                                Temp_String = string.Empty;
                                foreach (string word in ListWord)
                                {
                                    switch (word)
                                    {
                                        case "海鮮類":
                                            if (Temp.Exists(x => x["Name"] == "param_allergy_food_other_2_name"))
                                                Temp_String += word + "(" + Temp.Find(x => x["Name"] == "param_allergy_food_other_2_name")["Value"] + ")、";
                                            break;
                                        case "水果":
                                            if (Temp.Exists(x => x["Name"] == "param_allergy_food_other_4_name"))
                                                Temp_String += word + "(" + Temp.Find(x => x["Name"] == "param_allergy_food_other_4_name")["Value"] + ")、";
                                            break;
                                        case "其他":
                                            if (Temp.Exists(x => x["Name"] == "param_allergy_food_other_6_name"))
                                                Temp_String += Temp.Find(x => x["Name"] == "param_allergy_food_other_6_name")["Value"] + "、";
                                            break;
                                        default:
                                            Temp_String += word + "、";
                                            break;
                                    }
                                }
                                if (Temp_String.Length > 1)
                                    Dt["A_param_allergy_food_other"] = Temp_String.Substring(0, Temp_String.Length - 1);
                            }
                        }
                        //過敏史其他
                        if (Temp.Exists(x => x["Name"] == "param_allergy_other_other"))
                        {
                            ListWord = Temp.Find(x => x["Name"] == "param_allergy_other_other")["Value"].Split(',');
                            if (ListWord.Length > 0)
                            {
                                Temp_String = string.Empty;
                                foreach (string word in ListWord)
                                    Temp_String += word + "、";
                                    if (Temp_String.Length > 1)
                                        Temp_String = Temp_String.Substring(0, Temp_String.Length - 1);
                                if (Temp.Exists(x => x["Name"] == "param_allergy_other_other_6_name"))
                                    {
                                        if (Temp_String.Length > 2)
                                            Temp_String = Temp_String.Substring(0, Temp_String.Length - 2) + Temp.Find(x => x["Name"] == "param_allergy_other_other_6_name")["Value"];
                                    }
                                Dt["A_param_allergy_other_other"] = Temp_String;
                            }
                        }
                        //輸血過敏反應
                        if (Temp.Exists(x => x["Name"] == "transfusion_blood_dtl_txt") && Temp.Exists(x => x["Name"] == "transfusion_blood_dtl_txt"))
                            Dt["A_transfusion_blood_dtl_txt"] = Temp.Find(x => x["Name"] == "transfusion_blood_dtl_txt")["Value"];
                        //疾病史
                        if (Temp.Exists(x => x["Name"] == "param_im_history_item_other") && Temp.Exists(x => x["Name"] == "param_im_history_item_other"))
                        {
                            Temp_String = Temp.Find(x => x["Name"] == "param_im_history_item_other")["Value"];
                            if (Temp.Exists(x => x["Name"] == "param_im_history_item_other_txt"))
                                {
                                    if (Temp_String.Length >= 2)
                                        Temp_String = Temp_String.Substring(0, Temp_String.Length - 2) + Temp.Find(x => x["Name"] == "param_im_history_item_other_txt")["Value"];
                                }
                                Dt["A_param_im_history_item_other"] = Temp_String;
                        }
                        //住院史
                        if (Temp.Exists(x => x["Name"] == "param_ipd_past_reason"))
                        {
                            Temp_String = Temp.Find(x => x["Name"] == "param_ipd_past_reason")["Value"];
                            Dt["A_param_ipd_past_reason"] =  Temp_String ;
                        }
                        if (Temp.Exists(x => x["Name"] == "param_ipd_past_location"))
                        {
                            Temp_String = Temp.Find(x => x["Name"] == "param_ipd_past_location")["Value"];
                            Dt["A_param_ipd_past_location"] =  Temp_String;
                        }                        
                        //開刀史                        
                            if (Temp.Exists(x => x["Name"] == "param_ipd_surgery_reason"))
                            {
                                Temp_String = Temp.Find(x => x["Name"] == "param_ipd_surgery_reason")["Value"];
                                Dt["A_param_ipd_surgery_reason"] = Temp_String ;
                            }
                            if (Temp.Exists(x => x["Name"] == "param_ipd_surgery_location"))
                            {
                                Temp_String = Temp.Find(x => x["Name"] == "param_ipd_surgery_location")["Value"];
                                Dt["A_param_ipd_surgery_location"] = Temp_String;
                            }                        
                        //入院/轉入日期

                        if (Temp.Exists(x => x["Name"] == "param_tube_date"))
                            Dt["A_param_ipd_source"] = Temp.Find(x => x["Name"] == "param_tube_date")["Value"] + " ";

                        if (Dt.Keys.Contains("A_param_ipd_source"))
                        {
                            if (Temp.Exists(x => x["Name"] == "param_tube_time"))
                                Dt["A_param_ipd_source"] += Temp.Find(x => x["Name"] == "param_tube_time")["Value"];
                            if (Temp.Exists(x => x["Name"] == "param_ipd_source"))
                                Dt["A_param_ipd_source"] += Temp.Find(x => x["Name"] == "param_ipd_source")["Value"];
                            //if (Temp.Exists(x => x["Name"] == "param_assessment_other"))
                            //    Dt["A_param_ipd_source"] += Temp.Find(x => x["Name"] == "param_assessment_other")["Value"] + "入院";
                            //else
                            //    Dt["A_param_ipd_source"] += Temp.Find(x => x["Name"] == "param_assessment")["Value"] + "入院";
                            if (Temp.Exists(x => x["Name"] == "param_assessment"))
                            {
                                Dt["A_param_ipd_source"] += Temp.Find(x => x["Name"] == "param_assessment")["Value"];
                                if (Temp.Find(x => x["Name"] == "param_assessment")["Value"] == "轉入")
                                {
                                    if (Temp.Exists(x => x["Name"] == "param_assessment_turn"))
                                    {
                                        Dt["A_param_ipd_source"] += "，來源床號" + Temp.Find(x => x["Name"] == "param_assessment_turn")["Value"];
                                    }
                                }
                                else if (Temp.Find(x => x["Name"] == "param_assessment")["Value"] == "其他")
                                {
                                    if (Temp.Exists(x => x["Name"] == "param_assessment_other"))
                                    {
                                        Dt["A_param_ipd_source"] += ":" + Temp.Find(x => x["Name"] == "param_assessment_other")["Value"];
                                    }
                                }
                            }
                        }

                        //入院方式
                        if (Temp.Exists(x => x["Name"] == "param_ipd_style"))
                            Dt["A_param_ipd_style"] = Temp.Find(x => x["Name"] == "param_ipd_style")["Value"];
                        //入院原因/經過
                        if (Temp.Exists(x => x["Name"] == "param_ipd_reason"))
                            Dt["A_param_ipd_reason"] = Temp.Find(x => x["Name"] == "param_ipd_reason")["Value"];
                    }
                }

                    #endregion

                    #region 入院護理評估_兒童
                    sql = "SELECT * FROM (SELECT TABLEID, NATYPE FROM ASSESSMENTMASTER "
                    + "WHERE FEENO = '" + feeno + "' AND NATYPE = 'C' AND DELETED IS NULL "
                    + "AND STATUS NOT IN ('TEMPORARY','DELETE') "
                    + "ORDER BY CREATETIME DESC, MODIFYTIME DESC) WHERE ROWNUM <= 1 ";
                    TableId = string.Empty; NaType = string.Empty;

                    Dt2 = link.DBExecSQL(sql);
                    if (Dt2.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt2.Rows.Count; i++)

                        {
                            TableId = Dt2.Rows[i]["TABLEID"].ToString();
                            NaType = Dt2.Rows[i]["NATYPE"].ToString();

                        }
                    }
                    
                    if (TableId != string.Empty && NaType != string.Empty)
                    {
                        Temp = new List<Dictionary<string, string>>();
                        //取得所有入評值
                        sql = "SELECT * FROM ASSESSMENTDETAIL WHERE TABLEID = '" + TableId + "' ";
                
                        Dt2 = link.DBExecSQL(sql);
                        if (Dt2.Rows.Count > 0)
                        {
                            for (int i = 0; i < Dt2.Rows.Count; i++)

                            {
                                temp = new Dictionary<string, string>();
                                    temp["Name"] = Dt2.Rows[i]["ITEMID"].ToString();
                                    temp["Value"] = Dt2.Rows[i]["ITEMVALUE"].ToString();
                                    Temp.Add(temp);
                            }
                        }

                    if (Temp.Count > 0)
                    {
                        //血型
                        Dt["C_pt_blood"] = Blood_Type;
                        //過敏史食物
                        if (Temp.Exists(x => x["Name"] == "param_allergy_food_other"))
                        {
                            ListWord = Temp.Find(x => x["Name"] == "param_allergy_food_other")["Value"].Split(',');
                            if (ListWord.Length > 0)
                            {
                                Temp_String = string.Empty;
                                foreach (string word in ListWord)
                                {
                                    switch (word)
                                    {
                                        case "海鮮類":
                                            if (Temp.Exists(x => x["Name"] == "param_allergy_food_other_2_name"))
                                                Temp_String += word + "(" + Temp.Find(x => x["Name"] == "param_allergy_food_other_2_name")["Value"] + ")、";
                                            break;
                                        case "水果":
                                            if (Temp.Exists(x => x["Name"] == "param_allergy_food_other_4_name"))
                                                Temp_String += word + "(" + Temp.Find(x => x["Name"] == "param_allergy_food_other_4_name")["Value"] + ")、";
                                            break;
                                        case "其他":
                                            if (Temp.Exists(x => x["Name"] == "param_allergy_food_other_6_name"))
                                                Temp_String += Temp.Find(x => x["Name"] == "param_allergy_food_other_6_name")["Value"] + "、";
                                            break;
                                        default:
                                            Temp_String += word + "、";
                                            break;
                                    }
                                }
                                if (Temp_String.Length > 1)
                                    Dt["C_param_allergy_food_other"] = Temp_String.Substring(0, Temp_String.Length - 1);

                                }
                        }
                        //過敏史其他
                        if (Temp.Exists(x => x["Name"] == "param_allergy_other_other"))
                        {
                            ListWord = Temp.Find(x => x["Name"] == "param_allergy_other_other")["Value"].Split(',');
                            if (ListWord.Length > 0)
                            {
                                Temp_String = string.Empty;
                                foreach (string word in ListWord)
                                    Temp_String += word + "、";
                                if (Temp_String.Length > 1)
                                        Temp_String = Temp_String.Substring(0, Temp_String.Length - 1);
                                if (Temp.Exists(x => x["Name"] == "param_allergy_other_other_6_name"))
                                    {
                                        if (Temp_String.Length > 2)
                                            Temp_String = Temp_String.Substring(0, Temp_String.Length - 2) + Temp.Find(x => x["Name"] == "param_allergy_other_other_6_name")["Value"];
                                    }
                                    Dt["C_param_allergy_other_other"] = Temp_String;
                            }
                        }
                        //輸血過敏反應
                        if (Temp.Exists(x => x["Name"] == "transfusion_blood_dtl_txt") && Temp.Exists(x => x["Name"] == "transfusion_blood_dtl_txt"))
                            Dt["C_transfusion_blood_dtl_txt"] = Temp.Find(x => x["Name"] == "transfusion_blood_dtl_txt")["Value"];
                        //疾病史
                        if (Temp.Exists(x => x["Name"] == "param_im_history_item_other") && Temp.Exists(x => x["Name"] == "param_im_history_item_other"))
                        {
                            Temp_String = Temp.Find(x => x["Name"] == "param_im_history_item_other")["Value"];
                            if (Temp.Exists(x => x["Name"] == "param_im_history_item_other_txt"))
                                {
                                    if (Temp_String.Length > 2)
                                        Temp_String = Temp_String.Substring(0, Temp_String.Length - 2) + Temp.Find(x => x["Name"] == "param_im_history_item_other_txt")["Value"];
                                }
                                Dt["C_param_im_history_item_other"] = Temp_String;
                        }
                        //住院史
                        if (Temp.Exists(x => x["Name"] == "param_ipd_past_reason"))
                        {
                            Temp_String = Temp.Find(x => x["Name"] == "param_ipd_past_reason")["Value"];
                            Dt["C_param_ipd_past_reason"] = Temp_String;
                        }
                        if (Temp.Exists(x => x["Name"] == "param_ipd_past_location"))
                        {
                            Temp_String = Temp.Find(x => x["Name"] == "param_ipd_past_location")["Value"];
                            Dt["C_param_ipd_past_location"] = Temp_String;
                        }
                        //開刀史                        
                        if (Temp.Exists(x => x["Name"] == "param_ipd_surgery_reason"))
                        {
                            Temp_String = Temp.Find(x => x["Name"] == "param_ipd_surgery_reason")["Value"];
                            Dt["C_param_ipd_surgery_reason"] = Temp_String;
                        }
                        if (Temp.Exists(x => x["Name"] == "param_ipd_surgery_location"))
                        {
                            Temp_String = Temp.Find(x => x["Name"] == "param_ipd_surgery_location")["Value"];
                            Dt["C_param_ipd_surgery_location"] = Temp_String;
                        }
                        //入院/轉入日期
                        if (Temp.Exists(x => x["Name"] == "param_tube_date"))
                            Dt["C_param_ipd_source"] = Temp.Find(x => x["Name"] == "param_tube_date")["Value"] + " ";
                        if (Temp.Exists(x => x["Name"] == "param_tube_time"))
                            Dt["C_param_ipd_source"] += Temp.Find(x => x["Name"] == "param_tube_time")["Value"];
                        //Convert.ToDateTime(base.ptinfo.InDate).ToString("yyyy/MM/dd HH:mm") + 
                        if (Temp.Exists(x => x["Name"] == "param_ipd_source"))
                            Dt["C_param_ipd_source"] += Temp.Find(x => x["Name"] == "param_ipd_source")["Value"] + "入院";
                        //入院方式
                        if (Temp.Exists(x => x["Name"] == "param_ipd_style"))
                            Dt["C_param_ipd_style"] = Temp.Find(x => x["Name"] == "param_ipd_style")["Value"];
                        if (Temp.Exists(x => x["Name"] == "param_ipd_style_other"))
                            Dt["C_param_ipd_style"] = Temp.Find(x => x["Name"] == "param_ipd_style_other")["Value"];
                        //入院原因/經過
                        if (Temp.Exists(x => x["Name"] == "param_ipd_depiction"))
                            Dt["C_param_ipd_reason"] = Temp.Find(x => x["Name"] == "param_ipd_depiction")["Value"];
                    }
                }

                    #endregion

                    #region 每日評估_成人

                    //撈每日評估

                    sql = "SELECT * FROM(SELECT DBAM_ID, DBAM_TYPE FROM DAILY_BODY_ASSESSMENT_MASTER "
                    + "WHERE FEENO = '" + feeno + "' AND DELETED = 'N' AND DBAM_TYPE = 'adult' "
                    + "ORDER BY CREATTIME DESC, UPDTIME DESC "
                    + ") WHERE ROWNUM <= 1 ";
                    TableId = string.Empty; NaType = string.Empty;

                    Dt2 = link.DBExecSQL(sql);
                    if (Dt2.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt2.Rows.Count; i++)
                        {
                            TableId = Dt2.Rows[i]["DBAM_ID"].ToString();
                            NaType = Dt2.Rows[i]["DBAM_TYPE"].ToString();
                        }
                    }

                    if (TableId != string.Empty && NaType != string.Empty)
                    {
                        sql = "SELECT * FROM DAILY_BODY_ASSESSMENT_DETAIL "
                        + "WHERE DBAM_ID = '" + TableId + "' ";
                        Temp = new List<Dictionary<string, string>>();
                        Dt2 = link.DBExecSQL(sql);
                        if (Dt2.Rows.Count > 0)
                        {
                            for (int i = 0; i < Dt2.Rows.Count; i++)
                            {
                                temp = new Dictionary<string, string>();
                                temp["Name"] = Dt2.Rows[i]["DBAD_ITEMID"].ToString();
                                temp["Value"] = Dt2.Rows[i]["DBAD_ITEMVALUE"].ToString();
                                Temp.Add(temp);
                            }
                        }

                        if (Temp.Count > 0)
                        {
                            string RecordTime = "";
                            if (Temp.Exists(x => x["Name"] == "TxtDBAMDate") && Temp.Exists(x => x["Name"] == "TxtDBAMTime"))
                                RecordTime = Temp.Find(x => x["Name"] == "TxtDBAMDate")["Value"] + " " + Temp.Find(x => x["Name"] == "TxtDBAMTime")["Value"];
                            //醫療輔助器
                            if (Temp.Exists(x => x["Name"] == "RbnIsMedicalDevices") && Temp.Find(x => x["Name"] == "RbnIsMedicalDevices")["Value"] == "有")
                            {
                                if (Temp.Exists(x => x["Name"] == "RbnMedicalDevices"))
                                {
                                    Dt["A_RbnIsMedicalDevices"] = CombinedWord(Temp, "RbnMedicalDevices", "醫療輔助器", "");
                                    Dt["A_RbnIsMedicalDevices"] += CombinedWord(Temp, "TxtMedicalDevices1_1", "管徑", "");
                                    Dt["A_RbnIsMedicalDevices"] += CombinedWord(Temp, "TxtMedicalDevices1_2", "fix", "");
                                    Dt["A_RbnIsMedicalDevices"] += CombinedWord(Temp, "TxtMedicalDevices2_1", "管徑", "");
                                }
                            }
                            else
                                Dt["A_RbnIsMedicalDevices"] = "呼吸無使用醫療輔助器";
                            //氧氣治療
                            if (Temp.Exists(x => x["Name"] == "RbnOxygen") && Temp.Find(x => x["Name"] == "RbnOxygen")["Value"].Equals("有"))
                            {
                                string FaceMask_Ventilator = string.Empty;
                                if (!string.IsNullOrEmpty(CombinedWord(Temp, "RbnFaceMask_Ventilator", "", "")) || !string.IsNullOrEmpty(CombinedWord(Temp, "RbnNotInvasion_Other", "", "")))
                                {
                                    FaceMask_Ventilator = (!string.IsNullOrEmpty(CombinedWord(Temp, "RbnNotInvasion_Other", "", ""))) ? CombinedWord(Temp, "RbnNotInvasion_Other", "", "") : CombinedWord(Temp, "RbnFaceMask_Ventilator", "", "");
                                }

                                string RbnFaceMask_Ventilator = (!string.IsNullOrEmpty(FaceMask_Ventilator)) ? "_" + FaceMask_Ventilator : "";
                                Dt["RbnOxygen"] = RecordTime + " " + CombinedWord(Temp, "RbnFaceMask", "用氧方式", "")
                                    + RbnFaceMask_Ventilator;
                                Dt["RbnOxygen"] += CombinedWord(Temp, "TxtO2Concentration", "氧氣流量", "L/min。");
                                Dt["RbnOxygen"] += CombinedWord(Temp, "TxtFaceMask3_1", "FiO2", "%。");
                                Dt["RbnOxygen"] += CombinedWord(Temp, "TxtFaceMask5_1", "MODE", "");
                                Dt["RbnOxygen"] += CombinedWord(Temp, "TxtFaceMask5_2", "TV", "");
                                Dt["RbnOxygen"] += CombinedWord(Temp, "TxtFaceMask5_3", "FiO2", "%");
                                Dt["RbnOxygen"] += CombinedWord(Temp, "TxtFaceMask5_4", "PEEP", "");
                                Dt["RbnOxygen"] += CombinedWord(Temp, "TxtFaceMask5_5", "Rate", "");
                            }
                            else
                                Dt["RbnOxygen"] = "無使用氧氣治療";
                            //進食方式
                            if (Temp.Exists(x => x["Name"] == "RbnEatingPatterns"))
                            {
                                Dt["RbnEatingPatterns"] = Temp.Find(x => x["Name"] == "RbnEatingPatterns")["Value"];
                                Dt["RbnEatingPatterns"] += CombinedWord(Temp, "TxtEatPat2_1", "管徑", "");
                                Dt["RbnEatingPatterns"] += CombinedWord(Temp, "TxtEatPat2_2", "固定", "");
                                Dt["RbnEatingPatterns"] += CombinedWord(Temp, "RbnEatPat2Item", "輔助物", "");
                                Dt["RbnEatingPatterns"] += CombinedWord(Temp, "TxtEatPat2_3", "消化", "");
                            }
                            if (Temp.Exists(x => x["Name"] == "TxtEatingPatterns99Other"))
                                Dt["RbnEatingPatterns"] = Temp.Find(x => x["Name"] == "TxtEatingPatterns99Other")["Value"];
                            //飲食種類
                            if (Temp.Exists(x => x["Name"] == "TxtDietType99Other"))
                            {
                                Dt["RbnDietType"] = Temp.Find(x => x["Name"] == "TxtDietType99Other")["Value"];
                                //Dt["RbnDietType"] += CombinedWord(Temp, "TxtDietType6Cont", "熱量", "");
                            }
                            //if (Temp.Exists(x => x["Name"] == "TxtDietType99Other"))
                            //    Dt["RbnDietType"] = Temp.Find(x => x["Name"] == "TxtDietType99Other")["Value"];
                            //觸診

                            if (Temp.Exists(x => x["Name"] == "RbnPalpation"))
                            {
                                Dt["RbnPalpation"] = Temp.Find(x => x["Name"] == "RbnPalpation")["Value"];
                                if (Dt["RbnPalpation"].Equals("異常"))
                                {
                                    Dt["RbnPalpation"] = CombinedWord(Temp, "ChkPalpationAbn", "", "");
                                    if (Temp.Exists(x => x["Name"] == "TxtPalpationAbnElse99Other"))
                                        Dt["RbnPalpation"] = Dt["RbnPalpation"].Substring(0, Dt["RbnPalpation"].Length - 2) + Temp.Find(x => x["Name"] == "TxtPalpationAbnElse99Other")["Value"];
                                }
                            }
                            //腸蠕動
                            if (Temp.Exists(x => x["Name"] == "ChkPeristalsisNoAssess"))
                                Dt["TxtPeristalsis"] = Temp.Find(x => x["Name"] == "ChkPeristalsisNoAssess")["Value"];
                            else
                            {
                                if (Temp.Exists(x => x["Name"] == "TxtPeristalsis"))
                                {
                                    Dt["TxtPeristalsis"] = Temp.Find(x => x["Name"] == "TxtPeristalsis")["Value"] + "次/分";
                                }
                            }
                            //腸音
                            if (Temp.Exists(x => x["Name"] == "RbnBowelSounds"))
                            {
                                Dt["RbnBowelSounds"] = Temp.Find(x => x["Name"] == "RbnBowelSounds")["Value"];
                            }
                            //Decompression
                            if (Temp.Exists(x => x["Name"] == "RbnDecompression"))
                            {
                                Dt["RbnDecompression"] = Temp.Find(x => x["Name"] == "RbnDecompression")["Value"];

                                if (Dt["RbnDecompression"].Equals("有"))
                                {
                                    Dt["RbnDecompression"] = "量：" + Temp.Find(x => x["Name"] == "RbnDecCount")["Value"];
                                    Dt["RbnDecompression"] += "，顏色：" + Temp.Find(x => x["Name"] == "RbnDecColor")["Value"];
                                    if (Temp.Exists(x => x["Name"] == "TxtDecColor99Other"))
                                        Dt["RbnDecompression"] = Dt["RbnDecompression"].Substring(0, Dt["RbnDecompression"].Length - 2) + Temp.Find(x => x["Name"] == "TxtDecColor99Other")["Value"];
                                    Dt["RbnDecompression"] += "，性質：" + Temp.Find(x => x["Name"] == "RbnDecNature")["Value"];
                                    if (Temp.Exists(x => x["Name"] == "TxtDecNature99Other"))
                                        Dt["RbnDecompression"] = Dt["RbnDecompression"].Substring(0, Dt["RbnDecompression"].Length - 2) + Temp.Find(x => x["Name"] == "TxtDecNature99Other")["Value"];
                                }
                            }
                            //排尿狀況
                            if (Temp.Exists(x => x["Name"] == "RbnUrination"))
                                Dt["RbnUrination"] = Temp.Find(x => x["Name"] == "RbnUrination")["Value"];
                            if (Temp.Exists(x => x["Name"] == "ChkUrinationAbn"))
                            {
                                Dt["RbnUrination"] = Temp.Find(x => x["Name"] == "ChkUrinationAbn")["Value"];
                                if (Temp.Exists(x => x["Name"] == "TxtUrinationAbn99Other"))
                                    Dt["RbnUrination"] = Dt["RbnUrination"].Substring(0, Dt["RbnUrination"].Length - 2) + Temp.Find(x => x["Name"] == "TxtUrinationAbn99Other")["Value"];
                            }
                            //排尿方式
                            if (Temp.Exists(x => x["Name"] == "RbnVoidingPattern"))
                            {
                                Dt["RbnVoidingPattern"] = Temp.Find(x => x["Name"] == "RbnVoidingPattern")["Value"];
                                    if (Temp.Exists(x => x["Name"] == "TxtVoidingPattern99Other"))
                                        {
                                          Dt["RbnVoidingPattern"] = Dt["RbnVoidingPattern"].Substring(0, Dt["RbnVoidingPattern"].Length - 2) + Temp.Find(x => x["Name"] == "TxtVoidingPattern99Other")["Value"];
                                        }
                            }
                                //尿液性狀
                                if (Temp.Exists(x => x["Name"] == "TxtUrineCharactersAmount"))
                            {
                                Dt["TxtUrineCharactersAmount"] = "量：" + Temp.Find(x => x["Name"] == "TxtUrineCharactersAmount")["Value"];
                            }
                            else
                            {
                                Dt["TxtUrineCharactersAmount"] = "";
                            }
                            if (Temp.Exists(x => x["Name"] == "RbnUrineColor"))
                            {
                                Dt["TxtUrineCharactersAmount"] += "，顏色：" + Temp.Find(x => x["Name"] == "RbnUrineColor")["Value"];
                            if (Temp.Exists(x => x["Name"] == "Txt_UrineColor_Other"))
                                {
                                Dt["TxtUrineCharactersAmount"] = Dt["TxtUrineCharactersAmount"].Substring(0, Dt["TxtUrineCharactersAmount"].Length - 2) + Temp.Find(x => x["Name"] == "Txt_UrineColor_Other")["Value"];
                                }
                            }
                                if (Temp.Exists(x => x["Name"] == "RbnUrineNature"))
                            { Dt["TxtUrineCharactersAmount"] += "，性質：" + Temp.Find(x => x["Name"] == "RbnUrineNature")["Value"]; }
                            if (Temp.Exists(x => x["Name"] == "Txt_UrineNature_Other"))
                                Dt["TxtUrineCharactersAmount"] = Dt["TxtUrineCharactersAmount"].Substring(0, Dt["TxtUrineCharactersAmount"].Length - 2) + Temp.Find(x => x["Name"] == "Txt_UrineNature_Other")["Value"];
                            //排便狀況
                            if (Temp.Exists(x => x["Name"] == "RbnDefecation"))
                            {
                                Dt["RbnDefecation"] = Temp.Find(x => x["Name"] == "RbnDefecation")["Value"];
                            }
                            else
                            {
                                Dt["RbnDefecation"] = "";
                            }
                            if (Dt["RbnDefecation"].Equals("異常"))
                            {
                                if (Temp.Exists(x => x["Name"] == "ChkDefecationAbn"))
                                    ListWord = Temp.Find(x => x["Name"] == "ChkDefecationAbn")["Value"].Split(',');
                                if (ListWord.Length > 0)
                                {
                                    Temp_String = string.Empty;
                                    foreach (string word in ListWord)
                                    {
                                        switch (word)
                                        {
                                            case "便秘":
                                                Temp_String += word + "，";
                                                if (Temp.Exists(x => x["Name"] == "RbnDefecationAbn1"))
                                                Temp_String += Temp.Find(x => x["Name"] == "RbnDefecationAbn1")["Value"] + "、";
                                                break;
                                            case "其他":
                                                if (Temp.Exists(x => x["Name"] == "TxtDefecationAbn99Other"))
                                                Temp_String += Temp.Find(x => x["Name"] == "TxtDefecationAbn99Other")["Value"] + "、";
                                                break;
                                            default:
                                                Temp_String += word + "、";
                                                break;
                                        }
                                    }
                                    
                                    if (Temp_String.Length > 1)
                                        Dt["RbnDefecation"] = Temp_String.Substring(0, Temp_String.Length - 1);

                                }
                            }
                            //呼吸音
                            Dt["A_RbnBrthSnds"] = RecordTime + " ";
                            if (Temp.Exists(x => x["Name"] == "RbnBrthSndsR") && Temp.Find(x => x["Name"] == "RbnBrthSndsR")["Value"].Equals("異常"))
                            {
                                Dt["A_RbnBrthSnds"] += "<label class='LittleTitle'>右側呼吸音</label>：" + Temp.Find(x => x["Name"] == "ChkBrthSndsRAbnor")["Value"];
                                if (Temp.Exists(x => x["Name"] == "TxtBrthSndsRAbnorOther"))
                                    Dt["A_RbnBrthSnds"] = Dt["A_RbnBrthSnds"].Substring(0, Dt["A_RbnBrthSnds"].Length - 2) + Temp.Find(x => x["Name"] == "TxtBrthSndsRAbnorOther")["Value"];
                            }
                            else
                                Dt["A_RbnBrthSnds"] += CombinedWord(Temp, "RbnBrthSndsR", "右側呼吸音", "");
                            if (Temp.Exists(x => x["Name"] == "RbnBrthSndsL") && Temp.Find(x => x["Name"] == "RbnBrthSndsL")["Value"].Equals("異常"))
                            {
                                if (Temp.Exists(x => x["Name"] == "ChkBrthSndsLAbnor"))
                                    Dt["A_RbnBrthSnds"] += "; <label class='LittleTitle'>左側呼吸音</label>：" + Temp.Find(x => x["Name"] == "ChkBrthSndsLAbnor")["Value"];
                                if (Temp.Exists(x => x["Name"] == "TxtBrthSndsLAbnorOther"))
                                    Dt["A_RbnBrthSnds"] = Dt["A_RbnBrthSnds"].Substring(0, Dt["A_RbnBrthSnds"].Length - 2) + Temp.Find(x => x["Name"] == "TxtBrthSndsLAbnorOther")["Value"];
                            }
                            else
                                Dt["A_RbnBrthSnds"] += "; " + CombinedWord(Temp, "RbnBrthSndsL", "左側呼吸音", "");
                            //呼吸型態
                            if (Temp.Exists(x => x["Name"] == "RbnBreathing") && Temp.Find(x => x["Name"] == "RbnBreathing")["Value"] == "異常")
                            {
                                if (Temp.Exists(x => x["Name"] == "ChkBreathingAbnormal"))
                                    ListWord = Temp.Find(x => x["Name"] == "ChkBreathingAbnormal")["Value"].Split(',');
                                if (ListWord.Length > 0)
                                {
                                    Temp_String = string.Empty;
                                    foreach (string word in ListWord)
                                    {
                                        switch (word)
                                        {
                                            case "呼吸急促":
                                                if (Temp.Exists(x => x["Name"] == "TxtBreathingAbn1Cont"))
                                                    Temp_String += word + "，呼吸頻率：" + Temp.Find(x => x["Name"] == "TxtBreathingAbn1Cont")["Value"] + "次/分、";
                                                break;
                                            case "其他":
                                                if (Temp.Exists(x => x["Name"] == "TxtBreathingAbnormal99Other"))
                                                    Temp_String += Temp.Find(x => x["Name"] == "TxtBreathingAbnormal99Other")["Value"] + "、";
                                                break;
                                            default:
                                                Temp_String += word + "、";
                                                break;
                                        }
                                    }
                                    
                                    if (Temp_String.Length > 1)
                                        Dt["A_RbnBreathing"] = RecordTime + " 呼吸型態：" + Temp_String.Substring(0, Temp_String.Length - 1);

                                }
                            }
                            else if (Temp.Exists(x => x["Name"] == "RbnBreathing"))
                                Dt["A_RbnBreathing"] = RecordTime + " 呼吸型態：" + Temp.Find(x => x["Name"] == "RbnBreathing")["Value"];
                            //痰液
                            Dt["A_RbnSputum"] = RecordTime + " ";
                            if (Temp.Exists(x => x["Name"] == "RbnSputum") && Temp.Find(x => x["Name"] == "RbnSputum")["Value"].Equals("有"))
                            {
                                if (Temp.Exists(x => x["Name"] == "RbnSputumCount"))
                                    Dt["A_RbnSputum"] += "<label class='LittleTitle'>痰液量：</label>" + Temp.Find(x => x["Name"] == "RbnSputumCount")["Value"] + "，"
                                    + "<label class='LittleTitle'>顏色：</label>" + Temp.Find(x => x["Name"] == "RbnSputumColor")["Value"];
                                if (Temp.Exists(x => x["Name"] == "TxtSputumColor99Other"))
                                    Dt["A_RbnSputum"] = Dt["A_RbnSputum"].Substring(0, Dt["A_RbnSputum"].Length - 2) + Temp.Find(x => x["Name"] == "TxtSputumColor99Other")["Value"];
                                if (Temp.Exists(x => x["Name"] == "RbnSputumNature"))
                                    Dt["A_RbnSputum"] += "，<label class='LittleTitle'>性質：</label>" + Temp.Find(x => x["Name"] == "RbnSputumNature")["Value"];
                                if (Temp.Exists(x => x["Name"] == "TxtSputumNature99Other"))
                                    Dt["A_RbnSputum"] = Dt["A_RbnSputum"].Substring(0, Dt["A_RbnSputum"].Length - 2) + Temp.Find(x => x["Name"] == "TxtSputumNature99Other")["Value"];
                            }
                            else
                            {
                                if (Temp.Exists(x => x["Name"] == "RbnSputum"))
                                Dt["A_RbnSputum"] += "<label class='LittleTitle'>痰液量：</label>" + Temp.Find(x => x["Name"] == "RbnSputum")["Value"] + "。";
                            }
                        }
                    }

                    #endregion

                    #region 每日評估_兒童

                    //撈每日評估

                    sql = "SELECT * FROM(SELECT DBAM_ID, DBAM_TYPE FROM DAILY_BODY_ASSESSMENT_MASTER "
                    + "WHERE FEENO = '" + feeno + "' AND DELETED = 'N' AND DBAM_TYPE = 'child' "
                    + "ORDER BY CREATTIME DESC, UPDTIME DESC "
                    + ") WHERE ROWNUM <= 1 ";
                    TableId = string.Empty; NaType = string.Empty;
                    
                    Dt2 = link.DBExecSQL(sql);
                    if (Dt2.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt2.Rows.Count; i++)
                        {
                            TableId = Dt2.Rows[i]["DBAM_ID"].ToString();
                            NaType = Dt2.Rows[i]["DBAM_TYPE"].ToString();
                            //TableId = reader["DBAM_ID"].ToString();
                            //NaType = reader["DBAM_TYPE"].ToString();
                        }
                    }

                    if (TableId != string.Empty && NaType != string.Empty)
                    {
                        sql = "SELECT * FROM DAILY_BODY_ASSESSMENT_DETAIL "
                        + "WHERE DBAM_ID = '" + TableId + "' ";
                        Temp = new List<Dictionary<string, string>>();
                        
                        Dt2 = link.DBExecSQL(sql);
                        if (Dt2.Rows.Count > 0)
                        {
                            for (int i = 0; i < Dt2.Rows.Count; i++)
                            {
                                temp = new Dictionary<string, string>();
                                temp["Name"] = Dt2.Rows[i]["DBAD_ITEMID"].ToString();
                                temp["Value"] = Dt2.Rows[i]["DBAD_ITEMVALUE"].ToString();
                                Temp.Add(temp);
                            }
                        }

                        if (Temp.Count > 0)
                        {
                            string RecordTime = "";
                            if (Temp.Exists(x => x["Name"] == "TxtDBAMDate") && Temp.Exists(x => x["Name"] == "TxtDBAMTime"))
                                RecordTime = Temp.Find(x => x["Name"] == "TxtDBAMDate")["Value"] + " " + Temp.Find(x => x["Name"] == "TxtDBAMTime")["Value"];
                            //醫療輔助器
                            if (Temp.Exists(x => x["Name"] == "RbnIsMedicalDevices") && Temp.Find(x => x["Name"] == "RbnIsMedicalDevices")["Value"] == "有")
                            {
                                if (Temp.Exists(x => x["Name"] == "RbnMedicalDevices"))
                                {
                                    Dt["C_RbnIsMedicalDevices"] = CombinedWord(Temp, "RbnMedicalDevices", "醫療輔助器", "");
                                    Dt["C_RbnIsMedicalDevices"] += CombinedWord(Temp, "RbnFaceMask", "用氧方式", "");
                                    Dt["C_RbnIsMedicalDevices"] += CombinedWord(Temp, "TxtFaceMask5_1", "", " Mode");
                                    Dt["C_RbnIsMedicalDevices"] += CombinedWord(Temp, "TxtFaceMask5_2", "TV", "");
                                    Dt["C_RbnIsMedicalDevices"] += CombinedWord(Temp, "TxtFaceMask5_3", "FiO2", "");
                                    Dt["C_RbnIsMedicalDevices"] += CombinedWord(Temp, "TxtFaceMask5_4", "PEEP", "");
                                    Dt["C_RbnIsMedicalDevices"] += CombinedWord(Temp, "TxtFaceMask5_5", "Rate", "");
                                }
                            }
                            else
                                Dt["C_RbnIsMedicalDevices"] = "無使用醫療輔助器呼吸";
                            //呼吸
                            Dt["C_RbnBrthSnds"] = RecordTime + " ";
                            if (Temp.Exists(x => x["Name"] == "RbnBrthSndsR") && Temp.Find(x => x["Name"] == "RbnBrthSndsR")["Value"].Equals("異常"))
                            {
                                Dt["C_RbnBrthSnds"] += "<label class='LittleTitle'>右側呼吸音</label>：" + Temp.Find(x => x["Name"] == "ChkBrthSndsRAbnor")["Value"];
                                if (Temp.Exists(x => x["Name"] == "TxtBrthSndsRAbnorOther"))
                                    Dt["C_RbnBrthSnds"] = Dt["C_RbnBrthSnds"].Substring(0, Dt["C_RbnBrthSnds"].Length - 2) + Temp.Find(x => x["Name"] == "TxtBrthSndsRAbnorOther")["Value"];
                            }
                            else
                                Dt["C_RbnBrthSnds"] += CombinedWord(Temp, "RbnBrthSndsR", "右側呼吸音", "");
                            if (Temp.Exists(x => x["Name"] == "RbnBrthSndsL") && Temp.Find(x => x["Name"] == "RbnBrthSndsL")["Value"].Equals("異常"))
                            {
                                if (Temp.Exists(x => x["Name"] == "ChkBrthSndsLAbnor"))
                                    Dt["C_RbnBrthSnds"] += "; <label class='LittleTitle'>左側呼吸音</label>：" + Temp.Find(x => x["Name"] == "ChkBrthSndsLAbnor")["Value"];
                                if (Temp.Exists(x => x["Name"] == "TxtBrthSndsLAbnorOther"))
                                    Dt["C_RbnBrthSnds"] = Dt["C_RbnBrthSnds"].Substring(0, Dt["C_RbnBrthSnds"].Length - 2) + Temp.Find(x => x["Name"] == "TxtBrthSndsLAbnorOther")["Value"];
                            }
                            else
                                Dt["C_RbnBrthSnds"] += "; " + CombinedWord(Temp, "RbnBrthSndsL", "左側呼吸音", "");
                            //呼吸型態
                            if (Temp.Exists(x => x["Name"] == "RbnBreathing") && Temp.Find(x => x["Name"] == "RbnBreathing")["Value"] == "異常")
                            {
                                if (Temp.Exists(x => x["Name"] == "ChkBreathingAbnormal"))
                                    ListWord = Temp.Find(x => x["Name"] == "ChkBreathingAbnormal")["Value"].Split(',');
                                if (ListWord.Length > 0)
                                {
                                    Temp_String = string.Empty;
                                    foreach (string word in ListWord)
                                    {
                                        switch (word)
                                        {
                                            case "呼吸急促":
                                                if (Temp.Exists(x => x["Name"] == "TxtBreathingAbn1Cont"))
                                                    Temp_String += word + "，呼吸頻率：" + Temp.Find(x => x["Name"] == "TxtBreathingAbn1Cont")["Value"] + "次/分、";
                                                break;
                                            case "其他":
                                                if (Temp.Exists(x => x["Name"] == "TxtBreathingAbnormal99Other"))
                                                    Temp_String += Temp.Find(x => x["Name"] == "TxtBreathingAbnormal99Other")["Value"] + "、";
                                                break;
                                            default:
                                                Temp_String += word + "、";
                                                break;
                                        }
                                    }
                                    if (Temp_String.Length > 1)
                                        Dt["C_RbnBreathing"] = RecordTime + " 呼吸型態：" + Temp_String.Substring(0, Temp_String.Length - 1);
                                }
                            }
                            else if (Temp.Exists(x => x["Name"] == "RbnBreathing"))
                                Dt["C_RbnBreathing"] = RecordTime + " 呼吸型態：" + Temp.Find(x => x["Name"] == "RbnBreathing")["Value"];
                            //痰液
                            Dt["C_RbnSputum"] = RecordTime + " ";
                            if (Temp.Exists(x => x["Name"] == "RbnSputum") && Temp.Find(x => x["Name"] == "RbnSputum")["Value"].Equals("有"))
                            {
                                if (Temp.Exists(x => x["Name"] == "RbnSputumCount"))
                                    Dt["C_RbnSputum"] += "<label class='LittleTitle'>痰液量：</label>" + Temp.Find(x => x["Name"] == "RbnSputumCount")["Value"] + "，"
                                    + "<label class='LittleTitle'>顏色：</label>" + Temp.Find(x => x["Name"] == "RbnSputumColor")["Value"];
                                if (Temp.Exists(x => x["Name"] == "TxtSputumColor99Other"))
                                    Dt["C_RbnSputum"] = Dt["C_RbnSputum"].Substring(0, Dt["C_RbnSputum"].Length - 2) + Temp.Find(x => x["Name"] == "TxtSputumColor99Other")["Value"];
                                Dt["C_RbnSputum"] += "，<label class='LittleTitle'>性質：</label>" + Temp.Find(x => x["Name"] == "RbnSputumNature")["Value"];
                                if (Temp.Exists(x => x["Name"] == "TxtSputumNature99Other"))
                                    Dt["C_RbnSputum"] = Dt["C_RbnSputum"].Substring(0, Dt["C_RbnSputum"].Length - 2) + Temp.Find(x => x["Name"] == "TxtSputumNature99Other")["Value"];
                            }
                            else
                            {
                                if (Temp.Exists(x => x["Name"] == "RbnSputum"))
                                {
                                    Dt["C_RbnSputum"] += "<label class='LittleTitle'>痰液量：</label>" + Temp.Find(x => x["Name"] == "RbnSputum")["Value"] + "。";
                                }
                            }
                            //腸胃系統
                            if (Temp.Exists(x => x["Name"] == "RbnStomach") && Temp.Find(x => x["Name"] == "RbnStomach")["Value"] == "異常")
                            {
                                if (Temp.Exists(x => x["Name"] == "ChkStomachAbnormal"))
                                    ListWord = Temp.Find(x => x["Name"] == "ChkStomachAbnormal")["Value"].Split(',');
                                if (ListWord.Length > 0)
                                {
                                    Temp_String = string.Empty;
                                    foreach (string word in ListWord)
                                    {
                                        switch (word)
                                        {
                                            case "疝氣":
                                                if (Temp.Exists(x => x["Name"] == "ChkStomachAbn6Position"))
                                                    Temp_String += word + "，部位：" + Temp.Find(x => x["Name"] == "ChkStomachAbn6Position")["Value"] + "、";
                                                break;
                                            case "腹瀉":
                                                if (Temp.Exists(x => x["Name"] == "TxtStomachAbnl9Cont"))
                                                    Temp_String += word + "，頻次：" + Temp.Find(x => x["Name"] == "TxtStomachAbnl9Cont")["Value"] + "次/天"
                                                     + "，性狀：";
                                                if (Temp.Exists(x => x["Name"] == "RbnStoolNature"))
                                                {
                                                    Temp_String += Temp.Find(x => x["Name"] == "RbnStoolNature")["Value"] + "、";
                                                }
                                                break;
                                            case "其他":
                                                if (Temp.Exists(x => x["Name"] == "TxtStomachAbnormal99Other"))
                                                    Temp_String += Temp.Find(x => x["Name"] == "TxtStomachAbnormal99Other")["Value"] + "、";
                                                break;
                                            default:
                                                Temp_String += word + "、";
                                                break;
                                        }
                                    }
                                    if (Temp_String.Length > 1)
                                        Dt["RbnStomach"] = RecordTime + " " + Temp_String.Substring(0, Temp_String.Length - 1);
                                }
                            }
                            else
                            {
                                if (Temp.Exists(x => x["Name"] == "RbnStomach"))
                                    Dt["RbnStomach"] = RecordTime + " " + Temp.Find(x => x["Name"] == "RbnStomach")["Value"];
                            }
                            //泌尿系統
                            if (Temp.Exists(x => x["Name"] == "RbnUrinarySys") && Temp.Find(x => x["Name"] == "RbnUrinarySys")["Value"] == "異常")
                            {
                                if (Temp.Exists(x => x["Name"] == "ChkUrinarySysAbnormal"))
                                    Dt["RbnUrinarySys"] = RecordTime + " " + Temp.Find(x => x["Name"] == "ChkUrinarySysAbnormal")["Value"].Replace(",", "、");
                                if (Temp.Exists(x => x["Name"] == "TxtUrinarySysAbnormal99Other"))
                                    Dt["RbnUrinarySys"] = Dt["RbnUrinarySys"].Substring(0, Dt["RbnUrinarySys"].Length - 2) + Temp.Find(x => x["Name"] == "TxtUrinarySysAbnormal99Other")["Value"];
                            }
                            else
                            {
                                if (Temp.Exists(x => x["Name"] == "RbnUrinarySys"))
                                    Dt["RbnUrinarySys"] = RecordTime + " " + Temp.Find(x => x["Name"] == "RbnUrinarySys")["Value"];
                            }
                        }
                    }

                    #endregion

                    #region 壓傷

                    //壓傷
                    sql = "SELECT TOTAL, RECORDTIME FROM NIS_PRESSURE_SORE_DATA "
                    + "WHERE FEENO = '" + feeno + "' AND RECORDTIME = ( "
                    + "SELECT MAX(RECORDTIME) FROM NIS_PRESSURE_SORE_DATA WHERE FEENO = '" + feeno + "' ) ";
                    
                    Dt2 = link.DBExecSQL(sql);
                    if (Dt2.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt2.Rows.Count; i++)
                        {
                            Dt["PressureScore"] = "<label class='LittleTitle'>狀態</label>：#status#；#recordtime# <label class='LittleTitle'>評估結果</label>：#total#分";
                            if (int.Parse(Dt2.Rows[i]["TOTAL"].ToString()) < 19)
                                Dt["PressureScore"] = Dt["PressureScore"].Replace("#status#", "高危");
                            else
                                Dt["PressureScore"] = Dt["PressureScore"].Replace("#status#", "非高危");
                            Dt["PressureScore"] = Dt["PressureScore"].Replace("#recordtime#", Convert.ToDateTime(Dt2.Rows[i]["RECORDTIME"].ToString()).ToString("yyyy/MM/dd")).Replace("#total#", Dt2.Rows[i]["TOTAL"].ToString());
                        }
                    }
                    #endregion

                    #region 跌倒_成人

                    //跌倒
                    sql = "SELECT TOTAL, RECORDTIME FROM NIS_FALL_ASSESS_DATA "
                    + "WHERE FEENO = '" + feeno + "' AND RECORDTIME = ( "
                    + "SELECT MAX(RECORDTIME) FROM NIS_FALL_ASSESS_DATA WHERE FEENO = '" + feeno + "' ) ";
                    Temp = new List<Dictionary<string, string>>();
                    
                    Dt2 = link.DBExecSQL(sql);
                    if (Dt2.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt2.Rows.Count; i++)
                        {
                            temp = new Dictionary<string, string>();
                            temp["RECORDTIME"] = Dt2.Rows[i]["RECORDTIME"].ToString();
                            Temp.Add(temp);

                            Dt["A_FallAssess"] = "<label class='LittleTitle'>狀態</label>：#status#；#recordtime# <label class='LittleTitle'>評估結果</label>：#total#分";
                            if (int.Parse(Dt2.Rows[i]["TOTAL"].ToString()) > 2)  //0.1.2為非高危。3.4.5.6.7.8都屬高危
                                 Dt["A_FallAssess"] = Dt["A_FallAssess"].Replace("#status#", "高危");
                            else
                                Dt["A_FallAssess"] = Dt["A_FallAssess"].Replace("#status#", "非高危");
                            Dt["A_FallAssess"] = Dt["A_FallAssess"].Replace("#recordtime#", Convert.ToDateTime(Dt2.Rows[i]["RECORDTIME"].ToString()).ToString("yyyy/MM/dd")).Replace("#total#", Dt2.Rows[i]["TOTAL"].ToString());
                       }
                    }

                    #endregion

                    #region 跌倒_兒童

                    sql = "SELECT TOTAL, RECORDTIME FROM NIS_FALL_ASSESS_DATA_CHILD "
                    + "WHERE FEENO = '" + feeno + "' AND RECORDTIME = ( "
                    + "SELECT MAX(RECORDTIME) FROM NIS_FALL_ASSESS_DATA_CHILD WHERE FEENO = '" + feeno + "' ) ";
                    
                    Dt2 = link.DBExecSQL(sql);
                    if (Dt2.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt2.Rows.Count; i++)
                        {
                            if (Temp.Count > 0)
                            {
                                if (Convert.ToDateTime(Temp[0]["RECORDTIME"]) < Convert.ToDateTime(Dt2.Rows[i]["RECORDTIME"].ToString()))
                                {
                                    Dt["C_FallAssess"] = "<label class='LittleTitle'>狀態</label>：#status#；#recordtime# <label class='LittleTitle'>評估結果</label>：#total#分";
                                    if (int.Parse(Dt2.Rows[i]["TOTAL"].ToString()) > 2) //0.1.2為非高危。3.4.5.6.7.8都屬高危
                                        Dt["C_FallAssess"] = Dt["C_FallAssess"].Replace("#status#", "高危");
                                    else
                                        Dt["C_FallAssess"] = Dt["C_FallAssess"].Replace("#status#", "非高危");
                                    Dt["C_FallAssess"] = Dt["C_FallAssess"].Replace("#recordtime#", Convert.ToDateTime(Dt2.Rows[i]["RECORDTIME"].ToString()).ToString("yyyy/MM/dd")).Replace("#total#", Dt2.Rows[i]["TOTAL"].ToString());
                                }
                            }
                            else
                            {
                                Dt["C_FallAssess"] = "<label class='LittleTitle'>狀態</label>：#status#；#recordtime# <label class='LittleTitle'>評估結果</label>：#total#分";
                                if (int.Parse(Dt2.Rows[i]["TOTAL"].ToString()) >= 3)
                                    Dt["C_FallAssess"] = Dt["C_FallAssess"].Replace("#status#", "高危");
                                else
                                    Dt["C_FallAssess"] = Dt["C_FallAssess"].Replace("#status#", "非高危");
                                Dt["C_FallAssess"] = Dt["C_FallAssess"].Replace("#recordtime#", Convert.ToDateTime(Dt2.Rows[i]["RECORDTIME"].ToString()).ToString("yyyy/MM/dd")).Replace("#total#", Dt2.Rows[i]["TOTAL"].ToString());
                            }
                            //if (Temp.Count > 0)
                            //{
                            //    if (Convert.ToDateTime(Temp[0]["RECORDTIME"]) < Convert.ToDateTime(reader["RECORDTIME"].ToString()))
                            //    {
                            //        Dt["C_FallAssess"] = "<label class='LittleTitle'>狀態</label>：#status#；#recordtime# <label class='LittleTitle'>評估結果</label>：#total#分";
                            //        if (int.Parse(reader["TOTAL"].ToString()) > 2) //0.1.2為非高危。3.4.5.6.7.8都屬高危
                            //            Dt["C_FallAssess"] = Dt["C_FallAssess"].Replace("#status#", "高危");
                            //        else
                            //            Dt["C_FallAssess"] = Dt["C_FallAssess"].Replace("#status#", "非高危");
                            //        Dt["C_FallAssess"] = Dt["C_FallAssess"].Replace("#recordtime#", Convert.ToDateTime(reader["RECORDTIME"].ToString()).ToString("yyyy/MM/dd")).Replace("#total#", reader["TOTAL"].ToString());
                            //    }
                            //}
                            //else
                            //{
                            //    Dt["C_FallAssess"] = "<label class='LittleTitle'>狀態</label>：#status#；#recordtime# <label class='LittleTitle'>評估結果</label>：#total#分";
                            //    if (int.Parse(reader["TOTAL"].ToString()) >= 3)
                            //        Dt["C_FallAssess"] = Dt["C_FallAssess"].Replace("#status#", "高危");
                            //    else
                            //        Dt["C_FallAssess"] = Dt["C_FallAssess"].Replace("#status#", "非高危");
                            //    Dt["C_FallAssess"] = Dt["C_FallAssess"].Replace("#recordtime#", Convert.ToDateTime(reader["RECORDTIME"].ToString()).ToString("yyyy/MM/dd")).Replace("#total#", reader["TOTAL"].ToString());
                            //}
                        }
                    }

                    #endregion

                    #region 營養_成人

                    //營養_成人
                    sql = "SELECT NUTA_ASSESSMENT_ROW4 AS TOTAL, NUTA_ASSESSMENT_DTM AS RECORDTIME FROM NUTRITIONAL_ASSESSMENT "
                    + "WHERE FEENO = '" + feeno + "' AND NUTA_ASSESSMENT_DTM = ( "
                    + "SELECT MAX(NUTA_ASSESSMENT_DTM) FROM NUTRITIONAL_ASSESSMENT WHERE FEENO = '" + feeno + "' AND NUTA_TYPE = 'adult' ) "
                    + " AND NUTA_TYPE = 'adult'";
                    Dt2 = link.DBExecSQL(sql);
                    if (Dt2.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt2.Rows.Count; i++)
                        {
                            Dt["A_NutritionalAssessment"] = "<label class='LittleTitle'>狀態</label>：#status#；#recordtime# <label class='LittleTitle'>MUST總分</label>：#total#分";
                            if (int.Parse(Dt2.Rows[i]["TOTAL"].ToString()) > 2)
                                Dt["A_NutritionalAssessment"] = Dt["A_NutritionalAssessment"].Replace("#status#", "高危");
                            else
                                Dt["A_NutritionalAssessment"] = Dt["A_NutritionalAssessment"].Replace("#status#", "非高危");
                            Dt["A_NutritionalAssessment"] = Dt["A_NutritionalAssessment"].Replace("#recordtime#", Convert.ToDateTime(Dt2.Rows[i]["RECORDTIME"].ToString()).ToString("yyyy/MM/dd")).Replace("#total#", Dt2.Rows[i]["TOTAL"].ToString());
                            //Dt["A_NutritionalAssessment"] = "<label class='LittleTitle'>狀態</label>：#status#；#recordtime# <label class='LittleTitle'>MUST總分</label>：#total#分";
                            //if (int.Parse(reader["TOTAL"].ToString()) > 2)
                            //    Dt["A_NutritionalAssessment"] = Dt["A_NutritionalAssessment"].Replace("#status#", "高危");
                            //else
                            //    Dt["A_NutritionalAssessment"] = Dt["A_NutritionalAssessment"].Replace("#status#", "非高危");
                            //Dt["A_NutritionalAssessment"] = Dt["A_NutritionalAssessment"].Replace("#recordtime#", Convert.ToDateTime(reader["RECORDTIME"].ToString()).ToString("yyyy/MM/dd")).Replace("#total#", reader["TOTAL"].ToString());
                        }
                    }

                    //營養_兒童
                    sql = "SELECT * FROM NUTRITIONAL_ASSESSMENT "
                    + "WHERE FEENO = '" + feeno + "' AND NUTA_ASSESSMENT_DTM = ( "
                    + "SELECT MAX(NUTA_ASSESSMENT_DTM) FROM NUTRITIONAL_ASSESSMENT WHERE FEENO = '" + feeno + "' AND NUTA_TYPE = 'child' ) ";

                    //bool_read = link.DBExecSQL(sql, ref reader);
                    //if (bool_read)
                    //{
                    //    while (reader.Read())
                    //    {
                    //        Dt["C_NutritionalAssessment"] = "<label class='LittleTitle'>身高</label>：" + reader["NUTA_HEIGHT"].ToString() + "cm，<label class='LittleTitle'>百分比</label>：" + reader["NUTA_ASSESSMENT_ROW1"].ToString() + "；"
                    //            + "<label class='LittleTitle'>體重</label>：" + reader["NUTA_WEIGHT"].ToString() + "kg，" + reader["NUTA_ASSESSMENT_ROW2"].ToString() + "；"
                    //             + "<label class='LittleTitle'>BMI</label>：" + reader["NUTA_BMI"].ToString() + "，<label class='LittleTitle'>BMI百分比</label>：" + reader["NUTA_ASSESSMENT_ROW4"].ToString() + "；"
                    //             + "<label class='LittleTitle'>頭圍</label>：" + reader["NUTA_BMI"].ToString() + "，<label class='LittleTitle'>頭圍百分比</label>：" + reader["NUTA_ASSESSMENT_ROW3"].ToString();
                    //    }
                    //}
                    Dt2 = link.DBExecSQL(sql);
                    if (Dt2.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt2.Rows.Count; i++)
                        {
                            Dt["C_NutritionalAssessment"] = "<label class='LittleTitle'>身高</label>：" + Dt2.Rows[i]["NUTA_HEIGHT"].ToString() + "cm，<label class='LittleTitle'>百分比</label>：" + Dt2.Rows[i]["NUTA_ASSESSMENT_ROW1"].ToString() + "；"
                                + "<label class='LittleTitle'>體重</label>：" + Dt2.Rows[i]["NUTA_WEIGHT"].ToString() + "kg，" + Dt2.Rows[i]["NUTA_ASSESSMENT_ROW2"].ToString() + "；"
                                 + "<label class='LittleTitle'>BMI</label>：" + Dt2.Rows[i]["NUTA_BMI"].ToString() + "，<label class='LittleTitle'>BMI百分比</label>：" + Dt2.Rows[i]["NUTA_ASSESSMENT_ROW4"].ToString() + "；"
                                 + "<label class='LittleTitle'>頭圍</label>：" + Dt2.Rows[i]["NUTA_BMI"].ToString() + "，<label class='LittleTitle'>頭圍百分比</label>：" + Dt2.Rows[i]["NUTA_ASSESSMENT_ROW3"].ToString();
                        }
                    }
                    //reader.Close();
                    //reader.Dispose();

                    #endregion

                    #region 營養_兒童

                    //營養_兒童
                    sql = "SELECT * FROM NUTRITIONAL_ASSESSMENT "
                    + "WHERE FEENO = '" + feeno + "' AND NUTA_ASSESSMENT_DTM = ( "
                    + "SELECT MAX(NUTA_ASSESSMENT_DTM) FROM NUTRITIONAL_ASSESSMENT WHERE FEENO = '" + feeno + "' AND NUTA_TYPE = 'child' ) ";

                    Dt2 = link.DBExecSQL(sql);
                    if (Dt2.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt2.Rows.Count; i++)
                        {
                            Dt["C_NutritionalAssessment"] = "<label class='LittleTitle'>評估日期</label>：" + Convert.ToDateTime(Dt2.Rows[i]["NUTA_ASSESSMENT_DTM"]).ToString("yyyy/MM/dd") + "<label class='LittleTitle'>身高</label>：" + Dt2.Rows[i]["NUTA_HEIGHT"].ToString() + "cm，<label class='LittleTitle'>百分比</label>：" + Dt2.Rows[i]["NUTA_ASSESSMENT_ROW1"].ToString() + "；"
                                + "<label class='LittleTitle'>體重</label>：" + Dt2.Rows[i]["NUTA_WEIGHT"].ToString() + "kg，<label class='LittleTitle'>百分比</label>：" + Dt2.Rows[i]["NUTA_ASSESSMENT_ROW2"].ToString() + "；"
                                 + "<label class='LittleTitle'>BMI</label>：" + Dt2.Rows[i]["NUTA_BMI"].ToString() + "；"
                                 + "<label class='LittleTitle'>頭圍</label>：" + Dt2.Rows[i]["NUTA_BMI"].ToString() + "cm，<label class='LittleTitle'>頭圍百分比</label>：" + Dt2.Rows[i]["NUTA_ASSESSMENT_ROW3"].ToString();
                        }
                    }

                    #endregion

                    #region 約束

                    //約束
                    sql = "SELECT ID FROM BINDTABLE A WHERE A.FEENO = '" + feeno + "' AND STATUS <> 'del' AND A.BOUT = "
                    + "(SELECT MAX(BOUT) FROM BINDTABLE WHERE FEENO = '" + feeno + "' AND STATUS <> 'del') "
                    + "AND A.ENDDT IS NULL ";
                    string BindID = string.Empty;

                    Dt2 = link.DBExecSQL(sql);
                    if (Dt2.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt2.Rows.Count; i++)
                        {
                            BindID = Dt2.Rows[i]["ID"].ToString();
                        }
                    }
                    

                    if (!string.IsNullOrWhiteSpace(BindID))
                    {
                        sql = "SELECT ASSESSDT, REASON, REASON_OTHER, CONSCIOUS, CONSCIOUS_OTHER, PULSE, TIGHTNESS, TEMPERATURE, COLOUR, REMOVE, INTEGRITY, FEELING FROM BINDTABLESAVE A "
                        + "WHERE A.FEENO = '" + feeno + "' AND A.STATUS <> 'del' AND A.ID = '" + BindID + "' AND ASSESS <> '0' ORDER BY ASSESSDT ";
                        Temp = new List<Dictionary<string, string>>();

                        Dt2 = link.DBExecSQL(sql);
                        if (Dt2.Rows.Count > 0)
                        {
                            for (int i = 0; i < Dt2.Rows.Count; i++)
                            {
                                temp = new Dictionary<string, string>();
                                temp["ASSESSDT"] = Dt2.Rows[i]["ASSESSDT"].ToString();
                                temp["REASON"] = Dt2.Rows[i]["REASON"].ToString();
                                temp["REASON_OTHER"] = Dt2.Rows[i]["REASON_OTHER"].ToString();
                                temp["CONSCIOUS"] = Dt2.Rows[i]["CONSCIOUS"].ToString();
                                temp["CONSCIOUS_OTHER"] = Dt2.Rows[i]["CONSCIOUS_OTHER"].ToString();
                                temp["PULSE"] = Dt2.Rows[i]["PULSE"].ToString();
                                temp["TIGHTNESS"] = Dt2.Rows[i]["TIGHTNESS"].ToString();
                                temp["TEMPERATURE"] = Dt2.Rows[i]["TEMPERATURE"].ToString();
                                temp["COLOUR"] = Dt2.Rows[i]["COLOUR"].ToString();
                                temp["REMOVE"] = Dt2.Rows[i]["REMOVE"].ToString();
                                temp["INTEGRITY"] = Dt2.Rows[i]["INTEGRITY"].ToString();
                                temp["FEELING"] = Dt2.Rows[i]["FEELING"].ToString();
                                Temp.Add(temp);
                            }
                            temp = Temp.OrderBy(x => x["ASSESSDT"]).First();
                            Dt["BindAssessment"] = /*Convert.ToDateTime(temp["ASSESSDT"]).ToString("yyyy/MM/dd HH:mm") +*/
                                 "<label class='LittleTitle'>約束方式：</label>" + temp["REASON"];
                            if (!string.IsNullOrWhiteSpace(temp["REASON_OTHER"]))
                                Dt["BindAssessment"] = Dt["BindAssessment"].Substring(0, Dt["BindAssessment"].Length - 2) + temp["REASON_OTHER"];

                            Dt["BindAssessment"] += "<label class='LittleTitle'>部位：</label>" + temp["CONSCIOUS"];
                            if (!string.IsNullOrWhiteSpace(temp["CONSCIOUS_OTHER"]))
                                Dt["BindAssessment"] = Dt["BindAssessment"].Substring(0, Dt["BindAssessment"].Length - 2) + temp["CONSCIOUS_OTHER"];

                            //Dt["BindAssessment"] += "<label class='LittleTitle'>脈搏：</label>" + temp["PULSE"]
                            //    + "<label class='LittleTitle'>約束鬆緊程度：</label>" + temp["TIGHTNESS"]
                            //    + "<label class='LittleTitle'>肢體末稍溫度：</label>" + temp["TEMPERATURE"]
                            //    + "<label class='LittleTitle'>顏色：</label>" + temp["COLOUR"]
                            //    + "<label class='LittleTitle'>皮膚完整性：</label>" + temp["INTEGRITY"]
                            //    + "<label class='LittleTitle'>感覺度：</label>" + temp["FEELING"];
                            if (Temp.Exists(x => x["REMOVE"] == "是"))
                                Dt["BindAssessment"] += "<label class='LittleTitle'>最後一次鬆綁時間：</label>" + Convert.ToDateTime(Temp.FindLast(x => x["REMOVE"] == "是")["ASSESSDT"]).ToString("yyyy/MM/dd HH:mm");
                        }
                        
                    }

                    #endregion

                    #region 出院準備

                    //出院準備
                    sql = "SELECT ASSESS_DATE, ASSESS_TIME, CASE_STATE, TOTAL FROM DISCHARGED_CARE "
                    + "WHERE FEE_NO = '" + feeno + "' AND ASSESS_DATE || ASSESS_TIME = ( "
                    + "SELECT MAX(ASSESS_DATE || ASSESS_TIME) FROM DISCHARGED_CARE WHERE FEE_NO = '" + feeno + "' ) ";

                    Dt2 = link.DBExecSQL(sql);
                    if (Dt2.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt2.Rows.Count; i++)
                        {
                            Dt["Dischareged"] = Convert.ToDateTime(Dt2.Rows[i]["ASSESS_DATE"].ToString() + " " + Dt2.Rows[i]["ASSESS_TIME"].ToString()).ToString("yyyy/MM/dd HH:mm")
                                + " <label class='LittleTitle'>總分：</label>" + Dt2.Rows[i]["TOTAL"].ToString()
                                + " ，<label class='LittleTitle'>出院準備小組收案結果</label>：";
                            if (Dt2.Rows[i]["CASE_STATE"].ToString().Equals("Y"))
                                Dt["Dischareged"] += "收案";
                            else if (Dt2.Rows[i]["CASE_STATE"].ToString().Equals("N"))
                                Dt["Dischareged"] += "不收案";
                            else
                                Dt["Dischareged"] += "";//2019/3/4修改, 原:待收案
                        }
                    }
                    
                    #endregion

                    #region 護理計畫

                    //護理計畫
                    sql = "SELECT * FROM CAREPLANMASTER WHERE FEENO = '" + feeno + "' ORDER BY PNO ASC ";
                    Temp = new List<Dictionary<string, string>>();

                    Dt2 = link.DBExecSQL(sql);
                    if (Dt2.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt2.Rows.Count; i++)
                        {
                            temp = new Dictionary<string, string>();
                            temp["RECORDID"] = Dt2.Rows[i]["RECORDID"].ToString();
                            temp["PNO"] = Dt2.Rows[i]["PNO"].ToString();
                            temp["TOPICDESC"] = Dt2.Rows[i]["TOPICDESC"].ToString();
                            temp["PLANSTARTDATE"] = Dt2.Rows[i]["PLANSTARTDATE"].ToString();
                            temp["PLANENDDATE"] = Dt2.Rows[i]["PLANENDDATE"].ToString();
                            Temp.Add(temp);
                        }
                    }
                    

                    if (Temp.Count > 0)
                    {
                        Temp_String = string.Empty;
                        string CPRFDTL = "";
                        foreach (var item in Temp)
                        {
                            CPRFDTL = "";
                            sql = "SELECT RELATEDFACTORSDESC FROM CPRFDTL WHERE RECORDID = '" + item["RECORDID"].ToString() + "' ";
                           
                            Dt2 = link.DBExecSQL(sql);
                            if (Dt2.Rows.Count > 0)
                            {
                                for (int i = 0; i < Dt2.Rows.Count; i++)
                                {
                                    CPRFDTL += Dt2.Rows[i]["RELATEDFACTORSDESC"].ToString() + "、";
                                    //CPRFDTL += reader["RELATEDFACTORSDESC"].ToString() + "、";
                                }
                            }
                            if (!string.IsNullOrWhiteSpace(CPRFDTL))
                                CPRFDTL = CPRFDTL.Substring(0, CPRFDTL.Length - 1);
                            if (string.IsNullOrWhiteSpace(item["PLANENDDATE"]) || Convert.ToDateTime(item["PLANENDDATE"]).ToString("yyyy/MM/dd") == DateTime.Now.ToString("yyyy/MM/dd"))
                            {
                                //Temp_String += string.Format("{0}<label class='LittleTitle'>護理計畫名稱</label>：{1}<label class='LittleTitle'>導因</label>：{2} {3}~{4}<hr/>"
                                //    , item["PNO"]
                                //    , item["TOPICDESC"]
                                //    , CPRFDTL
                                //    , Convert.ToDateTime(item["PLANSTARTDATE"]).ToString("yyyy/MM/dd HH:mm")
                                //    , (string.IsNullOrWhiteSpace(item["PLANENDDATE"]) ? "0000/00/00 00:00" : Convert.ToDateTime(item["PLANENDDATE"]).ToString("yyyy/MM/dd HH:mm")));

                                Temp_String += string.Format("[#{0}]  {1} <label class='LittleTitle'>護理計畫名稱</label>：{3} <hr/>"
                                 , item["PNO"]
                                 , Convert.ToDateTime(item["PLANSTARTDATE"]).ToString("yyyy/MM/dd HH:mm")
                                 , (string.IsNullOrWhiteSpace(item["PLANENDDATE"]) ? "0000/00/00 00:00" : Convert.ToDateTime(item["PLANENDDATE"]).ToString("yyyy/MM/dd HH:mm"))
                                 , item["TOPICDESC"]);
                            }
                        }
                        if (!string.IsNullOrWhiteSpace(Temp_String))
                        {
                            if (Temp_String.Length > 5)
                                Dt["CarePlan"] = Temp_String.Substring(0, Temp_String.Length - 5);
                        }
                           
                    }

                    #endregion

                    #region 衛教

                    //衛教
                    sql = "SELECT HEALTH_EDUCATION_DATA.*, "
                    + "(SELECT NAME FROM HEALTH_EDUCATION_ITEM_DATA WHERE HEALTH_EDUCATION_ITEM_DATA.ITEM_ID = HEALTH_EDUCATION_DATA.ITEMID)NAME "
                    + "FROM HEALTH_EDUCATION_DATA "
                    + "WHERE FEENO = '" + feeno + "' AND DELETED IS NULL "
                    + "ORDER BY SCORE_TIME,RECORDTIME DESC ";
                    
                    Dt2 = link.DBExecSQL(sql);
                    if (Dt2.Rows.Count > 0)
                    {
                        Temp_String = string.Empty;
                        int temp_num = 0;

                        for (int i = 0; i < Dt2.Rows.Count; i++)
                        {
                            temp_num++;
                            Temp_String += string.Format("<label class='LittleTitle'>指導日期：</label>{0}，<label class='LittleTitle'>指導名稱：</label>{1}，<label class='LittleTitle'>指導對象：</label>{2}，<label class='LittleTitle'>指導評值：</label>{3}<hr/>"
                                , Convert.ToDateTime(Dt2.Rows[i]["RECORDTIME"].ToString()).ToString("yyyy/MM/dd HH:mm")
                                , Dt2.Rows[i]["NAME"].ToString()
                                , Dt2.Rows[i]["OBJECT"].ToString().Replace("99|", "其他：")
                                , (string.IsNullOrWhiteSpace(Dt2.Rows[i]["SCORE_RESULT"].ToString())) ? "尚未評值" : Dt2.Rows[i]["SCORE_RESULT"].ToString().Replace("|", "，"));
                       }
                        if (temp_num > 0)
                        {
                            if (Temp_String.Length > 5)
                                Dt["Education"] = Temp_String.Substring(0, Temp_String.Length - 5);
                        }
                    }

                    #endregion

                    #region 生命徵象

                    //生命徵象   20181213一天內只取每個項目的最新DATA
                    sql = "SELECT CREATE_DATE, VS_ITEM, VS_PART, VS_RECORD, VS_MEMO,VS_REASON  "
                    + " FROM ( "
                    + "     SELECT CREATE_DATE, VS_ITEM, VS_PART, VS_RECORD, VS_MEMO,VS_REASON, "
                    + "     ROW_NUMBER() OVER(PARTITION BY t.VS_ITEM ORDER BY t.CREATE_DATE desc) NUM "
                    + "     FROM DATA_VITALSIGN t "
                    + "     WHERE FEE_NO = '" + feeno + "' "
                    + "     AND CREATE_DATE >= to_date('" + DateTime.Now.AddDays(-1).ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/MM/dd hh24:mi:ss') "
                    + "     ORDER BY CREATE_DATE DESC ) MAIN "
                    + " WHERE MAIN.NUM = 1 ";
                    Temp = new List<Dictionary<string, string>>();
                    DataTable dt_check = null;

                    Dt2 = link.DBExecSQL(sql);
                    if (Dt2.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt2.Rows.Count; i++)
                        {
                            temp = new Dictionary<string, string>();
                            temp["CREATE_DATE"] = Dt2.Rows[i]["CREATE_DATE"].ToString();
                            temp["VS_ITEM"] = Dt2.Rows[i]["VS_ITEM"].ToString();
                            temp["VS_PART"] = Dt2.Rows[i]["VS_PART"].ToString();
                            temp["VS_REASON"] = Dt2.Rows[i]["VS_REASON"].ToString();
                            temp["VS_RECORD"] = Dt2.Rows[i]["VS_RECORD"].ToString();
                            temp["VS_MEMO"] = Dt2.Rows[i]["VS_MEMO"].ToString();
                            Temp.Add(temp);
                        }
                    }

                    if (Temp.Count > 0)
                    {
                        int rownum = 3;
                        List<Dictionary<string, string>> VitalSignList = new List<Dictionary<string, string>>();

                        VitalSignList = Temp.FindAll(x => x["VS_ITEM"] == "bt" && !string.IsNullOrEmpty(x["VS_RECORD"]));
                        Dt["bt"] = "";
                        dt_check = Get_Check_Abnormal_dt();
                        string color = "black";
                        if (VitalSignList.Count > 0)
                        {
                            for (int i = 0; i < ((VitalSignList.Count < rownum) ? VitalSignList.Count : rownum); i++)
                            {
                                color = "black";
                                string l_check = "btl_e", h_check = "bth_e";
                                string ck_value = VitalSignList[i]["VS_RECORD"], part = VitalSignList[i]["VS_PART"];
                                if (part == "腋溫")
                                {
                                    l_check = "btl_a";
                                    h_check = "bth_a";
                                }
                                else if (part == "肛溫")
                                {
                                    l_check = "btl_r";
                                    h_check = "bth_r";
                                }
                                else if (part == "額溫")
                                {
                                    l_check = "btl_f";
                                    h_check = "bth_f";
                                }
                                string measure = VitalSignList[i]["VS_REASON"];
                                foreach (DataRow r in dt_check.Rows)
                                {
                                    if (measure == "測不到")
                                    {
                                        color = "red";
                                    }
                                    else
                                    {
                                        if (r["MODEL_ID"].ToString() == l_check || r["MODEL_ID"].ToString() == h_check)
                                        {
                                            if (r["DECIDE"].ToString() == ">")
                                            {
                                                if (double.Parse(ck_value) > double.Parse(r["VALUE_LIMIT"].ToString()))
                                                {
                                                    color = "red";
                                                }
                                            }
                                            else if (r["DECIDE"].ToString() == "<")
                                            {
                                                if (double.Parse(ck_value) < double.Parse(r["VALUE_LIMIT"].ToString()))
                                                {
                                                    color = "blue";
                                                }
                                            }
                                        }
                                    }
                                }

                                Dt["bt"] += string.Format("{0}<label class='LittleTitle'>體溫：</label><font color='"+ color + "'>{1}</font> {2}。<br/>"
                                    , Convert.ToDateTime(VitalSignList[i]["CREATE_DATE"]).ToString("yyyy/MM/dd HH:mm")
                                    , (measure == "測不到") ? measure : VitalSignList[i]["VS_RECORD"] + "℃"
                                    , (!string.IsNullOrWhiteSpace(VitalSignList[i]["VS_MEMO"])) ? "，予" + VitalSignList[i]["VS_MEMO"] : "");
                            }
                        }
                        VitalSignList = Temp.FindAll(x => x["VS_ITEM"] == "mp" && !string.IsNullOrEmpty(x["VS_RECORD"]));
                        Dt["mp"] = "";
                        if (VitalSignList.Count > 0)
                        {
                            for (int i = 0; i < ((VitalSignList.Count < rownum) ? VitalSignList.Count : rownum); i++)
                            {
                                color = "black";
                                string ck_value = VitalSignList[i]["VS_RECORD"], part = VitalSignList[i]["VS_PART"];
                                string measure = VitalSignList[i]["VS_REASON"];
                                foreach (DataRow r in dt_check.Rows)
                                {
                                    if (measure == "測不到")
                                    {
                                        color = "red";
                                    }
                                    else
                                    {
                                        if (r["MODEL_ID"].ToString() == "mpl_" + type || r["MODEL_ID"].ToString() == "mph_" + type)
                                        {
                                            if (r["DECIDE"].ToString() == ">")
                                            {
                                                if (double.Parse(ck_value) > double.Parse(r["VALUE_LIMIT"].ToString()))
                                                {
                                                    color = "red";
                                                }
                                            }
                                            else if (r["DECIDE"].ToString() == "<")
                                            {
                                                if (double.Parse(ck_value) < double.Parse(r["VALUE_LIMIT"].ToString()))
                                                {
                                                    color = "blue";
                                                }
                                            }
                                        }
                                    }
                                }
                                Dt["mp"] += string.Format("{0}<label class='LittleTitle'>脈搏：</label><font color='" + color + "'>{1}</font> {2}。<br/>"
                                    , Convert.ToDateTime(VitalSignList[i]["CREATE_DATE"]).ToString("yyyy/MM/dd HH:mm")
                                    , (measure == "測不到") ? measure : VitalSignList[i]["VS_RECORD"] + "次/分"
                                    , (!string.IsNullOrWhiteSpace(VitalSignList[i]["VS_MEMO"])) ? "，予" + VitalSignList[i]["VS_MEMO"] : "");
                            }
                        }
                    VitalSignList = Temp.FindAll(x => x["VS_ITEM"] == "bf" && !string.IsNullOrEmpty(x["VS_RECORD"]));
                    Dt["bf"] = "";
                    if (VitalSignList.Count > 0)
                    {
                        for (int i = 0; i < ((VitalSignList.Count < rownum) ? VitalSignList.Count : rownum); i++)
                        {
                            color = "black";
                            string ck_value = VitalSignList[i]["VS_RECORD"], part = VitalSignList[i]["VS_PART"];
                            string measure = VitalSignList[i]["VS_REASON"];
                            foreach (DataRow r in dt_check.Rows)
                            {
                                if (measure == "測不到")
                                {
                                    color = "red";
                                }
                                else
                                {
                                    if (r["MODEL_ID"].ToString() == "bfl_" + type || r["MODEL_ID"].ToString() == "bfh_" + type)
                                    {
                                        if (r["DECIDE"].ToString() == ">")
                                        {
                                            if (double.Parse(ck_value) > double.Parse(r["VALUE_LIMIT"].ToString()))
                                            {
                                                color = "red";
                                            }
                                        }
                                        else if (r["DECIDE"].ToString() == "<")
                                        {
                                            if (double.Parse(ck_value) < double.Parse(r["VALUE_LIMIT"].ToString()))
                                            {
                                                color = "blue";
                                            }
                                        }
                                    }
                                }
                            }
                            Dt["bf"] += string.Format("{0}<label class='LittleTitle'>呼吸：</label><font color='" + color + "'>{1}</font> {2}。<br/>"
                                , Convert.ToDateTime(VitalSignList[i]["CREATE_DATE"]).ToString("yyyy/MM/dd HH:mm")
                                , (measure == "測不到") ? measure : VitalSignList[i]["VS_RECORD"] + "次/分"
                                , (!string.IsNullOrWhiteSpace(VitalSignList[i]["VS_MEMO"])) ? "，予" + VitalSignList[i]["VS_MEMO"] : "");
                        }
                    }
                    VitalSignList = Temp.FindAll(x => x["VS_ITEM"] == "sp" && !string.IsNullOrEmpty(x["VS_RECORD"]));
                    Dt["sp"] = "";
                    if (VitalSignList.Count > 0)
                    {                       
                        for (int i = 0; i < ((VitalSignList.Count < rownum) ? VitalSignList.Count : rownum); i++)
                        {
                            color = "black";
                            string ck_value = VitalSignList[i]["VS_RECORD"], part = VitalSignList[i]["VS_PART"];
                            string measure = VitalSignList[i]["VS_REASON"];
                            foreach (DataRow r in dt_check.Rows)
                            {
                                if (measure == "測不到")
                                {
                                    color = "red";
                                }
                                else
                                {
                                    if (r["MODEL_ID"].ToString() == "spl" || r["MODEL_ID"].ToString() == "sph")
                                    {
                                        if (r["DECIDE"].ToString() == ">")
                                        {
                                            if (double.Parse(ck_value) > double.Parse(r["VALUE_LIMIT"].ToString()))
                                            {
                                                color = "red";
                                            }
                                        }
                                        else if (r["DECIDE"].ToString() == "<")
                                        {
                                            if (double.Parse(ck_value) < double.Parse(r["VALUE_LIMIT"].ToString()))
                                            {
                                                color = "blue";
                                            }
                                        }
                                    }
                                }
                            }
                            Dt["sp"] += string.Format("{0}<label class='LittleTitle'>SPO2：</label><font color='" + color + "'>{1}</font> {2}。<br/>"
                                , Convert.ToDateTime(VitalSignList[i]["CREATE_DATE"]).ToString("yyyy/MM/dd HH:mm")
                                , (measure == "測不到") ?  measure  : VitalSignList[i]["VS_RECORD"] + "%"
                                , (!string.IsNullOrWhiteSpace(VitalSignList[i]["VS_MEMO"])) ? "，予" + VitalSignList[i]["VS_MEMO"] : "");
                        }
                    }
                    VitalSignList = Temp.FindAll(x => x["VS_ITEM"] == "bp" && !string.IsNullOrEmpty(x["VS_RECORD"].Replace("|","").Replace(" ","")));
                    Dt["bp"] = "";
                    if (VitalSignList.Count > 0)
                    {
                        for (int i = 0; i < ((VitalSignList.Count < rownum) ? VitalSignList.Count : rownum); i++)
                        {
                            string color_h = "black";
                            string color_l = "black";
                            string[] ck_value = VitalSignList[i]["VS_RECORD"].Split('|');
                            string measure = VitalSignList[i]["VS_REASON"];
                            string part = VitalSignList[i]["VS_PART"];
                            string map = "";
                            foreach (DataRow r in dt_check.Rows)
                            {
                                if (measure == "測不到")
                                {
                                    color_h = "red";
                                }
                                else
                                {
                                    if (r["MODEL_ID"].ToString() == "bpls_" + type || r["MODEL_ID"].ToString() == "bphs_" + type)
                                    {
                                        if (r["DECIDE"].ToString() == ">")
                                        {
                                            if (double.Parse(ck_value[0]) > double.Parse(r["VALUE_LIMIT"].ToString()))
                                            {
                                                color_h = "red";
                                            }
                                        }
                                        else if (r["DECIDE"].ToString() == "<")
                                        {
                                            if (double.Parse(ck_value[0]) < double.Parse(r["VALUE_LIMIT"].ToString()))
                                            {
                                                color_h = "blue";
                                            }
                                        }
                                    }
                                    if (r["MODEL_ID"].ToString() == "bpld_" + type || r["MODEL_ID"].ToString() == "bphd_" + type)
                                    {
                                        if (r["DECIDE"].ToString() == ">")
                                        {
                                            if (double.Parse(ck_value[1]) > double.Parse(r["VALUE_LIMIT"].ToString()))
                                            {
                                                color_l = "red";
                                            }
                                        }
                                        else if (r["DECIDE"].ToString() == "<")
                                        {
                                            if (double.Parse(ck_value[1]) < double.Parse(r["VALUE_LIMIT"].ToString()))
                                            {
                                                color_l = "blue";
                                            }
                                        }
                                    }
                                    //平均值
                                    if (ck_value.Length > 2)
                                    {
                                        if (string.IsNullOrEmpty(ck_value[2]))
                                        {
                                            double db = double.Parse(ck_value[0].Replace("#", "")) / 3 + double.Parse(ck_value[1].Replace("#", "")) / 3 * 2;
                                            map = "(" + Math.Round(db, 2).ToString() + ")";
                                        }
                                        else
                                        {
                                            string db = ck_value[2];
                                            map = "(" + db + ")" ;
                                        }
                                    }
                                    else
                                    {
                                        if (ck_value[0] != "" && ck_value[1] != "")
                                        {
                                            double db = double.Parse(ck_value[0].Replace("#", "")) / 3 + double.Parse(ck_value[1].Replace("#", "")) / 3 * 2;
                                            map = "(" + Math.Round(db, 2).ToString() + ")";
                                        }
                                    }
                                }
                            }
                            Dt["bp"] += string.Format("{0}<label class='LittleTitle'>血壓：</label>{1}{2}。<br/>"
                                , Convert.ToDateTime(VitalSignList[i]["CREATE_DATE"]).ToString("yyyy/MM/dd HH:mm")
                                , (measure == "測不到")? "<font color = '" + color_h + "'>" + measure + "</font>" : " <font color = '" + color_h + "'>" + ck_value[0] + "</font> / <font color='" + color_l + "'>" + ck_value[1]+ "</font>" + map + "mmHg"
                                , (!string.IsNullOrWhiteSpace(VitalSignList[i]["VS_MEMO"])) ? "，予" + VitalSignList[i]["VS_MEMO"] : "");
                        }
                    }
                    VitalSignList = Temp.FindAll(x => x["VS_ITEM"] == "bw" && !string.IsNullOrEmpty(x["VS_RECORD"]));
                    Dt["bw"] = "";
                    if (VitalSignList.Count > 0)
                    {
                        for (int i = 0; i < ((VitalSignList.Count < rownum) ? VitalSignList.Count : rownum); i++)
                        {
                            Dt["bw"] += string.Format("{0}<label class='LittleTitle'>體重：</label>{1} {2}。<br/>"
                                , Convert.ToDateTime(VitalSignList[i]["CREATE_DATE"]).ToString("yyyy/MM/dd HH:mm")
                                , VitalSignList[i]["VS_RECORD"].Replace("|", "")
                                , (!string.IsNullOrWhiteSpace(VitalSignList[i]["VS_MEMO"])) ? "，予" + VitalSignList[i]["VS_MEMO"] : "");
                        }
                    }
                    VitalSignList = Temp.FindAll(x => x["VS_ITEM"] == "gtwl" || x["VS_ITEM"] == "gthr" || x["VS_ITEM"] == "gtbu"
                         || x["VS_ITEM"] == "gtbl" || x["VS_ITEM"] == "gthl" || x["VS_ITEM"] == "gtlf" || x["VS_ITEM"] == "gtrf"
                          || x["VS_ITEM"] == "gtlua" || x["VS_ITEM"] == "gtrua" || x["VS_ITEM"] == "gtlt" || x["VS_ITEM"] == "gtrt"
                          || x["VS_ITEM"] == "gtll" || x["VS_ITEM"] == "gtrl" || x["VS_ITEM"] == "gtla" || x["VS_ITEM"] == "gtra" 
                          && !string.IsNullOrEmpty(x["VS_RECORD"]));
                    Dt["Girth"] = "";
                    if (VitalSignList.Count > 0)
                    {
                        for (int i = 0; i < ((VitalSignList.Count < rownum) ? VitalSignList.Count : rownum); i++)
                        {
                            Dt["Girth"] += string.Format("{0}<label class='LittleTitle'>{1}：</label>{2} cm{3}。<br/>"
                                , Convert.ToDateTime(VitalSignList[i]["CREATE_DATE"]).ToString("yyyy/MM/dd HH:mm")
                                , set_name(VitalSignList[i]["VS_ITEM"])
                                , VitalSignList[i]["VS_RECORD"].Replace("|", "，")
                                , (!string.IsNullOrWhiteSpace(VitalSignList[i]["VS_MEMO"])) ? "，予" + VitalSignList[i]["VS_MEMO"] : "");
                        }
                    }
                    VitalSignList = Temp.FindAll(x => x["VS_ITEM"] == "cv1" && !string.IsNullOrEmpty(x["VS_RECORD"]));
                    Dt["CVP"] = "";
                    if (VitalSignList.Count > 0)
                    {
                        for (int i = 0; i < ((VitalSignList.Count < rownum) ? VitalSignList.Count : rownum); i++)
                        {
                            color = "black";
                            string ck_value = VitalSignList[i]["VS_RECORD"], part = VitalSignList[i]["VS_PART"];
                            foreach (DataRow r in dt_check.Rows)
                            {
                                if (r["MODEL_ID"].ToString() == "cv1_h" || r["MODEL_ID"].ToString() == "cv1_l")
                                {
                                    if (r["DECIDE"].ToString() == ">")
                                    {
                                        if (double.Parse(ck_value) > double.Parse(r["VALUE_LIMIT"].ToString()))
                                        {
                                            color = "red";
                                        }
                                    }
                                    else if (r["DECIDE"].ToString() == "<")
                                    {
                                        if (double.Parse(ck_value) < double.Parse(r["VALUE_LIMIT"].ToString()))
                                        {
                                            color = "blue";
                                        }
                                    }
                                }
                            }
                            Dt["CVP"] += string.Format("{0}<label class='LittleTitle'>CVP：</label><font color='" + color + "'>{1}</font> mmHg{2}。<br/>"
                                , Convert.ToDateTime(VitalSignList[i]["CREATE_DATE"]).ToString("yyyy/MM/dd HH:mm")
                                , VitalSignList[i]["VS_RECORD"].Replace("|", "")
                                , (!string.IsNullOrWhiteSpace(VitalSignList[i]["VS_MEMO"])) ? "，予" + VitalSignList[i]["VS_MEMO"] : "");
                        }
                    }
                    VitalSignList = Temp.FindAll(x => x["VS_ITEM"] == "cv2" && !string.IsNullOrEmpty(x["VS_RECORD"]));
                    if (VitalSignList.Count > 0)
                    {
                        for (int i = 0; i < ((VitalSignList.Count < rownum) ? VitalSignList.Count : rownum); i++)
                        {
                            color = "black";
                            string ck_value = VitalSignList[i]["VS_RECORD"], part = VitalSignList[i]["VS_PART"];
                            foreach (DataRow r in dt_check.Rows)
                            {
                                if (r["MODEL_ID"].ToString() == "cv2_h" || r["MODEL_ID"].ToString() == "cv2_l")
                                {
                                    if (r["DECIDE"].ToString() == ">")
                                    {
                                        if (double.Parse(ck_value) > double.Parse(r["VALUE_LIMIT"].ToString()))
                                        {
                                            color = "red";
                                        }
                                    }
                                    else if (r["DECIDE"].ToString() == "<")
                                    {
                                        if (double.Parse(ck_value) < double.Parse(r["VALUE_LIMIT"].ToString()))
                                        {
                                            color = "blue";
                                        }
                                    }
                                }
                            }
                            Dt["CVP"] += string.Format("{0}<label class='LittleTitle'>CVP：</label><font color='" + color + "'>{1}</font> cmH2O{2}。<br/>"
                                , Convert.ToDateTime(VitalSignList[i]["CREATE_DATE"]).ToString("yyyy/MM/dd HH:mm")
                                , VitalSignList[i]["VS_RECORD"].Replace("|", "")
                                , (!string.IsNullOrWhiteSpace(VitalSignList[i]["VS_MEMO"])) ? "，予" + VitalSignList[i]["VS_MEMO"] : "");
                        }
                    }
                    VitalSignList = Temp.FindAll(x => x["VS_ITEM"] == "ic1" && !string.IsNullOrEmpty(x["VS_RECORD"]));
                    Dt["ICP"] = "";
                    if (VitalSignList.Count > 0)
                    {
                        for (int i = 0; i < ((VitalSignList.Count < rownum) ? VitalSignList.Count : rownum); i++)
                        {
                            color = "black";
                            string ck_value = VitalSignList[i]["VS_RECORD"], part = VitalSignList[i]["VS_PART"];
                            foreach (DataRow r in dt_check.Rows)
                            {
                                if (r["MODEL_ID"].ToString() == "ic1_l")
                                {
                                    if (r["DECIDE"].ToString() == ">")
                                    {
                                        if (double.Parse(ck_value) > double.Parse(r["VALUE_LIMIT"].ToString()))
                                        {
                                            color = "red";
                                        }
                                    }
                                    else if (r["DECIDE"].ToString() == "<")
                                    {
                                        if (double.Parse(ck_value) < double.Parse(r["VALUE_LIMIT"].ToString()))
                                        {
                                            color = "blue";
                                        }
                                    }
                                }
                            }
                                Dt["ICP"] += string.Format("{0}<label class='LittleTitle'>ICP：</label><font color='" + color + "'>{1}</font> mmHg{2}。<br/>"
                                , Convert.ToDateTime(VitalSignList[i]["CREATE_DATE"]).ToString("yyyy/MM/dd HH:mm")
                                , VitalSignList[i]["VS_RECORD"].Replace("|", "")
                                , (!string.IsNullOrWhiteSpace(VitalSignList[i]["VS_MEMO"])) ? "，予" + VitalSignList[i]["VS_MEMO"] : "");
                        }
                    }
                    VitalSignList = Temp.FindAll(x => x["VS_ITEM"] == "ic2" && !string.IsNullOrEmpty(x["VS_RECORD"]));
                    if (VitalSignList.Count > 0)
                    {
                        for (int i = 0; i < ((VitalSignList.Count < rownum) ? VitalSignList.Count : rownum); i++)
                        {
                                color = "black";
                                string ck_value = VitalSignList[i]["VS_RECORD"], part = VitalSignList[i]["VS_PART"];
                            foreach (DataRow r in dt_check.Rows)
                            {
                                if (r["MODEL_ID"].ToString() == "ic2_l")
                                {
                                    if (r["DECIDE"].ToString() == ">")
                                    {
                                        if (double.Parse(ck_value) > double.Parse(r["VALUE_LIMIT"].ToString()))
                                        {
                                            color = "red";
                                        }
                                    }
                                    else if (r["DECIDE"].ToString() == "<")
                                    {
                                        if (double.Parse(ck_value) < double.Parse(r["VALUE_LIMIT"].ToString()))
                                        {
                                            color = "blue";
                                        }
                                    }
                                }
                            }
                                    Dt["ICP"] += string.Format("{0}<label class='LittleTitle'>ICP：</label><font color='" + color + "'>{1}</font> cmH2O{2}。<br/>"
                                , Convert.ToDateTime(VitalSignList[i]["CREATE_DATE"]).ToString("yyyy/MM/dd HH:mm")
                                , VitalSignList[i]["VS_RECORD"].Replace("|", "")
                                , (!string.IsNullOrWhiteSpace(VitalSignList[i]["VS_MEMO"])) ? "，予" + VitalSignList[i]["VS_MEMO"] : "");
                        }
                    }
                    VitalSignList = Temp.FindAll(x => x["VS_ITEM"] == "gc");
                    Dt["EVM"] = "";
                    if (VitalSignList.Count > 0)
                    {
                        for (int i = 0; i < ((VitalSignList.Count < rownum) ? VitalSignList.Count : rownum); i++)
                        {
                            ListWord = VitalSignList[i]["VS_RECORD"].Split('|');
                            if (ListWord.Length > 0)
                            {
                                if (!(string.IsNullOrEmpty(ListWord[0]) && string.IsNullOrEmpty(ListWord[1]) && string.IsNullOrEmpty(ListWord[2])))
                                {                               
                                Dt["EVM"] += string.Format("{0}<label class='LittleTitle'>GCS：</label>E{1} V{2} M{3} {4}。<br/>"
                                    , Convert.ToDateTime(VitalSignList[i]["CREATE_DATE"]).ToString("yyyy/MM/dd HH:mm")
                                    , error_str(ListWord[0], "GCS-E" ,ref alertstr,"GCS")
                                    , error_str(ListWord[1], "GCS-V", ref alertstr, "GCS")
                                    , error_str(ListWord[2], "GCS-M", ref alertstr, "GCS")
                                    , (!string.IsNullOrWhiteSpace(VitalSignList[i]["VS_MEMO"])) ? "，予" + VitalSignList[i]["VS_MEMO"] : "");
                            }
                            }
                        }
                    }
                    VitalSignList = Temp.FindAll(x => x["VS_ITEM"] == "pupils");
                    Dt["Pupils"] = "";
                    if (VitalSignList.Count > 0)
                    {
                        for (int i = 0; i < ((VitalSignList.Count < rownum) ? VitalSignList.Count : rownum); i++)
                        {
                            ListWord = VitalSignList[i]["VS_RECORD"].Split('|');
                            if (ListWord.Length > 0)
                            {
                                if (!(string.IsNullOrEmpty(ListWord[0]) && string.IsNullOrEmpty(ListWord[1]) && string.IsNullOrEmpty(ListWord[2]) && string.IsNullOrEmpty(ListWord[3])))
                                {
                                    Dt["Pupils"] += string.Format("{0}<label class='LittleTitle'>左：</label>{1} ({2}) / <label class='LittleTitle'>右：</label>{3} ({4}){5}。<br/>"
                                    , Convert.ToDateTime(VitalSignList[i]["CREATE_DATE"]).ToString("yyyy/MM/dd HH:mm")
                                    , (ListWord[0].Contains("C")) ? "" : ListWord[1]
                                    , ListWord[0]
                                    , (ListWord[2].Contains("C")) ? "" : ListWord[3]
                                    , ListWord[2]
                                    , (!string.IsNullOrWhiteSpace(VitalSignList[i]["VS_MEMO"])) ? "，予" + VitalSignList[i]["VS_MEMO"] : "");
                                }
                            }
                        }
                    }
                    VitalSignList = Temp.FindAll(x => x["VS_ITEM"] == "msPower");
                    Dt["MusclePower"] = "";
                    if (VitalSignList.Count > 0)
                    {
                        for (int i = 0; i < ((VitalSignList.Count < rownum) ? VitalSignList.Count : rownum); i++)
                        {
                            ListWord = VitalSignList[i]["VS_RECORD"].Split('|');
                            if (ListWord.Length > 0)
                            {
                                Dt["MusclePower"] += string.Format("{0}<label class='LittleTitle'>左上肢：</label>{1}，<label class='LittleTitle'>右上肢：</label>{2}<label class='LittleTitle'>左下肢：</label>{3}，<label class='LittleTitle'>右下肢：</label>{4}{5}。<br/>"
                                    , Convert.ToDateTime(VitalSignList[i]["CREATE_DATE"]).ToString("yyyy/MM/dd HH:mm")
                                    , ListWord[0]
                                    , ListWord[1]
                                    , ListWord[2]
                                    , ListWord[3]
                                    , (!string.IsNullOrWhiteSpace(VitalSignList[i]["VS_MEMO"])) ? "，予" + VitalSignList[i]["VS_MEMO"] : "");
                            }
                        }
                    }
                    VitalSignList = Temp.FindAll(x => x["VS_ITEM"] == "eat");
                    if (VitalSignList.Count > 0)
                    {
                        Dt["eat"] = string.Format("{0} {1}"
                            , Convert.ToDateTime(VitalSignList[0]["CREATE_DATE"]).ToString("yyyy/MM/dd HH:mm")
                            , VitalSignList[0]["VS_RECORD"].Replace("|", ""));
                    }
                }

                #endregion

                    #region 傷口

                    //傷口
                    sql = "SELECT C.\"TYPE\", C.\"POSITION\", C.WOUND_ID, C.CREATTIME, C.ENDTIME, C.ENDT_REASON "
                    + ", D.RECORD_ID, D.RECORDTIME, D.RANGE_HEIGHT, D.RANGE_WIDTH, D.RANGE_DEPTH, D.EXTERIOR, D.EXUDATE_NATURE, D.EXUDATE_COLOR, D.HANDLE_ITEM, D.HANDLE_OTHER, D.GRADE "
                    + "FROM WOUND_DATA C LEFT JOIN ( "
                    + "SELECT A.* FROM WOUND_RECORD A "
                    + "INNER JOIN ( "
                    + "SELECT DISTINCT(WOUND_ID), MAX(RECORDTIME) AS RECORDTIME FROM WOUND_RECORD "
                    + "WHERE WOUND_ID IN ( "
                    + "SELECT WOUND_ID FROM WOUND_DATA WHERE FEENO = '" + feeno + "' "
                    + "AND DELETED IS NULL "
                    + ") AND DELETED IS NULL "
                    + "GROUP BY WOUND_ID "
                    + ") B "
                    + "ON A.WOUND_ID = B.WOUND_ID AND A.RECORDTIME = B.RECORDTIME "
                    + ") D ON C.WOUND_ID = D.WOUND_ID WHERE C.FEENO = '" + feeno + "' "
                    + "AND C.DELETED IS NULL ";

                    Dt2 = link.DBExecSQL(sql);
                    if (Dt2.Rows.Count > 0)
                    {
                        string Wound_Press = string.Empty;
                        Temp_String = string.Empty;
                        for (int i = 0; i < Dt2.Rows.Count; i++)
                        {
                            if (!string.IsNullOrWhiteSpace(Dt2.Rows[i]["RECORD_ID"].ToString()))
                            {
                                //if (Dt2.Rows[i]["TYPE"].ToString().Equals("壓瘡"))
                                string WoundType = Dt2.Rows[i]["TYPE"].ToString();

                                if (WoundType == "壓瘡" || WoundType == "壓傷")
                                {
                                    if (string.IsNullOrWhiteSpace(Dt2.Rows[i]["ENDTIME"].ToString()))
                                    {
                                        Wound_Press += string.Format("{0}<label class='LittleTitle'>發生日期</label>：{1} {2} {3}級壓傷傷口存，"
                                                + "<label class='LittleTitle'>大小：</label>{4} cm*{5} cm*{6} cm，"
                                                + "<label class='LittleTitle'>外觀：</label>{7}，"
                                                + "{8}{9}"
                                                + "<label class='LittleTitle'>處置：</label>{10}<hr/>"
                                               , Convert.ToDateTime(Dt2.Rows[i]["RECORDTIME"].ToString()).ToString("yyyy/MM/dd HH:mm")
                                           , (!string.IsNullOrWhiteSpace(Dt2.Rows[i]["CREATTIME"].ToString())) ? Convert.ToDateTime(Dt2.Rows[i]["CREATTIME"].ToString()).ToString("yyyy/MM/dd") : "不詳"
                                               , Dt2.Rows[i]["POSITION"].ToString()
                                               , Dt2.Rows[i]["GRADE"].ToString()
                                               , (!string.IsNullOrWhiteSpace(Dt2.Rows[i]["RANGE_HEIGHT"].ToString())) ? Dt2.Rows[i]["RANGE_HEIGHT"].ToString() : ""
                                               , (!string.IsNullOrWhiteSpace(Dt2.Rows[i]["RANGE_WIDTH"].ToString())) ? Dt2.Rows[i]["RANGE_WIDTH"].ToString() : ""
                                               , (!string.IsNullOrWhiteSpace(Dt2.Rows[i]["RANGE_DEPTH"].ToString())) ? Dt2.Rows[i]["RANGE_DEPTH"].ToString() : ""
                                               , Dt2.Rows[i]["EXTERIOR"].ToString().Replace("|", "、")
                                               , (!string.IsNullOrWhiteSpace(Dt2.Rows[i]["EXUDATE_NATURE"].ToString())) ? "<label class='LittleTitle'>滲出液性狀：</label>" + Dt2.Rows[i]["EXUDATE_NATURE"].ToString().Replace("|", "、") + "，" : ""
                                               , (!string.IsNullOrWhiteSpace(Dt2.Rows[i]["EXUDATE_COLOR"].ToString())) ? "<label class='LittleTitle'>顏色：</label>" + Dt2.Rows[i]["EXUDATE_COLOR"].ToString().Replace("|", "、") + "，" : ""
                                               , Dt2.Rows[i]["HANDLE_ITEM"].ToString().Replace("|", "、").Replace("其他", "") + ((!string.IsNullOrWhiteSpace(Dt2.Rows[i]["HANDLE_OTHER"].ToString())) ? "、" + Dt2.Rows[i]["HANDLE_OTHER"].ToString() : ""));
                                    }
                                    else if (Convert.ToDateTime(Dt2.Rows[i]["ENDTIME"].ToString()).ToString("yyyy/MM/dd") == DateTime.Now.ToString("yyyy/MM/dd"))
                                    {
                                        Wound_Press += string.Format("{0}<label class='LittleTitle'>發生日期</label>：{1} {2} {3}級壓傷傷口於 {4} 因 {5} 予結案。<hr/>"
                                           , Convert.ToDateTime(Dt2.Rows[i]["RECORDTIME"].ToString()).ToString("yyyy/MM/dd HH:mm")
                                           , (!string.IsNullOrWhiteSpace(Dt2.Rows[i]["CREATTIME"].ToString())) ? Convert.ToDateTime(Dt2.Rows[i]["CREATTIME"].ToString()).ToString("yyyy/MM/dd") : "不詳"
                                           , Dt2.Rows[i]["POSITION"].ToString()
                                           , Dt2.Rows[i]["GRADE"].ToString()
                                           , Convert.ToDateTime(Dt2.Rows[i]["ENDTIME"].ToString()).ToString("yyyy/MM/dd HH:mm")
                                           , Dt2.Rows[i]["ENDT_REASON"].ToString());
                                    }
                                }
                                else
                                {
                                    if (string.IsNullOrWhiteSpace(Dt2.Rows[i]["ENDTIME"].ToString()))
                                    {
                                        Temp_String += string.Format("{0}<label class='LittleTitle'>發生日期</label>：{1}，"
                                            + "<label class='LittleTitle'>部位：</label>{2}，"
                                            + "<label class='LittleTitle'>傷口種類：</label>{3}，"
                                            + "<label class='LittleTitle'>大小：</label>{4} cm*{5} cm*{6} cm，"
                                            + "<label class='LittleTitle'>外觀：</label>{7}，"
                                            + "{8}{9}"
                                            + "<label class='LittleTitle'>處置：</label>{10}<hr/>"
                                           , Convert.ToDateTime(Dt2.Rows[i]["RECORDTIME"].ToString()).ToString("yyyy/MM/dd HH:mm")
                                           , (!string.IsNullOrWhiteSpace(Dt2.Rows[i]["CREATTIME"].ToString())) ? Convert.ToDateTime(Dt2.Rows[i]["CREATTIME"].ToString()).ToString("yyyy/MM/dd") : "不詳"
                                           , Dt2.Rows[i]["POSITION"].ToString()
                                           , Dt2.Rows[i]["TYPE"].ToString()
                                           , (!string.IsNullOrWhiteSpace(Dt2.Rows[i]["RANGE_HEIGHT"].ToString())) ? Dt2.Rows[i]["RANGE_HEIGHT"].ToString() : ""
                                           , (!string.IsNullOrWhiteSpace(Dt2.Rows[i]["RANGE_WIDTH"].ToString())) ? Dt2.Rows[i]["RANGE_WIDTH"].ToString() : ""
                                           , (!string.IsNullOrWhiteSpace(Dt2.Rows[i]["RANGE_DEPTH"].ToString())) ? Dt2.Rows[i]["RANGE_DEPTH"].ToString() : ""
                                           , Dt2.Rows[i]["EXTERIOR"].ToString().Replace("|", "、")
                                           , (!string.IsNullOrWhiteSpace(Dt2.Rows[i]["EXUDATE_NATURE"].ToString())) ? "<label class='LittleTitle'>滲出液性狀：</label>" + Dt2.Rows[i]["EXUDATE_NATURE"].ToString().Replace("|", "、") + "，" : ""
                                           , (!string.IsNullOrWhiteSpace(Dt2.Rows[i]["EXUDATE_COLOR"].ToString())) ? "<label class='LittleTitle'>顏色：</label>" + Dt2.Rows[i]["EXUDATE_COLOR"].ToString().Replace("|", "、") + "，" : ""
                                           , Dt2.Rows[i]["HANDLE_ITEM"].ToString().Replace("|", "、").Replace("其他", "") + ((!string.IsNullOrWhiteSpace(Dt2.Rows[i]["HANDLE_OTHER"].ToString())) ? "、" + Dt2.Rows[i]["HANDLE_OTHER"].ToString() : ""));
                                    }
                                    else if (Convert.ToDateTime(Dt2.Rows[i]["ENDTIME"].ToString()).ToString("yyyy/MM/dd") == DateTime.Now.ToString("yyyy/MM/dd"))
                                    {
                                        Temp_String += string.Format("{0}<label class='LittleTitle'>發生日期</label>：{1} {2} {3} 於 {4} 因 {5} 予結案。<hr/>"
                                           , Convert.ToDateTime(Dt2.Rows[i]["RECORDTIME"].ToString()).ToString("yyyy/MM/dd HH:mm")
                                           , (!string.IsNullOrWhiteSpace(Dt2.Rows[i]["CREATTIME"].ToString())) ? Convert.ToDateTime(Dt2.Rows[i]["CREATTIME"].ToString()).ToString("yyyy/MM/dd") : "不詳"
                                           , Dt2.Rows[i]["POSITION"].ToString()
                                           , Dt2.Rows[i]["TYPE"].ToString()
                                           , Convert.ToDateTime(Dt2.Rows[i]["ENDTIME"].ToString()).ToString("yyyy/MM/dd HH:mm")
                                           , Dt2.Rows[i]["ENDT_REASON"].ToString());
                                    }
                                }
                            }
                        }
                        Dt["Wound_Press"] = (!string.IsNullOrWhiteSpace(Wound_Press)) ? Wound_Press.Substring(0, Wound_Press.Length - 5) : Wound_Press;
                        if (Temp_String.Length > 5)
                            Dt["Wound"] = (!string.IsNullOrWhiteSpace(Temp_String)) ? Temp_String.Substring(0, Temp_String.Length - 5) : Temp_String;
                    }
                   

                        #endregion

                    #region 疼痛

                    //疼痛
                    sql = "SELECT C.PAIN_POSITION, C.PAIN_CODE, C.END_DATE, C.END_REASON "
                    + ", D.PAIN_RECORD_CODE, D.RECORD_DATE, D.RECORD_JSON_DATA "
                    + "FROM NIS_DATA_PAIN_POSITION C LEFT JOIN ( "
                    + "SELECT A.* FROM NIS_DATA_PAIN_RECORD A "
                    + "INNER JOIN ( "
                    + "SELECT DISTINCT(PAIN_CODE), MAX(RECORD_DATE) AS RECORD_DATE FROM NIS_DATA_PAIN_RECORD "
                    + "WHERE PAIN_CODE IN ( "
                    + "SELECT PAIN_CODE FROM NIS_DATA_PAIN_POSITION WHERE FEE_NO = '" + feeno + "' "
                    + ") GROUP BY PAIN_CODE "
                    + ") B "
                    + "ON A.PAIN_CODE = B.PAIN_CODE AND A.RECORD_DATE = B.RECORD_DATE "
                    + ") D ON C.PAIN_CODE = D.PAIN_CODE WHERE C.FEE_NO = '" + feeno + "' ORDER BY C.RECORD_DATE DESC ";

                    Dt2 = link.DBExecSQL(sql);
                    if (Dt2.Rows.Count > 0)
                    {
                        Temp_String = string.Empty;
                        int PainLevel = 0;
                        for (int i = 0; i < Dt2.Rows.Count; i++)
                        {
                            if (!string.IsNullOrWhiteSpace(Dt2.Rows[i]["PAIN_RECORD_CODE"].ToString()))
                            {
                                PainLevel = 0;
                                Temp = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(Dt2.Rows[i]["RECORD_JSON_DATA"].ToString());
                                if (Temp.Count > 0)
                                {
                                    if (string.IsNullOrWhiteSpace(Dt2.Rows[i]["END_DATE"].ToString()))
                                    {
                                        if (Temp.Find(x => x["Name"] == "PainLevel")["Value"].Equals("數字量表"))
                                            PainLevel = int.Parse(Temp.Find(x => x["Name"] == "PainLevel_Number")["Value"].Substring(1, 1));
                                        else if (Temp.Find(x => x["Name"] == "PainLevel")["Value"].Equals("臉譜量表"))
                                            PainLevel = int.Parse(Temp.Find(x => x["Name"] == "PainLevel_Face")["Value"].Substring(1, 1));
                                        else
                                        {
                                            switch (Temp.Find(x => x["Name"] == "PainLevel")["Value"])
                                            {
                                                case "困難評估(成人)":
                                                    PainLevel += int.Parse(Temp.Find(x => x["Name"] == "PainLevel_Adult_Breath")["Value"].Substring(1, 1));
                                                    PainLevel += int.Parse(Temp.Find(x => x["Name"] == "PainLevel_Adult_NoLG")["Value"].Substring(1, 1));
                                                    PainLevel += int.Parse(Temp.Find(x => x["Name"] == "PainLevel_Adult_Face")["Value"].Substring(1, 1));
                                                    PainLevel += int.Parse(Temp.Find(x => x["Name"] == "PainLevel_Adult_Body")["Value"].Substring(1, 1));
                                                    PainLevel += int.Parse(Temp.Find(x => x["Name"] == "PainLevel_Adult_Appease")["Value"].Substring(1, 1));
                                                    break;
                                                case "困難評估(兒童)":
                                                    PainLevel += int.Parse(Temp.Find(x => x["Name"] == "PainLevel_Child_Face")["Value"].Substring(1, 1));
                                                    PainLevel += int.Parse(Temp.Find(x => x["Name"] == "PainLevel_Child_Legs")["Value"].Substring(1, 1));
                                                    PainLevel += int.Parse(Temp.Find(x => x["Name"] == "PainLevel_Child_Activity")["Value"].Substring(1, 1));
                                                    PainLevel += int.Parse(Temp.Find(x => x["Name"] == "PainLevel_Child_Crying")["Value"].Substring(1, 1));
                                                    PainLevel += int.Parse(Temp.Find(x => x["Name"] == "PainLevel_Child_Consolability")["Value"].Substring(1, 1));
                                                    break;
                                                case "困難評估(新生兒)":
                                                    PainLevel += int.Parse(Temp.Find(x => x["Name"] == "PainLevel_Baby_Crying")["Value"].Substring(1, 1));
                                                    PainLevel += int.Parse(Temp.Find(x => x["Name"] == "PainLevel_Baby_FiO2")["Value"].Substring(1, 1));
                                                    PainLevel += int.Parse(Temp.Find(x => x["Name"] == "PainLevel_Baby_VT")["Value"].Substring(1, 1));
                                                    PainLevel += int.Parse(Temp.Find(x => x["Name"] == "PainLevel_Baby_Expression")["Value"].Substring(1, 1));
                                                    PainLevel += int.Parse(Temp.Find(x => x["Name"] == "PainLevel_Baby_Sleepless")["Value"].Substring(1, 1));
                                                    break;
                                                case "CPOT評估(加護單位)":
                                                    PainLevel += int.Parse(Temp.Find(x => x["Name"] == "PainLevel_CPOT_Face")["Value"].Substring(1, 1));
                                                    PainLevel += int.Parse(Temp.Find(x => x["Name"] == "PainLevel_CPOT_Body")["Value"].Substring(1, 1));
                                                    PainLevel += int.Parse(Temp.Find(x => x["Name"] == "PainLevel_CPOT_Muscle")["Value"].Substring(1, 1));
                                                    PainLevel += int.Parse(Temp.Find(x => x["Name"] == "PainLevel_CPOT_Breath")["Value"].Substring(1, 1));
                                                    break;
                                            }
                                        }
                                        Temp_String += Convert.ToDateTime(Dt2.Rows[i]["RECORD_DATE"].ToString()).ToString("yyyy/MM/dd HH:mm")
                                            + "<label class='LittleTitle'>疼痛部位：</label>" + Dt2.Rows[i]["PAIN_POSITION"].ToString()
                                            + "，<label class='LittleTitle'>性質：</label>";

                                        Temp_String += Temp.Find(x => x["Name"] == "PainNature")["Value"];
                                        if (Temp.Find(x => x["Name"] == "PainNature")["Value"].Equals("其他"))
                                        {
                                            if (Temp.Exists(x => x["Name"] == "PainNature_Other"))
                                            {
                                                if (Temp_String.Length > 2)
                                                    Temp_String = Temp_String.Substring(0, Temp_String.Length - 2) + Temp.Find(x => x["Name"] == "PainNature_Other")["Value"];
                                            }
                                        }
                                        Temp_String += "，<label class='LittleTitle'>強度：</label>" + PainLevel.ToString() + "分"
                                            + "，<label class='LittleTitle'>";
                                        if (Temp.Exists(x => x["Name"] == "PainAbnormalItem"))
                                        {
                                            Temp_String += "處置：</label>" + Temp.Find(x => x["Name"] == "PainAbnormalItem")["Value"];
                                        }
                                        if (Temp.Exists(x => x["Name"] == "RelievePainMedItem"))
                                        {
                                            Temp_String += "，<label class='LittleTitle'>止痛藥：</label>";
                                            foreach (string MedID in Temp.Find(x => x["Name"] == "RelievePainMedItem")["Value"].Split(','))
                                            {
                                                if (MedID != null && MedID != "")
                                                {
                                                    Temp_String += Temp.Find(x => x["Name"] == "RelievePainMedItem_Name_" + MedID)["Value"] + " ";
                                                    Temp_String += Temp.Find(x => x["Name"] == "RelievePainMedItem_Amount_" + MedID)["Value"] + " ";
                                                    Temp_String += Temp.Find(x => x["Name"] == "RelievePainMedItem_Way_" + MedID)["Value"] + "、";
                                                }
                                            }
                                            if (Temp_String.Length > 1)
                                                Temp_String = Temp_String.Substring(0, Temp_String.Length - 1);
                                        }
                                        if (Temp.Exists(x => x["Name"] == "RelievePainSideEffectItem"))
                                        {
                                            Temp_String += "，<label class='LittleTitle'>副作用：</label>" + Temp.Find(x => x["Name"] == "RelievePainSideEffectItem")["Value"];
                                            if (Temp.Exists(x => x["Name"] == "RelievePainSideEffectItem_Other"))
                                            {
                                                if (Temp_String.Length > 2)
                                                    Temp_String = Temp_String.Substring(0, Temp_String.Length - 2) + Temp.Find(x => x["Name"] == "RelievePainSideEffectItem_Other")["Value"];
                                            }
                                        }
                                        Temp_String += "<hr/>";
                                    }
                                    else
                                    {
                                        Temp_String += Convert.ToDateTime(Dt2.Rows[i]["RECORD_DATE"].ToString()).ToString("yyyy/MM/dd HH:mm")
                                            + " " + Dt2.Rows[i]["PAIN_POSITION"].ToString() + " " + Temp.Find(x => x["Name"] == "PainNature")["Value"]
                                            + " 問題於 " + Convert.ToDateTime(Dt2.Rows[i]["END_DATE"].ToString()).ToString("yyyy/MM/dd HH:mm") + "結案。";
                                        Temp_String += "<hr/>";
                                    }
                                }
                            }
                        }
                        if (!string.IsNullOrWhiteSpace(Temp_String))
                        {
                            if (Temp_String.Length > 5)
                                Dt["PainRecord"] = Temp_String.Substring(0, Temp_String.Length - 5);
                        }
                            
                    }
                    
                    #endregion

                    #region 血糖

                    //血糖
                    sql = "SELECT * FROM BLOODSUGAR_SYMPTOMS ";
                    Temp = new List<Dictionary<string, string>>();

                    Dt2 = link.DBExecSQL(sql);
                    if (Dt2.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt2.Rows.Count; i++)
                        {
                            temp = new Dictionary<string, string>();
                            temp["Name"] = Dt2.Rows[i]["VALUE"].ToString();
                            temp["Value"] = Dt2.Rows[i]["ITEM"].ToString();
                            Temp.Add(temp);
                        }
                    }

                    sql = " SELECT * FROM (select * from BLOODSUGAR WHERE FEENO = '" + feeno + "' order by INDATE DESC ) "
                    + "WHERE(STATUS <> 'del' OR STATUS IS NULL) AND BLOODSUGAR IS NOT NULL "
                    + "AND ROWNUM <= 3 ";

                    dt_check = Get_Check_Abnormal_dt();
                    
                    Dt2 = link.DBExecSQL(sql);
                    if (Dt2.Rows.Count > 0)
                    {
                        Temp_String = string.Empty;
                        string DealWith = string.Empty, Symptom = string.Empty;
                        int Blood = 0;
                        for (int i = 0; i < Dt2.Rows.Count; i++)
                        {
                            string color = "black";
                            //string l_check = "bsl", h_check = "bsh", low_check= "bsl_low", high_check= "bsh_high";
                            Blood = int.Parse(Dt2.Rows[i]["BLOODSUGAR"].ToString());
                            string blood = "";
                            foreach (DataRow r in dt_check.Rows)
                            {
                                if (r["MODEL_ID"].ToString() == "bsl" || r["MODEL_ID"].ToString() == "bsh")
                                {
                                    blood = Blood.ToString() + " mg/dl";
                                    if (r["DECIDE"].ToString() == ">")
                                    {
                                        if (Blood > double.Parse(r["VALUE_LIMIT"].ToString()))
                                        {
                                            color = "red";
                                        }
                                    }
                                    else if (r["DECIDE"].ToString() == ">=")
                                    {
                                        if (Blood >= double.Parse(r["VALUE_LIMIT"].ToString()))
                                        {
                                            color = "red";
                                        }
                                    }
                                    else if (r["DECIDE"].ToString() == "<")
                                    {
                                        if (Blood < double.Parse(r["VALUE_LIMIT"].ToString()))
                                        {
                                            color = "blue";
                                        }
                                    }
                                    else if (r["DECIDE"].ToString() == "<=")
                                    {
                                        if (Blood <= double.Parse(r["VALUE_LIMIT"].ToString()))
                                        {
                                            color = "blue";
                                        }
                                    }
                                }else if (r["MODEL_ID"].ToString() == "bsl_low" || r["MODEL_ID"].ToString() == "bsh_high")
                                {
                                    if (r["DECIDE"].ToString() == ">")
                                    {
                                        if (Blood > double.Parse(r["VALUE_LIMIT"].ToString()))
                                        {
                                            blood = "High";
                                        }
                                    }
                                    else if (r["DECIDE"].ToString() == ">=")
                                    {
                                        if (Blood >= double.Parse(r["VALUE_LIMIT"].ToString()))
                                        {
                                            blood = "High";
                                        }
                                    }
                                    else if (r["DECIDE"].ToString() == "<")
                                    {
                                        if (Blood < double.Parse(r["VALUE_LIMIT"].ToString()))
                                        {
                                            blood = "Low";
                                        }
                                    }
                                    else if (r["DECIDE"].ToString() == "<=")
                                    {
                                        if (Blood <= double.Parse(r["VALUE_LIMIT"].ToString()))
                                        {
                                            blood = "Low";
                                        }
                                    }
                                }
                            }
                            DealWith = string.Empty;
                            Symptom = string.Empty;
                            if (!string.IsNullOrWhiteSpace(Dt2.Rows[i]["DEALWITH"].ToString()))
                            {
                                ListWord = Dt2.Rows[i]["DEALWITH"].ToString().Split(',');
                                foreach (string Deal in ListWord)
                                {
                                    DealWith += Temp.Find(x => x["Name"] == Deal)["Value"] + ",";
                                }
                                DealWith = DealWith.Substring(0, DealWith.Length - 1);
                            }
                            if (!string.IsNullOrWhiteSpace(Dt2.Rows[i]["SYMPTOM"].ToString()))
                            {
                                ListWord = Dt2.Rows[i]["SYMPTOM"].ToString().Split(',');
                                foreach (string Deal in ListWord)
                                {
                                    Symptom += Temp.Find(x => x["Name"] == Deal)["Value"] + ",";
                                }
                                Symptom = Symptom.Substring(0, Symptom.Length - 1);
                            }
                            Temp_String += string.Format("{0} 測量種類： {1}，<label class='LittleTitle'>血糖值：</label><font color='" + color + "'>{2}</font>{3}{4}{5}。<hr/>"
                                , Dt2.Rows[i]["INDATE"].ToString()
                                , Dt2.Rows[i]["MEAL_STATUS"].ToString().Replace("C", "不清楚").Replace("B", "飯前").Replace("A", "飯後")
                                , blood
                                , (!string.IsNullOrWhiteSpace(Symptom)) ? "，<label class='LittleTitle'>症狀：</label>" + Symptom : ""
                                , (!string.IsNullOrWhiteSpace(DealWith)) ? "，<label class='LittleTitle'>處置：</label>" + DealWith : ""
                                , (!string.IsNullOrWhiteSpace(Dt2.Rows[i]["NOTE"].ToString())) ? "，<label class='LittleTitle'>註記：</label>" + Dt2.Rows[i]["NOTE"].ToString() : "");
                        }
                        if (!string.IsNullOrWhiteSpace(Temp_String))
                        {
                            if (Temp_String.Length > 5)
                                Dt["BloodSugar"] = Temp_String.Substring(0, Temp_String.Length - 5);
                        }
                            
                    }
                    

                    #endregion

                    #region 攝入/輸出 (包含當班  往前三個班)

                    DateTime nowDate = DateTime.Now;
                    string EName = "", DName = "", NName = "";
                    //攝入/輸出
                    sql = "SELECT IO.*, CASE WHEN AMOUNT_UNIT <> '3' THEN AMOUNT ELSE AMOUNT*0.001 END AMOUNT_ALL "
                    + ",(SELECT P_GROUP FROM SYS_PARAMS WHERE P_MODEL = 'iotype' AND P_VALUE = IO.TYPEID)P_GROUP "
                    + ",(SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'iotype' AND P_VALUE = IO.TYPEID)P_NAME "
                    + ",(SELECT NAME FROM IO_ITEM WHERE IO_ITEM.ITEMID = IO.ITEMID)NAME "
                    + "FROM IO_DATA IO "
                    + "WHERE FEENO = '" + feeno + "' ";

                    string sql_Catheter = "SELECT RECORD_TIME, RECORD_CLASS, AMOUNT, COLOROTHER, BIT_SURPLUS "
                    + ",(SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'catheter_flush' AND P_GROUP = 'urine_color' AND P_VALUE = COLORID)COLORID "
                    + "FROM FLUSH_CATHETER_DATA "
                    + "WHERE FEENO = '" + feeno + "' ";

                    if (int.Parse(nowDate.ToString("HHmm")) <= 700)
                    {
                        sql += "AND CREATTIME BETWEEN to_date('" + Convert.ToDateTime(nowDate.AddDays(-1).ToString("yyyy/MM/dd 07:01:00")).ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/mm/dd hh24:mi:ss') ";
                        sql_Catheter += "AND RECORD_TIME BETWEEN to_date('" + Convert.ToDateTime(nowDate.AddDays(-1).ToString("yyyy/MM/dd 07:01:00")).ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/mm/dd hh24:mi:ss') ";
                        EName = nowDate.AddDays(-1).ToString("yyyy/MM/dd");
                        DName = EName;
                        NName = DName;
                    }
                    else if (int.Parse(nowDate.ToString("HHmm")) <= 1500)
                    {
                        sql += "AND CREATTIME BETWEEN to_date('" + Convert.ToDateTime(nowDate.ToString("yyyy/MM/dd 07:01:00")).ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/mm/dd hh24:mi:ss') ";
                        sql_Catheter += "AND RECORD_TIME BETWEEN to_date('" + Convert.ToDateTime(nowDate.ToString("yyyy/MM/dd 07:01:00")).ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/mm/dd hh24:mi:ss') ";
                        DName = nowDate.ToString("yyyy/MM/dd");
                        EName = DName;
                        NName = nowDate.AddDays(1).ToString("yyyy/MM/dd");
                    }
                    else if (int.Parse(nowDate.ToString("HHmm")) <= 2300)
                    {
                        sql += "AND CREATTIME BETWEEN to_date('" + Convert.ToDateTime(nowDate.ToString("yyyy/MM/dd 07:01:00")).ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/mm/dd hh24:mi:ss') ";
                        sql_Catheter += "AND RECORD_TIME BETWEEN to_date('" + Convert.ToDateTime(nowDate.ToString("yyyy/MM/dd 07:01:00")).ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/mm/dd hh24:mi:ss') ";
                        NName = nowDate.ToString("yyyy/MM/dd");
                        DName = NName;
                        EName = nowDate.AddDays(1).ToString("yyyy/MM/dd");
                    }
                    else
                    {
                        sql += "AND CREATTIME BETWEEN to_date('" + Convert.ToDateTime(nowDate.ToString("yyyy/MM/dd 07:01:00")).ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/mm/dd hh24:mi:ss') ";
                        sql_Catheter += "AND RECORD_TIME BETWEEN to_date('" + Convert.ToDateTime(nowDate.ToString("yyyy/MM/dd 07:01:00")).ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/mm/dd hh24:mi:ss') ";
                        EName = nowDate.ToString("yyyy/MM/dd");
                        DName = EName;
                        NName = nowDate.AddDays(1).ToString("yyyy/MM/dd");
                    }
                    sql += "AND to_date('" + nowDate.ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/mm/dd hh24:mi:ss') "
                    + "AND DELETED IS NULL ORDER BY CREATTIME, IO_ROW ";
                    sql_Catheter += "AND to_date('" + nowDate.ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/mm/dd hh24:mi:ss') "
                    + "AND DELETED IS NULL ORDER BY RECORD_TIME DESC";
                    Temp = new List<Dictionary<string, string>>();

                    Dt2 = link.DBExecSQL(sql);
                    if (Dt2.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt2.Rows.Count; i++)
                        {
                            temp = new Dictionary<string, string>();
                            if (int.Parse(Convert.ToDateTime(Dt2.Rows[i]["CREATTIME"].ToString()).ToString("HHmm")) <= 700)
                                temp["CLASS"] = "E";
                            else if (int.Parse(Convert.ToDateTime(Dt2.Rows[i]["CREATTIME"].ToString()).ToString("HHmm")) <= 1500)
                                temp["CLASS"] = "D";
                            else if (int.Parse(Convert.ToDateTime(Dt2.Rows[i]["CREATTIME"].ToString()).ToString("HHmm")) <= 2300)
                                temp["CLASS"] = "N";
                            else
                                temp["CLASS"] = "E";
                            temp["TYPEID"] = Dt2.Rows[i]["TYPEID"].ToString();
                            temp["ITEMID"] = Dt2.Rows[i]["ITEMID"].ToString();
                            temp["AMOUNT"] = Dt2.Rows[i]["AMOUNT"].ToString();
                            temp["AMOUNT_ALL"] = Dt2.Rows[i]["AMOUNT_ALL"].ToString();
                            temp["AMOUNT_UNIT"] = Dt2.Rows[i]["AMOUNT_UNIT"].ToString();
                            temp["P_GROUP"] = Dt2.Rows[i]["P_GROUP"].ToString();
                            temp["P_NAME"] = Dt2.Rows[i]["P_NAME"].ToString();
                            temp["NAME"] = Dt2.Rows[i]["NAME"].ToString();
                            temp["CREATTIME"] = Dt2.Rows[i]["CREATTIME"].ToString();
                            temp["REASON"] = Dt2.Rows[i]["REASON"].ToString();
                            temp["EXPLANATION_ITEM"] = Dt2.Rows[i]["EXPLANATION_ITEM"].ToString();
                            Temp.Add(temp);
                        }
                    }

                    List<Dictionary<string, string>> Temp_Catheter = new List<Dictionary<string, string>>();
                    Dt2 = link.DBExecSQL(sql_Catheter);
                    if (Dt2.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt2.Rows.Count; i++)
                        {
                            temp = new Dictionary<string, string>();
                            if (int.Parse(Convert.ToDateTime(Dt2.Rows[i]["RECORD_TIME"].ToString()).ToString("HHmm")) <= 700)
                                temp["CLASS"] = "E";
                            else if (int.Parse(Convert.ToDateTime(Dt2.Rows[i]["RECORD_TIME"].ToString()).ToString("HHmm")) <= 1500)
                                temp["CLASS"] = "D";
                            else if (int.Parse(Convert.ToDateTime(Dt2.Rows[i]["RECORD_TIME"].ToString()).ToString("HHmm")) <= 2300)
                                temp["CLASS"] = "N";
                            else
                                temp["CLASS"] = "E";
                            temp["RECORD_TIME"] = Dt2.Rows[i]["RECORD_TIME"].ToString();
                            temp["RECORD_CLASS"] = Dt2.Rows[i]["RECORD_CLASS"].ToString();
                            temp["AMOUNT"] = Dt2.Rows[i]["AMOUNT"].ToString();
                            temp["COLORID"] = Dt2.Rows[i]["COLORID"].ToString();
                            temp["COLOROTHER"] = Dt2.Rows[i]["COLOROTHER"].ToString();
                            temp["BIT_SURPLUS"] = Dt2.Rows[i]["BIT_SURPLUS"].ToString();
                            Temp_Catheter.Add(temp);
                        }
                    }

                    List<Dictionary<string, string>> IO_Temp = null;
                    IOManageController IO_M = new IOManageController();
                    Dt["I_FOOD"] = ""; Dt["I_BIT"] = ""; Dt["O_PEE"] = ""; Dt["O_TUBE"] = ""; Dt["O_HD"] = ""; Dt["IO"] = "";
                    double I = 0, O = 0, I_FOOD = 0, I_BIT = 0, O_PEE = 0, O_TUBE = 0, O_HD, I_TOTAL = 0, O_TOTAL = 0, HOUR_TOTAL = 0;
                    string I_FOOD_TITLE = "", I_BIT_TITLE = "", O_PEE_TITLE = "", O_TUBE_TITLE = "", O_HD_TITLE = "", TOTAL_LOSS = "";

                    IO_Temp = Temp.FindAll(x => x["CLASS"] == "D");
                    if (IO_Temp.Count > 0)
                    {
                        I = 0; O = 0; I_FOOD = 0; I_BIT = 0; O_PEE = 0; O_TUBE = 0; O_HD = 0;
                        I_FOOD_TITLE = ""; I_BIT_TITLE = ""; O_PEE_TITLE = ""; O_TUBE_TITLE = ""; O_HD_TITLE = "";
                        foreach (var item in IO_Temp.FindAll(x => string.IsNullOrWhiteSpace(x["REASON"])))
                        {
                            if (item["P_GROUP"] == "intaketype")
                                I += double.Parse(item["AMOUNT_ALL"]);
                            else
                                O += double.Parse(item["AMOUNT_ALL"]);
                            switch (item["TYPEID"])
                            {
                                case "2":
                                    I_FOOD += double.Parse(item["AMOUNT_ALL"]);
                                    I_FOOD_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["NAME"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    break;
                                case "1":
                                    I_BIT += double.Parse(item["AMOUNT_ALL"]);
                                    I_BIT_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["NAME"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    break;
                                case "6":
                                    O_PEE += double.Parse(item["AMOUNT_ALL"]);
                                    O_PEE_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["NAME"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    break;
                                case "9":
                                    O_TUBE += double.Parse(item["AMOUNT_ALL"]);
                                    O_TUBE_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), IO_M.sel_item_name(item["ITEMID"]), item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    break;
                                case "8":
                                    O_HD += double.Parse(item["AMOUNT_ALL"]);
                                    O_HD_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["EXPLANATION_ITEM"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    break;
                            }
                        }
                        Dt["I_FOOD"] += "<span title='" + I_FOOD_TITLE + "'>" + DName + " 白班內：由口進食 " + I_FOOD + " ml。</span><br/>";
                        Dt["I_BIT"] += "<span title='" + I_BIT_TITLE + "'>" + DName + " 白班內：點滴 " + I_BIT + " ml。</span><br/>";
                        Dt["O_PEE"] += "<span title='" + O_PEE_TITLE + "'>" + DName + " 白班內：尿液 " + O_PEE + " ml。</span><br/>";
                        Dt["O_TUBE"] += "<span title='" + O_TUBE_TITLE + "'>" + DName + " 白班內：引流管 " + O_TUBE + " ml。</span><br/>";
                        Dt["O_HD"] += "<span title='" + O_HD_TITLE + "'>" + DName + " 白班內：洗腎脫水量 " + O_HD + " ml。</span><br/>";
                        Dt["IO"] += DName + " 白班內：" + I.ToString() + " ml/" + O.ToString() + " ml(" + (I - O).ToString()
                            + ((IO_Temp.Exists(x => !string.IsNullOrWhiteSpace(x["REASON"]))) ? "+Loss" : "") + "/8小時)<br/>";
                        I_TOTAL = I_TOTAL + I;
                        O_TOTAL = O_TOTAL + O;
                        TOTAL_LOSS = ((IO_Temp.Exists(x => !string.IsNullOrWhiteSpace(x["REASON"]))) ? "+Loss" : "");
                        HOUR_TOTAL = HOUR_TOTAL + 8;
                    }

                    IO_Temp = Temp.FindAll(x => x["CLASS"] == "N");
                    if (IO_Temp.Count > 0)
                    {
                        I = 0; O = 0; I_FOOD = 0; I_BIT = 0; O_PEE = 0; O_TUBE = 0; O_HD = 0;
                        I_FOOD_TITLE = ""; I_BIT_TITLE = ""; O_PEE_TITLE = ""; O_TUBE_TITLE = ""; O_HD_TITLE = "";
                        foreach (var item in IO_Temp.FindAll(x => string.IsNullOrWhiteSpace(x["REASON"])))
                        {
                            if (item["P_GROUP"] == "intaketype")
                                I += double.Parse(item["AMOUNT_ALL"]);
                            else
                                O += double.Parse(item["AMOUNT_ALL"]);
                            switch (item["TYPEID"])
                            {
                                case "2":
                                    I_FOOD += double.Parse(item["AMOUNT_ALL"]);
                                    I_FOOD_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["NAME"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    break;
                                case "1":
                                    I_BIT += double.Parse(item["AMOUNT_ALL"]);
                                    I_BIT_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["NAME"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    break;
                                case "6":
                                    O_PEE += double.Parse(item["AMOUNT_ALL"]);
                                    O_PEE_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["NAME"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    break;
                                case "9":
                                    O_TUBE += double.Parse(item["AMOUNT_ALL"]);
                                    O_TUBE_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), IO_M.sel_item_name(item["ITEMID"]), item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    break;
                                case "8":
                                    O_HD += double.Parse(item["AMOUNT_ALL"]);
                                    O_HD_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["EXPLANATION_ITEM"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    break;
                            }
                        }
                        Dt["I_FOOD"] += "<span title='" + I_FOOD_TITLE + "'>" + NName + " 晚班內：由口進食 " + I_FOOD + " ml。</span><br/>";
                        Dt["I_BIT"] += "<span title='" + I_BIT_TITLE + "'>" + NName + " 晚班內：點滴 " + I_BIT + " ml。</span><br/>";
                        Dt["O_PEE"] += "<span title='" + O_PEE_TITLE + "'>" + NName + " 晚班內：尿液 " + O_PEE + " ml。</span><br/>";
                        Dt["O_TUBE"] += "<span title='" + O_TUBE_TITLE + "'>" + NName + " 晚班內：引流管 " + O_TUBE + " ml。</span><br/>";
                        Dt["O_HD"] += "<span title='" + O_HD_TITLE + "'>" + NName + " 晚班內：洗腎脫水量 " + O_HD + " ml。</span><br/>";
                        Dt["IO"] += NName + " 小夜班內：" + I.ToString() + " ml/" + O.ToString() + " ml(" + (I - O).ToString()
                            + ((IO_Temp.Exists(x => !string.IsNullOrWhiteSpace(x["REASON"]))) ? "+Loss" : "") + "/8小時)<br/>";
                        I_TOTAL = I_TOTAL + I;
                        O_TOTAL = O_TOTAL + O;
                        TOTAL_LOSS = ((IO_Temp.Exists(x => !string.IsNullOrWhiteSpace(x["REASON"]))) ? "+Loss" : "");
                        HOUR_TOTAL = HOUR_TOTAL + 8;
                    }

                    IO_Temp = Temp.FindAll(x => x["CLASS"] == "E");
                    if (IO_Temp.Count > 0)
                    {
                        I = 0; O = 0; I_FOOD = 0; I_BIT = 0; O_PEE = 0; O_TUBE = 0; O_HD = 0;
                        I_FOOD_TITLE = ""; I_BIT_TITLE = ""; O_PEE_TITLE = ""; O_TUBE_TITLE = ""; O_HD_TITLE = "";
                        foreach (var item in IO_Temp.FindAll(x => string.IsNullOrWhiteSpace(x["REASON"])))
                        {
                            if (item["P_GROUP"] == "intaketype")
                                I += double.Parse(item["AMOUNT_ALL"]);
                            else
                                O += double.Parse(item["AMOUNT_ALL"]);
                            switch (item["TYPEID"])
                            {
                                case "2":
                                    I_FOOD += double.Parse(item["AMOUNT_ALL"]);
                                    I_FOOD_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["NAME"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    break;
                                case "1":
                                    I_BIT += double.Parse(item["AMOUNT_ALL"]);
                                    I_BIT_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["NAME"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    break;
                                case "6":
                                    O_PEE += double.Parse(item["AMOUNT_ALL"]);
                                    O_PEE_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["NAME"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    break;
                                case "9":
                                    O_TUBE += double.Parse(item["AMOUNT_ALL"]);
                                    O_TUBE_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), IO_M.sel_item_name(item["ITEMID"]), item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    break;
                                case "8":
                                    O_HD += double.Parse(item["AMOUNT_ALL"]);
                                    O_HD_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["EXPLANATION_ITEM"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    break;
                            }
                        }
                        Dt["I_FOOD"] += "<span title='" + I_FOOD_TITLE + "'>" + EName + " 大夜班內：由口進食 " + I_FOOD + " ml。</span><br/>";
                        Dt["I_BIT"] += "<span title='" + I_BIT_TITLE + "'>" + EName + " 大夜班內：點滴 " + I_BIT + " ml。</span><br/>";
                        Dt["O_PEE"] += "<span title='" + O_PEE_TITLE + "'>" + EName + " 大夜班內：尿液 " + O_PEE + " ml。</span><br/>";
                        Dt["O_TUBE"] += "<span title='" + O_TUBE_TITLE + "'>" + EName + " 大夜班內：引流管 " + O_TUBE + " ml。</span><br/>";
                        Dt["O_HD"] += "<span title='" + O_HD_TITLE + "'>" + EName + " 大夜班內：洗腎脫水量 " + O_HD + " ml。</span><br/>";
                        Dt["IO"] += EName + " 大夜班內：" + I.ToString() + " ml/" + O.ToString() + " ml(" + (I - O).ToString()
                            + ((IO_Temp.Exists(x => !string.IsNullOrWhiteSpace(x["REASON"]))) ? "+Loss" : "") + "/8小時)<br/>";
                        I_TOTAL = I_TOTAL + I;
                        O_TOTAL = O_TOTAL + O;
                        TOTAL_LOSS = ((IO_Temp.Exists(x => !string.IsNullOrWhiteSpace(x["REASON"]))) ? "+Loss" : "");
                        HOUR_TOTAL = HOUR_TOTAL + 8;
                    }
                    Dt["IO"] +=  I_TOTAL.ToString() + " ml/" + O_TOTAL.ToString() + " ml(" + (I_TOTAL - O_TOTAL).ToString()
                        + TOTAL_LOSS + "/" + HOUR_TOTAL +"小時)";


                    #endregion

                    #region 昨日總量
                    //攝入/輸出
                    sql = "SELECT IO.*, CASE WHEN AMOUNT_UNIT <> '3' THEN AMOUNT ELSE AMOUNT*0.001 END AMOUNT_ALL "
                    + ",(SELECT P_GROUP FROM SYS_PARAMS WHERE P_MODEL = 'iotype' AND P_VALUE = IO.TYPEID)P_GROUP "
                    + ",(SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'iotype' AND P_VALUE = IO.TYPEID)P_NAME "
                    + ",(SELECT NAME FROM IO_ITEM WHERE IO_ITEM.ITEMID = IO.ITEMID)NAME "
                    + "FROM IO_DATA IO "
                    + "WHERE FEENO = '" + feeno + "' ";

                    sql_Catheter = "SELECT RECORD_TIME, RECORD_CLASS, AMOUNT, COLOROTHER, BIT_SURPLUS "
                    + ",(SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'catheter_flush' AND P_GROUP = 'urine_color' AND P_VALUE = COLORID)COLORID "
                    + "FROM FLUSH_CATHETER_DATA "
                    + "WHERE FEENO = '" + feeno + "' ";

                    string start_time = "";
                    string end_time = "";
                    string work_date = "";
                    switch (shiftcate)
                    {
                        case "D":
                            start_time = nowDate.AddDays(-1).ToString("yyyy/MM/dd 07:01:00");
                            end_time = nowDate.ToString("yyyy/MM/dd 07:00:00");
                            break;
                        case "E":
                            start_time = nowDate.AddDays(-1).ToString("yyyy/MM/dd 07:01:00");
                            end_time = nowDate.ToString("yyyy/MM/dd 07:00:00");
                            break;
                        case "N":
                            if (int.Parse(nowDate.ToString("HHmm")) <= 700)
                            {
                                start_time = nowDate.AddDays(-2).ToString("yyyy/MM/dd 07:01:00");
                                end_time = nowDate.AddDays(-1).ToString("yyyy/MM/dd 07:00:00");
                            }else
                            {
                                start_time = nowDate.AddDays(-1).ToString("yyyy/MM/dd 07:01:00");
                                end_time = nowDate.ToString("yyyy/MM/dd 07:00:00");
                            }
                            break;

                        default:
                            break;
                    }

                    sql += "AND CREATTIME BETWEEN to_date('" + Convert.ToDateTime(start_time).ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/mm/dd hh24:mi:ss') ";
                    sql_Catheter += "AND RECORD_TIME BETWEEN to_date('" + Convert.ToDateTime(start_time).ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/mm/dd hh24:mi:ss') ";
                    work_date = Convert.ToDateTime(start_time).ToString("yyyy/MM/dd");
                    string N_work_date = Convert.ToDateTime(start_time).AddDays(1).ToString("yyyy/MM/dd");
                    sql += "AND to_date('" + end_time + "','yyyy/mm/dd hh24:mi:ss') "
                    + "AND DELETED IS NULL ORDER BY CREATTIME, IO_ROW ";
                    sql_Catheter += "AND to_date('" + end_time + "','yyyy/mm/dd hh24:mi:ss') "
                    + "AND DELETED IS NULL ORDER BY RECORD_TIME DESC";
                    Temp = new List<Dictionary<string, string>>();

                    Dt2 = link.DBExecSQL(sql);
                    if (Dt2.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt2.Rows.Count; i++)
                        {
                            temp = new Dictionary<string, string>();
                            if (int.Parse(Convert.ToDateTime(Dt2.Rows[i]["CREATTIME"].ToString()).ToString("HHmm")) <= 700)
                                temp["CLASS"] = "E";
                            else if (int.Parse(Convert.ToDateTime(Dt2.Rows[i]["CREATTIME"].ToString()).ToString("HHmm")) <= 1500)
                                temp["CLASS"] = "D";
                            else if (int.Parse(Convert.ToDateTime(Dt2.Rows[i]["CREATTIME"].ToString()).ToString("HHmm")) <= 2300)
                                temp["CLASS"] = "N";
                            else
                                temp["CLASS"] = "E";
                            temp["TYPEID"] = Dt2.Rows[i]["TYPEID"].ToString();
                            temp["ITEMID"] = Dt2.Rows[i]["ITEMID"].ToString();
                            temp["AMOUNT"] = Dt2.Rows[i]["AMOUNT"].ToString();
                            temp["AMOUNT_ALL"] = Dt2.Rows[i]["AMOUNT_ALL"].ToString();
                            temp["AMOUNT_UNIT"] = Dt2.Rows[i]["AMOUNT_UNIT"].ToString();
                            temp["P_GROUP"] = Dt2.Rows[i]["P_GROUP"].ToString();
                            temp["P_NAME"] = Dt2.Rows[i]["P_NAME"].ToString();
                            temp["NAME"] = Dt2.Rows[i]["NAME"].ToString();
                            temp["CREATTIME"] = Dt2.Rows[i]["CREATTIME"].ToString();
                            temp["REASON"] = Dt2.Rows[i]["REASON"].ToString();
                            temp["EXPLANATION_ITEM"] = Dt2.Rows[i]["EXPLANATION_ITEM"].ToString();
                            Temp.Add(temp);
                        }
                    }
                    
                    Temp_Catheter = new List<Dictionary<string, string>>();
                    Dt2 = link.DBExecSQL(sql_Catheter);
                    if (Dt2.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt2.Rows.Count; i++)
                        {
                            temp = new Dictionary<string, string>();
                            if (int.Parse(Convert.ToDateTime(Dt2.Rows[i]["RECORD_TIME"].ToString()).ToString("HHmm")) <= 700)
                                temp["CLASS"] = "E";
                            else if (int.Parse(Convert.ToDateTime(Dt2.Rows[i]["RECORD_TIME"].ToString()).ToString("HHmm")) <= 1500)
                                temp["CLASS"] = "D";
                            else if (int.Parse(Convert.ToDateTime(Dt2.Rows[i]["RECORD_TIME"].ToString()).ToString("HHmm")) <= 2300)
                                temp["CLASS"] = "N";
                            else
                                temp["CLASS"] = "E";
                            temp["RECORD_TIME"] = Dt2.Rows[i]["RECORD_TIME"].ToString();
                            temp["RECORD_CLASS"] = Dt2.Rows[i]["RECORD_CLASS"].ToString();
                            temp["AMOUNT"] = Dt2.Rows[i]["AMOUNT"].ToString();
                            temp["COLORID"] = Dt2.Rows[i]["COLORID"].ToString();
                            temp["COLOROTHER"] = Dt2.Rows[i]["COLOROTHER"].ToString();
                            temp["BIT_SURPLUS"] = Dt2.Rows[i]["BIT_SURPLUS"].ToString();
                            Temp_Catheter.Add(temp);
                        }
                    }
                    

                    IO_Temp = null;
                    IO_M = new IOManageController();
                    Dt["I_FOOD"] = ""; Dt["I_BIT"] = ""; Dt["O_PEE"] = ""; Dt["O_TUBE"] = ""; Dt["O_HD"] = ""; Dt["IO_beforeday"] = "";

                    #region 昨日總量(暫時移除)

                    //IO_Temp = Temp.FindAll(x => x["CLASS"] == "D");
                    //if (IO_Temp.Count > 0)
                    //{
                    //    I = 0; O = 0; I_FOOD = 0; I_BIT = 0; O_PEE = 0; O_TUBE = 0; O_HD = 0;
                    //    I_FOOD_TITLE = ""; I_BIT_TITLE = ""; O_PEE_TITLE = ""; O_TUBE_TITLE = ""; O_HD_TITLE = "";
                    //    foreach (var item in IO_Temp.FindAll(x => string.IsNullOrWhiteSpace(x["REASON"])))
                    //    {
                    //        if (item["P_GROUP"] == "intaketype")
                    //            I += double.Parse(item["AMOUNT_ALL"]);
                    //        else
                    //            O += double.Parse(item["AMOUNT_ALL"]);
                    //    }
                    //    Dt["IO_beforeday"] += work_date + " 白班內：" + I.ToString() + " ml/" + O.ToString() + " ml(" + (I - O).ToString()
                    //        + ((IO_Temp.Exists(x => !string.IsNullOrWhiteSpace(x["REASON"]))) ? "+Loss" : "") + "/8小時)<br/>";
                    //}

                    //IO_Temp = Temp.FindAll(x => x["CLASS"] == "N");
                    //if (IO_Temp.Count > 0)
                    //{
                    //    I = 0; O = 0; I_FOOD = 0; I_BIT = 0; O_PEE = 0; O_TUBE = 0; O_HD = 0;
                    //    I_FOOD_TITLE = ""; I_BIT_TITLE = ""; O_PEE_TITLE = ""; O_TUBE_TITLE = ""; O_HD_TITLE = "";
                    //    foreach (var item in IO_Temp.FindAll(x => string.IsNullOrWhiteSpace(x["REASON"])))
                    //    {
                    //        if (item["P_GROUP"] == "intaketype")
                    //            I += double.Parse(item["AMOUNT_ALL"]);
                    //        else
                    //            O += double.Parse(item["AMOUNT_ALL"]);
                    //    }
                    //    Dt["IO_beforeday"] += work_date + " 小夜班內：" + I.ToString() + " ml/" + O.ToString() + " ml(" + (I - O).ToString()
                    //        + ((IO_Temp.Exists(x => !string.IsNullOrWhiteSpace(x["REASON"]))) ? "+Loss" : "") + "/8小時)<br/>";
                    //}

                    //IO_Temp = Temp.FindAll(x => x["CLASS"] == "E");
                    //if (IO_Temp.Count > 0)
                    //{
                    //    I = 0; O = 0; I_FOOD = 0; I_BIT = 0; O_PEE = 0; O_TUBE = 0; O_HD = 0;
                    //    I_FOOD_TITLE = ""; I_BIT_TITLE = ""; O_PEE_TITLE = ""; O_TUBE_TITLE = ""; O_HD_TITLE = "";
                    //    foreach (var item in IO_Temp.FindAll(x => string.IsNullOrWhiteSpace(x["REASON"])))
                    //    {
                    //        if (item["P_GROUP"] == "intaketype")
                    //            I += double.Parse(item["AMOUNT_ALL"]);
                    //        else
                    //            O += double.Parse(item["AMOUNT_ALL"]);
                    //    }
                    //    Dt["IO_beforeday"] += N_work_date + " 大夜班內：" + I.ToString() + " ml/" + O.ToString() + " ml(" + (I - O).ToString()
                    //        + ((IO_Temp.Exists(x => !string.IsNullOrWhiteSpace(x["REASON"]))) ? "+Loss" : "") + "/8小時)<br/>";
                    //}
                    #endregion

                    I = 0; O = 0; I_FOOD = 0; I_BIT = 0; O_PEE = 0; O_TUBE = 0; O_HD = 0;
                    I_FOOD_TITLE = ""; I_BIT_TITLE = ""; O_PEE_TITLE = ""; O_TUBE_TITLE = ""; O_HD_TITLE = "";
                    IO_Temp = Temp.FindAll(x => x["CLASS"] == "N" || x["CLASS"] == "D" || x["CLASS"] == "E");

                    if (IO_Temp.Count > 0)
                    {
                        foreach (var item in IO_Temp.FindAll(x => string.IsNullOrWhiteSpace(x["REASON"])))
                        {
                            if (item["P_GROUP"] == "intaketype")
                                I += double.Parse(item["AMOUNT_ALL"]);
                            else
                                O += double.Parse(item["AMOUNT_ALL"]);
                        }
                    }

                    Dt["IO_beforeday"] +=  I.ToString() + " ml/" + O.ToString() + " ml(" + (I - O).ToString()
                        + ((IO_Temp.Exists(x => !string.IsNullOrWhiteSpace(x["REASON"]))) ? "+Loss" : "") + "/24小時)<br/>";

                    #endregion

                    #region 連續性沖洗尿管出入量--功能已拿掉
                    /*
                    Dt["Catheter"] = "";
                    double I_Catheter = 0, O_Catheter = 0, IO_Catheter_Lass = 0;

                    IO_Temp = Temp_Catheter.FindAll(x => x["CLASS"] == "D");
                    if (IO_Temp.Count > 0)
                    {
                        I_Catheter = 0; O_Catheter = 0; IO_Catheter_Lass = 0;
                        foreach (var item in IO_Temp)
                        {
                            if (item["RECORD_CLASS"] == "0")
                                I_Catheter += double.Parse(item["AMOUNT"].ToString());
                            else if (item["RECORD_CLASS"] == "1")
                                O_Catheter += double.Parse(item["AMOUNT"].ToString());
                            if (!string.IsNullOrWhiteSpace(item["BIT_SURPLUS"].ToString()))
                                IO_Catheter_Lass += double.Parse(item["BIT_SURPLUS"].ToString());
                        }
                        Dt["Catheter"] += DName + " 白班：" + I_Catheter.ToString() + "ml/" + O_Catheter.ToString() + "ml "
                            + "(" + (I_Catheter - O_Catheter).ToString() + "ml)" + "，每小時尿量：" + ((I_Catheter - O_Catheter) / 8).ToString() + "ml/hr";
                        if (!string.IsNullOrWhiteSpace(IO_Temp[0]["COLORID"]))
                        {
                            Dt["Catheter"] += "，尿液顏色：" + IO_Temp[0]["COLORID"];
                            if (!string.IsNullOrWhiteSpace(IO_Temp[0]["COLOROTHER"]))
                                Dt["Catheter"] = Dt["Catheter"].Substring(0, Dt["Catheter"].Length - 2) + IO_Temp[0]["COLOROTHER"];
                        }
                        if (IO_Catheter_Lass > 0)
                            Dt["Catheter"] += "，點滴餘量：" + IO_Catheter_Lass.ToString() + "ml";
                        Dt["Catheter"] += "。<br/>";
                    }

                    IO_Temp = Temp_Catheter.FindAll(x => x["CLASS"] == "N");
                    if (IO_Temp.Count > 0)
                    {
                        I_Catheter = 0; O_Catheter = 0; IO_Catheter_Lass = 0;
                        foreach (var item in IO_Temp)
                        {
                            if (item["RECORD_CLASS"] == "0")
                                I_Catheter += double.Parse(item["AMOUNT"].ToString());
                            else if (item["RECORD_CLASS"] == "1")
                                O_Catheter += double.Parse(item["AMOUNT"].ToString());
                            if (!string.IsNullOrWhiteSpace(item["BIT_SURPLUS"].ToString()))
                                IO_Catheter_Lass += double.Parse(item["BIT_SURPLUS"].ToString());
                        }
                        Dt["Catheter"] += NName + " 晚班：" + I_Catheter.ToString() + "ml/" + O_Catheter.ToString() + "ml "
                            + "(" + (I_Catheter - O_Catheter).ToString() + "ml)" + "，每小時尿量：" + ((I_Catheter - O_Catheter) / 8).ToString() + "ml/hr";
                        if (!string.IsNullOrWhiteSpace(IO_Temp[0]["COLORID"]))
                        {
                            Dt["Catheter"] += "，尿液顏色：" + IO_Temp[0]["COLORID"];
                            if (!string.IsNullOrWhiteSpace(IO_Temp[0]["COLOROTHER"]))
                                Dt["Catheter"] = Dt["Catheter"].Substring(0, Dt["Catheter"].Length - 2) + IO_Temp[0]["COLOROTHER"];
                        }
                        if (IO_Catheter_Lass > 0)
                            Dt["Catheter"] += "，點滴餘量：" + IO_Catheter_Lass.ToString() + "ml。";
                        Dt["Catheter"] += " <br/>";
                    }

                    IO_Temp = Temp_Catheter.FindAll(x => x["CLASS"] == "E");
                    if (IO_Temp.Count > 0)
                    {
                        I_Catheter = 0; O_Catheter = 0; IO_Catheter_Lass = 0;
                        foreach (var item in IO_Temp)
                        {
                            if (item["RECORD_CLASS"] == "0")
                                I_Catheter += double.Parse(item["AMOUNT"].ToString());
                            else if (item["RECORD_CLASS"] == "1")
                                O_Catheter += double.Parse(item["AMOUNT"].ToString());
                            if (!string.IsNullOrWhiteSpace(item["BIT_SURPLUS"].ToString()))
                                IO_Catheter_Lass += double.Parse(item["BIT_SURPLUS"].ToString());
                        }
                        Dt["Catheter"] += EName + " 大夜班：" + I_Catheter.ToString() + "ml/" + O_Catheter.ToString() + "ml "
                            + "(" + (I_Catheter - O_Catheter).ToString() + "ml)" + "，每小時尿量：" + ((I_Catheter - O_Catheter) / 8).ToString() + "ml/hr";
                        if (!string.IsNullOrWhiteSpace(IO_Temp[0]["COLORID"]))
                        {
                            Dt["Catheter"] += "，尿液顏色：" + IO_Temp[0]["COLORID"];
                            if (!string.IsNullOrWhiteSpace(IO_Temp[0]["COLOROTHER"]))
                                Dt["Catheter"] = Dt["Catheter"].Substring(0, Dt["Catheter"].Length - 2) + IO_Temp[0]["COLOROTHER"];
                        }
                        if (IO_Catheter_Lass > 0)
                            Dt["Catheter"] += "，點滴餘量：" + IO_Catheter_Lass.ToString() + "ml。";
                        Dt["Catheter"] += " <br/>";
                    }
                    */
                    #endregion

                    #region CVVH--功能已拿掉
                    /*
                    List<string> Dates = new List<string>();
                    if (int.Parse(nowDate.ToString("HHmm")) <= 2300)
                    {
                        Dates.Add(nowDate.AddDays(-2).ToString("yyyy/MM/dd 23:00:00"));
                        Dates.Add(nowDate.AddDays(-1).ToString("yyyy/MM/dd 23:00:00"));
                        Dates.Add(nowDate.ToString("yyyy/MM/dd 23:00:00"));
                    }
                    else
                    {
                        Dates.Add(nowDate.AddDays(-1).ToString("yyyy/MM/dd 23:00:00"));
                        Dates.Add(nowDate.ToString("yyyy/MM/dd 23:00:00"));
                        Dates.Add(nowDate.AddDays(1).ToString("yyyy/MM/dd 23:00:00"));
                    }

                    sql = "SELECT DATA_TIME, I_REPLACE_FLUID, I_FLUSHES, O_UF "
                    + "FROM CVVH_DTL_DATA DTL INNER JOIN CVVH_MASTER MASTER "
                    + "ON DTL.RECORD_ID = MASTER.RECORD_ID "
                    + "WHERE MASTER.FEENO = '" + feeno + "' AND MASTER.DELETED IS NULL "
                    + "AND DTL.DATA_TIME BETWEEN to_date('" + Convert.ToDateTime(Dates[0]).AddDays(-1).ToString("yyyy/MM/dd 23:00:00") + "','yyyy/mm/dd hh24:mi:ss') "
                    + "AND to_date('" + nowDate.ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/mm/dd hh24:mi:ss') "
                    + "AND DTL.DELETED IS NULL ";

                    Temp = new List<Dictionary<string, string>>();

                    if (link.DBExecSQL(sql, ref reader) && reader.Read() )
                    {
                        while (reader.Read())
                        {
                            temp = new Dictionary<string, string>();
                            temp["DATA_TIME"] = reader["DATA_TIME"].ToString();
                            temp["I_REPLACE_FLUID"] = reader["I_REPLACE_FLUID"].ToString();
                            temp["I_FLUSHES"] = reader["I_FLUSHES"].ToString();
                            temp["O_UF"] = reader["O_UF"].ToString();
                            Temp.Add(temp);
                        }
                    }
                    reader.Close();
                    reader.Dispose();

                    Dt["CVVH"] = "";
                    if (Temp.Count > 0)
                    {
                        double I_REPLACE_FLUID = 0, I_FLUSHES = 0, O_UF = 0;
                        foreach (string Date in Dates)
                        {
                            I_REPLACE_FLUID = 0; I_FLUSHES = 0; O_UF = 0;
                            foreach (var item in Temp.FindAll(x => Convert.ToDateTime(x["DATA_TIME"]) < Convert.ToDateTime(Date)))
                            {
                                I_REPLACE_FLUID += double.Parse(item["I_REPLACE_FLUID"]);
                                I_FLUSHES += double.Parse(item["I_FLUSHES"]);
                                O_UF += double.Parse(item["O_UF"]);
                                Temp.Remove(item);
                            }
                            if (I_REPLACE_FLUID + I_FLUSHES + O_UF > 0)
                                Dt["CVVH"] += Convert.ToDateTime(Date).ToString("yyyy/MM/dd") + " 總量：" + (I_REPLACE_FLUID + I_FLUSHES).ToString()
                                    + "ml/" + O_UF.ToString() + "ml (" + ((I_REPLACE_FLUID + I_FLUSHES) - O_UF).ToString() + "ml)。<br/>";
                        }
                    }
                    */
                    #endregion

                    #region 點滴輸液

                    //點滴輸液
                    sql = " SELECT SUM(B.USE_DOSE)as USE_DOSE,TO_CHAR(EXEC_DATE, 'YYYY-MM-DD')as EXEC_DATE,UD_SEQ FROM (SELECT A.EXEC_DATE,A.UD_SEQ, A.USE_DOSE FROM DRUG_EXECUTE A ";
                    sql += " WHERE A.FEE_NO = '" + feeno + "' AND A.EXEC_DATE IS NOT NULL ";
                    sql += " AND TO_CHAR(EXEC_DATE, 'YYYY-MM-DD') IN ('" + DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd") + "','" + DateTime.Now.ToString("yyyy-MM-dd") + "') ";
                    sql += " AND A.UD_SEQPK LIKE 'V%')B GROUP BY TO_CHAR(EXEC_DATE, 'YYYY-MM-DD'),UD_SEQ ORDER BY EXEC_DATE";
                    Temp = new List<Dictionary<string, string>>();
                    Dt2 = link.DBExecSQL(sql);
                    if (Dt2.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt2.Rows.Count; i++)
                        {
                            temp = new Dictionary<string, string>();
                            temp["EXEC_DATE"] = Dt2.Rows[i]["EXEC_DATE"].ToString();
                            temp["UD_SEQ"] = Dt2.Rows[i]["UD_SEQ"].ToString();
                            temp["USE_DOSE"] = Dt2.Rows[i]["USE_DOSE"].ToString();
                            Temp.Add(temp);
                        }
                    }

                    byte[] GetUdOrder = webService.GetUdOrder(feeno.Trim(), "A");
                    if (GetUdOrder != null)
                    {
                        List<UdOrder> UdOrderList = JsonConvert.DeserializeObject<List<UdOrder>>(CompressTool.DecompressString(GetUdOrder));
                        Dt["Drip"] = "";
                        UdOrder udTemp = null;
                        foreach (var item in Temp)
                        {
                            udTemp = UdOrderList.Find(x => x.UD_SEQ == item["UD_SEQ"]);
                            if (udTemp != null)
                            {
                                Dt["Drip"] += Convert.ToDateTime(item["EXEC_DATE"]).ToString("yyyy/MM/dd") + " <label class='LittleTitle'>點滴名稱</label>：" + udTemp.MED_DESC + "(" + udTemp.ALISE_DESC + ")"
                                    + "<label class='LittleTitle'>劑量</label>：" + udTemp.UD_DOSE
                                     + "<label class='LittleTitle'>頻次</label>：" + udTemp.UD_CIR
                                     + "<label class='LittleTitle'>備註</label>：" + udTemp.UD_CMD
                                     + "<label class='LittleTitle'>已給劑量</label>：" + item["USE_DOSE"] + "瓶。<hr />";
                            }
                        }
                        if (!string.IsNullOrWhiteSpace(Dt["Drip"]))
                            Dt["Drip"] = Dt["Drip"].Substring(0, Dt["Drip"].Length - 5);
                    }
                    #endregion
                    
                    #region 藥物過敏
                    List<PatientInfo> allergy_list = new List<PatientInfo>();
                    byte[] allergyfoByteCode = webService.GetAllergyList(feeno.Trim());
                    if (allergyfoByteCode != null)
                    {
                        string allergyJosnArr = CompressTool.DecompressString(allergyfoByteCode);
                        List<PatientInfo> allergy = JsonConvert.DeserializeObject<List<PatientInfo>>(allergyJosnArr);
                        allergyJosnArr = string.Empty;
                        foreach (PatientInfo item in allergy)
                        {
                            allergyJosnArr += item.AllergyDesc + "，";
                        }
                        Dt["allergy_list"] = allergyJosnArr.Substring(0, allergyJosnArr.Length - 1);
                    }
                    #endregion

                    #region 會診
                    
                    //會診
                    byte[] ConsultationByte = webService.GetPatientConsult(feeno);
                    if (ConsultationByte != null)
                    {
                        List<Consultation> ConsultationList = JsonConvert.DeserializeObject<List<Consultation>>(CompressTool.DecompressString(ConsultationByte));

                        List<Consultation> NewConsultationList = new List<Consultation>();
                        if (ConsultationList.Count > 0)
                        {
                            Temp_String = string.Empty;
                            foreach (Consultation item in ConsultationList)
                            {
                                if (Temp_String != item.OrderDate.ToString("yyyy/MM/dd HH:mm:ss"))
                                {
                                    NewConsultationList.Add(new Consultation()
                                    {
                                        OrderDate = item.OrderDate,
                                        ConsDoc = item.ConsDoc,
                                        ConsDept = item.ConsDept,
                                        ConsContent = item.ConsContent,
                                        OrderNo = item.OrderNo,
                                    });
                                    Temp_String = item.OrderDate.ToString("yyyy/MM/dd HH:mm:ss");
                                }
                                else
                                    NewConsultationList[NewConsultationList.Count - 1].ConsContent += " " + item.ConsContent;
                            }
                            Temp_String = string.Empty;
                            foreach (Consultation item in NewConsultationList)
                            {
                                //Temp_String += string.Format("<label class='LittleTitle'>開單日期：</label>{0}，<label class='LittleTitle'>會診醫師：</label>{1}，<label class='LittleTitle'>會診科別：</label>{2}，<label class='LittleTitle'>會診結果：</label><input data-orderno='{3}' type='button' value='查詢' onclick='set_patient({4});get_content({4});' /><hr/>"
                                //    , item.OrderDate.ToString("yyyy/MM/dd HH:mm")
                                //    , item.ConsDoc, item.ConsDept, item.OrderNo , '"'+feeno+'"');
                                    Temp_String += string.Format("<input data-orderno='{0}' type='button' value='查詢' onclick='set_patient({1});get_content({1});' /><hr/>"
                                    ,item.OrderNo , '"'+feeno+'"');
                                break;
                            }
                            if (Temp_String.Length > 5)
                                Dt["Consultation"] = Temp_String.Substring(0, Temp_String.Length - 5);
                        }
                    }


                    Dt["Condition"] = Consultation;
                    Dt["AlertStr"] = "";
                    if (alertstr.Count>0)
                    {
                        Dt["AlertStr"] = string.Join("、",alertstr);
                    }


                    #endregion

                    return JsonConvert.SerializeObject(Dt);
                }
                else
                    return "";

            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(),ex);

                return "";
            }
            finally
            {
                this.link.DBClose();
            }
            
        }

        public ActionResult Get_GetPatientConsult(string feeno)
        {
            byte[] PatientConsultionCode = webService.GetPatientConsult(feeno);

            if (PatientConsultionCode != null)
            {
                string PatientConsultionJosnArr = NIS.UtilTool.CompressTool.DecompressString(PatientConsultionCode);
                List<Consultation> PatientConsultionInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Consultation>>(PatientConsultionJosnArr);
                foreach (var item in PatientConsultionInfo)
                {
                    switch (item.ConsultFlag)
                    {
                        case "0":
                            item.ConsultFlag = "會診";
                            break;
                        case "1":
                            item.ConsultFlag = "照會";
                            break;
                        default:
                            break;
                    }
                }
                ViewData["PatientConsultionInfo"] = PatientConsultionInfo;
            }
            return View();
        }

        private string CombinedWord(List<Dictionary<string, string>> Temp, string Name, string FirstWord, string LastWord)
        {
            if (Temp.Exists(x => x["Name"] == Name))
            {
                if (!string.IsNullOrWhiteSpace(FirstWord))
                    return "<label class='LittleTitle'>" + FirstWord + "</label>：" + Temp.Find(x => x["Name"] == Name)["Value"] + LastWord;
                else
                    return Temp.Find(x => x["Name"] == Name)["Value"] + LastWord;
            }
            else
                return "";
        }

        [HttpPost]
        public string GetConsult(string OrderNo)
        {
            byte[] ConsultationByte = webService.GetPatientConsult(base.ptinfo.FeeNo);
            if (ConsultationByte != null && !string.IsNullOrWhiteSpace(OrderNo))
            {
                List<Consultation> ConsultationList = JsonConvert.DeserializeObject<List<Consultation>>(CompressTool.DecompressString(ConsultationByte));
                return ConsultationList.Find(x => x.OrderNo == OrderNo).ConsContent;
            }
            else
                return "";
        }

        #endregion

        //醫囑頁面
        [HttpPost]
        public ActionResult TextAdvice()
        {
            try
            {
                //bool bool_read = true;
                if (Session["PatInfo"] != null)
                {
                    List<SignOrder> OrderList = new List<SignOrder>();
                    SignOrder Temp = null;
                    //取得病人文字醫囑資料
                    byte[] textorderByte = webService.GetTextOrder(base.ptinfo.FeeNo);
                    if (textorderByte != null)
                    {
                        string textorderJsonArr = CompressTool.DecompressString(textorderByte).Replace(">", "> ").Replace("<", "< ");
                        List<TextOrder> textorder = JsonConvert.DeserializeObject<List<TextOrder>>(textorderJsonArr);

                        if (textorder.Count > 0)
                        {
                            List<SignOrder> SingOrderList = new List<SignOrder>();
                            string sqlstr = "SELECT * FROM DATA_SIGNORDER "
                            + "WHERE FEE_NO = '" + base.ptinfo.FeeNo + "' AND SIGN_TYPE = 'T' ";

                            DataTable Dt = link.DBExecSQL(sqlstr);
                            if (Dt.Rows.Count > 0)
                            {
                                // 取得資料庫有簽過的內容
                                for (int i = 0; i < Dt.Rows.Count; i++)
                                {
                                    Temp = new SignOrder();
                                    Temp.fee_no = Dt.Rows[i]["fee_no"].ToString().Trim();
                                    Temp.sheet_no = Dt.Rows[i]["sheet_no"].ToString().Trim();
                                    Temp.order_type = Dt.Rows[i]["order_type"].ToString().Trim();
                                    Temp.order_content = Dt.Rows[i]["order_content"].ToString().Trim();
                                    Temp.start_date = Convert.ToDateTime(Dt.Rows[i]["start_date"].ToString().Trim());
                                    Temp.end_date = Convert.ToDateTime(Dt.Rows[i]["end_date"].ToString().Trim());
                                    Temp.sign_time = Convert.ToDateTime(Dt.Rows[i]["sign_time"].ToString().Trim());
                                    Temp.sign_user = Dt.Rows[i]["sign_user"].ToString().Trim();
                                    Temp.sign_user_name = Dt.Rows[i]["sign_user_name"].ToString().Trim();
                                    Temp.set_user = Dt.Rows[i]["set_user"].ToString().Trim();
                                    Temp.record_name = Dt.Rows[i]["RECORD_NAME"].ToString().Trim();
                                    Temp.record_time = Dt.Rows[i]["RECORD_TIME"].ToString().Trim();
                                    SingOrderList.Add(Temp);
                                }
                            }
                        
                            //bool_read = link.DBExecSQL(sqlstr, ref reader);
                            //if (bool_read)
                            //{
                            //    // 取得資料庫有簽過的內容
                            //    while (reader.Read())
                            //    {
                            //        Temp = new SignOrder();
                            //        Temp.fee_no = reader["fee_no"].ToString().Trim();
                            //        Temp.sheet_no = reader["sheet_no"].ToString().Trim();
                            //        Temp.order_type = reader["order_type"].ToString().Trim();
                            //        Temp.order_content = reader["order_content"].ToString().Trim();
                            //        Temp.start_date = Convert.ToDateTime(reader["start_date"].ToString().Trim());
                            //        Temp.end_date = Convert.ToDateTime(reader["end_date"].ToString().Trim());
                            //        Temp.sign_time = Convert.ToDateTime(reader["sign_time"].ToString().Trim());
                            //        Temp.sign_user = reader["sign_user"].ToString().Trim();
                            //        Temp.sign_user_name = reader["sign_user_name"].ToString().Trim();
                            //        Temp.set_user = reader["set_user"].ToString().Trim();
                            //        Temp.record_name = reader["RECORD_NAME"].ToString().Trim();
                            //        Temp.record_time = reader["RECORD_TIME"].ToString().Trim();
                            //        SingOrderList.Add(Temp);
                            //    }
                            //}
                         

                            SignOrder FindTemp = null;
                            foreach (TextOrder item in textorder)
                            {
                                Temp = new SignOrder();
                                Temp.fee_no = base.ptinfo.FeeNo;
                                Temp.sheet_no = item.SheetNo.Trim();
                                Temp.order_type = item.Category.Trim();
                                Temp.order_content = item.Content.Trim();
                                Temp.start_date = item.OrderStartDate;
                                Temp.end_date = item.OrderEndDate;
                                Temp.DC_FLAG = item.DC_FLAG;

                                FindTemp = SingOrderList.Find(x =>
                                    x.sheet_no.Trim() == item.SheetNo.Trim() &&
                                    x.order_type.Trim() == item.Category.Trim() &&
                                    x.order_content.Trim() == item.Content.Trim() &&
                                    (x.start_date == item.OrderStartDate || x.start_date.ToString("yyyy/MM/dd HH:mm:ss") == DateTime.MinValue.ToString("yyyy/MM/dd HH:mm:ss"))
                                    );
                                if (FindTemp != null)
                                {
                                    Temp.sign_user = FindTemp.sign_user;
                                    Temp.sign_user_name = FindTemp.sign_user_name;
                                    Temp.sign_time = FindTemp.sign_time;
                                    Temp.set_user = FindTemp.set_user;
                                    Temp.record_name = FindTemp.record_name;
                                    Temp.record_time = FindTemp.record_time;
                                }
                                OrderList.Add(Temp);
                            }
                        }

                        #region --測試資料 --
                        //mod塞資料 by jarvis 2016/06/13
                        //OrderList.Add(new SignOrder()
                        //{
                        //    fee_no = ptinfo.FeeNo,
                        //    sheet_no = "555555",
                        //    order_type = "S",
                        //    order_content = "【090220 K (Potassium) (Blood)】採檢時間:0600 Serum",
                        //    start_date = Convert.ToDateTime("2016/06/11 01:00:00"),
                        //    end_date = Convert.ToDateTime("2016/12/11 01:00:00"),
                        //    sign_user = "",
                        //    sign_user_name = "",
                        //    sign_time = DateTime.MinValue,
                        //    set_user = "",
                        //    DC_FLAG = ""
                        //});
                        //OrderList.Add(new SignOrder()
                        //{
                        //    fee_no = ptinfo.FeeNo,
                        //    sheet_no = "666666",
                        //    order_type = "S",
                        //    order_content = "【090220 K (Potassium) (Blood)】採檢時間:0600 Serum",
                        //    start_date = Convert.ToDateTime("2016/06/11 13:00:00"),
                        //    end_date = DateTime.MaxValue,
                        //    sign_user = "",
                        //    sign_user_name = "",
                        //    sign_time = DateTime.MinValue,
                        //    set_user = "",
                        //    DC_FLAG = ""
                        //});
                        //OrderList.Add(new SignOrder()
                        //{
                        //    fee_no = ptinfo.FeeNo,
                        //    sheet_no = "777777",
                        //    order_type = "S",
                        //    order_content = "【090220 K (Potassium) (Blood)】採檢時間:0600 Serum",
                        //    start_date = Convert.ToDateTime("2016/07/01 01:00:00"),
                        //    end_date = DateTime.MaxValue,
                        //    sign_user = "",
                        //    sign_user_name = "",
                        //    sign_time = DateTime.MinValue,
                        //    set_user = "",
                        //    DC_FLAG = ""
                        //});
                        //OrderList.Add(new SignOrder()
                        //{
                        //    fee_no = ptinfo.FeeNo,
                        //    sheet_no = "888888",
                        //    order_type = "S",
                        //    order_content = "【090220 K (Potassium) (Blood)】採檢時間:0600 Serum",
                        //    start_date = Convert.ToDateTime("2016/06/14 01:00:00"),
                        //    end_date = DateTime.MaxValue,
                        //    sign_user = "",
                        //    sign_user_name = "",
                        //    sign_time = DateTime.MinValue,
                        //    set_user = "",
                        //    DC_FLAG = ""
                        //});
                        //OrderList.Add(new SignOrder()
                        //{
                        //    fee_no = ptinfo.FeeNo,
                        //    sheet_no = "999999",
                        //    order_type = "R",
                        //    order_content = "【090220 K (Potassium) (Blood)】採檢時間:0600 Serum999999",
                        //    start_date = Convert.ToDateTime("2016/06/10 01:00:00"),
                        //    end_date = Convert.ToDateTime("2016/06/12 01:00:00"),
                        //    sign_user = "",
                        //    sign_user_name = "",
                        //    sign_time = DateTime.MinValue,
                        //    set_user = "",
                        //    DC_FLAG = "DC"
                        //});
                        #endregion
                        OrderList = OrderList.Where(x => x.DC_FLAG != "DC").ToList();//將DC_FLAG == "DC" 的資料，預設排除 by jarvis 20161228
                        //OrderList.RemoveAll(x => x.start_date > DateTime.Now);//把超過現在的過濾掉 by jarvis 20160614(暫時不用，使用前段判斷)
                        OrderList = OrderList.OrderBy(x => x.sign_time).ThenBy(x => x.order_type).ThenBy(x => x.start_date).ToList();//將簽過的放在前面 by jarvis 20161202,並照已簽收=>排序為矚型=>最新開立日期  排序
                    }
                    ViewData["OrderList"] = OrderList;
                }
                return View();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(),ex);
                return View();
            }
        }

        //藥矚頁面
        [HttpPost]
        public ActionResult MedicineAdvice()
        {
            //bool bool_read = true;
            if (Session["PatInfo"] != null)
            {
                List<SignOrder> OrderList = new List<SignOrder>();
                SignOrder Temp = null;
                //取得藥矚
                byte[] drugOrderByteCode = webService.GetIpdDrugOrder(ptinfo.FeeNo);
                if (drugOrderByteCode != null)
                {
                    string drugOrderJobj = CompressTool.DecompressString(drugOrderByteCode).Replace(">", "> ").Replace("<", "< ");
                    List<DrugOrder> drugOrderList = JsonConvert.DeserializeObject<List<DrugOrder>>(drugOrderJobj);

                    if (drugOrderList != null && drugOrderList.Count > 0)
                    {
                        List<SignOrder> sorder = new List<SignOrder>();
                        //IDataReader reader = null;
                        string sqlstr = "SELECT * FROM DATA_SIGNORDER "
                        + "WHERE FEE_NO = '" + base.ptinfo.FeeNo + "' and sign_type = 'M' ";
                        DataTable Dt = link.DBExecSQL(sqlstr);
                        if (Dt.Rows.Count > 0)
                        {
                            // 取得資料庫有簽過的內容
                            for (int i = 0; i < Dt.Rows.Count; i++)
                            {
                                Temp = new SignOrder();
                                Temp.fee_no = Dt.Rows[i]["fee_no"].ToString().Trim();
                                Temp.sheet_no = Dt.Rows[i]["sheet_no"].ToString().Trim();
                                Temp.order_type = Dt.Rows[i]["order_type"].ToString().Trim();
                                Temp.cir_code = Dt.Rows[i]["cir_code"].ToString().Trim();
                                Temp.path_code = Dt.Rows[i]["path_code"].ToString().Trim();
                                Temp.order_content = Dt.Rows[i]["order_content"].ToString().Trim();
                                Temp.start_date = Convert.ToDateTime(Dt.Rows[i]["start_date"].ToString().Trim());
                                Temp.end_date = Convert.ToDateTime(Dt.Rows[i]["end_date"].ToString().Trim());
                                Temp.sign_time = Convert.ToDateTime(Dt.Rows[i]["sign_time"].ToString().Trim());
                                Temp.sign_user = Dt.Rows[i]["sign_user"].ToString().Trim();
                                Temp.sign_user_name = Dt.Rows[i]["sign_user_name"].ToString().Trim();
                                Temp.set_user = Dt.Rows[i]["set_user"].ToString().Trim();
                                sorder.Add(Temp);
                            }
                        }

                       
                        //bool_read = link.DBExecSQL(sqlstr, ref reader);
                        //if (bool_read) {
                        //    // 取得資料庫有簽過的內容
                        //    while (reader.Read())
                        //    {
                        //        Temp = new SignOrder();
                        //        Temp.fee_no = reader["fee_no"].ToString().Trim();
                        //        Temp.sheet_no = reader["sheet_no"].ToString().Trim();
                        //        Temp.order_type = reader["order_type"].ToString().Trim();
                        //        Temp.cir_code = reader["cir_code"].ToString().Trim();
                        //        Temp.path_code = reader["path_code"].ToString().Trim();
                        //        Temp.order_content = reader["order_content"].ToString().Trim();
                        //        Temp.start_date = Convert.ToDateTime(reader["start_date"].ToString().Trim());
                        //        Temp.end_date = Convert.ToDateTime(reader["end_date"].ToString().Trim());
                        //        Temp.sign_time = Convert.ToDateTime(reader["sign_time"].ToString().Trim());
                        //        Temp.sign_user = reader["sign_user"].ToString().Trim();
                        //        Temp.sign_user_name = reader["sign_user_name"].ToString().Trim();
                        //        Temp.set_user = reader["set_user"].ToString().Trim();
                        //        sorder.Add(Temp);
                        //    }
                        //}
                        

                        SignOrder FindTemp = null;
                        foreach (DrugOrder item in drugOrderList)
                        {
                            if (item.OrderEndDate == DateTime.MinValue || item.OrderEndDate >= DateTime.Now
                             || item.Category.ToString().Trim() == "S" || item.DcFlag.ToString().Trim() == "Y")
                            {
                                Temp = new SignOrder();
                                Temp.fee_no = base.ptinfo.FeeNo;
                                Temp.sheet_no = item.SheetNo.ToString().Trim();
                                Temp.order_type = item.Category.ToString().Trim();
                                Temp.order_content = item.DrugName.ToString().Trim() + "|" + item.GenericDrugs.ToString().Trim();
                                Temp.cir_code = item.Feq.ToString().Trim();
                                Temp.path_code = item.Route.ToString().Trim();
                                Temp.start_date = item.OrderStartDate;
                                Temp.pre_qty = item.Dose.ToString();
                                Temp.memo = item.Note.ToString().Trim();
                                Temp.unit = item.DoseUnit.ToString().Trim();
                                Temp.end_date = item.OrderEndDate;
                                Temp.rate_l = item.RateL.ToString().Trim();
                                Temp.rate_h = item.RateH.ToString().Trim();
                                Temp.rate_memo = item.RateMemo.ToString().Trim();
                                Temp.DC_FLAG = item.DcFlag;

                                FindTemp = sorder.Find(x =>
                                    x.sheet_no == item.SheetNo.Trim() &&
                                    x.order_content == item.DrugName.Trim() + "|" + item.GenericDrugs.Trim() &&
                                    x.cir_code == item.Feq.Trim() &&
                                    x.path_code == item.Route.Trim() &&
                                    (x.start_date == item.OrderStartDate || x.start_date.ToString("yyyy/MM/dd HH:mm:ss") == DateTime.MinValue.ToString("yyyy/MM/dd HH:mm:ss"))
                                    );

                                if (FindTemp != null)
                                {
                                    Temp.sign_user = FindTemp.sign_user;
                                    Temp.sign_user_name = FindTemp.sign_user_name;
                                    Temp.sign_time = FindTemp.sign_time;
                                    Temp.set_user = FindTemp.set_user;
                                }
                                OrderList.Add(Temp);
                            }
                        }
                    }
                    //OrderList.RemoveAll(x => x.start_date > DateTime.Now);
                    OrderList = OrderList.OrderBy(x => x.sign_time).ThenBy(x => x.order_type).ThenBy(x => x.start_date).ToList();//將簽過的放在前面 by jarvis 20161202,並照已簽收=>排序為矚型=>最新開立日期  排序
                    ViewData["OrderList"] = OrderList;
                }
            }
            return View();
        }

        //取得圍長中文名稱
        private string set_name(string name)
        {
            string _name = "";
            switch (name)
            {
                case "gtwl":
                    _name = "腰圍";
                    break;
                case "gthr":
                    _name = "頭圍";
                    break;
                case "gtbu":
                    _name = "胸圍";
                    break;
                case "gtbl":
                    _name = "腹圍";
                    break;
                case "gthl":
                    _name = "臀圍";
                    break;
                case "gtlf":
                    _name = "左前臂";
                    break;
                case "gtrf":
                    _name = "右前臂";
                    break;
                case "gtlua":
                    _name = "左上臂";
                    break;
                case "gtrua":
                    _name = "右上臂";
                    break;
                case "gtlt":
                    _name = "左大腿";
                    break;
                case "gtrt":
                    _name = "右大腿";
                    break;
                case "gtll":
                    _name = "左小腿";
                    break;
                case "gtrl":
                    _name = "右小腿";
                    break;
                case "gtla":
                    _name = "左足踝";
                    break;
                case "gtra":
                    _name = "右足踝";
                    break;
                default:
                    _name = "";
                    break;
            }
            return _name;
        }


        //產生交班的頁面，存成PDF檔案
        public ActionResult TransferDuty_PDF()
        {
            //bool bool_read = true;
            string shift = Request["shift"].ToString(), bedno = Request["bedno"].ToString(), userNo = Request["userNo"].ToString(), SuccessorNo = Request["SuccessorNo"].ToString();
            List<TransList> tdList = new List<TransList>();
            //IDataReader reader = null;
            string sqlstr = string.Empty, shift_cate = string.Empty;
            string tmpshift = string.Empty, tmpcate = string.Empty, checkflag = "";
            int strtime = int.Parse(DateTime.Now.ToString("HHmm"));
            if (strtime >= 0 && strtime <= 759)
                checkflag = "N";
            else if (strtime >= 800 && strtime <= 1559)
                checkflag = "D";
            else if (strtime >= 1600 && strtime <= 2359)
                checkflag = "E";
            sqlstr += " SELECT BED_NO, SHIFT_CATE FROM DATA_DISPATCHING WHERE ";
            sqlstr += " RESPONSIBLE_USER = '" + userNo + "' ";
            sqlstr += " AND SHIFT_CATE = '" + shift + "' AND BED_NO='" + bedno + "' ";
            if (checkflag == "N")
            { sqlstr += " AND TO_CHAR(SHIFT_DATE, 'yyyy/MM/dd') BETWEEN TO_CHAR(SYSDATE-1,'yyyy/MM/dd') AND TO_CHAR(SYSDATE,'yyyy/MM/dd')"; }
            else
            { sqlstr += " AND TO_CHAR(SHIFT_DATE, 'yyyy/MM/dd') = TO_CHAR(SYSDATE,'yyyy/MM/dd')"; }
            sqlstr += " GROUP BY BED_NO, SHIFT_CATE ORDER BY SHIFT_CATE,BED_NO";
            List<SelectListItem> transcate_list = new List<SelectListItem>();

            DataTable Dt = link.DBExecSQL(sqlstr);
            if (Dt.Rows.Count > 0)
            {
                for (int i = 0; i < Dt.Rows.Count; i++)
                {
                    shift_cate = Dt.Rows[i]["SHIFT_CATE"].ToString().Trim();
                    byte[] ptinfobyte = webService.BedNoTransformFeeNo(Dt.Rows[i]["bed_no"].ToString().Trim());
                    if (ptinfobyte != null)
                    {
                        string ptinfojarr = CompressTool.DecompressString(ptinfobyte);
                        PatientInfo[] patinfo = JsonConvert.DeserializeObject<PatientInfo[]>(ptinfojarr);
                        // 取得VitalSign資料
                        string[] vital_info = tdc.getTranInfo(patinfo[0].FeeNo, shift_cate);
                        string[] wait_info = tdc.getWaitInfo(patinfo[0].FeeNo);
                        byte[] ptinfoByteCode = webService.GetPatientInfo(patinfo[0].FeeNo);
                        if (ptinfoByteCode != null)
                        {
                            string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                            PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                            patinfo[0] = pi;
                        }
                        string speical_info = string.Empty, remark_info = string.Empty;
                        string remarkExtra = "";
                        tdc.getSpecialInfo(patinfo[0].FeeNo, ref remark_info, ref speical_info, ref remarkExtra);
                        patinfo[0].BedNo = Dt.Rows[i]["bed_no"].ToString().Trim();
                        tdList.Add(new TransList(patinfo[0], vital_info, wait_info, remark_info, speical_info, shift_cate, remarkExtra));
                    }
                }
            }
            //bool_read = link.DBExecSQL(sqlstr, ref reader);
            //if (bool_read) {
            //    while (reader.Read())
            //    {
            //        shift_cate = reader["SHIFT_CATE"].ToString().Trim();
            //        byte[] ptinfobyte = webService.BedNoTransformFeeNo(reader["bed_no"].ToString().Trim());
            //        if (ptinfobyte != null)
            //        {
            //            string ptinfojarr = CompressTool.DecompressString(ptinfobyte);
            //            PatientInfo[] patinfo = JsonConvert.DeserializeObject<PatientInfo[]>(ptinfojarr);
            //            // 取得VitalSign資料
            //            string[] vital_info = tdc.getTranInfo(patinfo[0].FeeNo, shift_cate);
            //            string[] wait_info = tdc.getWaitInfo(patinfo[0].FeeNo);
            //            byte[] ptinfoByteCode = webService.GetPatientInfo(patinfo[0].FeeNo);
            //            if (ptinfoByteCode != null)
            //            {
            //                string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
            //                PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
            //                patinfo[0] = pi;
            //            }
            //            string speical_info = string.Empty, remark_info = string.Empty;
            //            tdc.getSpecialInfo(patinfo[0].FeeNo, ref remark_info, ref speical_info);
            //            patinfo[0].BedNo = reader["bed_no"].ToString().Trim();
            //            tdList.Add(new TransList(patinfo[0], vital_info, wait_info, remark_info, speical_info, shift_cate));
            //        }
            //    }
            //}

            switch (shift_cate)
            {
                case "D":
                    ViewData["transcate"] = "早班";
                    break;
                case "E":
                    ViewData["transcate"] = "小夜";
                    break;
                case "N":
                    ViewData["transcate"] = "大夜";
                    break;
                default:
                    ViewData["transcate"] = "";
                    break;
            }
            ViewData["transuser"] = GetEmployeeName(userNo);//取當前這班的人名子
            ViewData["SuccessorName"] = GetEmployeeName(SuccessorNo);//取接這班的人名子
            ViewData["tdList"] = tdList;
            tdList = null;
            return View();
        }

        //離開焦點時取得員工姓名-ajax
        public string GetEmployeeName(string empNo)
        {
            string empName = "";
            if (!string.IsNullOrEmpty(empNo))
            {
                byte[] listByteCode = webService.UserName(empNo);
                if (listByteCode != null)
                {
                    string listJsonArray = CompressTool.DecompressString(listByteCode);
                    UserInfo user_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                    empName = user_info.EmployeesName;
                }
            }
            return empName;
        }

        //儲存接班者
        //2022/03/11 家雄註解 已將邏輯合併到SaveKardex
        //產嬰似乎有Reference這個Method，先不刪
        public string SaveSuccessorNo(string SUCCESSOR)
        {
            string BEDNO = "";
            string SHIFTCATE = "";
            string where = "";
            int erow = 0;
            if (!string.IsNullOrEmpty(SUCCESSOR))
            {
                try
                {
                    BEDNO = Request["BEDNO"].ToString();
                    SHIFTCATE = Request["SHIFTCATE"].ToString();
                    string date = "";
                    DateTime Now = DateTime.Now;                  
                    if (Int32.Parse(Now.ToString("HH")) < 3 )
                    {
                        date = Now.AddDays(-1).ToString("yyyy/MM/dd");
                    }
                    else
                    {
                        date = Now.ToString("yyyy/MM/dd");
                    }
                    where = "RESPONSIBLE_USER = '" + userinfo.EmployeesNo + "' AND TO_CHAR(SHIFT_DATE, 'yyyy/MM/dd') = '" + date + "' AND BED_NO='" + BEDNO + "' AND SHIFT_CATE='" + SHIFTCATE + "'";
                    List<DBItem> dbItem = new List<DBItem>();
                    dbItem.Add(new DBItem("MODIFY_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("MODIFY_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                    dbItem.Add(new DBItem("SUCCESSOR", SUCCESSOR, DBItem.DBDataType.String));
                    erow = this.link.DBExecUpdate("DATA_DISPATCHING", dbItem, where);
                }
                catch (Exception ex)
                {
                    log.saveLogMsg(ex.Message.ToString() + "，SUCCESSOR：" + SUCCESSOR + "，BEDNO：" + Request["BEDNO"]  + "，SHIFTCATE：" + Request["SHIFTCATE"] + "，where：" + where , "SaveSucceNoLog");
                }
            }
            if (erow > 0)
            {
                return "Y";
            }
            else
            {
                log.saveLogMsg("SUCCESSOR：" + SUCCESSOR + "，BEDNO：" + Request["BEDNO"] + "，SHIFTCATE：" + Request["SHIFTCATE"] + "，where：" + where, "SaveSucceNoLog");

                return "N";
            }
        }
    }
}
