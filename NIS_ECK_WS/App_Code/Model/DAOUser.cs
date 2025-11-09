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
public class DAOUser
{
    OracleAgent m_dbAgency = new OracleAgent();

    public DAOUser()
    {
        //
        // TODO: 在此加入建構函式的程式碼
        //
    }

    /// <summary>
    /// 取得單位人員
    /// </summary>
    /// <param name="costcentercode">成本中心代碼</param>
    /// <returns></returns>
    public DataTable UserList(string costcentercode, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#costcentercode#", costcentercode);
        return m_dbAgency.GetDataTable(sql);
    }

    /// <summary>
    /// 使用者登錄查詢
    /// </summary>
    /// <param name="employeesno">員編</param>
    /// <param name="employeespwd">密碼</param>
    /// <returns></returns>
    public DataTable UserLogin(string employeesno, string employeespwd, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#employeesno#", employeesno);
        sql = sql.Replace("#employeespwd#", employeespwd);
        return m_dbAgency.GetDataTable(sql);
    }

    /// <summary>
    /// 取得使用者姓名
    /// </summary>
    /// <param name="employeesno">員編</param>
    /// <returns></returns>
    public DataTable UserName(string employeesno, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#employeesno#", employeesno);
        return m_dbAgency.GetDataTable(sql);
    }

    /// <summary>
    /// 班表查詢
    /// </summary>
    /// <param name="JobDate">時間</param>
    /// <param name="Shift_cate">班別 D:白班 N:小夜 E:大夜</param>
    /// <param name="costcode">成本中心代碼</param>
    /// <returns></returns>
    public DataTable GetShift(String JobDate, String Shift_cate, string costcode, string sqlCombine)
    {
        if (JobDate.ToString() == "")
            JobDate = DateTime.Now.ToString("yyyyMMdd");
        string sql = sqlCombine.Replace("#jobdate#", JobDate);
        sql = sql.Replace("#shift_cate#", Shift_cate);
        sql = sql.Replace("#costcode#", costcode.Trim());
        return m_dbAgency.GetDataTable(sql);
    }

    /// <summary>
    /// 身份證號碼轉員編
    /// </summary>
    /// <param name="PatientID">身份證號碼</param>
    /// <returns></returns>
    public DataTable GetIDToEmp(string PatientID, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#idno#", PatientID);
        return m_dbAgency.GetDataTable(sql);
    }


    /// <summary>
    /// 取得有效執導者
    /// </summary>
    public DataTable GetSigner(String costcentercode, string sqlCombine)
    {
        string sql = sqlCombine.Replace("#costcentercode#", costcentercode);
        return m_dbAgency.GetDataTable(sql);
    }


}
