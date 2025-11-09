using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using NIS.Models;
using System.Data;
using NIS.Data;
using NIS.UtilTool;
using Newtonsoft.Json;
//using iTextSharp.text;
//using iTextSharp.text.pdf;
using System.Drawing;
using NIS.Models.DBModel;
using Oracle.ManagedDataAccess.Client;
using System.Net.Http;
using System.Net;
using iTextSharp.text.pdf.qrcode;
using System.Windows.Interop;

namespace NIS.Controllers //護理紀錄與特殊事件註記共用此controller
{

    public class CareRecordController : BaseController
    {
        private CareRecord care_record_m;
        DBConnector link = new DBConnector();
        // private CARESCAPEGateway.CARESCAPEGateway EKG_ws;
        EMRReference.Service1 emr = new EMRReference.Service1();
        CARESCAPEGateway.CARESCAPEGateway EKG_ws = new CARESCAPEGateway.CARESCAPEGateway();

        public CareRecordController()
        {
            this.care_record_m = new CareRecord();
            EKG_ws.Url = MvcApplication.iniObj.NisSetting.Connection.EKGUrl;
        }

        #region 護理紀錄
        //檢傷
        public ActionResult GetTriageInfo()
        {
            byte[] TriageInfoByteCode = webService.GetTriageInfo(ptinfo.FeeNo);

            if (TriageInfoByteCode != null)
            {
                string TriageInfoJosnArr = NIS.UtilTool.CompressTool.DecompressString(TriageInfoByteCode);
                List<TriageInfo> TriageInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<List<TriageInfo>>(TriageInfoJosnArr);
                ViewData["TriageInfo"] = TriageInfo;
            }
            return View();
        }
        //首頁
        [HttpGet]
        public ActionResult List()
        {
            try
            {
                if (Session["Complement_List"] == null)
                {
                    string jsonstr = "{\"Status\":" + "false}";
                    Complement_List c_list = JsonConvert.DeserializeObject<Complement_List>(jsonstr);
                    Session["Complement_List"] = c_list;
                }
                string tmp_Complement_List = Session["Complement_List"].ToString();
                if (Session["PatInfo"] != null)
                {
                    string feeno = ptinfo.FeeNo;
                    DateTime now = DateTime.Now;
                    DataTable dt = care_record_m.sel_carerecord(feeno, "", "", now.ToString("yyyy/MM/dd 00:00"), now.ToString("yyyy/MM/dd HH:mm:59"), "");
                    dt = this.care_record_m.getRecorderName(dt);

                    UserInfo ui = (UserInfo)Session["ui"];

                    var category = userinfo.Category;
                    if (string.IsNullOrWhiteSpace(userinfo.Category))
                    {
                        category = ui.Category.ToString();
                    }
                    ViewBag.Category = category;
                    ViewBag.userno = userinfo.EmployeesNo;

                    ViewBag.feeno = feeno;
                    ViewBag.RootDocument = GetSourceUrl();
                    ViewBag.dt = dt;
                    //string titleStr = "總膽紅素檢測";
                    //foreach (DataRow dr in dt.Rows)
                    //{
                    //    //修改 Intake 總膽紅素
                    //    if (dr["SELF"].ToString() == "gi_j")
                    //    {
                    //        //修改 Intake 總膽紅素
                    //        DBConnector dbLink = new DBConnector();
                    //        List<DBItem> dsList = new List<DBItem>() { new DBItem("TITLE", titleStr, DBItem.DBDataType.String) };
                    //        int row = dbLink.DBExecUpdate("CARERECORD_DATA", dsList, string.Concat(" carerecord_id = '", dr["carerecord_id"].ToString(), "' AND SELF = 'gi_j'"));
                    //        if (row > 0)
                    //        {
                    //            //給畫面顯示
                    //            dr["TITLE"] = titleStr;
                    //        }
                    //    }
                    //}

                    return View();
                }
            }
            catch (Exception e)
            {
                //Do nothing
            }

            Response.Write("<script>alert('請重新選擇病患');</script>");
            return new EmptyResult();
        }

        //首頁
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult List(FormCollection form)
        {
            try
            {
                if (Session["PatInfo"] != null)
                {
                    string feeno = ptinfo.FeeNo;
                    DateTime start = Convert.ToDateTime(form["start_date"] + " " + form["start_time"]);
                    DateTime end = Convert.ToDateTime(form["end_date"] + " " + form["end_time"]);

                    if (form["CP"] == "Y")
                    {
                        DataTable dt = care_record_m.get_cp_record(feeno);
                        dt = this.care_record_m.getRecorderName(dt);
                        ViewBag.dt = dt;
                    }
                    else
                    {
                        //11 / 19 護理紀錄 護理計畫修改傳變數至前端 NEW
                        //string subtype = form["subtype"];
                        //ViewBag.start_date = start;
                        //ViewBag.end_date = end;
                        //ViewBag.Category = userinfo.Category.ToString();
                        //ViewBag.userno = userinfo.EmployeesNo;
                        //ViewBag.feeno = feeno;
                        //ViewBag.RootDocument = GetSourceUrl();
                        DataTable dt = care_record_m.sel_carerecord(feeno, "", "", start.ToString("yyyy/MM/dd HH:mm"), end.ToString("yyyy/MM/dd HH:mm:59"), "");
                        dt = this.care_record_m.getRecorderName(dt);
                        ViewBag.dt = dt;
                    }
                    //11 / 19 護理紀錄 護理計畫修改傳變數至前端 OLD
                    string subtype = form["subtype"];
                    ViewBag.start_date = start;
                    ViewBag.end_date = end;
                    ViewBag.Category = userinfo.Category.ToString();
                    ViewBag.userno = userinfo.EmployeesNo;
                    ViewBag.feeno = feeno;
                    ViewBag.RootDocument = GetSourceUrl();

                    return View();
                }
            }
            catch (Exception e)
            {
                //Do nothing
            }

            Response.Write("<script>alert('請重新選擇病患');</script>");
            return new EmptyResult();
        }

        //列印
        public ActionResult List_PDF(string feeno, string starttime, string endtime, string id, string fromVIEW = null)
        {
            PatientInfo pinfo = new PatientInfo();
            byte[] ByteCode = webService.GetPatientInfo(feeno);
            //病人資訊
            if (ByteCode != null)
                pinfo = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(ByteCode));
            ViewData["ptinfo"] = pinfo;
            if (!string.IsNullOrWhiteSpace(id))
            {
                DataTable dt = care_record_m.sel_carerecord(id);
                dt = this.care_record_m.getRecorderName(dt);
                ViewBag.dt = dt;
            }
            else
            {
                DateTime start = Convert.ToDateTime(starttime.Replace('|', ' '));
                DateTime end = Convert.ToDateTime(endtime.Replace('|', ' '));


                DataTable dt = care_record_m.sel_carerecord(feeno, "", "", start.ToString("yyyy/MM/dd HH:mm"), end.ToString("yyyy/MM/dd HH:mm:59"), "","", fromVIEW);
                dt = this.care_record_m.getRecorderName(dt);
                ViewBag.dt = dt;
            }

            return View();
        }

        //新增_首頁
        [HttpGet]
        public ActionResult Insert(string id, string type)
        {
            if (Session["PatInfo"] != null)
            {
                ViewBag.dt = care_record_m.sel_carerecord(ptinfo.FeeNo, "", id, "", "", "", type);
                string[] NursePlan_nameList = care_record_m.sel_NursePlan(ptinfo.FeeNo).AsEnumerable().Select(r => r.Field<string>("TITLE")).ToArray();
                ViewBag.dt_NursePlan = NursePlan_nameList;
                ViewBag.userno = userinfo.EmployeesNo;
                ViewBag.feeno = ptinfo.FeeNo;
                ViewBag.type = type;
                return View();
            }
            Response.Write("<script>alert('請重新選擇病患');</script>");
            return new EmptyResult();
        }

        //新增
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Insert(FormCollection form)
        {
            if (Session["PatInfo"] != null)
            {
                string date = form["txt_day"] + " " + form["txt_time"];
                string id = creatid("CARERECORD", userinfo.EmployeesNo, ptinfo.FeeNo, "0");
                string userno = userinfo.EmployeesNo;
                string feeno = ptinfo.FeeNo;
                string sign_userno = care_record_m.sel_guide_userno(userno, Convert.ToDateTime(date), Convert.ToDateTime(date).Hour);
                DateTime NowTime = DateTime.Now;
                List<DBItem> insertDataList = new List<DBItem>();

                insertDataList.Add(new DBItem("CARERECORD_ID", id, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("GUIDE_NO", sign_userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATTIME", NowTime.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("RECORDTIME", date, DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("TITLE", base.trans_date(form["title"]), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("C", base.trans_date(form["record_com"]), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("S", base.trans_date(form["record_s"]), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("O", base.trans_date(form["record_o"]), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("I", base.trans_date(form["record_i"]), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("E", base.trans_date(form["record_e"]), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("C_OTHER", base.trans_date(form["hid_record_com"]), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("S_OTHER", base.trans_date(form["hid_record_s"]), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("O_OTHER", base.trans_date(form["hid_record_o"]), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("I_OTHER", base.trans_date(form["hid_record_i"]), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("E_OTHER", base.trans_date(form["hid_record_e"]), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("SELF", "CARERECORD", DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("SIGN", "N", DBItem.DBDataType.String));

                int erow = care_record_m.DBExecInsert("CARERECORD_DATA", insertDataList);
                string msg = "";
                if (trans_date(base.trans_date(form["record_com"])) != "")
                    msg += trans_date(base.trans_date(form["record_com"])) + "\n";// + trans_date(C)

                if (trans_date(base.trans_date(form["record_s"])) != "")
                    msg += "S:" + trans_date(base.trans_date(form["record_s"])) + "\n";// + trans_date(S) 

                if (trans_date(base.trans_date(form["record_o"])) != "")
                    msg += "O:" + trans_date(base.trans_date(form["record_o"])) + "\n";//+ trans_date(O) 

                if (trans_date(base.trans_date(form["record_i"])) != "")
                    msg += "I:" + trans_date(base.trans_date(form["record_i"])) + "\n";// + trans_date(I) 

                if (trans_date(base.trans_date(form["record_e"])) != "")
                    msg += "E:" + trans_date(base.trans_date(form["record_e"])) + "\n";// + trans_date(E)

                #region 處理EKG圖片
                if (!string.IsNullOrWhiteSpace(form["ECKImgDate"]))
                {
                    try
                    {
                        link.DBCmd.CommandText = "UPDATE CARERECORD_DATA SET EKG = :EKG "
                                + " WHERE FEENO = '" + base.ptinfo.FeeNo + "' AND CARERECORD_ID = '" + id + "' ";
                        //link.DBCmd.Parameters.Add(":EKG", SqlDbType.Binary).Value = Convert.FromBase64String(form["ECKImgDate"]);
                        //link.DBCmd.Parameters.Add(":EKG", Convert.FromBase64String(form["ECKImgDate"]));
                        byte[] arr = Convert.FromBase64String(form["ECKImgDate"]);
                        link.DBCmd.Parameters.Add(":EKG", OracleDbType.Blob).Value = arr;

                        link.DBOpen();
                        link.DBCmd.ExecuteNonQuery();
                        link.DBClose();

                        arr = null;
                    }
                    catch (Exception ex)
                    {
                        //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                        string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                        string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                        write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                    }
                    finally
                    {
                        this.link.DBClose();
                    }
                }
                
                #endregion

                #region --簽章--

                byte[] allergen = webService.GetAllergyList(feeno);
                string ptJsonArr = string.Empty;
                string allergyDesc = string.Empty;
                if (allergen != null)
                {
                    ptJsonArr = CompressTool.DecompressString(allergen);
                }
                List<NIS.Data.PatientInfo> patList = JsonConvert.DeserializeObject<List<NIS.Data.PatientInfo>>(ptJsonArr);

                if (allergen != null)
                {
                    allergyDesc = patList[0].AllergyDesc;
                }
                string xml = care_record_m.care_Record_Get_xml(ptinfo.PatientName, ptinfo.ChartNo,
                    ptinfo.PatientGender, (ptinfo.Age).ToString(), ptinfo.BedNo, ptinfo.InDate.ToString("yyyyMMdd"),
                    ptinfo.InDate.ToString("HHmm"), allergyDesc, Convert.ToDateTime(date).ToString("yyyyMMdd"),
                    Convert.ToDateTime(date).ToString("HHmm"), userinfo.EmployeesName, base.trans_date(form["title"]), this.GetRecordStr(id + "CARERECORD"), form["ECKImgDate"]);



                #endregion
                //儲存成功
                if (erow > 0)
                {
                    //將紀錄回寫至 EMR Temp Table
                    ////string sqlstr = "begin P_NIS_EMRMS('" + feeno + "','014','護理紀錄單','" + id + "CARERECORD','" + date + "','" + sign_userno + "','I');end;";
                    ////care_record_m.DBExec(sqlstr);
                    #region --EMR--
                    //取得應簽章人員
                    byte[] listByteCode = webService.UserName(userinfo.Guider);
                    string listJsonArray = CompressTool.DecompressString(listByteCode);
                    UserInfo user_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                    string RecordTime = NowTime.ToString("yyyy/MM/dd HH:mm:ss");
                    string Temp_NowTime_Str = Convert.ToDateTime(RecordTime).ToString("yyyyMMddHHmmss");//時間採統一變數
                    string EmrXmlString = this.get_xml(
                        NowTime.ToString("yyyyMMddHHmmss.fffffff"), Temp_NowTime_Str + GetMd5Hash(id + "CARERECORD"), "A000040", Convert.ToDateTime(date).ToString("yyyyMMdd"),
                        GetMd5Hash(id + "CARERECORD"), Convert.ToDateTime(date).ToString("yyyyMMdd"), "", "",
                        user_info.EmployeesNo, user_info.EmployeesName, user_info.UserID, ptinfo.ChartNo, ptinfo.PatientName,
                        ptinfo.PatientID, ptinfo.PayInfo,
                        "C:\\EMR\\", "A000040" + GetMd5Hash(id + "CARERECORD") + Temp_NowTime_Str + ".xml", listJsonArray, "Insert"
                        );
                    erow = EMR_Sign(date, id , msg, base.trans_date(form["title"]), "CARERECORD");

                    //SaveEMRLogData(id + "CARERECORD", GetMd5Hash(id + "CARERECORD"), "EMR", RecordTime, "A000040" + GetMd5Hash(id + "CARERECORD") + Temp_NowTime_Str, xml);
                    //SaveEMRLogData(id + "CARERECORD", GetMd5Hash(id + "CARERECORD"), "Temp", RecordTime, Temp_NowTime_Str + "-" + GetMd5Hash(id + "CARERECORD"), EmrXmlString);
                    #endregion

                    #region JAG 簽章
                    ////// 20150608 EMR
                    ////string strPath = @"C:\\wkhtmltopdf\\wkhtmltopdf.exe";
                    ////string filename = @"C:\inetpub\NIS\Images\" + id + "CARERECORD.pdf";

                    ////string url = Request.Url.AbsoluteUri.ToString().Replace("Insert", "List_PDF?id=" + id + "CARERECORD&feeno=" + feeno);
                    ////Process p = new Process();
                    ////p.StartInfo.FileName = strPath;
                    ////p.StartInfo.Arguments = url + " " + filename;
                    ////p.StartInfo.UseShellExecute = true;
                    ////p.Start();
                    ////p.WaitForExit();

                    ////byte[] listByteCode = nis.UserName(userinfo.EmployeesNo);
                    ////string listJsonArray = CompressTool.DecompressString(listByteCode);
                    ////UserInfo user_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                    ////string emp_id = (user_info.UserID != null) ? user_info.UserID.Trim() : "";
                    ////string emp_name = (user_info.EmployeesName != null) ? user_info.EmployeesName.Trim() : "";
                    ////string dep_no = ptinfo.DeptNo;
                    ////string chr_no = ptinfo.ChartNo;
                    ////string pat_name = ptinfo.PatientName;
                    ////string in_date = ptinfo.InDate.ToString("yyyyMMdd");
                    ////string chagre_type = (ptinfo.PayInfo == "健保") ? "001" : "000";
                    ////int result = emr_sign(id + "CARERECORD", feeno, "014", userno, emp_name, emp_id, dep_no, chr_no, pat_name, in_date, chagre_type, filename);
                    #endregion

                    Response.Write("<script>alert('新增成功');window.location.href='List';</script>");
                }
                else
                    Response.Write("<script>alert('新增失敗');window.location.href='List';</script>");

                return new EmptyResult();
            }
            //session過期
            Response.Write("<script>alert('請重新選擇病患');</script>");
            return new EmptyResult();
        }

        //更新
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Update(FormCollection form)
        {
            if (Session["PatInfo"] != null)
            {              
                string date = form["txt_day"] + " " + form["txt_time"];
                int erow = upd_CareRecord(date, form["id"], form["title"], form["record_com"], form["record_s"], form["record_o"], form["record_i"], form["record_e"], form["ECKImgDate"], form["emrid"], form["self"], "");
                string url_href = "";
                //儲存成功
                if (!string.IsNullOrEmpty(form["source_type"]))
                {
                    url_href = "../NurseCarePlan/List";
                }
                else
                {
                    url_href = "List";
                }
                if (erow > 0)
                    Response.Write("<script>alert('更新成功');window.location.href='"+ url_href + "';</script>");
                else
                    Response.Write("<script>alert('更新失敗');window.location.href='"+ url_href + "';</script>");

                return new EmptyResult();
            }

            //session過期
            Response.Write("<script>alert('請重新選擇病患');</script>");
            return new EmptyResult();
        }

        //刪除
        [HttpPost]
        public string Delete(string id, string self)
        {
            int erow = base.Del_CareRecord(id, self);

            if (erow > 0)
                return "刪除成功";
            else
                return "刪除失敗";
        }

        #endregion

        #region 特殊事件

        //列表
        [HttpGet]
        public ActionResult Special_Event()
        {//判斷有無病人session
            if (Session["PatInfo"] != null)
            {
                DataTable dt = care_record_m.get_special_event(ptinfo.FeeNo, "");
                ViewBag.userno = userinfo.EmployeesNo;
                ViewBag.dt_care_plan_master = care_record_m.GetCarePlan_Master(ptinfo.FeeNo);

                dt.Columns.Add("username");
                if (dt.Rows.Count > 0)
                {
                    string userno = dt.Rows[0]["CREATNO"].ToString();
                    byte[] listByteCode = webService.UserName(userno);
                    string listJsonArray = CompressTool.DecompressString(listByteCode);
                    UserInfo user_name = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                    foreach (DataRow r in dt.Rows)
                    {
                        if (userno != r["CREATNO"].ToString())
                        {
                            userno = r["CREATNO"].ToString();
                            listByteCode = webService.UserName(userno);
                            if (listByteCode != null)
                            {
                                listJsonArray = CompressTool.DecompressString(listByteCode);
                                user_name = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                            }
                        }
                        r["username"] = user_name.EmployeesName;
                    }
                }
                ViewBag.dt = dt;
            }
            else
            {
                Response.Write("<script>alert('請重新選擇病患');</script>");
                return new EmptyResult();
            }
            return View();
        }

        //主頁
        public ActionResult Special_Event_Index(string id)
        {
            if (id != null)
                ViewBag.dt = care_record_m.get_special_event(ptinfo.FeeNo, id);
            return View();
        }

        //新增
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Insert_Special_Event(FormCollection form)
        {
            string type_id = form["type"].ToString(), title = "", content = "", care_content = "";
            string id = base.creatid("EVENT", userinfo.EmployeesNo, ptinfo.FeeNo, "0");
            string temp_item = "";

            if (type_id == "0")
            {
                title = "Sent Patient to";
                care_content = "病人因 " + form["st"].ToString() + "，送至 " + form["st1"].ToString() + "。";
                content = "Sent Patient to " + form["st1"].ToString() + " at " + form["txt_time"] + "";
                temp_item = form["st"].ToString() + "|" + form["st1"].ToString();
            }
            else if (type_id == "1")
            {
                title = "Transferred";
                //care_content = form["txt_amount_content"];
                //content = form["txt_amount"];
                care_content = form["txt_amount_content"];
                content = care_content;
                content = "Transferred to " + form["txt_amount"].ToString() + " at " + form["txt_time"] + "";
                temp_item = form["txt_amount"].ToString() + "|" + form["txt_amount_content"].ToString();
            }
            else if (type_id == "2")
            {
                title = "Delivered";
                care_content = "Delivered at " + form["txt_time"];
                content = care_content;
            }
            else if (type_id == "3")
            {
                title = "手術日"; //護理紀錄裡面可以帶出標題
                //care_content = "OP day";
                care_content = "病人於" + form["txt_day"] + " " + form["txt_time"] + "送至手術室。";
                content = care_content;
            }
            else if (type_id == "4")
            {
                title = "特殊醫療處置";
                care_content = form["txt_special_content"];
                content = care_content;
            }
            else if (type_id == "7")
            {
                title = "Refuse";
                care_content = "Refuse at  " + form["txt_time"]; ;
                content = "Refuse at  " + form["txt_time"];
            }
            else if (type_id == "8")
            {
                title = "外出";
                care_content = "Patient out at " + form["txt_time"];
                content = care_content;
            }
            else if (type_id == "9")
            {
                title = "特殊處置";
                care_content = "Ice pillow use at " + form["txt_time"];
                content = care_content;
            }
            else if (type_id == "10")
            {
                title = "返室";
                care_content = "病人返室 at " + form["txt_time"];
                content = care_content;
            }
            else if (type_id == "11")
            {
                title = "安寧會診紀錄";
                care_content = form["txt_special_content"];
                content = care_content;
            }
            else if (type_id == "12")
            {
                title = "出生";
                care_content = "Born at " + form["txt_time"];
                content = care_content;
            }
            else if (type_id == "13")
            {
                title = "ROSC";
                care_content = form["txt_content_13"];
                content = "ROSC at " + form["txt_time"] + "。";
                temp_item = form["txt_content_13"];
            }
            else if (type_id == "14")
            {
                title = "新生兒篩檢";
                care_content = form["txt_time"] + "執行新生兒篩檢。";
                content = form["txt_time"] + "執行新生兒篩檢。";
            }
            else if (type_id == "15")
            {
                title = "HBIG注射";
                care_content = form["txt_time"] + "執行HBIG注射。";
                content = form["txt_time"] + "執行HBIG注射。";
            }
            else if (type_id == "16")
            {
                title = "B型肝炎注射";
                care_content = form["txt_time"] + "執行B型肝炎注射。";
                content = form["txt_time"] + "執行B型肝炎注射。";
            }
            else if (type_id == "17")
            {
                title = "照光";
                care_content = "開始照光";
                content = form["txt_time"] + " on Photo。";

            }
            else if (type_id == "18")
            {
                title = "照光";
                care_content = "結束照光";
                content = form["txt_time"] + " off Photo。";

            }
            else if (type_id == "19")
            {
                title = "緩和醫療照會紀錄";
                care_content = form["txt_special_content"];
                content = care_content;
            }
            else if (type_id == "20")
            {
                title = "安寧共照照會紀錄";
                care_content = form["txt_special_content"];
                content = care_content;
            }

            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("EVENT_ID", id, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FEENO", ptinfo.FeeNo, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATNO", userinfo.EmployeesNo, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATTIME", form["txt_day"] + " " + form["txt_time"], DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("TYPE_ID", type_id, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CONTENT", content, DBItem.DBDataType.String));
            if (temp_item != "")
                insertDataList.Add(new DBItem("EDIT_ITEM", temp_item, DBItem.DBDataType.String));

            int erow = care_record_m.DBExecInsert("NIS_SPECIAL_EVENT_DATA", insertDataList);
            if (erow > 0)
            {
                if (type_id != "9")
                {
                    erow = base.Insert_CareRecord(form["txt_day"] + " " + form["txt_time"], id, title, care_content, "", "", "", "", "SPE_EVENT");
                }
                else
                {
                    erow = base.Insert_CareRecord(form["txt_day"] + " " + form["txt_time"], id, title, "", "", "", care_content, "", "SPE_EVENT");
                }
                Response.Write("<script>alert('新增成功');window.close();window.opener.location.reload();</script>");
            }
            else
                Response.Write("<script>alert('新增失敗');window.close();window.opener.location.reload();</script>");

            return new EmptyResult();
        }

        //特殊事件
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Upd_Special_Event(FormCollection form)
        {
            string type_id = form["type"].ToString(), title = "", content = "", care_content = "";
            string temp_item = "";

            if (type_id == "0")
            {
                title = "Sent Patient to";
                care_content = "病人因 " + form["st"].ToString() + "，送至 " + form["st1"].ToString() + "。";
                content = "Sent Patient to " + form["st1"].ToString() + " at " + form["txt_time"] + "";
                temp_item = form["st"].ToString() + "|" + form["st1"].ToString();
            }
            else if (type_id == "1")
            {
                title = "Transferred";
                //care_content = form["txt_amount_content"];
                //content = form["txt_amount"];
                care_content = form["txt_amount_content"];
                content = care_content;
                content = "Transferred to " + form["txt_amount"].ToString() + " at " + form["txt_time"] + "";
                temp_item = form["txt_amount"].ToString() + "|" + form["txt_amount_content"].ToString();
            }
            else if (type_id == "2")
            {
                title = "Delivered";
                care_content = "Delivered at " + form["txt_time"];
                content = care_content;
            }
            else if (type_id == "3")
            {

                care_content = "OP day";
                content = care_content;
            }
            else if (type_id == "4")
            {
                title = "特殊醫療處置";
                care_content = form["txt_special_content"];
                content = care_content;
            }
            else if (type_id == "7")
            {
                title = "Refuse";
                care_content = "Refuse at  " + form["txt_time"];
                content = "Refuse at  " + form["txt_time"];
            }
            else if (type_id == "8")
            {
                title = "外出";
                care_content = "Patient out at " + form["txt_time"];
                content = care_content;
            }
            else if (type_id == "9")
            {
                title = "特殊處置";
                care_content = "Ice pillow use at " + form["txt_time"];
                content = care_content;
            }
            else if (type_id == "10")
            {
                title = "返室";
                care_content = "病人返室 at " + form["txt_time"];
                content = care_content;
            }
            else if (type_id == "11")
            {
                title = "安寧會診紀錄";
                care_content = form["txt_special_content"];
                content = care_content;
            }
            else if (type_id == "12")
            {
                care_content = "Born at " + form["txt_time"];
                content = care_content;
            }
            else if (type_id == "13")
            {
                title = "ROSC";
                care_content = form["txt_content_13"];
                content = "ROSC at " + form["txt_time"] + "。";
                temp_item = form["txt_content_13"];
            }
            else if (type_id == "14")
            {
                title = "新生兒篩檢";
                care_content = form["txt_time"] + "執行新生兒篩檢。";
                content = form["txt_time"] + "執行新生兒篩檢。";
            }
            else if (type_id == "15")
            {
                title = "HBIG注射";
                care_content = form["txt_time"] + "執行HBIG注射。";
                content = form["txt_time"] + "執行HBIG注射。";
            }
            else if (type_id == "16")
            {
                title = "B型肝炎注射";
                care_content = form["txt_time"] + "執行B型肝炎注射。";
                content = form["txt_time"] + "執行B型肝炎注射。";
            }

            else if (type_id == "17")
            {
                title = "照光";
                care_content = "開始照光";
                content = form["txt_time"] + " on Photo。";

            }
            else if (type_id == "18")
            {
                title = "照光";
                care_content = "結束照光";
                content = form["txt_time"] + " off Photo。";

            }
            else if (type_id == "19")
            {
                title = "緩和醫療照會紀錄";
                care_content = form["txt_special_content"];
                content = care_content;
            }
            else if (type_id == "20")
            {
                title = "安寧共照照會紀錄";
                care_content = form["txt_special_content"];
                content = care_content;
            }
            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("CREATTIME", form["txt_day"] + " " + form["txt_time"], DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("CONTENT", content, DBItem.DBDataType.String));
            if (temp_item != "")
                insertDataList.Add(new DBItem("EDIT_ITEM", temp_item, DBItem.DBDataType.String));
            string where = "EVENT_ID = '" + form["event_id"] + "' AND FEENO='" + ptinfo.FeeNo + "'";
            int erow = 0;
            erow = care_record_m.DBExecUpdate("NIS_SPECIAL_EVENT_DATA", insertDataList, where);

            if (erow > 0)
            {
                erow = base.Upd_CareRecord(form["txt_day"] + " " + form["txt_time"], form["event_id"], title, care_content, "", "", "", "", "SPE_EVENT");
                Response.Write("<script>alert('修改成功');window.close();window.opener.location.reload();</script>");
            }
            else
                Response.Write("<script>alert('修改失敗');window.close();window.opener.location.reload();</script>");

            return new EmptyResult();
        }

        //刪除
        public ActionResult Del_Special_Event(string id)
        {
            string where = "EVENT_ID = '" + id + "' AND FEENO='" + ptinfo.FeeNo + "'";
            int erow = care_record_m.DBExecDelete("NIS_SPECIAL_EVENT_DATA", where);
            if (erow > 0)
            {
                erow = base.Del_CareRecord(id, "SPE_EVENT");
                Response.Write("<script>alert('刪除成功');window.location.href='Special_Event';</script>");
            }
            else
                Response.Write("<script>alert('刪除失敗');window.location.href='Special_Event';</script>");

            return new EmptyResult();
        }

        #endregion

        #region 模組

        //呼吸療法
        public ActionResult Breath(string id)
        {
            ViewBag.id = id;
            return View();
        }

        //Vital_Sign
        public ActionResult Vital_Sign(string id, string starttime, string endtime)
        {
            try
            {
                string start = DateTime.Now.ToString("yyyy/MM/dd 00:00");
                string end = DateTime.Now.AddMinutes(2).ToString("yyyy/MM/dd HH:mm");
                if (starttime != null && endtime != null)
                {
                    start = starttime;
                    end = endtime;
                }
                ViewBag.start = start;
                ViewBag.end = end;

                //確認是否有病人資料(有選取病人)
                if (Session["PatInfo"] != null)
                {
                    //宣告必須要使用到的變數
                    //DBConnector link = new DBConnector();
                    List<VitalSignDataList> vsList = new List<VitalSignDataList>();
                    List<string[]> vsId = new List<string[]>();
                    VitalSignDataList vsdl = null;

                    //取得vs_id
                    string sqlstr = " select CREATE_DATE,vs_id from data_vitalsign where fee_no = '" + ptinfo.FeeNo + "' ";
                    sqlstr += "and create_date between to_date('" + start + "','yyyy/MM/dd hh24:mi:ss') and to_date('" + end + "','yyyy/MM/dd hh24:mi:ss') ";
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
                        vsdl = new VitalSignDataList();
                        sqlstr = " select vsd.*, to_char(modify_date,'yyyy/MM/dd hh24:mi:ss') as m_date ";
                        sqlstr += " from data_vitalsign vsd ";
                        sqlstr += " where fee_no ='" + ptinfo.FeeNo + "' and vs_id = '" + vsId[i][0] + "' ";
                        sqlstr += " and create_date = to_date('" + Convert.ToDateTime(vsId[i][1]).ToString("yyyy/MM/dd HH:mm") + "','yyyy/MM/dd hh24:mi:ss')";

                        vsdl.vsid = vsId[i][0];
                        Dt = link.DBExecSQL(sqlstr);
                        if (Dt.Rows.Count > 0)
                        {
                            for (int j = 0; j < Dt.Rows.Count; j++)
                            {
                                vsdl.DataList.Add(new VitalSignData(
                                Dt.Rows[j]["vs_item"].ToString().Trim(),
                                Dt.Rows[j]["vs_part"].ToString().Trim(),
                                Dt.Rows[j]["vs_record"].ToString().Trim(),
                                Dt.Rows[j]["vs_reason"].ToString().Trim(),
                                Dt.Rows[j]["vs_memo"].ToString().Trim(),
                                Dt.Rows[j]["vs_other_memo"].ToString().Trim(),
                                "", "", "",
                                Dt.Rows[j]["m_date"].ToString().Trim()
                                ));
                            }
                        }
                        vsList.Add(vsdl);
                        vsdl = null;
                    }
                    ViewData["VSData"] = vsList;
                    ViewBag.set_record = new Func<List<VitalSignData>, string, string>(set_record);
                    ViewBag.id = id;
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
            finally
            {
                this.link.DBClose();
            }
        }

        public string set_record(List<VitalSignData> VSDataList, string Item)
        {
            List<string> rtStr = new List<string>();
            for (int i = 0; i <= VSDataList.Count - 1; i++)
            {
                if (VSDataList[i].vs_item.Trim() == Item.Trim() || Item == "")
                {
                    return VSDataList[i].vs_record;
                }
            }
            return "";
        }

        //管路
        public ActionResult Tube(string id)
        {
            if (Session["PatInfo"] != null)
            {
                string feeno = ptinfo.FeeNo;
                string userno = userinfo.EmployeesNo;
                ViewBag.id = id;
                TubeManager tubem = new TubeManager();
                ViewBag.dt_tube = tubem.sel_tube(feeno, "", "", "");
                return View();
            }
            //session過期
            Response.Write("<script>alert('請重新選擇病患');window.close();</script>");
            return new EmptyResult();
        }

        //給藥
        public ActionResult Medicine(string id)
        {
            ViewBag.id = id;

            if (Session["PatInfo"] != null)
            {
                string feeno = ptinfo.FeeNo;
                DateTime now = DateTime.Now;

                byte[] doByteCode = webService.GetUdOrder(feeno, "A");
                string doJsonArr = CompressTool.DecompressString(doByteCode);
                List<UdOrder> GetUdOrderList = JsonConvert.DeserializeObject<List<UdOrder>>(doJsonArr);
                DataTable dt = care_record_m.sel_med(ptinfo.FeeNo, now.AddDays(-2).ToString("yyyy/MM/dd 23:59:59"), now.AddDays(1).ToString("yyyy/MM/dd 00:00:00"));
                dt.Columns.Add("NOTE");
                dt.Columns.Add("MED_DESC");
                dt.Columns.Add("ALISE_DESC");
                dt.Columns.Add("UD_UNIT");
                dt.Columns.Add("UD_TYPE");
                #region 設定NOTE
                foreach (DataRow r in dt.Rows)
                {
                    var tmpList = (from a in GetUdOrderList
                                   where a.UD_SEQ == r["UD_SEQ"].ToString().Trim()
                                   select new { a.MED_DESC, a.ALISE_DESC, a.UD_UNIT, a.UD_TYPE }).ToList();
                    foreach (var q in tmpList)
                    {
                        r["MED_DESC"] = q.MED_DESC.ToString().Trim();
                        r["ALISE_DESC"] = q.ALISE_DESC.ToString().Trim();
                        r["UD_UNIT"] = q.UD_UNIT.ToString().Trim();
                        r["UD_TYPE"] = q.UD_TYPE.ToString().Trim();
                    }

                    string note = "";
                    if (r["REASONTYPE"].ToString() != "" || r["BADREACTION"].ToString() != "N")
                    {
                        if (r["REASONTYPE"].ToString() != "")
                        {
                            switch (r["REASONTYPE"].ToString())
                            {
                                case "1"://未執行
                                    note = "病人因 " + r["REASON"] + " 未執行 " + r["MED_DESC"] + "(" + r["ALISE_DESC"] + ")。";
                                    r["REASON"] = "病人因 " + r["REASON"] + " 未執行。";
                                    break;
                                case "2"://提早執行
                                    note = "病人因 " + r["REASON"] + " 提早執行 " + r["MED_DESC"] + "(" + r["ALISE_DESC"] + ")。";
                                    r["REASON"] = "病人因 " + r["REASON"] + " 提早執行。";
                                    break;
                                case "3"://延遲執行
                                    note = "病人因 " + r["REASON"] + " 延遲給予 " + r["MED_DESC"] + "(" + r["ALISE_DESC"] + ")。";
                                    r["REASON"] = "病人因 " + r["REASON"] + " 延遲給予。";
                                    break;
                                case "4"://過時執行
                                    note = "病人因 " + r["REASON"] + " 過時給予 " + r["MED_DESC"] + "(" + r["ALISE_DESC"] + ")。";
                                    r["REASON"] = "病人因 " + r["REASON"] + " 過時給予。";
                                    break;
                                case "5"://暫停
                                    note = "病人因 " + r["REASON"] + " 暫停 " + r["MED_DESC"] + "(" + r["ALISE_DESC"] + ")。";
                                    r["REASON"] = "病人因 " + r["REASON"] + " 暫停。";
                                    break;
                                case null:
                                    break;
                                default:
                                    break;

                            }
                        }
                        if (r["BADREACTION"].ToString() == "Y")
                        {
                            note += "病人於 " + r["EXEC_DATE"] + " 服用 " + r["MED_DESC"] + "(" + r["ALISE_DESC"] + ") 產生不良反應情形。";
                            r["REASON"] = "病人產生不良反應情形。";
                        }
                    }
                    else
                        note = "病人於 " + r["EXEC_DATE"] + " 服用 " + r["MED_DESC"] + "(" + r["ALISE_DESC"] + ") " + r["USE_DOSE"] + r["UD_UNIT"] + " 。";

                    r["NOTE"] = note;
                }
                #endregion

                ViewBag.dt = dt;
                return View();
            }

            //session過期
            Response.Write("<script>alert('請重新選擇病患');window.close();</script>");
            return new EmptyResult();
        }

        //留觀病人持續護理評估表
        public ActionResult CareAssess()
        {
            return View();
        }

        #endregion

        #region 出院護理摘要
        //舊出院護理摘要(出院通知)
        [HttpGet]
        public ActionResult Leaving_Hospital()
        {
            Education edu_m = new Education();
            try
            {
                //20140125 暫時取消帶患者 LAB 資料
                //List<Lab> lab_list = new List<Lab>();
                //byte[] labfoByteCode = nis.GetLab8HR(ptinfo.FeeNo);
                DataTable dt = new DataTable();

                string msg = "病人因__________於#InDate經由__________入院，意識狀態：#GCS，診斷為：#ICD9_code1。";
                //20140125 暫時取消帶患者 LAB 資料
                //if (labfoByteCode != null)
                //{
                //    string labJosnArr = CompressTool.DecompressString(labfoByteCode);
                //    lab_list = JsonConvert.DeserializeObject<List<Lab>>(labJosnArr);
                //    string temp = "住院期間於#LabDate進行#LabName檢查、";
                //    for (int i = 0; i < lab_list.Count; i++)
                //        msg += temp.Replace("#LabDate", Convert.ToDateTime(lab_list[i].LabDate).ToString("yyyy/MM/dd")).Replace("#LabName", lab_list[i].LabName.Trim());
                //    msg = msg.Substring(0, msg.Length - 1) + "。";
                //}
                //20140125 暫時取消帶患者 LAB 資料
                msg += "於__________部位置入__________導管，此導管於____/____/____移除，在__________部位有____cm傷口。";

                dt = care_record_m.GetCarePlan_Master(ptinfo.FeeNo);
                if (dt.Rows.Count > 0)
                {
                    msg += "住院過程中依病人病情狀況予建立";
                    foreach (DataRow r in dt.Rows)
                        msg += r["topicdesc"].ToString() + "、";
                    msg = msg.Substring(0, msg.Length - 1) + "之照顧計劃，";
                }
                dt = edu_m.sel_health_education(ptinfo.FeeNo, "", "");
                if (dt.Rows.Count > 0)
                {
                    msg += "並給予";
                    foreach (DataRow r in dt.Rows)
                        msg += r["NAME"].ToString() + "、";
                    msg = msg.Substring(0, msg.Length - 1) + "之護理指導。";
                }
                dt = this.sel_assess_data(ptinfo.FeeNo, "");

                msg = msg.Replace("#InDate", Convert.ToDateTime(ptinfo.InDate).ToString("yyyy/MM/dd"));
                msg = msg.Replace("#GCS", "E" + sel_data(dt, "param_EyesReflection") + "V" + sel_data(dt, "param_LanguageReflection") + "M" + sel_data(dt, "param_SportReflection"));
                msg = msg.Replace("#ICD9_code1", ptinfo.ICD9_code1.Substring(ptinfo.ICD9_code1.IndexOf("("), ptinfo.ICD9_code1.Length - ptinfo.ICD9_code1.IndexOf("(")));

                ViewBag.msg = msg;
                ViewBag.Indate = Convert.ToDateTime(ptinfo.InDate).ToString("yyyy/MM/dd");
                ViewBag.dt_care_plan_master = care_record_m.GetCarePlan_Master(ptinfo.FeeNo);
                ViewBag.dt_care_plan_object = care_record_m.GetCarePlan_Object(ptinfo.FeeNo);
                return View();
            }
            catch (Exception e)
            {
                Response.Write("Web Service 發生錯誤，請聯絡資訊室，詳細資料如下：\n\n" + e);
                return new EmptyResult();
            }
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Leaving_Hospital(FormCollection form)
        {
            string id = base.creatid("EVENT", userinfo.EmployeesNo, ptinfo.FeeNo, "0");
            string date = form["txt_day"] + " " + form["txt_time"];
            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("EVENT_ID", id, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FEENO", ptinfo.FeeNo, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATNO", userinfo.EmployeesNo, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATTIME", date, DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("TYPE_ID", "5", DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CONTENT", form["rb_leave"], DBItem.DBDataType.String));
            int erow = care_record_m.DBExecInsert("NIS_SPECIAL_EVENT_DATA", insertDataList);
            if (erow > 0)
            {
                base.Insert_CareRecord_Black(date, id, "出院護理摘要", form["msg"], "", "", "", "");
                string[] id_list;
                if (form["id_list"] != null)
                {
                    id_list = form["id_list"].Split(',');
                    for (int i = 0; i < id_list.Length; i++)
                    {
                        if (form[id_list[i]] != null && form[id_list[i]] != "")
                        {
                            if (id_list[i].Substring(0, 3) == "CPT")
                            {
                                insertDataList.Clear();
                                insertDataList.Add(new DBItem("TARGETENDDATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                                insertDataList.Add(new DBItem("CUSTOM", form[id_list[i]], DBItem.DBDataType.String));
                                insertDataList.Add(new DBItem("TARGETSTATUS", form[id_list[i] + "_reason"], DBItem.DBDataType.String));
                                care_record_m.DBExecUpdate("CPTARGETDTL", insertDataList, "SERIAL = '" + id_list[i] + "' ");
                            }
                            if (id_list[i].Substring(0, 3) == "CPO")
                            {
                                insertDataList.Clear();
                                insertDataList.Add(new DBItem("ENDDATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                                insertDataList.Add(new DBItem("NOTE", form[id_list[i]] + "|" + form[id_list[i] + "_reason"], DBItem.DBDataType.String));
                                care_record_m.DBExecUpdate("CPCUSTOMER", insertDataList, "SERIAL = '" + id_list[i] + "' ");
                            }
                        }
                    }
                }

                Response.Write("<script>alert('新增成功');window.close();window.opener.location.href='List';</script>");
            }
            else
                Response.Write("<script>alert('新增失敗');window.close();window.opener.location.href='List';</script>");

            return new EmptyResult();
        }

        #endregion

        #region 檢驗檢查

        //檢驗檢查
        [HttpGet]
        public ActionResult CheckTest()
        {
            Education edu_m = new Education();
            #region 檢查
            //取得檢查資料
            try
            {
                byte[] examByte = webService.GetExam(ptinfo.FeeNo);
                if (examByte != null)
                {
                    string examJson = CompressTool.DecompressString(examByte);
                    List<Exam> examList = JsonConvert.DeserializeObject<List<Exam>>(examJson);
                    ViewData["exam"] = examList;

                }
            }
            catch (Exception ex)
            {
                Response.Write("Web Service 發生錯誤，請聯絡資訊室，詳細資料如下：\n\n" + ex);
            }
            #endregion
            #region 檢驗
            //取得資料
            try
            {
                byte[] labByte = webService.GetLab(ptinfo.FeeNo);
                if (labByte != null)
                {
                    string labJson = CompressTool.DecompressString(labByte);
                    List<Lab> labList = JsonConvert.DeserializeObject<List<Lab>>(labJson);
                    ViewData["lab"] = labList;
                }
            }
            catch (Exception ex)
            {
                Response.Write("Web Service 發生錯誤，請聯絡資訊室，詳細資料如下：\n\n" + ex);
            }
            #endregion

            return View();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult CheckTest(FormCollection form)
        {
            string date = form["txt_day"] + " " + form["txt_time"];
            string msg = "";
            //檢驗
            string no = form["lab_ck"];
            if (no != null && no != "")
            {
                string[] ListNO = form["lab_ck"].Split(',');
                for (int i = 0; i < ListNO.Length; i++)
                {
                    string id = base.creatid("Lab" + ListNO[i].ToString(), userinfo.EmployeesNo, ptinfo.FeeNo, "0");
                    msg = form["d_" + ListNO[i].ToString()]
                        + "檢驗項目:" + form["i_" + ListNO[i].ToString()]
                         + "檢驗狀態:" + form["r_" + ListNO[i].ToString()];

                    base.Insert_CareRecord_Black(date, id, "檢驗", msg, "", "", "", "");
                }
            }
            //檢查
            no = form["exam_ck"];
            if (no != null && no != "")
            {
                string[] ListNO = form["exam_ck"].Split(',');
                for (int i = 0; i < ListNO.Length; i++)
                {
                    string id = base.creatid("Exam" + ListNO[i].ToString(), userinfo.EmployeesNo, ptinfo.FeeNo, "0");
                    msg = form["d_" + ListNO[i].ToString()]
                        + "檢查項目:" + form["i_" + ListNO[i].ToString()]
                         + "檢查結果:" + form["r_" + ListNO[i].ToString()];

                    base.Insert_CareRecord_Black(date, id, "檢查", msg, "", "", "", "");
                }
            }

            Response.Write("<script>alert('新增成功');window.close();window.opener.location.href='List';</script>");

            return new EmptyResult();
        }

        //檢驗檢查

        public ActionResult ExamButton(string stDate, string edDate, string HasBtn, string feeno = "")
        {
            Education edu_m = new Education();
            if (stDate == null || edDate == null)
            {
                stDate = DateTime.Now.ToString("yyyy/MM/dd");
                edDate = DateTime.Now.ToString("yyyy/MM/dd");
            }
            string assessment = string.Empty;
            if (!string.IsNullOrEmpty(feeno))
            {
                byte[] ptinfoByteCode = webService.GetPatientInfo(feeno);
                if (ptinfoByteCode != null)
                {
                    string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                    PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                    assessment = pi.Assessment;
                    ViewBag.ptinfo = pi;
                }
            }
            else
            {
                feeno = ptinfo.FeeNo;
                assessment = ptinfo.Assessment;
                ViewBag.ptinfo = (NIS.Data.PatientInfo)Session["PatInfo"];
            }
            #region 檢查
            //取得檢查資料
            try
            {
                byte[] examByte = webService.GetExambyDate(feeno, stDate, edDate, userinfo.EmployeesNo,userinfo.Pwd);
                string examJson = "[]";
                if (examByte != null)
                    examJson = CompressTool.DecompressString(examByte);

                List<Exam> examList = JsonConvert.DeserializeObject<List<Exam>>(examJson);
                ViewData["exam"] = examList;
            }
            catch (Exception ex)
            {
                Response.Write("Web Service 發生錯誤，請聯絡資訊室，詳細資料如下：\n\n" + ex);
            }
            #endregion

            ViewBag.ER = assessment.ToString();
            ViewBag.HasBtn = HasBtn;

            return View();
        }
        //檢驗檢查

        public ActionResult LabButton(string stDate, string edDate, string HasBtn, string feeno = "")
        {
            Education edu_m = new Education();
            var test = userinfo;
            if (stDate == null)
                stDate = DateTime.Now.ToString("yyyy/MM/dd");

            if (edDate == null)
                edDate = DateTime.Now.ToString("yyyy/MM/dd");
            string assessment = string.Empty;
            if (!string.IsNullOrEmpty(feeno))
            {
                byte[] ptinfoByteCode = webService.GetPatientInfo(feeno);
                if (ptinfoByteCode != null)
                {
                    string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                    PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                    assessment = pi.Assessment;
                    ViewBag.ptinfo = pi;
                }
            }
            else
            {
                feeno = ptinfo.FeeNo;
                assessment = ptinfo.Assessment;
                ViewBag.ptinfo = (NIS.Data.PatientInfo)Session["PatInfo"];
            }
            #region 檢驗
            //取得資料
            try
            {
                byte[] labByte = webService.GetLabbyDate(feeno, stDate, edDate);
                string labJson = "[]";
                if (labByte != null)
                {
                    labJson = CompressTool.DecompressString(labByte);
                    List<Lab> labList = JsonConvert.DeserializeObject<List<Lab>>(labJson);
                    if (labList != null)
                        ViewData["lab"] = labList;
                }
            }
            catch (Exception ex)
            {
                Response.Write("Web Service 發生錯誤，請聯絡資訊室，詳細資料如下：\n\n" + ex);
            }
            #endregion
            ViewBag.ER = assessment.ToString();
            ViewBag.HasBtn = HasBtn;

            return View();
        }
        public ActionResult VoiceButton(string stDate, string edDate, string HasBtn)
        {
            List<TALK_TABLE> voiceList = new List<TALK_TABLE>();
            List<SelectListItem> cPhraseList = new List<SelectListItem>();
            if (Session["PatInfo"] != null)
            {
                //宣告病患_取得住院日期
                PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];

                if (stDate == null)
                    stDate = DateTime.Now.ToString("yyyy/MM/dd");

                if (edDate == null)
                    edDate = DateTime.Now.ToString("yyyy/MM/dd");

                #region 語音工作 & 語音片語

                try
                {
                    #region 取得語音資料                    
                    //取得語音資料
                    DataTable dt = new DataTable();
                    string FEENO = ptInfo.FeeNo;
                    string strSQL = "SELECT * FROM TALK_TABLE ";
                    strSQL += " WHERE FEE_NO ='" + FEENO + "'  AND USERID ='" + userinfo.EmployeesNo + "' ";
                    strSQL += " AND ACTION_TYPE ='Insert' and STATUS ='Y' ";
                    //strSQL += " AND CREATE_TIME >= to_date(to_char(sysdate , 'yyyy/mm/dd'),'yyyy/mm/dd')";
                    strSQL += " AND START_DATETIME >= to_date ('" + stDate + "','yyyy/mm/dd') AND END_DATETIME <= to_date ('" + edDate + "' ,'yyyy/mm/dd')";
                    strSQL += " ORDER BY CREATE_TIME DESC ";
                    link.DBExecSQL(strSQL, ref dt);

                    if (dt != null)
                    {
                        voiceList = (List<TALK_TABLE>)dt.ToList<TALK_TABLE>();
                        voiceList = voiceList.ToList();
                        if (voiceList != null)
                            ViewData["voice"] = voiceList;
                    }
                    #endregion

                    #region 取得片語


                    //取得片語 -- 只限[語音片語]
                    DataTable dt_phrase = care_record_m.get_PhraseList(userinfo.EmployeesNo);
                    string creano = userinfo.EmployeesNo;
                    string VoiceNodeId = "";
                    string typeSrch = "self";
                    if (dt_phrase != null)
                    {
                        for (int i = 0; i < dt_phrase.Rows.Count; i++)
                        {
                            if (dt_phrase.Rows[i]["NAME"].ToString().Trim() == "語音片語")
                            {
                                VoiceNodeId = dt_phrase.Rows[i]["NODEID"].ToString().Trim();
                            }
                        }
                    }

                    DataTable dt_phraseData = care_record_m.sel_phrase_data(VoiceNodeId, "", typeSrch, creano);
                    ViewBag.dt_phraseData = dt_phraseData;
                    #endregion
                }
                catch (Exception ex)
                {
                    Response.Write("Web Service 發生錯誤，請聯絡資訊室，詳細資料如下：\n\n" + ex);
                }
                #endregion
                ViewBag.ER = ptinfo.Assessment.ToString();
                ViewBag.HasBtn = HasBtn;
                ViewData["PhraseList"] = cPhraseList;
                ViewData["voice"] = voiceList;
                return View();
            }
            else
            {
                Response.Write("<script>alert('登入逾時');</script>");

                ViewData["PhraseList"] = cPhraseList;
                ViewData["voice"] = voiceList;
                return new EmptyResult();
            }
        }

        //衛教語音
        public ActionResult VoiceEducation(string stDate, string edDate, string HasBtn)
        {
            List<TALK_TABLE> memoList = new List<TALK_TABLE>();
            if (Session["PatInfo"] != null)
            {
                //宣告病患_取得住院日期
                PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];

                if (stDate == null)
                    stDate = DateTime.Now.ToString("yyyy/MM/dd");

                if (edDate == null)
                    edDate = DateTime.Now.ToString("yyyy/MM/dd");

                #region 語音 & 片語
                //ViewBag.RootDocument = GetSourceUrl();
                try
                {
                    //取得語音MEMO
                    string FEENO = ptInfo.FeeNo;
                    #region 取得語音MEMO

                    DataTable dt_Vmemo = new DataTable();
                    FEENO = ptInfo.FeeNo;
                    string strSQL = "SELECT * FROM TALK_TABLE ";
                    strSQL += " WHERE FEE_NO ='" + FEENO + "' AND USERID ='" + userinfo.EmployeesNo + "' ";
                    strSQL += " AND ACTION_TYPE ='Insert' and CONTROLLER ='Voice/Memo'   AND  STATUS ='Y' ";
                    //strSQL += " AND CREATE_TIME >= to_date ('" + stDate + " 00:00:00','yyyy/mm/dd hh24:mi:ss') AND CREATE_TIME <= to_date ('" + edDate + " 23:59:59' ,'yyyy/mm/dd hh24:mi:ss')";
                    strSQL += " ORDER BY CREATE_TIME DESC ";

                    link.DBExecSQL(strSQL, ref dt_Vmemo);

                    if (dt_Vmemo != null)
                    {
                        memoList = (List<TALK_TABLE>)dt_Vmemo.ToList<TALK_TABLE>();
                        memoList = memoList.ToList();
                        if (memoList != null)
                            ViewData["voiceMemo"] = memoList;
                    }
                    #endregion
                }
                catch (Exception ex)
                {
                    Response.Write("Web Service 發生錯誤，請聯絡資訊室，詳細資料如下：\n\n" + ex);
                }
                #endregion
                ViewBag.ER = ptinfo.Assessment.ToString();
                ViewBag.HasBtn = HasBtn;
                return View();
            }
            else
            {
                Response.Write("<script>alert('登入逾時');</script>");

                ViewData["voiceMemo"] = memoList;
                return new EmptyResult();
            }
        }
        public ActionResult DeleteVoiceEdu(string pkid)
        {
            RESPONSE_MSG json_result = new RESPONSE_MSG();
            try
            {
                string where = "PK_ID = '" + pkid + "' AND FEE_NO='" + ptinfo.FeeNo + "'";
                List<DBItem> updDataList = new List<DBItem>();
                updDataList.Add(new DBItem("STATUS", "N", DBItem.DBDataType.String));

                int erow = link.DBExecUpdate("TALK_TABLE", updDataList, where);
                if (erow == 1)
                {
                    json_result.status = RESPONSE_STATUS.SUCCESS;
                    json_result.message = "";
                }
                else
                {
                    json_result.status = RESPONSE_STATUS.ERROR;
                    json_result.message = "刪除失敗。";
                }
            }
            catch (Exception ex)
            {
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                json_result.status = RESPONSE_STATUS.EXCEPTION;
                json_result.message = "語音刪除失敗，請連絡資訊室人員。(" + ex.ToString() + ")";
            }
            return Content(JsonConvert.SerializeObject(json_result), "application/json");
        }
        [HttpGet]
        //檢驗報告 for Ajax )
        public ActionResult LabButton_new(string stDate, string edDate, string HasBtn)
        {
            RESPONSE_MSG json_result = new RESPONSE_MSG();

            if (stDate == null)
                stDate = DateTime.Now.ToString("yyyy/MM/dd");

            if (stDate == null)
                edDate = DateTime.Now.ToString("yyyy/MM/dd");

            if (Session["PatInfo"] != null)
            {
                //宣告病患_取得住院日期
                PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
                ViewBag.InDate = Convert.ToDateTime(ptInfo.InDate).ToString("yyyy/MM/dd");

                //取得資料
                try
                {
                    byte[] labByte = webService.GetLabbyDate(ptinfo.FeeNo, stDate, edDate);
                    string labJson = "[]";
                    if (labByte != null)
                    {
                        labJson = CompressTool.DecompressString(labByte);
                        List<Lab> labList = JsonConvert.DeserializeObject<List<Lab>>(labJson);
                        labList = (List<Lab>)labList.ToList<Lab>();
                        if (labList != null)
                            labJson = JsonConvert.SerializeObject(labList);

                        json_result.attachment = labJson;
                    }
                }
                catch (Exception ex)
                {
                    Response.Write("Web Service 發生錯誤，請聯絡資訊室，詳細資料如下：\n\n" + ex);
                }

                ViewBag.ajaxData = json_result;
                ViewBag.ER = ptinfo.Assessment.ToString();
                ViewBag.HasBtn = HasBtn;
                ViewBag.date = stDate + " " + edDate;

                return Content(JsonConvert.SerializeObject(ViewBag), "application/json");
            }
            else
            {
                Response.Write("<script>alert('登入逾時');</script>");
                ViewBag.ajaxData = "";
                return new EmptyResult();
            }

        }

        #endregion


        #region EKG

        //裁剪圖片的View
        [HttpGet]
        public ActionResult GetEKG(string openerDataId, string openerImgId)
        {
            string feeno = ptinfo.FeeNo;
            ViewData["openerDataId"] = openerDataId;
            ViewData["openerImgId"] = openerImgId;
            return View();
        }
        //public JsonResult test(DateTime vs_time)
        //{
        //    DataTable obj = new DataTable();
        //    //DataTable to list
        //    //DataRow dr = new DataRow();
        //    //DataTable.Rows.OfType<DataRow>().Select(dr => dr.Field<VitalSignData>()).Tolist();
        //    //var Obj = obj.Rows.OfType<DataRow>().Select(dr => dr.Field<VitalSignData>("vs_item")).ToList();
        //    obj = obj.Rows.OfType<DataRow>().Where(dr => dr.Field<VitalSignData>("vs_item")).ToList()==vs_time)
        //   /*var obj = new {aaa="1",bbb="2"};*///模擬LIST

        //    return Json(obj, JsonRequestBehavior.AllowGet);
        //}

        [HttpPost]
        public string SearchECKImage(string SearchDateTime, string LEADS)
        {
            string StartDateTime = Convert.ToDateTime(SearchDateTime).ToString("yyyy/MM/dd HH:mm:ss");
            string EndDateTime = Convert.ToDateTime(StartDateTime).AddMinutes(3).ToString("yyyy/MM/dd HH:mm:ss");
            //return EKG_ws.GetEKG_IMG(base.ptinfo.ChartNo, StartDateTime, EndDateTime);

            string imageStr = "";
            try
            {
                imageStr = EKG_ws.GetEKG_IMG_LEAD(base.ptinfo.ChartNo, StartDateTime, EndDateTime, LEADS);
            }
            catch (Exception e)
            {
                //Do nothing
            }
            return imageStr;
        }

        [HttpPost]
        public JsonResult GetVitalSign(string vs_date, string vs_time)
        {
            List<ICUData> ICUDataList = new List<ICUData>();
            string VS_Date = vs_date.Replace("/", "").Replace(":", "").TrimEnd();
            string VS_Time = vs_time.Replace(":", "").TrimEnd();
            //TODO 日期分兩個存
            DataTable dt_vital = new DataTable();
            string StrSqlVital = "select * from measuredata where PATIENTID ='" + base.ptinfo.ChartNo.Trim() + "' and ";
            StrSqlVital += " DATADATE like '%" + VS_Date.Trim() + VS_Time.Trim() + "%' Order by DATADATE ";
            dt_vital = care_record_m.DBExecSQL(StrSqlVital);
            var totalValue = "";
            string MP, BF, SP, CV1, IC1, CPP, PA, PCWP, ETCO, NBP_S, NBP_D, ABP_S, ABP_D;
            totalValue += vs_time;
            List<string> datetimeList = new List<string>();
            datetimeList = dt_vital.AsEnumerable().ToList().Select(x => x["DATADATE"].ToString()).Distinct().ToList();
            var second = "";
            foreach (string datatime in datetimeList)
            {
                List<DataRow> dtList = dt_vital.AsEnumerable().ToList().FindAll(x => x["DATADATE"].ToString() == datatime);
                second = datatime.Substring(datatime.Length - 2);
                if (dtList != null)
                {

                    if (dtList.Exists(x => x["OBSERVATIONID"].ToString().Contains("HR")))
                    {
                        MP = dtList.Find(x => x["OBSERVATIONID"].ToString().Contains("HR"))["VALUE"].ToString();
                    }
                    else
                    {
                        MP = "無資料 ";
                    }
                    if (dtList.Exists(x => x["OBSERVATIONID"].ToString().Contains("RR")))
                    {
                        BF = dtList.Find(x => x["OBSERVATIONID"].ToString().Contains("RR"))["VALUE"].ToString();
                    }
                    else
                    {
                        BF = "無資料 ";
                    }
                    if (dtList.Exists(x => x["OBSERVATIONID"].ToString().Contains("NBP-S")))
                    {
                        NBP_S = dtList.Find(x => x["OBSERVATIONID"].ToString().Contains("NBP-S"))["VALUE"].ToString();
                    }
                    else
                    {
                        NBP_S = "無資料 ";
                    }
                    if (dtList.Exists(x => x["OBSERVATIONID"].ToString().Contains("NBP-D")))
                    {
                        NBP_D = dtList.Find(x => x["OBSERVATIONID"].ToString().Contains("NBP-D"))["VALUE"].ToString();
                    }
                    else
                    {
                        NBP_D = "無資料 ";
                    }
                    if (dtList.Exists(x => x["OBSERVATIONID"].ToString().Contains("SPO2")))
                    {
                        SP = dtList.Find(x => x["OBSERVATIONID"].ToString().Contains("SPO2"))["VALUE"].ToString();
                    }
                    else
                    {
                        SP = "無資料 ";
                    }
                    if (dtList.Exists(x => x["OBSERVATIONID"].ToString().Contains("CVP")))
                    {
                        CV1 = dtList.Find(x => x["OBSERVATIONID"].ToString().Contains("CVP"))["VALUE"].ToString();
                    }
                    else
                    {
                        CV1 = "無資料 ";
                    }
                    if (dtList.Exists(x => x["OBSERVATIONID"].ToString().Contains("ICP")))
                    {
                        IC1 = dtList.Find(x => x["OBSERVATIONID"].ToString().Contains("ICP"))["VALUE"].ToString();
                    }
                    else
                    {
                        IC1 = "無資料 ";
                    }
                    if (dtList.Exists(x => x["OBSERVATIONID"].ToString().Contains("CPP")))
                    {
                        CPP = dtList.Find(x => x["OBSERVATIONID"].ToString().Contains("CPP"))["VALUE"].ToString();
                    }
                    else
                    {
                        CPP = "無資料 ";
                    }
                    if (dtList.Exists(x => x["OBSERVATIONID"].ToString().Contains("ABP-S")))
                    {
                        ABP_S = dtList.Find(x => x["OBSERVATIONID"].ToString().Contains("ABP-S"))["VALUE"].ToString();
                    }
                    else
                    {
                        ABP_S = "無資料 ";
                    }
                    if (dtList.Exists(x => x["OBSERVATIONID"].ToString().Contains("ABP-D")))
                    {
                        ABP_D = dtList.Find(x => x["OBSERVATIONID"].ToString().Contains("ABP-D"))["VALUE"].ToString();
                    }
                    else
                    {
                        ABP_D = "無資料 ";
                    }
                    if (dtList.Exists(x => x["OBSERVATIONID"].ToString().Contains("PA")))
                    {
                        PA = dtList.Find(x => x["OBSERVATIONID"].ToString().Contains("PA"))["VALUE"].ToString();
                    }
                    else
                    {
                        PA = "無資料 ";
                    }
                    if (dtList.Exists(x => x["OBSERVATIONID"].ToString().Contains("PAW")))
                    {
                        PCWP = dtList.Find(x => x["OBSERVATIONID"].ToString().Contains("PAW"))["VALUE"].ToString();
                    }
                    else
                    {
                        PCWP = "無資料 ";
                    }
                    if (dtList.Exists(x => x["OBSERVATIONID"].ToString().Contains("CO2")))
                    {
                        ETCO = dtList.Find(x => x["OBSERVATIONID"].ToString().Contains("CO2"))["VALUE"].ToString();
                    }
                    else
                    {
                        ETCO = "無資料 ";
                    }
                    ICUDataList.Add(new ICUData()
                    {
                        VS_Date = vs_date + " " + vs_time + ":" + second,
                        mp = MP.Trim(),
                        bf = BF.Trim(),
                        bp = NBP_S.Trim() + "/" + NBP_D.Trim(),
                        sp = SP.Trim(),
                        cv1 = CV1.Trim(),
                        ic1 = IC1.Trim(),
                        cpp = CPP.Trim(),
                        abp = ABP_S.Trim() + "/" + ABP_D.Trim(),
                        pa = PA.Trim(),
                        pcwp = PCWP.Trim(),
                        etco = ETCO.Trim(),
                    });
                    totalValue += (" 心跳: " + MP + " ,呼吸: " + BF + " ,血壓: " + NBP_S.Trim() + "/" + NBP_D.Trim() + " ,血氧: " + SP + " ,中心靜脈壓: " + CV1 + " ,顱內壓: " + IC1 + " ,CPP: " + CPP + " ,ABP: " + ABP_S.Trim() + "/" + ABP_D.Trim() + " ,PA: " + PA + " ,PCWP: " + PCWP + " ,ETCO: " + ETCO);
                }

            }
            //    return totalValue;
            return Json(ICUDataList, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public string GetVitalSign1(string vs_date, string vs_time)
        {
            List<ICUData> ICUDataList = new List<ICUData>();
            string VS_Date = vs_date.Replace("/", "").Replace(":", "").TrimEnd();
            string VS_Time = vs_time.Replace(":", "").TrimEnd();
            //TODO 日期分兩個存
            DataTable dt_vital = new DataTable();
            string StrSqlVital = "select * from measuredata where PATIENTID ='" + base.ptinfo.ChartNo.Trim() + "' and ";
            StrSqlVital += " DATADATE like '" + VS_Date.Trim() + VS_Time.Trim() + "%' Order by DATADATE ";
            dt_vital = care_record_m.DBExecSQL(StrSqlVital);
            var totalValue = "";
            string MP, BF, SP, CV1, IC1, CPP, PCWP, ETCO, NBP_S, NBP_D, ABP_S, ABP_D, PA_S, PA_D;
            totalValue += vs_date + " " + vs_time + " ";
            List<string> datetimeList = new List<string>();
            datetimeList = dt_vital.AsEnumerable().ToList().Select(x => x["DATADATE"].ToString()).Distinct().ToList();
            var second = "";
            foreach (string datatime in datetimeList)
            {
                List<DataRow> dtList = dt_vital.AsEnumerable().ToList().FindAll(x => x["DATADATE"].ToString() == datatime);
                second = datatime.Substring(datatime.Length - 2);
                if (dtList != null)
                {

                    if (dtList.Exists(x => x["OBSERVATIONID"].ToString().Contains("HR")))
                    {
                        MP = dtList.Find(x => x["OBSERVATIONID"].ToString().Contains("HR"))["VALUE"].ToString();
                        totalValue += "心跳:" + MP + "次/分, ";
                    }
                    else
                    {
                        MP = "無資料 ";
                    }
                    if (dtList.Exists(x => x["OBSERVATIONID"].ToString().Contains("RR")))
                    {
                        BF = dtList.Find(x => x["OBSERVATIONID"].ToString().Contains("RR"))["VALUE"].ToString();
                        totalValue += "呼吸:" + BF + "次/分, ";
                    }
                    else
                    {
                        BF = "無資料 ";
                    }
                    if (dtList.Exists(x => x["OBSERVATIONID"].ToString().Contains("NBP-S")))
                    {
                        NBP_S = dtList.Find(x => x["OBSERVATIONID"].ToString().Contains("NBP-S"))["VALUE"].ToString();
                        totalValue += "血壓:" + NBP_S + "/";
                    }
                    else
                    {
                        NBP_S = "無資料 ";
                    }
                    if (dtList.Exists(x => x["OBSERVATIONID"].ToString().Contains("NBP-D")))
                    {
                        NBP_D = dtList.Find(x => x["OBSERVATIONID"].ToString().Contains("NBP-D"))["VALUE"].ToString();
                        totalValue += "" + NBP_D + "mmHg, ";
                    }
                    else
                    {
                        NBP_D = "無資料 ";
                    }
                    if (dtList.Exists(x => x["OBSERVATIONID"].ToString().Contains("SPO2")))
                    {
                        SP = dtList.Find(x => x["OBSERVATIONID"].ToString().Contains("SPO2"))["VALUE"].ToString();
                        totalValue += "血氧:" + SP + "%, ";
                    }
                    else
                    {
                        SP = "無資料 ";
                    }
                    if (dtList.Exists(x => x["OBSERVATIONID"].ToString().Contains("CVP")))
                    {
                        CV1 = dtList.Find(x => x["OBSERVATIONID"].ToString().Contains("CVP"))["VALUE"].ToString();
                        totalValue += "CVP:" + CV1 + "mmHg, ";

                    }
                    else
                    {
                        CV1 = "無資料 ";
                    }
                    if (dtList.Exists(x => x["OBSERVATIONID"].ToString().Contains("ICP")))
                    {
                        IC1 = dtList.Find(x => x["OBSERVATIONID"].ToString().Contains("ICP"))["VALUE"].ToString();
                        totalValue += "ICP:" + IC1 + "mmHg, ";
                    }
                    else
                    {
                        IC1 = "無資料 ";
                    }
                    if (dtList.Exists(x => x["OBSERVATIONID"].ToString().Contains("CPP")))
                    {
                        CPP = dtList.Find(x => x["OBSERVATIONID"].ToString().Contains("CPP"))["VALUE"].ToString();
                        totalValue += "CPP:" + CPP + "mmHg, ";
                    }
                    else
                    {
                        CPP = "無資料 ";
                    }
                    if (dtList.Exists(x => x["OBSERVATIONID"].ToString().Contains("ABP-S")))
                    {
                        ABP_S = dtList.Find(x => x["OBSERVATIONID"].ToString().Contains("ABP-S"))["VALUE"].ToString();
                        totalValue += "ABP:" + ABP_S + "/";
                    }
                    else
                    {
                        ABP_S = "無資料 ";
                    }
                    if (dtList.Exists(x => x["OBSERVATIONID"].ToString().Contains("ABP-D")))
                    {
                        ABP_D = dtList.Find(x => x["OBSERVATIONID"].ToString().Contains("ABP-D"))["VALUE"].ToString();
                        totalValue += "" + ABP_D + "mmHg, ";
                    }
                    else
                    {
                        ABP_D = "無資料 ";
                    }


                    if (dtList.Exists(x => x["OBSERVATIONID"].ToString().Contains("PA#-S")))
                    {
                        PA_S = dtList.Find(x => x["OBSERVATIONID"].ToString().Contains("PA#-S"))["VALUE"].ToString();
                        totalValue += "PA:" + PA_S + "/";
                    }
                    else
                    {
                        PA_S = "無資料 ";
                    }
                    if (dtList.Exists(x => x["OBSERVATIONID"].ToString().Contains("PA#-D")))
                    {
                        PA_D = dtList.Find(x => x["OBSERVATIONID"].ToString().Contains("PA#-D"))["VALUE"].ToString();
                        totalValue += "" + PA_D + "mmHg, ";
                    }
                    else
                    {
                        PA_D = "無資料 ";
                    }
                    if (dtList.Exists(x => x["OBSERVATIONID"].ToString().Contains("PAW")))
                    {
                        PCWP = dtList.Find(x => x["OBSERVATIONID"].ToString().Contains("PAW"))["VALUE"].ToString();
                        totalValue += "PCWP:" + PCWP + "mmHg, ";
                    }
                    else
                    {
                        PCWP = "無資料 ";
                    }
                    if (dtList.Exists(x => x["OBSERVATIONID"].ToString().Contains("CO2")))
                    {
                        ETCO = dtList.Find(x => x["OBSERVATIONID"].ToString().Contains("CO2"))["VALUE"].ToString();
                        totalValue += "ETCO2:" + ETCO + "mmHg, ";
                    }
                    else
                    {
                        ETCO = "無資料 ";
                    }
                    ICUDataList.Add(new ICUData()
                    {
                        VS_Date = vs_date + " " + vs_time + ":" + second,
                        mp = MP.Trim(),
                        bf = BF.Trim(),
                        bp = NBP_S.Trim() + "/" + NBP_D.Trim(),
                        sp = SP.Trim(),
                        cv1 = CV1.Trim(),
                        ic1 = IC1.Trim(),
                        cpp = CPP.Trim(),
                        abp = ABP_S.Trim() + "/" + ABP_D.Trim(),
                        pa = PA_S.Trim() + "/" + PA_D.Trim(),
                        pcwp = PCWP.Trim(),
                        etco = ETCO.Trim(),
                    });



                    //  totalValue += (" 心跳: " + MP + " ,呼吸: " + BF + " ,血壓: " + NBP_S.Trim() + "/" + NBP_D.Trim() + " ,血氧: " + SP + " ,中心靜脈壓: " + CV1 + " ,顱內壓: " + IC1 + " ,CPP: " + CPP + " ,ABP: " + ABP_S.Trim() + "/" + ABP_D.Trim() + " ,PA: " + PA + " ,PCWP: " + PCWP + " ,ETCO: " + ETCO);
                }

            }
            //wait(10);
            return totalValue.TrimEnd(',');
            //    return Json(ICUDataList, JsonRequestBehavior.AllowGet);
        }

        void wait(int x)
        {
            DateTime t = DateTime.Now;
            DateTime tf = DateTime.Now.AddSeconds(x);

            while (t < tf)
            {
                t = DateTime.Now;
            }
        }



        //          foreach (DataRow r in dt_vital.Rows)
        //            {

        //                dtList.Exists(x => x["OBSERVATIONID"].ToString().Contains("HR"));
        //                var vs_item = r["VS_ITEM"].ToString();
        //                if (vs_item)
        //                {
        //                    b +=" 心跳: "+  r["VS_RECORD"].ToString();
        //    }
        //                if(vs_item == "bf")
        //                {
        //                    b += " 呼吸: "+ r["VS_RECORD"].ToString();
        //}
        //                if (vs_item == "bp")
        //                {
        //                    b += " 血壓: " + r["VS_RECORD"].ToString();
        //                }
        //                if (vs_item == "sp")
        //                {
        //                    b += " 血氧: " + r["VS_RECORD"].ToString();
        //                }
        //                if (vs_item == "cvl")
        //                {
        //                    b += " 中心靜脈壓: " + r["VS_RECORD"].ToString()+ " (mmHg) ";
        //                }
        //                if (vs_item == "ic1")
        //                {
        //                    b += " 顱內壓: " + r["VS_RECORD"].ToString() + " (mmHg) ";
        //                }
        //                if (vs_item == "cpp")
        //                {
        //                    b += " cpp: " + r["VS_RECORD"].ToString()+ " (mmHg) ";
        //                }
        //                if (vs_item == "abp")
        //                {
        //                    b += " abp: " + r["VS_RECORD"].ToString();
        //                }
        //                if (vs_item == "pa")
        //                {
        //                    b += " pa: " + r["VS_RECORD"].ToString();
        //                }
        //                if (vs_item == "pcwp")
        //                {
        //                    b += " pcwp: " + r["VS_RECORD"].ToString();
        //                }
        //                if (vs_item == "etco")
        //                {
        //                    b += " etco: " + r["VS_RECORD"].ToString();
        //                }
        //            }











        [HttpPost]
        public string CutECKImage(string ImgDate, string x, string y, string width, string height)
        {
            string ReturnData = string.Empty;
            int _x = int.Parse(x),
                _y = int.Parse(y),
                _width = int.Parse(width),
                _height = int.Parse(height);

            if (!string.IsNullOrWhiteSpace(ImgDate))
            {
                byte[]  arr = Convert.FromBase64String(ImgDate);
                using (MemoryStream ms = new MemoryStream(arr)) {
                    Bitmap bmp = new Bitmap(ms);
                    System.Drawing.Image sourceImage = bmp;
                    //裁剪的區域
                    System.Drawing.Rectangle fromR = new System.Drawing.Rectangle(_x * 2, _y, _width * 2, _height * 2);
                    //裁剪出來的圖要放在畫布的哪個位置
                    System.Drawing.Rectangle toR = new System.Drawing.Rectangle(0, 0, _width, _height);
                    //要當畫布的Bitmap物件
                    System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(_width, _height);
                    //產生畫布
                    System.Drawing.Graphics g = Graphics.FromImage(bitmap);
                    //清空畫布，背景白色
                    g.Clear(Color.White);
                    //以像素做為測量單位
                    GraphicsUnit units = GraphicsUnit.Pixel;
                    //剪裁
                    g.DrawImage(sourceImage, toR, fromR, units);
                    //裁剪完成，存檔
                    using (MemoryStream ms_2 = new MemoryStream()) {
                        bitmap.Save(ms_2, System.Drawing.Imaging.ImageFormat.Jpeg);
                        arr = ms_2.ToArray();
                        ReturnData = Convert.ToBase64String(arr);
                        //釋放資源
                        bmp.Dispose();
                        g.Dispose();
                        bitmap.Dispose();
                        sourceImage.Dispose();
                    }
                }
                arr = null;
                //GC.Collect();
            }
            return ReturnData;
        }
        #endregion

        #region other_function

        //轉PDF頁面
        public ActionResult Html_To_Pdf(string url)
        {
            string strPath = @"C:\\wkhtmltopdf\\wkhtmltopdf.exe";
            string filename = DateTime.Now.ToString("yyyyMMddHHmmssfff") + url.Substring(url.IndexOf("feeno=") + 6, url.Length - url.IndexOf("feeno=") - 6) + ".pdf";
            string tempPath = "C:\\inetpub\\NIS\\Images\\" + filename;
            string pdfpath = "../Images/" + filename;
            Process p = new Process();
            p.StartInfo.FileName = strPath;
            p.StartInfo.Arguments = url + " " + tempPath;
            p.StartInfo.UseShellExecute = true;
            p.Start();
            p.WaitForExit();
            Response.Write("<script>window.open('Download_Pdf?filename=" + filename + "');window.location.href='List';</script>");

            return new EmptyResult();
        }

        public ActionResult Download_Pdf(string filename)
        {
            string tempPath = "C:\\inetpub\\NIS\\Images\\" + filename;

            FileInfo fileInfo = new FileInfo(tempPath);
            Response.Clear();
            Response.ClearContent();
            Response.ClearHeaders();
            Response.AddHeader("Content-Disposition", "attachment;filename=Report.pdf");
            Response.AddHeader("Content-Length", fileInfo.Length.ToString());
            Response.AddHeader("Content-Transfer-Encoding", "binary");
            Response.ContentType = "application/vnd.ms-excel";
            Response.ContentEncoding = Encoding.UTF8;
            Response.WriteFile(fileInfo.FullName);
            Response.Flush();
            Response.End();
            fileInfo.Delete();

            return new EmptyResult();
        }

        /// <summary>
        /// 更新護理紀錄
        /// </summary>
        /// <param name="time">紀錄時間</param>
        /// <param name="id">P_KEY</param>
        /// <param name="title">標題(可為空值)</param>
        /// <param name="C">內容:一般(可為空值)</param>
        /// <param name="S">內容:主觀(可為空值)</param>
        /// <param name="O">內容:客觀(可為空值)</param>
        /// <param name="I">內容:執行(可為空值)</param>
        /// <param name="E">內容:評值(可為空值)</param>
        public int upd_CareRecord(string time, string id, string title, string C, string S, string O, string I, string E
            , string ECKImgDate, string emr = "" , string self ="" ,string content = "")
        {
            int erow = 0;
            if (Session["PatInfo"] != null)
            {
                string userno = userinfo.EmployeesNo;
                string sign_userno = care_record_m.sel_guide_userno(userno, Convert.ToDateTime(time), Convert.ToDateTime(time).Hour);
                DateTime NowTime = DateTime.Now;
                List<DBItem> insertDataList = new List<DBItem>();
                insertDataList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("GUIDE_NO", sign_userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDTIME", NowTime.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("RECORDTIME", time, DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("TITLE", base.trans_date(title), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("C", base.trans_date(C), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("S", base.trans_date(S), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("O", base.trans_date(O), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("I", base.trans_date(I), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("E", base.trans_date(E), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("SIGN", "N", DBItem.DBDataType.String));
                string where = " CARERECORD_ID || SELF = '" + id + "' ";

                erow = care_record_m.DBExecUpdate("CARERECORD_DATA", insertDataList, where);

                if (erow > 0 && id != "")
                {
                    insertDataList.Clear();
                    insertDataList.Add(new DBItem("STATUS", "M", DBItem.DBDataType.String));
                    string EMRwhere = " RECORD_KEY  = '" + emr + "' ";
                    if(self != "")
                    {
                        EMRwhere += "AND SELF = '" + self + "'";
                    }
                    erow = care_record_m.DBExecUpdate("NIS_EMR_PACKAGE_DTL", insertDataList, EMRwhere);

                }

                #region 處理EKG圖片
                if (!string.IsNullOrEmpty(ECKImgDate))
                {
                    try
                    {
                        link.DBCmd.CommandText = "UPDATE CARERECORD_DATA SET EKG = :EKG "
                                + " WHERE FEENO = '" + base.ptinfo.FeeNo + "' AND CARERECORD_ID = '" + id + "' ";
                        //link.DBCmd.Parameters.Add(":EKG", SqlDbType.Binary).Value = Convert.FromBase64String(ECKImgDate);
                        //link.DBCmd.Parameters.Add(":EKG", Convert.FromBase64String(ECKImgDate));
                        byte[] arr = Convert.FromBase64String(ECKImgDate);
                        link.DBCmd.Parameters.Add(":EKG", OracleDbType.Blob).Value = arr;

                        link.DBOpen();
                        link.DBCmd.ExecuteNonQuery();
                        link.DBClose();
                        arr = null;
                    }
                    catch (Exception ex)
                    {
                        //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                        string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                        string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                        write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                    }
                    finally
                    {
                        this.link.DBClose();
                    }
                }

                #endregion

                byte[] allergen = webService.GetAllergyList(ptinfo.FeeNo);
                string ptJsonArr = string.Empty;
                string allergyDesc = string.Empty;
               
                if (allergen != null)
                {
                    ptJsonArr = CompressTool.DecompressString(allergen);
                }
                List<NIS.Data.PatientInfo> patList = JsonConvert.DeserializeObject<List<NIS.Data.PatientInfo>>(ptJsonArr);

                if (allergen != null)
                {
                    allergyDesc = patList[0].AllergyDesc;
                }

                string xml = care_record_m.care_Record_Get_xml(ptinfo.PatientName, ptinfo.ChartNo,
                   ptinfo.PatientGender, (ptinfo.Age).ToString(), ptinfo.BedNo, ptinfo.InDate.ToString("yyyyMMdd"),
                   ptinfo.InDate.ToString("HHmm"), allergyDesc, Convert.ToDateTime(time).ToString("yyyyMMdd"),
                   Convert.ToDateTime(time).ToString("HHmm"), userinfo.EmployeesName, base.trans_date(title), this.GetRecordStr(id), ECKImgDate);

                if (erow > 0)
                {
                    //將紀錄回寫至 EMR Temp Table
                    ////string sqlstr = "begin P_NIS_EMRMS('" + ptinfo.FeeNo + "','014','護理紀錄單','" + id + "','" + time + "','" + sign_userno + "','I');end;";
                    ////care_record_m.DBExec(sqlstr);
                    #region --EMR--
                    //取得應簽章人員
                    byte[] listByteCode = webService.UserName(userinfo.Guider);
                    string listJsonArray = CompressTool.DecompressString(listByteCode);
                    UserInfo user_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                    string RecordTime = NowTime.ToString("yyyy/MM/dd HH:mm:ss");
                    string Temp_NowTime_Str = Convert.ToDateTime(RecordTime).ToString("yyyyMMddHHmmss");//時間採統一變數
                    string EmrXmlString = this.get_xml(
                        NowTime.ToString("yyyyMMddHHmmss.fffffff"), Temp_NowTime_Str + GetMd5Hash(id), "A000040", Convert.ToDateTime(time).ToString("yyyyMMdd"),
                        GetMd5Hash(id), Convert.ToDateTime(time).ToString("yyyyMMdd"), "", "",
                         user_info.EmployeesNo, user_info.EmployeesName, user_info.UserID, ptinfo.ChartNo, ptinfo.PatientName,
                        ptinfo.PatientID, ptinfo.PayInfo,
                        "C:\\EMR\\", "A000040" + GetMd5Hash(id) + Temp_NowTime_Str + ".xml", listJsonArray, "upd_CareRecord"
                        );

                    erow = EMR_Sign(time, emr, "", title, self,"");
                    //SaveEMRLogData(id, GetMd5Hash(id), "EMR", RecordTime, "A000040" + GetMd5Hash(id) + Temp_NowTime_Str, xml);
                    //SaveEMRLogData(id, GetMd5Hash(id), "Temp", RecordTime, Temp_NowTime_Str + "-" + GetMd5Hash(id), EmrXmlString);

                    #endregion

                    #region JAG 簽章
                    //// 20150608 EMR
                    //string strPath = @"C:\\wkhtmltopdf\\wkhtmltopdf.exe";
                    //string filename = @"C:\inetpub\NIS\Images\" + id + ".pdf";

                    //string url = Request.Url.AbsoluteUri.ToString().Replace("Update", "List_PDF?id=" + id + "&feeno=" + ptinfo.FeeNo);
                    //Process p = new Process();
                    //p.StartInfo.FileName = strPath;
                    //p.StartInfo.Arguments = url + " " + filename;
                    //p.StartInfo.UseShellExecute = true;
                    //p.Start();
                    //p.WaitForExit();

                    //byte[] listByteCode = nis.UserName(userinfo.EmployeesNo);
                    //string listJsonArray = CompressTool.DecompressString(listByteCode);
                    //UserInfo user_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                    //string emp_id = (user_info.UserID != null) ? user_info.UserID.Trim() : "";
                    //string emp_name = (user_info.EmployeesName != null) ? user_info.EmployeesName.Trim() : "";
                    //string dep_no = ptinfo.DeptNo;
                    //string chr_no = ptinfo.ChartNo;
                    //string pat_name = ptinfo.PatientName;
                    //string in_date = ptinfo.InDate.ToString("yyyyMMdd");
                    //string chagre_type = (ptinfo.PayInfo == "健保") ? "001" : "000";
                    //int result = emr_sign(id + "CARERECORD", ptinfo.FeeNo, "014", userno, emp_name, emp_id, dep_no, chr_no, pat_name, in_date, chagre_type, filename);
                    #endregion

                }
            }

            return erow;
        }

        public string GetRecordStr(string TableIDself)
        {
            //DBConnector dbconnector = new DBConnector();
            string Sql = "SELECT * FROM CARERECORD_DATA WHERE CARERECORD_ID || SELF='" + TableIDself + "'  AND DELETED IS NULL";
            DataTable dt = link.DBExecSQL(Sql);
            string msg = "";
            if (dt != null && dt.Rows.Count > 0)
            {
                if (base.trans_date(dt.Rows[0]["C_OTHER"].ToString()) + base.trans_date(dt.Rows[0]["C"].ToString()) != "")
                    msg += base.trans_date(dt.Rows[0]["C_OTHER"].ToString()) + base.trans_date(dt.Rows[0]["C"].ToString()) + "\n";
                if (base.trans_date(dt.Rows[0]["S_OTHER"].ToString()) + base.trans_date(dt.Rows[0]["S"].ToString()) != "")
                    msg += "S:" + base.trans_date(dt.Rows[0]["S_OTHER"].ToString()) + base.trans_date(dt.Rows[0]["S"].ToString()) + "\n";
                if (base.trans_date(dt.Rows[0]["O_OTHER"].ToString()) + base.trans_date(dt.Rows[0]["O"].ToString()) != "")
                    msg += "O:" + base.trans_date(dt.Rows[0]["O_OTHER"].ToString()) + base.trans_date(dt.Rows[0]["O"].ToString()) + "\n";
                if (base.trans_date(dt.Rows[0]["I_OTHER"].ToString()) + base.trans_date(dt.Rows[0]["I"].ToString()) != "")
                    msg += "I:" + base.trans_date(dt.Rows[0]["I_OTHER"].ToString()) + base.trans_date(dt.Rows[0]["I"].ToString()) + "\n";
                if (base.trans_date(dt.Rows[0]["E_OTHER"].ToString()) + base.trans_date(dt.Rows[0]["E"].ToString()) != "")
                    msg += "E:" + base.trans_date(dt.Rows[0]["E_OTHER"].ToString()) + base.trans_date(dt.Rows[0]["E"].ToString()) + "\n";
            }
            return msg;
        }

        #endregion

        #region 列印_北醫

        /*//頁首頁尾的page事件
        public class HandleFooter : PdfPageEventHelper
        {
            public string[] _patientinfo;
            PdfContentByte cb;
            PdfTemplate tmp;
            BaseFont bf = null;
            DateTime prtTime = DateTime.Now;

            public override void OnOpenDocument(PdfWriter writer, Document document)
            {
                try
                {
                    string fontPath = Environment.GetFolderPath(Environment.SpecialFolder.System) + @"\..\Fonts\kaiu.ttf";
                    prtTime = DateTime.Now;
                    bf = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);
                    cb = writer.DirectContent;
                    tmp = cb.CreateTemplate(50, 50);
                }
                catch { }
            }
            
            public override void OnEndPage(PdfWriter writer, Document document)
            {
                base.OnEndPage(writer, document);
                int pageN = writer.PageNumber;
                string text = "Page " + pageN;
                float len = bf.GetWidthPoint(text, 8);
                iTextSharp.text.Rectangle PageSize = document.PageSize;
                cb.SetRGBColorFill(100, 100, 100);

                //頁首
                cb.BeginText();
                cb.SetFontAndSize(bf, 15);
                cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER, "護 理 紀 錄 單", PageSize.GetRight(300), PageSize.GetTop(20), 0);
                cb.EndText();

                //頁首第二行
                cb.BeginText();
                cb.SetFontAndSize(bf, 12);
                cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, "    病歷號：" + _patientinfo[0] + "   姓名：" + _patientinfo[1] + "   床號：" + _patientinfo[2] + "   出生日期：" + _patientinfo[3] + "   性別：" + _patientinfo[4], PageSize.GetLeft(20), PageSize.GetTop(40), 0); //vanda新增參數
                cb.EndText();

                //頁尾左下方頁碼
                cb.BeginText();
                cb.SetFontAndSize(bf, 8);
                cb.SetTextMatrix(PageSize.GetLeft(10), PageSize.GetBottom(10));
                //20140924 更新pdf列印為各院名稱
                string HospitalPrint = System.Configuration.ConfigurationManager.AppSettings["HospitalPrint"].ToString();
                cb.ShowText("" + HospitalPrint + "");
                cb.EndText();
                cb.AddTemplate(tmp, PageSize.GetLeft(10) + len, PageSize.GetBottom(10));

                //頁尾右下方資訊
                cb.BeginText();
                cb.SetFontAndSize(bf, 8);
                cb.ShowTextAligned(PdfContentByte.ALIGN_RIGHT, "第  " + pageN.ToString() + "  頁", PageSize.GetRight(10), PageSize.GetBottom(10), 0);
                cb.EndText();
            }

            public override void OnCloseDocument(PdfWriter writer, Document document)
            {
                base.OnCloseDocument(writer, document);
                tmp.BeginText();
                tmp.SetFontAndSize(bf, 8);
                tmp.SetTextMatrix(0, 0);
                tmp.EndText();
            }
        }
        */

        //public ActionResult exportPDF(string startdate, string enddate, string feeno)
        //{
        //    if (feeno != null)
        //    {
        //        byte[] ptinfoByteCode = webService.GetPatientInfo(feeno);
        //        if (ptinfoByteCode != null)
        //        {
        //            string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
        //            PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
        //            DataTable Record = care_record_m.sel_carerecord(feeno, "", "", Convert.ToDateTime(startdate).ToString("yyyy/MM/dd HH:mm"), Convert.ToDateTime(enddate).ToString("yyyy/MM/dd HH:mm:59"), "");
        //            //宣告字型
        //            string fontPath = System.Environment.GetEnvironmentVariable("windir") + @"\Fonts\mingliu.ttc";
        //            string fontName = "新細明體";
        //            FontFactory.Register(fontPath);
        //            iTextSharp.text.Font fontTh = FontFactory.GetFont(fontName, BaseFont.IDENTITY_H, 12, iTextSharp.text.Font.BOLD);
        //            iTextSharp.text.Font fontContentText = FontFactory.GetFont(fontName, BaseFont.IDENTITY_H, 12, iTextSharp.text.Font.NORMAL);

        //            //自訂DataTable
        //            DataTable dt = new DataTable();
        //            dt.Columns.Add("TIME");
        //            dt.Columns.Add("TITLE");
        //            dt.Columns.Add("CONTENT");
        //            dt.Columns.Add("NAME");
        //            foreach (DataRow r in Record.Rows)
        //            {
        //                DataRow dr = dt.NewRow();

        //                DateTime record_time = Convert.ToDateTime(r["RECORDTIME"]);
        //                int year = record_time.AddYears(-1911).Year;
        //                string msg = "";

        //                if (r["C"].ToString() + r["C_OTHER"].ToString() != "")
        //                    msg += r["C"].ToString() + r["C_OTHER"].ToString() + "\n";
        //                if (r["S"].ToString() + r["S_OTHER"].ToString() != "")
        //                    msg += "S:" + r["S"].ToString() + r["S_OTHER"].ToString() + "\n";
        //                if (r["O"].ToString() + r["O_OTHER"].ToString() != "")
        //                    msg += "O:" + r["O"].ToString() + r["O_OTHER"].ToString() + "\n";
        //                if (r["I"].ToString() + r["I_OTHER"].ToString() != "")
        //                    msg += "I" + r["I"].ToString() + r["I_OTHER"].ToString() + "\n";
        //                if (r["E"].ToString() + r["E_OTHER"].ToString() != "")
        //                    msg += "E" + r["E"].ToString() + r["E_OTHER"].ToString() + "\n";

        //                dr["TIME"] = year.ToString() + record_time.ToString("-MM-dd HH:mm");
        //                dr["TITLE"] = r["TITLE"].ToString();
        //                dr["CONTENT"] = msg;
        //                dr["NAME"] = r["CREATNAME"].ToString();
        //                dt.Rows.Add(dr);
        //            }

        //            //宣告doc_設定左右上下間距
        //            var doc = new Document(PageSize.A4, 45, 45, 50, 30);
        //            //把A4變橫式
        //            //doc.SetPageSize(iTextSharp.text.PageSize.A4.Rotate());
        //            //宣告pdfWriter暫存至記憶體
        //            MemoryStream Memory = new MemoryStream();
        //            PdfWriter pdfWriter = PdfWriter.GetInstance(doc, Memory);
        //            //宣告頁首頁尾
        //            HandleFooter _hf = new HandleFooter();
        //            string birthday = pi.Birthday.AddYears(-1911).Year.ToString() + "-" + pi.Birthday.ToString("MM-dd") + "(" + pi.Age + "歲)";
        //            _hf._patientinfo = new string[] { pi.ChartNo, pi.PatientName, pi.BedNo, birthday, pi.PatientGender };
        //            //新增頁首頁尾
        //            pdfWriter.PageEvent = _hf;

        //            doc.Open();

        //            //設定內容表格_相對寬度 
        //            PdfPTable table = new PdfPTable(new float[] { 1, 1, 5, 1 });
        //            table.WidthPercentage = 100;
        //            //宣告表格
        //            PdfPCell content = new PdfPCell();
        //            //新增表格_抬頭
        //            content = new PdfPCell(new Phrase("時間", fontTh));
        //            content.HorizontalAlignment = Element.ALIGN_CENTER;
        //            table.AddCell(content);
        //            content = new PdfPCell(new Phrase("記錄", fontTh));
        //            content.Colspan = 3;
        //            content.HorizontalAlignment = Element.ALIGN_CENTER;
        //            content.MinimumHeight = 20;
        //            table.AddCell(content);

        //            foreach (DataRow _dr in dt.Rows)
        //            {
        //                foreach (DataColumn _col in dt.Columns)
        //                    table.AddCell(new Phrase(_dr[_col.ColumnName].ToString().Trim(), fontContentText));
        //            }
        //            //輸出文件
        //            doc.Add(table);
        //            doc.Close();
        //            Response.Clear();
        //            Response.AddHeader("Content-Disposition", "attachment;filename=CareRecord.pdf");
        //            Response.ContentType = "application/octet-stream";
        //            Response.OutputStream.Write(Memory.GetBuffer(), 0, Memory.GetBuffer().Length);
        //            Response.OutputStream.Flush();
        //            Response.OutputStream.Close();
        //            Response.Flush();
        //            Response.End();
        //            return new EmptyResult();
        //        }
        //    }
        //    return new EmptyResult();
        //}
        #endregion


        //EKG清單
        public ActionResult EKGList ()
        {
            string chartNO = ptinfo.ChartNo.ToString().Trim();
            DataTable dt = care_record_m.get_EKG(chartNO);
            ViewBag.dt = dt;

            return View();
        }
        //新增EKG
        [HttpPost]
        [ValidateInput(false)]
        public int InsertEKG(List<EKGData> data)
        {
            if (Session["PatInfo"] != null)
            {
                for(int i = 0; i < data.Count; i++)
                {
                    string EKGBase64 = "";
                    string date = data[i].DATE;
                    DateTime transDate = DateTime.Parse(date);
                    string id = creatid("CARERECORD", userinfo.EmployeesNo, ptinfo.FeeNo, "0");
                    string userno = userinfo.EmployeesNo;
                    string feeno = ptinfo.FeeNo;
                    string sign_userno = care_record_m.sel_guide_userno(userno, Convert.ToDateTime(date), Convert.ToDateTime(date).Hour);
                    DateTime NowTime = DateTime.Now;
                    List<DBItem> insertDataList = new List<DBItem>();

                    insertDataList.Add(new DBItem("CARERECORD_ID", id, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("GUIDE_NO", sign_userno, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATTIME", NowTime.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("RECORDTIME", transDate.ToString("yyyy-MM-dd HH:mm"), DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("SELF", "CARERECORD", DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("SIGN", "N", DBItem.DBDataType.String));

                    int erow = care_record_m.DBExecInsert("CARERECORD_DATA", insertDataList);
                    string msg = "";

                    #region 處理EKG圖片
                    if (data[i].EKG != "")
                    {
                        var url = data[i].EKG;
                        try
                        {
                            byte[] arr = null;
                            using (var webClient = new MyWebClient())
                            {
                                arr = webClient.DownloadData(url);
                            }
                            EKGBase64 = Convert.ToBase64String(arr);
                            link.DBCmd.CommandText = "UPDATE CARERECORD_DATA SET EKG = :EKG "
                                    + " WHERE FEENO = '" + base.ptinfo.FeeNo + "' AND CARERECORD_ID = '" + id + "' ";

                            link.DBCmd.Parameters.Add(":EKG", OracleDbType.Blob).Value = arr;

                            link.DBOpen();
                            link.DBCmd.ExecuteNonQuery();
                            link.DBClose();

                            arr = null;
                        }
                        catch (Exception ex)
                        {
                            //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                            string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                            string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                            write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                        }
                        finally
                        {
                            this.link.DBClose();
                        }
                    }
                    #endregion

                    //儲存成功
                    if (erow > 0)
                    {
                        erow = EMR_Sign(transDate.ToString("yyyy-MM-dd HH:mm"), id, msg, "", "CARERECORD");

                        return 1;
                    }
                    else
                        Response.Write("<script>alert('新增失敗');window.location.href='List';</script>");

                    return 0;
                }
                //session過期
                Response.Write("<script>alert('請重新選擇病患');</script>");
            }
         
            return 0;
        }
        //新增EKG

        public ActionResult InsertEKGtoNIS(EKG data)
        {
            RESPONSE_MSG jsonResult = new RESPONSE_MSG();
            try
            {
                if (data != null)
                {

                        string EKGBase64 = "";
                        string imgevent = data.IMGEVENT.ToString();

                        List<EVENT> imgeventArr = JsonConvert.DeserializeObject<List<EVENT>>(imgevent);
                        
                        List<string> result = new List<string>();
                        int medCount = 0;
                        for (int i = 0; i < imgeventArr.Count(); i++)
                        {
                            if (imgeventArr[i].name == "CodeMarker" )
                            {
                                string med = imgeventArr[i].result.ToString();
                                medCount++;
                                med = med.Replace("Description", "藥物" + medCount);
                                result.Add(med);
                            }
                            if (imgeventArr[i].name == "Shock")
                            {
                                string shock = imgeventArr[i].result.ToString();
                                var shocharr = shock.Split(',');
                                if(shocharr != null)
                                {
                                  shock = shocharr[0].Replace("Joules", "電擊" + shocharr[1].Replace("Count: ", "")) + " 焦耳數";
                            }
                                result.Add(shock);
                            }
                        }
                        string ekgcontent = String.Join("。", result);
                        string content = "";
                        
                        if(result != null && ekgcontent != "")
                        {
                            content = "事件 : " + ekgcontent + "。";
                        }
                        
                        string date = data.IMGDATETIME;
                        DateTime transDate = DateTime.Parse(date);
                        string id = data.PKEY.Trim() + data.HISNUM.Trim() + data.IMGSEQ.Trim();
                        string userno = data.MODIFYUSR;
                        string ChartNo = data.HISNUM;
                        string feeno = "";
                        string EmployeesName = "";

                        byte[] doByteCode = webService.GetInHistory(ChartNo);
                        if (doByteCode != null)
                        {
                            string doJsonArr = CompressTool.DecompressString(doByteCode);
                            List<InHistory> IpdList = JsonConvert.DeserializeObject<List<InHistory>>(doJsonArr);
                            if (IpdList.Count > 0)
                            {
                                feeno = IpdList[0].FeeNo;
                            }
                        }

                        if (userno != "")
                         {
                                byte[] listByteCode = webService.UserName(userno);
                                string listJsonArray = CompressTool.DecompressString(listByteCode);
                                UserInfo user_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                                EmployeesName = user_info.EmployeesName.Trim();
                         }
      

                        string sign_userno = care_record_m.sel_guide_userno(userno, Convert.ToDateTime(date), Convert.ToDateTime(date).Hour);
                        DateTime NowTime = DateTime.Now;
                        List<DBItem> insertDataList = new List<DBItem>();

                        insertDataList.Add(new DBItem("CARERECORD_ID", id, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CREATNAME", EmployeesName, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("GUIDE_NO", sign_userno, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("O_OTHER", content, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CREATTIME", NowTime.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("RECORDTIME", transDate.ToString("yyyy-MM-dd HH:mm"), DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("SELF", "EKG", DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("TITLE", "EKG", DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("SIGN", "N", DBItem.DBDataType.String));

                        int erow = care_record_m.DBExecInsert("CARERECORD_DATA", insertDataList);
                        string msg = "";

                        #region 處理EKG圖片
                        if (data.IMGFILENAME != "")
                        {
                            var url = data.IMGFILENAME;
                            try
                            {
                            if(url != "" && url != null)
                            {
                                byte[] arr = null;
                                using (var webClient = new MyWebClient())
                                {
                                    arr = webClient.DownloadData(url);
                                }
                                EKGBase64 = Convert.ToBase64String(arr);
                                link.DBCmd.CommandText = "UPDATE CARERECORD_DATA SET EKG = :EKG "
                                        + " WHERE FEENO = '" + feeno + "' AND CARERECORD_ID = '" + id + "' ";

                                link.DBCmd.Parameters.Add(":EKG", OracleDbType.Blob).Value = arr;

                                link.DBOpen();
                                link.DBCmd.ExecuteNonQuery();
                                link.DBClose();

                                arr = null;
                            }

                            }
                            catch (Exception ex)
                            {
                                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                            }
                            finally
                            {
                                this.link.DBClose();
                            }
                        }
                        #endregion

                        //儲存成功
                        if (erow > 0)
                        {
                            erow = EMR_Sign(transDate.ToString("yyyy-MM-dd HH:mm"), id, msg, "EKG", "EKG", feeno, userno);

                            jsonResult.status = RESPONSE_STATUS.SUCCESS;
                        }
                        else
                            jsonResult.status = RESPONSE_STATUS.ERROR;
                            return Content(JsonConvert.SerializeObject(jsonResult), "application/json");

                    
                }
                else
                {
                    jsonResult.status = RESPONSE_STATUS.SUCCESS;
                    return Content(JsonConvert.SerializeObject(jsonResult), "application/json");
                }
            }
            catch(Exception e)
            {
                jsonResult.status = RESPONSE_STATUS.ERROR;
            }

            return Content(JsonConvert.SerializeObject(jsonResult), "application/json");
        }
        //刪除EKG
        public ActionResult DeleteEKGtoNIS(EKG data)
        {
            RESPONSE_MSG jsonResult = new RESPONSE_MSG();
            try
            {
                if (data != null )
                {
                    string id = data.PKEY.Trim() + data.HISNUM.Trim() + data.IMGSEQ.Trim();
                    string userNO = data.MODIFYUSR.ToString().Trim();
                    int erow = 0;

                    erow = base.Del_CareRecordEKG(id, "EKG", true, userNO);

                    if (erow > 0)
                    {
                        jsonResult.status = RESPONSE_STATUS.SUCCESS;
                    }
                    else
                        jsonResult.status = RESPONSE_STATUS.ERROR;
                }
            }
            catch(Exception ex )
            {
                jsonResult.status = RESPONSE_STATUS.ERROR;
            }

            return Content(JsonConvert.SerializeObject(jsonResult), "application/json");
        }
        public class MyWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri uri)
            {
                WebRequest WR = base.GetWebRequest(uri);
                WR.Timeout = 10 * 1000;
                return WR;
            }
        }
        public class EKG
        {
            public string PKEY { set; get; }

            public string HISNUM { set; get; }

            public string IMGSEQ { set; get; }

            public string IMGFILENAME { set; get; }

            public string IMGDATETIME { set; get; }

            public string IMGEVENT { set; get; }

            public string IMGVITALSIGN { set; get; }

            public string MODIFYDT { set; get; }

            public string MODIFYUSR { set; get; }
        }
        public class EKGData
        {
            public string EKG { set; get; }

            public string DATE { set; get; }

        }
        public class EVENT
        {
            public int id { get; set; }
            public string code { get; set; }
            public string name { get; set; }
            public string result { get; set; }
            public bool isVitalsign { get; set; }
            public bool isEvent { get; set; }
            public string resultDatetime { get; set; }
            public int fulTypeId { get; set; }
        }
    }
}
