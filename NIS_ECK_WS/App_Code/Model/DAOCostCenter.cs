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
public class DAOCostCenter
{
    OracleAgent m_dbAgency = new OracleAgent();

    public DAOCostCenter()
	{
		//
		// TODO: 在此加入建構函式的程式碼
		//
	}


    /// 成本中心列表
    /// </summary>
    /// <param name="someDate"></param>
    /// <returns></returns>
    public DataTable GetCostCenterList(string sqlCombine)
    {
        string sql = "";
        sql += sqlCombine;
        return m_dbAgency.GetDataTable(sql);
    }

    /// 出院三日(含)內病患清單(含系統日當天)
    /// </summary>
    /// <param name="CostCode"></param>
    /// <returns></returns>
    public DataTable Out3Days(string CostCode, string DRIVE_ID, string DRIVE_ID_2, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#cccode#", CostCode);
        sql = sql.Replace("#driveid#", DRIVE_ID);
        sql = sql.Replace("#driveid_2#", DRIVE_ID_2);
        return m_dbAgency.GetDataTable(sql);
    }

    /// 頻次表
    /// </summary>
    /// <returns></returns>
    public DataTable GetFre(string sqlCombine)
    {
        string sql = "";
        sql += sqlCombine;
        return m_dbAgency.GetDataTable(sql);
    }

    /// 點滴清單
    /// </summary>
    /// <returns></returns>
    public DataTable GetIVFList(string sqlCombine)
    {
        string sql = "";
        sql += sqlCombine;
        return m_dbAgency.GetDataTable(sql);
    }

    ///全院病房/床號清單
    /// </summary>
    /// <returns></returns>
    public DataTable GetBedList(string CostCode, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#COSTCODE#", CostCode);
        return m_dbAgency.GetDataTable(sql);
    }

    public DataTable GetINSPTList(string sqlCombine)
    {
        string sql = "";
        sql += sqlCombine;
        return m_dbAgency.GetDataTable(sql);
    }
}
