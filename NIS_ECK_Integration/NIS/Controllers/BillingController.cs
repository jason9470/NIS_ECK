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
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace NIS.Controllers
{
    public class BillingController : BaseController
    {
        private DBConnector link;

        public BillingController()
        {
            this.link = new DBConnector();
        }

        //首頁畫面
        public ActionResult index(string set = "")
        {
            string costcodeno = "";
            string PTcostcodeno = "";
            //判斷有無session
            if (Session["UserInfo"] != null)
            {
                UserInfo ui = (UserInfo)Session["UserInfo"];
                costcodeno = ui.CostCenterCode;
            }
            if (Session["PatInfo"] != null)
            {
                if (ptinfo.CostCenterCode != null)
                {
                    PTcostcodeno = ptinfo.CostCenterCode.Trim();
                }
                ViewBag.RootDocument = GetSourceUrl();
                ViewBag.feeno = ptinfo.FeeNo;
            }
            else
            {
                Response.Write("<script>alert('請重新選擇病患');</script>");
                return new EmptyResult();
            }
            byte[] listByteCode = webService.GetCostCenterList();
            string listJsonArray = CompressTool.DecompressString(listByteCode);
            List<CostCenterList> costlist = JsonConvert.DeserializeObject<List<CostCenterList>>(listJsonArray);
            List<SelectListItem> cCostList = new List<SelectListItem>();
            List<SelectListItem> ptCostList = new List<SelectListItem>();

            cCostList.Add(new SelectListItem()
            {
                Text = "請選擇",
                Value = "",
                Selected = false
            });
            for (int i = 0; i <= costlist.Count - 1; i++)
            {

                if(costlist[i].CostCenterCode.Trim() == PTcostcodeno)
                {
                    cCostList.Add(new SelectListItem()
                    {
                        Text = costlist[i].CCCDescription.Trim(),
                        Value = costlist[i].CostCenterCode.Trim(),
                        Selected = true
                    });
                }
                else
                {
                    cCostList.Add(new SelectListItem()
                    {
                        Text = costlist[i].CCCDescription.Trim(),
                        Value = costlist[i].CostCenterCode.Trim(),
                        Selected = false
                    });
                }
            }
            for (int i = 0; i <= costlist.Count - 1; i++)
            {

                if (costlist[i].CostCenterCode.Trim() == PTcostcodeno)
                {
                    ptCostList.Add(new SelectListItem()
                    {
                        Text = costlist[i].CCCDescription.Trim(),
                        Value = costlist[i].CostCenterCode.Trim(),
                        Selected = true
                    });
                }
                else
                {
                    ptCostList.Add(new SelectListItem()
                    {
                        Text = costlist[i].CCCDescription.Trim(),
                        Value = costlist[i].CostCenterCode.Trim(),
                        Selected = false
                    });
                }
            }
            ViewData["costlist"] = cCostList;
            ViewData["PTcostlist"] = ptCostList;

            ViewData["set"] = set;
            return View();
        }

        //計價套餐 維護介面
        public ActionResult SetMaintain()
        {
            string costcodeno = "";

            //判斷有無session
            if (Session["UserInfo"] != null)
            {
                UserInfo ui = (UserInfo)Session["UserInfo"];
                costcodeno = ui.CostCenterCode;
            }
            byte[] listByteCode = webService.GetCostCenterList();
            string listJsonArray = CompressTool.DecompressString(listByteCode);
            List<CostCenterList> costlist = JsonConvert.DeserializeObject<List<CostCenterList>>(listJsonArray);
            List<SelectListItem> cCostList = new List<SelectListItem>();
            cCostList.Add(new SelectListItem()
            {
                Text = "請選擇",
                Value = "",
                Selected = false
            });
            for (int i = 0; i <= costlist.Count - 1; i++)
            {

                if (costlist[i].CostCenterCode.Trim() == costcodeno)
                {
                    cCostList.Add(new SelectListItem()
                    {
                        Text = costlist[i].CostCenterCode.Trim(),
                        Value = costlist[i].CostCenterCode.Trim(),
                        Selected = true
                    });
                }
                else
                {
                    cCostList.Add(new SelectListItem()
                    {
                        Text = costlist[i].CostCenterCode.Trim(),
                        Value = costlist[i].CostCenterCode.Trim(),
                        Selected = false
                    });
                }
            }
            ViewData["costlist"] = cCostList;
            return View();
        }


        //取得搜尋清單
        public ActionResult GetBasicData(string type, string search = "", string setType = "")
        {
            RESPONSE_MSG jsonResult = new RESPONSE_MSG();
            List<Basic_Data> datalist = new List<Basic_Data>();

            string sqlstr = "";
            if (type == "組套")
            {
                sqlstr = "SELECT * FROM SYS_BILL_SET_MASTER WHERE STATUS = 'Y' AND (UPPER(HO_ID) LIKE UPPER('%" + search + "%') OR UPPER(SET_NAME) LIKE UPPER('%" + search + "%'))";
                if(setType != "")
                {
                    sqlstr += " AND SET_TYPE = '" + setType + "'";
                }
                DataTable dt = this.link.DBExecSQL(sqlstr);


                foreach (DataRow dr in dt.Rows)
                {
                    Basic_Data data = new Basic_Data();
                    data.HO_ID = dr["HO_ID"].ToString();
                    data.ITEM_NAME = dr["SET_NAME"].ToString();
                    datalist.Add(data);
                }
            }
            else
            {
                List<string> costBillList = new List<string>();
                sqlstr = "SELECT * FROM SYS_BILL_PRICE";

                if (setType != "")
                {
                    string sql = "SELECT ITEMID FROM VW_ITEM_MIS_STUFF_NIS WHERE WAREHOUSEID = '" + setType.Trim() + "'";
                    DataTable dtCost = this.link.DBExecSQL(sql);
                    if(dtCost.Rows.Count > 0 )
                    {
                        foreach(DataRow cost in dtCost.Rows)
                        {
                            costBillList.Add("'" + cost["ITEMID"].ToString() + "'");
                        }
                    }
                }

                if (search != "")
                {
                    sqlstr += " where UPPER(CODE) LIKE UPPER('%" + search + "%') OR UPPER(CHINESE_NAME)  LIKE UPPER('%" + search + "%') OR UPPER(NH_CODE) LIKE UPPER('%" + search + "%') ";
                }

                if(costBillList.Count > 0)
                {
                    sqlstr += " where CODE IN (" + String.Join(",", costBillList) +")";

                }

                DataTable dt = this.link.DBExecSQL(sqlstr);

                foreach (DataRow dr in dt.Rows)
                {
                    Basic_Data data = new Basic_Data();
                    var nh_type = dr["NH_ORDER_TYPE"].ToString();
                    if (nh_type == "1")
                    {
                        nh_type = "藥物";
                    }
                    else if (nh_type == "2")
                    {
                        nh_type = "處置";
                    }
                    else if (nh_type == "3")
                    {
                        nh_type = "衛材";
                    }
                    data.HO_ID = dr["CODE"].ToString();
                    data.ITEM_NAME = dr["CHINESE_NAME"].ToString().Trim();
                    data.ITEM_TYPE = nh_type;
                    data.ITEM_PRICE = dr["SELF_PRICE"].ToString();
                    data.ITEM_PRICE_NH = dr["NH_PRICE"].ToString();
                    data.NH_CODE = dr["NH_CODE"].ToString();
                    data.SET = "";
                    datalist.Add(data);
                }
            }
           
            jsonResult.attachment = new
            {
                list = datalist,
            };

            return Content(JsonConvert.SerializeObject(jsonResult), "application/json");
        }


        //取得搜尋清單UDI
        public ActionResult GetBasicDataUDI(string type, string search = "")
        {
            RESPONSE_MSG jsonResult = new RESPONSE_MSG();

            if (search != "")
            {
                string sqlstr = "";
                if (type == "組套")
                {

                }
                else
                {
                    sqlstr = "SELECT * FROM SYS_BILL_PRICE";

                    sqlstr += " where CODE = '" + search + "'";

                }
                DataTable dt = this.link.DBExecSQL(sqlstr);
                List<Basic_Data> datalist = new List<Basic_Data>();
 
                foreach (DataRow dr in dt.Rows)
                {
                    Basic_Data data = new Basic_Data();
                    var nh_type = dr["NH_ORDER_TYPE"].ToString();
                    if (nh_type == "1")
                    {
                        nh_type = "藥物";
                    }
                    else if (nh_type == "2")
                    {
                        nh_type = "處置";
                    }
                    else if (nh_type == "3")
                    {
                        nh_type = "衛材";
                    }
                    data.HO_ID = dr["CODE"].ToString();
                    data.ITEM_NAME = dr["CHINESE_NAME"].ToString();
                    data.ITEM_TYPE = nh_type;
                    data.ITEM_PRICE = dr["SELF_PRICE"].ToString();
                    data.ITEM_PRICE_NH = dr["NH_PRICE"].ToString();
                    data.NH_CODE = dr["NH_CODE"].ToString();

                    datalist.Add(data);
                }

                jsonResult.attachment = new
                {
                    list = datalist,
                };
            }

            return Content(JsonConvert.SerializeObject(jsonResult), "application/json");
        }

        //取得搜尋清單UDI(條碼)
        public ActionResult GetBasicDataBarCode(string type, string search = "")
        {
            RESPONSE_MSG jsonResult = new RESPONSE_MSG();

            // 排除括號
            search = Regex.Replace(search, @"\D", "");

            string udi = search;
            Match match = Regex.Match(udi, @"01(\d{14})");

            if (match.Success)
            {
                string result = match.Groups[1].Value;
                udi = result;
            }
            else
            {
                Console.WriteLine("找不到符合的字串");
            }


            string udisql = "SELECT * FROM CS.V_WEB_UDI WHERE DI ='" + udi + "'";
            DataTable dtUDI = this.link.DBExecSQL(udisql);

            if(dtUDI.Rows.Count > 0)
            {
                search = dtUDI.Rows[0]["CODE"].ToString().Trim();
            }


            if (search != "")
            {
                string sqlstr = "";
                if (type == "組套")
                {

                }
                else
                {
                    sqlstr = "SELECT * FROM SYS_BILL_PRICE";

                    sqlstr += " where CODE = '" + search + "'";

                }
                DataTable dt = this.link.DBExecSQL(sqlstr);
                List<Basic_Data> datalist = new List<Basic_Data>();

                foreach (DataRow dr in dt.Rows)
                {
                    Basic_Data data = new Basic_Data();
                    var nh_type = dr["NH_ORDER_TYPE"].ToString();
                    if (nh_type == "1")
                    {
                        nh_type = "藥物";
                    }
                    else if (nh_type == "2")
                    {
                        nh_type = "處置";
                    }
                    else if (nh_type == "3")
                    {
                        nh_type = "衛材";
                    }
                    data.HO_ID = dr["CODE"].ToString();
                    data.ITEM_NAME = dr["CHINESE_NAME"].ToString();
                    data.ITEM_TYPE = nh_type;
                    data.ITEM_PRICE = dr["SELF_PRICE"].ToString();
                    data.ITEM_PRICE_NH = dr["NH_PRICE"].ToString();
                    data.NH_CODE = dr["NH_CODE"].ToString();

                    datalist.Add(data);
                }

                jsonResult.attachment = new
                {
                    list = datalist,
                };
            }

            return Content(JsonConvert.SerializeObject(jsonResult), "application/json");
        }


        //取得搜尋清單(組套)
        public ActionResult GetBasicDataSet(string id)
        {
            RESPONSE_MSG jsonResult = new RESPONSE_MSG();

            if (id != "")
            {
                string sqlstr = "";
                sqlstr = "SELECT SD.HO_ID , SM.SET_NAME, SD.QUANTITY, SM.SET_TYPE,SD.IDENTITY FROM SYS_BILL_SET_MASTER SM LEFT JOIN SYS_BILL_SET_DETAIL SD ON (SM.SERIAL_M = SD.SERIAL_M )";
                sqlstr += " where SM.HO_ID = '" + id + "' AND SD.STATUS = 'Y' AND SM.STATUS = 'Y'";

                DataTable dt = this.link.DBExecSQL(sqlstr);
                 List<Basic_Data> datalist = new List<Basic_Data>();

                foreach (DataRow dr in dt.Rows)
                {
                    var CODE = dr["HO_ID"].ToString();
                    var NAME = dr["SET_NAME"].ToString();
                    string COUNT = dr["QUANTITY"].ToString();

                    sqlstr = "SELECT * FROM SYS_BILL_PRICE WHERE CODE = '"+ CODE + "'";
                    DataTable dtPrice = this.link.DBExecSQL(sqlstr);

                    if (dtPrice.Rows.Count > 0)
                    {
                        foreach (DataRow dp in dtPrice.Rows)
                        {

                            Basic_Data data = new Basic_Data();
                            var nh_type = dp["NH_ORDER_TYPE"].ToString();
                            if (nh_type == "1")
                            {
                                nh_type = "藥物";
                            }
                            else if (nh_type == "2")
                            {
                                nh_type = "處置";
                            }
                            else if (nh_type == "3")
                            {
                                nh_type = "衛材";
                            }
                            data.HO_ID = dp["CODE"].ToString();
                            data.ITEM_NAME = dp["CHINESE_NAME"].ToString();
                            data.ITEM_TYPE = nh_type;
                            data.ITEM_PRICE = dp["SELF_PRICE"].ToString();
                            data.ITEM_PRICE_NH = dp["NH_PRICE"].ToString();
                            data.COUNT = COUNT;
                            data.SET = NAME;
                            data.SET_TYPE = dr["SET_TYPE"].ToString();
                            data.ITEM_IDENTITY = dr["IDENTITY"].ToString();

                            datalist.Add(data);
                        }
                    }
                }
        
                jsonResult.attachment = new
                {
                    list = datalist,
                };
            }

            return Content(JsonConvert.SerializeObject(jsonResult), "application/json");
        }

        //儲存計價內容
        public ActionResult SaveBilling(List<Bill_Detal> data, string date, string CostCenterNo)
        {
            RESPONSE_MSG jsonResult = new RESPONSE_MSG();
            List<DBItem> insertDataList = new List<DBItem>();
            int erow = 0;
            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;
            string costcode = ptinfo.CostCenterNo;
            string now = DateTime.Now.ToString("yyyy/MM/dd/ HH:mm:ss");
            string serial = creatid("BILL_DATA", userno, feeno, "0");

            try
            {
                //Master
                insertDataList.Clear();
                insertDataList.Add(new DBItem("SERIAL_M", serial, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATE_DATE", now, DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("CREATE_ID", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("RECORD_DATE", date, DBItem.DBDataType.DataTime));
                erow = this.link.DBExecInsert("DATA_BILLING_TEMP_MASTER", insertDataList);

                if(erow > 0 )
                {
                    //Deatil
                    for (int i = 0; i < data.Count(); i++)
                    {
                        string serialD = creatid("BILL_DATA_D", userno, feeno, "0");
                        string cover = "N";
                        DateTime record = DateTime.Parse(date);
                        DateTime nowTime = DateTime.Parse(DateTime.Now.ToString("yyyy/MM/dd 00:00:00"));

                        if (record < nowTime)
                        {
                            cover = "Y";
                        }

                        insertDataList.Clear();
                        insertDataList.Add(new DBItem("SERIAL_M", serial, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("SERIAL_D", serialD, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("HO_ID", data[i].HO_ID.Trim(), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("ITEM_NAME", data[i].ITEM_NAME.Trim(), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("ITEM_TYPE", data[i].ITEM_TYPE, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("ITEM_IDENTITY", data[i].ITEM_IDENTITY, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("ITEM_PRICE", data[i].ITEM_PRICE, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("COUNT", data[i].COUNT, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("COSTCODE", data[i].COSTCODE, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CREATE_ID", userno, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("RECORD_DATE", date, DBItem.DBDataType.DataTime));
                        insertDataList.Add(new DBItem("SELF_PRICE", data[i].SELF_PRICE, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("NH_PRICE", data[i].NH_PRICE, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("COVER", cover, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("SET_NAME", data[i].SET, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("CHARTNO", ptinfo.ChartNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("BEDNO", ptinfo.BedNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("PT_COSTCODE", CostCenterNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("DOCTOR", ptinfo.DocNo, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("NH_CODE", data[i].NH_CODE, DBItem.DBDataType.String));

                        erow = this.link.DBExecInsert("DATA_BILLING_TEMP_DETAIL", insertDataList);
                    }
                }         
            }
            catch(Exception ex )
            {
                jsonResult.status = RESPONSE_STATUS.ERROR;
                jsonResult.message = ex.ToString();

            }
            if (erow > 0)
            {
                jsonResult.status = RESPONSE_STATUS.SUCCESS;
            }
            else
            {
                jsonResult.status = RESPONSE_STATUS.ERROR;
            }

            return Content(JsonConvert.SerializeObject(jsonResult), "application/json");
        }

        //儲存組套設定
        public ActionResult SaveSet(List<Bill_Detal> data, string setName , string setCode, string setType)
        {
            RESPONSE_MSG jsonResult = new RESPONSE_MSG();
            List<DBItem> insertDataList = new List<DBItem>();
            int erow = 0;
            string userno = userinfo.EmployeesNo;
            string now = DateTime.Now.ToString("yyyy/MM/dd/ HH:mm:ss");
            string serial = creatid("BILL_SET_M", userno, "", "0");

            if(setCode == "")
            {
                setCode = "SET"+ DateTime.Now.ToString("MMddHHmmss");
            }

            try
            {
                insertDataList.Clear();
                insertDataList.Add(new DBItem("STATUS", "D", DBItem.DBDataType.String));
                erow = this.link.DBExecUpdate("SYS_BILL_SET_MASTER", insertDataList,"HO_ID = '"+ setCode + "'");

                //Master
                insertDataList.Clear();
                insertDataList.Add(new DBItem("SERIAL_M", serial, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("SET_NAME", setName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATE_ID", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("HO_ID", setCode, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("SET_TYPE", setType, DBItem.DBDataType.String));


                erow = this.link.DBExecInsert("SYS_BILL_SET_MASTER", insertDataList);

                if (erow > 0)
                {
                    //Deatil
                    for (int i = 0; i < data.Count(); i++)
                    {
                        string serialD = creatid("BILL_SET_D", userno, "", "0");
                        insertDataList.Clear();
                        insertDataList.Add(new DBItem("SERIAL_M", serial, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("SERIAL_D", serialD, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("HO_ID", data[i].HO_ID.Trim(), DBItem.DBDataType.String));               
                        insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("QUANTITY", data[i].COUNT.Trim(), DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("IDENTITY", data[i].ITEM_IDENTITY.Trim(), DBItem.DBDataType.String));

                        erow = this.link.DBExecInsert("SYS_BILL_SET_DETAIL", insertDataList);
                    }
                }
            }
            catch (Exception ex)
            {
                jsonResult.status = RESPONSE_STATUS.ERROR;
                jsonResult.message = ex.ToString();

            }
            if (erow > 0)
            {
                jsonResult.status = RESPONSE_STATUS.SUCCESS;
            }
            else
            {
                jsonResult.status = RESPONSE_STATUS.ERROR;
            }

            return Content(JsonConvert.SerializeObject(jsonResult), "application/json");
        }
    }
}
