using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NIS.Data;
using NIS.Models;
using NIS.UtilTool;
using System.Data;
using NIS.WebService;

namespace NIS.Controllers
{
    public class Complement_InsertController : BaseController
    {
        Complement_Insert C_Insert_m = new Complement_Insert();
        public Complement_InsertController()
        {
        }
        //審核
        public ActionResult Exam_list()
        {
            byte[] listByteCode = this.webService.GetCostCenterList();
            string listJsonArray = CompressTool.DecompressString(listByteCode);
            List<CostCenterList> costlist = Newtonsoft.Json.JsonConvert.DeserializeObject<List<CostCenterList>>(listJsonArray);
            Dictionary<string, string> cCostList = new Dictionary<string, string>();
            // 設定使用者預設護理站
            cCostList.Add("全部", "");
            for (int i = 0; i <= costlist.Count - 1; i++)
                cCostList.Add(costlist[i].CCCDescription.Trim(), costlist[i].CostCenterCode.Trim());

            ViewData["costlist"] = cCostList;
            ViewBag.dt_complement_list = C_Insert_m.sel_complement_list("", "NY");
            ViewBag.cost = userinfo.CostCenterCode.ToString().Trim();
            return View();
        }

        //審核
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Insert_Exam(FormCollection form)
        {
            string[] id_list = form["id_list"].Split(',');
            int erow = 0;
            for (int i = 0; i < id_list.Length; i++)
            {
                string status = form["status"];
                List<DBItem> insertDataList = new List<DBItem>();
                insertDataList.Add(new DBItem("EXAM_NO",  userinfo.EmployeesNo, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EXAM_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EXAM_TIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("EXAM_STATUS", status, DBItem.DBDataType.String));
                if (status == "Y")
                    insertDataList.Add(new DBItem("END_TIME", DateTime.Now.AddDays(3).ToString("yyyy/MM/dd 23:59:59"), DBItem.DBDataType.DataTime));
                else if (status == "N")
                    insertDataList.Add(new DBItem("END_TIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                string where = "C_ID = '" + id_list[i] + "' ";

                erow += C_Insert_m.DBExecUpdate("NIS_COMPLEMENT_INSERT", insertDataList, where);
            }
            if (erow == id_list.Length)
                Response.Write("<script>alert('儲存成功');window.location.href='Exam_list';</script>");
            else
                Response.Write("<script>alert('儲存失敗');window.location.href='Exam_list';</script>");

            return new EmptyResult();
        }

        //申請
        public ActionResult Apply_List()
        {
            ViewBag.dt_func_list = C_Insert_m.sel_func_list(userinfo.EmployeesNo);
            return View();
        }

        //申請
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Insert_Complement(FormCollection form)
        {
            string userno = userinfo.EmployeesNo;
            string id = base.creatid("Complement", userno, userno, "0");
            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("C_ID", id, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("APL_NO", userno, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("APL_NANE", userinfo.EmployeesName, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("APL_TIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("APL_REASON", form["txt_Reason"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("EXAM_STATUS", "NY", DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("APL_COST_ID", userinfo.CostCenterCode.ToString().Trim(), DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("APL_COST_NAME", (userinfo.CostCenterName != null) ? userinfo.CostCenterName.ToString().Trim() : "", DBItem.DBDataType.String));

            int erow = C_Insert_m.DBExecInsert("NIS_COMPLEMENT_INSERT", insertDataList);
            if (erow > 0)
                Response.Write("<script>alert('儲存成功');window.close();</script>");
            else
                Response.Write("<script>alert('儲存失敗');window.close();</script>");

            return new EmptyResult();
        }

    }
}
