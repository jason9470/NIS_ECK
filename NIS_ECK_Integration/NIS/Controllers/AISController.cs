using NIS.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using NIS.UtilTool;
using NIS.Data;
using System.Globalization;
using System.Net.Http;
using System.Net;
using static NIS.MvcApplication;
using Com.Mayaminer;
using System.Diagnostics;
using System.Threading;
using static NIS.Models.AIS;

namespace NIS.Controllers
{
    public class AISController : BaseController
    {
        private CareRecord care_record_m;
        private string mode = NIS.MvcApplication.iniObj.NisSetting.ServerMode.ToString();
        private DBConnector link;
        LogTool log = new LogTool();
        public AISController()
        {
            this.care_record_m = new CareRecord();
            this.link = new DBConnector();
        }

        public ActionResult Index()
        {
            return View();
        }

        public class UdOrderWithExecute
        {
            public string UD_SEQ { get; set; }
            public string UD_TYPE { get; set; }
            public string UD_STATUS { get; set; }
            public string FEE_NO { get; set; }
            public string CHR_NO { get; set; }
            public string MED_CODE { get; set; }
            public string COST_CODE { get; set; }
            public string BED_NO { get; set; }
            public string PAT_NAME { get; set; }
            public string MED_DESC { get; set; }
            public string UD_CMD { get; set; }
            public string ALISE_DESC { get; set; }
            public string UD_DOSE { set; get; }
            public string UD_UNIT { set; get; }
            public string UD_DOSE_TOTAL { set; get; }
            public string UD_CIR { get; set; }
            public string BEGIN_DATE { get; set; }
            public string BEGIN_TIME { get; set; }
            public string DRUG_TYPE { get; set; }
            public string DrugPicPath { get; set; }
            public string DRUG_DATE { get; set; }
            public string EXEC_DATE { get; set; }
            public string EXEC_ID { get; set; }
            public string EXEC_NAME { get; set; }
            public string UD_PATH { get; set; }
        }

        public ActionResult GetPatientBodyInfo(string key, string feeno)
        {
            if (key != "AISConnection")
            {
                return Content(JsonConvert.SerializeObject(new { message = "key值錯誤!" }));
            }
            if (string.IsNullOrEmpty(feeno))
            {
                if (mode == "Maya")
                {
                    feeno = "I0332966";
                }
            }
            Dictionary<string, object> Data = new Dictionary<string, object>();
            try
            {
                //入院評估
                //身高,體重
                var table_id = "";
                var na_type = "";
                string[] itemList = { "param_BodyHeight", "param_BodyWeight" };
                string sql = "SELECT * FROM \n";
                sql += "(SELECT ASSESSMENTMASTER.*,ROW_NUMBER() OVER (ORDER BY MODIFYTIME desc) SN \n";
                sql += "FROM ASSESSMENTMASTER \n";
                sql += "WHERE FEENO = '" + feeno + "' \n";
                sql += "AND STATUS != 'delete') \n";
                sql += "WHERE SN='1'";
                DataTable assessmentMaster = assessmentMaster = link.DBExecSQL(sql);
                if (assessmentMaster.Rows.Count > 0)
                {
                    table_id = assessmentMaster.Rows[0]["TABLEID"].ToString();
                    na_type = assessmentMaster.Rows[0]["NATYPE"].ToString();
                    sql = "SELECT * \n";
                    sql += "FROM ASSESSMENTDETAIL \n";
                    sql += "WHERE TABLEID = '" + table_id + "' \n";
                    sql += "AND (";
                    foreach (var item in itemList)
                    {
                        sql += "ITEMID LIKE '" + item + "%' OR ";
                    }
                    sql += "0=1) \n";
                    DataTable assessmentDetail = link.DBExecSQL(sql);
                    if (assessmentDetail.Rows.Count > 0)
                    {
                        for (var i = 0; i < assessmentDetail.Rows.Count; i++)
                        {
                            var item_id = assessmentDetail.Rows[i]["ITEMID"].ToString();
                            var item_value = assessmentDetail.Rows[i]["ITEMVALUE"].ToString();
                            Data.Add(item_id, item_value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.log.saveLogMsg("FeeNo:" + feeno + " Error:" + ex.Message, "AISGetPatientBodyInfo");
            }
            if (Data.Count > 0)
            {
                return Content(JsonConvert.SerializeObject(Data));
            }
            else
            {
                return Content(JsonConvert.SerializeObject(new { message = "查無資料" }));
            }
        }

        public ActionResult GetSurgeryData(string key, string feeno)
        {
            if (key != "AISConnection")
            {
                return Content(JsonConvert.SerializeObject(new { message = "key值錯誤!" }));
            }
            if (string.IsNullOrEmpty(feeno))
            {
                if (mode == "Maya")
                {
                    feeno = "I0332966";
                }
            }

            Dictionary<string, object> Data = new Dictionary<string, object>();
            try
            {
                List<object> tube = new List<object>();
                List<object> bloodInfoList = new List<object>();

                //入院評估
                //疾病史,過敏史藥物,住院,手術
                var table_id = "";
                var na_type = "";
                string[] itemList = { "param_im_history", "param_allergy_med", "param_ipd_past", "param_ipd_surgery", "param_BodyHeight", "param_BodyWeight" };
                string sql = "SELECT * FROM \n";
                sql += "(SELECT ASSESSMENTMASTER.*,ROW_NUMBER() OVER (ORDER BY MODIFYTIME desc) SN \n";
                sql += "FROM ASSESSMENTMASTER \n";
                sql += "WHERE FEENO = '" + feeno + "' \n";
                sql += "AND STATUS != 'delete') \n";
                sql += "WHERE SN='1'";
                DataTable assessmentMaster = assessmentMaster = link.DBExecSQL(sql);
                if (assessmentMaster.Rows.Count > 0)
                {
                    table_id = assessmentMaster.Rows[0]["TABLEID"].ToString();
                    na_type = assessmentMaster.Rows[0]["NATYPE"].ToString();
                    sql = "SELECT * \n";
                    sql += "FROM ASSESSMENTDETAIL \n";
                    sql += "WHERE TABLEID = '" + table_id + "' \n";
                    sql += "AND (";
                    foreach (var item in itemList)
                    {
                        sql += "ITEMID LIKE '" + item + "%' OR ";
                    }
                    sql += "0=1) \n";
                    DataTable assessmentDetail = link.DBExecSQL(sql);
                    if (assessmentDetail.Rows.Count > 0)
                    {
                        for (var i = 0; i < assessmentDetail.Rows.Count; i++)
                        {
                            var item_id = assessmentDetail.Rows[i]["ITEMID"].ToString();
                            var item_value = assessmentDetail.Rows[i]["ITEMVALUE"].ToString();
                            Data.Add(item_id, item_value);
                        }
                    }
                }

                //傳染源
                sql = "SELECT * FROM \n";
                sql += "(SELECT DATA_TRANS_MEMO.*,ROW_NUMBER() OVER (ORDER BY CREATE_DATE desc) SN \n";
                sql += "FROM DATA_TRANS_MEMO \n";
                sql += "WHERE FEE_NO = '" + feeno + "') \n";
                sql += "WHERE SN= '1'\n";
                DataTable Trans = link.DBExecSQL(sql);
                if (Trans.Rows.Count > 0)
                {
                    var trans_remark_data = Trans.Rows[0]["TRANS_REMARK"].ToString();
                    Data.Add("TRANS_REMARK", trans_remark_data);
                }

                //壓力性損傷風險評估
                sql = "SELECT * FROM \n";
                sql += "(SELECT NIS_PRESSURE_SORE_DATA.*,ROW_NUMBER() OVER (ORDER BY CREATTIME desc) SN \n";
                sql += "FROM NIS_PRESSURE_SORE_DATA \n";
                sql += "WHERE FEENO = '" + feeno + "') \n";
                sql += "WHERE SN= '1'\n";
                sql += "AND DELETED IS NULL\n";
                DataTable Pressure = link.DBExecSQL(sql);
                if (Pressure.Rows.Count > 0)
                {
                    var total = Pressure.Rows[0]["TOTAL"].ToString();
                    if (int.Parse(total) >= 19)
                    {
                        Data.Add("PRESSURE_SORE", "有" + "(" + total + ")");
                    }
                    else
                    {
                        Data.Add("PRESSURE_SORE", "無" + "(" + total + ")");
                    }
                }

                //疼痛評估
                sql = "SELECT * FROM \n";
                sql += "(SELECT NIS_DATA_PAIN_POSITION.*,ROW_NUMBER() OVER (ORDER BY CREATE_DATE desc) SN \n";
                sql += "FROM NIS_DATA_PAIN_POSITION \n";
                sql += "WHERE FEE_NO = '" + feeno + "') \n";
                sql += "WHERE SN= '1'\n";
                DataTable Pain = link.DBExecSQL(sql);
                if (Pain.Rows.Count > 0)
                {
                    var record_json_data = Pain.Rows[0]["RECORD_JSON_DATA"].ToString();
                    List<Dictionary<string, object>> DesData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(record_json_data);
                    foreach (var item in DesData)
                    {
                        if ((item["Name"].ToString()).Contains("PainLevel"))
                        {
                            Data.Add(item["Name"].ToString(), item["Value"]);
                        }
                    }
                }

                //輸血同意書
                sql = "SELECT * FROM \n";
                sql += "(SELECT NIS_DATA_PT_SHIFT.*,ROW_NUMBER() OVER (ORDER BY RECORD_DATE desc) SN \n";
                sql += "FROM NIS_DATA_PT_SHIFT \n";
                sql += "WHERE FEE_NO = '" + feeno + "') \n";
                sql += "WHERE SN= '1'\n";
                DataTable BloodInfoList_Consent = link.DBExecSQL(sql);
                if (BloodInfoList_Consent.Rows.Count > 0)
                {
                    byte[] TempByte = null;
                    List<Dictionary<string, string>> BloodInfoListWS = new List<Dictionary<string, string>>();
                    TempByte = this.webService.GetBloodInfo(feeno);
                    if (TempByte != null)
                    {
                        BloodInfoListWS = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(CompressTool.DecompressString(TempByte));
                    }
                    var record_json_data = BloodInfoList_Consent.Rows[0]["RECORD_JSON_DATA"].ToString();
                    List<Dictionary<string, object>> DesDataList = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(record_json_data);

                    foreach (var BloodInfo in BloodInfoListWS)
                    {
                        var id = BloodInfo["Blood_NO"].ToString();
                        var consent = "";
                        var name = BloodInfo["Blood_Name"].ToString();
                        var time = DateTime.Parse(BloodInfo["Prepare_Date"]).ToString("yyyy/MM/dd HH:mm");
                        var reason = BloodInfo["Reason"].ToString();
                        var status_checkbox = "";
                        var status_text_StartBloodTransfusion = "";
                        var status_text_EndBloodTransfusion = "";
                        var remark = "";

                        foreach (var DesData in DesDataList)
                        {
                            var desName = DesData["Name"].ToString();
                            if (desName.Contains(id))
                            {
                                if (desName.Contains("BloodInfoList_Consent"))
                                {
                                    consent = DesData["Value"].ToString();
                                }
                                if (desName.Contains("BloodInfoList_Status_" + id))
                                {
                                    status_checkbox = DesData["Value"].ToString();
                                }
                                if (desName.Contains("BloodInfoList_Status_StartBloodTransfusion"))
                                {
                                    status_text_StartBloodTransfusion = DesData["Value"].ToString();
                                }
                                if (desName.Contains("BloodInfoList_Status_EndBloodTransfusion"))
                                {
                                    status_text_EndBloodTransfusion = DesData["Value"].ToString();
                                }
                                if (desName.Contains("BloodInfoList_Remark"))
                                {
                                    remark = DesData["Value"].ToString();
                                }
                            }
                        }
                        var obj = new
                        {
                            ID = id,
                            Consent = consent,
                            Name = name,
                            Time = time,
                            Reason = reason,
                            Status_Checkbox = status_checkbox,
                            Status_Text_StartBloodTransfusion = status_text_StartBloodTransfusion,
                            Status_Text_EndBloodTransfusion = status_text_EndBloodTransfusion,
                            Remark = remark
                        };
                        bloodInfoList.Add(obj);
                    }
                    Data.Add("BloodInfoList", bloodInfoList);
                }

                //管路
                sql = "SELECT TUBE.POSITION, \n";
                sql += "(SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'tube' AND P_GROUP = 'tubePosition' AND P_VALUE = TUBE.LOCATION) LOCATION_NAME, \n";
                sql += "(SELECT KINDNAME FROM TUBE_KIND WHERE KINDID = TUBE.TYPEID) TYPE_NAME \n";
                sql += "FROM TUBE \n";
                sql += "WHERE FEENO = '" + feeno + "' \n";
                sql += "AND ENDTIME IS NULL \n";
                sql += "AND DELETED IS NULL";
                DataTable TUBE = link.DBExecSQL(sql);
                if (TUBE.Rows.Count > 0)
                {
                    for (var i = 0; i < TUBE.Rows.Count; i++)
                    {
                        var type_name = TUBE.Rows[i]["TYPE_NAME"].ToString();
                        var location_name = TUBE.Rows[i]["LOCATION_NAME"].ToString().Trim() == "請選擇" ? "" : TUBE.Rows[i]["LOCATION_NAME"].ToString();
                        var position_name = TUBE.Rows[i]["POSITION"].ToString() ?? "";
                        var obj = new
                        {
                            Name = type_name,
                            Location = location_name,
                            Position = position_name
                        };
                        tube.Add(obj);
                    }
                    Data.Add("TubeList", tube);
                }

                //生命徵象
                //取得VS_ID
                sql = "SELECT DATA_VITALSIGN.*,ROW_NUMBER() OVER (ORDER BY CREATE_DATE asc) SN \n";
                sql += "FROM DATA_VITALSIGN \n";
                sql += "WHERE FEE_NO = '" + feeno + "' \n";
                sql += "AND DEL IS NULL \n";
                DataTable VS_IDList = link.DBExecSQL(sql);
                if (VS_IDList.Rows.Count > 0)
                {
                    var VS_IDListLastRow = VS_IDList.Rows.Count - 1;
                    var VS_ID = VS_IDList.Rows[VS_IDListLastRow]["VS_ID"];

                    sql = "SELECT * FROM DATA_VITALSIGN \n";
                    sql += "WHERE VS_ID = '" + VS_ID + "'\n";
                    DataTable VitalSign = link.DBExecSQL(sql);
                    if (VitalSign.Rows.Count > 0)
                    {
                        var bt_value = "";
                        var mp_value = "";
                        var bf_value = "";
                        var bp_value = "";
                        var create_date = "";
                        var modify_date = "";
                        for (var i = 0; i < VitalSign.Rows.Count; i++)
                        {
                            var vs_item = VitalSign.Rows[i]["VS_ITEM"].ToString().Trim();
                            var vs_record = VitalSign.Rows[i]["VS_RECORD"].ToString();
                            switch (vs_item)
                            {
                                case "bt":
                                    bt_value = vs_record;
                                    break;
                                case "mp":
                                    mp_value = vs_record;
                                    break;
                                case "bf":
                                    bf_value = vs_record;
                                    break;
                                case "bp":
                                    var bpHigh = 0.0;
                                    var bpLow = 0.0;
                                    var bpAvg = 0.0;
                                    var bpList = vs_record.Split('|');
                                    bpHigh = double.Parse(bpList[0]);
                                    bpLow = double.Parse(bpList[1]);
                                    if (bpList.Length > 2)
                                    {
                                        bpAvg = int.Parse(bpList[2]);
                                    }
                                    else
                                    {
                                        bpAvg = bpLow + ((bpHigh - bpLow) / 3);
                                    }
                                    bp_value = bpHigh.ToString() + "/" + bpLow.ToString() + "(" + bpAvg.ToString("0.##") + ")";
                                    break;
                            }
                            create_date = (DateTime.Parse(VitalSign.Rows[i]["CREATE_DATE"].ToString())).ToString("yyyy/MM/dd HH:mm");
                            modify_date = (DateTime.Parse(VitalSign.Rows[i]["MODIFY_DATE"].ToString())).ToString("yyyy/MM/dd HH:mm");
                        }
                        Data.Add("bt", bt_value);
                        Data.Add("mp", mp_value);
                        Data.Add("bf", bf_value);
                        Data.Add("bp", bp_value);

                        //if (modify_date != "")
                        //{
                        //    Data.Add("VitalSign_RecordDateTime", modify_date);
                        //}
                        //else
                        //{
                        //    Data.Add("VitalSign_RecordDateTime", create_date);
                        //}

                        Data.Add("VitalSign_RecordDateTime", create_date);
                    }
                }

                //GCS
                sql = "SELECT M.DBAM_DTM,D.* FROM DAILY_BODY_ASSESSMENT_MASTER M \n";
                sql += "LEFT JOIN DAILY_BODY_ASSESSMENT_DETAIL D \n";
                sql += "ON M.DBAM_ID=D.DBAM_ID \n";
                sql += "WHERE D.DBAM_ID = \n";
                sql += "(SELECT DBAM_ID FROM \n";
                sql += "(SELECT DAILY_BODY_ASSESSMENT_MASTER.*,ROW_NUMBER() OVER (ORDER BY CREATTIME DESC) SN \n";
                sql += "FROM DAILY_BODY_ASSESSMENT_MASTER \n";
                sql += "WHERE FEENO = '" + feeno + "' \n";
                sql += "AND DBAM_TEMP_TYPE='complete' \n";
                sql += "AND DELETED!='Y') \n";
                sql += "WHERE SN= '1') \n";
                DataTable GCS = link.DBExecSQL(sql);
                if (GCS.Rows.Count > 0)
                {
                    for (var i = 0; i < GCS.Rows.Count; i++)
                    {
                        var item_id = GCS.Rows[i]["DBAD_ITEMID"].ToString().Trim();
                        var item_value = GCS.Rows[i]["DBAD_ITEMVALUE"].ToString();
                        if (item_id.Contains("RbnAwarenessComa_"))
                        {
                            Data.Add(item_id, item_value);
                        }
                    }
                    var GCS_RecordDateTime = (DateTime.Parse(GCS.Rows[0]["DBAM_DTM"].ToString())).ToString("yyyy/MM/dd HH:mm");
                    Data.Add("GCS_RecordDateTime", GCS_RecordDateTime);
                }

                //手術前給藥，攜帶藥物
                //byte[] TempByteDrug = null;
                //List<Dictionary<string, string>> DrugOrderWS = new List<Dictionary<string, string>>();
                //TempByteDrug = this.webService.GetIpdDrugOrder(feeno);
                //if (TempByteDrug != null)
                //{
                //    DrugOrderWS = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(CompressTool.DecompressString(TempByteDrug));
                //}
                //if (DrugOrderWS.Count > 0)
                //{
                //    Data.Add("DrugList", DrugOrderWS);
                //}

                //手術前給藥，攜帶藥物
                byte[] doByteCode = webService.GetUdOrder(feeno, "A");
                string doJsonArr = CompressTool.DecompressString(doByteCode);
                List<UdOrder> GetUdOrderList = JsonConvert.DeserializeObject<List<UdOrder>>(doJsonArr);
                List<UdOrder> UdOrdersSTA = new List<UdOrder>();
                var CompareDate_S_STR = DateTime.Now.AddDays(-7).ToString("yyyy/MM/dd 00:00:00");
                var CompareDate_E_STR = DateTime.Now.ToString("yyyy/MM/dd 23:59:59");
                var CompareDate_S = DateTime.Parse(CompareDate_S_STR);
                var CompareDate_E = DateTime.Parse(CompareDate_E_STR);
                GetUdOrderList.ForEach((item) =>
                {
                    //var Begin_Date_Year = (int.Parse(item.BEGIN_DATE.Substring(0, 3)) + 1911).ToString();
                    //var Begin_Date_Mon = item.BEGIN_DATE.Substring(3, 2);
                    //var Begin_Date_Day = item.BEGIN_DATE.Substring(5, 2);
                    //var Begin_Date = DateTime.Parse(Begin_Date_Year + "/" + Begin_Date_Mon + "/" + Begin_Date_Day + " 00:00:00");
                    if (item.UD_CIR.Trim() == "ST")
                    {
                        //if (Begin_Date >= CompareDate_S && Begin_Date <= CompareDate_E)
                        //{
                        //    UdOrdersSTA.Add(item);
                        //}
                        UdOrdersSTA.Add(item);
                    }
                });
                var UD_SEQList = UdOrdersSTA.Select(a => a.UD_SEQ);
                sql = "SELECT * FROM DRUG_EXECUTE \n";
                sql += "WHERE UD_SEQ IN ('' ";
                foreach (var ud_seq in UD_SEQList)
                    sql += ",'" + ud_seq + "'";
                sql += ") \n";
                //sql += "AND TO_DATE(DRUG_DATE,'yyyy/MM/dd AM hh:mi:ss') BETWEEN TO_DATE('" + CompareDate_S_STR + "', 'yyyy/MM/dd HH24:mi:ss') \n";
                //sql += "AND TO_DATE('" + CompareDate_E_STR + "', 'yyyy/MM/dd HH24:mi:ss') \n";
                sql += "AND FEE_NO = '" + feeno + "' \n";
                sql += "ORDER BY UD_SEQ";
                DataTable DrugExecuteTable = link.DBExecSQL(sql);
                List<object> UdOrderWithExecute = new List<object>();
                foreach (var udOrder in UdOrdersSTA)
                {
                    UdOrderWithExecute obj = new UdOrderWithExecute
                    {
                        UD_SEQ = udOrder.UD_SEQ,
                        UD_TYPE = udOrder.UD_TYPE,
                        UD_STATUS = udOrder.UD_STATUS,
                        FEE_NO = udOrder.FEE_NO,
                        CHR_NO = udOrder.CHR_NO,
                        MED_CODE = udOrder.MED_CODE,
                        COST_CODE = udOrder.COST_CODE,
                        BED_NO = udOrder.BED_NO,
                        PAT_NAME = udOrder.PAT_NAME,
                        MED_DESC = udOrder.MED_DESC,
                        UD_CMD = udOrder.UD_CMD,
                        ALISE_DESC = udOrder.ALISE_DESC,
                        UD_DOSE = udOrder.UD_DOSE,
                        UD_UNIT = udOrder.UD_UNIT,
                        UD_DOSE_TOTAL = udOrder.UD_DOSE_TOTAL,
                        UD_CIR = udOrder.UD_CIR,
                        BEGIN_DATE = udOrder.BEGIN_DATE,
                        BEGIN_TIME = udOrder.BEGIN_TIME,
                        DRUG_TYPE = udOrder.DRUG_TYPE,
                        DrugPicPath = udOrder.DrugPicPath,
                        UD_PATH = udOrder.UD_PATH,
                    };
                    for (var i = 0; i < DrugExecuteTable.Rows.Count; i++)
                    {
                        if (DrugExecuteTable.Rows[i]["UD_SEQ"].ToString() == udOrder.UD_SEQ)
                        {
                            obj.DRUG_DATE = DrugExecuteTable.Rows[i]["DRUG_DATE"].ToString();
                            obj.EXEC_DATE = DrugExecuteTable.Rows[i]["EXEC_DATE"].ToString();
                            obj.EXEC_ID = DrugExecuteTable.Rows[i]["EXEC_ID"].ToString();
                            obj.EXEC_NAME = DrugExecuteTable.Rows[i]["EXEC_NAME"].ToString();
                        }
                    }
                    UdOrderWithExecute.Add(obj);
                }
                if (UdOrderWithExecute.Count > 0)
                {
                    Data.Add("DrugList", UdOrderWithExecute);
                }

                //壓傷評估
                sql = "SELECT * FROM \n";
                sql += "(SELECT NIS_PRESSURE_SORE_DATA.*,ROW_NUMBER() OVER (ORDER BY RECORDTIME DESC) SN \n";
                sql += "FROM NIS_PRESSURE_SORE_DATA \n";
                sql += "WHERE FEENO = '" + feeno + "' \n";
                sql += "AND DELETED IS NULL) \n";
                sql += "WHERE SN = 1 \n";
                DataTable PressureSoreTable = link.DBExecSQL(sql);
                if (PressureSoreTable.Rows.Count > 0)
                {
                    var PressureSoreObj = new
                    {
                        RecordTime = DateTime.Parse(PressureSoreTable.Rows[0]["RECORDTIME"].ToString()).ToString("yyyy/MM/dd HH:mm"),
                        Nutrition = PressureSoreTable.Rows[0]["NUTRITION"].ToString(),
                        Perception = PressureSoreTable.Rows[0]["PERCEPTION"].ToString(),
                        Damp = PressureSoreTable.Rows[0]["DAMP"].ToString(),
                        Activity = PressureSoreTable.Rows[0]["ACTIVITY"].ToString(),
                        Moving = PressureSoreTable.Rows[0]["MOVING"].ToString(),
                        Friction = PressureSoreTable.Rows[0]["FRICTION"].ToString(),
                        Have = PressureSoreTable.Rows[0]["HAVE"].ToString(),
                        Total = PressureSoreTable.Rows[0]["TOTAL"].ToString(),
                    };
                    Data.Add("PressureSore", PressureSoreObj);
                }
            }
            catch (Exception ex)
            {
                this.log.saveLogMsg("FeeNo:" + feeno + " Error:" + ex.Message, "AISGetSurgeryData");
            }
            if (Data.Count > 0)
            {
                return Content(JsonConvert.SerializeObject(Data));
            }
            else
            {
                return Content(JsonConvert.SerializeObject(new { message = "查無資料" }));
            }
        }

        //新增特殊事件註記
        public ActionResult MarkSpecialEvent(string key, string sourceKey, string userNo, string userName, string chartNo, string opDate)
        {
            if (key != "AISConnection")
            {
                return Content(JsonConvert.SerializeObject(new { isSuccess = false, messageByNotSuccess = "key值錯誤!" }));
            }
            if (string.IsNullOrEmpty(chartNo) || string.IsNullOrEmpty(userNo) || string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(opDate) || string.IsNullOrEmpty(sourceKey))
            {
                var errorMessage = string.IsNullOrEmpty(chartNo) ? $"取得空的chartNo:[{chartNo}]" :
                    string.IsNullOrEmpty(userNo) ? $"取得空的userNo:[{userNo}]" :
                    string.IsNullOrEmpty(userName) ? $"取得空的userName:[{userName}]" :
                    string.IsNullOrEmpty(opDate) ? $"取得空的opDate:[{opDate}]" :
                    string.IsNullOrEmpty(sourceKey) ? $"取得空的sourceKey:[{sourceKey}]" : "未取得所需參數";
                return Content(JsonConvert.SerializeObject(new { isSuccess = false, messageByNotSuccess = errorMessage }));
            }

            try
            {
                var opDate_Format = DateTime.Parse(opDate);
                InHistory inHistoryNow = new InHistory();
                byte[] doByteCode = webService.GetInHistory(chartNo);
                if (doByteCode != null)
                {
                    string doJsonArr = CompressTool.DecompressString(doByteCode);
                    List<InHistory> IpdList = JsonConvert.DeserializeObject<List<InHistory>>(doJsonArr);
                    if (IpdList.Count > 0)
                    {
                        List<string> feeNoList = new List<string>();
                        foreach (var item in IpdList)
                        {
                            var InDate = item.indate;
                            var OutDate = item.outdate;
                            if (opDate_Format >= InDate && (opDate_Format <= OutDate))
                            {
                                feeNoList.Add(item.FeeNo);
                            }
                            ;
                        }
                        if (feeNoList.Count > 0)
                        {
                            string type_id = "3",
                                title = "手術日",
                                care_content = "病人於" + DateTime.Now.ToString("yyyy/MM/dd HH:mm") + "送至手術室。",
                                content = care_content;
                            string temp_item = "";

                            foreach (var feeNo in feeNoList)
                            {
                                string id = id = "EVENT" + "_" + userNo + "_" + feeNo + "_" + sourceKey + "_" + "0";
                                string sql = "SELECT EVENT_ID FROM NIS_SPECIAL_EVENT_DATA \n";
                                sql += "WHERE EVENT_ID = '" + id + "' \n";
                                DataTable dt = link.DBExecSQL(sql);
                                if (dt.Rows.Count > 0)
                                {
                                    return Content(JsonConvert.SerializeObject(new { isSuccess = true, messageByNotSuccess = "此Source Key已有紀錄" }));
                                }
                                else
                                {
                                    List<DBItem> insertDataList = new List<DBItem>();
                                    insertDataList.Add(new DBItem("EVENT_ID", id, DBItem.DBDataType.String));
                                    insertDataList.Add(new DBItem("FEENO", feeNo, DBItem.DBDataType.String));
                                    insertDataList.Add(new DBItem("CREATNO", userNo, DBItem.DBDataType.String));
                                    insertDataList.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                                    insertDataList.Add(new DBItem("TYPE_ID", type_id, DBItem.DBDataType.String));
                                    insertDataList.Add(new DBItem("CONTENT", content, DBItem.DBDataType.String));
                                    if (temp_item != "")
                                        insertDataList.Add(new DBItem("EDIT_ITEM", temp_item, DBItem.DBDataType.String));

                                    int erow = care_record_m.DBExecInsert("NIS_SPECIAL_EVENT_DATA", insertDataList);
                                    if (erow > 0)
                                    {
                                        DateTime NowTime = DateTime.Now;
                                        LogTool lt = new LogTool();
                                        CareRecord care_record_m = new CareRecord();
                                        string sign_userno = care_record_m.sel_guide_userno(userNo, NowTime, NowTime.Hour);
                                        insertDataList = new List<DBItem>();
                                        insertDataList.Add(new DBItem("CARERECORD_ID", id, DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("CREATNO", userNo, DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("GUIDE_NO", sign_userno, DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("CREATNAME", userName, DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("CREATTIME", NowTime.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                                        insertDataList.Add(new DBItem("RECORDTIME", NowTime.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                                        insertDataList.Add(new DBItem("FEENO", feeNo, DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("TITLE", trans_date(title), DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("C_OTHER", trans_date(care_content), DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("SELF", "SPE_EVENT", DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("SIGN", "N", DBItem.DBDataType.String));
                                        erow = link.DBExecInsert("CARERECORD_DATA", insertDataList);
                                        if (erow > 0)
                                        {
                                            try
                                            {
                                                string msg = "";
                                                if (trans_date(care_content) != "")
                                                    msg += trans_date(care_content) + "\n";// + trans_date(C)
                                                erow = EMR_Sign(NowTime.ToString("yyyy/MM/dd HH:mm:ss"), id, msg, title, "SPE_EVENT", feeNo, userNo);
                                            }
                                            catch (Exception ex)
                                            {
                                                lt.saveLogMsg(ex.Message, "EMR_Pre_Operation_Log");
                                            }
                                            return Content(JsonConvert.SerializeObject(new { isSuccess = true, messageByNotSuccess = "" }));
                                        }
                                        else
                                        {
                                            return Content(JsonConvert.SerializeObject(new { isSuccess = false, messageByNotSuccess = "新增特殊事件註記失敗" }));
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            return Content(JsonConvert.SerializeObject(new { isSuccess = false, messageByNotSuccess = "無符合時間的FeeNo" }));
                        }
                    }
                    else
                    {
                        return Content(JsonConvert.SerializeObject(new { isSuccess = false, messageByNotSuccess = "查無此病歷號的入院紀錄" }));
                    }
                }
            }
            catch (Exception ex)
            {
                this.log.saveLogMsg("SourceKey:" + sourceKey + " Error:" + ex.Message, "AISMarkSpecialEvent");
            }
            return Content(JsonConvert.SerializeObject(new { isSuccess = false, messageByNotSuccess = "查無此病歷號" }));
        }

        //新增特殊事件註記(有區分急診及住院病人)
        public ActionResult MarkSpecialEvent_V2(string key, string sourceKey, string userNo, string userName, string chartNo, string opDate, bool isERPatient)
        {
            if (key != "AISConnection")
            {
                return Content(JsonConvert.SerializeObject(new { isSuccess = false, messageByNotSuccess = "key值錯誤!" }));
            }
            if (string.IsNullOrEmpty(chartNo) || string.IsNullOrEmpty(userNo) || string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(opDate) || string.IsNullOrEmpty(sourceKey))
            {
                var errorMessage = string.IsNullOrEmpty(chartNo) ? $"取得空的chartNo:[{chartNo}]" :
                    string.IsNullOrEmpty(userNo) ? $"取得空的userNo:[{userNo}]" :
                    string.IsNullOrEmpty(userName) ? $"取得空的userName:[{userName}]" :
                    string.IsNullOrEmpty(opDate) ? $"取得空的opDate:[{opDate}]" :
                    string.IsNullOrEmpty(sourceKey) ? $"取得空的sourceKey:[{sourceKey}]" : "未取得所需參數";
                return Content(JsonConvert.SerializeObject(new { isSuccess = false, messageByNotSuccess = errorMessage }));
            }

            try
            {
                var opDate_Format = DateTime.Parse(opDate);
                InHistory inHistoryNow = new InHistory();
                byte[] doByteCode = webService.GetInHistory(chartNo);
                if (doByteCode != null)
                {
                    string doJsonArr = CompressTool.DecompressString(doByteCode);
                    List<InHistory> IpdList = JsonConvert.DeserializeObject<List<InHistory>>(doJsonArr).OrderByDescending(x => x.indate).ToList();
                    if (IpdList.Count > 0)
                    {
                        List<string> feeNoList = new List<string>();
                        //判斷是否為急診病人
                        if (isERPatient)
                        {
                            //急診取最新一筆紀錄
                            InHistory firstERInHistory = IpdList.Where(x => x.HIS_TYPE == "急").OrderByDescending(x => x.indate).FirstOrDefault();
                            if (firstERInHistory != null)
                            {
                                feeNoList.Add(firstERInHistory.FeeNo);
                            }
                        }
                        else
                        {
                            //住院取手術時間於入院時間即出院時間之間
                            List<InHistory> admissionIpdList = IpdList.Where(x => x.HIS_TYPE == "住").OrderByDescending(x => x.indate).ToList();
                            if (admissionIpdList.Count > 0)
                            {
                                foreach (var item in admissionIpdList)
                                {
                                    var InDate = item.indate;
                                    var OutDate = item.outdate;
                                    if (opDate_Format >= InDate && (opDate_Format <= OutDate))
                                    {
                                        feeNoList.Add(item.FeeNo);
                                    }
                                }
                            }
                        }
                        if (feeNoList.Count > 0)
                        {
                            string type_id = "3",
                                title = "手術日",
                                care_content = "病人於" + DateTime.Now.ToString("yyyy/MM/dd HH:mm") + "送至手術室。",
                                content = care_content;
                            string temp_item = "";

                            foreach (var feeNo in feeNoList)
                            {
                                string id = id = "EVENT" + "_" + userNo + "_" + feeNo + "_" + sourceKey + "_" + "0";
                                string sql = "SELECT EVENT_ID FROM NIS_SPECIAL_EVENT_DATA \n";
                                sql += "WHERE EVENT_ID = '" + id + "' \n";
                                DataTable dt = link.DBExecSQL(sql);
                                if (dt.Rows.Count > 0)
                                {
                                    return Content(JsonConvert.SerializeObject(new { isSuccess = true, messageByNotSuccess = "此Source Key已有紀錄" }));
                                }
                                else
                                {
                                    List<DBItem> insertDataList = new List<DBItem>();
                                    insertDataList.Add(new DBItem("EVENT_ID", id, DBItem.DBDataType.String));
                                    insertDataList.Add(new DBItem("FEENO", feeNo, DBItem.DBDataType.String));
                                    insertDataList.Add(new DBItem("CREATNO", userNo, DBItem.DBDataType.String));
                                    insertDataList.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                                    insertDataList.Add(new DBItem("TYPE_ID", type_id, DBItem.DBDataType.String));
                                    insertDataList.Add(new DBItem("CONTENT", content, DBItem.DBDataType.String));
                                    if (temp_item != "")
                                        insertDataList.Add(new DBItem("EDIT_ITEM", temp_item, DBItem.DBDataType.String));

                                    int erow = care_record_m.DBExecInsert("NIS_SPECIAL_EVENT_DATA", insertDataList);
                                    if (erow > 0)
                                    {
                                        DateTime NowTime = DateTime.Now;
                                        LogTool lt = new LogTool();
                                        CareRecord care_record_m = new CareRecord();
                                        string sign_userno = care_record_m.sel_guide_userno(userNo, NowTime, NowTime.Hour);
                                        insertDataList = new List<DBItem>();
                                        insertDataList.Add(new DBItem("CARERECORD_ID", id, DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("CREATNO", userNo, DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("GUIDE_NO", sign_userno, DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("CREATNAME", userName, DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("CREATTIME", NowTime.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                                        insertDataList.Add(new DBItem("RECORDTIME", NowTime.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                                        insertDataList.Add(new DBItem("FEENO", feeNo, DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("TITLE", trans_date(title), DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("C_OTHER", trans_date(care_content), DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("SELF", "SPE_EVENT", DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("SIGN", "N", DBItem.DBDataType.String));
                                        erow = link.DBExecInsert("CARERECORD_DATA", insertDataList);
                                        if (erow > 0)
                                        {
                                            try
                                            {
                                                string msg = "";
                                                if (trans_date(care_content) != "")
                                                    msg += trans_date(care_content) + "\n";// + trans_date(C)
                                                erow = EMR_Sign(NowTime.ToString("yyyy/MM/dd HH:mm:ss"), id, msg, title, "SPE_EVENT", feeNo, userNo);
                                            }
                                            catch (Exception ex)
                                            {
                                                lt.saveLogMsg("[SPE_EVENT]" + id + ex.Message, "EMR_Pre_Operation_Log");
                                            }
                                            return Content(JsonConvert.SerializeObject(new { isSuccess = true, messageByNotSuccess = "" }));
                                        }
                                        else
                                        {
                                            return Content(JsonConvert.SerializeObject(new { isSuccess = false, messageByNotSuccess = "新增特殊事件註記失敗" }));
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            return Content(JsonConvert.SerializeObject(new { isSuccess = false, messageByNotSuccess = "無符合時間的FeeNo" }));
                        }
                    }
                    else
                    {
                        return Content(JsonConvert.SerializeObject(new { isSuccess = false, messageByNotSuccess = "查無此病歷號的入院紀錄" }));
                    }
                }
            }
            catch (Exception ex)
            {
                this.log.saveLogMsg("SourceKey:" + sourceKey + " Error:" + ex.Message, "AISMarkSpecialEvent");
            }
            return Content(JsonConvert.SerializeObject(new { isSuccess = false, messageByNotSuccess = "查無此病歷號" }));
        }

        public class DataObject
        {
            public bool IsSuccess { get; set; }
            public string MessageByNotSuccess { get; set; }
            public List<Notification> Notifications { get; set; } = new List<Notification>();
        }

        public class Notification
        {
            public string Key { get; set; }
            public string SurgeryDate { get; set; }
            public string ChartNo { get; set; }
            public string FeeNo { get; set; }
            public string CostCenterCode { get; set; }
            public string SurgeryRoom { get; set; }
            public string SurgeryDepartment { get; set; }
            public string Diagnosis { get; set; }
            public string PredictProcedure { get; set; }
            public string MessageTime { get; set; }
            public string MessageUser { get; set; }
            public string Message { get; set; }
            public string Memo { get; set; }
            public string PatientName { get; set; }
            public string BedNo { get; set; }
            public string PatientID { get; set; }
        }

        public string GetOPS_WardNotification()
        {
            string errorMsg = "";
            DataObject resultJson = new DataObject();
            DataObject returnData = new DataObject();
            List<Notification> Notifications = new List<Notification>();
            Notification tempData = new Notification();
            List<Notification> matchDataList = new List<Notification>();
            try
            {
                var client = new HttpClient();
                client.Timeout = TimeSpan.FromMilliseconds(1000);
                var formURLFormat = "http://{0}/ECK_OPS_API/api/HIS_DataQuery/GetOPS_WardNotification";
                string url = NIS.MvcApplication.iniObj.NisSetting.Connection.AISUrl;
                var formURL = String.Format(formURLFormat, url);
                //var formURL = String.Format(formURLFormat, "10.169.8.11:8081", Url.Encode(ChartNo), Url.Encode(StartTime), Url.Encode(EndTime), Url.Encode(userinfo.CostCenterCode), Url.Encode(userinfo.EmployeesNo));  公司內測試用                      
                var response = client.GetAsync(formURL).Result;
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    this.log.saveLogMsg(userinfo.EmployeesNo.ToString() + "_TimeOut", "AISGetOPS");
                }
                resultJson = response.Content.ReadAsAsync<DataObject>().Result;
                Notifications = resultJson.Notifications;

                if (mode == "Maya")
                {
                    Notifications = new List<Notification>() {
                    new Notification{
                        Key="4-OPSc456123d240328165727",
                        SurgeryDate="2024/05/13",
                        ChartNo="05789469",
                        SurgeryRoom="01",
                        SurgeryDepartment="口腔外科 ",
                        Diagnosis="牙齒萌發障礙2",
                        PredictProcedure="複雜齒切除術",
                        MessageTime="2024-04-24T16:58:59",
                        MessageUser="Maya|Maya",
                        Message="手術房通知病人 NIS 測試 (456123)",
                        Memo="TEST"
                    },
                        new Notification() { },
                    new Notification{
                        Key="4-OPSc456123d240328165728",
                        SurgeryDate=DateTime.Now.AddHours(-1).ToString("yyyy/MM/dd HH:mm:ss"),
                        ChartNo="05236073",
                        SurgeryRoom="02",
                        SurgeryDepartment="口腔外科 ",
                        Diagnosis="牙齒萌發障礙2",
                        PredictProcedure="複雜齒切除術2",
                        MessageTime="2024-04-23T16:58:59",
                        MessageUser="Maya|Maya",
                        Message="手術房通知病人 NIS 測試 (456123)",
                        Memo="TEST",
                        BedNo="1A032",
                        CostCenterCode="871A"
                    },
                    new Notification{
                        Key="4-OPSc456123d240328165729",
                        SurgeryDate="2024/06/24",
                        ChartNo="06210380",
                        SurgeryRoom="03",
                        SurgeryDepartment="口腔外科 ",
                        Diagnosis="牙齒萌發障礙2",
                        PredictProcedure="複雜齒切除術3",
                        MessageTime="2024-03-28T16:58:59",
                        MessageUser="Maya|Maya",
                        Message="手術房通知病人 NIS 測試 (456123)",
                        Memo="TEST"
                    },
                };
                }

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    if (resultJson.IsSuccess)
                    {
                        var shift = "";
                        DateTime dateNow = DateTime.Now;
                        var dateNowStringWithoutTime = dateNow.ToString("yyyy-MM-dd 00:00:00");
                        int strtime = int.Parse(dateNow.ToString("HHmm"));
                        if (strtime >= 0 && strtime <= 759)
                            shift = "N";
                        else if (strtime >= 800 && strtime <= 1559)
                            shift = "D";
                        else if (strtime >= 1600 && strtime <= 2359)
                            shift = "E";
                        var userNo = userinfo.EmployeesNo;
                        if (Notifications.Count > 0)
                        {
                            var sql = "SELECT * FROM DATA_DISPATCHING \n";
                            sql += "WHERE SHIFT_CATE = '" + shift + "' \n";
                            sql += "AND SHIFT_DATE = to_date('" + dateNowStringWithoutTime + "', 'yyyy-MM-dd HH24:mi:ss')\n";
                            sql += "AND RESPONSIBLE_USER = '" + userinfo.EmployeesNo + "'\n";

                            var sw = new Stopwatch();
                            sw.Start();
                            DataTable dt = link.DBExecSQL(sql);
                            sw.Stop();
                            this.log.saveLogMsg($"[DATA_DISPATCHING 時間]{sw.ElapsedMilliseconds}ms", "AISGetOPS");
                            foreach (var Notification in Notifications)
                            {
                                if (string.IsNullOrEmpty(Notification.SurgeryDate))
                                    continue;

                                //檢查手術時間(昨天到今天)
                                var opDate = DateTime.Parse(Notification.SurgeryDate);
                                var startDate = DateTime.Parse(DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd 00:00:00"));
                                var endDate = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd 23:59:59"));
                                if (opDate >= startDate && opDate <= endDate)
                                {

                                    //檢查派班表判斷病人此時是否屬於登入護理師
                                    //var sql = "SELECT * FROM DATA_DISPATCHING \n";
                                    //sql += "WHERE SHIFT_CATE = '" + shift + "' \n";
                                    //sql += "AND TRIM(COST_CODE) = '" + Notification.CostCenterCode + "'\n";
                                    //sql += "AND BED_NO = '" + Notification.BedNo + "'\n";
                                    //sql += "AND SHIFT_DATE = to_date('" + dateNowStringWithoutTime + "', 'yyyy-MM-dd HH24:mi:ss')\n";
                                    //sql += "AND RESPONSIBLE_USER = '" + userinfo.EmployeesNo + "'\n";
                                    //DataTable dt = link.DBExecSQL(sql);
                                    var trans = dt.AsEnumerable();
                                    var isPatch = trans.Where(x => x["COST_CODE"].ToString().Trim() == Notification.CostCenterCode && x["BED_NO"].ToString() == Notification.BedNo).ToList();

                                    if (isPatch.Count > 0)
                                    {
                                        var MessageTime = DateTime.Parse(Notification.MessageTime).ToString("yyyy-MM-dd HH:mm:ss");
                                        Notification.MessageTime = MessageTime;
                                        matchDataList.Add(Notification);
                                    }
                                }
                            }
                            if (matchDataList.Count > 0)
                            {
                                returnData.Notifications = matchDataList;
                            }
                        }
                        else
                        {
                            errorMsg = resultJson.MessageByNotSuccess;
                        }
                    }
                    else if (response.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        errorMsg = "呼叫 WebAPI 時發生錯誤。";
                    }
                    else
                        throw new Exception($"呼叫 WebAPI 時，於呼叫時發生錯誤。(可能是網路問題) [Response:{resultJson}]");
                }
            }
            catch (Exception e)
            {
                this.log.saveLogMsg(userinfo.EmployeesNo.ToString() + e.Message, "AISGetOPS");
                this.log.saveLogMsg(JsonConvert.SerializeObject(Notifications), "AISGetOPS");
            }

            returnData.IsSuccess = resultJson.IsSuccess;
            returnData.MessageByNotSuccess = errorMsg;

            return JsonConvert.SerializeObject(returnData);
        }

        public ActionResult PatchD_ConfirmeWardNotification(List<string> notificationConfirmKeyList)
        {
            var userNo = userinfo.EmployeesNo;
            string errorMsg = "";
            var client = new HttpClient();
            DataObject resultJson = new DataObject();
            var isSuccessList = "";
            try
            {
                foreach (var Key in notificationConfirmKeyList)
                {
                    var Data = new
                    {
                        Key = Key,
                        ConfirmeUserID = userNo
                    };
                    var formURLFormat = "http://{0}/ECK_OPS_API/api/HIS_DataQuery/PatchD_ConfirmeWardNotification";
                    string url = NIS.MvcApplication.iniObj.NisSetting.Connection.AISUrl;
                    var formURL = String.Format(formURLFormat, url);
                    //var formURL = String.Format(formURLFormat, "10.169.8.11:8081", Url.Encode(ChartNo), Url.Encode(StartTime), Url.Encode(EndTime), Url.Encode(userinfo.CostCenterCode), Url.Encode(userinfo.EmployeesNo));  公司內測試用                      
                    var response = client.PostAsJsonAsync(formURL, Data).Result;
                    resultJson = response.Content.ReadAsAsync<DataObject>().Result;
                    var Notifications = resultJson.Notifications;
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        if (resultJson.IsSuccess)
                        {
                            isSuccessList += "Key:" + Key + " " + resultJson.IsSuccess + " ";
                            errorMsg += "";
                        }
                        else
                        {
                            errorMsg += "Key:" + Key + resultJson.MessageByNotSuccess;
                        }
                    }
                    else if (response.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        errorMsg = "呼叫 WebAPI 時發生錯誤。";
                    }
                    else
                    {
                        throw new Exception($"呼叫 WebAPI 時，於呼叫時發生錯誤。(可能是網路問題) [Response:{resultJson}]");
                    }
                }
            }
            catch (Exception e)
            {
                errorMsg = e.Message;
            }
            return Content(JsonConvert.SerializeObject(new { isSuccess = isSuccessList, messageByNotSuccess = errorMsg }));
        }

        public string GetNISPatientInfo(string chartNo)
        {
            var feeNo = "";
            var bedNo = "";
            var costCenterNo = "";
            var patientID = "";
            var patientName = "";
            DateTime dateNow = DateTime.Now;
            try
            {
                byte[] doByteCode = webService.GetInHistory(chartNo);
                if (doByteCode != null)
                {
                    string doJsonArr = CompressTool.DecompressString(doByteCode);
                    List<InHistory> IpdList = JsonConvert.DeserializeObject<List<InHistory>>(doJsonArr);
                    if (IpdList.Count > 0)
                    {
                        feeNo = IpdList[0].FeeNo;

                        //用批價號病人資訊
                        doByteCode = webService.GetPatientInfo(feeNo);
                        if (doByteCode != null)
                        {
                            string ptinfoJosnArr = CompressTool.DecompressString(doByteCode);
                            PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                            bedNo = pi.BedNo;
                            if (mode == "Maya")
                            {
                                costCenterNo = pi.CostCenterNo.Trim();
                            }
                            else
                            {
                                costCenterNo = pi.CostCenterCode.Trim();
                            }
                            patientID = pi.PatientID;
                            patientName = pi.PatientName;
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
            var patInfo = new
            {
                FeeNo = feeNo.Trim(),
                BedNo = bedNo.Trim(),
                CostCenterCode = costCenterNo.Trim(),
                PatientID = patientID.Trim(),
                PatientName = patientName.Trim(),
            };
            return JsonConvert.SerializeObject(patInfo);
        }

        public ActionResult GetAdmissionAssessment(string key, string feeno)
        {
            if (key != "AISConnection")
            {
                return Content(JsonConvert.SerializeObject(new { message = "key值錯誤!" }));
            }

            if (string.IsNullOrEmpty(feeno))
            {
                if (mode == "Maya")
                {
                    feeno = "I0332966";
                }
            }

            GetAdmissionAssessment Data = new GetAdmissionAssessment();

            try
            {
                //入院評估
                //手術經驗,住院經驗
                var table_id = "";
                var na_type = "";
                string[] itemList = { "param_hasipd", "param_ipd_past_reason", "param_ipd_past_location", "param_surgery", "param_ipd_surgery_reason", "param_ipd_surgery_location" };
                string sql = "SELECT * FROM \n";
                sql += "(SELECT ASSESSMENTMASTER.*,ROW_NUMBER() OVER (ORDER BY MODIFYTIME desc) SN \n";
                sql += "FROM ASSESSMENTMASTER \n";
                sql += "WHERE FEENO = '" + feeno + "' \n";
                sql += "AND STATUS != 'delete') \n";
                sql += "WHERE SN='1'";
                DataTable assessmentMaster = assessmentMaster = link.DBExecSQL(sql);
                if (assessmentMaster.Rows.Count > 0)
                {
                    table_id = assessmentMaster.Rows[0]["TABLEID"].ToString();
                    na_type = assessmentMaster.Rows[0]["NATYPE"].ToString();
                    sql = "SELECT * \n";
                    sql += "FROM ASSESSMENTDETAIL \n";
                    sql += "WHERE TABLEID = '" + table_id + "' \n";
                    sql += "AND (";
                    foreach (var item in itemList)
                    {
                        sql += "ITEMID LIKE '" + item + "%' OR ";
                    }
                    sql += "0=1) \n";
                    DataTable assessmentDetail = link.DBExecSQL(sql);
                    if (assessmentDetail.Rows.Count > 0)
                    {
                        var TotalResult = assessmentDetail.AsEnumerable(); //方便使用Linq查詢

                        //住院經驗
                        var HasIpd = TotalResult.Where(x => x["ITEMID"].ToString().Trim() == "param_hasipd")
                                    .Select(x => x["ITEMVALUE"].ToString().Trim())
                                    .FirstOrDefault();
                        if (HasIpd != null)
                        {
                            Data.HasIpd = HasIpd;
                            if (HasIpd == "有")
                            {
                                var TempList = new List<Dictionary<string, string>>();
                                var IpdPastReason = TotalResult
                                    .Where(x => x["ITEMID"].ToString().Trim() == "param_ipd_past_reason")
                                    .Select(x => x["ITEMVALUE"].ToString())
                                    .FirstOrDefault();
                                string[] IpdPastReasonList = { };
                                if (IpdPastReason != null)
                                {
                                    IpdPastReasonList = IpdPastReason.Split(',');
                                }
                                var IpdPastLocation = TotalResult
                                    .Where(x => x["ITEMID"].ToString().Trim() == "param_ipd_past_location")
                                    .Select(x => x["ITEMVALUE"].ToString())
                                    .FirstOrDefault();
                                string[] IpdPastLocationList = { };
                                if (IpdPastLocation != null)
                                {
                                    IpdPastLocationList = IpdPastLocation.Split(',');
                                }
                                for (var j = 0; j < (IpdPastReasonList.Length > 0 ? IpdPastReasonList.Length : IpdPastLocationList.Length); j++)
                                {
                                    var temp = new Dictionary<string, string>();
                                    temp.Add("Reason", IpdPastReasonList.Length > 0 ? IpdPastReasonList[j] : "");
                                    temp.Add("Location", IpdPastLocationList.Length > 0 ? IpdPastLocationList[j] : "");
                                    TempList.Add(temp);
                                }
                                Data.IpdPast = TempList;
                            }
                        }
                        //手術經驗
                        var HasSurgery = TotalResult.Where(x => x["ITEMID"].ToString().Trim() == "param_surgery")
                                    .Select(x => x["ITEMVALUE"].ToString().Trim())
                                    .FirstOrDefault();
                        if (HasSurgery != null)
                        {
                            Data.HasSurgery = HasSurgery;
                            if (HasSurgery == "有")
                            {
                                var TempList = new List<Dictionary<string, string>>();
                                var IpdSurgeryReason = TotalResult
                                    .Where(x => x["ITEMID"].ToString().Trim() == "param_ipd_past_reason")
                                    .Select(x => x["ITEMVALUE"].ToString())
                                    .FirstOrDefault();
                                string[] IpdSurgeryReasonList = { };
                                if (IpdSurgeryReason != null)
                                {
                                    IpdSurgeryReasonList = IpdSurgeryReason.Split(',');
                                }
                                var IpdSurgeryLocation = TotalResult
                                    .Where(x => x["ITEMID"].ToString().Trim() == "param_ipd_surgery_location")
                                    .Select(x => x["ITEMVALUE"].ToString())
                                    .FirstOrDefault();
                                string[] IpdSurgeryLocationList = { };
                                if (IpdSurgeryLocation != null)
                                {
                                    IpdSurgeryLocationList = IpdSurgeryLocation.Split(',');
                                }
                                for (var j = 0; j < (IpdSurgeryReasonList.Length > 0 ? IpdSurgeryReasonList.Length : IpdSurgeryLocationList.Length); j++)
                                {
                                    var temp = new Dictionary<string, string>();
                                    temp.Add("Reason", IpdSurgeryReasonList.Length > 0 ? IpdSurgeryReasonList[j] : "");
                                    temp.Add("Location", IpdSurgeryLocationList.Length > 0 ? IpdSurgeryLocationList[j] : "");
                                    TempList.Add(temp);
                                }
                                Data.IpdSurgery = TempList;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var error_message = ex.Message.ToString();
            }

            return Content(JsonConvert.SerializeObject(Data));
        }
    }
}
