using Newtonsoft.Json;
using NIS.Data;
using NIS.Models;
using NIS.UtilTool;
using System.Net;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web.Mvc;
using System.Linq;
using Com.Mayaminer;
using static NIS.Models.VitalSign;
using System.Text.RegularExpressions;
using System.Globalization;
using NIS.Models.DBModel;
using System.Xml;

namespace NIS.Controllers
{

    #region VitalSign Object

    public enum VitalSignDataField
    {
        Item = 0,
        Part = 1,
        Record = 2,
        Reason = 3,
        Memo = 4,
        OtherMemo = 5,
        CreateUser = 6,
        CreateDate = 7,
        ModifyUser = 8,
        ModifyDate = 9,
        Plan = 10,
        vstype = 11,
        abnormal_flag = 12,
        color = 13
    }
    public enum RESPONSE_STATUS
    {
        SUCCESS = 0,
        ERROR = 1,
        EXCEPTION = 2
    }
    public class RESPONSE_MSG
    {
        /// <summary> 處理狀態 </summary>
        public RESPONSE_STATUS status { set; get; }

        /// <summary> 傳回訊息或內容 </summary>
        public string message { set; get; }

        /// <summary> 附帶物件 </summary>
        public object attachment { set; get; }

        /// <summary> 取得序列化結果 </summary>
        public string get_json()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class ICUData
    {
        public string T_Result { set; get; }
        public string VS_Date { set; get; }
        /// <summary>心跳</summary>
        public string mp { set; get; }
        /// <summary>心跳量測部位</summary>
        public string mp_part { set; get; }
        /// <summary>心跳異常值判斷</summary>
        public string mp_num_abnormal { set; get; }
        /// <summary>呼吸</summary>
        public string bf { set; get; }
        /// <summary>呼吸異常值判斷</summary>
        public string bf_num_abnormal { set; get; }
        /// <summary>血壓</summary>
        public string bp { set; get; }
        /// <summary>血壓量測部位</summary>
        public string bp_part { set; get; }
        /// <summary>血壓異常值判斷</summary>
        public string bp_num_abnormal { set; get; }
        /// <summary>血氧</summary>
        public string sp { set; get; }
        /// <summary>血氧量測部位</summary>
        public string sp_part { set; get; }
        /// <summary>血氧異常值判斷</summary>
        public string sp_num_abnormal { set; get; }
        /// <summary>中心靜脈壓CVP</summary>
        public string cv1 { set; get; }
        /// <summary>中心靜脈壓CVP異常值判斷</summary>
        public string cv1_num_abnormal { set; get; }
        /// <summary>顱內壓</summary>
        public string ic1 { set; get; }
        /// <summary>顱內壓異常值判斷</summary>
        public string ic1_num_abnormal { set; get; }
        /// <summary>CPP</summary>
        public string cpp { set; get; }
        /// <summary>CPP異常值判斷</summary>
        public string cpp_num_abnormal { set; get; }
        /// <summary>動脈血壓</summary>
        public string abp { set; get; }
        /// <summary>動脈血壓異常值判斷</summary>
        public string abp_num_abnormal { set; get; }
        /// <summary>肺動脈壓</summary>
        public string pa { set; get; }
        /// <summary>肺動脈壓異常值判斷</summary>
        public string pa_num_abnormal { set; get; }
        /// <summary>PCWP</summary>
        public string pcwp { set; get; }
        /// <summary>PCWP異常值判斷</summary>
        public string pcwp_num_abnormal { set; get; }
        /// <summary>ETCO2</summary>
        public string etco { set; get; }
        /// <summary>ETCO2異常值判斷</summary>
        public string etco_num_abnormal { set; get; }
        ///// <summary>PICCO血流動力</summary>
        //public string picco { set; get; }
        ///// <summary>PICCO血流動力異常值判斷</summary>
        //public string picco_num_abnormal { set; get; }
        /// <summary>已使用(已帶入過)</summary>
        public bool used { set; get; } = false;
    }

    public class VitalSignData
    {
        public string vs_item { set; get; }
        public string vs_part { set; get; }
        public string vs_record { set; get; }
        public string vs_reason { set; get; }
        public string vs_memo { set; get; }
        public string vs_other_memo { set; get; }
        public string create_user { set; get; }
        public string create_date { set; get; }
        public string modify_user { set; get; }
        public string modify_date { set; get; }
        public string vs_type { set; get; }
        public string plan { set; get; }
        public string color { get; set; }

        /// <summary> 回傳VitalSignData </summary>
        public static VitalSignData getVSItem(List<VitalSignData> VSDataList, string vs_item)
        {
            VitalSignData tmpData = null;
            for (int i = 0; i <= VSDataList.Count - 1; i++)
            {
                if (VSDataList[i].vs_item == vs_item)
                {
                    tmpData = VSDataList[i];
                    if (VSDataList[i].plan == "Y")
                    {
                        tmpData.plan = "checked";
                    }

                }

            }

            return tmpData;
        }

        public static void chkVSItemNull(ref VitalSignData vsd)
        {
            if (vsd.vs_item == null)
                vsd.vs_item = "";
            if (vsd.vs_part == null)
                vsd.vs_part = "";
            if (vsd.vs_reason == null)
                vsd.vs_reason = "";
            if (vsd.vs_record == null)
                vsd.vs_record = "";
            if (vsd.vs_memo == null)
                vsd.vs_memo = "";
            if (vsd.vs_other_memo == null)
                vsd.vs_other_memo = "";
            if (vsd.create_user == null)
                vsd.create_user = "";
            if (vsd.modify_user == null)
                vsd.modify_user = "";
            if (vsd.vs_type == null)
                vsd.vs_type = "";
            if (vsd.color == null)
                vsd.color = "black";
        }

        public static string getItem(List<VitalSignData> VSDataList, string Item, VitalSignDataField Field)
        {
            List<string> rtStr = new List<string>();
            for (int i = 0; i <= VSDataList.Count - 1; i++)
            {
                if (VSDataList[i].vs_item.Trim() == Item.Trim() || Item == "")
                {
                    switch (Field)
                    {
                        case VitalSignDataField.Record:
                            rtStr.Add(VSDataList[i].vs_record);
                            break;
                        case VitalSignDataField.Part:
                            rtStr.Add(VSDataList[i].vs_part);
                            break;
                        case VitalSignDataField.Memo:
                            rtStr.Add(VSDataList[i].vs_memo);
                            break;
                        case VitalSignDataField.OtherMemo:
                            rtStr.Add(VSDataList[i].vs_other_memo);
                            break;
                        case VitalSignDataField.Reason:
                            rtStr.Add(VSDataList[i].vs_reason);
                            break;
                        case VitalSignDataField.CreateUser:
                            rtStr.Add(VSDataList[i].create_user);
                            break;
                        case VitalSignDataField.CreateDate:
                            rtStr.Add(VSDataList[i].create_date);
                            break;
                        case VitalSignDataField.ModifyUser:
                            rtStr.Add(VSDataList[i].modify_user);
                            break;
                        case VitalSignDataField.ModifyDate:
                            rtStr.Add(VSDataList[i].modify_date);
                            break;
                        case VitalSignDataField.Plan:
                            rtStr.Add(VSDataList[i].plan);
                            break;
                        case VitalSignDataField.abnormal_flag:
                            rtStr.Add(VSDataList[i].color == "black" ? "N" : "Y");
                            break;
                        case VitalSignDataField.color:
                            rtStr.Add(VSDataList[i].color);
                            break;
                    }
                }
            }
            string[] tmpStrArr = rtStr.ToArray();
            return string.Join("<br />", tmpStrArr);
        }

        public VitalSignData(
            string vs_item = "", string vs_part = "", string vs_record = "",
            string vs_reason = "", string vs_memo = "", string vs_other_memo = "", string create_user = "",
            string create_date = "", string modify_user = "", string modify_date = "", string vs_type = "", string plan = "", string color = "black")
        {
            this.vs_item = vs_item;
            this.vs_part = vs_part;
            this.vs_record = vs_record;
            this.vs_reason = vs_reason;
            this.vs_memo = vs_memo;
            this.vs_other_memo = vs_other_memo;
            this.create_user = create_user;
            this.create_date = create_date;
            this.modify_user = modify_user;
            this.modify_date = modify_date;
            this.vs_type = vs_type;
            this.plan = plan;
            this.color = color;
        }

        /// <summary>
        /// 清除資料
        /// </summary>
        public void Clear()
        {
            this.vs_item = string.Empty;
            this.vs_part = string.Empty;
            this.vs_record = string.Empty;
            this.vs_reason = string.Empty;
            this.vs_memo = string.Empty;
            this.vs_other_memo = string.Empty;
            this.create_user = string.Empty;
            this.create_date = string.Empty;
            this.modify_user = string.Empty;
            this.modify_date = string.Empty;
            this.color = string.Empty;
        }
    }

    /// <summary> 生命徵象資料集 </summary>
    public class VitalSignDataList
    {
        public string vsid { set; get; }
        public string recordtime { set; get; }
        public List<VitalSignData> DataList { set; get; }

        public VitalSignDataList()
        {
            this.DataList = new List<VitalSignData>();
        }

        /// <summary>
        /// 清除資料
        /// </summary>
        public void Clear()
        {
            this.vsid = string.Empty;
            this.DataList.Clear();
        }

    }
    public class TPR_Data
    {
        public string IsFinish { set; get; }
        public string MachineUF { set; get; }
        public string FoodWeight { set; get; }
        public string NSWeight { set; get; }
        public string BloodWeight { set; get; }
        public string OtherAddWeight { set; get; }
        public DateTime BeginDateTime { set; get; }
        public DateTime FinishDateTime { set; get; }
    }
    public class Machine_DataList
    {
        public string ID { set; get; }
        /// <summary>ID</summary>
        public string OP_ID { set; get; }
        /// <summary>醫護人員ID</summary>
        public string PATIENT_ID { set; get; }
        /// <summary>病人ID</summary>
        public string PATIENT_NAME { set; get; }
        /// <summary>病人姓名</summary>
        public string DATA_TIME { set; get; }
        /// <summary>資料輸入時間</summary>
        public string UPLOAD_TIME { set; get; }
        /// <summary>上傳時間</summary>
        public string PAIN { set; get; }
        /// <summary>疼痛</summary>
        public string BLOODSUGAR { set; get; }
        /// <summary>瞳孔大小-右眼</summary>
        public string CVP { set; get; }
        /// <summary>瞳孔大小-左眼</summary>
        public string AG { set; get; }
        /// <summary>瞳孔反射-右眼</summary>
        public string HC { set; get; }
        /// <summary>瞳孔反射-左眼</summary>
        public string FOOD { set; get; }
        /// <summary>肌肉力量-右上肢</summary>
        public string IVF { set; get; }
        /// <summary>肌肉力量-左上肢</summary>
        public string TPN { set; get; }
        /// <summary>肌肉力量-右下肢</summary>
        public string BT { set; get; }
        /// <summary>肌肉力量-左下肢</summary>
        public string URINE { set; get; }
        /// <summary>排便性狀</summary>
        public string STOOL { set; get; }
        /// <summary>排便顏色</summary>
        public string DRAIN { set; get; }
        /// <summary>意識-眼睛</summary>
        public string BLOST { set; get; }
        /// <summary>意識-回應</summary>
        public string OUTPUT_IRRIG { set; get; }
        /// <summary>意識-動作</summary>
        /// <summary>
        /// 清除資料
        /// </summary>
        public void Clear()
        {
            this.ID = string.Empty;
        }
    }
    public class ICUDataWhereList
    {
        /// <summary>開始日期</summary>
        public string Start_Date { get; set; }
        /// <summary>結束時間</summary>
        public string Start_Time { get; set; }
        /// <summary>結束日期</summary>
        public string End_Date { get; set; }
        /// <summary>結束時間</summary>
        public string End_Time { get; set; }
    }

    #endregion
    //Blood GAS

    public class BloodGas
    {
        /// <summary>序號</summary>
        public string USEQ { get; set; }
        /// <summary>項目</summary>
        public string ITEMNAME { get; set; }
        /// <summary>紀錄值</summary>
        public string ITEMVALUE { get; set; }
        /// <summary>日期</summary>
        public string RDATE { get; set; }
        /// <summary>時間</summary>
        public string RTIME { get; set; }

    }

    public class BloodGasData
    {
        /// <summary>序號</summary>
        public string VS_ID { get; set; }

        public string CREATE_DATE { get; set; }

        public List<BloodGas> BG_List { set; get; }
    }


    public class BloodGasView
    {
        /// <summary>序號</summary>
        public string VS_ID { get; set; }

        /// <summary>建立日期</summary>
        public string CREATE_DATE { get; set; }

        public string pH { get; set; }

        public string pCO2 { get; set; }

        public string pO2 { get; set; }

        public string HCO3_act { get; set; }

        public string HCO3_std { get; set; }

        public string BE_B { get; set; }

        public string BE_ecf { get; set; }

        public string ctO2 { get; set; }

        public string tHb { get; set; }

        public string SO2 { get; set; }

        public string FO2Hb { get; set; }

        public string FCOHb { get; set; }

        public string FMetHb { get; set; }

        public string FHHB { get; set; }

        public string Na { get; set; }

        public string K { get; set; }

        public string Ca { get; set; }

        public string Ca_7 { get; set; }

        public string CI { get; set; }

        public string source { get; set; }
    }



    public class VitalSignController : BaseController
    {
        private CommData cd;    //常用資料Module
        private string iniDir = string.Empty;
        private DBConnector link;
        private DataTable dt_prn = new DataTable();
        private CommonMedicine cm;
        private BloodSugarAndInsulin bai;
        private CVVH_Data cvvh_d;
        private LogTool log;
        IOManager iom = new IOManager();


        //建構式
        public VitalSignController()
        {   //建立一般資料物件
            this.cd = new CommData();
            this.link = new DBConnector();
            this.cm = new CommonMedicine();
            this.bai = new BloodSugarAndInsulin();
            this.cvvh_d = new CVVH_Data();
            this.log = new LogTool();
        }

        #region VitalSign查詢
        //VitalSign查詢
        public ActionResult VitalSign_Index(string starttime, string endtime)
        {
            try
            {
                string start = DateTime.Now.ToString("yyyy/MM/dd 00:00");
                string end = DateTime.Now.AddMinutes(2).ToString("yyyy/MM/dd HH:mm");
                string  tmp_item = "", tmp_value = "";
                if (starttime != null && endtime != null)
                {
                    start = starttime;
                    end = endtime;
                }

                //加入欄位設定
                string[] func_list = null;
                string sqlstr = " SELECT * FROM NIS_SYS_USER_VSOPTIION WHERE EMP_NO = '" + base.ptinfo.FeeNo + "' ";

                DataTable Dt = this.link.DBExecSQL(sqlstr);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        func_list = Dt.Rows[i]["func_list"].ToString().Split('|');
                    }
                }

                if (func_list == null || func_list.Length == 0)
                    func_list = "bh|bw".Split('|');

                ViewData["func_list"] = func_list;
                ViewBag.start = start;
                ViewBag.end = end;

                //確認是否有病人資料(有選取病人)
                if (Session["PatInfo"] != null)
                {
                    //宣告必須要使用到的變數
                    //IDataReader reader = null;
                    List<VitalSignDataList> vsList = new List<VitalSignDataList>();
                    List<string[]> vsId = new List<string[]>();
                    VitalSignDataList vsdl = null;
                    //取得異常查檢表
                    DataTable dt_check = Get_Check_Abnormal_dt();

                    //取得vs_id
                    /*string*/
                    sqlstr = " select CREATE_DATE,vs_id from data_vitalsign where fee_no = '" + ptinfo.FeeNo + "' ";
                    sqlstr += "and create_date between to_date('" + start + "','yyyy/MM/dd hh24:mi:ss') and to_date('" + end + "','yyyy/MM/dd hh24:mi:ss') AND DEL is null ";
                    sqlstr += "group by CREATE_DATE,vs_id order by CREATE_DATE";

                    Dt = link.DBExecSQL(sqlstr);
                    if (Dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            vsId.Add(new string[] { Dt.Rows[i]["vs_id"].ToString().Trim(), Dt.Rows[i]["CREATE_DATE"].ToString() });
                        }
                    }

                    string color = "black";
                    // 開始處理資料
                    for (int i = 0; i <= vsId.Count - 1; i++)
                    {
                        //初始化資料
                        vsdl = new VitalSignDataList();

                        sqlstr = " select vsd.*, to_char(CREATE_DATE,'yyyy/MM/dd hh24:mi:ss') as m_date  from data_vitalsign vsd ";
                        sqlstr += " where fee_no ='" + ptinfo.FeeNo + "' and vs_id = '" + vsId[i][0] + "' ";
                        sqlstr += " and create_date = to_date('" + Convert.ToDateTime(vsId[i][1]).ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/MM/dd hh24:mi:ss')";
                        //sqlstr += " and vsd.vs_record is not null ";
                        vsdl.vsid = vsId[i][0];
                        Dt = this.link.DBExecSQL(sqlstr);
                        if (Dt.Rows.Count > 0)
                        {
                            for (int j = 0; j < Dt.Rows.Count; j++)
                            {
                                tmp_item = tmp_value = "";

                                tmp_value = Dt.Rows[j]["vs_record"].ToString().Trim();
                                //tmp = reader["vs_item"].ToString().Trim();
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
                    }

                    ViewBag.ck_type = base.get_check_type(ptinfo); //取得生命徵象異常年紀代號
                    ViewBag.dt_check = dt_check;
                    ViewBag.age = ptinfo.Age;
                    ViewData["VSData"] = vsList;
                    ViewBag.userno = userinfo.EmployeesNo;
                }
                //return View();
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



        //new  整合圍長/picco 頁面
        public ActionResult VitalSign_group(string group, string value_list)
        {
            string id = "", tmp_item = "", tmp_value = "", tmp_color = "", tmp_unit = "", tmp = "";

            //取得異常查檢表
            DataTable dt_check = Get_Check_Abnormal_dt();

            if (group.ToUpper() == "GW")
            {
                id = "gtwl,gthr,gtbu,gtbl,gthl,gtlf,gtrf,gtlua,gtrua,gtlt,gtrt,gtll,gtrl,gtla,gtra";
                ViewBag.title = "圍長";
                ViewBag.unitCol = "display:none";
            }
            else if (group.ToUpper() == "PICCO")
            {
                id = "picco_co,picco_ci,picco_sv,picco_svi,picco_svr,picco_svri,picco_svv,picco_scvo2";
                ViewBag.title = "血流動力";
                ViewBag.unitCol = "";
            }


            string[] i_list = id.Split(',');
            string[] v_list = value_list.Split(',');

            DataTable dt = new DataTable();
            dt.Columns.Add("item");
            dt.Columns.Add("value");
            dt.Columns.Add("color");
            dt.Columns.Add("unit");

            for (int i = 0; i < v_list.Length; i++)
            {
                if (v_list[i] != "")
                {
                    tmp_item = tmp_value = tmp_color = tmp_unit = "";

                    tmp_value = v_list[i].ToString();
                    tmp = i_list[i].ToString();
                    tmp_item = set_name(tmp);

                    if (group.ToUpper() == "PICCO")
                    {
                        tmp_unit = set_picco_unit(tmp);
                        tmp_color = check_abnormal_color(dt_check, tmp, tmp_value);
                        tmp_item = set_picco_name(tmp);
                    }

                    DataRow r = dt.NewRow();
                    r["item"] = tmp_item;
                    r["value"] = tmp_value;
                    r["color"] = tmp_color;
                    r["unit"] = tmp_unit;
                    dt.Rows.Add(r);
                }
            }


            ViewBag.dt = dt;
            return View();
        }

        #region VitalSign查詢 OLD (圍長/picco)

        //VitalSign查詢_圍長詳細資料頁面 OLD
        public ActionResult VitalSign_GW(string value_list)
        {
            string id = "gtwl,gthr,gtbu,gtbl,gthl,gtlf,gtrf,gtlua,gtrua,gtlt,gtrt,gtll,gtrl,gtla,gtra";
            string[] i_list = id.Split(',');
            string[] v_list = value_list.Split(',');
            DataTable dt = new DataTable();
            dt.Columns.Add("item");
            dt.Columns.Add("value");
            for (int i = 0; i < v_list.Length; i++)
            {
                if (v_list[i] != "")
                {
                    DataRow r = dt.NewRow();
                    r["item"] = set_name(i_list[i]);
                    r["value"] = v_list[i];
                    dt.Rows.Add(r);
                }
            }
            ViewBag.dt = dt;
            return View();
        }

        //VitalSign查詢_血流動力 OLD
        public ActionResult VitalSign_picco(string value_list)
        {
            string id = "picco_co,picco_ci,picco_sv,picco_svi,picco_svr,picco_svri,picco_svv,picco_scvo2";
            string[] i_list = id.Split(',');
            string[] v_list = value_list.Split(',');
            //取得異常查檢表
            DataTable dt_check = Get_Check_Abnormal_dt();
            DataTable dt = new DataTable();
            dt.Columns.Add("item");
            dt.Columns.Add("value");
            dt.Columns.Add("color");
            dt.Columns.Add("unit");
            string tmp_item = "", tmp_value = "", tmp_color = "", tmp_unit = "";
            for (int i = 0; i < v_list.Length; i++)
            {
                if (v_list[i] != "")
                {
                    tmp_item = i_list[i].ToString();
                    tmp_value = v_list[i].ToString();
                    tmp_color = check_abnormal_color(dt_check, tmp_item, tmp_value);
                    tmp_unit = set_picco_unit(tmp_item);
                    DataRow r = dt.NewRow();
                    r["item"] = set_picco_name(tmp_item);
                    r["value"] = tmp_value;
                    r["color"] = tmp_color;
                    r["unit"] = tmp_unit;
                    dt.Rows.Add(r);
                }
            }
            ViewBag.dt = dt;
            return View();
        }

        #endregion


        #region 查明細的周邊方法(圍長/picco)

        //取得圍長中文名稱
        private string set_name(string name)
        {
            string _name = "";
            switch (name)
            {
                case "gtwl":
                    _name = "腰圍";
                    break;
                case "gthr":
                    _name = "頭圍";
                    break;
                case "gtbu":
                    _name = "胸圍";
                    break;
                case "gtbl":
                    _name = "腹圍";
                    break;
                case "gthl":
                    _name = "臀圍";
                    break;
                case "gtlf":
                    _name = "左前臂";
                    break;
                case "gtrf":
                    _name = "右前臂";
                    break;
                case "gtlua":
                    _name = "左上臂";
                    break;
                case "gtrua":
                    _name = "右上臂";
                    break;
                case "gtlt":
                    _name = "左大腿";
                    break;
                case "gtrt":
                    _name = "右大腿";
                    break;
                case "gtll":
                    _name = "左小腿";
                    break;
                case "gtrl":
                    _name = "右小腿";
                    break;
                case "gtla":
                    _name = "左足踝";
                    break;
                case "gtra":
                    _name = "右足踝";
                    break;
                default:
                    _name = "";
                    break;
            }
            return _name;
        }

        //轉換picco名稱
        private string set_picco_name(string name)
        {
            string _name = "";
            switch (name)
            {
                case "picco_co":
                    _name = "CO";
                    break;
                case "picco_ci":
                    _name = "CI";
                    break;
                case "picco_sv":
                    _name = "SV";
                    break;
                case "picco_svi":
                    _name = "SVI";
                    break;
                case "picco_svr":
                    _name = "SVR";
                    break;
                case "picco_svri":
                    _name = "SVRI";
                    break;
                case "picco_svv":
                    _name = "SVV";
                    break;
                case "picco_scvo2":
                    _name = "ScvO2";
                    break;
                default:
                    _name = "";
                    break;
            }
            return _name;
        }

        //轉換picco單位
        private string set_picco_unit(string name)
        {
            string _name = "";
            switch (name)
            {
                case "picco_co":
                    _name = "L/min";
                    break;
                case "picco_ci":
                    _name = "L/min.m\u00B2";  //L/min.m2, \u00B2 = 上標2
                    break;
                case "picco_sv":
                    _name = "mL/beat";
                    break;
                case "picco_svi":
                    _name = "mL/beat/m\u00B2"; //mL/beat/m2, \u00B2 = 上標2
                    break;
                case "picco_svr":
                    _name = "dynes-sec/cm\u207B\u2075"; //dynes-sec/cm-5, \u207B = 上標-(負號), \u2075 = 上標5
                    break;
                case "picco_svri":
                    _name = "dynes-sec/cm\u207B\u2075/m\u00B2"; //dynes - sec / cm - 5 / m2
                    break;
                case "picco_svv":
                    _name = "%";
                    break;
                case "picco_scvo2":
                    _name = "%";
                    break;
                default:
                    _name = "";
                    break;
            }
            return " " + _name;
        }

        #endregion

        #endregion

        #region VitalSign拋轉介接

        //VitalSign拋轉介接
        public ActionResult VitalSign_Interfacing(string starttime, string endtime, string feeno)
        {

            string start = DateTime.Now.ToString("yyyy/MM/dd 00:00");
            string end = DateTime.Now.AddMinutes(2).ToString("yyyy/MM/dd HH:mm");
            if (starttime != null && endtime != null)
            {
                start = starttime;
                end = endtime;
            }
            ViewBag.start = start;
            ViewBag.end = end;
            ViewBag.feeno = feeno;

            byte[] VitalSignbyDateByteCode = webService.GetVitalSignbyDate(feeno, start, end, userinfo.EmployeesNo);

            if (VitalSignbyDateByteCode != null)
            {
                string VitalSignbyDateListJosnArr = NIS.UtilTool.CompressTool.DecompressString(VitalSignbyDateByteCode);
                List<VitalSignbyDate> VitalSignbyDateList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<VitalSignbyDate>>(VitalSignbyDateListJosnArr);
                ViewData["result"] = VitalSignbyDateList;
            }

            return View();
        }

        //儀器帶入生命徵象
        public ActionResult Device_Save(string date, string bp, string mp, string bt, string sp, string bh, string bw, string bf, string ps)
        {
            string vs_id = ptinfo.FeeNo + "_" + Convert.ToDateTime(date).ToString("yyyyMMddHHmm") + DateTime.Now.ToString("ssfff");
            insert_vs("bp", bp, date, vs_id);
            insert_vs("mp", mp, date, vs_id);
            insert_vs("bt", bt, date, vs_id);
            insert_vs("sp", sp, date, vs_id);
            insert_vs("bh", bh, date, vs_id);
            insert_vs("bw", bw, date, vs_id);
            insert_vs("bf", bf, date, vs_id);
            insert_vs("ps", ps, date, vs_id);
            return new EmptyResult();
        }

        private int insert_vs(string item, string value, string date, string vs_id)
        {
            if (value != "")
            {
                List<DBItem> vs_data = new List<DBItem>();
                vs_data.Add(new DBItem("fee_no", base.ptinfo.FeeNo, DBItem.DBDataType.String));
                vs_data.Add(new DBItem("vs_id", vs_id, DBItem.DBDataType.String));
                vs_data.Add(new DBItem("vs_item", item, DBItem.DBDataType.String));
                if (item == "ps")
                {
                    vs_data.Add(new DBItem("vs_part", "數字量表", DBItem.DBDataType.String));
                    vs_data.Add(new DBItem("vs_record", "(" + value + ")", DBItem.DBDataType.String));
                }
                else
                {
                    vs_data.Add(new DBItem("vs_record", value, DBItem.DBDataType.String));
                }
                vs_data.Add(new DBItem("create_date", Convert.ToDateTime(date).ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                vs_data.Add(new DBItem("modify_date", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                vs_data.Add(new DBItem("create_user", userinfo.EmployeesNo.Trim(), DBItem.DBDataType.String));
                vs_data.Add(new DBItem("modify_user", userinfo.EmployeesNo.Trim(), DBItem.DBDataType.String));
                vs_data.Add(new DBItem("plan", "N", DBItem.DBDataType.String));
                return this.link.DBExecInsert("data_vitalsign", vs_data);
            }
            else
                return 0;
        }

        #endregion

        #region ICU 資料處理
        public int RVaule(int s, int e)
        {
            Random rd = new Random(Guid.NewGuid().GetHashCode());
            int output = rd.Next(s, e);
            return output;
        }
        [HttpGet]
        public ActionResult ICUData(string feeno)
        {
            //DataTable dt_check = Get_Check_Abnormal_dt();
            //List<ICUData> ICUDataList = new List<ICUData>();
            //DateTime sdt = DateTime.Now;
            //ICUDataWhereList wherelist = new ICUDataWhereList();
            if (Session["PatInfo"] != null)
            {
                #region 假資料
                //for (int i = 0; i < 120; i++)
                //{
                //    ICUDataList.Add(new ICUData()
                //    {
                //        VS_Date = sdt.AddMinutes(i).ToString("HH:mm"),

                //        MP = RVaule(36, 40).ToString(),
                //        BF = RVaule(72, 110).ToString(),
                //        BP = RVaule(12, 30).ToString(),
                //        SP = RVaule(95, 180).ToString() + "/" + RVaule(70, 100).ToString(),
                //        CV1 = RVaule(85, 100).ToString(),
                //        IC1 = RVaule(85, 100).ToString(),
                //        CPP = RVaule(85, 100).ToString(),
                //        ABP = RVaule(85, 100).ToString(),
                //        PA = RVaule(85, 100).ToString(),
                //        PCWP = RVaule(85, 100).ToString(),
                //        ETCO = RVaule(85, 100).ToString(),
                //        ItemType = RVaule(1, 3).ToString()
                //    });
                //}

                #endregion
                //List<string> datetimeList = new List<string>();
                //DataTable dt = Get_ICU_Data(vs_date, start_date, start_time,wherelist.End_Date,end_time)
            }

            //ViewData["ICUDataList"] = ICUDataList;
            return View();
        }

        public ActionResult BloodGasData(string feeno)
        {
            //DataTable dt_check = Get_Check_Abnormal_dt();
            //List<ICUData> ICUDataList = new List<ICUData>();
            //DateTime sdt = DateTime.Now;
            //ICUDataWhereList wherelist = new ICUDataWhereList();
            if (Session["PatInfo"] != null)
            {
                #region 假資料
                //for (int i = 0; i < 120; i++)
                //{
                //    ICUDataList.Add(new ICUData()
                //    {
                //        VS_Date = sdt.AddMinutes(i).ToString("HH:mm"),

                //        MP = RVaule(36, 40).ToString(),
                //        BF = RVaule(72, 110).ToString(),
                //        BP = RVaule(12, 30).ToString(),
                //        SP = RVaule(95, 180).ToString() + "/" + RVaule(70, 100).ToString(),
                //        CV1 = RVaule(85, 100).ToString(),
                //        IC1 = RVaule(85, 100).ToString(),
                //        CPP = RVaule(85, 100).ToString(),
                //        ABP = RVaule(85, 100).ToString(),
                //        PA = RVaule(85, 100).ToString(),
                //        PCWP = RVaule(85, 100).ToString(),
                //        ETCO = RVaule(85, 100).ToString(),
                //        ItemType = RVaule(1, 3).ToString()
                //    });
                //}

                #endregion
                //List<string> datetimeList = new List<string>();
                //DataTable dt = Get_ICU_Data(vs_date, start_date, start_time,wherelist.End_Date,end_time)
            }

            //ViewData["ICUDataList"] = ICUDataList;
            return View();
        }

        private DataTable Get_ICU_Data(ICUDataWhereList wherelist)
        {
            DataTable dt = new DataTable();
            try
            {
                List<string> Where_list = new List<string>();
                string sql = "SELECT * FROM measuredata ";
                var chartNo = base.ptinfo.ChartNo.ToString();
                if (!string.IsNullOrEmpty(chartNo))
                    Where_list.Add("PATIENTID='" + chartNo + "'");
                if (!string.IsNullOrEmpty(wherelist.Start_Date))
                    Where_list.Add("DATADATE>='" + wherelist.Start_Date + wherelist.Start_Time + "'");
                if (!string.IsNullOrEmpty(wherelist.End_Date))
                    Where_list.Add("DATADATE<='" + wherelist.End_Date + wherelist.End_Time + "'");
                if (Where_list.Count > 0)
                {
                    sql += "where " + string.Join(" AND ", Where_list);
                }
                sql += " ORDER BY  datadate DESC";
                DataTable Dtt = link.DBExecSQL(sql);
                if (Dtt.Rows.Count > 0)
                {
                    dt = Dtt;
                }
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(),ex);
            }
            return dt;
        }

        /// <summary>
        /// 取得 VitalSignData
        /// </summary>
        /// <param name="fee_no">批價序號</param>
        /// <param name="wherelist">查詢條件</param>
        /// <param name="where_vs_item">項目條件(VS_ITEM)</param>
        /// <param name="where_vs_type">類型條件(VS_TYPE)</param>
        /// <returns>DB_NIS_VITALSIGN List</returns>
        private List<DB_NIS_VITALSIGN> Get_DB_NIS_VITALSIGN(string fee_no, ICUDataWhereList where_list, string where_vs_item = "", string where_vs_type = "")
        {
            List<DB_NIS_VITALSIGN> vs_list = new List<DB_NIS_VITALSIGN>();
            DataTable dt = new DataTable();
            List<string> Where_list = new List<string>();

            string sql = "SELECT * FROM DATA_VITALSIGN ";
            if (!string.IsNullOrEmpty(fee_no))
                Where_list.Add("FEE_NO='" + fee_no + "'");
            if (!string.IsNullOrEmpty(where_list.Start_Date))
                Where_list.Add("CREATE_DATE BETWEEN TO_DATE('" + where_list.Start_Date + where_list.Start_Time + "', 'yyyymmddhh24mi' )");
            if (!string.IsNullOrEmpty(where_list.End_Date))
                Where_list.Add("TO_DATE('" + where_list.End_Date + where_list.End_Time + "', 'yyyymmddhh24mi' )");
            if (!string.IsNullOrEmpty(where_vs_item))
                Where_list.Add("VS_ITEM ='" + where_vs_item + "'");
            if (!string.IsNullOrEmpty(where_vs_type))
                Where_list.Add("VS_TYPE ='" + where_vs_type + "'");
            if (Where_list.Count > 0)
            {
                sql += "WHERE " + string.Join(" AND ", Where_list);
            }

            sql += " AND DEL IS NULL ORDER BY CREATE_DATE DESC";
            sql = string.Format(sql, fee_no);
            dt = link.DBExecSQL(sql);
            if (dt != null && dt.Rows.Count > 0)
            {
                vs_list = (List<DB_NIS_VITALSIGN>)dt.ToList<DB_NIS_VITALSIGN>();
            }
            return vs_list;
        }

        /// <summary>
        /// 取得ICU Data Json
        /// </summary>
        /// <param name="start_date">開始日期</param>
        /// <param name="start_time">開始時間</param>
        /// <param name="end_date">結束日期</param>
        /// <param name="end_time">結束時間</param>
        /// <returns>Json</returns>
        public JsonResult ICUDataJson(string start_date, string start_time, string end_date, string end_time)
        {
            ICUDataWhereList whereList = new ICUDataWhereList();
            whereList.Start_Date = start_date.Replace("/", "");
            whereList.Start_Time = start_time.Replace(":", "");
            whereList.End_Date = end_date.Replace("/", "");
            whereList.End_Time = end_time.Replace(":", "");
            DataTable dt_check = Get_Check_Abnormal_dt();
            string type = get_check_type(ptinfo);
            List<ICUData> ICUDataList = new List<ICUData>();
            DateTime sdt = DateTime.Now;

            if (Session["PatInfo"] != null)
            {
                DataTable dt = Get_ICU_Data(whereList);
                #region 塞資料
                if (dt != null && dt.Rows.Count > 0)
                {

                    string start_datetime = start_date + " " + start_time;
                    List<string> datetimeList = new List<string>();
                    datetimeList = dt.AsEnumerable().ToList().Select(x => x["datadate"].ToString()).Distinct().ToList();

                    foreach (string datatime in datetimeList)
                    {
                        List<DataRow> dtList = dt.AsEnumerable().ToList().FindAll(x => x["datadate"].ToString() == datatime);
                        DateTime tempDate = DateTime.ParseExact(datatime, "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.None);
                        string temp_date = tempDate.ToString("yyyy/MM/dd HH:mm:ss");

                        List<MEASUREDATA> m_list = new List<MEASUREDATA>();
                        foreach (DataRow row in dtList)
                        {
                            MEASUREDATA m_data = new MEASUREDATA();
                            m_data.PATIENTID = string.IsNullOrEmpty(row.Field<string>("PATIENTID")) ? "" : row.Field<string>("PATIENTID").ToString();
                            m_data.DATADATE = string.IsNullOrEmpty(row.Field<string>("DATADATE")) ? "" : row.Field<string>("DATADATE").ToString();
                            m_data.OBSERVATIONID = string.IsNullOrEmpty(row.Field<string>("OBSERVATIONID")) ? "" : row.Field<string>("OBSERVATIONID").ToString();
                            m_data.VALUE = string.IsNullOrEmpty(row.Field<string>("VALUE")) ? "" : row.Field<string>("VALUE").ToString();
                            m_data.UNIT = string.IsNullOrEmpty(row.Field<string>("UNIT")) ? "" : row.Field<string>("UNIT").ToString();
                            m_data.LOCATION = string.IsNullOrEmpty(row.Field<string>("LOCATION")) ? "" : row.Field<string>("LOCATION").ToString();
                            m_data.STATUS = string.IsNullOrEmpty(row.Field<string>("STATUS")) ? "" : row.Field<string>("STATUS").ToString();
                            m_list.Add(m_data);
                        }
                        ICUData icu_data = new ICUData();
                        TimeSpan ts = new TimeSpan(Convert.ToDateTime(tempDate.ToString("yyyy/MM/dd HH:mm:ss")).Ticks - Convert.ToDateTime(start_datetime).Ticks);
                        icu_data.T_Result = Convert.ToString(ts.TotalMinutes);
                        icu_data.VS_Date = temp_date;

                        icu_data.mp = m_list.Exists(ml => ml.OBSERVATIONID == "HR") ? m_list.Find(ml => ml.OBSERVATIONID == "HR").VALUE : "";
                        icu_data.mp_num_abnormal = Check_Num_Abnormal("mpl_" + type, "mph_" + type, icu_data.mp, dt_check) == "Y" ? "Y" : "N";

                        icu_data.bf = m_list.Exists(ml => ml.OBSERVATIONID == "RR") ? m_list.Find(ml => ml.OBSERVATIONID == "RR").VALUE : "";
                        icu_data.bf_num_abnormal = Check_Num_Abnormal("bfl_" + type, "bfh_" + type, icu_data.bf, dt_check) == "Y" ? "Y" : "N";

                        string nbp_s = m_list.Exists(ml => ml.OBSERVATIONID.StartsWith("NBP") && ml.OBSERVATIONID.EndsWith("-S")) ? m_list.Find(ml => ml.OBSERVATIONID.StartsWith("NBP") && ml.OBSERVATIONID.EndsWith("-S")).VALUE : "";
                        string nbp_d = m_list.Exists(ml => ml.OBSERVATIONID.StartsWith("NBP") && ml.OBSERVATIONID.EndsWith("-D")) ? m_list.Find(ml => ml.OBSERVATIONID.StartsWith("NBP") && ml.OBSERVATIONID.EndsWith("-D")).VALUE : "";
                        if (string.IsNullOrEmpty(nbp_s) && string.IsNullOrEmpty(nbp_d))
                        {
                            icu_data.bp = "";
                        }
                        else
                        {
                            icu_data.bp = nbp_s + "/" + nbp_d;
                        }
                        icu_data.bp_num_abnormal = Check_Num_Abnormal("bpls_" + type, "bphs_" + type, nbp_s, dt_check) == "Y" || Check_Num_Abnormal("bpld_" + type, "bphd_" + type, nbp_d, dt_check) == "Y" ? "Y" : "N";

                        icu_data.sp = m_list.Exists(ml => ml.OBSERVATIONID.Contains("SPO2-%")) ? m_list.Find(ml => ml.OBSERVATIONID.Contains("SPO2-%")).VALUE : "";
                        icu_data.sp_num_abnormal = Check_Num_Abnormal("spl", "", icu_data.sp, dt_check) == "Y" ? "Y" : "N";

                        icu_data.cv1 = m_list.Exists(ml => ml.OBSERVATIONID.Contains("CVP")) ? m_list.Find(ml => ml.OBSERVATIONID.Contains("CVP")).VALUE : "";
                        //過濾 中心靜脈壓CVP(mmHg)小於等於0的數值
                        int cv1_value = 0;
                        if (Int32.TryParse(icu_data.cv1, out cv1_value))
                        {
                            if (cv1_value <= 0)
                            {
                                icu_data.cv1 = "";
                            }
                        }
                        icu_data.cv1_num_abnormal = Check_Num_Abnormal("cv1_l", "cv1_h", icu_data.cv1, dt_check) == "Y" ? "Y" : "N";

                        icu_data.ic1 = m_list.Exists(ml => ml.OBSERVATIONID.Contains("ICP")) ? m_list.Find(ml => ml.OBSERVATIONID.Contains("ICP")).VALUE : "";
                        icu_data.ic1_num_abnormal = Check_Num_Abnormal("ic1_l", "", icu_data.ic1, dt_check) == "Y" ? "Y" : "N";

                        icu_data.cpp = m_list.Exists(ml => ml.OBSERVATIONID.Contains("CCP")) ? m_list.Find(ml => ml.OBSERVATIONID.Contains("CCP")).VALUE : "";
                        icu_data.cpp_num_abnormal = "N";

                        string abp_s = m_list.Exists(ml => ml.OBSERVATIONID.StartsWith("AR") && ml.OBSERVATIONID.EndsWith("-S")) ? m_list.Find(ml => ml.OBSERVATIONID.StartsWith("AR") && ml.OBSERVATIONID.EndsWith("-S")).VALUE : "";
                        string abp_d = m_list.Exists(ml => ml.OBSERVATIONID.StartsWith("AR") && ml.OBSERVATIONID.EndsWith("-D")) ? m_list.Find(ml => ml.OBSERVATIONID.StartsWith("AR") && ml.OBSERVATIONID.EndsWith("-D")).VALUE : "";
                        if (string.IsNullOrEmpty(abp_s) && string.IsNullOrEmpty(abp_d))
                        {
                            icu_data.abp = "";
                        }
                        else
                        {
                            icu_data.abp = abp_s + "/" + abp_d;
                        }
                        icu_data.abp_num_abnormal = "N";

                        string pa_s = m_list.Exists(ml => ml.OBSERVATIONID.StartsWith("PA") && ml.OBSERVATIONID.EndsWith("-S")) ? m_list.Find(ml => ml.OBSERVATIONID.StartsWith("PA") && ml.OBSERVATIONID.EndsWith("-S")).VALUE : "";
                        string pa_d = m_list.Exists(ml => ml.OBSERVATIONID.StartsWith("PA") && ml.OBSERVATIONID.EndsWith("-D")) ? m_list.Find(ml => ml.OBSERVATIONID.StartsWith("PA") && ml.OBSERVATIONID.EndsWith("-D")).VALUE : "";
                        if (string.IsNullOrEmpty(pa_s) && string.IsNullOrEmpty(pa_d))
                        {
                            icu_data.pa = "";
                        }
                        else
                        {
                            icu_data.pa = pa_s + "/" + pa_d;
                        }
                        icu_data.pa_num_abnormal = Check_Num_Abnormal("pals", "pahs", pa_s, dt_check) == "Y" || Check_Num_Abnormal("pald", "pahd", pa_d, dt_check) == "Y" ? "Y" : "N";

                        icu_data.pcwp = m_list.Exists(ml => ml.OBSERVATIONID == "PAW") ? m_list.Find(ml => ml.OBSERVATIONID == "PAW").VALUE : "";
                        icu_data.pcwp_num_abnormal = "N";

                        icu_data.etco = m_list.Exists(ml => ml.OBSERVATIONID == "CO2-EX") ? m_list.Find(ml => ml.OBSERVATIONID == "CO2-EX").VALUE : "";
                        icu_data.etco_num_abnormal = "N";

                        ICUDataList.Add(icu_data);
                    }

                    //取得已帶入資料(VS_ITEM=mp AND VS_TYPE=ICU)並將其排除
                    List<DB_NIS_VITALSIGN> db_vsData_mp = Get_DB_NIS_VITALSIGN(ptinfo.FeeNo.ToString(), whereList, "mp", "ICU");
                    foreach (DB_NIS_VITALSIGN dvs in db_vsData_mp)
                    {
                        dvs.VS_RECORD = string.IsNullOrEmpty(dvs.VS_RECORD) ? "" : dvs.VS_RECORD;
                        ICUData sel_used_data = ICUDataList.Find(idl => Convert.ToDateTime(idl.VS_Date).Ticks == Convert.ToDateTime(dvs.CREATE_DATE).Ticks && idl.mp.Trim() == dvs.VS_RECORD.Trim());
                        if (sel_used_data != null)
                        {
                            //標記已使用(已帶入過)
                            sel_used_data.used = true;
                        }
                    }
                }
                #endregion
            }
            return new JsonResult { Data = ICUDataList, MaxJsonLength = int.MaxValue, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpPost]
        public JsonResult ICUDataSave(List<ICUData> icu_datas)
        {
            RESPONSE_MSG rm = new RESPONSE_MSG();
            bool success = false;
            int erow = 0;
            int i = 0;
            try
            {
                //link.DBOpen(true);
                int inster_part_total = 0;
                foreach (ICUData icu_data in icu_datas)
                {
                    //icu_data.VS_Date = Convert.ToDateTime(icu_data.VS_Date).ToString("yyyy/MM/dd HH:mm:00");  // 統一時間格式 不記錄秒數
                    string vs_id = base.ptinfo.FeeNo + "_" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + i.ToString().PadLeft(3, '0');
                    InsterDB_NIS_VITALSIGN(vs_id, "mp", string.IsNullOrEmpty(icu_data.mp) ? " " : icu_data.mp, icu_data.VS_Date, icu_data.mp_num_abnormal, ref erow, icu_data.mp_part);
                    InsterDB_NIS_VITALSIGN(vs_id, "bf", string.IsNullOrEmpty(icu_data.bf) ? " " : icu_data.bf, icu_data.VS_Date, icu_data.bf_num_abnormal, ref erow);
                    InsterDB_NIS_VITALSIGN(vs_id, "bp", string.IsNullOrEmpty(icu_data.bp) ? "|" : icu_data.bp.Replace("/", "|"), icu_data.VS_Date, icu_data.bp_num_abnormal, ref erow, icu_data.bp_part);
                    InsterDB_NIS_VITALSIGN(vs_id, "sp", string.IsNullOrEmpty(icu_data.sp) ? " " : icu_data.sp, icu_data.VS_Date, icu_data.sp_num_abnormal, ref erow, icu_data.sp_part);
                    InsterDB_NIS_VITALSIGN(vs_id, "cv1", string.IsNullOrEmpty(icu_data.cv1) ? " " : icu_data.cv1, icu_data.VS_Date, icu_data.cv1_num_abnormal, ref erow);
                    InsterDB_NIS_VITALSIGN(vs_id, "ic1", string.IsNullOrEmpty(icu_data.ic1) ? " " : icu_data.ic1, icu_data.VS_Date, icu_data.ic1_num_abnormal, ref erow);
                    InsterDB_NIS_VITALSIGN(vs_id, "cpp", string.IsNullOrEmpty(icu_data.cpp) ? " " : icu_data.cpp, icu_data.VS_Date, icu_data.cpp_num_abnormal, ref erow);
                    InsterDB_NIS_VITALSIGN(vs_id, "abp", string.IsNullOrEmpty(icu_data.abp) ? "|" : icu_data.abp.Replace("/", "|"), icu_data.VS_Date, icu_data.abp_num_abnormal, ref erow);
                    InsterDB_NIS_VITALSIGN(vs_id, "pa", string.IsNullOrEmpty(icu_data.pa) ? "|" : icu_data.pa.Replace("/", "|"), icu_data.VS_Date, icu_data.pa_num_abnormal, ref erow);
                    InsterDB_NIS_VITALSIGN(vs_id, "pcwp", string.IsNullOrEmpty(icu_data.pcwp) ? " " : icu_data.pcwp, icu_data.VS_Date, icu_data.pcwp_num_abnormal, ref erow);
                    InsterDB_NIS_VITALSIGN(vs_id, "etco", string.IsNullOrEmpty(icu_data.etco) ? " " : icu_data.etco, icu_data.VS_Date, icu_data.etco_num_abnormal, ref erow);
                    i++;
                }
                if (((icu_datas.Count * 11) + inster_part_total) == erow)
                {
                    success = true;
                };
            
            }
            catch (Exception ex)
            {
                log.saveLogMsg(ex.Message.ToString(), "ICUDataSave");
            }
            finally
            {
                if (success)
                {
                    link.DBCommit();
                    rm.status = RESPONSE_STATUS.SUCCESS;
                    rm.message = "帶入成功！";
                }
                else
                {
                    link.DBRollBack();
                    rm.status = RESPONSE_STATUS.ERROR;
                    rm.message = "帶入失敗！";
                }
            }

            return new JsonResult { Data = rm, MaxJsonLength = int.MaxValue, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// 插入DATA_VITALSIGN
        /// </summary>
        /// <param name="vs_id">VS資料ID</param>
        /// <param name="itemName">項目代碼</param>
        /// <param name="pPart">部位</param>
        /// <param name="pValue">紀錄值</param>
        /// <param name="datetime">建立、修改時間</param>
        /// <param name="plan">是否有轉護理紀錄</param>
        /// <param name="EROW">ref 已插入數</param>
        private void InsterDB_NIS_VITALSIGN(string vs_id, string itemName, string pValue, string datetime, string plan, ref int EROW, string pPart = "")
        {
            string fee_no = ptinfo.FeeNo;
            string now_datetime = DateTime.Now.ToString("yyyy/MM/dd HH:mm");
            List<DBItem> dbItem = new List<DBItem>();
            dbItem.Add(new DBItem("FEE_NO", fee_no, DBItem.DBDataType.String));
            dbItem.Add(new DBItem("VS_ID", vs_id, DBItem.DBDataType.String));
            dbItem.Add(new DBItem("VS_ITEM", itemName, DBItem.DBDataType.String));
            dbItem.Add(new DBItem("VS_RECORD", pValue, DBItem.DBDataType.String));
            dbItem.Add(new DBItem("CREATE_USER", userinfo.EmployeesNo.Trim(), DBItem.DBDataType.String));
            dbItem.Add(new DBItem("CREATE_DATE", datetime, DBItem.DBDataType.DataTime));
            dbItem.Add(new DBItem("MODIFY_USER", userinfo.EmployeesNo.Trim(), DBItem.DBDataType.String));
            dbItem.Add(new DBItem("MODIFY_DATE", now_datetime, DBItem.DBDataType.DataTime));
            dbItem.Add(new DBItem("VS_TYPE", "ICU", DBItem.DBDataType.String));//因前次發燒記錄
            dbItem.Add(new DBItem("PLAN", plan, DBItem.DBDataType.String));
            dbItem.Add(new DBItem("VS_OTHER_MEMO", "", DBItem.DBDataType.String));
            if (!string.IsNullOrEmpty(pPart))
            {
                dbItem.Add(new DBItem("VS_PART", pPart.Replace(',', '|'), DBItem.DBDataType.String));
            }
            int erow = link.DBExecInsertTns("data_vitalsign", dbItem);
            EROW += erow;

            //判斷是否加入護理紀錄
            if (plan == "Y")
            {
                string title = "";
                string content_o = "";
                string content_i = "";
                bool care_insert_error = false;
                switch (itemName)
                {
                    case "bs":
                        string care_vs_id = fee_no + DateTime.Now.AddYears(-1911).Year.ToString() + DateTime.Now.ToString("MMddHHmm");
                        List<DBItem> care_dbItem = new List<DBItem>();
                        care_dbItem.Add(new DBItem("BSID", care_vs_id, DBItem.DBDataType.String));
                        care_dbItem.Add(new DBItem("FEENO", fee_no, DBItem.DBDataType.String));
                        care_dbItem.Add(new DBItem("INDATE", datetime, DBItem.DBDataType.String));
                        care_dbItem.Add(new DBItem("BLOODSUGAR", pValue, DBItem.DBDataType.String));
                        care_dbItem.Add(new DBItem("INSDT", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                        care_dbItem.Add(new DBItem("INSOP", userinfo.EmployeesNo.Trim(), DBItem.DBDataType.String));
                        care_dbItem.Add(new DBItem("INSOPNAME", "系統拋轉", DBItem.DBDataType.String));
                        care_dbItem.Add(new DBItem("status", "SYSTEM", DBItem.DBDataType.String));
                        if (link.DBExecInsertTns("BLOODSUGAR", care_dbItem) <= 0)
                        {
                            care_insert_error = true;
                        }
                        break;
                    case "mp":
                        content_o = "心跳：" + pValue + " 次/分";
                        break;
                    case "bf":
                        content_o = "呼吸：" + pValue + " 次/分";
                        break;
                    case "bp":
                        content_o = "血壓：" + pValue.Replace("|", "/") + " mmHg";
                        break;
                    case "sp":
                        content_o = "血氧：" + pValue + " %";
                        break;
                    case "cv1":
                        content_o = "中心靜脈壓：" + pValue + " mmHg";
                        break;
                    case "ic1":
                        content_o = "顱內壓：" + pValue + " mmHg";
                        break;
                    case "cpp":
                        content_o = "CPP:" + pValue + "mmHg";
                        break;
                    case "abp":
                        content_o = "動脈血壓：" + pValue.Replace("|", " / ") + " mmHg";
                        break;
                    case "pa":
                        content_o = "肺動脈壓：" + pValue.Replace("|", " / ") + " mmHg";
                        break;
                    case "pcwp":
                        content_o = "PCWP：" + pValue + " mmHg";
                        break;
                    case "etco":
                        content_o = "ETCO2:" + pValue + "mmHg";
                        break;
                }
                if (content_o != "" || content_i != "")
                {
                    int insert_num = Insert_CareRecord(datetime, vs_id, title, "", "", content_o, content_i, "", itemName);
                    if (insert_num <= 0)
                    {
                        care_insert_error = true;
                    }
                }

                if (care_insert_error)
                {
                    EROW = EROW - 1;
                }
            }
        }

        [HttpPost]
        public JsonResult ICUDataSave_old(ICUData icu_data)
        {


            //ICUDataWhereList whereList = new ICUDataWhereList();
            DataTable dt_check = Get_Check_Abnormal_dt();
            string type = base.get_check_type(ptinfo);
            //var Allvalue=Convert.ToChar(allValue);
            List<ICUData> ICUDataList = new List<ICUData>();
            Dictionary<string, List<string>> vs_record = new Dictionary<string, List<string>>();
            string vs_id = base.ptinfo.FeeNo + "_" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
            string plan = "N";
            int EROW = 0;

            string content = string.Empty;
            string[] vs_idArr = Request["vs_id"].ToString().Split(',');

            //whereList.Start_Date = Request["start_date"].ToString();
            //whereList.Start_Time = Request["start_time"].ToString();

            for (int i = 0; i < vs_idArr.Length; i++)
            {
                string GetName = Convert.ToDateTime(Request["startDate"].ToString()).AddMinutes(Convert.ToInt64(vs_idArr[i])).ToString("yyyyMMddHHmmss");
                var datetime = Convert.ToDateTime(Request["startDate"].ToString()).AddMinutes(Convert.ToInt64(vs_idArr[i])).ToString("yyyy/MM/dd HH:mm:ss");
                string[] vs_item = Request[GetName].ToString().Split(',');


                getDBItem("mp", vs_item.GetValue(0).ToString(), vs_id, datetime, plan, ref EROW);
                getDBItem("bf", vs_item.GetValue(1).ToString(), vs_id, datetime, plan, ref EROW);
                getDBItem("bp", vs_item.GetValue(2).ToString(), vs_id, datetime, plan, ref EROW);
                getDBItem("sp", vs_item.GetValue(3).ToString(), vs_id, datetime, plan, ref EROW);
                getDBItem("cv1", vs_item.GetValue(4).ToString(), vs_id, datetime, plan, ref EROW);
                getDBItem("ic1", vs_item.GetValue(5).ToString(), vs_id, datetime, plan, ref EROW);
                getDBItem("cpp", vs_item.GetValue(6).ToString(), vs_id, datetime, plan, ref EROW);
                getDBItem("abp", vs_item.GetValue(7).ToString(), vs_id, datetime, plan, ref EROW);
                getDBItem("pa", vs_item.GetValue(8).ToString(), vs_id, datetime, plan, ref EROW);
                getDBItem("pcwp", vs_item.GetValue(9).ToString(), vs_id, datetime, plan, ref EROW);
                getDBItem("etco", vs_item.GetValue(10).ToString(), vs_id, datetime, plan, ref EROW);

                #region 帶護理紀錄


                //foreach (var item in vs_item)
                //{
                //    List<string> record = new List<string>();
                //    if (item == "bp" || item == "ab" || item == "pa" || item == "fb" || item == "pwh")
                //    {
                //        string[] bph_list = Request[item + "h_view"].ToString().Split(',');
                //        string[] bpl_list = Request[item + "l_view"].ToString().Split(',');
                //        for (int j = 0; j < bph_list.Length; j++)
                //            record.Add(bph_list[i] + "|" + bpl_list[i]);
                //    }
                //    else
                //    {
                //        string[] record_list = Request[item + "_view"].ToString().Split(',');
                //        foreach (var val in record_list)
                //            record.Add(val);
                //    }
                //    vs_record.Add(item, record);
                //}

                #endregion
            
            }
            #region old code
            //foreach (string item in allValue.AllKeys)
            //{
            //    DateTime date;

            //    bool result = DateTime.TryParseExact(item, "yyyyMMddHHmm", null, System.Globalization.DateTimeStyles.None, out date);
            //    var datetime = "";
            //    if (result)
            //    {
            //        datetime = item.ToString();
            //        string[] vs_item = Request[datetime].ToString().Split(',');

            //        for (int i = 0; i < vs_item.Count(); i++)
            //        {
            //            if (i == 0)
            //            {
            //                var MPvs_record = vs_item.GetValue(0).ToString();
            //                dbItem.Add(new DBItem("plan", plan, DBItem.DBDataType.String));

            //                var createDate = Convert.ToString(date.ToString("yyyy/MM/dd HH:mm:ss"));
            //                dbItem.Add(new DBItem("create_date", createDate, DBItem.DBDataType.DataTime));
            //                dbItem.Add(new DBItem("fee_no", base.ptinfo.FeeNo, DBItem.DBDataType.String));
            //                dbItem.Add(new DBItem("vs_id", vs_id, DBItem.DBDataType.String));
            //                dbItem.Add(new DBItem("VS_ITEM", "MP", DBItem.DBDataType.String));
            //                dbItem.Add(new DBItem("vs_record", MPvs_record, DBItem.DBDataType.String));
            //                dbItem.Add(new DBItem("create_user", userinfo.EmployeesNo.Trim(), DBItem.DBDataType.String));
            //                dbItem.Add(new DBItem("modify_user", userinfo.EmployeesNo.Trim(), DBItem.DBDataType.String));
            //                dbItem.Add(new DBItem("modify_date", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
            //                dbItem.Add(new DBItem("VS_TYPE", "M", DBItem.DBDataType.String));
            //                erow += this.link.DBExecInsert("data_vitalsign", dbItem);
            //            }
            //            if (i == 1)
            //            {
            //                var BF = vs_item.GetValue(1).ToString();
            //                dbItem.Add(new DBItem("plan", plan, DBItem.DBDataType.String));

            //                var createDate = Convert.ToString(date.ToString("yyyy/MM/dd HH:mm:ss"));
            //                dbItem.Add(new DBItem("create_date", createDate, DBItem.DBDataType.DataTime));
            //                dbItem.Add(new DBItem("fee_no", base.ptinfo.FeeNo, DBItem.DBDataType.String));
            //                dbItem.Add(new DBItem("vs_id", vs_id, DBItem.DBDataType.String));
            //                dbItem.Add(new DBItem("VS_ITEM", "ABP", DBItem.DBDataType.String));
            //                dbItem.Add(new DBItem("vs_record", BF, DBItem.DBDataType.String));
            //                dbItem.Add(new DBItem("create_user", userinfo.EmployeesNo.Trim(), DBItem.DBDataType.String));
            //                dbItem.Add(new DBItem("modify_user", userinfo.EmployeesNo.Trim(), DBItem.DBDataType.String));
            //                dbItem.Add(new DBItem("modify_date", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
            //                dbItem.Add(new DBItem("VS_TYPE", "M", DBItem.DBDataType.String));
            //                erow += this.link.DBExecInsert("data_vitalsign", dbItem);

            //            }
            //            if (i == 2)
            //            {
            //                var BF = vs_item.GetValue(2).ToString();
            //                dbItem.Add(new DBItem("plan", plan, DBItem.DBDataType.String));

            //                var createDate = Convert.ToString(date.ToString("yyyy/MM/dd HH:mm:ss"));
            //                dbItem.Add(new DBItem("create_date", createDate, DBItem.DBDataType.DataTime));
            //                dbItem.Add(new DBItem("fee_no", base.ptinfo.FeeNo, DBItem.DBDataType.String));
            //                dbItem.Add(new DBItem("vs_id", vs_id, DBItem.DBDataType.String));
            //                dbItem.Add(new DBItem("VS_ITEM", "ABP", DBItem.DBDataType.String));
            //                dbItem.Add(new DBItem("vs_record", BF, DBItem.DBDataType.String));
            //                dbItem.Add(new DBItem("create_user", userinfo.EmployeesNo.Trim(), DBItem.DBDataType.String));
            //                dbItem.Add(new DBItem("modify_user", userinfo.EmployeesNo.Trim(), DBItem.DBDataType.String));
            //                dbItem.Add(new DBItem("modify_date", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
            //                dbItem.Add(new DBItem("VS_TYPE", "M", DBItem.DBDataType.String));
            //                erow += this.link.DBExecInsert("data_vitalsign", dbItem);
            //            }
            //        }


            //        var BP = vs_item.GetValue(2);




            //    }
            
            //    return RedirectToAction("VitalSignSingle", new { @message = "儲存成功" });

            //}
            ////allValue["123"].
            //foreach (var item in vs_date)
            //{
            //    //= Request[item].ToString().Split(',');
            //}

            ////TODO: 參考批次 與前端hidden
            //int erow = 0;
            //List<ICUData> Dt = JsonConvert.DeserializeObject<List<ICUData>>(SelectedDate);
            //List<DBItem> dbItem  = new List<DBItem>();
            //string vs_id = base.ptinfo.FeeNo + "_" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
            //string plan = "N";


            //string content = string.Empty;

            //foreach (var item in Dt)
            //{
            //    dbItem.Add(new DBItem("plan", plan, DBItem.DBDataType.String));
            //    dbItem.Add(new DBItem("create_date", item.VS_Date, DBItem.DBDataType.DataTime));
            //    dbItem.Add(new DBItem("fee_no", base.ptinfo.FeeNo, DBItem.DBDataType.String));
            //    dbItem.Add(new DBItem("vs_id", vs_id, DBItem.DBDataType.String));
            //    dbItem.Add(new DBItem("VS_ITEM", "ABP", DBItem.DBDataType.String));
            //    dbItem.Add(new DBItem("vs_record", item.ABP, DBItem.DBDataType.String));
            //    dbItem.Add(new DBItem("create_user", userinfo.EmployeesNo.Trim(), DBItem.DBDataType.String));
            //    dbItem.Add(new DBItem("modify_user", userinfo.EmployeesNo.Trim(), DBItem.DBDataType.String));
            //    dbItem.Add(new DBItem("modify_date", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
            //    dbItem.Add(new DBItem("VS_TYPE", "M", DBItem.DBDataType.String));
            //    erow += this.link.DBExecInsert("data_vitalsign", dbItem);
            //}

            //foreach (var item in Dt)
            //{
            //    dbItem.Add(new DBItem("plan", "checked", DBItem.DBDataType.String));
            //    dbItem.Add(new DBItem("create_date", item.VS_Date, DBItem.DBDataType.DataTime));
            //    dbItem.Add(new DBItem("fee_no", base.ptinfo.FeeNo, DBItem.DBDataType.String));
            //    dbItem.Add(new DBItem("vs_id", vs_id, DBItem.DBDataType.String));
            //    dbItem.Add(new DBItem("VS_ITEM", "BF", DBItem.DBDataType.String));
            //    dbItem.Add(new DBItem("VS_RECORD", item.BF, DBItem.DBDataType.String));
            //    dbItem.Add(new DBItem("create_user", userinfo.EmployeesNo.Trim(), DBItem.DBDataType.String));
            //    dbItem.Add(new DBItem("modify_user", userinfo.EmployeesNo.Trim(), DBItem.DBDataType.String));
            //    dbItem.Add(new DBItem("modify_date", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
            //    erow += this.link.DBExecInsert("data_vitalsign", dbItem);
            //}

            #endregion
            
            ////return Json(b, JsonRequestBehavior.AllowGet);
            if (EROW > 0)
            {
                Response.Write("<script>alert('儲存成功');window.close();</script>");
                //return new EmptyResult();
            }
            else
            {
                Response.Write("<script>alert('儲存失敗');window.close();</script>");
                //return new EmptyResult();
            }
            return new JsonResult { Data = "", MaxJsonLength = int.MaxValue, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="itmemName"></param>
        /// <param name="pVakue"></param>
        /// <param name="vs_id"></param>
        /// <param name="datetime"></param>
        /// <param name="plan"></param>
        /// <param name="dbItem"></param>
        private void getDBItem(string itemName, string pValue, string vs_id, string datetime, string plan, ref int EROW /*ref List<DBItem> dbItem*/)
        {
            List<DBItem> dbItem = new List<DBItem>();
            dbItem.Add(new DBItem("plan", plan, DBItem.DBDataType.String));
            dbItem.Add(new DBItem("create_date", datetime, DBItem.DBDataType.DataTime));
            dbItem.Add(new DBItem("fee_no", base.ptinfo.FeeNo, DBItem.DBDataType.String));
            dbItem.Add(new DBItem("vs_id", vs_id, DBItem.DBDataType.String));
            dbItem.Add(new DBItem("VS_ITEM", itemName, DBItem.DBDataType.String));
            dbItem.Add(new DBItem("vs_record", pValue, DBItem.DBDataType.String));
            dbItem.Add(new DBItem("create_user", userinfo.EmployeesNo.Trim(), DBItem.DBDataType.String));
            dbItem.Add(new DBItem("modify_user", userinfo.EmployeesNo.Trim(), DBItem.DBDataType.String));
            dbItem.Add(new DBItem("modify_date", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
            dbItem.Add(new DBItem("VS_TYPE", "ICU", DBItem.DBDataType.String));
            int erow = this.link.DBExecInsert("data_vitalsign", dbItem);
            EROW += erow;
        }

        #endregion

        #region VitalSign單次

        //VitalSign單次_新增
        public ActionResult VitalSignSingle()
        {
            try
            {
                ParamsData pd = new ParamsData();
                pd.p_model = "vitalsign";
                pd.p_groups.Add("falseReason");
                pd.p_groups.Add("eyeReason");
                pd.p_groups.Add("stoolType");
                pd.p_groups.Add("stoolColor");
                pd.p_groups.Add("unit");
                pd.p_groups.Add("measurementTool");
                pd.p_groups.Add("sputumColor");
                pd.p_groups.Add("sputumStatus");
                pd.p_groups.Add("sputumUnit");
                pd.setGroupData();


                //判斷有無病人session
                if (Session["PatInfo"] != null)
                {
                    string[] func_list = null;
                    string sqlstr = " SELECT * FROM NIS_SYS_USER_VSOPTIION WHERE EMP_NO = '" + base.ptinfo.FeeNo + "' ";
                    DataTable Dt = this.link.DBExecSQL(sqlstr);
                    if (Dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            func_list = Dt.Rows[i]["func_list"].ToString().Split('|');
                        }
                    }
                    
                    //this.link.DBExecSQL(sqlstr, ref read);

                    ////if (this.link.DBExecSQL(sqlstr, ref reader) && reader.Read() ) {
                    //while (read.Read())
                    //{
                    //    func_list = read["func_list"].ToString().Split('|');
                    //}
                    ////}
                   

                    if (func_list == null || func_list.Length == 0)
                        func_list = "bh|bw".Split('|');

                    ViewData["func_list"] = func_list;
                    ViewData["dropdownitem"] = pd;
                    ViewBag.age = ptinfo.Age;
                    ViewBag.birthday = ptinfo.Birthday;
                    ViewData["MinDate"] = base.GetMinDate();
                    ViewBag.start = (DateTime.Now.AddDays(-4) < ptinfo.InDate) ? ptinfo.InDate : DateTime.Now.AddDays(-1);
                    ViewBag.end = DateTime.Now.ToString("yyyy/MM/dd");
                    ViewBag.inday = (Convert.ToDateTime(DateTime.Now.ToString("yyyy/MM/dd")) - Convert.ToDateTime(ptinfo.InDate.ToString("yyyy/MM/dd"))).Days;
                    ViewBag.feeno = ptinfo.FeeNo;
                    //Discharge time
                    string discharge_time = get_discharge_time(ptinfo.FeeNo);
                    int discharge_day = 0;
                    if (discharge_time != "")
                        discharge_day = (Convert.ToDateTime(Convert.ToDateTime(discharge_time).ToString("yyyy/MM/dd")) - Convert.ToDateTime(DateTime.Now.ToString("yyyy/MM/dd"))).Days;
                    ViewBag.discharge_time = discharge_time;
                    ViewBag.discharge_day = discharge_day;
                }
                else
                {
                    Response.Write("<script>alert('請重新選擇病患');</script>");
                    return new EmptyResult();
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
            finally
            {
                this.link.DBClose();
            }
            return View();
        }

        //VitalSign單次_新增_儲存
        public ActionResult VitalSignSave()
        {
            var serial = "";
            int erow = 0;
            var timenow = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            string isPain = string.Empty;
            if (!string.IsNullOrWhiteSpace(Request["vs_item"]))
            {
                string vs_id = base.ptinfo.FeeNo + "_" + timenow
                    , vs_st_id = base.ptinfo.FeeNo + "_" + DateTime.Now.AddDays(-1).ToString("yyyyMMddHHmmssfff")
                    , vs_date = Request["vs_date"].ToString()
                    , date = Request["vs_date"] + " " + Request["vs_time"]//日期時間
                    , st_date = string.Empty//Convert.ToDateTime(date).AddDays(-1).ToString("yyyy/MM/dd HH:mm")
                    , record = string.Empty
                    , reason = string.Empty
                    , content_i = string.Empty
                    , content_o = string.Empty
                    , title = string.Empty;
                serial = timenow;
                st_date = (Convert.ToDateTime(date).AddDays(-1) < ptinfo.InDate) ? ptinfo.InDate.ToString("yyyy/MM/dd HH:mm") : Convert.ToDateTime(date).AddDays(-1).ToString("yyyy/MM/dd HH:mm");//edit排便記錄時間 小於入院時間的，以入院時間記錄date
                string[] recordList;
                List<DBItem> vs_data = new List<DBItem>();
                List<string> VS_plan = new List<string>();

                if (Request["VS_plan"] != null)
                    VS_plan = Request["VS_plan"].ToString().Split(',').ToList();

                foreach (string item in Request["vs_item"].ToString().Split(','))
                {
                    string temp_vsid = string.Empty;
                    record = string.Empty;//紀錄  
                    reason = string.Empty;//原因
                    vs_data = new List<DBItem>();
                    vs_data.Add(new DBItem("fee_no", base.ptinfo.FeeNo, DBItem.DBDataType.String));
                    temp_vsid = (item.Trim().ToString().ToLower() == "st") ? vs_st_id : vs_id;
                    vs_data.Add(new DBItem("vs_id", temp_vsid, DBItem.DBDataType.String));
                    vs_data.Add(new DBItem("vs_item", item, DBItem.DBDataType.String));
                    vs_data.Add(new DBItem("create_user", userinfo.EmployeesNo.Trim(), DBItem.DBDataType.String));
                    vs_data.Add(new DBItem("modify_user", userinfo.EmployeesNo.Trim(), DBItem.DBDataType.String));
                    vs_data.Add(new DBItem("modify_date", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));


                    //vs_part 部位 _part名稱
                    if (Request[item + "_part"] != null)
                        vs_data.Add(new DBItem("vs_part", Request[item + "_part"].ToString().Replace(',', '|'), DBItem.DBDataType.String));

                    //_record 記錄值 
                    if (Request[item + "_record"] != null)
                    {

                        record = Request[item + "_record"].ToString().Replace(",", "|").Trim();
                        if (record.Replace("|", "").Replace("請選擇", "").Trim() != "")
                        {
                            //體重
                            if (item == "bw")
                                record = record.Replace("請選擇", "Kg");
                            //排便 於2017/09/07 刪除
                            if (item == "si" && record == "請選擇|請選擇") //痰液
                            {
                                record = "請選擇";
                            }
                            vs_data.Add(new DBItem("vs_record", record, DBItem.DBDataType.String));
                        }
                        else
                            record = "";
                    }

                    //vs_reason 不可測原因
                    if (Request[item + "_reason"] != null)
                    {
                        reason = Request[item + "_reason"].ToString();
                        vs_data.Add(new DBItem("vs_reason", reason, DBItem.DBDataType.String));
                    }

                    //vs_memo 護理紀錄
                    if (Request[item + "_memo"] != null)
                        vs_data.Add(new DBItem("vs_memo", Request[item + "_memo"].ToString().Replace(',', '|'), DBItem.DBDataType.String));

                    if (Request[item + "_other_memo"] != null)
                        vs_data.Add(new DBItem("vs_other_memo", Request[item + "_other_memo"], DBItem.DBDataType.String));

                    if (item == "st")
                        vs_data.Add(new DBItem("create_date", st_date, DBItem.DBDataType.DataTime));
                    else
                        vs_data.Add(new DBItem("create_date", date, DBItem.DBDataType.DataTime));

                    // VS_TYPE 因前次發燒記錄
                    if (Request[item + "_hi_record"] != null)
                        vs_data.Add(new DBItem("VS_TYPE", Request[item + "_hi_record"].ToString(), DBItem.DBDataType.String));

                    //帶護理紀錄是否勾選
                    if (VS_plan.Exists(x => x == item))
                        vs_data.Add(new DBItem("plan", "checked", DBItem.DBDataType.String));
                    else
                        vs_data.Add(new DBItem("plan", "N", DBItem.DBDataType.String));

                    if (record.Replace("|", "") != "" || reason != "")
                    {
                        content_i = ""; content_o = ""; title = "";
                        erow += this.link.DBExecInsert("data_vitalsign", vs_data);
                        if (record.Replace("|", "") != "")
                        {
                            if (item == "ps")
                            {
                                //設定是否轉入疼痛頁面
                                if (record.Split('|').ToList().Exists(x => !string.IsNullOrWhiteSpace(x) && int.Parse(x.Substring(1, x.IndexOf(")") - 1)) > 0))
                                    isPain = "ps";
                            }
                            #region 帶護理紀錄 新的
                            if (VS_plan.Exists(x => x == item))
                            {
                                switch (item)
                                {
                                    case "bt":
                                        if (Request[item + "_part"] != null)
                                        {
                                            if (Request[item + "_other_memo"].Replace("|", "").Replace("-1", "").Trim() != "")
                                                title = Request[item + "_other_memo"].Replace("|", " ").Replace("-1", "").Trim();
                                            content_o = ((Request[item + "_part"] != null) ? Request[item + "_part"] + "：" : "") + record + " ℃";
                                            content_i = Request[item + "_memo"];
                                        }
                                        break;
                                    case "mp":
                                        content_o = "脈搏 " + ((Request[item + "_part"] != null && Request[item + "_part"].ToString().IndexOf("心尖脈") < 0) ? Request[item + "_part"] + "：" + record + " 次/分" : "心尖脈：" + record + " 次/分");
                                        content_i = Request[item + "_memo"];
                                        break;
                                    case "bf":
                                        content_o = "呼吸：" + record + " 次/分";
                                        content_i = Request[item + "_memo"];
                                        break;
                                    case "bp":
                                        string[] part = new string[] { "", "" };
                                        part = Request[item + "_part"].Split('|');
                                        content_o = "量測姿勢：" + part[0] + "，部位：" + part[1] + "，血壓：" + record.Replace("|", " / ") + " mmHg";
                                        content_i = Request[item + "_memo"];
                                        break;
                                    case "sp":
                                        content_o = "血氧：" + record + " %";
                                        content_i = Request[item + "_memo"];
                                        break;
                                    case "st":
                                        if (record == "0")
                                        {
                                            content_o = Convert.ToDateTime(date).AddDays(-1).ToString("yyyy/MM/dd") + " " + ((Request[item + "_part"] != null) ? Request[item + "_part"] + "：" : "");
                                            content_o += record.Split('|').GetValue(0).ToString() + "次/天;";
                                        }
                                        else
                                        {
                                            content_o = Convert.ToDateTime(date).AddDays(-1).ToString("yyyy/MM/dd") + " " + ((Request[item + "_part"] != null) ? Request[item + "_part"] + "：" : "");
                                            content_o += record.Split('|').GetValue(0).ToString() + "次/天;";
                                            if ((record.Split('|').Length >= 3))
                                            {
                                                content_o += "種類：";
                                                content_o += (string.IsNullOrEmpty(record.Split('|').GetValue(2).ToString()) ? record.Split('|').GetValue(1).ToString() : record.Split('|').GetValue(2).ToString());
                                                content_o += ";";
                                            }
                                            else
                                            {
                                                content_o += "";
                                            }

                                            if ((record.Split('|').Length >= 5))
                                            {
                                                content_o += "顏色：";
                                                content_o += (string.IsNullOrEmpty(record.Split('|').GetValue(4).ToString()) ? record.Split('|').GetValue(3).ToString() : record.Split('|').GetValue(4).ToString());
                                                content_o += ";";
                                            }
                                            else
                                            {
                                                content_o += "";
                                            }
                                        }
                                        content_o = content_o.Replace("99次/天;", "");
                                        content_i = Request[item + "_memo"];
                                        break;
                                    case "eat":
                                        content_o = (record != null && record.ToString().IndexOf("|") > -1 && record.Split('|').GetValue(1).ToString() != "") ? "飲食：" + record.Split('|').GetValue(1).ToString() : "飲食：" + Request[item + "_part"];
                                        break;
                                    case "ps":
                                        List<string> ps_content_list = new List<string>();
                                        List<string> ps_item_list = new List<string>();
                                        if (!string.IsNullOrEmpty(Request[item + "_occasion"]))
                                        {
                                            ps_content_list.Add("評估時機：" + Request[item + "_occasion"]);
                                        }
                                        int ps_value = 0;
                                        Regex rgx = new Regex(@"^([(]\d+[)])|^(\d+)");
                                        foreach (string ps in record.ToString().Split('|'))
                                        {
                                            if (ps != "")
                                            {
                                                ps_item_list.Add(ps);
                                                ps_value += int.Parse(rgx.Match(ps).ToString().Replace("(", "").Replace(")", ""));
                                            }
                                        }
                                        if (ps_item_list.Count > 0)
                                        {
                                            string pain_val = "";
                                            switch (Request[item + "_assess"])
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
                                            ps_content_list.Add("疼痛強度：" + Request[item + "_assess"]);
                                            if (!string.IsNullOrEmpty(pain_val))
                                            {
                                                ps_content_list.Add("評估項目：" + pain_val);
                                            }
                                            ps_content_list.Add("總計：" + ps_value + "分");
                                        }
                                        //ps_content_list.Add("總計：" + ps_value + "分");
                                        if (!string.IsNullOrEmpty(Request[item + "_memo"]))
                                        {
                                            ps_content_list.Add("謢理措施：" + Request[item + "_memo"]);
                                        }
                                        if (ps_content_list.Count > 0)
                                        {
                                            content_o += string.Join("，", ps_content_list);
                                            title = "疼痛評估";
                                        }
                                        break;
                                    case "gc":
                                        recordList = record.Split('|');
                                        if (recordList.Length > 1)
                                        {
                                            content_o = "GCS:";
                                            if (recordList[0] != "")
                                                content_o += "E" + recordList[0].Substring(1, 1);
                                            if (recordList[1] != "")
                                                content_o += "V" + recordList[1].Substring(1, 1);
                                            if (recordList[2] != "")
                                                content_o += "M" + recordList[2].Substring(1, 1);
                                            content_i = Request[item + "_memo"];
                                        }
                                        break;
                                    case "pupils":
                                        recordList = record.Split('|');
                                        if (recordList.Length > 1)
                                        {
                                            content_o = "Pupil Size:";
                                            if (recordList[0] == "(C)無法睜眼")
                                                content_o += " 左眼: " + recordList[0];
                                            else if (recordList[0] == "無法評估")
                                                content_o += " 左眼: " + recordList[0];
                                            else if (recordList[0] == "其他")
                                                content_o += " 左眼: " + recordList[4];
                                            else
                                                content_o += " 左眼: " + recordList[1] + "mm(" + recordList[0] + ")";

                                            if (recordList[2] == "(C)無法睜眼")
                                                content_o += " 右眼: " + recordList[2];
                                            else if (recordList[2] == "無法評估")
                                                content_o += " 右眼: " + recordList[2];
                                            else if (recordList[2] == "其他")
                                                content_o += " 右眼: " + recordList[5];
                                            else
                                                content_o += " 右眼: " + recordList[3] + "mm(" + recordList[2] + ")";

                                            content_i = Request[item + "_memo"];
                                        }
                                        break;
                                    case "msPower":
                                        recordList = record.Split('|');
                                        if (recordList.Length > 1)
                                        {
                                            content_o = "Muscle Power:";
                                            content_o += "右上肢" + recordList[1] + ((recordList[1] != "無法評估") ? "分," : ",");
                                            content_o += "左上肢" + recordList[0] + ((recordList[0] != "無法評估") ? "分," : ",");
                                            content_o += "右下肢" + recordList[3] + ((recordList[3] != "無法評估") ? "分," : ",");
                                            content_o += "左下肢" + recordList[2] + ((recordList[2] != "無法評估") ? "分。" : "。");
                                            content_i = Request[item + "_memo"];
                                        }
                                        break;
                                    case "ra":
                                        content_o = "鎮靜程度：" + record;
                                        content_i = Request[item + "_memo"];
                                        break;
                                    case "bh":
                                        content_o = "身高：" + record + "cm";
                                        break;
                                    case "bw":
                                        content_o = "體重：" + record.Replace("|", "");
                                        recordList = record.Split('|');
                                        string sqlstr = "SELECT TABLEID FROM (SELECT TABLEID FROM ASSESSMENTMASTER WHERE FEENO = '" + base.ptinfo.FeeNo + "'"
                                            + " AND NATYPE = 'N' ORDER BY CREATETIME DESC) WHERE ROWNUM = 1";
                                        DataTable Dt = link.DBExecSQL(sqlstr);
                                        if (Dt.Rows.Count > 0)
                                        {
                                            string TABLEID = "";
                                            Decimal birthbw = 0;
                                            foreach (DataRow r in Dt.Rows)
                                            {
                                                TABLEID = r["TABLEID"].ToString();
                                            }
                                            sqlstr = "SELECT ITEMVALUE FROM ASSESSMENTDETAIL WHERE TABLEID ='" + TABLEID + "' AND ITEMID='txt_weight'";
                                            if (Dt.Rows.Count > 0)
                                            {
                                                foreach (DataRow r in Dt.Rows)
                                                {
                                                    birthbw = Convert.ToDecimal(r["ITEMVALUE"].ToString());
                                                }
                                            }
                                            string bwpa = ((Convert.ToDecimal(recordList[0]) - birthbw) / birthbw * 100).ToString() + "℅";
                                            content_o += "，生理性體重減輕℅=" + bwpa;
                                        }
                                        break;
                                    case "gtwl":
                                    case "gthr":
                                    case "gtbu":
                                    case "gtbl":
                                    case "gthl":
                                    case "gtlf":
                                    case "gtrf":
                                    case "gtlua":
                                    case "gtrua":
                                    case "gtlt":
                                    case "gtrt":
                                    case "gtll":
                                    case "gtrl":
                                    case "gtla":
                                    case "gtra":
                                        content_o = set_name(item) + "：" + record + "cm";
                                        break;
                                    case "cv1":
                                    case "cv2":
                                        content_o = "中心靜脈壓：" + record + ((item == "cv1") ? " mmHg" : " cmH2O");
                                        content_i = Request[item + "_memo"];
                                        break;
                                    case "ic1":
                                    case "ic2":
                                        content_o = "顱內壓：" + record + ((item == "ic1") ? " mmHg" : " cmH2O");
                                        content_i = Request[item + "_memo"];
                                        break;

                                    case "cpp":
                                        content_o = "CPP:" + record + "mmHg";
                                        content_i = Request[item + "_memo"];
                                        break;
                                    case "etco":
                                        content_o = "ETCO2:" + record + "mmHg";
                                        content_i = Request[item + "_memo"];
                                        break;
                                    case "pcwp":
                                        content_o = "PCWP：" + record + " mmHg";
                                        content_i = Request[item + "_memo"];
                                        break;
                                    case "picco_co":
                                        content_o = "CO：" + record + set_picco_unit(item);// + " L/min";
                                        content_i = Request[item + "_memo"];
                                        break;
                                    case "picco_ci":
                                        content_o = "CI：" + record + set_picco_unit(item);// " L/min.m\u00B2";
                                        content_i = Request[item + "_memo"];
                                        break;
                                    case "picco_sv":
                                        content_o = "SV：" + record + set_picco_unit(item);// " mL/beat";
                                        content_i = Request[item + "_memo"];
                                        break;
                                    case "picco_svi":
                                        content_o = "SVI：" + record + set_picco_unit(item);// " mL/beat/m\u00B2";
                                        content_i = Request[item + "_memo"];
                                        break;
                                    case "picco_svr":
                                        content_o = "SVR：" + record + set_picco_unit(item);// " dynes-sec/cm\u207B\u2075";
                                        content_i = Request[item + "_memo"];
                                        break;
                                    case "picco_svri":
                                        content_o = "SVRI：" + record + set_picco_unit(item);// "dynes-sec/cm\u207B\u2075/m\u00B2";
                                        content_i = Request[item + "_memo"];
                                        break;
                                    case "picco_svv":
                                        content_o = "SVV：" + record + set_picco_unit(item);// " %";
                                        content_i = Request[item + "_memo"];
                                        break;
                                    case "picco_scvo2":
                                        content_o = "ScvO2：" + record + set_picco_unit(item);// " %";
                                        content_i = Request[item + "_memo"];
                                        break;
                                    //case "abp":
                                    //    content_o = "ABP：" + record + ((item == "abp") ? " [ABP-S]" : "[ABP-D] ") + "(ABP-M) mmHg";
                                    //    content_i = Request[item + "_memo"];
                                    //    break;
                                    case "abp": //20170804 護理紀錄裡面去掉"[ABP-S]"等字元顯示並且帶平均出來 edit by AlanHuang
                                        string[] ck_value_list = record.Split('|');
                                        if (ck_value_list[0] != "" && ck_value_list[1] != "")
                                        {
                                            double abpm = double.Parse(ck_value_list[0].Replace("#", "")) / 3 + double.Parse(ck_value_list[1].Replace("#", "")) / 3 * 2;
                                            content_o = "ABP：" + record.Replace("|", "/") + "(" + Math.Round(abpm, 2).ToString() + ")mmHg";
                                        }
                                        content_i = Request[item + "_memo"];
                                        break;
                                    //case "pa":
                                    //    content_o = "PA：" + record + ((item == "pa") ? " [PA-S]" : "[PA-D]  ") + "(PA-M)mmHg";
                                    //    content_i = Request[item + "_memo"];
                                    //    break;
                                    case "pa":
                                        string[] pa_value_list = record.Split('|');
                                        if (pa_value_list[0] != "" && pa_value_list[1] != "")
                                        {
                                            double abpm = double.Parse(pa_value_list[0].Replace("#", "")) / 3 + double.Parse(pa_value_list[1].Replace("#", "")) / 3 * 2;
                                            content_o = "PA：" + record.Replace("|", "/") + "(" + Math.Round(abpm, 2).ToString() + ")mmHg";
                                        }
                                        content_i = Request[item + "_memo"];
                                        break;
                                    case "spo":
                                        content_o = "SPO2" + record + " [SPO2___%]";
                                        content_i = Request[item + "_memo"];
                                        break;
                                    case "gi":
                                        content_o = "保溫箱溫度：" + record + "℃";
                                        break;
                                    case "gi_j":
                                        content_o = "總膽紅素：" + record.Replace("|", "") + "mg/dl";
                                        title = "總膽紅素檢測";
                                        break;
                                    case "gi_c":
                                        content_o = "臍帶脫落：" + record.Replace("脫落", "").Replace("|", "，評估結果：") + "。";
                                        title = "臍帶評估";
                                        break;
                                    case "gi_u":
                                        string[] gi_u_value_list = record.Split('|');
                                        content_o = gi_u_value_list[0];
                                        content_o += ((gi_u_value_list[0] != "0") ? "次/ 尿液性質：" + gi_u_value_list[1] : "，" + gi_u_value_list[1]);
                                        break;
                                    case "gi_st":
                                        string[] gi_st_value_list = record.Split('|');
                                        content_o = "大便潛血：" + gi_st_value_list[0];
                                        break;
                                    case "gi_new":
                                        string[] gi_new_value_list = record.Split('|');
                                        content_o = "新生兒週數：" + gi_new_value_list[0] + "週";
                                        content_o += ((gi_new_value_list[1] == "" || gi_new_value_list[1] == "0") ? "" : "又" + gi_new_value_list[1] + "天");
                                        break;
                                    case "si":
                                        content_o = "保溫箱溫度：" + record + "℃";
                                        break;
                                    case "si_n":
                                        content_o = "膚色：" + record;
                                        break;
                                    case "si_b":
                                        content_o = "呼吸型態：" + record.Replace("|", "");
                                        break;
                                    case "si_r":
                                        content_o = "呼吸品質：" + record;
                                        break;
                                    case "si_s":
                                        string[] si_s_value_list = record.Split('|');
                                        content_o = "痰液：" + si_s_value_list[0] + ";";
                                        content_o += ((record.Split('|').Length >= 2) ? "性質：" + si_s_value_list[1] + ";" : "");
                                        content_o += ((record.Split('|').Length >= 3) ? "顏色：" + si_s_value_list[2] + ";" : "");
                                        content_o += ((record.Split('|').Length >= 4) ? si_s_value_list[3] + ";" : "");
                                        break;
                                    case "si_o":
                                        content_o = "翻身：" + record;
                                        break;
                                    case "si_inspect":
                                        string[] si_inspect_value_list = record.Split('|');
                                        content_o = (!string.IsNullOrEmpty(si_inspect_value_list[0])) ? "尿蛋白：" + si_inspect_value_list[0] + ";" : "";
                                        content_o += (!string.IsNullOrEmpty(si_inspect_value_list[1])) ? "尿糖：" + si_inspect_value_list[1] + ";" : "";
                                        content_o += (!string.IsNullOrEmpty(si_inspect_value_list[2])) ? "PH值：" + si_inspect_value_list[2] + ";" : "";
                                        break;
                                    case "gas":
                                        string[] gas_value_list = record.Split('|');
                                        string[] group = Request[item + "_select"].Split('|');

                                        //for (int i = 0; i < gas_value_list.Count();i++)
                                        //{
                                        //    if(gas_value_list[i] == "")
                                        //    {
                                        //        gas_value_list[i] = " - ";
                                        //    }
                                        //}
                                        part = Request[item + "_part"].Split('|');
                                        if (part != null)
                                        {
                                            if (part[0] != "")
                                            {
                                                content_o = "追蹤" + part[1] + "血氧氣體分析，";
                                            }

                                        }
                                        string[] itemcode = { "pH", "pCO2", "pO2", "HCO3-act", "HCO3-std", "BE(B)", "BE(ecf)", "ctO2", "tHb", "sO2", "FO2Hb", "FCOHb", "FMetHb", "FHHb", "Na+", "K+", "Ca++", "Ca++(7.4)", "Cl-" };
                                        string[] unitcode = { "", "mmHg", "mmHg", "mmol/L", "mmol/L", "mmol/L", "mmol/L", "mmol/L", "mtHb", "g/dL", "%", "%", "%", "%", "mmol/L", "mmol/L", "mmol/L", "mmol/L", "mmol/L" };

                                        for (int i = 0; i < group.Count(); i++)
                                        {
                                            var groupItem = group[i];
                                            switch(groupItem)
                                            {
                                                case "group1":
                                                    content_o += "【ACID / BASE】 ";
                                                    for(int j = 0; j < 8; j++)
                                                    {
                                                        if(gas_value_list[j] != "")
                                                        {
                                                            if (j > 0)
                                                            {
                                                                if (gas_value_list[j] != "" )
                                                                {
                                                                    var hasBgData = false;
                                                                    for(int ck = 0; ck < j;ck++)
                                                                    {
                                                                        if (gas_value_list[ck] != "")
                                                                        {
                                                                            hasBgData = true;
                                                                        }
                                                                    }
                                                                    if(hasBgData == true)
                                                                    {
                                                                        content_o += "、";
                                                                    }

                                                                }
                                                            }
                                                            content_o += itemcode[j] + " : " + gas_value_list[j] + unitcode[j];
                                                        } 
                                              
                                                    }
                                                    content_o += "。";
 
                                                    break;

                                                case "group2":
                                                    content_o += "【CO-OXIMETRY】 ";
                                                    for (int j = 8; j < 14; j++)
                                                    {
                                                        if (gas_value_list[j] != "")
                                                        {

                                                            if (j > 8)
                                                            {
                                                                if (gas_value_list[j] != "")
                                                                {
                                                                    var hasBgData = false;
                                                                    for (int ck = 8; ck < j; ck++)
                                                                    {
                                                                        if (gas_value_list[ck] != "")
                                                                        {
                                                                            hasBgData = true;
                                                                        }
                                                                    }
                                                                    if (hasBgData == true)
                                                                    {
                                                                        content_o += "、";
                                                                    }

                                                                }
                                                            }
                                                            content_o += itemcode[j] + " : " + gas_value_list[j] + unitcode[j];
                                                        }
                                                    }
                                                    content_o += "。";
                                                    break;

                                                case "group3":
                                                    content_o += "【ELECTROLYTES】 ";
                                                    for (int j = 14; j < 19; j++)
                                                    {
                                                        if (gas_value_list[j] != "")
                                                        {
                                                            if (j > 14)
                                                            {
                                                                if (gas_value_list[j] != "")
                                                                {
                                                                    var hasBgData = false;
                                                                    for (int ck = 14; ck < j; ck++)
                                                                    {
                                                                        if (gas_value_list[ck] != "")
                                                                        {
                                                                            hasBgData = true;
                                                                        }
                                                                    }
                                                                    if (hasBgData == true)
                                                                    {
                                                                        content_o += "、";
                                                                    }

                                                                }
                                                            }
                                                            content_o += itemcode[j] + " : " + gas_value_list[j] + unitcode[j];
                                                        }

                                                    }
                                                    content_o += "。";
                                                    break;
                                            }

                                        }
                                        var order = Request[item + "_memo"];
                                        if(order == "")
                                        {
                                            order = " - ";
                                        }
                                        else
                                        {
                                            content_i = "予" + order + "，續追蹤病情變化。";
                                        }
                                        break;
                                }
                            }
                            #endregion
                        }
                        else //mod by yungchen 20161220 測不到也要帶紀錄
                        {
                            #region 帶護理紀錄 測不到的五項
                            if (VS_plan.Exists(x => x == item))
                            {
                                switch (item)
                                {
                                    case "bt":
                                        if (Request[item + "_part"] != null)
                                        {
                                            if (Request[item + "_other_memo"].Replace("|", "").Replace("-1", "").Trim() != "")
                                                title = Request[item + "_other_memo"].Replace("|", " ").Replace("-1", "").Trim();
                                            content_o = ((Request[item + "_part"] != null) ? Request[item + "_part"] + "：" : "") + "測不到";
                                            content_i = Request[item + "_memo"];
                                        }
                                        break;
                                    case "mp":
                                        content_o = "脈搏 " + ((Request[item + "_part"] != null && Request[item + "_part"].ToString().IndexOf("心尖脈") < 0) ? Request[item + "_part"] + "：" + "測不到" : "心尖脈：" + "測不到");
                                        content_i = Request[item + "_memo"];
                                        break;
                                    case "bf":
                                        content_o = "呼吸：" + "測不到";
                                        content_i = Request[item + "_memo"];
                                        break;
                                    case "bp":
                                        content_o = "血壓：" + "測不到";
                                        content_i = Request[item + "_memo"];
                                        break;
                                    case "sp":
                                        content_o = "血氧：" + "測不到";
                                        content_i = Request[item + "_memo"];
                                        break;

                                    case "abp":
                                        content_o = "ABP：" + "測不到";
                                        content_i = Request[item + "_memo"];
                                        break;
                                    case "spo":
                                        content_o = "SPO2：" + "測不到";
                                        content_i = Request[item + "_memo"];
                                        break;

                                }
                            }
                            #endregion
                        }

                        if (content_o != "" || content_i != "")
                        {
                            Insert_CareRecord(date, temp_vsid, title, "", "", content_o, content_i, "", item);
                        }

                    }
                }
            }

            //Blood Gas 申報
            var gas_record = Request["gas_record"].ToString().Replace(",", "|").Trim();
            var gas_time = Request["gas_time"].ToString();
            var gas_chartno = base.ptinfo.ChartNo;
            var gas_feeno = base.ptinfo.FeeNo;
            var gas_doctor = base.ptinfo.DocNo;
            var gas_recorder = userinfo.EmployeesNo ;
            string[] gas_list = gas_record.Split('|');
            var gas_hasdata = false;

            for(int i = 0; i < gas_list.Count(); i++)
            {
                if(gas_list[i] != "")
                {
                    gas_hasdata = true;
                }
            }

            if (gas_hasdata)
            {
                var gas_ex_date = "";
                var gas_ex_time = "";
                var gas_serail = "1";
                string[] itemcode = { "Bloodgas_PH", "Bloodgas_PCO2", "Bloodgas_PO2", "Bloodgas_HCO3", "Bloodgas_HCO3_std", "Bloodgas_BE", "Bloodgas_BE_ecf", "Bloodgas_ctO2", "Bloodgas_HB", "Bloodgas_sO2", "Bloodgas_FO2Hb", "Bloodgas_FCOHb", "Bloodgas_FMetHb", "Bloodgas_FHHb", "Bloodgas_NA", "Bloodgas_K", "Bloodgas_CA", "Bloodgas_Ca_7_4", "Bloodgas_CL" };
                string[] unitcode = { "", "mmHg", "mmHg", "mmol/L", "mmol/L", "mmol/L", "mmol/L", "mmol/L", "mtHb", "g/dL", "%", "%", "%", "%", "mmol/L", "mmol/L", "mmol/L", "mmol/L", "mmol/L" };
                var type = Request["gas_source"].ToString();
                if(type != "")
                {
                    switch(type)
                    {
                        case "ARTERIAL":
                            type = "ArterialBlood";
                            break;
                        case "VENOUS":
                            type = "VenousBlood";
                            break;
                        case "CAPILLARY":
                            type = "Capillary";
                            break;
                        case "ARTERIAL-VENOUS":
                            type = "VenousMixedBlood";
                            break;
                        case "MIXEDV":
                            type = "MixedBlood";
                            break;
                    }           
                }
                else
                {
                    type = "ArterialBlood";
                }

                if (gas_time == "")
                {
                    gas_ex_date = DateTime.Now.ToString("yyyyMMdd");
                    gas_ex_time = DateTime.Now.ToString("HHmmss");
                }
                else
                {
                    var exDate = DateTime.ParseExact(gas_time, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.CurrentCulture);
                    gas_ex_date = exDate.ToString("yyyyMMdd");
                    gas_ex_time = exDate.ToString("HHmmss");
                }
                try
                {
                    List<DBItem> gas_data = new List<DBItem>();
                    gas_data.Add(new DBItem("CASE_NUMBER", gas_feeno + "_" + serial, DBItem.DBDataType.String));
                    gas_data.Add(new DBItem("SERIAL_NUMBER", gas_serail, DBItem.DBDataType.Number));
                    gas_data.Add(new DBItem("DATA_CREATED_DATE", DateTime.Now.ToString("yyyyMMdd"), DBItem.DBDataType.String));
                    gas_data.Add(new DBItem("DATA_CREATED_TIME", DateTime.Now.ToString("HHmmss"), DBItem.DBDataType.String));
                    gas_data.Add(new DBItem("EXAM_DATE", gas_ex_date, DBItem.DBDataType.String));
                    gas_data.Add(new DBItem("EXAM_TIME", gas_ex_time, DBItem.DBDataType.String));
                    gas_data.Add(new DBItem("MEDICAL_RECORD_NUMBER", gas_chartno, DBItem.DBDataType.String));
                    gas_data.Add(new DBItem("SOURCE", "NIS", DBItem.DBDataType.String));
                    gas_data.Add(new DBItem("EXAM_TYPE", type, DBItem.DBDataType.String));
                    gas_data.Add(new DBItem("ORDERING_DOCTOR", gas_doctor, DBItem.DBDataType.String));
                    gas_data.Add(new DBItem("EXECUTOR", gas_recorder, DBItem.DBDataType.String));
                    gas_data.Add(new DBItem("IS_DELETED", "0", DBItem.DBDataType.Number));
                    erow += this.link.DBExecInsert("HIS_BLOODGAS_INFO", gas_data);

                    if (erow > 0)
                    {
                        for (int i = 0; i < gas_list.Count(); i++)
                        {
                            if (gas_list[i] != "")
                            {
                                List<DBItem> gas_detail_data = new List<DBItem>();
                                gas_detail_data.Add(new DBItem("INFO_SERIALNUMBER", gas_serail, DBItem.DBDataType.Number));
                                gas_detail_data.Add(new DBItem("CASE_NUMBER", gas_feeno + "_" + serial, DBItem.DBDataType.String));
                                gas_detail_data.Add(new DBItem("ITEM_CODE", itemcode[i], DBItem.DBDataType.String));
                                gas_detail_data.Add(new DBItem("ITEM_VALUE", gas_list[i], DBItem.DBDataType.String));
                                gas_detail_data.Add(new DBItem("ITEM_UNIT", unitcode[i], DBItem.DBDataType.String));

                                erow = this.link.DBExecInsert("HIS_BLOODGAS_DETAIL", gas_detail_data);
                            }
                        }
                    }
                }

                catch (Exception ex)
                {
                    //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                    string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                    string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                    write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                    return RedirectToAction("VitalSign_Index", new { @message = "無資料可編輯" });
                }

            }

            if (erow > 0)
            {
                if (isPain == "")
                    return RedirectToAction("VitalSignSingle", new { @message = "儲存成功" });
                else
                    return Redirect("../AssessPain_ECK/Index?Msg=儲存成功");
            }
            else
                return RedirectToAction("VitalSign_Index");
        }

        //VitalSign單次_編輯
        public ActionResult VitalSignModify()
        {
            try
            {
                if (Request["vs_id"] != null)
                {
                    //建立下拉選單物件
                    ParamsData pd = new ParamsData("vitalsign");
                    pd.p_groups.Add("falseReason");
                    pd.p_groups.Add("eyeReason");
                    pd.p_groups.Add("stoolType");
                    pd.p_groups.Add("stoolColor");
                    pd.p_groups.Add("unit");
                    pd.p_groups.Add("measurementTool");
                    pd.p_groups.Add("sputumColor");
                    pd.p_groups.Add("sputumStatus");
                    pd.p_groups.Add("sputumUnit");
                    pd.setGroupData();
                    ViewData["dropdownitem"] = pd;
                    ViewBag.start = (DateTime.Now.AddDays(-4) < ptinfo.InDate) ? ptinfo.InDate : DateTime.Now.AddDays(-1);
                    ViewBag.end = DateTime.Now.ToString("yyyy/MM/dd");
                    ViewBag.inday = (Convert.ToDateTime(DateTime.Now.ToString("yyyy/MM/dd")) - Convert.ToDateTime(ptinfo.InDate.ToString("yyyy/MM/dd"))).Days;
                    ViewBag.feeno = ptinfo.FeeNo;
                    //Discharge time
                    string discharge_time = get_discharge_time(ptinfo.FeeNo);
                    int discharge_day = 0;
                    if (discharge_time != "")
                        discharge_day = (Convert.ToDateTime(Convert.ToDateTime(discharge_time).ToString("yyyy/MM/dd")) - Convert.ToDateTime(DateTime.Now.ToString("yyyy/MM/dd"))).Days;
                    ViewBag.discharge_time = discharge_time;
                    ViewBag.discharge_day = discharge_day;
                    string sqlstr = "select * from data_vitalsign where vs_id='" + Request["vs_id"].ToString().Trim() + "' ";
                    sqlstr += "and create_date = to_date('" + Convert.ToDateTime(Request["date"].ToString().Trim()).ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/MM/dd hh24:mi:ss') AND DEL IS NULL";

                    DataTable Dt = this.link.DBExecSQL(sqlstr);

                    VitalSignDataList vsdl = new VitalSignDataList();
                    vsdl.vsid = Request["vs_id"].ToString().Trim();
                    string func_list = "";
                    if (Dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            vsdl.DataList.Add(new VitalSignData(
                                Dt.Rows[i]["vs_item"].ToString(),
                                Dt.Rows[i]["vs_part"].ToString(),
                                Dt.Rows[i]["vs_record"].ToString(),
                                Dt.Rows[i]["vs_reason"].ToString(),
                                Dt.Rows[i]["vs_memo"].ToString(),
                                Dt.Rows[i]["vs_other_memo"].ToString(),
                                Dt.Rows[i]["create_user"].ToString(),
                                Dt.Rows[i]["create_date"].ToString(),
                                Dt.Rows[i]["modify_user"].ToString(),
                                Dt.Rows[i]["modify_date"].ToString(),
                                Dt.Rows[i]["VS_TYPE"].ToString(),
                                Dt.Rows[i]["Plan"].ToString()
                            ));
                            func_list += Dt.Rows[i]["vs_item"].ToString() + ",";
                        }
                    }

                    if (func_list != "")
                        ViewData["func_list"] = func_list.Substring(0, func_list.Length - 1).Split(',');
                    ViewData["vs_data"] = vsdl;
                    ViewBag.date = Convert.ToDateTime(Request["date"].ToString().Trim()).ToString("yyyy/MM/dd HH:mm");
                    ViewBag.age = ptinfo.Age;
                    ViewBag.birthday = ptinfo.Birthday;

                    //----------------------------

                    //欄位控制
                    string[] func_list_setting = null;
                    sqlstr = " SELECT * FROM NIS_SYS_USER_VSOPTIION WHERE EMP_NO = '" + base.ptinfo.FeeNo + "' ";
                    Dt = this.link.DBExecSQL(sqlstr);
                    string tmp = "";
                    if (Dt.Rows.Count > 0)
                    { 
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            func_list_setting = Dt.Rows[i]["func_list"].ToString().Split('|');
                            tmp = Dt.Rows[i]["func_list"].ToString();
                        }
                    }
                    
                    if (func_list_setting == null || func_list_setting.Length == 0)
                        func_list_setting = "bh|bw".Split('|');

                    ViewData["func_list"] = func_list_setting;
                    ViewData["func_list_setting"] = tmp;
                    //----------------------------

                    return View();
                }
                else
                {
                    return RedirectToAction("VitalSign_Index", new { @message = "無資料可編輯" });
                }
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(),ex);
                return RedirectToAction("VitalSign_Index", new { @message = "無資料可編輯" });
            }
            finally
            {
                this.link.DBClose();
            }
        }

        //VitalSign單次_編輯_儲存
        public ActionResult VitalSignUpdate()
        {//TODO: 可以新增 但不能刪除修改 可以嘗試把刪除跟儲存整個複製加入
            int j = 0;
            var serial = "";
            try
            {
                if (Request["vs_id"] != null)
                {
                    string[] gas_serial = Request["vs_id"].Split('_');
                    serial = gas_serial[1];
                }

                    if (!string.IsNullOrWhiteSpace(Request["vs_item"]))
                {
                    string[] update_item = Request["vs_item"].ToString().Split(',');
                    string vs_id = Request["vs_id"].ToString();
                    string vs_date = Request["vs_date"].ToString();
                    string date = (Convert.ToDateTime(Request["new_date"].ToString()) < ptinfo.InDate) ? ptinfo.InDate.ToString("yyyy/MM/dd HH:mm") : Request["new_date"].ToString();//edit 小於入院時間的，以入院時間記錄date
                    List<string> items = new List<string>();
                    if (vs_date != date)
                    {
                        string sqlstr = "select * from data_vitalsign where vs_id='" + Request["vs_id"].ToString().Trim() + "' AND DEL IS NULL ";
                        DataTable Dt = this.link.DBExecSQL(sqlstr);
                        if (Dt.Rows.Count > 0)
                        {
                            for (int i = 0; i < Dt.Rows.Count; i++)
                            {
                               items.Add(Dt.Rows[i]["vs_item"].ToString());
                            }
                        }
                    }
                    foreach (string tag in update_item)
                    {
                        if (!items.Exists(e => e.EndsWith(tag)))
                        {
                            items.Add(tag);
                        }
                    }
                    //items.Add("st");測試 使UPDATE強制進入

                    string where = string.Empty
                        , content_i = string.Empty
                        , content_o = string.Empty
                        , title = string.Empty;
                    string[] recordList;
                    List<DBItem> vs_data = new List<DBItem>();
                    List<string> VS_plan = new List<string>();
                    if (Request["VS_plan"] != null)
                        VS_plan = Request["VS_plan"].ToString().Split(',').ToList();

                    //this.link.DBExecNonSQL("delete DATA_VITALSIGN where fee_no='" + ptinfo.FeeNo.Trim() + "' and vs_id='" + vs_id + "'");
                    List<string> item_list = new List<string>();
                    foreach (string item_id in items)
                    {
                        item_list.Add("'" + item_id + "'");
                    }
                    string in_str = " IN (" + string.Join(",", item_list) + ")";

                    string whereCondition = " fee_no='" + ptinfo.FeeNo.Trim() + "' and vs_id='" + vs_id + "' " + " and vs_item" + in_str;
                    this.link.DBExecDelete("data_vitalsign", whereCondition);
                    foreach (var item in items)
                    {

                        string record = string.Empty, reason = string.Empty;
                        vs_data = new List<DBItem>();
                        vs_data.Add(new DBItem("fee_no", base.ptinfo.FeeNo, DBItem.DBDataType.String));
                        vs_data.Add(new DBItem("vs_id", vs_id, DBItem.DBDataType.String));
                        vs_data.Add(new DBItem("vs_item", item, DBItem.DBDataType.String));
                        vs_data.Add(new DBItem("create_user", userinfo.EmployeesNo.Trim(), DBItem.DBDataType.String));
                        vs_data.Add(new DBItem("create_date", date, DBItem.DBDataType.DataTime));
                        vs_data.Add(new DBItem("modify_date", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));//修改修改時間
                        vs_data.Add(new DBItem("modify_user", userinfo.EmployeesNo.Trim(), DBItem.DBDataType.String));

                        if (Request[item + "_part"] != null)
                            vs_data.Add(new DBItem("vs_part", Request[item + "_part"].ToString().Replace(',', '|'), DBItem.DBDataType.String));

                        if (Request[item + "_record"] != null)
                        {
                            record = Request[item + "_record"].ToString().Replace(",", "|").Trim();
                            if (record.Replace("|", "").Replace("請選擇", "").Trim() != "")
                            {
                                if (item == "bw" && (record == "|Kg" || record == "|g" || record == "請選擇"))
                                {
                                    record = record.Replace("請選擇", "Kg");
                                    //vs_data.Add(new DBItem("vs_record", record, DBItem.DBDataType.String));
                                    base.Del_CareRecord(vs_id, item, false);
                                    link.DBExecDelete("CARERECORD_DATA", "CARERECORD_ID = '" + vs_id + "' AND SELF = '" + item + "' ");
                                }
                                else if (item == "si" && record == "請選擇|請選擇") //痰液
                                {
                                    base.Del_CareRecord(vs_id, item, false);
                                    link.DBExecDelete("CARERECORD_DATA", "CARERECORD_ID = '" + vs_id + "' AND SELF = '" + item + "' ");
                                    //vs_data.Add(new DBItem("vs_record", "請選擇", DBItem.DBDataType.String));
                                }
                                else if (item == "st" && record == "0|請選擇||請選擇|")
                                {
                                    record = "0";
                                }
                                //else if (item == "cv" && record.Split('|').GetValue(0).ToString() == "")
                                //{  //體重有預設單位，未輸入時要排除 by iven
                                //    record = "";
                                //    link.DBExecDelete("NIS_CARERECORD", "CARERECORD_ID = '" + vs_id + "' AND SELF = '" + item + "' ");
                                //}
                                else if (item == "si" && record == "請選擇|請選擇") //痰液
                                {
                                    vs_data.Add(new DBItem("vs_record", "請選擇", DBItem.DBDataType.String));
                                    base.Del_CareRecord(vs_id, item, false);
                                    link.DBExecDelete("CARERECORD_DATA", "CARERECORD_ID = '" + vs_id + "' AND SELF = '" + item + "' ");
                                }
                                else if (item == "gi_u" && record == "|結晶尿" ||  record == "|無解尿" || record == "|黃")
                                {
                                    link.DBExecDelete("DATA_VITALSIGN", "vs_id = '" + vs_id + "' AND VS_ITEM = '" + item + "' AND DEL IS NULL ");
                                }
                                vs_data.Add(new DBItem("vs_record", record, DBItem.DBDataType.String));                            
                            }
                            else
                            {
                                record = "";
                                //vs_data.Add(new DBItem("vs_record", "", DBDataType.String));
                                //值為空時，需刪除護理紀錄 by iven

                                base.Del_CareRecord(vs_id, item, false);
                                link.DBExecDelete("CARERECORD_DATA", "CARERECORD_ID = '" + vs_id + "' AND SELF = '" + item + "' ");
                            }
                        }
                        if (Request[item + "_record"] == null || Request[item + "_record"] == ""/*|| record == ""*/)//if (item == null || item == "")
                        {
                            base.Del_CareRecord(vs_id, item, false);
                            link.DBExecDelete("CARERECORD_DATA", "CARERECORD_ID = '" + vs_id + "' AND SELF = '" + item + "' ");
                        }


                        if (Request[item + "_reason"] != null)
                        {
                            reason = Request[item + "_reason"].ToString();
                            vs_data.Add(new DBItem("vs_reason", reason, DBItem.DBDataType.String));
                        }

                        if (Request[item + "_memo"] != null)
                        {
                            vs_data.Add(new DBItem("vs_memo", Request[item + "_memo"].ToString().Replace(',', '|'), DBItem.DBDataType.String));
                        }

                        if (Request[item + "_other_memo"] != null)
                        {
                            vs_data.Add(new DBItem("vs_other_memo", Request[item + "_other_memo"], DBItem.DBDataType.String));
                        }

                        if (Request[item + "_hi_record"] != null)
                            vs_data.Add(new DBItem("VS_TYPE", Request[item + "_hi_record"].ToString(), DBItem.DBDataType.String));

                        if (VS_plan.Exists(x => x == item))
                            vs_data.Add(new DBItem("plan", "checked", DBItem.DBDataType.String));
                        else
                            vs_data.Add(new DBItem("plan", "N", DBItem.DBDataType.String));

                        if (record.Replace("|", "") != "" || reason != "")
                        {
                            //j += this.link.DBExecUpdate("NIS_VITALSIGN", vs_data, whereCondition);
                            

                            j += this.link.DBExecInsert("data_vitalsign", vs_data);
                            if (record.Replace("|", "") != "")
                            {
                                content_i = ""; content_o = ""; title = "";

                                #region 帶護理紀錄 新的
                                if (VS_plan.Exists(x => x == item))
                                {
                                    switch (item)
                                    {
                                        case "bt":
                                            if (Request[item + "_part"] != null)
                                            {
                                                if (Request[item + "_other_memo"].Replace("|", "").Replace("-1", "").Trim() != "")
                                                    title = Request[item + "_other_memo"].Replace("|", " ").Replace("-1", "").Trim();
                                                content_o = ((Request[item + "_part"] != null) ? Request[item + "_part"] + "：" : "") + record + " ℃";
                                                content_i = Request[item + "_memo"];
                                            }
                                            break;
                                        case "mp":
                                            content_o = "脈搏 " + ((Request[item + "_part"] != null && Request[item + "_part"].ToString().IndexOf("心尖脈") < 0) ? Request[item + "_part"] + "：" + record + " 次/分" : "心尖脈：" + record + " 次/分");
                                            content_i = Request[item + "_memo"];
                                            break;
                                        case "bf":
                                            content_o = "呼吸：" + record + " 次/分";
                                            content_i = Request[item + "_memo"];
                                            break;
                                        case "bp":
                                            string[] part = new string[] { "", "" };
                                            part = Request[item + "_part"].Split('|');
                                            string[] bp_record = new string[] { "", "", "" };
                                            bp_record = record.Split('|');
                                            content_o = "量測姿勢：" + part[0] + "，部位：" + part[1] + "，血壓：" + bp_record[0] + "/" + bp_record[1] + " mmHg";
                                            content_i = Request[item + "_memo"];
                                            break;
                                        case "sp":
                                            content_o = "血氧：" + record + " %";
                                            content_i = Request[item + "_memo"];
                                            break;
                                        case "st":
                                            if (record == "0")
                                            {
                                                date = Convert.ToDateTime(date).ToString("yyyy/MM/dd HH:mm:ss");
                                                content_o = Convert.ToDateTime(date).ToString("yyyy/MM/dd") + " " + ((Request[item + "_part"] != null) ? Request[item + "_part"] + "：" : "");
                                                content_o += record.Split('|').GetValue(0).ToString() + "次/天;";
                                            }
                                            else
                                            {
                                                content_o = Convert.ToDateTime(date).ToString("yyyy/MM/dd") + " " + ((Request[item + "_part"] != null) ? Request[item + "_part"] + "：" : "");
                                                content_o += record.Split('|').GetValue(0).ToString() + "次/天;";
                                                if ((record.Split('|').Length >= 3))
                                                {
                                                    content_o += "種類：";
                                                    content_o += (string.IsNullOrEmpty(record.Split('|').GetValue(2).ToString()) ? record.Split('|').GetValue(1).ToString() : record.Split('|').GetValue(2).ToString());
                                                    content_o += ";";
                                                }
                                                else
                                                {
                                                    content_o += "";
                                                }

                                                if ((record.Split('|').Length >= 5))
                                                {
                                                    content_o += "顏色：";
                                                    content_o += (string.IsNullOrEmpty(record.Split('|').GetValue(4).ToString()) ? record.Split('|').GetValue(3).ToString() : record.Split('|').GetValue(4).ToString());
                                                    content_o += ";";
                                                }
                                                else
                                                {
                                                    content_o += "";
                                                }
                                                //date = Convert.ToDateTime(date).AddDays(1).ToString("yyyy/MM/dd HH:mm:ss");
                                            }
                                            content_o = content_o.Replace("99次/天;", "");
                                            content_i = Request[item + "_memo"];
                                            break;
                                        case "eat":
                                            content_o = (record != null && record.ToString().IndexOf("|") > -1 && record.Split('|').GetValue(1).ToString() != "") ? "飲食：" + record.Split('|').GetValue(1).ToString() : "飲食：" + Request[item + "_part"];
                                            break;
                                        case "ps":
                                            List<string> ps_content_list = new List<string>();
                                            List<string> ps_item_list = new List<string>();
                                            if (!string.IsNullOrEmpty(Request[item + "_occasion"]))
                                            {
                                                ps_content_list.Add("評估時機：" + Request[item + "_occasion"]);
                                            }
                                            int ps_value = 0;
                                            Regex rgx = new Regex(@"^([(]\d+[)])|^(\d+)");
                                            foreach (string ps in record.ToString().Split('|'))
                                            {
                                                if (ps != "")
                                                {
                                                    ps_item_list.Add(ps);
                                                    ps_value += int.Parse(rgx.Match(ps).ToString().Replace("(", "").Replace(")", ""));
                                                }
                                            }
                                            if (ps_item_list.Count > 0)
                                            {
                                                string pain_val = "";
                                                switch (Request[item + "_assess"])
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
                                                ps_content_list.Add("疼痛強度：" + Request[item + "_assess"]);
                                                if (!string.IsNullOrEmpty(pain_val))
                                                {
                                                    ps_content_list.Add("評估項目：" + pain_val);
                                                }
                                                ps_content_list.Add("總計：" + ps_value + "分");
                                            }
                                            //ps_content_list.Add("總計：" + ps_value + "分");
                                            if (!string.IsNullOrEmpty(Request[item + "_memo"]))
                                            {
                                                ps_content_list.Add("謢理措施：" + Request[item + "_memo"]);
                                            }
                                            if (ps_content_list.Count > 0)
                                            {
                                                content_o += string.Join("，", ps_content_list);
                                                title = "疼痛評估";
                                            }
                                            break;
                                        case "gc":
                                            recordList = record.Split('|');
                                            if (recordList.Length > 1)
                                            {
                                                content_o = "GCS:";
                                                if (recordList[0] != "")
                                                    content_o += "E" + recordList[0].Substring(1, 1);
                                                if (recordList[1] != "")
                                                    content_o += "V" + recordList[1].Substring(1, 1);
                                                if (recordList[2] != "")
                                                    content_o += "M" + recordList[2].Substring(1, 1);
                                                content_i = Request[item + "_memo"];
                                            }
                                            break;
                                        case "pupils":
                                            recordList = record.Split('|');
                                            if (recordList.Length > 1)
                                            {
                                                content_o = "Pupil Size:";
                                                if (recordList[0] == "(C)無法睜眼")
                                                    content_o += " 左眼: " + recordList[0];
                                                else if (recordList[0] == "其他")
                                                    content_o += " 左眼: " + recordList[4];
                                                else
                                                    content_o += " 左眼: " + recordList[1] + "mm(" + recordList[0] + ")";

                                                if (recordList[2] == "(C)無法睜眼")
                                                    content_o += " 右眼: " + recordList[2];
                                                else if (recordList[2] == "其他")
                                                    content_o += " 右眼: " + recordList[5];
                                                else
                                                    content_o += " 右眼: " + recordList[3] + "mm(" + recordList[2] + ")";

                                                content_i = Request[item + "_memo"];
                                            }
                                            break;
                                        case "msPower":
                                            recordList = record.Split('|');
                                            if (recordList.Length > 1)
                                            {
                                                content_o = "Muscle Power:";
                                                content_o += "右上肢" + recordList[1] + ((recordList[1] != "無法評估") ? "分," : ",");
                                                content_o += "左上肢" + recordList[0] + ((recordList[0] != "無法評估") ? "分," : ",");
                                                content_o += "右下肢" + recordList[3] + ((recordList[3] != "無法評估") ? "分," : ",");
                                                content_o += "左下肢" + recordList[2] + ((recordList[2] != "無法評估") ? "分。" : "。");
                                                content_i = Request[item + "_memo"];
                                            }
                                            break;
                                        case "ra":
                                            content_o = "鎮靜程度：" + record;
                                            content_i = Request[item + "_memo"];
                                            break;
                                        case "bh":
                                            content_o = "身高：" + record + "cm";
                                            break;
                                        case "bw":
                                            content_o = "體重：" + record.Replace("|", "");
                                            recordList = record.Split('|');
                                            string sqlstr = "SELECT TABLEID FROM (SELECT TABLEID FROM ASSESSMENTMASTER WHERE FEENO = '" + base.ptinfo.FeeNo + "'"
                                                + " AND NATYPE = 'N' ORDER BY CREATETIME DESC) WHERE ROWNUM = 1";
                                            DataTable Dt = link.DBExecSQL(sqlstr);
                                            if (Dt.Rows.Count > 0)
                                            {
                                                string TABLEID = "";
                                                Decimal birthbw = 0;
                                                foreach (DataRow r in Dt.Rows)
                                                {
                                                    TABLEID = r["TABLEID"].ToString();
                                                }
                                                sqlstr = "SELECT ITEMVALUE FROM ASSESSMENTDETAIL WHERE TABLEID ='" + TABLEID + "' AND ITEMID='txt_weight'";
                                                if (Dt.Rows.Count > 0)
                                                {
                                                    foreach (DataRow r in Dt.Rows)
                                                    {
                                                        birthbw = Convert.ToDecimal(r["ITEMVALUE"].ToString());
                                                    }
                                                }
                                                string bwpa = ((Convert.ToDecimal(recordList[0]) - birthbw) / birthbw * 100).ToString() + "℅";
                                                content_o += "，生理性體重減輕℅=" + bwpa;
                                            }
                                            break;
                                        case "gtwl":
                                        case "gthr":
                                        case "gtbu":
                                        case "gtbl":
                                        case "gthl":
                                        case "gtlf":
                                        case "gtrf":
                                        case "gtlua":
                                        case "gtrua":
                                        case "gtlt":
                                        case "gtrt":
                                        case "gtll":
                                        case "gtrl":
                                        case "gtla":
                                        case "gtra":
                                            content_o = set_name(item) + "：" + record + "cm";
                                            break;
                                        case "cv1":
                                        case "cv2":
                                            content_o = "中心靜脈壓：" + record + ((item == "cv1") ? " mmHg" : " cmH2O");
                                            content_i = Request[item + "_memo"];
                                            break;
                                        case "ic1":
                                        case "ic2":
                                            content_o = "顱內壓：" + record + ((item == "ic1") ? " mmHg" : " cmH2O");
                                            content_i = Request[item + "_memo"];
                                            break;

                                        case "cpp":
                                            content_o = "CPP:" + record + "mmHg";
                                            content_i = Request[item + "_memo"];
                                            break;
                                        case "etco":
                                            content_o = "ETCO2：" + record + " mmHg";
                                            content_i = Request[item + "_memo"];
                                            break;
                                        case "pcwp":
                                            content_o = "PCWP：" + record + " mmHg";
                                            content_i = Request[item + "_memo"];
                                            break;
                                        case "picco_co":
                                        case "picco_ci":
                                        case "picco_sv":
                                        case "picco_svi":
                                        case "picco_svr":
                                        case "picco_svri":
                                        case "picco_svv":
                                        case "picco_scvo2":
                                            content_o = set_picco_name(item) + "：" + record + " " + set_picco_unit(item);
                                            content_i = Request[item + "_memo"];
                                            break;
                                        case "abp":
                                            string[] ck_value_list = record.Split('|');
                                            if (ck_value_list[0] != "" && ck_value_list[1] != "")
                                            {
                                                double abpm = double.Parse(ck_value_list[0].Replace("#", "")) / 3 + double.Parse(ck_value_list[1].Replace("#", "")) / 3 * 2;
                                                //content_o = "ABP：" + record + ((item == "abp") ? " [ABP-S]" : "[ABP-D] ") + "(ABP-M) mmHg";
                                                content_o = "ABP：" + record.Replace("|", "/") + "(" + Math.Round(abpm, 2).ToString() + ")mmHg";
                                            }
                                            content_i = Request[item + "_memo"];
                                            break;
                                        case "pa":
                                            content_o = "PA：" + record + ((item == "pa") ? " [PA-S]" : "[PA-D]  ") + "(PA-M)mmHg";
                                            content_i = Request[item + "_memo"];
                                            break;
                                        case "spo":
                                            content_o = "SPO2" + record + " [SPO2___%]";
                                            content_i = Request[item + "_memo"];
                                            break;
                                        case "gi":
                                            content_o = "保溫箱溫度：" + record + "℃";
                                            break;
                                        case "gi_j":
                                            content_o = "總膽紅素：" + record.Replace("|", "") + "mg/dl";
                                            title = "總膽紅素檢測";
                                            break;
                                        case "gi_c":
                                            content_o = "臍帶脫落：" + record.Replace("脫落", "").Replace("|", "，評估結果：") + "。";
                                            title = "臍帶評估";
                                            break;
                                        case "gi_u":
                                            string[] gi_u_value_list = record.Split('|');
                                            content_o = gi_u_value_list[0];
                                            content_o += ((gi_u_value_list[0] != "0") ? "次/ 尿液性質：" + gi_u_value_list[1] : "，" + gi_u_value_list[1]);
                                            break;
                                        case "gi_st":
                                            string[] gi_st_value_list = record.Split('|');
                                            content_o = "大便潛血：" + gi_st_value_list[0];
                                            break;
                                        case "gi_new":
                                            string[] gi_new_value_list = record.Split('|');
                                            content_o = "新生兒週數：" + gi_new_value_list[0] + "週";
                                            content_o += ((gi_new_value_list[1] == "" || gi_new_value_list[1] == "0") ? "" : "又" + gi_new_value_list[1] + "天");
                                            break;
                                        case "si":
                                            content_o = "保溫箱溫度：" + record + "℃";
                                            break;
                                        case "si_n":
                                            content_o = "膚色：" + record;
                                            break;
                                        case "si_b":
                                            content_o = "呼吸型態：" + record.Replace("|", "");
                                            break;
                                        case "si_r":
                                            content_o = "呼吸品質：" + record;
                                            break;
                                        case "si_s":
                                            string[] si_s_value_list = record.Split('|');
                                            content_o = "痰液：" + si_s_value_list[0] + ";";
                                            content_o += ((record.Split('|').Length >= 2) ? "性質：" + si_s_value_list[1] + ";" : "");
                                            content_o += ((record.Split('|').Length >= 3) ? "顏色：" + si_s_value_list[2] + ";" : "");
                                            content_o += ((record.Split('|').Length >= 4) ? si_s_value_list[3] + ";" : "");
                                            break;
                                        case "si_o":
                                            content_o = "翻身：" + record;
                                            break;
                                        case "si_inspect":
                                            string[] si_inspect_value_list = record.Split('|');
                                            content_o = "尿蛋白：" + si_inspect_value_list[0] + ";";
                                            content_o += ((record.Split('|').Length >= 2) ? "尿糖：" + si_inspect_value_list[1] + ";" : "");
                                            content_o += ((record.Split('|').Length >= 3) ? "PH值：" + si_inspect_value_list[2] + ";" : "");
                                            break;
                                        case "gas":
                                            string[] gas_value_list = record.Split('|');
                                            string[] group = Request[item + "_select"].Split('|');

                                            //for (int i = 0; i < gas_value_list.Count(); i++)
                                            //{
                                            //    if (gas_value_list[i] == "")
                                            //    {
                                            //        gas_value_list[i] = " - ";
                                            //    }
                                            //}
                                            part = Request[item + "_part"].Split('|');
                                            if (part != null)
                                            {
                                                content_o = "追蹤" + part[1] + "血氧氣體分析，";
                                            }

                                            string[] itemcode = { "pH", "pCO2", "pO2", "HCO3-act", "HCO3-std", "BE(B)", "BE(ecf)", "ctO2", "tHb", "sO2", "FO2Hb", "FCOHb", "FMetHb", "FHHb", "Na+", "K+", "Ca++", "Ca++(7.4)", "Cl-" };
                                            string[] unitcode = { "", "mmHg", "mmHg", "mmol/L", "mmol/L", "mmol/L", "mmol/L", "mmol/L", "mtHb", "g/dL", "%", "%", "%", "%", "mmol/L", "mmol/L", "mmol/L", "mmol/L", "mmol/L" };
                                            for (int i = 0; i < group.Count(); i++)
                                            {
                                                var groupItem = group[i];
                                                switch (groupItem)
                                                {
                                                    case "group1":
                                                        content_o += "【ACID / BASE】 ";
                                                        for (int bg = 0; bg < 8; bg++)
                                                        {
                                                            if (gas_value_list[bg] != "")
                                                            {
                                                                if (bg > 0)
                                                                {
                                                                    if (gas_value_list[bg] != "")
                                                                    {
                                                                        content_o += "、";
                                                                    }
                                                                }

                                                                content_o += itemcode[bg] + " : " + gas_value_list[bg] + unitcode[bg];

                                                            }

                                                        }
                                                        content_o += "。";

                                                        break;

                                                    case "group2":
                                                        content_o += "【CO-OXIMETRY】 ";
                                                        for (int bg = 8; bg < 14; bg++)
                                                        {
                                                            if (gas_value_list[bg] != "")
                                                            {
                                                                if (bg > 8)
                                                                {
                                                                    if (gas_value_list[bg] != "")
                                                                    {
                                                                        content_o += "、";
                                                                    }
                                                                }

                                                                content_o += itemcode[bg] + " : " + gas_value_list[bg] + unitcode[bg];

                                                            }

                                                        }
                                                        content_o += "。";
                                                        break;

                                                    case "group3":
                                                        content_o += "【ELECTROLYTES】 ";
                                                        for (int bg = 14; bg < 19; bg++)
                                                        {
                                                            if (gas_value_list[bg] != "")
                                                            {
                                                                if (bg > 14)
                                                                {
                                                                    if (gas_value_list[bg] != "")
                                                                    {
                                                                        content_o += "、";
                                                                    }
                                                                }

                                                                content_o += itemcode[bg] + " : " + gas_value_list[bg] + unitcode[bg];

                                                            }

                                                        }
                                                        content_o += "。";
                                                        break;
                                                }

                                            }
                                            var order = Request[item + "_memo"];
                                            if (order == "")
                                            {
                                                order = " - ";
                                            }
                                            else
                                            {
                                                content_i = "予" + order + "，續追蹤病情變化。";
                                            }
                                            break;
                                    }
                                }
                                #endregion


                                if (content_o != "" || content_i != "")
                                {
                                    base.Del_CareRecord(vs_id, item, false);
                                    //link.DBExecDelete("CARERECORD_DATA", "CARERECORD_ID = '" + vs_id + "' AND SELF = '" + item + "' ");
                                    //del_emr(vs_id + item, userinfo.EmployeesNo);
                                    //if (this.Upd_CareRecord(date, vs_id, title, "", "", content_o, content_i, "", item) < 1) //如果原來沒有帶護理紀錄 (就會等於零，原來有會大於零)
                                    Insert_CareRecord(date, vs_id, title, "", "", content_o, content_i, "", item);
                                }
                            }
                            else //mod by yungchen 20161220 測不到也要帶紀錄                     
                            {
                                #region 帶護理紀錄 測不到的五項
                                if (VS_plan.Exists(x => x == item))
                                {
                                    switch (item)
                                    {
                                        case "bt":
                                            if (Request[item + "_part"] != null)
                                            {
                                                if (Request[item + "_other_memo"].Replace("|", "").Replace("-1", "").Trim() != "")
                                                    title = Request[item + "_other_memo"].Replace("|", " ").Replace("-1", "").Trim();
                                                content_o = ((Request[item + "_part"] != null) ? Request[item + "_part"] + "：" : "") + "測不到";
                                                content_i = Request[item + "_memo"];
                                            }
                                            break;
                                        case "mp":
                                            content_o = "脈搏 " + ((Request[item + "_part"] != null && Request[item + "_part"].ToString().IndexOf("心尖脈") < 0) ? Request[item + "_part"] + "：" + "測不到" : "心尖脈：" + "測不到");
                                            content_i = Request[item + "_memo"];
                                            break;
                                        case "bf":
                                            content_o = "呼吸：" + "測不到";
                                            content_i = Request[item + "_memo"];
                                            break;
                                        case "bp":
                                            content_o = "血壓：" + "測不到";
                                            content_i = Request[item + "_memo"];
                                            break;
                                        case "sp":
                                            content_o = "血氧：" + "測不到";
                                            content_i = Request[item + "_memo"];
                                            break;

                                        case "abp":
                                            content_o = "ABP：" + "測不到";
                                            content_i = Request[item + "_memo"];
                                            break;
                                        case "spo":
                                            content_o = "SPO2：" + "測不到";
                                            content_i = Request[item + "_memo"];
                                            break;
                                        case "gi":
                                            content_o = "保溫箱溫度：" + record;
                                            break;
                                        case "gi_j":
                                            content_o = "黃疸：" + record;
                                            break;
                                        case "gi_c":
                                            content_o = "臍帶" + record.Replace("|", "/");
                                            break;
                                        case "gi_u":
                                            content_o = "尿液性質：" + record;
                                            break;
                                        case "si":
                                            content_o = "保溫箱溫度：" + record;
                                            break;
                                        case "si_n":
                                            content_o = "膚色：" + record;
                                            break;
                                        case "si_b":
                                            content_o = "呼吸型態：" + record;
                                            break;
                                        case "si_r":
                                            content_o = "呼吸品質：" + record;
                                            break;
                                        case "si_s":
                                            content_o = "痰液：" + record.Replace("|", "");
                                            break;
                                        case "si_o":
                                            content_o = "翻身：" + record;
                                            break;
                                    }
                                }
                                #endregion


                                if (content_o != "" || content_i != "")
                                {
                                    base.Del_CareRecord(vs_id, item, false);
                                    //link.DBExecDelete("CARERECORD_DATA", "CARERECORD_ID = '" + vs_id + "' AND SELF = '" + item + "' ");
                                    //del_emr(vs_id + item, userinfo.EmployeesNo);
                                    //if (this.Upd_CareRecord(date, vs_id, title, "", "", content_o, content_i, "", item) < 1) //如果原來沒有帶護理紀錄 (就會等於零，原來有會大於零)
                                    Insert_CareRecord(date, vs_id, title, "", "", content_o, content_i, "", item);
                                }
                            }

                        }
                    }
                }
                //Blood Gas 申報

                string gassqlstr = " SELECT * FROM HIS_BLOODGAS_INFO WHERE CASE_NUMBER = '" + Request["vs_id"].ToString().Trim() + "' AND IS_DELETED = 0";
                DataTable GasDt = this.link.DBExecSQL(gassqlstr);
                var effRow = 0;
                var exDay = "";
                var exTime = "";
                var exCDate = "";
                var exCTime = "";
                var exType = "";
                var exVersion = "";
                var gas_doctor = "";
                var gas_recorder = userinfo.EmployeesNo;

                if (GasDt.Rows.Count > 0)
                {
                    exDay = GasDt.Rows[0]["EXAM_DATE"].ToString();
                    exTime = GasDt.Rows[0]["EXAM_TIME"].ToString();
                    exType = GasDt.Rows[0]["EXAM_TYPE"].ToString();
                    exVersion = GasDt.Rows[0]["SERIAL_NUMBER"].ToString();
                    exCDate = GasDt.Rows[0]["DATA_CREATED_DATE"].ToString();
                    exCTime = GasDt.Rows[0]["DATA_CREATED_TIME"].ToString();
                    gas_doctor = GasDt.Rows[0]["ORDERING_DOCTOR"].ToString();
                    var vs_data = new List<DBItem>();
                    vs_data.Add(new DBItem("IS_DELETED", "1", DBItem.DBDataType.Number));
                    var whereCondition = "";
                    whereCondition = "CASE_NUMBER = '" + Request["vs_id"].ToString().Trim() + "' AND IS_DELETED = 0";
                    effRow = this.link.DBExecUpdate("HIS_BLOODGAS_INFO", vs_data, whereCondition);

                }
                if(effRow > 0)
                { 
          
                    var gas_record = Request["gas_record"].ToString().Replace(",", "|").Trim();
                    var gas_time = Request["gas_time"].ToString();
                    var gas_chartno = base.ptinfo.ChartNo;
                    var gas_feeno = base.ptinfo.FeeNo;

                    string[] gas_list = gas_record.Split('|');
                    var gas_hasdata = false;

                    for (int i = 0; i < gas_list.Count(); i++)
                    {
                        if (gas_list[i] != "")
                        {
                            gas_hasdata = true;
                        }
                    }

                    if (gas_hasdata)
                    {
                        var gaserow = 0;
                        var gas_ex_date = "";
                        var gas_ex_time = "";
                        int version = Int32.Parse(exVersion) + 1;
                        var gas_serail = version.ToString();
                        string[] itemcode = { "Bloodgas_PH", "Bloodgas_PCO2", "Bloodgas_PO2", "Bloodgas_HCO3", "Bloodgas_HCO3_std", "Bloodgas_BE", "Bloodgas_BE_ecf", "Bloodgas_ctO2", "Bloodgas_HB", "Bloodgas_sO2", "Bloodgas_FO2Hb", "Bloodgas_FCOHb", "Bloodgas_FMetHb", "Bloodgas_FHHb", "Bloodgas_NA", "Bloodgas_K", "Bloodgas_CA", "Bloodgas_Ca_7_4", "Bloodgas_CL" };
                        string[] unitcode = { "", "mmHg", "mmHg", "mmol/L", "mmol/L", "mmol/L", "mmol/L", "mmol/L", "mtHb", "g/dL", "%", "%", "%", "%", "mmol/L", "mmol/L", "mmol/L", "mmol/L", "mmol/L" };
                        if (exType == "")
                        {
                            exType = "ARTERIAL";
                        }

                        if (gas_time == "")
                        {
                            gas_ex_date = DateTime.Now.ToString("yyyyMMdd");
                            gas_ex_time = DateTime.Now.ToString("HHmmss");
                        }
                        else
                        {
                            var exDate = DateTime.ParseExact(gas_time, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.CurrentCulture);
                            gas_ex_date = exDate.ToString("yyyyMMdd");
                            gas_ex_time = exDate.ToString("HHmmss");
                        }
                        try
                        {
                            List<DBItem> gas_data = new List<DBItem>();
                            gas_data.Add(new DBItem("CASE_NUMBER", gas_feeno + "_" + serial, DBItem.DBDataType.String));
                            gas_data.Add(new DBItem("SERIAL_NUMBER", gas_serail, DBItem.DBDataType.Number));
                            gas_data.Add(new DBItem("DATA_CREATED_DATE", exCDate, DBItem.DBDataType.String));
                            gas_data.Add(new DBItem("DATA_CREATED_TIME", exCTime, DBItem.DBDataType.String));
                            gas_data.Add(new DBItem("EXAM_DATE", gas_ex_date, DBItem.DBDataType.String));
                            gas_data.Add(new DBItem("EXAM_TIME", gas_ex_time, DBItem.DBDataType.String));
                            gas_data.Add(new DBItem("MEDICAL_RECORD_NUMBER", gas_chartno, DBItem.DBDataType.String));
                            gas_data.Add(new DBItem("SOURCE", "NIS", DBItem.DBDataType.String));
                            gas_data.Add(new DBItem("EXAM_TYPE", exType, DBItem.DBDataType.String));
                            gas_data.Add(new DBItem("ORDERING_DOCTOR", gas_doctor, DBItem.DBDataType.String));
                            gas_data.Add(new DBItem("EXECUTOR", gas_recorder, DBItem.DBDataType.String));
                            gas_data.Add(new DBItem("IS_DELETED", "0", DBItem.DBDataType.Number));
                            gaserow = this.link.DBExecInsert("HIS_BLOODGAS_INFO", gas_data);

                            if (gaserow > 0)
                            {
                                for (int i = 0; i < gas_list.Count(); i++)
                                {
                                    if (gas_list[i] != "")
                                    {
                                        List<DBItem> gas_detail_data = new List<DBItem>();
                                        gas_detail_data.Add(new DBItem("INFO_SERIALNUMBER", gas_serail, DBItem.DBDataType.Number));
                                        gas_detail_data.Add(new DBItem("CASE_NUMBER", gas_feeno + "_" + serial, DBItem.DBDataType.String));
                                        gas_detail_data.Add(new DBItem("ITEM_CODE", itemcode[i], DBItem.DBDataType.String));
                                        gas_detail_data.Add(new DBItem("ITEM_VALUE", gas_list[i], DBItem.DBDataType.String));
                                        gas_detail_data.Add(new DBItem("ITEM_UNIT", unitcode[i], DBItem.DBDataType.String));

                                        gaserow += this.link.DBExecInsert("HIS_BLOODGAS_DETAIL", gas_detail_data);
                                    }
                                }
                            }
                        }


                        catch (Exception ex)
                        {
                            //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                            string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                            string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                            write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                            return RedirectToAction("VitalSign_Index", new { @message = "無資料可編輯" });
                        }
                    }

                }





            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(),ex);
                return RedirectToAction("VitalSign_Index");
            }
            finally
            {
                this.link.DBClose();
            }

            if (j > 0)
                return RedirectToAction("VitalSign_Index", new { @message = "儲存成功" });
            else
                return RedirectToAction("VitalSign_Index");
        }


        //VitalSign單次_刪除
        public EmptyResult VitalSignDelete()
        {
            try
            {
                if (Request["vs_id"] != null)
                {
                    DataTable dt = new DataTable();
                    string whereCondition = " fee_no='" + ptinfo.FeeNo + "' and vs_id='" + Request["vs_id"].ToString().Trim() + "' ";//20171023 日期條件加上:ss by ChiChia Huang
                    whereCondition += "and create_date = to_date('" + Convert.ToDateTime(Request["date"].ToString().Trim()).ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/MM/dd hh24:mi:ss') AND DEL IS NULL";
                    string sql = "SELECT VS_ITEM FROM DATA_VITALSIGN WHERE " + whereCondition;

                    link.DBExecSQL(sql, ref dt);
                    foreach (DataRow r in dt.Rows)
                        base.Del_CareRecord(Request["vs_id"].ToString().Trim(), r["VS_ITEM"].ToString());
                    var vs_data_single = new List<DBItem>();
                    vs_data_single.Add(new DBItem("DEL", "D", DBItem.DBDataType.Number));
                    //int effRow = this.link.DBExecDelete("data_vitalsign", whereCondition);
                    int effRow = this.link.DBExecUpdate("data_vitalsign", vs_data_single, whereCondition);
                    if (effRow != 0)
                        Response.Write("Y");
                    else
                        Response.Write("N");

                    //Gas 申報刪除
                    var sqlstr = " SELECT * FROM HIS_BLOODGAS_INFO WHERE CASE_NUMBER = '" + Request["vs_id"].ToString().Trim() + "' AND IS_DELETED = 0";
                    DataTable Dt = this.link.DBExecSQL(sqlstr);
                    if (Dt.Rows.Count > 0)
                    {
                        var vs_data = new List<DBItem>();
                        vs_data.Add(new DBItem("IS_DELETED", "1", DBItem.DBDataType.Number));
                        whereCondition = "";
                        whereCondition = "CASE_NUMBER = '" + Request["vs_id"].ToString().Trim() + "' AND IS_DELETED = 0";
                        effRow = this.link.DBExecUpdate("HIS_BLOODGAS_INFO", vs_data, whereCondition);

                    }
                }
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


            return new EmptyResult();
        }

        //VitalSign單次_疼痛
        public ActionResult VitalSignPS()
        {
            if (Request["ps_type"] != null)
            {
                ViewData["ps_type"] = Request["ps_type"].ToString().Trim();
                return View();
            }
            else
            {
                return new EmptyResult();
            }
        }

        //VitalSign單次_意識狀態
        public ActionResult VitalSignGCS()
        {
            if (Request["gc_type"] != null)
            {
                ViewData["gc_type"] = Request["gc_type"].ToString().Trim();
                return View();
            }
            else
            {
                return new EmptyResult();
            }
        }

        //VitalSign單次_生命徵象處置提示
        public ActionResult VitalSignPrompt()
        {
            string sqlstr = string.Empty;
            try
            {                
                string[] item = null;
                
                sqlstr = " SELECT * FROM NIS_SYS_VITALSIGN_OPTION WHERE ";
                sqlstr += " MODEL_ID IN ('" + Request["lfn"].ToString() + "','" + Request["hfn"].ToString() + "') ";

                if (Request["now_item"].ToString() != "")
                    ViewData["now_item"] = Request["now_item"].ToString().Split('|');
                if (Request["other_memo"] != null)
                    ViewData["other_memo"] = Request["other_memo"].ToString().Split('|');

                DataTable Dt = this.link.DBExecSQL(sqlstr);
                if (Dt.Rows.Count > 0)
                { 
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        switch (Dt.Rows[i]["decide"].ToString())
                        {
                            case ">":
                                if (double.Parse(Request["cvalue"].ToString()) > double.Parse(Dt.Rows[i]["value_limit"].ToString()))
                                {
                                    item = Dt.Rows[i]["item"].ToString().Split('|');
                                    ViewData["title"] = Dt.Rows[i]["title"].ToString();
                                    if (Dt.Rows[i]["other_title"].ToString() != "")
                                    {
                                        ViewData["other_title"] = Dt.Rows[i]["other_title"].ToString();
                                        ViewData["other_item"] = Dt.Rows[i]["other_item"].ToString().Split('|');
                                    }
                                }
                                break;
                            case "<":
                                if (double.Parse(Request["cvalue"].ToString()) < double.Parse(Dt.Rows[i]["value_limit"].ToString()))
                                {
                                    item = Dt.Rows[i]["item"].ToString().Split('|');
                                    ViewData["title"] = Dt.Rows[i]["title"].ToString();
                                    if (Dt.Rows[i]["other_title"].ToString() != "")
                                    {
                                        ViewData["other_title"] = Dt.Rows[i]["other_title"].ToString();
                                        ViewData["other_item"] = Dt.Rows[i]["other_item"].ToString().Split('|');
                                    }
                                }
                                break;
                        }
                    }
                    

                }
                ViewData["item"] = item;
                ViewData["model_name"] = Request["model_name"].ToString();

                return View();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action,"sql= " + sqlstr + "," + ex.ToString(),ex);
                return View(new { @message = ex.Message.ToString() });
            }
            finally
            {
                this.link.DBClose();
            }
        }
        //大便潛血查詢後功能
        public JsonResult VitalSign_OccBlood(string range, string feeno, string start_date = "", string end_date = "")
        {
            RESPONSE_MSG rm = new RESPONSE_MSG();
            string msg = "";
            byte[] tempByte1 = webService.GetBabyLab(feeno, "", "");
            string returnStr = "";
            string start = "";
            string end = "";
            string search_type = "";
            List<Dictionary<string, string>> list = new List<Dictionary<string, string>>();
            if (range == "range")
            {
                start = start_date;
                end = end_date;
                if (tempByte1 != null)
                {   /*6399121 為大便潛血的LabCode*/
                    list = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(CompressTool.DecompressString(tempByte1));
                    list = list.FindAll(x => x["LabCode"].ToString() == "6399121" && DateTime.Parse(x["INSPTDT"].ToString()) >= Convert.ToDateTime(start) && DateTime.Parse(x["INSPTDT"].ToString()) <= Convert.ToDateTime(end) && !string.IsNullOrEmpty(x["LabValue"].ToString()));//時間範圍
                    list.ForEach(x => x["INSPTDT"] = DateTime.Parse(x["INSPTDT"].ToString()).ToString("yyyy-MM-dd"));//將日期時間格式化為日期
                    list = list.OrderByDescending(x => DateTime.Parse(x["INSPTDT"].ToString())).ToList();//排序
                    if (returnStr == "")
                    {
                        msg = start + "至" + end + "沒有大便潛血資料";
                        search_type = "date_range";
                    }
                }
                else
                {
                    msg = start + "至" + end + "沒有大便潛血資料";
                    search_type = "date_range";
                }

                return Json(new { returnStr, msg, list, search_type });
            }
            else
            {
                start = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                end = DateTime.Now.ToString("yyyy-MM-dd");
                if (tempByte1 != null)
                {
                    list = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(CompressTool.DecompressString(tempByte1));
                    //foreach (Dictionary<string, string> item in list)
                    //{
                    //    if (Convert.ToDateTime(item["UseDate"]) > Convert.ToDateTime(start) && Convert.ToDateTime(item["UseDate"]) <= Convert.ToDateTime(end))
                    //    {
                    //        returnStr += item["T_Bilirubin"];
                    //    }
                    //}

                    list = list.FindAll(x => x["LabCode"].ToString() == "6399121" && !string.IsNullOrEmpty(x["LabValue"].ToString()) && DateTime.Parse(x["INSPTDT"].ToString()) >= Convert.ToDateTime(start) && DateTime.Parse(x["INSPTDT"].ToString()) <= DateTime.Now.Date);//時間範圍
                    list = list.OrderByDescending(x => DateTime.Parse(x["INSPTDT"].ToString())).ToList();//排序
                    if (list.Count > 0)//有資料
                    {
                        //資料傳到前端
                        returnStr = list[0]["LabValue"].ToString();
                    }
                    if (returnStr == "")
                    {
                        msg = start + "至" + end + "沒有大便潛血資料";
                        search_type = "second";
                    }

                }
                else
                {
                    if (returnStr == "")
                    {
                        msg = start + "至" + end + "沒有大便潛血資料";
                        search_type = "second";
                    }

                }
                return Json(new { returnStr, msg, list, search_type });
            }
        }

        //總膽紅素查詢後功能
        public JsonResult VitalSign_TBil(string range, string feeno, string start_date = "", string end_date = "")
        {
            RESPONSE_MSG rm = new RESPONSE_MSG();
            string msg = "";
            //取得T-Bilirubin 血球容積%  20170808 add by AlanHuang
            byte[] tempByte1 = webService.GetTBilHCT(feeno);
            string returnStr = "";
            string start = "";
            string end = "";
            string search_type = "";
            List<Dictionary<string, string>> list = new List<Dictionary<string, string>>();
            if (range == "range")
            {
                start = start_date;
                end = end_date;
                if (tempByte1 != null)
                {
                    list = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(CompressTool.DecompressString(tempByte1));
                    list = list.FindAll(x => DateTime.Parse(x["UseDate"].ToString()) >= Convert.ToDateTime(start) && DateTime.Parse(x["UseDate"].ToString()) <= Convert.ToDateTime(end) && !string.IsNullOrEmpty(x["T_Bilirubin"].ToString()));//時間範圍
                    list.ForEach(x => x["UseDate"] = DateTime.Parse(x["UseDate"].ToString()).ToString("yyyy-MM-dd"));//將日期時間格式化為日期
                    list = list.OrderByDescending(x => DateTime.Parse(x["UseDate"].ToString())).ToList();//排序
                    if (returnStr == "")
                    {
                        msg = start + "至" + end + "沒有總膽紅素資料";
                        search_type = "date_range";
                    }
                }
                else
                {
                    msg = start + "至" + end + "沒有總膽紅素資料";
                    search_type = "date_range";
                }

                return Json(new { returnStr, msg, list, search_type });
            }
            else
            {
                start = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                end = DateTime.Now.ToString("yyyy-MM-dd");
                if (tempByte1 != null)
                {
                    list = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(CompressTool.DecompressString(tempByte1));
                    //foreach (Dictionary<string, string> item in list)
                    //{
                    //    if (Convert.ToDateTime(item["UseDate"]) > Convert.ToDateTime(start) && Convert.ToDateTime(item["UseDate"]) <= Convert.ToDateTime(end))
                    //    {
                    //        returnStr += item["T_Bilirubin"];
                    //    }
                    //}

                    list = list.FindAll(x => DateTime.Parse(x["UseDate"].ToString()) >= Convert.ToDateTime(start) && DateTime.Parse(x["UseDate"].ToString()) <= DateTime.Now.Date);//時間範圍
                    list = list.OrderByDescending(x => DateTime.Parse(x["UseDate"].ToString())).ToList();//排序
                    if (list.Count > 0)//有資料
                    {
                        //資料傳到前端
                        returnStr = list[0]["T_Bilirubin"].ToString();
                    }
                    if (returnStr == "")
                    {
                        msg = start + "至" + end + "沒有總膽紅素資料";
                        search_type = "second";
                    }

                }
                else
                {
                    if (returnStr == "")
                    {
                        msg = start + "至" + end + "沒有總膽紅素資料";
                        search_type = "second";
                    }

                }
                return Json(new { returnStr, msg, list, search_type });
            }
        }


        #endregion


        //Blood Gas Data
        public JsonResult GasQueryRanger(string feeno , string start, string end)
        {
            feeno = base.ptinfo.ChartNo;
            feeno = feeno.TrimStart('0');
            //Blood Gas 撈資料模型
            BloodGas temp = new BloodGas();
            List<BloodGas> tempList = new List<BloodGas>();
            List<BloodGas> bgList = new List<BloodGas>();
            List<BloodGasData> bgvList = new List<BloodGasData>();
            List<BloodGasData> bgDataList = new List<BloodGasData>();
            BloodGasData tempV = new BloodGasData();

            if (start == null)
            {
                start = DateTime.Now.ToString("yyyyMMdd");
                end = DateTime.Now.ToString("yyyyMMdd");
            }
            else
            {
                var startDate = DateTime.ParseExact(start, "yyyy/MM/dd", System.Globalization.CultureInfo.CurrentCulture);
                start = startDate.ToString("yyyyMMdd");
                var endDate = DateTime.ParseExact(end, "yyyy/MM/dd", System.Globalization.CultureInfo.CurrentCulture);
                end = endDate.ToString("yyyyMMdd");
            }

            //取得所有資料
            string sql = "";
            sql += "SELECT * FROM(";
            sql += "SELECT d.USEQ, d.ITEMNAME, d.ITEMVALUE, o.IPID, o.RDATE, o.RTIME FROM ASADATA d ";
            sql += "INNER JOIN ASORDER o ";
            sql += "ON d.USEQ = o.USEQ) WHERE LTRIM(IPID, '0') = '"+feeno+"' AND (RDATE  BETWEEN  '" + start + "' AND '" + end + "') ORDER BY RDATE + RTIME DESC";
            DataTable Dt = this.link.DBExecSQL(sql);


            if (Dt != null && Dt.Rows.Count > 0)
            {
                bgList = (List<BloodGas>)Dt.ToList<BloodGas>();
            }
            var query = bgList.GroupBy(
               x => x.USEQ);

            foreach (var grouping in query)
            {
                var time = "";

                tempV = new BloodGasData();
                tempV.VS_ID = grouping.Key;
                tempList = new List<BloodGas>();
                foreach (var pet in grouping)
                {

                    temp = new BloodGas();
                    temp.ITEMNAME = pet.ITEMNAME;
                    temp.ITEMVALUE = pet.ITEMVALUE;
                    time = pet.RDATE + pet.RTIME;
                    tempList.Add(temp);
                }
                tempV.CREATE_DATE = time;
                tempV.BG_List = tempList;
                bgDataList.Add(tempV);
            }

            List<BloodGasView> bgViewData = new List<BloodGasView>();
            if (bgDataList.Count() > 0)
            {
                for (int i = 0; i < bgDataList.Count(); i++)
                {
                    BloodGasView gasview = new BloodGasView();
                    gasview.VS_ID = bgDataList[i].VS_ID;

                    for (int j = 0; j < bgDataList[i].BG_List.Count(); j++)
                    {
                        var type = bgDataList[i].BG_List[j].ITEMNAME;
                        var itemData = bgDataList[i].BG_List[j].ITEMVALUE;
                        switch (type)
                        {
                            case "mpH":
                                gasview.pH = itemData;
                                break;

                            case "mPCO2":
                                gasview.pCO2 = itemData;
                                break;

                            case "mPO2":
                                gasview.pO2 = itemData;
                                break;

                            case "cHCO3act":
                                gasview.HCO3_act = itemData;
                                break;

                            case "cHCO3std":
                                gasview.HCO3_std = itemData;
                                break;

                            case "cBE(vt)":
                                gasview.BE_B = itemData;
                                break;

                            case "cBE(vv)":
                                gasview.BE_ecf = itemData;
                                break;

                            case "ctCO2":
                                gasview.ctO2 = itemData;
                                break;

                            case "mtHb":
                                gasview.tHb = itemData;
                                break;

                            case "cSO2":
                                gasview.SO2 = itemData;
                                break;

                            case "msO2":
                                gasview.SO2 = itemData;
                                break;

                            case "mO2Hb":
                                gasview.FO2Hb = itemData;
                                break;

                            case "mCOHb":
                                gasview.FCOHb = itemData;
                                break;

                            case "mMetHb":
                                gasview.FMetHb = itemData;
                                break;

                            case "mHHb":
                                gasview.FHHB = itemData;
                                break;

                            case "mNa+":
                                gasview.Na = itemData;
                                break;

                            case "mK+":
                                gasview.K = itemData;
                                break;

                            case "mCa++":
                                gasview.Ca = itemData;
                                break;

                            case "cCa++":
                                gasview.Ca_7 = itemData;
                                break;

                            case "mCl-":
                                gasview.CI = itemData;
                                break;

                            case "iSOURCE":
                                gasview.source = itemData;
                                break;

                            default:
                                break;
                        }
                    }
                    var time = bgDataList[i].CREATE_DATE;
                    var dateTime = DateTime.ParseExact(time, "yyyyMMddHHmmss", System.Globalization.CultureInfo.CurrentCulture);
                    string gasTime = dateTime.ToString("yyyy-MM-dd HH:mm:ss");

                    gasview.CREATE_DATE = gasTime;
                    bgViewData.Add(gasview);
                }
                ViewData["BloodGas"] = bgViewData;
            }
            //回傳
            return Json(bgViewData, JsonRequestBehavior.AllowGet);
        }


        //Blood Gas
        public JsonResult VitalSign_Gas(string range, string feeno, string start_date = "", string end_date = "")
        {
            RESPONSE_MSG rm = new RESPONSE_MSG();
            string msg = "";
            //取得T-Bilirubin 血球容積%  20170808 add by AlanHuang
            byte[] tempByte1 = webService.GetTBilHCT(feeno);
            string returnStr = "";
            string start = "";
            string end = "";
            string search_type = "";
            List<Dictionary<string, string>> list = new List<Dictionary<string, string>>();

         
            //returnStr = Json(bgViewData, JsonRequestBehavior.AllowGet).ToString();


            if (range == "range")
            {
                start = start_date;
                end = end_date;
                if (tempByte1 != null)
                {
                    list = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(CompressTool.DecompressString(tempByte1));
                    list = list.FindAll(x => DateTime.Parse(x["UseDate"].ToString()) >= Convert.ToDateTime(start) && DateTime.Parse(x["UseDate"].ToString()) <= Convert.ToDateTime(end) && !string.IsNullOrEmpty(x["T_Bilirubin"].ToString()));//時間範圍
                    list.ForEach(x => x["UseDate"] = DateTime.Parse(x["UseDate"].ToString()).ToString("yyyy-MM-dd"));//將日期時間格式化為日期
                    list = list.OrderByDescending(x => DateTime.Parse(x["UseDate"].ToString())).ToList();//排序
                    if (returnStr == "")
                    {
                        //msg = start + "至" + end + "沒有血液氣體資料";
                        //search_type = "date_range";
                    }
                }
                else
                {
                    //msg = start + "至" + end + "沒有血液氣體資料";
                    //search_type = "date_range";
                }

                return Json(new { returnStr, msg, list, search_type });
            }
            else
            {
                start = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                end = DateTime.Now.ToString("yyyy-MM-dd");
                if (tempByte1 != null)
                {
                    list = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(CompressTool.DecompressString(tempByte1));
                    //foreach (Dictionary<string, string> item in list)
                    //{
                    //    if (Convert.ToDateTime(item["UseDate"]) > Convert.ToDateTime(start) && Convert.ToDateTime(item["UseDate"]) <= Convert.ToDateTime(end))
                    //    {
                    //        returnStr += item["T_Bilirubin"];
                    //    }
                    //}

                    list = list.FindAll(x => DateTime.Parse(x["UseDate"].ToString()) >= Convert.ToDateTime(start) && DateTime.Parse(x["UseDate"].ToString()) <= DateTime.Now.Date);//時間範圍
                    list = list.OrderByDescending(x => DateTime.Parse(x["UseDate"].ToString())).ToList();//排序
                    if (list.Count > 0)//有資料
                    {
                        //資料傳到前端
                        returnStr = list[0]["T_Bilirubin"].ToString();
                    }
                    if (returnStr == "")
                    {
                        //msg = start + "至" + end + "沒有血液氣體資料";
                        //search_type = "second";
                    }

                }
                else
                {
                    if (returnStr == "")
                    {
                        //msg = start + "至" + end + "沒有血液氣體資料";
                        //search_type = "second";
                    }

                }
                return Json(new { returnStr, msg, list, search_type });
            }
        }


        #region VitalSign批次

        //VitalSign批次_新增_編輯
        public ActionResult VitalSign_Multiple(string date, string shiftcategory, string strat_time, string end_time, string feq)
        {
            if (!string.IsNullOrWhiteSpace(strat_time) && !string.IsNullOrWhiteSpace(end_time))
            {
                ViewBag.dt = set_VitalSign_Multiple_dt(Convert.ToDateTime(date), strat_time, end_time, feq);
                ViewBag.shiftcategory = shiftcategory;
                ViewBag.strat_time = strat_time;
                ViewBag.end_time = end_time;
                ViewBag.date = date;
                ViewBag.feq = feq;
            }
            else if (date != null)
            {
                ViewBag.dt = set_VitalSign_Multiple_dt(Convert.ToDateTime(date), "", "", "");
                ViewBag.shiftcategory = "D";
                ViewBag.date = date;
                ViewBag.feq = "60";
            }
            else
            {
                DateTime now = DateTime.Now;
                if (DateTime.Now.Hour < 8)
                {
                    ViewBag.dt = set_VitalSign_Multiple_dt(now, now.ToString("yyyy/MM/dd 00:00"), now.ToString("yyyy/MM/dd 08:00"), "60");
                    ViewBag.shiftcategory = "N";
                }
                else if (DateTime.Now.Hour < 16)
                {
                    ViewBag.dt = set_VitalSign_Multiple_dt(now, now.ToString("yyyy/MM/dd 08:00"), now.ToString("yyyy/MM/dd 16:00"), "60");
                    ViewBag.shiftcategory = "D";
                }
                else
                {
                    ViewBag.dt = set_VitalSign_Multiple_dt(now, now.ToString("yyyy/MM/dd 16:00"), now.AddDays(1).ToString("yyyy/MM/dd 00:00"), "60");
                    ViewBag.shiftcategory = "E";
                }
                ViewBag.date = now.ToString("yyyy/MM/dd");
                ViewBag.feq = "60";
            }
            return View();
        }

        //VitalSign批次_儲存
        public ActionResult VitalSign_Multiple_Save(FormCollection form)
        {
            DataTable dt_check = Get_Check_Abnormal_dt();
            string type = base.get_check_type(ptinfo);
            string[] vs_item = Request["vs_item"].ToString().Split(',');
            string[] vs_date = Request["vs_date"].ToString().Split(',');
            string plan = "N";
            Dictionary<string, List<string>> vs_record = new Dictionary<string, List<string>>();
            foreach (var item in vs_item)
            {
                List<string> record = new List<string>();
                if (item == "bp" || item == "ab" || item == "pa" || item == "fb" || item == "pwh")
                {
                    string[] bph_list = Request[item + "h_view"].ToString().Split(',');
                    string[] bpl_list = Request[item + "l_view"].ToString().Split(',');
                    for (int i = 0; i < bph_list.Length; i++)
                        record.Add(bph_list[i] + "|" + bpl_list[i]);
                }
                else
                {
                    string[] record_list = Request[item + "_view"].ToString().Split(',');
                    foreach (var val in record_list)
                        record.Add(val);
                }
                vs_record.Add(item, record);
            }

            for (int i = 0; i <= vs_date.Length - 1; i++)
            {
                string vs_id = ptinfo.FeeNo + "_" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + i.ToString();
                string datetime = vs_date[i];
                for (int j = 0; j < vs_item.Length; j++)
                {
                    string record = vs_record[vs_item[j]][i].Trim();
                    if (record.Replace("|", "").Trim() != "")
                    {
                        List<DBItem> vs_data = new List<DBItem>();
                        vs_data.Add(new DBItem("fee_no", ptinfo.FeeNo, DBItem.DBDataType.String));
                        vs_data.Add(new DBItem("vs_id", vs_id, DBItem.DBDataType.String));
                        vs_data.Add(new DBItem("create_user", userinfo.EmployeesNo.Trim(), DBItem.DBDataType.String));
                        vs_data.Add(new DBItem("create_date", datetime, DBItem.DBDataType.DataTime));
                        vs_data.Add(new DBItem("modify_user", userinfo.EmployeesNo.Trim(), DBItem.DBDataType.String));
                        vs_data.Add(new DBItem("modify_date", datetime, DBItem.DBDataType.DataTime));
                        vs_data.Add(new DBItem("VS_TYPE", "M", DBItem.DBDataType.String));
                        vs_data.Add(new DBItem("vs_item", vs_item[j], DBItem.DBDataType.String));
                        vs_data.Add(new DBItem("vs_record", record, DBItem.DBDataType.String));


                        string content_o = "";
                        if (vs_item[j] != "fb")
                        {
                            if (vs_item[j] == "bt")
                            {
                                vs_data.Add(new DBItem("vs_part", "耳溫", DBItem.DBDataType.String));
                                if (Check_Num_Abnormal("btl_e", "bth_e", record, dt_check) == "Y")
                                {
                                    content_o = "耳溫：" + record + " ℃";
                                    plan = "Y";
                                }

                            }
                            else if (vs_item[j] == "mp")
                            {
                                vs_data.Add(new DBItem("vs_part", "心跳", DBItem.DBDataType.String));
                                if (Check_Num_Abnormal("mpl_" + type, "mph_" + type, record, dt_check) == "Y")
                                {
                                    content_o = "心跳：" + record + " 次/分";
                                    plan = "Y";
                                }

                            }
                            else if (vs_item[j] == "bf" && Check_Num_Abnormal("bfl_" + type, "bfh_" + type, record, dt_check) == "Y")
                            {
                                content_o = "呼吸：" + record + " 次/分";
                                plan = "Y";
                            }
                            else if (vs_item[j] == "bp")
                            {
                                vs_data.Add(new DBItem("vs_part", "躺|左手", DBItem.DBDataType.String));
                                if (Check_Num_Abnormal("bpls_" + type, "bphs_" + type, record.Split('|').GetValue(0).ToString(), dt_check) == "Y" || Check_Num_Abnormal("bpld_" + type, "bphd_" + type, record.Split('|').GetValue(1).ToString(), dt_check) == "Y")
                                {
                                    content_o = "血壓：" + record.Replace("|", " / ") + " mmHg";
                                    plan = "Y";
                                }
                            }
                            else if (vs_item[j] == "sp" && Check_Num_Abnormal("spl", "", record, dt_check) == "Y")
                            {
                                content_o = "血氧：" + record + " %";
                                plan = "Y";
                            }
                            else if (vs_item[j] == "cv1" && Check_Num_Abnormal("cv1_l", "cv1_h", record, dt_check) == "Y")
                            {
                                content_o = "中心靜脈壓：" + record + " mmHg";
                                plan = "Y";
                            }
                            else if (vs_item[j] == "cv2" && Check_Num_Abnormal("cv2_l", "cv2_h", record, dt_check) == "Y")
                            {
                                content_o = "中心靜脈壓：" + record + " cmH2O";
                                plan = "Y";
                            }
                            else if (vs_item[j] == "ic1" && Check_Num_Abnormal("ic1_l", "", record, dt_check) == "Y")
                            {
                                content_o = "顱內壓：" + record + " mmHg";
                                plan = "Y";
                            }
                            else if (vs_item[j] == "ic2" && Check_Num_Abnormal("ic2_l", "", record, dt_check) == "Y")
                            {
                                content_o = "顱內壓：" + record + " cmH2O";
                                plan = "Y";
                            }
                            //else if (vs_item[j] == "ab")
                            //{
                            //    if (Check_Num_Abnormal("abls", "abhs", record.Split('|').GetValue(0).ToString(), dt_check) == "Y" || Check_Num_Abnormal("abld", "abhd", record.Split('|').GetValue(1).ToString(), dt_check) == "Y")
                            //        content_o = "動脈血壓：" + record.Replace("|", " / ") + " mmHg";
                            //}
                            //else if (vs_item[j] == "pa")
                            //{
                            //    if (Check_Num_Abnormal("pals", "pahs", record.Split('|').GetValue(0).ToString(), dt_check) == "Y" || Check_Num_Abnormal("pald", "pahd", record.Split('|').GetValue(1).ToString(), dt_check) == "Y")
                            //        content_o = "肺動脈壓：" + record.Replace("|", " / ") + " mmHg";
                            //}
                            //else if (vs_item[j] == "cp" && Check_Num_Abnormal("cpl", "", record, dt_check) == "Y")
                            //    content_o = "腦灌流壓：" + record + " mmHg";
                            //else if (vs_item[j] == "co" && Check_Num_Abnormal("col", "coh", record, dt_check) == "Y")
                            //    content_o = "心輸出量：" + record + " L/min";
                            //else if (vs_item[j] == "ci" && Check_Num_Abnormal("cil", "cih", record, dt_check) == "Y")
                            //    content_o = "心輸出量指數：" + record + " L/min/m2";
                            //else if (vs_item[j] == "sv" && Check_Num_Abnormal("svl", "svh", record, dt_check) == "Y")
                            //    content_o = "混合靜脈血氧飽合度：" + record + " %";
                        }
                        vs_data.Add(new DBItem("plan", plan, DBItem.DBDataType.String)); //20160407新增 轉帶護理紀錄自動不帶
                        int erow = this.link.DBExecInsert("data_vitalsign", vs_data);
                        if (content_o != "" && erow > 0)
                            Insert_CareRecord(datetime, vs_id, "", "", "", content_o, "", "", vs_item[j]);
                    }
                }
            }
            return RedirectToAction("VitalSign_Multiple", new { @message = "儲存成功" });
        }

        private DataTable set_VitalSign_Multiple_dt(DateTime date, string strat_time, string end_time, string feq)
        {
            string sqlstr = " SELECT * FROM NIS_SYS_USER_VSOPTIION WHERE EMP_NO = '" + ptinfo.FeeNo + "' ";
            string[] func_list = null;
            DataTable dt = null;
            
            try
            {
                DataTable Dt = this.link.DBExecSQL(sqlstr);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        func_list = Dt.Rows[i]["func_list"].ToString().Split('|');
                    }
                }
                if (func_list != null)
                    ViewData["func_list"] = func_list;
                
                dt = new DataTable();
                sqlstr = "SELECT TEMP.*,";
                sqlstr += "(SELECT VS_RECORD FROM DATA_VITALSIGN WHERE FEE_NO = '" + ptinfo.FeeNo + "' AND VS_ID = TEMP.VS_ID AND VS_ITEM = 'bt')bt,";
                sqlstr += "(SELECT VS_RECORD FROM DATA_VITALSIGN WHERE FEE_NO = '" + ptinfo.FeeNo + "' AND VS_ID = TEMP.VS_ID AND VS_ITEM = 'mp')mp,";
                sqlstr += "(SELECT VS_RECORD FROM DATA_VITALSIGN WHERE FEE_NO = '" + ptinfo.FeeNo + "' AND VS_ID = TEMP.VS_ID AND VS_ITEM = 'bf')bf,";
                sqlstr += "(SELECT VS_RECORD FROM DATA_VITALSIGN WHERE FEE_NO = '" + ptinfo.FeeNo + "' AND VS_ID = TEMP.VS_ID AND VS_ITEM = 'bp')bp,";
                sqlstr += "(SELECT VS_RECORD FROM DATA_VITALSIGN WHERE FEE_NO = '" + ptinfo.FeeNo + "' AND VS_ID = TEMP.VS_ID AND VS_ITEM = 'sp')sp,";
                sqlstr += "(SELECT VS_RECORD FROM DATA_VITALSIGN WHERE FEE_NO = '" + ptinfo.FeeNo + "' AND VS_ID = TEMP.VS_ID AND VS_ITEM = 'cv1')cv1,";
                sqlstr += "(SELECT VS_RECORD FROM DATA_VITALSIGN WHERE FEE_NO = '" + ptinfo.FeeNo + "' AND VS_ID = TEMP.VS_ID AND VS_ITEM = 'cv2')cv2,";
                sqlstr += "(SELECT VS_RECORD FROM DATA_VITALSIGN WHERE FEE_NO = '" + ptinfo.FeeNo + "' AND VS_ID = TEMP.VS_ID AND VS_ITEM = 'ic1')ic1,";
                sqlstr += "(SELECT VS_RECORD FROM DATA_VITALSIGN WHERE FEE_NO = '" + ptinfo.FeeNo + "' AND VS_ID = TEMP.VS_ID AND VS_ITEM = 'ic2')ic2,";
                sqlstr += "(SELECT VS_RECORD FROM DATA_VITALSIGN WHERE FEE_NO = '" + ptinfo.FeeNo + "' AND VS_ID = TEMP.VS_ID AND VS_ITEM = 'ab')ab,";
                sqlstr += "(SELECT VS_RECORD FROM DATA_VITALSIGN WHERE FEE_NO = '" + ptinfo.FeeNo + "' AND VS_ID = TEMP.VS_ID AND VS_ITEM = 'pa')pa,";
                sqlstr += "(SELECT VS_RECORD FROM DATA_VITALSIGN WHERE FEE_NO = '" + ptinfo.FeeNo + "' AND VS_ID = TEMP.VS_ID AND VS_ITEM = 'cp')cp,";
                sqlstr += "(SELECT VS_RECORD FROM DATA_VITALSIGN WHERE FEE_NO = '" + ptinfo.FeeNo + "' AND VS_ID = TEMP.VS_ID AND VS_ITEM = 'fb')fb,";
                sqlstr += "(SELECT VS_RECORD FROM DATA_VITALSIGN WHERE FEE_NO = '" + ptinfo.FeeNo + "' AND VS_ID = TEMP.VS_ID AND VS_ITEM = 'co')co,";
                sqlstr += "(SELECT VS_RECORD FROM DATA_VITALSIGN WHERE FEE_NO = '" + ptinfo.FeeNo + "' AND VS_ID = TEMP.VS_ID AND VS_ITEM = 'ci')ci,";
                sqlstr += "(SELECT VS_RECORD FROM DATA_VITALSIGN WHERE FEE_NO = '" + ptinfo.FeeNo + "' AND VS_ID = TEMP.VS_ID AND VS_ITEM = 'sv')sv, ";
                sqlstr += "(SELECT VS_RECORD FROM DATA_VITALSIGN WHERE FEE_NO = '" + ptinfo.FeeNo + "' AND VS_ID = TEMP.VS_ID AND VS_ITEM = 'ral')ral, ";
                sqlstr += "(SELECT VS_RECORD FROM DATA_VITALSIGN WHERE FEE_NO = '" + ptinfo.FeeNo + "' AND VS_ID = TEMP.VS_ID AND VS_ITEM = 'rar')rar, ";
                sqlstr += "(SELECT VS_RECORD FROM DATA_VITALSIGN WHERE FEE_NO = '" + ptinfo.FeeNo + "' AND VS_ID = TEMP.VS_ID AND VS_ITEM = 'pwo')pwo, ";
                sqlstr += "(SELECT VS_RECORD FROM DATA_VITALSIGN WHERE FEE_NO = '" + ptinfo.FeeNo + "' AND VS_ID = TEMP.VS_ID AND VS_ITEM = 'pwh')pwh ";
                sqlstr += "FROM (SELECT distinct(create_date) as CREATE_DATE, VS_ID, CREATE_USER FROM DATA_VITALSIGN WHERE VS_TYPE = 'M' ";
                sqlstr += "AND FEE_NO = '" + ptinfo.FeeNo + "' ";
                sqlstr += "AND create_date  BETWEEN to_date('" + date.ToString("yyyy/MM/dd 00:00:00") + "','yyyy/MM/dd hh24:mi:ss') ";
                sqlstr += "AND to_date('" + date.ToString("yyyy/MM/dd 23:59:59") + "','yyyy/MM/dd hh24:mi:ss')";
                sqlstr += " AND DEL IS NULL ORDER BY create_date) TEMP ";
                link.DBExecSQL(sqlstr, ref dt);

                if (strat_time != "" && end_time != "" && feq != "")
                {
                    for (DateTime start = Convert.ToDateTime(strat_time); start < Convert.ToDateTime(end_time); start = start.AddMinutes(int.Parse(feq)))
                    {
                        bool success = true;

                        foreach (DataRow r in dt.Rows)
                        {
                            if (Convert.ToDateTime(r["CREATE_DATE"]) == start)
                            {
                                success = false;
                                break;
                            }
                        }
                        if (success)
                        {
                            DataRow dt_r = dt.NewRow();
                            dt_r["CREATE_DATE"] = start;
                            dt_r["VS_ID"] = "NEW";
                            dt.Rows.Add(dt_r);
                        }

                    }
                    dt.DefaultView.Sort = "CREATE_DATE asc";
                }
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

            return dt;
        }

        #endregion

        #region VitalSign單次_批次_共用function

        //紀錄VitalSign每位病人常用項目
        public EmptyResult SaveVSOption()
        {
            try
            {
                string sqlstr = " SELECT * FROM NIS_SYS_USER_VSOPTIION WHERE EMP_NO = '" + base.ptinfo.FeeNo + "' ";
                DataTable Dt = link.DBExecSQL(sqlstr);
                
                    List<DBItem> dataList = new List<DBItem>();
                dataList.Add(new DBItem("emp_no", base.ptinfo.FeeNo, DBItem.DBDataType.String));
                dataList.Add(new DBItem("func_list", Request["func_list"].ToString(), DBItem.DBDataType.String));
                if (Dt.Rows.Count > 0)
                {
                    this.link.DBExecUpdate("NIS_SYS_USER_VSOPTIION", dataList, "emp_no='" + base.ptinfo.FeeNo + "'");
                }
                else
                {
                    this.link.DBExecInsert("NIS_SYS_USER_VSOPTIION", dataList);
                }
            }
            catch (Exception ex)
            {
                Response.Write(ex.Message.ToString());
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(),ex);
            }
            finally
            {
                this.link.DBClose();
            }

            return new EmptyResult();
        }

               

        #endregion

        public ActionResult Machine_Datalist(string starttime, string endtime, string key)
        {
            return View();
        }

        public JsonResult Machine_DataTable()
        {
            List<Machine_DataList> MachineList = new List<Machine_DataList>();
            DataTable dt = new DataTable();

            //判斷有無病人session
            if (Session["PatInfo"] != null)
            {
                //string sqlstr = " select * from VIPBPTBL where OP_ID = '" + userinfo.EmployeesNo + "' ";//判斷輸入者
                string sqlstr = " select * from VIPBPTBL where ";
                sqlstr += "STATUS = '2' AND PATIENT_ID ='" + ptinfo.ChartNo + "'order by DATA_TIME";
                
                dt = link.DBExecSQL(sqlstr);
                if (dt != null && dt.Rows.Count > 0)
                {
                    MachineList = (List<Machine_DataList>)dt.ToList<Machine_DataList>();
                }

                foreach (var item in MachineList)
                {
                    try
                    {
                        item.DATA_TIME = DateTime.ParseExact(item.DATA_TIME, "yyyyMMddHHmmss", null).ToString("yyyy/MM/dd HH:mm:ss");
                    }
                    catch (Exception)
                    {
                    }
                    try
                    {
                        item.DATA_TIME = DateTime.ParseExact(item.DATA_TIME, "yyyy-MM-dd HH:mm:ss", null).ToString("yyyy/MM/dd HH:mm:ss");
                    }
                    catch (Exception ex)
                    {//寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                        string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                        string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                        write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(),ex);
                    }

                    byte[] tempByte1 = webService.GetPatientInfo(item.PATIENT_ID);
                    PatientInfo list = null;
                    if (tempByte1 != null)
                    {
                        string ptinfoJosnArr = CompressTool.DecompressString(tempByte1);
                        list = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                        item.PATIENT_NAME = list.PatientName;
                    }
                    else
                    {
                        item.PATIENT_NAME = item.PATIENT_ID;
                    }
                }

                MachineList = MachineList.OrderByDescending(a => a.DATA_TIME).ToList();
            }
            else
            {
                Response.Write("<script>alert('請重新選擇病患');</script>");
                return new JsonResult { };
            }
            return new JsonResult { Data = MachineList, MaxJsonLength = int.MaxValue, JsonRequestBehavior = JsonRequestBehavior.AllowGet };


        }

        public ActionResult machine_modify(List<Machine_DataList> new_Mdatas)
        {
            RESPONSE_MSG json_result = new RESPONSE_MSG();
            List<DBItem> Machine_datas = new List<DBItem>();
            int row = 0;
            foreach (var item in new_Mdatas)
            {
                Machine_datas = new List<DBItem>();
                Machine_datas.Add(new DBItem("ID", item.ID, DBItem.DBDataType.String));
                Machine_datas.Add(new DBItem("OP_ID", item.OP_ID, DBItem.DBDataType.String));
                Machine_datas.Add(new DBItem("PATIENT_ID", item.PATIENT_ID, DBItem.DBDataType.String));
                Machine_datas.Add(new DBItem("DATA_TIME", Convert.ToDateTime(item.DATA_TIME).ToString("yyyyMMddHHmmss"), DBItem.DBDataType.String));
                Machine_datas.Add(new DBItem("PAIN", item.PAIN, DBItem.DBDataType.Number));
                Machine_datas.Add(new DBItem("BLOODSUGAR", item.BLOODSUGAR, DBItem.DBDataType.Number));
                Machine_datas.Add(new DBItem("CVP", item.CVP, DBItem.DBDataType.Number));
                Machine_datas.Add(new DBItem("AG", item.AG, DBItem.DBDataType.Number));
                Machine_datas.Add(new DBItem("HC", item.HC, DBItem.DBDataType.Number));
                Machine_datas.Add(new DBItem("FOOD", item.FOOD, DBItem.DBDataType.Number));
                Machine_datas.Add(new DBItem("IVF", item.IVF, DBItem.DBDataType.Number));
                Machine_datas.Add(new DBItem("TPN", item.TPN, DBItem.DBDataType.Number));
                Machine_datas.Add(new DBItem("BT", item.BT, DBItem.DBDataType.Number));
                Machine_datas.Add(new DBItem("URINE", item.URINE, DBItem.DBDataType.Number));
                Machine_datas.Add(new DBItem("STOOL", item.STOOL, DBItem.DBDataType.Number));
                Machine_datas.Add(new DBItem("DRAIN", item.DRAIN, DBItem.DBDataType.Number));
                Machine_datas.Add(new DBItem("BLOST", item.BLOST, DBItem.DBDataType.Number));
                Machine_datas.Add(new DBItem("OUTPUT_IRRIG", item.OUTPUT_IRRIG, DBItem.DBDataType.Number));
                Machine_datas.Add(new DBItem("UPLOAD_TIME", DateTime.Now.ToString("yyyyMMddHHmmss"), DBItem.DBDataType.String));

                this.link.DBExecUpdate("VIPBPTBL", Machine_datas, "ID ='" + item.ID + "' AND OP_ID ='" + userinfo.EmployeesNo + "'");
                row++;
            }
            if (new_Mdatas.Count == row)
            {
                json_result.status = RESPONSE_STATUS.SUCCESS;
                json_result.attachment = "Y";
            }
            else
            {
                json_result.attachment = "N";
                json_result.status = RESPONSE_STATUS.ERROR;
                json_result.message = "修改資料有誤";

            }
            //return new JsonResult { Data = Machine_datas, MaxJsonLength = int.MaxValue, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
            return Content(JsonConvert.SerializeObject(json_result), "application/json");

        }

        #region TPR

        //Tpr_查詢頁面
        public ActionResult Tpr_Index(string start, string end, string status)
        {
            try
            {
                if (string.IsNullOrEmpty(start))
                {
                    ViewBag.start = (DateTime.Now.AddDays(-4) < ptinfo.InDate) ? ptinfo.InDate : DateTime.Now.AddDays(-4);
                }
                else
                {
                    ViewBag.start = Convert.ToDateTime(start);
                }
                if (string.IsNullOrEmpty(end))
                {
                    ViewBag.end = DateTime.Now.ToString("yyyy/MM/dd");
                }
                else
                {
                    ViewBag.end = end;
                }
                ViewBag.inday = (Convert.ToDateTime(DateTime.Now.ToString("yyyy/MM/dd")) - Convert.ToDateTime(ptinfo.InDate.ToString("yyyy/MM/dd"))).Days;
                ViewBag.feeno = ptinfo.FeeNo;
                ViewBag.status = status;
                //Discharge time
                string discharge_time = get_discharge_time(ptinfo.FeeNo);
                int discharge_day = 0;
                if (discharge_time != "")
                    discharge_day = (Convert.ToDateTime(Convert.ToDateTime(discharge_time).ToString("yyyy/MM/dd")) - Convert.ToDateTime(DateTime.Now.ToString("yyyy/MM/dd"))).Days;
                ViewBag.discharge_time = discharge_time;
                ViewBag.discharge_day = discharge_day;
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

        //Tpr_報表頁面 OLD 1119
        public ActionResult Partial_Tpr_backup(string feeno, string start, string end)
        {
            IOManager iom = new IOManager();
            DateTime Start = Convert.ToDateTime(start);
            DateTime End = Convert.ToDateTime(end);
            try
            {
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
                if (tempByte != null)
                    ViewData["Aantibiotic"] = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(CompressTool.DecompressString(tempByte));
                //↑轉為明碼

                //取得T-Bilirubin 血球容積%  20170808 add by AlanHuang
                byte[] tempByte1 = webService.GetTBilHCT(feeno);
                if (tempByte1 != null)
                    ViewData["TBilHCT"] = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(CompressTool.DecompressString(tempByte1));


                //病人資訊
                if (ptinfoByteCode != null)
                {
                    string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                    PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                    pinfo = pi;
                }
                //特殊處置
                VitalSign vs_m = new VitalSign();
                DataTable procedure = vs_m.get_event(feeno);
                if (procedureByte != null)
                {
                    string procedureJson = CompressTool.DecompressString(procedureByte);
                    List<Procedure> procedureList = JsonConvert.DeserializeObject<List<Procedure>>(procedureJson);
                    procedure_list = procedureList;
                }
                //住院日期時間取得
                string sql = "select itemvalue from assessmentdetail where tableid in ";
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
                foreach (var item in temp)
                {
                    temp_.AddRange(item.DataList);
                    if (item.DataList.Exists(x => x.vs_item == "bt" && x.vs_record != "")
                        || item.DataList.Exists(x => x.vs_item == "mp" && x.vs_record != "")
                        || item.DataList.Exists(x => x.vs_item == "bf" && x.vs_record != ""))
                    {
                        dt.Add(item);
                    }
                }

                temp_.Sort((x, y) => { return y.create_date.CompareTo(Convert.ToDateTime(x.create_date).ToString("yyyy/MM/dd HH:mi:ss")); });
                List<int> CountList = new List<int>();
                List<String> DateList = new List<String>();
                for (DateTime s = Start; s <= End; s = s.AddDays(1))
                {
                    int cont = dt.FindAll(x => Convert.ToDateTime(x.recordtime).ToString("yyyy/MM/dd") == s.ToString("yyyy/MM/dd")).Count;
                    if (cont > 0)
                    {
                        for (int i = cont; i > 0; i = (i - 6))
                        {
                            if (i >= 6)
                                CountList.Add(6);
                            else
                                CountList.Add(i);
                            if (i == cont)
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
                //血糖
                DataTable dt_bs = bai.sql_BStable(feeno, "", "del", Start.ToString("yyyy/MM/dd 00:00:00"), End.AddDays(1).ToString("yyyy/MM/dd 00:00:00"));
                //IO
                DataTable dt_i_item = iom.sel_sys_params_kind("iotype", "intaketype");
                DataTable dt_o_item = iom.sel_sys_params_kind("iotype", "outputtype");
                DataTable dt_io_data = iom.sel_io_data("", feeno, "", Start.ToString("yyyy/MM/dd 07:01:00"), End.AddDays(1).ToString("yyyy/MM/dd 07:00:59"), "1");
                string[] column = { "DATE", "P_VALUE", "AMOUNT", "CLORIE", "REASON", "TYPE" };
                iom.set_dt_column(dt_io, column);
                iom.set_new_list(dt_io_data, dt_i_item, dt_o_item, dt_io, Start, End);
                ViewBag.dt_bs = dt_bs;
                ViewBag.dt_io = dt_io;
                ViewBag.dt_i_item = dt_i_item;
                ViewBag.dt_o_item = dt_o_item;
                //OP Day
                ViewBag.dt_op_day = set_special_event(pinfo.FeeNo, "3", Start, End, "OP Day", int.MaxValue);
                //Delivered at
                ViewBag.dt_d_day = set_special_event(pinfo.FeeNo, "2", Start, End, "D Day", 4);
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
                ViewData["CVVH"] = cvvh_d.get_cvvh_data_list(feeno, Start, End);
                return View();
            }
            catch (Exception ex)
            {
                Response.Write("與HIS串接錯誤，請與資訊室聯繫，詳細錯誤訊息如下：");
                Response.Write(ex.Message);
                return new EmptyResult();
            }
            finally
            {
                this.link.DBClose();
            }
        }

        /// <summary>
        /// Tpr 報表頁面(New)
        /// </summary>
        /// <param name="feeno">批價序號</param>
        /// <param name="start">起</param>
        /// <param name="end">迄</param>
        /// <param name="print_set">需要隱藏的項目</param>
        /// <param name="source">判斷是否為HisView來源</param>
        /// <param name="DrawDay">單張繪製天數(預設5天，列印時固定5天)</param>
        /// <returns></returns>
        public ActionResult Partial_Tpr(string feeno, string start, string end, string print_set, string source = "", string DrawDay = "5")
        {
            string str_HISVIEW = "";
            PatientInfo pinfo = new PatientInfo();
            //病人資訊
            if (ptinfo != null && ptinfo.FeeNo == feeno)
            {
                pinfo = ptinfo;
                
                if (Session["TPR"] != null)
                {
                    if ((string)Session["TPR"] == "HISVIEW")
                    {
                        str_HISVIEW = "HISVIEW";
                    }
                }
            }
            else
            {
                byte[] ptinfoByteCode = webService.GetPatientInfo(feeno);
                if (ptinfoByteCode != null)
                {
                    string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                    PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                    pinfo = pi;
                }
            }
            if (pinfo != null)
            {
                ViewBag.start = start;
                ViewBag.end = end;
                ViewBag.feeno = feeno;
                ViewBag.source = source;
                ViewBag.print_set = print_set;
                ViewBag.str_HISVIEW = str_HISVIEW;
                ViewBag.draw_day = DrawDay;
                ViewData["ptinfo"] = pinfo;
            }
            else
            {
                Response.Write("System error：Unable to obtain patient information!");
                return new EmptyResult();
            }
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
        }
        // Patrick 20240614 血透 透析脫水量
        public List<DB_NIS_Dehydration> getDehydrationAmount(string feeno, string start, string end)
        {
            List<DB_NIS_Dehydration> DehydrationData = new List<DB_NIS_Dehydration>();
            var chrno = "";

            byte[] ptinfoByteCode = webService.GetPatientInfo(feeno.Trim());
            if (ptinfoByteCode != null)
            {
                string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                chrno = pi.ChartNo;
            }
            byte[] data = Encoding.UTF8.GetBytes("pat_no=" + chrno + "&inp_date=" + start + "&outp_date=" + end + "");
            //測試用資料
            try
            {
                //byte[] data = Encoding.UTF8.GetBytes("pat_no=0000346389&inp_date=2024-05-01&outp_date=");
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://172.20.100.30:82/PatHDReport.asmx/GetPatientHDRecord");
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = data.Length;
                request.Timeout = 3000;
                Stream requestStream = request.GetRequestStream();
                requestStream.Write(data, 0, data.Length);
                requestStream.Close();

                WebResponse response = request.GetResponse();
                string content = new StreamReader(response.GetResponseStream()).ReadToEnd();
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(content);

                string jsonText = xmlDoc.InnerText;
                List<string> calculations = new List<string>();

                string calculation = "";
                var Result = JsonConvert.DeserializeObject<List<TPR_Data>>(jsonText);
                foreach (var item in Result)
                {
                    if (item.MachineUF == "" || item.NSWeight == "" || item.FinishDateTime == null || item.BeginDateTime == null)
                    {

                        calculation = "資料不正確";
                    }
                    else
                    {
                        double duration = (item.FinishDateTime - item.BeginDateTime).TotalHours;
                        double machineUF = double.Parse(item.MachineUF);
                        double nsWeight = double.Parse(item.NSWeight);
                        double Dialysis = (machineUF - nsWeight);
                        string DialysisSt = Dialysis.ToString();
                        string durationSt = duration.ToString();
                        calculation = DialysisSt + "kg/" + durationSt + "hrs";
                    }

                    if (item.FinishDateTime != null || item.BeginDateTime != null)
                    {
                        DB_NIS_Dehydration dnv = new DB_NIS_Dehydration
                        {
                            CREATTIME = item.BeginDateTime,
                            CONTENT = calculation,
                        };
                        DehydrationData.Add(dnv);
                    }
                }
            }
            catch
            {

            }
         
            return DehydrationData;
        }
        /// <summary>
        /// Tpr 報表資料
        /// </summary>
        /// <param name="feeno">批價序號</param>
        /// <param name="start">起</param>
        /// <param name="end">迄</param>
        /// <param name="draw_day">單張繪製天數</param>
        /// <returns></returns>
        public JsonResult Partial_Tpr_Data(string feeno, string start, string end, string draw_day = "5")
        {
            List<DB_NIS_Dehydration> Dehydration = getDehydrationAmount(feeno, start, end);
            getSession(feeno); //HISVIEW 不走 MainController,因此另外處理session
            string str_HISVIEW = "";
            if (Session["TPR"] !=null)
            {
                if ((string)Session["TPR"] == "HISVIEW")
                {
                    str_HISVIEW = "HISVIEW";
                }                
            }
            if (ptinfo == null)
            {
                ptinfo = (PatientInfo)Session["PatInfo"];
            }
            PatientInfo pinfo = new PatientInfo();
            RESPONSE_MSG json_result = new RESPONSE_MSG();
            ViewTpr view_tpr = new ViewTpr();
            List<TprData> tpr_data_list = new List<TprData>();
            //病人資訊(此程序列印時為必要)
            if (ptinfo != null && ptinfo.FeeNo == feeno)
            {
                pinfo = ptinfo;
            }
            else
            {
                byte[] ptinfoByteCode = webService.GetPatientInfo(feeno);
                if (ptinfoByteCode != null)
                {
                    string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                    PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                    pinfo = pi;
                }
            }
            try
            {
                //宣告
                //List<Procedure> procedure_list = new List<Procedure>();
                //DataTable dt_op_day = new DataTable();
                //DataTable dt_d_day = new DataTable();
                DataTable dt = new DataTable();
                DataTable dt_io = new DataTable();
                DateTime start_date = DateTime.Now;
                DateTime end_date = DateTime.Now;
                view_tpr.in_date = pinfo.InDate.ToString("yyyy-MM-dd");

                //日期格式轉換
                if (start.Count() == 8)
                {
                    start_date = DateTime.ParseExact(start, "yyyyMMdd", CultureInfo.InvariantCulture);
                }
                else
                {
                    start = start.Replace("上午", "AM").Replace("下午", "PM");
                    DateTime.TryParse(start, out start_date);
                }
                if (end.Count() == 8)
                {
                    end_date = DateTime.ParseExact(end, "yyyyMMdd", CultureInfo.InvariantCulture);
                }
                else
                {
                    end = end.Replace("上午", "AM").Replace("下午", "PM");
                    DateTime.TryParse(end, out end_date);
                }
                end_date = end_date.Date.AddDays(1).AddSeconds(-1);

                view_tpr.feeno = feeno;
                view_tpr.start_date = start_date.ToString("yyyy-MM-dd HH:mm:ss");
                view_tpr.end_date = end_date.ToString("yyyy-MM-dd HH:mm:ss");
                int int_draw_day = 0;
                if (int.TryParse(draw_day, out int_draw_day))
                {
                    view_tpr.draw_day = int_draw_day;
                }

                //特殊註記事項
                VitalSign vs_m = new VitalSign();
                DataTable SEvents_dt = vs_m.get_event(feeno, "", "3,4,5,11,19,20", start_date.ToString("yyyy-MM-dd HH:mm:ss"), end_date.ToString("yyyy-MM-dd HH:mm:ss"));
                List<DB_NIS_SPECIALEVENT> SEvents_list = (List<DB_NIS_SPECIALEVENT>)SEvents_dt.ToList<DB_NIS_SPECIALEVENT>();
                string sql = "select itemvalue from assessmentdetail where tableid in ";
                sql += "(select tableid from assessmentmaster where feeno = '" + feeno + "' and createtime = ";
                sql += "(select min(createtime) from assessmentmaster where feeno = '" + feeno + "' and deleted is null and status <> 'temporary' )) ";
                sql += "and itemid in ('param_tube_date','param_tube_time','param_assessment')";
                DataTable tmp_dt = this.link.DBExecSQL(sql);
                DateTime datetime = DateTime.MaxValue;
                if (tmp_dt.Rows.Count == 3 && tmp_dt.Rows[2][0].ToString() == "入院")
                {
                    datetime = Convert.ToDateTime(tmp_dt.Rows[0][0].ToString() + " " + tmp_dt.Rows[1][0].ToString());
                }
                if (tmp_dt.Rows.Count > 0)
                {
                    DB_NIS_VITALSIGN dnv = new DB_NIS_VITALSIGN
                    {
                        CREATE_DATE = datetime,
                        MODIFY_DATE = datetime,
                        VS_ITEM = "special_events",
                        VS_RECORD = "Admitted at:" + datetime.ToString("HH:mm")
                    };
                    add_TprData(ref tpr_data_list, datetime, dnv, "special_events");
                }

                foreach (DB_NIS_SPECIALEVENT SEvents_data in SEvents_list)
                {
                    DB_NIS_VITALSIGN dnv = new DB_NIS_VITALSIGN
                    {
                        CREATE_DATE = SEvents_data.CREATTIME,
                        MODIFY_DATE = SEvents_data.CREATTIME,
                        VS_ITEM = SEvents_data.TYPE_ID.ToString(),
                        VS_RECORD = SEvents_data.CONTENT.ToString()
                    };
                    add_TprData(ref tpr_data_list, SEvents_data.CREATTIME, dnv, "special_events");
                }
               // Patrick 20240626 血透 透析脫水量
                foreach (DB_NIS_Dehydration Dehydration_data in Dehydration)
                {
                    DB_NIS_VITALSIGN dnv = new DB_NIS_VITALSIGN
                    {
                        CREATE_DATE = Dehydration_data.CREATTIME,
                        MODIFY_DATE = Dehydration_data.CREATTIME,
                        VS_ITEM = "Dehydration",
                        VS_RECORD = Dehydration_data.CONTENT.ToString()
                    };
                    add_TprData(ref tpr_data_list, Dehydration_data.CREATTIME, dnv, "Dehydration");
                }
                //檢驗檢查異常值	
                byte[] procedureByte = webService.GetProcedure(feeno);
                if (procedureByte != null)
                {
                    string procedureJson = CompressTool.DecompressString(procedureByte);
                    List<Procedure> procedureList = JsonConvert.DeserializeObject<List<Procedure>>(procedureJson);
                    foreach (Procedure procedure in procedureList)
                    {
                        DB_NIS_VITALSIGN dnv = new DB_NIS_VITALSIGN
                        {
                            CREATE_DATE = procedure.ProcedureDate,
                            MODIFY_DATE = procedure.ProcedureDate,
                            VS_ITEM = "Procedure",
                            VS_RECORD = procedure.ProcedureCode.Replace("#br#", "<br />")
                        };
                        add_TprData(ref tpr_data_list, procedure.ProcedureDate, dnv, "Procedure");
                    }
                    //procedure_list = procedureList;
                }
                //特殊處置
                DataTable procedure_dt = vs_m.get_event(feeno, "4", "", start_date.ToString("yyyy-MM-dd HH:mm:ss"), end_date.ToString("yyyy-MM-dd HH:mm:ss"));
                List<DB_NIS_SPECIALEVENT> procedure_list = (List<DB_NIS_SPECIALEVENT>)procedure_dt.ToList<DB_NIS_SPECIALEVENT>();
                foreach (DB_NIS_SPECIALEVENT procedure_data in procedure_list)
                {
                    DB_NIS_VITALSIGN dnv = new DB_NIS_VITALSIGN
                    {
                        CREATE_DATE = procedure_data.CREATTIME,
                        MODIFY_DATE = procedure_data.CREATTIME,
                        VS_ITEM = procedure_data.TYPE_ID.ToString(),
                        VS_RECORD = procedure_data.CONTENT.ToString()
                    };
                    add_TprData(ref tpr_data_list, procedure_data.CREATTIME, dnv, "SpecialProcedure");
                }

                if (!string.IsNullOrEmpty(feeno))
                {
                    //
                    DateTime Start = Convert.ToDateTime(start);
                    DateTime END = Convert.ToDateTime(end).AddDays(1).AddSeconds(-1);
                    List<VitalSignDataList> temp = vs_m.sel_vital(feeno, Start, END, "",str_HISVIEW);
                    List<VitalSignDataList> date_list = new List<VitalSignDataList>();
                    List<VitalSignData> temp_ = new List<VitalSignData>();
                    foreach (var item in temp)
                    {
                        temp_.AddRange(item.DataList);
                        if (item.DataList.Exists(x => x.vs_item == "bt" && x.vs_record != "")
                            || item.DataList.Exists(x => x.vs_item == "mp" && x.vs_record != "")
                            || item.DataList.Exists(x => x.vs_item == "bf" && x.vs_record != ""))
                        {
                            date_list.Add(item);
                        }
                    }

                    temp_.Sort((x, y) => { return y.create_date.CompareTo(Convert.ToDateTime(x.create_date).ToString("yyyy/MM/dd HH:mi:ss")); });

                    foreach (var r in date_list)
                    {
                        if (!string.IsNullOrEmpty(r.recordtime.ToString()))
                        {
                            DateTime Sdatetime = Convert.ToDateTime(r.recordtime.ToString());
                            DB_NIS_VITALSIGN dnv = new DB_NIS_VITALSIGN
                            {
                                CREATE_DATE = Sdatetime,
                                MODIFY_DATE = Sdatetime,
                                VS_ITEM = "date_list",
                                VS_RECORD = Sdatetime.ToString("HH:mm")
                            };
                            add_TprData(ref tpr_data_list, Sdatetime, dnv, "date_list");
                        }
                    }
                    //
                    //取得手術日期 ODDays
                    DataTable dt_op_day = set_special_event(pinfo.FeeNo, "3", Convert.ToDateTime(start), Convert.ToDateTime(end), "OP Day", int.MaxValue);
                    foreach (DataRow r in dt_op_day.Rows)
                    {
                        DB_NIS_VITALSIGN dnvO = new DB_NIS_VITALSIGN();
                        if (!string.IsNullOrEmpty(r["CONTENT"].ToString()))
                        {
                            dnvO = new DB_NIS_VITALSIGN
                            {
                                CREATE_DATE = Convert.ToDateTime(r["TIME"]),
                                MODIFY_DATE = Convert.ToDateTime(r["TIME"]),
                                VS_ITEM = "ODays",
                                VS_RECORD = r["CONTENT"].ToString()
                            };
                        }
                        add_TprData(ref tpr_data_list, Convert.ToDateTime(r["TIME"]), dnvO, "ODDays", "ODays");
                    }
                    DataTable dt_d_day = set_special_event(pinfo.FeeNo, "2", Convert.ToDateTime(start), Convert.ToDateTime(end), "D Day", 4);
                    foreach (DataRow r in dt_d_day.Rows)
                    {
                        DB_NIS_VITALSIGN dnvD = new DB_NIS_VITALSIGN();

                        dnvD = new DB_NIS_VITALSIGN
                        {
                            CREATE_DATE = Convert.ToDateTime(r["TIME"]),
                            MODIFY_DATE = Convert.ToDateTime(r["TIME"]),
                            VS_ITEM = "DDays",
                            VS_RECORD = r["CONTENT"].ToString()
                        };
                        add_TprData(ref tpr_data_list, Convert.ToDateTime(r["TIME"]), dnvD, "ODDays", "DDays");
                    }
                    //取得VitalSign Data 
                    List<DB_NIS_VITALSIGN> dnv_list = get_DbNisVitalsign(feeno, start_date, end_date, str_HISVIEW);

                    //篩選Height Weight
                    List<DB_NIS_VITALSIGN> bh_bw_list = dnv_list.FindAll(dl => dl.VS_ITEM == "bh" || dl.VS_ITEM == "bw");
                    foreach (DB_NIS_VITALSIGN bh_bw in bh_bw_list)
                    {
                        add_TprData(ref tpr_data_list, bh_bw.CREATE_DATE, bh_bw, "HeightWeight");
                    }
                    //篩選SPO2
                    List<DB_NIS_VITALSIGN> spo2_list = dnv_list.FindAll(dl => dl.MODIFY_DATE >= start_date && dl.MODIFY_DATE <= end_date && dl.VS_ITEM == "sp");
                    foreach (DB_NIS_VITALSIGN spo2 in spo2_list)
                    {
                        add_TprData(ref tpr_data_list, spo2.CREATE_DATE, spo2, "SPO2");
                    }
                    //篩選ComaScale
                    List<DB_NIS_VITALSIGN> comaScale_list = dnv_list.FindAll(dl => dl.VS_ITEM == "gc");
                    foreach (DB_NIS_VITALSIGN r in comaScale_list)
                    {
                        string[] record = r.VS_RECORD.ToString().Split('|');

                        if (!string.IsNullOrEmpty(r.CREATE_DATE.ToString()))
                        {
                            DB_NIS_VITALSIGN dnv = new DB_NIS_VITALSIGN
                            {
                                CREATE_DATE = Convert.ToDateTime(r.CREATE_DATE.ToString()),
                                MODIFY_DATE = Convert.ToDateTime(r.CREATE_DATE.ToString()),
                                VS_ITEM = "ComaScale",
                                VS_RECORD = record[0].Substring(1, 1).ToString()
                            };
                            add_TprData(ref tpr_data_list, r.CREATE_DATE, dnv, "ComaScale", "E");
                            dnv = new DB_NIS_VITALSIGN
                            {
                                CREATE_DATE = Convert.ToDateTime(r.CREATE_DATE.ToString()),
                                MODIFY_DATE = Convert.ToDateTime(r.CREATE_DATE.ToString()),
                                VS_ITEM = "ComaScale",
                                VS_RECORD = record[1].Substring(1, 1).ToString()
                            };
                            add_TprData(ref tpr_data_list, r.CREATE_DATE, dnv, "ComaScale", "V");
                            dnv = new DB_NIS_VITALSIGN
                            {
                                CREATE_DATE = Convert.ToDateTime(r.CREATE_DATE.ToString()),
                                MODIFY_DATE = Convert.ToDateTime(r.CREATE_DATE.ToString()),
                                VS_ITEM = "ComaScale",
                                VS_RECORD = record[2].Substring(1, 1).ToString()
                            };
                            add_TprData(ref tpr_data_list, r.CREATE_DATE, dnv, "ComaScale", "M");
                        }

                    }
                    //篩選瞳孔反應
                    List<DB_NIS_VITALSIGN> pupils_list = dnv_list.FindAll(dl => dl.MODIFY_DATE >= start_date && dl.MODIFY_DATE <= end_date && dl.VS_ITEM == "pupils");
                    foreach (DB_NIS_VITALSIGN pupils in pupils_list)
                    {
                        add_TprData(ref tpr_data_list, pupils.CREATE_DATE, pupils, "Pupils");
                    }
                    //篩選肌肉強度
                    List<DB_NIS_VITALSIGN> msPower_list = dnv_list.FindAll(dl => dl.MODIFY_DATE >= start_date && dl.MODIFY_DATE <= end_date && dl.VS_ITEM == "msPower");
                    foreach (DB_NIS_VITALSIGN msPower in msPower_list)
                    {
                        add_TprData(ref tpr_data_list, msPower.CREATE_DATE, msPower, "MsPower");
                    }
                    //篩選血壓
                    List<DB_NIS_VITALSIGN> bp_list = dnv_list.FindAll(dl => dl.MODIFY_DATE >= start_date && dl.MODIFY_DATE <= end_date && dl.VS_ITEM == "bp");
                    foreach (DB_NIS_VITALSIGN bp in bp_list)
                    {
                        if (bp.VS_RECORD.IndexOf('|') > 0)
                        {
                            string[] a = bp.VS_RECORD.Replace("#", "").Replace("|", "/").Split('/');
                            double S_bp_temp = 0;
                            double D_bp_temp = 0;
                            string S_bp_temp_get = "";
                            string D_bp_temp_get = "";
                            string vitalTempTotal = "";
                            S_bp_temp_get = (!string.IsNullOrEmpty(a[0])) ? a[0].ToString() : "0";
                            int int_S_bp_temp = 0;
                            int.TryParse(S_bp_temp_get, out int_S_bp_temp);
                            S_bp_temp = int_S_bp_temp;

                            D_bp_temp_get = (!string.IsNullOrEmpty(a[1])) ? a[1].ToString() : "0";
                            int int_D_bp_temp = 0;
                            int.TryParse(D_bp_temp_get, out int_D_bp_temp);
                            D_bp_temp = int_D_bp_temp;

                            if (!string.IsNullOrEmpty(S_bp_temp_get) && !string.IsNullOrEmpty(D_bp_temp_get))
                            {
                                if (a.Length > 2)
                                {
                                    vitalTempTotal = (string.IsNullOrEmpty(a.GetValue(2).ToString()) ? Math.Round(S_bp_temp / 3 + (D_bp_temp / 3 * 2)).ToString() : a.GetValue(2).ToString());
                                }
                                else
                                {
                                    vitalTempTotal = Math.Round(S_bp_temp / 3 + (D_bp_temp / 3 * 2)).ToString();
                                }
                            }
                            bp.VS_RECORD = a[0] + "/" + a[1] + "(" + vitalTempTotal + ")";
                        }
                        add_TprData(ref tpr_data_list, bp.CREATE_DATE, bp, "BloodPressureArea");
                    }
                    //取得血糖(One Touch)
                    DataTable dt_bsugar = bai.sql_BStable(feeno, "", "del", Convert.ToDateTime(start).ToString("yyyy/MM/dd 00:00:00"), Convert.ToDateTime(end).AddDays(1).ToString("yyyy/MM/dd 00:00:00"));
                    DataTable dt_check = Get_Check_Abnormal_dt();
                    List<NIS_SYS_VITALSIGN_OPTION> dt_check_list = (List<NIS_SYS_VITALSIGN_OPTION>)dt_check.ToList<NIS_SYS_VITALSIGN_OPTION>();
                    string l_check = "bsl", h_check = "bsh", low_check = "bsl_low", high_check = "bsh_high";
                    NIS_SYS_VITALSIGN_OPTION l_check_list = dt_check_list.Find(x => x.MODEL_ID == l_check); //<
                    NIS_SYS_VITALSIGN_OPTION low_check_list = dt_check_list.Find(x => x.MODEL_ID == low_check);//<
                    NIS_SYS_VITALSIGN_OPTION h_check_list = dt_check_list.Find(x => x.MODEL_ID == h_check);//>=
                    NIS_SYS_VITALSIGN_OPTION high_check_list = dt_check_list.Find(x => x.MODEL_ID == high_check);//>=

                    foreach (DataRow r in dt_bsugar.Rows)
                    {
                        string INDATE = r["INDATE"].ToString(); //該時間為頁面上顯示時間  以記錄時間
                        if (!string.IsNullOrEmpty(r["BLOODSUGAR"].ToString()))
                        {
                            int Blood = int.Parse(r["BLOODSUGAR"].ToString());
                            string value_status = "0";
                            if (Blood < double.Parse(l_check_list.VALUE_LIMIT.ToString()))
                            {
                                if (Blood < double.Parse(low_check_list.VALUE_LIMIT.ToString()))
                                {
                                    value_status = "2";
                                }
                                else
                                {
                                    value_status = "1";
                                }
                            }
                            else if (Blood >= double.Parse(h_check_list.VALUE_LIMIT.ToString()))
                            {
                                if (Blood >= double.Parse(high_check_list.VALUE_LIMIT.ToString()))
                                {
                                    value_status = "4";
                                }
                                else
                                {
                                    value_status = "3";
                                }
                            }


                            DB_NIS_VITALSIGN dnv = new DB_NIS_VITALSIGN
                            {
                                CREATE_DATE = Convert.ToDateTime(r["INDATE"]),
                                MODIFY_DATE = Convert.ToDateTime(INDATE),
                                VS_ITEM = "Blood Sugar",
                                VS_RECORD = r["MEAL_STATUS"] + "|" + r["BLOODSUGAR"].ToString() + " mg/dl "
                            };
                            add_TprData(ref tpr_data_list, Convert.ToDateTime(INDATE), dnv, "BloodSugarArea", "", value_status);
                        }
                    }
                    //篩選飲食
                    List<DB_NIS_VITALSIGN> eat_list = dnv_list.FindAll(dl => dl.MODIFY_DATE >= start_date && dl.MODIFY_DATE <= end_date && dl.VS_ITEM == "eat");
                    foreach (DB_NIS_VITALSIGN eat in eat_list)
                    {
                        //string[] record = eat.VS_RECORD.Split('|');

                        //string content = (record[0] != "") ? record[0] :record[1];

                        //eat.VS_RECORD = content;
                        add_TprData(ref tpr_data_list, eat.CREATE_DATE, eat, "Eat");
                    }
                    //取得Antibiotic     //抗生素
                    List<TPRAantibiotic> antibiotics_list = new List<TPRAantibiotic>();
                    byte[] tempByte = webService.GetTPRAantibiotic(feeno);
                    if (tempByte != null)
                    {
                        antibiotics_list = JsonConvert.DeserializeObject<List<TPRAantibiotic>>(CompressTool.DecompressString(tempByte));
                    }
                    antibiotics_list = antibiotics_list.FindAll(x => x.UseDate >= Start && x.UseDate <= END).ToList();
                    foreach (var r in antibiotics_list)
                    {
                        if (!string.IsNullOrEmpty(r.UseDate.ToString()))
                        {
                            DB_NIS_VITALSIGN dnv = new DB_NIS_VITALSIGN
                            {
                                CREATE_DATE = Convert.ToDateTime(r.UseDate.ToString()),
                                MODIFY_DATE = Convert.ToDateTime(r.UseDate.ToString()),
                                VS_ITEM = "Antibiotic/LASIX",
                                VS_RECORD = r.content.ToString()
                            };
                            add_TprData(ref tpr_data_list, Convert.ToDateTime(r.UseDate), dnv, "Antibiotic");
                        }
                    }
                    //篩選總膽紅素
                    List<DB_NIS_VITALSIGN> TBilHCT_list = dnv_list.FindAll(dl => dl.MODIFY_DATE >= start_date && dl.MODIFY_DATE <= end_date && dl.VS_ITEM == "gi_j");
                    foreach (DB_NIS_VITALSIGN TBilHCT in TBilHCT_list)
                    {
                        //string record = TBilHCT.VS_RECORD.Replace("|","");

                        //TBilHCT.VS_RECORD = content;
                        add_TprData(ref tpr_data_list, TBilHCT.CREATE_DATE, TBilHCT, "TBilHCT");
                    }
                    //取得血球容積比
                    List<TPRTBilHCT> HCT_list = new List<TPRTBilHCT>();
                    tempByte = webService.GetTBilHCT(feeno);
                    if (tempByte != null)
                    {
                        HCT_list = JsonConvert.DeserializeObject<List<TPRTBilHCT>>(CompressTool.DecompressString(tempByte));
                    }
                    HCT_list = HCT_list.FindAll(x => x.UseDate > Start && x.UseDate < END).ToList();
                    foreach (var r in HCT_list)
                    {
                        if (!string.IsNullOrEmpty(r.HCT.ToString()))
                        {
                            DB_NIS_VITALSIGN dnv = new DB_NIS_VITALSIGN
                            {
                                CREATE_DATE = Convert.ToDateTime(r.UseDate.ToString()),
                                MODIFY_DATE = Convert.ToDateTime(r.UseDate.ToString()),
                                VS_ITEM = "HCT",
                                VS_RECORD = r.HCT.ToString()
                            };
                            add_TprData(ref tpr_data_list, Convert.ToDateTime(r.UseDate), dnv, "HCT");
                        }
                    }
                    //取得CVVH
                    List<CVVH_Data.CVVH_TPR_Data> CVVH_list = cvvh_d.get_cvvh_data_list(feeno, Start, END);
                    foreach (CVVH_Data.CVVH_TPR_Data CVVH in CVVH_list)
                    {
                        DateTime create_date = new DateTime();
                        if (DateTime.TryParse(CVVH.DataDate, out create_date))
                        {
                            DB_NIS_VITALSIGN dnv = new DB_NIS_VITALSIGN
                            {
                                CREATE_DATE = create_date,
                                MODIFY_DATE = create_date,
                                VS_ITEM = "CVVH",
                                VS_RECORD = CVVH.IO_Total
                            };
                            add_TprData(ref tpr_data_list, create_date, dnv, "CVVH");
                        }
                    }
                    //篩選排便狀況
                    List<DB_NIS_VITALSIGN> excrement_list = dnv_list.FindAll(dl => dl.MODIFY_DATE >= start_date 
                    && dl.MODIFY_DATE <= end_date.AddDays(1) 
                    && dl.VS_ITEM == "st" 
                    && dl.VS_RECORD != ""
                    && dl.VS_RECORD != "|" 
                    //&& dl.VS_RECORD != "0"
                    && dl.VS_RECORD != "|Option|Option");
                    foreach (DB_NIS_VITALSIGN excrement in excrement_list)
                    {
                        add_TprData(ref tpr_data_list, excrement.CREATE_DATE, excrement, "Excrement");
                    }

                    //篩選CVP2
                    List<DB_NIS_VITALSIGN> cvp1_list = dnv_list.FindAll(dl => dl.MODIFY_DATE >= start_date && dl.MODIFY_DATE <= end_date && dl.VS_ITEM == "cv1" && !string.IsNullOrEmpty(dl.VS_RECORD));
                    foreach (DB_NIS_VITALSIGN cvp1 in cvp1_list)
                    {
                        cvp1.VS_RECORD = cvp1.VS_RECORD + "mmHg";
                        add_TprData(ref tpr_data_list, cvp1.CREATE_DATE, cvp1, "CVP2");
                    }
                    List<DB_NIS_VITALSIGN> cvp2_list = dnv_list.FindAll(dl => dl.MODIFY_DATE >= start_date && dl.MODIFY_DATE <= end_date && dl.VS_ITEM == "cv2");
                    foreach (DB_NIS_VITALSIGN cvp2 in cvp2_list)
                    {
                        cvp2.VS_RECORD = cvp2.VS_RECORD + "cmH2O";
                        add_TprData(ref tpr_data_list, cvp2.CREATE_DATE, cvp2, "CVP2");
                    }
                    //篩選ICP2
                    List<DB_NIS_VITALSIGN> icp1_list = dnv_list.FindAll(dl => dl.MODIFY_DATE >= start_date && dl.MODIFY_DATE <= end_date && dl.VS_ITEM == "ic1" && !string.IsNullOrEmpty(dl.VS_RECORD));
                    foreach (DB_NIS_VITALSIGN icp1 in icp1_list)
                    {
                        icp1.VS_RECORD = icp1.VS_RECORD + "mmHg";
                        add_TprData(ref tpr_data_list, icp1.CREATE_DATE, icp1, "ICP2");
                    }
                    List<DB_NIS_VITALSIGN> icp2_list = dnv_list.FindAll(dl => dl.MODIFY_DATE >= start_date && dl.MODIFY_DATE <= end_date && dl.VS_ITEM == "ic2");
                    foreach (DB_NIS_VITALSIGN icp2 in icp2_list)
                    {
                        icp2.VS_RECORD = icp2.VS_RECORD + "cmH2O";
                        add_TprData(ref tpr_data_list, icp2.CREATE_DATE, icp2, "ICP2");
                    }
                    //篩選CPP
                    List<DB_NIS_VITALSIGN> cpp_list = dnv_list.FindAll(dl => dl.MODIFY_DATE >= start_date && dl.MODIFY_DATE <= end_date && dl.VS_ITEM == "cpp" && !string.IsNullOrEmpty(dl.VS_RECORD));
                    foreach (DB_NIS_VITALSIGN cpp in cpp_list)
                    {
                        cpp.VS_RECORD = cpp.VS_RECORD + "mmHg";
                        add_TprData(ref tpr_data_list, cpp.CREATE_DATE, cpp, "CPP");
                    }
                    //篩選ABP2
                    List<DB_NIS_VITALSIGN> abp2_list = dnv_list.FindAll(dl => dl.MODIFY_DATE >= start_date && dl.MODIFY_DATE <= end_date && dl.VS_ITEM == "abp" && dl.VS_RECORD != "|");
                    foreach (DB_NIS_VITALSIGN abp2 in abp2_list)
                    {
                        if (abp2.VS_RECORD.IndexOf('|') > 0)
                        {
                            string[] a = abp2.VS_RECORD.Replace("#", "").Replace("|", "/").Split('/');
                            double S_temp = 0;
                            double D_temp = 0;
                            string S_temp_get = "";
                            string D_temp_get = "";
                            string vitalTempTotal = "";
                            S_temp_get = (!string.IsNullOrEmpty(a[0])) ? a[0].ToString() : "0";
                            int int_S_temp = 0;
                            int.TryParse(S_temp_get, out int_S_temp);
                            S_temp = int_S_temp;

                            D_temp_get = (!string.IsNullOrEmpty(a[1])) ? a[1].ToString() : "0";
                            int int_D_temp = 0;
                            int.TryParse(D_temp_get, out int_D_temp);
                            D_temp = int_D_temp;

                            if (!string.IsNullOrEmpty(S_temp_get) && !string.IsNullOrEmpty(D_temp_get))
                            {
                                vitalTempTotal = Math.Round(S_temp / 3 + (D_temp / 3 * 2)).ToString();
                            }
                            abp2.VS_RECORD = a[0] + "/" + a[1] + "(" + vitalTempTotal + ")";
                        }
                        add_TprData(ref tpr_data_list, abp2.CREATE_DATE, abp2, "ABP2");
                    }
                    //篩選PA
                    List<DB_NIS_VITALSIGN> pa_list = dnv_list.FindAll(dl => dl.MODIFY_DATE >= start_date && dl.MODIFY_DATE <= end_date && dl.VS_ITEM == "pa" && dl.VS_RECORD != "|");
                    foreach (DB_NIS_VITALSIGN pa in pa_list)
                    {
                        if (pa.VS_RECORD.IndexOf('|') > 0)
                        {
                            string[] a = pa.VS_RECORD.Replace("#", "").Replace("|", "/").Split('/');
                            double S_temp = 0;
                            double D_temp = 0;
                            string S_temp_get = "";
                            string D_temp_get = "";
                            string vitalTempTotal = "";
                            S_temp_get = (!string.IsNullOrEmpty(a[0])) ? a[0].ToString() : "0";
                            int int_S_temp = 0;
                            int.TryParse(S_temp_get, out int_S_temp);
                            S_temp = int_S_temp;

                            D_temp_get = (!string.IsNullOrEmpty(a[1])) ? a[1].ToString() : "0";
                            int int_D_temp = 0;
                            int.TryParse(D_temp_get, out int_D_temp);
                            D_temp = int_D_temp;

                            if (!string.IsNullOrEmpty(S_temp_get) && !string.IsNullOrEmpty(D_temp_get))
                            {
                                vitalTempTotal = Math.Round(S_temp / 3 + (D_temp / 3 * 2)).ToString();
                            }
                            pa.VS_RECORD = a[0] + "/" + a[1] + "(" + vitalTempTotal + ")";
                        }
                        add_TprData(ref tpr_data_list, pa.CREATE_DATE, pa, "PA");
                    }
                    //篩選CPP
                    List<DB_NIS_VITALSIGN> pcwp_list = dnv_list.FindAll(dl => dl.MODIFY_DATE >= start_date && dl.MODIFY_DATE <= end_date && dl.VS_ITEM == "pcwp" && !string.IsNullOrEmpty(dl.VS_RECORD));
                    foreach (DB_NIS_VITALSIGN pcwp in pcwp_list)
                    {
                        pcwp.VS_RECORD = pcwp.VS_RECORD + "mmHg";
                        add_TprData(ref tpr_data_list, pcwp.CREATE_DATE, pcwp, "PCWP");
                    }
                    //篩選ETCO2
                    List<DB_NIS_VITALSIGN> etco_list = dnv_list.FindAll(dl => dl.MODIFY_DATE >= start_date && dl.MODIFY_DATE <= end_date && dl.VS_ITEM == "etco" && !string.IsNullOrEmpty(dl.VS_RECORD));
                    foreach (DB_NIS_VITALSIGN etco in etco_list)
                    {
                        etco.VS_RECORD = etco.VS_RECORD + "mmHg";
                        add_TprData(ref tpr_data_list, etco.CREATE_DATE, etco, "ETCO2");
                    }
                    //篩選保溫箱溫度
                    List<DB_NIS_VITALSIGN> BTemp_list = dnv_list.FindAll(dl => dl.MODIFY_DATE >= start_date && dl.MODIFY_DATE <= end_date && dl.VS_ITEM == "gi");
                    foreach (DB_NIS_VITALSIGN BTemp in BTemp_list)
                    {
                        BTemp.VS_RECORD = BTemp.VS_RECORD + "℃";
                        add_TprData(ref tpr_data_list, BTemp.CREATE_DATE, BTemp, "BoxTemperature");
                    }

                    //篩選臍帶脫落
                    List<DB_NIS_VITALSIGN> Umbilical_list = dnv_list.FindAll(dl => dl.MODIFY_DATE >= start_date && dl.MODIFY_DATE <= end_date && dl.VS_ITEM == "gi_c");
                    foreach (DB_NIS_VITALSIGN Umbilical in Umbilical_list)
                    {
                        add_TprData(ref tpr_data_list, Umbilical.CREATE_DATE, Umbilical, "Umbilical");
                    }

                    //篩選尿液
                    List<DB_NIS_VITALSIGN> Urine_list = dnv_list.FindAll(dl => dl.MODIFY_DATE >= start_date && dl.MODIFY_DATE <= end_date && dl.VS_ITEM == "gi_u");
                    foreach (DB_NIS_VITALSIGN Urine in Urine_list)
                    {
                        add_TprData(ref tpr_data_list, Urine.CREATE_DATE, Urine, "Urine");
                    }

                    //篩選膚色
                    List<DB_NIS_VITALSIGN> SkinColor_list = dnv_list.FindAll(dl => dl.MODIFY_DATE >= start_date && dl.MODIFY_DATE <= end_date && dl.VS_ITEM == "si_n");
                    foreach (DB_NIS_VITALSIGN SkinColor in SkinColor_list)
                    {
                        add_TprData(ref tpr_data_list, SkinColor.CREATE_DATE, SkinColor, "SkinColor");
                    }

                    //篩選呼吸型態
                    List<DB_NIS_VITALSIGN> BPattern_list = dnv_list.FindAll(dl => dl.MODIFY_DATE >= start_date && dl.MODIFY_DATE <= end_date && dl.VS_ITEM == "si_b");
                    foreach (DB_NIS_VITALSIGN BPattern in BPattern_list)
                    {
                        add_TprData(ref tpr_data_list, BPattern.CREATE_DATE, BPattern, "BreathePattern");
                    }

                    //篩選呼吸品質
                    List<DB_NIS_VITALSIGN> BQuality_list = dnv_list.FindAll(dl => dl.MODIFY_DATE >= start_date && dl.MODIFY_DATE <= end_date && dl.VS_ITEM == "si_r");
                    foreach (DB_NIS_VITALSIGN BQuality in BQuality_list)
                    {
                        add_TprData(ref tpr_data_list, BQuality.CREATE_DATE, BQuality, "BreatheQuality");
                    }

                    //篩選痰液
                    List<DB_NIS_VITALSIGN> Sputum_list = dnv_list.FindAll(dl => dl.MODIFY_DATE >= start_date && dl.MODIFY_DATE <= end_date && dl.VS_ITEM == "si_s");
                    foreach (DB_NIS_VITALSIGN Sputum in Sputum_list)
                    {
                        add_TprData(ref tpr_data_list, Sputum.CREATE_DATE, Sputum, "Sputum");
                    }

                    //篩選翻身
                    List<DB_NIS_VITALSIGN> Tumble_list = dnv_list.FindAll(dl => dl.MODIFY_DATE >= start_date && dl.MODIFY_DATE <= end_date && dl.VS_ITEM == "si_o");
                    foreach (DB_NIS_VITALSIGN Tumble in Tumble_list)
                    {
                        add_TprData(ref tpr_data_list, Tumble.CREATE_DATE, Tumble, "Tumble");
                    }

                    //篩選血流動力
                    string PICCO = "picco_co,picco_ci,picco_sv,picco_svi,picco_svr,picco_svri,picco_svv,picco_scvo2";
                    string[] group_list = PICCO.Split(','); string tmp_item = "";
                    List<DB_NIS_VITALSIGN> PICCO_list = null;
                    for (int i = 0; i < group_list.Length; i++)
                    {
                        PICCO_list = dnv_list.FindAll(dl => dl.MODIFY_DATE >= start_date && dl.MODIFY_DATE <= end_date && dl.VS_ITEM == group_list[i]);
                        tmp_item = group_list[i];
                        foreach (DB_NIS_VITALSIGN pico in PICCO_list)
                        {
                            pico.VS_RECORD = set_picco_name(tmp_item) + ":" + pico.VS_RECORD + set_picco_unit(tmp_item);
                            add_TprData(ref tpr_data_list, pico.CREATE_DATE, pico, "PICCO");
                        }
                    }

                    //篩選圍長
                    string GW = "gtwl,gthr,gtbu,gtbl,gthl,gtlf,gtrf,gtlua,gtrua,gtlt,gtrt,gtll,gtrl,gtla,gtra";
                    group_list = GW.Split(','); tmp_item = "";
                    List<DB_NIS_VITALSIGN> Girth_list = null;
                    for (int i = 0; i < group_list.Length; i++)
                    {
                        Girth_list = dnv_list.FindAll(dl => dl.MODIFY_DATE >= start_date && dl.MODIFY_DATE <= end_date && dl.VS_ITEM == group_list[i]);
                        tmp_item = group_list[i];
                        foreach (DB_NIS_VITALSIGN Girth in Girth_list)
                        {
                            Girth.VS_RECORD = set_name(tmp_item) + Girth.VS_RECORD;
                            add_TprData(ref tpr_data_list, Girth.CREATE_DATE, Girth, "Girth");
                        }
                    }
                    

                    //取得IO項目清單
                    DataTable dt_io_item = iom.sel_sys_params_kind("iotype");
                    List<DB_NIS_SYS_PARAMS> io_item_list = (List<DB_NIS_SYS_PARAMS>)dt_io_item.ToList<DB_NIS_SYS_PARAMS>();
                    List<DB_NIS_SYS_PARAMS> i_item_list = io_item_list.FindAll(iil => iil.P_GROUP == "intaketype");
                    if (i_item_list != null)
                    {
                        view_tpr.i_item_list = i_item_list;
                    }
                    List<DB_NIS_SYS_PARAMS> o_item_list = io_item_list.FindAll(iil => iil.P_GROUP == "outputtype");
                    if (o_item_list != null)
                    {
                        view_tpr.o_item_list = o_item_list;
                    }

                    //取得IO資料
                    DataTable dt_io_data = iom.sel_io_data("", feeno, "", Convert.ToDateTime(start).ToString("yyyy/MM/dd 07:01"), Convert.ToDateTime(end).AddDays(1).ToString("yyyy/MM/dd 07:00"), "'1','3'","","", str_HISVIEW);
                    List<DB_NIS_IO_DATA_FUNC_IO_NAME> io_data_list = (List<DB_NIS_IO_DATA_FUNC_IO_NAME>)dt_io_data.ToList<DB_NIS_IO_DATA_FUNC_IO_NAME>();
                    List<DB_NIS_IO_DATA_FUNC_IO_NAME> i_data_list = io_data_list.FindAll(x => x.P_GROUP == "intaketype");
                    List<DB_NIS_IO_DATA_FUNC_IO_NAME> o_data_list = io_data_list.FindAll(x => x.P_GROUP == "outputtype");
                    foreach (DB_NIS_IO_DATA_FUNC_IO_NAME i_data in i_data_list)
                    {
                        string title = sel_io_title(i_data);
                        string vs_record = i_data.AMOUNT >= 0 ? i_data.AMOUNT_ALL.ToString() : "Loss";
                        DateTime create_date = new DateTime();
                        if (DateTime.TryParse(i_data.CREATTIME, out create_date))
                        {
                            DB_NIS_VITALSIGN dnv = new DB_NIS_VITALSIGN
                            {
                                CREATE_DATE = create_date,
                                MODIFY_DATE = create_date,
                                VS_ITEM = title,
                                VS_RECORD = vs_record.ToString()
                            };
                            add_TprData(ref tpr_data_list, create_date, dnv, "Input", i_data.TYPEID);
                        }
                    }
                    foreach (DB_NIS_IO_DATA_FUNC_IO_NAME o_data in o_data_list)
                    {
                        string title = sel_io_title(o_data);
                        string vs_record = o_data.AMOUNT >= 0 ? o_data.AMOUNT_ALL.ToString() : "Loss";
                        DateTime create_date = new DateTime();
                        if (DateTime.TryParse(o_data.CREATTIME, out create_date))
                        {
                            DB_NIS_VITALSIGN dnv = new DB_NIS_VITALSIGN
                            {
                                CREATE_DATE = create_date,
                                MODIFY_DATE = create_date,
                                VS_ITEM = title,
                                VS_RECORD = vs_record.ToString()
                            };
                            add_TprData(ref tpr_data_list, create_date, dnv, "Output", o_data.TYPEID);
                        }
                    }
                    //早產週數
                    //生產週數天數+（今天日期-生產日期），若大於40週則顯示足月，否則顯示週數天數
                    sql = $@"select * from (
select FEENO, '新生兒' AS Type, BIRTH, WEIGHT, GEST_M, GEST_D, BIRTH_TYPE, PROCESS, FROM_WHERE, RECORDTIME FROM OBS_NBENTR
WHERE FEENO = '{feeno}'  AND DELETED IS NULL
UNION select FEENO, '嬰幼兒' AS Type, BIRTH,WEIGHT, GEST_M, GEST_D,BIRTH_TYPE, PROCESS, FROM_WHERE, RECORDTIME FROM OBS_BABYENTR 
WHERE FEENO = '{feeno}' AND DELETED IS NULL) A 
ORDER BY RECORDTIME DESC";
                    DataTable Dt2 = link.DBExecSQL(sql);
                    if (Dt2.Rows.Count > 0)
                    {
                        int gestM = 0;
                        int gestD = 0;
                        if (Dt2.Rows[0]["GEST_M"].ToString() != "")
                        {
                            gestM = Convert.ToInt32(Dt2.Rows[0]["GEST_M"].ToString());
                        }
                        if (Dt2.Rows[0]["GEST_D"].ToString() != "")
                        {
                            gestD = Convert.ToInt32(Dt2.Rows[0]["GEST_D"].ToString());
                        }

                        view_tpr.birthday = ptinfo.Birthday.ToString("yyyy-MM-dd");
                        view_tpr.gest_d = (gestM * 7 + gestD).ToString();

                    }
                    //else
                    //{
                    //    int gestM = 39;
                    //    int gestD = 5;

                    //    view_tpr.birthday = "2019-12-14";
                    //    view_tpr.gest_d = (gestM * 7 + gestD).ToString();

                    //}

                    //Tpr資料
                    view_tpr.tpr_data = tpr_data_list;

                    //DataTable dt_i_item = iom.sel_sys_params_kind("iotypeTPR", "intaketypeTPR");
                    //DataTable dt_o_item = iom.sel_sys_params_kind("iotypeTPR", "outputtypeTPR");
                    //DataTable dt_io_data = iom.sel_io_data(feeno, "", Convert.ToDateTime(start).ToString("yyyy/MM/dd 07:01"), Convert.ToDateTime(end).AddDays(1).ToString("yyyy/MM/dd 07:00"), "'1','3'");
                    //string[] column = { "DATE", "P_VALUE", "AMOUNT", "CLORIE", "REASON", "TYPE" };
                    //iom.set_dt_column(dt_io, column);
                    //iom.set_new_list(dt_io_data, dt_i_item, dt_o_item, dt_io, Convert.ToDateTime(start), Convert.ToDateTime(end));

                    // 手術日期
                    //ViewBag.dt_op_day = get_Surgery_Day(Convert.ToDateTime(start), Convert.ToDateTime(end), feeno);
                    // 血糖(One Touch)
                    //ViewBag.dt_bsugar = get_BloodSugarList(feeno, start, end);
                    // VitalSign MEWS
                    //ViewBag.dt_mews = get_Vitalsign_MEWS(feeno, start, end);

                    ViewBag.dt = dt;
                    ViewBag.dt_io = dt_io;
                    //ViewBag.dt_i_item = dt_i_item;
                    //ViewBag.dt_o_item = dt_o_item;
                    //ViewBag.anti = get_antibiotics(feeno);
                    //ViewData["procedure_list"] = procedure_list;
                    ViewBag.DrawDay = (string.IsNullOrWhiteSpace(draw_day)) ? 5 : int.Parse(draw_day);
                    ViewBag.start = start;
                    ViewBag.end = end;
                    ViewBag.feeno = feeno;
                    ViewBag.str_HISVIEW = str_HISVIEW;
                    ViewBag.io_list = new Func<string, string, string, string>(sel_io_list);

                    json_result.status = RESPONSE_STATUS.SUCCESS;
                }
                else
                {
                    json_result.status = RESPONSE_STATUS.ERROR;
                    json_result.message = "System error：Unable to obtain patient information!";
                }
            }
            catch (Exception ex)
            {
                json_result.status = RESPONSE_STATUS.EXCEPTION;
                json_result.message = ex.Message;
            }
            finally
            {
                this.link.DBClose();
            }

            json_result.attachment = view_tpr;
            return new JsonResult { Data = json_result, MaxJsonLength = int.MaxValue, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        public bool add_TprData_old(ref List<TprData> view_tpr_list, DateTime data_date, string item_name, string item_value, string class_tag = "", string group_name = "")
        {
            try
            {
                TprData vtl_date = view_tpr_list.Find(x => x.data_date == data_date.ToString("yyyy/MM/dd"));
                ClassData class_data = new ClassData
                {
                    class_tag = class_tag,
                    group_name = group_name,
                    item_time = data_date.ToString("HH:mm"),
                    item_name = item_name,
                    item_value = item_value
                };
                if (vtl_date != null)
                {
                    //新增資料
                    vtl_date.data_list.Add(class_data);
                }
                else
                {
                    //新增日期、資料
                    TprData vt = new TprData();
                    vt.data_date = data_date.ToString("yyyy/MM/dd");
                    vt.data_list = new List<ClassData>(){
                        class_data
                    };
                    view_tpr_list.Add(vt);
                }
                return true;
            }
            catch (Exception ex)
            {//寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return false;
            }
        }

        public bool add_TprData(ref List<TprData> view_tpr_list, DateTime data_date, DB_NIS_VITALSIGN dnv, string class_tag = "", string group_name = "", string ValueStatus = "")
        {
            try
            {
                TprData vtl_date = view_tpr_list.Find(x => x.data_date == data_date.ToString("yyyy/MM/dd"));
                ClassData class_data = new ClassData
                {
                    class_tag = class_tag,
                    group_name = group_name,
                    item_time = data_date.ToString("HH:mm"),
                    item_part = dnv.VS_PART,
                    item_name = dnv.VS_ITEM,
                    item_value = dnv.VS_RECORD,
                    value_status = ValueStatus
                };
                if (vtl_date != null)
                {
                    //新增資料
                    vtl_date.data_list.Add(class_data);
                }
                else
                {
                    //新增日期、資料
                    TprData vt = new TprData();
                    vt.data_date = data_date.ToString("yyyy/MM/dd");
                    vt.data_list = new List<ClassData>(){
                        class_data
                    };
                    view_tpr_list.Add(vt);
                }
                return true;
            }
            catch (Exception ex)
            {//寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return false;
            }
        }


        /// <summary>
        /// 取得 DB_NIS_VITALSIGN
        /// </summary>
        /// <param name="feeno">批價序號</param>
        /// <param name="start_time">起</param>
        /// <param name="end_time">迄</param>
        /// <returns>List(DB_NIS_VITALSIGN)</returns>
        private List<DB_NIS_VITALSIGN> get_DbNisVitalsign(string feeno, DateTime start_time, DateTime end_time,string str_HISVIEW)
        {
            List<DB_NIS_VITALSIGN> nis_vitalsign_list = new List<DB_NIS_VITALSIGN>();
            string work_VITALSIGN = "";
            try
            {
                if (str_HISVIEW == "HISVIEW")
                    work_VITALSIGN = "H_DATA_VITALSIGN";
                else
                    work_VITALSIGN = "DATA_VITALSIGN";

                //取得VitalSign資料
                VitalSign vs = new VitalSign();
                string sql = "SELECT * FROM "+ work_VITALSIGN + " WHERE 0 = 0 ";
                if (feeno != "")
                    sql += "AND FEE_NO = '" + feeno + "' ";
                if (start_time.ToString("yyyy/MM/dd HH:mm:ss") != "" && end_time.ToString("yyyy/MM/dd HH:mm:ss") != "")
                    sql += "AND  TO_CHAR(CREATE_DATE,'yyyy/MM/dd HH:mi:ss') BETWEEN '" + start_time.ToString("yyyy/MM/dd HH:mm:ss") + "' AND '" + end_time.ToString("yyyy/MM/dd HH:mm:ss") + "' ";
                sql += " AND DEL IS NULL ORDER BY CREATE_DATE";

                DataTable dt = new DataTable();
                link.DBExecSQL(sql, ref dt);

                if (dt != null && dt.Rows.Count > 0)
                {
                    nis_vitalsign_list = (List<DB_NIS_VITALSIGN>)dt.ToList<DB_NIS_VITALSIGN>();
                }

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

            return nis_vitalsign_list;
        }

        public ActionResult Get_Result(string startdate, string enddate)
        {
            byte[] VitalSignbyDateByteCode = webService.GetVitalSignbyDate(ptinfo.FeeNo, startdate, enddate, userinfo.EmployeesNo);

            if (VitalSignbyDateByteCode != null)
            {
                string VitalSignbyDateListJosnArr = NIS.UtilTool.CompressTool.DecompressString(VitalSignbyDateByteCode);
                List<VitalSignbyDate> VitalSignbyDateList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<VitalSignbyDate>>(VitalSignbyDateListJosnArr);
                ViewData["result"] = VitalSignbyDateList;
            }
            return View();
        }
        //轉PDF頁面
        public ActionResult Html_To_Pdf(string url, string start, string end)
        {
            string strPath = @"C:\\wkhtmltopdf\\wkhtmltopdf.exe";
            string filename = DateTime.Now.ToString("yyyyMMddHHmmssfff") + url.Substring(url.IndexOf("feeno=") + 6, url.Length - url.IndexOf("feeno=") - 6) + ".pdf";
            string tempPath = "C:\\inetpub\\NIS\\Images\\" + filename;
            string pdfpath = "../Images/" + filename;
            Process p = new Process();
            p.StartInfo.FileName = strPath;
            p.StartInfo.Arguments = url + " --window-status "+"allImageLoaded"+" " + tempPath;
            p.StartInfo.UseShellExecute = true;
            p.Start();
            p.WaitForExit();
            Response.Write("<script>window.open('Download_Pdf?filename=" + filename + "');window.location.href='Tpr_Index?status=after_print&start=" + start + "&end=" + end + "';</script>");

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

        private string get_sql_by_interval(string feeno, DateTime starttime, DateTime endtime)
        {
            string sql = "SELECT DATA.VS_ITEM ";
            for (DateTime start = starttime; start <= endtime; start = start.AddDays(1))
            {
                string[] time_list = { start.ToString("yyyy/MM/dd 09:00:00"), start.ToString("yyyy/MM/dd 13:00:00"), start.ToString("yyyy/MM/dd 18:00:00"), start.ToString("yyyy/MM/dd 21:00:00") };
                for (int i = 0; i < 4; i++)
                {
                    sql += ", (SELECT VS_RECORD FROM DATA_VITALSIGN WHERE VS_ITEM = DATA.VS_ITEM AND VS_RECORD IS NOT NULL AND DEL IS NULL ";
                    sql += "AND FEE_NO = '" + feeno + "' AND CREATE_DATE BETWEEN ";
                    sql += "to_date('" + Convert.ToDateTime(time_list[i]).AddHours(-1).ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/mm/dd hh24:mi:ss') ";
                    sql += "AND to_date('" + Convert.ToDateTime(time_list[i]).AddHours(1).ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/mm/dd hh24:mi:ss') ";
                    sql += "AND rownum <=1 )AS \"" + time_list[i] + "\" ";
                }
            }
            sql += "FROM (SELECT DISTINCT(VS_ITEM) FROM DATA_VITALSIGN)DATA ";
            return sql;
        }

        private string get_sql(string feeno, DateTime starttime, DateTime endtime)
        {
            string sql = "SELECT DATA.VS_ITEM ";
            for (DateTime start = starttime; start <= endtime; start = start.AddDays(1))
            {
                sql += ", (SELECT VS_RECORD || ',' || VS_PART FROM DATA_VITALSIGN WHERE VS_ITEM = DATA.VS_ITEM AND VS_RECORD IS NOT NULL AND DEL IS NULL ";
                sql += "AND FEE_NO = '" + feeno + "' AND CREATE_DATE BETWEEN ";
                sql += "to_date('" + start.AddDays(-1).ToString("yyyy/MM/dd 23:59:59") + "','yyyy/mm/dd hh24:mi:ss') ";
                sql += "AND to_date('" + start.AddDays(1).ToString("yyyy/MM/dd 00:00:00") + "','yyyy/mm/dd hh24:mi:ss') ";
                sql += "AND rownum <=1 )AS \"" + start.ToString("yyyy/MM/dd") + "\" ";
            }
            sql += "FROM (SELECT DISTINCT(VS_ITEM) FROM DATA_VITALSIGN)DATA ";
            return sql;
        }



        private string get_special_event_sql(string feeno, string type)
        {
            string sql = "SELECT DISTINCT(to_char(CREATTIME,'yyyy/mm/dd'))CREATTIME FROM NIS_SPECIAL_EVENT_DATA WHERE 0 = 0 ";
            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if (type != "")
                sql += "AND TYPE_ID = '" + type + "' ";
            sql += "ORDER BY CREATTIME ";

            return sql;
        }

        private DataTable set_special_event(string feeno, string type, DateTime starttime, DateTime endtime, string Replace_Word, int count_num)
        {
            DataTable dt = new DataTable();

            try
            {
                string sql = "SELECT DISTINCT(to_char(CREATTIME,'yyyy/mm/dd'))CREATTIME FROM NIS_SPECIAL_EVENT_DATA WHERE 0 = 0 ";
                if (feeno != "")
                    sql += "AND FEENO = '" + feeno + "' ";
                if (type != "")
                    sql += "AND TYPE_ID = '" + type + "' ";
                sql += "ORDER BY CREATTIME ";
                DataTable dt_temp = iom.DBExecSQL(sql);
                
                dt.Columns.Add("TIME");
                dt.Columns.Add("CONTENT");

                for (DateTime start_day = starttime; start_day <= endtime; start_day = start_day.AddDays(1))
                {
                    string content = "";
                    foreach (DataRow r in dt_temp.Rows)
                    {
                        DateTime dt_temp_date = Convert.ToDateTime(r["CREATTIME"]);
                        if (start_day.Date >= dt_temp_date.Date && (start_day.Date - dt_temp_date.Date).Days < count_num)
                        {
                            if ((start_day.Date - dt_temp_date.Date).Days == 0)
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

            return dt;
        }

        private string sel_io_list(string date, string type, string feeno)
        {
            if (date != "" && type != "")
            {
                TubeManager tubem = new TubeManager();
                DataTable dt = iom.sel_io_data("", feeno, "", Convert.ToDateTime(date).ToString("yyyy/MM/dd 07:01:00"), Convert.ToDateTime(date).AddDays(1).ToString("yyyy/MM/dd 07:00:59"), "1", type);
                string content = "";
                foreach (DataRow r in dt.Rows)
                {
                    if (type != "9")
                    {
                        content += r["P_NAME"].ToString() + r["NAME"].ToString() + " ";
                        if (r["AMOUNT"].ToString() == "" && r["REASON"].ToString() != "")
                            content += "Loss";
                        else
                        {
                            if (r["AMOUNT_UNIT"].ToString() == "1")
                                content += r["AMOUNT"].ToString() + "mL";
                            else if (r["AMOUNT_UNIT"].ToString() == "2")
                                content += "  " + r["AMOUNT"].ToString() + "g";
                            else if (r["AMOUNT_UNIT"].ToString() == "3")
                                content += "  " + r["AMOUNT"].ToString() + "mg";
                        }
                    }
                    else
                    {
                        DataTable dt_tube_name = tubem.sel_tube("", "", r["ITEMID"].ToString(), "", "N");
                        if (dt_tube_name.Rows.Count > 0)
                        {
                            content += dt_tube_name.Rows[0]["TYPE_NAME"].ToString() + dt_tube_name.Rows[0]["POSITION"].ToString() + dt_tube_name.Rows[0]["LOCATION_NAME"].ToString();
                            if (dt_tube_name.Rows[0]["NUMBERID"].ToString() != "99")
                                content += "#" + dt_tube_name.Rows[0]["NUBER_NAME"].ToString();
                            else
                                content += "#" + dt_tube_name.Rows[0]["NUMBEROTHER"].ToString();
                        }
                        if (r["AMOUNT"].ToString() == "" && r["REASON"].ToString() != "")
                            content += "Loss";
                        else
                        {
                            if (r["AMOUNT_UNIT"].ToString() == "1")
                                content += "  " + r["AMOUNT"].ToString() + "mL";
                            else if (r["AMOUNT_UNIT"].ToString() == "2")
                                content += "  " + r["AMOUNT"].ToString() + "g";
                            else if (r["AMOUNT_UNIT"].ToString() == "3")
                                content += "  " + r["AMOUNT"].ToString() + "mg";
                        }
                    }
                    if (r["COLORID"].ToString() != "-1" && r["COLORID"].ToString() != "")
                        content += " 顏色：" + r["COLORNAME"].ToString().Replace("其他", "") + r["COLOROTHER"];
                    if (r["TASTEID"].ToString() != "-1" && r["TASTEID"].ToString() != "")
                        content += " 氣味：" + r["TASTENAME"].ToString().Replace("其他", "") + r["TASTEOTHER"];
                    if (r["NATUREID"].ToString() != "-1" && r["NATUREID"].ToString() != "")
                        content += " 性質：" + r["NATURENAME"].ToString().Replace("其他", "") + r["NATUREOTHER"];

                    content += "\n";
                }

                return content;
            }
            else
                return "";
        }

        private string sel_io_title(DB_NIS_IO_DATA_FUNC_IO_NAME i_data)
        {
            if (i_data != null)
            {
                string content = "";
                TubeManager tubem = new TubeManager();
                if (i_data.TYPEID != "9")
                {
                    content += i_data.P_NAME.ToString() + i_data.NAME.ToString() + " ";
                    if (i_data.AMOUNT.ToString() == "0" && i_data.REASON.ToString() != "")
                        content += "Loss";
                    else
                    {
                        if (i_data.AMOUNT_UNIT.ToString() == "1")
                            content += i_data.AMOUNT.ToString() + "mL";
                        else if (i_data.AMOUNT_UNIT.ToString() == "2")
                            content += "  " + i_data.AMOUNT.ToString() + "g";
                        else if (i_data.AMOUNT_UNIT.ToString() == "3")
                            content += "  " + i_data.AMOUNT.ToString() + "mg";
                        else if (i_data.AMOUNT_UNIT.ToString() == "4")
                            content += "  " + i_data.AMOUNT.ToString() + "分鐘";
                        else if (i_data.AMOUNT_UNIT.ToString() == "5")
                            content += "  " + i_data.AMOUNT.ToString() + "次";
                    }
                }
                else
                {
                    DataTable dt_tube_name = tubem.sel_tube("", "", i_data.ITEMID.ToString(), "", "N");
                    if (dt_tube_name.Rows.Count > 0)
                    {
                        content += dt_tube_name.Rows[0]["TYPE_NAME"].ToString() + dt_tube_name.Rows[0]["POSITION"].ToString() + dt_tube_name.Rows[0]["LOCATION_NAME"].ToString();
                        if (dt_tube_name.Rows[0]["NUMBERID"].ToString() != "99")
                            content += "#" + dt_tube_name.Rows[0]["NUBER_NAME"].ToString();
                        else
                            content += "#" + dt_tube_name.Rows[0]["NUMBEROTHER"].ToString();
                    }
                    if (i_data.AMOUNT.ToString() == "0" && i_data.REASON.ToString() != "")
                        content += "Loss";
                    else
                    {
                        if (i_data.AMOUNT_UNIT.ToString() == "1")
                            content += "  " + i_data.AMOUNT.ToString() + "mL";
                        else if (i_data.AMOUNT_UNIT.ToString() == "2")
                            content += "  " + i_data.AMOUNT.ToString() + "g";
                        else if (i_data.AMOUNT_UNIT.ToString() == "3")
                            content += "  " + i_data.AMOUNT.ToString() + "mg";
                    }
                }
                if (i_data.COLORID.ToString() != "-1" && i_data.COLORID.ToString() != "")
                    content += " 顏色：" + i_data.COLORNAME.ToString().Replace("其他", "") + i_data.COLOROTHER;
                if (i_data.TASTEID.ToString() != "-1" && i_data.TASTEID.ToString() != "")
                    content += " 氣味：" + i_data.TASTENAME.ToString().Replace("其他", "") + i_data.TASTEOTHER;
                if (i_data.NATUREID.ToString() != "-1" && i_data.NATUREID.ToString() != "")
                    content += " 性質：" + i_data.NATURENAME.ToString().Replace("其他", "") + i_data.NATUREOTHER;

                content += "\n";

                return content;
            }
            else
                return "";
        }

        #endregion

        #region 生命徵象異常值設定
        public ActionResult Set_VitalSign()
        {
            try
            {
            string sql = "SELECT A.* FROM NIS_SYS_VITALSIGN_OPTION  A";
            DataTable DT = this.link.DBExecSQL(sql);
            ViewBag.DT = DT;
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
        [HttpPost]
        public ActionResult Set_VitalSign_SAVE()
        {
            string[] ArrModelID = (Request["HidModelID"] != null) ? Request["HidModelID"].Split(',') : null;
            string[] ArrTitle = (Request["TxtTitle"] != null) ? Request["TxtTitle"].Split(',') : null;
            string[] ArrValueLimit = (Request["TxtValueLimit"] != null) ? Request["TxtValueLimit"].Split(',') : null;
            string[] ArrDecide = (Request["DDLDecide"] != null) ? Request["DDLDecide"].Split(',') : null;
            string[] ArrItem = (Request["TxtItem"] != null) ? Request["TxtItem"].Split(',') : null;
            string[] ArrOtherItem = (Request["TxtOtherItem"] != null) ? Request["TxtOtherItem"].Split(',') : null;
            string[] ArrOtherTitle = (Request["TxtOtherTitle"] != null) ? Request["TxtOtherTitle"].Split(',') : null;
            string UserNO = userinfo.EmployeesNo.Trim();
            string UserName = userinfo.EmployeesName.Trim();
            int ERow = 0;
            List<DBItem> insList = new List<DBItem>();

            if (ArrModelID != null && ArrModelID.Length > 0)
            {
                for (int i = 0; i <= ArrModelID.Length - 1; i++)
                {
                    insList.Clear();

                    //insList.Add(new DBItem("title", ArrTitle[i], DBItem.DBDataType.String));
                    insList.Add(new DBItem("value_limit", ArrValueLimit[i], DBItem.DBDataType.String));
                    insList.Add(new DBItem("decide", ArrDecide[i], DBItem.DBDataType.String));
                    insList.Add(new DBItem("item", ArrItem[i], DBItem.DBDataType.String));
                    //insList.Add(new DBItem("other_item", ArrOtherItem[i], DBItem.DBDataType.String));
                    //insList.Add(new DBItem("other_title", ArrOtherTitle[i], DBItem.DBDataType.String));
                    //insList.Add(new DBItem("mod_oper_id", UserNO, DBItem.DBDataType.String));
                    //insList.Add(new DBItem("mod_oper_name", UserName, DBItem.DBDataType.String));
                    //insList.Add(new DBItem("mod_dtm", DateTime.Now.ToUniversalTime().AddHours(8).ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));

                    ERow += this.link.DBExecUpdate("NIS_SYS_VITALSIGN_OPTION", insList, " model_id='" + ArrModelID[i] + "' ");
                }
            }

            //if (ERow > 0)
            //    return "Y";
            //else
            //    return "N";
            if (ERow > 0)
                Response.Write("<script>alert('儲存成功');window.location.href='Set_VitalSign';</script>");
            else
                Response.Write("<script>alert('儲存失敗');window.location.href='Set_VitalSign';</script>");

            return new EmptyResult();
        }

        

        #endregion //生命徵象異常值設定

        #region 共用處置
        /// <summary>
        /// 共用處置
        /// </summary>
        /// <param name="pCtrl">需帶入的TextArea之ID名稱</param>
        /// <param name="pType">共用處置類別</param>
        /// <remarks>2016/05/17 Vanda Add</remarks>
        [HttpGet]
        public ActionResult SharedVitalSign(string pCtrl, string pType = "")
        {
            string[] TypeArray = null;
            if (pType != null && pType != "") TypeArray = pType.Split(',');
            DataTable SVODt = this.link.DBExecSQL(this.cd.getSharedVitalSign(TypeArray));
            ViewBag.SVODt = SVODt;
            ViewBag.Ctrl = pCtrl;
            return View();
        }

        public ActionResult SharedMaintain()
        {
            string sql = "SELECT * FROM SHARED_VITALSIGN_OPTION";
            DataTable DT = this.link.DBExecSQL(sql);
            ViewBag.DT = DT;
            return View();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult SaveSharedMaintain(FormCollection pForm)
        {
            string[] ArrModelID = (Request["HidModelID"] != null) ? Request["HidModelID"].Split(',') : null;
            string[] ArrItem = (Request["TxtItem"] != null) ? Request["TxtItem"].Split(',') : null;
            string UserNO = userinfo.EmployeesNo.Trim();
            string UserName = userinfo.EmployeesName.Trim();
            int ERow = 0;
            List<DBItem> insList = new List<DBItem>();
            if (ArrModelID != null && ArrModelID.Length > 0)
            {
                for (int i = 0; i <= ArrModelID.Length - 1; i++)
                {
                    insList.Clear();
                    insList.Add(new DBItem("item", ArrItem[i], DBItem.DBDataType.String));
                    ERow += this.link.DBExecUpdate("SHARED_VITALSIGN_OPTION", insList, " svo_id='" + ArrModelID[i] + "' ");
                }
            }
            if (ERow > 0)
                Response.Write("<script>alert('儲存成功');window.location.href='SharedMaintain';</script>");
            else
                Response.Write("<script>alert('儲存失敗');window.location.href='SharedMaintain';</script>");

            return new EmptyResult();
        }
        #endregion
        [HttpPost]
        //引流管統計 for IO_Tube (引流管統計 Ajax )
        public ActionResult Inquire_tube_new(string date, string qry_date, string feeno, string source)
        {
            RESPONSE_MSG json_result = new RESPONSE_MSG();
            PatientInfo ptInfo = new PatientInfo();

            if (Session["PatInfo"] != null)
            {
                //宣告病患_取得住院日期
                ptInfo = (PatientInfo)Session["PatInfo"];
            }
            else if (source == "hisview")
            {
                byte[] ptinfoByteCode = webService.GetPatientInfo(feeno);
                if (ptinfoByteCode != null)
                {
                    string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                    ptInfo = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                }
            }
            else
            {
                Response.Write("<script>alert('登入逾時');</script>");
                ViewBag.tubeData = "";
                return new EmptyResult();
            }
            ViewBag.InDate = Convert.ToDateTime(ptInfo.InDate).ToString("yyyy/MM/dd");

            if (qry_date == "")
            {////此區為設定初始值
                date = DateTime.Now.ToString("yyyy/MM/dd");
            }
            else
            {
                date = qry_date;
            }

            string SqlStr = "";
            SqlStr = " SELECT IO_ID,IO_ITEMID  as ITEMID, MAIN.TYPEID,IO_CREATETIME as CREATETIME,IO_AMOUNT as AMOUNT,AMOUNT_UNIT,C_REASON as REASON,TUBEROW,TUBEID,FEENO,TYPE_NAME,POSITION,LOCATION_NAME,NUBER_NAME,NUMBEROTHER ";
            SqlStr += " ,CASE NUMBERID WHEN '99' THEN TYPE_NAME||POSITION||LOCATION_NAME||'#'||NUMBEROTHER ELSE TYPE_NAME||POSITION||LOCATION_NAME||'#'||NUBER_NAME END AS TUBE_CONTENT ";
            SqlStr += " FROM ( ";
            SqlStr += " Select IO_ID,ITEMID as IO_ITEMID, To_Char(CREATTIME,'yyyy/MM/dd hh24:mi:ss') IO_CREATETIME ,\"TYPEID\",SUM(CASE WHEN AMOUNT_UNIT<>'3' THEN AMOUNT ELSE AMOUNT*0.001 END) as IO_AMOUNT,AMOUNT_UNIT,count(REASON) as C_REASON";
            SqlStr += " From IO_DATA where TYPEID ='9' AND DELETED IS NULL AND FEENO = '" + ptInfo.FeeNo + "' ";
            SqlStr += " AND CREATTIME BETWEEN TO_DATE('" + date + " 07:01:00', 'yyyy/mm/dd hh24:mi:ss') ";
            SqlStr += " AND TO_DATE('" + Convert.ToDateTime(date).AddDays(1).ToString("yyyy/MM/dd") + " 07:00:59', 'yyyy/mm/dd hh24:mi:ss')";
            SqlStr += " Group by IO_ID,ITEMID,AMOUNT_UNIT, To_Char(CREATTIME,'yyyy/MM/dd hh24:mi:ss'),TYPEID,REASON";
            SqlStr += " Order by To_Char(CREATTIME,'yyyy/MM/dd hh24:mi:ss'),TO_NUMBER(TYPEID) ) MAIN ";
            SqlStr += "  LEFT OUTER JOIN ( ";
            SqlStr += " SELECT  TUBE.*,";
            SqlStr += " TUBE_FEATURE.NUMBERID,TUBE_FEATURE.NUMBEROTHER,TUBE_FEATURE.MATERIALID,TUBE_FEATURE.MATERIALOTHER, ";
            SqlStr += " (SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'tube' AND P_GROUP = 'tubePosition' AND P_VALUE = TUBE.LOCATION ) LOCATION_NAME, ";
            SqlStr += " (SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'tube' AND P_GROUP = 'tubeNumber' AND P_VALUE = TUBE_FEATURE.NUMBERID ) NUBER_NAME, ";
            SqlStr += "  (SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'tube' AND P_GROUP = 'tubeMaterial' AND P_VALUE = TUBE_FEATURE.MATERIALID ) MATERIAL_NAME, ";
            SqlStr += " (SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'tube' AND P_GROUP = 'tubeLengthUnit' AND P_VALUE = TUBE.LENGTHUNIT ) LENGTHUNIT_NAME, ";
            SqlStr += " (SELECT KINDNAME FROM TUBE_KIND WHERE KINDID = TUBE.TYPEID ) TYPE_NAME, ";
            SqlStr += " (SELECT TUBE_GROUP FROM TUBE_KIND WHERE KINDID = TUBE.TYPEID ) TYPE_GROUP, ";
            SqlStr += " (SELECT ASSESS_TYPE FROM TUBE_KIND WHERE KINDID = TUBE.TYPEID ) ASSESS_TYPE ";
            SqlStr += " FROM TUBE INNER JOIN TUBE_FEATURE ON TUBE.TUBEID = TUBE_FEATURE.FEATUREID  ORDER BY STARTTIME ASC ) DETAIL ";

            SqlStr += "    ON MAIN.IO_ITEMID = DETAIL.TUBEROW ";
            DataTable dt_io_tube = iom.DBExecSQL(SqlStr);

            List<IO_Inquire> IOList = new List<IO_Inquire>();
            IOList = (List<IO_Inquire>)dt_io_tube.ToList<IO_Inquire>();
            string tubejson = JsonConvert.SerializeObject(IOList);

            json_result.attachment = tubejson;


            ViewBag.tubeData = json_result;
            //return Content(JsonConvert.SerializeObject(json_result), "application/json");
            return Content(JsonConvert.SerializeObject(ViewBag), "application/json");
            //return View();

        }

        //~VitalSignController()
        //{
        //    base.Dispose(false);
        //}
    }

    
}