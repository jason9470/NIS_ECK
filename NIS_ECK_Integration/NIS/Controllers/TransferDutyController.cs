using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Diagnostics;
using NIS.Data;
using System.Data;
using System.Data.OleDb;
using Newtonsoft.Json;
using NIS.Models;
using NIS.WebService;
using NIS.UtilTool;
using Com.Mayaminer;
using System.Data.SqlClient;
using System.Configuration;
using System.Net;
using System.Globalization;
using System.Text.RegularExpressions;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace NIS.Controllers
{

    public class TransferDutyController : BaseController
    {
        private DBConnector link;
        private LogTool log;
        private TubeManager tubem;
        private IOManager iom;
        private CareRecord care_m;
        private Wound wound;
        private BaseController baseC = new BaseController();

        public TransferDutyController()
        {
            this.link = new DBConnector();
            this.log = new LogTool();
            this.tubem = new TubeManager();
            this.iom = new IOManager();
            this.care_m = new CareRecord();
            this.wound = new Wound();
        }

        #region 共用Method

        /// <summary>
        /// 取得VitelSign資料
        /// </summary>
        /// <param name="feeno">住院序號</param>
        /// <returns></returns>
        protected internal string[] getTranInfo(string feeno, string shift_cate, string successorName = "", PatientInfo ptinfo = null)
        {
            string type = "a";
            if (ptinfo != null)
            {
                int age = ptinfo.Age;
                type = base.get_check_type(ptinfo); //取得生命徵象異常年紀代號
            }

            List<string> vsinfo = new List<string>();
            try
            {
                string strDate = DateTime.Now.ToString("yyyy/MM/dd");
                int intTime = int.Parse(DateTime.Now.ToString("HHmm"));
                string sqlstr = "WITH table1 AS (SELECT VS_ITEM,	VS_PART,VS_REASON,VS_ID,MODIFY_DATE FROM DATA_VITALSIGN DV ";
                sqlstr += " WHERE FEE_NO = '" + feeno + "' AND VS_ITEM IN ( 'bt', 'mp', 'bf', 'bp', 'sp', 'cv1', 'cv2', 'ic1', 'ic2') ";
                sqlstr += " GROUP BY VS_ITEM,VS_PART,VS_REASON,VS_ID,MODIFY_DATE ORDER BY VS_ITEM,VS_ID),";
                sqlstr += " table2 AS ( SELECT VS_ITEM, MAX( MODIFY_DATE ) AS MODIFY_DATE FROM table1 GROUP BY VS_ITEM ), ";
                sqlstr += " table3 AS ( SELECT table1.* FROM table1 INNER JOIN table2 ON table1.VS_ITEM = table2.VS_ITEM AND table1.MODIFY_DATE = table2.MODIFY_DATE ) ";
                sqlstr += " SELECT table4.FEE_NO,table4.VS_ITEM,table4.VS_PART,table4.VS_REASON,table4.VS_RECORD AS ITEM_VALUE FROM table3";
                sqlstr += " INNER JOIN DATA_VITALSIGN table4 ON table3.VS_ID = table4.VS_ID AND table3.VS_ITEM = table4.VS_ITEM AND table3.MODIFY_DATE = table4.MODIFY_DATE";

                vsinfo.Add((successorName != "") ? "接班者：" + successorName : "");
                //Vital Sign

                List<Dictionary<string, string>> Temp = new List<Dictionary<string, string>>();
                Dictionary<string, string> temp = null;
                DataTable dt_check = null;

                DataTable Dt = link.DBExecSQL(sqlstr);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        temp = new Dictionary<string, string>();
                        temp["VS_ITEM"] = Dt.Rows[i]["VS_ITEM"].ToString();
                        temp["VS_PART"] = Dt.Rows[i]["VS_PART"].ToString();
                        temp["VS_REASON"] = Dt.Rows[i]["VS_REASON"].ToString();
                        temp["ITEM_VALUE"] = Dt.Rows[i]["ITEM_VALUE"].ToString();
                        Temp.Add(temp);
                    }
                }

                #region 異常值判斷
                if (Temp.Count > 0)
                {
                    int rownum = 3;
                    List<Dictionary<string, string>> VitalSignList = new List<Dictionary<string, string>>();

                    VitalSignList = Temp.FindAll(x => x["VS_ITEM"] == "bt" && (!string.IsNullOrEmpty(x["ITEM_VALUE"]) || x["VS_REASON"] == "測不到"));
                    dt_check = Get_Check_Abnormal_dt();
                    string color = "black";
                    if (VitalSignList.Count > 0)
                    {
                        for (int i = 0; i < ((VitalSignList.Count < rownum) ? VitalSignList.Count : rownum); i++)
                        {
                            color = "black";
                            string l_check = "btl_e", h_check = "bth_e";
                            string ck_value = VitalSignList[i]["ITEM_VALUE"], part = VitalSignList[i]["VS_PART"];
                            if (part == "腋溫")
                            {
                                l_check = "btl_a";
                                h_check = "bth_a";
                            }
                            else if (part == "肛溫")
                            {
                                l_check = "btl_r";
                                h_check = "bth_r";
                            }
                            else if (part == "額溫")
                            {
                                l_check = "btl_f";
                                h_check = "bth_f";
                            }
                            string measure = VitalSignList[i]["VS_REASON"];
                            foreach (DataRow r in dt_check.Rows)
                            {
                                if (measure == "測不到")
                                {
                                    color = "red";
                                }
                                else
                                {
                                    if (r["MODEL_ID"].ToString() == l_check || r["MODEL_ID"].ToString() == h_check)
                                    {
                                        if (r["DECIDE"].ToString() == ">")
                                        {
                                            if (double.Parse(ck_value) > double.Parse(r["VALUE_LIMIT"].ToString()))
                                            {
                                                color = "red";
                                            }
                                        }
                                        else if (r["DECIDE"].ToString() == "<")
                                        {
                                            if (double.Parse(ck_value) < double.Parse(r["VALUE_LIMIT"].ToString()))
                                            {
                                                color = "blue";
                                            }
                                        }
                                    }
                                }
                            }

                            vsinfo.Add(string.Format("BT：{0}"
                                , (measure == "測不到") ? "<font color='" + color + "'>" + measure + "</font>" : "<font color='" + color + "'>" + VitalSignList[i]["ITEM_VALUE"] + "</font>℃"
                                ));
                        }
                    }
                    VitalSignList = Temp.FindAll(x => x["VS_ITEM"] == "mp" && (!string.IsNullOrEmpty(x["ITEM_VALUE"]) || x["VS_REASON"] == "測不到"));
                    if (VitalSignList.Count > 0)
                    {
                        for (int i = 0; i < ((VitalSignList.Count < rownum) ? VitalSignList.Count : rownum); i++)
                        {
                            color = "black";
                            string ck_value = VitalSignList[i]["ITEM_VALUE"];
                            string measure = VitalSignList[i]["VS_REASON"];
                            foreach (DataRow r in dt_check.Rows)
                            {
                                if (measure == "測不到")
                                {
                                    color = "red";
                                }
                                else
                                {
                                    if (r["MODEL_ID"].ToString() == "mpl_" + type || r["MODEL_ID"].ToString() == "mph_" + type)
                                    {
                                        if (r["DECIDE"].ToString() == ">")
                                        {
                                            if (double.Parse(ck_value) > double.Parse(r["VALUE_LIMIT"].ToString()))
                                            {
                                                color = "red";
                                            }
                                        }
                                        else if (r["DECIDE"].ToString() == "<")
                                        {
                                            if (double.Parse(ck_value) < double.Parse(r["VALUE_LIMIT"].ToString()))
                                            {
                                                color = "blue";
                                            }
                                        }
                                    }
                                }
                            }
                            vsinfo.Add(string.Format("HR：{0}"
                                , (measure == "測不到") ? "<font color='" + color + "'>" + measure + "</font>" : "<font color='" + color + "'>" + VitalSignList[i]["ITEM_VALUE"] + "</font>次/分"
                                ));
                        }
                    }
                    VitalSignList = Temp.FindAll(x => x["VS_ITEM"] == "bf" && (!string.IsNullOrEmpty(x["ITEM_VALUE"]) || x["VS_REASON"] == "測不到"));
                    if (VitalSignList.Count > 0)
                    {
                        for (int i = 0; i < ((VitalSignList.Count < rownum) ? VitalSignList.Count : rownum); i++)
                        {
                            color = "black";
                            string ck_value = VitalSignList[i]["ITEM_VALUE"];
                            string measure = VitalSignList[i]["VS_REASON"];
                            foreach (DataRow r in dt_check.Rows)
                            {
                                if (measure == "測不到")
                                {
                                    color = "red";
                                }
                                else
                                {
                                    if (r["MODEL_ID"].ToString() == "bfl_" + type || r["MODEL_ID"].ToString() == "bfh_" + type)
                                    {
                                        if (r["DECIDE"].ToString() == ">")
                                        {
                                            if (double.Parse(ck_value) > double.Parse(r["VALUE_LIMIT"].ToString()))
                                            {
                                                color = "red";
                                            }
                                        }
                                        else if (r["DECIDE"].ToString() == "<")
                                        {
                                            if (double.Parse(ck_value) < double.Parse(r["VALUE_LIMIT"].ToString()))
                                            {
                                                color = "blue";
                                            }
                                        }
                                    }
                                }
                            }
                            vsinfo.Add(string.Format("RR：{0}"
                                , (measure == "測不到") ? "<font color='" + color + "'>" + measure + "</font>" : "<font color='" + color + "'>" + VitalSignList[i]["ITEM_VALUE"] + "</font>次/分"
                                ));
                        }
                    }
                    VitalSignList = Temp.FindAll(x => x["VS_ITEM"] == "sp" && (!string.IsNullOrEmpty(x["ITEM_VALUE"]) || x["VS_REASON"] == "測不到"));
                    if (VitalSignList.Count > 0)
                    {
                        for (int i = 0; i < ((VitalSignList.Count < rownum) ? VitalSignList.Count : rownum); i++)
                        {
                            color = "black";
                            string ck_value = VitalSignList[i]["ITEM_VALUE"], part = VitalSignList[i]["VS_PART"];
                            string measure = VitalSignList[i]["VS_REASON"];
                            foreach (DataRow r in dt_check.Rows)
                            {
                                if (measure == "測不到")
                                {
                                    color = "red";
                                }
                                else
                                {
                                    if (r["MODEL_ID"].ToString() == "spl" || r["MODEL_ID"].ToString() == "sph")
                                    {
                                        if (r["DECIDE"].ToString() == ">")
                                        {
                                            if (double.Parse(ck_value) > double.Parse(r["VALUE_LIMIT"].ToString()))
                                            {
                                                color = "red";
                                            }
                                        }
                                        else if (r["DECIDE"].ToString() == "<")
                                        {
                                            if (double.Parse(ck_value) < double.Parse(r["VALUE_LIMIT"].ToString()))
                                            {
                                                color = "blue";
                                            }
                                        }
                                    }
                                }
                            }
                            vsinfo.Add(string.Format("SPO2：{0}"
                                , (measure == "測不到") ? "<font color='" + color + "'>" + measure + "</font>" : "<font color='" + color + "'>" + VitalSignList[i]["ITEM_VALUE"] + "</font>%"
                                ));
                        }
                    }
                    VitalSignList = Temp.FindAll(x => x["VS_ITEM"] == "bp" && (!string.IsNullOrEmpty(x["ITEM_VALUE"].Replace("|", "").Replace(" ", "")) || x["VS_REASON"] == "測不到"));
                    if (VitalSignList.Count > 0)
                    {
                        for (int i = 0; i < ((VitalSignList.Count < rownum) ? VitalSignList.Count : rownum); i++)
                        {
                            string color_h = "black";
                            string color_l = "black";
                            string[] ck_value = VitalSignList[i]["ITEM_VALUE"].Split('|');
                            string measure = VitalSignList[i]["VS_REASON"];
                            string part = VitalSignList[i]["VS_PART"];
                            foreach (DataRow r in dt_check.Rows)
                            {
                                if (measure == "測不到")
                                {
                                    color_h = "red";
                                }
                                else
                                {
                                    if (r["MODEL_ID"].ToString() == "bpls_" + type || r["MODEL_ID"].ToString() == "bphs_" + type)
                                    {
                                        if (r["DECIDE"].ToString() == ">")
                                        {
                                            if (double.Parse(ck_value[0]) > double.Parse(r["VALUE_LIMIT"].ToString()))
                                            {
                                                color_h = "red";
                                            }
                                        }
                                        else if (r["DECIDE"].ToString() == "<")
                                        {
                                            if (double.Parse(ck_value[0]) < double.Parse(r["VALUE_LIMIT"].ToString()))
                                            {
                                                color_h = "blue";
                                            }
                                        }
                                    }
                                    if (r["MODEL_ID"].ToString() == "bpld_" + type || r["MODEL_ID"].ToString() == "bphd_" + type)
                                    {
                                        if (r["DECIDE"].ToString() == ">")
                                        {
                                            if (double.Parse(ck_value[1]) > double.Parse(r["VALUE_LIMIT"].ToString()))
                                            {
                                                color_l = "red";
                                            }
                                        }
                                        else if (r["DECIDE"].ToString() == "<")
                                        {
                                            if (double.Parse(ck_value[1]) < double.Parse(r["VALUE_LIMIT"].ToString()))
                                            {
                                                color_l = "blue";
                                            }
                                        }
                                    }
                                }
                            }
                            vsinfo.Add(string.Format("BP：{0}"
                                , (measure == "測不到") ? "<font color = '" + color_h + "'>" + measure + "</font>" : " <font color = '" + color_h + "'>" + ck_value[0] + "</font> / <font color='" + color_l + "'>" + ck_value[1] + "</font> mmHg"
                                ));
                        }
                    }
                    VitalSignList = Temp.FindAll(x => x["VS_ITEM"] == "cv1" && !string.IsNullOrEmpty(x["ITEM_VALUE"]));
                    if (VitalSignList.Count > 0)
                    {
                        for (int i = 0; i < ((VitalSignList.Count < rownum) ? VitalSignList.Count : rownum); i++)
                        {
                            color = "black";
                            string ck_value = VitalSignList[i]["ITEM_VALUE"], part = VitalSignList[i]["VS_PART"];
                            foreach (DataRow r in dt_check.Rows)
                            {
                                if (r["MODEL_ID"].ToString() == "cv1_h" || r["MODEL_ID"].ToString() == "cv1_l")
                                {
                                    if (r["DECIDE"].ToString() == ">")
                                    {
                                        if (double.Parse(ck_value) > double.Parse(r["VALUE_LIMIT"].ToString()))
                                        {
                                            color = "red";
                                        }
                                    }
                                    else if (r["DECIDE"].ToString() == "<")
                                    {
                                        if (double.Parse(ck_value) < double.Parse(r["VALUE_LIMIT"].ToString()))
                                        {
                                            color = "blue";
                                        }
                                    }
                                }
                            }
                            vsinfo.Add(string.Format("CVP：<font color='" + color + "'>{0} </font>mmHg"
                                , VitalSignList[i]["ITEM_VALUE"].Replace("|", "")
                                ));
                        }
                    }
                    VitalSignList = Temp.FindAll(x => x["VS_ITEM"] == "cv2" && !string.IsNullOrEmpty(x["ITEM_VALUE"]));
                    if (VitalSignList.Count > 0)
                    {
                        for (int i = 0; i < ((VitalSignList.Count < rownum) ? VitalSignList.Count : rownum); i++)
                        {
                            color = "black";
                            string ck_value = VitalSignList[i]["ITEM_VALUE"], part = VitalSignList[i]["VS_PART"];
                            foreach (DataRow r in dt_check.Rows)
                            {
                                if (r["MODEL_ID"].ToString() == "cv2_h" || r["MODEL_ID"].ToString() == "cv2_l")
                                {
                                    if (r["DECIDE"].ToString() == ">")
                                    {
                                        if (double.Parse(ck_value) > double.Parse(r["VALUE_LIMIT"].ToString()))
                                        {
                                            color = "red";
                                        }
                                    }
                                    else if (r["DECIDE"].ToString() == "<")
                                    {
                                        if (double.Parse(ck_value) < double.Parse(r["VALUE_LIMIT"].ToString()))
                                        {
                                            color = "blue";
                                        }
                                    }
                                }
                            }
                            vsinfo.Add(string.Format("CVP：<font color='" + color + "'>{0}</font>cmH2O"
                                , VitalSignList[i]["ITEM_VALUE"].Replace("|", "")
                                ));
                        }
                    }
                    VitalSignList = Temp.FindAll(x => x["VS_ITEM"] == "ic1" && !string.IsNullOrEmpty(x["ITEM_VALUE"]));
                    if (VitalSignList.Count > 0)
                    {
                        for (int i = 0; i < ((VitalSignList.Count < rownum) ? VitalSignList.Count : rownum); i++)
                        {
                            color = "black";
                            string ck_value = VitalSignList[i]["ITEM_VALUE"], part = VitalSignList[i]["VS_PART"];
                            foreach (DataRow r in dt_check.Rows)
                            {
                                if (r["MODEL_ID"].ToString() == "ic1_l")
                                {
                                    if (r["DECIDE"].ToString() == ">")
                                    {
                                        if (double.Parse(ck_value) > double.Parse(r["VALUE_LIMIT"].ToString()))
                                        {
                                            color = "red";
                                        }
                                    }
                                    else if (r["DECIDE"].ToString() == "<")
                                    {
                                        if (double.Parse(ck_value) < double.Parse(r["VALUE_LIMIT"].ToString()))
                                        {
                                            color = "blue";
                                        }
                                    }
                                }
                            }
                            vsinfo.Add(string.Format("ICP：<font color='" + color + "'>{0}</font>mmHg"
                            , VitalSignList[i]["ITEM_VALUE"].Replace("|", "")
                            ));
                        }
                    }
                    VitalSignList = Temp.FindAll(x => x["VS_ITEM"] == "ic2" && !string.IsNullOrEmpty(x["ITEM_VALUE"]));
                    if (VitalSignList.Count > 0)
                    {
                        for (int i = 0; i < ((VitalSignList.Count < rownum) ? VitalSignList.Count : rownum); i++)
                        {
                            color = "black";
                            string ck_value = VitalSignList[i]["ITEM_VALUE"], part = VitalSignList[i]["VS_PART"];
                            foreach (DataRow r in dt_check.Rows)
                            {
                                if (r["MODEL_ID"].ToString() == "ic2_l")
                                {
                                    if (r["DECIDE"].ToString() == ">")
                                    {
                                        if (double.Parse(ck_value) > double.Parse(r["VALUE_LIMIT"].ToString()))
                                        {
                                            color = "red";
                                        }
                                    }
                                    else if (r["DECIDE"].ToString() == "<")
                                    {
                                        if (double.Parse(ck_value) < double.Parse(r["VALUE_LIMIT"].ToString()))
                                        {
                                            color = "blue";
                                        }
                                    }
                                }
                            }
                            vsinfo.Add(string.Format("ICP：<font color='" + color + "'>{0}</font>cmH2O"
                        , VitalSignList[i]["ITEM_VALUE"].Replace("|", "")
                        ));
                        }
                    }
                }
                #endregion
                //約束提醒====20140414 by yungchen
                string sql = "select DISTINCT feeno from BINDTABLESAVE  where feeno='" + feeno + "' AND assess <>'1' AND status <> 'del'";
                Dt = link.DBExecSQL(sql);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        if (Dt.Rows[i]["FEENO"].ToString().Trim() == feeno)
                        { vsinfo.Add("尚有約束評估未完成"); }
                        else
                        { continue; }
                    }
                }

                //===約束提醒====20140414 by yungchen
                /*
                 * //輸入輸出
                DataTable dt_i = iom.sel_io_data_byClass(feeno, strDate, "1", "intaketype");
                DataTable dt_o = iom.sel_io_data_byClass(feeno, strDate, "1", "outputtype");
                double i_total = 0, o_total = 0;
                foreach (DataRow r in dt_i.Rows)
                    i_total += double.Parse(r[shift_cate].ToString());
                foreach (DataRow r in dt_o.Rows)
                    o_total += double.Parse(r[shift_cate].ToString());
                if (i_total != 0 && o_total != 0)
                    vsinfo.Add("輸入/輸出：" + i_total.ToString() + "/" + o_total.ToString() + "(" + Math.Round((i_total - o_total), 2).ToString() + ")");
           
                //評估類
                List<List<string>> assess_list = get_TransferDuty_Item_assess(feeno, "");
                for(int i = 0; i < assess_list.Count; i ++)
                    vsinfo.Add(assess_list[i][1] + assess_list[i][2]);
                //傷口
                DataTable dt_wound_data = wound.sel_wound_data("", feeno, "", "");
                foreach (DataRow r in dt_wound_data.Rows)
                {
                    if (r["ENDTIME"].ToString() == "" && r["DELETED"].ToString() == "")
                        vsinfo.Add(r["POSITION"].ToString() + r["TYPE"].ToString());
                }
                  */
                /* 20141009 因執行速度過慢取消以下相關資料
                //會診
                List<Consultation> con_list = get_TransferDuty_Item_Consultation(feeno, "");
                for (int i = 0; i < con_list.Count; i++)
                {
                    if (con_list[i].ConsContent == null || con_list[i].ConsContent == "")
                        vsinfo.Add(con_list[i].OrderDate + con_list[i].ConsDept);
                }
                //檢驗
                List<Exam> exam_list = get_TransferDuty_Item_exam_main(feeno, "Main");
                for (int i = 0; i < exam_list.Count; i++)
                    vsinfo.Add(exam_list[i].ExamDate + exam_list[i].ExamName);
                //檢查
                List<Lab> lab_list = get_TransferDuty_Item_lab_main(feeno, "Main");
                for (int i = 0; i < lab_list.Count; i++)
                    vsinfo.Add(lab_list[i].LabDate + lab_list[i].LabName);
                */
                return vsinfo.ToArray();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return vsinfo.ToArray();
            }
            finally
            {
                this.link.DBClose();
            }
        }

        /// <summary>
        /// 取得待處理工作清單
        /// </summary>
        /// <param name="feeno"></param>
        /// <returns></returns>
        protected internal string[] getWaitInfo(string feeno)
        {
            List<ExecResultList> elist = null;
            getExecResult(feeno, ref elist);

            List<string> vsinfo = new List<string>();
            try
            {
                string sqlstr = " SELECT SET_PRIOD, SET_ACTION, ORDER_CONTENT ,END_DATE ,DELETED ,CANCELTIME ";
                sqlstr += " FROM DATA_SIGNORDER WHERE FEE_NO = '" + feeno + "' AND TRIM(SIGN_USER) IS NOT NULL AND TRIM(SET_USER) IS NOT NULL ";
                sqlstr += " AND (DELETED is NULL OR TO_DATE(TO_CHAR(END_DATE, 'yyyy/MM/dd'),'yyyy/MM/dd') >= TO_DATE(TO_CHAR(SYSDATE, 'yyyy/MM/dd'),'yyyy/MM/dd'))";
                DataTable Dt = link.DBExecSQL(sqlstr);
                if (Dt.Rows.Count > 0)
                {
                    for (int k = 0; k < Dt.Rows.Count; k++)
                    {
                        string[] time_priod = Dt.Rows[k]["SET_PRIOD"].ToString().Trim().Split('|');
                        for (int i = 0; i <= time_priod.Length - 1; i++)
                        {
                            bool checkflag = false;
                            string noteStr = string.Empty;
                            for (int j = 0; j <= elist.Count - 1; j++)
                            {
                                if (elist[j].exec_priod == time_priod[i] &&
                                    elist[j].action == Dt.Rows[k]["SET_ACTION"].ToString().Trim() &&
                                    elist[j].order_content == Dt.Rows[k]["ORDER_CONTENT"].ToString().Trim()

                                    )
                                {
                                    checkflag = true;
                                    break;
                                }
                            }
                            if (!string.IsNullOrEmpty(Dt.Rows[k]["DELETED"].ToString()))//DC醫囑連動工作清單及交班單
                            {
                                string End_Datetime = Dt.Rows[k]["END_DATE"].ToString();
                                string Proid_Datetime = DateTime.Now.ToString("yyyy/MM/dd") + " " + time_priod[i];
                                if (Convert.ToDateTime(End_Datetime) < Convert.ToDateTime(Proid_Datetime))
                                {
                                    checkflag = true;
                                }
                            }
                            if (!string.IsNullOrEmpty(Dt.Rows[k]["CANCELTIME"].ToString()))//取消所有執行連動工作清單及交班單
                            {
                                string End_Datetime = Dt.Rows[k]["CANCELTIME"].ToString();
                                string Proid_Datetime = DateTime.Now.ToString("yyyy/MM/dd") + " " + time_priod[i];
                                if (Convert.ToDateTime(End_Datetime) <= Convert.ToDateTime(Proid_Datetime))
                                {
                                    checkflag = true;
                                }
                            }

                            if (checkflag == false)
                                vsinfo.Add(time_priod[i] + " " + Dt.Rows[k]["SET_ACTION"].ToString().Trim());
                        }
                    }
                }

                /*//管路
                List<List<string>> tube_list = get_TransferDuty_Item_tube(feeno, "");
                for (int i = 0; i < tube_list.Count; i++)
                {
                    if (tube_list[i][5] != "" && Convert.ToDateTime(tube_list[i][5]).Date <= DateTime.Now.Date)
                        vsinfo.Add(tube_list[i][0] + tube_list[i][5].Replace("/", "") + "到期");
                }
                vsinfo.Sort();
                 * */

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

            return vsinfo.ToArray();
        }
        /// <summary>
        /// 取得生命徵象異常值範圍
        /// </summary>
        /// <returns></returns>
        private DataTable Get_Check_Abnormal_dt()
        {
            DataTable dt = new DataTable();
            link.DBExecSQL("SELECT * FROM NIS_SYS_VITALSIGN_OPTION ", ref dt);
            return dt;
        }

        /// <summary>
        /// 取得特殊交班事項
        /// </summary>
        /// <param name="feeno">住院序號</param>
        /// <returns></returns>
        protected internal void getSpecialInfo(string feeno, ref string remark, ref string spinfo, ref string remarkExtra)
        {
            Function func_m = new Function();
            try
            {
                //link = new DBConnector();

                string sqlstr = " SELECT TRANS_MEMO, TRANS_REMARK FROM DATA_TRANS_MEMO WHERE FEE_NO = '" + feeno + "' AND TRANS_FLAG='M'";
                DataTable Dt = link.DBExecSQL(sqlstr);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        spinfo = Dt.Rows[i]["TRANS_MEMO"].ToString().Trim();
                        string remarkPro = Dt.Rows[i]["TRANS_REMARK"].ToString().Trim();
                        remarkPro = Regex.Replace(remarkPro, @"[\r]", "");
                        remarkPro = Regex.Replace(remarkPro, @"[\n]", ",");
                        remark = remarkPro;
                    }
                }
                //remark換行 Ryan 20240118
                //if (!string.IsNullOrEmpty(remark))
                //{
                //    remark = remark.Replace(",", "\n");
                //}

                string nosSpeech = "不能言語";
                bool hasReamark = remark.Contains(nosSpeech);
                //remark新增內容(成人入院評估溝通方式為不能言語時增加,抓修改日期最新那筆) Ryan 20240118
                string admission_sql = "WITH Master AS( \n";
                admission_sql += "SELECT TABLEID FROM( \n";
                admission_sql += "SELECT AM.TABLEID,ROW_NUMBER() OVER (ORDER BY MODIFYTIME DESC) SN \n";
                admission_sql += "FROM ASSESSMENTMASTER AM \n";
                admission_sql += "WHERE FEENO = '" + feeno + "' \n";
                admission_sql += "AND STATUS != 'delete' \n";
                admission_sql += ") \n";
                admission_sql += "WHERE SN = 1 \n";
                admission_sql += ") \n";
                admission_sql += "SELECT * FROM ASSESSMENTDETAIL WHERE TABLEID IN (SELECT TABLEID FROM Master) \n";
                admission_sql += "AND ITEMID IN ('param_lang_no','param_lang_other_no')";
                Dt = link.DBExecSQL(admission_sql);
                if (Dt.Rows.Count > 0)
                {
                    string lang_no = "";
                    string lang_no_other = "";
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        if (Dt.Rows[i]["ITEMID"].ToString() == "param_lang_no")
                        {
                            if (!string.IsNullOrEmpty(Dt.Rows[i]["ITEMVALUE"].ToString()))
                            {
                                lang_no = Dt.Rows[i]["ITEMVALUE"].ToString();
                            }
                        }
                        if (Dt.Rows[i]["ITEMID"].ToString() == "param_lang_other_no")
                        {
                            if (!string.IsNullOrEmpty(Dt.Rows[i]["ITEMVALUE"].ToString()))
                            {
                                lang_no_other = Dt.Rows[i]["ITEMVALUE"].ToString();
                            }
                        }
                    }
                    lang_no = lang_no.Replace("其他", lang_no_other);
                    remarkExtra = ",不能言語:" + lang_no;

                    if (!hasReamark)
                     {

                        remark += ",不能言語:" + lang_no;
                    }
                }
                //string FRIDs = (func_m.sel_FRIDs(ptinfo.FeeNo.Trim()) == true) ? "藥" : "";
                //if (FRIDs != "")
                //{
                //    remarkExtra = ",易跌藥品";
                //    remark += ",易跌藥品";
                //}
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }
        }

        //get Server IP
        public string GetIPAddress()
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];

            return ipAddress.ToString();
        }



        /// <summary>
        /// 取得簽收設定紀錄
        /// </summary>
        /// <param name="fee_no"></param>
        /// <param name="olist"></param>
        private void getSetting(string fee_no, ref List<OrderList> olist)
        {
            try
            {
                //link = new DBConnector();

                int num = 0;
                olist = new List<OrderList>();


                string sqlstr = "select /*+index(data_signorder data_signorder_idx1)*/ ";
                sqlstr += " FEE_NO,SHEET_NO,ORDER_TYPE,PATH_CODE,CIR_CODE,TO_CHAR(START_DATE, 'YYYY/MM/DD') START_DATE,";
                sqlstr += "END_DATE,TO_NCHAR(ORDER_CONTENT) ORDER_CONTENT,SIGN_TIME,SIGN_USER,SIGN_USER_NAME,SIGN_TYPE,SET_USER,SET_PRIOD,SET_ACTION,DELETED,CANCELTIME ";
                sqlstr += " from data_signorder";
                sqlstr += " where fee_no = '" + fee_no + "' and trim(sign_user) is not null and trim(set_user) is not null ";

                sqlstr += "UNION SELECT C.FEE_NO,C.SHEET_NO,'' ORDER_TYPE,'' PATH_CODE,'' CIR_CODE,TO_CHAR( C.EXEC_TIME, 'YYYY/MM/DD' ) START_DATE";

                //sqlstr += ",EXEC_TIME END_DATE,C.ORDER_CONTENT,C.EXEC_TIME SIGN_TIME,C.EXEC_USER SIGN_USER,N'' SIGN_USER_NAME,'T' SIGN_TYPE";
                sqlstr += ",EXEC_TIME END_DATE, TO_NCHAR(C.ORDER_CONTENT),C.EXEC_TIME SIGN_TIME,C.EXEC_USER SIGN_USER,N'' SIGN_USER_NAME,'T' SIGN_TYPE";
                sqlstr += ",C.EXEC_USER SET_USER,C.EXEC_PRIOD SET_PRIOD,C.SET_ACTION,'' DELETED,null CANCELTIME FROM DATA_TASK_EXEC_RECORD C";
                sqlstr += " WHERE /*C.EXEC_RESULT = 'D' AND*/ C.FEE_NO||C.SHEET_NO||C.EXEC_PRIOD||SET_ACTION||TO_CHAR(EXEC_TIME,'YYYYMMDDHH24mmss') IN";
                sqlstr += "(SELECT AA.PK_KEY||AA.TMP_COLUMN FROM (SELECT DISTINCT FEE_NO||SHEET_NO||EXEC_PRIOD||SET_ACTION AS PK_KEY, MAX(TO_CHAR(EXEC_TIME,'YYYYMMDDHH24mmss')) ";
                sqlstr += " AS TMP_COLUMN FROM DATA_TASK_EXEC_RECORD WHERE /*EXEC_RESULT = 'D' AND*/  TO_CHAR( EXEC_TIME, 'YYYYMMDD' ) =TO_CHAR( SYSDATE, 'YYYYMMDD' ) ";
                sqlstr += " AND fee_no = '" + fee_no + "' AND DELETED is null GROUP BY FEE_NO||SHEET_NO||EXEC_PRIOD||SET_ACTION) AA )";
                //sqlstr += "ORDER BY FEE_NO||SHEET_NO ,SIGN_TIME DESC";
                sqlstr += " UNION SELECT DISTINCT B.FEE_NO,'UD' SHEET_NO,'' ORDER_TYPE,'' PATH_CODE, '' CIR_CODE, TO_CHAR(TO_DATE( B.DRUG_DATE,'yyyy-mm-dd AM hh:mi:ss'),'yyyy/mm/dd') START_DATE, ";

                //sqlstr += " SYSDATE END_DATE, '' ORDER_CONTENT, SYSDATE SIGN_tIME, '' SIGN_USER, N'' SIGN_USER_NAME, '' SIGN_TYPE, ";
                sqlstr += " SYSDATE END_DATE, N'' ORDER_CONTENT, SYSDATE SIGN_TIME, '' SIGN_USER, N'' SIGN_USER_NAME, '' SIGN_TYPE, ";
                sqlstr += " '' SET_USER, TO_CHAR(TO_DATE( B.DRUG_DATE,'yyyy-mm-dd AM hh:mi:ss'),'HH24:MI') SET_PRIOD, N'常規給藥' SET_ACTION, '' DELETED, null CANCELTIME ";
                sqlstr += "  FROM DRUG_EXECUTE B WHERE B.FEE_NO = '" + fee_no + "' AND EXEC_DATE IS NULL AND INVALID_DATE IS NULL";
                sqlstr += " AND TO_CHAR(TO_DATE( B.DRUG_DATE,'yyyy-mm-dd AM hh:mi:ss'),'YYYY/MM/DD') = TO_CHAR(SYSDATE,'YYYY/MM/DD')";

                DataTable Dt = link.DBExecSQL(sqlstr);

                #region 抓是否有新未簽醫囑
                string JsonStr = "";
                List<TextOrder> TextList = null;
                byte[] EmployeeByteCode = webService.GetTextOrder(fee_no);
                if (EmployeeByteCode != null)
                {
                    JsonStr = CompressTool.DecompressString(EmployeeByteCode);
                    TextList = JsonConvert.DeserializeObject<List<TextOrder>>(JsonStr);
                }
                #endregion
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        TextOrder TextOrder = TextList.Find(x => x.SheetNo == "D" + Dt.Rows[i]["sheet_no"].ToString().Trim());
                        num++;
                        DateTime dc_dateline = Convert.ToDateTime(Dt.Rows[i]["END_DATE"]);
                        if (TextOrder != null)
                        {
                            if (TextOrder.OrderEndDate.ToString("mm:ss") == "00:00")
                            {
                                dc_dateline = Convert.ToDateTime(TextOrder.OrderEndDate.ToString("yyyy/MM/dd HH:00:00"));

                            }
                            else
                            {
                                dc_dateline = Convert.ToDateTime(TextOrder.OrderEndDate.ToString("yyyy/MM/dd HH:00:00")).AddHours(1);
                            }
                        }

                        DateTime? cancel = null;
                        if (!string.IsNullOrEmpty(Dt.Rows[i]["CANCELTIME"].ToString().Trim()))
                        {
                            try
                            {
                                cancel = DateTime.Parse(Dt.Rows[i]["CANCELTIME"].ToString().Trim());
                            }
                            catch (Exception ex)
                            {
                                log.saveLogMsg(ex.Message, "TransferDuty");
                            }
                        }
                        else
                        {
                            cancel = null;
                        }

                        olist.Add(new OrderList()
                        {
                            sheet_no = Dt.Rows[i]["sheet_no"].ToString().Trim(),
                            order_type = Dt.Rows[i]["ORDER_TYPE"].ToString().Trim(),
                            Dc_date = dc_dateline,
                            order_content = Dt.Rows[i]["ORDER_CONTENT"].ToString().Trim(),
                            set_priod = Dt.Rows[i]["set_priod"].ToString().Split('|'),
                            set_action = Dt.Rows[i]["set_action"].ToString().Trim(),
                            DELETED = Dt.Rows[i]["DELETED"].ToString().Trim(),
                            del_username = GetEmpName(Dt.Rows[i]["DELETED"].ToString().Trim()),
                            canceltime = cancel
                        });
                    }
                    olist = olist.FindAll(x => x.Dc_date.ToString("yyyy/MM/dd") == DateTime.Now.ToString("yyyy/MM/dd") || string.IsNullOrEmpty(x.DELETED)).ToList();
                }
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)      
                string tmp_controller = baseC.ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = baseC.ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, baseC.userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }
        }

        /// <summary>
        /// 取得當日已執行的紀錄
        /// </summary>
        /// <param name="fee_no"></param>
        /// <param name="elist"></param>
        public void getExecResult(string fee_no, ref List<ExecResultList> elist)
        {
            try
            {
                elist = new List<ExecResultList>();
                string sqlstr = " SELECT /*+index(DATA_TASK_EXEC_RECORD DATA_TASK_EXEC_RECORD_IDX1)*/ * FROM DATA_TASK_EXEC_RECORD ";
                sqlstr += " WHERE FEE_NO = '" + fee_no.ToString().Trim() + "' AND EXEC_TIME > TO_DATE(TO_CHAR(SYSDATE, 'yyyy/MM/dd'),'yyyy/MM/dd') ";
                sqlstr += " AND DELETED is null";
                DataTable Dt = link.DBExecSQL(sqlstr);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        elist.Add(new ExecResultList()
                        {
                            sheet_no = Dt.Rows[i]["sheet_no"].ToString().Trim(),
                            order_content = Dt.Rows[i]["order_content"].ToString().Trim(),
                            action = Dt.Rows[i]["set_action"].ToString().Trim(),
                            fee_no = Dt.Rows[i]["fee_no"].ToString().Trim(),
                            exec_priod = Dt.Rows[i]["exec_priod"].ToString().Trim(),
                            exec_name = GetEmpName(Dt.Rows[i]["exec_user"].ToString().Trim()),
                            exec_result = Dt.Rows[i]["exec_result"].ToString().Trim(),
                            exec_reason = Dt.Rows[i]["exec_reason"].ToString().Trim(),
                            exec_time = DateTime.Parse(Dt.Rows[i]["exec_time"].ToString().Trim()),
                        });
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
            finally
            {
                this.link.DBClose();
            }
        }

        //更改PatientInfo Session 
        public ActionResult WebS_ChangPtInfo()
        {
            string str = Request["strFeeNo"];
            if (Request["strFeeNo"] != null)
            {
                byte[] ptinfoByteCode = webService.GetPatientInfo(Request["strFeeNo"]);
                if (ptinfoByteCode != null)
                {
                    string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                    PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                    Session["PatInfo"] = pi;
                }
                else
                    Response.Write("Error");
            }
            return new EmptyResult();
        }
        #endregion

        #region 交班
        /// <summary>
        /// 交班單
        /// </summary>
        /// <returns></returns>
        public ActionResult TransferDuty_List()
        {
            string sqlstr = string.Empty, shift_cate = string.Empty, successorNoTemp = string.Empty, successorName = string.Empty;
            try
            {
                Function func_m = new Function();
                List<TransList> tdList = new List<TransList>();
                int count = 0;
                string tmpshift = string.Empty, tmpcate = string.Empty, checkflag = "";
                int strtime = int.Parse(DateTime.Now.ToString("HHmm"));
                if (strtime >= 0 && strtime <= 759)
                    checkflag = "N";
                else if (strtime >= 800 && strtime <= 1559)
                    checkflag = "D";
                else if (strtime >= 1600 && strtime <= 2359)
                    checkflag = "E";
                bool Category_NA = (userinfo.Category == "NA") ? true : false;

                sqlstr += " SELECT BED_NO, COST_CODE,  {0},{1}";
                //sqlstr += ", (CASE WHEN TO_CHAR(SYSDATE,'hh24mi') BETWEEN '0800' AND '1559' THEN 'D' ";
                //sqlstr += " WHEN TO_CHAR(SYSDATE,'hh24mi') BETWEEN '1600' AND '2359' THEN 'E' ";
                //sqlstr += " WHEN TO_CHAR(SYSDATE,'hh24mi') BETWEEN '0000' AND '0759' THEN 'N' ELSE SHIFT_CATE END ) SHIFT_CATE2";
                sqlstr += " FROM {2} WHERE {3} in('" + userinfo.EmployeesNo + "')";
                if (checkflag == "N")
                {
                    //sqlstr += " AND TO_CHAR(SHIFT_DATE, 'yyyy/MM/dd') BETWEEN TO_CHAR(SYSDATE-1,'yyyy/MM/dd') AND TO_CHAR(SYSDATE,'yyyy/MM/dd')"; 
                    sqlstr += " AND {4} = (SELECT MAX({4}) FROM {2} WHERE {3} = '" + userinfo.EmployeesNo + "')";
                }
                else
                {
                    sqlstr += " AND {5} = TO_CHAR(SYSDATE,'yyyy/MM/dd')";
                }
                sqlstr += " GROUP BY {6}, COST_CODE ORDER BY SHIFT_CATE,BED_NO";
                if (Category_NA)
                {
                    sqlstr = string.Format(sqlstr, "'A' SHIFT_CATE", "'' SUCCESSOR", "DATA_FAVPAT", "EMPLOYE_NO", "DATEITEM", "DATEITEM", "BED_NO");
                }
                else
                {
                    sqlstr = string.Format(sqlstr, "SHIFT_CATE", "SUCCESSOR", "DATA_DISPATCHING", "RESPONSIBLE_USER", "SHIFT_DATE", "TO_CHAR(SHIFT_DATE, 'yyyy/MM/dd')", "BED_NO,SHIFT_CATE,SUCCESSOR");
                }
                List<SelectListItem> transcate_list = new List<SelectListItem>();
                List<string> shiftcate = new List<string>() { "D", "E", "N" };

                DataTable Dt = link.DBExecSQL(sqlstr);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        #region --取接班者姓名區塊--
                        if (count == 0)
                        {
                            successorNoTemp = Dt.Rows[i]["SUCCESSOR"].ToString().Trim();//放一開始第一筆的暫存
                            successorName = (successorNoTemp != "") ? GetEmpName(successorNoTemp) : "";//第一次取到的名子
                        }
                        else
                        {//第二次迴圈開始
                            if (Dt.Rows[i]["SUCCESSOR"].ToString().Trim() != successorNoTemp)
                            {
                                successorNoTemp = Dt.Rows[i]["SUCCESSOR"].ToString().Trim();
                                successorName = (successorNoTemp != "") ? GetEmpName(successorNoTemp) : "";
                            }
                        }
                        #endregion --取接班者姓名區塊--end
                        shift_cate = Dt.Rows[i]["SHIFT_CATE"].ToString().Trim();

                        if (tmpshift != shift_cate && shift_cate != "A")
                        {
                            switch (shift_cate)
                            {
                                case "D":
                                    tmpcate = "早班";
                                    break;
                                case "E":
                                    tmpcate = "小夜";
                                    break;
                                case "N":
                                    tmpcate = "大夜";
                                    break;
                            }
                            tmpshift = shift_cate;
                            transcate_list.Add(new SelectListItem { Text = tmpcate, Value = tmpshift });

                        }
                 	   //20240329 更換WS BedNoTransformFeeNoWithCostCode
                        byte[] ptinfobyte = webService.BedNoTransformFeeNoWithCostCode(Dt.Rows[i]["bed_no"].ToString().Trim(), Dt.Rows[i]["COST_CODE"].ToString().Trim());

                        if (ptinfobyte != null)
                        {
                            string ptinfojarr = CompressTool.DecompressString(ptinfobyte);
                            PatientInfo[] patinfo = JsonConvert.DeserializeObject<PatientInfo[]>(ptinfojarr);

                            byte[] ptdatebyte = webService.GetPatientInfo(patinfo[0].FeeNo.ToString().Trim());
                            if (ptdatebyte != null)
                            {
                                string ptdatejarr = CompressTool.DecompressString(ptdatebyte);
                                PatientInfo patdate = JsonConvert.DeserializeObject<PatientInfo>(ptdatejarr);
                                patinfo[0].InDate = patdate.InDate;
                                patinfo[0].OutDate = patdate.OutDate;
                                patinfo[0].Expected_OutDate = patdate.Expected_OutDate;
                                patinfo[0].duty_code = patdate.duty_code;
                            }

                            //////2015.05.20 修改增加主治醫生 病人相關資料
                            ////byte[] ptinfoByteCode = webService.GetPatientInfo(patinfo[0].FeeNo);
                            ////if (ptinfoByteCode != null)
                            ////{
                            ////    string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                            ////    PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                            ////    patinfo[0] = pi;

                            ////    //下列為特殊住記
                            ////    //List<PatientInfo> allergy_list = new List<PatientInfo>();
                            ////    //byte[] allergyfoByteCode = webService.GetAllergyList(ptinfo.FeeNo.Trim());
                            ////    //if (allergyfoByteCode != null)
                            ////    //{
                            ////    //    string allergyJosnArr = CompressTool.DecompressString(allergyfoByteCode);
                            ////    //    List<PatientInfo> allergy = JsonConvert.DeserializeObject<List<PatientInfo>>(allergyJosnArr);
                            ////    //    allergy_list.AddRange(allergy);
                            ////    //}
                            ////    //patinfo[0].AllergyList = allergy_list.ToString();
                            ////}
                            //////--20150520

                            string speical_info = string.Empty, remark_info = string.Empty;
                            // 取得VitalSign資料
                            string[] vital_info = null;//多一個接班者姓名參數傳入
                            string[] wait_info = getWaitInfo(patinfo[0].FeeNo);
                            string remarkExtra = "";
                            
                            getSpecialInfo(patinfo[0].FeeNo, ref remark_info, ref speical_info, ref remarkExtra);
                            string FRIDs = (func_m.sel_FRIDs(patinfo[0].FeeNo.Trim()) == true) ? "藥" : "";


                            patinfo[0].BedNo = Dt.Rows[i]["bed_no"].ToString().Trim();
                            string feeno = patinfo[0].FeeNo.ToString();
                            patinfo[0].FeeNo += shift_cate;
                            if (Category_NA)
                            {
                                //不轉字串會造成迴圈參考影響到前面資料  造成全部資料都變成N(大夜型態)
                                foreach (var item in shiftcate)
                                {
                                    vital_info = getTranInfo(feeno, item, successorName);//多一個接班者姓名參數傳入
                                    tdList.Add(new TransList(patinfo[0], vital_info, wait_info, remark_info, speical_info, item, remarkExtra, FRIDs));
                                }
                            }
                            else
                            {
                                vital_info = getTranInfo(feeno, shift_cate, successorName);//多一個接班者姓名參數傳入
                                tdList.Add(new TransList(patinfo[0], vital_info, wait_info, remark_info, speical_info, shift_cate, remarkExtra, FRIDs));
                            }
                            //tdList.Add(new TransList(patinfo[0], vital_info, wait_info, remark_info, speical_info, shift_cate));
                            count++;//計算資料筆數
                        }
                    }
                }
                if (Category_NA)
                {
                    foreach (var item in shiftcate)
                    {

                        if (tmpshift != item)
                        {
                            switch (item)
                            {
                                case "D":
                                    tmpcate = "早班";
                                    break;
                                case "E":
                                    tmpcate = "小夜";
                                    break;
                                case "N":
                                    tmpcate = "大夜";
                                    break;
                            }
                            tmpshift = item;
                            transcate_list.Add(new SelectListItem { Text = tmpcate, Value = tmpshift });

                        }
                    }
                }
                switch (shift_cate)
                {
                    case "D":
                        ViewData["transcate"] = "早班";
                        break;
                    case "E":
                        ViewData["transcate"] = "小夜";
                        break;
                    case "N":
                        ViewData["transcate"] = "大夜";
                        break;
                    default:
                        ViewData["transcate"] = "";
                        break;
                }
                ViewBag.UserNo = userinfo.EmployeesNo;
                ViewBag.UserPwd = userinfo.Pwd;
                ViewBag.Transfer = ConfigurationManager.AppSettings["Transfer"].ToString();
                ViewData["tdList"] = tdList;
                ViewData["transuser"] = userinfo.EmployeesName;
                ViewBag.transcate_list = transcate_list;
                ViewBag.dt_TC = new Func<string, string, DataTable>(Sel_TeamCare);
                return View();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), "DBExecSQL", sqlstr, ex);
                return View();
            }
            finally
            {
                this.link.DBClose();
            }
        }
        public ActionResult TransferDuty_Manager(string paramsdata = "")
        {
            try
            {
                #region 設定使用者預設護理站
                byte[] listByteCode = webService.GetCostCenterList();
                string listJsonArray = CompressTool.DecompressString(listByteCode);
                List<CostCenterList> costlist = JsonConvert.DeserializeObject<List<CostCenterList>>(listJsonArray);
                List<SelectListItem> cCostList = new List<SelectListItem>();
                //第三順位_否則使用者歸屬單位
                string set_cost = paramsdata;
                //第一順位_使用者有選擇過
                if (Request["cost_code"] != null)
                    set_cost = Request["cost_code"];
                cCostList.Add(new SelectListItem()
                {
                    Text = "我的病人",
                    Value = "mylist",
                    Selected = true
                });
                for (int i = 0; i < costlist.Count; i++)
                {
                    bool select = false;
                    if (set_cost == costlist[i].CostCenterCode.Trim())
                        select = true;
                    cCostList.Add(new SelectListItem()
                    {
                        Text = costlist[i].CCCDescription.Trim(),
                        Value = costlist[i].CostCenterCode.Trim(),
                        Selected = select
                    });
                }

                ViewData["costlist"] = cCostList;
                #endregion
                List<TransList> tdList = new List<TransList>();
                Function func_m = new Function();

                string sqlstr = string.Empty, shift_cate = string.Empty, successorNoTemp = string.Empty, successorName = string.Empty;
                int count = 0;
                string tmpshift = string.Empty, tmpcate = string.Empty, checkflag = "";
                int strtime = int.Parse(DateTime.Now.ToString("HHmm"));
                if (strtime >= 0 && strtime <= 759)
                    checkflag = "N";
                else if (strtime >= 800 && strtime <= 1559)
                    checkflag = "D";
                else if (strtime >= 1600 && strtime <= 2359)
                    checkflag = "E";

                sqlstr += " SELECT BED_NO, COST_CODE, SHIFT_CATE,SUCCESSOR";
                //sqlstr += ", (CASE WHEN TO_CHAR(SYSDATE,'hh24mi') BETWEEN '0800' AND '1559' THEN 'D' ";
                //sqlstr += " WHEN TO_CHAR(SYSDATE,'hh24mi') BETWEEN '1600' AND '2359' THEN 'E' ";
                //sqlstr += " WHEN TO_CHAR(SYSDATE,'hh24mi') BETWEEN '0000' AND '0759' THEN 'N' ELSE SHIFT_CATE END ) SHIFT_CATE2";
                if (paramsdata == "mylist" || string.IsNullOrEmpty(paramsdata) || paramsdata == "undefined")
                {
                    sqlstr += " FROM DATA_DISPATCHING WHERE RESPONSIBLE_USER in('" + userinfo.EmployeesNo + "')";
                    if (checkflag == "N")
                    {
                        sqlstr += " AND SHIFT_DATE = (SELECT MAX(SHIFT_DATE) FROM DATA_DISPATCHING WHERE RESPONSIBLE_USER = '" + userinfo.EmployeesNo + "')";
                    }
                    else
                    {
                        sqlstr += " AND TO_CHAR(SHIFT_DATE, 'yyyy/MM/dd') = TO_CHAR(SYSDATE,'yyyy/MM/dd')";
                    }
                }
                else
                {
                    sqlstr += " FROM DATA_DISPATCHING WHERE cost_code = '" + paramsdata + "'";
                    sqlstr += " AND TO_CHAR(SHIFT_DATE, 'yyyy/MM/dd') = TO_CHAR(SYSDATE,'yyyy/MM/dd')";
                }

                sqlstr += " GROUP BY BED_NO,SHIFT_CATE,SUCCESSOR, COST_CODE ORDER BY SHIFT_CATE,BED_NO";
                #region 不綁派班選擇單位病人---待未來有更好的寫法
                List<SelectListItem> transcate_list = new List<SelectListItem>();
                DataTable Dt = link.DBExecSQL(sqlstr);
                if ((!(Dt.Rows.Count > 0)) && (paramsdata != "mylist" && !string.IsNullOrEmpty(paramsdata)))
                {
                    return new EmptyResult();
                }
                //else
                //{
                //    Dt = link.DBExecSQL(sqlstr);
                //    //DataTable dataTable = new DataTable();
                //    //dataTable.Load(reader);
                //}
                #endregion

                for (int i = 0; i < Dt.Rows.Count; i++)
                {
                    #region --取接班者姓名區塊--
                    if (count == 0)
                    {
                        successorNoTemp = Dt.Rows[i]["SUCCESSOR"].ToString().Trim();//放一開始第一筆的暫存
                        successorName = (successorNoTemp != "") ? GetEmpName(successorNoTemp) : "";//第一次取到的名子
                    }
                    else
                    {//第二次迴圈開始
                        if (Dt.Rows[i]["SUCCESSOR"].ToString().Trim() != successorNoTemp)
                        {
                            successorNoTemp = Dt.Rows[i]["SUCCESSOR"].ToString().Trim();
                            successorName = (successorNoTemp != "") ? GetEmpName(successorNoTemp) : "";
                        }
                    }
                    #endregion --取接班者姓名區塊--end
                    if (tmpshift != Dt.Rows[i]["SHIFT_CATE"].ToString().Trim())
                    {
                        switch (Dt.Rows[i]["SHIFT_CATE"].ToString().Trim())
                        {
                            case "D":
                                tmpcate = "早班";
                                break;
                            case "E":
                                tmpcate = "小夜";
                                break;
                            case "N":
                                tmpcate = "大夜";
                                break;
                        }
                        tmpshift = Dt.Rows[i]["SHIFT_CATE"].ToString().Trim();
                        transcate_list.Add(new SelectListItem { Text = tmpcate, Value = tmpshift });
                    }
                    shift_cate = Dt.Rows[i]["SHIFT_CATE"].ToString().Trim();
                    //20240329 更換WS BedNoTransformFeeNoWithCostCode
                    byte[] ptinfobyte = webService.BedNoTransformFeeNoWithCostCode(Dt.Rows[i]["bed_no"].ToString().Trim(), Dt.Rows[i]["COST_CODE"].ToString().Trim());
                    if (ptinfobyte != null)
                    {
                        string ptinfojarr = CompressTool.DecompressString(ptinfobyte);
                        PatientInfo[] patinfo = JsonConvert.DeserializeObject<PatientInfo[]>(ptinfojarr);
                        PatientInfo patdate = null;
                        byte[] ptdatebyte = webService.GetPatientInfo(patinfo[0].FeeNo.ToString().Trim());
                        if (ptdatebyte != null)
                        {
                            string ptdatejarr = CompressTool.DecompressString(ptdatebyte);
                            patdate = JsonConvert.DeserializeObject<PatientInfo>(ptdatejarr);
                            patinfo[0].InDate = patdate.InDate;
                            patinfo[0].OutDate = patdate.OutDate;
                            patinfo[0].Expected_OutDate = patdate.Expected_OutDate;
                            patinfo[0].duty_code = patdate.duty_code;
                        }
                        // 取得VitalSign資料
                        string[] vital_info = getTranInfo(patinfo[0].FeeNo, shift_cate, successorName, patdate);//多一個接班者姓名參數傳入
                        string[] wait_info = getWaitInfo(patinfo[0].FeeNo);
                        string speical_info = string.Empty, remark_info = string.Empty;
                        string remarkExtra = "";

                        getSpecialInfo(patinfo[0].FeeNo, ref remark_info, ref speical_info, ref remarkExtra);
                        string FRIDs = (func_m.sel_FRIDs(patinfo[0].FeeNo.Trim()) == true) ? "藥" : "";

                        patinfo[0].BedNo = Dt.Rows[i]["bed_no"].ToString().Trim();
                        patinfo[0].FeeNo += shift_cate;
                        tdList.Add(new TransList(patinfo[0], vital_info, wait_info, remark_info, speical_info, shift_cate, remarkExtra, FRIDs));
                        count++;//計算資料筆數
                    }
                }
                switch (shift_cate)
                {
                    case "D":
                        ViewData["transcate"] = "早班";
                        break;
                    case "E":
                        ViewData["transcate"] = "小夜";
                        break;
                    case "N":
                        ViewData["transcate"] = "大夜";
                        break;
                    default:
                        ViewData["transcate"] = "";
                        break;
                }
                ViewBag.UserNo = userinfo.EmployeesNo;
                ViewBag.UserPwd = userinfo.Pwd;
                ViewBag.Transfer = ConfigurationManager.AppSettings["Transfer"].ToString();
                ViewData["tdList"] = tdList;
                ViewData["transuser"] = userinfo.EmployeesName;
                ViewBag.transcate_list = transcate_list;
                ViewBag.dt_TC = new Func<string, string, DataTable>(Sel_TeamCare);

                return View();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return View();
            }
            finally
            {
                this.link.DBClose();
            }
        }
        public ActionResult TransferDuty_List_cost()
        {
            try
            {
                List<TransList> tdList = new List<TransList>();
                Function func_m = new Function();

                //IDataReader reader = null;
                string sqlstr = string.Empty, shift_cate = string.Empty;
                string tmpshift = string.Empty, tmpcate = string.Empty, checkflag = "";
                int strtime = int.Parse(DateTime.Now.ToString("HHmm"));
                if (strtime >= 0 && strtime <= 759)
                    checkflag = "N";
                else if (strtime >= 800 && strtime <= 1559)
                    checkflag = "D";
                else if (strtime >= 1600 && strtime <= 2359)
                    checkflag = "E";

                sqlstr += " SELECT BED_NO, COST_CODE, SHIFT_CATE";
                //sqlstr += ", (CASE WHEN TO_CHAR(SYSDATE,'hh24mi') BETWEEN '0800' AND '1559' THEN 'D' ";
                //sqlstr += " WHEN TO_CHAR(SYSDATE,'hh24mi') BETWEEN '1600' AND '2359' THEN 'E' ";
                //sqlstr += " WHEN TO_CHAR(SYSDATE,'hh24mi') BETWEEN '0000' AND '0759' THEN 'N' ELSE SHIFT_CATE END ) SHIFT_CATE2";
                sqlstr += " FROM DATA_DISPATCHING WHERE cost_code = '" + userinfo.CostCenterCode + "'";
                if (checkflag == "N")
                {
                    //sqlstr += " AND TO_CHAR(SHIFT_DATE, 'yyyy/MM/dd') BETWEEN TO_CHAR(SYSDATE-1,'yyyy/MM/dd') AND TO_CHAR(SYSDATE,'yyyy/MM/dd')"; 
                    sqlstr += " AND SHIFT_DATE = (SELECT MAX(SHIFT_DATE) FROM DATA_DISPATCHING WHERE cost_code = '" + userinfo.CostCenterCode + "')";
                }
                else
                {
                    sqlstr += " AND TO_CHAR(SHIFT_DATE, 'yyyy/MM/dd') = TO_CHAR(SYSDATE,'yyyy/MM/dd')";
                }
                sqlstr += " GROUP BY BED_NO, SHIFT_CATE, COST_CODE ORDER BY SHIFT_CATE,BED_NO";

                List<SelectListItem> transcate_list = new List<SelectListItem>();
                DataTable Dt = link.DBExecSQL(sqlstr);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        if (tmpshift != Dt.Rows[i]["SHIFT_CATE"].ToString().Trim())
                        {
                            switch (Dt.Rows[i]["SHIFT_CATE"].ToString().Trim())
                            {
                                case "D":
                                    tmpcate = "早班";
                                    break;
                                case "E":
                                    tmpcate = "小夜";
                                    break;
                                case "N":
                                    tmpcate = "大夜";
                                    break;
                            }
                            tmpshift = Dt.Rows[i]["SHIFT_CATE"].ToString().Trim();
                            transcate_list.Add(new SelectListItem { Text = tmpcate, Value = tmpshift });
                        }
                        shift_cate = Dt.Rows[i]["SHIFT_CATE"].ToString().Trim();
              		      //20240329 更換WS BedNoTransformFeeNoWithCostCode
                        byte[] ptinfobyte = webService.BedNoTransformFeeNoWithCostCode(Dt.Rows[i]["bed_no"].ToString().Trim(), Dt.Rows[i]["COST_CODE"].ToString().Trim());
                        if (ptinfobyte != null)
                        {
                            string ptinfojarr = CompressTool.DecompressString(ptinfobyte);
                            PatientInfo[] patinfo = JsonConvert.DeserializeObject<PatientInfo[]>(ptinfojarr);
                            // 取得VitalSign資料
                            string[] vital_info = getTranInfo(patinfo[0].FeeNo, shift_cate);
                            string[] wait_info = getWaitInfo(patinfo[0].FeeNo);
                            string speical_info = string.Empty, remark_info = string.Empty;
                            string remarkExtra = "";

                            getSpecialInfo(patinfo[0].FeeNo, ref remark_info, ref speical_info, ref remarkExtra);
                            string FRIDs = (func_m.sel_FRIDs(patinfo[0].FeeNo.Trim()) == true) ? "藥" : "";

                            patinfo[0].BedNo = Dt.Rows[i]["bed_no"].ToString().Trim();
                            patinfo[0].FeeNo += shift_cate;
                            tdList.Add(new TransList(patinfo[0], vital_info, wait_info, remark_info, speical_info, shift_cate, remarkExtra, FRIDs));
                        }
                    }
                }

                switch (shift_cate)
                {
                    case "D":
                        ViewData["transcate"] = "早班";
                        break;
                    case "E":
                        ViewData["transcate"] = "小夜";
                        break;
                    case "N":
                        ViewData["transcate"] = "大夜";
                        break;
                    default:
                        ViewData["transcate"] = "";
                        break;
                }
                ViewBag.UserNo = userinfo.EmployeesNo;
                ViewBag.UserPwd = userinfo.Pwd;
                ViewBag.Transfer = ConfigurationManager.AppSettings["Transfer"].ToString();
                ViewData["tdList"] = tdList;
                ViewData["transuser"] = userinfo.EmployeesName;
                ViewBag.transcate_list = transcate_list;
                ViewBag.dt_TC = new Func<string, string, DataTable>(Sel_TeamCare);
                return View();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return View();
            }
            finally
            {
                this.link.DBClose();
            }
        }
        public ActionResult TransferDuty_Print()
        {
            try
            {
                string shift = Request["shift"].ToString();
                string type = (string.IsNullOrEmpty(Request["type"]) ? "" : Request["type"].ToString());
                List<TransList> tdList = new List<TransList>();
                Function func_m = new Function();

                string sqlstr = string.Empty, shift_cate = string.Empty, successorNoTemp = string.Empty, successorName = string.Empty;
                int count = 0;
                string tmpshift = string.Empty, tmpcate = string.Empty, checkflag = "";
                int strtime = int.Parse(DateTime.Now.ToString("HHmm"));
                if (strtime >= 0 && strtime <= 759)
                    checkflag = "N";
                else if (strtime >= 800 && strtime <= 1559)
                    checkflag = "D";
                else if (strtime >= 1600 && strtime <= 2359)
                    checkflag = "E";

                bool Category_NA = (userinfo.Category == "NA") ? true : false;

                sqlstr += " SELECT BED_NO, COST_CODE, {0},{1}";
                if (type == "mylist" || string.IsNullOrEmpty(type) || type == "undefined")
                {
                    sqlstr += " FROM {2} WHERE {3} in('" + userinfo.EmployeesNo + "')";
                    if (checkflag == "N")
                    {
                        sqlstr += " AND {4} = (SELECT MAX({4}) FROM {2} WHERE {3} = '" + userinfo.EmployeesNo + "')";
                    }
                    else
                    {
                        sqlstr += " AND {5} = TO_CHAR(SYSDATE,'yyyy/MM/dd')";
                    }
                }
                else
                {
                    sqlstr += " FROM DATA_DISPATCHING WHERE cost_code = '" + type + "'";
                    sqlstr += " AND TO_CHAR(SHIFT_DATE, 'yyyy/MM/dd') = TO_CHAR(SYSDATE,'yyyy/MM/dd')";
                }
                if (!string.IsNullOrEmpty(shift) && !Category_NA)
                {
                    sqlstr += " AND SHIFT_CATE = '" + shift + "'";
                }

                sqlstr += " GROUP BY {6}, COST_CODE ORDER BY SHIFT_CATE,BED_NO";
                if (Category_NA)
                {
                    sqlstr = string.Format(sqlstr, "'" + shift + "' SHIFT_CATE", "'' SUCCESSOR", "DATA_FAVPAT", "EMPLOYE_NO", "DATEITEM", "DATEITEM", "BED_NO");
                }
                else
                {
                    sqlstr = string.Format(sqlstr, "SHIFT_CATE", "SUCCESSOR", "DATA_DISPATCHING", "RESPONSIBLE_USER", "SHIFT_DATE", "TO_CHAR(SHIFT_DATE, 'yyyy/MM/dd')", "BED_NO,SHIFT_CATE,SUCCESSOR");
                }
                #region 不綁派班選擇單位病人---待未來有更好的寫法
                List<SelectListItem> transcate_list = new List<SelectListItem>();

                #endregion
                DataTable Dt = link.DBExecSQL(sqlstr);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        #region --取接班者姓名區塊--
                        if (count == 0)
                        {
                            successorNoTemp = Dt.Rows[i]["SUCCESSOR"].ToString().Trim();//放一開始第一筆的暫存
                            successorName = (successorNoTemp != "") ? GetEmpName(successorNoTemp) : "";//第一次取到的名子
                        }
                        else
                        {//第二次迴圈開始
                            if (Dt.Rows[i]["SUCCESSOR"].ToString().Trim() != successorNoTemp)
                            {
                                successorNoTemp = Dt.Rows[i]["SUCCESSOR"].ToString().Trim();
                                successorName = (successorNoTemp != "") ? GetEmpName(successorNoTemp) : "";
                            }
                        }
                        #endregion --取接班者姓名區塊--end
                        shift_cate = Dt.Rows[i]["SHIFT_CATE"].ToString().Trim();
                	    //20240329 更換WS BedNoTransformFeeNoWithCostCode
                        byte[] ptinfobyte = webService.BedNoTransformFeeNoWithCostCode(Dt.Rows[i]["bed_no"].ToString().Trim(), Dt.Rows[i]["COST_CODE"].ToString().Trim());
                        if (ptinfobyte != null)
                        {
                            string ptinfojarr = CompressTool.DecompressString(ptinfobyte);
                            PatientInfo[] patinfo = JsonConvert.DeserializeObject<PatientInfo[]>(ptinfojarr);
                            // 取得VitalSign資料
                            string[] vital_info = getTranInfo(patinfo[0].FeeNo, shift_cate);
                            string[] wait_info = getWaitInfo(patinfo[0].FeeNo);
                            string speical_info = string.Empty, remark_info = string.Empty;
                            byte[] ptinfoByteCode = webService.GetPatientInfo(patinfo[0].FeeNo);
                            if (ptinfoByteCode != null)
                            {
                                string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                                PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                                patinfo[0] = pi;
                            }
                            string remarkExtra = "";
                            getSpecialInfo(patinfo[0].FeeNo, ref remark_info, ref speical_info, ref remarkExtra);
                            string FRIDs = (func_m.sel_FRIDs(patinfo[0].FeeNo.Trim()) == true) ? "藥" : "";

                            patinfo[0].BedNo = Dt.Rows[i]["bed_no"].ToString().Trim();
                            tdList.Add(new TransList(patinfo[0], vital_info, wait_info, remark_info, speical_info, shift_cate, remarkExtra, FRIDs));
                        }
                    }
                }

                switch (shift_cate)
                {
                    case "D":
                        ViewData["transcate"] = "早班";
                        break;
                    case "E":
                        ViewData["transcate"] = "小夜";
                        break;
                    case "N":
                        ViewData["transcate"] = "大夜";
                        break;
                    default:
                        ViewData["transcate"] = "";
                        break;
                }
                ViewData["tdList"] = tdList;
                ViewData["transuser"] = userinfo.EmployeesName;
                return View();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return View();
            }
            finally
            {
                this.link.DBClose();
            }
        }

        public ActionResult TeamCare_List(string feeno)
        {
            ViewBag.dt_TC = Sel_TeamCare(feeno, DateTime.Now.AddDays(-2).ToString("yyyy/MM/dd"));
            return View();
        }

        /// <summary>
        /// 交班資料儲存
        /// </summary>
        /// <returns></returns>
        public ActionResult TransferDutySave()
        {
            try
            {
                string[] feeList = Request["fee_no"].ToString().Split(',');
                string transcate = Request["transcate2"].ToString();

                for (int i = 0; i <= feeList.Length - 1; i++)
                {
                    int rowct = 0;
                    string feeno = feeList[i].Substring(0, feeList[i].ToString().Trim().Length - 1);
                    string transcate_check = feeList[i].Substring(feeList[i].ToString().Trim().Length - 1, 1);
                    List<DBItem> ditem = new List<DBItem>();
                    if (transcate != transcate_check)
                        continue;
                    DataTable Dt = this.link.DBExecSQL("select count(*) as ct from DATA_TRANS_MEMO where fee_no = '" + feeno + "' AND TRANS_FLAG='M' ");
                    if (Dt.Rows.Count > 0)
                    {
                        for (int j = 0; j < Dt.Rows.Count; j++)
                        {
                            rowct = int.Parse(Dt.Rows[j]["ct"].ToString());
                        }
                    }
                    var memo = Request[feeList[i] + "_memo"].ToString();
                    var memoTrans = memo.Replace("&lt;", "<");
                    memoTrans = memoTrans.Replace("&gt;", ">");
                    ditem.Add(new DBItem("trans_memo", memoTrans, DBItem.DBDataType.String));
                    if (Request[feeList[i] + "_remark"] != null)
                        ditem.Add(new DBItem("trans_remark", Request[feeList[i] + "_remark"].ToString(), DBItem.DBDataType.String));
                    ditem.Add(new DBItem("modify_user", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    ditem.Add(new DBItem("modify_date", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));

                    if (rowct == 0)
                    {
                        ditem.Add(new DBItem("fee_no", feeno, DBItem.DBDataType.String));
                        ditem.Add(new DBItem("create_user", userinfo.EmployeesNo, DBItem.DBDataType.String));
                        ditem.Add(new DBItem("create_date", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                        ditem.Add(new DBItem("trans_flag", "M", DBItem.DBDataType.String));
                        this.link.DBExecInsert("DATA_TRANS_MEMO", ditem);
                    }
                    else
                    {
                        this.link.DBExecUpdate("DATA_TRANS_MEMO", ditem, "fee_no = '" + feeno + "'  AND TRANS_FLAG='M' ");
                    }
                }
                return RedirectToAction("TransferDuty_List", new { @message = "儲存成功" });
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return RedirectToAction("TransferDuty_List", new { @message = "儲存失敗，" + ex.ToString() });
            }
            finally
            {
                this.link.DBClose();
            }
        }

        /// <summary>
        /// 改成在關閉特殊交班內容DIV小視窗後，直接儲存(只存勾的選項)
        /// </summary>
        /// <param name="feeno">feeno</param>
        /// <param name="list">勾的特殊交班項目</param>
        /// <returns></returns>
        public string TransferDuty_Memo_Save(string feeno, string list)
        {
            try
            {
                List<DBItem> ditem = new List<DBItem>();
                int rowct = 0, erow = 0;
                DataTable Dt = this.link.DBExecSQL("select count(*) as ct from DATA_TRANS_MEMO where fee_no = '" + feeno + "' AND TRANS_FLAG='M' ");
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        rowct = int.Parse(Dt.Rows[i]["ct"].ToString());
                    }
                }
                if (string.IsNullOrEmpty(list))
                {
                    list = "";
                }
                ditem.Add(new DBItem("trans_remark", list, DBItem.DBDataType.String));//勾的項目
                ditem.Add(new DBItem("modify_user", userinfo.EmployeesNo, DBItem.DBDataType.String));
                ditem.Add(new DBItem("modify_date", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));

                if (rowct == 0)
                {//新增
                    ditem.Add(new DBItem("fee_no", feeno, DBItem.DBDataType.String));
                    ditem.Add(new DBItem("create_user", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    ditem.Add(new DBItem("create_date", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                    ditem.Add(new DBItem("trans_flag", "M", DBItem.DBDataType.String));
                    erow = this.link.DBExecInsert("DATA_TRANS_MEMO", ditem);
                }
                else
                {//修改memo
                    erow = this.link.DBExecUpdate("DATA_TRANS_MEMO", ditem, "fee_no = '" + feeno + "'  AND TRANS_FLAG='M' ");
                }
                if (erow > 0)
                    return "Y";
                else
                    return "N";
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return "N";
            }
            finally
            {
                this.link.DBClose();
            }
        }

        public ActionResult TransferDuty_Main_Save(FormCollection form)
        {
            string item = "", memo = "", feeno = "", chr_no = "";
            feeno = Request["feeno"];
            chr_no = Request["chr_no"];
            try
            {
                //IDataReader reader = null;
                int rowct = 0;
                for (int i = 0; i < form.AllKeys.Count(); i++)
                {
                    item = form.AllKeys[i];
                    memo = Request[item];
                    if (item != "feeno" && item != "chr_no")
                    {
                        DataTable Dt = this.link.DBExecSQL("select count(*) as ct from DATA_TRANS_MEMO where fee_no = '" + feeno + "' AND TRANS_FLAG='" + item + "' ");
                        if (Dt.Rows.Count > 0)
                        {
                            for (int j = 0; j < Dt.Rows.Count; j++)
                            {
                                rowct = int.Parse(Dt.Rows[j]["ct"].ToString());
                            }
                        }
                        //this.link.DBExecSQL("select count(*) as ct from DATA_TRANS_MEMO where fee_no = '" + feeno + "' AND TRANS_FLAG='" + item + "' ", ref reader);
                        //while (reader.Read())
                        //{
                        //    rowct = int.Parse(reader["ct"].ToString());
                        //}
                        //reader.Close();
                        if (rowct == 0)//新增
                        {
                            List<DBItem> insertList = new List<DBItem>();
                            insertList.Add(new DBItem("FEE_NO", feeno, DBItem.DBDataType.String));
                            insertList.Add(new DBItem("TRANS_MEMO", memo, DBItem.DBDataType.String));
                            insertList.Add(new DBItem("CREATE_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                            insertList.Add(new DBItem("CREATE_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                            insertList.Add(new DBItem("TRANS_FLAG", item, DBItem.DBDataType.String));
                            this.link.DBExecInsert("DATA_TRANS_MEMO", insertList);
                        }
                        else //更新
                        {
                            List<DBItem> UpdateList = new List<DBItem>();
                            UpdateList.Add(new DBItem("TRANS_MEMO", memo, DBItem.DBDataType.String));
                            UpdateList.Add(new DBItem("MODIFY_USER", userinfo.EmployeesNo, DBItem.DBDataType.String));
                            UpdateList.Add(new DBItem("MODIFY_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                            this.link.DBExecUpdate("DATA_TRANS_MEMO", UpdateList, "FEE_NO ='" + feeno + "' and TRANS_FLAG='" + item + "' ");
                        }
                    }
                }
                return RedirectToAction("Detail_2", new { @message = "儲存成功", @feeno = feeno, @chr_no = chr_no });
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return RedirectToAction("Detail_2", new { @message = "儲存成功", @feeno = feeno, @chr_no = chr_no });
            }
            finally
            {
                this.link.DBClose();
            }
        }
        #endregion

        #region 工作清單
        /// <summary>
        /// 工作清單儲存
        /// </summary>
        /// <returns></returns>
        public ActionResult TaskSave()
        {
            try
            {
                string item = "", timepriod = "", feeno = "", sheetno = "", setaction = "";
                feeno = Request["feeno"].ToString().Trim();
                sheetno = Request["sheetno"].ToString().Trim();
                timepriod = Request["timepriod"].ToString().Trim();
                setaction = Request["setaction"].ToString().Trim();

                DateTime NowDate = DateTime.Now;
                int rowct = 0;
                string sqlwherestr = " fee_no = '" + feeno + "' AND sheet_no='" + sheetno + "' AND EXEC_PRIOD= '" + timepriod + "' AND SET_ACTION= '" + setaction;
                sqlwherestr += "' AND to_char(EXEC_TIME,'yyyy/mm/dd') = '" + NowDate.ToString("yyyy/MM/dd") + "' AND DELETED is null";

                string sqlstr = "select count(*) as ct from DATA_TASK_EXEC_RECORD where " + sqlwherestr;
                DataTable Dt = link.DBExecSQL(sqlstr);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        rowct = int.Parse(Dt.Rows[i]["ct"].ToString());
                    }
                }

                List<DBItem> UpdateList = new List<DBItem>();
                if (rowct > 0)//新增
                {
                    UpdateList.Add(new DBItem("DELETED", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    this.link.DBExecUpdate("DATA_TASK_EXEC_RECORD", UpdateList, sqlwherestr);
                }
                List<DBItem> insertItem = new List<DBItem>();
                insertItem.Add(new DBItem("fee_no", feeno, DBItem.DBDataType.String));
                insertItem.Add(new DBItem("sheet_no", sheetno, DBItem.DBDataType.String));
                insertItem.Add(new DBItem("order_content", Request["order"].ToString().Trim(), DBItem.DBDataType.String));
                insertItem.Add(new DBItem("set_action", Request["action"].ToString().Trim(), DBItem.DBDataType.String));
                insertItem.Add(new DBItem("exec_priod", timepriod, DBItem.DBDataType.String));
                insertItem.Add(new DBItem("exec_time", NowDate.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                insertItem.Add(new DBItem("exec_user", userinfo.EmployeesNo, DBItem.DBDataType.String));
                insertItem.Add(new DBItem("exec_result", Request["result"].ToString().Trim(), DBItem.DBDataType.String));
                if (Request["result"].ToString().Trim() != "Y")
                {
                    insertItem.Add(new DBItem("exec_reason", Request["reason"].ToString().Trim(), DBItem.DBDataType.String));
                }
                int execRow = this.link.DBExecInsert("DATA_TASK_EXEC_RECORD", insertItem);

                string cancelwhere = " fee_no = '" + feeno + "' AND sheet_no='" + sheetno + "' ";
                UpdateList = new List<DBItem>();
                if (Request["result"].ToString().Trim() == "D")
                {
                    UpdateList.Add(new DBItem("CANCELTIME", NowDate.ToString("yyyy/MM/dd") + " " + timepriod + ":00", DBItem.DBDataType.DataTime));
                }
                this.link.DBExecUpdate("DATA_SIGNORDER", UpdateList, cancelwhere);

                if (execRow != 1)
                    Response.Write("儲存失敗，請聯絡系統負責人");
                else
                    Response.Write("儲存成功");

                return new EmptyResult();
            }
            catch (Exception ex)
            {
                Response.Write(ex.Message.ToString());
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return new EmptyResult();
            }
            finally
            {
                this.link.DBClose();
            }
        }

        /// <summary>
        /// 工作清單
        /// </summary>
        /// <returns></returns>
        public ActionResult Task_List(string types = "")
        {
            try
            {
                List<TaskList> tkList = new List<TaskList>();

                string sqlstr = string.Empty, shift_cate = string.Empty;
                bool Category_NA = (userinfo.Category == "NA") ? true : false;
                if (Category_NA)
                {
                    sqlstr += " SELECT BED_NO, '' SHIFT_CATE, COST_CODE ";
                    sqlstr += " FROM data_favpat ";
                    sqlstr += " WHERE EMPLOYE_NO='" + userinfo.EmployeesNo + "' AND DATEITEM = TO_CHAR(SYSDATE,'yyyy/MM/dd') ";

                }
                else
                {
                    sqlstr += " SELECT BED_NO, SHIFT_CATE, COST_CODE, (CASE WHEN TO_CHAR(SYSDATE,'hh24mi') BETWEEN '0800' AND '1559' THEN 'D' ";
                    sqlstr += " WHEN TO_CHAR(SYSDATE,'hh24mi') BETWEEN '1600' AND '2359' THEN 'E' ";
                    sqlstr += " WHEN TO_CHAR(SYSDATE,'hh24mi') BETWEEN '0000' AND '0759' THEN 'N' ELSE SHIFT_CATE END ) SHIFT_CATE2";
                    sqlstr += " FROM DATA_DISPATCHING WHERE ";
                    sqlstr += " RESPONSIBLE_USER = '" + userinfo.EmployeesNo + "' AND TO_CHAR(SHIFT_DATE, 'yyyy/MM/dd') = TO_CHAR(SYSDATE,'yyyy/MM/dd') ";
                    //sqlstr += " AND SHIFT_CATE = CASE WHEN TO_CHAR(SYSDATE,'hh24mi') BETWEEN '0800' AND '1559' THEN 'D' ";
                    //sqlstr += " WHEN TO_CHAR(SYSDATE,'hh24mi') BETWEEN '1600' AND '2359' THEN 'E' ELSE 'N' END ";
                    sqlstr += " GROUP BY BED_NO, SHIFT_CATE, COST_CODE ORDER BY SHIFT_CATE,BED_NO";
                }
                int strtime = int.Parse(DateTime.Now.ToString("HHmm"));
                List<string> shiftcate = new List<string>() { "D", "E", "N" };

                DataTable Dt = link.DBExecSQL(sqlstr);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        //if ((strtime >= 0 && strtime <= 59) || (strtime >= 1600 && strtime <= 1659) || (strtime >= 800 && strtime <= 859))
                        //    shift_cate = reader["SHIFT_CATE"].ToString().Trim();
                        //else
                        //{
                        //    if (reader["SHIFT_CATE"].ToString().Trim() != reader["SHIFT_CATE2"].ToString().Trim())
                        //        continue;
                        //    shift_cate = reader["SHIFT_CATE2"].ToString().Trim();
                        //}

                        shift_cate = Dt.Rows[i]["SHIFT_CATE"].ToString().Trim();
                	    //20240329 更換WS BedNoTransformFeeNoWithCostCode
                        byte[] ptinfobyte = webService.BedNoTransformFeeNoWithCostCode(Dt.Rows[i]["bed_no"].ToString().Trim(), Dt.Rows[i]["COST_CODE"].ToString().Trim());
                        if (ptinfobyte != null)
                        {
                            string ptinfojarr = CompressTool.DecompressString(ptinfobyte);
                            List<PatientInfo> patinfo = JsonConvert.DeserializeObject<List<PatientInfo>>(ptinfojarr);
                            // 取得VitalSign資料
                            List<OrderList> olist = null;
                            List<ExecResultList> elist = null;
                            patinfo[0].BedNo = Dt.Rows[i]["bed_no"].ToString().Trim();
                            patinfo[0].CostCenterNo = Dt.Rows[i]["cost_code"].ToString().Trim();
                            patinfo[0].DeptNo = shift_cate; //借用patinfo.DeptNo欄位當班別
                            getSetting(patinfo[0].FeeNo.ToString().Trim(), ref olist);
                            getExecResult(patinfo[0].FeeNo.ToString().Trim(), ref elist);
                            //shift_cate = reader["SHIFT_CATE"].ToString().Trim();
                            //byte[] ptinfobyte = webService.BedNoTransformFeeNo(reader["bed_no"].ToString().Trim());
                            //if (ptinfobyte != null)
                            //{
                            //    string ptinfojarr = CompressTool.DecompressString(ptinfobyte);
                            //    List<PatientInfo> patinfo = JsonConvert.DeserializeObject<List<PatientInfo>>(ptinfojarr);
                            //    // 取得VitalSign資料
                            //    List<OrderList> olist = null;
                            //    List<ExecResultList> elist = null;
                            //    patinfo[0].BedNo = reader["bed_no"].ToString().Trim();
                            //    patinfo[0].CostCenterNo = reader["cost_code"].ToString().Trim();
                            //    patinfo[0].DeptNo = shift_cate; //借用patinfo.DeptNo欄位當班別
                            //    getSetting(patinfo[0].FeeNo.ToString().Trim(), ref olist);
                            //    getExecResult(patinfo[0].FeeNo.ToString().Trim(), ref elist);

                            if (Category_NA)
                            {
                                string json = JsonConvert.SerializeObject(patinfo[0]);
                                //不轉字串會造成迴圈參考影響到前面資料  造成全部資料都變成N(大夜型態)
                                foreach (var item in shiftcate)
                                {
                                    PatientInfo pat = JsonConvert.DeserializeObject<PatientInfo>(json);
                                    pat.DeptNo = item; //借用patinfo.DeptNo欄位當班別
                                    TaskList tk = new TaskList(pat, olist, elist);
                                    tkList.Add(tk);
                                }
                            }
                            else
                            {
                                tkList.Add(new TaskList(patinfo[0], olist, elist));
                            }
                        }
                    }
                }
                ViewBag.shiftcate = types;
                ViewData["tkList"] = tkList;
                ViewData["transuser"] = userinfo.EmployeesName;

                return View();
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return View();
            }
            finally
            {
                this.link.DBClose();
            }
        }

        #endregion

        #region 病人詳細資料_舊版

        /// <summary>
        /// 住院詳細資料
        /// </summary>
        /// <returns></returns>
        public ActionResult Detail()
        {
            string fee_no = string.Empty;
            if (Request["feeno"] != null && Request["chr_no"] != null)
            {
                byte[] ptInfoByte = webService.GetPatientInfo(Request["feeno"].ToString().Trim());
                if (ptInfoByte != null)
                {
                    string ptinfoJson = CompressTool.DecompressString(ptInfoByte);
                    PatientInfo ptinfo = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJson);
                    ViewData["ptinfo"] = ptinfo;

                    List<PatientInfo> allergy_list = new List<PatientInfo>();
                    byte[] allergyfoByteCode = webService.GetAllergyList(ptinfo.FeeNo.Trim());
                    if (allergyfoByteCode != null)
                    {
                        string allergyJosnArr = CompressTool.DecompressString(allergyfoByteCode);
                        List<PatientInfo> allergy = JsonConvert.DeserializeObject<List<PatientInfo>>(allergyJosnArr);
                        allergy_list = allergy;
                    }
                    ViewData["Allergy"] = allergy_list;
                }
            }
            return View();
        }

        public ActionResult DetailMain()
        {
            string message = string.Empty;
            try
            {
                if (Request["fee_no"] != null)
                {
                    string fee_no = Request["fee_no"].ToString().Trim();
                    string sqlstr = string.Empty;
                    #region 病人資訊
                    byte[] ptInfoByte = webService.GetPatientInfo(fee_no);
                    if (ptInfoByte != null)
                    {
                        string ptinfoJson = CompressTool.DecompressString(ptInfoByte);
                        PatientInfo ptinfo = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJson);
                        ViewData["ptinfo"] = ptinfo;
                    }
                    #endregion
                    #region VitalSign
                    // VitalSign
                    List<string> vsd = new List<string>();
                    sqlstr = "  SELECT (SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'system' AND P_GROUP = 'vs_item' AND P_VALUE = DV.VS_ITEM) AS VS_ITEM_NAME, ";
                    sqlstr += " VS_PART, VS_RECORD, VS_REASON FROM DATA_VITALSIGN DV WHERE FEE_NO = '" + fee_no + "' AND ";
                    sqlstr += " VS_ID = (SELECT MAX(VS_ID) FROM DATA_VITALSIGN WHERE FEE_NO = DV.FEE_NO) AND TRIM(VS_RECORD) IS NOT NULL ";

                    DataTable Dt = link.DBExecSQL(sqlstr);
                    if (Dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            if (Dt.Rows[i]["vs_item_name"].ToString().Trim() == "")
                                continue;
                            string tmpStr = Dt.Rows[i]["vs_item_name"].ToString().Trim();
                            if (Dt.Rows[i]["vs_part"].ToString().Trim() != "")
                                tmpStr += "[" + Dt.Rows[i]["vs_part"].ToString().Trim() + "] ";
                            tmpStr += Dt.Rows[i]["VS_RECORD"].ToString().Trim();
                            if (Dt.Rows[i]["VS_REASON"].ToString().Trim() != "")
                                tmpStr += " (" + Dt.Rows[i]["VS_REASON"].ToString().Trim() + ")";

                            vsd.Add(tmpStr);
                        }
                    }
                    ViewData["vitalsign"] = vsd;

                    //this.link.DBExecSQL(sqlstr, ref reader);
                    //while (reader.Read())
                    //{
                    //    if (reader["vs_item_name"].ToString().Trim() == "")
                    //        continue;
                    //    string tmpStr = reader["vs_item_name"].ToString().Trim();
                    //    if (reader["vs_part"].ToString().Trim() != "")
                    //        tmpStr += "[" + reader["vs_part"].ToString().Trim() + "] ";
                    //    tmpStr += reader["VS_RECORD"].ToString().Trim();
                    //    if (reader["VS_REASON"].ToString().Trim() != "")
                    //        tmpStr += " (" + reader["VS_REASON"].ToString().Trim() + ")";

                    //    vsd.Add(tmpStr);
                    //}
                    //ViewData["vitalsign"] = vsd;
                    //reader.Close();

                    // GCS
                    sqlstr = "  SELECT (SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'system' AND P_GROUP = 'vs_item' AND P_VALUE = DV.VS_ITEM) AS VS_ITEM_NAME, ";
                    sqlstr += " VS_PART, VS_RECORD, VS_REASON FROM DATA_VITALSIGN DV WHERE FEE_NO = '" + fee_no + "' AND VS_ITEM = 'gc' AND ";
                    sqlstr += " VS_ID = (SELECT MAX(VS_ID) FROM DATA_VITALSIGN WHERE FEE_NO = DV.FEE_NO AND VS_ITEM = DV.VS_ITEM) AND TRIM(VS_RECORD) IS NOT NULL ";

                    Dt = link.DBExecSQL(sqlstr);
                    if (Dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            if (Dt.Rows[i]["vs_item_name"].ToString().Trim() == "")
                                continue;
                            string tmpStr = Dt.Rows[i]["vs_item_name"].ToString().Trim();
                            if (Dt.Rows[i]["vs_part"].ToString().Trim() != "")
                                tmpStr += "[" + Dt.Rows[i]["vs_part"].ToString().Trim() + "] ";
                            tmpStr += Dt.Rows[i]["VS_RECORD"].ToString().Trim();
                            if (Dt.Rows[i]["VS_REASON"].ToString().Trim() != "")
                                tmpStr += " (" + Dt.Rows[i]["VS_REASON"].ToString().Trim() + ")";
                            ViewData["gcs"] = tmpStr;
                        }
                    }
                    ViewData["vitalsign"] = vsd;

                    //this.link.DBExecSQL(sqlstr, ref reader);
                    //while (reader.Read())
                    //{
                    //    if (reader["vs_item_name"].ToString().Trim() == "")
                    //        continue;
                    //    string tmpStr = reader["vs_item_name"].ToString().Trim();
                    //    if (reader["vs_part"].ToString().Trim() != "")
                    //        tmpStr += "[" + reader["vs_part"].ToString().Trim() + "] ";
                    //    tmpStr += reader["VS_RECORD"].ToString().Trim();
                    //    if (reader["VS_REASON"].ToString().Trim() != "")
                    //        tmpStr += " (" + reader["VS_REASON"].ToString().Trim() + ")";
                    //    ViewData["gcs"] = tmpStr;
                    //}
                    //ViewData["vitalsign"] = vsd;
                    //reader.Close();
                    #endregion
                    #region IO
                    // IO資料

                    List<string> io = new List<string>();
                    sqlstr = "  SELECT TP.P_NAME AS IO_NAME, CASE WHEN TP.P_GROUP = 'intaketype' THEN '[輸入]' ELSE '[輸出]' END AS IO_TYPE, IDA.AMOUNT AS IO_AMT, ";
                    sqlstr += "  CASE WHEN IDA.AMOUNT_UNIT = '1'tHEN 'mL' ELSE '次' END IO_UNIT FROM IO_DATA IDA JOIN (SELECT * FROM SYS_PARAMS WHERE P_MODEL ='iotype' ) TP ";
                    sqlstr += "  ON TP.P_VALUE = IDA.TYPEID WHERE FEENO = '" + fee_no + "' AND CREATTIME = (SELECT MAX(CREATTIME) FROM IO_DATA WHERE FEENO = IDA.FEENO) ";

                    Dt = link.DBExecSQL(sqlstr);
                    if (Dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < Dt.Rows.Count; i++)
                        {
                            string tmpStr = Dt.Rows[i]["io_name"].ToString().Trim();
                            tmpStr += "[" + Dt.Rows[i]["io_type"].ToString().Trim() + "] ";
                            tmpStr += Dt.Rows[i]["io_amt"].ToString().Trim();
                            tmpStr += " " + Dt.Rows[i]["io_unit"].ToString().Trim();
                            io.Add(tmpStr);
                        }
                    }
                    ViewData["io"] = io;

                    //this.link.DBExecSQL(sqlstr, ref reader);
                    //while (reader.Read())
                    //{
                    //    string tmpStr = reader["io_name"].ToString().Trim();
                    //    tmpStr += "[" + reader["io_type"].ToString().Trim() + "] ";
                    //    tmpStr += reader["io_amt"].ToString().Trim();
                    //    tmpStr += " " + reader["io_unit"].ToString().Trim();
                    //    io.Add(tmpStr);
                    //}
                    //ViewData["io"] = io;
                    //reader.Close();
                    #endregion
                    #region 管路
                    //管路資料
                    #endregion
                    #region 評估結果
                    //評估結果
                    #endregion
                    #region 檢驗
                    //取得資料
                    try
                    {
                        byte[] labByte = webService.GetLab8HR(fee_no);
                        if (labByte != null)
                        {
                            string labJson = CompressTool.DecompressString(labByte);
                            List<Lab> labList = JsonConvert.DeserializeObject<List<Lab>>(labJson);
                            ViewData["lab"] = labList;
                        }
                    }
                    catch (Exception ex)
                    {
                        //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                        string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                        string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                        write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                        message += ex.Message.ToString() + "\n";
                    }
                    #endregion
                    #region 檢查
                    //取得檢查資料
                    try
                    {
                        byte[] examByte = webService.GetExam(fee_no);
                        if (examByte != null)
                        {
                            string examJson = CompressTool.DecompressString(examByte);
                            List<Exam> examList = JsonConvert.DeserializeObject<List<Exam>>(examJson);
                            ViewData["exam"] = examList;
                        }
                    }
                    catch (Exception ex)
                    {
                        message += ex.Message.ToString() + "\n";
                    }
                    #endregion
                    #region 會診
                    //會診
                    try
                    {
                        List<Consultation> ConsultationList = new List<Consultation>();

                        byte[] ConsultationByte = webService.GetPatientConsult(fee_no);
                        if (ConsultationByte != null)
                        {
                            string ConsultationJson = CompressTool.DecompressString(ConsultationByte);
                            ConsultationList = JsonConvert.DeserializeObject<List<Consultation>>(ConsultationJson);
                        }
                        if (ConsultationList.Count() == 0)
                            ViewData["call"] = null;
                        else
                            ViewData["call"] = ConsultationList;
                    }
                    catch (Exception ex)
                    {
                        message += ex.Message.ToString() + "\n";
                    }
                    #endregion
                    #region 備血
                    List<string> blood = new List<string>();
                    try
                    {
                        byte[] ptByte = webService.GetPatientInfo(fee_no);
                        if (ptByte != null)
                        {
                            string ptJson = CompressTool.DecompressString(ptByte);
                            PatientInfo ptinfo = JsonConvert.DeserializeObject<PatientInfo>(ptJson);

                            if (ptinfo.Blood_Type != null)
                            {
                                blood.Add("血型：" + ptinfo.Blood_Type.Trim());
                            }
                            if (ptinfo.Prepare_Date != DateTime.MinValue)
                            {
                                blood.Add("備血日期：" + ptinfo.Prepare_Date.ToString("yyyy/MM/dd"));
                                blood.Add("血種：" + ptinfo.Code_Desc.Trim());
                                blood.Add("備血量：" + ptinfo.Ttl_Gty.ToString());
                                blood.Add("餘血量：" + ptinfo.Unrece_Gty.ToString());
                            }
                            if (blood.Count != 0)
                                ViewData["blood"] = blood;
                            else
                                ViewData["blood"] = null;
                        }


                    }
                    catch (Exception ex)
                    {
                        message += ex.Message.ToString() + "\n";
                    }
                    #endregion
                    #region 手術
                    //手術
                    get_TransferDuty_Item_op(fee_no, "Item");
                    #endregion
                    #region 護理問題
                    //護理問題
                    ViewBag.nur = care_m.GetCarePlan_Master(fee_no);
                    #endregion

                    #region 處置 不使用
                    //取得處置資料
                    /*try
                    {
                        byte[] procedureByte = webService.GetProcedure(fee_no);
                        if (procedureByte != null)
                        {
                            string procedureJson = CompressTool.DecompressString(procedureByte);
                            List<Procedure> procedureList = JsonConvert.DeserializeObject<List<Procedure>>(procedureJson);
                            ViewData["procedure"] = procedureList;
                        }
                    }
                    catch (Exception ex)
                    {
                        message += ex.Message.ToString() + "\n";
                    }*/
                    #endregion
                    #region 特殊交班 不使用
                    // 取得特殊交班
                    /*this.link.DBExecSQL(" SELECT TRANS_MEMO FROM DATA_TRANS_MEMO WHERE FEE_NO = '" + fee_no + "' ", ref reader);
                    while (reader.Read())
                    {
                        ViewData["shift_memo"] = reader["TRANS_MEMO"].ToString().Trim();
                    }
                    reader.Close();*/
                    #endregion
                    #region 工作清單 不使用
                    /*List<string> work = new List<string>();
                    sqlstr = "  SELECT SET_ACTION, SET_PRIOD FROM DATA_SIGNORDER WHERE FEE_NO = '" + fee_no + "'  AND TRIM(SIGN_USER) IS NOT NULL AND TRIM(SET_USER) IS NOT NULL";
                    this.link.DBExecSQL(sqlstr, ref reader);
                    while (reader.Read())
                    {
                        string[] timePriod = reader["SET_PRIOD"].ToString().Trim().Split('|');
                        string setAction = reader["SET_ACTION"].ToString().Trim();
                        for (int i = 0; i <= timePriod.Length - 1; i++ )
                        {
                            work.Add(timePriod[i] + " " + setAction);
                        }
                    }
                    ViewData["work"] = work;
                    reader.Close();

                    */
                    #endregion
                }

                return View(new { @message = message });
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return View(new { @message = message });
            }
            finally
            {
                this.link.DBClose();
            }
        }

        /// <summary>
        /// 手術轉送前檢查表
        /// </summary>
        /// <returns></returns>
        public ActionResult Surgery_Check()
        {
            return View();
        }

        /// <summary> 手術轉送前檢查表-登入 </summary>
        public ActionResult Login()
        {
            return View();
        }

        /// <summary> 心導管檢查護理查核表 </summary>
        public ActionResult Cardiac_Catheterization_Check()
        {
            return View();
        }

        /// <summary>
        /// 醫囑檢視
        /// </summary>
        /// <returns></returns>
        public ActionResult OrderList()
        {

            //文字醫囑
            if (Request["fee_no"] != null)
            {
                byte[] textorderByte = webService.GetTextOrder(Request["fee_no"].ToString().Trim());
                if (textorderByte != null)
                {
                    string textorderJsonArr = CompressTool.DecompressString(textorderByte);
                    List<TextOrder> textorder = JsonConvert.DeserializeObject<List<TextOrder>>(textorderJsonArr);
                    ViewData["textorder"] = textorder;
                }
            }
            return View();
        }

        /// <summary>
        /// 轉送查核表
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult HandOver()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Print_handover(string url)
        {
            string strPath = @"C:\\wkhtmltopdf\\wkhtmltopdf.exe";
            string filename = DateTime.Now.ToString("yyyyMMddHHmmssfff") + url.Substring(url.IndexOf("feeno=") + 6, url.Length - url.IndexOf("feeno=") - 6) + ".pdf";
            string tempPath = "C:\\inetpub\\NIS\\Images\\" + filename;
            //string strPath = @"C:\\Users\\JayYang\\Desktop\\wkhtmltopdf\\wkhtmltopdf.exe";
            //string filename = DateTime.Now.ToString("yyyyMMddHHmmssfff") + url.Substring(url.IndexOf("="), url.IndexOf("&") - url.IndexOf("=") -1).Trim() + ".pdf";
            //string tempPath = "D:\\Dropbox\\NIS\\NIS\\NIS\\Images\\" + filename;
            string pdfpath = "../Images/" + filename;
            Process p = new Process();
            p.StartInfo.FileName = strPath;
            p.StartInfo.Arguments = url.Substring(0, url.IndexOf("?")) + " " + tempPath;
            p.StartInfo.UseShellExecute = true;
            p.Start();
            p.WaitForExit();

            Response.Write("<script>window.location.href='HandOver';window.open('" + pdfpath + "');</script>");
            return new EmptyResult();
        }

        #endregion

        #region 病人詳細資料_新版

        //主頁
        public ActionResult Detail_2(string feeno, string chr_no)
        {
            string fee_no = string.Empty;
            if (feeno != null && chr_no != null)
            {
                ViewBag.chr_no = chr_no;
                string boolstring = feeno.Trim().Substring(feeno.Trim().Length - 1);
                if (boolstring == "D" || boolstring == "N" || boolstring == "E")
                    ViewBag.fee_no = feeno.Trim().Substring(0, feeno.ToString().Trim().Length - 1);
                else
                    ViewBag.fee_no = feeno;
            }
            return View();
        }

        //交班明細
        public ActionResult TransferDuty_Main()
        {
            if (Request["fee_no"] != null)
            {
                string fee_no = Request["fee_no"].ToString().Trim();
                string sqlstr = string.Empty;
                DataTable dt = this.sel_assess_data(fee_no, "");

                #region 病人資訊
                if (this.sel_data(dt, "param_ipd_reason") == "")
                {
                    byte[] ptinfoByteCode = webService.GetPatientInfo(fee_no);
                    if (ptinfoByteCode != null)
                    {
                        string ptinfoJosnArr = CompressTool.DecompressString(ptinfoByteCode);
                        PatientInfo pi = JsonConvert.DeserializeObject<PatientInfo>(ptinfoJosnArr);
                        ViewBag.icd9 = pi.ICD9_code1.Substring(pi.ICD9_code1.IndexOf("(") + 1, pi.ICD9_code1.Length - pi.ICD9_code1.IndexOf("(") - 2);
                    }
                }
                else
                    ViewBag.icd9 = this.sel_data(dt, "param_ipd_reason");
                #endregion
                #region 過去病史
                string last_inday = "";
                if (this.sel_data(dt, "param_ipd_lasttime") != "")
                    last_inday += "最近一次時間：" + this.sel_data(dt, "param_ipd_lasttime");
                if (this.sel_data(dt, "param_ipd_diag") != "")
                    last_inday += "，原因：" + this.sel_data(dt, "param_ipd_diag");
                ViewBag.last_inday = (last_inday != "") ? last_inday : "目前無資料";

                string last_drug = "";
                if (this.sel_data(dt, "param_med_name") != "")
                {
                    string[] drug_name = this.sel_data(dt, "param_med_name").Split(',');
                    string[] drug_fre = this.sel_data(dt, "param_med_frequency").Split(',');
                    string[] drug_amount = this.sel_data(dt, "param_med_amount").Split(',');
                    string[] drug_way = this.sel_data(dt, "param_med_way").Split(',');
                    for (int i = 0; i < drug_name.Length; i++)
                    {
                        last_drug += "藥物名稱：" + drug_name[i];
                        last_drug += "，頻次：" + drug_fre[i];
                        last_drug += "，劑量：" + drug_amount[i];
                        last_drug += "，途徑：" + drug_way[i] + "<br />";
                    }
                }
                ViewBag.last_drug = (last_drug != "") ? last_drug : "目前無資料";

                string im_history = "";
                if (this.sel_data(dt, "param_im_history_item1") != "")
                    im_history += "高血壓：" + this.sel_data(dt, "param_im_history_item1") + "年; ";
                if (this.sel_data(dt, "param_im_history_item2") != "")
                    im_history += "心臟病：" + this.sel_data(dt, "param_im_history_item2") + "年; ";
                if (this.sel_data(dt, "param_im_history_item3") != "")
                    im_history += "糖尿病：" + this.sel_data(dt, "param_im_history_item3") + "年; ";
                if (this.sel_data(dt, "param_im_history_item4") != "")
                    im_history += "氣喘：" + this.sel_data(dt, "param_im_history_item4") + "年; ";
                if (this.sel_data(dt, "param_im_history_item_other") != "")
                    im_history += "，其他疾病：" + this.sel_data(dt, "param_im_history_item_other") + this.sel_data(dt, "param_im_history_item_other_txt");
                ViewBag.im_history = (im_history != "") ? im_history : "目前無資料";

                string su_history = "";
                if (this.sel_data(dt, "param_su_history_trauma_txt") != "")
                    su_history += "外傷：" + this.sel_data(dt, "param_su_history_trauma_txt") + "; ";
                if (this.sel_data(dt, "param_su_history_surgery_txt") != "")
                    su_history += "手術：" + this.sel_data(dt, "param_su_history_surgery_txt") + "; ";
                if (this.sel_data(dt, "param_su_history_other_txt") != "")
                    su_history += "外傷/手術/外科疾病：" + this.sel_data(dt, "param_su_history_other_txt") + "; ";
                ViewBag.su_history = (su_history != "") ? su_history : "目前無資料";

                #endregion
                get_TransferDuty_Item_vital(fee_no, "Main");
                get_TransferDuty_Item_gcs(fee_no, "Main");
                get_TransferDuty_Item_io(fee_no);
                ViewData["tube"] = get_TransferDuty_Item_tube(fee_no, "Main");
                ViewData["assess"] = get_TransferDuty_Item_assess(fee_no, "Main");
                ViewData["lab"] = get_TransferDuty_Item_lab_main(fee_no, "Main");
                ViewData["exam"] = get_TransferDuty_Item_exam_main(fee_no, "Main");
                ViewData["call"] = get_TransferDuty_Item_Consultation(fee_no, "Main");
                get_TransferDuty_Item_blood(fee_no, "Main");
                get_TransferDuty_Item_op(fee_no, "Main");
                ViewBag.nur = care_m.GetCarePlan_Master(fee_no);
                get_memo(fee_no);

                ViewBag.feeno = fee_no;
                ViewBag.chr_no = Request["chr_no"].ToString().Trim();
                return View();
            }

            Response.Write("<script>alert('登入逾時');</script>");
            return new EmptyResult();
        }

        public ActionResult TransferDuty_Item()
        {
            string flag = Request["flag"], fee_no = Request["fee_no"];
            switch (flag)
            {
                case "vital":
                    get_TransferDuty_Item_vital(fee_no, "Item");
                    break;
                case "gcs":
                    get_TransferDuty_Item_gcs(fee_no, "Item");
                    break;
                case "io":
                    get_TransferDuty_Item_io(fee_no);
                    break;
                case "tube":
                    ViewData["tube"] = get_TransferDuty_Item_tube(fee_no, "Item");
                    break;
                case "assess":
                    ViewData["assess"] = get_TransferDuty_Item_assess(fee_no, "Item");
                    break;
                case "lab":
                    get_TransferDuty_Item_lab(fee_no, "Item");
                    break;
                case "exam":
                    get_TransferDuty_Item_exam(fee_no, "Item");
                    break;
                case "call":
                    ViewData["call"] = get_TransferDuty_Item_Consultation(fee_no, "Item");
                    break;
                case "blood":
                    get_TransferDuty_Item_blood(fee_no, "Item");
                    break;
                case "op":
                    get_TransferDuty_Item_op(fee_no, "Main");
                    break;
                case "nur":
                    ViewBag.nur = care_m.GetCarePlan_Master(fee_no);
                    break;
                case "spec":
                    get_TransferDuty_Item_spec(fee_no);
                    break;
                default:
                    break;
            }
            get_memo(fee_no);
            ViewData["flag"] = flag;
            return View();
        }

        #endregion

        #region 交班共用Method

        /// <summary> 取得生命徵象 </summary>
        public void get_TransferDuty_Item_vital(string fee_no, string flag)
        {
            try
            {
                List<string> vsId = new List<string>();
                List<VitalSignDataList> vsList = new List<VitalSignDataList>();
                VitalSignDataList vsdl = null;
                string sqlstr = "";
                if (flag == "Main")
                {
                    sqlstr = "SELECT * FROM ((SELECT VS_ID FROM DATA_VITALSIGN WHERE ";
                    sqlstr += "FEE_NO = '" + fee_no + "' AND VS_ITEM IN ('bt','mp','bf','bp','sp','cv') GROUP BY VS_ID) order by  Vs_id desc) WHERE ROWNUM < 4";
                }
                else
                {
                    sqlstr = "SELECT VS_ID FROM DATA_VITALSIGN WHERE FEE_NO = '" + fee_no + "' AND VS_ITEM IN ('bt','mp','bf','bp','sp','cv') ";
                    sqlstr += "GROUP BY VS_ID";
                }
                DataTable Dt = link.DBExecSQL(sqlstr);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        vsId.Add(Dt.Rows[i]["vs_id"].ToString().Trim());
                    }
                }


                // 開始處理資料
                for (int i = 0; i <= vsId.Count - 1; i++)
                {
                    //初始化資料
                    vsdl = new VitalSignDataList();

                    sqlstr = "select vsd.*, to_char(modify_date,'yyyy/MM/dd hh24:mi:ss') as m_date from data_vitalsign vsd where ";
                    sqlstr += "fee_no = '" + fee_no + "' and vs_id = '" + vsId[i] + "' and VS_ITEM IN ('bt','mp','bf','bp','sp','cv') ";

                    vsdl.vsid = vsId[i];
                    Dt = link.DBExecSQL(sqlstr);
                    if (Dt.Rows.Count > 0)
                    {
                        for (int j = 0; j < Dt.Rows.Count; j++)
                        {
                            vsdl.DataList.Add(new VitalSignData(
                                Dt.Rows[j]["vs_item"].ToString().Trim(),
                                Dt.Rows[j]["vs_part"].ToString().Trim(),
                                Dt.Rows[j]["vs_record"].ToString().Trim(),
                                Dt.Rows[j]["vs_reason"].ToString().Trim(),
                                Dt.Rows[j]["vs_memo"].ToString().Trim(),
                                Dt.Rows[j]["vs_other_memo"].ToString().Trim(),
                                "", "", "",
                                Dt.Rows[j]["m_date"].ToString().Trim()
                                ));
                        }
                    }
                    //link.DBExecSQL(sqlstr, ref reader);
                    //while (reader.Read())
                    //{
                    //    vsdl.DataList.Add(new VitalSignData(
                    //        reader["vs_item"].ToString().Trim(),
                    //        reader["vs_part"].ToString().Trim(),
                    //        reader["vs_record"].ToString().Trim(),
                    //        reader["vs_reason"].ToString().Trim(),
                    //        reader["vs_memo"].ToString().Trim(),
                    //        reader["vs_other_memo"].ToString().Trim(),
                    //        "", "", "",
                    //        reader["m_date"].ToString().Trim()
                    //        ));
                    //}
                    vsList.Add(vsdl);
                    vsdl = null;
                }

                ViewData["VSData"] = vsList;
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
        /// <summary> 取得意識狀況 </summary>
        public void get_TransferDuty_Item_gcs(string fee_no, string flag)
        {
            try
            {
                List<string> vsId = new List<string>();
                List<VitalSignDataList> GCSList = new List<VitalSignDataList>();
                VitalSignDataList vsdl = null;
                string sqlstr = "";
                if (flag == "Main")
                {
                    sqlstr = "SELECT * FROM (SELECT VS_ID FROM DATA_VITALSIGN WHERE ";
                    sqlstr += "FEE_NO = '" + fee_no + "' AND VS_ITEM IN ('gc','ms') GROUP BY VS_ID) WHERE ROWNUM < 4";
                }
                else
                {
                    sqlstr = "SELECT VS_ID FROM DATA_VITALSIGN WHERE FEE_NO = '" + fee_no + "' ";
                    sqlstr += "AND VS_ITEM IN ('gc','ms') GROUP BY VS_ID ";
                }
                DataTable Dt = link.DBExecSQL(sqlstr);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        vsId.Add(Dt.Rows[i]["vs_id"].ToString().Trim());
                    }
                }

                // 開始處理資料
                for (int i = 0; i <= vsId.Count - 1; i++)
                {
                    //初始化資料
                    vsdl = new VitalSignDataList();

                    sqlstr = " select vsd.*, to_char(modify_date,'yyyy/MM/dd hh24:mi:ss') as m_date ";
                    sqlstr += " from data_vitalsign vsd ";
                    sqlstr += " where fee_no ='" + fee_no + "' and vs_id = '" + vsId[i] + "' and VS_ITEM IN ('gc','ms')";

                    vsdl.vsid = vsId[i];
                    Dt = link.DBExecSQL(sqlstr);
                    if (Dt.Rows.Count > 0)
                    {
                        for (int j = 0; j < Dt.Rows.Count; j++)
                        {
                            vsdl.DataList.Add(new VitalSignData(
                                Dt.Rows[j]["vs_item"].ToString().Trim(),
                                Dt.Rows[j]["vs_part"].ToString().Trim(),
                                Dt.Rows[j]["vs_record"].ToString().Trim(),
                                Dt.Rows[j]["vs_reason"].ToString().Trim(),
                                Dt.Rows[j]["vs_memo"].ToString().Trim(),
                                Dt.Rows[j]["vs_other_memo"].ToString().Trim(),
                                "", "", "",
                                Dt.Rows[j]["m_date"].ToString().Trim()
                                ));
                        }
                    }
                    //link.DBExecSQL(sqlstr, ref reader);
                    //while (reader.Read())
                    //{
                    //    vsdl.DataList.Add(new VitalSignData(
                    //        reader["vs_item"].ToString().Trim(),
                    //        reader["vs_part"].ToString().Trim(),
                    //        reader["vs_record"].ToString().Trim(),
                    //        reader["vs_reason"].ToString().Trim(),
                    //        reader["vs_memo"].ToString().Trim(),
                    //        reader["vs_other_memo"].ToString().Trim(),
                    //        "", "", "",
                    //        reader["m_date"].ToString().Trim()
                    //        ));
                    //}
                    GCSList.Add(vsdl);
                    vsdl = null;
                }

                ViewData["GCSList"] = GCSList;
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
        /// <summary> 取得輸入/輸出 </summary>
        public void get_TransferDuty_Item_io(string fee_no)
        {
            try
            {
                string sql = "SELECT IO.*, TUBE.COLORID, TUBE.COLOROTHER, TUBE.NATUREID, TUBE.NATUREOTHER, TUBE.TASTEID, TUBE.TASTEOTHER ";
                sql += ",(SELECT P_GROUP FROM SYS_PARAMS WHERE P_MODEL = 'iotype' AND P_VALUE = IO.TYPEID)P_GROUP ";
                sql += ",(SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'iotype' AND P_VALUE = IO.TYPEID)P_NAME ";
                sql += ",(SELECT NAME FROM IO_ITEM WHERE IO_ITEM.ITEMID = IO.ITEMID)NAME ";
                sql += ",(SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'io' AND P_GROUP = 'outputcolor_Drainage' AND P_VALUE = TUBE.COLORID)COLORNAME ";
                sql += ",(SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'io' AND P_GROUP = 'outputnature_Drainage' AND P_VALUE = TUBE.NATUREID)NATURENAME ";
                sql += ",(SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'io' AND P_GROUP = 'outputtaste_Drainage' AND P_VALUE = TUBE.TASTEID)TASTENAME ";
                sql += "FROM IO_DATA IO LEFT JOIN TUBE_FEATURE TUBE ON IO.IO_ID = TUBE.FEATUREID WHERE 0 = 0 ";
                sql += "AND FEENO = '" + fee_no + "' ";
                sql += "AND CREATTIME = (SELECT MAX(CREATTIME) FROM IO_DATA WHERE FEENO =  '" + fee_no + "' ) ORDER BY TYPEID,NAME ";

                DataTable dt = new DataTable();
                this.link.DBExecSQL(sql, ref dt);

                ViewBag.io_data = dt;
                ViewBag.tube_name = new Func<string, string>(sel_item_name);
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
        /// <summary> 取得管路 </summary>
        public List<List<string>> get_TransferDuty_Item_tube(string fee_no, string flag)
        {
            List<List<string>> tube = new List<List<string>>();
            try
            {
                string sqlstr = "SELECT TO_CHAR(A.STARTTIME,'YYYY/MM/DD HH24:MI') STARTTIME,TO_CHAR(A.FORECAST,'YYYY/MM/DD HH24:MI') FORECAST,A.MODEL,A.LENGTH,A.CARERECORD,B.NUMBERID,B.NUMBEROTHER,B.MATERIALID, ";
                sqlstr += "(SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'tube' AND P_GROUP = 'tubeMaterial' AND P_VALUE = B.MATERIALID) MATERIAL_NAME,";
                sqlstr += "(SELECT P_NAME FROM SYS_PARAMS WHERE P_MODEL = 'tube' AND P_GROUP = 'tubeLengthUnit' AND P_VALUE = A.LENGTHUNIT) LENGTHUNIT_NAME,";
                sqlstr += "(SELECT KINDNAME FROM TUBE_KIND WHERE KINDID = A.TYPEID) TYPE_NAME,B.MATERIALOTHER ";
                sqlstr += "FROM TUBE A INNER JOIN TUBE_FEATURE B ON A.TUBEID = B.FEATUREID ";
                sqlstr += " WHERE FEENO = '" + fee_no + "' AND ( ENDTIME >= SYSDATE OR ENDTIME IS NULL) AND DELETED IS NULL ORDER BY TUBEROW ASC";

                DataTable Dt = link.DBExecSQL(sqlstr);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        string tmpstr2 = "";
                        string forecast = (Dt.Rows[i]["FORECAST"].ToString() == "") ? "" : Convert.ToDateTime(Dt.Rows[i]["FORECAST"]).ToString("yyyy/MM/dd");
                        if (Dt.Rows[i]["MATERIALID"].ToString() != "99")
                        {
                            tmpstr2 = Dt.Rows[i]["MATERIAL_NAME"].ToString().Trim();
                        }
                        else
                        {
                            tmpstr2 = Dt.Rows[i]["MATERIALOTHER"].ToString().Trim();
                        }
                        tube.Add(new List<string>() { Dt.Rows[i]["TYPE_NAME"].ToString().Trim(),
                                                    tmpstr2,
                                                    Dt.Rows[i]["MODEL"].ToString().Trim(),
                                                    Dt.Rows[i]["LENGTH"].ToString().Trim() + Dt.Rows[i]["LENGTHUNIT_NAME"].ToString().Trim(),
                                                    Dt.Rows[i]["STARTTIME"].ToString().Trim(),
                                                    forecast});
                    }
                }

                return tube;
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return tube;
            }
            finally
            {
                this.link.DBClose();
            }
        }
        /// <summary> 取得評估結果 </summary>
        public List<List<string>> get_TransferDuty_Item_assess(string fee_no, string flag)
        {
            List<List<string>> assess = new List<List<string>>();
            try
            {
                string sqlstr = "SELECT '跌倒傾向評估' TITLE,'跌倒高危'||TOTAL||'分' SCORE,TO_CHAR(CREATTIME,'YYYY/MM/DD HH24:MI') CREAT_DATE ";
                sqlstr += " FROM NIS_FALL_ASSESS_DATA WHERE FEENO = '" + fee_no + "'";
                sqlstr += " AND CREATTIME = (SELECT MAX(CREATTIME) FROM NIS_FALL_ASSESS_DATA WHERE FEENO = '" + fee_no + "') UNION ";
                sqlstr += " SELECT '兒童跌倒傾向評估' TITLE,'跌倒高危'||TOTAL||'分' SCORE,TO_CHAR(CREATTIME,'YYYY/MM/DD HH24:MI') CREAT_DATE";
                sqlstr += " FROM NIS_FALL_ASSESS_DATA_CHILD WHERE FEENO = '" + fee_no + "'";
                sqlstr += " AND CREATTIME = (SELECT MAX(CREATTIME) FROM NIS_FALL_ASSESS_DATA_CHILD WHERE FEENO = '" + fee_no + "') UNION ";
                sqlstr += " SELECT '壓傷危險因子評估' TITLE,'高度危險性'||TOTAL||'分' SCORE,TO_CHAR(CREATTIME,'YYYY/MM/DD HH24:MI') CREAT_DATE";
                sqlstr += " FROM NIS_PRESSURE_SORE_DATA WHERE FEENO = '" + fee_no + "'";
                sqlstr += " AND CREATTIME = (SELECT MAX(CREATTIME) FROM NIS_PRESSURE_SORE_DATA WHERE FEENO = '" + fee_no + "') UNION ";
                sqlstr += " SELECT '疼痛評估('||POSITION||')' TITLE,'疼痛強度' || STRENGTH || '分' SCORE,TO_CHAR(TO_DATE(B.INSDT, 'YYYY/MM/DD HH24:MI:SS'),'YYYY/MM/DD HH24:MI') CREAT_DATE";
                sqlstr += " FROM (SELECT ID,POSITION FROM PAINCONTINUED WHERE FEENO = '" + fee_no + "' AND STATUS <> '結案' GROUP BY ID,POSITION) A,";
                sqlstr += " PAINCONTINUEDSAVE B WHERE A.ID = B.ID AND B.INSDT = (SELECT MAX(INSDT) FROM PAINCONTINUEDSAVE WHERE ID = B.ID)";
                sqlstr += " AND TO_CHAR(TO_DATE(B.INSDT,'YYYY/MM/DD HH24:MI:SS'),'YYYY/MM/DD HH24MI') > TO_CHAR(SYSDATE - 9/24,'YYYY/MM/DD HH24MI')";

                DataTable Dt = link.DBExecSQL(sqlstr);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        assess.Add(new List<string>() { Dt.Rows[i]["CREAT_DATE"].ToString().Trim(),
                                                        Dt.Rows[i]["TITLE"].ToString().Trim(),
                                                        Dt.Rows[i]["SCORE"].ToString().Trim() });
                    }
                }

                return assess;
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                return assess;
            }
            finally
            {
                this.link.DBClose();
            }
        }
        /// <summary> 取得檢驗_八小時內 </summary>
        public List<Lab> get_TransferDuty_Item_lab_main(string fee_no, string flag)
        {
            List<Lab> labList = new List<Lab>();
            byte[] labByte = webService.GetLab8HR(fee_no);
            if (labByte != null)
            {
                string labJson = CompressTool.DecompressString(labByte);
                labList = JsonConvert.DeserializeObject<List<Lab>>(labJson);
            }
            return labList;
        }
        /// <summary> 取得檢驗 </summary>
        public void get_TransferDuty_Item_lab(string fee_no, string flag)
        {
            List<Lab> labList = new List<Lab>();
            byte[] labByte = webService.GetLab(fee_no);
            if (labByte != null)
            {
                string labJson = CompressTool.DecompressString(labByte);
                labList = JsonConvert.DeserializeObject<List<Lab>>(labJson);
            }
            ViewData["lab"] = labList;
        }
        /// <summary> 取得檢查_八小時內 </summary>
        public List<Exam> get_TransferDuty_Item_exam_main(string fee_no, string flag)
        {
            List<Exam> examList = new List<Exam>();
            byte[] examByte = webService.GetExam8HR(fee_no);
            if (examByte != null)
            {
                string examJson = CompressTool.DecompressString(examByte);
                examList = JsonConvert.DeserializeObject<List<Exam>>(examJson);
            }
            return examList;
        }
        /// <summary> 取得檢查 </summary>
        public void get_TransferDuty_Item_exam(string fee_no, string flag)
        {
            List<Exam> examList = new List<Exam>();
            byte[] examByte = webService.GetExam(fee_no);
            if (examByte != null)
            {
                string examJson = CompressTool.DecompressString(examByte);
                examList = JsonConvert.DeserializeObject<List<Exam>>(examJson);
            }
            ViewData["exam"] = examList;
        }
        /// <summary> 取得會診 </summary>
        public List<Consultation> get_TransferDuty_Item_Consultation(string fee_no, string flag)
        {
            List<Consultation> ConsultationList = new List<Consultation>();
            byte[] ConsultationByte = webService.GetPatientConsult(fee_no);
            if (ConsultationByte != null)
            {
                string ConsultationJson = CompressTool.DecompressString(ConsultationByte);
                ConsultationList = JsonConvert.DeserializeObject<List<Consultation>>(ConsultationJson);
            }

            string orderdt = "";
            string content = "";
            List<Consultation> PTList2 = new List<Consultation>();
            for (int j = 0; j < ConsultationList.Count; j++)
            {
                //日期如果不等於該日期時
                if (orderdt != ConsultationList[j].OrderDate.ToString("yyyy/MM/dd HH:mm:ss").Trim())
                {
                    PTList2.Add(new Consultation()
                    {
                        OrderDate = ConsultationList[j].OrderDate,
                        ConsDoc = ConsultationList[j].ConsDoc,
                        ConsDept = ConsultationList[j].ConsDept,
                        ConsContent = ConsultationList[j].ConsContent,
                        OrderNo = ConsultationList[j].OrderNo,
                    });
                    orderdt = ConsultationList[j].OrderDate.ToString("yyyy/MM/dd HH:mm:ss").Trim();
                    content = "";
                }
                else//同一筆 內容需相加
                {
                    content = PTList2[PTList2.Count - 1].ConsContent.Trim() + " " + ConsultationList[j].ConsContent.ToString().Trim();
                    PTList2.RemoveAt(PTList2.Count - 1);
                    PTList2.Add(new Consultation()
                    {
                        OrderDate = ConsultationList[j].OrderDate,
                        ConsDoc = ConsultationList[j].ConsDoc,
                        ConsDept = ConsultationList[j].ConsDept,
                        ConsContent = content,
                        OrderNo = ConsultationList[j].OrderNo,
                    });
                    orderdt = ConsultationList[j].OrderDate.ToString("yyyy/MM/dd HH:mm:ss").Trim();
                    content = ConsultationList[j].ConsContent;
                }
            }

            return PTList2;
        }
        /// <summary> 取得血庫 </summary>
        public void get_TransferDuty_Item_blood(string fee_no, string flag)
        {
            List<BloodInfo> blood = new List<BloodInfo>();
            byte[] ptByte = webService.GetBloodInfo(fee_no);
            if (ptByte != null)
            {
                string ptJson = CompressTool.DecompressString(ptByte);
                blood = JsonConvert.DeserializeObject<List<BloodInfo>>(ptJson);
            }
            ViewData["blood"] = blood;
        }
        /// <summary> 取得手術 </summary>
        public void get_TransferDuty_Item_op(string fee_no, string flag)
        {
            List<Surgery> oper = new List<Surgery>();
            byte[] ptByte = webService.GetOpInfo(fee_no);
            if (ptByte != null)
            {
                string ptJson = CompressTool.DecompressString(ptByte);
                oper = JsonConvert.DeserializeObject<List<Surgery>>(ptJson);
            }
            ViewData["oper"] = oper;
        }
        /// <summary> 取得備註 </summary>
        public void get_memo(string fee_no)
        {
            try
            {
                string sqlstr = " select * from DATA_TRANS_MEMO where fee_no = '" + fee_no + "' AND TRANS_FLAG != 'M' ";
                DataTable dt = new DataTable();
                this.link.DBExecSQL(sqlstr, ref dt);
                ViewBag.dt_memo = dt;
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
        /// <summary> 取得特殊交班 </summary>
        public void get_TransferDuty_Item_spec(string feeno)
        {
            List<List<string>> speical_info = new List<List<string>>();
            try
            {
                string sqlstr = " SELECT TRANS_MEMO, MODIFY_USER, TO_CHAR(MODIFY_DATE,'YYYY/MM/DD HH24:MI:SS') MODIFY_DATE ";
                sqlstr += " FROM DATA_TRANS_MEMO WHERE FEE_NO = '" + feeno + "' AND TRANS_FLAG='M'";
                DataTable Dt = link.DBExecSQL(sqlstr);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        speical_info.Add(new List<string>(){
                            Dt.Rows[i]["TRANS_MEMO"].ToString().Trim(),
                            Dt.Rows[i]["MODIFY_DATE"].ToString().Trim(),
                            Dt.Rows[i]["MODIFY_USER"].ToString().Trim()
                        });
                    }
                }

                ViewData["speical_info"] = speical_info;
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
                ViewData["speical_info"] = speical_info;
            }
            finally
            {
                this.link.DBClose();
            }
        }
        /// <summary> 未定義 </summary>
        public void get_TransferDuty_Item_call(string fee_no, string flag, ref List<VitalSignDataList> vsList)
        {
            //wait web service
        }
        /// <summary> 未定義 </summary>
        public void get_TransferDuty_Item_nur(string fee_no, string flag, ref List<VitalSignDataList> vsList)
        {
            //wait web service
        }

        /// <summary>取得TeamCare_List </summary>
        /// <param name="feeno">查詢的病歷號</param>
        /// <param name="date">當天往前推兩天的日期，格式為yyyy/mm/dd</param>
        /// <param name="ErrMsg">記錄的錯誤訊息</param>
        private DataTable Sel_TeamCare(string feeno, string date)
        {
            DataTable dt = new DataTable();
            if (!debug_mode)
            {
                try
                {
                    //DateTime RecordDate = Convert.ToDateTime(date);
                    //string sql = "Select a.RefPatient, (a.RecordDate + ' ' + a.RecordTime) as RecordDateTime, ";
                    //sql += "a.RecordContent, b.TeamGroupName as deptName ";
                    //sql += "from tcRecord as a  Left Join tcTeamGroup as b ";
                    //sql += "On a.TeamGroupID = b.TeamGroupID Where 0 = 0 ";
                    //sql += "AND RefPatient = '" + feeno + "'  ";
                    //sql += "AND a.RecordDate >= '" + RecordDate.ToString("yyyy/MM/dd") + "' ";
                    //sql += " Order By (a.recordDate + a.recordTime) desc ";

                    //string TC_connStr = System.Configuration.ConfigurationManager.AppSettings["TeamCareConnectionString"].ToString();
                    //SqlConnection conn = new SqlConnection(TC_connStr);
                    //conn.Open();
                    //SqlCommand cmd = new SqlCommand(sql, conn);
                    //SqlDataReader dr = cmd.ExecuteReader();
                    //dt.Load(dr);
                    //cmd.Cancel();
                    //dr.Close();
                    //conn.Close();
                    //conn.Dispose();
                }
                catch (Exception)
                {
                }
            }

            return dt;
        }

        private string sel_item_name(string item_id)
        {
            if (item_id != "")
            {
                DataTable dt = tubem.sel_tube("", "", item_id, "", "N");
                string content = "";
                if (dt.Rows.Count > 0)
                {
                    content = dt.Rows[0]["TYPE_NAME"].ToString() + dt.Rows[0]["POSITION"].ToString() + dt.Rows[0]["LOCATION_NAME"].ToString();
                    if (dt.Rows[0]["NUMBERID"].ToString() != "99")
                        content += "#" + dt.Rows[0]["NUBER_NAME"].ToString();
                    else
                        content += "#" + dt.Rows[0]["NUMBEROTHER"].ToString();
                }

                return content;
            }
            else
                return "";
        }

        #endregion

        public ActionResult InHistory()
        {
            if (Request["chr_no"] != null)
            {
                try
                {
                    //取得UserInfo
                    UserInfo ui = (UserInfo)Session["UserInfo"];
                    string _MBD_DATE = "";
                    Dictionary<string, string> DataZones = null;
                    // 取得住院歷史資料
                    byte[] inHistoryByte = webService.GetInHistory(Request["chr_no"].ToString().Trim());
                    if (inHistoryByte != null)
                    {
                        string inHistoryJson = CompressTool.DecompressString(inHistoryByte);
                        List<InHistory> inHistoryList = JsonConvert.DeserializeObject<List<InHistory>>(inHistoryJson);

                        //線上歷史區
                        string str_feeno = "";
                        DataZones = get_DataZone(Request["chr_no"].ToString().Trim());

                        //只有一次住院 而且住很久都沒出院
                        if (DataZones == null)
                        {
                            DataZones = new Dictionary<string, string>();
                            str_feeno = inHistoryList[0].FeeNo;
                            DataZones[str_feeno] = "CS";
                        }
                        foreach (var item in inHistoryList)
                        {
                            _MBD_DATE = Convert.ToDateTime(item.outdate).ToString("yyyyMMdd").ToString();

                            if (_MBD_DATE == "00010101")
                                item.DataZone = "CS1";
                            else
                            {
                                if (true == (DataZones.ContainsKey(item.FeeNo)))
                                    item.DataZone = DataZones[item.FeeNo];
                                else
                                    item.DataZone = "CS";
                            }

                            //排除 錯誤的出院日期資料( 在院病人的出院日期是空的 + 假的出院日期無法儲存入DB = 0001/01/01)
                            if (item.outdate != null && Convert.ToDateTime(item.outdate).ToString("yyyy").ToString() != "0001")
                            {
                                INSERT_MBD_PROFILE(item.FeeNo, item.outdate, item.ChrNo); // 將 WEBSERVICE 的出院病人資料塞入 PATIENT_PROFILE

                                if (item.DataZone == "CS1")
                                {
                                    str_feeno += "'" + item.FeeNo + "',";
                                }
                            }
                        }
                        Session["inHistory"] = inHistoryList;
                        ViewData["inHistory"] = inHistoryList;
                    }
                    else
                    {
                        Response.Write("<script>alert('查無此病歷號[" + Request["chr_no"] + "]，請重新確認。');window.close();</script>");
                        return new EmptyResult();
                    }
                }
                catch (Exception)
                {
                    //Do nothing
                }
            }
            if (Request["flag"] == "view")
                ViewBag.flag = "view";
            else if (Request["flag"] == "c")
                ViewBag.flag = "close";
            else
                ViewBag.flag = "open";
            return View();
        }


        // 出院三天內病人
        public ActionResult Discharged()
        {
            if (Request["station"] != null)
            {
                byte[] GetOutByteCode = webService.GetOut3Days(Request["station"].ToString().Trim());
                if (GetOutByteCode != null)
                {
                    string GetOutJsonArr = CompressTool.DecompressString(GetOutByteCode);
                    List<PatientInfo> GetOutList = JsonConvert.DeserializeObject<List<PatientInfo>>(GetOutJsonArr);
                    ViewData["GetOutList"] = GetOutList;
                }
            }
            if (Request["flag"] == "view")
            { ViewBag.flag = "view"; }
            else if (Request["flag"] == "c")
            { ViewBag.flag = "close"; }
            else
            { ViewBag.flag = "open"; }
            return View();
        }

        /// <summary>
        /// //取得員工姓名
        /// </summary>
        /// <param name="userno">傳入使用者代碼</param>
        /// <returns>回傳姓名，或是空值</returns>
        public string GetEmpName(string userno)
        {
            byte[] listByteCode = webService.UserName(userno);
            string UserName = "";
            if (listByteCode != null)
            {
                string listJsonArray = CompressTool.DecompressString(listByteCode);
                UserInfo user_name = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                UserName = user_name.EmployeesName;
            }
            return UserName;
        }
    }
}
