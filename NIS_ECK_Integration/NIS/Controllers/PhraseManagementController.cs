using System.Collections.Generic;
using System.Web.Mvc;
using NIS.Models;
using System.Data;
using NIS.Data;
using NIS.UtilTool;
using Newtonsoft.Json;

namespace NIS.Controllers
{

    public class PhraseManagementController : BaseController
    {
        private CareRecord care_record_m;

        public PhraseManagementController()
        {
            this.care_record_m = new CareRecord();
        }

        #region 片語

        //片語_節點
        public ActionResult Phrase(string type, string txtid, string srchval = "")
        {
            ViewBag.type = type;
            ViewBag.txtid = (txtid == null) ? "" : txtid;
            ViewBag.srchval = srchval;
            ViewBag.userno = userinfo.EmployeesNo;

            //片語類型 - 單位
            byte[] listByteCode = webService.GetCostCenterList();
            string listJsonArray = CompressTool.DecompressString(listByteCode);
            List<CostCenterList> costlist = JsonConvert.DeserializeObject<List<CostCenterList>>(listJsonArray);
            List<SelectListItem> cCostList = new List<SelectListItem>();
            cCostList.Add(new SelectListItem()
            {
                Text = "請選擇",
                Value = "-1",
            });
            for (int i = 0; i < costlist.Count; i++)
            {
                cCostList.Add(new SelectListItem()
                {
                    Text = costlist[i].CCCDescription.Trim(),
                    Value = costlist[i].CostCenterCode.Trim(),
                });
            }
            ViewData["costlist"] = cCostList;


            //片語目錄 -該人員底下有的資料 
            DataTable dt = care_record_m.get_PhraseList(userinfo.EmployeesNo);
              List<SelectListItem> cPhraseList = new List<SelectListItem>();
             for (int i = 0; i < dt.Rows.Count; i++)
            {
                cPhraseList.Add(new SelectListItem()
                {
                    Text = dt.Rows[i]["NAME"].ToString().Trim(),
                    Value =  dt.Rows[i]["NODEID"].ToString().Trim(),
                });
            }
            ViewData["PhraseList"] = cPhraseList;
           
            return View();
        }

        [HttpPost]
        //片語_節點
        public ActionResult PhraseTreeNodeList(string type, string txtid, string srchval = "", string type2 = "", string PhraseType = "")
        {
            string typeSrch = type;
            ViewBag.type = type;  //先記錄下來
            if (type == "copy")
                typeSrch = type2;
            else if (type == "user")
            {
                switch (PhraseType)
                {
                    case "1":
                        typeSrch = "product";
                        break;
                    case "2":
                        typeSrch = "unit";
                        break;
                    case "3":
                        typeSrch = "self";
                        break;
                    default:
                        typeSrch = "self";
                        break;
                }
            }
            //根目錄
            DataTable dt = care_record_m.sel_phrase_node("", "0", "", "", "", "");
            dt.Columns.Add("LAST");
            //子目錄
            if (dt != null && dt.Rows.Count > 0)
            {
                sel_node(dt, dt.Rows[0]["NODEID"].ToString(), typeSrch, srchval);
            }
            ViewBag.dt_phrase_node = dt;
            ViewBag.txtid = (txtid == null) ? "" : txtid;

            return View();
        }

        //片語_內容
        public ActionResult Phrase_Data(string id, string type, string creano = "")
        {
            string typeSrch = type;
            if (creano != null && creano != "")
            {
                typeSrch = "self";
            }
            if (type == "user")
                typeSrch = "self";

            if (id != null || (typeSrch == "self" && id == null))
            {
                if (type == "self" && id == null)
                {
                    id = "0";
                    creano = userinfo.EmployeesNo;
                }
                else if (type == "self")
                    creano = userinfo.EmployeesNo;
                else if (typeSrch == "self" && id == null)
                    id = "0";
                DataTable dt = care_record_m.sel_phrase_data(id, "", typeSrch, creano);
                ViewBag.dt = dt;
                ViewBag.type = type;
            }

            return View();
        }

        //搜尋節點
        private void sel_node(DataTable dt, string id, string type, string srchval = "")
        {
            DataTable dt_temp = care_record_m.sel_phrase_node("", "", id, "", type, srchval);
            dt_temp.Columns.Add("LAST");
            if (dt_temp.Rows.Count > 0)
            {
                for (int i = 0; i < dt_temp.Rows.Count; i++)
                {
                    DataRow n_row = dt.NewRow();
                    for (int j = 0; j < dt_temp.Columns.Count; j++)
                        n_row[j] = dt_temp.Rows[i][j];
                    if (i + 1 == dt_temp.Rows.Count)
                        n_row["LAST"] = "1";
                    dt.Rows.Add(n_row);
                    sel_node(dt, dt_temp.Rows[i]["NODEID"].ToString(), type, srchval);
                }
            }
        }

        //新增節點
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Insert_Phrase_node(string type, FormCollection form)
        {
            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("NODEID", NextPhraseNodeIDValue(), DBItem.DBDataType.Number));
            insertDataList.Add(new DBItem("CREANO", userinfo.EmployeesNo, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("NAME", form["node_name"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("DEPTH", form["depth"], DBItem.DBDataType.Number));
            insertDataList.Add(new DBItem("PARENT_NODE", form["parent_node"], DBItem.DBDataType.Number));
            insertDataList.Add(new DBItem("EXPLANATION", form["node_explain"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("PHRASE_TYPE", type, DBItem.DBDataType.String));
            if (type == "unit")
                insertDataList.Add(new DBItem("COST_UNIT", form["HidCostUnitInsert"], DBItem.DBDataType.String));

            int erow = care_record_m.DBExecInsert("PHRASE_NODE", insertDataList);
            if (erow > 0)
                Response.Write("<script>alert('新增成功');window.location.href='Phrase?type=" + type + "&srchval=" + form["HidCostUnitInsert"] + "';</script>");
            else
                Response.Write("<script>alert('新增失敗');window.location.href='Phrase?type=" + type + "&srchval=" + form["HidCostUnitInsert"] + "';</script>");

            return new EmptyResult();
        }

        //更新節點
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Edit_Phrase_node(string type, FormCollection form)
        {
            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("NAME", form["edit_node_name"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("EXPLANATION", form["edit_node_explain"], DBItem.DBDataType.String));
            string where = " NODEID = " + form["nodeid"];

            int erow = care_record_m.DBExecUpdate("PHRASE_NODE", insertDataList, where);
            if (erow > 0)
                Response.Write("<script>alert('更新成功');window.location.href='Phrase?type=" + type + "&srchval=" + form["HidCostUnitEdit"] + "';</script>");
            else
                Response.Write("<script>alert('更新失敗');window.location.href='Phrase?type=" + type + "&srchval=" + form["HidCostUnitEdit"] + "';</script>");

            return new EmptyResult();
        }

        //刪除節點
        public ActionResult Del_Phrase_node(string nodeid, string type, string srchval = "")
        {
            int erow = 0;
            string where = " NODEID IN (" + nodeid + ") ";
            erow = care_record_m.DBExecDelete("PHRASE_NODE", where);
            if (erow > 0)
            {
                where = " NODE_ID IN (" + nodeid + ") ";
                erow = care_record_m.DBExecDelete("PHRASE_DATA", where);
                Response.Write("<script>alert('刪除成功');window.location.href='Phrase?type=" + type + "&srchval=" + srchval + "';</script>");
            }
            else
                Response.Write("<script>alert('刪除失敗');window.location.href='Phrase?type=" + type + "&srchval=" + srchval + "';</script>");
            return new EmptyResult();
        }

        //新增片語 + 複製單筆片語
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Insert_Phrase_data(FormCollection form, string type = "")
        {
            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("DATA_ID", base.creatid("PHRASE_DATA", userinfo.EmployeesNo, "", "0"), DBItem.DBDataType.String));
            if (type == "copy")
                insertDataList.Add(new DBItem("NODE_ID", "0", DBItem.DBDataType.Number));  //複製功能暫時決議放在根目錄，用CREATNO分辨，如果之後有其它類型可以複製片語，這個邏輯基本上就已不適用，要重新架構  wawa
            else
                insertDataList.Add(new DBItem("NODE_ID", form["ck_nodeid"], DBItem.DBDataType.Number));
            insertDataList.Add(new DBItem("NAME", form["title"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("C", form["record_com"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("S", form["record_s"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("O", form["record_o"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("I", form["record_i"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("E", form["record_e"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREANO", userinfo.EmployeesNo, DBItem.DBDataType.String));

            int erow = care_record_m.DBExecInsert("PHRASE_DATA", insertDataList);
            if (erow > 0)
                Response.Write("1");
            else
                Response.Write("0");

            return new EmptyResult();
        }

        //更新片語
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Update_Phrase_data(FormCollection form)
        {
            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("NAME", form["title"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("C", form["record_com"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("S", form["record_s"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("O", form["record_o"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("I", form["record_i"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("E", form["record_e"], DBItem.DBDataType.String));
            string where = " DATA_ID = '" + form["ck_data"] + "' AND NODE_ID = " + form["ck_nodeid"];

            int erow = care_record_m.DBExecUpdate("PHRASE_DATA", insertDataList, where);
            if (erow > 0)
                Response.Write("1");
            else
                Response.Write("0");

            return new EmptyResult();
        }

        //移動目錄節點
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Move_Phrase_node(string type, FormCollection form)
        {

            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("NODE_ID", form["DdlPhraseList"], DBItem.DBDataType.String));
            string where = " DATA_ID = '" + form["id_node"] + "'" ;

            int erow = care_record_m.DBExecUpdate("PHRASE_DATA", insertDataList, where);
         
            if (erow > 0)
                Response.Write("<script>alert('更新成功');window.location.href='Phrase?type=" + type + "&srchval=" + form["HidCostUnitEdit"] + "';</script>");
            else
                Response.Write("<script>alert('更新失敗');window.location.href='Phrase?type=" + type + "&srchval=" + form["HidCostUnitEdit"] + "';</script>");

            return new EmptyResult();
        }
        //刪除
        [HttpPost]
        public ActionResult Del_Phrase_data(string id)
        {
            string where = " DATA_ID = '" + id + "' ";
            int erow = care_record_m.DBExecDelete("PHRASE_DATA", where);
            if (erow > 0)
                Response.Write("1");
            else
                Response.Write("0");

            return new EmptyResult();
        }

        //複製片語 整個資料夾
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Copy_Phrase_data(FormCollection form, string type = "")
        {
            //抓取所選資料夾資訊
            DataTable dtNodeData = new DataTable();
            string sqlstr = "SELECT A.* FROM PHRASE_NODE A WHERE NODEID = " + form["ck_nodeid"];
            dtNodeData = care_record_m.DBExecSQL(sqlstr);
            string NodeIDNum = NextPhraseNodeIDValue();

            //新增資料夾
            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("NODEID", NodeIDNum, DBItem.DBDataType.Number));
            insertDataList.Add(new DBItem("CREANO", userinfo.EmployeesNo, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("NAME", dtNodeData.Rows[0]["NAME"].ToString(), DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("DEPTH", "1", DBItem.DBDataType.Number));
            insertDataList.Add(new DBItem("PARENT_NODE", "0", DBItem.DBDataType.Number));
            insertDataList.Add(new DBItem("EXPLANATION", dtNodeData.Rows[0]["EXPLANATION"].ToString(), DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("PHRASE_TYPE", "self", DBItem.DBDataType.String));
            int erow = care_record_m.DBExecInsert("PHRASE_NODE", insertDataList);

            //抓取所選資料夾內片語
            DataTable dtPhrase = new DataTable();
            sqlstr = "SELECT A.* FROM PHRASE_DATA A WHERE NODE_ID = " + form["ck_nodeid"];
            if (dtNodeData.Rows[0]["PHRASE_TYPE"].ToString() == "self")
                sqlstr += " AND CREANO = '" + dtNodeData.Rows[0]["CREANO"].ToString() + "'";
            dtPhrase = care_record_m.DBExecSQL(sqlstr);

            //新增片語
            for (int i = 0; i <= dtPhrase.Rows.Count - 1; i++)
            {
                insertDataList.Clear();
                insertDataList.Add(new DBItem("DATA_ID", base.creatid("PHRASE_DATA", userinfo.EmployeesNo, "", "0"), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("NODE_ID", NodeIDNum, DBItem.DBDataType.Number));
                insertDataList.Add(new DBItem("NAME", dtPhrase.Rows[i]["NAME"].ToString(), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("C", dtPhrase.Rows[i]["C"].ToString(), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("S", dtPhrase.Rows[i]["S"].ToString(), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("O", dtPhrase.Rows[i]["O"].ToString(), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("I", dtPhrase.Rows[i]["I"].ToString(), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("E", dtPhrase.Rows[i]["E"].ToString(), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREANO", userinfo.EmployeesNo, DBItem.DBDataType.String));
                erow += care_record_m.DBExecInsert("PHRASE_DATA", insertDataList);
            }
            if (erow > 0)  //if(erow == (dtPhrase.Rows.Count + 1)) 成功 else rollback
                Response.Write("1");
            else
                Response.Write("0");

            return new EmptyResult();
        }

        public string NextPhraseNodeIDValue()
        {
            //NodeID
            DataTable dt = new DataTable();
            string sqlstr = "SELECT NODEID FROM PHRASE_NODE A ORDER BY NODEID DESC ";
            dt = care_record_m.DBExecSQL(sqlstr);
            int CountNum = int.Parse(dt.Rows[0]["NODEID"].ToString()) + 1;

            return CountNum.ToString();
        }

        #endregion



    }
}
