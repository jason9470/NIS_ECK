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
    public class DigitFlapTemperatureController : BaseController
    {
        private DigitFlapTemperature DigitFlapTemperature;

        public DigitFlapTemperatureController()
        {
            this.DigitFlapTemperature = new DigitFlapTemperature();
        }

        #region  部位

        public ActionResult listTemperature(string St = "", string Ed = "")
        {
            if(Session["PatInfo"] != null)
            {
                string feeno = ptinfo.FeeNo;
                string userno = userinfo.EmployeesNo;
                ViewBag.dt_data = DigitFlapTemperature.sel_temperature_data(feeno, "", "");  //部位
                ViewBag.feeno = feeno;
                ViewBag.RootDocument = GetSourceUrl();
                ViewBag.userno = userinfo.EmployeesNo;

                if(St != "" && Ed != "")
                {
                    ViewBag.dt_data = DigitFlapTemperature.sel_temperature_data_row(feeno, St, Ed);  //部位 for 查詢
                }
                ViewBag.St = St;
                ViewBag.Ed = Ed;

                return View();
            }

            Response.Write("<script>alert('登入逾時');</script>");
            return new EmptyResult();
        }

        [HttpGet]
        public ActionResult AddTemperaturePosition(string DFTempID, string ModeVal = "")
        {
            if(Session["PatInfo"] != null)
            {
                string feeno = ptinfo.FeeNo;
                ViewBag.dt_temperature_info = DigitFlapTemperature.sel_temperature_data(feeno, DFTempID, "");

                //抓取 已建立對照肢 
                DataTable DT = DigitFlapTemperature.sel_ctrl_limb_list(feeno);
                ViewBag.CtrlLimbList = SetDropDownList.GetddlItem("", DT, false, 1, 2);
                ViewBag.ModeVal = ModeVal;

                return View();
            }

            Response.Write("<script>alert('請重新選擇病患');window.close();</script>");
            return new EmptyResult();
        }

        //新增、修改 部位
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult SetTempPositionData(FormCollection form)
        {
            if(Session["PatInfo"] != null)
            {
                string feeno = ptinfo.FeeNo;
                string userno = userinfo.EmployeesNo;
                string ID = "";
                int ERow = 0;
                string DFTEMPCREATEDTM = form["TxtDFTempCreateDate"] + " " + form["TxtDFTempCreateTime"];

                List<DBItem> DBItemDataList = new List<DBItem>();

                if(form["HidDFTEMPID"] == "")
                {
                    //新增
                    ID = base.creatid("DIGI_FLAP_DATA", userno, feeno, "0");
                    //抓取 Num
                    DataTable DT = DigitFlapTemperature.sel_temperature_data_row(feeno, "", "");
                    int DFTempNum = DT.Rows.Count + 1;

                    DBItemDataList.Add(new DBItem("DFTEMP_ROW", "DIGI_FLAP_DATA_SEQUENCE.NEXTVAL", DBItem.DBDataType.Number));
                    DBItemDataList.Add(new DBItem("DFTEMP_ID", ID, DBItem.DBDataType.String));
                    DBItemDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                    DBItemDataList.Add(new DBItem("DFTEMP_NUM", DFTempNum.ToString(), DBItem.DBDataType.String));
                    DBItemDataList.Add(new DBItem("DFTEMP_TYPE", form["DDLDFTempType"], DBItem.DBDataType.String));
                    DBItemDataList.Add(new DBItem("DFTEMP_POSITION", form["DDLDFTempPosition"], DBItem.DBDataType.String));
                    DBItemDataList.Add(new DBItem("DFTEMP_POSITION_CONT", form["TxtDFTempPosition"], DBItem.DBDataType.String));
                    if(form["DDLDFTempType"] == "2") // if (form["DDLDFTempPosition"] == "2")
                    {
                        DBItemDataList.Add(new DBItem("DFTEMP_CTRL_LIMB", form["HidCtrlLimbText"], DBItem.DBDataType.String));
                        DBItemDataList.Add(new DBItem("DFTEMP_CTRL_LIMB_ID", form["DDLDFTempCtrlLimb"], DBItem.DBDataType.String));
                    }
                    DBItemDataList.Add(new DBItem("DFTEMP_CREATE_DTM", DFTEMPCREATEDTM, DBItem.DBDataType.DataTime));
                    DBItemDataList.Add(new DBItem("DELETED", "N", DBItem.DBDataType.String));
                    DBItemDataList.Add(new DBItem("CREANO", userno, DBItem.DBDataType.String));
                    DBItemDataList.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));

                    ERow = DigitFlapTemperature.DBExecInsert("DIGI_FLAP_DATA", DBItemDataList);
                }
                else
                {
                    //修改
                    ID = form["HidDFTEMPID"];

                    //DBItemDataList.Add(new DBItem("DFTEMP_CTRL_LIMB", form["DDLDFTempCtrlLimb"], DBItem.DBDataType.String));  //咏蓁說先不要讓客戶改對照肢
                    DBItemDataList.Add(new DBItem("DFTEMP_CREATE_DTM", DFTEMPCREATEDTM, DBItem.DBDataType.DataTime));
                    DBItemDataList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
                    DBItemDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));

                    ERow = DigitFlapTemperature.DBExecUpdate("DIGI_FLAP_DATA", DBItemDataList, string.Format("FEENO={0} AND DFTEMP_ID={1}", DigitFlapTemperature.SQLString(feeno), DigitFlapTemperature.SQLString(ID)));
                }


                if(ERow > 0)
                {
                    #region  護理紀錄
                    //咏蓁說部位的護理紀錄不需要
                    //string content = (DFTEMPCREATEDTM.Trim() != "" ? DFTEMPCREATEDTM : "不詳") + "，於" + DFTempPosition ;
                    //erow = base.Insert_CareRecord(DateTime.Now.ToString("yyyy/MM/dd HH:mm"), id, "", content, "", "", "", "", "Wound");
                    #endregion  護理紀錄 end
                    Response.Write("Y|" + ID);
                }
                else
                    Response.Write("N|");
                return new EmptyResult();
            }

            Response.Write("O|");
            return new EmptyResult();
        }

        //刪除部位
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult DelPositionInfo(FormCollection form)
        {
            if(Session["PatInfo"] != null)
            {
                string feeno = ptinfo.FeeNo;
                string userno = userinfo.EmployeesNo;
                string ID = form["HidDelDFTEMPID"];
                int ERow = 0;

                List<DBItem> DBItemDataList = new List<DBItem>();
                DBItemDataList.Add(new DBItem("DELETED", "Y", DBItem.DBDataType.String));
                DBItemDataList.Add(new DBItem("DEL_REASON", form["TxtDelPositionReason"], DBItem.DBDataType.String));
                DBItemDataList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
                DBItemDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));

                ERow = DigitFlapTemperature.DBExecUpdate("DIGI_FLAP_DATA", DBItemDataList, string.Format("FEENO={0} AND DFTEMP_ID={1}", DigitFlapTemperature.SQLString(feeno), DigitFlapTemperature.SQLString(ID)));

                if(ERow > 0)
                {
                    //ERow = base.Del_CareRecord(ID, "DigitFlapTemperature");  //因為目前部位新增沒有寫入護理紀錄，所以不需要刪除
                    base.Del_CareRecord("", ID);
                    Response.Write("Y|" + ID);
                }
                else
                    Response.Write("N|");

                return new EmptyResult();
            }

            Response.Write("O|");
            return new EmptyResult();
        }

        [HttpGet]
        public ActionResult SetTemperatureSatusPrompt(string row, string ModeVal)
        {
            if(Session["PatInfo"] != null)
            {
                string feeno = ptinfo.FeeNo;
                string userno = userinfo.EmployeesNo;

                DataTable DT = new DataTable();
                string[] ID = row.Split(',');
                for(int i = 0; i <= ID.Length - 1; i++)
                    DT = DigitFlapTemperature.sel_temperature_record_byLastInfo(ModeVal, DT, ID[i]);

                ViewBag.DT = DT;

                return View();
            }

            Response.Write("<script>alert('請重新選擇病患');window.close();</script>");
            return new EmptyResult();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult SaveTemperatureSatusPrompt(FormCollection form)
        {
            //結束
            if(Session["PatInfo"] != null)
            {
                string feeno = ptinfo.FeeNo;
                string userno = userinfo.EmployeesNo;
                string[] ID = (form["HidID[]"] != null) ? form["HidID[]"].Split(',') : null;
                string[] ENDTREASON = (form["TxtENDTREASON[]"] != null) ? form["TxtENDTREASON[]"].Split(',') : null;
                int ERow = 0, CheckRow = 0;

                DataTable dt = new DataTable();
                List<DBItem> DBItemDataList = new List<DBItem>();

                if(ID != null)
                {
                    for(int i = 0; i < ID.Length; i++)
                    {
                        DataTable DT_CR = DigitFlapTemperature.sel_temperature_data(feeno, ID[i], "");  //護理紀錄字串用，放在這裡是因為 sel_temperature_data 結束後就查不到了
                        string ENDTIME = form["TxtEndPositionDate[]"].Split(',').GetValue(i) + " " + form["TxtEndPositionTime[]"].Split(',').GetValue(i);

                        DBItemDataList.Clear();
                        DBItemDataList.Add(new DBItem("ENDTIME", ENDTIME, DBItem.DBDataType.DataTime));
                        DBItemDataList.Add(new DBItem("ENDT_REASON", ENDTREASON[i], DBItem.DBDataType.String));
                        DBItemDataList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
                        DBItemDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));

                        CheckRow = DigitFlapTemperature.DBExecUpdate("DIGI_FLAP_DATA", DBItemDataList, string.Format("FEENO={0} AND DFTEMP_ID={1}", DigitFlapTemperature.SQLString(feeno), DigitFlapTemperature.SQLString(ID[i])));

                        #region  護理紀錄(照傷口內容紀錄)
                        if(CheckRow > 0)
                        {
                            ERow += CheckRow;
                            if(ENDTIME != "")
                                ENDTIME = Convert.ToDateTime(ENDTIME).ToString("yyyy/MM/dd");
                            else
                                ENDTIME = "不詳";

                            string CareRecordCont = "於 " + ENDTIME + " 所發生的 " + DT_CR.Rows[0]["DFTEMP_TYPE"] + " 類型，" + DT_CR.Rows[0]["DFTEMP_POSITION"] + " 部位，因 " + ENDTREASON[i] + " 予以結案";

                            base.Insert_CareRecord(DateTime.Now.ToString("yyyy/MM/dd HH:mm"), ID[i], "皮瓣溫度", CareRecordCont, "", "", "", "", "DIGI_FLAP_DATA");
                        }
                        #endregion  護理紀錄 end
                    }
                }

                if(ERow > 0)
                {
                    Response.Write("Y");
                }
                else
                    Response.Write("N");

                return new EmptyResult();
            }

            Response.Write("O");
            return new EmptyResult();
        }

        #endregion  部位 end

        #region  紀錄

        public ActionResult Partial_Record(string DFTempID = "", string St = "", string Ed = "")
        {
            if(Session["PatInfo"] != null)
            {
                string feeno = ptinfo.FeeNo;

                DataTable dt = new DataTable();

                //if (DFTempID == "")  首次載入，取各部位最後一筆評估；點選部位時，依 DFTempID 查詢
                dt = DigitFlapTemperature.sel_temperature_record(feeno, DFTempID, St, Ed);


                //非登入者之員工資料 經由WS取得
                dt.Columns.Add("username");
                if(dt.Rows.Count > 0)
                {
                    string userno = userinfo.EmployeesNo;

                    foreach(DataRow r in dt.Rows)
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


                ViewBag.dt_record = dt;

                return PartialView();
            }

            Response.Write("<script>alert('登入逾時');</script>");

            return new EmptyResult();
        }

        public ActionResult AddTemperatureAssess(string row, string ModeVal)
        {
            if(Session["PatInfo"] != null)
            {
                string feeno = ptinfo.FeeNo;
                string userno = userinfo.EmployeesNo;

                //顏色
                DataTable DT_Color = DigitFlapTemperature.sel_category_list("temp_color", "digitflaptemperature");
                ViewBag.DDLColorList = SetDropDownList.GetddlItem("", DT_Color, false, 0, 1);

                //部位 + 評估
                DataTable DT = new DataTable();
                string[] ID = row.Split(',');
                for(int i = 0; i <= ID.Length - 1; i++)
                    DT = DigitFlapTemperature.sel_temperature_record_byLastInfo(ModeVal, DT, ID[i]);

                ViewBag.DT = DT;
                ViewBag.ModeVal = ModeVal;

                return View();
            }

            Response.Write("<script>alert('請重新選擇病患');window.close();</script>");
            return new EmptyResult();
        }

        //新增、修改 評估
        [HttpPost]
        public string SetTempAssessData()
        {
            if(Session["PatInfo"] != null)
            {
                string feeno = ptinfo.FeeNo;
                string userno = userinfo.EmployeesNo;
                string[] ID = Request["DFTempID[]"].Split(',');
                int ERow = 0;
                string DFRECORDID = "";
                string[] DFTempTemperatureRoom = Request["TxtDFTempTemperatureRoom[]"].Split(',');
                string[] DFTempTemperature = Request["TxtDFTempTemperature[]"].Split(',');
                string[] DFTempColor = Request["DFTempColor[]"].Split(',');
                string[] DFTempColorCont = Request["TxtTempColorElse[]"].Split(',');
                string[] DFTempPlumpness = Request["HidDFTempPlumpness[]"].Split(',');
                string[] DFTempMeasureYN = Request["HidDFTempMeasureYN[]"].Split(',');
                string[] DFTempMeasureCont = Request["HidDFTempMeasureCont[]"].Split(',');
                string[] DFTempRemark = Request["TxtDFTempRemark[]"].Split(',');
                string[] DFTempCareRecord = Request["HidDFTempCareRecord[]"].Split(',');

                List<DBItem> DBItemDataList = new List<DBItem>();

                if(ID.Length > 0)
                {
                    for(int i = 0; i <= ID.Length - 1; i++)
                    {
                        DBItemDataList.Clear();

                        if(Request["HidModeVal"] == "New")
                            DFRECORDID = base.creatid("DIGI_FLAP_RECORD", userno, feeno, "0");
                        else
                            DFRECORDID = Request["DFRecordID_" + ID[i]];

                        //儲存實體檔案
                        string FileNameTmp = "", ExtensionTemp = "", SaveNameTmp = "";
                        if(Request.Files["UpFileDFTempPic_" + ID[i]] != null && !string.IsNullOrWhiteSpace(Request.Files["UpFileDFTempPic_" + ID[i]].FileName))
                        {
                            HttpPostedFileBase UploadFileBase = Request.Files["UpFileDFTempPic_" + ID[i]];
                            FileNameTmp = UploadFileBase.FileName;
                            ExtensionTemp = System.IO.Path.GetExtension(FileNameTmp);  //副檔名
                            SaveNameTmp = DFRECORDID + ExtensionTemp;
                            UploadFileBase.SaveAs(Path.Combine(Server.MapPath("../UpFileMngArea/"), SaveNameTmp));
                        }

                        if(Request["HidModeVal"] == "New")
                        {
                            //新增
                            DBItemDataList.Add(new DBItem("DFRECORD_ID", DFRECORDID, DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("DFTEMP_ID", ID[i], DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("RECORDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                            DBItemDataList.Add(new DBItem("DFRECORD_TEMPERATURE_ROOM", DFTempTemperatureRoom[i], DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("DFRECORD_TEMPERATURE", DFTempTemperature[i], DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("DFRECORD_COLOR", DFTempColor[i], DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("DFRECORD_COLOR_CONT", DFTempColorCont[i], DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("DFRECORD_PLUMPNESS", DFTempPlumpness[i], DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("DFRECORD_MEASURE_YN", DFTempMeasureYN[i], DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("DFRECORD_MEASURE_CONT", DFTempMeasureCont[i], DBItem.DBDataType.String));
                            if(FileNameTmp != "")
                                DBItemDataList.Add(new DBItem("DFRECORD_PIC", SaveNameTmp, DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("DFRECORD_REMARK", DFTempRemark[i], DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("DFRECORD_CARE_RECORD", DFTempCareRecord[i], DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("DELETED", "N", DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("CREANO", userno, DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));

                            if(DigitFlapTemperature.DBExecInsert("DIGI_FLAP_RECORD", DBItemDataList) > 0)
                            {
                                ERow++;
                                if(DFTempCareRecord[i] == "Y")
                                {
                                    #region  護理紀錄
                                    //皮辦溫度帶記錄模板-12/12討論後版本
                                    //1.	正常
                                    //治療/處置：指(趾)或皮瓣溫度紀錄
                                    //一般：病室【室溫】℃，病人【患肢】【溫度】℃，顏色：【顏色】，毛細血管充盈評估:【毛細血管充盈評估】，續觀。
                                    //2.	異常(判斷標準依有沒有勾異常處置來判斷)
                                    //治療/處置：指(趾)或皮瓣溫度紀錄
                                    //一般：病室【室溫】℃，病人【患肢】【溫度】℃，皮膚呈【顏色】，毛細血管充盈評估:【毛細血管充盈評估】，予【異常護理措施】等處置，續觀。
                                    DataTable DT_CR = DigitFlapTemperature.sel_temperature_data(feeno, ID[i], "");
                                    //顏色
                                    string ColorStr = (DFTempColor[i] == "99") ? DFTempColorCont[i] : DFTempColor[i];
                                    //部位
                                    string PositionStr = (string.IsNullOrEmpty(DT_CR.Rows[0]["DFTEMP_POSITION_CONT"].ToString())) ? DT_CR.Rows[0]["DFTEMP_POSITION"].ToString() : DT_CR.Rows[0]["DFTEMP_POSITION_CONT"].ToString();
                                    //保飽度
                                    string PlumpnessStr = DFTempPlumpness[i].Replace("1", "未作")
                                        .Replace("2", "快(1-2秒內由蒼白轉為紅潤)")
                                        .Replace("3", "慢(大於2秒由蒼白轉為紅潤)")
                                        .Replace("4", "蒼白，無法回充盈");

                                    string CareRecordCont = "";

                                    if(string.IsNullOrWhiteSpace(DFTempMeasureYN[i]) || DFTempMeasureYN[i] == "N")
                                    {
                                        CareRecordCont = "病室" + DFTempTemperatureRoom[i] + "℃，病人" + PositionStr + " " + DFTempTemperature[i] + "℃"
                                            + "，顏色：" + ColorStr + "，毛細血管充盈評估:" + PlumpnessStr + "，續觀。";//"，備註：" + DFTempRemark[i] +
                                    }
                                    else if(!string.IsNullOrWhiteSpace(DFTempMeasureYN[i]) && DFTempMeasureYN[i] == "Y")
                                    {
                                        CareRecordCont = "病室" + DFTempTemperatureRoom[i] + "℃，病人" + PositionStr + " " + DFTempTemperature[i] + "℃"
                                            + "，皮膚呈" + ColorStr + "，毛細血管充盈評估:" + PlumpnessStr + "，予" + DFTempMeasureCont[i] + "等處置，續觀。";//，備註：" + DFTempRemark[i] + "
                                    }
                                    base.Insert_CareRecord(DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DFRECORDID, "指(趾)或皮瓣溫度紀錄", CareRecordCont, "", "", "", "", ID[i]);

                                    #endregion  護理紀錄 end
                                }
                            }
                        }
                        else
                        {
                            //修改
                            DBItemDataList.Add(new DBItem("DFRECORD_TEMPERATURE_ROOM", DFTempTemperatureRoom[i], DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("DFRECORD_TEMPERATURE", DFTempTemperature[i], DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("DFRECORD_COLOR", DFTempColor[i], DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("DFRECORD_COLOR_CONT", DFTempColorCont[i], DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("DFRECORD_PLUMPNESS", DFTempPlumpness[i], DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("DFRECORD_MEASURE_YN", DFTempMeasureYN[i], DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("DFRECORD_MEASURE_CONT", DFTempMeasureCont[i], DBItem.DBDataType.String));
                            if(FileNameTmp != "")
                                DBItemDataList.Add(new DBItem("DFRECORD_PIC", SaveNameTmp, DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("DFRECORD_REMARK", DFTempRemark[i], DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("DFRECORD_CARE_RECORD", DFTempCareRecord[i], DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));

                            if(DigitFlapTemperature.DBExecUpdate("DIGI_FLAP_RECORD", DBItemDataList, string.Format("FEENO={0} AND DFRECORD_ID={1}", DigitFlapTemperature.SQLString(feeno), DigitFlapTemperature.SQLString(DFRECORDID))) > 0)
                            {
                                ERow++;
                                if(DFTempCareRecord[i] == "Y")
                                {
                                    #region  護理紀錄
                                    DataTable DT_CR = DigitFlapTemperature.sel_temperature_data(feeno, ID[i], "");

                                    //顏色
                                    string ColorStr = (DFTempColor[i] == "99") ? DFTempColorCont[i] : DFTempColor[i];
                                    //部位
                                    string PositionStr = (string.IsNullOrEmpty(DT_CR.Rows[0]["DFTEMP_POSITION_CONT"].ToString())) ? DT_CR.Rows[0]["DFTEMP_POSITION"].ToString() : DT_CR.Rows[0]["DFTEMP_POSITION_CONT"].ToString();
                                    //保飽度
                                    string PlumpnessStr = DFTempPlumpness[i].Replace("1", "未作")
                                        .Replace("2", "快(1-2秒內由蒼白轉為紅潤)")
                                        .Replace("3", "慢(大於2秒由蒼白轉為紅潤)")
                                        .Replace("4", "蒼白，無法回充盈");

                                    string CareRecordCont = "";

                                    if(string.IsNullOrWhiteSpace(DFTempMeasureYN[i]) || DFTempMeasureYN[i] == "N")
                                    {
                                        CareRecordCont = "病室" + DFTempTemperatureRoom[i] + "℃，病人" + PositionStr + " " + DFTempTemperature[i] + "℃"
                                            + "，顏色：" + ColorStr + "，毛細血管充盈評估：" + PlumpnessStr + "，續觀。";//"，備註：" + DFTempRemark[i] + 
                                    }
                                    else if(!string.IsNullOrWhiteSpace(DFTempMeasureYN[i]) && DFTempMeasureYN[i] == "Y")
                                    {
                                        CareRecordCont = "病室" + DFTempTemperatureRoom[i] + "℃，病人" + PositionStr + " " + DFTempTemperature[i] + "℃"
                                            + "，皮膚呈：" + ColorStr + "，毛細血管充盈評估：" + PlumpnessStr + "，予" + DFTempMeasureCont[i] + "等處置，續觀。";//，備註：" + DFTempRemark[i] + "
                                    }
                                    if(base.Upd_CareRecord(Request["DFTempTime_" + ID[i]], DFRECORDID, "指(趾)或皮瓣溫度紀錄", CareRecordCont, "", "", "", "", ID[i]) < 1)
                                        base.Insert_CareRecord(Request["DFTempTime_" + ID[i]], DFRECORDID, "指(趾)或皮瓣溫度紀錄", CareRecordCont, "", "", "", "", ID[i]);

                                    #endregion  護理紀錄 end
                                }
                                else if(DFTempCareRecord[i] == "N")
                                {
                                    base.Del_CareRecord(DFRECORDID, ID[i]);
                                    //DigitFlapTemperature.DBExecDelete("CARERECORD_DATA", "CARERECORD_ID = '" + DFRECORDID + "' AND SELF = '" + ID[i] + "' ");
                                    //del_emr(DFRECORDID + "DIGI_FLAP_RECORD", userinfo.EmployeesNo);
                                }
                            }
                        }
                    }
                }

                if(Request["HidModeVal"] == "New")
                {
                    if (ERow > 0)
                    {
                        List<Bill_RECORD> billDataList = new List<Bill_RECORD>();
                        Bill_RECORD billData = new Bill_RECORD();

                        billData.HO_ID = "8447034";
                        billData.COUNT = "1";
                        billDataList.Add(billData);

                        SaveBillingRecord(billDataList);
                        return "<script>alert('新增成功'); window.opener.location.href = 'listTemperature';window.close();</script>";

                    }
                    else
                    {
                        return "<script>alert('新增失敗'); window.close();</script>";
                    }
                }
                else
                {
                    if(ERow > 0)
                        return "<script>alert('儲存成功'); window.opener.location.href = 'listTemperature';window.close();</script>";
                    else
                        return "<script>alert('儲存失敗'); window.close();</script>";
                }
            }

            return "<script>alert('請重新選擇病患'); window.opener.location.href = 'listTemperature';window.close();</script>";
        }

        //刪除評估
        [HttpPost]
        public string DelAssmentInfo(string RowStr)
        {
            if(Session["PatInfo"] != null)
            {
                string feeno = ptinfo.FeeNo;
                string userno = userinfo.EmployeesNo;
                string[] ID = RowStr.Split(',');
                int ERow = 0, CheckRow = 0;

                List<DBItem> DBItemDataList = new List<DBItem>();
                for(int i = 0; i <= ID.Length - 1; i++)
                {
                    DBItemDataList.Clear();
                    DBItemDataList.Add(new DBItem("DELETED", "Y", DBItem.DBDataType.String));
                    DBItemDataList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
                    DBItemDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));

                    CheckRow = DigitFlapTemperature.DBExecUpdate("DIGI_FLAP_RECORD", DBItemDataList, string.Format("FEENO={0} AND DFRECORD_ID={1}", DigitFlapTemperature.SQLString(feeno), DigitFlapTemperature.SQLString(ID[i])));
                    if(CheckRow > 0)
                    {
                        ERow += CheckRow;
                        ERow += base.Del_CareRecord(ID[i], "");
                    }
                }

                if(ERow > 0)
                    return "Y";
                else
                    return "N";

            }

            return "O";
        }

        [HttpPost]
        public string GetTemperatureAlert(string DFRecordID, string CtrlLimbID, string ThisTemp)
        {//判斷溫度是否異常  ||(此次對照溫度-前次對照溫度)|-|(此次患肢溫度-前次患肢)|| >= 1時要跳異常提示 
            //公式未完成，目前畫面設計沒有對照肢的此次溫度，咏蓁說要等客戶確認要怎麼修改後才再動工，可能幾個月後 by 2016/4/29
            if(Session["PatInfo"] != null)
            {
                //string feeno = ptinfo.FeeNo;
                //string userno = userinfo.EmployeesNo;
                //int LimbThisTemp = 0, LimbLastTemp = 0, CtrlLimbThisTemp = 0, CtrlLimbLastTemp = 0;

                //DataTable DT_Limb = DigitFlapTemperature.sel_record_temperature_info(feeno, DFRecordID);  //患肢
                //DataTable DT_Ctrl_Limb = DigitFlapTemperature.sel_record_temperature_info(feeno, CtrlLimbID);  //對照肢

                //if(DT_Limb != null && DT_Limb.Rows.Count > 0)
                //{
                //    LimbLastTemp = Convert.ToInt32(DT_Limb.Rows[0]["DFRECORD_TEMPERATURE"].ToString());
                //}

                //if (DT_Ctrl_Limb != null && DT_Ctrl_Limb.Rows.Count > 0)
                //{

                //}

                ////if (ERow > 0)
                ////    return "Y";
                ////else
                ////    return "N";
            }

            return "O";
        }

        #region  列印

        public ActionResult Record_List_PDF(string LoginEmpNo, string feeno, string St, string Ed)
        {
            string userno = "";  //這裡無法直接使用 userinfo ，會造成無法抓取的情況，故用參數值

            PatientInfo pinfo = new PatientInfo();
            byte[] ByteCode = webService.GetPatientInfo(feeno);
            //病人資訊
            if(ByteCode != null)
                pinfo = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(ByteCode));
            ViewData["ptinfo"] = pinfo;

            ViewBag.DT_data = DigitFlapTemperature.sel_temperature_data_row(feeno, St, Ed);  //部位

            DataTable DT = DigitFlapTemperature.sel_temperature_record_by_date(feeno, St, Ed);  //評估

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
            ViewBag.DT_record = DT;

            return View();
        }

        #endregion  列印 end

        #endregion  紀錄 end

        #region  共用

        [HttpPost]
        public JsonResult GetCategoryContList(string CategoryNum)
        {//切換類別，抓取部位資訊
            string PGroup = "";
            switch(CategoryNum)
            {
                case "1":
                    //對照肢
                    PGroup = "digit_position";
                    break;
                case "2":
                    //患肢
                    PGroup = "digit_position";
                    break;
                case "3":
                    //皮瓣
                    PGroup = "flap_position";
                    break;
                default:
                    break;
            }
            //取得 類別所屬部位
            DataTable DT = DigitFlapTemperature.sel_category_list(PGroup, "digitflaptemperature");

            List<SelectListItem> DropList = SetDropDownList.GetddlItem("", DT, false, 0, 1);
            return Json(DropList.ToList());
        }

        [HttpPost]
        public string SelectDelPositionInfo(string DFTempID)
        {//抓取單筆資訊(目前for刪除用)
            string feeno = ptinfo.FeeNo;
            DataTable DT = DigitFlapTemperature.sel_temperature_data(feeno, DFTempID, "");

            return DigitFlapTemperature.DatatableToJsonArray(DT);
        }

        #endregion  共用 end
    }
}
