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
using System.Drawing;
using Oracle.ManagedDataAccess.Client;

namespace NIS.Controllers
{
    public class WoundController : BaseController
    {
        private Wound wound;
        Assess ass_m = new Assess();
        private DBConnector link = new DBConnector();

        public WoundController()
        {
            this.wound = new Wound();
        }

        //傷口列表(首頁)
        public ActionResult listTrauma(string id)
        {
            if (Session["PatInfo"] != null)
            {
                //護理站列表
                byte[] listByteCode = this.webService.GetCostCenterList();
                string listJsonArray = CompressTool.DecompressString(listByteCode);
                ViewData["costlist"] = JsonConvert.DeserializeObject<List<CostCenterList>>(listJsonArray);
                ViewBag.dt_wound_data = wound.sel_wound_data(base.ptinfo.FeeNo, "");
                ViewBag.feeno = base.ptinfo.FeeNo;
                ViewBag.RootDocument = GetSourceUrl();
                ViewBag.userno = base.userinfo.EmployeesNo;
                ViewBag.new_id = id;
                return View();
            }
            Response.Write("<script>alert('登入逾時');</script>");
            return new EmptyResult();
        }

        #region 新增傷口

        [HttpGet]
        public ActionResult AddTraumaPrompt(string num)
        {
            if (Session["PatInfo"] != null)
            {
                ViewBag.dt_w_g = SetDropDownList.GetddlItem("", ass_m.sel_type_list("wound_general", "assess"), false, 0, 1);//一般
                ViewBag.dt_w_s = SetDropDownList.GetddlItem("", ass_m.sel_type_list("wound_scald", "assess"), false, 0, 1);//燒傷
                ViewBag.dt_w_p = SetDropDownList.GetddlItem("", ass_m.sel_type_list("wound_pressure", "assess"), false, 0, 1);//壓傷
                ViewBag.dt_w_l = SetDropDownList.GetddlItem("", ass_m.sel_type_list("wound_labor", "assess"), false, 0, 1);//生產
                //護理站列表
                byte[] listByteCode = this.webService.GetCostCenterList();
                string listJsonArray = CompressTool.DecompressString(listByteCode);
                ViewData["costlist"] = JsonConvert.DeserializeObject<List<CostCenterList>>(listJsonArray);
                ViewBag.num = num;
                return View();
            }

            Response.Write("<script>alert('請重新選擇病患');window.close();</script>");
            return new EmptyResult();
        }

        //新增傷口
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult AddTraumaPrompt(FormCollection form)
        {
            if (Session["PatInfo"] != null)
            {
                string feeno = ptinfo.FeeNo;
                string userno = userinfo.EmployeesNo;
                string id = base.creatid("WOUND_DATA", userno, feeno, "0");
                string date = "", type = form["wound_type"], position = form["position"], location = form["location"], reason = form["reason"], positionType = form["param_postion_type"];
                if (form["undate"] == null)
                    date = form["date"];
                if (form["wound_type"] == "99")
                    type = form["txt_woundtype_other"];
                List<DBItem> insertDataList = new List<DBItem>();
                insertDataList.Add(new DBItem("WOUND_ROW", "WOUND_DATA_SEQUENCE.NEXTVAL", DBItem.DBDataType.Number));
                insertDataList.Add(new DBItem("WOUND_ID", id, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREANO", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATTIME", date, DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("NUM", form["num"], DBItem.DBDataType.Number));
                insertDataList.Add(new DBItem("TYPE", type, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("POSITION", position, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("POSITION_TYPE", positionType, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("LOCATION", location, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("REASON", reason, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                int erow = wound.DBExecInsert("WOUND_DATA", insertDataList);
                if (erow > 0)
                {
                    try
                    {
                        string content = string.Empty;
                        if (type == "壓瘡" || type == "壓傷")
                        {//於【部位】因【導因】等原因，自【發生地點】發生一壓傷傷口，發生日期不詳。
                            if (date == "")
                                content = "於" + position + "因" + reason + "等原因，自" + location + "發生一" + type + "傷口，發生日期不詳。";
                            //【發生日期】【發生時間】，於【部位】因【導因】等原因，自【發生地點】發生一壓傷傷口。
                            else
                                content = date + "，於" + position + "因" + reason + "等原因，自" + location + "發生一" + type + "傷口。";
                        }
                        else
                        {//於病人【部位】有一【傷口類別】傷口，發生日期不詳。
                            if (date == "")
                                content = "於病人" + position + "有一" + type + "傷口，發生日期不詳。";
                            //【發生日期】【發生時間】，於【部位】發生一【傷口類別】傷口。
                            else
                                content = date + "，於" + position + "發生一" + type + "傷口。";
                        }
                        base.Insert_CareRecord(DateTime.Now.ToString("yyyy/MM/dd HH:mm:00"), id, "", content, "", "", "", "", "Wound");
                    }
                    catch { }
                    Response.Write("<script>alert('新增成功');window.close();window.opener.location.href='listTrauma?id=" + id + "';</script>");
                }
                else
                    Response.Write("<script>alert('新增失敗');window.close();</script>");
                return new EmptyResult();
            }
            Response.Write("<script>alert('請重新選擇病患');window.close();</script>");
            return new EmptyResult();
        }

        //修改地點
        [HttpPost]
        public ActionResult Update_Wound_Data(string wound_id, string location, string reason)
        {
            if (Session["PatInfo"] != null)
            {
                List<DBItem> insertDataList = new List<DBItem>();
                insertDataList.Add(new DBItem("LOCATION", location, DBItem.DBDataType.String));
                string where = " FEENO = '" + base.ptinfo.FeeNo + "' AND WOUND_ID = '" + wound_id + "' ";
                insertDataList.Add(new DBItem("REASON", reason, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDNO", userinfo.EmployeesNo, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                int erow = wound.DBExecUpdate("WOUND_DATA", insertDataList, where);
                if (erow > 0)
                {
                    DataTable dt = wound.sel_wound_data(base.ptinfo.FeeNo, wound_id);
                    DataTable dt_care = new DataTable();
                    wound.DBExecSQL("SELECT RECORDTIME FROM CARERECORD_DATA WHERE FEENO = '" + base.ptinfo.FeeNo + "' AND CARERECORD_ID = '" + wound_id + "' AND SELF = 'Wound' ", ref dt_care);
                    try
                    {
                        string content = string.Empty;
                        if (dt.Rows[0]["TYPE"].ToString() == "壓瘡" || dt.Rows[0]["TYPE"].ToString() == "壓傷")
                        {//於【部位】因【導因】等原因，自【發生地點】發生一壓傷傷口，發生日期不詳。
                            if (dt.Rows[0]["CREATTIME"].ToString() == "")
                                content = "於" + dt.Rows[0]["POSITION"].ToString() + "因" + dt.Rows[0]["REASON"].ToString() + "等原因，自" + dt.Rows[0]["LOCATION"].ToString() + "發生一" + dt.Rows[0]["TYPE"].ToString() + "傷口，發生日期不詳。";
                            //【發生日期】【發生時間】，於【部位】因【導因】等原因，自【發生地點】發生一壓傷傷口。
                            else
                                content = dt.Rows[0]["CREATTIME"].ToString() + "，於" + dt.Rows[0]["POSITION"].ToString() + "因" + dt.Rows[0]["REASON"].ToString() + "等原因，自" + dt.Rows[0]["LOCATION"].ToString() + "發生一" + dt.Rows[0]["TYPE"].ToString() + "傷口。";
                        }
                        else
                        {//於病人【部位】有一【傷口類別】傷口，發生日期不詳。
                            if (dt.Rows[0]["CREATTIME"].ToString() == "")
                                content = "於病人" + dt.Rows[0]["POSITION"].ToString() + "有一" + dt.Rows[0]["TYPE"].ToString() + "傷口，發生日期不詳。";
                            //【發生日期】【發生時間】，於【部位】發生一【傷口類別】傷口。
                            else
                                content = dt.Rows[0]["CREATTIME"].ToString() + "，於" + dt.Rows[0]["POSITION"].ToString() + "發生一" + dt.Rows[0]["TYPE"].ToString() + "傷口。";
                        }
                        base.Upd_CareRecord(Convert.ToDateTime(dt_care.Rows[0]["RECORDTIME"]).ToString("yyyy/MM/dd HH:mm:ss"), wound_id, "", content, "", "", "", "", "Wound");
                    }
                    catch { }
                    Response.Write("<script>alert('更新成功');window.location.href='listTrauma';</script>");
                }
                else
                    Response.Write("<script>alert('更新失敗');window.location.href='listTrauma';</script>");
            }
            else
                Response.Write("<script>alert('登入逾時');</script>");
            return new EmptyResult();
        }

        //刪除傷口
        [HttpPost]
        public ActionResult Delete_Wound_Data(string wound_id, string txt_del_reason)
        {
            List<DBItem> dbItem = new List<DBItem>();
            dbItem.Add(new DBItem("DELETED", base.userinfo.EmployeesNo, DBItem.DBDataType.String));
            dbItem.Add(new DBItem("DEL_REASON", txt_del_reason, DBItem.DBDataType.String));
            dbItem.Add(new DBItem("UPDNO", userinfo.EmployeesNo, DBItem.DBDataType.String));
            dbItem.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
            string where = "FEENO = '" + base.ptinfo.FeeNo + "' AND WOUND_ID = '" + wound_id + "' ";
            int erow = this.wound.DBExecUpdate("WOUND_DATA", dbItem, where);
            if (erow > 0)
            {
                try
                {
                    base.Del_CareRecord(wound_id, "Wound");//刪除傷口紀錄的護理紀錄
                    //base.Del_CareRecord("", wound_id);//刪除傷口護理的護理紀錄
                }
                catch { }
                Response.Write("<script>alert('刪除成功');window.location.href='listTrauma';</script>");
            }
            else
                Response.Write("<script>alert('刪除失敗');window.location.href='listTrauma';</script>");
            return new EmptyResult();
        }

        #endregion

        #region 傷口紀錄

        //傷口列表(首頁)_傷口紀錄
        public ActionResult Partial_Record(string wound_id)
        {
            if (Session["PatInfo"] != null)
            {
                string feeno = ptinfo.FeeNo;
                DataTable dt = new DataTable();
                dt = wound.sel_wound_record_data(feeno, wound_id, "");
                string ExudateNature = wound.sel_wound_sysparams_data("assess", "wound_exterior");

                dt.Columns.Add("username");
                if (dt.Rows.Count > 0)
                {
                    string userno = dt.Rows[0]["CREANO"].ToString();
                    byte[] listByteCode = webService.UserName(userno);
                    string listJsonArray = CompressTool.DecompressString(listByteCode);
                    UserInfo user_name = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                    foreach (DataRow r in dt.Rows)
                    {
                        if (userno != r["CREANO"].ToString())
                        {
                            userno = r["CREANO"].ToString();
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

                ViewBag.dt_wound_record = dt;
                ViewBag.login = userinfo.EmployeesNo;
                return PartialView();
            }
            Response.Write("<script>alert('登入逾時');</script>");
            return new EmptyResult();
        }
        //傷口列表(首頁)_傷口紀錄
        public string Partial_Record_Img(string record_id)
        {
            string return_img = null;
            if (Session["PatInfo"] != null)
            {
                string feeno = ptinfo.FeeNo;
                DataTable dt = new DataTable();
                dt = wound.sel_wound_record_img(feeno, record_id);

                if (dt.Rows.Count == 1)
                {
                    byte[] imageBytes = (byte[])dt.Rows[0]["WOUND_IMG_DATA"];
                    return_img = Convert.ToBase64String(imageBytes);
                }

                return return_img;
            }
            Response.Write("<script>alert('登入逾時');</script>");
            return "";
        }
        public ActionResult AddWoundRecordPrompt(string row, string ModeVal)
        {
            if (Session["PatInfo"] != null)
            {
                string feeno = ptinfo.FeeNo;
                string userno = userinfo.EmployeesNo;
                DataTable DT = new DataTable();
                foreach (string ID in row.Split(','))
                    DT = wound.sel_wound_record_byLastInfo(ModeVal, DT, ID);

                //傷口 外觀(抓取為部位)
                //string WoundExterior = wound.sel_wound_sysparams_data("assess", "wound_general");
                string WoundExterior = wound.sel_wound_sysparams_data("assess", "wound_exterior");
                //傷口 滲出液 顏色
                string ExudateColor = wound.sel_wound_sysparams_data("assess", "exudate_color");
                //傷口 滲出液 性狀
                string ExudateNature = wound.sel_wound_sysparams_data("assess", "exudate_nature");

                ViewBag.DT = DT;
                ViewBag.WoundExterior = WoundExterior;  //傷口 外觀(抓取為部位)
                ViewBag.ExudateColor = ExudateColor;  //傷口 滲出液 顏色
                ViewBag.ExudateNature = ExudateNature;  //傷口 滲出液 性狀
                ViewBag.mod = ModeVal;

                return View();
            }

            Response.Write("<script>alert('請重新選擇病患');window.close();</script>");
            return new EmptyResult();
        }

        //新增、修改 評估
        [HttpPost]
        [ValidateInput(false)]
        public string SetWoundRecordData(FormCollection form, List<HttpPostedFileBase> Wound_Img)
        {
            if (Session["PatInfo"] != null)
            {
                string[] StrArr_WoundLevel = { "N/A", "第I級(Superficial burn)：表皮層受損，皮膚發紅沒有水泡", "第II級(Superficial partial thickness burn)：表皮及部分真皮破損", "第III級(Deep partial thickness burn)：表皮及真皮破損，有傷及毛囊及毛脂腺", "第IV級(Full partial thickness burn)：表皮、真皮及皮下組織" };
                string[] arrgrade = { "等級Ｉ：局部皮膚發紅，施以指壓不會變白", "等級II：皮膚表淺部分皮層受損，已損及真皮，會出現表淺性粉紅凹洞傷口，但沒有壞死組織。也許會出現未破損或破裂的水泡",
                                    "等級III：真皮層皮膚完全受損。也許可看見皮下脂肪組織，但未影響到骨頭、肌腱或肌肉層", "等級IV：大量的組織壞死深及骨頭、肌腱或肌肉",
                                    "無法分級：全層皮膚組織缺損，並傷口底部被腐肉或焦痂覆蓋，導致無法評估潰傷的深度", "疑似深層組織損傷(SDTI)：潛在的壓力或剪力，導致軟組織損傷，局部皮膚出現紫色或紫褐色，或充血的水泡" };
                string userno = userinfo.EmployeesNo;
                string feeno = ptinfo.FeeNo;
                string[] ID = form["ID[]"].Split(',');
                string[] ROWVal = form["row[]"].Split(',');
                string RecordID = "";
                int ERow = 0;
                string[] WoundType = form["HidWoundType[]"].Split(',');  //傷口種類 BedSore(壓傷)、WoundGeneral(非壓傷皆為一般傷口)
                string[] TypeHidVal = form["type[]"].Split(',');  //傷口種類 實際文字

                //備註：hidden 會取做 HidRbn 或 HidChk，單純因為在抓取各控件的 hidden 時，直接將其冠上 Hid 就不需各別指定名稱，較方便取得  by wawa 2016/7/11
                string[] wound_date = form["wound_date[]"].Split(',');
                string[] wound_time = form["wound_time[]"].Split(',');

                string[] RangeYN = form["HidRbnRangeYN[]"].Split(',');  //range_YN
                string[] BedSoreRange = new string[ID.Length], RangeHeight = new string[ID.Length], RangeWidth = new string[ID.Length], RangeDepth = new string[ID.Length];
                string[] WoundLevel = new string[ID.Length];
                string[] ScaldRange = new string[ID.Length];
                if (ID.Length > 0)
                {
                    for (int i = 0; i <= ID.Length - 1; i++)
                    {

                        if (WoundType[i] == "WoundGeneral" && TypeHidVal[i] == "燙傷")
                        {
                            WoundLevel = form["Hidwound_class[]"].Split(',');
                            ScaldRange = form["txt_wound_range[]"].Split(',');
                        }
                        else
                        {
                            BedSoreRange = form["HidRbnBedSoreRange[]"].Split(',');  //grade
                            RangeHeight = form["TxtRangeHeight[]"].Split(',');
                            RangeWidth = form["TxtRangeWidth[]"].Split(',');
                            RangeDepth = form["TxtRangeDepth[]"].Split(',');
                        }
                    }
                }
                string[] BackoutYN = form["HidRbnBackoutYN[]"].Split(',');  //backout_YN
                string[] Backout99 = form["TxtBackout99[]"].Split(',');  //range_other
                string[] RangeNReason = form["TxtRangeNReason[]"].Split(',');  //range_other
                //string[] ExudateYN = form["TxtRangeNReason[]"].Split(',');
                string[] ExudateYN = form["HidRbnExudateYN[]"].Split(',');

                ////↓edit by jarvis 20160913
                string[] Exterior = form["HidChkExudateExterior[]"].Split(',');  //Exterior
                string[] Exterior_other = new string[ID.Length];  //外觀其他的textbox
                for (int i = 0; i < ROWVal.Length; i++)
                {
                    if (form["Txt_ExudateExterior" + ROWVal[i] + "_Other"] != null)
                    {
                        Exterior_other[i] = form["Txt_ExudateExterior" + ROWVal[i] + "_Other"];
                    }
                    else
                    {
                        Exterior_other[i] = "";
                    }
                }//"Txt_ExudateExterior544_Other"
                string[] ExudateColor = form["HidChkExudateColor[]"].Split(',');  //exudate_color
                string[] ExudateColor_other = new string[ROWVal.Length];//一般傷口的顏色--其他的textbox
                for (int i = 0; i < ROWVal.Length; i++)
                {//Txt_ExudateColor548_Other
                    if (form["Txt_ExudateColor" + ROWVal[i] + "_Other"] != null)
                    {
                        ExudateColor_other[i] = form["Txt_ExudateColor" + ROWVal[i] + "_Other"];
                    }
                    else
                    {
                        ExudateColor_other[i] = "";
                    }
                }
                string[] ExudateNature_other = new string[ROWVal.Length];//一般傷口的性狀--其他的textbox
                for (int i = 0; i < ROWVal.Length; i++)
                {//Txt_ExudateNature548_Other
                    if (form["Txt_ExudateNature" + ROWVal[i] + "_Other"] != null)
                    {
                        ExudateNature_other[i] = form["Txt_ExudateNature" + ROWVal[i] + "_Other"];
                    }
                    else
                    {
                        ExudateNature_other[i] = "";
                    }
                }///↑edit  by jarvis 20160913
                string[] ExudateNatureBedSore = form["HidChkExudateNatureBedSore[]"].Split(',');  //exudate_color
                string[] ExudateNature = form["HidChkExudateNature[]"].Split(',');  //exudate_nature
                string[] ExudateNatureBedSore99 = form["TxtExudateNatureBedSore99[]"].Split(',');  //exudate_nature

                string[] RbnExudateAmount = form["HidRbnExudateAmount[]"].Split(',');  //RbnExudateAmount
                string[] SuturesYN = form["HidRbnSuturesYN[]"].Split(',');  //sutures_YN
                string[] StitchesDate = form["TxtStitchesDate[]"].Split(',');  //stitchesDate
                string[] SuturesYOther = form["TxtSuturesYOther[]"].Split(',');  //pin
                string[] PinVal = form["TxtPin[]"].Split(',');  //pin
                string[] HandleYN = form["HidRbnHandleYN[]"].Split(',');  //handle_YN
                string[] HandleItemWound = form["HidChkHandleItemWound[]"].Split(',');  //handle_item
                string[] HandleItemBedSore = form["HidChkHandleItemBedSore[]"].Split(',');  //handle_item
                string[] HandleItemBabyLabor = form["HidChkHandleItemBabyLabor[]"].Split(',');  //handle_item
                string[] HandleItemWound99 = form["TxtHandleItemWound99[]"].Split(',');  //handle_other
                string[] HandleItemBedSore99 = form["TxtHandleItemBedSore99[]"].Split(',');  //handle_other
                string[] HandleItemBabyLabor99 = form["TxtHandleItemBabyLabor99[]"].Split(',');  //handle_other
                string[] PlanYN = form["HidChkPlanYN"].Split(',');  //plan_YN
                string[] PositionVal = form["position[]"].Split(',');  //傷口部位
                string[] PositionTypeVal = form["positionType[]"].Split(',');  //傷口部位

                string[] ObsRange = form["HidObsRange_class[]"].Split(',');  //會陰範圍(傷口邊緣)
                string[] ObsRed = form["HidObsRed_class[]"].Split(',');  //會陰(紅)
                string[] ObsSwollen = form["HidObsSwollen_class[]"].Split(',');  //會陰(腫)
                string[] ObsSwollen_range = form["txt_ObsSwollen_range[]"].Split(',');  //會陰水腫範圍
                string[] ObsBruise = form["HidObsBruise_class[]"].Split(',');  //會陰(瘀血)
                string[] ObsSecretion = form["HidObsSecretion_class[]"].Split(',');  //會陰分泌物
                List<DBItem> DBItemDataList = new List<DBItem>();
                if (ID.Length > 0)
                {
                    for (int i = 0; i <= ID.Length - 1; i++)
                    {
                        DBItemDataList.Clear();

                        if (form["HidModeVal"] == "Insert_Record")
                        {
                            //新增
                            RecordID = base.creatid("DIGI_FLAP_RECORD", userno, feeno, "0");
                            DBItemDataList.Add(new DBItem("plan_YN", PlanYN[i], DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("RECORD_ID", RecordID, DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("WOUND_ID", ID[i], DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("RECORDTIME", wound_date[i] + " " + wound_time[i], DBItem.DBDataType.DataTime));
                            DBItemDataList.Add(new DBItem("range_YN", RangeYN[i], DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("backout_YN", BackoutYN[i], DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("exudate_YN", ExudateYN[i], DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("sutures_YN", SuturesYN[i], DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("stitchesDate", StitchesDate[i], DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("EXTERIOR_OTHER", Exterior_other[i], DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("EXTERIOR", Exterior[i], DBItem.DBDataType.String));
                            if (SuturesYN[i] == "有縫線")
                                DBItemDataList.Add(new DBItem("pin", SuturesYOther[i], DBItem.DBDataType.String));
                            else if (SuturesYN[i] == "縫線已拆")
                                DBItemDataList.Add(new DBItem("pin", PinVal[i], DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("handle_YN", HandleYN[i], DBItem.DBDataType.String));
                            if (TypeHidVal[i] == "生產") //Obs
                            {
                                DBItemDataList.Add(new DBItem("PERINEUM_RANGE", ObsRange[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("PERINEUM_RED", ObsRed[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("PERINEUM_SWOLLEN", ObsSwollen[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("SWOLLEN_RANGE", ObsSwollen_range[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("PERINEUM_BRUISE", ObsBruise[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("PERINEUM_SECRETION", ObsSecretion[i], DBItem.DBDataType.String));
                            }
                            else if (TypeHidVal[i] != "燙傷")
                            {
                                DBItemDataList.Add(new DBItem("RANGE_WIDTH", RangeWidth[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("RANGE_HEIGHT", RangeHeight[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("RANGE_DEPTH", RangeDepth[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("grade", BedSoreRange[i], DBItem.DBDataType.String));
                            }
                            else
                            {
                                DBItemDataList.Add(new DBItem("WOUND_LEVEL", WoundLevel[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("SCALD_RANGE", ScaldRange[i], DBItem.DBDataType.String));
                            }
                            if (WoundType[i] == "BedSore")
                            {
                                DBItemDataList.Add(new DBItem("range_other", RangeNReason[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("EXUDATE_NATURE", ExudateNatureBedSore[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("EXUDATE_NATURE_OTHER", ExudateNatureBedSore99[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("handle_item", HandleItemBedSore[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("handle_other", HandleItemBedSore99[i], DBItem.DBDataType.String));
                            }
                            else if (WoundType[i] == "BabyLabor")
                            {
                                DBItemDataList.Add(new DBItem("handle_item", HandleItemBabyLabor[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("handle_other", HandleItemBabyLabor99[i], DBItem.DBDataType.String));
                            }
                            else
                            {
                                DBItemDataList.Add(new DBItem("range_other", Backout99[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("EXUDATE_COLOR", ExudateColor[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("EXUDATE_NATURE", ExudateNature[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("EXUDATE_COLOR_OTHER", ExudateColor_other[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("EXUDATE_NATURE_OTHER", ExudateNature_other[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("handle_item", HandleItemWound[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("handle_other", HandleItemWound99[i], DBItem.DBDataType.String));
                            }
                            DBItemDataList.Add(new DBItem("EXUDATE_AMOUNT", RbnExudateAmount[i], DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("CREANO", userno, DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                            ERow = this.wound.DBExecInsert("WOUND_RECORD", DBItemDataList);
                            if (ERow > 0)
                            {
                                if (Wound_Img[i] != null)
                                {
                                    wound.DBCmd.CommandText = "UPDATE WOUND_RECORD SET WOUND_IMG_ID = '" + Wound_Img[i].FileName + "', WOUND_IMG_DATA = :WOUND_IMG_DATA "
                                        + " WHERE FEENO = '" + base.ptinfo.FeeNo + "' AND RECORD_ID = '" + RecordID + "' ";
                                    //wound.DBCmd.Parameters.Add(":WOUND_IMG_DATA", SqlDbType.Binary).Value = ConvertFileToByte(Wound_Img[i]);
                                    //wound.DBCmd.Parameters.Add(":WOUND_IMG_DATA", ConvertFileToByte(Wound_Img[i]));
                                    byte[] arr = ConvertFileToByte(Wound_Img[i]);
                                    wound.DBCmd.Parameters.Add(":WOUND_IMG_DATA", OracleDbType.Blob).Value = arr;

                                    wound.DBOpen();
                                    wound.DBCmd.ExecuteNonQuery();
                                    wound.DBClose();
                                    arr = null;
                                }
                                #region  護理紀錄--新增
                                if (PlanYN[i] == "Y")
                                {
                                    try
                                    {
                                        string CareRecordContI = "", CareRecordContE = "", CareRecordTitle = "";
                                        string BedSoreRangeCont = "", SuturesYNCont = "", ExudateNatureColorCont = "", TxtExterior = "", Temp_Wound_Size = "";//
                                        string ObsRangeCont = "", ObsRedCont = "", ObsSwollenCont = "", ObsBruiseCont = "", ObsSecretionCont = "";

                                        TxtExterior = (Exterior_other[i] != "") ? Exterior[i].ToString().Replace('|', ',') + ":" + Exterior_other[i] : Exterior[i].ToString().Replace('|', ',');
                                        if (WoundType[i] == "BedSore")//壓傷
                                        {
                                            CareRecordTitle = "壓傷傷口護理";
                                            if (HandleYN[i] == "是")
                                            {
                                                CareRecordContI = "給予 " + HandleItemBedSore[i].ToString().Replace('|', ',');
                                                if (HandleItemBedSore99[i] != "")
                                                {
                                                    CareRecordContI += ":" + HandleItemBedSore99[i];
                                                }
                                                CareRecordContI += " 等護理處置，並持續觀察傷口狀況。";
                                            }
                                            else
                                            {
                                                CareRecordContI = " 無給予護理措施，持續觀察。";
                                            }
                                            try
                                            {
                                                BedSoreRangeCont = "，等級 " + arrgrade[Convert.ToInt32(BedSoreRange[i].ToString()) - 1];
                                            }
                                            catch (Exception)
                                            {
                                                BedSoreRangeCont = "，等級 " + BedSoreRange[i].ToString();
                                            }
                                            ExudateNatureColorCont = "，性質：" + ExudateNatureBedSore[i].ToString().Replace('|', ',');
                                            if (ExudateNatureBedSore99[i] != "")
                                            {
                                                ExudateNatureColorCont += ":" + ExudateNatureBedSore99[i];
                                            }
                                            //【部位】壓傷傷口大小【長】x【寬】x【深】，等級【壓傷分級】，周圍皮膚【外觀】，傷口滲出液量【量】，性質：【滲出液性質】。
                                            CareRecordContE = PositionVal[i].ToString() + " " + TypeHidVal[i].ToString();   //部位 + 傷口型態
                                            if (RangeYN[i] == "是")
                                            {
                                                CareRecordContE += "傷口大小 " + RangeHeight[i].ToString() + "cm" + "*" + RangeWidth[i].ToString() + "cm";
                                                if (RangeDepth[i].ToString() != "")
                                                {
                                                    CareRecordContE += "*" + RangeDepth[i].ToString() + "cm";
                                                }
                                            }
                                            else
                                            {
                                                CareRecordContE += "傷口 ";//大小不可測量，原因：" + RangeNReason[i];//edit暫不補上，不在格式範圍內
                                            }
                                            CareRecordContE += BedSoreRangeCont + "，周圍皮膚 " + TxtExterior;//SuturesYNCont +
                                            if (ExudateYN[i] != "無")
                                            {
                                                CareRecordContE += "，傷口滲出液量" + RbnExudateAmount[i] + ExudateNatureColorCont;
                                            }
                                            else
                                            {
                                                CareRecordContE += "，無滲液";
                                            }
                                            CareRecordContE += "。";
                                        }
                                        else if (WoundType[i] == "WoundGeneral")
                                        {
                                            if (TypeHidVal[i].ToString() == "燙傷")
                                            {//燙傷
                                                CareRecordTitle = "燙傷傷口護理";
                                                if (HandleYN[i] == "是")
                                                {
                                                    CareRecordContI = "給予 " + HandleItemWound[i].ToString().Replace('|', ',');
                                                    if (HandleItemWound99[i] != "")
                                                    {
                                                        CareRecordContI += ":" + HandleItemWound99[i];
                                                    }
                                                    CareRecordContI += " 等護理處置，並持續觀察傷口狀況。";
                                                }
                                                else
                                                {
                                                    CareRecordContI = " 無給予護理措施，持續觀察。";
                                                }
                                                ExudateNatureColorCont = "，性質：" + ExudateNature[i].ToString().Replace('|', ',');// + "，顏色 " + ExudateColor[i].ToString().Replace('|', ',');
                                                if (ExudateNature_other[i] != "")
                                                {
                                                    ExudateNatureColorCont += ":" + ExudateNature_other[i];
                                                }
                                                //string TempLevel = (WoundLevel[i].ToString() == "0") ? "N/A" : WoundLevel[i].ToString();
                                                string TempLevel = WoundLevel[i].ToString();
                                                BedSoreRangeCont = "，等級 " + StrArr_WoundLevel[Convert.ToInt32(TempLevel)];//StrArr_WoundLevel
                                                //【部位】燙傷範圍【燙傷範圍】，等級【傷口等級】，周圍皮膚【外觀】，傷口滲出液量【量】，性質：【滲出液性質】。
                                                CareRecordContE = PositionVal[i].ToString() + " ";
                                                if (RangeYN[i] != "否")
                                                {
                                                    CareRecordContE += TypeHidVal[i].ToString() + "範圍 " + ScaldRange[i] + "%";
                                                }
                                                else
                                                {
                                                    CareRecordContE += TypeHidVal[i].ToString() + "範圍 不可量測";
                                                }
                                                CareRecordContE += BedSoreRangeCont + "，周圍皮膚 " + TxtExterior;
                                                if (ExudateYN[i] != "無")
                                                {
                                                    CareRecordContE += "，傷口滲出液量" + RbnExudateAmount[i] + ExudateNatureColorCont;
                                                }
                                                else
                                                {
                                                    CareRecordContE += "，無滲液";
                                                }
                                                CareRecordContE += "。";
                                            }
                                            else
                                            {   //一般傷口----不等於燙傷的那些
                                                CareRecordTitle = "傷口護理";
                                                if (HandleYN[i] == "是")
                                                {
                                                    CareRecordContI = "給予 " + HandleItemWound[i].ToString().Replace('|', ',');
                                                    if (HandleItemWound99[i] != "")
                                                    {
                                                        CareRecordContI += ":" + HandleItemWound99[i];
                                                    }
                                                    CareRecordContI += " 等護理處置，並持續觀察傷口狀況。";
                                                }
                                                else
                                                {
                                                    CareRecordContI = " 無給予護理措施，持續觀察。";
                                                }
                                                SuturesYNCont = " 傷口" + SuturesYN[i].ToString();//有無縫線
                                                ExudateNatureColorCont = "，性質：" + ExudateNature[i].ToString().Replace('|', ',');//
                                                if (ExudateNature_other[i] != "")
                                                {
                                                    ExudateNatureColorCont += ":" + ExudateNature_other[i];
                                                }
                                                ExudateNatureColorCont += "，顏色：" + ExudateColor[i].ToString().Replace('|', ',');
                                                if (ExudateColor_other[i] != "")
                                                {
                                                    ExudateNatureColorCont += ":" + ExudateColor_other[i];
                                                }
                                                if (RangeYN[i] != "否")
                                                {
                                                    Temp_Wound_Size = " (大小" + RangeHeight[i].ToString() + "cm" + "*" + RangeWidth[i].ToString() + "cm";
                                                    if (RangeDepth[i].ToString() != "")
                                                    {
                                                        Temp_Wound_Size += "*" + RangeDepth[i].ToString() + "cm";
                                                    }
                                                    Temp_Wound_Size += ")";
                                                }
                                                else
                                                {
                                                    if (BackoutYN[i] == "傷口敷料暫不拆包")
                                                    {
                                                        Temp_Wound_Size = "(傷口敷料暫不拆包)";
                                                    }
                                                    else
                                                    {//如果有其他: <-----  加在這
                                                        //BackoutYNCont = ")";
                                                    }
                                                }
                                                //【部位】【傷口類別】大小(【長】x【寬】x【深】/手術傷口暫不拆包)，傷口【是否有縫線】，周圍皮膚【外觀】，傷口滲出液量【量】，性質【滲出液性質】，顏色【顏色】。
                                                CareRecordContE = PositionVal[i].ToString() + " " + TypeHidVal[i].ToString() + "傷口";
                                                CareRecordContE += Temp_Wound_Size + SuturesYNCont + "，周圍皮膚 " + TxtExterior;
                                                if (ExudateYN[i] != "無")
                                                {
                                                    // CareRecordContE += "，傷口滲出液，" + ExudateNatureColorCont;
                                                    CareRecordContE += "，傷口滲出液量" + RbnExudateAmount[i] + ExudateNatureColorCont;
                                                }
                                                else
                                                {
                                                    CareRecordContE += "，無滲液";
                                                }
                                                CareRecordContE += "。";
                                            }
                                        }
                                        else if (WoundType[i] == "BabyLabor")
                                        {
                                            CareRecordTitle = "會陰傷口護理";
                                            if (HandleYN[i] == "是")
                                            {
                                                CareRecordContI = "給予 " + HandleItemBabyLabor[i].ToString().Replace('|', ',');
                                                if (HandleItemBabyLabor99[i] != "")
                                                {
                                                    CareRecordContI += ":" + HandleItemBabyLabor99[i];
                                                }
                                                CareRecordContI += " 等護理處置，並持續觀察傷口狀況。";
                                            }
                                            else
                                            {
                                                CareRecordContI = " 無給予護理措施，持續觀察。";
                                            }
                                            ObsRangeCont = " 傷口邊緣" + ObsRange[i].ToString();//有無縫線
                                            ObsRedCont = "，紅：" + ObsRed[i].ToString();//
                                            ObsSwollenCont = "，水腫：" + ObsSwollen[i].ToString();
                                            if (ObsSwollen_range[i] != "")
                                            {
                                                ObsSwollenCont += "，範圍:" + ObsSwollen_range[i];
                                            }
                                            ObsBruiseCont = "，瘀血：" + ObsBruise[i].ToString();
                                            ObsSecretionCont = "，分泌物：" + ObsSecretion[i].ToString();

                                            CareRecordContE = PositionVal[i].ToString() + " " + TypeHidVal[i].ToString() + "傷口";
                                            CareRecordContE += ObsRangeCont + ObsRedCont + ObsSwollenCont + ObsBruiseCont + ObsSecretionCont;
                                            CareRecordContE += "。";
                                        }
                                        base.Insert_CareRecord(wound_date[i] + " " + wound_time[i] + ":59", RecordID, CareRecordTitle, "", "", "", CareRecordContI, CareRecordContE, ID[i]);
                                    }
                                    catch (Exception ex)
                                    {
                                        //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                                        string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                                        string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                                        write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                                    }
                                }
                                #endregion


                                //計價 (根據燙傷範圍計價)
                                string rangeValue = ScaldRange[i];
                                if(rangeValue != "" && rangeValue != null)
                                {
                                    List<Bill_RECORD> billDataList = new List<Bill_RECORD>();
                                    Bill_RECORD billData = new Bill_RECORD();
                                    int range = int.Parse(ScaldRange[i]);

                                    if (range < 11)
                                    {
                                        billData.HO_ID = "8448018";
                                        billData.COUNT = "1";
                                    }
                                    else if (range >= 11 && range <= 35)
                                    {
                                        billData.HO_ID = "8448019";
                                        billData.COUNT = "1";

                                    }
                                    else if (range >= 36 && range <= 50)
                                    {
                                        billData.HO_ID = "8448020";
                                        billData.COUNT = "1";

                                    }
                                    else if (range >= 51 && range <= 70)
                                    {
                                        billData.HO_ID = "8448021";
                                        billData.COUNT = "1";

                                    }
                                    else if (range >= 71 && range <= 90)
                                    {
                                        billData.HO_ID = "8448031";
                                        billData.COUNT = "1";

                                    }
                                    else if (range > 90)
                                    {
                                        billData.HO_ID = "8448032";
                                        billData.COUNT = "1";

                                    }
                                    billDataList.Add(billData);

                                    SaveBillingRecord(billDataList);
                                }

                                //計價 燙傷外計算長度
                                string rangeHeightValue = RangeHeight[i];
                                string positionType = PositionTypeVal[i];
                                if (rangeHeightValue != "" && rangeHeightValue != null)
                                {
                                    List<Bill_RECORD> billDataList = woundCount(positionType, rangeHeightValue);
                                 
                                    SaveBillingRecordWound(billDataList, rangeHeightValue, positionType);
                                }
                            }
                        }
                        else
                        {
                            //修改
                            RecordID = form["RecordID_" + ROWVal[i]];

                            DBItemDataList.Add(new DBItem("range_YN", RangeYN[i], DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("backout_YN", BackoutYN[i], DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("exudate_YN", ExudateYN[i], DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("sutures_YN", SuturesYN[i], DBItem.DBDataType.String));

                            DBItemDataList.Add(new DBItem("EXTERIOR_OTHER", Exterior_other[i], DBItem.DBDataType.String));

                            DBItemDataList.Add(new DBItem("EXTERIOR", Exterior[i], DBItem.DBDataType.String));

                            DBItemDataList.Add(new DBItem("stitchesDate", StitchesDate[i], DBItem.DBDataType.String));
                            if (SuturesYN[i] == "有縫線")
                                DBItemDataList.Add(new DBItem("pin", SuturesYOther[i], DBItem.DBDataType.String));
                            else if (SuturesYN[i] == "縫線已拆")
                                DBItemDataList.Add(new DBItem("pin", PinVal[i], DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("handle_YN", HandleYN[i], DBItem.DBDataType.String));
                            if (TypeHidVal[i] == "生產") //Obs
                            {
                                DBItemDataList.Add(new DBItem("PERINEUM_RANGE", ObsRange[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("PERINEUM_RED", ObsRed[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("PERINEUM_SWOLLEN", ObsSwollen[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("SWOLLEN_RANGE", ObsSwollen_range[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("PERINEUM_BRUISE", ObsBruise[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("PERINEUM_SECRETION", ObsSecretion[i], DBItem.DBDataType.String));
                            }
                            else if (TypeHidVal[0] != "燙傷")
                            {
                                DBItemDataList.Add(new DBItem("RANGE_WIDTH", RangeWidth[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("RANGE_HEIGHT", RangeHeight[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("RANGE_DEPTH", RangeDepth[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("grade", BedSoreRange[i], DBItem.DBDataType.String));
                            }
                            else
                            {
                                DBItemDataList.Add(new DBItem("WOUND_LEVEL", WoundLevel[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("SCALD_RANGE", ScaldRange[i], DBItem.DBDataType.String));
                            }
                            if (WoundType[i] == "BedSore")
                            {
                                DBItemDataList.Add(new DBItem("range_other", RangeNReason[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("EXUDATE_NATURE", ExudateNatureBedSore[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("EXUDATE_NATURE_OTHER", ExudateNatureBedSore99[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("handle_item", HandleItemBedSore[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("handle_other", HandleItemBedSore99[i], DBItem.DBDataType.String));
                            }
                            else if (WoundType[i] == "BabyLabor")
                            {
                                DBItemDataList.Add(new DBItem("handle_item", HandleItemBabyLabor[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("handle_other", HandleItemBabyLabor99[i], DBItem.DBDataType.String));
                            }
                            else
                            {
                                DBItemDataList.Add(new DBItem("range_other", Backout99[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("EXUDATE_COLOR", ExudateColor[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("EXUDATE_NATURE", ExudateNature[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("EXUDATE_COLOR_OTHER", ExudateColor_other[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("EXUDATE_NATURE_OTHER", ExudateNature_other[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("handle_item", HandleItemWound[i], DBItem.DBDataType.String));
                                DBItemDataList.Add(new DBItem("handle_other", HandleItemWound99[i], DBItem.DBDataType.String));
                            }
                            DBItemDataList.Add(new DBItem("EXUDATE_AMOUNT", RbnExudateAmount[i], DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));

                            ERow = wound.DBExecUpdate("WOUND_RECORD", DBItemDataList, string.Format("FEENO={0} AND RECORD_ID={1}", DigitFlapTemperature.SQLString(feeno), DigitFlapTemperature.SQLString(RecordID)));
                            if (ERow > 0)
                            {
                                if (Wound_Img[i] != null)
                                {
                                    wound.DBCmd.CommandText = "UPDATE WOUND_RECORD SET WOUND_IMG_ID = '" + Wound_Img[i].FileName + "', WOUND_IMG_DATA = :WOUND_IMG_DATA "
                                        + " WHERE FEENO = '" + base.ptinfo.FeeNo + "' AND RECORD_ID = '" + RecordID + "' ";
                                    //wound.DBCmd.Parameters.Add(":WOUND_IMG_DATA", SqlDbType.Binary).Value = ConvertFileToByte(Wound_Img[i]);
                                    //wound.DBCmd.Parameters.Add(":WOUND_IMG_DATA", ConvertFileToByte(Wound_Img[i]));
                                    byte[] arr = ConvertFileToByte(Wound_Img[i]);
                                    wound.DBCmd.Parameters.Add(":WOUND_IMG_DATA", OracleDbType.Blob).Value = arr;

                                    wound.DBOpen();
                                    wound.DBCmd.ExecuteNonQuery();
                                    wound.DBClose();
                                    arr = null;
                                }
                                #region  護理紀錄--修改
                                if (PlanYN[i] == "")
                                {
                                    try
                                    {
                                        string CareRecordContI = "", CareRecordContE = "", CareRecordTitle = "";
                                        string BedSoreRangeCont = "", SuturesYNCont = "", ExudateNatureColorCont = "", TxtExterior = "", Temp_Wound_Size = "";//
                                        string ObsRangeCont = "", ObsRedCont = "", ObsSwollenCont = "", ObsBruiseCont = "", ObsSecretionCont = "";
                                        TxtExterior = (Exterior_other[i] != "") ? Exterior[i].ToString().Replace('|', ',') + ":" + Exterior_other[i] : Exterior[i].ToString().Replace('|', ',');
                                        if (WoundType[i] == "BedSore")//壓傷
                                        {
                                            CareRecordTitle = "壓傷傷口護理";
                                            if (HandleYN[i] == "是")
                                            {
                                                CareRecordContI = "給予 " + HandleItemBedSore[i].ToString().Replace('|', ',');
                                                if (HandleItemBedSore99[i] != "")
                                                {
                                                    CareRecordContI += ":" + HandleItemBedSore99[i];
                                                }
                                                CareRecordContI += " 等護理處置，並持續觀察傷口狀況。";
                                            }
                                            else
                                            {
                                                CareRecordContI = " 無給予護理措施，持續觀察。";
                                            }
                                            try
                                            {
                                                BedSoreRangeCont = "，等級 " + arrgrade[Convert.ToInt32(BedSoreRange[i].ToString()) - 1];
                                            }
                                            catch (Exception)
                                            {
                                                BedSoreRangeCont = "，等級 " + BedSoreRange[i].ToString();
                                            }
                                            ExudateNatureColorCont = "，性質：" + ExudateNatureBedSore[i].ToString().Replace('|', ',');
                                            if (ExudateNatureBedSore99[i] != "")
                                            {
                                                ExudateNatureColorCont += ":" + ExudateNatureBedSore99[i];
                                            }
                                            //【部位】壓傷傷口大小【長】x【寬】x【深】，等級【壓傷分級】，周圍皮膚【外觀】，傷口滲出液量【量】，性質：【滲出液性質】。
                                            CareRecordContE = PositionVal[i].ToString() + " " + TypeHidVal[i].ToString();   //部位 + 傷口型態
                                            if (RangeYN[i] == "是")
                                            {
                                                CareRecordContE += "傷口大小 " + RangeHeight[i].ToString() + "cm" + "*" + RangeWidth[i].ToString() + "cm";
                                                if (RangeDepth[i].ToString() != "")
                                                {
                                                    CareRecordContE += "*" + RangeDepth[i].ToString() + "cm";
                                                }
                                            }
                                            else
                                            {
                                                CareRecordContE += "傷口 ";//大小不可測量，原因：" + RangeNReason[i];//edit暫不補上，不在格式範圍內
                                            }
                                            CareRecordContE += BedSoreRangeCont + "，周圍皮膚 " + TxtExterior;//SuturesYNCont +
                                            if (ExudateYN[i] != "無")
                                            {
                                                CareRecordContE += "，傷口滲出液量" + RbnExudateAmount[i] + ExudateNatureColorCont;
                                            }
                                            else
                                            {
                                                CareRecordContE += "，無滲液";
                                            }
                                            CareRecordContE += "。";
                                        }
                                        else if (WoundType[i] == "WoundGeneral")
                                        {
                                            if (TypeHidVal[i].ToString() == "燙傷")
                                            {//燙傷
                                                CareRecordTitle = "燙傷傷口護理";
                                                if (HandleYN[i] == "是")
                                                {
                                                    CareRecordContI = "給予 " + HandleItemWound[i].ToString().Replace('|', ',');
                                                    if (HandleItemWound99[i] != "")
                                                    {
                                                        CareRecordContI += ":" + HandleItemWound99[i];
                                                    }
                                                    CareRecordContI += " 等護理處置，並持續觀察傷口狀況。";
                                                }
                                                else
                                                {
                                                    CareRecordContI = " 無給予護理措施，持續觀察。";
                                                }
                                                ExudateNatureColorCont = "，性質：" + ExudateNature[i].ToString().Replace('|', ',');// + "，顏色 " + ExudateColor[i].ToString().Replace('|', ',');
                                                if (ExudateNature_other[i] != "")
                                                {
                                                    ExudateNatureColorCont += ":" + ExudateNature_other[i];
                                                }
                                                //string TempLevel = (WoundLevel[i].ToString() == "0") ? "N/A" : WoundLevel[i].ToString();
                                                string TempLevel = WoundLevel[i].ToString();
                                                BedSoreRangeCont = "，等級 " + StrArr_WoundLevel[Convert.ToInt32(TempLevel)];//StrArr_WoundLevel
                                                //【部位】燙傷範圍【燙傷範圍】，等級【傷口等級】，周圍皮膚【外觀】，傷口滲出液量【量】，性質：【滲出液性質】。
                                                CareRecordContE = PositionVal[i].ToString() + " ";
                                                if (RangeYN[i] != "否")
                                                {
                                                    CareRecordContE += TypeHidVal[i].ToString() + "範圍 " + ScaldRange[i] + "%";
                                                }
                                                else
                                                {
                                                    CareRecordContE += TypeHidVal[i].ToString() + "範圍 不可量測";
                                                }
                                                CareRecordContE += BedSoreRangeCont + "，周圍皮膚 " + TxtExterior;
                                                if (ExudateYN[i] != "無")
                                                {
                                                    CareRecordContE += "，傷口滲出液量" + RbnExudateAmount[i] + ExudateNatureColorCont;
                                                }
                                                else
                                                {
                                                    CareRecordContE += "，無滲液";
                                                }
                                                CareRecordContE += "。";
                                            }
                                            else
                                            {   //一般傷口----不等於燙傷的那些
                                                CareRecordTitle = "傷口護理";
                                                if (HandleYN[i] == "是")
                                                {
                                                    CareRecordContI = "給予 " + HandleItemWound[i].ToString().Replace('|', ',');
                                                    if (HandleItemWound99[i] != "")
                                                    {
                                                        CareRecordContI += ":" + HandleItemWound99[i];
                                                    }
                                                    CareRecordContI += " 等護理處置，並持續觀察傷口狀況。";
                                                }
                                                else
                                                {
                                                    CareRecordContI = " 無給予護理措施，持續觀察。";
                                                }
                                                SuturesYNCont = " 傷口" + SuturesYN[i].ToString();//有無縫線
                                                ExudateNatureColorCont = "，性質：" + ExudateNature[i].ToString().Replace('|', ',');//
                                                if (ExudateNature_other[i] != "")
                                                {
                                                    ExudateNatureColorCont += ":" + ExudateNature_other[i];
                                                }
                                                ExudateNatureColorCont += "，顏色：" + ExudateColor[i].ToString().Replace('|', ',');
                                                if (ExudateColor_other[i] != "")
                                                {
                                                    ExudateNatureColorCont += ":" + ExudateColor_other[i];
                                                }
                                                if (RangeYN[i] != "否")
                                                {
                                                    Temp_Wound_Size = " (大小" + RangeHeight[i].ToString() + "cm" + "*" + RangeWidth[i].ToString() + "cm";
                                                    if (RangeDepth[i].ToString() != "")
                                                    {
                                                        Temp_Wound_Size += "*" + RangeDepth[i].ToString() + "cm";
                                                    }
                                                    Temp_Wound_Size += ")";
                                                }
                                                else
                                                {
                                                    if (BackoutYN[i] == "傷口敷料暫不拆包")
                                                    {
                                                        Temp_Wound_Size = "(傷口敷料暫不拆包)";
                                                    }
                                                    else
                                                    {//如果有其他: <-----  加在這
                                                        //BackoutYNCont = ")";
                                                    }
                                                }
                                                //【部位】【傷口類別】大小(【長】x【寬】x【深】/手術傷口暫不拆包)，傷口【是否有縫線】，周圍皮膚【外觀】，傷口滲出液量【量】，性質【滲出液性質】，顏色【顏色】。
                                                CareRecordContE = PositionVal[i].ToString() + " " + TypeHidVal[i].ToString() + "傷口";
                                                CareRecordContE += Temp_Wound_Size + SuturesYNCont + "，周圍皮膚 " + TxtExterior;
                                                if (ExudateYN[i] != "無")
                                                {
                                                    // CareRecordContE += "，傷口滲出液，" + ExudateNatureColorCont;
                                                    CareRecordContE += "，傷口滲出液量" + RbnExudateAmount[i] + ExudateNatureColorCont;
                                                }
                                                else
                                                {
                                                    CareRecordContE += "，無滲液";
                                                }
                                                CareRecordContE += "。";
                                            }
                                        }
                                        else if (WoundType[i] == "BabyLabor")
                                        {
                                            CareRecordTitle = "會陰傷口護理";
                                            if (HandleYN[i] == "是")
                                            {
                                                CareRecordContI = "給予 " + HandleItemBabyLabor[i].ToString().Replace('|', ',');
                                                if (HandleItemBabyLabor99[i] != "")
                                                {
                                                    CareRecordContI += ":" + HandleItemBabyLabor99[i];
                                                }
                                                CareRecordContI += " 等護理處置，並持續觀察傷口狀況。";
                                            }
                                            else
                                            {
                                                CareRecordContI = " 無給予護理措施，持續觀察。";
                                            }
                                            ObsRangeCont = " 傷口邊緣" + ObsRange[i].ToString();//有無縫線
                                            ObsRedCont = "，紅：" + ObsRed[i].ToString();//
                                            ObsSwollenCont = "，水腫：" + ObsSwollen[i].ToString();
                                            if (ObsSwollen_range[i] != "")
                                            {
                                                ObsSwollenCont += "，範圍:" + ObsSwollen_range[i];
                                            }
                                            ObsBruiseCont = "，瘀血：" + ObsBruise[i].ToString();
                                            ObsSecretionCont = "，分泌物：" + ObsSecretion[i].ToString();

                                            CareRecordContE = PositionVal[i].ToString() + " " + TypeHidVal[i].ToString() + "傷口";
                                            CareRecordContE += ObsRangeCont + ObsRedCont + ObsSwollenCont + ObsBruiseCont + ObsSecretionCont;
                                            CareRecordContE += "。";
                                        }
                                        base.Upd_CareRecord(wound_date[i] + " " + wound_time[i] + ":59", RecordID, CareRecordTitle, "", "", "", CareRecordContI, CareRecordContE, ID[i]);
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
                            }
                        }
                    }
                }


                if (form["HidModeVal"] == "Update_Record")
                {
                    if (ERow > 0)
                        return "<script>alert('儲存成功');window.opener.location.href = 'listTrauma';window.close();</script>";
                    else
                        return "<script>alert('儲存失敗');window.close();</script>";
                }
                else
                {
                    if (ERow > 0)
                        return "<script>alert('新增成功');if (confirm('新增成功!是否前往衛教頁面?')) { window.opener.location.href = '../Education/Sel_Item?mode=edit&key=傷口'; } else { window.opener.location.href = 'listTrauma'; };window.close();</script>";
                    else
                        return "<script>alert('新增失敗');window.close();</script>";
                }
            }

            return "<script>alert('請重新選擇病患');window.opener.location.href = 'listTrauma';</script>";
        }

        public List<Bill_RECORD> woundCount (string postion , string value)
        {
            List<DBItem> insertDataList = new List<DBItem>();
            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;
            string start = DateTime.Now.ToString("yyyy/MM/dd HH:00:00");
            string end = DateTime.Now.AddHours(1).ToString("yyyy/MM/dd HH:00:00");
            string sql = "SELECT * FROM NIS_WOUND_LOG WHERE FEENO = '" + feeno + "' AND CREATE_DATE BETWEEN TO_DATE('" + start + "' , 'yyyy/MM/dd HH24:mi:ss') AND TO_DATE('" + end + "' , 'yyyy/MM/dd HH24:mi:ss') AND STATUS = 'Y' AND WOUND_POSITION = '"+ postion + "' ";
            DataTable dt = new DataTable();
            link.DBExecSQL(sql, ref dt);

            List<string> lengthArr = new List<string>();
            List<string> billArr = new List<string>();

            if (dt.Rows.Count > 0)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    lengthArr.Add(dr["WOUND_VALUE"].ToString());
                    billArr.Add("'" + dr["BILL_ID"].ToString() + "'");
                }
            }

            int lengthRecord = 0;
            int length = 0;

            if (lengthArr.Count > 0)
            {
                foreach (var item in lengthArr)
                {
                    lengthRecord += int.Parse(item);
                }
            }
            //刪除該小時已拋紀錄

            if(billArr.Count() > 0)
            {
                string billRecord = String.Join(",", billArr);
                insertDataList.Clear();
                insertDataList.Add(new DBItem("STATUS", "D", DBItem.DBDataType.String));
                link.DBExecUpdate("DATA_BILLING_TEMP_DETAIL", insertDataList, " SERIAL_D IN (" + billRecord + ")");
            }



            if (value != "")
            {
                length = int.Parse(value);
            }

            int height = lengthRecord + length;

            List<Bill_RECORD> billDataList = new List<Bill_RECORD>();
            Bill_RECORD billData = new Bill_RECORD();

            if (height > 0 && height < 10)
            {
                billData.HO_ID = "8448011";
                billData.COUNT = "1";

            }
            else if (height >= 10 && height <= 20)
            {
                billData.HO_ID = "8448012";
                billData.COUNT = "1";

            }
            else if (height > 20)
            {
                billData.HO_ID = "8448013";
                billData.COUNT = "1";

            }
            billDataList.Add(billData);

            return billDataList;
        }

        public Image Base64ToImage(string base64String)
        {
            // Convert base 64 string to byte[]
            byte[] imageBytes = Convert.FromBase64String(base64String);
            Image image = null;
            // Convert byte[] to Image
            using (var ms = new MemoryStream(imageBytes, 0, imageBytes.Length))
            {
                image = Image.FromStream(ms, true);
            }
            return image;
        }

        public byte[] ConvertFileToByte(HttpPostedFileBase inputFile)
        {
            byte[] result = null;
            using (MemoryStream target = new MemoryStream())
            {
                inputFile.InputStream.CopyTo(target);
                result = target.ToArray();
            }
            return result;
            //return Convert.ToBase64String(target.ToArray());
        }

        [HttpGet]
        public ActionResult Delete_Record(string id)
        {
            string feeno = ptinfo.FeeNo;
            string userno = userinfo.EmployeesNo;

            #region 宣告Table
            DataTable dt = new DataTable();
            string[] column = { "UPDNO", "UPDTIME", "DELETED", "WHERE" };
            string[] datatype = { "String", "DataTime", "String", "NONE" };
            set_dt_column(dt, column);
            set_dt_datatype(dt, datatype);

            DataRow dt_row = dt.NewRow();
            dt_row["UPDNO"] = userno;
            dt_row["UPDTIME"] = DateTime.Now.ToString("yyyy/MM/dd HH:mm");
            dt_row["DELETED"] = "Y";
            dt_row["WHERE"] = " FEENO = '" + feeno + "' AND RECORD_ID = '" + id + "' ";
            dt.Rows.Add(dt_row);
            #endregion

            int erow = wound.upd("WOUND_RECORD", dt);

            if (erow > 0)
            {
                erow = base.Del_CareRecord(id, "");
                Response.Write("<script>alert('刪除成功');window.opener.location.href='listTrauma';window.close();</script>");
            }
            else
                Response.Write("<script>alert('刪除失敗');window.close();</script>");

            return new EmptyResult();
        }

        #endregion

        #region 結案

        public ActionResult SetTraumaStatusPrompt(string IDList)
        {
            if (Session["PatInfo"] != null)
            {
                ViewBag.dt = wound.sel_wound_data(base.ptinfo.FeeNo, IDList);
                return View();
            }

            Response.Write("<script>alert('請重新選擇病患');window.close();</script>");
            return new EmptyResult();
        }

        [HttpPost]
        public string SaveSetTraumaStatusPrompt(string DataList)
        {
            bool Success = false;
            if (Session["PatInfo"] != null)
            {
                try
                {
                    List<Dictionary<string, string>> Dt = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(DataList);
                    List<DBItem> dbItem = null;
                    string content = string.Empty;
                    foreach (var item in Dt)
                    {
                        dbItem = new List<DBItem>();
                        dbItem.Add(new DBItem("ENDTIME", item["EndDateTime"], DBItem.DBDataType.DataTime));
                        dbItem.Add(new DBItem("ENDT_REASON", item["EndReason"], DBItem.DBDataType.String));
                        if (this.link.DBExecUpdate("WOUND_DATA", dbItem, "FEENO = '" + base.ptinfo.FeeNo + "' AND WOUND_ID = '" + item["WoundID"] + "' ") > 0)
                        {
                            try
                            {
                                Success = true;
                                //於【發生日期】發生之【部位】【傷口類別】因【結案原因】予以結案。
                                if (!string.IsNullOrWhiteSpace(item["CreateTime"]))
                                    content = "於" + Convert.ToDateTime(item["CreateTime"]).ToString("yyyy/MM/dd") + "發生之" + item["WoundPosition"] + item["WoundType"] + "因" + item["EndReason"] + "予以結案。";
                                //【部位】之【傷口類別】傷口因【結案原因】予以結案。
                                else
                                    content = item["WoundPosition"] + "之" + item["WoundType"] + "傷口因" + item["EndReason"] + "予以結案。";
                                base.Insert_CareRecord(item["EndDateTime"], item["WoundID"] + "_End", "", "", "", "", "", content, item["WoundID"]);
                            }
                            catch { }
                        }
                        else
                            Success = false;
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

            if (Success)
                return "Y";
            else
                return "N";
        }

        #endregion

        #region  列印

        public ActionResult Record_List_PDF(string LoginEmpNo, string feeno)
        {
            string userno = "";  //這裡無法直接使用 userinfo ，會造成無法抓取的情況，故用參數值
            //病人資訊
            PatientInfo pinfo = new PatientInfo();
            byte[] ByteCode = webService.GetPatientInfo(feeno);
            if (ByteCode != null)
                pinfo = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(ByteCode));
            ViewData["ptinfo"] = pinfo;

            DataTable DT = wound.sel_wound_record_data(feeno, "", "all");

            //非登入者之員工資料 經由WS取得
            DT.Columns.Add("username");
            if (DT.Rows.Count > 0)
            {
                foreach (DataRow r in DT.Rows)
                {
                    userno = LoginEmpNo;  //登入者
                    if (userno != r["CREANO"].ToString())
                    {//登入者和紀錄者資料不相同時，訂正 userno
                        userno = r["CREANO"].ToString();
                    }

                    //因為 userinfo 無法使用，所以不論相不相同，都要重抓名字
                    byte[] listByteCode = webService.UserName(userno);
                    if (listByteCode != null)
                    {
                        string listJsonArray = CompressTool.DecompressString(listByteCode);
                        UserInfo other_user_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                        r["username"] = other_user_info.EmployeesName;
                    }
                }
            }
            ViewBag.dt_wound_data = wound.sel_wound_data(feeno, "");
            ViewBag.dt_wound_record = DT;

            return View();
        }

        public ActionResult Record(string id)
        {
            string feeno = ptinfo.FeeNo;
            string userno = userinfo.EmployeesNo;
            DataTable dt = wound.sel_wound_record(userno, feeno, "", "");
            foreach (DataRow r in dt.Rows)
                r["CREANO"] = userinfo.EmployeesName;
            ViewBag.dt_wound_record = dt;
            ViewBag.id = id;
            return View();

        }

        #endregion  列印 end

        #region other_function

        /// <summary>
        /// 設定table的colum
        /// </summary>
        /// <param name="dt">資料表</param>
        /// <param name="clumn">欄位_陣列</param>
        protected DataTable set_dt_column(DataTable dt, string[] clumn)
        {
            for (int i = 0; i < clumn.Length; i++)
            {
                dt.Columns.Add(clumn[i]);
            }
            return dt;
        }

        /// <summary>
        /// 設定欄位的型態
        /// </summary>
        /// <param name="dt">資料表</param>
        /// <param name="datatype">型態_陣列</param>
        protected DataTable set_dt_datatype(DataTable dt, string[] datatype)
        {
            DataRow row_type = dt.NewRow();
            for (int i = 0; i < dt.Columns.Count; i++)
                row_type[i] = datatype[i];

            dt.Rows.Add(row_type);

            return dt;
        }

        #endregion

    }
}
