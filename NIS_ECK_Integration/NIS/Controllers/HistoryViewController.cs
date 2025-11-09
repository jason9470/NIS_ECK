using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using NIS.Data;
using System.Threading;
using System.Diagnostics;
using System.Text;
using System.IO;
using NIS.Models;

namespace NIS.Controllers
{
    public class HistoryViewController : BaseController
    {
        private DBConnector link = new DBConnector();
        
        
        public ActionResult Index()
        {
            string show_history_button = "N";
            
            if (Session["PatInfo"] != null)
            {
                if (Session["bl_isHistoryPatient"] != null)
                {  //如果此病人有歷史區資料, 在 HTML 就顯示 [修改歷史區資料] button
                    show_history_button = Session["bl_isHistoryPatient"].ToString();
                }

                PatientInfo pinfo = (PatientInfo)Session["PatInfo"];

                if (Session["inHistory"] !=null)
                {
                    //取得病人歷次住院/本次住院資料/登入者資料
                    List<InHistory> inHistoryList = (List<InHistory>)Session["inHistory"];
                    UserInfo User = (UserInfo)Session["UserInfo"];

                    //線上歷史區
                    foreach (var item in inHistoryList)
                    {
                        if ((item.FeeNo == pinfo.FeeNo) && (item.DataZone == "CS1"))
                        {
                            show_history_button = "Y";
                            break;
                        }
                    }

                }

                ViewData["ptinfo"] = pinfo;
                ViewData["feeno"] = pinfo.FeeNo;

                ViewBag.ptinfo = pinfo;
                ViewBag.feeno = pinfo.FeeNo; 
                ViewBag.ChartNo = pinfo.ChartNo;
                ViewBag.showHistory = show_history_button;
                return View();
            }
            else
            {
                Response.Write("<script>alert('請選擇病人!');</script>");
                return new EmptyResult();
            }
        }

        #region 搬移歷史區資料
        public void APPLY_RECOVER(string FEENO)
        {
            

            if (Session["inHistory"] != null)
            {
                try
                {
                    //取得本次住院資料
                    PatientInfo pinfo =  (PatientInfo)Session["PatInfo"];                  
                                       
                    if (FEENO != null)
                    {
                        HISTORY_APPLY_RECOVER(pinfo.FeeNo); //申請修改 歷史區資料
                        Response.Write("<script>alert('歷史資料準備搬移，請５分鐘後查詢!');window.close();</script>");
                    }                    

                }
                catch (Exception ex)
                {
                    //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                    string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                    string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                    write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                    Response.Write("<script>alert('登入逾時');</script>");
                }
                finally
                {
                    this.link.DBClose();
                }
            }
        }

        #endregion

    }
}