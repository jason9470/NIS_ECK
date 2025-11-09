using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using NIS.Data;
using NIS.Models;
using NIS.UtilTool;
using NIS.WebService;

namespace NIS.Controllers
{
    public class PatientListController : BaseController
    {
        //
        // GET: /PatientList/
        private CommData cd;
        private DBConnector link;
        private Assess ass_m;
        private Function func_m;
        

        public PatientListController()
        {
            this.cd = new CommData();
            this.link = new DBConnector();
            this.ass_m = new Assess();
            this.func_m = new Function();
        }

        #region 病人清單

        #region --病人清單頁面--
        public ActionResult List()
        {
            List<string> cost_code = new List<string>();
            this.GetUnitDDL(cost_code);
            ViewBag.RootDocument = GetSourceUrl();
            ViewBag.userno = userinfo.CostCenterCode;
            ViewBag.userinfo = userinfo;
            ViewBag.Popen_mode = "mylist";
            return View();
        }
        #endregion

        
        #region --病人清單-(我的病人)列表取得 --
        [HttpPost]
        public ActionResult PatList()
        {
            List<PatientListPage> tdList = new List<PatientListPage>();
            List<SpecialNotes> spnote = new List<SpecialNotes>();
            string Popen_mode = Request["Popen_mode"];
            string PQuery = Request["PQuery"];
            DataTable dt = new DataTable();
            dt.Columns.Add("FEENO");
            dt.Columns.Add("Exam");
            dt.Columns.Add("op");
            dt.Columns.Add("call");

            if(Popen_mode != null)
            {
                byte[] ptByteCode = null;
                switch(Popen_mode)
                {
                    //依照我的病人
                    case "mylist":
                        ptByteCode = webService.GetPatientListByBedList(GetMyPatientList());
                        break;
                    //按區域及條件式找
                    case "rb_unit":
                        ptByteCode = webService.GetPatientList(PQuery);
                        break;
                    //依病歷號找
                    case "rb_chrno":
                        ptByteCode = webService.GetInHistory(PQuery);//叫用WebService
                        break;
                    case "rb_bedno":
                        ptByteCode = webService.GetPatientListByBedList(UseBedNoGetData(PQuery));
                        break;
                    case "discharge":
                        ptByteCode = webService.GetExLeaveHospital();
                        break;
                }

                if(ptByteCode != null)
                {
                    string ptJsonArr = CompressTool.DecompressString(ptByteCode);
                    List<NIS.Data.PatientInfo> patList = new List<PatientInfo>();
                    try
                    {
                        patList = JsonConvert.DeserializeObject<List<NIS.Data.PatientInfo>>(ptJsonArr);
                        if(patList[0] == null && patList[1] == null)
                            patList.RemoveRange(1, patList.Count - 1);
                        patList = patList.OrderBy(x => x.BedNo).ToList();//按照床號排序
                    }
                    catch(Exception ex)
                    {
                        Response.Write("無法取得資料！");
                        string www = ex.ToString();
                        return new EmptyResult();
                    }
                    if (Popen_mode == "rb_chrno")//18/AUG/17在恩主公無法用查病歷功能，發現BUG是因:單一病號從WS搜的住院紀錄大部分不會只有一筆 所以新增判斷式
                    {//刪除所有!="還未出院"的紀錄
                        patList.RemoveAll(x => x.OutDate != Convert.ToDateTime("2910/12/31"));//註:由於"OutDate出院日期"的WS欄位是設定DateTime型別無法設為Null，因此還未出院的是設為2910/12/31
                    }
                    if(patList[0] != null)
                    {
                        for(int y = 0; y < patList.Count; y++)
                        {
                            //取得單筆個案的所有資訊
                            byte[] ptinfoByteCode = webService.GetPatientInfo(patList[y].FeeNo);
                            if(ptinfoByteCode != null)
                            {//若超過一筆解壓縮就會出錯，所以新增上面的篩選判斷式，只找出尚未出院的那一筆
                                string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                                PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);

                                spnote = new List<SpecialNotes>();

                                #region 特殊註記--設定內容
                                string fall = "";
                                if(pi.Age >= 18)
                                {
                                    DataTable dt_fall = ass_m.sel_fall_assess_data(pi.FeeNo.Trim(), "", "NIS_FALL_ASSESS_DATA");
                                    if(dt_fall.Rows.Count > 0)
                                    {
                                        if(int.Parse(dt_fall.Rows[0]["TOTAL"].ToString()) >= 3)
                                            fall = "跌";
                                    }
                                }
                                else
                                {
                                    DataTable dt_fall = ass_m.sel_fall_assess_data(pi.FeeNo.Trim(), "", "NIS_FALL_ASSESS_DATA_CHILD");
                                    if(dt_fall.Rows.Count > 0)
                                    {
                                        if(int.Parse(dt_fall.Rows[0]["TOTAL"].ToString()) >= 3)
                                            fall = "跌";
                                    }
                                }
                                                                
                                string Constraint = "";//約束的操作
                                DataTable dt_Constraint = new DataTable();
                                string StrsqlConstraint = "SELECT * FROM BINDTABLE WHERE 1=1  AND FEENO = '" + pi.FeeNo.Trim() + "' AND STATUS <> 'del'  ORDER BY LPAD(BOUT,3,'0'),INSDT";
                                dt_Constraint = this.link.DBExecSQL(StrsqlConstraint);
                                if(dt_Constraint != null && dt_Constraint.Rows.Count > 0)
                                {
                                    int ENDDTCount = 0;
                                    for(int i = 0; i < dt_Constraint.Rows.Count; i++)
                                    {
                                        if(dt_Constraint.Rows[i]["ENDDT"].ToString() == "")
                                        {
                                            ENDDTCount++;
                                        }
                                    }
                                    if(ENDDTCount > 0)
                                    {
                                        Constraint = "約";
                                    }
                                }

                                //2017/7/27 新增傷害的操作
                                string Suicide = "";
                                DataTable dt_Suicide = new DataTable();
                                string StrsqlSuicide = "SELECT total_score FROM MOOD_ASSESSMENT_DATA where  ASSESS_DT in (SELECT MAX(ASSESS_DT) FROM MOOD_ASSESSMENT_DATA WHERE FEE_NO ='" + pi.FeeNo.Trim() + "' and DEL_USER is null ) AND DEL_USER is null AND TOTAL_SCORE >= 10";
                                dt_Suicide = this.link.DBExecSQL(StrsqlSuicide);
                                if (dt_Suicide != null && dt_Suicide.Rows.Count > 0)
                                {                                
                                        Suicide = "傷";                                   
                                }

                                //給藥註記
                                //"yyyy/MM/dd tt hh:mm:ss"
                                string Drug = "";
                                DataTable dt_Drug = new DataTable();
                                int time_desc = int.Parse(DateTime.Now.ToString("HHmm"));
                                string StartTime = DateTime.Today.ToString("yyyy/MM/dd");
                                string EndTime = DateTime.Today.ToString("yyyy/MM/dd");
                                if (time_desc >= 800 && time_desc <= 1559)
                                {
                                    StartTime += " 08:00:00";
                                    EndTime += " 15:59:59";
                                }
                                else if (time_desc >= 1600 && time_desc <= 2359)
                                {
                                    StartTime += " 16:00:00";
                                    EndTime += " 23:59:59";
                                }
                                else
                                {
                                    StartTime += " 00:00:00";
                                    EndTime += " 07:59:59";
                                }
                                string StrsqlDrug = "select * from drug_execute where fee_no ='" + pi.FeeNo.Trim() + "' and ";
                                StrsqlDrug +=  "to_date(drug_date, 'yyyy/MM/dd AM hh:mi:ss') between to_date('" + StartTime + "', 'yyyy/MM/dd hh24:mi:ss') and to_date('" + EndTime + "', 'yyyy/MM/dd hh24:mi:ss') and exec_date is null";
                                dt_Drug = this.link.DBExecSQL(StrsqlDrug);
                                if (dt_Drug != null && dt_Drug.Rows.Count > 0)
                                {
                                    Drug = "藥";
                                }



                                string discharge = string.Empty;
                                spnote.Add(new SpecialNotes()
                                {
                                    DNR = (pi.DNR == "Y") ? "拒" : "",
                                    Allergy = (pi.Allergy == "Y") ? "敏" : "",
                                    Security = (pi.Security == "Y") ? "密" : "",
                                    IcnDiseaseStr = (pi.IcnDiseaseStr != null) ? "隔" : (func_m.sel_icndiseasestr_data(pi.FeeNo.Trim()).Rows.Count > 0) ? "隔" : "",
                                    Hospice = (pi.Hospice == "Y") ? "寧" : "",
                                    OrganDonation = (pi.OrganDonation == "Y") ? "捐" : "",
                                    fall_assess = fall,
                                    pressure = (func_m.sel_pressure_data(pi.FeeNo.Trim()).Rows.Count > 0) ? "壓" : "",
                                    pain = (func_m.sel_pain_data(pi.FeeNo.Trim()).Rows.Count > 0) ? "痛" : "",
                                    discharge = discharge,
                                    //Suicide = (pi.Suicide == "Y") ? "傷" : "",
                                    //Suicide = (func_m.sel_suicide_data(ptinfo.FeeNo.Trim()).Rows.Count > 0) ? "傷" : "",
                                    //viewBag.suicide = 
                                    Suicide = Suicide,
                                    Drug = Drug,

                                    Constraint = Constraint,
                                });
                                
                                DataRow row = dt.NewRow();
                                byte[] piNoteByteCode = webService.GetPatientNote(patList[y].FeeNo);
                                if (ptinfoByteCode != null)
                                {
                                    string piNoteJosnArr = CompressTool.DecompressString(piNoteByteCode);
                                    PatientInfo piNote = JsonConvert.DeserializeObject<PatientInfo>(piNoteJosnArr);

                                    row["FEENO"] = patList[y].FeeNo;
                                    row["Exam"] = (piNote.Exam!=null&& piNote.Exam =="Y") ? "Y" : "N";
                                    row["op"] = (piNote.Surgery != null && piNote.Surgery == "Y") ? "Y" : "N";
                                    row["call"] = (piNote.Consultation!= null && piNote.Consultation == "Y") ? "Y" : "N";
                                }
                                dt.Rows.Add(row);
                                #endregion
                                tdList.Add(new PatientListPage(pi, spnote));
                                spnote = null;
                                pi = null;
                            }
                        }
                    }
                }
                else
                {
                    //如果Webservice沒資料且又是預計出院病人的按鈕
                    if(Popen_mode == "discharge")
                    {
                        Response.Write("<script>showHint('目前沒有預計出院的病人。');</script>");
                    }
                }
                ViewBag.dt = dt;
                ViewData["tdList"] = tdList;
                ViewBag.Popen_mode = Popen_mode;
                return View();
            }
            return new EmptyResult();
        }
        #endregion

        #region --取得我的病人床位清單 --
        private string[] GetMyPatientList(string type ="show", string EmployeesNo = "", string jobtype="")
        {
            List<string> retArr = new List<string>();

            try
            { 
                string dt_name = "";
                string sqlstr = "";
                if (type=="show")
                {
                    EmployeesNo = userinfo.EmployeesNo;
                    jobtype = userinfo.Category;
                }

                if (jobtype == "NA")
                {
                    dt_name = "DATA_FAVPAT";
                    sqlstr = " SELECT BED_NO FROM " + dt_name + " WHERE ";
                    sqlstr += " EMPLOYE_NO = '" + EmployeesNo + "' AND DATEITEM = TO_CHAR(SYSDATE,'yyyy/MM/dd')";
                }
                else
                {
                    dt_name = "DATA_DISPATCHING";
                    sqlstr = " SELECT BED_NO FROM " + dt_name + " WHERE ";
                    sqlstr += " RESPONSIBLE_USER = '" + EmployeesNo + "' AND SHIFT_DATE = TO_DATE(TO_CHAR(SYSDATE,'yyyy/MM/dd'), 'yyyy/MM/dd') ";
                }
                DataTable Dt = this.link.DBExecSQL(sqlstr);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        retArr.Add(Dt.Rows[i]["bed_no"].ToString().Trim());
                    }
                }
                return retArr.ToArray();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(),ex);
                return retArr.ToArray();
            }
        }
        #endregion

        #region --用床號先裝進陣列--
        private string[] UseBedNoGetData(string bedno)
        {
            List<string> retArr = new List<string>();
            retArr.Add(bedno);
            return retArr.ToArray();
        }
        #endregion

        #region 設定使用者預設護理站
        private void GetUnitDDL(List<string> cost_code)
        {
            byte[] listByteCode = webService.GetCostCenterList();
            string listJsonArray = CompressTool.DecompressString(listByteCode);
            List<CostCenterList> costlist = JsonConvert.DeserializeObject<List<CostCenterList>>(listJsonArray);
            List<SelectListItem> cCostList = new List<SelectListItem>();
            //第三順位_否則使用者歸屬單位
            string set_cost = userinfo.CostCenterCode.Trim();
            //第一順位_使用者有選擇過
            if(Request["cost_code"] != null)
                set_cost = Request["cost_code"];
            //第二順位_派班表有_以第一筆為優先
            else if(cost_code.Count > 0)
                set_cost = cost_code[0];

            for(int i = 0; i < costlist.Count; i++)
            {
                bool select = false;
                if(set_cost == costlist[i].CostCenterCode.Trim())
                    select = true;
                cCostList.Add(new SelectListItem()
                {
                    Text = costlist[i].CCCDescription.Trim(),
                    Value = costlist[i].CostCenterCode.Trim(),
                    Selected = select
                });
            }

            ViewData["costlist"] = cCostList;
            
        }
        #endregion

        #region --預覽資料--

        /// <summary>
        /// 預覽分頁
        /// </summary>
        /// <param name="feeno">批價序號</param>
        /// <param name="type">類型(檢查、會診、手術)</param>
        /// <returns></returns>頁面
        public ActionResult PartialData(string feeno, string type)
        {
            switch(type)
            {
                case "exam":
                    ViewData["exam"] = get_TransferDuty_Item_exam(feeno, "Item");
                    break;
                case "call":
                    ViewData["call"] = get_TransferDuty_Item_Consultation(feeno, "Item");
                    break;
                case "op":
                    ViewData["oper"] = get_TransferDuty_Item_op(feeno, "Main");
                    break;
            }


            ViewBag.feeno = feeno;
            ViewBag.type = type;
            return View();
        }

        #region --呼叫Web Service--檢查，會診，手術--
        /// <summary> 取得檢查 </summary>
        public List<Exam> get_TransferDuty_Item_exam(string fee_no, string flag)
        {
            List<Exam> examList = new List<Exam>();
            byte[] examByte = webService.GetExam(fee_no);
            if(examByte != null)
            {
                string examJson = CompressTool.DecompressString(examByte);
                examList = JsonConvert.DeserializeObject<List<Exam>>(examJson);
            }
            return examList;
        }
        /// <summary> 取得會診 </summary>
        public List<Consultation> get_TransferDuty_Item_Consultation(string fee_no, string flag)
        {
            List<Consultation> ConsultationList = new List<Consultation>();
            byte[] ConsultationByte = webService.GetPatientConsult(fee_no);
            if(ConsultationByte != null)
            {
                string ConsultationJson = CompressTool.DecompressString(ConsultationByte);
                ConsultationList = JsonConvert.DeserializeObject<List<Consultation>>(ConsultationJson);
            }

            string orderdt = "";
            string content = "";
            List<Consultation> PTList2 = new List<Consultation>();
            for(int j = 0; j < ConsultationList.Count; j++)
            {
                //日期如果不等於該日期時
                if(orderdt != ConsultationList[j].OrderDate.ToString("yyyy/MM/dd HH:mm:ss").Trim())
                {
                    PTList2.Add(new Consultation()
                    {
                        OrderDate = ConsultationList[j].OrderDate,
                        ConsDoc = ConsultationList[j].ConsDoc,
                        ConsDept = ConsultationList[j].ConsDept,
                        ConsContent = ConsultationList[j].ConsContent,
                        OrderNo = ConsultationList[j].OrderNo,
                    });
                    orderdt = ConsultationList[j].OrderDate.ToString("yyyy/MM/dd HH:mm:ss").Trim();
                    content = "";
                }
                else//同一筆 內容需相加
                {
                    content = PTList2[PTList2.Count - 1].ConsContent.Trim() + " " + ConsultationList[j].ConsContent.ToString().Trim();
                    PTList2.RemoveAt(PTList2.Count - 1);
                    PTList2.Add(new Consultation()
                    {
                        OrderDate = ConsultationList[j].OrderDate,
                        ConsDoc = ConsultationList[j].ConsDoc,
                        ConsDept = ConsultationList[j].ConsDept,
                        ConsContent = content,
                        OrderNo = ConsultationList[j].OrderNo,
                    });
                    orderdt = ConsultationList[j].OrderDate.ToString("yyyy/MM/dd HH:mm:ss").Trim();
                    content = ConsultationList[j].ConsContent;
                }
            }
            return PTList2;
        }

        /// <summary> 取得手術 </summary>
        public List<Surgery> get_TransferDuty_Item_op(string fee_no, string flag)
        {
            List<Surgery> oper = new List<Surgery>();
            byte[] ptByte = webService.GetOpInfo(fee_no);
            if(ptByte != null)
            {
                string ptJson = CompressTool.DecompressString(ptByte);
                oper = JsonConvert.DeserializeObject<List<Surgery>>(ptJson);
            }
            return oper;
        }
        #endregion



        #endregion
        #endregion

        public ActionResult List_pdf(string PQuery, string userno,string EmployeesNo, string jobtype, string Popen_mode = "mylist")
        {
            List<string> cost_code = new List<string>();
            ViewBag.RootDocument = GetSourceUrl();
            ViewBag.userno = userno;
            List<PatientListPage> tdList = new List<PatientListPage>();
            DataTable dt = new DataTable();
            dt.Columns.Add("FEENO");
            dt.Columns.Add("Exam");
            dt.Columns.Add("op");
            dt.Columns.Add("call");
            if (Popen_mode != null)
            {
                byte[] ptByteCode = null;
                switch (Popen_mode)
                {
                    //依照我的病人
                    case "mylist":
                        ptByteCode = webService.GetPatientListByBedList(GetMyPatientList("print", EmployeesNo, jobtype));
                        break;
                    //按區域及條件式找
                    case "rb_unit":
                        ptByteCode = webService.GetPatientList(PQuery);
                        break;
                    //依病歷號找
                    case "rb_chrno":
                        ptByteCode = webService.GetInHistory(PQuery);//叫用WebService
                        break;
                    case "rb_bedno":
                        ptByteCode = webService.GetPatientListByBedList(UseBedNoGetData(PQuery));
                        break;
                    case "discharge":
                        ptByteCode = webService.GetExLeaveHospital();
                        break;
                }                        
                if (ptByteCode != null)
                {
                    string ptJsonArr = CompressTool.DecompressString(ptByteCode);
                    List<NIS.Data.PatientInfo> patList = new List<PatientInfo>();
                    try
                    {
                        patList = JsonConvert.DeserializeObject<List<NIS.Data.PatientInfo>>(ptJsonArr);
                        if (patList[0] == null && patList[1] == null)
                            patList.RemoveRange(1, patList.Count - 1);
                        patList = patList.OrderBy(x => x.BedNo).ToList();//按照床號排序
                    }
                    catch (Exception ex)
                    {
                        Response.Write("無法取得資料！");
                        string www = ex.ToString();
                        return new EmptyResult();
                    }
                    if (Popen_mode == "rb_chrno")//18/AUG/17在恩主公無法用查病歷功能，發現BUG是因:單一病號從WS搜的住院紀錄大部分不會只有一筆 所以新增判斷式
                    {//刪除所有!="還未出院"的紀錄
                        patList.RemoveAll(x => x.OutDate != Convert.ToDateTime("2910/12/31"));//註:由於"OutDate出院日期"的WS欄位是設定DateTime型別無法設為Null，因此還未出院的是設為2910/12/31
                    }
                    if (patList[0] != null)
                    {
                        for (int y = 0; y < patList.Count; y++)
                        {
                            //取得單筆個案的所有資訊
                            byte[] ptinfoByteCode = webService.GetPatientInfo(patList[y].FeeNo);
                            if (ptinfoByteCode != null)
                            {//若超過一筆解壓縮就會出錯，所以新增上面的篩選判斷式，只找出尚未出院的那一筆
                                string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                                PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);

                                List<SpecialNotes>spnote = new List<SpecialNotes>();

                                #region 特殊註記--設定內容
                                string fall = "";
                                if (pi.Age >= 18)
                                {
                                    DataTable dt_fall = ass_m.sel_fall_assess_data(pi.FeeNo.Trim(), "", "NIS_FALL_ASSESS_DATA");
                                    if (dt_fall.Rows.Count > 0)
                                    {
                                        if (int.Parse(dt_fall.Rows[0]["TOTAL"].ToString()) >= 3)
                                            fall = "跌";
                                    }
                                }
                                else
                                {
                                    DataTable dt_fall = ass_m.sel_fall_assess_data(pi.FeeNo.Trim(), "", "NIS_FALL_ASSESS_DATA_CHILD");
                                    if (dt_fall.Rows.Count > 0)
                                    {
                                        if (int.Parse(dt_fall.Rows[0]["TOTAL"].ToString()) >= 3)
                                            fall = "跌";
                                    }
                                }

                                string Constraint = "";//約束的操作
                                DataTable dt_Constraint = new DataTable();
                                string StrsqlConstraint = "SELECT * FROM BINDTABLE WHERE 1=1  AND FEENO = '" + pi.FeeNo.Trim() + "' AND STATUS <> 'del'  ORDER BY LPAD(BOUT,3,'0'),INSDT";
                                dt_Constraint = this.link.DBExecSQL(StrsqlConstraint);
                                if (dt_Constraint != null && dt_Constraint.Rows.Count > 0)
                                {
                                    int ENDDTCount = 0;
                                    for (int i = 0; i < dt_Constraint.Rows.Count; i++)
                                    {
                                        if (dt_Constraint.Rows[i]["ENDDT"].ToString() == "")
                                        {
                                            ENDDTCount++;
                                        }
                                    }
                                    if (ENDDTCount > 0)
                                    {
                                        Constraint = "約";
                                    }
                                }

                                //2017/7/27 新增傷害的操作
                                string Suicide = "";
                                DataTable dt_Suicide = new DataTable();
                                string StrsqlSuicide = "SELECT total_score FROM MOOD_ASSESSMENT_DATA where  ASSESS_DT in (SELECT MAX(ASSESS_DT) FROM MOOD_ASSESSMENT_DATA WHERE FEE_NO ='" + pi.FeeNo.Trim() + "' and DEL_USER is null ) AND DEL_USER is null AND TOTAL_SCORE >= 10";
                                dt_Suicide = this.link.DBExecSQL(StrsqlSuicide);
                                if (dt_Suicide != null && dt_Suicide.Rows.Count > 0)
                                {
                                    Suicide = "傷";
                                }

                                //給藥註記
                                //"yyyy/MM/dd tt hh:mm:ss"
                                string Drug = "";
                                DataTable dt_Drug = new DataTable();
                                int time_desc = int.Parse(DateTime.Now.ToString("HHmm"));
                                string StartTime = DateTime.Today.ToString("yyyy/MM/dd");
                                string EndTime = DateTime.Today.ToString("yyyy/MM/dd");
                                if (time_desc >= 800 && time_desc <= 1559)
                                {
                                    StartTime += " 08:00:00";
                                    EndTime += " 15:59:59";
                                }
                                else if (time_desc >= 1600 && time_desc <= 2359)
                                {
                                    StartTime += " 16:00:00";
                                    EndTime += " 23:59:59";
                                }
                                else
                                {
                                    StartTime += " 00:00:00";
                                    EndTime += " 07:59:59";
                                }
                                string StrsqlDrug = "select * from drug_execute where fee_no ='" + pi.FeeNo.Trim() + "' and ";
                                StrsqlDrug += "to_date(drug_date, 'yyyy/MM/dd AM hh:mi:ss') between to_date('" + StartTime + "', 'yyyy/MM/dd hh24:mi:ss') and to_date('" + EndTime + "', 'yyyy/MM/dd hh24:mi:ss') and exec_date is null";
                                dt_Drug = this.link.DBExecSQL(StrsqlDrug);
                                if (dt_Drug != null && dt_Drug.Rows.Count > 0)
                                {
                                    Drug = "藥";
                                }



                                string discharge = string.Empty;
                                spnote.Add(new SpecialNotes()
                                {
                                    DNR = (pi.DNR == "Y") ? "拒" : "",
                                    Allergy = (pi.Allergy == "Y") ? "敏" : "",
                                    Security = (pi.Security == "Y") ? "密" : "",
                                    IcnDiseaseStr = (pi.IcnDiseaseStr != null) ? "隔" : (func_m.sel_icndiseasestr_data(pi.FeeNo.Trim()).Rows.Count > 0) ? "隔" : "",
                                    Hospice = (pi.Hospice == "Y") ? "寧" : "",
                                    OrganDonation = (pi.OrganDonation == "Y") ? "捐" : "",
                                    fall_assess = fall,
                                    pressure = (func_m.sel_pressure_data(pi.FeeNo.Trim()).Rows.Count > 0) ? "壓" : "",
                                    pain = (func_m.sel_pain_data(pi.FeeNo.Trim()).Rows.Count > 0) ? "痛" : "",
                                    discharge = discharge,
                                    //Suicide = (pi.Suicide == "Y") ? "傷" : "",
                                    //Suicide = (func_m.sel_suicide_data(ptinfo.FeeNo.Trim()).Rows.Count > 0) ? "傷" : "",
                                    //viewBag.suicide = 
                                    Suicide = Suicide,
                                    Drug = Drug,

                                    Constraint = Constraint,
                                });                                
                                DataRow row = dt.NewRow();
                                byte[] piNoteByteCode = webService.GetPatientNote(patList[y].FeeNo);
                                if (ptinfoByteCode != null)
                                {
                                    string piNoteJosnArr = CompressTool.DecompressString(piNoteByteCode);
                                    PatientInfo piNote = JsonConvert.DeserializeObject<PatientInfo>(piNoteJosnArr);

                                    row["FEENO"] = patList[y].FeeNo;
                                    row["Exam"] = (piNote.Exam != null && piNote.Exam == "Y") ? "Y" : "N";
                                    row["op"] = (piNote.Surgery != null && piNote.Surgery == "Y") ? "Y" : "N";
                                    row["call"] = (piNote.Consultation != null && piNote.Consultation == "Y") ? "Y" : "N";
                                }
                                dt.Rows.Add(row);
                                #endregion
                                tdList.Add(new PatientListPage(pi, spnote));
                                spnote = null;
                                pi = null;
                                
                            }
                        }
                    }
                }
                else
                {
                    //如果Webservice沒資料且又是預計出院病人的按鈕
                    if (Popen_mode == "discharge")
                    {
                        Response.Write("<script>showHint('目前沒有預計出院的病人。');</script>");
                    }
                }
                ViewData["tdList"] = tdList;
                ViewBag.dt = dt;
                return View();
            }
            return new EmptyResult();
        }
    }
}
