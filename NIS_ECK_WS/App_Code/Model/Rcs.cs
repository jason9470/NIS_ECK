using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using DSHealth;
using NIS;
using Newtonsoft.Json;

namespace RCS.Data
{
    [JsonObject(MemberSerialization.OptOut)]
    public class RCSConsultList
    {
        
        [JsonProperty]
        public string START_DATE { get; set; }
        [JsonProperty]
        public string START_TIME { get; set; }
        [JsonProperty]
        public string BED_NO { get; set; }
        [JsonProperty]
        public string CHART_NO { get; set; }
        [JsonProperty]
        public string PT_NAME { get; set; }
        [JsonProperty]
        public string DIV_NO { get; set; }
        [JsonProperty]
        public string DIV_SHORT_NAME { get; set; }
        [JsonProperty]
        public string VS_NO { get; set; }
        [JsonProperty]
        public string DOCTOR_NAME { get; set; }
        [JsonProperty]
        public string ADMIT_NO { get; set; }
        [JsonProperty]
        public string CUT_NO { get; set; }
    }
    public class RCSERAndOpdPTList
    {

        [JsonProperty]
        public string CHART_NO { get; set; }
        [JsonProperty]
        public string PT_NAME { get; set; }
        [JsonProperty]
        public string SEX { get; set; }
        [JsonProperty]
        public string BIRTH_DATE { get; set; }
        [JsonProperty]
        public string START_DATE { get; set; }
        [JsonProperty]
        public string START_TIME { get; set; }
        [JsonProperty]
        public string END_DATE { get; set; }
        [JsonProperty]
        public string END_TIME { get; set; }
        [JsonProperty]
        public string BED_NO { get; set; }
        [JsonProperty]
        public string CLINIC_FLAG { get; set; }
        [JsonProperty]
        public string PKEY { get; set; }
        [JsonProperty]
        public string COURSE { get; set; }
        public string ID_NO { get; set; }

    }

    public class RCSPtDrugList
    {
        [JsonProperty]
        public string FEE_NO { get; set; }
        [JsonProperty]
        public string CHR_NO { get; set; }
        [JsonProperty]
        public string COST_CODE { get; set; }
        [JsonProperty]
        public string BED_NO { get; set; }
        [JsonProperty]
        public string DRUG_CODE { get; set; }
        [JsonProperty]
        public string DRUG_NAME { get; set; }
        [JsonProperty]
        public string START_DATE { get; set; }
        [JsonProperty]
        public string START_TIME { get; set; }
        [JsonProperty]
        public string END_DATE { get; set; }
        [JsonProperty]
        public string END_TIME { get; set; }
        [JsonProperty]
        public string DOSE_QTY { get; set; }
        [JsonProperty]
        public string DOSE_UNIT { get; set; }
        [JsonProperty]
        public string FREQUENCY_CODE { get; set; }
        [JsonProperty]
        public string METHOD_CODE { get; set; }

    }

}


/// <summary>
/// Rcs 的摘要描述
/// </summary>
public class Rcs
{
    OracleAgent m_dbAgency = new OracleAgent();
    public Rcs()
    {
        //
        // TODO: 在這裡新增建構函式邏輯
        //
    }

    

    public DataTable GetRCSConsultList(string sqlCombine)
    {
        string sql = "";
        sql += sqlCombine;
        return m_dbAgency.GetDataTable(sql);
    }

    public DataTable ERAndOpdPTListByChartNo(string sqlCombine, string drive_id, string drive_id_1, string chart_no)
    {
        string sql = sqlCombine.Replace("#CHART_NO#", chart_no);
        sql = sql.Replace("#DRIVE_ID#", drive_id);
        sql = sql.Replace("#DRIVE_ID_1#", drive_id_1);

        return m_dbAgency.GetDataTable(sql);
    }

    public DataTable GetRCSPtDrugList(string FeeNo, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#FeeNo#", FeeNo);
        return m_dbAgency.GetDataTable(sql);
    }



    public string GetDriveID(string drive_id, int year, int month)
    {
        switch (year % 5)
        {
            case 1:
                drive_id += month.ToString("00");
                break;
            case 2:
                drive_id += "B" + month.ToString("00");
                break;
            case 3:
                drive_id += "C" + month.ToString("00");
                break;
            case 4:
                drive_id += "D" + month.ToString("00");
                break;
            case 0:
                drive_id += "E" + month.ToString("00");
                break;
        }
        return drive_id;
    }

    public string GetADDate(string chinese_date)
    {
        string wk_date = "";
        string wk_str = "";
        wk_str = int.Parse(chinese_date).ToString("0000000");
        if (wk_str != "0000000")
        {
            wk_date = (int.Parse(wk_str.Substring(0, 3)) + 1911).ToString() + wk_str.Substring(3);
        }
        else
        {
            wk_date = "00000000";
        }

        return wk_date;
    }


}