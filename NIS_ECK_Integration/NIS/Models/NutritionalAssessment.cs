using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NIS.Models;
using System.Data;
using System.Data.OleDb;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace NIS.Models
{
    public class NutritionalAssessment : DBConnector
    {

        public DataTable sel_nutritional_data(string feeno, string id, string nutatype, string St = "", string Ed = "")
        {
            DataTable dt = new DataTable();

            string sql = "SELECT A.*, (TO_CHAR(A.NUTA_ASSESSMENT_DTM,'yyyy/mm/dd')) AS ASSDATE, (TO_CHAR(A.NUTA_ASSESSMENT_DTM,'hh24:mi')) AS ASSTIME FROM NUTRITIONAL_ASSESSMENT A WHERE DELETED='N' ";

            if (feeno != "")
                sql += "AND A.FEENO='" + feeno + "' ";
            if (id != "")  //抓取單筆時使用id
                sql += "AND A.NUTA_ID='" + id + "' ";
            if (nutatype != "")  //判斷 成人/兒童 之評估
                sql += "AND A.NUTA_TYPE='" + nutatype + "' ";
            if (St != "" && Ed != "")
            {
                sql += "AND (A.NUTA_ASSESSMENT_DTM BETWEEN to_date('" + Convert.ToDateTime(St).ToString("yyyy/MM/dd 00:00:00") + "','yyyy/mm/dd hh24:mi:ss') ";
                sql += "AND to_date('" + Convert.ToDateTime(Ed).ToString("yyyy/MM/dd 23:59:59") + "','yyyy/mm/dd hh24:mi:ss') ) ";                
            }

            sql += "ORDER BY A.NUTA_ASSESSMENT_DTM ASC ";

            base.DBExecSQL(sql, ref dt);

            return dt;
        }

        public static String SQLString(String ColString, bool IsRegex = false, bool Num = false)
        {
            if (ColString != null)
            {
                if (IsRegex)
                {
                    //   \ to \\
                    ColString = Regex.Replace(ColString, "[^\\w\\~～ 。@!！$＄%％^︿&＆*＊(（）)'＼／’]", "");
                }

                int _find = ColString.IndexOf("'");
                if (_find >= 0)
                {
                    ColString = ColString.Replace("'", "''");

                }
                return "'" + ColString.ToString().Trim() + "'";
            }
            else
            {
                if (Num) return "0";
                return "null";
            }
        }

        //將DataTable轉成JsonArray
        public static string DatatableToJsonArray(DataTable pDt)
        {
            return JsonConvert.SerializeObject(pDt, Formatting.Indented);
        }


        public string ReturnVitalSignData(string feeno, string vs_item)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM (SELECT * FROM DATA_VITALSIGN WHERE 0 = 0 ";
            sql += "AND FEE_NO = '" + feeno + "' AND VS_ITEM='" + vs_item + "' order by modify_date desc) where  rownum <=1";
            base.DBExecSQL(sql, ref dt);

            if(dt != null && dt.Rows.Count > 0)
            {
                return dt.Rows[0]["VS_RECORD"].ToString();
            }
            else
            {
                return "";
            }
        }
    }
}
