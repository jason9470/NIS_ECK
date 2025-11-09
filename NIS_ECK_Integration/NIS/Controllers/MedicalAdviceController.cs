using Newtonsoft.Json;
using NIS.Data;
using NIS.Models;
using NIS.UtilTool;
using NIS.WebService;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Web.Mvc;
using System.Net;
using System.IO;
using System.Text;
using Com.Mayaminer;
using System.Linq;
using System.Data;

namespace NIS.Controllers
{
    public class MedicalAdviceController : BaseController
    {

        private DBConnector link;
        private LogTool log;
        private CommData cd;
        DateTime? nullDateTime = null;
        /// <summary> 建構式 </summary>
        public MedicalAdviceController()
        {
            this.link = new DBConnector();
            this.log = new LogTool();
            this.cd = new CommData();
        }

        /// <summary> 醫囑簽收 </summary>
        public ActionResult Sign(string type)
        {
            try
            {
                if (type == "" || type == null)
                {
                    type = "R";
                }
                if (Session["PatInfo"] != null)
                {
                    //取得病人文字醫囑資料
                    byte[] textorderByte = webService.GetTextOrder(ptinfo.FeeNo);
                    if (textorderByte != null)
                    {
                        string textorderJsonArr = CompressTool.DecompressString(textorderByte).Replace(">", "> ").Replace("<", "< ");
                        List<TextOrder> textorder = JsonConvert.DeserializeObject<List<TextOrder>>(textorderJsonArr);
                        List<SignOrder> signorder = new List<SignOrder>();
                        string sqlstr = "select * from data_signorder where fee_no = '" + ptinfo.FeeNo + "' and sign_type='T' ORDER BY SIGN_TIME DESC,SHEET_NO ASC ";
                        DataTable Dt = this.link.DBExecSQL(sqlstr);
                        List<SignOrder> sorder = new List<SignOrder>();
                        if (Dt.Rows.Count > 0)
                        {
                            // 取得資料庫有簽過的內容
                            for (int i = 0; i < Dt.Rows.Count; i++)
                            {
                                string sheet_no = Dt.Rows[i]["sheet_no"].ToString().Trim();
                                string fee_no = Dt.Rows[i]["fee_no"].ToString().Trim();
                                sorder.Add(new SignOrder()
                                {
                                    fee_no = fee_no,
                                    sheet_no = sheet_no,
                                    order_type = Dt.Rows[i]["order_type"].ToString().Trim(),
                                    order_content = Dt.Rows[i]["order_content"].ToString().Trim(),
                                    start_date = DateTime.Parse(Dt.Rows[i]["start_date"].ToString().Trim()),
                                    end_date = DateTime.Parse(Dt.Rows[i]["end_date"].ToString().Trim()),
                                    sign_time = DateTime.Parse(Dt.Rows[i]["sign_time"].ToString().Trim()),
                                    sign_user = Dt.Rows[i]["sign_user"].ToString().Trim(),
                                    sign_user_name = Dt.Rows[i]["sign_user_name"].ToString().Trim(),
                                    set_user = Dt.Rows[i]["set_user"].ToString().Trim()
                                   ,
                                    record_name = Dt.Rows[i]["RECORD_NAME"].ToString().Trim(),
                                    //record_time = (reader["RECORD_NAME"].ToString().Trim() != null) ? DateTime.Parse(reader["RECORD_TIME"].ToString().Trim()) : DateTime.MinValue
                                    record_time = (Dt.Rows[i]["RECORD_TIME"] != null) ? Dt.Rows[i]["RECORD_TIME"].ToString().Trim() : ""
                                });
                                //string sheet_no = reader["sheet_no"].ToString().Trim();
                                //string fee_no = reader["fee_no"].ToString().Trim();
                                //sorder.Add(new SignOrder()
                                //{
                                //    fee_no = fee_no,
                                //    sheet_no = sheet_no,
                                //    order_type = reader["order_type"].ToString().Trim(),
                                //    order_content = reader["order_content"].ToString().Trim(),
                                //    start_date = DateTime.Parse(reader["start_date"].ToString().Trim()),
                                //    end_date = DateTime.Parse(reader["end_date"].ToString().Trim()),
                                //    sign_time = DateTime.Parse(reader["sign_time"].ToString().Trim()),
                                //    sign_user = reader["sign_user"].ToString().Trim(),
                                //    sign_user_name = reader["sign_user_name"].ToString().Trim(),
                                //    set_user = reader["set_user"].ToString().Trim()
                                //   ,
                                //    record_name = reader["RECORD_NAME"].ToString().Trim(),
                                //    //record_time = (reader["RECORD_NAME"].ToString().Trim() != null) ? DateTime.Parse(reader["RECORD_TIME"].ToString().Trim()) : DateTime.MinValue
                                //    record_time = (reader["RECORD_TIME"] != null) ? reader["RECORD_TIME"].ToString().Trim() : ""
                                //});
                            }
                        }

                        string i_sign_user = string.Empty, i_sign_user_name = string.Empty, i_set_user = string.Empty, i_DC_FLAG = string.Empty;//
                        string i_record_name = string.Empty;
                        DateTime i_sign_date = DateTime.MinValue;
                        string i_record_time = "";

                        for (int i = 0; i <= textorder.Count - 1; i++)
                        {
                            for (int j = 0; j <= sorder.Count - 1; j++)
                            {
                                if (
                                    sorder[j].sheet_no.Trim() == textorder[i].SheetNo.Trim() &&
                                    sorder[j].order_type.Trim() == textorder[i].Category.Trim() &&
                                    sorder[j].order_content.Trim() == textorder[i].Content.Trim() &&
                                    (sorder[j].start_date == textorder[i].OrderStartDate ||
                                    sorder[j].start_date.ToString("yyyy/MM/dd HH:mm:ss") == DateTime.MinValue.ToString("yyyy/MM/dd HH:mm:ss"))
                                    )
                                {
                                    i_sign_user = sorder[j].sign_user;
                                    i_sign_user_name = sorder[j].sign_user_name;
                                    i_sign_date = sorder[j].sign_time;
                                    i_set_user = sorder[j].set_user;
                                    i_record_name = sorder[j].record_name;
                                    i_record_time = sorder[j].record_time;
                                    //i_DC_FLAG = sorder[j].DC_FLAG;//
                                    break;
                                }
                                else
                                {
                                    i_sign_user = string.Empty;
                                    i_sign_user_name = string.Empty;
                                    i_sign_date = DateTime.MinValue;
                                    i_set_user = string.Empty;
                                    i_record_name = string.Empty;
                                    i_record_time = "";
                                    //i_DC_FLAG = string.Empty;//
                                }
                            }
                            //mod by yungchen 2014/06/03 長期醫囑 僅顯示3天之內
                            //mod by yungchen 2014/06/06 james說其實是"臨時"醫囑 才有顯示3天之內條件...改來改去改到viwe去寫
                            //if (textorder[i].Category.Trim() != "S" || (textorder[i].OrderStartDate.AddDays(3) >= DateTime.Now))
                            {
                                //Mod by yungchen 文字醫囑 DC的照樣顯示 
                                //if (textorder[i].OrderEndDate == DateTime.MinValue || textorder[i].OrderEndDate >= DateTime.Now)
                                {
                                    signorder.Add(new SignOrder()
                                    {
                                        fee_no = ptinfo.FeeNo,
                                        sheet_no = textorder[i].SheetNo.Trim(),
                                        order_type = textorder[i].Category.Trim(),
                                        order_content = textorder[i].Content.Trim(),
                                        start_date = textorder[i].OrderStartDate,
                                        end_date = textorder[i].OrderEndDate,
                                        sign_user = i_sign_user,
                                        sign_user_name = i_sign_user_name,
                                        sign_time = i_sign_date,
                                        set_user = i_set_user,
                                        DC_FLAG = textorder[i].DC_FLAG,
                                        record_name = i_record_name,
                                        record_time = i_record_time
                                    });
                                }
                            }
                        }
                        #region 抓是否有新未簽醫囑
                        string JsonStr = "";
                        byte[] EmployeeByteCode = webService.GetTextOrderItem(ptinfo.FeeNo);
                        if (EmployeeByteCode != null)
                        {
                            JsonStr = CompressTool.DecompressString(EmployeeByteCode);
                            List<OrderItem> TextList = JsonConvert.DeserializeObject<List<OrderItem>>(JsonStr);
                            ViewData["TextList"] = TextList;
                        }
                        #endregion
                        #region --測試資料 --
                        //mod塞資料 by jarvis 2016/06/13
                        //signorder.Add(new SignOrder()
                        //{
                        //    fee_no = ptinfo.FeeNo,
                        //    sheet_no = "555555",
                        //    order_type = "S",
                        //    order_content = "【090220 K (Potassium) (Blood)】採檢時間:0600 Serum",
                        //    start_date = Convert.ToDateTime("2016/06/11 01:00:00"),
                        //    end_date = Convert.ToDateTime("2016/12/11 01:00:00"),
                        //    sign_user = i_sign_user,
                        //    sign_user_name = i_sign_user_name,
                        //    sign_time = i_sign_date,
                        //    set_user = i_set_user,
                        //    DC_FLAG = ""
                        //});
                        //signorder.Add(new SignOrder()
                        //{
                        //    fee_no = ptinfo.FeeNo,
                        //    sheet_no = "666666",
                        //    order_type = "S",
                        //    order_content = "【090220 K (Potassium) (Blood)】採檢時間:0600 Serum",
                        //    start_date = Convert.ToDateTime("2016/06/11 13:00:00"),
                        //    end_date = DateTime.MaxValue,
                        //    sign_user = i_sign_user,
                        //    sign_user_name = i_sign_user_name,
                        //    sign_time = i_sign_date,
                        //    set_user = i_set_user,
                        //    DC_FLAG = ""
                        //});
                        //signorder.Add(new SignOrder()
                        //{
                        //    fee_no = ptinfo.FeeNo,
                        //    sheet_no = "777777",
                        //    order_type = "S",
                        //    order_content = "【090220 K (Potassium) (Blood)】採檢時間:0600 Serum",
                        //    start_date = Convert.ToDateTime("2016/07/01 01:00:00"),
                        //    end_date = DateTime.MaxValue,
                        //    sign_user = i_sign_user,
                        //    sign_user_name = i_sign_user_name,
                        //    sign_time = i_sign_date,
                        //    set_user = i_set_user,
                        //    DC_FLAG = ""
                        //});
                        //signorder.Add(new SignOrder()
                        //{
                        //    fee_no = ptinfo.FeeNo,
                        //    sheet_no = "888888",
                        //    order_type = "S",
                        //    order_content = "【090220 K (Potassium) (Blood)】採檢時間:0600 Serum",
                        //    start_date = Convert.ToDateTime("2016/06/14 01:00:00"),
                        //    end_date = DateTime.MaxValue,
                        //    sign_user = i_sign_user,
                        //    sign_user_name = i_sign_user_name,
                        //    sign_time = i_sign_date,
                        //    set_user = i_set_user,
                        //    DC_FLAG = ""
                        //});
                        //signorder.Add(new SignOrder()
                        //{
                        //    fee_no = ptinfo.FeeNo,
                        //    sheet_no = "999999",
                        //    order_type = "R",
                        //    order_content = "【090220 K (Potassium) (Blood)】採檢時間:0600 Serum999999",
                        //    start_date = Convert.ToDateTime("2016/06/10 01:00:00"),
                        //    end_date = Convert.ToDateTime("2016/06/12 01:00:00"),
                        //    sign_user = "",
                        //    sign_user_name = "",
                        //    sign_time = i_sign_date,
                        //    set_user = "",
                        //    DC_FLAG = "DC"
                        //});
                        #endregion
                        //signorder.RemoveAll(x => x.start_date > DateTime.Now);//把超過現在的過濾掉 by jarvis 20160614(暫時不用，使用前段判斷)
                        signorder = signorder.OrderBy(x => x.sign_time).ToList();//將簽過的放在前面 by jarvis 20160902
                        ViewBag.ordertype = type;
                        ViewData["signorder"] = signorder;
                    }
                }
                else
                {
                    Response.Write("<script>alert('請重新選擇病患');</script>");
                    return new EmptyResult();
                }
                return View();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(),ex);

                Response.Write("<script>alert('請重新選擇病患');</script>");
                return new EmptyResult();
            }
        }

        /// <summary>
        ///  醫囑簽收 交班單檢視用
        /// </summary>
        public ActionResult Sign_View(string feeno)
        {
            try
            {
                //feeno = feeno.ToString().Trim().Substring(0, feeno.ToString().Trim().Length - 1);
                //取得病人文字醫囑資料
                byte[] textorderByte = webService.GetTextOrder(feeno);
                if(textorderByte != null)
                {

                    string textorderJsonArr = CompressTool.DecompressString(textorderByte).Replace(">", "> ").Replace("<", "< ");
                    List<TextOrder> textorder = JsonConvert.DeserializeObject<List<TextOrder>>(textorderJsonArr);
                    List<SignOrder> signorder = new List<SignOrder>();

                    string sqlstr = "select * from data_signorder where fee_no = '" + feeno + "' and sign_type='T' ";
                    DataTable Dt = this.link.DBExecSQL(sqlstr);
                    List<SignOrder> sorder = new List<SignOrder>();

                    // 取得資料庫有簽過的內容
                    if (Dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            sorder.Add(new SignOrder()
                            {
                                fee_no = Dt.Rows[i]["fee_no"].ToString().Trim(),
                                sheet_no = Dt.Rows[i]["sheet_no"].ToString().Trim(),
                                order_type = Dt.Rows[i]["order_type"].ToString().Trim(),
                                order_content = Dt.Rows[i]["order_content"].ToString().Trim(),
                                start_date = DateTime.Parse(Dt.Rows[i]["start_date"].ToString().Trim()),
                                end_date = DateTime.Parse(Dt.Rows[i]["end_date"].ToString().Trim()),
                                sign_time = DateTime.Parse(Dt.Rows[i]["sign_time"].ToString().Trim()),
                                sign_user = Dt.Rows[i]["sign_user"].ToString().Trim(),
                                sign_user_name = Dt.Rows[i]["sign_user_name"].ToString().Trim(),
                                set_user = Dt.Rows[i]["set_user"].ToString().Trim()
                                ,
                                record_name = Dt.Rows[i]["RECORD_NAME"].ToString().Trim(),
                                //record_time = (reader["RECORD_NAME"].ToString().Trim() != null) ? DateTime.Parse(reader["RECORD_TIME"].ToString().Trim()) : DateTime.MinValue
                                record_time = (Dt.Rows[i]["RECORD_TIME"] != null) ? Dt.Rows[i]["RECORD_TIME"].ToString().Trim() : ""
                            });
                        }
                    }

                    string i_sign_user = string.Empty, i_sign_user_name = string.Empty, i_set_user = string.Empty;
                    DateTime i_sign_date = DateTime.MinValue;
                    string i_record_name = string.Empty; string i_record_time = "";
                    for(int i = 0; i <= textorder.Count - 1; i++)
                    {
                        for(int j = 0; j <= sorder.Count - 1; j++)
                        {
                            if(
                                sorder[j].sheet_no.Trim() == textorder[i].SheetNo.Trim() &&
                                sorder[j].order_type.Trim() == textorder[i].Category.Trim() &&
                                sorder[j].order_content.Trim() == textorder[i].Content.Trim() &&
                                (sorder[j].start_date == textorder[i].OrderStartDate ||
                                sorder[j].start_date.ToString("yyyy/MM/dd HH:mm:ss") == DateTime.MinValue.ToString("yyyy/MM/dd HH:mm:ss"))
                                )
                            {
                                i_sign_user = sorder[j].sign_user;
                                i_sign_user_name = sorder[j].sign_user_name;
                                i_sign_date = sorder[j].sign_time;
                                i_set_user = sorder[j].set_user;
                                i_record_name = sorder[j].record_name;
                                i_record_time = sorder[j].record_time;
                                break;
                            }
                            else
                            {
                                i_sign_user = string.Empty;
                                i_sign_user_name = string.Empty;
                                i_sign_date = DateTime.MinValue;
                                i_set_user = string.Empty;
                                i_record_name = string.Empty;
                                i_record_time = "";
                            }
                        }
                        //mod by yungchen 2014/06/03 長期醫囑 僅顯示3天之內
                        //mod by yungchen 2014/06/06 james說其實是"臨時"醫囑 才有顯示3天之內條件
                        // if (textorder[i].Category.Trim() != "S" || (textorder[i].OrderStartDate.AddDays(3) >= DateTime.Now))
                        {
                            //if(textorder[i].OrderEndDate == DateTime.MinValue || textorder[i].OrderEndDate >= DateTime.Now)
                            {
                                signorder.Add(new SignOrder()
                                {
                                    fee_no = feeno,
                                    sheet_no = textorder[i].SheetNo.Trim(),
                                    order_type = textorder[i].Category.Trim(),
                                    order_content = textorder[i].Content.Trim(),
                                    start_date = textorder[i].OrderStartDate,
                                    end_date = textorder[i].OrderEndDate,
                                    sign_user = i_sign_user,
                                    sign_user_name = i_sign_user_name,
                                    sign_time = i_sign_date,
                                    set_user = i_set_user,
                                    DC_FLAG = textorder[i].DC_FLAG,
                                    record_name = i_record_name,
                                    record_time = i_record_time
                                });
                            }
                        }
                    }
                    signorder = signorder.Where(x => x.DC_FLAG != "DC").ToList();//將DC_FLAG == "DC" 的資料，預設排除 by jarvis 20161228
                    signorder = signorder.OrderBy(x => x.sign_time).ThenBy(x=>x.start_date).ToList();//將簽過的放在前面 by jarvis 20160902
                    //ViewBag.ordertype = type;
                    ViewData["signorder"] = signorder;
                }
                return View();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(),ex);
                return View();
            }
        }

        /// <summary> 醫囑執行 </summary>
        public ActionResult Excute(string type)
        {
            try
            {
                if (type == "" || type == null)
                {
                    type = "R";
                }
                byte[] drugOrderByteCode = webService.GetIpdDrugOrder(ptinfo.FeeNo);
                if(drugOrderByteCode != null)
                {
                    List<SignOrder> drugOrder = new List<SignOrder>();
                    string drugOrderJobj = CompressTool.DecompressString(drugOrderByteCode).Replace(">", "> ").Replace("<", "< ");
                    List<DrugOrder> drugOrderList = JsonConvert.DeserializeObject<List<DrugOrder>>(drugOrderJobj);

                    string sqlstr = "select * from data_signorder where fee_no = '" + ptinfo.FeeNo + "' and sign_type='M' ";

                    DataTable Dt = this.link.DBExecSQL(sqlstr);
                    List<SignOrder> sorder = new List<SignOrder>();
                    // 取得資料庫有簽過的內容
                    if (Dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            sorder.Add(new SignOrder()
                            {
                                fee_no = Dt.Rows[i]["fee_no"].ToString().Trim(),
                                sheet_no = Dt.Rows[i]["sheet_no"].ToString().Trim(),
                                order_type = Dt.Rows[i]["order_type"].ToString().Trim(),
                                cir_code = Dt.Rows[i]["cir_code"].ToString().Trim(),
                                path_code = Dt.Rows[i]["path_code"].ToString().Trim(),
                                order_content = Dt.Rows[i]["order_content"].ToString().Trim(),
                                start_date = DateTime.Parse(Dt.Rows[i]["start_date"].ToString().Trim()),
                                end_date = DateTime.Parse(Dt.Rows[i]["end_date"].ToString().Trim()),
                                sign_time = DateTime.Parse(Dt.Rows[i]["sign_time"].ToString().Trim()),
                                sign_user = Dt.Rows[i]["sign_user"].ToString().Trim(),
                                sign_user_name = Dt.Rows[i]["sign_user_name"].ToString().Trim(),
                                set_user = Dt.Rows[i]["set_user"].ToString().Trim(),
                            });
                        }
                    }

                    // inital 
                    string i_sign_user = string.Empty, i_sign_user_name = string.Empty, i_set_user = string.Empty;
                    DateTime i_sign_date = DateTime.MinValue;

                    for(int i = 0; i <= drugOrderList.Count - 1; i++)
                    {
                        for(int j = 0; j <= sorder.Count - 1; j++)
                        {
                            if(
                                sorder[j].sheet_no == drugOrderList[i].SheetNo.Trim() &&
                                sorder[j].order_content == drugOrderList[i].DrugName.Trim() + "|" + drugOrderList[i].GenericDrugs.Trim() &&
                                sorder[j].cir_code == drugOrderList[i].Feq.Trim() &&
                                sorder[j].path_code == drugOrderList[i].Route.Trim() &&
                                (sorder[j].start_date == drugOrderList[i].OrderStartDate ||
                                sorder[j].start_date.ToString("yyyy/MM/dd HH:mm:ss") == DateTime.MinValue.ToString("yyyy/MM/dd HH:mm:ss"))
                                )
                            {
                                i_sign_user = sorder[j].sign_user;
                                i_sign_user_name = sorder[j].sign_user_name;
                                i_sign_date = sorder[j].sign_time;
                                i_set_user = sorder[j].set_user;
                                break;
                            }
                            else
                            {
                                i_sign_user = string.Empty;
                                i_sign_user_name = string.Empty;
                                i_sign_date = DateTime.MinValue;
                                i_set_user = string.Empty;
                            }
                        }
                        if(drugOrderList[i].OrderEndDate == DateTime.MinValue || drugOrderList[i].OrderEndDate >= DateTime.Now
                            || drugOrderList[i].Category.ToString().Trim() == "S" || drugOrderList[i].DcFlag.ToString().Trim() == "Y")
                        {
                            drugOrder.Add(new SignOrder()
                            {
                                fee_no = ptinfo.FeeNo,
                                sheet_no = drugOrderList[i].SheetNo.ToString().Trim(),
                                order_type = drugOrderList[i].Category.ToString().Trim(),
                                order_content = drugOrderList[i].DrugName.ToString().Trim() + "|" + drugOrderList[i].GenericDrugs.ToString().Trim(),
                                cir_code = drugOrderList[i].Feq.ToString().Trim(),
                                path_code = drugOrderList[i].Route.ToString().Trim(),
                                start_date = drugOrderList[i].OrderStartDate,
                                pre_qty = drugOrderList[i].Dose.ToString(),
                                memo = drugOrderList[i].Note.ToString().Trim(),
                                unit = drugOrderList[i].DoseUnit.ToString().Trim(),
                                end_date = drugOrderList[i].OrderEndDate,
                                sign_user = i_sign_user,
                                sign_user_name = i_sign_user_name,
                                sign_time = i_sign_date,
                                set_user = i_set_user,
                                rate_l = drugOrderList[i].RateL.ToString().Trim(),
                                rate_h = drugOrderList[i].RateH.ToString().Trim(),
                                rate_memo = drugOrderList[i].RateMemo.ToString().Trim(),
                                //DC_FLAG = (drugOrderList[i].DC_FLAG != null) ? drugOrderList[i].DC_FLAG.ToString().Trim() : ""//**
                                DC_FLAG = drugOrderList[i].DcFlag,
                                IsFRIDs = drugOrderList[i].IsFRIDs,

                            });
                        }
                    }
                    //drugOrder.RemoveAll(x => x.start_date > DateTime.Now);//把超過現在的過濾掉 by jarvis 20160614
                    #region 抓是否有新未簽醫囑
                    string JsonStr = "";

                    byte[] UDByteCode = webService.GetDrugOrderItem(ptinfo.FeeNo);
                    if(UDByteCode != null)
                    {
                        JsonStr = CompressTool.DecompressString(UDByteCode);
                        List<OrderItem> DrugList = JsonConvert.DeserializeObject<List<OrderItem>>(JsonStr);
                        ViewData["DrugList"] = DrugList;
                    }
                    #endregion
                    drugOrder = drugOrder.OrderBy(x => x.sign_time).ToList();//將簽過的放在前面 by jarvis 20160902
                    ViewBag.ordertype = type;
                    ViewData["drugOrder"] = drugOrder;
                }
                return View();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(),ex);
                return View();
            }
        }


        /// <summary> 藥囑執行 交班單檢視用</summary>
        public ActionResult Excute_View(string feeno)
        {
            try
            {
                //feeno = feeno.ToString().Trim().Substring(0, feeno.ToString().Trim().Length - 1);

                byte[] drugOrderByteCode = webService.GetIpdDrugOrder(feeno);
                if(drugOrderByteCode != null)
                {
                    List<SignOrder> drugOrder = new List<SignOrder>();
                    string drugOrderJobj = CompressTool.DecompressString(drugOrderByteCode).Replace(">", "> ").Replace("<", "< ");
                    List<DrugOrder> drugOrderList = JsonConvert.DeserializeObject<List<DrugOrder>>(drugOrderJobj);

                    string sqlstr = "select * from data_signorder where fee_no = '" + feeno + "' and sign_type='M' ";
                    DataTable Dt = this.link.DBExecSQL(sqlstr);
                    List<SignOrder> sorder = new List<SignOrder>();
                    // 取得資料庫有簽過的內容
                    if (Dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            sorder.Add(new SignOrder()
                            {
                                fee_no = Dt.Rows[i]["fee_no"].ToString().Trim(),
                                sheet_no = Dt.Rows[i]["sheet_no"].ToString().Trim(),
                                order_type = Dt.Rows[i]["order_type"].ToString().Trim(),
                                cir_code = Dt.Rows[i]["cir_code"].ToString().Trim(),
                                path_code = Dt.Rows[i]["path_code"].ToString().Trim(),
                                order_content = Dt.Rows[i]["order_content"].ToString().Trim(),
                                start_date = DateTime.Parse(Dt.Rows[i]["start_date"].ToString().Trim()),
                                end_date = DateTime.Parse(Dt.Rows[i]["end_date"].ToString().Trim()),
                                sign_time = DateTime.Parse(Dt.Rows[i]["sign_time"].ToString().Trim()),
                                sign_user = Dt.Rows[i]["sign_user"].ToString().Trim(),
                                sign_user_name = Dt.Rows[i]["sign_user_name"].ToString().Trim(),
                                set_user = Dt.Rows[i]["set_user"].ToString().Trim()
                            });
                        }
                    }

                    // inital 
                    string i_sign_user = string.Empty, i_sign_user_name = string.Empty, i_set_user = string.Empty;
                    DateTime i_sign_date = DateTime.MinValue;

                    for(int i = 0; i <= drugOrderList.Count - 1; i++)
                    {
                        for(int j = 0; j <= sorder.Count - 1; j++)
                        {
                            if(
                                sorder[j].sheet_no == drugOrderList[i].SheetNo.Trim() &&
                                sorder[j].order_content == drugOrderList[i].DrugName.Trim() + "|" + drugOrderList[i].GenericDrugs.Trim() &&
                                sorder[j].cir_code == drugOrderList[i].Feq.Trim() &&
                                sorder[j].path_code == drugOrderList[i].Route.Trim() &&
                                (sorder[j].start_date == drugOrderList[i].OrderStartDate ||
                                sorder[j].start_date.ToString("yyyy/MM/dd HH:mm:ss") == DateTime.MinValue.ToString("yyyy/MM/dd HH:mm:ss"))
                                )
                            {
                                i_sign_user = sorder[j].sign_user;
                                i_sign_user_name = sorder[j].sign_user_name;
                                i_sign_date = sorder[j].sign_time;
                                i_set_user = sorder[j].set_user;
                                break;
                            }
                            else
                            {
                                i_sign_user = string.Empty;
                                i_sign_user_name = string.Empty;
                                i_sign_date = DateTime.MinValue;
                                i_set_user = string.Empty;
                            }
                        }
                        if(drugOrderList[i].OrderEndDate == DateTime.MinValue || drugOrderList[i].OrderEndDate >= DateTime.Now
                            || drugOrderList[i].Category.ToString().Trim() == "S")
                        {
                            drugOrder.Add(new SignOrder()
                            {
                                fee_no = feeno,
                                sheet_no = drugOrderList[i].SheetNo.ToString().Trim(),
                                order_type = drugOrderList[i].Category.ToString().Trim(),
                                order_content = drugOrderList[i].DrugName.ToString().Trim() + "|" + drugOrderList[i].GenericDrugs.ToString().Trim(),
                                cir_code = drugOrderList[i].Feq.ToString().Trim(),
                                path_code = drugOrderList[i].Route.ToString().Trim(),
                                start_date = drugOrderList[i].OrderStartDate,
                                pre_qty = drugOrderList[i].Dose.ToString(),
                                memo = drugOrderList[i].Note.ToString().Trim(),
                                unit = drugOrderList[i].DoseUnit.ToString().Trim(),
                                end_date = drugOrderList[i].OrderEndDate,
                                sign_user = i_sign_user,
                                sign_user_name = i_sign_user_name,
                                sign_time = i_sign_date,
                                set_user = i_set_user,
                                rate_l = drugOrderList[i].RateL.ToString().Trim(),
                                rate_h = drugOrderList[i].RateH.ToString().Trim(),
                                rate_memo = drugOrderList[i].RateMemo.ToString().Trim(),
                                DC_FLAG = drugOrderList[i].DcFlag
                            });
                        }
                    }
                    drugOrder = drugOrder.OrderBy(x => x.sign_time).ThenBy(x => x.start_date).ToList();//將簽過的放在前面 by jarvis 20160902
                    //ViewBag.ordertype = type;
                    ViewData["drugOrder"] = drugOrder;
                }
                return View();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(),ex);
                return View();
            }
        }
        /// <summary> 設定執行 </summary>
        public ActionResult Set_Excute()
        {
            if(Request["id"] != null)
            {
                ViewData["id"] = Request["id"].ToString().Trim();
                ViewData["item"] = Request["item"].ToString().Trim();
            }
            ViewData["doAction"] = this.cd.getSelectItem("system", "doAction");
            return View();
        }

        public ActionResult Save_JobSetting()
        {
            try
            {
                string sheet_no = Request["sheet_no"].ToString().Trim();
                string start_date = Request["start_date"].ToString().Trim();
                string order_content = Request["order_content"].ToString().Trim();
                string set_priod = Request["set_priod"].ToString().Trim();
                string set_action = Request["do_action"].ToString().Trim();
                string whereCondition = "fee_no = '" + ptinfo.FeeNo + "' ";
                whereCondition += "and sheet_no = '" + sheet_no + "' ";
                whereCondition += "and start_date = to_date('" + start_date + "', 'yyyy/MM/dd hh24:mi:ss') ";
                whereCondition += "and order_content like '" + order_content.Replace("'", "''") + "%' ";
                whereCondition += "and length(order_content) =  length('" + order_content.Replace("'", "''") + "')  ";

                List<DBItem> upditem = new List<DBItem>();
                upditem.Add(new DBItem("set_priod", set_priod, DBItem.DBDataType.String));
                upditem.Add(new DBItem("set_user", userinfo.EmployeesNo, DBItem.DBDataType.String));
                upditem.Add(new DBItem("set_action", set_action, DBItem.DBDataType.String));
                upditem.Add(new DBItem("CANCELTIME", nullDateTime.ToString(), DBItem.DBDataType.DataTime));
                int result = this.link.DBExecUpdate("data_signorder", upditem, whereCondition);

                if(result == 0)
                    Response.Write("設定失敗");
                else
                    Response.Write("設定完成");
            }
            catch(Exception ex)
            {
                Response.Write("設定失敗：" + ex.Message.ToString());
            }
            return new EmptyResult();
        }


        /// <summary>
        /// 儲存醫囑簽收
        /// </summary>
        /// <returns></returns>
        public ActionResult SignSave()
        {
            try
            {
                webService.UpdNewOrderFlag(ptinfo.FeeNo);

                if(Request["c_index"] != null)
                {
                    string[] c_index = Request["c_index"].Split(',');
                    string type = Request["order"];
                    string fee_no = ptinfo.FeeNo.Trim();
                    for (int i = 0; i <= c_index.Length - 1; i++)
                    {
                        List<DBItem> signSaveItem = new List<DBItem>();
                        signSaveItem.Add(new DBItem("fee_no", fee_no, DBItem.DBDataType.String));
                        signSaveItem.Add(new DBItem("sheet_no", Request[c_index[i] + "_sheet_no"].ToString().Trim(), DBItem.DBDataType.String));
                        if(Request[c_index[i] + "_order_type"].ToString().Trim() != "")
                            signSaveItem.Add(new DBItem("order_type", Request[c_index[i] + "_order_type"].ToString().Trim(), DBItem.DBDataType.String));
                        if(Request[c_index[i] + "_start_date"].ToString().Trim() != "")
                            signSaveItem.Add(new DBItem("start_date", Request[c_index[i] + "_start_date"].ToString().Trim(), DBItem.DBDataType.DataTime));
                        else
                            signSaveItem.Add(new DBItem("start_date", DateTime.MinValue.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));

                        if(Request[c_index[i] + "_end_date"].ToString().Trim() != "")
                            signSaveItem.Add(new DBItem("end_date", Request[c_index[i] + "_end_date"].ToString().Trim(), DBItem.DBDataType.DataTime));
                        else
                            signSaveItem.Add(new DBItem("end_date", DateTime.MaxValue.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                        signSaveItem.Add(new DBItem("order_content", Request[c_index[i] + "_order_content"].ToString().Trim(), DBItem.DBDataType.String));
                        signSaveItem.Add(new DBItem("sign_time", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                        signSaveItem.Add(new DBItem("sign_user", userinfo.EmployeesNo, DBItem.DBDataType.String));
                        signSaveItem.Add(new DBItem("sign_user_name", userinfo.EmployeesName, DBItem.DBDataType.String));
                        if(type != "med")
                        {
                            signSaveItem.Add(new DBItem("sign_type", "T", DBItem.DBDataType.String));
                            if (Request[c_index[i] + "_sheet_no"].ToString().Trim().StartsWith("D"))
                            {
                                string del_no = Request[c_index[i] + "_sheet_no"].ToString().Trim().Substring(1);
                                int uperow = 0;

                                List<DBItem> updDataList = new List<DBItem>();
                                updDataList.Add(new DBItem("DELETED", userinfo.EmployeesNo, DBItem.DBDataType.String));
                                updDataList.Add(new DBItem("END_DATE", Request[c_index[i] + "_end_date"].ToString().Trim(), DBItem.DBDataType.DataTime));

                                uperow = link.DBExecUpdate("data_signorder", updDataList, " fee_no = '" + fee_no + "' AND sheet_no = '" + del_no + "'");
                            }
                        }
                        else
                        {
                            signSaveItem.Add(new DBItem("sign_type", "M", DBItem.DBDataType.String));
                            signSaveItem.Add(new DBItem("cir_code", Request[c_index[i] + "_cir_code"].ToString().Trim(), DBItem.DBDataType.String));
                            signSaveItem.Add(new DBItem("path_code", Request[c_index[i] + "_path_code"].ToString().Trim(), DBItem.DBDataType.String));
                        }
                        int erow = this.link.DBExecInsert("data_signorder", signSaveItem);
                    }
                    if(type != "med")
                    {
                        return RedirectToAction("Sign", new { @type = Request["displayordertype"], @message = "簽收完成" });
                    }
                    else
                    {
                        return RedirectToAction("Excute", new { @type = Request["ordertype"], @message = "簽收完成" });
                    }

                }
                return new EmptyResult();
                //else
                //    return RedirectToAction("Sign", new { @message = "尚未選擇簽收項目" });
            }
            catch(Exception ex)
            {
                this.log.saveLogMsg(ex, "SignSave");
                return RedirectToAction("Sign", new { @message = ex.Message.ToString() });
            }
        }



        //帶入護理記錄-臨時
        public string SignTakeCareRecord()
        {
            string ChkCare = (Request["ChkCare"] == null) ? "" : Request["ChkCare"].ToString().Trim();
            string temp = Request["temp"].ToString().Trim();
            string P_id = Request["id"].ToString().Trim();//以這個id來判斷是否為批次還是單次(空值為批次，有值為單次)
            string P_sheetno = Request["sheetno"].ToString().Trim();
            string P_content = Request["content"].ToString().Trim();
            //start_time
            string P_start_time = Request["start_time"].ToString().Trim();//醫囑的開始時間
            string PTemp_date_time = Request["Temp_date_time"].ToString().Trim();//註記的當下時間
            string id = base.creatid("TEXTORDER", userinfo.EmployeesNo, ptinfo.FeeNo, "0");//產生id給帶護理記錄用
            string P_order_type = Request["order_type"].ToString().Trim();//單次才使用，舊code就不改了
            P_content = P_content.Replace("#br#", "");////////////////////////by test(取代#br#)
            DateTime NowTime = DateTime.Now;
            int erow = 0;
            if(ChkCare == "Y")
            {//帶護理記錄
                erow = base.Insert_CareRecord_Black(PTemp_date_time, id, "", P_content, "", "", "", "");
            }
            if(P_id != "")
            {//單筆
                List<DBItem> signSaveItem = new List<DBItem>();
                string where = "FEE_NO='" + ptinfo.FeeNo + "' AND SHEET_NO='" + P_sheetno + "' AND ORDER_TYPE='" + P_order_type + "' AND SIGN_TYPE='T' AND to_char(START_DATE,'YYYY/MM/DD HH24:MI:SS')='" + Convert.ToDateTime(P_start_time).ToString("yyyy/MM/dd HH:mm:ss") + "'";
                signSaveItem.Add(new DBItem("RECORD_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                signSaveItem.Add(new DBItem("RECORD_TIME", PTemp_date_time, DBItem.DBDataType.DataTime));
                erow = this.link.DBExecUpdate("DATA_SIGNORDER", signSaveItem, where);//加上falg註記
            }
            else
            {//批次
                string[] ArrP_sheetno = P_sheetno.Split(',');
                string[] ArrP_start_time = P_start_time.Split(',');
                List<DBItem> signSaveItem = new List<DBItem>();
                for(int x = 0; x < ArrP_sheetno.Length; x++)
                {
                    signSaveItem.Clear();
                    string where = "FEE_NO='" + ptinfo.FeeNo + "' AND SHEET_NO='" + ArrP_sheetno[x] + "' AND ORDER_TYPE='S' AND SIGN_TYPE='T' AND to_char(START_DATE,'YYYY/MM/DD HH24:MI:SS')='" + Convert.ToDateTime(ArrP_start_time[x]).ToString("yyyy/MM/dd HH:mm:ss") + "'";
                    signSaveItem.Add(new DBItem("RECORD_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                    signSaveItem.Add(new DBItem("RECORD_TIME", PTemp_date_time, DBItem.DBDataType.DataTime));
                    erow = this.link.DBExecUpdate("DATA_SIGNORDER", signSaveItem, where);//加上falg註記
                }
            }
            if(erow > 0)
            {
                return temp;
            }
            else
            {
                return "N";
            }

        }

        //取消護理記錄(改成只取消註記)-臨時
        public string SignRemoveCareRecord()
        {
            string temp = Request["temp"].ToString().Trim();
            string P_id = Request["id"].ToString().Trim();
            string P_sheetno = Request["sheetno"].ToString().Trim();
            string P_start_time = Request["start_time"].ToString().Trim();
            string P_order_type = Request["order_type"].ToString().Trim();

            List<DBItem> signSaveItem = new List<DBItem>();
            string where = "FEE_NO='" + ptinfo.FeeNo + "' AND SHEET_NO='" + P_sheetno + "' AND ORDER_TYPE='" + P_order_type + "' AND to_char(START_DATE,'YYYY/MM/DD HH24:MI:SS')='" + Convert.ToDateTime(P_start_time).ToString("yyyy/MM/dd HH:mm:ss") + "'";
            signSaveItem.Add(new DBItem("RECORD_NAME", "", DBItem.DBDataType.String));
            signSaveItem.Add(new DBItem("RECORD_TIME", "", DBItem.DBDataType.DataTime));

            int erow = this.link.DBExecUpdate("DATA_SIGNORDER", signSaveItem, where);
            if(erow > 0)
            {
                return temp;
            }
            else
            {
                return "N";
            }
        }
    }
}
