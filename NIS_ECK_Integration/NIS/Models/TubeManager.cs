using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using System.Data.OleDb;
using Com.Mayaminer;
using NIS.Controllers;
using System.Web.WebPages;

namespace NIS.Models
{
    public class TubeManager : DBConnector
    {
        private BaseController baseC = new BaseController();
        private DBConnector link = new DBConnector();
        /// <summary>
        /// 取得Tube
        /// </summary>
        /// <param name="userno">依據userno</param>
        /// <param name="row">依據row</param>
        /// <param name="id">依據id</param>
        /// <returns>回傳table</returns>
        public DataTable sel_tube(string feeno, string userno, string row, string id, string check = "")
        {
            DataTable dt = new DataTable();
            try
            {
                string sql = "SELECT TUBE.*,TUBE_FEATURE.NUMBERID,TUBE_FEATURE.NUMBEROTHER,TUBE_FEATURE.MATERIALID,TUBE_FEATURE.MATERIALOTHER, ";
                sql += "(select P_NAME from SYS_PARAMS WHERE P_MODEL = 'tube' AND P_GROUP = 'tubePosition' AND P_VALUE = TUBE.LOCATION)LOCATION_NAME, ";
                sql += "(select P_NAME from SYS_PARAMS WHERE P_MODEL = 'tube' AND P_GROUP = 'tubeNumber' AND P_VALUE = TUBE_FEATURE.NUMBERID)NUBER_NAME, ";
                sql += "(select P_NAME from SYS_PARAMS WHERE P_MODEL = 'tube' AND P_GROUP = 'tubeMaterial' AND P_VALUE = TUBE_FEATURE.MATERIALID)MATERIAL_NAME, ";
                sql += "(select P_NAME from SYS_PARAMS WHERE P_MODEL = 'tube' AND P_GROUP = 'tubeLengthUnit' AND P_VALUE = TUBE.LENGTHUNIT)LENGTHUNIT_NAME, ";
                sql += "(select KINDNAME from TUBE_KIND WHERE KINDID = TUBE.TYPEID)TYPE_NAME, ";
                sql += "(select TUBE_GROUP from TUBE_KIND WHERE KINDID = TUBE.TYPEID)TYPE_GROUP, ";
                sql += "(select ASSESS_TYPE from TUBE_KIND WHERE KINDID = TUBE.TYPEID)ASSESS_TYPE, ";
                sql += "(select BUNDLE_CARE from TUBE_KIND WHERE KINDID = TUBE.TYPEID)BUNDLE_CARE ";//LU 20230105 增加參數索引
                sql += "FROM TUBE INNER JOIN TUBE_FEATURE ON TUBE.TUBEID = TUBE_FEATURE.FEATUREID WHERE 0 = 0 ";
                if (feeno != "")
                    sql += "AND FEENO = '" + feeno + "' ";
                if (userno != "")
                    sql += "AND CREATNO = '" + userno + "' ";
                if (row != "")
                    sql += "AND TUBEROW = '" + row + "' ";
                if (id != "")
                    sql += "AND TUBEID = '" + id + "' ";
                if (check == "")
                {
                    sql += "AND ENDTIME is NULL AND DELETED is NULL order by STARTTIME ASC ";
                    link.DBExecSQL(sql, ref dt);
                }
                else
                {
                    sql += "order by STARTTIME ASC ";
                    link.DBExecSQL(sql, ref dt);
                }
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = baseC.ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = baseC.ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, baseC.userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }
            finally
            {
                this.link.DBClose();
            }

            return dt;
        }

        /// <summary>
        /// 搜尋管路種類
        /// </summary>
        /// <param name="kind">以ID搜尋</param>
        /// <param name="other">其他(0:系統設定 1:其他)</param>
        public List<Dictionary<string, string>> sel_tubekind(string kind, string other)
        {
            List<Dictionary<string, string>> dt = new List<Dictionary<string, string>>();
            try
            {
                Dictionary<string, string> temp = null;
                string sql = "SELECT * FROM TUBE_KIND WHERE 0 = 0 ";
                if (kind != "")
                    sql += "AND TUBE_GROUP = '" + kind + "' ";
                if (other != "")
                    sql += "AND OTHER = '" + other + "' AND STATUS IS NULL ORDER BY TUBE_SEQ ";
                DataTable Dt = link.DBExecSQL(sql);
                if (Dt.Rows.Count > 0)
                {
                    List<string> columns = new List<string>();
                    for (int i = 0; i < Dt.Columns.Count; i++)
                        columns.Add(Dt.Columns[i].ToString());

                    for (int d = 0; d < Dt.Rows.Count; d++)
                    {
                        temp = new Dictionary<string, string>();
                        foreach (string item in columns)
                            temp[item] = Dt.Rows[d][item].ToString();
                        dt.Add(temp);
                    }
                }
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = baseC.ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = baseC.ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, baseC.userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }
            finally
            {
                this.link.DBClose();
            }
            return dt;
        }

        /// <summary>
        /// 搜尋管路評估_其他種類評估
        /// </summary>
        public DataTable sel_tube_assess_other(string feeno, string starttime, string endtime, string id)
        {
            DataTable dt = new DataTable();
            try
            {
                string sql = "SELECT ASSESS.*,TUBE_FEATURE.COLOROTHER,TUBE_FEATURE.NATUREOTHER,TUBE_FEATURE.TASTEOTHER,TUBE_FEATURE.COLORID,TUBE_FEATURE.TASTEID,TUBE_FEATURE.NATUREID, ";
                sql += "(SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'io' AND P_GROUP ='outputcolor_Drainage' AND P_VALUE = TUBE_FEATURE.COLORID)COLORNAME, ";
                sql += "(SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'io' AND P_GROUP ='outputtaste_Drainage' AND P_VALUE = TUBE_FEATURE.TASTEID)TASTENAME, ";
                sql += "(SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'io' AND P_GROUP ='outputnature_Drainage' AND P_VALUE = TUBE_FEATURE.NATUREID)NATURENAME, ";
                sql += "(SELECT KINDNAME FROM TUBE_KIND WHERE KINDID = (SELECT TYPEID FROM TUBE WHERE TUBEROW = ASSESS.TUBEROW))TYPE_NAME, ";
                sql += "(SELECT BUNDLE_CARE FROM TUBE_KIND WHERE KINDID = (SELECT TYPEID FROM TUBE WHERE TUBEROW = ASSESS.TUBEROW))BUNDLE_CARE ";
                sql += "FROM NIS_TUBE_ASSESS_OTHER_DATA ASSESS INNER JOIN TUBE_FEATURE ON ASSESS.ASSESS_ID = TUBE_FEATURE.FEATUREID WHERE 0 = 0 ";
                if (starttime != "")
                    sql += "AND RECORDTIME BETWEEN to_date('" + starttime + "','yyyy/mm/dd hh24:mi:ss') AND to_date('" + endtime + "','yyyy/mm/dd hh24:mi:ss') ";
                if (feeno != "")
                    sql += "AND FEENO = '" + feeno + "' ";
                if (id != "")
                    sql += "AND ASSESS_ID = '" + id + "' ";

                sql += "AND DELETED is null ORDER BY RECORDTIME DESC ";

                link.DBExecSQL(sql, ref dt);
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = baseC.ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = baseC.ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, baseC.userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }
            finally
            {
                this.link.DBClose();
            }

            return dt;
        }

        /// <summary>
        /// 搜尋管路評估_氣切種類評估
        /// </summary>
        public DataTable sel_tube_assess_tracheostomy(string feeno, string starttime, string endtime, string id)
        {
            DataTable dt = new DataTable();
            try
            {
                string sql = "SELECT ASSESS.*, ";
                sql += "(SELECT KINDNAME FROM TUBE_KIND WHERE KINDID = (SELECT TYPEID FROM TUBE WHERE TUBEROW = ASSESS.TUBEROW))TYPE_NAME, ";
                sql += "(SELECT BUNDLE_CARE FROM TUBE_KIND WHERE KINDID = (SELECT TYPEID FROM TUBE WHERE TUBEROW = ASSESS.TUBEROW))BUNDLE_CARE ";
                sql += "FROM NIS_TUBE_ASSESS_TRACH_DATA ASSESS WHERE 0 = 0 ";
                if (starttime != "")
                    sql += "AND RECORDTIME BETWEEN to_date('" + starttime + "','yyyy/mm/dd hh24:mi:ss') AND to_date('" + endtime + "','yyyy/mm/dd hh24:mi:ss') ";
                if (feeno != "")
                    sql += "AND FEENO = '" + feeno + "' ";
                if (id != "")
                    sql += "AND ASSESS_ID = '" + id + "' ";

                sql += "AND DELETED is null ORDER BY RECORDTIME DESC ";

                link.DBExecSQL(sql, ref dt);
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = baseC.ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = baseC.ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, baseC.userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }
            finally
            {
                this.link.DBClose();
            }
            return dt;
        }

        /// <summary>
        /// 搜尋管路評估_動脈種類評估
        /// </summary>
        public DataTable sel_tube_assess_artery(string feeno, string starttime, string endtime, string id)
        {
            DataTable dt = new DataTable();
            try
            {
                string sql = "SELECT ASSESS.*, ";
                sql += "(SELECT KINDNAME FROM TUBE_KIND WHERE KINDID = (SELECT TYPEID FROM TUBE WHERE TUBEROW = ASSESS.TUBEROW))TYPE_NAME, ";
                sql += "(SELECT BUNDLE_CARE FROM TUBE_KIND WHERE KINDID = (SELECT TYPEID FROM TUBE WHERE TUBEROW = ASSESS.TUBEROW))BUNDLE_CARE ";
                sql += "FROM NIS_TUBE_ASSESS_ARTERY_DATA ASSESS WHERE 0 = 0 ";
                if (starttime != "")
                    sql += "AND RECORDTIME BETWEEN to_date('" + starttime + "','yyyy/mm/dd hh24:mi:ss') AND to_date('" + endtime + "','yyyy/mm/dd hh24:mi:ss') ";
                if (feeno != "")
                    sql += "AND FEENO = '" + feeno + "' ";
                if (id != "")
                    sql += "AND ASSESS_ID = '" + id + "' ";

                sql += "AND DELETED is null ORDER BY RECORDTIME DESC ";

                link.DBExecSQL(sql, ref dt);
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = baseC.ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = baseC.ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, baseC.userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }
            finally
            {
                this.link.DBClose();
            }
            return dt;
        }

        /// <summary>
        /// 搜尋管路評估_最大編號
        /// </summary>
        public DataTable sel_tube_max_number(string feeno)
        {
            DataTable dt = new DataTable();
            try
            {
                string sql = "SELECT max(LPAD(numberother,3,'0')) FROM TUBE INNER JOIN TUBE_FEATURE ON TUBE.TUBEID = TUBE_FEATURE.FEATUREID WHERE 0=0 ";
                if (feeno != "")
                    sql += "AND FEENO = '" + feeno + "' ";

                link.DBExecSQL(sql, ref dt);
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = baseC.ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = baseC.ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, baseC.userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }
            finally
            {
                this.link.DBClose();
            }
            return dt;
        }

        public DataTable sel_sys_param_data(string model)
        {
            DataTable dt = new DataTable();
            try
            {
                string sql = "SELECT * FROM SYS_PARAMS WHERE 0 = 0 ";
                if (model != "")
                    sql += "AND P_MODEL='" + model + "' ";

                link.DBExecSQL(sql, ref dt);
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = baseC.ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = baseC.ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, baseC.userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }
            finally
            {
                this.link.DBClose();
            }
            return dt;
        }

        /// <summary>
        /// 取得評估項目的值
        /// </summary>
        /// <param name="id">欲搜尋ID</param>
        public string sel_data(DataTable dt, string group, string dt_value)
        {
            string value = "";
            try
            {
                foreach (DataRow r in dt.Rows)
                {
                    if (r["P_GROUP"].ToString() == group && r["P_VALUE"].ToString() == dt_value)
                        value = r["P_NAME"].ToString();
                }
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = baseC.ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = baseC.ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, baseC.userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }
            finally
            {
                this.link.DBClose();
            }
            return value;
        }

        #region 管路照護

        /// <summary>
        /// LU 20230110
        /// 取得bundle master 表
        /// </summary>
        /// <param name="feeno">批價序號(住院序號)</param>
        /// <param name="ass_id">COLUMN[ASSESS_ID]</param>
        /// <param name="tube_id">COLUMN[TUBEID]</param>
        /// <returns>DataTable By [CREATE_TIME] DESC</returns>
        public DataTable sel_tube_bundle_master(string feeno, string ass_id, string tube_id)
        {
            DataTable dt = new DataTable();
            try
            {
                string sql = "SELECT BUNDLE.* ";
                sql += "FROM NIS_TUBE_ASSESS_BUNDLE_MASTER BUNDLE WHERE 0 = 0 ";
                sql += "AND DELETE_TIME IS NULL ";
                if (feeno != "")
                    sql += "AND FEENO = '" + feeno + "' ";
                if (ass_id != "")
                    sql += "AND ASSESS_ID = '" + ass_id + "' ";
                if (tube_id != "")
                    sql += "AND TUBEID = '" + tube_id + "' ";
                sql += "ORDER BY CREATE_TIME DESC ";

                link.DBExecSQL(sql, ref dt);
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = baseC.ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = baseC.ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, baseC.userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }
            finally
            {
                this.link.DBClose();
            }
            return dt;
        }

        /// <summary>
        /// LU 20230110
        /// 取得bundle detail 表
        /// </summary>
        /// <param name="table_id">COLUMN[TABLE_ID]</param>
        /// <returns>All Conform Items[ID,TYPE,VALUE]</returns>
        public DataTable sel_tube_bundle_datail(string table_id)
        {
            DataTable dt = new DataTable();
            try
            {
                string sql = "SELECT BUNDLE.* ";
                sql += "FROM NIS_TUBE_ASSESS_BUNDLE_DETAIL BUNDLE WHERE 0 = 0 ";
                if (table_id != "")
                    sql += "AND TABLE_ID = '" + table_id + "' ";
                sql += "ORDER BY SERIAL ";

                link.DBExecSQL(sql, ref dt);
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = baseC.ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = baseC.ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, baseC.userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }
            finally
            {
                this.link.DBClose();
            }
            return dt;
        }

        /// <summary>
        /// 20230912 LU
        /// 找到管路照護相關跑馬燈資料
        /// </summary>
        /// <param name="feeno">住院序號</param>
        /// <param name="bc_type">管路種類</param>
        /// <returns>資料表</returns>
        public DataTable sel_bundle_notice_id(string feeno, string bc_type)
        {
            DataTable dt = new DataTable();
            try
            {
                string sql = "SELECT * ";
                sql += "FROM DATA_NOTICE ";
                sql += "WHERE TIMEOUT = to_date('9999/12/31','yyyy/mm/dd') ";
                sql += "AND NT_ID like 'NIS_TUBE_ASSESS_BUNDLE_MASTER%' ";
                if (!feeno.IsEmpty())
                    sql += "AND FEE_NO = '" + feeno + "' ";
                if (!bc_type.IsEmpty())
                    sql += "AND NT_ID like '%" + bc_type + "' ";
                sql += "ORDER BY NT_ID ASC ";

                link.DBExecSQL(sql, ref dt);
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = baseC.ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = baseC.ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, baseC.userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }
            finally
            {
                this.link.DBClose();
            }

            return dt;
        }

        /// <summary>
        /// 20230912 LU
        /// 依管路找到管路照護種類相關資料
        /// </summary>
        /// <param name="tubeId">管路唯一序號</param>
        /// <returns>資料表</returns>
        public DataTable sel_tube_bundlecare_by_id(string tubeId)
        {
            DataTable dt = new DataTable();
            try
            {
                string sql = "SELECT t.TUBEID,t.TYPEID,k.BUNDLE_CARE ";
                sql += "FROM TUBE t LEFT JOIN TUBE_KIND k on t.TYPEID = k.KINDID ";
                sql += "WHERE 0 = 0 ";
                if (!tubeId.IsEmpty())
                    sql += "AND t.TUBEID = '" + tubeId + "' ";
                link.DBExecSQL(sql, ref dt);
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = baseC.ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = baseC.ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, baseC.userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }
            finally
            {
                this.link.DBClose();
            }

            return dt;
        }
        #endregion

    }
    #region Bundle Care DAO
    /// <summary>
    /// Bundle Care Data    
    /// </summary>
    public class DB_NIS_TUBE_ASSESS_BUNDLE1
    {
        public string NAME { get; set; }
        public string VALUE { get; set; }
        public string TYPE { get; set; }
        public string ASSID { get; set; }
        public string TUBEID { get; set; }
        public string TUBEROW { get; set; }
        public string BC_TYPE { get; set; }
    }
    public class DB_NIS_TUBE_ASSESS_BUNDLE
    {
        public string NAME { get; set; }
        public string VALUE { get; set; }
        public string TYPE { get; set; }
    }
    #endregion
}
