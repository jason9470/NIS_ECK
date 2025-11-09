using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Data;
using NIS.Models;
using Newtonsoft.Json;
using System.Web.WebPages;
using Com.Mayaminer;
using NIS.UtilTool;
using NIS.Data;
using DocumentFormat.OpenXml.Office2010.Excel;

namespace NIS.Controllers
{
    public class TubeManageController : BaseController
    {
        private CommData cd;    //常用資料Module
        private DBConnector link;
        private TubeManager tubem;
        private const string CRLF = "\n";

        public TubeManageController()
        {
            this.cd = new CommData();
            this.link = new DBConnector();
            this.tubem = new TubeManager();
        }

        //管路管理首頁
        public ActionResult Index()
        {
            //判斷有無病人session
            if (Session["PatInfo"] != null)
            {
                //下拉式選單
                ViewData["tubeNumber"] = this.cd.getSelectItem("tube", "tubeNumber");
                ViewData["tubeMaterial"] = this.cd.getSelectItem("tube", "tubeMaterial");
                ViewData["tubeLengthUnit"] = this.cd.getSelectItem("tube", "tubeLengthUnit");
                ViewData["tubePosition"] = this.cd.getSelectItem("tube", "tubePosition");
                ViewData["color_drainage"] = this.cd.getSelectItem("io", "outputcolor_Drainage");
                ViewData["taste_drainage"] = this.cd.getSelectItem("io", "outputtaste_Drainage");
                ViewData["nature_drainage"] = this.cd.getSelectItem("io", "outputnature_Drainage");

                //管路LIST
                ViewBag.dt_tube = tubem.sel_tube(base.ptinfo.FeeNo, "", "", "", "N");

                //管路種類
                List<Dictionary<string, string>> TubeKind = tubem.sel_tubekind("", "0");
                ViewBag.TubeKindNameArrary = TubeKind.Select(r => r["KINDNAME"]).ToArray(); ;
                ViewBag.TubeKind = TubeKind;

                DataTable MaxNumber = tubem.sel_tube_max_number(base.ptinfo.FeeNo);
                int maxnumber = 0;
                if (MaxNumber != null && MaxNumber.Rows.Count > 0
                    && !string.IsNullOrWhiteSpace(MaxNumber.Rows[0][0].ToString())
                        && int.TryParse(MaxNumber.Rows[0][0].ToString(), out maxnumber))
                    maxnumber += 1;

                ViewBag.dt_tubekind_num = maxnumber;
                ViewBag.MinDate = base.GetMinDate();

                //護理站
                byte[] listByteCode = webService.GetCostCenterList();
                string listJsonArray = CompressTool.DecompressString(listByteCode);
                List<CostCenterList> costlist = JsonConvert.DeserializeObject<List<CostCenterList>>(listJsonArray);
                List<SelectListItem> cCostList = new List<SelectListItem>();
                //cCostList.Add
                cCostList.Add(new SelectListItem()
                {
                    Text = "請選擇",
                    Value = "",
                    Selected = false
                });
                for (int i = 0; i <= costlist.Count - 1; i++)
                {
                    cCostList.Add(new SelectListItem()
                    {
                        Text = costlist[i].CCCDescription,
                        Value = (costlist[i].CCCDescription).Trim(),
                        Selected = false
                    });
                }
                ViewData["costlist"] = cCostList;
            }
            else
            {
                Response.Write("<script>alert('請重新選擇病患');</script>");
                return new EmptyResult();
            }
            ViewBag.userno = userinfo.EmployeesNo;
            return View();
        }
        public ActionResult Index_View(string feeno)
        {
            //管路LIST
            if (feeno != "")
            {
                ViewBag.dt_tube = tubem.sel_tube(feeno, "", "", "", "N");
            }
            return View();
        }

        #region 新增、修改、刪除

        [HttpPost]
        [ValidateInput(false)]
        public string Insert(FormCollection form)
        {
            string Msg = string.Empty;
            try
            {
                if (Session["PatInfo"] != null)
                {
                    bool success = true;

                    string ID = base.creatid("TUBE", base.userinfo.EmployeesNo, base.ptinfo.FeeNo, "0");
                    string typeID = form["tube_kind_id"];
                    string date = (string.IsNullOrWhiteSpace(form["NoTime"])) ?
                        Convert.ToDateTime(form["tube_start_date"] + " " + form["tube_start_time"]).ToString("yyyy/MM/dd HH:mm") : string.Empty;
                    List<DBItem> dbItemList;

                    #region 儲存資料

                    if (typeID == "")
                    {
                        typeID = ID;
                        //其他自定義管路名稱
                        dbItemList = new List<DBItem>();
                        dbItemList.Add(new DBItem("KINDID", typeID, DBItem.DBDataType.String));
                        dbItemList.Add(new DBItem("KINDNAME", form["Prompt_TubeKind"], DBItem.DBDataType.String));
                        dbItemList.Add(new DBItem("TUBE_GROUP", form["group"], DBItem.DBDataType.String));
                        dbItemList.Add(new DBItem("BUNDLE_CARE", "0", DBItem.DBDataType.String));
                        dbItemList.Add(new DBItem("ASSESS_TYPE", "0", DBItem.DBDataType.String));
                        dbItemList.Add(new DBItem("OTHER", "1", DBItem.DBDataType.String));
                        if (this.link.DBExecInsertTns("TUBE_KIND", dbItemList) > 0)
                            success = true;
                        else
                            success = false;
                    }

                    if (success)
                    {
                        dbItemList = new List<DBItem>();
                        dbItemList.Add(new DBItem("TUBEROW", "TUBE_SEQUENCE.NEXTVAL", DBItem.DBDataType.Number));
                        dbItemList.Add(new DBItem("TUBEID", ID, DBItem.DBDataType.String));
                        dbItemList.Add(new DBItem("FEENO", base.ptinfo.FeeNo, DBItem.DBDataType.String));
                        dbItemList.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                        dbItemList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                        dbItemList.Add(new DBItem("CREATNO", base.userinfo.EmployeesNo, DBItem.DBDataType.String));
                        dbItemList.Add(new DBItem("UPDNO", base.userinfo.EmployeesNo, DBItem.DBDataType.String));
                        dbItemList.Add(new DBItem("STARTTIME", date, DBItem.DBDataType.DataTime));
                        dbItemList.Add(new DBItem("ENDTIME", "", DBItem.DBDataType.DataTime));
                        if (form["tube_forecast_time"] != "")
                            dbItemList.Add(new DBItem("FORECAST", Convert.ToDateTime(form["tube_forecast_time"]).ToString("yyyy/MM/dd 00:00"), DBItem.DBDataType.DataTime));
                        else
                            dbItemList.Add(new DBItem("FORECAST", "", DBItem.DBDataType.DataTime));
                        dbItemList.Add(new DBItem("TYPEID", typeID, DBItem.DBDataType.String));
                        dbItemList.Add(new DBItem("POSITION", form["txt_position"], DBItem.DBDataType.String));
                        dbItemList.Add(new DBItem("LOCATION", form["tubePosition"], DBItem.DBDataType.String));
                        dbItemList.Add(new DBItem("MODEL", form["param_tube_model"], DBItem.DBDataType.String));
                        dbItemList.Add(new DBItem("LENGTH", form["param_tube_length"], DBItem.DBDataType.String));
                        dbItemList.Add(new DBItem("LENGTHUNIT", form["tubeLengthUnit"], DBItem.DBDataType.String));
                        dbItemList.Add(new DBItem("AMOUNT", form["txt_amount"], DBItem.DBDataType.String));
                        dbItemList.Add(new DBItem("CARERECORD", form["nur_text"], DBItem.DBDataType.String));
                        dbItemList.Add(new DBItem("REMARK", (form["tube_put_back"]) ?? "", DBItem.DBDataType.String));
                        var placeStationType = form["place_station"];
                        var placeStationValue = string.Empty;
                        var placeStationName = string.Empty;
                        if (placeStationType == "院內")
                        {

                            placeStationValue = form["costlist"];
                        }
                        else if (placeStationType == "院外")
                        {

                            if (form["outHosp"] != "0" || form["outHosp"] != "家中")
                            {
                                placeStationValue = form["outHosp"] + "|" + form["outHosp_other"];
                            }
                            else
                            {
                                placeStationValue = form["outHosp"];
                            }
                        }
                        dbItemList.Add(new DBItem("PLACE_STATION", placeStationType + "|" + placeStationValue, DBItem.DBDataType.String));
                        //dbItemList.Add(new DBItem("PLACE_STATION", placeStation, DBItem.DBDataType.String));
                        if (form["bundle_uti_reason"] != null)
                        {
                            dbItemList.Add(new DBItem("PLACE_REASON", form["bundle_uti_reason"] + "|" + form["bundle_uti_reason_other"] + "|" + form["bundle_uti_hand_hygiene"] + "|" + form["bundle_uti_Aseptic"] + "|" + form["bundle_uti_Urinary_catheter"] + "|" + form["bundle_uti_urine_collection"] + "|" + form["bundle_uti_Avoid_creasing"] + "|" + form["bundle_uti_Capacity"] + "|" + form["bundle_uti_hand_hygiene_after"], DBItem.DBDataType.String));
                            var utiPlace = "";
                            var reason = "";

                            if (form["bundle_uti_reason"] != "" && form["bundle_uti_reason_other"] != "")
                            {
                                reason = form["bundle_uti_reason"].Replace("其他", form["bundle_uti_reason_other"]);
                            }
                            else
                            {
                                reason = form["bundle_uti_reason"];
                            }

                            utiPlace += "因為" + reason + "等因素，依醫囑放置" + form["Prompt_TubeKind"] + "。";
                            utiPlace += "放置時進行以下評估：";
                            utiPlace += "置放導尿管前執行手部衛生：" + form["bundle_uti_hand_hygiene"] + "。";
                            utiPlace += "無菌技術操作：" + form["bundle_uti_Aseptic"] + "。";
                            utiPlace += "導尿管固定於：" + form["bundle_uti_Urinary_catheter"] + "。";
                            utiPlace += "集尿袋應維持在膀胱以下位置，末置於地面：" + form["bundle_uti_urine_collection"] + "。";
                            utiPlace += "集尿袋維持密閉、無菌且通暢的引流系統，避免管路扭曲或壓折: " + form["bundle_uti_Avoid_creasing"] + "。";
                            utiPlace += "集尿袋不可超過八分滿:" + form["bundle_uti_Capacity"] + "。";
                            utiPlace += "完成導尿管置放後，立即執行手部衛生: " + form["bundle_uti_hand_hygiene_after"] + "。";

                            if (utiPlace != "")
                            {
                                base.Insert_CareRecord(DateTime.Now.ToString("yyyy/MM/dd HH:mm:01"), ID + "_UTI", "UTI Bundle 置入照護評估", "", "", "", utiPlace, "", "Tube_UTI");
                            }
                        }
                        if (this.link.DBExecInsertTns("TUBE", dbItemList) > 0)
                        {
                            dbItemList = new List<DBItem>();
                            dbItemList.Add(new DBItem("FEATUREID", ID, DBItem.DBDataType.String));
                            dbItemList.Add(new DBItem("NUMBERID", form["tubeNumber"] ?? "", DBItem.DBDataType.String));
                            dbItemList.Add(new DBItem("NUMBEROTHER", form["tubeNumber_other"] ?? "", DBItem.DBDataType.String));
                            dbItemList.Add(new DBItem("MATERIALID", form["tubeMaterial"] ?? "", DBItem.DBDataType.String));
                            dbItemList.Add(new DBItem("MATERIALOTHER", form["tubeMaterial_other"] ?? "", DBItem.DBDataType.String));
                            dbItemList.Add(new DBItem("COLORID", form["color_drainage"] ?? "", DBItem.DBDataType.String));
                            dbItemList.Add(new DBItem("COLOROTHER", form["color_other"] ?? "", DBItem.DBDataType.String));
                            dbItemList.Add(new DBItem("NATUREID", form["nature_drainage"] ?? "", DBItem.DBDataType.String));
                            dbItemList.Add(new DBItem("NATUREOTHER", form["nature_other"] ?? "", DBItem.DBDataType.String));
                            dbItemList.Add(new DBItem("TASTEID", form["taste_drainage"] ?? "", DBItem.DBDataType.String));
                            dbItemList.Add(new DBItem("TASTEOTHER", form["taste_other"] ?? "", DBItem.DBDataType.String));
                            if (this.link.DBExecInsertTns("TUBE_FEATURE", dbItemList) > 0)
                            {
                                if (base.Insert_CareRecordTns(DateTime.Now.ToString("yyyy/MM/dd HH:mm:00"), ID, "放置" + form["Prompt_TubeKind"], "", "", "", form["nur_text"], "", "Tube", ref this.link) > 0)
                                    success = true;
                                else
                                    success = false;
                            }
                            else
                                success = false;
                        }
                        else
                            success = false;
                    }

                    #endregion

                    if (success)
                    {
                        this.link.DBCommit();
                        Msg = "新增成功！";
                    }
                    else
                    {
                        this.link.DBRollBack();
                        Msg = "新增失敗！";
                    }

                }
                else
                    Msg = "登入逾時！";

            }
            catch (Exception ex)
            {
                Msg = "新增失敗！";
                if (ex.ToString() != "DBCommit Fail")
                {
                    this.link.DBRollBack();
                }

                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }
            finally
            {
                this.link.DBClose();
            }

            return Msg;
        }

        [HttpPost]
        [ValidateInput(false)]
        public string Edit(FormCollection form)
        {
            string Msg = string.Empty;
            try
            {
                if (Session["PatInfo"] != null)
                {
                    bool success = false;
                    List<DBItem> dbItemList;

                    dbItemList = new List<DBItem>();
                    dbItemList.Add(new DBItem("UPDNO", base.userinfo.EmployeesNo, DBItem.DBDataType.String));
                    dbItemList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                    if (form["tube_forecast_time"] != "")
                        dbItemList.Add(new DBItem("FORECAST", Convert.ToDateTime(form["tube_forecast_time"]).ToString("yyyy/MM/dd 00:00"), DBItem.DBDataType.DataTime));
                    else
                        dbItemList.Add(new DBItem("FORECAST", "", DBItem.DBDataType.DataTime));
                    dbItemList.Add(new DBItem("MODEL", form["param_tube_model"], DBItem.DBDataType.String));
                    dbItemList.Add(new DBItem("LENGTH", form["param_tube_length"], DBItem.DBDataType.String));
                    dbItemList.Add(new DBItem("LENGTHUNIT", form["tubeLengthUnit"], DBItem.DBDataType.String));
                    var placeStationType = form["place_station"];
                    var placeStationValue = string.Empty;
                    if (placeStationType == "院內")
                    {
                        placeStationValue = form["costlist"];
                    }
                    else if (placeStationType == "院外" && form["outHosp"] != "0")
                    {
                        if (form["outHosp"] == "家中")
                        {
                            placeStationValue = form["outHosp"];
                        }
                        else
                        {
                            placeStationValue = form["outHosp"] + "|" + form["outHosp_other"];
                        }
                    }
                    dbItemList.Add(new DBItem("PLACE_STATION", placeStationType + "|" + placeStationValue, DBItem.DBDataType.String));
                    dbItemList.Add(new DBItem("POSITION", form["txt_position"], DBItem.DBDataType.String));
                    dbItemList.Add(new DBItem("LOCATION", form["tubePosition"], DBItem.DBDataType.String));
                    //dbItemList.Add(new DBItem("PLACE_STATION", (form["place_station"]) ?? "", DBItem.DBDataType.String));
                    //dbItemList.Add(new DBItem("PLACE_REASON", form["bundle_uti_reason"] + "|" + form["bundle_uti_reason_other"], DBItem.DBDataType.String));
                    if (form["bundle_uti_reason"] != null)
                    {
                        dbItemList.Add(new DBItem("PLACE_REASON", form["bundle_uti_reason"] + "|" + form["bundle_uti_reason_other"] + "|" + form["bundle_uti_hand_hygiene"] + "|" + form["bundle_uti_Aseptic"] + "|" + form["bundle_uti_Urinary_catheter"] + "|" + form["bundle_uti_urine_collection"] + "|" + form["bundle_uti_Avoid_creasing"] + "|" + form["bundle_uti_Capacity"] + "|" + form["bundle_uti_hand_hygiene_after"], DBItem.DBDataType.String));
                    }
                    if (this.link.DBExecUpdateTns("TUBE", dbItemList, " FEENO = '" + base.ptinfo.FeeNo + "' AND TUBEROW = " + form["tuberow"] + " AND TUBEID = '" + form["tubeid"] + "' ") > 0)
                    {
                        dbItemList = new List<DBItem>();
                        dbItemList.Add(new DBItem("MATERIALID", form["tubeMaterial"] ?? "", DBItem.DBDataType.String));
                        dbItemList.Add(new DBItem("MATERIALOTHER", form["tubeMaterial_other"] ?? "", DBItem.DBDataType.String));
                        if (this.link.DBExecUpdateTns("TUBE_FEATURE", dbItemList, " FEATUREID = '" + form["tubeid"] + "' ") > 0)
                        {
                            success = true;
                            //base.Insert_CareRecord_BlackTns(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), base.creatid(form["tubeid"], base.userinfo.EmployeesNo, base.ptinfo.FeeNo, "0"), form["Prompt_TubeKind"] + "資料更新", "", "", "", form["nur_text"], "", ref link);
                            //base.Upd_CareRecord(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), form["tubeid"], "放置" + form["Prompt_TubeKind"], form["nur_text"], "", "", "", "", "Tube");
                            base.Insert_CareRecord(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), form["tubeid"], "放置" + form["Prompt_TubeKind"], form["nur_text"], "", "", "", "", "Tube");

                        }
                    }
                    if (form["bundle_uti_reason"] != null)
                    {
                        dbItemList.Add(new DBItem("PLACE_REASON", form["bundle_uti_reason"] + "|" + form["bundle_uti_reason_other"] + "|" + form["bundle_uti_hand_hygiene"] + "|" + form["bundle_uti_Aseptic"] + "|" + form["bundle_uti_Urinary_catheter"] + "|" + form["bundle_uti_urine_collection"] + "|" + form["bundle_uti_Avoid_creasing"] + "|" + form["bundle_uti_Capacity"] + "|" + form["bundle_uti_hand_hygiene_after"], DBItem.DBDataType.String));
                        var utiPlace = "";
                        var reason = "";

                        if (form["bundle_uti_reason"] != "" && form["bundle_uti_reason_other"] != "")
                        {
                            reason = form["bundle_uti_reason"].Replace("其他", form["bundle_uti_reason_other"]);
                        }
                        else
                        {
                            reason = form["bundle_uti_reason"];
                        }

                        utiPlace += "因為" + reason + "等因素，依醫囑放置" + form["Prompt_TubeKind"] + "。";
                        utiPlace += "放置時進行以下評估：";
                        utiPlace += "置放導尿管前執行手部衛生：" + form["bundle_uti_hand_hygiene"] + "。";
                        utiPlace += "無菌技術操作：" + form["bundle_uti_Aseptic"] + "。";
                        utiPlace += "導尿管固定於：" + form["bundle_uti_Urinary_catheter"] + "。";
                        utiPlace += "集尿袋應維持在膀胱以下位置，末置於地面：" + form["bundle_uti_urine_collection"] + "。";
                        utiPlace += "集尿袋維持密閉、無菌且通暢的引流系統，避免管路扭曲或壓折: " + form["bundle_uti_Avoid_creasing"] + "。";
                        utiPlace += "集尿袋不可超過八分滿:" + form["bundle_uti_Capacity"] + "。";
                        utiPlace += "完成導尿管置放後，立即執行手部衛生: " + form["bundle_uti_hand_hygiene_after"] + "。";

                        if (utiPlace != "")
                        {
                            //base.Upd_CareRecord(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), form["tubeid"] + "_UTI", "UTI Bundle 置入照護評估", "", "", "", utiPlace, "", "Tube_UTI");
                            base.Insert_CareRecord(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), form["tubeid"] + "_UTI", "UTI Bundle 置入照護評估", "", "", "", utiPlace, "", "Tube_UTI");

                        }
                    }

                    if (success)
                    {
                        this.link.DBCommit();
                        Msg = "更新成功！";
                    }
                    else
                    {
                        this.link.DBRollBack();
                        Msg = "更新失敗！";
                    }

                }
                else
                    Msg = "登入逾時！";

            }
            catch (Exception ex)
            {
                Msg = "新增失敗！";
                if (ex.ToString() != "DBCommit Fail")
                {
                    this.link.DBRollBack();
                }
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }
            finally
            {
                this.link.DBClose();
            }

            return Msg;
        }

        [HttpPost]
        public string Delete(string tubeRow, string tubeID)
        {
            string Msg = string.Empty;
            try
            {
                if (Session["PatInfo"] != null)
                {
                    bool success = false;

                    try
                    {
                        List<DBItem> dbItemList = new List<DBItem>();
                        dbItemList.Add(new DBItem("UPDNO", base.userinfo.EmployeesNo, DBItem.DBDataType.String));
                        dbItemList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                        dbItemList.Add(new DBItem("DELETED", base.userinfo.EmployeesNo, DBItem.DBDataType.String));

                        if (this.link.DBExecUpdateTns("TUBE", dbItemList, " FEENO = '" + base.ptinfo.FeeNo + "' AND TUBEROW = " + tubeRow + " AND TUBEID = '" + tubeID + "' ") > 0)
                        {
                            //if (base.Del_CareRecordTns(tubeID, "Tube", ref link) > 0)
                            if (base.Del_CareRecord(tubeID, "Tube") > 0)

                                success = true;
                        }
                    }
                    catch
                    {
                        success = false;
                    }
                    finally
                    {
                        if (success)
                        {
                            link.DBCommit(); //delete
                            Msg = "刪除成功！";
                        }
                        else
                        {
                            link.DBRollBack();
                            Msg = "刪除失敗！";
                        }
                    }
                }
                else
                    Msg = "登入逾時！";

            }
            catch (Exception ex)
            {
                link.DBRollBack();
                Msg = "刪除失敗！";
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }
            finally
            {
                this.link.DBClose();
            }

            return Msg;
        }

        #endregion

        #region 拔管

        [HttpGet]
        public ActionResult UpdEndtime(string tuberow, string tubeid)
        {
            if (Session["PatInfo"] != null)
            {
                DataTable dt_tube = tubem.sel_tube(base.ptinfo.FeeNo, "", tuberow, tubeid);
                ViewBag.dt_tube = dt_tube;
                ViewBag.userno = userinfo.EmployeesNo;
            }
            else
                Response.Write("<script>alert('登入逾時！');window.close();</script>");
            return View();
        }

        [HttpPost]
        public string UpdEndtime(string JsonString)
        {
            if (Session["PatInfo"] != null)
            {
                List<Dictionary<string, string>> dt = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(JsonString);
                int erow = 0;
                List<DBItem> dbItemList;
                string content = string.Empty;

                string tubeId = "";

                foreach (var item in dt)
                {
                    dbItemList = new List<DBItem>();
                    dbItemList.Add(new DBItem("UPDNO", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    dbItemList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                    dbItemList.Add(new DBItem("ENDTIME", item["RecordTime"], DBItem.DBDataType.DataTime));
                    dbItemList.Add(new DBItem("ENDREMARK", item["Remark"] + "|" + item["RemarkReason"], DBItem.DBDataType.String));
                    erow = this.link.DBExecUpdate("TUBE", dbItemList, " FEENO = '" + base.ptinfo.FeeNo + "' AND TUBEROW = " + item["Row"] + " AND TUBEID = '" + item["ID"] + "' ");
                    if (erow > 0)
                    {
                        content = "於" + item["RecordTime"] + "，因" + item["Remark"].Replace("其他", "") + item["RemarkReason"] + " " + item["CareMsg"];
                        base.Insert_CareRecord(DateTime.Now.ToString("yyyy/MM/dd HH:mm"), item["ID"], "", "", "", "", content, "", item["ID"]);
                        tubeId = item["ID"];
                    }
                }
                //20230912 LU拔管後確認是否有管路照護紀錄提醒 有要取消提醒
                if (erow > 0)
                {
                    DataTable dt_check = tubem.sel_tube_bundlecare_by_id(tubeId);
                    if (dt_check.Rows.Count > 0)
                    {
                        string bc_type = dt_check.Rows[0]["BUNDLE_CARE"].ToString();
                        bc_type = bc_type == "-1" ? "" : bc_type == "1" ? "UTI" : bc_type == "2" ? "VAP" : bc_type == "3" ? "BSI" : "";
                        if (bc_type != "")
                        {
                            dt_check.Clear();
                            dt_check = tubem.sel_bundle_notice_id(ptinfo.FeeNo, bc_type);
                            if (dt_check.Rows.Count > 0)
                            {
                                string preId = dt_check.Rows[0]["NT_ID"].ToString();
                                if (!preId.IsEmpty() && erow > 0)
                                {
                                    dbItemList = new List<DBItem>
                                    {
                                        new DBItem("TIMEOUT", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime)
                                    };
                                    erow = link.DBExecUpdate("DATA_NOTICE", dbItemList, "NT_ID = '" + preId + "' ");
                                }
                            }
                        }
                    }
                }

                if (erow > 0)
                    return "儲存成功！";
                else
                    return "儲存失敗！";

            }
            else
                return "登入逾時！";
        }


        //取消拔管
        [HttpPost]
        public string Delete_Endtime(string tubeRow, string tubeID)
        {
            string Msg = string.Empty;
            try
            {
                if (Session["PatInfo"] != null)
                {
                    bool success = false;

                    try
                    {
                        List<DBItem> dbItemList = new List<DBItem>();
                        dbItemList.Add(new DBItem("UPDNO", base.userinfo.EmployeesNo, DBItem.DBDataType.String));
                        dbItemList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                        dbItemList.Add(new DBItem("ENDTIME", "", DBItem.DBDataType.DataTime));
                        dbItemList.Add(new DBItem("ENDREMARK", "", DBItem.DBDataType.String));

                        if (this.link.DBExecUpdateTns("TUBE", dbItemList, " FEENO = '" + base.ptinfo.FeeNo + "' AND TUBEROW = " + tubeRow + " AND TUBEID = '" + tubeID + "' ") > 0)
                        {
                            //if (base.Del_CareRecordTns(tubeID, tubeID, ref link) > 0)
                            if (base.Del_CareRecord(tubeID, tubeID) > 0)

                                success = true;
                        }

                    }
                    catch
                    {
                        success = false;
                    }
                    finally
                    {
                        if (success)
                        {
                            link.DBCommit();
                            Msg = "取消拔管成功！";
                        }
                        else
                        {
                            link.DBRollBack();
                            Msg = "取消失敗！";
                        }
                    }
                }
                else
                    Msg = "登入逾時！";

            }
            catch (Exception ex)
            {
                link.DBRollBack();
                Msg = "取消失敗！";
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }
            finally
            {
                this.link.DBClose();
            }
            return Msg;
        }
        #endregion

        #region 管路評估

        //管路評估_List
        public ActionResult Assessment_List(string starttime, string endtime, string type = "")
        {
            string start = DateTime.Now.AddDays(-7).ToString("yyyy/MM/dd 00:00");
            string end = DateTime.Now.ToString("yyyy/MM/dd HH:mm");
            string feeno = ptinfo.FeeNo;
            if (starttime != null && endtime != null)
            {
                start = starttime;
                end = endtime;
            }
            DataTable dt_Artery = new DataTable();
            DataTable dt_Tracheostomy = new DataTable();

            ViewBag.userno = userinfo.EmployeesNo;
            ViewBag.start = start;
            ViewBag.end = end;
            ViewBag.type = type;
            ViewBag.dt_Artery = tubem.sel_tube_assess_artery(feeno, start, end, "");
            ViewBag.dt_Tracheostomy = tubem.sel_tube_assess_tracheostomy(feeno, start, end, "");
            ViewBag.dt_Other = tubem.sel_tube_assess_other(feeno, start, end, "");

            /*
            //LU 20230130 修改arterybundle 資料加載 ↓ 測試可用  尚未啟用
            string assid;
            string bundle_Text;

            dt_Artery = tubem.sel_tube_assess_artery(feeno, start, end, "");
            dt_Artery.Columns.Add("BUNDLE_TEXT", typeof(string));
            for (int i = 0; i < dt_Artery.Rows.Count; i++)
            {
                assid = dt_Artery.Rows[i]["ASSESS_ID"].ToString();
                bundle_Text = SELECT_BundleCare2(assid);
                dt_Artery.Rows[i]["BUNDLE_TEXT"] = bundle_Text;
            }
            ViewBag.dt_Artery = dt_Artery;
            //LU 20230130 修改arterybundle 資料加載  ↑
            */

            return View();
        }
        public ActionResult Assessment_List_View(string starttime, string endtime, string feeno)
        {
            string start = DateTime.Now.AddDays(-7).ToString("yyyy/MM/dd 00:00");
            string end = DateTime.Now.ToString("yyyy/MM/dd HH:mm");
            if (starttime != null && endtime != null)
            {
                start = starttime;
                end = endtime;
            }
            DataTable dt_Artery = new DataTable();
            DataTable dt_Tracheostomy = new DataTable();

            ViewBag.start = start;
            ViewBag.end = end;
            if (feeno != "")
            {
                ViewBag.feeno = feeno;
                ViewBag.dt_Artery = tubem.sel_tube_assess_artery(feeno, start, end, "");
                ViewBag.dt_Tracheostomy = tubem.sel_tube_assess_tracheostomy(feeno, start, end, "");
                ViewBag.dt_Other = tubem.sel_tube_assess_other(feeno, start, end, "");
            }


            return View();
        }

        //管路評估頁面
        public ActionResult Assess(string id, string row, string type)
        {
            if (Session["PatInfo"] != null)
            {
                string feeno = ptinfo.FeeNo;
                string userno = userinfo.EmployeesNo;

                //ViewBag.dt = tubem.sel_tube(feeno, "", "", "");

                if (id != null)
                {
                    ViewBag.dt = tubem.sel_tube(feeno, "", row, "", "Update");

                    ViewBag.id = id;
                    ViewBag.row = row;
                    ViewBag.type = type;
                }
                else
                {
                    ViewBag.dt = tubem.sel_tube(feeno, "", "", "");
                }

                return View();
            }

            Response.Write("<script>alert('登入逾時！');window.close();</script>");
            return new EmptyResult();
        }

        #region Other評估

        //首頁Other評估
        public ActionResult Assess_Other_Index(string id)
        {
            if (id != null)
                ViewBag.dt = tubem.sel_tube_assess_other(ptinfo.FeeNo, "", "", id);

            ViewData["color_drainage"] = this.cd.getSelectItem("io", "outputcolor_Drainage", "");
            ViewData["taste_drainage"] = this.cd.getSelectItem("io", "outputtaste_Drainage", "");
            ViewData["nature_drainage"] = this.cd.getSelectItem("io", "outputnature_Drainage", "");
            return View();
        }

        //新增Other評估
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Insert_Assess_Other(FormCollection form)
        {
            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;
            string id = this.creatid("ASSESS_OTHER", userno, feeno, "0");

            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("ASSESS_ID", id, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("RECORDTIME", form["other_start_date"] + " " + form["other_start_time"], DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("TUBEROW", form["tuberow"], DBItem.DBDataType.Number));
            insertDataList.Add(new DBItem("TUBE_POSITION", form["other_position"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FIX", form["other_fix"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("SKIN_STATUS", form["other_skin_status"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("RANGE", (form["txt_other_range"] == null || form["txt_other_range"] == "") ? "0" : form["txt_other_range"], DBItem.DBDataType.Number));
            insertDataList.Add(new DBItem("EXUDATE_AMOUNT", form["other_exudate_amount"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("EXUDATE_TYPE", form["other_exudate_type"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("DRAINAGE", form["other_drainage"], DBItem.DBDataType.String));

            int erow = tubem.DBExecInsert("NIS_TUBE_ASSESS_OTHER_DATA", insertDataList);
            if (erow > 0)
            {
                insertDataList.Clear();
                insertDataList.Add(new DBItem("FEATUREID", id, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("COLORID", form["color_drainage"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("COLOROTHER", (form["color_drainage_other"] == null) ? "" : form["color_drainage_other"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("TASTEID", form["taste_drainage"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("TASTEOTHER", (form["taste_drainage_other"] == null) ? "" : form["taste_drainage_other"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("NATUREID", form["nature_drainage"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("NATUREOTHER", (form["nature_drainage_other"] == null) ? "" : form["nature_drainage_other"], DBItem.DBDataType.String));
                erow = tubem.DBExecInsert("TUBE_FEATURE", insertDataList);

                if (form["other_position"] != "")
                {
                    string content = "";
                    if (form["other_position"] == "是" && form["other_fix"] == "是")
                        content += form["tube_name"] + "置放於" + form["tube_position"] + "，觀察位置正確固定妥當";
                    else if (form["other_position"] == "是" && form["other_fix"] == "否")
                        content += form["tube_name"] + "置放於" + form["tube_position"] + "，觀察位置正確需重新與以固定";
                    else if (form["other_position"] == "否")
                        content += form["tube_name"] + "置放於" + form["tube_position"] + "，觀察位置不正確，與已重新確認位置並固定妥當";

                    if (content != "")
                    {
                        DataTable dt = tubem.sel_sys_param_data("io");
                        if (form["other_skin_status"] != "")
                            content += "，周圍皮膚" + form["other_skin_status"] + ((form["txt_other_range"] == null || form["txt_other_range"] == "") ? "0" : form["txt_other_range"]) + "cm";
                        if (form["other_skin_status"].IndexOf("潮濕") > -1)
                            content += "，滲出液：" + form["other_exudate_amount"] + " 呈 " + form["other_exudate_type"];
                        if (form["other_drainage"] != "")
                        {
                            content += "，引流：" + form["other_drainage"];
                            if (form["other_drainage"] != "此管路非作為引流用途")
                            {
                                if (!string.IsNullOrEmpty(tubem.sel_data(dt, "outputcolor_Drainage", form["color_drainage"])))
                                {
                                    string idx_taste = "", idx_nature = "";
                                    idx_taste = form["taste_drainage"] + ((form["taste_drainage_other"] == null) ? "" : form["taste_drainage_other"]);
                                    idx_nature = form["nature_drainage"] + ((form["nature_drainage_other"] == null) ? "" : form["nature_drainage_other"]);

                                    content += "，引流色" + tubem.sel_data(dt, "outputcolor_Drainage", form["color_drainage"]) + ((form["color_drainage_other"] == null) ? "" : form["color_drainage_other"]);
                                    if (idx_taste != "-1")
                                        content += "，味道" + tubem.sel_data(dt, "outputtaste_Drainage", form["taste_drainage"]) + ((form["taste_drainage_other"] == null) ? "" : form["taste_drainage_other"]);
                                    if (idx_nature != "-1")
                                        content += "，性質" + tubem.sel_data(dt, "outputnature_Drainage", form["nature_drainage"]) + ((form["nature_drainage_other"] == null) ? "" : form["nature_drainage_other"]);
                                }
                            }
                        }
                        base.Insert_CareRecord(form["other_start_date"] + " " + form["other_start_time"], id, "", content, "", "", "", "", "TUBE_ASSESS");
                    }
                }

                if (erow > 0)
                    Response.Write("<script>alert('新增成功！');window.close();window.opener.location.href='Assessment_List';</script>");
                else
                    Response.Write("<script>alert('新增失敗！');window.close();window.opener.location.href='Assessment_List';</script>");
            }
            else
                Response.Write("<script>alert('新增失敗！');window.close();window.opener.location.href='Assessment_List';</script>");
            return new EmptyResult();
        }

        //新增Other評估
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Insert_Assess_Other2(FormCollection form)
        {
            LogTool lt = new LogTool();
            string id = "";
            try
            {
                string userno = userinfo.EmployeesNo;
                string feeno = ptinfo.FeeNo;
                id = this.creatid("ASSESS_OTHER", userno, feeno, "0");
                string date = form["start_date"] + " " + form["start_time"];
                string exudate_amount = form["rb_other_exudate_amount_1"] + "|" + form["rb_other_exudate_amount_2"];
                string exudate_type = form["ck_other_exudate_type_1"] + "|" + form["ck_other_exudate_type_2"];

                List<DBItem> insertDataList = new List<DBItem>();
                insertDataList.Add(new DBItem("ASSESS_ID", id, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("RECORDTIME", date, DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("TUBEROW", form["tuberow"], DBItem.DBDataType.Number));
                insertDataList.Add(new DBItem("TUBE_POSITION", form["other_position"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FIX", form["other_fix"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("SKIN_STATUS", form["other_skin_status"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("RANGE", form["txt_other_range"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EXUDATE_AMOUNT", exudate_amount, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EXUDATE_TYPE", exudate_type, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("DRAINAGE", form["rb_other_drainage"], DBItem.DBDataType.String));

                int erow = tubem.DBExecInsert("NIS_TUBE_ASSESS_OTHER_DATA", insertDataList);
                if (erow > 0)
                {
                    insertDataList.Clear();
                    insertDataList.Add(new DBItem("FEATUREID", id, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("COLORID", form["color_drainage"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("COLOROTHER", (form["color_drainage_other"] == null) ? "" : form["color_drainage_other"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("TASTEID", form["taste_drainage"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("TASTEOTHER", (form["taste_drainage_other"] == null) ? "" : form["taste_drainage_other"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("NATUREID", form["nature_drainage"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("NATUREOTHER", (form["nature_drainage_other"] == null) ? "" : form["nature_drainage_other"], DBItem.DBDataType.String));
                    erow = tubem.DBExecInsert("TUBE_FEATURE", insertDataList);

                    DataTable dt = tubem.sel_sys_param_data("io");
                    int status_count = 0;
                    string[] status_list = form["other_skin_status"].Split(',');
                    string[] range_list = form["txt_other_range"].Split(',');
                    string[] exudate_amount_list = exudate_amount.Split('|');
                    string[] exudate_type_list = exudate_type.Split('|');
                    string content = form["tube_name"] + "置放於" + form["tube_position"];
                    if (form["other_position"] == "是" && form["other_fix"] == "是")
                        content += "，觀察位置正確固定妥當，周圍皮膚：";
                    else if (form["other_position"] == "是" && form["other_fix"] == "否")
                        content += "，觀察位置正確需重新與以固定，周圍皮膚：";
                    else if (form["other_position"] == "否")
                        content += "，觀察位置不正確，與已重新確認位置並固定妥當，周圍皮膚：";

                    for (int i = 0; i < range_list.Length; i++)
                    {
                        if (range_list[i] != "")
                        {
                            content += status_list[status_count] + range_list[i] + " cm、";
                            status_count++;
                        }
                    }

                    for (int i = 0; i < exudate_amount_list.Length; i++)
                    {
                        if (exudate_amount_list[i] != "")
                        {
                            content += status_list[status_count] + ":滲出液" + exudate_amount_list[i];
                            if (exudate_type_list[i] != "")
                            {
                                content += "呈";
                                if (exudate_type_list[i].Split(',').GetValue(exudate_type_list[i].Split(',').Length - 1).ToString() != "")
                                    content += exudate_type_list[i] + "、";
                                else
                                    content += exudate_type_list[i].Substring(0, exudate_type_list[i].Length - 1) + "、";
                            }
                            else
                                content += "、";
                            status_count++;
                        }
                    }
                    if (status_count == 0)
                        content += status_list[status_count];
                    else
                        content = content.Substring(0, content.Length - 1);

                    content += "，引流功能：" + form["rb_other_drainage"];
                    if (form["rb_other_drainage"] != "此管路非作為引流用途")
                    {
                        string idx_taste = "", idx_nature = "";
                        idx_taste = form["taste_drainage"] + ((form["taste_drainage_other"] == null) ? "" : form["taste_drainage_other"]);
                        idx_nature = form["nature_drainage"] + ((form["nature_drainage_other"] == null) ? "" : form["nature_drainage_other"]);

                        if (!string.IsNullOrEmpty(tubem.sel_data(dt, "outputcolor_Drainage", form["color_drainage"])))
                        {
                            content += "，引流色" + tubem.sel_data(dt, "outputcolor_Drainage", form["color_drainage"]) + ((form["color_drainage_other"] == null) ? "" : form["color_drainage_other"]);
                        }
                        if (!string.IsNullOrEmpty(tubem.sel_data(dt, "outputcolor_Drainage", form["taste_drainage"])))
                        {
                            if (idx_taste != "-1")
                                content += "，味道" + tubem.sel_data(dt, "outputtaste_Drainage", form["taste_drainage"]) + ((form["taste_drainage_other"] == null) ? "" : form["taste_drainage_other"]);
                        }
                        if (!string.IsNullOrEmpty(tubem.sel_data(dt, "outputcolor_Drainage", form["nature_drainage"])))
                        {
                            if (idx_nature != "-1")
                                content += "，性質" + tubem.sel_data(dt, "outputnature_Drainage", form["nature_drainage"]) + ((form["nature_drainage_other"] == null) ? "" : form["nature_drainage_other"]);
                        }

                    }
                    base.Insert_CareRecord(date, id, "", "", "", "", "", content + "。", "TUBE_ASSESS");

                    if (erow > 0)
                        //Response.Write("<script>alert('新增成功！');window.close();window.opener.location.href='Assessment_List';</script>");
                        Response.Write("管路評估新增成功!" + id);
                    else
                        //Response.Write("<script>alert('新增失敗！');window.close();window.opener.location.href='Assessment_List';</script>");
                        Response.Write("管路評估新增失敗!");
                }
                else
                    //Response.Write("<script>alert('新增失敗！');window.close();window.opener.location.href='Assessment_List';</script>");
                    Response.Write("管路評估新增失敗!");
            }
            catch (Exception ex)
            {
                lt.saveLogMsg(ex.Message + id, "CareRecord_Log_Red");
            }
            return new EmptyResult();
        }

        //更新Other評估
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Upd_Assess_Other(FormCollection form)
        {
            string date = form["start_date"] + " " + form["start_time"];
            string exudate_amount = form["rb_other_exudate_amount_1"] + "|" + form["rb_other_exudate_amount_2"];
            string exudate_type = form["ck_other_exudate_type_1"] + "|" + form["ck_other_exudate_type_2"];

            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("UPDNO", userinfo.EmployeesNo, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("RECORDTIME", date, DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("TUBE_POSITION", form["other_position"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FIX", form["other_fix"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("SKIN_STATUS", form["other_skin_status"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("RANGE", form["txt_other_range"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("EXUDATE_AMOUNT", exudate_amount, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("EXUDATE_TYPE", exudate_type, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("DRAINAGE", form["rb_other_drainage"], DBItem.DBDataType.String));
            string where = " ASSESS_ID = '" + form["id"] + "' ";

            int erow = tubem.DBExecUpdate("NIS_TUBE_ASSESS_OTHER_DATA", insertDataList, where);
            if (erow > 0)
            {
                insertDataList.Clear();
                insertDataList.Add(new DBItem("COLORID", form["color_drainage"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("COLOROTHER", (form["color_drainage_other"] == null) ? "" : form["color_drainage_other"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("TASTEID", form["taste_drainage"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("TASTEOTHER", (form["taste_drainage_other"] == null) ? "" : form["taste_drainage_other"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("NATUREID", form["nature_drainage"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("NATUREOTHER", (form["nature_drainage_other"] == null) ? "" : form["nature_drainage_other"], DBItem.DBDataType.String));
                where = " FEATUREID = '" + form["id"] + "' ";
                erow = tubem.DBExecUpdate("TUBE_FEATURE", insertDataList, where);

                DataTable dt = tubem.sel_sys_param_data("io");
                int status_count = 0;
                string[] status_list = form["other_skin_status"].Split(',');
                string[] range_list = form["txt_other_range"].Split(',');
                string[] exudate_amount_list = exudate_amount.Split('|');
                string[] exudate_type_list = exudate_type.Split('|');
                string content = form["tube_name"] + "置放於" + form["tube_position"];
                if (form["other_position"] == "是" && form["other_fix"] == "是")
                    content += "，觀察位置正確固定妥當，周圍皮膚：";
                else if (form["other_position"] == "是" && form["other_fix"] == "否")
                    content += "，觀察位置正確需重新與以固定，周圍皮膚：";
                else if (form["other_position"] == "否")
                    content += "，觀察位置不正確，與已重新確認位置並固定妥當，周圍皮膚：";

                for (int i = 0; i < range_list.Length; i++)
                {
                    if (range_list[i] != "")
                    {
                        content += status_list[status_count] + range_list[i] + " cm、";
                        status_count++;
                    }
                }

                for (int i = 0; i < exudate_amount_list.Length; i++)
                {
                    if (exudate_amount_list[i] != "")
                    {
                        content += status_list[status_count] + ":滲出液" + exudate_amount_list[i];
                        if (exudate_type_list[i] != "")
                        {
                            content += "呈";
                            if (exudate_type_list[i].Split(',').GetValue(exudate_type_list[i].Split(',').Length - 1).ToString() != "")
                                content += exudate_type_list[i] + "、";
                            else
                                content += exudate_type_list[i].Substring(0, exudate_type_list[i].Length - 1) + "、";
                        }
                        else
                            content += "、";
                        status_count++;
                    }
                }
                if (status_count == 0)
                    content += status_list[status_count];
                else
                    content = content.Substring(0, content.Length - 1);

                content += "，引流功能：" + form["rb_other_drainage"];
                if (form["rb_other_drainage"] != "此管路非作為引流用途")
                {
                    if (!string.IsNullOrEmpty(tubem.sel_data(dt, "outputcolor_Drainage", form["color_drainage"])))
                    {
                        string idx_taste = "", idx_nature = "";
                        idx_taste = form["taste_drainage"] + ((form["taste_drainage_other"] == null) ? "" : form["taste_drainage_other"]);
                        idx_nature = form["nature_drainage"] + ((form["nature_drainage_other"] == null) ? "" : form["nature_drainage_other"]);
                        content += "，引流色" + tubem.sel_data(dt, "outputcolor_Drainage", form["color_drainage"]) + ((form["color_drainage_other"] == null) ? "" : form["color_drainage_other"]);
                        if (idx_taste != "-1")
                            content += "，味道" + tubem.sel_data(dt, "outputtaste_Drainage", form["taste_drainage"]) + ((form["taste_drainage_other"] == null) ? "" : form["taste_drainage_other"]);
                        if (idx_nature != "-1")
                            content += "，性質" + tubem.sel_data(dt, "outputnature_Drainage", form["nature_drainage"]) + ((form["nature_drainage_other"] == null) ? "" : form["nature_drainage_other"]);
                    }
                }
                base.Upd_CareRecord(date, form["id"], "", content + "。", "", "", "", "", "TUBE_ASSESS");
                string id = form["id"];

                if (erow > 0)
                    //Response.Write("<script>alert('更新成功！');window.close();window.opener.location.href='Assessment_List';</script>");
                    Response.Write("管路評估更新成功!" + id);
                else
                    //Response.Write("<script>alert('更新失敗！');window.close();window.opener.location.href='Assessment_List';</script>");
                    Response.Write("管路評估更新失敗!");
            }
            else
                //Response.Write("<script>alert('更新失敗！');window.close();window.opener.location.href='Assessment_List';</script>");
                Response.Write("管路評估更新失敗!");

            return new EmptyResult();
        }

        #endregion

        #region Tracheostomy評估

        //首頁Other評估
        public ActionResult Assess_Tracheostomy_Index(string id)
        {
            if (id != null)
                ViewBag.dt = tubem.sel_tube_assess_tracheostomy(ptinfo.FeeNo, "", "", id);
            return View();
        }

        //新增Tracheostomy評估
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Insert_Assess_Tracheostomy(FormCollection form)
        {
            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;
            string id = this.creatid("ASSESS_TRACHEOSTOMY", userno, feeno, "0");

            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("ASSESS_ID", id, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("RECORDTIME", form["other_start_date"] + " " + form["other_start_time"], DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("TUBEROW", form["tuberow"], DBItem.DBDataType.Number));
            insertDataList.Add(new DBItem("TUBE_POSITION", form["tracheostomy_position"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FIX", form["tracheostomy_fix"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FIXATION", form["tracheostomy_fixation"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FIX_CUFF_1", (form["txt_tracheostomy_fix_cuff_1"] == null) ? "0" : form["txt_tracheostomy_fix_cuff_1"], DBItem.DBDataType.Number));
            insertDataList.Add(new DBItem("FIX_CUFF_2", (form["txt_tracheostomy_fix_cuff_2"] == null) ? "0" : form["txt_tracheostomy_fix_cuff_2"], DBItem.DBDataType.Number));
            insertDataList.Add(new DBItem("FIX_CUFF_3", (form["txt_tracheostomy_fix_cuff_3"] == null) ? "0" : form["txt_tracheostomy_fix_cuff_3"], DBItem.DBDataType.Number));

            int erow = tubem.DBExecInsert("NIS_TUBE_ASSESS_TRACH_DATA", insertDataList);
            if (erow > 0)
            {
                List<string> temp = new List<string>();
                if (form["tracheostomy_position"] != "")
                {
                    string content = "";
                    if (form["tracheostomy_fix"] == "是")
                        content += form["tube_name"] + "置放於" + form["tube_position"] + "，觀察位置正確";
                    else if (form["tracheostomy_fix"] == "否")
                        content += form["tube_name"] + "置放於" + form["tube_position"] + "，觀察位置正確需重新與以固定";

                    if (content != "")
                    {
                        if (form["tracheostomy_fixation"] != "")
                            content += "，以" + form["tracheostomy_fixation"] + "固定妥當";
                        if (form["txt_tracheostomy_fix_cuff_1"] != null || form["txt_tracheostomy_fix_cuff_2"] != null || form["txt_tracheostomy_fix_cuff_3"] != null)
                        {
                            content += "，目前氣囊壓力";
                            temp = new List<string>();
                            if (form["txt_tracheostomy_fix_cuff_1"] != "")
                            {
                                temp.Add(form["txt_tracheostomy_fix_cuff_1"] + "mmHg ");

                            }
                            if (form["txt_tracheostomy_fix_cuff_2"] != "")
                            {
                                temp.Add(form["txt_tracheostomy_fix_cuff_2"] + "mL ");

                            }
                            if (form["txt_tracheostomy_fix_cuff_3"] != "")
                            {
                                temp.Add(form["txt_tracheostomy_fix_cuff_3"] + "cmH₂O ");
                            }
                            if (temp.Count > 0)
                            {
                                content += string.Join("，", temp);
                            }
                        }
                    }
                    if (content != "")
                        base.Insert_CareRecord(form["other_start_date"] + " " + form["other_start_time"], id, "", content, "", "", "", "", "TUBE_ASSESS");
                }
                Response.Write("<script>alert('新增成功！');window.close();window.opener.location.href='Assessment_List';</script>");
            }
            else
                Response.Write("<script>alert('新增失敗！');window.close();window.opener.location.href='Assessment_List';</script>");
            return new EmptyResult();
        }

        //新增Tracheostomy評估
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Insert_Assess_Tracheostomy2(FormCollection form)
        {
            LogTool lt = new LogTool();
            string id = "";
            try
            {
                string userno = userinfo.EmployeesNo;
                string feeno = ptinfo.FeeNo;
                id = this.creatid("ASSESS_TRACHEOSTOMY", userno, feeno, "0");
                string date = form["start_date"] + " " + form["start_time"];

                List<DBItem> insertDataList = new List<DBItem>();
                insertDataList.Add(new DBItem("ASSESS_ID", id, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("RECORDTIME", date, DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("TUBEROW", form["tuberow"], DBItem.DBDataType.Number));
                insertDataList.Add(new DBItem("TUBE_POSITION", form["tracheostomy_position"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FIX", form["tracheostomy_fix"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FIXATION", form["tracheostomy_fixation"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FIX_CUFF_1", form["txt_tracheostomy_fix_cuff_1"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FIX_CUFF_2", form["txt_tracheostomy_fix_cuff_2"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FIX_CUFF_3", form["txt_tracheostomy_fix_cuff_3"], DBItem.DBDataType.String));

                int erow = tubem.DBExecInsert("NIS_TUBE_ASSESS_TRACH_DATA", insertDataList);
                if (erow > 0)
                {
                    List<string> temp = new List<string>();
                    string content = form["tube_name"] + "置放於" + form["tube_position"];
                    if (form["tracheostomy_fix"] == "是")
                        content += "，觀察位置正確";
                    else if (form["tracheostomy_fix"] == "否")
                        content += "，觀察位置正確需重新與以固定";

                    if (form["tracheostomy_fixation"].Split(',').GetValue(form["tracheostomy_fixation"].Split(',').Length - 1).ToString() != "")
                        content += "，以" + form["tracheostomy_fixation"] + "固定妥當，目前氣囊壓力固定";
                    else
                        content += "，以" + form["tracheostomy_fixation"].Substring(0, form["tracheostomy_fixation"].Length - 1) + "固定妥當，目前氣囊壓力固定";
                    temp = new List<string>();
                    if (form["txt_tracheostomy_fix_cuff_1"] != "")
                    {
                        temp.Add(form["txt_tracheostomy_fix_cuff_1"] + "mmHg ");

                    }
                    if (form["txt_tracheostomy_fix_cuff_2"] != "")
                    {
                        temp.Add(form["txt_tracheostomy_fix_cuff_2"] + "mL ");

                    }
                    if (form["txt_tracheostomy_fix_cuff_3"] != "")
                    {
                        temp.Add(form["txt_tracheostomy_fix_cuff_3"] + "cmH₂O ");
                    }
                    if (temp.Count > 0)
                    {
                        content += string.Join("，", temp);
                    }
                    base.Insert_CareRecord(date, id, "", content + "。", "", "", "", "", "TUBE_ASSESS");

                    List<Bill_RECORD> billDataList = new List<Bill_RECORD>();
                    Bill_RECORD billData = new Bill_RECORD();

                    if (form["tube_name"] == "氣切導管")
                    {
                        billData.HO_ID = "3056022";
                        billData.COUNT= "1";
                        billDataList.Add(billData);
                        SaveBillingRecord(billDataList);
                    }

                    //Response.Write("<script>alert('新增成功！');window.close();window.opener.location.href='Assessment_List';</script>");
                    Response.Write("管路評估新增成功!" + id);
                }
                else
                    //Response.Write("<script>alert('新增失敗！');window.close();window.opener.location.href='Assessment_List';</script>");
                    Response.Write("管路評估新增失敗!");
            }
            catch (Exception ex)
            {
                lt.saveLogMsg(ex.Message + id, "CareRecord_Log_Red");
            }
            return new EmptyResult();
        }

        //更新Tracheostomy評估
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Upd_Assess_Tracheostomy(FormCollection form)
        {
            string date = form["start_date"] + " " + form["start_time"];

            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("UPDNO", userinfo.EmployeesNo, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("RECORDTIME", date, DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("TUBE_POSITION", form["tracheostomy_position"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FIX", form["tracheostomy_fix"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FIXATION", form["tracheostomy_fixation"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FIX_CUFF_1", form["txt_tracheostomy_fix_cuff_1"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FIX_CUFF_2", form["txt_tracheostomy_fix_cuff_2"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FIX_CUFF_3", form["txt_tracheostomy_fix_cuff_3"], DBItem.DBDataType.String));
            string where = " ASSESS_ID = '" + form["id"] + "' ";

            int erow = tubem.DBExecUpdate("NIS_TUBE_ASSESS_TRACH_DATA", insertDataList, where);
            if (erow > 0)
            {
                List<string> temp = new List<string>();
                string content = form["tube_name"] + "置放於" + form["tube_position"];
                if (form["tracheostomy_fix"] == "是")
                    content += "，觀察位置正確";
                else if (form["tracheostomy_fix"] == "否")
                    content += "，觀察位置正確需重新與以固定";

                if (form["tracheostomy_fixation"].Split(',').GetValue(form["tracheostomy_fixation"].Split(',').Length - 1).ToString() != "")
                    content += "，以" + form["tracheostomy_fixation"] + "固定妥當，目前氣囊壓力固定";
                else
                    content += "，以" + form["tracheostomy_fixation"].Substring(0, form["tracheostomy_fixation"].Length - 1) + "固定妥當，目前氣囊壓力固定";
                temp = new List<string>();
                if (form["txt_tracheostomy_fix_cuff_1"] != "")
                {
                    temp.Add(form["txt_tracheostomy_fix_cuff_1"] + "mmHg ");

                }
                if (form["txt_tracheostomy_fix_cuff_2"] != "")
                {
                    temp.Add(form["txt_tracheostomy_fix_cuff_2"] + "mL ");

                }
                if (form["txt_tracheostomy_fix_cuff_3"] != "")
                {
                    temp.Add(form["txt_tracheostomy_fix_cuff_3"] + "cmH₂O ");
                }
                if (temp.Count > 0)
                {
                    content += string.Join("，", temp);
                }

                base.Upd_CareRecord(date, form["id"], "", content + "。", "", "", "", "", "TUBE_ASSESS");
                string id = form["id"];
                //Response.Write("<script>alert('更新成功！');window.close();window.opener.location.href='Assessment_List';</script>");
                Response.Write("管路評估更新成功!" + id);
            }
            else
                //Response.Write("<script>alert('更新失敗！');window.close();window.opener.location.href='Assessment_List';</script>");
                Response.Write("管路評估更新失敗!");
            return new EmptyResult();
        }

        #endregion

        #region Artery評估

        //首頁Artery評估
        public ActionResult Assess_Artery_Index(string id)
        {
            if (id != null)
                ViewBag.dt = tubem.sel_tube_assess_artery(ptinfo.FeeNo, "", "", id);
            return View();
        }

        //新增Artery評估
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Insert_Assess_Artery(FormCollection form)
        {
            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;
            string id = this.creatid("ASSESS_ARTERY", userno, feeno, "0");

            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("ASSESS_ID", id, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("RECORDTIME", form["other_start_date"] + " " + form["other_start_time"], DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("TUBEROW", form["tuberow"], DBItem.DBDataType.Number));
            insertDataList.Add(new DBItem("TUBE_POSITION", form["artery_position"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FIX", form["artery_fix"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("SKIN_STATUS", form["artery_skin_status"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("RANGE", (form["txt_artery_range"] == null || form["txt_artery_range"] == "") ? "0" : form["txt_artery_range"], DBItem.DBDataType.Number));
            insertDataList.Add(new DBItem("EXUDATE_AMOUNT", form["artery_exudate_amount"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("EXUDATE_TYPE", form["artery_exudate_type"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("UNIMPEDED", form["artery_unimpeded"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("GIRTH", (form["txt_artery_girth"] == "") ? "0" : form["txt_artery_girth"], DBItem.DBDataType.Number));

            int erow = tubem.DBExecInsert("NIS_TUBE_ASSESS_ARTERY_DATA", insertDataList);
            if (erow > 0)
            {
                if (form["artery_position"] != "")
                {
                    string content = "";
                    if (form["artery_fix"] == "是")
                        content += form["tube_name"] + "置放於" + form["tube_position"] + "，觀察位置正確固定妥當";
                    else if (form["artery_fix"] == "否")
                        content += form["tube_name"] + "置放於" + form["tube_position"] + "，觀察位置正確需重新與以固定";

                    if (content != "")
                    {
                        DataTable dt = tubem.sel_sys_param_data("io");
                        if (form["artery_skin_status"] != "")
                            content += "，周圍皮膚" + form["artery_skin_status"] + ((form["txt_artery_range"] == null || form["txt_artery_range"] == "") ? "0" : form["txt_artery_range"]) + "cm";
                        if (form["artery_skin_status"].IndexOf("潮濕") > -1)
                            content += "，滲出液：" + form["artery_exudate_amount"] + " 呈 " + form["artery_exudate_type"];
                        if (form["artery_unimpeded"] == "無")
                            content += "，管路不通暢予以NS沖洗";
                        else if (form["artery_unimpeded"] == "有")
                            content += "，管路通暢";
                    }

                    if (content != "")
                        base.Insert_CareRecord(form["other_start_date"] + " " + form["other_start_time"], id, "", content, "", "", "", "", "TUBE_ASSESS");
                }
                Response.Write("<script>alert('新增成功！');window.close();window.opener.location.href='Assessment_List';</script>");
            }
            else
                Response.Write("<script>alert('新增失敗！');window.close();window.opener.location.href='Assessment_List';</script>");
            return new EmptyResult();
        }

        //新增Artery評估
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Insert_Assess_Artery2(FormCollection form)
        {
            LogTool lt = new LogTool();
            string id = "";
            try
            {
                string userno = userinfo.EmployeesNo;
                string feeno = ptinfo.FeeNo;
                id = this.creatid("ASSESS_ARTERY", userno, feeno, "0");
                string date = form["start_date"] + " " + form["start_time"];
                string exudate_amount = form["rb_artery_exudate_amount_1"] + "|" + form["rb_artery_exudate_amount_2"];
                string exudate_type = form["ck_artery_exudate_type_1"] + "|" + form["ck_artery_exudate_type_2"];

                List<DBItem> insertDataList = new List<DBItem>();
                insertDataList.Add(new DBItem("ASSESS_ID", id, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("RECORDTIME", date, DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("TUBEROW", form["tuberow"], DBItem.DBDataType.Number));
                insertDataList.Add(new DBItem("TUBE_POSITION", form["artery_position"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FIX", form["artery_fix"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("SKIN_STATUS", form["artery_skin_status"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("RANGE", form["txt_artery_range"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EXUDATE_AMOUNT", exudate_amount, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EXUDATE_TYPE", exudate_type, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UNIMPEDED", form["artery_unimpeded"], DBItem.DBDataType.String));

                int erow = tubem.DBExecInsert("NIS_TUBE_ASSESS_ARTERY_DATA", insertDataList);
                if (erow > 0)
                {
                    string content = form["tube_name"] + "置放於" + form["tube_position"];
                    if (form["artery_fix"] == "是")
                        content += "，觀察位置正確固定妥當，周圍皮膚";
                    else if (form["artery_fix"] == "否")
                        content += "，觀察位置正確需重新與以固定，周圍皮膚";

                    DataTable dt = tubem.sel_sys_param_data("io");
                    int status_count = 0;
                    string[] status_list = form["artery_skin_status"].Split(',');
                    string[] range_list = form["txt_artery_range"].Split(',');
                    string[] exudate_amount_list = exudate_amount.Split('|');
                    string[] exudate_type_list = exudate_type.Split('|');

                    for (int i = 0; i < range_list.Length; i++)
                    {
                        if (range_list[i] != "")
                        {
                            content += status_list[status_count] + range_list[i] + " cm、";
                            status_count++;
                        }
                    }

                    for (int i = 0; i < exudate_amount_list.Length; i++)
                    {
                        if (exudate_amount_list[i] != "")
                        {
                            content += status_list[status_count] + ":滲出液" + exudate_amount_list[i];
                            if (exudate_type_list[i] != "")
                            {
                                content += "呈";
                                if (exudate_type_list[i].Split(',').GetValue(exudate_type_list[i].Split(',').Length - 1).ToString() != "")
                                    content += exudate_type_list[i] + "、";
                                else
                                    content += exudate_type_list[i].Substring(0, exudate_type_list[i].Length - 1) + "、";
                            }
                            else
                                content += "、";
                            status_count++;
                        }
                    }
                    if (status_count == 0)
                        content += status_list[status_count];
                    else
                        content = content.Substring(0, content.Length - 1);

                    if (form["artery_unimpeded"] == "無")
                        content += "，管路不通暢予以NS沖洗";
                    else if (form["artery_unimpeded"] == "有")
                        content += "，管路通暢";

                    base.Insert_CareRecord(date, id, "", content + "。", "", "", "", "", "TUBE_ASSESS");

                    //Response.Write("<script>alert('新增成功！');window.close();window.opener.location.href='Assessment_List';</script>");
                    Response.Write("管路評估新增成功!" + id);
                }
                else
                    //Response.Write("<script>alert('新增失敗！');window.close();window.opener.location.href='Assessment_List';</script>");
                    Response.Write("管路評估新增失敗!");
            }
            catch (Exception ex)
            {
                lt.saveLogMsg(ex.Message + id, "CareRecord_Log_Red");
            }
            return new EmptyResult();
        }

        //更新Artery評估
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Upd_Assess_Artery(FormCollection form)
        {
            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;
            string date = form["start_date"] + " " + form["start_time"];
            string exudate_amount = form["rb_artery_exudate_amount_1"] + "|" + form["rb_artery_exudate_amount_2"];
            string exudate_type = form["ck_artery_exudate_type_1"] + "|" + form["ck_artery_exudate_type_2"];

            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("UPDNO", userinfo.EmployeesNo, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("RECORDTIME", date, DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("TUBE_POSITION", form["artery_position"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FIX", form["artery_fix"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("SKIN_STATUS", form["artery_skin_status"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("RANGE", form["txt_artery_range"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("EXUDATE_AMOUNT", exudate_amount, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("EXUDATE_TYPE", exudate_type, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("UNIMPEDED", form["artery_unimpeded"], DBItem.DBDataType.String));
            string where = " ASSESS_ID = '" + form["id"] + "' ";

            int erow = tubem.DBExecUpdate("NIS_TUBE_ASSESS_ARTERY_DATA", insertDataList, where);
            if (erow > 0)
            {
                string content = form["tube_name"] + "置放於" + form["tube_position"];
                if (form["artery_fix"] == "是")
                    content += "，觀察位置正確固定妥當，周圍皮膚";
                else if (form["artery_fix"] == "否")
                    content += "，觀察位置正確需重新與以固定，周圍皮膚";

                DataTable dt = tubem.sel_sys_param_data("io");
                int status_count = 0;
                string[] status_list = form["artery_skin_status"].Split(',');
                string[] range_list = form["txt_artery_range"].Split(',');
                string[] exudate_amount_list = exudate_amount.Split('|');
                string[] exudate_type_list = exudate_type.Split('|');

                for (int i = 0; i < range_list.Length; i++)
                {
                    if (range_list[i] != "")
                    {
                        content += status_list[status_count] + range_list[i] + " cm、";
                        status_count++;
                    }
                }

                for (int i = 0; i < exudate_amount_list.Length; i++)
                {
                    if (exudate_amount_list[i] != "")
                    {
                        content += status_list[status_count] + ":滲出液" + exudate_amount_list[i];
                        if (exudate_type_list[i] != "")
                        {
                            content += "呈";
                            if (exudate_type_list[i].Split(',').GetValue(exudate_type_list[i].Split(',').Length - 1).ToString() != "")
                                content += exudate_type_list[i] + "、";
                            else
                                content += exudate_type_list[i].Substring(0, exudate_type_list[i].Length - 1) + "、";
                        }
                        else
                            content += "、";
                        status_count++;
                    }
                }
                if (status_count == 0)
                    content += status_list[status_count];
                else
                    content = content.Substring(0, content.Length - 1);

                if (form["artery_unimpeded"] == "無")
                    content += "，管路不通暢予以NS沖洗";
                else if (form["artery_unimpeded"] == "有")
                    content += "，管路通暢";

                base.Upd_CareRecord(date, form["id"], "", content + "。", "", "", "", "", "TUBE_ASSESS");
                string id = form["id"];
                //Response.Write("<script>alert('更新成功！');window.close();window.opener.location.href='Assessment_List';</script>");
                Response.Write("管路評估更新成功!" + id);
            }
            else
                //Response.Write("<script>alert('更新失敗！');window.close();window.opener.location.href='Assessment_List';</script>");
                Response.Write("管路評估更新失敗!");
            return new EmptyResult();
        }

        #endregion

        #region 刪除評估
        public ActionResult Del_Assess(string tablename, string id)
        {
            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("DELETED", userinfo.EmployeesNo, DBItem.DBDataType.String));
            string where = " ASSESS_ID = '" + id + "' ";
            int erow = tubem.DBExecUpdate(tablename, insertDataList, where);
            if (erow > 0)
            {
                //LU 20230118新增刪除BUNDLE
                DataTable dt = tubem.sel_tube_bundle_master("", id, "");
                if (dt.Rows.Count > 0)
                {
                    string userno = userinfo.EmployeesNo;
                    string username = userinfo.EmployeesName;
                    string DELETE_TIME = DateTime.Now.ToString("yyyy/MM/dd HH:mm");
                    insertDataList.Clear();
                    insertDataList.Add(new DBItem("DELETE_ID", userno, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("DELETE_NAME", username, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("DELETE_TIME", DELETE_TIME, DBItem.DBDataType.DataTime));
                    erow = tubem.DBExecUpdate("NIS_TUBE_ASSESS_BUNDLE_MASTER", insertDataList, where);
                }

                if (erow > 0)
                {
                    base.Del_CareRecord(id, "TUBE_ASSESS");
                    //LU 20230621 Bundle 護理紀錄 刪除
                    base.Del_CareRecord(id, "TUBE_BUNDLE_ASSESS");
                    Response.Write("<script>alert('刪除成功！');window.location.href='Assessment_List';</script>");
                }
            }
            else
                Response.Write("<script>alert('刪除失敗！');window.location.href='Assessment_List';</script>");
            return new EmptyResult();
        }

        #endregion 刪除評估
        #endregion

        #region 管路照護
        /// <summary>
        /// LU 20230104
        /// 新增管路照護UTI TABLE VIEW
        /// </summary>
        /// <param name="tube_id">COLUMN[TUBEID]</param>
        /// <returns>Multi Data By Newest</returns>
        /// <param name="ass_id">COLUMN[ASSESS_ID]</param>
        /// <returns>Single Data</returns>
        public ActionResult Assess_BundleCare_UTI(string tube_id, string ass_id)
        {
            if (tube_id != null || ass_id != null)
            {
                string feeno = ptinfo.FeeNo;
                DataTable dt_master = new DataTable();
                string table_id = "";
                if (tube_id != null)
                {
                    dt_master = tubem.sel_tube_bundle_master(feeno, "", tube_id);
                    if (dt_master.Rows.Count > 0)
                        table_id = dt_master.Rows[0]["TABLE_ID"].ToString();
                }
                else if (ass_id != null)
                {
                    dt_master = tubem.sel_tube_bundle_master(feeno, ass_id, "");
                    if (dt_master.Rows.Count > 0)
                        table_id = dt_master.Rows[0]["TABLE_ID"].ToString();
                    ViewBag.table_id = table_id;
                }
                if (table_id != "")
                {
                    DataTable dt_detail = tubem.sel_tube_bundle_datail(table_id);
                    if (dt_detail.Rows.Count > 0)
                    {
                        List<DB_NIS_TUBE_ASSESS_BUNDLE> BUNDLES = new List<DB_NIS_TUBE_ASSESS_BUNDLE>();
                        foreach (DataRow row in dt_detail.Rows)
                        {
                            BUNDLES.Add(new DB_NIS_TUBE_ASSESS_BUNDLE
                            {
                                NAME = row["ITEM_ID"].ToString(),
                                VALUE = row["ITEM_VALUE"].ToString(),
                                TYPE = row["ITEM_TYPE"].ToString()
                            });
                        }
                        ViewBag.dt_detail = BUNDLES;
                    }
                }
            }
            return View();
        }

        /// <summary>
        /// LU 20230104
        /// 新增管路照護VAP TABLE VIEW
        /// </summary>
        /// <param name="tube_id">COLUMN[TUBEID]</param>
        /// <returns>Multi Data By Newest</returns>
        /// <param name="ass_id">COLUMN[ASSESS_ID]</param>
        /// <returns>Single Data</returns>
        public ActionResult Assess_BundleCare_VAP(string tube_id, string ass_id)
        {
            if (tube_id != null || ass_id != null)
            {
                string feeno = ptinfo.FeeNo;
                DataTable dt_master = new DataTable();
                string table_id = "";
                if (tube_id != null)
                {
                    dt_master = tubem.sel_tube_bundle_master(feeno, "", tube_id);
                    if (dt_master.Rows.Count > 0)
                        table_id = dt_master.Rows[0]["TABLE_ID"].ToString();
                }
                else if (ass_id != null)
                {
                    dt_master = tubem.sel_tube_bundle_master(feeno, ass_id, "");
                    if (dt_master.Rows.Count > 0)
                        table_id = dt_master.Rows[0]["TABLE_ID"].ToString();
                    ViewBag.table_id = table_id;
                }
                if (table_id != "")
                {
                    DataTable dt_detail = tubem.sel_tube_bundle_datail(table_id);
                    if (dt_detail.Rows.Count > 0)
                    {
                        List<DB_NIS_TUBE_ASSESS_BUNDLE> BUNDLES = new List<DB_NIS_TUBE_ASSESS_BUNDLE>();
                        foreach (DataRow row in dt_detail.Rows)
                        {
                            BUNDLES.Add(new DB_NIS_TUBE_ASSESS_BUNDLE
                            {
                                NAME = row["ITEM_ID"].ToString(),
                                VALUE = row["ITEM_VALUE"].ToString(),
                                TYPE = row["ITEM_TYPE"].ToString()
                            });
                        }
                        ViewBag.dt_detail = BUNDLES;
                    }
                }
            }
            return View();
        }

        /// <summary>
        /// LU 20230104
        /// 新增管路照護BSI TABLE VIEW
        /// </summary>
        /// <param name="tube_id">COLUMN[TUBEID]</param>
        /// <returns>Multi Data By Newest</returns>
        /// <param name="ass_id">COLUMN[ASSESS_ID]</param>
        /// <returns>Single Data</returns>
        public ActionResult Assess_BundleCare_BSI(string tube_id, string ass_id)
        {
            if (tube_id != null || ass_id != null)
            {
                string feeno = ptinfo.FeeNo;
                DataTable dt_master = new DataTable();
                string table_id = "";
                if (tube_id != null)
                {
                    dt_master = tubem.sel_tube_bundle_master(feeno, "", tube_id);
                    if (dt_master.Rows.Count > 0)
                        table_id = dt_master.Rows[0]["TABLE_ID"].ToString();
                }
                else if (ass_id != null)
                {
                    dt_master = tubem.sel_tube_bundle_master(feeno, ass_id, "");
                    if (dt_master.Rows.Count > 0)
                        table_id = dt_master.Rows[0]["TABLE_ID"].ToString();
                    ViewBag.table_id = table_id;
                }
                if (table_id != "")
                {
                    DataTable dt_detail = tubem.sel_tube_bundle_datail(table_id);
                    if (dt_detail.Rows.Count > 0)
                    {
                        List<DB_NIS_TUBE_ASSESS_BUNDLE> BUNDLES = new List<DB_NIS_TUBE_ASSESS_BUNDLE>();
                        foreach (DataRow row in dt_detail.Rows)
                        {
                            BUNDLES.Add(new DB_NIS_TUBE_ASSESS_BUNDLE
                            {
                                NAME = row["ITEM_ID"].ToString(),
                                VALUE = row["ITEM_VALUE"].ToString(),
                                TYPE = row["ITEM_TYPE"].ToString()
                            });
                        }
                        ViewBag.dt_detail = BUNDLES;
                    }
                }
            }
            return View();
        }

        //LU 20230109 新增BundleCare  暫時未使用
        public ActionResult Insert_BundleCare1(List<DB_NIS_TUBE_ASSESS_BUNDLE1> model)
        {
            string feeno = ptinfo.FeeNo;
            string userno = userinfo.EmployeesNo;
            string username = userinfo.EmployeesName;
            string ass_Id = model[0].ASSID;
            string tube_Id = model[0].TUBEID;
            string tube_Row = model[0].TUBEROW;
            string BC_TYPE = model[0].BC_TYPE;
            string Table_Id = this.creatid("BUNDLE_MASTER", userno, feeno, BC_TYPE);
            string CREATE_TIME = DateTime.Now.ToString("yyyy/MM/dd HH:mm");
            RESPONSE_MSG json_result = new RESPONSE_MSG();

            if (ass_Id == "")
            {
                json_result.status = RESPONSE_STATUS.ERROR;
                json_result.message = "管路評估未新增成功";

                return Json(JsonConvert.SerializeObject(json_result));
            }

            //新增主表
            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("TUBEID", tube_Id, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("TUBEROW", tube_Row, DBItem.DBDataType.Number));
            insertDataList.Add(new DBItem("ASSESS_ID", ass_Id, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("TABLE_ID", Table_Id, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("BC_TYPE", BC_TYPE, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATE_ID", userno, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATE_NAME", username, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATE_TIME", CREATE_TIME, DBItem.DBDataType.DataTime));

            int erow = tubem.DBExecInsert("NIS_TUBE_ASSESS_BUNDLE_MASTER", insertDataList);

            //增加細項
            if (erow > 0)
            {
                for (int i = 1; i < model.Count; i++)
                {
                    insertDataList.Clear();
                    string SERIAL = this.creatid("BUNDLE_DETAIL", userno, feeno, i.ToString());
                    insertDataList.Add(new DBItem("SERIAL", SERIAL, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("TABLE_ID", Table_Id, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ITEM_ID", model[i].NAME, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ITEM_TYPE", model[i].TYPE, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ITEM_VALUE", model[i].VALUE, DBItem.DBDataType.String));
                    erow = tubem.DBExecInsert("NIS_TUBE_ASSESS_BUNDLE_DETAIL", insertDataList);
                }

                if (erow > 0)
                {
                    //Response.Write("<script>alert('新增成功！');window.close();window.opener.location.href='Assessment_List';</script>");
                    json_result.status = RESPONSE_STATUS.SUCCESS;
                    json_result.message = "管路照護新增成功!";
                    return Json(JsonConvert.SerializeObject(json_result));
                }
            }
            json_result.status = RESPONSE_STATUS.ERROR;
            json_result.message = "管路照護新增失敗!";
            return Json(JsonConvert.SerializeObject(json_result));
        }

        /// <summary>
        /// LU 20230118 新增BundleCare 修改版
        /// </summary>
        /// <param name="ass_id"></param>
        /// <param name="tube_id"></param>
        /// <param name="tube_row"></param>
        /// <param name="bc_type"></param>
        /// <param name="model"></param>
        /// <returns>Json[RESPONSE_MSG]= json_result</returns>        
        public ActionResult Insert_BundleCare(string ass_id, string tube_id, string tube_row, string bc_type, string record_time, List<DB_NIS_TUBE_ASSESS_BUNDLE> model)
        {
            RESPONSE_MSG json_result = new RESPONSE_MSG();
            if (ass_id == "" || tube_id == "" || tube_row == "" || bc_type == "")
            {
                json_result.status = RESPONSE_STATUS.ERROR;
                json_result.message = "管路評估未新增成功";
                return Json(JsonConvert.SerializeObject(json_result));
            }
            string feeno = ptinfo.FeeNo;
            string userno = userinfo.EmployeesNo;
            string username = userinfo.EmployeesName;
            string TABLE_ID = this.creatid("BUNDLE_MASTER", userno, feeno, bc_type);
            string CREATE_TIME = DateTime.Now.ToString("yyyy/MM/dd HH:mm");

            //新增主表
            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("TUBEID", tube_id, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("TUBEROW", tube_row, DBItem.DBDataType.Number));
            insertDataList.Add(new DBItem("ASSESS_ID", ass_id, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("TABLE_ID", TABLE_ID, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("BC_TYPE", bc_type, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("RECORDTIME", record_time, DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("CREATE_ID", userno, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATE_NAME", username, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATE_TIME", CREATE_TIME, DBItem.DBDataType.DataTime));

            int erow = tubem.DBExecInsert("NIS_TUBE_ASSESS_BUNDLE_MASTER", insertDataList);
            //增加細項
            if (erow > 0 && model != null)
            {
                for (int i = 0; i < model.Count; i++)
                {
                    string SERIAL = this.creatid("BUNDLE_DETAIL", userno, feeno, i.ToString());
                    insertDataList.Clear();
                    insertDataList.Add(new DBItem("SERIAL", SERIAL, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("TABLE_ID", TABLE_ID, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ITEM_ID", model[i].NAME, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ITEM_TYPE", model[i].TYPE, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ITEM_VALUE", model[i].VALUE, DBItem.DBDataType.String));
                    erow = tubem.DBExecInsert("NIS_TUBE_ASSESS_BUNDLE_DETAIL", insertDataList);
                }
                if (erow > 0)
                {
                    //LU 20230621 Bundle 拋護理紀錄
                    string content = BundleTransferContent(bc_type, model);
                    if (!content.IsEmpty())
                    {
                        string title = bc_type == "UTI" ? "導尿管每日照護紀錄" : bc_type == "VAP" ? "呼吸器使用每日照護紀錄" : "中心導管每日照護紀錄";
                        erow = base.Insert_CareRecord(record_time, ass_id, title, content, "", "", "", "", "TUBE_BUNDLE_ASSESS");

                        if (erow > 0)
                        {
                            //拋提醒跑馬燈
                            erow = ConfirmNotice(feeno, userno, CREATE_TIME, bc_type);
                            if (erow > 0)
                            {
                                json_result.status = RESPONSE_STATUS.SUCCESS;
                                json_result.message = "管路照護新增成功!";
                                return Json(JsonConvert.SerializeObject(json_result));
                            }
                        }
                    }
                    else
                    {
                        erow += base.Del_CareRecord(ass_id, "TUBE_BUNDLE_ASSESS");
                        if (erow > 0)
                        {
                            json_result.status = RESPONSE_STATUS.SUCCESS;
                            json_result.message = "管路照護新增成功!";
                            return Json(JsonConvert.SerializeObject(json_result));
                        }
                    }
                }
            }
            json_result.status = RESPONSE_STATUS.ERROR;
            json_result.message = "管路照護新增失敗!";
            return Json(JsonConvert.SerializeObject(json_result));
        }

        //LU 懸浮視窗資料傳遞 20230116 暫時未使用
        public ActionResult SELECT_BundleCare1(string ass_id)
        {
            string feeno = ptinfo.FeeNo;
            RESPONSE_MSG json_result = new RESPONSE_MSG();

            if (ass_id != null)
            {
                DataTable dt_master = tubem.sel_tube_bundle_master(feeno, ass_id, "");

                if (dt_master.Rows.Count > 0)
                {
                    string table_id = dt_master.Rows[0]["TABLE_ID"].ToString();

                    DataTable dt_detail = tubem.sel_tube_bundle_datail(table_id);

                    List<DB_NIS_TUBE_ASSESS_BUNDLE> BUNDLES = new List<DB_NIS_TUBE_ASSESS_BUNDLE>();
                    //DB_NIS_TUBE_ASSESS_BUNDLE BUNDLE = new DB_NIS_TUBE_ASSESS_BUNDLE();

                    foreach (DataRow row in dt_detail.Rows)
                    {
                        BUNDLES.Add(new DB_NIS_TUBE_ASSESS_BUNDLE
                        {
                            NAME = row["ITEM_ID"].ToString(),
                            VALUE = row["ITEM_VALUE"].ToString(),
                            TYPE = row["ITEM_TYPE"].ToString()
                        });
                    }

                    json_result.status = RESPONSE_STATUS.SUCCESS;
                    json_result.message = dt_master.Rows[0]["BC_TYPE"].ToString();
                    json_result.attachment = BUNDLES;

                }
            }
            return Json(JsonConvert.SerializeObject(json_result));
        }

        /// <summary>
        /// LU 懸浮視窗資料傳遞 20230118 修改版
        /// </summary>
        /// <param name="ass_id"></param>
        /// <returns></returns>        
        public ActionResult SELECT_BundleCare(string ass_id)
        {
            string feeno = ptinfo.FeeNo;
            RESPONSE_MSG json_result = new RESPONSE_MSG();

            if (ass_id != null)
            {
                DataTable dt_master = tubem.sel_tube_bundle_master(feeno, ass_id, "");
                if (dt_master.Rows.Count > 0)
                {
                    string table_id = dt_master.Rows[0]["TABLE_ID"].ToString();
                    DataTable dt_detail = tubem.sel_tube_bundle_datail(table_id);
                    if (dt_detail.Rows.Count > 0)
                    {
                        List<DB_NIS_TUBE_ASSESS_BUNDLE> BUNDLES = new List<DB_NIS_TUBE_ASSESS_BUNDLE>();
                        foreach (DataRow row in dt_detail.Rows)
                        {
                            BUNDLES.Add(new DB_NIS_TUBE_ASSESS_BUNDLE
                            {
                                NAME = row["ITEM_ID"].ToString(),
                                VALUE = row["ITEM_VALUE"].ToString(),
                                TYPE = row["ITEM_TYPE"].ToString()
                            });
                        }

                        string type = dt_master.Rows[0]["BC_TYPE"].ToString();

                        string text = "";
                        string text1 = "";
                        string text2 = "";
                        string text3 = "";
                        string text4 = "";
                        string text5 = "";
                        string text6 = "";
                        string text7 = "";
                        string text8 = "";
                        string text9 = "";

                        if (BUNDLES.Count == 0)
                            text = "";
                        else if (type == "UTI")
                        {
                            foreach (DB_NIS_TUBE_ASSESS_BUNDLE BUNDLE in BUNDLES)
                            {
                                if ("bundle_uti_reason" == BUNDLE.NAME)
                                    text1 += BUNDLE.VALUE + ",";
                                else if ("bundle_uti_reason_other" == BUNDLE.NAME)
                                    text1 = text1.Substring(0, text1.Length - 1) + "：" + BUNDLE.VALUE + ",";
                                else if ("bundle_uti_hand" == BUNDLE.NAME)
                                    text2 += BUNDLE.VALUE + ",";
                                else if ("bundle_uti_tube" == BUNDLE.NAME)
                                    text3 += BUNDLE.VALUE + ",";
                                else if ("bundle_uti_bag" == BUNDLE.NAME)
                                    text4 += BUNDLE.VALUE + ",";
                                else if ("bundle_uti_clean_condition" == BUNDLE.NAME)
                                    text7 += BUNDLE.VALUE + ",";
                                else if ("bundle_uti_position" == BUNDLE.NAME)
                                    text5 += BUNDLE.VALUE + ",";
                                else if ("bundle_uti_clean_method" == BUNDLE.NAME)
                                    text6 += BUNDLE.VALUE + ",";
                                else if ("bundle_uti_clean_method_other" == BUNDLE.NAME)
                                    text6 = text6.Substring(0, text6.Length - 1) + "：" + BUNDLE.VALUE + ",";
                            }
                            if (text1 != "")
                            {
                                text += "導管需要留存原因：" + text1.Substring(0, text1.Length - 1) + CRLF;
                            }
                            if (text2 != "")
                            {
                                text += "1.手部衛生：" + text2.Substring(0, text2.Length - 1) + CRLF;
                            }
                            if (text5 != "")
                            {
                                text += "2.導尿管固定位置：" + text5.Substring(0, text5.Length - 1) + CRLF;
                            }
                            if (text3 != "")
                            {
                                text += "3.密閉、無菌且通暢的引流系統，避免管路扭曲或壓折：" + text3.Substring(0, text3.Length - 1) + CRLF;
                            }
                            if (text4 != "")
                            {
                                text += "4.集尿袋不可超過8分滿：" + text4.Substring(0, text4.Length - 1) + CRLF;
                            }
                            if (text7 != "")
                            {
                                text += "5.尿道口清潔：" + text7.Substring(0, text7.Length - 1) + CRLF;
                            }
                            if (text6 != "")
                            {
                                text += "6.尿道口清潔方式：" + text6.Substring(0, text6.Length - 1) + CRLF;
                            }
                        }
                        else if (type == "VAP")
                        {

                            foreach (DB_NIS_TUBE_ASSESS_BUNDLE BUNDLE in BUNDLES)
                            {
                                if ("bundle_vap_reason" == BUNDLE.NAME)
                                    text1 += BUNDLE.VALUE + ",";
                                else if ("bundle_vap_reason_other" == BUNDLE.NAME)
                                    text1 = text1.Substring(0, text1.Length - 1) + "：" + BUNDLE.VALUE + ",";
                                else if ("bundle_vap_sedation" == BUNDLE.NAME)
                                    if (BUNDLE.VALUE == "否")
                                        text2 += BUNDLE.VALUE + "，未停用原因：";
                                    else
                                        text2 += BUNDLE.VALUE + "：";
                                else if ("bundle_vap_sedation_none" == BUNDLE.NAME)
                                    text2 += BUNDLE.VALUE + ",";
                                else if ("bundle_vap_sedation_other" == BUNDLE.NAME)
                                    text2 = text2.Substring(0, text2.Length - 1) + "：" + BUNDLE.VALUE + ",";
                                else if ("bundle_vap_mouth" == BUNDLE.NAME)
                                    text3 += BUNDLE.VALUE + ",";
                                else if ("bundle_vap_bed_height" == BUNDLE.NAME)
                                    if (BUNDLE.VALUE == "否")
                                        text4 += BUNDLE.VALUE + "，未抬高原因：";
                                    else
                                        text4 += BUNDLE.VALUE + "：";
                                else if ("bundle_vap_bed_height_none" == BUNDLE.NAME)
                                    text4 += BUNDLE.VALUE + ",";
                                else if ("bundle_vap_bed_height_other" == BUNDLE.NAME)
                                    text4 = text4.Substring(0, text4.Length - 1) + "：" + BUNDLE.VALUE + ",";
                                else if ("bundle_vap_effusion" == BUNDLE.NAME)
                                    text5 += BUNDLE.VALUE + ",";
                            }

                            if (text1 != "")
                            {
                                text += "使用呼吸器適應症：" + text1.Substring(0, text1.Length - 1) + CRLF;
                            }
                            if (text2 != "")
                            {
                                text += "中止鎮靜劑：" + text2.Substring(0, text2.Length - 1) + CRLF;
                            }
                            if (text3 != "")
                            {
                                text += "1.口腔照護：" + text3.Substring(0, text3.Length - 1) + CRLF;
                            }
                            if (text4 != "")
                            {
                                text += "2.床頭抬高：" + text4.Substring(0, text4.Length - 1) + CRLF;
                            }
                            if (text5 != "")
                            {
                                text += "3.排空積水：" + text5.Substring(0, text5.Length - 1) + CRLF;
                            }
                        }
                        else if (type == "BSI")
                        {
                            foreach (DB_NIS_TUBE_ASSESS_BUNDLE BUNDLE in BUNDLES)
                            {
                                if ("bundle_bsi_reason" == BUNDLE.NAME)
                                    text1 += BUNDLE.VALUE + ",";
                                else if ("bundle_bsi_hand" == BUNDLE.NAME)
                                    text2 += BUNDLE.VALUE + ",";
                                else if ("bundle_bsi_date_start_date" == BUNDLE.NAME)
                                    text3 += BUNDLE.VALUE + ",";
                                else if ("bundle_bsi_date_end_date" == BUNDLE.NAME)
                                    text4 += BUNDLE.VALUE + ",";
                                else if ("bundle_bsi_position" == BUNDLE.NAME)
                                    text5 += BUNDLE.VALUE + ",";
                                //else if ("bundle_bsi_complication" == BUNDLE.NAME)
                                //    text6 += BUNDLE.VALUE + ",";
                                else if ("bundle_bsi_copied_type" == BUNDLE.NAME)
                                    text6 += BUNDLE.VALUE + ",";
                                else if ("bundle_bsi_complication_infection" == BUNDLE.NAME)
                                    text7 += BUNDLE.VALUE + ",";
                                else if ("bundle_bsi_skin_disinfect" == BUNDLE.NAME)
                                    text8 += BUNDLE.VALUE + ",";
                                else if ("bundle_bsi_tube_disinfect" == BUNDLE.NAME)
                                    text9 += BUNDLE.VALUE + ",";
                            }
                            if (text1 != "")
                            {
                                text += "導管是否需要留存：" + text1.Substring(0, text1.Length - 1) + CRLF;
                            }
                            if (text2 != "")
                            {
                                text += "1.手部衛生：" + text2.Substring(0, text2.Length - 1) + CRLF;
                            }
                            if (text6 != "")
                            {
                                text += "2.敷料種類：" + text6.Substring(0, text6.Length - 1) + CRLF;
                            }
                            if (text3 != "")
                            {
                                text += "3-1.敷料有效起始日：" + text3.Substring(0, text3.Length - 1) + CRLF;
                            }
                            if (text4 != "")
                            {
                                text += "3-2.敷料有效截止日：" + text4.Substring(0, text4.Length - 1) + CRLF;
                            }
                            if (text5 != "")
                            {
                                text += "4.置放部位無紅、腫、熱、痛等情形：" + text5.Substring(0, text5.Length - 1) + CRLF;
                            }
                            //if (text6 != "")
                            //{
                            //    text += "合併症：" + text6.Substring(0, text6.Length - 1) + CRLF;
                            //}
                            if (text7 != "")
                            {
                                text += "是否感染：" + text7.Substring(0, text7.Length - 1) + CRLF;
                            }
                            if (text8 != "")
                            {
                                text += "5.消毒皮膚方式：" + text8.Substring(0, text8.Length - 1) + CRLF;
                            }
                            if (text9 != "")
                            {
                                text += "管路照護消毒：" + text9.Substring(0, text9.Length - 1) + CRLF;
                            }
                        }
                        json_result.status = RESPONSE_STATUS.SUCCESS;
                        json_result.message = text;
                    }
                }
            }
            return Json(JsonConvert.SerializeObject(json_result));
        }

        //LU 懸浮視窗資料傳遞 20230116 暫時未使用
        public string SELECT_BundleCare2(string ass_id)
        {
            string feeno = ptinfo.FeeNo;
            RESPONSE_MSG json_result = new RESPONSE_MSG();
            string text = "";

            if (ass_id != null)
            {
                DataTable dt_master = tubem.sel_tube_bundle_master(feeno, ass_id, "");

                if (dt_master.Rows.Count > 0)
                {
                    string table_id = dt_master.Rows[0]["TABLE_ID"].ToString();

                    DataTable dt_detail = tubem.sel_tube_bundle_datail(table_id);

                    List<DB_NIS_TUBE_ASSESS_BUNDLE> BUNDLES = new List<DB_NIS_TUBE_ASSESS_BUNDLE>();
                    //DB_NIS_TUBE_ASSESS_BUNDLE BUNDLE = new DB_NIS_TUBE_ASSESS_BUNDLE();

                    foreach (DataRow row in dt_detail.Rows)
                    {
                        BUNDLES.Add(new DB_NIS_TUBE_ASSESS_BUNDLE
                        {
                            NAME = row["ITEM_ID"].ToString(),
                            VALUE = row["ITEM_VALUE"].ToString(),
                            TYPE = row["ITEM_TYPE"].ToString()
                        });
                    }

                    string type = dt_master.Rows[0]["BC_TYPE"].ToString();

                    string text1 = "";
                    string text2 = "";
                    string text3 = "";
                    string text4 = "";
                    string text5 = "";
                    string text6 = "";
                    string text7 = "";
                    string text8 = "";
                    string text9 = "";

                    if ("UTI" == type)
                    {
                        foreach (DB_NIS_TUBE_ASSESS_BUNDLE BUNDLE in BUNDLES)
                        {
                            if ("bundle_uti_reason" == BUNDLE.NAME)
                                text1 += BUNDLE.VALUE;
                            else if ("bundle_uti_hand" == BUNDLE.NAME)
                                text2 += BUNDLE.VALUE;
                            else if ("bundle_uti_tube" == BUNDLE.NAME)
                                text3 += BUNDLE.VALUE;
                            else if ("bundle_uti_bag" == BUNDLE.NAME)
                                text4 += BUNDLE.VALUE;
                            else if ("bundle_uti_clean_condition" == BUNDLE.NAME)
                                text5 += BUNDLE.VALUE;
                            else if ("bundle_uti_clean_method" == BUNDLE.NAME)
                                text6 += BUNDLE.VALUE;
                        }
                        text += "導管需要留存原因：" + text1 + CRLF;
                        text += "手部衛生：" + text2 + CRLF;
                        text += "密閉、無菌且通暢的引流系統，避免管路扭曲或壓折：" + text3 + CRLF;
                        text += "集尿袋不可超過8分滿：" + text4 + CRLF;
                        text += "尿道口清潔無分泌物及異味：" + text5 + CRLF;
                        text += "尿道口清潔方式：" + text6 + CRLF;
                    }
                    else if ("VAP" == type)
                    {

                        foreach (DB_NIS_TUBE_ASSESS_BUNDLE BUNDLE in BUNDLES)
                        {
                            if ("bundle_vap_reason" == BUNDLE.NAME)
                                text1 += BUNDLE.VALUE;
                            else if ("bundle_vap_sedation" == BUNDLE.NAME)
                                text2 += BUNDLE.VALUE;
                            else if ("bundle_vap_mouth" == BUNDLE.NAME)
                                text3 += BUNDLE.VALUE;
                            else if ("bundle_vap_bed_height" == BUNDLE.NAME)
                                text4 += BUNDLE.VALUE;
                            else if ("bundle_vap_effusion" == BUNDLE.NAME)
                                text5 += BUNDLE.VALUE;
                        }
                        text += "使用呼吸器適應症：" + text1 + CRLF;
                        text += "中止鎮靜劑：" + text2 + CRLF;
                        text += "口腔照護：" + text3 + CRLF;
                        text += "床頭抬高：" + text4 + CRLF;
                        text += "排空積水：" + text5 + CRLF;
                    }
                    else if ("BSI" == type)
                    {
                        foreach (DB_NIS_TUBE_ASSESS_BUNDLE BUNDLE in BUNDLES)
                        {
                            if ("bundle_bsi_reason" == BUNDLE.NAME)
                                text1 += BUNDLE.VALUE;
                            else if ("bundle_bsi_hand" == BUNDLE.NAME)
                                text2 += BUNDLE.VALUE;
                            else if ("bundle_bsi_date_start_date" == BUNDLE.NAME)
                                text3 += BUNDLE.VALUE;
                            else if ("bundle_bsi_date_end_date" == BUNDLE.NAME)
                                text4 += BUNDLE.VALUE;
                            else if ("bundle_bsi_position" == BUNDLE.NAME)
                                text5 += BUNDLE.VALUE;
                            else if ("bundle_bsi_complication" == BUNDLE.NAME)
                                text6 += BUNDLE.VALUE;
                            else if ("bundle_bsi_complication_infection" == BUNDLE.NAME)
                                text7 += BUNDLE.VALUE;
                            else if ("bundle_bsi_skin_disinfect" == BUNDLE.NAME)
                                text8 += BUNDLE.VALUE;
                            else if ("bundle_bsi_tube_disinfect" == BUNDLE.NAME)
                                text9 += BUNDLE.VALUE;
                        }
                        text += "導管是否需要留存：" + text1 + CRLF;
                        text += "手部衛生：" + text2 + CRLF;
                        text += "敷料有效起始日：" + text3 + CRLF;
                        text += "敷料有效截止日：" + text4 + CRLF;
                        text += "置放部位有無紅、腫、熱、痛等情形：" + text5 + CRLF;
                        text += "合併症：" + text6 + CRLF;
                        text += "是否感染：" + text7 + CRLF;
                        text += "消毒皮膚方式：" + text8 + CRLF;
                        text += "管路照護消毒：" + text9 + CRLF;
                    }
                    json_result.status = RESPONSE_STATUS.SUCCESS;
                    json_result.message = text;
                }
            }
            return text;
        }

        //LU 20230117 更新bundle 未使用
        public ActionResult Update_BundleCare1(List<DB_NIS_TUBE_ASSESS_BUNDLE1> model)
        {
            string feeno = ptinfo.FeeNo;
            string userno = userinfo.EmployeesNo;
            string username = userinfo.EmployeesName;
            string ass_id = model[0].ASSID;
            string table_id = "";
            string UPDATE_TIME = DateTime.Now.ToString("yyyy/MM/dd HH:mm");
            RESPONSE_MSG json_result = new RESPONSE_MSG();

            if (ass_id == "")
            {
                json_result.status = RESPONSE_STATUS.ERROR;
                json_result.message = "管路照護未更新成功";

                return Json(JsonConvert.SerializeObject(json_result));
            }

            DataTable dt_master = tubem.sel_tube_bundle_master("", ass_id, "");
            if (dt_master.Rows.Count > 0)
                table_id = dt_master.Rows[0]["TABLE_ID"].ToString();

            if (table_id == "")
            {
                json_result.status = RESPONSE_STATUS.ERROR;
                json_result.message = "管路照護未更新成功";

                return Json(JsonConvert.SerializeObject(json_result));
            }

            //更新主表
            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("UPDATE_ID", userno, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("UPDATE_NAME", username, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("UPDATE_TIME", UPDATE_TIME, DBItem.DBDataType.DataTime));
            string where = " ASSESS_ID = '" + ass_id + "' ";
            where += "AND TABLE_ID = '" + table_id + "' ";

            int erow = tubem.DBExecUpdate("NIS_TUBE_ASSESS_BUNDLE_MASTER", insertDataList, where);

            //更新細項
            if (erow > 0)
            {
                erow = tubem.DBExecDelete("NIS_TUBE_ASSESS_BUNDLE_DETAIL", " TABLEID = '" + table_id + "' ");
                if (erow > 0)
                {
                    for (int i = 1; i < model.Count; i++)
                    {
                        insertDataList.Clear();
                        string SERIAL = this.creatid("BUNDLE_DETAIL", userno, feeno, i.ToString());
                        insertDataList.Add(new DBItem("SERIAL", SERIAL, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("TABLE_ID", table_id, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("ITEM_ID", model[i].NAME, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("ITEM_TYPE", model[i].TYPE, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("ITEM_VALUE", model[i].VALUE, DBItem.DBDataType.String));
                        erow = tubem.DBExecInsert("NIS_TUBE_ASSESS_BUNDLE_DETAIL", insertDataList);
                    }

                    if (erow > 0)
                    {
                        json_result.status = RESPONSE_STATUS.SUCCESS;
                        json_result.message = "管路照護更新成功!";
                        return Json(JsonConvert.SerializeObject(json_result));
                    }
                }
            }
            json_result.status = RESPONSE_STATUS.ERROR;
            json_result.message = "管路照護更新失敗!";
            return Json(JsonConvert.SerializeObject(json_result));
        }

        /// <summary>
        /// LU 20230118 更新bundle 修改版
        /// </summary>
        /// <param name="ass_id"></param>
        /// <param name="model"></param>
        /// <returns></returns>        
        public ActionResult Update_BundleCare(string ass_id, string tube_id, string tube_row, string bc_type, string record_time, List<DB_NIS_TUBE_ASSESS_BUNDLE> model)
        {
            RESPONSE_MSG json_result = new RESPONSE_MSG();
            if (ass_id == "")
            {
                json_result.status = RESPONSE_STATUS.ERROR;
                json_result.message = "管路照護無法更新";
                return Json(JsonConvert.SerializeObject(json_result));
            }
            string feeno = ptinfo.FeeNo;
            string table_id;
            DataTable dt_master = tubem.sel_tube_bundle_master(feeno, ass_id, "");
            if (dt_master.Rows.Count > 0)
                table_id = dt_master.Rows[0]["TABLE_ID"].ToString();
            else
            {
                if (model != null)
                {
                    return Insert_BundleCare(ass_id, tube_id, tube_row, bc_type, record_time, model);
                }
                else
                {
                    json_result.status = RESPONSE_STATUS.EXCEPTION;
                    json_result.message = "管路照護不須紀錄!";
                    return Json(JsonConvert.SerializeObject(json_result));
                }
            }

            string userno = userinfo.EmployeesNo;
            string username = userinfo.EmployeesName;
            string UPDATE_TIME = DateTime.Now.ToString("yyyy/MM/dd HH:mm");
            //更新主表
            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("UPDATE_ID", userno, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("UPDATE_NAME", username, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("UPDATE_TIME", UPDATE_TIME, DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("RECORDTIME", record_time, DBItem.DBDataType.DataTime));
            string where = " ASSESS_ID = '" + ass_id + "' ";
            where += "AND TABLE_ID = '" + table_id + "' ";

            int erow = tubem.DBExecUpdate("NIS_TUBE_ASSESS_BUNDLE_MASTER", insertDataList, where);
            //更新細項
            if (erow > 0)
            {
                DataTable dt = tubem.sel_tube_bundle_datail(table_id);
                if (dt.Rows.Count > 0)
                    erow = tubem.DBExecDelete("NIS_TUBE_ASSESS_BUNDLE_DETAIL", " TABLE_ID = '" + table_id + "' ");
                if (erow > 0)
                {
                    if (model != null)
                    {
                        for (int i = 0; i < model.Count; i++)
                        {
                            string SERIAL = this.creatid("BUNDLE_DETAIL", userno, feeno, i.ToString());
                            insertDataList.Clear();
                            insertDataList.Add(new DBItem("SERIAL", SERIAL, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("TABLE_ID", table_id, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("ITEM_ID", model[i].NAME, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("ITEM_TYPE", model[i].TYPE, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("ITEM_VALUE", model[i].VALUE, DBItem.DBDataType.String));
                            erow = tubem.DBExecInsert("NIS_TUBE_ASSESS_BUNDLE_DETAIL", insertDataList);
                        }
                    }
                    if (erow > 0)
                    {
                        //LU 20230621 Bundle 拋護理紀錄 更新
                        string content = BundleTransferContent(bc_type, model);
                        if (!content.IsEmpty())
                        {
                            string title = bc_type == "UTI" ? "導尿管每日照護紀錄" : bc_type == "VAP" ? "呼吸器使用每日照護紀錄" : "中心導管每日照護紀錄";
                            erow = base.Upd_CareRecord(record_time, ass_id, title, content, "", "", "", "", "TUBE_BUNDLE_ASSESS");

                            if (erow > 0)
                            {
                                json_result.status = RESPONSE_STATUS.SUCCESS;
                                json_result.message = "管路照護更新成功!";
                                return Json(JsonConvert.SerializeObject(json_result));
                            }
                            else
                            {
                                erow = base.Insert_CareRecord(record_time, ass_id, title, content, "", "", "", "", "TUBE_BUNDLE_ASSESS");
                                if (erow > 0)
                                {
                                    json_result.status = RESPONSE_STATUS.SUCCESS;
                                    json_result.message = "管路照護更新成功!";
                                    return Json(JsonConvert.SerializeObject(json_result));
                                }
                            }
                        }
                        else
                        {
                            erow += base.Del_CareRecord(ass_id, "TUBE_BUNDLE_ASSESS");
                            if (erow > 0)
                            {
                                json_result.status = RESPONSE_STATUS.SUCCESS;
                                json_result.message = "管路照護新增成功!";
                                return Json(JsonConvert.SerializeObject(json_result));
                            }
                        }
                    }
                }
            }
            json_result.status = RESPONSE_STATUS.ERROR;
            json_result.message = "管路照護更新失敗!";
            return Json(JsonConvert.SerializeObject(json_result));
        }

        /// <summary>
        ///BundleCare 轉護理紀錄文字模板 LU 20230620 
        /// </summary>
        /// <param name="bc_type">評估種類</param>
        /// <param name="models">評估選項</param>
        /// <returns>護理紀錄文字</returns>      
        public string BundleTransferContent(string bc_type, List<DB_NIS_TUBE_ASSESS_BUNDLE> models)
        {
            string context = "";
            if (models != null && bc_type != null)
            {
                switch (bc_type)
                {
                    // UTI模板
                    case "UTI":
                        string uti_reason = "";
                        string uti_hand = "";
                        string uti_position = "";
                        string uti_position_reason = "";
                        string uti_tube = "";
                        string bag = "";
                        string clean_condition = "";
                        string method = "";
                        foreach (var model in models)
                        {
                            var name = model.NAME;
                            var value = model.VALUE;
                            switch (name)
                            {
                                case "bundle_uti_reason":
                                case "bundle_uti_reason_other":
                                    if ("其他" != value)
                                    {
                                        uti_reason += value + "、";
                                    }
                                    break;
                                case "bundle_uti_hand":
                                    uti_hand = "是" == value ? "執行導尿管照護前、後，有進行手部衛生。" : "執行導尿管照護前、後，沒有進行手部衛生。";
                                    break;
                                case "bundle_uti_position":
                                    //uti_position = "大腿" == value ? "導尿管固定位置於大腿。" : "導尿管固定位置於下腹部。";
                                    if (value != "")
                                    {
                                        uti_position = "導尿管固定位置於" + value + "。";
                                    }
                                    else
                                    {
                                        uti_position = "";
                                    }
                                    break;
                                case "bundle_uti_position_reason_ck":
                                    uti_position_reason += value + "、";
                                    break;
                                case "bundle_uti_position_reason":
                                    uti_position_reason = uti_position_reason.Replace("其他", value);
                                    break;
                                case "bundle_uti_tube":
                                    uti_tube = "是" == value ? "密閉、無菌且通暢的引流系統，有避免管路扭曲或壓折。" : "密閉、無菌且通暢的引流系統，沒有避免管路扭曲或壓折。";
                                    break;
                                case "bundle_uti_bag":
                                    bag = "是" == value ? "集尿袋未超過8分滿。" : "集尿袋超過8分滿。";
                                    break;
                                case "bundle_uti_clean_condition":
                                    clean_condition = "是" == value ? "執行尿道口清潔。" : "未執行尿道口清潔。";
                                    break;
                                case "bundle_uti_clean_method":
                                    if ("其他" != value && "無此需求" != value)
                                    {
                                        method = "以" + value + "進行尿道口清潔。";
                                    }
                                    break;
                                case "bundle_uti_clean_method_other":
                                    method = "以" + value + "進行尿道口清潔。";
                                    break;
                            }
                        }
                        if (uti_reason != "")
                        {
                            uti_reason = uti_reason.Substring(0, uti_reason.Length - 1);
                            uti_reason = "導尿管留存原因：" + uti_reason + "。";
                        }
                        if (uti_position_reason != "")
                        {
                            uti_position_reason = uti_position_reason.Substring(0, uti_position_reason.Length - 1);
                            uti_position_reason = "未放置下腹部原因為" + uti_position_reason + "。";
                        }
                        if (uti_hand != "" && uti_position != "" && uti_tube != "" && bag != "")
                        {
                            context = "病人" +
                            uti_reason +
                            "導尿管每日照護紀錄：" +
                            uti_hand +
                            uti_position +
                            uti_position_reason +
                            uti_tube +
                            bag +
                            clean_condition +
                            method;
                        }
                        break;

                    // VAP模板
                    case "VAP":
                        string vap_reason = "";
                        string sedation = "";
                        int sedationLength = 0;
                        string mouth = "";
                        string height = "";
                        int heightLength = 0;
                        string effusion = "";
                        foreach (var model in models)
                        {
                            var name = model.NAME;
                            var value = model.VALUE;
                            switch (name)
                            {
                                case "bundle_vap_reason":
                                case "bundle_vap_reason_other":
                                    if ("其他" != value)
                                    {
                                        vap_reason += value + "、";
                                    }
                                    break;
                                case "bundle_vap_sedation":
                                case "bundle_vap_sedation_none":
                                case "bundle_vap_sedation_other":
                                    if ("未使用鎮定劑" == value)
                                    {
                                        sedation = "未使用鎮定劑。";
                                    }
                                    else if ("是" == value)
                                    {
                                        sedation = "中止鎮靜劑使用。";
                                    }
                                    else
                                    {
                                        if ("其他" != value && "否" != value)
                                        {
                                            sedation += value + "、";
                                        }
                                        sedationLength = sedation.Length;
                                    }
                                    break;
                                case "bundle_vap_mouth":
                                    mouth = "是" == value ? "執行口腔照護。" : "未執行口腔照護。";
                                    break;
                                case "bundle_vap_bed_height":
                                case "bundle_vap_bed_height_none":
                                case "bundle_vap_bed_height_other":
                                    if ("是" == value)
                                    {
                                        height = "進行床頭抬高30度 ~ 40度。";
                                    }
                                    else
                                    {
                                        if ("其他" != value && "否" != value)
                                        {
                                            height += value + "、";
                                        }
                                        heightLength = height.Length;
                                    }
                                    break;
                                case "bundle_vap_effusion":
                                    effusion = "是" == value ? "執行呼吸器管路積水排空。" : "未執行呼吸器管路積水排空。";
                                    break;
                            }
                        }
                        if (vap_reason != "")
                        {
                            vap_reason = vap_reason.Substring(0, vap_reason.Length - 1);
                            vap_reason = "呼吸器使用適應症：" + vap_reason + "。";
                        }
                        if (mouth != "" && height != "" && effusion != "")
                        {
                            if (sedationLength > 0)
                            {
                                sedation = sedation.Substring(0, sedationLength - 1);
                                sedation = "未中止鎮靜劑使用，原因：" + sedation + "。";
                            }
                            if (heightLength > 0)
                            {
                                height = height.Substring(0, heightLength - 1);
                                height = "未進行床頭抬高，原因：" + height + "。";
                            }
                            context = "病人" +
                                vap_reason +
                                "呼吸器使用每日照護紀錄：" +
                                //sedation +
                                mouth +
                                height +
                                effusion;
                        }
                        break;

                    // BSI模板
                    case "BSI":
                        string bsi_reason = "";
                        string bsi_hand = "";
                        string bsi_copied = "";
                        string start = "";
                        string end = "";
                        string position = "";
                        string skin = "";
                        int skinLength = 0;
                        //string bsi_tube = "";
                        foreach (var model in models)
                        {
                            var name = model.NAME;
                            var value = model.VALUE;
                            switch (name)
                            {
                                case "bundle_bsi_reason":
                                    bsi_reason = "是" == value ? "中心導管有留存需求。" : "中心導管無留存需求。";
                                    break;
                                case "bundle_bsi_hand":
                                    bsi_hand = "是" == value ? "照護中心導管前確實執行手部衛生。" : "照護中心導管前未確實執行手部衛生。";
                                    break;
                                case "bundle_bsi_copied_type":
                                    bsi_copied = "使用" + value + "敷料，";
                                    break;
                                case "bundle_bsi_copied_type_other":
                                    bsi_copied = bsi_copied.Replace("其他", value);
                                    break;
                                case "bundle_bsi_date_start_date":
                                    start = value;
                                    break;
                                case "bundle_bsi_date_end_date":
                                    end = value;
                                    break;
                                case "bundle_bsi_position":
                                    position = "是" == value ? "評估中心導管置放部位無紅、腫、熱、痛等情形。" : "評估中心導管置放部位有紅、腫、熱、痛等情形。";
                                    break;
                                case "bundle_bsi_skin_disinfect":
                                    if ("本日不須更換敷料" == value)
                                    {
                                        skin = "敷料未到期故本日不須更換敷料。";
                                    }
                                    else
                                    {
                                        skin += value.Substring(2) + "、";
                                        skinLength = skin.Length;
                                    }
                                    break;
                                    //case "bundle_bsi_tube_disinfect":
                                    //    if ("有執行管路照護但未消毒" == value)
                                    //    {
                                    //        bsi_tube = "有執行管路照護但未消毒。";
                                    //    }
                                    //    else if ("本日未執行管路照護工作" == value)
                                    //    {
                                    //        bsi_tube = "本日未執行管路照護工作。";
                                    //    }
                                    //    else
                                    //    {
                                    //        bsi_tube += value + "、";
                                    //    }
                                    //    break;
                            }
                        }
                        if (bsi_hand != "" && bsi_copied != "" && start != "" && start != "" && position != "" && skin != "")
                        {
                            if (skinLength > 0)
                            {
                                skin = skin.Substring(0, skin.Length - 1);
                                skin = "更換敷料前以" + skin + "進行皮膚消毒。";
                            }

                            //bsi_tube = bsi_tube.Substring(0, bsi_tube.Length - 1);
                            //bsi_tube += "進行管路照護消毒。";

                            context = "病人" +
                                bsi_reason +
                                "中心導管每日照護紀錄：" +
                                bsi_hand +
                                bsi_copied +
                                "敷料有效日期：" + start + "~" + end + "。" +
                                position +
                                skin;
                        }
                        break;
                }
            }

            return context;
        }

        /// <summary>
        /// 拋提醒跑馬燈
        /// </summary>
        /// <param name="feeno">住院序號</param>
        /// <param name="userno">使用者</param>
        /// <param name="CREATE_TIME">新增時間</param>
        /// <param name="bc_type">評估種類</param>
        /// <returns>0:失敗 1:成功</returns>
        public int ConfirmNotice(string feeno, string userno, string CREATE_TIME, string bc_type)
        {
            List<DBItem> insertDataList = new List<DBItem>();
            DateTime STARTTIME = DateTime.Parse(CREATE_TIME).Date.AddDays(1);
            DataTable dt = tubem.sel_bundle_notice_id(feeno, bc_type);
            int erow = 0;
            string ntID = "NIS_TUBE_ASSESS_BUNDLE_MASTER_" + feeno + "_" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + "_" + bc_type;

            if (dt.Rows.Count > 0)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    string preId = dt.Rows[i]["NT_ID"].ToString();
                    if (!preId.IsEmpty())
                    {
                        insertDataList.Clear();
                        insertDataList.Add(new DBItem("TIMEOUT", CREATE_TIME, DBItem.DBDataType.DataTime));
                        erow = link.DBExecUpdate("DATA_NOTICE", insertDataList, "NT_ID = '" + preId + "' ");
                    }
                }

            }
            insertDataList.Clear();
            insertDataList.Add(new DBItem("NT_ID", ntID, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FEE_NO", feeno, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("MEMO", "此病人" + bc_type + "管路每日照護紀錄已超過24小時", DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("STARTTIME", STARTTIME.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("ACTIONLINK", "", DBItem.DBDataType.String));
            erow = tubem.DBExecInsert("DATA_NOTICE", insertDataList);


            return erow;
        }

        #endregion

        #region 管路維護

        [HttpGet]
        public ActionResult Tube_Kind_Maintain()
        {
            ViewData["dt"] = tubem.sel_tubekind("", "0");
            return View();
        }

        [HttpPost]
        public string Tube_Kind_Maintain(string JsonString)
        {
            List<Dictionary<string, string>> dt = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(JsonString);
            bool success = true;
            string Msg = string.Empty;
            List<DBItem> dbItemList;

            try
            {
                foreach (var item in dt)
                {
                    dbItemList = new List<DBItem>();
                    dbItemList.Add(new DBItem("ASSESS_TYPE", item["AssessType"], DBItem.DBDataType.String));
                    if (this.link.DBExecUpdateTns("TUBE_KIND", dbItemList, " KINDID = '" + item["KindID"] + "' ") > 0)
                        success = true;
                    else
                    {
                        success = false;
                        break;
                    }
                }

                if (success)
                {
                    this.link.DBCommit();
                    Msg = "更新成功！";
                }
                else
                {
                    this.link.DBRollBack();
                    Msg = "更新失敗！";
                }
            }
            catch (Exception ex)
            {
                Msg = "更新失敗！";
                if (ex.ToString() != "DBCommit Fail")
                {
                    this.link.DBRollBack();
                }
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }
            finally
            {
                this.link.DBClose();
            }

            return Msg;
        }

        #endregion

        public string getTubeBillSet (string tubeID)
        {
            string result = "";
            string sql = "SELECT * FROM TUBE_KIND WHERE KINDID = '" + tubeID + "'";
            DataTable dt = new DataTable();

            link.DBExecSQL(sql, ref dt);

            if(dt.Rows.Count > 0 )
            {
                result = dt.Rows[0]["BILL_SET"].ToString();
            }

            return result;
        }
    }
}