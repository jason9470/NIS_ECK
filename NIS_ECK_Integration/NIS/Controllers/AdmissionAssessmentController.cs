using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.Text;
using System.Data;
using System.Collections;
using System.Diagnostics;
using NIS.Data;
using NIS.Models;
using NIS.UtilTool;
using NIS.WebService;
using Newtonsoft.Json;
using System.Configuration;
using Com.Mayaminer;
using System.Xml;
using System.Data.OleDb;
using System.Net;
using System.Text.RegularExpressions;
using Oracle.ManagedDataAccess.Client;
using DocumentFormat.OpenXml.Office2010.Excel;
using System.Windows.Interop;
using static DotNetOpenAuth.OpenId.Extensions.AttributeExchange.WellKnownAttributes;

namespace NIS.Controllers
{
    public class AdmissionAssessmentController : BaseController
    {
        Assess ass_m = new Assess();
        LogTool log = new LogTool();
        Obstetrics obs_m = new Obstetrics();
        CommData cd = new CommData();
        TubeManager tubem = new TubeManager();
        private DailyBodyAssessment DailyBodyAssessment;
        private string DirectUrl;
        public AssessPain_ECKController ap_eck = new AssessPain_ECKController();
        private DBConnector link;
        private Wound wound;

        public AdmissionAssessmentController()
        {
            this.DirectUrl = System.AppDomain.CurrentDomain.BaseDirectory;
            this.DailyBodyAssessment = new DailyBodyAssessment();
            this.link = new DBConnector();
            this.wound = new Wound();
        }

        #region 產生戒煙個管 XML 函式
        private XmlDocument createSuccessXmlDoc(string feeno, string date, string creator)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement element = doc.CreateElement("nis");
            doc.AppendChild(element);
            xmlAddChild(element, "feeNo", feeno);
            xmlAddChild(element, "date", date);
            xmlAddChild(element, "creator", creator);
            return doc;
        }

        private XmlElement xmlAddChild(XmlElement parent, String name, String value)
        {
            XmlElement element = parent.OwnerDocument.CreateElement(name);
            element.InnerText = value;
            parent.AppendChild(element);
            return element;
        }

        private XmlElement xmlAddChild(XmlElement parent, String name)
        {
            XmlElement element = parent.OwnerDocument.CreateElement(name);
            parent.AppendChild(element);
            return element;
        }

        private static void __outputSmokeXML(XmlElement pXmlNode, string param_cigarette, string param_cigarette_yes_year, string param_cigarette_yes_amount, string param_cigarette_agree_stop)
        {
            XmlElement _listNode = (XmlElement)pXmlNode.AppendChild(pXmlNode.OwnerDocument.CreateElement("smok"));
            XmlElement _itemNode = null;

            string status = string.Empty;
            if (param_cigarette == "1")
            {
                status = "無";
            }
            else if (param_cigarette == "2")
            {
                status = "有";
            }
            else
            {
                status = "戒煙";
            }

            string year = param_cigarette_yes_year;
            string count = string.Empty;
            try
            {
                count = (Math.Round((float.Parse(param_cigarette_yes_amount) / 20), 2)).ToString();
            }
            catch (Exception)
            {

            }
            string wish = param_cigarette_agree_stop;

            _itemNode = (XmlElement)_listNode.AppendChild(pXmlNode.OwnerDocument.CreateElement("status"));
            _itemNode.InnerText = status;

            if (param_cigarette == "2")
            {
                _itemNode = (XmlElement)_listNode.AppendChild(pXmlNode.OwnerDocument.CreateElement("year"));
                _itemNode.InnerText = year;
                _itemNode = (XmlElement)_listNode.AppendChild(pXmlNode.OwnerDocument.CreateElement("count"));
                _itemNode.InnerText = count;
                _itemNode = (XmlElement)_listNode.AppendChild(pXmlNode.OwnerDocument.CreateElement("wish"));
                _itemNode.InnerText = wish;
            }

        }

        private static void __outputWineXML(XmlElement pXmlNode, string param_drink, string param_drink_day, string param_drink_unit)
        {
            XmlElement _listNode = (XmlElement)pXmlNode.AppendChild(pXmlNode.OwnerDocument.CreateElement("wine"));
            XmlElement _itemNode = null;

            string status = param_drink;

            string bottle = param_drink_day;
            string cup = param_drink_unit;

            _itemNode = (XmlElement)_listNode.AppendChild(pXmlNode.OwnerDocument.CreateElement("status"));
            _itemNode.InnerText = status;

            if (status == "每天喝")
            {
                _itemNode = (XmlElement)_listNode.AppendChild(pXmlNode.OwnerDocument.CreateElement("bottle"));
                _itemNode.InnerText = bottle;
                _itemNode = (XmlElement)_listNode.AppendChild(pXmlNode.OwnerDocument.CreateElement("cup"));
                _itemNode.InnerText = cup;
            }

        }

        private static void __outputFooterXML(XmlElement pXmlNode, string param_marrage, string param_education, string param_EMGContact_3, string content)
        {
            XmlElement _listNode = (XmlElement)pXmlNode.AppendChild(pXmlNode.OwnerDocument.CreateElement("marriage"));
            _listNode.InnerText = param_marrage;

            _listNode = (XmlElement)pXmlNode.AppendChild(pXmlNode.OwnerDocument.CreateElement("education"));
            _listNode.InnerText = param_education;

            _listNode = (XmlElement)pXmlNode.AppendChild(pXmlNode.OwnerDocument.CreateElement("telephone"));
            _listNode.InnerText = param_EMGContact_3.Split(',').GetValue(0).ToString();

            _listNode = (XmlElement)pXmlNode.AppendChild(pXmlNode.OwnerDocument.CreateElement("nursingRecord"));
            _listNode.InnerText = content;

        }


        private string createcswbxml(string feeno, string createdate, string creator,
            string param_cigarette, string param_cigarette_yes_year, string param_cigarette_yes_amount, string param_cigarette_agree_stop,
            string param_drink, string param_drink_day, string param_drink_unit,
            string param_marrage, string param_education, string param_EMGContact_3, string content)
        {
            XmlDocument _doc = createSuccessXmlDoc(feeno, createdate, creator);
            __outputSmokeXML(_doc.DocumentElement, param_cigarette, param_cigarette_yes_year, param_cigarette_yes_amount, param_cigarette_agree_stop);
            __outputWineXML(_doc.DocumentElement, param_drink, param_drink_day, param_drink_unit);
            __outputFooterXML(_doc.DocumentElement, param_marrage, param_education, param_EMGContact_3, content);
            return _doc.InnerXml;
        }
        #endregion
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
        // 入院評估首頁
        public ActionResult AssessmentIndex(string page)
        {//判斷有無病人session
            if (Session["PatInfo"] != null)
            {
                if (page == null && page != "X")
                {
                    ViewBag.page = ptinfo.Assessment.Trim();
                }
                ViewBag.num = ass_m.sel_assessment_list(ptinfo.FeeNo.Trim(), "B").Rows.Count;
            }
            else
            {
                Response.Write("<script>alert('請重新選擇病患');</script>");
                return new EmptyResult();
            }
            return View();
        }

        public ActionResult Login()
        {
            return View();
        }

        public ActionResult AssessmentER(string tableid, string mode)
        {
            set_viewbag("ER", tableid, mode);
            ViewBag.dt_w_type = SetDropDownList.GetddlItem("", ass_m.sel_type_list("wound_type", "assess"), false, 0, 1);//種類
            ViewBag.dt_w_g = SetDropDownList.GetddlItem("", ass_m.sel_type_list("wound_general", "assess"), false, 0, 1);//一般
            ViewBag.dt_w_s = SetDropDownList.GetddlItem("", ass_m.sel_type_list("wound_scald", "assess"), false, 0, 1);//燒傷
            ViewBag.dt_w_p = SetDropDownList.GetddlItem("", ass_m.sel_type_list("wound_pressure", "assess"), false, 0, 1);//壓傷
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
                    Value = costlist[i].CCCDescription,
                    Selected = false
                });
            }
            ViewData["costlist"] = cCostList;
            return View();
        }

        //傳入_檢查_病歷號
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult check(FormCollection form)
        {
            string str = string.Empty;
            string chartno = form["PatientNo"];
            byte[] doByteCode = webService.GetInHistory(chartno);
            if (doByteCode != null)
            {
                string doJsonArr = CompressTool.DecompressString(doByteCode);
                List<InHistory> IpdList = JsonConvert.DeserializeObject<List<InHistory>>(doJsonArr);
                if (IpdList.Count > 0)
                {
                    Response.Write("<script>window.location.href='InHistory?chartno=" + chartno + "';</script>");
                    return new EmptyResult();
                }
            }

            Response.Write("<script>alert('此病歷號[" + chartno + "]無住院紀錄，請重新確認。');window.location.href='Login';</script>");
            return new EmptyResult();
        }

        //帶出FeeNo_List
        public ActionResult InHistory(string chartno)
        {
            if (chartno != null)
            {
                // 取得住院歷史資料
                byte[] inHistoryByte = webService.GetInHistory(chartno);
                if (inHistoryByte != null)
                {
                    string inHistoryJson = CompressTool.DecompressString(inHistoryByte);
                    List<InHistory> inHistoryList = JsonConvert.DeserializeObject<List<InHistory>>(inHistoryJson);
                    ViewData["inHistory"] = inHistoryList;
                }
            }
            return View();
        }

        //列出胎兒胎數
        public ActionResult Child_List(string feeno)
        {
            if (feeno != null)
            {
                Obstetrics obs_m = new Obstetrics();
                DataTable dt = obs_m.sel_Child_Brith(feeno, "", "");
                string num = "";
                if (dt.Rows.Count > 0)
                {
                    num = dt.Rows[0]["NUM"].ToString();
                    if (num == "0")
                        Response.Write("<script>window.opener.location.href='AssessmentNewBorn?mother_feeno=" + feeno + "&mode=insert';window.close();</script>");
                    else if (num == "1")
                        Response.Write("<script>window.opener.location.href='AssessmentNewBorn?mother_feeno=" + feeno + "&mode=insert&num=1';window.close();</script>");
                    else
                    {
                        ViewBag.dt = dt;
                        return View();
                    }
                }
                else
                    Response.Write("<script>opener.location.href = 'AssessmentNewBorn?mother_feeno=" + feeno + "&mode=insert';window.close();</script>");
            }
            return View();
        }

        /// <summary>
        /// 評估_列表
        /// </summary>
        /// <param name="natype">評估類型</param>
        public ActionResult AssessmentList(string natype)
        {
            try
            {
                DataTable dt = ass_m.sel_assessment_list(ptinfo.FeeNo.Trim(), natype);

                dt.Columns.Add("username");
                if (dt.Rows.Count > 0)
                {
                    string userno = dt.Rows[0]["MODIFYUSER"].ToString();
                    byte[] listByteCode = webService.UserName(userno);
                    string listJsonArray = CompressTool.DecompressString(listByteCode);
                    UserInfo user_name = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                    foreach (DataRow r in dt.Rows)
                    {
                        if (userno != r["MODIFYUSER"].ToString())
                        {
                            userno = r["MODIFYUSER"].ToString();
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

                if (natype == "C")
                {
                    DataTable dt_child_develope = ass_m.sel_child_develope(ptinfo.FeeNo);
                    if (dt_child_develope.Rows.Count > 0)
                        ViewBag.child_assess = dt_child_develope.Rows[0]["SEQ_NO"].ToString().Trim();
                    else
                    {
                        double birth = Math.Ceiling((DateTime.Now - ptinfo.Birthday).TotalDays);
                        if (birth <= 2505)
                        {
                            string strsql2 = "SELECT SEQ_NO FROM NIS_ASSESSMENTCHILD_MASTER WHERE STATUS ='Y' AND MINDAY <= " + birth + " AND MAXDAY >= " + birth;
                            DataTable dt_birth = new DataTable();
                            ass_m.DBExecSQL(strsql2, ref dt_birth);
                            if (dt_birth.Rows.Count > 0)
                                ViewBag.child_assess = dt_birth.Rows[0][0].ToString().Trim();
                        }
                    }
                }
                ViewBag.LoginEmployeesName = userinfo.EmployeesName;
                ViewBag.feeno = ptinfo.FeeNo;
                ViewBag.dt = dt;
                ViewBag.natype = natype;
                ViewBag.RootDocument = GetSourceUrl();

            }
            catch (Exception ex)
            {
                try
                {
                    //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                    string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                    string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                    write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                }
                catch (Exception e)
                {
                    //Do nothing
                }
            }
            return View();
        }

        // 兒童入院評估
        public ActionResult AssessmentChild(string tableid, string mode)
        {
            string natype = "C";//**
            if (tableid != null)
                set_viewbag(natype, tableid, mode);
            else
            {
                string[] item_id = { "param_education", "param_mentality", "param_economy", "param_payment", "param_living_style",
                                       "param_brother_elder", "param_brother_younger", "param_sister_elder", "param_sister_younger", "param_Volunteer_Help",
                                       "param_Volunteer_Help_Dtl", "param_need_placement", "param_primary_care", "param_primary_care_other",
                                       "param_care_education", "param_job", "param_job_other", "param_religion", "param_religion_other", "param_lang",
                                       "param_lang_other", "param_needtrans", "param_psychological", "param_psychological_other", "param_marrage",
                                       "param_cigarette", "param_cigarette_yes_amount", "param_cigarette_yes_year", "param_cigarette_agree_stop",
                                       "param_cigarette_stop_year", "param_BornHistory", "param_BornHistory_full_weight", "param_BornHistory_Preterm_weight",
                                       "param_BornHistory_Preterm_week", "param_BornHistory_Preterm_other", "param_FamilySickHistory", "param_fshDtl", "param_fshDtl_other",
                                       "param_PastHistory", "param_ipdAmt", "param_ipdReason", "param_ipdPlace", "param_opAmt", "param_opReason", "param_opPlace",
                                       "param_PastHistory_Innate", "param_PastHistory_Innate_dtl", "param_PastHistory_Innate_dtl_other", "param_PastHistory_Acquired",
                                       "param_PastHistory_Acquired_dtl", "param_PastHistory_Acquired_dtl_other", "param_med", "param_med_name", "param_med_frequency",
                                       "param_med_amount", "param_med_way", "param_allergy_med", "param_allergy_med_other", "param_allergy_med_other_2_name",
                                       "param_allergy_med_other_4_name", "param_allergy_med_other_6_name", "param_allergy_med_other_7_name", "param_allergy_med_other_8_name",
                                       "param_allergy_med_other_9_name", "param_allergy_med_other_10_name", "param_allergy_food", "param_allergy_food_other",
                                       "param_allergy_food_other_2_name", "param_allergy_food_other_4_name", "param_allergy_food_other_6_name", "param_allergy_other",
                                       "param_allergy_other_other", "param_allergy_other_other_1_name", "param_allergy_other_other_2_name", "param_allergy_other_other_3_name",
                                       "param_allergy_other_other_4_name", "param_allergy_other_other_5_name", "param_allergy_other_other_6_name", "param_Light", "param_Light_Boy_Abnormal",
                                       "param_Light_Boy_Abnormal_1", "param_Light_Boy_Abnormal_2", "param_FBAbnormal", "param_FBAbnormal_Dtl", "param_FBAbnormal_Dtl_other",
                                       "param_MCStart", "param_Last_MC", "param_MCCycle", "param_MCCycle_rule", "param_MCDay", "param_MCAmount", "param_FBAbnormalDtl",
                                       "param_FBAbnormalOther", "param_EMGContact", "param_ContactRole", "param_ContactRole_other", "param_EMGContact_1",
                                       "param_EMGContact_2", "param_EMGContact_3",
                                       "param_im_history","param_im_history_item_other","param_im_history_item_txt","param_im_history_item_other_txt"//曾患疾病
                                     ,"param_hasipd","param_ipd_past_count","param_ipd_past_reason","param_ipd_past_location"//住院次數
                                     ,"param_surgery","param_ipd_surgery_count","param_ipd_surgery_reason","param_ipd_surgery_location"//手術情形
                                     ,"param_blood","param_blood_reaction","transfusion_blood_dtl_txt","param_take_medicine","param_take_medicine_dtl_txt"//輸血經驗、服藥
                                     ,"param_Congenital_disease","Congenital_disease_dtl","Congenital_disease_dtl_other"//先天疾患
                                     ,"param_HepatitisB","param_HepatitisB1","param_HepatitisB2","param_HepatitisB3","param_5in1Vaccine1","param_5in1Vaccine2","param_5in1Vaccine3","param_5in1Vaccine4"//B型肝炎、五合一疫苗
                                     ,"param_MMR1","param_MMR2","param_VaricellaVaccine","param_JP_encephalitis1","param_JP_encephalitis2","param_JP_encephalitis3","param_JP_encephalitis4"//MMR、水痘疫苗、日本腦炎疫苗
                                     ,"param_DPT","param_InfluenzaVaccination"//DPT及小兒麻痺混合疫苗、流感疫苗
                                     ,"param_Stre_PneumococcalVaccine1","param_Stre_PneumococcalVaccine2","param_Stre_PneumococcalVaccine3","param_Stre_PneumococcalVaccine4"//肺炎鏈球菌疫苗
                                     ,"param_Hepatitis_A1","param_SHepatitis_A2","param_Rotavirus1","param_Rotavirus2","VaccineName"//A型肝炎疫苗、自費項目輪狀病毒疫苗、其他
                                     ,"param_Diet","param_Diet_other","param_Diet_type","param_Diet_type_other","param_Sleep_Habits"//飲食方式種類、睡眠習慣
                                     ,"param_Voiding_patterns","param_Excretion_patterns","param_Defecation_D","param_Defecation_F"//解尿型態、排泄型態、排便次數
                                     ,"param_Color_defecation","param_Color_defecation_other","param_Bowel_defecation","param_Bowel_defecation_other"//排便顏色、排便性狀
                                     ,"param_Daily_activities","param_Daily_activities_other"//日常生活
                                     ,"param_Behavioral_Development","param_Behavioral_Development_YN","param_Behavioral_Development_txt"//行為發展
            };
                set_viewbag(natype, tableid, mode, item_id);
            }

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
                    Value = costlist[i].CCCDescription,
                    Selected = false
                });
            }
            ViewData["costlist"] = cCostList;
            ViewBag.dt_w_type = SetDropDownList.GetddlItem("", ass_m.sel_type_list("wound_type", "assess"), false, 0, 1);//種類
            ViewBag.dt_w_g = SetDropDownList.GetddlItem("", ass_m.sel_type_list("wound_general", "assess"), false, 0, 1);//一般
            ViewBag.dt_w_s = SetDropDownList.GetddlItem("", ass_m.sel_type_list("wound_scald", "assess"), false, 0, 1);//燒傷
            ViewBag.dt_w_p = SetDropDownList.GetddlItem("", ass_m.sel_type_list("wound_pressure", "assess"), false, 0, 1);//壓傷
            ViewBag.dt_tube = SetDropDownList.GetddlItem("", ass_m.sel_tube_list(), false, 0, 1);//管路
            //----- 2016/06/03 Vanda Add 
            //ViewData["tubeMaterial"] = this.cd.getSelectItem("tube", "tubeMaterial");//管路材質
            //ViewData["tubePosition"] = this.cd.getSelectItem("tube", "tubePosition");//位置
            //ViewData["tubeSection"] = this.cd.getSelectItem("assess", "tube_section");//部位
            ViewData["mode"] = mode;
            ViewData["division"] = this.GetNanda();
            //-----
            ViewData["ptinfo"] = ptinfo;
            return View();
        }
        // 兒童入院評估_Nanda下拉選單
        private List<SelectListItem> GetNanda()
        {
            List<SelectListItem> typeList = new List<SelectListItem>();
            string sql = "SELECT * FROM NIS_SYS_DIAGNOSIS_DOMAIN";
            DataTable Dt = ass_m.DBExecSQL(sql);
            if (Dt.Rows.Count > 0)
            {
                for (int i = 0; i < Dt.Rows.Count; i++)
                {
                    typeList.Add(new SelectListItem { Text = Dt.Rows[i]["DIAGNOSIS_DOMAIN_DESC"].ToString(), Value = Dt.Rows[i]["DIAGNOSIS_DOMAIN_CODE"].ToString() });
                }
            }
            return typeList;
        }
        // 兒童入院評估_護理計劃項目列表
        public ActionResult AssessmentChild_PartialItem(string mode, string column, string key)
        {
            NurseCarePlanController ncp = new NurseCarePlanController();
            ViewData["division"] = this.GetNanda();
            if (column != null && key != null)
            {
                if (key != "")
                {
                    if (column == "CATEGORY_ID") //依Nanda
                        ViewBag.dt = ncp.SearchCarePlanTopic("SUBJECT", "", key, ptinfo.FeeNo);
                    else //依關鍵字
                        ViewBag.dt = ncp.SearchCarePlanTopic("NAME", key, "", ptinfo.FeeNo);
                }
            }
            return View();
        }
        //成人評估
        public ActionResult AssessmentAdult(string tableid, string mode)
        {
            Function func_m = new Function();
            string natype = "A";//**
            if (tableid != null)
                set_viewbag(natype, tableid, mode);
            else
            {
                #region string[] item_id = {}                
                string[] item_id = { "param_education", "param_job","param_job_other", "param_economy", "param_payment", "param_religion", "param_religion_other",
                                       "param_lang", "param_lang_other", "param_needtrans", "param_marrage", "param_care", "param_care_other",
                                       "param_psychological", "param_psychological_other", "param_child", "param_child_f", "param_child_m",
                                       "param_living", "param_living_other", "param_Volunteer_Help", "param_Volunteer_Help_Dtl",
                                       "param_cigarette", "param_cigarette_yes_amount", "param_cigarette_yes_year", "param_cigarette_agree_stop",
                                       "param_cigarette_stop_year", "param_drink", "param_drink_day", "param_drink_unit", "param_im_history",
                                       "param_im_history_item1", "param_im_history_item2", "param_im_history_item3", "param_im_history_item4",
                                       "param_im_history_item_other" ,"param_im_history_item_other_txt" ,"param_im_history_status" ,"param_su_history" ,
                                       "param_su_history_trauma_txt" ,"param_su_history_surgery_txt" ,"param_su_history_other_txt" ,"param_other_history" ,
                                       "param_other_history_desc" ,"param_med" ,"param_med_name" ,"param_med_frequency" ,"param_med_amount" ,
                                       "param_med_way" ,"param_allergy_med" ,"param_allergy_med_other" ,"param_allergy_med_other_2_name" ,
                                       "param_allergy_med_other_4_name","param_allergy_med_other_6_name","param_allergy_med_other_7_name",
                                       "param_allergy_med_other_8_name","param_allergy_med_other_9_name","param_allergy_med_other_10_name",
                                       "param_allergy_food","param_allergy_food_other","param_allergy_food_other_2_name","param_allergy_food_other_4_name",
                                       "param_allergy_other","param_allergy_other_other","param_allergy_other_other_1_name","param_allergy_other_other_2_name",
                                       "param_allergy_other_other_3_name","param_allergy_other_other_4_name","param_allergy_other_other_5_name",
                                       "param_allergy_other_other_6_name","param_bp","param_kind","param_asthma","param_epilepsy","param_HeartDisease",
                                       "param_PepticUlcer","param_tuberculosis","param_MentalIllness_txt","param_Diabetes","param_Cancer","param_LiverDisease",
                                       "param_OtherDiseaseDesc","param_OtherDisease","param_Gynecology","param_MC","param_MCStart","param_Last_MC",
                                       "param_MCCycle_rule","param_MCCycle_rule_day","param_MCDay","param_MCAmount","param_FBAbnormalDtl",
                                       "param_FBAbnormalOther","param_MCEnd","param_SelfCheck_Breast","param_SelfCheck_Vagina","param_SelfCheck_Vagina_Date",
                                       "param_BornHistory","param_BornHistoryNL","param_BornHistoryND","param_BornHistoryHL","param_BornHistoryHD","param_AbortionHistory",
                                       "param_AbortionN","param_AbortionH","param_Contraception","param_ContraceptionDesc","param_ContraceptionDesc_other",
                                       "param_EMGContact","param_ContactRole","param_ContactRole_other","param_EMGContact_1","param_EMGContact_2","param_EMGContact_3",
                                       "param_hasipd","param_ipd_past_reason","param_ipd_past_location","param_surgery","param_ipd_surgery_reason",
                                        "param_ipd_surgery_location","param_blood","param_blood_reaction","transfusion_blood_dtl_txt","param_language",
                                        "param_lang_no","param_lang_other_no","param_food_drink","param_cigarette_tutor","param_areca","param_areca_stop_year",
                                        "param_areca_tutor","param_Sleeping_text","param_Sleeping","param_Sleeping_abnormal","param_eating_self","param_garb",
                                        "param_bathe","param_toilet","param_bed_activities","param_respiratory_therapy_other","param_respiratory_therapy_other_2_rb",
                                        "param_respiratory_therapy_TxtO2Concentration","param_respiratory_therapy_TxtFaceMask3_1","param_respiratory_therapy_RbnFaceMask_kind",
                                        "param_respiratory_therapy_RbnNotInvasion_Other","param_respiratory_therapy_TxtFaceMask5_1","param_respiratory_therapy_TxtFaceMask5_2",
                                        "param_respiratory_therapy_TxtFaceMask5_3","param_respiratory_therapy_TxtFaceMask5_4","param_respiratory_therapy_TxtFaceMask5_5"
                                   };
                #endregion

                set_viewbag(natype, tableid, mode, item_id);
            }
            // DataTable DT_type = ass_m.sel_type_list("wound_type", "assess");
            byte[] listByteCode = webService.GetCostCenterList();
            string listJsonArray = CompressTool.DecompressString(listByteCode);
            List<CostCenterList> costlist = JsonConvert.DeserializeObject<List<CostCenterList>>(listJsonArray);
            List<SelectListItem> cCostList = new List<SelectListItem>();
            DataTable dtClock = ass_m.sel_assessment_clock(tableid);
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
                    Value = costlist[i].CCCDescription,
                    Selected = false
                });
            }
            ViewData["costlist"] = cCostList;
            ViewBag.dt_w_type = SetDropDownList.GetddlItem("", ass_m.sel_type_list("wound_type", "assess"), false, 0, 1);//種類
            ViewBag.dt_w_g = SetDropDownList.GetddlItem("", ass_m.sel_type_list("wound_general", "assess"), false, 0, 1);//一般
            ViewBag.dt_w_s = SetDropDownList.GetddlItem("", ass_m.sel_type_list("wound_scald", "assess"), false, 0, 1);//燒傷
            ViewBag.dt_w_p = SetDropDownList.GetddlItem("", ass_m.sel_type_list("wound_pressure", "assess"), false, 0, 1);//壓傷
            ViewBag.dt_tube = SetDropDownList.GetddlItem("", ass_m.sel_tube_list(), false, 0, 1);//管路
            ViewBag.duty_code = ptinfo.duty_code.ToString();
            ViewBag.clock = dtClock;
            ViewBag.FRIDs = (func_m.sel_FRIDs(ptinfo.FeeNo.Trim()) == true) ? "藥" : "";
            List<SysParamItem> PositionList = this.ap_eck.SelectSysParams("PainPosition", "Adult");//this.ap_eck.SelectSysParams("PainPosition", ((base.ptinfo.Age < 18) ? "Child" : "Adult"));
            string TempPositionStr = "";
            for (int j = 0; j < PositionList.Count; j++)
            {
                TempPositionStr += PositionList[j].p_name + "|";
            }
            ViewData["PositionList"] = TempPositionStr;
            //----- 2016/06/03 Vanda Add 
            //ViewData["tubeMaterial"] = this.cd.getSelectItem("tube", "tubeMaterial");//管路材質
            //ViewData["tubePosition"] = this.cd.getSelectItem("tube", "tubePosition");//位置
            //ViewData["tubeSection"] = this.cd.getSelectItem("assess", "tube_section");//部位
            ViewData["mode"] = mode;
            //-----
            return View();
        }
        //成人評估_以管路名稱帶入預設部位及管徑   
        [HttpPost]
        public string GetTubeChange(string pTube)
        {
            DataTable Dt = ass_m.GetCurrentTubeInfo(pTube);
            return JsonConvert.SerializeObject(Dt, Newtonsoft.Json.Formatting.Indented);
        }

        //精神科評估
        public ActionResult AssessmentSpirit(string tableid, string mode)
        {
            string natype = "S";//**
            if (tableid != null)
                set_viewbag(natype, tableid, mode);
            else
            {
                string[] item_id = { "param_education", "param_job", "param_job_other", "param_economy", "param_payment", "param_religion", "param_religion_other", "param_lang",
                                       "param_lang_other", "param_needtrans", "param_marrage", "param_care", "param_care_other", "param_psychological", "param_psychological_other",
                                       "param_child", "param_child_f", "param_child_m", "param_living", "param_living_other", "param_Volunteer_Help", "param_Volunteer_Help_Dtl", "param_cigarette",
                                       "param_cigarette_yes_amount", "param_cigarette_yes_year", "param_cigarette_agree_stop", "param_cigarette_stop_year", "param_drink", "param_drink_day",
                                       "param_drink_unit", "param_Exterior", "param_Exterior_other", "param_Behavor", "param_Behavor_other", "param_SportAMT", "param_SportAMT_other",
                                       "param_SLang", "param_SLang_Dtl", "param_SLang_Dtl_Other", "param_Emotion", "param_Emotion_other", "param_Consciousness", "param_Disorientation",
                                       "param_Think", "param_Think_other", "param_Hallucination", "param_Hallucination_option", "param_Event", "param_Event_option", "param_Event_option_other",
                                       "param_FeedSick", "param_First", "param_Process", "param_Process_Other", "param_hasipd", "param_ipd_count", "param_ipd_lasttime", "param_ipd_diag", "param_im_history",
                                       "param_im_history_item1", "param_im_history_item2", "param_im_history_item3", "param_im_history_item4", "param_im_history_item_other", "param_im_history_item_other_txt",
                                       "param_im_history_status", "param_su_history", "param_su_history_trauma_txt", "param_su_history_surgery_txt", "param_su_history_other_txt", "param_other_history",
                                       "param_other_history_desc", "param_med", "param_med_name", "param_med_frequency", "param_med_amount", "param_med_way", "param_bp", "param_kind",
                                       "param_asthma", "param_epilepsy", "param_HeartDisease", "param_PepticUlcer", "param_tuberculosis", "param_MentalIllness_txt", "param_Diabetes", "param_Cancer",
                                       "param_LiverDisease", "param_OtherDiseaseDesc", "param_OtherDisease", "param_Gynecology", "param_MC", "param_MCStart", "param_Last_MC", "param_MCCycle_rule",
                                       "param_MCCycle_rule_day", "param_MCDay", "param_MCAmount", "param_FBAbnormalDtl", "param_FBAbnormalOther", "param_MCEnd", "param_SelfCheck_Breast", "param_SelfCheck_Vagina",
                                       "param_SelfCheck_Vagina_Date", "param_BornHistory", "param_BornHistoryNL", "param_BornHistoryND", "param_BornHistoryHL", "param_BornHistoryHD", "param_AbortionHistory",
                                       "param_AbortionN", "param_AbortionH", "param_Contraception", "param_ContraceptionDesc", "param_ContraceptionDesc_other", "param_EMGContact", "param_ContactRole",
                                       "param_ContactRole_other", "param_EMGContact_1", "param_EMGContact_2", "param_EMGContact_3" ,
                                   "param_primary_father_name","param_primary_father_age","param_primary_father_jop","param_father_education",
                                    "param_primary_mother_name","param_primary_mother_age","param_primary_mother_jop","param_mother_education",
                                    "param_source","param_im_history","param_im_history_item_other","param_im_history_item_txt","param_im_history_item_other",
                                    "param_im_history_item_other_txt","param_hasipd","param_ipd_past_count","param_ipd_past_reason","param_ipd_past_location",
                                    "param_surgery","param_ipd_surgery_count","param_ipd_surgery_reason","param_ipd_surgery_location","param_blood",
                                    "param_blood_reaction","transfusion_blood_dtl_txt","param_take_medicine","param_take_medicine_dtl_txt",
                                    "param_Congenital_disease","Congenital_disease_dtl","Congenital_disease_dtl_other","param_HepatitisB","param_HepatitisB1",
                                    "param_HepatitisB2","param_HepatitisB3","param_5in1Vaccine1","param_5in1Vaccine2","param_5in1Vaccine3",
                                    "param_5in1Vaccine4","param_MMR1","param_MMR2","param_VaricellaVaccine","param_JP_encephalitis1","param_JP_encephalitis2",
                                    "param_JP_encephalitis3","param_JP_encephalitis4","param_DPT","param_InfluenzaVaccination",
                                    "param_Stre_PneumococcalVaccine1","param_Stre_PneumococcalVaccine2","param_Stre_PneumococcalVaccine3",
                                    "param_Stre_PneumococcalVaccine4","param_Hepatitis_A1","param_Hepatitis_A2","param_Rotavirus1","param_Rotavirus2",
                                    "VaccineName","param_Diet","param_Diet_type","param_Diet_type_other","param_Sleep_Habits","param_Voiding_patterns",
                                    "param_Excretion_patterns","param_Defecation_D","param_Defecation_F","param_Color_defecation",
                                    "param_Color_defecation_other","param_Bowel_defecation","param_Bowel_defecation_other","param_Daily_activities",
                                    "param_Daily_activities_other","param_Behavioral_Development","param_Behavioral_Development_YN",
                                    "param_Behavioral_Development_txt"
                                   };
                set_viewbag(natype, tableid, mode, item_id);
            }
            return View();
        }

        //精神科_出院評估
        public ActionResult AssessmentSpirit_O(string tableid, string mode)
        {
            ViewBag.mode = mode;
            ViewBag.pt_indate = ptinfo.InDate;
            ViewBag.dt = ass_m.sel_assessment_contnet(tableid);

            return View();
        }

        //產科評估
        public ActionResult AssessmentObstetrics(string tableid, string mode)
        {
            string natype = "O";//**
            if (tableid != null)
                set_viewbag(natype, tableid, mode);
            else
            {
                string[] item_id = { "param_parent_toget", "param_feed_style", "param_education", "param_job", "param_job_other", "param_economy", "param_payment", "param_religion", "param_religion_other",
                                       "param_lang", "param_lang_other", "param_needtrans", "param_marrage", "param_care", "param_care_other", "param_psychological", "param_psychological_other", "param_child", "param_child_f",
                                       "param_child_m", "param_living", "param_living_other", "param_Volunteer_Help", "param_Volunteer_Help_Dtl", "param_cigarette", "param_cigarette_yes_amount", "param_cigarette_yes_year",
                                       "param_cigarette_agree_stop", "param_cigarette_stop_year", "param_drink", "param_drink_day", "param_drink_unit", "param_hasipd", "param_ipd_count", "param_ipd_lasttime", "param_ipd_diag",
                                       "param_im_history", "param_im_history_item1", "param_im_history_item2", "param_im_history_item3", "param_im_history_item4", "param_im_history_item_other", "param_im_history_item_other_txt",
                                       "param_im_history_status", "param_su_history", "param_su_history_trauma_txt", "param_su_history_surgery_txt", "param_su_history_other_txt", "param_pregnancy_history", "param_pregnancy_history_dtl",
                                       "param_pregnancy_history_dtl_other", "param_pregnancy_handle", "param_pregnancy_handle_other", "param_pregnancy_med", "param_other_history", "param_other_history_desc", "param_med",
                                       "param_med_name", "param_med_frequency", "param_med_amount", "param_med_way", "param_allergy_med","param_allergy_med_other" ,"param_allergy_med_other_2_name" , "param_allergy_med_other_4_name",
                                       "param_allergy_med_other_6_name","param_allergy_med_other_7_name", "param_allergy_med_other_8_name","param_allergy_med_other_9_name","param_allergy_med_other_10_name",
                                       "param_allergy_food","param_allergy_food_other","param_allergy_food_other_2_name","param_allergy_food_other_4_name", "param_allergy_other","param_allergy_other_other","param_allergy_other_other_1_name",
                                       "param_allergy_other_other_2_name", "param_allergy_other_other_3_name","param_allergy_other_other_4_name","param_allergy_other_other_5_name", "param_allergy_other_other_6_name","param_bp",
                                       "param_kind","param_asthma","param_epilepsy","param_HeartDisease", "param_PepticUlcer","param_tuberculosis","param_MentalIllness_txt","param_Diabetes","param_Cancer","param_LiverDisease",
                                       "param_OtherDiseaseDesc","param_OtherDisease", "param_MCStart", "param_MCCycle_rule", "param_MCCycle_rule_day", "param_MCDay", "param_MCAmount", "param_FBAbnormalDtl", "param_FBAbnormalOther",
                                       "param_SelfCheck_Breast", "param_SelfCheck_Vagina", "param_SelfCheck_Vagina_Date", "param_BornHistory", "param_BornHistory_G", "param_BornHistory_P", "param_BornHistoryNL", "param_BornHistoryND",
                                       "param_BornHistoryHL", "param_BornHistoryHD", "param_Ectopic", "param_AbortionHistory", "param_AbortionN", "param_AbortionH", "param_Contraception", "param_ContraceptionDesc", "param_ContraceptionDesc_other",
                                       "param_EMGContact", "param_ContactRole", "param_ContactRole_other", "param_EMGContact_1", "param_EMGContact_2", "param_EMGContact_3" };
                set_viewbag(natype, tableid, mode, item_id);
            }
            return View();
        }

        //新生兒評估
        public ActionResult AssessmentNewBorn(string tableid, string mode, string mother_feeno, string num)
        {
            string natype = "NB";//**
            if (mother_feeno == null)
                set_viewbag(natype, tableid, mode);
            else
            {
                ViewBag.pt_indate = ptinfo.InDate;
                ViewBag.pt_icd9 = ptinfo.ICD9_code1;
                DataTable dt = this.sel_assess_data(mother_feeno, "");
                PatientInfo pinfo = new PatientInfo();
                byte[] ByteCode = webService.GetPatientInfo(mother_feeno);
                string JosnArr = "";
                //病人資訊
                if (ByteCode != null)
                {
                    #region 帶入母親_嬰兒資料
                    JosnArr = CompressTool.DecompressString(ByteCode);
                    pinfo = JsonConvert.DeserializeObject<PatientInfo>(JosnArr);
                    List<DrugOrder> Drug_list = new List<DrugOrder>();
                    byte[] labfoByteCode = webService.GetOpdMed(mother_feeno);
                    if (labfoByteCode != null)
                    {
                        string labJosnArr = CompressTool.DecompressString(labfoByteCode);
                        Drug_list = JsonConvert.DeserializeObject<List<DrugOrder>>(labJosnArr);
                    }
                    ViewData["Drug_list"] = Drug_list;

                    DataRow dt_r = dt.NewRow();
                    dt_r["SERIAL"] = "SERIAL";
                    dt_r["TABLEID"] = "TABLEID";
                    dt_r["ITEMID"] = "param_mother_name";
                    dt_r["ITEMVALUE"] = pinfo.PatientName;
                    dt_r["ITEMTYPE"] = "text";
                    dt.Rows.Add(dt_r);
                    DataRow dt_r2 = dt.NewRow();
                    dt_r2["SERIAL"] = "SERIAL";
                    dt_r2["TABLEID"] = "TABLEID";
                    dt_r2["ITEMID"] = "param_mother_feeno";
                    dt_r2["ITEMVALUE"] = pinfo.ChartNo;
                    dt_r2["ITEMTYPE"] = "text";
                    dt.Rows.Add(dt_r2);
                    DataRow dt_r3 = dt.NewRow();
                    dt_r3["SERIAL"] = "SERIAL";
                    dt_r3["TABLEID"] = "TABLEID";
                    dt_r3["ITEMID"] = "param_mother_age";
                    dt_r3["ITEMVALUE"] = pinfo.Age;
                    dt_r3["ITEMTYPE"] = "text";
                    dt.Rows.Add(dt_r3);
                    string rupture_date = obs_m.sel_Instantly_Time(mother_feeno, "RUPTURETIME");
                    if (rupture_date != "")
                    {
                        DataRow dt_r4 = dt.NewRow();
                        dt_r4["SERIAL"] = "SERIAL";
                        dt_r4["TABLEID"] = "TABLEID";
                        dt_r4["ITEMID"] = "Rupture_day";
                        dt_r4["ITEMVALUE"] = Convert.ToDateTime(rupture_date).ToString("yyyy/MM/dd");
                        dt_r4["ITEMTYPE"] = "text";
                        dt.Rows.Add(dt_r4);
                        DataRow dt_r5 = dt.NewRow();
                        dt_r5["SERIAL"] = "SERIAL";
                        dt_r5["TABLEID"] = "TABLEID";
                        dt_r5["ITEMID"] = "Rupture_time";
                        dt_r5["ITEMVALUE"] = Convert.ToDateTime(rupture_date).ToString("HH:mm");
                        dt_r5["ITEMTYPE"] = "text";
                        dt.Rows.Add(dt_r5);
                    }
                    DataTable dt_temp = obs_m.sel_Child_Brith(mother_feeno, "", "");
                    if (dt_temp.Rows.Count > 0)
                    {
                        string[] Amniotic_fluid = dt_temp.Rows[0]["RECORD"].ToString().Split('|');
                        if (Amniotic_fluid.Length > 1)
                        {
                            if (Amniotic_fluid[8] != "")
                            {
                                DataRow dt_r6 = dt.NewRow();
                                dt_r6["SERIAL"] = "SERIAL";
                                dt_r6["TABLEID"] = "TABLEID";
                                dt_r6["ITEMID"] = "rb_Amniotic_fluid_amount";
                                dt_r6["ITEMVALUE"] = Amniotic_fluid[8];
                                dt_r6["ITEMTYPE"] = "radio";
                                dt.Rows.Add(dt_r6);
                            }
                            if (Amniotic_fluid[9] != "")
                            {
                                string[] Amniotic_fluid_type = Amniotic_fluid[9].Split(',');
                                if (Amniotic_fluid_type[0] != "")
                                {
                                    DataRow dt_r7 = dt.NewRow();
                                    dt_r7["SERIAL"] = "SERIAL";
                                    dt_r7["TABLEID"] = "TABLEID";
                                    dt_r7["ITEMID"] = "rb_Amniotic_fluid_type";
                                    dt_r7["ITEMVALUE"] = Amniotic_fluid_type[0];
                                    dt_r7["ITEMTYPE"] = "radio";
                                    dt.Rows.Add(dt_r7);
                                }
                                if (Amniotic_fluid_type[0] != "")
                                {
                                    DataRow dt_r8 = dt.NewRow();
                                    dt_r8["SERIAL"] = "SERIAL";
                                    dt_r8["TABLEID"] = "TABLEID";
                                    dt_r8["ITEMID"] = "Amniotic_fluid_type_other";
                                    dt_r8["ITEMVALUE"] = Amniotic_fluid_type[1];
                                    dt_r8["ITEMTYPE"] = "text";
                                    dt.Rows.Add(dt_r8);
                                }
                            }
                            if (Amniotic_fluid[10] != "")
                            {
                                DataRow dt_r9 = dt.NewRow();
                                dt_r9["SERIAL"] = "SERIAL";
                                dt_r9["TABLEID"] = "TABLEID";
                                dt_r9["ITEMID"] = "rb_complications";
                                dt_r9["ITEMVALUE"] = Amniotic_fluid[10];
                                dt_r9["ITEMTYPE"] = "radio";
                                dt.Rows.Add(dt_r9);
                            }
                            if (Amniotic_fluid[7] != "")
                            {
                                DataRow dt_r10 = dt.NewRow();
                                dt_r10["SERIAL"] = "SERIAL";
                                dt_r10["TABLEID"] = "TABLEID";
                                dt_r10["ITEMID"] = "ck_complications";
                                dt_r10["ITEMVALUE"] = Amniotic_fluid[7];
                                dt_r10["ITEMTYPE"] = "checkbox";
                                dt.Rows.Add(dt_r10);
                            }
                            if (num != null)
                            {
                                int val = int.Parse(num) - 1;
                                DataTable dt_ = obs_m.sel_Child_Brith(mother_feeno, "", "");
                                DataRow dt_r11 = dt.NewRow();
                                dt_r11["SERIAL"] = "SERIAL";
                                dt_r11["TABLEID"] = "TABLEID";
                                dt_r11["ITEMID"] = "rb_birth_type";
                                dt_r11["ITEMVALUE"] = dt_.Rows[0]["BIRTH_TYPE"].ToString().Split('|').GetValue(val).ToString();
                                dt_r11["ITEMTYPE"] = "radio";
                                dt.Rows.Add(dt_r11);
                                DataRow dt_r12 = dt.NewRow();
                                dt_r12["SERIAL"] = "SERIAL";
                                dt_r12["TABLEID"] = "TABLEID";
                                dt_r12["ITEMID"] = "rb_birth_type_dtl";
                                dt_r12["ITEMVALUE"] = dt_.Rows[0]["BIRTH_TYPE_DTL"].ToString().Split('|').GetValue(val).ToString();
                                dt_r12["ITEMTYPE"] = "radio";
                                dt.Rows.Add(dt_r12);
                                DataRow dt_r13 = dt.NewRow();
                                dt_r13["SERIAL"] = "SERIAL";
                                dt_r13["TABLEID"] = "TABLEID";
                                dt_r13["ITEMID"] = "txt_birth_type_reason";
                                dt_r13["ITEMVALUE"] = dt_.Rows[0]["BIRTH_TYPE_REASON"].ToString().Split('|').GetValue(val).ToString();
                                dt_r13["ITEMTYPE"] = "text";
                                dt.Rows.Add(dt_r13);
                                DataRow dt_r14 = dt.NewRow();
                                dt_r14["SERIAL"] = "SERIAL";
                                dt_r14["TABLEID"] = "TABLEID";
                                dt_r14["ITEMID"] = "rb_Fetal";
                                dt_r14["ITEMVALUE"] = dt_.Rows[0]["FETAL"].ToString().Split('|').GetValue(val).ToString();
                                dt_r14["ITEMTYPE"] = "radio";
                                dt.Rows.Add(dt_r14);
                                if (dt_.Rows[0]["BIRTH_DAY"].ToString().Split('|').GetValue(val).ToString() != "")
                                {
                                    DataRow dt_r15 = dt.NewRow();
                                    dt_r15["SERIAL"] = "SERIAL";
                                    dt_r15["TABLEID"] = "TABLEID";
                                    dt_r15["ITEMID"] = "Born_day";
                                    dt_r15["ITEMVALUE"] = Convert.ToDateTime(dt_.Rows[0]["BIRTH_DAY"].ToString().Split('|').GetValue(val).ToString()).ToString("yyyy/MM/dd");
                                    dt_r15["ITEMTYPE"] = "text";
                                    dt.Rows.Add(dt_r15);
                                    DataRow dt_r16 = dt.NewRow();
                                    dt_r16["SERIAL"] = "SERIAL";
                                    dt_r16["TABLEID"] = "TABLEID";
                                    dt_r16["ITEMID"] = "Born__time";
                                    dt_r16["ITEMVALUE"] = Convert.ToDateTime(dt_.Rows[0]["BIRTH_DAY"].ToString().Split('|').GetValue(val).ToString()).ToString("HH:mm");
                                    dt_r16["ITEMTYPE"] = "text";
                                    dt.Rows.Add(dt_r16);
                                }
                                if (dt_.Rows[0]["RB_AS_1"].ToString().Split('|').GetValue(val).ToString() != "")
                                {
                                    int v = 0;
                                    if (dt_.Rows[0]["RB_AS_1"].ToString().Split('|').GetValue(val).ToString().IndexOf(",") > -1)
                                    {
                                        string[] value = dt_.Rows[0]["RB_AS_1"].ToString().Split('|').GetValue(val).ToString().Split(',');
                                        for (int i = 0; i < value.Length; i++)
                                        {
                                            if (value[i] != "")
                                                v += int.Parse(value[i]);
                                        }
                                    }
                                    else
                                        v = int.Parse(dt_.Rows[0]["RB_AS_1"].ToString().Split('|').GetValue(val).ToString());
                                    DataRow dt_r17 = dt.NewRow();
                                    dt_r17["SERIAL"] = "SERIAL";
                                    dt_r17["TABLEID"] = "TABLEID";
                                    dt_r17["ITEMID"] = "txt_apgar_score_1";
                                    dt_r17["ITEMVALUE"] = v;
                                    dt_r17["ITEMTYPE"] = "text";
                                    dt.Rows.Add(dt_r17);
                                }
                                if (dt_.Rows[0]["RB_AS_5"].ToString().Split('|').GetValue(val).ToString() != "")
                                {
                                    int v = 0;
                                    if (dt_.Rows[0]["RB_AS_5"].ToString().Split('|').GetValue(val).ToString().IndexOf(",") > -1)
                                    {
                                        string[] value = dt_.Rows[0]["RB_AS_5"].ToString().Split('|').GetValue(val).ToString().Split(',');
                                        for (int i = 0; i < value.Length; i++)
                                        {
                                            if (value[i] != "")
                                                v += int.Parse(value[i]);
                                        }
                                    }
                                    else
                                        v = int.Parse(dt_.Rows[0]["RB_AS_5"].ToString().Split('|').GetValue(val).ToString());
                                    DataRow dt_r18 = dt.NewRow();
                                    dt_r18["SERIAL"] = "SERIAL";
                                    dt_r18["TABLEID"] = "TABLEID";
                                    dt_r18["ITEMID"] = "txt_apgar_score_5";
                                    dt_r18["ITEMVALUE"] = v;
                                    dt_r18["ITEMTYPE"] = "text";
                                    dt.Rows.Add(dt_r18);
                                }
                                DataRow dt_r19 = dt.NewRow();
                                dt_r19["SERIAL"] = "SERIAL";
                                dt_r19["TABLEID"] = "TABLEID";
                                dt_r19["ITEMID"] = "rb_Exterior";
                                dt_r19["ITEMVALUE"] = dt_.Rows[0]["EXTERIOR"].ToString().Split('|').GetValue(val).ToString();
                                dt_r19["ITEMTYPE"] = "radio";
                                dt.Rows.Add(dt_r19);
                                DataRow dt_r20 = dt.NewRow();
                                dt_r20["SERIAL"] = "SERIAL";
                                dt_r20["TABLEID"] = "TABLEID";
                                dt_r20["ITEMID"] = "txt_Exterior_other";
                                dt_r20["ITEMVALUE"] = dt_.Rows[0]["EXTERIOR_OTHER"].ToString().Split('|').GetValue(val).ToString();
                                dt_r20["ITEMTYPE"] = "text";
                                dt.Rows.Add(dt_r20);
                                DataRow dt_r21 = dt.NewRow();
                                dt_r21["SERIAL"] = "SERIAL";
                                dt_r21["TABLEID"] = "TABLEID";
                                dt_r21["ITEMID"] = "rb_Meconium_Color";
                                dt_r21["ITEMVALUE"] = dt_.Rows[0]["MECONIUM_COLOR"].ToString().Split('|').GetValue(val).ToString();
                                dt_r21["ITEMTYPE"] = "radio";
                                dt.Rows.Add(dt_r21);
                                DataRow dt_r22 = dt.NewRow();
                                dt_r22["SERIAL"] = "SERIAL";
                                dt_r22["TABLEID"] = "TABLEID";
                                dt_r22["ITEMID"] = "txt_Meconium_Color_Degree_dtl";
                                dt_r22["ITEMVALUE"] = dt_.Rows[0]["MECONIUM_COLOR_DEGREE_DTL"].ToString().Split('|').GetValue(val).ToString();
                                dt_r22["ITEMTYPE"] = "text";
                                dt.Rows.Add(dt_r22);
                                DataRow dt_r23 = dt.NewRow();
                                dt_r23["SERIAL"] = "SERIAL";
                                dt_r23["TABLEID"] = "TABLEID";
                                dt_r23["ITEMID"] = "txt_weight";
                                dt_r23["ITEMVALUE"] = dt_.Rows[0]["WEIGHT"].ToString().Split('|').GetValue(val).ToString();
                                dt_r23["ITEMTYPE"] = "text";
                                dt.Rows.Add(dt_r23);
                                DataRow dt_r24 = dt.NewRow();
                                dt_r24["SERIAL"] = "SERIAL";
                                dt_r24["TABLEID"] = "TABLEID";
                                dt_r24["ITEMID"] = "txt_height";
                                dt_r24["ITEMVALUE"] = dt_.Rows[0]["HEIGHT"].ToString().Split('|').GetValue(val).ToString();
                                dt_r24["ITEMTYPE"] = "text";
                                dt.Rows.Add(dt_r24);
                                DataRow dt_r25 = dt.NewRow();
                                dt_r25["SERIAL"] = "SERIAL";
                                dt_r25["TABLEID"] = "TABLEID";
                                dt_r25["ITEMID"] = "txt_head";
                                dt_r25["ITEMVALUE"] = dt_.Rows[0]["HEAD"].ToString().Split('|').GetValue(val).ToString();
                                dt_r25["ITEMTYPE"] = "text";
                                dt.Rows.Add(dt_r25);
                                DataRow dt_r26 = dt.NewRow();
                                dt_r26["SERIAL"] = "SERIAL";
                                dt_r26["TABLEID"] = "TABLEID";
                                dt_r26["ITEMID"] = "txt_chest";
                                dt_r26["ITEMVALUE"] = dt_.Rows[0]["CHEST"].ToString().Split('|').GetValue(val).ToString();
                                dt_r26["ITEMTYPE"] = "text";
                                dt.Rows.Add(dt_r26);
                                DataRow dt_r27 = dt.NewRow();
                                dt_r27["SERIAL"] = "SERIAL";
                                dt_r27["TABLEID"] = "TABLEID";
                                dt_r27["ITEMID"] = "rb_Meconium_Color_Degree";
                                dt_r27["ITEMVALUE"] = dt_.Rows[0]["MECONIUM_COLOR_DEGREE"].ToString().Split('|').GetValue(val).ToString();
                                dt_r27["ITEMTYPE"] = "radio";
                                dt.Rows.Add(dt_r27);
                            }
                        }
                    }
                    #endregion
                }

                ViewBag.mode = mode;
                ViewBag.dt = dt;
            }

            return View();
        }

        //儲存護理評估_靜態評估
        public ActionResult SaveAssessment(List<HttpPostedFileBase> clock_file)
        {
            Hashtable Input_Type = set_hashtable(Request.Form["na_all_id"].ToString().Trim().Split(','), Request.Form["na_all_type"].ToString().Trim().Split(','));
            List<DBItem> insertDataList = new List<DBItem>();
            DateTime admittedTime = DateTime.Now;
            string na_type = Request.Form["na_type"].ToString().Trim();
            string mode = Request.Form["maode"].ToString().Trim();
            string userno = userinfo.EmployeesNo, feeno = ptinfo.FeeNo;
            string TableID = base.creatid("ASSESSMENTMASTER", userno, feeno, na_type);
            int erow = 0;
            int hasDelirium = 0;
            byte[] arr = new byte[0];


            //20230814 ken 調整時鐘圖
            if(na_type == "A")
            {
                if (clock_file[0] != null)
                {
                    arr = ConvertFileToByte(clock_file[0]);
                }
            }
   
            string DBAMID = "", DBADID = "";
            //-----2016/06/03 Vanda Add
            //string HfTubeName = Request["HfTubeName"].Replace("請選擇", "");
            //string HfTubePose = Request["HfTubePose"].Replace("請選擇", "");
            //if (HfTubeName.EndsWith(",")) HfTubeName = HfTubeName.Substring(0, HfTubeName.Length - 1);
            //if (HfTubePose.EndsWith(",")) HfTubePose = HfTubePose.Substring(0, HfTubePose.Length - 1);
            //-----
            try
            {
                DataTable temp = ass_m.sel_assessment_list(ptinfo.FeeNo.Trim(), na_type);
                int temp_count = 0;
                string old_Status = string.Empty;
                if (!string.IsNullOrEmpty(Request.Form["tableid"]))
                {
                    DataRow[] temp_thisDatas = temp.Select("TABLEID='" + Request.Form["tableid"].ToString() + "'");

                }
                foreach (DataRow r in temp.Rows)
                {
                    if (r["STATUS"].ToString() != "temporary" && r["STATUS"].ToString() != "delete")
                    {
                        temp_count++;
                    }
                    if (r["TABLEID"].ToString() == Request.Form["tableid"].ToString())
                    {
                        old_Status = r["STATUS"].ToString();
                    }
                }

                #region 儲存評估
                if (mode == "insert" || (mode == "temporary" && Request.Form["tableid"].ToString() == ""))
                {
                    //新增主表
                    insertDataList.Clear();
                    insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("TABLEID", TableID, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATEUSER", userno, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("MODIFYUSER", userno, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATETIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("MODIFYTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("NATYPE", na_type, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("STATUS", "temporary", DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("SIGN", "N", DBItem.DBDataType.String));
                    erow = ass_m.DBExecInsert("ASSESSMENTMASTER", insertDataList);
                    //20141114 mod by yungchen 應該要寫到nis_emrms
                    ////string sqlstr = "begin P_NIS_EMRMS('" + feeno + "','039','入院病人護理評估表','" + feeno + "039','" + DateTime.Now.ToString("yyyy/MM/dd HH:mm") + "','" + userno + "','I');end;";



                }
                else if (mode == "update" || (mode == "temporary" && Request.Form["tableid"].ToString() != ""))
                {
                    TableID = Request.Form["tableid"].ToString().Trim();
                    //更新主表
                    insertDataList.Clear();
                    insertDataList.Add(new DBItem("MODIFYUSER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("MODIFYTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("SIGN", "N", DBItem.DBDataType.String));
                    if (mode != "temporary")
                    {
                        insertDataList.Add(new DBItem("STATUS", "temporary", DBItem.DBDataType.String));
                        erow = ass_m.DBExecUpdate("ASSESSMENTMASTER", insertDataList, " TABLEID = '" + TableID + "' ");
                        //20140923 mod by yungchen 因僅抓病歷號的單號與出院病摘重覆 故加上簽章類別039
                        ////string sqlstr = "begin P_NIS_EMRMS('" + feeno +"','039','入院病人護理評估表','" + feeno + "039','" + DateTime.Now.ToString("yyyy/MM/dd HH:mm") + "','" + userno + "','I');end;";
                        ////ass_m.DBExec(sqlstr);
                    }
                    else //if(mode == "temporary") 
                    {
                        erow = ass_m.DBExecUpdate("ASSESSMENTMASTER", insertDataList, " TABLEID = '" + TableID + "' ");
                    }
                    //清空細項
                    ass_m.DBExecDelete("ASSESSMENTDETAIL", " TABLEID = '" + TableID + "' ");
                }

                if (erow > 0 && (mode == "insert" || (mode == "update" && old_Status == "temporary")) && base.switchAssessmentInto == "Y")
                {

                    #region  拋轉 每日身體評估 主檔(轉存身評) by wawa
                    string DBAMType = "";
                    if (na_type == "A") //成人
                        DBAMType = "adult";
                    else if (na_type == "ER")//20190305修改: 急診入評不帶入兒童身評
                        DBAMType = "";
                    else  //兒童
                        DBAMType = "child";
                    //新增
                    if (DBAMType != "")
                    {
                        insertDataList.Clear();
                        DBAMID = base.creatid("DAILY_BODY_ASSESSMENT_MASTER", userno, feeno, "0");
                        insertDataList.Add(new DBItem("DBAM_ID", DBAMID, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("DBAM_DTM", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("DBAM_TYPE", DBAMType, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("DBAM_CARE_RECORD", "Y", DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("DELETED", "N", DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CREANO", userno, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));

                        erow += ass_m.DBExecInsert("DAILY_BODY_ASSESSMENT_MASTER", insertDataList);
                    }
                    #endregion  拋轉 每日身體評估 主檔(轉存身評) end
                }

                #region  建立 入院護理評估 與 每日身體評估 的欄位對應資料表 by wawa
                DataTable DT_TBTmp = new DataTable();
                if (erow > 0)
                {
                    DT_TBTmp.Columns.Add("ITEMNUM");
                    DT_TBTmp.Columns.Add("ITEMID");
                    DT_TBTmp.Columns.Add("ITEMVALUE");
                    string TmpTableVal = "";

                    if (na_type == "A")
                    {
                        TmpTableVal = "gc_r1,RbnAwarenessComa_E|gc_r2,RbnAwarenessComa_V|gc_r3,RbnAwarenessComa_M|param_pupil_reflection_r,RbnPupilItemRef_R|pupil_size_r,RbnPupilItemSize_R|";//edit by jarvis 20161108
                        TmpTableVal += "param_pupil_reflection_l,RbnPupilItemRef_L|pupil_size_l,RbnPupilItemSize_L|param_pupil_reflection_r5_other_txt,Txt_RbnPupilItemRef_R_Other|param_pupil_reflection_l5_other_txt,Txt_RbnPupilItemRef_L_Other|";
                        TmpTableVal += "gc_r8,RbnAwarenessMuscleRU|param_gc_r8_other_txt,Txt_AwarenessMuscleRU_Other|gc_r9,RbnAwarenessMuscleLU|";
                        TmpTableVal += "param_gc_r9_other_txt,Txt_AwarenessMuscleLU_Other|gc_r10,RbnAwarenessMuscleRD|param_gc_r10_other_txt,Txt_AwarenessMuscleRD_Other|gc_r11,RbnAwarenessMuscleLD|param_gc_r11_other_txt,Txt_AwarenessMuscleLD_Other|";
                        TmpTableVal += "param_vision,RbnVision|param_vision_deviant,ChkVisionAbnormal|param_vision_deviant_1,ChkBlindness|param_vision_deviant_2,ChkHemianopia|param_vision_deviant_3,ChkDiplopia|param_auxiliary,RbnAuxiliary|";
                        TmpTableVal += "param_auxiliary_other,ChkAuxiliaryAbnormal|param_auxiliary_other_2_rb,ChkArtificialEyes|param_hearing,RbnHearing|param_hearing_other,ChkHearingAbnormal|param_auxiliary_other_1_ck,ChkHardHearing|";
                        TmpTableVal += "param_auxiliary_other_2_ck,ChkDeaf|param_auxiliary_other_3_ck,ChkTinnitus|param_audiphones,RbnIsHearingAid|param_audiphones_deviant,ChkHearingAid|param_nose,RbnNose|param_taste,RbnTaste|";
                        TmpTableVal += "param_Heart_Rhythm,RbnPulse|param_Peripheral_circulation,RbnTip|param_Peripheral_circulation_other,ChkTipAbnormal|param_Peripheral_circulation_other_txt,TxtTipAbnormal99Other|";
                        TmpTableVal += "param_Breathing_Type,RbnBreathing|param_Breathing_Type_Abnormal,ChkBreathingAbnormal|param_LeftFoot_Artery_Strength,RbnDorsalPulseL|param_RightFoot_Artery_Strength,RbnDorsalPulseR|";
                        TmpTableVal += "param_Breathing_Type_other_txt,TxtBreathingAbnormal99Other|param_Breathing_RightVoive,RbnBrthSndsR|param_Breathing_RightVoive_Abnormal,ChkBrthSndsRAbnor|";
                        TmpTableVal += "param_Breathing_RightVoive_Abnormal_other,TxtBrthSndsRAbnorOther|param_Breathing_LeftVoive,RbnBrthSndsL|param_Breathing_LeftVoive_Abnormal,ChkBrthSndsLAbnor|";
                        TmpTableVal += "param_Breathing_LeftVoive_Abnormal_other,TxtBrthSndsLAbnorOther|param_Sputum_Amount,RbnSputum|param_Sputum_Amount_Option,RbnSputumCount|param_Sputum_Amount_Color,RbnSputumColor|";
                        TmpTableVal += "param_Sputum_Amount_Color_other,TxtSputumColor99Other|param_Sputum_Amount_Type,RbnSputumNature|param_Sputum_Amount_Type_other,TxtSputumNature99Other|";//param_Eating,RbnEatingPatterns|";
                                                                                                                                                                                               //TmpTableVal += "param_SwallowingStatus_other_txt0,TxtEatingPatterns99Other|param_SwallowingStatus_other_txt1,TxtEatPat2_1|param_SwallowingStatus_other_txt2,TxtEatPat2_2|param_Eating_NG_Feeding,RbnEatPat2Item|";
                        TmpTableVal += "param_Abdominal_palpation,RbnPalpation|param_Abdominal_palpation_Tube,ChkPalpationAbn|param_Abdominal_palpation_Desc_other_txt,TxtPalpationAbnElse99Other|param_PeristalsisStatus_txt,TxtPeristalsis|";
                        TmpTableVal += "param_PeristalsisStatus,ChkPeristalsisNoAssess|param_PeristalsisStatus_voice,RbnBowelSounds|param_Decompression,RbnDecompression|param_Decompression_Option,RbnDecCount|param_Decompression_Color,RbnDecColor|";
                        TmpTableVal += "param_Decompression_Color_other,TxtDecColor99Other|param_Decompression_Type,RbnDecNature|param_Decompression_Type_other,TxtDecNature99Other|param_excrete,RbnUrination|param_excrete_Option,ChkUrinationAbn|";
                        TmpTableVal += "span_param_excrete_Option_txt,TxtUrinationAbn99Other|param_urination,RbnVoidingPattern|span_param_urination_Option_txt,TxtVoidingPattern99Other|param_urine_Option,TxtUrineCharactersAmount|";
                        TmpTableVal += "param_urine_Color,RbnUrineColor|param_urine_Color_other,Txt_UrineColor_Other|param_urine_Type,RbnUrineNature|param_urine_Type_other,Txt_UrineNature_Other|param_defecation,RbnDefecation|";
                        TmpTableVal += "param_defecation_Option,ChkDefecationAbn|param_Peripheral_circulation_other_1_rb,RbnDefecationAbn1|param_defecation_Option_txt,TxtDefecationAbn99Other|param_skeleton,RbnSkeletalSystem|";
                        TmpTableVal += "param_skeleton_Desc,ChkSkeletalSystemAbn|param_skeleton_Desc_other,TxtSkeletalSystemAbn99Other|param_skeleton_Desc_fracture,ChkSkeletalSys2Position|param_skeleton_Desc_dislocation,ChkSkeletalSys3Position|";
                        TmpTableVal += "param_skeleton_Desc_arthrosis,ChkSkeletalSys4Position|param_skeleton_Desc_amputation,ChkSkeletalSys5Position|param_skeleton_Desc_deformity,ChkSkeletalSys6Position|param_activity,RbnBonesEvents|";
                        TmpTableVal += "param_activity_Desc,ChkBonesEventsAbn|param_activity_other,TxtBonesEventsAbn99Other|param_Skin_Exterior,RbnSkinAppearance|param_Skin_Exterior_Desc,ChkSkinAppearanceAbn|";
                        TmpTableVal += "param_Skin_Exterior_Desc_other,TxtSkinAppearanceAbn99Other|param_Skin_Exterior_Desc_Edema,ChkSkinAppearance6Position|param_Skin_Exterior_Desc_Edema_other,Txt_SkinAppearance6Position_Other|";
                        TmpTableVal += "param_Skin_Exterior_Desc_Edema_extent,RbnSkinAppearAbn6Plus|param_Skin_Exterior_Desc_Rash,ChkSkinAppearance7Position|param_Skin_Exterior_Desc_Rash_other,Txt_SkinAppearance7Position_Other|";
                        TmpTableVal += "param_taboo_position,param_taboo_position|param_taboo_position_txt,param_taboo_position_txt|param_taboo_position_other,param_taboo_position_other|param_Light,RbnReproductiveSysSex|";
                        TmpTableVal += "param_Light_Boy_status,RbnReproductiveSysStatus|param_FBAbnormal,RbnReproductiveSysStatus|param_FBAbnormal_Dtl,ChkRepSysFemale|param_FBAbnormal_Dtl_other,TxtRepSysFemale99Other|";
                        TmpTableVal += "param_Light_Boy_abnormal,ChkRepSysMale|param_Light_Boy_abnormal_bump_L,TxtRepSysMale3Width|param_Light_Boy_abnormal_bump_W,TxtRepSysMale3Height|param_Light_Boy_abnormal_other,TxtRepSysMale99Other";
                    }
                    else
                    {
                        TmpTableVal = "param_General_appearance,RbnGeneralExterior|param_General_appearance_other,TxtRbnGeneralExterior5Other|param_consciousness,RbnAwarenessStatus|gc_r1,RbnAwarenessComa_E|gc_r2,RbnAwarenessComa_V|";//edit by jarvis 20161109
                        TmpTableVal += "gc_r3,RbnAwarenessComa_M|param_emotional_state,RbnEmotional|param_emotional_state_other,ChkEmotionalAbn|param_emotional_state_other_txt,TxtEmotionalAbn99Other|param_nervous_system,RbnNervousSys|";
                        TmpTableVal += "param_nervous_system_other,ChkNervousAbnormal|param_feel_other,RbnFeelingSys|param_feel_other_other,ChkFeelingAbn|";
                        TmpTableVal += "param_feel_other_1,ChkFeelingAbn5Position|param_feel_other_other_1_name,Txt_Feeling5Position_Other|";
                        TmpTableVal += "param_feel_other_2,ChkFeelingAbn1Position|param_feel_other_other_2_name,Txt_Feeling1Position_Other|";
                        TmpTableVal += "param_feel_other_3,ChkFeelingAbn2Position|param_feel_other_other_3_name,Txt_Feeling2Position_Other|param_feel_other_4,ChkFeelingAbn3Position|param_feel_other_other_4_name,Txt_Feeling3Position_Other|";
                        TmpTableVal += "param_feel_other_5,ChkFeelingAbn4Position|param_feel_other_other_5_name,Txt_Feeling4Position_Other|param_eye,RbnVision|param_param_eye_Desc,ChkVisionAbnormal|param_param_eye_Desc1,ChkAmblyopia|";
                        TmpTableVal += "param_param_eye_Desc2,ChkStrabismus|param_param_eye_Desc3,ChkHemianopia|param_param_eye_Desc_other,TxtVisionAbnormal99Other|param_ear,RbnHearing|param_param_ear_Desc,ChkHearingAbnormal|";
                        TmpTableVal += "param_param_ear_Desc1,ChkHardHearing|param_param_ear_Desc2,ChkTinnitus|param_param_ear_Desc_other,TxtHearingAbnormal99Other|param_nose,RbnNose|param_param_nose_Desc,ChkNoseAbnormal|";
                        TmpTableVal += "param_param_nose_Desc_other,TxtNoseAbnormal99Other|param_oral,RbnMouth|param_param_oral_Desc,ChkMouthAbnormal|param_param_oral_Desc_other,TxtMouthAbnormal99Other|param_Breathing_Type,RbnBreathing|";
                        TmpTableVal += "param_Breathing_Type_Abnormal,ChkBreathingAbnormal|param_Breathing_Type_txt,TxtBreathingAbn1Cont|param_Breathing_Type_other_txt,TxtBreathingAbnormal99Other|param_Breathing_RightVoive,RbnBrthSndsR|";
                        TmpTableVal += "param_Breathing_RightVoive_Abnormal,ChkBrthSndsRAbnor|param_Breathing_RightVoive_Abnormal_other,TxtBrthSndsRAbnorOther|param_Breathing_LeftVoive,RbnBrthSndsL|param_Breathing_LeftVoive_Abnormal,ChkBrthSndsLAbnor|";
                        TmpTableVal += "param_Breathing_LeftVoive_Abnormal_other,TxtBrthSndsLAbnorOther|param_Sputum_Amount,RbnSputum|param_Sputum_Amount_Option,RbnSputumCount|param_Sputum_Amount_Color,RbnSputumColor|";
                        TmpTableVal += "param_Sputum_Amount_Color_other,Txt_SputumColor_Other|param_Sputum_Amount_Type,RbnSputumNature|param_Sputum_Amount_Type_other,Txt_SputumNature_Other|param_heartbeat,RbnCardiovascular|";
                        TmpTableVal += "param_param_heartbeat_Desc,ChkCardiovascularAbnormal|param_param_heartbeat_Desc_other,TxtCardiovascularAbnormal99Other|param_defecation,RbnStomach|param_defecation_Option,ChkStomachAbnormal|";
                        TmpTableVal += "param_defecation_Option2,TxtStomachAbn6Position|param_Peripheral_circulation_other_t,TxtStomachAbnl9Cont|param_Peripheral_circulation_other_1_rb,RbnStoolNature|Peripheral_circulation_other_1_rb_txt,Txt_StoolNature_Other|";
                        TmpTableVal += "param_defecation_Option_txt,TxtStomachAbnormal99Other|";
                        TmpTableVal += "param_urinary,RbnUrinarySys|param_param_urinary_Desc,ChkUrinarySysAbnormal|param_param_urinary_Desc_other,TxtUrinarySysAbnormal99Other|param_Skin_appearance,RbnSkinAppearance|";
                        TmpTableVal += "param_Skin_appearance_other,ChkSkinAppearanceAbn|param_Skin_appearance_other_8_name,TxtSkinAppearanceAbn99Other|Skin_appearance_other_1_name,ChkSkinAppearance1Position|";
                        TmpTableVal += "txt_span_Skin_appearance_other_1_name_other,Txt_SkinAppearance1Position_Other|Skin_appearance_other_2_name,ChkSkinAppearance2Position|txt_span_Skin_appearance_other_2_name_other,Txt_SkinAppearance2Position_Other|";
                        TmpTableVal += "Skin_appearance_other_3_name,ChkSkinAppearance3Position|txt_span_Skin_appearance_other_3_name_other,Txt_SkinAppearance3Position_Other|Skin_appearance_other_4_name,ChkSkinAppearance4Position|";
                        TmpTableVal += "txt_span_Skin_appearance_other_4_name_other,Txt_SkinAppearance4Position_Other|Skin_appearance_other_5_name,ChkSkinAppearance5Position|txt_span_Skin_appearance_other_5_name_other,Txt_SkinAppearance5Position_Other|";
                        TmpTableVal += "Skin_appearance_other_6_name,ChkSkinAppearance6Position|txt_span_Skin_appearance_other_6_name_other,Txt_SkinAppearance6Position_Other|Skin_appearance_other_7_name,ChkSkinAppearance7Position|";
                        TmpTableVal += "txt_span_Skin_appearance_other_7_name_other,Txt_SkinAppearance7Position_Other|Skin_appearance_other_8_name_ck,ChkSkinAppearance8Position|span_Skin_appearance_other_8_name_txt,Txt_SkinAppearance8Position_Other|";
                        TmpTableVal += "param_skeleton,RbnSkeletalSystem|param_skeleton_Desc,ChkSkeletalSystemAbn|param_skeleton_Desc_other,TxtSkeletalSystemAbn99Other|param_skeleton_Desc_fracture,ChkSkeletalSys2Position|";
                        TmpTableVal += "param_skeleton_Desc_dislocation,ChkSkeletalSys3Position|param_skeleton_Desc_deformity,ChkSkeletalSys6Position|param_Light,RbnReproductiveSysSex|param_Light_Boy_status,RbnReproductiveSysStatus|";
                        //TmpTableVal += "param_FBAbnormal_Dtl,ChkRepSysFemale|param_FBAbnormal_Dtl_other,TxtRepSysFemale99Other|";//沒有這兩個欄位
                        //TmpTableVal += "param_respiratory_therapy_other,param_respiratory_therapy_other_2_rb,param_respiratory_therapy_TxtO2Concentration,param_respiratory_therapy_TxtFaceMask3_1,param_respiratory_therapy_RbnFaceMask_kind";
                        //TmpTableVal += "param_respiratory_therapy_RbnNotInvasion_Other,param_respiratory_therapy_TxtFaceMask5_1,param_respiratory_therapy_TxtFaceMask5_2,param_respiratory_therapy_TxtFaceMask5_3,param_respiratory_therapy_TxtFaceMask5_4,param_respiratory_therapy_TxtFaceMask5_5";
                        TmpTableVal += "param_Light_Boy_abnormal,ChkRepSysMale|param_Light_Boy_abnormal_other1,ChkRepSysMale1Site|param_Light_Boy_abnormal_other,TxtRepSysMale99Other";
                    }

                    //這些是要改，和要確認的 tag wawa edit
                    #region  這些是要改，和要確認的
                    //【成人】
                    //RASS鎮靜程度評估 入院沒有
                    //神經系統 文件裡只有一項，但入院分成神經系統和感覺，這裡要確認；神經系統 部份選項不同、入院沒有其他
                    //感官知覺 視力 部份選項不同；輔助物 義眼及其他不同(tag wawa edit 義眼再確認一次文件)
                    //感官知覺 聽力 的選項類型不同，這裡再確認一次文件 tag wawa edi；無法評估選項不同
                    //末梢 入院沒有水腫程度 身評沒有其他
                    //使用Dulplex 入院沒有
                    //足背動脈 項目不同；入院沒有橈動脈
                    //呼吸系統 呼吸型態 醫療輔助器的選項型態不同，再確認文件 tag wawa edit；選項細項也不同
                    //入 param_respiratory_therapy_other checkbox 每 RbnOxygen radio  這裡也做確認，或是直接改程式儲存的資料內容 tag wawa edit
                    //呼吸音的左右選項的型態不同，確認文件 tag wawa edit
                    //食慾異常下的選項不同，要確認 tag wawa edit
                    //進食方式的選項，入院和每日有出入
                    //進食方式裡的消化格，入院沒有
                    //飲食種類 的型態和選項的方式不同，無法對應
                    //消化系統 腹部評估 觸診 選項內容不同，無法對應
                    //入院 尿液性狀 量 為選項，每日為填空，待確認，會影響到下面的顏色和性狀
                    //禁治療部位選項方式不同
                    //【兒童】
                    //一般外觀 身評沒有其他
                    //感官系統 眼睛 身評的只有分泌物有左右眼，而入院則是都有
                    //呼吸系統 呼吸型態 選項型態不同，無法對應
                    //醫療輔助器 選項型態不同，無法對應
                    //呼吸音 左、右 異常選項型態不同，無法對應
                    //腸胃系統 疝氣的部位型態不同，入院為填空，身評為checkbox，無法對應
                    //皮膚外觀 其它 身評沒有部位選項，待確認
                    //生殖系統 女 入院選項和身評完全不同，無法對應，待確認
                    //入院的 E、V、M value和身評不同，待確認 tag wawa edit
                    //神經系統 感覺 值，不相同
                    #endregion  這些是要改，和要確認的 end

                    string[] TableValSplit = TmpTableVal.Split('|');
                    for (int i = 0; i <= TableValSplit.Length - 1; i++)
                    {
                        if (TableValSplit[i].ToString().Replace(",", "") != "")
                        {
                            string[] FieldVal = TableValSplit[i].ToString().Split(',');
                            DT_TBTmp.Rows.Add(i);
                            DT_TBTmp.Rows[i]["ITEMID"] = FieldVal[0].ToString();
                            DT_TBTmp.Rows[i]["ITEMVALUE"] = FieldVal[1].ToString();
                        }
                    }
                }
                #endregion  建立 入院護理評估 與 每日身體評估 的欄位對應資料表 end

                string[] ItemIDList = Request.Form.AllKeys;
                for (int i = 12; i < ItemIDList.Length; i++)
                {
                    string ItemID = ItemIDList[i].ToString();
                    string ItemValue = Request[ItemID].ToString().Trim();
                    string ItemType = "", DailyItemValue = "";
                    if (!string.IsNullOrEmpty(ItemID))
                    {
                        if (ItemID == "inHosp" || ItemID == "outHosp" || ItemID == "wound_pressure"
                        || ItemID == "Prompt_TubeKind" || ItemID == "txt_section" || ItemID == "txt_position"
                        || ItemID == "tubeMaterial" || ItemID == "ddl_wound_type" || ItemID == "ddl_wound_general"
                        || ItemID == "ddl_wound_scald")
                            ItemType = "ddl";
                        else
                            ItemType = Input_Type[ItemID].ToString();
                    }

                    if (ItemValue != "")
                    {
                        insertDataList.Clear();
                        insertDataList.Add(new DBItem("SERIAL", base.creatid("ASSESSMENTDETAIL", userno, feeno, i.ToString()), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("TABLEID", TableID, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("ITEMID", ItemID, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("ITEMTYPE", ItemType, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("ITEMVALUE", ItemValue.Replace("'", "’").Replace("\r\n", ""), DBItem.DBDataType.String));
                        erow = ass_m.DBExecInsert("ASSESSMENTDETAIL", insertDataList);

                        #region  拋轉 每日身體評估 細項檔(轉存身評) by wawa
                        if (erow > 0 && (mode == "insert" || (mode == "update" && old_Status == "temporary")) && base.switchAssessmentInto == "Y")
                        {
                            try
                            {
                                //每日身體評估
                                if (sel_data(DT_TBTmp, ItemID) != null && sel_data(DT_TBTmp, ItemID) != "")
                                {
                                    insertDataList.Clear();
                                    DBADID = base.creatid("DAILY_BODY_ASSESSMENT_DETAIL", userno, feeno, i.ToString());
                                    insertDataList.Add(new DBItem("DBAD_ID", DBADID, DBItem.DBDataType.String));
                                    insertDataList.Add(new DBItem("DBAM_ID", DBAMID, DBItem.DBDataType.String));
                                    insertDataList.Add(new DBItem("DBAD_ITEMID", sel_data(DT_TBTmp, ItemID), DBItem.DBDataType.String));
                                    if (ItemType == "textarea" || ItemType == "text")
                                    {
                                        if (ItemID == "param_taboo_position_txt" || ItemID == "param_taboo_position_other")
                                        {//為了處理 身評禁治療部位 的多欄位共存一個name，改採※＆分隔儲存 //jarvis add 201610281145
                                            DailyItemValue = ItemValue.Trim().Replace(",", "※＆");//Split by multiple characters by jarvis
                                            DailyItemValue = DailyBodyAssessment.trans_special_code_with_Daily_Body(DailyItemValue);
                                            insertDataList.Add(new DBItem("DBAD_ITEMVALUE", DailyItemValue.Replace("'", "’").Replace("\r\n", ""), DBItem.DBDataType.String));
                                        }
                                        else
                                        {
                                            ItemValue = DailyBodyAssessment.trans_special_code_with_Daily_Body(ItemValue);
                                            insertDataList.Add(new DBItem("DBAD_ITEMVALUE", ItemValue.Replace("'", "’").Replace("\r\n", ""), DBItem.DBDataType.String));
                                        }
                                    }
                                    else if (ItemType == "radio" && na_type == "C")
                                    {
                                        if (ItemID == "gc_r1" || ItemID == "gc_r2" || ItemID == "gc_r3")
                                        {
                                            ItemValue = ItemValue.Substring(1, 1);
                                            insertDataList.Add(new DBItem("DBAD_ITEMVALUE", ItemValue.Replace("'", "’").Replace("\r\n", ""), DBItem.DBDataType.String));
                                        }
                                        else
                                        {
                                            insertDataList.Add(new DBItem("DBAD_ITEMVALUE", ItemValue.Replace("'", "’").Replace("\r\n", ""), DBItem.DBDataType.String));
                                        }
                                    }
                                    else
                                    {
                                        insertDataList.Add(new DBItem("DBAD_ITEMVALUE", ItemValue.Replace("'", "’").Replace("\r\n", ""), DBItem.DBDataType.String));
                                    }
                                    insertDataList.Add(new DBItem("DBAD_ITEMTYPE", ItemType, DBItem.DBDataType.String));
                                    erow += ass_m.DBExecInsert("DAILY_BODY_ASSESSMENT_DETAIL", insertDataList);
                                }
                            }
                            catch (Exception ex)
                            {
                                this.log.saveLogMsg(ex.Message.ToString() + "身評,tableid：" + DBAMID, "DBExecInsert");
                            }

                        }
                        #endregion  拋轉 每日身體評估 細項檔(轉存身評) end
                    }
                }
                #endregion

                #region --xml -- by.jarvis-20160630
                DataTable dt = ass_m.sel_assessment_contnet(TableID);
                DataTable dtClock = ass_m.sel_assessment_clock(TableID);
                if (mode == "insert" || mode == "update")
                {
                    //string sql = "SELECT * FROM ASSESSMENTDETAIL WHERE TABLEID='" + TableID + "'";
                    string sql = "SELECT  A.*,B.CREATETIME FROM ASSESSMENTDETAIL A LEFT JOIN ASSESSMENTMASTER B ON A.TABLEID=B.TABLEID ";
                    sql += "WHERE A.TABLEID='" + TableID + "' AND B.DELETED IS NULL ORDER BY B.CREATETIME DESC";
                    DataTable dtxml = this.ass_m.DBExecSQL(sql);
                    if (dtxml != null)
                    {
                        Assessment_XML("AdmissionAssessment", na_type, TableID, dtxml);
                    }
                }
                #endregion
                #region 圖片儲存

                var cfs = sel_data(dt, "rb_cfs");
                var cog = sel_data(dt, "ck_minicog");
                int scoreCheck = 0;
                if(sel_data(dt, "uploadJPG") == "N")
                {
                    insertDataList.Clear();
                    link.DBCmd.CommandText = "UPDATE ASSESSMENTOBJ SET  DEL = 'Y' "
                                                       + " WHERE TABLEID = '" + TableID + "' ";
                    link.DBOpen();
                    link.DBCmd.ExecuteNonQuery();
                    link.DBClose();
                }
                if (cfs == "不需要" || cfs == "無法評估" || cog == "無法評估")
                {
                    insertDataList.Clear();
                    link.DBCmd.CommandText = "UPDATE ASSESSMENTOBJ SET  DEL = 'Y' "
                                                       + " WHERE TABLEID = '" + TableID + "' ";
                    link.DBOpen();
                    link.DBCmd.ExecuteNonQuery();
                    link.DBClose();
                }
                else
                {
                    int val = new int();
                    //scoreCheck = int.Parse(sel_data(dt, "param_total_score"));
                    int.TryParse(sel_data(dt, "param_total_score"), out val);
                    scoreCheck = val;

                }
                if (scoreCheck < 4  || scoreCheck > 7)
                {
                    insertDataList.Clear();
                    link.DBCmd.CommandText = "UPDATE ASSESSMENTOBJ SET  DEL = 'Y' "
                                                       + " WHERE TABLEID = '" + TableID + "' ";
                    link.DBOpen();
                    link.DBCmd.ExecuteNonQuery();
                    link.DBClose();
                }
                else
                {
                    if (na_type == "A")
                    {
                        if (clock_file[0] != null)
                        {

                            string strClock = "SELECT * FROM ASSESSMENTOBJ WHERE TABLEID = '" + TableID + "' ";
                            DataTable dtck = new DataTable();
                            this.link.DBExecSQL(strClock, ref dtck);
                            if (dtck.Rows.Count > 0)
                            {
                                insertDataList.Clear();
                                insertDataList.Add(new DBItem("DEL", "D", DBItem.DBDataType.String));
                                string where = "TABLEID='" + TableID + "'";
                                this.link.DBExecUpdate("ASSESSMENTOBJ", insertDataList, where);
                            }


                            insertDataList.Clear();
                            string id_OBJ = creatid("ASS_OBJ", userno, feeno, "0");

                            insertDataList.Add(new DBItem("SERIAL", id_OBJ, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("TABLEID", TableID, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("ITEMID", "Obj_Clock", DBItem.DBDataType.String));
                            erow = ass_m.DBExecInsert("ASSESSMENTOBJ", insertDataList);

                            link.DBCmd.CommandText = "UPDATE ASSESSMENTOBJ SET  ITEMVALUE = :CLOCK_FILE "
                                                                + " WHERE SERIAL = '" + id_OBJ + "' AND TABLEID = '" + TableID + "' ";

                            link.DBCmd.Parameters.Add(":CLOCK_FILE", OracleDbType.Blob).Value = arr;

                            link.DBOpen();
                            link.DBCmd.ExecuteNonQuery();
                            link.DBClose();

                        }
                    }
                }

                #endregion
                #region 拋轉
                string content = "";
                string ih_desc = "＿＿＿＿", ih_date = "＿＿＿＿";
                if (temp_count == 0 && mode != "temporary")
                {
                    InHistory ih = getLastDrag();
                    if (ih != null)
                    {
                        ih_desc = ih.Description.Trim();
                        ih_date = ih.indate.ToString("yyyy/MM/dd");
                    }

                    #region 拋轉傷口_新
                    int wound_cound = 0;
                    if (sel_data(dt, "param_General_wound") == "有" && sel_data(dt, "ddl_wound_type") != "" && base.switchAssessmentInto == "Y")//如果有傷口
                    {
                        string[] date = sel_data(dt, "wound_date").Split(',');
                        string[] type = sel_data(dt, "ddl_wound_type").Split(',');

                        string[] position1 = sel_data(dt, "ddl_wound_scald").Split(',');
                        string[] position2 = sel_data(dt, "ddl_wound_general").Split(',');
                        string[] position_other = sel_data(dt, "wound_other_txt").Split(',');
                        for (int j = 0; j < type.Length; j++)
                        {
                            string position = "";
                            if (type[j].ToString() == "燙傷")
                            {
                                position = position1[j];
                                if (position1[j].ToString() == "其他")
                                    position = position_other[j];
                            }
                            else
                            {
                                position = position2[j];
                                if (position2[j].ToString() == "其他")
                                    position = position_other[j];
                            }
                            Insert_Wound(date[j], type[j].ToString(), position.ToString(), wound_cound, "");
                            wound_cound++;
                        }
                    }
                    if (sel_data(dt, "param_pressure") == "有" && sel_data(dt, "wound_pressure") != "" && base.switchAssessmentInto == "Y")//如果有壓傷
                    {
                        string[] date = sel_data(dt, "wound_pre_date").Split(',');
                        //string[] place = sel_data(dt, "place").Split(',');
                        string[] location1 = sel_data(dt, "inHosp").Split(',');
                        string[] location2 = sel_data(dt, "outHosp").Split(',');
                        string[] location2o = sel_data(dt, "outHosp_other").Split(',');


                        string[] position = sel_data(dt, "wound_pressure").Split(',');
                        string[] position_other = sel_data(dt, "wound_pre_other_txt").Split(',');

                        for (int j = 0; j < position.Length; j++)
                        {
                            string location = location1[j];
                            string position_prn = position[j];
                            if (position[j].ToString() == "其他")
                                position_prn = position_other[j].ToString();

                            if (j != 0)
                            {
                                if (sel_data(dt, "place_" + j) == "2")//院外=2
                                {
                                    location = location2[j] + location2o[j];
                                }
                            }
                            else
                            {
                                if (sel_data(dt, "place") == "2")//院外=2
                                {
                                    location = location2[j] + location2o[j];
                                }
                            }

                            Insert_Wound(date[j], "壓傷", position_prn, wound_cound, location);
                            wound_cound++;
                        }
                    }
                    #endregion

                    #region 拋轉壓傷
                    if (sel_data(dt, "param_total_pressure_sores") != "" && base.switchAssessmentInto == "Y")//有壓傷分數
                    {
                        insertDataList.Clear();
                        string date_time = sel_data(dt, "param_tube_date") + " " + sel_data(dt, "param_tube_time");
                        string id = creatid("PRESSURE_SORE_ASSESS", userno, feeno, "0");

                        insertDataList.Add(new DBItem("PRESSURE_ID", id, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CREATNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("RECORDTIME", date_time, DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("NUTRITION", sel_data(dt, "param_nutrition_pressure_sores"), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("PERCEPTION", sel_data(dt, "param_feeling_pressure_sores"), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("DAMP", sel_data(dt, "param_wet_pressure_sores"), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("ACTIVITY", sel_data(dt, "param_activities_pressure_sores"), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("MOVING", sel_data(dt, "param_moving_pressure_sores"), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("FRICTION", sel_data(dt, "param_friction_pressure_sores"), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("HAVE", sel_data(dt, "param_pressure"), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("TOTAL", sel_data(dt, "param_total_pressure_sores"), DBItem.DBDataType.Number));

                        if (this.link.DBExecInsertTns("NIS_PRESSURE_SORE_DATA", insertDataList) > 0)
                        {
                            string carerecord = "";
                            int score = int.Parse(sel_data(dt, "param_total_pressure_sores"));
                            if (score <= 9)
                            {
                                carerecord = $"壓傷高危險因子評估分數{score}分，為壓傷極高度危險群";
                            }
                            else if (score < 13)
                            {
                                carerecord = $"壓傷高危險因子評估分數{score}分，為壓傷高度危險群";
                            }
                            else if (score < 15)
                            {
                                carerecord = $"壓傷高危險因子評估分數{score}分，為壓傷中度危險群";
                            }
                            else if (score < 19)
                            {
                                carerecord = $"壓傷高危險因子評估分數{score}分，為壓傷輕度危險群";
                            }
                            else
                            {
                                carerecord = $"壓傷高危險因子評估分數{score}分，非壓傷高危險群";
                            }

                            string msg = "病人皮膚感覺知覺為" + sel_data(dt, "param_feeling_pressure_sores").Substring(0, sel_data(dt, "param_feeling_pressure_sores").Length - 3) + "，"
                            + "潮溼程度為" + sel_data(dt, "param_wet_pressure_sores").Substring(0, sel_data(dt, "param_wet_pressure_sores").Length - 3) + "，"
                            + "活動力為" + sel_data(dt, "param_activities_pressure_sores").Substring(0, sel_data(dt, "param_activities_pressure_sores").Length - 3) + "，"
                            + "移動力為" + sel_data(dt, "param_moving_pressure_sores").Substring(0, sel_data(dt, "param_moving_pressure_sores").Length - 3) + "，"
                            + "營養狀態為" + sel_data(dt, "param_nutrition_pressure_sores").Substring(0, sel_data(dt, "param_nutrition_pressure_sores").Length - 3) + "，"
                            + "摩擦力/剪力為" + sel_data(dt, "param_friction_pressure_sores").Substring(0, sel_data(dt, "param_friction_pressure_sores").Length - 3) + "，"
                            + carerecord + "，"
                            + "目前" + sel_data(dt, "param_pressure_assessment").ToString() + "壓傷。";

                            Insert_CareRecordTns(date_time, id, "壓傷危險評估", "", "", msg, "", "", "PRESSURE_SORE_DATA", ref link);
                        }                      
                    }

                    #endregion

                    #region 拋轉疼痛

                    if (!string.IsNullOrWhiteSpace(sel_data(dt, "param_feel_other_2")) && base.switchAssessmentInto == "Y")
                    {
                        insertDataList.Clear();
                        string Pain_Positon = sel_data(dt, "param_feel_other_2").Replace("其他", sel_data(dt, "param_feel_other_other_2_name"));
                        insertDataList.Add(new DBItem("PAIN_CODE", base.creatid("NIS_DATA_PAIN_POSITION", userno, feeno, "0"), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("FEE_NO", feeno, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("RECORD_DATE", sel_data(dt, "param_tube_date") + " " + sel_data(dt, "param_tube_time") + ":00", DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("PAIN_POSITION", Pain_Positon, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("RECORD_JSON_DATA", "[]", DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CREATE_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CREATE_USERNO", userno, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CREATE_USERNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("UPDATE_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("UPDATE_USERNO", userno, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("UPDATE_USERNAME", userinfo.EmployeesName, DBItem.DBDataType.String));

                        ass_m.DBExecInsert("NIS_DATA_PAIN_POSITION", insertDataList);
                    }

                    #endregion

                    if (na_type == "A")//成人
                    {
                        #region 拋轉跌倒
                        if (sel_data(dt, "param_total_fall") != "" && base.switchAssessmentInto == "Y")//有跌倒分數
                        {
                            insertDataList.Clear();
                            string date = sel_data(dt, "param_tube_date") + " " + sel_data(dt, "param_tube_time");
                            string id = creatid("FALL_ASSESS_DATA", userno, feeno, "0");

                            insertDataList.Add(new DBItem("FALL_ID", id, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("CREATNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                            insertDataList.Add(new DBItem("RECORDTIME", date, DBItem.DBDataType.DataTime));
                            insertDataList.Add(new DBItem("NUM", "0", DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("AGE", sel_data(dt, "param_age_fall"), DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("ACTIVITY", sel_data(dt, "param_activity_fall"), DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("COMMUNICATION", sel_data(dt, "param_communication_fall"), DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("CONSCIOUSNESS", sel_data(dt, "param_consciousness_fall"), DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("DIZZINESS", sel_data(dt, "param_dizziness_fall"), DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("EXCRETION", sel_data(dt, "param_excretion_fall"), DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("FALL", sel_data(dt, "param_history_fall"), DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("DRUG", sel_data(dt, "param_drug_fall"), DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("TOTAL", sel_data(dt, "param_total_fall").ToString(), DBItem.DBDataType.Number));
                            insertDataList.Add(new DBItem("REASON", "入院/轉入", DBItem.DBDataType.String));

                            if (ass_m.DBExecInsert("NIS_FALL_ASSESS_DATA", insertDataList) > 0)
                            {
                                ass_m.DBExecDelete("DATA_NOTICE", " NT_ID = 'FALL_ADULT_" + base.ptinfo.FeeNo + "' AND FEE_NO = '" + base.ptinfo.FeeNo + "' ");
                                string notice_day = ass_m.sel_last_fall_assess_time_And_total(base.ptinfo.FeeNo, "NIS_FALL_ASSESS_DATA");
                                if (notice_day != "")
                                    new FallAssessController().Do_FallNotice(base.ptinfo.FeeNo, "ADULT", notice_day, "9999/12/31 00:00:00");

                                string msg = string.Empty;
                                if (int.Parse(sel_data(dt, "param_total_fall")) > 2)
                                {
                                    msg = "評估原因：" + "入院/轉入" + "，評估病人跌倒危險因子分數為" + sel_data(dt, "param_total_fall").ToString() + "，持續追蹤病人狀況。";

                                    string item_id = ass_m.sel_health_education_item(base.ptinfo.FeeNo, "FALL");
                                    if (item_id != "")
                                    {
                                        insertDataList.Clear();
                                        insertDataList.Add(new DBItem("EDU_ID", base.creatid("EDUCATION_DATA", base.userinfo.EmployeesNo, base.ptinfo.FeeNo, "0"), DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("FEENO", base.ptinfo.FeeNo, DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("CREATNO", base.userinfo.EmployeesNo, DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                                        insertDataList.Add(new DBItem("RECORDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                                        insertDataList.Add(new DBItem("ITEMID", item_id, DBItem.DBDataType.String));
                                        ass_m.DBExecInsert("HEALTH_EDUCATION_DATA", insertDataList);
                                    }
                                }
                                else
                                    msg = "評估原因：" + "入院/轉入" + "，評估病人跌倒危險因子分數為" + sel_data(dt, "param_total_fall").ToString() + "分，持續追蹤病人狀況。";

                                Insert_CareRecord(date, id, "跌倒危險性評估", "", "", msg, "", "", "FALL_ASSESS");
                            }
                        }

                        #endregion

                        #region 拋轉譫妄
                        if (sel_data(dt, "param_total_delirium") != "")
                        {
                            string delirium_date = sel_data(dt, "param_tube_date") + " " + sel_data(dt, "param_tube_time");
                            insertDataList.Clear();
                            string id = creatid("DELIRIUM_DATA", userno, feeno, "0");
                            string[] del_obj_db = { "SCORE_1A", "SCORE_1B", "SCORE_2", "SCORE_3", "SCORE_4" };
                            string[] del_obj = { "rb_acute_attack_1a", "rb_acute_attack_1b", "rb_attention", "rb_ponder", "rb_consciousness" };
                            int total_score = 0;
                            for (int i = 0; i < del_obj_db.Length; i++)
                            {
                                string score = sel_data(dt, del_obj[i]);
                                if (score == "是")
                                {
                                    score = "0";
                                    total_score = total_score + 1;
                                }
                                else if (score == "否")
                                {
                                    score = "1";
                                }
                                else
                                {
                                    score = "2";
                                }
                                insertDataList.Add(new DBItem(del_obj_db[i], score, DBItem.DBDataType.String));
                            }
                            insertDataList.Add(new DBItem("ASSESS_ID", id, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("CARERECORD_ID", id, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("FEE_NO", feeno, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("ASSESS_DT", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                            insertDataList.Add(new DBItem("CREATE_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("CREATE_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("CREATE_USER_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("MODIFY_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("MODIFY_USER_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("RESULT", sel_data(dt, "param_total_delirium"), DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("ASSESSMENT_REASON", "新病人", DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("DELIRIUM_RESULT", sel_data(dt, "param_result_delirium"), DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("SOURCE", "Assessment", DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("TOTAL_SCORE", total_score.ToString(), DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("TABLEID", TableID, DBItem.DBDataType.String));

                            var reason = "";
                            var msg = "";
                            if (sel_data(dt, "param_total_delirium") != null)
                            {
                                reason = sel_data(dt, "param_total_delirium");
                            }
                            msg += "病人評核原因：新病人，評估項目：1.急性發作且病程波動：";

                            //1a
                            msg += "1a.與平常相比較，有任何證據顯示病人精神狀態產生急性變化：";
                            if (sel_data(dt, "rb_acute_attack_1a") != null )
                            {
                                msg += sel_data(dt, "rb_acute_attack_1a");
                            }

                            msg += "，";

                            //1b
                            msg += "1b.這些不正常的行為在一天中呈現波動狀態：";
                            if (sel_data(dt, "rb_acute_attack_1b") != null)
                            {
                                msg += sel_data(dt, "rb_acute_attack_1b");
                            }
                            msg += "、";

                            //2
                            msg += "2.注意力不集中：";
                            if (sel_data(dt, "rb_attention") != null)
                            {
                                msg += sel_data(dt, "rb_attention");
                            }
                            msg += "、";

                            //3
                            msg += "3.思考缺乏組織：";
                            if (sel_data(dt, "rb_ponder") != null)
                            {
                                msg += sel_data(dt, "rb_ponder");
                            }
                            msg += "、";

                            //4
                            msg += "4.意識狀態改變：";
                            if (sel_data(dt, "rb_consciousness") != null)
                            {
                                msg += sel_data(dt, "rb_consciousness");
                            }
                            msg += "。";

                            //result
                            msg += "評估結果：";
                            if (sel_data(dt, "param_total_delirium") != null)
                            {
                                msg += sel_data(dt, "param_total_delirium");
                            }
                            else
                            {
                                msg += "";
                            }
                            if(msg != "")
                            {
                                msg += "。";
                                erow += base.Insert_CareRecord(DateTime.Now.ToString("yyyy/MM/dd HH:mm:01"), id, "譫妄評估", msg, "", "", "", "", "Delirium");
                            }

                       
                            erow = ass_m.DBExecInsert("DELIRIUM_DATA", insertDataList);
                        }
                        #endregion

                        #region 拋轉衰弱
                        if (sel_data(dt, "param_total_score") != "")
                        {
                            insertDataList.Clear();
                            string id = creatid("CFS_DATA", userno, feeno, "0");
                            string cfs_date = sel_data(dt, "param_tube_date") + " " + sel_data(dt, "param_tube_time");
                            string msg = "";

                            insertDataList.Add(new DBItem("ASSESS_ID", id, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("CARERECORD_ID", id, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("FEE_NO", feeno, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("ASSESS_DT", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                            insertDataList.Add(new DBItem("CREATE_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("CREATE_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("CREATE_USER_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("MODIFY_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("MODIFY_USER_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("SOURCE", "Assessment", DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("CFS_SCORE", sel_data(dt, "param_total_score"), DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("MC_SCORE", sel_data(dt, "param_total_cog"), DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("MC_SCORE1", sel_data(dt, "param_cog_1").Replace('|', ','), DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("MC_SCORE2", sel_data(dt, "param_cog_2"), DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("MC_SCORE3", sel_data(dt, "param_cog_3"), DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("TABLEID", TableID, DBItem.DBDataType.String));
                            if (sel_data(dt, "cog_ck_result") != null)
                            {
                                insertDataList.Add(new DBItem("COG_RESULT", sel_data(dt, "cog_ck_result"), DBItem.DBDataType.String));
                            }
                            insertDataList.Add(new DBItem("COG_RESULT_OTHER", sel_data(dt, "span_cog_Y_other"), DBItem.DBDataType.String));
        
                            //衰弱評估 拋轉護理紀錄
                            if (sel_data(dt, "param_total_score") == null)
                            {
                                if (ptinfo.Age < 65)
                                {
                                    msg += "病人年齡<65歲";

                                }
                                else
                                {
                                    msg += "病人年齡≧65歲";
                                }
                                msg +="，衰弱評估為無此需要，續追蹤。";
                            }
                            else
                            {
                                var score = sel_data(dt, "param_total_score");
                                var status = "";
                                var status_note = "";
                                switch (score)
                                {
                                    case "1":
                                        status = "非常健康";
                                        status_note = "無衰弱，鼓勵參與社區健康促進活動";
                                        break;
                                    case "2":
                                        status = "健康";
                                        status_note = "無衰弱，鼓勵參與社區健康促進活動";
                                        break;
                                    case "3":
                                        status = "維持良好";
                                        status_note = "無衰弱，鼓勵參與社區健康促進活動";
                                        break;
                                    case "4":
                                        status = "脆弱較易受傷害";
                                        status_note = "衰弱前期，通知醫療科照會各職類";
                                        break;
                                    case "5":
                                        status = "輕度衰弱";
                                        status_note = "衰弱前期，通知醫療科照會各職類";
                                        break;
                                    case "6":
                                        status = "中度衰弱	";
                                        status_note = "衰弱前期，通知醫療科照會各職類";
                                        break;
                                    case "7":
                                        status = "嚴重衰弱	";
                                        status_note = "衰弱期，通知醫療科照會出院準備";
                                        break;
                                    case "8":
                                        status = "非常嚴重衰弱	";
                                        status_note = "	衰弱期，通知醫療科照會出院準備";
                                        break;
                                    case "9":
                                        status = "末期	";
                                        status_note = "衰弱期，通知醫療科照會出院準備";
                                        break;
                                }

                                if (ptinfo.Age < 65)
                                {
                                    msg += "病人年齡<65歲";
                                }
                                else
                                {
                                    msg += "病人年齡≧65歲";
                                }
                                msg += "，衰弱評估健康狀態：" + status + " ，";
                                msg += "分數：";
                                if (sel_data(dt, "param_total_score") != null)
                                {
                                    msg += sel_data(dt, "param_total_score");
                                }
                                msg += "分，";
                                msg += "結果：" + status_note + "，續追蹤。";
                            }

                            base.Insert_CareRecord(admittedTime.ToString("yyyy/MM/dd HH:mm:02"), id, "衰弱評估", msg, "", "", "", "", "CFS");

                            //失智評估
                            msg = "";
                            if (sel_data(dt, "param_total_cog") == null || sel_data(dt, "param_total_cog") == "")
                            {
                                if (sel_data(dt, "cog_ck_result") != null && sel_data(dt, "cog_ck_result") != "")
                                {
                                    msg += "病人Mini-Cog失智評估為無法評估，原因：";
                                    var CFSresult = sel_data(dt, "cog_ck_result").Split(',');
                                    var resultOther = "";
                                    for (int i = 0; i < CFSresult.Count(); i++)
                                    {
                                        if (CFSresult[i] != "其他")
                                        {
                                            resultOther += CFSresult[i];
                                        }
                                        if (i < CFSresult.Count() - 1)
                                        {
                                            resultOther += ",";
                                        }
                                    }
                                    msg += resultOther;
                                }
                                if (sel_data(dt, "span_cog_Y_other") != null)
                                {
                                    msg += sel_data(dt, "span_cog_Y_other");
                                }
                                if(msg != "")
                                {
                                    msg += "，續追蹤。";
                                }
                            }
                            else
                            {
                                //1.病人可依引導於畫鐘後，正確覆誦"紅色、快樂、腳踏車"
                                var score = sel_data(dt, "param_total_cog");
                                var total = int.Parse(sel_data(dt, "param_total_cog"));
                                msg += "病人Mini-Cog失智評估為衰弱前期評估，";
                                msg += "評估項目：";
                                msg += "1.病人可依引導於畫鐘後，正確覆誦";
                                msg += '"';
                                msg += "紅色、快樂、腳踏車";
                                msg += '"';
                                msg += "：病人回答";

                                var score1 = 0;
                                if (sel_data(dt, "param_cog_3") != null)
                                {
                                    if (sel_data(dt, "param_cog_1") != "皆無回答")
                                    {
                                        var bicycleText = sel_data(dt, "param_cog_1");
                                        score1 = sel_data(dt, "param_cog_1").Split('|').Count();
                                        msg += sel_data(dt, "param_cog_1").Replace('|', ',');
                                    }
                                    else
                                    {
                                        score1 = 0;
                                        msg += "不正確";
                                    }
                                }
                                else
                                {
                                    msg += "不正確";
                                    score1 = 0;

                                }
                                msg += "，";

                                msg += "分數：" + score1 + "分、";


                                //2.病人可自行完成畫鐘數字及順序正確
                                msg += "2.病人可自行完成畫鐘數字及順序正確：";
                                var score2 = 0;
                                if (sel_data(dt, "param_cog_2") != null)
                                {
                                    if (sel_data(dt, "param_cog_2") == "0")
                                    {
                                        msg += "是";
                                        score2 = 1;
                                    }
                                    else
                                    {
                                        msg += "否";
                                        score2 = 0;
                                    }
                                }
                                msg += "，";
                                msg += "分數：" + score2.ToString() + "分、";

                                //3.病人可於畫鐘上畫上指針正確指向11:10
                                msg += "3.病人可於畫鐘上畫上指針正確指向11:10：";
                                var score3 = 0;

                                if (sel_data(dt, "param_cog_3") != null)
                                {
                                    if (sel_data(dt, "param_cog_3") == "0")
                                    {
                                        msg += "是";
                                        score3 = 1;
                                    }
                                    else
                                    {
                                        msg += "否";
                                        score3 = 0;
                                    }
                                }
                                msg += "，";
                                msg += "分數：" + score3.ToString() + "分，";

                                //總分
                                msg += "總分：" + total + "分";
                                if (total <= 2)
                                {
                                    msg += "，提醒醫療科進一步評估。";
                                }
                                else
                                {
                                    msg += "。";
                                }

                            }
                            if(msg != "")
                            {
                                base.Insert_CareRecord(admittedTime.ToString("yyyy/MM/dd HH:mm:03"), id, "Mini-Cog失智評估", msg, "", "", "", "", "MINICOG");
                            }

                            erow = ass_m.DBExecInsert("CFS_DATA", insertDataList);
                            if (na_type == "A")
                            {
                                if (clock_file[0] != null)
                                {
                                    link.DBCmd.CommandText = "";
                                    link.DBCmd.Parameters.Clear();
                                    link.DBCmd.CommandText = "UPDATE CFS_DATA SET  CLOCK_FILE = :CLOCK_FILE "
                                                                       + " WHERE FEE_NO = '" + feeno + "' AND ASSESS_ID = '" + id + "' ";
                                    //byte[] arr2 = ConvertFileToByte(clock_file[0]);
                                    link.DBCmd.Parameters.Add(":CLOCK_FILE", OracleDbType.Blob).Value = arr;
                                    link.DBOpen();
                                    link.DBCmd.ExecuteNonQuery();
                                    link.DBClose();
                                }
                            }

                        }
                        #endregion

                        #region 拋轉TOCC
                        string TOCCid = creatid("TOCC_DATA", userno, feeno, "0");

                        var TOCCmsg = "";
                        //症狀
                        TOCCmsg += "病人目前";
                        if(sel_data(dt, "param_symptom") == "無" || sel_data(dt, "param_symptom") == "")
                        {
                            TOCCmsg += "無症狀。";
                        }
                        else
                        {
                            var transsymptom = sel_data(dt, "param_symptom").Replace(',','、');
                            string symptomOther = "";
                            if (sel_data(dt, "param_symptom_other").ToString() != "")
                            {
                                symptomOther += sel_data(dt, "param_symptom_other").ToString();
                                transsymptom = transsymptom.Replace("其他", symptomOther);
                            }

                            TOCCmsg += transsymptom;
                            TOCCmsg += "。";
                        }
                        //旅遊史(Travel)
                        TOCCmsg += "旅遊史(Travel)：最近14日內";

                        if (sel_data(dt, "param_travel") == "最近 14 日內無國內、外旅遊")
                        {
                            TOCCmsg += "無國內、外旅遊。";
                           
                        }
                        else
                        {
                            var test = sel_data(dt, "param_travel");
                            string travel = sel_data(dt, "param_travel").ToString();
                            var travleArr = travel.Split(',');
                            for(int i = 0; i < travleArr.Count(); i++)
                            {
                                switch(travleArr[i])
                                {
                                    case "最近 14 日內國內旅遊":
                                        TOCCmsg += "國內旅遊";
                                        TOCCmsg += "(";
                                        if (sel_data(dt, "param_travel_domestic_city") != "")
                                        {
                                            TOCCmsg += sel_data(dt, "param_travel_domestic_city");
                                        }
                                        if (sel_data(dt, "param_travel_domestic_city") != "" && sel_data(dt, "param_travel_domestic_viewpoint") != "")
                                        {
                                            TOCCmsg += "，";
                                        }
                                        if (sel_data(dt, "param_travel_domestic_viewpoint") != "")
                                        {
                                            TOCCmsg += sel_data(dt, "param_travel_domestic_viewpoint");
                                        }
                                        if (sel_data(dt, "param_travel_domestic_city") != "" || sel_data(dt, "param_travel_domestic_viewpoint") != "")
                                        {
                                            TOCCmsg += "，";
                                        }
                                        string traffic = "交通方式：" + sel_data(dt, "param_travel_domestic_traffic");
                                        string trafficOther = "";
                                        if (sel_data(dt, "param_travel_domestic_traffic_other") != "")
                                        {
                                            trafficOther += sel_data(dt, "param_travel_domestic_traffic_other");
                                        }
                                        traffic = traffic.Replace("其他", trafficOther);
                                        TOCCmsg += traffic;
                                        TOCCmsg += ")";

                                        if (travleArr.Count() > 1)
                                        {
                                            TOCCmsg += "、";
                                        }
                                        break;
                                    case "最近 14 日內國外旅遊(包含轉機或船舶停靠曾到訪)":
                                        TOCCmsg += "國外旅遊(包含轉機或船舶停靠曾到訪)";
                                        TOCCmsg += "(";
                                        if (sel_data(dt, "param_travel_aboard_country") != "")
                                        {
                                            TOCCmsg += sel_data(dt, "param_travel_aboard_country");
                                        }
                                        if (sel_data(dt, "param_travel_aboard_country") != "" && sel_data(dt, "param_travel_aboard_destination") != "")
                                        {
                                            TOCCmsg += "，";
                                        }
                                        if (sel_data(dt, "param_travel_aboard_destination") != "")
                                        {
                                            TOCCmsg += sel_data(dt, "param_travel_aboard_destination");
                                        }
                                        if (sel_data(dt, "param_travel_aboard_country") != "" || sel_data(dt, "param_travel_aboard_destination") != "")
                                        {
                                            TOCCmsg += "，";
                                        }
                                        string trafficAboard = "交通方式：" + sel_data(dt, "param_travel_aboard_traffic");
                                        string trafficAboardOther = "";
                                        if (sel_data(dt, "param_travel_aboard_traffic_other") != "")
                                        {
                                            trafficAboardOther += sel_data(dt, "param_travel_aboard_traffic_other");
                                            trafficAboard = trafficAboard.Replace("其他", trafficAboardOther);
                                        }
                                        TOCCmsg += trafficAboard;
                                        TOCCmsg += ")";
                                        break;
                                }
                                
                            }
                            TOCCmsg += "。";

                        }
                        //職業別
                        string occupation = "";
                        string occupation_other = "";

                        occupation += "職業別(Occupation)：";
                        occupation += sel_data(dt, "param_occupation").ToString();
                        if (sel_data(dt, "param_occupation_other") != "")
                        {
                            occupation_other += sel_data(dt, "param_occupation_other");
                            occupation = occupation.Replace("其他", occupation_other);
                        }
                        TOCCmsg += occupation + "。";

                        //接觸史
                        TOCCmsg += "接觸史(Contact)：";

                        if (sel_data(dt, "param_contact") == "無")
                        {
                            TOCCmsg += "無。";
                        }
                        else
                        {
                            string contact = sel_data(dt, "param_contact").ToString();
                            var contactArr = contact.Split(',');
                            for (int i = 0; i < contactArr.Count(); i++)
                            {
                                switch (contactArr[i])
                                {
                                    case "接觸禽鳥類、畜類等":
                                        TOCCmsg += "接觸禽鳥類、畜類等 : ";
                                        if(sel_data(dt, "param_contact_birds") != "")
                                        {
                                            TOCCmsg += "(" + sel_data(dt, "param_contact_birds") + ")";
                                        }
                                        TOCCmsg += "。";
                                        break;
                                    case "孕/產婦接觸史":
                                        TOCCmsg += "孕/產婦接觸史：";
                                        if (sel_data(dt, "param_contact_obstetrics_symptom") != "")
                                        {
                                            TOCCmsg += "(1) 生產前 14 天內，同住家人有發燒、腹瀉、咳嗽、流鼻水等疑似感染症狀 : " + sel_data(dt, "param_contact_obstetrics_symptom") +"。"　;
                                        }
                                        if (sel_data(dt, "param_contact_obstetrics_sickleave") != "")
                                        {
                                            TOCCmsg += "(2) 生產前 14 天內，同住家人學校班上同學有因為傳染病請假或班級停課之情形 : " + sel_data(dt, "param_contact_obstetrics_sickleave") + "。";
                                        }
                                        if (sel_data(dt, "param_contact_obstetrics_symptomcaregiver") != "")
                                        {
                                            TOCCmsg += "(3) 住院期間照顧者(應盡量維持同一人)，目前有發燒、腹瀉、咳嗽、流鼻水等疑似感染症狀 : " + sel_data(dt, "param_contact_obstetrics_symptomcaregiver") + "。";
                                        }
                                        break;
                                    case "其他":
                                        if (sel_data(dt, "param_contact_other") != "")
                                        {
                                            TOCCmsg += sel_data(dt, "param_contact_other") + "。";
                                        }
                                        break;
                                }
                            }
                        }
                        //群聚史(Cluster)
                        TOCCmsg += "群聚史(Cluster)：";
                        if (sel_data(dt, "param_cluster") != "")
                        {
                            if (sel_data(dt, "param_cluster") == "無")
                            {
                                TOCCmsg += "無。";
                            }
                            else
                            {
                                TOCCmsg += "家人/朋友/同事有發燒或類流感症狀 : ";
                                if (sel_data(dt, "param_cluster_relatives") != "")
                                {
                                    var transcluster = sel_data(dt, "param_cluster_relatives").Replace(',', '、');
                                    var clusterOther = "";
                                    if (sel_data(dt, "param_cluster_relatives_other") != "")
                                    {
                                        clusterOther += sel_data(dt, "param_cluster_relatives_other");
                                        transcluster = transcluster.Replace("其他", clusterOther);
                                    }
                                    TOCCmsg += transcluster;

                                }
                                TOCCmsg += "。";
                            }
                        }

                        if (TOCCmsg != "")
                        {
                            erow += base.Insert_CareRecord(DateTime.Now.ToString("yyyy/MM/dd HH:mm:05"), TOCCid, "TOCC評估", TOCCmsg, "", "", "", "", "TOCC");
                            base.Insert_CareRecordMapper(DateTime.Now.ToString("yyyy/MM/dd HH:mm:05"), TOCCid, TableID, "TOCC");
                        }

                        #endregion

                        #region 拋轉VitalSign
                        //有身高體重
                        string record_time = sel_data(dt, "param_tube_date") + " " + sel_data(dt, "param_tube_time");
                        string vs_id = creatid("ASSTOVS", userno, feeno, "0");
                        if ((sel_data(dt, "param_BodyHeight") != "" || sel_data(dt, "param_BodyWeight") != "") && base.switchAssessmentInto == "Y")
                        {

                            if (sel_data(dt, "param_BodyHeight") != "")
                                insert_vs("bh", "", sel_data(dt, "param_BodyHeight"), record_time, vs_id);
                            if (sel_data(dt, "param_BodyWeight") != "")
                                insert_vs("bw", "", sel_data(dt, "param_BodyWeight") + "|Kg", record_time, vs_id);
                        }
                        //體溫
                        if (sel_data(dt, "bt_part") != "" && sel_data(dt, "bt_record") != "" && base.switchAssessmentInto == "Y")
                            insert_vs("bt", sel_data(dt, "bt_part"), sel_data(dt, "bt_record"), record_time, vs_id);
                        //心跳
                        if (sel_data(dt, "mp_part") != "" && sel_data(dt, "mp_record") != "" && base.switchAssessmentInto == "Y")
                            insert_vs("mp", sel_data(dt, "mp_part") + "|左側", sel_data(dt, "mp_record"), record_time, vs_id);
                        //呼吸
                        if (sel_data(dt, "bf_record") != "" && base.switchAssessmentInto == "Y")
                            insert_vs("bf", "", sel_data(dt, "bf_record"), record_time, vs_id);
                        //血壓
                        if (sel_data(dt, "bp_position") != "" && sel_data(dt, "bp_posture") != "" && sel_data(dt, "bp_record_s") != "" && sel_data(dt, "bp_record_d") != "" && base.switchAssessmentInto == "Y")
                            insert_vs("bp", sel_data(dt, "bp_posture") + "|" + sel_data(dt, "bp_position"), sel_data(dt, "bp_record_s") + "|" + sel_data(dt, "bp_record_d"), record_time, vs_id);
                        #endregion

                        #region 拋轉VitalSign 疼痛
                        int ps_value = 0;
                        if (sel_data(dt, "param_pain_assessment_assess") != "" && base.switchAssessmentInto == "Y")
                        {
                            //string vs_part = sel_data(dt, "param_pain_assessment_occasion") + "|" + sel_data(dt, "param_pain_assessment_assess");
                            string vs_part = sel_data(dt, "param_pain_assessment_assess");
                            string vs_record = sel_data(dt, "param_pain_assessment_record");
                            //疼痛計分
                            Regex rgx = new Regex(@"^([(]\d+[)])|^(\d+)");
                            foreach (string ps in vs_record.ToString().Split('|'))
                            {
                                if (ps != "")
                                {
                                    ps_value += int.Parse(rgx.Match(ps).ToString().Replace("(", "").Replace(")", ""));
                                }
                            }
                            //拋轉
                            insert_vs("ps", vs_part, vs_record, record_time, vs_id);
                        }
                        #endregion

                        #region 拋轉心情評估
                        if (sel_data(dt, "param_mood") == "否")
                        {
                            insertDataList.Clear();
                            string[] StrMoodScore = { "完全沒有(0)", "輕微(1)", "中等程度(2)", "厲害(3)", "非常厲害(4)" };
                            string TempSpirituality1 = "", TempSpirituality2 = "", TempSpirituality3 = "", TempSpirituality4 = "";
                            string mood_id = creatid("MOOD_ASSESS", userno, feeno, "0");
                            string mood_date = sel_data(dt, "param_tube_date") + " " + sel_data(dt, "param_tube_time");
                            string tempStr = "", msg = "", total_remark = "";
                            int score = Convert.ToInt32(sel_data(dt, "param_hidden_mood_total"));
                            string i_p = sel_data(dt, "Spirituality_issues_patient");
                            string i_f = sel_data(dt, "Spirituality_issues_family");
                            string r_p = sel_data(dt, "Spirituality_religion_patient");
                            string r_f = sel_data(dt, "Spirituality_religion_family");
                            tempStr = i_p + "|" + i_f + "|" + r_p + "|" + r_f;

                            if (sel_data(dt, "param_Spirituality_issues_patient") != null)
                                TempSpirituality1 = (sel_data(dt, "param_Spirituality_issues_patient").IndexOf("其他") < 0) ? sel_data(dt, "param_Spirituality_issues_patient")
                                : sel_data(dt, "param_Spirituality_issues_patient") + "|" + sel_data(dt, "param_Spirituality_issues_patient_other");
                            if (sel_data(dt, "param_Spirituality_issues_family") != null)
                                TempSpirituality2 = (sel_data(dt, "param_Spirituality_issues_family").IndexOf("其他") < 0) ? sel_data(dt, "param_Spirituality_issues_family")
                                : sel_data(dt, "param_Spirituality_issues_family") + "|" + sel_data(dt, "param_Spirituality_issues_family_other");
                            if (sel_data(dt, "param_Spirituality_religion_patient") != null)
                                TempSpirituality3 = (sel_data(dt, "param_Spirituality_religion_patient").IndexOf("其他") < 0) ? sel_data(dt, "param_Spirituality_religion_patient")
                                : sel_data(dt, "param_Spirituality_religion_patient") + "|" + sel_data(dt, "param_Spirituality_religion_patient_other");
                            if (sel_data(dt, "param_Spirituality_religion_family") != null)
                                TempSpirituality4 = (sel_data(dt, "param_Spirituality_religion_family").IndexOf("其他") < 0) ? sel_data(dt, "param_Spirituality_religion_family")
                                : sel_data(dt, "param_Spirituality_religion_family") + "|" + sel_data(dt, "param_Spirituality_religion_family_other");

                            if (score >= 15)
                                total_remark = "(重度情緒困擾，建議諮詢精神科醫師接受進一步評估。)";
                            else if (score <= 14 && score >= 10)
                                total_remark = "(中度情緒困擾，建議諮詢精神科醫師接受進一步評估。)";
                            else if (score <= 9 && score >= 6)
                                total_remark = "(輕度情緒困擾，建議尋求紓壓管道或接受心理專業諮詢。)";
                            else if (score <= 5)
                            {
                                total_remark = "(身心適應狀況良好。)";
                            }

                            #region 護理紀錄
                            msg += "住院病友心情評估總分：" + sel_data(dt, "param_hidden_mood_total") + "分，" + total_remark;
                            if (sel_data(dt, "param_mood_suicidal_thoughts").Split(':')[0] != "" && sel_data(dt, "param_mood_suicidal_thoughts").Split(':')[0] != null)
                                msg += "自殺想法：" + StrMoodScore[Convert.ToInt32(sel_data(dt, "param_mood_suicidal_thoughts").Split(':')[0])] + "。";
                            if (i_p == "無" && r_p == "無")
                            {
                                msg += "病人無社會及靈性宗教問題。";
                            }
                            else
                            {
                                if (i_p == "有" && r_p == "無")
                                    msg += "病人有" + TempSpirituality1.Replace("|", ":") + "等社會問題，無靈性宗教問題。";
                                if (i_p == "無" && r_p == "有")
                                    msg += "病人有" + TempSpirituality3.Replace("|", ":") + "等靈性宗教問題，無社會問題。";
                                if (i_p == "有" && r_p == "有")
                                    msg += "病人有" + TempSpirituality1.Replace("|", ":") + "等社會問題，靈性宗教有" + TempSpirituality3.Replace("|", ":") + "問題。";
                            }

                            if (i_p == "無法評估" && r_p == "無法評估")
                            {
                                msg += "無法評估病人社會及靈性宗教問題。";
                            }
                            else
                            {
                                if (i_p == "無法評估" && r_p == "無")
                                    msg += "無法評估病人是否有社會問題，靈性宗教無問題。";
                                if (i_p == "無法評估" && r_p == "有")
                                    msg += "無法評估病人是否有社會問題，靈性宗教有" + TempSpirituality3.Replace("|", ":") + "等問題。";
                                if (i_p == "無" && r_p == "無法評估")
                                    msg += "病人無社會問題，靈性宗教問題無法評估。";
                                if (i_p == "有" && r_p == "無法評估")
                                    msg += "病人有" + TempSpirituality1.Replace("|", ":") + "等社會問題，靈性宗教問題無法評估。";
                            }

                            if (i_f == "無" && r_f == "無")
                            {
                                msg += "家屬無社會及靈性宗教問題。";
                            }
                            else
                            {
                                if (i_f == "有" && r_f == "無")
                                    msg += "家屬有" + TempSpirituality2.Replace("|", ":") + "等社會問題，無靈性宗教問題。";
                                if (i_f == "無" && r_f == "有")
                                    msg += "家屬有" + TempSpirituality4.Replace("|", ":") + "等靈性宗教問題，無社會問題。";
                                if (i_f == "有" && r_f == "有")
                                    msg += "家屬有" + TempSpirituality2.Replace("|", ":") + "等社會問題，靈性宗教有" + TempSpirituality4.Replace("|", ":") + "問題。";
                            }

                            if (i_f == "無法評估" && r_f == "無法評估")
                            {
                                msg += "無法評估家屬社會及靈性宗教問題。";
                            }
                            else
                            {
                                if (i_f == "無法評估" && r_f == "無")
                                    msg += "無法評估家屬是否有社會問題，靈性宗教無問題。";
                                if (i_f == "無法評估" && r_f == "有")
                                    msg += "無法評估家屬是否有社會問題，靈性宗教有" + TempSpirituality4.Replace("|", ":") + "等問題。";
                                if (i_f == "無" && r_f == "無法評估")
                                    msg += "家屬無社會問題，靈性宗教問題無法評估。";
                                if (i_f == "有" && r_f == "無法評估")
                                    msg += "家屬有" + TempSpirituality2.Replace("|", ":") + "等社會問題，靈性宗教問題無法評估。";
                            }
                            //if(!string.IsNullOrEmpty(form["label_remark_0"].Trim()))
                            //{
                            //    msg += "備註：" + form["label_remark_0"] + "。";
                            //}
                            #endregion //帶入心情評估-護理紀錄

                            insertDataList.Add(new DBItem("ASSESS_DT", mood_date, DBItem.DBDataType.DataTime));
                            insertDataList.Add(new DBItem("SCORE_1", sel_data(dt, "param_mood_jittery").Split(':')[0], DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("SCORE_2", sel_data(dt, "param_mood_distress_flare").Split(':')[0], DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("SCORE_3", sel_data(dt, "param_mood_gloomy").Split(':')[0], DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("SCORE_4", sel_data(dt, "param_mood_feeling_failure").Split(':')[0], DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("SCORE_5", sel_data(dt, "param_mood_difficulty_sleeping").Split(':')[0], DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("SUICIDAL_THOUGHTS_SCORE", sel_data(dt, "param_mood_suicidal_thoughts").Split(':')[0], DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("TOTAL_SCORE", sel_data(dt, "param_hidden_mood_total"), DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("SPIRITUALITY", tempStr, DBItem.DBDataType.String));

                            insertDataList.Add(new DBItem("SOCIETY_PT", TempSpirituality1, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("SOCIETY_FM", TempSpirituality2, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("RELIGION_PT", TempSpirituality3, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("RELIGION_FM", TempSpirituality4, DBItem.DBDataType.String));

                            insertDataList.Add(new DBItem("MEASURE", "", DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("REMARK", "", DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("DEL_USER", "", DBItem.DBDataType.String));

                            insertDataList.Add(new DBItem("ASSESS_ID", mood_id, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("FEE_NO", feeno, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("CREATE_USER", userno, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("CREATE_USER_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("CREATE_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("MODIFY_USER", userno, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("MODIFY_USER_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("MODIFY_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.String));
                            erow = ass_m.DBExecInsert("MOOD_ASSESSMENT_DATA", insertDataList);
                            erow += base.Insert_CareRecord(mood_date, mood_id, "病友心情評估", msg, "", "", "", "", "Mood");
                        }
                        //ass_m.DBExecInsert("NIS_PRESSURE_SORE_DATA", insertDataList);


                        #endregion

                        #region 營養評估-成人
                        //if(!string.IsNullOrEmpty(sel_data(dt, "param_nutrition_fasting")))
                        //{
                        List<DBItem> DBItemDataList = new List<DBItem>();
                        string n_a_ID = base.creatid("NUTRITIONAL_ASSESSMENT", userno, feeno, "0");
                        string n_a_date = sel_data(dt, "param_tube_date") + " " + sel_data(dt, "param_tube_time");
                        DBItemDataList.Add(new DBItem("NUTA_ID", n_a_ID, DBItem.DBDataType.String));
                        DBItemDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                        DBItemDataList.Add(new DBItem("NUTA_ASSESSMENT_DTM", n_a_date, DBItem.DBDataType.DataTime));
                        DBItemDataList.Add(new DBItem("NUTA_HEIGHT", sel_data(dt, "param_BodyHeight"), DBItem.DBDataType.String));
                        DBItemDataList.Add(new DBItem("NUTA_WEIGHT", sel_data(dt, "param_BodyWeight"), DBItem.DBDataType.String));
                        //DBItemDataList.Add(new DBItem("NUTA_HEAD", NutaHead, DBItem.DBDataType.String));  //  兒童才有
                        DBItemDataList.Add(new DBItem("NUTA_BMI", (!string.IsNullOrEmpty(sel_data(dt, "param_BodyHeight")) && !string.IsNullOrEmpty(sel_data(dt, "param_BodyWeight"))) ? BMI_Compute(Convert.ToDouble(sel_data(dt, "param_BodyHeight")), Convert.ToDouble(sel_data(dt, "param_BodyWeight"))) : "", DBItem.DBDataType.String));
                        DBItemDataList.Add(new DBItem("NUTA_ASSESSMENT_ROW1", sel_data(dt, "param_nutrition_quality").Substring(0, 1), DBItem.DBDataType.String));
                        DBItemDataList.Add(new DBItem("NUTA_ASSESSMENT_ROW2", sel_data(dt, "param_nutrition_loss").Substring(0, 1), DBItem.DBDataType.String));
                        DBItemDataList.Add(new DBItem("NUTA_ASSESSMENT_ROW3", sel_data(dt, "param_nutrition_fasting").Substring(0, 1), DBItem.DBDataType.String));
                        //if(NutaType == "adult")
                        //{
                        DBItemDataList.Add(new DBItem("NUTA_ASSESSMENT_ROW4", sel_data(dt, "param_nutrition_totle"), DBItem.DBDataType.String));//param_nutrition_totle
                                                                                                                                                //}
                        DBItemDataList.Add(new DBItem("NUTA_ASSESSMENT_RESULT", "", DBItem.DBDataType.String));//沒用到這欄位
                        DBItemDataList.Add(new DBItem("NUTA_CARE_RECORD", "Y", DBItem.DBDataType.String));
                        //DBItemDataList.Add(new DBItem("NUTA_WEIGHT_UNIT", form["DdlNutaWeightUnit"], DBItem.DBDataType.String));  //拿掉，詳見頁面程式
                        DBItemDataList.Add(new DBItem("NUTA_TYPE", "adult", DBItem.DBDataType.String));
                        DBItemDataList.Add(new DBItem("DELETED", "N", DBItem.DBDataType.String));
                        DBItemDataList.Add(new DBItem("CREANO", userno, DBItem.DBDataType.String));
                        DBItemDataList.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                        int ERow = ass_m.DBExecInsert("NUTRITIONAL_ASSESSMENT", DBItemDataList);
                        if (ERow > 0)
                        {
                            #region  護理紀錄
                            string CareRecordCont = "", title = "";
                            title = "營養評估-成人";
                            CareRecordCont = "病人" + sel_data(dt, "param_BodyHeight") + "cm，" + sel_data(dt, "param_BodyWeight") + "kg，BMI：";
                            CareRecordCont += (!string.IsNullOrEmpty(sel_data(dt, "param_BodyHeight")) && !string.IsNullOrEmpty(sel_data(dt, "param_BodyWeight"))) ? BMI_Compute(Convert.ToDouble(sel_data(dt, "param_BodyHeight")), Convert.ToDouble(sel_data(dt, "param_BodyWeight"))) : "";// "，MUST營養評估" + NutaAssessmentRow4 + "分。";
                            if (Convert.ToInt32(sel_data(dt, "param_nutrition_totle")) < 3)
                            {
                                CareRecordCont += "，MUST營養評估" + sel_data(dt, "param_nutrition_totle") + "分，暫時不需要進一步的營養評估。";
                            }
                            else
                            {
                                CareRecordCont += "，MUST營養評估" + sel_data(dt, "param_nutrition_totle") + "分，為營養不良高度風險，通知主治醫師決定是否照會營養師做進一步的營養評估。";
                            }

                            base.Insert_CareRecord(n_a_date, n_a_ID, title, CareRecordCont, "", "", "", "", "NUTRITIONAL_ASSESSMENT");

                            #endregion  護理紀錄 end
                        }
                        DBItemDataList.Clear();
                        //}
                        #endregion 營養評估-成人-end

                        #region 拋轉護理紀錄
                        //舊版拋轉
                        //content = "病人因" + sel_data(dt, "param_ipd_reason").Replace("|", ",") + "於" + sel_data(dt, "param_tube_date") + " " + sel_data(dt, "param_tube_time");
                        //content += "經由" + sel_data(dt, "param_ipd_style") + sel_data(dt, "param_ipd_style_other").Replace("|", ",") + "入院，";
                        //content += "給予病人/家屬入院護理及環境介紹，通知醫師前往診視。";

                        content = "病人因" + sel_data(dt, "param_ipd_reason").Replace("|", "，") + "於" + sel_data(dt, "param_tube_date") + " " + sel_data(dt, "param_tube_time");
                        content += "經由" + sel_data(dt, "param_ipd_source") + sel_data(dt, "param_ipd_style").Replace("|", "，") + "入" + ptinfo.BedNo + "治療，入院診斷：" + ptinfo.ICD9_code1;
                        content += "， Vital sign：" + sel_data(dt, "bt_record") + "℃，" + sel_data(dt, "mp_record") + "次/分，" + sel_data(dt, "bf_record") + "次/分，";
                        var gc_st = "";
                        if(sel_data(dt, "gc_r1").Equals("") || sel_data(dt, "gc_r2").Equals("") || sel_data(dt, "gc_r3").Equals(""))
                        {
                            gc_st = "";
                        }
                        else
                        {
                            gc_st = "，意識狀況：E" + sel_data(dt, "gc_r1") + "V" + sel_data(dt, "gc_r2") + "M" + sel_data(dt, "gc_r3")+ "";
                        }
                        content += sel_data(dt, "bp_record_s") + "/" + sel_data(dt, "bp_record_d") + "mmHg，疼痛強度：" + sel_data(dt, "param_pain_assessment_assess") + " " + ps_value + "分"+gc_st;

                        //content += sel_data(dt, "bp_record_s") + "/" + sel_data(dt, "bp_record_d") + "mmHg，疼痛強度：" + sel_data(dt, "param_pain_assessment_assess") + " " + ps_value + "分，意識狀況：E" + sel_data(dt, "gc_r1") + "V" + sel_data(dt, "gc_r2") + "M" + sel_data(dt, "gc_r3") + "";
                        content += "， 給予病人/家屬入院護理及環境介紹。";
                        #endregion
                    }
                    else if (na_type == "C")//兒童
                    {
                        #region 拋轉跌倒兒童版
                        if (sel_data(dt, "param_total_fall") != "" && base.switchAssessmentInto == "Y")//有跌倒分數
                        {
                            insertDataList.Clear();
                            string date = sel_data(dt, "param_tube_date") + " " + sel_data(dt, "param_tube_time");
                            string id = creatid("FALL_ASSESS_DATA", userno, feeno, "0");

                            insertDataList.Add(new DBItem("FALL_ID", id, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("CREATNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                            insertDataList.Add(new DBItem("RECORDTIME", date, DBItem.DBDataType.DataTime));
                            insertDataList.Add(new DBItem("NUM", "", DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("AGE", sel_data(dt, "param_age_fall"), DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("GENDER", sel_data(dt, "param_sex_fall"), DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("FITNESS_FALL", sel_data(dt, "param_fitness_fall"), DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("FALL_HISTORY", sel_data(dt, "param_history_fall"), DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("ACTIVITY", sel_data(dt, "param_motility_fall"), DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("DRUG", sel_data(dt, "param_drug_fall"), DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("DISEASE", sel_data(dt, "param_disease_fall"), DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("TOTAL", sel_data(dt, "param_total_fall"), DBItem.DBDataType.Number));
                            insertDataList.Add(new DBItem("REASON", "入院/轉入", DBItem.DBDataType.String));

                            if (ass_m.DBExecInsert("NIS_FALL_ASSESS_DATA_CHILD", insertDataList) > 0)
                            {
                                ass_m.DBExecDelete("DATA_NOTICE", " NT_ID = 'FALL_CHILD_" + feeno + "' AND FEE_NO = '" + feeno + "' ");
                                string notice_day = ass_m.sel_last_fall_assess_time_And_total(feeno, "NIS_FALL_ASSESS_DATA_CHILD");
                                if (notice_day != "")
                                {
                                    new FallAssessController().Do_FallNotice(feeno, "CHILD", notice_day, "9999/12/31 00:00:00");
                                }
                                string msg = "";
                                if (int.Parse(sel_data(dt, "param_total_fall")) >= 3)
                                {
                                    msg = "評估原因：" + "入院/轉入" + "，評估病人跌倒危險因子分數為" + sel_data(dt, "param_total_fall").ToString() + "，持續追蹤病人狀況。";
                                    string item_id = ass_m.sel_health_education_item(feeno, "FALL");
                                    if (item_id != "")
                                    {
                                        string health_education_id = base.creatid("EDUCATION_DATA", userno, feeno, "0");
                                        string health_education_date = DateTime.Now.ToString("yyyy/MM/dd HH:mm");
                                        insertDataList.Clear();
                                        insertDataList.Add(new DBItem("EDU_ID", health_education_id, DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("CREATTIME", health_education_date, DBItem.DBDataType.DataTime));
                                        insertDataList.Add(new DBItem("RECORDTIME", health_education_date, DBItem.DBDataType.DataTime));
                                        insertDataList.Add(new DBItem("ITEMID", item_id, DBItem.DBDataType.String));
                                        ass_m.DBExecInsert("HEALTH_EDUCATION_DATA", insertDataList);
                                    }
                                }
                                else
                                {
                                    msg = "評估原因：" + "入院/轉入" + "，評估病人跌倒危險因子分數為" + sel_data(dt, "param_total_fall").ToString() + "分，持續追蹤病人狀況。";
                                }
                                Insert_CareRecord(date, id, "跌倒危險性評估", "", "", msg, "", "", "FALL_ASSESS");
                            }
                        }

                        #endregion

                        string vs_id = creatid("ASSTOVS", userno, feeno, "0");
                        #region 拋轉身高體重
                        if ((sel_data(dt, "param_body_height") != "" || sel_data(dt, "param_body_weight") != "") && base.switchAssessmentInto == "Y")//有身高體重
                        {
                            string date_time = sel_data(dt, "param_tube_date") + " " + sel_data(dt, "param_tube_time");
                            if (sel_data(dt, "param_body_height") != "")
                                insert_vs("bh", "", sel_data(dt, "param_body_height"), date_time, vs_id);
                            if (sel_data(dt, "param_body_weight") != "")
                                insert_vs("bw", "", sel_data(dt, "param_body_weight") + "|Kg", date_time, vs_id);
                        }
                        #endregion

                        #region 拋轉VitalSign 疼痛(兒童)
                        int ps_value = 0;
                        if (sel_data(dt, "param_pain_assessment_assess") != "" && base.switchAssessmentInto == "Y")
                        {
                            //string vs_part = sel_data(dt, "param_pain_assessment_occasion") + "|" + sel_data(dt, "param_pain_assessment_assess");
                            string vs_part = sel_data(dt, "param_pain_assessment_assess");
                            string vs_record = sel_data(dt, "param_pain_assessment_record");
                            //疼痛計分
                            Regex rgx = new Regex(@"^([(]\d+[)])|^(\d+)");
                            foreach (string ps in vs_record.ToString().Split('|'))
                            {
                                if (ps != "")
                                {
                                    ps_value += int.Parse(rgx.Match(ps).ToString().Replace("(", "").Replace(")", ""));
                                }
                            }
                            //拋轉
                            insert_vs("ps", vs_part, vs_record, sel_data(dt, "param_tube_date"), vs_id);
                        }
                        #endregion

                        #region 拋轉心情評估
                        if (sel_data(dt, "param_mood") == "否")
                        {
                            insertDataList.Clear();
                            string[] StrMoodScore = { "完全沒有(0)", "輕微(1)", "中等程度(2)", "厲害(3)", "非常厲害(4)" };
                            string TempSpirituality1 = "", TempSpirituality2 = "", TempSpirituality3 = "", TempSpirituality4 = "";
                            string mood_id = creatid("MOOD_ASSESS", userno, feeno, "0");
                            string mood_date = sel_data(dt, "param_tube_date") + " " + sel_data(dt, "param_tube_time");
                            string tempStr = "", msg = "", total_remark = "";
                            int score = Convert.ToInt32(sel_data(dt, "param_hidden_mood_total"));
                            string i_p = sel_data(dt, "Spirituality_issues_patient");
                            string i_f = sel_data(dt, "Spirituality_issues_family");
                            string r_p = sel_data(dt, "Spirituality_religion_patient");
                            string r_f = sel_data(dt, "Spirituality_religion_family");
                            tempStr = i_p + "|" + i_f + "|" + r_p + "|" + r_f;

                            var test = sel_data(dt, "Spirituality_issues_patient");
                            if (sel_data(dt, "param_Spirituality_issues_patient") != null)
                                TempSpirituality1 = (sel_data(dt, "param_Spirituality_issues_patient").IndexOf("其他") < 0) ? sel_data(dt, "param_Spirituality_issues_patient")
                                : sel_data(dt, "param_Spirituality_issues_patient") + "|" + sel_data(dt, "param_Spirituality_issues_patient_other");
                            if (sel_data(dt, "param_Spirituality_issues_family") != null)
                                TempSpirituality2 = (sel_data(dt, "param_Spirituality_issues_family").IndexOf("其他") < 0) ? sel_data(dt, "param_Spirituality_issues_family")
                                : sel_data(dt, "param_Spirituality_issues_family") + "|" + sel_data(dt, "param_Spirituality_issues_family_other");
                            if (sel_data(dt, "param_Spirituality_religion_patient") != null)
                                TempSpirituality3 = (sel_data(dt, "param_Spirituality_religion_patient").IndexOf("其他") < 0) ? sel_data(dt, "param_Spirituality_religion_patient")
                                : sel_data(dt, "param_Spirituality_religion_patient") + "|" + sel_data(dt, "param_Spirituality_religion_patient_other");
                            if (sel_data(dt, "param_Spirituality_religion_family") != null)
                                TempSpirituality4 = (sel_data(dt, "param_Spirituality_religion_family").IndexOf("其他") < 0) ? sel_data(dt, "param_Spirituality_religion_family")
                                : sel_data(dt, "param_Spirituality_religion_family") + "|" + sel_data(dt, "param_Spirituality_religion_family_other");

                            if (score >= 15)
                                total_remark = "(重度情緒困擾，建議諮詢精神科醫師接受進一步評估。)";
                            else if (score <= 14 && score >= 10)
                                total_remark = "(中度情緒困擾，建議諮詢精神科醫師接受進一步評估。)";
                            else if (score <= 9 && score >= 6)
                                total_remark = "(輕度情緒困擾，建議尋求紓壓管道或接受心理專業諮詢。)";
                            else if (score <= 5)
                            {
                                total_remark = "(身心適應狀況良好。)";
                            }

                            #region 護理紀錄
                            msg += "住院病友心情評估總分：" + sel_data(dt, "param_hidden_mood_total") + "分" + total_remark;
                            if (sel_data(dt, "param_mood_suicidal_thoughts").Split(':')[0] != "" && sel_data(dt, "param_mood_suicidal_thoughts").Split(':')[0] != null)
                                msg += "自殺想法：" + StrMoodScore[Convert.ToInt32(sel_data(dt, "param_mood_suicidal_thoughts").Split(':')[0])] + "。";
                            if (i_p == "無" && r_p == "無")
                            {
                                msg += "病人無社會及靈性宗教問題。";
                            }
                            else
                            {
                                if (i_p == "有" && r_p == "無")
                                    msg += "病人有" + TempSpirituality1.Replace("|", ":") + "等社會問題，無靈性宗教問題。";
                                if (i_p == "無" && r_p == "有")
                                    msg += "病人有" + TempSpirituality3.Replace("|", ":") + "等靈性宗教問題，無社會問題。";
                                if (i_p == "有" && r_p == "有")
                                    msg += "病人有" + TempSpirituality1.Replace("|", ":") + "等社會問題，靈性宗教有" + TempSpirituality3.Replace("|", ":") + "問題。";
                            }

                            if (i_p == "無法評估" && r_p == "無法評估")
                            {
                                msg += "無法評估病人社會及靈性宗教問題。";
                            }
                            else
                            {
                                if (i_p == "無法評估" && r_p == "無")
                                    msg += "無法評估病人是否有社會問題，靈性宗教無問題。";
                                if (i_p == "無法評估" && r_p == "有")
                                    msg += "無法評估病人是否有社會問題，靈性宗教有" + TempSpirituality3.Replace("|", ":") + "等問題。";
                                if (i_p == "無" && r_p == "無法評估")
                                    msg += "病人無社會問題，靈性宗教問題無法評估。";
                                if (i_p == "有" && r_p == "無法評估")
                                    msg += "病人有" + TempSpirituality1.Replace("|", ":") + "等社會問題，靈性宗教問題無法評估。";
                            }

                            if (i_f == "無" && r_f == "無")
                            {
                                msg += "家屬無社會及靈性宗教問題。";
                            }
                            else
                            {
                                if (i_f == "有" && r_f == "無")
                                    msg += "家屬有" + TempSpirituality2.Replace("|", ":") + "等社會問題，無靈性宗教問題。";
                                if (i_f == "無" && r_f == "有")
                                    msg += "家屬有" + TempSpirituality4.Replace("|", ":") + "等靈性宗教問題，無社會問題。";
                                if (i_f == "有" && r_f == "有")
                                    msg += "家屬有" + TempSpirituality2.Replace("|", ":") + "等社會問題，靈性宗教有" + TempSpirituality4.Replace("|", ":") + "問題。";
                            }

                            if (i_f == "無法評估" && r_f == "無法評估")
                            {
                                msg += "無法評估家屬社會及靈性宗教問題。";
                            }
                            else
                            {
                                if (i_f == "無法評估" && r_f == "無")
                                    msg += "無法評估家屬是否有社會問題，靈性宗教無問題。";
                                if (i_f == "無法評估" && r_f == "有")
                                    msg += "無法評估家屬是否有社會問題，靈性宗教有" + TempSpirituality4.Replace("|", ":") + "等問題。";
                                if (i_f == "無" && r_f == "無法評估")
                                    msg += "家屬無社會問題，靈性宗教問題無法評估。";
                                if (i_f == "有" && r_f == "無法評估")
                                    msg += "家屬有" + TempSpirituality2.Replace("|", ":") + "等社會問題，靈性宗教問題無法評估。";
                            }
                            //if(!string.IsNullOrEmpty(form["label_remark_0"].Trim()))
                            //{
                            //    msg += "備註：" + form["label_remark_0"] + "。";
                            //}
                            #endregion //帶入心情評估-護理紀錄

                            insertDataList.Add(new DBItem("ASSESS_DT", mood_date, DBItem.DBDataType.DataTime));
                            insertDataList.Add(new DBItem("SCORE_1", sel_data(dt, "param_mood_jittery").Split(':')[0], DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("SCORE_2", sel_data(dt, "param_mood_distress_flare").Split(':')[0], DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("SCORE_3", sel_data(dt, "param_mood_gloomy").Split(':')[0], DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("SCORE_4", sel_data(dt, "param_mood_feeling_failure").Split(':')[0], DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("SCORE_5", sel_data(dt, "param_mood_difficulty_sleeping").Split(':')[0], DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("SUICIDAL_THOUGHTS_SCORE", sel_data(dt, "param_mood_suicidal_thoughts").Split(':')[0], DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("TOTAL_SCORE", sel_data(dt, "param_hidden_mood_total"), DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("SPIRITUALITY", tempStr, DBItem.DBDataType.String));

                            insertDataList.Add(new DBItem("SOCIETY_PT", TempSpirituality1, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("SOCIETY_FM", TempSpirituality2, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("RELIGION_PT", TempSpirituality3, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("RELIGION_FM", TempSpirituality4, DBItem.DBDataType.String));

                            insertDataList.Add(new DBItem("MEASURE", "", DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("REMARK", "", DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("DEL_USER", "", DBItem.DBDataType.String));

                            insertDataList.Add(new DBItem("ASSESS_ID", mood_id, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("FEE_NO", feeno, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("CREATE_USER", userno, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("CREATE_USER_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("CREATE_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("MODIFY_USER", userno, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("MODIFY_USER_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                            insertDataList.Add(new DBItem("MODIFY_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.String));
                            erow = ass_m.DBExecInsert("MOOD_ASSESSMENT_DATA", insertDataList);
                            erow += base.Insert_CareRecord(mood_date, mood_id, "病友心情評估", msg, "", "", "", "", "Mood");
                        }
                        //ass_m.DBExecInsert("NIS_PRESSURE_SORE_DATA", insertDataList);


                        #endregion

                        #region 營養評估-兒童 //只拋選擇［嬰幼兒生長曲線、及青少年營養評估］的 ＊因為MUST選項的欄位屬於成人頁面相關，在營養評估頁面會產生錯誤
                        if (!string.IsNullOrEmpty(sel_data(dt, "param_Evaluation_methods")) && sel_data(dt, "param_Evaluation_methods").ToString() != "MUST營養評估")//param_Evaluation_methods
                        {
                            string n_a_ID = base.creatid("NUTRITIONAL_ASSESSMENT", userno, feeno, "0");
                            string n_a_date = sel_data(dt, "param_tube_date") + " " + sel_data(dt, "param_tube_time");
                            List<DBItem> DBItemDataList = new List<DBItem>();
                            DBItemDataList.Add(new DBItem("NUTA_ID", n_a_ID, DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("NUTA_ASSESSMENT_DTM", n_a_date, DBItem.DBDataType.DataTime));
                            DBItemDataList.Add(new DBItem("NUTA_HEIGHT", sel_data(dt, "param_body_height"), DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("NUTA_WEIGHT", sel_data(dt, "param_body_weight"), DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("NUTA_HEAD", sel_data(dt, "param_body_gthr"), DBItem.DBDataType.String));  //  兒童才有
                            DBItemDataList.Add(new DBItem("NUTA_BMI", (!string.IsNullOrEmpty(sel_data(dt, "param_body_height")) && !string.IsNullOrEmpty(sel_data(dt, "param_body_weight"))) ? BMI_Compute(Convert.ToDouble(sel_data(dt, "param_body_height")), Convert.ToDouble(sel_data(dt, "param_body_weight"))) : "", DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("NUTA_ASSESSMENT_ROW1", sel_data(dt, "param_percentage_H"), DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("NUTA_ASSESSMENT_ROW2", sel_data(dt, "param_percentage_W"), DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("NUTA_ASSESSMENT_ROW3", sel_data(dt, "param_percentage_pHead"), DBItem.DBDataType.String));

                            DBItemDataList.Add(new DBItem("NUTA_ASSESSMENT_RESULT", "", DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("NUTA_CARE_RECORD", "Y", DBItem.DBDataType.String));
                            //DBItemDataList.Add(new DBItem("NUTA_WEIGHT_UNIT", form["DdlNutaWeightUnit"], DBItem.DBDataType.String));  //拿掉，詳見頁面程式
                            DBItemDataList.Add(new DBItem("NUTA_TYPE", "child", DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("DELETED", "N", DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("CREANO", userno, DBItem.DBDataType.String));
                            DBItemDataList.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));

                            int ERow = ass_m.DBExecInsert("NUTRITIONAL_ASSESSMENT", DBItemDataList);

                            if (ERow > 0)
                            {
                                #region  護理紀錄
                                string CareRecordCont = "";
                                string title = "營養評估-兒童";
                                if (sel_data(dt, "param_Evaluation_methods").ToString() == "青少年營養評估")
                                {
                                    CareRecordCont = "病童" + sel_data(dt, "param_body_height") + "cm，" + sel_data(dt, "param_body_weight") + "kg，BMI：";
                                    CareRecordCont += (!string.IsNullOrEmpty(sel_data(dt, "param_body_height")) && !string.IsNullOrEmpty(sel_data(dt, "param_body_weight"))) ? BMI_Compute(Convert.ToDouble(sel_data(dt, "param_body_height")), Convert.ToDouble(sel_data(dt, "param_body_weight"))) : "";
                                    CareRecordCont += "。";
                                }
                                else
                                {
                                    CareRecordCont = "病童" + sel_data(dt, "param_body_height") + "cm，身高百分比區間" + sel_data(dt, "param_percentage_H") + "%，" + sel_data(dt, "param_body_weight") + "kg，體重百分比區間" + sel_data(dt, "param_percentage_W") + "%，BMI：";
                                    CareRecordCont += (!string.IsNullOrEmpty(sel_data(dt, "param_body_height")) && !string.IsNullOrEmpty(sel_data(dt, "param_body_weight"))) ? BMI_Compute(Convert.ToDouble(sel_data(dt, "param_body_height")), Convert.ToDouble(sel_data(dt, "param_body_weight"))) : "";
                                    CareRecordCont += "，頭圍" + sel_data(dt, "param_body_gthr") + "cm，頭圍百分比區間" + sel_data(dt, "param_percentage_pHead") + "%。";

                                }
                                int p_erow = base.Insert_CareRecord(n_a_date, n_a_ID, title, CareRecordCont, "", "", "", "", "NUTRITIONAL_ASSESSMENT");
                                #endregion  護理紀錄 end
                            }
                        }
                        #endregion 營養評估-兒童-end

                        #region 拋轉TOCC
                        string TOCCid = creatid("TOCC_DATA", userno, feeno, "0");

                        var TOCCmsg = "";
                        //症狀
                        TOCCmsg += "病人目前";
                        if (sel_data(dt, "param_symptom") == "無" || sel_data(dt, "param_symptom") == "")
                        {
                            TOCCmsg += "無症狀。";
                        }
                        else
                        {
                            var transsymptom = sel_data(dt, "param_symptom").Replace(',', '、');
                            string symptomOther = "";
                            if (sel_data(dt, "param_symptom_other").ToString() != "")
                            {
                                symptomOther += sel_data(dt, "param_symptom_other").ToString();
                                transsymptom = transsymptom.Replace("其他", symptomOther);
                            }

                            TOCCmsg += transsymptom;
                            TOCCmsg += "。";
                        }
                        //旅遊史(Travel)
                        TOCCmsg += "旅遊史(Travel)：最近14日內";

                        if (sel_data(dt, "param_travel") == "最近 14 日內無國內、外旅遊")
                        {
                            TOCCmsg += "無國內、外旅遊。";
                        }
                        else
                        {
                            string travel = sel_data(dt, "param_travel").ToString();
                            var travleArr = travel.Split(',');
                            for (int i = 0; i < travleArr.Count(); i++)
                            {
                                switch (travleArr[i])
                                {
                                    case "最近 14 日內國內旅遊":
                                        TOCCmsg += "國內旅遊";
                                        TOCCmsg += "(";
                                        if (sel_data(dt, "param_travel_domestic_city") != "")
                                        {
                                            TOCCmsg += sel_data(dt, "param_travel_domestic_city");
                                        }
                                        if (sel_data(dt, "param_travel_domestic_city") != "" && sel_data(dt, "param_travel_domestic_viewpoint") != "")
                                        {
                                            TOCCmsg += "，";
                                        }
                                        if (sel_data(dt, "param_travel_domestic_viewpoint") != "")
                                        {
                                            TOCCmsg += sel_data(dt, "param_travel_domestic_viewpoint");
                                        }
                                        if (sel_data(dt, "param_travel_domestic_city") != "" || sel_data(dt, "param_travel_domestic_viewpoint") != "")
                                        {
                                            TOCCmsg += "，";
                                        }
                                        string traffic = "交通方式：" + sel_data(dt, "param_travel_domestic_traffic");
                                        string trafficOther = "";
                                        if (sel_data(dt, "param_travel_domestic_traffic_other") != "")
                                        {
                                            trafficOther += sel_data(dt, "param_travel_domestic_traffic_other");
                                        }
                                        traffic = traffic.Replace("其他", trafficOther);
                                        TOCCmsg += traffic;
                                        TOCCmsg += ")";

                                        if (travleArr.Count() > 1)
                                        {
                                            TOCCmsg += "、";
                                        }
                                        break;
                                    case "最近 14 日內國外旅遊(包含轉機或船舶停靠曾到訪)":
                                        TOCCmsg += "國外旅遊(包含轉機或船舶停靠曾到訪)";
                                        TOCCmsg += "(";
                                        if (sel_data(dt, "param_travel_aboard_country") != "")
                                        {
                                            TOCCmsg += sel_data(dt, "param_travel_aboard_country");
                                        }
                                        if (sel_data(dt, "param_travel_aboard_country") != "" && sel_data(dt, "param_travel_aboard_destination") != "")
                                        {
                                            TOCCmsg += "，";
                                        }
                                        if (sel_data(dt, "param_travel_aboard_destination") != "")
                                        {
                                            TOCCmsg += sel_data(dt, "param_travel_aboard_destination");
                                        }
                                        if (sel_data(dt, "param_travel_aboard_country") != "" || sel_data(dt, "param_travel_aboard_destination") != "")
                                        {
                                            TOCCmsg += "，";
                                        }
                                        string trafficAboard = "交通方式：" + sel_data(dt, "param_travel_aboard_traffic");
                                        string trafficAboardOther = "";
                                        if (sel_data(dt, "param_travel_aboard_traffic_other") != "")
                                        {
                                            trafficAboardOther += sel_data(dt, "param_travel_aboard_traffic_other");
                                            trafficAboard = trafficAboard.Replace("其他", trafficAboardOther);
                                        }
                                        TOCCmsg += trafficAboard;
                                        TOCCmsg += ")";

                                        break;
                                }

                            }
                            TOCCmsg += "。";

                        }
                        //職業別
                        string occupation = "";
                        string occupation_other = "";

                        occupation += "職業別(Occupation)：";
                        occupation += sel_data(dt, "param_occupation").ToString();
                        if (sel_data(dt, "param_occupation_other") != "")
                        {
                            occupation_other += sel_data(dt, "param_occupation_other");
                            occupation = occupation.Replace("其他", occupation_other);
                        }
                        TOCCmsg += occupation + "。";

                        //接觸史
                        TOCCmsg += "接觸史(Contact)：";

                        if (sel_data(dt, "param_contact") == "無")
                        {
                            TOCCmsg += "無。";
                        }
                        else
                        {
                            string contact = sel_data(dt, "param_contact").ToString();
                            var contactArr = contact.Split(',');

                            if(contactArr.Contains("接觸禽鳥類、畜類等"))
                            {
                                TOCCmsg += "接觸禽鳥類、畜類等 : ";
                                if (sel_data(dt, "param_contact_birds") != "")
                                {
                                    TOCCmsg += "(" + sel_data(dt, "param_contact_birds") + ")";
                                }
                                TOCCmsg += "。";
                            }
                            TOCCmsg += "兒童接觸史：";
                            if (sel_data(dt, "param_contact_child_symptom") != "")
                            {
                                TOCCmsg += "(1) 住院前 14 天內，同住家人有發燒、腹瀉、咳嗽、流鼻水等疑似感染症狀 : " + sel_data(dt, "param_contact_child_symptom") + "。";
                            }
                            if (sel_data(dt, "param_contact_child_sickleave") != "")
                            {
                                TOCCmsg += "(2) 住院前 14 天內，同住家人學校班上同學有因為傳染病請假或班級停課之情形 : " + sel_data(dt, "param_contact_child_sickleave") + "。";
                            }
                            if (sel_data(dt, "param_contact_child_symptomcaregiver") != "")
                            {
                                TOCCmsg += "(3) 住院期間照顧者(應盡量維持同一人)，目前有發燒、腹瀉、咳嗽、流鼻水等疑似感染症狀 : " + sel_data(dt, "param_contact_child_symptomcaregiver") + "。";
                            }
                            if (contactArr.Contains("其他"))
                            {
                                if (sel_data(dt, "param_contact_other") != "")
                                { 
                                        TOCCmsg += sel_data(dt, "param_contact_other") + "。";
                                    
                                }
                            }                    
                        }
                        //群聚史(Cluster)
                        TOCCmsg += "群聚史(Cluster)：";
                        if (sel_data(dt, "param_cluster") != "")
                        {
                            if (sel_data(dt, "param_cluster") == "無")
                            {
                                TOCCmsg += "無。";
                            }
                            else
                            {
                                TOCCmsg += "家人/朋友/同事有發燒或類流感症狀 : ";
                                if (sel_data(dt, "param_cluster_relatives") != "")
                                {
                                    var transcluster = sel_data(dt, "param_cluster_relatives").Replace(',', '、');
                                    var clusterOther = "";
                                    if (sel_data(dt, "param_cluster_relatives_other") != "")
                                    {
                                        clusterOther += sel_data(dt, "param_cluster_relatives_other");
                                        transcluster = transcluster.Replace("其他", clusterOther);
                                    }
                                    TOCCmsg += transcluster;

                                }
                                TOCCmsg += "。";
                            }
                        }

                        if (TOCCmsg != "")
                        {
                            erow += base.Insert_CareRecord(DateTime.Now.ToString("yyyy/MM/dd HH:mm:05"), TOCCid, "TOCC評估", TOCCmsg, "", "", "", "", "TOCC");
                            base.Insert_CareRecordMapper(DateTime.Now.ToString("yyyy/MM/dd HH:mm:05"), TOCCid, TableID, "TOCC");
                        }

                        #endregion

                        //content = "病人因" + sel_data(dt, "param_ipd_reason").Replace("|", ",") + "於" + sel_data(dt, "param_tube_date") + " " + sel_data(dt, "param_tube_time");
                        //content += "經由" + sel_data(dt, "param_ipd_style") + sel_data(dt, "param_ipd_style_other").Replace("|", ",") + "入院，";
                        //content += "，診斷為：" + ptinfo.ICD9_code1.Trim() + "，最近一次因" + ih_desc + "於" + ih_date + "住院。";
                        //content += "給予病人/家屬入院護理及環境介紹，通知醫師前往診視。";

                        content = "病童於" + sel_data(dt, "param_tube_date") + " " + sel_data(dt, "param_tube_time") + "由" + sel_data(dt, "param_primary_care").Replace("|", "，");
                        if (sel_data(dt, "param_ipd_style_other").Replace("|", "，") == "")
                        {
                            content += "陪同" + sel_data(dt, "param_ipd_style").Replace("|", "，") + "入";
                        }
                        else
                        {
                            content += "陪同" + sel_data(dt, "param_ipd_style_other").Replace("|", "，") + "入";
                        }

                        content += "，" + sel_data(dt, "param_source").Replace("|", "，") + "主訴因" + sel_data(dt, "param_ipd_depiction").Replace("|", "，") + "入院治療，";
                        content += "疼痛強度：" + sel_data(dt, "param_pain_assessment_assess") + " " + ps_value + "分，";
                        content += "給予病童/家屬入院護理及環境介紹。";
                    }
                    else if (na_type == "S")//精神
                    {
                        content = "病人因＿＿＿＿＿於" + sel_data(dt, "param_tube_date") + " " + sel_data(dt, "param_tube_time");
                        content += "經由" + sel_data(dt, "param_ipd_style") + sel_data(dt, "param_ipd_style_other").Replace("|", "，") + "入院，";
                        content += "給予病人/家屬入院護理及環境介紹，通知醫師前往診視。";
                    }
                    else if (na_type == "ER")//急診
                    {
                        string vs_id = creatid("ASSTOVS", userno, feeno, "0");
                        #region 拋轉VitalSign 疼痛
                        int ps_value = 0;
                        if (sel_data(dt, "param_pain_assessment_assess") != "" && base.switchAssessmentInto == "Y")
                        {
                            //string vs_part = sel_data(dt, "param_pain_assessment_occasion") + "|" + sel_data(dt, "param_pain_assessment_assess");
                            string vs_part = sel_data(dt, "param_pain_assessment_assess");
                            string vs_record = sel_data(dt, "param_pain_assessment_record");
                            //疼痛計分
                            Regex rgx = new Regex(@"^([(]\d+[)])|^(\d+)");
                            foreach (string ps in vs_record.ToString().Split('|'))
                            {
                                if (ps != "")
                                {
                                    ps_value += int.Parse(rgx.Match(ps).ToString().Replace("(", "").Replace(")", ""));
                                }
                            }
                            //拋轉
                            insert_vs("ps", vs_part, vs_record, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), vs_id);
                        }
                        #endregion

                        #region 急診帶護理紀錄
                        content = "病人一般外觀" + sel_data(dt, "pt_action").Replace(',', '、') + "，";
                        if (sel_data(dt, "param_vs_conscious") == "清醒")
                        {
                            content += "意識評估：" + sel_data(dt, "param_vs_conscious") + "，";
                        }
                        else
                        {
                            content += "意識評估：" + sel_data(dt, "param_vs_conscious_item_other").Replace(',', '、') + "，";
                        }
                        content += "GCS：E：" + sel_data(dt, "gc_r1").Substring(1, 1) + "、V：" + sel_data(dt, "gc_r2").Substring(1, 1) + "、M：" + sel_data(dt, "gc_r3").Substring(1, 1) + "，";
                        if (sel_data(dt, "param_MentalCondition") == "正常")
                            content += "精神狀態：" + sel_data(dt, "param_MentalCondition") + "，";
                        else
                        {
                            content += "精神狀態：" + sel_data(dt, "param_MentalCondition_desc_item_other").Replace(',', '、');
                            if (sel_data(dt, "param_MentalCondition_desc_other_txt") != "")
                                content += "：" + sel_data(dt, "param_MentalCondition_desc_other_txt");
                            content += "，";

                        }
                        if (sel_data(dt, "param_NervousSystem") == "正常")
                            content += "神經糸統：" + sel_data(dt, "param_NervousSystem") + "，";
                        else
                        {
                            string NervousSystemResult = sel_data(dt, "param_Nervous_desc_item_other").Replace(',', '、');
                            if (NervousSystemResult.IndexOf("抽搐") != -1)
                                NervousSystemResult = NervousSystemResult.Replace("抽搐", "抽搐部位：" + sel_data(dt, "twitch_part_txt").Replace(',', '、'));
                            if (NervousSystemResult.IndexOf("痛") != -1)
                                NervousSystemResult = NervousSystemResult.Replace("痛", "痛部位：" + sel_data(dt, "pain_part_txt").Replace(',', '、'));
                            if (NervousSystemResult.IndexOf("麻") != -1)
                                NervousSystemResult = NervousSystemResult.Replace("麻", "麻部位：" + sel_data(dt, "hpan_part_txt").Replace(',', '、'));
                            if (NervousSystemResult.IndexOf("肢體障礙") != -1)
                                NervousSystemResult = NervousSystemResult.Replace("肢體障礙", "肢體障礙：" + sel_data(dt, "LimbDisorders_part").Replace(',', '、'));
                            if (NervousSystemResult.IndexOf("其他") != -1)
                                NervousSystemResult = NervousSystemResult.Replace("其他", "其他：" + sel_data(dt, "param_NervousSystem_desc_other_txt"));
                            content += "神經糸統：" + NervousSystemResult + "，";
                        }
                        if (sel_data(dt, "param_CardiovascularSystem") == "正常")
                            content += "心臟血管系統：" + sel_data(dt, "param_CardiovascularSystem") + "，";
                        else
                        {
                            content += "心臟血管系統：" + sel_data(dt, "param_Cardiovascular_desc_item_other").Replace(',', '、');
                            if (sel_data(dt, "param_Cardiovascular_desc_other_txt") != "")
                                content += "：" + sel_data(dt, "param_Cardiovascular_desc_other_txt");
                            content += "，";
                        }
                        content += "呼吸系統：";
                        if (sel_data(dt, "param_RespiratoryTract") == "通暢")
                            content += "呼吸道：" + sel_data(dt, "param_RespiratoryTract") + "、";
                        else
                        {
                            string RespiratoryTractResult = sel_data(dt, "param_RespiratoryTract_desc_item_other").Replace(',', '、');
                            if (RespiratoryTractResult.IndexOf("痰") != -1)
                                RespiratoryTractResult = RespiratoryTractResult.Replace("痰", "痰：量" + sel_data(dt, "sputum_amount") + "，顏色" + sel_data(dt, "sputum_color"));
                            content += "呼吸道：" + RespiratoryTractResult;
                            if (sel_data(dt, "param_RespiratoryTract_other_txt") != "")
                                content += "：" + sel_data(dt, "param_RespiratoryTract_other_txt");
                            content += "、";
                        }
                        if (sel_data(dt, "param_RespiratoryRateAndPattern") == "正常")
                            content += "呼吸速率：" + sel_data(dt, "param_RespiratoryRateAndPattern") + "、";
                        else
                        {
                            content += "呼吸速率：" + sel_data(dt, "param_RespiratoryRateAndPattern_desc_item_other").Replace(',', '、');
                            if (sel_data(dt, "param_RespiratoryRateAndPattern_other_txt") != "")
                                content += "：" + sel_data(dt, "param_RespiratoryRateAndPattern_other_txt");
                            content += "、";
                        }
                        if (sel_data(dt, "param_RespiratorySound") == "正常")
                            content += "呼吸音：" + sel_data(dt, "param_RespiratorySound") + "，";
                        else
                        {
                            content += "呼吸音：" + sel_data(dt, "param_RespiratorySound_desc_item_other").Replace(',', '、');
                            if (sel_data(dt, "param_RespiratorySound_other_txt") != "")
                                content += "：" + sel_data(dt, "param_RespiratorySound_other_txt");
                            content += "，";
                        }
                        if (sel_data(dt, "param_GastrointestinalSystem") == "正常")
                            content += "腸胃系統：" + sel_data(dt, "param_GastrointestinalSystem") + "，";
                        else
                        {
                            content += "腸胃系統：" + sel_data(dt, "param_GastrointestinalSystem_desc_item_other").Replace(',', '、');
                            if (sel_data(dt, "param_GastrointestinalSystem_desc_other_txt") != "")
                                content += "：" + sel_data(dt, "param_GastrointestinalSystem_desc_other_txt");
                            content += "，";
                        }
                        if (sel_data(dt, "param_UrinarySystem") == "正常")
                            content += "泌尿系統：" + sel_data(dt, "param_UrinarySystem") + "，";
                        else
                        {
                            content += "泌尿系統：" + sel_data(dt, "param_UrinarySystem_desc_item_other").Replace(',', '、');
                            if (sel_data(dt, "param_UrinarySystem_desc_other_txt") != "")
                                content += "：" + sel_data(dt, "param_UrinarySystem_desc_other_txt");
                            content += "，";
                        }
                        if (sel_data(dt, "param_SkinSystem") == "正常")
                            content += "皮膚系統：" + sel_data(dt, "param_SkinSystem") + "，";
                        else
                        {
                            content += "皮膚系統：" + sel_data(dt, "param_SkinSystem_desc_item_other").Replace(',', '、');
                            if (sel_data(dt, "param_SkinSystem_desc_other_txt") != "")
                                content += "：" + sel_data(dt, "param_SkinSystem_desc_other_txt");
                            content += "，";
                        }
                        if (sel_data(dt, "param_skeleton") == "正常")
                            content += "骨骼系統：" + sel_data(dt, "param_skeleton");
                        else
                        {
                            string BoneSystem = sel_data(dt, "param_skeleton_Desc").Replace(',', '、');
                            if (BoneSystem.IndexOf("骨折") != -1)
                                BoneSystem = BoneSystem.Replace("骨折", "骨折部位：" + sel_data(dt, "param_skeleton_Desc_fracture").Replace(',', '、'));
                            if (BoneSystem.IndexOf("脫臼") != -1)
                                BoneSystem = BoneSystem.Replace("脫臼", "脫臼部位：" + sel_data(dt, "param_skeleton_Desc_dislocation").Replace(',', '、'));
                            if (BoneSystem.IndexOf("關節腫") != -1)
                                BoneSystem = BoneSystem.Replace("關節腫", "關節腫部位：" + sel_data(dt, "param_skeleton_Desc_arthrosis").Replace(',', '、'));
                            if (BoneSystem.IndexOf("截肢") != -1)
                                BoneSystem = BoneSystem.Replace("截肢", "截肢部位：" + sel_data(dt, "param_skeleton_Desc_amputation").Replace(',', '、'));
                            if (BoneSystem.IndexOf("畸形") != -1)
                                BoneSystem = BoneSystem.Replace("畸形", "畸形部位：" + sel_data(dt, "param_skeleton_Desc_deformity").Replace(',', '、'));
                            if (BoneSystem.IndexOf("其他") != -1)
                                BoneSystem = BoneSystem.Replace("其他", "其他：" + sel_data(dt, "param_skeleton_Desc_other"));
                            content += "骨骼系統：" + BoneSystem;

                        }
                        content += "，疼痛強度：" + sel_data(dt, "param_pain_assessment_assess") + " " + ps_value + "分";
                        if (sel_data(dt, "other_desc_txt") != "")
                            content += "、其他：" + sel_data(dt, "other_desc_txt");
                        content += "。";
                        #endregion
                    }
                    else if (na_type == "B")//新生兒
                    {
                        if ((sel_data(dt, "Born_day") != "" && sel_data(dt, "Born__time") != "") && base.switchAssessmentInto == "Y")
                        {
                            string date = sel_data(dt, "Born_day") + " " + sel_data(dt, "Born__time");
                            DateTime time = Convert.ToDateTime(date);
                            for (int i = 1; i < 4; i++)
                            {
                                time = Convert.ToDateTime(date).AddMinutes(i * 60);

                                insertDataList.Clear();
                                insertDataList.Add(new DBItem("IID", base.creatid("OBS", userno, feeno, i.ToString()), DBItem.DBDataType.String));
                                insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                                insertDataList.Add(new DBItem("RECORDTIME", time.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));

                                obs_m.DBExecInsert("NIS_CHILD_BODY", insertDataList);
                            }
                        }
                        content = "個案於 " + sel_data(dt, "Born_day") + " " + sel_data(dt, "Born__time") + " 出生，因" + sel_data(dt, "param_ipd_reason").Replace("|", "，") + " ，予以身體評估，並給予家屬入院護理及環境介紹，並通知醫師前往診視。";
                    }

                    if (na_type != "ER")
                    {
                        Insert_CareRecord_Black(admittedTime.ToString("yyyy/MM/dd HH:mm:00"), TableID, "Admitted at " + sel_data(dt, "param_tube_time"), content, "", "", "", "");
                    }
                    else
                    {
                        Insert_CareRecord_Black(admittedTime.ToString("yyyy/MM/dd HH:mm:00"), TableID, "入院護理評估", content, "", "", "", "");
                    }
                }
                #region 拋轉VitalSign 疼痛
                else if (temp_count > 0 && mode != "temporary") //非第一次入評資料
                {
                    string vs_id = creatid("ASSTOVS", userno, feeno, "0");
                    string content_o = "", title = "";

                    int ps_value = 0;
                    if (sel_data(dt, "param_pain_assessment_assess") != "" && base.switchAssessmentInto == "Y")
                    {
                        string vs_part = sel_data(dt, "param_pain_assessment_assess");
                        string vs_record = sel_data(dt, "param_pain_assessment_record");
                        List<string> ps_content_list = new List<string>();
                        List<string> ps_item_list = new List<string>();

                        //疼痛計分
                        Regex rgx = new Regex(@"^([(]\d+[)])|^(\d+)");
                        foreach (string ps in vs_record.ToString().Split('|'))
                        {
                            if (ps != "")
                            {
                                ps_item_list.Add(ps);
                                ps_value += int.Parse(rgx.Match(ps).ToString().Replace("(", "").Replace(")", ""));
                            }
                        }
                        //拋轉  急診無住院時間資料，故使用NOW
                        if (na_type != "ER")
                        {
                            insert_vs("ps", vs_part, vs_record, sel_data(dt, "param_tube_date") + " " + sel_data(dt, "param_tube_time"), vs_id);
                        }
                        else
                        {
                            insert_vs("ps", vs_part, vs_record, DateTime.Now.ToString("yyyy/MM/dd HH:mm"), vs_id);
                        }
                        //////
                        if (ps_item_list.Count > 0)
                        {
                            string pain_val = "";
                            switch (sel_data(dt, "param_pain_assessment_assess"))
                            {
                                case "困難評估(成人)":
                                    pain_val = "呼吸：" + ps_item_list[0] + "、非言語表達：" + ps_item_list[1] + "、臉部表情：" + ps_item_list[2];
                                    pain_val += "肢體語言：" + ps_item_list[3] + "、安撫：" + ps_item_list[4];
                                    break;
                                case "困難評估(兒童)":
                                    pain_val = "臉部表情：" + ps_item_list[0] + "、腳部：" + ps_item_list[1] + "、活動力：" + ps_item_list[2];
                                    pain_val += "哭泣：" + ps_item_list[3] + "、安撫：" + ps_item_list[4];
                                    break;
                                case "困難評估(新生兒)":
                                    pain_val = "哭泣：" + ps_item_list[0] + "、需氧量；血氧飽合濃度高於95%：" + ps_item_list[1];
                                    pain_val += "、生命徵象：" + ps_item_list[2] + "表情：" + ps_item_list[3] + "、睡眠狀態：" + ps_item_list[4];
                                    break;
                                case "CPOT評估(加護單位)":
                                    pain_val = "臉部表情：" + ps_item_list[0] + "、身體動作：" + ps_item_list[1];
                                    pain_val += "、肌肉緊張：" + ps_item_list[2] + "呼吸器：" + ps_item_list[3];
                                    break;
                                default:
                                    break;
                            }
                            ps_content_list.Add("疼痛強度：" + sel_data(dt, "param_pain_assessment_assess"));
                            if (!string.IsNullOrEmpty(pain_val))
                            {
                                ps_content_list.Add("評估項目：" + pain_val);
                            }
                            ps_content_list.Add("總計：" + ps_value + "分");
                        }
                        if (ps_content_list.Count > 0)
                        {
                            content_o += string.Join("，", ps_content_list);
                            title = "疼痛評估";
                        }
                        //////
                        Insert_CareRecord(DateTime.Now.ToString("yyyy/MM/dd HH:mm"), vs_id, title, "", "", content_o, "", "", "ps");
                    }


                    //更新譫妄
                    #region 拋轉譫妄
                    //if (sel_data(dt, "param_total_delirium") != "")
                    //{
                    //    string delirium_date = sel_data(dt, "param_tube_date") + " " + sel_data(dt, "param_tube_time");
                    //    insertDataList.Clear();
                    //    insertDataList.Add(new DBItem("MODIFY_USER", userno, DBItem.DBDataType.String));
                    //    insertDataList.Add(new DBItem("MODIFY_USER_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    //    insertDataList.Add(new DBItem("MODIFY_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                    //    insertDataList.Add(new DBItem("STATUS", "M", DBItem.DBDataType.String));
                    //    string where = "TABLEID='" + TableID + "' AND STATUS = 'Y'";
                    //    erow = this.link.DBExecUpdate("DELIRIUM_DATA", insertDataList, where);
                    //    //base.Upd_CareRecord(date, id, "病友譫妄評估", "", "", "", "", "", "Mood");

                    //    insertDataList.Clear();
                    //    string id = creatid("DELIRIUM_DATA", userno, feeno, "0");
                    //    string[] del_obj_db = { "SCORE_1A", "SCORE_1B", "SCORE_2", "SCORE_3", "SCORE_4" };
                    //    string[] del_obj = { "rb_acute_attack_1a", "rb_acute_attack_1b", "rb_attention", "rb_ponder", "rb_consciousness" };
                    //    int total_score = 0;
                    //    for (int i = 0; i < del_obj_db.Length; i++)
                    //    {
                    //        string score = sel_data(dt, del_obj[i]);
                    //        if (score == "是")
                    //        {
                    //            score = "0";
                    //            total_score = total_score + 1;
                    //        }
                    //        else
                    //        {
                    //            score = "1";
                    //        }
                    //        insertDataList.Add(new DBItem(del_obj_db[i], score, DBItem.DBDataType.String));
                    //    }
                    //    insertDataList.Add(new DBItem("ASSESS_ID", id, DBItem.DBDataType.String));
                    //    insertDataList.Add(new DBItem("FEE_NO", feeno, DBItem.DBDataType.String));
                    //    insertDataList.Add(new DBItem("ASSESS_DT", delirium_date, DBItem.DBDataType.DataTime));
                    //    insertDataList.Add(new DBItem("CREATE_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    //    insertDataList.Add(new DBItem("CREATE_USER_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    //    insertDataList.Add(new DBItem("MODIFY_USER", userno, DBItem.DBDataType.String));
                    //    insertDataList.Add(new DBItem("MODIFY_USER_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    //    insertDataList.Add(new DBItem("MODIFY_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                    //    insertDataList.Add(new DBItem("RESULT", sel_data(dt, "param_total_delirium"), DBItem.DBDataType.String));
                    //    insertDataList.Add(new DBItem("DELIRIUM_RESULT", sel_data(dt, "param_result_delirium"), DBItem.DBDataType.String));
                    //    insertDataList.Add(new DBItem("SOURCE", "Assessment", DBItem.DBDataType.String));
                    //    insertDataList.Add(new DBItem("TOTAL_SCORE", total_score.ToString(), DBItem.DBDataType.String));
                    //    insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                    //    insertDataList.Add(new DBItem("CREATE_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                    //    insertDataList.Add(new DBItem("TABLEID", TableID, DBItem.DBDataType.String));
                    //    erow = ass_m.DBExecInsert("DELIRIUM_DATA", insertDataList);
                    //}
                    #endregion

                    #region 拋轉衰弱
                    //if (sel_data(dt, "param_total_score") != "")
                    //{
                    //    string cfs_date = sel_data(dt, "param_tube_date") + " " + sel_data(dt, "param_tube_time");

                    //    insertDataList.Clear();
                    //    string id = creatid("CFS_DATA", userno, feeno, "0");
                    //    string delirium_date = sel_data(dt, "param_tube_date") + " " + sel_data(dt, "param_tube_time");
                    //    insertDataList.Clear();
                    //    insertDataList.Add(new DBItem("MODIFY_USER", userno, DBItem.DBDataType.String));
                    //    insertDataList.Add(new DBItem("MODIFY_USER_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    //    insertDataList.Add(new DBItem("MODIFY_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                    //    insertDataList.Add(new DBItem("STATUS", "M", DBItem.DBDataType.String));
                    //    string where = "TABLEID='" + TableID + "' AND STATUS = 'Y'";
                    //    erow = this.link.DBExecUpdate("CFS_DATA", insertDataList, where);
                    //    //base.Upd_CareRecord(date, id, "病友譫妄評估", "", "", "", "", "", "Mood");

                    //    insertDataList.Clear();
                    //    insertDataList.Add(new DBItem("ASSESS_ID", id, DBItem.DBDataType.String));
                    //    insertDataList.Add(new DBItem("FEE_NO", feeno, DBItem.DBDataType.String));
                    //    insertDataList.Add(new DBItem("ASSESS_DT", cfs_date, DBItem.DBDataType.DataTime));
                    //    insertDataList.Add(new DBItem("CREATE_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    //    insertDataList.Add(new DBItem("CREATE_USER_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    //    insertDataList.Add(new DBItem("MODIFY_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    //    insertDataList.Add(new DBItem("MODIFY_USER_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    //    insertDataList.Add(new DBItem("MODIFY_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                    //    insertDataList.Add(new DBItem("SOURCE", "Assessment", DBItem.DBDataType.String));
                    //    insertDataList.Add(new DBItem("CFS_SCORE", sel_data(dt, "param_total_score"), DBItem.DBDataType.String));
                    //    insertDataList.Add(new DBItem("MC_SCORE", sel_data(dt, "param_total_cog"), DBItem.DBDataType.String));
                    //    insertDataList.Add(new DBItem("MC_SCORE1", sel_data(dt, "param_cog_1"), DBItem.DBDataType.String));
                    //    insertDataList.Add(new DBItem("MC_SCORE2", sel_data(dt, "param_cog_2"), DBItem.DBDataType.String));
                    //    insertDataList.Add(new DBItem("MC_SCORE3", sel_data(dt, "param_cog_3"), DBItem.DBDataType.String));
                    //    insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                    //    insertDataList.Add(new DBItem("TABLEID", TableID, DBItem.DBDataType.String));
                    //    if (sel_data(dt, "cog_ck_result") != null)
                    //    {
                    //        insertDataList.Add(new DBItem("COG_RESULT", sel_data(dt, "cog_ck_result"), DBItem.DBDataType.String));
                    //    }
                    //    insertDataList.Add(new DBItem("COG_RESULT_OTHER", sel_data(dt, "span_cog_Y_other"), DBItem.DBDataType.String));
                    //    erow = ass_m.DBExecInsert("CFS_DATA", insertDataList);

                    //if (clock_file[0] != null)
                    //{
                    //    link.DBCmd.CommandText = "";
                    //    link.DBCmd.Parameters.Clear();
                    //    link.DBCmd.CommandText = "UPDATE CFS_DATA SET  CLOCK_FILE = :CLOCK_FILE "
                    //                                       + " WHERE FEE_NO = '" + feeno + "' AND ASSESS_ID = '" + id + "' AND STATUS = 'Y' ";
                    //byte[] arr2 = ConvertFileToByte(clock_file[0]);
                    //link.DBCmd.Parameters.Add(":CLOCK_FILE", OracleDbType.Blob).Value = arr;
                    //    link.DBOpen();
                    //    link.DBCmd.ExecuteNonQuery();
                    //    link.DBClose();

                    //}
                    //}
                    #endregion

                    #region 拋轉TOCC(修改)
                    string TOCCid = creatid("TOCC_DATA", userno, feeno, "0");

                    var TOCCmsg = "";
                    //症狀
                    TOCCmsg += "病人目前";
                    if (sel_data(dt, "param_symptom") == "無" || sel_data(dt, "param_symptom") == "")
                    {
                        TOCCmsg += "無症狀。";
                    }
                    else
                    {
                        var transsymptom = sel_data(dt, "param_symptom").Replace(',', '、');
                        string symptomOther = "";
                        if (sel_data(dt, "param_symptom_other").ToString() != "")
                        {
                            symptomOther += sel_data(dt, "param_symptom_other").ToString();
                            transsymptom = transsymptom.Replace("其他", symptomOther);
                        }

                        TOCCmsg += transsymptom;
                        TOCCmsg += "。";
                    }
                    //旅遊史(Travel)
                    TOCCmsg += "旅遊史(Travel)：最近14日內";

                    if (sel_data(dt, "param_travel") == "最近 14 日內無國內、外旅遊")
                    {
                        TOCCmsg += "無國內、外旅遊。";

                    }
                    else
                    {
                        var test = sel_data(dt, "param_travel");
                        string travel = sel_data(dt, "param_travel").ToString();
                        var travleArr = travel.Split(',');
                        for (int i = 0; i < travleArr.Count(); i++)
                        {
                            switch (travleArr[i])
                            {
                                case "最近 14 日內國內旅遊":
                                    TOCCmsg += "國內旅遊";
                                    TOCCmsg += "(";
                                    if (sel_data(dt, "param_travel_domestic_city") != "")
                                    {
                                        TOCCmsg += sel_data(dt, "param_travel_domestic_city");
                                    }
                                    if (sel_data(dt, "param_travel_domestic_city") != "" && sel_data(dt, "param_travel_domestic_viewpoint") != "")
                                    {
                                        TOCCmsg += "，";
                                    }
                                    if (sel_data(dt, "param_travel_domestic_viewpoint") != "")
                                    {
                                        TOCCmsg += sel_data(dt, "param_travel_domestic_viewpoint");
                                    }
                                    if (sel_data(dt, "param_travel_domestic_city") != "" || sel_data(dt, "param_travel_domestic_viewpoint") != "")
                                    {
                                        TOCCmsg += "，";
                                    }
                                    string traffic = "交通方式：" + sel_data(dt, "param_travel_domestic_traffic");
                                    string trafficOther = "";
                                    if (sel_data(dt, "param_travel_domestic_traffic_other") != "")
                                    {
                                        trafficOther += sel_data(dt, "param_travel_domestic_traffic_other");
                                    }
                                    traffic = traffic.Replace("其他", trafficOther);
                                    TOCCmsg += traffic;
                                    TOCCmsg += ")";

                                    if (travleArr.Count() > 1)
                                    {
                                        TOCCmsg += "、";
                                    }
                                    break;
                                case "最近 14 日內國外旅遊(包含轉機或船舶停靠曾到訪)":
                                    TOCCmsg += "國外旅遊(包含轉機或船舶停靠曾到訪)";
                                    TOCCmsg += "(";
                                    if (sel_data(dt, "param_travel_aboard_country") != "")
                                    {
                                        TOCCmsg += sel_data(dt, "param_travel_aboard_country");
                                    }
                                    if (sel_data(dt, "param_travel_aboard_country") != "" && sel_data(dt, "param_travel_aboard_destination") != "")
                                    {
                                        TOCCmsg += "，";
                                    }
                                    if (sel_data(dt, "param_travel_aboard_destination") != "")
                                    {
                                        TOCCmsg += sel_data(dt, "param_travel_aboard_destination");
                                    }
                                    if (sel_data(dt, "param_travel_aboard_country") != "" || sel_data(dt, "param_travel_aboard_destination") != "")
                                    {
                                        TOCCmsg += "，";
                                    }
                                    string trafficAboard = "交通方式：" + sel_data(dt, "param_travel_aboard_traffic");
                                    string trafficAboardOther = "";
                                    if (sel_data(dt, "param_travel_aboard_traffic_other") != "")
                                    {
                                        trafficAboardOther += sel_data(dt, "param_travel_aboard_traffic_other");
                                        trafficAboard = trafficAboard.Replace("其他", trafficAboardOther);
                                    }
                                    TOCCmsg += trafficAboard;
                                    TOCCmsg += ")";
                                    break;
                            }

                        }
                        TOCCmsg += "。";

                    }
                    //職業別
                    string occupation = "";
                    string occupation_other = "";

                    occupation += "職業別(Occupation)：";
                    occupation += sel_data(dt, "param_occupation").ToString();
                    if (sel_data(dt, "param_occupation_other") != "")
                    {
                        occupation_other += sel_data(dt, "param_occupation_other");
                        occupation = occupation.Replace("其他", occupation_other);
                    }
                    TOCCmsg += occupation + "。";

                    //接觸史
                    TOCCmsg += "接觸史(Contact)：";

                    if (sel_data(dt, "param_contact") == "無")
                    {
                        TOCCmsg += "無。";
                    }
                    else
                    {
                        string contact = sel_data(dt, "param_contact").ToString();
                        var contactArr = contact.Split(',');
                        for (int i = 0; i < contactArr.Count(); i++)
                        {
                            switch (contactArr[i])
                            {
                                case "接觸禽鳥類、畜類等":
                                    TOCCmsg += "接觸禽鳥類、畜類等 : ";
                                    if (sel_data(dt, "param_contact_birds") != "")
                                    {
                                        TOCCmsg += "(" + sel_data(dt, "param_contact_birds") + ")";
                                    }
                                    TOCCmsg += "。";
                                    break;
                                case "孕/產婦接觸史":
                                    TOCCmsg += "孕/產婦接觸史：";
                                    if (sel_data(dt, "param_contact_obstetrics_symptom") != "")
                                    {
                                        TOCCmsg += "(1) 生產前 14 天內，同住家人有發燒、腹瀉、咳嗽、流鼻水等疑似感染症狀 : " + sel_data(dt, "param_contact_obstetrics_symptom") + "。";
                                    }
                                    if (sel_data(dt, "param_contact_obstetrics_sickleave") != "")
                                    {
                                        TOCCmsg += "(2) 生產前 14 天內，同住家人學校班上同學有因為傳染病請假或班級停課之情形 : " + sel_data(dt, "param_contact_obstetrics_sickleave") + "。";
                                    }
                                    if (sel_data(dt, "param_contact_obstetrics_symptomcaregiver") != "")
                                    {
                                        TOCCmsg += "(3) 住院期間照顧者(應盡量維持同一人)，目前有發燒、腹瀉、咳嗽、流鼻水等疑似感染症狀 : " + sel_data(dt, "param_contact_obstetrics_symptomcaregiver") + "。";
                                    }
                                    break;
                                case "其他":
                                    if (sel_data(dt, "param_contact_other") != "")
                                    {
                                        TOCCmsg += sel_data(dt, "param_contact_other") + "。";
                                    }
                                    break;
                            }
                        }
                    }
                    //群聚史(Cluster)
                    TOCCmsg += "群聚史(Cluster)：";
                    if (sel_data(dt, "param_cluster") != "")
                    {
                        if (sel_data(dt, "param_cluster") == "無")
                        {
                            TOCCmsg += "無。";
                        }
                        else
                        {
                            TOCCmsg += "家人/朋友/同事有發燒或類流感症狀 : ";
                            if (sel_data(dt, "param_cluster_relatives") != "")
                            {
                                var transcluster = sel_data(dt, "param_cluster_relatives").Replace(',', '、');
                                var clusterOther = "";
                                if (sel_data(dt, "param_cluster_relatives_other") != "")
                                {
                                    clusterOther += sel_data(dt, "param_cluster_relatives_other");
                                    transcluster = transcluster.Replace("其他", clusterOther);
                                }
                                TOCCmsg += transcluster;

                            }
                            TOCCmsg += "。";
                        }
                    }

                    if (TOCCmsg != "")
                    {
                        List<string> CID = base.Get_CareRecordMapper_Pid(TableID, "TOCC");
                        if (CID.Count == 0)
                        {
                            erow += base.Insert_CareRecord(DateTime.Now.ToString("yyyy/MM/dd HH:mm:05"), TOCCid, "TOCC評估", TOCCmsg, "", "", "", "", "TOCC");
                            base.Insert_CareRecordMapper(DateTime.Now.ToString("yyyy/MM/dd HH:mm:05"), TOCCid, TableID, "TOCC");
                        }
                        else
                        {
                            erow += base.Upd_CareRecord(DateTime.Now.ToString("yyyy/MM/dd HH:mm:05"), CID.First(), "TOCC評估", TOCCmsg, "", "", "", "", "TOCC");
                        }
                    }

                    #endregion
                }
                #endregion

                #endregion
                var result = sel_data(dt, "param_result_delirium");
                if (sel_data(dt, "param_result_delirium") == "Y")
                {
                    hasDelirium = 1;
                }
                if (erow > 0)
                {
                    if (mode == "insert")
                    {
                        if(hasDelirium == 1)
                        {
                            Response.Write("<script> if(confirm('評估為有譫妄，確定要自動連結約束評估作業？?')) {window.location.href='../ConstraintsAssessment/ListNew'; } else {window.location.href='AssessmentList?natype=" + na_type + "';}</script>");
                        }
                        else
                        {
                            Response.Write("<script>alert('新增成功');window.location.href='AssessmentList?natype=" + na_type + "';</script>");
                        }
                    }
                    else
                    {
                        if (hasDelirium == 1)
                        {
                            Response.Write("<script> if(confirm('評估為有譫妄，確定要自動連結約束評估作業？?')) {window.location.href='../ConstraintsAssessment/ListNew'; } else {window.location.href='AssessmentList?natype=" + na_type + "';}</script>");
                        }
                        else
                        {
                            Response.Write("<script>alert('更新成功');window.location.href='AssessmentList?natype=" + na_type + "';</script>");
                        }
                    }
                }
                else
                {
                    if (mode == "insert")
                        Response.Write("<script>alert('新增失敗');window.location.href='AssessmentList?natype=" + na_type + "';</script>");
                    else
                        Response.Write("<script>alert('更新失敗');window.location.href='AssessmentList?natype=" + na_type + "';</script>");
                }

                if (mode == "insert" || (mode == "temporary" && Request.Form["tableid"].ToString() == ""))
                {
                    //新增主表
                    insertDataList.Clear();
                    insertDataList.Add(new DBItem("MODIFYUSER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("MODIFYTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("SIGN", "N", DBItem.DBDataType.String));
                    if (mode != "temporary")
                    {
                        insertDataList.Add(new DBItem("STATUS", mode, DBItem.DBDataType.String));
                        erow = ass_m.DBExecUpdate("ASSESSMENTMASTER", insertDataList, " TABLEID = '" + TableID + "' ");
                    }
                    else //if(mode == "temporary") 
                    {
                        erow = ass_m.DBExecUpdate("ASSESSMENTMASTER", insertDataList, " TABLEID = '" + TableID + "' ");
                    }
                }
                else if (mode == "update" || (mode == "temporary" && Request.Form["tableid"].ToString() != ""))
                {
                    TableID = Request.Form["tableid"].ToString().Trim();
                    //更新主表
                    insertDataList.Clear();
                    insertDataList.Add(new DBItem("MODIFYUSER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("MODIFYTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("SIGN", "N", DBItem.DBDataType.String));
                    if (mode != "temporary")
                    {
                        insertDataList.Add(new DBItem("STATUS", mode, DBItem.DBDataType.String));
                        erow = ass_m.DBExecUpdate("ASSESSMENTMASTER", insertDataList, " TABLEID = '" + TableID + "' ");
                    }
                    else //if(mode == "temporary") 
                    {
                        erow = ass_m.DBExecUpdate("ASSESSMENTMASTER", insertDataList, " TABLEID = '" + TableID + "' ");
                    }
                }
            }
            catch (Exception ex)
            {
                this.log.saveLogMsg(ex.Message.ToString() + "入評來源：" + na_type, "SaveAssessment");
            }

            return new EmptyResult();
        }
        public bool Assessment_XML(string source, string na_type, string TableID, DataTable dtxml = null, string obs_xml = "")
        {
            try
            {
                //取得應簽章人員
                byte[] listByteCode = webService.UserName(userinfo.Guider);
                string listJsonArray = CompressTool.DecompressString(listByteCode);
                UserInfo user_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                DateTime NowTime = DateTime.Now;
                string RecordTime = NowTime.ToString("yyyy/MM/dd HH:mm:ss");
                string Temp_NowTime_Str = Convert.ToDateTime(RecordTime).ToString("yyyyMMddHHmmss");//時間採統一變數
                if (source == "AdmissionAssessment")
                {
                    #region 成人 兒童 急診入評簽章
                    if (na_type == "A")
                    {
                        string TempXml = Composition_Xml_A(dtxml);
                        string EmrXmlString = this.get_xml(
                            NowTime.ToString("yyyyMMddHHmmss.fffffff"), Temp_NowTime_Str + GetMd5Hash(TableID), "A000034", Convert.ToDateTime(dtxml.Rows[0]["CREATETIME"]).ToString("yyyyMMdd"),
                            GetMd5Hash(TableID), Convert.ToDateTime(dtxml.Rows[0]["CREATETIME"]).ToString("yyyyMMdd"), "", "",
                            user_info.EmployeesNo, user_info.EmployeesName, user_info.UserID, ptinfo.ChartNo, ptinfo.PatientName,
                            ptinfo.PatientID, ptinfo.PayInfo,
                            "C:\\EMR\\", "A000034" + GetMd5Hash(TableID) + Temp_NowTime_Str + ".xml", listJsonArray, "SaveAssessment_A"
                            );
                        SaveEMRLogData(TableID, GetMd5Hash(TableID), "EMR", RecordTime, "A000034" + GetMd5Hash(TableID) + Temp_NowTime_Str, TempXml);
                        SaveEMRLogData(TableID, GetMd5Hash(TableID), "Temp", RecordTime, Temp_NowTime_Str + "-" + GetMd5Hash(TableID), EmrXmlString);
                    }
                    else if (na_type == "C")
                    {
                        string TempXml = Composition_Xml_C(dtxml);
                        string EmrXmlString = this.get_xml(
                            NowTime.ToString("yyyyMMddHHmmss.fffffff"), Temp_NowTime_Str + GetMd5Hash(TableID), "A000011", Convert.ToDateTime(dtxml.Rows[0]["CREATETIME"]).ToString("yyyyMMdd"),
                            GetMd5Hash(TableID), Convert.ToDateTime(dtxml.Rows[0]["CREATETIME"]).ToString("yyyyMMdd"), "", "",
                            user_info.EmployeesNo, user_info.EmployeesName, user_info.UserID, ptinfo.ChartNo, ptinfo.PatientName,
                            ptinfo.PatientID, ptinfo.PayInfo,
                            "C:\\EMR\\", "A000011" + GetMd5Hash(TableID) + Temp_NowTime_Str + ".xml", listJsonArray, "SaveAssessment_C"
                            );
                        SaveEMRLogData(TableID, GetMd5Hash(TableID), "EMR", RecordTime, "A000011" + GetMd5Hash(TableID) + Temp_NowTime_Str, TempXml);
                        SaveEMRLogData(TableID, GetMd5Hash(TableID), "Temp", RecordTime, Temp_NowTime_Str + "-" + GetMd5Hash(TableID), EmrXmlString);
                    }
                    else if (na_type == "ER")
                    {
                        #region 急診入評電子簽章
                        string xml = string.Empty;
                        xml += "<?xml version='1.0' encoding='UTF-8'?>";
                        xml += "<Document>";
                        #region 病患資訊
                        string allergyDesc = string.Empty;
                        byte[] allergen = webService.GetAllergyList(ptinfo.FeeNo);
                        string ptJsonArr = string.Empty;
                        if (allergen != null)
                        {
                            ptJsonArr = CompressTool.DecompressString(allergen);
                        }

                        List<NIS.Data.PatientInfo> patList = JsonConvert.DeserializeObject<List<NIS.Data.PatientInfo>>(ptJsonArr);
                        if (allergen != null)
                        {
                            allergyDesc = patList[0].AllergyDesc;
                        }
                        xml += "<TING_PT_NAME>" + ptinfo.PatientName + "</TING_PT_NAME>";
                        xml += "<TING_CHART_NO>" + ptinfo.ChartNo + "</TING_CHART_NO>";
                        xml += "<TING_SEX>" + ptinfo.PatientGender + "</TING_SEX>";
                        xml += "<TING_AGE>" + ptinfo.Age + "</TING_AGE>";
                        xml += "<TING_BED_NO>" + ptinfo.BedNo + "</TING_BED_NO>";
                        xml += "<TING_ADMIT_DATE>" + ptinfo.InDate.ToString("yyyyMMdd") + "</TING_ADMIT_DATE>";
                        xml += "<TING_ADMIT_TIME>" + ptinfo.InDate.ToString("HHmm") + "</TING_ADMIT_TIME>";
                        xml += "<TING_ALLERGEN>" + allergyDesc + "</TING_ALLERGEN>";
                        #endregion
                        #region 身體評估                        
                        xml += "<GCS>COMA SCALE：E：" + sel_data(dtxml, "gc_r1").Substring(1, 1) + "、V：" + sel_data(dtxml, "gc_r2").Substring(1, 1) + "、M：" + sel_data(dtxml, "gc_r3").Substring(1, 1) + "</GCS>";
                        xml += "<PT_ACTION>一般外觀：" + sel_data(dtxml, "pt_action").Replace(',', '、') + "</PT_ACTION>";
                        string conscious = string.Empty;
                        if (sel_data(dtxml, "param_vs_conscious") == "清醒")
                        {
                            conscious = "意識評估：" + sel_data(dtxml, "param_vs_conscious");
                        }
                        else
                        {
                            conscious = "意識評估：" + sel_data(dtxml, "param_vs_conscious_item_other").Replace(',', '、');
                        }
                        xml += "<PARAM_VS_CONSCIOUS>" + conscious + "</PARAM_VS_CONSCIOUS>";
                        string MentalCondition = string.Empty;
                        if (sel_data(dtxml, "param_MentalCondition") == "正常")
                            MentalCondition = sel_data(dtxml, "param_MentalCondition") + "，";
                        else
                        {
                            MentalCondition = sel_data(dtxml, "param_MentalCondition_desc_item_other").Replace(',', '、');
                            if (sel_data(dtxml, "param_MentalCondition_desc_other_txt") != "")
                                MentalCondition += "：" + sel_data(dtxml, "param_MentalCondition_desc_other_txt");
                        }
                        xml += "<PARAM_MENTALCONDITION>精神狀態：" + MentalCondition + "</PARAM_MENTALCONDITION>";
                        string NervousSystem = string.Empty;
                        if (sel_data(dtxml, "param_NervousSystem") == "正常")
                            NervousSystem = sel_data(dtxml, "param_NervousSystem");
                        else
                        {
                            string NervousSystemResult = sel_data(dtxml, "param_Nervous_desc_item_other").Replace(',', '、');
                            if (NervousSystemResult.IndexOf("抽搐") != -1)
                                NervousSystemResult = NervousSystemResult.Replace("抽搐", "抽搐部位：" + sel_data(dtxml, "twitch_part_txt").Replace(',', '、'));
                            if (NervousSystemResult.IndexOf("痛") != -1)
                                NervousSystemResult = NervousSystemResult.Replace("痛", "痛部位：" + sel_data(dtxml, "pain_part_txt").Replace(',', '、'));
                            if (NervousSystemResult.IndexOf("麻") != -1)
                                NervousSystemResult = NervousSystemResult.Replace("麻", "麻部位：" + sel_data(dtxml, "hpan_part_txt").Replace(',', '、'));
                            if (NervousSystemResult.IndexOf("肢體障礙") != -1)
                                NervousSystemResult = NervousSystemResult.Replace("肢體障礙", "肢體障礙：" + sel_data(dtxml, "LimbDisorders_part").Replace(',', '、'));
                            if (NervousSystemResult.IndexOf("其他") != -1)
                                NervousSystemResult = NervousSystemResult.Replace("其他", "其他：" + sel_data(dtxml, "param_NervousSystem_desc_other_txt"));
                            NervousSystem = NervousSystemResult;
                        }
                        xml += "<PARAM_NERVOUSSYSTEM>神經糸統：" + NervousSystem + "</PARAM_NERVOUSSYSTEM>";
                        string CardiovascularSystem = string.Empty;
                        if (sel_data(dtxml, "param_CardiovascularSystem") == "正常")
                            CardiovascularSystem = sel_data(dtxml, "param_CardiovascularSystem") + "，";
                        else
                        {
                            CardiovascularSystem = sel_data(dtxml, "param_Cardiovascular_desc_item_other").Replace(',', '、');
                            if (sel_data(dtxml, "param_Cardiovascular_desc_other_txt") != "")
                                CardiovascularSystem += "：" + sel_data(dtxml, "param_Cardiovascular_desc_other_txt");
                        }
                        xml += "<PARAM_CARDIOVASCULARSYSTEM>心臟血管系統：" + CardiovascularSystem + "</PARAM_CARDIOVASCULARSYSTEM>";
                        string param_RespiratoryTract, param_RespiratoryRateAndPattern, param_RespiratorySound;
                        if (sel_data(dtxml, "param_RespiratoryTract") == "通暢")
                            param_RespiratoryTract = "呼吸道：" + sel_data(dtxml, "param_RespiratoryTract") + "、";
                        else
                        {
                            string RespiratoryTractResult = sel_data(dtxml, "param_RespiratoryTract_desc_item_other").Replace(',', '、');
                            if (RespiratoryTractResult.IndexOf("痰") != -1)
                                RespiratoryTractResult = RespiratoryTractResult.Replace("痰", "痰：量" + sel_data(dtxml, "sputum_amount") + "，顏色" + sel_data(dtxml, "sputum_color"));
                            param_RespiratoryTract = "呼吸道：" + RespiratoryTractResult;
                            if (sel_data(dtxml, "param_RespiratoryTract_other_txt") != "")
                                param_RespiratoryTract += "：" + sel_data(dtxml, "param_RespiratoryTract_other_txt");
                        }
                        if (sel_data(dtxml, "param_RespiratoryRateAndPattern") == "正常")
                            param_RespiratoryRateAndPattern = "呼吸速率：" + sel_data(dtxml, "param_RespiratoryRateAndPattern") + "、";
                        else
                        {
                            param_RespiratoryRateAndPattern = "呼吸速率：" + sel_data(dtxml, "param_RespiratoryRateAndPattern_desc_item_other").Replace(',', '、');
                            if (sel_data(dtxml, "param_RespiratoryRateAndPattern_other_txt") != "")
                                param_RespiratoryRateAndPattern += "：" + sel_data(dtxml, "param_RespiratoryRateAndPattern_other_txt");
                        }
                        if (sel_data(dtxml, "param_RespiratorySound") == "正常")
                            param_RespiratorySound = "呼吸音：" + sel_data(dtxml, "param_RespiratorySound") + "，";
                        else
                        {
                            param_RespiratorySound = "呼吸音：" + sel_data(dtxml, "param_RespiratorySound_desc_item_other").Replace(',', '、');
                            if (sel_data(dtxml, "param_RespiratorySound_other_txt") != "")
                                param_RespiratorySound += "：" + sel_data(dtxml, "param_RespiratorySound_other_txt");
                        }

                        xml += "<RESPIRATORY_SYSTEM>呼吸系統：" + param_RespiratoryTract + "、" + param_RespiratoryRateAndPattern + "、" + param_RespiratorySound + "</RESPIRATORY_SYSTEM>";
                        string GASTROINTESTINALSYSTEM = string.Empty;
                        if (sel_data(dtxml, "param_GastrointestinalSystem") == "正常")
                            GASTROINTESTINALSYSTEM = "腸胃系統：" + sel_data(dtxml, "param_GastrointestinalSystem") + "，";
                        else
                        {
                            GASTROINTESTINALSYSTEM = "腸胃系統：" + sel_data(dtxml, "param_GastrointestinalSystem_desc_item_other").Replace(',', '、');
                            if (sel_data(dtxml, "param_GastrointestinalSystem_desc_other_txt") != "")
                                GASTROINTESTINALSYSTEM += "：" + sel_data(dtxml, "param_GastrointestinalSystem_desc_other_txt");
                        }
                        xml += "<PARAM_GASTROINTESTINALSYSTEM>" + GASTROINTESTINALSYSTEM + "</PARAM_GASTROINTESTINALSYSTEM>";
                        string URINARYSYSTEM = string.Empty;
                        if (sel_data(dtxml, "param_UrinarySystem") == "正常")
                            URINARYSYSTEM = "泌尿系統：" + sel_data(dtxml, "param_UrinarySystem") + "，";
                        else
                        {
                            URINARYSYSTEM = "泌尿系統：" + sel_data(dtxml, "param_UrinarySystem_desc_item_other").Replace(',', '、');
                            if (sel_data(dtxml, "param_UrinarySystem_desc_other_txt") != "")
                                URINARYSYSTEM += "：" + sel_data(dtxml, "param_UrinarySystem_desc_other_txt");
                        }
                        xml += "<PARAM_URINARYSYSTEM>" + URINARYSYSTEM + "</PARAM_URINARYSYSTEM>";
                        string SKINSYSTEM = string.Empty;
                        if (sel_data(dtxml, "param_SkinSystem") == "正常")
                            SKINSYSTEM = "皮膚系統：" + sel_data(dtxml, "param_SkinSystem") + "，";
                        else
                        {
                            SKINSYSTEM = "皮膚系統：" + sel_data(dtxml, "param_SkinSystem_desc_item_other").Replace(',', '、');
                            if (sel_data(dtxml, "param_SkinSystem_desc_other_txt") != "")
                                SKINSYSTEM += "：" + sel_data(dtxml, "param_SkinSystem_desc_other_txt");
                        }
                        xml += "<PARAM_SKINSYSTEM>" + SKINSYSTEM + "</PARAM_SKINSYSTEM>";
                        string BONESYSTEM = string.Empty;
                        if (sel_data(dtxml, "param_skeleton") == "正常")
                            BONESYSTEM = "骨骼系統：" + sel_data(dtxml, "param_skeleton");
                        else
                        {
                            string BoneSystem = sel_data(dtxml, "param_skeleton_Desc").Replace(',', '、');
                            if (BoneSystem.IndexOf("骨折") != -1)
                                BoneSystem = BoneSystem.Replace("骨折", "骨折部位：" + sel_data(dtxml, "param_skeleton_Desc_fracture").Replace(',', '、'));
                            if (BoneSystem.IndexOf("脫臼") != -1)
                                BoneSystem = BoneSystem.Replace("脫臼", "脫臼部位：" + sel_data(dtxml, "param_skeleton_Desc_dislocation").Replace(',', '、'));
                            if (BoneSystem.IndexOf("關節腫") != -1)
                                BoneSystem = BoneSystem.Replace("關節腫", "關節腫部位：" + sel_data(dtxml, "param_skeleton_Desc_arthrosis").Replace(',', '、'));
                            if (BoneSystem.IndexOf("截肢") != -1)
                                BoneSystem = BoneSystem.Replace("截肢", "截肢部位：" + sel_data(dtxml, "param_skeleton_Desc_amputation").Replace(',', '、'));
                            if (BoneSystem.IndexOf("畸形") != -1)
                                BoneSystem = BoneSystem.Replace("畸形", "畸形部位：" + sel_data(dtxml, "param_skeleton_Desc_deformity").Replace(',', '、'));
                            if (BoneSystem.IndexOf("其他") != -1)
                                BoneSystem = BoneSystem.Replace("其他", "其他：" + sel_data(dtxml, "param_skeleton_Desc_other"));
                            BONESYSTEM = "骨骼系統：" + BoneSystem;
                        }
                        xml += "<PARAM_BONESYSTEM>" + BONESYSTEM + "</PARAM_BONESYSTEM>";
                        xml += "<OTHER>其他：" + sel_data(dtxml, "other_desc_txt") + "</OTHER>";
                        #endregion
                        #region --傷口--
                        string[] wound_type = sel_data(dtxml, "ddl_wound_type").Split(',');
                        string[] wound_general = sel_data(dtxml, "ddl_wound_general").Split(',');
                        string[] wound_scald = sel_data(dtxml, "ddl_wound_scald").Split(',');//燙傷
                        string[] wound_date = sel_data(dtxml, "wound_date").Split(',');//日期
                        string[] wound_date_unknown = sel_data(dtxml, "wound_date_unknown").Split(',');//不確定的checkbox
                        xml += "<param_General_wound>一般傷口:" + sel_data(dtxml, "param_General_wound") + "</param_General_wound>";
                        xml += "<wound_type>";
                        if (sel_data(dtxml, "param_General_wound") == "有")
                        {
                            for (int i = 0; i < wound_type.Length; i++)
                            {
                                xml += "傷口種類:";
                                xml += wound_type[i];
                                if (wound_type[i] == "燙傷")
                                {
                                    xml += ";部位:" + wound_scald[i] + ";";
                                }
                                else
                                {
                                    xml += ";部位:" + wound_general[i] + ";";
                                }
                                //加日期 --先住解~~撈不到值
                                xml += "發生日期:";
                                string Temp_wound_date = (wound_date[i] == "") ? "不詳" : wound_date[i];
                                xml += Temp_wound_date;
                                xml += ";";
                            }
                            xml = xml.TrimEnd(';');
                        }
                        xml += "</wound_type>";
                        /*----------------------壓傷傷口---------------*/
                        // string[] Temp_place = sel_data(dt, "place").Split(',');
                        string[] Temp_inHosp = sel_data(dtxml, "inHosp").Split(',');
                        string[] Temp_outHosp = sel_data(dtxml, "outHosp").Split(',');
                        string[] Temp_outHosp_other = sel_data(dtxml, "outHosp_other").Split(',');
                        string[] Temp_wound_pressure = sel_data(dtxml, "wound_pressure").Split(',');
                        string[] Temp_wound_pre_other_txt = sel_data(dtxml, "wound_pre_other_txt").Split(',');
                        string[] wound_pre_date = sel_data(dtxml, "wound_pre_date").Split(',');
                        string[] wound_pre_date_unknown = sel_data(dtxml, "wound_pre_date_unknown").Split(',');
                        xml += "<param_pressure>壓傷傷口:" + sel_data(dtxml, "param_pressure") + "</param_pressure>";
                        xml += "<place>";
                        if (sel_data(dtxml, "param_pressure") == "有")
                        {
                            for (int i = 0; i < Temp_wound_pre_other_txt.Length; i++)
                            {
                                string Temp_place = "";
                                if (i == 0)
                                {
                                    Temp_place = sel_data(dtxml, "place");
                                }
                                else
                                {
                                    Temp_place = sel_data(dtxml, "place_" + i);
                                }
                                xml += "傷口種類:發生地點:";
                                if (Temp_place == "1")
                                {
                                    xml += "院內:" + Temp_inHosp[i].Trim();
                                }
                                else
                                {
                                    if (Temp_outHosp[i] == "其他醫院")
                                    {
                                        xml += "院外:其他醫院:" + Temp_outHosp_other[i];
                                    }
                                    else if (Temp_outHosp[i] == "長期養護機構")
                                    {
                                        xml += "院外:長期養護機構:" + Temp_outHosp_other[i];
                                    }
                                    else
                                    {
                                        xml += "院外:" + Temp_outHosp[i];
                                    }
                                }
                                xml += ";";
                                xml += "部位:";
                                if (Temp_wound_pressure[i] == "其他")
                                {
                                    xml += "其他" + Temp_wound_pre_other_txt[i];
                                }
                                else
                                {
                                    xml += Temp_wound_pressure[i];
                                }
                                xml += ";";
                                //加日期 --先住解~~撈不到值
                                xml += "發生日期:";
                                string Temp_wound_pre_date = (wound_pre_date[i] == "") ? "不詳" : wound_pre_date[i];
                                xml += Temp_wound_pre_date;
                                xml += ";";
                            }
                            xml = xml.TrimEnd(';');
                        }
                        xml += "</place>";
                        #endregion
                        #region --疼痛評估--
                        xml += "<param_pain_assessment_assess>疼痛評估工具:" + sel_data(dtxml, "param_pain_assessment_assess") + "</param_pain_assessment_assess>";
                        xml += "<param_pain_assessment_record>疼痛項目:" + sel_data(dtxml, "param_pain_assessment_record") + "</param_pain_assessment_record>";
                        #region 拋轉VitalSign 疼痛
                        int ps_value = 0;
                        if (sel_data(dtxml, "param_pain_assessment_assess") != "" && base.switchAssessmentInto == "Y")
                        {
                            string vs_record = sel_data(dtxml, "param_pain_assessment_record");
                            //疼痛計分
                            Regex rgx = new Regex(@"^([(]\d+[)])|^(\d+)");
                            foreach (string ps in vs_record.ToString().Split('|'))
                            {
                                if (ps != "")
                                {
                                    ps_value += int.Parse(rgx.Match(ps).ToString().Replace("(", "").Replace(")", ""));
                                }
                            }
                        }
                        #endregion
                        xml += "<param_pain_assessment_level>疼痛總分:" + ps_value + "</param_pain_assessment_level>";
                        #endregion
                        xml += "</Document>";
                        string EmrXmlString = this.get_xml(
                            NowTime.ToString("yyyyMMddHHmmss.fffffff"), Temp_NowTime_Str + GetMd5Hash(TableID), "A010008", Convert.ToDateTime(dtxml.Rows[0]["CREATETIME"]).ToString("yyyyMMdd"),
                            GetMd5Hash(TableID), Convert.ToDateTime(dtxml.Rows[0]["CREATETIME"]).ToString("yyyyMMdd"), "", "",
                            user_info.EmployeesNo, user_info.EmployeesName, user_info.UserID, ptinfo.ChartNo, ptinfo.PatientName,
                            ptinfo.PatientID, ptinfo.PayInfo,
                            "C:\\EMR\\", "A010008" + GetMd5Hash(TableID) + Temp_NowTime_Str + ".xml", listJsonArray, "SaveAssessment_ER"
                            );
                        SaveEMRLogData(TableID, GetMd5Hash(TableID), "EMR", RecordTime, "A010008" + GetMd5Hash(TableID) + Temp_NowTime_Str, xml);
                        SaveEMRLogData(TableID, GetMd5Hash(TableID), "Temp", RecordTime, Temp_NowTime_Str + "-" + GetMd5Hash(TableID), EmrXmlString);
                        #endregion
                    }
                    #endregion
                    return true;
                }
                else if (source == "OBS")
                {
                    if (na_type == "N")//newborn
                    {
                        string TempXml = obs_xml;
                        string EmrXmlString = this.get_xml(
                            NowTime.ToString("yyyyMMddHHmmss.fffffff"), Temp_NowTime_Str + GetMd5Hash(TableID), "A000034", Convert.ToDateTime(dtxml.Rows[0]["CREATETIME"]).ToString("yyyyMMdd"),
                            GetMd5Hash(TableID), Convert.ToDateTime(dtxml.Rows[0]["CREATETIME"]).ToString("yyyyMMdd"), "", "",
                            user_info.EmployeesNo, user_info.EmployeesName, user_info.UserID, ptinfo.ChartNo, ptinfo.PatientName,
                            ptinfo.PatientID, ptinfo.PayInfo,
                            "C:\\EMR\\", "A000034" + GetMd5Hash(TableID) + Temp_NowTime_Str + ".xml", listJsonArray, "SaveAssessment_N"
                            );
                        SaveEMRLogData(TableID, GetMd5Hash(TableID), "EMR", RecordTime, "A000034" + GetMd5Hash(TableID) + Temp_NowTime_Str, TempXml);
                        SaveEMRLogData(TableID, GetMd5Hash(TableID), "Temp", RecordTime, Temp_NowTime_Str + "-" + GetMd5Hash(TableID), EmrXmlString);
                    }
                    else if (na_type == "B")//baby
                    {
                        string TempXml = obs_xml;
                        string EmrXmlString = this.get_xml(
                            NowTime.ToString("yyyyMMddHHmmss.fffffff"), Temp_NowTime_Str + GetMd5Hash(TableID), "A000034", Convert.ToDateTime(dtxml.Rows[0]["CREATETIME"]).ToString("yyyyMMdd"),
                            GetMd5Hash(TableID), Convert.ToDateTime(dtxml.Rows[0]["CREATETIME"]).ToString("yyyyMMdd"), "", "",
                            user_info.EmployeesNo, user_info.EmployeesName, user_info.UserID, ptinfo.ChartNo, ptinfo.PatientName,
                            ptinfo.PatientID, ptinfo.PayInfo,
                            "C:\\EMR\\", "A000034" + GetMd5Hash(TableID) + Temp_NowTime_Str + ".xml", listJsonArray, "SaveAssessment_B"
                            );
                        SaveEMRLogData(TableID, GetMd5Hash(TableID), "EMR", RecordTime, "A000034" + GetMd5Hash(TableID) + Temp_NowTime_Str, TempXml);
                        SaveEMRLogData(TableID, GetMd5Hash(TableID), "Temp", RecordTime, Temp_NowTime_Str + "-" + GetMd5Hash(TableID), EmrXmlString);

                    }
                    return true;
                }
                else
                {
                    return false;
                }

            }
            catch (Exception ex)
            {
                this.log.saveLogMsg(ex.Message.ToString() + "入評來源：" + na_type, "Assessment_XML");
                return false;
            }

        }

        private void save_To_Pdf()
        {
            throw new NotImplementedException();
        }

        //刪除護理評估_靜態評估
        public ActionResult DelAssessment(string tableid, string natype)
        {
            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("DELETED", userinfo.EmployeesNo, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("STATUS", "delete", DBItem.DBDataType.String));
            int erow = ass_m.DBExecUpdate("ASSESSMENTMASTER", insertDataList, " TABLEID = '" + tableid + "' ");
            string StrSql = "";

            //刪除譫妄
            #region 刪除譫妄


            StrSql = "SELECT * FROM DELIRIUM_DATA WHERE TABLEID='" + tableid + "' AND STATUS = 'Y' ";
            DataTable dt = this.link.DBExecSQL(StrSql);

            if (dt.Rows.Count > 0)
            {
                insertDataList.Clear();
                insertDataList.Add(new DBItem("DEL_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("DEL_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("STATUS", "D", DBItem.DBDataType.String));
                string where = "TABLEID='" + tableid + "' AND STATUS = 'Y'";
                erow = this.link.DBExecUpdate("DELIRIUM_DATA", insertDataList, where);
                //base.Upd_CareRecord(date, id, "病友譫妄評估", "", "", "", "", "", "Mood");
            }
            #endregion
            //int erow = this.link.DBExecUpdate("ASSESSMENTMASTER", insertDataList, " TABLEID = '" + tableid + "' ");

            //刪除譫妄
            #region 刪除衰弱


            StrSql = "SELECT * FROM DELIRIUM_DATA WHERE TABLEID='" + tableid + "' AND STATUS = 'Y' ";
            DataTable dt_CFS = this.link.DBExecSQL(StrSql);

            if (dt_CFS.Rows.Count > 0)
            {
                insertDataList.Clear();
                insertDataList.Add(new DBItem("DEL_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("DEL_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("STATUS", "D", DBItem.DBDataType.String));
                string where = "TABLEID='" + tableid + "' AND STATUS = 'Y'";
                erow = this.link.DBExecUpdate("CFS_DATA", insertDataList, where);
                //base.Upd_CareRecord(date, id, "病友譫妄評估", "", "", "", "", "", "Mood");
            }
            #endregion
            //int erow = this.link.DBExecUpdate("ASSESSMENTMASTER", insertDataList, " TABLEID = '" + tableid + "' ");

            if (erow > 0)
            {
                //將紀錄回寫至 EMR Temp Table
                //20140923 mod by yungchen 因僅抓病歷號的單號與出院病摘重覆 故加上簽章類別039
                ////string sqlstr = "begin P_NIS_EMRMS('" + ptinfo.FeeNo + "','039','入院病人護理評估表','" + ptinfo.FeeNo + "039','','" + userinfo.EmployeesNo + "','D');end;";
                #region JAG 簽章刪除
                int result = del_emr(tableid, userinfo.EmployeesNo);
                #endregion
                ////ass_m.DBExec(sqlstr);
                Response.Write("<script>alert('刪除成功');window.location.href='AssessmentList?natype=" + natype + "';</script>");
            }
            else
                Response.Write("<script>alert('刪除失敗');window.location.href='AssessmentList?natype=" + natype + "';</script>");

            return new EmptyResult();
        }

        //退回暫存
        public ActionResult BackAssessment(string tableid, string natype)
        {
            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("STATUS", "temporary", DBItem.DBDataType.String));
            int erow = ass_m.DBExecUpdate("ASSESSMENTMASTER", insertDataList, " TABLEID = '" + tableid + "' ");
          
            if (erow > 0)
            {

                Response.Write("<script>alert('退回成功');window.location.href='AssessmentList?natype=" + natype + "';</script>");
            }
            else
                Response.Write("<script>alert('退回失敗');window.location.href='AssessmentList?natype=" + natype + "';</script>");

            return new EmptyResult();
        }



        #region 列印

        //急診入評列印
        public ActionResult AssessmentER_PDF(string tableid, string feeno)
        {
            set_viewbag_PDF(tableid, feeno, "ER");
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
                    Text = costlist[i].CCCDescription.Trim(),
                    Value = costlist[i].CCCDescription.Trim(),
                    Selected = false
                });
            }
            ViewData["costlist"] = cCostList;
            ViewBag.dt_w_type = SetDropDownList.GetddlItem("", ass_m.sel_type_list("wound_type", "assess"), false, 0, 1);//種類
            ViewBag.dt_w_g = SetDropDownList.GetddlItem("", ass_m.sel_type_list("wound_general", "assess"), false, 0, 1);//一般
            ViewBag.dt_w_s = SetDropDownList.GetddlItem("", ass_m.sel_type_list("wound_scald", "assess"), false, 0, 1);//燒傷
            ViewBag.dt_w_p = SetDropDownList.GetddlItem("", ass_m.sel_type_list("wound_pressure", "assess"), false, 0, 1);//壓傷
            ViewBag.dt_tube = SetDropDownList.GetddlItem("", ass_m.sel_tube_list(), false, 0, 1);//管路

            List<SysParamItem> PositionList = this.ap_eck.SelectSysParams("PainPosition", "Adult");//this.ap_eck.SelectSysParams("PainPosition", ((base.ptinfo.Age < 18) ? "Child" : "Adult"));
            string TempPositionStr = "";
            for (int j = 0; j < PositionList.Count; j++)
            {
                TempPositionStr += PositionList[j].p_name + "|";
            }
            ViewData["PositionList"] = TempPositionStr;

            return View();
        }
        //兒童入院評估_列印
        public ActionResult AssessmentChild_PDF(string tableid, string feeno)
        {
            set_viewbag_PDF(tableid, feeno, "C");
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
                    Text = costlist[i].CCCDescription.Trim(),
                    Value = costlist[i].CCCDescription.Trim(),
                    Selected = false
                });
            }
            ViewData["costlist"] = cCostList;
            ViewBag.dt_w_type = SetDropDownList.GetddlItem("", ass_m.sel_type_list("wound_type", "assess"), false, 0, 1);//種類
            ViewBag.dt_w_g = SetDropDownList.GetddlItem("", ass_m.sel_type_list("wound_general", "assess"), false, 0, 1);//一般
            ViewBag.dt_w_s = SetDropDownList.GetddlItem("", ass_m.sel_type_list("wound_scald", "assess"), false, 0, 1);//燒傷
            ViewBag.dt_w_p = SetDropDownList.GetddlItem("", ass_m.sel_type_list("wound_pressure", "assess"), false, 0, 1);//壓傷
            ViewBag.dt_tube = SetDropDownList.GetddlItem("", ass_m.sel_tube_list(), false, 0, 1);//管路
            //----- 2016/06/03 Vanda Add 
            //ViewData["tubeMaterial"] = this.cd.getSelectItem("tube", "tubeMaterial");//管路材質
            //ViewData["tubePosition"] = this.cd.getSelectItem("tube", "tubePosition");//位置
            //ViewData["tubeSection"] = this.cd.getSelectItem("assess", "tube_section");//部位
            ViewData["division"] = this.GetNanda();
            return View();
        }

        //成人評估_列印
        public ActionResult AssessmentAdult_PDF(string tableid, string feeno)
        {
            set_viewbag_PDF(tableid, feeno, "A");
            byte[] listByteCode = webService.GetCostCenterList();
            string listJsonArray = CompressTool.DecompressString(listByteCode);
            List<CostCenterList> costlist = JsonConvert.DeserializeObject<List<CostCenterList>>(listJsonArray);
            List<SelectListItem> cCostList = new List<SelectListItem>();
            DataTable dtClock = ass_m.sel_assessment_clock(tableid);

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
                    Text = costlist[i].CCCDescription.Trim(),
                    Value = costlist[i].CCCDescription.Trim(),
                    Selected = false
                });
            }
            ViewData["costlist"] = cCostList;
            ViewBag.dt_w_type = SetDropDownList.GetddlItem("", ass_m.sel_type_list("wound_type", "assess"), false, 0, 1);//種類
            ViewBag.dt_w_g = SetDropDownList.GetddlItem("", ass_m.sel_type_list("wound_general", "assess"), false, 0, 1);//一般
            ViewBag.dt_w_s = SetDropDownList.GetddlItem("", ass_m.sel_type_list("wound_scald", "assess"), false, 0, 1);//燒傷
            ViewBag.dt_w_p = SetDropDownList.GetddlItem("", ass_m.sel_type_list("wound_pressure", "assess"), false, 0, 1);//壓傷
            ViewBag.dt_tube = SetDropDownList.GetddlItem("", ass_m.sel_tube_list(), false, 0, 1);//管路
            ViewBag.clock = dtClock;

            List<SysParamItem> PositionList = this.ap_eck.SelectSysParams("PainPosition", "Adult");//this.ap_eck.SelectSysParams("PainPosition", ((base.ptinfo.Age < 18) ? "Child" : "Adult"));
            string TempPositionStr = "";
            for (int j = 0; j < PositionList.Count; j++)
            {
                TempPositionStr += PositionList[j].p_name + "|";
            }
            ViewData["PositionList"] = TempPositionStr;
            //----- 2016/06/03 Vanda Add 
            //ViewData["tubeMaterial"] = this.cd.getSelectItem("tube", "tubeMaterial");//管路材質
            //ViewData["tubePosition"] = this.cd.getSelectItem("tube", "tubePosition");//位置
            //ViewData["tubeSection"] = this.cd.getSelectItem("assess", "tube_section");//部位
            //ViewData["mode"] = mode;
            //-----

            return View();
        }

        //精神評估_列印
        public ActionResult AssessmentSpirit_PDF(string tableid, string feeno)
        {
            set_viewbag_PDF(tableid, feeno, "S");
            return View();
        }

        //精神評估_列印
        public ActionResult AssessmentSpirit_O_PDF(string tableid, string feeno)
        {
            set_viewbag_PDF(tableid, feeno, "Z");
            return View();
        }

        //產兒科_列印
        public ActionResult AssessmentObstetrics_PDF(string tableid, string feeno)
        {
            set_viewbag_PDF(tableid, feeno, "G");
            return View();
        }

        #endregion
        public string Partial_Record_Img(string record_id)
        {
            string return_img = null;
            if (Session["PatInfo"] != null)
            {
                string feeno = ptinfo.FeeNo;
                DataTable dt = new DataTable();
                dt = ass_m.sel_assessment_clock(record_id);

                if (dt.Rows.Count == 1)
                {
                    byte[] imageBytes = (byte[])dt.Rows[0]["ITEMVALUE"];
                    return_img = Convert.ToBase64String(imageBytes);
                }

                return return_img;
            }
            Response.Write("<script>alert('登入逾時');</script>");
            return "";
        }
        #region other function
        public ActionResult ShowIMG(string record_id)
        {
            string return_img = null;
            if (Session["PatInfo"] != null)
            {
                string feeno = ptinfo.FeeNo;
                DataTable dt = new DataTable();
                dt = ass_m.sel_assessment_clock(record_id);

                if (dt.Rows.Count == 1)
                {
                    byte[] imageBytes = (byte[])dt.Rows[0]["ITEMVALUE"];
                    return_img = Convert.ToBase64String(imageBytes);
                }
                ViewBag.clock = dt;
            }
            else
            {
                Response.Write("<script>alert('登入逾時');</script>");

            }
            return View();
        }
        private void set_viewbag_PDF(string tableid, string feeno, string na_type)
        {
            DataTable dt = new DataTable();
            PatientInfo pinfo = new PatientInfo();
            byte[] ByteCode = webService.GetPatientInfo(feeno);
            string JosnArr = "", TableId = "";
            //病人資訊
            if (ByteCode != null)
            {
                JosnArr = CompressTool.DecompressString(ByteCode);
                pinfo = JsonConvert.DeserializeObject<PatientInfo>(JosnArr);
            }

            if (tableid != null)
            {
                TableId = tableid;
                dt = ass_m.sel_assessment_contnet(tableid);
            }
            else
            {
                dt = this.sel_assess_data(pinfo.FeeNo, na_type);
                if (dt.Rows.Count > 0)
                    TableId = dt.Rows[0]["TABLEID"].ToString();
            }

            // 20140119 新增病人資訊 取得初次建立者及最後修改者資訊
            ViewData["ptinfo"] = pinfo;
            DataTable createinfo = ass_m.get_assessment_user(tableid);
            ViewBag.createtime = "";
            ViewBag.modifytime = "";
            ViewBag.createuser = "";
            ViewBag.modifyuser = "";
            if (createinfo.Rows.Count > 0)
            {
                ViewBag.createtime = ((DateTime)createinfo.Rows[0]["CREATETIME"]).ToString("yyyy/MM/dd HH:mm:ss");
                ViewBag.modifytime = (createinfo.Rows[0]["MODIFYTIME"].ToString() != "") ? ((DateTime)createinfo.Rows[0]["MODIFYTIME"]).ToString("yyyy/MM/dd HH:mm:ss") : "";

                ByteCode = webService.UserName(createinfo.Rows[0]["CREATEUSER"].ToString());
                if (ByteCode != null)
                {
                    JosnArr = CompressTool.DecompressString(ByteCode);
                    ViewBag.createuser = JsonConvert.DeserializeObject<UserInfo>(JosnArr).EmployeesName;
                }

                if (createinfo.Rows[0]["MODIFYUSER"].ToString() != "")
                {
                    ByteCode = webService.UserName(createinfo.Rows[0]["MODIFYUSER"].ToString());
                    if (ByteCode != null)
                    {
                        JosnArr = CompressTool.DecompressString(ByteCode);
                        ViewBag.modifyuser = JsonConvert.DeserializeObject<UserInfo>(JosnArr).EmployeesName;
                    }
                }
            }

            ViewBag.pt_icd9 = pinfo.ICD9_code1;
            List<DrugOrder> Drug_list = new List<DrugOrder>();
            byte[] labfoByteCode = webService.GetOpdMed(pinfo.FeeNo);
            if (labfoByteCode != null)
            {
                string labJosnArr = CompressTool.DecompressString(labfoByteCode);
                Drug_list = JsonConvert.DeserializeObject<List<DrugOrder>>(labJosnArr);
            }
            ViewData["Drug_list"] = Drug_list;
            ViewBag.temp = "0";
            if (dt.Rows.Count > 0)
                ViewBag.temp = "";
            ViewBag.dt = dt;
        }

        private void set_viewbag(string ntype, string tableid, string mode, string[] item_id = null)
        {
            ViewBag.mode = mode;
            ViewBag.pt_indate = ptinfo.InDate;
            ViewBag.pt_icd9 = ptinfo.ICD9_code1;

            //取得目前用藥
            List<DrugOrder> Drug_list = new List<DrugOrder>();
            byte[] labfoByteCode = webService.GetOpdMed(ptinfo.FeeNo);
            if (labfoByteCode != null)
            {
                string labJosnArr = CompressTool.DecompressString(labfoByteCode);
                Drug_list = JsonConvert.DeserializeObject<List<DrugOrder>>(labJosnArr);
            }
            ViewData["Drug_list"] = Drug_list;

            //if (tableid != null)
            //    ViewBag.dt = ass_m.sel_assessment_contnet(tableid);//by jarvis 20160608
            if (tableid != null && mode == "insert" && item_id == null)
            {
                if (ntype != "ER")
                    ViewBag.dt = ass_m.sel_assessment_contnet_CarryOutInsert(tableid, ntype);
            }
            else if (tableid != null)
                ViewBag.dt = ass_m.sel_assessment_contnet(tableid);
            else
            {
                if (ass_m.get_vs(ptinfo.FeeNo, "bh").Rows.Count > 0)
                    ViewBag.Heigth = ass_m.get_vs(ptinfo.FeeNo, "bh").Rows[0]["vs_record"].ToString();
                if (ass_m.get_vs(ptinfo.FeeNo, "bw").Rows.Count > 0)
                    ViewBag.Weigth = ass_m.get_vs(ptinfo.FeeNo, "bw").Rows[0]["vs_record"].ToString().Split('|').GetValue(0);
                //20140604增加 生命徵象(疼痛評估)拋轉入院評估 mod by yungchen
                if (ass_m.get_vs(ptinfo.FeeNo, "ps").Rows.Count > 0)
                {
                    if (ass_m.get_vs(ptinfo.FeeNo, "ps").Rows[0]["VS_PART"].ToString() == "數字量表(成人)")
                    {
                        ViewBag.param_PainScale = "一般疼痛";
                    }
                    else if (ass_m.get_vs(ptinfo.FeeNo, "ps").Rows[0]["VS_PART"].ToString() == "困難評估(成人)")
                    {
                        ViewBag.param_PainScale = "困難評估";
                    }
                    ViewBag.painlist = ass_m.get_vs(ptinfo.FeeNo, "ps").Rows[0]["VS_RECORD"].ToString();
                }
                //20140604 mod by yungchen
                byte[] inHistoryByte = webService.GetInHistory(ptinfo.ChartNo);
                if (inHistoryByte != null)
                {
                    string inHistoryJson = CompressTool.DecompressString(inHistoryByte);
                    List<InHistory> inHistoryList = JsonConvert.DeserializeObject<List<InHistory>>(inHistoryJson);
                    ViewData["inHistory"] = inHistoryList;
                    inHistoryList = inHistoryList.Where(x => x.FeeNo != null && x.FeeNo.Length <= 10).ToList();
                    inHistoryList.Sort((x, y) => { return -x.indate.CompareTo(y.indate); });
                    if (inHistoryList.Count > 1)
                    {
                        ViewBag.number = (inHistoryList.Count - 1).ToString();//住院次數(不含這次)
                        ViewBag.lastdate = inHistoryList[1].indate.ToString("yyyy/MM/dd");
                        ViewBag.lastICD9 = inHistoryList[1].Description.ToString();
                        if (ntype != "ER")
                            ViewBag.dt = base.sel_assess_data(inHistoryList[1].FeeNo, "", item_id);
                    }
                }
            }

        }

        /// <summary> 取得最後一次診斷 </summary>
        public InHistory getLastDrag()
        {
            string inHistoryJson = string.Empty;
            try
            {
                byte[] inHistoryByte = webService.GetInHistory(ptinfo.ChartNo.Trim());

                inHistoryJson = CompressTool.DecompressString(inHistoryByte);
                List<InHistory> inHistoryObj = JsonConvert.DeserializeObject<List<InHistory>>(inHistoryJson);

                return inHistoryObj[inHistoryObj.Count - 1];
            }
            catch (Exception ex)
            {
                this.log.saveLogMsg(inHistoryJson, "getLastDrag");
                this.log.saveLogMsg(ex.Message.ToString(), "getLastDrag");

                return null;
            }
        }

        //轉PDF頁面
        public ActionResult Html_To_Pdf(string url)
        {
            string strPath = @"C:\\wkhtmltopdf\\wkhtmltopdf.exe";
            string filename = DateTime.Now.ToString("yyyyMMddHHmmssfff") + url.Substring(url.IndexOf("feeno=") + 6, url.IndexOf("natype=") - url.IndexOf("feeno=") - 7) + ".pdf";
            string tempPath = this.DirectUrl + @"\Images\" + filename;
            string option = " ";
            //20140924 更新pdf列印為各院名稱
            string HospitalName = System.Configuration.ConfigurationManager.AppSettings["HospitalName"].ToString();
            if (System.Configuration.ConfigurationManager.AppSettings["Footer"].ToString() == "Y")
                option = " --footer-font-size 8  --footer-left \"F-8100-057D 101 年 04 月 27 日" + HospitalName + "病歷管理委員會審查通過 -1010427 修 第[page]頁/共[topage]頁\" ";

            Process p = new Process();
            p.StartInfo.FileName = strPath;
            p.StartInfo.Arguments = url + option + tempPath;
            p.StartInfo.UseShellExecute = true;
            p.Start();
            p.WaitForExit();
            //Response.Write("<script>window.location.href='Download_Pdf?filename=" + filename + "';</script>");
            Response.Write("<script>window.open('Download_Pdf?filename=" + filename + "');window.location.href='AssessmentList?natype=" + url.Substring(url.IndexOf("natype=") + 7, 1) + "';</script>");

            return new EmptyResult();
        }

        public ActionResult Download_Pdf(string filename)
        {
            string tempPath = this.DirectUrl + @"\Images\" + filename;

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
        /// 拋轉傷口
        /// </summary>
        /// <param name="type">傷口種類</param>
        /// <param name="position">傷口部位</param>
        /// <param name="wound_cound">傷口順序</param>
        private int Insert_Wound(string date, string type, string position, int wound_cound, string location)
        {
            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;
            string id = base.creatid("WOUND_DATA", userno, feeno, wound_cound.ToString());
            //string date = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("WOUND_ROW", "WOUND_DATA_SEQUENCE.NEXTVAL", DBItem.DBDataType.Number));
            insertDataList.Add(new DBItem("WOUND_ID", id, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREANO", userno, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
            if (date != null && date != "")
                insertDataList.Add(new DBItem("CREATTIME", date, DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("NUM", "(SELECT (COUNT(*) + 1) FROM WOUND_DATA WHERE FEENO = '" + feeno + "')", DBItem.DBDataType.Number));
            insertDataList.Add(new DBItem("TYPE", type, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("POSITION", position, DBItem.DBDataType.String));
            if (location != "")
                insertDataList.Add(new DBItem("LOCATION", location, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
            int erow = ass_m.DBExecInsert("WOUND_DATA", insertDataList);
            #region --帶入護理記錄-傷口--
            string Str = "";
            if (type == "壓瘡" || type == "壓傷")
            {//於【部位】因【導因】等原因，自【發生地點】發生一壓傷傷口。
                Str += "於" + position + "，自";
                Str += location + "發生一壓傷傷口。";
            }
            else
            {//於【部位】發生一【傷口類別】傷口。
                Str += "於" + position + "發生一" + type + "傷口。";
            }
            erow += Insert_CareRecord(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), id, "", "", "", Str, "", "", "Wound");//(date, id, "Intake", "", "", Str, "", "", "IO_DATA");
            #endregion
            return erow;
        }
        /// <summary>
        /// 拋轉管路
        /// </summary>
        /// <param name="name">管路名稱</param>
        /// <param name="tube_mat">管路材質</param>
        /// <param name="tube_model">管路管徑</param>
        /// <param name="tube_dt">管路放置日期</param>
        /// <param name="end_time">預計到期日</param>
        /// <param name="position">放置部位</param>
        /// <param name="location">放置部位上下左右</param>">
        /// <param name="tube_cound">筆數</param>
        private int Insert_tube(string name, string tube_mat, string tube_model, string tube_dt, string end_time, string position, string location, int tube_cound, string position_other, string tube_mat_other, string name_text, string location_text)
        {
            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;
            if (position.Trim() == "其他") position = position_other.Trim();

            List<DBItem> insertTubeList = new List<DBItem>();
            string id = base.creatid("TUBE", userno, feeno, tube_cound.ToString());
            insertTubeList.Add(new DBItem("TUBEROW", "TUBE_SEQUENCE.NEXTVAL", DBItem.DBDataType.Number));
            insertTubeList.Add(new DBItem("TUBEID", id, DBItem.DBDataType.String));
            insertTubeList.Add(new DBItem("TYPEID", name, DBItem.DBDataType.String));
            insertTubeList.Add(new DBItem("POSITION", position, DBItem.DBDataType.String));
            insertTubeList.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
            insertTubeList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
            insertTubeList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
            insertTubeList.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
            insertTubeList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
            insertTubeList.Add(new DBItem("STARTTIME", Convert.ToDateTime(tube_dt).ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
            if (end_time != "")
                insertTubeList.Add(new DBItem("FORECAST", Convert.ToDateTime(end_time).ToString("yyyy/MM/dd"), DBItem.DBDataType.DataTime));

            insertTubeList.Add(new DBItem("LOCATION", location, DBItem.DBDataType.String));
            insertTubeList.Add(new DBItem("MODEL", tube_model, DBItem.DBDataType.String));

            List<DBItem> insertFeatureList = new List<DBItem>();
            int maxnumber = 0;

            if (tubem.sel_tube_max_number(feeno).Rows.Count > 0 && tubem.sel_tube_max_number(feeno).Rows[0][0] != null)
            {
                bool result = int.TryParse(tubem.sel_tube_max_number(feeno).Rows[0][0].ToString(), out maxnumber);
                maxnumber = maxnumber + 1;
            }
            insertFeatureList.Add(new DBItem("FEATUREID", id, DBItem.DBDataType.String));
            insertFeatureList.Add(new DBItem("NUMBERID", "99", DBItem.DBDataType.String));//
            insertFeatureList.Add(new DBItem("NUMBEROTHER", maxnumber.ToString(), DBItem.DBDataType.String));//編號
            insertFeatureList.Add(new DBItem("MATERIALID", tube_mat, DBItem.DBDataType.String));//
            insertFeatureList.Add(new DBItem("MATERIALOTHER", tube_mat_other, DBItem.DBDataType.String));//管路材質 其他
            insertFeatureList.Add(new DBItem("COLORID", "-1", DBItem.DBDataType.String));//
            insertFeatureList.Add(new DBItem("NATUREID", "-1", DBItem.DBDataType.String));//
            insertFeatureList.Add(new DBItem("TASTEID", "-1", DBItem.DBDataType.String));//         


            int erow_tube = ass_m.DBExecInsert("TUBE", insertTubeList);
            int erow_feature = ass_m.DBExecInsert("TUBE_FEATURE", insertFeatureList);

            int erow = erow_tube + erow_feature;

            if (erow == 2)
            {
                string Str = string.Format("於{0}在{1}{2}放置{3}", tube_dt, position, location_text, name_text);
                erow += Insert_CareRecord(DateTime.Now.ToString("yyyy/MM/dd HH:mm"), id, "放置" + name_text, "", "", Str, "", "", "TUBE");
            }

            return erow;
        }

        /// <summary>
        /// 拋轉生命徵象
        /// </summary>
        private int insert_vs(string item, string part, string value, string date, string vs_id)
        {
            if (value != "")
            {
                List<DBItem> vs_data = new List<DBItem>();
                vs_data.Add(new DBItem("fee_no", ptinfo.FeeNo.Trim(), DBItem.DBDataType.String));
                vs_data.Add(new DBItem("vs_id", vs_id, DBItem.DBDataType.String));
                vs_data.Add(new DBItem("vs_item", item, DBItem.DBDataType.String));
                vs_data.Add(new DBItem("VS_PART", part, DBItem.DBDataType.String));
                vs_data.Add(new DBItem("vs_record", value, DBItem.DBDataType.String));
                vs_data.Add(new DBItem("create_date", Convert.ToDateTime(date).ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                vs_data.Add(new DBItem("modify_date", Convert.ToDateTime(date).ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                vs_data.Add(new DBItem("create_user", userinfo.EmployeesNo.Trim(), DBItem.DBDataType.String));
                vs_data.Add(new DBItem("modify_user", userinfo.EmployeesNo.Trim(), DBItem.DBDataType.String));
                vs_data.Add(new DBItem("plan", "N", DBItem.DBDataType.String));
                return ass_m.DBExecInsert("data_vitalsign", vs_data);
            }
            else
                return 0;
        }


        /// <summary>
        /// 拋轉生命徵象
        /// </summary>
        private int insert_del(string item, string part, string value, string date, string vs_id)
        {
            if (value != "")
            {
                List<DBItem> vs_data = new List<DBItem>();
                vs_data.Add(new DBItem("ASSESS_DT", date, DBItem.DBDataType.DataTime));
                //vs_data.Add(new DBItem("SCORE_1A", form["rb_acute_attack_1a"], DBItem.DBDataType.String));
                //vs_data.Add(new DBItem("SCORE_1B", form["rb_acute_attack_1b"], DBItem.DBDataType.String));
                //vs_data.Add(new DBItem("SCORE_2", form["rb_attention"], DBItem.DBDataType.String));
                //vs_data.Add(new DBItem("SCORE_3", form["rb_ponder"], DBItem.DBDataType.String));
                //vs_data.Add(new DBItem("SCORE_4", form["rb_consciousness"], DBItem.DBDataType.String));
                //vs_data.Add(new DBItem("TOTAL_SCORE", form["hid_score"], DBItem.DBDataType.String));
                //vs_data.Add(new DBItem("RESULT", form["hid_score_remark"], DBItem.DBDataType.String));
                vs_data.Add(new DBItem("DEL_USER", "", DBItem.DBDataType.String));
                //vs_data.Add(new DBItem("ASSESSMENT_REASON", msg, DBItem.DBDataType.String));
                return ass_m.DBExecInsert("DELIRIUM_DATA", vs_data);
            }
            else
                return 0;
        }


        #endregion


        #region --xml組成成人入評--
        public string Composition_Xml_A(DataTable dt)
        {
            string xml = string.Empty;
            xml += "<?xml version='1.0' encoding='UTF-8'?>";
            //xml += "<?xml-stylesheet type='text/xsl' href='./NIS_A000034.XSL'?>";
            xml += "<Document>";
            #region 病患資訊
            string allergyDesc = string.Empty;
            byte[] allergen = webService.GetAllergyList(ptinfo.FeeNo);
            string ptJsonArr = string.Empty;
            if (allergen != null)
            {
                ptJsonArr = CompressTool.DecompressString(allergen);
            }

            List<NIS.Data.PatientInfo> patList = JsonConvert.DeserializeObject<List<NIS.Data.PatientInfo>>(ptJsonArr);
            if (allergen != null)
            {
                allergyDesc = patList[0].AllergyDesc;
            }
            xml += "<TING_PT_NAME>" + ptinfo.PatientName + "</TING_PT_NAME>";
            xml += "<TING_CHART_NO>" + ptinfo.ChartNo + "</TING_CHART_NO>";
            xml += "<TING_SEX>" + ptinfo.PatientGender + "</TING_SEX>";
            xml += "<TING_AGE>" + ptinfo.Age + "</TING_AGE>";
            xml += "<TING_BED_NO>" + ptinfo.BedNo + "</TING_BED_NO>";
            xml += "<TING_ADMIT_DATE>" + ptinfo.InDate.ToString("yyyyMMdd") + "</TING_ADMIT_DATE>";
            xml += "<TING_ADMIT_TIME>" + ptinfo.InDate.ToString("HHmm") + "</TING_ADMIT_TIME>";
            xml += "<TING_ALLERGEN>" + allergyDesc + "</TING_ALLERGEN>";
            #endregion
            #region --基本資料--
            xml += "<param_tube_date>" + "病房報到日期:" + sel_data(dt, "param_tube_date") + " " + sel_data(dt, "param_tube_time") + "</param_tube_date>";
            if (sel_data(dt, "param_assessment") == "其他")
            { xml += "<param_assessment>" + "類別:" + sel_data(dt, "param_assessment") + ":" + sel_data(dt, "param_assessment_other") + "</param_assessment>"; }
            else if (sel_data(dt, "param_assessment") == "轉入")
            { xml += "<param_assessment>" + "類別:" + sel_data(dt, "param_assessment") + ";來源床號:" + sel_data(dt, "param_assessment_turn") + "</param_assessment>"; }
            else
            { xml += "<param_assessment>" + "類別:" + sel_data(dt, "param_assessment") + "</param_assessment>"; }
            xml += "<param_ipd_reason>" + "入院經過:" + sel_data(dt, "param_ipd_reason") + "</param_ipd_reason>";
            xml += "<param_ipd_source>" + "入院來源:" + sel_data(dt, "param_ipd_source") + "</param_ipd_source>";
            if (sel_data(dt, "param_ipd_style") != "其他")
            { xml += "<param_ipd_style>" + "入院方式:" + sel_data(dt, "param_ipd_style") + "</param_ipd_style>"; }
            else
            { xml += "<param_ipd_style>" + "入院方式:" + sel_data(dt, "param_ipd_style") + ":" + sel_data(dt, "param_ipd_style_other") + "</param_ipd_style>"; }
            xml += "<param_education>" + "教育:" + sel_data(dt, "param_education") + "</param_education>";
            if (sel_data(dt, "param_religion") != "其他")
            { xml += "<param_religion>" + "宗教:" + sel_data(dt, "param_religion") + "</param_religion>"; }
            else
            {
                xml += "<param_religion>" + "宗教:" + sel_data(dt, "param_religion") + ":" + sel_data(dt, "param_religion_other") + "</param_religion>";
            }
            xml += "<param_job>" + "職業:" + sel_data(dt, "param_job") + "</param_job>";
            xml += "<param_marrage>" + "婚姻:" + sel_data(dt, "param_marrage") + "</param_marrage>";
            xml += "<param_child_f>" + "子女:男" + sel_data(dt, "param_child_f") + "人</param_child_f>";
            xml += "<param_child_m>" + "子女:女" + sel_data(dt, "param_child_m") + "人</param_child_m>";
            if (sel_data(dt, "param_living") != "其他")
            {
                xml += "<param_living>" + "居住方式:" + sel_data(dt, "param_living") + "</param_living>";
            }
            else
            {
                xml += "<param_living>" + "居住方式:" + sel_data(dt, "param_living") + ":" + sel_data(dt, "param_living_other") + "</param_living>";
            }
            xml += "<param_ph_home>";
            if (sel_data(dt, "param_ph_home") != "")
            {
                xml += "病人聯絡電話(宅):" + sel_data(dt, "param_ph_home");
            }
            xml += "</param_ph_home>";
            xml += "<param_ph_office>";
            if (sel_data(dt, "param_ph_office") != "")
            {
                xml += "病人聯絡電話(公):" + sel_data(dt, "param_ph_office");
            }
            xml += "</param_ph_office>";
            xml += "<param_ph_number>";
            if (sel_data(dt, "param_ph_number") != "")
            {
                xml += "病人聯絡電話(手機):" + sel_data(dt, "param_ph_number");
            }
            xml += "</param_ph_number>";

            string[] EMGName = sel_data(dt, "param_EMGContact").Split(',');
            //string[] EMGContactRole = sel_data(dt, "param_ContactRole").Trim(',').Split(',');
            string[] EMGContactRole_other = sel_data(dt, "param_ContactRole_other").Split(',');
            string[] EMGContact_1 = sel_data(dt, "param_EMGContact_1").Split(',');
            string[] EMGContact_2 = sel_data(dt, "param_EMGContact_2").Split(',');
            string[] EMGContact_3 = sel_data(dt, "param_EMGContact_3").Split(',');
            xml += "<param_EMGContact>";
            for (int i = 0; i < EMGName.Length; i++)
            {
                xml += "緊急聯絡人姓名:" + EMGName[i] + ";";
                string TempEMGContactRole = (i != 0) ? sel_data(dt, "param_ContactRole_" + i.ToString()) : sel_data(dt, "param_ContactRole");
                if (TempEMGContactRole != "其他")
                {
                    xml += "稱謂:" + TempEMGContactRole + ";";
                }
                else
                {
                    xml += "稱謂:" + TempEMGContactRole + ":" + EMGContactRole_other[i] + ";";
                }
                if (EMGContact_1[i].ToString().Trim() != "")
                {
                    xml += "連絡電話:住家:" + EMGContact_1[i] + ";";
                }
                if (EMGContact_2[i].ToString().Trim() != "")
                {
                    xml += "公司:" + EMGContact_2[i] + ";";
                }
                if (EMGContact_3[i].ToString().Trim() != "")
                {
                    xml += "手機:" + EMGContact_3[i] + ";";
                }
                xml = xml.Substring(0, xml.Length - 1);
                xml += "。";
            }
            xml += "</param_EMGContact>";

            #endregion
            #region --個人病史--
            xml += "<param_hasipd>" + "住院經驗:" + sel_data(dt, "param_hasipd");
            xml += "</param_hasipd>";
            xml += "<param_ipd_past_reason>";
            if (sel_data(dt, "param_hasipd") == "有")
            {
                //xml = xml + ";";
                string[] ipd_past_reason = sel_data(dt, "param_ipd_past_reason").Split(',');
                string[] ipd_past_location = sel_data(dt, "param_ipd_past_location").Split(',');
                for (int i = 0; i < ipd_past_reason.Length; i++)
                {
                    xml += "原因:" + ipd_past_reason[i];
                    xml += "地點:" + ipd_past_location[i] + "。";
                }
            }
            xml += "</param_ipd_past_reason>";
            xml += "<param_surgery>" + "手術經驗:" + sel_data(dt, "param_surgery");
            xml += "</param_surgery>";
            xml += "<param_ipd_surgery_reason>";
            if (sel_data(dt, "param_surgery") == "有")
            {
                // xml = xml + ";";
                string[] ipd_surgery_reason = sel_data(dt, "param_ipd_surgery_reason").Split(',');
                string[] ipd_surgery_location = sel_data(dt, "param_ipd_surgery_location").Split(',');
                for (int i = 0; i < ipd_surgery_reason.Length; i++)
                {
                    xml += "原因:" + ipd_surgery_reason[i] + ";";
                    xml += "地點:" + ipd_surgery_location[i] + ";";
                }
            }
            xml += "</param_ipd_surgery_reason>";
            /*-----------------------------------------------201606232102檢查到此---------------------------------------*/
            xml += "<param_blood>" + "輸血經驗:" + sel_data(dt, "param_blood");
            xml += "</param_blood>";
            //if(sel_data(dt, "param_blood") == "有")
            //{
            //    xml = xml + ";";
            xml += "<param_transfusion_blood_allergy>";
            if (sel_data(dt, "param_blood") == "有")
            {
                //xml += "過敏反應:" + sel_data(dt, "param_transfusion_blood_allergy");
                //if(sel_data(dt, "param_transfusion_blood_allergy") == "是")
                //{
                //    xml += ";" + sel_data(dt, "transfusion_blood_dtl_txt");
                //}
                xml += "輸血過敏反應:" + sel_data(dt, "param_blood_reaction");
                if (sel_data(dt, "param_blood_reaction") == "有")
                {
                    xml += ";" + sel_data(dt, "transfusion_blood_dtl_txt");
                }
            }
            xml += "</param_transfusion_blood_allergy>";
            //}
            xml += "<param_allergy_med>" + "過敏史藥物:" + sel_data(dt, "param_allergy_med");
            if (sel_data(dt, "param_allergy_med") == "有")
            {//不詳,pyrin,aspirin,NSAID,顯影劑,磺氨類,盤尼西林類,抗生素類,麻醉藥,其他
                xml = xml + ";" + sel_data(dt, "param_allergy_med_other");
            }
            xml += "</param_allergy_med>";
            string[] allergy_med_other = sel_data(dt, "param_allergy_med_other").Split(',');
            xml += "<param_allergy_med_other_2_name>";
            if (sel_data(dt, "param_allergy_med_other_2_name") != "")
            {
                xml += "匹林系藥物(pyrin):";
                for (int i = 0; i < allergy_med_other.Length; i++)
                {
                    if (allergy_med_other[i] == "pyrin")
                    {
                        xml += sel_data(dt, "param_allergy_med_other_2_name");
                    }
                }
            }
            xml += "</param_allergy_med_other_2_name>";
            xml += "<param_allergy_med_other_4_name>";
            if (sel_data(dt, "param_allergy_med_other_4_name") != "")
            {
                xml += "非類固醇抗炎藥物(NSAID):";
                for (int i = 0; i < allergy_med_other.Length; i++)
                {
                    if (allergy_med_other[i] == "NSAID")
                    {

                        xml += sel_data(dt, "param_allergy_med_other_4_name");
                    }
                }
            }
            xml += "</param_allergy_med_other_4_name>";
            xml += "<param_allergy_med_other_6_name>";
            if (sel_data(dt, "param_allergy_med_other_6_name") != "")
            {
                xml += "磺氨類:";
                for (int i = 0; i < allergy_med_other.Length; i++)
                {
                    if (allergy_med_other[i] == "磺氨類")
                    {
                        xml += sel_data(dt, "param_allergy_med_other_6_name");
                    }
                }
            }
            xml += "</param_allergy_med_other_6_name>";
            xml += "<param_allergy_med_other_7_name>";
            if (sel_data(dt, "param_allergy_med_other_7_name") != "")
            {
                xml += "盤尼西林類:";
                for (int i = 0; i < allergy_med_other.Length; i++)
                {
                    if (allergy_med_other[i] == "盤尼西林類")
                    {
                        xml += sel_data(dt, "param_allergy_med_other_7_name");
                    }
                }
            }
            xml += "</param_allergy_med_other_7_name>";
            xml += "<param_allergy_med_other_8_name>";
            if (sel_data(dt, "param_allergy_med_other_8_name") != "")
            {
                xml += "抗生素類:";
                for (int i = 0; i < allergy_med_other.Length; i++)
                {
                    if (allergy_med_other[i] == "抗生素類")
                    {
                        xml += sel_data(dt, "param_allergy_med_other_8_name");
                    }
                }
            }
            xml += "</param_allergy_med_other_8_name>";
            xml += "<param_allergy_med_other_9_name>";
            if (sel_data(dt, "param_allergy_med_other_9_name") != "")
            {
                xml += "麻醉藥:";
                for (int i = 0; i < allergy_med_other.Length; i++)
                {
                    if (allergy_med_other[i] == "麻醉藥")
                    {
                        xml += sel_data(dt, "param_allergy_med_other_9_name");
                    }
                }
            }
            xml += "</param_allergy_med_other_9_name>";
            xml += "<param_allergy_med_other_10_name>";
            if (sel_data(dt, "param_allergy_med_other_10_name") != "")
            {
                xml += "其他:";
                for (int i = 0; i < allergy_med_other.Length; i++)
                {
                    if (allergy_med_other[i] == "其他")
                    {
                        xml += sel_data(dt, "param_allergy_med_other_10_name");
                    }
                }
            }
            xml += "</param_allergy_med_other_10_name>";
            // xml = xml.Substring(0, xml.Length - 1);
            xml += "<param_allergy_food>" + "過敏史食物:" + sel_data(dt, "param_allergy_food");
            if (sel_data(dt, "param_allergy_food") == "有")
            {
                xml = xml + ";" + sel_data(dt, "param_allergy_food_other");
            }
            xml += "</param_allergy_food>";
            string[] allergy_food_other = sel_data(dt, "param_allergy_food_other").Split(',');
            xml += "<param_allergy_food_other_2_name>";
            if (sel_data(dt, "param_allergy_food_other_2_name") != "")
            {
                xml += "海鮮類:";
                for (int i = 0; i < allergy_food_other.Length; i++)
                {
                    if (allergy_food_other[i] == "海鮮類")
                    {
                        xml += sel_data(dt, "param_allergy_food_other_2_name");
                    }
                }
            }
            xml += "</param_allergy_food_other_2_name>";
            xml += "<param_allergy_food_other_4_name>";
            if (sel_data(dt, "param_allergy_food_other_4_name") != "")
            {
                xml += "水果:";
                for (int i = 0; i < allergy_food_other.Length; i++)
                {
                    if (allergy_food_other[i] == "水果")
                    {
                        xml += sel_data(dt, "param_allergy_food_other_4_name");
                    }
                }
            }
            xml += "</param_allergy_food_other_4_name>";
            xml += "<param_allergy_food_other_6_name>";
            if (sel_data(dt, "param_allergy_food_other_6_name") != "")
            {
                xml += "過敏史食物其他:";
                for (int i = 0; i < allergy_food_other.Length; i++)
                {
                    if (allergy_food_other[i] == "其他")
                    {
                        xml += sel_data(dt, "param_allergy_food_other_6_name");
                    }
                }
            }
            xml += "</param_allergy_food_other_6_name>";
            xml += "<param_allergy_other>" + "過敏史其他:" + sel_data(dt, "param_allergy_other");
            if (sel_data(dt, "param_allergy_other") == "有")
            {
                if (sel_data(dt, "param_allergy_other_other") != "")
                {
                    xml = xml + ";";
                    string[] Temp_param_allergy_other_other = sel_data(dt, "param_allergy_other_other").Split(',');
                    //+ sel_data(dt, "param_allergy_other_other");
                    for (int i = 0; i < Temp_param_allergy_other_other.Length; i++)
                    {
                        string Temp1 = Temp_param_allergy_other_other[i].Substring(0, 1);
                        if (Temp1 == "麈")
                        {

                            xml += "麈蟎,";
                        }
                        else
                        {
                            xml += Temp_param_allergy_other_other[i] + ",";
                        }
                    }
                    xml = xml.Substring(0, xml.Length - 1);
                }
            }
            xml += "</param_allergy_other>";
            string[] allergy_other_other = sel_data(dt, "param_allergy_other_other").Split(',');
            xml += "<param_allergy_other_other_1_name>";
            //for (int i = 0; i < allergy_other_other.Length; i++)
            //{
            //xml += "不詳:";
            //    if (allergy_other_other[i] == "不詳")
            //    {
            //        xml += sel_data(dt, "param_allergy_other_other_1_name");
            //    }
            //}
            xml += "</param_allergy_other_other_1_name>";
            xml += "<param_allergy_other_other_2_name>";
            //for (int i = 0; i < allergy_other_other.Length; i++)
            //{
            //xml += "輸血:";
            //    if (allergy_other_other[i] == "輸血")
            //    {
            //        xml += sel_data(dt, "param_allergy_other_other_2_name");
            //    }
            //}
            xml += "</param_allergy_other_other_2_name>";
            xml += "<param_allergy_other_other_3_name>";
            //for (int i = 0; i < allergy_other_other.Length; i++)
            //{
            //xml += "油漆:";
            //    if (allergy_other_other[i] == "油漆")
            //    {
            //        xml += sel_data(dt, "param_allergy_other_other_3_name");
            //    }
            //}
            xml += "</param_allergy_other_other_3_name>";
            xml += "<param_allergy_other_other_4_name>";
            //for (int i = 0; i < allergy_other_other.Length; i++)
            //{
            //xml += "昆蟲:";
            //    if (allergy_other_other[i] == "昆蟲")
            //    {
            //        xml += sel_data(dt, "param_allergy_other_other_4_name");
            //    }
            //}
            xml += "</param_allergy_other_other_4_name>";
            xml += "<param_allergy_other_other_5_name>";
            //for (int i = 0; i < allergy_other_other.Length; i++)
            //{
            //xml += "麈蟎:";
            //    if (allergy_other_other[i] == "麈蟎")
            //    {
            //        xml += sel_data(dt, "param_allergy_other_other_5_name");
            //    }
            //}
            xml += "</param_allergy_other_other_5_name>";
            xml += "<param_allergy_other_other_6_name>";
            if (sel_data(dt, "param_allergy_other_other_6_name") != "")
            {
                for (int i = 0; i < allergy_other_other.Length; i++)
                {
                    xml += "其他:";
                    if (allergy_other_other[i] == "其他")
                    {
                        xml += sel_data(dt, "param_allergy_other_other_6_name");
                    }
                }
            }
            xml += "</param_allergy_other_other_6_name>";
            // xml = xml.Substring(0, xml.Length - 1);
            xml += "<param_im_history>" + "疾病史:" + sel_data(dt, "param_im_history");
            if (sel_data(dt, "param_im_history") == "有")
            {
                xml = xml + ";";
                string[] im_history_item_other = sel_data(dt, "param_im_history_item_other").Split(',');
                for (int i = 0; i < im_history_item_other.Length; i++)
                {
                    if (im_history_item_other[i] == "其他")
                    {
                        xml += im_history_item_other[i] + ":" + sel_data(dt, "param_im_history_item_other_txt") + ",";
                    }
                    else
                    {
                        xml += im_history_item_other[i] + ",";
                    }
                }
                xml = xml.Substring(0, xml.Length - 1);
            }
            xml += "</param_im_history>";


            xml += "<param_med>" + "服藥:" + sel_data(dt, "param_med");
            if (sel_data(dt, "param_med") == "有")
            {
                List<DrugOrder> Drug_list = new List<DrugOrder>();
                byte[] labfoByteCode = webService.GetOpdMed(ptinfo.FeeNo);
                if (labfoByteCode != null)
                {
                    string labJosnArr = CompressTool.DecompressString(labfoByteCode);
                    Drug_list = JsonConvert.DeserializeObject<List<DrugOrder>>(labJosnArr);
                }
                xml = xml + ";";
                for (int j = 0; j < Drug_list.Count; j++)
                {
                    xml += "門診用藥-藥物名稱:" + Drug_list[j].DrugName + ",";
                    xml += "頻次:" + Drug_list[j].Feq + ",";
                    xml += "劑量:" + Drug_list[j].Dose + ",";
                    xml += "途徑:" + Drug_list[j].Route + ",";
                    xml += ";";
                }
                string[] med_name = sel_data(dt, "param_med_name").Split(',');
                for (int i = 0; i < med_name.Length; i++)
                {
                    xml += med_name[i] + ",";
                }
                xml = xml.Substring(0, xml.Length - 1);
            }
            xml += "</param_med>";
            #endregion
            #region --日常生活--
            string Templanguage = (sel_data(dt, "param_language") == "2") ? "語言" : "不能言語";
            xml += "<param_language>" + "溝通方式:" + Templanguage + ";";
            xml += "</param_language>";
            //if(sel_data(dt, "param_language") == "2")
            //{
            xml += "<param_lang>";
            if (sel_data(dt, "param_language") == "2")
            {
                xml += "語言:";
                string[] language = sel_data(dt, "param_lang").Split(',');
                for (int i = 0; i < language.Length; i++)
                {
                    if (language[i] != "其他")
                    {
                        xml += language[i] + ",";
                    }
                    else
                    {
                        xml += language[i] + ":" + sel_data(dt, "param_lang_other") + ",";
                    }
                }
                xml = xml.Substring(0, xml.Length - 1);
                xml = xml + ";";
            }
            xml += "</param_lang>";
            //}
            //else
            //{
            xml += "<param_lang_no>";
            if (sel_data(dt, "param_language") != "2")
            {
                xml += "不能言語:";
                string[] no_language = sel_data(dt, "param_lang_no").Split(',');
                for (int i = 0; i < no_language.Length; i++)
                {
                    if (no_language[i] != "其他")
                    {
                        xml += no_language[i] + ",";
                    }
                    else
                    {
                        xml += no_language[i] + ":" + sel_data(dt, "param_lang_other_no") + ",";
                    }
                }
                xml = xml.Substring(0, xml.Length - 1);
                xml = xml + ";";
            }
            xml += "</param_lang_no>";
            //}
            xml += "<param_food_drink>" + "飲食:" + sel_data(dt, "param_food_drink") + "</param_food_drink>";
            xml += "<param_food_drink_eschew_text>";
            if (sel_data(dt, "param_food_drink_eschew_text") != "")
            {
                xml += "禁忌食物:" + sel_data(dt, "param_food_drink_eschew_text");
            }
            xml += "</param_food_drink_eschew_text>";

            string[] Temp_cigarette = { "不抽", "經常", "已戒菸", "偶爾" };
            if (sel_data(dt, "param_cigarette") != null && sel_data(dt, "param_cigarette") != "")
                xml += "<param_cigarette>" + "抽煙:" + Temp_cigarette[Convert.ToInt32(sel_data(dt, "param_cigarette")) - 1];
                if(sel_data(dt, "param_cigarette") == "2" || sel_data(dt, "param_cigarette") == "4")
                {
                   xml += sel_data(dt, "param_cigarette_type").ToString();
                }

            xml += "</param_cigarette>";

            string cigarette_type = sel_data(dt, "param_cigarette_type").ToString();
            if(cigarette_type.Contains("抽香菸"))
            {
                xml += "<param_cigarette_yes_amount>";
                if (sel_data(dt, "param_cigarette_yes_amount") != "")
                {
                    xml += "每日:" + sel_data(dt, "param_cigarette_yes_amount") + "包";
                }
                xml += "</param_cigarette_yes_amount>";
                xml += "<param_cigarette_yes_year>";
                if (sel_data(dt, "param_cigarette_yes_year") != "")
                {
                    xml += "已抽:" + sel_data(dt, "param_cigarette_yes_year") + "年";
                }
                xml += "</param_cigarette_yes_year>";
            }
            xml += "<param_cigarette_tutor>";
            if (sel_data(dt, "param_cigarette_tutor") != "")
            {
                if (Convert.ToInt32(sel_data(dt, "param_cigarette")) == 2 || Convert.ToInt32(sel_data(dt, "param_cigarette")) == 4)
                {
                    xml += "有無勸戒輔導:" + sel_data(dt, "param_cigarette_tutor");
                }
            }
            xml += "</param_cigarette_tutor>";
            xml += "<param_cigarette_stop_year>";
            if (sel_data(dt, "param_cigarette_stop_year") != "")
            {
                xml += " 已戒:" + sel_data(dt, "param_cigarette_stop_year") + "年";
            }
            xml += "</param_cigarette_stop_year>";
            string[] Temp_drink = { "不喝", "大量", "已戒酒", "偶爾" };
            if (sel_data(dt, "param_drink") != null && sel_data(dt, "param_drink") != "")
                xml += "<param_drink>" + "喝酒:" + Temp_drink[Convert.ToInt32(sel_data(dt, "param_drink")) - 1];
            xml += "</param_drink>";

            //if(sel_data(dt, "param_drink") == "3")
            // {//已戒
            //xml = xml + ";";
            // }
            // else if(sel_data(dt, "param_drink") == "2")
            // {
            //     xml = xml + ";";
            xml += "<param_drink_yes_amount>";
            if (sel_data(dt, "param_drink_yes_amount") != "")
            {
                xml += "每日:" + sel_data(dt, "param_drink_yes_amount") + "瓶";
            }
            xml += "</param_drink_yes_amount>";
            xml += "<param_drink_yes_year>";
            if (sel_data(dt, "param_drink_yes_year") != "")
            {
                xml += "已喝:" + sel_data(dt, "param_drink_yes_year") + "年";
            }
            xml += "</param_drink_yes_year>";
            //  }
            xml += "<param_drink_tutor>";
            if (sel_data(dt, "param_drink_tutor") != "")
            {
                if (Convert.ToInt32(sel_data(dt, "param_drink")) == 2 || Convert.ToInt32(sel_data(dt, "param_drink")) == 4)
                {
                    xml += "有無勸戒輔導:" + sel_data(dt, "param_drink_tutor");
                }
            }
            xml += "</param_drink_tutor>";
            xml += "<param_drink_stop_year>";
            if (sel_data(dt, "param_drink_stop_year") != "")
            {
                xml += "已戒:" + sel_data(dt, "param_drink_stop_year") + "年";
            }
            xml += "</param_drink_stop_year>";
            string[] Temp_areca = { "不吃", "有", "已戒", "偶爾" };
            if (sel_data(dt, "param_areca") != null && sel_data(dt, "param_areca") != "")
                xml += "<param_areca>" + "吃檳榔:" + Temp_areca[Convert.ToInt32(sel_data(dt, "param_areca")) - 1];
            xml += "</param_areca>";
            xml += "<param_areca_stop_year>";
            if (sel_data(dt, "param_areca_stop_year") != "")
            {
                xml += "已戒:" + sel_data(dt, "param_areca_stop_year") + "年";
            }
            xml += "</param_areca_stop_year>";
            xml += "<param_areca_tutor>";
            if (sel_data(dt, "param_areca_tutor") != "")
            {
                if (Convert.ToInt32(sel_data(dt, "param_areca")) == 2 || Convert.ToInt32(sel_data(dt, "param_areca")) == 4)
                {
                    xml += "有無勸戒輔導:" + sel_data(dt, "param_areca_tutor");
                }
            }
            xml += "</param_areca_tutor>";
            string Temo_Sleeping = (sel_data(dt, "param_Sleeping") == "1") ? "正常" : "異常";
            xml += "<param_Sleeping_text>" + "睡眠習慣:" + sel_data(dt, "param_Sleeping_text") + "小時/天;" + Temo_Sleeping;
            xml += "</param_Sleeping_text>";
            xml += "<param_Sleeping_abnormal>";
            if (sel_data(dt, "param_Sleeping") == "2")
            {
                xml += "異常:";
                string[] Sleeping_abnormal = sel_data(dt, "param_Sleeping_abnormal").Split(',');
                for (int i = 0; i < Sleeping_abnormal.Length; i++)
                {
                    if (Sleeping_abnormal[i] == "其他")
                    {
                        xml += Sleeping_abnormal[i] + ":" + sel_data(dt, "param_sleeping_abnormal_other") + ",";
                    }
                    else
                    {
                        xml += Sleeping_abnormal[i] + ",";
                    }
                }
                xml = xml.Substring(0, xml.Length - 1);
            }
            xml += "</param_Sleeping_abnormal>";
            xml += "<param_eating_self>" + "自我照顧能力-進食:" + sel_data(dt, "param_eating_self") + "</param_eating_self>";
            xml += "<param_garb>" + "自我照顧能力-穿衣:" + sel_data(dt, "param_garb") + "</param_garb>";
            xml += "<param_bathe>" + "自我照顧能力-沐浴:" + sel_data(dt, "param_bathe") + "</param_bathe>";
            xml += "<param_toilet>" + "自我照顧能力-如廁:" + sel_data(dt, "param_toilet") + "</param_toilet>";
            xml += "<param_bed_activities>" + "自我照顧能力-床上活動:" + sel_data(dt, "param_bed_activities") + "</param_bed_activities>";
            #endregion
            #region --身體評估--
            xml += "<bt_part>" + "生命徵象-體溫:測量部位" + sel_data(dt, "bt_part") + ";測量值:" + sel_data(dt, "bt_record") + "℃</bt_part>";
            xml += "<mp_part>" + "生命徵象-心跳:測量部位" + sel_data(dt, "mp_part") + ";心跳速率:" + sel_data(dt, "mp_record") + "次/分</mp_part>";
            xml += "<bf_record>" + "生命徵象-呼吸:呼吸速率" + sel_data(dt, "bf_record") + "次/分</bf_record>";
            xml += "<sp_record>";
            if (sel_data(dt, "sp_record") != "")
            {
                xml += "SpO2:" + sel_data(dt, "sp_record") + "％";
            }
            xml += "</sp_record>";
            xml += "<bp_position>" + "生命徵象-血壓:測量部位" + sel_data(dt, "bp_position") + ";測量姿勢:" + sel_data(dt, "bp_posture");
            xml += ";收縮壓:" + sel_data(dt, "bp_record_s") + "mmHg";
            xml += ";舒張壓:" + sel_data(dt, "bp_record_d") + "mmHg";
            xml += "</bp_position>";

            xml += "<param_BodyHeight>";
            if (sel_data(dt, "param_BodyHeight") != "")
            {
                xml += "身體量測-身高:" + sel_data(dt, "param_BodyHeight") + "cm";
            }
            xml += "</param_BodyHeight>";
            xml += "<param_BodyWeight>";
            if (sel_data(dt, "param_BodyWeight") != "")
            {
                xml += "身體量測-體重:" + sel_data(dt, "param_BodyWeight") + "kg";
            }
            xml += "</param_BodyWeight>";
            xml += "<bmiHint>";
            if (sel_data(dt, "param_BodyHeight") != "" && sel_data(dt, "param_BodyWeight") != "" && sel_data(dt, "param_BodyWeight") != null && sel_data(dt, "param_BodyHeight") != null)
            {
                xml += "身體質量指數(BMI):";
                string advice = string.Empty;
                string Str_bmi = BMI_Compute(Convert.ToDouble(sel_data(dt, "param_BodyHeight")), Convert.ToDouble(sel_data(dt, "param_BodyWeight")));
                double result = Convert.ToDouble(Str_bmi);
                if (result < 18.5)
                    advice = "體重過輕";
                else if (result < 24)
                    advice = "";
                else if (result < 27)
                    advice = "過重";
                else if (result < 30)
                    advice = "輕度肥胖";
                else if (result < 35)
                    advice = "中度肥胖";
                else
                    advice = "重度肥胖";
                xml += advice + " " + Str_bmi;
            }
            xml += "</bmiHint>";
            xml += "<gtbl_record>";
            if (sel_data(dt, "gtbl_record") != "")
            {
                xml += "腹圍:" + sel_data(dt, "gtbl_record") + "cm";
            }
            xml += "</gtbl_record>";
            xml += "<gtbu_record>";
            if (sel_data(dt, "gtbu_record") != "")
            {
                xml += "胸圍:" + sel_data(dt, "gtbu_record") + "cm";
            }
            xml += "</gtbu_record>";
            xml += "<gtlt_record>";
            if (sel_data(dt, "gtlt_record") != "")
            {
                xml += "大腿圍左:" + sel_data(dt, "gtlt_record") + "cm";
            }
            xml += "</gtlt_record>";
            xml += "<gtrt_record>";
            if (sel_data(dt, "gtrt_record") != "")
            {
                xml += "大腿圍右:" + sel_data(dt, "gtrt_record") + "cm";
            }
            xml += "</gtrt_record>";
            xml += "<gtll_record>";
            if (sel_data(dt, "gtll_record") != "")
            {
                xml += "小腿圍左:" + sel_data(dt, "gtll_record") + "cm";
            }
            xml += "</gtll_record>";
            xml += "<gtrl_record>";
            if (sel_data(dt, "gtrl_record") != "")
            {
                xml += "小腿圍右:" + sel_data(dt, "gtrl_record") + "cm";
            }
            xml += "</gtrl_record>";
            xml += "<param_posture>" + "體態:" + sel_data(dt, "param_posture") + "</param_posture>";
            xml += "<param_vs_conscious>" + "意識狀況-型態:" + sel_data(dt, "param_vs_conscious");
            xml += "</param_vs_conscious>";
            // if(sel_data(dt, "param_vs_conscious") == "欠清")
            //{
            // xml = xml + ";";
            string Temp_vs_conscious_item_other = "";
            string[] vs_conscious_item_other = sel_data(dt, "param_vs_conscious_item_other").Split(',');
            for (int i = 0; i < vs_conscious_item_other.Length; i++)
            {
                if (vs_conscious_item_other[i] == "其他")
                {
                    Temp_vs_conscious_item_other += vs_conscious_item_other[i] + ":" + sel_data(dt, "param_vs_conscious_item_other_txt") + ",";
                }
                else
                {
                    Temp_vs_conscious_item_other += vs_conscious_item_other[i] + ",";
                }
            }
            Temp_vs_conscious_item_other = Temp_vs_conscious_item_other.Substring(0, Temp_vs_conscious_item_other.Length - 1);
            xml += "<param_vs_conscious_item_other>";
            if (Temp_vs_conscious_item_other != "")
            {
                xml += "欠清:" + Temp_vs_conscious_item_other;
            }
            xml += "</param_vs_conscious_item_other>";
            // }
            xml += "<gc_r1>";
            if (sel_data(dt, "gc_r1") != "")
            {
                xml += "GCS-Eyes:" + sel_data(dt, "gc_r1");
            }
            xml += "</gc_r1>";
            xml += "<gc_r2>";
            if (sel_data(dt, "gc_r2") != "")
            {
                xml += "GCS-Verbal:" + sel_data(dt, "gc_r2");
            }
            xml += "</gc_r2>";
            xml += "<gc_r3>";
            if (sel_data(dt, "gc_r3") != "")
            {
                xml += "GCS-Motor:" + sel_data(dt, "gc_r3");
            }
            xml += "</gc_r3>";
            if (sel_data(dt, "gc_r8") == "其他原因致無法測量")
            {
                xml += "<gc_r8>";
                if (sel_data(dt, "gc_r8") != "")
                {
                    xml += "Muscle Power-右上肢:" + sel_data(dt, "gc_r8") + sel_data(dt, "param_gc_r8_other_txt");
                }
                xml += "</gc_r8>";
            }
            else
            {
                xml += "<gc_r8>";
                if (sel_data(dt, "gc_r8") != "")
                {
                    xml += "Muscle Power-右上肢:" + sel_data(dt, "gc_r8");
                }
                xml += "</gc_r8>";
            }
            if (sel_data(dt, "gc_r9") == "其他原因致無法測量")
            {
                xml += "<gc_r9>";
                if (sel_data(dt, "gc_r9") != "")
                {
                    xml += "Muscle Power-左上肢:" + sel_data(dt, "gc_r9") + sel_data(dt, "param_gc_r9_other_txt");
                }
                xml += "</gc_r9>";
            }
            else
            {
                xml += "<gc_r9>";
                if (sel_data(dt, "gc_r9") != "")
                {
                    xml += "Muscle Power-左上肢:" + sel_data(dt, "gc_r9");
                }
                xml += "</gc_r9>";
            }
            if (sel_data(dt, "gc_r10") == "其他原因致無法測量")
            {
                xml += "<gc_r10>";
                if (sel_data(dt, "gc_r10") != "")
                {
                    xml += "Muscle Power-右下肢:" + sel_data(dt, "gc_r10") + sel_data(dt, "param_gc_r10_other_txt");
                }
                xml += "</gc_r10>";
            }
            else
            {
                xml += "<gc_r10>";
                if (sel_data(dt, "gc_r10") != "")
                {
                    xml += "Muscle Power-右下肢:" + sel_data(dt, "gc_r10");
                }
                xml += "</gc_r10>";
            }
            if (sel_data(dt, "gc_r11") == "其他原因致無法測量")
            {
                xml += "<gc_r11>";
                if (sel_data(dt, "gc_r11") != "")
                {
                    xml += "Muscle Power-左下肢:" + sel_data(dt, "gc_r11") + sel_data(dt, "param_gc_r11_other_txt");
                }
                xml += "</gc_r11>";
            }
            else
            {
                xml += "<gc_r11>";
                if (sel_data(dt, "gc_r11") != "")
                {
                    xml += "Muscle Power-左下肢:" + sel_data(dt, "gc_r11");
                }
                xml += "</gc_r11>";
            }
            xml += "<param_pupil_reflection_r>" + "瞳孔(右):反射";
            if (sel_data(dt, "param_pupil_reflection_r") == "無法睜眼" || sel_data(dt, "param_pupil_reflection_r") == "無法評估")
            {
                xml += sel_data(dt, "param_pupil_reflection_r");
            }
            else
            {
                if (sel_data(dt, "param_pupil_reflection_r") != "其他")
                {
                    xml += "大小(mm):" + sel_data(dt, "param_pupil_reflection_r") + sel_data(dt, "param_pupil_size_r") + ";";
                }
                else
                {
                    xml += sel_data(dt, "param_pupil_reflection_r5_other_txt") + ";";
                }
            }
            xml += "</param_pupil_reflection_r>";
            xml += "<param_pupil_reflection_l>" + "瞳孔(左):反射";
            if (sel_data(dt, "param_pupil_reflection_l") == "無法睜眼" || sel_data(dt, "param_pupil_reflection_l") == "無法評估")
            {
                xml += sel_data(dt, "param_pupil_reflection_l") + ";";
            }
            else
            {
                if (sel_data(dt, "param_pupil_reflection_l") != "其他")
                {
                    xml += "大小(mm):" + sel_data(dt, "param_pupil_reflection_l") + sel_data(dt, "param_pupil_size_l") + ";";
                }
                else
                {
                    xml += sel_data(dt, "param_pupil_reflection_l5_other_txt") + ";";
                }
            }
            xml += "</param_pupil_reflection_l>";
            xml += "<param_spirit>" + "精神狀況:" + sel_data(dt, "param_spirit");
            xml += "</param_spirit>";
            xml += "<param_spirit_deviant>";
            if (sel_data(dt, "param_spirit") == "異常")
            {
                xml += "異常:";
                string[] spirit_deviant = sel_data(dt, "param_spirit_deviant").Split(',');
                for (int i = 0; i < spirit_deviant.Length; i++)
                {
                    if (spirit_deviant[i] == "其他")
                    {
                        xml += spirit_deviant[i] + ":" + sel_data(dt, "param_spirit_deviant_txt") + ",";
                    }
                    else
                    {
                        xml += spirit_deviant[i] + ",";
                    }
                }
                xml = xml.Substring(0, xml.Length - 1);
            }
            xml += "</param_spirit_deviant>";
            xml += "<param_nerve>" + "神經系統:" + sel_data(dt, "param_nerve");
            xml += "</param_nerve>";
            xml += "<param_nerve_deviant>";
            if (sel_data(dt, "param_nerve") == "異常")
            {
                xml += "異常:";
                string[] nerve_deviant = sel_data(dt, "param_nerve_deviant").Split(',');
                for (int i = 0; i < nerve_deviant.Length; i++)
                {
                    if (nerve_deviant[i] == "其他")
                    {
                        xml += nerve_deviant[i] + ":" + sel_data(dt, "param_nerve_deviant_txt") + ",";
                    }
                    else
                    {
                        xml += nerve_deviant[i] + ",";
                    }
                }
                xml = xml.Substring(0, xml.Length - 1);
            }
            xml += "</param_nerve_deviant>";
            xml += "<param_feel_other>" + "神經系統-感覺:" + sel_data(dt, "param_feel_other");
            string[] feel_other_other = sel_data(dt, "param_feel_other_other").Split(',');
            if (sel_data(dt, "param_feel_other") == "異常")
            {
                xml = xml + ";" + sel_data(dt, "param_feel_other_other");
            }
            xml += "</param_feel_other>";
            xml += "<param_feel_other_1>";//酸 部位:";
            for (int i = 0; i < feel_other_other.Length; i++)
            {
                if (feel_other_other[i] == "酸")
                {
                    xml += "酸 部位:";
                    string[] feel_other_1 = sel_data(dt, "param_feel_other_1").Split(',');
                    for (int j = 0; j < feel_other_1.Length; j++)
                    {
                        if (feel_other_1[j] == "其他")
                        {
                            xml += feel_other_1[j] + ":" + sel_data(dt, "param_feel_other_other_1_name") + ",";
                        }
                        else
                        {
                            xml += feel_other_1[j] + ",";
                        }
                    }
                    xml = xml.Substring(0, xml.Length - 1);
                }
            }
            xml += "</param_feel_other_1>";
            xml += "<param_feel_other_2>";//痛 部位:";
            for (int i = 0; i < feel_other_other.Length; i++)
            {
                if (feel_other_other[i] == "痛")
                {
                    xml += "痛 部位:";
                    string[] feel_other_2 = sel_data(dt, "param_feel_other_2").Split(',');
                    for (int j = 0; j < feel_other_2.Length; j++)
                    {
                        if (feel_other_2[j] == "其他")
                        {
                            xml += feel_other_2[j] + ":" + sel_data(dt, "param_feel_other_other_2_name") + ",";
                        }
                        else
                        {
                            xml += feel_other_2[j] + ",";
                        }
                    }
                    xml = xml.Substring(0, xml.Length - 1);
                }
            }
            xml += "</param_feel_other_2>";
            xml += "<param_feel_other_3>";//麻 部位:";
            for (int i = 0; i < feel_other_other.Length; i++)
            {
                if (feel_other_other[i] == "麻")
                {
                    xml += "麻 部位:";
                    string[] feel_other_3 = sel_data(dt, "param_feel_other_3").Split(',');
                    for (int j = 0; j < feel_other_3.Length; j++)
                    {
                        if (feel_other_3[j] == "其他")
                        {
                            xml += feel_other_3[j] + ":" + sel_data(dt, "param_feel_other_other_3_name") + ",";
                        }
                        else
                        {
                            xml += feel_other_3[j] + ",";
                        }
                    }
                    xml = xml.Substring(0, xml.Length - 1);
                }
            }
            xml += "</param_feel_other_3>";
            xml += "<param_feel_other_4>";//無知覺 部位:";
            for (int i = 0; i < feel_other_other.Length; i++)
            {
                if (feel_other_other[i] == "無知覺")
                {
                    xml += "無知覺 部位:";
                    string[] feel_other_4 = sel_data(dt, "param_feel_other_4").Split(',');
                    for (int j = 0; j < feel_other_4.Length; j++)
                    {
                        if (feel_other_4[j] == "其他")
                        {
                            xml += feel_other_4[j] + ":" + sel_data(dt, "param_feel_other_other_4_name") + ",";
                        }
                        else
                        {
                            xml += feel_other_4[j] + ",";
                        }
                    }
                    xml = xml.Substring(0, xml.Length - 1);
                }
            }
            xml += "</param_feel_other_4>";
            xml += "<param_feel_other_5>";//抽搐 部位:";
            for (int i = 0; i < feel_other_other.Length; i++)
            {
                if (feel_other_other[i] == "抽搐")
                {
                    xml += "抽搐 部位:";
                    string[] feel_other_5 = sel_data(dt, "param_feel_other_5").Split(',');
                    for (int j = 0; j < feel_other_5.Length; j++)
                    {
                        if (feel_other_5[j] == "其他")
                        {
                            xml += feel_other_5[j] + ":" + sel_data(dt, "param_feel_other_other_5_name") + ",";
                        }
                        else
                        {
                            xml += feel_other_5[j] + ",";
                        }
                    }
                    xml = xml.Substring(0, xml.Length - 1);
                }
            }
            xml += "</param_feel_other_5>";
            xml += "<param_vision>" + "感官知覺-視力:" + sel_data(dt, "param_vision");
            if (sel_data(dt, "param_vision") == "異常")
            {
                xml = xml + ";" + sel_data(dt, "param_vision_deviant");
            }
            xml += "</param_vision>";
            string[] vision_deviant = sel_data(dt, "param_vision_deviant").Split(',');
            //for(int i = 0; i < vision_deviant.Length; i++)
            //{
            //    xml += vision_deviant[i] + ",";
            //}
            //xml = xml.Substring(0, xml.Length - 1);
            xml += "<param_vision_deviant_1>";//失明:";
            for (int i = 0; i < vision_deviant.Length; i++)
            {
                if (vision_deviant[i] == "失明")
                {
                    xml += "失明:";
                    string[] vision_deviant_1 = sel_data(dt, "param_vision_deviant_1").Split(',');
                    for (int j = 0; j < vision_deviant_1.Length; j++)
                    {
                        xml += vision_deviant_1[j] + ",";
                    }
                    xml = xml.Substring(0, xml.Length - 1);
                }
            }
            xml += "</param_vision_deviant_1>";
            xml += "<param_vision_deviant_2>";//偏盲:";
            for (int i = 0; i < vision_deviant.Length; i++)
            {
                if (vision_deviant[i] == "偏盲")
                {
                    xml += "偏盲:";
                    string[] vision_deviant_2 = sel_data(dt, "param_vision_deviant_2").Split(',');
                    for (int j = 0; j < vision_deviant_2.Length; j++)
                    {
                        xml += vision_deviant_2[j] + ",";
                    }
                    xml = xml.Substring(0, xml.Length - 1);
                }
            }
            xml += "</param_vision_deviant_2>";
            xml += "<param_vision_deviant_3>";//複視:";
            for (int i = 0; i < vision_deviant.Length; i++)
            {
                if (vision_deviant[i] == "複視")
                {
                    xml += "複視:";
                    string[] vision_deviant_3 = sel_data(dt, "param_vision_deviant_3").Split(',');
                    for (int j = 0; j < vision_deviant_3.Length; j++)
                    {
                        xml += vision_deviant_3[j] + ",";
                    }
                    xml = xml.Substring(0, xml.Length - 1);
                }
            }
            xml += "</param_vision_deviant_3>";
            xml += "<param_vision_deviant_4>";//視力模糊:";
            for (int i = 0; i < vision_deviant.Length; i++)
            {
                if (vision_deviant[i] == "視力模糊")
                {
                    xml += "視力模糊:";
                    string[] vision_deviant_4 = sel_data(dt, "param_vision_deviant_4").Split(',');
                    for (int j = 0; j < vision_deviant_4.Length; j++)
                    {
                        xml += vision_deviant_4[j] + ",";
                    }
                    xml = xml.Substring(0, xml.Length - 1);
                }
            }
            xml += "</param_vision_deviant_4>";
            xml += "<param_auxiliary>" + "視力-輔助物:" + sel_data(dt, "param_auxiliary");
            string[] auxiliary_other = sel_data(dt, "param_auxiliary_other").Split(',');
            if (sel_data(dt, "param_auxiliary") == "有")
            {
                xml = xml + ";" + sel_data(dt, "param_auxiliary_other");
            }
            xml += "</param_auxiliary>";
            //for(int i = 0; i < auxiliary_other.Length; i++)
            //{
            //    xml += auxiliary_other[i] + ",";
            //}
            //xml = xml.Substring(0, xml.Length - 1);
            xml += "<param_auxiliary_other_2_rb>";//義眼:";
            for (int i = 0; i < auxiliary_other.Length; i++)
            {
                if (auxiliary_other[i] == "義眼")
                {
                    xml = xml + "義眼:";
                    string[] auxiliary_other_2_rb = sel_data(dt, "param_auxiliary_other_2_rb").Split(',');
                    for (int j = 0; j < auxiliary_other_2_rb.Length; j++)
                    {
                        xml += auxiliary_other_2_rb[j] + ",";
                    }
                    xml = xml.Substring(0, xml.Length - 1);
                }
            }
            xml += "</param_auxiliary_other_2_rb>";
            xml += "<param_hearing>" + "感官知覺-聽力:" + sel_data(dt, "param_hearing");
            if (sel_data(dt, "param_hearing") == "障礙")
            {
                xml += ";" + sel_data(dt, "param_hearing_other");
            }
            xml += "</param_hearing>";
            xml += "<param_auxiliary_other_1_ck>";//重聽:";
            if (sel_data(dt, "param_hearing_other") == "重聽")
            {
                xml = xml + "重聽:";
                string[] auxiliary_other_1_ck = sel_data(dt, "param_auxiliary_other_1_ck").Split(',');
                for (int i = 0; i < auxiliary_other_1_ck.Length; i++)
                {
                    xml += auxiliary_other_1_ck[i] + ",";
                }
                xml = xml.Substring(0, xml.Length - 1);
            }
            xml += "</param_auxiliary_other_1_ck>";
            xml += "<param_auxiliary_other_2_ck>";//失聰:";
            if (sel_data(dt, "param_hearing_other") == "失聰")
            {
                xml = xml + "失聰:";
                string[] auxiliary_other_2_ck = sel_data(dt, "param_auxiliary_other_2_ck").Split(',');
                for (int i = 0; i < auxiliary_other_2_ck.Length; i++)
                {
                    xml += auxiliary_other_2_ck[i] + ",";
                }
                xml = xml.Substring(0, xml.Length - 1);
            }
            // xml = xml.Substring(0, xml.Length - 1);
            xml += "</param_auxiliary_other_2_ck>";
            xml += "<param_auxiliary_other_3_ck>";//耳鳴:";
            if (sel_data(dt, "param_hearing_other") == "耳鳴")
            {
                xml = xml + "耳鳴:";
                string[] auxiliary_other_3_ck = sel_data(dt, "param_auxiliary_other_3_ck").Split(',');
                for (int i = 0; i < auxiliary_other_3_ck.Length; i++)
                {
                    xml += auxiliary_other_3_ck[i] + ",";
                }
                xml = xml.Substring(0, xml.Length - 1);
            }
            xml += "</param_auxiliary_other_3_ck>";
            xml += "<param_audiphones>" + "使用助聽器:" + sel_data(dt, "param_audiphones");
            if (sel_data(dt, "param_audiphones") == "有")
            {
                xml = xml + ";";
                string[] audiphones_deviant = sel_data(dt, "param_audiphones_deviant").Split(',');
                for (int i = 0; i < audiphones_deviant.Length; i++)
                {
                    xml += audiphones_deviant[i] + ",";
                }
                xml = xml.Substring(0, xml.Length - 1);
            }
            xml += "</param_audiphones>";
            xml += "<param_nose>" + "感官知覺-嗅覺:" + sel_data(dt, "param_nose") + "</param_nose>";
            xml += "<param_taste>" + "感官知覺-味覺:" + sel_data(dt, "param_taste") + "</param_taste>";
            xml += "<param_sensory_perception_other_txt>";
            if (sel_data(dt, "param_sensory_perception_other_txt") != "")
            {
                xml += "感官知覺-其他:" + sel_data(dt, "param_sensory_perception_other_txt");
            }
            xml += "</param_sensory_perception_other_txt>";

            xml += "<param_Breathing_Type>" + "呼吸型態:" + sel_data(dt, "param_Breathing_Type");
            if (sel_data(dt, "param_Breathing_Type") == "異常")
            {
                xml = xml + ";";
                string[] Breathing_Type_Abnormal = sel_data(dt, "param_Breathing_Type_Abnormal").Split(',');
                for (int i = 0; i < Breathing_Type_Abnormal.Length; i++)
                {
                    if (Breathing_Type_Abnormal[i] == "其他")
                    {
                        xml += Breathing_Type_Abnormal[i] + ":" + sel_data(dt, "param_Breathing_Type_other_txt") + ",";
                    }
                    else
                    {
                        xml += Breathing_Type_Abnormal[i] + ",";
                    }
                }
                xml = xml.Substring(0, xml.Length - 1);
            }
            xml += "</param_Breathing_Type>";
            xml += "<param_respiratory_therapy>" + "醫療輔助器:" + sel_data(dt, "param_respiratory_therapy");
            string sadpjjwd = sel_data(dt, "param_respiratory_therapy");
            if (sel_data(dt, "param_respiratory_therapy") == "有")
            {
                xml = xml + ";" + sel_data(dt, "param_respiratory_therapy_other");
            }
            xml += "</param_respiratory_therapy>";
            string[] respiratory_therapy_other = sel_data(dt, "param_respiratory_therapy_other").Split(',');
            xml += "<param_respiratory_therapy_other_2_rb>";
            if (sel_data(dt, "param_respiratory_therapy_other_2_rb") != "")
            {
                xml += "氧氣治療:";
                for (int i = 0; i < respiratory_therapy_other.Length; i++)
                {
                    if (respiratory_therapy_other[i] == "氧氣治療")
                    {
                        xml += sel_data(dt, "param_respiratory_therapy_other_2_rb");
                    }
                }
            }
            xml += "</param_respiratory_therapy_other_2_rb>";
            xml += "<param_Ventilator_mode>";
            if (sel_data(dt, "param_respiratory_therapy_TxtFaceMask5_1") != "")
            {
                xml += "mode:" + sel_data(dt, "param_respiratory_therapy_TxtFaceMask5_1");
            }
            xml += "</param_Ventilator_mode>";
            xml += "<param_Ventilator_tv>";
            if (sel_data(dt, "param_respiratory_therapy_TxtFaceMask5_2") != "")
            {
                xml += "TV:" + sel_data(dt, "param_respiratory_therapy_TxtFaceMask5_2") + " ml";
            }
            xml += "</param_Ventilator_tv>";
            xml += "<param_Ventilator_fio>";
            if (sel_data(dt, "param_respiratory_therapy_TxtFaceMask5_3") != "")
            {
                xml += "FiO2:" + sel_data(dt, "param_respiratory_therapy_TxtFaceMask5_3") + " %";
            }
            xml += "</param_Ventilator_fio>";
            xml += "<param_Ventilator_peep>";
            if (sel_data(dt, "param_respiratory_therapy_TxtFaceMask5_4") != "")
            {
                xml += "PEEP:" + sel_data(dt, "param_respiratory_therapy_TxtFaceMask5_4") + " cmH2O";
            }
            xml += "</param_Ventilator_peep>";
            xml += "<param_Ventilator_rate>";
            if (sel_data(dt, "param_respiratory_therapy_TxtFaceMask5_5") != "")
            {
                xml += "Rate:" + sel_data(dt, "param_respiratory_therapy_TxtFaceMask5_5") + " 次/分";
            }
            xml += "</param_Ventilator_rate>";
            xml += "<param_Breathing_LeftVoive>" + "呼吸音-左側:" + sel_data(dt, "param_Breathing_LeftVoive");
            if (sel_data(dt, "param_Breathing_LeftVoive") == "異常")
            {
                xml = xml + ";";
                if (sel_data(dt, "param_Breathing_LeftVoive_Abnormal").IndexOf("其他") != -1)
                {
                    xml += sel_data(dt, "param_Breathing_LeftVoive_Abnormal") + ":" + sel_data(dt, "param_Breathing_LeftVoive_Abnormal_other");
                }
                else
                {
                    xml += sel_data(dt, "param_Breathing_LeftVoive_Abnormal");
                }
            }
            xml += "</param_Breathing_LeftVoive>";
            xml += "<param_Breathing_RightVoive>" + "呼吸音-右側:" + sel_data(dt, "param_Breathing_RightVoive");
            if (sel_data(dt, "param_Breathing_RightVoive") == "異常")
            {
                xml = xml + ";";
                if (sel_data(dt, "param_Breathing_RightVoive_Abnormal").IndexOf("其他") != -1)
                {
                    xml += sel_data(dt, "param_Breathing_RightVoive_Abnormal") + ":" + sel_data(dt, "param_Breathing_RightVoive_Abnormal_other");
                }
                else
                {
                    xml += sel_data(dt, "param_Breathing_RightVoive_Abnormal");
                }
            }
            xml += "</param_Breathing_RightVoive>";
            xml += "<param_Sputum_Amount>" + "痰液:" + sel_data(dt, "param_Sputum_Amount");
            xml += "</param_Sputum_Amount>";
            xml += "<param_Sputum_Amount_Option>";
            if (sel_data(dt, "param_Sputum_Amount") == "有")
            {
                xml += "痰液量:" + sel_data(dt, "param_Sputum_Amount_Option");
            }
            xml += "</param_Sputum_Amount_Option>";
            xml += "<param_Sputum_Amount_Color>";
            if (sel_data(dt, "param_Sputum_Amount") == "有")
            {
                if (sel_data(dt, "param_Sputum_Amount_Color") != "其他")
                {
                    xml += "痰液顏色:" + sel_data(dt, "param_Sputum_Amount_Color");
                }
                else
                {
                    xml += "痰液顏色:" + sel_data(dt, "param_Sputum_Amount_Color") + ":" + sel_data(dt, "param_Sputum_Amount_Color_other");
                }
            }
            xml += "</param_Sputum_Amount_Color>";
            xml += "<param_Sputum_Amount_Type>";
            if (sel_data(dt, "param_Sputum_Amount") == "有")
            {
                if (sel_data(dt, "param_Sputum_Amount_Type") != "其他")
                {
                    xml += "痰液性質:" + sel_data(dt, "param_Sputum_Amount_Type");
                }
                else
                {
                    xml += "痰液性質:" + sel_data(dt, "param_Sputum_Amount_Type") + ":" + sel_data(dt, "param_Sputum_Amount_Type_other");
                }
            }
            xml += "</param_Sputum_Amount_Type>";
            //}
            xml += "<param_Heart_strength>" + "脈搏強度:" + sel_data(dt, "param_Heart_strength") + "</param_Heart_strength>";
            xml += "<param_Heart_Rhythm>" + "脈搏心律:" + sel_data(dt, "param_Heart_Rhythm") + "</param_Heart_Rhythm>";
            xml += "<param_Peripheral_circulation>" + "末梢:" + sel_data(dt, "param_Peripheral_circulation");
            //string[] Peripheral_circulation_other = sel_data(dt, "param_Peripheral_circulation_other").Split(',');
            if (sel_data(dt, "param_Peripheral_circulation") == "異常")
            {
                xml = xml + ";" + sel_data(dt, "param_Peripheral_circulation_other");
                //for(int i = 0; i < Peripheral_circulation_other.Length; i++)
                //{
                //    xml += Peripheral_circulation_other[i] + ",";
                //}
                //xml = xml.Substring(0, xml.Length - 1);
            }
            xml += "</param_Peripheral_circulation>";
            //for(int i = 0; i < Peripheral_circulation_other.Length; i++)
            //{
            //    if(Peripheral_circulation_other[i] == "水腫")
            //    {
            //xml = xml + ";";
            //xml += "<param_Peripheral_circulation_other_Edema>水腫:" + sel_data(dt, "param_Peripheral_circulation_other_Edema");
            //xml += "</param_Peripheral_circulation_other_Edema>";
            //}
            //else if(Peripheral_circulation_other[i] == "其他")
            //{
            //xml = xml + ";";
            xml += "<param_Peripheral_circulation_other_txt>";
            if (sel_data(dt, "param_Peripheral_circulation_other_txt") != "")
            {
                xml += "其他:" + sel_data(dt, "param_Peripheral_circulation_other_txt");
            }
            xml += "</param_Peripheral_circulation_other_txt>";
            //    }
            //}
            xml += "<param_LeftFoot_Artery_Strength>" + "足背動脈強度:左" + sel_data(dt, "param_LeftFoot_Artery_Strength") + "</param_LeftFoot_Artery_Strength>";
            xml += "<param_RightFoot_Artery_Strength>" + "足背動脈強度:右" + sel_data(dt, "param_RightFoot_Artery_Strength") + "</param_RightFoot_Artery_Strength>";

            xml += "<param_Appetite>" + "食慾:" + sel_data(dt, "param_Appetite");
            if (sel_data(dt, "param_Appetite") == "異常")
            {
                xml = xml + ";";
                string[] Appetite_Abnormal = sel_data(dt, "param_Appetite_Abnormal").Split(',');
                for (int i = 0; i < Appetite_Abnormal.Length; i++)
                {
                    if (Appetite_Abnormal[i] == "其他")
                    {
                        xml += Appetite_Abnormal[i] + ":" + sel_data(dt, "param_Appetite_Abnormal_other") + ",";
                    }
                    else
                    {
                        xml += Appetite_Abnormal[i] + ",";
                    }
                }
                xml = xml.Substring(0, xml.Length - 1);
            }
            xml += "</param_Appetite>";
            xml += "<param_Swallowing>" + "吞嚥能力:" + sel_data(dt, "param_Swallowing");
            if (sel_data(dt, "param_Swallowing") == "異常")
            {
                xml = xml + ";";
                string[] SwallowingStatus = sel_data(dt, "param_SwallowingStatus").Split(',');
                for (int i = 0; i < SwallowingStatus.Length; i++)
                {
                    if (SwallowingStatus[i] == "其他")
                    {
                        xml += SwallowingStatus[i] + ":" + sel_data(dt, "param_SwallowingStatus_other_txt") + ",";
                    }
                    else
                    {
                        xml += SwallowingStatus[i] + ",";
                    }
                }
                xml = xml.Substring(0, xml.Length - 1);
            }
            xml += "</param_Swallowing>";
            /*------------------------------需往前檢視上面是否有類似"神經系統-感覺"，酸、痛部位的顯示方式---需對照成人入評txt資料--------------------*/
            xml += "<param_Eating>進食方式:" + sel_data(dt, "param_Eating");
            if (sel_data(dt, "param_Eating_other") == "其它方式")
            {
                xml = xml + ";";
                xml += "其它方式" + sel_data(dt, "param_SwallowingStatus_other_txt0");
            }
            else
            {
                xml = xml + ";";
                xml += sel_data(dt, "param_Eating_other");
            }
            xml += "</param_Eating>";
            xml += "<param_SwallowingStatus_other_txt1>";
            if (sel_data(dt, "param_SwallowingStatus_other_txt1") != "")
            {
                xml += "fr.:" + sel_data(dt, "param_SwallowingStatus_other_txt1");
            }
            xml += "</param_SwallowingStatus_other_txt1>";
            xml += "<param_SwallowingStatus_other_txt2>";
            if (sel_data(dt, "param_SwallowingStatus_other_txt2") != "")
            {
                xml += "fix:" + sel_data(dt, "param_SwallowingStatus_other_txt2");
            }
            xml += "</param_SwallowingStatus_other_txt2>";
            xml += "<param_Eating_NG_Feeding>";
            if (sel_data(dt, "param_Eating_NG_Feeding") != "")
            {
                xml += "輔助物:" + sel_data(dt, "param_Eating_NG_Feeding");
            }
            xml += "</param_Eating_NG_Feeding>";
            xml += "<param_FoodKind>飲食種類:" + sel_data(dt, "param_FoodKind") + "</param_FoodKind>";
            if (sel_data(dt, "param_FoodKind_Tube") == "其他")
            {
                xml += "<param_FoodKind_Tube>" + sel_data(dt, "param_FoodKind_Tube") + ":" + sel_data(dt, "param_FoodKind_Desc_other_txt") + "</param_FoodKind_Tube>";
            }
            else
            {
                xml += "<param_FoodKind_Tube>" + sel_data(dt, "param_FoodKind_Tube") + "</param_FoodKind_Tube>";
            }
            xml += "<param_Abdominal_palpation>腹部評估-觸診:" + sel_data(dt, "param_Abdominal_palpation") + ";";
            string[] Abdominal_palpation_Tube = sel_data(dt, "param_Abdominal_palpation_Tube").Split(',');
            for (int i = 0; i < Abdominal_palpation_Tube.Length; i++)
            {
                if (Abdominal_palpation_Tube[i] != "其他")
                {
                    xml += Abdominal_palpation_Tube[i] + ",";
                }
                else
                {
                    xml += Abdominal_palpation_Tube[i] + ":" + sel_data(dt, "param_Abdominal_palpation_Desc_other_txt") + ",";
                }
            }
            xml = xml.Substring(0, xml.Length - 1);
            xml += "</param_Abdominal_palpation>";
            string Temp_PeristalsisStatus_txt = (sel_data(dt, "param_PeristalsisStatus") == "無評估需求") ? "無評估需求" : sel_data(dt, "param_PeristalsisStatus_txt") + "次/分";
            xml += "<param_PeristalsisStatus_txt>腸蠕動:" + Temp_PeristalsisStatus_txt + "</param_PeristalsisStatus_txt>";
            xml += "<param_PeristalsisStatus_voice>腸音:" + sel_data(dt, "param_PeristalsisStatus_voice") + "</param_PeristalsisStatus_voice>";
            xml += "<param_Decompression>Decompression:" + sel_data(dt, "param_Decompression");
            if (sel_data(dt, "param_Decompression") == "有")
            {
                xml += ";量:" + sel_data(dt, "param_Decompression_Option");
            }
            xml += "</param_Decompression>";
            string Temp_Decompression_Color = (sel_data(dt, "param_Decompression_Color") == "其他") ? "其他:" + sel_data(dt, "param_Decompression_Color_other") : sel_data(dt, "param_Decompression_Color");
            xml += "<param_Decompression_Color>";
            if (sel_data(dt, "param_Decompression") == "有")
            {
                xml += "顏色:" + Temp_Decompression_Color;
            }
            xml += "</param_Decompression_Color>";
            string Temp_Decompression_Type = (sel_data(dt, "param_Decompression_Type") == "其他") ? "其他:" + sel_data(dt, "param_Decompression_Type_other") : sel_data(dt, "param_Decompression_Type");
            xml += "<param_Decompression_Type>";
            if (sel_data(dt, "param_Decompression") == "有")
            {
                xml += "性質:" + Temp_Decompression_Type;
            }
            xml += "</param_Decompression_Type>";
            xml += "<param_excrete>排尿:" + sel_data(dt, "param_excrete");
            if (sel_data(dt, "param_excrete") == "異常")
            {
                xml += ";";
                string[] Temp_excrete_Option = sel_data(dt, "param_excrete_Option").Split(',');
                for (int i = 0; i < Temp_excrete_Option.Length; i++)
                {
                    if (Temp_excrete_Option[i] != "其他")
                    {
                        xml += Temp_excrete_Option[i] + ",";
                    }
                    else
                    {
                        xml += Temp_excrete_Option[i] + ":" + sel_data(dt, "span_param_excrete_Option_txt") + ",";
                    }
                }
                xml = xml.Substring(0, xml.Length - 1);
            }
            xml += "</param_excrete>";
            string Temp_urination = (sel_data(dt, "param_urination") == "其他") ? "其他:" + sel_data(dt, "span_param_urination_Option_txt") : sel_data(dt, "param_urination");
            xml += "<param_urination>排尿方式:" + Temp_urination + "</param_urination>";
            xml += "<param_urine>尿液性狀:" + sel_data(dt, "param_urine") + "</param_urine>";
            xml += "<param_urine_Option>";
            if (sel_data(dt, "param_urine") == "有")
            {
                xml += "量:" + sel_data(dt, "param_urine_Option");
            }
            xml += "</param_urine_Option>";
            string Temp_urine_Color = (sel_data(dt, "param_urine_Color") == "其他") ? "其他:" + sel_data(dt, "param_urine_Color_other") : sel_data(dt, "param_urine_Color");
            xml += "<param_urine_Color>";
            if (sel_data(dt, "param_urine") == "有")
            {
                xml += "顏色:" + Temp_urine_Color;
            }
            xml += "</param_urine_Color>";
            string Temp_urine_Type = (sel_data(dt, "param_urine_Type") == "其他") ? "其他:" + sel_data(dt, "param_urine_Type_other") : sel_data(dt, "param_urine_Type");
            xml += "<param_urine_Type>";
            if (sel_data(dt, "param_urine") == "有")
            {
                xml += "性質:" + Temp_urine_Type;
            }
            xml += "</param_urine_Type>";
            xml += "<param_defecation>排便:" + sel_data(dt, "param_defecation") + ";";
            string[] defecation_Option = sel_data(dt, "param_defecation_Option").Split(',');
            for (int i = 0; i < defecation_Option.Length; i++)
            {
                if (defecation_Option[i] != "其他")
                {
                    xml += defecation_Option[i] + ",";
                }
                else
                {
                    xml += defecation_Option[i] + ":" + sel_data(dt, "param_defecation_Option_txt") + ",";
                }
            }
            xml = xml.Substring(0, xml.Length - 1);
            xml += "</param_defecation>";
            xml += "<param_Peripheral_circulation_other_1_rb>";
            if (sel_data(dt, "param_Peripheral_circulation_other_1_rb") != "")
            {
                xml += "便秘:" + sel_data(dt, "param_Peripheral_circulation_other_1_rb");
            }
            xml += "</param_Peripheral_circulation_other_1_rb>";
            //if (sel_data(dt, "param_Light") == "男")
            //{
            xml += "<param_Light_Boy_status>";
            if (sel_data(dt, "param_Light") == "男")
            {
                xml += "生殖系統:" + sel_data(dt, "param_Light") + ";" + sel_data(dt, "param_Light_Boy_status") + ":";
                string[] Light_Boy_abnormal = sel_data(dt, "param_Light_Boy_abnormal").Split(',');
                for (int i = 0; i < Light_Boy_abnormal.Length; i++)
                {
                    if (Light_Boy_abnormal[i] != "其他")
                    {
                        xml += Light_Boy_abnormal[i] + ",";
                    }
                    else
                    {
                        xml += Light_Boy_abnormal[i] + ":" + sel_data(dt, "param_Light_Boy_abnormal_other") + ",";
                    }
                }
                xml = xml.Substring(0, xml.Length - 1);
            }
            xml += "</param_Light_Boy_status>";
            xml += "<param_Light_Boy_abnormal_bump_L>";
            if (sel_data(dt, "param_Light") == "男")
            {
                if (sel_data(dt, "param_Light_Boy_abnormal_bump_L") != "")
                {
                    xml += "腫塊大小長:" + sel_data(dt, "param_Light_Boy_abnormal_bump_L") + "cm";
                }
            }
            xml += "</param_Light_Boy_abnormal_bump_L>";
            xml += "<param_Light_Boy_abnormal_bump_W>";
            if (sel_data(dt, "param_Light") == "男")
            {
                if (sel_data(dt, "param_Light_Boy_abnormal_bump_W") != "")
                {
                    xml += "腫塊大小寬:" + sel_data(dt, "param_Light_Boy_abnormal_bump_W") + "cm";
                }
            }
            xml += "</param_Light_Boy_abnormal_bump_W>";
            //}
            //else
            //{
            xml += "<param_FBAbnormal_Dtl>";
            if (sel_data(dt, "param_Light") != "男")
            {
                xml += "生殖系統:" + sel_data(dt, "param_Light") + ";" + sel_data(dt, "param_FBAbnormal") + ":";
                string[] FBAbnormal_Dtl = sel_data(dt, "param_FBAbnormal_Dtl").Split(',');
                for (int i = 0; i < FBAbnormal_Dtl.Length; i++)
                {
                    if (FBAbnormal_Dtl[i] != "其他")
                    {
                        xml += FBAbnormal_Dtl[i] + ",";
                    }
                    else
                    {
                        xml += FBAbnormal_Dtl[i] + ":" + sel_data(dt, "param_FBAbnormal_Dtl_other") + ",";
                    }
                }
                xml = xml.Substring(0, xml.Length - 1);
            }
            xml += "</param_FBAbnormal_Dtl>";
            string Temp_Last_MC = (sel_data(dt, "param_Last_MC") == "遺忘或無法回答") ? "遺忘或無法回答" : sel_data(dt, "param_MCStart") + "歲";
            xml += "<param_MCStart>";
            if (sel_data(dt, "param_Light") != "男")
            {
                if (Temp_Last_MC != "歲")
                {
                    xml += "初經年齡:" + Temp_Last_MC;
                }
            }
            xml += "</param_MCStart>";
            string Temp_Last_MC_End = "";
            if (sel_data(dt, "param_Last_MC_End") == "遺忘或無法回答")
            {
                Temp_Last_MC_End = "遺忘或無法回答";
            }
            else
            {
                string Temp_param_MCEnd = (sel_data(dt, "param_MCEnd"));
                if (Temp_param_MCEnd != "")
                {
                    string param_MCEnd_txt = (sel_data(dt, "param_MCEnd_txt") != "") ? "，" + sel_data(dt, "param_MCEnd_txt") + "歲" : "";
                    Temp_Last_MC_End = (sel_data(dt, "param_MCEnd") == "是") ? "是" + param_MCEnd_txt : "否";
                }
                else
                {
                    Temp_Last_MC_End = "";
                }
            }
            xml += "<param_MCEnd>";
            if (sel_data(dt, "param_Light") != "男")
            {
                //if(Temp_Last_MC_End != "遺忘或無法回答"&&Temp_Last_MC_End != )
                if (Temp_Last_MC_End != "")
                {
                    xml += "是否已停經:" + Temp_Last_MC_End;
                }
            }
            xml += "</param_MCEnd>";
            string Temp_Last_MC_D = (sel_data(dt, "param_Last_MC_L") == "遺忘或無法回答") ? "遺忘或無法回答" : sel_data(dt, "param_MC_D");
            xml += "<param_MC_D>";
            if (sel_data(dt, "param_Light") != "男")
            {
                if (Temp_Last_MC_D != "")
                {
                    xml += "最後月經日:" + Temp_Last_MC_D;
                }
            }
            xml += "</param_MC_D>";
            string Temp_Last_MC_C = (sel_data(dt, "param_Last_MC_C") == "遺忘或無法回答") ? "遺忘或無法回答" : sel_data(dt, "param_MCCycle");
            xml += "<param_MCCycle>";
            if (sel_data(dt, "param_Light") != "男")
            {
                if (Temp_Last_MC_C != "")
                {
                    xml += "月經週期:" + Temp_Last_MC_C;
                }
            }
            xml += "</param_MCCycle>";
            string Temp_param_Last_MC_D = (sel_data(dt, "param_Last_MC_D") == "遺忘或無法回答") ? "遺忘或無法回答" : sel_data(dt, "param_MCDay");
            xml += "<param_MCDay>";
            if (sel_data(dt, "param_Light") != "男")
            {
                if (Temp_param_Last_MC_D != "")
                {
                    xml += "月經天數:" + Temp_param_Last_MC_D;
                }
            }
            xml += "</param_MCDay>";
            string Temp_Last_MC_Amount = "";
            if (sel_data(dt, "param_Last_MC_Amount") == "遺忘或無法回答")
            {
                Temp_Last_MC_Amount = "遺忘或無法回答";
            }
            else
            {
                Temp_Last_MC_Amount = sel_data(dt, "param_MCAmount");
            }
            xml += "<param_MCAmount>";
            if (sel_data(dt, "param_Light") != "男")
            {
                if (Temp_Last_MC_Amount != "")
                {
                    xml += "月經量:" + Temp_Last_MC_Amount;
                }
            }
            xml += "</param_MCAmount>";
            //string Temp_FBAbnormalDtl = (sel_data(dt, "param_FBAbnormalDtl") == "其他") ? "其他" + sel_data(dt, "param_FBAbnormalOther") : sel_data(dt, "param_FBAbnormalDtl");
            string Temp_FBAbnormalDtl = sel_data(dt, "param_FBAbnormalDtl");
            string[] Arr_FBAbnormalDtl = sel_data(dt, "param_FBAbnormalDtl").Split(',');
            xml += "<param_FBAbnormalDtl>";
            if (sel_data(dt, "param_Light") != "男")
            {
                if (Temp_FBAbnormalDtl != "")
                {
                    xml += "月經期間:";
                    for (int i = 0; i < Arr_FBAbnormalDtl.Length; i++)
                    {
                        if (Arr_FBAbnormalDtl[i] != "其他")
                        {
                            xml += Arr_FBAbnormalDtl[i] + ",";
                        }
                        else
                        {
                            xml += Arr_FBAbnormalDtl[i];
                            if (sel_data(dt, "param_FBAbnormalOther") != "")
                            {
                                xml += ":" + sel_data(dt, "param_FBAbnormalOther");
                            }
                            xml += ",";
                        }
                    }
                    xml = xml.Substring(0, xml.Length - 1);
                }
            }
            xml += "</param_FBAbnormalDtl>";
            string param_pregnancy = string.Empty;
            if (sel_data(dt, "param_pregnancy") == "是")
            {
                param_pregnancy = "是";
                if (sel_data(dt, "param_pregnancy_other_txt") != "")
                {
                    param_pregnancy += ";預產期:" + sel_data(dt, "param_pregnancy_other_txt");
                }
            }
            else
            {
                param_pregnancy = "否";
            }
            xml += "<param_pregnancy>";
            if (sel_data(dt, "param_Light") != "男")
            {
                if (sel_data(dt, "param_pregnancy") != "")
                {
                    xml += "是否懷孕:" + param_pregnancy;
                }
            }
            xml += "</param_pregnancy>";
            xml += "<param_BornHistory_G>";
            if (sel_data(dt, "param_Light") != "男")
            {
                if (sel_data(dt, "param_BornHistory_G") != "")
                {
                    xml += "懷孕次數(G):" + sel_data(dt, "param_BornHistory_G");
                }
            }
            xml += "</param_BornHistory_G>";
            xml += "<param_BornHistory_P>";
            if (sel_data(dt, "param_Light") != "男")
            {
                if (sel_data(dt, "param_BornHistory_P") != "")
                {
                    xml += "生產數(P):" + sel_data(dt, "param_BornHistory_P");
                }
            }
            xml += "</param_BornHistory_P>";
            xml += "<param_BornHistory_A>";
            if (sel_data(dt, "param_Light") != "男")
            {
                if (sel_data(dt, "param_BornHistory_A") != "")
                {
                    xml += "流產數(A):" + sel_data(dt, "param_BornHistory_A");
                }
            }
            xml += "</param_BornHistory_A>";
            xml += "<param_BornHistory_E>";
            if (sel_data(dt, "param_Light") != "男")
            {
                if (sel_data(dt, "param_BornHistory_E") != "")
                {
                    xml += "子宮外孕(E):" + sel_data(dt, "param_BornHistory_E");
                }
            }
            xml += "</param_BornHistory_E>";
            //}
            xml += "<param_skeleton>骨骼系統:" + sel_data(dt, "param_skeleton") + ";" + sel_data(dt, "param_skeleton_Desc") + "</param_skeleton>";
            xml += "<param_skeleton_Desc_fracture>";
            if (sel_data(dt, "param_skeleton_Desc_fracture") != "")
            {
                xml += "骨折，部位:" + sel_data(dt, "param_skeleton_Desc_fracture");
            }
            xml += "</param_skeleton_Desc_fracture>";
            xml += "<param_skeleton_Desc_dislocation>";
            if (sel_data(dt, "param_skeleton_Desc_dislocation") != "")
            {
                xml += "脫臼，部位:" + sel_data(dt, "param_skeleton_Desc_dislocation");
            }
            xml += "</param_skeleton_Desc_dislocation>";
            xml += "<param_skeleton_Desc_arthrosis>";
            if (sel_data(dt, "param_skeleton_Desc_arthrosis") != "")
            {
                xml += "關節腫，部位:" + sel_data(dt, "param_skeleton_Desc_arthrosis");
            }
            xml += "</param_skeleton_Desc_arthrosis>";
            xml += "<param_skeleton_Desc_amputation>";
            if (sel_data(dt, "param_skeleton_Desc_amputation") != "")
            {
                xml += "截肢，部位:" + sel_data(dt, "param_skeleton_Desc_amputation");
            }
            xml += "</param_skeleton_Desc_amputation>";
            xml += "<param_skeleton_Desc_deformity>";
            if (sel_data(dt, "param_skeleton_Desc_deformity") != "")
            {
                xml += "畸形，部位:" + sel_data(dt, "param_skeleton_Desc_deformity");
            }
            xml += "</param_skeleton_Desc_deformity>";
            xml += "<param_skeleton_Desc_other>";
            if (sel_data(dt, "param_skeleton_Desc_other") != "")
            {
                xml += "其他:" + sel_data(dt, "param_skeleton_Desc_other");
            }
            xml += "</param_skeleton_Desc_other>";
            /*----------------------------------------*/
            xml += "<param_activity>活動功能:" + sel_data(dt, "param_activity") + ";";
            string[] activity_Desc = sel_data(dt, "param_activity_Desc").Split(',');
            for (int i = 0; i < activity_Desc.Length; i++)
            {
                if (activity_Desc[i] != "其他")
                {
                    xml += activity_Desc[i] + ",";
                }
                else
                {
                    xml += activity_Desc[i] + ":" + sel_data(dt, "param_activity_other") + ",";
                }
            }
            xml = xml.Substring(0, xml.Length - 1);
            xml += "</param_activity>";
            xml += "<param_Skin_Exterior>皮膚外觀:" + sel_data(dt, "param_Skin_Exterior") + "</param_Skin_Exterior>";
            xml += "<param_Skin_Exterior_Desc>";
            if (sel_data(dt, "param_Skin_Exterior") != "正常")
            {
                xml += "異常:" + sel_data(dt, "param_Skin_Exterior_Desc");
            }
            xml += "</param_Skin_Exterior_Desc>";
            xml += "<param_Skin_Exterior_Desc_Edema_extent>";
            if (sel_data(dt, "param_Skin_Exterior_Desc_Edema_extent") != "")
            {
                xml += "程度:" + sel_data(dt, "param_Skin_Exterior_Desc_Edema_extent");
            }
            xml += "</param_Skin_Exterior_Desc_Edema_extent>";

            xml += "<param_Skin_Exterior_Desc_Edema>";
            if (sel_data(dt, "param_Skin_Exterior_Desc_Edema") != "")
            {
                xml += "水腫部位:";
                string[] Skin_Exterior_Desc_Edema = sel_data(dt, "param_Skin_Exterior_Desc_Edema").Split(',');
                for (int i = 0; i < Skin_Exterior_Desc_Edema.Length; i++)
                {
                    if (Skin_Exterior_Desc_Edema[i] != "其他")
                    {
                        xml += Skin_Exterior_Desc_Edema[i] + ",";
                    }
                    else
                    {
                        xml += Skin_Exterior_Desc_Edema[i] + ":" + sel_data(dt, "param_Skin_Exterior_Desc_Edema_other") + ",";
                    }
                }
                xml = xml.Substring(0, xml.Length - 1);
            }
            xml += "</param_Skin_Exterior_Desc_Edema>";
            xml += "<param_Skin_Exterior_Desc_Rash>";
            if (sel_data(dt, "param_Skin_Exterior_Desc_Rash") != "")
            {
                xml += "紅疹部位:";
                string[] Skin_Exterior_Desc_Rash = sel_data(dt, "param_Skin_Exterior_Desc_Rash").Split(',');
                for (int i = 0; i < Skin_Exterior_Desc_Rash.Length; i++)
                {
                    if (Skin_Exterior_Desc_Rash[i] != "其他")
                    {
                        xml += Skin_Exterior_Desc_Rash[i] + ",";
                    }
                    else
                    {
                        xml += Skin_Exterior_Desc_Rash[i] + sel_data(dt, "param_Skin_Exterior_Desc_Rash_other") + ",";
                    }
                }
                xml = xml.Substring(0, xml.Length - 1);
            }
            xml += "</param_Skin_Exterior_Desc_Rash>";
            xml += "<param_Skin_Exterior_Desc_other>";
            //if(sel_data(dt, "param_skeleton_Desc_other") != "")
            //{
            //    xml += "皮膚異常其他:" + sel_data(dt, "param_skeleton_Desc_other");
            //}
            if (sel_data(dt, "param_Skin_Exterior_Desc_other") != "")
            {
                xml += "皮膚異常其他:" + sel_data(dt, "param_Skin_Exterior_Desc_other");
            }
            xml += "</param_Skin_Exterior_Desc_other>";
            #endregion
            #region --傷口--
            /////保留，之後再回來改
            string[] wound_type = sel_data(dt, "ddl_wound_type").Split(',');
            string[] wound_general = sel_data(dt, "ddl_wound_general").Split(',');
            string[] wound_scald = sel_data(dt, "ddl_wound_scald").Split(',');//燙傷
            string[] wound_date = sel_data(dt, "wound_date").Split(',');//日期
            string[] wound_date_unknown = sel_data(dt, "wound_date_unknown").Split(',');//不確定的checkbox
            xml += "<param_General_wound>一般傷口:" + sel_data(dt, "param_General_wound") + "</param_General_wound>";
            xml += "<wound_type>";
            if (sel_data(dt, "param_General_wound") == "有")
            {
                for (int i = 0; i < wound_type.Length; i++)
                {
                    xml += "傷口種類:";
                    xml += wound_type[i];
                    if (wound_type[i] == "燙傷")
                    {
                        xml += ";部位:" + wound_scald[i] + ";";
                    }
                    else
                    {
                        xml += ";部位:" + wound_general[i] + ";";
                    }
                    //加日期 --先住解~~撈不到值
                    xml += "發生日期:";
                    string Temp_wound_date = (wound_date[i] == "") ? "不詳" : wound_date[i];
                    xml += Temp_wound_date;
                    xml += ";";
                }
            }
            xml += "</wound_type>";
            /*----------------------壓傷傷口---------------*/
            // string[] Temp_place = sel_data(dt, "place").Split(',');
            string[] Temp_inHosp = sel_data(dt, "inHosp").Split(',');
            string[] Temp_outHosp = sel_data(dt, "outHosp").Split(',');
            string[] Temp_outHosp_other = sel_data(dt, "outHosp_other").Split(',');
            string[] Temp_wound_pressure = sel_data(dt, "wound_pressure").Split(',');
            string[] Temp_wound_pre_other_txt = sel_data(dt, "wound_pre_other_txt").Split(',');
            string[] wound_pre_date = sel_data(dt, "wound_pre_date").Split(',');
            string[] wound_pre_date_unknown = sel_data(dt, "wound_pre_date_unknown").Split(',');
            xml += "<param_pressure>壓傷傷口:" + sel_data(dt, "param_pressure") + "</param_pressure>";
            xml += "<place>";
            if (sel_data(dt, "param_pressure") == "有")
            {
                for (int i = 0; i < Temp_wound_pre_other_txt.Length; i++)
                {
                    string Temp_place = "";
                    if (i == 0)
                    {
                        Temp_place = sel_data(dt, "place");
                    }
                    else
                    {
                        Temp_place = sel_data(dt, "place_" + i);
                    }
                    xml += "傷口種類:發生地點:";
                    if (Temp_place == "1")
                    {
                        xml += "院內:" + Temp_inHosp[i].Trim();
                    }
                    else
                    {
                        if (Temp_outHosp[i] == "其他醫院")
                        {
                            xml += "院外:其他醫院:" + Temp_outHosp_other[i];
                        }
                        else if (Temp_outHosp[i] == "長期養護機構")
                        {
                            xml += "院外:長期養護機構:" + Temp_outHosp_other[i];
                        }
                        else
                        {
                            xml += "院外:" + Temp_outHosp[i];
                        }
                    }
                    xml += ";";
                    xml += "部位:";
                    if (Temp_wound_pressure[i] == "其他")
                    {
                        xml += "其他" + Temp_wound_pre_other_txt[i];
                    }
                    else
                    {
                        xml += Temp_wound_pressure[i];
                    }
                    xml += ";";
                    //加日期 --先住解~~撈不到值
                    xml += "發生日期:";
                    string Temp_wound_pre_date = (wound_pre_date[i] == "") ? "不詳" : wound_pre_date[i];
                    xml += Temp_wound_pre_date;
                    xml += ";";
                }
            }
            xml += "</place>";
            #endregion
            #region --壓傷危險性評估--
            xml += "<param_feeling_pressure_sores>感覺:" + sel_data(dt, "param_feeling_pressure_sores") + "</param_feeling_pressure_sores>";
            xml += "<param_wet_pressure_sores>潮濕:" + sel_data(dt, "param_wet_pressure_sores") + "</param_wet_pressure_sores>";
            xml += "<param_moving_pressure_sores>移動:" + sel_data(dt, "param_moving_pressure_sores") + "</param_moving_pressure_sores>";
            xml += "<param_activities_pressure_sores>活動:" + sel_data(dt, "param_activities_pressure_sores") + "</param_activities_pressure_sores>";
            xml += "<param_nutrition_pressure_sores>營養:" + sel_data(dt, "param_nutrition_pressure_sores") + "</param_nutrition_pressure_sores>";
            xml += "<param_friction_pressure_sores>磨擦力和剪力:" + sel_data(dt, "param_friction_pressure_sores") + "</param_friction_pressure_sores>";
            xml += "<param_total_pressure_sores>總分:" + sel_data(dt, "param_total_pressure_sores") + "</param_total_pressure_sores>";
            xml += "<param_pressure_assessment>壓傷危險因子:" + sel_data(dt, "param_pressure_assessment");
            string[] pressure_assessment_Desc = sel_data(dt, "param_pressure_assessment_Desc").Split(',');
            for (int i = 0; i < pressure_assessment_Desc.Length; i++)
            {
                if (pressure_assessment_Desc[i] != "其他")
                {
                    xml += pressure_assessment_Desc[i] + ",";
                }
                else
                {
                    xml += pressure_assessment_Desc[i] + ":" + sel_data(dt, "param_pressure_assessment_Desc_other") + ",";
                }
            }
            xml = xml.Substring(0, xml.Length - 1);
            xml += "</param_pressure_assessment>";
            #endregion
            #region --跌倒評估--
            xml += "<param_age_fall>年齡:" + sel_data(dt, "param_age_fall") + "</param_age_fall>";
            xml += "<param_consciousness_fall>意識狀態:" + sel_data(dt, "param_consciousness_fall") + "</param_consciousness_fall>";
            xml += "<param_dizziness_fall>頭暈/眩暈/虛弱感/視力異常:" + sel_data(dt, "param_dizziness_fall") + "</param_dizziness_fall>";
            xml += "<param_drug_fall>使用特殊藥物易致跌倒:" + sel_data(dt, "param_drug_fall") + "</param_drug_fall>";
            xml += "<param_excretion_fall>排泄情形:" + sel_data(dt, "param_excretion_fall") + "</param_excretion_fall>";
            xml += "<param_history_fall>跌倒史:" + sel_data(dt, "param_history_fall") + "</param_history_fall>";
            xml += "<param_activity_fall>輔助使用:" + sel_data(dt, "param_activity_fall") + "</param_activity_fall>";
            xml += "<param_communication_fall>執意下床:" + sel_data(dt, "param_communication_fall") + "</param_communication_fall>";
            xml += "<param_total_fall>總分:" + sel_data(dt, "param_total_fall") + "</param_total_fall>";
            #endregion
            #region --禁治療部位--
            xml += "<param_taboo_position>禁治療部位:" + sel_data(dt, "param_taboo_position") + "</param_taboo_position>";
            xml += "<param_taboo_position_txt>";
            if (sel_data(dt, "param_taboo_position") != "無")
            {
                string[] taboo_position_txt = sel_data(dt, "param_taboo_position_txt").Split(',');
                string[] taboo_position_other = sel_data(dt, "param_taboo_position_other").Split(',');
                for (int i = 0; i < taboo_position_txt.Length; i++)
                {
                    xml += "部位:" + taboo_position_txt[i] + ";原因:" + taboo_position_other[i];
                }
            }
            xml += "</param_taboo_position_txt>";
            #endregion
            #region --住院病友心情評估--
            xml += "<param_mood>無法評估:" + sel_data(dt, "param_mood") + "</param_mood>";
            string Temp_mood_yes_Dtl = (sel_data(dt, "param_mood_yes_Dtl") == "其他") ? "其他:" + sel_data(dt, "param_mood_yes_Dtl_txt") : sel_data(dt, "param_mood_yes_Dtl");
            xml += "<param_mood_yes_Dtl>";
            if (sel_data(dt, "param_mood") == "是")
            {
                xml += "無法評估原因:" + Temp_mood_yes_Dtl;
            }
            xml += "</param_mood_yes_Dtl>";
            xml += "<param_mood_jittery>";
            if (sel_data(dt, "param_mood") != "是")
            {
                xml += "感覺緊張不安:" + sel_data(dt, "param_mood_jittery");
            }
            xml += "</param_mood_jittery>";
            xml += "<param_mood_distress_flare>";
            if (sel_data(dt, "param_mood") != "是")
            {
                xml += "感覺容易苦惱或動怒:" + sel_data(dt, "param_mood_distress_flare");
            }
            xml += "</param_mood_distress_flare>";
            xml += "<param_mood_gloomy>";
            if (sel_data(dt, "param_mood") != "是")
            {
                xml += "感覺憂鬱、心情低落:" + sel_data(dt, "param_mood_gloomy");
            }
            xml += "</param_mood_gloomy>";
            xml += "<param_mood_feeling_failure>";
            if (sel_data(dt, "param_mood") != "是")
            {
                xml += "覺得比不上別人:" + sel_data(dt, "param_mood_feeling_failure");
            }
            xml += "</param_mood_feeling_failure>";
            xml += "<param_mood_difficulty_sleeping>";
            if (sel_data(dt, "param_mood") != "是")
            {
                xml += "睡眠困難，譬如難以入睡、易睡或早醒:" + sel_data(dt, "param_mood_difficulty_sleeping");
            }
            xml += "</param_mood_difficulty_sleeping>";
            xml += "<param_mood_total>";
            if (sel_data(dt, "param_mood") != "是")
            {
                xml += "總分:" + sel_data(dt, "param_mood_total");
            }
            xml += "</param_mood_total>";
            xml += "<param_mood_suicidal_thoughts>";
            if (sel_data(dt, "param_mood") != "是")
            {
                xml += "有自殺的想法:" + sel_data(dt, "param_mood_suicidal_thoughts");
            }
            xml += "</param_mood_suicidal_thoughts>";
            xml += "<param_Spirituality>靈性評估:" + sel_data(dt, "param_Spirituality") + "</param_Spirituality>";
            xml += "<Spirituality_issues_patient>";
            string Temp_Spirituality_issues_patient = string.Empty, Temp_Spirituality_issues_family = string.Empty, Temp_Spirituality_religion_patient = string.Empty, Temp_Spirituality_religion_family = string.Empty;
            if (sel_data(dt, "param_Spirituality") == "需要")
            {
                if (sel_data(dt, "Spirituality_issues_patient") == "有")
                {
                    Temp_Spirituality_issues_patient = sel_data(dt, "Spirituality_issues_patient");
                    Temp_Spirituality_issues_patient = Temp_Spirituality_issues_patient + "，";
                    string[] param_Spirituality_issues_patient = sel_data(dt, "param_Spirituality_issues_patient").Split(',');
                    for (int i = 0; i < param_Spirituality_issues_patient.Length; i++)
                    {
                        if (param_Spirituality_issues_patient[i] == "其他")
                        {
                            Temp_Spirituality_issues_patient += param_Spirituality_issues_patient[i] + ":" + sel_data(dt, "param_Spirituality_issues_patient_other") + ",";
                        }
                        else
                        {
                            Temp_Spirituality_issues_patient += param_Spirituality_issues_patient[i] + ",";
                        }
                    }
                    Temp_Spirituality_issues_patient = Temp_Spirituality_issues_patient.Substring(0, Temp_Spirituality_issues_patient.Length - 1);
                }
                else
                {
                    Temp_Spirituality_issues_patient = sel_data(dt, "Spirituality_issues_patient");
                }
                xml += "社會問題-病人:" + Temp_Spirituality_issues_patient;
            }
            xml += "</Spirituality_issues_patient>";
            xml += "<Spirituality_issues_family>";
            if (sel_data(dt, "param_Spirituality") == "需要")
            {
                if (sel_data(dt, "Spirituality_issues_family") == "有")
                {
                    Temp_Spirituality_issues_family = sel_data(dt, "Spirituality_issues_family");
                    Temp_Spirituality_issues_family = Temp_Spirituality_issues_family + "，";
                    string[] param_Spirituality_issues_family = sel_data(dt, "param_Spirituality_issues_family").Split(',');
                    for (int i = 0; i < param_Spirituality_issues_family.Length; i++)
                    {
                        if (param_Spirituality_issues_family[i] == "其他")
                        {
                            Temp_Spirituality_issues_family += param_Spirituality_issues_family[i] + ":" + sel_data(dt, "param_Spirituality_issues_family_other") + ",";
                        }
                        else
                        {
                            Temp_Spirituality_issues_family += param_Spirituality_issues_family[i] + ",";
                        }
                    }
                    Temp_Spirituality_issues_family = Temp_Spirituality_issues_family.Substring(0, Temp_Spirituality_issues_family.Length - 1);
                }
                else
                {
                    Temp_Spirituality_issues_family = sel_data(dt, "Spirituality_issues_family");
                }
                xml += "社會問題-家屬:" + Temp_Spirituality_issues_family;
            }
            xml += "</Spirituality_issues_family>";
            xml += "<Spirituality_religion_patient>";
            if (sel_data(dt, "param_Spirituality") == "需要")
            {
                if (sel_data(dt, "Spirituality_religion_patient") == "有")
                {
                    Temp_Spirituality_religion_patient = sel_data(dt, "Spirituality_religion_patient");
                    Temp_Spirituality_religion_patient = Temp_Spirituality_religion_patient + "，";
                    string[] param_Spirituality_religion_patient = sel_data(dt, "param_Spirituality_religion_patient").Split(',');
                    for (int i = 0; i < param_Spirituality_religion_patient.Length; i++)
                    {
                        if (param_Spirituality_religion_patient[i] == "其他")
                        {
                            Temp_Spirituality_religion_patient += param_Spirituality_religion_patient[i] + ":" + sel_data(dt, "param_Spirituality_religion_patient_other") + ",";
                        }
                        else
                        {
                            Temp_Spirituality_religion_patient += param_Spirituality_religion_patient[i] + ",";
                        }
                    }
                    Temp_Spirituality_religion_patient = Temp_Spirituality_religion_patient.Substring(0, Temp_Spirituality_religion_patient.Length - 1);
                }
                else
                {
                    Temp_Spirituality_religion_patient = sel_data(dt, "Spirituality_religion_patient");
                }
                xml += "靈性宗教問題-病人:" + Temp_Spirituality_religion_patient;
            }
            xml += "</Spirituality_religion_patient>";
            xml += "<Spirituality_religion_family>";
            if (sel_data(dt, "param_Spirituality") == "需要")
            {
                if (sel_data(dt, "Spirituality_religion_family") == "有")
                {
                    Temp_Spirituality_religion_family = sel_data(dt, "Spirituality_religion_family");
                    Temp_Spirituality_religion_family = Temp_Spirituality_religion_family + "，";
                    string[] param_Spirituality_religion_family = sel_data(dt, "param_Spirituality_religion_family").Split(',');
                    for (int i = 0; i < param_Spirituality_religion_family.Length; i++)
                    {
                        if (param_Spirituality_religion_family[i] == "其他")
                        {
                            Temp_Spirituality_religion_family += param_Spirituality_religion_family[i] + ":" + sel_data(dt, "param_Spirituality_religion_family_other") + ",";
                        }
                        else
                        {
                            Temp_Spirituality_religion_family += param_Spirituality_religion_family[i] + ",";
                        }
                    }
                    Temp_Spirituality_religion_family = Temp_Spirituality_religion_family.Substring(0, Temp_Spirituality_religion_family.Length - 1);
                }
                else
                {
                    Temp_Spirituality_religion_family = sel_data(dt, "Spirituality_religion_family");
                }
                xml += "靈性宗教問題-家屬:" + Temp_Spirituality_religion_family;
            }
            xml += "</Spirituality_religion_family>";
            #endregion
            #region --MUST營養不良篩檢--
            xml += "<param_nutrition_quality>身體質量指數:" + sel_data(dt, "param_nutrition_quality") + "</param_nutrition_quality>";
            xml += "<param_nutrition_loss>三到六個月體重減輕:" + sel_data(dt, "param_nutrition_loss") + "</param_nutrition_loss>";
            xml += "<param_nutrition_fasting>急性疾病狀態幾乎無進食或禁食:" + sel_data(dt, "param_nutrition_fasting") + "</param_nutrition_fasting>";
            xml += "<param_nutrition_totle>MUST總分:" + sel_data(dt, "param_nutrition_totle");
            if (Convert.ToInt32(sel_data(dt, "param_nutrition_totle")) < 3)
            {
                xml += "(MUST小於3分，暫時不需要進一步的營養評估)";
            }
            if (Convert.ToInt32(sel_data(dt, "param_nutrition_totle")) >= 3)
            {
                xml += "(MUST≧3分為營養不良高度風險，通知主治醫師決定是否照會營養師做進一步的營養評估)";
            }
            xml += "</param_nutrition_totle>";
            #endregion
            #region --疼痛評估--
            xml += "<param_pain_assessment_assess>疼痛評估工具:" + sel_data(dt, "param_pain_assessment_assess") + "</param_pain_assessment_assess>";
            xml += "<param_pain_assessment_record>疼痛項目:" + sel_data(dt, "param_pain_assessment_record") + "</param_pain_assessment_record>";
            #region 拋轉VitalSign 疼痛
            int ps_value = 0;
            if (sel_data(dt, "param_pain_assessment_assess") != "" && base.switchAssessmentInto == "Y")
            {
                string vs_record = sel_data(dt, "param_pain_assessment_record");
                //疼痛計分
                Regex rgx = new Regex(@"^([(]\d+[)])|^(\d+)");
                foreach (string ps in vs_record.ToString().Split('|'))
                {
                    if (ps != "")
                    {
                        ps_value += int.Parse(rgx.Match(ps).ToString().Replace("(", "").Replace(")", ""));
                    }
                }
            }
            #endregion
            xml += "<param_pain_assessment_level>疼痛總分:" + ps_value + "</param_pain_assessment_level>";
            #endregion
            //#region --接觸史--
            //string mother_str = "", family_str = "", brosis_str = "";
            //if (sel_data(dt, "rb_mother") == "無")
            //{
            //    mother_str = sel_data(dt, "rb_mother");
            //}
            //else
            //{
            //    mother_str = sel_data(dt, "rb_mother") + "," + sel_data(dt, "mother_ck_cp").Replace(",", "、");
            //    if (!string.IsNullOrEmpty(sel_data(dt, "txt_mother_Y_other")))
            //    {
            //        mother_str = mother_str.Replace("其他", sel_data(dt, "txt_mother_Y_other"));
            //    }
            //}
            //if (sel_data(dt, "rb_family") == "無")
            //{
            //    family_str = "：" + sel_data(dt, "rb_family");
            //}
            //else
            //{
            //    family_str = "(" + sel_data(dt, "txt_family_appellation") + ")：" + sel_data(dt, "rb_family") + "," + sel_data(dt, "family_ck_cp").Replace(",", "、");
            //    if (!string.IsNullOrEmpty(sel_data(dt, "txt_family_Y_other")))
            //    {
            //        family_str = family_str.Replace("其他", sel_data(dt, "txt_family_Y_other"));
            //    }
            //}
            //if (sel_data(dt, "rb_brosis") == "無")
            //{
            //    brosis_str = sel_data(dt, "rb_brosis");
            //}
            //else
            //{
            //    brosis_str = sel_data(dt, "rb_brosis") + "," + sel_data(dt, "brosis_ck_cp").Replace(",", "、");
            //    if (!string.IsNullOrEmpty(sel_data(dt, "txt_brosis_Y_other")))
            //    {
            //        brosis_str = brosis_str.Replace("其他", sel_data(dt, "txt_brosis_Y_other"));
            //    }
            //}
            //xml += "<param_mother_assess>媽媽："
            //    + mother_str + "</param_mother_assess>";
            //xml += "<param_family_assess>同住家人"
            //    + family_str + "</param_family_assess>";
            //xml += "<param_brosis_assess>哥哥、姊姊學校班上同學："
            //    + brosis_str + "</param_brosis_assess>";
            //xml += "<param_company_assess>住院期間照顧者："
            //    + sel_data(dt, "rb_company") + "</param_company_assess>";
            //#endregion
            #region --譫妄評估--
            //20230908 KEN 新增入評EMR 譫妄
            if(sel_data(dt, "rb_acute_attack_1a") != "")
            {
                xml += "<param_acute_attack_1a>1a.與平常相比較，是否有任何證據顯示病人精神狀態產生急性變化?:" + sel_data(dt, "rb_acute_attack_1a") + "</param_acute_attack_1a>";
                xml += "<param_acute_attack_1b>1b. 這些不正常的行為是否在一天中呈現波動狀態? 意即症狀來來去去或嚴重程度起起落落。:" + sel_data(dt, "rb_acute_attack_1b") + "</param_acute_attack_1b>";
                xml += "<param_attention>2.注意力不集中：病人是否集中注意力有困難? 例如容易分心或無法接續剛剛說過的話。:" + sel_data(dt, "rb_attention") + "</param_attention>";
                xml += "<param_ponder>3.思考缺乏組織：病人是否思考缺乏組織或不連貫? 如雜亂或答非所問的對話、不清楚或不合邏輯的想法或無預期的從一個主題跳到另一個主題。:" + sel_data(dt, "rb_ponder") + "</param_ponder>";
                xml += "<param_consciousness>4.意識狀態改變：整體而言，您認為病人的意識狀態為過度警覺、嗜睡、木僵或昏迷。:" + sel_data(dt, "rb_consciousness") + "</param_consciousness>";
                xml += "<param_total_delirium>譫妄結果 : " + sel_data(dt, "param_total_delirium") + "</param_total_delirium>";
            }
            #endregion
            #region --衰弱評估--
            //20230908 KEN 新增入評EMR 衰弱

            if (sel_data(dt, "rb_cfs") != "")
            {
                xml += "<param_cfs>衰弱評估:" + sel_data(dt, "rb_cfs") + "</param_cfs>";
                if(sel_data(dt, "rb_cfs") == "需要")
                {
                    xml += "<param_cfs_score>評估狀態 :" + sel_data(dt, "rb_cfs_score") + "分，" + sel_data(dt, "param_cfs_status") + "</param_cfs_score>";
                    if (sel_data(dt, "ck_bicycle") != "")
                    {
                        xml += "<param_cog_1>病人可依引導於畫鐘後，正確覆誦" + '"' + "紅色、快樂、腳踏車" + '"' + " : " + sel_data(dt, "ck_bicycle") + "</param_cog_1>";
                        xml += "<param_cog_2>病人可自行完成畫鐘數字及順序正確 : " + sel_data(dt, "rb_clock") + "</param_cog_2>";
                        xml += "<param_cog_3>病人可於畫鐘上畫上指針正確指向 11:10 : " + sel_data(dt, "rb_clock_2") + "</param_cog_3>";
                        xml += "<param_total_cog>總分 : " + sel_data(dt, "param_total_cog") + "分</param_total_cog>";
                    }
                    else
                    {
                        xml += "<param_minicog>Mini-Cog失智評估 : " + sel_data(dt, "ck_minicog") + "</param_minicog>";
                        xml += "<param_cog_result>" + sel_data(dt, "cog_ck_result");
                        if (sel_data(dt, "span_cog_Y_other") != "")
                        {
                            xml += '(' + sel_data(dt, "span_cog_Y_other") + ')';
                        }
                        xml += "</param_cog_result>";
                    }
                }
            }
            #endregion
            #region --TOCC--
            //TOCC 
            //症狀
            if (sel_data(dt, "param_symptom") != "")
            {
                string symptom = "";
                string symptomOther = "";
                symptom += "<param_symptom>";
                symptom += "症狀:";
                symptom += sel_data(dt, "param_symptom");
                if(sel_data(dt, "param_symptom_other") != "")
                {
                    symptomOther = sel_data(dt, "param_symptom_other");
                }
                symptom = symptom.Replace("其他", symptomOther);
                symptom += "</param_symptom>";

                xml += symptom;
            }
            //旅遊史(Travel)
            if (sel_data(dt, "param_travel") != "")
            {
                xml += "<param_travel>";
                xml += "旅遊史:";
                xml += sel_data(dt, "param_travel");
                xml += "</param_travel>";
                string domestic = "";
                string domesticOther = "";
                string aboard = "";
                string aboardOther = "";
                //國內
                if (sel_data(dt, "param_travel_domestic_city") != "")
                {
                    domestic += "<param_travel_domestic_city>城市: ";
                    domestic += sel_data(dt, "param_travel_domestic_city");
                    domestic += "</param_travel_domestic_city>";
                }
                if (sel_data(dt, "param_travel_domestic_viewpoint") != "")
                {
                    domestic += "<param_travel_domestic_viewpoint>景點: ";
                    domestic += sel_data(dt, "param_travel_domestic_viewpoint");
                    domestic += "</param_travel_domestic_viewpoint>";
                }
                if (sel_data(dt, "param_travel_domestic_traffic") != "")
                {
                    domestic += "<param_travel_domestic_traffic>交通方式(國內): ";
                    domestic += sel_data(dt, "param_travel_domestic_traffic");
                    if (sel_data(dt, "param_travel_domestic_traffic_other") != "")
                    {
                        domesticOther += sel_data(dt, "param_travel_domestic_traffic_other");
                    }
                    domestic = domestic.Replace("其他", domesticOther);
                    domestic += "</param_travel_domestic_traffic>";
                    xml += domestic;
                }

                //國外
                if (sel_data(dt, "param_travel_aboard_country") != "")
                {
                    aboard += "<param_travel_aboard_country>國家: ";
                    aboard += sel_data(dt, "param_travel_aboard_country");
                    aboard += "</param_travel_aboard_country>";
                }
                if (sel_data(dt, "param_travel_aboard_destination") != "")
                {
                    aboard += "<param_travel_aboard_destination>目的地: ";
                    aboard += sel_data(dt, "param_travel_aboard_destination");
                    aboard += "</param_travel_aboard_destination>";
                }
                if (sel_data(dt, "param_travel_aboard_traffic") != "")
                {
                    aboard += "<param_travel_aboard_traffic>交通方式(國外):";
                    aboard += sel_data(dt, "param_travel_aboard_traffic");
                    if (sel_data(dt, "param_travel_aboard_traffic_other") != "")
                    {
                        aboardOther += "(" + sel_data(dt, "param_travel_aboard_traffic_other") + ")";
                    }
                    aboard = aboard.Replace("其他", aboardOther);
                    aboard += "</param_travel_aboard_traffic>";
                    xml += aboard;

                }
            }
            //職業別
            if (sel_data(dt, "param_occupation") != "")
            {
                string occupation = "";
                string occupationOther = "";
                occupation += "<param_occupation>";
                occupation += "職業別:";
                occupation += sel_data(dt, "param_occupation");
                if (sel_data(dt, "param_occupation_other") != "")
                {
                    occupationOther += sel_data(dt, "param_occupation_other");
                }
                occupation += "</param_occupation>";
                occupation = occupation.Replace("其他", occupationOther);
                xml += occupation;
            }
            //接觸史
            if (sel_data(dt, "param_contact") != "")
            {
                string contact = "";
                string contactOther = "";
                contact += "<param_contact>";
                contact += "接觸史:";
                contact += sel_data(dt, "param_contact");
                //other
                if (sel_data(dt, "param_contact_other") != "")
                {
                    contactOther += sel_data(dt, "param_contact_other");
                }
                contact = contact.Replace("其他", contactOther);
                contact += "</param_contact>";
                xml += contact;

                //禽鳥
                if (sel_data(dt, "param_contact_birds") != "")
                {
                    xml += "<span_contact_birds>";
                    xml += "接觸禽鳥類、畜類等 : ";
                    xml += sel_data(dt, "param_contact_birds");
                    xml += "</span_contact_birds>";
                }
                //1
                if (sel_data(dt, "param_contact_obstetrics_symptom") != "")
                {
                    xml += "<param_contact_obstetrics_symptom>";
                    xml += "生產前 14 天內，同住家人有發燒、腹瀉、咳嗽、流鼻水等疑似感染症狀?";
                    xml += sel_data(dt, "param_contact_obstetrics_symptom");
                    xml += "</param_contact_obstetrics_symptom>";
                }
                //2
                if (sel_data(dt, "param_contact_obstetrics_sickleave") != "")
                {
                    xml += "<param_contact_obstetrics_sickleave>";
                    xml += "生產前 14 天內，同住家人學校班上同學有因為傳染病請假或班級停課之情形? ";
                    xml += sel_data(dt, "param_contact_obstetrics_sickleave");
                    xml += "</param_contact_obstetrics_sickleave>";
                }
                //3
                if (sel_data(dt, "param_contact_obstetrics_symptomcaregiver") != "")
                {
                    xml += "<param_contact_obstetrics_symptomcaregiver>";
                    xml += "住院期間照顧者(應盡量維持同一人)，目前有發燒、腹瀉、咳嗽、流鼻水等疑似感染症狀? ";
                    xml += sel_data(dt, "param_contact_obstetrics_symptomcaregiver");
                    xml += "</param_contact_obstetrics_symptomcaregiver>";
                }
            }
            //群聚史
            if (sel_data(dt, "param_cluster") != "")
            {
                string cluster = "";
                string clusterOther = "";
                cluster += "<param_cluster>";
                cluster += "群聚史: ";
                cluster += sel_data(dt, "param_cluster");
                cluster += "</param_cluster>";
                if (sel_data(dt, "param_cluster_relatives") != "")
                {
                    cluster += "<param_cluster_relatives>";
                    cluster += sel_data(dt, "param_cluster_relatives");
                    if (sel_data(dt, "param_cluster_relatives_other") != "")
                    {
                        clusterOther += sel_data(dt, "param_cluster_relatives_other");
                    }
                    cluster = cluster.Replace("其他", clusterOther);
                    cluster += "</param_cluster_relatives>";
                }
                xml += cluster;
            }

            #endregion
            xml += "</Document>";
            return xml;
        }
        #endregion


        #region --xml組成兒童入評--
        public string Composition_Xml_C(DataTable dt)
        {
            string xml = string.Empty;
            xml += "<?xml version='1.0' encoding='UTF-8'?>";
            //xml += "<?xml-stylesheet type='text/xsl' href='./NIS_A000011.XSL'?>";
            xml += "<Document>";
            #region 病患資訊
            string allergyDesc = string.Empty;
            byte[] allergen = webService.GetAllergyList(ptinfo.FeeNo);
            string ptJsonArr = string.Empty;
            if (allergen != null)
            {
                ptJsonArr = CompressTool.DecompressString(allergen);
            }

            List<NIS.Data.PatientInfo> patList = JsonConvert.DeserializeObject<List<NIS.Data.PatientInfo>>(ptJsonArr);
            if (allergen != null)
            {
                allergyDesc = patList[0].AllergyDesc;
            }
            xml += "<TING_PT_NAME>" + ptinfo.PatientName + "</TING_PT_NAME>";
            xml += "<TING_CHART_NO>" + ptinfo.ChartNo + "</TING_CHART_NO>";
            xml += "<TING_SEX>" + ptinfo.PatientGender + "</TING_SEX>";
            xml += "<TING_AGE>" + ptinfo.Age + "</TING_AGE>";
            xml += "<TING_BED_NO>" + ptinfo.BedNo + "</TING_BED_NO>";
            xml += "<TING_ADMIT_DATE>" + ptinfo.InDate.ToString("yyyyMMdd") + "</TING_ADMIT_DATE>";
            xml += "<TING_ADMIT_TIME>" + ptinfo.InDate.ToString("HHmm") + "</TING_ADMIT_TIME>";
            xml += "<TING_ALLERGEN>" + allergyDesc + "</TING_ALLERGEN>";
            #endregion
            #region --基本資料--
            xml += "<param_tube_date>入院日期:" + sel_data(dt, "param_tube_date") + " " + sel_data(dt, "param_tube_time") + "</param_tube_date>";
            xml += "<param_ipd_source>入院來源:" + sel_data(dt, "param_ipd_source") + "</param_ipd_source>";
            if (sel_data(dt, "param_ipd_style") != "其他")
            { xml += "<param_ipd_style>" + "入病房方式:" + sel_data(dt, "param_ipd_style") + "</param_ipd_style>"; }
            else
            { xml += "<param_ipd_style>" + "入病房方式:" + sel_data(dt, "param_ipd_style") + ":" + sel_data(dt, "param_ipd_style_other") + "</param_ipd_style>"; }
            xml += "<param_ipd_depiction>病史描述:" + sel_data(dt, "param_ipd_depiction") + "</param_ipd_depiction>";
            xml += "<param_allergy_med>" + "過敏史藥物:" + sel_data(dt, "param_allergy_med");
            if (sel_data(dt, "param_allergy_med") == "有")
            {//不詳,pyrin,aspirin,NSAID,顯影劑,磺氨類,盤尼西林類,抗生素類,麻醉藥,其他
                xml = xml + ";" + sel_data(dt, "param_allergy_med_other");
            }
            xml += "</param_allergy_med>";
            string[] allergy_med_other = sel_data(dt, "param_allergy_med_other").Split(',');
            xml += "<param_allergy_med_other_2_name>";
            if (sel_data(dt, "param_allergy_med_other_2_name") != "")
            {
                xml += "匹林系藥物(pyrin):";
                for (int i = 0; i < allergy_med_other.Length; i++)
                {
                    if (allergy_med_other[i] == "pyrin")
                    {
                        xml += sel_data(dt, "param_allergy_med_other_2_name");
                    }
                }
            }
            xml += "</param_allergy_med_other_2_name>";
            xml += "<param_allergy_med_other_4_name>";
            if (sel_data(dt, "param_allergy_med_other_4_name") != "")
            {
                xml += "非類固醇抗炎藥物(NSAID):";
                for (int i = 0; i < allergy_med_other.Length; i++)
                {
                    if (allergy_med_other[i] == "NSAID")
                    {

                        xml += sel_data(dt, "param_allergy_med_other_4_name");
                    }
                }
            }
            xml += "</param_allergy_med_other_4_name>";
            xml += "<param_allergy_med_other_6_name>";
            if (sel_data(dt, "param_allergy_med_other_6_name") != "")
            {
                xml += "磺氨類:";
                for (int i = 0; i < allergy_med_other.Length; i++)
                {
                    if (allergy_med_other[i] == "磺氨類")
                    {
                        xml += sel_data(dt, "param_allergy_med_other_6_name");
                    }
                }
            }
            xml += "</param_allergy_med_other_6_name>";
            xml += "<param_allergy_med_other_7_name>";
            if (sel_data(dt, "param_allergy_med_other_7_name") != "")
            {
                xml += " 盤尼西林類:";
                for (int i = 0; i < allergy_med_other.Length; i++)
                {
                    if (allergy_med_other[i] == "盤尼西林類")
                    {
                        xml += sel_data(dt, "param_allergy_med_other_7_name");
                    }
                }
            }
            xml += "</param_allergy_med_other_7_name>";
            xml += "<param_allergy_med_other_8_name>";
            if (sel_data(dt, "param_allergy_med_other_8_name") != "")
            {
                xml += "抗生素類:";
                for (int i = 0; i < allergy_med_other.Length; i++)
                {
                    if (allergy_med_other[i] == "抗生素類")
                    {
                        xml += sel_data(dt, "param_allergy_med_other_8_name");
                    }
                }
            }
            xml += "</param_allergy_med_other_8_name>";
            xml += "<param_allergy_med_other_9_name>";
            if (sel_data(dt, "param_allergy_med_other_9_name") != "")
            {
                xml += "麻醉藥:";
                for (int i = 0; i < allergy_med_other.Length; i++)
                {
                    if (allergy_med_other[i] == "麻醉藥")
                    {
                        xml += sel_data(dt, "param_allergy_med_other_9_name");
                    }
                }
            }
            xml += "</param_allergy_med_other_9_name>";
            xml += "<param_allergy_med_other_10_name>";
            if (sel_data(dt, "param_allergy_med_other_10_name") != "")
            {
                xml += "過敏史藥物其他:";
                for (int i = 0; i < allergy_med_other.Length; i++)
                {
                    if (allergy_med_other[i] == "其他")
                    {
                        xml += sel_data(dt, "param_allergy_med_other_10_name");
                    }
                }
            }
            xml += "</param_allergy_med_other_10_name>";
            xml += "<param_allergy_food>" + "過敏史食物:" + sel_data(dt, "param_allergy_food");
            if (sel_data(dt, "param_allergy_food") == "有")
            {
                xml = xml + ";" + sel_data(dt, "param_allergy_food_other");
            }
            xml += "</param_allergy_food>";
            string[] allergy_food_other = sel_data(dt, "param_allergy_food_other").Split(',');
            xml += "<param_allergy_food_other_2_name>";
            if (sel_data(dt, "param_allergy_food_other_2_name") != "")
            {
                xml += "海鮮類:";
                for (int i = 0; i < allergy_food_other.Length; i++)
                {
                    if (allergy_food_other[i] == "海鮮類")
                    {
                        xml += sel_data(dt, "param_allergy_food_other_2_name");
                    }
                }
            }
            xml += "</param_allergy_food_other_2_name>";
            xml += "<param_allergy_food_other_4_name>";
            if (sel_data(dt, "param_allergy_food_other_4_name") != "")
            {
                xml += "水果:";
                for (int i = 0; i < allergy_food_other.Length; i++)
                {
                    if (allergy_food_other[i] == "水果")
                    {
                        xml += sel_data(dt, "param_allergy_food_other_4_name");
                    }
                }
            }
            xml += "</param_allergy_food_other_4_name>";
            xml += "<param_allergy_food_other_6_name>";
            if (sel_data(dt, "param_allergy_food_other_6_name") != "")
            {
                xml += "過敏史食物其他:";
                for (int i = 0; i < allergy_food_other.Length; i++)
                {
                    if (allergy_food_other[i] == "其他")
                    {
                        xml += sel_data(dt, "param_allergy_food_other_6_name");
                    }
                }
            }
            xml += "</param_allergy_food_other_6_name>";
            xml += "<param_allergy_other>" + "過敏史其他:" + sel_data(dt, "param_allergy_other");
            if (sel_data(dt, "param_allergy_other") == "有")
            {
                if (sel_data(dt, "param_allergy_other_other") != "")
                {
                    xml = xml + ";";
                    string[] arr_param_allergy_other_other = sel_data(dt, "param_allergy_other_other").Split(',');
                    for (int i = 0; i < arr_param_allergy_other_other.Length; i++)
                    {
                        string Temp1 = arr_param_allergy_other_other[i].Substring(0, 1);
                        if (Temp1 == "麈")
                        {

                            xml += "麈蟎,";
                        }
                        else
                        {
                            xml += arr_param_allergy_other_other[i] + ",";
                        }
                    }
                    xml = xml.Substring(0, xml.Length - 1);
                }
            }
            xml += "</param_allergy_other>";

            string[] allergy_other_other = sel_data(dt, "param_allergy_other_other").Split(',');
            xml += "<param_allergy_other_other_1_name>";
            //if(sel_data(dt, "param_allergy_other") == "有")
            //{
            //    xml += "不詳:";
            //    for(int i = 0; i < allergy_other_other.Length; i++)
            //    {
            //        if(allergy_other_other[i] == "不詳")
            //        {
            //            xml += sel_data(dt, "param_allergy_other_other_1_name");
            //        }
            //    }
            //}
            xml += "</param_allergy_other_other_1_name>";
            xml += "<param_allergy_other_other_2_name>";
            //if(sel_data(dt, "param_allergy_other") == "有")
            //{
            //    xml+="輸血:";
            //    for(int i = 0; i < allergy_other_other.Length; i++)
            //    {
            //        if(allergy_other_other[i] == "輸血")
            //        {
            //            xml += sel_data(dt, "param_allergy_other_other_2_name");
            //        }
            //    }
            //}
            xml += "</param_allergy_other_other_2_name>";
            xml += "<param_allergy_other_other_3_name>";
            //if(sel_data(dt, "param_allergy_other") == "有")
            //{
            //    xml += "油漆:";
            //    for(int i = 0; i < allergy_other_other.Length; i++)
            //    {
            //        if(allergy_other_other[i] == "油漆")
            //        {
            //            xml += sel_data(dt, "param_allergy_other_other_3_name");
            //        }
            //    }
            //}
            xml += "</param_allergy_other_other_3_name>";
            xml += "<param_allergy_other_other_4_name>";
            //if(sel_data(dt, "param_allergy_other") == "有")
            //{
            //    xml += "昆蟲:";
            //    for(int i = 0; i < allergy_other_other.Length; i++)
            //    {
            //        if(allergy_other_other[i] == "昆蟲")
            //        {
            //            xml += sel_data(dt, "param_allergy_other_other_4_name");
            //        }
            //    }
            //}
            xml += "</param_allergy_other_other_4_name>";
            xml += "<param_allergy_other_other_5_name>";
            //if(sel_data(dt, "param_allergy_other") == "有")
            //{
            //xml += "麈蟎:";
            //for(int i = 0; i < allergy_other_other.Length; i++)
            //{
            //    if(allergy_other_other[i] == "麈蟎")
            //    {
            //        xml += sel_data(dt, "param_allergy_other_other_5_name");
            //    }
            //}
            //}
            xml += "</param_allergy_other_other_5_name>";
            xml += "<param_allergy_other_other_6_name>";
            if (sel_data(dt, "param_allergy_other") == "有")
            {
                if (sel_data(dt, "param_allergy_other_other_6_name") != "")
                {
                    xml += "其他:";
                    for (int i = 0; i < allergy_other_other.Length; i++)
                    {
                        if (allergy_other_other[i] == "其他")
                        {
                            xml += sel_data(dt, "param_allergy_other_other_6_name");
                        }
                    }
                }
            }
            xml += "</param_allergy_other_other_6_name>";
            xml += "<param_primary_care>主要照顧者:" + sel_data(dt, "param_primary_care") + "</param_primary_care>";
            //<!-- 父親資料 -->
            xml += "<param_primary_father_name>";
            if (sel_data(dt, "param_primary_father_name").ToString().Trim() != "")
            {
                xml += "姓名:" + sel_data(dt, "param_primary_father_name") + ";";
            }
            xml += "</param_primary_father_name>";
            xml += "<param_primary_father_age>";
            if (sel_data(dt, "param_primary_father_age").ToString().Trim() != "")
            {
                xml += "年齡:" + sel_data(dt, "param_primary_father_age") + ";";
            }
            xml += "</param_primary_father_age>";
            xml += "<param_primary_father_jop>";
            if (sel_data(dt, "param_primary_father_jop").ToString().Trim() != "")
            {
                xml += "職業:" + sel_data(dt, "param_primary_father_jop") + ";";
            }
            xml += "</param_primary_father_jop>";
            xml += "<param_father_education>";
            if (sel_data(dt, "param_father_education").ToString().Trim() != "")
            {
                xml += "教育程度:" + sel_data(dt, "param_father_education") + ";";
            }
            xml += "</param_father_education>";
            //<!-- 母親資料-->
            xml += "<param_primary_mother_name>";
            if (sel_data(dt, "param_primary_mother_name").ToString().Trim() != "")
            {
                xml += "姓名:" + sel_data(dt, "param_primary_mother_name") + ";";
            }
            xml += "</param_primary_mother_name>";
            xml += "<param_primary_mother_age>";
            if (sel_data(dt, "param_primary_mother_age").ToString().Trim() != "")
            {
                xml += "年齡:" + sel_data(dt, "param_primary_mother_age") + ";";
            }
            xml += "</param_primary_mother_age>";
            xml += "<param_primary_mother_jop>";
            if (sel_data(dt, "param_primary_mother_jop").ToString().Trim() != "")
            {
                xml += "職業:" + sel_data(dt, "param_primary_mother_jop") + ";";
            }
            xml += "</param_primary_mother_jop>";
            xml += "<param_mother_education>";
            if (sel_data(dt, "param_mother_education").ToString().Trim() != "")
            {
                xml += "教育程度:" + sel_data(dt, "param_mother_education") + ";";
            }
            xml += "</param_mother_education>";
            xml += "<param_source>資料來源:" + sel_data(dt, "param_source") + ";</param_source>";
            string[] EMGName = sel_data(dt, "param_EMGContact").Split(',');
            //string[] EMGContactRole = sel_data(dt, "param_ContactRole").Trim(',').Split(',');
            string[] EMGContactRole_other = sel_data(dt, "param_ContactRole_other").Split(',');
            string[] EMGContact_1 = sel_data(dt, "param_EMGContact_1").Split(',');
            string[] EMGContact_2 = sel_data(dt, "param_EMGContact_2").Split(',');
            string[] EMGContact_3 = sel_data(dt, "param_EMGContact_3").Split(',');
            xml += "<param_EMGContact>";
            for (int i = 0; i < EMGName.Length; i++)
            {
                xml += "緊急聯絡人姓名:" + EMGName[i] + ";";
                string TempEMGContactRole = (i != 0) ? sel_data(dt, "param_ContactRole_" + i.ToString()) : sel_data(dt, "param_ContactRole");
                if (TempEMGContactRole != "其他")
                {
                    xml += "稱謂:" + TempEMGContactRole + ";";
                }
                else
                {
                    xml += "稱謂:" + TempEMGContactRole + ":" + EMGContactRole_other[i] + ";";
                }
                if (EMGContact_1[i].ToString().Trim() != "")
                {
                    xml += "連絡電話:住家:" + EMGContact_1[i] + ";";
                }
                if (EMGContact_2[i].ToString().Trim() != "")
                {
                    xml += "公司:" + EMGContact_2[i] + ";";
                }
                if (EMGContact_3[i].ToString().Trim() != "")
                {
                    xml += "手機:" + EMGContact_3[i] + ";";
                }
                xml = xml.Substring(0, xml.Length - 1);
                xml += "。";
            }
            xml += "</param_EMGContact>";
            #endregion
            #region --個人病史--
            xml += "<param_im_history>曾患疾病:" + sel_data(dt, "param_im_history") + "，" + sel_data(dt, "param_im_history_item_other") + "</param_im_history>";
            xml += "<param_im_history_item_txt>";
            if (sel_data(dt, "param_im_history_item_txt") != "")
            {
                xml += "感冒多久:" + sel_data(dt, "param_im_history_item_txt") + "次/年";
            }
            xml += "</param_im_history_item_txt>";
            xml += "<param_im_history_item_other_txt>";
            if (sel_data(dt, "param_im_history_item_other_txt") != "")
            {
                xml += "其他疾病:" + sel_data(dt, "param_im_history_item_other_txt");
            }
            xml += "</param_im_history_item_other_txt>";
            xml += "<param_hasipd>住院次數:" + sel_data(dt, "param_hasipd");
            if (sel_data(dt, "param_hasipd") == "有")
            {
                xml += ";" + sel_data(dt, "param_ipd_past_count") + "次";
            }
            xml += "</param_hasipd>";
            xml += "<param_ipd_past_reason>";
            if (sel_data(dt, "param_hasipd") == "有")
            {
                xml += "上次住院原因:" + sel_data(dt, "param_ipd_past_reason") + "，上次住院地點:" + sel_data(dt, "param_ipd_past_location") + ";";
            }
            xml += "</param_ipd_past_reason>";
            xml += "<param_surgery>手術情形:" + sel_data(dt, "param_surgery");
            if (sel_data(dt, "param_surgery") == "有")
            {
                xml += ";" + sel_data(dt, "param_ipd_surgery_count") + "次";
            }
            xml += "</param_surgery>";
            xml += "<param_ipd_surgery_reason>";
            if (sel_data(dt, "param_surgery") == "有")
            {
                xml += "上次手術原因:" + sel_data(dt, "param_ipd_surgery_reason") + ";上次手術地點:" + sel_data(dt, "param_ipd_surgery_location") + ";";
            }
            xml += "</param_ipd_surgery_reason>";
            xml += "<param_blood>輸血經驗:" + sel_data(dt, "param_blood") + "</param_blood>";
            xml += "<param_blood_reaction>";
            if (sel_data(dt, "param_blood") == "有")
            {
                xml += "輸血過敏反應:" + sel_data(dt, "param_blood_reaction");
                if (sel_data(dt, "param_blood_reaction") != "無")
                {
                    xml += ";" + sel_data(dt, "transfusion_blood_dtl_txt");//param_blood_reaction
                }
            }
            xml += "</param_blood_reaction>";


            xml += "<param_take_medicine>服藥:" + sel_data(dt, "param_take_medicine") + "</param_take_medicine>";
            xml += "<param_take_medicine_dtl_txt>";
            if (sel_data(dt, "param_take_medicine") == "有")
            {
                List<DrugOrder> Drug_list = new List<DrugOrder>();
                byte[] labfoByteCode = webService.GetOpdMed(ptinfo.FeeNo);
                if (labfoByteCode != null)
                {
                    string labJosnArr = CompressTool.DecompressString(labfoByteCode);
                    Drug_list = JsonConvert.DeserializeObject<List<DrugOrder>>(labJosnArr);
                }
                xml += ";";
                for (int j = 0; j < Drug_list.Count; j++)
                {
                    xml += "門診用藥-藥物名稱:" + Drug_list[j].DrugName + ",";
                    xml += "頻次:" + Drug_list[j].Feq + ",";
                    xml += "劑量:" + Drug_list[j].Dose + ",";
                    xml += "途徑:" + Drug_list[j].Route + ",";
                    xml += ";";
                }
                xml += "藥物名稱及使用情形:" + sel_data(dt, "param_take_medicine_dtl_txt");
            }
            xml += "</param_take_medicine_dtl_txt>";
            xml += "<param_Congenital_disease>先天疾患:" + sel_data(dt, "param_Congenital_disease");
            if (sel_data(dt, "Congenital_disease_dtl") != "")
            {
                string[] Congenital_disease_dtl = sel_data(dt, "Congenital_disease_dtl").Split(',');
                xml += ",";
                for (int i = 0; i < Congenital_disease_dtl.Length; i++)
                {
                    if (Congenital_disease_dtl[i] == "其他")
                    {
                        xml += Congenital_disease_dtl[i] + ":" + sel_data(dt, "Congenital_disease_dtl_other") + ",";
                    }
                    else
                    {
                        xml += Congenital_disease_dtl[i] + ",";
                    }
                }
                xml = xml.Substring(0, xml.Length - 1);
            }
            xml += "</param_Congenital_disease>";
            #endregion
            #region --預防接種--
            //<!-- B型肝炎 -->
            xml += "<param_HepatitisB>";
            if (sel_data(dt, "param_HepatitisB").ToString().Trim() != "")
            {
                xml += "B型肝炎免疫球蛋白:" + sel_data(dt, "param_HepatitisB");
            }
            xml += "</param_HepatitisB>";
            xml += "<param_HepatitisB1>";
            if (sel_data(dt, "param_HepatitisB1").ToString().Trim() != "")
            {
                xml += "B型肝炎疫苗第一劑:" + sel_data(dt, "param_HepatitisB1");
            }
            xml += "</param_HepatitisB1>";
            xml += "<param_HepatitisB2>";
            if (sel_data(dt, "param_HepatitisB2").ToString().Trim() != "")
            {
                xml += "B型肝炎疫苗第二劑:" + sel_data(dt, "param_HepatitisB2");
            }
            xml += "</param_HepatitisB2>";
            xml += "<param_HepatitisB3>";
            if (sel_data(dt, "param_HepatitisB3").ToString().Trim() != "")
            {
                xml += "B型肝炎疫苗第三劑:" + sel_data(dt, "param_HepatitisB3");
            }
            xml += "</param_HepatitisB3>";
            //	<!-- 五合一疫苗 -->
            xml += "<param_5in1Vaccine1>";
            if (sel_data(dt, "param_5in1Vaccine1").ToString().Trim() != "")
            {
                xml += "第一劑:" + sel_data(dt, "param_5in1Vaccine1");
            }
            xml += "</param_5in1Vaccine1>";
            xml += "<param_5in1Vaccine2>";
            if (sel_data(dt, "param_5in1Vaccine2").ToString().Trim() != "")
            {
                xml += "第二劑:" + sel_data(dt, "param_5in1Vaccine2");
            }
            xml += "</param_5in1Vaccine2>";
            xml += "<param_5in1Vaccine3>";
            if (sel_data(dt, "param_5in1Vaccine3").ToString().Trim() != "")
            {
                xml += "第三劑:" + sel_data(dt, "param_5in1Vaccine3");
            }
            xml += "</param_5in1Vaccine3>";
            xml += "<param_5in1Vaccine4>";
            if (sel_data(dt, "param_5in1Vaccine4").ToString().Trim() != "")
            {
                xml += "第四劑(追加):" + sel_data(dt, "param_5in1Vaccine4");
            }
            xml += "</param_5in1Vaccine4>";
            //	<!-- MMR -->
            xml += "<param_MMR1>";
            if (sel_data(dt, "param_MMR1").ToString().Trim() != "")
            {
                xml += "第一劑:" + sel_data(dt, "param_MMR1");
            }
            xml += "</param_MMR1>";
            xml += "<param_MMR2>";
            if (sel_data(dt, "param_MMR2").ToString().Trim() != "")
            {
                xml += "第二劑(追加):" + sel_data(dt, "param_MMR2");
            }
            xml += "</param_MMR2>";
            //	<!-- 水痘疫苗 -->
            xml += "<param_VaricellaVaccine>";
            if (sel_data(dt, "param_VaricellaVaccine").ToString().Trim() != "")
            {
                xml += "水痘疫苗:" + sel_data(dt, "param_VaricellaVaccine");
            }
            xml += "</param_VaricellaVaccine>";
            //	<!-- 日本腦炎疫苗 -->
            xml += "<param_JP_encephalitis1>";
            if (sel_data(dt, "param_JP_encephalitis1").ToString().Trim() != "")
            {
                xml += "第一劑:" + sel_data(dt, "param_JP_encephalitis1");
            }
            xml += "</param_JP_encephalitis1>";
            xml += "<param_JP_encephalitis2>";
            if (sel_data(dt, "param_JP_encephalitis2").ToString().Trim() != "")
            {
                xml += "第二劑:" + sel_data(dt, "param_JP_encephalitis2");
            }
            xml += "</param_JP_encephalitis2>";
            xml += "<param_JP_encephalitis3>";
            if (sel_data(dt, "param_JP_encephalitis3").ToString().Trim() != "")
            {
                xml += "第三劑:" + sel_data(dt, "param_JP_encephalitis3");
            }
            xml += "</param_JP_encephalitis3>";
            xml += "<param_JP_encephalitis4>";
            if (sel_data(dt, "param_JP_encephalitis4").ToString().Trim() != "")
            {
                xml += "第四劑:" + sel_data(dt, "param_JP_encephalitis4");
            }
            xml += "</param_JP_encephalitis4>";
            //	<!-- DPT及小兒麻痺混合疫苗 -->
            xml += "<param_DPT>";
            if (sel_data(dt, "param_DPT").ToString().Trim() != "")
            {
                xml += "DPT及小兒麻痺混合疫苗:" + sel_data(dt, "param_DPT");
            }
            xml += "</param_DPT>";
            xml += "<param_InfluenzaVaccination>";
            if (sel_data(dt, "param_InfluenzaVaccination").ToString().Trim() != "")
            {
                xml += "流感疫苗:" + sel_data(dt, "param_InfluenzaVaccination");
            }
            xml += "</param_InfluenzaVaccination>";
            //	<!-- 肺炎鏈球菌疫苗 -->
            xml += "<param_Stre_PneumococcalVaccine1>";
            if (sel_data(dt, "param_Stre_PneumococcalVaccine1").ToString().Trim() != "")
            {
                xml += "第一劑:" + sel_data(dt, "param_Stre_PneumococcalVaccine1");
            }
            xml += "</param_Stre_PneumococcalVaccine1>";
            xml += "<param_Stre_PneumococcalVaccine2>";
            if (sel_data(dt, "param_Stre_PneumococcalVaccine2").ToString().Trim() != "")
            {
                xml += "第二劑:" + sel_data(dt, "param_Stre_PneumococcalVaccine2");
            }
            xml += "</param_Stre_PneumococcalVaccine2>";
            xml += "<param_Stre_PneumococcalVaccine3>";
            if (sel_data(dt, "param_Stre_PneumococcalVaccine3").ToString().Trim() != "")
            {
                xml += "第三劑:" + sel_data(dt, "param_Stre_PneumococcalVaccine3");
            }
            xml += "</param_Stre_PneumococcalVaccine3>";
            xml += "<param_Stre_PneumococcalVaccine4>";
            if (sel_data(dt, "param_Stre_PneumococcalVaccine4").ToString().Trim() != "")
            {
                xml += "第四劑:" + sel_data(dt, "param_Stre_PneumococcalVaccine4");
            }
            xml += "</param_Stre_PneumococcalVaccine4>";
            //	<!-- A型肝炎疫苗 -->
            xml += "<param_Hepatitis_A1>";
            if (sel_data(dt, "param_Hepatitis_A1").ToString().Trim() != "")
            {
                xml += "第一劑:" + sel_data(dt, "param_Hepatitis_A1");
            }
            xml += "</param_Hepatitis_A1>";
            xml += "<param_SHepatitis_A2>";
            if (sel_data(dt, "param_SHepatitis_A2").ToString().Trim() != "")
            {
                xml += "第二劑:" + sel_data(dt, "param_SHepatitis_A2");
            }
            xml += "</param_SHepatitis_A2>";
            //	<!-- 輪狀病毒疫苗 -->
            xml += "<param_Rotavirus1>";
            if (sel_data(dt, "param_Rotavirus1").ToString().Trim() != "")
            {
                xml += "第一劑:" + sel_data(dt, "param_Rotavirus1");
            }
            xml += "</param_Rotavirus1>";
            xml += "<param_Rotavirus2>";
            if (sel_data(dt, "param_Rotavirus2").ToString().Trim() != "")
            {
                xml += "第二劑:" + sel_data(dt, "param_Rotavirus2");
            }
            xml += "</param_Rotavirus2>";
            xml += "<VaccineName>";
            if (sel_data(dt, "VaccineName").ToString().Trim() != "")
            {
                xml += "其他疫苗:" + sel_data(dt, "VaccineName");
            }
            xml += "</VaccineName>";
            #endregion
            #region --日常生活--
            string Temp_param_Diet = (sel_data(dt, "param_Diet") == "其他") ? sel_data(dt, "param_Diet") + ":" + sel_data(dt, "param_Diet_other") : sel_data(dt, "param_Diet");
            xml += "<param_Diet>飲食方式:" + Temp_param_Diet + "</param_Diet>";
            string Temp_param_Diet_type = (sel_data(dt, "param_Diet_type") == "其他") ? sel_data(dt, "param_Diet_type") + ":" + sel_data(dt, "param_Diet_type_other") : sel_data(dt, "param_Diet_type");
            xml += "<param_Diet_type>飲食種類:" + Temp_param_Diet_type + "</param_Diet_type>";
            xml += "<param_Sleep_Habits>睡眠習慣:" + sel_data(dt, "param_Sleep_Habits") + "小時/天</param_Sleep_Habits>";
            xml += "<param_Voiding_patterns>解尿型態:" + sel_data(dt, "param_Voiding_patterns") + "</param_Voiding_patterns>";
            xml += "<param_Excretion_patterns>排泄型態:" + sel_data(dt, "param_Excretion_patterns") + "</param_Excretion_patterns>";
            xml += "<param_Defecation_D>排便次數:" + sel_data(dt, "param_Defecation_D") + "天" + sel_data(dt, "param_Defecation_F") + "次</param_Defecation_D>";
            string Temp_param_Color_defecation = (sel_data(dt, "param_Color_defecation") == "其他") ? sel_data(dt, "param_Color_defecation") + ":" + sel_data(dt, "param_Color_defecation_other") : sel_data(dt, "param_Color_defecation");
            xml += "<param_Color_defecation>排便顏色:" + Temp_param_Color_defecation + "</param_Color_defecation>";
            string Temp_param_Bowel_defecation = (sel_data(dt, "param_Bowel_defecation") == "其他") ? sel_data(dt, "param_Bowel_defecation") + ":" + sel_data(dt, "param_Bowel_defecation_other") : sel_data(dt, "param_Bowel_defecation");
            xml += "<param_Bowel_defecation>排便性狀:" + Temp_param_Bowel_defecation + "</param_Bowel_defecation>";
            //xml += "<param_Defecation_particles>排便顆粒:稀</param_Defecation_particles>";
            string Temp_param_Daily_activities = (sel_data(dt, "param_Daily_activities") == "其他") ? sel_data(dt, "param_Daily_activities") + ":" + sel_data(dt, "param_Daily_activities_other") : sel_data(dt, "param_Daily_activities");
            xml += "<param_Daily_activities>日常活動:" + Temp_param_Daily_activities + "</param_Daily_activities>";
            xml += "<param_Behavioral_Development>行為發展:" + sel_data(dt, "param_Behavioral_Development") + "</param_Behavioral_Development>";
            //遲緩，是--->地點
            xml += "<param_Behavioral_Development_YN>";
            if (sel_data(dt, "param_Behavioral_Development") == "遲緩")
            {
                xml += "是否有在做治療:" + sel_data(dt, "param_Behavioral_Development_YN");
                if (sel_data(dt, "param_Behavioral_Development_YN") == "是")
                {
                    xml += "，地點:" + sel_data(dt, "param_Behavioral_Development_txt");
                }
            }
            xml += "</param_Behavioral_Development_YN>";
            #endregion
            #region --一般狀況評估--
            string Temp_param_General_appearance = (sel_data(dt, "param_General_appearance") == "其他") ? sel_data(dt, "param_General_appearance") + ":" + sel_data(dt, "param_General_appearance_other") : sel_data(dt, "param_General_appearance");
            xml += "<param_General_appearance>一般外觀:" + Temp_param_General_appearance + "</param_General_appearance>";
            xml += "<param_consciousness>意識狀態:" + sel_data(dt, "param_consciousness") + "</param_consciousness>";
            //<!-- COMA SCALE -->
            xml += "<gc_r1>";
            if (sel_data(dt, "gc_r1") != "")
            {
                xml += "(E)睜眼反射:" + sel_data(dt, "gc_r1");
            }
            xml += "</gc_r1>";
            xml += "<gc_r2>";
            if (sel_data(dt, "gc_r2") != "")
            {
                xml += "(V)語言反射:" + sel_data(dt, "gc_r2");
            }
            xml += "</gc_r2>";
            xml += "<gc_r3>";
            if (sel_data(dt, "gc_r3") != "")
            {
                xml += "(M)運動反射:" + sel_data(dt, "gc_r3");
            }
            xml += "</gc_r3>";
            xml += "<param_emotional_state>情緒狀態:" + sel_data(dt, "param_emotional_state");
            if (sel_data(dt, "param_emotional_state") == "異常")
            {
                xml = xml + ";";
                string[] Temp_param_emotional_state_other = sel_data(dt, "param_emotional_state_other").Split(',');
                for (int i = 0; i < Temp_param_emotional_state_other.Length; i++)
                {
                    if (Temp_param_emotional_state_other[i] == "其他")
                    {
                        xml += Temp_param_emotional_state_other[i] + ":" + sel_data(dt, "param_emotional_state_other_txt") + ",";
                    }
                    else
                    {
                        xml += Temp_param_emotional_state_other[i] + ",";
                    }
                }
                xml = xml.Substring(0, xml.Length - 1);
            }
            xml += "</param_emotional_state>";
            xml += "<param_nervous_system>神經系統:" + sel_data(dt, "param_nervous_system");
            if (sel_data(dt, "param_nervous_system") != "正常")
            {
                xml += "，" + sel_data(dt, "param_nervous_system_other");
            }
            xml += "</param_nervous_system>";
            xml += "<param_feel_other>神經系統-感覺:" + sel_data(dt, "param_feel_other");
            if (sel_data(dt, "param_feel_other") != "正常")
            {
                xml += ";" + sel_data(dt, "param_feel_other_other");
            }
            xml += "</param_feel_other>";
            xml += "<param_feel_other_1>";
            if (sel_data(dt, "param_feel_other_1") != "")
            {
                xml += "酸，部位:";
                string[] Temp_param_feel_other_1 = sel_data(dt, "param_feel_other_1").Split(',');
                for (int i = 0; i < Temp_param_feel_other_1.Length; i++)
                {
                    if (Temp_param_feel_other_1[i] == "其他")
                    {
                        xml += Temp_param_feel_other_1[i] + ":" + sel_data(dt, "param_feel_other_other_1_name") + ",";
                    }
                    else
                    {
                        xml += Temp_param_feel_other_1[i] + ",";
                    }
                }
                xml = xml.Substring(0, xml.Length - 1);
            }
            xml += "</param_feel_other_1>";
            xml += "<param_feel_other_2>";
            if (sel_data(dt, "param_feel_other_2") != "")
            {
                xml += "痛，部位:";//右臉
                string[] Temp_param_feel_other_2 = sel_data(dt, "param_feel_other_2").Split(',');
                for (int i = 0; i < Temp_param_feel_other_2.Length; i++)
                {
                    if (Temp_param_feel_other_2[i] == "其他")
                    {
                        xml += Temp_param_feel_other_2[i] + ":" + sel_data(dt, "param_feel_other_other_2_name") + ",";
                    }
                    else
                    {
                        xml += Temp_param_feel_other_2[i] + ",";
                    }
                }
                xml = xml.Substring(0, xml.Length - 1);
            }
            xml += "</param_feel_other_2>";
            xml += "<param_feel_other_3>";
            if (sel_data(dt, "param_feel_other_3") != "")
            {
                xml += "麻，部位:";//右臉
                string[] Temp_param_feel_other_3 = sel_data(dt, "param_feel_other_3").Split(',');
                for (int i = 0; i < Temp_param_feel_other_3.Length; i++)
                {
                    if (Temp_param_feel_other_3[i] == "其他")
                    {
                        xml += Temp_param_feel_other_3[i] + ":" + sel_data(dt, "param_feel_other_other_3_name") + ",";
                    }
                    else
                    {
                        xml += Temp_param_feel_other_3[i] + ",";
                    }
                }
                xml = xml.Substring(0, xml.Length - 1);
            }
            xml += "</param_feel_other_3>";
            xml += "<param_feel_other_4>";
            if (sel_data(dt, "param_feel_other_4") != "")
            {
                xml += "無知覺，部位:";//右臉
                string[] Temp_param_feel_other_4 = sel_data(dt, "param_feel_other_4").Split(',');
                for (int i = 0; i < Temp_param_feel_other_4.Length; i++)
                {
                    if (Temp_param_feel_other_4[i] == "其他")
                    {
                        xml += Temp_param_feel_other_4[i] + ":" + sel_data(dt, "param_feel_other_other_4_name") + ",";
                    }
                    else
                    {
                        xml += Temp_param_feel_other_4[i] + ",";
                    }
                }
                xml = xml.Substring(0, xml.Length - 1);
            }
            xml += "</param_feel_other_4>";
            xml += "<param_feel_other_5>";
            if (sel_data(dt, "param_feel_other_5") != "")
            {
                xml += "抽搐，部位:";//右臉
                string[] Temp_param_feel_other_5 = sel_data(dt, "param_feel_other_5").Split(',');
                for (int i = 0; i < Temp_param_feel_other_5.Length; i++)
                {
                    if (Temp_param_feel_other_5[i] == "其他")
                    {
                        xml += Temp_param_feel_other_5[i] + ":" + sel_data(dt, "param_feel_other_other_5_name") + ",";
                    }
                    else
                    {
                        xml += Temp_param_feel_other_5[i] + ",";
                    }
                }
                xml = xml.Substring(0, xml.Length - 1);
            }
            xml += "</param_feel_other_5>";
            xml += "<param_Skin_appearance>皮膚外觀:" + sel_data(dt, "param_Skin_appearance") + ";" + sel_data(dt, "param_Skin_appearance_other") + "</param_Skin_appearance>";
            xml += "<Skin_appearance_other_1_name>";
            if (sel_data(dt, "Skin_appearance_other_1_name") != "" && sel_data(dt, "param_Skin_appearance_other").ToString().IndexOf("乾燥") > -1)
            {
                xml += "乾燥，部位:";
                string[] Temp_Skin_appearance_other_1_name = sel_data(dt, "Skin_appearance_other_1_name").Split(',');
                for (int i = 0; i < Temp_Skin_appearance_other_1_name.Length; i++)
                {
                    if (Temp_Skin_appearance_other_1_name[i] == "其他")
                    {
                        xml += Temp_Skin_appearance_other_1_name[i] + ":" + sel_data(dt, "txt_span_Skin_appearance_other_1_name_other") + ",";
                    }
                    else
                    {
                        xml += Temp_Skin_appearance_other_1_name[i] + ",";
                    }
                }
                xml = xml.Substring(0, xml.Length - 1);
            }
            xml += "</Skin_appearance_other_1_name>";
            xml += "<Skin_appearance_other_2_name>";
            if (sel_data(dt, "Skin_appearance_other_2_name") != "" && sel_data(dt, "param_Skin_appearance_other").ToString().IndexOf("水腫") > -1)
            {
                xml += "水腫，部位:";
                string[] Temp_Skin_appearance_other_2_name = sel_data(dt, "Skin_appearance_other_2_name").Split(',');
                for (int i = 0; i < Temp_Skin_appearance_other_2_name.Length; i++)
                {
                    if (Temp_Skin_appearance_other_2_name[i] == "其他")
                    {
                        xml += Temp_Skin_appearance_other_2_name[i] + ":" + sel_data(dt, "txt_span_Skin_appearance_other_2_name_other") + ",";
                    }
                    else
                    {
                        xml += Temp_Skin_appearance_other_2_name[i] + ",";
                    }
                }
                xml = xml.Substring(0, xml.Length - 1);
            }
            xml += "</Skin_appearance_other_2_name>";
            xml += "<Skin_appearance_other_3_name>";
            if (sel_data(dt, "Skin_appearance_other_3_name") != "" && sel_data(dt, "param_Skin_appearance_other").ToString().IndexOf("黃疸") > -1)
            {
                xml += "黃疸，部位:";
                //右臉
                string[] Temp_Skin_appearance_other_3_name = sel_data(dt, "Skin_appearance_other_3_name").Split(',');
                for (int i = 0; i < Temp_Skin_appearance_other_3_name.Length; i++)
                {
                    if (Temp_Skin_appearance_other_3_name[i] == "其他")
                    {
                        xml += Temp_Skin_appearance_other_3_name[i] + ":" + sel_data(dt, "txt_span_Skin_appearance_other_3_name_other") + ",";
                    }
                    else
                    {
                        xml += Temp_Skin_appearance_other_3_name[i] + ",";
                    }
                }
                xml = xml.Substring(0, xml.Length - 1);
            }
            xml += "</Skin_appearance_other_3_name>";
            xml += "<Skin_appearance_other_4_name>";
            if (sel_data(dt, "Skin_appearance_other_4_name") != "" && sel_data(dt, "param_Skin_appearance_other").ToString().IndexOf("發紺") > -1)
            {
                xml += "發紺，部位:";//右臉
                string[] Temp_Skin_appearance_other_4_name = sel_data(dt, "Skin_appearance_other_4_name").Split(',');
                for (int i = 0; i < Temp_Skin_appearance_other_4_name.Length; i++)
                {
                    if (Temp_Skin_appearance_other_4_name[i] == "其他")
                    {
                        xml += Temp_Skin_appearance_other_4_name[i] + ":" + sel_data(dt, "txt_span_Skin_appearance_other_4_name_other") + ",";
                    }
                    else
                    {
                        xml += Temp_Skin_appearance_other_4_name[i] + ",";
                    }
                }
                xml = xml.Substring(0, xml.Length - 1);
            }
            xml += "</Skin_appearance_other_4_name>";
            xml += "<Skin_appearance_other_5_name>";
            if (sel_data(dt, "Skin_appearance_other_5_name") != "" && sel_data(dt, "param_Skin_appearance_other").ToString().IndexOf("腫塊") > -1)
            {
                xml += "腫塊，部位:";
                //右臉
                string[] Temp_Skin_appearance_other_5_name = sel_data(dt, "Skin_appearance_other_5_name").Split(',');
                for (int i = 0; i < Temp_Skin_appearance_other_5_name.Length; i++)
                {
                    if (Temp_Skin_appearance_other_5_name[i] == "其他")
                    {
                        xml += Temp_Skin_appearance_other_5_name[i] + ":" + sel_data(dt, "txt_span_Skin_appearance_other_5_name_other") + ",";
                    }
                    else
                    {
                        xml += Temp_Skin_appearance_other_5_name[i] + ",";
                    }
                }
                xml = xml.Substring(0, xml.Length - 1);
            }
            xml += "</Skin_appearance_other_5_name>";
            xml += "<Skin_appearance_other_6_name>";
            if (sel_data(dt, "Skin_appearance_other_6_name") != "" && sel_data(dt, "param_Skin_appearance_other").ToString().IndexOf("疹") > -1)
            {
                xml += "疹，部位:";
                //右臉
                string[] Temp_Skin_appearance_other_6_name = sel_data(dt, "Skin_appearance_other_6_name").Split(',');
                for (int i = 0; i < Temp_Skin_appearance_other_6_name.Length; i++)
                {
                    if (Temp_Skin_appearance_other_6_name[i] == "其他")
                    {
                        xml += Temp_Skin_appearance_other_6_name[i] + ":" + sel_data(dt, "txt_span_Skin_appearance_other_6_name_other") + ",";
                    }
                    else
                    {
                        xml += Temp_Skin_appearance_other_6_name[i] + ",";
                    }
                }
                xml = xml.Substring(0, xml.Length - 1);
            }
            xml += "</Skin_appearance_other_6_name>";
            xml += "<Skin_appearance_other_7_name>";
            if (sel_data(dt, "Skin_appearance_other_7_name") != "" && sel_data(dt, "param_Skin_appearance_other").ToString().IndexOf("癢") > -1)
            {
                xml += "癢，部位:";
                //右臉
                string[] Temp_Skin_appearance_other_7_name = sel_data(dt, "Skin_appearance_other_7_name").Split(',');
                for (int i = 0; i < Temp_Skin_appearance_other_7_name.Length; i++)
                {
                    if (Temp_Skin_appearance_other_7_name[i] == "其他")
                    {
                        xml += Temp_Skin_appearance_other_7_name[i] + ":" + sel_data(dt, "txt_span_Skin_appearance_other_7_name_other") + ",";
                    }
                    else
                    {
                        xml += Temp_Skin_appearance_other_7_name[i] + ",";
                    }
                }
                xml = xml.Substring(0, xml.Length - 1);
            }
            xml += "</Skin_appearance_other_7_name>";
            xml += "<Skin_appearance_other_8_name>";
            if (sel_data(dt, "param_Skin_appearance_other_8_name") != "" && sel_data(dt, "param_Skin_appearance_other").ToString().IndexOf("其他") > -1)
            {
                xml += "其他，部位:" + sel_data(dt, "param_Skin_appearance_other_8_name");
                if (sel_data(dt, "Skin_appearance_other_8_name_ck") != "")
                {
                    xml += "，部位:";
                    //右臉
                    string[] Temp_Skin_appearance_other_8_name = sel_data(dt, "Skin_appearance_other_8_name_ck").Split(',');
                    for (int i = 0; i < Temp_Skin_appearance_other_8_name.Length; i++)
                    {
                        if (Temp_Skin_appearance_other_8_name[i] == "其他")
                        {
                            xml += Temp_Skin_appearance_other_8_name[i] + ":" + sel_data(dt, "span_Skin_appearance_other_8_name_txt") + ",";
                        }
                        else
                        {
                            xml += Temp_Skin_appearance_other_8_name[i] + ",";
                        }
                    }
                    xml = xml.Substring(0, xml.Length - 1);
                }
            }
            xml += "</Skin_appearance_other_8_name>";
            //	<!--傷口-->
            #region --傷口--
            /////保留，之後再回來改
            string[] wound_type = sel_data(dt, "ddl_wound_type").Split(',');
            string[] wound_general = sel_data(dt, "ddl_wound_general").Split(',');
            string[] wound_scald = sel_data(dt, "ddl_wound_scald").Split(',');//燙傷
            string[] wound_date = sel_data(dt, "wound_date").Split(',');//日期
            string[] wound_date_unknown = sel_data(dt, "wound_date_unknown").Split(',');//不確定的checkbox
            xml += "<param_General_wound>一般傷口:" + sel_data(dt, "param_General_wound") + "</param_General_wound>";
            xml += "<wound_type>";
            if (sel_data(dt, "param_General_wound") == "有")
            {
                for (int i = 0; i < wound_type.Length; i++)
                {
                    xml += "傷口種類:";
                    xml += wound_type[i];
                    if (wound_type[i] == "燙傷")
                    {
                        xml += ";部位:" + wound_scald[i] + ";";
                    }
                    else
                    {
                        xml += ";部位:" + wound_general[i] + ";";
                    }
                    //加日期 --先住解~~撈不到值
                    xml += "發生日期:";
                    string Temp_wound_date = (wound_date[i] == "") ? "不詳" : wound_date[i];
                    xml += Temp_wound_date;
                    xml += ";";
                }
            }
            xml += "</wound_type>";
            /*----------------------壓傷傷口---------------*/
            string[] Temp_inHosp = sel_data(dt, "inHosp").Split(',');
            string[] Temp_outHosp = sel_data(dt, "outHosp").Split(',');
            string[] Temp_outHosp_other = sel_data(dt, "outHosp_other").Split(',');
            string[] Temp_wound_pressure = sel_data(dt, "wound_pressure").Split(',');
            string[] Temp_wound_pre_other_txt = sel_data(dt, "wound_pre_other_txt").Split(',');
            string[] wound_pre_date = sel_data(dt, "wound_pre_date").Split(',');
            string[] wound_pre_date_unknown = sel_data(dt, "wound_pre_date_unknown").Split(',');
            xml += "<param_pressure>壓傷傷口:" + sel_data(dt, "param_pressure") + "</param_pressure>";
            xml += "<place>";
            if (sel_data(dt, "param_pressure") == "有")
            {
                for (int i = 0; i < Temp_wound_pre_other_txt.Length; i++)
                {
                    string Temp_place = "";
                    if (i == 0)
                    {
                        Temp_place = sel_data(dt, "place");
                    }
                    else
                    {
                        Temp_place = sel_data(dt, "place_" + i);
                    }
                    xml += "傷口種類:發生地點:";
                    //if(Temp_place == "1")
                    //{
                    //    xml += Temp_inHosp[i].Trim();
                    //}
                    //else
                    //{
                    //    if(Temp_outHosp[i] == "其他醫院")
                    //    {
                    //        xml += "其他醫院:" + Temp_outHosp_other[i];
                    //    }
                    //    else if(Temp_outHosp[i] == "長期養護機構")
                    //    {
                    //        xml += "長期養護機構:" + Temp_outHosp_other[i];
                    //    }
                    //    else
                    //    {
                    //        xml += Temp_outHosp[i];
                    //    }
                    //}

                    if (Temp_place == "1")
                    {
                        xml += "院內:" + Temp_inHosp[i].Trim();
                    }
                    else
                    {
                        if (Temp_outHosp[i] == "其他醫院")
                        {
                            xml += "院外:其他醫院:" + Temp_outHosp_other[i];
                        }
                        else if (Temp_outHosp[i] == "長期養護機構")
                        {
                            xml += "院外:長期養護機構:" + Temp_outHosp_other[i];
                        }
                        else
                        {
                            xml += "院外:" + Temp_outHosp[i];
                        }
                    }
                    xml += ";";
                    //"院外-家中，部位:臀部";
                    xml += "部位:";
                    if (Temp_wound_pressure[i] == "其他")
                    {
                        xml += "其他" + Temp_wound_pre_other_txt[i];
                    }
                    else
                    {
                        xml += Temp_wound_pressure[i];
                    }
                    xml += ";";
                    //加日期 --先住解~~撈不到值
                    xml += "發生日期:";
                    string Temp_wound_pre_date = (wound_pre_date[i] == "") ? "不詳" : wound_pre_date[i];
                    xml += Temp_wound_pre_date;
                    xml += ";";
                }
            }
            xml += "</place>";
            #endregion
            //	<!--感官系統-->
            xml += "<param_eye>感官系統-眼:" + sel_data(dt, "param_eye");
            if (sel_data(dt, "param_eye") == "異常")
            {
                xml += ";";
                xml += sel_data(dt, "param_param_eye_Desc");
            }
            //"，近視,弱視
            xml += "</param_eye>";
            xml += "<param_param_eye_Desc1>";
            if (sel_data(dt, "param_param_eye_Desc1") != "")
            {
                xml += "弱視:" + sel_data(dt, "param_param_eye_Desc1");
            }
            xml += "</param_param_eye_Desc1>";
            xml += "<param_param_eye_Desc2>";
            if (sel_data(dt, "param_param_eye_Desc2") != "")
            {
                xml += "斜視:" + sel_data(dt, "param_param_eye_Desc2");
            }
            xml += "</param_param_eye_Desc2>";
            xml += "<param_param_eye_Desc3>";
            if (sel_data(dt, "param_param_eye_Desc3") != "")
            {
                xml += "分泌物:" + sel_data(dt, "param_param_eye_Desc3");
            }
            xml += "</param_param_eye_Desc3>";
            xml += "<param_param_eye_Desc_other>";
            if (sel_data(dt, "param_param_eye_Desc_other") != "")
            {
                xml += "眼睛其他:" + sel_data(dt, "param_param_eye_Desc_other");
            }
            xml += "</param_param_eye_Desc_other>";
            xml += "<param_ear>感官系統-耳:" + sel_data(dt, "param_ear");
            // + "，聽力障礙
            if (sel_data(dt, "param_ear") == "異常")
            {
                xml += ";";
                xml += sel_data(dt, "param_param_ear_Desc");
            }
            xml += "</param_ear>";
            xml += "<param_param_ear_Desc1>";
            if (sel_data(dt, "param_param_ear_Desc1") != "")
            {
                xml += "聽力障礙:" + sel_data(dt, "param_param_ear_Desc1");
            }
            xml += "</param_param_ear_Desc1>";
            xml += "<param_param_ear_Desc2>";
            if (sel_data(dt, "param_param_ear_Desc2") != "")
            {
                xml += "耳鳴:" + sel_data(dt, "param_param_ear_Desc2");
            }
            xml += "</param_param_ear_Desc2>";
            xml += "<param_param_ear_Desc_other>";
            if (sel_data(dt, "param_param_ear_Desc_other") != "")
            {
                xml += "耳其他:" + sel_data(dt, "param_param_ear_Desc_other");
            }
            xml += "</param_param_ear_Desc_other>";
            xml += "<param_nose>感官系統-鼻:" + sel_data(dt, "param_nose");
            // + ";流鼻水
            if (sel_data(dt, "param_nose") == "異常")
            {
                xml += ";";
                string[] Temp_nose_Desc = sel_data(dt, "param_param_nose_Desc").Split(',');
                for (int i = 0; i < Temp_nose_Desc.Length; i++)
                {
                    if (Temp_nose_Desc[i] == "其他")
                    {
                        xml += Temp_nose_Desc[i] + ":" + sel_data(dt, "param_param_nose_Desc_other") + ",";
                    }
                    else
                    {
                        xml += Temp_nose_Desc[i] + ",";
                    }
                }
                xml = xml.Substring(0, xml.Length - 1);
            }
            xml += "</param_nose>";
            xml += "<param_oral>感官系統-口腔:" + sel_data(dt, "param_oral");
            //兔唇
            if (sel_data(dt, "param_oral") == "異常")
            {
                xml += ";";
                string[] Temp_oral_Desc = sel_data(dt, "param_param_oral_Desc").Split(',');
                for (int i = 0; i < Temp_oral_Desc.Length; i++)
                {
                    if (Temp_oral_Desc[i] == "其他")
                    {
                        xml += Temp_oral_Desc[i] + ":" + sel_data(dt, "param_param_oral_Desc_other") + ",";
                    }
                    else
                    {
                        xml += Temp_oral_Desc[i] + ",";
                    }
                }
                xml = xml.Substring(0, xml.Length - 1);
            }
            xml += "</param_oral>";
            xml += "<param_Breathing_Type>呼吸型態:" + sel_data(dt, "param_Breathing_Type") + ";" + sel_data(dt, "param_Breathing_Type_Abnormal") + "</param_Breathing_Type>";
            xml += "<param_Breathing_Type_txt>";
            if (sel_data(dt, "param_Breathing_Type_txt") != "")
            {
                xml += "呼吸急促:" + sel_data(dt, "param_Breathing_Type_txt") + "次/分";
            }
            xml += "</param_Breathing_Type_txt>";
            xml += "<param_Breathing_Type_other_txt>";
            if (sel_data(dt, "param_Breathing_Type_other_txt") != "")
            {
                xml += "呼吸其他:" + sel_data(dt, "param_Breathing_Type_other_txt");
            }
            xml += "</param_Breathing_Type_other_txt>";
            xml += "<param_respiratory_therapy>醫療輔助器:" + sel_data(dt, "param_respiratory_therapy");
            //氣切,氣管內管,氧氣治療
            if (sel_data(dt, "param_respiratory_therapy") == "有")
            {
                xml += ";";
                xml += sel_data(dt, "param_respiratory_therapy_other");
            }
            xml += "</param_respiratory_therapy>";
            xml += "<param_respiratory_therapy_other_2_rb>";
            if (sel_data(dt, "param_respiratory_therapy_other_2_rb") != "")
            {
                xml += "氧氣治療:" + sel_data(dt, "param_respiratory_therapy_other_2_rb");
            }
            xml += "</param_respiratory_therapy_other_2_rb>";
            string Temp_Breathing_LeftVoive_Abnormal = (sel_data(dt, "param_Breathing_LeftVoive_Abnormal") == "其他") ? sel_data(dt, "param_Breathing_LeftVoive_Abnormal") + ":" + sel_data(dt, "param_Breathing_LeftVoive_Abnormal_other") : sel_data(dt, "param_Breathing_LeftVoive_Abnormal");
            xml += "<param_Breathing_LeftVoive>呼吸音-左側:" + sel_data(dt, "param_Breathing_LeftVoive");
            if (sel_data(dt, "param_Breathing_LeftVoive") == "異常")
            {
                xml += ";" + Temp_Breathing_LeftVoive_Abnormal;
            }
            xml += "</param_Breathing_LeftVoive>";
            string Temp_Breathing_RightVoive_Abnormal = (sel_data(dt, "param_Breathing_RightVoive_Abnormal") == "其他") ? sel_data(dt, "param_Breathing_RightVoive_Abnormal") + ":" + sel_data(dt, "param_Breathing_RightVoive_Abnormal_other") : sel_data(dt, "param_Breathing_RightVoive_Abnormal");
            xml += "<param_Breathing_RightVoive>呼吸音-右側:" + sel_data(dt, "param_Breathing_RightVoive");
            if (sel_data(dt, "param_Breathing_RightVoive") == "異常")
            {
                xml += ";" + Temp_Breathing_RightVoive_Abnormal;
            }
            xml += "</param_Breathing_RightVoive>";
            xml += "<param_Sputum_Amount>痰液:" + sel_data(dt, "param_Sputum_Amount") + "</param_Sputum_Amount>";
            xml += "<param_Sputum_Amount_Option>";
            if (sel_data(dt, "param_Sputum_Amount") == "有")
            {
                xml += "痰液量:" + sel_data(dt, "param_Sputum_Amount_Option");
            }
            xml += "</param_Sputum_Amount_Option>";
            string Temp_param_Sputum_Amount_Color = (sel_data(dt, "param_Sputum_Amount_Color") == "其他") ? sel_data(dt, "param_Sputum_Amount_Color") + ":" + sel_data(dt, "param_Sputum_Amount_Color_other") : sel_data(dt, "param_Sputum_Amount_Color");
            xml += "<param_Sputum_Amount_Color>";
            if (sel_data(dt, "param_Sputum_Amount") == "有")
            {
                xml += "痰液顏色:" + Temp_param_Sputum_Amount_Color;
            }
            xml += "</param_Sputum_Amount_Color>";
            string Temp_param_Sputum_Amount_Type = (sel_data(dt, "param_Sputum_Amount_Type") == "其他") ? sel_data(dt, "param_Sputum_Amount_Type") + ":" + sel_data(dt, "param_Sputum_Amount_Type_other") : sel_data(dt, "param_Sputum_Amount_Type");
            xml += "<param_Sputum_Amount_Type>";
            if (sel_data(dt, "param_Sputum_Amount") == "有")
            {
                xml += "痰液性狀:" + Temp_param_Sputum_Amount_Type;
            }
            xml += "</param_Sputum_Amount_Type>";
            xml += "<param_heartbeat>心臟血管系統:" + sel_data(dt, "param_heartbeat");
            if (sel_data(dt, "param_heartbeat") == "異常")
            {
                xml += ";";
                //"心跳規則
                string[] Temp_heartbeat_Desc = sel_data(dt, "param_param_heartbeat_Desc").Split(',');
                for (int i = 0; i < Temp_heartbeat_Desc.Length; i++)
                {
                    if (Temp_heartbeat_Desc[i] == "其他")
                    {
                        xml += Temp_heartbeat_Desc[i] + ":" + sel_data(dt, "param_param_heartbeat_Desc_other") + ",";
                    }
                    else
                    {
                        xml += Temp_heartbeat_Desc[i] + ",";
                    }
                }
                xml = xml.Substring(0, xml.Length - 1);
            }
            xml += "</param_heartbeat>";
            xml += "<param_defecation>腸胃系統:" + sel_data(dt, "param_defecation") + ";" + sel_data(dt, "param_defecation_Option") + "</param_defecation>";
            xml += "<param_defecation_Option2>";
            if (sel_data(dt, "param_defecation_Option2") != "")
            {
                xml += "疝氣，部位:" + sel_data(dt, "param_defecation_Option2");
            }
            xml += "</param_defecation_Option2>";
            xml += "<param_Peripheral_circulation_other_t>";
            if (sel_data(dt, "param_Peripheral_circulation_other_t") != "")
            {
                xml += "腹瀉:" + sel_data(dt, "param_Peripheral_circulation_other_t") + "次/天";
            }
            xml += " </param_Peripheral_circulation_other_t>";
            xml += "<param_defecation_Option_txt>";
            if (sel_data(dt, "param_defecation_Option_txt") != "")
            {
                xml += "其他:" + sel_data(dt, "param_defecation_Option_txt");
            }
            xml += "</param_defecation_Option_txt>";
            string Temp_Peripheral_circulation_other_1_rb = (sel_data(dt, "param_Peripheral_circulation_other_1_rb") == "其他") ? "其他:" + sel_data(dt, "Peripheral_circulation_other_1_rb_txt") : sel_data(dt, "param_Peripheral_circulation_other_1_rb");
            xml += "<param_Peripheral_circulation_other_1_rb>";
            string xxxxxx = sel_data(dt, "param_defecation_Option");
            if (sel_data(dt, "param_defecation_Option").ToString().IndexOf("腹瀉") > -1)
            {
                xml += "性質:" + Temp_Peripheral_circulation_other_1_rb;
            }
            xml += "</param_Peripheral_circulation_other_1_rb>";

            xml += "<param_param_urinary_Desc>泌尿系統:" + sel_data(dt, "param_urinary");
            if (sel_data(dt, "param_urinary") == "異常")
            {
                xml += ";";
                //"心跳規則
                string[] Temp_urinary_Desc = sel_data(dt, "param_param_urinary_Desc").Split(',');
                for (int i = 0; i < Temp_urinary_Desc.Length; i++)
                {
                    if (Temp_urinary_Desc[i] == "其他")
                    {
                        xml += Temp_urinary_Desc[i] + ":" + sel_data(dt, "param_param_urinary_Desc_other") + ",";
                    }
                    else
                    {
                        xml += Temp_urinary_Desc[i] + ",";
                    }
                }
                xml = xml.Substring(0, xml.Length - 1);
            }
            //+ ";尿量較少,血尿
            xml += "</param_param_urinary_Desc>";
            xml += "<param_Light>生殖系統:" + sel_data(dt, "param_Light") + "</param_Light>";
            xml += "<param_Light_Boy_status>";
            if (sel_data(dt, "param_Light") == "男")
            {
                if (sel_data(dt, "param_Light_Boy_status") != "")
                {
                    xml += "生殖系統-男:" + sel_data(dt, "param_Light_Boy_status");
                    //，尿道下裂,陰囊水腫
                    if (sel_data(dt, "param_Light_Boy_status") == "異常")
                    {
                        if (sel_data(dt, "param_Light_Boy_abnormal") != "")
                        {
                            xml += ";";
                            string[] Temp_Light_Boy_abnormal = sel_data(dt, "param_Light_Boy_abnormal").Split(',');
                            for (int i = 0; i < Temp_Light_Boy_abnormal.Length; i++)
                            {
                                if (Temp_Light_Boy_abnormal[i] == "其他")
                                {
                                    xml += Temp_Light_Boy_abnormal[i] + ":" + sel_data(dt, "param_Light_Boy_abnormal_other") + ",";
                                }
                                else
                                {
                                    xml += Temp_Light_Boy_abnormal[i] + ",";
                                }
                            }
                            xml = xml.Substring(0, xml.Length - 1);
                        }
                    }
                }
            }
            xml += "</param_Light_Boy_status>";
            xml += "<param_Light_Boy_abnormal_other1>";
            if (sel_data(dt, "param_Light") == "男")
            {
                if (sel_data(dt, "param_Light_Boy_abnormal_other1") != "")
                {
                    xml += "睪丸未降:" + sel_data(dt, "param_Light_Boy_abnormal_other1");
                }
            }
            xml += "</param_Light_Boy_abnormal_other1>";
            xml += "<param_FBAbnormal>";
            //if(sel_data(dt, "param_Light") != "男")
            //{
            //xml += "生殖系統-女:" + sel_data(dt, "param_FBAbnormal");
            ////+ ";陰道分泌物
            //if(sel_data(dt, "param_FBAbnormal") == "異常")
            //{
            //    string[] Temp_FBAbnormal_Dtl = sel_data(dt, "param_FBAbnormal_Dtl").Split(',');
            //    for(int i = 0; i < Temp_FBAbnormal_Dtl.Length; i++)
            //    {
            //        if(Temp_FBAbnormal_Dtl[i] == "其他")
            //        {
            //            xml += Temp_FBAbnormal_Dtl[i];
            //            if(sel_data(dt, "param_FBAbnormal_Dtl_other") != "")
            //            {
            //                xml += ":" + sel_data(dt, "param_FBAbnormal_Dtl_other") + ",";
            //            }
            //        }
            //        else
            //        {
            //            xml += Temp_FBAbnormal_Dtl[i] + ",";
            //        }
            //    }
            //    xml = xml.Substring(0, xml.Length - 1);
            //}
            //}
            xml += "</param_FBAbnormal>";
            xml += "<param_MC_status>";
            if (sel_data(dt, "param_Light") != "男")
            {
                xml += "是否初經:" + sel_data(dt, "param_MC_status");
                if (sel_data(dt, "param_MC_status") == "是")
                {
                    xml += ";年齡:" + sel_data(dt, "param_MCStart") + "歲";
                }
            }
            xml += "</param_MC_status>";
            string Temp_Last_MC_L = (sel_data(dt, "param_Last_MC_L") == "不確定") ? "不確定" : sel_data(dt, "param_MC_Y");
            xml += "<param_MC_Y>";
            if (sel_data(dt, "param_Light") != "男")
            {
                if (Temp_Last_MC_L != "")
                {
                    xml += "最後月經日:" + Temp_Last_MC_L;
                }
            }
            xml += "</param_MC_Y>";
            string Temp_Last_MC_C = (sel_data(dt, "param_Last_MC_C") == "不確定") ? "不確定" : sel_data(dt, "param_MCCycle") + "天";
            xml += "<param_MCCycle>";
            if (sel_data(dt, "param_Light") != "男")
            {
                if (Temp_Last_MC_C != "天")
                {
                    xml += "月經週期:" + Temp_Last_MC_C;
                }
            }
            xml += "</param_MCCycle>";
            string Temp_Last_MC_D = (sel_data(dt, "param_Last_MC_D") == "不確定") ? "不確定" : sel_data(dt, "param_MCDay") + "天";
            xml += "<param_MCDay>";
            if (sel_data(dt, "param_Light") != "男")
            {
                if (Temp_Last_MC_D != "天")
                {
                    xml += "月經天數:" + Temp_Last_MC_D;
                }
            }
            xml += "</param_MCDay>";
            xml += "<param_MCAmount>";
            if (sel_data(dt, "param_Light") != "男")
            {
                xml += "月經量:" + sel_data(dt, "param_MCAmount");
            }
            xml += "</param_MCAmount>";
            xml += "<param_FBAbnormalDtl>";
            if (sel_data(dt, "param_Light") != "男")
            {
                xml += "月經期間:";
                //腹痛,腰痛,頭痛
                string[] Temp_param_FBAbnormalDtl = sel_data(dt, "param_FBAbnormalDtl").Split(',');
                for (int i = 0; i < Temp_param_FBAbnormalDtl.Length; i++)
                {
                    if (Temp_param_FBAbnormalDtl[i] == "其他")
                    {
                        xml += Temp_param_FBAbnormalDtl[i] + ":" + sel_data(dt, "param_FBAbnormalOther") + ",";

                    }
                    else
                    {
                        xml += Temp_param_FBAbnormalDtl[i] + ",";
                    }
                }
                xml = xml.Substring(0, xml.Length - 1);
            }
            xml += "</param_FBAbnormalDtl>";

            xml += "<param_skeleton>骨骼系統:" + sel_data(dt, "param_skeleton") + ";" + sel_data(dt, "param_skeleton_Desc") + "</param_skeleton>";
            xml += "<param_skeleton_Desc_fracture>";
            if (sel_data(dt, "param_skeleton_Desc_fracture") != "")
            {
                xml += "骨折，部位:" + sel_data(dt, "param_skeleton_Desc_fracture");
            }
            xml += "</param_skeleton_Desc_fracture>";
            xml += "<param_skeleton_Desc_dislocation>";
            if (sel_data(dt, "param_skeleton_Desc_dislocation") != "")
            {
                xml += "脫臼，部位:" + sel_data(dt, "param_skeleton_Desc_dislocation");
            }
            xml += "</param_skeleton_Desc_dislocation>";
            //xml += "<param_skeleton_Desc_arthrosis>關節腫，部位:" + sel_data(dt, "param_skeleton_Desc_arthrosis") + "</param_skeleton_Desc_arthrosis>";
            //xml += "<param_skeleton_Desc_amputation>截肢，部位:" + sel_data(dt, "param_skeleton_Desc_amputation") + "</param_skeleton_Desc_amputation>";
            xml += "<param_skeleton_Desc_deformity>";
            if (sel_data(dt, "param_skeleton_Desc_deformity") != "")
            {
                xml += "畸形，部位:" + sel_data(dt, "param_skeleton_Desc_deformity");
            }
            xml += "</param_skeleton_Desc_deformity>";
            xml += "<param_skeleton_Desc_other>";
            if (sel_data(dt, "param_skeleton_Desc_other") != "")
            {
                xml += "其他:" + sel_data(dt, "param_skeleton_Desc_other");
            }
            xml += "</param_skeleton_Desc_other>";

            //xml+="	<param_skeleton>骨骼系統:異常，骨折,脫臼</param_skeleton>";
            //xml+="	<param_skeleton_Desc_fracture>骨折，部位:右大粗隆</param_skeleton_Desc_fracture>";
            //xml+="	<param_skeleton_Desc_deformity>畸形，部位:右大粗隆</param_skeleton_Desc_deformity>";
            //xml+="	<param_skeleton_Desc_dislocation>脫臼，部位:右大粗隆</param_skeleton_Desc_dislocation>";
            //xml+="	<param_skeleton_Desc_other>骨骼異常其他，部位:右大粗隆</param_skeleton_Desc_other>";

            #endregion
            #region --壓傷危險性評估--
            xml += "<param_feeling_pressure_sores>感覺:" + sel_data(dt, "param_feeling_pressure_sores") + "</param_feeling_pressure_sores>";
            xml += "<param_wet_pressure_sores>潮濕:" + sel_data(dt, "param_wet_pressure_sores") + "</param_wet_pressure_sores>";
            xml += "<param_moving_pressure_sores>移動:" + sel_data(dt, "param_moving_pressure_sores") + "</param_moving_pressure_sores>";
            xml += "<param_activities_pressure_sores>活動:" + sel_data(dt, "param_activities_pressure_sores") + "</param_activities_pressure_sores>";
            xml += "<param_nutrition_pressure_sores>營養:" + sel_data(dt, "param_nutrition_pressure_sores") + "</param_nutrition_pressure_sores>";
            xml += "<param_friction_pressure_sores>磨擦力和剪力:" + sel_data(dt, "param_friction_pressure_sores") + "</param_friction_pressure_sores>";
            xml += "<param_total_pressure_sores>總分:" + sel_data(dt, "param_total_pressure_sores") + "</param_total_pressure_sores>";
            xml += "<param_pressure_assessment>壓傷危險因子:" + sel_data(dt, "param_pressure_assessment");
            if (sel_data(dt, "param_pressure_assessment") == "有")
            {
                xml = xml + ";";
                string[] Temp_param_pressure_assessment_Desc = sel_data(dt, "param_pressure_assessment_Desc").Split(',');
                for (int i = 0; i < Temp_param_pressure_assessment_Desc.Length; i++)
                {
                    if (Temp_param_pressure_assessment_Desc[i] == "其他")
                    {
                        xml += Temp_param_pressure_assessment_Desc[i] + ":" + sel_data(dt, "param_pressure_assessment_Desc_other") + ",";
                    }
                    else
                    {
                        xml += Temp_param_pressure_assessment_Desc[i] + ",";
                    }
                }
                xml = xml.Substring(0, xml.Length - 1);
            }
            //意識障礙
            xml += "</param_pressure_assessment>";
            #endregion
            #region --營養狀態評估--
            xml += "<param_Evaluation_methods>評估方式:" + sel_data(dt, "param_Evaluation_methods") + "</param_Evaluation_methods>";
            xml += "<param_measuring_age>";
            if (sel_data(dt, "param_measuring_age") != "")
            {
                xml += "年齡(月):" + sel_data(dt, "param_measuring_age");
            }
            xml += "</param_measuring_age>";

            xml += "<param_body_height>";
            if (sel_data(dt, "param_body_height") != "")
            {
                xml += "身高:" + sel_data(dt, "param_body_height");
            }
            xml += "</param_body_height>";
            xml += "<param_percentage_H>";
            if (sel_data(dt, "param_percentage_H") != "")
            {
                xml += "身高百分比:" + sel_data(dt, "param_percentage_H");
            }
            xml += "</param_percentage_H>";
            xml += "<param_body_weight>";
            if (sel_data(dt, "param_body_weight") != "")
            {
                xml += "體重:" + sel_data(dt, "param_body_weight");
            }
            xml += "</param_body_weight>";
            xml += "<param_percentage_W>";
            if (sel_data(dt, "param_percentage_W") != "")
            {
                xml += "體重百分比:" + sel_data(dt, "param_percentage_W");
            }
            xml += "</param_percentage_W>";
            xml += "<bmiHint>";
            if (sel_data(dt, "param_body_height") != "" && sel_data(dt, "param_body_weight") != "" && sel_data(dt, "param_body_weight") != null && sel_data(dt, "param_body_height") != null)
            {
                string tempbmi = BMI_Compute(Convert.ToDouble(sel_data(dt, "param_body_height")), Convert.ToDouble(sel_data(dt, "param_body_weight")));
                xml += "BMI:";
                xml += tempbmi + ";" + BMIajax(tempbmi, ptinfo.PatientGender, ptinfo.Age);
            }
            xml += "</bmiHint>";
            xml += "<param_percentage_BMI>";
            if (sel_data(dt, "param_percentage_BMI") != "")
            {
                xml += "BMI百分比區間:" + sel_data(dt, "param_percentage_BMI");
            }
            xml += "</param_percentage_BMI>";
            xml += "<param_body_gthr>";
            if (sel_data(dt, "param_body_gthr") != "")
            {
                xml += "頭圍:" + sel_data(dt, "param_body_gthr");
            }
            xml += "</param_body_gthr>";
            xml += "<param_percentage_pHead>";
            if (sel_data(dt, "param_percentage_pHead") != "")
            {
                xml += "頭圍百分比:" + sel_data(dt, "param_percentage_pHead");
            }
            xml += "</param_percentage_pHead>";
            xml += "<param_nutrition_quality>";
            if (sel_data(dt, "param_Evaluation_methods") == "MUST營養評估")
            {
                xml += "身體質量指數:" + sel_data(dt, "param_nutrition_quality");
            }
            xml += "</param_nutrition_quality>";
            xml += "<param_nutrition_loss>";
            if (sel_data(dt, "param_Evaluation_methods") == "MUST營養評估")
            {
                xml += "三到六個月體重減輕:" + sel_data(dt, "param_nutrition_loss");
            }
            xml += "</param_nutrition_loss>";
            xml += "<param_nutrition_fasting>";
            if (sel_data(dt, "param_Evaluation_methods") == "MUST營養評估")
            {
                xml += "急性疾病狀態幾乎無進食或禁食:" + sel_data(dt, "param_nutrition_fasting");
            }
            xml += "</param_nutrition_fasting>";
            xml += "<param_nutrition_totle>";
            if (sel_data(dt, "param_Evaluation_methods") == "MUST營養評估")
            {
                xml += "MUST總分:" + sel_data(dt, "param_nutrition_totle");
                if (sel_data(dt, "param_nutrition_totle") != "" && Convert.ToInt32(sel_data(dt, "param_nutrition_totle")) < 3)
                {
                    xml += "(MUST小於3分，暫時不需要進一步的營養評估)";
                }
                if (sel_data(dt, "param_nutrition_totle") != "" && Convert.ToInt32(sel_data(dt, "param_nutrition_totle")) >= 3)
                {
                    xml += "(MUST≧3分為營養不良高度風險，通知主治醫師決定是否照會營養師做進一步的營養評估)";
                }
            }
            xml += "</param_nutrition_totle>";
            #endregion
            #region --高危險跌倒評估--
            xml += "<param_age_fall>年齡:" + sel_data(dt, "param_age_fall") + "</param_age_fall>";
            xml += "<param_sex_fall>性別:" + sel_data(dt, "param_sex_fall") + "</param_sex_fall>";
            xml += "<param_fitness_fall>體能狀況:" + sel_data(dt, "param_fitness_fall") + "</param_fitness_fall>";
            xml += "<param_history_fall>跌倒史:" + sel_data(dt, "param_history_fall") + "</param_history_fall>";
            xml += "<param_motility_fall>活動力:" + sel_data(dt, "param_motility_fall") + "</param_motility_fall>";
            xml += "<param_drug_fall>藥物(抗組織胺類、抗癲癇藥物):" + sel_data(dt, "param_drug_fall") + "</param_drug_fall>";
            xml += "<param_disease_fall>疾病因素:" + sel_data(dt, "param_disease_fall") + "</param_disease_fall>";
            xml += "<param_total_fall>總分:" + sel_data(dt, "param_total_fall") + "分</param_total_fall>";
            #endregion
            #region --住院病友心情評估--
            xml += "<param_mood_age>年齡層:" + sel_data(dt, "param_mood_age") + "</param_mood_age>";
            xml += "<param_mood>無法評估:" + sel_data(dt, "param_mood") + "</param_mood>";
            string Temp_mood_yes_Dtl = (sel_data(dt, "param_mood_yes_Dtl") == "其他") ? "其他:" + sel_data(dt, "param_mood_yes_Dtl_txt") : sel_data(dt, "param_mood_yes_Dtl");
            xml += "<param_mood_yes_Dtl>";
            if (sel_data(dt, "param_mood") == "是")
            {
                xml += "無法評估原因:" + Temp_mood_yes_Dtl;
            }
            xml += "</param_mood_yes_Dtl>";
            xml += "<param_mood_jittery>";
            if (sel_data(dt, "param_mood") != "是")
            {
                xml += "感覺緊張不安:" + sel_data(dt, "param_mood_jittery");
            }
            xml += "</param_mood_jittery>";
            xml += "<param_mood_distress_flare>";
            if (sel_data(dt, "param_mood") != "是")
            {
                xml += "感覺容易苦惱或動怒:" + sel_data(dt, "param_mood_distress_flare");
            }
            xml += "</param_mood_distress_flare>";
            xml += "<param_mood_gloomy>";
            if (sel_data(dt, "param_mood") != "是")
            {
                xml += "感覺憂鬱、心情低落:" + sel_data(dt, "param_mood_gloomy");
            }
            xml += "</param_mood_gloomy>";
            xml += "<param_mood_feeling_failure>";
            if (sel_data(dt, "param_mood") != "是")
            {
                xml += "覺得比不上別人:" + sel_data(dt, "param_mood_feeling_failure");
            }
            xml += "</param_mood_feeling_failure>";
            xml += "<param_mood_difficulty_sleeping>";
            if (sel_data(dt, "param_mood") != "是")
            {
                xml += "睡眠困難，譬如難以入睡、易睡或早醒:" + sel_data(dt, "param_mood_difficulty_sleeping");
            }
            xml += "</param_mood_difficulty_sleeping>";
            xml += "<param_mood_total>";
            if (sel_data(dt, "param_mood") != "是")
            {
                xml += "總分:" + sel_data(dt, "param_mood_total");
            }
            xml += "</param_mood_total>";
            xml += "<param_mood_suicidal_thoughts>";
            if (sel_data(dt, "param_mood") != "是")
            {
                xml += "有自殺的想法:" + sel_data(dt, "param_mood_suicidal_thoughts");
            }
            xml += "</param_mood_suicidal_thoughts>";
            xml += "<param_Spirituality>靈性評估:" + sel_data(dt, "param_Spirituality") + "</param_Spirituality>";
            xml += "<Spirituality_issues_patient>";
            string Temp_Spirituality_issues_patient = string.Empty, Temp_Spirituality_issues_family = string.Empty, Temp_Spirituality_religion_patient = string.Empty, Temp_Spirituality_religion_family = string.Empty;
            if (sel_data(dt, "param_Spirituality") == "需要")
            {
                if (sel_data(dt, "Spirituality_issues_patient") == "有")
                {
                    Temp_Spirituality_issues_patient = sel_data(dt, "Spirituality_issues_patient");
                    Temp_Spirituality_issues_patient = Temp_Spirituality_issues_patient + "，";
                    string[] param_Spirituality_issues_patient = sel_data(dt, "param_Spirituality_issues_patient").Split(',');
                    for (int i = 0; i < param_Spirituality_issues_patient.Length; i++)
                    {
                        if (param_Spirituality_issues_patient[i] == "其他")
                        {
                            Temp_Spirituality_issues_patient += param_Spirituality_issues_patient[i] + ":" + sel_data(dt, "param_Spirituality_issues_patient_other") + ",";
                        }
                        else
                        {
                            Temp_Spirituality_issues_patient += param_Spirituality_issues_patient[i] + ",";
                        }
                    }
                    Temp_Spirituality_issues_patient = Temp_Spirituality_issues_patient.Substring(0, Temp_Spirituality_issues_patient.Length - 1);
                }
                else
                {
                    Temp_Spirituality_issues_patient = sel_data(dt, "Spirituality_issues_patient");
                }
                xml += "社會問題-病人:" + Temp_Spirituality_issues_patient;
            }
            xml += "</Spirituality_issues_patient>";
            xml += "<Spirituality_issues_family>";
            if (sel_data(dt, "param_Spirituality") == "需要")
            {
                if (sel_data(dt, "Spirituality_issues_family") == "有")
                {
                    Temp_Spirituality_issues_family = sel_data(dt, "Spirituality_issues_family");
                    Temp_Spirituality_issues_family = Temp_Spirituality_issues_family + "，";
                    string[] param_Spirituality_issues_family = sel_data(dt, "param_Spirituality_issues_family").Split(',');
                    for (int i = 0; i < param_Spirituality_issues_family.Length; i++)
                    {
                        if (param_Spirituality_issues_family[i] == "其他")
                        {
                            Temp_Spirituality_issues_family += param_Spirituality_issues_family[i] + ":" + sel_data(dt, "param_Spirituality_issues_family_other") + ",";
                        }
                        else
                        {
                            Temp_Spirituality_issues_family += param_Spirituality_issues_family[i] + ",";
                        }
                    }
                    Temp_Spirituality_issues_family = Temp_Spirituality_issues_family.Substring(0, Temp_Spirituality_issues_family.Length - 1);
                }
                else
                {
                    Temp_Spirituality_issues_family = sel_data(dt, "Spirituality_issues_family");
                }
                xml += "社會問題-家屬:" + Temp_Spirituality_issues_family;
            }
            xml += "</Spirituality_issues_family>";
            xml += "<Spirituality_religion_patient>";
            if (sel_data(dt, "param_Spirituality") == "需要")
            {
                if (sel_data(dt, "Spirituality_religion_patient") == "有")
                {
                    Temp_Spirituality_religion_patient = sel_data(dt, "Spirituality_religion_patient");
                    Temp_Spirituality_religion_patient = Temp_Spirituality_religion_patient + "，";
                    string[] param_Spirituality_religion_patient = sel_data(dt, "param_Spirituality_religion_patient").Split(',');
                    for (int i = 0; i < param_Spirituality_religion_patient.Length; i++)
                    {
                        if (param_Spirituality_religion_patient[i] == "其他")
                        {
                            Temp_Spirituality_religion_patient += param_Spirituality_religion_patient[i] + ":" + sel_data(dt, "param_Spirituality_religion_patient_other") + ",";
                        }
                        else
                        {
                            Temp_Spirituality_religion_patient += param_Spirituality_religion_patient[i] + ",";
                        }
                    }
                    Temp_Spirituality_religion_patient = Temp_Spirituality_religion_patient.Substring(0, Temp_Spirituality_religion_patient.Length - 1);
                }
                else
                {
                    Temp_Spirituality_religion_patient = sel_data(dt, "Spirituality_religion_patient");
                }
                xml += "靈性宗教問題-病人:" + Temp_Spirituality_religion_patient;
            }
            xml += "</Spirituality_religion_patient>";
            xml += "<Spirituality_religion_family>";
            if (sel_data(dt, "param_Spirituality") == "需要")
            {
                if (sel_data(dt, "Spirituality_religion_family") == "有")
                {
                    Temp_Spirituality_religion_family = sel_data(dt, "Spirituality_religion_family");
                    Temp_Spirituality_religion_family = Temp_Spirituality_religion_family + "，";
                    string[] param_Spirituality_religion_family = sel_data(dt, "param_Spirituality_religion_family").Split(',');
                    for (int i = 0; i < param_Spirituality_religion_family.Length; i++)
                    {
                        if (param_Spirituality_religion_family[i] == "其他")
                        {
                            Temp_Spirituality_religion_family += param_Spirituality_religion_family[i] + ":" + sel_data(dt, "param_Spirituality_religion_family_other") + ",";
                        }
                        else
                        {
                            Temp_Spirituality_religion_family += param_Spirituality_religion_family[i] + ",";
                        }
                    }
                    Temp_Spirituality_religion_family = Temp_Spirituality_religion_family.Substring(0, Temp_Spirituality_religion_family.Length - 1);
                }
                else
                {
                    Temp_Spirituality_religion_family = sel_data(dt, "Spirituality_religion_family");
                }
                xml += "靈性宗教問題-家屬:" + Temp_Spirituality_religion_family;
            }
            xml += "</Spirituality_religion_family>";
            #endregion
            #region --疼痛評估--
            xml += "<param_pain_assessment_assess>疼痛評估工具:" + sel_data(dt, "param_pain_assessment_assess") + "</param_pain_assessment_assess>";
            xml += "<param_pain_assessment_record>疼痛項目:" + sel_data(dt, "param_pain_assessment_record") + "</param_pain_assessment_record>";
            #region 拋轉VitalSign 疼痛
            int ps_value = 0;
            if (sel_data(dt, "param_pain_assessment_assess") != "" && base.switchAssessmentInto == "Y")
            {
                string vs_record = sel_data(dt, "param_pain_assessment_record");
                //疼痛計分
                Regex rgx = new Regex(@"^([(]\d+[)])|^(\d+)");
                foreach (string ps in vs_record.ToString().Split('|'))
                {
                    if (ps != "")
                    {
                        ps_value += int.Parse(rgx.Match(ps).ToString().Replace("(", "").Replace(")", ""));
                    }
                }
            }
            #endregion
            xml += "<param_pain_assessment_level>疼痛總分:" + ps_value + "</param_pain_assessment_level>";
            #endregion

            #region --主要護理問題--
            xml += "<param_ipd_Nursing_Program>主要護理問題:" + sel_data(dt, "param_ipd_Nursing_Program") + "</param_ipd_Nursing_Program>";

            #endregion

            #region --TOCC--
            //症狀
            if (sel_data(dt, "param_symptom") != "")
            {
                string symptom = "";
                string symptomOther = "";
                symptom += "<param_symptom>";
                symptom += "症狀:";
                symptom += sel_data(dt, "param_symptom");
                if (sel_data(dt, "param_symptom_other") != "")
                {
                    symptomOther = sel_data(dt, "param_symptom_other");
                }
                symptom = symptom.Replace("其他", symptomOther);
                symptom += "</param_symptom>";

                xml += symptom;
            }
            //職業別
            if (sel_data(dt, "param_occupation") != "")
            {
                string occupation = "";
                string occupationOther = "";
                occupation += "<param_occupation>";
                occupation += "職業別:";
                occupation += sel_data(dt, "param_occupation");
                if (sel_data(dt, "param_occupation_other") != "")
                {
                    occupationOther += sel_data(dt, "param_occupation_other");
                }
                occupation += "</param_occupation>";
                occupation = occupation.Replace("其他", occupationOther);
                xml += occupation;
            }
            //旅遊史(Travel)
            if (sel_data(dt, "param_travel") != "")
            {
                xml += "<param_travel>";
                xml += "旅遊史:";
                xml += sel_data(dt, "param_travel");
                xml += "</param_travel>";
                string domestic = "";
                string domesticOther = "";
                string aboard = "";
                string aboardOther = "";
                //國內
                if (sel_data(dt, "param_travel_domestic_city") != "")
                {
                    domestic += "<param_travel_domestic_city>城市: ";
                    domestic += sel_data(dt, "param_travel_domestic_city");
                    domestic += "</param_travel_domestic_city>";
                }
                if (sel_data(dt, "param_travel_domestic_viewpoint") != "")
                {
                    domestic += "<param_travel_domestic_viewpoint>景點: ";
                    domestic += sel_data(dt, "param_travel_domestic_viewpoint");
                    domestic += "</param_travel_domestic_viewpoint>";
                }
                if (sel_data(dt, "param_travel_domestic_traffic") != "")
                {
                    domestic += "<param_travel_domestic_traffic>交通方式(國內): ";
                    domestic += sel_data(dt, "param_travel_domestic_traffic");
                    if (sel_data(dt, "param_travel_domestic_traffic_other") != "")
                    {
                        domesticOther += sel_data(dt, "param_travel_domestic_traffic_other");
                    }
                    domestic = domestic.Replace("其他", domesticOther);
                    domestic += "</param_travel_domestic_traffic>";
                    xml += domestic;
                }

                //國外
                if (sel_data(dt, "param_travel_aboard_country") != "")
                {
                    aboard += "<param_travel_aboard_country>國家: ";
                    aboard += sel_data(dt, "param_travel_aboard_country");
                    aboard += "</param_travel_aboard_country>";
                }
                if (sel_data(dt, "param_travel_aboard_destination") != "")
                {
                    aboard += "<param_travel_aboard_destination>目的地: ";
                    aboard += sel_data(dt, "param_travel_aboard_destination");
                    aboard += "</param_travel_aboard_destination>";
                }
                if (sel_data(dt, "param_travel_aboard_traffic") != "")
                {
                    aboard += "<param_travel_aboard_traffic>交通方式(國外):";
                    aboard += sel_data(dt, "param_travel_aboard_traffic");
                    if (sel_data(dt, "param_travel_aboard_traffic_other") != "")
                    {
                        aboardOther += "(" + sel_data(dt, "param_travel_aboard_traffic_other") + ")";
                    }
                    aboard = aboard.Replace("其他", aboardOther);
                    aboard += "</param_travel_aboard_traffic>";
                    xml += aboard;

                }
            }
            //接觸史
            if (sel_data(dt, "param_contact") != "" || sel_data(dt, "param_contact_child_symptom") != "")
            {
                string contact = "";
                string contactOther = "";
                contact += "<param_contact>";
                contact += "接觸史:";
                contact += sel_data(dt, "param_contact");
                //other
                if (sel_data(dt, "param_contact_other") != "")
                {
                    contactOther += sel_data(dt, "param_contact_other");
                }
                contact = contact.Replace("其他", contactOther);
                contact += "</param_contact>";
                xml += contact;

                //禽鳥

                if (sel_data(dt, "param_contact_birds") != "")
                {
                    xml += "<span_contact_birds>";
                    xml += "接觸禽鳥類、畜類等 : ";
                    xml += sel_data(dt, "param_contact_birds");
                    xml += "</span_contact_birds>";
                }
                //1
                if (sel_data(dt, "param_contact_child_symptom") != "")
                {
                    xml += "<param_contact_child_symptom>";
                    xml += "住院前 14 天內，同住家人有發燒、腹瀉、咳嗽、流鼻水等疑似感染症狀?";
                    xml += sel_data(dt, "param_contact_child_symptom");
                    xml += "</param_contact_child_symptom>";
                }
                //2
                if (sel_data(dt, "param_contact_child_sickleave") != "")
                {
                    xml += "<param_contact_child_sickleave>";
                    xml += "住院前 14 天內，同住家人學校班上同學有因為傳染病請假或班級停課之情形? ";
                    xml += sel_data(dt, "param_contact_child_sickleave");
                    xml += "</param_contact_child_sickleave>";
                }
                //3
                if (sel_data(dt, "param_contact_child_symptomcaregiver") != "")
                {
                    xml += "<param_contact_child_symptomcaregiver>";
                    xml += "住院期間照顧者(應盡量維持同一人)，目前有發燒、腹瀉、咳嗽、流鼻水等疑似感染症狀? ";
                    xml += sel_data(dt, "param_contact_child_symptomcaregiver");
                    xml += "</param_contact_child_symptomcaregiver>";
                }
            }
            //群聚史
            if (sel_data(dt, "param_cluster") != "")
            {
                string cluster = "";
                string clusterOther = "";
                cluster += "<param_cluster>";
                cluster += "群聚史: ";
                cluster += sel_data(dt, "param_cluster");
                cluster += "</param_cluster>";
                if (sel_data(dt, "param_cluster_relatives") != "")
                {
                    cluster += "<param_cluster_relatives>";
                    cluster += sel_data(dt, "param_cluster_relatives");
                    if (sel_data(dt, "param_cluster_relatives_other") != "")
                    {
                        clusterOther += sel_data(dt, "param_cluster_relatives_other");
                    }
                    cluster = cluster.Replace("其他", clusterOther);
                    cluster += "</param_cluster_relatives>";
                }
                xml += cluster;
            }
            #endregion

            xml += "</Document>";
            return xml;
        }
        #endregion

        /// <summary>
        /// 計算BMI
        /// </summary>
        /// <param name="Height">身高</param>
        /// <param name="Weight">體重</param>
        /// <returns>BMI值</returns>
        public string BMI_Compute(double Height, double Weight)
        {
            string result = "";
            if (Height != 0.0D && Weight != 0.0D)
            {
                result = (Weight / (Height * Height / 10000)).ToString("F2");
            }
            return result;
        }

        /// <summary>
        /// 1.在兒童的入評view有使用ajax傳值
        /// 2.在EMR的組字簽章也有使用到
        /// </summary>
        /// <param name="bmi">BMI值字串型態</param>
        /// <returns>評語字串</returns>
        public string BMIajax(string bmi, string sex, int age)
        {
            return ass_m.Get_BMI_String(ass_m.GetBMISort(sex.Trim(), age), bmi.Trim());

        }
    }
}