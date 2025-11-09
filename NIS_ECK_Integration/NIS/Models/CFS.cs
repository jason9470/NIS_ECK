using System.Web;
using System.Web.Mvc;
using NIS.Models;
using System.Data;
using System.Data.OleDb;

namespace NIS.Models
{
    public class CFS: DBConnector
    {
        public DataTable sel_cfs_record_img(string feeno, string record_id)
        {//各部位之評估 最新一筆列表 
            DataTable dt = new DataTable();

            string sql = " SELECT CLOCK_FILE";
            sql += " FROM CFS_DATA ";

            if (!string.IsNullOrEmpty(feeno))
            {
                sql += "WHERE FEE_NO='" + feeno + "' ";
            }
            if (!string.IsNullOrEmpty(record_id))
            {
                sql += " AND ASSESS_ID='" + record_id + "' ";
            }
            sql += " AND STATUS='Y'";
            base.DBExecSQL(sql, ref dt);

            return dt;
        }
    }
}