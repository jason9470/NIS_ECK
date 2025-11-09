using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace NIS.Controllers
{
    public class Daily_AssessmentController : Controller
    {
        //
        // GET: /Daily_Assessment/

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Insert()
        {
            List<SelectListItem> listitem = new List<SelectListItem>();
            listitem.Add(new SelectListItem { Text = "PC/AC", Value = "" });
            listitem.Add(new SelectListItem { Text = "VC/AC", Value = "" });
            listitem.Add(new SelectListItem { Text = "PRVC", Value = "" });
            listitem.Add(new SelectListItem { Text = "PC/SIMV+PS", Value = "" });
            listitem.Add(new SelectListItem { Text = "VC/SIMV+PS", Value = "" });
            listitem.Add(new SelectListItem { Text = "PRVC/SIMV+PS", Value = "" });
            listitem.Add(new SelectListItem { Text = "PSV", Value = "" });
            listitem.Add(new SelectListItem { Text = "CPAP", Value = "" });
            listitem.Add(new SelectListItem { Text = "Ext-CPAP", Value = "" });
            listitem.Add(new SelectListItem { Text = "APRV", Value = "" });
            listitem.Add(new SelectListItem { Text = "Bi-vent", Value = "" });
            listitem.Add(new SelectListItem { Text = "Duo PAP", Value = "" });
            listitem.Add(new SelectListItem { Text = "BIPAP", Value = "" });
            listitem.Add(new SelectListItem { Text = "ASV", Value = "" });
            listitem.Add(new SelectListItem { Text = "S/T mode", Value = "" });
            listitem.Add(new SelectListItem { Text = "S mode", Value = "" });
            listitem.Add(new SelectListItem { Text = "Time mode", Value = "" });
            listitem.Add(new SelectListItem { Text = "NIV/PC", Value = "" });
            listitem.Add(new SelectListItem { Text = "NIV/PS", Value = "" });
            listitem.Add(new SelectListItem { Text = "其他", Value = "" });
            ViewData["list_vitalsign_opt17"] = listitem;
            return View();
        }

        public ActionResult Mood()
        {
            return View();
        }

        public ActionResult Body()
        {
            return View();
        }

        public ActionResult Conventional()
        {
            return View();
        }

    }
}
