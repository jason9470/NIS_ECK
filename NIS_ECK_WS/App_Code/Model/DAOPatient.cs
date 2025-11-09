using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using DSHealth;
using NIS;

/// <summary>
/// DAOAccount 的摘要描述
/// </summary>
public class DAOPatient
{
    OracleAgent m_dbAgency = new OracleAgent();

    public DAOPatient()
    {
        //
        // TODO: 在此加入建構函式的程式碼
        //
    }

    /// 取得患者清單
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetPatientList(string CCCode, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#cccode#", CCCode);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 取得患者清單by床位清單
    /// </summary>
    /// <param name="bedList"></param>
    /// <param name="sqlCombine"></param>
    /// <returns></returns>
    public DataTable GetPatientList(string[] bedList, string sqlCombine)
    {
        string BedList = "'" + string.Join("','", bedList) + "'";
        string sql = sqlCombine.Replace("#bedlist#", BedList);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 取得患者資訊
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetPatientInfo(string FeeNo, string DriveID, string DriveID2, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FeeNo);
        sql = sql.Replace("#driveid#", DriveID);
        sql = sql.Replace("#driveid2#", DriveID2);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 取得患者是否有特殊註記資訊
    /// </summary>
    /// <returns></returns>
    public DataTable GetPatientNote(string FeeNo, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FeeNo);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 預計出院床號清單
    /// </summary>
    /// <returns></returns>
    public DataTable GetExLeaveHospital( string sqlCombine)
    {
        return m_dbAgency.GetDataTable(sqlCombine);
    }
    
   
    /// 取得患者最近一次轉床時間
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetPatientTrans(string FeeNo,string DriveID, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FeeNo);
        sql = sql.Replace("#driveid#", DriveID);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 取得患者最近一次轉床時間
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetPatientDisDate(string FeeNo, string DriveID, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FeeNo);
        sql = sql.Replace("#driveid#", DriveID);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 取得患者會診
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetPatientConsult(string FeeNo, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FeeNo);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 取得患者手術
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetPatientOp(string FeeNo, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FeeNo);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 最後一次門診用藥
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetOpdMed(string FeeNo, string DriveID, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FeeNo);
        sql = sql.Replace("#driveid#", DriveID);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 最後一次門診用藥
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetOpdDate(string FeeNo, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FeeNo);
        return m_dbAgency.GetDataTable(sql);
    }
    

    /// 出院帶藥
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetUdOrderD(string FeeNo, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FeeNo);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 取得病患血型. 備血日期. 備血量. 血種. 餘血量(備血-領血)
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetBloodInfo(string FeeNo, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FeeNo);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 取得病患藥品.食物.其他過敏清單
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetAllergyList(string FeeNo, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FeeNo);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 取得患者基本資料
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetPatientBasic(string FeeNo)
    {
        string sql = "";
        sql += " SELECT * ";
        sql += " FROM PatientBasic";
        sql += " Where FeeNo = '" + FeeNo + "'";
        return m_dbAgency.GetDataTable(sql);
    }

    /// 取得患者住院歷程
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetInHistory(string PatientNo, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#patientno#", PatientNo);
        return m_dbAgency.GetDataTable(sql);
    }



    /// 取得營養醫囑
    public DataTable GetDietOrder(string FeeNo,  string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FeeNo);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 取得文字醫囑
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetTextOrder(string FeeNo,string DRIVE_ID, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FeeNo);
        sql = sql.Replace("#driveid#", DRIVE_ID);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 取得文字醫囑個項目是否有未簽 
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetTextOrderItem(string FeeNo, string DRIVE_ID, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FeeNo);
        sql = sql.Replace("#driveid#", DRIVE_ID);
        return m_dbAgency.GetDataTable(sql);
    }


    /// 取得STAT文字醫囑
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetSTextOrder(string FeeNo, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FeeNo);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 取得患者藥囑
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetDrugOrder(string FeeNo, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FeeNo);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 取得藥品醫囑個項目是否有未簽 
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetDrugOrderItem(string FeeNo, string DriveID, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FeeNo);
        sql = sql.Replace("#driveid#", DriveID);
        return m_dbAgency.GetDataTable(sql);
    }
    


    /// 取得患者藥囑
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetIpdDrugOrder(string FeeNo, string DriveID, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FeeNo);
        sql = sql.Replace("#driveid#", DriveID);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 取得患者藥囑(展藥後)
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetUdOrder(string FeeNo, string DriveID, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FeeNo);
        sql = sql.Replace("#driveid#", DriveID);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 取得患者下次注射部位
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetPosition(string FeeNo, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FeeNo);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 取得患者化療藥囑
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetChemotherapyDrugOrder(string FeeNo, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FeeNo);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 取得患者處置
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetProcedure(string FeeNo, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FeeNo);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 取得患者檢驗
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetLab(string FeeNo, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FeeNo);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 取得患者檢驗
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetLabbyDate(string FeeNo, string Sta_Date, string End_Date, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FeeNo);
        sql = sql.Replace("#StartDate#", Sta_Date);
        sql = sql.Replace("#EndDate#", End_Date);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 取得患者產檢項目
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetLabBorn(string FeeNo, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FeeNo);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 取得患者檢查
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetExam(string FeeNo, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FeeNo);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 取得患者檢查
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetExambyDate(string FeeNo, string Sta_Date, string End_Date, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FeeNo);
        sql = sql.Replace("#StartDate#", Sta_Date);
        sql = sql.Replace("#EndDate#", End_Date);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 取得患者 Chemotherapy 結果
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetChemotherapy(string FeeNo, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FeeNo);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 取得患者所需施打抗生素
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetAntibiotics(string FeeNo, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FeeNo);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 床號轉批價序號
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable BedNoTransformFeeNo(string BedNo, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#bedno#", BedNo);
        return m_dbAgency.GetDataTable(sql);
    }
    public DataTable BedNoTransformFeeNoWithCostCode(string BedNo, string CostCode, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#bedno#", BedNo);
        sql = sql.Replace("#costcode#", CostCode);
        return m_dbAgency.GetDataTable(sql);
    }
    /// 取得藥品資訊
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetMedicalInfo(string MedicalNo, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#medicalno#", MedicalNo);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 取得藥品資訊
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetDrugPicPath(string MedicalNo, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#medicalno#", MedicalNo);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 新醫囑
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetNewOrderFlag(string sqlCombine)
    {
        string sql = "";
        sql += sqlCombine;
        return m_dbAgency.GetDataTable(sql);
    }

    /// 新醫囑 (update)
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public int UpdNewOrderFlag(string FeeNo, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FeeNo);
        return m_dbAgency.ExeSqlTransaction(sql);
    }

    /// 取得病患轉床紀錄
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetBedTransList(string FeeNo, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FeeNo);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 取得病患轉床紀錄2
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetTransBedDetail(string CostCode, string BeginDate, string EndDate, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#CostCode#", CostCode);
        sql = sql.Replace("#BeginDate#", BeginDate);
        sql = sql.Replace("#EndDate#", EndDate);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 取得病患轉床紀錄3
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetTransBedDetail2(string BeginDate, string EndDate, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#BeginDate#", BeginDate);
        sql = sql.Replace("#EndDate#", EndDate);
        return m_dbAgency.GetDataTable(sql);
    }

    /// <summary>
    /// 新版轉床歷程
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetBed_Trans(string CostCode, string BeginDate, string EndDate, string sqlCombine)
    {
        string sql = string.Empty;
        if (CostCode != "")
            sql = sqlCombine.Replace("#COSTCODE#", CostCode);
        else
            sql = sqlCombine.Replace("cost_code = '#COSTCODE#' and ", " ");
        sql = sql.Replace("#BEGINDATE#", BeginDate);
        sql = sql.Replace("#ENDDATE#", EndDate);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 取得患者VitalSign
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetVitalSign(string FeeNo, string Sta_Date, string End_Date, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#FeeNo#", FeeNo);
        sql = sql.Replace("#StartDate#", Sta_Date);
        sql = sql.Replace("#EndDate#", End_Date);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 取得交班單預計項目數值
    /// </summary>
    /// <returns></returns>
    public DataTable GetExpectedItem(string FeeNo,  string sqlCombine)
    {
        string sql = sqlCombine.Replace("#FeeNo#", FeeNo);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 取得TPR抗生素
    /// </summary>
    /// <returns></returns>
    public DataTable GetTPRAantibiotic(string FeeNo, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#FeeNo#", FeeNo);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 取得TriageInfo
    /// </summary>
    /// <returns></returns>
    public DataTable GetTriageInfo(string FeeNo, string DriveID, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FeeNo);
        sql = sql.Replace("#driveid#", DriveID);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 取得GetERListAll
    /// </summary>
    /// <returns></returns>
    public DataTable GetERListAll(string sqlCombine)
    {
        return m_dbAgency.GetDataTable(sqlCombine);
    }

    /// 取得ERListbyDate
    /// </summary>
    /// <returns></returns>
    public DataTable ERListbyDate(string sqlCombine, string start_date, string end_date)
    {
        string sql = sqlCombine.Replace("#STR_DATE#", start_date);
        sql = sql.Replace("#END_DATE#", end_date);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 取得ERListbyChartNo
    /// </summary>
    /// <returns></returns>
    public DataTable ERListbyChartNo(string sqlCombine, string DriveID, string CHART_NO, string CLINIC_DATE,string DUPLICATE_NO,string status)
    {
        string sql = sqlCombine.Replace("#CHARTNO#", CHART_NO);
        sql = sql.Replace("#driveid#", DriveID);
        sql = sql.Replace("#CLINIC_DATE#", CLINIC_DATE);
        sql = sql.Replace("#DUPLICATE_NO#", DUPLICATE_NO);
        sql = sql.Replace("#STATUS#", status);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 取得OpdDateByChartNo
    /// </summary>
    /// <returns></returns>
    public DataTable OpdDateByChartNo(string sqlCombine, string CHART_NO)
    {
        string sql = sqlCombine.Replace("#CHARTNO#", CHART_NO);
        return m_dbAgency.GetDataTable(sql);
    }
    

    /// 取得ER_WOUND
    /// </summary>
    /// <returns></returns>
    public DataTable ER_WOUND(string ER_SERIAL_NO, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#SERIALNO#", ER_SERIAL_NO);
        return m_dbAgency.GetDataTable(sql);
    }

     public DataTable GetTextOrderRecord(string FEENO, string DriveID, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FEENO);
        sql = sql.Replace("#driveid#", DriveID);
        return m_dbAgency.GetDataTable(sql);
    }

    public DataTable GetERConsultation(string FEENO, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FEENO);
        return m_dbAgency.GetDataTable(sql);
    }

    public DataTable GetTPRTBilHCT(string FEENO, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#FeeNo#", FEENO);
        return m_dbAgency.GetDataTable(sql);
    }

    public DataTable GetLabErrorFlag(string CHARTNO, string LABCODE, string STADATE, string ENDDATE, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#chartno#", CHARTNO);
        sql = sql.Replace("#labcode#", LABCODE);
        sql = sql.Replace("#startdate#", STADATE);
        sql = sql.Replace("#enddate#", ENDDATE);
        return m_dbAgency.GetDataTable(sql);
    }


    public DataTable GetBabyLab(string FEE_NO, string Sta_Date, string End_Date, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FEE_NO);
        sql = sql.Replace("#StartDate#", Sta_Date);
        sql = sql.Replace("#EndDate#", End_Date);
        return m_dbAgency.GetDataTable(sql);
    }

    public DataTable GetPatInfo(string IDNO, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#IDNO#", IDNO);
        return m_dbAgency.GetDataTable(sql);
    }

    public DataTable GetPatInfo1(string CHARTNO, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#CHARTNO#", CHARTNO);
        return m_dbAgency.GetDataTable(sql);
    }

    public DataTable GetBaByPatInfo(string FEENO, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#FEENO#", FEENO);
        return m_dbAgency.GetDataTable(sql);
    }

    public DataTable GetBaByMatInfo(string IDNO, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#IDNO#", IDNO);
        return m_dbAgency.GetDataTable(sql);
    }
    
    public DataTable GetSDM(string CHARTNO, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#CHARTNO#", CHARTNO);
        return m_dbAgency.GetDataTable(sql);
    }

    public DataTable GetBABYLIKN(string FEENO, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FEENO);
        return m_dbAgency.GetDataTable(sql);
    }

    public DataTable GetBirthTrauma(string FEENO, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FEENO);
        return m_dbAgency.GetDataTable(sql);
    }

    public DataTable GetBabyFeeNo2(string CHARTNO, string FEENO, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#feeno#", FEENO);
        sql = sql.Replace("#chartno#", CHARTNO);
        return m_dbAgency.GetDataTable(sql);
    }

    public DataTable GetTraumaRate(string START, string END, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#LH_START#", START);
        sql = sql.Replace("#LH_END#", END);
        return m_dbAgency.GetDataTable(sql);
    }
   


}
