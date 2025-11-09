using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using System.Xml;
using System.IO;
using NIS.Data;
using NIS.Models;
using NIS.UtilTool;
using NIS.WebService;
using Newtonsoft.Json;
using ChtEncryptLibrary;
using System.Configuration;

namespace NIS.Controllers
{
    public class ElectSignController : BaseController
    {
        CareRecord care_record_m = new CareRecord();
        Assess ass_m = new Assess();
        EMRReference.Service1 emr = new EMRReference.Service1();
        public ElectSignController()
        {
        }
        public ActionResult List()
        {
            ViewBag.NewEMR = ConfigurationManager.AppSettings["NewEMR"].ToString();
            return View();
        }

        //護理紀錄列表
        public ActionResult CareRecord_List(string start, string end, string type)
        {
            DateTime start_time = (start != null) ? Convert.ToDateTime(start) : DateTime.Now;
            DateTime end_time = (end != null) ? Convert.ToDateTime(end) : DateTime.Now;
            set_carerecord_dt(start_time, end_time, type);
            return View();
        }

        //給藥列表
        public ActionResult Med_List(string start, string end, string type)
        {
            DateTime start_time = (start != null) ? Convert.ToDateTime(start) : DateTime.Now;
            DateTime end_time = (end != null) ? Convert.ToDateTime(end) : DateTime.Now;
            set_med_dt(start_time, end_time, type);
            return View();
        }

        //入評列表
        public ActionResult Assessment_List(string start, string end, string type)
        {
            DateTime start_time = (start != null) ? Convert.ToDateTime(start) : DateTime.Now;
            DateTime end_time = (end != null) ? Convert.ToDateTime(end) : DateTime.Now;
            set_assess_dt(start_time, end_time, type);
            return View();
        }

        #region 執行簽章

        //呼叫簽章
        public ActionResult Call_Sign()
        {
            string pin_code = Request["pin_code"].ToString();
            ChtEncrypt cht = new ChtEncrypt();
            string CHTEncryptUrl = ConfigurationManager.AppSettings["CHTEncrypt"].ToString();
            string url = string.Empty;
            string error_msg = "";
            if (ConfigurationManager.AppSettings["NewEMR"].ToString() == "Y")
                url = cht.CHTEncrypt_AP(CHTEncryptUrl, userinfo.EmployeesNo, userinfo.Pwd, pin_code, "014,039,067", "NIS", ConfigurationManager.AppSettings["NewEMR_AutoClose"].ToString(), out error_msg);
            else
                url = cht.CHTEncrypt(CHTEncryptUrl, userinfo.EmployeesNo + "|" + userinfo.Pwd + "|" + Request.ServerVariables["LOCAL_ADDR"] + "|NIS");
            Response.Write("<script>window.open('" + url + "');window.location.href='List';</script>");

            return new EmptyResult();
        }

        //上傳區間內所有類型單張
        public ActionResult All_Sign(string start_date, string end_date)
        {
            DateTime start_time = (start_date != null) ? Convert.ToDateTime(start_date) : DateTime.Now;
            DateTime end_time = (end_date != null) ? Convert.ToDateTime(end_date) : DateTime.Now;
            DataTable dt_CareRecord = care_record_m.sel_carerecord(userinfo.EmployeesNo, "", start_time.AddDays(-1).ToString("yyyy/MM/dd 23:59:59"), end_time.AddDays(1).ToString("yyyy/MM/dd 00:00:00"), "N");
            DataTable dt_Med = care_record_m.sel_elect_med(userinfo.EmployeesNo, "", start_time.AddDays(-1).ToString("yyyy/MM/dd 23:59:59"), end_time.AddDays(1).ToString("yyyy/MM/dd 00:00:00"), "N");
            DataTable dt_Assess = ass_m.sel_assessment_list_for_elec_sign("", userinfo.EmployeesNo, start_time.AddDays(-1).ToString("yyyy/MM/dd 23:59:59"), end_time.AddDays(1).ToString("yyyy/MM/dd 00:00:00"), "N");
            string pin_code = Request["pincode"].ToString().Trim();
            string error_msg = "";

            string DocDKind_CareRecord = "014";
            string DocDKind_Med = "067";
            string DocDKind_Assess = "039";

            Send_CareRecord(dt_CareRecord, DocDKind_CareRecord);
            Send_Med(dt_Med, DocDKind_Med);
            Send_Assess(dt_Assess, DocDKind_Assess);

            ChtEncrypt cht = new ChtEncrypt();
            string CHTEncryptUrl = ConfigurationManager.AppSettings["CHTEncrypt"].ToString();
            string url = string.Empty;
            if (ConfigurationManager.AppSettings["NewEMR"].ToString() == "Y")
                url = cht.CHTEncrypt_AP(CHTEncryptUrl, userinfo.EmployeesNo, userinfo.Pwd, pin_code, DocDKind_CareRecord + "," + DocDKind_Med + "," + DocDKind_Assess, "NIS", ConfigurationManager.AppSettings["NewEMR_AutoClose"].ToString(), out error_msg);
            else
                url = cht.CHTEncrypt(CHTEncryptUrl, userinfo.EmployeesNo + "|" + userinfo.Pwd + "|" + Request.ServerVariables["LOCAL_ADDR"] + "|NIS");
            Response.Write("<script>window.open('" + url + "');window.location.href='List';</script>");

            return new EmptyResult();
        }

        //上傳護理紀錄
        public ActionResult CareRecord_Sign()
        {
            string id_list = "('" + Request["id_list"].ToString().Replace(",", "','") + "') ";
            DataTable dt = care_record_m.sel_carerecord("", id_list, "", "", "");
            string pin_code = Request["pin_code"].ToString();
            string DocDKind = "014";
            string error_msg = "";

            Send_CareRecord(dt, DocDKind);

            ChtEncrypt cht = new ChtEncrypt();
            string CHTEncryptUrl = ConfigurationManager.AppSettings["CHTEncrypt"].ToString();
            string url = string.Empty;
            if (ConfigurationManager.AppSettings["NewEMR"].ToString() == "Y")
                url = cht.CHTEncrypt_AP(CHTEncryptUrl, userinfo.EmployeesNo, userinfo.Pwd, pin_code, DocDKind, "NIS", ConfigurationManager.AppSettings["NewEMR_AutoClose"].ToString(), out error_msg);
            else
                url = cht.CHTEncrypt(CHTEncryptUrl, userinfo.EmployeesNo + "|" + userinfo.Pwd + "|" + Request.ServerVariables["LOCAL_ADDR"] + "|NIS");
            Response.Write("<script>window.open('" + url + "');window.location.href='List';</script>");

            return new EmptyResult();
        }

        //上傳給藥
        public ActionResult Med_Sign()
        {
            string id_list = "('" + Request["id_list"].ToString().Replace(",", "','") + "') ";
            DataTable dt = care_record_m.sel_elect_med("", id_list, "", "", "");
            string pin_code = Request["pin_code"].ToString();
            string DocDKind = "067";
            string error_msg = "";

            Send_Med(dt, DocDKind);

            ChtEncrypt cht = new ChtEncrypt();
            string CHTEncryptUrl = ConfigurationManager.AppSettings["CHTEncrypt"].ToString();
            string url = string.Empty;
            if (ConfigurationManager.AppSettings["NewEMR"].ToString() == "Y")
                url = cht.CHTEncrypt_AP(CHTEncryptUrl, userinfo.EmployeesNo, userinfo.Pwd, pin_code, DocDKind, "NIS", ConfigurationManager.AppSettings["NewEMR_AutoClose"].ToString(), out error_msg);
            else
                url = cht.CHTEncrypt(CHTEncryptUrl, userinfo.EmployeesNo + "|" + userinfo.Pwd + "|" + Request.ServerVariables["LOCAL_ADDR"] + "|NIS");
            Response.Write("<script>window.open('" + url + "');window.location.href='List';</script>");

            return new EmptyResult();
        }

        //上傳入評
        public ActionResult Assess_Sign()
        {
            string id_list = "('" + Request["id_list"].ToString().Replace(",", "','") + "') ";
            DataTable dt = ass_m.sel_assessment_list_for_elec_sign(id_list, "", "", "", "");
            string pin_code = Request["pin_code"].ToString();
            string DocDKind = "039";
            string error_msg = "";

            Send_Assess(dt, DocDKind);

            ChtEncrypt cht = new ChtEncrypt();
            string CHTEncryptUrl = ConfigurationManager.AppSettings["CHTEncrypt"].ToString();
            string url = string.Empty;
            if (ConfigurationManager.AppSettings["NewEMR"].ToString() == "Y")
                url = cht.CHTEncrypt_AP(CHTEncryptUrl, userinfo.EmployeesNo, userinfo.Pwd, pin_code, DocDKind, "NIS", ConfigurationManager.AppSettings["NewEMR_AutoClose"].ToString(), out error_msg);
            else
                url = cht.CHTEncrypt(CHTEncryptUrl, userinfo.EmployeesNo + "|" + userinfo.Pwd + "|" + Request.ServerVariables["LOCAL_ADDR"] + "|NIS");
            Response.Write("<script>window.open('" + url + "');window.location.href='List';</script>");

            return new EmptyResult();
        }

        //護理紀錄
        private void Send_CareRecord(DataTable dt, string DocDKind)
        {
            if (dt.Rows.Count > 0)
            {
                string feeno = dt.Rows[0]["FEENO"].ToString();

                byte[] listByteCode = webService.UserName(userinfo.EmployeesNo);
                string listJsonArray = CompressTool.DecompressString(listByteCode);
                UserInfo user_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                byte[] ptinfoByteCode = webService.GetPatientInfo(feeno);
                string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);

                string HospitalNHIID = ConfigurationManager.AppSettings["HospitalNHIID"].ToString();
                string HospitalName = ConfigurationManager.AppSettings["HospitalName"].ToString();
                string Confidentiality = "N";
                string record_user_no = (user_info.EmployeesNo != null) ? user_info.EmployeesNo.Trim() : "";
                string record_user_id = (user_info.UserID != null) ? user_info.UserID.Trim() : "";
                string record_user_name = (user_info.EmployeesName != null) ? user_info.EmployeesName.Trim() : "";
                string record_user_center_id = (user_info.CostCenterCode != null) ? user_info.CostCenterCode.Trim() : "";
                string record_user_center_name = (user_info.CostCenterName != null) ? user_info.CostCenterName.Trim() : "";
                //宣告xml基本檔
                string xml_carerecord = Transfer_XML(DocDKind, record_user_no, record_user_id, record_user_name, record_user_center_id, record_user_center_name);

                //宣告上傳所需變數
                string PatientName = pi.PatientName;
                string PatientID = (pi.PatientID != null) ? pi.PatientID.Trim() : "";
                string Gender = pi.PatientGender.Trim();
                string ChartNo = pi.ChartNo;
                foreach (DataRow r in dt.Rows)
                {
                    if (feeno != r["FEENO"].ToString())
                    {
                        feeno = r["FEENO"].ToString();
                        ptinfoByteCode = webService.GetPatientInfo(feeno);
                        if (ptinfoByteCode != null)
                        {
                            ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                            pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                            PatientName = pi.PatientName;
                            PatientID = (pi.PatientID != null) ? pi.PatientID.Trim() : "";
                            Gender = pi.PatientGender;
                            ChartNo = pi.ChartNo;
                        }
                    }
                    //宣告上傳所需變數
                    string OrderNO = r["CARERECORD_ID"].ToString() + r["SELF"].ToString();
                    string GenDocDate = DateTime.Now.ToString("yyyyMMddHHmmss");
                    string ErrMsg = "";
                    //宣告 replace xml 所需變數
                    DateTime temp_date = Convert.ToDateTime(r["RECORDTIME"]);
                    string record_day = temp_date.AddYears(-1911).ToString("yyyy") + temp_date.ToString("MMddHHmmss");
                    string recorded_username = r["CREATNAME"].ToString();
                    string record_datetime = (temp_date.Year - 1911).ToString() + temp_date.ToString("/MM/dd HH:mm");
                    string record_title = r["TITLE"].ToString();
                    string record_content = "";
                    if (r["C_OTHER"].ToString() != "" || r["C"].ToString() != "")
                        record_content += "一般：" + trans_special_code(r["C_OTHER"].ToString()) + trans_special_code(r["C"].ToString());
                    if (r["S_OTHER"].ToString() != "" || r["S"].ToString() != "")
                        record_content += "S：" + trans_special_code(r["S_OTHER"].ToString()) + trans_special_code(r["S"].ToString());
                    if (r["O_OTHER"].ToString() != "" || r["O"].ToString() != "")
                        record_content += "O：" + trans_special_code(r["O_OTHER"].ToString()) + trans_special_code(r["O"].ToString());
                    if (r["I_OTHER"].ToString() != "" || r["I"].ToString() != "")
                        record_content += "I：" + trans_special_code(r["I_OTHER"].ToString()) + trans_special_code(r["I"].ToString());
                    if (r["E_OTHER"].ToString() != "" || r["E"].ToString() != "")
                        record_content += "E：" + trans_special_code(r["E_OTHER"].ToString()) + trans_special_code(r["E"].ToString());

                    //替換xml
                    string CDAXml = Transfer_CareRecord_XML(pi, record_day, record_datetime, record_title, record_content, record_user_no, recorded_username, GenDocDate, OrderNO, xml_carerecord);
                    //上傳
                    emr.Url = ConfigurationManager.AppSettings["EMRReference"].ToString();
                    bool success = emr.EMRExchange(HospitalNHIID, HospitalName, DocDKind, OrderNO, GenDocDate, PatientName, PatientID, Gender, ChartNo, record_user_id, record_user_name, Confidentiality, CDAXml, out ErrMsg);

                    if (success)
                    {
                        string order_no = r["CARERECORD_ID"].ToString() + r["SELF"].ToString();
                        string where = "CARERECORD_ID || SELF = '" + order_no + "' ";
                        List<DBItem> insertDataList = new List<DBItem>();
                        insertDataList.Add(new DBItem("SIGN", "Y", DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("SIGNTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("VER", "(SELECT (NVL(VER,0) +1) FROM CARERECORD_DATA WHERE " + where + ")", DBItem.DBDataType.Number));
                        care_record_m.DBExecUpdate("CARERECORD_DATA", insertDataList, where);
                        //將紀錄回寫至 EMR Temp Table
                        insertDataList.Clear();
                        insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                        care_record_m.DBExecUpdate("NIS_EMRMS", insertDataList, "ORDER_NO = '" + order_no + "'");
                    }
                    else
                    {
                        List<DBItem> insertDataList = new List<DBItem>();
                        insertDataList.Add(new DBItem("SIGN", ErrMsg, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("SIGNTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                        string where = "CARERECORD_ID || SELF = '" + r["CARERECORD_ID"].ToString() + r["SELF"].ToString() + "' ";
                        care_record_m.DBExecUpdate("CARERECORD_DATA", insertDataList, where);
                    }
                }
            }
        }

        //給藥
        private void Send_Med(DataTable dt, string DocDKind)
        {
            if (dt.Rows.Count > 0)
            {
                string feeno = dt.Rows[0]["FEE_NO"].ToString();

                byte[] listByteCode = webService.UserName(userinfo.EmployeesNo);
                string listJsonArray = CompressTool.DecompressString(listByteCode);
                UserInfo user_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                byte[] ptinfoByteCode = webService.GetPatientInfo(feeno);
                string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);

                string HospitalNHIID = ConfigurationManager.AppSettings["HospitalNHIID"].ToString();
                string HospitalName = ConfigurationManager.AppSettings["HospitalName"].ToString();
                string Confidentiality = "N";
                string record_user_no = (user_info.EmployeesNo != null) ? user_info.EmployeesNo.Trim() : "";
                string record_user_id = (user_info.UserID != null) ? user_info.UserID.Trim() : "";
                string record_user_name = (user_info.EmployeesName != null) ? user_info.EmployeesName.Trim() : "";
                string record_user_center_id = (user_info.CostCenterCode != null) ? user_info.CostCenterCode.Trim() : "";
                string record_user_center_name = (user_info.CostCenterName != null) ? user_info.CostCenterName.Trim() : "";
                //宣告xml基本檔
                string xml_med = Transfer_XML(DocDKind, record_user_no, record_user_id, record_user_name, record_user_center_id, record_user_center_name);

                //宣告上傳所需變數
                string PatientName = pi.PatientName;
                string PatientID = (pi.PatientID != null) ? pi.PatientID.Trim() : "";
                string Gender = pi.PatientGender;
                string ChartNo = pi.ChartNo;
                string PhysicianID = pi.DocNo;
                string PhysicianName = pi.DocName;

                foreach (DataRow r in dt.Rows)
                {
                    //藥物內容格式
                    string content = "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                    content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                    content += "<code code=\"46240-8\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"History of hospitalizations+History of outpatient visits\" />";
                    content += "<title>#Title#</title>";
                    content += "<text>";
                    content += "<list>";
                    content += "<item>#DrugName#</item>";
                    content += "<item>#Unit#</item>";
                    content += "<item>#Path#</item>";
                    content += "<item>#Seq#</item>";
                    content += "</list>";
                    content += "</text>";
                    content += "</section>";
                    content += "</component>";

                    if (feeno != r["FEE_NO"].ToString())
                    {
                        feeno = r["FEE_NO"].ToString();
                        ptinfoByteCode = webService.GetPatientInfo(feeno);
                        if (ptinfoByteCode != null)
                        {
                            ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                            pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                            PatientName = pi.PatientName;
                            PatientID = (pi.PatientID != null) ? pi.PatientID.Trim() : "";
                            Gender = pi.PatientGender;
                            ChartNo = pi.ChartNo;
                        }
                    }
                    //宣告上傳所需變數
                    string OrderNO = r["UD_SEQPK"].ToString();
                    string GenDocDate = DateTime.Now.ToString("yyyyMMddHHmmss");
                    string record_day = Convert.ToDateTime(r["DRUG_DATE"]).AddYears(-1911).ToString("yyyy") + Convert.ToDateTime(r["DRUG_DATE"]).ToString("MMddHHmmss");
                    string ErrMsg = "";

                    if (r["UD_TYPE"].ToString() == "R")
                        content = content.Replace("#Title#", "常規藥物");
                    else if (r["UD_TYPE"].ToString() == "P")
                        content = content.Replace("#Title#", "必要時給予(PRN)藥物");
                    else if (r["UD_TYPE"].ToString() == "S")
                        content = content.Replace("#Title#", "立即給予(Stat)藥物");

                    content = content.Replace("#DrugName#", trans_special_code(r["MED_DESC"].ToString()));
                    content = content.Replace("#Unit#", trans_special_code(r["USE_DOSE"].ToString() + r["UD_UNIT"].ToString()));
                    content = content.Replace("#Path#", trans_special_code(r["UD_PATH"].ToString()));
                    content = content.Replace("#Seq#", trans_special_code(r["UD_CIR"].ToString()));

                    //替換xml
                    string CDAXml = Transfer_Med_XML(pi, xml_med, content, OrderNO, GenDocDate, record_day);

                    //上傳
                    emr.Url = ConfigurationManager.AppSettings["EMRReference"].ToString();
                    bool success = emr.EMRExchange(HospitalNHIID, HospitalName, DocDKind, OrderNO, GenDocDate, PatientName, PatientID, Gender, ChartNo, record_user_id, record_user_name, Confidentiality, CDAXml, out ErrMsg);
                    if (success)
                    {
                        string where = "UD_SEQPK = '" + r["UD_SEQPK"].ToString() + "' ";
                        List<DBItem> insertDataList = new List<DBItem>();
                        insertDataList.Add(new DBItem("RECORD_ID", "Y", DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("RECORD_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                        care_record_m.DBExecUpdate("DRUG_EXECUTE", insertDataList, where);
                        //將紀錄回寫至 EMR Temp Table
                        insertDataList.Clear();
                        insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                        care_record_m.DBExecUpdate("NIS_EMRMS", insertDataList, "ORDER_NO = '" + r["UD_SEQPK"].ToString() + "'");
                    }
                    else
                    {
                        string where = "UD_SEQPK = '" + r["UD_SEQPK"].ToString() + "' ";
                        List<DBItem> insertDataList = new List<DBItem>();
                        insertDataList.Add(new DBItem("RECORD_ID", ErrMsg, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("RECORD_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                        care_record_m.DBExecUpdate("DRUG_EXECUTE", insertDataList, where);
                    }
                }
            }
        }

        //入評
        private void Send_Assess(DataTable dt, string DocDKind)
        {
            if (dt.Rows.Count > 0)
            {
                string feeno = dt.Rows[0]["FEENO"].ToString();

                byte[] listByteCode = webService.UserName(userinfo.EmployeesNo);
                string listJsonArray = CompressTool.DecompressString(listByteCode);
                UserInfo user_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                byte[] ptinfoByteCode = webService.GetPatientInfo(feeno);
                string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);

                string HospitalNHIID = ConfigurationManager.AppSettings["HospitalNHIID"].ToString();
                string HospitalName = ConfigurationManager.AppSettings["HospitalName"].ToString();
                string Confidentiality = "N";
                string record_user_no = (user_info.EmployeesNo != null) ? user_info.EmployeesNo.Trim() : "";
                string record_user_id = (user_info.UserID != null) ? user_info.UserID.Trim() : "";
                string record_user_name = (user_info.EmployeesName != null) ? user_info.EmployeesName.Trim() : "";
                string record_user_center_id = (user_info.CostCenterCode != null) ? user_info.CostCenterCode.Trim() : "";
                string record_user_center_name = (user_info.CostCenterName != null) ? user_info.CostCenterName.Trim() : "";
                //宣告xml基本檔
                string xml_assess = Transfer_XML(DocDKind, record_user_no, record_user_id, record_user_name, record_user_center_id, record_user_center_name);

                //宣告上傳所需變數
                string PatientName = pi.PatientName;
                string PatientID = (pi.PatientID != null) ? pi.PatientID.Trim() : "";
                string Gender = pi.PatientGender;
                string ChartNo = pi.ChartNo;
                string PhysicianID = pi.DocNo;
                string PhysicianName = pi.DocName;

                foreach (DataRow r in dt.Rows)
                {
                    if (feeno != r["FEENO"].ToString())
                    {
                        feeno = r["FEENO"].ToString();
                        ptinfoByteCode = webService.GetPatientInfo(feeno);
                        if (ptinfoByteCode != null)
                        {
                            ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                            pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                            PatientName = pi.PatientName;
                            PatientID = (pi.PatientID != null) ? pi.PatientID.Trim() : "";
                            Gender = pi.PatientGender;
                            ChartNo = pi.ChartNo;
                        }
                    }
                    //宣告上傳所需變數
                    //20140923 mod by yungchen 因僅抓病歷號的單號與出院病摘重覆 故加上簽章類別039
                    string OrderNO = r["FEENO"].ToString()+"039";
                    string GenDocDate = DateTime.Now.ToString("yyyyMMddHHmmss");
                    string record_day = Convert.ToDateTime(r["MODIFYTIME"]).AddYears(-1911).ToString("yyyy") + Convert.ToDateTime(r["MODIFYTIME"]).ToString("MMddHHmmss");
                    string ErrMsg = "";
                    string content = set_admission_content(r["TABLEID"].ToString(), r["NATYPE"].ToString(), feeno);

                    //替換xml
                    string CDAXml = Transfer_Assess_XML(pi, xml_assess, content, OrderNO, GenDocDate, record_day);

                   // //back xml 
                    //if (!debug_mode)
                    //{
                    //    XmlDocument xd = new XmlDocument();
                    //    xd.LoadXml(CDAXml);
                    //    if (!System.IO.Directory.Exists(AppDomain.CurrentDomain.BaseDirectory.ToString() + "/xml/"))
                    //        System.IO.Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory.ToString() + "/xml/");
                    //    xd.Save(AppDomain.CurrentDomain.BaseDirectory.ToString() + "/xml/" + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + OrderNO);
                    //    xd = null;
                    //}

                    //上傳
                    emr.Url = ConfigurationManager.AppSettings["EMRReference"].ToString();
                    bool success = emr.EMRExchange(HospitalNHIID, HospitalName, DocDKind, OrderNO, GenDocDate, PatientName, PatientID, Gender, ChartNo, record_user_id, record_user_name, Confidentiality, CDAXml, out ErrMsg);

                    if (success)
                    {
                        string where = "FEENO = '" + r["FEENO"].ToString() + "' ";
                        List<DBItem> insertDataList = new List<DBItem>();
                        insertDataList.Add(new DBItem("SIGN", "Y", DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("SIGNTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("VER", "(SELECT (NVL(MAX(VER),0) +1) FROM ASSESSMENTMASTER WHERE " + where + ")", DBItem.DBDataType.Number));
                        care_record_m.DBExecUpdate("ASSESSMENTMASTER", insertDataList, where);
                        //將紀錄回寫至 EMR Temp Table
                        insertDataList.Clear();
                        insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                        care_record_m.DBExecUpdate("NIS_EMRMS", insertDataList, "ORDER_NO = '" + r["FEENO"].ToString() + "039'");
                    }
                    else
                    {
                        string where = "FEENO = '" + r["FEENO"].ToString() + "' ";
                        List<DBItem> insertDataList = new List<DBItem>();
                        insertDataList.Add(new DBItem("SIGN", ErrMsg, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("SIGNTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                        care_record_m.DBExecUpdate("ASSESSMENTMASTER", insertDataList, where);
                    }
                }
            }
        }

        #endregion

        #region 替換xml

        public string Transfer_XML(string filename, string record_user_no, string record_user_id, string record_user_name, string record_user_center_id, string record_user_center_name)
        {
            // 載入範例檔
            string strPath = @"C:\\wkhtmltopdf\\" + filename + ".xml";
            XmlDocument doc1 = new XmlDocument();
            doc1.Load(strPath);
            doc1.Save(strPath);
            StringWriter sw = new StringWriter();
            XmlTextWriter tx = new XmlTextWriter(sw);
            doc1.WriteTo(tx);
            string str = sw.ToString();
            str = str.Replace("#record_user_no#", trans_special_code(record_user_no));
            str = str.Replace("#record_user_id#", trans_special_code(record_user_id));
            str = str.Replace("#record_user_name#", trans_special_code(record_user_name));
            str = str.Replace("#record_user_center_id#", trans_special_code(record_user_center_id));
            str = str.Replace("#record_user_center_name#", trans_special_code(record_user_center_name));

            return str;
        }

        public string Transfer_CareRecord_XML(PatientInfo pi, string record_day, string record_datetime, string record_title, string record_content, string user_no, string recorded_username, string GenDocDate, string OrderNO, string xml_carerecord)
        {
            string str = xml_carerecord;
            str = str.Replace("#Today_DateTime#", trans_special_code(GenDocDate));
            str = str.Replace("#pt_chartno#", trans_special_code(pi.ChartNo));
            str = str.Replace("#pt_id#", trans_special_code(pi.PatientID));
            str = str.Replace("#pt_name#", trans_special_code(pi.PatientName));
            str = str.Replace("#pt_gender#", trans_special_code(pi.PatientGender));
            str = str.Replace("#pt_birthday#", trans_special_code(pi.Birthday.ToString("yyyyMMdd")));
            str = str.Replace("#Doc_no#", trans_special_code(pi.DocNo));
            str = str.Replace("#Doc_name#", trans_special_code(pi.DocName));
            str = str.Replace("#pt_dept_name#", trans_special_code(pi.DeptName));
            str = str.Replace("#pt_dept_id#", trans_special_code(pi.DeptNo));
            str = str.Replace("#Today_Day#", DateTime.Now.ToString("yyyyMMdd"));
            str = str.Replace("#user_no#", trans_special_code(user_no));
            str = str.Replace("#OrderNO#", trans_special_code(OrderNO));
            str = str.Replace("#record_day#", trans_special_code(record_day));
            str = str.Replace("#recorded_username#", trans_special_code(recorded_username));
            str = str.Replace("#record_datetime#", trans_special_code(record_datetime));
            str = str.Replace("#record_title#", trans_special_code(record_title));
            str = str.Replace("#record_content#", trans_special_code(record_content));
            return str;
        }

        public string Transfer_Med_XML(PatientInfo pi, string xml_med, string component, string OrderNO, string GenDocDate, string record_day)
        {
            string str = xml_med;
            str = str.Replace("#Today_DateTime#", trans_special_code(GenDocDate));
            str = str.Replace("#pt_chartno#", trans_special_code(pi.ChartNo));
            str = str.Replace("#pt_address#", (pi.PatientAddress.Trim() == "") ? "無" : trans_special_code(pi.PatientAddress));
            str = str.Replace("#pt_HomeNo#", (pi.PatientHomeNo.Trim() == "") ? "無" : trans_special_code(pi.PatientHomeNo));
            str = str.Replace("#pt_WorkNo#", (pi.PatientWorkNo.Trim() == "") ? "無" : trans_special_code(pi.PatientWorkNo));
            str = str.Replace("#pt_Mobile#", (pi.PatientMobile.Trim() == "") ? "無" : trans_special_code(pi.PatientMobile));
            str = str.Replace("#pt_email#", (pi.PatientEmail.Trim() == "") ? "無" : trans_special_code(pi.PatientEmail));
            str = str.Replace("#pt_id#", (pi.PatientID.Trim() == "") ? "無" : trans_special_code(pi.PatientID));
            str = str.Replace("#pt_name#", (pi.PatientName.Trim() == "") ? "無" : trans_special_code(pi.PatientName));
            str = str.Replace("#pt_gender#", trans_special_code(pi.PatientGender));
            str = str.Replace("#pt_birthday#", trans_special_code(pi.Birthday.ToString("yyyyMMdd")));
            str = str.Replace("#pt_Marry#", (pi.PatientMarryStatus.Trim() == "") ? "無" : trans_special_code(pi.PatientMarryStatus));
            str = str.Replace("#pt_Religion#", (pi.PatientReligion.Trim() == "") ? "無" : trans_special_code(pi.PatientReligion));
            str = str.Replace("#pt_BirthPlace#", (pi.PatientBirthPlace.Trim() == "") ? "無" : trans_special_code(pi.PatientBirthPlace));
            str = str.Replace("#Today_Day#", DateTime.Now.ToString("yyyyMMdd"));
            str = str.Replace("#Doc_no#", trans_special_code(pi.DocNo));
            str = str.Replace("#Doc_name#", trans_special_code(pi.DocName));
            str = str.Replace("#pt_dept_name#", trans_special_code(pi.DeptName));
            str = str.Replace("#pt_dept_id#", trans_special_code(pi.DeptNo));
            str = str.Replace("#SpouseName#", (pi.PatientSpouseName.Trim() == "") ? "無" : trans_special_code(pi.PatientSpouseName));
            str = str.Replace("#Contact_Relationship#", (pi.ContactRelationship.Trim() == "") ? "無" : trans_special_code(pi.ContactRelationship));
            str = str.Replace("#Contact_HomeNo#", (pi.ContactHomeNo.Trim() == "") ? "無" : trans_special_code(pi.ContactHomeNo));
            str = str.Replace("#Contact_WorkNo#", (pi.ContactWorkNo.Trim() == "") ? "無" : trans_special_code(pi.ContactWorkNo));
            str = str.Replace("#Contact_Mobile#", (pi.ContactMobile.Trim() == "") ? "無" : trans_special_code(pi.ContactMobile));
            str = str.Replace("#Contact_Email#", (pi.Contactemail.Trim() == "") ? "無" : trans_special_code(pi.Contactemail));
            str = str.Replace("#Contact_Name#", (pi.ContactName.Trim() == "") ? "無" : trans_special_code(pi.ContactName));
            str = str.Replace("#Indays#", trans_special_code(pi.InDay.ToString()));
            str = str.Replace("#Indate#", trans_special_code(pi.InDate.ToString("yyyyMMdd")));
            str = str.Replace("#pt_bedno#", trans_special_code(pi.BedNo));
            str = str.Replace("#OrderNO#", trans_special_code(OrderNO));
            str = str.Replace("#record_day#", trans_special_code(record_day));
            str = str.Replace("#component#", component);
            return str;
        }

        public string Transfer_Assess_XML(PatientInfo pi, string xml_assess, string assess_data, string OrderNO, string GenDocDate, string record_day)
        {
            string str = xml_assess;
            str = str.Replace("#Today_DateTime#", trans_special_code(GenDocDate));
            str = str.Replace("#pt_chartno#", trans_special_code(pi.ChartNo));
            str = str.Replace("#pt_address#", (pi.PatientAddress.Trim() == "") ? "無" : trans_special_code(pi.PatientAddress));
            str = str.Replace("#pt_HomeNo#", (pi.PatientHomeNo.Trim() == "") ? "無" : trans_special_code(pi.PatientHomeNo));
            str = str.Replace("#pt_WorkNo#", (pi.PatientWorkNo.Trim() == "") ? "無" : trans_special_code(pi.PatientWorkNo));
            str = str.Replace("#pt_Mobile#", (pi.PatientMobile.Trim() == "") ? "無" : trans_special_code(pi.PatientMobile));
            str = str.Replace("#pt_email#", (pi.PatientEmail.Trim() == "") ? "無" : trans_special_code(pi.PatientEmail));
            str = str.Replace("#pt_id#", (pi.PatientID.Trim() == "") ? "無" : trans_special_code(pi.PatientID));
            str = str.Replace("#pt_name#", (pi.PatientName.Trim() == "") ? "無" : trans_special_code(pi.PatientName));
            str = str.Replace("#pt_gender#", trans_special_code(pi.PatientGender));
            str = str.Replace("#pt_birthday#", trans_special_code(pi.Birthday.ToString("yyyyMMdd")));
            str = str.Replace("#pt_dept_name#", trans_special_code(pi.DeptName));
            str = str.Replace("#pt_Marry#", (pi.PatientMarryStatus.Trim() == "") ? "無" : trans_special_code(pi.PatientMarryStatus));
            str = str.Replace("#pt_Religion#", (pi.PatientReligion.Trim() == "") ? "無" : trans_special_code(pi.PatientReligion));
            str = str.Replace("#pt_BirthPlace#", (pi.PatientBirthPlace.Trim() == "") ? "無" : trans_special_code(pi.PatientBirthPlace));
            str = str.Replace("#SpouseName#", (pi.PatientSpouseName.Trim() == "") ? "無" : trans_special_code(pi.PatientSpouseName));
            str = str.Replace("#Contact_Relationship#", (pi.ContactRelationship.Trim() == "") ? "無" : trans_special_code(pi.ContactRelationship));
            str = str.Replace("#Contact_HomeNo#", (pi.ContactHomeNo.Trim() == "") ? "無" : trans_special_code(pi.ContactHomeNo));
            str = str.Replace("#Contact_WorkNo#", (pi.ContactWorkNo.Trim() == "") ? "無" : trans_special_code(pi.ContactWorkNo));
            str = str.Replace("#Contact_Mobile#", (pi.ContactMobile.Trim() == "") ? "無" : trans_special_code(pi.ContactMobile));
            str = str.Replace("#Contact_Email#", (pi.Contactemail.Trim() == "") ? "無" : trans_special_code(pi.Contactemail));
            str = str.Replace("#Contact_Name#", (pi.ContactName.Trim() == "") ? "無" : trans_special_code(pi.ContactName));
            str = str.Replace("#Indays#", trans_special_code(pi.InDay.ToString()));
            str = str.Replace("#Indate#", trans_special_code(pi.InDate.ToString("yyyyMMdd")));
            str = str.Replace("#pt_bedno#", trans_special_code(pi.BedNo));
            str = str.Replace("#OrderNO#", trans_special_code(OrderNO));
            str = str.Replace("#record_day#", trans_special_code(record_day));
            str = str.Replace("#assess_data#", assess_data);
            return str;
        }

        #endregion

        #region 取得病患床號_姓名

        /// <summary>
        /// 取得病患床號_姓名
        /// </summary>
        private void set_carerecord_dt(DateTime start, DateTime end, string type)
        {
            DataTable dt = care_record_m.sel_carerecord(userinfo.EmployeesNo, "", start.AddDays(-1).ToString("yyyy/MM/dd 23:59:59"), end.AddDays(1).ToString("yyyy/MM/dd 00:00:00"), type);
            dt.Columns.Add("chartno");
            dt.Columns.Add("badno");
            dt.Columns.Add("ptname");
            if (dt.Rows.Count > 0)
            {
                string feeno = dt.Rows[0]["FEENO"].ToString();
                byte[] ptinfoByteCode = webService.GetPatientInfo(feeno);
                string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                foreach (DataRow r in dt.Rows)
                {
                    if (feeno != r["FEENO"].ToString())
                    {
                        feeno = r["FEENO"].ToString();
                        ptinfoByteCode = webService.GetPatientInfo(feeno);
                        if (ptinfoByteCode != null)
                        {
                            ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                            pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                        }
                    }
                    r["chartno"] = pi.ChartNo;
                    r["badno"] = pi.BedNo;
                    r["ptname"] = pi.PatientName;
                }
            }
            ViewBag.dt = dt;
            ViewBag.type = type;
        }

        /// <summary>
        /// 取得病患床號_姓名
        /// </summary>
        private void set_med_dt(DateTime start, DateTime end, string type)
        {
            DataTable dt = care_record_m.sel_elect_med(userinfo.EmployeesNo, "", start.AddDays(-1).ToString("yyyy/MM/dd 23:59:59"), end.AddDays(1).ToString("yyyy/MM/dd 00:00:00"), type);
            dt.Columns.Add("chartno");
            dt.Columns.Add("badno");
            dt.Columns.Add("ptname");
            if (dt.Rows.Count > 0)
            {
                string feeno = dt.Rows[0]["FEE_NO"].ToString();
                byte[] ptinfoByteCode = webService.GetPatientInfo(feeno);
                string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                foreach (DataRow r in dt.Rows)
                {
                    if (feeno != r["FEE_NO"].ToString())
                    {
                        feeno = r["FEE_NO"].ToString();
                        ptinfoByteCode = webService.GetPatientInfo(feeno);
                        if (ptinfoByteCode != null)
                        {
                            ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                            pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                        }
                    }
                    r["chartno"] = pi.ChartNo;
                    r["badno"] = pi.BedNo;
                    r["ptname"] = pi.PatientName;
                }
            }
            ViewBag.dt = dt;
            ViewBag.type = type;
        }

        /// <summary>
        /// 取得病患床號_姓名
        /// </summary>
        private void set_assess_dt(DateTime start, DateTime end, string type)
        {
            DataTable dt = ass_m.sel_assessment_list_for_elec_sign("", userinfo.EmployeesNo, start.AddDays(-1).ToString("yyyy/MM/dd 23:59:59"), end.AddDays(1).ToString("yyyy/MM/dd 00:00:00"), type);
            dt.Columns.Add("chartno");
            dt.Columns.Add("badno");
            dt.Columns.Add("ptname");
            dt.Columns.Add("Creat_User");
            if (dt.Rows.Count > 0)
            {
                string feeno = dt.Rows[0]["FEENO"].ToString();
                byte[] ptinfoByteCode = webService.GetPatientInfo(feeno);
                string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                foreach (DataRow r in dt.Rows)
                {
                    if (feeno != r["FEENO"].ToString())
                    {
                        feeno = r["FEENO"].ToString();
                        ptinfoByteCode = webService.GetPatientInfo(feeno);
                        if (ptinfoByteCode != null)
                        {
                            ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                            pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                        }
                    }
                    r["chartno"] = pi.ChartNo;
                    r["badno"] = pi.BedNo;
                    r["ptname"] = pi.PatientName;
                }
                string Creat_User = dt.Rows[0]["MODIFYUSER"].ToString();
                byte[] listByteCode = webService.UserName(Creat_User);
                string listJsonArray = CompressTool.DecompressString(listByteCode);
                UserInfo Creat_User_name = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                foreach (DataRow r in dt.Rows)
                {
                    if (Creat_User != r["MODIFYUSER"].ToString())
                    {
                        Creat_User = r["MODIFYUSER"].ToString();
                        listByteCode = webService.GetPatientInfo(Creat_User);
                        if (listByteCode != null)
                        {
                            listJsonArray = CompressTool.DecompressString(listByteCode);
                            Creat_User_name = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                        }
                    }
                    r["Creat_User"] = Creat_User_name.EmployeesName;
                }
            }
            ViewBag.dt = dt;
            ViewBag.type = type;
        }

        #endregion

        private string set_admission_content(string tableid, string natype, string feeno)
        {
            string content = "";
            DataTable dt = ass_m.sel_assessment_contnet(tableid);
            if (natype == "A")//成人
            {
                #region 設定內容

                #region 基本資料
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-0\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"基本資料\" />";
                content += "<title>基本資料</title><text><list>";

                if (sel_data_(dt, "param_tube_date") != "" && sel_data_(dt, "param_tube_time") != "")
                    content += "<item>入院日期：" + sel_data_(dt, "param_tube_date") + " " + sel_data_(dt, "param_tube_time") + "</item>";
                if (sel_data_(dt, "param_ipd_reason") != "")
                    content += "<item>入院原因：" + sel_data_(dt, "param_ipd_reason").Replace("|", ",") + "</item>";
                if (sel_data_(dt, "param_ipd_style") != "")
                {
                    content += "<item>入院方式：" + sel_data_(dt, "param_ipd_style").Replace("其他", "");
                    if (sel_data_(dt, "param_ipd_style_other") != "")
                        content += sel_data_(dt, "param_ipd_style_other").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_education") != "")
                    content += "<item>教育：" + sel_data_(dt, "param_education") + "</item>";
                if (sel_data_(dt, "param_job") != "")
                {
                    content += "<item>職業：" + sel_data_(dt, "param_job").Replace("其他", "");
                    if (sel_data_(dt, "param_job_other") != "")
                        content += sel_data_(dt, "param_job_other").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_economy") != "")
                    content += "<item>經濟狀況：" + sel_data_(dt, "param_economy") + "</item>";
                if (sel_data_(dt, "param_payment") != "")
                    content += "<item>身分別：" + sel_data_(dt, "param_payment") + "</item>";
                if (sel_data_(dt, "param_religion") != "")
                {
                    content += "<item>宗教：" + sel_data_(dt, "param_religion").Replace("其他", "");
                    if (sel_data_(dt, "param_religion_other") != "")
                        content += sel_data_(dt, "param_religion_other").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_lang") != "")
                {
                    content += "<item>語言：" + sel_data_(dt, "param_lang").Replace("其他", "");
                    if (sel_data_(dt, "param_lang_other") != "")
                        content += sel_data_(dt, "param_lang_other").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_needtrans") != "")
                    content += "<item>需翻譯：" + sel_data_(dt, "param_needtrans") + "</item>";
                if (sel_data_(dt, "param_marrage") != "")
                    content += "<item>婚姻狀況：" + sel_data_(dt, "param_marrage") + "</item>";
                if (sel_data_(dt, "param_care") != "")
                {
                    content += "<item>主要照顧者：" + sel_data_(dt, "param_care").Replace("其他", "");
                    if (sel_data_(dt, "param_care_other") != "")
                        content += sel_data_(dt, "param_care_other").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_psychological") != "")
                {
                    content += "<item>心理狀況：" + sel_data_(dt, "param_psychological").Replace("其他", "");
                    if (sel_data_(dt, "param_psychological_other") != "")
                        content += sel_data_(dt, "param_psychological_other").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_child") != "")
                {
                    content += "<item>子女：" + sel_data_(dt, "param_child");
                    if (sel_data_(dt, "param_child_f") != "")
                        content += "，男" + sel_data_(dt, "param_child_f") + "人";
                    if (sel_data_(dt, "param_child_m") != "")
                        content += "，女" + sel_data_(dt, "param_child_m") + "人";
                    content += "</item>";
                }
                if (sel_data_(dt, "param_living") != "")
                {
                    content += "<item>居住方式：" + sel_data_(dt, "param_living");
                    if (sel_data_(dt, "param_living_other") != "")
                        content += " : " + sel_data_(dt, "param_living_other").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_Volunteer_Help") != "")
                {
                    content += "<item>需社工幫助：" + sel_data_(dt, "param_Volunteer_Help");
                    if (sel_data_(dt, "param_Volunteer_Help_Dtl") != "")
                        content += "，" + sel_data_(dt, "param_Volunteer_Help_Dtl").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_cigarette") != "")
                {
                    if (sel_data_(dt, "param_cigarette") == "1")
                        content += "<item>抽菸：無";
                    if (sel_data_(dt, "param_cigarette") == "2")
                        content += "<item>抽菸：有";
                    if (sel_data_(dt, "param_cigarette") == "3")
                        content += "<item>抽菸：戒菸";
                    if (sel_data_(dt, "param_cigarette_yes_amount") != "")
                        content += "，每日" + sel_data_(dt, "param_cigarette_yes_amount") + "支";
                    if (sel_data_(dt, "param_cigarette_yes_year") != "")
                        content += "，已抽" + sel_data_(dt, "param_cigarette_yes_year") + "年";
                    if (sel_data_(dt, "param_cigarette_agree_stop") != "")
                        content += "，有無戒菸意願：" + sel_data_(dt, "param_cigarette_agree_stop");
                    if (sel_data_(dt, "param_cigarette_stop_year") != "")
                        content += "，" + sel_data_(dt, "param_cigarette_stop_year") + "年";
                    content += "</item>";
                }
                if (sel_data_(dt, "param_drink") != "")
                {
                    content += "<item>喝酒：" + sel_data_(dt, "param_drink");
                    if (sel_data_(dt, "param_drink_day") != "")
                        content += "，每日劑量" + sel_data_(dt, "param_drink_day") + "瓶";
                    if (sel_data_(dt, "param_drink_unit") != "")
                        content += " " + sel_data_(dt, "param_drink_unit") + "mL";
                    content += "</item>";
                }
                content += "</list></text></section></component>";

                #endregion

                #region 過去病史
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-1\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"過去病史\" />";
                content += "<title>過去病史</title><text><list>";

                if (sel_data_(dt, "param_hasipd") != "")
                {
                    content += "<item>住院次數：" + sel_data_(dt, "param_hasipd");
                    if (sel_data_(dt, "param_ipd_count") != "")
                        content += "，" + sel_data_(dt, "param_ipd_count");
                    if (sel_data_(dt, "param_ipd_lasttime") != "")
                        content += "，最近一次時間" + sel_data_(dt, "param_ipd_lasttime");
                    if (sel_data_(dt, "param_ipd_diag") != "")
                        content += "，診斷" + sel_data_(dt, "param_ipd_diag").Replace("|", ",");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_im_history") != "")
                {
                    content += "<item>內科病史：" + sel_data_(dt, "param_im_history");
                    if (sel_data_(dt, "param_im_history_item1") != "")
                        content += "，高血壓" + sel_data_(dt, "param_im_history_item1") + "年";
                    if (sel_data_(dt, "param_im_history_item2") != "")
                        content += "，心臟病" + sel_data_(dt, "param_im_history_item2") + "年";
                    if (sel_data_(dt, "param_im_history_item3") != "")
                        content += "，糖尿病" + sel_data_(dt, "param_im_history_item3") + "年";
                    if (sel_data_(dt, "param_im_history_item4") != "")
                        content += "，氣喘" + sel_data_(dt, "param_im_history_item4") + "年";
                    if (sel_data_(dt, "param_im_history_item_other") != "")
                        content += "，其他疾病：" + sel_data_(dt, "param_im_history_item_other").Replace("其他", "");
                    if (sel_data_(dt, "param_im_history_item_other_txt") != "" && sel_data_(dt, "param_im_history_item_other").IndexOf("其他")>-1)
                        content += sel_data_(dt, "param_im_history_item_other_txt").Replace("|", ",");
                    if (sel_data_(dt, "param_im_history_status") != "")
                        content += "，疾病發生時間，處理情形及目前狀況：" + sel_data_(dt, "param_im_history_status").Replace("|", ",");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_su_history") != "")
                {
                    content += "<item>外科病史：" + sel_data_(dt, "param_su_history");
                    if (sel_data_(dt, "param_su_history_trauma_txt") != "")
                        content += "，外傷：" + sel_data_(dt, "param_su_history_trauma_txt").Replace("|", ",");
                    if (sel_data_(dt, "param_su_history_surgery_txt") != "")
                        content += "，手術：" + sel_data_(dt, "param_su_history_surgery_txt").Replace("|", ",");
                    if (sel_data_(dt, "param_su_history_other_txt") != "")
                        content += "，外傷/手術/外科疾病：" + sel_data_(dt, "param_su_history_other_txt").Replace("|", ",");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_other_history") != "")
                {
                    content += "<item>其他病史：" + sel_data_(dt, "param_other_history");
                    if (sel_data_(dt, "param_other_history_desc") != "")
                        content += "，" + sel_data_(dt, "param_other_history_desc").Replace("|", ",");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_med") != "")
                {
                    content += "<item>目前服用藥物：" + sel_data_(dt, "param_med");
                    if (sel_data_(dt, "param_med") == "有")
                    {
                        //取得目前用藥
                        List<DrugOrder> Drug_list = new List<DrugOrder>();
                        byte[] labfoByteCode = webService.GetOpdMed(feeno);
                        if (labfoByteCode != null)
                        {
                            string labJosnArr = CompressTool.DecompressString(labfoByteCode);
                            Drug_list = JsonConvert.DeserializeObject<List<DrugOrder>>(labJosnArr);
                            if (Drug_list.Count > 0)
                            {
                                content += "<list>";
                                for (int i = 0; i < Drug_list.Count; i++)
                                {
                                    content += "<item>門診用藥-藥物名稱：" + trans_special_code(Drug_list[i].DrugName);
                                    content += "，頻次：" + trans_special_code(Drug_list[i].Feq);
                                    content += "，劑量：" + trans_special_code(Drug_list[i].Dose.ToString());
                                    content += "，途徑：" + trans_special_code(Drug_list[i].Route) + "</item>";
                                }
                                content += "</list>";
                            }
                        }
                    }
                    if (sel_data_(dt, "param_med_name") != "")
                    {
                        content += "<list>";
                        string[] DrugName = sel_data_(dt, "param_med_name").Split(',');
                        string[] Feq = sel_data_(dt, "param_med_frequency").Split(',');
                        string[] Dose = sel_data_(dt, "param_med_amount").Split(',');
                        string[] Route = sel_data_(dt, "param_med_way").Split(',');
                        for (int i = 0; i < DrugName.Length; i++)
                        {
                            content += "<item>藥物名稱：" + DrugName[i].Replace("|", ",");
                            content += "，頻次：" + Feq[i].Replace("|", ",");
                            content += "，劑量：" + Dose[i].Replace("|", ",");
                            content += "，途徑：" + Route[i].Replace("|", ",") + "</item>";
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_allergy_med") != "")
                {
                    content += "<item>過敏史藥物：" + sel_data_(dt, "param_allergy_med");
                    if (sel_data_(dt, "param_allergy_med_other") != "")
                    {
                        string[] type = sel_data_(dt, "param_allergy_med_other").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "不詳")
                                content += "<item>不詳</item>";
                            else if (type[i] == "pyrin")
                            {
                                content += "<item>匹林系藥物(pyrin)";
                                if (sel_data_(dt, "param_allergy_med_other_2_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_med_other_2_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "aspirin")
                                content += "<item>水楊酸鹽類(包括aspirin)</item>";
                            else if (type[i] == "NSAID")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_med_other_4_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_med_other_4_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "磺氨類")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_med_other_6_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_med_other_6_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "盤尼西林類")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_med_other_7_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_med_other_7_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "抗生素類")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_med_other_8_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_med_other_8_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "麻醉藥")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_med_other_9_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_med_other_9_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "其他")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_med_other_10_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_med_other_10_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else
                                content += "<item>" + type[i] + "</item>";
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_allergy_food") != "")
                {
                    content += "<item>過敏史食物：" + sel_data_(dt, "param_allergy_food");
                    if (sel_data_(dt, "param_allergy_food_other") != "")
                    {
                        string[] type = sel_data_(dt, "param_allergy_food_other").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "海鮮類")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_food_other_2_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_food_other_2_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "水果")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_food_other_4_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_food_other_4_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "其他")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_food_other_6_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_food_other_6_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else
                                content += "<item>" + type[i] + "</item>";
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_allergy_other") != "")
                {
                    content += "<item>過敏史其他：" + sel_data_(dt, "param_allergy_other");
                    if (sel_data_(dt, "param_allergy_other_other") != "")
                    {
                        string[] type = sel_data_(dt, "param_allergy_other_other").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "不詳")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_other_other_1_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_other_other_1_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "輸血")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_other_other_2_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_other_other_2_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "油漆")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_other_other_3_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_other_other_3_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "昆蟲")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_other_other_4_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_other_other_4_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i].IndexOf("麈") > -1)
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_other_other_5_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_other_other_5_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "其他")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_other_other_6_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_other_other_6_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else
                                content += "<item>" + type[i] + "</item>";
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }
                content += "</list></text></section></component>";
                #endregion

                #region 家族病史
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-2\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"家族病史\" />";
                content += "<title>家族病史</title><text><list>";

                if (sel_data_(dt, "param_family_history") != "")
                    content += "<item>家族病史：" + sel_data_(dt, "param_family_history") + "</item>";
                if (sel_data_(dt, "param_bp") != "")
                    content += "<item>高血壓：" + sel_data_(dt, "param_bp") + "</item>";
                if (sel_data_(dt, "param_kind") != "")
                    content += "<item>腎臟病：" + sel_data_(dt, "param_kind") + "</item>";
                if (sel_data_(dt, "param_asthma") != "")
                    content += "<item>氣喘：" + sel_data_(dt, "param_asthma") + "</item>";
                if (sel_data_(dt, "param_epilepsy") != "")
                    content += "<item>癲癇：" + sel_data_(dt, "param_epilepsy") + "</item>";
                if (sel_data_(dt, "param_HeartDisease") != "")
                    content += "<item>心臟病：" + sel_data_(dt, "param_HeartDisease") + "</item>";
                if (sel_data_(dt, "param_PepticUlcer") != "")
                    content += "<item>消化性潰瘍：" + sel_data_(dt, "param_PepticUlcer") + "</item>";
                if (sel_data_(dt, "param_tuberculosis") != "")
                    content += "<item>肺結核：" + sel_data_(dt, "param_tuberculosis") + "</item>";
                if (sel_data_(dt, "param_MentalIllness") != "")
                    content += "<item>精神病：" + sel_data_(dt, "param_MentalIllness") + "</item>";
                if (sel_data_(dt, "param_Diabetes") != "")
                    content += "<item>糖尿病：" + sel_data_(dt, "param_Diabetes") + "</item>";
                if (sel_data_(dt, "param_Cancer") != "")
                    content += "<item>癌症：" + sel_data_(dt, "param_Cancer") + "</item>";
                if (sel_data_(dt, "param_LiverDisease") != "")
                    content += "<item>肝臟疾病：" + sel_data_(dt, "param_LiverDisease") + "</item>";
                if (sel_data_(dt, "param_OtherDiseaseDesc") != "" || sel_data_(dt, "param_OtherDisease") != "")
                    content += "<item>其他疾病名稱：" + sel_data_(dt, "param_OtherDiseaseDesc").Replace("|", ",") + "，" + sel_data_(dt, "param_OtherDisease") + "</item>";

                content += "</list></text></section></component>";
                #endregion

                #region 一般外觀
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-3\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"一般外觀\" />";
                content += "<title>一般外觀</title><text><list>";

                if (sel_data_(dt, "param_LeftEye") != "")
                {
                    content += "<item>左眼視力：" + sel_data_(dt, "param_LeftEye").Replace("其他", "");
                    if (sel_data_(dt, "param_LeftEyeOther") != "")
                        content += sel_data_(dt, "param_LeftEyeOther").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_LeftEyeRedress") != "")
                {
                    content += "<item>左眼矯正：" + sel_data_(dt, "param_LeftEyeRedress").Replace("其他", "");
                    if (sel_data_(dt, "param_LeftEyeRedressDesc") != "")
                        content += sel_data_(dt, "param_LeftEyeRedressDesc").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_RightEye") != "")
                {
                    content += "<item>右眼視力：" + sel_data_(dt, "param_RightEye").Replace("其他", "");
                    if (sel_data_(dt, "param_RightEyeOther") != "")
                        content += sel_data_(dt, "param_RightEyeOther").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_RightEyeRedress") != "")
                {
                    content += "<item>右眼矯正：" + sel_data_(dt, "param_RightEyeRedress").Replace("其他", "");
                    if (sel_data_(dt, "param_RightEyeRedressDesc") != "")
                        content += sel_data_(dt, "param_RightEyeRedressDesc").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_LeftEra") != "")
                {
                    content += "<item>左耳聽力：" + sel_data_(dt, "param_LeftEra").Replace("其他", "");
                    if (sel_data_(dt, "param_LeftEraDesc") != "")
                        content += sel_data_(dt, "param_LeftEraDesc").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_RightEra") != "")
                {
                    content += "<item>右耳聽力：" + sel_data_(dt, "param_RightEra").Replace("其他", "");
                    if (sel_data_(dt, "param_RightEraDesc") != "")
                        content += " : " + sel_data_(dt, "param_RightEraDesc").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_Mouth") != "")
                {
                    content += "<item>口腔，黏膜：" + sel_data_(dt, "param_Mouth");
                    if (sel_data_(dt, "param_Mouth_Abnormal") != "")
                        content += "，" + sel_data_(dt, "param_Mouth_Abnormal") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_Tooth") != "")
                {
                    content += "<item>假牙：" + sel_data_(dt, "param_Tooth");
                    if (sel_data_(dt, "param_ToothUpDesc") != "")
                        content += "，上：" + sel_data_(dt, "param_ToothUpDesc");
                    if (sel_data_(dt, "param_ToothDownDesc") != "")
                        content += "，下：" + sel_data_(dt, "param_ToothDownDesc");
                    content += "</item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #region 皮膚狀況
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-4\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"皮膚狀況\" />";
                content += "<title>皮膚狀況</title><text><list>";

                if (sel_data_(dt, "param_Skin_Temp") != "")
                {
                    content += "<item>皮膚溫度：" + sel_data_(dt, "param_Skin_Temp");
                    if (sel_data_(dt, "param_Skin_Temp_Abnormal") != "")
                    {
                        string[] type = sel_data_(dt, "param_Skin_Temp_Abnormal").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "發熱")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Temp_Abnormal_Hot_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Temp_Abnormal_Hot_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Temp_Abnormal_Hot_position_other") != "")
                                        content += " : " + sel_data_(dt, "param_Skin_Temp_Abnormal_Hot_position_other").Replace("|", ",");
                                }
                                content += "</item>";
                            }
                            else if (type[i] == "冰冷")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Temp_Abnormal_Cold_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Temp_Abnormal_Cold_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Temp_Abnormal_Cold_position_other") != "")
                                        content += " : " + sel_data_(dt, "param_Skin_Temp_Abnormal_Cold_position_other").Replace("|", ",");
                                }
                                content += "</item>";
                            }
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Skin_Exterior") != "")
                {
                    content += "<item>皮膚外觀：" + sel_data_(dt, "param_Skin_Exterior");
                    if (sel_data_(dt, "param_Skin_Exterior_Abnormal") != "")
                    {
                        string[] type = sel_data_(dt, "param_Skin_Exterior_Abnormal").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "蒼白")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Pale_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Exterior_Abnormal_Pale_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Pale_position_other") != "")
                                        content += "" + sel_data_(dt, "param_Skin_Exterior_Abnormal_Pale_position_other").Replace("|", ",");
                                }
                                content += "</item>";
                            }
                            else if (type[i] == "發紺")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Cyanotic_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Exterior_Abnormal_Cyanotic_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Cyanotic_position_other") != "")
                                        content += "" + sel_data_(dt, "param_Skin_Exterior_Abnormal_Cyanotic_position_other").Replace("|", ",");
                                }
                                content += "</item>";
                            }
                            else if (type[i] == "紅疹")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Rash_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Exterior_Abnormal_Rash_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Rash_position_other") != "")
                                        content += "" + sel_data_(dt, "param_Skin_Exterior_Abnormal_Rash_position_other").Replace("|", ",");
                                }
                                content += "</item>";
                            }
                            else if (type[i] == "黃疸")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Jaundice_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Exterior_Abnormal_Jaundice_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Jaundice_position_other") != "")
                                        content += "" + sel_data_(dt, "param_Skin_Exterior_Abnormal_Jaundice_position_other").Replace("|", ",");
                                }
                                content += "</item>";
                            }
                            else if (type[i] == "瘀青")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Bruises_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Exterior_Abnormal_Bruises_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Bruises_position_other") != "")
                                        content += "" + sel_data_(dt, "param_Skin_Exterior_Abnormal_Bruises_position_other").Replace("|", ",");
                                }
                                content += "</item>";
                            }
                            else if (type[i] == "脫屑")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Desquamation_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Exterior_Abnormal_Desquamation_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Desquamation_position_other") != "")
                                        content += "" + sel_data_(dt, "param_Skin_Exterior_Abnormal_Desquamation_position_other").Replace("|", ",");
                                }
                                content += "</item>";
                            }
                            else if (type[i] == "潮濕")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Moist_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Exterior_Abnormal_Moist_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Moist_position_other") != "")
                                        content += "" + sel_data_(dt, "param_Skin_Exterior_Abnormal_Moist_position_other").Replace("|", ",");
                                }
                                content += "</item>";
                            }
                            else if (type[i] == "水泡")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Blister_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Exterior_Abnormal_Blister_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Blister_position_other") != "")
                                        content += "" + sel_data_(dt, "param_Skin_Exterior_Abnormal_Blister_position_other").Replace("|", ",");
                                } content += "</item>";
                            }
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Skin_Wound") != "")
                {
                    content += "<item>傷口：" + sel_data_(dt, "param_Skin_Wound");
                    if (sel_data_(dt, "param_Skin_Wound_Abnormal") != "")
                    {
                        string[] type = sel_data_(dt, "param_Skin_Wound_Abnormal").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "壓瘡" || type[i] == "壓傷")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Wound_Abnormal_Pressure_sores_position") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Wound_Abnormal_Pressure_sores_position");
                                content += "</item>";
                            }
                            else if (type[i] == "手術")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Wound_Abnormal_Surgery_position") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Wound_Abnormal_Surgery_position");
                                content += "</item>";
                            }
                            else if (type[i] == "外傷")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Wound_Abnormal_Trauma_position") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Wound_Abnormal_Trauma_position");
                                content += "</item>";
                            }
                            else if (type[i] == "燙傷")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Wound_Abnormal_Scald_position") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Wound_Abnormal_Scald_position");
                                content += "</item>";
                            }
                            else if (type[i] == "蜂窩性組織炎")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Wound_Abnormal_Cellulitis_position") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Wound_Abnormal_Cellulitis_position");
                                content += "</item>";
                            }
                            else if (type[i] == "糖尿病足(DM Foot)")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Wound_Abnormal_DM_Foot_position") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Wound_Abnormal_DM_Foot_position");
                                content += "</item>";
                            }
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #region 心肺系統
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-5\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"心肺系統\" />";
                content += "<title>心肺系統</title><text><list>";

                if (sel_data_(dt, "param_Breathing_Type") != "")
                {
                    content += "<item>呼吸型態：" + sel_data_(dt, "param_Breathing_Type");
                    if (sel_data_(dt, "param_Breathing_Type_Abnormal") != "")
                        content += "，" + sel_data_(dt, "param_Breathing_Type_Abnormal") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_Breathing_LeftVoive") != "")
                {
                    content += "<item>呼吸音-左側：" + sel_data_(dt, "param_Breathing_LeftVoive");
                    if (sel_data_(dt, "param_Breathing_LeftVoive_Abnormal") != "")
                        content += "，" + sel_data_(dt, "param_Breathing_LeftVoive_Abnormal") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_Breathing_RightVoive") != "")
                {
                    content += "<item>呼吸音-右側：" + sel_data_(dt, "param_Breathing_RightVoive");
                    if (sel_data_(dt, "param_Breathing_RightVoive_Abnormal") != "")
                        content += "，" + sel_data_(dt, "param_Breathing_RightVoive_Abnormal") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_Breathing_Treatment") != "")
                {
                    content += "<item>氧氣治療：" + sel_data_(dt, "param_Breathing_Treatment");
                    if (sel_data_(dt, "param_Breathing_Treatment_Abnormal") != "")
                        content += "，" + sel_data_(dt, "param_Breathing_Treatment_Abnormal");
                    if (sel_data_(dt, "param_Breathing_Treatment_Abnormal_Mask_1") != "")
                        content += "，" + sel_data_(dt, "param_Breathing_Treatment_Abnormal_Mask_1").Replace("|", ",") + "L/min";
                    if (sel_data_(dt, "param_Breathing_Treatment_Abnormal_Mask_2") != "")
                        content += "，" + sel_data_(dt, "param_Breathing_Treatment_Abnormal_Mask_2").Replace("|", ",") + "%";
                    if (sel_data_(dt, "param_Breathing_Treatment_Abnormal_Desc") != "")
                        content += " : " + sel_data_(dt, "param_Breathing_Treatment_Abnormal_Desc").Replace("|", ",");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Sputum_Amount") != "")
                {
                    content += "<item>痰液量：" + sel_data_(dt, "param_Sputum_Amount");
                    if (sel_data_(dt, "param_Sputum_Amount_Option") != "")
                        content += " : " + sel_data_(dt, "param_Sputum_Amount_Option");
                    if (sel_data_(dt, "param_Sputum_Amount_Color") != "")
                    {
                        content += "，痰液色：" + sel_data_(dt, "param_Sputum_Amount_Color");
                        if (sel_data_(dt, "param_Sputum_Amount_Color_other") != "")
                            content += " : " + sel_data_(dt, "param_Sputum_Amount_Color_other").Replace("|", ",");
                    }
                    if (sel_data_(dt, "param_Sputum_Amount_Type") != "")
                        content += "，痰液性質：" + sel_data_(dt, "param_Sputum_Amount_Type");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Heart_Rhythm") != "")
                {
                    content += "<item>心跳節律：" + sel_data_(dt, "param_Heart_Rhythm");
                    if (sel_data_(dt, "param_Heart_Rhythm_Name") != "")
                        content += " : " + sel_data_(dt, "param_Heart_Rhythm_Name").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_Heart_Symptoms") != "")
                {
                    content += "<item>病徵：" + sel_data_(dt, "param_Heart_Symptoms");
                    if (sel_data_(dt, "param_Heart_Symptoms_Dtl") != "")
                    {
                        content += "，" + sel_data_(dt, "param_Heart_Symptoms_Dtl").Replace("其他", "");
                        if (sel_data_(dt, "param_Heart_Symptoms_Dtl_Other") != "")
                            content += " : " + sel_data_(dt, "param_Heart_Symptoms_Dtl_Other").Replace("|", ",");
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_LeftFoot_Artery_Strength") != "" || sel_data_(dt, "param_RightFoot_Artery_Strength") != "")
                {
                    content += "<item>足背動脈強度<list>";
                    if (sel_data_(dt, "param_LeftFoot_Artery_Strength") != "")
                        content += "<item>左：" + sel_data_(dt, "param_LeftFoot_Artery_Strength") + "</item>";
                    if (sel_data_(dt, "param_RightFoot_Artery_Strength") != "")
                        content += "<item>右：" + sel_data_(dt, "param_RightFoot_Artery_Strength") + "</item>";
                    content += "</list></item>";
                }
                if (sel_data_(dt, "param_Tip") != "")
                {
                    content += "<item>末梢：" + sel_data_(dt, "param_Tip");
                    if (sel_data_(dt, "param_Tip_Abnormal_LeftTop") != "" || sel_data_(dt, "param_Tip_Abnormal_RightTop") != "" || sel_data_(dt, "param_Tip_Abnormal_LeftDown") != "" || sel_data_(dt, "param_Tip_Abnormal_RightDown") != "")
                    {
                        content += "<list>";
                        if (sel_data_(dt, "param_Tip_Abnormal_LeftTop") != "")
                            content += "<item>左上肢：" + sel_data_(dt, "param_Tip_Abnormal_LeftTop") + "</item>";
                        if (sel_data_(dt, "param_Tip_Abnormal_RightTop") != "")
                            content += "<item>右上肢：" + sel_data_(dt, "param_Tip_Abnormal_RightTop") + "</item>";
                        if (sel_data_(dt, "param_Tip_Abnormal_LeftDown") != "")
                            content += "<item>左下肢：" + sel_data_(dt, "param_Tip_Abnormal_LeftDown") + "</item>";
                        if (sel_data_(dt, "param_Tip_Abnormal_RightDown") != "")
                            content += "<item>右下肢：" + sel_data_(dt, "param_Tip_Abnormal_RightDown") + "</item>";
                        content += "</list>";
                    }
                    content += "</item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #region 泌尿系統
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-6\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"泌尿系統\" />";
                content += "<title>泌尿系統</title><text><list>";

                if (sel_data_(dt, "param_Voiding_Way") != "")
                    content += "<item>排尿方式：" + sel_data_(dt, "param_Voiding_Way") + "</item>";
                if (sel_data_(dt, "param_Voiding_Type") != "")
                {
                    content += "<item>排尿型態：" + sel_data_(dt, "param_Voiding_Type");
                    if (sel_data_(dt, "param_Voiding_Type_Desc") != "")
                        content += "，" + sel_data_(dt, "param_Voiding_Type_Desc") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_Voiding_Characters") != "")
                {
                    content += "<item>排尿性狀：" + sel_data_(dt, "param_Voiding_Characters");
                    if (sel_data_(dt, "param_Voiding_Characters_Desc") != "")
                    {
                        content += "，" + sel_data_(dt, "param_Voiding_Characters_Desc").Replace("其他", "");
                        if (sel_data_(dt, "param_Voiding_Characters_Desc_other") != "")
                            content += " : " + sel_data_(dt, "param_Voiding_Characters_Desc_other").Replace("|", ",");
                    }
                    content += "</item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #region 腸胃及營養評估
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-7\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"腸胃及營養評估\" />";
                content += "<title>腸胃及營養評估</title><text><list>";

                if (sel_data_(dt, "param_FoodKind") != "")
                {
                    content += "<item>飲食種類：" + sel_data_(dt, "param_FoodKind");
                    if (sel_data_(dt, "param_FoodKind_Tube") != "")
                        content += "，" + sel_data_(dt, "param_FoodKind_Tube");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Appetite") != "")
                    content += "<item>食慾：" + sel_data_(dt, "param_Appetite") + "</item>";
                if (sel_data_(dt, "param_Chew") != "")
                    content += "<item>咀嚼：" + sel_data_(dt, "param_Chew") + "</item>";
                if (sel_data_(dt, "param_Swallowing") != "")
                {
                    content += "<item>吞嚥：" + sel_data_(dt, "param_Swallowing");
                    if (sel_data_(dt, "param_SwallowingStatus") != "")
                        content += "，" + sel_data_(dt, "param_SwallowingStatus");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Eating") != "")
                {
                    content += "<item>進食方式：" + sel_data_(dt, "param_Eating");
                    if (sel_data_(dt, "param_EatingStatus1") != "")
                        content += "，" + sel_data_(dt, "param_EatingStatus1");
                    if (sel_data_(dt, "param_EatingStatus2") != "")
                    {
                        string[] type = sel_data_(dt, "param_EatingStatus2").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "全靜脈營養(TPN)")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_EatingStatus2_1") != "")
                                    content += "：" + sel_data_(dt, "param_EatingStatus2_1").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "周邊靜脈營養(PPN)")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_EatingStatus2_2") != "")
                                    content += "：" + sel_data_(dt, "param_EatingStatus2_2").Replace("|", ",");
                                content += "</item>";
                            }
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Peristalsis") != "")
                {
                    content += "<item>腸蠕動：" + sel_data_(dt, "param_Peristalsis");
                    if (sel_data_(dt, "param_PeristalsisStatus") != "")
                        content += "，" + sel_data_(dt, "param_PeristalsisStatus");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Gastrointestinal") != "")
                {
                    content += "<item>腸胃症狀：" + sel_data_(dt, "param_Gastrointestinal");
                    if (sel_data_(dt, "param_GastrointestinalStatus_1") != "")
                        content += "，嘔吐" + sel_data_(dt, "param_GastrointestinalStatus_1") + "天";
                    if (sel_data_(dt, "param_GastrointestinalStatus_2") != "")
                        content += "，噁心" + sel_data_(dt, "param_GastrointestinalStatus_2") + "天";
                    if (sel_data_(dt, "param_GastrointestinalStatus_3") != "")
                        content += "，腹脹" + sel_data_(dt, "param_GastrointestinalStatus_3") + "天";
                    if (sel_data_(dt, "param_GastrointestinalStatus_4") != "")
                        content += "，腹瀉" + sel_data_(dt, "param_GastrointestinalStatus_4") + "天";
                    if (sel_data_(dt, "param_GastrointestinalStatus_5") != "")
                        content += "，厭食" + sel_data_(dt, "param_GastrointestinalStatus_5") + "天";
                    if (sel_data_(dt, "param_GastrointestinalStatus_6") != "" || sel_data_(dt, "param_GastrointestinalStatus_reason") != "")
                        content += "，其他" + sel_data_(dt, "param_GastrointestinalStatus_reason") + "：" + sel_data_(dt, "param_GastrointestinalStatus_6") + "天";
                    content += "</item>";
                }
                if (sel_data_(dt, "param_StoolStatus") != "")
                {
                    content += "<item>排便：" + sel_data_(dt, "param_StoolStatus");
                    if (sel_data_(dt, "param_StoolCount") != "")
                        content += "，" + sel_data_(dt, "param_StoolCount") + "次/";
                    if (sel_data_(dt, "param_StoolSequence") != "")
                        content += sel_data_(dt, "param_StoolSequence") + "日";
                    if (sel_data_(dt, "param_StoolAbnormal") != "")
                        content += "，" + sel_data_(dt, "param_StoolAbnormal");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_BodyHeight") != "" || sel_data_(dt, "param_BodyWeight") != "" || sel_data_(dt, "param_BWDown") != "" || sel_data_(dt, "param_FoodUnit") != "")
                {
                    content += "<item>營養評估<list>";
                    if (sel_data_(dt, "param_BodyHeight") != "")
                        content += "<item>身高：" + sel_data_(dt, "param_BodyHeight") + "公分</item>";
                    if (sel_data_(dt, "param_BodyWeight") != "")
                        content += "<item>體重：" + sel_data_(dt, "param_BodyWeight") + "公斤</item>";
                    if (sel_data_(dt, "param_BodyHeight") != "" && sel_data_(dt, "param_BodyWeight") != "")
                    {
                        try
                        {
                            float BMI = float.Parse(sel_data_(dt, "param_BodyWeight")) / (float.Parse(sel_data_(dt, "param_BodyHeight")) * float.Parse(sel_data_(dt, "param_BodyHeight")) / 10000);
                            content += "<item>身體質量指數(BMI)：" + Math.Round(BMI, 1, MidpointRounding.AwayFromZero).ToString() + "</item>";
                        }
                        catch (Exception)
                        { }
                    }
                    if (sel_data_(dt, "param_BWDown") != "")
                        content += "<item>半年內體重下降6Kg：" + sel_data_(dt, "param_BWDown") + "</item>";
                    if (sel_data_(dt, "param_FoodUnit") != "")
                        content += "<item>食物攝取量：" + sel_data_(dt, "param_FoodUnit") + "</item>";
                    content += "</list></item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #region 肌肉骨骼系統
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-8\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"肌肉骨骼系統\" />";
                content += "<title>肌肉骨骼系統</title><text><list>";

                if (sel_data_(dt, "param_RULimb") != "" || sel_data_(dt, "param_RDLimb") != "" || sel_data_(dt, "param_LULimb") != "" || sel_data_(dt, "param_LDLimb") != "")
                {
                    content += "<item>肌力<list>";
                    if (sel_data_(dt, "param_RULimb") != "")
                        content += "<item>右上肢：" + sel_data_(dt, "param_RULimb") + "</item>";
                    if (sel_data_(dt, "param_RDLimb") != "")
                        content += "<item>右下肢：" + sel_data_(dt, "param_RDLimb") + "</item>";
                    if (sel_data_(dt, "param_LULimb") != "")
                        content += "<item>左上肢：" + sel_data_(dt, "param_LULimb") + "</item>";
                    if (sel_data_(dt, "param_LDLimb") != "")
                        content += "<item>左下肢：" + sel_data_(dt, "param_LDLimb") + "</item>";
                    content += "</list></item>";
                }
                if (sel_data_(dt, "param_ActiveStatus") != "")
                {
                    content += "<item>活動情形：" + sel_data_(dt, "param_ActiveStatus");
                    if (sel_data_(dt, "param_UnActiveDesc") != "")
                        content += "，" + sel_data_(dt, "param_UnActiveDesc");
                    if (sel_data_(dt, "param_ActiveDesc") != "")
                        content += "，" + sel_data_(dt, "param_ActiveDesc");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Eat") != "" || sel_data_(dt, "param_Dressing") != "" || sel_data_(dt, "param_Bathing") != "" || sel_data_(dt, "param_Toilet") != "" || sel_data_(dt, "param_Sport") != "")
                {
                    content += "<item>自我照護能力 (ADL)<list>";
                    if (sel_data_(dt, "param_Eat") != "")
                        content += "<item>進食：" + sel_data_(dt, "param_Eat") + "</item>";
                    if (sel_data_(dt, "param_Dressing") != "")
                        content += "<item>穿衣：" + sel_data_(dt, "param_Dressing") + "</item>";
                    if (sel_data_(dt, "param_Bathing") != "")
                        content += "<item>沐浴：" + sel_data_(dt, "param_Bathing") + "</item>";
                    if (sel_data_(dt, "param_Toilet") != "")
                        content += "<item>如廁：" + sel_data_(dt, "param_Toilet") + "</item>";
                    if (sel_data_(dt, "param_Sport") != "")
                        content += "<item>一般運動：" + sel_data_(dt, "param_Sport") + "</item>";
                    content += "</list></item>";
                }
                if (sel_data_(dt, "param_MotionRange") != "")
                {
                    content += "<item>關節活動度：" + sel_data_(dt, "param_MotionRange");
                    if (sel_data_(dt, "param_MotionAbnormalDesc") != "")
                    {
                        string[] type = sel_data_(dt, "param_MotionAbnormalDesc").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "肢體攣縮")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_MotionAbnormalDesc_1_name") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_MotionAbnormalDesc_1_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "關節腫脹")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_MotionAbnormalDesc_2_name") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_MotionAbnormalDesc_2_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "關節變形")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_MotionAbnormalDesc_3_name") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_MotionAbnormalDesc_3_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "晨間僵硬")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_MotionAbnormalDesc_4_name") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_MotionAbnormalDesc_4_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "關節疼痛")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_MotionAbnormalDesc_5_name") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_MotionAbnormalDesc_5_name").Replace("|", ",");
                                content += "</item>";
                            }
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #region 神經系統
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-9\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"神經系統\" />";
                content += "<title>神經系統</title><text><list>";

                if (sel_data_(dt, "param_EyesReflection") != "" || sel_data_(dt, "param_LanguageReflection") != "" || sel_data_(dt, "param_SportReflection") != "")
                {
                    content += "<item>昏迷指標(GCS)<list>";
                    if (sel_data_(dt, "param_EyesReflection") != "")
                        content += "<item>睜眼反射(E)：" + sel_data_(dt, "param_EyesReflection") + "</item>";
                    if (sel_data_(dt, "param_LanguageReflection") != "")
                        content += "<item>語言反射(V)：" + sel_data_(dt, "param_LanguageReflection") + "</item>";
                    if (sel_data_(dt, "param_SportReflection") != "")
                        content += "<item>運動反射(M)：" + sel_data_(dt, "param_SportReflection") + "</item>";
                    content += "</list></item>";
                }
                if (sel_data_(dt, "param_Gait") != "")
                    content += "<item>步態：" + sel_data_(dt, "param_Gait") + "</item>";
                if (sel_data_(dt, "param_Talk") != "")
                {
                    content += "<item>語言表達：" + sel_data_(dt, "param_Talk");
                    if (sel_data_(dt, "param_UnTalkDesc") != "")
                    {
                        content += "，" + sel_data_(dt, "param_UnTalkDesc").Replace("其他", "");
                        if (sel_data_(dt, "param_UnTalkDesc_other") != "")
                            content += sel_data_(dt, "param_UnTalkDesc_other").Replace("|", ",");
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Cognitive") != "")
                {
                    content += "<item>認知感受：" + sel_data_(dt, "param_Cognitive");
                    if (sel_data_(dt, "param_CognitiveStatus") != "" || sel_data_(dt, "param_CognitivePain") != "" || sel_data_(dt, "param_CognitiveNoFeeling") != "" || sel_data_(dt, "param_CognitiveHemp") != "" || sel_data_(dt, "param_CognitivePumpedStorage") != "")
                    {
                        content += "<list>";
                        if (sel_data_(dt, "param_CognitiveStatus") != "")
                        {
                            string[] type = sel_data_(dt, "param_CognitiveStatus").Split(',');
                            for (int i = 0; i < type.Length; i++)
                                content += "<item>" + type[i] + "</item>";
                        }
                        if (sel_data_(dt, "param_CognitivePain") != "")
                        {
                            content += "<item>疼痛，部位：" + sel_data_(dt, "param_CognitivePain").Replace("其他", "");
                            if (sel_data_(dt, "param_CognitivePainOther") != "")
                                content += sel_data_(dt, "param_CognitivePainOther").Replace("|", ",");
                            content += "</item>";
                        }
                        if (sel_data_(dt, "param_CognitiveNoFeeling") != "")
                        {
                            content += "<item>無知覺，部位：" + sel_data_(dt, "param_CognitiveNoFeeling").Replace("其他", "");
                            if (sel_data_(dt, "param_CognitiveNoFeelingOther") != "")
                                content += sel_data_(dt, "param_CognitiveNoFeelingOther").Replace("|", ",");
                            content += "</item>";
                        }
                        if (sel_data_(dt, "param_CognitiveHemp") != "")
                        {
                            content += "<item>麻，部位：" + sel_data_(dt, "param_CognitiveHemp").Replace("其他", "");
                            if (sel_data_(dt, "param_CognitiveHempOther") != "")
                                content += sel_data_(dt, "param_CognitiveHempOther").Replace("|", ",");
                            content += "</item>";
                        }
                        if (sel_data_(dt, "param_CognitivePumpedStorage") != "")
                        {
                            content += "<item>抽搐，部位：" + sel_data_(dt, "param_CognitivePumpedStorage").Replace("其他", "");
                            if (sel_data_(dt, "param_CognitivePumpedStorageOther") != "")
                                content += sel_data_(dt, "param_CognitivePumpedStorageOther").Replace("|", ",");
                            content += "</item>";
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #region 產科史
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-10\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"產科史\" />";
                content += "<title>產科史</title><text><list>";

                if (sel_data_(dt, "param_Gynecology") != "")
                {
                    content += "<item>產科史：" + sel_data_(dt, "param_Gynecology") + "</item>";
                    if (sel_data_(dt, "param_Gynecology") == "女")
                    {
                        if (sel_data_(dt, "param_MC") != "")
                        {
                            content += "<item>月經：" + sel_data_(dt, "param_MC");
                            if (sel_data_(dt, "param_MC") == "已停經" && sel_data_(dt, "param_MCEnd") != "")
                                content += "，停經：" + sel_data_(dt, "param_MCEnd") + "歲";
                            if (sel_data_(dt, "param_MC") == "未停經")
                            {
                                content += "<list>";
                                if (sel_data_(dt, "param_MCStart") != "")
                                    content += "<item>初經年齡：" + sel_data_(dt, "param_MCStart") + "歲</item>";
                                if (sel_data_(dt, "param_Last_MC") != "")
                                    content += "<item>最後月經日：" + sel_data_(dt, "param_Last_MC") + "</item>";
                                if (sel_data_(dt, "param_MCCycle_rule") != "")
                                {
                                    content += "<item>月經週期：" + sel_data_(dt, "param_MCCycle_rule");
                                    if (sel_data_(dt, "param_MCCycle_rule_day") != "")
                                        content += "，" + sel_data_(dt, "param_MCCycle_rule_day") + "天";
                                    content += "</item>";
                                }
                                if (sel_data_(dt, "param_MCDay") != "")
                                    content += "<item>月經天數：" + sel_data_(dt, "param_MCDay") + "天</item>";
                                if (sel_data_(dt, "param_MCAmount") != "")
                                    content += "<item>月經量：" + sel_data_(dt, "param_MCAmount") + "</item>";
                                if (sel_data_(dt, "param_FBAbnormalDtl") != "")
                                {
                                    content += "<item>月經期間：" + sel_data_(dt, "param_FBAbnormalDtl").Replace("其他", "");
                                    if (sel_data_(dt, "param_FBAbnormalOther") != "")
                                        content += sel_data_(dt, "param_FBAbnormalOther").Replace("|", ",");
                                    content += "</item>";
                                }
                                content += "</list>";
                            }
                            content += "</item>";
                        }
                        if (sel_data_(dt, "param_SelfCheck_Breast") != "")
                            content += "<item>乳房自我檢查：" + sel_data_(dt, "param_SelfCheck_Breast") + "</item>";
                        if (sel_data_(dt, "param_SelfCheck_Vagina") != "")
                        {
                            content += "<item>陰道抹片檢查：" + sel_data_(dt, "param_SelfCheck_Vagina");
                            if (sel_data_(dt, "param_SelfCheck_Vagina_Date") != "")
                                content += "，最後一次檢查日期：" + sel_data_(dt, "param_SelfCheck_Vagina_Date");
                            content += "</item>";
                        }
                        if (sel_data_(dt, "param_BornHistory") != "")
                        {
                            content += "<item>生產史：" + sel_data_(dt, "param_BornHistory");
                            if (sel_data_(dt, "param_BornHistoryNL") != "" || sel_data_(dt, "param_BornHistoryND") != "" || sel_data_(dt, "param_BornHistoryHL") != "" || sel_data_(dt, "param_BornHistoryHD") != "")
                            {
                                content += "<list>";
                                if (sel_data_(dt, "param_BornHistoryNL") != "")
                                    content += "<item>自然產，活產數：" + sel_data_(dt, "param_BornHistoryNL") + "人</item>";
                                if (sel_data_(dt, "param_BornHistoryND") != "")
                                    content += "<item>自然產，死產數(懷孕20週以上)：" + sel_data_(dt, "param_BornHistoryND") + "人</item>";
                                if (sel_data_(dt, "param_BornHistoryHL") != "")
                                    content += "<item>剖腹產，活產數：" + sel_data_(dt, "param_BornHistoryHL") + "人</item>";
                                if (sel_data_(dt, "param_BornHistoryHD") != "")
                                    content += "<item>剖腹產，死產數(懷孕20週以上)：" + sel_data_(dt, "param_BornHistoryHD") + "人</item>";
                                content += "</list>";
                            }
                            content += "</item>";
                        }
                        if (sel_data_(dt, "param_AbortionHistory") != "")
                        {
                            content += "<item>流產史(懷孕週數小於20週)：" + sel_data_(dt, "param_AbortionHistory");
                            if (sel_data_(dt, "param_AbortionN") != "" || sel_data_(dt, "param_AbortionH") != "")
                            {
                                content += "<list>";
                                if (sel_data_(dt, "param_AbortionN") != "")
                                    content += "<item>自然流產：" + sel_data_(dt, "param_AbortionN") + "人</item>";
                                if (sel_data_(dt, "param_AbortionH") != "")
                                    content += "<item>人工流產：" + sel_data_(dt, "param_AbortionH") + "次</item>";
                                content += "</list>";
                            }
                            content += "</item>";
                        }
                        if (sel_data_(dt, "param_Contraception") != "")
                        {
                            content += "<item>避孕(懷孕週數小於20週)：" + sel_data_(dt, "param_Contraception");
                            if (sel_data_(dt, "param_ContraceptionDesc") != "")
                            {
                                content += "，" + sel_data_(dt, "param_ContraceptionDesc").Replace("其他", "");
                                if (sel_data_(dt, "param_ContraceptionDesc_other") != "")
                                    content += sel_data_(dt, "param_ContraceptionDesc_other").Replace("|", ",");
                            }
                            content += "</item>";
                        }
                    }
                }

                content += "</list></text></section></component>";
                #endregion

                #region 疼痛評估
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-11\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"疼痛評估\" />";
                content += "<title>疼痛評估</title><text><list>";

                if (sel_data_(dt, "param_Awareness") != "")
                    content += "<item>目前意識狀態：" + sel_data_(dt, "param_Awareness") + "</item>";
                if (sel_data_(dt, "param_PainScale") != "")
                {
                    content += "<item>疼痛：" + sel_data_(dt, "param_PainScale");
                    if (sel_data_(dt, "param_PainSt") != "" || sel_data_(dt, "param_PainSDBrief") != "" || sel_data_(dt, "param_PainSDLang") != "" || sel_data_(dt, "param_PainSDFace") != "" || sel_data_(dt, "param_PainSDBodyLang") != "" || sel_data_(dt, "param_PainSDPiece") != "" || sel_data_(dt, "param_PainSDScole") != "")
                    {
                        content += "<list>";
                        if (sel_data_(dt, "param_PainSt") != "")
                            content += "<item>疼痛強度：" + sel_data_(dt, "param_PainSt").Substring(1, sel_data_(dt, "param_PainSt").IndexOf(")") - 1) + "分</item>";
                        if (sel_data_(dt, "param_PainSDBrief") != "")
                            content += "<item>呼吸：" + sel_data_(dt, "param_PainSDBrief") + "</item>";
                        if (sel_data_(dt, "param_PainSDLang") != "")
                            content += "<item>非言語表達：" + sel_data_(dt, "param_PainSDLang") + "</item>";
                        if (sel_data_(dt, "param_PainSDFace") != "")
                            content += "<item>臉部表情：" + sel_data_(dt, "param_PainSDFace") + "</item>";
                        if (sel_data_(dt, "param_PainSDBodyLang") != "")
                            content += "<item>肢體語言：" + sel_data_(dt, "param_PainSDBodyLang") + "</item>";
                        if (sel_data_(dt, "param_PainSDPiece") != "")
                            content += "<item>安撫：" + sel_data_(dt, "param_PainSDPiece") + "</item>";
                        if (sel_data_(dt, "param_PainSDScole") != "")
                            content += "<item>總分：" + sel_data_(dt, "param_PainSDScole") + "</item>";
                        content += "</list>";
                    }
                    content += "</item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #region 出院準備計畫評估
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-12\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"出院準備計畫評估\" />";
                content += "<title>出院準備計畫評估</title><text><list>";

                if (sel_data_(dt, "param_CPDBook") != "" || sel_data_(dt, "param_CPDAwareness") != "" || sel_data_(dt, "param_CPDActive") != "" || sel_data_(dt, "param_CPDPiss") != "" || sel_data_(dt, "param_CPDStool") != "" || sel_data_(dt, "param_CPDCare") != "" || sel_data_(dt, "param_CPDResouce") != "" || sel_data_(dt, "param_CPDSP2") != "" || sel_data_(dt, "param_CPDScole") != "")
                {
                    content += "<item>出院準備計畫評估<list>";
                    if (sel_data_(dt, "param_CPDBook") != "")
                        content += "<item>殘障手冊：" + sel_data_(dt, "param_CPDBook") + "</item>";
                    if (sel_data_(dt, "param_CPDAwareness") != "")
                        content += "<item>意識：" + sel_data_(dt, "param_CPDAwareness") + "</item>";
                    if (sel_data_(dt, "param_CPDActive") != "")
                        content += "<item>活動：" + sel_data_(dt, "param_CPDActive") + "</item>";
                    if (sel_data_(dt, "param_CPDPiss") != "")
                        content += "<item>解尿：" + sel_data_(dt, "param_CPDPiss") + "</item>";
                    if (sel_data_(dt, "param_CPDStool") != "")
                        content += "<item>大便：" + sel_data_(dt, "param_CPDStool") + "</item>";
                    if (sel_data_(dt, "param_CPDCare") != "")
                        content += "<item>照顧特質：" + sel_data_(dt, "param_CPDCare") + "</item>";
                    if (sel_data_(dt, "param_CPDResouce") != "")
                        content += "<item>照顧資源：" + sel_data_(dt, "param_CPDResouce") + "</item>";
                    if (sel_data_(dt, "param_CPDSP2") != "")
                    {
                        content += "<item>特殊照護：" + sel_data_(dt, "param_CPDSP2");
                        if (sel_data_(dt, "param_CPDSP2Other") != "")
                            content += " : " + sel_data_(dt, "param_CPDSP2Other").Replace("|", ",");
                        content += "</item>";
                    }
                    if (sel_data_(dt, "param_CPDScole") != "")
                        content += "<item>總分：" + sel_data_(dt, "param_CPDScole") + "</item>";
                    content += "</list></item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #region 聯絡資料
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-13\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"聯絡資料\" />";
                content += "<title>聯絡資料</title><text><list>";

                if (sel_data_(dt, "param_EMGContact") != "")
                {
                    content += "<item>聯絡資料<list>";
                    string[] Name = sel_data_(dt, "param_EMGContact").Split(',');
                    string[] Role_other = sel_data_(dt, "param_ContactRole_other").Split(',');
                    string[] Phone_1 = sel_data_(dt, "param_EMGContact_1").Split(',');
                    string[] Phone_2 = sel_data_(dt, "param_EMGContact_2").Split(',');
                    string[] Phone_3 = sel_data_(dt, "param_EMGContact_3").Split(',');
                    for (int i = 0; i < Name.Length; i++)
                    {
                        content += "<item>緊急聯絡人姓名：" + Name[i].Replace("|", ",");
                        if (i == 0)
                            content += "，稱謂：" + sel_data_(dt, "param_ContactRole").Replace("其他", "") + " " + Role_other[i].Replace("|", ",");
                        else
                            content += "，稱謂：" + sel_data_(dt, "param_ContactRole_" + i.ToString()).Replace("其他", "") + " " + Role_other[i].Replace("|", ",");
                        content += "，連絡電話-公司：" + Phone_1[i];
                        content += "，連絡電話-住家：" + Phone_2[i];
                        content += "，連絡電話-手機：" + Phone_3[i];
                        content += "</item>";
                    }
                    content += "</list></item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #endregion
            }
            else if (natype == "C")//兒童
            {
                #region 設定內容

                #region 基本資料
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-0\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"基本資料\" />";
                content += "<title>基本資料</title><text><list>";
                if (sel_data_(dt, "param_tube_date") != "" && sel_data_(dt, "param_tube_time") != "")
                    content += "<item>入院日期：" + sel_data_(dt, "param_tube_date") + " " + sel_data_(dt, "param_tube_time") + "</item>";
                if (sel_data_(dt, "param_ipd_reason") != "")
                    content += "<item>入院原因：" + sel_data_(dt, "param_ipd_reason").Replace("|", ",") + "</item>";
                if (sel_data_(dt, "param_ipd_style") != "")
                {
                    content += "<item>入院方式：" + sel_data_(dt, "param_ipd_style").Replace("其他", "");
                    if (sel_data_(dt, "param_ipd_style_other") != "")
                        content += sel_data_(dt, "param_ipd_style_other").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_body_height") != "" || sel_data_(dt, "param_body_height_percent") != "")
                {
                    content += "<item>身高：";
                    if (sel_data_(dt, "param_body_height") != "")
                        content += sel_data_(dt, "param_body_height") + "cm";
                    if (sel_data_(dt, "param_body_height_percent") != "")
                        content += sel_data_(dt, "param_body_height_percent") + "百分位";
                    content += "</item>";
                }
                if (sel_data_(dt, "param_body_weight") != "" || sel_data_(dt, "param_body_weight_percent") != "")
                {
                    content += "<item>體重：";
                    if (sel_data_(dt, "param_body_weight") != "")
                        content += sel_data_(dt, "param_body_weight") + "kg";
                    if (sel_data_(dt, "param_body_weight_percent") != "")
                        content += sel_data_(dt, "param_body_weight_percent") + "百分位";
                    if (sel_data_(dt, "param_body_height") != "" && sel_data_(dt, "param_body_weight") != "")
                    {
                        try
                        {
                            float BMI = float.Parse(sel_data_(dt, "param_body_weight")) / (float.Parse(sel_data_(dt, "param_body_height")) * float.Parse(sel_data_(dt, "param_body_height")) / 10000);
                            content += "，身體質量指數(BMI)：" + Math.Round(BMI, 1, MidpointRounding.AwayFromZero).ToString();
                        }
                        catch (Exception)
                        {
                          //  string ErrMsg = ex.Message.ToString();
                        }
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_education") != "")
                    content += "<item>教育：" + sel_data_(dt, "param_education") + "</item>";
                if (sel_data_(dt, "param_mentality") != "")
                    content += "<item>心理狀況：" + sel_data_(dt, "param_mentality") + "</item>";
                if (sel_data_(dt, "param_economy") != "")
                    content += "<item>經濟狀況：" + sel_data_(dt, "param_economy") + "</item>";
                if (sel_data_(dt, "param_payment") != "")
                    content += "<item>身分別：" + sel_data_(dt, "param_payment") + "</item>";
                if (sel_data_(dt, "param_living_style") != "")
                {
                    content += "<item>居住方式：" + sel_data_(dt, "param_living_style");
                    if (sel_data_(dt, "param_living_style_other") != "")
                        content += "，" + sel_data_(dt, "param_living_style_other").Replace("|", ",");
                    if (sel_data_(dt, "param_living_style_dtl") != "")
                        content += "，" + sel_data_(dt, "param_living_style_dtl");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_brother_elder") != "" || sel_data_(dt, "param_brother_younger") != "" || sel_data_(dt, "param_sister_elder") != "" || sel_data_(dt, "param_sister_younger") != "")
                {
                    content += "<item>家庭成員：";
                    if (sel_data_(dt, "param_brother_elder") != "")
                        content += "兄" + sel_data_(dt, "param_brother_elder") + "人，";
                    if (sel_data_(dt, "param_brother_younger") != "")
                        content += "弟" + sel_data_(dt, "param_brother_younger") + "人，";
                    if (sel_data_(dt, "param_sister_elder") != "")
                        content += "姊" + sel_data_(dt, "param_sister_elder") + "人，";
                    if (sel_data_(dt, "param_sister_younger") != "")
                        content += "妹" + sel_data_(dt, "param_sister_younger") + "人";
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Volunteer_Help") != "")
                {
                    content += "<item>需社工幫助：" + sel_data_(dt, "param_Volunteer_Help");
                    if (sel_data_(dt, "param_Volunteer_Help_Dtl") != "")
                        content += "，" + sel_data_(dt, "param_Volunteer_Help_Dtl").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_need_placement") != "")
                    content += "<item>社會局安置：" + sel_data_(dt, "param_need_placement") + "</item>";
                content += "</list></text></section></component>";

                #endregion

                #region 照顧者
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-1\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"照顧者\" />";
                content += "<title>照顧者</title><text><list>";

                if (sel_data_(dt, "param_primary_care") != "")
                {
                    content += "<item>主要照顧者：" + sel_data_(dt, "param_primary_care").Replace("其他", "");
                    if (sel_data_(dt, "param_primary_care_other") != "")
                        content += sel_data_(dt, "param_primary_care_other").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_care_education") != "")
                    content += "<item>教育：" + sel_data_(dt, "param_care_education") + "</item>";
                if (sel_data_(dt, "param_job") != "")
                {
                    content += "<item>職業：" + sel_data_(dt, "param_job").Replace("其他", "");
                    if (sel_data_(dt, "param_job_other") != "")
                        content += sel_data_(dt, "param_job_other").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_religion") != "")
                {
                    content += "<item>宗教：" + sel_data_(dt, "param_religion").Replace("其他", "");
                    if (sel_data_(dt, "param_religion_other") != "")
                        content += sel_data_(dt, "param_religion_other").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_lang") != "")
                {
                    content += "<item>語言：" + sel_data_(dt, "param_lang").Replace("其他", "");
                    if (sel_data_(dt, "param_lang_other") != "")
                        content += sel_data_(dt, "param_lang_other").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_needtrans") != "")
                    content += "<item>需翻譯：" + sel_data_(dt, "param_needtrans") + "</item>";
                if (sel_data_(dt, "param_psychological") != "")
                {
                    content += "<item>心理狀況：" + sel_data_(dt, "param_psychological").Replace("其他", "");
                    if (sel_data_(dt, "param_psychological_other") != "")
                        content += sel_data_(dt, "param_psychological_other").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_marrage") != "")
                    content += "<item>婚姻狀況：" + sel_data_(dt, "param_marrage") + "</item>";
                if (sel_data_(dt, "param_cigarette") != "")
                {
                    if (sel_data_(dt, "param_cigarette") == "1")
                        content += "<item>抽菸：無";
                    if (sel_data_(dt, "param_cigarette") == "2")
                        content += "<item>抽菸：有";
                    if (sel_data_(dt, "param_cigarette") == "3")
                        content += "<item>抽菸：戒菸";
                    if (sel_data_(dt, "param_cigarette_yes_amount") != "")
                        content += "，每日" + sel_data_(dt, "param_cigarette_yes_amount") + "支";
                    if (sel_data_(dt, "param_cigarette_yes_year") != "")
                        content += "，已抽" + sel_data_(dt, "param_cigarette_yes_year") + "年";
                    if (sel_data_(dt, "param_cigarette_agree_stop") != "")
                        content += "，有無戒菸意願：" + sel_data_(dt, "param_cigarette_agree_stop");
                    if (sel_data_(dt, "param_cigarette_stop_year") != "")
                        content += "，" + sel_data_(dt, "param_cigarette_stop_year") + "年";
                    content += "</item>";
                }
                content += "</list></text></section></component>";
                #endregion

                #region 過去病史
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-2\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"過去病史\" />";
                content += "<title>過去病史</title><text><list>";
                if (sel_data_(dt, "param_BornHistory") != "")
                {
                    content += "<item>出生史：" + sel_data_(dt, "param_BornHistory");
                    if (sel_data_(dt, "param_BornHistory_full_weight") != "")
                        content += "，出生體重：" + sel_data_(dt, "param_BornHistory_full_weight") + "gm（公克）";
                    if (sel_data_(dt, "param_BornHistory_Preterm_weight") != "")
                        content += "，出生體重：" + sel_data_(dt, "param_BornHistory_Preterm_weight") + "gm（公克）";
                    if (sel_data_(dt, "param_BornHistory_Preterm_week") != "")
                        content += "，週數：" + sel_data_(dt, "param_BornHistory_Preterm_week") + "週";
                    if (sel_data_(dt, "param_BornHistory_Preterm_other") != "")
                        content += "，其他特殊狀況：" + sel_data_(dt, "param_BornHistory_Preterm_other").Replace("|", "") + "週";
                    content += "</item>";
                }
                if (sel_data_(dt, "param_FamilySickHistory") != "")
                {
                    content += "<item>家族病史：" + sel_data_(dt, "param_FamilySickHistory");
                    if (sel_data_(dt, "param_fshDtl") != "")
                    {
                        content += "，" + sel_data_(dt, "param_fshDtl").Replace("其他", "");
                        if (sel_data_(dt, "param_fshDtl_other") != "")
                            content += "，出生體重：" + sel_data_(dt, "param_fshDtl_other").Replace("|", ",");
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_PastHistory") != "")
                {
                    content += "<item>過去病史：" + sel_data_(dt, "param_PastHistory");
                    if (sel_data_(dt, "param_ipdAmt") != "" || sel_data_(dt, "param_ipdReason") != "" || sel_data_(dt, "param_ipdPlace") != "" || sel_data_(dt, "param_opAmt") != "" || sel_data_(dt, "param_opReason") != "" || sel_data_(dt, "param_opPlace") != "")
                    {
                        content += "<list>";
                        if (sel_data_(dt, "param_ipdAmt") != "" || sel_data_(dt, "param_ipdReason") != "" || sel_data_(dt, "param_ipdPlace") != "")
                        {
                            content += "<item>";
                            if (sel_data_(dt, "param_ipdAmt") != "")
                                content += "住院經驗：" + sel_data_(dt, "param_ipdAmt").Replace("|", ",") + "次，";
                            if (sel_data_(dt, "param_ipdReason") != "")
                                content += "原因：" + sel_data_(dt, "param_ipdReason").Replace("|", ",") + "，";
                            if (sel_data_(dt, "param_ipdPlace") != "")
                                content += "住院地點：" + sel_data_(dt, "param_ipdPlace").Replace("|", ",") + "，";
                            content = content.Substring(0, content.Length - 1) + "</item>";
                        }
                        if (sel_data_(dt, "param_opAmt") != "" || sel_data_(dt, "param_opReason") != "" || sel_data_(dt, "param_opPlace") != "")
                        {
                            content += "<item>";
                            if (sel_data_(dt, "param_opAmt") != "")
                                content += "手術經驗：" + sel_data_(dt, "param_opAmt").Replace("|", ",") + "次，";
                            if (sel_data_(dt, "param_opReason") != "")
                                content += "原因：" + sel_data_(dt, "param_opReason").Replace("|", ",") + "，";
                            if (sel_data_(dt, "param_opPlace") != "")
                                content += "住院地點：" + sel_data_(dt, "param_opPlace").Replace("|", ",") + "，";
                            content = content.Substring(0, content.Length - 1) + "</item>";
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_PastHistory_Innate") != "")
                {
                    content += "<item>先天疾病：" + sel_data_(dt, "param_PastHistory_Innate");
                    if (sel_data_(dt, "param_PastHistory_Innate_dtl") != "")
                    {
                        content += "，" + sel_data_(dt, "param_PastHistory_Innate_dtl").Replace("其他", "");
                        if (sel_data_(dt, "param_PastHistory_Innate_dtl_other") != "")
                            content += "，出生體重：" + sel_data_(dt, "param_PastHistory_Innate_dtl_other").Replace("|", ",");
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_PastHistory_Acquired") != "")
                {
                    content += "<item>後天疾病：" + sel_data_(dt, "param_PastHistory_Acquired");
                    if (sel_data_(dt, "param_PastHistory_Acquired_dtl") != "")
                    {
                        content += "，" + sel_data_(dt, "param_PastHistory_Acquired_dtl").Replace("其他", "");
                        if (sel_data_(dt, "param_PastHistory_Acquired_dtl_other") != "")
                            content += "，出生體重：" + sel_data_(dt, "param_PastHistory_Acquired_dtl_other").Replace("|", ",");
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_med") != "")
                {
                    content += "<item>目前服用藥物：" + sel_data_(dt, "param_med");
                    if (sel_data_(dt, "param_med") == "有")
                    {
                        //取得目前用藥
                        List<DrugOrder> Drug_list = new List<DrugOrder>();
                        byte[] labfoByteCode = webService.GetOpdMed(feeno);
                        if (labfoByteCode != null)
                        {
                            string labJosnArr = CompressTool.DecompressString(labfoByteCode);
                            Drug_list = JsonConvert.DeserializeObject<List<DrugOrder>>(labJosnArr);
                            if (Drug_list.Count > 0)
                            {
                                content += "<list>";
                                for (int i = 0; i < Drug_list.Count; i++)
                                {
                                    content += "<item>門診用藥-藥物名稱：" + trans_special_code(Drug_list[i].DrugName);
                                    content += "，頻次：" + trans_special_code(Drug_list[i].Feq);
                                    content += "，劑量：" + trans_special_code(Drug_list[i].Dose.ToString());
                                    content += "，途徑：" + trans_special_code(Drug_list[i].Route) + "</item>";
                                }
                                content += "</list>";
                            }
                        }
                    }
                    if (sel_data_(dt, "param_med_name") != "")
                    {
                        content += "<list>";
                        string[] DrugName = sel_data_(dt, "param_med_name").Split(',');
                        string[] Feq = sel_data_(dt, "param_med_frequency").Split(',');
                        string[] Dose = sel_data_(dt, "param_med_amount").Split(',');
                        string[] Route = sel_data_(dt, "param_med_way").Split(',');
                        for (int i = 0; i < DrugName.Length; i++)
                        {
                            content += "<item>藥物名稱：" + DrugName[i].Replace("|", ",");
                            content += "，頻次：" + Feq[i].Replace("|", ",");
                            content += "，劑量：" + Dose[i].Replace("|", ",");
                            content += "，途徑：" + Route[i].Replace("|", ",") + "</item>";
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_allergy_med") != "")
                {
                    content += "<item>過敏史藥物：" + sel_data_(dt, "param_allergy_med");
                    if (sel_data_(dt, "param_allergy_med_other") != "")
                    {
                        string[] type = sel_data_(dt, "param_allergy_med_other").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "不詳")
                                content += "<item>不詳</item>";
                            else if (type[i] == "pyrin")
                            {
                                content += "<item>匹林系藥物(pyrin)";
                                if (sel_data_(dt, "param_allergy_med_other_2_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_med_other_2_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "aspirin")
                                content += "<item>水楊酸鹽類(包括aspirin)</item>";
                            else if (type[i] == "NSAID")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_med_other_4_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_med_other_4_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "磺氨類")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_med_other_6_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_med_other_6_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "盤尼西林類")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_med_other_7_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_med_other_7_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "抗生素類")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_med_other_8_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_med_other_8_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "麻醉藥")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_med_other_9_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_med_other_9_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "其他")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_med_other_10_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_med_other_10_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else
                                content += "<item>" + type[i] + "</item>";
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_allergy_food") != "")
                {
                    content += "<item>過敏史食物：" + sel_data_(dt, "param_allergy_food");
                    if (sel_data_(dt, "param_allergy_food_other") != "")
                    {
                        string[] type = sel_data_(dt, "param_allergy_food_other").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "海鮮類")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_food_other_2_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_food_other_2_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "水果")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_food_other_4_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_food_other_4_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "其他")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_food_other_6_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_food_other_6_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else
                                content += "<item>" + type[i] + "</item>";
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_allergy_other") != "")
                {
                    content += "<item>過敏史其他：" + sel_data_(dt, "param_allergy_other");
                    if (sel_data_(dt, "param_allergy_other_other") != "")
                    {
                        string[] type = sel_data_(dt, "param_allergy_other_other").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "不詳")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_other_other_1_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_other_other_1_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "輸血")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_other_other_2_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_other_other_2_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "油漆")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_other_other_3_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_other_other_3_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "昆蟲")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_other_other_4_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_other_other_4_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i].IndexOf("麈") > -1)
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_other_other_5_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_other_other_5_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "其他")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_other_other_6_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_other_other_6_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else
                                content += "<item>" + type[i] + "</item>";
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #region 感官知覺
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-3\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"感官知覺\" />";
                content += "<title>感官知覺</title><text><list>";
                if (sel_data_(dt, "param_LeftEye") != "")
                {
                    content += "<item>左眼視力：" + sel_data_(dt, "param_LeftEye").Replace("其他", "");
                    if (sel_data_(dt, "param_LeftEyeOther") != "")
                        content += sel_data_(dt, "param_LeftEyeOther").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_LeftEyeRedress") != "")
                {
                    content += "<item>左眼矯正：" + sel_data_(dt, "param_LeftEyeRedress").Replace("其他", "");
                    if (sel_data_(dt, "param_LeftEyeRedressDesc") != "")
                        content += sel_data_(dt, "param_LeftEyeRedressDesc").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_RightEye") != "")
                {
                    content += "<item>右眼視力：" + sel_data_(dt, "param_RightEye").Replace("其他", "");
                    if (sel_data_(dt, "param_RightEyeOther") != "")
                        content += sel_data_(dt, "param_RightEyeOther").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_RightEyeRedress") != "")
                {
                    content += "<item>右眼矯正：" + sel_data_(dt, "param_RightEyeRedress").Replace("其他", "");
                    if (sel_data_(dt, "param_RightEyeRedressDesc") != "")
                        content += sel_data_(dt, "param_RightEyeRedressDesc").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_LeftEra") != "")
                {
                    content += "<item>左耳聽力：" + sel_data_(dt, "param_LeftEra").Replace("其他", "");
                    if (sel_data_(dt, "param_LeftEraDesc") != "")
                        content += sel_data_(dt, "param_LeftEraDesc").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_RightEra") != "")
                {
                    content += "<item>右耳聽力：" + sel_data_(dt, "param_RightEra").Replace("其他", "");
                    if (sel_data_(dt, "param_RightEraDesc") != "")
                        content += " : " + sel_data_(dt, "param_RightEraDesc").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                content += "</list></text></section></component>";
                #endregion

                #region 皮膚狀況
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-4\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"皮膚狀況\" />";
                content += "<title>皮膚狀況</title><text><list>";

                if (sel_data_(dt, "param_Skin_Temp") != "")
                {
                    content += "<item>皮膚溫度：" + sel_data_(dt, "param_Skin_Temp");
                    if (sel_data_(dt, "param_Skin_Temp_Abnormal") != "")
                    {
                        string[] type = sel_data_(dt, "param_Skin_Temp_Abnormal").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "發熱")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Temp_Abnormal_Hot_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Temp_Abnormal_Hot_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Temp_Abnormal_Hot_position_other") != "")
                                        content += " : " + sel_data_(dt, "param_Skin_Temp_Abnormal_Hot_position_other").Replace("|", ",");
                                }
                                content += "</item>";
                            }
                            else if (type[i] == "冰冷")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Temp_Abnormal_Cold_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Temp_Abnormal_Cold_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Temp_Abnormal_Cold_position_other") != "")
                                        content += " : " + sel_data_(dt, "param_Skin_Temp_Abnormal_Cold_position_other").Replace("|", ",");
                                }
                                content += "</item>";
                            }
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Skin_Exterior") != "")
                {
                    content += "<item>皮膚外觀：" + sel_data_(dt, "param_Skin_Exterior");
                    if (sel_data_(dt, "param_Skin_Exterior_Abnormal") != "")
                    {
                        string[] type = sel_data_(dt, "param_Skin_Exterior_Abnormal").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "蒼白")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Pale_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Exterior_Abnormal_Pale_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Pale_position_other") != "")
                                        content += "" + sel_data_(dt, "param_Skin_Exterior_Abnormal_Pale_position_other").Replace("|", ",");
                                }
                                content += "</item>";
                            }
                            else if (type[i] == "發紺")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Cyanotic_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Exterior_Abnormal_Cyanotic_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Cyanotic_position_other") != "")
                                        content += "" + sel_data_(dt, "param_Skin_Exterior_Abnormal_Cyanotic_position_other").Replace("|", ",");
                                }
                                content += "</item>";
                            }
                            else if (type[i] == "紅疹")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Rash_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Exterior_Abnormal_Rash_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Rash_position_other") != "")
                                        content += "" + sel_data_(dt, "param_Skin_Exterior_Abnormal_Rash_position_other").Replace("|", ",");
                                }
                                content += "</item>";
                            }
                            else if (type[i] == "黃疸")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Jaundice_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Exterior_Abnormal_Jaundice_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Jaundice_position_other") != "")
                                        content += "" + sel_data_(dt, "param_Skin_Exterior_Abnormal_Jaundice_position_other").Replace("|", ",");
                                }
                                content += "</item>";
                            }
                            else if (type[i] == "瘀青")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Bruises_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Exterior_Abnormal_Bruises_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Bruises_position_other") != "")
                                        content += "" + sel_data_(dt, "param_Skin_Exterior_Abnormal_Bruises_position_other").Replace("|", ",");
                                }
                                content += "</item>";
                            }
                            else if (type[i] == "脫屑")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Desquamation_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Exterior_Abnormal_Desquamation_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Desquamation_position_other") != "")
                                        content += "" + sel_data_(dt, "param_Skin_Exterior_Abnormal_Desquamation_position_other").Replace("|", ",");
                                }
                                content += "</item>";
                            }
                            else if (type[i] == "潮濕")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Moist_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Exterior_Abnormal_Moist_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Moist_position_other") != "")
                                        content += "" + sel_data_(dt, "param_Skin_Exterior_Abnormal_Moist_position_other").Replace("|", ",");
                                }
                                content += "</item>";
                            }
                            else if (type[i] == "水泡")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Blister_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Exterior_Abnormal_Blister_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Blister_position_other") != "")
                                        content += "" + sel_data_(dt, "param_Skin_Exterior_Abnormal_Blister_position_other").Replace("|", ",");
                                } content += "</item>";
                            }
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Skin_Wound") != "")
                {
                    content += "<item>傷口：" + sel_data_(dt, "param_Skin_Wound");
                    if (sel_data_(dt, "param_Skin_Wound_Abnormal") != "")
                    {
                        string[] type = sel_data_(dt, "param_Skin_Wound_Abnormal").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "壓瘡" || type[i] == "壓傷")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Wound_Abnormal_Pressure_sores_position") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Wound_Abnormal_Pressure_sores_position");
                                content += "</item>";
                            }
                            else if (type[i] == "手術")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Wound_Abnormal_Surgery_position") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Wound_Abnormal_Surgery_position");
                                content += "</item>";
                            }
                            else if (type[i] == "外傷")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Wound_Abnormal_Trauma_position") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Wound_Abnormal_Trauma_position");
                                content += "</item>";
                            }
                            else if (type[i] == "燙傷")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Wound_Abnormal_Scald_position") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Wound_Abnormal_Scald_position");
                                content += "</item>";
                            }
                            else if (type[i] == "蜂窩性組織炎")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Wound_Abnormal_Cellulitis_position") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Wound_Abnormal_Cellulitis_position");
                                content += "</item>";
                            }
                            else if (type[i] == "糖尿病足(DM Foot)")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Wound_Abnormal_DM_Foot_position") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Wound_Abnormal_DM_Foot_position");
                                content += "</item>";
                            }
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #region 心肺系統
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-5\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"心肺系統\" />";
                content += "<title>心肺系統</title><text><list>";

                if (sel_data_(dt, "param_Breathing_Type") != "")
                {
                    content += "<item>呼吸型態：" + sel_data_(dt, "param_Breathing_Type");
                    if (sel_data_(dt, "param_Breathing_Type_Abnormal") != "")
                        content += "，" + sel_data_(dt, "param_Breathing_Type_Abnormal") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_Breathing_LeftVoive") != "")
                {
                    content += "<item>呼吸音-左側：" + sel_data_(dt, "param_Breathing_LeftVoive");
                    if (sel_data_(dt, "param_Breathing_LeftVoive_Abnormal") != "")
                        content += "，" + sel_data_(dt, "param_Breathing_LeftVoive_Abnormal") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_Breathing_RightVoive") != "")
                {
                    content += "<item>呼吸音-右側：" + sel_data_(dt, "param_Breathing_RightVoive");
                    if (sel_data_(dt, "param_Breathing_RightVoive_Abnormal") != "")
                        content += "，" + sel_data_(dt, "param_Breathing_RightVoive_Abnormal") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_Breathing_Treatment") != "")
                {
                    content += "<item>氧氣治療：" + sel_data_(dt, "param_Breathing_Treatment");
                    if (sel_data_(dt, "param_Breathing_Treatment_Abnormal") != "")
                        content += "，" + sel_data_(dt, "param_Breathing_Treatment_Abnormal");
                    if (sel_data_(dt, "param_Breathing_Treatment_Abnormal_Mask_1") != "")
                        content += "，" + sel_data_(dt, "param_Breathing_Treatment_Abnormal_Mask_1").Replace("|", ",") + "L/min";
                    if (sel_data_(dt, "param_Breathing_Treatment_Abnormal_Mask_2") != "")
                        content += "，" + sel_data_(dt, "param_Breathing_Treatment_Abnormal_Mask_2").Replace("|", ",") + "%";
                    if (sel_data_(dt, "param_Breathing_Treatment_Abnormal_Desc") != "")
                        content += " : " + sel_data_(dt, "param_Breathing_Treatment_Abnormal_Desc").Replace("|", ",");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Heart_Rhythm") != "")
                {
                    content += "<item>心跳節律：" + sel_data_(dt, "param_Heart_Rhythm");
                    if (sel_data_(dt, "param_Heart_Rhythm_Name") != "")
                        content += " : " + sel_data_(dt, "param_Heart_Rhythm_Name").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_Heart_Symptoms") != "")
                {
                    content += "<item>病徵：" + sel_data_(dt, "param_Heart_Symptoms");
                    if (sel_data_(dt, "param_Heart_Symptoms_Dtl") != "")
                    {
                        content += "，" + sel_data_(dt, "param_Heart_Symptoms_Dtl").Replace("其他", "");
                        if (sel_data_(dt, "param_Heart_Symptoms_Dtl_Other") != "")
                            content += " : " + sel_data_(dt, "param_Heart_Symptoms_Dtl_Other").Replace("|", ",");
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Tip") != "")
                {
                    content += "<item>末梢：" + sel_data_(dt, "param_Tip");
                    if (sel_data_(dt, "param_Tip_Abnormal_LeftTop") != "" || sel_data_(dt, "param_Tip_Abnormal_RightTop") != "" || sel_data_(dt, "param_Tip_Abnormal_LeftDown") != "" || sel_data_(dt, "param_Tip_Abnormal_RightDown") != "")
                    {
                        content += "<list>";
                        if (sel_data_(dt, "param_Tip_Abnormal_LeftTop") != "")
                            content += "<item>左上肢：" + sel_data_(dt, "param_Tip_Abnormal_LeftTop") + "</item>";
                        if (sel_data_(dt, "param_Tip_Abnormal_RightTop") != "")
                            content += "<item>右上肢：" + sel_data_(dt, "param_Tip_Abnormal_RightTop") + "</item>";
                        if (sel_data_(dt, "param_Tip_Abnormal_LeftDown") != "")
                            content += "<item>左下肢：" + sel_data_(dt, "param_Tip_Abnormal_LeftDown") + "</item>";
                        if (sel_data_(dt, "param_Tip_Abnormal_RightDown") != "")
                            content += "<item>右下肢：" + sel_data_(dt, "param_Tip_Abnormal_RightDown") + "</item>";
                        content += "</list>";
                    }
                    content += "</item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #region 排泄系統
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-6\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"排泄系統\" />";
                content += "<title>排泄系統</title><text><list>";

                if (sel_data_(dt, "param_piss") != "")
                    content += "<item>解尿：" + sel_data_(dt, "param_piss") + "</item>";
                if (sel_data_(dt, "param_PissStatus") != "")
                {
                    content += "<item>症狀：" + sel_data_(dt, "param_PissStatus");
                    if (sel_data_(dt, "param_PissStatusDesc") != "")
                        content += "，" + sel_data_(dt, "param_PissStatusDesc") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_StoolStatus") != "")
                {
                    content += "<item>排便：" + sel_data_(dt, "param_StoolStatus");
                    if (sel_data_(dt, "param_StoolCount") != "")
                        content += "，" + sel_data_(dt, "param_StoolCount") + "次/";
                    if (sel_data_(dt, "param_StoolSequence") != "")
                        content += sel_data_(dt, "param_StoolSequence") + "日";
                    if (sel_data_(dt, "param_StoolAbnormal") != "")
                        content += "，" + sel_data_(dt, "param_StoolAbnormal");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_PissTrain") != "")
                    content += "<item>解尿訓練：" + sel_data_(dt, "param_PissTrain") + "</item>";
                if (sel_data_(dt, "param_StoolTrain") != "")
                    content += "<item>排便訓練：" + sel_data_(dt, "param_StoolTrain") + "</item>";

                content += "</list></text></section></component>";
                #endregion

                #region 腸胃及營養評估
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-7\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"腸胃及營養評估\" />";
                content += "<title>腸胃及營養評估</title><text><list>";

                if (sel_data_(dt, "param_Mouth") != "")
                {
                    content += "<item>口腔，黏膜：" + sel_data_(dt, "param_Mouth");
                    if (sel_data_(dt, "param_Mouth_Abnormal") != "")
                        content += "，" + sel_data_(dt, "param_Mouth_Abnormal") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_eating_type") != "")
                {
                    content += "<item>飲食種類<list>";
                    string[] type = sel_data_(dt, "param_eating_type").Split(',');
                    for (int i = 0; i < type.Length; i++)
                    {
                        if (type[i] == "牛奶")
                        {
                            content += "<item>" + type[i];
                            if (sel_data_(dt, "param_mdMilk_cc") != "")
                                content += "，奶量 " + sel_data_(dt, "param_mdMilk_cc").Replace("|", ",") + "mL/次";
                            if (sel_data_(dt, "param_mdMilk_day") != "")
                                content += "，奶量 " + sel_data_(dt, "param_mdMilk_day").Replace("|", ",") + "次/天";
                            content += "</item>";
                        }
                        else if (type[i] == "管灌飲食")
                        {
                            content += "<item>" + type[i];
                            if (sel_data_(dt, "param_FoodKind_Tube") != "")
                                content += "，" + sel_data_(dt, "param_FoodKind_Tube");
                            content += "</item>";
                        }
                        else
                            content += "<item>" + type[i] + "</item>";
                    }
                    content += "</list></item>";
                }
                if (sel_data_(dt, "param_Chew") != "")
                    content += "<item>咀嚼：" + sel_data_(dt, "param_Chew") + "</item>";
                if (sel_data_(dt, "param_Swallowing") != "")
                {
                    content += "<item>吞嚥：" + sel_data_(dt, "param_Swallowing");
                    if (sel_data_(dt, "param_SwallowingStatus") != "")
                        content += "，" + sel_data_(dt, "param_SwallowingStatus");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Gastrointestinal") != "")
                {
                    content += "<item>腸胃症狀：" + sel_data_(dt, "param_Gastrointestinal");
                    if (sel_data_(dt, "param_GastrointestinalStatus_1") != "")
                        content += "，嘔吐" + sel_data_(dt, "param_GastrointestinalStatus_1") + "天";
                    if (sel_data_(dt, "param_GastrointestinalStatus_2") != "")
                        content += "，噁心" + sel_data_(dt, "param_GastrointestinalStatus_2") + "天";
                    if (sel_data_(dt, "param_GastrointestinalStatus_3") != "")
                        content += "，腹脹" + sel_data_(dt, "param_GastrointestinalStatus_3") + "天";
                    if (sel_data_(dt, "param_GastrointestinalStatus_4") != "")
                        content += "，腹瀉" + sel_data_(dt, "param_GastrointestinalStatus_4") + "天";
                    if (sel_data_(dt, "param_GastrointestinalStatus_5") != "")
                        content += "，厭食" + sel_data_(dt, "param_GastrointestinalStatus_5") + "天";
                    if (sel_data_(dt, "param_GastrointestinalStatus_6") != "" || sel_data_(dt, "param_GastrointestinalStatus_reason") != "")
                        content += "，其他" + sel_data_(dt, "param_GastrointestinalStatus_reason") + "：" + sel_data_(dt, "param_GastrointestinalStatus_6") + "天";
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Assessment") != "")
                    content += "<item>評估：" + sel_data_(dt, "param_Assessment") + "</item>";

                content += "</list></text></section></component>";
                #endregion

                #region 肌肉骨骼系統
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-8\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"肌肉骨骼系統\" />";
                content += "<title>肌肉骨骼系統</title><text><list>";

                if (sel_data_(dt, "param_RULimb") != "" || sel_data_(dt, "param_RDLimb") != "" || sel_data_(dt, "param_LULimb") != "" || sel_data_(dt, "param_LDLimb") != "")
                {
                    content += "<item>肌力<list>";
                    if (sel_data_(dt, "param_RULimb") != "")
                        content += "<item>右上肢：" + sel_data_(dt, "param_RULimb") + "</item>";
                    if (sel_data_(dt, "param_RDLimb") != "")
                        content += "<item>右下肢：" + sel_data_(dt, "param_RDLimb") + "</item>";
                    if (sel_data_(dt, "param_LULimb") != "")
                        content += "<item>左上肢：" + sel_data_(dt, "param_LULimb") + "</item>";
                    if (sel_data_(dt, "param_LDLimb") != "")
                        content += "<item>左下肢：" + sel_data_(dt, "param_LDLimb") + "</item>";
                    content += "</list></item>";
                }
                if (sel_data_(dt, "param_ActiveStatus") != "")
                {
                    content += "<item>活動情形：" + sel_data_(dt, "param_ActiveStatus");
                    if (sel_data_(dt, "param_UnActiveDesc") != "")
                        content += "，" + sel_data_(dt, "param_UnActiveDesc");
                    if (sel_data_(dt, "param_ActiveDesc") != "")
                        content += "，" + sel_data_(dt, "param_ActiveDesc");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Eat") != "" || sel_data_(dt, "param_Dressing") != "" || sel_data_(dt, "param_Bathing") != "" || sel_data_(dt, "param_Toilet") != "" || sel_data_(dt, "param_Sport") != "")
                {
                    content += "<item>自我照護能力 (ADL)<list>";
                    if (sel_data_(dt, "param_Eat") != "")
                        content += "<item>進食：" + sel_data_(dt, "param_Eat") + "</item>";
                    if (sel_data_(dt, "param_Dressing") != "")
                        content += "<item>穿衣：" + sel_data_(dt, "param_Dressing") + "</item>";
                    if (sel_data_(dt, "param_Bathing") != "")
                        content += "<item>沐浴：" + sel_data_(dt, "param_Bathing") + "</item>";
                    if (sel_data_(dt, "param_Toilet") != "")
                        content += "<item>如廁：" + sel_data_(dt, "param_Toilet") + "</item>";
                    if (sel_data_(dt, "param_Sport") != "")
                        content += "<item>一般運動：" + sel_data_(dt, "param_Sport") + "</item>";
                    content += "</list></item>";
                }
                if (sel_data_(dt, "param_MotionRange") != "")
                {
                    content += "<item>關節活動度：" + sel_data_(dt, "param_MotionRange");
                    if (sel_data_(dt, "param_MotionAbnormalDesc") != "")
                    {
                        string[] type = sel_data_(dt, "param_MotionAbnormalDesc").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "肢體攣縮")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_MotionAbnormalDesc_1_name") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_MotionAbnormalDesc_1_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "關節腫脹")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_MotionAbnormalDesc_2_name") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_MotionAbnormalDesc_2_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "關節變形")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_MotionAbnormalDesc_3_name") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_MotionAbnormalDesc_3_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "晨間僵硬")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_MotionAbnormalDesc_4_name") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_MotionAbnormalDesc_4_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "關節疼痛")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_MotionAbnormalDesc_5_name") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_MotionAbnormalDesc_5_name").Replace("|", ",");
                                content += "</item>";
                            }
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Exterior") != "")
                {
                    content += "<item>外觀：" + sel_data_(dt, "param_Exterior");
                    if (sel_data_(dt, "param_Exterior_Abnorma") != "")
                    {
                        string[] type = sel_data_(dt, "param_Exterior_Abnorma").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "肌肉萎縮")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Exterior_Abnorma_2_p") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_Exterior_Abnorma_2_p").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "脊椎側彎")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Exterior_Abnorma_3_p") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_Exterior_Abnorma_3_p").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "截肢")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Exterior_Abnorma_4_p") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_Exterior_Abnorma_4_p").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "骨折")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Exterior_Abnorma_5_p") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_Exterior_Abnorma_5_p").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "其他")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Exterior_Abnorma_6_n") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_Exterior_Abnorma_6_n").Replace("|", ",");
                                if (sel_data_(dt, "param_Exterior_Abnorma_6_p") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_Exterior_Abnorma_6_p").Replace("|", ",");
                                content += "</item>";
                            }
                            else
                                content += "<item>" + type[i] + "</item>";
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #region 神經系統
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-9\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"神經系統\" />";
                content += "<title>神經系統</title><text><list>";

                if (sel_data_(dt, "param_EyesReflection") != "" || sel_data_(dt, "param_LanguageReflection") != "" || sel_data_(dt, "param_SportReflection") != "")
                {
                    content += "<item>昏迷指標(GCS)<list>";
                    if (sel_data_(dt, "param_EyesReflection") != "")
                        content += "<item>睜眼反射(E)：" + sel_data_(dt, "param_EyesReflection") + "</item>";
                    if (sel_data_(dt, "param_LanguageReflection") != "")
                        content += "<item>語言反射(V)：" + sel_data_(dt, "param_LanguageReflection") + "</item>";
                    if (sel_data_(dt, "param_SportReflection") != "")
                        content += "<item>運動反射(M)：" + sel_data_(dt, "param_SportReflection") + "</item>";
                    content += "</list></item>";
                }
                if (sel_data_(dt, "param_pupil_size_left") != "")
                    content += "<item>瞳孔大小-左眼：" + sel_data_(dt, "param_pupil_size_left") + "</item>";
                if (sel_data_(dt, "param_pupil_size_right") != "")
                    content += "<item>瞳孔大小-右眼：" + sel_data_(dt, "param_pupil_size_right") + "</item>";
                if (sel_data_(dt, "param_eye_reflect_left") != "")
                    content += "<item>瞳孔反應-左眼：" + sel_data_(dt, "param_eye_reflect_left") + "</item>";
                if (sel_data_(dt, "param_eye_reflect_right") != "")
                    content += "<item>瞳孔反應-右眼：" + sel_data_(dt, "param_eye_reflect_right") + "</item>";
                if (sel_data_(dt, "param_Gait") != "")
                    content += "<item>步態：" + sel_data_(dt, "param_Gait") + "</item>";
                if (sel_data_(dt, "param_Talk") != "")
                {
                    content += "<item>語言表達：" + sel_data_(dt, "param_Talk");
                    if (sel_data_(dt, "param_UnTalkDesc") != "")
                    {
                        content += "，" + sel_data_(dt, "param_UnTalkDesc").Replace("其他", "");
                        if (sel_data_(dt, "param_UnTalkDesc_other") != "")
                            content += sel_data_(dt, "param_UnTalkDesc_other").Replace("|", ",");
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Cognitive") != "")
                {
                    content += "<item>認知感受：" + sel_data_(dt, "param_Cognitive");
                    if (sel_data_(dt, "param_CognitiveStatus") != "" || sel_data_(dt, "param_CognitivePain") != "" || sel_data_(dt, "param_CognitiveNoFeeling") != "" || sel_data_(dt, "param_CognitiveHemp") != "" || sel_data_(dt, "param_CognitivePumpedStorage") != "")
                    {
                        content += "<list>";
                        if (sel_data_(dt, "param_CognitiveStatus") != "")
                        {
                            string[] type = sel_data_(dt, "param_CognitiveStatus").Split(',');
                            for (int i = 0; i < type.Length; i++)
                                content += "<item>" + type[i] + "</item>";
                        }
                        if (sel_data_(dt, "param_CognitivePain") != "")
                        {
                            content += "<item>疼痛，部位：" + sel_data_(dt, "param_CognitivePain").Replace("其他", "");
                            if (sel_data_(dt, "param_CognitivePainOther") != "")
                                content += sel_data_(dt, "param_CognitivePainOther").Replace("|", ",");
                            content += "</item>";
                        }
                        if (sel_data_(dt, "param_CognitiveNoFeeling") != "")
                        {
                            content += "<item>無知覺，部位：" + sel_data_(dt, "param_CognitiveNoFeeling").Replace("其他", "");
                            if (sel_data_(dt, "param_CognitiveNoFeelingOther") != "")
                                content += sel_data_(dt, "param_CognitiveNoFeelingOther").Replace("|", ",");
                            content += "</item>";
                        }
                        if (sel_data_(dt, "param_CognitiveHemp") != "")
                        {
                            content += "<item>麻，部位：" + sel_data_(dt, "param_CognitiveHemp").Replace("其他", "");
                            if (sel_data_(dt, "param_CognitiveHempOther") != "")
                                content += sel_data_(dt, "param_CognitiveHempOther").Replace("|", ",");
                            content += "</item>";
                        }
                        if (sel_data_(dt, "param_CognitivePumpedStorage") != "")
                        {
                            content += "<item>抽搐，部位：" + sel_data_(dt, "param_CognitivePumpedStorage").Replace("其他", "");
                            if (sel_data_(dt, "param_CognitivePumpedStorageOther") != "")
                                content += sel_data_(dt, "param_CognitivePumpedStorageOther").Replace("|", ",");
                            content += "</item>";
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #region 生殖系統
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-10\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"生殖系統\" />";
                content += "<title>生殖系統</title><text><list>";

                if (sel_data_(dt, "param_Light") != "")
                {
                    content += "<item>生殖系統：" + sel_data_(dt, "param_Light") + "</item>";
                    if (sel_data_(dt, "param_Gynecology") == "男")
                    {
                        if (sel_data_(dt, "param_Light_Boy_Abnormal") != "")
                        {
                            content += "<item>" + sel_data_(dt, "param_Light_Boy_Abnormal");
                            if (sel_data_(dt, "param_Light_Boy_Abnormal_1") != "")
                                content += "，" + sel_data_(dt, "param_Light_Boy_Abnormal_1");
                            if (sel_data_(dt, "param_Light_Boy_Abnormal_2") != "")
                                content += "，性狀：" + sel_data_(dt, "param_Light_Boy_Abnormal_2");
                            content += "</item>";
                        }
                    }
                    else if (sel_data_(dt, "param_Gynecology") == "女")
                    {
                        if (sel_data_(dt, "param_FBAbnormal") != "")
                        {
                            content += "<item>乳房，陰道：" + sel_data_(dt, "param_FBAbnormal");
                            if (sel_data_(dt, "param_FBAbnormal_Dtl") != "")
                            {
                                content += "，" + sel_data_(dt, "param_FBAbnormal_Dtl").Replace("其他", "");
                                if (sel_data_(dt, "param_FBAbnormal_Dtl_other") != "")
                                    content += "，" + sel_data_(dt, "param_FBAbnormal_Dtl_other").Replace("|", ",");
                            }
                            content += "</item>";
                        }
                        if (sel_data_(dt, "param_MCStart") != "" || sel_data_(dt, "param_Last_MC") != "" || sel_data_(dt, "param_MCCycle") != "" || sel_data_(dt, "param_MCCycle_rule") != "" || sel_data_(dt, "param_MCDay") != "" || sel_data_(dt, "param_MCAmount") != "" || sel_data_(dt, "param_FBAbnormalDtl") != "")
                        {
                            content += "<item>月經<list>";
                            if (sel_data_(dt, "param_MCStart") != "")
                                content += "<item>初經年齡：" + sel_data_(dt, "param_MCStart") + "歲</item>";
                            if (sel_data_(dt, "param_Last_MC") != "")
                                content += "<item>最後月經日：" + sel_data_(dt, "param_Last_MC") + "</item>";
                            if (sel_data_(dt, "param_MCCycle") != "")
                            {
                                content += "<item>月經週期：" + sel_data_(dt, "param_MCCycle");
                                if (sel_data_(dt, "param_MCCycle_rule") != "")
                                    content += "，" + sel_data_(dt, "param_MCCycle_rule");
                                content += "</item>";
                            }
                            if (sel_data_(dt, "param_MCDay") != "")
                                content += "<item>月經天數：" + sel_data_(dt, "param_MCDay") + "天</item>";
                            if (sel_data_(dt, "param_MCAmount") != "")
                                content += "<item>月經量：" + sel_data_(dt, "param_MCAmount") + "</item>";
                            if (sel_data_(dt, "param_FBAbnormalDtl") != "")
                            {
                                content += "<item>月經期間：" + sel_data_(dt, "param_FBAbnormalDtl").Replace("其他", "");
                                if (sel_data_(dt, "param_FBAbnormalOther") != "")
                                    content += sel_data_(dt, "param_FBAbnormalOther").Replace("|", ",");
                                content += "</item>";
                            }
                            content += "</list></item>";
                        }
                    }
                }

                content += "</list></text></section></component>";
                #endregion

                #region 預防接種
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-11\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"預防接種\" />";
                content += "<title>預防接種</title><text><list>";

                if (sel_data_(dt, "param_HepatitisB") != "")
                    content += "<item>B肝免疫球蛋白：" + sel_data_(dt, "param_HepatitisB") + "</item>";
                if (sel_data_(dt, "param_3in1Vaccine") != "")
                    content += "<item>三合一疫苗：" + sel_data_(dt, "param_3in1Vaccine") + "</item>";
                if (sel_data_(dt, "param_VaricellaVaccine") != "")
                    content += "<item>水痘疫苗：" + sel_data_(dt, "param_VaricellaVaccine") + "</item>";
                if (sel_data_(dt, "param_BCG") != "")
                    content += "<item>卡介苗：" + sel_data_(dt, "param_BCG") + "</item>";
                if (sel_data_(dt, "param_5in1Vaccine") != "")
                    content += "<item>五合一疫苗：" + sel_data_(dt, "param_5in1Vaccine") + "</item>";
                if (sel_data_(dt, "param_InfluenzaVaccination") != "")
                    content += "<item>流感疫苗：" + sel_data_(dt, "param_InfluenzaVaccination") + "</item>";
                if (sel_data_(dt, "param_HepatitisBvaccine") != "")
                    content += "<item>B型肝炎疫苗：" + sel_data_(dt, "param_HepatitisBvaccine") + "</item>";
                if (sel_data_(dt, "param_6in1Vaccine") != "")
                    content += "<item>六合一疫苗：" + sel_data_(dt, "param_6in1Vaccine") + "</item>";
                if (sel_data_(dt, "param_HaemophilusVaccine") != "")
                    content += "<item>嗜血桿菌疫苗：" + sel_data_(dt, "param_HaemophilusVaccine") + "</item>";
                if (sel_data_(dt, "param_PneumococcalVaccine") != "")
                    content += "<item>肺炎雙球菌疫苗：" + sel_data_(dt, "param_PneumococcalVaccine") + "</item>";
                if (sel_data_(dt, "param_OralVaccine") != "")
                    content += "<item>口服輪狀病毒疫苗：" + sel_data_(dt, "param_OralVaccine") + "</item>";
                if (sel_data_(dt, "param_Stre_PneumococcalVaccine") != "")
                    content += "<item>肺炎鏈球菌疫苗：" + sel_data_(dt, "param_Stre_PneumococcalVaccine") + "</item>";
                if (sel_data_(dt, "VaccineName") != "" || sel_data_(dt, "VaccineAmt") != "")
                {
                    content += "<item>疫苗名稱：" + sel_data_(dt, "VaccineName");
                    content += "，共" + sel_data_(dt, "VaccineAmt") + "劑</item>";
                }
                if (sel_data_(dt, "VaccineName1") != "" || sel_data_(dt, "VaccineAmt1") != "")
                {
                    content += "<item>疫苗名稱：" + sel_data_(dt, "VaccineName1");
                    content += "，共" + sel_data_(dt, "VaccineAmt1") + "劑</item>";
                }
                if (sel_data_(dt, "VaccineName2") != "" || sel_data_(dt, "VaccineAmt2") != "")
                {
                    content += "<item>疫苗名稱：" + sel_data_(dt, "VaccineName2");
                    content += "，共" + sel_data_(dt, "VaccineAmt2") + "劑</item>";
                }
                content += "</list></text></section></component>";
                #endregion

                #region 出院準備計畫評估
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-12\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"出院準備計畫評估\" />";
                content += "<title>出院準備計畫評估</title><text><list>";

                if (sel_data_(dt, "param_CPDBook") != "" || sel_data_(dt, "param_CPDAwareness") != "" || sel_data_(dt, "param_CPDActive") != "" || sel_data_(dt, "param_CPDPiss") != "" || sel_data_(dt, "param_CPDStool") != "" || sel_data_(dt, "param_CPDCare") != "" || sel_data_(dt, "param_CPDResouce") != "" || sel_data_(dt, "param_CPDSP2") != "" || sel_data_(dt, "param_CPDScole") != "")
                {
                    content += "<item>出院準備計畫評估<list>";
                    if (sel_data_(dt, "param_CPDBook") != "")
                        content += "<item>殘障手冊：" + sel_data_(dt, "param_CPDBook") + "</item>";
                    if (sel_data_(dt, "param_CPDAwareness") != "")
                        content += "<item>意識：" + sel_data_(dt, "param_CPDAwareness") + "</item>";
                    if (sel_data_(dt, "param_CPDActive") != "")
                        content += "<item>活動：" + sel_data_(dt, "param_CPDActive") + "</item>";
                    if (sel_data_(dt, "param_CPDPiss") != "")
                        content += "<item>解尿：" + sel_data_(dt, "param_CPDPiss") + "</item>";
                    if (sel_data_(dt, "param_CPDStool") != "")
                        content += "<item>大便：" + sel_data_(dt, "param_CPDStool") + "</item>";
                    if (sel_data_(dt, "param_CPDCare") != "")
                        content += "<item>照顧特質：" + sel_data_(dt, "param_CPDCare") + "</item>";
                    if (sel_data_(dt, "param_CPDResouce") != "")
                        content += "<item>照顧資源：" + sel_data_(dt, "param_CPDResouce") + "</item>";
                    if (sel_data_(dt, "param_CPDSP2") != "")
                    {
                        content += "<item>特殊照護：" + sel_data_(dt, "param_CPDSP2");
                        if (sel_data_(dt, "param_CPDSP2Other") != "")
                            content += " : " + sel_data_(dt, "param_CPDSP2Other").Replace("|", ",");
                        content += "</item>";
                    }
                    if (sel_data_(dt, "param_CPDScole") != "")
                        content += "<item>總分：" + sel_data_(dt, "param_CPDScole") + "</item>";
                    content += "</list></item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #region 聯絡資料
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-13\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"聯絡資料\" />";
                content += "<title>聯絡資料</title><text><list>";

                if (sel_data_(dt, "param_EMGContact") != "")
                {
                    content += "<item>聯絡資料<list>";
                    string[] Name = sel_data_(dt, "param_EMGContact").Split(',');
                    string[] Role_other = sel_data_(dt, "param_ContactRole_other").Split(',');
                    string[] Phone_1 = sel_data_(dt, "param_EMGContact_1").Split(',');
                    string[] Phone_2 = sel_data_(dt, "param_EMGContact_2").Split(',');
                    string[] Phone_3 = sel_data_(dt, "param_EMGContact_3").Split(',');
                    for (int i = 0; i < Name.Length; i++)
                    {
                        content += "<item>緊急聯絡人姓名：" + Name[i].Replace("|", ",");
                        if (i == 0)
                            content += "，稱謂：" + sel_data_(dt, "param_ContactRole").Replace("其他", "") + " " + Role_other[i].Replace("|", ",");
                        else
                            content += "，稱謂：" + sel_data_(dt, "param_ContactRole_" + i.ToString()).Replace("其他", "") + " " + Role_other[i].Replace("|", ",");
                        content += "，連絡電話-公司：" + Phone_1[i];
                        content += "，連絡電話-住家：" + Phone_2[i];
                        content += "，連絡電話-手機：" + Phone_3[i];
                        content += "</item>";
                    }
                    content += "</list></item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #region 兒童發展評估
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-14\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"兒童發展評估\" />";
                content += "<title>兒童發展評估</title><text><list>";

                DataTable dt_chlid = ass_m.sel_child_develope(feeno);
                if (dt_chlid.Rows.Count > 0)
                {
                    string value_temp = dt_chlid.Rows[0]["VAL"].ToString();
                    string[] value = new string[value_temp.Length];
                    for (int i = 0; i < value_temp.Length; i++)
                        value[i] = value_temp.Substring(i, 1);
                    for (int i = 0; i < dt_chlid.Rows.Count; i++)
                        content += "<item>" + dt_chlid.Rows[i]["ASSESS_ITEM"].ToString() + "：" + value[i].Replace("Y", "是").Replace("N", "否") + "</item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #endregion
            }
            else if (natype == "S")//精神
            {
                #region 設定內容

                #region 基本資料
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-0\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"基本資料\" />";
                content += "<title>基本資料</title><text><list>";

                if (sel_data_(dt, "param_tube_date") != "" && sel_data_(dt, "param_tube_time") != "")
                    content += "<item>入院日期：" + sel_data_(dt, "param_tube_date") + " " + sel_data_(dt, "param_tube_time") + "</item>";
                if (sel_data_(dt, "param_ipd_reason") != "")
                    content += "<item>入院原因：" + sel_data_(dt, "param_ipd_reason").Replace("|", ",") + "</item>";
                if (sel_data_(dt, "param_ipd_style") != "")
                {
                    content += "<item>入院方式：" + sel_data_(dt, "param_ipd_style").Replace("其他", "");
                    if (sel_data_(dt, "param_ipd_style_other") != "")
                        content += sel_data_(dt, "param_ipd_style_other").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_education") != "")
                    content += "<item>教育：" + sel_data_(dt, "param_education") + "</item>";
                if (sel_data_(dt, "param_job") != "")
                {
                    content += "<item>職業：" + sel_data_(dt, "param_job").Replace("其他", "");
                    if (sel_data_(dt, "param_job_other") != "")
                        content += sel_data_(dt, "param_job_other").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_economy") != "")
                    content += "<item>經濟狀況：" + sel_data_(dt, "param_economy") + "</item>";
                if (sel_data_(dt, "param_payment") != "")
                    content += "<item>身分別：" + sel_data_(dt, "param_payment") + "</item>";
                if (sel_data_(dt, "param_religion") != "")
                {
                    content += "<item>宗教：" + sel_data_(dt, "param_religion").Replace("其他", "");
                    if (sel_data_(dt, "param_religion_other") != "")
                        content += sel_data_(dt, "param_religion_other").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_lang") != "")
                {
                    content += "<item>語言：" + sel_data_(dt, "param_lang").Replace("其他", "");
                    if (sel_data_(dt, "param_lang_other") != "")
                        content += sel_data_(dt, "param_lang_other").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_needtrans") != "")
                    content += "<item>需翻譯：" + sel_data_(dt, "param_needtrans") + "</item>";
                if (sel_data_(dt, "param_marrage") != "")
                    content += "<item>婚姻狀況：" + sel_data_(dt, "param_marrage") + "</item>";
                if (sel_data_(dt, "param_care") != "")
                {
                    content += "<item>主要照顧者：" + sel_data_(dt, "param_care").Replace("其他", "");
                    if (sel_data_(dt, "param_care_other") != "")
                        content += sel_data_(dt, "param_care_other").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_psychological") != "")
                {
                    content += "<item>心理狀況：" + sel_data_(dt, "param_psychological").Replace("其他", "");
                    if (sel_data_(dt, "param_psychological_other") != "")
                        content += sel_data_(dt, "param_psychological_other").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_child") != "")
                {
                    content += "<item>子女：" + sel_data_(dt, "param_child");
                    if (sel_data_(dt, "param_child_f") != "")
                        content += "，男" + sel_data_(dt, "param_child_f") + "人";
                    if (sel_data_(dt, "param_child_m") != "")
                        content += "，女" + sel_data_(dt, "param_child_m") + "人";
                    content += "</item>";
                }
                if (sel_data_(dt, "param_living") != "")
                {
                    content += "<item>居住方式：" + sel_data_(dt, "param_living");
                    if (sel_data_(dt, "param_living_other") != "")
                        content += " : " + sel_data_(dt, "param_living_other").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_Volunteer_Help") != "")
                {
                    content += "<item>需社工幫助：" + sel_data_(dt, "param_Volunteer_Help");
                    if (sel_data_(dt, "param_Volunteer_Help_Dtl") != "")
                        content += "，" + sel_data_(dt, "param_Volunteer_Help_Dtl").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_cigarette") != "")
                {
                    if (sel_data_(dt, "param_cigarette") == "1")
                        content += "<item>抽菸：無";
                    if (sel_data_(dt, "param_cigarette") == "2")
                        content += "<item>抽菸：有";
                    if (sel_data_(dt, "param_cigarette") == "3")
                        content += "<item>抽菸：戒菸";
                    if (sel_data_(dt, "param_cigarette_yes_amount") != "")
                        content += "，每日" + sel_data_(dt, "param_cigarette_yes_amount") + "支";
                    if (sel_data_(dt, "param_cigarette_yes_year") != "")
                        content += "，已抽" + sel_data_(dt, "param_cigarette_yes_year") + "年";
                    if (sel_data_(dt, "param_cigarette_agree_stop") != "")
                        content += "，有無戒菸意願：" + sel_data_(dt, "param_cigarette_agree_stop");
                    if (sel_data_(dt, "param_cigarette_stop_year") != "")
                        content += "，" + sel_data_(dt, "param_cigarette_stop_year") + "年";
                    content += "</item>";
                }
                if (sel_data_(dt, "param_drink") != "")
                {
                    content += "<item>喝酒：" + sel_data_(dt, "param_drink");
                    if (sel_data_(dt, "param_drink_day") != "")
                        content += "，每日劑量" + sel_data_(dt, "param_drink_day") + "瓶";
                    if (sel_data_(dt, "param_drink_unit") != "")
                        content += " " + sel_data_(dt, "param_drink_unit") + "mL";
                    content += "</item>";
                }
                content += "</list></text></section></component>";

                #endregion

                #region 精神病史與過去病史
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-1\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"精神病史與過去病史\" />";
                content += "<title>精神病史與過去病史</title><text><list>";

                if (sel_data_(dt, "param_First") != "")
                    content += "<item>首次發病時間：" + sel_data_(dt, "param_First") + "</item>";
                if (sel_data_(dt, "param_Process") != "")
                {
                    content += "<item>當時處理：" + sel_data_(dt, "param_Process");
                    if (sel_data_(dt, "param_Process_Other") != "")
                        content += "，" + sel_data_(dt, "param_Process_Other").Replace("|", ",");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_hasipd") != "")
                {
                    content += "<item>住院次數：" + sel_data_(dt, "param_hasipd");
                    if (sel_data_(dt, "param_ipd_count") != "")
                        content += "，" + sel_data_(dt, "param_ipd_count");
                    if (sel_data_(dt, "param_ipd_lasttime") != "")
                        content += "，最近一次時間" + sel_data_(dt, "param_ipd_lasttime");
                    if (sel_data_(dt, "param_ipd_diag") != "")
                        content += "，診斷" + sel_data_(dt, "param_ipd_diag").Replace("|", ",");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_im_history") != "")
                {
                    content += "<item>內科病史：" + sel_data_(dt, "param_im_history");
                    if (sel_data_(dt, "param_im_history_item1") != "")
                        content += "，高血壓" + sel_data_(dt, "param_im_history_item1") + "年";
                    if (sel_data_(dt, "param_im_history_item2") != "")
                        content += "，心臟病" + sel_data_(dt, "param_im_history_item2") + "年";
                    if (sel_data_(dt, "param_im_history_item3") != "")
                        content += "，糖尿病" + sel_data_(dt, "param_im_history_item3") + "年";
                    if (sel_data_(dt, "param_im_history_item4") != "")
                        content += "，氣喘" + sel_data_(dt, "param_im_history_item4") + "年";
                    if (sel_data_(dt, "param_im_history_item_other") != "")
                        content += "，其他疾病：" + sel_data_(dt, "param_im_history_item_other").Replace("其他", "");
                    if (sel_data_(dt, "param_im_history_item_other_txt") != "" && sel_data_(dt, "param_im_history_item_other").IndexOf("其他") > -1)
                        content += sel_data_(dt, "param_im_history_item_other_txt").Replace("|", ",");
                    if (sel_data_(dt, "param_im_history_status") != "")
                        content += "，疾病發生時間，處理情形及目前狀況：" + sel_data_(dt, "param_im_history_status").Replace("|", ",");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_su_history") != "")
                {
                    content += "<item>外科病史：" + sel_data_(dt, "param_su_history");
                    if (sel_data_(dt, "param_su_history_trauma_txt") != "")
                        content += "，外傷：" + sel_data_(dt, "param_su_history_trauma_txt").Replace("|", ",");
                    if (sel_data_(dt, "param_su_history_surgery_txt") != "")
                        content += "，手術：" + sel_data_(dt, "param_su_history_surgery_txt").Replace("|", ",");
                    if (sel_data_(dt, "param_su_history_other_txt") != "")
                        content += "，外傷/手術/外科疾病：" + sel_data_(dt, "param_su_history_other_txt").Replace("|", ",");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_other_history") != "")
                {
                    content += "<item>其他病史：" + sel_data_(dt, "param_other_history");
                    if (sel_data_(dt, "param_other_history_desc") != "")
                        content += "，" + sel_data_(dt, "param_other_history_desc").Replace("|", ",");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_med") != "")
                {
                    content += "<item>目前服用藥物：" + sel_data_(dt, "param_med");
                    if (sel_data_(dt, "param_med") == "有")
                    {
                        //取得目前用藥
                        List<DrugOrder> Drug_list = new List<DrugOrder>();
                        byte[] labfoByteCode = webService.GetOpdMed(feeno);
                        if (labfoByteCode != null)
                        {
                            string labJosnArr = CompressTool.DecompressString(labfoByteCode);
                            Drug_list = JsonConvert.DeserializeObject<List<DrugOrder>>(labJosnArr);
                            if (Drug_list.Count > 0)
                            {
                                content += "<list>";
                                for (int i = 0; i < Drug_list.Count; i++)
                                {
                                    content += "<item>門診用藥-藥物名稱：" + trans_special_code(Drug_list[i].DrugName);
                                    content += "，頻次：" + trans_special_code(Drug_list[i].Feq);
                                    content += "，劑量：" + trans_special_code(Drug_list[i].Dose.ToString());
                                    content += "，途徑：" + trans_special_code(Drug_list[i].Route) + "</item>";
                                }
                                content += "</list>";
                            }
                        }
                    }
                    if (sel_data_(dt, "param_med_name") != "")
                    {
                        content += "<list>";
                        string[] DrugName = sel_data_(dt, "param_med_name").Split(',');
                        string[] Feq = sel_data_(dt, "param_med_frequency").Split(',');
                        string[] Dose = sel_data_(dt, "param_med_amount").Split(',');
                        string[] Route = sel_data_(dt, "param_med_way").Split(',');
                        for (int i = 0; i < DrugName.Length; i++)
                        {
                            content += "<item>藥物名稱：" + DrugName[i].Replace("|", ",");
                            content += "，頻次：" + Feq[i].Replace("|", ",");
                            content += "，劑量：" + Dose[i].Replace("|", ",");
                            content += "，途徑：" + Route[i].Replace("|", ",") + "</item>";
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_allergy_med") != "")
                {
                    content += "<item>過敏史藥物：" + sel_data_(dt, "param_allergy_med");
                    if (sel_data_(dt, "param_allergy_med_other") != "")
                    {
                        string[] type = sel_data_(dt, "param_allergy_med_other").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "不詳")
                                content += "<item>不詳</item>";
                            else if (type[i] == "pyrin")
                            {
                                content += "<item>匹林系藥物(pyrin)";
                                if (sel_data_(dt, "param_allergy_med_other_2_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_med_other_2_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "aspirin")
                                content += "<item>水楊酸鹽類(包括aspirin)</item>";
                            else if (type[i] == "NSAID")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_med_other_4_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_med_other_4_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "磺氨類")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_med_other_6_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_med_other_6_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "盤尼西林類")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_med_other_7_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_med_other_7_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "抗生素類")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_med_other_8_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_med_other_8_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "麻醉藥")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_med_other_9_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_med_other_9_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "其他")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_med_other_10_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_med_other_10_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else
                                content += "<item>" + type[i] + "</item>";
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_allergy_food") != "")
                {
                    content += "<item>過敏史食物：" + sel_data_(dt, "param_allergy_food");
                    if (sel_data_(dt, "param_allergy_food_other") != "")
                    {
                        string[] type = sel_data_(dt, "param_allergy_food_other").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "海鮮類")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_food_other_2_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_food_other_2_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "水果")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_food_other_4_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_food_other_4_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "其他")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_food_other_6_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_food_other_6_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else
                                content += "<item>" + type[i] + "</item>";
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_allergy_other") != "")
                {
                    content += "<item>過敏史其他：" + sel_data_(dt, "param_allergy_other");
                    if (sel_data_(dt, "param_allergy_other_other") != "")
                    {
                        string[] type = sel_data_(dt, "param_allergy_other_other").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "不詳")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_other_other_1_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_other_other_1_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "輸血")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_other_other_2_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_other_other_2_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "油漆")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_other_other_3_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_other_other_3_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "昆蟲")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_other_other_4_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_other_other_4_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i].IndexOf("麈") > -1)
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_other_other_5_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_other_other_5_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "其他")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_other_other_6_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_other_other_6_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else
                                content += "<item>" + type[i] + "</item>";
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }
                content += "</list></text></section></component>";
                #endregion

                #region 家族病史
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-2\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"家族病史\" />";
                content += "<title>家族病史</title><text><list>";

                if (sel_data_(dt, "param_family_history") != "")
                    content += "<item>家族病史：" + sel_data_(dt, "param_family_history") + "</item>";
                if (sel_data_(dt, "param_bp") != "")
                    content += "<item>高血壓：" + sel_data_(dt, "param_bp") + "</item>";
                if (sel_data_(dt, "param_kind") != "")
                    content += "<item>腎臟病：" + sel_data_(dt, "param_kind") + "</item>";
                if (sel_data_(dt, "param_asthma") != "")
                    content += "<item>氣喘：" + sel_data_(dt, "param_asthma") + "</item>";
                if (sel_data_(dt, "param_epilepsy") != "")
                    content += "<item>癲癇：" + sel_data_(dt, "param_epilepsy") + "</item>";
                if (sel_data_(dt, "param_HeartDisease") != "")
                    content += "<item>心臟病：" + sel_data_(dt, "param_HeartDisease") + "</item>";
                if (sel_data_(dt, "param_PepticUlcer") != "")
                    content += "<item>消化性潰瘍：" + sel_data_(dt, "param_PepticUlcer") + "</item>";
                if (sel_data_(dt, "param_tuberculosis") != "")
                    content += "<item>肺結核：" + sel_data_(dt, "param_tuberculosis") + "</item>";
                if (sel_data_(dt, "param_MentalIllness") != "")
                    content += "<item>精神病：" + sel_data_(dt, "param_MentalIllness") + "</item>";
                if (sel_data_(dt, "param_Diabetes") != "")
                    content += "<item>糖尿病：" + sel_data_(dt, "param_Diabetes") + "</item>";
                if (sel_data_(dt, "param_Cancer") != "")
                    content += "<item>癌症：" + sel_data_(dt, "param_Cancer") + "</item>";
                if (sel_data_(dt, "param_LiverDisease") != "")
                    content += "<item>肝臟疾病：" + sel_data_(dt, "param_LiverDisease") + "</item>";
                if (sel_data_(dt, "param_OtherDiseaseDesc") != "" || sel_data_(dt, "param_OtherDisease") != "")
                    content += "<item>其他疾病名稱：" + sel_data_(dt, "param_OtherDiseaseDesc").Replace("|", ",") + "，" + sel_data_(dt, "param_OtherDisease") + "</item>";

                content += "</list></text></section></component>";
                #endregion

                #region 一般外觀
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-3\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"一般外觀\" />";
                content += "<title>一般外觀</title><text><list>";

                if (sel_data_(dt, "param_LeftEye") != "")
                {
                    content += "<item>左眼視力：" + sel_data_(dt, "param_LeftEye").Replace("其他", "");
                    if (sel_data_(dt, "param_LeftEyeOther") != "")
                        content += sel_data_(dt, "param_LeftEyeOther").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_LeftEyeRedress") != "")
                {
                    content += "<item>左眼矯正：" + sel_data_(dt, "param_LeftEyeRedress").Replace("其他", "");
                    if (sel_data_(dt, "param_LeftEyeRedressDesc") != "")
                        content += sel_data_(dt, "param_LeftEyeRedressDesc").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_RightEye") != "")
                {
                    content += "<item>右眼視力：" + sel_data_(dt, "param_RightEye").Replace("其他", "");
                    if (sel_data_(dt, "param_RightEyeOther") != "")
                        content += sel_data_(dt, "param_RightEyeOther").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_RightEyeRedress") != "")
                {
                    content += "<item>右眼矯正：" + sel_data_(dt, "param_RightEyeRedress").Replace("其他", "");
                    if (sel_data_(dt, "param_RightEyeRedressDesc") != "")
                        content += sel_data_(dt, "param_RightEyeRedressDesc").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_LeftEra") != "")
                {
                    content += "<item>左耳聽力：" + sel_data_(dt, "param_LeftEra").Replace("其他", "");
                    if (sel_data_(dt, "param_LeftEraDesc") != "")
                        content += sel_data_(dt, "param_LeftEraDesc").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_RightEra") != "")
                {
                    content += "<item>右耳聽力：" + sel_data_(dt, "param_RightEra").Replace("其他", "");
                    if (sel_data_(dt, "param_RightEraDesc") != "")
                        content += " : " + sel_data_(dt, "param_RightEraDesc").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_Mouth") != "")
                {
                    content += "<item>口腔，黏膜：" + sel_data_(dt, "param_Mouth");
                    if (sel_data_(dt, "param_Mouth_Abnormal") != "")
                        content += "，" + sel_data_(dt, "param_Mouth_Abnormal") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_Tooth") != "")
                {
                    content += "<item>假牙：" + sel_data_(dt, "param_Tooth");
                    if (sel_data_(dt, "param_ToothUpDesc") != "")
                        content += "，上：" + sel_data_(dt, "param_ToothUpDesc");
                    if (sel_data_(dt, "param_ToothDownDesc") != "")
                        content += "，下：" + sel_data_(dt, "param_ToothDownDesc");
                    content += "</item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #region 皮膚狀況
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-4\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"皮膚狀況\" />";
                content += "<title>皮膚狀況</title><text><list>";

                if (sel_data_(dt, "param_Skin_Temp") != "")
                {
                    content += "<item>皮膚溫度：" + sel_data_(dt, "param_Skin_Temp");
                    if (sel_data_(dt, "param_Skin_Temp_Abnormal") != "")
                    {
                        string[] type = sel_data_(dt, "param_Skin_Temp_Abnormal").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "發熱")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Temp_Abnormal_Hot_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Temp_Abnormal_Hot_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Temp_Abnormal_Hot_position_other") != "")
                                        content += " : " + sel_data_(dt, "param_Skin_Temp_Abnormal_Hot_position_other").Replace("|", ",");
                                }
                                content += "</item>";
                            }
                            else if (type[i] == "冰冷")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Temp_Abnormal_Cold_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Temp_Abnormal_Cold_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Temp_Abnormal_Cold_position_other") != "")
                                        content += " : " + sel_data_(dt, "param_Skin_Temp_Abnormal_Cold_position_other").Replace("|", ",");
                                }
                                content += "</item>";
                            }
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Skin_Exterior") != "")
                {
                    content += "<item>皮膚外觀：" + sel_data_(dt, "param_Skin_Exterior");
                    if (sel_data_(dt, "param_Skin_Exterior_Abnormal") != "")
                    {
                        string[] type = sel_data_(dt, "param_Skin_Exterior_Abnormal").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "蒼白")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Pale_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Exterior_Abnormal_Pale_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Pale_position_other") != "")
                                        content += "" + sel_data_(dt, "param_Skin_Exterior_Abnormal_Pale_position_other").Replace("|", ",");
                                }
                                content += "</item>";
                            }
                            else if (type[i] == "發紺")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Cyanotic_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Exterior_Abnormal_Cyanotic_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Cyanotic_position_other") != "")
                                        content += "" + sel_data_(dt, "param_Skin_Exterior_Abnormal_Cyanotic_position_other").Replace("|", ",");
                                }
                                content += "</item>";
                            }
                            else if (type[i] == "紅疹")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Rash_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Exterior_Abnormal_Rash_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Rash_position_other") != "")
                                        content += "" + sel_data_(dt, "param_Skin_Exterior_Abnormal_Rash_position_other").Replace("|", ",");
                                }
                                content += "</item>";
                            }
                            else if (type[i] == "黃疸")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Jaundice_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Exterior_Abnormal_Jaundice_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Jaundice_position_other") != "")
                                        content += "" + sel_data_(dt, "param_Skin_Exterior_Abnormal_Jaundice_position_other").Replace("|", ",");
                                }
                                content += "</item>";
                            }
                            else if (type[i] == "瘀青")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Bruises_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Exterior_Abnormal_Bruises_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Bruises_position_other") != "")
                                        content += "" + sel_data_(dt, "param_Skin_Exterior_Abnormal_Bruises_position_other").Replace("|", ",");
                                }
                                content += "</item>";
                            }
                            else if (type[i] == "脫屑")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Desquamation_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Exterior_Abnormal_Desquamation_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Desquamation_position_other") != "")
                                        content += "" + sel_data_(dt, "param_Skin_Exterior_Abnormal_Desquamation_position_other").Replace("|", ",");
                                }
                                content += "</item>";
                            }
                            else if (type[i] == "潮濕")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Moist_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Exterior_Abnormal_Moist_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Moist_position_other") != "")
                                        content += "" + sel_data_(dt, "param_Skin_Exterior_Abnormal_Moist_position_other").Replace("|", ",");
                                }
                                content += "</item>";
                            }
                            else if (type[i] == "水泡")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Blister_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Exterior_Abnormal_Blister_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Blister_position_other") != "")
                                        content += "" + sel_data_(dt, "param_Skin_Exterior_Abnormal_Blister_position_other").Replace("|", ",");
                                } content += "</item>";
                            }
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Skin_Wound") != "")
                {
                    content += "<item>傷口：" + sel_data_(dt, "param_Skin_Wound");
                    if (sel_data_(dt, "param_Skin_Wound_Abnormal") != "")
                    {
                        string[] type = sel_data_(dt, "param_Skin_Wound_Abnormal").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "壓瘡" || type[i] == "壓傷")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Wound_Abnormal_Pressure_sores_position") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Wound_Abnormal_Pressure_sores_position");
                                content += "</item>";
                            }
                            else if (type[i] == "手術")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Wound_Abnormal_Surgery_position") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Wound_Abnormal_Surgery_position");
                                content += "</item>";
                            }
                            else if (type[i] == "外傷")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Wound_Abnormal_Trauma_position") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Wound_Abnormal_Trauma_position");
                                content += "</item>";
                            }
                            else if (type[i] == "燙傷")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Wound_Abnormal_Scald_position") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Wound_Abnormal_Scald_position");
                                content += "</item>";
                            }
                            else if (type[i] == "蜂窩性組織炎")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Wound_Abnormal_Cellulitis_position") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Wound_Abnormal_Cellulitis_position");
                                content += "</item>";
                            }
                            else if (type[i] == "糖尿病足(DM Foot)")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Wound_Abnormal_DM_Foot_position") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Wound_Abnormal_DM_Foot_position");
                                content += "</item>";
                            }
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #region 心肺系統
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-5\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"心肺系統\" />";
                content += "<title>心肺系統</title><text><list>";

                if (sel_data_(dt, "param_Breathing_Type") != "")
                {
                    content += "<item>呼吸型態：" + sel_data_(dt, "param_Breathing_Type");
                    if (sel_data_(dt, "param_Breathing_Type_Abnormal") != "")
                        content += "，" + sel_data_(dt, "param_Breathing_Type_Abnormal") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_Breathing_LeftVoive") != "")
                {
                    content += "<item>呼吸音-左側：" + sel_data_(dt, "param_Breathing_LeftVoive");
                    if (sel_data_(dt, "param_Breathing_LeftVoive_Abnormal") != "")
                        content += "，" + sel_data_(dt, "param_Breathing_LeftVoive_Abnormal") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_Breathing_RightVoive") != "")
                {
                    content += "<item>呼吸音-右側：" + sel_data_(dt, "param_Breathing_RightVoive");
                    if (sel_data_(dt, "param_Breathing_RightVoive_Abnormal") != "")
                        content += "，" + sel_data_(dt, "param_Breathing_RightVoive_Abnormal") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_Breathing_Treatment") != "")
                {
                    content += "<item>氧氣治療：" + sel_data_(dt, "param_Breathing_Treatment");
                    if (sel_data_(dt, "param_Breathing_Treatment_Abnormal") != "")
                        content += "，" + sel_data_(dt, "param_Breathing_Treatment_Abnormal");
                    if (sel_data_(dt, "param_Breathing_Treatment_Abnormal_Mask_1") != "")
                        content += "，" + sel_data_(dt, "param_Breathing_Treatment_Abnormal_Mask_1").Replace("|", ",") + "L/min";
                    if (sel_data_(dt, "param_Breathing_Treatment_Abnormal_Mask_2") != "")
                        content += "，" + sel_data_(dt, "param_Breathing_Treatment_Abnormal_Mask_2").Replace("|", ",") + "%";
                    if (sel_data_(dt, "param_Breathing_Treatment_Abnormal_Desc") != "")
                        content += " : " + sel_data_(dt, "param_Breathing_Treatment_Abnormal_Desc").Replace("|", ",");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Sputum_Amount") != "")
                {
                    content += "<item>痰液量：" + sel_data_(dt, "param_Sputum_Amount");
                    if (sel_data_(dt, "param_Sputum_Amount_Option") != "")
                        content += " : " + sel_data_(dt, "param_Sputum_Amount_Option");
                    if (sel_data_(dt, "param_Sputum_Amount_Color") != "")
                    {
                        content += "，痰液色：" + sel_data_(dt, "param_Sputum_Amount_Color");
                        if (sel_data_(dt, "param_Sputum_Amount_Color_other") != "")
                            content += " : " + sel_data_(dt, "param_Sputum_Amount_Color_other").Replace("|", ",");
                    }
                    if (sel_data_(dt, "param_Sputum_Amount_Type") != "")
                        content += "，痰液性質：" + sel_data_(dt, "param_Sputum_Amount_Type");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Heart_Rhythm") != "")
                {
                    content += "<item>心跳節律：" + sel_data_(dt, "param_Heart_Rhythm");
                    if (sel_data_(dt, "param_Heart_Rhythm_Name") != "")
                        content += " : " + sel_data_(dt, "param_Heart_Rhythm_Name").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_Heart_Symptoms") != "")
                {
                    content += "<item>病徵：" + sel_data_(dt, "param_Heart_Symptoms");
                    if (sel_data_(dt, "param_Heart_Symptoms_Dtl") != "")
                    {
                        content += "，" + sel_data_(dt, "param_Heart_Symptoms_Dtl").Replace("其他", "");
                        if (sel_data_(dt, "param_Heart_Symptoms_Dtl_Other") != "")
                            content += " : " + sel_data_(dt, "param_Heart_Symptoms_Dtl_Other").Replace("|", ",");
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_LeftFoot_Artery_Strength") != "" || sel_data_(dt, "param_RightFoot_Artery_Strength") != "")
                {
                    content += "<item>足背動脈強度<list>";
                    if (sel_data_(dt, "param_LeftFoot_Artery_Strength") != "")
                        content += "<item>左：" + sel_data_(dt, "param_LeftFoot_Artery_Strength") + "</item>";
                    if (sel_data_(dt, "param_RightFoot_Artery_Strength") != "")
                        content += "<item>右：" + sel_data_(dt, "param_RightFoot_Artery_Strength") + "</item>";
                    content += "</list></item>";
                }
                if (sel_data_(dt, "param_Tip") != "")
                {
                    content += "<item>末梢：" + sel_data_(dt, "param_Tip");
                    if (sel_data_(dt, "param_Tip_Abnormal_LeftTop") != "" || sel_data_(dt, "param_Tip_Abnormal_RightTop") != "" || sel_data_(dt, "param_Tip_Abnormal_LeftDown") != "" || sel_data_(dt, "param_Tip_Abnormal_RightDown") != "")
                    {
                        content += "<list>";
                        if (sel_data_(dt, "param_Tip_Abnormal_LeftTop") != "")
                            content += "<item>左上肢：" + sel_data_(dt, "param_Tip_Abnormal_LeftTop") + "</item>";
                        if (sel_data_(dt, "param_Tip_Abnormal_RightTop") != "")
                            content += "<item>右上肢：" + sel_data_(dt, "param_Tip_Abnormal_RightTop") + "</item>";
                        if (sel_data_(dt, "param_Tip_Abnormal_LeftDown") != "")
                            content += "<item>左下肢：" + sel_data_(dt, "param_Tip_Abnormal_LeftDown") + "</item>";
                        if (sel_data_(dt, "param_Tip_Abnormal_RightDown") != "")
                            content += "<item>右下肢：" + sel_data_(dt, "param_Tip_Abnormal_RightDown") + "</item>";
                        content += "</list>";
                    }
                    content += "</item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #region 泌尿系統
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-6\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"泌尿系統\" />";
                content += "<title>泌尿系統</title><text><list>";

                if (sel_data_(dt, "param_Voiding_Way") != "")
                    content += "<item>排尿方式：" + sel_data_(dt, "param_Voiding_Way") + "</item>";
                if (sel_data_(dt, "param_Voiding_Type") != "")
                {
                    content += "<item>排尿型態：" + sel_data_(dt, "param_Voiding_Type");
                    if (sel_data_(dt, "param_Voiding_Type_Desc") != "")
                        content += "，" + sel_data_(dt, "param_Voiding_Type_Desc") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_Voiding_Characters") != "")
                {
                    content += "<item>排尿性狀：" + sel_data_(dt, "param_Voiding_Characters");
                    if (sel_data_(dt, "param_Voiding_Characters_Desc") != "")
                    {
                        content += "，" + sel_data_(dt, "param_Voiding_Characters_Desc").Replace("其他", "");
                        if (sel_data_(dt, "param_Voiding_Characters_Desc_other") != "")
                            content += " : " + sel_data_(dt, "param_Voiding_Characters_Desc_other").Replace("|", ",");
                    }
                    content += "</item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #region 腸胃及營養評估
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-7\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"腸胃及營養評估\" />";
                content += "<title>腸胃及營養評估</title><text><list>";

                if (sel_data_(dt, "param_FoodKind") != "")
                {
                    content += "<item>飲食種類：" + sel_data_(dt, "param_FoodKind");
                    if (sel_data_(dt, "param_FoodKind_Tube") != "")
                        content += "，" + sel_data_(dt, "param_FoodKind_Tube");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Appetite") != "")
                    content += "<item>食慾：" + sel_data_(dt, "param_Appetite") + "</item>";
                if (sel_data_(dt, "param_Chew") != "")
                    content += "<item>咀嚼：" + sel_data_(dt, "param_Chew") + "</item>";
                if (sel_data_(dt, "param_Swallowing") != "")
                {
                    content += "<item>吞嚥：" + sel_data_(dt, "param_Swallowing");
                    if (sel_data_(dt, "param_SwallowingStatus") != "")
                        content += "，" + sel_data_(dt, "param_SwallowingStatus");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Eating") != "")
                {
                    content += "<item>進食方式：" + sel_data_(dt, "param_Eating");
                    if (sel_data_(dt, "param_EatingStatus1") != "")
                        content += "，" + sel_data_(dt, "param_EatingStatus1");
                    if (sel_data_(dt, "param_EatingStatus2") != "")
                    {
                        string[] type = sel_data_(dt, "param_EatingStatus2").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "全靜脈營養(TPN)")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_EatingStatus2_1") != "")
                                    content += "：" + sel_data_(dt, "param_EatingStatus2_1").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "周邊靜脈營養(PPN)")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_EatingStatus2_2") != "")
                                    content += "：" + sel_data_(dt, "param_EatingStatus2_2").Replace("|", ",");
                                content += "</item>";
                            }
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Peristalsis") != "")
                {
                    content += "<item>腸蠕動：" + sel_data_(dt, "param_Peristalsis");
                    if (sel_data_(dt, "param_PeristalsisStatus") != "")
                        content += "，" + sel_data_(dt, "param_PeristalsisStatus");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Gastrointestinal") != "")
                {
                    content += "<item>腸胃症狀：" + sel_data_(dt, "param_Gastrointestinal");
                    if (sel_data_(dt, "param_GastrointestinalStatus_1") != "")
                        content += "，嘔吐" + sel_data_(dt, "param_GastrointestinalStatus_1") + "天";
                    if (sel_data_(dt, "param_GastrointestinalStatus_2") != "")
                        content += "，噁心" + sel_data_(dt, "param_GastrointestinalStatus_2") + "天";
                    if (sel_data_(dt, "param_GastrointestinalStatus_3") != "")
                        content += "，腹脹" + sel_data_(dt, "param_GastrointestinalStatus_3") + "天";
                    if (sel_data_(dt, "param_GastrointestinalStatus_4") != "")
                        content += "，腹瀉" + sel_data_(dt, "param_GastrointestinalStatus_4") + "天";
                    if (sel_data_(dt, "param_GastrointestinalStatus_5") != "")
                        content += "，厭食" + sel_data_(dt, "param_GastrointestinalStatus_5") + "天";
                    if (sel_data_(dt, "param_GastrointestinalStatus_6") != "" || sel_data_(dt, "param_GastrointestinalStatus_reason") != "")
                        content += "，其他" + sel_data_(dt, "param_GastrointestinalStatus_reason")+"：" + sel_data_(dt, "param_GastrointestinalStatus_6") + "天";
                    content += "</item>";
                }
                if (sel_data_(dt, "param_StoolStatus") != "")
                {
                    content += "<item>排便：" + sel_data_(dt, "param_StoolStatus");
                    if (sel_data_(dt, "param_StoolCount") != "")
                        content += "，" + sel_data_(dt, "param_StoolCount") + "次/";
                    if (sel_data_(dt, "param_StoolSequence") != "")
                        content += sel_data_(dt, "param_StoolSequence") + "日";
                    if (sel_data_(dt, "param_StoolAbnormal") != "")
                        content += "，" + sel_data_(dt, "param_StoolAbnormal");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_BodyHeight") != "" || sel_data_(dt, "param_BodyWeight") != "" || sel_data_(dt, "param_BWDown") != "" || sel_data_(dt, "param_FoodUnit") != "")
                {
                    content += "<item>營養評估<list>";
                    if (sel_data_(dt, "param_BodyHeight") != "")
                        content += "<item>身高：" + sel_data_(dt, "param_BodyHeight") + "公分</item>";
                    if (sel_data_(dt, "param_BodyWeight") != "")
                        content += "<item>體重：" + sel_data_(dt, "param_BodyWeight") + "公斤</item>";
                    if (sel_data_(dt, "param_BodyHeight") != "" && sel_data_(dt, "param_BodyWeight") != "")
                    {

                        try
                        {
                            float BMI = float.Parse(sel_data_(dt, "param_BodyWeight")) / (float.Parse(sel_data_(dt, "param_BodyHeight")) * float.Parse(sel_data_(dt, "param_BodyHeight")) / 10000);
                            content += "<item>身體質量指數(BMI)：" + Math.Round(BMI, 1, MidpointRounding.AwayFromZero).ToString() + "</item>";
                        }
                        catch (Exception)
                        { }
                    }
                    if (sel_data_(dt, "param_BWDown") != "")
                        content += "<item>半年內體重下降6Kg：" + sel_data_(dt, "param_BWDown") + "</item>";
                    if (sel_data_(dt, "param_FoodUnit") != "")
                        content += "<item>食物攝取量：" + sel_data_(dt, "param_FoodUnit") + "</item>";
                    content += "</list></item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #region 肌肉骨骼系統
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-8\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"肌肉骨骼系統\" />";
                content += "<title>肌肉骨骼系統</title><text><list>";

                if (sel_data_(dt, "param_RULimb") != "" || sel_data_(dt, "param_RDLimb") != "" || sel_data_(dt, "param_LULimb") != "" || sel_data_(dt, "param_LDLimb") != "")
                {
                    content += "<item>肌力<list>";
                    if (sel_data_(dt, "param_RULimb") != "")
                        content += "<item>右上肢：" + sel_data_(dt, "param_RULimb") + "</item>";
                    if (sel_data_(dt, "param_RDLimb") != "")
                        content += "<item>右下肢：" + sel_data_(dt, "param_RDLimb") + "</item>";
                    if (sel_data_(dt, "param_LULimb") != "")
                        content += "<item>左上肢：" + sel_data_(dt, "param_LULimb") + "</item>";
                    if (sel_data_(dt, "param_LDLimb") != "")
                        content += "<item>左下肢：" + sel_data_(dt, "param_LDLimb") + "</item>";
                    content += "</list></item>";
                }
                if (sel_data_(dt, "param_ActiveStatus") != "")
                {
                    content += "<item>活動情形：" + sel_data_(dt, "param_ActiveStatus");
                    if (sel_data_(dt, "param_UnActiveDesc") != "")
                        content += "，" + sel_data_(dt, "param_UnActiveDesc");
                    if (sel_data_(dt, "param_ActiveDesc") != "")
                        content += "，" + sel_data_(dt, "param_ActiveDesc");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Eat") != "" || sel_data_(dt, "param_Dressing") != "" || sel_data_(dt, "param_Bathing") != "" || sel_data_(dt, "param_Toilet") != "" || sel_data_(dt, "param_Sport") != "")
                {
                    content += "<item>自我照護能力 (ADL)<list>";
                    if (sel_data_(dt, "param_Eat") != "")
                        content += "<item>進食：" + sel_data_(dt, "param_Eat") + "</item>";
                    if (sel_data_(dt, "param_Dressing") != "")
                        content += "<item>穿衣：" + sel_data_(dt, "param_Dressing") + "</item>";
                    if (sel_data_(dt, "param_Bathing") != "")
                        content += "<item>沐浴：" + sel_data_(dt, "param_Bathing") + "</item>";
                    if (sel_data_(dt, "param_Toilet") != "")
                        content += "<item>如廁：" + sel_data_(dt, "param_Toilet") + "</item>";
                    if (sel_data_(dt, "param_Sport") != "")
                        content += "<item>一般運動：" + sel_data_(dt, "param_Sport") + "</item>";
                    content += "</list></item>";
                }
                if (sel_data_(dt, "param_MotionRange") != "")
                {
                    content += "<item>關節活動度：" + sel_data_(dt, "param_MotionRange");
                    if (sel_data_(dt, "param_MotionAbnormalDesc") != "")
                    {
                        string[] type = sel_data_(dt, "param_MotionAbnormalDesc").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "肢體攣縮")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_MotionAbnormalDesc_1_name") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_MotionAbnormalDesc_1_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "關節腫脹")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_MotionAbnormalDesc_2_name") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_MotionAbnormalDesc_2_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "關節變形")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_MotionAbnormalDesc_3_name") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_MotionAbnormalDesc_3_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "晨間僵硬")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_MotionAbnormalDesc_4_name") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_MotionAbnormalDesc_4_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "關節疼痛")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_MotionAbnormalDesc_5_name") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_MotionAbnormalDesc_5_name").Replace("|", ",");
                                content += "</item>";
                            }
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #region 產科史
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-9\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"產科史\" />";
                content += "<title>產科史</title><text><list>";

                if (sel_data_(dt, "param_Gynecology") != "")
                {
                    content += "<item>產科史：" + sel_data_(dt, "param_Gynecology") + "</item>";
                    if (sel_data_(dt, "param_Gynecology") == "女")
                    {
                        if (sel_data_(dt, "param_MC") != "")
                        {
                            content += "<item>月經：" + sel_data_(dt, "param_MC");
                            if (sel_data_(dt, "param_MC") == "已停經" && sel_data_(dt, "param_MCEnd") != "")
                                content += "，停經：" + sel_data_(dt, "param_MCEnd") + "歲";
                            if (sel_data_(dt, "param_MC") == "未停經")
                            {
                                content += "<list>";
                                if (sel_data_(dt, "param_MCStart") != "")
                                    content += "<item>初經年齡：" + sel_data_(dt, "param_MCStart") + "歲</item>";
                                if (sel_data_(dt, "param_Last_MC") != "")
                                    content += "<item>最後月經日：" + sel_data_(dt, "param_Last_MC") + "</item>";
                                if (sel_data_(dt, "param_MCCycle_rule") != "")
                                {
                                    content += "<item>月經週期：" + sel_data_(dt, "param_MCCycle_rule");
                                    if (sel_data_(dt, "param_MCCycle_rule_day") != "")
                                        content += "，" + sel_data_(dt, "param_MCCycle_rule_day") + "天";
                                    content += "</item>";
                                }
                                if (sel_data_(dt, "param_MCDay") != "")
                                    content += "<item>月經天數：" + sel_data_(dt, "param_MCDay") + "天</item>";
                                if (sel_data_(dt, "param_MCAmount") != "")
                                    content += "<item>月經量：" + sel_data_(dt, "param_MCAmount") + "</item>";
                                if (sel_data_(dt, "param_FBAbnormalDtl") != "")
                                {
                                    content += "<item>月經期間：" + sel_data_(dt, "param_FBAbnormalDtl").Replace("其他", "");
                                    if (sel_data_(dt, "param_FBAbnormalOther") != "")
                                        content += sel_data_(dt, "param_FBAbnormalOther").Replace("|", ",");
                                    content += "</item>";
                                }
                                content += "</list>";
                            }
                            content += "</item>";
                        }
                        if (sel_data_(dt, "param_SelfCheck_Breast") != "")
                            content += "<item>乳房自我檢查：" + sel_data_(dt, "param_SelfCheck_Breast") + "</item>";
                        if (sel_data_(dt, "param_SelfCheck_Vagina") != "")
                        {
                            content += "<item>陰道抹片檢查：" + sel_data_(dt, "param_SelfCheck_Vagina");
                            if (sel_data_(dt, "param_SelfCheck_Vagina_Date") != "")
                                content += "，最後一次檢查日期：" + sel_data_(dt, "param_SelfCheck_Vagina_Date");
                            content += "</item>";
                        }
                        if (sel_data_(dt, "param_BornHistory") != "")
                        {
                            content += "<item>生產史：" + sel_data_(dt, "param_BornHistory");
                            if (sel_data_(dt, "param_BornHistoryNL") != "" || sel_data_(dt, "param_BornHistoryND") != "" || sel_data_(dt, "param_BornHistoryHL") != "" || sel_data_(dt, "param_BornHistoryHD") != "")
                            {
                                content += "<list>";
                                if (sel_data_(dt, "param_BornHistoryNL") != "")
                                    content += "<item>自然產，活產數：" + sel_data_(dt, "param_BornHistoryNL") + "人</item>";
                                if (sel_data_(dt, "param_BornHistoryND") != "")
                                    content += "<item>自然產，死產數(懷孕20週以上)：" + sel_data_(dt, "param_BornHistoryND") + "人</item>";
                                if (sel_data_(dt, "param_BornHistoryHL") != "")
                                    content += "<item>剖腹產，活產數：" + sel_data_(dt, "param_BornHistoryHL") + "人</item>";
                                if (sel_data_(dt, "param_BornHistoryHD") != "")
                                    content += "<item>剖腹產，死產數(懷孕20週以上)：" + sel_data_(dt, "param_BornHistoryHD") + "人</item>";
                                content += "</list>";
                            }
                            content += "</item>";
                        }
                        if (sel_data_(dt, "param_AbortionHistory") != "")
                        {
                            content += "<item>流產史(懷孕週數小於20週)：" + sel_data_(dt, "param_AbortionHistory");
                            if (sel_data_(dt, "param_AbortionN") != "" || sel_data_(dt, "param_AbortionH") != "")
                            {
                                content += "<list>";
                                if (sel_data_(dt, "param_AbortionN") != "")
                                    content += "<item>自然流產：" + sel_data_(dt, "param_AbortionN") + "人</item>";
                                if (sel_data_(dt, "param_AbortionH") != "")
                                    content += "<item>人工流產：" + sel_data_(dt, "param_AbortionH") + "次</item>";
                                content += "</list>";
                            }
                            content += "</item>";
                        }
                        if (sel_data_(dt, "param_Contraception") != "")
                        {
                            content += "<item>避孕(懷孕週數小於20週)：" + sel_data_(dt, "param_Contraception");
                            if (sel_data_(dt, "param_ContraceptionDesc") != "")
                            {
                                content += "，" + sel_data_(dt, "param_ContraceptionDesc").Replace("其他", "");
                                if (sel_data_(dt, "param_ContraceptionDesc_other") != "")
                                    content += sel_data_(dt, "param_ContraceptionDesc_other").Replace("|", ",");
                            }
                            content += "</item>";
                        }
                    }
                }

                content += "</list></text></section></component>";
                #endregion

                #region 疼痛評估
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-10\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"疼痛評估\" />";
                content += "<title>疼痛評估</title><text><list>";

                if (sel_data_(dt, "param_Awareness") != "")
                    content += "<item>目前意識狀態：" + sel_data_(dt, "param_Awareness") + "</item>";
                if (sel_data_(dt, "param_PainScale") != "")
                {
                    content += "<item>疼痛：" + sel_data_(dt, "param_PainScale");
                    if (sel_data_(dt, "param_PainSt") != "" || sel_data_(dt, "param_PainSDBrief") != "" || sel_data_(dt, "param_PainSDLang") != "" || sel_data_(dt, "param_PainSDFace") != "" || sel_data_(dt, "param_PainSDBodyLang") != "" || sel_data_(dt, "param_PainSDPiece") != "" || sel_data_(dt, "param_PainSDScole") != "")
                    {
                        content += "<list>";
                        if (sel_data_(dt, "param_PainSt") != "")
                            content += "<item>疼痛強度：" + sel_data_(dt, "param_PainSt").Substring(1, sel_data_(dt, "param_PainSt").IndexOf(")") - 1) + "分</item>";
                        if (sel_data_(dt, "param_PainSDBrief") != "")
                            content += "<item>呼吸：" + sel_data_(dt, "param_PainSDBrief") + "</item>";
                        if (sel_data_(dt, "param_PainSDLang") != "")
                            content += "<item>非言語表達：" + sel_data_(dt, "param_PainSDLang") + "</item>";
                        if (sel_data_(dt, "param_PainSDFace") != "")
                            content += "<item>臉部表情：" + sel_data_(dt, "param_PainSDFace") + "</item>";
                        if (sel_data_(dt, "param_PainSDBodyLang") != "")
                            content += "<item>肢體語言：" + sel_data_(dt, "param_PainSDBodyLang") + "</item>";
                        if (sel_data_(dt, "param_PainSDPiece") != "")
                            content += "<item>安撫：" + sel_data_(dt, "param_PainSDPiece") + "</item>";
                        if (sel_data_(dt, "param_PainSDScole") != "")
                            content += "<item>總分：" + sel_data_(dt, "param_PainSDScole") + "</item>";
                        content += "</list>";
                    }
                    content += "</item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #region 精神狀態評估
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-11\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"精神狀態評估\" />";
                content += "<title>精神狀態評估</title><text><list>";

                if (sel_data_(dt, "param_Exterior") != "")
                {
                    content += "<item>外觀：" + sel_data_(dt, "param_Exterior").Replace("其他", "");
                    if (sel_data_(dt, "param_Exterior_other") != "")
                        content += sel_data_(dt, "param_Exterior_other").Replace("|", ",");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Behavor") != "")
                {
                    content += "<item>行為：" + sel_data_(dt, "param_Behavor").Replace("其他", "");
                    if (sel_data_(dt, "param_Behavor_other") != "")
                        content += sel_data_(dt, "param_Behavor_other").Replace("|", ",");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_SportAMT") != "")
                {
                    content += "<item>活動量：" + sel_data_(dt, "param_SportAMT").Replace("其他", "");
                    if (sel_data_(dt, "param_SportAMT_other") != "")
                        content += sel_data_(dt, "param_SportAMT_other").Replace("|", ",");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_SLang") != "")
                {
                    content += "<item>語言：" + sel_data_(dt, "param_SLang");
                    if (sel_data_(dt, "param_SLang_Dtl") != "")
                    {
                        content += "，" + sel_data_(dt, "param_SLang_Dtl").Replace("其他", "");
                        if (sel_data_(dt, "param_SLang_Dtl_Other") != "")
                            content += sel_data_(dt, "param_SLang_Dtl_Other").Replace("|", ",");
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Emotion") != "")
                {
                    content += "<item>情緒：" + sel_data_(dt, "param_Emotion").Replace("其他", "");
                    if (sel_data_(dt, "param_Emotion_other") != "")
                        content += sel_data_(dt, "param_Emotion_other").Replace("|", ",");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Consciousness") != "")
                    content += "<item>意識狀態：" + sel_data_(dt, "param_Consciousness") + "</item>";
                if (sel_data_(dt, "param_Disorientation") != "")
                    content += "<item>定向感：" + sel_data_(dt, "param_Disorientation") + "</item>";
                if (sel_data_(dt, "param_Think") != "")
                {
                    content += "<item>思想：" + sel_data_(dt, "param_Think").Replace("其他", "");
                    if (sel_data_(dt, "param_Think_other") != "")
                        content += sel_data_(dt, "param_Think_other").Replace("|", ",");
                    content += "</item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #region 聯絡資料
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-12\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"聯絡資料\" />";
                content += "<title>聯絡資料</title><text><list>";

                if (sel_data_(dt, "param_EMGContact") != "")
                {
                    content += "<item>聯絡資料<list>";
                    string[] Name = sel_data_(dt, "param_EMGContact").Split(',');
                    string[] Role_other = sel_data_(dt, "param_ContactRole_other").Split(',');
                    string[] Phone_1 = sel_data_(dt, "param_EMGContact_1").Split(',');
                    string[] Phone_2 = sel_data_(dt, "param_EMGContact_2").Split(',');
                    string[] Phone_3 = sel_data_(dt, "param_EMGContact_3").Split(',');
                    for (int i = 0; i < Name.Length; i++)
                    {
                        content += "<item>緊急聯絡人姓名：" + Name[i].Replace("|", ",");
                        if (i == 0)
                            content += "，稱謂：" + sel_data_(dt, "param_ContactRole").Replace("其他", "") + " " + Role_other[i].Replace("|", ",");
                        else
                            content += "，稱謂：" + sel_data_(dt, "param_ContactRole_" + i.ToString()).Replace("其他", "") + " " + Role_other[i].Replace("|", ",");
                        content += "，連絡電話-公司：" + Phone_1[i];
                        content += "，連絡電話-住家：" + Phone_2[i];
                        content += "，連絡電話-手機：" + Phone_3[i];
                        content += "</item>";
                    }
                    content += "</list></item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #endregion
            }
            else if (natype == "Z")//精神出院
            {
                #region 設定內容

                #region 出院準備評估
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-0\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"出院準備評估\" />";
                content += "<title>出院準備評估</title><text><list>";

                if (sel_data_(dt, "param_tube_date") != "" && sel_data_(dt, "param_tube_time") != "")
                    content += "<item>評估日期：" + sel_data_(dt, "param_tube_date") + " " + sel_data_(dt, "param_tube_time") + "</item>";
                if (sel_data_(dt, "so_Type") != "")
                    content += "<item>出院方式：" + sel_data_(dt, "so_Type") + "</item>";
                if (sel_data_(dt, "so_Awareness") != "")
                    content += "<item>患者情況病識感：" + sel_data_(dt, "so_Awareness") + "</item>";
                if (sel_data_(dt, "so_Condition") != "")
                    content += "<item>病情：" + sel_data_(dt, "so_Condition") + "</item>";
                if (sel_data_(dt, "so_CPDCare") != "")
                    content += "<item>照顧特質：" + sel_data_(dt, "so_CPDCare") + "</item>";
                if (sel_data_(dt, "so_CPDResouce") != "")
                {
                    content += "<item>照顧資源：" + sel_data_(dt, "so_CPDResouce").Replace("其他", "");
                    if (sel_data_(dt, "so_CPDResouce_Other") != "")
                        content += sel_data_(dt, "so_CPDResouce_Other").Replace("|", ",");
                    content += "</item>";
                }
                if (sel_data_(dt, "so_CPDSpecial") != "")
                {
                    content += "<item>特殊照護：" + sel_data_(dt, "so_CPDSpecial");
                    if (sel_data_(dt, "so_CPDSpecial_Other") != "")
                        content += sel_data_(dt, "so_CPDSpecial_Other").Replace("|", ",");
                    content += "</item>";
                }
                if (sel_data_(dt, "so_Education") != "")
                    content += "<item>衛教指導：" + sel_data_(dt, "so_Education") + "</item>";
                if (sel_data_(dt, "so_Explanation") != "")
                    content += "<item>給予並說明：" + sel_data_(dt, "so_Explanation") + "</item>";
                if (sel_data_(dt, "so_Result") != "")
                    content += "<item>評值結果：" + sel_data_(dt, "so_Result") + "</item>";

                content += "</list></text></section></component>";

                #endregion

                #endregion
            }
            else if (natype == "G")//產科
            {
                #region 設定內容

                #region 基本資料
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-0\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"基本資料\" />";
                content += "<title>基本資料</title><text><list>";

                if (sel_data_(dt, "param_tube_date") != "" && sel_data_(dt, "param_tube_time") != "")
                    content += "<item>入院日期：" + sel_data_(dt, "param_tube_date") + " " + sel_data_(dt, "param_tube_time") + "</item>";
                if (sel_data_(dt, "param_ipd_source") != "")
                    content += "<item>入院來源：" + sel_data_(dt, "param_ipd_source") + "</item>";
                if (sel_data_(dt, "param_ipd_style") != "")
                {
                    content += "<item>入院方式：" + sel_data_(dt, "param_ipd_style").Replace("其他", "");
                    if (sel_data_(dt, "param_ipd_style_other") != "")
                        content += sel_data_(dt, "param_ipd_style_other").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_ipd_reason") != "")
                    content += "<item>入住原因：" + sel_data_(dt, "param_ipd_reason") + "</item>";
                if (sel_data_(dt, "param_parent_toget") != "")
                    content += "<item>親子同室：" + sel_data_(dt, "param_parent_toget") + "</item>";
                if (sel_data_(dt, "param_feed_style") != "")
                    content += "<item>哺乳方式：" + sel_data_(dt, "param_feed_style") + "</item>";
                if (sel_data_(dt, "param_education") != "")
                    content += "<item>教育：" + sel_data_(dt, "param_education") + "</item>";
                if (sel_data_(dt, "param_job") != "")
                {
                    content += "<item>職業：" + sel_data_(dt, "param_job").Replace("其他", "");
                    if (sel_data_(dt, "param_job_other") != "")
                        content += sel_data_(dt, "param_job_other").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_economy") != "")
                    content += "<item>經濟狀況：" + sel_data_(dt, "param_economy") + "</item>";
                if (sel_data_(dt, "param_payment") != "")
                    content += "<item>身分別：" + sel_data_(dt, "param_payment") + "</item>";
                if (sel_data_(dt, "param_religion") != "")
                {
                    content += "<item>宗教：" + sel_data_(dt, "param_religion").Replace("其他", "");
                    if (sel_data_(dt, "param_religion_other") != "")
                        content += sel_data_(dt, "param_religion_other").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_lang") != "")
                {
                    content += "<item>語言：" + sel_data_(dt, "param_lang").Replace("其他", "");
                    if (sel_data_(dt, "param_lang_other") != "")
                        content += sel_data_(dt, "param_lang_other").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_needtrans") != "")
                    content += "<item>需翻譯：" + sel_data_(dt, "param_needtrans") + "</item>";
                if (sel_data_(dt, "param_marrage") != "")
                    content += "<item>婚姻狀況：" + sel_data_(dt, "param_marrage") + "</item>";
                if (sel_data_(dt, "param_care") != "")
                {
                    content += "<item>主要照顧者：" + sel_data_(dt, "param_care").Replace("其他", "");
                    if (sel_data_(dt, "param_care_other") != "")
                        content += sel_data_(dt, "param_care_other").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_psychological") != "")
                {
                    content += "<item>心理狀況：" + sel_data_(dt, "param_psychological").Replace("其他", "");
                    if (sel_data_(dt, "param_psychological_other") != "")
                        content += sel_data_(dt, "param_psychological_other").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_child") != "")
                {
                    content += "<item>子女：" + sel_data_(dt, "param_child");
                    if (sel_data_(dt, "param_child_f") != "")
                        content += "，男" + sel_data_(dt, "param_child_f") + "人";
                    if (sel_data_(dt, "param_child_m") != "")
                        content += "，女" + sel_data_(dt, "param_child_m") + "人";
                    content += "</item>";
                }
                if (sel_data_(dt, "param_living") != "")
                {
                    content += "<item>居住方式：" + sel_data_(dt, "param_living");
                    if (sel_data_(dt, "param_living_other") != "")
                        content += " : " + sel_data_(dt, "param_living_other").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_Volunteer_Help") != "")
                {
                    content += "<item>需社工幫助：" + sel_data_(dt, "param_Volunteer_Help");
                    if (sel_data_(dt, "param_Volunteer_Help_Dtl") != "")
                        content += "，" + sel_data_(dt, "param_Volunteer_Help_Dtl").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_cigarette") != "")
                {
                    if (sel_data_(dt, "param_cigarette") == "1")
                        content += "<item>抽菸：無";
                    if (sel_data_(dt, "param_cigarette") == "2")
                        content += "<item>抽菸：有";
                    if (sel_data_(dt, "param_cigarette") == "3")
                        content += "<item>抽菸：戒菸";
                    if (sel_data_(dt, "param_cigarette_yes_amount") != "")
                        content += "，每日" + sel_data_(dt, "param_cigarette_yes_amount") + "支";
                    if (sel_data_(dt, "param_cigarette_yes_year") != "")
                        content += "，已抽" + sel_data_(dt, "param_cigarette_yes_year") + "年";
                    if (sel_data_(dt, "param_cigarette_agree_stop") != "")
                        content += "，有無戒菸意願：" + sel_data_(dt, "param_cigarette_agree_stop");
                    if (sel_data_(dt, "param_cigarette_stop_year") != "")
                        content += "，" + sel_data_(dt, "param_cigarette_stop_year") + "年";
                    content += "</item>";
                }
                if (sel_data_(dt, "param_drink") != "")
                {
                    content += "<item>喝酒：" + sel_data_(dt, "param_drink");
                    if (sel_data_(dt, "param_drink_day") != "")
                        content += "，每日劑量" + sel_data_(dt, "param_drink_day") + "瓶";
                    if (sel_data_(dt, "param_drink_unit") != "")
                        content += " " + sel_data_(dt, "param_drink_unit") + "mL";
                    content += "</item>";
                }
                content += "</list></text></section></component>";

                #endregion

                #region 過去病史
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-1\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"過去病史\" />";
                content += "<title>過去病史</title><text><list>";

                if (sel_data_(dt, "param_hasipd") != "")
                {
                    content += "<item>住院次數：" + sel_data_(dt, "param_hasipd");
                    if (sel_data_(dt, "param_ipd_count") != "")
                        content += "，" + sel_data_(dt, "param_ipd_count");
                    if (sel_data_(dt, "param_ipd_lasttime") != "")
                        content += "，最近一次時間" + sel_data_(dt, "param_ipd_lasttime");
                    if (sel_data_(dt, "param_ipd_diag") != "")
                        content += "，診斷" + sel_data_(dt, "param_ipd_diag").Replace("|", ",");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_im_history") != "")
                {
                    content += "<item>內科病史：" + sel_data_(dt, "param_im_history");
                    if (sel_data_(dt, "param_im_history_item1") != "")
                        content += "，高血壓" + sel_data_(dt, "param_im_history_item1") + "年";
                    if (sel_data_(dt, "param_im_history_item2") != "")
                        content += "，心臟病" + sel_data_(dt, "param_im_history_item2") + "年";
                    if (sel_data_(dt, "param_im_history_item3") != "")
                        content += "，糖尿病" + sel_data_(dt, "param_im_history_item3") + "年";
                    if (sel_data_(dt, "param_im_history_item4") != "")
                        content += "，氣喘" + sel_data_(dt, "param_im_history_item4") + "年";
                    if (sel_data_(dt, "param_im_history_item_other") != "")
                        content += "，其他疾病：" + sel_data_(dt, "param_im_history_item_other").Replace("其他", "");
                    if (sel_data_(dt, "param_im_history_item_other_txt") != "" && sel_data_(dt, "param_im_history_item_other").IndexOf("其他") > -1)
                        content += sel_data_(dt, "param_im_history_item_other_txt").Replace("|", ",");
                    if (sel_data_(dt, "param_im_history_status") != "")
                        content += "，疾病發生時間，處理情形及目前狀況：" + sel_data_(dt, "param_im_history_status").Replace("|", ",");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_su_history") != "")
                {
                    content += "<item>外科病史：" + sel_data_(dt, "param_su_history");
                    if (sel_data_(dt, "param_su_history_trauma_txt") != "")
                        content += "，外傷：" + sel_data_(dt, "param_su_history_trauma_txt").Replace("|", ",");
                    if (sel_data_(dt, "param_su_history_surgery_txt") != "")
                        content += "，手術：" + sel_data_(dt, "param_su_history_surgery_txt").Replace("|", ",");
                    if (sel_data_(dt, "param_su_history_other_txt") != "")
                        content += "，外傷/手術/外科疾病：" + sel_data_(dt, "param_su_history_other_txt").Replace("|", ",");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_pregnancy_history") != "")
                {
                    content += "<item>孕期病史：" + sel_data_(dt, "param_pregnancy_history");
                    if (sel_data_(dt, "param_pregnancy_history_dtl") != "")
                    {
                        content += "，" + sel_data_(dt, "param_pregnancy_history_dtl").Replace("其他", "");
                        if (sel_data_(dt, "param_pregnancy_history_dtl_other") != "")
                            content += sel_data_(dt, "param_pregnancy_history_dtl_other").Replace("|", ",");
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_pregnancy_handle") != "")
                {
                    content += "<item>孕期處置：" + sel_data_(dt, "param_pregnancy_handle");
                    if (sel_data_(dt, "param_pregnancy_handle_other") != "")
                        content += "，" + sel_data_(dt, "param_pregnancy_handle_other").Replace("|", ",");
                    if (sel_data_(dt, "param_pregnancy_med") != "")
                        content += "，" + sel_data_(dt, "param_pregnancy_med");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_other_history") != "")
                {
                    content += "<item>其他病史：" + sel_data_(dt, "param_other_history");
                    if (sel_data_(dt, "param_other_history_desc") != "")
                        content += "，" + sel_data_(dt, "param_other_history_desc").Replace("|", ",");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_med") != "")
                {
                    content += "<item>目前服用藥物：" + sel_data_(dt, "param_med");
                    if (sel_data_(dt, "param_med") == "有")
                    {
                        //取得目前用藥
                        List<DrugOrder> Drug_list = new List<DrugOrder>();
                        byte[] labfoByteCode = webService.GetOpdMed(feeno);
                        if (labfoByteCode != null)
                        {
                            string labJosnArr = CompressTool.DecompressString(labfoByteCode);
                            Drug_list = JsonConvert.DeserializeObject<List<DrugOrder>>(labJosnArr);
                            if (Drug_list.Count > 0)
                            {
                                content += "<list>";
                                for (int i = 0; i < Drug_list.Count; i++)
                                {
                                    content += "<item>門診用藥-藥物名稱：" + trans_special_code(Drug_list[i].DrugName);
                                    content += "，頻次：" + trans_special_code(Drug_list[i].Feq);
                                    content += "，劑量：" + trans_special_code(Drug_list[i].Dose.ToString());
                                    content += "，途徑：" + trans_special_code(Drug_list[i].Route) + "</item>";
                                }
                                content += "</list>";
                            }
                        }
                    }
                    if (sel_data_(dt, "param_med_name") != "")
                    {
                        content += "<list>";
                        string[] DrugName = sel_data_(dt, "param_med_name").Split(',');
                        string[] Feq = sel_data_(dt, "param_med_frequency").Split(',');
                        string[] Dose = sel_data_(dt, "param_med_amount").Split(',');
                        string[] Route = sel_data_(dt, "param_med_way").Split(',');
                        for (int i = 0; i < DrugName.Length; i++)
                        {
                            content += "<item>藥物名稱：" + DrugName[i].Replace("|", ",");
                            content += "，頻次：" + Feq[i].Replace("|", ",");
                            content += "，劑量：" + Dose[i].Replace("|", ",");
                            content += "，途徑：" + Route[i].Replace("|", ",") + "</item>";
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_allergy_med") != "")
                {
                    content += "<item>過敏史藥物：" + sel_data_(dt, "param_allergy_med");
                    if (sel_data_(dt, "param_allergy_med_other") != "")
                    {
                        string[] type = sel_data_(dt, "param_allergy_med_other").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "不詳")
                                content += "<item>不詳</item>";
                            else if (type[i] == "pyrin")
                            {
                                content += "<item>匹林系藥物(pyrin)";
                                if (sel_data_(dt, "param_allergy_med_other_2_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_med_other_2_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "aspirin")
                                content += "<item>水楊酸鹽類(包括aspirin)</item>";
                            else if (type[i] == "NSAID")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_med_other_4_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_med_other_4_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "磺氨類")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_med_other_6_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_med_other_6_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "盤尼西林類")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_med_other_7_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_med_other_7_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "抗生素類")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_med_other_8_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_med_other_8_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "麻醉藥")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_med_other_9_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_med_other_9_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "其他")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_med_other_10_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_med_other_10_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else
                                content += "<item>" + type[i] + "</item>";
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_allergy_food") != "")
                {
                    content += "<item>過敏史食物：" + sel_data_(dt, "param_allergy_food");
                    if (sel_data_(dt, "param_allergy_food_other") != "")
                    {
                        string[] type = sel_data_(dt, "param_allergy_food_other").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "海鮮類")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_food_other_2_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_food_other_2_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "水果")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_food_other_4_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_food_other_4_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "其他")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_food_other_6_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_food_other_6_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else
                                content += "<item>" + type[i] + "</item>";
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_allergy_other") != "")
                {
                    content += "<item>過敏史其他：" + sel_data_(dt, "param_allergy_other");
                    if (sel_data_(dt, "param_allergy_other_other") != "")
                    {
                        string[] type = sel_data_(dt, "param_allergy_other_other").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "不詳")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_other_other_1_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_other_other_1_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "輸血")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_other_other_2_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_other_other_2_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "油漆")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_other_other_3_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_other_other_3_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "昆蟲")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_other_other_4_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_other_other_4_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i].IndexOf("麈") > -1)
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_other_other_5_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_other_other_5_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "其他")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_allergy_other_other_6_name") != "")
                                    content += "，名稱 : " + sel_data_(dt, "param_allergy_other_other_6_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else
                                content += "<item>" + type[i] + "</item>";
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }
                content += "</list></text></section></component>";
                #endregion

                #region 家族病史
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-2\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"家族病史\" />";
                content += "<title>家族病史</title><text><list>";

                if (sel_data_(dt, "param_family_history") != "")
                    content += "<item>家族病史：" + sel_data_(dt, "param_family_history") + "</item>";
                if (sel_data_(dt, "param_bp") != "")
                    content += "<item>高血壓：" + sel_data_(dt, "param_bp") + "</item>";
                if (sel_data_(dt, "param_kind") != "")
                    content += "<item>腎臟病：" + sel_data_(dt, "param_kind") + "</item>";
                if (sel_data_(dt, "param_asthma") != "")
                    content += "<item>氣喘：" + sel_data_(dt, "param_asthma") + "</item>";
                if (sel_data_(dt, "param_epilepsy") != "")
                    content += "<item>癲癇：" + sel_data_(dt, "param_epilepsy") + "</item>";
                if (sel_data_(dt, "param_HeartDisease") != "")
                    content += "<item>心臟病：" + sel_data_(dt, "param_HeartDisease") + "</item>";
                if (sel_data_(dt, "param_PepticUlcer") != "")
                    content += "<item>消化性潰瘍：" + sel_data_(dt, "param_PepticUlcer") + "</item>";
                if (sel_data_(dt, "param_tuberculosis") != "")
                    content += "<item>肺結核：" + sel_data_(dt, "param_tuberculosis") + "</item>";
                if (sel_data_(dt, "param_MentalIllness") != "")
                    content += "<item>精神病：" + sel_data_(dt, "param_MentalIllness") + "</item>";
                if (sel_data_(dt, "param_Diabetes") != "")
                    content += "<item>糖尿病：" + sel_data_(dt, "param_Diabetes") + "</item>";
                if (sel_data_(dt, "param_Cancer") != "")
                    content += "<item>癌症：" + sel_data_(dt, "param_Cancer") + "</item>";
                if (sel_data_(dt, "param_LiverDisease") != "")
                    content += "<item>肝臟疾病：" + sel_data_(dt, "param_LiverDisease") + "</item>";
                if (sel_data_(dt, "param_OtherDiseaseDesc") != "" || sel_data_(dt, "param_OtherDisease") != "")
                    content += "<item>其他疾病名稱：" + sel_data_(dt, "param_OtherDiseaseDesc").Replace("|", ",") + "，" + sel_data_(dt, "param_OtherDisease") + "</item>";

                content += "</list></text></section></component>";
                #endregion

                #region 一般外觀
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-3\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"一般外觀\" />";
                content += "<title>一般外觀</title><text><list>";

                if (sel_data_(dt, "param_LeftEye") != "")
                {
                    content += "<item>左眼視力：" + sel_data_(dt, "param_LeftEye").Replace("其他", "");
                    if (sel_data_(dt, "param_LeftEyeOther") != "")
                        content += sel_data_(dt, "param_LeftEyeOther").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_LeftEyeRedress") != "")
                {
                    content += "<item>左眼矯正：" + sel_data_(dt, "param_LeftEyeRedress").Replace("其他", "");
                    if (sel_data_(dt, "param_LeftEyeRedressDesc") != "")
                        content += sel_data_(dt, "param_LeftEyeRedressDesc").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_RightEye") != "")
                {
                    content += "<item>右眼視力：" + sel_data_(dt, "param_RightEye").Replace("其他", "");
                    if (sel_data_(dt, "param_RightEyeOther") != "")
                        content += sel_data_(dt, "param_RightEyeOther").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_RightEyeRedress") != "")
                {
                    content += "<item>右眼矯正：" + sel_data_(dt, "param_RightEyeRedress").Replace("其他", "");
                    if (sel_data_(dt, "param_RightEyeRedressDesc") != "")
                        content += sel_data_(dt, "param_RightEyeRedressDesc").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_LeftEra") != "")
                {
                    content += "<item>左耳聽力：" + sel_data_(dt, "param_LeftEra").Replace("其他", "");
                    if (sel_data_(dt, "param_LeftEraDesc") != "")
                        content += sel_data_(dt, "param_LeftEraDesc").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_RightEra") != "")
                {
                    content += "<item>右耳聽力：" + sel_data_(dt, "param_RightEra").Replace("其他", "");
                    if (sel_data_(dt, "param_RightEraDesc") != "")
                        content += " : " + sel_data_(dt, "param_RightEraDesc").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_Mouth") != "")
                {
                    content += "<item>口腔，黏膜：" + sel_data_(dt, "param_Mouth");
                    if (sel_data_(dt, "param_Mouth_Abnormal") != "")
                        content += "，" + sel_data_(dt, "param_Mouth_Abnormal") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_Tooth") != "")
                {
                    content += "<item>假牙：" + sel_data_(dt, "param_Tooth");
                    if (sel_data_(dt, "param_ToothUpDesc") != "")
                        content += "，上：" + sel_data_(dt, "param_ToothUpDesc");
                    if (sel_data_(dt, "param_ToothDownDesc") != "")
                        content += "，下：" + sel_data_(dt, "param_ToothDownDesc");
                    content += "</item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #region 皮膚狀況
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-4\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"皮膚狀況\" />";
                content += "<title>皮膚狀況</title><text><list>";

                if (sel_data_(dt, "param_Skin_Temp") != "")
                {
                    content += "<item>皮膚溫度：" + sel_data_(dt, "param_Skin_Temp");
                    if (sel_data_(dt, "param_Skin_Temp_Abnormal") != "")
                    {
                        string[] type = sel_data_(dt, "param_Skin_Temp_Abnormal").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "發熱")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Temp_Abnormal_Hot_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Temp_Abnormal_Hot_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Temp_Abnormal_Hot_position_other") != "")
                                        content += " : " + sel_data_(dt, "param_Skin_Temp_Abnormal_Hot_position_other").Replace("|", ",");
                                }
                                content += "</item>";
                            }
                            else if (type[i] == "冰冷")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Temp_Abnormal_Cold_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Temp_Abnormal_Cold_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Temp_Abnormal_Cold_position_other") != "")
                                        content += " : " + sel_data_(dt, "param_Skin_Temp_Abnormal_Cold_position_other").Replace("|", ",");
                                }
                                content += "</item>";
                            }
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Skin_Exterior") != "")
                {
                    content += "<item>皮膚外觀：" + sel_data_(dt, "param_Skin_Exterior");
                    if (sel_data_(dt, "param_Skin_Exterior_Abnormal") != "")
                    {
                        string[] type = sel_data_(dt, "param_Skin_Exterior_Abnormal").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "蒼白")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Pale_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Exterior_Abnormal_Pale_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Pale_position_other") != "")
                                        content += "" + sel_data_(dt, "param_Skin_Exterior_Abnormal_Pale_position_other").Replace("|", ",");
                                }
                                content += "</item>";
                            }
                            else if (type[i] == "發紺")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Cyanotic_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Exterior_Abnormal_Cyanotic_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Cyanotic_position_other") != "")
                                        content += "" + sel_data_(dt, "param_Skin_Exterior_Abnormal_Cyanotic_position_other").Replace("|", ",");
                                }
                                content += "</item>";
                            }
                            else if (type[i] == "紅疹")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Rash_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Exterior_Abnormal_Rash_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Rash_position_other") != "")
                                        content += "" + sel_data_(dt, "param_Skin_Exterior_Abnormal_Rash_position_other").Replace("|", ",");
                                }
                                content += "</item>";
                            }
                            else if (type[i] == "黃疸")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Jaundice_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Exterior_Abnormal_Jaundice_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Jaundice_position_other") != "")
                                        content += "" + sel_data_(dt, "param_Skin_Exterior_Abnormal_Jaundice_position_other").Replace("|", ",");
                                }
                                content += "</item>";
                            }
                            else if (type[i] == "瘀青")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Bruises_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Exterior_Abnormal_Bruises_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Bruises_position_other") != "")
                                        content += "" + sel_data_(dt, "param_Skin_Exterior_Abnormal_Bruises_position_other").Replace("|", ",");
                                }
                                content += "</item>";
                            }
                            else if (type[i] == "脫屑")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Desquamation_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Exterior_Abnormal_Desquamation_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Desquamation_position_other") != "")
                                        content += "" + sel_data_(dt, "param_Skin_Exterior_Abnormal_Desquamation_position_other").Replace("|", ",");
                                }
                                content += "</item>";
                            }
                            else if (type[i] == "潮濕")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Moist_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Exterior_Abnormal_Moist_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Moist_position_other") != "")
                                        content += "" + sel_data_(dt, "param_Skin_Exterior_Abnormal_Moist_position_other").Replace("|", ",");
                                }
                                content += "</item>";
                            }
                            else if (type[i] == "水泡")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Blister_position") != "")
                                {
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Exterior_Abnormal_Blister_position").Replace("其他", "");
                                    if (sel_data_(dt, "param_Skin_Exterior_Abnormal_Blister_position_other") != "")
                                        content += "" + sel_data_(dt, "param_Skin_Exterior_Abnormal_Blister_position_other").Replace("|", ",");
                                } content += "</item>";
                            }
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Skin_Wound") != "")
                {
                    content += "<item>傷口：" + sel_data_(dt, "param_Skin_Wound");
                    if (sel_data_(dt, "param_Skin_Wound_Abnormal") != "")
                    {
                        string[] type = sel_data_(dt, "param_Skin_Wound_Abnormal").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "壓瘡" || type[i] == "壓傷")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Wound_Abnormal_Pressure_sores_position") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Wound_Abnormal_Pressure_sores_position");
                                content += "</item>";
                            }
                            else if (type[i] == "手術")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Wound_Abnormal_Surgery_position") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Wound_Abnormal_Surgery_position");
                                content += "</item>";
                            }
                            else if (type[i] == "外傷")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Wound_Abnormal_Trauma_position") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Wound_Abnormal_Trauma_position");
                                content += "</item>";
                            }
                            else if (type[i] == "燙傷")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Wound_Abnormal_Scald_position") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Wound_Abnormal_Scald_position");
                                content += "</item>";
                            }
                            else if (type[i] == "蜂窩性組織炎")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Wound_Abnormal_Cellulitis_position") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Wound_Abnormal_Cellulitis_position");
                                content += "</item>";
                            }
                            else if (type[i] == "糖尿病足(DM Foot)")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_Skin_Wound_Abnormal_DM_Foot_position") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_Skin_Wound_Abnormal_DM_Foot_position");
                                content += "</item>";
                            }
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #region 心肺系統
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-5\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"心肺系統\" />";
                content += "<title>心肺系統</title><text><list>";

                if (sel_data_(dt, "param_Breathing_Type") != "")
                {
                    content += "<item>呼吸型態：" + sel_data_(dt, "param_Breathing_Type");
                    if (sel_data_(dt, "param_Breathing_Type_Abnormal") != "")
                        content += "，" + sel_data_(dt, "param_Breathing_Type_Abnormal") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_Breathing_LeftVoive") != "")
                {
                    content += "<item>呼吸音-左側：" + sel_data_(dt, "param_Breathing_LeftVoive");
                    if (sel_data_(dt, "param_Breathing_LeftVoive_Abnormal") != "")
                        content += "，" + sel_data_(dt, "param_Breathing_LeftVoive_Abnormal") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_Breathing_RightVoive") != "")
                {
                    content += "<item>呼吸音-右側：" + sel_data_(dt, "param_Breathing_RightVoive");
                    if (sel_data_(dt, "param_Breathing_RightVoive_Abnormal") != "")
                        content += "，" + sel_data_(dt, "param_Breathing_RightVoive_Abnormal") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_Breathing_Treatment") != "")
                {
                    content += "<item>氧氣治療：" + sel_data_(dt, "param_Breathing_Treatment");
                    if (sel_data_(dt, "param_Breathing_Treatment_Abnormal") != "")
                        content += "，" + sel_data_(dt, "param_Breathing_Treatment_Abnormal");
                    if (sel_data_(dt, "param_Breathing_Treatment_Abnormal_Mask_1") != "")
                        content += "，" + sel_data_(dt, "param_Breathing_Treatment_Abnormal_Mask_1").Replace("|", ",") + "L/min";
                    if (sel_data_(dt, "param_Breathing_Treatment_Abnormal_Mask_2") != "")
                        content += "，" + sel_data_(dt, "param_Breathing_Treatment_Abnormal_Mask_2").Replace("|", ",") + "%";
                    if (sel_data_(dt, "param_Breathing_Treatment_Abnormal_Desc") != "")
                        content += " : " + sel_data_(dt, "param_Breathing_Treatment_Abnormal_Desc").Replace("|", ",");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Sputum_Amount") != "")
                {
                    content += "<item>痰液量：" + sel_data_(dt, "param_Sputum_Amount");
                    if (sel_data_(dt, "param_Sputum_Amount_Option") != "")
                        content += " : " + sel_data_(dt, "param_Sputum_Amount_Option");
                    if (sel_data_(dt, "param_Sputum_Amount_Color") != "")
                    {
                        content += "，痰液色：" + sel_data_(dt, "param_Sputum_Amount_Color");
                        if (sel_data_(dt, "param_Sputum_Amount_Color_other") != "")
                            content += " : " + sel_data_(dt, "param_Sputum_Amount_Color_other").Replace("|", ",");
                    }
                    if (sel_data_(dt, "param_Sputum_Amount_Type") != "")
                        content += "，痰液性質：" + sel_data_(dt, "param_Sputum_Amount_Type");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Heart_Rhythm") != "")
                {
                    content += "<item>心跳節律：" + sel_data_(dt, "param_Heart_Rhythm");
                    if (sel_data_(dt, "param_Heart_Rhythm_Name") != "")
                        content += " : " + sel_data_(dt, "param_Heart_Rhythm_Name").Replace("|", ",") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_Heart_Symptoms") != "")
                {
                    content += "<item>病徵：" + sel_data_(dt, "param_Heart_Symptoms");
                    if (sel_data_(dt, "param_Heart_Symptoms_Dtl") != "")
                    {
                        content += "，" + sel_data_(dt, "param_Heart_Symptoms_Dtl").Replace("其他", "");
                        if (sel_data_(dt, "param_Heart_Symptoms_Dtl_Other") != "")
                            content += " : " + sel_data_(dt, "param_Heart_Symptoms_Dtl_Other").Replace("|", ",");
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_LeftFoot_Artery_Strength") != "" || sel_data_(dt, "param_RightFoot_Artery_Strength") != "")
                {
                    content += "<item>足背動脈強度<list>";
                    if (sel_data_(dt, "param_LeftFoot_Artery_Strength") != "")
                        content += "<item>左：" + sel_data_(dt, "param_LeftFoot_Artery_Strength") + "</item>";
                    if (sel_data_(dt, "param_RightFoot_Artery_Strength") != "")
                        content += "<item>右：" + sel_data_(dt, "param_RightFoot_Artery_Strength") + "</item>";
                    content += "</list></item>";
                }
                if (sel_data_(dt, "param_Tip") != "")
                {
                    content += "<item>末梢：" + sel_data_(dt, "param_Tip");
                    if (sel_data_(dt, "param_Tip_Abnormal_LeftTop") != "" || sel_data_(dt, "param_Tip_Abnormal_RightTop") != "" || sel_data_(dt, "param_Tip_Abnormal_LeftDown") != "" || sel_data_(dt, "param_Tip_Abnormal_RightDown") != "")
                    {
                        content += "<list>";
                        if (sel_data_(dt, "param_Tip_Abnormal_LeftTop") != "")
                            content += "<item>左上肢：" + sel_data_(dt, "param_Tip_Abnormal_LeftTop") + "</item>";
                        if (sel_data_(dt, "param_Tip_Abnormal_RightTop") != "")
                            content += "<item>右上肢：" + sel_data_(dt, "param_Tip_Abnormal_RightTop") + "</item>";
                        if (sel_data_(dt, "param_Tip_Abnormal_LeftDown") != "")
                            content += "<item>左下肢：" + sel_data_(dt, "param_Tip_Abnormal_LeftDown") + "</item>";
                        if (sel_data_(dt, "param_Tip_Abnormal_RightDown") != "")
                            content += "<item>右下肢：" + sel_data_(dt, "param_Tip_Abnormal_RightDown") + "</item>";
                        content += "</list>";
                    }
                    content += "</item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #region 泌尿系統
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-6\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"泌尿系統\" />";
                content += "<title>泌尿系統</title><text><list>";

                if (sel_data_(dt, "param_Voiding_Way") != "")
                    content += "<item>排尿方式：" + sel_data_(dt, "param_Voiding_Way") + "</item>";
                if (sel_data_(dt, "param_Voiding_Type") != "")
                {
                    content += "<item>排尿型態：" + sel_data_(dt, "param_Voiding_Type");
                    if (sel_data_(dt, "param_Voiding_Type_Desc") != "")
                        content += "，" + sel_data_(dt, "param_Voiding_Type_Desc") + "</item>";
                    else
                        content += "</item>";
                }
                if (sel_data_(dt, "param_Voiding_Characters") != "")
                {
                    content += "<item>排尿性狀：" + sel_data_(dt, "param_Voiding_Characters");
                    if (sel_data_(dt, "param_Voiding_Characters_Desc") != "")
                    {
                        content += "，" + sel_data_(dt, "param_Voiding_Characters_Desc").Replace("其他", "");
                        if (sel_data_(dt, "param_Voiding_Characters_Desc_other") != "")
                            content += " : " + sel_data_(dt, "param_Voiding_Characters_Desc_other").Replace("|", ",");
                    }
                    content += "</item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #region 腸胃及營養評估
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-7\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"腸胃及營養評估\" />";
                content += "<title>腸胃及營養評估</title><text><list>";

                if (sel_data_(dt, "param_FoodKind") != "")
                {
                    content += "<item>飲食種類：" + sel_data_(dt, "param_FoodKind");
                    if (sel_data_(dt, "param_FoodKind_Tube") != "")
                        content += "，" + sel_data_(dt, "param_FoodKind_Tube");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Appetite") != "")
                    content += "<item>食慾：" + sel_data_(dt, "param_Appetite") + "</item>";
                if (sel_data_(dt, "param_Chew") != "")
                    content += "<item>咀嚼：" + sel_data_(dt, "param_Chew") + "</item>";
                if (sel_data_(dt, "param_Swallowing") != "")
                {
                    content += "<item>吞嚥：" + sel_data_(dt, "param_Swallowing");
                    if (sel_data_(dt, "param_SwallowingStatus") != "")
                        content += "，" + sel_data_(dt, "param_SwallowingStatus");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Eating") != "")
                {
                    content += "<item>進食方式：" + sel_data_(dt, "param_Eating");
                    if (sel_data_(dt, "param_EatingStatus1") != "")
                        content += "，" + sel_data_(dt, "param_EatingStatus1");
                    if (sel_data_(dt, "param_EatingStatus2") != "")
                    {
                        string[] type = sel_data_(dt, "param_EatingStatus2").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "全靜脈營養(TPN)")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_EatingStatus2_1") != "")
                                    content += "：" + sel_data_(dt, "param_EatingStatus2_1").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "周邊靜脈營養(PPN)")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_EatingStatus2_2") != "")
                                    content += "：" + sel_data_(dt, "param_EatingStatus2_2").Replace("|", ",");
                                content += "</item>";
                            }
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Peristalsis") != "")
                {
                    content += "<item>腸蠕動：" + sel_data_(dt, "param_Peristalsis");
                    if (sel_data_(dt, "param_PeristalsisStatus") != "")
                        content += "，" + sel_data_(dt, "param_PeristalsisStatus");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Gastrointestinal") != "")
                {
                    content += "<item>腸胃症狀：" + sel_data_(dt, "param_Gastrointestinal");
                    if (sel_data_(dt, "param_GastrointestinalStatus_1") != "")
                        content += "，嘔吐" + sel_data_(dt, "param_GastrointestinalStatus_1") + "天";
                    if (sel_data_(dt, "param_GastrointestinalStatus_2") != "")
                        content += "，噁心" + sel_data_(dt, "param_GastrointestinalStatus_2") + "天";
                    if (sel_data_(dt, "param_GastrointestinalStatus_3") != "")
                        content += "，腹脹" + sel_data_(dt, "param_GastrointestinalStatus_3") + "天";
                    if (sel_data_(dt, "param_GastrointestinalStatus_4") != "")
                        content += "，腹瀉" + sel_data_(dt, "param_GastrointestinalStatus_4") + "天";
                    if (sel_data_(dt, "param_GastrointestinalStatus_5") != "")
                        content += "，厭食" + sel_data_(dt, "param_GastrointestinalStatus_5") + "天";
                    if (sel_data_(dt, "param_GastrointestinalStatus_6") != "" || sel_data_(dt, "param_GastrointestinalStatus_reason") != "")
                        content += "，其他" + sel_data_(dt, "param_GastrointestinalStatus_reason") + "：" + sel_data_(dt, "param_GastrointestinalStatus_6") + "天";
                    content += "</item>";
                }
                if (sel_data_(dt, "param_StoolStatus") != "")
                {
                    content += "<item>排便：" + sel_data_(dt, "param_StoolStatus");
                    if (sel_data_(dt, "param_StoolCount") != "")
                        content += "，" + sel_data_(dt, "param_StoolCount") + "次/";
                    if (sel_data_(dt, "param_StoolSequence") != "")
                        content += sel_data_(dt, "param_StoolSequence") + "日";
                    if (sel_data_(dt, "param_StoolAbnormal") != "")
                        content += "，" + sel_data_(dt, "param_StoolAbnormal");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_BodyHeight") != "" || sel_data_(dt, "param_BodyWeight") != "" || sel_data_(dt, "param_BWDown") != "" || sel_data_(dt, "param_FoodUnit") != "")
                {
                    content += "<item>營養評估<list>";
                    if (sel_data_(dt, "param_BodyHeight") != "")
                        content += "<item>身高：" + sel_data_(dt, "param_BodyHeight") + "公分</item>";
                    if (sel_data_(dt, "param_BodyWeight") != "")
                        content += "<item>體重：" + sel_data_(dt, "param_BodyWeight") + "公斤</item>";
                    if (sel_data_(dt, "param_BodyHeight") != "" && sel_data_(dt, "param_BodyWeight") != "")
                    {
                        try
                        {
                            float BMI = float.Parse(sel_data_(dt, "param_BodyWeight")) / (float.Parse(sel_data_(dt, "param_BodyHeight")) * float.Parse(sel_data_(dt, "param_BodyHeight")) / 10000);
                            content += "<item>身體質量指數(BMI)：" + Math.Round(BMI, 1, MidpointRounding.AwayFromZero).ToString() + "</item>";
                        }
                        catch (Exception)
                        { }
                    }
                    if (sel_data_(dt, "param_BWDown") != "")
                        content += "<item>半年內體重下降6Kg：" + sel_data_(dt, "param_BWDown") + "</item>";
                    if (sel_data_(dt, "param_FoodUnit") != "")
                        content += "<item>食物攝取量：" + sel_data_(dt, "param_FoodUnit") + "</item>";
                    content += "</list></item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #region 肌肉骨骼系統
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-8\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"肌肉骨骼系統\" />";
                content += "<title>肌肉骨骼系統</title><text><list>";

                if (sel_data_(dt, "param_RULimb") != "" || sel_data_(dt, "param_RDLimb") != "" || sel_data_(dt, "param_LULimb") != "" || sel_data_(dt, "param_LDLimb") != "")
                {
                    content += "<item>肌力<list>";
                    if (sel_data_(dt, "param_RULimb") != "")
                        content += "<item>右上肢：" + sel_data_(dt, "param_RULimb") + "</item>";
                    if (sel_data_(dt, "param_RDLimb") != "")
                        content += "<item>右下肢：" + sel_data_(dt, "param_RDLimb") + "</item>";
                    if (sel_data_(dt, "param_LULimb") != "")
                        content += "<item>左上肢：" + sel_data_(dt, "param_LULimb") + "</item>";
                    if (sel_data_(dt, "param_LDLimb") != "")
                        content += "<item>左下肢：" + sel_data_(dt, "param_LDLimb") + "</item>";
                    content += "</list></item>";
                }
                if (sel_data_(dt, "param_ActiveStatus") != "")
                {
                    content += "<item>活動情形：" + sel_data_(dt, "param_ActiveStatus");
                    if (sel_data_(dt, "param_UnActiveDesc") != "")
                        content += "，" + sel_data_(dt, "param_UnActiveDesc");
                    if (sel_data_(dt, "param_ActiveDesc") != "")
                        content += "，" + sel_data_(dt, "param_ActiveDesc");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Eat") != "" || sel_data_(dt, "param_Dressing") != "" || sel_data_(dt, "param_Bathing") != "" || sel_data_(dt, "param_Toilet") != "" || sel_data_(dt, "param_Sport") != "")
                {
                    content += "<item>自我照護能力 (ADL)<list>";
                    if (sel_data_(dt, "param_Eat") != "")
                        content += "<item>進食：" + sel_data_(dt, "param_Eat") + "</item>";
                    if (sel_data_(dt, "param_Dressing") != "")
                        content += "<item>穿衣：" + sel_data_(dt, "param_Dressing") + "</item>";
                    if (sel_data_(dt, "param_Bathing") != "")
                        content += "<item>沐浴：" + sel_data_(dt, "param_Bathing") + "</item>";
                    if (sel_data_(dt, "param_Toilet") != "")
                        content += "<item>如廁：" + sel_data_(dt, "param_Toilet") + "</item>";
                    if (sel_data_(dt, "param_Sport") != "")
                        content += "<item>一般運動：" + sel_data_(dt, "param_Sport") + "</item>";
                    content += "</list></item>";
                }
                if (sel_data_(dt, "param_MotionRange") != "")
                {
                    content += "<item>關節活動度：" + sel_data_(dt, "param_MotionRange");
                    if (sel_data_(dt, "param_MotionAbnormalDesc") != "")
                    {
                        string[] type = sel_data_(dt, "param_MotionAbnormalDesc").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "肢體攣縮")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_MotionAbnormalDesc_1_name") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_MotionAbnormalDesc_1_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "關節腫脹")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_MotionAbnormalDesc_2_name") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_MotionAbnormalDesc_2_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "關節變形")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_MotionAbnormalDesc_3_name") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_MotionAbnormalDesc_3_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "晨間僵硬")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_MotionAbnormalDesc_4_name") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_MotionAbnormalDesc_4_name").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "關節疼痛")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "param_MotionAbnormalDesc_5_name") != "")
                                    content += "，部位 : " + sel_data_(dt, "param_MotionAbnormalDesc_5_name").Replace("|", ",");
                                content += "</item>";
                            }
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #region 神經系統
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-9\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"神經系統\" />";
                content += "<title>神經系統</title><text><list>";

                if (sel_data_(dt, "param_EyesReflection") != "" || sel_data_(dt, "param_LanguageReflection") != "" || sel_data_(dt, "param_SportReflection") != "")
                {
                    content += "<item>昏迷指標(GCS)<list>";
                    if (sel_data_(dt, "param_EyesReflection") != "")
                        content += "<item>睜眼反射(E)：" + sel_data_(dt, "param_EyesReflection") + "</item>";
                    if (sel_data_(dt, "param_LanguageReflection") != "")
                        content += "<item>語言反射(V)：" + sel_data_(dt, "param_LanguageReflection") + "</item>";
                    if (sel_data_(dt, "param_SportReflection") != "")
                        content += "<item>運動反射(M)：" + sel_data_(dt, "param_SportReflection") + "</item>";
                    content += "</list></item>";
                }
                if (sel_data_(dt, "param_Gait") != "")
                    content += "<item>步態：" + sel_data_(dt, "param_Gait") + "</item>";
                if (sel_data_(dt, "param_Talk") != "")
                {
                    content += "<item>語言表達：" + sel_data_(dt, "param_Talk");
                    if (sel_data_(dt, "param_UnTalkDesc") != "")
                    {
                        content += "，" + sel_data_(dt, "param_UnTalkDesc").Replace("其他", "");
                        if (sel_data_(dt, "param_UnTalkDesc_other") != "")
                            content += sel_data_(dt, "param_UnTalkDesc_other").Replace("|", ",");
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Cognitive") != "")
                {
                    content += "<item>認知感受：" + sel_data_(dt, "param_Cognitive");
                    if (sel_data_(dt, "param_CognitiveStatus") != "" || sel_data_(dt, "param_CognitivePain") != "" || sel_data_(dt, "param_CognitiveNoFeeling") != "" || sel_data_(dt, "param_CognitiveHemp") != "" || sel_data_(dt, "param_CognitivePumpedStorage") != "")
                    {
                        content += "<list>";
                        if (sel_data_(dt, "param_CognitiveStatus") != "")
                        {
                            string[] type = sel_data_(dt, "param_CognitiveStatus").Split(',');
                            for (int i = 0; i < type.Length; i++)
                                content += "<item>" + type[i] + "</item>";
                        }
                        if (sel_data_(dt, "param_CognitivePain") != "")
                        {
                            content += "<item>疼痛，部位：" + sel_data_(dt, "param_CognitivePain").Replace("其他", "");
                            if (sel_data_(dt, "param_CognitivePainOther") != "")
                                content += sel_data_(dt, "param_CognitivePainOther").Replace("|", ",");
                            content += "</item>";
                        }
                        if (sel_data_(dt, "param_CognitiveNoFeeling") != "")
                        {
                            content += "<item>無知覺，部位：" + sel_data_(dt, "param_CognitiveNoFeeling").Replace("其他", "");
                            if (sel_data_(dt, "param_CognitiveNoFeelingOther") != "")
                                content += sel_data_(dt, "param_CognitiveNoFeelingOther").Replace("|", ",");
                            content += "</item>";
                        }
                        if (sel_data_(dt, "param_CognitiveHemp") != "")
                        {
                            content += "<item>麻，部位：" + sel_data_(dt, "param_CognitiveHemp").Replace("其他", "");
                            if (sel_data_(dt, "param_CognitiveHempOther") != "")
                                content += sel_data_(dt, "param_CognitiveHempOther").Replace("|", ",");
                            content += "</item>";
                        }
                        if (sel_data_(dt, "param_CognitivePumpedStorage") != "")
                        {
                            content += "<item>抽搐，部位：" + sel_data_(dt, "param_CognitivePumpedStorage").Replace("其他", "");
                            if (sel_data_(dt, "param_CognitivePumpedStorageOther") != "")
                                content += sel_data_(dt, "param_CognitivePumpedStorageOther").Replace("|", ",");
                            content += "</item>";
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #region 產科史
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-10\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"產科史\" />";
                content += "<title>產科史</title><text><list>";

                if (sel_data_(dt, "param_MCStart") != "" || sel_data_(dt, "param_Last_MC") != "" || sel_data_(dt, "param_MCCycle_rule") != "" || sel_data_(dt, "param_MCDay") != "" || sel_data_(dt, "param_MCAmount") != "" || sel_data_(dt, "param_FBAbnormalDtl") != "")
                {
                    content += "<item>月經<list>";
                    if (sel_data_(dt, "param_MCStart") != "")
                        content += "<item>初經年齡：" + sel_data_(dt, "param_MCStart") + "歲</item>";
                    if (sel_data_(dt, "param_Last_MC") != "")
                        content += "<item>最後月經日：" + sel_data_(dt, "param_Last_MC") + "</item>";
                    if (sel_data_(dt, "param_MCCycle_rule") != "")
                    {
                        content += "<item>月經週期：" + sel_data_(dt, "param_MCCycle_rule");
                        if (sel_data_(dt, "param_MCCycle_rule_day") != "")
                            content += "，" + sel_data_(dt, "param_MCCycle_rule_day").Replace("|", ",") + "天";
                        content += "</item>";
                    }
                    if (sel_data_(dt, "param_MCDay") != "")
                        content += "<item>月經天數：" + sel_data_(dt, "param_MCDay") + "天</item>";
                    if (sel_data_(dt, "param_MCAmount") != "")
                        content += "<item>月經量：" + sel_data_(dt, "param_MCAmount") + "</item>";
                    if (sel_data_(dt, "param_FBAbnormalDtl") != "")
                    {
                        content += "<item>月經期間：" + sel_data_(dt, "param_FBAbnormalDtl").Replace("其他", "");
                        if (sel_data_(dt, "param_FBAbnormalOther") != "")
                            content += "，" + sel_data_(dt, "param_FBAbnormalOther").Replace("|", ",");
                        content += "</item>";
                    }
                    content += "</list></item>";
                }
                if (sel_data_(dt, "param_SelfCheck_Breast") != "")
                    content += "<item>乳房自我檢查：" + sel_data_(dt, "param_SelfCheck_Breast") + "</item>";
                if (sel_data_(dt, "param_SelfCheck_Vagina") != "")
                {
                    content += "<item>陰道抹片檢查：" + sel_data_(dt, "param_SelfCheck_Vagina");
                    if (sel_data_(dt, "param_SelfCheck_Vagina_Date") != "")
                        content += "，最後一次檢查日期：" + sel_data_(dt, "param_SelfCheck_Vagina_Date").Replace("|", ",");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_BornHistory") != "")
                {
                    content += "<item>生產史：" + sel_data_(dt, "param_BornHistory");
                    if (sel_data_(dt, "param_BornHistory_G") != "" || sel_data_(dt, "param_BornHistory_P") != "" || sel_data_(dt, "param_BornHistoryNL") != "" || sel_data_(dt, "param_BornHistoryND") != "" || sel_data_(dt, "param_BornHistoryHL") != "" || sel_data_(dt, "param_BornHistoryHD") != "" || sel_data_(dt, "param_Ectopic") != "")
                    {
                        content += "<list>";
                        if (sel_data_(dt, "param_BornHistory_G") != "")
                            content += "<item>孕次(G)：" + sel_data_(dt, "param_BornHistory_G") + "</item>";
                        if (sel_data_(dt, "param_BornHistory_P") != "")
                            content += "<item>產次(P)：" + sel_data_(dt, "param_BornHistory_P") + "</item>";
                        if (sel_data_(dt, "param_BornHistoryNL") != "")
                            content += "<item>自然產，活產數：" + sel_data_(dt, "param_BornHistoryNL") + "人</item>";
                        if (sel_data_(dt, "param_BornHistoryND") != "")
                            content += "<item>自然產，死產數(懷孕20週以上)：" + sel_data_(dt, "param_BornHistoryND") + "人</item>";
                        if (sel_data_(dt, "param_BornHistoryHL") != "")
                            content += "<item>剖腹產，活產數：" + sel_data_(dt, "param_BornHistoryHL") + "人</item>";
                        if (sel_data_(dt, "param_BornHistoryHD") != "")
                            content += "<item>剖腹產，死產數(懷孕20週以上)：" + sel_data_(dt, "param_BornHistoryHD") + "人</item>";
                        if (sel_data_(dt, "param_Ectopic") != "")
                            content += "<item>子宮外孕(Ectopic)：" + sel_data_(dt, "param_Ectopic") + "</item>";
                        content += "</list>";
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_AbortionHistory") != "")
                {
                    content += "<item>流產史(懷孕週數小於20週)：" + sel_data_(dt, "param_AbortionHistory");
                    if (sel_data_(dt, "param_AbortionN") != "" || sel_data_(dt, "param_AbortionH") != "")
                    {
                        content += "<list>";
                        if (sel_data_(dt, "param_AbortionN") != "")
                            content += "<item>自然流產：" + sel_data_(dt, "param_AbortionN") + "人</item>";
                        if (sel_data_(dt, "param_AbortionH") != "")
                            content += "<item>人工流產：" + sel_data_(dt, "param_AbortionH") + "次</item>";
                        content += "</list>";
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Contraception") != "")
                {
                    content += "<item>避孕(懷孕週數小於20週)：" + sel_data_(dt, "param_Contraception");
                    if (sel_data_(dt, "param_ContraceptionDesc") != "")
                    {
                        content += "，" + sel_data_(dt, "param_ContraceptionDesc").Replace("其他", "");
                        if (sel_data_(dt, "param_ContraceptionDesc_other") != "")
                            content += sel_data_(dt, "param_ContraceptionDesc_other").Replace("|", ",");
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "param_EDC") != "")
                    content += "<item>預產期(E.D.C)：" + sel_data_(dt, "param_EDC") + "</item>";
                if (sel_data_(dt, "param_Week") != "")
                    content += "<item>週數：" + sel_data_(dt, "param_Week") + "週</item>";

                content += "</list></text></section></component>";
                #endregion

                #region 疼痛評估
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-11\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"疼痛評估\" />";
                content += "<title>疼痛評估</title><text><list>";

                if (sel_data_(dt, "param_Awareness") != "")
                    content += "<item>目前意識狀態：" + sel_data_(dt, "param_Awareness") + "</item>";
                if (sel_data_(dt, "param_PainScale") != "")
                {
                    content += "<item>疼痛：" + sel_data_(dt, "param_PainScale");
                    if (sel_data_(dt, "param_PainSt") != "" || sel_data_(dt, "param_PainSDBrief") != "" || sel_data_(dt, "param_PainSDLang") != "" || sel_data_(dt, "param_PainSDFace") != "" || sel_data_(dt, "param_PainSDBodyLang") != "" || sel_data_(dt, "param_PainSDPiece") != "" || sel_data_(dt, "param_PainSDScole") != "")
                    {
                        content += "<list>";
                        if (sel_data_(dt, "param_PainSt") != "")
                            content += "<item>疼痛強度：" + sel_data_(dt, "param_PainSt").Substring(1, sel_data_(dt, "param_PainSt").IndexOf(")") - 1) + "分</item>";
                        if (sel_data_(dt, "param_PainSDBrief") != "")
                            content += "<item>呼吸：" + sel_data_(dt, "param_PainSDBrief") + "</item>";
                        if (sel_data_(dt, "param_PainSDLang") != "")
                            content += "<item>非言語表達：" + sel_data_(dt, "param_PainSDLang") + "</item>";
                        if (sel_data_(dt, "param_PainSDFace") != "")
                            content += "<item>臉部表情：" + sel_data_(dt, "param_PainSDFace") + "</item>";
                        if (sel_data_(dt, "param_PainSDBodyLang") != "")
                            content += "<item>肢體語言：" + sel_data_(dt, "param_PainSDBodyLang") + "</item>";
                        if (sel_data_(dt, "param_PainSDPiece") != "")
                            content += "<item>安撫：" + sel_data_(dt, "param_PainSDPiece") + "</item>";
                        if (sel_data_(dt, "param_PainSDScole") != "")
                            content += "<item>總分：" + sel_data_(dt, "param_PainSDScole") + "</item>";
                        content += "</list>";
                    }
                    content += "</item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #region 出院準備計畫評估
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-12\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"出院準備計畫評估\" />";
                content += "<title>出院準備計畫評估</title><text><list>";

                if (sel_data_(dt, "param_CPDBook") != "" || sel_data_(dt, "param_CPDAwareness") != "" || sel_data_(dt, "param_CPDActive") != "" || sel_data_(dt, "param_CPDPiss") != "" || sel_data_(dt, "param_CPDStool") != "" || sel_data_(dt, "param_CPDCare") != "" || sel_data_(dt, "param_CPDResouce") != "" || sel_data_(dt, "param_CPDSP2") != "" || sel_data_(dt, "param_CPDScole") != "")
                {
                    content += "<item>出院準備計畫評估<list>";
                    if (sel_data_(dt, "param_CPDBook") != "")
                        content += "<item>殘障手冊：" + sel_data_(dt, "param_CPDBook") + "</item>";
                    if (sel_data_(dt, "param_CPDAwareness") != "")
                        content += "<item>意識：" + sel_data_(dt, "param_CPDAwareness") + "</item>";
                    if (sel_data_(dt, "param_CPDActive") != "")
                        content += "<item>活動：" + sel_data_(dt, "param_CPDActive") + "</item>";
                    if (sel_data_(dt, "param_CPDPiss") != "")
                        content += "<item>解尿：" + sel_data_(dt, "param_CPDPiss") + "</item>";
                    if (sel_data_(dt, "param_CPDStool") != "")
                        content += "<item>大便：" + sel_data_(dt, "param_CPDStool") + "</item>";
                    if (sel_data_(dt, "param_CPDCare") != "")
                        content += "<item>照顧特質：" + sel_data_(dt, "param_CPDCare") + "</item>";
                    if (sel_data_(dt, "param_CPDResouce") != "")
                        content += "<item>照顧資源：" + sel_data_(dt, "param_CPDResouce") + "</item>";
                    if (sel_data_(dt, "param_CPDSP2") != "")
                    {
                        content += "<item>特殊照護：" + sel_data_(dt, "param_CPDSP2");
                        if (sel_data_(dt, "param_CPDSP2Other") != "")
                            content += " : " + sel_data_(dt, "param_CPDSP2Other").Replace("|", ",");
                        content += "</item>";
                    }
                    if (sel_data_(dt, "param_CPDScole") != "")
                        content += "<item>總分：" + sel_data_(dt, "param_CPDScole") + "</item>";
                    content += "</list></item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #region 聯絡資料
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-13\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"聯絡資料\" />";
                content += "<title>聯絡資料</title><text><list>";

                if (sel_data_(dt, "param_EMGContact") != "")
                {
                    content += "<item>聯絡資料<list>";
                    string[] Name = sel_data_(dt, "param_EMGContact").Split(',');
                    string[] Role_other = sel_data_(dt, "param_ContactRole_other").Split(',');
                    string[] Phone_1 = sel_data_(dt, "param_EMGContact_1").Split(',');
                    string[] Phone_2 = sel_data_(dt, "param_EMGContact_2").Split(',');
                    string[] Phone_3 = sel_data_(dt, "param_EMGContact_3").Split(',');
                    for (int i = 0; i < Name.Length; i++)
                    {
                        content += "<item>緊急聯絡人姓名：" + Name[i].Replace("|", ",");
                        if (i == 0)
                            content += "，稱謂：" + sel_data_(dt, "param_ContactRole").Replace("其他", "") + " " + Role_other[i].Replace("|", ",");
                        else
                            content += "，稱謂：" + sel_data_(dt, "param_ContactRole_" + i.ToString()).Replace("其他", "") + " " + Role_other[i].Replace("|", ",");
                        content += "，連絡電話-公司：" + Phone_1[i];
                        content += "，連絡電話-住家：" + Phone_2[i];
                        content += "，連絡電話-手機：" + Phone_3[i];
                        content += "</item>";
                    }
                    content += "</list></item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #endregion
            }
            else if (natype == "B")//新生兒
            {
                #region 設定內容

                #region 母親資料
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-0\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"母親資料\" />";
                content += "<title>母親資料</title><text><list>";

                if (sel_data_(dt, "ck_span_param_mother") != "")
                    content += "<item>產前一般資料：不詳</item>";
                else
                {
                    if (sel_data_(dt, "param_mother_name") != "" || sel_data_(dt, "param_mother_feeno") != "" || sel_data_(dt, "param_mother_age") != "" || sel_data_(dt, "param_BornHistory_G") != "" || sel_data_(dt, "param_BornHistory_P") != "" || sel_data_(dt, "param_AbortionH") != "" || sel_data_(dt, "param_AbortionN") != "" || sel_data_(dt, "param_EDC") != "")
                    {
                        content += "<item>產前一般資料<list>";
                        if (sel_data_(dt, "param_mother_name") != "")
                            content += "<item>母親姓名：" + sel_data_(dt, "param_mother_name") + "</item>";
                        if (sel_data_(dt, "param_mother_feeno") != "")
                            content += "<item>母親病歷號：" + sel_data_(dt, "param_mother_feeno") + "</item>";
                        if (sel_data_(dt, "param_mother_age") != "")
                            content += "<item>母親年齡：" + sel_data_(dt, "param_mother_age") + "</item>";
                        if (sel_data_(dt, "param_BornHistory_G") != "")
                            content += "<item>孕次(G)：" + sel_data_(dt, "param_BornHistory_G") + "</item>";
                        if (sel_data_(dt, "param_BornHistory_P") != "")
                            content += "<item>產次(P)：" + sel_data_(dt, "param_BornHistory_P") + "</item>";
                        if (sel_data_(dt, "param_AbortionH") != "")
                            content += "<item>人工流產(AA)：" + sel_data_(dt, "param_AbortionH") + "</item>";
                        if (sel_data_(dt, "param_AbortionN") != "")
                            content += "<item>自然流產(SA)：" + sel_data_(dt, "param_AbortionN") + "</item>";
                        if (sel_data_(dt, "param_EDC") != "")
                            content += "<item>預產期(EDC)：" + sel_data_(dt, "param_EDC") + "</item>";
                        content += "</list></item>";
                    }
                }
                if (sel_data_(dt, "ck_span_param_mother_check") != "")
                    content += "<item>產前檢查-母親：不詳</item>";
                else
                {
                    if (sel_data_(dt, "param_blood_type") != "" || sel_data_(dt, "param_RH") != "" || sel_data_(dt, "param_VDRL") != "" || sel_data_(dt, "param_HIV") != "" || sel_data_(dt, "param_HBsAg") != "" || sel_data_(dt, "param_HBeAg") != "" || sel_data_(dt, "param_GBS") != "")
                    {
                        content += "<item>產前檢查-母親<list>";
                        if (sel_data_(dt, "param_blood_type") != "")
                        {
                            content += "<item>血型：" + sel_data_(dt, "param_blood_type").Replace("其他", "");
                            if (sel_data_(dt, "param_blood_type_other") != "")
                                content += sel_data_(dt, "param_blood_type_other").Replace("|", ",");
                            content += "</item>";
                        }
                        if (sel_data_(dt, "param_RH") != "")
                            content += "<item>Rh血型(Rhesus monkeys)：" + sel_data_(dt, "param_RH") + "</item>";
                        if (sel_data_(dt, "param_VDRL") != "")
                            content += "<item>梅毒血清檢查(VDRL)：" + sel_data_(dt, "param_VDRL") + "</item>";
                        if (sel_data_(dt, "param_HIV") != "")
                            content += "<item>人類免疫缺陷病毒(HIV)：" + sel_data_(dt, "param_HIV") + "</item>";
                        if (sel_data_(dt, "param_HBsAg") != "")
                            content += "<item>人類免疫缺陷病毒(HBsAg)：" + sel_data_(dt, "param_HBsAg") + "</item>";
                        if (sel_data_(dt, "param_HBeAg") != "")
                            content += "<item>B型肝炎E抗原(HBeAg)：" + sel_data_(dt, "param_HBeAg") + "</item>";
                        if (sel_data_(dt, "param_GBS") != "")
                            content += "<item>B乙型鏈球菌(GBS)：" + sel_data_(dt, "param_GBS") + "</item>";
                        content += "</list></item>";
                    }
                }
                if (sel_data_(dt, "ck_span_param_father_check") != "")
                    content += "<item>產前檢查-父親：不詳</item>";
                else
                {
                    if (sel_data_(dt, "param_blood_type_f") != "" || sel_data_(dt, "param_RH_f") != "" || sel_data_(dt, "param_VDRL_f") != "" || sel_data_(dt, "param_HIV_f") != "" || sel_data_(dt, "param_HBsAg_f") != "" || sel_data_(dt, "param_HBeAg_f") != "" || sel_data_(dt, "param_GBS_f") != "")
                    {
                        content += "<item>產前檢查-父親<list>";
                        if (sel_data_(dt, "param_blood_type_f") != "")
                        {
                            content += "<item>血型：" + sel_data_(dt, "param_blood_type_f").Replace("其他", "");
                            if (sel_data_(dt, "param_blood_type_other_f") != "")
                                content += sel_data_(dt, "param_blood_type_other_f").Replace("|", ",");
                            content += "</item>";
                        }
                        if (sel_data_(dt, "param_RH_f") != "")
                            content += "<item>Rh血型(Rhesus monkeys)：" + sel_data_(dt, "param_RH_f") + "</item>";
                        if (sel_data_(dt, "param_VDRL_f") != "")
                            content += "<item>梅毒血清檢查(VDRL)：" + sel_data_(dt, "param_VDRL_f") + "</item>";
                        if (sel_data_(dt, "param_HIV_f") != "")
                            content += "<item>人類免疫缺陷病毒(HIV)：" + sel_data_(dt, "param_HIV_f") + "</item>";
                        if (sel_data_(dt, "param_HBsAg_f") != "")
                            content += "<item>人類免疫缺陷病毒(HBsAg)：" + sel_data_(dt, "param_HBsAg_f") + "</item>";
                        if (sel_data_(dt, "param_HBeAg_f") != "")
                            content += "<item>B型肝炎E抗原(HBeAg)：" + sel_data_(dt, "param_HBeAg_f") + "</item>";
                        if (sel_data_(dt, "param_GBS_f") != "")
                            content += "<item>B乙型鏈球菌(GBS)：" + sel_data_(dt, "param_GBS_f") + "</item>";
                        content += "</list></item>";
                    }
                }
                if (sel_data_(dt, "param_im_history") != "")
                {
                    content += "<item>內科病史：" + sel_data_(dt, "param_im_history");
                    if (sel_data_(dt, "param_im_history_item1") != "")
                        content += "，高血壓" + sel_data_(dt, "param_im_history_item1") + "年";
                    if (sel_data_(dt, "param_im_history_item2") != "")
                        content += "，心臟病" + sel_data_(dt, "param_im_history_item2") + "年";
                    if (sel_data_(dt, "param_im_history_item3") != "")
                        content += "，糖尿病" + sel_data_(dt, "param_im_history_item3") + "年";
                    if (sel_data_(dt, "param_im_history_item4") != "")
                        content += "，氣喘" + sel_data_(dt, "param_im_history_item4") + "年";
                    if (sel_data_(dt, "param_im_history_item_other") != "")
                        content += "，其他疾病：" + sel_data_(dt, "param_im_history_item_other").Replace("其他", "");
                    if (sel_data_(dt, "param_im_history_item_other_txt") != "" && sel_data_(dt, "param_im_history_item_other").IndexOf("其他") > -1)
                        content += sel_data_(dt, "param_im_history_item_other_txt").Replace("|", ",");
                    if (sel_data_(dt, "param_im_history_status") != "")
                        content += "，疾病發生時間，處理情形及目前狀況：" + sel_data_(dt, "param_im_history_status").Replace("|", ",");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_cigarette_ck") != "")
                    content += "<item>抽菸：不詳</item>";
                else
                {
                    if (sel_data_(dt, "param_cigarette") != "")
                    {
                        if (sel_data_(dt, "param_cigarette") == "1")
                            content += "<item>抽菸：無";
                        if (sel_data_(dt, "param_cigarette") == "2")
                            content += "<item>抽菸：有";
                        if (sel_data_(dt, "param_cigarette") == "3")
                            content += "<item>抽菸：戒菸";
                        if (sel_data_(dt, "param_cigarette_yes_amount") != "")
                            content += "，每日" + sel_data_(dt, "param_cigarette_yes_amount") + "支";
                        if (sel_data_(dt, "param_cigarette_yes_year") != "")
                            content += "，已抽" + sel_data_(dt, "param_cigarette_yes_year") + "年";
                        if (sel_data_(dt, "param_cigarette_agree_stop") != "")
                            content += "，有無戒菸意願：" + sel_data_(dt, "param_cigarette_agree_stop");
                        if (sel_data_(dt, "param_cigarette_stop_year") != "")
                            content += "，" + sel_data_(dt, "param_cigarette_stop_year") + "年";
                        content += "</item>";
                    }
                }
                if (sel_data_(dt, "param_drink_ck") != "")
                    content += "<item>喝酒：不詳</item>";
                else
                {
                    if (sel_data_(dt, "param_drink") != "")
                    {
                        content += "<item>喝酒：" + sel_data_(dt, "param_drink");
                        if (sel_data_(dt, "param_drink_day") != "")
                            content += "，每日劑量" + sel_data_(dt, "param_drink_day") + "瓶";
                        if (sel_data_(dt, "param_drink_unit") != "")
                            content += " " + sel_data_(dt, "param_drink_unit") + "mL";
                        content += "</item>";
                    }
                }
                if (sel_data_(dt, "param_med_ck") != "")
                    content += "<item>目前服用藥物：不詳</item>";
                else
                {
                    if (sel_data_(dt, "param_med") != "")
                    {
                        content += "<item>目前服用藥物：" + sel_data_(dt, "param_med");
                        if (sel_data_(dt, "param_med") == "有")
                        {
                            //取得目前用藥
                            List<DrugOrder> Drug_list = new List<DrugOrder>();
                            byte[] labfoByteCode = webService.GetOpdMed(feeno);
                            if (labfoByteCode != null)
                            {
                                string labJosnArr = CompressTool.DecompressString(labfoByteCode);
                                Drug_list = JsonConvert.DeserializeObject<List<DrugOrder>>(labJosnArr);
                                if (Drug_list.Count > 0)
                                {
                                    content += "<list>";
                                    for (int i = 0; i < Drug_list.Count; i++)
                                    {
                                        content += "<item>門診用藥-藥物名稱：" + trans_special_code(Drug_list[i].DrugName);
                                        content += "，頻次：" + trans_special_code(Drug_list[i].Feq);
                                        content += "，劑量：" + trans_special_code(Drug_list[i].Dose.ToString());
                                        content += "，途徑：" + trans_special_code(Drug_list[i].Route) + "</item>";
                                    }
                                    content += "</list>";
                                }
                            }
                        }
                        if (sel_data_(dt, "param_med_name") != "")
                        {
                            content += "<list>";
                            string[] DrugName = sel_data_(dt, "param_med_name").Split(',');
                            string[] Feq = sel_data_(dt, "param_med_frequency").Split(',');
                            string[] Dose = sel_data_(dt, "param_med_amount").Split(',');
                            string[] Route = sel_data_(dt, "param_med_way").Split(',');
                            for (int i = 0; i < DrugName.Length; i++)
                            {
                                content += "<item>藥物名稱：" + DrugName[i].Replace("|", ",");
                                content += "，頻次：" + Feq[i].Replace("|", ",");
                                content += "，劑量：" + Dose[i].Replace("|", ",");
                                content += "，途徑：" + Route[i].Replace("|", ",") + "</item>";
                            }
                            content += "</list>";
                        }
                        content += "</item>";
                    }
                }
                if (sel_data_(dt, "rb_birth_type_ck") != "")
                    content += "<item>分娩方式：不詳</item>";
                else
                {
                    if (sel_data_(dt, "rb_birth_type") != "")
                        content += "<item>分娩方式：" + sel_data_(dt, "rb_birth_type");
                    if (sel_data_(dt, "txt_birth_type_reason") != "")
                        content += "，原因" + sel_data_(dt, "txt_birth_type_reason").Replace("|", ",");
                    if (sel_data_(dt, "rb_birth_type_dtl") != "")
                        content += "，" + sel_data_(dt, "rb_birth_type_dtl");
                    content += "</item>";
                }
                if (sel_data_(dt, "rb_Fetal_ck") != "")
                    content += "<item>胎位：不詳</item>";
                else
                {
                    if (sel_data_(dt, "rb_Fetal") != "")
                        content += "<item>胎位：" + sel_data_(dt, "rb_Fetal") + "</item>";
                }
                if (sel_data_(dt, "Rupture_day_ck") != "")
                    content += "<item>破水時間：不詳</item>";
                else
                {
                    if (sel_data_(dt, "Rupture_day") != "" || sel_data_(dt, "Rupture_time") != "" || sel_data_(dt, "txt_birth_type_reason") != "")
                    {
                        content += "<item>破水時間<list>";
                        if (sel_data_(dt, "Rupture_day") != "")
                            content += "<item>時間：" + sel_data_(dt, "Rupture_day") + " " + sel_data_(dt, "Rupture_time") + "</item>";
                        if (sel_data_(dt, "txt_birth_type_reason") != "")
                            content += "<item>早期破水(PROM)：" + sel_data_(dt, "txt_birth_type_reason").Replace("|", ",") + "</item>";
                        content += "</list></item>";
                    }
                }
                if (sel_data_(dt, "rb_Amniotic_fluid_amount_ck") != "")
                    content += "<item>羊水量：不詳</item>";
                else
                {
                    if (sel_data_(dt, "rb_Amniotic_fluid_amount") != "")
                        content += "<item>羊水量：" + sel_data_(dt, "rb_Amniotic_fluid_amount") + "</item>";
                }
                if (sel_data_(dt, "rb_Amniotic_fluid_type") != "")
                {
                    content += "<item>羊水性狀：" + sel_data_(dt, "rb_Amniotic_fluid_type");
                    if (sel_data_(dt, "rb_Amniotic_fluid_type_dtl") != "")
                        content += "，" + sel_data_(dt, "rb_Amniotic_fluid_type_dtl");
                    if (sel_data_(dt, "Amniotic_fluid_type_other") != "")
                        content += "，" + sel_data_(dt, "Amniotic_fluid_type_other").Replace("|", ",");
                    content += "</item>";
                }
                if (sel_data_(dt, "rb_complications_ck") != "")
                    content += "<item>分娩期合併症：不詳</item>";
                else
                {
                    if (sel_data_(dt, "rb_complications") != "")
                    {
                        content += "<item>分娩期合併症：" + sel_data_(dt, "rb_complications");
                        if (sel_data_(dt, "ck_complications") != "")
                            content += "，" + sel_data_(dt, "ck_complications");
                        content += "</item>";
                    }
                }

                content += "</list></text></section></component>";

                #endregion

                #region 嬰兒資料
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-1\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"嬰兒資料\" />";
                content += "<title>嬰兒資料</title><text><list>";

                if (sel_data_(dt, "param_tube_date") != "" && sel_data_(dt, "param_tube_time") != "")
                    content += "<item>入院日期：" + sel_data_(dt, "param_tube_date") + " " + sel_data_(dt, "param_tube_time") + "</item>";
                if (sel_data_(dt, "param_ipd_reason") != "")
                    content += "<item>入院原因：" + sel_data_(dt, "param_ipd_reason").Replace("|", ",") + "</item>";
                if (sel_data_(dt, "Born_day_ck") != "")
                    content += "<item>出生時間：不詳</item>";
                else
                {
                    if (sel_data_(dt, "Born_day") != "" || sel_data_(dt, "txt_weight") != "")
                    {
                        content += "<item>";
                        if (sel_data_(dt, "Born_day") != "")
                            content += "出生時間：" + sel_data_(dt, "Born_day") + " " + sel_data_(dt, "Born__time");
                        if (sel_data_(dt, "txt_weight") != "")
                            content += "，出生體重：" + sel_data_(dt, "txt_weight");
                        content += "</item>";
                    }
                }
                if (sel_data_(dt, "txt_apgar_score_ck") != "")
                    content += "<item>阿帕嘉分數(Apgar score)：不詳</item>";
                else
                {
                    if (sel_data_(dt, "txt_apgar_score_1") != "" || sel_data_(dt, "txt_apgar_score_5") != "")
                    {
                        content += "<item>阿帕嘉分數(Apgar score)：";
                        if (sel_data_(dt, "txt_apgar_score_1") != "")
                            content += "1’" + sel_data_(dt, "txt_apgar_score_1");
                        if (sel_data_(dt, "txt_apgar_score_5") != "")
                            content += "5’" + sel_data_(dt, "txt_apgar_score_5");
                        content += "</item>";
                    }
                }
                if (sel_data_(dt, "rb_born_Situation_ck") != "")
                    content += "<item>出生情形：不詳</item>";
                else
                {
                    if (sel_data_(dt, "rb_born_Situation") != "")
                    {
                        content += "<item>出生情形：" + sel_data_(dt, "rb_born_Situation");
                        if (sel_data_(dt, "rb_born_Situation_other") != "")
                            content += "，" + sel_data_(dt, "rb_born_Situation_other").Replace("|", ",");
                        content += "</item>";
                    }
                }
                if (sel_data_(dt, "rb_Exterior") != "")
                {
                    content += "<item>外觀：" + sel_data_(dt, "rb_Exterior");
                    if (sel_data_(dt, "txt_Exterior_other") != "")
                        content += "，" + sel_data_(dt, "txt_Exterior_other").Replace("|", ",");
                    content += "</item>";
                }
                if (sel_data_(dt, "rb_Meconium_Color_ck") != "")
                    content += "<item>胎便染色：不詳</item>";
                else
                {
                    if (sel_data_(dt, "rb_Meconium_Color") != "")
                    {
                        content += "<item>胎便染色：" + sel_data_(dt, "rb_Meconium_Color");
                        if (sel_data_(dt, "rb_Meconium_Color_Degree") != "")
                            content += "，程度：" + sel_data_(dt, "rb_Meconium_Color_Degree");
                        if (sel_data_(dt, "txt_Meconium_Color_Degree_dtl") != "")
                            content += "，處置：" + sel_data_(dt, "txt_Meconium_Color_Degree_dtl").Replace("|", ",");
                        content += "</item>";
                    }
                }
                if (sel_data_(dt, "rb_OutMed") != "")
                {
                    content += "<item>在外就醫或服藥：" + sel_data_(dt, "rb_OutMed");
                    if (sel_data_(dt, "rb_OutMed_Dtl") != "")
                        content += "，" + sel_data_(dt, "rb_OutMed_Dtl").Replace("|", ",");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_BabyCheck_ck") != "")
                    content += "<item>新生兒篩檢：不詳</item>";
                else
                {
                    if (sel_data_(dt, "param_BabyCheck") != "")
                    {
                        content += "<item>新生兒篩檢：" + sel_data_(dt, "param_BabyCheck");
                        if (sel_data_(dt, "param_BabyCheck_Dtl") != "")
                            content += "，原因：" + sel_data_(dt, "param_BabyCheck_Dtl").Replace("|", ",");
                        content += "</item>";
                    }
                }
                if (sel_data_(dt, "param_Vaccination") != "")
                {
                    content += "<item>預防接種<list>";
                    string[] type = sel_data_(dt, "param_Vaccination").Split(',');
                    for (int i = 0; i < type.Length; i++)
                    {
                        if (type[i] == "均未接種")
                        {
                            content += "<item>" + type[i];
                            if (sel_data_(dt, "param_Vaccination_4_dtl") != "")
                                content += "，未接種原因：" + sel_data_(dt, "param_Vaccination_4_dtl").Replace("|", ",");
                            content += "</item>";
                        }
                        else if (type[i] == "HBIG")
                        {
                            content += "<item>B型肝炎免疫球蛋白(HBIG)</item>";
                        }
                        else if (type[i] == "BCG")
                        {
                            content += "<item>卡介苗(BCG)</item>";
                        }
                        else if (type[i] == "HBV")
                        {
                            content += "<item>B型肝炎疫苗(HBV)";
                            if (sel_data_(dt, "param_Vaccination_3_dtl") != "")
                                content += "，已打：" + sel_data_(dt, "param_Vaccination_3_dtl").Replace("|", ",") + "劑";
                            content += "</item>";
                        }
                        else if (type[i] == "五合一疫苗")
                        {
                            content += "<item>五合一(白喉、破傷風、非細胞性百日咳、B型嗜血桿菌、不活化小兒麻痺)";
                            if (sel_data_(dt, "param_Vaccination_5_dtl") != "")
                                content += "，已打：" + sel_data_(dt, "param_Vaccination_5_dtl").Replace("|", ",") + "劑";
                            content += "</item>";
                        }
                    }
                    content += "</list></item>";
                }
                if (sel_data_(dt, "param_ParentNot") != "")
                {
                    content += "<item>角色關係：" + sel_data_(dt, "param_ParentNot");
                    if (sel_data_(dt, "param_ParentOBorther") != "")
                        content += "，兄" + sel_data_(dt, "param_ParentOBorther").Replace("|", ",") + "人";
                    if (sel_data_(dt, "param_ParentYBorther") != "")
                        content += "，弟" + sel_data_(dt, "param_ParentYBorther").Replace("|", ",") + "人";
                    if (sel_data_(dt, "param_ParentOSister") != "")
                        content += "，姐" + sel_data_(dt, "param_ParentOSister").Replace("|", ",") + "人";
                    if (sel_data_(dt, "param_ParentYSister") != "")
                        content += "，妹" + sel_data_(dt, "param_ParentYSister").Replace("|", ",") + "人";
                    content += "</item>";
                }
                if (sel_data_(dt, "param_ParentCare") != "")
                {
                    content += "<item>嬰兒主要照顧者：" + sel_data_(dt, "param_ParentCare").Replace("其他", "");
                    if (sel_data_(dt, "param_ParentCare_dtl") != "")
                        content += sel_data_(dt, "param_ParentCare_dtl").Replace("|", ",");
                    content += "</item>";
                }
                if (sel_data_(dt, "param_Same_Room") != "")
                    content += "<item>親子同室：" + sel_data_(dt, "param_Same_Room") + "</item>";
                if (sel_data_(dt, "param_Bay_Care") != "")
                {
                    content += "<item>嬰兒照護：" + sel_data_(dt, "param_Bay_Care");
                    if (sel_data_(dt, "param_Bottle_Steriliz") != "" || sel_data_(dt, "param_Milk_Mix") != "" || sel_data_(dt, "param_Baby_Birth_Amount") != "" || sel_data_(dt, "param_Baby_Birth_Day") != "" || sel_data_(dt, "param_Umbilical_Process") != "")
                    {
                        content += "<list>";
                        if (sel_data_(dt, "param_Bottle_Steriliz") != "")
                            content += "<item>奶瓶消毒：" + sel_data_(dt, "param_Bottle_Steriliz") + "</item>";
                        if (sel_data_(dt, "param_Milk_Mix") != "")
                            content += "<item>調奶方法：" + sel_data_(dt, "param_Milk_Mix") + "</item>";
                        if (sel_data_(dt, "param_Baby_Birth_Amount") != "" || sel_data_(dt, "param_Baby_Birth_Day") != "")
                            content += "<item>洗澡：" + sel_data_(dt, "param_Baby_Birth_Amount") + "次/" + sel_data_(dt, "param_Baby_Birth_Day") + "天</item>";
                        if (sel_data_(dt, "param_Umbilical_Process") != "")
                            content += "<item>臍帶處理：" + sel_data_(dt, "param_Umbilical_Process") + "</item>";
                        content += "</list>";
                    }
                    content += "</item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #region 身體評估
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-2\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"身體評估\" />";
                content += "<title>身體評估</title><text><list>";

                if (sel_data_(dt, "txt_width_now") != "" || sel_data_(dt, "txt_height") != "" || sel_data_(dt, "txt_head") != "" || sel_data_(dt, "txt_chest") != "")
                {
                    content += "<item>身體評估<list>";
                    if (sel_data_(dt, "txt_width_now") != "")
                        content += "<item>體重：" + sel_data_(dt, "txt_width_now") + "</item>";
                    if (sel_data_(dt, "txt_height") != "")
                        content += "<item>身長：" + sel_data_(dt, "txt_height") + "</item>";
                    if (sel_data_(dt, "txt_head") != "")
                        content += "<item>頭圍：" + sel_data_(dt, "txt_head") + "</item>";
                    if (sel_data_(dt, "txt_chest") != "")
                        content += "<item>胸圍：" + sel_data_(dt, "txt_chest") + "</item>";
                    content += "</list></item>";
                }
                if (sel_data_(dt, "rb_Stroke") != "")
                {
                    content += "<item>心搏：" + sel_data_(dt, "rb_Stroke");
                    if (sel_data_(dt, "rb_Stroke_Abnormal") != "")
                        content += "，" + sel_data_(dt, "rb_Stroke_Abnormal");
                    content += "</item>";
                }
                if (sel_data_(dt, "rb_Breath") != "")
                {
                    content += "<item>呼吸：" + sel_data_(dt, "rb_Breath");
                    if (sel_data_(dt, "rb_Breath_Abnormal") != "")
                        content += "，" + sel_data_(dt, "rb_Breath_Abnormal");
                    content += "</item>";
                }
                if (sel_data_(dt, "rb_Head") != "")
                {
                    content += "<item>頭部：" + sel_data_(dt, "rb_Head");
                    if (sel_data_(dt, "rb_Head_Abnormal") != "")
                    {
                        string[] type = sel_data_(dt, "rb_Head_Abnormal").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "產瘤")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "rb_Head_Abnormal_1_dtl") != "")
                                    content += "，名稱 : " + sel_data_(dt, "rb_Head_Abnormal_1_dtl").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "血腫")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "rb_Head_Abnormal_2_dtl") != "")
                                    content += "，名稱 : " + sel_data_(dt, "rb_Head_Abnormal_2_dtl").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "瘀點")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "rb_Head_Abnormal_3_dtl") != "")
                                    content += "，名稱 : " + sel_data_(dt, "rb_Head_Abnormal_3_dtl").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "破皮")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "rb_Head_Abnormal_4_dtl") != "")
                                    content += "，名稱 : " + sel_data_(dt, "rb_Head_Abnormal_4_dtl").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "其他")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "rb_Head_Abnormal_5_dtl") != "")
                                    content += "，名稱 : " + sel_data_(dt, "rb_Head_Abnormal_5_dtl").Replace("|", ",");
                                content += "</item>";
                            }
                            else
                                content += "<item>" + type[i] + "</item>";
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "rb_face") != "")
                {
                    content += "<item>顏面：" + sel_data_(dt, "rb_face");
                    if (sel_data_(dt, "rb_face_Abnormal") != "")
                    {
                        string[] type = sel_data_(dt, "rb_face_Abnormal").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "嘴角下垂")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "rb_face_Abnormal_1_dtl") != "")
                                    content += "，" + sel_data_(dt, "rb_face_Abnormal_1_dtl");
                                content += "</item>";
                            }
                            else if (type[i] == "其他")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "rb_face_Abnormal_3_dtl") != "")
                                    content += "，" + sel_data_(dt, "rb_face_Abnormal_3_dtl").Replace("|", ",");
                                content += "</item>";
                            }
                            else
                                content += "<item>" + type[i] + "</item>";
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "rb_left_eye") != "")
                {
                    content += "<item>眼睛 - 左：" + sel_data_(dt, "rb_left_eye");
                    if (sel_data_(dt, "rb_eye_Abnormal_Left") != "")
                    {
                        string[] type = sel_data_(dt, "rb_eye_Abnormal_Left").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "分泌物")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "rb_eye_Abnormal_Left_type") != "")
                                    content += "，性狀：" + sel_data_(dt, "rb_eye_Abnormal_Left_type");
                                content += "</item>";
                            }
                            else if (type[i] == "其他")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "rb_eye_Abnormal_Left_other") != "")
                                    content += "，" + sel_data_(dt, "rb_eye_Abnormal_Left_other").Replace("|", ",");
                                content += "</item>";
                            }
                            else
                                content += "<item>" + type[i] + "</item>";
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "rb_right_eye") != "")
                {
                    content += "<item>眼睛 - 右：" + sel_data_(dt, "rb_right_eye");
                    if (sel_data_(dt, "rb_eye_Abnormal_Right") != "")
                    {
                        string[] type = sel_data_(dt, "rb_eye_Abnormal_Right").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "分泌物")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "rb_eye_Abnormal_Right_type") != "")
                                    content += "，性狀：" + sel_data_(dt, "rb_eye_Abnormal_Right_type");
                                content += "</item>";
                            }
                            else if (type[i] == "其他")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "rb_eye_Abnormal_Right_other") != "")
                                    content += "，" + sel_data_(dt, "rb_eye_Abnormal_Right_other").Replace("|", ",");
                                content += "</item>";
                            }
                            else
                                content += "<item>" + type[i] + "</item>";
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "rb_left_ear") != "")
                {
                    content += "<item>耳朵-左：" + sel_data_(dt, "rb_left_ear");
                    if (sel_data_(dt, "rb_ear_Abnormal_Left") != "")
                    {
                        content += "，" + sel_data_(dt, "rb_ear_Abnormal_Left").Replace("其他", "");
                        if (sel_data_(dt, "rb_ear_Abnormal_Left_other") != "")
                            content += sel_data_(dt, "rb_ear_Abnormal_Left_other").Replace("|", ",");
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "rb_right_ear") != "")
                {
                    content += "<item>耳朵-右：" + sel_data_(dt, "rb_right_ear");
                    if (sel_data_(dt, "rb_ear_Abnormal_Right") != "")
                    {
                        content += "，" + sel_data_(dt, "rb_ear_Abnormal_Right").Replace("其他", "");
                        if (sel_data_(dt, "rb_ear_Abnormal_Right_other") != "")
                            content += sel_data_(dt, "rb_ear_Abnormal_Right_other").Replace("|", ",");
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "rb_nose") != "")
                {
                    content += "<item>鼻子：" + sel_data_(dt, "rb_nose");
                    if (sel_data_(dt, "rb_nose_Abnormal") != "")
                    {
                        content += "，" + sel_data_(dt, "rb_nose_Abnormal").Replace("其他", "");
                        if (sel_data_(dt, "rb_nose_Abnormal_other") != "")
                            content += sel_data_(dt, "rb_nose_Abnormal_other").Replace("|", ",");
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "rb_neck") != "")
                {
                    content += "<item>頸部：" + sel_data_(dt, "rb_neck");
                    if (sel_data_(dt, "rb_neck_Abnormal") != "")
                    {
                        string[] type = sel_data_(dt, "rb_neck_Abnormal").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "斜頸")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "rb_neck_Abnormal_1_dtl") != "")
                                    content += "，" + sel_data_(dt, "rb_neck_Abnormal_1_dtl");
                                content += "</item>";
                            }
                            else if (type[i] == "其他")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "rb_neck_Abnormal_3_dtl") != "")
                                    content += "，" + sel_data_(dt, "rb_neck_Abnormal_3_dtl").Replace("|", ",");
                                content += "</item>";
                            }
                            else
                                content += "<item>" + type[i] + "</item>";
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "rb_Breast") != "")
                    content += "<item>乳腺：" + sel_data_(dt, "rb_Breast") + "</item>";
                if (sel_data_(dt, "rb_abdomen") != "")
                {
                    content += "<item>腹部：" + sel_data_(dt, "rb_abdomen");
                    if (sel_data_(dt, "rb_abdomen_Abnormal") != "")
                    {
                        content += "，" + sel_data_(dt, "rb_abdomen_Abnormal").Replace("其他", "");
                        if (sel_data_(dt, "rb_abdomen_Abnormal_other") != "")
                            content += sel_data_(dt, "rb_abdomen_Abnormal_other").Replace("|", ",");
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "rb_rear") != "")
                {
                    content += "<item>背部：" + sel_data_(dt, "rb_rear");
                    if (sel_data_(dt, "rb_rear_Abnormal") != "")
                    {
                        content += "，" + sel_data_(dt, "rb_rear_Abnormal");
                        if (sel_data_(dt, "rb_rear_Abnormal_other") != "")
                            content += sel_data_(dt, "rb_rear_Abnormal_other").Replace("|", ",");
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "rb_limbs_left_top") != "")
                {
                    content += "<item>左上肢：" + sel_data_(dt, "rb_limbs_left_top");
                    if (sel_data_(dt, "rb_limbs_left_top_Abnormal") != "")
                    {
                        string[] type = sel_data_(dt, "rb_limbs_left_top_Abnormal").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "麻痺")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "rb_limbs_left_top_Abnormal_1_dtl") != "")
                                    content += "：" + sel_data_(dt, "rb_limbs_left_top_Abnormal_1_dtl");
                                content += "</item>";
                            }
                            else if (type[i] == "併指")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "rb_limbs_left_top_Abnormal_5_dtl") != "")
                                    content += "：" + sel_data_(dt, "rb_limbs_left_top_Abnormal_5_dtl").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "多指")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "rb_limbs_left_top_Abnormal_4_dtl") != "")
                                    content += "：" + sel_data_(dt, "rb_limbs_left_top_Abnormal_4_dtl").Replace("|", ",");
                                content += "</item>";
                            }
                            else
                                content += "<item>" + type[i] + "</item>";
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "rb_limbs_right_top") != "")
                {
                    content += "<item>右上肢：" + sel_data_(dt, "rb_limbs_right_top");
                    if (sel_data_(dt, "rb_limbs_right_top_Abnormal") != "")
                    {
                        string[] type = sel_data_(dt, "rb_limbs_right_top_Abnormal").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "麻痺")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "rb_limbs_right_top_Abnormal_1_dtl") != "")
                                    content += "：" + sel_data_(dt, "rb_limbs_right_top_Abnormal_1_dtl");
                                content += "</item>";
                            }
                            else if (type[i] == "併指")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "rb_limbs_right_top_Abnormal_5_dtl") != "")
                                    content += "：" + sel_data_(dt, "rb_limbs_right_top_Abnormal_5_dtl").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "多指")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "rb_limbs_right_top_Abnormal_4_dtl") != "")
                                    content += "：" + sel_data_(dt, "rb_limbs_right_top_Abnormal_4_dtl").Replace("|", ",");
                                content += "</item>";
                            }
                            else
                                content += "<item>" + type[i] + "</item>";
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "rb_limbs_left_down") != "")
                {
                    content += "<item>左下肢：" + sel_data_(dt, "rb_limbs_left_down");
                    if (sel_data_(dt, "rb_limbs_left_down_Abnormal") != "")
                    {
                        string[] type = sel_data_(dt, "rb_limbs_left_down_Abnormal").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "麻痺")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "rb_limbs_left_down_Abnormal_1_dtl") != "")
                                    content += "：" + sel_data_(dt, "rb_limbs_left_down_Abnormal_1_dtl");
                                content += "</item>";
                            }
                            else if (type[i] == "併指")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "rb_limbs_left_down_Abnormal_5_dtl") != "")
                                    content += "：" + sel_data_(dt, "rb_limbs_left_down_Abnormal_5_dtl").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "多指")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "rb_limbs_left_down_Abnormal_4_dtl") != "")
                                    content += "：" + sel_data_(dt, "rb_limbs_left_down_Abnormal_4_dtl").Replace("|", ",");
                                content += "</item>";
                            }
                            else
                                content += "<item>" + type[i] + "</item>";
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "rb_limbs_right_down_") != "")
                {
                    content += "<item>右下肢：" + sel_data_(dt, "rb_limbs_right_down_");
                    if (sel_data_(dt, "rb_limbs_right_down_Abnormal") != "")
                    {
                        string[] type = sel_data_(dt, "rb_limbs_right_down_Abnormal").Split(',');
                        content += "<list>";
                        for (int i = 0; i < type.Length; i++)
                        {
                            if (type[i] == "麻痺")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "rb_limbs_right_down_Abnormal_1_dtl") != "")
                                    content += "：" + sel_data_(dt, "rb_limbs_right_down_Abnormal_1_dtl");
                                content += "</item>";
                            }
                            else if (type[i] == "併指")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "rb_limbs_right_down_Abnormal_5_dtl") != "")
                                    content += "：" + sel_data_(dt, "rb_limbs_right_down_Abnormal_5_dtl").Replace("|", ",");
                                content += "</item>";
                            }
                            else if (type[i] == "多指")
                            {
                                content += "<item>" + type[i];
                                if (sel_data_(dt, "rb_limbs_right_down_Abnormal_4_dtl") != "")
                                    content += "：" + sel_data_(dt, "rb_limbs_right_down_Abnormal_4_dtl").Replace("|", ",");
                                content += "</item>";
                            }
                            else
                                content += "<item>" + type[i] + "</item>";
                        }
                        content += "</list>";
                    }
                    content += "</item>";
                }
                if (sel_data_(dt, "rb_skin") != "")
                {
                    content += "<item>皮膚<list>";
                    string[] type = sel_data_(dt, "rb_skin").Split(',');
                    for (int i = 0; i < type.Length; i++)
                    {
                        if (type[i] == "黃痘")
                        {
                            content += "<item>" + type[i];
                            if (sel_data_(dt, "rb_skin_Abnormal_6_dtl") != "")
                                content += "，指數：" + sel_data_(dt, "rb_skin_Abnormal_6_dtl");
                            content += "</item>";
                        }
                        else if (type[i] == "出血點")
                        {
                            content += "<item>" + type[i];
                            if (sel_data_(dt, "rb_skin_Abnormal_7_dtl") != "")
                                content += "，部位：" + sel_data_(dt, "rb_skin_Abnormal_7_dtl").Replace("|", ",");
                            content += "</item>";
                        }
                        else if (type[i] == "血瘤管")
                        {
                            content += "<item>" + type[i];
                            if (sel_data_(dt, "rb_skin_Abnormal_8_dtl") != "")
                                content += "，部位：" + sel_data_(dt, "rb_skin_Abnormal_8_dtl").Replace("|", ",");
                            content += "</item>";
                        }
                        else if (type[i] == "胎記")
                        {
                            content += "<item>" + type[i];
                            if (sel_data_(dt, "rb_skin_Abnormal_9_dtl") != "")
                                content += "，部位：" + sel_data_(dt, "rb_skin_Abnormal_9_dtl").Replace("|", ",");
                            content += "</item>";
                        }
                        else if (type[i] == "破皮")
                        {
                            content += "<item>" + type[i];
                            if (sel_data_(dt, "rb_skin_Abnormal_10_dtl") != "")
                                content += "，部位：" + sel_data_(dt, "rb_skin_Abnormal_10_dtl").Replace("|", ",");
                            content += "</item>";
                        }
                        else if (type[i] == "蒙古班")
                        {
                            content += "<item>" + type[i];
                            if (sel_data_(dt, "rb_skin_Abnormal_11_dtl") != "")
                                content += "，部位：" + sel_data_(dt, "rb_skin_Abnormal_11_dtl").Replace("|", ",");
                            content += "</item>";
                        }
                        else
                            content += "<item>" + type[i] + "</item>";
                    }
                    content += "</list></item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #region 營養代謝
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-3\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"營養代謝\" />";
                content += "<title>營養代謝</title><text><list>";

                if (sel_data_(dt, "param_feed") != "" || sel_data_(dt, "rb_Auxillary") != "")
                {
                    content += "<item>營養代謝<list>";
                    if (sel_data_(dt, "param_feed") != "")
                    {
                        content += "<item>餵食：" + sel_data_(dt, "param_feed").Replace("其他", "");
                        if (sel_data_(dt, "param_feed_other") != "")
                            content += "，" + sel_data_(dt, "param_feed_other").Replace("|", ",");
                        if (sel_data_(dt, "param_feed_1_dtl_1") != "" || sel_data_(dt, "param_feed_1_dtl_2") != "" || sel_data_(dt, "param_feed_1_dtl_3") != "")
                        {
                            content += "，每" + sel_data_(dt, "param_feed_1_dtl_1").Replace("|", ",") + "小時吃";
                            content += sel_data_(dt, "param_feed_1_dtl_2").Replace("|", ",") + sel_data_(dt, "param_feed_1_dtl_3");
                        }
                        if (sel_data_(dt, "param_feed_2_dtl_1") != "" || sel_data_(dt, "param_feed_2_dtl_2") != "" || sel_data_(dt, "param_feed_2_dtl_3") != "" || sel_data_(dt, "param_feed_2_dtl_4") != "")
                        {
                            content += "，" + sel_data_(dt, "param_feed_2_dtl_1").Replace("|", ",") + "奶粉，濃度";
                            content += sel_data_(dt, "param_feed_2_dtl_2").Replace("|", ",") + "%，每";
                            content += sel_data_(dt, "param_feed_2_dtl_3").Replace("|", ",") + "小時吃";
                            content += sel_data_(dt, "param_feed_2_dtl_4").Replace("|", ",") + "mL";
                        }
                        content += "</item>";
                    }
                    if (sel_data_(dt, "rb_Auxillary") != "")
                    {
                        content += "<item>副食品：" + sel_data_(dt, "rb_Auxillary");
                        if (sel_data_(dt, "rb_Auxillary_dtl") != "")
                            content += "，" + sel_data_(dt, "rb_Auxillary_dtl").Replace("|", ",");
                        if (sel_data_(dt, "rb_feed_Situation") != "")
                        {
                            content += "，食用情形：" + sel_data_(dt, "rb_feed_Situation");
                            if (sel_data_(dt, "rb_feed_Situation_Abnormal") != "")
                            {
                                content += "，" + sel_data_(dt, "rb_feed_Situation_Abnormal").Replace("其他", "");
                                if (sel_data_(dt, "rb_feed_Situation_Abnormal_other") != "")
                                    content += sel_data_(dt, "rb_feed_Situation_Abnormal_other").Replace("|", ",");
                            }
                        }
                        content += "</item>";
                    }
                    content += "</list></item>";
                }
                if (sel_data_(dt, "rb_Urine") != "" || sel_data_(dt, "param_stool") != "")
                {
                    content += "<item>排泄<list>";
                    if (sel_data_(dt, "rb_Urine") != "")
                    {
                        content += "<item>解尿：" + sel_data_(dt, "rb_Urine");
                        if (sel_data_(dt, "rb_Urine_Abnormal") != "")
                        {
                            content += "，" + sel_data_(dt, "rb_Urine_Abnormal").Replace("其他", "");
                            if (sel_data_(dt, "rb_Urine_Abnormal_other") != "")
                                content += sel_data_(dt, "rb_Urine_Abnormal_other").Replace("|", ",");
                        }
                        if (sel_data_(dt, "rb_Urine_Abnormal_Color") != "")
                        {
                            content += "，顏色：" + sel_data_(dt, "rb_Urine_Abnormal_Color").Replace("其他", "");
                            if (sel_data_(dt, "rb_Urine_Abnormal_Color_other") != "")
                                content += sel_data_(dt, "rb_Urine_Abnormal_Color_other").Replace("|", ",");
                        }
                        if (sel_data_(dt, "rb_Urine_Abnormal_Type") != "")
                        {
                            content += "，性狀：" + sel_data_(dt, "rb_Urine_Abnormal_Type").Replace("其他", "");
                            if (sel_data_(dt, "rb_Urine_Abnormal_Type_other") != "")
                                content += sel_data_(dt, "rb_Urine_Abnormal_Type_other").Replace("|", ",");
                        }
                        content += "</item>";
                    }
                    if (sel_data_(dt, "param_stool") != "")
                    {
                        content += "<item>大便：" + sel_data_(dt, "param_stool").Replace("其他", "");
                        if (sel_data_(dt, "param_stool_other") != "")
                            content += sel_data_(dt, "param_stool_other").Replace("|", ",");
                        if (sel_data_(dt, "param_stool_dtl_1") != "" || sel_data_(dt, "param_stool_dtl_2") != "")
                        {
                            content += "，" + sel_data_(dt, "param_stool_dtl_1").Replace("|", ",");
                            content += "次 /" + sel_data_(dt, "param_stool_dtl_2").Replace("|", ",") + "天";
                        }
                        if (sel_data_(dt, "span_param_stool_dtl_color") != "")
                            content += "，顏色" + sel_data_(dt, "span_param_stool_dtl_color");
                        if (sel_data_(dt, "span_param_stool_dtl_type") != "")
                            content += "，性狀" + sel_data_(dt, "span_param_stool_dtl_type");
                        content += "</item>";
                    }
                    content += "</list></item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #region 活動運動及休息睡眠
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-4\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"活動運動及休息睡眠\" />";
                content += "<title>活動運動及休息睡眠</title><text><list>";

                if (sel_data_(dt, "rb_cry") != "" || sel_data_(dt, "rb_power") != "" || sel_data_(dt, "rb_muscle_tension") != "" || sel_data_(dt, "rb_reflex_sucking") != "" || sel_data_(dt, "rb_reflex_more") != "" || sel_data_(dt, "rb_reflex_rooting") != "" || sel_data_(dt, "rb_reflex_tonic_neck") != "" || sel_data_(dt, "rb_reflex_grasp") != "" || sel_data_(dt, "rb_reflex_other") != "")
                {
                    content += "<item>活動運動<list>";
                    if (sel_data_(dt, "rb_cry") != "")
                        content += "<item>哭聲：" + sel_data_(dt, "rb_cry") + "</item>";
                    if (sel_data_(dt, "rb_power") != "")
                        content += "<item>活力：" + sel_data_(dt, "rb_power") + "</item>";
                    if (sel_data_(dt, "rb_muscle_tension") != "")
                        content += "<item>肌肉張力：" + sel_data_(dt, "rb_muscle_tension") + "</item>";
                    if (sel_data_(dt, "rb_reflex_sucking") != "" || sel_data_(dt, "rb_reflex_more") != "" || sel_data_(dt, "rb_reflex_rooting") != "" || sel_data_(dt, "rb_reflex_tonic_neck") != "" || sel_data_(dt, "rb_reflex_grasp") != "" || sel_data_(dt, "rb_reflex_other") != "")
                    {
                        content += "<item>神經反射<list>";
                        if (sel_data_(dt, "rb_reflex_sucking") != "")
                            content += "<item>吸吮反射(sucking reflex)：" + sel_data_(dt, "rb_reflex_sucking") + "</item>";
                        if (sel_data_(dt, "rb_reflex_more") != "")
                            content += "<item>莫羅氏反射(跳反射)(Moro reflex)：" + sel_data_(dt, "rb_reflex_more") + "</item>";
                        if (sel_data_(dt, "rb_reflex_rooting") != "")
                            content += "<item>覓乳反射(rooting reflex)：" + sel_data_(dt, "rb_reflex_rooting") + "</item>";
                        if (sel_data_(dt, "rb_reflex_tonic_neck") != "")
                            content += "<item>伸頸反射(tonic neck reflex)：" + sel_data_(dt, "rb_reflex_tonic_neck") + "</item>";
                        if (sel_data_(dt, "rb_reflex_grasp") != "")
                            content += "<item>抓握反射(grasp reflex)：" + sel_data_(dt, "rb_reflex_grasp") + "</item>";
                        if (sel_data_(dt, "rb_reflex_other") != "")
                            content += "<item>其他：" + sel_data_(dt, "rb_reflex_other").Replace("|", ",") + "</item>";
                        content += "</list></item>";
                    }
                    content += "</list></item>";
                }
                if (sel_data_(dt, "rb_reflex_light") != "" || sel_data_(dt, "rb_reflex_sound") != "" || sel_data_(dt, "rb_reflex_heart") != "")
                {
                    content += "<item>活動運動<list>";
                    if (sel_data_(dt, "rb_reflex_light") != "")
                        content += "<item>對光線刺激反應：" + sel_data_(dt, "rb_reflex_light") + "</item>";
                    if (sel_data_(dt, "rb_reflex_sound") != "")
                        content += "<item>對聲音刺激反應：" + sel_data_(dt, "rb_reflex_sound") + "</item>";
                    if (sel_data_(dt, "rb_reflex_heart") != "")
                        content += "<item>對痛刺激敏感度：" + sel_data_(dt, "rb_reflex_heart") + "</item>";
                    content += "</list></item>";
                }
                if (sel_data_(dt, "rb_sleep") != "")
                    content += "<item>睡眠休息：" + sel_data_(dt, "rb_sleep") + "</item>";

                content += "</list></text></section></component>";
                #endregion

                #region 泌尿生殖
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-5\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"泌尿生殖\" />";
                content += "<title>泌尿生殖</title><text><list>";

                if (sel_data_(dt, "param_Sex") != "")
                {
                    content += "<item>泌尿生殖：" + sel_data_(dt, "param_Sex") + "<list>";
                    if (sel_data_(dt, "param_Testicular") != "")
                    {
                        content += "<item>睪丸：" + sel_data_(dt, "param_Testicular");
                        if (sel_data_(dt, "param_Testicular_dtl") != "")
                            content += "，" + sel_data_(dt, "param_Testicular_dtl");
                        content += "</item>";
                    }
                    if (sel_data_(dt, "param_Urethra") != "")
                        content += "<item>尿道口：" + sel_data_(dt, "param_Urethra") + "</item>";
                    if (sel_data_(dt, "param_Scrotum") != "")
                        content += "<item>陰囊：" + sel_data_(dt, "param_Scrotum") + "</item>";
                    if (sel_data_(dt, "param_Man_Ass") != "")
                        content += "<item>肛門：" + sel_data_(dt, "param_Man_Ass") + "</item>";
                    if (sel_data_(dt, "param_Man_Secretions") != "")
                        content += "<item>分泌物：" + sel_data_(dt, "param_Man_Secretions") + "</item>";
                    if (sel_data_(dt, "param_Sex_Man_other") != "")
                        content += "<item>其他：" + sel_data_(dt, "param_Sex_Man_other").Replace("|", ",") + "</item>";
                    if (sel_data_(dt, "param_Vaginal") != "")
                        content += "<item>陰道口：" + sel_data_(dt, "param_Vaginal") + "</item>";
                    if (sel_data_(dt, "param_Labia") != "")
                        content += "<item>陰唇腫：" + sel_data_(dt, "param_Labia") + "</item>";
                    if (sel_data_(dt, "param_Woman_Ass") != "")
                        content += "<item>肛門：" + sel_data_(dt, "param_Woman_Ass") + "</item>";
                    if (sel_data_(dt, "param_Woman_Secretions") != "")
                        content += "<item>分泌物：" + sel_data_(dt, "param_Woman_Secretions") + "</item>";
                    if (sel_data_(dt, "param_Sex_Woman_other") != "")
                        content += "<item>其他：" + sel_data_(dt, "param_Sex_Woman_other").Replace("|", ",") + "</item>";
                    content += "</list></item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #region 聯絡資料
                content += "<component typeCode=\"COMP\" contextConductionInd=\"true\">";
                content += "<section classCode=\"DOCSECT\" moodCode=\"EVN\">";
                content += "<code code=\"46240-6\" codeSystem=\"2.16.840.1.113883.6.1\" codeSystemName=\"LOINC\" displayName=\"聯絡資料\" />";
                content += "<title>聯絡資料</title><text><list>";

                if (sel_data_(dt, "param_EMGContact") != "")
                {
                    content += "<item>聯絡資料<list>";
                    string[] Name = sel_data_(dt, "param_EMGContact").Split(',');
                    string[] Role_other = sel_data_(dt, "param_ContactRole_other").Split(',');
                    string[] Phone_1 = sel_data_(dt, "param_EMGContact_1").Split(',');
                    string[] Phone_2 = sel_data_(dt, "param_EMGContact_2").Split(',');
                    string[] Phone_3 = sel_data_(dt, "param_EMGContact_3").Split(',');
                    for (int i = 0; i < Name.Length; i++)
                    {
                        content += "<item>緊急聯絡人姓名：" + Name[i].Replace("|", ",");
                        if (i == 0)
                            content += "，稱謂：" + sel_data_(dt, "param_ContactRole").Replace("其他", "") + " " + Role_other[i].Replace("|", ",");
                        else
                            content += "，稱謂：" + sel_data_(dt, "param_ContactRole_" + i.ToString()).Replace("其他", "") + " " + Role_other[i].Replace("|", ",");
                        content += "，連絡電話-公司：" + Phone_1[i];
                        content += "，連絡電話-住家：" + Phone_2[i];
                        content += "，連絡電話-手機：" + Phone_3[i];
                        content += "</item>";
                    }
                    content += "</list></item>";
                }

                content += "</list></text></section></component>";
                #endregion

                #endregion
            }

            return content;
        }

        #region Other_Function

        /// <summary>
        /// 轉換特殊字元
        /// </summary>
        private string trans_special_code(string content)
        {
            if (content != null)
            {
                content = content.Trim();
                content = content.Replace("&", "&amp;");
                content = content.Replace("<", "&lt;");
                content = content.Replace(">", "&gt;");
                content = content.Replace("'", "&apos;");
                content = content.Replace("\"", "&quot;");

                content = content.Replace("\u0001", "");
                content = content.Replace("\u000B", "");                
                content = content.Replace("\r\n", "&#xD;&#xA;");
                content = content.Replace("\n", "&#xA;");
                return content;
            }
            else
                return "";
        }

        /// <summary>
        /// 取得評估項目的值
        /// </summary>
        /// <param name="id">欲搜尋ID</param>
        private string sel_data_(DataTable dt, string id)
        {
            string value = "";
            foreach (DataRow r in dt.Rows)
            {
                if (r["ITEMID"].ToString() == id)
                    value = trans_special_code(r["ITEMVALUE"].ToString());
            }
            return value;
        }

        #endregion
    }
}