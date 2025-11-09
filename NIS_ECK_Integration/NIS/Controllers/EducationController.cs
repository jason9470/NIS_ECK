using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NIS.Data;
using NIS.Models;
using NIS.UtilTool;
using NIS.WebService;
using Newtonsoft.Json;
using System.Data;

namespace NIS.Controllers
{
    public class EducationController : BaseController
    {
        private CommData cd;
        private Education edu_m;
        private DBConnector link;
        private DBConnector2 link2;

        //建構子
        public EducationController()
        {
            this.cd = new CommData();
            this.edu_m = new Education();
            this.link = new DBConnector();
            this.link2 = new DBConnector2();
        }

        //衛教首頁
        public ActionResult List()
        {//判斷有無病人session
            if (Session["PatInfo"] != null)
            {
                try
                {
                    DataTable dt = edu_m.sel_health_education(ptinfo.FeeNo, "", "");
                    dt.Columns.Add("Creat_User");
                    dt.Columns.Add("Score_User");
                    if (dt.Rows.Count > 0)
                    {
                        foreach (DataRow r in dt.Rows)
                        {
                            if (r["CREATNO"].ToString() != "")
                            {
                                string Creat_User = r["CREATNO"].ToString();
                                byte[] listByteCode = webService.UserName(Creat_User);
                                if (listByteCode != null)
                                {
                                    string listJsonArray = CompressTool.DecompressString(listByteCode);
                                    UserInfo Creat_User_name = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                                    r["Creat_User"] = Creat_User_name.EmployeesName;
                                }
                            }
                        }
                        foreach (DataRow r in dt.Rows)
                        {
                            if (r["SCORENO"].ToString() != "")
                            {
                                string Score_User = r["SCORENO"].ToString();
                                byte[] listByteCode_ = webService.UserName(Score_User);
                                if (listByteCode_ != null)
                                {
                                    string listJsonArray_ = CompressTool.DecompressString(listByteCode_);
                                    UserInfo Score_User_name = JsonConvert.DeserializeObject<UserInfo>(listJsonArray_);
                                    r["Score_User"] = Score_User_name.EmployeesName;
                                }
                            }
                        }
                    }
                    ViewBag.dt = dt;
                    ViewBag.FeeNo = ptinfo.FeeNo;
                    ViewBag.RootDocument = GetSourceUrl();
                }
                catch (Exception ex)
                {
                    //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                    string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                    string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                    write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                }
                finally
                {
                    this.link.DBClose();
                }
            }
            else
            {
                Response.Write("<script>alert('請重新選擇病患');</script>");
                return new EmptyResult();
            }
            return View();
        }

        //列印衛教總表
        public ActionResult List_pdf(string feeno, string id_list, string select)
        {
            List<string> idList = new List<string>();
            if (!string.IsNullOrWhiteSpace(id_list))
                idList = id_list.Split(',').ToList();
            PatientInfo pinfo = new PatientInfo();
            byte[] ByteCode = webService.GetPatientInfo(feeno);
            //病人資訊
            if (ByteCode != null)
                pinfo = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(ByteCode));
            ViewData["ptinfo"] = pinfo;


            DataTable dt = edu_m.sel_health_education(feeno, "", "");
            dt.Columns.Add("Creat_User");
            dt.Columns.Add("Score_User");
            if (dt.Rows.Count > 0)
            {
                foreach (DataRow r in dt.Rows)
                {
                    if (idList.Count == 0 || idList.Exists(x => x == r["EDU_ID"].ToString()))
                    {
                        if (r["CREATNO"].ToString() != "")
                        {
                            string Creat_User = r["CREATNO"].ToString();
                            byte[] listByteCode = webService.UserName(Creat_User);
                            if (listByteCode != null)
                            {
                                string listJsonArray = CompressTool.DecompressString(listByteCode);
                                UserInfo Creat_User_name = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                                r["Creat_User"] = Creat_User_name.EmployeesName;
                            }
                        }
                        if (r["SCORENO"].ToString() != "")
                        {
                            string Score_User = r["SCORENO"].ToString();
                            byte[] listByteCode_ = webService.UserName(Score_User);
                            if (listByteCode_ != null)
                            {
                                string listJsonArray_ = CompressTool.DecompressString(listByteCode_);
                                UserInfo Score_User_name = JsonConvert.DeserializeObject<UserInfo>(listJsonArray_);
                                r["Score_User"] = Score_User_name.EmployeesName;
                            }
                        }
                    }
                    else
                        r.Delete();
                }
                dt.AcceptChanges();
            }
            ViewBag.dt = dt;
            ViewBag.Select = select;
            ViewBag.RootDocument = GetSourceUrl();
         
            return View();
        }

        //新增衛教_選擇衛教項目
        public ActionResult Sel_Item(string mode, string key)
        {
            Function func_m = new Function();
            ViewBag.mode = mode;
            ViewData["division"] = this.cd.getSelectItem("health_education", "division");
            ViewData["branch_division"] = this.cd.getSelectItem("health_education", "branch_division");
            ViewBag.dt = func_m.sel_type("health_education", "division");
            if (key != null)
                ViewBag.key = key;
            return View();
        }
        
        //新增"其他"衛教項目
        public ActionResult Insert_Item(string name)
        {
            string id = base.creatid("HE_ITEM" , userinfo.EmployeesNo, ptinfo.FeeNo, "0");

            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("ITEM_ID", id, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("NAME", name, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("OTHER", "1", DBItem.DBDataType.String));
            int erow = edu_m.DBExecInsert("HEALTH_EDUCATION_ITEM_DATA", insertDataList);

            if (erow > 0)
                Response.Write("<script>window.location.href='Comply?item=" + id + "';</script>");
            else
                Response.Write("<script>alert('新增失敗');window.location.href='Sel_Item';</script>");

            return new EmptyResult();
        }

        //刪除"其他"衛教項目
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Del_Item(FormCollection form)
        {
            if (form["del_ID"] != null)
            {
                string[] id = form["del_ID"].ToString().Split(',');
                string where = " ITEM_ID IN ('" + String.Join("','", id) + "')";
                int erow = edu_m.DBExecDelete("HEALTH_EDUCATION_ITEM_DATA", where);
            }

            return new EmptyResult();
        }

        //衛教項目列表
        public ActionResult Partial_Item_List(string mode, string column, string key)
        {
            Function func_m = new Function();
            ViewBag.mode = mode;
            ViewData["division"] = this.cd.getSelectItem("health_education", "division");
            if (column != null && key != null)
            {
                if (key != "")
                {
                    if (column == "CATEGORY_ID")
                        ViewBag.dt = func_m.sel_health_education_item(column, "", key, "Y");
                    else
                        ViewBag.dt = func_m.sel_health_education_item("", column, key, "Y");
                }
            }

            return View();
        }

        //執行衛教
        [HttpGet]
        public ActionResult Comply(string ids,string temp="")
        {
            if (Request["item"] != null)
            {
                DataTable dt_item = new DataTable();
                string[] id = Request["item"].ToString().Split(',');
                for (int i = 0; i < id.Length; i++)
                    dt_item = edu_m.sel_item(dt_item, id[i]);
                ViewBag.dt_item = dt_item;
            }
            else if (ids != null)
            {
                string[] id = ids.Split(',');
                string edu_id = "'" + String.Join("','", id) + "'";
                ViewBag.dt_edu = edu_m.sel_health_education(ptinfo.FeeNo, edu_id, "");
                ViewBag.temp = temp;
            }
            else
                return RedirectToAction("Sel_Item");
            return View();
        }

        //新增執行衛教
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Insert_Comply(FormCollection form)
        {
            try
            {
            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;
            string[] itemid = form["item_id"].ToString().Split(',');
            int erow = 0;

            for (int i = 0; i < itemid.Length; i++)
            {
                string id = base.creatid("EDUCATION_DATA", userno, feeno, i.ToString());
                string date = form["txt_day"].ToString().Split(',').GetValue(i) + " " + form["txt_time"].ToString().Split(',').GetValue(i);
                List<DBItem> insertDataList = new List<DBItem>();
                insertDataList.Add(new DBItem("EDU_ID", id, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("RECORDTIME", date, DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("ITEMID", itemid[i], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("OBJECT", form["object"].ToString().Split(',').GetValue(i).ToString(), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("OBJECT_NAME", form["name_object"].ToString().Split(',').GetValue(i).ToString(), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ACCOMPANY_NAME", form["name_accompany"].ToString().Split(',').GetValue(i).ToString(), DBItem.DBDataType.String));
                //insertDataList.Add(new DBItem("NATIONALITY", form["nationality"].ToString().Split(',').GetValue(i).ToString(), DBItem.DBDataType.String));
                //insertDataList.Add(new DBItem("EDUCATION", form["education"].ToString().Split(',').GetValue(i).ToString(), DBItem.DBDataType.String));
                //insertDataList.Add(new DBItem("MOTIVE", form["motive"].ToString().Split(',').GetValue(i).ToString(), DBItem.DBDataType.String));
                //insertDataList.Add(new DBItem("LANGUAGE", form["language"].ToString().Split(',').GetValue(i).ToString(), DBItem.DBDataType.String));
                //insertDataList.Add(new DBItem("OBSTACLE", form["obstacle"].ToString().Split(',').GetValue(i).ToString(), DBItem.DBDataType.String));
                //insertDataList.Add(new DBItem("OBSTACLE_REPLACE", form["txt_ck_list1_replace"].ToString().Split(',').GetValue(i).ToString(), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("DESCRIPTION", form["description"].ToString().Split(',').GetValue(i).ToString(), DBItem.DBDataType.String));
                //insertDataList.Add(new DBItem("DESCRIPTION_REPLACE", form["txt_ck_list2_replace"].ToString().Split(',').GetValue(i).ToString(), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UNDERSTAND", form["txt_instruct"].ToString().Split(',').GetValue(i).ToString(), DBItem.DBDataType.String));//備註 
                insertDataList.Add(new DBItem("INSTRUCT", form["item"].ToString().Split(',').GetValue(i).ToString(), DBItem.DBDataType.String));
                int e = link2.DBExecInsert("HEALTH_EDUCATION_DATA", insertDataList);

                if (e > 0)
                {
                    string content = "以" + form["description"].ToString().Split(',').GetValue(i).ToString().Replace('|', '、') + "方式向病人" + form["object"].ToString().Split(',').GetValue(i).ToString().Replace("99|", "");
                    content += "教導" + form["item_name"].ToString().Split(',').GetValue(i) + "，";
                    if (form["txt_instruct"].ToString().Split(',').GetValue(i).ToString() != "")
                        content += form["txt_instruct"].ToString().Split(',').GetValue(i) + "。";
                    content += "衛教內容：" + form["item"].ToString().Split(',').GetValue(i).ToString().Replace('|', '、') + "。";

                    this.Insert_CareRecord(date + ":01", id, "執行護理指導", "", "", "", content, "", "Education");
                    erow += e;
                }
            }

            if (erow > 0)
                Response.Write("<script>alert('新增成功');window.location.href='List';</script>");
            else
                Response.Write("<script>alert('新增失敗');window.location.href='List';</script>");

            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }
            finally
            {
                this.link2.DBClose();
            }
            return new EmptyResult();
        }

        //更新執行衛教
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Upd_Comply(FormCollection form)
        {
            try
            {
            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;
            string[] itemid = form["item_id"].ToString().Split(',');

            int erow = 0;

            for (int i = 0; i < itemid.Length; i++)
            {
                List<DBItem> insertDataList = new List<DBItem>();
                insertDataList.Add(new DBItem("RECORDTIME", form["txt_day"].ToString().Split(',').GetValue(i) + " " + form["txt_time"].ToString().Split(',').GetValue(i), DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("ITEMID", itemid[i], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("OBJECT", form["object"].ToString().Split(',').GetValue(i).ToString(), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("OBJECT_NAME", form["name_object"].ToString().Split(',').GetValue(i).ToString(), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ACCOMPANY_NAME", form["name_accompany"].ToString().Split(',').GetValue(i).ToString(), DBItem.DBDataType.String));
                //insertDataList.Add(new DBItem("NATIONALITY", form["nationality"].ToString().Split(',').GetValue(i).ToString(), DBItem.DBDataType.String));
                //insertDataList.Add(new DBItem("EDUCATION", form["education"].ToString().Split(',').GetValue(i).ToString(), DBItem.DBDataType.String));
                //insertDataList.Add(new DBItem("MOTIVE", form["motive"].ToString().Split(',').GetValue(i).ToString(), DBItem.DBDataType.String));
                //insertDataList.Add(new DBItem("LANGUAGE", form["language"].ToString().Split(',').GetValue(i).ToString(), DBItem.DBDataType.String));
                //insertDataList.Add(new DBItem("OBSTACLE", form["obstacle"].ToString().Split(',').GetValue(i).ToString(), DBItem.DBDataType.String));
                //insertDataList.Add(new DBItem("OBSTACLE_REPLACE", form["txt_ck_list1_replace"].ToString().Split(',').GetValue(i).ToString(), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("DESCRIPTION", form["description"].ToString().Split(',').GetValue(i).ToString(), DBItem.DBDataType.String));
                //insertDataList.Add(new DBItem("DESCRIPTION_REPLACE", form["txt_ck_list2_replace"].ToString().Split(',').GetValue(i).ToString(), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UNDERSTAND", form["txt_instruct"].ToString().Split(',').GetValue(i).ToString(), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("INSTRUCT", form["item"].ToString().Split(',').GetValue(i).ToString(), DBItem.DBDataType.String));

                erow += link2.DBExecUpdate("HEALTH_EDUCATION_DATA", insertDataList, " EDU_ID = '" + form["edu_id"].ToString().Split(',').GetValue(i) + "' ");

                //20150702 衛教更新連動護理記錄
                if (erow > 0)
                {
                    string content = "以" + form["description"].ToString().Split(',').GetValue(i).ToString().Replace('|', '、') + "方式向病人" + form["object"].ToString().Split(',').GetValue(i).ToString().Replace("99|", "");
                    content += "教導" + form["item_name"].ToString().Split(',').GetValue(i) + "，" ;
                    if (form["txt_instruct"].ToString().Split(',').GetValue(i).ToString() != "")
                        content +=  form["txt_instruct"].ToString().Split(',').GetValue(i) + "。";
                    content += "衛教內容：" + form["item"].ToString().Split(',').GetValue(i).ToString().Replace('|', '、') + "。";

                    this.Upd_CareRecord(form["txt_day"].ToString().Split(',').GetValue(i) + " " + form["txt_time"].ToString().Split(',').GetValue(i), form["edu_id"].ToString().Split(',').GetValue(i).ToString(), "執行護理指導", "", "", "", content, "", "Education");

                    erow += erow;
                }
            }

            if (erow > 0)
                Response.Write("<script>alert('儲存成功');window.location.href='List';</script>");
            else
                Response.Write("<script>alert('儲存失敗');window.location.href='List';</script>");
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }
            finally
            {
                this.link2.DBClose();
            }

            return new EmptyResult();
        }

        //衛教評值主畫面
        public ActionResult Score()
        {
            string[] id = Request["ids"].ToString().Split(',');
            string edu_id = "'" + String.Join("','", id) + "'";
            ViewBag.dt = edu_m.sel_health_education(ptinfo.FeeNo, edu_id, "");
            return View();
        }

        //儲存衛教評值
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Insert_Score(FormCollection form)
        {
            string[] id = form["id"].ToString().Split(',');
            string[] result = form["result"].ToString().Split(',');
            int erow = 0;

            for (int i = 0; i < id.Length; i++)
            {
                if (result[i].Split('|').GetValue(0).ToString() != "稍後評值")
                {
                    string date = form["txt_day"].ToString().Split(',').GetValue(i) + " " + form["txt_time"].ToString().Split(',').GetValue(i);
                    List<DBItem> insertDataList = new List<DBItem>();
                    insertDataList.Add(new DBItem("SCORE_NAME", form["name"].ToString().Split(',').GetValue(i).ToString(), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("SCORENO", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("SCORE_TIME", date, DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("SCORE_RESULT", form["result"].ToString().Split(',').GetValue(i).ToString(), DBItem.DBDataType.String));
                    int e = edu_m.DBExecUpdate("HEALTH_EDUCATION_DATA", insertDataList, " EDU_ID = '" + id[i] + "' ");

                    if (e > 0)
                    {
                        string content = "追蹤" + form["time"].ToString().Split(',').GetValue(i) + "向病人" + form["object"].ToString().Split(',').GetValue(i);
                        content += "所教導" + form["item_name"].ToString().Split(',').GetValue(i) + "，病人" + form["object"].ToString().Split(',').GetValue(i);
                        content += "對於衛教內容" + form["result"].ToString().Split(',').GetValue(i).ToString().Replace('|', '，') + "。";
                        this.Insert_CareRecord(date + ":59", id[i]+"_R", "護理指導評值", "", "", "", "", content, "Education");
                        erow += e;
                    }
                }
                else
                    erow++;
            }

            if (erow > 0)
                Response.Write("1");
            else
                Response.Write("0");

            return new EmptyResult();
        }

        //刪除衛教評值
        [HttpPost]
        public ActionResult Del_Score(string edu_id)
        {
           
            int erow = 0;
                    List<DBItem> insertDataList = new List<DBItem>();
                    insertDataList.Add(new DBItem("SCORE_NAME", "", DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("SCORENO", "", DBItem.DBDataType.String));                    
                    insertDataList.Add(new DBItem("SCORE_RESULT", "", DBItem.DBDataType.String));
                    int e = edu_m.DBExecUpdate("HEALTH_EDUCATION_DATA", insertDataList, " EDU_ID = '" + edu_id + "' ");
                    bool IfOK = edu_m.DBExec("UPDATE HEALTH_EDUCATION_DATA SET SCORE_TIME=NULL WHERE EDU_ID='" + edu_id  + "'");
                    if (e > 0 && IfOK)
                    {
                        erow = Del_CareRecord(edu_id +"_R", "Education");
                        Response.Write("1");
                    }
                    else
                        Response.Write("0");
            return new EmptyResult();
        }

        //刪除衛教
        [HttpPost]
        public ActionResult Del_Comply()
        {
            string[] id = Request["ids"].ToString().Split(',');
            string edu_id = "'" + String.Join("','", id) + "'";
            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("DELETED", userinfo.EmployeesNo, DBItem.DBDataType.String));
            int erow = edu_m.DBExecUpdate("HEALTH_EDUCATION_DATA", insertDataList, " EDU_ID IN (" + edu_id + ") ");

            if (erow > 0)
            {
                Response.Write("<script>alert('刪除成功');window.location.href='List';</script>");
                for (int z = 0; z < id.Length; z++)
                    erow = Del_CareRecord(id[z], "Education");
            }
            else
                Response.Write("<script>alert('刪除失敗');window.location.href='List';</script>");

            return new EmptyResult();
        }


        public string SetCount() 
        {
            string edu_name = Request["edu_name"].ToString();
            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("NURSES_NO", userinfo.EmployeesNo.Trim(), DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("NURSES_UNIT", userinfo.CostCenterCode.Trim(), DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CLICK_DATE", DateTime.Now.ToString("yyyy/MM/dd"), DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CLICK_TIME", DateTime.Now.ToString("HH:mm:ss"), DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("DIAGNOSIS", ptinfo.ICD9_code1.Trim(), DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("BED_NO", ptinfo.BedNo.Trim(), DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("EDU_NAME", edu_name, DBItem.DBDataType.String));
            int erow = edu_m.DBExecInsert("COUNT_CLICK_TABLE", insertDataList);
            if(erow > 0)
                return "1";
            else
                return "0";
        }
    }
}