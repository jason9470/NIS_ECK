using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Data;
using NIS.Models;
using System.Data.OleDb;
using Newtonsoft.Json;

namespace NIS.Controllers
{
    public class CarePlanManagementController : BaseController
    {
        private DBConnector link;

        public CarePlanManagementController()
        {
            this.link = new DBConnector();
        }

        //維護護理問題_首頁
        public ActionResult Index()
        {
            List<SelectListItem> typeList = new List<SelectListItem>();

            string sql = "SELECT * FROM NIS_SYS_DIAGNOSIS_DOMAIN";
            DataTable Dt = this.link.DBExecSQL(sql);
            if (Dt.Rows.Count > 0)
            {
                foreach (DataRow r in Dt.Rows)
                {
                    typeList.Add(new SelectListItem { 
                        Text = r["DIAGNOSIS_DOMAIN_DESC"].ToString(), 
                        Value = r["DIAGNOSIS_DOMAIN_CODE"].ToString()
                    });
                }
            }
            ViewData["typeList"] = typeList;

            return View();
        }

        /// <summary>
        /// 搜尋護理問題
        /// </summary>
        /// <param name="rd">搜尋種類(SUBJECT: 科別, ITEM: 關鍵字)</param>
        /// <param name="text">關鍵字內容</param>
        /// <param name="sub">Nanda領域 代碼</param>
        /// <returns></returns>
        public PartialViewResult CarePlanSearch(string rd, string text, string sub)
        {
            ViewBag.dtsearch = this.SearchCarePlanTopic(rd, text, sub);
            return PartialView();
        }

        //停用護理問題
        public string CarePlanDisabled(string DiagnosisCode)
        {
            Boolean success = false;
            string Word = "";
            try
            {
                List<DBItem> dbItem = new List<DBItem>();
                dbItem.Add(new DBItem("DISABLE_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                dbItem.Add(new DBItem("MODIFY_ID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                dbItem.Add(new DBItem("MODIFY_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                dbItem.Add(new DBItem("MODIFY_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                if (this.link.DBExecUpdate("NIS_SYS_DIAGNOSIS", dbItem, "DIAGNOSIS_CODE = '" + DiagnosisCode + "' ") == 1)
                    success = true;
                else
                    success = false;
            }
            catch
            {
                success = false;
            }
            finally
            {
                if (success)
                    Word = "Y";
                else
                    Word = "N";

                link.DBClose();
            }
            return Word;
        }

        //啟用護理問題
        public string CarePlanAble(string DiagnosisCode)
        {
            Boolean success = false;
            string Word = "";
            try
            {
                List<DBItem> dbItem = new List<DBItem>();
                dbItem.Add(new DBItem("DISABLE_DATE", "", DBItem.DBDataType.String));
                dbItem.Add(new DBItem("MODIFY_ID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                dbItem.Add(new DBItem("MODIFY_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                dbItem.Add(new DBItem("MODIFY_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                if (this.link.DBExecUpdate("NIS_SYS_DIAGNOSIS", dbItem, "DIAGNOSIS_CODE = '" + DiagnosisCode + "' ") == 1)
                    success = true;
                else
                    success = false;
            }
            catch
            {
                success = false;
            }
            finally
            {
                if (success)
                    Word = "Y";
                else
                    Word = "N";

                this.link.DBClose();
            }
            return Word;
        }

        //護理計畫管理-護理問題，細項頁面
        public ActionResult CarePlanDetail()
        {
            List<SelectListItem> typeList = new List<SelectListItem>();

            string sql = "SELECT * FROM NIS_SYS_DIAGNOSIS_DOMAIN";
            DataTable Dt = this.link.DBExecSQL(sql);
            if (Dt.Rows.Count > 0)
            {
                foreach (DataRow r in Dt.Rows)
                {
                    typeList.Add(new SelectListItem
                    {
                        Text = r["DIAGNOSIS_DOMAIN_DESC"].ToString(),
                        Value = r["DIAGNOSIS_DOMAIN_CODE"].ToString()
                    });
                }
            }
            ViewData["typeList"] = typeList;
            return View();
        }

        /// <summary>
        /// 存檔
        /// </summary>
        /// <param name="OldDiagnosisCode"></param>
        /// <param name="DiagnosisCode"></param>
        /// <param name="DiagnosisDesc"></param>
        /// <param name="DiagnosisDomain"></param>
        /// <param name="FeatureList"></param>
        /// <param name="TargetList"></param>
        /// <param name="InducementsList"></param>
        /// <param name="MeasureList"></param>
        /// <returns></returns>
        public string SaveCarePlan(string OldDiagnosisCode, string DiagnosisCode, string DiagnosisDesc, string DiagnosisDomain
            , string FeatureList, string TargetList, string InducementsList, string MeasureList)
        {
            bool success = true;
            List<DBItem> dbItem = null;
            try
            {
                #region if_DiagnosisCode

                if (string.IsNullOrWhiteSpace(OldDiagnosisCode))
            {
                dbItem = new List<DBItem>();
                dbItem.Add(new DBItem("DIAGNOSIS_CODE", DiagnosisCode, DBItem.DBDataType.String));
                dbItem.Add(new DBItem("DIAGNOSIS_NAME", DiagnosisDesc, DBItem.DBDataType.String));
                dbItem.Add(new DBItem("DIAGNOSIS_DOMAIN_CODE", DiagnosisDomain, DBItem.DBDataType.String));
                dbItem.Add(new DBItem("CREATE_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                dbItem.Add(new DBItem("CREATE_ID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                dbItem.Add(new DBItem("CREATE_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                if (link.DBExecInsert("NIS_SYS_DIAGNOSIS", dbItem) == 1)
                    success = true;
                else
                    success = false;
            }
            else if (OldDiagnosisCode == DiagnosisCode)
            {
                dbItem = new List<DBItem>();
                dbItem.Add(new DBItem("DIAGNOSIS_NAME", DiagnosisDesc, DBItem.DBDataType.String));
                dbItem.Add(new DBItem("DIAGNOSIS_DOMAIN_CODE", DiagnosisDomain, DBItem.DBDataType.String));
                dbItem.Add(new DBItem("MODIFY_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                dbItem.Add(new DBItem("MODIFY_ID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                dbItem.Add(new DBItem("MODIFY_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                if (link.DBExecUpdate("NIS_SYS_DIAGNOSIS", dbItem, "DIAGNOSIS_CODE = '" + OldDiagnosisCode + "' ") == 1)
                    success = true;
                else
                    success = false;
            }
            else
                return "N";

            #endregion

            #region if_Success

            if (success)
            {
                link.DBExecDelete("nis_sys_diagnosis_feature", "DIAGNOSIS_CODE = '" + DiagnosisCode + "'");
                link.DBExecDelete("nis_sys_diagnosis_inducements", "DIAGNOSIS_CODE = '" + DiagnosisCode + "'");
                link.DBExecDelete("nis_sys_diagnosis_target", "DIAGNOSIS_CODE = '" + DiagnosisCode + "'");
                link.DBExecDelete("nis_sys_diagnosis_measure", "DIAGNOSIS_CODE = '" + DiagnosisCode + "'");

                List<Care_Plan_Item> dt_FeatureList = JsonConvert.DeserializeObject<List<Care_Plan_Item>>(FeatureList);
                List<Care_Plan_Item> dt_TargetList = JsonConvert.DeserializeObject<List<Care_Plan_Item>>(TargetList);
                List<Care_Plan_Item> dt_InducementsList = JsonConvert.DeserializeObject<List<Care_Plan_Item>>(InducementsList);
                List<Care_Plan_Item> dt_MeasureList = JsonConvert.DeserializeObject<List<Care_Plan_Item>>(MeasureList);

                foreach (Care_Plan_Item item in dt_FeatureList)
                {
                    dbItem = new List<DBItem>();
                    dbItem.Add(new DBItem("DIAGNOSIS_CODE", DiagnosisCode, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("FEATURE_CODE", item.FEATURE_CODE, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("FEATURE_DESC", item.FEATURE_DESC, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("CREATE_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("CREATE_ID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("CREATE_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("MODIFY_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("MODIFY_ID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("MODIFY_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("DISABLE_DATE", (item.DISABLE_DATE == "Y") ? "" : DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                    link.DBExecInsert("nis_sys_diagnosis_feature", dbItem);
                }
                foreach (Care_Plan_Item item in dt_TargetList)
                {
                    dbItem = new List<DBItem>();
                    dbItem.Add(new DBItem("DIAGNOSIS_CODE", DiagnosisCode, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("TARGET_CODE", item.TARGET_CODE, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("TARGET_DESC", item.TARGET_DESC, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("CREATE_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("CREATE_ID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("CREATE_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("MODIFY_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("MODIFY_ID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("MODIFY_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("DISABLE_DATE", (item.DISABLE_DATE == "Y") ? "" : DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                    link.DBExecInsert("nis_sys_diagnosis_target", dbItem);
                }
                foreach (Care_Plan_Item item in dt_InducementsList)
                {
                    dbItem = new List<DBItem>();
                    dbItem.Add(new DBItem("DIAGNOSIS_CODE", DiagnosisCode, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("INDUCEMENTS_CODE", item.INDUCEMENTS_CODE, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("INDUCEMENTS_DESC", item.INDUCEMENTS_DESC, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("CREATE_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("CREATE_ID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("CREATE_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("MODIFY_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("MODIFY_ID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("MODIFY_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("DISABLE_DATE", (item.DISABLE_DATE == "Y") ? "" : DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                    link.DBExecInsert("nis_sys_diagnosis_inducements", dbItem);
                }
                foreach (Care_Plan_Item item in dt_MeasureList)
                {
                    dbItem = new List<DBItem>();
                    dbItem.Add(new DBItem("DIAGNOSIS_CODE", DiagnosisCode, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("MEASURE_CODE", item.MEASURE_CODE, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("MEASURE_DESC", item.MEASURE_DESC, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("CREATE_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("CREATE_ID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("CREATE_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("MODIFY_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("MODIFY_ID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("MODIFY_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("DISABLE_DATE", (item.DISABLE_DATE == "Y") ? "" : DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                    link.DBExecInsert("nis_sys_diagnosis_measure", dbItem);
                }
                return "Y";
            }
            else
                return "N";

                #endregion

            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return "N";
            }
            finally
            {
                this.link.DBClose();
            }
        }

        #region 取得資料

        /// <summary> 關鍵字搜尋 </summary>
        public DataTable SearchCarePlanTopic(string rd, string text, string sub)
        {
            DataTable dt = new DataTable();

            string sql = "select a.diagnosis_code, a.diagnosis_name, a.disable_date, a.diagnosis_domain_code, b.diagnosis_domain_desc "
            + ", (select count(*) from CAREPLANMASTER where topicid = a.diagnosis_code) as use_num "
            + "from nis_sys_diagnosis a, nis_sys_diagnosis_domain b "
            + "where a.diagnosis_domain_code = b.diagnosis_domain_code ";

            if (rd == "SUBJECT" && !string.IsNullOrWhiteSpace(sub))
                sql += "and b.diagnosis_domain_code = '" + sub + "' ";
            else if (rd == "ITEM" && !string.IsNullOrWhiteSpace(text))
                sql += "AND a.diagnosis_name LIKE '%" + text + "%' ";

            sql += "ORDER BY a.diagnosis_name ASC";

            this.link.DBExecSQL(sql, ref dt);
            return dt;
        }

        public string Sel_Topic(string DiagnosisCode)
        {
            try
            {
                List<Care_Plan_Item> temp = new List<Care_Plan_Item>();
                string sql = "select * from nis_sys_diagnosis where diagnosis_code = '" + DiagnosisCode + "' ";
                DataTable Dt = link.DBExecSQL(sql);
                if (Dt.Rows.Count > 0)
                {
                    Care_Plan_Item temp_data = null;
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        temp_data = new Care_Plan_Item();
                        temp_data.DIAGNOSIS_CODE = Dt.Rows[i]["DIAGNOSIS_CODE"].ToString();
                        temp.Add(temp_data);
                    }
                }
                if (temp.Count > 0)
                    return "Y";
                else
                    return "N";
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return "N";
            }
        }

        /// <summary> 定義性特徵 </summary>
        public string Sel_Feature(string DiagnosisCode)
        {
            Care_Plan_Item temp_data = null;
            try
            {
                List<Care_Plan_Item> temp = new List<Care_Plan_Item>();
                string sql = "select * from nis_sys_diagnosis_feature where diagnosis_code = '" + DiagnosisCode + "' ";
                DataTable Dt = link.DBExecSQL(sql);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        temp_data = new Care_Plan_Item();
                        temp_data.FEATURE_CODE = Dt.Rows[i]["FEATURE_CODE"].ToString();
                        temp_data.FEATURE_DESC = Dt.Rows[i]["FEATURE_DESC"].ToString();
                        temp_data.DISABLE_DATE = Dt.Rows[i]["DISABLE_DATE"].ToString();
                        temp.Add(temp_data);
                    }
                }
                return JsonConvert.SerializeObject(temp);
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return JsonConvert.SerializeObject("");
            }
        }

        /// <summary> 相關因素 </summary>
        public string Sel_Inducements(string DiagnosisCode)
        {
            Care_Plan_Item temp_data = null;
            try
            {
                List<Care_Plan_Item> temp = new List<Care_Plan_Item>();
                string sql = "select * from nis_sys_diagnosis_inducements where diagnosis_code ='" + DiagnosisCode + "' ";
                DataTable Dt = link.DBExecSQL(sql);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        temp_data = new Care_Plan_Item();
                        temp_data.INDUCEMENTS_CODE = Dt.Rows[i]["INDUCEMENTS_CODE"].ToString();
                        temp_data.INDUCEMENTS_DESC = Dt.Rows[i]["INDUCEMENTS_DESC"].ToString();
                        temp_data.DISABLE_DATE = Dt.Rows[i]["DISABLE_DATE"].ToString();
                        temp.Add(temp_data);
                    }
                }
                return JsonConvert.SerializeObject(temp);
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return JsonConvert.SerializeObject("");
            }

        }

        /// <summary> 目標 </summary>
        public string Sel_Target(string DiagnosisCode)
        {
            Care_Plan_Item temp_data = null;
            try
            {
                List<Care_Plan_Item> temp = temp = new List<Care_Plan_Item>();
                string sql = "select * from nis_sys_diagnosis_target where diagnosis_code ='" + DiagnosisCode + "' ";

                DataTable Dt = link.DBExecSQL(sql);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        temp_data = new Care_Plan_Item();
                        temp_data.TARGET_CODE = Dt.Rows[i]["TARGET_CODE"].ToString();
                        temp_data.TARGET_DESC = Dt.Rows[i]["TARGET_DESC"].ToString();
                        temp_data.DISABLE_DATE = Dt.Rows[i]["DISABLE_DATE"].ToString();
                        temp.Add(temp_data);
                    }
                }
                return JsonConvert.SerializeObject(temp);
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return JsonConvert.SerializeObject("");
            }
        }

        /// <summary> 護理措施 </summary>
        public string Sel_Measure(string DiagnosisCode)
        {
            List<Care_Plan_Item> temp = temp = new List<Care_Plan_Item>();
            Care_Plan_Item temp_data = null;
            try
            {
                string sql = "select * from nis_sys_diagnosis_measure where diagnosis_code ='" + DiagnosisCode + "' ";

                DataTable Dt = link.DBExecSQL(sql);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        {
                            temp_data = new Care_Plan_Item();
                            temp_data.MEASURE_CODE = Dt.Rows[i]["MEASURE_CODE"].ToString();
                            temp_data.MEASURE_DESC = Dt.Rows[i]["MEASURE_DESC"].ToString();
                            temp_data.DISABLE_DATE = Dt.Rows[i]["DISABLE_DATE"].ToString();
                            temp.Add(temp_data);
                        }
                    }
                }
                return JsonConvert.SerializeObject(temp);
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return JsonConvert.SerializeObject("");
            }
        }

        #endregion

        //維護 Nanda領域
        public ActionResult DomainDetail()
        {
            DataTable dt = new DataTable();
            this.link.DBExecSQL("select * from NIS_SYS_DIAGNOSIS_DOMAIN ", ref dt);
            ViewBag.dt = dt;

            return View();
        }

        public string SaveDomain(string DomainList, string NewDomainList)
        {
            List<DBItem> dbItem = null;
            bool Success = true;
            List<Dictionary<string, string>> domainList = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(DomainList);
            List<Dictionary<string, string>> newdomainList = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(NewDomainList);
            try
            {
                foreach (var item in domainList)
                {
                    if (Success)
                    {
                        dbItem = new List<DBItem>();
                        dbItem.Add(new DBItem("DIAGNOSIS_DOMAIN_DESC", item["DIAGNOSIS_DOMAIN_DESC"], DBItem.DBDataType.String));
                        dbItem.Add(new DBItem("MODIFY_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                        dbItem.Add(new DBItem("MODIFY_ID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                        dbItem.Add(new DBItem("MODIFY_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                        if (item["DISABLE_DATE"].Equals("Y"))
                            dbItem.Add(new DBItem("DISABLE_DATE", "", DBItem.DBDataType.String));
                        else
                            dbItem.Add(new DBItem("DISABLE_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));

                        if (link.DBExecUpdate("NIS_SYS_DIAGNOSIS_DOMAIN", dbItem, "DIAGNOSIS_DOMAIN_CODE = '" + item["DIAGNOSIS_DOMAIN_CODE"] + "' ") >= 0)
                            Success = true;
                        else
                            Success = false;
                    }
                }
                foreach (var item in newdomainList)
                {
                    dbItem = new List<DBItem>();
                    dbItem.Add(new DBItem("DIAGNOSIS_DOMAIN_CODE", item["DIAGNOSIS_DOMAIN_CODE"], DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("DIAGNOSIS_DOMAIN_DESC", item["DIAGNOSIS_DOMAIN_DESC"], DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("CREATE_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("CREATE_ID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("CREATE_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("MODIFY_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("MODIFY_ID", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    dbItem.Add(new DBItem("MODIFY_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    if (item["DISABLE_DATE"].Equals("Y"))
                        dbItem.Add(new DBItem("DISABLE_DATE", "", DBItem.DBDataType.String));
                    else
                        dbItem.Add(new DBItem("DISABLE_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));

                    link.DBExecInsert("NIS_SYS_DIAGNOSIS_DOMAIN", dbItem);
                }
            }
            catch
            {
                Success = false;
            }
            finally
            {
                link.DBClose();
            }

            if (Success)
                return "Y";
            else
                return "N";
        }
    }
}
