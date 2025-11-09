using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Collections;

/// <summary>
/// CustomClass 的摘要描述
/// </summary>
public class CustomClass
{
	public CustomClass()
	{
		//
		// TODO: 在這裡新增建構函式邏輯
		//
	}

    /// <summary> HashTable轉換為DataTable</summary>
    /// <remarks>
    /// </remarks>
    /// <parameter>HashTable</parameter>
    /// <returns>Datatable</returns>     
    public static DataTable HashTableToDataTable(Hashtable ht)
    {
        try
        {
            //創建DataTable
            DataTable dt = new DataTable();
            //創建新列
            DataColumn dc1 = dt.Columns.Add("CostCenterCode", typeof(string));
            DataColumn dc2 = dt.Columns.Add("CCCDescription", typeof(string));

            //將HashTable中的值添加到DataTable中
            foreach (DictionaryEntry element in ht)
            {
                DataRow dr = dt.NewRow();
                dr["CostCenterCode"] = (string)element.Value;
                dr["CCCDescription"] = (string)element.Key;

                dt.Rows.Add(dr);
            }

            return dt;
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
}