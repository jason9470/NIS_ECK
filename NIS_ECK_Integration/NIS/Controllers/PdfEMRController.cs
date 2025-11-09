using Com.Mayaminer;
using Newtonsoft.Json;
using NIS.Data;
using NIS.Models;
using NIS.UtilTool;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static NIS.Models.PdfEMR;

namespace NIS.Controllers
{
    public class PdfEMRController : BaseController
    {
        private LogTool log;
        private DBConnector link;
        private PrintController PrintC;
        public PdfEMRController()
        {
            this.log = new LogTool();
            this.link = new DBConnector();
            this.PrintC = new PrintController();

        }
        public ActionResult Index()
        {
            return View();
        }
        #region 產生PDF EMR簽章

        public string GetPDF_EMR(string source_type, string fee_no, string user_no, string emr_source, string pk_id = "", string mom_fee_no ="")
        {
            EMR_pdf_json obj = new EMR_pdf_json();
            RESPONSE_MSG json_result = new RESPONSE_MSG();
            string sour_code = string.Empty,guider_no = string.Empty, re_jsonstr = string.Empty ;
            var test = userinfo;
            var pt = ptinfo;
            bool success = false;
            switch (emr_source)
            {
                case "Production_Record"://生產紀錄單
                    sour_code = "IA070001";
                    break;
                case "Insert_BabyEntr"://嬰幼兒入評
                    sour_code = "IA051001";
                    break;
                case "Insert_NBENTR"://新生兒入評
                    sour_code = "IA080004";
                    break;
                case "Child_Birth"://嬰兒出生紀錄單
                    sour_code = "IA080003";
                    break;
                default:
                    break;
            }
            #region 病人資料
            if (ptinfo ==null)
            {
                byte[] ptinfo_byte = webService.GetPatientInfo(fee_no);
                string ptJsonArr = string.Empty;
                if (ptinfo_byte != null)
                {
                    ptJsonArr = CompressTool.DecompressString(ptinfo_byte);
                    ptinfo = JsonConvert.DeserializeObject<PatientInfo>(ptJsonArr);
                }

            }

            #endregion

            #region 資料輸入人員(簽章產生人員)
            if (userinfo == null)
            {
                byte[] listByteCode1 = webService.UserName(user_no);
                string listJsonArray1 = string.Empty;
                if (listByteCode1 != null)
                {
                    listJsonArray1 = CompressTool.DecompressString(listByteCode1);
                    userinfo = JsonConvert.DeserializeObject<UserInfo>(listJsonArray1);
                    guider_no = RequestName(userinfo.EmployeesNo);//抓不到代簽人ID則以自己簽 由ECK自行處理錯誤資料
                }


                if (string.IsNullOrEmpty(guider_no))
                {
                    guider_no = user_no;
                }
                userinfo.Guider = guider_no;

            }
            #endregion

            #region 取得應簽章人員(有可能是原資料輸入人員或簽章產生人員)
            UserInfo guider_info = new UserInfo();
            string listJsonArray = string.Empty;
            byte[] listByteCode = webService.UserName(userinfo.Guider);//代簽人ID
            if (listByteCode != null)
            {
                listJsonArray = CompressTool.DecompressString(listByteCode);//為了紀錄LOG
                guider_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
            }
            #endregion

            if (source_type == "Btn")
            {
                re_jsonstr = GetPDF_EMRinBtn(obj, guider_info, sour_code, emr_source);

            }
            else if (source_type == "Save")
            {
                re_jsonstr = GetPDF_EMRinSave(obj, guider_info, sour_code, pk_id, emr_source,ref success, mom_fee_no);
            }
            return re_jsonstr;
        }

        /// <summary>
        /// 獨立轉歸鍵產生PDF檔
        /// </summary>
        [HttpGet]
        public string GetPDF_EMRinBtn(EMR_pdf_json obj, UserInfo guider_info,string sour_code, string emr_source)
        {
            DateTime NowDtime = DateTime.Now;
            string pk_id = string.Empty;
            string RequestDocOrderDate = string.Empty;
            string sqlstr = string.Empty, HeaderVal = string.Empty, FooterVal = string.Empty;
            DataTable Dt = new DataTable();
            string char_no = ptinfo.ChartNo;
            string fee_no = ptinfo.FeeNo;
            string user_no = userinfo.EmployeesNo;

            string listJsonArray = JsonConvert.SerializeObject(guider_info);//為了紀錄LOG

            switch (emr_source)
            {
                case "Child_Birth": //嬰兒出生紀錄單
                    sqlstr = "select IID from OBS_NB WHERE NB_CHARTNO='" + char_no + "'";
                    link.DBExecSQL(sqlstr, ref Dt);
                    if (Dt.Rows.Count > 0)
                    {
                        pk_id = Dt.Rows[0]["IID"].ToString();

                    }

                    Dt = new DataTable();
                    sqlstr = "select BIRTH_DAY from OBS_NB WHERE NB_CHARTNO='" + char_no + "'";
                    this.link.DBExecSQL(sqlstr, ref Dt);
                    if (Dt.Rows.Count > 0)
                    {
                        try
                        {
                            RequestDocOrderDate = Convert.ToDateTime(Dt.Rows[0]["BIRTH_DAY"].ToString()).ToString("yyyyMMdd");
                        }
                        catch (Exception)
                        {

                            throw;
                        }
                    }
                    obj.titleStr = "嬰兒出生紀錄單";
                    obj.marginTop = "23";
                    obj.titleUrl = GetSourceUrl() + "/BabyBorn/ENTR_HEADER?feeno=" + fee_no + "&Type=" + emr_source;
                    obj.footLeft = "(2019_Q2)恩主公醫院BAIA080-003-02";
                    break;
                default:
                    break;
            }
            pk_id = GetMd5Hash(pk_id);

            string filename = sour_code+ "_" + fee_no + "_" + char_no + "_" + NowDtime.ToString("yyyyMMddHHmmss");
            System.Globalization.TaiwanCalendar taiwanCalendar = new System.Globalization.TaiwanCalendar();
            string TaiwnYear = taiwanCalendar.GetYear(NowDtime).ToString();
            string GetMonth = NowDtime.ToString("MM");
            int Return_Msg = 0;

            try
            {
                //檢查資料夾是否存在
                if (Directory.Exists(@"\\172.20.110.157\ToEMR\MAYA-NIS\" + TaiwnYear + @"\" + GetMonth))
                {
                    //資料夾存在
                }
                else
                {
                    //新增資料夾
                    Directory.CreateDirectory(@"\\172.20.110.157\ToEMR\MAYA-NIS\" + TaiwnYear + @"\" + GetMonth);
                }

                string tempfilePath = "C:\\EMR\\";
                string filePath = @"\\172.20.110.157\ToEMR\MAYA-NIS\" + TaiwnYear + @"\" + GetMonth + @"\";

                string Parameters = Request.Url.Query + "&DelFile=false&filename=" + filename;
                string obj_toJson = JsonConvert.SerializeObject(obj);
                PrintC.GetPDF(true, filename, Parameters, obj_toJson);
                System.IO.File.Move(tempfilePath + filename +".pdf", filePath + filename + ".pdf");//將存在本機的簽章 搬至 指定位置

                string EmrXmlString = this.getPDF_xml(
                    NowDtime, sour_code, pk_id, ptinfo.InDate.ToString("yyyyMMdd"), fee_no, RequestDocOrderDate, userinfo.CostCenterCode, guider_info.CostCenterCode, guider_info.EmployeesNo,
                    guider_info.EmployeesName, guider_info.UserID, char_no, ptinfo.PatientName, ptinfo.PatientID, ptinfo.PayInfo, filePath, filename, listJsonArray, emr_source
                    );
                string ErrMsg = "";
                string[] VerifyStr = GetVerifyStr();
                PutJTEMRWsService_WS.EnvirMode ws_status = PutJTEMRWsService_WS.EnvirMode.TestEnvir;
                if (NIS.MvcApplication.iniObj.NisSetting.ServerMode.ToString() == "Production")
                {
                    ws_status = PutJTEMRWsService_WS.EnvirMode.ProductionEnvir;
                }
                Return_Msg = _EMRnis.UploadEMRFile(ws_status, VerifyStr, EmrXmlString, filePath + filename + ".pdf", ref ErrMsg);

                if (Return_Msg > 0)
                {
                    Response.Write("<script>alert('簽章產生失敗(" + Return_Msg + ")，請連絡資訊室人員。');</script>");
                }
                else
                {
                    Response.Write("<script>alert('簽章產生成功。');</script>");
                }
            }
            catch (Exception ex)
            {
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                Response.Write("<script>alert('簽章產生失敗，請連絡資訊室人員。');</script>");

            }
            finally
            {
                Response.Write("<script>window.close();</script>");
            }
            return filename;

        }

        /// <summary>
        /// 存檔後產生PDF檔
        /// </summary>
        [HttpGet]
        public string GetPDF_EMRinSave(EMR_pdf_json obj,UserInfo guider_info,string sour_code, string pk_id,string emr_source, ref bool success,  string mom_fee_no = "")
        {
            RESPONSE_MSG json_result = new RESPONSE_MSG();
            DateTime NowDtime = DateTime.Now;
            string sqlstr = string.Empty, HeaderVal=string.Empty, FooterVal =string.Empty;
            string ChartNo = ptinfo.ChartNo;
            string fee_no = ptinfo.FeeNo;
            string user_no = userinfo.EmployeesNo;
            string listJsonArray = JsonConvert.SerializeObject(guider_info);//為了紀錄LOG
            DataTable Dt = new DataTable();

            string Parameters = string.Empty;
            string filename = sour_code + "_" + fee_no + "_" + ChartNo + "_" + NowDtime.ToString("yyyyMMddHHmmss");
            switch (emr_source)
            {
                case "Production_Record":
                    #region 流動護理人員簽章
                    Dt = new DataTable();
                    sqlstr = "select CLNURSE_1,CLNURSE_2 from OBS_BTHSTA WHERE feeno='" + fee_no + "'";
                    this.link.DBExecSQL(sqlstr, ref Dt);
                    if (Dt.Rows.Count > 0)
                    {
                        try
                        {
                            string temp_id = (string.IsNullOrEmpty(Dt.Rows[0]["CLNURSE_2"].ToString())) ? Dt.Rows[0]["CLNURSE_1"].ToString() : Dt.Rows[0]["CLNURSE_2"].ToString();
                            string Creatno = RequestName(temp_id);//抓不到代簽人ID則以自己簽 由ECK自行處理錯誤資料
                            if (string.IsNullOrEmpty(Creatno))
                            {
                                Creatno = temp_id;
                            }

                            byte[] listByteCode = webService.UserName(temp_id);//代簽人ID
                            if (listByteCode != null)
                            {
                                listJsonArray = CompressTool.DecompressString(listByteCode);//為了紀錄LOG
                                userinfo = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);//userinfo

                            }

                            listByteCode = webService.UserName(Creatno);//代簽人ID
                            if (listByteCode != null)
                            {
                                listJsonArray = CompressTool.DecompressString(listByteCode);//為了紀錄LOG
                                guider_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);//userinfo
                            }

                        }
                        catch (Exception ex)
                        {
                            string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                            string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                            write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);

                            json_result.status = RESPONSE_STATUS.EXCEPTION;
                            json_result.message = "人員資料有問題造成簽章產生失敗，請連絡資訊室人員。(" + ex.ToString() + ")";
                            return JsonConvert.SerializeObject(json_result);
                        }
                    }
                    #endregion
                    obj.titleStr = "生產紀錄單";
                    obj.marginTop = "33";
                    obj.titleUrl = GetSourceUrl() + "/BabyBorn/ENTR_HEADER?feeno=" + fee_no + "&Type=" + emr_source;
                    obj.footLeft = "(2019_Q4)恩主公醫院BAIA070-001-03";

                    Parameters = string.Format("?url={0}/Obstetrics/Production_Record_Print?fee_no={1}&DelFile=false&filename={2}"
                        , GetSourceUrl()
                        , fee_no
                        , filename
                        );
                    break;
                case "Child_Birth": //嬰兒出生紀錄單
                    sqlstr = "select IID from OBS_NB WHERE NB_CHARTNO='" + ChartNo + "'";
                    link.DBExecSQL(sqlstr, ref Dt);
                    if (Dt.Rows.Count > 0)
                    {
                        pk_id = Dt.Rows[0]["IID"].ToString();

                    }

                    obj.titleStr = "嬰兒出生紀錄單";
                    obj.marginTop = "23";
                    obj.titleUrl = GetSourceUrl() + "/BabyBorn/ENTR_HEADER?feeno=" + fee_no + "&Type=" + emr_source;
                    obj.footLeft = "(2019_Q2)恩主公醫院BAIA080-003-02";

                    Parameters = string.Format("?url={0}/Obstetrics/Child_Birth_Print?fee_no={1}&DelFile=false&filename={2}"
                        , GetSourceUrl()
                        , fee_no
                        , filename
                        );
                    break;
                case "Insert_NBENTR":
                    obj.titleStr = "新生兒入院護理評估表";
                    obj.marginTop = "35";
                    obj.footLeft = "(2019_Q2)恩主公醫院BAIA080-004-03";
                    obj.titleUrl = GetSourceUrl() + "/BabyBorn/ENTR_HEADER?feeno=" + fee_no + "&Type=" + emr_source;

                    Parameters = string.Format("?url={0}/BabyBorn/Insert_NBENTR_PDF?feeno={1}&mother_fee_no={2}&tableid={3}&DelFile=false&filename={4}"
                        , GetSourceUrl()
                        ,fee_no
                        ,mom_fee_no
                        ,pk_id
                        ,filename
                        );
                    break;
                case "Insert_BabyEntr":
                    obj.titleStr = "嬰幼兒入院護理評估表";
                    obj.marginTop = "33";
                    obj.footLeft = "(2019_Q2)恩主公醫院BAIA051-001-03";
                    obj.titleUrl = GetSourceUrl() + "/BabyBorn/ENTR_HEADER?feeno=" + fee_no + "&Type=" + emr_source;

                    Parameters = string.Format("?url={0}/BabyBorn/Insert_BabyEntr_PDF?feeno={1}&tableid={2}&DelFile=false&filename={3}"
                        , GetSourceUrl()
                        , fee_no
                        , pk_id
                        , filename
                        );
                    break;
                default:
                    break;
            }


            System.Globalization.TaiwanCalendar taiwanCalendar = new System.Globalization.TaiwanCalendar();
            string TaiwnYear = taiwanCalendar.GetYear(NowDtime).ToString();
            string GetMonth = NowDtime.ToString("MM");
            int Return_Msg = 0;

            try
            {
                //檢查資料夾是否存在
                if (Directory.Exists(@"\\172.20.110.157\ToEMR\MAYA-NIS\" + TaiwnYear + @"\" + GetMonth))
                {
                    //資料夾存在
                }
                else
                {
                    //新增資料夾
                    Directory.CreateDirectory(@"\\172.20.110.157\ToEMR\MAYA-NIS\" + TaiwnYear + @"\" + GetMonth);
                }
                string filePath = @"\\172.20.110.157\ToEMR\MAYA-NIS\" + TaiwnYear + @"\" + GetMonth + @"\";
                string tempfilePath = "C:\\EMR\\";

                string obj_toJson = JsonConvert.SerializeObject(obj);
                PrintC.GetPDF(true, filename, Parameters, obj_toJson);
                //因應下載功能先將MOVE改為COPY
                System.IO.File.Move(tempfilePath + filename + ".pdf", filePath + filename + ".pdf");
                //System.IO.File.Copy(tempfilePath + filename + ".pdf", filePath + filename + ".pdf");

                string EmrXmlString = this.getPDF_xml(
                    NowDtime, sour_code, pk_id, ptinfo.InDate.ToString("yyyyMMdd"), fee_no, NowDtime.ToString("yyyyMMdd"), userinfo.CostCenterCode, guider_info.CostCenterCode, guider_info.EmployeesNo,
                    guider_info.EmployeesName, guider_info.UserID, ChartNo, ptinfo.PatientName, ptinfo.PatientID, ptinfo.PayInfo,filePath, filename, listJsonArray, emr_source
                    );
                string ErrMsg = "";
                string[] VerifyStr = GetVerifyStr();
                PutJTEMRWsService_WS.EnvirMode ws_status = PutJTEMRWsService_WS.EnvirMode.TestEnvir;
                if (NIS.MvcApplication.iniObj.NisSetting.ServerMode.ToString() == "Production")
                {
                    ws_status = PutJTEMRWsService_WS.EnvirMode.ProductionEnvir;
                }
                Return_Msg = _EMRnis.UploadEMRFile(ws_status, VerifyStr, EmrXmlString, filePath + filename + ".pdf", ref ErrMsg);

                if (Return_Msg > 0)
                {
                    json_result.status = RESPONSE_STATUS.ERROR;
                    json_result.message = "簽章產生失敗(" + Return_Msg + ")，請連絡資訊室人員。";

                    //Response.Write("<script>alert('簽章產生失敗(" + Return_Msg + ")，請連絡資訊室人員。');</script>");
                }
                else
                {
                    json_result.status = RESPONSE_STATUS.SUCCESS;
                    json_result.message = filename;

                    //Response.Write("<script>alert('簽章產生成功。');</script>");
                }
            }
            catch (Exception ex)
            {
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                //Response.Write("<script>alert('簽章產生失敗，請連絡資訊室人員。');</script>");
                json_result.status = RESPONSE_STATUS.EXCEPTION;
                json_result.message = "簽章產生失敗，請連絡資訊室人員。("+ ex.ToString() + ")";

            }
            finally
            {
            }
            return JsonConvert.SerializeObject(json_result);

        }
        #endregion

    }
}
