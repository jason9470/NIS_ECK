using Newtonsoft.Json;
using NIS.Data;
using NIS.Models;
using NIS.UtilTool;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web.Mvc;
using System.Linq;
using NIS.Models.DBModel;

namespace NIS.Controllers
{
    public class HISViewController : BaseController
    {
        private CommData cd;    //常用資料Module
        private DBConnector link;
        private TubeManager tubem;
        private CareRecord care_record_m;
        private Wound wound;
        private IOManager iom;
        private BloodSugarAndInsulin bai;
        private ConstraintsAssessment ca;
        private CommonMedication cm;
        private FlushCatheter fc;
        private DigitFlapTemperature dft;
        private CVVH_Data cvvh_data;


        // == 給藥紀錄用到的 DataTable ↓==
        private DataTable dt_udorder = new DataTable();
        private DataTable dt_stat = new DataTable();
        private DataTable dt_reg = new DataTable();
        private DataTable dt_prn = new DataTable();
        private DataTable dt_iv = new DataTable();
        private DataTable dt_all = new DataTable();
        // == 給藥紀錄用到的 DataTable ↑==

        //建構式
        public HISViewController()
        {   //建立一般資料物件
            this.link = new DBConnector();
            this.cd = new CommData();
            this.tubem = new TubeManager();
            this.care_record_m = new CareRecord();
            this.wound = new Wound();
            this.iom = new IOManager();
            this.bai = new BloodSugarAndInsulin();
            this.ca = new ConstraintsAssessment();
            this.cm = new CommonMedication();
            this.fc = new FlushCatheter();
            this.dft = new DigitFlapTemperature();
            this.cvvh_data = new CVVH_Data();
        }

        #region IO

        #region IO-新
        ////網址範例http://172.20.110.223/NIS/HisView/Index?feeno=1050328001
        [HttpGet]
        public ActionResult Index(string feeno, string date = "", string hour = "", string unit = "",string type="")
        {
            getSession(feeno); //HISVIEW 不走 MainController,因此另外處理session
            switch (type)
            {
                case "nis":
                    Response.Write("<script>window.location.href='../IOManage/Inquire?hisview=T&feeno=" + feeno + "&date=" + date + "&hour=" + hour + "';</script>");
                    break;
                case "obs":
                    Response.Write("<script>window.location.href='../IOManage/Inquire_obs?hisview=T&feeno=" + feeno + "&date=" + date + "&hour=" + hour + "';</script>");
                    break;
                default:
                    //Response.Write("<script>window.location.href='../HISView/Index?feeno=" + form["feeno"] + "&date=" + form["txtdate"] + "&hour=" + form["ddl_total_interval"] + "';</script>");
                    break;
            }
            return new EmptyResult();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Index(FormCollection form)
        {
            getSession(form["feeno"]); //HISVIEW 不走 MainController,因此另外處理session
            switch (form["type"])
            {
                case "nis":
                    Response.Write("<script>window.location.href='../IOManage/Inquire?hisview=T&feeno=" + form["feeno"] + "&date=" + form["txtdate"] + "&hour=" + form["ddl_total_interval"] + "&feeno=" + ptinfo.FeeNo + "';</script>");
                    break;
                case "obs":
                    Response.Write("<script>window.location.href='../IOManage/Inquire_obs?hisview=T&feeno=" + form["feeno"] + "&date=" + form["txtdate"] + "&hour=" + form["ddl_total_interval"] + "&feeno=" + ptinfo.FeeNo + "';</script>");
                    break;
                default:
                    //Response.Write("<script>window.location.href='../HISView/Index?feeno=" + form["feeno"] + "&date=" + form["txtdate"] + "&hour=" + form["ddl_total_interval"] + "';</script>");
                    break;
            }
            return new EmptyResult();
        }
        #endregion

        [HttpGet]
        public ActionResult Old_Index(string feeno)
        {
            DateTime now = DateTime.Now;

            DataTable dt_i_item = iom.sel_sys_params_kind("iotype", "intaketype");
            DataTable dt_o_item = iom.sel_sys_params_kind("iotype", "outputtype");
            DataTable dt_io_data = iom.sel_io_data("", feeno, "", now.ToString("yyyy/MM/dd 07:01:00"), now.AddDays(1).ToString("yyyy/MM/dd 07:00:59"), "1");
            DataTable dt = new DataTable();

            string[] column = { "DATE", "P_VALUE", "AMOUNT", "CLORIE", "REASON", "TYPE" };
            iom.set_dt_column(dt, column);
            iom.set_new_list(dt_io_data, dt_i_item, dt_o_item, dt, now, now);

            ViewBag.dt = dt;
            ViewBag.feeno = feeno;
            ViewBag.dt_i_item = dt_i_item;
            ViewBag.dt_o_item = dt_o_item;

            return View();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Old_Index(FormCollection form)
        {
            string feeno = form["feeno"];
            getSession(feeno); //HISVIEW 不走 MainController,因此另外處理session
            DateTime start = Convert.ToDateTime(form["start_date"]);
            DateTime end = Convert.ToDateTime(form["end_date"]);
            TimeSpan Total = end.Subtract(start);
            ViewBag.key = Total.Days;
            ViewBag.start_date = start;
            ViewBag.end_date = end;

            DataTable dt_i_item = iom.sel_sys_params_kind("iotype", "intaketype");
            DataTable dt_o_item = iom.sel_sys_params_kind("iotype", "outputtype");
            DataTable dt_io_data = iom.sel_io_data("", feeno, "", start.ToString("yyyy/MM/dd 07:01:00"), end.AddDays(1).ToString("yyyy/MM/dd 07:00:59"), form["unit"]);
            DataTable dt = new DataTable();

            string[] column = { "DATE", "P_VALUE", "AMOUNT", "CLORIE", "REASON", "TYPE" };
            iom.set_dt_column(dt, column);
            iom.set_new_list(dt_io_data, dt_i_item, dt_o_item, dt, start, end);

            ViewBag.dt = dt;
            ViewBag.dt_i_item = dt_i_item;
            ViewBag.dt_o_item = dt_o_item;
            ViewBag.seleunit = form["unit"];
            ViewBag.feeno = feeno;

            return View();
        }

        #region Detail
        [HttpGet]
        public ActionResult Detail(string feeno, string date, string day)
        {
            getSession(feeno); //HISVIEW 不走 MainController,因此另外處理session
            string userno = "";
            DateTime now = Convert.ToDateTime(day);
            DataTable dt = new DataTable();
            DateTime yesterday = now.AddDays(-1);

            if(date == "N")
                dt = iom.sel_io_data("", feeno, "", yesterday.ToString("yyyy/MM/dd 23:01:00"), now.ToString("yyyy/MM/dd 07:00:59"), "");
            else if(date == "D")
                dt = iom.sel_io_data("", feeno, "", now.ToString("yyyy/MM/dd 07:01:00"), now.ToString("yyyy/MM/dd 15:00:59"), "");
            else if(date == "E")
                dt = iom.sel_io_data("", feeno, "", now.ToString("yyyy/MM/dd 15:01:00"), now.ToString("yyyy/MM/dd 23:00:59"), "");
            else if(date == "all")
                dt = iom.sel_io_data("", feeno, "", now.ToString("yyyy/MM/dd 07:01:00"), now.AddDays(1).ToString("yyyy/MM/dd 07:00:59"), "");

            ViewBag.dt_io_data = set_dt(dt);
            ViewBag.date = date;
            ViewBag.day = day;
            ViewBag.userno = userno;
            ViewBag.tube_name = new Func<string, string>(sel_item_name);
            return View();
        }
        #endregion
        #endregion IO-END

        #region VitalSign查詢
        //HISView/VitalSign_Query?feeno=I0333003
        public ActionResult VitalSign_Query(string starttime, string endtime, string feeno)
        {
            try
            {
                getSession(feeno); //HISVIEW 不走 MainController,因此另外處理session
                string start = DateTime.Now.ToString("yyyy/MM/dd 00:00");
                string end = DateTime.Now.AddMinutes(2).ToString("yyyy/MM/dd HH:mm");
                string tmp_item = "", tmp_value = "";
                if (starttime != null && endtime != null)
                {
                    start = starttime;
                    end = endtime;
                }
                ViewBag.start = start;
                ViewBag.end = end;
                ViewBag.feeno = feeno.Trim();
                if(!string.IsNullOrEmpty(feeno.Trim()))
                {
                    feeno = feeno.Trim();
                    byte[] ptinfoByteCode = webService.GetPatientInfo(feeno);
                    if(ptinfoByteCode != null)
                    {//撈取ptinfo
                        string ptinfoJosnArr = NIS.UtilTool.CompressTool.DecompressString(ptinfoByteCode);
                        NIS.Data.PatientInfo pi = JsonConvert.DeserializeObject<NIS.Data.PatientInfo>(ptinfoJosnArr);

                        //宣告必須要使用到的變數
                        //IDataReader reader = null;
                        List<VitalSignDataList> vsList = new List<VitalSignDataList>();
                        List<string[]> vsId = new List<string[]>();
                        VitalSignDataList vsdl = null;

                        //取得異常查檢表
                        DataTable dt_check = Get_Check_Abnormal_dt();
                        DataTable Dt = null;

                        //取得vs_id
                        string sqlstr = " select CREATE_DATE,vs_id from H_data_vitalsign where fee_no = '" + pi.FeeNo + "' ";
                        sqlstr += "and create_date between to_date('" + start + "','yyyy/MM/dd hh24:mi:ss') and to_date('" + end + "','yyyy/MM/dd hh24:mi:ss') ";
                        sqlstr += "group by CREATE_DATE,vs_id order by CREATE_DATE";

                        Dt = link.DBExecSQL(sqlstr);
                        if (Dt.Rows.Count > 0)
                        {
                            for (int i = 0; i < Dt.Rows.Count; i++)
                                vsId.Add(new string[] { Dt.Rows[i]["vs_id"].ToString().Trim(), Dt.Rows[i]["CREATE_DATE"].ToString() });
                        }
                        // 開始處理資料
                        string color = "black";
                        for (int i = 0; i <= vsId.Count - 1; i++)
                        {
                            //初始化資料
                            vsdl = new VitalSignDataList();

                            sqlstr = " select vsd.*, to_char(CREATE_DATE,'yyyy/MM/dd hh24:mi:ss') as m_date  from H_data_vitalsign vsd ";
                            sqlstr += " where fee_no ='" + pi.FeeNo + "' and vs_id = '" + vsId[i][0] + "' ";
                            sqlstr += " and create_date = to_date('" + Convert.ToDateTime(vsId[i][1]).ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/MM/dd hh24:mi:ss')";

                            vsdl.vsid = vsId[i][0];
                            Dt = link.DBExecSQL(sqlstr);
                            if (Dt.Rows.Count > 0)
                            {
                                for (int j = 0; j < Dt.Rows.Count; j++)
                                {
                                    tmp_item = tmp_value = "";

                                    tmp_value = Dt.Rows[j]["vs_record"].ToString().Trim();
                                    tmp_item = Dt.Rows[j]["vs_item"].ToString().Trim();

                                    if (tmp_value.Contains("|"))
                                        color = "black";
                                    else
                                        color = check_abnormal_color(dt_check, tmp_item, tmp_value);

                                    vsdl.DataList.Add(new VitalSignData(
                                        Dt.Rows[j]["vs_item"].ToString().Trim(),
                                        Dt.Rows[j]["vs_part"].ToString().Trim(),
                                        Dt.Rows[j]["vs_record"].ToString().Trim(),
                                        Dt.Rows[j]["vs_reason"].ToString().Trim(),
                                        Dt.Rows[j]["vs_memo"].ToString().Trim(),
                                        Dt.Rows[j]["vs_other_memo"].ToString().Trim(),
                                        Dt.Rows[j]["CREATE_USER"].ToString().Trim(),
                                        "", "",
                                        Dt.Rows[j]["m_date"].ToString().Trim(),
                                        Dt.Rows[j]["vs_type"].ToString().Trim(),//區分TYPE類型
                                        "",
                                        color
                                     ));
                                }
                            }
                            vsList.Add(vsdl);
                            vsdl = null;
                        }
                        ViewBag.age = pi.Age;
                        ViewBag.Birthday = pi.Birthday;
                        ViewBag.ck_type = base.get_check_type(pi);
                        ViewData["VSData"] = vsList;
                        ViewBag.dt_check = dt_check;
                        return View();
                    }

                }
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(),ex);
                Response.Write("<script>alert('登入逾時');</script>");
                return new EmptyResult();
            }
            finally
            {
                this.link.DBClose();
            }

            Response.Write("<script>alert('登入逾時');</script>");
            return new EmptyResult();
        }

        
        #endregion

        #region 傷口列表(首頁)
        //HISView/listTrauma?feeno=I0333003
        public ActionResult listTrauma(string feeno)
        {
            if(feeno != null)
            {
                getSession(feeno); //HISVIEW 不走 MainController,因此另外處理session
                ViewBag.dt_wound_data = wound.sel_wound_data(feeno, "");
                ViewBag.RootDocument = GetSourceUrl();
                ViewBag.feeno = feeno;
                return View();
            }
            Response.Write("<script>alert('登入逾時');</script>");
            return new EmptyResult();
        }

        //應該沒有使用
        public ActionResult Record_List_PDF(string feeno)
        {//因和咏蓁討論後，HISVIEW的查詢鈕暫關，所以這裡暫時無使用 By wawa 2016/7/18
            PatientInfo pinfo = new PatientInfo();
            byte[] ByteCode = webService.GetPatientInfo(feeno);
            //病人資訊
            if(ByteCode != null)
                pinfo = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(ByteCode));
            ViewData["ptinfo"] = pinfo;
            DataTable dt = wound.sel_wound_record("", feeno, "", "");
            dt.Columns.Add("username");
            if(dt.Rows.Count > 0)
            {
                string userno = dt.Rows[0]["CREANO"].ToString();
                byte[] listByteCode = webService.UserName(userno);
                string listJsonArray = CompressTool.DecompressString(listByteCode);
                UserInfo user_name = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                foreach(DataRow r in dt.Rows)
                {
                    if(userno != r["CREANO"].ToString())
                    {
                        userno = r["CREANO"].ToString();
                        listByteCode = webService.UserName(userno);
                        if(listByteCode != null)
                        {
                            listJsonArray = CompressTool.DecompressString(listByteCode);
                            user_name = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                        }
                    }
                    r["username"] = user_name.EmployeesName;
                }
            }
            ViewBag.dt_wound_data = wound.sel_wound_data(feeno, "");
            ViewBag.dt_wound_record = dt;

            return View();
        }

        //轉PDF頁面//應該沒有使用
        public ActionResult Html_To_Pdf(string url)
        {
            string strPath = @"C:\wkhtmltopdf\wkhtmltopdf.exe";
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
            Response.Write("<script>window.location.href='Download_Pdf?filename=" + filename + "';</script>");
            return new EmptyResult();
        }

        //應該沒有使用
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

        //傷口列表(首頁)_傷口紀錄
        public ActionResult Partial_Record(string wound_id, string feeno)
        {
            getSession(feeno); //HISVIEW 不走 MainController,因此另外處理session

            if(!string.IsNullOrEmpty(feeno))
            {
                DataTable dt = new DataTable();
                dt = wound.sel_wound_record_data(feeno, wound_id, "");
                dt.Columns.Add("username");
                if(dt.Rows.Count > 0)
                {
                    string userno = dt.Rows[0]["CREANO"].ToString();
                    byte[] listByteCode = webService.UserName(userno);
                    string listJsonArray = CompressTool.DecompressString(listByteCode);
                    UserInfo user_name = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                    foreach(DataRow r in dt.Rows)
                    {
                        userno = r["CREANO"].ToString();
                        listByteCode = webService.UserName(userno);
                        if(listByteCode != null)
                        {
                            listJsonArray = CompressTool.DecompressString(listByteCode);
                            user_name = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                        }
                        r["username"] = user_name.EmployeesName;
                    }
                }

                ViewBag.dt_wound_record = dt;
                return PartialView();
            }
            Response.Write("<script>alert('登入逾時');</script>");
            return new EmptyResult();
        }
        #endregion

        #region 護理紀錄
        //HISView/CareRecord?feeno=I0333003
        [HttpGet]
        public ActionResult CareRecord(string feeno)
        {
            getSession(feeno); //HISVIEW 不走 MainController,因此另外處理session
             DateTime now = DateTime.Now;
            DataTable dt = care_record_m.sel_carerecord(feeno, "", "", now.ToString("yyyy/MM/dd 00:00"), now.ToString("yyyy/MM/dd HH:mm:59"), "", "", "HISVIEW");
            dt = this.care_record_m.getRecorderName(dt);
            ViewBag.dt = dt;
            ViewBag.feeno = feeno;
            ViewBag.RootDocument = GetSourceUrl();
            return View();
        }

        //HISVIEW 不走 MainController,因此另外處理session
        public void getSession(string feeno)
        {
            if (Session["Complement_List"] == null)
            {
                string jsonstr = "{\"Status\":" + "false}";
                Complement_List c_list = JsonConvert.DeserializeObject<Complement_List>(jsonstr);
                Session["Complement_List"] = c_list;
            }

            byte[] ptinfoByteCode = webService.GetPatientInfo(feeno.Trim());
            if (ptinfoByteCode != null)
            {
                string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                Session["PatInfo"] = pi;
            }
            Session["TPR"] = "HISVIEW";
        }

        //查詢_護理紀錄
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult CareRecord(FormCollection form)
        {
            string feeno = Request.Form["feeno"].ToString().Trim();
            DateTime start = Convert.ToDateTime(form["start_date"] + " " + form["start_time"]);
            DateTime end = Convert.ToDateTime(form["end_date"] + " " + form["end_time"]);
            ViewBag.start_date = start;
            ViewBag.end_date = end;
            getSession(feeno); //HISVIEW 不走 MainController,因此另外處理session

            ViewBag.feeno = feeno;
            ViewBag.RootDocument = GetSourceUrl();
            DataTable dt = care_record_m.sel_carerecord(feeno, "", "", start.ToString("yyyy/MM/dd HH:mm"), end.ToString("yyyy/MM/dd HH:mm:59"), "", "", "HISVIEW");
            dt = this.care_record_m.getRecorderName(dt);
            ViewBag.dt = dt;
            return View();
        }

        //列印
        public ActionResult CareRecord_PDF(string feeno, string starttime, string endtime)
        {
            getSession(feeno); //HISVIEW 不走 MainController,因此另外處理session
            PatientInfo pinfo = new PatientInfo();
            byte[] ByteCode = webService.GetPatientInfo(feeno);
            //病人資訊
            if(ByteCode != null)
                pinfo = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(ByteCode));
            ViewData["ptinfo"] = pinfo;
            DateTime start = Convert.ToDateTime(starttime.Replace('|', ' '));
            DateTime end = Convert.ToDateTime(endtime.Replace('|', ' '));

            ViewBag.dt = care_record_m.sel_carerecord(feeno, "", "", start.ToString("yyyy/MM/dd HH:mm"), end.ToString("yyyy/MM/dd HH:mm:59"), "", "", "HISVIEW");
            return View();
        }
        #endregion

        #region 管路管理首頁
        public ActionResult Tube(string feeno)
        {
            //管路LIST
            if(feeno != "")
            {
                ViewBag.dt_tube = tubem.sel_tube(feeno, "", "", "", "N");
                getSession(feeno); //HISVIEW 不走 MainController,因此另外處理session
            }
            return View();
        }

        //管路評估_List
        public ActionResult Assessment_List(string starttime, string endtime, string feeno)
        {
            string start = DateTime.Now.AddDays(-7).ToString("yyyy/MM/dd 00:00");
            string end = DateTime.Now.ToString("yyyy/MM/dd HH:mm");
            if(starttime != null && endtime != null)
            {
                start = starttime;
                end = endtime;
            }
            DataTable dt_Artery = new DataTable();
            DataTable dt_Tracheostomy = new DataTable();

            ViewBag.start = start;
            ViewBag.end = end;
            if(feeno != "")
            {
                getSession(feeno); //HISVIEW 不走 MainController,因此另外處理session
                ViewBag.feeno = feeno;
                ViewBag.dt_Artery = tubem.sel_tube_assess_artery(feeno, start, end, "");
                ViewBag.dt_Tracheostomy = tubem.sel_tube_assess_tracheostomy(feeno, start, end, "");
                ViewBag.dt_Other = tubem.sel_tube_assess_other(feeno, start, end, "");
            }


            return View();
        }
        #endregion

        #region TPR
        public ActionResult Tpr_Index(string feeno)
        {
            getSession(feeno); //HISVIEW 不走 MainController,因此另外處理session

            byte[] ptinfoByteCode = webService.GetPatientInfo(feeno);
            if(ptinfoByteCode != null)
            {
                string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                PatientInfo ptinfo = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);

                ViewBag.start = (DateTime.Now.AddDays(-4) < ptinfo.InDate) ? ptinfo.InDate : DateTime.Now.AddDays(-4);
                ViewBag.inday = (Convert.ToDateTime(DateTime.Now.ToString("yyyy/MM/dd")) - Convert.ToDateTime(ptinfo.InDate.ToString("yyyy/MM/dd"))).Days;
                ViewBag.feeno = feeno;
            }
            else
            {
                ViewBag.start = DateTime.Now.AddDays(-4);
                ViewBag.inday = DateTime.Now;
                ViewBag.feeno = feeno;
            }
            //Discharge time
            string discharge_time = get_discharge_time(feeno);
            int discharge_day = 0;
            if(discharge_time != "")
                discharge_day = (Convert.ToDateTime(Convert.ToDateTime(discharge_time).ToString("yyyy/MM/dd")) - Convert.ToDateTime(DateTime.Now.ToString("yyyy/MM/dd"))).Days;
            ViewBag.discharge_time = discharge_time;
            ViewBag.discharge_day = discharge_day;
            ViewBag.end = DateTime.Now.ToString("yyyy/MM/dd");
            ViewBag.RootDocument = GetSourceUrl();
            return View();
        }

        //Tpr_報表頁面
        public ActionResult Partial_Tpr(string feeno, string start, string end)
        {
            IOManager iom = new IOManager();
            DateTime Start = Convert.ToDateTime(start);
            DateTime End = Convert.ToDateTime(end);
            try
            {
                getSession(feeno); //HISVIEW 不走 MainController,因此另外處理session
                //宣告
                PatientInfo pinfo = new PatientInfo();
                List<Procedure> procedure_list = new List<Procedure>();
                DataTable dt_op_day = new DataTable();
                DataTable dt_d_day = new DataTable();
                DataTable dt_io = new DataTable();
                byte[] ptinfoByteCode = webService.GetPatientInfo(feeno);
                byte[] procedureByte = webService.GetProcedure(feeno);
                //抗生素
                byte[] tempByte = webService.GetTPRAantibiotic(feeno);
                if(tempByte != null)
                    ViewData["Aantibiotic"] = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(CompressTool.DecompressString(tempByte));
                //取得T-Bilirubin 血球容積% 20170809 add by AlanHuang
                byte[] tempByte1 = webService.GetTBilHCT(feeno);
                if (tempByte1 != null)
                    ViewData["TBilHCT"] = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(CompressTool.DecompressString(tempByte1));

                //病人資訊
                if(ptinfoByteCode != null)
                {
                    string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                    PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                    pinfo = pi;
                }
                //特殊處置
                VitalSign vs_m = new VitalSign();
                DataTable procedure = vs_m.get_event(feeno);
                if(procedureByte != null)
                {
                    string procedureJson = CompressTool.DecompressString(procedureByte);
                    List<Procedure> procedureList = JsonConvert.DeserializeObject<List<Procedure>>(procedureJson);
                    procedure_list = procedureList;
                }
                //住院日期時間取得
                string sql = "select itemvalue from assessmentdetail where tableid IN ";
                sql += "(select tableid from assessmentmaster where feeno = '" + feeno + "' and createtime = ";
                sql += "(select max(createtime) from assessmentmaster where feeno = '" + feeno + "' and deleted is null and status <> 'temporary' )) ";
                sql += "and itemid in ('param_tube_date','param_tube_time','param_assessment')";
                DataTable tmp_dt = this.link.DBExecSQL(sql);
                if (tmp_dt.Rows.Count == 3 && tmp_dt.Rows[2][0].ToString() == "入院")
                {
                    ViewBag.admission_date = Convert.ToDateTime(tmp_dt.Rows[0][0].ToString() + " " + tmp_dt.Rows[1][0].ToString());
                }
                else
                {
                    ViewBag.admission_date = DateTime.MaxValue;
                }

                List<VitalSignDataList> temp = vs_m.sel_vital(feeno, Start, Convert.ToDateTime(end + " 23:59:59"), "");
                List<VitalSignDataList> dt = new List<VitalSignDataList>();
                List<VitalSignData> temp_ = new List<VitalSignData>();
                foreach(var item in temp)
                {
                    temp_.AddRange(item.DataList);
                    if(item.DataList.Exists(x => x.vs_item == "bt" && x.vs_record != "")
                        || item.DataList.Exists(x => x.vs_item == "mp" && x.vs_record != "")
                        || item.DataList.Exists(x => x.vs_item == "bf" && x.vs_record != ""))
                    {
                        dt.Add(item);
                    }
                }
                //temp_.Sort((x, y) => { return y.create_date.CompareTo(x.create_date); });
                temp_.Sort((x, y) => { return y.create_date.CompareTo(Convert.ToDateTime(x.create_date).ToString("yyyy/MM/dd HH:mi:ss")); });
                List<int> CountList = new List<int>();
                List<String> DateList = new List<String>();
                for(DateTime s = Start; s <= End; s = s.AddDays(1))
                {
                    int cont = dt.FindAll(x => Convert.ToDateTime(x.recordtime).ToString("yyyy/MM/dd") == s.ToString("yyyy/MM/dd")).Count;
                    if(cont > 0)
                    {
                        for(int i = cont; i > 0; i = (i - 6))
                        {
                            if(i >= 6)
                                CountList.Add(6);
                            else
                                CountList.Add(i);
                            if(i == cont)
                                DateList.Add(s.ToString("yyyy/MM/dd"));
                            else
                                DateList.Add("");
                        }
                    }
                    else
                    {
                        CountList.Add(0);
                        DateList.Add(s.ToString("yyyy/MM/dd"));
                    }
                }
                ViewData["CountList"] = CountList;
                ViewData["DateList"] = DateList;
                //IO
                DataTable dt_i_item = iom.sel_sys_params_kind("iotype", "intaketype");
                DataTable dt_o_item = iom.sel_sys_params_kind("iotype", "outputtype");
                DataTable dt_io_data = iom.sel_io_data("", feeno, "", Start.ToString("yyyy/MM/dd 07:01:00"), End.AddDays(1).ToString("yyyy/MM/dd 07:00:59"), "1");
                string[] column = { "DATE", "P_VALUE", "AMOUNT", "CLORIE", "REASON", "TYPE" };
                iom.set_dt_column(dt_io, column);
                iom.set_new_list(dt_io_data, dt_i_item, dt_o_item, dt_io, Start, End);
                //血糖
                DataTable dt_bs = bai.sql_BStable(feeno, "", "del", Start.ToString("yyyy/MM/dd 00:00:00"), End.AddDays(1).ToString("yyyy/MM/dd 00:00:00"));
                //OP Day
                dt_op_day = iom.DBExecSQL(get_special_event_sql(pinfo.FeeNo, "3"));
                //Delivered at
                dt_d_day = iom.DBExecSQL(get_special_event_sql(pinfo.FeeNo, "2"));

                ViewBag.dt_bs = dt_bs;
                ViewBag.dt_io = dt_io;
                ViewBag.dt_i_item = dt_i_item;
                ViewBag.dt_o_item = dt_o_item;
                ViewBag.dt_op_day = set_special_event(dt_op_day, Start, End, "OP Day", int.MaxValue);
                ViewBag.dt_d_day = set_special_event(dt_d_day, Start, End, "D Day", 4);
                ViewData["ptinfo"] = pinfo;
                ViewData["procedure_list"] = procedure_list;
                ViewBag.procedure = procedure;
                ViewBag.start = start;
                ViewBag.end = end;
                ViewBag.feeno = feeno;
                ViewBag.io_list = new Func<string, string, string, string>(sel_io_list);
                ViewBag.dt_CVP = new Func<string, string, DataTable>(Sel_CVP);
                ViewBag.dt_ICP = new Func<string, string, DataTable>(Sel_ICP);
                ViewBag.dt_CPP = new Func<string, string, DataTable>(Sel_CPP);
                ViewBag.dt_ABP = new Func<string, string, DataTable>(Sel_ABP);
                ViewBag.dt_PCWP = new Func<string, string, DataTable>(Sel_PCWP);
                ViewBag.dt_ETCO2 = new Func<string, string, DataTable>(Sel_ETCO2);
                ViewBag.dt_GI = new Func<string, string, DataTable>(Sel_GI);
                ViewBag.dt_GI_J = new Func<string, string, DataTable>(Sel_GI_J);
                ViewBag.dt_GI_C = new Func<string, string, DataTable>(Sel_GI_C);
                ViewBag.dt_GI_U = new Func<string, string, DataTable>(Sel_GI_U);
                ViewBag.dt_SI_N = new Func<string, string, DataTable>(Sel_SI_N);
                ViewBag.dt_SI_B = new Func<string, string, DataTable>(Sel_SI_B);
                ViewBag.dt_SI_R = new Func<string, string, DataTable>(Sel_SI_R);
                ViewBag.dt_SI_S = new Func<string, string, DataTable>(Sel_SI_S);
                ViewBag.dt_SI_O = new Func<string, string, DataTable>(Sel_SI_O);
                ViewData["VitalSign"] = temp_;
                ViewData["ChartDt"] = dt;
                ViewData["CVVH"] = cvvh_data.get_cvvh_data_list(feeno, Start, End);
                return View();
            }
            catch(Exception ex)
            {
                Response.Write("與HIS串接錯誤，請與資訊室聯繫，詳細錯誤訊息如下：");
                Response.Write(ex.Message);
                return new EmptyResult();
            }
        }
        #endregion

        #region 持續疼痛評估列表(首頁) & 列印
        //HISView/listPain?feeno=I0333003
        public ActionResult listPain(string feeno)
        {
            if(webService.GetPatientInfo(feeno) != null)
            {
                getSession(feeno); //HISVIEW 不走 MainController,因此另外處理session
                ViewBag.RootDocument = GetSourceUrl();
                ViewBag.feeno = feeno;
                return View();
            }
            else
            {
                Response.Write("與HIS串接錯誤，請與資訊室聯繫，詳細錯誤訊息如下：");
                return new EmptyResult();
            }
        }

        //列印HISVIEW才有的疼痛列印
        //HISView/listPain_PDF?feeno=I0333003
        public ActionResult listPain_PDF(string feeno)
        {
            if(webService.GetPatientInfo(feeno) != null)
            {
                getSession(feeno); //HISVIEW 不走 MainController,因此另外處理session
                PatientInfo pinfo = new PatientInfo();
                byte[] ByteCode = webService.GetPatientInfo(feeno);
                //病人資訊
                if(ByteCode != null)
                    pinfo = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(ByteCode));
                ViewData["ptinfo"] = pinfo;
                ViewBag.feeno = feeno;
                return View();
            }
            else
            {
                Response.Write("與HIS串接錯誤，請與資訊室聯繫，詳細錯誤訊息如下：");
                return new EmptyResult();
            }
        }

        #endregion 持續疼痛評估列表(首頁)

        #region 約束(列表)
        //HISView/Constraints_ListNew?feeno=I0333003
        public ActionResult Constraints_ListNew(string feeno, string id = "")
        {
            if(!string.IsNullOrEmpty(feeno))
            {
                getSession(feeno); //HISVIEW 不走 MainController,因此另外處理session
                //PatientInfo pinfo = new PatientInfo();
                //byte[] ByteCode = nis.GetPatientInfo(feeno);
                ////病人資訊
                //if (ByteCode != null)
                //    pinfo = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(ByteCode));
                //ViewData["ptinfo"] = pinfo;

                DataTable dt_a1 = ca.get_table("BINDTABLE_ADD_REASON", feeno, "");
                DataView view = dt_a1.DefaultView;
                if (dt_a1.Rows.Count > 0)
                {
                    view.Sort = "BOUTDESC DESC";
                }
                DataTable dt_a = view.ToTable();
                ViewBag.dt_a = dt_a;

                if(id == "" && dt_a.Rows.Count > 0)
                {//初始取最新的一筆評估的主檔ID給下方撈職時使用
                    //id = dt_a.Rows[dt_a.Rows.Count - 1]["ID"].ToString().Trim();
                    id = dt_a.Rows[0]["ID"].ToString().Trim();
                }
                DataTable dt_x = ca.get_table("BINDTABLESAVE", feeno, id);
                DataView view_x = dt_x.DefaultView;
                if (dt_x.Rows.Count > 0)
                {
                    view_x.Sort = "ASSESSDT ASC";
                }
                DataTable dt = view_x.ToTable();

                ViewBag.RootDocument = GetSourceUrl();
                ViewBag.feeno = feeno;
                ViewBag.dt = dt;
                return View();
            }
            else
            {
                Response.Write("<script>alert('登入逾時');</script>");
                return new EmptyResult();
            }
        }
        #endregion

        #region  血糖 BloodSugar_List
        //直接將血糖 BloodSugarAndInsulin 裡的程式搬過來，主要修改只有 getfeeno  2016/07/18 by wawa
        //HISView/BloodSugar_List?feeno=I0332966    //edit by jarvis
        [HttpGet]
        public ActionResult BloodSugar_List(string feeno, string qs, string ts, string qe, string te, bool print_df = false)
        {//string success,
            if(!string.IsNullOrEmpty(feeno))
            {
                getSession(feeno); //HISVIEW 不走 MainController,因此另外處理session

                #region 下拉式選單設定
                List<SelectListItem> listitem = new List<SelectListItem>();
                listitem.Add(new SelectListItem { Text = "一般", Value = "Normal" });
                listitem.Add(new SelectListItem { Text = "S", Value = "S" });
                listitem.Add(new SelectListItem { Text = "O", Value = "O" });
                listitem.Add(new SelectListItem { Text = "I", Value = "I" });
                listitem.Add(new SelectListItem { Text = "E", Value = "E" });
                ViewData["List"] = listitem;
                List<SelectListItem> listitem2 = new List<SelectListItem>();
                listitem2.Add(new SelectListItem { Text = "拒絕", Value = "拒絕" });
                listitem2.Add(new SelectListItem { Text = "測不到", Value = "測不到" });
                ViewData["List2"] = listitem2;
                List<SelectListItem> listitem3 = new List<SelectListItem>();
                listitem3.Add(new SelectListItem { Text = "MIXTARD 30HM★1000IU/10ml 需冷藏(30%Insulin Human Regular +70%Insulin HumanIsophane(NPH))", Value = "1" });
                listitem3.Add(new SelectListItem { Text = "HUMULIN N★ 1000IU/10ml 需冷藏(NPH )", Value = "2" });
                listitem3.Add(new SelectListItem { Text = "HUMULIN R★1000IU/10ml 需冷藏 (Insulin Human Regular)", Value = "3" });
                listitem3.Add(new SelectListItem { Text = "penfill Novorapid★300IU/3ml 需冷藏(Insulin aspart)", Value = "4" });
                listitem3.Add(new SelectListItem { Text = "penfill Insulatard HM★300IU/3ml 需冷藏(NPH)", Value = "5" });
                listitem3.Add(new SelectListItem { Text = "Lantus★1000IU/10ml 需冷藏(Insulin glargine)", Value = "6" });
                listitem3.Add(new SelectListItem { Text = "penfill Novomix 30★300IU/3ml 需冷藏(30% Insulin aspart + 70% protamine Insulin aspart )", Value = "7" });
                listitem3.Add(new SelectListItem { Text = "其它", Value = "o" });
                ViewData["List3"] = listitem3;
                List<SelectListItem> listitem4 = new List<SelectListItem>();
                listitem4.Add(new SelectListItem { Text = "IV", Value = "0" });
                listitem4.Add(new SelectListItem { Text = "IVD", Value = "1" });
                listitem4.Add(new SelectListItem { Text = "SC", Value = "2" });
                ViewData["List4"] = listitem4;
                List<SelectListItem> listitem5 = new List<SelectListItem>();
                listitem5.Add(new SelectListItem { Text = "unit", Value = "0" });
                ViewData["List5"] = listitem5;
                #endregion

                #region get高低血壓項目
                ViewData["Lsymptoms"] = this.bai.getTypeItem("Lsymptoms");
                ViewData["Ldealwith"] = this.bai.getTypeItem("Ldealwith");
                ViewData["Hsymptoms"] = this.bai.getTypeItem("Hsymptoms");
                ViewData["Hdealwith"] = this.bai.getTypeItem("Hdealwith");
                #endregion

                //if(success == "yes")
                //    Response.Write("<script>alert('儲存成功!')</script>");
                //else if(success == "no")
                //    Response.Write("<script>alert('儲存失敗!')</script>");
                //else if(success == "upd")
                //    Response.Write("<script>alert('更新成功!')</script>");
                //else if(success == "ck")
                //    Response.Write("<script>alert('未勾選給藥時間!')</script>");
                //else if(success == "unselcet_pat")
                //    Response.Write("<script>alert('尚未選擇病患!')</script>");
                //else if(success == "overdue")
                //    Response.Write("<script>alert('Session過期!')</script>");

                #region LOAD
                //宣告病患_取得住院序號
                Response.Write("");
                DataTable dt = new DataTable();

                if(qs != null && qe != null && ts != null && te != null)
                {
                    dt = bai.sql_BStable(feeno, "", "del", Convert.ToDateTime(qs + " " + ts).ToString("yyyy/MM/dd HH:mm"), Convert.ToDateTime(qe + " " + te).ToString("yyyy/MM/dd HH:mm"));
                    ViewBag.start_day = qs;
                    ViewBag.start_time = ts;
                    ViewBag.end_day = qe;
                    ViewBag.end_time = te;
                }
                else
                {
                    dt = bai.sql_BStable(feeno, "", "del", "", "");
                }
                ViewBag.table = dt;
                ViewBag.text = "";
                dt.Columns.Add("SYval");
                dt.Columns.Add("DWval");
                DataTable dt_b = new DataTable();

                foreach(DataRow dr in dt.Rows)
                {
                    if(dr["BLOODSUGAR"].ToString() == "")
                    {
                        dr["BLOODSUGAR"] = " ";
                    }
                    dr["SYval"] = dr["SYMPTOM"];
                    dr["DWval"] = dr["DEALWITH"];
                    dr["SYMPTOM"] = bai.get_itemname("SYMPTOM", dr["SYMPTOM"].ToString());
                    dr["DEALWITH"] = bai.get_itemname("DEALWITH", dr["DEALWITH"].ToString());
                }
                #endregion
                byte[] ptinfoByteCode = webService.GetPatientInfo(feeno);
                if(ptinfoByteCode != null)
                {
                    string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                    PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                    ViewBag.pi = pi;
                }
                ViewBag.print_df = print_df;
                ViewBag.RootDocument = GetSourceUrl();
                ViewBag.feeno = feeno;
                return View();
            }
            Response.Write("<script>alert('登入逾時');</script>");
            return new EmptyResult();
        }

        //20131228 Edit By James 修改血糖列表儲存方式///201701110 edit by jarvis 
        [HttpPost]
        public ActionResult BloodSugar_List()
        {
            if(Request.Form["QueryStatus"].ToString() == "True")//查詢
            {
                string qs = Request.Form["start_day"].ToString().Trim();
                string ts = Request.Form["start_time"].ToString().Trim();
                string qe = Request.Form["end_day"].ToString().Trim();
                string te = Request.Form["end_time"].ToString().Trim();
                if(Request.Form["getfeeno"].ToString().Trim() != "")
                {
                    return Redirect("../HISView/BloodSugar_List?feeno=" + Request.Form["getfeeno"].ToString().Trim() + "&qs=" + qs + "&ts=" + ts + "&qe=" + qe + "&te=" + te);
                }
            }
            Response.Write("<script>alert('登入逾時');</script>");
            return new EmptyResult();

            #region  這一整段應該都和查詢無關，因為這裡是 HISView ，所以關起來  by wawa 2016/07/18
            ////20140131 修改護理紀錄內容
            //string SYMPTOM = string.Empty;
            //string DEALWITH = string.Empty;
            //SYMPTOM = Request.Form["SymptomContent"].ToString();
            //if (SYMPTOM != "")
            //    SYMPTOM = SYMPTOM.Substring(0, SYMPTOM.Length - 1);
            //DEALWITH = Request.Form["HandleContent"].ToString();
            //if (DEALWITH != "")
            //    DEALWITH = DEALWITH.Substring(0, DEALWITH.Length - 1);

            //string SymptomRecord = string.Empty;
            //string DealwithRecord = string.Empty;

            //DataTable SD = this.bai.GetBSDS();
            //string[] AllSDItem = new string[SD.Rows.Count + 1];
            //string[] AllSDValue = new string[SD.Rows.Count + 1];
            ////將症狀及處置中文說明放到陣列中
            //for (int sdc = 0; sdc < SD.Rows.Count; sdc++)
            //{
            //    AllSDItem[sdc] = SD.Rows[sdc]["ITEM"].ToString();
            //    AllSDValue[sdc] = SD.Rows[sdc]["VALUE"].ToString();
            //}

            ////如果儲存之值符合則轉換成中文說明
            //for (int sdc = 0; sdc < SD.Rows.Count; sdc++)
            //{
            //    for (int src = 0; src < SYMPTOM.Split(',').Length; src++)
            //    {
            //        if (SYMPTOM.Split(',').GetValue(src).ToString() == AllSDValue[sdc])
            //        {
            //            SymptomRecord += AllSDItem[sdc] + "，";
            //        }
            //    }
            //    for (int drc = 0; drc < DEALWITH.Split(',').Length; drc++)
            //    {
            //        if (DEALWITH.Split(',').GetValue(drc).ToString() == AllSDValue[sdc])
            //        {
            //            DealwithRecord += AllSDItem[sdc] + "，";
            //        }
            //    }
            //}
            //if (SymptomRecord != null && SymptomRecord != "")
            //    SymptomRecord = "，症狀：" + SymptomRecord.Substring(0, SymptomRecord.Length - 1) + "。";


            //if (DealwithRecord != null && DealwithRecord != "")
            //    DealwithRecord = "處置：" + DealwithRecord.Substring(0, DealwithRecord.Length - 1) + "。";


            ////20140131 修改護理紀錄內容
            //string INDATE = Request.Form["now_day"].ToString() + " " + Request.Form["now_time"].ToString();
            //string INSDT = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            //string FeeNo = ptinfo.FeeNo.ToString().Trim();
            //string BLOODSUGAR = Request.Form["value"].ToString();
            //string BSStatus = string.Empty;
            //string ReturnMessage = string.Empty;
            //string tmp = "";
            //string[] ItemIDList = Request.Form.AllKeys;

            //if (BLOODSUGAR != null && BLOODSUGAR.Trim() != "")
            //{
            //    tmp = BLOODSUGAR;
            //    if (Convert.ToInt32(BLOODSUGAR.Replace("#", "")) < 20)
            //    { BLOODSUGAR = "Low"; }
            //    else if (Convert.ToInt32(BLOODSUGAR.Replace("#", "")) > 600)
            //    { BLOODSUGAR = "High"; }
            //}



            //if (BLOODSUGAR.Trim() != "")
            //{

            //}
            //else
            //{
            //    if (Request.Form["SubmitStatus"].ToString() != "del" && Request.Form["low"] != null && (Request.Form["List2"].ToString() != "無法測量原因" && Request.Form["List2"].ToString() != ""))
            //    {
            //        if (Request.Form["low"].ToString() == "0")
            //        {
            //            BSStatus = "Low";
            //        }
            //        else if (Request.Form["low"].ToString() == "1")
            //        {
            //            BSStatus = "High";
            //        }
            //    }
            //}

            //string Note = Request.Form["note"].ToString();
            //string MONITOR = string.Empty;
            //if (Request.Form["List2"].ToString() != "無法測量原因" && Request.Form["List2"].ToString() != "")
            //{
            //    MONITOR = Request.Form["List2"].ToString();
            //}
            ////如果沒有 BSID 則新增，若有則修改
            //if (Request.Form["BSID"].ToString() == "" && Request.Form["SubmitStatus"].ToString() == "")
            //{
            //    string status = string.Empty;
            //    if (BSStatus == "")
            //    {
            //        status = "new";
            //    }
            //    else
            //    {
            //        status = BSStatus;
            //    }
            //    string BSID = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            //    List<DBItem> insertDataList = new List<DBItem>();
            //    insertDataList.Add(new DBItem("FEENO", FeeNo, DBItem.DBDataType.String));
            //    insertDataList.Add(new DBItem("INDATE", INDATE, DBItem.DBDataType.String));
            //    insertDataList.Add(new DBItem("BLOODSUGAR", tmp, DBItem.DBDataType.String));//20140724 mod by yungchen
            //    insertDataList.Add(new DBItem("NOTE", Note, DBItem.DBDataType.String));
            //    insertDataList.Add(new DBItem("SYMPTOM", SYMPTOM, DBItem.DBDataType.String));
            //    insertDataList.Add(new DBItem("DEALWITH", DEALWITH, DBItem.DBDataType.String));
            //    insertDataList.Add(new DBItem("MONITOR", MONITOR, DBItem.DBDataType.String));
            //    insertDataList.Add(new DBItem("INSDT", INSDT, DBItem.DBDataType.String));
            //    insertDataList.Add(new DBItem("INSOP", userinfo.EmployeesNo, DBItem.DBDataType.String));
            //    insertDataList.Add(new DBItem("INSOPNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
            //    insertDataList.Add(new DBItem("STATUS", status, DBItem.DBDataType.String));
            //    insertDataList.Add(new DBItem("BSID", BSID, DBItem.DBDataType.String));
            //    insertDataList.Add(new DBItem("COST_CODE", ptinfo.CostCenterNo.ToString().Trim(), DBItem.DBDataType.String));
            //    //add MEAL_STATUS 加入飯前飯後 2016/6/15
            //    string MEAL_STATUS = Request.Form["rbn_meal_status"].ToString();
            //    if (MEAL_STATUS != null && MEAL_STATUS != "")
            //        insertDataList.Add(new DBItem("MEAL_STATUS", MEAL_STATUS, DBItem.DBDataType.String));
            //    int erow = link.DBExecInsert("BLOODSUGAR", insertDataList);
            //    ReturnMessage = "資料已新增";


            //    if (Request.Form["List2"].ToString() != "無法測量原因" && Request.Form["List2"].ToString() != "")
            //    {
            //        string message = INDATE + " 病人無法測量血糖，因 " + Request.Form["List2"].ToString() + "。";
            //        Insert_CareRecord(Convert.ToDateTime(INDATE).ToString("yyyy/MM/dd HH:mm:ss"), BSID, "", "", "", message, "", "", "BloodSugar");

            //    }
            //    else
            //    {
            //        if (status == "Low" || status == "High")
            //        {
            //            //Insert_CareRecord(Convert.ToDateTime(INDATE).ToString("yyyy/MM/dd HH:mm:ss"), BSID, "", "", "", "血糖：" + status + "，症狀：" + SymptomRecord, "處置：" + DealwithRecord, "", "BloodSugar");
            //            Insert_CareRecord(Convert.ToDateTime(INDATE).ToString("yyyy/MM/dd HH:mm:ss"), BSID, "", "", "", "血糖：" + status + " " + SymptomRecord, DealwithRecord, "", "BloodSugar");
            //        }
            //        else
            //        {
            //            if (int.Parse(tmp.Replace("#", "")) < 70 || int.Parse(tmp.Replace("#", "")) > 110)
            //            {
            //                if (BLOODSUGAR != "Low" && BLOODSUGAR != "High") { BLOODSUGAR = BLOODSUGAR + " mg/dl"; }
            //                //Insert_CareRecord(Convert.ToDateTime(INDATE).ToString("yyyy/MM/dd HH:mm:ss"), BSID, "", "", "", "血糖：" + BLOODSUGAR + "，症狀：" + SymptomRecord, "處置：" + DealwithRecord, "", "BloodSugar");
            //                Insert_CareRecord(Convert.ToDateTime(INDATE).ToString("yyyy/MM/dd HH:mm:ss"), BSID, "", "", "", "血糖：" + BLOODSUGAR + " " + SymptomRecord, DealwithRecord, "", "BloodSugar");
            //            }
            //        }
            //    }

            //}
            //else
            //{
            //    //標記為刪除
            //    if (Request.Form["SubmitStatus"].ToString() == "del")
            //    {
            //        string where = " BSID = '" + Request.Form["BSID"].ToString() + "' ";
            //        List<DBItem> updList = new List<DBItem>();
            //        updList.Add(new DBItem("status", "del", DBItem.DBDataType.String));
            //        int effRow = this.link.DBExecUpdate("BLOODSUGAR", updList, where);
            //        ReturnMessage = "資料已刪除";
            //        Del_CareRecord(Request.Form["BSID"].ToString(), "BloodSugar");
            //    }
            //    else
            //    {

            //        string status = string.Empty;
            //        if (BSStatus == "")
            //        {
            //            status = "upd";
            //        }
            //        else
            //        {
            //            status = BSStatus;
            //        }
            //        string where = " BSID = '" + Request.Form["BSID"].ToString() + "' ";
            //        List<DBItem> updList = new List<DBItem>();
            //        updList.Add(new DBItem("bloodsugar", tmp, DBItem.DBDataType.String));//20140724 mod yungchen
            //        updList.Add(new DBItem("note", Note, DBItem.DBDataType.String));
            //        updList.Add(new DBItem("symptom", SYMPTOM, DBItem.DBDataType.String));
            //        updList.Add(new DBItem("dealwith", DEALWITH, DBItem.DBDataType.String));
            //        updList.Add(new DBItem("monitor", MONITOR, DBItem.DBDataType.String));
            //        updList.Add(new DBItem("moddt", INSDT, DBItem.DBDataType.String));
            //        updList.Add(new DBItem("modop", userinfo.EmployeesNo, DBItem.DBDataType.String));
            //        updList.Add(new DBItem("modname", userinfo.EmployeesName, DBItem.DBDataType.String));
            //        updList.Add(new DBItem("status", status, DBItem.DBDataType.String));
            //        //add MEAL_STATUS 加入飯前飯後 2016/6/15
            //        string MEAL_STATUS = Request.Form["rbn_meal_status"].ToString();
            //        if (MEAL_STATUS != null && MEAL_STATUS != "")
            //            updList.Add(new DBItem("MEAL_STATUS", MEAL_STATUS, DBItem.DBDataType.String));

            //        int effRow = this.link.DBExecUpdate("BLOODSUGAR", updList, where);
            //        ReturnMessage = "資料已更新";
            //        if (Request.Form["List2"].ToString() != "無法測量原因" && Request.Form["List2"].ToString() != "")
            //        {
            //            string message = INDATE + " 病人無法測量血糖，因 " + Request.Form["List2"].ToString() + "。";
            //            if (Upd_CareRecord(Convert.ToDateTime(INDATE).ToString("yyyy/MM/dd HH:mm:ss"), Request.Form["BSID"].ToString(), "", "", "", message, "", "", "BloodSugar") == 0)
            //                Insert_CareRecord(Convert.ToDateTime(INDATE).ToString("yyyy/MM/dd HH:mm:ss"), Request.Form["BSID"].ToString(), "", "", "", message, "", "", "BloodSugar");

            //        }
            //        else
            //        {
            //            if (status == "Low" || status == "High")
            //            {

            //                if (Upd_CareRecord(Convert.ToDateTime(INDATE).ToString("yyyy/MM/dd HH:mm:ss"), Request.Form["BSID"].ToString(), "", "", "", "血糖：" + status + " " + SymptomRecord, "" + DealwithRecord, "", "BloodSugar") == 0)
            //                    Insert_CareRecord(Convert.ToDateTime(INDATE).ToString("yyyy/MM/dd HH:mm:ss"), Request.Form["BSID"].ToString(), "", "", "", "血糖：" + status + " " + SymptomRecord, DealwithRecord, "", "BloodSugar");
            //                //Insert_CareRecord(Convert.ToDateTime(INDATE).ToString("yyyy/MM/dd HH:mm:ss"), Request.Form["BSID"].ToString(), "", "", "", "血糖：" + status + "，症狀：" + SymptomRecord, "處置：" + DealwithRecord, "", "BloodSugar");

            //            }
            //            else
            //            {
            //                if (BLOODSUGAR != "Low" && BLOODSUGAR != "High") { BLOODSUGAR = BLOODSUGAR + " mg/dl"; }
            //                if (Upd_CareRecord(Convert.ToDateTime(INDATE).ToString("yyyy/MM/dd HH:mm:ss"), Request.Form["BSID"].ToString(), "", "", "", "血糖：" + BLOODSUGAR + " " + SymptomRecord, "" + DealwithRecord, "", "BloodSugar") == 0)
            //                    Insert_CareRecord(Convert.ToDateTime(INDATE).ToString("yyyy/MM/dd HH:mm:ss"), Request.Form["BSID"].ToString(), "", "", "", "血糖：" + BLOODSUGAR + " " + SymptomRecord, DealwithRecord, "", "BloodSugar");
            //                //Insert_CareRecord(Convert.ToDateTime(INDATE).ToString("yyyy/MM/dd HH:mm:ss"), Request.Form["BSID"].ToString(), "", "", "", "血糖：" + BLOODSUGAR + "，症狀：" + SymptomRecord, "處置：" + DealwithRecord, "", "BloodSugar");

            //            }
            //        }

            //    }
            //}

            //return RedirectToAction("BloodSugar_List", new { @message = "" + ReturnMessage + "" });

            #endregion  這一整段應該都和查詢無關，因為這裡是 HISView ，所以關起來  end
        }

        #endregion   血糖 end

        #region 血糖胰島素清單 BSugarInsulin_List
        public ActionResult BSugarInsulinList(FormCollection form, string feeno)
        {
            //宣告病患_取得住院序號
            ViewBag.feeno = feeno;
            bool lb_use = false;
            byte[] ptinfoByteCode = webService.GetPatientInfo(feeno);
            if(ptinfoByteCode != null)
            {
                getSession(feeno); //HISVIEW 不走 MainController,因此另外處理session
                string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                ViewBag.ChartNo = pi.ChartNo;
                ViewBag.FeeNo = pi.FeeNo;
                ViewBag.PatientName = pi.PatientName;
                ViewBag.BedNo = pi.BedNo;
                ViewBag.Age = pi.Age;
                ViewBag.PatientGender = pi.PatientGender;
            }
            else
            { ViewBag.ChartNo = null; }

            string start = "", end = "";
            if(form["start_date"] == "" || form["start_date"] == null)
            {
                start = DateTime.Now.AddDays(-1).ToString("yyyy/MM/dd") + " 00:00:00";
                end = DateTime.Now.ToString("yyyy/MM/dd") + " 23:59:59";
            }
            else
            {
                start = Convert.ToDateTime(form["start_date"] + " " + form["start_time"]).ToString("yyyy/MM/dd HH:mm:ss");    //查詢_開始日期時間
                end = Convert.ToDateTime(form["end_date"] + " " + form["end_time"]).ToString("yyyy/MM/dd HH:mm:ss");          //查詢_結束日期時間
                ViewBag.start_date = Convert.ToDateTime(form["start_date"]).ToString("yyyy/MM/dd");
                ViewBag.start_time = Convert.ToDateTime(form["start_time"]).ToString("HH:mm");
                ViewBag.end_date = Convert.ToDateTime(form["end_date"]).ToString("yyyy/MM/dd");
                ViewBag.end_time = Convert.ToDateTime(form["end_time"]).ToString("HH:mm");
            }
            DataTable dt_BSugar = bai.get_BSugarInsulin_list(feeno, "B", start, end);
            DataTable dt_Insulin = bai.get_BSugarInsulin_list(feeno, "I", start, end);
            List<BloodSugarInsulin_List> BloodSugar_List = new List<BloodSugarInsulin_List>();
            List<BloodSugarInsulin_List> Insulin_List = new List<BloodSugarInsulin_List>();

            BloodSugar_List = (from a in dt_BSugar.AsEnumerable()
                               select new BloodSugarInsulin_List()
                               {
                                   BLOODSUGAR = a.Field<string>("BLOODSUGAR"),
                                   INSOPNAME = a.Field<string>("INSOPNAME"),
                                   B_INDATE = a.Field<string>("B_INDATE"),
                                   MEAL_STATUS = a.Field<string>("MEAL_STATUS"),
                                   CHECK_FLAG = "N"
                               }).ToList<BloodSugarInsulin_List>();

            Insulin_List = (from a in dt_Insulin.AsEnumerable()
                            select new BloodSugarInsulin_List()
                            {
                                I_INDATE = a.Field<string>("I_INDATE"),
                                IN_DRUGNAME = a.Field<string>("IN_DRUGNAME"),
                                IN_DOSE = a.Field<decimal>("IN_DOSE").ToString("0.#"),
                                IN_DOSEUNIT = a.Field<string>("IN_DOSEUNIT"),
                                POSITION = a.Field<string>("POSITION"),
                                INJECTION = a.Field<string>("INJECTION"),
                                SS_DRUGNAME = a.Field<string>("SS_DRUGNAME"),
                                SS_DOSE = a.Field<decimal>("SS_DOSE").ToString("#.#"),
                                CHECK_FLAG = "N"
                            }).ToList<BloodSugarInsulin_List>();
            List<BloodSugarInsulin_List> BSugarInsulin_List = new List<BloodSugarInsulin_List>();
            int tmpcount = 0;
            for(int i = 0; i <= BloodSugar_List.Count - 1; i++)
            {
                if(BloodSugar_List[i].BLOODSUGAR.ToString().IndexOf("#") == -1)
                    tmpcount++;

               DateTime Date_B = new DateTime();
                //DateTime.TryParse(BloodSugar_List[i].B_INDATE, out Date_B);
                Date_B = Convert.ToDateTime(BloodSugar_List[i].B_INDATE.ToString());

                DateTime Date_B2 = Date_B;
                if (i + 1 < BloodSugar_List.Count)
                //DateTime.TryParse(BloodSugar_List[i+1].B_INDATE, out Date_B2);
                Date_B2 = Convert.ToDateTime(BloodSugar_List[i + 1].B_INDATE.ToString());

                for (int j = 0; j <= Insulin_List.Count - 1; j++)
                {
                    DateTime Date_I = new DateTime();
                    //DateTime.TryParse(BloodSugar_List[j].B_INDATE, out Date_I);
                    Date_I = Convert.ToDateTime(Insulin_List[j].I_INDATE.ToString());
                    if (i == 61)
                    {
                        int a = DateTime.Compare(Date_B, Date_I);
                    }
                    if(DateTime.Compare(Date_B, Date_I) <= 0 && DateTime.Compare(Date_B2, Date_I) == 1 && Insulin_List[j].CHECK_FLAG.ToString() != "Y" ||
                        (i == 0 && DateTime.Compare(Date_B, Date_I) >= 0) || (i == BloodSugar_List.Count - 1 && DateTime.Compare(Date_B, Date_I) == -1))
                    {
                        BSugarInsulin_List.Insert(0, new BloodSugarInsulin_List()
                        {
                            BLOODSUGAR = BloodSugar_List[i].BLOODSUGAR.ToString(),
                            INSOPNAME = BloodSugar_List[i].INSOPNAME.ToString(),
                            B_INDATE = BloodSugar_List[i].B_INDATE.ToString(),
                            I_INDATE = Insulin_List[j].I_INDATE.ToString(),
                            IN_DRUGNAME = Insulin_List[j].IN_DRUGNAME.ToString(),
                            IN_DOSE = Insulin_List[j].IN_DOSE.ToString(),
                            IN_DOSEUNIT = Insulin_List[j].IN_DOSEUNIT,
                            POSITION = Insulin_List[j].POSITION,
                            INJECTION = Insulin_List[j].INJECTION.ToString(),
                            SS_DRUGNAME = Insulin_List[j].SS_DRUGNAME.ToString(),
                            SS_DOSE = Insulin_List[j].SS_DOSE.ToString(),
                            MEAL_STATUS = BloodSugar_List[i].MEAL_STATUS.ToString()
                        });
                        Insulin_List[j].CHECK_FLAG = "Y";
                        BloodSugar_List[i].CHECK_FLAG = "Y";
                        lb_use = true;
                    }
                }
                if(lb_use == false || BloodSugar_List[i].CHECK_FLAG == "N")
                {
                    BSugarInsulin_List.Insert(0, new BloodSugarInsulin_List()
                    {
                        BLOODSUGAR = BloodSugar_List[i].BLOODSUGAR.ToString(),
                        INSOPNAME = BloodSugar_List[i].INSOPNAME.ToString(),
                        B_INDATE = BloodSugar_List[i].B_INDATE.ToString(),
                        MEAL_STATUS = BloodSugar_List[i].MEAL_STATUS,
                        I_INDATE = "",
                        IN_DRUGNAME = "",
                        IN_DOSE = "",
                        IN_DOSEUNIT = "",
                        POSITION = "",
                        INJECTION = "",
                        SS_DRUGNAME = "",
                        SS_DOSE = "",
                    });
                    BloodSugar_List[i].CHECK_FLAG = "Y";
                }
            }

            for(int j = Insulin_List.Count - 1; j >= 0; j--)
            {
                if(Insulin_List[j].CHECK_FLAG.ToString() != "Y")
                {
                    BSugarInsulin_List.Insert(0, new BloodSugarInsulin_List()
                    {
                        BLOODSUGAR = "",
                        INSOPNAME = "",
                        B_INDATE = "",
                        I_INDATE = Insulin_List[j].I_INDATE,
                        IN_DRUGNAME = Insulin_List[j].IN_DRUGNAME,
                        IN_DOSE = Insulin_List[j].IN_DOSE,
                        IN_DOSEUNIT = Insulin_List[j].IN_DOSEUNIT,
                        POSITION = Insulin_List[j].POSITION,
                        INJECTION = Insulin_List[j].INJECTION,
                        SS_DRUGNAME = Insulin_List[j].SS_DRUGNAME,
                        SS_DOSE = Insulin_List[j].SS_DOSE
                    });
                    Insulin_List[j].CHECK_FLAG = "Y";
                }
            }
            for(int i = BloodSugar_List.Count - 1; i >= 0; i--)
            {
                if(BloodSugar_List[i].CHECK_FLAG.ToString() != "Y")
                {
                    BSugarInsulin_List.Insert(0, new BloodSugarInsulin_List()
                    {
                        BLOODSUGAR = BloodSugar_List[i].BLOODSUGAR.ToString(),
                        INSOPNAME = BloodSugar_List[i].INSOPNAME.ToString(),
                        B_INDATE = BloodSugar_List[i].B_INDATE.ToString(),
                        MEAL_STATUS = BloodSugar_List[i].MEAL_STATUS,
                        I_INDATE = "",
                        IN_DRUGNAME = "",
                        IN_DOSE = "",
                        IN_DOSEUNIT = "",
                        POSITION = "",
                        INJECTION = "",
                        SS_DRUGNAME = "",
                        SS_DOSE = "",
                    });
                }
            }
            if(BSugarInsulin_List.Count == 0)
                ViewData["BSugarInsulin_List"] = null;
            else
                ViewData["BSugarInsulin_List"] = BSugarInsulin_List;

            var before = 0;
            var after = 0;
            var understand = 0;

            for(int i = 0; i < BloodSugar_List.Count; i++)
            {
                var meal = BloodSugar_List[i].MEAL_STATUS;

            switch(meal)
                {
                    case "B":
                        before = before + 1;
                        break;
                    case "A":
                        after = after + 1;
                        break;
                    case "C":
                        understand = understand + 1;
                        break;
                }
            }
            ViewBag.b_count = "血糖監測次數：" + BloodSugar_List.Count + "(含手動輸入：" + tmpcount + "，飯前:" + before.ToString() + " 飯後:" + after.ToString() + " 不清楚:" + understand.ToString() + ")，";
            ViewBag.i_count = "胰島素注射次數：" + Insulin_List.Count;
            return View();
        }

        public ActionResult Bloodsugar_report(FormCollection form, string ExecFlag)
        {
            #region 前置處理
            try
            {
                //查詢時間預設
                string start = "", end = "";
                if(ExecFlag == "" || ExecFlag == null)
                {
                    start = DateTime.Now.AddDays(-1).ToString("yyyy/MM/dd") + "00:00";
                    end = DateTime.Now.AddDays(-1).ToString("yyyy/MM/dd") + "23:59";
                }
                else
                {
                    start = Convert.ToDateTime(form["start_date"] + " " + form["start_time"]).ToString("yyyy/MM/dd HH:mm");    //查詢_開始日期時間
                    end = Convert.ToDateTime(form["end_date"] + " " + form["end_time"]).ToString("yyyy/MM/dd HH:mm");          //查詢_結束日期時間
                    ViewBag.start_date = Convert.ToDateTime(form["start_date"]).ToString("yyyy/MM/dd");
                    ViewBag.start_time = Convert.ToDateTime(form["start_time"]).ToString("HH:mm");
                    ViewBag.end_date = Convert.ToDateTime(form["end_date"]).ToString("yyyy/MM/dd");
                    ViewBag.end_time = Convert.ToDateTime(form["end_time"]).ToString("HH:mm");
                }
                //護理站列表
                byte[] listByteCode = webService.GetCostCenterList();
                string listJsonArray = CompressTool.DecompressString(listByteCode);
                List<CostCenterList> costlist = JsonConvert.DeserializeObject<List<CostCenterList>>(listJsonArray);
                List<SelectListItem> cCostList = new List<SelectListItem>();
                for(int i = 0; i <= costlist.Count - 1; i++)
                {
                    if(Request["selStation"] == costlist[i].CostCenterCode)
                    {
                        cCostList.Add(new SelectListItem()
                        {
                            Text = costlist[i].CCCDescription,
                            Value = costlist[i].CostCenterCode,
                            Selected = true
                        });
                    }
                    else
                    {
                        cCostList.Add(new SelectListItem()
                        {
                            Text = costlist[i].CCCDescription,
                            Value = costlist[i].CostCenterCode,
                            Selected = false
                        });
                    }
                }
                ViewData["costlist"] = cCostList;
                #endregion
                if(ExecFlag != null && ExecFlag != "")
                {
                    List<List<string>> bloods_list = new List<List<string>>();
                    string cost_code = form["cost_code"];
                    string strsql = "SELECT A.COST_CODE,A.BED_NO,A.CHR_NO,A.PAT_NAME,(SELECT COUNT(*) FROM BLOODSUGAR B ";
                    strsql += " WHERE B.FEENO=TRIM(A.FEE_NO) AND STATUS<>'del' AND TO_DATE(INDATE,'YYYY/MM/DD HH24:MI') BETWEEN ";
                    strsql += " TO_DATE('" + start + "','YYYY/MM/DD HH24:MI') AND TO_DATE('" + end + "','YYYY/MM/DD HH24:MI')) COUNT";
                    strsql += " FROM IPD_BASIC A WHERE IPD_FLAG='Y' AND COST_CODE = '" + ExecFlag + "' ORDER BY A.BED_NO";
                    DataTable Dt = link.DBExecSQL(strsql);
                    if (Dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            bloods_list.Add(new List<string>{
                                Dt.Rows[i]["COST_CODE"].ToString().Trim(),
                                Dt.Rows[i]["BED_NO"].ToString().Trim(),
                                Dt.Rows[i]["CHR_NO"].ToString().Trim(),
                                Dt.Rows[i]["PAT_NAME"].ToString().Trim(),
                                Dt.Rows[i]["COUNT"].ToString().Trim()
                            });
                        }
                    }
                    ViewBag.bloods_list = bloods_list;
                }
                else
                { ViewBag.bloods_list = null; }
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(),ex);
            }
            return View();
        }
        #endregion

        #region 點滴報表
        public ActionResult Drug_iv_report(FormCollection form, string feeno, string ExecFlag)
        {
            try
            {
                getSession(feeno); //HISVIEW 不走 MainController,因此另外處理session
                #region 前置處理
                //護理站列表
                byte[] listByteCode = webService.GetCostCenterList();
                string listJsonArray = CompressTool.DecompressString(listByteCode);
                List<CostCenterList> costlist = JsonConvert.DeserializeObject<List<CostCenterList>>(listJsonArray);
                List<SelectListItem> cCostList = new List<SelectListItem>();
                for(int i = 0; i <= costlist.Count - 1; i++)
                {
                    if(Request["selStation"] == costlist[i].CostCenterCode)
                    {
                        cCostList.Add(new SelectListItem()
                        {
                            Text = costlist[i].CCCDescription,
                            Value = costlist[i].CostCenterCode,
                            Selected = true
                        });
                    }
                    else
                    {
                        cCostList.Add(new SelectListItem()
                        {
                            Text = costlist[i].CCCDescription,
                            Value = costlist[i].CostCenterCode,
                            Selected = false
                        });
                    }
                }
                if(Request["selStation"] == "ALL")
                {
                    cCostList.Add(new SelectListItem()
                    {
                        Text = "全院",
                        Value = "ALL",
                        Selected = true
                    });
                }
                else
                {
                    cCostList.Add(new SelectListItem()
                    {
                        Text = "全院",
                        Value = "ALL",
                        Selected = false
                    });
                }

                ViewData["costlist"] = cCostList;
                ViewBag.feeno = feeno;
                #endregion
                ViewData["FeeNo"] = feeno;
                if(ExecFlag == "" || ExecFlag == null)
                    return View();
                var ExecType = Request["ExecType"].ToString();
                #region 病人資料
                if(ExecType == "people")
                {
                    byte[] ptinfoByteCode = webService.GetPatientInfo(feeno);
                    if(ptinfoByteCode != null)
                    {
                        string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                        PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                        ViewData["PatInfo"] = "病歷號：" + pi.ChartNo + " 姓名：" + pi.PatientName + " 床號：" + pi.BedNo + " 批價序號：" + feeno;
                        ViewData["ChrNo"] = pi.ChartNo;
                    }
                }
                else
                    ViewData["PatInfo"] = null;
                #endregion

                //查詢時間預設
                string start = Convert.ToDateTime(form["start_date"] + " 00:00").ToString("yyyy/MM/dd HH:mm");    //查詢_開始日期時間
                string end = Convert.ToDateTime(form["end_date"] + " 23:59").ToString("yyyy/MM/dd HH:mm");          //查詢_結束日期時間
                ViewBag.start_date = Convert.ToDateTime(form["start_date"]).ToString("yyyy/MM/dd");
                ViewBag.end_date = Convert.ToDateTime(form["end_date"]).ToString("yyyy/MM/dd");
                
                string sql = "";

                if(ExecFlag == "day")
                    sql = get_iv_report_1(Request["selStation"].ToString().Trim(), start, end, Request["feeno"].ToString(), ExecType);
                else if(ExecFlag == "mon")
                    sql = get_iv_report_2(Request["selStation"].ToString().Trim(), Request["startDate"].ToString().Trim(), Request["feeno"].ToString(), ExecType);
                else if(ExecFlag == "detail")
                    sql = get_iv_report_3(Request["selStation"].ToString().Trim(), start, end, Request["feeno"].ToString(), ExecType);
                DataTable Dt = link.DBExecSQL(sql);

                List<List<string>> iv_list = new List<List<string>>();
                if(ExecFlag == "detail")
                {
                    List<List<string>> ivdetail_list = new List<List<string>>();
                    if (Dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            ivdetail_list.Add(new List<string>{
                                Dt.Rows[i]["COST_CODE"].ToString().Trim(),
                                Dt.Rows[i]["DRUG_DATE"].ToString().Trim(),
                                Dt.Rows[i]["FEE_NO"].ToString().Trim(),
                                Dt.Rows[i]["MED_CODE"].ToString().Trim(),
                                Dt.Rows[i]["USE_DOSE"].ToString().Trim(),
                                Dt.Rows[i]["USE_UNIT"].ToString().Trim(),
                                Dt.Rows[i]["CHR_NO"].ToString().Trim(),
                                Dt.Rows[i]["BED_NO"].ToString().Trim()
                                //reader["COST_CODE"].ToString().Trim(),
                                //reader["DRUG_DATE"].ToString().Trim(),
                                //reader["FEE_NO"].ToString().Trim(),
                                //reader["MED_CODE"].ToString().Trim(),
                                //reader["USE_DOSE"].ToString().Trim(),
                                //reader["USE_UNIT"].ToString().Trim(),
                                //reader["CHR_NO"].ToString().Trim(),
                                //reader["BED_NO"].ToString().Trim()
                            });
                        }
                    }
                    ViewBag.ivdetail_list = ivdetail_list;
                    sql = get_iv_report_4(Request["selStation"].ToString().Trim(), start, end, Request["feeno"].ToString(), ExecType);
                    
                    Dt = link.DBExecSQL(sql);
                    decimal iv_total = 0;
                    if (Dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            iv_total = decimal.Parse(Dt.Rows[i]["USE_DOSE"].ToString().Trim()) * decimal.Parse(Dt.Rows[i]["BOT"].ToString().Trim());
                            iv_list.Add(new List<string>{
                            Dt.Rows[i]["COST_CODE"].ToString().Trim(),
                            Dt.Rows[i]["USE_DOSE"].ToString().Trim(),
                            Dt.Rows[i]["MED_CODE"].ToString().Trim(),
                            Dt.Rows[i]["BOT"].ToString().Trim(),
                            iv_total.ToString("0.#")
                            //iv_total = decimal.Parse(reader["USE_DOSE"].ToString().Trim()) * decimal.Parse(reader["BOT"].ToString().Trim());
                            //iv_list.Add(new List<string>{
                            //reader["COST_CODE"].ToString().Trim(),
                            //reader["USE_DOSE"].ToString().Trim(),
                            //reader["MED_CODE"].ToString().Trim(),
                            //reader["BOT"].ToString().Trim(),
                            //iv_total.ToString("0.#")
                        });
                        }
                    }
                }
                else
                {
                    if (Dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            iv_list.Add(new List<string>{
                                Dt.Rows[i]["COST_CODE"].ToString().Trim(),
                                Dt.Rows[i]["DRUG_DATE"].ToString().Trim(),
                                Dt.Rows[i]["MED_CODE"].ToString().Trim(),
                                Dt.Rows[i]["BOT"].ToString().Trim()
                                //reader["COST_CODE"].ToString().Trim(),
                                //reader["DRUG_DATE"].ToString().Trim(),
                                //reader["MED_CODE"].ToString().Trim(),
                                //reader["BOT"].ToString().Trim()
                            });
                        }
                    }
                }
                if(iv_list.Count == 0)
                {
                    ViewBag.iv_list = null;
                    ViewData["Message"] = "無使用點滴紀錄";
                }
                else
                    ViewBag.iv_list = iv_list;
                ViewData["ExecFlag"] = ExecFlag;
                ViewData["ExecType"] = ExecType;
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(),ex);
            }
            finally
            {
                this.link.DBClose();
            }

            return View();
        }
        private string get_iv_report_1(string cost_code, string start, string end, string feeno, string ExecType)
        {   //依日期
            string sql = "SELECT COST_CODE,TO_CHAR(DRUG_DATE,'YYYY/MM/DD') DRUG_DATE,MED_CODE,SUM(ROUND(USE_DOSE)) BOT";
            sql += " FROM DRUG_IV WHERE ";
            if(cost_code != "ALL")
                sql += " COST_CODE ='" + cost_code + "' AND ";
            if(ExecType == "people")
                sql += " fee_no = '" + feeno + "' AND ";
            sql += " DRUG_DATE BETWEEN TO_DATE('" + start + "','YYYY/MM/DD HH24:MI') AND TO_DATE('" + end + "','YYYY/MM/DD HH24:MI')";
            sql += " GROUP BY COST_CODE,TO_CHAR(DRUG_DATE,'YYYY/MM/DD'),MED_CODE ORDER BY COST_CODE,DRUG_DATE";
            return sql;
        }
        private string get_iv_report_2(string cost_code, string start, string feeno, string ExecType)
        {   //依月份
            string sql = "SELECT COST_CODE,TO_CHAR(DRUG_DATE,'YYYY/MM') DRUG_DATE,MED_CODE,SUM(ROUND(USE_DOSE)) BOT";
            sql += " FROM DRUG_IV WHERE ";
            if(cost_code != "ALL")
                sql += " COST_CODE ='" + cost_code + "' AND ";
            if(ExecType == "people")
                sql += " fee_no = '" + feeno + "' AND ";
            sql += " TO_CHAR(DRUG_DATE,'YYYY/MM') = '" + start + "'";
            sql += " GROUP BY COST_CODE,TO_CHAR(DRUG_DATE,'YYYY/MM'),MED_CODE ORDER BY COST_CODE,MED_CODE";
            return sql;
        }
        private string get_iv_report_3(string cost_code, string start, string end, string feeno, string ExecType)
        {   //明細
            string sql = "SELECT COST_CODE,FEE_NO,MED_CODE,TO_NUMBER(USE_DOSE) USE_DOSE,USE_UNIT,TO_CHAR(DRUG_DATE,'YYYY/MM/DD HH24:MI') DRUG_DATE";
            sql += " ,CHR_NO,BED_NO FROM DRUG_IV WHERE ";
            if(cost_code != "ALL")
                sql += " COST_CODE ='" + cost_code + "' AND ";
            if(ExecType == "people")
                sql += " fee_no = '" + feeno + "' AND ";
            sql += " DRUG_DATE BETWEEN TO_DATE('" + start + "','YYYY/MM/DD HH24:MI') AND TO_DATE('" + end + "','YYYY/MM/DD HH24:MI') ORDER BY DRUG_DATE";
            return sql;
        }
        private string get_iv_report_4(string cost_code, string start, string end, string feeno, string ExecType)
        {   //合計
            string sql = "SELECT COST_CODE,MED_CODE,TO_NUMBER(USE_DOSE) USE_DOSE,COUNT(*) BOT";
            sql += " FROM DRUG_IV WHERE ";
            if(cost_code != "ALL")
                sql += " COST_CODE ='" + cost_code + "' AND ";
            if(ExecType == "people")
                sql += " fee_no = '" + feeno + "' AND ";
            sql += " DRUG_DATE BETWEEN TO_DATE('" + start + "','YYYY/MM/DD HH24:MI') AND TO_DATE('" + end + "','YYYY/MM/DD HH24:MI')";
            sql += " GROUP BY COST_CODE,MED_CODE,USE_DOSE ORDER BY COST_CODE,MED_CODE,USE_DOSE";
            return sql;
        }
        #endregion

        #region 衛教
        public ActionResult Education_List(string feeno)
        {
            if(feeno != null && feeno.Trim() != "")
            {
                getSession(feeno); //HISVIEW 不走 MainController,因此另外處理session
                Education edu_m = new Education();
                ViewBag.dt = edu_m.sel_health_education(feeno, "", "");
            }
            return View();
        }
        #endregion

        #region other_function

        private DataTable set_dt(DataTable dt)
        {
            if(dt.Rows.Count > 0)
            {
                dt.Columns.Add("username");

                string userno = dt.Rows[0]["CREANO"].ToString();
                byte[] listByteCode = webService.UserName(userno);
                string listJsonArray = CompressTool.DecompressString(listByteCode);
                UserInfo user_name = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                foreach(DataRow r in dt.Rows)
                {
                    if(userno != r["CREANO"].ToString())
                    {
                        userno = r["CREANO"].ToString();
                        listByteCode = webService.UserName(userno);
                        listJsonArray = CompressTool.DecompressString(listByteCode);
                        user_name = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                    }
                    r["username"] = user_name.EmployeesName;
                }
            }
            return dt;
        }

        private string sel_item_name(string item_id)
        {
            DataTable dt = tubem.sel_tube("", "", item_id, "", "N");
            string content = "";
            if(dt.Rows.Count > 0)
            {
                content = dt.Rows[0]["TYPE_NAME"].ToString() + dt.Rows[0]["POSITION"].ToString() + dt.Rows[0]["LOCATION_NAME"].ToString();
                if(dt.Rows[0]["NUMBERID"].ToString() != "99")
                    content += "#" + dt.Rows[0]["NUBER_NAME"].ToString();
                else
                    content += "#" + dt.Rows[0]["NUMBEROTHER"].ToString();
            }

            return content;
        }

        private string get_sql_by_interval(string feeno, DateTime starttime, DateTime endtime)
        {
            string sql = "SELECT DATA.VS_ITEM ";
            for(DateTime start = starttime; start <= endtime; start = start.AddDays(1))
            {
                string[] time_list = { start.ToString("yyyy/MM/dd 09:00:00"), start.ToString("yyyy/MM/dd 13:00:00"), start.ToString("yyyy/MM/dd 18:00:00"), start.ToString("yyyy/MM/dd 21:00:00") };
                for(int i = 0; i < 4; i++)
                {
                    sql += ", (SELECT VS_RECORD FROM H_DATA_VITALSIGN WHERE VS_ITEM = DATA.VS_ITEM AND VS_RECORD IS NOT NULL ";
                    sql += "AND FEE_NO = '" + feeno + "' AND CREATE_DATE BETWEEN ";
                    sql += "to_date('" + Convert.ToDateTime(time_list[i]).AddHours(-1).ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/mm/dd hh24:mi:ss') ";
                    sql += "AND to_date('" + Convert.ToDateTime(time_list[i]).AddHours(1).ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/mm/dd hh24:mi:ss') ";
                    sql += "AND rownum <=1 )AS \"" + time_list[i] + "\" ";
                }
            }
            sql += "FROM (SELECT DISTINCT(VS_ITEM) FROM H_DATA_VITALSIGN)DATA ";
            return sql;
        }

        private string get_sql(string feeno, DateTime starttime, DateTime endtime)
        {
            string sql = "SELECT DATA.VS_ITEM ";
            for(DateTime start = starttime; start <= endtime; start = start.AddDays(1))
            {
                sql += ", (SELECT VS_RECORD || ',' || VS_PART FROM H_DATA_VITALSIGN WHERE VS_ITEM = DATA.VS_ITEM AND VS_RECORD IS NOT NULL ";
                sql += "AND FEE_NO = '" + feeno + "' AND CREATE_DATE BETWEEN ";
                sql += "to_date('" + start.AddDays(-1).ToString("yyyy/MM/dd 23:59:59") + "','yyyy/mm/dd hh24:mi:ss') ";
                sql += "AND to_date('" + start.AddDays(1).ToString("yyyy/MM/dd 00:00:00") + "','yyyy/mm/dd hh24:mi:ss') ";
                sql += "AND rownum <=1 )AS \"" + start.ToString("yyyy/MM/dd") + "\" ";
            }
            sql += "FROM (SELECT DISTINCT(VS_ITEM) FROM H_DATA_VITALSIGN)DATA ";
            return sql;
        }

        private string get_special_event_sql(string feeno, string type)
        {
            string sql = "SELECT DISTINCT(to_char(CREATTIME,'yyyy/mm/dd'))CREATTIME FROM NIS_SPECIAL_EVENT_DATA WHERE 0 = 0 ";
            if(feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if(type != "")
                sql += "AND TYPE_ID = '" + type + "' ";
            sql += "ORDER BY CREATTIME ";

            return sql;
        }

        ////取得CVP
        //private DataTable Sel_CVP(string feeno, string date)
        //{
        //    DataTable dt = new DataTable();
        //    string sql = "SELECT * FROM (SELECT create_date, VS_ITEM, VS_RECORD FROM DATA_VITALSIGN WHERE VS_RECORD IS NOT NULL ";
        //    sql += "AND FEE_NO = '" + feeno + "' AND CREATE_DATE BETWEEN ";
        //    sql += "to_date('" + Convert.ToDateTime(date).AddDays(-1).ToString("yyyy/MM/dd 23:59:59") + "','yyyy/mm/dd hh24:mi:ss') ";
        //    sql += "AND to_date('" + Convert.ToDateTime(date).AddDays(1).ToString("yyyy/MM/dd 00:00:00") + "','yyyy/mm/dd hh24:mi:ss') ";
        //    sql += "AND (VS_ITEM = 'cv1' OR VS_ITEM = 'cv2') ORDER BY CREATE_DATE) WHERE rownum <= 3 ";

        //    link.DBExecSQL(sql, ref dt);
        //    return dt;
        //}

        private DataTable set_special_event(DataTable dt_temp, DateTime starttime, DateTime endtime, string Replace_Word, int count_num)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("TIME");
            dt.Columns.Add("CONTENT");

            for(DateTime start_day = starttime; start_day <= endtime; start_day = start_day.AddDays(1))
            {
                string content = "";
                foreach(DataRow r in dt_temp.Rows)
                {
                    DateTime dt_temp_date = Convert.ToDateTime(r["CREATTIME"]);
                    //if (start_day.Date > dt_temp_date.Date && (start_day.Date - dt_temp_date.Date).Days < count_num)
                    //{
                    //    if ((start_day.Date - dt_temp_date.Date).Days == 0)
                    //        content = Replace_Word + "/";
                    //    else
                    //        content = (start_day.Date - dt_temp_date.Date).Days.ToString() + "/";
                    //}
                    //if (start_day.Date == dt_temp_date.Date && (start_day.Date - dt_temp_date.Date).Days < count_num)
                    //{
                    //    if ((start_day.Date - dt_temp_date.Date).Days == 0)
                    //        content += Replace_Word + "/";
                    //    else
                    //        content += (start_day.Date - dt_temp_date.Date).Days.ToString() + "/";
                    //}
                    if(start_day.Date >= dt_temp_date.Date && (start_day.Date - dt_temp_date.Date).Days < count_num)
                    {
                        if((start_day.Date - dt_temp_date.Date).Days == 0)
                            content += Replace_Word + "/ ";
                        else
                            content += (start_day.Date - dt_temp_date.Date).Days.ToString() + "/ ";
                    }
                }
                DataRow new_r = dt.NewRow();
                new_r["TIME"] = start_day;
                new_r["CONTENT"] = (content != "") ? content.Substring(0, content.Length - 2) : content;
                dt.Rows.Add(new_r);
            }

            return dt;
        }

        private string sel_io_list(string date, string type, string feeno)
        {
            if(date != "" && type != "")
            {
                TubeManager tubem = new TubeManager();
                DataTable dt = iom.sel_io_data("", feeno, "", Convert.ToDateTime(date).ToString("yyyy/MM/dd 07:01:00"), Convert.ToDateTime(date).AddDays(1).ToString("yyyy/MM/dd 07:00:59"), "1", type);
                string content = "";
                foreach(DataRow r in dt.Rows)
                {
                    if(type != "9")
                    {
                        content += r["P_NAME"].ToString() + r["NAME"].ToString() + " ";
                        if(r["AMOUNT"].ToString() == "" && r["REASON"].ToString() != "")
                            content += "Loss";
                        else
                        {
                            if(r["AMOUNT_UNIT"].ToString() == "1")
                                content += r["AMOUNT"].ToString() + "mL";
                            else if(r["AMOUNT_UNIT"].ToString() == "2")
                                content += "  " + r["AMOUNT"].ToString() + "g";
                            else if(r["AMOUNT_UNIT"].ToString() == "3")
                                content += "  " + r["AMOUNT"].ToString() + "mg";
                        }
                    }
                    else
                    {
                        DataTable dt_tube_name = tubem.sel_tube("", "", r["ITEMID"].ToString(), "", "N");
                        if(dt_tube_name.Rows.Count > 0)
                        {
                            content += dt_tube_name.Rows[0]["TYPE_NAME"].ToString() + dt_tube_name.Rows[0]["POSITION"].ToString() + dt_tube_name.Rows[0]["LOCATION_NAME"].ToString();
                            if(dt_tube_name.Rows[0]["NUMBERID"].ToString() != "99")
                                content += "#" + dt_tube_name.Rows[0]["NUBER_NAME"].ToString();
                            else
                                content += "#" + dt_tube_name.Rows[0]["NUMBEROTHER"].ToString();
                        }
                        if(r["AMOUNT"].ToString() == "" && r["REASON"].ToString() != "")
                            content += "Loss";
                        else
                        {
                            if(r["AMOUNT_UNIT"].ToString() == "1")
                                content += "  " + r["AMOUNT"].ToString() + "mL";
                            else if(r["AMOUNT_UNIT"].ToString() == "2")
                                content += "  " + r["AMOUNT"].ToString() + "g";
                            else if(r["AMOUNT_UNIT"].ToString() == "3")
                                content += "  " + r["AMOUNT"].ToString() + "mg";
                        }
                    }
                    if(r["COLORID"].ToString() != "-1" && r["COLORID"].ToString() != "")
                        content += " 顏色：" + r["COLORNAME"].ToString().Replace("其他", "") + r["COLOROTHER"];
                    if(r["TASTEID"].ToString() != "-1" && r["TASTEID"].ToString() != "")
                        content += " 氣味：" + r["TASTENAME"].ToString().Replace("其他", "") + r["TASTEOTHER"];
                    if(r["NATUREID"].ToString() != "-1" && r["NATUREID"].ToString() != "")
                        content += " 性質：" + r["NATURENAME"].ToString().Replace("其他", "") + r["NATUREOTHER"];

                    content += "\n";
                }

                return content;
            }
            else
                return "";
        }

        #endregion

        #region 醫生,書記用畫面

        //單一病人_必傳批價序號
        public ActionResult HView_Index(string feeno)
        {
            if(feeno != null)
            {
                getSession(feeno); //HISVIEW 不走 MainController,因此另外處理session
                byte[] ptinfoByteCode = webService.GetPatientInfo(feeno);
                if(ptinfoByteCode != null)
                    ViewBag.fee_no = feeno;
                else
                    ViewBag.fee_no = "";
            }
            Session["FunUrl"] = "";

            return View();
        }

        //包含病人清單_可自由切換
        public ActionResult HView_Main(string feeno, string costcode)
        {
            if(feeno != null)
            {
                byte[] ptinfoByteCode = webService.GetPatientInfo(feeno);
                if(ptinfoByteCode != null)
                {
                    string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                    PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                    ViewBag.costcode = pi.CostCenterNo.Trim();
                    ViewBag.feeno = feeno;
                }
            }
            else
            {
                ViewBag.feeno = feeno;
                ViewBag.costcode = costcode;
            }
            Session["FunUrl"] = "";

            return View();
        }

        //病人資訊_功能清單
        public ActionResult HView_Function(string feeno)
        {
            try
            {
                if (feeno == null || feeno == "")
                    return View();

                byte[] ptinfoByteCode = webService.GetPatientInfo(feeno);
                if(ptinfoByteCode != null)
                {
                    getSession(feeno); //HISVIEW 不走 MainController,因此另外處理session
                    string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                    PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                    ViewBag.ChartNo = pi.ChartNo;
                    ViewBag.PatientName = pi.PatientName;
                    ViewBag.BedNo = pi.BedNo;
                    ViewBag.Age = pi.Age;
                    ViewBag.PatientGender = pi.PatientGender;
                }
                else
                    ViewBag.ChartNo = null;

                string sqlstr = " select * from SYS_PARAMS where P_MODEL='HISView' ORDER BY P_SORT";
                DataTable Dt = link.DBExecSQL(sqlstr);
                List<List<string>> FunList = new List<List<string>>();
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        FunList.Add(new List<string> {
                            Dt.Rows[i]["P_NAME"].ToString(),
                            Dt.Rows[i]["P_VALUE"].ToString(),
                            Dt.Rows[i]["P_GROUP"].ToString(),
                            Dt.Rows[i]["P_MEMO"].ToString()
                            //reader["P_NAME"].ToString(),
                            //reader["P_VALUE"].ToString(),
                            //reader["P_GROUP"].ToString(),
                            //reader["P_MEMO"].ToString()
                        });
                    }
                }
                ViewData["FunList"] = FunList;
                ViewBag.feeno = feeno;
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(),ex);
            }
            return View();
        }

        public string Session_Modify(string funurl, string fun)
        {
            if(funurl == "get")
                return Session["FunUrl"].ToString().Trim();
            else
            {
                Session["FunUrl"] = funurl + "[" + fun + "]";
                return "Y";
            }
        }

        public ActionResult HView_PatList()
        {
            if(Request["costcode"] != null)
            {
                byte[] ptByteCode = webService.GetPatientList(Request["costcode"].ToString());

                if(ptByteCode != null)
                {
                    string ptJsonArr = CompressTool.DecompressString(ptByteCode);
                    List<PatientList> patList = JsonConvert.DeserializeObject<List<PatientList>>(ptJsonArr);
                    patList.Sort((x, y) => { return -x.BedNo.CompareTo(y.BedNo); });
                    patList.Reverse();

                    byte[] listByteCode = webService.GetCostCenterList();
                    string listJsonArray = CompressTool.DecompressString(listByteCode);
                    List<CostCenterList> costlist = JsonConvert.DeserializeObject<List<CostCenterList>>(listJsonArray);
                    List<SelectListItem> cCostList = new List<SelectListItem>();
                    // 設定使用者預設護理站
                    for(int i = 0; i <= costlist.Count - 1; i++)
                    {
                        bool select = false;
                        if(Request["costcode"].ToString() == costlist[i].CostCenterCode.Trim())
                            select = true;
                        cCostList.Add(new SelectListItem()
                        {
                            Text = costlist[i].CCCDescription.Trim(),
                            Value = costlist[i].CostCenterCode.Trim(),
                            Selected = select
                        });
                    }
                    ViewData["costlist"] = cCostList;
                    ViewData["costcode"] = Request["costcode"].ToString().Trim();
                    ViewData["PatList"] = patList;

                    return View();
                }
                else
                    return new EmptyResult();
            }
            return null;
        }

        public ActionResult HView_InHistory()
        {
            if(Request["chr_no"] != null)
            {
                // 取得住院歷史資料
                byte[] inHistoryByte = webService.GetInHistory(Request["chr_no"].ToString().Trim());
                if(inHistoryByte != null)
                {
                    string inHistoryJson = CompressTool.DecompressString(inHistoryByte);
                    List<InHistory> inHistoryList = JsonConvert.DeserializeObject<List<InHistory>>(inHistoryJson);
                    ViewData["inHistory"] = inHistoryList;
                }
            }
            ViewBag.flag = "view";
            return View();
        }

        //病人住院紀錄
        public ActionResult WebS_InHistory()
        {
            string str = string.Empty;
            if(Request["str_Barcode"] != null)
            {
                byte[] doByteCode = webService.GetInHistory(Request["str_Barcode"].ToString().Trim());
                if(doByteCode == null)
                { Response.Write("Error"); }
                else
                {
                    string doJsonArr = CompressTool.DecompressString(doByteCode);
                    List<InHistory> IpdList = JsonConvert.DeserializeObject<List<InHistory>>(doJsonArr);
                    if(IpdList.Count > 0)
                    {
                        var inFeeNo = from a in IpdList where a.IpdFlag == "Y" select a.FeeNo.Trim();
                        var costcode = from a in IpdList where a.IpdFlag == "Y" select a.CostCode.Trim();
                        if(inFeeNo.Count() > 0)
                        {
                            byte[] ptinfoByteCode = webService.GetPatientInfo(inFeeNo.First());
                            if(ptinfoByteCode != null)
                            {
                                string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                                PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                                Session["PatInfo"] = pi;
                                Response.Write("./HView_Function?feeno=" + inFeeNo.First());
                            }
                            else
                                Response.Write("Not in IPD");
                        }
                        else
                            Response.Write("Not in IPD");
                    }
                    else
                        Response.Write("Error");
                }
            }
            return new EmptyResult();
        }

        #endregion

        #region  給藥紀錄 Med_QueryExecLogByUD by jarvis 2017/01/17 該方法已無用，以Med_QueryExecLog為主
        //HISView/Med_QueryExecLogByUD?TypeFlag=All&feeno=I0332966
        public ActionResult Med_QueryExecLogByUD(FormCollection form, string feeno, string ExecFlag, string TypeFlag)
        {
            if(!string.IsNullOrEmpty(feeno)) 
            {
                getSession(feeno); //HISVIEW 不走 MainController,因此另外處理session
                ViewBag.feeno = feeno;
                ExecFlag = "new";
                ViewBag.ExecFlag = ExecFlag;
                ViewBag.TypeFlag = TypeFlag;
                ViewBag.RootDocument = GetSourceUrl();
                return View();
            }
            Response.Write("<script>alert('登入逾時');</script>");
            return new EmptyResult();
        }

        //病人用藥
        public string WebS_UdOrder(string feeno, string start, string end, string ExecFlag, string medcode)
        {
            getSession(feeno); //HISVIEW 不走 MainController,因此另外處理session
            byte[] doByteCode = webService.GetUdOrder(feeno, "A");
            if(doByteCode == null)
            { return ("weberror"); }
            string doJsonArr = CompressTool.DecompressString(doByteCode);
            List<UdOrder> GetUdOrderList = JsonConvert.DeserializeObject<List<UdOrder>>(doJsonArr);

            dt_udorder = ConvertToDataTable(GetUdOrderList);
            DataRow[] dr_row;

            if(ExecFlag == "quiryAll" || ExecFlag == "quiryInterval")
            {   //給藥查詢
                dt_all = cm.get_drugtable();
                dt_all = dt_udorder.Copy();
            }
            else if(ExecFlag == "quiryAllByUD" || ExecFlag == "quiryIntervalByUD")
            {   //給藥查詢
                dt_all = cm.get_drugtable();
                if(ExecFlag == "quiryAllByUD")
                { dt_all = dt_udorder.Copy(); }
                else
                {
                    dr_row = dt_udorder.Select(cm.Get_DrugListSql(feeno, "UD", start, end, ExecFlag));
                    foreach(DataRow dr in dr_row)
                        dt_all.ImportRow(dr);
                }
            }
            else if(ExecFlag == "C_first" || ExecFlag == "C_quiryAll")
            {   //取消給藥
                dt_all = cm.get_drugtable();
                dr_row = dt_udorder.Select(cm.Get_DrugListSql(feeno, "C", start, end, ExecFlag));
                foreach(DataRow dr in dr_row)
                    dt_all.ImportRow(dr);
            }
            else if(ExecFlag == "QueryOrder")
            {   //用藥查詢
                dt_all = dt_udorder;
            }
            else
            {   //執行給藥
                dt_stat = cm.get_drugtable();
                dt_reg = dt_stat.Clone();
                dt_prn = dt_stat.Clone();
                dt_iv = dt_stat.Clone();

                dr_row = dt_udorder.Select(cm.Get_DrugListSql(feeno, "S", start, end, ExecFlag));
                foreach(DataRow dr in dr_row)
                    dt_stat.ImportRow(dr);

                dr_row = null;
                dr_row = dt_udorder.Select(cm.Get_DrugListSql(feeno, "R", start, end, ExecFlag));
                foreach(DataRow dr in dr_row)
                    dt_reg.ImportRow(dr);

                dr_row = null;
                dr_row = dt_udorder.Select(cm.Get_DrugListSql(feeno, "P", start, end, ExecFlag));
                foreach(DataRow dr in dr_row)
                    dt_prn.ImportRow(dr);

                dr_row = null;
                dr_row = dt_udorder.Select(cm.Get_DrugListSql(feeno, "V", start, end, ExecFlag));
                foreach(DataRow dr in dr_row)
                    dt_iv.ImportRow(dr);

                var sliding = from tmp in GetUdOrderList
                              where tmp.UD_STATUS == "2" && tmp.UD_PATH.Trim() == "SC" && (tmp.UD_TYPE == "P" || tmp.UD_TYPE == "S" || tmp.UD_CIR.Trim() == "ASORDER")
                              select new
                              {
                                  UD_SEQ = tmp.UD_SEQ,
                                  MED_DESC = tmp.MED_DESC.ToString().Trim(),
                                  UD_DOSE = decimal.Parse(tmp.UD_DOSE.ToString().Trim()).ToString("0")
                              };
                List<SelectListItem> sliding_list = new List<SelectListItem>();
                sliding_list.Add(new SelectListItem { Text = "請選擇", Value = "0" });
                if(sliding.Count() > 0)
                {
                    foreach(var tmp in sliding)
                    {
                        sliding_list.Add(new SelectListItem { Text = tmp.MED_DESC, Value = tmp.UD_DOSE });
                    }
                }
                ViewBag.sliding_list = sliding_list;
            }
            return ("OK");
        }

        public DataTable ConvertToDataTable<T>(IList<T> data)
        {
            System.ComponentModel.PropertyDescriptorCollection properties = System.ComponentModel.TypeDescriptor.GetProperties(typeof(T));
            DataTable table = new DataTable();
            foreach(System.ComponentModel.PropertyDescriptor prop in properties)
                table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            foreach(T item in data)
            {
                DataRow row = table.NewRow();
                foreach(System.ComponentModel.PropertyDescriptor prop in properties)
                    row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
                table.Rows.Add(row);
            }
            return table;

        }

        //補一個session用
        public ActionResult WebS_ChangPtInfo()
        {
            string str = Request["strFeeNo"];
            if(str != null)
            {
                byte[] ptinfoByteCode = webService.GetPatientInfo(str);
                if (ptinfoByteCode != null)
                {
                    string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                    PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                    Session["PatInfo"] = pi;
                }
                else
                    Response.Write("Error");
            }
            return new EmptyResult();
        }
        #endregion  給藥紀錄 end

        #region 給藥紀錄的資料 2016/11/09 by yungchen
        [HttpGet]
        public ActionResult Med_SearchList(string FeeNo)
        {
            getSession(FeeNo); //HISVIEW 不走 MainController,因此另外處理session
            ViewBag.FeeNo = FeeNo;
            ViewBag.RootDocument = GetSourceUrl();
            return View();
        }
        [HttpPost]
        public string Med_SearchList(string FeeNo, string Date)
        {
            try
            {
                //string FeeNo = Request.Form["feeno"].ToString().Trim();
                //string Date = Convert.ToDateTime(form["StartDate"]).ToString("yyyy/MM/dd");
                if (FeeNo != null)
                {
                    getSession(FeeNo); //HISVIEW 不走 MainController,因此另外處理session
                    List<UdOrder> UdOrderList = new List<UdOrder>();
                    UdOrder Ud_Temp = null;
                    byte[] Temp_Byte = webService.GetUdOrder(FeeNo, "A");
                    if(Temp_Byte != null)
                        UdOrderList = JsonConvert.DeserializeObject<List<UdOrder>>(CompressTool.DecompressString(Temp_Byte));

                    Date = (string.IsNullOrWhiteSpace(Date)) ? DateTime.Now.ToString("yyyy/MM/dd 00:00:00") : Convert.ToDateTime(Date).ToString("yyyy/MM/dd 00:00:00");
                    string endDate = Convert.ToDateTime(Date).ToString("yyyy/MM/dd 23:59:59");
                    List<Dictionary<string, string>> Med_List = new List<Dictionary<string, string>>(), Dt = new List<Dictionary<string, string>>();
                    Dictionary<string, string> Temp = null;
                    string sql = string.Format("SELECT UD_SEQ, EXEC_DATE, EXEC_NAME, REASONTYPE, REASON,CHECKER,EARLY_REASON,"
                    + "DRUG_OTHER,NON_DRUG_OTHER,INSULIN_SITE,REMARK "
                    + "FROM H_DRUG_EXECUTE "
                    + "WHERE EXEC_DATE IS NOT NULL AND FEE_NO = '{0}' AND EXEC_DATE BETWEEN TO_DATE('{1}','yyyy/MM/dd hh24:mi:ss') "
                    + "AND TO_DATE('{2}','yyyy/MM/dd hh24:mi:ss')", FeeNo, Date, endDate);

                    DataTable Dt2 = cm.DBExecSQL(sql); //上面Med_List裡面已有Dt
                    if (Dt2.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt2.Rows.Count; i++)
                        {
                            Temp = new Dictionary<string, string>();
                            Temp["UD_SEQ"] = Dt2.Rows[i]["UD_SEQ"].ToString();
                            Temp["EXEC_DATE"] = Convert.ToDateTime(Dt2.Rows[i]["EXEC_DATE"].ToString()).ToString("yyyy/MM/dd HH:mm");
                            Temp["EXEC_NAME"] = Dt2.Rows[i]["EXEC_NAME"].ToString();
                            Temp["REASONTYPE"] = Dt2.Rows[i]["REASONTYPE"].ToString();
                            Temp["REASON"] = Dt2.Rows[i]["REASON"].ToString();

                            Temp["CHECKER"] = Dt2.Rows[i]["CHECKER"].ToString();
                            Temp["EARLY_REASON"] = Dt2.Rows[i]["EARLY_REASON"].ToString();
                            Temp["NON_DRUG_OTHER"] = Dt2.Rows[i]["NON_DRUG_OTHER"].ToString();
                            Temp["DRUG_OTHER"] = Dt2.Rows[i]["DRUG_OTHER"].ToString();
                            Temp["INSULIN_SITE"] = Dt2.Rows[i]["INSULIN_SITE"].ToString();
                            Temp["REMARK"] = Dt2.Rows[i]["REMARK"].ToString();
                            Med_List.Add(Temp);
                        }
                    }
                    if(Med_List.Count > 0)
                    {
                        List<string> SeqList = Med_List.Select(x => x["UD_SEQ"].ToString()).Distinct().ToList();
                        List<Dictionary<string, string>> Med_List_Temp = null;
                        foreach(string Seq in SeqList)
                        {
                            Temp = new Dictionary<string, string>();
                            string temp = "";
                            Ud_Temp = UdOrderList.Find(x => x.UD_SEQ == Seq);
                            Temp["MED_DESC"] = (Ud_Temp != null) ? Ud_Temp.MED_DESC.Trim() : "";
                            Temp["UD_DOSE"] = (Ud_Temp != null) ? Ud_Temp.UD_DOSE.Trim() + " " + Ud_Temp.UD_UNIT.Trim() : "";
                            Temp["UD_CIR"] = (Ud_Temp != null) ? Ud_Temp.UD_CIR.Trim() : "";
                            Temp["UD_PATH"] = (Ud_Temp != null) ? Ud_Temp.UD_PATH.Trim() : "";
                            Med_List_Temp = Med_List.FindAll(x => x["UD_SEQ"].ToString() == Seq);
                            foreach(var r in Med_List_Temp)
                            {
                                temp = string.Format("實際給藥時間：{0}，給藥人員姓名：{1}，",
                                        r["EXEC_DATE"], r["EXEC_NAME"]);

                                if(!string.IsNullOrWhiteSpace(r["CHECKER"].ToString()))
                                    temp += string.Format("覆核者姓名：{0}", r["CHECKER"] + "，");

                                if(!string.IsNullOrWhiteSpace(r["INSULIN_SITE"].ToString()) && r["INSULIN_SITE"].ToString().Trim() != "選擇部位")
                                    temp += string.Format("注射部位：{0}", r["INSULIN_SITE"] + "，");

                                if(!string.IsNullOrWhiteSpace(r["REASONTYPE"].ToString()))
                                    temp += string.Format("未給予：{0}" + "，", r["REASONTYPE"].ToString().Trim() != "其他" ? r["REASONTYPE"] : r["NON_DRUG_OTHER"]);
                                else if(!string.IsNullOrWhiteSpace(r["REASON"].ToString()))
                                    temp += string.Format("延遲給藥：{0}" + "，", r["REASON"].ToString().Trim() != "其他" ? r["REASON"] : r["DRUG_OTHER"]);
                                else if(!string.IsNullOrWhiteSpace(r["EARLY_REASON"].ToString()))
                                    temp += string.Format("提早給藥：{0}" + "，", r["EARLY_REASON"].ToString().Trim() != "其他" ? r["EARLY_REASON"] : r["DRUG_OTHER"]);

                                if(!string.IsNullOrWhiteSpace(r["REMARK"].ToString()))
                                    temp += string.Format("備註：{0}", r["REMARK"] + "，");

                                Temp["REMARK"] = temp.TrimEnd('，');

                                Dt.Add(Temp);
                                Temp = new Dictionary<string, string>();
                            }
                        }
                    }
                    ViewBag.FeeNo = FeeNo;
                    return JsonConvert.SerializeObject(Dt);

                }
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(),ex);
                return "";
            }
            return "";
        }
        #endregion

        #region 膀胱訓練 2016/11/09 by yungchen
        //列表查詢
        [HttpGet]
        public ActionResult BladderTraining_List(string feeno, string StartDate, string EndDate)
        {
            if(feeno != null)
            {
                getSession(feeno); //HISVIEW 不走 MainController,因此另外處理session
                if (string.IsNullOrWhiteSpace(StartDate))
                    StartDate = DateTime.Now.ToString("yyyy/MM/dd");
                if(string.IsNullOrWhiteSpace(EndDate))
                    EndDate = DateTime.Now.ToString("yyyy/MM/dd");

                string StartYesterday = Convert.ToDateTime(StartDate).AddDays(-1).ToString("yyyy/MM/dd 23:00:00");
                string SqlStr = "SELECT * FROM BLADDER_DATA "
                + "WHERE FEENO = '" + feeno + "' AND DELETED IS NULL "
                + "AND RECORD_TIME BETWEEN TO_DATE('" + StartYesterday + "','yyyy/mm/dd hh24:mi:ss') "
                + "AND TO_DATE('" + EndDate + " 22:59:59','yyyy/mm/dd hh24:mi:ss') "
                + "ORDER BY RECORD_TIME, UPDTIME";
                DataTable dt = this.link.DBExecSQL(SqlStr);

                ViewBag.dt = dt;
                ViewBag.StartDate = StartDate;
                ViewBag.EndDate = EndDate;
                ViewBag.FeeNo = feeno;
                ViewBag.RootDocument = GetSourceUrl();
                return View();
            }

            Response.Write("<script>alert('請重新選擇病患');</script>");
            return new EmptyResult();
        }
        [HttpPost]
        //列表查詢
        public ActionResult BladderTraining_List(FormCollection form)
        {
            string feeno = Request.Form["feeno"].ToString().Trim();
            string StartDate = Convert.ToDateTime(form["StartDate"]).ToString("yyyy/MM/dd");
            string EndDate = Convert.ToDateTime(form["EndDate"]).ToString("yyyy/MM/dd");
            if(feeno != null)
            {
                getSession(feeno); //HISVIEW 不走 MainController,因此另外處理session
                string StartYesterday = Convert.ToDateTime(StartDate).AddDays(-1).ToString("yyyy/MM/dd 23:00:00");
                string SqlStr = "SELECT * FROM BLADDER_DATA "
                + "WHERE FEENO = '" + feeno + "' AND DELETED IS NULL "
                + "AND RECORD_TIME BETWEEN TO_DATE('" + StartYesterday + "','yyyy/mm/dd hh24:mi:ss') "
                + "AND TO_DATE('" + EndDate + " 22:59:59','yyyy/mm/dd hh24:mi:ss') "
                + "ORDER BY RECORD_TIME, UPDTIME";
                DataTable dt = this.link.DBExecSQL(SqlStr);

                ViewBag.dt = dt;
                ViewBag.StartDate = StartDate;
                ViewBag.EndDate = EndDate;
                ViewBag.FeeNo = feeno;
                ViewBag.RootDocument = GetSourceUrl();
                return View();
            }

            Response.Write("<script>alert('請重新選擇病患');</script>");
            return new EmptyResult();
        }
        #endregion

        #region 尿管訓練 2016/11/09 by yungchen

        #region --範圍查詢頁面--
        [HttpGet]
        public ActionResult Range_Index(string feeno, string StartDate = "", string EndDate = "")
        {
            if(feeno != null)
            {
                getSession(feeno); //HISVIEW 不走 MainController,因此另外處理session
                if (StartDate == "")
                {
                    StartDate = DateTime.Now.ToString("yyyy/MM/dd");
                }
                if(EndDate == "")
                {
                    EndDate = DateTime.Now.ToString("yyyy/MM/dd");
                }

                DataTable dt_data = fc.sel_flush_catheter_data_bydate(feeno, StartDate, EndDate);
                List<NIS.Models.FlushCatheter.flush_catheter_data> fc_List = new List<NIS.Models.FlushCatheter.flush_catheter_data>();
                if(dt_data != null && dt_data.Rows.Count > 0)
                {
                    for(int i = 0; i < dt_data.Rows.Count; i++)
                    {
                        fc_List.Add(new NIS.Models.FlushCatheter.flush_catheter_data(
                              dt_data.Rows[i]["RECORD_ID"].ToString(), Convert.ToDateTime(dt_data.Rows[i]["RECORD_TIME"].ToString())
                            , dt_data.Rows[i]["RECORD_CLASS"].ToString(), dt_data.Rows[i]["AMOUNT"].ToString()
                            , dt_data.Rows[i]["COLORID"].ToString(), dt_data.Rows[i]["COLOROTHER"].ToString()
                            , dt_data.Rows[i]["COLORNAME"].ToString(), dt_data.Rows[i]["BIT_SURPLUS"].ToString()
                            , dt_data.Rows[i]["POST_OP"].ToString()
                        ));
                    }
                }
                DataTable dt_time_table = new DataTable();
                dt_time_table.Columns.Add("morning");
                dt_time_table.Columns.Add("night");
                dt_time_table.Columns.Add("bignight");
                string[] classhourRangeStart = { "07:01", "15:01", "23:01" };
                string[] classhourRangeEnd = { "15:00", "23:00", "07:00" };
                for(DateTime x = Convert.ToDateTime(StartDate); x <= Convert.ToDateTime(EndDate); x = x.AddDays(1))
                {
                    DataRow row = dt_time_table.NewRow();
                    if(x <= Convert.ToDateTime(EndDate))
                    {
                        for(int y = 0; y < 3; y++)
                        {
                            //DateTime TempX = (y != 2) ? x : x.AddDays(-1);
                            DateTime TempX = (y != 2) ? x : x.AddDays(1);
                            //DataTable dt = fc.min_max_recordtime(feeno, TempX.ToString("yyyy/MM/dd ") + classhourRangeStart[y], x.ToString("yyyy/MM/dd ") + classhourRangeEnd[y]);
                            DataTable dt = fc.min_max_recordtime(feeno, x.ToString("yyyy/MM/dd ") + classhourRangeStart[y], TempX.ToString("yyyy/MM/dd ") + classhourRangeEnd[y]);
                            //if(dt != null && dt.Rows.Count > 0 && dt.Rows[0]["MAX"].ToString() != "")
                            if(dt != null && dt.Rows.Count > 0 && dt.Rows[0]["RECORD_TIME"].ToString() != "")
                            {
                                //修正為dt有值就跟目前班別結束時間相減，沒值就是帶出8小時 by jarvis lu 20160912
                                TimeSpan Total = Convert.ToDateTime(TempX.ToString("yyyy/MM/dd ") + classhourRangeEnd[y]).Subtract(Convert.ToDateTime(dt.Rows[0]["RECORD_TIME"].ToString()));
                                string minutes = Math.Round(((decimal)(Total.Minutes) / 60), 1).ToString();
                                double time_difference = Convert.ToDouble(minutes) + Total.Hours;
                                switch(y)
                                {
                                    case 0:
                                        row["morning"] = time_difference;
                                        break;
                                    case 1:
                                        row["night"] = time_difference;
                                        break;
                                    case 2:
                                        row["bignight"] = time_difference;
                                        break;
                                }
                            }
                            else
                            {
                                switch(y)
                                {
                                    case 0:
                                        row["morning"] = "8";
                                        break;
                                    case 1:
                                        row["night"] = "8";
                                        break;
                                    case 2:
                                        row["bignight"] = "8";
                                        break;
                                }
                            }
                        }
                    }
                    dt_time_table.Rows.Add(row);
                }
                dt_time_table.AcceptChanges();
                ViewData["fc_List"] = fc_List;
                ViewBag.dt_time_table = dt_time_table;
                ViewBag.start_date = StartDate;
                ViewBag.end_date = EndDate;
                ViewBag.FeeNo = feeno;
                ViewBag.RootDocument = GetSourceUrl();
                return View();
            }
            Response.Write("<script>alert('登入逾時');</script>");
            return new EmptyResult();
        }
        #endregion

        #region --範圍查詢頁面--回傳POST查詢日期跳轉
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Range_Index(FormCollection form)
        {

            if(Request.Form["feeno"].ToString().Trim() != null)
            {
                Response.Write("<script>window.location.href='../hisview/Range_Index?feeno=" + form["feeno"] + "&StartDate=" + form["start_date"] + "&EndDate=" + form["end_date"] + "';</script>");
            }
            Response.Write("<script>alert('登入逾時');</script>");
            return new EmptyResult();
        }




        #endregion

        #region--明細--
        [HttpGet]
        public ActionResult CatheterFlushVolume_Detail(string feeno, string date)
        {
            getSession(feeno); //HISVIEW 不走 MainController,因此另外處理session
            DataTable dt_data = fc.sel_flush_catheter_data_bydate(feeno, date, date);
            ViewBag.date = date;
            ViewBag.dt_data = dt_data;

            return View();
        }
        #endregion

        #endregion

        #region CVVH 2016/11/14 by jarvis
        //HISView/CVVH_Index?feeno=I0333003//CVVH-HISVIEW主表頁面
        public ActionResult CVVH_Index(string feeno, string StartDate = "", string EndDate = "")
        {
            if(!string.IsNullOrEmpty(feeno))
            {
                getSession(feeno); //HISVIEW 不走 MainController,因此另外處理session
                if (StartDate == "")
                {
                    StartDate = DateTime.Now.ToString("yyyy/MM/dd");
                }
                if(EndDate == "")
                {
                    EndDate = DateTime.Now.ToString("yyyy/MM/dd");
                }
                feeno = feeno.Trim();
                StartDate = StartDate.Trim();
                EndDate = EndDate.Trim();
                ViewBag.dt = this.cvvh_data.CVVH_Main_List_Data(feeno, StartDate, EndDate);
                ViewBag.feeno = feeno;
                return View();
            }
            Response.Write("<script>alert('登入逾時');</script>");
            return new EmptyResult();
        }

        //HISView/CVVH_PrintList?feeno=I0333003//CVVH-HISVIEW記錄頁面
        public ActionResult CVVH_PrintList(string feeno, string dtldate, string id)
        {
            if(!string.IsNullOrEmpty(feeno))
            {
                getSession(feeno); //HISVIEW 不走 MainController,因此另外處理session
                if (string.IsNullOrEmpty(dtldate))
                {
                    dtldate = DateTime.Now.ToString("yyyy/MM/dd");
                }
                DataTable dt = this.cvvh_data.CVVH_Record_Data(id);
                #region 取統計值
                DataTable dtTotal = this.cvvh_data.CVVH_total(feeno, dtldate);
                if(dtTotal != null && dtTotal.Rows.Count > 0)
                {
                    ViewBag.beforeday = dtTotal.Rows[0]["beforeday"];
                    ViewBag.nextday = dtTotal.Rows[0]["nextday"];
                    ViewBag.white = dtTotal.Rows[0]["morning"];
                    ViewBag.night = dtTotal.Rows[0]["after"];
                    ViewBag.longnight = dtTotal.Rows[0]["night"];
                }
                #endregion
                ViewBag.dt = dt;
                ViewBag.id = id;
                ViewBag.dtldate = dtldate;
                ViewBag.RootDocument = GetSourceUrl();
                ViewBag.feeno = feeno;
                return View();
            }
            Response.Write("<script>alert('登入逾時');</script>");
            return new EmptyResult();
        }
        #endregion

        #region 皮瓣溫度 2016/11/14 by jarvis
        //HISView/DigitFlap_List?feeno=I0332966
        public ActionResult DigitFlap_List(string feeno, string St = "", string Ed = "")
        {
            if(!string.IsNullOrEmpty(feeno))
            {
                getSession(feeno); //HISVIEW 不走 MainController,因此另外處理session
                ViewBag.dt_data = dft.sel_temperature_data(feeno, "", "");  //部位
                ViewBag.feeno = feeno;
                ViewBag.RootDocument = GetSourceUrl();
                ViewBag.userno = "";
                if(St != "" && Ed != "")
                {
                    ViewBag.dt_data = dft.sel_temperature_data_row(feeno, St, Ed);  //部位 for 查詢
                }
                ViewBag.St = St;
                ViewBag.Ed = Ed;
                return View();
            }
            Response.Write("<script>alert('登入逾時');</script>");
            return new EmptyResult();
        }

        public ActionResult DigitFlap_Record(string feeno, string DFTempID = "", string St = "", string Ed = "")
        {
            if(!string.IsNullOrEmpty(feeno))
            {
                getSession(feeno); //HISVIEW 不走 MainController,因此另外處理session
                DataTable dt = new DataTable();
                //if (DFTempID == "")  首次載入，取各部位最後一筆評估；點選部位時，依 DFTempID 查詢
                dt = dft.sel_temperature_record(feeno, DFTempID, St, Ed);
                //改成所有的員工名稱都 經由WS取得
                dt.Columns.Add("username");
                if(dt.Rows.Count > 0)
                {
                    string userno = "";
                    foreach(DataRow r in dt.Rows)
                    {
                        userno = r["CREANO"].ToString();
                        byte[] listByteCode = webService.UserName(userno);
                        if(listByteCode != null)
                        {
                            string listJsonArray = CompressTool.DecompressString(listByteCode);
                            UserInfo other_user_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                            r["username"] = other_user_info.EmployeesName;
                        }
                    }
                }
                ViewBag.dt_record = dt;
                return PartialView();
            }
            Response.Write("<script>alert('登入逾時');</script>");
            return new EmptyResult();
        }
        #endregion

        #region 特殊事件(安寧照護)
        public JsonResult Sp_status(string chrno, string userid, string type, string sDate, string eDate)
        {
            type = "11"; //type11為安寧照護類別
            string feeno = "";
            if (Request["chrno"] != null)
            {
                // 取得住院歷史資料
                byte[] inHistoryByte = webService.GetInHistory(chrno.Trim());
                if (inHistoryByte != null)
                {
                    string inHistoryJson = CompressTool.DecompressString(inHistoryByte);
                    List<InHistory> inHistoryList = JsonConvert.DeserializeObject<List<InHistory>>(inHistoryJson);
                    List<string> aa = inHistoryList.Select(x => x.FeeNo).ToList();
                    feeno = string.Join("','", aa);
                }
            }
            List<string> where = new List<string>();
            if (!string.IsNullOrEmpty(userid))
            {
                where.Add("A.CREATNO = '" + userid + "'");
            }
            if (!string.IsNullOrEmpty(type))
            {
                where.Add("A.TYPE_ID = '" + type + "'");
            }
            if (!string.IsNullOrEmpty(sDate) && !string.IsNullOrEmpty(eDate))
            {
                where.Add("A.CREATTIME BETWEEN to_date('" + sDate + "','yyyy/mm/dd hh24:mi:ss') AND to_date('" + eDate + " 23:59:59', 'yyyy/mm/dd hh24:mi:ss') ");
            }
            DataTable dt = null;
            if (!string.IsNullOrEmpty(feeno))
            {
                //getSession(feeno); //HISVIEW 不走 MainController,因此另外處理session
                dt = care_record_m.get_special_event(feeno, "", where);
            }
            List<dataObj> InDataList = new List<dataObj>();
            DateTime NowDate = Convert.ToDateTime(sDate);
            while (NowDate <= Convert.ToDateTime(eDate))
            {
                InDataList.Add(new dataObj()
                {
                    chrno = chrno,
                    feeno = feeno,
                    date = NowDate.ToString("yyyy/MM/dd"),
                    hasData = (dt == null)? "N" : dt.AsEnumerable().ToList().Exists(x => Convert.ToDateTime(x["CREATTIME"]).ToString("yyyy/MM/dd") == NowDate.ToString("yyyy/MM/dd")) ? "Y" : "N"
                });
                NowDate = NowDate.AddDays(1);
            }
            return Json(InDataList, JsonRequestBehavior.AllowGet);
        }

        //列表
        [HttpGet]
        public ActionResult Special_Event(string chrno, string userid, string type , string sDate, string eDate, string feeno = "")
        {
            type = "11"; //type11為安寧照護類別
            byte[] WS_Byte = null;
            string WSJson = null;
            List<string> aa = null;
            if (Request["chrno"] != null)
            {
                // 取得住院歷史資料
                WS_Byte = webService.GetInHistory(chrno.Trim());
                if (WS_Byte != null)
                {
                    WSJson = CompressTool.DecompressString(WS_Byte);
                    List<InHistory> inHistoryList = JsonConvert.DeserializeObject<List<InHistory>>(WSJson);
                    aa = inHistoryList.Select(x => x.FeeNo).ToList();
                    feeno = string.Join("','", aa);

                }
            }
            if (!string.IsNullOrEmpty(feeno))
            {
                //getSession(feeno); //HISVIEW 不走 MainController,因此另外處理session
                WS_Byte = webService.GetPatientInfo((aa == null) ? "" : aa[0]);
                if (WS_Byte != null)
                {
                    WSJson = CompressTool.DecompressString(WS_Byte);
                    PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(WSJson);
                    ViewBag.pistring = "病患姓名:" + pi.PatientName + "，出生年月日:" + pi.Birthday.ToString("yyyy/MM/dd") + "，性別:" + pi.PatientGender;
                }
                List<string> where = new List<string>();
                if (!string.IsNullOrEmpty(userid))
                {
                    where.Add("A.CREATNO = '" + userid + "'");
                }
                if (!string.IsNullOrEmpty(type))
                {
                    //where.Add("A.TYPE_ID = '" + type + "'");
                    where.Add("A.TYPE_ID IN ('11','20')"); //顯示安寧會診紀錄(11)(已停用)及安寧共照照會紀錄(20)
                }
                if (!string.IsNullOrEmpty(sDate) && !string.IsNullOrEmpty(eDate))
                {
                    where.Add("A.CREATTIME BETWEEN to_date('" + sDate + "','yyyy/mm/dd hh24:mi:ss') AND to_date('" + eDate + " 23:59:59', 'yyyy/mm/dd hh24:mi:ss') ");
                }
                DataTable dt = null;
                if (!string.IsNullOrEmpty(feeno))
                {
                    dt = care_record_m.get_special_event(feeno, "", where);
                }
                ViewBag.userno = userid;
                ViewBag.dt_care_plan_master = care_record_m.GetCarePlan_Master(feeno);

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
                List<dataObj> InDataList = new List<dataObj>();
                DateTime NowDate = Convert.ToDateTime(sDate);
                while (NowDate <= Convert.ToDateTime(eDate))
                {
                    InDataList.Add(new dataObj()
                    {
                        chrno = chrno,
                        feeno = feeno,
                        date = NowDate.ToString("yyyy/MM/dd"),
                        hasData = (dt == null) ? "N" : dt.AsEnumerable().ToList().Exists(x => Convert.ToDateTime(x["CREATTIME"]).ToString("yyyy/MM/dd") == NowDate.ToString("yyyy/MM/dd")) ? "Y" : "N"
                    });
                    NowDate = NowDate.AddDays(1);
                }
            }
            return View();
        }
        #endregion
        #region 譫妄
        public ActionResult Delirium_List(string feeno = "")
        {
            if (!string.IsNullOrEmpty(feeno))
            {
                getSession(feeno); //HISVIEW 不走 MainController,因此另外處理session
            }
            ViewBag.feeno = feeno;
            ViewBag.RootDocument = GetSourceUrl();
            return View();
        }
        public ActionResult Delirium_ListData(string Pstart, string Pend ,string feeno)
        {
            if (Pstart == "" || Pstart == null)
            {
                Pstart = DateTime.Now.ToString("yyyy/MM/dd");
            }
            if (Pend == "" || Pend == null)
            {
                Pend = DateTime.Now.ToString("yyyy/MM/dd");
            }

            string StrSql = "SELECT * FROM DELIRIUM_DATA WHERE FEE_NO='" + feeno + "' ";
            StrSql += "AND ASSESS_DT BETWEEN TO_DATE('" + Pstart + " 00:00:00','yyyy/mm/dd hh24:mi:ss') ";
            StrSql += "AND  TO_DATE('" + Pend + " 23:59:59','yyyy/mm/dd hh24:mi:ss') ";
            StrSql += "AND DEL_USER IS NULL  AND STATUS = 'Y' ORDER BY  ASSESS_DT DESC , CREATE_DATE DESC";
            DataTable dt = this.link.DBExecSQL(StrSql);
            ViewBag.dt = dt;
            return View();
        }
        #endregion

        #region 衰弱
        public ActionResult CFS_List(string feeno = "")
        {
            if (!string.IsNullOrEmpty(feeno))
            {
                getSession(feeno); //HISVIEW 不走 MainController,因此另外處理session
            }
            ViewBag.feeno = feeno;
            ViewBag.RootDocument = GetSourceUrl();
            return View();
        }
        public ActionResult CFS_ListData(string Pstart, string Pend, string feeno)
        {
            //string feeno = ptinfo.FeeNo;
            if (Pstart == "" || Pstart == null)
            {
                Pstart = DateTime.Now.ToString("yyyy/MM/dd");
            }
            if (Pend == "" || Pend == null)
            {
                Pend = DateTime.Now.ToString("yyyy/MM/dd");
            }

            string StrSql = "SELECT * FROM CFS_DATA WHERE FEE_NO='" + feeno + "' ";
            StrSql += "AND ASSESS_DT BETWEEN TO_DATE('" + Pstart + " 00:00:00','yyyy/mm/dd hh24:mi:ss') ";
            StrSql += "AND  TO_DATE('" + Pend + " 23:59:59','yyyy/mm/dd hh24:mi:ss') ";
            StrSql += "AND DEL_USER IS NULL  AND STATUS = 'Y'  ORDER BY  ASSESS_DT DESC, CREATE_DATE DESC";
            DataTable dt = this.link.DBExecSQL(StrSql);

            ViewBag.dt = dt;
            return View();
        }
        #endregion
    }
}
