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
using System.IO;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;

namespace NIS.Controllers
{
    public class CFSController : BaseController
    {
        // GET: /Delirium/
        private DBConnector link;
        private CFS cfs;
        //患者譫妄評估
        public CFSController()
        {
            this.cfs = new CFS();
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
            string StrSql = "SELECT * FROM CFS_DATA WHERE FEE_NO='" + feeno + "' ";
            StrSql += "AND ASSESS_DT BETWEEN TO_DATE('" + Starttime + " 00:00:00','yyyy/mm/dd hh24:mi:ss') ";
            StrSql += "AND  TO_DATE('" + endtime + " 23:59:59','yyyy/mm/dd hh24:mi:ss') ";
            StrSql += "AND DEL_USER IS NULL AND STATUS = 'Y' ORDER BY  ASSESS_DT DESC, CREATE_DATE DESC";
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
                DataTable dt = this.link.DBExecSQL("SELECT * FROM CFS_DATA WHERE FEE_NO='" + feeno + "' AND ASSESS_ID='" + id + "'");
                ViewBag.dt = dt;
            }
            return View();
        }
        public byte[] ConvertFileToByte(HttpPostedFileBase inputFile)
        {
            byte[] result = null;
            using (MemoryStream target = new MemoryStream())
            {
                inputFile.InputStream.CopyTo(target);
                result = target.ToArray();
            }
            return result;
            //return Convert.ToBase64String(target.ToArray());
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult CFS_Save(FormCollection form, List<HttpPostedFileBase> clock_file)
        {
            string[] StrMoodScore = { "是", "否", "無法評估" };
            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;
            string date = form["txt_day"] + " " + form["txt_time"];
            string id = creatid("CFS_DATA", userno, feeno, "0");
            string type = "新增";
            int erow = '0';
            string msg = "";
            byte[] imageBytes = null;
            if (form["id"] != "" && form["id"] != null)
                id = form["id"];
            #region 新增
            #region 護理紀錄
            msg += "評核原因：" + form["rb_assessment_reason"];

            #endregion //護理紀錄
            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("ASSESS_DT", date, DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("CFS_SCORE", form["param_total_score"], DBItem.DBDataType.String));
            if (form["param_total_score_mc"] != null)
            {
                insertDataList.Add(new DBItem("MC_SCORE", form["param_total_score_mc"], DBItem.DBDataType.String));
            }
            if (form["rb_draw_clock"] != null)
            {
                insertDataList.Add(new DBItem("MC_SCORE2", form["rb_draw_clock"], DBItem.DBDataType.String));
            }
            if (form["rb_clock"] != null)
            {
                insertDataList.Add(new DBItem("MC_SCORE3", form["rb_clock"], DBItem.DBDataType.String));
            }
            insertDataList.Add(new DBItem("MC_SCORE1", form["param_total_result_mc"], DBItem.DBDataType.String));
            if (form["cog_ck_result"] != null)
            {
                insertDataList.Add(new DBItem("COG_RESULT", form["cog_ck_result"], DBItem.DBDataType.String));
            }
            else
            {
                insertDataList.Add(new DBItem("COG_RESULT", "", DBItem.DBDataType.String));
            }

            
            if (form["span_cog_Y_other"] != null)
            {
                insertDataList.Add(new DBItem("COG_RESULT_OTHER", form["span_cog_Y_other"], DBItem.DBDataType.String));
            }
     
            insertDataList.Add(new DBItem("DEL_USER", "", DBItem.DBDataType.String)); 

            #endregion //新增
            if (form["id"] != "" && form["id"] != null)
            { //修改
                var careRecordId = "";
                DataTable dt = new DataTable();
                var sql = "SELECT * FROM CFS_DATA WHERE ASSESS_ID ='" + id + "' AND STATUS = 'Y'";
                this.link.DBExecSQL(sql, ref dt);
                if (dt.Rows.Count > 0)
                {
                    careRecordId = dt.Rows[0]["CARERECORD_ID"].ToString();
                }
                int scoreCheck = int.Parse(form["param_total_score"]);
                if (clock_file[0] == null && form["uploadJPG"].ToString() != "N")
                {
                    if(scoreCheck  >3 && scoreCheck <7)
                    {
                        if (form["cog_ck_result"] == null)
                        {
                            DataTable dtck = new DataTable();
                            string sqlck = "";
                            sqlck = "SELECT CLOCK_FILE FROM CFS_DATA WHERE ASSESS_ID ='" + id + "' AND STATUS = 'Y'";
                            this.link.DBExecSQL(sql, ref dtck);
                            if (dtck.Rows.Count > 0)
                            {
                                var CLOCK = dtck.Rows[0]["CLOCK_FILE"].ToString();
                                if (CLOCK != "")
                                {
                                    imageBytes = (byte[])dtck.Rows[0]["CLOCK_FILE"];
                                }

                            }
                        }
                    }

                }
                insertDataList.Clear();
                type = "修改";
                insertDataList.Add(new DBItem("MODIFY_USER", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MODIFY_USER_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MODIFY_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("STATUS", "M", DBItem.DBDataType.String));
                string where = "ASSESS_ID='" + id + "'";
                erow = this.link.DBExecUpdate("CFS_DATA", insertDataList, where);

                msg = "";
                //衰弱評估 拋轉護理紀錄
                if (form["param_total_score"] == null)
                {
                    msg = "";
                    if (ptinfo.Age < 65)
                    {
                        msg += "病人年齡<65歲";

                    }
                    else
                    {
                        msg += "病人年齡≧65歲";
                    }
                    msg += "，衰弱評估為無此需要，續追蹤。";
                }
                else
                {
                    msg = "";
                    var score = form["param_total_score"];
                    var status = "";
                    var status_note = "";

                    switch (score)
                    {
                        case "1":
                            status = "非常健康";
                            status_note = "無衰弱，鼓勵參與社區健康促進活動";
                            break;
                        case "2":
                            status = "健康";
                            status_note = "無衰弱，鼓勵參與社區健康促進活動";
                            break;
                        case "3":
                            status = "維持良好";
                            status_note = "無衰弱，鼓勵參與社區健康促進活動";
                            break;
                        case "4":
                            status = "脆弱較易受傷害";
                            status_note = "衰弱前期，通知醫療科照會各職類";
                            break;
                        case "5":
                            status = "輕度衰弱";
                            status_note = "衰弱前期，通知醫療科照會各職類";
                            break;
                        case "6":
                            status = "中度衰弱	";
                            status_note = "衰弱前期，通知醫療科照會各職類";
                            break;
                        case "7":
                            status = "嚴重衰弱	";
                            status_note = "衰弱期，通知醫療科照會出院準備";
                            break;
                        case "8":
                            status = "非常嚴重衰弱	";
                            status_note = "	衰弱期，通知醫療科照會出院準備";
                            break;
                        case "9":
                            status = "末期	";
                            status_note = "衰弱期，通知醫療科照會出院準備";
                            break;
                    }
                    if (ptinfo.Age < 65)
                    {
                        msg += "病人年齡<65歲";

                    }
                    else
                    {
                        msg += "病人年齡≧65歲";
                    }
                    msg += "，衰弱評估健康狀態：" + status + " ，";

                    msg += "分數：";
                    if (form["param_total_score"] != null)
                    {
                        msg += form["param_total_score"];
                    }
                    msg += "，";
                    msg += "結果：" + status_note + "，續追蹤。";
                }
                erow = base.Upd_CareRecord(date, careRecordId, "衰弱評估", msg, "", "", "", "", "CFS");

                //失智評估
                msg = "";
                if (form["param_total_score_mc"] == null || form["param_total_score_mc"] == " ")
                {
                    if (form["cog_ck_result"] != null)
                    {
                        msg += "病人Mini-Cog失智評估為無法評估，原因：";
                        var result = form["cog_ck_result"].Split(',');
                        var resultOther = "";
                        for (int i = 0; i < result.Count(); i++)
                        {
                            if (result[i] != "其他")
                            {
                                resultOther += result[i];
                            }
                            if (i < result.Count() - 1)
                            {
                                resultOther += ",";
                            }
                        }
                        msg += resultOther;
                    }
                    if (form["span_cog_Y_other"] != null)
                    {
                        msg += form["span_cog_Y_other"];
                    }
                    if(msg != "")
                    {
                        msg += "，續追蹤。";

                    }
                }
                else
                {
                    //1.病人可依引導於畫鐘後，正確覆誦"紅色、快樂、腳踏車"
                    msg = "";
                    var score = form["param_total_score_mc"];
                    var total = int.Parse(form["param_total_score_mc"]);
                    msg += "病人Mini-Cog失智評估為衰弱前期評估，";
                    msg += "評估項目：";
                    msg += "1.病人可依引導於畫鐘後，正確覆誦";
                    msg += '"';
                    msg += "紅色、快樂、腳踏車";
                    msg += '"';
                    msg += "：病人回答";

                    var score1 = 0;
                    if (form["param_total_result_mc"] != null && form["param_total_result_mc"] != "皆無回答")
                    {
                        msg += form["param_total_result_mc"];
                        score1 = form["param_total_result_mc"].Split(',').Count();
                    }
                    else
                    {
                        msg += "不正確";
                        score1 = 0;
                    }
                    msg += "，";

                    msg += "分數：" + score1 + "分、";


                    //2.病人可自行完成畫鐘數字及順序正確
                    msg += "2.病人可自行完成畫鐘數字及順序正確：";
                    var score2 = 0;
                    if (form["rb_draw_clock"] != null)
                    {
                        if (form["rb_draw_clock"] == "0")
                        {
                            msg += "是";
                            score2 = 1;
                        }
                        else
                        {
                            msg += "否";
                            score2 = 0;
                        }
                    }
                    msg += "，";
                    msg += "分數：" + score2.ToString() + "分、";

                    //3.病人可於畫鐘上畫上指針正確指向11:10
                    msg += "3.病人可於畫鐘上畫上指針正確指向11:10：";
                    var score3 = 0;

                    if (form["rb_clock"] != null)
                    {
                        if (form["rb_clock"] == "0")
                        {
                            msg += "是";
                            score3 = 1;
                        }
                        else
                        {
                            msg += "否";
                            score3 = 0;
                        }
                    }
                    msg += "，";
                    msg += "分數：" + score3.ToString() + "分，";

                    //總分
                    msg += "總分：" + total + "分";
                    if (total <= 2)
                    {
                        msg += "，提醒醫療科進一步評估。";
                    }
                    else
                    {
                        msg += "。";
                    }

                }
                if(msg != "")
                {
                    string resultScore = form["param_total_score"].ToString();
                    int score = int.Parse(resultScore);

                    if(score > 3 && score < 7)
                    {
                        int cogresult = base.Upd_CareRecord(date, careRecordId, "Mini-Cog失智評估", msg, "", "", "", "", "MINICOG");
                        DataTable dtrc = new DataTable();
                        sql = "";
                        sql = "SELECT * FROM CARERECORD_DATA WHERE CARERECORD_ID ='" + id + "' AND SELF = 'MINICOG' AND DELETED IS NULL";
                        this.link.DBExecSQL(sql, ref dt);
                        if (dtrc.Rows.Count < 1)
                        {
                            cogresult = 0;
                        }

                        if (cogresult == 0)
                        {
                            erow += base.Del_CareRecord(careRecordId, "MINICOG");
                            base.Insert_CareRecord(date, careRecordId, "Mini-Cog失智評估", msg, "", "", "", "", "MINICOG");
                        }
                    }
                    else
                    {
                        erow += base.Del_CareRecord(careRecordId, "MINICOG");
                    }
                }
                else
                {
                    string resultScore = form["param_total_score"].ToString();
                    int score = int.Parse(resultScore);
                    if (score < 4 || score > 6)
                    {
                        erow += base.Del_CareRecord(careRecordId, "MINICOG");
                    }
                }
                
                id = creatid("CFS_DATA", userno, feeno, "0");
                insertDataList.Clear();

                insertDataList.Add(new DBItem("ASSESS_ID", id, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CARERECORD_ID", careRecordId, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FEE_NO", feeno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATE_USER", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATE_USER_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATE_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MODIFY_USER", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MODIFY_USER_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MODIFY_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASSESS_DT", date, DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("CFS_SCORE", form["param_total_score"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("BED_NO", ptinfo.BedNo, DBItem.DBDataType.String));
                if (form["param_total_score_mc"] != null)
                {
                    insertDataList.Add(new DBItem("MC_SCORE", form["param_total_score_mc"], DBItem.DBDataType.String));
                }
                if (form["rb_draw_clock"] != null)
                {
                    insertDataList.Add(new DBItem("MC_SCORE2", form["rb_draw_clock"], DBItem.DBDataType.String));
                }
                if (form["rb_clock"] != null)
                {
                    insertDataList.Add(new DBItem("MC_SCORE3", form["rb_clock"], DBItem.DBDataType.String));
                }
                insertDataList.Add(new DBItem("MC_SCORE1", form["param_total_result_mc"], DBItem.DBDataType.String));
                if (form["cog_ck_result"] != null)
                {
                    insertDataList.Add(new DBItem("COG_RESULT", form["cog_ck_result"], DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("COG_RESULT", "", DBItem.DBDataType.String));
                }


                if (form["span_cog_Y_other"] != null)
                {
                    insertDataList.Add(new DBItem("COG_RESULT_OTHER", form["span_cog_Y_other"], DBItem.DBDataType.String));
                }

                insertDataList.Add(new DBItem("DEL_USER", "", DBItem.DBDataType.String));
                erow = this.link.DBExecInsert("CFS_DATA", insertDataList);

                if (erow > 0)
                {
                    if (clock_file[0] != null)
                    {
                        if (scoreCheck > 3 && scoreCheck < 7)
                        {
                            if (form["cog_ck_result"] == null)
                            {
                                cfs.DBCmd.CommandText = "UPDATE CFS_DATA SET  CLOCK_FILE = :CLOCK_FILE "
                                                           + " WHERE FEE_NO = '" + base.ptinfo.FeeNo + "' AND ASSESS_ID = '" + id + "' ";
                                //wound.DBCmd.Parameters.Add(":WOUND_IMG_DATA", SqlDbType.Binary).Value = ConvertFileToByte(Wound_Img[i]);
                                //wound.DBCmd.Parameters.Add(":WOUND_IMG_DATA", ConvertFileToByte(Wound_Img[i]));
                                byte[] arr = ConvertFileToByte(clock_file[0]);
                                cfs.DBCmd.Parameters.Add(":CLOCK_FILE", OracleDbType.Blob).Value = arr;

                                cfs.DBOpen();
                                cfs.DBCmd.ExecuteNonQuery();
                                cfs.DBClose();
                                arr = null;
                            }
                        }
                    }
                    else
                    {
                        cfs.DBCmd.CommandText = "UPDATE CFS_DATA SET  CLOCK_FILE = :CLOCK_FILE "
                                                          + " WHERE FEE_NO = '" + base.ptinfo.FeeNo + "' AND ASSESS_ID = '" + id + "' ";
                        //wound.DBCmd.Parameters.Add(":WOUND_IMG_DATA", SqlDbType.Binary).Value = ConvertFileToByte(Wound_Img[i]);
                        //wound.DBCmd.Parameters.Add(":WOUND_IMG_DATA", ConvertFileToByte(Wound_Img[i]));
                        cfs.DBCmd.Parameters.Add(":CLOCK_FILE", OracleDbType.Blob).Value = imageBytes;

                        cfs.DBOpen();
                        cfs.DBCmd.ExecuteNonQuery();
                        cfs.DBClose();
                        imageBytes = null;
                    }
                    
                }
            }
            else
            {  //新增              
                insertDataList.Add(new DBItem("ASSESS_ID", id, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CARERECORD_ID", id, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FEE_NO", feeno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATE_USER", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATE_USER_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATE_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MODIFY_USER", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MODIFY_USER_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MODIFY_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("STATUS", "Y", DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("BED_NO", ptinfo.BedNo, DBItem.DBDataType.String));
                erow = this.link.DBExecInsert("CFS_DATA", insertDataList);
                int scoreCheck = int.Parse(form["param_total_score"]);


                if (erow > 0)
                {
                    if (clock_file[0] != null)
                    {
                        if (scoreCheck > 3 && scoreCheck < 7)
                        {
                            if (form["cog_ck_result"] == null)
                            {
                                cfs.DBCmd.CommandText = "UPDATE CFS_DATA SET  CLOCK_FILE = :CLOCK_FILE "
                                                           + " WHERE FEE_NO = '" + base.ptinfo.FeeNo + "' AND ASSESS_ID = '" + id + "' ";
                                //wound.DBCmd.Parameters.Add(":WOUND_IMG_DATA", SqlDbType.Binary).Value = ConvertFileToByte(Wound_Img[i]);
                                //wound.DBCmd.Parameters.Add(":WOUND_IMG_DATA", ConvertFileToByte(Wound_Img[i]));
                                byte[] arr = ConvertFileToByte(clock_file[0]);
                                cfs.DBCmd.Parameters.Add(":CLOCK_FILE", OracleDbType.Blob).Value = arr;

                                cfs.DBOpen();
                                cfs.DBCmd.ExecuteNonQuery();
                                cfs.DBClose();
                                arr = null;
                            }
                        }
                    }
                }
             

                //失智評估
                msg = "";
                if (form["param_total_score_mc"] == null || form["param_total_score_mc"] == " ")
                {
                    if (form["cog_ck_result"] != null)
                    {
                        msg += "病人Mini-Cog失智評估為無法評估，原因：";
                        var result = form["cog_ck_result"].Split(',');
                        var resultOther = "";
                        for(int i = 0; i < result.Count(); i ++)
                        {
                            if(result[i] != "其他")
                            {
                                resultOther += result[i] ;
                            }
                            if(i < result.Count() - 1)
                            {
                                resultOther += ",";
                            }
                        }
                        msg += resultOther;
                    }
                    if (form["span_cog_Y_other"] != null)
                    {
                        msg += form["span_cog_Y_other"];
                    }
                    if(msg != "")
                    {
                        msg += "，續追蹤。";
                    }
                }
                else
                {
                    //1.病人可依引導於畫鐘後，正確覆誦"紅色、快樂、腳踏車"
                    var score = form["param_total_score_mc"];
                    var total = int.Parse(form["param_total_score_mc"]);
                    msg += "病人Mini-Cog 失智評估為衰弱前期評估，";
                    msg += "評估項目：";
                    msg += "1.病人可依引導於畫鐘後，正確覆誦";
                    msg += '"';
                    msg += "紅色、快樂、腳踏車";
                    msg += '"';
                    msg += "：病人回答";

                    var score1 = 0;
                    if (form["param_total_result_mc"] != null && form["param_total_result_mc"] != "皆無回答")
                    {
                        msg += form["param_total_result_mc"];
                        score1 = form["param_total_result_mc"].Split(',').Count();
                    }
                    else
                    {
                        msg += "不正確";
                        score1 = 0;
                    }
                    msg += "，";

                    msg += "分數：" + score1 + "分、";


                    //2.病人可自行完成畫鐘數字及順序正確
                    msg += "2.病人可自行完成畫鐘數字及順序正確：";
                    var score2 = 0;
                    if (form["rb_draw_clock"] != null)
                    {
                        if(form["rb_draw_clock"] == "0")
                        {
                            msg += "是";
                            score2 = 1;
                        }
                        else
                        {
                            msg += "否";
                            score2 = 0;
                        }
                    }
                    msg += "，";
                    msg += "分數："+ score2 .ToString()+ "分、";

                    //3.病人可於畫鐘上畫上指針正確指向11:10
                    msg += "3.病人可於畫鐘上畫上指針正確指向11:10：";
                    var score3 = 0;

                    if (form["rb_clock"] != null)
                    {
                        if (form["rb_clock"] == "0")
                        {
                            msg += "是";
                            score3 = 1;
                        }
                        else
                        {
                            msg += "否";
                            score3 = 0;
                        }
                    }
                    msg += "，";
                    msg += "分數：" + score3.ToString() + "分，";

                    //總分
                    msg += "總分：" + total + "分";
                    if (total <= 2)
                    {
                        msg += "，提醒醫療科進一步評估。";
                    }
                    else
                    {
                        msg += "。";
                    }

                }
                if(msg != "")
                {
                    base.Insert_CareRecord(date, id, "Mini-Cog失智評估", msg, "", "", "", "", "MINICOG");
                }

                //衰弱評估 拋轉護理紀錄
                if (form["param_total_score"] == null)
                {
                    msg = "";
                    if (ptinfo.Age < 65)
                    {
                        msg += "病人年齡<65歲";

                    }
                    else
                    {
                        msg += "病人年齡≧65歲";
                    }
                    msg += "，衰弱評估為無此需要，續追蹤。";
                }
                else
                {
                    msg = "";
                    var score = form["param_total_score"];
                    var status = "";
                    var status_note = "";
                    switch (score)
                    {
                        case "1":
                            status = "非常健康";
                            status_note = "無衰弱，鼓勵參與社區健康促進活動";
                            break;
                        case "2":
                            status = "健康";
                            status_note = "無衰弱，鼓勵參與社區健康促進活動";
                            break;
                        case "3":
                            status = "維持良好";
                            status_note = "無衰弱，鼓勵參與社區健康促進活動";
                            break;
                        case "4":
                            status = "脆弱較易受傷害";
                            status_note = "衰弱前期，通知醫療科照會各職類";
                            break;
                        case "5":
                            status = "輕度衰弱";
                            status_note = "衰弱前期，通知醫療科照會各職類";
                            break;
                        case "6":
                            status = "中度衰弱	";
                            status_note = "衰弱前期，通知醫療科照會各職類";
                            break;
                        case "7":
                            status = "嚴重衰弱	";
                            status_note = "衰弱期，通知醫療科照會出院準備";
                            break;
                        case "8":
                            status = "非常嚴重衰弱	";
                            status_note = "	衰弱期，通知醫療科照會出院準備";
                            break;
                        case "9":
                            status = "末期	";
                            status_note = "衰弱期，通知醫療科照會出院準備";
                            break;
                    }

                    if (ptinfo.Age < 65)
                    {
                        msg += "病人年齡<65歲";

                    }
                    else
                    {
                        msg += "病人年齡≧65歲";
                    }

                    msg += "，衰弱評估健康狀態：" + status + " ，";
                    msg += "分數：";
                    if (form["param_total_score"] != null)
                    {
                        msg += form["param_total_score"];
                    }
                    msg += "分，";
                    msg += "結果：" + status_note + "，續追蹤。";
                }

                base.Insert_CareRecord(date, id, "衰弱評估", msg, "", "", "", "", "CFS");
            }
            if (erow > 0)
            {
                Response.Write("<script>window.location.href='index';</script>");

            }
            //Response.Write("<script>alert('" + type + "成功!');window.location.href='index';</script>");
            else
                Response.Write("<script>alert('" + type + "失敗!');window.location.href='index';</script>");
            return new EmptyResult();
        }
        #endregion

        #region --刪除--
        [HttpPost]
        public JsonResult Del_Data(string id)
        {
            var careRecordId = "";
            DataTable dt = new DataTable();
            var sql = "SELECT * FROM CFS_DATA WHERE ASSESS_ID='" + id + "' AND STATUS = 'Y'";
            this.link.DBExecSQL(sql, ref dt);
            if (dt.Rows.Count > 0)
            {
                careRecordId = dt.Rows[0]["CARERECORD_ID"].ToString();
            }

            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;
            string where = " ASSESS_ID = '" + id + "' AND FEE_NO = '" + feeno + "' ";
            List<DBItem> DelDataList = new List<DBItem>();
            DelDataList.Add(new DBItem("DEL_USER", userno, DBItem.DBDataType.String));
            DelDataList.Add(new DBItem("MODIFY_USER", userno, DBItem.DBDataType.String));
            DelDataList.Add(new DBItem("MODIFY_USER_NAME", userinfo.EmployeesName, DBItem.DBDataType.String));
            DelDataList.Add(new DBItem("MODIFY_DATE", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.String));
            DelDataList.Add(new DBItem("STATUS", "D", DBItem.DBDataType.String));
            int erow = this.link.DBExecUpdate("CFS_DATA", DelDataList, where);
            erow += base.Del_CareRecord(careRecordId, "MINICOG");
            erow += base.Del_CareRecord(careRecordId, "CFS");

            if (erow > 0)
                return Json(1);
            else
                return Json(0);
            //return Json(1);
        }
        #endregion

        #region --列印--
        public ActionResult PrintList(string feeno, string StartDate = "", string EndDate = "")
        {
            PatientInfo pinfo = new PatientInfo();
            byte[] ByteCode = webService.GetPatientInfo(feeno);
            //病人資訊
            if (ByteCode != null)
                pinfo = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(ByteCode));
            ViewData["ptinfo"] = pinfo;
            ViewBag.StartDate = StartDate;
            ViewBag.EndDate = EndDate;
            ViewBag.FeeNo = feeno;
            ViewBag.RootDocument = GetSourceUrl();
            DataTable dt = QueryData(feeno, StartDate, EndDate);
            ViewBag.dt = dt;
            return View();
        }

        public ActionResult ShowIMG(string record_id)
        {
            string return_img = null;
            if (Session["PatInfo"] != null)
            {
                string feeno = ptinfo.FeeNo;
                DataTable dt = new DataTable();
                dt = cfs.sel_cfs_record_img(feeno, record_id);

                if (dt.Rows.Count == 1)
                {
                    byte[] imageBytes = (byte[])dt.Rows[0]["CLOCK_FILE"];
                    return_img = Convert.ToBase64String(imageBytes);
                }
                ViewBag.clock = dt;
            }
            else
            {
                Response.Write("<script>alert('登入逾時');</script>");

            }
            return View();
        }
        #endregion
        public string Partial_Record_Img(string record_id)
        {
            string return_img = null;
            if (Session["PatInfo"] != null)
            {
                string feeno = ptinfo.FeeNo;
                DataTable dt = new DataTable();
                dt = cfs.sel_cfs_record_img(feeno, record_id);

                if (dt.Rows.Count == 1)
                {
                    byte[] imageBytes = (byte[])dt.Rows[0]["CLOCK_FILE"];
                    return_img = Convert.ToBase64String(imageBytes);
                }

                return return_img;
            }
            else
            {
                Response.Write("<script>alert('登入逾時');</script>");
            }
            return "";
        }
    }
}
