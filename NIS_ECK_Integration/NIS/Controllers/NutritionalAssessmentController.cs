using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using NIS.Data;
using NIS.Models;
using NIS.UtilTool;
using NIS.WebService;
using Newtonsoft.Json;

namespace NIS.Controllers
{
    public class NutritionalAssessmentController : BaseController
    {
        private NutritionalAssessment NutritionalAssessment;

        public NutritionalAssessmentController()
        {
            this.NutritionalAssessment = new NutritionalAssessment();
        }

        //營養評估
        public ActionResult Index(string modeval = "")
        {//判斷有無病人session
            if (Session["PatInfo"] != null)
            {
                if (modeval == "")  //從 menu 選擇，自動切換
            {
                int ptAge = ptinfo.Age;

                if(ptAge < 18)
                {
                    //兒童                    
                    Response.Write("<script>window.location.href='AddNutAssessList?type=child';</script>");
                }
                else
                {
                    //成人                    
                    Response.Write("<script>window.location.href='AddNutAssessList?type=adult';</script>");
                }
                return new EmptyResult();
            }
        }
            else
            {
                Response.Write("<script>alert('請重新選擇病患');</script>");
                return new EmptyResult();
    }
            return View();
        }

        public ActionResult AddNutAssessList(string type, string St = "", string Ed = "")
        {
            DataTable dt_list = new DataTable();
            string feeno = ptinfo.FeeNo;
            ViewBag.bodyheight = this.NutritionalAssessment.ReturnVitalSignData(feeno, "bh");
            ViewBag.bodyweight = this.NutritionalAssessment.ReturnVitalSignData(feeno, "bw");
            dt_list = NutritionalAssessment.sel_nutritional_data(feeno, "", type, St, Ed);

            //非登入者之員工資料 經由WS取得
            dt_list.Columns.Add("username");
            if(dt_list.Rows.Count > 0)
            {
                string userno = userinfo.EmployeesNo;

                foreach(DataRow r in dt_list.Rows)
                {
                    userno = userinfo.EmployeesNo;  //登入者

                    if(userno != r["CREANO"].ToString())
                    {//登入者和紀錄者資料不相同時
                        userno = r["CREANO"].ToString();
                        byte[] listByteCode = webService.UserName(userno);
                        if(listByteCode != null)
                        {
                            string listJsonArray = CompressTool.DecompressString(listByteCode);
                            UserInfo other_user_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                            r["username"] = other_user_info.EmployeesName;
                        }
                    }
                    else
                        r["username"] = userinfo.EmployeesName;
                }
            }

            //身體質量指數（BMI）建議值 pdf連結判斷檔名
            int ptAge = ptinfo.Age;
            string PDFName = "";
            if(ptAge > 4 && ptAge < 18)
            {//5歲以上之兒童
                PDFName = "Young";
            }
            else
            {
                if(ptinfo.PatientGender == "男")
                    PDFName = "Boy1";
                else
                    PDFName = "Girl2";
            }

            ViewBag.NutType = type;
            ViewBag.dt_list = dt_list;
            ViewBag.St = St;
            ViewBag.Ed = Ed;
            ViewBag.feeno = feeno;
            ViewBag.PDFName = PDFName;
            ViewBag.RootDocument = GetSourceUrl();
            ViewBag.userno = userinfo.EmployeesNo;

            return View();
        }

        [HttpPost]
        public string SelectEditNutAssessInfo(string NUTAID, string type)
        {//抓取單筆資訊
            string feeno = ptinfo.FeeNo;
            DataTable DT = NutritionalAssessment.sel_nutritional_data(feeno, NUTAID, type);

            return NutritionalAssessment.DatatableToJsonArray(DT);
        }

        //新增、修改 營養評估
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult SetNutAssessmentData(FormCollection form)
        {
            if(Session["PatInfo"] != null)
            {
                string feeno = ptinfo.FeeNo;
                string userno = userinfo.EmployeesNo;
                string ID = "";
                int ERow = 0;
                string NutaAssessmentDTM = form["TxtNutaAssessmentDTMDate"] + " " + form["TxtNutaAssessmentDTMTime"];
                string NutaType = form["HidNUTAType"];
                string NutaHead, NutaAssessmentRow1, NutaAssessmentRow2, NutaAssessmentRow3, NutaAssessmentRow4 = string.Empty;

                if(NutaType == "adult")
                {
                    NutaHead = "";
                    NutaAssessmentRow1 = form["RbnAdultRow1"];
                    NutaAssessmentRow2 = form["RbnAdultRow2"];
                    NutaAssessmentRow3 = form["RbnAdultRow3"];
                    NutaAssessmentRow4 = form["TxtAdultRow4"];
                }
                else
                {
                    NutaHead = form["TxtNutaHead"];
                    NutaAssessmentRow1 = form["TxtChildRow1"];
                    NutaAssessmentRow2 = form["TxtChildRow2"];
                    NutaAssessmentRow3 = form["TxtChildRow3"];
                    //NutaAssessmentRow4 = "";// form["TxtChildRow4"];
                }

                List<DBItem> DBItemDataList = new List<DBItem>();

                if(form["HidNUTAID"] == "")
                {
                    //新增
                    ID = base.creatid("NUTRITIONAL_ASSESSMENT", userno, feeno, "0");

                    DBItemDataList.Add(new DBItem("NUTA_ID", ID, DBItem.DBDataType.String));
                    DBItemDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                    DBItemDataList.Add(new DBItem("NUTA_ASSESSMENT_DTM", NutaAssessmentDTM, DBItem.DBDataType.DataTime));
                    DBItemDataList.Add(new DBItem("NUTA_HEIGHT", form["TxtNutaHeight"], DBItem.DBDataType.String));
                    DBItemDataList.Add(new DBItem("NUTA_WEIGHT", form["TxtNutaWeight"], DBItem.DBDataType.String));
                    DBItemDataList.Add(new DBItem("NUTA_HEAD", NutaHead, DBItem.DBDataType.String));  //  兒童才有
                    DBItemDataList.Add(new DBItem("NUTA_BMI", form["TxtNutaBMI"], DBItem.DBDataType.String));
                    DBItemDataList.Add(new DBItem("NUTA_ASSESSMENT_ROW1", NutaAssessmentRow1, DBItem.DBDataType.String));
                    DBItemDataList.Add(new DBItem("NUTA_ASSESSMENT_ROW2", NutaAssessmentRow2, DBItem.DBDataType.String));
                    DBItemDataList.Add(new DBItem("NUTA_ASSESSMENT_ROW3", NutaAssessmentRow3, DBItem.DBDataType.String));
                    if(NutaType == "adult")
                    {
                        DBItemDataList.Add(new DBItem("NUTA_ASSESSMENT_ROW4", NutaAssessmentRow4, DBItem.DBDataType.String));
                    }
                    DBItemDataList.Add(new DBItem("NUTA_ASSESSMENT_RESULT", "", DBItem.DBDataType.String));
                    DBItemDataList.Add(new DBItem("NUTA_CARE_RECORD", (!string.IsNullOrWhiteSpace(form["ChkNutaCareRecord"])) ? form["ChkNutaCareRecord"] : "", DBItem.DBDataType.String));
                    //DBItemDataList.Add(new DBItem("NUTA_WEIGHT_UNIT", form["DdlNutaWeightUnit"], DBItem.DBDataType.String));  //拿掉，詳見頁面程式
                    DBItemDataList.Add(new DBItem("NUTA_TYPE", NutaType, DBItem.DBDataType.String));
                    DBItemDataList.Add(new DBItem("DELETED", "N", DBItem.DBDataType.String));
                    DBItemDataList.Add(new DBItem("CREANO", userno, DBItem.DBDataType.String));
                    DBItemDataList.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));

                    ERow = NutritionalAssessment.DBExecInsert("NUTRITIONAL_ASSESSMENT", DBItemDataList);

                    if(ERow > 0)
                    {
                        #region  護理紀錄
                        //if (form["RbnNutaCareRecord"] == "Y")
                        if(form["ChkNutaCareRecord"] == "Y")
                        {
                            string CareRecordCont = "", title = "";
                            switch(NutaType)
                            {
                                case "adult":
                                    title = "營養評估-成人";
                                    CareRecordCont = "病人" + form["TxtNutaHeight"] + "cm，" + form["TxtNutaWeight"] + "kg，BMI：" + form["TxtNutaBMI"];// "，MUST營養評估" + NutaAssessmentRow4 + "分。";
                                    if(int.Parse(NutaAssessmentRow4) < 3)
                                    {
                                        CareRecordCont += "，MUST營養評估" + NutaAssessmentRow4 + "分，暫時不需要進一步的營養評估。";
                                    }
                                    else
                                    {
                                        CareRecordCont += "，MUST營養評估" + NutaAssessmentRow4 + "分，為營養不良高度風險，通知主治醫師決定是否照會營養師做進一步的營養評估。";
                                    }
                                    break;
                                case "child":
                                    title = "營養評估-兒童";
                                    CareRecordCont = "病童" + form["TxtNutaHeight"] + "cm，";
                                    if (NutaAssessmentRow1 != "")
                                    CareRecordCont += "身高百分比區間" + NutaAssessmentRow1 + "%，";
                                    CareRecordCont +=  form["TxtNutaWeight"] + "kg，";
                                    if (NutaAssessmentRow2 != "")
                                    CareRecordCont +="體重百分比區間" + NutaAssessmentRow2 + "%，";
                                    if (form["TxtNutaBMI"] != "")
                                    CareRecordCont +=  "BMI：" + form["TxtNutaBMI"];
                                    //CareRecordCont += "BMI百分比區間BMI" + NutaAssessmentRow4 + "%";
                                    if (NutaHead != "")
                                        CareRecordCont += "，頭圍" + NutaHead + "cm，";
                                    if (NutaAssessmentRow3 != "")
                                    CareRecordCont +=   "頭圍百分比區間" + NutaAssessmentRow3 + "%。";
                                    break;
                            }
                            int p_erow = base.Insert_CareRecord(DateTime.Now.ToString("yyyy/MM/dd HH:mm"), ID, title, CareRecordCont, "", "", "", "", "NUTRITIONAL_ASSESSMENT");
                        }
                        #endregion  護理紀錄 end
                    }
                }
                else
                {
                    //修改
                    ID = form["HidNUTAID"];
                    DBItemDataList.Add(new DBItem("NUTA_ASSESSMENT_DTM", NutaAssessmentDTM, DBItem.DBDataType.DataTime));
                    DBItemDataList.Add(new DBItem("NUTA_HEIGHT", form["TxtNutaHeight"], DBItem.DBDataType.String));
                    DBItemDataList.Add(new DBItem("NUTA_WEIGHT", form["TxtNutaWeight"], DBItem.DBDataType.String));
                    DBItemDataList.Add(new DBItem("NUTA_HEAD", NutaHead, DBItem.DBDataType.String));  //  兒童才有
                    DBItemDataList.Add(new DBItem("NUTA_BMI", form["TxtNutaBMI"], DBItem.DBDataType.String));
                    DBItemDataList.Add(new DBItem("NUTA_ASSESSMENT_ROW1", NutaAssessmentRow1, DBItem.DBDataType.String));
                    DBItemDataList.Add(new DBItem("NUTA_ASSESSMENT_ROW2", NutaAssessmentRow2, DBItem.DBDataType.String));
                    DBItemDataList.Add(new DBItem("NUTA_ASSESSMENT_ROW3", NutaAssessmentRow3, DBItem.DBDataType.String));
                    if(NutaType == "adult")
                    {
                        DBItemDataList.Add(new DBItem("NUTA_ASSESSMENT_ROW4", NutaAssessmentRow4, DBItem.DBDataType.String));
                    }
                    DBItemDataList.Add(new DBItem("NUTA_ASSESSMENT_RESULT", "", DBItem.DBDataType.String));
                    DBItemDataList.Add(new DBItem("NUTA_CARE_RECORD", (!string.IsNullOrWhiteSpace(form["ChkNutaCareRecord"])) ? form["ChkNutaCareRecord"] : "", DBItem.DBDataType.String));
                    DBItemDataList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
                    DBItemDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));

                    ERow = NutritionalAssessment.DBExecUpdate("NUTRITIONAL_ASSESSMENT", DBItemDataList, string.Format("FEENO={0} AND NUTA_ID={1}", DigitFlapTemperature.SQLString(feeno), DigitFlapTemperature.SQLString(ID)));
                    if(ERow > 0)
                    {
                        #region  護理紀錄
                        //if (form["RbnNutaCareRecord"] == "Y")
                        if(form["ChkNutaCareRecord"] == "Y")
                        {
                            string CareRecordCont = "", title = "";
                            switch(NutaType)
                            {
                                case "adult":
                                    title = "營養評估-成人";
                                    CareRecordCont = "病人" + form["TxtNutaHeight"] + "cm，" + form["TxtNutaWeight"] + "kg，BMI：" + form["TxtNutaBMI"];// "，MUST營養評估" + NutaAssessmentRow4 + "。";
                                    if(int.Parse(NutaAssessmentRow4) < 3)
                                    {
                                        CareRecordCont += "，MUST營養評估" + NutaAssessmentRow4 + "分，暫時不需要進一步的營養評估。";
                                    }
                                    else
                                    {
                                        CareRecordCont += "，MUST營養評估" + NutaAssessmentRow4 + "分，為營養不良高度風險，通知主治醫師決定是否照會營養師做進一步的營養評估。";
                                    }
                                    break;
                                case "child":
                                    title = "營養評估-兒童";
                                    CareRecordCont = "病童" + form["TxtNutaHeight"] + "cm，";
                                    if (NutaAssessmentRow1 != "")
                                    CareRecordCont += "身高百分比區間" + NutaAssessmentRow1 + "%，";
                                    CareRecordCont +=  form["TxtNutaWeight"] + "kg，";
                                    if (NutaAssessmentRow2 != "")
                                    CareRecordCont +="體重百分比區間" + NutaAssessmentRow2 + "%，";
                                    if (form["TxtNutaBMI"] != "")
                                    CareRecordCont +=  "BMI：" + form["TxtNutaBMI"];
                                    //CareRecordCont += "BMI百分比區間BMI" + NutaAssessmentRow4 + "%";
                                    if (NutaHead != "")
                                        CareRecordCont += "，頭圍" + NutaHead + "cm，";
                                    if (NutaAssessmentRow3 != "")
                                    CareRecordCont +=   "頭圍百分比區間" + NutaAssessmentRow3 + "%。";
                                    break;
                            }
                            int p_erow = base.Upd_CareRecord(DateTime.Now.ToString("yyyy/MM/dd HH:mm"), ID, title, CareRecordCont, "", "", "", "", "NUTRITIONAL_ASSESSMENT");
                        }
                        else
                        {
                            Del_CareRecord(ID, "NUTRITIONAL_ASSESSMENT");
                        }
                        #endregion  護理紀錄 end
                    }
                }
                if(ERow > 0)
                    Response.Write("Y");
                else
                    Response.Write("N");
                return new EmptyResult();
            }

            Response.Write("O");
            return new EmptyResult();
        }

        //刪除評估
        [HttpPost]
        public string DelNutAssmentInfo(string NUTAID)
        {
            if(Session["PatInfo"] != null)
            {
                string feeno = ptinfo.FeeNo;
                string userno = userinfo.EmployeesNo;
                int ERow = 0;

                List<DBItem> DBItemDataList = new List<DBItem>();

                DBItemDataList.Clear();
                DBItemDataList.Add(new DBItem("DELETED", "Y", DBItem.DBDataType.String));
                DBItemDataList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
                DBItemDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));

                ERow = NutritionalAssessment.DBExecUpdate("NUTRITIONAL_ASSESSMENT", DBItemDataList, string.Format("FEENO={0} AND NUTA_ID={1}", DigitFlapTemperature.SQLString(feeno), DigitFlapTemperature.SQLString(NUTAID)));
                if(ERow > 0)
                {
                    ERow += base.Del_CareRecord(NUTAID, "NUTRITIONAL_ASSESSMENT");
                }

                if(ERow > 0)
                    return "Y";
                else
                    return "N";
            }

            return "O";
        }

        #region  列印

        public ActionResult MUST_Print_List(string LoginEmpNo, string feeno, string type, string St, string Ed)
        {
            string userno = "";  //這裡無法直接使用 userinfo ，會造成無法抓取的情況，故用參數值

            //病人資訊
            PatientInfo pinfo = new PatientInfo();
            byte[] ByteCode = webService.GetPatientInfo(feeno);
            if(ByteCode != null)
                pinfo = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(ByteCode));
            ViewData["ptinfo"] = pinfo;

            DataTable DT = NutritionalAssessment.sel_nutritional_data(feeno, "", type, St, Ed);

            //非登入者之員工資料 經由WS取得
            DT.Columns.Add("username");
            if(DT.Rows.Count > 0)
            {
                foreach(DataRow r in DT.Rows)
                {
                    userno = LoginEmpNo;  //登入者
                    if(userno != r["CREANO"].ToString())
                    {//登入者和紀錄者資料不相同時，訂正 userno
                        userno = r["CREANO"].ToString();
                    }

                    //因為 userinfo 無法使用，所以不論相不相同，都要重抓名字
                    byte[] listByteCode = webService.UserName(userno);
                    if(listByteCode != null)
                    {
                        string listJsonArray = CompressTool.DecompressString(listByteCode);
                        UserInfo other_user_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                        r["username"] = other_user_info.EmployeesName;
                    }
                }
            }
            ViewBag.dt = DT;
            ViewBag.NutType = type;
            return View();
        }

        #endregion  列印 end
    }
}
