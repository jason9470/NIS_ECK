using Com.Mayaminer;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Spreadsheet;
using iTextSharp.text.pdf.qrcode;
using Newtonsoft.Json;
using NIS.Data;
using NIS.Models;
using NIS.UtilTool;
using NIS.WebService;
using Org.BouncyCastle.Utilities.Encoders;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.UI.WebControls;


namespace NIS.Controllers
{
    public class BillingSummaryController : BaseController
    {
        LogTool log = new LogTool();
        private DBConnector link;

        public BillingSummaryController()
        {
            this.link = new DBConnector();
        }

        //計價摘要 - 待送出計價摘要查詢 上半部主畫面
        #region --查詢列表--
        public ActionResult index(string start = "", string end = "")
        {
            ViewBag.RootDocument = GetSourceUrl();
            byte[] listByteCode = webService.GetCostCenterList();
            string listJsonArray = CompressTool.DecompressString(listByteCode);
            List<CostCenterList> costlist = JsonConvert.DeserializeObject<List<CostCenterList>>(listJsonArray);
            List<SelectListItem> cCostListName = new List<SelectListItem>();

            cCostListName.Add(new SelectListItem()
            {
                Text = "請選擇",
                Value = "",
                Selected = false
            });
            for (int i = 0; i <= costlist.Count - 1; i++)
            {
                cCostListName.Add(new SelectListItem()
                {
                    Text = costlist[i].CCCDescription.Trim(),
                    Value = costlist[i].CostCenterCode.Trim(),
                    Selected = false
                });
            }
            ViewData["costlistName"] = cCostListName;

            return View();
        }


        //計價摘要 - 已送出計價摘要查詢 上半部主畫面
        public ActionResult summary()
        {
            ViewBag.RootDocument = GetSourceUrl();

            byte[] listByteCode = webService.GetCostCenterList();
            string listJsonArray = CompressTool.DecompressString(listByteCode);
            List<CostCenterList> costlist = JsonConvert.DeserializeObject<List<CostCenterList>>(listJsonArray);
            List<SelectListItem> cCostList = new List<SelectListItem>();
            List<SelectListItem> cCostListName = new List<SelectListItem>();

            //cCostList.Add
            cCostList.Add(new SelectListItem()
            {
                Text = "請選擇",
                Value = "",
                Selected = false
            });
            for (int i = 0; i <= costlist.Count - 1; i++)
            {
                cCostList.Add(new SelectListItem()
                {
                    Text = costlist[i].CostCenterCode.Trim(),
                    Value = costlist[i].CostCenterCode.Trim(),
                    Selected = false
                });
            }
            cCostListName.Add(new SelectListItem()
            {
                Text = "請選擇",
                Value = "",
                Selected = false
            });
            for (int i = 0; i <= costlist.Count - 1; i++)
            {
                cCostListName.Add(new SelectListItem()
                {
                    Text = costlist[i].CCCDescription.Trim(),
                    Value = costlist[i].CostCenterCode.Trim(),
                    Selected = false
                });
            }
            ViewData["costlist"] = cCostList;
            ViewData["costlistName"] = cCostListName;

            return View();
        }

        //計價摘要 帳務員 上半部主畫面
        public ActionResult summaryAccounts()
        {
            ViewBag.RootDocument = GetSourceUrl();

            byte[] listByteCode = webService.GetCostCenterList();
            string listJsonArray = CompressTool.DecompressString(listByteCode);
            List<CostCenterList> costlist = JsonConvert.DeserializeObject<List<CostCenterList>>(listJsonArray);
            List<SelectListItem> cCostList = new List<SelectListItem>();
            List<SelectListItem> cCostListName = new List<SelectListItem>();

            //cCostList.Add
            cCostList.Add(new SelectListItem()
            {
                Text = "請選擇",
                Value = "",
                Selected = false
            });
            for (int i = 0; i <= costlist.Count - 1; i++)
            {
                cCostList.Add(new SelectListItem()
                {
                    Text = costlist[i].CostCenterCode.Trim(),
                    Value = costlist[i].CostCenterCode.Trim(),
                    Selected = false
                });
            }
            cCostListName.Add(new SelectListItem()
            {
                Text = "請選擇",
                Value = "",
                Selected = false
            });
            for (int i = 0; i <= costlist.Count - 1; i++)
            {
                cCostListName.Add(new SelectListItem()
                {
                    Text = costlist[i].CCCDescription.Trim(),
                    Value = costlist[i].CostCenterCode.Trim(),
                    Selected = false
                });
            }
            ViewData["costlist"] = cCostList;
            ViewData["costlistName"] = cCostListName;

            return View();
        }

        //帳務員(日結) 上半部主畫面
        public ActionResult summaryAccountsDaily()
        {
            ViewBag.RootDocument = GetSourceUrl();

            byte[] listByteCode = webService.GetCostCenterList();
            string listJsonArray = CompressTool.DecompressString(listByteCode);
            List<CostCenterList> costlist = JsonConvert.DeserializeObject<List<CostCenterList>>(listJsonArray);
            List<SelectListItem> cCostList = new List<SelectListItem>();
            List<SelectListItem> cCostListName = new List<SelectListItem>();

            //cCostList.Add
            cCostList.Add(new SelectListItem()
            {
                Text = "請選擇",
                Value = "",
                Selected = false
            });
            for (int i = 0; i <= costlist.Count - 1; i++)
            {
                cCostList.Add(new SelectListItem()
                {
                    Text = costlist[i].CostCenterCode.Trim(),
                    Value = costlist[i].CostCenterCode.Trim(),
                    Selected = false
                });
            }
            cCostListName.Add(new SelectListItem()
            {
                Text = "請選擇",
                Value = "",
                Selected = false
            });
            for (int i = 0; i <= costlist.Count - 1; i++)
            {
                cCostListName.Add(new SelectListItem()
                {
                    Text = costlist[i].CCCDescription.Trim(),
                    Value = costlist[i].CostCenterCode.Trim(),
                    Selected = false
                });
            }
            ViewData["costlist"] = cCostList;
            ViewData["costlistName"] = cCostListName;

            return View();
        }

        #endregion

        //待送出計價摘要查詢 資料清單
        public ActionResult TempListData(string Starttime, string endtime, string chartNO = "", string bedNO = "", string costCode = "")
        {
            string start = DateTime.Now.ToString("yyyy/MM/dd 00:00:00");
            string end = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

            if (Starttime != null && endtime != null)
            {
                start = DateTime.Parse(Starttime).ToString("yyyy/MM/dd 00:00:00");

                DateTime endTemp = DateTime.Parse(endtime);
                DateTime nowTemp = DateTime.Now;
                start = DateTime.Parse(Starttime).ToString("yyyy/MM/dd 00:00:00");

                if ((endTemp - nowTemp).TotalDays > -1)
                {
                    end = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

                }
                else
                {
                    end = DateTime.Parse(endtime).ToString("yyyy/MM/dd 23:59:59");

                }
            }
            string userID = userinfo.EmployeesNo;
            string str = "";
            DataTable dt = new DataTable();
            List<Bill_SELECT_LIST> selectList = new List<Bill_SELECT_LIST>();


            //我的清單
            if (chartNO == "" && bedNO == "" && costCode == "")
            {
                //抓取派班清單
                str = "SELECT distinct(BED_NO),COST_CODE  FROM DATA_DISPATCHING WHERE RESPONSIBLE_USER = '" + userID + "' AND SHIFT_DATE BETWEEN TO_DATE('" + start + "','yyyy-MM-dd HH24:mi:ss') and TO_DATE('" + end + "','yyyy-MM-dd HH24:mi:ss')";
                dt = this.link.DBExecSQL(str);
                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        Bill_SELECT_LIST data = new Bill_SELECT_LIST();

                        data.BEDNO = dt.Rows[i]["BED_NO"].ToString();
                        data.COSTCODE = dt.Rows[i]["COST_CODE"].ToString();
                        selectList.Add(data);
                    }
                }
            }
            //病歷號查詢
            else if (chartNO != "")
            {
                Bill_SELECT_LIST data = new Bill_SELECT_LIST();
                string feeno = "";
                byte[] doByteCode = webService.GetInHistory(chartNO);
                if (doByteCode != null)
                {
                    string doJsonArr = CompressTool.DecompressString(doByteCode);
                    List<InHistory> IpdList = JsonConvert.DeserializeObject<List<InHistory>>(doJsonArr);
                    if (IpdList.Count > 0)
                    {
                        feeno = IpdList[0].FeeNo;
                        //用批價號病人資訊
                        doByteCode = webService.GetPatientInfo(feeno);
                        if (doByteCode != null)
                        {
                            string ptinfoJosnArr = CompressTool.DecompressString(doByteCode);
                            PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                            data.BEDNO = pi.BedNo;
                            data.COSTCODE = pi.CostCenterNo;
                            selectList.Add(data);
                        }
                    }
                }
            }
            //床號
            else if (bedNO != "")
            {
                Bill_SELECT_LIST data = new Bill_SELECT_LIST();
                data.BEDNO = bedNO;
                // 為測試站台可查詢排班資料先減7天[TODO]
                string strBill = "SELECT * FROM DATA_DISPATCHING WHERE BED_NO = '" + bedNO + "' AND SHIFT_DATE BETWEEN TO_DATE('" + start + "','yyyy/MM/dd HH24:mi:ss') -7 and TO_DATE('" + end + "','yyyy/MM/dd HH24:mi:ss') ORDER BY SHIFT_DATE DESC";
                DataTable dtBill = this.link.DBExecSQL(strBill);
                if (dtBill.Rows.Count > 0)
                {
                    data.COSTCODE = dtBill.Rows[0]["COST_CODE"].ToString();
                    selectList.Add(data);
                }
            }
            //CostCode
            else if (costCode != "")
            {
                List<PatientList> patList = new List<PatientList>();

                byte[] ptByteCode = null;
                ptByteCode = webService.GetPatientList(Request["costcode"].ToString());
                if (ptByteCode != null)
                {
                    string ptJsonArr = CompressTool.DecompressString(ptByteCode);
                    patList = JsonConvert.DeserializeObject<List<PatientList>>(ptJsonArr);
                }
                if (patList.Count > 0)
                {
                    patList.Sort((x, y) => { return -x.BedNo.CompareTo(y.BedNo); });
                    patList.Reverse();

                    for (int i = 0; i < patList.Count; i++)
                    {
                        Bill_SELECT_LIST data = new Bill_SELECT_LIST();
                        data.BEDNO = patList[i].BedNo;
                        data.COSTCODE = costCode;
                        selectList.Add(data);
                    }
                }
            }
            List<Bill_Summary_Master> Summarydata = new List<Bill_Summary_Master>();

            if (selectList.Count > 0)
            {
                for (int i = 0; i < selectList.Count; i++)
                {
                    string bedNo = selectList[i].BEDNO.ToString();
                    string costcode = selectList[i].COSTCODE.ToString().Trim();

                    byte[] ptinfobyte = webService.BedNoTransformFeeNoWithCostCode(bedNo, costcode);

                    if (ptinfobyte != null)
                    {
                        string ptinfojarr = CompressTool.DecompressString(ptinfobyte);
                        PatientInfo[] patinfo = JsonConvert.DeserializeObject<PatientInfo[]>(ptinfojarr);
                        string feeno = patinfo[0].FeeNo.ToString().Trim();
                        string chartno = patinfo[0].ChartNo.ToString().Trim(); if (chartNO != "")
                        {
                            if (chartno != chartNO)
                            {
                                continue;
                            }
                        }
                        string name = patinfo[0].PatientName.ToString().Trim();
                        string sex = patinfo[0].PatientGender.ToString().Trim();

                        string strBill = "SELECT * FROM DATA_BILLING_TEMP_DETAIL WHERE FEENO = '" + feeno + "' AND CREATE_ID ='" + userID + "' AND STATUS = 'Y' AND RECORD_DATE BETWEEN TO_DATE('" + start + "','yyyy/MM/dd HH24:mi:ss') and TO_DATE('" + end + "','yyyy/MM/dd HH24:mi:ss') ORDER BY RECORD_DATE DESC";
                        DataTable dtBill = this.link.DBExecSQL(strBill);
                        Bill_Summary_Master SummarydataPT = new Bill_Summary_Master();
                        List<Bill_Summary_Detail> datalist = new List<Bill_Summary_Detail>();

                        if (dtBill.Rows.Count > 0)
                        {
                            foreach (DataRow dr in dtBill.Rows)
                            {
                                Bill_Summary_Detail data = new Bill_Summary_Detail();

                                data.HO_ID = dr["HO_ID"].ToString();
                                data.ITEM_NAME = dr["ITEM_NAME"].ToString();
                                data.ITEM_TYPE = dr["ITEM_TYPE"].ToString();
                                data.ITEM_PRICE = dr["ITEM_PRICE"].ToString();
                                data.COUNT = dr["COUNT"].ToString();

                                // 扣庫單位預設為記錄者單位
                                #if DEBUG
                                    data.COSTCODE = dr["COSTCODE"].ToString();
                                #else
                                    string strSql = $"select* from MAST.PASSWD1 WHERE CLERK_ID = '{dr["CREATE_ID"].ToString().Trim()}' ";
                                    DataTable employee = this.link.DBExecSQL(strSql);
                                    if (employee.Rows.Count > 0)
                                    {
                                        data.COSTCODE = employee.Rows[0]["DEPT_NO"].ToString().Trim();
                                    }
                                #endif

                                data.ITEM_IDENTITY = dr["ITEM_IDENTITY"].ToString();
                                data.RECORD_DATE = dr["RECORD_DATE"].ToString();
                                data.CREATE_ID = dr["CREATE_ID"].ToString();
                                data.COVER = dr["COVER"].ToString();
                                data.NH_PRICE = dr["NH_PRICE"].ToString();
                                data.SELF_PRICE = dr["SELF_PRICE"].ToString();
                                data.SERIAL = dr["SERIAL_D"].ToString();
                                data.SET = dr["SET_NAME"].ToString();
                                data.DOCTOR = dr["DOCTOR"].ToString();
                                data.NH_CODE = dr["NH_CODE"].ToString();
                                data.PT_COSTCODE = dr["PT_COSTCODE"].ToString();

                                datalist.Add(data);
                            }
                            SummarydataPT.FEENO = feeno;
                            SummarydataPT.CHARTNO = chartno;
                            SummarydataPT.BEDNO = bedNo;
                            SummarydataPT.NAME = name;
                            SummarydataPT.SEX = sex;
                            SummarydataPT.Bill_D = datalist;
                            Summarydata.Add(SummarydataPT);
                        }
                    }

                    // 依照床號排序
                    Summarydata = Summarydata.OrderBy(item => item.BEDNO).ToList();

                    ViewBag.Summary = Summarydata;

                }
            }

            byte[] listByteCode = webService.GetCostCenterList();
            string listJsonArray = CompressTool.DecompressString(listByteCode);
            List<CostCenterList> costlist = JsonConvert.DeserializeObject<List<CostCenterList>>(listJsonArray);
            List<SelectListItem> cCostList = new List<SelectListItem>();
            List<SelectListItem> cCostListName = new List<SelectListItem>();
            List<SelectListItem> typeList = new List<SelectListItem>();
            typeList.Add(new SelectListItem()
            {
                Text = "健保",
                Value = "健保",
                Selected = false
            });
            typeList.Add(new SelectListItem()
            {
                Text = "自費",
                Value = "自費",
                Selected = false
            });
            typeList.Add(new SelectListItem()
            {
                Text = "吸收",
                Value = "吸收",
                Selected = false
            });

            //cCostList.Add
            cCostList.Add(new SelectListItem()
            {
                Text = "請選擇",
                Value = "",
                Selected = false
            });
            for (int i = 0; i <= costlist.Count - 1; i++)
            {
                cCostList.Add(new SelectListItem()
                {
                    Text = costlist[i].CCCDescription.Trim(),
                    Value = costlist[i].CostCenterCode.Trim(),
                    Selected = false
                });
            }
            cCostListName.Add(new SelectListItem()
            {
                Text = "請選擇",
                Value = "",
                Selected = false
            });
            for (int i = 0; i <= costlist.Count - 1; i++)
            {
                cCostListName.Add(new SelectListItem()
                {
                    Text = costlist[i].CCCDescription.Trim(),
                    Value = costlist[i].CostCenterCode.Trim(),
                    Selected = false
                });
            }
            ViewData["costlist"] = cCostList;
            ViewData["costlistName"] = cCostListName;
            ViewData["typeList"] = typeList;

            return View();
        }


        //已送出計價摘要查詢 資料清單
        public ActionResult ListData(string Starttime, string endtime, string chartNO = "", string bedNO = "", string costCode = "", string userNo = "")
        {
            string start = DateTime.Now.ToString("yyyy/MM/dd 00:00:00");
            string end = DateTime.Now.ToString("yyyy/MM/dd 23:59:59");

            if (Starttime != "" && endtime != "")
            {
                start = DateTime.Parse(Starttime).ToString("yyyy/MM/dd 00:00:00");
                end = DateTime.Parse(endtime).ToString("yyyy/MM/dd 23:59:59");
            }
            string userID = "";
            //抓取派班清單
            if (userNo == "")
            {
                userID = userinfo.EmployeesNo;
            }
            else
            {
                userID = userNo;
            }
            DataTable dt = new DataTable();
            List<Bill_SELECT_LIST> selectList = new List<Bill_SELECT_LIST>();
            //我的清單
            if (chartNO == "" && bedNO == "" && costCode == "")
            {
                string str = "SELECT distinct(BED_NO),COST_CODE  FROM DATA_DISPATCHING WHERE RESPONSIBLE_USER = '" + userID + "' AND SHIFT_DATE BETWEEN TO_DATE('" + start + "','yyyy-MM-dd HH24:mi:ss') and TO_DATE('" + end + "','yyyy-MM-dd HH24:mi:ss')";
                dt = this.link.DBExecSQL(str);
                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        Bill_SELECT_LIST data = new Bill_SELECT_LIST();

                        data.BEDNO = dt.Rows[i]["BED_NO"].ToString();
                        data.COSTCODE = dt.Rows[i]["COST_CODE"].ToString();
                        selectList.Add(data);
                    }
                }
            }

            //病歷號查詢
            else if (chartNO != "")
            {
                Bill_SELECT_LIST data = new Bill_SELECT_LIST();
                string feeno = "";
                byte[] doByteCode = webService.GetInHistory(chartNO);
                if (doByteCode != null)
                {
                    string doJsonArr = CompressTool.DecompressString(doByteCode);
                    List<InHistory> IpdList = JsonConvert.DeserializeObject<List<InHistory>>(doJsonArr);
                    if (IpdList.Count > 0)
                    {
                        feeno = IpdList[0].FeeNo;
                        //用批價號病人資訊
                        doByteCode = webService.GetPatientInfo(feeno);
                        if (doByteCode != null)
                        {
                            string ptinfoJosnArr = CompressTool.DecompressString(doByteCode);
                            PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                            data.BEDNO = pi.BedNo;
                            data.COSTCODE = pi.CostCenterNo;
                            selectList.Add(data);
                        }
                    }
                }
            }
            //床號
            else if (bedNO != "")
            {
                Bill_SELECT_LIST data = new Bill_SELECT_LIST();
                data.BEDNO = bedNO;
                // 為測試站台可查詢排班資料先減7天[TODO]
                string strBill = "SELECT * FROM DATA_DISPATCHING WHERE BED_NO = '" + bedNO + "' AND SHIFT_DATE BETWEEN TO_DATE('" + start + "','yyyy/MM/dd HH24:mi:ss') -7 and TO_DATE('" + end + "','yyyy/MM/dd HH24:mi:ss') ORDER BY SHIFT_DATE DESC";
                DataTable dtBill = this.link.DBExecSQL(strBill);
                if (dtBill.Rows.Count > 0)
                {
                    data.COSTCODE = dtBill.Rows[0]["COST_CODE"].ToString();
                    selectList.Add(data);
                }
            }
            //CostCode
            else if (costCode != "")
            {
                List<PatientList> patList = new List<PatientList>();

                byte[] ptByteCode = null;
                ptByteCode = webService.GetPatientList(Request["costcode"].ToString());
                if (ptByteCode != null)
                {
                    string ptJsonArr = CompressTool.DecompressString(ptByteCode);
                    patList = JsonConvert.DeserializeObject<List<PatientList>>(ptJsonArr);
                }
                if (patList.Count > 0)
                {
                    patList.Sort((x, y) => { return -x.BedNo.CompareTo(y.BedNo); });
                    patList.Reverse();

                    for (int i = 0; i < patList.Count; i++)
                    {
                        Bill_SELECT_LIST data = new Bill_SELECT_LIST();
                        data.BEDNO = patList[i].BedNo;
                        data.COSTCODE = costCode;


                        selectList.Add(data);
                    }

                }
            }
            List<Bill_Summary_Master> Summarydata = new List<Bill_Summary_Master>();

            for (int i = 0; i < selectList.Count; i++)
            {
                string bedNo = selectList[i].BEDNO.ToString();
                string costcode = selectList[i].COSTCODE.ToString();
                byte[] ptinfobyte = webService.BedNoTransformFeeNoWithCostCode(bedNo, costcode);

                if (ptinfobyte != null)
                {
                    string ptinfojarr = CompressTool.DecompressString(ptinfobyte);
                    PatientInfo[] patinfo = JsonConvert.DeserializeObject<PatientInfo[]>(ptinfojarr);
                    string feeno = patinfo[0].FeeNo.ToString().Trim();

                    string chartno = patinfo[0].ChartNo.ToString().Trim();
                    if (chartNO != "")
                    {
                        if (chartno != chartNO)
                        {
                            continue;
                        }
                    }
                    string name = patinfo[0].PatientName.ToString().Trim();
                    string sex = patinfo[0].PatientGender.ToString().Trim();

                    string strMaster = "SELECT * FROM DATA_BILLING_CONFIRM_MASTER WHERE FEENO ='" + feeno + "' AND STATUS = 'Y' AND RECORD_DATE BETWEEN TO_DATE('" + start + "','yyyy/MM/dd HH24:mi:ss') and TO_DATE('" + end + "','yyyy/MM/dd HH24:mi:ss') ORDER BY RECORD_DATE DESC ";
                    DataTable dtMaster = this.link.DBExecSQL(strMaster);

                    string serialM = "";

                    if (dtMaster.Rows.Count > 0)
                    {
                        byte[] listByteCode = webService.GetCostCenterList();
                        string listJsonArray = CompressTool.DecompressString(listByteCode);
                        List<CostCenterList> costlist = JsonConvert.DeserializeObject<List<CostCenterList>>(listJsonArray);

                        for (int j = 0; j < dtMaster.Rows.Count; j++)
                        {
                            serialM = dtMaster.Rows[j]["SERIAL_M"].ToString();
                            string strBill = "SELECT * FROM DATA_BILLING_CONFIRM_DETAIL WHERE FEENO = '" + feeno + "' AND STATUS = 'Y' AND SERIAL_M = '" + serialM + "' ORDER BY RECORD_DATE DESC";
                            DataTable dtBill = this.link.DBExecSQL(strBill);
                            if (dtBill.Rows.Count > 0)
                            {
                                Bill_Summary_Master SummarydataPT = new Bill_Summary_Master();

                                List<Bill_Summary_Detail> datalist = new List<Bill_Summary_Detail>();

                                foreach (DataRow dr in dtBill.Rows)
                                {
                                    Bill_Summary_Detail data = new Bill_Summary_Detail();

                                    data.HO_ID = dr["HO_ID"].ToString();
                                    data.ITEM_NAME = dr["ITEM_NAME"].ToString();
                                    data.ITEM_TYPE = dr["ITEM_TYPE"].ToString();
                                    data.ITEM_PRICE = dr["ITEM_PRICE"].ToString();
                                    data.COUNT = dr["COUNT"].ToString();

                                    // 轉換成單位名稱
                                    string COSTNAME = costlist.Where(item => item.CostCenterCode.Trim() == dr["COSTCODE"].ToString()).FirstOrDefault()?.CCCDescription;
                                    data.COSTCODE = COSTNAME ?? dr["COSTCODE"].ToString();

                                    data.ITEM_IDENTITY = dr["ITEM_IDENTITY"].ToString();
                                    data.RECORD_DATE = dr["RECORD_DATE"].ToString();
                                    data.CREATE_ID = dr["CREATE_ID"].ToString();
                                    data.COVER = dr["COVER"].ToString();
                                    data.NH_PRICE = dr["NH_PRICE"].ToString();
                                    data.SELF_PRICE = dr["SELF_PRICE"].ToString();
                                    data.SERIAL = dr["SERIAL_D"].ToString();
                                    data.SET = dr["SET_NAME"].ToString();
                                    data.DOCTOR = dr["DOCTOR"].ToString();
                                    data.BRING_STATUS = dr["BRING_STATUS"].ToString();
                                    data.NH_CODE = dr["NH_CODE"].ToString();

                                    datalist.Add(data);
                                }
                                SummarydataPT.FEENO = feeno;
                                SummarydataPT.CHARTNO = chartno;
                                SummarydataPT.BEDNO = bedNo;
                                SummarydataPT.NAME = name;
                                SummarydataPT.SEX = sex;
                                SummarydataPT.Bill_D = datalist;
                                SummarydataPT.RECORD_DATE = dtMaster.Rows[j]["RECORD_DATE"].ToString();

                                Summarydata.Add(SummarydataPT);
                            }
                        }
                    }
                }
            }

            // 依照床號排序
            Summarydata = Summarydata.OrderBy(item => item.BEDNO).ToList();

            ViewBag.Summary = Summarydata;

            return View();
        }

        //帳務員計價摘要 (FROM TEMP) 資料清單
        public ActionResult ListDataAccountsTemp(string Starttime, string endtime, string chartNO = "", string bedNO = "", string costCode = "", string userNo = "", string type = "")
        {

            CultureInfo culture = new CultureInfo("zh-TW");
            culture.DateTimeFormat.Calendar = new TaiwanCalendar();

            string startDate = DateTime.Now.AddDays(-3).ToString("yyyMMdd", culture);
            string startTime = DateTime.Now.ToString("0000");

            string endDate = DateTime.Now.AddDays(1).ToString("yyyMMdd", culture);
            string endTime = DateTime.Now.AddDays(1).ToString("2359");


            if (Starttime != "" && endtime != "")
            {
                startDate = DateTime.Parse(Starttime).ToString("yyyMMdd", culture);
                startTime = DateTime.Parse(Starttime).ToString("HHmm");
                endDate = DateTime.Now.AddDays(1).ToString("yyyMMdd", culture);
                endTime = DateTime.Now.AddDays(1).ToString("HHmm");
            }

            List<string> chartNolist = new List<string>();
            List<PatientList> patList = new List<PatientList>();


            //從HIS TEMP 抓取未取帳
            DataTable dt = new DataTable();
            string sql = "";
            if (costCode != "")
            {
                byte[] ptByteCode = null;
                ptByteCode = webService.GetPatientList(costCode);
                if (ptByteCode != null)
                {
                    string ptJsonArr = CompressTool.DecompressString(ptByteCode);
                    patList = JsonConvert.DeserializeObject<List<PatientList>>(ptJsonArr);
                }
                if (patList.Count > 0)
                {
                    patList.Sort((x, y) => { return -x.BedNo.CompareTo(y.BedNo); });
                    patList.Reverse();

                    for (int i = 0; i < patList.Count; i++)
                    {
                        chartNolist.Add("'" + patList[i].ChrNo + "'");
                    }

                }
                string chartNolistStr = string.Join(",", chartNolist);
                if (costCode == "A000")
                {
                    sql = "SELECT * FROM ANES_CHARGE_MAIN WHERE 1 = 1 ";
                }
                else
                {
                    sql = "SELECT * FROM ANES_CHARGE_MAIN WHERE CHART_NO IN (" + chartNolistStr + ") ";
                }
            }
            else if (chartNO != "")
            {
                sql = "SELECT * FROM ANES_CHARGE_MAIN WHERE  CHART_NO ='" + chartNO + "' ";

            }
            else if (bedNO != "")
            {
                byte[] ptinfobyte = webService.BedNoTransformFeeNo(bedNO.Trim());
                if (ptinfobyte != null)
                {
                    string ptinfojarr = CompressTool.DecompressString(ptinfobyte);
                    PatientInfo[] patinfo = JsonConvert.DeserializeObject<PatientInfo[]>(ptinfojarr);
                    string feeno = patinfo[0].FeeNo.ToString().Trim();
                    string chartno = patinfo[0].ChartNo.ToString().Trim();
                    if (chartno != "")
                    {
                        sql = "SELECT * FROM ANES_CHARGE_MAIN WHERE  CHART_NO ='" + chartno + "' ";
                    }
                }
            }
            if (type == "未取帳")
            {
                sql += " AND (DATA_STATUS_DRUG = 'N' AND DATA_STATUS_MATERIAL = 'N')";
            }
            else if (type == "已取帳")
            {
                sql += " AND ( DATA_STATUS_MATERIAL = 'Y')";
            }
            sql += " AND SYSTEM_TYPE = 'NIS' AND ANES_START_DATE > '" + startDate + "' AND ANES_START_DATE < '" + endDate + "'";
            sql += " ORDER BY ANES_START_DATE DESC";

            link.DBExecSQL(sql, ref dt);

            if (dt.Rows.Count > 0)
            {
                List<Bill_Summary_Master> masters = new List<Bill_Summary_Master>();

                foreach (DataRow dr in dt.Rows)
                {
                    Bill_Summary_Master masterTemp = new Bill_Summary_Master();
                    string chartno = dr["CHART_NO"].ToString();
                    string feeno = "";
                    string seq = dr["CHARGE_MAIN_SEQNO"].ToString();
                    string recordTemp = dr["ANES_START_DATE"].ToString();
                    string tempType = dr["DATA_STATUS_MATERIAL"].ToString();

                    if (recordTemp.Length == 7)
                    {
                        // 取出民國年、月、日
                        int rocYear = int.Parse(recordTemp.Substring(0, 3));
                        int month = int.Parse(recordTemp.Substring(3, 2));
                        int day = int.Parse(recordTemp.Substring(5, 2));

                        // 轉換成西元年
                        int year = rocYear + 1911;

                        DateTime date = new DateTime(year, month, day);

                        masterTemp.RECORD_DATE = date.ToString("yyyy/MM/dd");
                    }


                    masterTemp.CHARTNO = chartno;
                    byte[] doByteCode = webService.GetInHistory(chartno);
                    if (doByteCode != null)
                    {
                        string doJsonArr = CompressTool.DecompressString(doByteCode);
                        List<InHistory> IpdList = JsonConvert.DeserializeObject<List<InHistory>>(doJsonArr);
                        if (IpdList.Count > 0)
                        {
                            feeno = IpdList[0].FeeNo;
                            //用批價號病人資訊
                            doByteCode = webService.GetPatientInfo(feeno);
                            if (doByteCode != null)
                            {
                                string ptinfoJosnArr = CompressTool.DecompressString(doByteCode);
                                PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                                masterTemp.BEDNO = pi.BedNo;
                                masterTemp.NAME = pi.PatientName;
                                masterTemp.SEX = pi.PatientGender;
                            }
                        }
                    }

                    //Detail
                    string deatil = "SELECT * FROM anes_charge_detail AD LEFT JOIN SYS_BILL_PRICE B ON (AD.CHARGE_ITEM_NO = trim(B.CODE)) WHERE ad.charge_main_seq_no = '" + seq + "' ORDER BY CHARGE_ITEM_NO ASC";
                    DataTable detailDT = new DataTable();

                    link.DBExecSQL(deatil, ref detailDT);

                    if (detailDT.Rows.Count > 0)
                    {
                        List<Bill_Summary_Detail> details = new List<Bill_Summary_Detail>();

                        foreach (DataRow drD in detailDT.Rows)
                        {
                            Bill_Summary_Detail data = new Bill_Summary_Detail();

                            data.HO_ID = drD["CHARGE_ITEM_NO"].ToString();
                            data.ITEM_NAME = drD["CHINESE_NAME"].ToString();
                            var nh_type = drD["NH_ORDER_TYPE"].ToString();
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
                            data.ITEM_TYPE = nh_type;


                            //data.ITEM_PRICE = drD["ITEM_PRICE"].ToString();
                            data.COUNT = drD["CHARGE_ITEM_VALUE"].ToString();
                            data.COSTCODE = drD["MEDICAL_ORDER_LOCATION"].ToString();
                            data.ITEM_IDENTITY = drD["CHARGE_RULE"].ToString();
                            data.RECORD_DATE = drD["MEDICAL_ORDER_START_DATE"].ToString();
                            //data.CREATE_ID = drD["CHARGE_NO"].ToString();
                            data.BRING_STATUS = tempType;
                            details.Add(data);
                        }
                        masterTemp.Bill_D = details;
                    }
                    masters.Add(masterTemp);
                }
                ViewBag.Summary = masters;

            }
            return View();
        }

        //帳務員日結 資料清單
        public ActionResult ListDataAccounts(string Starttime, string endtime, string chartNO = "", string bedNO = "", string costCode = "", string userNo = "")
        {
            string start = DateTime.Now.ToString("yyyy/MM/dd 00:00:00");
            string end = DateTime.Now.ToString("yyyy/MM/dd 23:59:59");

            if (Starttime != "" && endtime != "")
            {
                start = DateTime.Parse(Starttime).ToString("yyyy/MM/dd 00:00:00");
                end = DateTime.Parse(endtime).ToString("yyyy/MM/dd 23:59:59");
            }
            string userID = "";
            //抓取派班清單
            if (userNo == "")
            {
                userID = userinfo.EmployeesNo;
            }
            else
            {
                userID = userNo;
            }
            DataTable dt = new DataTable();
            List<Bill_SELECT_LIST> selectList = new List<Bill_SELECT_LIST>();
            //我的清單
            if (chartNO == "" && bedNO == "" && costCode == "")
            {
                string str = "SELECT distinct(BED_NO),COST_CODE  FROM DATA_DISPATCHING WHERE RESPONSIBLE_USER = '" + userID + "' AND SHIFT_DATE BETWEEN TO_DATE('" + start + "','yyyy-MM-dd HH24:mi:ss') and TO_DATE('" + end + "','yyyy-MM-dd HH24:mi:ss')";
                dt = this.link.DBExecSQL(str);
                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        Bill_SELECT_LIST data = new Bill_SELECT_LIST();

                        data.BEDNO = dt.Rows[i]["BED_NO"].ToString();
                        data.COSTCODE = dt.Rows[i]["COST_CODE"].ToString();
                        selectList.Add(data);
                    }
                }
            }

            //病歷號查詢
            else if (chartNO != "")
            {
                Bill_SELECT_LIST data = new Bill_SELECT_LIST();
                string feeno = "";
                byte[] doByteCode = webService.GetInHistory(chartNO);
                if (doByteCode != null)
                {
                    string doJsonArr = CompressTool.DecompressString(doByteCode);
                    List<InHistory> IpdList = JsonConvert.DeserializeObject<List<InHistory>>(doJsonArr);
                    if (IpdList.Count > 0)
                    {
                        feeno = IpdList[0].FeeNo;
                        //用批價號病人資訊
                        doByteCode = webService.GetPatientInfo(feeno);
                        if (doByteCode != null)
                        {
                            string ptinfoJosnArr = CompressTool.DecompressString(doByteCode);
                            PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                            data.BEDNO = pi.BedNo;
                            data.COSTCODE = pi.CostCenterCode;
                            selectList.Add(data);
                        }
                    }
                }
            }
            //床號
            else if (bedNO != "")
            {
                Bill_SELECT_LIST data = new Bill_SELECT_LIST();
                data.BEDNO = bedNO;
                selectList.Add(data);
            }
            //CostCode
            else if (costCode != "")
            {
                List<PatientList> patList = new List<PatientList>();

                byte[] ptByteCode = null;
                ptByteCode = webService.GetPatientList(Request["costcode"].ToString());
                if (ptByteCode != null)
                {
                    string ptJsonArr = CompressTool.DecompressString(ptByteCode);
                    patList = JsonConvert.DeserializeObject<List<PatientList>>(ptJsonArr);
                }
                if (patList.Count > 0)
                {
                    patList.Sort((x, y) => { return -x.BedNo.CompareTo(y.BedNo); });
                    patList.Reverse();

                    for (int i = 0; i < patList.Count; i++)
                    {
                        Bill_SELECT_LIST data = new Bill_SELECT_LIST();
                        data.BEDNO = patList[i].BedNo;
                        data.COSTCODE = costCode;
                        selectList.Add(data);
                    }

                }
            }
            List<Bill_Summary_Master> Summarydata = new List<Bill_Summary_Master>();

            for (int i = 0; i < selectList.Count; i++)
            {

                string bedNo = selectList[i].BEDNO.ToString();
                string costcode = selectList[i].COSTCODE.ToString();
                byte[] ptinfobyte = webService.BedNoTransformFeeNoWithCostCode(bedNo, costcode);

                if (ptinfobyte != null)
                {
                    string ptinfojarr = CompressTool.DecompressString(ptinfobyte);
                    PatientInfo[] patinfo = JsonConvert.DeserializeObject<PatientInfo[]>(ptinfojarr);
                    string feeno = patinfo[0].FeeNo.ToString().Trim();
                    string chartno = patinfo[0].ChartNo.ToString().Trim();
                    if (chartNO != "")
                    {
                        if (chartno != chartNO)
                        {
                            continue;
                        }
                    }
                    string name = patinfo[0].PatientName.ToString().Trim();
                    string sex = patinfo[0].PatientGender.ToString().Trim();

                    string strMaster = "SELECT * FROM DATA_BILLING_CONFIRM_MASTER WHERE FEENO ='" + feeno + "' AND STATUS = 'Y' AND RECORD_DATE BETWEEN TO_DATE('" + start + "','yyyy/MM/dd HH24:mi:ss') and TO_DATE('" + end + "','yyyy/MM/dd HH24:mi:ss') ORDER BY RECORD_DATE DESC ";
                    DataTable dtMaster = this.link.DBExecSQL(strMaster);

                    string serialM = "";

                    if (dtMaster.Rows.Count > 0)
                    {
                        for (int j = 0; j < dtMaster.Rows.Count; j++)
                        {
                            serialM = dtMaster.Rows[j]["SERIAL_M"].ToString();
                            string strBill = "SELECT * FROM DATA_BILLING_CONFIRM_DETAIL WHERE FEENO = '" + feeno + "' AND STATUS = 'Y' AND SERIAL_M = '" + serialM + "' ORDER BY RECORD_DATE DESC";
                            DataTable dtBill = this.link.DBExecSQL(strBill);
                            if (dtBill.Rows.Count > 0)
                            {
                                Bill_Summary_Master SummarydataPT = new Bill_Summary_Master();

                                List<Bill_Summary_Detail> datalist = new List<Bill_Summary_Detail>();

                                foreach (DataRow dr in dtBill.Rows)
                                {
                                    Bill_Summary_Detail data = new Bill_Summary_Detail();

                                    data.HO_ID = dr["HO_ID"].ToString();
                                    data.ITEM_NAME = dr["ITEM_NAME"].ToString();
                                    data.ITEM_TYPE = dr["ITEM_TYPE"].ToString();
                                    data.ITEM_PRICE = dr["ITEM_PRICE"].ToString();
                                    data.COUNT = dr["COUNT"].ToString();
                                    data.COSTCODE = dr["COSTCODe"].ToString();
                                    data.ITEM_IDENTITY = dr["ITEM_IDENTITY"].ToString();
                                    data.RECORD_DATE = dr["RECORD_DATE"].ToString();
                                    data.CREATE_ID = dr["CREATE_ID"].ToString();
                                    data.COVER = dr["COVER"].ToString();
                                    data.NH_PRICE = dr["NH_PRICE"].ToString();
                                    data.SELF_PRICE = dr["SELF_PRICE"].ToString();
                                    data.SERIAL = dr["SERIAL_D"].ToString();
                                    data.SET = dr["SET_NAME"].ToString();
                                    data.DOCTOR = dr["DOCTOR"].ToString();
                                    data.BRING_STATUS = dr["BRING_STATUS"].ToString();
                                    data.NH_CODE = dr["NH_CODE"].ToString();

                                    datalist.Add(data);
                                }
                                SummarydataPT.FEENO = feeno;
                                SummarydataPT.CHARTNO = chartno;
                                SummarydataPT.BEDNO = bedNo;
                                SummarydataPT.NAME = name;
                                SummarydataPT.SEX = sex;
                                SummarydataPT.Bill_D = datalist;
                                Summarydata.Add(SummarydataPT);
                            }
                        }
                    }
                }
            }
            ViewBag.Summary = Summarydata;

            return View();
        }
        //計價摘要列印清單
        public ActionResult ListDataPrint(string Starttime, string endtime, string chartNO = "", string bedNO = "", string costCode = "", string userNo = "", string userName = "")
        {
            string start = DateTime.Now.ToString("yyyy/MM/dd 00:00:00");
            string end = DateTime.Now.ToString("yyyy/MM/dd 23:59:59");

            if (Starttime != "" && endtime != "")
            {
                start = DateTime.Parse(Starttime).ToString("yyyy/MM/dd 00:00:00");
                end = DateTime.Parse(endtime).ToString("yyyy/MM/dd 23:59:59");
            }
            string userID = "";
            //抓取派班清單
            if (userNo == "")
            {
                userID = userinfo.EmployeesNo;
            }
            else
            {
                userID = userNo;
            }
            DataTable dt = new DataTable();
            List<Bill_SELECT_LIST> selectList = new List<Bill_SELECT_LIST>();
            //我的清單
            if (chartNO == "" && bedNO == "" && costCode == "")
            {
                string str = "SELECT distinct(BED_NO),COST_CODE FROM DATA_DISPATCHING WHERE RESPONSIBLE_USER = '" + userID + "' AND SHIFT_DATE BETWEEN TO_DATE('" + start + "','yyyy-MM-dd HH24:mi:ss') and TO_DATE('" + end + "','yyyy-MM-dd HH24:mi:ss')";
                dt = this.link.DBExecSQL(str);
                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        Bill_SELECT_LIST data = new Bill_SELECT_LIST();

                        data.BEDNO = dt.Rows[i]["BED_NO"].ToString();
                        data.COSTCODE = dt.Rows[i]["COST_CODE"].ToString();
                        selectList.Add(data);
                    }
                }
            }

            //病歷號查詢
            else if (chartNO != "")
            {
                Bill_SELECT_LIST data = new Bill_SELECT_LIST();
                string feeno = "";
                byte[] doByteCode = webService.GetInHistory(chartNO);
                if (doByteCode != null)
                {
                    string doJsonArr = CompressTool.DecompressString(doByteCode);
                    List<InHistory> IpdList = JsonConvert.DeserializeObject<List<InHistory>>(doJsonArr);
                    if (IpdList.Count > 0)
                    {
                        feeno = IpdList[0].FeeNo;
                        //用批價號病人資訊
                        doByteCode = webService.GetPatientInfo(feeno);
                        if (doByteCode != null)
                        {
                            string ptinfoJosnArr = CompressTool.DecompressString(doByteCode);
                            PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                            data.BEDNO = pi.BedNo;
                            data.COSTCODE = pi.CostCenterCode;
                            selectList.Add(data);
                        }
                    }
                }
            }
            //床號
            else if (bedNO != "")
            {
                Bill_SELECT_LIST data = new Bill_SELECT_LIST();
                data.BEDNO = bedNO;
                selectList.Add(data);
            }
            //CostCode
            else if (costCode != "")
            {
                List<PatientList> patList = new List<PatientList>();

                byte[] ptByteCode = null;
                ptByteCode = webService.GetPatientList(Request["costcode"].ToString());
                if (ptByteCode != null)
                {
                    string ptJsonArr = CompressTool.DecompressString(ptByteCode);
                    patList = JsonConvert.DeserializeObject<List<PatientList>>(ptJsonArr);
                }
                if (patList.Count > 0)
                {
                    patList.Sort((x, y) => { return -x.BedNo.CompareTo(y.BedNo); });
                    patList.Reverse();

                    for (int i = 0; i < patList.Count; i++)
                    {
                        Bill_SELECT_LIST data = new Bill_SELECT_LIST();
                        data.BEDNO = patList[i].BedNo;
                        data.COSTCODE = costCode;
                        selectList.Add(data);
                    }

                }
            }
            List<Bill_Summary_Master> Summarydata = new List<Bill_Summary_Master>();

            // 成本中心
            byte[] listByteCode = webService.GetCostCenterList();
            string listJsonArray = CompressTool.DecompressString(listByteCode);
            List<CostCenterList> costlist = JsonConvert.DeserializeObject<List<CostCenterList>>(listJsonArray);

            for (int i = 0; i < selectList.Count; i++)
            {
                string bedNo = selectList[i].BEDNO.ToString();
                byte[] ptinfobyte = webService.BedNoTransformFeeNo(bedNo.Trim());

                if (ptinfobyte != null)
                {
                    string ptinfojarr = CompressTool.DecompressString(ptinfobyte);
                    PatientInfo[] patinfo = JsonConvert.DeserializeObject<PatientInfo[]>(ptinfojarr);
                    string feeno = patinfo[0].FeeNo.ToString().Trim();
                    string chartno = patinfo[0].ChartNo.ToString().Trim();
                    if (chartNO != "")
                    {
                        if (chartno != chartNO)
                        {
                            continue;
                        }
                    }
                    string name = patinfo[0].PatientName.ToString().Trim();
                    string sex = patinfo[0].PatientGender.ToString().Trim();

                    string strMaster = "SELECT * FROM DATA_BILLING_CONFIRM_MASTER WHERE FEENO ='" + feeno + "' AND STATUS = 'Y' AND RECORD_DATE BETWEEN TO_DATE('" + start + "','yyyy-MM-dd HH24:mi:ss') and TO_DATE('" + end + "','yyyy-MM-dd HH24:mi:ss') ORDER BY RECORD_DATE DESC ";
                    DataTable dtMaster = this.link.DBExecSQL(strMaster);

                    string serialM = "";

                    if (dtMaster.Rows.Count > 0)
                    {
                        for (int j = 0; j < dtMaster.Rows.Count; j++)
                        {
                            serialM = dtMaster.Rows[j]["SERIAL_M"].ToString();
                            string strBill = "SELECT * FROM DATA_BILLING_CONFIRM_DETAIL WHERE FEENO = '" + feeno + "' AND STATUS = 'Y' AND SERIAL_M = '" + serialM + "' ORDER BY RECORD_DATE DESC";
                            DataTable dtBill = this.link.DBExecSQL(strBill);
                            if (dtBill.Rows.Count > 0)
                            {
                                Bill_Summary_Master SummarydataPT = new Bill_Summary_Master();

                                List<Bill_Summary_Detail> datalist = new List<Bill_Summary_Detail>();

                                foreach (DataRow dr in dtBill.Rows)
                                {
                                    Bill_Summary_Detail data = new Bill_Summary_Detail();

                                    data.HO_ID = dr["HO_ID"].ToString();
                                    data.ITEM_NAME = dr["ITEM_NAME"].ToString();
                                    data.ITEM_TYPE = dr["ITEM_TYPE"].ToString();
                                    data.ITEM_PRICE = dr["ITEM_PRICE"].ToString();
                                    data.COUNT = dr["COUNT"].ToString();

                                    string COSTNAME = costlist.Where(item => item.CostCenterCode.Trim() == dr["COSTCODE"].ToString()).FirstOrDefault()?.CCCDescription;
                                    data.COSTCODE = COSTNAME ?? dr["COSTCODE"].ToString();

                                    //data.COSTCODE = dr["COSTCODe"].ToString();
                                    data.ITEM_IDENTITY = dr["ITEM_IDENTITY"].ToString();
                                    data.RECORD_DATE = dr["RECORD_DATE"].ToString();
                                    data.CREATE_ID = dr["CREATE_ID"].ToString();
                                    data.COVER = dr["COVER"].ToString();
                                    data.NH_PRICE = dr["NH_PRICE"].ToString();
                                    data.SELF_PRICE = dr["SELF_PRICE"].ToString();
                                    data.SERIAL = dr["SERIAL_D"].ToString();
                                    data.SET = dr["SET_NAME"].ToString();
                                    data.DOCTOR = dr["DOCTOR"].ToString();
                                    data.BRING_STATUS = dr["BRING_STATUS"].ToString();
                                    data.NH_CODE = dr["NH_CODE"].ToString();

                                    datalist.Add(data);
                                }
                                SummarydataPT.FEENO = feeno;
                                SummarydataPT.CHARTNO = chartno;
                                SummarydataPT.BEDNO = bedNo;
                                SummarydataPT.NAME = name;
                                SummarydataPT.SEX = sex;
                                SummarydataPT.Bill_D = datalist;
                                Summarydata.Add(SummarydataPT);
                            }
                        }
                    }
                }
            }
            ViewBag.Summary = Summarydata;

            return View();
        }



        //儲存計價摘要 從待確認到已確認

        public ActionResult SaveBillingTemp(List<Bill_Summary_Master> data, string date)
        {
            RESPONSE_MSG jsonResult = new RESPONSE_MSG();
            List<DBItem> insertDataList = new List<DBItem>();
            int erow = 0;
            try
            {
                if (data.Count > 0)
                {
                    string userno = userinfo.EmployeesNo;
                    string now = DateTime.Now.ToString("yyyy/MM/dd/ HH:mm:ss");

                    for (int i = 0; i < data.Count; i++)
                    {
                        string start = DateTime.Now.ToString("yyyy-MM-dd 00:00:00");
                        string end = DateTime.Now.ToString("yyyy-MM-dd 23:59:59");
                        DateTime startRange = DateTime.Parse(start);
                        DateTime endRange = DateTime.Parse(end);
                        string chartno = data[i].CHARTNO.ToString().Trim();
                        string bedno = data[i].BEDNO.ToString().Trim();
                        string feeno = data[i].FEENO.ToString().Trim();
                        string serial = "";
                        string ptType = ptinfo.Assessment;

                        List<Bill_Summary_Detail> detail = new List<Bill_Summary_Detail>();
                        detail = data[i].Bill_D;
                        if (detail != null)
                        {
                            string str = "SELECT * FROM DATA_BILLING_CONFIRM_MASTER WHERE FEENO ='" + feeno + "' AND STATUS = 'Y' AND RECORD_DATE BETWEEN TO_DATE('" + start + "','yyyy-MM-dd HH24:mi:ss') and TO_DATE('" + end + "','yyyy-MM-dd HH24:mi:ss') ";
                            DataTable dt = this.link.DBExecSQL(str);

                            if (dt.Rows.Count > 0)
                            {
                                serial = dt.Rows[0]["SERIAL_M"].ToString();

                                for (int j = 0; j < detail.Count; j++)
                                {
                                    string serialD = creatid("BILL_DATA_D", userno, feeno, "0");
                                    string record_date = DateTime.Parse(detail[j].RECORD_DATE).ToString("yyyy-MM-dd HH:mm:ss");


                                    var record_date_temp = DateTime.Parse(detail[j].RECORD_DATE.ToString());
                                    if (record_date_temp > startRange && record_date_temp < endRange)
                                    {

                                    }
                                    else
                                    {
                                        start = record_date_temp.ToString("yyyy-MM-dd 00:00:00");
                                        end = record_date_temp.ToString("yyyy-MM-dd 23:59:59");
                                        string strOld = "SELECT * FROM DATA_BILLING_CONFIRM_MASTER WHERE FEENO ='" + feeno + "' AND STATUS = 'Y' AND RECORD_DATE BETWEEN TO_DATE('" + start + "','yyyy-MM-dd HH24:mi:ss') and TO_DATE('" + end + "','yyyy-MM-dd HH24:mi:ss') ";
                                        DataTable dtOld = this.link.DBExecSQL(strOld);
                                        if (dtOld.Rows.Count > 0)
                                        {
                                            serial = dtOld.Rows[0]["SERIAL_M"].ToString();
                                        }
                                        else
                                        {
                                            serial = creatid("BILL_DATA_M", userno, feeno, "0");
                                            insertDataList.Clear();
                                            insertDataList.Add(new DBItem("SERIAL_M", serial, DBItem.DBDataType.String));
                                            insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                                            insertDataList.Add(new DBItem("CHARTNO", chartno, DBItem.DBDataType.String));
                                            insertDataList.Add(new DBItem("CREATE_DATE", now, DBItem.DBDataType.DataTime));
                                            insertDataList.Add(new DBItem("CREATE_ID", userno, DBItem.DBDataType.String));
                                            insertDataList.Add(new DBItem("RECORD_DATE", record_date_temp.ToString("yyyy-MM-dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                                            insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                                            insertDataList.Add(new DBItem("BRING_STATUS", "N", DBItem.DBDataType.String));

                                            if (ptType == "ER")
                                            {
                                                ptType = "E";
                                            }
                                            else
                                            {
                                                ptType = "I";
                                            }

                                            insertDataList.Add(new DBItem("PT_TYPE", ptType, DBItem.DBDataType.String));
                                            erow = this.link.DBExecInsert("DATA_BILLING_CONFIRM_MASTER", insertDataList);
                                        }
                                    }
                                    insertDataList.Clear();
                                    insertDataList.Add(new DBItem("SERIAL_M", serial, DBItem.DBDataType.String));
                                    insertDataList.Add(new DBItem("SERIAL_D", serialD, DBItem.DBDataType.String));
                                    insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                                    insertDataList.Add(new DBItem("HO_ID", detail[j].HO_ID.Trim(), DBItem.DBDataType.String));
                                    insertDataList.Add(new DBItem("ITEM_NAME", detail[j].ITEM_NAME.Trim(), DBItem.DBDataType.String));
                                    insertDataList.Add(new DBItem("ITEM_TYPE", detail[j].ITEM_TYPE, DBItem.DBDataType.String));
                                    insertDataList.Add(new DBItem("ITEM_IDENTITY", detail[j].ITEM_IDENTITY, DBItem.DBDataType.String));
                                    insertDataList.Add(new DBItem("ITEM_PRICE", detail[j].ITEM_PRICE, DBItem.DBDataType.String));
                                    if (detail[j].COUNT != null)
                                    {
                                        insertDataList.Add(new DBItem("COUNT", detail[j].COUNT, DBItem.DBDataType.String));
                                    }
                                    else
                                    {
                                        insertDataList.Add(new DBItem("COUNT", "1", DBItem.DBDataType.String));
                                    }
                                    insertDataList.Add(new DBItem("COSTCODE", detail[j].COSTCODE, DBItem.DBDataType.String));
                                    insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                                    insertDataList.Add(new DBItem("CREATE_ID", userno, DBItem.DBDataType.String));
                                    insertDataList.Add(new DBItem("RECORD_DATE", record_date, DBItem.DBDataType.DataTime));
                                    insertDataList.Add(new DBItem("COVER", detail[j].COVER, DBItem.DBDataType.String));
                                    insertDataList.Add(new DBItem("SET_NAME", detail[j].SET, DBItem.DBDataType.String));
                                    insertDataList.Add(new DBItem("DOCTOR", detail[j].DOCTOR, DBItem.DBDataType.String));
                                    insertDataList.Add(new DBItem("BRING_STATUS", "N", DBItem.DBDataType.String));
                                    insertDataList.Add(new DBItem("NH_CODE", detail[j].NH_CODE, DBItem.DBDataType.String));
                                    insertDataList.Add(new DBItem("PT_COSTCODE", detail[j].PT_COSTCODE, DBItem.DBDataType.String));
                                    insertDataList.Add(new DBItem("SELF_PRICE", detail[j].SELF_PRICE, DBItem.DBDataType.String));

                                    if (ptType == "ER")
                                    {
                                        ptType = "E";
                                    }
                                    else
                                    {
                                        ptType = "I";
                                    }

                                    insertDataList.Add(new DBItem("PT_TYPE", ptType, DBItem.DBDataType.String));

                                    erow = this.link.DBExecInsert("DATA_BILLING_CONFIRM_DETAIL", insertDataList);

                                    insertDataList.Clear();
                                    insertDataList.Add(new DBItem("BRING_STATUS", "N", DBItem.DBDataType.String));
                                    erow = this.link.DBExecUpdate("DATA_BILLING_CONFIRM_MASTER", insertDataList, "SERIAL_M = '" + serial + "'");

                                    if (erow > 0)
                                    {
                                        insertDataList.Clear();
                                        insertDataList.Add(new DBItem("STATUS", "N", DBItem.DBDataType.String));
                                        erow = this.link.DBExecUpdate("DATA_BILLING_TEMP_DETAIL", insertDataList, "SERIAL_D = '" + detail[j].SERIAL.ToString().Trim() + "'");
                                    }
                                }
                            }
                            else
                            {
                                //Master
                                serial = creatid("BILL_DATA_M", userno, feeno, "0");
                                insertDataList.Clear();
                                insertDataList.Add(new DBItem("SERIAL_M", serial, DBItem.DBDataType.String));
                                insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                                insertDataList.Add(new DBItem("CHARTNO", chartno, DBItem.DBDataType.String));
                                insertDataList.Add(new DBItem("CREATE_DATE", now, DBItem.DBDataType.DataTime));
                                insertDataList.Add(new DBItem("CREATE_ID", userno, DBItem.DBDataType.String));
                                insertDataList.Add(new DBItem("RECORD_DATE", now, DBItem.DBDataType.DataTime));
                                insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                                insertDataList.Add(new DBItem("BRING_STATUS", "N", DBItem.DBDataType.String));

                                if (ptType == "ER")
                                {
                                    ptType = "E";
                                }
                                else
                                {
                                    ptType = "I";
                                }

                                insertDataList.Add(new DBItem("PT_TYPE", ptType, DBItem.DBDataType.String));
                                erow = this.link.DBExecInsert("DATA_BILLING_CONFIRM_MASTER", insertDataList);

                                if (erow > 0)
                                {
                                    for (int j = 0; j < detail.Count; j++)
                                    {
                                        string serialD = creatid("BILL_DATA_D", userno, feeno, "0");

                                        var record_date_temp = DateTime.Parse(detail[j].RECORD_DATE.ToString());
                                        if (record_date_temp > startRange && record_date_temp < endRange)
                                        {

                                        }
                                        else
                                        {
                                            start = record_date_temp.ToString("yyyy-MM-dd 00:00:00");
                                            end = record_date_temp.ToString("yyyy-MM-dd 23:59:59");
                                            string strOld = "SELECT * FROM DATA_BILLING_CONFIRM_MASTER WHERE FEENO ='" + feeno + "' AND STATUS = 'Y' AND RECORD_DATE BETWEEN TO_DATE('" + start + "','yyyy-MM-dd HH24:mi:ss') and TO_DATE('" + end + "','yyyy-MM-dd HH24:mi:ss') ";
                                            DataTable dtOld = this.link.DBExecSQL(strOld);
                                            if (dtOld.Rows.Count > 0)
                                            {
                                                serial = dtOld.Rows[0]["SERIAL_M"].ToString();
                                            }
                                            else
                                            {
                                                serial = creatid("BILL_DATA_M", userno, feeno, "0");
                                                insertDataList.Clear();
                                                insertDataList.Add(new DBItem("SERIAL_M", serial, DBItem.DBDataType.String));
                                                insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                                                insertDataList.Add(new DBItem("CHARTNO", chartno, DBItem.DBDataType.String));
                                                insertDataList.Add(new DBItem("CREATE_DATE", now, DBItem.DBDataType.DataTime));
                                                insertDataList.Add(new DBItem("CREATE_ID", userno, DBItem.DBDataType.String));
                                                insertDataList.Add(new DBItem("RECORD_DATE", record_date_temp.ToString("yyyy-MM-dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                                                insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));

                                                if (ptType == "ER")
                                                {
                                                    ptType = "E";
                                                }
                                                else
                                                {
                                                    ptType = "I";
                                                }

                                                insertDataList.Add(new DBItem("PT_TYPE", ptType, DBItem.DBDataType.String));
                                                erow = this.link.DBExecInsert("DATA_BILLING_CONFIRM_MASTER", insertDataList);
                                            }
                                        }
                                        insertDataList.Clear();
                                        insertDataList.Add(new DBItem("SERIAL_M", serial, DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("SERIAL_D", serialD, DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("HO_ID", detail[j].HO_ID.Trim(), DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("ITEM_NAME", detail[j].ITEM_NAME.Trim(), DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("ITEM_TYPE", detail[j].ITEM_TYPE, DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("ITEM_IDENTITY", detail[j].ITEM_IDENTITY, DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("ITEM_PRICE", detail[j].ITEM_PRICE, DBItem.DBDataType.String));
                                        if (detail[j].COUNT != null)
                                        {
                                            insertDataList.Add(new DBItem("COUNT", detail[j].COUNT, DBItem.DBDataType.String));
                                        }
                                        else
                                        {
                                            insertDataList.Add(new DBItem("COUNT", "1", DBItem.DBDataType.String));
                                        }
                                        insertDataList.Add(new DBItem("COSTCODE", detail[j].COSTCODE, DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("CREATE_ID", userno, DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("RECORD_DATE", record_date_temp.ToString("yyyy-MM-dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                                        insertDataList.Add(new DBItem("COVER", detail[j].COVER, DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("SET_NAME", detail[j].SET, DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("DOCTOR", detail[j].DOCTOR, DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("BRING_STATUS", "N", DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("NH_CODE", detail[j].NH_CODE, DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("PT_COSTCODE", detail[j].PT_COSTCODE, DBItem.DBDataType.String));
                                        insertDataList.Add(new DBItem("SELF_PRICE", detail[j].SELF_PRICE, DBItem.DBDataType.String));


                                        if (ptType == "ER")
                                        {
                                            ptType = "E";
                                        }
                                        else
                                        {
                                            ptType = "I";
                                        }

                                        insertDataList.Add(new DBItem("PT_TYPE", ptType, DBItem.DBDataType.String));
                                        erow = this.link.DBExecInsert("DATA_BILLING_CONFIRM_DETAIL", insertDataList);
                                        if (erow > 0)
                                        {
                                            insertDataList.Clear();
                                            insertDataList.Add(new DBItem("STATUS", "N", DBItem.DBDataType.String));
                                            erow = this.link.DBExecUpdate("DATA_BILLING_TEMP_DETAIL", insertDataList, "SERIAL_D = '" + detail[j].SERIAL.ToString().Trim() + "'");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.log.saveLogMsg($"[SaveBillingTemp][Error][feeNo:{ptinfo.FeeNo.Trim()}]\ninsertDataList:{JsonConvert.SerializeObject(insertDataList)}\nerrorMessage:{ex.Message.ToString()}", "SaveBillingTemp");
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

        public int delTempDetail(string serial)
        {
            List<DBItem> insertDataList = new List<DBItem>();
            int erow = 0;
            insertDataList.Clear();
            insertDataList.Add(new DBItem("STATUS", "D", DBItem.DBDataType.String));
            erow = this.link.DBExecUpdate("DATA_BILLING_TEMP_DETAIL", insertDataList, "SERIAL_D = '" + serial + "'");

            return 0;
        }
        //手動強制拋轉進入醫院temp
        public int ManualTrans(string feeno)
        {
            byte[] data = Encoding.UTF8.GetBytes("feenoSingle=" + feeno);
            var request = (HttpWebRequest)WebRequest.Create("http://172.20.110.223:8096/api/ECK/NIS_ConfirmDataTransferToHIS_TempByFeeNo");
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Timeout = 15000;

            Stream newStream = request.GetRequestStream();
            newStream.Write(data, 0, data.Length);
            newStream.Close();

            request.ServicePoint.Expect100Continue = false;
            request.ProtocolVersion = HttpVersion.Version11;

            var response = (HttpWebResponse)request.GetResponse();
            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
            if (responseString != null && responseString != "")
            {
                BILL_RESPONSE_MSG jsonResult = new BILL_RESPONSE_MSG();
                jsonResult = JsonConvert.DeserializeObject<BILL_RESPONSE_MSG>(responseString);

                if (jsonResult.status != "ERROR")
                {
                    return 1;
                }
            }

            return 0;
        }


    }
    public class Bill_Summary_Detail
    {
        /// <summary> 序號 </summary>
        public string SERIAL { get; set; }
        /// <summary> 院內碼 </summary>
        public string HO_ID { get; set; }
        /// <summary> 項目名稱 </summary>
        public string ITEM_NAME { get; set; }
        /// <summary> 項目類別 </summary>
        public string ITEM_TYPE { get; set; }
        /// <summary> 項目身份 </summary>
        public string ITEM_IDENTITY { get; set; }
        /// <summary> 項目價格 </summary>
        public string ITEM_PRICE { get; set; }

        public string ITEM_PRICE_NH { get; set; }
        /// <summary> 數量 </summary>
        public string COUNT { get; set; }
        /// <summary> 扣庫單位 </summary>
        public string COSTCODE { get; set; }
        /// <summary> 扣庫單位 </summary>
        public string RECORD_DATE { get; set; }
        /// <summary> 建立者 </summary>
        public string CREATE_ID { get; set; }
        /// <summary> 自費價格 </summary>
        public string SELF_PRICE { get; set; }
        /// <summary> 健保價格 </summary>
        public string NH_PRICE { get; set; }
        /// <summary> 是否回補 </summary>
        public string COVER { get; set; }
        /// <summary> 組套 </summary>
        public string SET { get; set; }
        /// <summary> 醫生編號 </summary>
        public string DOCTOR { get; set; }
        /// <summary> 抓取狀態 </summary>
        public string BRING_STATUS { get; set; }
        /// <summary> 健保碼 </summary>
        public string NH_CODE { get; set; }
        /// <summary> 使用單位成本代碼 </summary>
        public string PT_COSTCODE { get; set; }


    }
    public class Bill_Summary_Master
    {
        /// <summary> 病歷號 </summary>
        public string CHARTNO { get; set; }
        /// <summary> 批價號 </summary>
        public string FEENO { get; set; }
        /// <summary> 病人姓名 </summary>
        public string NAME { get; set; }
        /// <summary> 床號 </summary>
        public string BEDNO { get; set; }
        /// <summary> 性別 </summary>
        public string SEX { get; set; }
        /// <summary> 紀錄時間 </summary>
        public string RECORD_DATE { get; set; }
        /// <summary> 計價明細 </summary>
        public List<Bill_Summary_Detail> Bill_D { get; set; }


    }
    public class Bill_SELECT_LIST
    {
        /// <summary> 單位成本代碼 </summary>
        public string COSTCODE { get; set; }
        /// <summary> 處理狀態 </summary>
        public string BEDNO { get; set; }
        /// <summary> 病歷號 </summary>
        public string CHARTNO { get; set; }
        /// <summary> 批價號 </summary>
        public string FEENO { get; set; }

    }
    public class BILL_RESPONSE_MSG
    {
        /// <summary> 處理狀態 </summary>
        public string status { set; get; }

        /// <summary> 傳回訊息或內容 </summary>
        public string message { set; get; }

        /// <summary> 附帶物件 </summary>
        public object attachment { set; get; }


    }
}
