using Com.Mayaminer;
using Newtonsoft.Json;
using NIS.Data;
using NIS.Models;
using NIS.UtilTool;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace NIS.Controllers
{
    public class OBSShiftManageController : BaseController
    {
        private DBConnector link;
        private TransferDutyController tdc;
        private MainController mc;
        private LogTool log;
        Obstetrics obs_m = new Obstetrics();

        public OBSShiftManageController()
        {
            this.link = new DBConnector();
            this.tdc = new TransferDutyController();
            this.mc = new MainController();
            this.log = new LogTool();
        }

        //041交班單
        public ActionResult Kardex()
        {
            //交班單預計項目數值
            byte[] TempByte = this.webService.GetExpectedItem(base.ptinfo.FeeNo);
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
            TempByte = this.webService.GetBloodInfo(base.ptinfo.FeeNo);
            List<Dictionary<string, string>> BloodInfoList = new List<Dictionary<string, string>>();
            if (TempByte != null)
            {
                BloodInfoList = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(CompressTool.DecompressString(TempByte));
            }

            ViewData["BloodInfoList"] = BloodInfoList;
            //ViewBag.shiftcate = "D";
            //ViewBag.type = "M41";
            //ViewBag.paramsdata = "undefined";
            ViewBag.shiftcate = Request["shiftcate"].ToString().Trim();
            ViewBag.type = Request["type"].ToString().Trim();
            ViewBag.paramsdata = Request["paramsdata"].ToString().Trim();
            ViewBag.RootDocument = GetSourceUrl();
            ViewBag.Assessment = base.ptinfo.Assessment;
            ViewBag.feeno = base.ptinfo.FeeNo.Trim();
            ViewBag.chartNo = base.ptinfo.ChartNo.Trim();
            ViewBag.BedNo = base.ptinfo.BedNo.Trim();
            ViewBag.Age = base.ptinfo.Age;

            //新生兒取得媽媽的FEENO
            if (base.ptinfo.Age < 1)
            {
                var sql = "SELECT * FROM OBS_BABYLINK_DATA "
                 + "WHERE BABY_FEE_NO = '" + base.ptinfo.FeeNo + "'";

                DataTable Dt2 = link.DBExecSQL(sql);
                if (Dt2.Rows.Count > 0)
                {
                    ViewBag.MomFeeno = Dt2.Rows[0]["MOM_FEE_NO"].ToString();
                }
            }
            else
            {
                var sql = "SELECT * FROM OBS_BABYLINK_DATA "
                + "WHERE MOM_FEE_NO = '" + base.ptinfo.FeeNo + "'";

                DataTable Dt2 = link.DBExecSQL(sql);
                ViewBag.BabyDt = Dt2;
            }

            DataTable vy_dt = new Obstetrics().sel_vagyarn(ptinfo?.FeeNo);
            if (vy_dt != null)
            {
                var sum = 0;
                foreach (DataRow r in vy_dt.Rows)
                {
                    if (r["PUTIN_AMT"].ToString() != "")
                        sum += Convert.ToInt32(r["PUTIN_AMT"].ToString());
                    if (r["TAKEOUT_AMT"].ToString() != "")
                        sum -= Convert.ToInt32(r["TAKEOUT_AMT"].ToString());
                }
                ViewBag.vy_sum = sum;
                ViewBag.vy = true;
            }
            else
            {
                ViewBag.vy = false;
            }

            try
            {

                var breath_sys = ws_rcs.GetRCSSystemURLFromIPDByResultMode("RCS_G1", base.ptinfo.FeeNo);
                ViewBag.breath_sys = breath_sys.returnJSON;
            }
            catch (Exception ex)
            {
            }

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
                string sql = "SELECT RECORD_JSON_DATA FROM OBS_TRANS_SHIFT "
                + "WHERE FEE_NO = '" + feeno + "' AND RECORD_DATE = ( "
                + "SELECT MAX(RECORD_DATE) FROM OBS_TRANS_SHIFT WHERE FEE_NO = '" + feeno + "' ) ";

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
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return JsonConvert.SerializeObject(Temp);
            }
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
                    dbItem.Add(new DBItem("SHIFT_CODE", base.creatid("OBS_TRANS_SHIFT", base.userinfo.EmployeesNo, base.ptinfo.FeeNo, "0"), DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("FEE_NO", base.ptinfo.FeeNo, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("RECORD_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("RECORD_JSON_DATA", Data, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("RECORD_USERNO", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("RECORD_USERNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    if (this.link.DBExecInsert("OBS_TRANS_SHIFT", dbItem) == 1)
                        Success = true;
                }
                catch
                {
                    Success = false;
                }
            }

            if(Success)
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
                queryString = "url=" + baseURL + "/OBSShiftManage/TransferDuty_PDF?";
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
                queryString = "url=" + baseURL + "/OBSShiftManage/Kardex_Detail_PDF?";
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

        private string error_str(string value, string message, ref List<string> alert_list, string aa_type = "")
        {
            if (string.IsNullOrEmpty(value))
            {
                value = "<font color=\"red\">資料異常</font>";
                alert_list.Add(message);
            }
            else
            {
                switch (aa_type.ToUpper())
                {
                    case "GCS":
                        value = value.Substring(1, 1);
                        break;
                    default:
                        break;
                }
            }
            return value;
        }

        [HttpPost]
        public string SelectDt()
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

                PatientInfo pf = (PatientInfo)Session["PatInfo"];
                int age = (base.ptinfo != null) ? base.ptinfo.Age : Int32.Parse(Request["age"].ToString());
                string type = base.get_check_type(ptinfo); //取得生命徵象異常年紀代號
                var shiftcate = Request["SHIFTCATE"].ToString();
                List<string> alertstr = new List<string>();
                if (Session["PatInfo"] != null || Request["feeno"].ToString() != null)//feeno的這個回傳參數，是為了，給列印明細頁面讀取時用的
                {
                    List<Dictionary<string, string>> Temp = new List<Dictionary<string, string>>();
                    Dictionary<string, string> temp = null, Dt = new Dictionary<string, string>();
                    DataTable Dt2 = new DataTable();
                    string sql = string.Empty;
                    string Temp_String = string.Empty;
                    string[] ListWord = null;
                    string feeno = (base.ptinfo != null) ? base.ptinfo.FeeNo : Request["feeno"].ToString();
                    string Blood_Type = (base.ptinfo != null) ? base.ptinfo.Blood_Type : Request["Blood_Type"].ToString();
                    string Consultation = (base.ptinfo != null) ? base.ptinfo.Consultation : Request["Consultation"].ToString();// Dt["Condition"]看起來這格是要取Condition才對，先暫時取這個
                    string mom_feeno = string.Empty;
                    string mom_seq = string.Empty;

                    //兒科-出生日期
                    Dt["C_BirthDay"] = (base.ptinfo != null) ? base.ptinfo.Birthday.ToString("yyyy/MM/dd") : "";
                    Dt["C_param_ipd_source"] = (base.ptinfo != null) ? base.ptinfo.InDate.ToString("yyyy/MM/dd") : "";
                    Dt["C_pt_blood"] = Blood_Type;

                    #region 新生兒取得媽媽的FEENO
                    if (age < 1)
                    {
                        sql = "SELECT * FROM OBS_BABYLINK_DATA "
                        + "WHERE BABY_FEE_NO = '" + feeno + "'";

                        Dt2 = link.DBExecSQL(sql);
                        if (Dt2.Rows.Count > 0)
                        {
                            mom_feeno = Dt2.Rows[0]["MOM_FEE_NO"].ToString();
                            ViewBag.Mom_FeeNo = mom_feeno;
                            mom_seq = Dt2.Rows[0]["BABY_SEQ"].ToString();
                        }
                    }
                    #endregion

                    #region 入院護理評估_成人
                    sql = "SELECT * FROM (SELECT TABLEID, NATYPE FROM ASSESSMENTMASTER "
                    + "WHERE FEENO = '" + feeno + "' AND NATYPE = 'A' AND DELETED IS NULL "
                    + "AND STATUS NOT IN ('TEMPORARY','DELETE') "
                    + "ORDER BY CREATETIME DESC, MODIFYTIME DESC) WHERE ROWNUM <= 1 ";
                    string TableId = string.Empty, NaType = string.Empty;

                    Dt2 = link.DBExecSQL(sql); //上面已有Dt變數
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
                                    if (Temp_String.Length > 2)
                                        Temp_String = Temp_String.Substring(0, Temp_String.Length - 2) + Temp.Find(x => x["Name"] == "param_im_history_item_other_txt")["Value"];
                                }
                                Dt["A_param_im_history_item_other"] = Temp_String;
                            }
                            //住院史
                            if (Temp.Exists(x => x["Name"] == "param_ipd_past_reason"))
                            {
                                Temp_String = Temp.Find(x => x["Name"] == "param_ipd_past_reason")["Value"];
                                Dt["A_param_ipd_past_reason"] = Temp_String;
                            }
                            if (Temp.Exists(x => x["Name"] == "param_ipd_past_location"))
                            {
                                Temp_String = Temp.Find(x => x["Name"] == "param_ipd_past_location")["Value"];
                                Dt["A_param_ipd_past_location"] = Temp_String;
                            }
                            //開刀史                        
                            if (Temp.Exists(x => x["Name"] == "param_ipd_surgery_reason"))
                            {
                                Temp_String = Temp.Find(x => x["Name"] == "param_ipd_surgery_reason")["Value"];
                                Dt["A_param_ipd_surgery_reason"] = Temp_String;
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

                            //接觸史TOCC
                            //媽媽
                            if (Temp.Exists(x => x["Name"] == "rb_mother"))
                            {
                                if (Temp.Find(x => x["Name"] == "rb_mother")["Value"] == "有")
                                {
                                    Dt["A_Mom_TOCC"] = $"<label class='LittleTitle'>媽媽</label>：{Temp.Find(x => x["Name"] == "rb_mother")["Value"]}";
                                    if (Temp.Exists(x => x["Name"] == "mother_ck_cp"))
                                    {
                                        if (Temp.Exists(x => x["Name"] == "txt_mother_Y_other"))
                                            Dt["A_Mom_TOCC"] += $"，{Temp.Find(x => x["Name"] == "mother_ck_cp")["Value"].Replace("其他", Temp.Find(x => x["Name"] == "txt_mother_Y_other")["Value"])}";
                                        else
                                            Dt["A_Mom_TOCC"] += $"，{Temp.Find(x => x["Name"] == "mother_ck_cp")["Value"]}";
                                    }
                                }
                            }
                            //同住家人
                            if (Temp.Exists(x => x["Name"] == "rb_family"))
                            {
                                if (Temp.Find(x => x["Name"] == "rb_family")["Value"] == "有")
                                {
                                    Dt["A_Mom_TOCC"] = $"{(Dt.ContainsKey("A_Mom_TOCC") ? Dt["A_Mom_TOCC"] + "<hr />" : "")}<label class='LittleTitle'>同住家人</label>：{Temp.Find(x => x["Name"] == "rb_family")["Value"]}";
                                    if (Temp.Exists(x => x["Name"] == "txt_family_appellation"))
                                        Dt["A_Mom_TOCC"] = Dt["A_Mom_TOCC"].Replace("同住家人", Temp.Find(x => x["Name"] == "txt_family_appellation")["Value"]);
                                    if (Temp.Exists(x => x["Name"] == "family_ck_cp"))
                                    {
                                        if (Temp.Exists(x => x["Name"] == "txt_family_Y_other"))
                                            Dt["A_Mom_TOCC"] += $"，{Temp.Find(x => x["Name"] == "family_ck_cp")["Value"].Replace("其他", Temp.Find(x => x["Name"] == "txt_family_Y_other")["Value"])}";
                                        else
                                            Dt["A_Mom_TOCC"] += $"，{Temp.Find(x => x["Name"] == "family_ck_cp")["Value"]}";
                                    }
                                }
                            }
                            //生產前14天內，寶寶的哥哥、姊姊學校有無班上同學因為傳染病請假或班級停課之情形
                            if (Temp.Exists(x => x["Name"] == "rb_brosis"))
                            {
                                if (Temp.Find(x => x["Name"] == "rb_brosis")["Value"] == "有")
                                {
                                    Dt["A_Mom_TOCC"] = $"{(Dt.ContainsKey("A_Mom_TOCC") ? Dt["A_Mom_TOCC"] + "<hr />" : "")}<label class='LittleTitle'>哥哥姊姊</label>：{Temp.Find(x => x["Name"] == "rb_brosis")["Value"]}";
                                    if (Temp.Exists(x => x["Name"] == "brosis_ck_cp"))
                                    {
                                        if (Temp.Exists(x => x["Name"] == "txt_brosis_Y_other"))
                                            Dt["A_Mom_TOCC"] += $"，{Temp.Find(x => x["Name"] == "brosis_ck_cp")["Value"].Replace("其他", Temp.Find(x => x["Name"] == "txt_brosis_Y_other")["Value"])}";
                                        else
                                            Dt["A_Mom_TOCC"] += $"，{Temp.Find(x => x["Name"] == "brosis_ck_cp")["Value"]}";
                                    }
                                }
                            }
                            //住院期間照顧者(應盡量維持同一人)，目前有無：發燒、腹瀉、咳嗽、流鼻水等疑似感染症狀
                            if (Temp.Exists(x => x["Name"] == "rb_company"))
                                Dt["A_Mom_TOCC"] = $"{(Dt.ContainsKey("A_Mom_TOCC") ? Dt["A_Mom_TOCC"] + "<hr />" : "")}住院期間照顧者(應盡量維持同一人)，目前{Temp.Find(x => x["Name"] == "rb_company")["Value"]}發燒、腹瀉、咳嗽、流鼻水等疑似感染症狀";
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
                            //if (Temp.Exists(x => x["Name"] == "param_tube_date"))
                            //    Dt["C_param_ipd_source"] = Temp.Find(x => x["Name"] == "param_tube_date")["Value"] + " ";
                            //if (Temp.Exists(x => x["Name"] == "param_tube_time"))
                            //    Dt["C_param_ipd_source"] += Temp.Find(x => x["Name"] == "param_tube_time")["Value"];
                            //Convert.ToDateTime(base.ptinfo.InDate).ToString("yyyy/MM/dd HH:mm") + 
                            if (Temp.Exists(x => x["Name"] == "param_ipd_source"))
                                Dt["C_param_ipd_source"] += Temp.Find(x => x["Name"] == "param_ipd_source")["Value"] + "入院";
                            //入院方式
                            if (Temp.Exists(x => x["Name"] == "param_ipd_style"))
                                Dt["C_param_ipd_style"] = Temp.Find(x => x["Name"] == "param_ipd_style")["Value"];
                            if (Temp.Exists(x => x["Name"] == "param_ipd_style_other"))
                                Dt["C_param_ipd_style"] = Temp.Find(x => x["Name"] == "param_ipd_style_other")["Value"];
                        }
                    }

                    #endregion

                    #region 入評-新生兒&嬰幼兒(入院經過/來源)
                    sql = $@"select * from (
select FEENO, '新生兒' AS Type, BIRTH, WEIGHT, GEST_M, GEST_D, BIRTH_TYPE, PROCESS, FROM_WHERE, RECORDTIME FROM OBS_NBENTR
WHERE FEENO = '{feeno}'  AND DELETED IS NULL
UNION select FEENO, '嬰幼兒' AS Type, BIRTH,WEIGHT, GEST_M, GEST_D,BIRTH_TYPE, PROCESS, FROM_WHERE, RECORDTIME FROM OBS_BABYENTR 
WHERE FEENO = '{feeno}' AND DELETED IS NULL) A 
ORDER BY RECORDTIME DESC";

                    Dt2 = link.DBExecSQL(sql);

                    if (Dt2 != null && Dt2.Rows.Count > 0)
                    {
                        //來源
                        if (Dt2.Rows[0]["FROM_WHERE"].ToString() != "")
                        {
                            if (Dt2.Rows[0]["Type"].ToString() == "新生兒")
                            {
                                if (Dt2.Rows[0]["FROM_WHERE"].ToString() == "0")
                                {
                                    Dt["C_Source"] = "本院生產";
                                }
                                else if (Dt2.Rows[0]["FROM_WHERE"].ToString() == "1")
                                {
                                    Dt["C_Source"] = "院外生產";
                                }
                                else if (Dt2.Rows[0]["FROM_WHERE"].ToString() == "2")
                                {
                                    Dt["C_Source"] = "外院轉入";
                                }
                            }
                            if (Dt2.Rows[0]["Type"].ToString() == "嬰幼兒")
                            {
                                if (Dt2.Rows[0]["FROM_WHERE"].ToString() == "0")
                                {
                                    Dt["C_Source"] = "急診";
                                }
                                else if (Dt2.Rows[0]["FROM_WHERE"].ToString() == "1")
                                {
                                    Dt["C_Source"] = "門診";
                                }
                                else if (Dt2.Rows[0]["FROM_WHERE"].ToString() == "2")
                                {
                                    Dt["C_Source"] = "病房";
                                }
                                else if (Dt2.Rows[0]["FROM_WHERE"].ToString() == "3")
                                {
                                    Dt["C_Source"] = "外院轉入";
                                }
                            }
                        }
                        //入院經過
                        if (Dt2.Rows[0]["PROCESS"].ToString() != "")
                        {
                            Dt["C_param_ipd_reason"] = Dt2.Rows[0]["PROCESS"].ToString();
                        }

                        //早產週數
                        //生產週數天數+（今天日期-生產日期），若大於40週則顯示足月，否則顯示週數天數
                        if (Dt2.Rows[0]["BIRTH"].ToString() != "")
                        {
                            var spanTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy/MM/dd")).Subtract(Convert.ToDateTime(Convert.ToDateTime(Dt2.Rows[0]["BIRTH"]).ToString("yyyy/MM/dd")));
                            var gestM = 0;
                            var gestD = 0;

                            if (Dt2.Rows[0]["GEST_M"].ToString() != "")
                                gestM = Convert.ToInt32(Dt2.Rows[0]["GEST_M"].ToString());
                            if (Dt2.Rows[0]["GEST_D"].ToString() != "")
                                gestD = Convert.ToInt32(Dt2.Rows[0]["GEST_D"].ToString());

                            gestD += spanTime.Days;
                            gestM += (gestD / 7);
                            gestD = gestD % 7;

                            if (gestM >= 40)
                                Dt["C_Preterm_Birth"] = "足月";
                            else
                                Dt["C_Preterm_Birth"] = $"{gestM}週{gestD}天";
                        }

                        //妊娠週數
                        if (Dt2.Rows[0]["GEST_M"].ToString() != "")
                        {
                            var gest = $"{Dt2.Rows[0]["GEST_M"].ToString()}週";
                            if (Dt2.Rows[0]["GEST_D"].ToString() != "")
                                Dt["C_Gest"] = $"{gest} {Dt2.Rows[0]["GEST_D"].ToString()}天";
                            else
                                Dt["C_Gest"] = gest;
                        }

                        //生產方式
                        if (Dt2.Rows[0]["BIRTH_TYPE"].ToString() != "")
                        {
                            var birthType = Dt2.Rows[0]["BIRTH_TYPE"].ToString();
                            if (birthType == "0")
                                Dt["C_BirthType"] = "自然產";
                            else if (birthType == "1")
                                Dt["C_BirthType"] = "剖腹產";
                        }

                        //出生體重
                        if (Dt2.Rows[0]["WEIGHT"].ToString() != "")
                        {
                            Dt["C_Weight"] = Dt2.Rows[0]["WEIGHT"].ToString();
                        }
                    }
                    #endregion

                    #region 入院護理評估_新生兒
                    sql = "SELECT * FROM OBS_NBENTR "
                    + "WHERE FEENO = '" + feeno + "' AND DELETED IS NULL ORDER BY CREATTIME DESC";

                    Dt2 = link.DBExecSQL(sql);
                    if (Dt2.Rows.Count > 0)
                    {
                        //母親病史-服藥史
                        if (Dt2.Rows[0]["MED"].ToString() != "")
                        {
                            if (Dt2.Rows[0]["MED"].ToString() == "無")
                                Dt["C_Mom_Med"] = "無";
                            if (Dt2.Rows[0]["MED"].ToString() == "有")
                            {
                                if (Dt2.Rows[0]["MED_ITM"].ToString() != "")
                                    Dt["C_Mom_Med"] = Dt2.Rows[0]["MED_ITM"].ToString();
                                else
                                    Dt["C_Mom_Med"] = "有";
                            }
                        }
                        //母親病史-疾病史
                        if (Dt2.Rows[0]["DISE"].ToString() != "")
                        {
                            if (Dt2.Rows[0]["DISE"].ToString() == "0")
                                Dt["C_Mom_Dise"] = "無";
                            if (Dt2.Rows[0]["DISE"].ToString() == "1")
                            {
                                if (Dt2.Rows[0]["DISE_ITM"].ToString() != "")
                                {
                                    var Item = new List<string>()
                                    {
                                        "肝炎","梅毒","驚厥","腫瘤",
                                        "氣喘","肺結核","高血壓","心臟病","肝臟疾病","腎臟疾病",
                                         "痛風","糖尿病","甲狀腺疾病","中風","其他"
                                    };
                                    var DiseItem = Dt2.Rows[0]["DISE_ITM"].ToString();
                                    var DiseItem_val = new List<string>();
                                    for (var i = 0; i < DiseItem.Length; i++)
                                    {
                                        if (DiseItem[i] == '1' && i != 14)
                                            DiseItem_val.Add(Item[i]);
                                        if (i == 14 && Dt2.Rows[0]["DISE_ITM_OTH"].ToString() != "")
                                            DiseItem_val.Add(Dt2.Rows[0]["DISE_ITM_OTH"].ToString());
                                    }
                                    Dt["C_Mom_Dise"] = String.Join(",", DiseItem_val);
                                }
                                else
                                    Dt["C_Mom_Dise"] = "有";
                            }
                        }
                        //母親病史-妊娠併發症
                        if (Dt2.Rows[0]["PREG"].ToString() != "")
                        {
                            if (Dt2.Rows[0]["PREG"].ToString() == "0")
                                Dt["C_Mom_Preg"] = "無";
                            if (Dt2.Rows[0]["PREG"].ToString() == "1")
                            {
                                if (Dt2.Rows[0]["PREG_ITM"].ToString() != "")
                                {
                                    var Item = new List<string>()
                                    {
                                        "妊娠高血壓","嚴重妊娠高血壓","子癲前症","中度子癲前症",
                                        "重度子癲前症","絨毛膜羊膜囊炎","前置胎盤","胎盤早期剝離",
                                         "妊娠糖尿病","心臟病","腎臟疾病","免疫性疾病(SLE)","其他"
                                    };
                                    var PregItem = Dt2.Rows[0]["PREG_ITM"].ToString();
                                    var PregItem_val = new List<string>();
                                    for (var i = 0; i < PregItem.Length; i++)
                                    {
                                        if (PregItem[i] == '1' && i != 12)
                                            PregItem_val.Add(Item[i]);
                                        if (i == 12 && Dt2.Rows[0]["PREG_ITM_OTH"].ToString() != "")
                                            PregItem_val.Add(Dt2.Rows[0]["PREG_ITM_OTH"].ToString());
                                    }
                                    Dt["C_Mom_Preg"] = String.Join(",", PregItem_val);
                                }
                                else
                                    Dt["C_Mom_Preg"] = "有";
                            }
                        }

                        //HBeAg
                        if (Dt2.Rows[0]["HBEAG"].ToString() != "")
                        {
                            switch (Dt2.Rows[0]["HBEAG"].ToString())
                            {
                                case "0":
                                    Dt["C_HBeAg"] = "+陽性";
                                    break;
                                case "1":
                                    Dt["C_HBeAg"] = "-陰性";
                                    break;
                                case "2":
                                    Dt["C_HBeAg"] = "F偽陽";
                                    break;
                                case "3":
                                    Dt["C_HBeAg"] = "X未驗";
                                    break;
                                case "4":
                                    Dt["C_HBeAg"] = "外院";
                                    break;
                                default:
                                    break;
                            }
                        }
                        //HBsAG
                        if (Dt2.Rows[0]["HBSAG"].ToString() != "")
                        {
                            switch (Dt2.Rows[0]["HBSAG"].ToString())
                            {
                                case "0":
                                    Dt["C_HBsAG"] = "+陽性";
                                    break;
                                case "1":
                                    Dt["C_HBsAG"] = "-陰性";
                                    break;
                                case "2":
                                    Dt["C_HBsAG"] = "F偽陽";
                                    break;
                                case "3":
                                    Dt["C_HBsAG"] = "X未驗";
                                    break;
                                case "4":
                                    Dt["C_HBsAG"] = "外院";
                                    break;
                                default:
                                    break;
                            }
                        }
                        //Rubella
                        if (Dt2.Rows[0]["RUBELLA"].ToString() != "")
                        {
                            switch (Dt2.Rows[0]["RUBELLA"].ToString())
                            {
                                case "0":
                                    Dt["C_Rubella"] = "+陽性";
                                    break;
                                case "1":
                                    Dt["C_Rubella"] = "-陰性";
                                    break;
                                case "2":
                                    Dt["C_Rubella"] = "F偽陽";
                                    break;
                                case "3":
                                    Dt["C_Rubella"] = "X未驗";
                                    break;
                                case "4":
                                    Dt["C_Rubella"] = "外院";
                                    break;
                                default:
                                    break;
                            }
                        }
                        //VDRL
                        if (Dt2.Rows[0]["VDRL1"].ToString() != "")
                        {
                            switch (Dt2.Rows[0]["VDRL1"].ToString())
                            {
                                case "0":
                                    Dt["C_VDRL"] = "+陽性";
                                    break;
                                case "1":
                                    Dt["C_VDRL"] = "-陰性";
                                    break;
                                case "2":
                                    Dt["C_VDRL"] = "F偽陽";
                                    break;
                                case "3":
                                    Dt["C_VDRL"] = "X未驗";
                                    break;
                                case "4":
                                    Dt["C_VDRL"] = "外院";
                                    break;
                                default:
                                    break;
                            }
                        }
                        //VDRL2
                        if (Dt2.Rows[0]["VDRL2"].ToString() != "")
                        {
                            switch (Dt2.Rows[0]["VDRL2"].ToString())
                            {
                                case "0":
                                    Dt["C_VDRL2"] = "+陽性";
                                    break;
                                case "1":
                                    Dt["C_VDRL2"] = "-陰性";
                                    break;
                                case "2":
                                    Dt["C_VDRL2"] = "F偽陽";
                                    break;
                                case "3":
                                    Dt["C_VDRL2"] = "X未驗";
                                    break;
                                case "4":
                                    Dt["C_VDRL2"] = "外院";
                                    break;
                                default:
                                    break;
                            }
                        }
                        //HIV(快篩)
                        if (Dt2.Rows[0]["HIV"].ToString() != "")
                        {
                            switch (Dt2.Rows[0]["HIV"].ToString())
                            {
                                case "0":
                                    Dt["C_HIV"] = "+陽性";
                                    break;
                                case "1":
                                    Dt["C_HIV"] = "-陰性";
                                    break;
                                case "2":
                                    Dt["C_HIV"] = "F偽陽";
                                    break;
                                case "3":
                                    Dt["C_HIV"] = "X未驗";
                                    break;
                                case "4":
                                    Dt["C_HIV"] = "外院";
                                    break;
                                default:
                                    break;
                            }
                        }
                        //GBS，施打時間
                        if (Dt2.Rows[0]["GBS"].ToString() != "")
                        {
                            switch (Dt2.Rows[0]["GBS"].ToString())
                            {
                                case "0":
                                    Dt["C_GBS"] = "+陽性";
                                    break;
                                case "1":
                                    Dt["C_GBS"] = "-陰性";
                                    break;
                                case "2":
                                    Dt["C_GBS"] = "F偽陽";
                                    break;
                                case "3":
                                    Dt["C_GBS"] = "X未驗";
                                    break;
                                case "4":
                                    Dt["C_GBS"] = "外院";
                                    break;
                                default:
                                    break;
                            }
                        }

                        //接觸史TOCC
                        //媽媽
                        if (Dt2.Rows[0]["INF_MOM"].ToString() != "")
                        {
                            if (Dt2.Rows[0]["INF_MOM"].ToString() == "1")
                            {
                                Dt["C_Mom_TOCC"] = "<label class='LittleTitle'>媽媽</label>：有";
                                if (Dt2.Rows[0]["INF_MOM_SYM"].ToString() != "")
                                {
                                    var Item = new List<string>()
                                    {
                                        "發燒","腹瀉","咳嗽","流鼻水",
                                        "出疹子","其他"
                                    };
                                    var DiseItem = Dt2.Rows[0]["INF_MOM_SYM"].ToString();
                                    var DiseItem_val = new List<string>();
                                    for (var i = 0; i < DiseItem.Length; i++)
                                    {
                                        if (DiseItem[i] == '1' && i != 5)
                                            DiseItem_val.Add(Item[i]);
                                        if (i == 5 && Dt2.Rows[0]["INF_MOM_OTH"].ToString() != "")
                                            DiseItem_val.Add(Dt2.Rows[0]["INF_MOM_OTH"].ToString());
                                    }
                                    Dt["C_Mom_TOCC"] += "，" + String.Join(",", DiseItem_val);
                                }
                            }
                        }
                        //同住家人
                        if (Dt2.Rows[0]["INF_OTH"].ToString() != "")
                        {
                            if (Dt2.Rows[0]["INF_OTH"].ToString() == "1")
                            {
                                Dt["C_Mom_TOCC"] = $"{(Dt.ContainsKey("C_Mom_TOCC") ? Dt["C_Mom_TOCC"] + "<hr />" : "")}<label class='LittleTitle'>同住家人</label>：有";
                                if (Dt2.Rows[0]["INF_OTH_WHO"].ToString() != "")
                                    Dt["C_Mom_TOCC"] = Dt["C_Mom_TOCC"].Replace("同住家人", Dt2.Rows[0]["INF_OTH_WHO"].ToString());
                                if (Dt2.Rows[0]["INF_OTH_SYM"].ToString() != "")
                                {
                                    var Item = new List<string>()
                                    {
                                        "發燒","腹瀉","咳嗽","流鼻水",
                                        "出疹子","其他"
                                    };
                                    var DiseItem = Dt2.Rows[0]["INF_OTH_SYM"].ToString();
                                    var DiseItem_val = new List<string>();
                                    for (var i = 0; i < DiseItem.Length; i++)
                                    {
                                        if (DiseItem[i] == '1' && i != 5)
                                            DiseItem_val.Add(Item[i]);
                                        if (i == 5 && Dt2.Rows[0]["INF_OTH_OTH"].ToString() != "")
                                            DiseItem_val.Add(Dt2.Rows[0]["INF_OTH_OTH"].ToString());
                                    }
                                    Dt["C_Mom_TOCC"] += "，" + String.Join(",", DiseItem_val);
                                }
                            }
                        }
                        //生產前14天內，寶寶的哥哥、姊姊學校有無班上同學因為傳染病請假或班級停課之情形
                        if (Dt2.Rows[0]["BS_CLS"].ToString() != "")
                        {
                            if (Dt2.Rows[0]["BS_CLS"].ToString() == "1")
                            {
                                Dt["C_Mom_TOCC"] = $"{(Dt.ContainsKey("C_Mom_TOCC") ? Dt["C_Mom_TOCC"] + "<hr />" : "")}<label class='LittleTitle'>哥哥姊姊</label>：有";
                                if (Dt2.Rows[0]["BS_CLS_RS"].ToString() != "")
                                {
                                    var Item = new List<string>()
                                    {
                                        "腸病毒","流感","水痘","其他"
                                    };
                                    var DiseItem = Dt2.Rows[0]["BS_CLS_RS"].ToString();
                                    var DiseItem_val = new List<string>();
                                    for (var i = 0; i < DiseItem.Length; i++)
                                    {
                                        if (DiseItem[i] == '1' && i != 3)
                                            DiseItem_val.Add(Item[i]);
                                        if (i == 3 && Dt2.Rows[0]["BS_CLS_OTH"].ToString() != "")
                                            DiseItem_val.Add(Dt2.Rows[0]["BS_CLS_OTH"].ToString());
                                    }
                                    Dt["C_Mom_TOCC"] += "，" + String.Join(",", DiseItem_val);
                                }
                            }
                        }
                        //住院期間照顧者(應盡量維持同一人)，目前有無：發燒、腹瀉、咳嗽、流鼻水等疑似感染症狀
                        if (Dt2.Rows[0]["BS_CLS"].ToString() != "")
                            Dt["C_Mom_TOCC"] = $"{(Dt.ContainsKey("C_Mom_TOCC") ? Dt["C_Mom_TOCC"] + "<hr />" : "")}住院期間照顧者(應盡量維持同一人)，目前{(Dt2.Rows[0]["BS_CLS"].ToString() == "0" ? "無" : "有")}發燒、腹瀉、咳嗽、流鼻水等疑似感染症狀";
                    }
                    #endregion

                    #region 入院護理評估_嬰幼兒
                    sql = "SELECT * FROM OBS_BABYENTR "
                    + "WHERE FEENO = '" + feeno + "' AND DELETED IS NULL ORDER BY CREATTIME DESC";

                    Dt2 = link.DBExecSQL(sql);
                    if (Dt2.Rows.Count > 0)
                    {
                        //個人病史-服藥史
                        if (Dt2.Rows[0]["MED"].ToString() != "")
                        {
                            if (Dt2.Rows[0]["MED"].ToString() == "0")
                                Dt["C_Self_Med"] = "無";
                            if (Dt2.Rows[0]["MED"].ToString() == "1")
                            {
                                if (Dt2.Rows[0]["MED_ITM"].ToString() != "")
                                    Dt["C_Self_Med"] = Dt2.Rows[0]["MED_ITM"].ToString();
                                else
                                    Dt["C_Self_Med"] = "有";
                            }
                        }
                        //個人病史-疾病史
                        if (Dt2.Rows[0]["DISE"].ToString() != "")
                        {
                            if (Dt2.Rows[0]["DISE"].ToString() == "0")
                                Dt["C_Mom_Dise"] = "無";
                            if (Dt2.Rows[0]["DISE"].ToString() == "1")
                            {
                                if (Dt2.Rows[0]["DISE_ITM"].ToString() != "")
                                {
                                    var Item = new List<string>()
                                    {
                                        "氣喘","肺結核","心臟病","肝臟疾病",
                                        "腎臟疾病","糖尿病","感冒","其他"
                                    };
                                    var DiseItem = Dt2.Rows[0]["DISE_ITM"].ToString();
                                    var DiseItem_val = new List<string>();
                                    for (var i = 0; i < DiseItem.Length; i++)
                                    {
                                        if (DiseItem[i] == '1' && i != 7)
                                        {
                                            if (i == 6 && Dt2.Rows[0]["DISE_ITM_CD"].ToString() != "")
                                                DiseItem_val.Add(Item[i] + Dt2.Rows[0]["DISE_ITM_CD"].ToString() + "次/年");
                                            else
                                                DiseItem_val.Add(Item[i]);
                                        }
                                        if (i == 7 && Dt2.Rows[0]["DISE_OTH"].ToString() != "")
                                            DiseItem_val.Add(Dt2.Rows[0]["DISE_OTH"].ToString());
                                    }
                                    Dt["C_Self_Dise"] = String.Join(",", DiseItem_val);
                                }
                                else
                                    Dt["C_Self_Dise"] = "有";
                            }
                        }

                        //接觸史TOCC
                        //媽媽
                        if (Dt2.Rows[0]["INF_MOM"].ToString() != "")
                        {
                            if (Dt2.Rows[0]["INF_MOM"].ToString() == "1")
                            {
                                Dt["C_TOCC"] = "<label class='LittleTitle'>媽媽</label>：有";
                                if (Dt2.Rows[0]["INF_MOM_SYM"].ToString() != "")
                                {
                                    var Item = new List<string>()
                                    {
                                        "發燒","腹瀉","咳嗽","流鼻水",
                                        "出疹子","其他"
                                    };
                                    var DiseItem = Dt2.Rows[0]["INF_MOM_SYM"].ToString();
                                    var DiseItem_val = new List<string>();
                                    for (var i = 0; i < DiseItem.Length; i++)
                                    {
                                        if (DiseItem[i] == '1' && i != 5)
                                            DiseItem_val.Add(Item[i]);
                                        if (i == 5 && Dt2.Rows[0]["INF_MOM_OTH"].ToString() != "")
                                            DiseItem_val.Add(Dt2.Rows[0]["INF_MOM_OTH"].ToString());
                                    }
                                    Dt["C_TOCC"] += "，" + String.Join(",", DiseItem_val);
                                }
                            }
                        }
                        //同住家人
                        if (Dt2.Rows[0]["INF_OTH"].ToString() != "")
                        {
                            if (Dt2.Rows[0]["INF_OTH"].ToString() == "1")
                            {
                                Dt["C_TOCC"] = $"{(Dt.ContainsKey("C_TOCC") ? Dt["C_TOCC"] + "<hr />" : "")}<label class='LittleTitle'>同住家人</label>：有";
                                if (Dt2.Rows[0]["INF_OTH_WHO"].ToString() != "")
                                    Dt["C_TOCC"] = Dt["C_TOCC"].Replace("同住家人", Dt2.Rows[0]["INF_OTH_WHO"].ToString());
                                if (Dt2.Rows[0]["INF_OTH_SYM"].ToString() != "")
                                {
                                    var Item = new List<string>()
                                    {
                                        "發燒","腹瀉","咳嗽","流鼻水",
                                        "出疹子","其他"
                                    };
                                    var DiseItem = Dt2.Rows[0]["INF_OTH_SYM"].ToString();
                                    var DiseItem_val = new List<string>();
                                    for (var i = 0; i < DiseItem.Length; i++)
                                    {
                                        if (DiseItem[i] == '1' && i != 5)
                                            DiseItem_val.Add(Item[i]);
                                        if (i == 5 && Dt2.Rows[0]["INF_OTH_OTH"].ToString() != "")
                                            DiseItem_val.Add(Dt2.Rows[0]["INF_OTH_OTH"].ToString());
                                    }
                                    Dt["C_TOCC"] += "，" + String.Join(",", DiseItem_val);
                                }
                            }
                        }
                        //生產前14天內，寶寶的哥哥、姊姊學校有無班上同學因為傳染病請假或班級停課之情形
                        if (Dt2.Rows[0]["BS_CLS"].ToString() != "")
                        {
                            if (Dt2.Rows[0]["BS_CLS"].ToString() == "1")
                            {
                                Dt["C_TOCC"] = $"{(Dt.ContainsKey("C_TOCC") ? Dt["C_TOCC"] + "<hr />" : "")}<label class='LittleTitle'>哥哥姊姊</label>：有";
                                if (Dt2.Rows[0]["BS_CLS_RS"].ToString() != "")
                                {
                                    var Item = new List<string>()
                                    {
                                        "腸病毒","流感","水痘","其他"
                                    };
                                    var DiseItem = Dt2.Rows[0]["BS_CLS_RS"].ToString();
                                    var DiseItem_val = new List<string>();
                                    for (var i = 0; i < DiseItem.Length; i++)
                                    {
                                        if (DiseItem[i] == '1' && i != 3)
                                            DiseItem_val.Add(Item[i]);
                                        if (i == 3 && Dt2.Rows[0]["BS_CLS_OTH"].ToString() != "")
                                            DiseItem_val.Add(Dt2.Rows[0]["BS_CLS_OTH"].ToString());
                                    }
                                    Dt["C_TOCC"] += "，" + String.Join(",", DiseItem_val);
                                }
                            }
                        }
                        //住院期間照顧者(應盡量維持同一人)，目前有無：發燒、腹瀉、咳嗽、流鼻水等疑似感染症狀
                        if (Dt2.Rows[0]["BS_CLS"].ToString() != "")
                            Dt["C_TOCC"] = $"{(Dt.ContainsKey("C_TOCC") ? Dt["C_TOCC"] + "<hr />" : "")}住院期間照顧者(應盡量維持同一人)，目前{(Dt2.Rows[0]["BS_CLS"].ToString() == "0" ? "無" : "有")}發燒、腹瀉、咳嗽、流鼻水等疑似感染症狀";

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
                                Dt["RbnOxygen"] = RecordTime + " " + CombinedWord(Temp, "RbnFaceMask", "用氧方式", "");
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
                                                     + "，性狀：" + Temp.Find(x => x["Name"] == "RbnStoolNature")["Value"] + "、";
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

                                Dt["bt"] += string.Format("{0}<label class='LittleTitle'>體溫：</label><font color='" + color + "'>{1}</font> {2}。<br/>"
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
                                    , (measure == "測不到") ? measure : VitalSignList[i]["VS_RECORD"] + "%"
                                    , (!string.IsNullOrWhiteSpace(VitalSignList[i]["VS_MEMO"])) ? "，予" + VitalSignList[i]["VS_MEMO"] : "");
                            }
                        }
                        VitalSignList = Temp.FindAll(x => x["VS_ITEM"] == "bp" && !string.IsNullOrEmpty(x["VS_RECORD"].Replace("|", "").Replace(" ", "")));
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
                                                map = "(" + db + ")";
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
                                    , (measure == "測不到") ? "<font color = '" + color_h + "'>" + measure + "</font>" : " <font color = '" + color_h + "'>" + ck_value[0] + "</font> / <font color='" + color_l + "'>" + ck_value[1] + "</font>" + map + "mmHg"
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

                        //20190418 add by Alan
                        VitalSignList = Temp.FindAll(x => x["VS_ITEM"] == "bh" && !string.IsNullOrEmpty(x["VS_RECORD"]));
                        Dt["bh"] = "";
                        if (VitalSignList.Count > 0)
                        {
                            for (int i = 0; i < ((VitalSignList.Count < rownum) ? VitalSignList.Count : rownum); i++)
                            {
                                Dt["bh"] += string.Format("{0}<label class='LittleTitle'>身高：</label>{1} {2}。<br/>"
                                    , Convert.ToDateTime(VitalSignList[i]["CREATE_DATE"]).ToString("yyyy/MM/dd HH:mm")
                                    , VitalSignList[i]["VS_RECORD"].Replace("|", "")
                                    , (!string.IsNullOrWhiteSpace(VitalSignList[i]["VS_MEMO"])) ? "，予" + VitalSignList[i]["VS_MEMO"] : "");
                            }
                        }


                        VitalSignList = Temp.FindAll(x => x["VS_ITEM"] == "gi_j" && !string.IsNullOrEmpty(x["VS_RECORD"]));
                        Dt["gi_j"] = "";
                        if (VitalSignList.Count > 0)
                        {
                            for (int i = 0; i < ((VitalSignList.Count < rownum) ? VitalSignList.Count : rownum); i++)
                            {
                                Dt["gi_j"] += string.Format("{0}<label class='LittleTitle'>黃疸值：</label>{3} - {1} {2}。<br/>"
                                    , Convert.ToDateTime(VitalSignList[i]["CREATE_DATE"]).ToString("yyyy/MM/dd HH:mm")
                                    , VitalSignList[i]["VS_RECORD"].Replace("|", "")
                                    , (!string.IsNullOrWhiteSpace(VitalSignList[i]["VS_MEMO"])) ? "，予" + VitalSignList[i]["VS_MEMO"] : ""
                                    , VitalSignList[i]["VS_PART"]);
                            }
                        }

                        VitalSignList = Temp.FindAll(x => x["VS_ITEM"] == "gi_st" && !string.IsNullOrEmpty(x["VS_RECORD"]));
                        Dt["gi_st"] = "";
                        if (VitalSignList.Count > 0)
                        {
                            for (int i = 0; i < ((VitalSignList.Count < rownum) ? VitalSignList.Count : rownum); i++)
                            {
                                Dt["gi_st"] += string.Format("{0}<label class='LittleTitle'>大便潛血：</label>{1} {2}。<br/>"
                                    , Convert.ToDateTime(VitalSignList[i]["CREATE_DATE"]).ToString("yyyy/MM/dd HH:mm")
                                    , VitalSignList[i]["VS_RECORD"].Replace("|", "")
                                    , (!string.IsNullOrWhiteSpace(VitalSignList[i]["VS_MEMO"])) ? "，予" + VitalSignList[i]["VS_MEMO"] : "");
                            }
                        }

                        VitalSignList = Temp.FindAll(x => x["VS_ITEM"] == "si_inspect" && !string.IsNullOrEmpty(x["VS_RECORD"]));
                        Dt["si_inspect_e"] = "";
                        Dt["si_inspect_s"] = "";
                        Dt["si_inspect_ph"] = "";
                        if (VitalSignList.Count > 0)
                        {
                            for (int i = 0; i < ((VitalSignList.Count < rownum) ? VitalSignList.Count : rownum); i++)
                            {
                                string[] records = VitalSignList[i]["VS_RECORD"].Split('|');

                                Dt["si_inspect_e"] += string.Format("{0}<label class='LittleTitle'>蛋白質：</label>{1} {2}。<br/>"
                                    , Convert.ToDateTime(VitalSignList[i]["CREATE_DATE"]).ToString("yyyy/MM/dd HH:mm")
                                    , records[0].Trim()
                                    , (!string.IsNullOrWhiteSpace(VitalSignList[i]["VS_MEMO"])) ? "，予" + VitalSignList[i]["VS_MEMO"] : "");

                                Dt["si_inspect_s"] += string.Format("{0}<label class='LittleTitle'>尿糖：</label>{1} {2}。<br/>"
                                    , Convert.ToDateTime(VitalSignList[i]["CREATE_DATE"]).ToString("yyyy/MM/dd HH:mm")
                                    , records[1].Trim()
                                    , (!string.IsNullOrWhiteSpace(VitalSignList[i]["VS_MEMO"])) ? "，予" + VitalSignList[i]["VS_MEMO"] : "");

                                Dt["si_inspect_ph"] += string.Format("{0}<label class='LittleTitle'>PH：</label>{1} {2}。<br/>"
                                    , Convert.ToDateTime(VitalSignList[i]["CREATE_DATE"]).ToString("yyyy/MM/dd HH:mm")
                                    , records[2].Trim()
                                    , (!string.IsNullOrWhiteSpace(VitalSignList[i]["VS_MEMO"])) ? "，予" + VitalSignList[i]["VS_MEMO"] : "");
                            }
                        }
                        //20190418 add by Alan

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
                                            , error_str(ListWord[0], "GCS-E", ref alertstr, "GCS")
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
                                }
                                else if (r["MODEL_ID"].ToString() == "bsl_low" || r["MODEL_ID"].ToString() == "bsh_high")
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
                    Dt["O_URINE"] = ""; Dt["O_Defecation"] = ""; Dt["IO_BLOOD"] = ""; Dt["I_MILK"] = "";
                    double I = 0, O = 0, I_FOOD = 0, I_BIT = 0, O_PEE = 0, O_TUBE = 0, O_HD, I_TOTAL = 0, O_TOTAL = 0, HOUR_TOTAL = 0;
                    double O_URINE = 0, O_Defecation = 0, IO_BLOOD = 0, I_MILK = 0;
                    string I_FOOD_TITLE = "", I_BIT_TITLE = "", O_PEE_TITLE = "", O_TUBE_TITLE = "", O_HD_TITLE = "", TOTAL_LOSS = "";
                    string O_URINE_TITLE = "", O_Defecation_TITLE = "", IO_BLOOD_TITLE = "", I_MILK_TITLE = "";

                    IO_Temp = Temp.FindAll(x => x["CLASS"] == "D");
                    if (IO_Temp.Count > 0)
                    {
                        I = 0; O = 0; I_FOOD = 0; I_BIT = 0; O_PEE = 0; O_TUBE = 0; O_HD = 0;
                        O_URINE = 0; O_Defecation = 0; IO_BLOOD = 0; I_MILK = 0;
                        I_FOOD_TITLE = ""; I_BIT_TITLE = ""; O_PEE_TITLE = ""; O_TUBE_TITLE = ""; O_HD_TITLE = "";
                        O_URINE_TITLE = ""; O_Defecation_TITLE = ""; IO_BLOOD_TITLE = ""; I_MILK_TITLE = "";
                        foreach (var item in IO_Temp.FindAll(x => string.IsNullOrWhiteSpace(x["REASON"])))
                        {
                            if (item["P_GROUP"] == "intaketype")
                                I += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                            else
                                O += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                            switch (item["TYPEID"])
                            {
                                case "2":
                                    I_FOOD += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                                    I_FOOD_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["NAME"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    //20190509 modified by chih
                                    if (item["NAME"] == "母奶" || item["NAME"] == "配方奶")
                                    {
                                        I_MILK += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                                        I_MILK_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["NAME"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    }
                                    //20190509 modified by chih
                                    break;
                                case "1":
                                    I_BIT += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                                    I_BIT_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["NAME"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    break;
                                case "6":
                                    O_PEE += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                                    O_PEE_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["NAME"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    //20190509 modified by chih
                                    O_URINE += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                                    O_URINE_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["NAME"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    //20190509 modified by chih
                                    break;
                                case "9":
                                    O_TUBE += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                                    O_TUBE_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), IO_M.sel_item_name(item["ITEMID"]), item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    break;
                                case "8":
                                    O_HD += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                                    O_HD_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["EXPLANATION_ITEM"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    if (item["NAME"].ToString() == "失血量")
                                    {
                                        IO_BLOOD += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                                        IO_BLOOD_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["EXPLANATION_ITEM"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    }
                                    break;
                                case "7":
                                    O_Defecation += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                                    O_Defecation_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["EXPLANATION_ITEM"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    break;
                                case "4":
                                    if (item["NAME"].ToString().IndexOf("RBC") >= 0)
                                    {
                                        IO_BLOOD += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                                        IO_BLOOD_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["EXPLANATION_ITEM"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    }
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

                        //20190509 modified by chih
                        Dt["O_URINE"] += "<span title='" + O_URINE_TITLE + "'>" + DName + " 白班內：尿液 " + O_URINE + " ml。</span><br/>";
                        Dt["O_Defecation"] += "<span title='" + O_Defecation_TITLE + "'>" + DName + " 白班內：排便 " + O_Defecation + " g。</span><br/>";
                        Dt["IO_BLOOD"] += "<span title='" + IO_BLOOD_TITLE + "'>" + DName + " 白班內：血量累積 " + IO_BLOOD + " ml。</span><br/>";
                        Dt["I_MILK"] += "<span title='" + I_MILK_TITLE + "'>" + DName + " 白班內：奶量 " + I_MILK + " ml。</span><br/>";
                        //20190509 modified by chih
                    }

                    IO_Temp = Temp.FindAll(x => x["CLASS"] == "N");
                    if (IO_Temp.Count > 0)
                    {
                        I = 0; O = 0; I_FOOD = 0; I_BIT = 0; O_PEE = 0; O_TUBE = 0; O_HD = 0;
                        O_URINE = 0; O_Defecation = 0; IO_BLOOD = 0; I_MILK = 0;
                        I_FOOD_TITLE = ""; I_BIT_TITLE = ""; O_PEE_TITLE = ""; O_TUBE_TITLE = ""; O_HD_TITLE = "";
                        O_URINE_TITLE = ""; O_Defecation_TITLE = ""; IO_BLOOD_TITLE = ""; I_MILK_TITLE = "";
                        foreach (var item in IO_Temp.FindAll(x => string.IsNullOrWhiteSpace(x["REASON"])))
                        {
                            if (item["P_GROUP"] == "intaketype")
                                I += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                            else
                                O += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                            switch (item["TYPEID"])
                            {
                                case "2":
                                    I_FOOD += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                                    I_FOOD_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["NAME"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    //20190509 modified by chih
                                    if (item["NAME"] == "母奶" || item["NAME"] == "配方奶")
                                    {
                                        I_MILK += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                                        I_MILK_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["NAME"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    }
                                    //20190509 modified by chih
                                    break;
                                case "1":
                                    I_BIT += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                                    I_BIT_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["NAME"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    break;
                                case "6":
                                    O_PEE += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                                    O_PEE_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["NAME"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    //20190509 modified by chih
                                    O_URINE += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                                    O_URINE_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["NAME"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    //20190509 modified by chih
                                    break;
                                case "9":
                                    O_TUBE += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                                    O_TUBE_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), IO_M.sel_item_name(item["ITEMID"]), item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    break;
                                case "8":
                                    O_HD += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                                    O_HD_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["EXPLANATION_ITEM"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    if (item["NAME"].ToString() == "失血量")
                                    {
                                        IO_BLOOD += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                                        IO_BLOOD_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["EXPLANATION_ITEM"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    }
                                    break;
                                case "7":
                                    O_Defecation += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                                    O_Defecation_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["EXPLANATION_ITEM"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    break;
                                case "4":
                                    if (item["NAME"].ToString().IndexOf("RBC") >= 0)
                                    {
                                        IO_BLOOD += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                                        IO_BLOOD_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["EXPLANATION_ITEM"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    }
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
                        //20190509 modified by chih
                        Dt["O_URINE"] += "<span title='" + O_URINE_TITLE + "'>" + NName + " 晚班內：尿液 " + O_URINE + " ml。</span><br/>";
                        Dt["O_Defecation"] += "<span title='" + O_Defecation_TITLE + "'>" + NName + " 晚班內：排便 " + O_Defecation + " g。</span><br/>";
                        Dt["IO_BLOOD"] += "<span title='" + IO_BLOOD_TITLE + "'>" + DName + " 晚班內：血量累積 " + IO_BLOOD + " ml。</span><br/>";
                        Dt["I_MILK"] += "<span title='" + I_MILK_TITLE + "'>" + DName + " 晚班內：奶量 " + I_MILK + " ml。</span><br/>";
                        //20190509 modified by chih
                    }

                    IO_Temp = Temp.FindAll(x => x["CLASS"] == "E");
                    if (IO_Temp.Count > 0)
                    {
                        I = 0; O = 0; I_FOOD = 0; I_BIT = 0; O_PEE = 0; O_TUBE = 0; O_HD = 0;
                        O_URINE = 0; O_Defecation = 0; IO_BLOOD = 0; I_MILK = 0;
                        I_FOOD_TITLE = ""; I_BIT_TITLE = ""; O_PEE_TITLE = ""; O_TUBE_TITLE = ""; O_HD_TITLE = "";
                        O_URINE_TITLE = ""; O_Defecation_TITLE = ""; IO_BLOOD_TITLE = ""; I_MILK_TITLE = "";
                        foreach (var item in IO_Temp.FindAll(x => string.IsNullOrWhiteSpace(x["REASON"])))
                        {
                            if (item["P_GROUP"] == "intaketype")
                                I += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                            else
                                O += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                            switch (item["TYPEID"])
                            {
                                case "2":
                                    I_FOOD += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                                    I_FOOD_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["NAME"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    //20190509 modified by chih
                                    if (item["NAME"] == "母奶" || item["NAME"] == "配方奶")
                                    {
                                        I_MILK += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                                        I_MILK_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["NAME"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    }
                                    //20190509 modified by chih
                                    break;
                                case "1":
                                    I_BIT += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                                    I_BIT_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["NAME"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    break;
                                case "6":
                                    O_PEE += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                                    O_PEE_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["NAME"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    //20190509 modified by chih
                                    O_URINE += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                                    O_URINE_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["NAME"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    //20190509 modified by chih
                                    break;
                                case "9":
                                    O_TUBE += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                                    O_TUBE_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), IO_M.sel_item_name(item["ITEMID"]), item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    break;
                                case "8":
                                    O_HD += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                                    O_HD_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["EXPLANATION_ITEM"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    if (item["NAME"].ToString() == "失血量")
                                    {
                                        IO_BLOOD += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                                        IO_BLOOD_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["EXPLANATION_ITEM"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    }
                                    break;
                                case "7":
                                    O_Defecation += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                                    O_Defecation_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["EXPLANATION_ITEM"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    break;
                                case "4":
                                    if (item["NAME"].ToString().IndexOf("RBC") >= 0)
                                    {
                                        IO_BLOOD += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                                        IO_BLOOD_TITLE += string.Format("{0} 項目：{1}，量：{2} {3}\r\n", Convert.ToDateTime(item["CREATTIME"]).ToString("yyyy/MM/dd HH:mm"), item["EXPLANATION_ITEM"], item["AMOUNT"], item["AMOUNT_UNIT"].Replace("1", "mL").Replace("2", "g").Replace("3", "mg"));
                                    }
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
                        //20190509 modified by chih
                        Dt["O_URINE"] += "<span title='" + O_URINE_TITLE + "'>" + EName + " 大夜班內：尿液 " + O_URINE + " ml。</span><br/>";
                        Dt["O_Defecation"] += "<span title='" + O_Defecation_TITLE + "'>" + EName + " 大夜班內：排便 " + O_Defecation + " g。</span><br/>";
                        Dt["IO_BLOOD"] += "<span title='" + IO_BLOOD_TITLE + "'>" + DName + " 大夜班內：血量累積 " + IO_BLOOD + " ml。</span><br/>";
                        Dt["I_MILK"] += "<span title='" + I_MILK_TITLE + "'>" + DName + " 大夜班內：奶量 " + I_MILK + " ml。</span><br/>";
                        //20190509 modified by chih
                    }
                    Dt["IO"] += I_TOTAL.ToString() + " ml/" + O_TOTAL.ToString() + " ml(" + (I_TOTAL - O_TOTAL).ToString()
                        + TOTAL_LOSS + "/" + HOUR_TOTAL + "小時)";


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
                            }
                            else
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
                                I += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                            else
                                O += double.Parse(item["AMOUNT_ALL"].ToString() == "" ? "0" : item["AMOUNT_ALL"].ToString());
                        }
                    }

                    Dt["IO_beforeday"] += I.ToString() + " ml/" + O.ToString() + " ml(" + (I - O).ToString()
                        + ((IO_Temp.Exists(x => !string.IsNullOrWhiteSpace(x["REASON"]))) ? "+Loss" : "") + "/24小時)<br/>";

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
                                , item.OrderNo, '"' + feeno + '"');
                                break;
                            }
                            if (Temp_String.Length > 5)
                                Dt["Consultation"] = Temp_String.Substring(0, Temp_String.Length - 5);
                        }
                    }


                    Dt["Condition"] = Consultation;
                    Dt["AlertStr"] = "";
                    if (alertstr.Count > 0)
                    {
                        Dt["AlertStr"] = string.Join("、", alertstr);
                    }


                    #endregion

                    #region 產程監測 抗生素時間
                    var A_GBSTime = "";
                    var A_GBSReason = "";
                    DataTable dtPrc = obs_m.sel_Instantly_Be_Birth(ptinfo.FeeNo, "", "");
                    if (dtPrc != null && dtPrc.Rows.Count > 0)
                    {
                        for (var a = dtPrc.Rows.Count - 1; a >= 0; a--)
                        {
                            var iid = dtPrc.Rows[a]["IID"].ToString();

                            var dtlSql = $@"SELECT * FROM OBS_BPDISP 
WHERE IID = '{iid}' AND TM_MED_ANTIBIOTIC != '00000' ORDER BY CREATTIME";
                            DataTable DtBpDisp = obs_m.DBExecSQL(dtlSql);
                            if (DtBpDisp != null && DtBpDisp.Rows.Count > 0)
                            {
                                foreach (DataRow bp_r in DtBpDisp.Rows)
                                {
                                    var ANTIBIOTIC = bp_r["TM_MED_ANTIBIOTIC"].ToString();
                                    if (ANTIBIOTIC != "" && ANTIBIOTIC != "00000")
                                    {
                                        //20200207
                                        //A_GBSTime = Convert.ToDateTime(dtPrc.Rows[a]["RECORDTIME"].ToString()).ToString("yyyy/MM/dd HH:mm");
                                        if (A_GBSTime == "")
                                            A_GBSTime = Convert.ToDateTime(dtPrc.Rows[a]["RECORDTIME"].ToString()).ToString("yyyy/MM/dd HH:mm");

                                        var ANTIBIOTICV = new List<string>();
                                        for (var p = 0; p < ANTIBIOTIC.ToString().Length; p++)
                                        {
                                            if (ANTIBIOTIC[p] == '1')
                                            {
                                                if (p == 0)
                                                {
                                                    ANTIBIOTICV.Add("GBS");
                                                }
                                                if (p == 1)
                                                {
                                                    ANTIBIOTICV.Add("發燒");
                                                }
                                                if (p == 2)
                                                {
                                                    ANTIBIOTICV.Add("破水");
                                                }
                                                if (p == 3)
                                                {
                                                    ANTIBIOTICV.Add("感染");
                                                }
                                                if (p == 4)
                                                {
                                                    ANTIBIOTICV.Add("其他");
                                                }
                                            }
                                        }
                                        A_GBSReason = String.Join(",", ANTIBIOTICV);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    #endregion

                    #region 生產史

                    //生產史
                    sql = "SELECT * FROM OBS_BTHHIS "
                    + "WHERE FEENO = '" + feeno + "' AND DELETED IS NULL ORDER BY RECORDTIME DESC";

                    Dt2 = link.DBExecSQL(sql);
                    var A_GPAE = "#G#、#P#、#A#、#E#";


                    if (Dt2.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt2.Rows.Count; i++)
                        {
                            //G.P.A.E. - GAE
                            if (Dt2.Rows[i]["GRAVIDA"].ToString() != "")
                                A_GPAE = A_GPAE.Replace("#G#", "G" + Dt2.Rows[i]["GRAVIDA"].ToString());
                            else
                                A_GPAE = A_GPAE.Replace("#G#", "無");

                            if (Dt2.Rows[i]["ABORTION"].ToString() != "")
                                A_GPAE = A_GPAE.Replace("#A#", "A" + Dt2.Rows[i]["ABORTION"].ToString());
                            else
                                A_GPAE = A_GPAE.Replace("#A#", "無");

                            if (Dt2.Rows[i]["ECCYESIS"].ToString() != "")
                                A_GPAE = A_GPAE.Replace("#E#", "E" + Dt2.Rows[i]["ECCYESIS"].ToString());
                            else
                                A_GPAE = A_GPAE.Replace("#E#", "無");

                            //預產期
                            if (Dt2.Rows[i]["EDC"].ToString() != "")
                            {
                                Dt["A_EDC"] = Convert.ToDateTime(Dt2.Rows[i]["EDC"].ToString()).ToString("yyyy/MM/dd");
                            }
                            //HBeAg
                            if (Dt2.Rows[i]["HBEAG"].ToString() != "")
                            {
                                switch (Dt2.Rows[i]["HBEAG"].ToString())
                                {
                                    case "0":
                                        Dt["A_HBeAg"] = "+陽性";
                                        break;
                                    case "1":
                                        Dt["A_HBeAg"] = "-陰性";
                                        break;
                                    case "2":
                                        Dt["A_HBeAg"] = "F偽陽";
                                        break;
                                    case "3":
                                        Dt["A_HBeAg"] = "X未驗";
                                        break;
                                    case "4":
                                        Dt["A_HBeAg"] = "外院";
                                        break;
                                    default:
                                        break;
                                }
                            }
                            //HBsAG
                            if (Dt2.Rows[i]["HBSAG"].ToString() != "")
                            {
                                switch (Dt2.Rows[i]["HBSAG"].ToString())
                                {
                                    case "0":
                                        Dt["A_HBsAG"] = "+陽性";
                                        break;
                                    case "1":
                                        Dt["A_HBsAG"] = "-陰性";
                                        break;
                                    case "2":
                                        Dt["A_HBsAG"] = "F偽陽";
                                        break;
                                    case "3":
                                        Dt["A_HBsAG"] = "X未驗";
                                        break;
                                    case "4":
                                        Dt["A_HBsAG"] = "外院";
                                        break;
                                    default:
                                        break;
                                }
                            }
                            //Rubella
                            if (Dt2.Rows[i]["RUBELLA"].ToString() != "")
                            {
                                switch (Dt2.Rows[i]["RUBELLA"].ToString())
                                {
                                    case "0":
                                        Dt["A_Rubella"] = "+陽性";
                                        break;
                                    case "1":
                                        Dt["A_Rubella"] = "-陰性";
                                        break;
                                    case "2":
                                        Dt["A_Rubella"] = "F偽陽";
                                        break;
                                    case "3":
                                        Dt["A_Rubella"] = "X未驗";
                                        break;
                                    case "4":
                                        Dt["A_Rubella"] = "外院";
                                        break;
                                    default:
                                        break;
                                }
                            }
                            //VDRL
                            if (Dt2.Rows[i]["VDRL1"].ToString() != "")
                            {
                                switch (Dt2.Rows[i]["VDRL1"].ToString())
                                {
                                    case "0":
                                        Dt["A_VDRL"] = "+陽性";
                                        break;
                                    case "1":
                                        Dt["A_VDRL"] = "-陰性";
                                        break;
                                    case "2":
                                        Dt["A_VDRL"] = "F偽陽";
                                        break;
                                    case "3":
                                        Dt["A_VDRL"] = "X未驗";
                                        break;
                                    case "4":
                                        Dt["A_VDRL"] = "外院";
                                        break;
                                    default:
                                        break;
                                }
                            }
                            //VDRL2
                            if (Dt2.Rows[i]["VDRL2"].ToString() != "")
                            {
                                switch (Dt2.Rows[i]["VDRL2"].ToString())
                                {
                                    case "0":
                                        Dt["A_VDRL2"] = "+陽性";
                                        break;
                                    case "1":
                                        Dt["A_VDRL2"] = "-陰性";
                                        break;
                                    case "2":
                                        Dt["A_VDRL2"] = "F偽陽";
                                        break;
                                    case "3":
                                        Dt["A_VDRL2"] = "X未驗";
                                        break;
                                    case "4":
                                        Dt["A_VDRL2"] = "外院";
                                        break;
                                    default:
                                        break;
                                }
                            }
                            //HIV(快篩)
                            if (Dt2.Rows[i]["HIV"].ToString() != "")
                            {
                                switch (Dt2.Rows[i]["HIV"].ToString())
                                {
                                    case "0":
                                        Dt["A_HIV"] = "+陽性";
                                        break;
                                    case "1":
                                        Dt["A_HIV"] = "-陰性";
                                        break;
                                    case "2":
                                        Dt["A_HIV"] = "F偽陽";
                                        break;
                                    case "3":
                                        Dt["A_HIV"] = "X未驗";
                                        break;
                                    case "4":
                                        Dt["A_HIV"] = "外院";
                                        break;
                                    default:
                                        break;
                                }
                            }
                            //GBS，施打時間
                            if (Dt2.Rows[i]["GBS"].ToString() != "")
                            {
                                switch (Dt2.Rows[i]["GBS"].ToString())
                                {
                                    case "0":
                                        Dt["A_GBS"] = "+陽性";
                                        break;
                                    case "1":
                                        Dt["A_GBS"] = "-陰性";
                                        break;
                                    case "2":
                                        Dt["A_GBS"] = "F偽陽";
                                        break;
                                    case "3":
                                        Dt["A_GBS"] = "X未驗";
                                        break;
                                    case "4":
                                        Dt["A_GBS"] = "外院";
                                        break;
                                    default:
                                        break;
                                }

                                //if (Dt2.Rows[i]["GBS"].ToString() == "0")
                                //{
                                //    if (Dt2.Rows[i]["GBS_AB"].ToString() != "")
                                //    {
                                //        A_GBSTime = Convert.ToDateTime(Dt2.Rows[i]["GBS_AB"].ToString()).ToString("yyyy/MM/dd HH:mm");
                                //    }
                                //}                                
                            }

                            //20200210  注射時間
                            if (Dt2.Rows[i]["GBS_AB"].ToString() != "")
                            {
                                if (A_GBSTime == "")
                                    A_GBSTime = Convert.ToDateTime(Dt2.Rows[i]["GBS_AB"].ToString()).ToString("yyyy/MM/dd HH:mm");
                            }

                            //餵食選擇
                            if (Dt2.Rows[i]["FEED"].ToString() != "")
                            {
                                var formula = Dt2.Rows[i]["FEED"].ToString();
                                if (formula == "0")
                                    Dt["A_Formula"] = "純母乳";
                                else if (formula == "1")
                                    Dt["A_Formula"] = "混合乳";
                                else if (formula == "2")
                                    Dt["A_Formula"] = "配方奶";
                                else if (formula == "3")
                                    Dt["A_Formula"] = "尚未決定";
                            }
                            //親子同室
                            if (Dt2.Rows[i]["ROOM_IN"].ToString() != "")
                            {
                                var room_in = Dt2.Rows[i]["ROOM_IN"].ToString();
                                if (room_in == "0")
                                    Dt["A_RoomIn"] = "24小時";
                                else if (room_in == "1")
                                    Dt["A_RoomIn"] = "部分時段";
                                else if (room_in == "2")
                                    Dt["A_RoomIn"] = "分離照顧";
                            }
                        }
                    }
                    #endregion

                    #region 第二、三產程

                    //第二、三產程
                    sql = "SELECT * FROM OBS_BTHSTA "
                    + "WHERE FEENO = '" + feeno + "' AND DELETED IS NULL ORDER BY RECORDTIME DESC";

                    Dt2 = link.DBExecSQL(sql);
                    if (Dt2.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt2.Rows.Count; i++)
                        {
                            //G.P.A.E. - P
                            if (Dt2.Rows[i]["PARITY_N"].ToString() != "")
                                A_GPAE = A_GPAE.Replace("#P#", "P" + Dt2.Rows[i]["PARITY_N"].ToString());
                            else
                                A_GPAE = A_GPAE.Replace("#P#", "無");
                            //總活產數
                            if (Dt2.Rows[i]["LB_TOTAL_N"].ToString() != "")
                            {
                                Dt["A_VB"] = Dt2.Rows[i]["LB_TOTAL_N"].ToString();
                            }
                            //生產方式
                            if (Dt2.Rows[i]["BIRTH_TYPE"].ToString() != "")
                            {
                                var birthType = Dt2.Rows[i]["BIRTH_TYPE"].ToString();
                                if (birthType == "0")
                                    Dt["A_BirthType"] = "自然產";
                                else if (birthType == "1")
                                    Dt["A_BirthType"] = "剖腹產";
                            }
                            //妊娠週數
                            if (Dt2.Rows[i]["GEST_M"].ToString() != "")
                            {
                                var gest = $"{Dt2.Rows[i]["GEST_M"].ToString()}週";
                                if (Dt2.Rows[i]["GEST_D"].ToString() != "")
                                    Dt["A_Gest"] = $"{gest} {Dt2.Rows[i]["GEST_D"].ToString()}天";
                                else
                                    Dt["A_Gest"] = gest;
                            }
                            else
                                Dt["A_Gest"] = "?";

                            var CSReason = "<label class='LittleTitle'>主要原因：#CS_MR#</label><hr /><label class='LittleTitle'>次要原因：#CS_OR#</label>";
                            //C/S主要原因
                            if (Dt2.Rows[i]["CS_MR"].ToString() != "")
                                CSReason = CSReason.Replace("#CS_MR#", Dt2.Rows[i]["CS_MR"].ToString());
                            else
                                CSReason = CSReason.Replace("#CS_MR#", "無");
                            //C/S次要原因
                            if (Dt2.Rows[i]["CS_OR"].ToString() != "")
                                CSReason = CSReason.Replace("#CS_OR#", Dt2.Rows[i]["CS_OR"].ToString());
                            else
                                CSReason = CSReason.Replace("#CS_OR#", "無");

                            Dt["A_CSReason"] = CSReason;

                            //破水時間
                            if (Dt2.Rows[i]["RFM_TIME"].ToString() != "")
                                Dt["A_GRFMTime"] = Convert.ToDateTime(Dt2.Rows[i]["RFM_TIME"]).ToString("yyyy/MM/dd HH:mm");

                            //總破水時間
                            if (Dt2.Rows[i]["BIRTH"].ToString() != "" && Dt2.Rows[i]["RFM_TIME"].ToString() != "")
                            {
                                var GRuptureTime = Convert.ToDateTime(Dt2.Rows[i]["BIRTH"]).Subtract(Convert.ToDateTime(Dt2.Rows[i]["RFM_TIME"]));
                                Dt["A_GRuptureTime"] = GRuptureTime.Days.ToString() + "天" + GRuptureTime.Hours.ToString() + "小時" + GRuptureTime.Minutes.ToString() + "分鐘" + GRuptureTime.Seconds.ToString() + "秒";
                            }

                            //施打抗生素時間與生產時間間隔4小時以內需顯示紅字
                            if (Dt2.Rows[i]["BIRTH"].ToString() != "" && A_GBSTime != "")
                            {
                                var spanTime = Convert.ToDateTime(Dt2.Rows[i]["BIRTH"]).Subtract(Convert.ToDateTime(A_GBSTime));
                                if (Math.Abs(spanTime.Days) > 0 || Math.Abs(spanTime.Hours) >= 4)
                                    Dt["A_GBSTime"] = $"<label class='RedContent'>{A_GBSTime} {A_GBSReason}</label>";
                                else
                                    Dt["A_GBSTime"] = A_GBSTime + " " + A_GBSReason;
                            }
                            else
                            {
                                if (A_GBSTime != "")
                                    Dt["A_GBSTime"] = A_GBSTime + " " + A_GBSReason;
                            }


                        }
                    }
                    else
                    {
                        if (A_GBSTime != "")
                            Dt["A_GBSTime"] = A_GBSTime + " " + A_GBSReason;
                    }
                    Dt["A_GPAE"] = A_GPAE;
                    #endregion

                    #region 高危險妊娠
                    byte[] BaByPatInfolistByteCode = webService.GetBaByPatInfo(ptinfo?.FeeNo);
                    if (BaByPatInfolistByteCode == null)
                        Dt["A_Pregnancy"] = "";
                    else
                    {
                        string listJsonArray = CompressTool.DecompressString(BaByPatInfolistByteCode);
                        BaByPatInfo_FeeNo[] baByPatInfo = JsonConvert.DeserializeObject<BaByPatInfo_FeeNo[]>(listJsonArray);
                        Dt["A_Pregnancy"] = baByPatInfo[0].PATPregnancy;
                    }
                    #endregion

                    #region 母嬰同室
                    if (ptinfo.Age < 1)
                    {
                        sql = $@"SELECT * FROM OBS_SKTSK WHERE 0 = 0 AND DELETED IS NULL 
                            AND NB_CHARTNO = '{ptinfo.ChartNo.PadLeft(10, '0')}' ORDER BY START_TIME DESC";
                        Dt2 = link.DBExecSQL(sql);
                        if (Dt2.Rows.Count > 0)
                        {
                            if (Dt2.Rows[0]["START_TIME"].ToString() != "" && Dt2.Rows[0]["END_TIME"].ToString() != "")
                            {
                                Dt["C_RoomInStatus"] = "否";
                            }
                            else
                            {
                                Dt["C_RoomInStatus"] = "是";
                            }
                        }
                    }
                    else
                    {
                        var sk_result = new List<string>();
                        DataTable dt_nb = new Obstetrics().sel_nbcha("", "", ptinfo.FeeNo);
                        if (dt_nb != null && dt_nb.Rows.Count > 0)
                        {
                            foreach (DataRow r in dt_nb.Rows)
                            {
                                sql = $@"SELECT * FROM OBS_ROOMIN WHERE 0 = 0 AND DELETED IS NULL 
                                        AND NB_CHARTNO = '{ r["BABY_CHART_NO"].ToString().PadLeft(10, '0') }' 
                                        AND SEQ_OF_NB = '{ r["BABY_SEQ"].ToString() }' 
                                        ORDER BY START_TIME DESC";
                                Dt2 = link.DBExecSQL(sql);
                                if (Dt2.Rows.Count > 0)
                                {
                                    if (Dt2.Rows[0]["START_TIME"].ToString() != "" && Dt2.Rows[0]["END_TIME"].ToString() != "")
                                    {
                                        sk_result.Add($"新生兒{Dt2.Rows[0]["SEQ_OF_NB"].ToString() }：否");
                                    }
                                    else
                                    {
                                        sk_result.Add($"新生兒{Dt2.Rows[0]["SEQ_OF_NB"].ToString() }：是");
                                    }
                                }
                            }
                        }
                        Dt["A_RoomInStatus"] = String.Join(",", sk_result);
                    }
                    #endregion

                    #region 肌膚接觸
                    if (ptinfo.Age < 1)
                    {
                        sql = $@"SELECT * FROM OBS_SKTSK WHERE 0 = 0 AND DELETED IS NULL 
                            AND NB_CHARTNO = '{ptinfo.ChartNo.PadLeft(10, '0')}' ORDER BY START_TIME DESC";
                        Dt2 = link.DBExecSQL(sql);
                        if (Dt2.Rows.Count > 0)
                        {
                            if (Dt2.Rows[0]["START_TIME"].ToString() != "" && Dt2.Rows[0]["END_TIME"].ToString() != "")
                            {
                                Dt["C_SkinTouchStatus"] = "否";
                            }
                            else
                            {
                                Dt["C_SkinTouchStatus"] = "是";
                            }
                        }
                    }
                    else
                    {
                        var sk_result = new List<string>();
                        DataTable dt_nb = new Obstetrics().sel_nbcha("", "", ptinfo.FeeNo);
                        if (dt_nb != null && dt_nb.Rows.Count > 0)
                        {
                            foreach (DataRow r in dt_nb.Rows)
                            {
                                sql = $@"SELECT * FROM OBS_SKTSK WHERE 0 = 0 AND DELETED IS NULL 
                                        AND NB_CHARTNO = '{ r["BABY_CHART_NO"].ToString().PadLeft(10, '0') }' 
                                        AND SEQ_OF_NB = '{ r["BABY_SEQ"].ToString() }' 
                                        ORDER BY START_TIME DESC";
                                Dt2 = link.DBExecSQL(sql);
                                if (Dt2.Rows.Count > 0)
                                {
                                    if (Dt2.Rows[0]["START_TIME"].ToString() != "" && Dt2.Rows[0]["END_TIME"].ToString() != "")
                                    {
                                        sk_result.Add($"新生兒{Dt2.Rows[0]["SEQ_OF_NB"].ToString() }：否");
                                    }
                                    else
                                    {
                                        sk_result.Add($"新生兒{Dt2.Rows[0]["SEQ_OF_NB"].ToString() }：是");
                                    }
                                }
                            }
                        }
                        Dt["A_SkinTouchStatus"] = String.Join(",", sk_result);
                    }
                    #endregion

                    #region 愛丁堡評估

                    //愛丁堡評估
                    sql = "SELECT SCORE, MEMO, RECORDTIME FROM OBS_EPDS "
            + "WHERE FEENO = '" + feeno + "' AND DELETED IS NULL ORDER BY RECORDTIME DESC";

                    Dt2 = link.DBExecSQL(sql);
                    if (Dt2.Rows.Count > 0)
                    {
                        Dt["A_EPDSScore"] = "<label class='LittleTitle'>總分</label>：#score#分<hr /><label class='LittleTitle'>處置建議</label>：#memo#";

                        Dt["A_EPDSScore"] = Dt["A_EPDSScore"].Replace("#score#", Dt2.Rows[0]["SCORE"].ToString() == "" ? "0" : Dt2.Rows[0]["SCORE"].ToString());
                        Dt["A_EPDSScore"] = Dt["A_EPDSScore"].Replace("#memo#", Dt2.Rows[0]["MEMO"].ToString() == "" ? "無" : Dt2.Rows[0]["MEMO"].ToString());
                    }
                    #endregion

                    #region 母嬰護理評估

                    sql = "SELECT * FROM OBS_PATNB "
            + "WHERE FEENO = '" + feeno + "' AND DELETED IS NULL ";

                    var lastSql = "SELECT * FROM OBS_PATNB "
            + "WHERE FEENO = '" + feeno + "' AND DELETED IS NULL ";

                    if (int.Parse(nowDate.ToString("HHmm")) <= 700)
                    {
                        sql += "AND CREATTIME BETWEEN to_date('" + Convert.ToDateTime(nowDate.AddDays(-1).ToString("yyyy/MM/dd 23:01:00")).ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/mm/dd hh24:mi:ss') ";

                        lastSql += "AND CREATTIME BETWEEN to_date('" + Convert.ToDateTime(nowDate.AddDays(-1).ToString("yyyy/MM/dd 15:01:00")).ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/mm/dd hh24:mi:ss') ";
                        lastSql += "AND to_date('" + nowDate.AddDays(-1).ToString("yyyy/MM/dd 23:00:00") + "','yyyy/mm/dd hh24:mi:ss') AND DELETED IS NULL ORDER BY CREATTIME DESC";
                    }
                    else if (int.Parse(nowDate.ToString("HHmm")) <= 1500)
                    {
                        sql += "AND CREATTIME BETWEEN to_date('" + Convert.ToDateTime(nowDate.ToString("yyyy/MM/dd 07:01:00")).ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/mm/dd hh24:mi:ss') ";

                        lastSql += "AND CREATTIME BETWEEN to_date('" + Convert.ToDateTime(nowDate.AddDays(-1).ToString("yyyy/MM/dd 23:01:00")).ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/mm/dd hh24:mi:ss') ";
                        lastSql += "AND to_date('" + nowDate.ToString("yyyy/MM/dd 07:00:00") + "','yyyy/mm/dd hh24:mi:ss') AND DELETED IS NULL ORDER BY CREATTIME DESC";
                    }
                    else if (int.Parse(nowDate.ToString("HHmm")) <= 2300)
                    {
                        sql += "AND CREATTIME BETWEEN to_date('" + Convert.ToDateTime(nowDate.ToString("yyyy/MM/dd 15:01:00")).ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/mm/dd hh24:mi:ss') ";

                        lastSql += "AND CREATTIME BETWEEN to_date('" + Convert.ToDateTime(nowDate.ToString("yyyy/MM/dd 07:01:00")).ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/mm/dd hh24:mi:ss') ";
                        lastSql += "AND to_date('" + nowDate.ToString("yyyy/MM/dd 15:00:00") + "','yyyy/mm/dd hh24:mi:ss') AND DELETED IS NULL ORDER BY CREATTIME DESC";
                    }
                    else
                    {
                        sql += "AND CREATTIME BETWEEN to_date('" + Convert.ToDateTime(nowDate.ToString("yyyy/MM/dd 07:01:00")).ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/mm/dd hh24:mi:ss') ";
                        lastSql += "AND CREATTIME BETWEEN to_date('" + Convert.ToDateTime(nowDate.AddDays(-1).ToString("yyyy/MM/dd 23:01:00")).ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/mm/dd hh24:mi:ss') ";
                        lastSql += "AND to_date('" + nowDate.ToString("yyyy/MM/dd 07:00:00") + "','yyyy/mm/dd hh24:mi:ss') AND DELETED IS NULL ORDER BY CREATTIME DESC";
                    }

                    sql += "AND to_date('" + nowDate.ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/mm/dd hh24:mi:ss') "
                   + "AND DELETED IS NULL ORDER BY CREATTIME DESC";

                    #region 當班
                    Dt2 = link.DBExecSQL(sql);
                    if (Dt2.Rows.Count > 0)
                    {
                        Dt["A_Breast"] = "乳頭：#NIPPLE#，乳房：#BREAST#，泌乳：#LACT#。";

                        #region 乳頭
                        var nipple = "";
                        var commaFlag = false;
                        var NIPPLEs = Dt2.Rows[0]["NIPPLE"].ToString();
                        var L_NIPPLE = new List<string>();
                        var R_NIPPLE = new List<string>();

                        for (var i = 0; i < NIPPLEs.Length; i++)
                        {
                            if (Convert.ToInt32(i) < 8 && NIPPLEs[i] == '1')
                                L_NIPPLE.Add(i.ToString());
                            else if (Convert.ToInt32(i) >= 8 && NIPPLEs[i] == '1')
                                R_NIPPLE.Add((Convert.ToInt32(i) - 8).ToString());
                        }

                        //左右相同
                        if (String.Join(",", L_NIPPLE) == String.Join(",", R_NIPPLE))
                        {
                            commaFlag = false;
                            foreach (var i in L_NIPPLE)
                            {
                                if (commaFlag)
                                    nipple += "、";

                                switch (i)
                                {
                                    case "0":
                                        nipple += "正常";
                                        break;
                                    case "1":
                                        nipple += "凹";
                                        break;
                                    case "2":
                                        nipple += "平";
                                        break;
                                    case "3":
                                        nipple += "短";
                                        break;
                                    case "4":
                                        nipple += "大";
                                        break;
                                    case "5":
                                        nipple += "小";
                                        break;
                                    case "6":
                                        nipple += "破皮";
                                        break;
                                    case "7":
                                        nipple += "結痂";
                                        break;
                                    default:
                                        nipple += "";
                                        break;
                                }

                                commaFlag = true;
                            }
                        }
                        else
                        {
                            //左邊
                            commaFlag = false;
                            nipple += "左";
                            if (L_NIPPLE == null)
                                nipple += "未填寫";
                            else
                            {
                                foreach (var i in L_NIPPLE)
                                {
                                    if (commaFlag)
                                        nipple += "、";

                                    switch (i)
                                    {
                                        case "0":
                                            nipple += "正常";
                                            break;
                                        case "1":
                                            nipple += "凹";
                                            break;
                                        case "2":
                                            nipple += "平";
                                            break;
                                        case "3":
                                            nipple += "短";
                                            break;
                                        case "4":
                                            nipple += "大";
                                            break;
                                        case "5":
                                            nipple += "小";
                                            break;
                                        case "6":
                                            nipple += "破皮";
                                            break;
                                        case "7":
                                            nipple += "結痂";
                                            break;
                                        default:
                                            nipple += "";
                                            break;
                                    }
                                    commaFlag = true;
                                }
                            }
                            //右邊
                            commaFlag = false;
                            nipple += "、右";
                            if (R_NIPPLE == null)
                                nipple += "未填寫";
                            else
                            {
                                foreach (var i in R_NIPPLE)
                                {
                                    if (commaFlag)
                                        nipple += "、";

                                    switch (i)
                                    {
                                        case "0":
                                            nipple += "正常";
                                            break;
                                        case "1":
                                            nipple += "凹";
                                            break;
                                        case "2":
                                            nipple += "平";
                                            break;
                                        case "3":
                                            nipple += "短";
                                            break;
                                        case "4":
                                            nipple += "大";
                                            break;
                                        case "5":
                                            nipple += "小";
                                            break;
                                        case "6":
                                            nipple += "破皮";
                                            break;
                                        case "7":
                                            nipple += "結痂";
                                            break;
                                        default:
                                            nipple += "";
                                            break;
                                    }
                                    commaFlag = true;
                                }
                            }
                        }

                        Dt["A_Breast"] = Dt["A_Breast"].Replace("#NIPPLE#", nipple);

                        #endregion

                        #region 乳房
                        var breast = "";
                        commaFlag = false;
                        var BREASTs = Dt2.Rows[0]["BREAST"].ToString();
                        var L_BREAST = new List<string>();
                        var R_BREAST = new List<string>();

                        for (var i = 0; i < BREASTs.Length; i++)
                        {
                            if (i < 5 && BREASTs[i] == '1')
                                L_BREAST.Add(i.ToString());
                            else if (i >= 5 && BREASTs[i] == '1')
                                R_BREAST.Add((Convert.ToInt32(i) - 5).ToString());
                        }

                        //左右相同
                        if (String.Join(",", L_BREAST) == String.Join(",", R_BREAST))
                        {
                            commaFlag = false;
                            foreach (var i in L_BREAST)
                            {
                                if (commaFlag)
                                    breast += "、";

                                switch (i)
                                {
                                    case "0":
                                        breast += "鬆軟";
                                        break;
                                    case "1":
                                        breast += "充盈";
                                        break;
                                    case "2":
                                        breast += "緊繃";
                                        break;
                                    case "3":
                                        breast += "腫脹";
                                        break;
                                    case "4":
                                        breast += "硬";
                                        break;
                                    default:
                                        breast += "";
                                        break;
                                }

                                commaFlag = true;
                            }
                        }
                        else
                        {
                            //左邊
                            commaFlag = false;
                            breast += "左";
                            if (L_BREAST == null)
                                breast += "未填寫";
                            else
                            {
                                foreach (var i in L_BREAST)
                                {
                                    if (commaFlag)
                                        breast += "、";

                                    switch (i)
                                    {
                                        case "0":
                                            breast += "鬆軟";
                                            break;
                                        case "1":
                                            breast += "充盈";
                                            break;
                                        case "2":
                                            breast += "緊繃";
                                            break;
                                        case "3":
                                            breast += "腫脹";
                                            break;
                                        case "4":
                                            breast += "硬";
                                            break;
                                        default:
                                            breast += "";
                                            break;
                                    }
                                    commaFlag = true;
                                }
                            }
                            //右邊
                            commaFlag = false;
                            breast += "，右";
                            if (R_BREAST == null)
                                breast += "未填寫";
                            else
                            {
                                foreach (var i in R_BREAST)
                                {
                                    if (commaFlag)
                                        breast += "、";

                                    switch (i)
                                    {
                                        case "0":
                                            breast += "鬆軟";
                                            break;
                                        case "1":
                                            breast += "充盈";
                                            break;
                                        case "2":
                                            breast += "緊繃";
                                            break;
                                        case "3":
                                            breast += "腫脹";
                                            break;
                                        case "4":
                                            breast += "硬";
                                            break;
                                        default:
                                            breast += "";
                                            break;
                                    }
                                    commaFlag = true;
                                }
                            }
                        }

                        Dt["A_Breast"] = Dt["A_Breast"].Replace("#BREAST#", breast);
                        #endregion

                        #region 泌乳
                        var LACT = "";
                        commaFlag = false;
                        if (Dt2.Rows[0]["LACT_L"] == Dt2.Rows[0]["LACT_R"] && IsNoEmpty(Dt2.Rows[0]["LACT_L"].ToString()) && IsNoEmpty(Dt2.Rows[0]["LACT_L"].ToString()))
                        {
                            switch ((Dt2.Rows[0]["LACT_L"] ?? "").ToString())
                            {
                                case "0":
                                    LACT += "無分泌";
                                    break;
                                case "1":
                                    LACT += "微泌乳";
                                    break;
                                case "2":
                                    LACT += "已分泌";
                                    break;
                                default:
                                    LACT += "";
                                    break;
                            }
                        }
                        else
                        {
                            //左
                            LACT += "左";
                            if (IsNoEmpty(Dt2.Rows[0]["LACT_L"].ToString()))
                            {
                                switch ((Dt2.Rows[0]["LACT_L"] ?? "").ToString())
                                {
                                    case "0":
                                        LACT += "無分泌";
                                        break;
                                    case "1":
                                        LACT += "微泌乳";
                                        break;
                                    case "2":
                                        LACT += "已分泌";
                                        break;
                                    default:
                                        LACT += "";
                                        break;
                                }
                            }
                            else
                            {
                                LACT += "未填寫";
                            }

                            //右
                            LACT += "、右";
                            if (IsNoEmpty(Dt2.Rows[0]["LACT_R"].ToString()))
                            {
                                switch ((Dt2.Rows[0]["LACT_R"] ?? "").ToString())
                                {
                                    case "0":
                                        LACT += "無分泌";
                                        break;
                                    case "1":
                                        LACT += "微泌乳";
                                        break;
                                    case "2":
                                        LACT += "已分泌";
                                        break;
                                    default:
                                        LACT += "";
                                        break;
                                }
                            }
                            else
                            {
                                LACT += "未填寫";
                            }
                            Dt["A_Breast"] = Dt["A_Breast"].Replace("#LACT#", LACT);
                        }
                        #endregion

                        var defecation_time = 0; //排便次數
                        var defecation_c = new List<string>(); //排便性狀

                        var urine_time = 0; //排尿次數
                        var urine_c = new List<string>(); //排尿性狀

                        var milking_time = 0; //含乳時間
                        var breast_milk = 0; //母乳
                        var formula = 0; //配方奶
                        var water = 0;//水
                        var d5w = 0; //D5W
                        foreach (DataRow r in Dt2.Rows)
                        {
                            if (r["DEF_COL"].ToString() == "13")
                            {
                                defecation_time += Convert.ToInt32(r["DEF_FRQ"].ToString() == "" ? "0" : r["DEF_FRQ"].ToString());
                                defecation_c.Add($"{Convert.ToDateTime(r["CREATTIME"].ToString()).ToString("HH:mm")}");
                            }

                            if (r["URI_CHA"].ToString() == "6")
                            {
                                urine_time += Convert.ToInt32(r["URI_FRQ"].ToString() == "" ? "0" : r["URI_FRQ"].ToString());
                                urine_c.Add($"{Convert.ToDateTime(r["CREATTIME"].ToString()).ToString("HH:mm")}");
                            }

                            milking_time += Convert.ToInt32(r["BRE_TSS2"].ToString() == "" ? "0" : r["BRE_TSS2"].ToString());
                            breast_milk += Convert.ToInt32(r["BRE_MLK"].ToString() == "" ? "0" : r["BRE_MLK"].ToString());
                            formula += Convert.ToInt32(r["MILK"].ToString() == "" ? "0" : r["MILK"].ToString());
                            water += Convert.ToInt32(r["WATER"].ToString() == "" ? "0" : r["WATER"].ToString());
                            d5w += Convert.ToInt32(r["D5W"].ToString() == "" ? "0" : r["D5W"].ToString());
                        }
                        Dt["A_FEED"] = $"含乳{milking_time.ToString()}分、母乳{breast_milk.ToString()}cc、配方奶{formula.ToString()}cc、水{water.ToString()}cc、D5W{d5w.ToString()}cc。";

                        if (defecation_time > 0)
                        {
                            Dt["A_Defecation"] = $"{defecation_time.ToString()}次，灰白便({String.Join("、", defecation_c)})";
                        }
                        else
                        {
                            Dt["A_Defecation"] = "";
                        }

                        if (urine_time > 0)
                        {
                            Dt["A_Urine"] = $"{urine_time.ToString()}次，結晶({(urine_c.Count() > 0 ? String.Join("、", urine_c) : "無")})";
                        }
                        else
                        {
                            Dt["A_Urine"] = "";
                        }


                        //兒科當班
                        Dt["C_FEED_Now"] = $"含乳{milking_time.ToString()}分、母乳{breast_milk.ToString()}cc、配方奶{formula.ToString()}cc、水{water.ToString()}cc、D5W{d5w.ToString()}cc。";
                        if (defecation_time > 0)
                        {
                            Dt["C_Defecation_Now"] = $"{defecation_time.ToString()}次，灰白便({(defecation_c.Count() > 0 ? String.Join("、", defecation_c) : "無")})";
                        }
                        else
                        {
                            Dt["C_Defecation_Now"] = "";
                        }

                        if (urine_time > 0)
                        {
                            Dt["C_Urine_Now"] = $"{urine_time.ToString()}次，結晶({(urine_c.Count() > 0 ? String.Join("、", urine_c) : "無")})";
                        }
                        else
                        {
                            Dt["C_Urine_Now"] = "";
                        }
                    }
                    #endregion


                    #region 上一班
                    Dt2 = link.DBExecSQL(lastSql);
                    if (Dt2.Rows.Count > 0)
                    {
                        var defecation_time = 0; //排便次數
                        var defecation_c = new List<string>(); //排便性狀

                        var urine_time = 0; //排尿次數
                        var urine_c = new List<string>(); //排尿性狀

                        var milking_time = 0; //含乳時間
                        var breast_milk = 0; //母乳
                        var formula = 0; //配方奶
                        var water = 0;//水
                        var d5w = 0; //D5W
                        foreach (DataRow r in Dt2.Rows)
                        {
                            defecation_time += Convert.ToInt32(r["DEF_FRQ"].ToString() == "" ? "0" : r["DEF_FRQ"].ToString());
                            if (r["DEF_COL"].ToString() == "13")
                                defecation_c.Add($"{Convert.ToDateTime(r["CREATTIME"].ToString()).ToString("HH:mm")}");

                            urine_time += Convert.ToInt32(r["URI_FRQ"].ToString() == "" ? "0" : r["URI_FRQ"].ToString());
                            if (r["URI_CHA"].ToString() == "6")
                                urine_c.Add($"{Convert.ToDateTime(r["CREATTIME"].ToString()).ToString("HH:mm")}");

                            milking_time += Convert.ToInt32(r["BRE_TSS2"].ToString() == "" ? "0" : r["BRE_TSS2"].ToString());
                            breast_milk += Convert.ToInt32(r["BRE_MLK"].ToString() == "" ? "0" : r["BRE_MLK"].ToString());
                            formula += Convert.ToInt32(r["MILK"].ToString() == "" ? "0" : r["MILK"].ToString());
                            water += Convert.ToInt32(r["WATER"].ToString() == "" ? "0" : r["WATER"].ToString());
                            d5w += Convert.ToInt32(r["D5W"].ToString() == "" ? "0" : r["D5W"].ToString());
                        }

                        //兒科上一班
                        Dt["C_FEED_Last"] = $"含乳{milking_time.ToString()}分、母乳{breast_milk.ToString()}cc、配方奶{formula.ToString()}cc、水{water.ToString()}cc、D5W{d5w.ToString()}cc。";
                        if (defecation_time > 0)
                        {
                            Dt["C_Defecation_Last"] = $"{defecation_time.ToString()}次，灰白便({(defecation_c.Count() > 0 ? String.Join("、", defecation_c) : "無")})";
                        }
                        else
                        {
                            Dt["C_Defecation_Last"] = "";
                        }

                        if (urine_time > 0)
                        {
                            Dt["C_Urine_Last"] = $"{urine_time.ToString()}次，結晶({(urine_c.Count() > 0 ? String.Join("、", urine_c) : "無")})";
                        }
                        else
                        {
                            Dt["C_Urine_Last"] = "";
                        }
                    }
                    #endregion

                    #endregion

                    if (mom_feeno != "")
                    {
                        #region 產程監測 抗生素時間
                        var C_GBSTime = "";
                        var C_GBSReason = "";
                        DataTable C_dtPrc = obs_m.sel_Instantly_Be_Birth(mom_feeno, "", "");
                        if (C_dtPrc != null && C_dtPrc.Rows.Count > 0)
                        {
                            for (var a = C_dtPrc.Rows.Count - 1; a >= 0; a--)
                            {
                                var iid = C_dtPrc.Rows[a]["IID"].ToString();

                                var dtlSql = $@"SELECT * FROM OBS_BPDISP 
WHERE IID = '{iid}' AND TM_MED_ANTIBIOTIC != '00000' ORDER BY CREATTIME";
                                DataTable DtBpDisp = obs_m.DBExecSQL(dtlSql);
                                if (DtBpDisp != null && DtBpDisp.Rows.Count > 0)
                                {
                                    foreach (DataRow bp_r in DtBpDisp.Rows)
                                    {
                                        var ANTIBIOTIC = bp_r["TM_MED_ANTIBIOTIC"].ToString();
                                        if (ANTIBIOTIC != "" && ANTIBIOTIC != "00000")
                                        {
                                            //20200207
                                            //C_GBSTime = Convert.ToDateTime(C_dtPrc.Rows[a]["RECORDTIME"].ToString()).ToString("yyyy/MM/dd HH:mm");
                                            if (C_GBSTime == "")
                                                C_GBSTime = Convert.ToDateTime(C_dtPrc.Rows[a]["RECORDTIME"].ToString()).ToString("yyyy/MM/dd HH:mm");
                                            var ANTIBIOTICV = new List<string>();
                                            for (var p = 0; p < ANTIBIOTIC.ToString().Length; p++)
                                            {
                                                if (ANTIBIOTIC[p] == '1')
                                                {
                                                    if (p == 0)
                                                    {
                                                        ANTIBIOTICV.Add("GBS");
                                                    }
                                                    if (p == 1)
                                                    {
                                                        ANTIBIOTICV.Add("發燒");
                                                    }
                                                    if (p == 2)
                                                    {
                                                        ANTIBIOTICV.Add("破水");
                                                    }
                                                    if (p == 3)
                                                    {
                                                        ANTIBIOTICV.Add("感染");
                                                    }
                                                    if (p == 4)
                                                    {
                                                        ANTIBIOTICV.Add("其他");
                                                    }
                                                }
                                            }
                                            C_GBSReason = String.Join(",", ANTIBIOTICV);
                                            break;
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                        #endregion

                        #region 母親生產史
                        //生產史
                        sql = "SELECT * FROM OBS_BTHHIS "
                        + "WHERE FEENO = '" + mom_feeno + "' AND DELETED IS NULL ORDER BY RECORDTIME DESC";

                        Dt2 = link.DBExecSQL(sql);

                        if (Dt2.Rows.Count > 0)
                        {
                            for (int i = 0; i < Dt2.Rows.Count; i++)
                            {
                                //餵食選擇
                                if (Dt2.Rows[i]["FEED"].ToString() != "")
                                {
                                    var formula = Dt2.Rows[i]["FEED"].ToString();
                                    if (formula == "0")
                                        Dt["C_Formula"] = "純母乳";
                                    else if (formula == "1")
                                        Dt["C_Formula"] = "混合乳";
                                    else if (formula == "2")
                                        Dt["C_Formula"] = "配方奶";
                                    else if (formula == "3")
                                        Dt["C_Formula"] = "尚未決定";
                                }
                                //親子同室
                                if (Dt2.Rows[i]["ROOM_IN"].ToString() != "")
                                {
                                    var room_in = Dt2.Rows[i]["ROOM_IN"].ToString();
                                    if (room_in == "0")
                                        Dt["C_RoomIn"] = "24小時";
                                    else if (room_in == "1")
                                        Dt["C_RoomIn"] = "部分時段";
                                    else if (room_in == "2")
                                        Dt["C_RoomIn" +
                                            ""] = "分離照顧";
                                }

                                //20200210  注射時間
                                if (Dt2.Rows[i]["GBS_AB"].ToString() != "")
                                {
                                    if (C_GBSTime == "")
                                        C_GBSTime = Convert.ToDateTime(Dt2.Rows[i]["GBS_AB"].ToString()).ToString("yyyy/MM/dd HH:mm");
                                }

                                //(母:XX，父:XX。)
                                if (Dt.Where(x => x.Key == "C_pt_blood").Count() == 0)
                                    Dt["C_pt_blood"] = "";
                                Dt["C_pt_blood"] += " (母:#母#，父:#父#。)";
                                if (Dt2.Rows[i]["PAT_BT"].ToString() != "")
                                    Dt["C_pt_blood"] = Dt["C_pt_blood"].Replace("#母#", Dt2.Rows[i]["PAT_BT"].ToString());
                                else
                                    Dt["C_pt_blood"] = Dt["C_pt_blood"].Replace("#母#", "無");
                                if (Dt2.Rows[i]["MAT_BT"].ToString() != "")
                                    Dt["C_pt_blood"] = Dt["C_pt_blood"].Replace("#父#", Dt2.Rows[i]["MAT_BT"].ToString());
                                else
                                    Dt["C_pt_blood"] = Dt["C_pt_blood"].Replace("#父#", "無");
                            }
                        }

                        #endregion

                        #region 母親第二、三產程
                        //第二、三產程
                        sql = "SELECT * FROM OBS_BTHSTA "
                        + "WHERE FEENO = '" + mom_feeno + "' AND DELETED IS NULL ORDER BY RECORDTIME DESC";

                        Dt2 = link.DBExecSQL(sql);
                        if (Dt2.Rows.Count > 0)
                        {
                            for (int i = 0; i < Dt2.Rows.Count; i++)
                            {
                                //施打抗生素時間與生產時間間隔4小時以內需顯示紅字
                                if (Dt2.Rows[i]["BIRTH"].ToString() != "" && C_GBSTime != "")
                                {
                                    var spanTime = Convert.ToDateTime(Dt2.Rows[i]["BIRTH"]).Subtract(Convert.ToDateTime(C_GBSTime));
                                    if (Math.Abs(spanTime.Days) > 0 || Math.Abs(spanTime.Hours) >= 4)
                                        Dt["C_GBSTime"] = $"<label class='RedContent'>{C_GBSTime} {C_GBSReason}</label>";
                                    else
                                        Dt["C_GBSTime"] = C_GBSTime + " " + C_GBSReason;
                                }
                                else
                                {
                                    if (C_GBSTime != "")
                                        Dt["C_GBSTime"] = C_GBSTime + " " + C_GBSReason;
                                }
                            }
                        }

                        #endregion

                        #region 新生兒出生紀錄
                        sql = "SELECT * FROM OBS_NB "
                        + "WHERE NB_TEMPNO = '" + mom_feeno + mom_seq + "' AND DELETED IS NULL ORDER BY RECORDTIME DESC";

                        Dt2 = link.DBExecSQL(sql);
                        if (Dt2.Rows.Count > 0)
                        {
                            for (int i = 0; i < Dt2.Rows.Count; i++)
                            {

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
                        + "     WHERE FEE_NO = '" + mom_feeno + "' "
                        + "     AND CREATE_DATE >= to_date('" + DateTime.Now.AddDays(-1).ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/MM/dd hh24:mi:ss') "
                        + "     ORDER BY CREATE_DATE DESC ) MAIN "
                        + " WHERE MAIN.NUM = 1 ";
                        Temp = new List<Dictionary<string, string>>();

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
                            Dt["C_Mom_bt"] = "";
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

                                    Dt["C_Mom_bt"] += string.Format("{0}<label class='LittleTitle'>體溫：</label><font color='" + color + "'>{1}</font> {2}。<br/>"
                                        , Convert.ToDateTime(VitalSignList[i]["CREATE_DATE"]).ToString("yyyy/MM/dd HH:mm")
                                        , (measure == "測不到") ? measure : VitalSignList[i]["VS_RECORD"] + "℃"
                                        , (!string.IsNullOrWhiteSpace(VitalSignList[i]["VS_MEMO"])) ? "，予" + VitalSignList[i]["VS_MEMO"] : "");
                                }
                            }
                        }

                        #endregion
                    }
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
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);

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
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
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
                        tdList.Add(new TransList(patinfo[0], vital_info, wait_info, remark_info, speical_info, shift_cate));
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
                    if (Int32.Parse(Now.ToString("HH")) < 3)
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
                    log.saveLogMsg(ex.Message.ToString() + "，SUCCESSOR：" + SUCCESSOR + "，BEDNO：" + Request["BEDNO"] + "，SHIFTCATE：" + Request["SHIFTCATE"] + "，where：" + where, "SaveSucceNoLog");
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

        #region 值不為空回傳True，否則為False
        public bool IsNoEmpty(string str)
        {
            if (str != null && str != "")
                return true;
            else
                return false;
        }
        #endregion
    }
}
