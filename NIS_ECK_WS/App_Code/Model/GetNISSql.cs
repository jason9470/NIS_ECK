using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using NIS;

/// <summary>
/// GetSql 的摘要描述
/// </summary>
public class GetNISSql
{
    OracleAgent m_dbAgency = new OracleAgent();

    public GetNISSql()
	{
		//
		// TODO: 在此加入建構函式的程式碼
		//
	}

    /// <summary>
    /// 取得需要使用的SQL
    /// </summary>
    /// <returns></returns>
    public DataTable SqlLst(string FunName)
    {
        string sql = string.Empty;
        sql += "select fun_sql from nis_websql_bak where trim(fun_name)='" + FunName + "' order by fun_seq";

        return m_dbAgency.GetDataTable(sql);
    }

    public DataTable SqlLst2(string FunName)
    {
        string sql = string.Empty;
        sql += "select fun_arg_seq,fun_arg_con from nis_websql_dtl where trim(fun_name)='" + FunName + "' order by fun_arg_seq";

        return m_dbAgency.GetDataTable(sql);
    }

    public DataTable SqlMed(string FunName)
    {
        return m_dbAgency.GetDataTable(FunName);
    }

    public int Insert_VoiceData(string sqlstr)
    {
        int num = m_dbAgency.DBInsertData(sqlstr);
        int returnStr = 0;
        if (num > 0)
        {
            returnStr = 1;
        }
        else
        {
            returnStr = 9;
        }
        return returnStr;
    }
    public DataTable GetDt(string sqlstr)
    {
        return m_dbAgency.GetDataTable(sqlstr);
    }
}