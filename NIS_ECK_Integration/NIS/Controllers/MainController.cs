using Newtonsoft.Json;
using NIS.Data;
using NIS.Models;
using NIS.UtilTool;
using NIS.WebService;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Web;
using System.Web.Mvc;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Net.Http;
using System.Net;
using Com.Mayaminer;
using System.Diagnostics;

namespace NIS.Controllers
{
    //取得手術訪視資料
    public class DataObject
    {
        public bool IsSuccess { get; set; }
        public string MessageByNotSuccess { get; set; }
        public List<SurgeryVisitItem> SurgeryVisitItems { get; set; }
    }
    public class SurgeryVisitItem
    {
        public string OPS_HEXNOW_NO { get; set; }
        public string SurgeryDate { get; set; }
        public string ASV_LastVisitDate { get; set; }
        public string PSV_LastVisitDate { get; set; }

    }
    public class MainController : BaseController
    {
        private CommData cd;
        private DBConnector link;
        LogTool log = new LogTool();

        public MainController()
        {
            this.cd = new CommData();
            this.link = new DBConnector();
        }

        // 首頁
        public ActionResult Index()
        {
            Session["TPR"] = null;
            if (Session["UserInfo"] != null)
            {
                ViewBag.username = userinfo.EmployeesName.Trim();
                ViewData["userid"] = userinfo.EmployeesNo;
                ViewData["Guiderid"] = userinfo.EmployeesNo;
                DataTable dt = new DataTable();
                string sqlstr = "", mode = "clist";
                List<string> cost_code = new List<string>();
                //if(debug_mode)
                //    userinfo.Category = "AD";
                #region 取出個人所屬功能列表
                sqlstr = " SELECT SM.*, SU.SET_DEFAULT_PAGE ";
                sqlstr += " , (SELECT COUNT(*) FROM SYS_FAVLIST WHERE EMPLOYE_NO = SU.EMP_NO AND MO_ID = SU.MO_ID )FAV ";
                sqlstr += " FROM SYS_MODELS SM JOIN SYS_USER_MODELS SU ON SM.MO_ID = SU.MO_ID ";
                sqlstr += " WHERE SU.EMP_NO = '" + userinfo.Category.ToString() + "' ";
                ////if (userinfo.Category != "ND")
                ////{
                ////    sqlstr += " AND SM.MO_ID <> 'T04' ";
                ////}
                //////ND 才能有管理功能
                ////if (userinfo.Category != "AD")
                ////    sqlstr += " AND SM.MO_ID <> 'M32' ";
                ////else
                ////    sqlstr += " AND SM.MO_ID <> 'M09' ";
                if (userinfo.EmployeesNo != "13278" && userinfo.EmployeesNo != "09277")
                    sqlstr += " AND SM.MO_ID <> 'V01' "; //HISVIEW測試  
                sqlstr += " ORDER BY SM.MODEL_SORT ASC, SM.FUNC_SORT ASC ";

                List<ModelItem> left_models = new List<ModelItem>();
                List<ModelItem> right_models = new List<ModelItem>();
                link.DBExecSQL(sqlstr, ref dt);
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    ModelItem models = new ModelItem()
                    {
                        mo_id = dt.Rows[i]["mo_id"].ToString().Trim(),
                        classname = dt.Rows[i]["class_name"].ToString().Trim(),
                        button_name = dt.Rows[i]["button_name"].ToString().Trim(),
                        model_url = dt.Rows[i]["model_url"].ToString().Trim(),
                        check_patinfo = dt.Rows[i]["check_patinfo"].ToString().Trim(),
                        hide_patinfo = dt.Rows[i]["hide_patinfo"].ToString().Trim(),
                        open_window = dt.Rows[i]["open_window"].ToString().Trim(),
                        set_default_page = dt.Rows[i]["set_default_page"].ToString().Trim(),
                        model_type = dt.Rows[i]["model_type"].ToString().Trim()
                    };
                    if (i % 2 == 0)
                        left_models.Add(models);
                    else
                        right_models.Add(models);
                    #region 功能清單排序 以類別排序分類 三院改排序方式 暫時不用
                    //switch (reader["class_name"].ToString().Trim())
                    //{
                    //    case "funcMed":
                    //    case "funcNursing":
                    //    case "funcManagement":
                    //    case "funcCPD":
                    //        left_models.Add(new ModelItem()
                    //        {
                    //            mo_id = reader["mo_id"].ToString().Trim(),
                    //            classname = reader["class_name"].ToString().Trim(),
                    //            button_name = reader["button_name"].ToString().Trim(),
                    //            model_url = reader["model_url"].ToString().Trim(),
                    //            check_patinfo = reader["check_patinfo"].ToString().Trim(),
                    //            hide_patinfo = reader["hide_patinfo"].ToString().Trim(),
                    //            open_window = reader["open_window"].ToString().Trim(),
                    //            set_default_page = reader["set_default_page"].ToString().Trim(),
                    //            model_type = reader["model_type"].ToString().Trim()
                    //        });
                    //        break;
                    //    case "funcVS":
                    //    case "funcAssessment":
                    //    case "funcMedAdv":
                    //        right_models.Add(new ModelItem()
                    //        {
                    //            mo_id = reader["mo_id"].ToString().Trim(),
                    //            classname = reader["class_name"].ToString().Trim(),
                    //            button_name = reader["button_name"].ToString().Trim(),
                    //            model_url = reader["model_url"].ToString().Trim(),
                    //            check_patinfo = reader["check_patinfo"].ToString().Trim(),
                    //            hide_patinfo = reader["hide_patinfo"].ToString().Trim(),
                    //            open_window = reader["open_window"].ToString().Trim(),
                    //            set_default_page = reader["set_default_page"].ToString().Trim(),
                    //            model_type = reader["model_type"].ToString().Trim()
                    //        });
                    //        break;
                    //}
                    #endregion
                }
                ViewData["left_models"] = left_models;
                ViewData["right_models"] = right_models;
                #endregion

                #region 取得派班資料
                sqlstr = "SELECT * FROM DATA_DISPATCHING WHERE RESPONSIBLE_USER = '" + userinfo.EmployeesNo + "' ";
                sqlstr += " AND SHIFT_DATE = TO_DATE(TO_CHAR(SYSDATE, 'yyyy/MM/dd'), 'yyyy/MM/dd')";
                dt = new DataTable();
                this.link.DBExecSQL(sqlstr, ref dt);
                if (dt.Rows.Count > 0)
                {
                    string shift_cate = "E";
                    if (DateTime.Now < Convert.ToDateTime(DateTime.Now.ToString("yyyy/MM/dd 8:00:00")))
                        shift_cate = "N";
                    else if (DateTime.Now < Convert.ToDateTime(DateTime.Now.ToString("yyyy/MM/dd 16:00:00")))
                        shift_cate = "D";
                    foreach (DataRow r in dt.Rows)
                    {
                        if (r["SHIFT_CATE"].ToString().Trim() == shift_cate)
                        {
                            mode = "mylist";
                            cost_code.Add(r["COST_CODE"].ToString().Trim());
                        }
                    }
                    ViewData["noSetShift"] = "N";
                }
                else
                    ViewData["noSetShift"] = "Y";

                if (Request["mode"] != null)
                    ViewData["mode"] = Request["mode"];
                else
                    ViewData["mode"] = mode;

                #endregion

                #region 設定使用者預設護理站
                byte[] listByteCode = webService.GetCostCenterList();
                string listJsonArray = CompressTool.DecompressString(listByteCode);
                List<CostCenterList> costlist = JsonConvert.DeserializeObject<List<CostCenterList>>(listJsonArray);
                List<SelectListItem> cCostList = new List<SelectListItem>();
                //第三順位_否則使用者歸屬單位
                string set_cost = userinfo.CostCenterCode.Trim();
                //第一順位_使用者有選擇過
                if (Request["cost_code"] != null)
                    set_cost = Request["cost_code"];
                //第二順位_派班表有_以第一筆為優先
                else if (cost_code.Count > 0)
                    set_cost = cost_code[0];

                for (int i = 0; i < costlist.Count; i++)
                {
                    bool select = false;
                    if (set_cost == costlist[i].CostCenterCode.Trim())
                        select = true;
                    cCostList.Add(new SelectListItem()
                    {
                        Text = costlist[i].CCCDescription.Trim(),
                        Value = costlist[i].CostCenterCode.Trim(),
                        Selected = select
                    });
                }

                ViewData["costlist"] = cCostList;
                #endregion

                #region 設定病人資料
                if (Session["PatInfo"] != null)
                    ViewData["hasPat"] = "Y";
                else
                    ViewData["hasPat"] = "N";
                #endregion

                return View();
            }
            else
                return RedirectToAction("Login");
        }

        #region Loging

        //登入
        public ActionResult Login()
        {
            //伺服器模式切換，請注意此功能為全域設定，主要是提供給佈署工具使用，設定時全部使用者皆會受到影響，不可隨意呼叫，需要更改設定時才呼叫，並需知會相關人員。
            if (!string.IsNullOrEmpty(Request["server_mode"]))
            {
                MvcApplication.ServerMode server_mode_enum = new MvcApplication.ServerMode();
                if (!Enum.TryParse(Request["server_mode"].ToString().Trim(), true, out server_mode_enum))
                {
                    //辨識失敗使用自動模式
                    server_mode_enum = MvcApplication.ServerMode.Auto;
                }
                MvcApplication.setServerMode(server_mode_enum);
            }
            //顯示伺服器資訊
            if (!string.IsNullOrEmpty(Request["show_info"]))
            {
                if (Request["show_info"].ToString().Trim() == "true")
                {
                    ViewData["server_info"] = MvcApplication.localIp + "(" + MvcApplication.iniObj.NisSetting.ServerMode.ToString() + ")";
                }
            }

            try
            {
                List<AnnouncmentItem> sa_list = new List<AnnouncmentItem>();
                string sqlstr = "select * from nis_sys_announcement where to_date(sa_dateline,'YYYY-MM-DD HH24:MI:SS') > sysdate order by sa_sort asc";
                DataTable Dt = this.link.DBExecSQL(sqlstr);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        sa_list.Add(new AnnouncmentItem(
                            int.Parse(Dt.Rows[i]["sa_id"].ToString().Trim()),
                            Dt.Rows[i]["sa_desc"].ToString().Trim(),
                            DateTime.Parse(Dt.Rows[i]["sa_dateline"].ToString().Trim()),
                            int.Parse(Dt.Rows[i]["sa_sort"].ToString().Trim())
                        ));
                    }
                }
                ViewData["sa_list"] = sa_list;

                return View();
            }
            catch (Exception ex)
            {
                string err = ex.Message;

                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return View();
            }
        }

        //驗證帳密
        public ActionResult LogCheck(string userName, string userPwd)
        {
            try
            {
                userName = userName.Trim();
                userPwd = userPwd.Trim();
                byte[] uData = webService.UserLogin(userName, userPwd);
                if (uData != null)
                {
                    string jsonstr = CompressTool.DecompressString(uData);
                    jsonstr = jsonstr.Substring(0, jsonstr.Length - 1) + ", \"Pwd\":\"" + userPwd + "\"}";
                    UserInfo ui = JsonConvert.DeserializeObject<UserInfo>(jsonstr.Replace(" ", string.Empty));
                    Session["ui"] = ui;
                    //if(ui.Category != "SN") 非實習護理師(SN)，則將指導者列為自己   --已改為不由NIS選擇代簽人員
                    string tempstr = RequestName(ui.EmployeesNo, ui.EmployeesNo, ui.Category);
                    if (string.IsNullOrEmpty(tempstr))
                    {
                        return RedirectToAction("Login", new { @message = "代簽護理師資料有誤，請與資訊室聯絡" });
                    }
                    ui.Guider = tempstr;
                    if (!string.IsNullOrEmpty(ui.CostCenterCode))
                    {
                        ui.Guider_CCCode = ui.CostCenterCode.Trim();
                    }
                    if (ui.Guider != ui.EmployeesNo)
                    {
                        byte[] listByteCode = webService.UserName(ui.Guider.Trim());
                        if (listByteCode != null)
                        {
                            string listJsonArray = CompressTool.DecompressString(listByteCode);
                            UserInfo user_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                            ui.Guider_CCCode = (string.IsNullOrEmpty(user_info.CostCenterCode)) ? user_info.CostCenterCode : user_info.CostCenterCode.Trim();
                        }
                    }
                    Session["UserInfo"] = ui;

                    // 加入登入資訊表
                    if (Session["UserDataList"] == null)
                        Session["UserDataList"] = new List<UserData>();
                    List<UserData> userlist = (List<UserData>)Session["UserDataList"];
                    UserData ud = new UserData();
                    ud.user_id = userName;
                    ud.SetSecCode(userPwd);
                    userlist.Add(ud);
                    Session["UserDataList"] = userlist;

                    //登入人員無權限則新增權限
                    string sqlstr = "";

                    if (ui.Category.Trim() != "ER")
                    {
                        int num = 0;
                        sqlstr = "SELECT COUNT(*) ROWCOUNT FROM SYS_USER_MODELS WHERE EMP_NO = '" + Request["userName"].ToString().Trim() + "'";
                        DataTable Dt = this.link.DBExecSQL(sqlstr);
                        if (Dt.Rows.Count > 0)
                        {
                            for (int i = 0; i < Dt.Rows.Count; i++)
                            {
                                num = Int16.Parse(Dt.Rows[i]["ROWCOUNT"].ToString());
                            }
                        }
                        if (num != 0)
                        {
                            sqlstr = "delete from SYS_USER_MODELS WHERE EMP_NO = '" + Request["userName"].ToString().Trim() + "'";
                            Dt = this.link.DBExecSQL(sqlstr);

                            sqlstr = "INSERT INTO SYS_USER_MODELS ";
                            sqlstr += " (SELECT '" + Request["userName"].ToString().Trim() + "',MO_ID,SET_DEFAULT_PAGE FROM SYS_USER_MODELS WHERE EMP_NO='Administrator' ";
                            if (ui.Category.Trim() != "AD")
                                sqlstr += " AND MO_ID <> 'M32' ";
                            sqlstr += " ) ";
                            Dt = this.link.DBExecSQL(sqlstr);
                        }
                    }
                    else
                    {
                        sqlstr = "delete from SYS_USER_MODELS WHERE EMP_NO = '" + Request["userName"].ToString().Trim() + "'";
                        DataTable Dt = this.link.DBExecSQL(sqlstr);
                        sqlstr = sqlstr = "INSERT INTO SYS_USER_MODELS ";
                        sqlstr += " (SELECT '" + Request["userName"].ToString().Trim() + "',MO_ID,SET_DEFAULT_PAGE FROM SYS_USER_MODELS WHERE EMP_NO='ER_Administrator' ";
                        sqlstr += " ) ";

                        Dt = this.link.DBExecSQL(sqlstr);
                    }

                    //取得補輸清單
                    Complement_Insert C_Insert_m = new Complement_Insert();
                    DataTable dt = C_Insert_m.sel_func_list(ui.EmployeesNo);
                    jsonstr = "";
                    if (dt.Rows.Count > 0 && dt.Rows[0][0].ToString() == "Y")
                        jsonstr += "{\"Status\":" + "true}";
                    else
                        jsonstr += "{\"Status\":" + "false}";
                    Complement_List c_list = JsonConvert.DeserializeObject<Complement_List>(jsonstr);
                    Session["Complement_List"] = c_list;

                    return RedirectToAction("Index");
                }
                else
                {
                    return RedirectToAction("Login", new { @message = "帳號或密碼錯誤" });
                }
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return RedirectToAction("Login", new { @message = "網路服務發生錯誤，請聯絡資訊室" });
            }
        }

        //登出
        public ActionResult Logout()
        {
            Session.Remove("UserInfo");
            Session.Remove("PatInfo");
            Session.Remove("Complement_List");
            Session.RemoveAll();
            return RedirectToAction("Login", new { @message = "已登出系統" });
        }

        ////即時訊息內容
        //public ActionResult MqInfo()
        //{
        //    if (Session["PatInfo"] != null)
        //    {
        //        ViewData["mqInfo"] = this.cd.getMqInfo(ptinfo.FeeNo);
        //    }
        //    return View();
        //}

        //即時訊息內容
        public string MqInfo()
        {
            if (Session["PatInfo"] != null)
            {
                return JsonConvert.SerializeObject(this.cd.getMqInfo(ptinfo.FeeNo));
            }
            else
            {
                return JsonConvert.SerializeObject(this.cd.getMqInfo("all"));
            }
        }

        #endregion

        #region My Favorites

        /// <summary> 移除我的最愛 </summary>
        public EmptyResult delFav()
        {
            if (Request["model_id"] != null)
            {
                string whereCondition = string.Empty;
                whereCondition += " employe_no='" + userinfo.EmployeesNo.Trim() + "' ";
                whereCondition += " and mo_id='" + Request["model_id"].ToString().Trim() + "' ";
                int effRow = this.link.DBExecDelete("sys_favlist", whereCondition);
                if (effRow == 1)
                    Response.Write("【" + Request["model_name"].ToString().Trim() + "】已移除");
                else
                    Response.Write("移除失敗，請聯絡資訊室");
            }
            return new EmptyResult();
        }

        /// <summary> 加入我的最愛 </summary>
        public EmptyResult addFav()
        {
            int selResult = 0;
            string model_name = string.Empty;
            if (Request["model_id"] != null && Session["UserInfo"] != null)
            {
                model_name = Request["model_name"].ToString().Trim();
                List<DBItem> dbi = new List<DBItem>();
                dbi.Add(new DBItem("employe_no", userinfo.EmployeesNo.ToString(), DBItem.DBDataType.String));
                dbi.Add(new DBItem("mo_id", Request["model_id"].ToString(), DBItem.DBDataType.String));
                selResult = this.link.DBExecInsert("sys_favlist", dbi);
            }

            switch (selResult)
            {
                case 0: Response.Write("加入失敗"); break;
                case 1: Response.Write("【" + model_name + "】加入成功"); break;
                default: Response.Write("超過快捷功能數量上限"); break;
            }
            return new EmptyResult();
        }
        /// <summary> 加入我的病人 </summary>
        public EmptyResult addPat(string model_name = "", string model_id = "", string model_bedno = "")
        {
            try
            {
                int selResult = 0;
                if (Request["model_id"] != null && Session["UserInfo"] != null)
                {
                    string sqlstr = "";
                    string CCCode = "";
                    string em_no = userinfo.EmployeesNo.ToString();
                    byte[] ptinfoByteCode = webService.GetPatientInfo(model_id.Trim());
                    if (ptinfoByteCode != null)
                    {
                        string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                        PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                        model_bedno = pi.BedNo;
                        CCCode = pi.CostCenterNo;
                    }

                    sqlstr = "SELECT * FROM DATA_FAVPAT WHERE EMPLOYE_NO='" + em_no + "' AND FEENO ='" + model_id + "'AND BED_NO ='" + model_bedno + "'";
                    sqlstr += " AND DATEITEM ='" + DateTime.Now.ToString("yyyy/MM/dd") + "'";

                    DataTable Dt = link.DBExecSQL(sqlstr);
                    if (Dt.Rows.Count <= 0)
                    {
                        bool HasRow = false;
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            HasRow = true;
                        }
                        if (!HasRow)
                        {
                            List<DBItem> dbi = new List<DBItem>();
                            dbi.Add(new DBItem("EMPLOYE_NO", em_no, DBItem.DBDataType.String));
                            dbi.Add(new DBItem("FEENO", model_id, DBItem.DBDataType.String));
                            dbi.Add(new DBItem("BED_NO", model_bedno, DBItem.DBDataType.String));
                            dbi.Add(new DBItem("CREATEDATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                            dbi.Add(new DBItem("DATEITEM", DateTime.Now.ToString("yyyy/MM/dd"), DBItem.DBDataType.String));
                            dbi.Add(new DBItem("COST_CODE", CCCode, DBItem.DBDataType.String));
                            selResult = this.link.DBExecInsert("data_favpat", dbi);
                        }
                    }
                }
                switch (selResult)
                {
                    case 0: Response.Write("該病人已加入至我的病人"); break;
                    case 1: Response.Write("【" + model_name + "】加入成功"); break;
                }

                return new EmptyResult();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return new EmptyResult();
            }
        }

        /// <summary> 移出我的病人 </summary>
        public EmptyResult delPat(string model_name = "", string model_id = "", string model_bedno = "")
        {
            try
            {
                int selResult = 0;
                if (Request["model_id"] != null && Session["UserInfo"] != null)
                {
                    string sqlstr = "";
                    string em_no = userinfo.EmployeesNo.ToString();
                    sqlstr = "SELECT * FROM DATA_FAVPAT WHERE EMPLOYE_NO='" + em_no + "' AND FEENO ='" + model_id + "'AND BED_NO ='" + model_bedno + "'";
                    sqlstr += " AND DATEITEM ='" + DateTime.Now.ToString("yyyy/MM/dd") + "'";
                    DataTable Dt = link.DBExecSQL(sqlstr);
                    if (Dt.Rows.Count > 0)
                    {
                        bool HasRow = false;
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            HasRow = true;
                        }
                        if (HasRow)
                        {
                            List<DBItem> dbi = new List<DBItem>();
                            DateTime Nowdate = DateTime.Now;
                            string whereCondition = " EMPLOYE_NO = '" + em_no + "' AND FEENO = '" + model_id + "' AND DATEITEM = '" + Nowdate.ToString("yyyy/MM/dd") + "'";
                            selResult = this.link.DBExecDelete("data_favpat", whereCondition);
                        }
                    }
                }
                switch (selResult)
                {
                    case 0: Response.Write("該病人已移出至我的病人"); break;
                    case 1: Response.Write("【" + model_name + "】移出成功"); break;
                }
                return new EmptyResult();

            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);

                return new EmptyResult();
            }
        }
        /// <summary> 送簽清單 </summary>
        public ActionResult SignList()
        {
            try
            {
                string userNo = userinfo.EmployeesNo.ToString().Trim();
                string sqlstr = "";
                sqlstr = " SELECT * FROM NIS_EMR_PACKAGE_MST WHERE (SIGNER = '" + userNo + "' OR REP_ID = '" + userNo + "' OR GUIDE_ID = '" + userNo + "')  AND SIGN_STATUS = 'U' AND (ERR IS NULL OR ERR = 'N') AND (STATUS = 'Y' OR STATUS IS NULL)";
                DataTable DtCK = this.link.DBExecSQL(sqlstr);
                List<SignList> signlist = new List<SignList>();
                if (DtCK.Rows.Count > 0)
                {
                    for (int i = 0; i < DtCK.Rows.Count; i++)
                    {
                        string ptinfoJosnArr = "";
                        PatientInfo pi = new PatientInfo();
                        var feeno = DtCK.Rows[i]["FEENO"].ToString().Trim();
                        byte[] ptinfoByteCode = this.webService.GetPatientInfo(feeno);
                        if (ptinfoByteCode != null)
                        {
                            ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                            pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                        }
                        var parsedDate = DateTime.Parse(DtCK.Rows[i]["CREATE_DTM"].ToString().Trim());
                        var recordDay = parsedDate.ToString("yyyy/MM/dd");

                        string rep = DtCK.Rows[i]["REP_ID"].ToString().Trim();
                        string repName = "";
                        if (rep != "")
                        {
                            byte[] listByteCode = webService.UserName(rep);
                            string listJsonArray = CompressTool.DecompressString(listByteCode);
                            UserInfo user_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                            repName = user_info.EmployeesName;
                        }
                        signlist.Add(new SignList()
                        {
                            Date = recordDay,
                            Chartno = DtCK.Rows[i]["CHART_NO"].ToString().Trim(),
                            //PatientName = DtCK.Rows[i]["button_name"].ToString().Trim(),
                            Shift = DtCK.Rows[i]["SHIFT"].ToString().Trim(),
                            Signer = DtCK.Rows[i]["SIGNER_NAME"].ToString().Trim(),
                            PatientName = pi.PatientName ?? "",
                            Bedno = pi.BedNo ?? "",
                            GuiderId = DtCK.Rows[i]["GUIDE_ID"].ToString().Trim(),
                            RepId = repName,
                        });
                    }
                    ViewBag.SignList = signlist;
                }
                sqlstr = "";
                var trim_employeeNo = userinfo.EmployeesNo.Trim();
                sqlstr = " SELECT * FROM NIS_EMR_PACKAGE_MST WHERE (SIGNER = '" + trim_employeeNo + "' OR REP_ID = '" + trim_employeeNo + "' OR GUIDE_ID = '" + trim_employeeNo + "') AND SIGN_STATUS = 'U' AND ERR = 'E'";
                DataTable DtER = this.link.DBExecSQL(sqlstr);
                List<SignList> signlister = new List<SignList>();
                if (DtER.Rows.Count > 0)
                {
                    for (int i = 0; i < DtER.Rows.Count; i++)
                    {
                        string ptinfoJosnArr = "";
                        PatientInfo pi = new PatientInfo();
                        var feeno = DtER.Rows[i]["FEENO"].ToString().Trim();
                        byte[] ptinfoByteCode = this.webService.GetPatientInfo(feeno);
                        if (ptinfoByteCode != null)
                        {
                            ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                            pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                        }
                        var parsedDate = DateTime.Parse(DtER.Rows[i]["CREATE_DTM"].ToString().Trim());
                        var recordDay = parsedDate.ToString("yyyy/MM/dd");
                        string rep = DtER.Rows[i]["REP_ID"].ToString().Trim();
                        string repName = "";
                        if (rep != "")
                        {
                            string listJsonArray = "";
                            UserInfo user_info = new UserInfo();
                            byte[] listByteCode = webService.UserName(rep);
                            if (listByteCode != null)
                            {
                                listJsonArray = CompressTool.DecompressString(listByteCode);
                                user_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                            }
                            repName = user_info.EmployeesName;
                        }
                        signlister.Add(new SignList()
                        {
                            Date = recordDay,
                            Chartno = DtER.Rows[i]["CHART_NO"].ToString().Trim(),
                            //PatientName = DtCK.Rows[i]["button_name"].ToString().Trim(),
                            Shift = DtER.Rows[i]["SHIFT"].ToString().Trim(),
                            Signer = DtER.Rows[i]["SIGNER_NAME"].ToString().Trim(),
                            PatientName = pi.PatientName ?? "",
                            Bedno = pi.BedNo ?? "",
                            GuiderId = DtER.Rows[i]["GUIDE_ID"].ToString().Trim(),
                            RepId = repName,
                        });
                    }
                    ViewBag.SignListER = signlister;
                }
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

        /// <summary> 送簽清單 </summary>
        public int SignCount()
        {
            int count = 0;
            try
            {
                string userNo = userinfo.EmployeesNo.ToString().Trim();
                string sqlstr = "";
                sqlstr = " SELECT * FROM NIS_EMR_PACKAGE_MST WHERE SIGNER = '" + userNo + "' AND SIGN_STATUS = 'U' AND (STATUS = 'Y' OR STATUS IS NULL)";
                DataTable DtCK = this.link.DBExecSQL(sqlstr);
                List<SignList> signlist = new List<SignList>();
                if (DtCK.Rows.Count > 0)
                {
                    for (int i = 0; i < DtCK.Rows.Count; i++)
                    {
                        signlist.Add(new SignList()
                        {
                            Date = DtCK.Rows[i]["CREATE_DTM"].ToString().Trim(),
                            Feeno = DtCK.Rows[i]["FEENO"].ToString().Trim(),
                            //PatientName = DtCK.Rows[i]["button_name"].ToString().Trim(),
                            Shift = DtCK.Rows[i]["SHIFT"].ToString().Trim(),
                            Signer = DtCK.Rows[i]["SIGNER_NAME"].ToString().Trim(),
                            RepId = DtCK.Rows[i]["REP_NAME"].ToString().Trim(),

                        });
                    }

                    ViewBag.SignList = signlist;
                }

                count = signlist.Count;
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

            return count;
        }

        /// <summary> 送簽清單 </summary>
        public int SignErrCount()
        {
            int count = 0;
            try
            {
                string userNo = userinfo.EmployeesNo.ToString().Trim();
                string sqlstr = "";
                sqlstr = " SELECT * FROM NIS_EMR_PACKAGE_MST WHERE SIGNER = '" + userNo + "' AND SIGN_STATUS = 'U' AND ERR ='E'";
                DataTable DtCK = this.link.DBExecSQL(sqlstr);
                List<SignList> signlist = new List<SignList>();
                if (DtCK.Rows.Count > 0)
                {
                    for (int i = 0; i < DtCK.Rows.Count; i++)
                    {
                        signlist.Add(new SignList()
                        {
                            Date = DtCK.Rows[i]["CREATE_DTM"].ToString().Trim(),
                            Feeno = DtCK.Rows[i]["FEENO"].ToString().Trim(),
                            //PatientName = DtCK.Rows[i]["button_name"].ToString().Trim(),
                            Shift = DtCK.Rows[i]["SHIFT"].ToString().Trim(),
                            Signer = DtCK.Rows[i]["SIGNER_NAME"].ToString().Trim(),
                            RepId = DtCK.Rows[i]["REP_NAME"].ToString().Trim(),

                        });
                    }

                    ViewBag.SignList = signlist;
                }

                count = signlist.Count;
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

            return count;
        }
        /// <summary> 我的最愛清單 </summary>
        public ActionResult MyFunction()
        {
            try
            {
                string sqlstr = "";
                if (Session["UserInfo"] != null)
                {
                    List<ModelItem> fl = new List<ModelItem>();

                    sqlstr = " SELECT /*+index(SYS_MODELS SYS_MODELS_IDX1)*/ * FROM SYS_MODELS ";
                    sqlstr += " WHERE MO_ID IN (SELECT MO_ID FROM SYS_FAVLIST WHERE EMPLOYE_NO = '" + userinfo.EmployeesNo.ToString() + "') ";
                    sqlstr += " ORDER BY MODEL_SORT ASC ";

                    DataTable Dt = this.link.DBExecSQL(sqlstr);
                    if (Dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            //取代網址列中的變數
                            string model_url = Dt.Rows[i]["model_url"].ToString().Trim();
                            if (Session["PatInfo"] != null)
                                model_url = model_url.Replace("#fee_no#", ptinfo.FeeNo.ToString()).Replace("#user_id#", userinfo.EmployeesNo.ToString());

                            fl.Add(new ModelItem()
                            {
                                mo_id = Dt.Rows[i]["mo_id"].ToString().Trim(),
                                classname = Dt.Rows[i]["class_name"].ToString().Trim(),
                                button_name = Dt.Rows[i]["button_name"].ToString().Trim(),
                                model_url = model_url,
                                check_patinfo = Dt.Rows[i]["check_patinfo"].ToString().Trim(),
                                hide_patinfo = Dt.Rows[i]["hide_patinfo"].ToString().Trim(),
                                open_window = Dt.Rows[i]["open_window"].ToString().Trim(),
                                model_type = Dt.Rows[i]["model_type"].ToString().Trim()
                            });
                        }
                    }
                    ViewData["favList"] = fl;
                }
                sqlstr = "";
                sqlstr = " SELECT * FROM NIS_EMR_PACKAGE_MST WHERE SIGNER = '" + base.userinfo.EmployeesNo + "' AND SIGN_STATUS = 'U' AND (STATUS = 'Y' OR STATUS IS NULL)";
                DataTable DtCK = this.link.DBExecSQL(sqlstr);
                if (DtCK.Rows.Count > 0)
                {
                    ViewBag.SignCount = DtCK.Rows.Count;
                }
                else
                {
                    ViewBag.SignCount = 0;
                }

                ViewBag.username = base.userinfo.EmployeesName;
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

        #endregion

        #region Patient

        /// <summary> 確定是否登出 </summary>
        public ActionResult CheckSession()
        {
            if (Session["UserInfo"] != null)
                Response.Write("Y");
            else
                Response.Write("N");
            return new EmptyResult();
        }

        // 取得病人基本資料 
        public ActionResult GetPatInfo()
        {
            PatientInfo pt = new PatientInfo();
            UserInfo userInfo = new UserInfo();
            DataObject resultJson = new DataObject();
            if (ptinfo != null)
            {
                //可能一些網路斷線或一些更細部的原因造成抓資料錯誤
                //導致物件沒有產生拋錯
                try
                {
                    DataTable dt = new DataTable();
                    pt = ptinfo;

                    // 取得病人狀態資料
                    Function func_m = new Function();
                    Assess ass_m = new Assess();
                    ViewBag.pressure = (func_m.sel_pressure_data(ptinfo.FeeNo.Trim()).Rows.Count > 0) ? "壓" : "";
                    ViewBag.pain = (func_m.sel_pain_data(ptinfo.FeeNo.Trim()).Rows.Count > 0) ? "痛" : "";
                    ViewBag.constraint = (func_m.sel_constraint_data(ptinfo.FeeNo.Trim()).Rows.Count > 0) ? "約" : "";
                    ViewBag.suicide = (func_m.sel_suicide_data(ptinfo.FeeNo.Trim()).Rows.Count > 0) ? "傷" : "";
                    ViewBag.icndiseasestr = (func_m.sel_icndiseasestr_data(ptinfo.FeeNo.Trim()).Rows.Count > 0) ? "隔" : "";
                    ViewBag.FRIDs = (func_m.sel_FRIDs(ptinfo.FeeNo.Trim()) == true) ? "藥" : "";
                    if (ptinfo.Age >= 18)
                    {

                        dt = ass_m.sel_fall_assess_data(ptinfo.FeeNo, "", "NIS_FALL_ASSESS_DATA");
                        if (dt.Rows.Count > 0)
                        {
                            if (int.Parse(dt.Rows[0]["TOTAL"].ToString()) >= 3)
                                ViewBag.fall = "跌";
                        }
                        dt.Reset();
                    }
                    else
                    {
                        dt = ass_m.sel_fall_assess_data(ptinfo.FeeNo, "", "NIS_FALL_ASSESS_DATA_CHILD");
                        if (dt.Rows.Count > 0)
                        {
                            if (int.Parse(dt.Rows[0]["TOTAL"].ToString()) >= 3)
                                ViewBag.fall = "跌";
                        }
                        dt.Reset();
                    }
                    List<PatientInfo> allergy_list = new List<PatientInfo>();
                    byte[] allergyfoByteCode = webService.GetAllergyList(ptinfo.FeeNo.Trim());
                    if (allergyfoByteCode != null)
                    {
                        string allergyJosnArr = CompressTool.DecompressString(allergyfoByteCode);
                        List<PatientInfo> allergy = JsonConvert.DeserializeObject<List<PatientInfo>>(allergyJosnArr);
                        allergy_list.AddRange(allergy);
                    }
                    ViewData["Allergy"] = allergy_list;

                    //取得手術訪視資料
                    //userInfo = userinfo;
                    //var outDate = pt.OutDate == DateTime.Parse("0001/1/1 00:00:00") ? "" : pt.OutDate.ToString("yyyy/MM/dd HH:mm");
                    //var client = new HttpClient();
                    //client.Timeout = TimeSpan.FromMilliseconds(5000);
                    //var formURLFormat = "http://{0}/ECK_OPS_API/api/HIS_DataQuery/GetOPS_SurgeryVisitData?ChartNo={1}&StartTime={2}&EndTime={3}&CallingUserID={4}";
                    //string url = MvcApplication.iniObj.NisSetting.Connection.AISUrl;
                    ////var formURL = String.Format(formURLFormat, url);
                    //var formURL = String.Format(formURLFormat, url, Url.Encode(pt.ChartNo), Url.Encode(pt.InDate.ToString("yyyy/MM/dd HH:mm")), Url.Encode(outDate), Url.Encode(userinfo.EmployeesNo));
                    //var sw = new Stopwatch();
                    //sw.Start();
                    //var response = client.GetAsync(formURL).Result;
                    //sw.Stop();
                    //this.log.saveLogMsg($"[GetOPS_SurgeryVisitData 時間]{sw.ElapsedMilliseconds}ms", "GetPatInfo");
                    //resultJson = response.Content.ReadAsAsync<DataObject>().Result;
                    //var SurgeryVisitItems = resultJson.SurgeryVisitItems;
                    //ViewData["SurgeryVisitItems"] = SurgeryVisitItems;
                    ViewData["SurgeryVisitItems"] = new List<SurgeryVisitItem>();
                }
                catch (Exception e)
                {
                    this.log.saveLogMsg(ptinfo.FeeNo.ToString() + e.Message.ToString(), "GetPatInfo");
                }
            }
            else
            {
                PropertyInfo[] pi = pt.GetType().GetProperties();
                foreach (PropertyInfo pi_tmp in pi)
                {
                    switch (pi_tmp.PropertyType.Name.ToLower())
                    {
                        case "string":
                            pi_tmp.SetValue(pt, "", null);
                            break;
                        case "datetime":
                            pi_tmp.SetValue(pt, DateTime.MinValue, null);
                            break;
                        case "int32":
                            pi_tmp.SetValue(pt, 0, null);
                            break;
                    }
                }
            }
            ViewData["userid"] = userinfo.EmployeesNo;
            ViewData["ptinfo"] = pt;
            return View();
        }

        // 檢查Session資料
        [HttpPost]
        public ActionResult SessionCheck()
        {
            //檢查使用者
            //string aa = Request["now_add"].ToString() + "____" + Request["now_reduce"].ToString();
            if (userinfo != null)
            {
                try
                {
                    if (userinfo.EmployeesNo != Request["user_id"].ToString())
                    {
                        UserData ud = user_list.Find(x => x.user_id == Request["user_id"].ToString().Trim());
                        byte[] uData = webService.UserLogin(ud.user_id, ud.GetSecCode());
                        string jsonstr = CompressTool.DecompressString(uData);
                        jsonstr = jsonstr.Substring(0, jsonstr.Length - 1) + ", \"Pwd\":\"" + ud.GetSecCode() + "\"}";
                        UserInfo ui = JsonConvert.DeserializeObject<UserInfo>(jsonstr.Replace(" ", string.Empty));
                        string tempstr = RequestName(ui.EmployeesNo);
                        if (string.IsNullOrEmpty(tempstr))
                        {
                            return RedirectToAction("Login", new { @message = "代簽護理師資料有誤，請與資訊室聯絡" });
                        }
                        ui.Guider = tempstr;
                        ui.Guider = RequestName(ui.EmployeesNo);
                        if (!string.IsNullOrEmpty(ui.CostCenterCode))
                        {
                            ui.Guider_CCCode = ui.CostCenterCode.Trim();
                        }
                        if (ui.Guider != ui.EmployeesNo)
                        {
                            byte[] listByteCode = webService.UserName(ui.Guider.Trim());
                            if (listByteCode != null)
                            {
                                string listJsonArray = CompressTool.DecompressString(listByteCode);
                                UserInfo user_info = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                                ui.Guider_CCCode = (string.IsNullOrEmpty(user_info.CostCenterCode)) ? user_info.CostCenterCode : user_info.CostCenterCode.Trim();
                            }
                        }
                        Session["UserInfo"] = ui;
                    }

                    //檢查病人
                    if (ptinfo != null && Request["fee_no"].ToString() != ptinfo.FeeNo)
                    {
                        // 已變更病人資訊
                        Response.Write("C");
                    }
                    else
                    {
                        // 不做任何變化
                        Response.Write("N");
                    }
                }
                catch (Exception)
                {
                    // 若發生錯誤則狀態回傳 登入逾時
                    // 前端會進行Log out
                    Response.Write("T");
                }
            }
            else
            {
                //登入逾時
                Response.Write("T");
            }

            return new EmptyResult();
        }

        // 檢查日期是否被改動過
        [HttpPost]
        public ActionResult UserDateCheck()
        {
            DateTime NowDate = DateTime.Now;//ToString("yyyy/MM/dd HH:mm")
            bool EditDate = false;// SERVER與本機時間是否對應

            try
            {
                //最近發現有Event log有很多Warning
                //疑似有回傳非DateTime可解析的字串，故做例外處理
                if (NowDate < Convert.ToDateTime(Request["now_add"].ToString()) && NowDate > Convert.ToDateTime(Request["now_reduce"].ToString()))
                {
                    EditDate = true;
                }
            }
            catch (Exception)
            {
                //若轉不成功，則EditDate維持false
            }


            if (!EditDate)
            {
                Response.Write("E");
            }
            else// 不做任何變化
            {
                Response.Write("N");
            }

            return new EmptyResult();
        }

        // 設定病人資料並返回首頁
        public ActionResult SetPatient(string fee_no)
        {
            if (fee_no != null && fee_no != "")
            {
                //fee_no = "I0332966";要用feeno查病歷號時候用
                byte[] ptinfoByteCode = webService.GetPatientInfo(fee_no.Trim());
                if (ptinfoByteCode != null)
                {
                    string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                    PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                    Session["PatInfo"] = pi;
                }
            }
            return new EmptyResult();
        }

        /// <summary> 取得我的病人床位清單 </summary>
        private string[] GetMyPatientList(string type = "")
        {
            List<string> retArr = new List<string>();
            try
            {
                string sqlstr = "";
                if (type == "mylist")
                {
                    sqlstr = " SELECT BED_NO FROM DATA_DISPATCHING WHERE ";
                    sqlstr += " RESPONSIBLE_USER = '" + userinfo.EmployeesNo + "' AND SHIFT_DATE = TO_DATE(TO_CHAR(SYSDATE,'yyyy/MM/dd'), 'yyyy/MM/dd') ";
                }
                else if (type == "favlist")
                {
                    sqlstr = " SELECT BED_NO FROM DATA_FAVPAT WHERE ";
                    sqlstr += " EMPLOYE_NO = '" + userinfo.EmployeesNo + "'AND DATEITEM = TO_CHAR(SYSDATE,'yyyy/MM/dd') ";
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
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);

                return retArr.ToArray();
            }
        }

        /// <summary> 取得我的病人床位清單 </summary>

        /// <summary> 病人清單 </summary>      
        public ActionResult PatList()
        {
            if (Request["mode"] != null)
            {
                byte[] ptByteCode = null;
                List<PatientList> patList = new List<PatientList>();
                //IDataReader reader = null;
                //嘗試抓取病人清單，因為中間會透過Web Service可能會發生錯誤
                try
                {
                    if (Request["mode"].ToString() == "clist")
                    {
                        ptByteCode = webService.GetPatientList(Request["costcode"].ToString());
                    }
                    else
                    {
                        if (userinfo.Category == "NA")
                        {
                            ptByteCode = webService.GetPatientListByBedList(GetMyPatientList("favlist"));
                        }
                        else
                        {
                            ptByteCode = webService.GetPatientListByBedList(GetMyPatientList("mylist"));
                        }
                    }

                    if (ptByteCode != null)
                    {
                        string ptJsonArr = CompressTool.DecompressString(ptByteCode);
                        patList = JsonConvert.DeserializeObject<List<PatientList>>(ptJsonArr);
                        patList.Sort((x, y) => { return -x.BedNo.CompareTo(y.BedNo); });
                        patList.Reverse();
                        byte[] NewOrderByteCode = webService.GetNewOrderFlag();
                        if (NewOrderByteCode != null)
                        {
                            string NewOrderJsonArr = CompressTool.DecompressString(NewOrderByteCode);
                            List<PatientList> NewOrderFlag = JsonConvert.DeserializeObject<List<PatientList>>(NewOrderJsonArr);
                            for (int i = 0; i < patList.Count; i++)
                            {
                                patList[i].New_Med_Advice = false;
                                for (int j = 0; j < NewOrderFlag.Count; j++)
                                {
                                    if (NewOrderFlag[j].FeeNo == patList[i].FeeNo)
                                    {
                                        patList[i].New_Med_Advice = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    //若發生錯誤就是抓不到PatientList
                }

                ViewData["costcode"] = Request["costcode"].ToString().Trim();
                ViewBag.mode = Request["mode"].ToString().Trim();
                ViewData["PatList"] = patList;
                ViewBag.Category = (userinfo != null ? userinfo.Category : null);
                patList = null;
                return View();
            }
            return new EmptyResult();
        }

        //全院(單位)所有床號
        protected string[] getBedListbyCode(string costcode)
        {
            List<string> bedlistArr = new List<string>();
            byte[] ptByteCode = null;
            ptByteCode = webService.GetBedList(costcode);
            if (ptByteCode != null)
            {
                string JsonArr = CompressTool.DecompressString(ptByteCode);
                List<BedItem> BedList = JsonConvert.DeserializeObject<List<BedItem>>(JsonArr);
                for (int i = 0; i < BedList.Count; i++)
                {
                    if (bedlistArr.IndexOf(BedList[i].BedNo) == -1)
                        bedlistArr.Add(BedList[i].BedNo);
                }

            }

            return bedlistArr.ToArray();
        }

        //病人住院紀錄
        public ActionResult WebS_InHistory(string str_Barcode)
        {
            string str = string.Empty;
            try
            {
                byte[] doByteCode = webService.GetInHistory(str_Barcode.Trim());
                string doJsonArr = CompressTool.DecompressString(doByteCode);
                List<InHistory> IpdList = JsonConvert.DeserializeObject<List<InHistory>>(doJsonArr);
                var inFeeNo = from a in IpdList where a.IpdFlag == "Y" select a.FeeNo.Trim();
                if (inFeeNo.Count() > 0)
                    Response.Write(inFeeNo.First());
                else
                    Response.Write("NotInIpd");
            }
            catch
            {
                Response.Write("Error");
            }
            return new EmptyResult();
        }

        #endregion

        #region  指導員

        public ActionResult SetGuider(string costcenter)
        {//設定指導員頁面     
            string JsonStr = "";
            byte[] EmployeeByteCode = webService.GetSigner(costcenter);  //實際應該為 GetSigner ， GetUserList 為暫時的 ws  tag wawa edit
            if (EmployeeByteCode != null)
            {
                JsonStr = CompressTool.DecompressString(EmployeeByteCode);
                List<UserInfo> EmployeeDataList = JsonConvert.DeserializeObject<List<UserInfo>>(JsonStr);
                ViewData["EmployeeData"] = EmployeeDataList;
            }

            return View();
        }

        public string SetGuiderToSession(string GuiderID, string GuiderName)
        {//儲存指導者至現有 Session  
            if (Session["UserInfo"] != null)
            {
                UserInfo ui = (UserInfo)Session["UserInfo"];
                ui.Guider = GuiderID;
                Session["UserInfo"] = ui;

                return GuiderName + "_" + GuiderID;
            }

            return "N";
        }
        #endregion  指導員 end
    }
}