using System;
using System.Web.Mvc;
using System.Net.Http;
using System.Net;
using iTextSharp.text.pdf.security;
using DocumentFormat.OpenXml.Drawing.Charts;

namespace NIS.Controllers
{
    public class GatewayController : BaseController
    {
        public class DataObject
        {
            public bool IsSuccess { get; set; }
            public string MessageByNotSuccess { get; set; }
            public string URL { get; set; }
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult GetDestinationURL(string goDest)
        {
            //this.ControllerContext.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            var client = new HttpClient();
            switch (goDest)
            {
                case "WardORList":
                    try
                    {
                        var ChartNo = ptinfo == null ? "" : ptinfo.ChartNo;
                        var StartTime = DateTime.Now.ToString("o");
                        var EndTime = "";
                        var formURLFormat = "http://{0}/ECK_OPS_API/api/HIS_DataQuery/GetOPS_WardCheckList_URL?ChartNo={1}&StartTime={2}&EndTime={3}&NurseStationNo={4}&CallingUserID={5}";
                        string url = NIS.MvcApplication.iniObj.NisSetting.Connection.AISUrl;
                        var formURL = String.Format(formURLFormat, url, Url.Encode(ChartNo), Url.Encode(StartTime), Url.Encode(EndTime), Url.Encode(userinfo.CostCenterCode), Url.Encode(userinfo.EmployeesNo));
                        //var formURL = String.Format(formURLFormat, "10.169.8.11:8081", Url.Encode(ChartNo), Url.Encode(StartTime), Url.Encode(EndTime), Url.Encode(userinfo.CostCenterCode), Url.Encode(userinfo.EmployeesNo));  公司內測試用                      
                        var response = client.GetAsync(formURL).Result;
                        var resultJson = response.Content.ReadAsAsync<DataObject>().Result;
                        return Json(resultJson, JsonRequestBehavior.AllowGet);
                    }
                    catch (Exception e)
                    {
                    }
                    break;
                case "WardCheckList":
                    try
                    {
                        var resultJson = new DataObject();
                        var ChartNo = "";
                        var StartTime = "";
                        var EndTime = "";
                        if (ptinfo != null)
                        {
                            ChartNo = ptinfo == null ? "" : ptinfo.ChartNo;
                            StartTime = ptinfo == null ? "" : ptinfo.InDate.ToString("yyyy/MM/dd HH:mm:ss");
                            var CompareDate = new DateTime(1000, 1, 1);
                            if (!string.IsNullOrEmpty(ptinfo.OutDate.ToString()))
                            {
                                if (ptinfo.OutDate > CompareDate)
                                {
                                    EndTime = ptinfo.OutDate.ToString("yyyy/MM/dd HH:mm:ss");
                                }
                            }
                        }
                        else
                        {
                            resultJson.IsSuccess = false;
                            resultJson.MessageByNotSuccess = "請進行病人選取,再進行手術前護理單點擊";
                            return Json(resultJson, JsonRequestBehavior.AllowGet);
                        }
                        var formURLFormat = "http://{0}/ECK_OPS_API/api/HIS_DataQuery/GetOPS_WardCheckDetail_URL?ChartNo={1}&StartTime={2}&EndTime={3}&CallingUserID={4}";
                        string url = NIS.MvcApplication.iniObj.NisSetting.Connection.AISUrl;
                        var formURL = String.Format(formURLFormat, url, Url.Encode(ChartNo), Url.Encode(StartTime), Url.Encode(EndTime), Url.Encode(userinfo.EmployeesNo));
                        //var formURL = String.Format(formURLFormat, "10.169.8.11:8081", Url.Encode(ChartNo), Url.Encode(StartTime), Url.Encode(EndTime), Url.Encode(userinfo.EmployeesNo));  公司內測試用
                        var response = client.GetAsync(formURL).Result;
                        resultJson = response.Content.ReadAsAsync<DataObject>().Result;
                        return Json(resultJson, JsonRequestBehavior.AllowGet);
                    }
                    catch (Exception e)
                    {
                    }
                    break;
                default:
                    break;
            }

            return new EmptyResult();
        }

    }
}
