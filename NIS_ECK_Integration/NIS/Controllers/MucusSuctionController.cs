using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using NIS.Data;
using NIS.Models;
using NIS.UtilTool;
using NIS.WebService;
using System.Data;
using System;
using System.Collections.Generic;
using System.Windows.Interop;

namespace NIS.Controllers
{
    public class MucusSuctionController : BaseController
    {
        // GET: /Delirium/
        private DBConnector link;

        //患者譫妄評估
        public MucusSuctionController()
        {
            this.link = new DBConnector();
        }

        #region --查詢列表--
        public ActionResult index()
        {//判斷有無病人session
            if (Session["PatInfo"] != null)
            {

                ViewBag.RootDocument = GetSourceUrl();
                ViewBag.feeno = ptinfo.FeeNo;
            }
            else
            {
                Response.Write("<script>alert('請重新選擇病患');</script>");
                return new EmptyResult();
            }
            return View();
        }

        public ActionResult ListData()
        {
            string feeno = ptinfo.FeeNo;
            ViewBag.dt = QueryData(feeno, Request["Pstart"], Request["Pend"]);
            return View();
        }

        private DataTable QueryData(string feeno, string Starttime, string endtime)
        {
            string StrSql = "SELECT * FROM MUCUSSUCTION_DATA WHERE FEENO='" + feeno + "' ";
            StrSql += "AND RECORD_TIME BETWEEN TO_DATE('" + Starttime + " 00:00:00','yyyy/mm/dd hh24:mi:ss') ";
            StrSql += "AND  TO_DATE('" + endtime + " 23:59:59','yyyy/mm/dd hh24:mi:ss') ";
            StrSql += "AND STATUS = 'Y'  ORDER BY  RECORD_TIME DESC, CREATE_TIME DESC";
            DataTable dt = this.link.DBExecSQL(StrSql);
            return dt;
        }
        #endregion

        #region --新增--
        public ActionResult Insert(string id)
        {
            if (id != null)
            {
                string feeno = ptinfo.FeeNo;
                DataTable dt = this.link.DBExecSQL("SELECT * FROM MUCUSSUCTION_DATA WHERE FEENO='" + feeno + "' AND SERIAL='" + id + "' AND STATUS = 'Y'");
                ViewBag.dt = dt;
            }
            return View();
        }

        [HttpPost]
        [ValidateInput(false)]
        public string Save(FormCollection form)
        {
            List<DBItem> insertDataList = new List<DBItem>();
            int erow = 0;
            bool isUpdate = false;

            try
            {
                string id = form["id"];
                string userno = userinfo.EmployeesNo;
                string feeno = ptinfo.FeeNo;
                string now = DateTime.Now.ToString("yyyy/MM/dd/ HH:mm:ss");
                string recordTime = form["record_day"] + " " + form["record_time"];
                string start = form["start_day"] + " " + form["start_time"];
                string end = form["end_day"] + " " + form["end_time"];
                string serial = creatid("MUCUSSUCTION_DATA", userno, feeno, "0");

                if (id == "")
                {
                    //新增
                    insertDataList.Clear();
                    insertDataList.Add(new DBItem("SERIAL", serial, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("RECORD_TIME", recordTime, DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("START_TIME", start, DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("END_TIME", end, DBItem.DBDataType.DataTime));
                    string color = "";
                    if (form["Sputum_color"] == "其他")
                    {
                        color = form["param_color_other_txt"].ToString();
                        insertDataList.Add(new DBItem("COLOR_OTHER", color, DBItem.DBDataType.String));

                    }
                    insertDataList.Add(new DBItem("COLOR", form["Sputum_color"], DBItem.DBDataType.String));
                    string nature = "";
                    if (form["Sputum_nature"] == "其他")
                    {
                        nature = form["param_naturer_other_txt"].ToString();
                        insertDataList.Add(new DBItem("NATURE_OTHER", nature, DBItem.DBDataType.String));
                    }
                    insertDataList.Add(new DBItem("NATURE", form["Sputum_nature"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("TIMES", form["Sputum_Times"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("VOIUME", form["Sputum_volume"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATE_TIME", now, DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("CREATE_USER", userno, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATE_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("MODIFY_TIME", now, DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("MODIFY_USER", userno, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("MODIFY_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));

                    erow = this.link.DBExecInsert("MUCUSSUCTION_DATA", insertDataList);

                    if(erow > 0 )
                    {
                        // 新增默許
                        List<Bill_RECORD> billDataList = new List<Bill_RECORD>();

                        Bill_RECORD billData = new Bill_RECORD();
                        billData.HO_ID = "8447041";
                        billData.COUNT = form["Sputum_Times"].ToString();

                        billDataList.Add(billData);

                        SaveBillingRecord(billDataList);

                        //新增護理紀錄
                        string content = "於" + start + "~" + end + "，予以抽痰" + form["Sputum_Times"] + "次，痰液：" + form["Sputum_volume"] + "，顏色：" + form["Sputum_color"] + "，性質：" + form["Sputum_nature"] + "。";
                        base.Insert_CareRecord(recordTime, serial, "抽痰紀錄", content, "", "", "", "", "MUCUSSUCTION");
                    }
               
                }
                else
                {
                    isUpdate = true;

                    //更新
                    insertDataList.Clear();
                    insertDataList.Add(new DBItem("MODIFY_TIME", now, DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("MODIFY_USER", userno, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("MODIFY_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("STATUS", "M", DBItem.DBDataType.String));
                    string where = "SERIAL = '" + id + "' AND STATUS = 'Y'";
                    erow = this.link.DBExecUpdate("MUCUSSUCTION_DATA", insertDataList, where);

                    insertDataList.Clear();
                    insertDataList.Add(new DBItem("SERIAL", id, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("RECORD_TIME", recordTime, DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("START_TIME", start, DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("END_TIME", end, DBItem.DBDataType.DataTime));
                    string color = "";
                    if (form["Sputum_color"] == "其他")
                    {
                        color = form["param_color_other_txt"].ToString();
                        insertDataList.Add(new DBItem("COLOR_OTHER", color, DBItem.DBDataType.String));
                    }

                    insertDataList.Add(new DBItem("COLOR", form["Sputum_color"], DBItem.DBDataType.String));

                    string nature = "";
                    if (form["Sputum_nature"] == "其他")
                    {
                        nature = form["param_naturer_other_txt"].ToString();
                        insertDataList.Add(new DBItem("NATURE_OTHER", nature, DBItem.DBDataType.String));
                    }

                    insertDataList.Add(new DBItem("NATURE", form["Sputum_nature"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("TIMES", form["Sputum_Times"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("VOIUME", form["Sputum_volume"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATE_TIME", now, DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("CREATE_USER", userno, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CREATE_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("MODIFY_TIME", now, DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("MODIFY_USER", userno, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("MODIFY_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                    erow = this.link.DBExecInsert("MUCUSSUCTION_DATA", insertDataList);
                    if(erow > 0 )
                    {
                        string content = "於" + start + "~" + end + "，予以抽痰" + form["Sputum_Times"] + "次，痰液：" + form["Sputum_volume"];
                        content += "，顏色：";
                        if (form["Sputum_color"] == "其他")
                        {
                            content +=  form["param_color_other_txt"].ToString();
                        }
                        else
                        {
                            content += form["Sputum_color"].ToString();

                        }
                        content += "，性質：";
                        if (form["Sputum_nature"] == "其他")
                        {
                            content += form["param_naturer_other_txt"].ToString() + "。";
                        }
                        else
                        {
                            content += form["Sputum_nature"].ToString() + "。";

                        }

                        base.Upd_CareRecord(recordTime, id, "抽痰紀錄", content, "", "", "", "", "MUCUSSUCTION");
                    }             
                }
            }
            catch(Exception ex)
            {
                Response.Write("<script>alert('新增失敗!');window.location.href='index';</script>");
            }

            if (erow > 0)
            {
                if (isUpdate)
                {
                    return "更新";
                }
                else
                {
                    return "新增";
                }
                
            }

            else
            {
                Response.Write("<script>alert('新增失敗!');window.location.href='index';</script>");
                return "N";

            }

        }
        #endregion

        #region --刪除--
        [HttpPost]
        public JsonResult Delete(string id)
        {
            DataTable dt = new DataTable();
            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;
            string where = " SERIAL = '" + id + "' AND FEENO = '" + feeno + "' ";
            List<DBItem> DelDataList = new List<DBItem>();
            DelDataList.Add(new DBItem("DELETE_USER", userno, DBItem.DBDataType.String));
            DelDataList.Add(new DBItem("DELETE_TIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
            DelDataList.Add(new DBItem("STATUS", "D", DBItem.DBDataType.String));
            int erow = this.link.DBExecUpdate("MUCUSSUCTION_DATA", DelDataList, where);

            if (erow > 0)
            {
                Del_CareRecord(id, "MUCUSSUCTION");
                return Json(1);
            }

            else
                return Json(0);
        }
        #endregion

        public int checkFirst ()
        {
            string setID = "";
            string sql = "SELECT * FROM MUCUSSUCTION_DATA WHERE FEENO = '" + ptinfo.FeeNo.Trim() + "' AND STATUS = 'Y'";
            DataTable dt = new DataTable();

            link.DBExecSQL(sql, ref dt);
            if(dt.Rows.Count == 1)
            {
                return 1;
            }

            return 0;
        }




        #region --列印--
        //public ActionResult PrintList(string feeno, string StartDate = "", string EndDate = "")
        //{
        //    PatientInfo pinfo = new PatientInfo();
        //    byte[] ByteCode = webService.GetPatientInfo(feeno);
        //    //病人資訊
        //    if (ByteCode != null)
        //        pinfo = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(ByteCode));
        //    ViewData["ptinfo"] = pinfo;
        //    ViewBag.StartDate = StartDate;
        //    ViewBag.EndDate = EndDate;
        //    ViewBag.FeeNo = feeno;
        //    ViewBag.RootDocument = GetSourceUrl();
        //    DataTable dt = QueryData(feeno, StartDate, EndDate);
        //    ViewBag.dt = dt;
        //    return View();
        //}
        #endregion
    }
}
