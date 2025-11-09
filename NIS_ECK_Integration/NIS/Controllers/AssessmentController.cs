using Com.Mayaminer;
using Newtonsoft.Json;
using NIS.Data;
using NIS.Models;
using NIS.UtilTool;
using NIS.WebService;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text;

namespace NIS.Controllers
{
    public class AssessmentController : BaseController
    {
        private DBConnector link;
        private LogTool log;
        private CommData cd;
        private AssessmentPain apc;
        Assess ass_m = new Assess();

        public AssessmentController()
        {
            this.link = new DBConnector();
            this.log = new LogTool();
            this.cd = new CommData();
            this.apc = new AssessmentPain();
        }

        #region 高危再評估

        // 高危再評估
        public ActionResult AssessmentDanger(string starttime, string endtime)
        {
            if (Session["PatInfo"] != null)
            {
                string start = DateTime.Now.AddDays(-4).ToString("yyyy/MM/dd 00:00");
                string end = DateTime.Now.ToString("yyyy/MM/dd HH:mm");
                if (starttime != null && endtime != null)
                {
                    start = starttime;
                    end = endtime;
                }

                ViewBag.dt = ass_m.sel_danger_data_dt(ptinfo.FeeNo, "", start, end);
                ViewBag.start = start;
                ViewBag.end = end;
                ViewBag.userno = userinfo.EmployeesNo;
                return View();
            }
            Response.Write("<script>alert('請選擇病人!');window.location.href='../Function/Ais_Inquiry_Index';</script>");
            return new EmptyResult();
        }

        [HttpGet]
        public ActionResult Insert_Danger(string id)
        {
            if (id != null)
                ViewBag.dt = ass_m.sel_danger_data_dt(ptinfo.FeeNo, id, "", "");
            return View();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Insert_Danger(FormCollection form)
        {
            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;

            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("DANGER_ID", creatid("DANGER_DATA", userno, feeno, "0"), DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATNAME", userinfo.EmployeesName, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("RECORDTIME", form["txt_day"] + " " + form["txt_time"], DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("DISABILITY", form["disability"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("AWARENESS", form["awareness"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("ACTIVE", form["active"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("PISS", form["piss"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("STOOL", form["stool"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CARE_CHARACTERISTICS", form["care_characteristics"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CARE_RESOURCE", form["care_resource"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CARE_SPECIAL", form["care_special"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("TOTAL", form["total"], DBItem.DBDataType.Number));

            int erow = ass_m.DBExecInsert("NIS_DANGER_DATA", insertDataList);
            #region 拋轉跨團隊出備

            string DANGER_FLAG = "N";
            string operdate = DateTime.Now.AddYears(-1911).Year.ToString() + DateTime.Now.ToString("MMdd");
            string opertime = DateTime.Now.AddYears(-1911).ToString("HHmmss");
            if (form["disability"].ToString().Trim() == "3")
            {
                DANGER_FLAG = "Y";
            }
            string PA01 = form["rb_awareness"].ToString().Trim();
            if (PA01 == "3")
            {
                DANGER_FLAG = "Y";
            }
            string PA02 = form["rb_active"].ToString().Trim();
            if (PA02 == "3")
            {
                DANGER_FLAG = "Y";
            }
            string PA03 = form["rb_piss"].ToString().Trim();
            if (PA03 == "3")
            {
                DANGER_FLAG = "Y";
                if (form["piss"].ToString().Trim().Substring(0, 1) == "存")
                    PA03 = "3-1";
                if (form["piss"].ToString().Trim().Substring(0, 1) == "間")
                    PA03 = "3-2";
                if (form["piss"].ToString().Trim().Substring(0, 1) == "膀")
                    PA03 = "3-3";
            }
            string PA04 = form["rb_stool"].ToString().Trim();
            if (PA04 == "1")
            {
                if (form["stool"].ToString().Trim().Substring(0, 1) == "腹")
                    PA04 = "1-1";
                if (form["stool"].ToString().Trim().Substring(0, 1) == "便")
                    PA04 = "1-2";
            }
            if (PA04 == "3")
            {
                DANGER_FLAG = "Y";
            }
            string PA05 = form["rb_care_characteristics"].ToString().Trim();
            if (PA05 == "3")
            {
                DANGER_FLAG = "Y";
            }
            string PA06 = form["rb_care_resource"].ToString().Trim();
            if (PA06 == "0")
            {
                if (form["care_resource"].ToString().Trim().Substring(0, 1) == "獨")
                    PA06 = "0-1";
                if (form["care_resource"].ToString().Trim().Substring(0, 1) == "父")
                    PA06 = "0-2";
            }
            if (PA06 == "1")
            {
                if (form["care_resource"].ToString().Trim().Substring(0, 1) == "祖")
                    PA06 = "1-1";
            }
            if (PA06 == "2")
            {
                if (form["care_resource"].ToString().Trim().Substring(0, 1) == "僅")
                    PA06 = "2-1";
                if (form["care_resource"].ToString().Trim().Substring(0, 1) == "單")
                    PA06 = "2-2";
            }
            if (PA06 == "3")
            {
                DANGER_FLAG = "Y";
                if (form["care_resource"].ToString().Trim().Substring(0, 1) == "養")
                    PA06 = "3-1";
                if (form["care_resource"].ToString().Trim().Substring(0, 1) == "獨")
                    PA06 = "3-2";
            }
            string PA07 = "0";
            string PA07_1 = "N";
            string PA07_2 = "N";
            string PA07_3 = "N";
            string PA07_4 = "N";
            string PA07_5 = "N";
            string PA07_6 = "N";
            if (form["care_special"] != "")
            {
                PA07 = "3";
                DANGER_FLAG = "Y";
                if (form["care_special"].ToString().Trim().IndexOf("鼻胃管") > -1)
                    PA07_1 = "Y";
                if (form["care_special"].ToString().Trim().IndexOf("氣切") > -1)
                    PA07_2 = "Y";
                if (form["care_special"].ToString().Trim().IndexOf("壓瘡") > -1 || form["care_special"].ToString().Trim().IndexOf("壓傷") > -1)
                    PA07_3 = "Y";
                if (form["care_special"].ToString().Trim().IndexOf("腸造廔") > -1)
                    PA07_4 = "Y";
                if (form["care_special"].ToString().Trim().IndexOf("胃造廔") > -1)
                    PA07_5 = "Y";
                if (form["care_special"].ToString().Trim().IndexOf("其他特殊需求") > -1)
                    PA07_6 = "Y";
            }
            string DANGER_NUM = form["total"].ToString().Trim();

            if (int.Parse(DANGER_NUM) > 7)
            {
                DANGER_FLAG = "Y";
            }
            insertDataList.Clear();
            insertDataList.Add(new DBItem("FEE_NO", ptinfo.FeeNo, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("OPER_DATE", operdate, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("OPER_TIME", opertime, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("OPER_ID", userinfo.EmployeesNo, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("P00_TYPE", "A", DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("PA01", PA01, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("PA02", PA02, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("PA03", PA03, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("PA04", PA04, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("PA05", PA05, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("PA06", PA06, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("PA07", PA07, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("PA07_1", PA07_1, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("PA07_2", PA07_2, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("PA07_3", PA07_3, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("PA07_4", PA07_4, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("PA07_5", PA07_5, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("PA07_6", PA07_6, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("DANGER_NUM", DANGER_NUM, DBItem.DBDataType.Number));
            insertDataList.Add(new DBItem("DANGER_FLAG", DANGER_FLAG, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CASE_TYPE", "0", DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("OPEN_TYPE", "0", DBItem.DBDataType.String));
            try
            {
                ass_m.DBExecInsert("dps_danger_access", insertDataList);
            }
            catch (Exception ex)
            {
                this.log.saveLogMsg(ex.Message.ToString() + "dps_danger_access：tableid：出備高危再評估", "DBExecInsert");
            }
            #endregion
            if (erow > 0)
                Response.Write("<script>alert('新增成功!');window.location.href='AssessmentDanger';</script>");
            else
                Response.Write("<script>alert('新增失敗!');window.location.href='AssessmentDanger';</script>");

            return new EmptyResult();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Upd_Danger(FormCollection form)
        {
            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;
            string id = form["id"];
            string where = " DANGER_ID = '" + id + "' AND FEENO = '" + feeno + "' ";
            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("RECORDTIME", form["txt_day"] + " " + form["txt_time"], DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("DISABILITY", form["disability"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("AWARENESS", form["awareness"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("ACTIVE", form["active"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("PISS", form["piss"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("STOOL", form["stool"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CARE_CHARACTERISTICS", form["care_characteristics"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CARE_RESOURCE", form["care_resource"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CARE_SPECIAL", form["care_special"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("TOTAL", form["total"], DBItem.DBDataType.Number));

            int erow = ass_m.DBExecUpdate("NIS_DANGER_DATA", insertDataList, where);
            if (erow > 0)
                Response.Write("<script>alert('儲存成功!');window.location.href='AssessmentDanger';</script>");
            else
                Response.Write("<script>alert('儲存失敗!');window.location.href='AssessmentDanger';</script>");

            return new EmptyResult();
        }

        [HttpPost]
        public ActionResult Del_Danger(string id)
        {
            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;
            string where = " DANGER_ID = '" + id + "' AND FEENO = '" + feeno + "' ";
            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("DELETED", userno, DBItem.DBDataType.String));

            int erow = ass_m.DBExecUpdate("NIS_DANGER_DATA", insertDataList, where);

            if (erow > 0)
                Response.Write("<script>alert('刪除成功!');window.location.href='AssessmentDanger';</script>");
            else
                Response.Write("<script>alert('刪除失敗!');window.location.href='AssessmentDanger';</script>");
            return new EmptyResult();
        }

        #endregion

        #region 舊的評估單(入院、專科)

        /// <summary>
        /// 專科護理評估首頁
        /// </summary>
        /// <returns></returns>
        public ActionResult AssessmentSpecial()
        {
            return View();
        }

        /// <summary>
        /// 新生兒入院評估
        /// </summary>
        /// <returns></returns>
        public ActionResult NewBornAssessment()
        {
            return View();
        }

        /// <summary>
        /// 新生兒加護病房評估
        /// </summary>
        /// <returns></returns>
        public ActionResult NewBornAssessment2()
        {
            return View();
        }


        /// <summary>
        /// 婦產入院評估
        /// </summary>
        /// <returns></returns>
        public ActionResult ObstetricsAssessment()
        {
            return View();
        }

        /// <summary>
        /// 精神入院評估
        /// </summary>
        public ActionResult SpiritAssessment()
        {
            return View();
        }

        #endregion

        #region 新版評估單(入院、專科、每日)

        /// <summary> 護理評估單主表身 </summary>
        public ActionResult AssessmentPage()
        {
            if (Request["na_cate"] != null)
            {
                ViewData["na_cate"] = Request["na_cate"];
                ViewBag.Assessment = ptinfo.Assessment;
                string sqlstr = string.Empty;
                string na_cate = Request["na_cate"].ToString().Trim();

                try
                {

                    sqlstr = " SELECT * FROM SYS_NAITEM WHERE NA_CATE = '" + na_cate + "' ORDER BY NA_SORT ASC, NA_NAME ASC  ";
                    DataTable Dt = ass_m.DBExecSQL(sqlstr);

                    //取得評估類型
                    Dictionary<string, string> na_item_list = new Dictionary<string, string>();
                    if (Dt.Rows.Count > 0)
                    {
                        //na_item_list.Add(reader["na_type"].ToString().Trim(), reader["na_name"].ToString().Trim());
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            na_item_list.Add(Dt.Rows[i]["na_type"].ToString().Trim(), Dt.Rows[i]["na_name"].ToString().Trim());
                        }
                    }
                    ViewData["na_item_list"] = na_item_list;
                    //ViewBag.age = ptinfo.Age;

                    double birth = Math.Ceiling((DateTime.Now - ptinfo.Birthday).TotalDays);
                    if (birth <= 2505)
                    {
                        string strsql2 = "SELECT SEQ_NO FROM NIS_ASSESSMENTCHILD_MASTER WHERE STATUS ='Y' AND MINDAY <= " + birth + " AND MAXDAY >= " + birth;
                        DataTable dt = new DataTable();
                        this.link.DBExecSQL(strsql2, ref dt);
                        if (dt.Rows.Count > 0)
                        {
                            ViewBag.child_assess = dt.Rows[0][0].ToString().Trim();
                        }
                    }
                    return View();

                }
                catch (Exception ex)
                {
                    //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                    string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                    string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                    write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                    return RedirectToAction("AssessmentPage", new { @message = "參數錯誤" });
                }
            }
            else
            {
                return RedirectToAction("AssessmentPage", new { @message = "參數錯誤" });
            }
        }

        /// <summary> 護理評估表單內容 </summary>
        public ActionResult AssessmentContent()
        {
            try
            {
                if (Request["na_type"] != null)
                {
                    string na_type = Request["na_type"].ToString().Trim();
                    ViewData["na_type"] = na_type;

                    // 先取得版本
                    string sqlstr = " SELECT * FROM SYS_NAMAIN SN WHERE NA_TYPE= '" + na_type + "' AND ";
                    sqlstr += "NA_VERSION = (SELECT MAX(NA_VERSION) FROM SYS_NAMAIN WHERE NA_TYPE= SN.NA_TYPE AND NA_STATUS='C' ) ";
                    NursingAssessmentMain na_main_info = new NursingAssessmentMain();
                    DataTable Dt = ass_m.DBExecSQL(sqlstr);
                    if (Dt.Rows.Count > 0)
                    {
                        //na_main_info.na_id = reader["na_id"].ToString().Trim();
                        //na_main_info.na_iso = reader["na_iso"].ToString().Trim();
                        //na_main_info.na_desc = reader["na_desc"].ToString().Trim();
                        //na_main_info.na_version = reader["na_version"].ToString().Trim();
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {

                            na_main_info.na_id = Dt.Rows[i]["na_id"].ToString().Trim();
                            na_main_info.na_iso = Dt.Rows[i]["na_iso"].ToString().Trim();
                            na_main_info.na_desc = Dt.Rows[i]["na_desc"].ToString().Trim();
                            na_main_info.na_version = Dt.Rows[i]["na_version"].ToString().Trim();
                        }
                    }
                    ViewData["na_main_info"] = na_main_info;

                    if (na_main_info.na_id != null)
                    {

                        // 取得標籤
                        sqlstr = " SELECT * FROM SYS_NATAG WHERE NA_ID = '" + na_main_info.na_id + "' ORDER BY TAG_SORT ASC ";
                        List<NursingAssessmentTag> na_tag_info = new List<NursingAssessmentTag>();
                        Dt = ass_m.DBExecSQL(sqlstr);
                        if (Dt.Rows.Count > 0)
                        {

                            for (int i = 0; i < Dt.Rows.Count; i++)
                            {

                                int tag_sort = 0;
                                if (Dt.Rows[i]["tag_sort"].ToString().Trim() != "")
                                    tag_sort = int.Parse(Dt.Rows[i]["tag_sort"].ToString().Trim());

                                na_tag_info.Add(new NursingAssessmentTag()
                                {
                                    tag_name = Dt.Rows[i]["tag_name"].ToString().Trim(),
                                    tag_id = Dt.Rows[i]["tag_id"].ToString().Trim(),
                                    tag_help = Dt.Rows[i]["tag_help"].ToString().Trim(),
                                    tag_sort = tag_sort
                                });
                            }
                        }

                        ViewData["na_tag_info"] = na_tag_info;

                        List<NursingAssessmentDtl> na_dtl_list = new List<NursingAssessmentDtl>();
                        sqlstr = " SELECT * FROM SYS_NADTL WHERE NA_ID='" + na_main_info.na_id + "' ORDER BY DTL_SORT ASC ";
                        Dt = ass_m.DBExecSQL(sqlstr);

                        var DefaultValue = "";
                        if (Dt.Rows.Count > 0)
                        {
                            for (int i = 0; i < Dt.Rows.Count; i++)
                            {

                                int dtl_length = 0;
                                if (Dt.Rows[i]["dtl_length"] != null)
                                    dtl_length = int.Parse(Dt.Rows[i]["dtl_length"].ToString().Trim());
                                int dtl_sort = 0;
                                if (Dt.Rows[i]["dtl_sort"] != null)
                                    dtl_sort = int.Parse(Dt.Rows[i]["dtl_sort"].ToString().Trim());

                                DefaultValue = getDefaultValue(Dt.Rows[i]["dtl_title"].ToString().Trim(), Dt.Rows[i]["dtl_default_value"].ToString().Trim(), na_main_info.na_id);
                                na_dtl_list.Add(new NursingAssessmentDtl()
                                {
                                    na_id = Dt.Rows[i]["na_id"].ToString().Trim(),
                                    tag_id = Dt.Rows[i]["tag_id"].ToString().Trim(),
                                    dtl_id = Dt.Rows[i]["dtl_id"].ToString().Trim(),
                                    dtl_length = dtl_length,
                                    dtl_parent_id = Dt.Rows[i]["dtl_parent_id"].ToString().Trim(),
                                    dtl_child_hide = Dt.Rows[i]["dtl_child_hide"].ToString().Trim(),
                                    //如果有舊資料帶舊資料，如果沒舊資料帶預設值
                                    dtl_default_value = DefaultValue, //reader["dtl_default_value"].ToString().Trim(),
                                    dtl_help = Dt.Rows[i]["dtl_help"].ToString().Trim(),
                                    dtl_rear_word = Dt.Rows[i]["dtl_rear_word"].ToString().Trim(),
                                    dtl_show_value = Dt.Rows[i]["dtl_show_value"].ToString().Trim(),
                                    dtl_sort = dtl_sort,
                                    dtl_title = Dt.Rows[i]["dtl_title"].ToString().Trim(),
                                    dtl_type = Dt.Rows[i]["dtl_type"].ToString().Trim(),
                                    dtl_value = Dt.Rows[i]["dtl_value"].ToString().Trim(),
                                    dtl_must = Dt.Rows[i]["dtl_must"].ToString().Trim()
                                });
                            }
                        }

                        for (int i = 0; i < na_dtl_list.Count; i++)
                        {
                            string sql_date = "";
                            sql_date = " SELECT DTL_VALUE FROM NIS_DATA_NARECORD_DETAIL  ";
                            sql_date += " WHERE DTL_SNO = (SELECT DTL_SNO FROM NIS_DATA_NARECORD_MASTER WHERE FEE_NO = '" + ptinfo.FeeNo + "' AND NA_TYPE='" + na_type + "'";
                            sql_date += " AND RECORD_DATE = (SELECT MAX(RECORD_DATE) FROM NIS_DATA_NARECORD_MASTER WHERE FEE_NO = '" + ptinfo.FeeNo + "' AND NA_TYPE = '" + na_type + "')) ";
                            sql_date += " AND DTL_ID = '" + na_dtl_list[i].dtl_id.Trim() + "'";
                            DataTable dt = new DataTable();
                            link.DBExecSQL(sql_date, ref dt, true);
                            if (dt.Rows.Count > 0)
                            {
                                if (dt.Rows[0][0].ToString() != "")
                                    na_dtl_list[i].dtl_default_value = dt.Rows[0][0].ToString().Trim();
                            }
                        }

                        List<NA_View_Obj> na_dtl_info = new List<NA_View_Obj>();
                        if (na_dtl_list.Count > 0)
                        {
                            for (int t = 0; t <= na_tag_info.Count - 1; t++)
                            {
                                na_dtl_info.Add(new NA_View_Obj()
                                {
                                    tag_id = na_tag_info[t].tag_id,
                                    del_list = this.getDtlObj(ref na_dtl_list, na_tag_info[t].tag_id, "")
                                });
                            }

                            ViewData["na_dtl_info"] = na_dtl_info;
                        }
                    }
                    else
                    {
                        ViewData["na_info"] = "無評估資訊";
                    }
                }
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }

            return View();
        }

        /// <summary> 儲存護理評估_儲存 </summary>
        public ActionResult AssessmentSave()
        {

            // variable
            List<DBItem> masterItem = new List<DBItem>();
            string sqlstr = string.Empty;
            string[] save_dtl_id = Request["save_dtl_id"].ToString().Trim().Split(',');
            string na_id = Request["na_id"].ToString().Trim();
            string na_version = Request["na_version"].ToString().Trim();
            string na_type = Request["na_type"].ToString().Trim();
            string dtl_sno = SecurityTool.EncodeMD5(ptinfo.FeeNo.Trim() + na_id + na_version + na_type + DateTime.Now.ToString("yyyyMMdd"));
            IDataReader reader = null;

            try
            {
                // 主檔
                sqlstr = "select count(*) as na_count from nis_data_narecord_master where fee_no = '" + ptinfo.FeeNo.ToString() + "' ";
                sqlstr += " and na_id='" + na_id + "' and na_type='" + na_type + "' and dtl_sno='" + dtl_sno + "' ";
                this.link.DBExecSQL(sqlstr);
                int chk_master = int.Parse(reader["na_count"].ToString());

                // 主檔儲存項目
                masterItem.Add(new DBItem("fee_no", ptinfo.FeeNo.Trim(), DBItem.DBDataType.String));
                masterItem.Add(new DBItem("na_id", na_id, DBItem.DBDataType.String));
                masterItem.Add(new DBItem("na_type", na_type, DBItem.DBDataType.String));
                masterItem.Add(new DBItem("na_version", na_version, DBItem.DBDataType.String));
                masterItem.Add(new DBItem("record_date", DateTime.Now.ToString("yyyy/MM/dd"), DBItem.DBDataType.DataTime));
                masterItem.Add(new DBItem("dtl_sno", dtl_sno, DBItem.DBDataType.String));
                masterItem.Add(new DBItem("modify_user", userinfo.EmployeesNo, DBItem.DBDataType.String));
                masterItem.Add(new DBItem("modify_date", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                if (chk_master != 0)
                {
                    string whereCondition = " fee_no = '" + ptinfo.FeeNo.ToString() + "' and dtl_sno='" + dtl_sno + "'";
                    whereCondition += " and na_type='" + na_type + "' and na_id='" + na_id + "' ";
                    this.link.DBExecUpdate("nis_data_narecord_master", masterItem, whereCondition);
                }
                else
                {
                    masterItem.Add(new DBItem("create_user", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    masterItem.Add(new DBItem("create_date", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                    this.link.DBExecInsert("nis_data_narecord_master", masterItem);
                }
                // 明細
                sqlstr = " select * from nis_data_narecord_detail where dtl_sno ='" + dtl_sno + "' ";
                DataTable Dt = ass_m.DBExecSQL(sqlstr);
                List<string> detail_record = new List<string>();

                if (Dt.Rows.Count > 0)
                {
                    detail_record.Add(reader["dtl_id"].ToString());
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        detail_record.Add(Dt.Rows[i]["dtl_id"].ToString());
                    }

                }
                reader.Close();

                // 開始儲存明細
                for (int i = 0; i <= save_dtl_id.Length - 1; i++)
                {
                    List<DBItem> detailItem = new List<DBItem>();
                    // 如果此DTL_ID有值才做儲存
                    if (Request[save_dtl_id[i]] != null)
                    {
                        string dtl_id = save_dtl_id[i];
                        bool haveFlag = false;
                        for (int j = 0; j <= detail_record.Count - 1; j++)
                        {
                            if (detail_record[j] == save_dtl_id[i])
                            {
                                haveFlag = true;
                                break;
                            }
                        }
                        /* if (save_dtl_id[i] == "D0000000000000010227")
                         {
                             string test = Request[save_dtl_id[i]].ToString();
                             Response.Write(test);
                         }*/
                        //detailItem.Add(new DBItem("dtl_id", dtl_id, DBItem.DBDataType.String));
                        detailItem.Add(new DBItem("dtl_value", Request[save_dtl_id[i]].ToString(), DBItem.DBDataType.String));
                        if (haveFlag)
                        {
                            this.link.DBExecUpdate("nis_data_narecord_detail", detailItem, " dtl_sno='" + dtl_sno + "' and dtl_id ='" + dtl_id + "'");
                        }
                        else
                        {
                            detailItem.Add(new DBItem("dtl_id", dtl_id, DBItem.DBDataType.String));
                            detailItem.Add(new DBItem("dtl_sno", dtl_sno, DBItem.DBDataType.String));
                            this.link.DBExecInsert("nis_data_narecord_detail", detailItem);
                        }
                    }
                    detailItem = null;
                }


                //try
                //{
                //首次新增入院護理評估時才新增護理紀錄
                if (chk_master != 0)
                {
                    string content = string.Empty, ih_desc = "＿＿＿＿", ih_date = "＿＿＿＿";
                    string MedicalHistory = "＿＿＿＿＿";
                    string AllergyHistory = "＿＿＿＿＿";
                    string breathe_count = "＿＿＿＿＿";
                    string breathe_type = "＿＿＿＿＿";
                    string pulse_type = "＿＿＿＿＿";
                    string pulse_count = "＿＿＿＿＿";
                    string Hallucination = "＿＿＿＿＿";
                    InHistory ih = getLastDrag();
                    if (ih != null)
                    {
                        ih_desc = ih.Description.Trim();
                        ih_date = ih.indate.ToString("yyyy/MM/dd");
                    }

                    switch (na_type)
                    {
                        //入院護理評估(成人)
                        case "A":
                            content = "病人因" + getNaInfo(na_type, "入院原因") + "於" + getNaInfo(na_type, "入院日期") + "經由" + getNaInfo(na_type, "入院方式") + "入院，";
                            content += "意識狀態：E" + getNaInfo(na_type, "(E)睜眼反射") + "V" + getNaInfo(na_type, "(V)語言反射") + "M" + getNaInfo(na_type, "(M)語言反射") + "，";
                            content += "診斷為：" + ptinfo.ICD9_code1.Trim() + "，最近一次因" + ih_desc + "於" + ih_date + "住院";
                            if (getNaInfo(na_type, "高血壓") != "")
                                MedicalHistory = "高血壓 " + getNaInfo(na_type, "高血壓") + " 年、";
                            if (getNaInfo(na_type, "心臟病") != "")
                                if (MedicalHistory == "＿＿＿＿＿")
                                    MedicalHistory = "心臟病 " + getNaInfo(na_type, "心臟病") + " 年、";
                                else
                                    MedicalHistory += "心臟病 " + getNaInfo(na_type, "心臟病") + " 年、";
                            if (getNaInfo(na_type, "糖尿病") != "")
                                if (MedicalHistory == "＿＿＿＿＿")
                                    MedicalHistory = "糖尿病 " + getNaInfo(na_type, "糖尿病") + " 年、";
                                else
                                    MedicalHistory += "糖尿病 " + getNaInfo(na_type, "糖尿病") + " 年、";
                            if (getNaInfo(na_type, "氣喘") != "")
                                if (MedicalHistory == "＿＿＿＿＿")
                                    MedicalHistory = "氣喘 " + getNaInfo(na_type, "氣喘") + " 年";
                                else
                                    MedicalHistory += "氣喘 " + getNaInfo(na_type, "氣喘") + " 年";
                            content += "曾罹患 " + MedicalHistory + " 。";
                            if (getNaInfo_sub(na_type, "過敏史藥物") != "")
                                AllergyHistory = getNaInfo_sub(na_type, "過敏史藥物") + " ";
                            if (getNaInfo_sub(na_type, "過敏史食物") != "")
                                if (AllergyHistory == "＿＿＿＿＿")
                                    AllergyHistory = getNaInfo_sub(na_type, "過敏史食物") + " ";
                                else
                                    AllergyHistory += "、" + getNaInfo_sub(na_type, "過敏史食物") + " ";
                            content += "對 " + AllergyHistory + "過敏。";
                            if (getNaInfo(na_type, "呼吸") != "")
                                breathe_count = getNaInfo(na_type, "呼吸");
                            if (getNaInfo(na_type, "呼吸狀態") != "")
                                breathe_type = getNaInfo(na_type, "呼吸狀態");
                            if (getNaInfo(na_type, "心跳狀態") != "")
                                pulse_type = getNaInfo(na_type, "心跳狀態");
                            if (getNaInfo(na_type, "心跳") != "")
                                pulse_count = getNaInfo(na_type, "心跳");
                            content += "目前病人狀況呼吸次數： " + breathe_count + " 次/分" + breathe_type + "，心跳" + pulse_type + " " + pulse_count + " 次/分，＿＿＿＿＿。";
                            content += "給予病人/家屬入院護理及環境介紹，說明" + getNaInfo(na_type, "環境") + "，通知醫師前往診視";
                            break;
                        //入院護理評估(精神科)
                        case "S":
                            content = "病人因＿＿＿＿＿於" + getNaInfo(na_type, "入院日期") + "經由" + getNaInfo(na_type, "入院方式") + "入院，";
                            content += "外觀" + getNaInfo(na_type, "外觀：") + "，行為" + getNaInfo(na_type, "行為：") + "，情緒" + getNaInfo(na_type, "情緒：") + "，定向感" + getNaInfo(na_type, "定向感：");
                            if (getNaInfo(na_type, "幻覺狀況：") == "有")
                                Hallucination = "及 " + getNaInfo_sub(na_type, "幻覺狀況：") + "，";
                            content += "，具" + getNaInfo(na_type, "思想：") + "，" + getNaInfo_sub(na_type, "幻覺狀況：") + "病識感" + getNaInfo(na_type, "病識感：");
                            content += "，需注意" + getNaInfo_sub(na_type, "意外事件預防要點：") + "之意外事件之預防，＿";
                            content += "＿＿＿＿。給予病人/家屬入院護理及環境介紹，說明 ";
                            content += getNaInfo(na_type, "環境") + "，通知醫師前往診。";
                            break;
                        //入院護理評估(兒童)
                        case "C":
                            content = "病人因＿＿＿＿＿於" + getNaInfo(na_type, "入院日期") + "經由" + getNaInfo(na_type, "入院方式") + "入院，";
                            content += "意識狀態：(E)睜眼反射" + getNaInfo_sub_title(na_type, "(E)睜眼反射") + "、(V)語言反應" + getNaInfo_sub_title(na_type, "(V)語言反應") + "、(M)運動反射" + getNaInfo_sub_title(na_type, "(M)運動反射");
                            content += "，診斷為：" + ptinfo.ICD9_code1.Trim() + "，最近一次因" + ih_desc + "於" + ih_date + "住院。";
                            content += "曾罹患" + getNaInfo(na_type, "先天疾病史") + "、" + getNaInfo(na_type, "後天疾病史") + "，對 " + getNaInfo(na_type, "過敏藥物");
                            content += " 過敏。目前病人狀況呼吸次數 " + getNaInfo(na_type, "呼吸") + " 次";
                            content += "/分" + getNaInfo(na_type, "呼吸型態") + "，心跳 " + getNaInfo(na_type, "規律與否") + getNaInfo(na_type, "心跳") + "次/分，＿＿＿ ＿";
                            content += "＿。給予病人/家屬環境介紹，說明" + getNaInfo(na_type, "環境");
                            break;
                    }
                    string id = "Assessment" + DateTime.Now.ToString("yyyyMMddHHmmss");
                    //Insert_CareRecord(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "", content, "", "", "", "", 0);
                    Insert_CareRecord(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), id, "", content, "", "", "", "", "ASSESSMENT");
                }
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }
            return RedirectToAction("AssessmentResult", new { @message = "儲存成功", @na_type = na_type });
            //return RedirectToAction("AssessmentContent", new { @message = "儲存成功", @na_type = na_type });
        }

        //20190423 modified by chih
        public ActionResult SpecialistIndex()
        {
            #region 抓WS將新生兒資料帶入溝通表
            DateTime Now = DateTime.Now;
            var End = Now.ToString("yyyy-MM-dd 23:59:59");
            var Start = Now.AddDays(-30).ToString("yyyy-MM-dd 00:00:00");
            var sql = "SELECT * FROM OBS_NB WHERE NB_CHARTNO IS NULL AND CREATTIME BETWEEN to_date('" + Start + "','yyyy-MM-dd HH24:mi:ss') AND to_date('" + End + "','yyyy-MM-dd HH24:mi:ss') ";

            DataTable Dt = link.DBExecSQL(sql);
            if (Dt != null && Dt.Rows.Count > 0)
            {
                foreach (DataRow r in Dt.Rows)
                {
                    sql = "SELECT * FROM OBS_BABYLINK_DATA WHERE MOM_FEE_NO = '"
                       + r["FEENO"].ToString().Trim()
                       + "' AND BABY_SEQ = '" + r["NB_NAME"].ToString().Trim().Replace("新生兒", "") + "'";

                    DataTable Dt2 = link.DBExecSQL(sql);

                    if (Dt2 != null && Dt2.Rows.Count > 0)
                    {
                        foreach (DataRow s in Dt2.Rows)
                        {
                            //add by chih,20200507
                            var cdate = Convert.ToDateTime(s["CREATE_DATE"]);
                            DateTime dtnow = DateTime.Now;
                            TimeSpan ts = dtnow.Subtract(cdate);
                            if (Math.Abs(ts.Days) >= 30) {
                                continue;
                            }
                            //end add by chih,20200507

                            var seq = (Convert.ToInt32(Convert.ToChar(s["BABY_SEQ"])) - 64);

                            if (s["BABY_CHART_NO"].ToString() == "")
                            {
                                byte[] BaBylinklistByteCode = webService.GetBabylink(r["FEENO"].ToString());
                                if (BaBylinklistByteCode == null)
                                {
                                }
                                else
                                {
                                    string listJsonArray = CompressTool.DecompressString(BaBylinklistByteCode);
                                    Babylink[] babylink = JsonConvert.DeserializeObject<Babylink[]>(listJsonArray);
                                    var target = babylink.Where(x => Convert.ToInt32(x.NB_SEQ) == seq).FirstOrDefault();
                                    if (target != null)
                                    {
                                        List<DBItem> insertDataList = new List<DBItem>();
                                        insertDataList.Add(new DBItem("BABY_CHART_NO", target?.NB_CHARNO.ToString().Trim(), DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("BABY_FEE_NO", target?.NB_FEENO.ToString().Trim(), DBItem.DBDataType.String));

                                        string where = " MOM_FEE_NO = '" + r["FEENO"].ToString().Trim()
                                            + "' AND BABY_SEQ = '" + s["BABY_SEQ"].ToString().Trim() + "'";
                                        int erow = new Obstetrics().DBExecUpdate("OBS_BABYLINK_DATA", insertDataList, where);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            #endregion

            #region 將溝通表的資料帶入出生紀錄
            if (Dt != null && Dt.Rows.Count > 0)
            {
                foreach (DataRow r in Dt.Rows)
                {
                    //找到對應的新生兒病歷號
                    sql = "SELECT * FROM OBS_BABYLINK_DATA WHERE MOM_FEE_NO = '"
                        + r["FEENO"].ToString().Trim()
                        + "' AND BABY_SEQ = '" + r["NB_NAME"].ToString().Trim().Replace("新生兒", "") + "'";

                    DataTable Dt2 = link.DBExecSQL(sql);
                    if (Dt2 != null && Dt2.Rows.Count > 0)
                    {
                        foreach (DataRow r2 in Dt2.Rows)
                        {
                            List<DBItem> insertDataList = new List<DBItem>();
                            insertDataList.Add(new DBItem("NB_CHARTNO", r2["BABY_CHART_NO"].ToString().Trim(), DBItem.DBDataType.String));

                            string where = " FEENO = '" + r["FEENO"].ToString().Trim()
                                + "' AND NB_NAME = '" + r["NB_NAME"].ToString().Trim() + "'";
                            int erow = new Obstetrics().DBExecUpdate("OBS_NB", insertDataList, where);
                        }
                    }
                }
            }
            #endregion

            #region 回填皮膚接觸的新生兒病歷號
            var sk_sql = "SELECT * FROM OBS_SKTSK WHERE NB_CHARTNO IS NULL AND DELETED IS NULL";
            DataTable sk_dt = link.DBExecSQL(sk_sql);
            if (sk_dt != null && sk_dt.Rows.Count > 0)
            {
                var momfeeno = "";
                foreach (DataRow sk in sk_dt.Rows)
                {
                    byte[] listByteCode = webService.GetPatFeeNo(sk["PAT_FEENO"].ToString());
                    if (listByteCode == null) { }
                    else
                    {
                        string listJsonArray = CompressTool.DecompressString(listByteCode);
                        BabyLab[] patinfo = JsonConvert.DeserializeObject<BabyLab[]>(listJsonArray);
                        if (patinfo != null && patinfo.Count() > 0)
                            momfeeno = patinfo[0].FEE_NO;
                    }

                    //找到對應的新生兒病歷號
                    sql = "SELECT * FROM OBS_BABYLINK_DATA WHERE MOM_FEE_NO = '"
                        + momfeeno
                        + "' AND BABY_SEQ = '" + sk["SEQ_OF_NB"].ToString() + "'";

                    DataTable Dt2 = link.DBExecSQL(sql);
                    if (Dt2 != null && Dt2.Rows.Count > 0)
                    {
                        foreach (DataRow r2 in Dt2.Rows)
                        {
                            List<DBItem> insertDataList = new List<DBItem>();
                            insertDataList.Add(new DBItem("NB_CHARTNO", r2["BABY_CHART_NO"].ToString().Trim(), DBItem.DBDataType.String));

                            string where = " IID = '" + sk["IID"].ToString().Trim() + "'";
                            int erow = new Obstetrics().DBExecUpdate("OBS_SKTSK", insertDataList, where);
                        }
                    }
                }
            }


            #endregion

            if (Session["PatInfo"] != null)
            {
                Response.Write("<script>window.location.href='SpecialistIndex_Logged';</script>");
                return new EmptyResult();
            }
            else
                return View();
        }

        //有角色登入
        public ActionResult SpecialistIndex_Logged()
        {
            return View();
        }
        //20190423 modified by chih

        /// <summary> 取得最後一次診斷 </summary>
        public InHistory getLastDrag()
        {
            try
            {
                byte[] inHistoryByte = this.webService.GetInHistory(ptinfo.ChartNo.Trim());

                string inHistoryJson = CompressTool.DecompressString(inHistoryByte);
                List<InHistory> inHistoryObj = JsonConvert.DeserializeObject<List<InHistory>>(inHistoryJson);

                return inHistoryObj[inHistoryObj.Count - 1];
            }
            catch
            {
                return null;
            }
        }

        /// <summary> 取得最後一次診斷 </summary>
        public string GetHistory(string colnum)
        {
            try
            {
                byte[] inHistoryByte = this.webService.GetInHistory(ptinfo.ChartNo.Trim());

                string inHistoryJson = CompressTool.DecompressString(inHistoryByte);
                List<InHistory> inHistoryObj = JsonConvert.DeserializeObject<List<InHistory>>(inHistoryJson);
                var indate = (from a in inHistoryObj where a.IpdFlag == "C" select a.indate).Max();
                switch (colnum)
                {
                    case "indate":
                        return indate.ToString("yyyy/MM/dd HH:mm");
                    case "Description":
                        IEnumerable<string> tmpDescription = from a in inHistoryObj where a.IpdFlag == "C" && a.indate == indate select a.Description;
                        string Description = "";
                        foreach (string tmp in tmpDescription)
                        {
                            Description = tmp;
                        }
                        return Description;
                    case "count":
                        var count = (from a in inHistoryObj where a.IpdFlag == "C" select a).Count();
                        return count.ToString();
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary> 儲存護理評估細項_取得細項樹狀物件 </summary>
        private List<NursingAssessmentDtlObj> getDtlObj(ref List<NursingAssessmentDtl> inObj, string tag_id = "", string refId = "")
        {
            List<NursingAssessmentDtlObj> retObj = new List<NursingAssessmentDtlObj>();
            for (int j = 0; j <= inObj.Count - 1; j++)
            {
                if (tag_id == inObj[j].tag_id && inObj[j].dtl_parent_id == refId)
                {
                    string c_dtl_value = inObj[j].dtl_value;
                    if (inObj[j].dtl_type == "G")
                    {
                        c_dtl_value = getGroupId(inObj[j].dtl_id);
                    }

                    retObj.Add(new NursingAssessmentDtlObj()
                    {
                        na_id = inObj[j].na_id,
                        tag_id = inObj[j].tag_id,
                        dtl_id = inObj[j].dtl_id,
                        dtl_title = inObj[j].dtl_title,
                        dtl_child_hide = inObj[j].dtl_child_hide,
                        dtl_default_value = inObj[j].dtl_default_value,
                        dtl_help = inObj[j].dtl_help,
                        dtl_length = inObj[j].dtl_length,
                        dtl_parent_id = inObj[j].dtl_parent_id,
                        dtl_rear_word = inObj[j].dtl_rear_word,
                        dtl_show_value = inObj[j].dtl_show_value,
                        dtl_sort = inObj[j].dtl_sort,
                        dtl_type = inObj[j].dtl_type,
                        dtl_value = c_dtl_value,
                        dtl_must = inObj[j].dtl_must,
                        child_obj = getDtlObj(ref inObj, tag_id, inObj[j].dtl_id)
                    });
                }
                else
                {
                    continue;
                }
            }
            return retObj;

        }

        /// <summary> 取得群組下ID </summary>
        private string getGroupId(string dtl_id)
        {
            List<string> resultList = new List<string>();
            string sqlstr = string.Empty;
            sqlstr = "select dtl_id from sys_nadtl where dtl_parent_id='" + dtl_id + "' ";
            DataTable Dt = link.DBExecSQL(sqlstr);
            if (Dt.Rows.Count > 0)
            {
                for (int d = 0; d < Dt.Rows.Count; d++)
                {
                    resultList.Add(Dt.Rows[d]["dtl_id"].ToString().Trim());
                }
            }
            return string.Join("|", resultList.ToArray());
        }

        // 護理評估預設值
        private string getDefaultValue(string title, string value, string na_id)
        {
            string strReturn = "";
            switch (title)
            {
                case "入院日期":
                    strReturn = ptinfo.InDate.ToString("yyyy/MM/dd HH:mm");
                    break;
                case "住院次數":
                    strReturn = GetHistory("count");
                    break;
                case "最近一次時間":
                    strReturn = GetHistory("indate");
                    //if (strReturn == null)
                    //    strReturn = ptinfo.InDate.ToString("yyyy/MM/dd HH:mm");
                    break;
                case "住院原因":
                    strReturn = GetHistory("Description");
                    break;
                case "#口腔黏膜-顏色1":
                    string strsql = "";
                    strsql = "SELECT DTL_SNO,NA_ID FROM NIS_DATA_NARECORD_MASTER WHERE FEE_NO='" + ptinfo.FeeNo + "' AND CREATE_DATE = (SELECT MAX(CREATE_DATE) ";
                    strsql += " FROM NIS_DATA_NARECORD_MASTER WHERE FEE_NO='" + ptinfo.FeeNo + "' AND NA_TYPE IN (SELECT NA_TYPE FROM SYS_NAITEM A WHERE NA_CATE = 'I'))";

                    DataTable Dt = link.DBExecSQL(strsql);
                    if (Dt.Rows.Count > 0)
                    {
                        for (int d = 0; d < Dt.Rows.Count; d++)
                        {
                            string str_dtlsno = Dt.Rows[d]["DTL_SNO"].ToString();
                            string str_naid = Dt.Rows[d]["NA_ID"].ToString();

                            if (str_dtlsno != "")
                            { //有入評
                                strsql = "SELECT DTL_VALUE FROM NIS_DATA_NARECORD_DETAIL WHERE DTL_SNO='" + str_dtlsno + "' AND DTL_ID = ";
                                strsql += " (SELECT DTL_ID FROM SYS_NADTL WHERE NA_ID='" + str_naid + "' AND DTL_TITLE='口腔，黏膜') ";
                            }
                            else
                            { //無入評
                              //strsql = " SELECT ";
                            }
                        }
                    }

                    ass_m.DBExecSQL(strsql);
                    DataTable Dt2 = link.DBExecSQL(strsql);
                    if (Dt2.Rows.Count > 0)
                    {
                        for (int d = 0; d < Dt.Rows.Count; d++)
                        {
                            strReturn = Dt2.Rows[d]["DTL_VALUE"].ToString();
                        }
                    }
                    break;
                default:
                    strReturn = value;
                    break;
            }
            return strReturn;
        }
        #endregion

        #region 護理評估儲存結果預覽
        public ActionResult AssessmentResult()
        {
            string na_type = string.Empty;
            if (Request["na_type"].ToString().Trim() != "")
            {
                na_type = Request["na_type"].ToString().Trim();
            }

            ViewData["na_type"] = na_type;

            // 先取得版本
            string sqlstr = " SELECT * FROM SYS_NAMAIN SN WHERE NA_TYPE= '" + na_type + "' AND ";
            sqlstr += "NA_VERSION = (SELECT MAX(NA_VERSION) FROM SYS_NAMAIN WHERE NA_TYPE= SN.NA_TYPE AND NA_STATUS='C' ) ";
            NursingAssessmentMain na_main_info = new NursingAssessmentMain();
            this.link.DBExecSQL(sqlstr);
            DataTable Dt = link.DBExecSQL(sqlstr);
            if (Dt.Rows.Count > 0)
            {
                for (int d = 0; d < Dt.Rows.Count; d++)
                {
                    na_main_info.na_id = Dt.Rows[d]["na_id"].ToString().Trim();
                    na_main_info.na_iso = Dt.Rows[d]["na_iso"].ToString().Trim();
                    na_main_info.na_desc = Dt.Rows[d]["na_desc"].ToString().Trim();
                    na_main_info.na_version = Dt.Rows[d]["na_version"].ToString().Trim();
                }
            }
            ViewData["na_main_info"] = na_main_info;

            if (na_main_info.na_id != null)
            {

                // 取得標籤
                sqlstr = " SELECT * FROM SYS_NATAG WHERE NA_ID = '" + na_main_info.na_id + "' ORDER BY TAG_SORT ASC ";
                List<NursingAssessmentTag> na_tag_info = new List<NursingAssessmentTag>();

                Dt = null;
                Dt = link.DBExecSQL(sqlstr);
                if (Dt.Rows.Count > 0)
                {
                    int tag_sort = 0;
                    for (int d = 0; d < Dt.Rows.Count; d++)
                    {
                        if (Dt.Rows[d]["tag_sort"].ToString().Trim() != "")
                            tag_sort = int.Parse(Dt.Rows[d]["tag_sort"].ToString().Trim());

                        na_tag_info.Add(new NursingAssessmentTag()
                        {
                            tag_name = Dt.Rows[d]["tag_name"].ToString().Trim(),
                            tag_id = Dt.Rows[d]["tag_id"].ToString().Trim(),
                            tag_help = Dt.Rows[d]["tag_help"].ToString().Trim(),
                            tag_sort = tag_sort
                        });
                    }
                }
                ViewData["na_tag_info"] = na_tag_info;

                List<NursingAssessmentDtl> na_dtl_list = new List<NursingAssessmentDtl>();
                sqlstr = " SELECT * FROM SYS_NADTL WHERE NA_ID='" + na_main_info.na_id + "' ORDER BY DTL_SORT ASC ";

                Dt = null;
                Dt = link.DBExecSQL(sqlstr);
                if (Dt.Rows.Count > 0)
                {
                    for (int d = 0; d < Dt.Rows.Count; d++)
                    {
                        int dtl_length = 0;
                        if (Dt.Rows[d]["dtl_length"] != null)
                            dtl_length = int.Parse(Dt.Rows[d]["dtl_length"].ToString().Trim());
                        int dtl_sort = 0;
                        if (Dt.Rows[d]["dtl_sort"] != null)
                            dtl_sort = int.Parse(Dt.Rows[d]["dtl_sort"].ToString().Trim());

                        na_dtl_list.Add(new NursingAssessmentDtl()
                        {
                            na_id = Dt.Rows[d]["na_id"].ToString().Trim(),
                            tag_id = Dt.Rows[d]["tag_id"].ToString().Trim(),
                            dtl_id = Dt.Rows[d]["dtl_id"].ToString().Trim(),
                            dtl_length = dtl_length,
                            dtl_parent_id = Dt.Rows[d]["dtl_parent_id"].ToString().Trim(),
                            dtl_child_hide = Dt.Rows[d]["dtl_child_hide"].ToString().Trim(),
                            //如果有舊資料帶舊資料，如果沒舊資料帶預設值
                            dtl_default_value = Dt.Rows[d]["dtl_default_value"].ToString().Trim(),
                            dtl_help = Dt.Rows[d]["dtl_help"].ToString().Trim(),
                            dtl_rear_word = Dt.Rows[d]["dtl_rear_word"].ToString().Trim(),
                            dtl_show_value = Dt.Rows[d]["dtl_show_value"].ToString().Trim(),
                            dtl_sort = dtl_sort,
                            dtl_title = Dt.Rows[d]["dtl_title"].ToString().Trim(),
                            dtl_type = Dt.Rows[d]["dtl_type"].ToString().Trim(),
                            dtl_value = Dt.Rows[d]["dtl_value"].ToString().Trim()
                        });
                    }
                }

                for (int i = 0; i < na_dtl_list.Count; i++)
                {
                    string sql_date = "";
                    sql_date = " SELECT DTL_VALUE FROM NIS_DATA_NARECORD_DETAIL  ";
                    sql_date += " WHERE DTL_SNO = (SELECT DTL_SNO FROM NIS_DATA_NARECORD_MASTER WHERE FEE_NO = '" + ptinfo.FeeNo + "' AND NA_TYPE='" + na_type + "'";
                    sql_date += " AND RECORD_DATE = (SELECT MAX(RECORD_DATE) FROM NIS_DATA_NARECORD_MASTER WHERE FEE_NO = '" + ptinfo.FeeNo + "' AND NA_TYPE = '" + na_type + "')) ";
                    sql_date += " AND DTL_ID = '" + na_dtl_list[i].dtl_id.Trim() + "'";
                    DataTable dt = new DataTable();
                    link.DBExecSQL(sql_date, ref dt, true);
                    if (dt.Rows.Count > 0)
                    {
                        if (dt.Rows[0][0].ToString() != "")
                            na_dtl_list[i].dtl_default_value = dt.Rows[0][0].ToString().Trim();
                    }
                }

                List<NA_View_Obj> na_dtl_info = new List<NA_View_Obj>();
                if (na_dtl_list.Count > 0)
                {
                    for (int t = 0; t <= na_tag_info.Count - 1; t++)
                    {
                        na_dtl_info.Add(new NA_View_Obj()
                        {
                            tag_id = na_tag_info[t].tag_id,
                            del_list = this.getDtlObj(ref na_dtl_list, na_tag_info[t].tag_id, "")
                        });
                    }
                    ViewData["na_dtl_info"] = na_dtl_info;
                }
            }
            return View();
        }
        #endregion



        #region 兒童發展評估
        public ActionResult AssessmentChild(string seqno)
        {//判斷有無病人session
            if (Session["PatInfo"] != null)
            {
                if (seqno == null)
                {
                    ViewBag.flag = "ok";
                }
                else
                {
                    PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
                    string strsql = "", ls_count = "0";
                    ViewBag.flag = seqno;
                    ViewBag.seq_no = seqno;
                    //Check
                    strsql = "SELECT COUNT(*) AS COUNT FROM NIS_ASSESSMENTCHILD WHERE FEE_NO = '" + ptInfo.FeeNo + "' AND SEQ_NO = '" + seqno + "'";

                    DataTable Dt = link.DBExecSQL(strsql);
                    if (Dt.Rows.Count > 0)
                    {
                        for (int d = 0; d < Dt.Rows.Count; d++)
                        {
                            ls_count = Dt.Rows[d]["COUNT"].ToString();
                        }
                    }
                    if (ls_count != "0")
                    {
                        ViewBag.flag = "Yes";
                        return View();
                    }
                    //Load 
                    strsql = " SELECT A.GROWTH_DESC,A.MINDAY,A.MAXDAY,B.* FROM NIS_ASSESSMENTCHILD_MASTER A, NIS_ASSESSMENTCHILD_DTL B ";
                    strsql += " WHERE A.SEQ_NO = B.SEQ_NO AND A.SEQ_NO = '" + seqno + "' ORDER BY B.ITEM_NO";
                    DataTable dt_child = new DataTable();
                    link.DBExecSQL(strsql, ref dt_child);
                    if (dt_child.Rows.Count > 0)
                    {
                        ViewBag.dt_child = dt_child;
                        ViewBag.child_title = dt_child.Rows[0]["GROWTH_DESC"].ToString().Trim();
                    }
                    else
                    {
                        ViewBag.message = "Error";
                    }
                    int birth = int.Parse(Math.Floor((DateTime.Now - ptinfo.Birthday).TotalDays).ToString());
                    int birth_m = birth / 30;
                    int birth_d = birth - (birth_m * 30);

                    ViewBag.birthday = birth_m + "個月" + birth_d + "天";
                }
                return View();
            }
            else
            {
                Response.Write("<script>alert('請重新選擇病患');</script>");
                return new EmptyResult();
            }
        }

        public ActionResult AssessmentChild_PDF(string seqno, string feeno)
        {
            string strsql = " SELECT A.GROWTH_DESC,A.MINDAY,A.MAXDAY,B.* FROM NIS_ASSESSMENTCHILD_MASTER A, NIS_ASSESSMENTCHILD_DTL B ";
            strsql += " WHERE A.SEQ_NO = B.SEQ_NO AND A.SEQ_NO = '" + seqno + "' ORDER BY B.ITEM_NO";
            DataTable dt_child = new DataTable(), dt_answer = new DataTable();
            link.DBExecSQL(strsql, ref dt_child);
            if (dt_child.Rows.Count > 0)
            {
                ViewBag.dt_child = dt_child;
                ViewBag.child_title = dt_child.Rows[0]["GROWTH_DESC"].ToString().Trim();
            }
            strsql = "SELECT ASSESS_ITEM, ";
            strsql += "(SELECT * FROM (SELECT ITEM_VALUE FROM NIS_ASSESSMENTCHILD WHERE ";
            strsql += "FEE_NO = '" + feeno + "' ORDER BY MODIFY_DATE DESC) WHERE rownum <=1) VAL ";
            strsql += "FROM NIS_ASSESSMENTCHILD_DTL DTL WHERE ";
            strsql += "DTL.SEQ_NO = '" + seqno + "' ";
            link.DBExecSQL(strsql, ref dt_answer);
            if (dt_answer.Rows.Count > 0)
                ViewBag.dt_answer = dt_answer;

            byte[] ptinfoByteCode = this.webService.GetPatientInfo(feeno);
            string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
            PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);

            int birth = int.Parse(Math.Floor((DateTime.Now - Convert.ToDateTime(pi.Birthday)).TotalDays).ToString());
            int birth_m = birth / 30;
            int birth_d = birth - (birth_m * 30);

            ViewBag.birthday = birth_m + "個月" + birth_d + "天";
            ViewBag.seq_no = seqno;
            ViewBag.ptinfo = pi;
            return View();
        }

        //儲存
        public ActionResult AssessmentChild_Save()
        {
            PatientInfo ptInfo = (PatientInfo)Session["PatInfo"];
            string ls_count = "0", where = "";
            string child_title = Request["child_title"].ToString();
            string child_value = Request["child_value"].ToString();
            string c_version = Request["c_version"].ToString();
            string status = Request["child_status"].ToString();

            string strsql = "SELECT COUNT(*) AS COUNT FROM NIS_ASSESSMENTCHILD WHERE FEE_NO = '" + ptInfo.FeeNo + "' AND SEQ_NO = '" + c_version + "'";
            int effRow = 0;

            DataTable Dt = link.DBExecSQL(strsql);
            if (Dt.Rows.Count > 0)
            {
                ls_count = Dt.Rows[0]["COUNT"].ToString();
            }

            if (ls_count == "0")
            {
                List<DBItem> insertList = new List<DBItem>();
                insertList.Add(new DBItem("FEE_NO", ptInfo.FeeNo, DBItem.DBDataType.String));
                insertList.Add(new DBItem("SEQ_NO", c_version, DBItem.DBDataType.String));
                insertList.Add(new DBItem("ITEM_VALUE", child_value, DBItem.DBDataType.String));
                insertList.Add(new DBItem("UPD_OPER", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertList.Add(new DBItem("CREATE_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                insertList.Add(new DBItem("MODIFY_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                effRow += this.link.DBExecInsert("NIS_ASSESSMENTCHILD", insertList);
            }
            else
            {
                List<DBItem> updList = new List<DBItem>();
                updList.Add(new DBItem("ITEM_VALUE", child_value, DBItem.DBDataType.String));
                updList.Add(new DBItem("UPD_OPER", userinfo.EmployeesName, DBItem.DBDataType.String));
                updList.Add(new DBItem("MODIFY_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                where = "FEE_NO = '" + ptInfo.FeeNo + "' AND SEQ_NO = '" + c_version + "'";
                effRow += this.link.DBExecUpdate("NIS_ASSESSMENTCHILD", updList, where);
            }
            if (status == "Abnormal")
            {
                child_title = "兒童發展評估(" + child_title + ")";
                //Insert_CareRecord(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), ptinfo.FeeNo + c_version, child_title, "", "", "", "評估結果：疑似異常。", "", "C_AssessmentChild");
                Insert_CareRecord(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), ptinfo.FeeNo + c_version, "", "", "", "", "評估結果：疑似異常。", "", "C_AssessmentChild");
            }
            return RedirectToAction("AssessmentChild", new { @message = "儲存成功" });
        }

        //轉PDF頁面
        public ActionResult Html_To_Pdf(string url)
        {
            string strPath = @"C:\\wkhtmltopdf\\wkhtmltopdf.exe";
            string filename = DateTime.Now.ToString("yyyyMMddHHmmssfff") + url.Substring(url.IndexOf("feeno=") + 6, url.IndexOf("seqno=") - url.IndexOf("feeno=") - 7) + ".pdf";
            string tempPath = "C:\\inetpub\\NIS\\Images\\" + filename;
            string pdfpath = "../Images/" + filename;
            Process p = new Process();
            p.StartInfo.FileName = strPath;
            p.StartInfo.Arguments = url + " " + tempPath;
            p.StartInfo.UseShellExecute = true;
            p.Start();
            p.WaitForExit();
            Response.Write("<script>window.opener.open('Download_Pdf?filename=" + filename + "');window.close();</script>");

            return new EmptyResult();
        }

        public ActionResult Download_Pdf(string filename)
        {
            string tempPath = "C:\\inetpub\\NIS\\Images\\" + filename;

            FileInfo fileInfo = new FileInfo(tempPath);
            Response.Clear();
            Response.ClearContent();
            Response.ClearHeaders();
            Response.AddHeader("Content-Disposition", "attachment;filename=Report.pdf");
            Response.AddHeader("Content-Length", fileInfo.Length.ToString());
            Response.AddHeader("Content-Transfer-Encoding", "binary");
            Response.ContentType = "application/vnd.ms-excel";
            Response.ContentEncoding = Encoding.UTF8;
            Response.WriteFile(fileInfo.FullName);
            Response.Flush();
            Response.End();
            fileInfo.Delete();

            return new EmptyResult();
        }

        #endregion
    }
}

