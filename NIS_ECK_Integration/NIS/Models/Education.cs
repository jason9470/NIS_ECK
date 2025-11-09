using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NIS.Models;
using System.Data;
using Com.Mayaminer;
using NIS.Controllers;

namespace NIS.Models
{
    public class Education : DBConnector
    {
        private BaseController baseC = new BaseController();
        private DBConnector link;
        public Education()
        {
            this.link = new DBConnector();
        }

        public DataTable sel_health_education(string feeno, string edu_id, string check)
        {
            DataTable dt = new DataTable();
            try
            {
                string sql = "SELECT HEALTH_EDUCATION_DATA.*, ";
                sql += "(SELECT ITEM_CATEGORY FROM HEALTH_EDUCATION_ITEM_DATA WHERE HEALTH_EDUCATION_ITEM_DATA.ITEM_ID = HEALTH_EDUCATION_DATA.ITEMID)ITEM_CATEGORY, ";
                sql += "(SELECT NAME FROM HEALTH_EDUCATION_ITEM_DATA WHERE HEALTH_EDUCATION_ITEM_DATA.ITEM_ID = HEALTH_EDUCATION_DATA.ITEMID)NAME, ";
                sql += "(SELECT ITEM_REFERENCE_URL FROM HEALTH_EDUCATION_ITEM_DATA WHERE HEALTH_EDUCATION_ITEM_DATA.ITEM_ID = HEALTH_EDUCATION_DATA.ITEMID)ITEM_REFERENCE_URL, ";
                sql += "(SELECT P_NAME FROM SYS_PARAMS WHERE (SELECT EXPLANATION_ID FROM HEALTH_EDUCATION_ITEM_DATA WHERE HEALTH_EDUCATION_ITEM_DATA.ITEM_ID = HEALTH_EDUCATION_DATA.ITEMID) = P_VALUE)EXPLANATION_NAME ";
                sql += "FROM HEALTH_EDUCATION_DATA WHERE 0 = 0 ";
                if (feeno != "")
                    sql += "AND FEENO = '" + feeno + "' ";
                if (edu_id != "")
                    sql += "AND EDU_ID IN (" + edu_id + ") ";
                if (check != "")
                    sql += "AND SCORE_TIME IS NULL ";

                sql += "AND DELETED IS NULL ORDER BY SCORE_TIME,RECORDTIME DESC ";

                link.DBExecSQL(sql, ref dt);
            }
            catch (Exception ex)
            {//寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = baseC.ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = baseC.ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, baseC.userinfo.EmployeesNo, tmp_action, ex.ToString(),ex);
            }
            finally
            {
                this.link.DBClose();
            }

            return dt;
        }


        public DataTable sel_item(DataTable dt, string id)
        {
            try
            {
                string sql = "SELECT * FROM HEALTH_EDUCATION_ITEM_DATA WHERE 0 = 0 ";
                if (id != "")
                    sql += " AND ITEM_ID = '" + id + "' ";

                link.DBExecSQL(sql, ref dt);
            }
            catch (Exception ex)
            {//寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = baseC.ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = baseC.ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, baseC.userinfo.EmployeesNo, tmp_action, ex.ToString(),ex);
            }
            finally
            {
                this.link.DBClose();
            }
            return dt;
        }


       
    }
}
