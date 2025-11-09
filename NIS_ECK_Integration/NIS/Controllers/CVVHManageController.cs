using System;
using System.Collections.Generic;
using System.Data;
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
    public class CVVHManageController : BaseController
    {
        private DBConnector link;
        private CVVH_Data cd;
        public CVVHManageController()
        {
            this.link = new DBConnector();
            this.cd = new CVVH_Data();
        }

        /// <summary>
        /// 主頁
        /// </summary>
        /// <param name="StartDate">開始時間</param>
        /// <param name="EndDate">結束時間</param>
        /// <returns></returns>
        #region CVVH-列表
        public ActionResult List(string StartDate = "", string EndDate = "")
        {
            if(StartDate == "")
            {
                StartDate = DateTime.Now.ToString("yyyy/MM/dd");
            }
            if(EndDate == "")
            {
                EndDate = DateTime.Now.ToString("yyyy/MM/dd");
            }
            //判斷有無病人session
            if (Session["PatInfo"] != null)
            {
                string feeno = ptinfo.FeeNo;
            ViewBag.dt = this.cd.CVVH_Main_List_Data(feeno, StartDate, EndDate);
            }
            else
            {
                Response.Write("<script>alert('請重新選擇病患');</script>");
                return new EmptyResult();
            }
            return View();
        }

        public ActionResult Insert_Record(FormCollection form)
        {
            if(Session["PatInfo"] != null)
            {
                string date = form["start_date"] + " " + form["start_time"];
                string txt_weight = form["popup_txt_weight"];
                string feeno = ptinfo.FeeNo;
                string username = userinfo.EmployeesName;
                string userno = userinfo.EmployeesNo;
                string id = base.creatid("CVVH_MASTER", userno, feeno, "0");
                string nowtime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                List<DBItem> insertDataList = new List<DBItem>();
                insertDataList.Add(new DBItem("RECORD_ID", id, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("RECORD_TIME", date, DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("WEIGHT", txt_weight, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATNAME", username, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATTIME", nowtime, DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDNAME", username, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDTIME", nowtime, DBItem.DBDataType.DataTime));
                int erow = this.link.DBExecInsert("CVVH_MASTER", insertDataList);
                RecordVitalsign(date, id, "bw", txt_weight);//帶到生命徵象
                if(erow > 0)
                {
                    Response.Write("<script>window.location.href = '../CVVHManage/Record?id=" + id + "&dtldate=" + form["start_date"] + "';</script>");
                }
                return new EmptyResult();
            }
            Response.Write("<script>alert('登入逾時');</script>");
            return new EmptyResult();
        }

        //刪除
        public string DelRecordBasic(string id)
        {
            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;
            string where = " RECORD_ID = '" + id + "' AND FEENO = '" + feeno + "' ";
            List<DBItem> DelDataList = new List<DBItem>();
            DelDataList.Add(new DBItem("DELETED", userno, DBItem.DBDataType.String));
            DelDataList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
            DelDataList.Add(new DBItem("UPDNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
            DelDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
            int erow = this.link.DBExecUpdate("CVVH_MASTER", DelDataList, where);
            bool D_erow = DEL_RecordVitalsign(id, "bw");
            //erow += base.Del_CareRecord(id, "Mood");
            if(erow > 0)
                return "Y";
            else
                return "N";
        }
        #endregion

        #region CVVH-新增明細
        public ActionResult Record(string id = "", string dtldate = "")
        {
            if(id != "" && dtldate != "")
            {
                string feeno = ptinfo.FeeNo;
                DataTable dt = this.cd.CVVH_Record_Data(id);
                #region 取統計值
                DataTable dtTotal = this.cd.CVVH_total(feeno, dtldate);
                if(dtTotal != null && dtTotal.Rows.Count > 0)
                {
                    ViewBag.beforeday = dtTotal.Rows[0]["beforeday"];
                    ViewBag.nextday = dtTotal.Rows[0]["nextday"];
                    ViewBag.white = (!string.IsNullOrEmpty(dtTotal.Rows[0]["morning"].ToString()) ? dtTotal.Rows[0]["morning"].ToString() : "0");
                    ViewBag.night = (!string.IsNullOrEmpty(dtTotal.Rows[0]["after"].ToString()) ? dtTotal.Rows[0]["after"].ToString() : "0");
                    ViewBag.longnight = (!string.IsNullOrEmpty(dtTotal.Rows[0]["night"].ToString()) ? dtTotal.Rows[0]["night"].ToString() : "0");
                }
                #endregion
                ViewBag.dtCount = dt.Rows.Count;
                ViewBag.dt = dt;
                ViewBag.id = id;
                ViewBag.dtldate = dtldate;
                ViewBag.FeeNo = feeno;
                ViewBag.RootDocument = GetSourceUrl();
                return View();
            }
            Response.Write("<script>alert('登入逾時');window.location.href = '../CVVHManage/List'</script>");
            return new EmptyResult();
        }

        public string InsertItem(FormCollection form)
        {
            if(Session["PatInfo"] != null)
            {
                string date = form["record_start_date"] + " " + form["record_start_time"];
                string feeno = ptinfo.FeeNo;
                string username = userinfo.EmployeesName;
                string userno = userinfo.EmployeesNo;
                string MRecordid = form["MRecordid"];
                string id = base.creatid("CVVH_DTL_DATA", userno, feeno, "0");
                string nowtime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                List<DBItem> insertDataList = new List<DBItem>();
                insertDataList.Add(new DBItem("DATA_ID", id, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("DATA_TIME", date, DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("BLOOD_FLOW_RATE", form["txt_blood_flow_rate"], DBItem.DBDataType.Number));
                insertDataList.Add(new DBItem("VENOUS_PRESSURE", form["txt_venous_pressure"], DBItem.DBDataType.Number));
                insertDataList.Add(new DBItem("AIR_DETECTOR", form["rb_air_detector"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CLOTS_IN_VENOUS_TRAP", form["rb_clots_in_venous_trap"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("HEPARIN", form["txt_heparin"], DBItem.DBDataType.Number));
                insertDataList.Add(new DBItem("I_REPLACE_FLUID", form["txt_replace_fluid_ab"], DBItem.DBDataType.Number));
                insertDataList.Add(new DBItem("I_FLUSHES", form["txt_flushes"], DBItem.DBDataType.Number));
                insertDataList.Add(new DBItem("O_UF", form["txt_uf"], DBItem.DBDataType.Number));
                insertDataList.Add(new DBItem("IO_TOTAL", form["txt_io_total"], DBItem.DBDataType.Number));
                insertDataList.Add(new DBItem("RECORD_ID", MRecordid, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATNAME", username, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATTIME", nowtime, DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDNAME", username, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDTIME", nowtime, DBItem.DBDataType.DataTime));
                int erow = this.link.DBExecInsert("CVVH_DTL_DATA", insertDataList);
                if(erow > 0)
                {
                    return "Y";
                }
                else
                {
                    return "N";
                }
                //Response.Write("<script>window.location.href = '../CVVHManage/Record?id=" + MRecordid + "&dtldate=" + date + "';</script>");
            }
            Response.Write("<script>alert('登入逾時');</script>");
            return "N";
        }

        public ActionResult Edit(FormCollection form)
        {
            if(Session["PatInfo"] != null)
            {
                string date = form["record_start_date"] + " " + form["record_start_time"];
                string username = userinfo.EmployeesName;
                string userno = userinfo.EmployeesNo;
                string MRecordid = form["MRecordid"];
                string nowtime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                List<DBItem> UpDataList = new List<DBItem>();
                UpDataList.Add(new DBItem("DATA_TIME", date, DBItem.DBDataType.DataTime));
                UpDataList.Add(new DBItem("BLOOD_FLOW_RATE", form["txt_blood_flow_rate"], DBItem.DBDataType.Number));
                UpDataList.Add(new DBItem("VENOUS_PRESSURE", form["txt_venous_pressure"], DBItem.DBDataType.Number));
                UpDataList.Add(new DBItem("AIR_DETECTOR", form["rb_air_detector"], DBItem.DBDataType.String));
                UpDataList.Add(new DBItem("CLOTS_IN_VENOUS_TRAP", form["rb_clots_in_venous_trap"], DBItem.DBDataType.String));
                UpDataList.Add(new DBItem("HEPARIN", form["txt_heparin"], DBItem.DBDataType.Number));
                UpDataList.Add(new DBItem("I_REPLACE_FLUID", form["txt_replace_fluid_ab"], DBItem.DBDataType.Number));
                UpDataList.Add(new DBItem("I_FLUSHES", form["txt_flushes"], DBItem.DBDataType.Number));
                UpDataList.Add(new DBItem("O_UF", form["txt_uf"], DBItem.DBDataType.Number));
                UpDataList.Add(new DBItem("IO_TOTAL", form["txt_io_total"], DBItem.DBDataType.Number));
                UpDataList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
                UpDataList.Add(new DBItem("UPDNAME", username, DBItem.DBDataType.String));
                UpDataList.Add(new DBItem("UPDTIME", nowtime, DBItem.DBDataType.DataTime));
                string strsql = "DATA_ID='" + form["hid_recordid"] + "' AND RECORD_ID='" + MRecordid + "' ";
                int erow = this.link.DBExecUpdate("CVVH_DTL_DATA", UpDataList, strsql);
                if(erow > 0)
                {
                    Response.Write("<script>alert('修改成功');</script>");
                }
                else
                {
                    Response.Write("<script>alert('修改失敗');</script>");
                }
                Response.Write("<script>window.location.href = '../CVVHManage/Record?id=" + MRecordid + "&dtldate=" + date + "';</script>");
            }
            Response.Write("<script>alert('登入逾時');</script>");
            return new EmptyResult();
        }

        //刪除
        public string DelRecordDtl(string id, string dtl_id)
        {
            string userno = userinfo.EmployeesNo;
            string where = " RECORD_ID = '" + id + "' AND DATA_ID = '" + dtl_id + "'";
            List<DBItem> DelDataList = new List<DBItem>();
            DelDataList.Add(new DBItem("DELETED", userno, DBItem.DBDataType.String));
            DelDataList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
            DelDataList.Add(new DBItem("UPDNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
            DelDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
            int erow = this.link.DBExecUpdate("CVVH_DTL_DATA", DelDataList, where);
            if(erow > 0)
                return "Y";
            else
                return "N";
        }

        //明細班別帶入護理記錄
        public string TakeCareRecord()
        {
            string[] Ptype = Request["Ptype"].TrimEnd(',').Split(',');
            string[] values = Request["values"].TrimEnd(',').Split(',');
            string date = Request["Pdate"];
            string feeno = ptinfo.FeeNo;
            string userno = userinfo.EmployeesNo;
            string id = "", str = "", type = "";
            int erow = 0;
            for(int i = 0;i < Ptype.Length;i++)
            {
                switch(Ptype[i])
                {
                    case "0":
                        str = "24小時 CVVH TOTAL I/O ：" + values[i] + "ml";
                        type = Ptype[i];
                        break;
                    case "1":
                        str = "白班 CVVH TOTAL I/O：" + values[i] + "ml";
                        type = Ptype[i];
                        break;
                    case "2":
                        str = "小夜班 CVVH TOTAL I/O：" + values[i] + "ml";
                        type = Ptype[i];
                        break;
                    case "3":
                        str = "大夜班 CVVH TOTAL I/O：" + values[i] + "ml";
                        type = Ptype[i];
                        break;
                }
                id = base.creatid("CVVH_DATA", userno, feeno, Ptype[i]);
                erow = base.Insert_CareRecord(date, id, "Record CVVH I/O", "", "", str, "", "", "CVVH_DATA");
            }
            if(erow > 0)
                return "Y";
            else
                return "N";
        }

        //檢查該"日期"是否有新增過主檔
        public string CheckDate()
        {
            string feeno = ptinfo.FeeNo;
            string date = Convert.ToDateTime(Request["date"]).ToString("yyyy-MM-dd");
            DataTable dt = this.link.DBExecSQL("SELECT * FROM  CVVH_MASTER WHERE 0=0 AND  FEENO='" + feeno + "' AND DELETED IS NULL AND TO_CHAR(RECORD_TIME, 'YYYY-MM-DD')  = '" + date + "'");
            
            if(dt != null && dt.Rows.Count > 0)
            {
                return dt.Rows[0]["RECORD_ID"].ToString();
            }
            else
            {
                return "N";
            }

        }
        #endregion

        /// <summary>
        /// 輸入體重到生命徵象
        /// </summary>
        /// <param name="createtime">建立時間</param>
        /// <param name="vstype">型態:體重</param>
        /// <param name="vs_record">數值</param>
        /// <returns></returns>
        public bool RecordVitalsign(string createtime, string vs_id, string vstype = "bw", string vs_record = "0")
        {
            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;
            vs_record = vs_record + "|kg";
            createtime = Convert.ToDateTime(createtime).ToString("yyyy/MM/dd HH:mm:00");
            //string vs_id = feeno + "_" + Convert.ToDateTime(createtime).ToString("yyyyMMddHHmm");
            //string vs_id ="CVVH_"+ feeno + "_" + Convert.ToDateTime(createtime).ToString("yyyyMMddHHmm");
          
            List<DBItem> vs_data = new List<DBItem>();
            vs_data.Add(new DBItem("fee_no", feeno, DBItem.DBDataType.String));
            vs_data.Add(new DBItem("vs_id", vs_id, DBItem.DBDataType.String));
            vs_data.Add(new DBItem("vs_item", vstype, DBItem.DBDataType.String));
            vs_data.Add(new DBItem("vs_record", vs_record, DBItem.DBDataType.String));
            vs_data.Add(new DBItem("create_user", userno, DBItem.DBDataType.String));
            vs_data.Add(new DBItem("modify_user", userno, DBItem.DBDataType.String));
            vs_data.Add(new DBItem("create_date", createtime.Trim(), DBItem.DBDataType.DataTime));
            vs_data.Add(new DBItem("modify_date", createtime.Trim(), DBItem.DBDataType.DataTime));
            vs_data.Add(new DBItem("PLAN", "N", DBItem.DBDataType.String));
            int erow = this.link.DBExecInsert("DATA_VITALSIGN", vs_data);
            
            if(erow > 0)
                return true;
            else
                return false;
        }

        public bool DEL_RecordVitalsign(string vs_id, string vstype = "bw")
        {
            string feeno = ptinfo.FeeNo;
            //string vs_id ="CVVH_"+ feeno + "_" + Convert.ToDateTime(createtime).ToString("yyyyMMddHHmm");
            string where = " VS_ID='" + vs_id + "' AND FEE_NO='" + feeno + "' AND VS_ITEM='" + vstype + "' ";
            int erow = this.link.DBExecDelete("DATA_VITALSIGN", where);
            if(erow > 0)
                return true;
            else
                return false;
        }

        /// <summary>
        /// 列印
        /// </summary>
        /// <param name="feeno"></param>
        /// <param name="dtldate"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult PrintList(string feeno, string dtldate, string id)
        {
            PatientInfo pinfo = new PatientInfo();
            byte[] ByteCode = webService.GetPatientInfo(feeno);
            //病人資訊
            if(ByteCode != null)
                pinfo = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(ByteCode));
            ViewData["ptinfo"] = pinfo;
            
            DataTable dt = this.cd.CVVH_Record_Data(id);
            ViewBag.dt = dt;
            return View();
        }
    }
}
