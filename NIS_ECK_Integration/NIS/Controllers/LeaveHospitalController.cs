using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NIS.Data;
using NIS.Models;
using NIS.UtilTool;
using NIS.WebService;
using Newtonsoft.Json;

namespace NIS.Controllers
{
    public class LeaveHospitalController : BaseController
    {
        Assess ass_m = new Assess();

        public LeaveHospitalController()
        {
        }
        #region 團隊照護

        public ActionResult Team_Care_Index(string feeno)
        {
            if (feeno == null)
                ViewBag.dt = ass_m.sel_team_care_data(ptinfo.FeeNo);
            else
                ViewBag.dt = ass_m.sel_team_care_data(feeno);
            return View();
        }

        public ActionResult Team_Care(string feeno)
        {
            return View();
        }

        public ActionResult Insert_Team_Care(FormCollection form)
        {
            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;
            string NURSE = "", PHARMACIST = "", PHARMACIST_TEACH = "", DIETITIANS = "", DIETITIANS_TYPE = "", DIETITIANS_FOOD = "", THERAPIST = "", SOCIAL = "";
            if (form["rb_nurse"] != null)
                NURSE = form["rb_nurse"];
            if (form["rb_pharmacist"] != null)
                PHARMACIST = form["rb_pharmacist"];
            if (form["txt_rb_pharmacist_3"] != null)
                PHARMACIST_TEACH = form["txt_rb_pharmacist_3"];
            if (form["rb_dietitians"] != null)
                DIETITIANS = form["rb_dietitians"];
            if (form["rb_dietitians_type"] != null)
                DIETITIANS_TYPE = form["rb_dietitians_type"];
            if (form["rb_dietitians_food"] != null)
                DIETITIANS_FOOD = form["rb_dietitians_food"];
            if (form["rb_therapist"] != null)
                THERAPIST = form["rb_therapist"];
            if (form["rb_social"] != null)
                SOCIAL = form["rb_social"];

            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("TEAM_CARE_ID", creatid("TEAM_CARE", userno, feeno, "0"), DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("NURSE", NURSE, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("PHARMACIST", PHARMACIST, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("PHARMACIST_TEACH", PHARMACIST_TEACH, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("DIETITIANS", DIETITIANS, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("DIETITIANS_TYPE", DIETITIANS_TYPE, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("DIETITIANS_FOOD", DIETITIANS_FOOD, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("THERAPIST", THERAPIST, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("SOCIAL", SOCIAL, DBItem.DBDataType.String));

            int erow = ass_m.DBExecInsert("NIS_TEAM_CARE_DATA", insertDataList);
            if (erow > 0)
                Response.Write("<script>alert('新增成功!');window.location.href='Html_To_Pdf?feeno=" + ptinfo.FeeNo + "';</script>");
            else
                Response.Write("<script>alert('新增失敗!');window.location.href='Team_Care';</script>");

            return new EmptyResult();
        }

        public ActionResult Upd_Team_Care(FormCollection form)
        {
            string NURSE = "", PHARMACIST = "", PHARMACIST_TEACH = "", DIETITIANS = "", DIETITIANS_TYPE = "", DIETITIANS_FOOD = "", THERAPIST = "", SOCIAL = "";
            if (form["rb_nurse"] != null)
                NURSE = form["rb_nurse"];
            if (form["rb_pharmacist"] != null)
                PHARMACIST = form["rb_pharmacist"];
            if (form["txt_rb_pharmacist_3"] != null)
                PHARMACIST_TEACH = form["txt_rb_pharmacist_3"];
            if (form["rb_dietitians"] != null)
                DIETITIANS = form["rb_dietitians"];
            if (form["rb_dietitians_type"] != null)
                DIETITIANS_TYPE = form["rb_dietitians_type"];
            if (form["rb_dietitians_food"] != null)
                DIETITIANS_FOOD = form["rb_dietitians_food"];
            if (form["rb_therapist"] != null)
                THERAPIST = form["rb_therapist"];
            if (form["rb_social"] != null)
                SOCIAL = form["rb_social"];

            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("UPDNO", userinfo.EmployeesName, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("NURSE", NURSE, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("PHARMACIST", PHARMACIST, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("PHARMACIST_TEACH", PHARMACIST_TEACH, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("DIETITIANS", DIETITIANS, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("DIETITIANS_TYPE", DIETITIANS_TYPE, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("DIETITIANS_FOOD", DIETITIANS_FOOD, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("THERAPIST", THERAPIST, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("SOCIAL", SOCIAL, DBItem.DBDataType.String));
            string where = "TEAM_CARE_ID = '" + form["id"] + "' ";

            int erow = ass_m.DBExecUpdate("NIS_TEAM_CARE_DATA", insertDataList, where);
            if (erow > 0)
                Response.Write("<script>alert('儲存成功!');window.location.href='Html_To_Pdf?feeno=" + ptinfo.FeeNo + "';</script>");
            else
                Response.Write("<script>alert('儲存失敗!');window.location.href='Team_Care';</script>");

            return new EmptyResult();
        }

        //轉PDF頁面
        public ActionResult Html_To_Pdf()
        {
            string url = Request.Url.AbsoluteUri.Replace("Html_To_Pdf", "Team_Care_Index");
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
            Response.Write("<script>window.location.href='Download_Pdf?filename=" + filename + "';</script>");
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

        #endregion

        #region 出院轉介

        public ActionResult Leave_Referral()
        {//判斷有無病人session
            if (Session["PatInfo"] != null)
            {
                string feeno = ptinfo.FeeNo;
                TubeManager tubem = new TubeManager();
                CareRecord care_record_m = new CareRecord();
                Wound wound = new Wound();
                List<Lab> lab_list = new List<Lab>();
                byte[] labfoByteCode = webService.GetLab(feeno);
                if (labfoByteCode != null)
                {
                    string labJosnArr = CompressTool.DecompressString(labfoByteCode);
                    List<Lab> lab = JsonConvert.DeserializeObject<List<Lab>>(labJosnArr);
                    lab_list = lab;
                }
                List<PatientInfo> allergy_list = new List<PatientInfo>();
                byte[] allergyfoByteCode = webService.GetAllergyList(feeno);
                if (allergyfoByteCode != null)
                {
                    string allergyJosnArr = CompressTool.DecompressString(allergyfoByteCode);
                    List<PatientInfo> allergy = JsonConvert.DeserializeObject<List<PatientInfo>>(allergyJosnArr);
                    allergy_list = allergy;
                }
                //用藥資訊by yungchen 2014.03.14
                List<DrugOrder> UdOrderList = new List<DrugOrder>();
                //get_TransferDuty_Item_Consultation(fee_no, "Main", ref ConsultationList);
                byte[] UdOrderByteCode = webService.GetUdOrderD(feeno);
                if (UdOrderByteCode != null)
                {
                    string UdOrderJosnArr = CompressTool.DecompressString(UdOrderByteCode);
                    List<DrugOrder> UdOrder = JsonConvert.DeserializeObject<List<DrugOrder>>(UdOrderJosnArr);
                    UdOrderList = UdOrder;
                }
                ViewData["med"] = UdOrderList;

                ViewData["lab_list"] = lab_list;
                ViewData["Allergy"] = allergy_list;
                ViewBag.dt = ass_m.sel_leave_referral_data(feeno);
                ViewBag.dt_tube = tubem.sel_tube(feeno, "", "", "", "");

                //皮膚完整性(傷口) by yungchen 2014.06.24
                ViewBag.dt_wound = wound.sel_group_wound_record("", feeno);
                //ViewBag.dt_tube = tubem.sel_tube(feeno, "", "", "", "Y");
                ViewBag.feeno = feeno;
            }
            else
            {
                Response.Write("<script>alert('請重新選擇病患');</script>");
                return new EmptyResult();
            }
            return View();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Insert_leave(FormCollection form)
        {
            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;

            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("LEAVE_ID", creatid("LEAVE", userno, feeno, "0"), DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("CONSCIOUSNESS", form["consciousness_status"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("OXYGEN_STATUS", form["oxygen_status"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("SENCE_ACTION", form["sence_action"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("ACTIVITY", form["activity"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FOOD", form["food"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("URINATION", form["urination"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("DEFECATION", form["defecation"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("SPECIAL_ITEM", form["special_item"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("ISOLATION", form["isolation"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("NURSE_CARE", form["nurse_care"], DBItem.DBDataType.String));

            int erow = ass_m.DBExecInsert("NIS_LEAVE_REFERRAL_DATA", insertDataList);
            if (erow > 0)
                Response.Write("<script>alert('新增成功!');window.location.href='Leave_Referral';</script>");
            else
                Response.Write("<script>alert('新增失敗!');window.location.href='Leave_Referral';</script>");
            return new EmptyResult();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Upd_leave(FormCollection form)
        {
            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;

            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("CONSCIOUSNESS", form["consciousness_status"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("OXYGEN_STATUS", form["oxygen_status"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("SENCE_ACTION", form["sence_action"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("ACTIVITY", form["activity"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FOOD", form["food"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("URINATION", form["urination"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("DEFECATION", form["defecation"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("SPECIAL_ITEM", form["special_item"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("ISOLATION", form["isolation"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("NURSE_CARE", form["nurse_care"], DBItem.DBDataType.String));
            string where = "LEAVE_ID = '" + form["id"] + "' ";

            int erow = ass_m.DBExecUpdate("NIS_LEAVE_REFERRAL_DATA", insertDataList, where);
            if (erow > 0)
                Response.Write("<script>alert('更新成功!');window.location.href='Leave_Referral';</script>");
            else
                Response.Write("<script>alert('更新失敗!');window.location.href='Leave_Referral';</script>");
            return new EmptyResult();
        }

        #endregion

        //出院轉介護理摘要
        public ActionResult Leave_Referral_PDF(string feeno)
        {
            TubeManager tubem = new TubeManager();
            CareRecord care_record_m = new CareRecord();
            Wound wound = new Wound();
            List<Lab> lab_list = new List<Lab>();
            byte[] labfoByteCode = webService.GetLab(feeno);
            if (labfoByteCode != null)
            {
                string labJosnArr = CompressTool.DecompressString(labfoByteCode);
                List<Lab> lab = JsonConvert.DeserializeObject<List<Lab>>(labJosnArr);
                lab_list = lab;
            }
            List<PatientInfo> allergy_list = new List<PatientInfo>();
            byte[] allergyfoByteCode = webService.GetAllergyList(feeno);
            if (allergyfoByteCode != null)
            {
                string allergyJosnArr = CompressTool.DecompressString(allergyfoByteCode);
                List<PatientInfo> allergy = JsonConvert.DeserializeObject<List<PatientInfo>>(allergyJosnArr);
                allergy_list = allergy;
            }
            //用藥資訊by yungchen 2014.03.14
            List<DrugOrder> UdOrderList = new List<DrugOrder>();
            //get_TransferDuty_Item_Consultation(fee_no, "Main", ref ConsultationList);
            byte[] UdOrderByteCode = webService.GetUdOrderD(feeno);
            if (UdOrderByteCode != null)
            {
                string UdOrderJosnArr = CompressTool.DecompressString(UdOrderByteCode);
                List<DrugOrder> UdOrder = JsonConvert.DeserializeObject<List<DrugOrder>>(UdOrderJosnArr);
                UdOrderList = UdOrder;
            }
            ViewData["med"] = UdOrderList;

            ViewData["lab_list"] = lab_list;
            ViewData["Allergy"] = allergy_list;
            ViewBag.dt = ass_m.sel_leave_referral_data(feeno);
            ViewBag.dt_tube = tubem.sel_tube(feeno, "", "", "", "");
            //皮膚完整性(傷口) by yungchen 2014.06.24
            ViewBag.dt_wound = wound.sel_group_wound_record("", feeno);
            PatientInfo pinfo = new PatientInfo();
            byte[] ByteCode = webService.GetPatientInfo(feeno);
            //病人資訊
            if (ByteCode != null)
                pinfo = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(ByteCode));
            ViewData["ptinfo"] = pinfo;

            return View();
        }

        //轉PDF頁面
        public ActionResult Html_To_Pdf_New(string url)
        {
            string strPath = @"C:\\wkhtmltopdf\\wkhtmltopdf.exe";
            string filename = DateTime.Now.ToString("yyyyMMddHHmmssfff") + url.Substring(url.IndexOf("feeno=") + 6, url.Length - url.IndexOf("feeno=") - 6) + ".pdf";
            string tempPath = "C:\\inetpub\\NIS\\Images\\" + filename;
            //string tempPath = "D:\\Dropbox\\NIS\\NIS\\NIS\\Images\\" + filename;
            string pdfpath = "../Images/" + filename;
            Process p = new Process();
            p.StartInfo.FileName = strPath;
            p.StartInfo.Arguments = url + " " + tempPath;
            p.StartInfo.UseShellExecute = true;
            p.Start();
            p.WaitForExit();
            Response.Write("<script>window.open('Download_Pdf_New?filename=" + filename + "');window.location.href='Leave_Referral';</script>");

            return new EmptyResult();
        }

        public ActionResult Download_Pdf_New(string filename)
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

    }
}
