using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NIS.Models;
using System.Data;
using System.Data.OleDb;
using NIS.Controllers;
using Com.Mayaminer;

namespace NIS.Models
{
    public class DBAction : DBConnector
    {
        //
        // GET: /DBAcion/
        private BaseController baseC = new BaseController();

        public int getinsert(string TableName, List<DBItem> sql)
        {
            int effectRow = DBExecInsert(TableName, sql);

            return effectRow;
        }

        public int getupd(string TableName, List<DBItem> sql, string where)
        {
           int effectRow = DBExecUpdate(TableName, sql, where);

            return effectRow;
        }

        public int getdelete(string TableName, string where)
        {
            int effectRow = DBExecDelete(TableName, where);
            return effectRow;
        }

        public DataTable gettable(string sql)
        {
            DataTable dt = new DataTable();
            try
            {
                DataTable Dt = DBExecSQL(sql);
                if (Dt.Rows.Count > 0)
                {
                    dt = Dt;
                }
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = baseC.ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = baseC.ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, baseC.userinfo.EmployeesNo, tmp_action, ex.ToString(),ex);
            }
            return dt;
        }

    }
}
