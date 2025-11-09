using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using NIS.Data;
using NIS.Models;
using NIS.UtilTool;
using NIS.WebService;

namespace NIS.Controllers
{
    public class TempController : BaseController
    {
        private CommData cd;
        private DBConnector link;

        public TempController()
        {
            this.cd = new CommData();
            this.link = new DBConnector();
        }
        //
        // GET: /Temp/
        public ActionResult List()
        {
            return View();
        }


        public ActionResult Record()
        {
            return View();
        }

        //轉出照護摘要單
        public ActionResult Leave_Referral()
        {
            return View();
        }

        #region 病人清單
        ////病人清單頁面--可刪
        //public ActionResult PatientList()
        //{
        //    List<string> cost_code = new List<string>();
        //    this.GetUnitDDL(cost_code);
        //    return View();
        //}

        ///// <summary> 病人清單 </summary>(我的病人)列表取得  
        //public ActionResult PatList()
        //{
        //    List<TransList> tdList = new List<TransList>();

        //    if(Request["mode"] != null)
        //    {
        //        byte[] ptByteCode = null;

        //        if(Request["mode"].ToString() == "clist")
        //            ptByteCode = this.nis.GetPatientList(Request["costcode"].ToString());
        //        else
        //            ptByteCode = this.nis.GetPatientListByBedList(GetMyPatientList());
        //        if(ptByteCode != null)
        //        {
        //            string ptJsonArr = CompressTool.DecompressString(ptByteCode);
        //            List<PatientList> patList = JsonConvert.DeserializeObject<List<PatientList>>(ptJsonArr);
        //            for(int y = 0; y < patList.Count; y++)
        //            {
        //                byte[] ptinfoByteCode = this.nis.GetPatientInfo(patList[y].FeeNo);
        //                if(ptinfoByteCode != null)
        //                {
        //                    string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
        //                    PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
        //                    tdList.Add(new TransList(pi, null, null, "", "", ""));
        //                    pi = null;
        //                }
        //            }
        //        }
        //        ViewData["tdList"] = tdList;
        //        return View();
        //    }
        //    return new EmptyResult();
        //}

        ///// <summary> 取得我的病人床位清單 </summary>
        //private string[] GetMyPatientList()
        //{
        //    IDataReader reader = null;
        //    List<string> retArr = new List<string>();
        //    string sqlstr = " SELECT BED_NO FROM DATA_DISPATCHING WHERE ";
        //    sqlstr += " RESPONSIBLE_USER = '" + userinfo.EmployeesNo + "' AND SHIFT_DATE = TO_DATE(TO_CHAR(SYSDATE,'yyyy/MM/dd'), 'yyyy/MM/dd') ";

       
        //    this.link.DBExecSQL(sqlstr, ref reader);
        //    if(reader != null)
        //    {
        //        while(reader.Read())
        //        {
        //            retArr.Add(reader["bed_no"].ToString().Trim());
        //        }
        //    }
        
        //    return retArr.ToArray();
        //}

        //#region 設定使用者預設護理站
        //private void GetUnitDDL(List<string> cost_code)
        //{
        //    byte[] listByteCode = this.nis.GetCostCenterList();
        //    string listJsonArray = CompressTool.DecompressString(listByteCode);
        //    List<CostCenterList> costlist = JsonConvert.DeserializeObject<List<CostCenterList>>(listJsonArray);
        //    List<SelectListItem> cCostList = new List<SelectListItem>();
        //    //第三順位_否則使用者歸屬單位
        //    string set_cost = userinfo.CostCenterCode.Trim();
        //    //第一順位_使用者有選擇過
        //    if(Request["cost_code"] != null)
        //        set_cost = Request["cost_code"];
        //    //第二順位_派班表有_以第一筆為優先
        //    else if(cost_code.Count > 0)
        //        set_cost = cost_code[0];

        //    for(int i = 0; i < costlist.Count; i++)
        //    {
        //        bool select = false;
        //        if(set_cost == costlist[i].CostCenterCode.Trim())
        //            select = true;
        //        cCostList.Add(new SelectListItem()
        //        {
        //            Text = costlist[i].CCCDescription.Trim(),
        //            Value = costlist[i].CostCenterCode.Trim(),
        //            Selected = select
        //        });
        //    }

        //    ViewData["costlist"] = cCostList;
        //}
        //#endregion
        #endregion

        //027出院準備服務
        public ActionResult DischargePlanningServices()
        {
            return View();
        }
        //031交班
        public ActionResult Kardex()
        {
            return View();
        }
        //003轉出照護摘要單OutCareSummaryReceipt
        public ActionResult OutCareSummaryReceipt()
        {
            return View();
        }
        //
        public ActionResult T004()
        {
            return View();
        }
        //
        public ActionResult T005()
        {
            return View();
        }
    }
}
