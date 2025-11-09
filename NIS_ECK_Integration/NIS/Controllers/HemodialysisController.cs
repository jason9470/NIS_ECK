using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace NIS.Controllers
{
    public class HemodialysisController : BaseController
    {
        //
        // GET: /Hemodialysis/

        public ActionResult InjectionRecord()
        {
            return View();
        }

        public ActionResult TreatmentRecord()
        {
            return View();
        }

        public ActionResult InsertTreatment()
        {
            return View();
        }

    }
}
