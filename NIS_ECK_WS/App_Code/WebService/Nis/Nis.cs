using System;
using System.Data;
using System.Globalization;
using System.Web;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Web.Script.Services;
using System.Web.Script.Serialization;
using System.Collections;
using NIS.Models;
using System.Collections.Generic;
using NIS.Data;
using Newtonsoft.Json;
using RCS.Data;
using System.IO;
using System.Net;

[WebService(Namespace = "http://digisoft.com.tw/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
public class Nis : System.Web.Services.WebService
{
    GetEBM.GetEBMWsServiceSoapClient WK_EBM = new GetEBM.GetEBMWsServiceSoapClient();
    public Nis()
    {
        //如果使用設計的元件，請取消註解下行程式碼 
        //InitializeComponent(); 
    }

    private string StrFilter(string inStr)
    {
        //return inStr.Replace("'", "").Replace("＃", "").Replace("<", "＜").Replace(">", "＞");
        return inStr;
    }
    private DAOCostCenter DAOCostCenter = new DAOCostCenter();
    private DAOUser DAOUser = new DAOUser();
    private DAOPatient DAOPatient = new DAOPatient();
    private GetSql GetSql = new GetSql();
    private GetNISSql GetNISSql = new GetNISSql();

    private Rcs Rcs = new Rcs();
    public DateTime StrTransformDate(string ROCDate, string ROCTime)
    {
        //ROCDate = "1050223";
        DateTime dt;
        string Year = (int.Parse(ROCDate.Substring(0, 3)) + 1911).ToString();
        string Month = ROCDate.Substring(3, 2);
        string Day = ROCDate.Substring(5, 2);
        string OutputTime = "";
        if (ROCTime != "")
        {
            OutputTime = ROCTime.Substring(0, 2) + ":" + ROCTime.Substring(2, 2) + ":00";
        }
        else
        {
            OutputTime = "00:00:00";
        }
        string OutputDate = Year + "/" + Month + "/" + Day + " " + OutputTime;
        dt = Convert.ToDateTime(OutputDate);
        return dt;
    }

    /// <summary> 
    /// 取得成本中心列表 Finish
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetCostCenterList()
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("CostCenterList");
        dtSql2 = GetSql.SqlLst2("CostCenterList");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }
        
        DataTable dt;
        dt = DAOCostCenter.GetCostCenterList(sqlCombine);
        if (dt.Rows.Count > 0)
        {
            CostCenterList[] e = new CostCenterList[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new CostCenterList();
                e[i].CostCenterCode = dt.Rows[i]["CostCenterCode"].ToString();
                e[i].CCCDescription = dt.Rows[i]["CCCDescription"].ToString();
            }

            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }
	
	//[WebMethod]
 //   [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
 //   public byte[] GetVitalSignbyDate(string a, string b, string c, string d)
 //   {
        
 //           return null;
 //   }

    /// <summary>
    /// 頻次表
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetFre()
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("Fre");
        dtSql2 = GetSql.SqlLst2("Fre");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt;
        dt = DAOCostCenter.GetFre(sqlCombine);
        if (dt.Rows.Count > 0)
        {
            Fre[] e = new Fre[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new Fre();
                e[i].FreCode = dt.Rows[i]["FRECODE"].ToString();
                e[i].FreName = dt.Rows[i]["FRENAME"].ToString();
                e[i].FreTime = dt.Rows[i]["FreTime1"].ToString() + "|" + dt.Rows[i]["FreTime2"].ToString() + "|";
                e[i].FreTime = e[i].FreTime + dt.Rows[i]["FreTime3"].ToString() + "|" + dt.Rows[i]["FreTime4"].ToString() + "|";
                e[i].FreTime = e[i].FreTime + dt.Rows[i]["FreTime5"].ToString() + "|" + dt.Rows[i]["FreTime6"].ToString() + "|";
                e[i].FreTime = e[i].FreTime + dt.Rows[i]["FreTime7"].ToString() + "|" + dt.Rows[i]["FreTime8"].ToString() + "|";
                e[i].FreTime = e[i].FreTime + dt.Rows[i]["FreTime9"].ToString() + "|" + dt.Rows[i]["FreTime10"].ToString() + "|";
                e[i].FreTime = e[i].FreTime + dt.Rows[i]["FreTime11"].ToString() + "|" + dt.Rows[i]["FreTime12"].ToString() + "|";
                e[i].FreTime = e[i].FreTime + dt.Rows[i]["FreTime13"].ToString() + "|" + dt.Rows[i]["FreTime14"].ToString() + "|";
                e[i].FreTime = e[i].FreTime + dt.Rows[i]["FreTime15"].ToString() + "|" + dt.Rows[i]["FreTime16"].ToString() + "|";
                e[i].FreTime = e[i].FreTime + dt.Rows[i]["FreTime17"].ToString() + "|" + dt.Rows[i]["FreTime18"].ToString() + "|";
                e[i].FreTime = e[i].FreTime + dt.Rows[i]["FreTime19"].ToString() + "|" + dt.Rows[i]["FreTime20"].ToString() + "|";
                e[i].FreTime = e[i].FreTime + dt.Rows[i]["FreTime21"].ToString() + "|" + dt.Rows[i]["FreTime22"].ToString() + "|";
                e[i].FreTime = e[i].FreTime + dt.Rows[i]["FreTime23"].ToString() + "|" + dt.Rows[i]["FreTime24"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 取得單位所有人員清單 Finish
    /// </summary>
    /// <param name="CostCenterCode">成本中心代碼</param>
    /// <returns></returns>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetUserList(string CostCenterCode)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("UserList");
        dtSql2 = GetSql.SqlLst2("UserList");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }
        
        DataTable dt = new DataTable();
        dt = DAOUser.UserList(CostCenterCode, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            UserInfo[] e = new UserInfo[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new UserInfo();
                e[i].Category = dt.Rows[i]["Category"].ToString();
                e[i].CostCenterCode = dt.Rows[i]["CostCenterCode"].ToString();
                e[i].CostCenterName = dt.Rows[i]["CostCenterName"].ToString();
                e[i].EmployeesName = dt.Rows[i]["EmployeesName"].ToString();
                e[i].EmployeesNo = dt.Rows[i]["EmployeesNo"].ToString();
                e[i].JobGrade = dt.Rows[i]["JobGrade"].ToString();
                e[i].UserID = dt.Rows[i]["UserID"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 使用者登入判斷 Finish
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] UserLogin(string EmployessNo, string EmployessPwd)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("UserLogin");
        dtSql2 = GetSql.SqlLst2("UserLogin");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOUser.UserLogin(EmployessNo, EmployessPwd, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            UserInfo[] e = new UserInfo[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new UserInfo();
                e[i].Category = dt.Rows[i]["Category"].ToString();
                e[i].CostCenterCode = dt.Rows[i]["CostCenterCode"].ToString();
                e[i].CostCenterName = dt.Rows[i]["CostCenterName"].ToString();
                e[i].EmployeesName = dt.Rows[i]["EmployeesName"].ToString();
                e[i].EmployeesNo = dt.Rows[i]["EmployeesNo"].ToString();
                e[i].JobGrade = dt.Rows[i]["JobGrade"].ToString();
                e[i].UserID = dt.Rows[i]["UserID"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e[0]));
            return result;
        }
        else
        {
            return null;
        }
    }


    /// <summary>
    /// 取得使用者姓名Finish
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] UserName(string EmployessNo)
    {
        //log(EmployessNo,"剛傳入參數");
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("UserName");
        dtSql2 = GetSql.SqlLst2("UserName");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOUser.UserName(EmployessNo, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            //log(EmployessNo, dt.Rows.Count.ToString());
            UserInfo[] e = new UserInfo[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new UserInfo();

                e[i].Category = dt.Rows[i]["Category"].ToString();
                e[i].CostCenterCode = dt.Rows[i]["CostCenterCode"].ToString();
                e[i].CostCenterName = dt.Rows[i]["CostCenterName"].ToString();
                e[i].EmployeesName = dt.Rows[i]["EmployeesName"].ToString();
                e[i].EmployeesNo = dt.Rows[i]["EmployeesNo"].ToString();
                e[i].JobGrade = dt.Rows[i]["JobGrade"].ToString();
                e[i].UserID = dt.Rows[i]["UserID"].ToString();

                //e[i].EmployeesNo = EmployessNo;
                //e[i].EmployeesName = dt.Rows[i]["EmployeesName"].ToString();
                //e[i].CostCenterNo = dt.Rows[i]["CostCenterNo"].ToString();
                //e[i].CostCenterName = dt.Rows[i]["CostCenterName"].ToString();
                //e[i].UserID = dt.Rows[i]["UserID"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e[0]));
            return result;
        }
        else
        {
           // log(EmployessNo, dt.Rows.Count.ToString());
            return null;
        }
    }

    #region Patient List

    /// <summary>
    /// 患者清單轉床號清單 
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetPatientListByBedList(string[] bedList)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("PatientListByBedList");
        dtSql2 = GetSql.SqlLst2("PatientListByBedList");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();

        dt = DAOPatient.GetPatientList(bedList, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            PatientList[] e = new PatientList[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new PatientList();
                e[i].ChrNo = dt.Rows[i]["ChrNo"].ToString();
                e[i].FeeNo = dt.Rows[i]["FeeNo"].ToString();
                e[i].BedNo = dt.Rows[i]["BedNo"].ToString();
                e[i].PatientName = dt.Rows[i]["PatientName"].ToString();
                //e[i].Note = dt.Rows[i]["note"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 取得患者清單 Finish
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetPatientList(string CCCode)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("PatientList");
        dtSql2 = GetSql.SqlLst2("PatientList");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetPatientList(CCCode, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            PatientList[] e = new PatientList[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new PatientList();
                e[i].ChrNo = dt.Rows[i]["ChrNo"].ToString();
                e[i].FeeNo = dt.Rows[i]["FeeNo"].ToString();
                e[i].BedNo = dt.Rows[i]["BedNo"].ToString();
                e[i].PatientName = dt.Rows[i]["PatientName"].ToString();
               // e[i].Note = dt.Rows[i]["note"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    #endregion

    /// <summary>
    /// 取得病人資料Finish
    /// </summary>
    /// <param name="FeeNo">住院序號</param>
    /// <returns>回傳病人基本資料</returns>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetPatientInfo(string FeeNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        string DRIVE_ID = "OPD";
        string DRIVE_ID2 = "OPD";
        int CLINIC_DATE_YEAR = 0;
        if ((FeeNo.Trim() ).Length > 10)
        {
            dtSql = GetSql.SqlLst("PatientInfoER");
            dtSql2 = GetSql.SqlLst2("PatientInfoER");
            CLINIC_DATE_YEAR = int.Parse(FeeNo.ToString().Substring(10, 3));
            switch (CLINIC_DATE_YEAR % 5)
            {
                case 1:
                    DRIVE_ID = "OHIS" + FeeNo.ToString().Substring(13, 2);
                    DRIVE_ID2="ONH" + FeeNo.ToString().Substring(13, 2);
                    break;
                case 2:
                    DRIVE_ID = "OHISB" + FeeNo.ToString().Substring(13, 2);
                    DRIVE_ID2 = "ONHB" + FeeNo.ToString().Substring(13, 2);
                    break;
                case 3:
                    DRIVE_ID = "OHISC" + FeeNo.ToString().Substring(13, 2);
                    DRIVE_ID2 = "ONHC" + FeeNo.ToString().Substring(13, 2);
                    break;
                case 4:
                    DRIVE_ID = "OHISD" + FeeNo.ToString().Substring(13, 2);
                    DRIVE_ID2 = "ONHD" + FeeNo.ToString().Substring(13, 2);
                    break;
                case 0:
                    DRIVE_ID = "OHISE" + FeeNo.ToString().Substring(13, 2);
                    DRIVE_ID2 = "ONHE" + FeeNo.ToString().Substring(13, 2);
                    break;
            }
        }
        else
        {
            dtSql = GetSql.SqlLst("PatientInfo");
            dtSql2 = GetSql.SqlLst2("PatientInfo");
        }
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetPatientInfo(FeeNo.Trim(), DRIVE_ID , DRIVE_ID2, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            PatientInfo[] e = new PatientInfo[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new PatientInfo();
                e[i].FeeNo = FeeNo;
                e[i].ChartNo = dt.Rows[i]["ChartNo"].ToString().Trim();
                e[i].BedNo = dt.Rows[i]["BedNo"].ToString().Trim();
                if (dt.Rows[i]["PatientGender"].ToString() == "1")
                    e[i].PatientGender = "男";
                else
                    e[i].PatientGender = "女";
                e[i].PatientName = dt.Rows[i]["PatientName"].ToString().Trim();
                DateTime Birthday = StrTransformDate(dt.Rows[i]["Birthday"].ToString(), "");
                e[i].Birthday = Birthday;
                //新生兒年齡要？歲？個月
                e[i].Age = int.Parse((DateTime.Now.Year - e[i].Birthday.Year).ToString());
                e[i].Month = int.Parse((DateTime.Now.Month - e[i].Birthday.Month).ToString());
                if (e[i].Month <= 0)
                {
                    if (e[i].Age == 0 & e[i].Month == 0)
                    {
                        e[i].Age = 0;
                        e[i].Month = 0;
                    }
                    else
                    {
                       e[i].Age = e[i].Age - 1;    
                       e[i].Month = e[i].Month + 12;
                    }
                }
                e[i].Blood_Type= dt.Rows[i]["Blood_Type"].ToString().Trim() + " " + dt.Rows[i]["Blood_Type2"].ToString().Trim();
                e[i].DocName = dt.Rows[i]["DocName"].ToString().Trim();
                e[i].DocNo = dt.Rows[i]["DocNo"].ToString().Trim();
                DateTime Indate = StrTransformDate(dt.Rows[i]["InDate"].ToString(), dt.Rows[i]["InTime"].ToString());
                e[i].InDate = Indate;
                if (Int32 .Parse(dt.Rows[i]["OutDate"].ToString()) + Int32.Parse(dt.Rows[i]["OutTime"].ToString()) > 0)
                {
                    DateTime OutDate = StrTransformDate(dt.Rows[i]["OutDate"].ToString(),"");
                    e[i].OutDate = OutDate;
                }
                else
                {
                    e[i].OutDate = DateTime.MinValue;
                }
                   
                DateTime Indate24 = StrTransformDate(dt.Rows[i]["InDate"].ToString(),"000000");
                TimeSpan Total = DateTime.Now.Subtract(Indate24);
                e[i].InDay = int.Parse(Total.Days.ToString()) + 1;

                e[i].ICD9_code1 = dt.Rows[i]["ICD9_code1"].ToString().Trim() + "(" + dt.Rows[i]["mdiag_desc1"].ToString().Trim() + ")";
                if (dt.Rows[i]["ICD9_code2"].ToString().Trim() != "" && dt.Rows[i]["sdiag_desc2"].ToString().Trim() != "")
                    e[i].ICD9_code2 = dt.Rows[i]["ICD9_code2"].ToString().Trim() + "(" + dt.Rows[i]["sdiag_desc2"].ToString().Trim() + ")";
                else
                    e[i].ICD9_code2 = "";
                if (dt.Rows[i]["ICD9_code3"].ToString().Trim() != "" && dt.Rows[i]["sdiag_desc3"].ToString().Trim() != "")
                    e[i].ICD9_code3 = dt.Rows[i]["ICD9_code3"].ToString().Trim() + "(" + dt.Rows[i]["sdiag_desc3"].ToString().Trim() + ")";
                else
                    e[i].ICD9_code3 = "";
                if (dt.Rows[i]["ICD9_code4"].ToString().Trim() != "" && dt.Rows[i]["sdiag_desc4"].ToString().Trim() != "")
                    e[i].ICD9_code4 = dt.Rows[i]["ICD9_code4"].ToString().Trim() + "(" + dt.Rows[i]["sdiag_desc4"].ToString().Trim() + ")";
                else
                    e[i].ICD9_code4 = "";
                if (dt.Rows[i]["ICD9_code5"].ToString().Trim() != "" && dt.Rows[i]["sdiag_desc5"].ToString().Trim() != "")
                    e[i].ICD9_code5 = dt.Rows[i]["ICD9_code5"].ToString().Trim() + "(" + dt.Rows[i]["sdiag_desc5"].ToString().Trim() + ")";
                else
                    e[i].ICD9_code5 = "";
                e[i].CancerICD9 = dt.Rows[i]["CancerICD9"].ToString().Trim();

                //e[i].DNR = dt.Rows[i]["DNR"].ToString();
                //if (int.Parse(dt.Rows[i]["NHDNR"].ToString()) > 0)
                //    e[i].DNR = "Y";
                //else
                if (dt.Rows[i]["DNR"].ToString().Trim ().Length > 0)
                    e[i].DNR = "Y";
                else
                    e[i].DNR = "N";
                //e[i].Hospice = dt.Rows[i]["Hospice"].ToString();
                //1070322
                //if (int.Parse(dt.Rows[i]["Hospice"].ToString()) > 0)
                //    e[i].Hospice = "Y";
                //else
                e[i].Hospice = "N";
                e[i].Suicide = dt.Rows[i]["Suicide"].ToString();
                //e[i].OrganDonation = dt.Rows[i]["OrganDonation"].ToString();
                //e[i].OrganDonation = "N";
                //if (int.Parse(dt.Rows[i]["NHOrganDonation"].ToString()) > 0)
                //    e[i].OrganDonation = "Y";
                //else

                if ((dt.Rows[i]["OrganDonation"].ToString().Trim().Length) > 0)
                    e[i].OrganDonation = "Y";
                else
                    e[i].OrganDonation = "N";

                e[i].card_specialinfo  = GET_DNR_CODE_NAME(dt.Rows[i]["CARD_SPECIALINFO"].ToString().Trim()); //健保卡特殊註記
                e[i].hospital_specialinfo = GET_DNR_CODE_NAME(dt.Rows[i]["HOSP_SPECIALINFO"].ToString().Trim());//醫院特殊註記

                //e[i].Allergy = dt.Rows[i]["Allergy"].ToString();
                if (int.Parse(dt.Rows[i]["Allergy"].ToString()) > 0)
                    e[i].Allergy = "Y";
                else
                    e[i].Allergy = "N";
                e[i].Security = dt.Rows[i]["Security"].ToString();
                e[i].NursePractitioner = dt.Rows[i]["NursePractitioner"].ToString();//專師
                e[i].Terminally = dt.Rows[i]["Terminally"].ToString(); //是否病危(若病危回傳"Y" )
                e[i].DeptNo = dt.Rows[i]["DeptNo"].ToString();
                e[i].DeptName = dt.Rows[i]["DeptName"].ToString();
                e[i].PatientID = dt.Rows[i]["PatientID"].ToString();
                e[i].CostCenterNo = dt.Rows[i]["CostCenterNo"].ToString();
                e[i].CostCenterName = dt.Rows[i]["CostCenterName"].ToString();
                e[i].PayInfo = dt.Rows[i]["PayInfo"].ToString();
                e[i].PatientAddress = dt.Rows[i]["PatientAddress"].ToString(); //患者地址
                e[i].PatientHomeNo = dt.Rows[i]["PatientHomeNo"].ToString(); //患者家裡電話
                e[i].PatientWorkNo = dt.Rows[i]["PatientWorkNo"].ToString(); //患者公司電話
                e[i].PatientMobile = dt.Rows[i]["PatientMobile"].ToString(); //患者行動電話
                e[i].PatientEmail = dt.Rows[i]["PatientEmail"].ToString(); //患者電子郵件
                e[i].PatientMarryStatus = dt.Rows[i]["PatientMarryStatus"].ToString(); //患者婚姻狀況
                e[i].PatientReligion = dt.Rows[i]["PatientReligion"].ToString(); //患者宗教 x
                e[i].PatientBirthPlace = dt.Rows[i]["PatientBirthPlace"].ToString(); //患者出生地
                e[i].PatientSpouseName = dt.Rows[i]["PatientSpouseName"].ToString(); //患者配偶姓名  x
                e[i].ContactName = dt.Rows[i]["ContactName"].ToString(); //緊急聯絡人姓名
                e[i].ContactRelationship = dt.Rows[i]["ContactRelationship"].ToString(); //緊急聯絡人關係
                e[i].ContactHomeNo = dt.Rows[i]["ContactHomeNo"].ToString(); //緊急聯絡人家裡電話
                e[i].ContactWorkNo = dt.Rows[i]["ContactWorkNo"].ToString(); //緊急聯絡人公司電話 x
                e[i].ContactMobile = dt.Rows[i]["ContactMobile"].ToString(); //緊急聯絡人行動電話 x
                e[i].Contactemail = dt.Rows[i]["Contactemail"].ToString(); //緊急聯絡人電子郵件 x

                if (FeeNo.Length > 10)
                {
                    e[i].Assessment = "ER";
                }
                else
                {
                    if (e[i].Age >= 18)//A為成人、C為兒童、D-新生兒、E-嬰幼兒
                        e[i].Assessment ="A"; //預帶評估項目代碼 x
                    else
                    {
                        e[i].Assessment ="C"; //預帶評估項目代碼 x
                        switch (e[i].BedNo)
                        {
                            case "B101":
                            case "B102": 
                            case "B103": 
                            case "B105":
                            case "B201": 
                            case "B202": 
                            case "B203": 
                            case "B205": 
                            case "B206": 
                            case "B207":
                            case "B208": 
                            case "B301": 
                            case "B302": 
                            case "B303": 
                            case "B305": 
                            case "B306": 
                            case "B307": 
                            case "B501": 
                            case "B502": 
                            case "B503": 
                            case "B505": 
                            case "B506": 
                            case "B507": 
                            case "B508": 
                            case "B509":
                            case "B601A":
                            case "B602A":
                            case "B603A":
                            case "B605A": e[i].Assessment = "D"; break;

                            case "P1":
                            case "P2":
                            case "P3":
                            case "P5":
                            case "P6":
                            case "P7":
                            case "BI1":
                            case "BI2":
                            case "BI3":
                            case "BI5":
                            case "BI6":
                            case "BI7":
                            case "BI8":
                            case "BI9":
                            case "BI10":
                            case "BI11":
                            case "BI12":
                                if (e[i].Age == 0 & e[i].Month <=1 )
                                       {
                                        e[i].Assessment = "D";
                                        }
                                else
                                       {
                                        e[i].Assessment = "E";
                                        }
                                break;
                        }
                        
                        if (Int32.Parse(dt.Rows[i]["MIN_MOM"].ToString()) > 0)
                        {
                            e[i].Assessment = "A"; 
                        }
                        switch (e[i].ChartNo)
                        {
                            case "0008513260":
                            case "0008795948":
                        //    case "0008119950":
                        //    case "0004589985":
                        //    case "0006700165":
                        //    case "0004388563":
                                e[i].Assessment = "D"; break;
                        }
                    }    
                }
                e[i].duty_code = "";
                switch (dt.Rows[i]["DeptNo"].ToString())
                {
                    case "5000": e[i].duty_code = "BM"; break;
                    //case "5100": break;
                }
                switch (e[i].Assessment)
                {
                    case "D": e[i].duty_code = "BM"; break;
                    case "E": e[i].duty_code = "BM"; break;
                }

                string Trans_Dt = GetPatientTrans(FeeNo.Trim ()).ToString();
                if (!String.IsNullOrEmpty(Trans_Dt))
                {
                    //e[i].TransDate = (System.DateTime)(GetPatientTrans(FeeNo));
                    e[i].TransDate = Convert.ToDateTime(Trans_Dt);
                }
                else
                {
                    e[i].TransDate = DateTime.MinValue;
                }
                //e[i].Condition = dt.Rows[i]["Condition"].ToString();
                switch (dt.Rows[i]["Condition"].ToString())
                {
                    case "0": e[i].Condition = "Condition：Unknown"; break;
                    case "1": e[i].Condition = "Condition：Satisfactory"; break;
                    case "2": e[i].Condition = "Condition：Serious"; break;
                    case "3": e[i].Condition = "Condition：Critical"; break;
                    case "4": e[i].Condition = "Condition：Guarded"; break;
                    case "5": e[i].Condition = "Condition：Stable"; break;
                    case "6": e[i].Condition = "Condition：Unstable"; break;
                }
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e[0]));
            return result;
            //this.Context.Response.ContentType = "application/json";
            //this.Context.Response.Write(new JavaScriptSerializer().Serialize(e[0]));
        }
        else
        {
            return null;
            //this.Context.Response.ContentType = "application/json";
            //this.Context.Response.Write(new JavaScriptSerializer().Serialize(null));
        }
    }

    /// <summary>
    /// 取得病人資料Finish
    /// </summary>
    /// <param name="FeeNo">住院序號</param>
    /// <returns>回傳病人基本資料</returns>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetPatientNote(string FeeNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("PatientNote");
        dtSql2 = GetSql.SqlLst2("PatientNote");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetPatientNote(FeeNo, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            PatientInfo[] e = new PatientInfo[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new PatientInfo();
                e[i].FeeNo = FeeNo;
                e[i].ChartNo = dt.Rows[i]["ChartNo"].ToString().Trim();
                e[i].BedNo = dt.Rows[i]["BedNo"].ToString().Trim();
                if (dt.Rows[i]["PatientGender"].ToString() == "1")
                   e[i].PatientGender = "男";
                else
                    e[i].PatientGender = "女";
                e[i].PatientName = dt.Rows[i]["PatientName"].ToString().Trim();
                //新生兒年齡要？歲？個月
                e[i].Age = int.Parse((DateTime.Now.Year - e[i].Birthday.Year).ToString());
                if (e[i].Month < 0)
                {
                    e[i].Age = e[i].Age - 1;
                }
                e[i].DocName = dt.Rows[i]["DocName"].ToString().Trim();
                DateTime Indate = StrTransformDate(dt.Rows[i]["InDate"].ToString(), dt.Rows[i]["InTime"].ToString());
                e[i].InDate = Indate;
                if (int.Parse(dt.Rows[i]["NHDNR"].ToString()) > 0)//是否拒絕急救
                    e[i].DNR = "Y";
                else
                    if (int.Parse(dt.Rows[i]["DNR"].ToString()) > 0)//是否拒絕急救
                        e[i].DNR = "Y";
                    else
                        e[i].DNR = "N";
                if (int.Parse(dt.Rows[i]["Hospice"].ToString()) > 0)//是否為安寧病房患者
                    e[i].Hospice = "Y";
                else
                    e[i].Hospice = "N";
                e[i].Suicide = dt.Rows[i]["Suicide"].ToString();//是否有自殺傷傾向
                if (int.Parse(dt.Rows[i]["NHOrganDonation"].ToString()) > 0)//是否同意器官捐贈
                    e[i].OrganDonation = "Y";
                else
                    if (int.Parse(dt.Rows[i]["OrganDonation"].ToString()) > 0)//是否同意器官捐贈
                        e[i].OrganDonation = "Y";
                    else
                        e[i].OrganDonation = "N";
                if (int.Parse(dt.Rows[i]["Allergy"].ToString()) > 0)//是否有過敏
                    e[i].Allergy = "Y";
                else
                    e[i].Allergy = "N";
                e[i].Security = dt.Rows[i]["Security"].ToString();//是否需保密
                e[i].NursePractitioner = dt.Rows[i]["NursePractitioner"].ToString();//專師
                e[i].Terminally = dt.Rows[i]["Terminally"].ToString(); //是否病危(若病危回傳"Y" )
                e[i].CostCenterNo = dt.Rows[i]["CostCenterNo"].ToString();

                if (int.Parse(dt.Rows[i]["Exam"].ToString()) > 0)
                    e[i].Exam = "Y";
                else
                    e[i].Exam = "N";
                if (int.Parse(dt.Rows[i]["Surgery"].ToString()) > 0)
                    e[i].Surgery = "Y";
                else
                    e[i].Surgery = "N";
                if (int.Parse(dt.Rows[i]["Consultation"].ToString()) > 0)
                    e[i].Consultation = "Y";
                else
                    e[i].Consultation = "N";
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e[0]));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 預計出院床號清單
    /// </summary>

    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetExLeaveHospital()
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("ExLeaveHospital");
        dtSql2 = GetSql.SqlLst2("ExLeaveHospital");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetExLeaveHospital(sqlCombine);
        if (dt.Rows.Count > 0)
        {
            PatientInfo[] e = new PatientInfo[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                    e[i] = new PatientInfo();
                    e[i].BedNo = dt.Rows[i]["bedno"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }



    /// <summary>
    /// 取得患者最近一次轉床時間
    /// </summary>
    /// <param name="FeeNo">住院序號</param>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    private Nullable<DateTime> GetPatientTrans(string FeeNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        string DRIVE_ID = "OPD";
        int CLINIC_DATE_YEAR = 0;
        if (FeeNo.Trim().Length > 10)
        {
            dtSql = GetSql.SqlLst("PatientTransLastER");
            dtSql2 = GetSql.SqlLst2("PatientTransLastER");
            CLINIC_DATE_YEAR = int.Parse(FeeNo.Trim().ToString().Substring(10, 3));
            switch (CLINIC_DATE_YEAR % 5)
            {
                case 1:
                    DRIVE_ID = "OHIS" + FeeNo.ToString().Substring(13, 2);
                    break;
                case 2:
                    DRIVE_ID = "OHISB" + FeeNo.ToString().Substring(13, 2);
                    break;
                case 3:
                    DRIVE_ID = "OHISC" + FeeNo.ToString().Substring(13, 2);
                    break;
                case 4:
                    DRIVE_ID = "OHISD" + FeeNo.ToString().Substring(13, 2);
                    break;
                case 0:
                    DRIVE_ID = "OHISE" + FeeNo.ToString().Substring(13, 2);
                    break;
            }
        }
        else
        {
            dtSql = GetSql.SqlLst("PatientTransLast");
            dtSql2 = GetSql.SqlLst2("PatientTransLast");
        }

        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetPatientTrans(FeeNo, DRIVE_ID, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            PatientInfo[] e = new PatientInfo[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new PatientInfo();
                e[i].TransDate = StrTransformDate(dt.Rows[i]["trans_date"].ToString(), dt.Rows[i]["trans_time"].ToString());
            }
            return e[0].TransDate; //只會有一筆資料
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 取得病人會診
    /// </summary>
    /// <param name="FeeNo">住院序號</param>
    /// <returns>回傳病人會診資料</returns>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetPatientConsult(string FeeNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("PatientConsult");
        dtSql2 = GetSql.SqlLst2("PatientConsult");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetPatientConsult(FeeNo, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            Consultation[] e = new Consultation[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new Consultation();
                //主KEY
                e[i].OrderNo = dt.Rows[i]["OrderNo"].ToString();
                //申請時間
                e[i].OrderDate = StrTransformDate(dt.Rows[i]["OrderDate"].ToString(), dt.Rows[i]["OrderTime"].ToString());
                
                //指定會診科別
                e[i].ConsDept = dt.Rows[i]["ConsDept"].ToString();
                //指定會診醫師
                e[i].ConsDoc = dt.Rows[i]["ConsDoc"].ToString();
                //會診回覆結果
                e[i].ConsContent = dt.Rows[i]["ConsContent"].ToString();

                //類別	ConsultFlag 0-會診，1-照會。
                e[i].ConsultFlag = dt.Rows[i]["ConsultFlag"].ToString();


                //會診類別
                switch (dt.Rows[i]["CONSULT_TYPE"].ToString())
                {
                    case "1":
                        e[i].Cateory = "ROUTINE";
                        break;
                    case "2":
                        e[i].Cateory = "ELECTIVE";
                        break;
                    case "3":
                        e[i].Cateory = "EMERGENCY";
                        break;
                    case "4":
                        e[i].Cateory = "FOLLOWING";
                        break;
                    case "5":
                        e[i].Cateory = "Nutrition Therapy";
                        break;
                    case "6":
                        e[i].Cateory = "ER Emergency";
                        break;
                    case "7":
                        e[i].Cateory = "ER Urgent";
                        break;
                    case "8":
                        e[i].Cateory = "ER Regular";
                        break;
                    case "9":
                        e[i].Cateory = "EXAMINATION";
                        break;
                }
                switch (dt.Rows[i]["CONSULT_DATE"].ToString())
                {
                    case "0":
                        e[i].Status = "未回覆";
                        break;
                    default:
                        e[i].Status = "已回覆";
                        break;
                }
                //申請醫師
                e[i].ApplicationDoc = dt.Rows[i]["APPLY_DOCTOR_NO"].ToString();
                //指定照會科別
                e[i].NoteDept = dt.Rows[i]["ConsDept"].ToString();
                //指定照會醫師
                e[i].NoteDoc    = dt.Rows[i]["ConsDoc"].ToString();
                //會診申請內容
                e[i].Summery = dt.Rows[i]["APPLY_SUMMARY"].ToString();
                //會診時間
                e[i].ReplyTime = dt.Rows[i]["CONSULT_DATE"].ToString() + "  " + dt.Rows[i]["CONSULT_HHMM"].ToString();
                //申請人
                e[i].EnterName = dt.Rows[i]["CLERK_NAME"].ToString();

            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 最後一次門診用藥
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetOpdMed(string FeeNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("OpdDate");
        dtSql2 = GetSql.SqlLst2("OpdDate");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }
        DataTable dt = new DataTable();
        dt = DAOPatient.GetOpdDate(FeeNo, sqlCombine);
        string DRIVE_ID = "OPD";
        if (dt.Rows.Count > 0)
        {
            int   CLINIC_DATE_YEAR = 0;
            
            CLINIC_DATE_YEAR = int.Parse ( dt.Rows[0]["CLINIC_DATE"].ToString().Substring(0, 3));
            switch (CLINIC_DATE_YEAR % 5)
            {
            case 1:
                DRIVE_ID = "OHIS" + dt.Rows[0]["CLINIC_DATE"].ToString().Substring(3, 2);
                break;
            case 2:
                DRIVE_ID = "OHISB" + dt.Rows[0]["CLINIC_DATE"].ToString().Substring(3, 2);
                break;
            case 3:
                DRIVE_ID = "OHISC" + dt.Rows[0]["CLINIC_DATE"].ToString().Substring(3, 2);
                break;
            case 4:
                DRIVE_ID = "OHISD" + dt.Rows[0]["CLINIC_DATE"].ToString().Substring(3, 2);
                break;
            case 0:
                DRIVE_ID = "OHISE" + dt.Rows[0]["CLINIC_DATE"].ToString().Substring(3, 2);
                break;
            }
        }
              
        sqlCombine = "";
        dtSql = GetSql.SqlLst("OpdMed");
        dtSql2 = GetSql.SqlLst2("OpdMed");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }
        DataTable dt2 = new DataTable();
        dt2 = DAOPatient.GetOpdMed(FeeNo, DRIVE_ID, sqlCombine);
        if (dt2.Rows.Count > 0)
        {
            DrugOrder[] e = new DrugOrder[dt2.Rows.Count];
            for (int i = 0; i < dt2.Rows.Count; i++)
            {
                e[i] = new DrugOrder();
                e[i].Med_code = dt2.Rows[i]["Med_code"].ToString();
                e[i].DrugName = dt2.Rows[i]["DrugName"].ToString();
                e[i].Feq = dt2.Rows[i]["Feq"].ToString(); //頻次
                e[i].Dose =dt2.Rows[i]["Dose"].ToString(); //劑量
                e[i].Route = dt2.Rows[i]["Route"].ToString(); //途徑
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 取得病患血型. 備血日期. 備血量. 血種. 餘血量(備血-領血)
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetBloodInfo(string FeeNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("BloodInfo");
        dtSql2 = GetSql.SqlLst2("BloodInfo");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetBloodInfo(FeeNo, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            BloodInfo[] e = new BloodInfo[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new BloodInfo();
                e[i].Blood_NO = dt.Rows[i]["BLOOD_NO"].ToString().Trim();
                e[i].Blood_Type = dt.Rows[i]["Blood_Type"].ToString().Trim() ; //血型
                DateTime Prepare_Date = StrTransformDate(dt.Rows[i]["Prepare_Date"].ToString(), dt.Rows[i]["READY_TIME"].ToString());
                e[i].Prepare_Date = Prepare_Date; //備血日期
                e[i].Code_Desc = dt.Rows[i]["Code_Desc"].ToString().Trim(); //血種
                e[i].Ttl_Gty = int.Parse(dt.Rows[i]["Ttl_Gty"].ToString()) ; //備血量
                e[i].Unrece_Gty = int.Parse(dt.Rows[i]["Unrece_Gty"].ToString()); //餘血量
                e[i].Blood_Name = dt.Rows[i]["Blood_Name"].ToString().Trim() + " 劑量: " + int.Parse(dt.Rows[i]["Ttl_Gty"].ToString()) + " U";//血品名稱
                e[i].Reason = dt.Rows[i]["Reason"].ToString().Trim();//備血原因
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }
    
    /// <summary>
    /// 取得病患藥品.食物.其他過敏清單
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetAllergyList(string FeeNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("AllergyList");
        dtSql2 = GetSql.SqlLst2("AllergyList");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetAllergyList(FeeNo, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            PatientInfo[] e = new PatientInfo[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new PatientInfo();
                e[i].AllergyDesc =  dt.Rows[i]["AllergyDesc"].ToString().Trim();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {            
            return null;
        }
    }

    /// <summary>
    /// 出院三日(含)內病患清單(含系統日當天)
    /// <param name="CostCode">成本中心代碼</param>
    /// </summary>
    /// <returns>回傳病患清單</returns>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetOut3Days(String CostCode)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("Out3Days");
        dtSql2 = GetSql.SqlLst2("Out3Days");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }
        string DRIVE_ID = "";
        switch (int.Parse((DateTime.Now.Month).ToString()))
        {
            case 1:
                DRIVE_ID = "IHIS0" + int.Parse((DateTime.Now.Month).ToString());
                break;
            case 2:
                DRIVE_ID = "IHIS0" + int.Parse((DateTime.Now.Month).ToString());
                break;
            case 3:
                DRIVE_ID = "IHIS0" + int.Parse((DateTime.Now.Month).ToString());
                break;
            case 4:
                DRIVE_ID = "IHIS0" + int.Parse((DateTime.Now.Month).ToString());
                break;
            case 5:
                DRIVE_ID = "IHIS0" + int.Parse((DateTime.Now.Month).ToString());
                break;
            case 6:
                DRIVE_ID = "IHIS0" + int.Parse((DateTime.Now.Month).ToString());
                break;
            case 7:
                DRIVE_ID = "IHIS0" + int.Parse((DateTime.Now.Month).ToString());
                break;
            case 8:
                DRIVE_ID = "IHIS0" + int.Parse((DateTime.Now.Month).ToString());
                break;
            case 9:
                DRIVE_ID = "IHIS0" + int.Parse((DateTime.Now.Month).ToString());
                break;
            case 10:
                DRIVE_ID = "IHIS" + int.Parse((DateTime.Now.Month).ToString());
                break;
            case 11:
                DRIVE_ID = "IHIS" + int.Parse((DateTime.Now.Month).ToString());
                break;
            case 12:
                DRIVE_ID = "IHIS" + int.Parse((DateTime.Now.Month).ToString());
                break;
        }
        string DRIVE_ID_2 = "";
        switch (int.Parse((DateTime.Now.Month).ToString()))
        {
            case 1:
                DRIVE_ID_2 = "IHIS12" ;
                break;
            case 2:
                DRIVE_ID_2 = "IHIS01";
                break;
            case 3:
                DRIVE_ID_2 = "IHIS02" ;
                break;
            case 4:
                DRIVE_ID_2 = "IHIS03" ;
                break;
            case 5:
                DRIVE_ID_2 = "IHIS04" ;
                break;
            case 6:
                DRIVE_ID_2 = "IHIS05" ;
                break;
            case 7:
                DRIVE_ID_2 = "IHIS06";
                break;
            case 8:
                DRIVE_ID_2 = "IHIS07";
                break;
            case 9:
                DRIVE_ID_2 = "IHIS08" ;
                break;
            case 10:
                DRIVE_ID_2 = "IHIS09" ;
                break;
            case 11:
                DRIVE_ID_2 = "IHIS10" ;
                break;
            case 12:
                DRIVE_ID_2 = "IHIS11" ;
                break;
        }
        DataTable dt = new DataTable();
        dt = DAOCostCenter.Out3Days(CostCode, DRIVE_ID, DRIVE_ID_2, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            PatientInfo[] e = new PatientInfo[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new PatientInfo();
                e[i].BedNo = dt.Rows[i]["BedNo"].ToString();
                e[i].ChartNo = dt.Rows[i]["ChartNo"].ToString();
                e[i].PatientName = dt.Rows[i]["PatientName"].ToString();
                DateTime Indate = StrTransformDate(dt.Rows[i]["ADMIT_DATE"].ToString(), dt.Rows[i]["ADMIT_TIME"].ToString());
                e[i].InDate = Indate;
                DateTime Outdate = StrTransformDate(dt.Rows[i]["DISCHARGE_DATE"].ToString(), dt.Rows[i]["DISCHARGE_TIME"].ToString());
                e[i].OutDate = Outdate;
                if (dt.Rows[i]["ICD_CODE"].ToString().Trim() != "")
                    e[i].ICD9_code1 = dt.Rows[i]["ICD_CODE"].ToString().Trim() + dt.Rows[i]["ICD_DESC"].ToString().Trim();
                else
                    e[i].ICD9_code1 = "";
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 取得護理人員班表 等待管發完成中
    /// <param name="JobDate">排班日期</param>
    /// <param name="Shift_cate">班別</param>
    /// <param name="costcode">成本中心代碼</param>
    /// </summary>
    /// <returns>回傳護理人員值班表</returns>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetShiftData(String JobDate, String Shift_cate, String costcode)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("ShiftData");
        dtSql2 = GetSql.SqlLst2("ShiftData");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOUser.GetShift(JobDate, Shift_cate, costcode, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            ShiftData[] e = new ShiftData[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new ShiftData();
                e[i].employe_no = dt.Rows[i]["employe_no"].ToString();
                e[i].employe_name = dt.Rows[i]["employe_name"].ToString();
                e[i].employe_title = dt.Rows[i]["employe_title"].ToString();
                e[i].shift_cate = dt.Rows[i]["shift_cate"].ToString();
               // e[i].cost_name = dt.Rows[i]["cost_name"].ToString();     
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /* HIS 資料不足，改由 NIS 帶入上次紀錄
    /// <summary>
    /// 取得患者基本資料
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetPatientBasic(String FeeNo)
    {
        DataTable dt = new DataTable(); ;
        dt = DAOPatient.GetPatientBasic(FeeNo);
        if (dt.Rows.Count > 0)
        {
            PatientBasic[] e = new PatientBasic[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new PatientBasic();
                e[i].InDate = ((DateTime)dt.Rows[i]["InDate"]);
                e[i].edu = dt.Rows[i]["edu"].ToString();
                e[i].Profession = dt.Rows[i]["Profession"].ToString();
                e[i].PatientName = dt.Rows[i]["PatientName"].ToString();
                e[i].EmergencyContact = dt.Rows[i]["EmergencyContact"].ToString();
                e[i].Relationship = dt.Rows[i]["Relationship"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }
    */

    /// <summary>
    /// 患者住院歷程 Finish
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetInHistory(String PatientNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("InHistory");
        dtSql2 = GetSql.SqlLst2("InHistory");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetInHistory(PatientNo, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            InHistory[] e = new InHistory[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new InHistory();
                e[i].InTime = int.Parse(dt.Rows[i]["InTime"].ToString());
                DateTime indate = StrTransformDate(dt.Rows[i]["InDate"].ToString(), "");
                e[i].indate = indate;
                //if(int.Parse(dt.Rows[i]["OutDate"].ToString()) == 0)
                //{
                //    e[i].outdate = StrTransformDate("9991231", "");
                //}
                // else
                //{
                //    e[i].outdate = StrTransformDate(dt.Rows[i]["OutDate"].ToString(), "");
                //}
                string outdate_Dt = GetPatientDisDate(dt.Rows[i]["FeeNo"].ToString().Trim()).ToString();
                if (!String.IsNullOrEmpty(outdate_Dt))
                {
                    //e[i].TransDate = (System.DateTime)(GetPatientTrans(FeeNo));
                    e[i].outdate = Convert.ToDateTime(outdate_Dt);
                }
                else
                {
                    e[i].outdate = DateTime.MinValue;
                }

                e[i].Description = dt.Rows[i]["Description"].ToString().Trim() + "(" + dt.Rows[i]["mdiag_desc"].ToString().Trim() + ")";
                e[i].FeeNo = dt.Rows[i]["FeeNo"].ToString().Trim();
                if(int.Parse(dt.Rows[i]["IpdFlag"].ToString().Trim()) == 1)
                {
                    e[i].IpdFlag = "Y";
                }
                else
                {
                    e[i].IpdFlag = "N";
                }
                e[i].HIS_TYPE = dt.Rows[i]["HIS_TYPE"].ToString().Trim();
                e[i].CostCode = dt.Rows[i]["CostCode"].ToString().Trim();
                e[i].DeptName = dt.Rows[i]["DeptName"].ToString().Trim();
                e[i].ChrNo = dt.Rows[i]["ChrNo"].ToString().Trim();
                e[i].PatName = dt.Rows[i]["PacName"].ToString().Trim();
                e[i].SexType = dt.Rows[i]["SexType"].ToString().Trim();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 取得患者最近一次轉床時間
    /// </summary>
    /// <param name="FeeNo">住院序號</param>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    private Nullable<DateTime> GetPatientDisDate(string FeeNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        string DRIVE_ID = "OPD";
        int CLINIC_DATE_YEAR = 0;
        if (FeeNo.Trim().Length > 10)
        {
            dtSql = GetSql.SqlLst("PatientDisDateER");
            dtSql2 = GetSql.SqlLst2("PatientDisDateER");
            CLINIC_DATE_YEAR = int.Parse(FeeNo.Trim().ToString().Substring(10, 3));
            switch (CLINIC_DATE_YEAR % 5)
            {
                case 1:
                    DRIVE_ID = "OHIS" + FeeNo.ToString().Substring(13, 2);
                    break;
                case 2:
                    DRIVE_ID = "OHISB" + FeeNo.ToString().Substring(13, 2);
                    break;
                case 3:
                    DRIVE_ID = "OHISC" + FeeNo.ToString().Substring(13, 2);
                    break;
                case 4:
                    DRIVE_ID = "OHISD" + FeeNo.ToString().Substring(13, 2);
                    break;
                case 0:
                    DRIVE_ID = "OHISE" + FeeNo.ToString().Substring(13, 2);
                    break;
            }
        }
        else
        {
            dtSql = GetSql.SqlLst("PatientDisDate");
            dtSql2 = GetSql.SqlLst2("PatientDisDate");
        }

        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetPatientDisDate(FeeNo, DRIVE_ID, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            PatientInfo[] e = new PatientInfo[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new PatientInfo();

                if (int.Parse(dt.Rows[i]["DIS_DATE"].ToString()) == 0)
                {
                    e[i].TransDate = StrTransformDate("9991231", "");
                }
                else
                {
                    e[i].TransDate = StrTransformDate(dt.Rows[i]["DIS_DATE"].ToString(),"");
                }

            }
            return e[0].TransDate; //只會有一筆資料
        }
        else
        {
            return null;
        }
    }





    private void IF(bool v)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 取得患者營養醫囑 Finish
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetDietOrder(String FeeNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("DietOrder");
        dtSql2 = GetSql.SqlLst2("DietOrder");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }
        DataTable dt = new DataTable();
        dt = DAOPatient.GetDietOrder(FeeNo, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            TextOrder[] e = new TextOrder[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new TextOrder();
                e[i].SheetNo = dt.Rows[i]["SheetNo"].ToString();
                e[i].Category = dt.Rows[i]["Category"].ToString();
                DateTime OrderStartDate = StrTransformDate(dt.Rows[i]["START_DATE"].ToString(), dt.Rows[i]["START_TIME"].ToString());
                e[i].OrderStartDate = OrderStartDate;
                DateTime OrderEndDate;
                if ((dt.Rows[i]["DC_DATE"].ToString() + dt.Rows[i]["DC_TIME"].ToString()) != "00000000000")
                {
                    DateTime NowDateTime = DateTime.Now;
                    OrderEndDate = StrTransformDate(dt.Rows[i]["DC_DATE"].ToString(), dt.Rows[i]["DC_TIME"].ToString());
                    if (NowDateTime > OrderEndDate)
                    {
                        e[i].OrderEndDate = OrderEndDate;
                        e[i].DC_FLAG = "DC";
                        e[i].SheetNo = "D" + dt.Rows[i]["SheetNo"].ToString();
                    }
                    else
                    {
                        e[i].OrderEndDate = OrderEndDate;
                        e[i].DC_FLAG = "";
                    }
                }
                else
                {
                    e[i].OrderEndDate = DateTime.MaxValue;
                    e[i].DC_FLAG = "";
                }
                string WK_CONTENT = "";
                int WK_DAYS = 0;
                if (dt.Rows[i]["TEXT_TYPE"].ToString() == "LT")
                {
                    if (dt.Rows[i]["PRICING_FLAG"].ToString() == "S")
                    {
                        WK_CONTENT = "[自費]";
                    }
                    if (WK_CONTENT == "")
                    {
                        WK_CONTENT = dt.Rows[i]["KEYIN_CODE"].ToString() + " - " + dt.Rows[i]["TEXT_ORDER_TYPE"].ToString() + " - " + dt.Rows[i]["ORDER_DESC"] + " - " + dt.Rows[i]["FREQUENCY_CODE"].ToString();
                    }
                    else
                    {
                        WK_CONTENT = WK_CONTENT + "#br#" + dt.Rows[i]["KEYIN_CODE"].ToString() + " - " + dt.Rows[i]["TEXT_ORDER_TYPE"].ToString() + " - " + dt.Rows[i]["ORDER_DESC"].ToString() + " - " + dt.Rows[i]["FREQUENCY_CODE"].ToString();
                    }
                }
                if (dt.Rows[i]["TEXT_TYPE"].ToString() == "ST")
                {
                    if (dt.Rows[i]["MAKEUP_FLAG"].ToString() == "Y")
                    {
                        WK_CONTENT = "[補登]";
                    }
                    if (dt.Rows[i]["PRICING_FLAG"].ToString() == "S")
                    {
                        WK_CONTENT = WK_CONTENT + "[自費]";
                    }
                    if (WK_CONTENT == "")
                    {
                        WK_CONTENT = "預計執行時間:" + StrTransformDate(dt.Rows[i]["START_DATE"].ToString(), dt.Rows[i]["START_TIME"].ToString()) + "#br#" + dt.Rows[i]["KEYIN_CODE"].ToString() + " - " + dt.Rows[i]["TEXT_ORDER_TYPE"].ToString() + " - " + dt.Rows[i]["ORDER_DESC"].ToString();
                    }
                    else
                    {
                        WK_CONTENT = WK_CONTENT + "#br#" + "預計執行時間:" + StrTransformDate(dt.Rows[i]["START_DATE"].ToString(), dt.Rows[i]["START_TIME"].ToString()) + "#br#" + dt.Rows[i]["KEYIN_CODE"].ToString() + " - " + dt.Rows[i]["TEXT_ORDER_TYPE"].ToString() + " - " + dt.Rows[i]["ORDER_DESC"].ToString();
                    }
                }
                if (dt.Rows[i]["TEXT_TYPE"].ToString() == "LE")
                {
                    //e[i].OrderStartDate = StrTransformDate(dt.Rows[i]["ORDER_DATE"].ToString(), dt.Rows[i]["ORDER_TIME"].ToString());
                    if (dt.Rows[i]["EMG_FLAG"].ToString() == "Y")
                    {
                        WK_CONTENT = "[急件]";
                    }
                    if (dt.Rows[i]["PRICING_FLAG"].ToString() == "S")
                    {
                        WK_CONTENT = WK_CONTENT + "[自費]";
                    }
                    if (WK_CONTENT == "")
                    {
                        WK_CONTENT = dt.Rows[i]["ORDER_DESC"].ToString() + dt.Rows[i]["KEYIN_CODE"].ToString() + " - " + dt.Rows[i]["TEXT_ORDER_TYPE"].ToString() + " - "
                                           + dt.Rows[i]["FREQUENCY_CODE"].ToString() + "#br#" + "備註:" + dt.Rows[i]["ORDER_REMARK"].ToString();
                    }
                    else
                    {
                        WK_CONTENT = WK_CONTENT + "#br#" + dt.Rows[i]["ORDER_DESC"].ToString() + dt.Rows[i]["KEYIN_CODE"].ToString() + " - " + dt.Rows[i]["TEXT_ORDER_TYPE"].ToString() + " - "
                                           + dt.Rows[i]["FREQUENCY_CODE"].ToString() + "#br#" + "備註:" + dt.Rows[i]["ORDER_REMARK"].ToString();
                    }
                }
                if (dt.Rows[i]["TEXT_TYPE"].ToString() == "SE")
                {
                    e[i].OrderStartDate = StrTransformDate(dt.Rows[i]["ORDER_DATE"].ToString(), dt.Rows[i]["ORDER_TIME"].ToString());
                    if (dt.Rows[i]["MAKEUP_FLAG"].ToString() == "Y")
                    {
                        WK_CONTENT = "[補登]";
                    }
                    if (dt.Rows[i]["EMG_FLAG"].ToString() == "Y")
                    {
                        WK_CONTENT = WK_CONTENT + "[急件]";
                    }
                    if (dt.Rows[i]["PRICING_FLAG"].ToString() == "S")
                    {
                        WK_CONTENT = WK_CONTENT + "[自費]";
                    }
                    if (WK_CONTENT == "")
                    {
                        WK_CONTENT = "預計執行時間:" + StrTransformDate(dt.Rows[i]["START_DATE"].ToString(), dt.Rows[i]["START_TIME"].ToString()) + "#br#";
                        WK_CONTENT = WK_CONTENT + dt.Rows[i]["ORDER_DESC"].ToString() + dt.Rows[i]["KEYIN_CODE"].ToString() + " - " + dt.Rows[i]["TEXT_ORDER_TYPE"].ToString()
                            + "#br#" + " 備註:" + dt.Rows[i]["ORDER_REMARK"].ToString();
                    }
                    else
                    {
                        WK_CONTENT = WK_CONTENT + "#br#" + "預計執行時間:" + StrTransformDate(dt.Rows[i]["START_DATE"].ToString(), dt.Rows[i]["START_TIME"].ToString()) + "#br#";
                        WK_CONTENT = WK_CONTENT + dt.Rows[i]["ORDER_DESC"].ToString() + dt.Rows[i]["KEYIN_CODE"].ToString() + " - " + dt.Rows[i]["TEXT_ORDER_TYPE"].ToString()
                            + "#br#" + " 備註:" + dt.Rows[i]["ORDER_REMARK"].ToString();
                    }
                }
                DateTime OrderOrderDate;
                if (dt.Rows[i]["TEXT_TYPE"].ToString() == "DI")
                {
                    OrderOrderDate = StrTransformDate(dt.Rows[i]["ORDER_DATE"].ToString(), dt.Rows[i]["ORDER_TIME"].ToString());
                    if (dt.Rows[i]["PRICING_FLAG"].ToString() == "S")
                    {
                        WK_CONTENT = "[自費]";
                    }
                    if (dt.Rows[i]["POWDER_REMARK"].ToString() == "Y")
                    {
                        WK_CONTENT = WK_CONTENT + "[磨粉]";
                    }
                    if (dt.Rows[i]["MAKEUP_FLAG"].ToString() == "Y")
                    {
                        WK_CONTENT = WK_CONTENT + "[補登]";
                    }
                    if (WK_CONTENT == "")
                    {
                        WK_CONTENT = "預計出院日期:" + OrderOrderDate + "#br#" + dt.Rows[i]["TEXT_ORDER_TYPE"].ToString() + "#br#" + "備註:" + dt.Rows[i]["ORDER_REMARK"].ToString();
                    }
                    else
                    {
                        WK_CONTENT = "預計出院日期:" + OrderOrderDate + "#br#" + WK_CONTENT + "#br#" + dt.Rows[i]["TEXT_ORDER_TYPE"].ToString() + "#br#" + "備註:" + dt.Rows[i]["ORDER_REMARK"].ToString();
                    }
                }
                if (dt.Rows[i]["TEXT_TYPE"].ToString() == "DM")
                {
                    OrderOrderDate = StrTransformDate(dt.Rows[i]["ORDER_DATE"].ToString(), dt.Rows[i]["ORDER_TIME"].ToString());
                    if (dt.Rows[i]["PRICING_FLAG"].ToString() == "S")
                    {
                        WK_CONTENT = "[自費]";
                    }
                    if (dt.Rows[i]["POWDER_REMARK"].ToString() == "Y")
                    {
                        WK_CONTENT = WK_CONTENT + "[磨粉]";
                    }
                    if (dt.Rows[i]["MAKEUP_FLAG"].ToString() == "Y")
                    {
                        WK_CONTENT = WK_CONTENT + "[補登]";
                    }
                    WK_DAYS = int.Parse(dt.Rows[i]["DAYS"].ToString()) + int.Parse(dt.Rows[i]["SELF_DAYS"].ToString());
                    if (WK_CONTENT == "")
                    {
                        WK_CONTENT = "預計出院日期:" + OrderOrderDate + "#br#" + dt.Rows[i]["TEXT_ORDER_TYPE"].ToString() + " - " + dt.Rows[i]["DOSE_QTY"].ToString() + dt.Rows[i]["DOSE_UNIT"].ToString() + " - " + dt.Rows[i]["METHOD_CODE"].ToString()
                            + " - " + dt.Rows[i]["FREQUENCY_CODE"].ToString() + "(" + WK_DAYS.ToString() + "天)"
                            + "#br#" + "備註:" + dt.Rows[i]["ORDER_REMARK"].ToString();
                    }
                    else
                    {
                        WK_CONTENT = "預計出院日期:" + OrderOrderDate + "#br#" + WK_CONTENT + "#br#" + dt.Rows[i]["TEXT_ORDER_TYPE"].ToString() + " - " + dt.Rows[i]["DOSE_QTY"].ToString() + dt.Rows[i]["DOSE_UNIT"].ToString() + " - " + dt.Rows[i]["METHOD_CODE"].ToString()
                            + " - " + dt.Rows[i]["FREQUENCY_CODE"].ToString() + "(" + WK_DAYS.ToString() + "天)"
                            + "#br#" + "備註:" + dt.Rows[i]["ORDER_REMARK"].ToString();
                    }
                }
                e[i].Content = WK_CONTENT;
            }

            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }


    /// <summary>
    /// 取得患者文字醫囑 Finish
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetTextOrder(String FeeNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("TextOrder");
        dtSql2 = GetSql.SqlLst2("TextOrder");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }
        string DRIVE_ID = "OPD";
        int CLINIC_DATE_YEAR = 0;
        if (FeeNo.Length > 10)
        {
            CLINIC_DATE_YEAR = int.Parse(FeeNo.ToString().Substring(10, 3));
            switch (CLINIC_DATE_YEAR % 5)
            {
                case 1:
                    DRIVE_ID = "OHIS" + FeeNo.ToString().Substring(13, 2);
                    break;
                case 2:
                    DRIVE_ID = "OHISB" + FeeNo.ToString().Substring(13, 2);
                    break;
                case 3:
                    DRIVE_ID = "OHISC" + FeeNo.ToString().Substring(13, 2);
                    break;
                case 4:
                    DRIVE_ID = "OHISD" + FeeNo.ToString().Substring(13, 2);
                    break;
                case 0:
                    DRIVE_ID = "OHISE" + FeeNo.ToString().Substring(13, 2);
                    break;
            }
        }
        DataTable dt = new DataTable();
        dt = DAOPatient.GetTextOrder(FeeNo, DRIVE_ID, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            TextOrder[] e = new TextOrder[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new TextOrder();
                e[i].SheetNo = dt.Rows[i]["SheetNo"].ToString();
                e[i].Category = dt.Rows[i]["Category"].ToString();
                DateTime OrderStartDate = StrTransformDate(dt.Rows[i]["START_DATE"].ToString(), dt.Rows[i]["START_TIME"].ToString());
                e[i].OrderStartDate = OrderStartDate;
                DateTime OrderEndDate;
                if ((dt.Rows[i]["DC_DATE"].ToString() + dt.Rows[i]["DC_TIME"].ToString()) != "00000000000")
                {
                    DateTime NowDateTime = DateTime.Now;
                    OrderEndDate = StrTransformDate(dt.Rows[i]["DC_DATE"].ToString(), dt.Rows[i]["DC_TIME"].ToString());
                    if (NowDateTime > OrderEndDate)
                    {
                        e[i].OrderEndDate = OrderEndDate;
                        e[i].DC_FLAG = "DC";
                        e[i].SheetNo = "D"+ dt.Rows[i]["SheetNo"].ToString();
                    }
                    else
                    {
                        e[i].OrderEndDate = OrderEndDate;
                        e[i].DC_FLAG = "";
                    }
                }
                else
                {
                    e[i].OrderEndDate = DateTime.MaxValue;
                    e[i].DC_FLAG = "";
                }
                string WK_CONTENT = "";
                int WK_DAYS = 0;
                if (dt.Rows[i]["TEXT_TYPE"].ToString() == "LT")
                {
                    if (dt.Rows[i]["PRICING_FLAG"].ToString() == "S")
                    {
                        WK_CONTENT = "[自費]";
                    }
                    if (WK_CONTENT == "")
                    {
                        WK_CONTENT =  dt.Rows[i]["KEYIN_CODE"].ToString() + " - " + dt.Rows[i]["TEXT_ORDER_TYPE"].ToString() + " - " + dt.Rows[i]["ORDER_DESC"] + " - " + dt.Rows[i]["FREQUENCY_CODE"].ToString();
                    }
                    else
                    {
                        WK_CONTENT = WK_CONTENT + "#br#" + dt.Rows[i]["KEYIN_CODE"].ToString() + " - " + dt.Rows[i]["TEXT_ORDER_TYPE"].ToString() + " - " + dt.Rows[i]["ORDER_DESC"].ToString() + " - " + dt.Rows[i]["FREQUENCY_CODE"].ToString();
                    }
                }
                if (dt.Rows[i]["TEXT_TYPE"].ToString() == "ST")
                {
                    if (dt.Rows[i]["MAKEUP_FLAG"].ToString() == "Y")
                    {
                        WK_CONTENT = "[補登]";
                    }
                    if (dt.Rows[i]["PRICING_FLAG"].ToString() == "S")
                    {
                        WK_CONTENT = WK_CONTENT + "[自費]";
                    }
                    if (WK_CONTENT == "")
                    {
                        WK_CONTENT = "預計執行時間:" + StrTransformDate(dt.Rows[i]["START_DATE"].ToString() , dt.Rows[i]["START_TIME"].ToString()) + "#br#" + dt.Rows[i]["KEYIN_CODE"].ToString() + " - " + dt.Rows[i]["TEXT_ORDER_TYPE"].ToString() + " - " + dt.Rows[i]["ORDER_DESC"].ToString();
                    }
                    else
                    {
                        WK_CONTENT = WK_CONTENT + "#br#" + "預計執行時間:" + StrTransformDate(dt.Rows[i]["START_DATE"].ToString() , dt.Rows[i]["START_TIME"].ToString())+ "#br#" + dt.Rows[i]["KEYIN_CODE"].ToString() + " - " + dt.Rows[i]["TEXT_ORDER_TYPE"].ToString() + " - " + dt.Rows[i]["ORDER_DESC"].ToString();
                    }
                }
                if (dt.Rows[i]["TEXT_TYPE"].ToString() == "LE")
                {
                    //e[i].OrderStartDate = StrTransformDate(dt.Rows[i]["ORDER_DATE"].ToString(), dt.Rows[i]["ORDER_TIME"].ToString());
                    if (dt.Rows[i]["EMG_FLAG"].ToString() == "Y")
                    {
                        WK_CONTENT = "[急件]";
                    }
                    if (dt.Rows[i]["PRICING_FLAG"].ToString() == "S")
                    {
                        WK_CONTENT = WK_CONTENT + "[自費]";
                    }
                    if (WK_CONTENT == "")
                    {
                        WK_CONTENT = dt.Rows[i]["ORDER_DESC"].ToString() + dt.Rows[i]["KEYIN_CODE"].ToString() + " - " + dt.Rows[i]["TEXT_ORDER_TYPE"].ToString() + " - "
                                           + dt.Rows[i]["FREQUENCY_CODE"].ToString() + "#br#" + "備註:" + dt.Rows[i]["ORDER_REMARK"].ToString();
                    }
                    else
                    {
                        WK_CONTENT = WK_CONTENT + "#br#" + dt.Rows[i]["ORDER_DESC"].ToString() + dt.Rows[i]["KEYIN_CODE"].ToString() + " - " + dt.Rows[i]["TEXT_ORDER_TYPE"].ToString() + " - "
                                           + dt.Rows[i]["FREQUENCY_CODE"].ToString() + "#br#" + "備註:" + dt.Rows[i]["ORDER_REMARK"].ToString();
                    }
                }
                if (dt.Rows[i]["TEXT_TYPE"].ToString() == "SE")
                {
                    e[i].OrderStartDate = StrTransformDate(dt.Rows[i]["ORDER_DATE"].ToString(), dt.Rows[i]["ORDER_TIME"].ToString());
                    if (dt.Rows[i]["MAKEUP_FLAG"].ToString() == "Y")
                    {
                        WK_CONTENT = "[補登]";
                    }
                    if (dt.Rows[i]["EMG_FLAG"].ToString() == "Y")
                    {
                        WK_CONTENT = WK_CONTENT + "[急件]";
                    }
                    if (dt.Rows[i]["PRICING_FLAG"].ToString() == "S")
                    {
                        WK_CONTENT = WK_CONTENT + "[自費]";
                    }
                    if (WK_CONTENT == "")
                    {
                        WK_CONTENT = "預計執行時間:" + StrTransformDate(dt.Rows[i]["START_DATE"].ToString(), dt.Rows[i]["START_TIME"].ToString()) + "#br#";
                        WK_CONTENT = WK_CONTENT + dt.Rows[i]["ORDER_DESC"].ToString() + dt.Rows[i]["KEYIN_CODE"].ToString() + " - " + dt.Rows[i]["TEXT_ORDER_TYPE"].ToString() 
                            + "#br#" + " 備註:" + dt.Rows[i]["ORDER_REMARK"].ToString();
                    }
                    else
                    {
                        WK_CONTENT = WK_CONTENT + "#br#" + "預計執行時間:" + StrTransformDate(dt.Rows[i]["START_DATE"].ToString(), dt.Rows[i]["START_TIME"].ToString()) + "#br#";
                        WK_CONTENT = WK_CONTENT + dt.Rows[i]["ORDER_DESC"].ToString() + dt.Rows[i]["KEYIN_CODE"].ToString() + " - " + dt.Rows[i]["TEXT_ORDER_TYPE"].ToString() 
                            + "#br#" + " 備註:" + dt.Rows[i]["ORDER_REMARK"].ToString();
                    }
                }
                DateTime OrderOrderDate;
                if (dt.Rows[i]["TEXT_TYPE"].ToString() == "DI")
                {
                    OrderOrderDate = StrTransformDate(dt.Rows[i]["ORDER_DATE"].ToString(), dt.Rows[i]["ORDER_TIME"].ToString());
                    if (dt.Rows[i]["PRICING_FLAG"].ToString() == "S")
                    {
                        WK_CONTENT = "[自費]";
                    }
                    if (dt.Rows[i]["POWDER_REMARK"].ToString() == "Y")
                    {
                        WK_CONTENT = WK_CONTENT + "[磨粉]";
                    }
                    if (dt.Rows[i]["MAKEUP_FLAG"].ToString() == "Y")
                    {
                        WK_CONTENT = WK_CONTENT + "[補登]";
                    }
                    if (WK_CONTENT == "")
                    {
                        WK_CONTENT = "預計出院日期:" + OrderOrderDate + "#br#" + dt.Rows[i]["TEXT_ORDER_TYPE"].ToString() + "#br#" + "備註:" + dt.Rows[i]["ORDER_REMARK"].ToString();
                    }
                    else
                    {
                        WK_CONTENT = "預計出院日期:" + OrderOrderDate + "#br#" + WK_CONTENT + "#br#" + dt.Rows[i]["TEXT_ORDER_TYPE"].ToString() + "#br#" + "備註:" + dt.Rows[i]["ORDER_REMARK"].ToString();
                    }
                }
                if (dt.Rows[i]["TEXT_TYPE"].ToString() == "DM")
                {
                    OrderOrderDate = StrTransformDate(dt.Rows[i]["ORDER_DATE"].ToString(), dt.Rows[i]["ORDER_TIME"].ToString());
                    if (dt.Rows[i]["PRICING_FLAG"].ToString() == "S")
                    {
                        WK_CONTENT = "[自費]";
                    }
                    if (dt.Rows[i]["POWDER_REMARK"].ToString() == "Y")
                    {
                        WK_CONTENT = WK_CONTENT + "[磨粉]";
                    }
                    if (dt.Rows[i]["MAKEUP_FLAG"].ToString() == "Y")
                    {
                        WK_CONTENT = WK_CONTENT + "[補登]";
                    }
                    WK_DAYS = int.Parse(dt.Rows[i]["DAYS"].ToString()) + int.Parse(dt.Rows[i]["SELF_DAYS"].ToString());
                    if (WK_CONTENT == "")
                    {
                        WK_CONTENT = "預計出院日期:" + OrderOrderDate + "#br#" +  dt.Rows[i]["TEXT_ORDER_TYPE"].ToString() + " - " + dt.Rows[i]["DOSE_QTY"].ToString() + dt.Rows[i]["DOSE_UNIT"].ToString() + " - " + dt.Rows[i]["METHOD_CODE"].ToString()
                            + " - " + dt.Rows[i]["FREQUENCY_CODE"].ToString() + "(" + WK_DAYS.ToString() + "天)"
                            + "#br#" + "備註:" + dt.Rows[i]["ORDER_REMARK"].ToString();
                    }
                    else
                    {
                        WK_CONTENT = "預計出院日期:" + OrderOrderDate + "#br#" + WK_CONTENT + "#br#" + dt.Rows[i]["TEXT_ORDER_TYPE"].ToString() + " - " + dt.Rows[i]["DOSE_QTY"].ToString() + dt.Rows[i]["DOSE_UNIT"].ToString() + " - " + dt.Rows[i]["METHOD_CODE"].ToString()
                            + " - " + dt.Rows[i]["FREQUENCY_CODE"].ToString() + "(" + WK_DAYS.ToString() + "天)"
                            + "#br#" + "備註:" + dt.Rows[i]["ORDER_REMARK"].ToString();
                    }
                }


                e[i].Content = WK_CONTENT;
            }

            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }
    
    /// <summary>
    /// 取得患者文字醫囑 8HR Finish
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetTextOrder8HR(String FeeNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("TextOrder8HR");
        dtSql2 = GetSql.SqlLst2("TextOrder8HR");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetTextOrder(FeeNo, "OPD",sqlCombine);
        if (dt.Rows.Count > 0)
        {
            TextOrder[] e = new TextOrder[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new TextOrder();

                e[i].SheetNo = dt.Rows[i]["SheetNo"].ToString();
                e[i].Category = dt.Rows[i]["Category"].ToString();
                DateTime OrderStartDate = StrTransformDate(dt.Rows[i]["START_DATE"].ToString(), dt.Rows[i]["START_TIME"].ToString());
                e[i].OrderStartDate = OrderStartDate;
                DateTime OrderEndDate;
                if (dt.Rows[i]["DC_DATE"].ToString() != "" && dt.Rows[i]["DC_TIME"].ToString() != "")
                {
                    OrderEndDate = StrTransformDate(dt.Rows[i]["DC_DATE"].ToString(), dt.Rows[i]["DC_TIME"].ToString());
                    e[i].OrderEndDate = OrderEndDate;
                }
                else
                {
                    e[i].OrderEndDate = DateTime.MaxValue;
                }
                e[i].Content = dt.Rows[i]["TEXT_ORDER_TYPE"].ToString() + dt.Rows[i]["ORDER_DESC"].ToString() +
                               dt.Rows[i]["FREQUENCY_CODE"].ToString();
            }

            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 取得患者藥囑 Finish
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetUdOrder(String FeeNo, String Flag)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        string sqlPosition = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();

        string DRIVE_ID = "OPD";
        int CLINIC_DATE_YEAR = 0;
        if (FeeNo.Length > 10)
        {
            if (Flag == "H")
            {
                dtSql = GetSql.SqlLst("UdOrderHR");
                dtSql2 = GetSql.SqlLst2("UdOrderHR");
            }
            else
            {
                dtSql = GetSql.SqlLst("UdOrderER");
                dtSql2 = GetSql.SqlLst2("UdOrderER");
            }
            CLINIC_DATE_YEAR = int.Parse(FeeNo.ToString().Substring(10, 3));
            switch (CLINIC_DATE_YEAR % 5)
            {
                case 1:
                    DRIVE_ID = "OHIS" + FeeNo.ToString().Substring(13, 2);
                    break;
                case 2:
                    DRIVE_ID = "OHISB" + FeeNo.ToString().Substring(13, 2);
                    break;
                case 3:
                    DRIVE_ID = "OHISC" + FeeNo.ToString().Substring(13, 2);
                    break;
                case 4:
                    DRIVE_ID = "OHISD" + FeeNo.ToString().Substring(13, 2);
                    break;
                case 0:
                    DRIVE_ID = "OHISE" + FeeNo.ToString().Substring(13, 2);
                    break;
            }
        }
        else
        {
            dtSql = GetSql.SqlLst("UdOrder");
            dtSql2 = GetSql.SqlLst2("UdOrder");
        }

        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }
        string WK_NOTE = string.Empty;
        DataTable dt = new DataTable();
        dt = DAOPatient.GetUdOrder(FeeNo, DRIVE_ID, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            UdOrder[] e = new UdOrder[dt.Rows.Count];
            //int j = 1;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                WK_NOTE = " ";
                    e[i] = new UdOrder();
                    e[i].UD_SEQ = dt.Rows[i]["UD_SEQ"].ToString();
                    e[i].UD_SEQ_OLD = dt.Rows[i]["UD_SEQ_OLD"].ToString();
                    e[i].UD_TYPE = dt.Rows[i]["UD_TYPE"].ToString();
                    e[i].UD_STATUS = dt.Rows[i]["UD_STATUS"].ToString();
                    e[i].FEE_NO = dt.Rows[i]["FEE_NO"].ToString();
                    //e[i].CHR_NO = dt.Rows[i]["CHR_NO"].ToString();
                    e[i].MED_CODE = dt.Rows[i]["MED_CODE"].ToString();
                    e[i].COST_CODE = dt.Rows[i]["COST_CODE"].ToString();
                    //e[i].BED_NO = dt.Rows[i]["BED_NO"].ToString();
                    //e[i].PAT_NAME = dt.Rows[i]["PAT_NAME"].ToString();
                    e[i].MED_DESC = StrFilter(dt.Rows[i]["MED_DESC"].ToString());
                    e[i].ALISE_DESC = dt.Rows[i]["ALISE_DESC"].ToString();
                    e[i].UD_DOSE =dt.Rows[i]["UD_DOSE"].ToString();
                    e[i].UD_UNIT = dt.Rows[i]["UD_UNIT"].ToString();
                    e[i].UD_CIR = dt.Rows[i]["UD_CIR"].ToString();
                    e[i].UD_PATH = dt.Rows[i]["UD_PATH"].ToString();
                    e[i].UD_LIMIT = dt.Rows[i]["UD_LIMIT"].ToString();
                    e[i].UD_QTY = dt.Rows[i]["UD_QTY"].ToString();
                    //e[i].PAY_FLAG = dt.Rows[i]["PAY_FLAG"].ToString();
                    e[i].PROG_FLAG = dt.Rows[i]["PROG_FLAG"].ToString();
                    e[i].BEGIN_DATE = dt.Rows[i]["BEGIN_DATE"].ToString();
                    e[i].BEGIN_TIME = dt.Rows[i]["BEGIN_TIME"].ToString();
                    e[i].DC_DATE = dt.Rows[i]["END_DATE"].ToString();
                    e[i].DC_TIME = dt.Rows[i]["END_TIME"].ToString();
                    
                    e[i].END_DATE = dt.Rows[i]["END_DATE"].ToString();
                    e[i].END_TIME = dt.Rows[i]["END_TIME"].ToString();

                    e[i].DRUG_TYPE = dt.Rows[i]["DRUG_TYPE"].ToString();
                if (dt.Rows[i]["MED_CODE"].ToString() == "HM")
                {
                    WK_NOTE = WK_NOTE + "自備";
                    e[i].MED_DESC = "自備藥(" + StrFilter(dt.Rows[i]["UD_CMD"].ToString()) + ")";
                    e[i].ALISE_DESC = "自備藥(" + StrFilter(dt.Rows[i]["UD_CMD"].ToString()) + ")";
                    //e[i].DRUG_TYPE = "Y";
                }
                else
                {
                    if (dt.Rows[i]["MAKEUP_FLAG"].ToString() == "T")
                    {
                        WK_NOTE = WK_NOTE + "自備";
                        //e[i].DRUG_TYPE = "Y";
                    }
                }
               
                if (dt.Rows[i]["POWDER_REMARK"].ToString() == "Y")
                {
                    if (WK_NOTE == " ")
                    { WK_NOTE = "磨粉"; }
                    else
                    { WK_NOTE = WK_NOTE + "、磨粉"; }
                }
                if (dt.Rows[i]["PRICING_FLAG"].ToString() == "Y")
                {
                    if (WK_NOTE == " ")
                    { WK_NOTE = "自費"; }
                    else
                    { WK_NOTE = WK_NOTE + "、自費"; }
                }
                
                if (dt.Rows[i]["REUSE_FLAG"].ToString() == "Y")
                {
                    if (WK_NOTE == " ")
                    { WK_NOTE = "留藥"; }
                    else
                    { WK_NOTE = WK_NOTE + "、留藥"; }
                }

                if (dt.Rows[i]["RECHECK_FLAG"].ToString() == "Y")
                {
                    if (WK_NOTE == " ")
                    { WK_NOTE = "高警訊藥"; }
                    else
                    { WK_NOTE = WK_NOTE + "、高警訊藥"; }
                }

                if (dt.Rows[i]["MED_UNGIVE_RETURN_FLAG"].ToString() == "N")
                {
                    if (WK_NOTE == " ")
                    { WK_NOTE = "不需退藥單"; }
                    else
                    {
                        if (dt.Rows[i]["PRICING_FLAG"].ToString() != "Y")
                        { WK_NOTE = WK_NOTE + "、不需退藥單"; }
                    }
                }

                if (WK_NOTE == " ")
                //{ e[i].UD_CMD = dt.Rows[i]["UD_CMD"].ToString().Replace("'", "").Replace("<", "＜").Replace(">", "＞").TrimEnd(); }
                { e[i].UD_CMD = StrFilter(dt.Rows[i]["UD_CMD"].ToString().TrimEnd()); }
                else
                //{ e[i].UD_CMD = dt.Rows[i]["UD_CMD"].ToString().Replace("'", "").Replace("<", "＜").Replace(">", "＞").TrimEnd() + "、" + WK_NOTE; }
                { e[i].UD_CMD = StrFilter(dt.Rows[i]["UD_CMD"].ToString().TrimEnd() + "、" + WK_NOTE); }
                    e[i].DOC_CODE = dt.Rows[i]["DOC_CODE"].ToString();
                    e[i].SEND_AMT = dt.Rows[i]["SEND_AMT"].ToString();
                    e[i].BACK_AMT = dt.Rows[i]["BACK_AMT"].ToString();
                    e[i].FEE_DATE = dt.Rows[i]["FEE_DATE"].ToString();
                    e[i].FEE_TIME = dt.Rows[i]["FEE_TIME"].ToString();
                    e[i].UD_DOSE_TOTAL = dt.Rows[i]["UD_DOSE_TOTAL"].ToString();
                    e[i].BEGIN_DAY = dt.Rows[i]["BEGIN_DAY"].ToString();
                    e[i].DC_FLAG = "";
                    //e[i].DC_DAY = dt.Rows[i]["DC_DAY"].ToString();
                    if (dt.Rows[i]["DC_FLAG"].ToString() == "DC")
                    {
                        e[i].DC_FLAG = "D"; // 排序用
                    }
                    e[i].DoubleCheck = dt.Rows[i]["DOUBLE_CHECK"].ToString();
                    e[i].DAY_CNT = dt.Rows[i]["DAY_CNT"].ToString();
                    //e[i].DRUG_TYPE = dt.Rows[i]["DRUG_TYPE"].ToString();
                    e[i].FLOW_SPEED = dt.Rows[i]["FLOW_SPEED"].ToString();
                    if (dt.Rows[i]["MED_CODE"].ToString().Trim() == "HM")//自備藥
                    {
                        e[i].DrugPicPath = "";
                    }
                    else
                    {
                    string DrugPicPath = GetDrugPicPath(dt.Rows[i]["MED_CODE"].ToString()).ToString();
                    if (!String.IsNullOrEmpty(DrugPicPath))
                        {
                            e[i].DrugPicPath = DrugPicPath;
                        }
                    }
            }

            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// </summary>
    /// <param name="MedNo">藥品</param>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    private string GetDrugPicPath(string MedNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("DrugPicPath");
        dtSql2 = GetSql.SqlLst2("DrugPicPath");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetDrugPicPath(MedNo, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            MedicalInfo[] e = new MedicalInfo[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new MedicalInfo();
                e[i].DrugPicPath = dt.Rows[i]["DrugPicPath"].ToString();
            }
            string result = e[0].DrugPicPath;
            return result;
        }
        else
        {
            return "";
        }
    }

    /// <summary>
    /// 取得患者出院帶藥
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetUdOrderD(String FeeNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("UdOrderD");
        dtSql2 = GetSql.SqlLst2("UdOrderD");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetUdOrderD(FeeNo, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            DrugOrder[] e = new DrugOrder[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new DrugOrder();
                e[i].Med_code = dt.Rows[i]["Med_code"].ToString();
                e[i].DrugName = dt.Rows[i]["DrugName"].ToString();
                e[i].Feq = dt.Rows[i]["Feq"].ToString(); //頻次
                e[i].Dose =dt.Rows[i]["Dose"].ToString(); //劑量
                e[i].Route = dt.Rows[i]["Route"].ToString(); //途徑
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 取得患者藥囑 Finish
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetDrugOrder(String FeeNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("DrugOrder");
        dtSql2 = GetSql.SqlLst2("DrugOrder");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetDrugOrder(FeeNo, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            DrugOrder[] e = new DrugOrder[dt.Rows.Count];
            int j = 1;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new DrugOrder();
                e[i].SheetNo = dt.Rows[i]["SheetNo"].ToString();
                int k = 0;
                if (i == 0)
                    i = k;
                else
                    k = i - 1;
                if (dt.Rows[i]["dc_date"].ToString() != "" && dt.Rows[i]["dc_time"].ToString() != "")
                {
                    e[i].DcFlag = "Y";
                    e[i].DcFlag = "DC";
                }
                else
                {
                    e[i].DcFlag = "N";
                    e[i].DcFlag = "";
                }
                if (e[i].SheetNo == e[k].SheetNo)
                    j = j + 1;
                else
                    j = 1;
                e[i].GiveSerial = j.ToString();
                string GiveTime = "";
                if (dt.Rows[i]["prsn_time"].ToString() != "")
                    GiveTime = dt.Rows[i]["prsn_time"].ToString();
                string GiveDate = DateTime.Today.AddYears(-1911).ToString("yyyy/MM/dd").Replace("/", "").Substring(1, 7);
                DateTime CheckDate = StrTransformDate(GiveDate, GiveTime);

                e[i].Category = dt.Rows[i]["ud_type"].ToString();
                if (dt.Rows[i]["OrderStartDate"].ToString() != "")
                    e[i].OrderStartDate = ((DateTime)dt.Rows[i]["OrderStartDate"]);
                else
                    e[i].OrderStartDate = DateTime.MinValue;
                //TimeSpan Total = new TimeSpan();
                if (dt.Rows[i]["OrderEndDate"].ToString() != "")
                {
                    e[i].OrderEndDate = ((DateTime)dt.Rows[i]["OrderEndDate"]);
                    //DateTime DayStar = StrTransformDate(GiveTime, "1700");
                    //Total = CheckDate.Subtract(e[i].OrderEndDate);
                }
                else
                {
                    e[i].OrderEndDate = DateTime.MaxValue;
                }
                if (GiveTime != "")
                {
                    e[i].GiveDate = CheckDate.ToString("yyyy/MM/dd");
                    e[i].GiveTime = CheckDate.ToString("HH:mm:ss");
                }
                else
                {
                    e[i].GiveDate = "";
                    e[i].GiveTime = "";
                }

                e[i].DrugName = StrFilter(dt.Rows[i]["med_desc"].ToString());
                e[i].GenericDrugs = StrFilter(dt.Rows[i]["alise_desc"].ToString());
                //e[i].Dose = float.Parse(dt.Rows[i]["ud_dose"].ToString());
                e[i].Dose =dt.Rows[i]["ud_dose"].ToString();
                e[i].DoseUnit = dt.Rows[i]["ud_unit"].ToString();
                e[i].Route = dt.Rows[i]["ud_path"].ToString();
                e[i].RateL = dt.Rows[i]["RateL"].ToString(); 
                e[i].RateH = dt.Rows[i]["RateH"].ToString();
                e[i].RateMemo = dt.Rows[i]["RateMemo"].ToString();
                e[i].CostTime = dt.Rows[i]["CostTime"].ToString();
                e[i].Feq = dt.Rows[i]["ud_cir"].ToString();
                e[i].Note = StrFilter(dt.Rows[i]["ud_cmd"].ToString()).TrimEnd() + dt.Rows[i]["FLOW_SPEED"].ToString();
                e[i].DoubleCheck = dt.Rows[i]["DoubleCheck"].ToString();
                e[i].Med_code = dt.Rows[i]["Med_code"].ToString();
                e[i].Ud_status = dt.Rows[i]["Ud_status"].ToString();
               // e[i].flow_speed = dt.Rows[i]["FLOW_SPEED"].ToString();
            }

            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 取得患者藥囑(未展開) Finish
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetIpdDrugOrder(String FeeNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("IpdDrugOrder");
        dtSql2 = GetSql.SqlLst2("IpdDrugOrder");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }
        string WK_NOTE = string.Empty;
        string DRIVE_ID = "OPD";
        int CLINIC_DATE_YEAR = 0;
        if (FeeNo.Length > 10)
        {
            CLINIC_DATE_YEAR = int.Parse(FeeNo.ToString().Substring(10, 3));
            switch (CLINIC_DATE_YEAR % 5)
            {
                case 1:
                    DRIVE_ID = "OHIS" + FeeNo.ToString().Substring(13, 2);
                    break;
                case 2:
                    DRIVE_ID = "OHISB" + FeeNo.ToString().Substring(13, 2);
                    break;
                case 3:
                    DRIVE_ID = "OHISC" + FeeNo.ToString().Substring(13, 2);
                    break;
                case 4:
                    DRIVE_ID = "OHISD" + FeeNo.ToString().Substring(13, 2);
                    break;
                case 0:
                    DRIVE_ID = "OHISE" + FeeNo.ToString().Substring(13, 2);
                    break;
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetIpdDrugOrder(FeeNo, DRIVE_ID, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            DrugOrder[] e = new DrugOrder[dt.Rows.Count];
            //int j = 1;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                WK_NOTE = " ";
                e[i] = new DrugOrder();
                e[i].SheetNo = dt.Rows[i]["SheetNo"].ToString();
                int k = 0;
                if (i == 0)
                    i = k;
                else
                    k = i - 1;
                if (dt.Rows[i]["UD_TYPE"].ToString()=="S")
                {
                    e[i].DcFlag = "N";
                }
                else
                {           
                    if ((dt.Rows[i]["OrderEndDate"].ToString() + dt.Rows[i]["OrderEndTime"].ToString()) != "00000000000")
                    {
                        DateTime NowDateTime = DateTime.Now;
                        DateTime OrderEndDate = StrTransformDate(dt.Rows[i]["OrderEndDate"].ToString(), dt.Rows[i]["OrderEndTime"].ToString());
                        if (NowDateTime > OrderEndDate)
                        {
                            e[i].DcFlag = "Y";
                            e[i].SheetNo = "D" + dt.Rows[i]["SheetNo"].ToString();
                        }
                        else
                            e[i].DcFlag = "N";
                    }
                    else
                        e[i].DcFlag = "N";
                }
                e[i].Category = dt.Rows[i]["ud_type"].ToString();
                if (dt.Rows[i]["OrderStartDate"].ToString() != "0000000")
                    e[i].OrderStartDate = StrTransformDate(dt.Rows[i]["OrderStartDate"].ToString(), dt.Rows[i]["OrderStartTime"].ToString());
                else
                    e[i].OrderStartDate = DateTime.MinValue;
                if (dt.Rows[i]["OrderEndDate"].ToString() != "0000000")
                {
                    e[i].OrderEndDate = StrTransformDate(dt.Rows[i]["OrderEndDate"].ToString(), dt.Rows[i]["OrderEndTime"].ToString());
                }
                else
                {
                    e[i].OrderEndDate = DateTime.MaxValue;
                }
                e[i].DrugName = StrFilter(dt.Rows[i]["DrugName"].ToString());
                e[i].GenericDrugs = StrFilter(dt.Rows[i]["GenericDrugs"].ToString());
                // e[i].Dose = float.Parse(dt.Rows[i]["Dose"].ToString());
                e[i].Dose =dt.Rows[i]["Dose"].ToString();
                e[i].DoseUnit = dt.Rows[i]["DoseUnit"].ToString();
                e[i].Route =  dt.Rows[i]["Route"].ToString();
                e[i].RateL = "";//dt.Rows[i]["RateL"].ToString();
                e[i].RateH = "";// dt.Rows[i]["RateH"].ToString();
                e[i].RateMemo = "";// dt.Rows[i]["RateMemo"].ToString();
                e[i].CostTime = "";// dt.Rows[i]["CostTime"].ToString();
                e[i].Feq = dt.Rows[i]["Feq"].ToString();

                if (dt.Rows[i]["Med_code"].ToString() == "HM")
                {
                    WK_NOTE = WK_NOTE + "自備";
                    e[i].DrugName = "自備藥(" + StrFilter(dt.Rows[i]["Note"].ToString()) + ")";
                    e[i].GenericDrugs = "自備藥(" + StrFilter(dt.Rows[i]["Note"].ToString()) + ")";
                }
                else
                {
                    if (dt.Rows[i]["MAKEUP_FLAG"].ToString() == "T")
                    {
                        WK_NOTE = WK_NOTE + "自備";
                    }
                }
                if (dt.Rows[i]["POWDER_REMARK"].ToString() == "Y")
                {
                    if (WK_NOTE == " ")
                    {WK_NOTE = " 磨粉";}
                    else
                    { WK_NOTE = WK_NOTE + "、磨粉"; }
                }
                if (dt.Rows[i]["PRICING_FLAG"].ToString() == "Y")
                {
                    if (WK_NOTE == " ")
                    { WK_NOTE = " 自費"; }
                    else
                    { WK_NOTE = WK_NOTE + "、自費"; }
                }
                if (WK_NOTE == " ")
                { e[i].Note = dt.Rows[i]["Note"].ToString().Replace("'", "").TrimEnd(); }
                else
                { e[i].Note = dt.Rows[i]["Note"].ToString().Replace("'", "").TrimEnd() + WK_NOTE ; }
                //e[i].Note = dt.Rows[i]["Note"].ToString().Replace("'", "").TrimEnd(); //+ dt.Rows[i]["FLOW_SPEED"].ToString();
                                e[i].DoubleCheck = dt.Rows[i]["DoubleCheck"].ToString();
                e[i].Med_code = dt.Rows[i]["Med_code"].ToString();
                e[i].Ud_status = dt.Rows[i]["Ud_status"].ToString();
                //e[i].flow_speed = dt.Rows[i]["FLOW_SPEED"].ToString();
            }

            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 取得患者化療藥囑 Finish
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetChemotherapyDrugOrder(String FeeNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("ChemotherapyDrugOrder");
        dtSql2 = GetSql.SqlLst2("ChemotherapyDrugOrder");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }
        
        DataTable dt = new DataTable();
        dt = DAOPatient.GetChemotherapyDrugOrder(FeeNo, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            ChemotherapyDrugOrder[] e = new ChemotherapyDrugOrder[dt.Rows.Count];
            string[,] MedList = new string[6, dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                MedList[0, i] = dt.Rows[i]["med_type"].ToString().Trim();
                MedList[1, i] = dt.Rows[i]["med_code"].ToString().Trim();
                MedList[2, i] = dt.Rows[i]["rely_med_code"].ToString().Trim();
                MedList[3, i] = dt.Rows[i]["seq_no"].ToString().Trim();
                MedList[4, i] = dt.Rows[i][0].ToString().Trim();
            }
            int j = 0;
            for (int x = 0; x < dt.Rows.Count; x++)
            {
                if (MedList[0, x] == "BEFORE")
                {
                    j = j + 1;
                    MedList[4, x] = j.ToString();
                }
            }
            for (int x = 0; x < dt.Rows.Count; x++)
            {
                if (MedList[0, x] == "CHEMO")
                {
                    j = j + 1;
                    MedList[4, x] = j.ToString();
                }
            }
            for (int x = 0; x < dt.Rows.Count; x++)
            {
                if (MedList[0, x] == "AFTER")
                {
                    j = j + 1;
                    MedList[4, x] = j.ToString();
                }
            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new ChemotherapyDrugOrder();
                e[i].OrderNo = dt.Rows[i]["serial_no"].ToString();                
                e[i].days = dt.Rows[i]["days"].ToString();

                e[i].Sequence = MedList[4, i];

                e[i].SheetNo = e[i].OrderNo + e[i].Sequence;
                if (dt.Rows[i]["OrderStartDate"].ToString() != "")
                    e[i].OrderStartDate = ((DateTime)dt.Rows[i]["OrderStartDate"]);
                else
                    e[i].OrderStartDate = DateTime.MinValue;
                if (dt.Rows[i]["OrderEndDate"].ToString() != "")
                    e[i].OrderEndDate = ((DateTime)dt.Rows[i]["OrderEndDate"]);
                else
                    e[i].OrderEndDate = DateTime.MaxValue;
                e[i].DrugName = StrFilter(dt.Rows[i]["med_desc"].ToString());
                e[i].GenericDrugs = StrFilter(dt.Rows[i]["alise_desc"].ToString());
                e[i].Dose = float.Parse(dt.Rows[i]["actual_qty"].ToString());
                e[i].DoseUnit = dt.Rows[i]["qty_unit"].ToString();
                e[i].Route = dt.Rows[i]["path_code"].ToString();
                e[i].Total = " ";
                e[i].Rate = dt.Rows[i]["flow_rate"].ToString(); //流速
                e[i].Feq = dt.Rows[i]["cir_code"].ToString();
                e[i].Memo = StrFilter(dt.Rows[i]["note"].ToString());
                e[i].ud_status = dt.Rows[i]["chemo_status"].ToString();
                e[i].dur_time = dt.Rows[i]["dur_time"].ToString();
                e[i].InfusionSolution = dt.Rows[i]["in_med_code"].ToString();
            }

            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 取得患者處置 
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetProcedure(String FeeNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("Procedure");
        dtSql2 = GetSql.SqlLst2("Procedure");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetProcedure(FeeNo, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            Procedure[] e = new Procedure[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new Procedure();
                DateTime ProcedureDate = StrTransformDate(dt.Rows[i]["EXECUTED_DATE"].ToString(), dt.Rows[i]["EXECUTED_TIME"].ToString());
                e[i].ProcedureDate = ProcedureDate;
                //e[i].ProcedureDate = Convert.ToDateTime(dt.Rows[i]["ProcedureDate"].ToString());
                e[i].ProcedureName = dt.Rows[i]["ProcedureName"].ToString();

                e[i].ProcedureCode = dt.Rows[i]["ProcedureCode"].ToString().Replace("\r\n", "#br#");
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 取得患者檢查 Finish
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetExam(String FeeNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
                if (FeeNo.Length > 10)
        {
            dtSql = GetSql.SqlLst("ExamER");
            dtSql2 = GetSql.SqlLst2("ExamER");
        }
        else
        {
            dtSql = GetSql.SqlLst("Exam");
            dtSql2 = GetSql.SqlLst2("Exam");
        }
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetExam(FeeNo, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            Exam[] e = new Exam[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new Exam();
                //e[i].ExamNo = dt.Rows[i]["ExamNo"].ToString(); //檢查單號+項目
                ////開單日期
                //if (dt.Rows[i]["OrderDate"].ToString() != "0000000")
                //{
                //    DateTime OrderDate = StrTransformDate(dt.Rows[i]["OrderDate"].ToString(), "");
                //    e[i].OrderDate = OrderDate;
                //}
                //else
                //{
                //    e[i].OrderDate = DateTime.MinValue;
                //}
                //檢查日期
                if (dt.Rows[i]["EXAM_DATE"].ToString() != "0000000")
                {
                    DateTime ExamDate = StrTransformDate(dt.Rows[i]["EXAM_DATE"].ToString(), dt.Rows[i]["EXAM_TIME"].ToString());
                    e[i].ExamDate = ExamDate;
                }
                else
                {
                    e[i].ExamDate = DateTime.MinValue;
                }
                e[i].ExamName = dt.Rows[i]["ExamName"].ToString();
                e[i].ExamReport = dt.Rows[i]["ExamReport"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 取得患者檢查 Finish
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetExambyDate(String FeeNo, String StartDate, String EndDate,string UserId,string UserPw)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        if (FeeNo.Length > 10)
        {
            dtSql = GetSql.SqlLst("ExambyDateER");
            dtSql2 = GetSql.SqlLst2("ExambyDateER");
        }
        else
        {
            dtSql = GetSql.SqlLst("ExambyDate");
            dtSql2 = GetSql.SqlLst2("ExambyDate");
        }
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

       DataTable dt = new DataTable();
        dt = DAOPatient.GetExambyDate(FeeNo, StartDate, EndDate, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            Exam[] e = new Exam[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                    e[i] = new Exam();
                    //檢查日期
                    if (dt.Rows[i]["EXAM_DATE"].ToString() != "0000000")
                        {
                        DateTime ExamDate = StrTransformDate(dt.Rows[i]["EXAM_DATE"].ToString(), dt.Rows[i]["EXAM_TIME"].ToString());
                        e[i].ExamDate = ExamDate;
                        }
                    else
                        {
                        e[i].ExamDate = DateTime.MinValue;
                        }
                    e[i].ExamName = dt.Rows[i]["ExamName"].ToString();
               

                string WK_ErrMsg2 = "";
                string CHART_NO = "";
                string GM_MAYA_NIS_Prikey = "HZj1zICmbObgFaM4zPlBw2Kjv61Q6K6kemRSVCaACulfD0VeNkecaBizMtA6OuiWjOhXVwwxn2Eixum2i3opkw==";
                string[] VerifyStr2 = new string[6];
                Double timeStamp = DateTime.UtcNow.AddHours(0).Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
                string IP4Address = string.Empty;
                foreach (IPAddress IPA in Dns.GetHostAddresses(Dns.GetHostName()))
                {
                    if (IPA.AddressFamily.ToString() == "InterNetwork")
                    {
                        IP4Address = IPA.ToString();
                        break;
                    }
                }

                //18 ER
                if ((FeeNo.Trim()).Length > 10)
                {
                     CHART_NO = FeeNo.ToString().Substring(0, 10);
                }
                else
                {
                     CHART_NO = GetPatChartNo(FeeNo).ToString();
                }
                VerifyStr2[0] = Convert.ToInt64(timeStamp).ToString();
                VerifyStr2[1] = IP4Address;
                VerifyStr2[2] = "MAYA.NIS";
                VerifyStr2[3] = UserId;
                VerifyStr2[4] = "1";
                VerifyStr2[5] = GetDynamicHashKey(GM_MAYA_NIS_Prikey, VerifyStr2[0]);

                e[i].ExamUrl = WK_EBM.Show_PACS_WEB_VIEWER_V2(VerifyStr2, UserId.Trim() , UserPw.Trim(), CHART_NO, dt.Rows[i]["EXAMNO"].ToString().Trim(), ref WK_ErrMsg2);

                //e[i].ExamUrl = "";
                if (dt.Rows[i]["ExamReport"].ToString().Trim() == "")
                {
                    string ReportText;
                    string WK_ErrMsg;
                    DataTable WK_REPORT = new DataTable();
                    string[] VerifyStr = new string[5];
                    ReportText = "";
                    WK_ErrMsg = "";

                    //VerifyStr[0] = DateTime.Now.ToString();
                    //VerifyStr[1] = "MAYA.NIS";
                    //VerifyStr[2] = "admin";
                    //VerifyStr[3] = GetPW1(VerifyStr[2]);
                    //VerifyStr[4] = GetPW2(VerifyStr[2], VerifyStr[0], "M@Y@NIS");

                    //WK_REPORT = WK_EBM.UniReportTextResult(VerifyStr, dt.Rows[i]["EXAMNO"].ToString().Trim(), ref WK_ErrMsg);

                    VerifyStr2[0] = timeStamp.ToString();
                    VerifyStr2[1] = IP4Address;
                    VerifyStr2[2] = "MAYA.NIS";
                    VerifyStr2[3] = UserId;
                    VerifyStr2[4] = "1";
                    VerifyStr2[5] = GetDynamicHashKey(GM_MAYA_NIS_Prikey, VerifyStr2[0]);

                    WK_REPORT = WK_EBM.UniReportTextResult_V2(VerifyStr2, dt.Rows[i]["EXAMNO"].ToString().Trim(), ref WK_ErrMsg);

                    for (int J = 0; J < WK_REPORT.Rows.Count; J++)
                    {
                        ReportText = WK_REPORT.Rows[J]["REPORT"].ToString();
                    }
                    e[i].ExamReport = ReportText;
                }
                else
                {
                    e[i].ExamReport = dt.Rows[i]["ExamReport"].ToString();
                }
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 取得患者檢查 8HR Finish
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetExam8HR(String FeeNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("Exam8HR");
        dtSql2 = GetSql.SqlLst2("Exam8HR");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetExam(FeeNo, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            Exam[] e = new Exam[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new Exam();
                //檢查日期
                if (dt.Rows[i]["EXAM_DATE"].ToString() != "0000000")
                {
                    DateTime ExamDate = StrTransformDate(dt.Rows[i]["EXAM_DATE"].ToString(), dt.Rows[i]["EXAM_TIME"].ToString());
                    e[i].ExamDate = ExamDate;
                }
                else
                {
                    e[i].ExamDate = DateTime.MinValue;
                }
                e[i].ExamName = dt.Rows[i]["ExamName"].ToString();
                e[i].ExamReport = dt.Rows[i]["ExamReport"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 取得患者檢驗 Finish
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetLab(String FeeNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        if (FeeNo.Length > 10)
        {
            dtSql = GetSql.SqlLst("LabER");
            dtSql2 = GetSql.SqlLst2("LabER");
        }
        else
        {
            dtSql = GetSql.SqlLst("Lab");
            dtSql2 = GetSql.SqlLst2("Lab");
        }
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetLab(FeeNo, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            Lab[] e = new Lab[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new Lab();
                if (dt.Rows[i]["LabDate"].ToString() != "0000000")
                {
                    DateTime LabDate = StrTransformDate(dt.Rows[i]["LabDate"].ToString(), dt.Rows[i]["LabTime"].ToString());
                    e[i].LabDate = LabDate;
                }
                else
                {
                    e[i].LabDate = DateTime.MinValue;
                }
                e[i].LabName = dt.Rows[i]["LabName"].ToString();
                if (dt.Rows[i]["LabValue"].ToString().Trim() == "")
                {
                    e[i].LabValue = "報告未出";
                    e[i].LabValueUnit = "";
                }
                else
                {
                    e[i].LabValue = dt.Rows[i]["LabValue"].ToString();
                    e[i].LabValueUnit = dt.Rows[i]["LabValueUnit"].ToString();
                }
                //e[i].LabValue = dt.Rows[i]["LabValue"].ToString();
                //e[i].LVL = dt.Rows[i]["LVL"].ToString();
                //e[i].LVH = dt.Rows[i]["LVH"].ToString();
                if (dt.Rows[i]["odr_date"].ToString() != "0000000")
                {
                    DateTime OdrDate = StrTransformDate(dt.Rows[i]["odr_date"].ToString(), dt.Rows[i]["odr_time"].ToString());
                    e[i].OrderDate = OdrDate;
                }
                else
                {
                    e[i].OrderDate = DateTime.MinValue;
                }
                e[i].Group = dt.Rows[i]["GRP"].ToString();




                //string sqlCombine_2 = string.Empty;
                //DataTable dtSql_2 = new DataTable();
                //dtSql_2 = GetSql.SqlLst("LabITEM");
                //    if (dtSql_2.Rows.Count > 0)
                //{
                //    for (int J = 0; J < dtSql_2.Rows.Count; i++)
                //    {
                //        sqlCombine_2 += dtSql_2.Rows[i]["fun_sql"];
                //    }
                //}
                //DataTable dt_2 = new DataTable();
                //dt_2 = DAOPatient.GetLab(FeeNo, sqlCombine_2);

                


                e[i].Specimen = dt.Rows[i]["Specimen"].ToString();
                e[i].Status = dt.Rows[i]["Status"].ToString();
        

            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 取得患者檢驗 傳日期查詢日期格式yyy/MM/dd
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetLabbyDate(String FeeNo, String StartDate, String EndDate)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();

        if (FeeNo.Length > 10)
        {
            dtSql = GetSql.SqlLst("LabbyDateER");
            dtSql2 = GetSql.SqlLst2("LabbyDateER");
        }
        else
        {
            dtSql = GetSql.SqlLst("LabbyDate");
            dtSql2 = GetSql.SqlLst2("LabbyDate");
        }
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetLabbyDate(FeeNo, StartDate, EndDate, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            Lab[] e = new Lab[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                        e[i] = new Lab();
                        if (dt.Rows[i]["LabDate"].ToString() != "0000000")
                        {
                            DateTime LabDate = StrTransformDate(dt.Rows[i]["LabDate"].ToString(), dt.Rows[i]["LabTime"].ToString());
                            e[i].LabDate = LabDate;
                        }
                        else
                        {
                            e[i].LabDate = DateTime.MinValue;
                        }
                        e[i].LabName = dt.Rows[i]["LabName"].ToString();
                        if (dt.Rows[i]["LabValue"].ToString().Trim() == "")
                        {
                            e[i].LabValue = "報告未出";
                            e[i].LabValueUnit = "";
                        }
                        else
                        {
                            e[i].LabValue = dt.Rows[i]["LabValue"].ToString();
                            e[i].LabValueUnit = dt.Rows[i]["LabValueUnit"].ToString();
                        }
                        //e[i].LabValue = dt.Rows[i]["LabValue"].ToString();
                        //e[i].LabValueUnit = dt.Rows[i]["LabValueUnit"].ToString(); //現在已經沒用了
                        //e[i].LVL = dt.Rows[i]["LVL"].ToString();
                        //e[i].LVH = dt.Rows[i]["LVH"].ToString();
                        if (dt.Rows[i]["odr_date"].ToString() != "0000000")
                        {
                            DateTime OdrDate = StrTransformDate(dt.Rows[i]["odr_date"].ToString(), dt.Rows[i]["odr_time"].ToString());
                            e[i].OrderDate = OdrDate;
                        }
                        else
                        {
                            e[i].OrderDate = DateTime.MinValue;
                        }
                        e[i].Group = dt.Rows[i]["GRP"].ToString();
                        e[i].Specimen = dt.Rows[i]["Specimen"].ToString();
                        e[i].Status = dt.Rows[i]["Status"].ToString();
                        e[i].LabCode = dt.Rows[i]["LAB_CODE"].ToString();
                        string LabErrorFlag = GetLabErrorFlag(dt.Rows[i]["CHART_NO"].ToString(),dt.Rows[i]["LAB_CODE"].ToString(), dt.Rows[i]["LabDate"].ToString(), dt.Rows[i]["LabDate"].ToString()).ToString();
                        e[i].LabErrorFlag = LabErrorFlag;

            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    private int INT(string startDate)
    {
        throw new NotImplementedException();
    }
    
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    private string GetLabErrorFlag(string CHARTNO, string LABCODE, string STADATE, string ENDDATE) 
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("LabErrorFlag");
        dtSql2 = GetSql.SqlLst2("LabErrorFlag");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetLabErrorFlag(CHARTNO ,LABCODE,STADATE,ENDDATE, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                double WK_RESULT_DESCRIPTION;
                if (double.TryParse(dt.Rows[i]["RESULT_DESCRIPTION"].ToString(), out  WK_RESULT_DESCRIPTION))
                {
                    if (Double.Parse(dt.Rows[i]["RESULT_DESCRIPTION"].ToString()) < Double.Parse(dt.Rows[i]["MINMIN"].ToString()))
                    {
                    return "Y";
                    }
                    if (Double.Parse(dt.Rows[i]["RESULT_DESCRIPTION"].ToString()) > Double.Parse(dt.Rows[i]["MAXMAX"].ToString()))
                    {
                    return "Y";
                    }
                }
                else
                {
                    return "N";
                }
            }
            return "N";
        }
        else
        {
            return "N";
        }

    }


    /// <summary>
    /// 取得患者檢驗 8HR Finish
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetLab8HR(String FeeNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("Lab8HR");
        dtSql2 = GetSql.SqlLst2("Lab8HR");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetLab(FeeNo, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            Lab[] e = new Lab[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new Lab();
                if (dt.Rows[i]["LabDate"].ToString() != "0000000")
                {
                    DateTime LabDate = StrTransformDate(dt.Rows[i]["LabDate"].ToString(), dt.Rows[i]["LabTime"].ToString());
                    e[i].LabDate = LabDate;
                }
                else
                {
                    e[i].LabDate = DateTime.MinValue;
                }
                //e[i].LabName = dt.Rows[i]["item_name"].ToString();
                //e[i].LabValue = dt.Rows[i]["value"].ToString();
                e[i].LabValueUnit = dt.Rows[i]["LabValueUnit"].ToString(); //現在已經沒用了
                //e[i].LVL = dt.Rows[i]["r_min"].ToString();
                //e[i].LVH = dt.Rows[i]["r_max"].ToString();
                //if (dt.Rows[i]["odr_date"].ToString() != "0000000")
                //{
                //    DateTime OdrDate = StrTransformDate(dt.Rows[i]["odr_date"].ToString(), dt.Rows[i]["odr_time"].ToString());
                //    e[i].OrderDate = OdrDate;
                //}
                //else
                //{
                //    e[i].OrderDate = DateTime.MinValue;
                //}
                //e[i].Group = dt.Rows[i]["Grp"].ToString();
                //e[i].Specimen = dt.Rows[i]["Specimen"].ToString();
                //e[i].ItemName = dt.Rows[i]["item_name2"].ToString();
                //e[i].Status = dt.Rows[i][""].ToString();
                //e[i].Memo = dt.Rows[i][""].ToString();
                e[i].LabName = dt.Rows[i]["LabName"].ToString();
                e[i].LabValue = dt.Rows[i]["LabValue"].ToString();
                e[i].LVL = dt.Rows[i]["LVL"].ToString();
                e[i].LVH = dt.Rows[i]["LVH"].ToString();
                //e[i].ItemName = " ";
                //e[i].Status = " ";
                //e[i].Memo = " ";
                //院內檢驗報告網址
                //e[i].lab_page = dt.Rows[i]["lab_page"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 取得患者產檢項目
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetLabBorn(String FeeNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("LabBorn");
        dtSql2 = GetSql.SqlLst2("LabBorn");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetLabBorn(FeeNo, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            LabBorn[] e = new LabBorn[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new LabBorn();
                e[i].Item = dt.Rows[i]["item"].ToString();
                e[i].Result = dt.Rows[i]["value"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 取得患者 Chemotherapy 結果
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetChemotherapy(String FeeNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("Chemotherapy");
        dtSql2 = GetSql.SqlLst2("Chemotherapy");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetChemotherapy(FeeNo, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            ChemotherapyStatus[] e = new ChemotherapyStatus[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new ChemotherapyStatus();
                e[i].BSA = dt.Rows[i]["bsa"].ToString();
                e[i].Scr = dt.Rows[i]["scr"].ToString();
                e[i].Ccr = dt.Rows[i]["ccr"].ToString();
                e[i].WBC = dt.Rows[i]["wbc"].ToString();
                e[i].WBCDate = (DateTime)dt.Rows[i]["WBCDate"];
                e[i].T = dt.Rows[i]["stage_t"].ToString();
                e[i].N = dt.Rows[i]["stage_n"].ToString();
                e[i].M = dt.Rows[i]["stage_m"].ToString();
                e[i].Stage = dt.Rows[i]["stage"].ToString();
                e[i].ANC = "";
                e[i].Weight = dt.Rows[i]["weight"].ToString();
                e[i].Height = dt.Rows[i]["height"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 取得患者施打抗生素列表
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetAntibiotics(String FeeNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("Antibiotics");
        dtSql2 = GetSql.SqlLst2("Antibiotics");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetAntibiotics(FeeNo, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            AntibioticsList[] e = new AntibioticsList[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new AntibioticsList();
                if (dt.Rows[i]["OrderStartDate"].ToString() != "")
                    e[i].OrderStartDate = StrTransformDate(dt.Rows[i]["OrderStartDate"].ToString(), dt.Rows[i]["OrderStartTime"].ToString());
                //e[i].OrderStartDate =(DateTime)dt.Rows[i]["OrderStartDate"];
                else
                    e[i].OrderStartDate = DateTime.MinValue;
                if (dt.Rows[i]["OrderEndDate"].ToString() != "")
                    e[i].OrderEndDate = StrTransformDate(dt.Rows[i]["OrderEndDate"].ToString(), dt.Rows[i]["OrderEndTime"].ToString());
                //e[i].OrderEndDate = (DateTime)dt.Rows[i]["OrderEndDate"];
                else
                    e[i].OrderEndDate = DateTime.MaxValue;
                e[i].DrugName = dt.Rows[i]["DRUGNAME"].ToString().Trim();
                e[i].Dose = float.Parse(dt.Rows[i]["Dose"].ToString());
                e[i].DoseUnit = dt.Rows[i]["DoseUnit"].ToString();
                e[i].QTY = dt.Rows[i]["QTY"].ToString();
                e[i].Route = dt.Rows[i]["Route"].ToString();
                e[i].Feq = dt.Rows[i]["Feq"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 床號轉成批價序號  Finish
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] BedNoTransformFeeNo(String BedNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("BedNoTransformFeeNo");
        dtSql2 = GetSql.SqlLst2("BedNoTransformFeeNo");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.BedNoTransformFeeNo(BedNo, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            PatientInfo[] e = new PatientInfo[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new PatientInfo();
                e[i].ChartNo = dt.Rows[i]["ChartNo"].ToString().Trim();
                e[i].PatientName = dt.Rows[i]["PatientName"].ToString().Trim();
                e[i].FeeNo = dt.Rows[i]["FeeNo"].ToString().Trim();
                if (dt.Rows[i]["PatientGender"].ToString() == "M")
                    e[i].PatientGender = "男";
                else
                    e[i].PatientGender = "女";
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 床號轉成批價序號  CostCode 20240328
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] BedNoTransformFeeNoWithCostCode(String BedNo, String CostCode)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("BedNoTransformFeeNo");
        dtSql2 = GetSql.SqlLst2("BedNoTransformFeeNo");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
            sqlCombine += " and cost_code = '#costcode#'";
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.BedNoTransformFeeNoWithCostCode(BedNo, CostCode, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            PatientInfo[] e = new PatientInfo[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new PatientInfo();
                e[i].ChartNo = dt.Rows[i]["ChartNo"].ToString().Trim();
                e[i].PatientName = dt.Rows[i]["PatientName"].ToString().Trim();
                e[i].FeeNo = dt.Rows[i]["FeeNo"].ToString().Trim();
                if (dt.Rows[i]["PatientGender"].ToString() == "M")
                    e[i].PatientGender = "男";
                else
                    e[i].PatientGender = "女";
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }
    /// <summary>
    /// 藥品資訊 Finish
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetMedicalInfo(String MedicalNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("MedicalInfo");
        dtSql2 = GetSql.SqlLst2("MedicalInfo");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetMedicalInfo(MedicalNo, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            MedicalInfo[] e = new MedicalInfo[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new MedicalInfo();
                e[i].DrugCode = dt.Rows[i]["DrugCode"].ToString();
                e[i].DrugName = dt.Rows[i]["DrugName"].ToString();
                e[i].GenericDrugs = dt.Rows[i]["GenericDrugs"].ToString();
                e[i].DrugEffects = dt.Rows[i]["DrugEffects"].ToString();
                e[i].DrugSideEffects = dt.Rows[i]["DrugSideEffects"].ToString();
                e[i].DrugPicPath = dt.Rows[i]["DrugPicPath"].ToString();
                e[i].DrugHref = dt.Rows[i]["DrugHref"].ToString();
                //e[i].DrugsPath = dt.Rows[i]["DrugsPath"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 新醫囑
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetNewOrderFlag()
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("NewOrderFlag");
        dtSql2 = GetSql.SqlLst2("NewOrderFlag");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetNewOrderFlag(sqlCombine);
        if (dt.Rows.Count > 0)
        {
            PatientList[] e = new PatientList[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new PatientList();
                e[i].FeeNo = dt.Rows[i]["FEENO"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 新醫囑 update
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] UpdNewOrderFlag(String FeeNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("NewOrderFlagUpd");
        dtSql2 = GetSql.SqlLst2("NewOrderFlagUpd");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        int result = DAOPatient.UpdNewOrderFlag(FeeNo, sqlCombine);
        return null;
    }

    /// <summary>
    /// 新醫囑
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetMedOrderRenew()
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("MedOrderRenew");
        dtSql2 = GetSql.SqlLst2("MedOrderRenew");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetNewOrderFlag(sqlCombine);
        if (dt.Rows.Count > 0)
        {
            PatientList[] e = new PatientList[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new PatientList();
                e[i].FeeNo = dt.Rows[i]["FEENO"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }






    /// <summary>
    /// MED ORDER RENEW
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetMedOrderRenew2(string str1,string strFlag)
    {
        byte[] result = null;
        string[] arr_udseq = str1.ToString().Split(',');
        string[] strCheck = new string[arr_udseq.Length];
        string[] strSeq = new string[arr_udseq.Length];
        string[] strORDERDATE = new string[arr_udseq.Length];
        string strUDSEQ = arr_udseq[0];
        string strSql = "", strSql1 = "", strSql2 = "",strSql3 = "";
        string FeeNo = "";
        DataTable dt = new DataTable();
        DataTable dt_tmp2 = new DataTable();
        #region 步驟一 找FEENO及藥品資料
        strSql = "SELECT UD_SEQ,FEE_NO,MED_CODE,UD_TYPE,UD_CIR,UD_DOSE,UD_UNIT,UD_PATH,ORDER_SEQ FROM UD_ORDER WHERE UD_SEQ = '";
        strSql1 = strSql + strUDSEQ + "'";
        DataTable dt_tmp = GetSql.SqlMed(strSql1);
        if (dt_tmp.Rows.Count == 0)
            return result;
        FeeNo = dt_tmp.Rows[0]["FEE_NO"].ToString();
        #endregion

        #region 步驟二 是否有DC 
        for (int i = 0; i <= arr_udseq.Length - 1; i++)
        {
            strSql2 = "SELECT DC_SHEET_NO, ORDER_SEQ_B, TO_CHAR(TO_NUMBER(ORDER_DATE)+19110000||ORDER_TIME) ORDERDATE FROM IPD_ALL_ORDER";
            strSql2 += " WHERE FEE_NO = '" + FeeNo + "' AND SHEET_NO = (SELECT DC_SHEET_NO FROM IPD_ALL_ORDER WHERE FEE_NO = '" + FeeNo + "' AND ORDER_SEQ_B = ";
            strSql2 += "(SELECT ORDER_SEQ FROM UD_ORDER WHERE UD_SEQ = '" + arr_udseq[i] + "'))";
            strSql2 += "AND FEE_CODE = (SELECT MED_CODE FROM UD_ORDER WHERE UD_SEQ = '" + arr_udseq[i] + "') AND DEL_FLAG = 'N'";
            dt_tmp2 = GetSql.SqlMed(strSql2);
            if (dt_tmp2.Rows.Count == 0)
            {
                strSeq[i] = "";
                strCheck[i] = "";
                continue;
            }
            else
            {
                strSeq[i] = dt_tmp2.Rows[0]["ORDER_SEQ_B"].ToString().Trim();
                strCheck[i] = dt_tmp2.Rows[0]["DC_SHEET_NO"].ToString().Trim();
            }

            //當藥被DC多次，則要找到最後正使用中的藥
            while (strCheck[i] != "")
            {
                strSql2 = "SELECT DC_SHEET_NO,ORDER_SEQ_B,TO_CHAR(TO_NUMBER(ORDER_DATE)+19110000||ORDER_TIME) ORDERDATE FROM IPD_ALL_ORDER";
                strSql2 += " WHERE FEE_NO = '" + dt_tmp.Rows[0]["FEE_NO"].ToString() + "' AND SHEET_NO = '" + dt_tmp2.Rows[0]["DC_SHEET_NO"].ToString().Trim() + "'";
                strSql2 += " AND FEE_CODE = '" + dt_tmp.Rows[0]["MED_CODE"].ToString() + "' AND DEL_FLAG = 'N'";
                dt_tmp2 = GetSql.SqlMed(strSql2);
                if (dt_tmp2.Rows.Count == 0)
                {
                    strCheck[i] = "";
                }
                else
                {
                    strCheck[i] = dt_tmp2.Rows[0]["DC_SHEET_NO"].ToString().Trim();
                    strSeq[i] = dt_tmp2.Rows[0]["ORDER_SEQ_B"].ToString();
                }
            }
        }
        #endregion

        #region 步驟三 回傳被DC以及執行中藥品資料
        if (strFlag == "D")
        {
            if (strSeq[0] != "")
            {
                strSql3 = "SELECT * FROM (SELECT MED_CODE,UD_SEQ,UD_DOSE,UD_UNIT,UD_CIR,UD_PATH,UD_TYPE,";
                strSql3 += "TO_CHAR(TO_NUMBER(BEGIN_DATE)+19110000)||BEGIN_TIME ORDERDATE FROM UD_ORDER WHERE";
                strSql3 += " FEE_NO = '" + dt_tmp.Rows[0]["FEE_NO"].ToString() + "'";
                strSql3 += " AND MED_CODE = '" + dt_tmp.Rows[0]["MED_CODE"].ToString() + "'";
                strSql3 += " AND UD_TYPE = '" + dt_tmp.Rows[0]["UD_TYPE"].ToString() + "'";
                strSql3 += " AND ORDER_SEQ = '" + strSeq[0].ToString() + "') A,";
                strSql3 += "(SELECT MED_CODE,UD_SEQ,UD_DOSE,UD_UNIT,UD_CIR,UD_PATH,UD_TYPE FROM UD_ORDER WHERE FEE_NO='" + dt_tmp.Rows[0]["FEE_NO"].ToString() + "' AND UD_SEQ ='" + strUDSEQ + "') B";
            }
        }
        else
        {
            strUDSEQ = "";
            for (int i = 0; i <= arr_udseq.Length - 1; i++)
            {
                strUDSEQ += strSeq[i] + "','";
            }
            strSql3 = "SELECT A.MED_CODE,A.UD_SEQ,A.UD_DOSE,A.UD_UNIT,A.UD_CIR,A.UD_PATH,A.UD_TYPE,A.ORDER_SEQ,";
            strSql3 += "TO_CHAR(TO_NUMBER(A.BEGIN_DATE)+19110000)||A.BEGIN_TIME ORDERDATE FROM UD_ORDER A,IPD_ALL_ORDER B";
            strSql3 += " WHERE A.FEE_NO = B.FEE_NO AND A.ORDER_SEQ = B.ORDER_SEQ_B AND A.FEE_NO='" + FeeNo + "'";
            strSql3 += " AND A.ORDER_SEQ IN ('" + strUDSEQ.ToString() + "')";
        }
        if (strSql3 == "") {
            return result;
        }
        else
        {
            dt = GetSql.SqlMed(strSql3);
            if (dt.Rows.Count == 0)
                return result;
        }
        #endregion

        MedOrderRenew[] e = new MedOrderRenew[dt.Rows.Count];
        if (strFlag == "D")
        {
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new MedOrderRenew();
                e[i].MED_CODE = dt.Rows[i]["MED_CODE"].ToString();
                e[i].UDSEQ = dt.Rows[i]["UD_SEQ"].ToString();
                e[i].UD_DOSE = dt.Rows[i]["UD_DOSE"].ToString();
                e[i].UD_UNIT = dt.Rows[i]["UD_UNIT"].ToString();
                e[i].UD_CIR = dt.Rows[i]["UD_CIR"].ToString();
                e[i].UD_PATH = dt.Rows[i]["UD_PATH"].ToString();
                e[i].UD_TYPE = dt.Rows[i]["UD_TYPE"].ToString();
                e[i].ORDERDATE = dt.Rows[i]["ORDERDATE"].ToString();
                e[i].UDSEQ_O = dt.Rows[i]["UD_SEQ1"].ToString();
                e[i].UD_DOSE_O = dt.Rows[i]["UD_DOSE1"].ToString();
                e[i].UD_UNIT_O = dt.Rows[i]["UD_UNIT1"].ToString();
                e[i].UD_CIR_O = dt.Rows[i]["UD_CIR1"].ToString();
                e[i].UD_PATH_O = dt.Rows[i]["UD_PATH1"].ToString();
                e[i].UD_TYPE_O = dt.Rows[i]["UD_TYPE1"].ToString();
            }
        }
        else
        {
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new MedOrderRenew();
                e[i].MED_CODE = dt.Rows[i]["MED_CODE"].ToString();
                e[i].UDSEQ = dt.Rows[i]["UD_SEQ"].ToString();
                e[i].UD_DOSE = dt.Rows[i]["UD_DOSE"].ToString();
                e[i].UD_UNIT = dt.Rows[i]["UD_UNIT"].ToString();
                e[i].UD_CIR = dt.Rows[i]["UD_CIR"].ToString();
                e[i].UD_PATH = dt.Rows[i]["UD_PATH"].ToString();
                e[i].UD_TYPE = dt.Rows[i]["UD_TYPE"].ToString();
                e[i].ORDERDATE = dt.Rows[i]["ORDERDATE"].ToString();
                e[i].UDSEQ_O = "";
                e[i].UD_DOSE_O = "";
                e[i].UD_UNIT_O = "";
                e[i].UD_CIR_O = "";
                e[i].UD_PATH_O = "";
                e[i].UD_TYPE_O = "";
            }
        }
        result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
        return result;
    }

    /// <summary>
    /// 身份證號碼轉員編 Finish
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetIDToEmp(string IdNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("IDToEmp");
        dtSql2 = GetSql.SqlLst2("IDToEmp");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOUser.GetIDToEmp(IdNo, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            UserInfo[] e = new UserInfo[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new UserInfo();
                e[i].EmployeesNo = dt.Rows[i]["EmployeesNo"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 取得患者手術
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetOpInfo(string FeeNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("OpInfo");
        dtSql2 = GetSql.SqlLst2("OpInfo");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetPatientOp(FeeNo, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            Surgery[] e = new Surgery[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new Surgery();
                e[i].SurgeryDate = StrTransformDate(dt.Rows[i]["SurgeryDate"].ToString(), "");
                e[i].SurgeryNo = dt.Rows[i]["SurgeryNo"].ToString();
                e[i].SurgeryContent = dt.Rows[i]["SurgeryContent"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary> 
    /// 取得病患轉床紀錄
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetBedTransList(string FeeNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("BedTransList");
        dtSql2 = GetSql.SqlLst2("BedTransList");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt;
        dt = DAOPatient.GetBedTransList(FeeNo,sqlCombine);
        if (dt.Rows.Count > 0)
        {
            BedTransList[] e = new BedTransList[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new BedTransList();
                DateTime TransDate = StrTransformDate(dt.Rows[i]["trans_date"].ToString(), dt.Rows[i]["trans_time"].ToString());
                e[i].TransDate = TransDate;
                e[i].BedNo = dt.Rows[i]["BedNo"].ToString();
                e[i].CostCode = dt.Rows[i]["CostCode"].ToString();
                e[i].CostDesc = dt.Rows[i]["CostDesc"].ToString();
            }

            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 取得病患轉床紀錄2
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetTransBedDetail(string CostCode, string BeginDate, string EndDate)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("BedTransList2");
        dtSql2 = GetSql.SqlLst2("BedTransList2");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt;
        dt = DAOPatient.GetTransBedDetail(CostCode, BeginDate, EndDate, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            TransListDetail[] e = new TransListDetail[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new TransListDetail();
                DateTime TransDate = StrTransformDate(dt.Rows[i]["trans_date"].ToString(), dt.Rows[i]["trans_time"].ToString());
                e[i].TransDate = TransDate;
                e[i].FeeNo = dt.Rows[i]["fee_no"].ToString();
                e[i].BedNo = dt.Rows[i]["ori_bed_no"].ToString();
                e[i].DeptName = dt.Rows[i]["full_desc"].ToString();
                e[i].DocName = dt.Rows[i]["doc_name"].ToString();
                e[i].ICD9_code1 = dt.Rows[i]["mdiag_desc"].ToString();
            }

            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 取得病患轉床紀錄3
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetTransBedDetail2(string BeginDate, string EndDate)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("BedTransList3");
        dtSql2 = GetSql.SqlLst2("BedTransList3");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt;
        dt = DAOPatient.GetTransBedDetail2(BeginDate, EndDate, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            TransListDetail[] e = new TransListDetail[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new TransListDetail();
                DateTime TransDate = StrTransformDate(dt.Rows[i]["trans_date"].ToString(), dt.Rows[i]["trans_time"].ToString());
                e[i].TransDate = TransDate;
                e[i].FeeNo = dt.Rows[i]["fee_no"].ToString();
                e[i].BedNo = dt.Rows[i]["ori_bed_no"].ToString();
                e[i].DeptName = dt.Rows[i]["full_desc"].ToString();
                e[i].DocName = dt.Rows[i]["doc_name"].ToString();
                e[i].ICD9_code1 = dt.Rows[i]["mdiag_desc"].ToString();
            }

            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 新版轉床歷程
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetBed_Trans(string CostCode, string BeginDate, string EndDate)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("Bed_Trans");
        dtSql2 = GetSql.SqlLst2("Bed_Trans");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt;
        dt = DAOPatient.GetBed_Trans(CostCode, BeginDate, EndDate, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            BED_TRANS[] e = new BED_TRANS[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new BED_TRANS();
                DateTime BgnDate = StrTransformDate(dt.Rows[i]["BNG_DATE"].ToString(), dt.Rows[i]["BNG_TIME"].ToString().Substring(0,4));
                DateTime EdDate = StrTransformDate(dt.Rows[i]["END_DATE"].ToString(), dt.Rows[i]["END_TIME"].ToString().Substring(0,4));
                DateTime Birthday = StrTransformDate(dt.Rows[i]["BIRTH_DATE"].ToString(),"");
                e[i].FEENO = dt.Rows[i]["FEE_NO"].ToString();
                e[i].BEDNO = dt.Rows[i]["BED_NO"].ToString();
                e[i].CostCode = dt.Rows[i]["COST_CODE"].ToString();
                e[i].CostName = dt.Rows[i]["COST_DESC"].ToString();
                e[i].BeginDate = BgnDate;
                e[i].EndDate = EdDate;
                e[i].ChartNo = dt.Rows[i]["CHR_NO"].ToString();
                e[i].PatName = dt.Rows[i]["PAT_NAME"].ToString();
                e[i].Birthday = Birthday;
                e[i].AgeY = dt.Rows[i]["AGE_YEAR"].ToString();
                e[i].AgeM = dt.Rows[i]["AGE_MONTH"].ToString();
                if (dt.Rows[i]["AGE_MONTH"].ToString() == "0")
                {
                    e[i].Gender = "男";
                }else{
                    e[i].Gender = "女";
                }
                e[i].ICD9Code = dt.Rows[i]["MDIAG_CODE"].ToString();
                e[i].ICD9Desc = dt.Rows[i]["MDIAG_DESC"].ToString();
                e[i].DepCode = dt.Rows[i]["DEPT_CODE"].ToString();
                e[i].DepName = dt.Rows[i]["DEPT_NAME"].ToString();
                e[i].DocCode = dt.Rows[i]["DOC_CODE"].ToString();
                e[i].DocName = dt.Rows[i]["DOC_NAME"].ToString();

            }

            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 新版轉床歷程
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetBed_TransList(string CostCode, string BeginDate, string EndDate)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("Bed_TransList");
        dtSql2 = GetSql.SqlLst2("Bed_TransList");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt;
        dt = DAOPatient.GetBed_Trans(CostCode, BeginDate, EndDate, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            BED_TRANS[] e = new BED_TRANS[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new BED_TRANS();
                DateTime BgnDate = StrTransformDate(dt.Rows[i]["BNG_DATE"].ToString(), dt.Rows[i]["BNG_TIME"].ToString().Substring(0, 4));
                DateTime EdDate = StrTransformDate(dt.Rows[i]["END_DATE"].ToString(), dt.Rows[i]["END_TIME"].ToString().Substring(0, 4));
                DateTime Birthday = StrTransformDate(dt.Rows[i]["BIRTH_DATE"].ToString(), "");
                e[i].FEENO = dt.Rows[i]["FEE_NO"].ToString();
                e[i].BEDNO = dt.Rows[i]["BED_NO"].ToString();
                e[i].CostCode = dt.Rows[i]["COST_CODE"].ToString();
                e[i].CostName = dt.Rows[i]["COST_DESC"].ToString();
                e[i].BeginDate = BgnDate;
                e[i].EndDate = EdDate;
                e[i].ChartNo = dt.Rows[i]["CHR_NO"].ToString();
                e[i].PatName = dt.Rows[i]["PAT_NAME"].ToString();
                e[i].Birthday = Birthday;
                e[i].AgeY = dt.Rows[i]["AGE_YEAR"].ToString();
                e[i].AgeM = dt.Rows[i]["AGE_MONTH"].ToString();
                if (dt.Rows[i]["AGE_MONTH"].ToString() == "0")
                {
                    e[i].Gender = "男";
                }
                else
                {
                    e[i].Gender = "女";
                }
                e[i].ICD9Code = dt.Rows[i]["MDIAG_CODE"].ToString();
                e[i].ICD9Desc = dt.Rows[i]["MDIAG_DESC"].ToString();
                e[i].DepCode = dt.Rows[i]["DEPT_CODE"].ToString();
                e[i].DepName = dt.Rows[i]["DEPT_NAME"].ToString();
                e[i].DocCode = dt.Rows[i]["DOC_CODE"].ToString();
                e[i].DocName = dt.Rows[i]["DOC_NAME"].ToString();

            }

            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    ///  取得有效執導者(建議傳入單位 才不會一次太多人名 呼叫時回傳可代簽人員)
    /// </summary>
    /// <param name="CostCenterCode">成本中心代碼</param>
    /// <returns></returns>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetSigner(string CostCenterCode)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("Signer");
        dtSql2 = GetSql.SqlLst2("Signer");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOUser.GetSigner(CostCenterCode, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            UserInfo[] e = new UserInfo[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new UserInfo();
                e[i].EmployeesNo = dt.Rows[i]["EmployeesNo"].ToString();
                e[i].EmployeesName = dt.Rows[i]["EmployeesName"].ToString();
                e[i].CostCenterCode = CostCenterCode;
                e[i].CostCenterName = dt.Rows[i]["CostCenterName"].ToString();

            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 取得患者VitalSign Finish
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetVitalSignbyDate(String FeeNo, String StartDate, String EndDate,string EmpId)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("VitalSignbyDate");
        dtSql2 = GetSql.SqlLst2("VitalSignbyDate");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetVitalSign(FeeNo, StartDate, EndDate, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            VitalSignbyDate[] e = new VitalSignbyDate[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new VitalSignbyDate();
                //檢查日期
               
                e[i].INSID = dt.Rows[i]["INSID"].ToString();
                e[i].SYSTOLIC = dt.Rows[i]["SYSTOLIC"].ToString();
                e[i].DIATOLIC = dt.Rows[i]["DIATOLIC"].ToString();
                if (dt.Rows[i]["MAP"].ToString() =="0")
                { e[i].MAP = ""; }
                else
                { e[i].MAP = dt.Rows[i]["MAP"].ToString(); }
                
                e[i].HR = dt.Rows[i]["HR"].ToString();
                e[i].TEMP = dt.Rows[i]["TEMP"].ToString();
                e[i].SPO2 = dt.Rows[i]["SPO2"].ToString();
                e[i].SPO2HR = dt.Rows[i]["SPO2HR"].ToString();
                e[i].HEIGHT = dt.Rows[i]["HEIGHT"].ToString();
                e[i].WEIGHT = dt.Rows[i]["WEIGHT"].ToString();
                e[i].RESP = dt.Rows[i]["RESP"].ToString();
                e[i].PAIN = dt.Rows[i]["PAIN"].ToString();
                e[i].PATIENICIANID = dt.Rows[i]["PATIENTID"].ToString();
                e[i].CLINICIANID = dt.Rows[i]["CLINICIANID"].ToString();
                //e[i].MDATE = dt.Rows[i]["MDATE"].ToString() + dt.Rows[i]["TIME"].ToString();//日期時間(yyyy/MM/dd HH:mm:ss)
                DateTime Indate = StrTransformDate(dt.Rows[i]["MDATE"].ToString(), dt.Rows[i]["TIME"].ToString());
                e[i].MDATE =Indate.ToString () ;
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }





    /// <summary>
    /// 取得文字醫囑個項目是否有未簽 
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetTextOrderItem(String FeeNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();

        string DRIVE_ID = "OPD";
        int CLINIC_DATE_YEAR = 0;
        if (FeeNo.Length > 10)
        {
            dtSql = GetSql.SqlLst("TextOrderItem");
            dtSql2 = GetSql.SqlLst2("TextOrderItem");
            CLINIC_DATE_YEAR = int.Parse(FeeNo.ToString().Substring(10, 3));
            switch (CLINIC_DATE_YEAR % 5)
            {
                case 1:
                    DRIVE_ID = "OHIS" + FeeNo.ToString().Substring(13, 2);
                    break;
                case 2:
                    DRIVE_ID = "OHISB" + FeeNo.ToString().Substring(13, 2);
                    break;
                case 3:
                    DRIVE_ID = "OHISC" + FeeNo.ToString().Substring(13, 2);
                    break;
                case 4:
                    DRIVE_ID = "OHISD" + FeeNo.ToString().Substring(13, 2);
                    break;
                case 0:
                    DRIVE_ID = "OHISE" + FeeNo.ToString().Substring(13, 2);
                    break;
            }
        }
        else
        {
            dtSql = GetSql.SqlLst("TextOrderItem");
            dtSql2 = GetSql.SqlLst2("TextOrderItem");
        }

        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }
        DataTable dt = new DataTable();
        dt = DAOPatient.GetTextOrderItem(FeeNo, DRIVE_ID,sqlCombine);
        if (dt.Rows.Count > 0)
        {
            OrderItem[] e = new OrderItem[1];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[0] = new OrderItem();
                e[0].R = dt.Rows[i]["R_FLAG"].ToString();
                e[0].S = dt.Rows[i]["S_FLAG"].ToString();
                e[0].S3 = dt.Rows[i]["S3_FLAG"].ToString();
                e[0].D = dt.Rows[i]["D_FLAG"].ToString();
                e[0].DC = dt.Rows[i]["DC_FLAG"].ToString();
                if(dt.Rows[i]["R_FLAG"].ToString()=="Y")
                {e[0].ALL = "Y"; }
                if (dt.Rows[i]["S_FLAG"].ToString() == "Y")
                { e[0].ALL = "Y"; }
                if (dt.Rows[i]["S3_FLAG"].ToString() == "Y")
                { e[0].ALL = "Y"; }
                if (dt.Rows[i]["D_FLAG"].ToString() == "Y")
                { e[0].ALL = "Y"; }
                //if (dt.Rows[i]["DC_FLAG"].ToString() == "Y")
                //{ e[0].ALL = "Y"; }
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 取得藥品醫囑個項目是否有未簽 
    /// </summary>
    /// 
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetDrugOrderItem(String FeeNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        string DRIVE_ID = "OPD";
        int CLINIC_DATE_YEAR = 0;
        if (FeeNo.Length > 10)
        {
            dtSql = GetSql.SqlLst("DrugOrderItemER");
            dtSql2 = GetSql.SqlLst2("DrugOrderItemER");
            CLINIC_DATE_YEAR = int.Parse(FeeNo.ToString().Substring(10, 3));
            switch (CLINIC_DATE_YEAR % 5)
            {
                case 1:
                    DRIVE_ID = "OHIS" + FeeNo.ToString().Substring(13, 2);
                    break;
                case 2:
                    DRIVE_ID = "OHISB" + FeeNo.ToString().Substring(13, 2);
                    break;
                case 3:
                    DRIVE_ID = "OHISC" + FeeNo.ToString().Substring(13, 2);
                    break;
                case 4:
                    DRIVE_ID = "OHISD" + FeeNo.ToString().Substring(13, 2);
                    break;
                case 0:
                    DRIVE_ID = "OHISE" + FeeNo.ToString().Substring(13, 2);
                    break;
            }
        }
        else
        {
            dtSql = GetSql.SqlLst("DrugOrderItem");
            dtSql2 = GetSql.SqlLst2("DrugOrderItem");
        }
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }
        DataTable dt = new DataTable();
        dt = DAOPatient.GetDrugOrderItem(FeeNo, DRIVE_ID,  sqlCombine);
        if (dt.Rows.Count > 0)
        {
            OrderItem[] e = new OrderItem[1];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[0] = new OrderItem();
                e[0].R = dt.Rows[i]["R_FLAG"].ToString();
                e[0].S = dt.Rows[i]["S_FLAG"].ToString();
                e[0].DC = dt.Rows[i]["DC_FLAG"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    // <summary>
    /// 取得交班單預計項目數值
    /// </summary>
    /// 
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetExpectedItem(String FeeNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("ExpectedItem");
        dtSql2 = GetSql.SqlLst2("ExpectedItem");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }
        DataTable dt = new DataTable();
        dt = DAOPatient.GetExpectedItem(FeeNo, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            ExpectedItem[] e = new ExpectedItem[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new ExpectedItem();
                DateTime UseDate = StrTransformDate(dt.Rows[i]["ORDER_DATE"].ToString(), dt.Rows[i]["ORDER_TIME"].ToString());
                e[i].UseDate = UseDate;
                e[i].PkNo = dt.Rows[i]["PK_NO"].ToString();
                e[i].ItemType = dt.Rows[i]["EXAM_TYPE"].ToString();
                e[i].ItemName = dt.Rows[i]["EXAM_NAME"].ToString();
                e[i].ComplyDate = StrTransformDate(dt.Rows[i]["EXAM_DATE"].ToString(),"").ToString(); //dt.Rows[i]["EXAM_DATE"].ToString();
                e[i].Type = dt.Rows[i]["ITEM_TYPE"].ToString();
                e[i].LabNo = dt.Rows[i]["LAB_NO"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }


    ///// <summary>
    ///// 取得TPR抗生素
    ///// </summary>
    //[WebMethod]
    //[ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    //public byte[] GetTPRAantibiotic(String FeeNo)
    //{
    //    TPRAantibiotic[] e = new TPRAantibiotic[3];
    //    e[0] = new TPRAantibiotic();

    //    DateTime UseDate = Convert.ToDateTime(DateTime.Now.ToString("yyyy/MM/dd HH:mm"));
    //    e[0].UseDate = UseDate.AddDays(-3);
    //    e[0].content = "內容1";

    //    e[1] = new TPRAantibiotic();
    //    e[1].UseDate = UseDate.AddDays(-1);
    //    e[1].content = "內容2";

    //    e[2] = new TPRAantibiotic();
    //    e[2].UseDate = UseDate.AddDays(-5);
    //    e[2].content = "內容3";

    //    byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
    //    return result;
    //}


    // <summary>
    /// 取得TPR抗生素
    /// </summary>
    /// 
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetTPRAantibiotic(String FeeNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("TPRAantibiotic");
        dtSql2 = GetSql.SqlLst2("TPRAantibiotic");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }
        DataTable dt = new DataTable();
        dt = DAOPatient.GetTPRAantibiotic(FeeNo, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            DateTime NewDate = DateTime .Now ;
            int WK_COUNT = dt.Rows.Count;
            int WK_DAYS = 0;
            int WK_ADDDAYS = 0;
            int WK_CLASS_COUNT = 0;
            int j = 0;
            for (int i = 0; i < WK_COUNT; i++)
            {
                if (dt.Rows[i]["ER_FLAG"].ToString() == "E")
                {
                    DateTime UseDate_COUNT = StrTransformDate(dt.Rows[i]["UseDate"].ToString(), "");
                    TimeSpan tsDay_COUNT = NewDate - UseDate_COUNT;
                    WK_CLASS_COUNT = WK_CLASS_COUNT + (int)tsDay_COUNT.TotalDays+1;
                }
                else
                {
                    WK_CLASS_COUNT = WK_CLASS_COUNT + 1;
                }
            }
            //TPRAantibiotic[] e = new TPRAantibiotic[dt.Rows.Count];
            TPRAantibiotic[] e = new TPRAantibiotic[WK_CLASS_COUNT];
            for (int i = 0; i < WK_COUNT; i++)
            {
                DateTime UseDate = StrTransformDate(dt.Rows[j]["UseDate"].ToString(), ""); 
                if (dt.Rows[j]["ER_FLAG"].ToString()=="E")
                {
                    TimeSpan tsDay = NewDate - UseDate;
                    WK_DAYS = (int)tsDay.TotalDays + i + 1;
                    WK_ADDDAYS = 0;
                    for (int X = i; X < WK_DAYS; X++)
                    {
                        e[X] = new TPRAantibiotic();
                        e[X].UseDate = UseDate.AddDays (WK_ADDDAYS);
                        e[X].content = dt.Rows[j]["Content"].ToString();
                        WK_COUNT = WK_COUNT+1 ;
                        WK_ADDDAYS = WK_ADDDAYS + 1;
                    }
                    i = i + WK_DAYS-1;
                    WK_COUNT = WK_COUNT - 1;
                }
                else
                {
                e[i] = new TPRAantibiotic();
                e[i].UseDate = UseDate;
                e[i].content = dt.Rows[j]["Content"].ToString();
                }
                j = j + 1;
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            TPRAantibiotic[] e = new TPRAantibiotic[0];
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
    }

    ///// <summary>
    ///// 取得有效執導者(不用傳入值 呼叫時回傳可代簽人員)
    ///// </summary>
    //[WebMethod]
    //[ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    //public byte[] GetSigner()
    //{
    //    string sqlCombine = string.Empty;
    //    string sqlCombine2 = string.Empty;
    //    DataTable dtSql = new DataTable();
    //    DataTable dtSql2 = new DataTable();
    //    dtSql = GetSql.SqlLst("Signer");
    //    dtSql2 = GetSql.SqlLst2("Signer");
    //    if (dtSql.Rows.Count > 0)
    //    {
    //        for (int i = 0; i < dtSql.Rows.Count; i++)
    //        {
    //            sqlCombine += dtSql.Rows[i]["fun_sql"];
    //        }
    //    }
    //    if (dtSql2.Rows.Count > 0)
    //    {
    //        for (int i = 0; i < dtSql2.Rows.Count; i++)
    //        {
    //            sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
    //        }
    //    }

    //    DataTable dt;
    //    dt = DAOUser.GetSigner(sqlCombine);
    //    if (dt.Rows.Count > 0)
    //    {
    //        Signer[] e = new Signer[dt.Rows.Count];
    //        for (int i = 0; i < dt.Rows.Count; i++)
    //        {
    //            e[i] = new Signer();
    //            e[i].EmployeesNo = dt.Rows[i]["EmployeesNo"].ToString();
    //            e[i].EmployeesName = dt.Rows[i]["EmployeesName"].ToString();
    //            e[i].CostCenterCode = dt.Rows[i]["CostCenterCode"].ToString();
    //            e[i].CostCenterName = dt.Rows[i]["CostCenterName"].ToString();
    //        }
    //        byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
    //        return result;
    //    }
    //    else
    //    {
    //        return null;
    //    }
    //}


    /// <summary>
    /// 呼吸治療LOGIN用
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] Rcs_UserLogin(string EmployessNo, string EmployessPwd)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("Rcs_UserLogin");
        dtSql2 = GetSql.SqlLst2("Rcs_UserLogin");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOUser.UserLogin(EmployessNo, EmployessPwd, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            UserInfo[] e = new UserInfo[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new UserInfo();
                e[i].EmployeesNo = EmployessNo;
                e[i].EmployeesName = dt.Rows[i]["EmployeesName"].ToString();
                e[i].CostCenterCode = dt.Rows[i]["CostCenterCode"].ToString();
                //e[i].Category = dt.Rows[i]["Category"].ToString();
                e[i].JobGrade = dt.Rows[i]["JobGrade"].ToString();
                e[i].UserID = dt.Rows[i]["EmployeesIdNo"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e[0]));
            return result;
        }
        else
        {
            return null;
        }
    }


    /// <summary>
    /// 呼吸治療GetRCSConsultList用
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetRCSConsultList()
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("Rcs_ConsultList");
        dtSql2 = GetSql.SqlLst2("Rcs_ConsultList");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = Rcs.GetRCSConsultList(sqlCombine);
        if (dt.Rows.Count > 0)
        {
            RCSConsultList[] e = new RCSConsultList[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new RCSConsultList();
                e[i].ADMIT_NO = dt.Rows[i]["ADMIT_NO"].ToString();
                e[i].BED_NO = dt.Rows[i]["BED_NO"].ToString();
                e[i].CHART_NO = dt.Rows[i]["CHART_NO"].ToString();
                e[i].CUT_NO = dt.Rows[i]["CUT_NO"].ToString();
                e[i].DIV_NO = dt.Rows[i]["DIV_NO"].ToString();
                e[i].DIV_SHORT_NAME = dt.Rows[i]["DIV_SHORT_NAME"].ToString();
                e[i].DOCTOR_NAME = dt.Rows[i]["DOCTOR_NAME"].ToString();
                e[i].PT_NAME = dt.Rows[i]["PT_NAME"].ToString();
                e[i].START_DATE = dt.Rows[i]["START_DATE"].ToString();
                e[i].START_TIME = dt.Rows[i]["START_TIME"].ToString();
                e[i].VS_NO = dt.Rows[i]["VS_NO"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }


    /// <summary>
    /// 呼吸治療GetRCSERAndOPDListbyChartNo
    /// </summary>
    /// <returns></returns>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetRCSERAndOPDListbyChartNo(string ChartNo)
    {

        string sqlCombine = string.Empty;
        DataTable dtSql = new DataTable();
        dtSql = GetSql.SqlLst("Rcs_ERAndOpdPTListByChartNo");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        int cYear = System.DateTime.Now.Year - 1911;
        int cMonth = System.DateTime.Now.Month;
        int bYear = System.DateTime.Now.AddMonths(-1).Year - 1911;
        int bMonth = System.DateTime.Now.AddMonths(-1).Month;
        string DRIVE_ID = Rcs.GetDriveID("OHIS", cYear, cMonth);
        string DRIVE_ID_1 = Rcs.GetDriveID("OHIS", bYear, bMonth);
        DataTable dt = new DataTable();
        dt = Rcs.ERAndOpdPTListByChartNo(sqlCombine, DRIVE_ID, DRIVE_ID_1, ChartNo);
        if (dt.Rows.Count > 0)
        {
            RCSERAndOpdPTList[] e = new RCSERAndOpdPTList[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new RCSERAndOpdPTList();
                e[i].CHART_NO = dt.Rows[i]["CHART_NO"].ToString();
                e[i].PT_NAME = dt.Rows[i]["PT_NAME"].ToString().Trim();
                switch (dt.Rows[i]["SEX"].ToString().Trim())
                {
                    case "M":
                        e[i].SEX = "男";
                        break;
                    case "F":
                        e[i].SEX = "女";
                        break;
                    default:
                        e[i].SEX = "";
                        break;
                }
                e[i].BIRTH_DATE = Rcs.GetADDate(dt.Rows[i]["BIRTH_DATE"].ToString());

                e[i].START_DATE = Rcs.GetADDate(dt.Rows[i]["START_DATE"].ToString());
                e[i].START_TIME = dt.Rows[i]["START_TIME"].ToString();
                e[i].END_DATE = Rcs.GetADDate(dt.Rows[i]["END_DATE"].ToString());
                e[i].END_TIME = dt.Rows[i]["END_TIME"].ToString();
                e[i].BED_NO = dt.Rows[i]["BED_NO"].ToString().Trim();
                e[i].CLINIC_FLAG = dt.Rows[i]["CLINIC_FLAG"].ToString().Trim();
                e[i].PKEY = dt.Rows[i]["PKEY"].ToString().Trim();
                e[i].COURSE = dt.Rows[i]["COURSE"].ToString().Trim();
                e[i].ID_NO = dt.Rows[i]["ID_NO"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 呼吸治療GetRCSPtDrugList用
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetRCSPtDrugList(string FeeNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("Rcs_PtDrugList");
        dtSql2 = GetSql.SqlLst2("Rcs_PtDrugList");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = Rcs.GetRCSPtDrugList(FeeNo, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            RCSPtDrugList[] e = new RCSPtDrugList[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new RCSPtDrugList();
                e[i].FEE_NO = dt.Rows[i]["FEE_NO"].ToString();
                e[i].CHR_NO = dt.Rows[i]["CHART_NO"].ToString();
                e[i].COST_CODE = dt.Rows[i]["COST_CODE"].ToString();
                e[i].BED_NO = dt.Rows[i]["BED_NO"].ToString();
                e[i].DRUG_CODE = dt.Rows[i]["DRUG_CODE"].ToString();
                e[i].DRUG_NAME = dt.Rows[i]["DRUG_NAME"].ToString();
                e[i].START_DATE = dt.Rows[i]["START_DATE"].ToString();
                e[i].START_TIME = dt.Rows[i]["START_TIME"].ToString();
                e[i].END_DATE = dt.Rows[i]["END_DATE"].ToString();
                e[i].END_TIME = dt.Rows[i]["END_TIME"].ToString();
                e[i].DOSE_QTY = dt.Rows[i]["DOSE_QTY"].ToString();
                e[i].DOSE_UNIT = dt.Rows[i]["DOSE_UNIT"].ToString();
                e[i].FREQUENCY_CODE = dt.Rows[i]["FREQUENCY_CODE"].ToString();
                e[i].METHOD_CODE = dt.Rows[i]["METHOD_CODE"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }




    #region 急診
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetERDept()
    {
        ERDeptList[] e = new ERDeptList[6];
        e[0] = new ERDeptList();
        e[0].DeptNo = "6000";
        e[0].DeptName = "急診醫學科";
        e[1] = new ERDeptList();
        e[1].DeptNo = "6001";
        e[1].DeptName = "急診內科";
        e[2] = new ERDeptList();
        e[2].DeptNo = "6002";
        e[2].DeptName = "急診外科";
        e[3] = new ERDeptList();
        e[3].DeptNo = "6003";
        e[3].DeptName = "急診兒科";
        e[4] = new ERDeptList();
        e[4].DeptNo = "6004";
        e[4].DeptName = "急診婦產科";
        e[5] = new ERDeptList();
        e[5].DeptNo = "6005";
        e[5].DeptName = "急診兒專科";
        byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
        return result;
    }

    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetTriageInfo(string FeeNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        string sqlCombine3 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        DataTable dtSql3 = new DataTable();
        dtSql = GetSql.SqlLst("TriageInfo");
        dtSql2 = GetSql.SqlLst2("TriageInfo");
        dtSql3 = GetSql.SqlLst("TraumaInfo");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }
        if (dtSql3.Rows.Count > 0)
            for (int i = 0; i < dtSql3.Rows.Count; i++)
            {
                sqlCombine3 += dtSql3.Rows[i]["fun_sql"];
            }

        string DRIVE_ID = "OPD";
        int CLINIC_DATE_YEAR = 0;
        if (FeeNo.Length > 10)
        {
            CLINIC_DATE_YEAR = int.Parse(FeeNo.ToString().Substring(10, 3));
            switch (CLINIC_DATE_YEAR % 5)
            {
                case 1:
                    DRIVE_ID = "OHIS" + FeeNo.ToString().Substring(13, 2);
                    break;
                case 2:
                    DRIVE_ID = "OHISB" + FeeNo.ToString().Substring(13, 2);
                    break;
                case 3:
                    DRIVE_ID = "OHISC" + FeeNo.ToString().Substring(13, 2);
                    break;
                case 4:
                    DRIVE_ID = "OHISD" + FeeNo.ToString().Substring(13, 2);
                    break;
                case 0:
                    DRIVE_ID = "OHISE" + FeeNo.ToString().Substring(13, 2);
                    break;
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetTriageInfo(FeeNo, DRIVE_ID, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            TriageInfo[] e = new TriageInfo[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new TriageInfo();
                e[i].FeeNo = dt.Rows[i]["FEENO"].ToString(); //"I0302234";
                switch (dt.Rows[i]["ER_WOUND_LEVEL"].ToString().Trim())
                {
                    case "1": e[i].Level = "第一級"; break;
                    case "2": e[i].Level = "第二級"; break;
                    case "3": e[i].Level = "第三級"; break;
                    case "4": e[i].Level = "第四級"; break;
                    case "5": e[i].Level = "第五級"; break;
                }
                //**e[0].Level = dt.Rows[i]["ER_WOUND_LEVEL"].ToString();//"一級";
                e[i].ChartNo = dt.Rows[i]["CHART_NO"].ToString();//"04845859";
                e[i].DeptNo = dt.Rows[i]["DIV_NO"].ToString();//"科別代碼";
                switch (dt.Rows[i]["DIV_NO"].ToString().Trim())
                {
                    case "6000": e[i].DeptName = "急診醫學科"; break;
                    case "6001": e[i].DeptName = "急診內科"; break;
                    case "6002": e[i].DeptName = "急診外科"; break;
                    case "6003": e[i].DeptName = "急診兒科"; break;
                    case "6004": e[i].DeptName = "急診婦科"; break;
                    case "6005": e[i].DeptName = "急診兒專科"; break;
                }
                //**e[0].DeptName = "科別名稱";
                e[i].PT_NAME = dt.Rows[i]["PT_NAME"].ToString();//"王大明";
                e[i].ID_NO = dt.Rows[i]["ID_NO"].ToString();//"A123456789";
                switch (dt.Rows[i]["SEX"].ToString().Trim())
                {
                    case "M": e[i].SEX = "男"; break;
                    case "F": e[i].SEX = "女"; break;
                }
                //**e[0].SEX = dt.Rows[i]["SEX"].ToString(); //"男";
                e[i].LIGHT_REACTION_RIGHT = dt.Rows[i]["LIGHT_REACTION_RIGHT"].ToString();//"R1";
                e[i].LIGHT_REACTION_LEFT = dt.Rows[i]["LIGHT_REACTION_LEFT"].ToString();//"L2";
                e[i].BIRTH_DATE = string.Format("{0}/{1}/{2}", dt.Rows[i]["BIRTH_DATE"].ToString().Substring(1, 3), dt.Rows[i]["BIRTH_DATE"].ToString().Substring(4, 2), dt.Rows[i]["BIRTH_DATE"].ToString().Substring(6, 2));//"075/11/11";
                e[i].TEL_NO = dt.Rows[i]["TEL_NO"].ToString();//"0912345678";
                e[i].ADMIT_DATE = StrTransformDate(dt.Rows[i]["ADMIT_DATE"].ToString(), dt.Rows[i]["ADMIT_TIME"].ToString()).ToString("yyyy/MM/dd");//DateTime.Now.ToString("yyyy/MM/dd");
                e[i].ADMIT_TIME = StrTransformDate(dt.Rows[i]["ADMIT_DATE"].ToString(), dt.Rows[i]["ADMIT_TIME"].ToString()).ToString("HH:mm");//DateTime.Now.ToString("HH:mm");
                e[i].OHCA_FLAG = dt.Rows[i]["OHCA_FLAG"].ToString();//"Y";
                e[i].DOMESTIC_VIOLENCE_FLAG = dt.Rows[i]["DOMESTIC_VIOLENCE_FLAG"].ToString();//"Y ";
                e[i].SEXUAL_ABUSE_FLAG = dt.Rows[i]["SEXUAL_ABUSE_FLAG"].ToString();//"Y";
                e[i].SUICIDE_FLAG = dt.Rows[i]["SUICIDE_FLAG"].ToString();//"Y";
                e[i].DANGER_FLAG = dt.Rows[i]["DANGER_FLAG"].ToString();//"Y";
                e[i].TRAUMA_FLAG = dt.Rows[i]["TRAUMA_FLAG"].ToString();//"Y";
                switch (dt.Rows[i]["IN_HOSP_TYPE"].ToString().Trim())
                {
                    case "0": e[i].IN_HOSP_TYPE = "自行走入" + dt.Rows[i]["TRANSFER_IN_HOSP"].ToString().Trim(); break;
                    case "1": e[i].IN_HOSP_TYPE = "推床" + dt.Rows[i]["TRANSFER_IN_HOSP"].ToString().Trim(); break;
                    case "2": e[i].IN_HOSP_TYPE = "輪椅" + dt.Rows[i]["TRANSFER_IN_HOSP"].ToString().Trim(); break;
                    case "3": e[i].IN_HOSP_TYPE = "抱入" + dt.Rows[i]["TRANSFER_IN_HOSP"].ToString().Trim(); break;
                    case "4": e[i].IN_HOSP_TYPE = "119入(分隊:" + dt.Rows[i]["TRANSFER_IN_HOSP"].ToString().Trim()+")"; break;
                    case "5": e[i].IN_HOSP_TYPE = "他院救護車" + dt.Rows[i]["TRANSFER_IN_HOSP"].ToString().Trim(); break;
                    case "6": e[i].IN_HOSP_TYPE = "安養院" + dt.Rows[i]["TRANSFER_IN_HOSP"].ToString().Trim(); break;
                    case "7": e[i].IN_HOSP_TYPE = "轉入" + dt.Rows[i]["TRANSFER_IN_HOSP"].ToString().Trim(); break;
                }
                //**e[0].IN_HOSP_TYPE = dt.Rows[i]["IN_HOSP_TYPE"].ToString();//"步行";
                switch (dt.Rows[i]["ESCORTS_PERSON"].ToString().Trim())
                {
                    case "A": e[i].ESCORTS_PERSON = "自行"; break;
                    case "B": e[i].ESCORTS_PERSON = "親友"; break;
                    case "C": e[i].ESCORTS_PERSON = "肇事者"; break;
                    case "D": e[i].ESCORTS_PERSON = "警員"; break;
                    case "E": e[i].ESCORTS_PERSON = "安養人員"; break;
                    case "Z": e[i].ESCORTS_PERSON = "其他:"+dt.Rows[i]["ESCORTS_PERSON_TEL"]+dt.Rows[i]["ESCORTS_REMARK"].ToString().Trim(); break;
                }
                //**e[0].ESCORTS_PERSON = dt.Rows[i]["ESCORTS_PERSON"].ToString();//"林詩涵 ";
                e[i].GCS_E = dt.Rows[i]["GCS_E"].ToString();//"(C) 腫到睜不開";
                e[i].GCS_V = dt.Rows[i]["GCS_V"].ToString();//"(2) 可發出聲音";
                e[i].GCS_M = dt.Rows[i]["GCS_M"].ToString();//"(4) 對疼痛刺激有反應，肢體會閃避";

                double x;
                if (double.TryParse(dt.Rows[i]["GCS_E"].ToString(), out x))
                {
                    if (double.TryParse(dt.Rows[i]["GCS_V"].ToString(), out x))
                    {
                        e[i].GCS_Total = (int.Parse(dt.Rows[i]["GCS_E"].ToString()) + int.Parse(dt.Rows[i]["GCS_V"].ToString()) + int.Parse(dt.Rows[i]["GCS_M"].ToString())).ToString();
                    }
                    else
                    {
                        e[i].GCS_Total = (int.Parse(dt.Rows[i]["GCS_E"].ToString()) + int.Parse(dt.Rows[i]["GCS_M"].ToString()) +dt.Rows[i]["GCS_V"].ToString()).ToString();
                    }
                }
                else
                {
                    if (double.TryParse(dt.Rows[i]["GCS_V"].ToString(), out x))
                    {
                        e[i].GCS_Total = (int.Parse(dt.Rows[i]["GCS_V"].ToString()) + int.Parse(dt.Rows[i]["GCS_M"].ToString())+ dt.Rows[i]["GCS_E"].ToString()).ToString();
                    }
                    else
                    {
                        e[i].GCS_Total =(int.Parse(dt.Rows[i]["GCS_M"].ToString()) + dt.Rows[i]["GCS_E"].ToString() +  dt.Rows[i]["GCS_V"].ToString()).ToString();
                    }
                }
                //e[i].GCS_Total = (int.Parse(dt.Rows[i]["GCS_E"].ToString()) + int.Parse(dt.Rows[i]["GCS_V"].ToString()) + int.Parse(dt.Rows[i]["GCS_M"].ToString())).ToString();//"14";
                e[i].BLOOD_TYPE = dt.Rows[i]["BLOOD_TYPE_PT"].ToString(); //"B +";

                e[i].PUPIL_SIZE_LEFT = dt.Rows[i]["PUPIL_SIZE_LEFT"].ToString();//"+ 1.1";
                e[i].PUPIL_SIZE_RIGHT = dt.Rows[i]["PUPIL_SIZE_RIGHT"].ToString(); //"+ 1.2";
                if (dt.Rows[i]["PUPIL_SIZE_LEFT"].ToString() == "0.0")
                {
                    e[i].PUPIL_SIZE_LEFT = "NA";
                }
                if (dt.Rows[i]["PUPIL_SIZE_RIGHT"].ToString() == "0.0")
                {
                    e[i].PUPIL_SIZE_RIGHT = "NA";
                }

                string[] SKIN_SIGN = dt.Rows[i]["SKIN_SIGN"].ToString().Trim().Split(',');
                foreach (var flag in SKIN_SIGN)
                {
                    switch (flag)
                    {
                        case "A": e[i].SKIN_SIGN += "正常"; break;
                        case "B": e[i].SKIN_SIGN += "蒼白"; break;
                        case "C": e[i].SKIN_SIGN += "|潮紅"; break;
                        case "D": e[i].SKIN_SIGN += "|發紺"; break;
                        case "E": e[i].SKIN_SIGN += "|冰涼"; break;
                        case "F": e[i].SKIN_SIGN += "|乾燥"; break;
                        case "G": e[i].SKIN_SIGN += "|潮濕"; break;
                        case "H": e[i].SKIN_SIGN += "|外傷"; break;
                        case "I": e[i].SKIN_SIGN += "|傷口"; break;
                        case "J": e[i].SKIN_SIGN += "|壓瘡"; break;
                        case "Z":
                            e[i].SKIN_SIGN += "|其他:";
                            e[i].SKIN_SIGN += dt.Rows[i]["SKIN_SIGN_REMARK"].ToString().Trim();
                            break;
                    }
                }
                if (e[i].SKIN_SIGN == null)
                {
                    e[i].SKIN_SIGN = "";
                }
                else
                {
                    e[i].SKIN_SIGN = e[i].SKIN_SIGN.TrimStart('|');
                }
                //e**[0].SKIN_SIGN = dt.Rows[i]["SKIN_SIGN"].ToString();//"皮膚徵象";

                string[] SKIN_UNUSUAL_SPOT = dt.Rows[i]["SKIN_UNUSUAL_SPOT"].ToString().Trim().Split(',');
                foreach (var flag in SKIN_UNUSUAL_SPOT)
                {

                    switch (dt.Rows[i]["SKIN_UNUSUAL_SPOT"].ToString().Trim())
                    {
                        case "A": e[i].SKIN_UNUSUAL_SPOT += "|全身"; break;
                        case "B": e[i].SKIN_UNUSUAL_SPOT += "|頸"; break;
                        case "C": e[i].SKIN_UNUSUAL_SPOT += "|臉部"; break;
                        case "D": e[i].SKIN_UNUSUAL_SPOT += "|上肢"; break;
                        case "E": e[i].SKIN_UNUSUAL_SPOT += "|下肢"; break;
                        case "F": e[i].SKIN_UNUSUAL_SPOT += "|背部"; break;
                        case "G": e[i].SKIN_UNUSUAL_SPOT += "|軀幹"; break;
                        case "Z":
                            e[i].SKIN_UNUSUAL_SPOT += "|其他:";
                            e[i].SKIN_UNUSUAL_SPOT += dt.Rows[i]["SKIN_UNUSUAL_SPOT_REMARK"].ToString().Trim();
                            break;
                    }
                }
                if (e[i].SKIN_UNUSUAL_SPOT == null)
                {
                    e[i].SKIN_UNUSUAL_SPOT = "";
                }
                else
                {
                    e[i].SKIN_UNUSUAL_SPOT = e[i].SKIN_UNUSUAL_SPOT.TrimStart('|');
                }
                //**e[0].SKIN_UNUSUAL_SPOT = dt.Rows[i]["SKIN_UNUSUAL_SPOT"].ToString();//"皮膚異常部位";
                switch (dt.Rows[i]["ALLERGY_FLAG"].ToString().Trim())
                {
                    case " ": e[i].ALLERGY_FLAG = "不明"; break;
                    case "Y": e[i].ALLERGY_FLAG = "有"; break;
                    case "N": e[i].ALLERGY_FLAG = "無"; break;
                }
                //**e[0].ALLERGY_FLAG = dt.Rows[i]["ALLERGY_FLAG"].ToString();//"有";
                e[i].ALLERGY_MED_FOOD = dt.Rows[i]["ALLERGY_MED_FOOD"].ToString();//"過敏_藥食物";
                e[i].ALLERGY_OTHER = dt.Rows[i]["ALLERGY_OTHER"].ToString();//"過敏_其他";

                string[] HIS_MEDICAL = dt.Rows[i]["HIS_MEDICAL"].ToString().Trim().Split(',');
                foreach (var flag in HIS_MEDICAL)
                {
                    switch (flag)
                    {
                        case "A": e[i].HIS_MEDICAL += "無"; break;
                        case "B": e[i].HIS_MEDICAL += "高血壓"; break;
                        case "C": e[i].HIS_MEDICAL += "|心臟病"; break;
                        case "D": e[i].HIS_MEDICAL += "|糖尿病"; break;
                        case "E": e[i].HIS_MEDICAL += "|腎臟疾病"; break;
                        case "F": e[i].HIS_MEDICAL += "|肺部疾病"; break;
                        case "G":
                            e[i].HIS_MEDICAL += "|癌病:";
                            e[i].HIS_MEDICAL += dt.Rows[i]["HIS_MEDICAL_CANCEL"].ToString().Trim();
                            break;
                        case "H": e[i].HIS_MEDICAL += "|癲癇"; break;
                        case "I": e[i].HIS_MEDICAL += "|熱痙攣"; break;
                        case "J":
                            e[i].HIS_MEDICAL += "|手術:";
                            e[i].HIS_MEDICAL += dt.Rows[i]["HIS_MEDICAL_OP"].ToString().Trim();
                            break;
                        case "Z":
                            e[i].HIS_MEDICAL += "|其他:";
                            e[i].HIS_MEDICAL += dt.Rows[i]["HIS_MEDICAL_OTHER"].ToString().Trim();
                            break;
                    }
                }
                if (e[i].HIS_MEDICAL == null)
                {
                    e[i].HIS_MEDICAL = "";
                }
                else
                {
                    e[i].HIS_MEDICAL = e[i].HIS_MEDICAL.TrimStart('|');
                }
                //**e[i].HIS_MEDICAL = dt.Rows[i]["HIS_MEDICAL"].ToString();//"病史";

                e[i].VITAL_SIGNS_FLAG = dt.Rows[i]["VITAL_SIGNS_FLAG"].ToString();//"直入";
                e[i].BP_SYSTOLIC = dt.Rows[i]["BP_SYSTOLIC"].ToString();//"124";
                e[i].BP_DIASTOLIC = dt.Rows[i]["BP_DIASTOLIC"].ToString() ;//"71";
                if (dt.Rows[i]["BP_SYSTOLIC"].ToString()=="0")
                {
                    e[i].BP_SYSTOLIC = "無";
                }
                if (dt.Rows[i]["BP_DIASTOLIC"].ToString()=="0")
                {
                    e[i].BP_DIASTOLIC = "無";
                }
                e[i].BP_REMARK = dt.Rows[i]["BP_REMARK"].ToString();//"血壓_備註";
                e[i].TEMPERATURE = dt.Rows[i]["TEMPERATURE"].ToString();// + "℃";//"37.5";
                e[i].TEMPERATURE_NOTE = "T";
                e[i].PULSE = dt.Rows[i]["PULSE"].ToString();// + "bpm"; //"89";
                if (dt.Rows[i]["PULSE"].ToString()=="0")
                {
                    e[i].PULSE = "無";
                }
                e[i].RESPIRE = dt.Rows[i]["RESPIRE"].ToString();// + "bpm";//"17";
                if (dt.Rows[i]["RESPIRE"].ToString() == "0")
                {
                    e[i].RESPIRE = "無";
                }
                e[i].BODY_WEIGHT = dt.Rows[i]["BODY_WEIGHT"].ToString();//"60";
                if (Convert.ToDouble(dt.Rows[i]["BODY_WEIGHT"])==0)
                {
                    e[i].BODY_WEIGHT = "NA";
                }
                switch (dt.Rows[i]["HIS_CONTACT_FLAG"].ToString().Trim())
                {
                    case "Y": e[i].HIS_CONTACT_FLAG = "有:";
                        e[i].HIS_CONTACT_FLAG += dt.Rows[i]["HIS_CONTACT_REMARK"].ToString().Trim();
                        break;
                    case "N": e[i].HIS_CONTACT_FLAG = "無"; break;
                }
                //**e[0].HIS_CONTACT_FLAG = dt.Rows[i]["HIS_CONTACT_FLAG"].ToString();//"接觸史";
                switch (dt.Rows[i]["HIS_TRAVEL_FLAG"].ToString().Trim())
                {
                    case "Y": e[i].HIS_TRAVEL_FLAG = "有:";
                        e[i].HIS_TRAVEL_FLAG += dt.Rows[i]["HIS_TRAVEL_REMARK"].ToString().Trim();
                        break;
                    case "N": e[i].HIS_TRAVEL_FLAG = "否"; break;
                }
                //**e[0].HIS_TRAVEL_FLAG = dt.Rows[i]["HIS_TRAVEL_FLAG"].ToString();//"旅遊史";
                switch (dt.Rows[i]["HIS_GATHER_FLAG"].ToString().Trim())
                {
                    case "Y": e[i].HIS_GATHER_FLAG = "有:";
                        e[i].HIS_GATHER_FLAG += dt.Rows[i]["HIS_GATHER_REMARK"].ToString().Trim();
                        break;
                    case "N": e[i].HIS_GATHER_FLAG = "無"; break;
                }
                //**e[i].HIS_GATHER_FLAG = dt.Rows[i]["HIS_GATHER_FLAG"].ToString();//"群聚史";
                //**e[i].HIS_OCCUPATION_FLAG = dt.Rows[i]["HIS_OCCUPATION_FLAG"].ToString();//"職業史";
                switch (dt.Rows[i]["HIS_OCCUPATION_FLAG"].ToString().Trim())
                {
                    case "9": e[i].HIS_OCCUPATION_FLAG = "其他:";
                        e[i].HIS_OCCUPATION_FLAG += dt.Rows[i]["HIS_OCCUPATION_REMARK"].ToString().Trim();
                        break;
                    case "N": e[i].HIS_OCCUPATION_FLAG = "無"; break;
                }
                switch (dt.Rows[i]["BREATH_CONDITION"].ToString().Trim())
                {
                    case "A": e[i].BREATH_CONDITION = "正常"; break;
                    case "B": e[i].BREATH_CONDITION = "淺快"; break;
                    case "C": e[i].BREATH_CONDITION = "費力"; break;
                    case "D": e[i].BREATH_CONDITION = "深慢"; break;
                    case "E": e[i].BREATH_CONDITION = "無"; break;
                }
                //**e[i].BREATH_CONDITION = dt.Rows[i]["BREATH_CONDITION"].ToString();//"呼吸型態";
                e[i].SUBJECT = dt.Rows[i]["SUBJECT"].ToString();//"主訴";
                e[i].SPO2 = dt.Rows[i]["SPO2"].ToString();// +"%";//"97";
                if (dt.Rows[i]["SPO2"].ToString() == "0")
                {
                    e[i].SPO2 = "NA";
                }
                e[i].MEASURING_TIME = Convert.ToDateTime(string.Format("{0}:{1}", dt.Rows[i]["MEASURING_TIME"].ToString().Substring(0, 2), dt.Rows[i]["MEASURING_TIME"].ToString().Substring(2, 2))).ToString("HH:mm");//DateTime.Now.ToString("HH:mm");
                switch (dt.Rows[i]["INJURY_FLAG"].ToString().Trim())
                {
                    case "1": e[i].INJURY_FLAG = "車禍"; break;
                    case "2": e[i].INJURY_FLAG = "跌倒"; break;
                    case "3": e[i].INJURY_FLAG = "墜落"; break;
                    case "4": e[i].INJURY_FLAG = "壓砸鈍傷"; break;
                    case "5": e[i].INJURY_FLAG = "穿刺切割傷"; break;
                    case "6": e[i].INJURY_FLAG = "溺水"; break;
                    case "7": e[i].INJURY_FLAG = "燒燙傷"; break;
                    case "8": e[i].INJURY_FLAG = "暴力"; break;
                    case "9": e[i].INJURY_FLAG = "其他"; break;
                }
                //**e[i].INJURY_FLAG = dt.Rows[i]["INJURY_FLAG"].ToString();//"車禍";
                DataTable dt2 = new DataTable();
                dt2 = DAOPatient.ER_WOUND(dt.Rows[i]["ER_SERIAL_NO"].ToString().Trim(), sqlCombine3);
                for (int j = 0; j < dt2.Rows.Count; j++)
                {
                    e[i].ER_WOUND += "|" + dt2.Rows[j]["TRAUMA_NAME"].ToString().Trim() + ":" + dt2.Rows[j]["POSITION"].ToString().Trim() + "(" + dt2.Rows[j]["SIGN"].ToString().Trim() + ")";
                }

                if (e[i].ER_WOUND == null)
                {
                    e[i].ER_WOUND = "";
                }
                else
                {
                    e[i].ER_WOUND = e[i].ER_WOUND.TrimStart('|');
                }

                //e[0].ER_WOUND = "很多傷口";
                //重複e[0].HIS_MEDICAL = "糖尿病、高血壓、中風。癌症：大腸癌三期。手術：割盲腸。";
                e[i].TRIAGE_NURSE = dt.Rows[i]["ER_WOUND_CLERK"].ToString();//"檢傷護士";
                e[i].CONFRIM_NURSE = dt.Rows[i]["ER_WOUND_CLERK"].ToString();//"確認護士";
                e[i].VITAL_SIGNS_NOTE = dt.Rows[i]["TPR_REMARK"].ToString();//"生命徵象備註";
                e[i].VAS = "數字量表 "+ dt.Rows[i]["VAS"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetERListAll()
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("ERListAll");
        dtSql2 = GetSql.SqlLst2("ERListAll");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }
        DataTable dt = new DataTable();
        dt = DAOPatient.GetERListAll(sqlCombine);

        if (dt.Rows.Count > 0)
        {
            ERList[] e = new ERList[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new ERList();
                e[i].RegTime = StrTransformDate(dt.Rows[i]["ADMIT_DATE"].ToString(), dt.Rows[i]["ADMIT_TIME"].ToString());
                e[i].ChartNo = dt.Rows[i]["CHART_NO"].ToString();
                e[i].PatientName = dt.Rows[i]["PATIENTNAME"].ToString();
                e[i].Doc = dt.Rows[i]["DOC"].ToString();
                e[i].Level = dt.Rows[i]["ER_WOUND_LEVEL"].ToString();
                e[i].BedNo = dt.Rows[i]["ER_BED_NO"].ToString();
                e[i].DBack = dt.Rows[i]["DBACK"].ToString();
                e[i].CT = dt.Rows[i]["CT"].ToString();
                if (dt.Rows[i]["CT"].ToString().Trim() == "")
                {
                    e[i].CT = "0";
                }
                e[i].XRay = dt.Rows[i]["XRAY"].ToString();
                if (dt.Rows[i]["XRAY"].ToString().Trim() == "")
                {
                    e[i].XRay = "0";
                }
                e[i].Bio = dt.Rows[i]["BIO"].ToString();
                if (dt.Rows[i]["BIO"].ToString().Trim()=="")
                {
                    e[i].Bio = "0";
                }
                e[i].ME = dt.Rows[i]["ME"].ToString();
                if (dt.Rows[i]["ME"].ToString().Trim() == "")
                {
                    e[i].ME = "0";
                }
                e[i].Blood = dt.Rows[i]["BLOOD"].ToString();
                if (dt.Rows[i]["BLOOD"].ToString().Trim() == "")
                {
                    e[i].Blood = "0";
                }
                e[i].EKG = dt.Rows[i]["EKG"].ToString();
                if (dt.Rows[i]["EKG"].ToString().Trim()=="")
                {
                    e[i].EKG = "0";
                }
                e[i].FeeNo = dt.Rows[i]["FEENO"].ToString();
                e[i].Note = dt.Rows[i]["NOTE"].ToString();
                e[i].DeptNo = dt.Rows[i]["DEPTNO"].ToString();
                e[i].Status = dt.Rows[i]["STATUS"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
        //ERList[] e = new ERList[3];
        //e[0] = new ERList();
        //e[0].RegTime = DateTime.Now.AddHours(-1);
        //e[0].ChartNo = "01031342";
        //e[0].PatientName = "王O淳";
        //e[0].Doc = "方O彬";
        //e[0].Level = "一級";
        //e[0].BedNo = "6C061";
        //e[0].DBack = "";
        //e[0].CT = "0";
        //e[0].XRay = "3";
        //e[0].Bio = "99";
        //e[0].ME = "2";
        //e[0].Blood = "0";
        //e[0].EKG = "1";
        //e[0].FeeNo = "I0333030";
        //e[0].Note = "特殊註記";
        //e[0].DeptNo = "6000";
        //e[0].Status = "Y";
        //e[1] = new ERList();
        //e[1].RegTime = DateTime.Now.AddDays(-3);
        //e[1].ChartNo = "04368674";
        //e[1].PatientName = "林O彤";
        //e[1].Doc = "方O彬";
        //e[1].Level = "三級";
        //e[1].BedNo = "6C131";
        //e[1].DBack = "返";
        //e[1].CT = "1";
        //e[1].XRay = "13";
        //e[1].Bio = "9";
        //e[1].ME = "23";
        //e[1].Blood = "1";
        //e[1].EKG = "10";
        //e[1].FeeNo = "I0333036";
        //e[1].Note = "特殊註記";
        //e[1].DeptNo = "6001";
        //e[1].Status = "N";
        //e[2] = new ERList();
        //e[2].RegTime = DateTime.Now.AddHours(-5);
        //e[2].ChartNo = "01542967";
        //e[2].PatientName = "林O羽";
        //e[2].Doc = "方O彬";
        //e[2].Level = "五級";
        //e[2].BedNo = "6C153";
        //e[2].DBack = "";
        //e[2].CT = "10";
        //e[2].XRay = "14";
        //e[2].Bio = "19";
        //e[2].ME = "3";
        //e[2].Blood = "1";
        //e[2].EKG = "1";
        //e[2].FeeNo = "I0333014";
        //e[2].Note = "特殊註記";
        //e[2].DeptNo = "6002";
        //e[2].Status = "Y";
        //byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
        //return result;
    }

    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetERListbyDate(string start_date, string end_date,string status)
    {
        string sqlCombine_1 = string.Empty;
        string sqlCombine2_1 = string.Empty;
        DataTable dtSql_1 = new DataTable();
        DataTable dtSql2_1 = new DataTable();
        dtSql_1 = GetSql.SqlLst("ERListbyDate");
        dtSql2_1 = GetSql.SqlLst2("ERListbyDate");
        if (dtSql_1.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql_1.Rows.Count; i++)
            {
                sqlCombine_1 += dtSql_1.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2_1.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2_1.Rows.Count; i++)
            {
                sqlCombine_1 = sqlCombine_1.Replace("#" + dtSql2_1.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2_1.Rows[i]["fun_arg_con"].ToString());
            }
        }
        DataTable dt_1 = new DataTable();
        dt_1 = DAOPatient.ERListbyDate(sqlCombine_1, start_date, end_date);
        if (dt_1.Rows.Count > 0)
        {
            string DRIVE_ID = "OPD";
            int CLINIC_DATE_YEAR = 0;
            ERList[] e = new ERList[dt_1.Rows.Count];
            for (int j = 0; j < dt_1.Rows.Count; j++)
            {
                e[j] = new ERList();
                string sqlCombine = string.Empty;
                string sqlCombine2 = string.Empty;
                DataTable dtSql = new DataTable();
                DataTable dtSql2 = new DataTable();
                dtSql = GetSql.SqlLst("ERListbyChartNo");
                dtSql2 = GetSql.SqlLst2("ERListbyChartNo");
                if (dtSql.Rows.Count > 0)
                {
                    for (int i = 0; i < dtSql.Rows.Count; i++)
                    {
                        sqlCombine += dtSql.Rows[i]["fun_sql"];
                    }
                }
                if (dtSql2.Rows.Count > 0)
                {
                    for (int i = 0; i < dtSql2.Rows.Count; i++)
                    {
                        sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
                    }
                }
                CLINIC_DATE_YEAR = int.Parse(dt_1.Rows[j]["CLINIC_DATE"].ToString().Substring(0, 3));
                switch (CLINIC_DATE_YEAR % 5)
                {
                    case 1:
                        DRIVE_ID = "OHIS" + dt_1.Rows[j]["CLINIC_DATE"].ToString().Substring(3, 2);
                        break;
                    case 2:
                        DRIVE_ID = "OHISB" + dt_1.Rows[j]["CLINIC_DATE"].ToString().Substring(3, 2);
                        break;
                    case 3:
                        DRIVE_ID = "OHISC" + dt_1.Rows[j]["CLINIC_DATE"].ToString().Substring(3, 2);
                        break;
                    case 4:
                        DRIVE_ID = "OHISD" + dt_1.Rows[j]["CLINIC_DATE"].ToString().Substring(3, 2);
                        break;
                    case 0:
                        DRIVE_ID = "OHISE" + dt_1.Rows[j]["CLINIC_DATE"].ToString().Substring(3, 2);
                        break;
                }
                DataTable dt = new DataTable();
                if (status == "A")
                {
                    //dt = DAOPatient.ERListbyChartNo(sqlCombine, DRIVE_ID, dt_1.Rows[j]["CHART_NO"].ToString(), dt_1.Rows[j]["CLINIC_DATE"].ToString(), " ");
                    dt = DAOPatient.ERListbyChartNo(sqlCombine, DRIVE_ID, dt_1.Rows[j]["CHART_NO"].ToString(), dt_1.Rows[j]["CLINIC_DATE"].ToString(), dt_1.Rows[j]["DUPLICATE_NO"].ToString(), "Y','P',' ");
                }
                else
                {
                    dt = DAOPatient.ERListbyChartNo(sqlCombine, DRIVE_ID, dt_1.Rows[j]["CHART_NO"].ToString(), dt_1.Rows[j]["CLINIC_DATE"].ToString(), dt_1.Rows[j]["DUPLICATE_NO"].ToString(), " ");
                }
                if (dt.Rows.Count > 0)
                {
                     //e[i].RegTime = DateTime.Now.AddHours(-1);
                    e[j].RegTime = StrTransformDate(dt.Rows[0]["ADMIT_DATE"].ToString(), dt.Rows[0]["ADMIT_TIME"].ToString());
                        e[j].ChartNo = dt.Rows[0]["CHART_NO"].ToString();
                        e[j].PatientName = dt.Rows[0]["PT_NAME"].ToString();
                        e[j].Doc = dt.Rows[0]["DOC"].ToString();
                        e[j].Level = dt.Rows[0]["ER_WOUND_LEVEL"].ToString();
                        e[j].BedNo = dt.Rows[0]["ER_BED_NO"].ToString();
                        e[j].DBack = dt.Rows[0]["DBACK"].ToString();
                        e[j].CT = dt.Rows[0]["CT"].ToString();
                        if (dt.Rows[0]["CT"].ToString().Trim() == "")
                        {
                            e[j].CT = "0";
                        }
                        e[j].XRay = dt.Rows[0]["XRAY"].ToString();
                        if (dt.Rows[0]["XRAY"].ToString().Trim() == "")
                        {
                            e[j].XRay = "0";
                        }
                        e[j].Bio = dt.Rows[0]["BIO"].ToString();
                        if (dt.Rows[0]["BIO"].ToString().Trim() == "")
                        {
                            e[j].Bio = "0";
                        }
                        e[j].ME = dt.Rows[0]["ME"].ToString();
                        if (dt.Rows[0]["ME"].ToString().Trim() == "")
                        {
                            e[j].ME = "0";
                        }
                        e[j].Blood = dt.Rows[0]["BLOOD"].ToString();
                        if (dt.Rows[0]["BLOOD"].ToString().Trim() == "")
                        {
                            e[j].Blood = "0";
                        }
                        e[j].EKG = dt.Rows[0]["EKG"].ToString();
                        if (dt.Rows[0]["EKG"].ToString().Trim() == "")
                        {
                            e[j].EKG = "0";
                        }
                        e[j].FeeNo = dt.Rows[0]["FEENO"].ToString();
                        e[j].Note = dt.Rows[0]["NOTE"].ToString();
                        e[j].DeptNo = dt.Rows[0]["DEPTNO"].ToString();
                        e[j].Status = dt.Rows[0]["STATUS"].ToString();
                }
                else
                {
                    e[j].RegTime = DateTime.MaxValue;
                    e[j].ChartNo = "";
                    e[j].PatientName = "";
                    e[j].Doc = "";
                    e[j].Level = "";
                    e[j].BedNo = "";
                    e[j].DBack = "";
                    e[j].CT = "0";
                    e[j].XRay = "0";
                    e[j].Bio = "0";
                    e[j].ME = "0";
                    e[j].Blood = "0";
                    e[j].EKG = "0";
                    e[j].FeeNo = "";
                    e[j].Note = "";
                    e[j].DeptNo = "";
                    e[j].Status = "";
                }
            }
           
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }


    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetERListbyChartNo(string ChartNo)
    {
        string sqlCombine_1 = string.Empty;
        //string sqlCombine2_1 = string.Empty;
        DataTable dtSql_1 = new DataTable();
        DataTable dtSql2_1 = new DataTable();
        dtSql_1 = GetSql.SqlLst("OpdDateByChartNo");
        dtSql2_1 = GetSql.SqlLst2("OpdDateByChartNo");
        if (dtSql_1.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql_1.Rows.Count; i++)
            {
                sqlCombine_1 += dtSql_1.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2_1.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2_1.Rows.Count; i++)
            {
                sqlCombine_1 = sqlCombine_1.Replace("#" + dtSql2_1.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2_1.Rows[i]["fun_arg_con"].ToString());
            }
        }
        DataTable dt_1 = new DataTable();
        dt_1 = DAOPatient.OpdDateByChartNo(sqlCombine_1, ChartNo);
        if (dt_1.Rows.Count > 0)
        {
            string DRIVE_ID = "OPD";
            int CLINIC_DATE_YEAR = 0;
            ERList[] e = new ERList[dt_1.Rows.Count];
            for (int j = 0; j < dt_1.Rows.Count; j++)
            {
                e[j] = new ERList();
                string sqlCombine = string.Empty;
                string sqlCombine2 = string.Empty;
                DataTable dtSql = new DataTable();
                DataTable dtSql2 = new DataTable();
                dtSql = GetSql.SqlLst("ERListbyChartNo");
                dtSql2 = GetSql.SqlLst2("ERListbyChartNo");
                if (dtSql.Rows.Count > 0)
                {
                    for (int i = 0; i < dtSql.Rows.Count; i++)
                    {
                        sqlCombine += dtSql.Rows[i]["fun_sql"];
                    }
                }
                if (dtSql2.Rows.Count > 0)
                {
                    for (int i = 0; i < dtSql2.Rows.Count; i++)
                    {
                        sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
                    }
                }
                CLINIC_DATE_YEAR = int.Parse(dt_1.Rows[j]["CLINIC_DATE"].ToString().Substring(0, 3));
                switch (CLINIC_DATE_YEAR % 5)
                {
                    case 1:
                        DRIVE_ID = "OHIS" + dt_1.Rows[j]["CLINIC_DATE"].ToString().Substring(3, 2);
                        break;
                    case 2:
                        DRIVE_ID = "OHISB" + dt_1.Rows[j]["CLINIC_DATE"].ToString().Substring(3, 2);
                        break;
                    case 3:
                        DRIVE_ID = "OHISC" + dt_1.Rows[j]["CLINIC_DATE"].ToString().Substring(3, 2);
                        break;
                    case 4:
                        DRIVE_ID = "OHISD" + dt_1.Rows[j]["CLINIC_DATE"].ToString().Substring(3, 2);
                        break;
                    case 0:
                        DRIVE_ID = "OHISE" + dt_1.Rows[j]["CLINIC_DATE"].ToString().Substring(3, 2);
                        break;
                }
                DataTable dt = new DataTable();
                dt = DAOPatient.ERListbyChartNo(sqlCombine, DRIVE_ID, dt_1.Rows[j]["CHART_NO"].ToString(), dt_1.Rows[j]["CLINIC_DATE"].ToString(), dt_1.Rows[j]["DUPLICATE_NO"].ToString(), "Y','P',' ");
                if (dt.Rows.Count > 0)
                {
                    //e[i].RegTime = DateTime.Now.AddHours(-1);
                    e[j].RegTime = StrTransformDate(dt.Rows[0]["ADMIT_DATE"].ToString(), dt.Rows[0]["ADMIT_TIME"].ToString());
                    e[j].ChartNo = dt.Rows[0]["CHART_NO"].ToString();
                    e[j].PatientName = dt.Rows[0]["PT_NAME"].ToString();
                    e[j].Doc = dt.Rows[0]["DOC"].ToString();
                    e[j].Level = dt.Rows[0]["ER_WOUND_LEVEL"].ToString();
                    e[j].BedNo = dt.Rows[0]["ER_BED_NO"].ToString();
                    e[j].DBack = dt.Rows[0]["DBACK"].ToString();
                    e[j].CT = dt.Rows[0]["CT"].ToString();
                    if (dt.Rows[0]["CT"].ToString().Trim() == "")
                    {
                        e[j].CT = "0";
                    }
                    e[j].XRay = dt.Rows[0]["XRAY"].ToString();
                    if (dt.Rows[0]["XRAY"].ToString().Trim() == "")
                    {
                        e[j].XRay = "0";
                    }
                    e[j].Bio = dt.Rows[0]["BIO"].ToString();
                    if (dt.Rows[0]["BIO"].ToString().Trim() == "")
                    {
                        e[j].Bio = "0";
                    }
                    e[j].ME = dt.Rows[0]["ME"].ToString();
                    if (dt.Rows[0]["ME"].ToString().Trim() == "")
                    {
                        e[j].ME = "0";
                    }
                    e[j].Blood = dt.Rows[0]["BLOOD"].ToString();
                    if (dt.Rows[0]["BLOOD"].ToString().Trim() == "")
                    {
                        e[j].Blood = "0";
                    }
                    e[j].EKG = dt.Rows[0]["EKG"].ToString();
                    if (dt.Rows[0]["EKG"].ToString().Trim() == "")
                    {
                        e[j].EKG = "0";
                    }
                    e[j].FeeNo = dt.Rows[0]["FEENO"].ToString();
                    e[j].Note = dt.Rows[0]["NOTE"].ToString();
                    e[j].DeptNo = dt.Rows[0]["DEPTNO"].ToString();
                    e[j].Status = dt.Rows[0]["STATUS"].ToString();
                }
                else
                {
                    e[j].RegTime = DateTime.MaxValue; 
                    e[j].ChartNo = "";
                    e[j].PatientName = "";
                    e[j].Doc = "";
                    e[j].Level = "";
                    e[j].BedNo = "";
                    e[j].DBack = "";
                    e[j].CT = "0";
                    e[j].XRay = "0";
                    e[j].Bio = "0";
                    e[j].ME = "0";
                    e[j].Blood = "0";
                    e[j].EKG = "0";
                    e[j].FeeNo = "";
                    e[j].Note = "";
                    e[j].DeptNo = "";
                    e[j].Status = "";
                }
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetERConsultation(string feeno)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("ERConsultation");
        dtSql2 = GetSql.SqlLst2("ERConsultation");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }
        DataTable dt = new DataTable();
        dt = DAOPatient.GetERConsultation(feeno, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            ERConsultation[] e = new ERConsultation[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new ERConsultation();
                e[i].ApplicationTime = dt.Rows[i]["APPLY_DATE"].ToString() + "  "+ dt.Rows[i]["APPLY_HHMM"].ToString();
                e[i].Cateory = dt.Rows[i]["CONSULT_TYPE"].ToString();
                //1-ROUTINE 2-ELECTIVE 3-EMERGENCY 4-FOLLOWING 5-營養治療Nutrition Therapy  6-ER Emergency(急診特急;限10min) 7-ER Urgent(急診急會;限30min) 8-ER Regular(急診常規;限4hr) 9-EXAMINATION(檢查研究會診)
                switch (dt.Rows[i]["CONSULT_TYPE"].ToString())
                {
                    case "1":
                        e[i].Cateory = "ROUTINE";
                        break;
                    case "2":
                        e[i].Cateory = "ELECTIVE";
                        break;
                    case "3":
                        e[i].Cateory = "EMERGENCY";
                        break;
                    case "4":
                        e[i].Cateory = "FOLLOWING";
                        break;
                    case "5":
                        e[i].Cateory = "Nutrition Therapy";
                        break;
                    case "6":
                        e[i].Cateory = "ER Emergency";
                        break;
                    case "7":
                        e[i].Cateory = "ER Urgent";
                        break;
                    case "8":
                        e[i].Cateory = "ER Regular";
                        break;
                    case "9":
                        e[i].Cateory = "EXAMINATION";
                        break;
                }
                e[i].NoteDept = dt.Rows[i]["REFERED_DIV_NAME"].ToString();
                e[i].NoteDoc = dt.Rows[i]["REFERED_DOCTOR_NO"].ToString();

                switch (dt.Rows[i]["CONSULT_DATE"].ToString())
                {
                    case "0":
                        e[i].Status = "未回覆";
                        break;
                    default:
                         e[i].Status = "已回覆";
                        break;
                }
                e[i].EnterName = dt.Rows[i]["APPLY_DOCTOR_NO"].ToString();
                e[i].ApplicationDoc = dt.Rows[i]["APPLY_DOCTOR_NO"].ToString();
                e[i].ApplicationDept = dt.Rows[i]["APPLY_DIV_NO"].ToString();
                e[i].Summery = dt.Rows[i]["APPLY_SUMMARY"].ToString();
                e[i].Result = dt.Rows[i]["REFERED_SUMMARY"].ToString();
                e[i].ConsultationDoc = dt.Rows[i]["REFERED_DOCTOR_NO"].ToString();
                e[i].ReplyTime = dt.Rows[i]["CONSULT_DATE"].ToString() + "  " +  dt.Rows[i]["CONSULT_HHMM"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            ERConsultation[] e = new ERConsultation[0];
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
    }

    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetTextOrderRecord(string feeno)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();

        string DRIVE_ID = "OPD";
        int CLINIC_DATE_YEAR = 0;
        if (feeno.Length > 10)
        {
            dtSql = GetSql.SqlLst("TextOrderRecord");
            dtSql2 = GetSql.SqlLst2("TextOrderRecord");
            CLINIC_DATE_YEAR = int.Parse(feeno.ToString().Substring(10, 3));
            switch (CLINIC_DATE_YEAR % 5)
            {
                case 1:
                    DRIVE_ID = "OHIS" + feeno.ToString().Substring(13, 2);
                    break;
                case 2:
                    DRIVE_ID = "OHISB" + feeno.ToString().Substring(13, 2);
                    break;
                case 3:
                    DRIVE_ID = "OHISC" + feeno.ToString().Substring(13, 2);
                    break;
                case 4:
                    DRIVE_ID = "OHISD" + feeno.ToString().Substring(13, 2);
                    break;
                case 0:
                    DRIVE_ID = "OHISE" + feeno.ToString().Substring(13, 2);
                    break;
            }
        }
        else
        {
            dtSql = GetSql.SqlLst("TextOrderRecord");
            dtSql2 = GetSql.SqlLst2("TextOrderRecord");
        }

        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }
        DataTable dt = new DataTable();
        dt = DAOPatient.GetTextOrderRecord(feeno, DRIVE_ID, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            TextOrderRecord[] e = new TextOrderRecord[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new TextOrderRecord();
                e[i].Content = dt.Rows[i]["Content"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            TextOrderRecord[] e = new TextOrderRecord[0];
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }

    }
    #endregion

    public static void log(string no, string count)
    {
        string now = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
        string today = DateTime.Now.ToString("yyyy-MM");


        if (!Directory.Exists("C:\\inetpub\\NIS_WS\\ws_log"))
        {
            Directory.CreateDirectory("C:\\inetpub\\NIS_WS\\ws_log");
        }
        string path = "C:\\inetpub\\NIS_WS\\ws_log\\" + today + ".txt";
        if (!File.Exists(path))
        {
            StreamWriter sw = File.CreateText(path);
            {
                sw.WriteLine(now + ":" +"員編"+ no +",筆數:"+ count);
            }
            sw.Close();
        }
        else
        {
            StreamWriter sr = new StreamWriter(path, true);
            {
                sr.WriteLine(now + ":" + "員編" + no + ",筆數:" + count);
            };
            sr.Close();
        }
    }

    /// <summary>
    /// 取得TPR的T-Bilirubin、血球容積%
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetTBilHCT(String FeeNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("TBilHCT");
        dtSql2 = GetSql.SqlLst2("TBilHCT");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }
        DataTable dt = new DataTable();
        dt = DAOPatient.GetTPRTBilHCT(FeeNo, sqlCombine);

        if (dt.Rows.Count > 0)
        {
            TPRTBilHCT[] e = new TPRTBilHCT[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                //DateTime UseDate = Convert.ToDateTime(DateTime.Now.ToString("yyyy/MM/dd HH:mm"));
                //e[0].UseDate = UseDate.AddDays(-3);
                //e[0].T_Bilirubin = "總膽紅素1";
                //e[0].HCT = "";
                
                e[i] = new TPRTBilHCT();
                e[i].UseDate = StrTransformDate(dt.Rows[i]["ORDER_DATE"].ToString(), "");
                if (dt.Rows[i]["CODE"].ToString() == "6309029")
                {
                    e[i].T_Bilirubin = dt.Rows[i]["RESULT_DESCRIPTION"].ToString();
                    e[i].HCT = "";
                }
                if (dt.Rows[i]["CODE"].ToString() == "6308004")
                {
                    e[i].T_Bilirubin = "";
                    e[i].HCT = dt.Rows[i]["RESULT_DESCRIPTION"].ToString();
                }
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 取得PRICE的IV list
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetIVFList()
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("IVFList");
        dtSql2 = GetSql.SqlLst2("IVFList");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }
        DataTable dt = new DataTable();
        dt = DAOCostCenter .GetIVFList(sqlCombine);

        if (dt.Rows.Count > 0)
        {
            IVItem[] e = new IVItem [dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new IVItem();
                e[i].name  =  dt.Rows[i]["CHINESE_NAME"].ToString().Replace("'", "’");
                e[i].sort = dt.Rows[i]["SEQ"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 取得全院病房/床號清單 Finish
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetBedList(string CCCode)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("BedList");
        dtSql2 = GetSql.SqlLst2("BedList");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }
        if (CCCode=="") { CCCode = "ALL"; } //全院

        DataTable dt = new DataTable();
        dt = DAOCostCenter.GetBedList(CCCode, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            BedItem[] e = new BedItem[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new BedItem();
                e[i].CostCenterCode = dt.Rows[i]["DEPT_CODE"].ToString();
                e[i].BedNo = dt.Rows[i]["BEDNUM"].ToString();
                e[i].CostCenterName  = dt.Rows[i]["DEPT_NAME"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }


    ///產科WEB SERVICE


    /// <summary>
    /// 產檢資料
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetBabyLab(string FEE_NO, String StartDate, String EndDate)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("BabyLab");
        dtSql2 = GetSql.SqlLst2("BabyLab");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetBabyLab(FEE_NO, StartDate, EndDate, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            BabyLab[] e = new BabyLab[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new BabyLab();
                e[i].FEE_NO = dt.Rows[i]["FEENO"].ToString();
                e[i].LabCode = dt.Rows[i]["LAB_CODE"].ToString();
                e[i].LabNameENG = dt.Rows[i]["LABNAME"].ToString();
                e[i].LabName = dt.Rows[i]["LABNAME"].ToString();
                e[i].LabValue = dt.Rows[i]["LABVALUE"].ToString();
                e[i].INSPTNo = "00";
                e[i].INSPTName = "行天宮醫療志業醫療財團法人恩主公醫院";
                DateTime LabDate = StrTransformDate(dt.Rows[i]["LabDate"].ToString(), "");
                e[i].INSPTDT = LabDate;
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }


    /// <summary>
    /// 病患資料
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetPatInfo(string IID)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("PatInfo");
        dtSql2 = GetSql.SqlLst2("PatInfo");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetPatInfo(IID, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            BaByPatInfo_IID[] e = new BaByPatInfo_IID[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new BaByPatInfo_IID();
                e[i].CHARNO  = dt.Rows[i]["CHART_NO"].ToString();
                e[i].PATName = dt.Rows[i]["PT_NAME"].ToString();
                e[i].PATBirthday = dt.Rows[i]["BIRTH_DATE"].ToString();
                e[i].PATAddress = dt.Rows[i]["ADDRESS"].ToString();
                e[i].PATHomeNo = dt.Rows[i]["TEL_HOME"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetPatInfo1(string CHARTNO)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("PatInfo1");
        dtSql2 = GetSql.SqlLst2("PatInfo1");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetPatInfo1(CHARTNO, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            BaByPatInfo_IID[] e = new BaByPatInfo_IID[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new BaByPatInfo_IID();
                e[i].CHARNO = dt.Rows[i]["CHART_NO"].ToString();
                e[i].PATName = dt.Rows[i]["PT_NAME"].ToString();
                e[i].PATBirthday = dt.Rows[i]["BIRTH_DATE"].ToString();
                e[i].PATAddress = dt.Rows[i]["ADDRESS"].ToString();
                e[i].PATHomeNo = dt.Rows[i]["TEL_HOME"].ToString();
                e[i].IID = dt.Rows[i]["ID_NO"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 病患資料
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetBaByPatInfo(string FeeNo)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("BaByPatInfo");
        dtSql2 = GetSql.SqlLst2("BaByPatInfo");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetBaByPatInfo(FeeNo, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            BaByPatInfo_FeeNo[] e = new BaByPatInfo_FeeNo[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new BaByPatInfo_FeeNo();
                e[i].PATID = dt.Rows[i]["ID_NO"].ToString();
                e[i].Blood_Type = dt.Rows[i]["BLOOD_TYPE"].ToString();
                e[i].PATBirthPlace = dt.Rows[i]["HOME_AREA_CODE"].ToString();
                e[i].PATBirthPlaceC = dt.Rows[i]["HOME_AREA_NAME"].ToString();
                e[i].PATAddress = dt.Rows[i]["ADDRESS"].ToString();
                e[i].PATHomeNo = dt.Rows[i]["TEL_HOME"].ToString();
                e[i].PATEducation = dt.Rows[i]["EDUCATION"].ToString();
                e[i].PATNational = dt.Rows[i]["ORIGIN_TOWN"].ToString();
                e[i].PATPregnancy = dt.Rows[i]["PREGNANCY"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 病患資料
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetBaByMatInfo(string IID)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("BaByMatInfo");
        dtSql2 = GetSql.SqlLst2("BaByMatInfo");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetBaByMatInfo(IID, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            BaByMatInfo[] e = new BaByMatInfo[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new BaByMatInfo();
                e[i].PATName = dt.Rows[i]["PT_NAME"].ToString();
                e[i].PATBirthday = dt.Rows[i]["BIRTH_DATE"].ToString();
                e[i].PATBirthPlace = dt.Rows[i]["HOME_AREA_CODE"].ToString();
                e[i].PATBirthPlaceC = dt.Rows[i]["HOME_AREA_NAME"].ToString();
                e[i].PATNational = dt.Rows[i]["ORIGIN_TOWN"].ToString();
                e[i].Blood_Type = dt.Rows[i]["BLOOD_TYPE"].ToString();
                e[i].PATEducation = dt.Rows[i]["EDUCATION"].ToString();
                e[i].PATHomeNo = dt.Rows[i]["TEL_HOME"].ToString();
                e[i].PATOcc = dt.Rows[i]["OCCUPATION_TYPE"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }
    
    /// <summary>
    /// 檢驗院所資料
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetINSPTList()
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("INSPTList");
        dtSql2 = GetSql.SqlLst2("INSPTList");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOCostCenter.GetINSPTList(sqlCombine);
        if (dt.Rows.Count > 0)
        {
            INSPTList[] e = new INSPTList[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new INSPTList();
                e[i].INSPTNo = dt.Rows[i]["HOSPITAL_CODE"].ToString();
                e[i].INSPTName = dt.Rows[i]["HOSPITAL_FULL_NAME"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 病患資料
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetSDM(string CHARTNO)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("SDM");
        dtSql2 = GetSql.SqlLst2("SDM");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetSDM(CHARTNO, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            SDMInfo[] e = new SDMInfo[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new SDMInfo();
                e[i].DATATime = dt.Rows[i]["CLINIC_DATE"].ToString();
                e[i].SDM1 = dt.Rows[i]["OPT_1"].ToString();
                e[i].SDM2 = dt.Rows[i]["OPT_2"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public string GetPatChartNo(string FEENO)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("PatChartNo");
        dtSql2 = GetSql.SqlLst2("PatChartNo");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetBABYLIKN(FEENO, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            Babylink[] e = new Babylink[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new Babylink();
                e[i].NB_CHARNO = dt.Rows[i]["CHART_NO"].ToString();
            }
            //byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));

            string result = e[0].NB_CHARNO;

            return result;
        }
        else
        {
            return null;
        }
    }

    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetPatFeeNo(string CHARTNO)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("PatFeeNo");
        dtSql2 = GetSql.SqlLst2("PatFeeNo");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetSDM(CHARTNO, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            BabyLab[] e = new BabyLab[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new BabyLab();
                e[i].FEE_NO = dt.Rows[i]["FEE_NO"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetBabylink(string FEENO)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("Babylink");
        dtSql2 = GetSql.SqlLst2("Babylink");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetBABYLIKN(FEENO, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            Babylink[] e = new Babylink[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new Babylink();
                e[i].NB_FEENO = dt.Rows[i]["ADMIT_NO"].ToString();
                e[i].NB_CHARNO = dt.Rows[i]["CHART_NO"].ToString();
                e[i].NB_SEQ = dt.Rows[i]["SEQ"].ToString();
                DateTime BIRTH_DATE = StrTransformDate(dt.Rows[i]["BIRTH_DATE"].ToString(), "");
                e[i].NB_BTDAY = BIRTH_DATE;
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetBirthTrauma(string FEENO)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("BirthTrauma");
        dtSql2 = GetSql.SqlLst2("BirthTrauma");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetBirthTrauma(FEENO, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            BirthTrauma[] e = new BirthTrauma[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new BirthTrauma();
                e[i].NUMBER_OF_BT = Int32.Parse(dt.Rows[i]["NUMBER_OF_BT"].ToString());
                e[i].Steroid = false;
                if (dt.Rows[i]["STEROID"].ToString() == "Y")
                {
                    e[i].Steroid = true;
                }
               
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }

    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetBabyFeeNo2(string NB_CHARNO,string NB_FEENO2)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("BabyFeeNo2");
        dtSql2 = GetSql.SqlLst2("BabyFeeNo2");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetBabyFeeNo2(NB_CHARNO, NB_FEENO2, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            BabyFeeNo2[] e = new BabyFeeNo2[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new BabyFeeNo2();
                e[i].NB_FEENO = dt.Rows[i]["ADMIT_NO"].ToString();
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }


    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetTraumaRate(string LH_START, string LH_END)
    {
        string sqlCombine = string.Empty;
        string sqlCombine2 = string.Empty;
        DataTable dtSql = new DataTable();
        DataTable dtSql2 = new DataTable();
        dtSql = GetSql.SqlLst("TraumaRate");
        dtSql2 = GetSql.SqlLst2("TraumaRate");
        if (dtSql.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql.Rows.Count; i++)
            {
                sqlCombine += dtSql.Rows[i]["fun_sql"];
            }
        }
        if (dtSql2.Rows.Count > 0)
        {
            for (int i = 0; i < dtSql2.Rows.Count; i++)
            {
                sqlCombine = sqlCombine.Replace("#" + dtSql2.Rows[i]["fun_arg_seq"].ToString().Trim() + "#", dtSql2.Rows[i]["fun_arg_con"].ToString());
            }
        }

        DataTable dt = new DataTable();
        dt = DAOPatient.GetTraumaRate(LH_START, LH_END, sqlCombine);
        if (dt.Rows.Count > 0)
        {
            TraumaRate[] e = new TraumaRate[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                e[i] = new TraumaRate();
                e[i].NO_OF_TRAUMA = Int32.Parse(dt.Rows[i]["NO_OF_TRAUMA"].ToString());
                e[i].NO_OF_LH =Int32.Parse(dt.Rows[i]["NO_OF_LH"].ToString());
            }
            byte[] result = CompressTool.CompressString(JsonConvert.SerializeObject(e));
            return result;
        }
        else
        {
            return null;
        }
    }


    private string GetPW1(string LK_UserAccount)
    {
        String WK_Nonce_str = LK_UserAccount;
        byte[] WK_Nonce_Byte = System.Text.Encoding.Default.GetBytes(WK_Nonce_str);
        String WK_Nonce_Base64 = Convert.ToBase64String(WK_Nonce_Byte);
        return WK_Nonce_Base64;
    }

    private string GetPW2(string LK_UserAccount, string LK_DateTimeNow, string LK_UserPW)
    {
        System.Security.Cryptography.SHA512 WK_SHA512 = new System.Security.Cryptography.SHA512CryptoServiceProvider();
        byte[] Combin_Byte = System.Text.Encoding.Default.GetBytes(LK_UserAccount + "1131090019" + LK_DateTimeNow + "MAYA" + LK_UserPW);
        byte[] WK_SHA512_Cryptp = WK_SHA512.ComputeHash(Combin_Byte);
        String WK_PW_Base64_SHA512 = Convert.ToBase64String(WK_SHA512_Cryptp);
        return WK_PW_Base64_SHA512;
    }

    private string GetDynamicHashKey(string lk_PrivateKey, string lk_TimeStamp)
    {
        System.Security.Cryptography.SHA512 WK_SHA512 = new System.Security.Cryptography.SHA512CryptoServiceProvider();
        byte[] Combin_Byte = System.Text.Encoding.Default.GetBytes(lk_PrivateKey + lk_TimeStamp);
        byte[] WK_SHA512_Cryptp = WK_SHA512.ComputeHash(Combin_Byte);
        String WK_PW_Base64_SHA512 = Convert.ToBase64String(WK_SHA512_Cryptp);
        return WK_PW_Base64_SHA512;
    }

    //DNR代碼對照
    private string GET_DNR_CODE_NAME(string DNR_CODE)
    {
        string DNR_NAME = "";
        switch (DNR_CODE)
        {
            case "1":
                DNR_NAME = "同意器官捐贈";
                break;
            case "2":
                DNR_NAME = "同意安寧和醫療";
                break;
            case "3":
                DNR_NAME = "同意不施行心肺復甦術";
                break;
            case "4":
                DNR_NAME = "同意器官捐贈|同意安寧和醫療|同意不施行心肺復甦術|同意不施行維生醫療";
                break;
            case "5":
                DNR_NAME = "同意器官捐贈|同意安寧和醫療";
                break;
            case "6":
                DNR_NAME = "同意器官捐贈|同意不施行心肺復甦術";
                break;
            case "7":
                DNR_NAME = "同意安寧和醫療|同意不施行心肺復甦術|同意不施行維生醫療";
                break;
            case "A":
                DNR_NAME = "同意不施行維生醫療";
                break;
            case "B":
                DNR_NAME = "同意器官捐贈|同意不施行維生醫療";
                break;
            case "C":
                DNR_NAME = "同意安寧和醫療|同意不施行維生醫療";
                break;
            case "D":
                DNR_NAME = "同意不施行心肺復甦術|同意不施行維生醫療";
                break;
            case "E":
                DNR_NAME = "同意器官捐贈|同意安寧和醫療|同意不施行心肺復甦術|同意不施行維生醫療";
                break;
            case "F":
                DNR_NAME = "同意器官捐贈|同意安寧和醫療|同意不施行維生醫療";
                break;
            case "G":
                DNR_NAME = "同意器官捐贈|同意不施行心肺復甦術|同意不施行維生醫療";
                break;
            case "H":
                DNR_NAME = "同意安寧和醫療|同意不施行心肺復甦術|同意不施行維生醫療";
                break;
            case "I":
                DNR_NAME = "同意器官捐贈|同意安寧和醫療|同意不施行心肺復甦術";
                break;
            case "J":
                DNR_NAME = "同意安寧和醫療|同意不施行心肺復甦術";
                break;
        }
        return DNR_NAME;
    }
    /// <summary>
    /// 語音紀錄LOG連動 其他WS新增功能 
    /// </summary>
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public string SetVoiceData(string JsonStr)
    {
        string sqlstr = string.Empty;
        string STATUS = "N";
        //string listJsonArray = CompressTool.DecompressString(data);
        string listJsonArray = JsonStr;
        VoiceCommand Vdata = JsonConvert.DeserializeObject<VoiceCommand>(listJsonArray);

        sqlstr = "insert into TALK_TABLE (PK_id, UserID, Action_Type, Controller, Fee_no,Start_Datetime, End_Datetime,";
        sqlstr += "Parameter, Parameter_CMD , Create_time, cellular_Phone ,Status, VOICE_PATH, ERROR_MSG) values('#PK_id#', '#UserID#', '#Action_Type#',";
        sqlstr += "'#Controller#', '#Fee_no#', TO_DATE('#Start_Datetime#', 'yyyy/mm/dd hh24:mi:ss' ), TO_DATE('#End_Datetime#', 'yyyy/mm/dd hh24:mi:ss' ), '#Parameter#', '#Parameter_CMD#', TO_DATE('#Create_time#', 'yyyy/mm/dd hh24:mi:ss' ), '#cellular_Phone#', '#Status#', '#VOICE_PATH#', '#ERROR_MSG#')";
        DateTime Nowdate = DateTime.Now;
        string returnStr = "";
        string PK_id = "";
        if (Vdata != null)
        {
            PK_id = Nowdate.ToString("yyyyMMddHHmmss") + "_" + Vdata.Fee_no;
            sqlstr = sqlstr.Replace("#PK_id#", PK_id);
            sqlstr = sqlstr.Replace("#UserID#", Vdata.UserId);
            sqlstr = sqlstr.Replace("#Action_Type#", Vdata.Action_Type);
            sqlstr = sqlstr.Replace("#Controller#", Vdata.Controller);
            sqlstr = sqlstr.Replace("#Fee_no#", Vdata.Fee_no);
            sqlstr = sqlstr.Replace("#Start_Datetime#", Convert.ToDateTime(Vdata.Start_Datetime).ToString("yyyy/MM/dd HH:mm:ss"));
            sqlstr = sqlstr.Replace("#End_Datetime#", Convert.ToDateTime(Vdata.End_Datetime).ToString("yyyy/MM/dd HH:mm:ss"));
            sqlstr = sqlstr.Replace("#Parameter#", Vdata.Parameter);
            sqlstr = sqlstr.Replace("#Parameter_CMD#", Vdata.Parameter_CMD);
            sqlstr = sqlstr.Replace("#Create_time#", Nowdate.ToString("yyyy/MM/dd HH:mm:ss"));
            sqlstr = sqlstr.Replace("#cellular_Phone#", Vdata.Cellular_Phone);
            sqlstr = sqlstr.Replace("#VOICE_PATH#", Vdata.VOICE_PATH);

            int WS_ReStr = 0;
            string errormsg = "";
            string Source = "";
            if (Vdata.Action_Type == "Insert")
            {
                switch (Vdata.Controller)
                {
                    case "TransferDuty/Task_List":
                        Source = "工作清單 ";
                        WS_ReStr = SetVoiceToTaskList(Vdata.Parameter_CMD, Vdata.UserId, Vdata.Fee_no);
                        break;
                    case "PhraseManagement/Phrase":
                        Source = "片語維護 ";
                        WS_ReStr = SetVoiceToPhrase(Vdata.Parameter_CMD, Vdata.UserId);
                        break;
                    case "Voice/Memo":
                        Source = "語音錄音檔 ";
                        WS_ReStr = 1;
                        break;
                    case "ConstraintsAssessment/ListNew":
                        Source = "約束評估語音內容 ";
                        WS_ReStr = 1;
                        break;
                    default:
                        WS_ReStr = 3;
                        break;
                }
            }

            switch (WS_ReStr)
            {
                case 0: //query成功
                    STATUS = "Y";
                    break;
                case 1: //新增成功
                    STATUS = "Y";
                    break;
                case 2:
                    errormsg += "CMD_Json內容有問題。";
                    break;
                case 3: //Insert功能中無該Controller功能
                    errormsg += "該新增語法未經授權";
                    break;
                case 9:
                    errormsg += "新增失敗，請與資訊室聯絡。";
                    break;
                case 11:
                    errormsg += "該病患於此時間無此工作項目";
                    break;
                case 12:
                    errormsg += "取消工作需有取消原因";
                    break;
                default:
                    errormsg += "不明原因造成新增失敗";
                    break;
            }
            sqlstr = sqlstr.Replace("#Status#", STATUS);
            sqlstr = sqlstr.Replace("#ERROR_MSG#", Source + errormsg);

            int returnNum = 0;
            returnNum = GetNISSql.Insert_VoiceData(sqlstr);
            switch (returnNum)
            {
                case 1:
                    if (string.IsNullOrEmpty(errormsg))
                    {
                        returnStr = "新增成功";
                    }
                    else
                    {
                        returnStr = errormsg;
                    }
                    break;
                case 9:
                    returnStr = "新增失敗，請與資訊室聯絡。";
                    break;
            }
        }
        else
        {
            returnStr = "Json內容有問題。";
        }
        return returnStr;

    }
    /// <summary>
    /// 語音工作清單新增WS 
    /// </summary>
    //[WebMethod]  //方法不對外開放
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public int SetVoiceToTaskList(string JsonStr, string userID, string feeno)
    {
        string sqlstr = string.Empty, sheet_no = string.Empty, order_content = string.Empty, Status = string.Empty, deleted = string.Empty;
        int returnStr = 0;
        DataTable Dt = new DataTable();
        DateTime Nowdate = DateTime.Now;
        //string listJsonArray = CompressTool.DecompressString(data);
        string listJsonArray = JsonStr;
        VoiceToTaskList Vdata = JsonConvert.DeserializeObject<VoiceToTaskList>(listJsonArray);
        string get_SheetNo_sql = "select SHEET_NO,ORDER_CONTENT,SET_PRIOD from DATA_SIGNORDER WHERE "
        + "FEE_NO = '#Fee_no#' AND SET_ACTION = '#SET_ACTION#' AND SET_PRIOD LIKE '%#SET_PRIOD#%' AND CANCELTIME is null AND ROWNUM =1";
        get_SheetNo_sql = get_SheetNo_sql.Replace("#Fee_no#", feeno);
        get_SheetNo_sql = get_SheetNo_sql.Replace("#SET_ACTION#", Vdata.Set_Action);
        get_SheetNo_sql = get_SheetNo_sql.Replace("#SET_PRIOD#", Vdata.Exec_Priod);
        Dt = GetNISSql.GetDt(get_SheetNo_sql);
        if (Dt.Rows.Count > 0)
        {
            sheet_no = Dt.Rows[0]["SHEET_NO"].ToString();
            order_content = Dt.Rows[0]["ORDER_CONTENT"].ToString();

            int rowct = 0;
            string sqlwherestr = " fee_no = '#fee_no#' AND sheet_no='#sheet_no#' AND EXEC_PRIOD= '#EXEC_PRIOD#' AND SET_ACTION= '#SET_ACTION#'";
            sqlwherestr += " AND to_char(EXEC_TIME,'yyyy/mm/dd') = '#EXEC_TIME#' AND DELETED is null";
            sqlwherestr = sqlwherestr.Replace("#fee_no#", feeno);
            sqlwherestr = sqlwherestr.Replace("#sheet_no#", sheet_no);
            sqlwherestr = sqlwherestr.Replace("#EXEC_PRIOD#", Vdata.Exec_Priod);
            sqlwherestr = sqlwherestr.Replace("#SET_ACTION#", Vdata.Set_Action);
            sqlwherestr = sqlwherestr.Replace("#EXEC_TIME#", Nowdate.ToString("yyyy/MM/dd"));

            string sql_count = "select count(*) as ct from DATA_TASK_EXEC_RECORD where " + sqlwherestr;
            DataTable Dt_count = GetNISSql.GetDt(sql_count);
            if (Dt_count.Rows.Count > 0)
            {
                rowct = int.Parse(Dt_count.Rows[0]["ct"].ToString());
                if (rowct > 0)//判斷該天該時間該工作是否有資料，有則進行刪除
                {
                    sql_count = "update DATA_TASK_EXEC_RECORD set DELETED = '#DELETED#'";
                    sql_count += "WHERE " + sqlwherestr;
                    sql_count = sql_count.Replace("#DELETED#", userID);
                    returnStr = GetNISSql.Insert_VoiceData(sql_count);
                }
            }

            sqlstr = "insert into DATA_TASK_EXEC_RECORD (FEE_NO, SHEET_NO, SET_ACTION, EXEC_PRIOD, EXEC_TIME,EXEC_USER, EXEC_REASON,";
            sqlstr += "EXEC_RESULT, DELETED, ORDER_CONTENT) values('#FEE_NO#', '#SHEET_NO#', '#SET_ACTION#',";
            sqlstr += "'#EXEC_PRIOD#', TO_DATE('#EXEC_TIME#', 'yyyy/mm/dd hh24:mi:ss' ), '#EXEC_USER#', '#EXEC_REASON#', '#EXEC_RESULT#', '#DELETED#', '#ORDER_CONTENT#')";
            if (Vdata != null)
            {
                Status = Vdata.Exec_Result;
                if (string.IsNullOrEmpty(Vdata.Exec_Reason) && (Status == "C" || Status == "ALL"))
                {
                    returnStr = 12;//取消工作需有取消原因
                    return returnStr;
                }
                if (Status == "ALL")
                {
                    string update_sql = "";
                    update_sql = "update DATA_SIGNORDER set CANCELTIME =TO_DATE('#CANCELTIME#', 'yyyy/mm/dd hh24:mi:ss' )";
                    update_sql += "WHERE SHEET_NO = '#SHEET_NO#' AND FEE_NO = '#FEE_NO#'";
                    update_sql = update_sql.Replace("#CANCELTIME#", Nowdate.ToString("yyyy/MM/dd HH:mm:ss"));
                    update_sql = update_sql.Replace("#SHEET_NO#", sheet_no);
                    update_sql = update_sql.Replace("#FEE_NO#", feeno);
                    returnStr = GetNISSql.Insert_VoiceData(update_sql);

                    Status = "D";
                }

                sqlstr = sqlstr.Replace("#FEE_NO#", feeno);
                sqlstr = sqlstr.Replace("#SHEET_NO#", sheet_no);
                sqlstr = sqlstr.Replace("#SET_ACTION#", Vdata.Set_Action);
                sqlstr = sqlstr.Replace("#EXEC_PRIOD#", Vdata.Exec_Priod);
                sqlstr = sqlstr.Replace("#EXEC_TIME#", Nowdate.ToString("yyyy/MM/dd HH:mm:ss"));
                sqlstr = sqlstr.Replace("#EXEC_USER#", userID);
                sqlstr = sqlstr.Replace("#EXEC_REASON#", Vdata.Exec_Reason);
                sqlstr = sqlstr.Replace("#EXEC_RESULT#", Status);
                sqlstr = sqlstr.Replace("#DELETED#", deleted);
                sqlstr = sqlstr.Replace("#ORDER_CONTENT#", order_content);
            }
            else
            {
                returnStr = 2;//Json有問題
                return returnStr;
            }


            returnStr = GetNISSql.Insert_VoiceData(sqlstr);
        }
        else
        {
            returnStr = 11;//該病患無此工作項目
        }

        return returnStr;

    }

    /// <summary>
    /// 語音個人片語新增WS 
    /// </summary>
    //[WebMethod]  //方法不對外開放
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public int SetVoiceToPhrase(string JsonStr, string userID)
    {
        string sqlstr = string.Empty, sheet_no = string.Empty, order_content = string.Empty, deleted = string.Empty;
        int returnStr = 0;
        DataTable Dt = new DataTable();
        DateTime Nowdate = DateTime.Now;
        string listJsonArray = JsonStr;
        VoicePhrase Vdata = JsonConvert.DeserializeObject<VoicePhrase>(listJsonArray);

        string get_nodeid_sql = "select NODEID from PHRASE_NODE WHERE "
        + "CREANO = '#CREANO#' AND NAME = '#NAME#' AND PHRASE_TYPE = 'self' AND ROWNUM =1";
        get_nodeid_sql = get_nodeid_sql.Replace("#CREANO#", userID);
        get_nodeid_sql = get_nodeid_sql.Replace("#NAME#", "語音片語");
        Dt = GetNISSql.GetDt(get_nodeid_sql);
        string NodeID = string.Empty;
        if (Dt.Rows.Count > 0)
        {
            NodeID = Dt.Rows[0]["NODEID"].ToString();
        }
        else
        {
            sqlstr = "insert into PHRASE_NODE (NODEID,CREANO,NAME,DEPTH,PARENT_NODE,PHRASE_TYPE";
            sqlstr += ") values(#NODEID#, '#CREANO#', '#NAME#','1', '0', 'self')";
            sqlstr = sqlstr.Replace("#NODEID#", Nowdate.ToString("yyyyMMddHHmmss"));
            sqlstr = sqlstr.Replace("#CREANO#", userID);
            sqlstr = sqlstr.Replace("#NAME#", "語音片語");
            returnStr = GetNISSql.Insert_VoiceData(sqlstr);
            if (returnStr == 9)
            {
                return returnStr;
            }
            Dt = GetNISSql.GetDt(get_nodeid_sql);
            if (Dt.Rows.Count > 0)
            {
                NodeID = Dt.Rows[0]["NODEID"].ToString();
            }
        }
        sqlstr = "insert into PHRASE_DATA (DATA_ID,NODE_ID,NAME,CREANO,C,S,O,I,E";
        sqlstr += ") values('#DATA_ID#', #NODEID#, '#NAME#', '#CREANO#', '#C#', '#S#', '#O#', '#I#', '#E#')";
        if (Vdata != null)
        {
            sqlstr = sqlstr.Replace("#DATA_ID#", "PHRASE_DATA" + userID + "__" + Nowdate.ToString("yyyyMMddHHmmssffff") + "_0");
            sqlstr = sqlstr.Replace("#NODEID#", NodeID);
            sqlstr = sqlstr.Replace("#NAME#", Vdata.Phrase_Name);
            sqlstr = sqlstr.Replace("#CREANO#", userID);
            sqlstr = sqlstr.Replace("#C#", Vdata.C_str);
            sqlstr = sqlstr.Replace("#S#", Vdata.S_str);
            sqlstr = sqlstr.Replace("#O#", Vdata.O_str);
            sqlstr = sqlstr.Replace("#I#", Vdata.I_str);
            sqlstr = sqlstr.Replace("#E#", Vdata.E_str);
        }
        else
        {
            returnStr = 2;//Json有問題
            return returnStr;
        }
        returnStr = GetNISSql.Insert_VoiceData(sqlstr);

        return returnStr;

    }

    /// <summary>
    /// APP病患瀏覽語音資料 
    /// </summary>
    [WebMethod]  //方法不對外開放
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public byte[] GetVoiceEducationToApp(string fee_no)
    {
        string sqlstr = string.Empty, json_str = string.Empty;
        byte[] result = null;
        DataTable Dt = new DataTable();
        DateTime Nowdate = DateTime.Now;
        List<VoiceCommand> VoiceCommand = new List<VoiceCommand>();

        string get_edudata_sql = "select CREATE_TIME,PARAMETER_CMD,PARAMETER,VOICE_PATH from Talk_Table WHERE "
        + "FEE_NO = '#FEE_NO#' AND CONTROLLER = '#CONTROLLER#' AND STATUS ='Y'";
        get_edudata_sql = get_edudata_sql.Replace("#FEE_NO#", fee_no);
        get_edudata_sql = get_edudata_sql.Replace("#CONTROLLER#", "Voice/Memo");
        Dt = GetNISSql.GetDt(get_edudata_sql);
        if (Dt.Rows.Count > 0)
        {
            VoiceCommand = (List<VoiceCommand>)Dt.ToList<VoiceCommand>();
            //json_str = JsonConvert.SerializeObject(VoiceCommand);
            result = CompressTool.CompressString(JsonConvert.SerializeObject(VoiceCommand));
        }


        return result;
    }
}
