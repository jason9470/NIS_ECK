using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NIS.WebService;
using System.Data.OleDb;
using System.Data;
using Newtonsoft.Json;
using NIS.Data;
using NIS.Models;
using NIS.UtilTool;
using System.Collections;

namespace NIS.Controllers
{
    public class ERController : BaseController
    {
        private CommData cd;
        private DBConnector link;
        //
        // GET: /ER/
        public ERController()
        {
            this.cd = new CommData();
            this.link = new DBConnector();
        }
        public ActionResult Index()
        {
            if (ptinfo == null)
                ViewBag.feeno = "";
            else
                ViewBag.feeno = ptinfo.FeeNo;
            return View();
        }

        public ActionResult Get_Detection_Result(string feeno)
        {
            byte[] TriageInfoByteCode = webService.GetTriageInfo(feeno);

            if (TriageInfoByteCode != null)
            {
                string TriageInfoJosnArr = NIS.UtilTool.CompressTool.DecompressString(TriageInfoByteCode);
                List<TriageInfo> TriageInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<List<TriageInfo>>(TriageInfoJosnArr);
                ViewData["TriageInfo"] = TriageInfo;
            }
            return View();
        }

        public ActionResult Get_ERConsultation(string feeno)
        {
            byte[] ERConsultationCode = webService.GetERConsultation(feeno);

            if (ERConsultationCode != null)
            {
                string ERConsultationJosnArr = NIS.UtilTool.CompressTool.DecompressString(ERConsultationCode);
                List<ERConsultation> ERConsultationInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ERConsultation>>(ERConsultationJosnArr);
                ViewData["ERConsultationInfo"] = ERConsultationInfo;
            }
            return View();
        }

        public ActionResult Get_TextOrderRecord(string feeno)
        {
            byte[] TextOrderRecordByteCode = webService.GetTextOrderRecord(feeno);

            if (TextOrderRecordByteCode != null)
            {
                string TextOrderRecordJosnArr = NIS.UtilTool.CompressTool.DecompressString(TextOrderRecordByteCode);
                List<TextOrderRecord> TextOrderRecordInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<List<TextOrderRecord>>(TextOrderRecordJosnArr);
                ViewData["TextOrderRecordInfo"] = TextOrderRecordInfo;
            }
            return View();
        }

        [HttpPost]
        public ActionResult CareRecordContent(string title, string content)
        {
            ViewBag.title = title;
            ViewBag.content = content;
            return View();
        }

        [HttpGet]
        public ActionResult TransferOut()
        {
            DataTable dt = link.DBExecSQL("select * from TRANSFEROUT where FEENO ='" + ptinfo.FeeNo + "'");
            ViewBag.dt = dt;
            ViewBag.save_status = "I";
            if (dt.Rows.Count > 0)
                ViewBag.save_status = dt.Rows[0]["SERIAL"].ToString() + "|" + dt.Rows[0]["ADD_DATE"].ToString();
            return View();
        }
        [HttpPost]
        public ActionResult TransferOut(
            string save_status, string PT_ACTION, string DISCHARGE_STATUS, string TRANFER_NAME, string TRANFER_TYPE, string REFERRAL, string OTHER, string BED_NO, string ACCOMPANIED, string TXT_ER_SITUATION_BT, string TXT_ER_SITUATION_MP,
            string TXT_ER_SITUATION_BF, string TXT_ER_SITUATION_BP1, string TXT_ER_SITUATION_BP2, string TXT_ER_SITUATION_SP, string DIRECTOR, string DIRECTOR_FAMILY_TXT, string DIRECTOR_OTHER_TXT, string DIRECTOR_TYPE, string OUT_TIME,
            string PFE_ITEM, string PFE_ITEM_OTHER_TXT, string REGISTERED_TYPE, string REGISTERED_TYPE_OTHER_TXT, string REG_DEPT, string REG_DATE, string REG_TYPE, string DISCHARGE_CHECK_LIST, string ELECTROCARDIOGRAM_AMOUNT, string PT_STATUS, string title, string content
            )
        {
            List<DBItem> insertDataList = new List<DBItem>();            
            //新增主表
            if (save_status == "I")
            {
                string TableID = base.creatid("TransferOut", userinfo.EmployeesNo, ptinfo.FeeNo, "");
                insertDataList.Clear();
                insertDataList.Add(new DBItem("SERIAL", TableID, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FEENO", ptinfo.FeeNo, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PT_ACTION", replace_null(PT_ACTION), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("DISCHARGE_STATUS", replace_null(DISCHARGE_STATUS), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("TRANFER_NAME", replace_null(TRANFER_NAME), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("TRANFER_TYPE", replace_null(TRANFER_TYPE), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("REFERRAL", replace_null(REFERRAL), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("OTHER", replace_null(OTHER), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("BED_NO", replace_null(BED_NO), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ACCOMPANIED", replace_null(ACCOMPANIED), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("TXT_ER_SITUATION_BT", replace_null(TXT_ER_SITUATION_BT), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("TXT_ER_SITUATION_MP", replace_null(TXT_ER_SITUATION_MP), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("TXT_ER_SITUATION_BF", replace_null(TXT_ER_SITUATION_BF), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("TXT_ER_SITUATION_BP1", replace_null(TXT_ER_SITUATION_BP1), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("TXT_ER_SITUATION_BP2", replace_null(TXT_ER_SITUATION_BP2), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("TXT_ER_SITUATION_SP", replace_null(TXT_ER_SITUATION_SP), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("DIRECTOR", replace_null(DIRECTOR), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("DIRECTOR_FAMILY_TXT", replace_null(DIRECTOR_FAMILY_TXT), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("DIRECTOR_OTHER_TXT", replace_null(DIRECTOR_OTHER_TXT), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("DIRECTOR_TYPE", replace_null(DIRECTOR_TYPE), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PFE_ITEM", replace_null(PFE_ITEM), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PFE_ITEM_OTHER_TXT", replace_null(PFE_ITEM_OTHER_TXT), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("REGISTERED_TYPE", replace_null(REGISTERED_TYPE), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("REGISTERED_TYPE_OTHER_TXT", replace_null(REGISTERED_TYPE_OTHER_TXT), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("REG_DEPT", replace_null(REG_DEPT), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("REG_DATE", replace_null(REG_DATE), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("REG_TYPE", replace_null(REG_TYPE), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("DISCHARGE_CHECK_LIST", replace_null(DISCHARGE_CHECK_LIST), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ELECTROCARDIOGRAM_AMOUNT", replace_null(ELECTROCARDIOGRAM_AMOUNT), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PT_STATUS", replace_null(PT_STATUS), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ADD_EMP", userinfo.EmployeesNo, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ADD_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("OUT_TIME", replace_null(OUT_TIME), DBItem.DBDataType.String));
                int errow = link.DBExecInsert("TransferOut", insertDataList);
                if (errow > 0)
                {
                    if (replace_null(PT_STATUS) == "Pending")
                        title = title + "-未結案";
                    Insert_CareRecord(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), TableID, title, content, "", "", "", "", "TransferOut");
                    if (PT_STATUS == "Pending")
                    {
                        insertDataList.Clear();
                        insertDataList.Add(new DBItem("NT_ID", TableID, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("STARTTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("FEE_NO", ptinfo.FeeNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("MEMO", "此病人尚未結案", DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("ACTIONLINK", "", DBItem.DBDataType.String));
                        link.DBExecInsert("DATA_NOTICE", insertDataList);
                    }
                    Response.Write("Success");
                }
                else
                {
                    Response.Write("Fail");
                }
            }
            else
            {
                insertDataList.Clear();                
                insertDataList.Add(new DBItem("PT_ACTION", replace_null(PT_ACTION), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("DISCHARGE_STATUS", replace_null(DISCHARGE_STATUS), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("TRANFER_NAME", replace_null(TRANFER_NAME), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("TRANFER_TYPE", replace_null(TRANFER_TYPE), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("REFERRAL", replace_null(REFERRAL), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("OTHER", replace_null(OTHER), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("BED_NO", replace_null(BED_NO), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ACCOMPANIED", replace_null(ACCOMPANIED), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("TXT_ER_SITUATION_BT", replace_null(TXT_ER_SITUATION_BT), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("TXT_ER_SITUATION_MP", replace_null(TXT_ER_SITUATION_MP), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("TXT_ER_SITUATION_BF", replace_null(TXT_ER_SITUATION_BF), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("TXT_ER_SITUATION_BP1", replace_null(TXT_ER_SITUATION_BP1), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("TXT_ER_SITUATION_BP2", replace_null(TXT_ER_SITUATION_BP2), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("TXT_ER_SITUATION_SP", replace_null(TXT_ER_SITUATION_SP), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("DIRECTOR", replace_null(DIRECTOR), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("DIRECTOR_FAMILY_TXT", replace_null(DIRECTOR_FAMILY_TXT), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("DIRECTOR_OTHER_TXT", replace_null(DIRECTOR_OTHER_TXT), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("DIRECTOR_TYPE", replace_null(DIRECTOR_TYPE), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PFE_ITEM", replace_null(PFE_ITEM), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PFE_ITEM_OTHER_TXT", replace_null(PFE_ITEM_OTHER_TXT), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("REGISTERED_TYPE", replace_null(REGISTERED_TYPE), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("REGISTERED_TYPE_OTHER_TXT", replace_null(REGISTERED_TYPE_OTHER_TXT), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("REG_DEPT", replace_null(REG_DEPT), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("REG_DATE", replace_null(REG_DATE), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("REG_TYPE", replace_null(REG_TYPE), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("DISCHARGE_CHECK_LIST", replace_null(DISCHARGE_CHECK_LIST), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ELECTROCARDIOGRAM_AMOUNT", replace_null(ELECTROCARDIOGRAM_AMOUNT), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PT_STATUS", replace_null(PT_STATUS), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EDIT_EMP", userinfo.EmployeesNo, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EDIT_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("OUT_TIME", replace_null(OUT_TIME), DBItem.DBDataType.String));
                int errow = link.DBExecUpdate("TransferOut", insertDataList, "FEENO ='" + ptinfo.FeeNo + "'");
                if (errow > 0)
                {
                    if (replace_null(PT_STATUS) == "Pending")
                        title = title + "-未結案";
                    Upd_CareRecord(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), save_status.Split('|').GetValue(0).ToString(), title, content, "", "", "", "", "TransferOut");
                    //Upd_CareRecord(save_status.Split('|').GetValue(1).ToString(), save_status.Split('|').GetValue(0).ToString(), title, content, "", "", "", "", "TransferOut");
                    // 20170331 更新護理紀錄後清除刪除註記
                    insertDataList.Clear();
                    insertDataList.Add(new DBItem("DELETED", "", DBItem.DBDataType.String));
                    link.DBExecUpdate("carerecord_data", insertDataList, "CARERECORD_ID ='" + save_status.Split('|').GetValue(0).ToString() + "'");

                    link.DBExecDelete("DATA_NOTICE", "NT_ID ='" + save_status.Split('|').GetValue(0).ToString() + "'");
                    if (PT_STATUS == "Pending")
                    {
                        string TableID = save_status.Split('|').GetValue(0).ToString();                        
                        insertDataList.Clear();
                        insertDataList.Add(new DBItem("NT_ID", TableID, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("STARTTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("FEE_NO", ptinfo.FeeNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("MEMO", "此病人尚未結案", DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("ACTIONLINK", "", DBItem.DBDataType.String));
                        link.DBExecInsert("DATA_NOTICE", insertDataList);
                    }
                    Response.Write("Success");
                }
                else
                {
                    Response.Write("Fail");
                }
            }            
            
            
            return new EmptyResult();
        }

        public string replace_null(string check_string)
        {
            string result;
            if(String.IsNullOrEmpty(check_string))
                result = "";
            else
                result = check_string;
            return result;
        }
        [HttpGet]
        public ActionResult PatientList()
        {
            byte[] ERListByteCode = webService.GetERListAll();

            if (ERListByteCode != null)
            {
                string ERListJosnArr = NIS.UtilTool.CompressTool.DecompressString(ERListByteCode);
                List<ERList> ERList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ERList>>(ERListJosnArr);
                ERList = ERList.OrderByDescending(x => x.RegTime).ToList();
                ViewData["result"] = ERList;
            }

            byte[] ERDeptListByteCode = webService.GetERDept();
            if (ERDeptListByteCode != null)
            {
                string ERDeptListJosnArr = NIS.UtilTool.CompressTool.DecompressString(ERDeptListByteCode);
                List<ERDeptList> ERDeptList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ERDeptList>>(ERDeptListJosnArr);
                ViewData["ERDeptList"] = ERDeptList;
            }
            ViewBag.startdate = DateTime.Now.AddDays(-1).ToString("yyyy/MM/dd");
            ViewBag.end = DateTime.Now.ToString("yyyy/MM/dd");
            ViewBag.status = "A";
            return View();
        }

        [HttpPost]
        public ActionResult PatientList(string chartno, string startdate, string enddate, string status)
        {
            if(chartno != "" )
            {
                ViewData["result"] = null;
                byte[] ERListByteCode = webService.GetERListbyChartNo(chartno);

                if (ERListByteCode != null)
                {
                    string ERListJosnArr = NIS.UtilTool.CompressTool.DecompressString(ERListByteCode);
                    List<ERList> ERList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ERList>>(ERListJosnArr);
                    ERList = ERList.OrderByDescending(x => x.RegTime).ToList();
                    ViewData["result"] = ERList;
                }
            }
            else
            {
                byte[] ERListByteCode = webService.GetERListbyDate(startdate, enddate, status);

                if (ERListByteCode != null)
                {
                    string ERListJosnArr = NIS.UtilTool.CompressTool.DecompressString(ERListByteCode);
                    List<ERList> ERList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ERList>>(ERListJosnArr);
                    if (status == "N")
                    {
                        //TODO:排除已結案的批價序號
                        string sql = "SELECT FEENO from TransferOut where PT_STATUS = 'Close' ";
                        DataTable dt = this.link.DBExecSQL(sql);
                        for (int i =0;i<dt.Rows.Count;i++)
                        {
                            //ERList.Remove(ERList.Find(x => x.FeeNo == dt.Rows[i][0].ToString()));
                            ERList.RemoveAll(x => x.FeeNo == dt.Rows[i][0].ToString());
                        }                        
                    }
                    ERList.RemoveAll(x => x.FeeNo == "");
                    ERList = ERList.OrderByDescending(x => x.RegTime).ToList();
                    
                    ViewData["result"] = ERList;
                }
            }
            

            byte[] ERDeptListByteCode = webService.GetERDept();
            if (ERDeptListByteCode != null)
            {
                string ERDeptListJosnArr = NIS.UtilTool.CompressTool.DecompressString(ERDeptListByteCode);
                List<ERDeptList> ERDeptList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ERDeptList>>(ERDeptListJosnArr);
                ViewData["ERDeptList"] = ERDeptList;
            }
            ViewBag.startdate = startdate;
            ViewBag.end = enddate;
            ViewBag.status = status;
            return View();
        }

    }
}
