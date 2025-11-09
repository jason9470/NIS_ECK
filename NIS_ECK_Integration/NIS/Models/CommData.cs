using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NIS.Controllers;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using System.IO;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Data;
using Com.Mayaminer;

namespace NIS.Models
{

    public class SelectObj {
        public string p_group { set; get; }
        public Dictionary<string, string> p_value = new Dictionary<string, string>();
    }

    /// <summary>
    /// 建立下拉選單物件
    /// </summary>
    public class ParamsData{

        private DBConnector link;
        private BaseController baseC = new BaseController();
        /// <summary>
        /// 設定模組名稱
        /// </summary>
        public string p_model { set; get; }
        /// <summary>
        /// 設定要取得的模組名單
        /// </summary>
        public List<string> p_groups = new List<string>();
        public List<SelectObj> selectItem = new List<SelectObj>();
        
        public ParamsData(string i_p_model = null){
            this.link = new DBConnector();
            if(i_p_model != null)
                this.p_model = i_p_model;
        }
        
        /// <summary>
        /// 執行取得下拉選單資料
        /// </summary>
        public void setGroupData(){
            try
            {
                string sql = string.Empty;
                for (int i = 0; i <= this.p_groups.Count - 1; i++)
                {
                    SelectObj so = new SelectObj();
                    so.p_group = this.p_groups[i];
                    Dictionary<string, string> tDictionary = new Dictionary<string, string>();
                    sql = "select * from sys_params where p_model='" + this.p_model + "' and p_group= '" + this.p_groups[i] + "' order by p_sort asc";
                    DataTable Dt = link.DBExecSQL(sql);
                    if (Dt.Rows.Count > 0)
                    {
                        for (int d = 0; d < Dt.Rows.Count; d++)
                        {
                            so.p_value.Add(Dt.Rows[d]["p_name"].ToString(), Dt.Rows[d]["p_value"].ToString());
                        }
                    }
                    this.selectItem.Add(so);
                }
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = baseC.ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = baseC.ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, baseC.userinfo.EmployeesNo, tmp_action, ex.ToString(),ex);
            }
        }


        #region 寫ErrorLog (write_logMsg)
        //寫入 write_logMsg
        public static void write_logMsg(string controllerName, string loginID, string actionName, string err_msg, string err_folder = "DBExecSQL", string strSQL = "", Exception ex = null)
        {
            string tmp_msg = "", tmpfolder = "";
            if (err_folder != "")
                tmpfolder = err_folder;
            else
                tmpfolder = "DBExecSQL";
            tmp_msg = "loginID: " + loginID + ",\tcontrollerName: " + controllerName + "\t,ActionName: " + actionName + "\t行號: #" + GetLineNumber(ex).ToString() + "\n";
            LogTool log = new LogTool();
            if (strSQL != "")
            {
                log.saveLogMsg(tmp_msg + ", err_msg= \t" + err_msg, tmpfolder);
                log.saveLogMsg(tmp_msg + ", SQL= \t" + strSQL, tmpfolder);
            }
            else
                log.saveLogMsg(tmp_msg + ", err_msg= \t" + err_msg, tmpfolder);
        }
        public static void write_logMsg(string controllerName, string loginID, string actionName, string err_msg)
        {
            string tmp_msg = "", tmpfolder = "DBExecSQL", strSQL = "", err_folder = "";
            if (err_folder != "")
                tmpfolder = err_folder;
            else
                tmpfolder = "DBExecSQL";
            tmp_msg = "loginID: " + loginID + ",\tcontrollerName: " + controllerName + "\t,ActionName: " + actionName + "\n";
            LogTool log = new LogTool();
            if (strSQL != "")
            {
                log.saveLogMsg(tmp_msg + ", err_msg= \t" + err_msg, tmpfolder);
                log.saveLogMsg(tmp_msg + ", SQL= \t" + strSQL, tmpfolder);
            }
            else
                log.saveLogMsg(tmp_msg + ", err_msg= \t" + err_msg, tmpfolder);
        }

        public static void write_logMsg(string controllerName, string loginID, string actionName, string err_msg, Exception ex = null)
        {
            string tmp_msg = "", tmpfolder = "DBExecSQL", strSQL = "", err_folder = "";
            if (err_folder != "")
                tmpfolder = err_folder;
            else
                tmpfolder = "DBExecSQL";
            tmp_msg = "loginID: " + loginID + ",\tcontrollerName: " + controllerName + "\t,ActionName: " + actionName + "\t行號: #" + GetLineNumber(ex).ToString() + "\n";
            LogTool log = new LogTool();
            if (strSQL != "")
            {
                log.saveLogMsg(tmp_msg + ", err_msg= \t" + err_msg, tmpfolder);
                log.saveLogMsg(tmp_msg + ", SQL= \t" + strSQL, tmpfolder);
            }
            else
                log.saveLogMsg(tmp_msg + ", err_msg= \t" + err_msg, tmpfolder);
        }

        //回傳error行號
        public static int GetLineNumber(Exception ex)
        {
            var lineNumber = 0;
            const string lineSearch = ":line ";
            var index = ex.StackTrace.LastIndexOf(lineSearch);
            if (index != -1)
            {
                var lineNumberText = ex.StackTrace.Substring(index + lineSearch.Length);
                if (int.TryParse(lineNumberText, out lineNumber))
                {
                }
            }
            return lineNumber;
        }
        #endregion

        /// <summary>
        /// 取得所需要的下拉選單
        /// </summary>
        /// <param name="p_group"></param>
        /// <returns></returns>
        public Dictionary<string, string> getGroupData(string p_group) {

            Dictionary<string, string> temp = null;
            for (int i = 0; i <= this.selectItem.Count - 1; i++ )
            {
                if (this.selectItem[i].p_group == p_group) {
                    temp = this.selectItem[i].p_value;
                    break;
                }
            }
            return temp;
        }
    }


    public class CommData 
    {
        private DBConnector link;
        private BaseController baseC = new BaseController();

        public CommData() {
            this.link = new DBConnector();
        }

        public void getFuncListItem(string userType, ref List<string> fList)
        {
            fList.Clear();
            // 拼湊字串  ==>  群組$名稱$連結位置
            string[] data = new string[] { "生理監測", "VitalSign", "..//VitalSign//VitalSign_Index" };
            fList.Add(String.Join( "$", data));

            data = new string[] { "生理監測", "Intake//Output", "..//VitalSign//IO_Index" };
            fList.Add(String.Join("$", data));
        }

        //寫入 write_logMsg
        public static void write_logMsg(string controllerName, string loginID, string actionName, string err_msg, string err_folder = "DBExecSQL", string strSQL = "")
        {
            string tmp_msg = "", tmpfolder = "";
            if (err_folder != "")
                tmpfolder = err_folder;
            else
                tmpfolder = "DBExecSQL";
            tmp_msg = "loginID: " + loginID + ",\tcontrollerName: " + controllerName + "\t,ActionName: " + actionName + "\n";
            LogTool log = new LogTool();
            if (strSQL != "")
            {
                log.saveLogMsg(tmp_msg + ", err_msg= \t" + err_msg, tmpfolder);
                log.saveLogMsg(tmp_msg + ", SQL= \t" + strSQL, tmpfolder);
            }
            else
                log.saveLogMsg(tmp_msg + ", err_msg= \t" + err_msg, tmpfolder);
        }

        /// <summary>
        /// 取得即時訊息
        /// </summary>
        /// <param name="FeeNo"></param>
        /// <returns></returns>
        public List<string[]> getMqInfo(string FeeNo)
        {
            List<string[]> mqList = new List<string[]>();
            try
            {
                string sqlstr = string.Empty;
                
                string Memo;
                string ACTIONLINK;
                sqlstr = "select MEMO,ACTIONLINK from data_notice where STARTTIME < ";
                sqlstr += "to_date('" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/MM/dd hh24:mi:ss') and ";
                sqlstr += "TIMEOUT > to_date('" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/MM/dd hh24:mi:ss') and ";
                sqlstr += "Fee_No in( 'all','" + FeeNo + "') ORDER BY FEE_NO DESC ,STARTTIME";
                DataTable Dt = link.DBExecSQL(sqlstr);
                if (Dt.Rows.Count > 0)
                {
                    for (int d = 0; d < Dt.Rows.Count; d++)
                    {
                        Memo = Dt.Rows[d]["MEMO"].ToString().Trim();
                        ACTIONLINK = Dt.Rows[d]["ACTIONLINK"].ToString().Trim();
                        mqList.Add(new string[] { ACTIONLINK, Memo });
                    }
                }
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = baseC.ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = baseC.ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, baseC.userinfo.EmployeesNo, tmp_action, ex.ToString());
            }
            finally
            {
                link.DBClose();
            }
            return mqList;
        }

        /// <summary>
        /// 回傳下拉選單
        /// </summary>
        /// <param name="ModelName"></param>
        /// <param name="GroupName"></param>
        /// <returns></returns>
        public Dictionary<string, string> getSelectList(string ModelName, string GroupName)
        {
            Dictionary<string, string> MyList = new Dictionary<string, string>();
            try
            {
                string sql = string.Empty;
                sql = "  SELECT * FROM SYS_PARAMS ";
                sql += " WHERE P_MODEL='" + ModelName.Trim() + "' ";
                if (GroupName != "")
                    sql += " AND P_GROUP = '" + GroupName.Trim() + "' ";
                sql += " AND P_LANG='" + NIS.Controllers.BaseController.cultureName.Trim() + "' ";
                sql += " ORDER BY P_SORT ASC ";
                
                DataTable Dt = this.link.DBExecSQL(sql);
                if (Dt.Rows.Count > 0)
                {
                    for (int d = 0; d < Dt.Rows.Count; d++)
                    {
                       MyList.Add(Dt.Rows[d]["p_name"].ToString().Trim(), Dt.Rows[d]["p_value"].ToString().Trim());
                    }
                }
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = baseC.ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = baseC.ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, baseC.userinfo.EmployeesNo, tmp_action, ex.ToString());
            }
            finally
            {
                this.link.DBClose();
            }

            return MyList;
        }

        /// <summary>
        /// 取得下拉選單內容
        /// </summary>
        /// <param name="ModelName">模組名稱</param>
        /// <param name="GroupName">群組名稱</param>
        /// <param name="MyList">清單記憶體位置</param>
        public List<SelectListItem> getSelectItem(string ModelName, string GroupName, string defaultValue = "")
        {
            List<SelectListItem> MyList = new List<SelectListItem>();
            try
            {
                string sqlStatment = string.Empty;

                sqlStatment = "  SELECT * FROM SYS_PARAMS ";
                sqlStatment += " WHERE P_MODEL='" + ModelName.Trim() + "' ";
                if (GroupName != "")
                    sqlStatment += " AND P_GROUP = '" + GroupName.Trim() + "' ";
                sqlStatment += " AND P_LANG='" + NIS.Controllers.BaseController.cultureName.Trim() + "' ";
                sqlStatment += " ORDER BY P_SORT ASC ";
                
                DataTable Dt = this.link.DBExecSQL(sqlStatment);
                if (Dt.Rows.Count > 0)
                {
                    for (int d = 0; d < Dt.Rows.Count; d++)
                    {
                        if (defaultValue == Dt.Rows[d]["p_value"].ToString().Trim())
                        {
                            MyList.Add(new SelectListItem()
                            {
                                Text = Dt.Rows[d]["p_name"].ToString().Trim(),
                                Value = Dt.Rows[d]["p_value"].ToString().Trim(),
                                Selected = true
                            });
                        }
                        else
                        {
                            MyList.Add(new SelectListItem()
                            {
                                Text = Dt.Rows[d]["p_name"].ToString().Trim(),
                                Value = Dt.Rows[d]["p_value"].ToString().Trim(),
                                Selected = false
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = baseC.ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = baseC.ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, baseC.userinfo.EmployeesNo, tmp_action, ex.ToString());
            }
            finally
            {
                this.link.DBClose();
            }

            return MyList;
        }

        /// <summary>
        /// 設定下拉選單預設值
        /// </summary>
        /// <param name="cList">參照的下拉選單內容</param>
        /// <param name="value">要設定的值或文字</param>
        public static void setSelectItem(ref List<SelectListItem> cList, string value)
        {
                if (cList.Count > 0)
                {
                    // 先全部清除
                    for (int i = 0; i <= cList.Count - 1; i++)
                    {
                        cList[i].Selected = false;
                    }

                    //設定值
                    for (int i = 0; i <= cList.Count - 1; i++)
                    {
                        if (cList[i].Text == value || cList[i].Value == value)
                        {
                            cList[i].Selected = true;
                        }
                        else
                        {
                            cList[i].Selected = false;
                        }
                    }
                    
                }
        }

        /// <summary>
        /// 取得共用處置資料集
        /// </summary>
        /// <param name="pSVOType">共用處置類型，例如：{"bfl","bph"}</param>
        /// <remarks>2016/05/17 Vanda Add</remarks>
        public string getSharedVitalSign(string[] pSVOType = null)
        {
            string TypeCondition = "";
            if (pSVOType != null && pSVOType.Length > 0) TypeCondition = string.Format("WHERE SVO_TYPE IN ('{0}')", string.Join("','", pSVOType));
            return string.Format("SELECT * FROM SHARED_VITALSIGN_OPTION {0}", TypeCondition);
        }

    }
}