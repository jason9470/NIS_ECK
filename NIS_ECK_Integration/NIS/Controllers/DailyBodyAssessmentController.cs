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
using System.Collections;
using System.Data.OleDb;
using System.Text.RegularExpressions;

namespace NIS.Controllers
{
    public class DailyBodyAssessmentController : BaseController
    {
        private DailyBodyAssessment DailyBodyAssessment;
        private DBConnector link;

        public DailyBodyAssessmentController()
        {
            this.DailyBodyAssessment = new DailyBodyAssessment();
            this.link = new DBConnector();
        }

        //每日身體評估
        public ActionResult Index(string modeval)
        {
            if(Session["PatInfo"] != null)
            {
                if(string.IsNullOrWhiteSpace(modeval))
                {
                    //自動切換
                    if(base.ptinfo.Age < 18)
                        return RedirectToAction("DailyBodyAssessListchild");
                    else
                        return RedirectToAction("DailyBodyAssessListadult");
                }
                else
                    return View();
            }

            Response.Write("<script>alert('登入逾時');</script>");
            return new EmptyResult();
        }

        //成人
        public ActionResult DailyBodyAssessListadult(string St, string Ed)
        {
            _DailyBodyAssessList("adult", St, Ed);
            return View();
        }

        //兒童
        public ActionResult DailyBodyAssessListchild(string St, string Ed)
        {
            _DailyBodyAssessList("child", St, Ed);
            return View();
        }

        //取資料
        public void _DailyBodyAssessList(string type, string St, string Ed)
        {
            DataTable DT_M = new DataTable(), DT_D = new DataTable();
            List<DailyBodyData> bd = new List<DailyBodyData>();
            string feeno = ptinfo.FeeNo, userno = userinfo.EmployeesNo, TempUserName = "";
            string[] ArrItem = { };
            if(string.IsNullOrWhiteSpace(St))
                St = DateTime.Now.AddDays(-7).ToString("yyyy/MM/dd HH:mm"); //預設查詢區間為目前至一週前
            if(string.IsNullOrWhiteSpace(Ed))
                Ed = DateTime.Now.ToString("yyyy/MM/dd HH:mm");
            DT_M = DailyBodyAssessment.sel_dailybodyassess_data_M(feeno, type, St, Ed);//取主表的值
            DT_D = DailyBodyAssessment.sel_dailybodyassess_data_D(feeno, type, St, Ed);//取子表的值
            int Dt_D = DT_D.Rows.Count;
            for(int i = 0; i < DT_M.Rows.Count; i++)
            {//先過濾ID後，再裝成新的DataTable
                DataTable newDT = NIS.Models.DailyBodyAssessment.FiltData(DT_D, "DBAM_ID='" + DT_M.Rows[i]["DBAM_ID"].ToString() + "'", "");
                if(newDT != null && newDT.Rows.Count > 0)
                {
                    Array.Resize(ref ArrItem, newDT.Rows.Count);//重新定義陣列長度
                    for(int j = 0; j < newDT.Rows.Count; j++)
                    {//將A的資料丟成字串陣列
                        ArrItem[j] = newDT.Rows[j]["A"].ToString();
                    }
                }
                //else 
                //{
                //    Array.Resize(ref ArrItem, 0);
                //}
                if(userno != DT_M.Rows[i]["CREANO"].ToString())
                {//登入者和紀錄者資料不相同時
                    userno = DT_M.Rows[i]["CREANO"].ToString();
                    byte[] listByteCode = webService.UserName(userno);
                    if(listByteCode != null)
                    {
                        string listJsonArray = CompressTool.DecompressString(listByteCode);
                        UserInfo other_user_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                        TempUserName = other_user_info.EmployeesName;
                    }
                }
                else
                {
                    if(i==0)
                    TempUserName = userinfo.EmployeesName;
                }
                bd.Add(new NIS.Models.DailyBodyData(DT_M.Rows[i]["DBAM_ID"].ToString().Trim(), DT_M.Rows[i]["DBAM_DTM"].ToString().Trim(), ArrItem, DT_M.Rows[i]["CREANO"].ToString().Trim(), TempUserName, DT_M.Rows[i]["DBAM_TEMP_TYPE"].ToString().Trim()));
                ArrItem = null;
            }
            ViewBag.DBAMType = type;
            ViewData["BodyData"] = bd;
            bd = null;
            ViewBag.userno = userinfo.EmployeesNo;
            ViewBag.St = St;
            ViewBag.Ed = Ed;
            ViewBag.RootDocument = GetSourceUrl();
            ViewBag.FeeNo = feeno;

        }

        public ActionResult AddBodyAssessDirect(string type, string DBAMID = "")
        {
            DataTable DT = new DataTable();

            if(DBAMID != "")
            {
                DT = DailyBodyAssessment.sel_dailybodyassess_data(base.ptinfo.FeeNo, DBAMID, type);  //修改時，當筆評估

                DT.Columns.Add("modetype");
                if(DT.Rows.Count > 0)
                {
                    foreach(DataRow r in DT.Rows)
                    {
                        r["modetype"] = "edit";
                    }
                }
            }
            else
            {
                DT = DailyBodyAssessment.sel_dailybodyassess_last_info(base.ptinfo.FeeNo, type);  //新增時，上一筆評估

                DT.Columns.Add("modetype");
                if(DT.Rows.Count > 0)
                {
                    foreach(DataRow r in DT.Rows)
                    {
                        r["modetype"] = "add";
                    }
                }
            }

            SetDailyBasicData();
            ViewBag.SexVal = base.ptinfo.PatientGender;
            ViewBag.DBAMType = type;
            ViewBag.DT = DT;
            ViewBag.ptAge = ptinfo.Age;  //病人年齡
            ViewBag.ptMon = ptinfo.Month;  //病人小於一歲時，月齡
            ViewBag.FeeNo = ptinfo.FeeNo;

            if(type == "adult")
                return View("SetAdultAssess");
            else if(type == "child")
                return View("SetChildAssess");

            return new EmptyResult();
        }

        [HttpPost]
        public ActionResult SetAdultAssess()
        {
            ViewBag.DBAMType = "adult";
            SetDailyBasicData();
            ViewData["ptinfo"] = base.ptinfo;
            return View();
        }

        [HttpPost]
        public ActionResult SetChildAssess()
        {
            ViewBag.DBAMType = "child";
            SetDailyBasicData();
            ViewData["ptinfo"] = base.ptinfo;
            return View();
        }

        [HttpPost]
        public string SelectAdultAssess(string ID, string Type)
        {
            if(Session["PatInfo"] != null)
            {
                DataTable Dt = new DataTable();
                DateTime Nowtime = DateTime.Now;
                if (!string.IsNullOrWhiteSpace(ID))
                {
                    Dt = DailyBodyAssessment.sel_dailybodyassess_data(base.ptinfo.FeeNo, ID, Type);
                    return JsonConvert.SerializeObject(Dt);
                }
                else
                {
                    Dt = DailyBodyAssessment.sel_dailybodyassess_last_info(base.ptinfo.FeeNo, Type);
                    
                    List<DailyAssessment> Dt_list = (List<DailyAssessment>)Dt.ToList<DailyAssessment>();
                    //foreach (DataRow dr in Dt.Rows)
                    //{
                    //    DailyAssessment doc = new DailyAssessment();
                    //    doc.DBAM_ID = dr.Field<string>("DBAM_ID");
                    //    doc.FEENO = dr.Field<string>("FEENO");
                    //    doc.DBAM_TYPE = dr.Field<string>("DBAM_TYPE");
                    //    doc.DBAM_CARE_RECORD = dr.Field<string>("DBAM_CARE_RECORD");
                    //    doc.DELETED = dr.Field<string>("DELETED");
                    //    doc.CREANO = dr.Field<string>("CREANO");
                    //    doc.CREATTIME = dr.Field<DateTime>("CREATTIME");
                    //    doc.UPDNO = dr.Field<object>("UPDNO");
                    //    doc.UPDTIME = dr.Field<object>("UPDTIME");
                    //    doc.DBAM_DTM = dr.Field<DateTime>("DBAM_DTM");
                    //    doc.DBAM_TEMP_TYPE = dr.Field<string>("DBAM_TEMP_TYPE");
                    //    doc.DBAD_ID = dr.Field<string>("DBAD_ID");
                    //    doc.DBAM_ID1 = dr.Field<string>("DBAM_ID1");
                    //    doc.DBAD_ITEMID = dr.Field<string>("DBAD_ITEMID");
                    //    if (doc.DBAD_ITEMID == "TxtDietType99Other")
                    //    {
                    //        List<TextOrder> DietOrder = new List<TextOrder>();
                    //        byte[] ByteCode = webService.GetDietOrder(doc.FEENO);
                    //        //病人資訊
                    //        if (ByteCode != null)
                    //        {
                    //            DietOrder = JsonConvert.DeserializeObject<List<TextOrder>>(CompressTool.DecompressString(ByteCode));
                    //        }
                    //        DietOrder = DietOrder.FindAll(x => x.OrderStartDate <= Nowtime && x.OrderEndDate >= Nowtime);
                    //        doc.DBAD_ITEMVALUE = DietOrder[0].Content;                       
                    //    }
                    //    else
                    //    {
                    //        doc.DBAD_ITEMVALUE = dr.Field<string>("DBAD_ITEMVALUE");
                    //    }
                    //    doc.DBAD_ITEMTYPE = dr.Field<string>("DBAD_ITEMTYPE");
                    //    doc.ASSDATE = dr.Field<string>("ASSDATE");
                    //    doc.ASSTIME = dr.Field<string>("ASSTIME");
                    //    Dt_list.Add(doc);
                    //}
                    byte[] ByteCode = webService.GetDietOrder(ptinfo.FeeNo);
                    //病人資訊
                    if (ByteCode != null)
                    {
                        List<TextOrder> DietOrder = new List<TextOrder>();
                        DietOrder = JsonConvert.DeserializeObject<List<TextOrder>>(CompressTool.DecompressString(ByteCode));
                        DietOrder = DietOrder.FindAll(x => x.OrderStartDate <= Nowtime && x.OrderEndDate >= Nowtime);
                        if (DietOrder.Count > 0)
                        {
                            DailyAssessment da = Dt_list.Find(x => x.DBAD_ITEMID == "TxtDietType99Other");
                            if (da != null)
                            {
                                da.DBAD_ITEMVALUE = DietOrder[0].Content;
                            }
                            else
                            {
                                da = new DailyAssessment();
                                da.DBAD_ITEMID = "TxtDietType99Other";
                                da.DBAD_ITEMTYPE = "text";
                                da.DELETED = "N";
                                da.DBAD_ITEMVALUE = DietOrder[0].Content;
                                Dt_list.Add(da);
                            }
                        }
                        
                    }                   
                    //if (Dt_list.DBAD_ITEMID == "TxtDietType99Other")
                    //{


                    //}
                    //else
                    //{
                    //    doc.DBAD_ITEMVALUE = dr.Field<string>("DBAD_ITEMVALUE");
                    //}
                    return JsonConvert.SerializeObject(Dt_list);
                }
            }
            return "";
        }

        [HttpPost]
        public string SelectChildAssess(string ID, string Type)
        {
            if(Session["PatInfo"] != null)
            {
                DataTable Dt = new DataTable();
                if(!string.IsNullOrWhiteSpace(ID))
                    Dt = DailyBodyAssessment.sel_dailybodyassess_data(base.ptinfo.FeeNo, ID, Type);
                else
                    Dt = DailyBodyAssessment.sel_dailybodyassess_last_info(base.ptinfo.FeeNo, Type);
                return JsonConvert.SerializeObject(Dt);
            }
            return "";
        }

        private void SetDailyBasicData()
        {
            //神經部位
            ViewBag.NervousPosition = DailyBodyAssessment.sel_daly_body_sysparams_data("dailybodyassessment", "nervous_position");
            //疝氣部位
            ViewBag.HerniaPosition = DailyBodyAssessment.sel_daly_body_sysparams_data("dailybodyassessment", "hernia_position");
            //皮膚外觀部位
            ViewBag.SkinPosition = DailyBodyAssessment.sel_daly_body_sysparams_data("dailybodyassessment", "skin_position");
            //禁治療部位
            ViewBag.BanTreatmentPosition = DailyBodyAssessment.sel_daly_body_sysparams_data("dailybodyassessment", "ban_treatment_position");
            //骨骼異常部位
            ViewBag.SkeletonPosition = DailyBodyAssessment.sel_daly_body_sysparams_data("dailybodyassessment", "skeleton_position");
            //尿液顏色
            ViewBag.UrineColor = DailyBodyAssessment.sel_daly_body_sysparams_data("dailybodyassessment", "urine_color");
            //尿液性質
            ViewBag.UrineNature = DailyBodyAssessment.sel_daly_body_sysparams_data("dailybodyassessment", "urine_nature");
            //  //糞便顏色//等資料庫建好，就可以解開 by jarvis
            //  ViewBag.StoolColor = DailyBodyAssessment.sel_daly_body_sysparams_data("dailybodyassessment", "stool_color");
            //糞便性質
            ViewBag.StoolNature = DailyBodyAssessment.sel_daly_body_sysparams_data("dailybodyassessment", "stool_nature");
            //分泌物顏色
            ViewBag.DischargeColor = DailyBodyAssessment.sel_daly_body_sysparams_data("dailybodyassessment", "discharge_color");
            //分泌物性狀
            ViewBag.DischargeNature = DailyBodyAssessment.sel_daly_body_sysparams_data_sorting("dailybodyassessment", "discharge_nature");
            //分泌物性狀-兒童
            ViewBag.DischargeNature_child = DailyBodyAssessment.sel_daly_body_sysparams_data_sorting("dailybodyassessment", "discharge_nature_child");
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult SetDailyAssessData(FormCollection form)
        {
            if(Session["PatInfo"] != null)
            {
                string feeno = ptinfo.FeeNo;
                string userno = userinfo.EmployeesNo;
                string ID = "", DTLID = "";
                int ERow = 0;
                string TempStatus = Request["temp"].ToString();
                string CareString = Request["CareString"].ToString().Replace('＆', '&').Replace('＋','+').Trim();
                Dictionary<string, string> InputType = SetHashtable(form["HidAllID"].ToString().Trim().Split(','), form["HidAllType"].ToString().Trim().Split(','));
                string DBAMDTM = form["TxtDBAMDate"] + " " + form["TxtDBAMTime"];
                bool SaveStatus = false;
                ViewBag.DBAMType = form["HidDBAMType"];

                List<DBItem> DBItemDataList = new List<DBItem>();

                //主檔
                if(string.IsNullOrEmpty(form["HidDBAMID"]))
                {
                    //新增
                    ID = base.creatid("DAILY_BODY_ASSESSMENT_MASTER", userno, feeno, "0");
                    DBItemDataList.Add(new DBItem("DBAM_ID", ID, DBItem.DBDataType.String));
                    DBItemDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                    DBItemDataList.Add(new DBItem("DBAM_DTM", DBAMDTM, DBItem.DBDataType.DataTime));
                    DBItemDataList.Add(new DBItem("DBAM_TYPE", form["HidDBAMType"], DBItem.DBDataType.String));

                    if(!string.IsNullOrEmpty(form["ChkPlanYN"]) && TempStatus != "Y")
                    {
                        DBItemDataList.Add(new DBItem("DBAM_CARE_RECORD", form["ChkPlanYN"], DBItem.DBDataType.String));
                    }
                    DBItemDataList.Add(new DBItem("DELETED", "N", DBItem.DBDataType.String));
                    DBItemDataList.Add(new DBItem("CREANO", userno, DBItem.DBDataType.String));
                    DBItemDataList.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                    if(!string.IsNullOrEmpty(TempStatus) && TempStatus == "Y")
                    {
                        DBItemDataList.Add(new DBItem("DBAM_TEMP_TYPE", "temporary", DBItem.DBDataType.String));//temporary
                    }
                    else
                    {
                        DBItemDataList.Add(new DBItem("DBAM_TEMP_TYPE", "complete", DBItem.DBDataType.String));//complete
                    }
                    ERow = DailyBodyAssessment.DBExecInsert("DAILY_BODY_ASSESSMENT_MASTER", DBItemDataList);
                }
                else
                {
                    SaveStatus = true;
                    //修改
                    ID = form["HidDBAMID"];
                    if(!string.IsNullOrEmpty(form["ChkPlanYN"]) && TempStatus != "Y")
                    {
                        DBItemDataList.Add(new DBItem("DBAM_CARE_RECORD", form["ChkPlanYN"], DBItem.DBDataType.String));
                    }
                    DBItemDataList.Add(new DBItem("DBAM_DTM", DBAMDTM, DBItem.DBDataType.DataTime));
                    DBItemDataList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
                    DBItemDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                    if(!string.IsNullOrEmpty(TempStatus) && TempStatus == "Y")
                    {
                        DBItemDataList.Add(new DBItem("DBAM_TEMP_TYPE", "temporary", DBItem.DBDataType.String));//temporary
                    }
                    else
                    {
                        DBItemDataList.Add(new DBItem("DBAM_TEMP_TYPE", "complete", DBItem.DBDataType.String));//complete
                    }
                    ERow = DailyBodyAssessment.DBExecUpdate("DAILY_BODY_ASSESSMENT_MASTER", DBItemDataList, string.Format("FEENO={0} AND DBAM_ID={1}", DigitFlapTemperature.SQLString(feeno), DigitFlapTemperature.SQLString(ID)));
                }

                //細項檔
                if(ERow > 0)
                {
                    //每次修改Master，Detail 都會清空再新增
                    ERow += DailyBodyAssessment.DBExecDelete("DAILY_BODY_ASSESSMENT_DETAIL", string.Format("DBAM_ID={0}", DigitFlapTemperature.SQLString(ID)));

                    foreach(KeyValuePair<string, string> DE in InputType)
                    {
                        int i = 0;
                        string ItemID = DE.Key.ToString();
                        string ItemType = DE.Value.ToString();
                        if(form[ItemID] != null && form[ItemID].ToString().Trim() != "" && ItemID != "HidAllID" && ItemID != "HidAllType")
                        {
                            string ItemValue = form[ItemID];
                            if(ItemType == "textarea" || ItemType == "text")
                            {
                                //ItemValue = Regex.Replace(ItemValue, @"[\W_]+", "");//jarvis add 過濾全部特殊字元的狀態 但後來不採用201610271638
                                if(ItemID == "param_taboo_position_txt" || ItemID == "param_taboo_position_other")
                                {//為了處理 禁治療部位 的多欄位共存一個name，改採※＆分隔儲存 //jarvis add 201610281145
                                    ItemValue = ItemValue.Trim().Replace(",", "※＆");//Split by multiple characters by jarvis
                                }
                                ItemValue = this.DailyBodyAssessment.trans_special_code_with_Daily_Body(ItemValue);
                            }
                            DBItemDataList.Clear();
                            DTLID = base.creatid("DAILY_BODY_ASSESSMENT_DETAIL", userno, feeno, i.ToString());
                            DBItemDataList.Add(new DBItem("DBAD_ID", DTLID, DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("DBAM_ID", ID, DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("DBAD_ITEMID", ItemID, DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("DBAD_ITEMVALUE", ItemValue.Replace("'", "’").Replace("\r\n", ""), DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("DBAD_ITEMTYPE", ItemType, DBItem.DBDataType.String));
                            ERow += DailyBodyAssessment.DBExecInsert("DAILY_BODY_ASSESSMENT_DETAIL", DBItemDataList);
                            i++;
                        }
                    }
                }

                if(ERow > 0)
                {
                    //這裡要改---------------------------------------------------------
                    if(!string.IsNullOrEmpty(CareString) && form["ChkPlanYN"] == "Y" && TempStatus != "Y")
                    {//1.過濾掉暫存的狀態 2.護理記錄字串不能為空 3.帶記錄的勾勾要勾
                        #region  護理紀錄
                        string CareRecordCont = string.Empty;
                        if(form["HidDBAMType"] != "child")
                        {
                            #region 成人身體評估--護理記錄組字 (20161102///20161216修正 by jarvis)
                            /* CareRecordCont = "病人意識 - E：" + form["RbnAwarenessComa_E"] + " V：" + form["RbnAwarenessComa_V"] + " M：" + form["RbnAwarenessComa_M"]
                                + "，Pupil size：R";
                            //CareRecordCont += ((form["RbnPupilItemRef_R"] != "無法睜眼") ? form["RbnPupilItemSize_R"] + form["RbnPupilItemRef_R"] : form["RbnPupilItemRef_R"]);
                            if(form["RbnPupilItemRef_R"] != "無法睜眼")
                            {
                                if(form["RbnPupilItemRef_R"] != "無法評估")
                                {
                                    CareRecordCont += form["RbnPupilItemSize_R"] + form["RbnPupilItemRef_R"];
                                }
                                else
                                {//無法評估
                                    CareRecordCont += form["RbnPupilItemRef_R"] + ":" + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["Txt_RbnPupilItemRef_R_Other"]);
                                }
                            }
                            else
                            {//無法睜眼
                                CareRecordCont += form["RbnPupilItemRef_R"];
                            }
                            CareRecordCont += "/L";
                            //CareRecordCont += ((form["RbnPupilItemRef_L"] != "無法睜眼") ? form["RbnPupilItemSize_L"] + form["RbnPupilItemRef_L"] : form["RbnPupilItemRef_L"]);
                            if(form["RbnPupilItemRef_L"] != "無法睜眼")
                            {
                                if(form["RbnPupilItemRef_L"] != "無法評估")
                                {
                                    CareRecordCont += form["RbnPupilItemSize_L"] + form["RbnPupilItemRef_L"];
                                }
                                else
                                {//無法評估
                                    CareRecordCont += form["RbnPupilItemRef_L"] + ":" + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["Txt_RbnPupilItemRef_L_Other"]);
                                }
                            }
                            else
                            {//無法睜眼
                                CareRecordCont += form["RbnPupilItemRef_L"];
                            }
                            CareRecordCont += "，肌肉力量：上肢Ｒ " + form["RbnAwarenessMuscleRU"]
                                + ((!string.IsNullOrWhiteSpace(form["Txt_AwarenessMuscleRU_Other"])) ? ":" + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["Txt_AwarenessMuscleRU_Other"]) : "")
                                + "/L " + form["RbnAwarenessMuscleLU"]
                                + ((!string.IsNullOrWhiteSpace(form["Txt_AwarenessMuscleLU_Other"])) ? ":" + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["Txt_AwarenessMuscleLU_Other"]) : "")
                                + "、下肢Ｒ " + form["RbnAwarenessMuscleRD"]
                                + ((!string.IsNullOrWhiteSpace(form["Txt_AwarenessMuscleRD_Other"])) ? ":" + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["Txt_AwarenessMuscleRD_Other"]) : "")
                                + "/L " + form["RbnAwarenessMuscleLD"]
                                + ((!string.IsNullOrWhiteSpace(form["Txt_AwarenessMuscleLD_Other"])) ? ":" + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["Txt_AwarenessMuscleLD_Other"]) : "")
                                + "\r\n。";
                            if(form["RbnAwarenessRASS"] != "不需要")
                            {
                                CareRecordCont += "RASS鎮靜評估 - ";
                                CareRecordCont += ((!string.IsNullOrWhiteSpace(form["RbnAwarenessRASSScore"])) ? form["RbnAwarenessRASSScore"] + "\r\n。" : "\r\n");
                            }

                            if(!string.IsNullOrWhiteSpace(form["RbnNervousSys"]))
                            {//神經系統
                                if(form["RbnNervousSys"] == "異常")
                                {
                                    CareRecordCont += "\r\n神經系統：";
                                    if(!string.IsNullOrWhiteSpace(form["ChkNervousAbnormal"]))
                                    {
                                        foreach(string item in form["ChkNervousAbnormal"].Split(','))
                                        {
                                            switch(item)
                                            {
                                                case "痛"://【神經系統-疼痛-部位】疼痛【神經系統-疼痛-分數】分。???分數的數值來源??
                                                    CareRecordCont += form["ChkNervous3Position"];
                                                    if(!string.IsNullOrWhiteSpace(form["Txt_Nervous3Position_Other"]))
                                                        CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["Txt_Nervous3Position_Other"]);
                                                    CareRecordCont += " " + item + " 問題存。";
                                                    break;
                                                case "麻":
                                                    CareRecordCont += form["ChkNervous4Position"];
                                                    if(!string.IsNullOrWhiteSpace(form["Txt_Nervous4Position_Other"]))
                                                        CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["Txt_Nervous4Position_Other"]);
                                                    CareRecordCont += " " + item + " 問題存。";
                                                    break;
                                                case "無知覺":
                                                    CareRecordCont += form["ChkNervous5Position"];
                                                    if(!string.IsNullOrWhiteSpace(form["Txt_Nervous5Position_Other"]))
                                                        CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["Txt_Nervous5Position_Other"]);
                                                    CareRecordCont += " " + item + " 問題存。";
                                                    break;
                                                case "抽搐":
                                                    CareRecordCont += form["ChkNervous6Position"];
                                                    if(!string.IsNullOrWhiteSpace(form["Txt_Nervous6Position_Other"]))
                                                        CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["Txt_Nervous6Position_Other"]);
                                                    CareRecordCont += " " + item + " 問題存。";
                                                    break;
                                                case "其他":
                                                    if(!string.IsNullOrWhiteSpace(form["TxtNervousAbnormal99Other"]))
                                                        CareRecordCont += this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtNervousAbnormal99Other"]) + " 問題存。";
                                                    break;
                                                default:
                                                    CareRecordCont += item + " 問題存。";
                                                    break;
                                            }
                                        }
                                    }
                                }
                            }
                            if(!string.IsNullOrWhiteSpace(form["RbnVision"]))
                            {//感官知覺--視力
                                if(form["RbnVision"] == "異常")
                                {
                                    CareRecordCont += "\r\n感官知覺：";
                                    if(!string.IsNullOrWhiteSpace(form["ChkVisionAbnormal"]))
                                    {
                                        foreach(string item in form["ChkVisionAbnormal"].Split(','))
                                        {
                                            switch(item)
                                            {
                                                case "失明":
                                                    CareRecordCont += (form["ChkBlindness"] == "右,左") ? "雙眼" + item + "。" : form["ChkBlindness"] + "眼" + item + "。";
                                                    break;
                                                case "偏盲":
                                                    CareRecordCont += (form["ChkHemianopia"] == "右,左") ? "雙眼" + item + "。" : form["ChkHemianopia"] + "眼" + item + "。";
                                                    break;
                                                case "複視":
                                                    CareRecordCont += (form["ChkDiplopia"] == "右,左") ? "雙眼" + item + "。" : form["ChkDiplopia"] + "眼" + item + "。";
                                                    break;
                                                case "其他":
                                                    if(!string.IsNullOrWhiteSpace(form["TxtVisionAbnormal99Other"]))
                                                        CareRecordCont += this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtVisionAbnormal99Other"]) + "問題存。";
                                                    break;
                                                default:
                                                    CareRecordCont += item + "。";
                                                    break;
                                            }
                                        }
                                    }
                                }
                            }
                            //感官知覺 輔助物，目前沒寫

                            //感官知覺 聽力
                            if(!string.IsNullOrWhiteSpace(form["RbnHearing"]))
                            {//感官知覺
                                if(form["RbnHearing"] == "障礙")
                                {
                                    CareRecordCont += "\r\n聽力 ";
                                    if(!string.IsNullOrWhiteSpace(form["ChkHearingAbnormal"]))
                                    {
                                        foreach(string item in form["ChkHearingAbnormal"].Split(','))
                                        {
                                            switch(item)
                                            {
                                                case "重聽":
                                                    CareRecordCont += (form["ChkHardHearing"] == "右,左") ? "雙耳" + item + "。" : form["ChkHardHearing"] + "耳" + item + "。";
                                                    break;
                                                case "失聰":
                                                    CareRecordCont += (form["ChkDeaf"] == "右,左") ? "雙耳" + item + "。" : form["ChkDeaf"] + "耳" + item + "。";
                                                    break;
                                                case "耳鳴":
                                                    CareRecordCont += (form["ChkTinnitus"] == "右,左") ? "雙耳" + item + "。" : form["ChkTinnitus"] + "耳" + item + "。";
                                                    break;
                                            }
                                        }
                                    }
                                }
                            }
                            if(!string.IsNullOrWhiteSpace(form["ChkHearingAid"]))
                                CareRecordCont += (form["ChkHearingAid"] == "右,左") ? "雙耳助聽器使用。" : form["ChkHearingAid"] + "耳助聽器使用。";
                            if(!string.IsNullOrWhiteSpace(form["RbnNose"]))
                                CareRecordCont += "嗅覺：" + form["RbnNose"] + "。";
                            if(!string.IsNullOrWhiteSpace(form["RbnTaste"]))
                                CareRecordCont += "味覺：" + form["RbnTaste"] + "。";
                            if(!string.IsNullOrWhiteSpace(form["RbnPulse"]))
                            {
                                CareRecordCont += "\r\n循環方面：";
                                //if(form["RbnPulse"] == "不規律")
                                //{
                                CareRecordCont += "脈搏 " + form["RbnPulse"] + "。";
                                //}
                            }
                            if(!string.IsNullOrWhiteSpace(form["ChkTipAbnormal"]))
                            {
                                CareRecordCont += "末梢 " + form["ChkTipAbnormal"];
                                if(!string.IsNullOrWhiteSpace(form["TxtTipAbnormal99Other"]))
                                {
                                    CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtTipAbnormal99Other"]);
                                }
                                CareRecordCont += "，";
                            }
                            if(!string.IsNullOrWhiteSpace(form["RbnDulplex"]) && form["RbnDulplex"] == "是")
                                CareRecordCont += "Dulpex使用 ";
                            if(!string.IsNullOrWhiteSpace(form["RbnDorsalPulseL"]))
                            {
                                if(!string.IsNullOrWhiteSpace(form["TxtDorsalPulseLOther"]))
                                    CareRecordCont += "左側足背動脈強度因" + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtDorsalPulseLOther"]) + "無法評估，";
                                else
                                    CareRecordCont += "左側足背動脈強度" + form["RbnDorsalPulseL"] + "，";
                            }
                            if(!string.IsNullOrWhiteSpace(form["RbnDorsalPulseR"]))
                            {
                                if(!string.IsNullOrWhiteSpace(form["TxtDorsalPulseROther"]))
                                    CareRecordCont += "右側足背動脈強度因" + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtDorsalPulseROther"]) + "無法評估，";
                                else
                                    CareRecordCont += "右側足背動脈強度" + form["RbnDorsalPulseR"] + "，";
                            }
                            if(!string.IsNullOrWhiteSpace(form["RbnRadialPulseL"]))
                            {
                                if(!string.IsNullOrWhiteSpace(form["TxtRadialPulseLOther"]))
                                    CareRecordCont += "左側橈動脈強度因" + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtRadialPulseLOther"]) + "無法評估，";
                                else
                                    CareRecordCont += "左側橈動脈強度" + form["RbnRadialPulseL"] + "，";
                            }
                            if(!string.IsNullOrWhiteSpace(form["RbnRadialPulseR"]))
                            {
                                if(!string.IsNullOrWhiteSpace(form["TxtRadialPulseROther"]))
                                    CareRecordCont += "右側橈動脈強度因" + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtRadialPulseROther"]) + "無法評估，";
                                else
                                    CareRecordCont += "右側橈動脈強度" + form["RbnRadialPulseR"] + "，";
                            }
                            CareRecordCont += "\r\n呼吸系統：";
                            if(!string.IsNullOrWhiteSpace(form["RbnBreathing"]))
                            {//呼吸系統 呼吸型態
                                if(form["RbnBreathing"] == "異常")
                                {
                                    if(!string.IsNullOrWhiteSpace(form["ChkBreathingAbnormal"]))
                                    {
                                        CareRecordCont += "病人呼吸型態 " + form["ChkBreathingAbnormal"];
                                        if(!string.IsNullOrWhiteSpace(form["TxtBreathingAbnormal99Other"]))
                                            CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtBreathingAbnormal99Other"]);
                                        CareRecordCont += "，";
                                    }
                                }
                            }
                            if(!string.IsNullOrWhiteSpace(form["RbnIsMedicalDevices"]))
                            {
                                if(!string.IsNullOrWhiteSpace(form["RbnMedicalDevices"]))
                                {
                                    if(!string.IsNullOrWhiteSpace(form["TxtMedicalDevices1_1"]) || !string.IsNullOrWhiteSpace(form["TxtMedicalDevices1_2"]))
                                        CareRecordCont += this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtMedicalDevices1_1"]) + "fr." + form["RbnMedicalDevices"] + "fix" + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtMedicalDevices1_2"]) + "cm。";
                                    if(!string.IsNullOrWhiteSpace(form["TxtMedicalDevices2_1"]))
                                        CareRecordCont += this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtMedicalDevices2_1"]) + "fr." + form["RbnMedicalDevices"] + "存。";
                                }
                                else
                                    CareRecordCont += "未使用醫療輔助器輔助呼吸。";
                            }
                            if(!string.IsNullOrWhiteSpace(form["RbnOxygen"]) && form["RbnOxygen"] == "有")
                            {
                                if(form["RbnFaceMask"] != "Aerosol mask" && form["RbnFaceMask"] != "Ventilator"){
                             if ($("input:radio[name=RbnFaceMask]:checked").val() == "Ventri mask")
                            {
                                         carerecordcont += form["RbnFaceMask"]+"。";//20161223有說要加上去一個Ventri mask選項，但這裡是註解，也順便加上去好了
                                }else{
                                    CareRecordCont += form["RbnFaceMask"] + " O2 " + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtO2Concentration"]) + " L/min使用。";
                            }}
                                else if(form["RbnFaceMask"] == "Aerosol mask"){
                                    // CareRecordCont += form["RbnFaceMask"] + " FiO2：" + form["TxtFaceMask3_1"] + "L/min使用。";
                                    CareRecordCont += form["RbnFaceMask"] + " FiO2：" + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtFaceMask3_1"]) + " %。";}

                                else if(form["RbnFaceMask"] == "Ventilator")
                                   { CareRecordCont += form["RbnFaceMask"] + " on" + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtFaceMask5_1"]) + "，TV：" + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtFaceMask5_2"])
                                        //   + "，FiO2：" + form["TxtFaceMask5_3"] + "，PEEP：" + form["TxtFaceMask5_4"]
                                       + "，FiO2：" + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtFaceMask5_3"]) + "%，PEEP：" + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtFaceMask5_4"])
                                        + "，Rate：" + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtFaceMask5_5"]);// +"，O2" + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtO2Concentration"]) + "L/min使用。";
                            }}
                            if(!string.IsNullOrWhiteSpace(form["ChkBrthSndsRAbnor"]))
                            {
                                CareRecordCont += "右側呼吸音：" + form["ChkBrthSndsRAbnor"];
                                if(!string.IsNullOrWhiteSpace(form["TxtBrthSndsRAbnorOther"]))
                                    CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtBrthSndsRAbnorOther"]);
                                CareRecordCont += "，";
                            }
                            else
                                CareRecordCont += "右側呼吸音正常，";
                            if(!string.IsNullOrWhiteSpace(form["ChkBrthSndsLAbnor"]))
                            {
                                CareRecordCont += "左側呼吸音：" + form["ChkBrthSndsLAbnor"];
                                if(!string.IsNullOrWhiteSpace(form["TxtBrthSndsLAbnorOther"]))
                                    CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtBrthSndsLAbnorOther"]);
                                CareRecordCont += "，";
                            }
                            else
                                CareRecordCont += "左側呼吸音正常，";
                            if(!string.IsNullOrWhiteSpace(form["RbnSputum"]) && form["RbnSputum"] == "有")
                            {
                                CareRecordCont += "痰液 量" + form["RbnSputumCount"] + "，色" + form["RbnSputumColor"];
                                if(!string.IsNullOrWhiteSpace(form["TxtSputumColor99Other"]))
                                    CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtSputumColor99Other"]);
                                CareRecordCont += "，呈" + form["RbnSputumNature"];
                                if(!string.IsNullOrWhiteSpace(form["TxtSputumNature99Other"]))
                                    CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtSputumNature99Other"]);
                                CareRecordCont += "狀。";
                            }
                            else
                            {
                                CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 1);
                                CareRecordCont += "。";
                            }
                            CareRecordCont += "\r\n消化系統：";
                            if(!string.IsNullOrWhiteSpace(form["RbnAppetiteAbnItem"]))
                                CareRecordCont += "食欲異常" + form["RbnAppetiteAbnItem"] + "，";
                            if(!string.IsNullOrWhiteSpace(form["ChkAppetiteAbnElse"]))
                            {
                                CareRecordCont += "有" + form["ChkAppetiteAbnElse"];
                                if(!string.IsNullOrWhiteSpace(form["TxtAppetiteAbnElse99Other"]))
                                    CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtAppetiteAbnElse99Other"]);
                                CareRecordCont += "腸胃道症狀存。";
                            }
                            if(!string.IsNullOrWhiteSpace(form["RbnEatingPatterns"]))
                            {
                                switch(form["RbnEatingPatterns"])
                                {
                                    case "由口進食":
                                        CareRecordCont += "由口進食，";
                                        break;
                                    case "NG Feeding":
                                        CareRecordCont += "On " + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtEatPat2_1"]) + "fr. NG tube fix" + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtEatPat2_2"]) + "cm，";
                                        if(form["RbnEatPat2Item"] != "無")
                                        {
                                            CareRecordCont += "使用" + form["RbnEatPat2Item"] + "輔助，";
                                        }
                                        CareRecordCont += "消化" + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtEatPat2_3"]) + "，";
                                        break;
                                    case "OG tube":
                                    case "Jujunostomy":
                                        CareRecordCont += "On " + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtEatPat2_1"]) + "fr. " + form["RbnEatingPatterns"]
                                            + " fix" + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtEatPat2_2"]) + "cm，" + "消化" + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtEatPat2_3"]) + "，";
                                        break;
                                    case "ND tube":
                                    case "PEG":
                                        CareRecordCont += "On " + form["RbnEatingPatterns"] + "消化" + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtEatPat2_3"]) + "，";
                                        break;
                                    case "其他":
                                        if(!string.IsNullOrWhiteSpace(form["TxtEatingPatterns99Other"]))
                                            CareRecordCont += "以" + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtEatingPatterns99Other"]) + "方式進食，";
                                        break;
                                }
                            }
                            if(!string.IsNullOrWhiteSpace(form["RbnDietType"]))
                            {
                                switch(form["RbnDietType"])
                                {
                                    case "管灌飲食":
                                        CareRecordCont += "On " + form["RbnDietType"] + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtDietType6Cont"]) + "卡/日。";
                                        break;
                                    case "NPO":
                                    case "NPO除藥":
                                        CareRecordCont += "現 " + form["RbnDietType"] + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtDietType6Cont"]) + "。";
                                        break;
                                    case "其他":
                                        if(!string.IsNullOrWhiteSpace(form["TxtDietType99Other"]))
                                            CareRecordCont += "On " + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtDietType99Other"]) + "。";
                                        break;
                                    default:
                                        CareRecordCont += "On " + form["RbnDietType"] + "。";
                                        break;
                                }
                            }
                            if(!string.IsNullOrWhiteSpace(form["RbnPalpation"]))
                            {
                                if(!string.IsNullOrWhiteSpace(form["ChkPalpationAbn"]))
                                {
                                    CareRecordCont += "腹部觸診" + form["ChkPalpationAbn"];
                                    if(!string.IsNullOrWhiteSpace(form["TxtPalpationAbnElse99Other"]))
                                        CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtPalpationAbnElse99Other"]);
                                    CareRecordCont += "，";
                                }
                                else
                                    CareRecordCont += "腹部觸診" + form["RbnPalpation"] + "，";
                            }
                            if(!string.IsNullOrWhiteSpace(form["TxtPeristalsis"]))
                                CareRecordCont += "腸蠕動" + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtPeristalsis"]) + "次/分，";
                            if(!string.IsNullOrWhiteSpace(form["RbnBowelSounds"]))
                                CareRecordCont += "腸音" + form["RbnBowelSounds"] + "。";
                            if(!string.IsNullOrWhiteSpace(form["RbnDecompression"]) && form["RbnDecompression"] == "有")
                            {
                                CareRecordCont += "Decompression量" + form["RbnDecCount"] + "，色" + form["RbnDecColor"];
                                if(!string.IsNullOrWhiteSpace(form["TxtDecColor99Other"]))
                                    CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtDecColor99Other"]);
                                CareRecordCont += "，呈" + form["RbnDecNature"];
                                if(!string.IsNullOrWhiteSpace(form["TxtDecNature99Other"]))
                                    CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtDecNature99Other"]);
                                CareRecordCont += "狀。";
                            }
                            CareRecordCont += "\r\n排泄系統：";
                            if(!string.IsNullOrWhiteSpace(form["ChkUrinationAbn"]))
                            {
                                CareRecordCont += "排尿：" + form["ChkUrinationAbn"];
                                if(!string.IsNullOrWhiteSpace(form["TxtUrinationAbn99Other"]))
                                    CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtUrinationAbn99Other"]);
                                CareRecordCont += "問題存，";
                            }
                            if(!string.IsNullOrWhiteSpace(form["RbnVoidingPattern"]))
                            {
                                if(form["RbnVoidingPattern"] == "自解")
                                    CareRecordCont += "病人尿液可自解，";
                                else
                                {
                                    CareRecordCont += form["RbnVoidingPattern"];
                                    if(!string.IsNullOrWhiteSpace(form["TxtVoidingPattern99Other"]))
                                        CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtVoidingPattern99Other"]);
                                    CareRecordCont += "使用。";
                                }
                            }
                            if(!string.IsNullOrWhiteSpace(form["TxtUrineCharactersAmount"]))
                                CareRecordCont += "尿液量" + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtUrineCharactersAmount"]);
                            if(!string.IsNullOrWhiteSpace(form["RbnUrineColor"]))
                            {
                                CareRecordCont += "，色" + form["RbnUrineColor"];
                                if(!string.IsNullOrWhiteSpace(form["Txt_UrineColor_Other"]))
                                    CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["Txt_UrineColor_Other"]);
                            }
                            if(!string.IsNullOrWhiteSpace(form["RbnUrineNature"]))
                            {
                                CareRecordCont += "，呈" + form["RbnUrineNature"];
                                if(!string.IsNullOrWhiteSpace(form["Txt_UrineNature_Other"]))
                                    CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["Txt_UrineNature_Other"]);
                                CareRecordCont += "狀。";
                            }
                            if(!string.IsNullOrWhiteSpace(form["ChkDefecationAbn"]))
                            {
                                CareRecordCont += "排便狀況" + form["ChkDefecationAbn"];
                                if(!string.IsNullOrWhiteSpace(form["TxtDefecationAbn99Other"]))
                                    CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtDefecationAbn99Other"]);
                                CareRecordCont += "問題存。";
                            }
                            if(!string.IsNullOrWhiteSpace(form["ChkSkeletalSystemAbn"]))
                            {
                                CareRecordCont += "\r\n骨骼系統：";
                                foreach(string item in form["ChkSkeletalSystemAbn"].Split(','))
                                {
                                    switch(item)
                                    {
                                        case "駝背":
                                            CareRecordCont += item + " 問題存。";
                                            break;
                                        case "骨折":
                                            CareRecordCont += form["ChkSkeletalSys2Position"] + " " + item + " 問題存。";
                                            break;
                                        case "脫臼":
                                            CareRecordCont += form["ChkSkeletalSys3Position"] + " " + item + " 問題存。";
                                            break;
                                        case "關節腫":
                                            CareRecordCont += form["ChkSkeletalSys4Position"] + " " + item + " 問題存。";
                                            break;
                                        case "截肢":
                                            CareRecordCont += form["ChkSkeletalSys5Position"] + " " + item + " 問題存。";
                                            break;
                                        case "畸形":
                                            CareRecordCont += form["ChkSkeletalSys6Position"] + " " + item + " 問題存。";
                                            break;
                                        case "其他":
                                            if(!string.IsNullOrWhiteSpace(form["TxtSkeletalSystemAbn99Other"]))
                                                CareRecordCont += this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtSkeletalSystemAbn99Other"]) + " 問題存。";
                                            break;
                                    }
                                }
                            }
                            //if(!string.IsNullOrWhiteSpace(form["ChkBonesEventsAbn"]))
                            //{
                            //    CareRecordCont += "\r\n活動運動：";
                            //    CareRecordCont += form["ChkBonesEventsAbn"];
                            //    if(!string.IsNullOrWhiteSpace(form["TxtBonesEventsAbn99Other"]))
                            //        CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtBonesEventsAbn99Other"]);
                            //    CareRecordCont += "問題存。";
                            //}
                            //if(!string.IsNullOrWhiteSpace(form["RbnBonesEvents"]) && form["RbnBonesEvents"] == "無法行動")
                            //    CareRecordCont += "病人現臥床無法行動。";
                            if(!string.IsNullOrWhiteSpace(form["ChkBonesEventsAbn"]))
                            {
                                CareRecordCont += "\r\n活動運動：";
                                string TempStr1 = string.Empty, TempStr2 = string.Empty;
                                foreach(string item in form["ChkBonesEventsAbn"].Split(','))
                                {
                                    switch(item)
                                    {
                                        case "跛行":
                                            TempStr1 += item + ",";
                                            break;
                                        case "行走疼痛":
                                            TempStr1 += item + ",";
                                            break;
                                        case "使用輔助工具":
                                            TempStr1 += item + ",";
                                            break;
                                        case "義肢":
                                            TempStr1 += item + ",";
                                            break;
                                        case "無法行動":
                                            TempStr2 = "病人現臥床無法行動。";
                                            break;
                                        case "其他":
                                            if(!string.IsNullOrWhiteSpace(form["TxtBonesEventsAbn99Other"]))
                                                TempStr1 += this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtBonesEventsAbn99Other"]) + ",";
                                            break;
                                    }
                                }
                                TempStr1 = TempStr1.Substring(0, TempStr1.Length - 1);
                                CareRecordCont += TempStr1 + " 問題存。";
                                if(!string.IsNullOrEmpty(TempStr2))
                                {
                                    CareRecordCont += TempStr2;
                                }
                            }

                            if(!string.IsNullOrWhiteSpace(form["ChkSkinAppearanceAbn"]))
                            {
                                CareRecordCont += "\r\n皮膚完整性：";
                                foreach(string item in form["ChkSkinAppearanceAbn"].Split(','))
                                {
                                    switch(item)
                                    {
                                        case "水腫":
                                            CareRecordCont += "外觀" + form["ChkSkinAppearance6Position"];
                                            if(!string.IsNullOrWhiteSpace(form["Txt_SkinAppearance6Position_Other"]))
                                                CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["Txt_SkinAppearance6Position_Other"]);
                                            CareRecordCont += item + form["RbnSkinAppearAbn6Plus"] + "問題存。";
                                            break;
                                        case "紅疹":
                                            CareRecordCont += "外觀" + form["ChkSkinAppearance7Position"];
                                            if(!string.IsNullOrWhiteSpace(form["Txt_SkinAppearance7Position_Other"]))
                                                CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["Txt_SkinAppearance7Position_Other"]);
                                            CareRecordCont += item + "問題存。";
                                            break;
                                        case "其他":
                                            if(!string.IsNullOrWhiteSpace(form["TxtSkinAppearanceAbn99Other"]))
                                                CareRecordCont += "外觀" + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtSkinAppearanceAbn99Other"]) + "問題存。";
                                            break;
                                        default:
                                            CareRecordCont += "外觀" + item + "問題存。";
                                            break;
                                    }
                                }
                            }
                            //禁治療部位
                            if(!string.IsNullOrWhiteSpace(form["param_taboo_position"]))
                            {
                                if(form["param_taboo_position"] == "有")
                                {
                                    string[] param_taboo_position = form["param_taboo_position_txt"].Split(',');//.Split(new string[] { "※＆" }, StringSplitOptions.None);
                                    string[] param_taboo_position_other = form["param_taboo_position_other"].Split(',');//.Split(new string[] { "※＆" }, StringSplitOptions.None);
                                    if(param_taboo_position.Length == param_taboo_position_other.Length)
                                    {
                                        for(int j = 0; j < param_taboo_position.Length; j++)
                                        {
                                            CareRecordCont += param_taboo_position[j] + "因" + param_taboo_position_other[j] + "禁治療。";
                                        }
                                    }
                                }
                            }
                            if(!string.IsNullOrWhiteSpace(form["RbnReproductiveSysStatus"]))
                            {
                                if(form["RbnReproductiveSysStatus"].ToString() != "正常")
                                {
                                    CareRecordCont += "\r\n生殖系統：";
                                    if(!string.IsNullOrWhiteSpace(form["ChkRepSysFemale"]))
                                    {
                                        CareRecordCont += form["ChkRepSysFemale"];
                                        if(!string.IsNullOrWhiteSpace(form["TxtRepSysFemale99Other"]))
                                            CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtRepSysFemale99Other"]);
                                        CareRecordCont += "問題存。";
                                    }
                                    if(!string.IsNullOrWhiteSpace(form["ChkRepSysMale"]))
                                    {
                                        foreach(string item in form["ChkRepSysMale"].Split(','))
                                        {
                                            switch(item)
                                            {
                                                case "腫塊":
                                                    CareRecordCont += this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtRepSysMale3Width"]) + "*" + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtRepSysMale3Height"]) + "cm 腫塊存。";
                                                    break;
                                                case "其他":
                                                    if(!string.IsNullOrWhiteSpace(form["TxtRepSysMale99Other"]))
                                                        CareRecordCont += this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtRepSysMale99Other"]) + "問題存。";
                                                    break;
                                                default:
                                                    CareRecordCont += item + "問題存。";
                                                    break;
                                            }
                                        }
                                    }
                                }
                            }
                            if(!string.IsNullOrWhiteSpace(form["TxtElseRemark"]))
                            {//備註//其他
                                CareRecordCont += "\r\n備註：";
                                CareRecordCont += this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtElseRemark"]) + "。";
                            }*/
                            #endregion 成人身體評估--護理記錄組字 end
                            //if(base.Upd_CareRecord(DBAMDTM, ID, "身體評估", CareString, "", "", "", "", "DAILY_BODY_ASSESSMENT_MASTER") == 0)
                            base.Insert_CareRecord_Black(DBAMDTM, ID, "身體評估", CareString, "", "", "", "");
                        }
                        else
                        {
                            #region 兒童身體評估--護理記錄組字 by jarvis 20160901///20161216  (20161102修正)
                            /*CareRecordCont = "一般外觀：";
                            if(!string.IsNullOrWhiteSpace(form["RbnGeneralExterior"]))
                            {//一般外觀
                                CareRecordCont += "病童外觀" + form["RbnGeneralExterior"];
                                if(!string.IsNullOrWhiteSpace(form["TxtRbnGeneralExterior5Other"]))
                                {
                                    CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtRbnGeneralExterior5Other"]);
                                }
                                CareRecordCont += "，";
                            }
                            if(!string.IsNullOrWhiteSpace(form["RbnAwarenessStatus"]))
                            {//意識狀態s
                                CareRecordCont += "意識狀態 " + form["RbnAwarenessStatus"] + "，GCS：E " + form["RbnAwarenessComa_E"] + " V " + form["RbnAwarenessComa_V"] + " M " + form["RbnAwarenessComa_M"] + "，";
                            }
                            CareRecordCont += "\r\n";
                            if(!string.IsNullOrWhiteSpace(form["RbnEmotional"]))
                            {//情緒狀態
                                if(form["RbnEmotional"] != "異常")
                                {
                                    CareRecordCont += "情緒平靜。";
                                }
                                else
                                {
                                    CareRecordCont += "情緒 " + form["ChkEmotionalAbn"];
                                    if(!string.IsNullOrWhiteSpace(form["TxtEmotionalAbn99Other"]))
                                    {
                                        CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtEmotionalAbn99Other"]);
                                    }
                                    CareRecordCont += "。";
                                }
                            }
                            if(form["RbnNervousSys"] == "異常" || form["RbnFeelingSys"] == "異常")
                            {
                                CareRecordCont += "\r\n神經系統：";
                            }
                            if(!string.IsNullOrWhiteSpace(form["RbnNervousSys"]))
                            {//神經系統
                                if(form["RbnNervousSys"] == "異常")
                                {
                                    if(!string.IsNullOrWhiteSpace(form["ChkNervousAbnormal"]))
                                    {
                                        CareRecordCont += form["ChkNervousAbnormal"] + " 問題存。";
                                    }
                                }
                            }
                            if(!string.IsNullOrWhiteSpace(form["RbnFeelingSys"]))
                            {//神經系統 感覺
                                if(form["RbnFeelingSys"] == "異常")
                                {
                                    if(!string.IsNullOrWhiteSpace(form["ChkFeelingAbn"]))
                                    {
                                        foreach(string item in form["ChkFeelingAbn"].Split(','))
                                        {
                                            switch(item)
                                            {
                                                case "酸":
                                                    CareRecordCont += form["ChkFeelingAbn5Position"];
                                                    if(!string.IsNullOrWhiteSpace(form["Txt_Feeling5Position_Other"]))
                                                    {
                                                        CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["Txt_Feeling5Position_Other"]);
                                                    }
                                                    CareRecordCont += " " + item + " 問題存。";
                                                    break;
                                                case "痛":
                                                    CareRecordCont += form["ChkFeelingAbn1Position"];
                                                    if(!string.IsNullOrWhiteSpace(form["Txt_Feeling1Position_Other"]))
                                                    {
                                                        CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["Txt_Feeling1Position_Other"]);
                                                    }
                                                    CareRecordCont += " " + item + " 問題存。";
                                                    break;
                                                case "麻":
                                                    CareRecordCont += form["ChkFeelingAbn2Position"];
                                                    if(!string.IsNullOrWhiteSpace(form["Txt_Feeling2Position_Other"]))
                                                    {
                                                        CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["Txt_Feeling2Position_Other"]);
                                                    }
                                                    CareRecordCont += " " + item + " 問題存。";
                                                    break;
                                                case "無知覺":
                                                    CareRecordCont += form["ChkFeelingAbn3Position"];
                                                    if(!string.IsNullOrWhiteSpace(form["Txt_Feeling3Position_Other"]))
                                                    {
                                                        CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["Txt_Feeling3Position_Other"]);
                                                    }
                                                    CareRecordCont += " " + item + " 問題存。";
                                                    break;
                                                case "抽搐":
                                                    CareRecordCont += form["ChkFeelingAbn4Position"];
                                                    if(!string.IsNullOrWhiteSpace(form["Txt_Feeling4Position_Other"]))
                                                    {
                                                        CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["Txt_Feeling4Position_Other"]);
                                                    }
                                                    CareRecordCont += " " + item + " 問題存。";
                                                    break;
                                                case "無法測":
                                                    //CareRecordCont += form["ChkFeelingAbn6Position"];
                                                    CareRecordCont += " " + item + "。";
                                                    break;
                                                case "其他":
                                                    if(!string.IsNullOrWhiteSpace(form["TxtFeelingAbn99Other"]))
                                                    {
                                                        CareRecordCont += this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtFeelingAbn99Other"]);
                                                    }
                                                    CareRecordCont += " 問題存。";
                                                    break;
                                            }
                                        }
                                    }
                                }
                            }
                            if(form["RbnVision"] != "正常" || form["RbnHearing"] != "正常" || form["RbnNose"] != "正常" || form["RbnMouth"] != "正常")
                            {
                                CareRecordCont += "\r\n感官系統：病童 ";
                            }
                            if(!string.IsNullOrWhiteSpace(form["RbnVision"]))
                            {//感官系統 眼
                                if(form["RbnVision"] == "異常")
                                {
                                    if(!string.IsNullOrWhiteSpace(form["ChkVisionAbnormal"]))
                                    {
                                        foreach(string item in form["ChkVisionAbnormal"].Split(','))
                                        {
                                            switch(item)
                                            {
                                                case "近視":
                                                    CareRecordCont += item + "，";
                                                    break;
                                                case "弱視":
                                                    if(!string.IsNullOrWhiteSpace(form["ChkAmblyopia"]))
                                                    {
                                                        string[] TempStrArr = form["ChkAmblyopia"].Split(',');
                                                        for(int j = 0; j < TempStrArr.Length; j++)
                                                        {
                                                            CareRecordCont += TempStrArr[j] + "眼 " + item + "，";
                                                        }
                                                    }
                                                    break;
                                                case "斜視":
                                                    if(!string.IsNullOrWhiteSpace(form["ChkStrabismus"]))
                                                    {
                                                        string[] TempStrArr = form["ChkStrabismus"].Split(',');
                                                        for(int j = 0; j < TempStrArr.Length; j++)
                                                        {
                                                            CareRecordCont += TempStrArr[j] + "眼 " + item + "，";
                                                        }
                                                    }
                                                    break;
                                                case "分泌物":
                                                    if(!string.IsNullOrWhiteSpace(form["ChkHemianopia"]))
                                                    {
                                                        string[] TempStrArr = form["ChkHemianopia"].Split(',');
                                                        for(int j = 0; j < TempStrArr.Length; j++)
                                                        {
                                                            CareRecordCont += TempStrArr[j] + "眼 " + item + "，";
                                                        }
                                                    }
                                                    break;
                                                case "其他":
                                                    if(!string.IsNullOrWhiteSpace(form["TxtVisionAbnormal99Other"]))
                                                    {
                                                        CareRecordCont += this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtVisionAbnormal99Other"]);
                                                    }
                                                    break;
                                            }
                                        }
                                        CareRecordCont += " 問題存。";
                                    }
                                }
                                else if(form["RbnVision"] == "無法評估")
                                {
                                    CareRecordCont += "眼部無法評估。";
                                }
                            }
                            CareRecordCont += "\r\n";
                            if(!string.IsNullOrWhiteSpace(form["RbnHearing"]))
                            {//感官系統 耳
                                if(form["RbnHearing"] == "異常")
                                {
                                    if(!string.IsNullOrWhiteSpace(form["ChkHearingAbnormal"]))
                                    {
                                        foreach(string item in form["ChkHearingAbnormal"].Split(','))
                                        {
                                            switch(item)
                                            {
                                                case "聽力障礙":
                                                    if(!string.IsNullOrWhiteSpace(form["ChkHardHearing"]))
                                                    {
                                                        //CareRecordCont += form["ChkHardHearing"];
                                                        string[] TempStrArr = form["ChkHardHearing"].Split(',');
                                                        for(int j = 0; j < TempStrArr.Length; j++)
                                                        {
                                                            CareRecordCont += TempStrArr[j] + "耳 " + item + "，";
                                                        }
                                                    }
                                                    //CareRecordCont += " " + item + "，";
                                                    break;
                                                case "耳鳴":
                                                    if(!string.IsNullOrWhiteSpace(form["ChkTinnitus"]))
                                                    {
                                                        //CareRecordCont += form["ChkTinnitus"];
                                                        string[] TempStrArr = form["ChkTinnitus"].Split(',');
                                                        for(int j = 0; j < TempStrArr.Length; j++)
                                                        {
                                                            CareRecordCont += TempStrArr[j] + "耳 " + item + "，";
                                                        }
                                                    }
                                                    //CareRecordCont += " " + item + "，";
                                                    break;
                                                case "其他":
                                                    if(!string.IsNullOrWhiteSpace(form["TxtHearingAbnormal99Other"]))
                                                    {
                                                        CareRecordCont += this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtHearingAbnormal99Other"]);
                                                    }
                                                    break;
                                            }
                                        }
                                        CareRecordCont += " 問題存。";
                                    }
                                }
                            }
                            CareRecordCont += "\r\n";
                            if(!string.IsNullOrWhiteSpace(form["RbnNose"]))
                            {//感官系統 鼻
                                if(form["RbnNose"] == "異常")
                                {
                                    if(!string.IsNullOrWhiteSpace(form["ChkNoseAbnormal"]))
                                    {
                                        CareRecordCont += form["ChkNoseAbnormal"];//.Replace(',','，');
                                        if(!string.IsNullOrWhiteSpace(form["TxtNoseAbnormal99Other"]))
                                        {
                                            CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtNoseAbnormal99Other"]);
                                        }
                                        CareRecordCont += " 問題存。";
                                    }
                                }
                            }
                            CareRecordCont += "\r\n";
                            if(!string.IsNullOrWhiteSpace(form["RbnMouth"]))
                            {//感官系統 口腔
                                if(form["RbnMouth"] == "異常")
                                {
                                    if(!string.IsNullOrWhiteSpace(form["ChkMouthAbnormal"]))
                                    {
                                        CareRecordCont += form["ChkMouthAbnormal"];
                                        if(!string.IsNullOrWhiteSpace(form["TxtMouthAbnormal99Other"]))
                                        {
                                            CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtMouthAbnormal99Other"]);
                                        }
                                        CareRecordCont += " 問題存。";
                                    }
                                }
                            }
                            CareRecordCont += "\r\n呼吸系統：";
                            if(!string.IsNullOrWhiteSpace(form["RbnBreathing"]))
                            {//呼吸系統 呼吸型態
                                if(form["RbnBreathing"] == "異常")
                                {
                                    if(!string.IsNullOrWhiteSpace(form["ChkBreathingAbnormal"]))
                                    {
                                        CareRecordCont += "呼吸型態：";
                                        foreach(string item in form["ChkBreathingAbnormal"].Split(','))
                                        {
                                            switch(item)
                                            {
                                                case "呼吸急促":
                                                    CareRecordCont += item;
                                                    if(!string.IsNullOrWhiteSpace(form["TxtBreathingAbn1Cont"]))
                                                    {
                                                        CareRecordCont += this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtBreathingAbn1Cont"]);
                                                    }
                                                    CareRecordCont += " 次/分，";
                                                    break;
                                                case "鼻翼煽動":
                                                    CareRecordCont += item + "，";
                                                    break;
                                                case "胸骨或肋緣凹陷":
                                                    CareRecordCont += item + "，";
                                                    break;
                                                case "呻吟聲":
                                                    CareRecordCont += item + "，";
                                                    break;
                                                case "端坐呼吸":
                                                    CareRecordCont += item + "，";
                                                    break;
                                                case "咳嗽":
                                                    CareRecordCont += item + "，";
                                                    break;
                                                case "其他":
                                                    if(!string.IsNullOrWhiteSpace(form["TxtBreathingAbnormal99Other"]))
                                                    {
                                                        CareRecordCont += this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtBreathingAbnormal99Other"]);
                                                    }
                                                    CareRecordCont += "，";
                                                    break;
                                            }
                                        }
                                    }
                                }
                            }
                            CareRecordCont += "\r\n";
                            if(!string.IsNullOrWhiteSpace(form["RbnIsMedicalDevices"]))
                            {//呼吸系統 醫療輔助器
                                if(form["RbnIsMedicalDevices"] == "有")
                                {
                                    if(!string.IsNullOrWhiteSpace(form["RbnMedicalDevices"]))
                                    {
                                        switch(form["RbnMedicalDevices"])
                                        {
                                            case "氣切":
                                                CareRecordCont += form["RbnMedicalDevices"] + " 存。";
                                                break;
                                            case "氣管內管":
                                                CareRecordCont += form["RbnMedicalDevices"] + " 存。";
                                                break;
                                            case "氧氣治療":
                                                if(!string.IsNullOrWhiteSpace(form["RbnFaceMask"]))
                                                {
                                                    switch(form["RbnFaceMask"])
                                                    {
                                                        case "Nasal Cannula":
                                                            CareRecordCont += form["RbnFaceMask"] + " 使用。";
                                                            break;
                                                        case "Simple mask":
                                                            CareRecordCont += form["RbnFaceMask"] + " 使用。";
                                                            break;
                                                        case "Aerosol mask":
                                                            CareRecordCont += form["RbnFaceMask"] + " 使用。";
                                                            break;
                                                        case "non-rebreathing mask":
                                                            CareRecordCont += form["RbnFaceMask"] + " 使用。";
                                                            break;
                                                        case "Ventilator":
                                                            CareRecordCont += form["RbnFaceMask"] + " 使用，";
                                                            if(!string.IsNullOrWhiteSpace(form["TxtFaceMask5_1"]))
                                                            {
                                                                CareRecordCont += this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtFaceMask5_1"]) + "mode, ";
                                                            }
                                                            if(!string.IsNullOrWhiteSpace(form["TxtFaceMask5_2"]))
                                                            {
                                                                CareRecordCont += " TV：" + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtFaceMask5_2"]);
                                                            }
                                                            if(!string.IsNullOrWhiteSpace(form["TxtFaceMask5_3"]))
                                                            {
                                                                CareRecordCont += " ,FiO2：" + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtFaceMask5_3"]) + " %";
                                                            }
                                                            if(!string.IsNullOrWhiteSpace(form["TxtFaceMask5_4"]))
                                                            {
                                                                CareRecordCont += " ,PEEP：" + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtFaceMask5_4"]);
                                                            }
                                                            if(!string.IsNullOrWhiteSpace(form["TxtFaceMask5_5"]))
                                                            {
                                                                CareRecordCont += " ,Rate：" + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtFaceMask5_5"]) + "次/分。";
                                                            }
                                                            break;
                                                    }
                                                }
                                                break;
                                        }
                                    }
                                }
                                else
                                {
                                    CareRecordCont += "未使用醫療輔助器輔助呼吸。";
                                }
                            }
                            CareRecordCont += "\r\n";
                            if(!string.IsNullOrWhiteSpace(form["RbnBrthSndsR"]))
                            {//呼吸系統 呼吸音 右側
                                if(form["RbnBrthSndsR"] != "正常")
                                {
                                    if(!string.IsNullOrWhiteSpace(form["ChkBrthSndsRAbnor"]))
                                    {
                                        CareRecordCont += "右側呼吸音 " + form["ChkBrthSndsRAbnor"];
                                        if(!string.IsNullOrWhiteSpace(form["TxtBrthSndsRAbnorOther"]))
                                        {
                                            CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtBrthSndsRAbnorOther"]);
                                        }
                                        CareRecordCont += "。";
                                    }
                                }
                                else
                                {
                                    CareRecordCont += "右側呼吸音正常，";
                                }
                            }
                            CareRecordCont += "\r\n";
                            if(!string.IsNullOrWhiteSpace(form["RbnBrthSndsL"]))
                            {//呼吸系統 呼吸音 左側
                                if(form["RbnBrthSndsL"] != "正常")
                                {
                                    if(!string.IsNullOrWhiteSpace(form["ChkBrthSndsLAbnor"]))
                                    {
                                        CareRecordCont += "左側呼吸音 " + form["ChkBrthSndsLAbnor"];
                                        if(!string.IsNullOrWhiteSpace(form["TxtBrthSndsLAbnorOther"]))
                                        {
                                            CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtBrthSndsLAbnorOther"]);
                                        }
                                        CareRecordCont += "。";
                                    }
                                }
                                else
                                {
                                    CareRecordCont += "左側呼吸音正常。";
                                }
                            }
                            CareRecordCont += "\r\n";
                            if(!string.IsNullOrWhiteSpace(form["RbnSputum"]))
                            {//痰液
                                if(form["RbnSputum"] != "無")
                                {
                                    CareRecordCont += "痰液量 ";
                                    if(!string.IsNullOrWhiteSpace(form["RbnSputumCount"]))
                                    {
                                        CareRecordCont += form["RbnSputumCount"] + "，";
                                    }
                                    if(!string.IsNullOrWhiteSpace(form["RbnSputumColor"]))
                                    {
                                        CareRecordCont += "色 " + form["RbnSputumColor"];
                                    }
                                    if(!string.IsNullOrWhiteSpace(form["Txt_SputumColor_Other"]))
                                    {
                                        CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["Txt_SputumColor_Other"]);
                                    }
                                    CareRecordCont += "，呈";
                                    if(!string.IsNullOrWhiteSpace(form["RbnSputumNature"]))
                                    {
                                        CareRecordCont += form["RbnSputumNature"];
                                    }
                                    if(!string.IsNullOrWhiteSpace(form["Txt_SputumNature_Other"]))
                                    {
                                        CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["Txt_SputumNature_Other"]);
                                    }
                                    CareRecordCont += " 狀。";
                                }
                            }
                            if(!string.IsNullOrWhiteSpace(form["RbnCardiovascular"]))
                            {//心臟血管系統
                                if(form["RbnCardiovascular"] != "心跳規則")
                                {
                                    CareRecordCont += "\r\n心臟血管系統：";
                                    CareRecordCont += form["ChkCardiovascularAbnormal"];
                                    if(!string.IsNullOrWhiteSpace(form["TxtCardiovascularAbnormal99Other"]))
                                    {
                                        CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtCardiovascularAbnormal99Other"]);
                                    }
                                    CareRecordCont += " 問題存。";
                                }
                                else
                                {
                                    CareRecordCont += "\r\n心臟血管系統：心跳規則。";
                                }
                            }
                            if(!string.IsNullOrWhiteSpace(form["RbnStomach"]))
                            {//腸胃系統
                                string temp_str_Stomach1 = string.Empty, temp_str_Stomach2 = string.Empty, temp_str_Stomach3 = string.Empty;
                                if(form["RbnStomach"] != "正常")
                                {
                                    CareRecordCont += "\r\n腸胃系統：";
                                    if(!string.IsNullOrWhiteSpace(form["ChkStomachAbnormal"]))
                                    {
                                        foreach(string item in form["ChkStomachAbnormal"].Split(','))
                                        {
                                            switch(item)
                                            {
                                                case "食慾減少":
                                                    temp_str_Stomach1 += item + ",";
                                                    break;
                                                case "噁心":
                                                    temp_str_Stomach1 += item + " ,";
                                                    break;
                                                case "嘔吐":
                                                    temp_str_Stomach1 += item + " ,";
                                                    break;
                                                case "腹脹":
                                                    temp_str_Stomach1 += item + " ,";
                                                    break;
                                                case "腹水":
                                                    temp_str_Stomach1 += item + " ,";
                                                    break;
                                                case "疝氣":////
                                                    //temp_str_Stomach2 += form["ChkStomachAbn6Position"];//item + " ,";
                                                    if(!string.IsNullOrEmpty(form["TxtStomachAbn6Position"]))
                                                    {
                                                        temp_str_Stomach2 += this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtStomachAbn6Position"]) + " 疝氣問題存。";//item + " ,";
                                                    }
                                                    break;
                                                case "體重改變":
                                                    temp_str_Stomach1 += item + " ,";
                                                    break;
                                                case "便秘":
                                                    temp_str_Stomach1 += item + " ,";
                                                    break;
                                                case "腹瀉"://////目前畫面上只有性狀，並無客戶給予的表單上顯示還有"顏色"該選項
                                                    temp_str_Stomach3 += item + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtStomachAbnl9Cont"]) + " 次/天 ";
                                                    // string temp_str0 = (form["Txt_Stoolcolor_Other"] != "") ? this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["Txt_Stoolcolor_Other"]) : form["RbnStoolcolor"];//顏色目前沒有，所以先假設name(預留)
                                                    // temp_str_Stomach3 += "，糞便色"+temp_str0;                                                                                                                                  //顏色目前沒有，所以先假設name(預留)
                                                    string temp_str = (form["Txt_StoolNature_Other"] != "") ? this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["Txt_StoolNature_Other"]) : form["RbnStoolNature"];
                                                    temp_str_Stomach3 += "，呈" + temp_str + "狀。";
                                                    break;
                                                case "血便":
                                                    temp_str_Stomach1 += item + " ,";
                                                    break;
                                                case "其他":////
                                                    //temp_str_Stomach1 += item;
                                                    if(!string.IsNullOrWhiteSpace(form["TxtStomachAbnormal99Other"]))
                                                    {
                                                        temp_str_Stomach1 += this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtStomachAbnormal99Other"]);
                                                    }
                                                    temp_str_Stomach1 += " ,";
                                                    break;
                                            }
                                        }
                                        temp_str_Stomach1 = temp_str_Stomach1.Substring(0, temp_str_Stomach1.Length - 1);
                                        CareRecordCont += temp_str_Stomach1 + " 問題存。" + temp_str_Stomach2 + temp_str_Stomach3;
                                    }
                                }
                                else
                                {
                                    CareRecordCont += "\r\n腸胃系統：正常。";
                                }
                            }
                            if(!string.IsNullOrWhiteSpace(form["RbnUrinarySys"]))
                            {//泌尿系統
                                if(form["RbnUrinarySys"] != "正常")
                                {
                                    CareRecordCont += "\r\n泌尿系統：";
                                    CareRecordCont += form["ChkUrinarySysAbnormal"];
                                    if(!string.IsNullOrWhiteSpace(form["TxtUrinarySysAbnormal99Other"]))
                                    {
                                        CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtUrinarySysAbnormal99Other"]);
                                    }
                                    CareRecordCont += " 問題存。";
                                }
                                else
                                {
                                    CareRecordCont += "\r\n泌尿系統：正常。";
                                }
                            }
                            if(!string.IsNullOrWhiteSpace(form["RbnSkinAppearance"]))
                            { //皮膚狀況 一般外觀
                                if(form["RbnSkinAppearance"] != "正常")
                                {
                                    CareRecordCont += "\r\n皮膚狀況：";
                                    if(!string.IsNullOrWhiteSpace(form["ChkSkinAppearanceAbn"]))
                                    {
                                        foreach(string item in form["ChkSkinAppearanceAbn"].Split(','))
                                        {
                                            switch(item)
                                            {
                                                case "乾燥":
                                                    CareRecordCont += form["ChkSkinAppearance1Position"];
                                                    if(!string.IsNullOrWhiteSpace(form["Txt_SkinAppearance1Position_Other"]))
                                                    {
                                                        CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["Txt_SkinAppearance1Position_Other"]);
                                                    }
                                                    CareRecordCont += " " + item + " 問題存。";
                                                    break;
                                                case "水腫":
                                                    CareRecordCont += form["ChkSkinAppearance2Position"];
                                                    if(!string.IsNullOrWhiteSpace(form["Txt_SkinAppearance2Position_Other"]))
                                                    {
                                                        CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["Txt_SkinAppearance2Position_Other"]);
                                                    }
                                                    CareRecordCont += " " + item + " 問題存。";
                                                    break;
                                                case "黃疸":
                                                    CareRecordCont += form["ChkSkinAppearance3Position"];
                                                    if(!string.IsNullOrWhiteSpace(form["Txt_SkinAppearance3Position_Other"]))
                                                    {
                                                        CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["Txt_SkinAppearance3Position_Other"]);
                                                    }
                                                    CareRecordCont += " " + item + " 問題存。";
                                                    break;
                                                case "發紺":
                                                    CareRecordCont += form["ChkSkinAppearance4Position"];
                                                    if(!string.IsNullOrWhiteSpace(form["Txt_SkinAppearance4Position_Other"]))
                                                    {
                                                        CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["Txt_SkinAppearance4Position_Other"]);
                                                    }
                                                    CareRecordCont += " " + item + " 問題存。";
                                                    break;
                                                case "腫塊":
                                                    CareRecordCont += form["ChkSkinAppearance5Position"];
                                                    if(!string.IsNullOrWhiteSpace(form["Txt_SkinAppearance5Position_Other"]))
                                                    {
                                                        CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["Txt_SkinAppearance5Position_Other"]);
                                                    }
                                                    CareRecordCont += " " + item + " 問題存。";
                                                    break;
                                                case "疹":
                                                    CareRecordCont += form["ChkSkinAppearance6Position"];
                                                    if(!string.IsNullOrWhiteSpace(form["Txt_SkinAppearance6Position_Other"]))
                                                    {
                                                        CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["Txt_SkinAppearance6Position_Other"]);
                                                    }
                                                    CareRecordCont += " " + item + " 問題存。";
                                                    break;
                                                case "癢":
                                                    CareRecordCont += form["ChkSkinAppearance7Position"];
                                                    if(!string.IsNullOrWhiteSpace(form["Txt_SkinAppearance7Position_Other"]))
                                                    {
                                                        CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["Txt_SkinAppearance7Position_Other"]);
                                                    }
                                                    CareRecordCont += " " + item + " 問題存。";
                                                    break;
                                                case "其他":
                                                    //CareRecordCont += item;
                                                    CareRecordCont += form["ChkSkinAppearance8Position"];
                                                    if(!string.IsNullOrWhiteSpace(form["Txt_SkinAppearance8Position_Other"]))
                                                    {
                                                        CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["Txt_SkinAppearance8Position_Other"]);
                                                    }
                                                    if(!string.IsNullOrWhiteSpace(form["TxtSkinAppearanceAbn99Other"]))
                                                    {
                                                        CareRecordCont += " " + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtSkinAppearanceAbn99Other"]);
                                                    }
                                                    CareRecordCont += " 問題存。";
                                                    break;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    CareRecordCont += "\r\n皮膚狀況：正常。";
                                }
                            }
                            if(!string.IsNullOrWhiteSpace(form["RbnSkeletalSystem"]))
                            {//骨骼系統
                                if(form["RbnSkeletalSystem"] != "正常")
                                {
                                    CareRecordCont += "\r\n骨骼系統：";
                                    if(!string.IsNullOrWhiteSpace(form["ChkSkeletalSystemAbn"]))
                                    {
                                        foreach(string item in form["ChkSkeletalSystemAbn"].Split(','))
                                        {
                                            switch(item)
                                            {
                                                case "駝背":
                                                    CareRecordCont += item + " 問題存。";
                                                    break;
                                                case "骨折":
                                                    CareRecordCont += form["ChkSkeletalSys2Position"] + " " + item + " 問題存。";
                                                    break;
                                                case "脫臼":
                                                    CareRecordCont += form["ChkSkeletalSys3Position"] + " " + item + " 問題存。";
                                                    break;
                                                case "關節腫":
                                                    CareRecordCont += form["ChkSkeletalSys4Position"] + " " + item + " 問題存。";
                                                    break;
                                                case "截肢":
                                                    CareRecordCont += form["ChkSkeletalSys5Position"] + " " + item + " 問題存。";
                                                    break;
                                                case "畸形":
                                                    CareRecordCont += form["ChkSkeletalSys6Position"] + " " + item + " 問題存。";
                                                    break;
                                                case "脊椎側彎":
                                                    CareRecordCont += item + " 問題存。";
                                                    break;
                                                case "其他":
                                                    if(!string.IsNullOrWhiteSpace(form["TxtSkeletalSystemAbn99Other"]))
                                                        CareRecordCont += this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtSkeletalSystemAbn99Other"]) + " 問題存。";
                                                    break;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    CareRecordCont += "\r\n骨骼系統：正常。";
                                }
                            }
                            if(!string.IsNullOrWhiteSpace(form["RbnReproductiveSysSex"]))
                            {//生殖系統
                                if(form["RbnReproductiveSysSex"] != "女")
                                {//男
                                    if(!string.IsNullOrWhiteSpace(form["RbnReproductiveSysStatus"]))
                                    {
                                        if(form["RbnReproductiveSysStatus"] != "正常")
                                        {
                                            CareRecordCont += "\r\n生殖系統：";
                                            foreach(string item in form["ChkRepSysMale"].Split(','))
                                            {
                                                switch(item)
                                                {
                                                    case "睪丸未降":
                                                        if(!string.IsNullOrWhiteSpace(form["ChkRepSysMale1Site"]))
                                                        {
                                                            CareRecordCont += (form["ChkRepSysMale1Site"] != "右,左") ? (form["ChkRepSysMale1Site"]) : ("雙側");
                                                        }
                                                        CareRecordCont += item + ",";
                                                        break;
                                                    case "尿道下裂":
                                                        CareRecordCont += item + ",";
                                                        break;
                                                    case "陰囊水腫":
                                                        CareRecordCont += item + ",";
                                                        break;
                                                    case "其他":
                                                        // CareRecordCont += item;
                                                        if(!string.IsNullOrWhiteSpace(form["TxtRepSysMale99Other"]))
                                                        {
                                                            CareRecordCont += this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtRepSysMale99Other"]);
                                                        }
                                                        break;
                                                }
                                            }
                                            CareRecordCont += " 問題存。";
                                        }
                                        else
                                        {
                                            CareRecordCont += "\r\n生殖系統：正常。";
                                        }
                                    }
                                }
                                else
                                {//女
                                    if(!string.IsNullOrWhiteSpace(form["RbnReproductiveSysStatus"]))
                                    {
                                        if(form["RbnReproductiveSysStatus"] != "正常")
                                        {
                                            CareRecordCont += "\r\n生殖系統：";
                                            CareRecordCont += form["ChkRepSysFemale"];
                                            if(!string.IsNullOrWhiteSpace(form["TxtRepSysFemale99Other"]))
                                            {
                                                CareRecordCont = CareRecordCont.Substring(0, CareRecordCont.Length - 2) + this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtRepSysFemale99Other"]);
                                            }
                                            CareRecordCont += " 問題存。";
                                        }
                                        else
                                        {
                                            CareRecordCont += "\r\n生殖系統：正常。";
                                        }
                                    }
                                }
                            }
                            if(!string.IsNullOrWhiteSpace(form["TxtElseRemark"].Trim()))
                            {//備註//其他
                                CareRecordCont += "\r\n備註：";
                                CareRecordCont += this.DailyBodyAssessment.trans_special_code_with_Daily_Body(form["TxtElseRemark"]) + "。";
                            }*/


                            #endregion 兒童身體評估--護理記錄組字 end
                            //if(base.Upd_CareRecord(DBAMDTM, ID, "身體評估", CareRecordCont, "", "", "", "", "DAILY_BODY_ASSESSMENT_MASTER") == 0)
                            base.Insert_CareRecord_Black(DBAMDTM, ID, "身體評估", CareString, "", "", "", "");
                        }
                        #endregion
                    }
                    if(SaveStatus == true && TempStatus != "Y" && string.IsNullOrEmpty(CareString) && form["ChkPlanYN"] == null)
                    {//判斷是否為修改(且暫存flag不能等於Y，且CareString等於無字狀態，表示這筆已經是修改完整版的內容)，是就是X，不是就是Y
                        Response.Write("X");
                    }
                    else
                    {
                        Response.Write("Y");
                    }
                }
                else
                {
                    Response.Write("N");
                }
                return new EmptyResult();
            }
            Response.Write("O");
            return new EmptyResult();
        }

        //刪除評估
        [HttpPost]
        public string DelDailyAssessInfo(string DBAMID)
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

                ERow = DailyBodyAssessment.DBExecUpdate("DAILY_BODY_ASSESSMENT_MASTER", DBItemDataList, string.Format("FEENO={0} AND DBAM_ID={1}", DigitFlapTemperature.SQLString(feeno), DigitFlapTemperature.SQLString(DBAMID)));
                if(ERow > 0)
                {
                    ERow += base.Del_CareRecord(DBAMID, "DAILY_BODY_ASSESSMENT_MASTER");
                }

                if(ERow > 0)
                    return "Y";
                else
                    return "N";
            }

            return "O";
        }

        #region  列印

        //列印
        public ActionResult List_PDF(string LoginEmpNo, string feeno, string type, string Std, string Stt, string Edd, string Edt)
        {
            string userno = "";  //這裡無法直接使用 userinfo ，會造成無法抓取的情況，故用參數值

            PatientInfo pinfo = new PatientInfo();
            byte[] ByteCode = webService.GetPatientInfo(feeno);
            //病人資訊
            if(ByteCode != null)
                pinfo = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(ByteCode));
            ViewData["ptinfo"] = pinfo;

            DataTable DT_M = new DataTable(), DT_D = new DataTable();
            List<DailyBodyData> bd = new List<DailyBodyData>();
            string TempUserName = string.Empty;
            string[] ArrItem = { };
            if(string.IsNullOrWhiteSpace((Std + " " + Stt).Trim()))
            {
                Std = DateTime.Now.AddDays(-7).ToString("yyyy/MM/dd"); //預設查詢區間為目前至一週前
                Stt = "00:00";
            }
            if(string.IsNullOrWhiteSpace((Edd + " " + Edt).Trim()))
            {
                Edd = DateTime.Now.ToString("yyyy/MM/dd");
                Edt = DateTime.Now.ToString("23:59");
            }
            DT_M = DailyBodyAssessment.sel_dailybodyassess_data_M(feeno, type, Std + " " + Stt, Edd + " " + Edt);//取主表的值
            DT_D = DailyBodyAssessment.sel_dailybodyassess_data_D(feeno, type, Std + " " + Stt, Edd + " " + Edt);//取子表的值
            for(int i = 0; i < DT_M.Rows.Count; i++)
            {//先過濾ID後，再裝成新的DataTable
                DataTable newDT = NIS.Models.DailyBodyAssessment.FiltData(DT_D, "DBAM_ID='" + DT_M.Rows[i]["DBAM_ID"].ToString() + "'", "");
                if(newDT != null && newDT.Rows.Count > 0)
                {
                    Array.Resize(ref ArrItem, newDT.Rows.Count);//重新定義陣列長度
                    for(int j = 0; j < newDT.Rows.Count; j++)
                    {//將A的資料丟成字串陣列
                        ArrItem[j] = newDT.Rows[j]["A"].ToString();
                    }
                }
                userno = LoginEmpNo;  //登入者
                if(userno != DT_M.Rows[i]["CREANO"].ToString())
                {//登入者和紀錄者資料不相同時，訂正 userno
                    userno = DT_M.Rows[i]["CREANO"].ToString();
                }

                //        //因為 userinfo 無法使用，所以不論相不相同，都要重抓名字
                byte[] listByteCode = webService.UserName(userno);
                if(listByteCode != null)
                {
                    string listJsonArray = CompressTool.DecompressString(listByteCode);
                    UserInfo other_user_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                    TempUserName = other_user_info.EmployeesName;
                }
                bd.Add(new NIS.Models.DailyBodyData(DT_M.Rows[i]["DBAM_ID"].ToString().Trim(), DT_M.Rows[i]["DBAM_DTM"].ToString().Trim(), ArrItem, DT_M.Rows[i]["CREANO"].ToString().Trim(), TempUserName, DT_M.Rows[i]["DBAM_TEMP_TYPE"].ToString().Trim()));
                ArrItem = null;
            }
            ViewBag.DBAMType = type;
            ViewData["BodyData"] = bd;
            bd = null;
            ViewBag.userno = userno;
            ViewBag.RootDocument = GetSourceUrl();
            ViewBag.FeeNo = feeno;
            return View("List_PDF" + type);
        }

        #endregion  列印 end

        #region  共用
        /// <summary>
        /// 設定所有項目的TYPE
        /// </summary>
        /// <param name="AllKey">所有項目</param>
        /// <param name="AllType">所有項目的TYPE</param>
        private Dictionary<string, string> SetHashtable(string[] AllKey, string[] AllType)
        {
            Dictionary<string, string> InputType = new Dictionary<string, string>();

            for(int i = 0; i < AllKey.Length; i++)
                InputType.Add(AllKey[i], AllType[i]);

            return InputType;
        }

        #endregion  共用 end

    }
}
