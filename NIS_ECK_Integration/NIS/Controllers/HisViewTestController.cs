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


namespace NIS.Controllers
{
    public class HisViewTestController : BaseController
    {
        public ActionResult Index()
        {
            if (Session["PatInfo"] != null)
            {
                PatientInfo pinfo = new PatientInfo();
                pinfo = (PatientInfo)Session["PatInfo"];
                UserInfo User = (UserInfo)Session["UserInfo"];
                ViewData["ptinfo"] = pinfo;
                ViewData["feeno"] = pinfo.FeeNo;

                ViewBag.ptinfo = pinfo;
                ViewBag.feeno = pinfo.FeeNo; 
                ViewBag.ChartNo = pinfo.ChartNo;
                return View();
            }
            else
            {
                Response.Write("<script>alert('請選擇病人!');</script>");
                return new EmptyResult();
            }
        }

        #region 語音測試區

        #endregion
    }
}