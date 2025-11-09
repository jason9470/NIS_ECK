using Newtonsoft.Json;
using NIS.Data;
using NIS.Models;
using NIS.UtilTool;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Mvc;

namespace NIS.Controllers
{
    public class BabyBornController : BaseController
    {
        Obstetrics obs_m = new Obstetrics();
        private DBConnector link;
        private PdfEMRController PdfEmr;
        private HISViewController his;

        //建構式
        public BabyBornController()
        {   //建立一般資料物件
            this.link = new DBConnector();
            PdfEmr = new PdfEMRController();
            this.his = new HISViewController();
        }

        #region GetINSPTList 檢驗院所代碼對應
        public string Get_INSPTList(string INSPTNo)
        {
            byte[] INSPTListByteCode = webService.GetINSPTList();
            if (INSPTListByteCode == null)
                return "";
            else
            {
                string listJsonArray = CompressTool.DecompressString(INSPTListByteCode);
                INSPTList[] iNSPTList = JsonConvert.DeserializeObject<INSPTList[]>(listJsonArray);
                return iNSPTList.Where(x => x.INSPTNo.Trim() == INSPTNo.Trim()).FirstOrDefault()?.INSPTName.Trim();
            }
        }
        #endregion

        #region UserName取得員工姓名
        /// <summary>
        /// 呼叫WebService取得員工姓名
        /// </summary>
        /// <param name="userno"></param>
        /// <returns></returns>
        public string Get_Employee_Name(string userno)
        {
            byte[] listByteCode = webService.UserName(userno);
            if (listByteCode == null)
                return "";
            string listJsonArray = CompressTool.DecompressString(listByteCode);
            UserInfo user = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
            return user.EmployeesName;
        }
        #endregion


        #region 新生兒入院護理評估
        /// <summary>
        /// 新生兒入院護理評估
        /// </summary>
        /// <param name="mother_feeno"></param>
        /// <returns></returns>
        public ActionResult NBENTR_List(string feeno, string chartno, bool IsHisView = false)
        {
            ViewBag.IsHisView = IsHisView;

            if (IsHisView)
            {
                his.ControllerContext = ControllerContext;
                his.getSession(feeno);
            }

            if (Session["PatInfo"] != null)
            {
                if (feeno == "" || feeno == null)
                    feeno = ptinfo.FeeNo;
                DataTable dt = obs_m.sel_nbentr(feeno, "", "");
                dt.Columns.Add("UPDNAME");
                dt.Columns.Add("IS_CREATNO");
                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow r in dt.Rows)
                    {
                        if (r["UPDNO"].ToString() != "")
                        {
                            byte[] listByteCode = webService.UserName(r["UPDNO"].ToString());
                            if (listByteCode == null)
                                r["UPDNAME"] = "";
                            else
                            {
                                string listJsonArray = CompressTool.DecompressString(listByteCode);
                                UserInfo user_name = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                                r["UPDNAME"] = user_name.EmployeesName;
                            }
                        }
                        if (!IsHisView)
                        {
                            if (r["CREATNO"].ToString() == userinfo.EmployeesNo)
                                r["IS_CREATNO"] = true;
                            else
                                r["IS_CREATNO"] = false;
                        }
                        else
                        {
                            r["IS_CREATNO"] = false;
                        }
                    }
                }
                ViewBag.dt = set_dt(dt);
                if (!IsHisView)
                {
                    ViewBag.LoginEmployeesName = userinfo.EmployeesName;
                }
                ViewBag.feeno = feeno;
                ViewBag.RootDocument = GetSourceUrl();
                return View();
            }
            Response.Write("<script>alert('請重新選擇病患');</script>");
            return new EmptyResult();
        }
        #endregion

        #region 新生兒入院護理評估新增/編輯畫面
        /// <summary>
        /// 新生兒入院護理評估新增/編輯畫面
        /// </summary>
        /// <param name="mother_feeno"></param>
        /// <returns></returns>
        public ActionResult Insert_NBENTR(string mother_feeno, string IID, bool IsHisView = false)
        {
            ViewBag.IsHisView = IsHisView;

            DataTable babylink = obs_m.sel_nbcha(ptinfo.FeeNo);
            if (babylink != null && babylink.Rows.Count > 0)
            {
                mother_feeno = babylink.Rows[0]["MOM_FEE_NO"].ToString();
            }

            #region 產程監測 抗生素時間
            DataTable dtPrc = obs_m.sel_Instantly_Be_Birth(mother_feeno, "", "");
            if (dtPrc != null && dtPrc.Rows.Count > 0)
            {
                for (var a = dtPrc.Rows.Count - 1; a >= 0; a--)
                {
                    var iid = dtPrc.Rows[a]["IID"].ToString();

                    var dtlSql = $@"SELECT * FROM OBS_BPDISP 
WHERE IID = '{iid}' AND TM_MED_ANTIBIOTIC != '00000'";
                    DataTable DtBpDisp = obs_m.DBExecSQL(dtlSql);
                    if (DtBpDisp != null && DtBpDisp.Rows.Count > 0)
                    {
                        ViewBag.GBS_AB = dtPrc.Rows[a]["RECORDTIME"].ToString();
                        break;
                    }
                }
            }
            #endregion

            DataTable dt_nbentr = obs_m.sel_nbentr(ptinfo?.FeeNo, "", "");
            if (dt_nbentr != null && dt_nbentr.Rows.Count != 0)
            {
                ViewBag.dt_nbentr = dt_nbentr.Rows[0];
            }

            //新生兒出生紀錄
            DataTable dt_nb = obs_m.sel_nb("", ptinfo.ChartNo.PadLeft(10, '0'));
            if (dt_nb != null && dt_nb.Rows.Count != 0)
            {
                ViewBag.dt_nb = dt_nb.Rows[0];
            }

            DataTable dt_babylink = obs_m.sel_nbcha("", ptinfo.ChartNo.PadLeft(10, '0'));
            if (dt_babylink != null && dt_babylink.Rows.Count != 0)
            {
                ViewBag.momfeeno = dt_babylink.Rows[0]["MOM_FEE_NO"];
                mother_feeno = dt_babylink.Rows[0]["MOM_FEE_NO"].ToString();
            }

            DataTable dt_bthhis = obs_m.sel_bthhis(mother_feeno);
            if (dt_bthhis != null && dt_bthhis.Rows.Count != 0)
            {
                dt_bthhis.Columns.Add("HBSAG_INST_NAME");
                dt_bthhis.Columns.Add("HBEAG_INST_NAME");
                dt_bthhis.Columns.Add("RUBELLA_INST_NAME");
                dt_bthhis.Columns.Add("VDRL1_INST_NAME");
                dt_bthhis.Columns.Add("VDRL2_INST_NAME");
                dt_bthhis.Columns.Add("HIV_INST_NAME");
                dt_bthhis.Columns.Add("GBS_INST_NAME");

                foreach (DataRow r in dt_bthhis.Rows)
                {
                    r["HBSAG_INST_NAME"] = Get_INSPTList(r["HBSAG_INST"].ToString());
                    r["HBEAG_INST_NAME"] = Get_INSPTList(r["HBEAG_INST"].ToString());
                    r["RUBELLA_INST_NAME"] = Get_INSPTList(r["RUBELLA_INST"].ToString());
                    r["VDRL1_INST_NAME"] = Get_INSPTList(r["VDRL1_INST"].ToString());
                    r["VDRL2_INST_NAME"] = Get_INSPTList(r["VDRL2_INST"].ToString());
                    r["HIV_INST_NAME"] = Get_INSPTList(r["HIV_INST"].ToString());
                    r["GBS_INST_NAME"] = Get_INSPTList(r["GBS_INST"].ToString());
                }
                ViewBag.dt_bthhis = dt_bthhis.Rows[0];
            }

            DataTable dt_bthsta = obs_m.sel_bthsta(mother_feeno);
            if (dt_bthsta != null && dt_bthsta.Rows.Count != 0)
            {
                ViewBag.dt_bthsta = dt_bthsta.Rows[0];
            }

            List<DrugOrder> Drug_list = new List<DrugOrder>();
            byte[] labfoByteCode = webService.GetOpdMed(mother_feeno);
            if (labfoByteCode != null)
            {
                string labJosnArr = CompressTool.DecompressString(labfoByteCode);
                Drug_list = JsonConvert.DeserializeObject<List<DrugOrder>>(labJosnArr);
            }
            ViewData["Drug_list"] = Drug_list;
            DataTable dt_ASS_1F = obs_m.sel_ass_1_1f(ptinfo?.FeeNo, "NBENTR", IID, "");
            ViewBag.dt_ASS_1F = set_dt(dt_ASS_1F);
            if (IID != "" && IID != null)
            {
                DataTable dt = obs_m.sel_nbentr(ptinfo?.FeeNo, "", IID);
                dt.Columns.Add("HBSAG_INST_NAME");
                dt.Columns.Add("HBEAG_INST_NAME");
                dt.Columns.Add("RUBELLA_INST_NAME");
                dt.Columns.Add("VDRL1_INST_NAME");
                dt.Columns.Add("VDRL2_INST_NAME");
                dt.Columns.Add("HIV_INST_NAME");
                dt.Columns.Add("GBS_INST_NAME");

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow r in dt.Rows)
                    {
                        r["HBSAG_INST_NAME"] = Get_INSPTList(r["HBSAG_INST"].ToString());
                        r["HBEAG_INST_NAME"] = Get_INSPTList(r["HBEAG_INST"].ToString());
                        r["RUBELLA_INST_NAME"] = Get_INSPTList(r["RUBELLA_INST"].ToString());
                        r["VDRL1_INST_NAME"] = Get_INSPTList(r["VDRL1_INST"].ToString());
                        r["VDRL2_INST_NAME"] = Get_INSPTList(r["VDRL2_INST"].ToString());
                        r["HIV_INST_NAME"] = Get_INSPTList(r["HIV_INST"].ToString());
                        r["GBS_INST_NAME"] = Get_INSPTList(r["GBS_INST"].ToString());

                        if (!IsHisView)
                        {
                            if (r["CREATNO"].ToString() == userinfo.EmployeesNo)
                                ViewBag.CREATNO = true;
                            else
                                ViewBag.CREATNO = false;
                        }
                        else
                        {
                            ViewBag.CREATNO = false;
                        }
                    }
                }
                ViewBag.dt = set_dt(dt);
            }
            else
            {
                ViewBag.CREATNO = null;
            }
            if (!IsHisView)
            {
                ViewBag.userno = userinfo.EmployeesNo;
            }

            #region 入院護理評估_成人
            var sql = "SELECT * FROM (SELECT TABLEID, NATYPE FROM ASSESSMENTMASTER "
            + "WHERE FEENO = '" + mother_feeno + "' AND NATYPE = 'A' AND DELETED IS NULL "
            + "AND STATUS NOT IN ('TEMPORARY','DELETE') "
            + "ORDER BY CREATETIME DESC, MODIFYTIME DESC) WHERE ROWNUM <= 1 ";
            string TableId = string.Empty, NaType = string.Empty;

            DataTable Dt2 = link.DBExecSQL(sql); //上面已有Dt變數
            if (Dt2.Rows.Count > 0)
            {
                for (int i = 0; i < Dt2.Rows.Count; i++)

                {
                    TableId = Dt2.Rows[i]["TABLEID"].ToString();
                    NaType = Dt2.Rows[i]["NATYPE"].ToString();

                }
            }

            if (TableId != string.Empty && NaType != string.Empty)
            {
                var Temp = new List<Dictionary<string, string>>();
                //取得所有入評值
                sql = "SELECT * FROM ASSESSMENTDETAIL WHERE TABLEID = '" + TableId + "' ";

                Dt2 = link.DBExecSQL(sql);
                if (Dt2.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt2.Rows.Count; i++)

                    {
                        var temp = new Dictionary<string, string>();
                        temp["Name"] = Dt2.Rows[i]["ITEMID"].ToString();
                        temp["Value"] = Dt2.Rows[i]["ITEMVALUE"].ToString();
                        Temp.Add(temp);
                    }
                }

                if (Temp.Count > 0)
                {
                    //接觸史TOCC
                    //媽媽
                    if (Temp.Exists(x => x["Name"] == "rb_mother"))
                    {
                        ViewBag.INF_MOM = Temp.Find(x => x["Name"] == "rb_mother")["Value"];
                        if (Temp.Exists(x => x["Name"] == "mother_ck_cp"))
                        {
                            var INF_MOM_SYM = Temp.Find(x => x["Name"] == "mother_ck_cp")["Value"];
                            INF_MOM_SYM = INF_MOM_SYM.Replace("發燒", "0");
                            INF_MOM_SYM = INF_MOM_SYM.Replace("腹瀉", "1");
                            INF_MOM_SYM = INF_MOM_SYM.Replace("咳嗽", "2");
                            INF_MOM_SYM = INF_MOM_SYM.Replace("流鼻水", "3");
                            INF_MOM_SYM = INF_MOM_SYM.Replace("出疹子", "4");
                            INF_MOM_SYM = INF_MOM_SYM.Replace("其他", "5");
                            ViewBag.INF_MOM_SYM = INF_MOM_SYM;
                        }
                        if (Temp.Exists(x => x["Name"] == "txt_mother_Y_other"))
                            ViewBag.INF_MOM_OTH = Temp.Find(x => x["Name"] == "txt_mother_Y_other")["Value"];
                    }
                    //同住家人
                    if (Temp.Exists(x => x["Name"] == "rb_family"))
                    {
                        ViewBag.INF_OTH = Temp.Find(x => x["Name"] == "rb_family")["Value"];
                        if (Temp.Exists(x => x["Name"] == "txt_family_appellation"))
                            ViewBag.INF_OTH_WHO = Temp.Find(x => x["Name"] == "txt_family_appellation")["Value"];

                        if (Temp.Exists(x => x["Name"] == "family_ck_cp"))
                        {
                            var INF_OTH_SYM = Temp.Find(x => x["Name"] == "family_ck_cp")["Value"];
                            INF_OTH_SYM = INF_OTH_SYM.Replace("發燒", "0");
                            INF_OTH_SYM = INF_OTH_SYM.Replace("腹瀉", "1");
                            INF_OTH_SYM = INF_OTH_SYM.Replace("咳嗽", "2");
                            INF_OTH_SYM = INF_OTH_SYM.Replace("流鼻水", "3");
                            INF_OTH_SYM = INF_OTH_SYM.Replace("出疹子", "4");
                            INF_OTH_SYM = INF_OTH_SYM.Replace("其他", "5");
                            ViewBag.INF_OTH_SYM = INF_OTH_SYM;
                        }
                        if (Temp.Exists(x => x["Name"] == "txt_family_Y_other"))
                            ViewBag.INF_OTH_OTH = Temp.Find(x => x["Name"] == "txt_family_Y_other")["Value"];
                    }
                    //生產前14天內，寶寶的哥哥、姊姊學校有無班上同學因為傳染病請假或班級停課之情形
                    if (Temp.Exists(x => x["Name"] == "rb_brosis"))
                    {
                        ViewBag.BS_CLS = Temp.Find(x => x["Name"] == "rb_brosis")["Value"];
                        if (Temp.Exists(x => x["Name"] == "brosis_ck_cp"))
                        {
                            var BS_CLS_RS = Temp.Find(x => x["Name"] == "brosis_ck_cp")["Value"];
                            BS_CLS_RS = BS_CLS_RS.Replace("腸病毒", "0");
                            BS_CLS_RS = BS_CLS_RS.Replace("流感", "1");
                            BS_CLS_RS = BS_CLS_RS.Replace("水痘", "2");
                            BS_CLS_RS = BS_CLS_RS.Replace("其他", "3");
                            ViewBag.BS_CLS_RS = BS_CLS_RS;
                        }

                        if (Temp.Exists(x => x["Name"] == "txt_brosis_Y_other"))
                            ViewBag.BS_CLS_OTH = Temp.Find(x => x["Name"] == "txt_brosis_Y_other")["Value"];
                    }
                    //住院期間照顧者(應盡量維持同一人)，目前有無：發燒、腹瀉、咳嗽、流鼻水等疑似感染症狀
                    if (Temp.Exists(x => x["Name"] == "rb_company"))
                        ViewBag.INF_CARE = Temp.Find(x => x["Name"] == "rb_company")["Value"];
                }
            }

            #endregion
            return View();
        }
        #endregion

        #region 新生兒入院護理評估列印
        /// <summary>
        /// 新生兒入院護理評估列印
        /// </summary>
        /// <param name="mother_feeno"></param>
        /// <param name="feeno"></param>
        /// <param name="tableid"></param>
        /// <returns></returns>
        public ActionResult Insert_NBENTR_PDF(string mother_feeno, string feeno, string tableid)
        {
            PatientInfo pinfo = new PatientInfo();
            byte[] ByteCode = webService.GetPatientInfo(feeno);
            string JosnArr = "";
            //病人資訊
            if (ByteCode != null)
            {
                JosnArr = CompressTool.DecompressString(ByteCode);
                pinfo = JsonConvert.DeserializeObject<PatientInfo>(JosnArr);
            }
            ViewData["ptinfo"] = pinfo;

            List<DrugOrder> Drug_list = new List<DrugOrder>();
            byte[] labfoByteCode = webService.GetOpdMed(mother_feeno);
            if (labfoByteCode != null)
            {
                string labJosnArr = CompressTool.DecompressString(labfoByteCode);
                Drug_list = JsonConvert.DeserializeObject<List<DrugOrder>>(labJosnArr);
            }
            ViewData["Drug_list"] = Drug_list;

            if (tableid != "" && tableid != null)
            {
                DataTable dt = obs_m.sel_nbentr(feeno, "", tableid);
                dt.Columns.Add("HBSAG_INST_NAME");
                dt.Columns.Add("HBEAG_INST_NAME");
                dt.Columns.Add("RUBELLA_INST_NAME");
                dt.Columns.Add("VDRL1_INST_NAME");
                dt.Columns.Add("VDRL2_INST_NAME");
                dt.Columns.Add("HIV_INST_NAME");
                dt.Columns.Add("GBS_INST_NAME");

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow r in dt.Rows)
                    {
                        switch (r["HBSAG"].ToString() ?? "")
                        {
                            case "0":
                                r["HBSAG"] = "+陽性";
                                break;
                            case "1":
                                r["HBSAG"] = "-陰性";
                                break;
                            case "2":
                                r["HBSAG"] = "F偽陽";
                                break;
                            case "3":
                                r["HBSAG"] = "X未驗";
                                break;
                            default:
                                r["HBSAG"] = "";
                                break;
                        }
                        switch (r["HBEAG"].ToString() ?? "")
                        {
                            case "0":
                                r["HBEAG"] = "+陽性";
                                break;
                            case "1":
                                r["HBEAG"] = "-陰性";
                                break;
                            case "2":
                                r["HBEAG"] = "F偽陽";
                                break;
                            case "3":
                                r["HBEAG"] = "X未驗";
                                break;
                            default:
                                r["HBEAG"] = "";
                                break;
                        }
                        switch (r["RUBELLA"].ToString() ?? "")
                        {
                            case "0":
                                r["RUBELLA"] = "+陽性";
                                break;
                            case "1":
                                r["RUBELLA"] = "-陰性";
                                break;
                            case "2":
                                r["RUBELLA"] = "F偽陽";
                                break;
                            case "3":
                                r["RUBELLA"] = "X未驗";
                                break;
                            default:
                                r["RUBELLA"] = "";
                                break;
                        }
                        switch (r["VDRL1"].ToString() ?? "")
                        {
                            case "0":
                                r["VDRL1"] = "+陽性";
                                break;
                            case "1":
                                r["VDRL1"] = "-陰性";
                                break;
                            case "2":
                                r["VDRL1"] = "F偽陽";
                                break;
                            case "3":
                                r["VDRL1"] = "X未驗";
                                break;
                            default:
                                r["VDRL1"] = "";
                                break;
                        }
                        switch (r["VDRL2"].ToString() ?? "")
                        {
                            case "0":
                                r["VDRL2"] = "+陽性";
                                break;
                            case "1":
                                r["VDRL2"] = "-陰性";
                                break;
                            case "2":
                                r["VDRL2"] = "F偽陽";
                                break;
                            case "3":
                                r["VDRL2"] = "X未驗";
                                break;
                            default:
                                r["VDRL2"] = "";
                                break;
                        }
                        switch (r["HIV"].ToString() ?? "")
                        {
                            case "0":
                                r["HIV"] = "+陽性";
                                break;
                            case "1":
                                r["HIV"] = "-陰性";
                                break;
                            case "2":
                                r["HIV"] = "F偽陽";
                                break;
                            case "3":
                                r["HIV"] = "X未驗";
                                break;
                            default:
                                r["HIV"] = "";
                                break;
                        }
                        switch (r["GBS"].ToString() ?? "")
                        {
                            case "0":
                                r["GBS"] = "+陽性";
                                break;
                            case "1":
                                r["GBS"] = "-陰性";
                                break;
                            case "2":
                                r["GBS"] = "F偽陽";
                                break;
                            case "3":
                                r["GBS"] = "X未驗";
                                break;
                            default:
                                r["GBS"] = "";
                                break;
                        }

                        r["HBSAG_INST_NAME"] = Get_INSPTList(r["HBSAG_INST"].ToString());
                        r["HBEAG_INST_NAME"] = Get_INSPTList(r["HBEAG_INST"].ToString());
                        r["RUBELLA_INST_NAME"] = Get_INSPTList(r["RUBELLA_INST"].ToString());
                        r["VDRL1_INST_NAME"] = Get_INSPTList(r["VDRL1_INST"].ToString());
                        r["VDRL2_INST_NAME"] = Get_INSPTList(r["VDRL2_INST"].ToString());
                        r["HIV_INST_NAME"] = Get_INSPTList(r["HIV_INST"].ToString());
                        r["GBS_INST_NAME"] = Get_INSPTList(r["GBS_INST"].ToString());
                    }
                }


                ViewBag.dt = set_dt(dt);

                DataTable dt_ASS_1F = obs_m.sel_ass_1_1f(feeno, "NBENTR", tableid, "");
                ViewBag.dt_ASS_1F = set_dt(dt_ASS_1F);
            }
            return View();
        }
        #endregion

        #region 新生兒入院護理評估新增
        /// <summary>
        /// 新生兒入院護理評估新增
        /// </summary>
        ///  <param name="form"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Insert_NBENTR(FormCollection form)
        {
            RESPONSE_MSG json_result = new RESPONSE_MSG();
            string mom_feeno = string.Empty, tableid = string.Empty; //簽章用 此處以存檔僅會單筆為基礎  多筆則不適用需改寫法
            int erow = 0;
            link.DBOpen(true);
            string feeno = string.Empty;
            string userno = string.Empty;
            string PK_ID = string.Empty;
            string birth = string.Empty;

            var dateNow = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            try
            {
                DataTable dt_nbentr = obs_m.sel_nbentr(ptinfo.FeeNo, "", "");
                if (dt_nbentr == null || dt_nbentr.Rows.Count == 0)
                {
                    byte[] listByteCode = webService.GetBabyFeeNo2(ptinfo.ChartNo, ptinfo.FeeNo);
                    if (listByteCode == null) { }
                    else
                    {
                        string listJsonArray = CompressTool.DecompressString(listByteCode);
                        BabyFeeNo2[] baby_feeno2 = JsonConvert.DeserializeObject<BabyFeeNo2[]>(listJsonArray);

                        List<DBItem> insertDataList_baby = new List<DBItem>();
                        insertDataList_baby.Add(new DBItem("BABY_FEE_NO2", ptinfo.FeeNo, DBItem.DBDataType.String));

                        string where = " BABY_FEE_NO = '" + baby_feeno2[0].NB_FEENO
                            + "' AND BABY_CHART_NO = '" + ptinfo.ChartNo + "'";
                        obs_m.DBExecUpdate("OBS_BABYLINK_DATA", insertDataList_baby, where);
                    }
                }

                userno = userinfo.EmployeesNo;
                feeno = ptinfo.FeeNo;
                string id = base.creatid("OBS_NBENTR", userno, feeno, "0");
                PK_ID = id;
                List<DBItem> insertDataList = new List<DBItem>();
                insertDataList.Add(new DBItem("IID", id, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDTIME", dateNow, DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("RECORDTIME", dateNow, DBItem.DBDataType.DataTime));

                insertDataList.Add(new DBItem("STATUS", form["TYPE"], DBItem.DBDataType.String));

                insertDataList.Add(new DBItem("DESCRIPTION", "入院護理評估-新生兒", DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("VERSION", "1", DBItem.DBDataType.String));

                #region 新生兒出生資料
                insertDataList.Add(new DBItem("BIRTH_PLACE", form["RB_BIRTH_PLACE"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FROM_WHERE", form["RB_FROM_WHERE"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PROCESS", form["TXT_PROCESS"], DBItem.DBDataType.String));

                if (form["TXT_BIRTH_DAY"] != "" && form["TXT_BIRTH_DAY"] != null && form["TXT_BIRTH_TIME"] != "" && form["TXT_BIRTH_TIME"] != null)
                {
                    insertDataList.Add(new DBItem("BIRTH", form["TXT_BIRTH_DAY"] + " " + form["TXT_BIRTH_TIME"], DBItem.DBDataType.DataTime));
                    birth = form["TXT_BIRTH_DAY"];
                }

                insertDataList.Add(new DBItem("GENDER", form["RB_GENDER"], DBItem.DBDataType.String));

                insertDataList.Add(new DBItem("BIRTH_TYPE", form["RB_BIRTH_TYPE"], DBItem.DBDataType.String));
                if (form["RB_BIRTH_TYPE"] == "0")
                    insertDataList.Add(new DBItem("VACUUM_NSD", form["RB_VACUUM_NSD"], DBItem.DBDataType.String));
                else if (form["RB_BIRTH_TYPE"] == "1")
                    insertDataList.Add(new DBItem("VACUUM_CS", form["RB_VACUUM_CS"], DBItem.DBDataType.String));
                else
                {
                    insertDataList.Add(new DBItem("VACUUM_NSD", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("VACUUM_CS", null, DBItem.DBDataType.String));
                }

                insertDataList.Add(new DBItem("GEST_M", form["TXT_GEST_M"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("GEST_D", form["TXT_GEST_D"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("GEST_REMARK", form["TXT_GEST_REMARK"], DBItem.DBDataType.String));

                insertDataList.Add(new DBItem("HEIGHT", form["TXT_HEIGHT"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("WEIGHT", form["TXT_WEIGHT"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("HEAD", form["TXT_HEAD"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CHEST", form["TXT_CHEST"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("RB_AS_1_NUM", form["TXT_RB_AS_1_NUM"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("RB_AS_5_NUM", form["TXT_RB_AS_5_NUM"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("RB_AS_10_NUM", form["TXT_RB_AS_10_NUM"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("RB_AS_REMARK", form["TXT_RB_AS_REMARK"], DBItem.DBDataType.String));

                insertDataList.Add(new DBItem("MAT_NAME", form["TXT_MAT_NAME"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MAT_AGE", form["TXT_MAT_AGE"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MAT_OCU", form["TXT_MAT_OCU"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MAT_EDU", form["RB_MAT_EDU"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MAT_TEL", form["TXT_MAT_TEL"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PAT_NAME", form["TXT_PAT_NAME"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PAT_AGE", form["TXT_PAT_AGE"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PAT_OCU", form["TXT_PAT_OCU"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PAT_EDU", form["RB_PAT_EDU"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PAT_TEL", form["TXT_PAT_TEL"], DBItem.DBDataType.String));
                #endregion

                #region 母親病史
                insertDataList.Add(new DBItem("MED", form["RB_MED"], DBItem.DBDataType.String));
                if (form["RB_MED"] == "1")
                {

                }

                #region 疾病史
                insertDataList.Add(new DBItem("DISE", form["RB_DISE"], DBItem.DBDataType.String));
                if (form["RB_DISE"] == "1")
                {
                    var DISE_ITM = form["CK_DISE_ITM"];
                    var DISE_ITMV = new List<string>() { "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0" };
                    if (DISE_ITM == null)
                        insertDataList.Add(new DBItem("DISE_ITM", String.Join("", DISE_ITMV), DBItem.DBDataType.String));
                    else
                    {
                        var DISE_ITM_SP = DISE_ITM.Split(',');
                        foreach (var i in DISE_ITM_SP)
                            DISE_ITMV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("DISE_ITM", String.Join("", DISE_ITMV), DBItem.DBDataType.String));
                    }
                    if (DISE_ITMV[14] == "1")
                        insertDataList.Add(new DBItem("DISE_ITM_OTH", form["TXT_DISE_ITM_OTH"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("DISE_ITM_OTH", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("DISE_ITM", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("DISE_ITM_OTH", null, DBItem.DBDataType.String));
                }
                #endregion                

                insertDataList.Add(new DBItem("HBSAG", form["DDL_HBSAG"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("HBSAG_INST", form["TXT_HBSAG_INST"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("HBSAG_OTH", form["TXT_HBSAG_OTH"], DBItem.DBDataType.String));

                if (form["HBSAGTYPE"].ToString() != "")
                {
                    insertDataList.Add(new DBItem("HBSAG_RCA", form["TXT_HBSAG_RCA"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("HBSAG_RCR", form["TXT_HBSAG_RCR"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("HBSAG_RCT", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                }
                else
                {
                    if (form["TYPE"] == "已完成")
                    {
                        json_result.status = RESPONSE_STATUS.ERROR;
                        json_result.message = "HBsAg(B型肝炎表面抗原)尚未按確定覆核!";
                        return Content(JsonConvert.SerializeObject(json_result), "application/json");
                    }
                }

                insertDataList.Add(new DBItem("HBEAG", form["DDL_HBEAG"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("HBEAG_INST", form["TXT_HBEAG_INST"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("HBEAG_OTH", form["TXT_HBEAG_OTH"], DBItem.DBDataType.String));

                if (form["HBEAGTYPE"].ToString() != "")
                {
                    insertDataList.Add(new DBItem("HBEAG_RCA", form["TXT_HBEAG_RCA"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("HBEAG_RCR", form["TXT_HBEAG_RCR"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("HBEAG_RCT", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                }
                else
                {
                    if (form["TYPE"] == "已完成")
                    {
                        json_result.status = RESPONSE_STATUS.ERROR;
                        json_result.message = "HBeAg(e抗原)尚未按確定覆核!";
                        return Content(JsonConvert.SerializeObject(json_result), "application/json");
                    }
                }

                insertDataList.Add(new DBItem("RUBELLA", form["DDL_RUBELLA"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("RUBELLA_INST", form["TXT_RUBELLA_INST"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("RUBELLA_OTH", form["TXT_RUBELLA_OTH"], DBItem.DBDataType.String));

                insertDataList.Add(new DBItem("VDRL1", form["DDL_VDRL1"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("VDRL1_INST", form["TXT_VDRL1_INST"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("VDRL1_OTH", form["TXT_VDRL1_OTH"], DBItem.DBDataType.String));

                insertDataList.Add(new DBItem("VDRL2", form["DDL_VDRL2"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("VDRL2_INST", form["TXT_VDRL2_INST"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("VDRL2_OTH", form["TXT_VDRL2_OTH"], DBItem.DBDataType.String));

                insertDataList.Add(new DBItem("HIV", form["DDL_HIV"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("HIV_INST", form["TXT_HIV_INST"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("HIV_OTH", form["TXT_HIV_OTH"], DBItem.DBDataType.String));

                insertDataList.Add(new DBItem("HIVPT_R", form["RB_HIVPT_R"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("GBS", form["DDL_GBS"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("GBS_INST", form["TXT_GBS_INST"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("GBS_OTH", form["TXT_GBS_OTH"], DBItem.DBDataType.String));

                if (form["TXT_GBS_AB_DAY"] != "" && form["TXT_GBS_AB_DAY"] != null && form["TXT_GBS_AB_TIME"] != "" && form["TXT_GBS_AB_TIME"] != null)
                    insertDataList.Add(new DBItem("GBS_AB", form["TXT_GBS_AB_DAY"] + " " + form["TXT_GBS_AB_TIME"], DBItem.DBDataType.DataTime));


                #region 妊娠併發症
                insertDataList.Add(new DBItem("PREG", form["RB_PREG"], DBItem.DBDataType.String));
                if (form["RB_PREG"] == "1")
                {
                    var PREG_ITM = form["CK_PREG_ITM"];
                    var PREG_ITMV = new List<string>() { "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0" };
                    if (PREG_ITM == null)
                        insertDataList.Add(new DBItem("PREG_ITM", String.Join("", PREG_ITM), DBItem.DBDataType.String));
                    else
                    {
                        var PREG_ITM_SP = PREG_ITM.Split(',');
                        foreach (var i in PREG_ITM_SP)
                            PREG_ITMV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("PREG_ITM", String.Join("", PREG_ITMV), DBItem.DBDataType.String));
                    }
                    if (PREG_ITMV[12] == "1")
                        insertDataList.Add(new DBItem("PREG_ITM_OTH", form["TXT_PREG_ITM_OTH"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("PREG_ITM_OTH", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("PREG_ITM", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PREG_ITM_OTH", null, DBItem.DBDataType.String));
                }
                #endregion

                #endregion

                #region 護理評估

                #region 1.頭部
                insertDataList.Add(new DBItem("ASS_1_1", form["RB_ASS_1_1"], DBItem.DBDataType.String));

                var ADD_ITEM = new List<string>() {
                    form["CK_ASS_1_1F1"] != null ? "1" : "0",
                    form["CK_ASS_1_1F2"] != null ? "1" : "0",
                    form["CK_ASS_1_1F3"] != null ? "1" : "0",
                    form["CK_ASS_1_1F4"] != null ? "1" : "0",
                    form["CK_ASS_1_1F5"] != null ? "1" : "0",
                    };

                var param = 0;
                foreach (var a in ADD_ITEM)
                {
                    param = ++param;
                    List<DBItem> data = new List<DBItem>();
                    data.Add(new DBItem("IID", id, DBItem.DBDataType.String));
                    data.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                    data.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
                    data.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                    data.Add(new DBItem("RECORDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                    data.Add(new DBItem("TYPE", "NBENTR", DBItem.DBDataType.String));
                    data.Add(new DBItem("SNO", param.ToString(), DBItem.DBDataType.String));
                    var ITM = "";
                    switch (param.ToString())
                    {
                        case "1":
                            ITM = "頭骨";
                            break;
                        case "2":
                            ITM = "頂骨";
                            break;
                        case "3":
                            ITM = "左顳骨";
                            break;
                        case "4":
                            ITM = "右顳骨";
                            break;
                        case "5":
                            ITM = "枕骨";
                            break;
                    }

                    data.Add(new DBItem("ITM", ITM, DBItem.DBDataType.String));

                    if (a == "1")
                    {
                        var ASS_1_1F = form[$"CK_ASS_1_1F{param}"];
                        var ASS_1_1FV = new List<string>() { "0", "0", "0", "0", "0" };

                        var ASS_1_1F_SP = ASS_1_1F.Split(',');
                        foreach (var i in ASS_1_1F_SP)
                            ASS_1_1FV[Convert.ToInt32(i)] = "1";

                        data.Add(new DBItem("ITM_REMARK", String.Join("", ASS_1_1FV), DBItem.DBDataType.String));

                        if (ASS_1_1FV[0] == "1")
                            data.Add(new DBItem("ITM_RED_NO", form[$"TXT_{param}_RED"], DBItem.DBDataType.String));
                        else
                            data.Add(new DBItem("ITM_RED_NO", null, DBItem.DBDataType.String));

                        if (ASS_1_1FV[1] == "1")
                            data.Add(new DBItem("ITM_BLI_NO", form[$"TXT_{param}_BLI"], DBItem.DBDataType.String));
                        else
                            data.Add(new DBItem("ITM_BLI_NO", null, DBItem.DBDataType.String));

                        if (ASS_1_1FV[2] == "1")
                            data.Add(new DBItem("ITM_HUR_NO", form[$"TXT_{param}_HUR"], DBItem.DBDataType.String));
                        else
                            data.Add(new DBItem("ITM_HUR_NO", null, DBItem.DBDataType.String));

                        if (ASS_1_1FV[3] == "1")
                            data.Add(new DBItem("ITM_CS_NO", form[$"TXT_{param}_CS"], DBItem.DBDataType.String));
                        else
                            data.Add(new DBItem("ITM_CS_NO", null, DBItem.DBDataType.String));

                        if (ASS_1_1FV[4] == "1")
                            data.Add(new DBItem("ITM_OTH_NO", form[$"TXT_{param}_OTH"], DBItem.DBDataType.String));
                        else
                            data.Add(new DBItem("ITM_OTH_NO", null, DBItem.DBDataType.String));
                    }
                    data = setNullToEmpty(data);
                    if (link.DBExecInsertTns("OBS_ASS_1_1F", data) < 0)
                    {
                        link.DBRollBack();
                        json_result.status = RESPONSE_STATUS.ERROR;
                        json_result.message = "新增失敗";
                        //return "-1";
                        return Content(JsonConvert.SerializeObject(json_result), "application/json");
                    }
                }

                insertDataList.Add(new DBItem("ASS_1_2", form["RB_ASS_1_2"], DBItem.DBDataType.String));
                if (form["RB_ASS_1_2"] == "0")
                {
                    var ASS_1_2F1 = form["CK_ASS_1_2F1"];
                    var ASS_1_2F1V = new List<string>() { "0", "0", "0", "0", "0", "0", "0", "0" };
                    if (ASS_1_2F1 == null)
                        insertDataList.Add(new DBItem("ASS_1_2F1", String.Join("", ASS_1_2F1V), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_1_2F1_SP = ASS_1_2F1.Split(',');
                        foreach (var i in ASS_1_2F1_SP)
                            ASS_1_2F1V[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_1_2F1", String.Join("", ASS_1_2F1V), DBItem.DBDataType.String));
                    }
                    if (ASS_1_2F1V[3] == "1")
                        insertDataList.Add(new DBItem("ASS_1_2F2", form["TXT_ASS_1_2F2"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_1_2F2", null, DBItem.DBDataType.String));
                    if (ASS_1_2F1V[7] == "1")
                        insertDataList.Add(new DBItem("ASS_1_2F3", form["TXT_ASS_1_2F3"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_1_2F3", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_1_2F1", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_1_2F2", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_1_2F3", null, DBItem.DBDataType.String));
                }
                #endregion

                #region 2.顏面
                insertDataList.Add(new DBItem("ASS_2", form["RB_ASS_2"], DBItem.DBDataType.String));
                if (form["RB_ASS_2"] == "0")
                {
                    var ASS_2F = form["CK_ASS_2F"];
                    var ASS_2FV = new List<string>() { "0", "0", "0", "0", "0", "0" };
                    if (ASS_2F == null)
                        insertDataList.Add(new DBItem("ASS_2F", String.Join("", ASS_2FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_2F_SP = ASS_2F.Split(',');
                        foreach (var i in ASS_2F_SP)
                            ASS_2FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_2F", String.Join("", ASS_2FV), DBItem.DBDataType.String));
                    }
                    if (ASS_2FV[5] == "1")
                        insertDataList.Add(new DBItem("ASS_2FO", form["TXT_ASS_2FO"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_2FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_2F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_2FO", null, DBItem.DBDataType.String));
                }
                #endregion

                #region 3.眼睛
                insertDataList.Add(new DBItem("ASS_3", form["RB_ASS_3"], DBItem.DBDataType.String));
                if (form["RB_ASS_3"] == "0")
                {
                    var ASS_3F = form["CK_ASS_3F"];
                    var ASS_3FV = new List<string>() { "0", "0", "0", "0", "0", "0" };
                    if (ASS_3F == null)
                        insertDataList.Add(new DBItem("ASS_3F", String.Join("", ASS_3FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_3F_SP = ASS_3F.Split(',');
                        foreach (var i in ASS_3F_SP)
                            ASS_3FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_3F", String.Join("", ASS_3FV), DBItem.DBDataType.String));
                    }
                    if (ASS_3FV[5] == "1")
                        insertDataList.Add(new DBItem("ASS_3FO", form["TXT_ASS_3FO"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_3FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_3F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_3FO", null, DBItem.DBDataType.String));
                }
                #endregion

                #region 4.耳朵
                insertDataList.Add(new DBItem("ASS_4", form["RB_ASS_4"], DBItem.DBDataType.String));
                if (form["RB_ASS_4"] == "0")
                {
                    var ASS_4F = form["CK_ASS_4F"];
                    var ASS_4FV = new List<string>() { "0", "0", "0", "0", "0", "0", "0" };
                    if (ASS_4F == null)
                        insertDataList.Add(new DBItem("ASS_4F", String.Join("", ASS_4FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_4F_SP = ASS_4F.Split(',');
                        foreach (var i in ASS_4F_SP)
                            ASS_4FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_4F", String.Join("", ASS_4FV), DBItem.DBDataType.String));
                    }
                    if (ASS_4FV[6] == "1")
                        insertDataList.Add(new DBItem("ASS_4FO", form["TXT_ASS_4FO"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_4FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_4F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_4FO", null, DBItem.DBDataType.String));
                }
                #endregion

                #region 5.鼻子
                var ASS_5 = form["CK_ASS_5"];
                var ASS_5V = new List<string>() { "0", "0", "0" };
                if (ASS_5 == null)
                    insertDataList.Add(new DBItem("ASS_5", String.Join("", ASS_5V), DBItem.DBDataType.String));
                else
                {
                    var ASS_5_SP = ASS_5.Split(',');
                    foreach (var i in ASS_5_SP)
                        ASS_5V[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("ASS_5", String.Join("", ASS_5V), DBItem.DBDataType.String));
                }
                if (ASS_5V[2] == "1")
                    insertDataList.Add(new DBItem("ASS_5O", form["TXT_ASS_5O"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_5O", null, DBItem.DBDataType.String));
                #endregion

                #region 6.口腔
                insertDataList.Add(new DBItem("ASS_6", form["RB_ASS_6"], DBItem.DBDataType.String));
                if (form["RB_ASS_6"] == "0")
                {
                    var ASS_6F = form["CK_ASS_6F"];
                    var ASS_6FV = new List<string>() { "0", "0", "0", "0", "0" };
                    if (ASS_6F == null)
                        insertDataList.Add(new DBItem("ASS_6F", String.Join("", ASS_6FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_6F_SP = ASS_6F.Split(',');
                        foreach (var i in ASS_6F_SP)
                            ASS_6FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_6F", String.Join("", ASS_6FV), DBItem.DBDataType.String));
                    }
                    if (ASS_6FV[4] == "1")
                        insertDataList.Add(new DBItem("ASS_6FO", form["TXT_ASS_6FO"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_6FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_6F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_6FO", null, DBItem.DBDataType.String));
                }
                #endregion

                #region 7.頸部
                insertDataList.Add(new DBItem("ASS_7", form["RB_ASS_7"], DBItem.DBDataType.String));
                if (form["RB_ASS_7"] == "0")
                {
                    var ASS_7F = form["CK_ASS_7F"];
                    var ASS_7FV = new List<string>() { "0", "0", "0", "0", "0", "0", "0" };
                    if (ASS_7F == null)
                        insertDataList.Add(new DBItem("ASS_7F", String.Join("", ASS_7FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_7F_SP = ASS_7F.Split(',');
                        foreach (var i in ASS_7F_SP)
                            ASS_7FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_7F", String.Join("", ASS_7FV), DBItem.DBDataType.String));
                    }
                    if (ASS_7FV[6] == "1")
                        insertDataList.Add(new DBItem("ASS_7FO", form["TXT_ASS_7FO"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_7FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_7F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_7FO", null, DBItem.DBDataType.String));
                }
                #endregion

                #region 8.胸部
                //外觀
                insertDataList.Add(new DBItem("ASS_8_1", form["RB_ASS_8_1"], DBItem.DBDataType.String));
                if (form["RB_ASS_8_1"] == "0")
                {
                    var ASS_8_1F = form["CK_ASS_8_1F"];
                    var ASS_8_1FV = new List<string>() { "0", "0", "0", "0", "0" };
                    if (ASS_8_1F == null)
                        insertDataList.Add(new DBItem("ASS_8_1F", String.Join("", ASS_8_1FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_8_1F_SP = ASS_8_1F.Split(',');
                        foreach (var i in ASS_8_1F_SP)
                            ASS_8_1FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_8_1F", String.Join("", ASS_8_1FV), DBItem.DBDataType.String));
                    }
                    if (ASS_8_1FV[4] == "1")
                        insertDataList.Add(new DBItem("ASS_8_1FO", form["TXT_ASS_8_1FO"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_8_1FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_8_1F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_8_1FO", null, DBItem.DBDataType.String));
                }
                //呼吸
                insertDataList.Add(new DBItem("ASS_8_2", form["RB_ASS_8_2"], DBItem.DBDataType.String));
                if (form["RB_ASS_8_2"] == "0")
                {
                    var ASS_8_2F = form["CK_ASS_8_2F"];
                    var ASS_8_2FV = new List<string>() { "0", "0", "0", "0" };
                    if (ASS_8_2F == null)
                        insertDataList.Add(new DBItem("ASS_8_2F", String.Join("", ASS_8_2FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_8_2F_SP = ASS_8_2F.Split(',');
                        foreach (var i in ASS_8_2F_SP)
                            ASS_8_2FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_8_2F", String.Join("", ASS_8_2FV), DBItem.DBDataType.String));
                    }
                    if (ASS_8_2FV[0] == "1")
                        insertDataList.Add(new DBItem("ASS_8_2FO", form["TXT_ASS_8_2FO"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_8_2FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_8_2F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_8_2FO", null, DBItem.DBDataType.String));
                }
                //呼吸音
                insertDataList.Add(new DBItem("ASS_8_3", form["RB_ASS_8_3"], DBItem.DBDataType.String));
                if (form["RB_ASS_8_3"] == "0")
                {
                    var ASS_8_3F = form["CK_ASS_8_3F"];
                    var ASS_8_3FV = new List<string>() { "0", "0", "0", "0", "0" };
                    if (ASS_8_3F == null)
                        insertDataList.Add(new DBItem("ASS_8_3F", String.Join("", ASS_8_3FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_8_3F_SP = ASS_8_3F.Split(',');
                        foreach (var i in ASS_8_3F_SP)
                            ASS_8_3FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_8_3F", String.Join("", ASS_8_3FV), DBItem.DBDataType.String));
                    }
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_8_3F", null, DBItem.DBDataType.String));
                }
                //心臟
                insertDataList.Add(new DBItem("ASS_8_4", form["RB_ASS_8_4"], DBItem.DBDataType.String));
                if (form["RB_ASS_8_4"] == "0")
                {
                    var ASS_8_4F = form["CK_ASS_8_4F"];
                    var ASS_8_4FV = new List<string>() { "0", "0", "0" };
                    if (ASS_8_4F == null)
                        insertDataList.Add(new DBItem("ASS_8_4F", String.Join("", ASS_8_4FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_8_4F_SP = ASS_8_4F.Split(',');
                        foreach (var i in ASS_8_4F_SP)
                            ASS_8_4FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_8_4F", String.Join("", ASS_8_4FV), DBItem.DBDataType.String));
                    }
                    if (ASS_8_4FV[2] == "1")
                        insertDataList.Add(new DBItem("ASS_8_4FO", form["TXT_ASS_8_4FO"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_8_4FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_8_4F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_8_4FO", null, DBItem.DBDataType.String));
                }
                #endregion

                #region 9.腹部
                //疝氣
                insertDataList.Add(new DBItem("ASS_9_1", form["RB_ASS_9_1"], DBItem.DBDataType.String));
                if (form["RB_ASS_9_1"] == "1")
                {
                    var ASS_9_1F = form["CK_ASS_9_1F"];
                    var ASS_9_1FV = new List<string>() { "0", "0", "0", "0" };
                    if (ASS_9_1F == null)
                        insertDataList.Add(new DBItem("ASS_9_1F", String.Join("", ASS_9_1FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_9_1F_SP = ASS_9_1F.Split(',');
                        foreach (var i in ASS_9_1F_SP)
                            ASS_9_1FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_9_1F", String.Join("", ASS_9_1FV), DBItem.DBDataType.String));
                    }
                    if (ASS_9_1FV[3] == "1")
                        insertDataList.Add(new DBItem("ASS_9_1FO", form["TXT_ASS_9_1FO"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_9_1FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_9_1F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_9_1FO", null, DBItem.DBDataType.String));
                }
                //腹脹
                insertDataList.Add(new DBItem("ASS_9_2", form["RB_ASS_9_2"], DBItem.DBDataType.String));
                //臍帶滲血
                insertDataList.Add(new DBItem("ASS_9_3", form["RB_ASS_9_3"], DBItem.DBDataType.String));
                //臍動脈
                insertDataList.Add(new DBItem("ASS_9_4", form["RB_ASS_9_4"], DBItem.DBDataType.String));
                if (form["RB_ASS_9_4"] == "2")
                    insertDataList.Add(new DBItem("ASS_9_4O", form["TXT_ASS_9_5O"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_9_4O", null, DBItem.DBDataType.String));
                //臍靜脈
                insertDataList.Add(new DBItem("ASS_9_5", form["RB_ASS_9_5"], DBItem.DBDataType.String));
                if (form["RB_ASS_9_5"] == "2")
                    insertDataList.Add(new DBItem("ASS_9_5O", form["TXT_ASS_9_5O"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_9_5O", null, DBItem.DBDataType.String));
                //其他
                insertDataList.Add(new DBItem("ASS_9_6", form["TXT_ASS_9_6"], DBItem.DBDataType.String));
                #endregion

                #region 10.臀背部
                insertDataList.Add(new DBItem("ASS_10", form["RB_ASS_10"], DBItem.DBDataType.String));
                if (form["RB_ASS_10"] == "0")
                {
                    var ASS_10F = form["CK_ASS_10F"];
                    var ASS_10FV = new List<string>() { "0", "0", "0", "0" };
                    if (ASS_10F == null)
                        insertDataList.Add(new DBItem("ASS_10F", String.Join("", ASS_10FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_10F_SP = ASS_10F.Split(',');
                        foreach (var i in ASS_10F_SP)
                            ASS_10FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_10F", String.Join("", ASS_10FV), DBItem.DBDataType.String));
                    }
                    if (ASS_10FV[3] == "1")
                        insertDataList.Add(new DBItem("ASS_10FO", form["TXT_ASS_10FO"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_10FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_10F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_10FO", null, DBItem.DBDataType.String));
                }
                #endregion

                #region 11.泌尿生殖器外觀
                //男
                if (form["RB_GENDER"] == "0")
                {
                    //睪丸
                    insertDataList.Add(new DBItem("ASS_11_1", form["RB_ASS_11_1"], DBItem.DBDataType.String));
                    if (form["RB_ASS_11_1"] == "0")
                    {
                        var ASS_11_1F = form["CK_ASS_11_1F"];
                        var ASS_11_1FV = new List<string>() { "0", "0" };
                        if (ASS_11_1F == null)
                            insertDataList.Add(new DBItem("ASS_11_1F", String.Join("", ASS_11_1FV), DBItem.DBDataType.String));
                        else
                        {
                            var ASS_11_1F_SP = ASS_11_1F.Split(',');
                            foreach (var i in ASS_11_1F_SP)
                                ASS_11_1FV[Convert.ToInt32(i)] = "1";
                            insertDataList.Add(new DBItem("ASS_11_1F", String.Join("", ASS_11_1FV), DBItem.DBDataType.String));
                        }
                    }
                    else
                    {
                        insertDataList.Add(new DBItem("ASS_11_1F", null, DBItem.DBDataType.String));
                    }
                    //睪丸
                    insertDataList.Add(new DBItem("ASS_11_2", form["RB_ASS_11_2"], DBItem.DBDataType.String));
                    if (form["RB_ASS_11_2"] == "0")
                    {
                        var ASS_11_2F = form["CK_ASS_11_2F"];
                        var ASS_11_2FV = new List<string>() { "0", "0", "0" };
                        if (ASS_11_2F == null)
                            insertDataList.Add(new DBItem("ASS_11_2F", String.Join("", ASS_11_2FV), DBItem.DBDataType.String));
                        else
                        {
                            var ASS_11_2F_SP = ASS_11_2F.Split(',');
                            foreach (var i in ASS_11_2F_SP)
                                ASS_11_2FV[Convert.ToInt32(i)] = "1";
                            insertDataList.Add(new DBItem("ASS_11_2F", String.Join("", ASS_11_2FV), DBItem.DBDataType.String));
                        }
                        if (ASS_11_2FV[2] == "1")
                            insertDataList.Add(new DBItem("ASS_11_2FO", form["TXT_ASS_11_2FO"], DBItem.DBDataType.String));
                        else
                            insertDataList.Add(new DBItem("ASS_11_2FO", null, DBItem.DBDataType.String));
                    }
                    else
                    {
                        insertDataList.Add(new DBItem("ASS_11_2F", null, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("ASS_11_2FO", null, DBItem.DBDataType.String));
                    }
                    //尿道下裂
                    insertDataList.Add(new DBItem("ASS_11_3", form["RB_ASS_11_3"], DBItem.DBDataType.String));
                    //其他
                    insertDataList.Add(new DBItem("ASS_11_4", form["TXT_ASS_11_4"], DBItem.DBDataType.String));
                    //清除女的欄位
                    insertDataList.Add(new DBItem("ASS_11_5", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_11_6", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_11_7", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_11_7F", null, DBItem.DBDataType.String));
                }
                //女
                else if (form["RB_GENDER"] == "1")
                {
                    //清除男的欄位
                    insertDataList.Add(new DBItem("ASS_11_1", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_11_1F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_11_2", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_11_2F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_11_3", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_11_4", null, DBItem.DBDataType.String));
                    //陰道口
                    insertDataList.Add(new DBItem("ASS_11_5", form["RB_ASS_11_5"], DBItem.DBDataType.String));
                    //陰唇腫
                    insertDataList.Add(new DBItem("ASS_11_6", form["RB_ASS_11_6"], DBItem.DBDataType.String));
                    //陰道分泌物
                    insertDataList.Add(new DBItem("ASS_11_7", form["RB_ASS_11_7"], DBItem.DBDataType.String));
                    if (form["RB_ASS_11_7"] == "1")
                    {
                        var ASS_11_7F = form["CK_ASS_11_7F"];
                        var ASS_11_7FV = new List<string>() { "0", "0", "0" };
                        if (ASS_11_7F == null)
                            insertDataList.Add(new DBItem("ASS_11_7F", String.Join("", ASS_11_7FV), DBItem.DBDataType.String));
                        else
                        {
                            var ASS_11_7F_SP = ASS_11_7F.Split(',');
                            foreach (var i in ASS_11_7F_SP)
                                ASS_11_7FV[Convert.ToInt32(i)] = "1";
                            insertDataList.Add(new DBItem("ASS_11_7F", String.Join("", ASS_11_7FV), DBItem.DBDataType.String));
                        }
                    }
                    else
                    {
                        insertDataList.Add(new DBItem("ASS_11_7F", null, DBItem.DBDataType.String));
                    }
                }
                //肛門
                insertDataList.Add(new DBItem("ASS_11_8", form["RB_ASS_11_8"], DBItem.DBDataType.String));
                if (form["RB_ASS_11_8"] == "0")
                {
                    var ASS_11_8F = form["CK_ASS_11_8F"];
                    var ASS_11_8FV = new List<string>() { "0", "0", "0", "0", "0" };
                    if (ASS_11_8F == null)
                        insertDataList.Add(new DBItem("ASS_11_8F", String.Join("", ASS_11_8FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_11_8F_SP = ASS_11_8F.Split(',');
                        foreach (var i in ASS_11_8F_SP)
                            ASS_11_8FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_11_8F", String.Join("", ASS_11_8FV), DBItem.DBDataType.String));
                    }
                    if (ASS_11_8FV[4] == "1")
                        insertDataList.Add(new DBItem("ASS_11_8FO", form["TXT_ASS_11_8FO"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_11_8FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_11_8F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_11_8FO", null, DBItem.DBDataType.String));
                }
                #endregion

                #region 12.骨骼
                //骨折
                insertDataList.Add(new DBItem("ASS_12_1", form["RB_ASS_12_1"], DBItem.DBDataType.String));
                if (form["RB_ASS_12_1"] == "1")
                {
                    var ASS_12_1F = form["CK_ASS_12_1F"];
                    var ASS_12_1FV = new List<string>() { "0", "0", "0" };
                    if (ASS_12_1F == null)
                        insertDataList.Add(new DBItem("ASS_12_1F", String.Join("", ASS_12_1FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_12_1F_SP = ASS_12_1F.Split(',');
                        foreach (var i in ASS_12_1F_SP)
                            ASS_12_1FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_12_1F", String.Join("", ASS_12_1FV), DBItem.DBDataType.String));
                    }
                    if (ASS_12_1FV[2] == "1")
                        insertDataList.Add(new DBItem("ASS_12_1FO", form["TXT_ASS_12_1FO"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_12_1FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_12_1F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_12_1FO", null, DBItem.DBDataType.String));
                }
                //脫臼
                insertDataList.Add(new DBItem("ASS_12_2", form["RB_ASS_12_2"], DBItem.DBDataType.String));
                if (form["RB_ASS_12_2"] == "1")
                    insertDataList.Add(new DBItem("ASS_12_2F", form["TXT_ASS_12_2F"], DBItem.DBDataType.String));
                //新生兒先天性髖關節脫臼檢查：巴氏測驗(Barlow test) && 歐氏測驗(Ortolani test)
                insertDataList.Add(new DBItem("ASS_12_3", form["RB_ASS_12_3"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_12_4", form["RB_ASS_12_4"], DBItem.DBDataType.String));
                #endregion

                #region 13.肌肉緊張度 14.哭聲
                insertDataList.Add(new DBItem("ASS_13", form["RB_ASS_13"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_14", form["RB_ASS_14"], DBItem.DBDataType.String));

                if (form["RB_ASS_14"] == "4")
                {
                    insertDataList.Add(new DBItem("ASS_14_OTH", form["TXT_ASS_14_OTH"], DBItem.DBDataType.String));
                }
                #endregion

                #region 15.皮膚
                var ASS_15 = form["CK_ASS_15"];
                var ASS_15V = new List<string>() { "0", "0", "0", "0", "0", "0", "0" };
                if (ASS_15 == null)
                    insertDataList.Add(new DBItem("ASS_15", String.Join("", ASS_15V), DBItem.DBDataType.String));
                else
                {
                    var ASS_15_SP = ASS_15.Split(',');
                    foreach (var i in ASS_15_SP)
                        ASS_15V[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("ASS_15", String.Join("", ASS_15V), DBItem.DBDataType.String));
                }
                if (ASS_15V[6] == "1")
                    insertDataList.Add(new DBItem("ASS_15O", form["TXT_ASS_15O"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_15O", null, DBItem.DBDataType.String));
                //血管瘤
                insertDataList.Add(new DBItem("ASS_15_1", form["RB_ASS_15_1"], DBItem.DBDataType.String));
                if (form["RB_ASS_15_1"] == "1")
                {
                    insertDataList.Add(new DBItem("ASS_15_1P", form["TXT_ASS_15_1P"], DBItem.DBDataType.String));

                    //var ASS_15_1F = form["CK_ASS_15_1F"];
                    //var ASS_15_1FV = new List<string>() { "0", "0", "0" };
                    //if (ASS_15_1F == null)
                    //    insertDataList.Add(new DBItem("ASS_15_1F", String.Join("", ASS_15_1FV), DBItem.DBDataType.String));
                    //else
                    //{
                    //    var ASS_15_1F_SP = ASS_15_1F.Split(',');
                    //    foreach (var i in ASS_15_1F_SP)
                    //        ASS_15_1FV[Convert.ToInt32(i)] = "1";
                    //    insertDataList.Add(new DBItem("ASS_15_1F", String.Join("", ASS_15_1FV), DBItem.DBDataType.String));
                    //}
                    //if (ASS_15_1FV[0] == "1")
                    //    insertDataList.Add(new DBItem("ASS_15_1F1", form["TXT_ASS_15_1F1"], DBItem.DBDataType.String));
                    //else
                    //    insertDataList.Add(new DBItem("ASS_15_1F1", null, DBItem.DBDataType.String));
                    //if (ASS_15_1FV[1] == "1")
                    //    insertDataList.Add(new DBItem("ASS_15_1F2", form["TXT_ASS_15_1F2"], DBItem.DBDataType.String));
                    //else
                    //    insertDataList.Add(new DBItem("ASS_15_1F2", null, DBItem.DBDataType.String));
                    //if (ASS_15_1FV[2] == "1")
                    //    insertDataList.Add(new DBItem("ASS_15_1F3", form["TXT_ASS_15_1F3"], DBItem.DBDataType.String));
                    //else
                    //    insertDataList.Add(new DBItem("ASS_15_1F3", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_15_1P", null, DBItem.DBDataType.String));
                    //insertDataList.Add(new DBItem("ASS_15_1F", null, DBItem.DBDataType.String));
                    //insertDataList.Add(new DBItem("ASS_15_1F1", null, DBItem.DBDataType.String));
                    //insertDataList.Add(new DBItem("ASS_15_1F2", null, DBItem.DBDataType.String));
                    //insertDataList.Add(new DBItem("ASS_15_1F3", null, DBItem.DBDataType.String));
                }
                //胎記
                insertDataList.Add(new DBItem("ASS_15_2", form["RB_ASS_15_2"], DBItem.DBDataType.String));
                if (form["RB_ASS_15_2"] == "1")
                {
                    var ASS_15_2F = form["CK_ASS_15_2F"];
                    var ASS_15_2FV = new List<string>() { "0", "0", "0", "0" };
                    if (ASS_15_2F == null)
                        insertDataList.Add(new DBItem("ASS_15_2F", String.Join("", ASS_15_2FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_15_2F_SP = ASS_15_2F.Split(',');
                        foreach (var i in ASS_15_2F_SP)
                            ASS_15_2FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_15_2F", String.Join("", ASS_15_2FV), DBItem.DBDataType.String));
                    }
                    if (ASS_15_2FV[0] == "1")
                        insertDataList.Add(new DBItem("ASS_15_2F1", form["TXT_ASS_15_2F1"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_15_2F1", null, DBItem.DBDataType.String));
                    if (ASS_15_2FV[1] == "1")
                        insertDataList.Add(new DBItem("ASS_15_2F2", form["TXT_ASS_15_2F2"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_15_2F2", null, DBItem.DBDataType.String));
                    if (ASS_15_2FV[2] == "1")
                        insertDataList.Add(new DBItem("ASS_15_2F3", form["TXT_ASS_15_2F3"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_15_2F3", null, DBItem.DBDataType.String));
                    if (ASS_15_2FV[3] == "1")
                        insertDataList.Add(new DBItem("ASS_15_2FO", form["TXT_ASS_15_2FO"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_15_2FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_15_2F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_15_2F1", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_15_2F2", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_15_2F3", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_15_2FO", null, DBItem.DBDataType.String));
                }
                #endregion

                #region 16.四肢
                insertDataList.Add(new DBItem("ASS_16", form["RB_ASS_16"], DBItem.DBDataType.String));
                if (form["RB_ASS_16"] == "0")
                {
                    #region 四肢 - 足內翻
                    insertDataList.Add(new DBItem("ASS_16_1", form["RB_ASS_16_1"], DBItem.DBDataType.String));
                    if (form["RB_ASS_16_1"] == "1")
                    {
                        var ASS_16_1F = form["CK_ASS_16_1F"];
                        var ASS_16_1FV = new List<string>() { "0", "0" };
                        if (ASS_16_1F == null)
                            insertDataList.Add(new DBItem("ASS_16_1F", String.Join("", ASS_16_1FV), DBItem.DBDataType.String));
                        else
                        {
                            var ASS_16_1F_SP = ASS_16_1F.Split(',');
                            foreach (var i in ASS_16_1F_SP)
                                ASS_16_1FV[Convert.ToInt32(i)] = "1";
                            insertDataList.Add(new DBItem("ASS_16_1F", String.Join("", ASS_16_1FV), DBItem.DBDataType.String));
                        }
                    }
                    else
                        insertDataList.Add(new DBItem("ASS_16_1F", null, DBItem.DBDataType.String));
                    #endregion

                    #region 四肢 - 足外翻
                    insertDataList.Add(new DBItem("ASS_16_2", form["RB_ASS_16_2"], DBItem.DBDataType.String));
                    if (form["RB_ASS_16_2"] == "1")
                    {
                        var ASS_16_2F = form["CK_ASS_16_2F"];
                        var ASS_16_2FV = new List<string>() { "0", "0" };
                        if (ASS_16_2F == null)
                            insertDataList.Add(new DBItem("ASS_16_2F", String.Join("", ASS_16_2FV), DBItem.DBDataType.String));
                        else
                        {
                            var ASS_16_2F_SP = ASS_16_2F.Split(',');
                            foreach (var i in ASS_16_2F_SP)
                                ASS_16_2FV[Convert.ToInt32(i)] = "1";
                            insertDataList.Add(new DBItem("ASS_16_2F", String.Join("", ASS_16_2FV), DBItem.DBDataType.String));
                        }
                    }
                    else
                        insertDataList.Add(new DBItem("ASS_16_2F", null, DBItem.DBDataType.String));
                    #endregion

                    #region 四肢 - 單一手紋(斷掌)
                    insertDataList.Add(new DBItem("ASS_16_3", form["RB_ASS_16_3"], DBItem.DBDataType.String));
                    if (form["RB_ASS_16_3"] == "1")
                    {
                        var ASS_16_3F = form["CK_ASS_16_3F"];
                        var ASS_16_3FV = new List<string>() { "0", "0" };
                        if (ASS_16_3F == null)
                            insertDataList.Add(new DBItem("ASS_16_3F", String.Join("", ASS_16_3FV), DBItem.DBDataType.String));
                        else
                        {
                            var ASS_16_3F_SP = ASS_16_3F.Split(',');
                            foreach (var i in ASS_16_3F_SP)
                                ASS_16_3FV[Convert.ToInt32(i)] = "1";
                            insertDataList.Add(new DBItem("ASS_16_3F", String.Join("", ASS_16_3FV), DBItem.DBDataType.String));
                        }
                    }
                    else
                        insertDataList.Add(new DBItem("ASS_16_3F", null, DBItem.DBDataType.String));
                    #endregion
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_16_1", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_1F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_2", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_2F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_3", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_3F", null, DBItem.DBDataType.String));
                }

                #region 四肢 - 活動力
                insertDataList.Add(new DBItem("ASS_16_4", form["RB_ASS_16_4"], DBItem.DBDataType.String));
                if (form["RB_ASS_16_4"] == "0")
                {
                    var ASS_16_4F = form["CK_ASS_16_4F"];
                    var ASS_16_4FV = new List<string>() { "0", "0", "0", "0" };
                    if (ASS_16_4F == null)
                        insertDataList.Add(new DBItem("ASS_16_4F", String.Join("", ASS_16_4FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_16_4F_SP = ASS_16_4F.Split(',');
                        foreach (var i in ASS_16_4F_SP)
                            ASS_16_4FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_16_4F", String.Join("", ASS_16_4FV), DBItem.DBDataType.String));
                    }
                }
                else
                    insertDataList.Add(new DBItem("ASS_16_4F", null, DBItem.DBDataType.String));
                #endregion

                #region 四肢 - 水腫
                insertDataList.Add(new DBItem("ASS_16_5", form["RB_ASS_16_5"], DBItem.DBDataType.String));
                if (form["RB_ASS_16_5"] == "1")
                {
                    var ASS_16_5F = form["CK_ASS_16_5F"];
                    var ASS_16_5FV = new List<string>() { "0", "0", "0", "0" };
                    if (ASS_16_5F == null)
                        insertDataList.Add(new DBItem("ASS_16_5F", String.Join("", ASS_16_5FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_16_5F_SP = ASS_16_5F.Split(',');
                        foreach (var i in ASS_16_5F_SP)
                            ASS_16_5FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_16_5F", String.Join("", ASS_16_5FV), DBItem.DBDataType.String));
                    }
                }
                else
                    insertDataList.Add(new DBItem("ASS_16_5F", null, DBItem.DBDataType.String));
                #endregion

                #region 四肢 - 麻痺
                insertDataList.Add(new DBItem("ASS_16_6", form["RB_ASS_16_6"], DBItem.DBDataType.String));
                if (form["RB_ASS_16_6"] == "1")
                {
                    var ASS_16_6F = form["CK_ASS_16_6F"];
                    var ASS_16_6FV = new List<string>() { "0", "0", "0", "0" };
                    if (ASS_16_6F == null)
                        insertDataList.Add(new DBItem("ASS_16_6F", String.Join("", ASS_16_6FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_16_6F_SP = ASS_16_6F.Split(',');
                        foreach (var i in ASS_16_6F_SP)
                            ASS_16_6FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_16_6F", String.Join("", ASS_16_6FV), DBItem.DBDataType.String));
                    }
                }
                else
                    insertDataList.Add(new DBItem("ASS_16_6F", null, DBItem.DBDataType.String));
                #endregion

                #region 四肢 - 手指
                insertDataList.Add(new DBItem("ASS_16_7", form["RB_ASS_16_7"], DBItem.DBDataType.String));
                if (form["RB_ASS_16_7"] == "0")
                {
                    var ASS_16_7F = form["CK_ASS_16_7F"];
                    var ASS_16_7FV = new List<string>() { "0", "0", "0", "0", "0", "0", "0", "0" };
                    if (ASS_16_7F == null)
                        insertDataList.Add(new DBItem("ASS_16_7F", String.Join("", ASS_16_7FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_16_7F_SP = ASS_16_7F.Split(',');
                        foreach (var i in ASS_16_7F_SP)
                            ASS_16_7FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_16_7F", String.Join("", ASS_16_7FV), DBItem.DBDataType.String));
                    }
                    if (ASS_16_7FV[0] == "1")
                        insertDataList.Add(new DBItem("ASS_16_7F1", form["TXT_ASS_16_7F1"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_16_7F1", null, DBItem.DBDataType.String));

                    if (ASS_16_7FV[1] == "1")
                        insertDataList.Add(new DBItem("ASS_16_7F2", form["TXT_ASS_16_7F2"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_16_7F2", null, DBItem.DBDataType.String));

                    if (ASS_16_7FV[2] == "1")
                        insertDataList.Add(new DBItem("ASS_16_7F3", form["TXT_ASS_16_7F3"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_16_7F3", null, DBItem.DBDataType.String));

                    if (ASS_16_7FV[3] == "1")
                        insertDataList.Add(new DBItem("ASS_16_7F4", form["TXT_ASS_16_7F4"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_16_7F4", null, DBItem.DBDataType.String));

                    if (ASS_16_7FV[4] == "1")
                        insertDataList.Add(new DBItem("ASS_16_7F5", form["TXT_ASS_16_7F5"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_16_7F5", null, DBItem.DBDataType.String));

                    if (ASS_16_7FV[5] == "1")
                        insertDataList.Add(new DBItem("ASS_16_7F6", form["TXT_ASS_16_7F6"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_16_7F6", null, DBItem.DBDataType.String));

                    if (ASS_16_7FV[6] == "1")
                        insertDataList.Add(new DBItem("ASS_16_7F7", form["TXT_ASS_16_7F7"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_16_7F7", null, DBItem.DBDataType.String));

                    if (ASS_16_7FV[7] == "1")
                        insertDataList.Add(new DBItem("ASS_16_7F8", form["TXT_ASS_16_7F8"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_16_7F8", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_16_7F1", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_7F2", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_7F3", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_7F4", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_7F5", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_7F6", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_7F7", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_7F8", null, DBItem.DBDataType.String));
                }
                #endregion

                #region 四肢 - 腳趾
                insertDataList.Add(new DBItem("ASS_16_8", form["RB_ASS_16_8"], DBItem.DBDataType.String));
                if (form["RB_ASS_16_8"] == "0")
                {
                    var ASS_16_8F = form["CK_ASS_16_8F"];
                    var ASS_16_8FV = new List<string>() { "0", "0", "0", "0", "0", "0" };
                    if (ASS_16_8F == null)
                        insertDataList.Add(new DBItem("ASS_16_8F", String.Join("", ASS_16_8FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_16_8F_SP = ASS_16_8F.Split(',');
                        foreach (var i in ASS_16_8F_SP)
                            ASS_16_8FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_16_8F", String.Join("", ASS_16_8FV), DBItem.DBDataType.String));
                    }
                    if (ASS_16_8FV[0] == "1")
                        insertDataList.Add(new DBItem("ASS_16_8F1", form["TXT_ASS_16_8F1"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_16_8F1", null, DBItem.DBDataType.String));

                    if (ASS_16_8FV[1] == "1")
                        insertDataList.Add(new DBItem("ASS_16_8F2", form["TXT_ASS_16_8F2"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_16_8F2", null, DBItem.DBDataType.String));

                    if (ASS_16_8FV[2] == "1")
                        insertDataList.Add(new DBItem("ASS_16_8F3", form["TXT_ASS_16_8F3"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_16_8F3", null, DBItem.DBDataType.String));

                    if (ASS_16_8FV[3] == "1")
                        insertDataList.Add(new DBItem("ASS_16_8F4", form["TXT_ASS_16_8F4"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_16_8F4", null, DBItem.DBDataType.String));

                    if (ASS_16_8FV[4] == "1")
                        insertDataList.Add(new DBItem("ASS_16_8F5", form["TXT_ASS_16_8F5"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_16_8F5", null, DBItem.DBDataType.String));

                    if (ASS_16_8FV[5] == "1")
                        insertDataList.Add(new DBItem("ASS_16_8F6", form["TXT_ASS_16_8F6"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_16_8F6", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_16_8F1", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_8F2", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_8F3", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_8F4", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_8F5", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_8F6", null, DBItem.DBDataType.String));
                }
                #endregion
                #endregion

                #region 17.反射
                insertDataList.Add(new DBItem("ASS_17_1", form["RB_ASS_17_1"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_17_2", form["RB_ASS_17_2"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_17_3", form["RB_ASS_17_3"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_17_4", form["RB_ASS_17_4"], DBItem.DBDataType.String));
                #endregion

                #endregion

                #region 疼痛評估
                var PAIN_AS = form["RB_PAIN_AS"];
                insertDataList.Add(new DBItem("PAIN_AS", PAIN_AS, DBItem.DBDataType.String));

                //數字量表
                if (PAIN_AS == "0")
                    insertDataList.Add(new DBItem("PAIN_AS1", form["RB_PAIN_AS1"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("PAIN_AS1", null, DBItem.DBDataType.String));

                //臉譜量表
                if (PAIN_AS == "1")
                    insertDataList.Add(new DBItem("PAIN_AS2", form["RB_PAIN_AS2"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("PAIN_AS2", null, DBItem.DBDataType.String));

                //困難評估(新生兒)
                if (PAIN_AS == "4")
                {
                    insertDataList.Add(new DBItem("PAIN_AS3_1", form["RB_PAIN_AS3_1"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS3_2", form["RB_PAIN_AS3_2"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS3_3", form["RB_PAIN_AS3_3"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS3_4", form["RB_PAIN_AS3_4"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS3_5", form["RB_PAIN_AS3_5"], DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("PAIN_AS3_1", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS3_2", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS3_3", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS3_4", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS3_5", null, DBItem.DBDataType.String));
                }

                //CPOT評估(加護單位)
                if (PAIN_AS == "5")
                {
                    insertDataList.Add(new DBItem("PAIN_AS4_1", form["RB_PAIN_AS4_1"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS4_2", form["RB_PAIN_AS4_2"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS4_3", form["RB_PAIN_AS4_3"], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS4_4", form["RB_PAIN_AS4_4"], DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("PAIN_AS4_1", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS4_2", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS4_3", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS4_4", null, DBItem.DBDataType.String));
                }
                #endregion

                #region 接觸史
                //生產前14天內，產婦或同住家人有無：發燒、腹瀉、咳嗽、流鼻水等疑似感染症狀？
                insertDataList.Add(new DBItem("INF_MOM", form["RB_INF_MOM"], DBItem.DBDataType.String));
                if (form["RB_INF_MOM"] == "1")
                {
                    var INF_MOM_SYM = form["CK_INF_MOM_SYM"];
                    var INF_MOM_SYMV = new List<string>() { "0", "0", "0", "0", "0", "0" };
                    if (INF_MOM_SYM == null)
                        insertDataList.Add(new DBItem("INF_MOM_SYM", String.Join("", INF_MOM_SYMV), DBItem.DBDataType.String));
                    else
                    {
                        var INF_MOM_SYM_SP = INF_MOM_SYM.Split(',');
                        foreach (var i in INF_MOM_SYM_SP)
                            INF_MOM_SYMV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("INF_MOM_SYM", String.Join("", INF_MOM_SYMV), DBItem.DBDataType.String));
                    }
                    if (INF_MOM_SYMV[5] == "1")
                        insertDataList.Add(new DBItem("INF_MOM_OTH", form["TXT_INF_MOM_OTH"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("INF_MOM_OTH", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("INF_MOM_SYM", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("INF_MOM_OTH", null, DBItem.DBDataType.String));
                }
                //同住家人
                insertDataList.Add(new DBItem("INF_OTH", form["RB_INF_OTH"], DBItem.DBDataType.String));
                if (form["RB_INF_OTH"] == "1")
                {
                    insertDataList.Add(new DBItem("INF_OTH_WHO", form["TXT_INF_OTH_WHO"], DBItem.DBDataType.String));
                    var INF_OTH_SYM = form["CK_INF_OTH_SYM"];
                    var INF_OTH_SYMV = new List<string>() { "0", "0", "0", "0", "0", "0" };
                    if (INF_OTH_SYM == null)
                        insertDataList.Add(new DBItem("INF_OTH_SYM", String.Join("", INF_OTH_SYMV), DBItem.DBDataType.String));
                    else
                    {
                        var INF_OTH_SYM_SP = INF_OTH_SYM.Split(',');
                        foreach (var i in INF_OTH_SYM_SP)
                            INF_OTH_SYMV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("INF_OTH_SYM", String.Join("", INF_OTH_SYMV), DBItem.DBDataType.String));
                    }
                    if (INF_OTH_SYMV[5] == "1")
                        insertDataList.Add(new DBItem("INF_OTH_OTH", form["TXT_INF_OTH_OTH"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("INF_OTH_OTH", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("INF_OTH_WHO", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("INF_OTH_SYM", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("INF_OTH_OTH", null, DBItem.DBDataType.String));
                }
                //生產前14天內，寶寶的哥哥、姊姊學校有無班上同學因為傳染病請假或班級停課之情形
                insertDataList.Add(new DBItem("BS_CLS", form["RB_BS_CLS"], DBItem.DBDataType.String));
                if (form["RB_BS_CLS"] == "1")
                {
                    var BS_CLS_RS = form["CK_BS_CLS_RS"];
                    var BS_CLS_RSV = new List<string>() { "0", "0", "0", "0" };
                    if (BS_CLS_RS == null)
                        insertDataList.Add(new DBItem("BS_CLS_RS", String.Join("", BS_CLS_RSV), DBItem.DBDataType.String));
                    else
                    {
                        var BS_CLS_RS_SP = BS_CLS_RS.Split(',');
                        foreach (var i in BS_CLS_RS_SP)
                            BS_CLS_RSV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("BS_CLS_RS", String.Join("", BS_CLS_RSV), DBItem.DBDataType.String));
                    }
                    if (BS_CLS_RSV[3] == "1")
                        insertDataList.Add(new DBItem("BS_CLS_OTH", form["TXT_BS_CLS_OTH"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("BS_CLS_OTH", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("BS_CLS_RS", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("BS_CLS_OTH", null, DBItem.DBDataType.String));
                }
                //住院期間照顧者(應盡量維持同一人)，目前有無：發燒、腹瀉、咳嗽、流鼻水等疑似感染症狀？
                insertDataList.Add(new DBItem("INF_CARE", form["RB_INF_CARE"], DBItem.DBDataType.String));
                #endregion

                #region TOCC評估
                //症狀
                var SYM = form["CK_SYM"];
                var SYMV = new List<string>() { "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0" };
                if (SYM == null)
                    insertDataList.Add(new DBItem("SYM", String.Join("", SYMV), DBItem.DBDataType.String));
                else
                {
                    var SYM_SP = SYM.Split(',');
                    foreach (var i in SYM_SP)
                        SYMV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("SYM", String.Join("", SYMV), DBItem.DBDataType.String));
                }
                if (SYMV[10] == "1")//(其他TXT)
                    insertDataList.Add(new DBItem("SYM_OTH", form["TXT_SYM_OTH"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("SYM_OTH", null, DBItem.DBDataType.String));

                //旅遊史
                var TRAV = form["CK_TRAV"];
                var TRAVV = new List<string>() { "0", "0", "0" };
                if (TRAV == null)
                    insertDataList.Add(new DBItem("TRAV", String.Join("", TRAVV), DBItem.DBDataType.String));
                else
                {
                    var TRAV_SP = TRAV.Split(',');
                    foreach (var i in TRAV_SP)
                        TRAVV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("TRAV", String.Join("", TRAVV), DBItem.DBDataType.String));
                }
                //國內旅遊城市
                insertDataList.Add(new DBItem("TRAV_DOME_CITY", form["TXT_TRAV_DOME_CITY"], DBItem.DBDataType.String));
                //國內旅遊景點
                insertDataList.Add(new DBItem("TRAV_DOME_VIEW", form["TXT_TRAV_DOME_VIEW"], DBItem.DBDataType.String));
                //國內旅遊交通
                var TRAV_DOME_TRAF = form["CK_TRAV_DOME_TRAF"];
                var TRAV_DOME_TRAFV = new List<string>() { "0", "0", "0" };
                if (TRAV_DOME_TRAF == null)
                    insertDataList.Add(new DBItem("TRAV_DOME_TRAF", String.Join("", TRAV_DOME_TRAFV), DBItem.DBDataType.String));
                else
                {
                    var TRAV_DOME_TRAF_SP = TRAV_DOME_TRAF.Split(',');
                    foreach (var i in TRAV_DOME_TRAF_SP)
                        TRAV_DOME_TRAFV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("TRAV_DOME_TRAF", String.Join("", TRAV_DOME_TRAFV), DBItem.DBDataType.String));
                }
                if (TRAV_DOME_TRAFV[2] == "1")//(其他TXT)
                    insertDataList.Add(new DBItem("TRAV_DOME_TRAF_OTH", form["TXT_TRAV_DOME_TRAF_OTH"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("TRAV_DOME_TRAF_OTH", null, DBItem.DBDataType.String));
                //國外旅遊國家
                insertDataList.Add(new DBItem("TRAV_ABO_COUN", form["TXT_TRAV_ABO_COUN"], DBItem.DBDataType.String));
                //國外旅遊目的地
                insertDataList.Add(new DBItem("TRAV_ABO_DEST", form["TXT_TRAV_ABO_DEST"], DBItem.DBDataType.String));
                //國外旅遊交通方式
                var TRAV_ABO_TRAF = form["CK_TRAV_ABO_TRAF"];
                var TRAV_ABO_TRAFV = new List<string>() { "0", "0", "0" };
                if (TRAV_ABO_TRAF == null)
                    insertDataList.Add(new DBItem("TRAV_ABO_TRAF", String.Join("", TRAV_ABO_TRAFV), DBItem.DBDataType.String));
                else
                {
                    var TRAV_ABO_TRAF_SP = TRAV_ABO_TRAF.Split(',');
                    foreach (var i in TRAV_ABO_TRAF_SP)
                        TRAV_ABO_TRAFV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("TRAV_ABO_TRAF", String.Join("", TRAV_ABO_TRAFV), DBItem.DBDataType.String));
                }
                if (TRAV_ABO_TRAFV[2] == "1")//(其他TXT)
                    insertDataList.Add(new DBItem("TRAV_ABO_TRAF_OTH", form["TXT_TRAV_ABO_TRAF_OTH"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("TRAV_ABO_TRAF_OTH", null, DBItem.DBDataType.String));
                //職業別
                var OCCU = form["CK_OCCU"];
                var OCCUV = new List<string>() { "0", "0", "0", "0", "0" };
                if (OCCU == null)
                    insertDataList.Add(new DBItem("OCCU", String.Join("", OCCUV), DBItem.DBDataType.String));
                else
                {
                    var OCCU_SP = OCCU.Split(',');
                    foreach (var i in OCCU_SP)
                        OCCUV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("OCCU", String.Join("", OCCUV), DBItem.DBDataType.String));
                }
                if (OCCUV[4] == "1")//(其他TXT)
                    insertDataList.Add(new DBItem("OCCU_OTH", form["TXT_OCCU_OTH"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("OCCU_OTH", null, DBItem.DBDataType.String));
                //接觸史
                var CONT = form["CK_CONT"];
                var CONTV = new List<string>() { "0", "0", "0" };
                if (CONT == null)
                    insertDataList.Add(new DBItem("CONT", String.Join("", CONTV), DBItem.DBDataType.String));
                else
                {
                    var CONT_SP = CONT.Split(',');
                    foreach (var i in CONT_SP)
                        CONTV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("CONT", String.Join("", CONTV), DBItem.DBDataType.String));
                }
                if (CONTV[1] == "1")//(接觸禽鳥類、畜類等)
                {
                    var CONT_BIRD = form["CK_CONT_BIRD"];
                    var CONT_BIRDV = new List<string>() { "0", "0" };
                    if (CONT_BIRD == null)
                        insertDataList.Add(new DBItem("CONT_BIRD", String.Join("", CONT_BIRDV), DBItem.DBDataType.String));
                    else
                    {
                        var CONT_BIRD_SP = CONT_BIRD.Split(',');
                        foreach (var i in CONT_BIRD_SP)
                            CONT_BIRDV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("CONT_BIRD", String.Join("", CONT_BIRDV), DBItem.DBDataType.String));
                    }
                }
                else
                    insertDataList.Add(new DBItem("CONT_BIRD", null, DBItem.DBDataType.String));

                //嬰幼兒、新生兒接觸史
                //(生產前 14 天內，產婦或同住家人有發燒、腹瀉、咳嗽、流鼻水等疑似感染症狀?)
                insertDataList.Add(new DBItem("CONT_OBS_SYM", form["RB_CONT_OBS_SYM"], DBItem.DBDataType.String));
                //(生產前 14 天內，寶寶的哥哥、姊姊學校班上同學有因為傳染病請假或班級停課之情形?)
                insertDataList.Add(new DBItem("CONT_OBS_SICKLEAVE", form["RB_CONT_OBS_SICKLEAVE"], DBItem.DBDataType.String));
                //(住院期間照顧者(應盡量維持同一人)，目前有發燒、腹瀉、咳嗽、流鼻水等疑似感染症狀?)
                insertDataList.Add(new DBItem("CONT_OBS_CARESYM", form["RB_CONT_OBS_CARESYM"], DBItem.DBDataType.String));

                if (CONTV[2] == "1")//(其他)
                {
                    insertDataList.Add(new DBItem("CONT_OTH", form["TXT_CONT_OTH"], DBItem.DBDataType.String));
                }
                else
                    insertDataList.Add(new DBItem("CONT_OTH", null, DBItem.DBDataType.String));

                //群聚史
                insertDataList.Add(new DBItem("CLU", form["RB_CLU"], DBItem.DBDataType.String));
                if (form["RB_CLU"] == "1")//(家人/朋友/同事有發燒或類流感症狀)
                {
                    var CLU_RELA = form["CK_CLU_RELA"];
                    var CLU_RELAV = new List<string>() { "0", "0", "0", "0" };
                    if (CLU_RELA == null)
                        insertDataList.Add(new DBItem("CLU_RELA", String.Join("", CLU_RELAV), DBItem.DBDataType.String));
                    else
                    {
                        var CLU_RELA_SP = CLU_RELA.Split(',');
                        foreach (var i in CLU_RELA_SP)
                            CLU_RELAV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("CLU_RELA", String.Join("", CLU_RELAV), DBItem.DBDataType.String));
                    }
                    if (CLU_RELAV[3] == "1")
                    {
                        insertDataList.Add(new DBItem("CLU_RELA_OTH", form["TXT_CLU_RELA_OTH"], DBItem.DBDataType.String));
                    }
                    else
                    {
                        insertDataList.Add(new DBItem("CLU_RELA_OTH", null, DBItem.DBDataType.String));
                    }
                }
                else
                {
                    var CLU_RELAV = new List<string>() { "0", "0", "0", "0" };
                    insertDataList.Add(new DBItem("CLU_RELA", String.Join("", CLU_RELAV), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CLU_RELA_OTH", null, DBItem.DBDataType.String));
                }
                #endregion

                insertDataList = setNullToEmpty(insertDataList);
                erow = link.DBExecInsertTns("OBS_NBENTR", insertDataList);

                if (erow == 0)
                {
                    json_result.status = RESPONSE_STATUS.ERROR;
                    json_result.message = "新增失敗";
                    return Content(JsonConvert.SerializeObject(json_result), "application/json");
                }

                #region 護理紀錄
                var exceptionResult = new List<string>();
                var finalResult = "入院經過：";
                finalResult += $"{(form["TXT_PROCESS"] == "" ? "未填寫" : form["TXT_PROCESS"])}。";
                finalResult += "護理評估：";

                #region 頭部-外觀
                var headFlag = false;
                if (form["RB_ASS_1_1"] == "0")
                {
                    var items = new List<string>();
                    var bone = new List<string>() { "1.頭骨", "2.頂骨", "3.左顳骨", "4.右顳骨", "5.枕骨" };
                    #region 五大判斷
                    for (var i = 1; i <= 5; i++)
                    {
                        var CK_ASS_1_1F = form[$"CK_ASS_1_1F{i}"];
                        if (IsNoEmpty(CK_ASS_1_1F))
                        {
                            var other = new List<string>();

                            var values = CK_ASS_1_1F.Split(',');
                            if (values.Length > 0)
                                foreach (var val in values)
                                {
                                    if (val == "0" && IsNoEmpty(form[$"TXT_{i}_RED"]))
                                        other.Add($"紅腫{form[$"TXT_{i}_RED"]}");

                                    if (val == "1" && IsNoEmpty(form[$"TXT_{i}_BLI"]))
                                        other.Add($"水泡{form[$"TXT_{i}_BLI"]}");

                                    if (val == "2" && IsNoEmpty(form[$"TXT_{i}_HUR"]))
                                        other.Add($"破皮{form[$"TXT_{i}_HUR"]}");

                                    if (val == "3" && IsNoEmpty(form[$"TXT_{i}_CS"]))
                                        other.Add($"產瘤{form[$"TXT_{i}_CS"]}");

                                    if (val == "4" && IsNoEmpty(form[$"TXT_{i}_OTH"]))
                                        other.Add(form[$"TXT_{i}_OTH"]);
                                }
                            items.Add($"{bone[i - 1]}：{String.Join("、", other)}");
                        }
                    }
                    #endregion
                    if (items.Count > 0)
                        exceptionResult.Add($"頭部外觀：{String.Join("、", items)}");
                }
                #endregion

                #region 頭部-囪門
                if (form["RB_ASS_1_2"] == "0")
                {
                    var CK_ASS_1_2F1 = form["CK_ASS_1_2F1"];
                    if (IsNoEmpty(CK_ASS_1_2F1))
                    {
                        var front = new List<string>();
                        var back = new List<string>();
                        var result = "";

                        var values = CK_ASS_1_2F1.Split(',');
                        if (values.Length > 0)
                            result += $"{(headFlag == false ? "頭部" : "")}囪門：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                front.Add("關閉");
                            if (val == "1")
                                front.Add("凹陷");
                            if (val == "2")
                                front.Add("膨出");
                            if (val == "3" && IsNoEmpty(form["TXT_ASS_1_2F2"]))
                                front.Add(form["TXT_ASS_1_2F2"]);

                            if (val == "4")
                                back.Add("關閉");
                            if (val == "5")
                                back.Add("凹陷");
                            if (val == "6")
                                back.Add("膨出");
                            if (val == "7" && IsNoEmpty(form["TXT_ASS_1_2F3"]))
                                back.Add(form["TXT_ASS_1_2F3"]);
                        }
                        if (front.Count > 0)
                            result += "前：" + String.Join("、", front);
                        if (back.Count > 0)
                            result += $"{(front.Count > 0 ? "，" : "")}後：{String.Join("、", back)}";

                        exceptionResult.Add(result);
                    }
                }
                #endregion

                #region 顏面
                if (form["RB_ASS_2"] == "0")
                {
                    var CK_ASS_2F = form["CK_ASS_2F"];
                    if (IsNoEmpty(CK_ASS_2F))
                    {
                        var left = new List<string>();
                        var right = new List<string>();
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_2F.Split(',');
                        result += "顏面：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                result += "不對稱";

                            if (val == "1")
                                left.Add("嘴角下垂");
                            if (val == "2")
                                left.Add("瘜肉");

                            if (val == "3")
                                right.Add("嘴角下垂");
                            if (val == "4")
                                right.Add("瘜肉");

                            if (val == "7" && IsNoEmpty(form["TXT_ASS_2FO"]))
                                other.Add(form["TXT_ASS_2FO"].ToString());
                        }
                        if (left.Count > 0)
                            result += $"，左：{String.Join("、", left)}";
                        if (right.Count > 0)
                            result += $"，右：{String.Join("、", right)}";
                        if (other.Count > 0)
                            result += $"，{String.Join("、", other)}";

                        exceptionResult.Add(result);
                    }
                }
                #endregion

                #region 眼睛
                if (form["RB_ASS_3"] == "0")
                {
                    var CK_ASS_3F = form["CK_ASS_3F"];
                    if (IsNoEmpty(CK_ASS_3F))
                    {
                        var left = new List<string>();
                        var right = new List<string>();
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_3F.Split(',');
                        result += "眼睛：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                left.Add("結膜出血");
                            if (val == "1")
                                left.Add("眼瞼水腫");

                            if (val == "2")
                                right.Add("結膜出血");
                            if (val == "3")
                                right.Add("眼瞼水腫");
                            if (val == "4")
                                other.Add("雙眼距離過大");
                            if (val == "5" && IsNoEmpty(form["TXT_ASS_3FO"]))
                                other.Add(form["TXT_ASS_3FO"].ToString());
                        }
                        if (left.Count > 0)
                            result += $"左：{String.Join("、", left)}";
                        if (right.Count > 0)
                            result += $"{(left.Count > 0 ? "，" : "")}右：{String.Join("、", right)}";
                        if (other.Count > 0)
                            result += $"{(left.Count > 0 || right.Count > 0 ? "，" : "")}{String.Join("、", other)}";

                        exceptionResult.Add(result);
                    }
                }
                #endregion

                #region 耳朵
                if (form["RB_ASS_4"] == "0")
                {
                    var CK_ASS_4F = form["CK_ASS_4F"];
                    if (IsNoEmpty(CK_ASS_4F))
                    {
                        var left = new List<string>();
                        var right = new List<string>();
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_4F.Split(',');
                        result += "耳朵：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                left.Add("低位耳");
                            if (val == "1")
                                left.Add("耳殼異常");
                            if (val == "2")
                                left.Add("瘜肉");

                            if (val == "3")
                                right.Add("低位耳");
                            if (val == "4")
                                right.Add("耳殼異常");
                            if (val == "5")
                                right.Add("瘜肉");

                            if (val == "6" && IsNoEmpty(form["TXT_ASS_4FO"]))
                                other.Add(form["TXT_ASS_4FO"].ToString());
                        }
                        if (left.Count > 0)
                            result += $"左：{String.Join("、", left)}";
                        if (right.Count > 0)
                            result += $"{(left.Count > 0 ? "，" : "")}右：{String.Join("、", right)}";
                        if (other.Count > 0)
                            result += $"{(left.Count > 0 || right.Count > 0 ? "，" : "")}{String.Join("、", other)}";

                        exceptionResult.Add(result);
                    }
                }
                #endregion

                #region 口腔
                if (form["RB_ASS_6"] == "0")
                {
                    var CK_ASS_6F = form["CK_ASS_6F"];
                    if (IsNoEmpty(CK_ASS_6F))
                    {
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_6F.Split(',');
                        result += "口腔：唇裂與顎裂：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                other.Add("單側不完全唇裂");
                            if (val == "1")
                                other.Add("單側完全唇裂與顎裂");
                            if (val == "2")
                                other.Add("雙側完全唇裂與顎裂");
                            if (val == "3")
                                other.Add("顎裂");
                            if (val == "4" && IsNoEmpty(form["TXT_ASS_6FO"]))
                                other.Add(form["TXT_ASS_6FO"].ToString());
                        }
                        result += $"{String.Join("、", other)}";

                        exceptionResult.Add(result);
                    }
                }
                #endregion

                #region 頸部
                if (form["RB_ASS_7"] == "0")
                {
                    var CK_ASS_7F = form["CK_ASS_7F"];
                    if (IsNoEmpty(CK_ASS_7F))
                    {
                        var left = new List<string>();
                        var right = new List<string>();
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_7F.Split(',');
                        result += "頸部：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                left.Add("腫塊");
                            if (val == "1")
                                left.Add("疑斜頸");
                            if (val == "2")
                                left.Add("僵硬");

                            if (val == "3")
                                right.Add("腫塊");
                            if (val == "4")
                                right.Add("疑斜頸");
                            if (val == "5")
                                right.Add("僵硬");

                            if (val == "6" && IsNoEmpty(form["TXT_ASS_7FO"]))
                                other.Add(form["TXT_ASS_7FO"].ToString());
                        }
                        if (left.Count > 0)
                            result += $"左：{String.Join("、", left)}";
                        if (right.Count > 0)
                            result += $"{(left.Count > 0 ? "，" : "")}右：{String.Join("、", right)}";
                        if (other.Count > 0)
                            result += $"{(left.Count > 0 || right.Count > 0 ? "，" : "")}{String.Join("、", other)}";

                        exceptionResult.Add(result);
                    }
                }
                #endregion

                #region 胸部-外觀
                var chestFlag = false;
                if (form["RB_ASS_8_1"] == "0")
                {
                    var CK_ASS_8_1F = form["CK_ASS_8_1F"];
                    if (IsNoEmpty(CK_ASS_8_1F))
                    {
                        var left = new List<string>();
                        var right = new List<string>();
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_8_1F.Split(',');
                        result += "胸部外觀：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                left.Add("腫大");
                            if (val == "1")
                                left.Add("魔乳");

                            if (val == "2")
                                right.Add("腫大");
                            if (val == "3")
                                right.Add("魔乳");

                            if (val == "4" && IsNoEmpty(form["TXT_ASS_8_1FO"]))
                                other.Add(form["TXT_ASS_8_1FO"].ToString());
                        }
                        if (left.Count > 0)
                            result += $"，左：{String.Join("、", left)}";
                        if (right.Count > 0)
                            result += $"，右：{String.Join("、", right)}";
                        if (other.Count > 0)
                            result += $"，{String.Join("、", other)}";

                        exceptionResult.Add(result);
                    }
                }
                #endregion

                #region 胸部-呼吸
                if (form["RB_ASS_8_2"] == "0")
                {
                    var CK_ASS_8_2F = form["CK_ASS_8_2F"];
                    if (IsNoEmpty(CK_ASS_8_2F))
                    {
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_8_2F.Split(',');
                        result += $"{(chestFlag == false ? "胸部" : "")}呼吸：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                other.Add($"{(IsNoEmpty(form["TXT_ASS_8_2FO"]) ? "呼吸急促" + form["TXT_ASS_8_2FO"] + "次/分" : "呼吸急促")}");
                            if (val == "1")
                                other.Add("鼻翼搧動");
                            if (val == "2")
                                other.Add("呻吟聲(grunting)");
                            if (val == "3")
                                other.Add("胸骨凹陷");
                        }
                        result += $"{String.Join("、", other)}";

                        exceptionResult.Add(result);
                        chestFlag = true;
                    }
                }
                #endregion

                #region 胸部-呼吸音
                if (form["RB_ASS_8_3"] == "0")
                {
                    var CK_ASS_8_3F = form["CK_ASS_8_3F"];
                    if (IsNoEmpty(CK_ASS_8_3F))
                    {
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_8_3F.Split(',');
                        result += $"{(chestFlag == false ? "胸部" : "")}呼吸音：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                other.Add("Rhonchi 乾囉音");
                            if (val == "1")
                                other.Add("wheeze 喘嗚音");
                            if (val == "2")
                                other.Add("Bronchial 支氣管音");
                            if (val == "3")
                                other.Add("Rub 摩擦音");
                            if (val == "4")
                                other.Add("Crackles 囉音");
                        }
                        result += $"{String.Join("、", other)}";

                        exceptionResult.Add(result);
                        chestFlag = true;
                    }
                }
                #endregion

                #region 胸部-心臟
                if (form["RB_ASS_8_4"] == "0")
                {
                    var CK_ASS_8_4F = form["CK_ASS_8_4F"];
                    if (IsNoEmpty(CK_ASS_8_4F))
                    {
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_8_4F.Split(',');
                        result += $"{(chestFlag == false ? "胸部" : "")}心臟：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                other.Add("心跳不規則");
                            if (val == "1")
                                other.Add("雜音");
                            if (val == "2" && IsNoEmpty(form["TXT_ASS_8_4FO"]))
                                other.Add(form["TXT_ASS_8_4FO"]);
                        }
                        result += $"{String.Join("、", other)}";

                        exceptionResult.Add(result);
                        chestFlag = true;
                    }
                }
                #endregion

                #region 臀背部
                if (form["RB_ASS_10"] == "0")
                {
                    var CK_ASS_10F = form["CK_ASS_10F"];
                    if (IsNoEmpty(CK_ASS_10F))
                    {
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_10F.Split(',');
                        result += $"臀背部：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                other.Add("毛性小凹");
                            if (val == "1")
                                other.Add("脊髓膜膨出");
                            if (val == "2")
                                other.Add("脊椎彎曲");
                            if (val == "3" && IsNoEmpty(form["TXT_ASS_10FO"]))
                                other.Add(form["TXT_ASS_10FO"]);
                        }
                        result += $"{String.Join("、", other)}";

                        exceptionResult.Add(result);
                    }
                }
                #endregion

                #region 泌尿生殖器外觀-男-陰囊
                var downFlag = false;
                if (form["RB_ASS_11_2"] == "0")
                {
                    var CK_ASS_11F = form["CK_ASS_11F"];
                    if (IsNoEmpty(CK_ASS_11F))
                    {
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_11F.Split(',');
                        result += $"泌尿生殖器外觀男陰囊：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                other.Add("水腫");
                            if (val == "1")
                                other.Add("破皮");
                            if (val == "2" && IsNoEmpty(form["TXT_ASS_11_2FO"]))
                                other.Add(form["TXT_ASS_11_2FO"]);
                        }
                        result += $"{String.Join("、", other)}";

                        exceptionResult.Add(result);
                        downFlag = true;
                    }
                }
                #endregion

                #region 泌尿生殖器外觀-肛門
                if (form["RB_ASS_11_8"] == "0")
                {
                    var CK_ASS_11_8F = form["CK_ASS_11_8F"];
                    if (IsNoEmpty(CK_ASS_11_8F))
                    {
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_11_8F.Split(',');
                        result += $"{(downFlag == false ? "泌尿生殖器外觀" : "")}肛門：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                other.Add("無肛");
                            if (val == "1")
                                other.Add("閉鎖");
                            if (val == "2")
                                other.Add("瘜肉");
                            if (val == "3")
                                other.Add("裂隙");
                            if (val == "4" && IsNoEmpty(form["TXT_ASS_11_8FO"]))
                                other.Add(form["TXT_ASS_11_8FO"]);
                        }
                        result += $"{String.Join("、", other)}";

                        exceptionResult.Add(result);
                        downFlag = true;
                    }
                }
                #endregion

                #region 四肢
                var feetFlag = false;
                if (form["RB_ASS_16"] == "0")
                {
                    #region 足內翻
                    if (form["RB_ASS_16_1"] == "1")
                    {
                        var CK_ASS_16_1F = form["CK_ASS_16_1F"];
                        if (IsNoEmpty(CK_ASS_16_1F))
                        {
                            var other = new List<string>();
                            var result = "";

                            var values = CK_ASS_16_1F.Split(',');
                            result += $"{(feetFlag == false ? "四肢：" : "")}足內翻：";
                            foreach (var val in values)
                            {
                                if (val == "0")
                                    other.Add("左腳");
                                if (val == "1")
                                    other.Add("右腳");
                            }

                            result += $"{String.Join("、", other)}";

                            exceptionResult.Add(result);
                            feetFlag = true;
                        }
                    }
                    #endregion

                    #region 足外翻
                    if (form["RB_ASS_16_2"] == "1")
                    {
                        var CK_ASS_16_2F = form["CK_ASS_16_2F"];
                        if (IsNoEmpty(CK_ASS_16_2F))
                        {
                            var other = new List<string>();
                            var result = "";

                            var values = CK_ASS_16_2F.Split(',');
                            result += $"{(feetFlag == false ? "四肢：" : "")}足外翻：";
                            foreach (var val in values)
                            {
                                if (val == "0")
                                    other.Add("左腳");
                                if (val == "1")
                                    other.Add("右腳");
                            }

                            result += $"{String.Join("、", other)}";

                            exceptionResult.Add(result);
                            feetFlag = true;
                        }
                    }
                    #endregion

                    #region 單一手紋(斷掌)
                    if (form["RB_ASS_16_3"] == "1")
                    {
                        var CK_ASS_16_3F = form["CK_ASS_16_3F"];
                        if (IsNoEmpty(CK_ASS_16_3F))
                        {
                            var other = new List<string>();
                            var result = "";

                            var values = CK_ASS_16_3F.Split(',');
                            result += $"{(feetFlag == false ? "四肢：" : "")}單一手紋(斷掌)：";
                            foreach (var val in values)
                            {
                                if (val == "0")
                                    other.Add("左手");
                                if (val == "1")
                                    other.Add("右手");
                            }

                            result += $"{String.Join("、", other)}";

                            exceptionResult.Add(result);
                            feetFlag = true;
                        }
                    }
                    #endregion
                }
                #endregion

                #region 四肢-手指
                if (form["RB_ASS_16_7"] == "0")
                {
                    var CK_ASS_16_7F = form["CK_ASS_16_7F"];
                    if (IsNoEmpty(CK_ASS_16_7F))
                    {
                        var left = new List<string>();
                        var right = new List<string>();
                        var result = "";

                        var values = CK_ASS_16_7F.Split(',');
                        result += $"{(feetFlag == false ? "四肢 : " : "")}手指：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                left.Add($"{(IsNoEmpty(form["TXT_ASS_16_7F1"]) ? "少指，部位：" + form["TXT_ASS_16_7F1"] : "少指")}");
                            if (val == "1")
                                left.Add($"{(IsNoEmpty(form["TXT_ASS_16_7F2"]) ? "多指，部位：" + form["TXT_ASS_16_7F2"] : "多指")}");
                            if (val == "2")
                                left.Add($"{(IsNoEmpty(form["TXT_ASS_16_7F3"]) ? "併指，部位：" + form["TXT_ASS_16_7F3"] : "併指")}");
                            if (val == "3")
                                left.Add($"{(IsNoEmpty(form["TXT_ASS_16_7F4"]) ? "蹼狀指，部位：" + form["TXT_ASS_16_7F4"] : "蹼狀指")}");

                            if (val == "4")
                                right.Add($"{(IsNoEmpty(form["TXT_ASS_16_7F5"]) ? "少指，部位：" + form["TXT_ASS_16_7F5"] : "少指")}");
                            if (val == "5")
                                right.Add($"{(IsNoEmpty(form["TXT_ASS_16_7F6"]) ? "多指，部位：" + form["TXT_ASS_16_7F6"] : "多指")}");
                            if (val == "6")
                                right.Add($"{(IsNoEmpty(form["TXT_ASS_16_7F7"]) ? "併指，部位：" + form["TXT_ASS_16_7F7"] : "併指")}");
                            if (val == "7")
                                right.Add($"{(IsNoEmpty(form["TXT_ASS_16_7F8"]) ? "蹼狀指，部位：" + form["TXT_ASS_16_7F8"] : "蹼狀指")}");
                        }

                        if (left.Count > 0)
                            result += $"左：{String.Join("、", left)}";
                        if (right.Count > 0)
                            result += $"{(left.Count > 0 ? "。" : "")}右：{String.Join("、", right)}";

                        exceptionResult.Add(result);
                        feetFlag = true;
                    }
                }
                #endregion

                #region 四肢-腳趾
                if (form["RB_ASS_16_8"] == "0")
                {
                    var CK_ASS_16_8F = form["CK_ASS_16_8F"];
                    if (IsNoEmpty(CK_ASS_16_8F))
                    {
                        var left = new List<string>();
                        var right = new List<string>();
                        var result = "";

                        var values = CK_ASS_16_8F.Split(',');
                        result += $"{(feetFlag == false ? "四肢 : " : "")}腳趾：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                left.Add($"{(IsNoEmpty(form["TXT_ASS_16_8F1"]) ? "少趾，部位：" + form["TXT_ASS_16_8F1"] : "少趾")}");
                            if (val == "1")
                                left.Add($"{(IsNoEmpty(form["TXT_ASS_16_8F2"]) ? "多趾，部位：" + form["TXT_ASS_16_8F2"] : "多趾")}");
                            if (val == "2")
                                left.Add($"{(IsNoEmpty(form["TXT_ASS_16_8F3"]) ? "併趾，部位：" + form["TXT_ASS_16_8F3"] : "併趾")}");

                            if (val == "3")
                                right.Add($"{(IsNoEmpty(form["TXT_ASS_16_8F4"]) ? "少趾，部位：" + form["TXT_ASS_16_8F4"] : "少趾")}");
                            if (val == "4")
                                right.Add($"{(IsNoEmpty(form["TXT_ASS_16_8F5"]) ? "多趾，部位：" + form["TXT_ASS_16_8F5"] : "多趾")}");
                            if (val == "5")
                                right.Add($"{(IsNoEmpty(form["TXT_ASS_16_8F6"]) ? "併趾，部位：" + form["TXT_ASS_16_8F6"] : "併趾")}");
                        }

                        if (left.Count > 0)
                            result += $"左：{String.Join("、", left)}";
                        if (right.Count > 0)
                            result += $"{(left.Count > 0 ? "。" : "")}右：{String.Join("、", right)}";

                        exceptionResult.Add(result);
                        feetFlag = true;
                    }
                }
                #endregion

                if (exceptionResult.Count > 0)
                    finalResult += String.Join("。", exceptionResult);
                else
                    finalResult += "無異常項目";

                if (form["type"].ToString() != "暫存")
                {
                    base.Insert_CareRecordTns(dateNow, id, "新生兒入院護理評估", finalResult + "。", "", "", "", "", "OBS_BABYENTR", ref link);
                }

                #endregion

                #region 拋轉TOCC
                string TOCCid = creatid("TOCC_DATA", userno, feeno, "0");

                var TOCCmsg = "";
                //症狀
                TOCCmsg += "病人目前";
                if(form["CK_SYM"] != "" && form["CK_SYM"] != null)
                {
                    if (form["CK_SYM"] == "0" || form["CK_SYM"] == "")
                    {
                        TOCCmsg += "無症狀。";
                    }
                    else
                    {
                        var symptomArr = form["CK_SYM"].Split(',');
                        var symptomTransArr = new List<string>();

                        foreach (var val in symptomArr)
                        {
                            if (val == "1")
                                symptomTransArr.Add("發燒(≥38℃)");
                            if (val == "2")
                                symptomTransArr.Add("咳嗽");
                            if (val == "3")
                                symptomTransArr.Add("喘");
                            if (val == "4")
                                symptomTransArr.Add("流鼻水");
                            if (val == "5")
                                symptomTransArr.Add("鼻塞");
                            if (val == "6")
                                symptomTransArr.Add("喉嚨痛");
                            if (val == "7")
                                symptomTransArr.Add("肌肉痠痛");
                            if (val == "8")
                                symptomTransArr.Add("頭痛");
                            if (val == "9")
                                symptomTransArr.Add("極度疲倦感");
                            //if (val == "10")
                                //symptomTransArr.Add("其他");
                        }


                        if (form["TXT_SYM_OTH"].ToString() != "" && form["TXT_SYM_OTH"] != null)
                        {
                            symptomTransArr.Add(form["TXT_SYM_OTH"].ToString());
                        }
                        TOCCmsg += String.Join("、", symptomTransArr);

                        TOCCmsg += "。";
                    }
                }

                //旅遊史(Travel)
                TOCCmsg += "旅遊史(Travel)：最近14日內";

                if (form["CK_TRAV"] == "0")
                {
                    TOCCmsg += "無國內、外旅遊。";
                }
                else
                {
                    if(form["CK_TRAV"] != "" && form["CK_TRAV"]　!= null)
                    {
                        string travel = form["CK_TRAV"].ToString();
                        var travleArr = travel.Split(',');
                        for (int i = 0; i < travleArr.Count(); i++)
                        {
                            switch (travleArr[i])
                            {
                                case "1":
                                    TOCCmsg += "國內旅遊";
                                    TOCCmsg += "(";
                                    if (form["TXT_TRAV_DOME_CITY"] != "")
                                    {
                                        TOCCmsg += form["TXT_TRAV_DOME_CITY"];
                                    }
                                    if (form["TXT_TRAV_DOME_CITY"] != "" && form["TXT_TRAV_DOME_VIEW"] != "")
                                    {
                                        TOCCmsg += "，";
                                    }
                                    if (form["TXT_TRAV_DOME_VIEW"] != "")
                                    {
                                        TOCCmsg += form["TXT_TRAV_DOME_VIEW"];
                                    }
                                    if (form["TXT_TRAV_DOME_CITY"] != "" || form["TXT_TRAV_DOME_VIEW"] != "")
                                    {
                                        TOCCmsg += "，";
                                    }
                                    TOCCmsg += "交通方式：";

                                    var traffic = form["CK_TRAV_DOME_TRAF"].Split(',');
                                    var trafficTransArr = new List<string>();

                                    foreach (var val in traffic)
                                    {
                                        if (val == "0")
                                            trafficTransArr.Add("大眾運輸");
                                        if (val == "1")
                                            trafficTransArr.Add("自行駕駛");
                                        //if (val == "2")
                                        //    trafficTransArr.Add("其他");

                                    }
                                    if (form["TXT_TRAV_DOME_TRAF_OTH"] != "")
                                    {
                                        trafficTransArr.Add(form["TXT_TRAV_DOME_TRAF_OTH"]);
                                    }
                                    TOCCmsg += String.Join("、", trafficTransArr);


                                    if (travleArr.Count() > 1)
                                    {
                                        TOCCmsg += "、";
                                    }
                                    break;
                                case "2":
                                    TOCCmsg += "國外旅遊(包含轉機或船舶停靠曾到訪)";
                                    TOCCmsg += "(";
                                    if (form["TXT_TRAV_ABO_COUN"] != "")
                                    {
                                        TOCCmsg += form["TXT_TRAV_ABO_COUN"];
                                    }
                                    if (form["TXT_TRAV_ABO_COUN"] != "" && form["TXT_TRAV_ABO_DEST"] != "")
                                    {
                                        TOCCmsg += "，";
                                    }
                                    if (form["TXT_TRAV_ABO_DEST"] != "")
                                    {
                                        TOCCmsg += form["TXT_TRAV_ABO_DEST"];
                                    }
                                    if (form["TXT_TRAV_ABO_COUN"] != "" || form["TXT_TRAV_ABO_DEST"] != "")
                                    {
                                        TOCCmsg += "，";
                                    }
                                    TOCCmsg += "交通方式：";

                                    var trafficAbo = form["CK_TRAV_ABO_TRAF"].Split(',');
                                    var trafficTransAboArr = new List<string>();

                                    foreach (var val in trafficAbo)
                                    {
                                        if (val == "0")
                                            trafficTransAboArr.Add("大眾運輸");
                                        if (val == "1")
                                            trafficTransAboArr.Add("自行駕駛");

                                    }
                                    if (form["TXT_TRAV_ABO_TRAF_OTH"] != "" && form["TXT_TRAV_ABO_TRAF_OTH"] != null)
                                    {
                                        trafficTransAboArr.Add(form["TXT_TRAV_ABO_TRAF_OTH"]);
                                    }
                                    TOCCmsg += String.Join("、", trafficTransAboArr);

                                    break;
                            }
                        }
                    }
                    TOCCmsg += "。";
                }
                //職業別
                TOCCmsg += "職業別(Occupation)：";
                if(form["CK_OCCU"] != "" && form["CK_OCCU"] != null)
                {
                    var occuArr = form["CK_OCCU"].Split(',');
                    var occuTransArr = new List<string>();

                    foreach (var val in occuArr)
                    {
                        if (val == "0")
                            occuTransArr.Add("無");
                        if (val == "1")
                            occuTransArr.Add("醫事機構工作者");
                        if (val == "2")
                            occuTransArr.Add("禽畜販賣業者");
                        if (val == "3")
                            occuTransArr.Add("航空服務業工作者");
                    }
                    if (form["TXT_OCCU_OTH"] != "" && form["TXT_OCCU_OTH"] != null)
                    {
                        occuTransArr.Add(form["TXT_OCCU_OTH"]);
                    }
                    TOCCmsg += String.Join("、", occuTransArr);
                    TOCCmsg += "。";
                }

                //接觸史
                TOCCmsg += "接觸史(Contact)：";

                if((form["CK_CONT"] != "" && form["CK_CONT"] != null )|| (form["RB_CONT_OBS_SYM"] != null && form["RB_CONT_OBS_SYM"] != ""))
                {
                    if (form["CK_CONT"] == "0")
                    {
                        TOCCmsg += "無。";
                    }
                    else
                    {
                        if(form["CK_CONT"] != "" && form["CK_CONT"] != null)
                        {
                            string contact = form["CK_CONT"].ToString();

                            var contactArr = contact.Split(',');


                            if (contactArr.Contains("1"))
                            {
                                TOCCmsg += "接觸禽鳥類、畜類等 : ";
                                if (form["CK_CONT_BIRD"] != "" && form["CK_CONT_BIRD"] != null)
                                {
                                    var birdArr = form["CK_CONT_BIRD"].Split(',');
                                    var tempBird = new List<string>();
                                    foreach (var val in birdArr)
                                    {
                                        if (val == "0")
                                            tempBird.Add("禽鳥類接觸：如雞、鴨等");
                                        if (val == "1")
                                            tempBird.Add("畜類接觸：如豬、貓、狗等");
                                    }
                                    TOCCmsg += "(" + String.Join("、", tempBird) + ")";
                                }
                                TOCCmsg += "。";
                            }

                        }

                        TOCCmsg += "新生兒接觸史：";
                        if (form["RB_CONT_OBS_SYM"] != "" && form["RB_CONT_OBS_SYM"] != null)
                        {
                            TOCCmsg += "(1) 生產前 14 天內，產婦或同住家人有發燒、腹瀉、咳嗽、流鼻水等疑似感染症狀 : ";
                            if(form["RB_CONT_OBS_SYM"] == "0")
                            {
                                TOCCmsg += "無。";
                            }
                            else
                            {
                                TOCCmsg += "有。";
                            }
                        }
                        if (form["RB_CONT_OBS_SICKLEAVE"] != "" && form["RB_CONT_OBS_SICKLEAVE"] != null)
                        {
                            TOCCmsg += "(2) 生產前 14 天內，產婦或同住家人學校班上同學有因為傳染病請假或班級停課之情形 : ";
                            if (form["RB_CONT_OBS_SICKLEAVE"] == "0")
                            {
                                TOCCmsg += "無。";
                            }
                            else
                            {
                                TOCCmsg += "有。";
                            }
                        }
                        if (form["RB_CONT_OBS_CARESYM"] != "" && form["RB_CONT_OBS_CARESYM"] != null)
                        {
                            TOCCmsg += "(3) 住院期間照顧者(應盡量維持同一人)，目前有發燒、腹瀉、咳嗽、流鼻水等疑似感染症狀 : ";
                            if (form["RB_CONT_OBS_CARESYM"] == "0")
                            {
                                TOCCmsg += "無。";
                            }
                            else
                            {
                                TOCCmsg += "有。";
                            }
                        }
                        if (form["CK_CONT"] != "" && form["CK_CONT"] != null)
                        {
                            string contact = form["CK_CONT"].ToString();

                            var contactArr = contact.Split(',');
                            if (contactArr.Contains("2"))
                            {
                                if (form["TXT_CONT_OTH"] != "" && form["TXT_CONT_OTH"] != null)
                                {
                                    TOCCmsg += form["TXT_CONT_OTH"] + "。";
                                }
                            }
                        }
                    }
                }
               
                //群聚史(Cluster)
                TOCCmsg += "群聚史(Cluster)：";
                if (form["RB_CLU"] != "" && form["RB_CLU"] != null)
                {
                    if (form["RB_CLU"] == "0")
                    {
                        TOCCmsg += "無。";
                    }
                    else
                    {
                        TOCCmsg += "家人/朋友/同事有發燒或類流感症狀：";
                        if (form["CK_CLU_RELA"] != "" && form["CK_CLU_RELA"] != null)
                        {
                            var clusterArr = form["CK_CLU_RELA"].Split(',');
                            var clusterTransArr = new List<string>();

                            foreach (var val in clusterArr)
                            {
                                if (val == "0")
                                    clusterTransArr.Add("家人");
                                if (val == "1")
                                    clusterTransArr.Add("朋友");
                                if (val == "2")
                                    clusterTransArr.Add("同事");
  
                            }
                            TOCCmsg += String.Join("、", clusterTransArr);
                            if (form["TXT_CLU_RELA_OTH"] != "" && form["TXT_CLU_RELA_OTH"] != null)
                            {
                                TOCCmsg += ":" + form["TXT_CLU_RELA_OTH"];
                            }
                        }
                        TOCCmsg += "。";
                    }
                }

                if (TOCCmsg != "" && form["type"].ToString() != "暫存")
                {
                    erow += base.Insert_CareRecord(dateNow, TOCCid, "TOCC評估", TOCCmsg, "", "", "", "", "TOCC");
                }
                #endregion


                mom_feeno = "";
                var seq = '0';
                if (ptinfo.Age < 1)
                {
                    DataTable babylink = obs_m.sel_nbcha("", ptinfo.ChartNo);
                    if (babylink != null && babylink.Rows.Count > 0)
                    {
                        mom_feeno = babylink.Rows[0]["MOM_FEE_NO"].ToString();
                        seq = Convert.ToChar(babylink.Rows[0]["BABY_SEQ"].ToString());

                        #region 出生紀錄護理紀錄
                        DataRow bthstaRow = null;
                        DataTable stadt = obs_m.sel_bthsta(mom_feeno);
                        if (stadt != null)
                            bthstaRow = stadt.Rows[0];

                        DataRow nbRow = null;
                        DataTable nbdt = obs_m.sel_nb(mom_feeno, ptinfo.ChartNo);
                        if (nbdt != null)
                            nbRow = nbdt.Rows[0];

                        DataTable nbcarerecord = obs_m.sel_carerecord(ptinfo.FeeNo, $"新生兒出生紀錄-{(ptinfo.Age < 1 ? "小孩" : "母親")}");
                        if (nbcarerecord == null || nbcarerecord.Rows.Count == 0)
                        {
                            var content = $"出生時間：{Convert.ToDateTime(nbRow["BIRTH_DAY"].ToString()).ToString("yyyy/MM/dd HH:mm")}。";
                            content += $"性別：{nbRow["GENDER"]}。生產方式：{(nbRow["BIRTH_TYPE"].ToString() == "0" ? "陰道產" : "剖腹產")}。";
                            content += $"妊娠週數：{bthstaRow["GEST_M"].ToString()}+{bthstaRow["GEST_D"].ToString()}週。";
                            content += $"身高：{(nbRow["HEIGHT"].ToString() == "" ? "0" : nbRow["HEIGHT"].ToString())}公分。體重：{(nbRow["WEIGHT"].ToString() == "" ? "0" : nbRow["WEIGHT"].ToString())}公克。";
                            content += $"頭圍：{(nbRow["HEAD"].ToString() == "" ? "0" : nbRow["HEAD"].ToString())}公分。胸圍：{(nbRow["CHEST"].ToString() == "" ? "0" : nbRow["CHEST"].ToString())}公分。";

                            var apgar = new List<string>();

                            if (nbRow["RB_AS_1_1"].ToString() != "" || nbRow["RB_AS_1_2"].ToString() != "" || nbRow["RB_AS_1_3"].ToString() != ""
                                || nbRow["RB_AS_1_4"].ToString() != "" || nbRow["RB_AS_1_5"].ToString() != "")
                            {
                                apgar.Add($"第一分鐘：{(nbRow["RB_AS_1_NUM"].ToString() == "" ? "0" : nbRow["RB_AS_1_NUM"])}分");
                            }

                            if (nbRow["RB_AS_5_1"].ToString() != "" || nbRow["RB_AS_5_2"].ToString() != "" || nbRow["RB_AS_5_3"].ToString() != ""
                            || nbRow["RB_AS_5_4"].ToString() != "" || nbRow["RB_AS_5_5"].ToString() != "")
                            {
                                apgar.Add($"第五分鐘：{(nbRow["RB_AS_5_NUM"].ToString() == "" ? "0" : nbRow["RB_AS_5_NUM"])}分");
                            }

                            if (nbRow["RB_AS_10_1"].ToString() != "" || nbRow["RB_AS_10_2"].ToString() != "" || nbRow["RB_AS_10_3"].ToString() != ""
                            || nbRow["RB_AS_10_4"].ToString() != "" || nbRow["RB_AS_10_5"].ToString() != "")
                            {
                                apgar.Add($"第十分鐘：{(nbRow["RB_AS_10_NUM"].ToString() == "" ? "0" : nbRow["RB_AS_10_NUM"])}分");
                            }
                            if (apgar.Count() > 0)
                                content += $"Apgar Score：{String.Join("、", apgar)}";


                            //小孩多急救處置
                            if (ptinfo.Age < 1)
                            {
                                DataTable child_emrdt = obs_m.sel_nb_emr(mom_feeno, "", (Convert.ToInt32(seq) - 64).ToString());
                                if (child_emrdt != null && child_emrdt.Rows.Count > 0)
                                {
                                    foreach (DataRow r in child_emrdt.Rows)
                                    {
                                        content += r["ET"].ToString() + "。";
                                    }
                                }
                            }

                            base.Insert_CareRecordTns(Convert.ToDateTime(nbRow["BIRTH_DAY"].ToString()).ToString("yyyy/MM/dd HH:mm"), nbRow["IID"].ToString(), $"新生兒出生紀錄-{(ptinfo.Age < 1 ? "小孩" : "母親")}", content, "", "", "", "", "OBS_NB", ref link);
                        }
                        #endregion

                        #region 母嬰護理轉護理記錄
                        DataTable patnbcarerecord = obs_m.sel_carerecord(ptinfo.FeeNo, $"母嬰護理及飲食排泄紀錄");
                        if (patnbcarerecord == null || patnbcarerecord.Rows.Count == 0)
                        {
                            DataTable patnbdt = obs_m.sel_Breast_Fedding(mom_feeno, "", "");
                            if (patnbdt != null && patnbdt.Rows.Count > 0)
                            {
                                foreach (DataRow r in patnbdt.Rows)
                                {
                                    var baby_feeno = r["NB_SEQ_FEENO"].ToString().Split(',')[2];

                                    if (baby_feeno.Trim() == ptinfo.FeeNo.Trim())
                                    {
                                        #region 帶入IO
                                        // 含乳時間IO
                                        if (r["BRE_TSS2"].ToString() != "" && IsNoEmpty(baby_feeno))
                                        {
                                            Insert_IO_DATA(Convert.ToDateTime(r["RECORDTIME"].ToString()).ToString("yyyy/MM/dd HH:mm:ss"), id + "_11", "11", "", r["BRE_TSS2"].ToString(), "4", id + "_BRE_TSS2", "OBS_PATNB");
                                        }
                                        // 水IO
                                        if (r["WATER"].ToString() != "" && IsNoEmpty(baby_feeno))
                                        {
                                            var ioSql = "SELECT * FROM IO_ITEM WHERE TYPEID = '2' AND NAME = '水'";
                                            var io_dt = obs_m.DBExecSQL(ioSql);
                                            if (io_dt != null && io_dt.Rows.Count > 0)
                                            {
                                                var itemid = io_dt.Rows[0]["ITEMID"].ToString();
                                                Insert_IO_DATA(Convert.ToDateTime(r["RECORDTIME"].ToString()).ToString("yyyy/MM/dd HH:mm:ss"), id + "_2", "2", itemid, r["WATER"].ToString(), "1", id + "_WATER", "OBS_PATNB");
                                            }
                                        }
                                        //母乳IO
                                        if (r["BRE_MLK"].ToString() != "" && IsNoEmpty(baby_feeno))
                                        {
                                            var ioSql = "SELECT * FROM IO_ITEM WHERE TYPEID = '2' AND NAME = '母奶'";
                                            var io_dt = obs_m.DBExecSQL(ioSql);
                                            if (io_dt != null && io_dt.Rows.Count > 0)
                                            {
                                                var itemid = io_dt.Rows[0]["ITEMID"].ToString();
                                                Insert_IO_DATA(Convert.ToDateTime(r["RECORDTIME"].ToString()).ToString("yyyy/MM/dd HH:mm:ss"), id + "_2", "2", itemid, r["BRE_MLK"].ToString(), "1", id + "_BRE_MLK", "OBS_PATNB");
                                            }
                                        }
                                        //配方奶IO
                                        if (r["MILK"].ToString() != "" && IsNoEmpty(baby_feeno))
                                        {
                                            var ioSql = "SELECT * FROM IO_ITEM WHERE TYPEID = '2' AND NAME = '配方奶'";
                                            var io_dt = obs_m.DBExecSQL(ioSql);
                                            if (io_dt != null && io_dt.Rows.Count > 0)
                                            {
                                                var itemid = io_dt.Rows[0]["ITEMID"].ToString();
                                                Insert_IO_DATA(Convert.ToDateTime(r["RECORDTIME"].ToString()).ToString("yyyy/MM/dd HH:mm:ss"), id + "_2", "2", itemid, r["MILK"].ToString(), "1", id + "_MILK", "OBS_PATNB");
                                            }
                                        }
                                        //D5W
                                        if (r["D5W"].ToString() != "" && IsNoEmpty(baby_feeno))
                                        {
                                            var ioSql = "SELECT * FROM IO_ITEM WHERE TYPEID = '2' AND NAME = 'D5W'";
                                            var io_dt = obs_m.DBExecSQL(ioSql);
                                            if (io_dt != null && io_dt.Rows.Count > 0)
                                            {
                                                var itemid = io_dt.Rows[0]["ITEMID"].ToString();
                                                Insert_IO_DATA(Convert.ToDateTime(r["RECORDTIME"].ToString()).ToString("yyyy/MM/dd HH:mm:ss"), id + "_2", "2", itemid, r["D5W"].ToString(), "1", id + "_D5W", "OBS_PATNB");
                                            }
                                        }
                                        //排便IO
                                        if (r["DEF_FRQ"].ToString() != "" && IsNoEmpty(baby_feeno))
                                        {
                                            Insert_IO_DATA(Convert.ToDateTime(r["RECORDTIME"].ToString()).ToString("yyyy/MM/dd HH:mm:ss"), id + "_12", "12", "", r["DEF_FRQ"].ToString(), "5", id + "_DEF_FRQ", "OBS_PATNB");
                                            Insert_IO_Additional(id + "_12", (r["DEF_COL"].ToString() == "99" && !string.IsNullOrWhiteSpace(r["DEF_COL"].ToString())) ? r["DEF_COL"].ToString().Trim() : "-1", r["DEF_CHA"].ToString(), (r["DEF_CHA"].ToString() == "99" && !string.IsNullOrWhiteSpace(r["DEF_CHA"].ToString())) ? r["DEF_CHA"].ToString().Trim() : "-1", "", "", "");
                                        }
                                        //排尿IO
                                        if (r["URI_FRQ"].ToString() != "" && IsNoEmpty(baby_feeno))
                                        {
                                            Insert_IO_DATA(Convert.ToDateTime(r["RECORDTIME"].ToString()).ToString("yyyy/MM/dd HH:mm:ss"), id + "_13", "13", "", r["URI_FRQ"].ToString(), "5", id + "_URI_FRQ", "OBS_PATNB");
                                            Insert_IO_Additional(id + "_13", (r["URI_COL"].ToString() == "99" && !string.IsNullOrWhiteSpace(r["URI_COL"].ToString())) ? r["URI_COL"].ToString().Trim() : "-1", r["URI_CHA"].ToString(), (r["URI_CHA"].ToString() == "99" && !string.IsNullOrWhiteSpace(r["URI_CHA"].ToString())) ? r["URI_CHA"].ToString().Trim() : "-1", "", "", "");
                                        }
                                        #endregion

                                        if (r["NB_CARE_RECORD"].ToString() == "1")
                                        {
                                            var content = new List<string>();
                                            var semicolon = new List<string>();
                                            var field = "";
                                            var commaFlag = false;

                                            #region 乳房
                                            field = "#from#評估產婦乳房";
                                            if (IsNoEmpty(r["BREAST"].ToString()))
                                            {
                                                var BREAST_ITM = (r["BREAST"].ToString() ?? "");
                                                var BREASTs = new List<string>();

                                                for (var i = 0; i < BREAST_ITM.ToString().Length; i++)
                                                {
                                                    if (BREAST_ITM[i] == '1')
                                                    {
                                                        BREASTs.Add(i.ToString());
                                                    }
                                                }

                                                var L_BREAST = new List<string>();
                                                var R_BREAST = new List<string>();

                                                foreach (var i in BREASTs)
                                                {
                                                    if (Convert.ToInt32(i) < 5)
                                                        L_BREAST.Add(i);
                                                    else
                                                        R_BREAST.Add((Convert.ToInt32(i) - 5).ToString());
                                                }

                                                //左右相同
                                                if (String.Join(",", L_BREAST) == String.Join(",", R_BREAST))
                                                {
                                                    commaFlag = false;
                                                    foreach (var i in L_BREAST)
                                                    {
                                                        if (commaFlag)
                                                            field += "、";

                                                        switch (i)
                                                        {
                                                            case "0":
                                                                field += "鬆軟";
                                                                break;
                                                            case "1":
                                                                field += "充盈";
                                                                break;
                                                            case "2":
                                                                field += "緊繃";
                                                                break;
                                                            case "3":
                                                                field += "腫脹";
                                                                break;
                                                            case "4":
                                                                field += "硬";
                                                                break;
                                                            default:
                                                                field += "";
                                                                break;
                                                        }

                                                        commaFlag = true;
                                                    }
                                                }
                                                else
                                                {
                                                    //左邊
                                                    commaFlag = false;
                                                    field += "左";
                                                    if (L_BREAST == null)
                                                        field += "未填寫";
                                                    else
                                                    {
                                                        foreach (var i in L_BREAST)
                                                        {
                                                            if (commaFlag)
                                                                field += "、";

                                                            switch (i)
                                                            {
                                                                case "0":
                                                                    field += "鬆軟";
                                                                    break;
                                                                case "1":
                                                                    field += "充盈";
                                                                    break;
                                                                case "2":
                                                                    field += "緊繃";
                                                                    break;
                                                                case "3":
                                                                    field += "腫脹";
                                                                    break;
                                                                case "4":
                                                                    field += "硬";
                                                                    break;
                                                                default:
                                                                    field += "";
                                                                    break;
                                                            }
                                                            commaFlag = true;
                                                        }
                                                    }
                                                    //右邊
                                                    commaFlag = false;
                                                    field += "、右";
                                                    if (R_BREAST == null)
                                                        field += "未填寫";
                                                    else
                                                    {
                                                        foreach (var i in R_BREAST)
                                                        {
                                                            if (commaFlag)
                                                                field += "、";

                                                            switch (i)
                                                            {
                                                                case "0":
                                                                    field += "鬆軟";
                                                                    break;
                                                                case "1":
                                                                    field += "充盈";
                                                                    break;
                                                                case "2":
                                                                    field += "緊繃";
                                                                    break;
                                                                case "3":
                                                                    field += "腫脹";
                                                                    break;
                                                                case "4":
                                                                    field += "硬";
                                                                    break;
                                                                default:
                                                                    field += "";
                                                                    break;
                                                            }
                                                            commaFlag = true;
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                field += "未填寫";
                                            }
                                            content.Add(field);
                                            #endregion

                                            #region 乳頭
                                            field = "乳頭";
                                            if (IsNoEmpty(r["NIPPLE"].ToString()))
                                            {
                                                var NIPPLE_ITM = (r["NIPPLE"].ToString() ?? "");
                                                var NIPPLEs = new List<string>();

                                                for (var i = 0; i < NIPPLE_ITM.ToString().Length; i++)
                                                {
                                                    if (NIPPLE_ITM[i] == '1')
                                                    {
                                                        NIPPLEs.Add(i.ToString());
                                                    }
                                                }

                                                var L_NIPPLE = new List<string>();
                                                var R_NIPPLE = new List<string>();

                                                foreach (var i in NIPPLEs)
                                                {
                                                    if (Convert.ToInt32(i) < 8)
                                                        L_NIPPLE.Add(i);
                                                    else
                                                        R_NIPPLE.Add((Convert.ToInt32(i) - 8).ToString());
                                                }

                                                //左右相同
                                                if (String.Join(",", L_NIPPLE) == String.Join(",", R_NIPPLE))
                                                {
                                                    commaFlag = false;
                                                    foreach (var i in L_NIPPLE)
                                                    {
                                                        if (commaFlag)
                                                            field += "、";

                                                        switch (i)
                                                        {
                                                            case "0":
                                                                field += "正常";
                                                                break;
                                                            case "1":
                                                                field += "凹";
                                                                break;
                                                            case "2":
                                                                field += "平";
                                                                break;
                                                            case "3":
                                                                field += "短";
                                                                break;
                                                            case "4":
                                                                field += "大";
                                                                break;
                                                            case "5":
                                                                field += "小";
                                                                break;
                                                            case "6":
                                                                field += "破皮";
                                                                break;
                                                            case "7":
                                                                field += "結痂";
                                                                break;
                                                            default:
                                                                field += "";
                                                                break;
                                                        }

                                                        commaFlag = true;
                                                    }
                                                }
                                                else
                                                {
                                                    //左邊
                                                    commaFlag = false;
                                                    field += "左";
                                                    if (L_NIPPLE == null)
                                                        field += "未填寫";
                                                    else
                                                    {
                                                        foreach (var i in L_NIPPLE)
                                                        {
                                                            if (commaFlag)
                                                                field += "、";

                                                            switch (i)
                                                            {
                                                                case "0":
                                                                    field += "正常";
                                                                    break;
                                                                case "1":
                                                                    field += "凹";
                                                                    break;
                                                                case "2":
                                                                    field += "平";
                                                                    break;
                                                                case "3":
                                                                    field += "短";
                                                                    break;
                                                                case "4":
                                                                    field += "大";
                                                                    break;
                                                                case "5":
                                                                    field += "小";
                                                                    break;
                                                                case "6":
                                                                    field += "破皮";
                                                                    break;
                                                                case "7":
                                                                    field += "結痂";
                                                                    break;
                                                                default:
                                                                    field += "";
                                                                    break;
                                                            }
                                                            commaFlag = true;
                                                        }
                                                    }
                                                    //右邊
                                                    commaFlag = false;
                                                    field += "、右";
                                                    if (R_NIPPLE == null)
                                                        field += "未填寫";
                                                    else
                                                    {
                                                        foreach (var i in R_NIPPLE)
                                                        {
                                                            if (commaFlag)
                                                                field += "、";

                                                            switch (i)
                                                            {
                                                                case "0":
                                                                    field += "正常";
                                                                    break;
                                                                case "1":
                                                                    field += "凹";
                                                                    break;
                                                                case "2":
                                                                    field += "平";
                                                                    break;
                                                                case "3":
                                                                    field += "短";
                                                                    break;
                                                                case "4":
                                                                    field += "大";
                                                                    break;
                                                                case "5":
                                                                    field += "小";
                                                                    break;
                                                                case "6":
                                                                    field += "破皮";
                                                                    break;
                                                                case "7":
                                                                    field += "結痂";
                                                                    break;
                                                                default:
                                                                    field += "";
                                                                    break;
                                                            }
                                                            commaFlag = true;
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                field += "未填寫";
                                            }
                                            content.Add(field);
                                            #endregion

                                            #region 泌乳
                                            field = "乳汁";

                                            //左右相同
                                            if (r["LACT_L"].ToString() == r["LACT_R"].ToString() && IsNoEmpty(r["LACT_L"].ToString()) && IsNoEmpty(r["LACT_L"].ToString()))
                                            {
                                                switch ((r["LACT_L"] ?? "").ToString())
                                                {
                                                    case "0":
                                                        field += "無分泌";
                                                        break;
                                                    case "1":
                                                        field += "微泌乳";
                                                        break;
                                                    case "2":
                                                        field += "已分泌";
                                                        break;
                                                    default:
                                                        field += "";
                                                        break;
                                                }
                                            }
                                            else
                                            {
                                                //左
                                                field += "左";
                                                if (IsNoEmpty(r["LACT_L"].ToString()))
                                                {
                                                    switch ((r["LACT_L"] ?? "").ToString())
                                                    {
                                                        case "0":
                                                            field += "無分泌";
                                                            break;
                                                        case "1":
                                                            field += "微泌乳";
                                                            break;
                                                        case "2":
                                                            field += "已分泌";
                                                            break;
                                                        default:
                                                            field += "";
                                                            break;
                                                    }
                                                }
                                                else
                                                {
                                                    field += "未填寫";
                                                }

                                                //右
                                                field += "、右";
                                                if (IsNoEmpty(r["LACT_R"].ToString()))
                                                {
                                                    switch ((r["LACT_R"].ToString() ?? "").ToString())
                                                    {
                                                        case "0":
                                                            field += "無分泌";
                                                            break;
                                                        case "1":
                                                            field += "微泌乳";
                                                            break;
                                                        case "2":
                                                            field += "已分泌";
                                                            break;
                                                        default:
                                                            field += "";
                                                            break;
                                                    }
                                                }
                                                else
                                                {
                                                    field += "未填寫";
                                                }
                                            }
                                            content.Add(field);
                                            #endregion

                                            semicolon.Add(String.Join("，", content));
                                            content = new List<string>();
                                            field = "";

                                            #region 嬰兒[嬰兒生理評估 清醒]
                                            field = "嬰兒";
                                            switch ((r["AWAKE"] ?? "").ToString())
                                            {
                                                case "0":
                                                    field += "警醒";
                                                    break;
                                                case "1":
                                                    field += "睡覺";
                                                    break;
                                                case "2":
                                                    field += "哭鬧";
                                                    break;
                                                default:
                                                    field += "";
                                                    break;
                                            }

                                            content.Add(field);
                                            #endregion

                                            #region 呼吸[嬰兒生理評估 呼吸型態]
                                            field = "呼吸";
                                            switch ((r["BRE_TYP"] ?? "").ToString())
                                            {
                                                case "0":
                                                    field += "順暢";
                                                    break;
                                                case "1":
                                                    field += "胸骨凹陷";
                                                    break;
                                                case "2":
                                                    field += "困難";
                                                    break;
                                                default:
                                                    field += "未填寫";
                                                    break;
                                            }
                                            content.Add(field);
                                            #endregion

                                            #region 膚色[嬰兒生理評估 膚色]
                                            field = "膚色";
                                            switch ((r["SKIN"] ?? "").ToString())
                                            {
                                                case "0":
                                                    field += "粉紅";
                                                    break;
                                                case "1":
                                                    field += "蒼白";
                                                    break;
                                                case "2":
                                                    field += "發紺";
                                                    break;
                                                default:
                                                    field += "未填寫";
                                                    break;
                                            }
                                            content.Add(field);
                                            #endregion

                                            #region 肌肉張力[嬰兒生理評估 肌肉張力]
                                            field = "肌肉張力";
                                            switch ((r["MUSCLE"] ?? "").ToString())
                                            {
                                                case "0":
                                                    field += "佳";
                                                    break;
                                                case "1":
                                                    field += "中";
                                                    break;
                                                case "2":
                                                    field += "弱";
                                                    break;
                                                default:
                                                    field += "未填寫";
                                                    break;
                                            }
                                            content.Add(field);
                                            #endregion

                                            #region 活動力[嬰兒生理評估 活動力]
                                            field = "活動力";
                                            switch ((r["ACTI"] ?? "").ToString())
                                            {
                                                case "0":
                                                    field += "強";
                                                    break;
                                                case "1":
                                                    field += "中";
                                                    break;
                                                case "2":
                                                    field += "弱";
                                                    break;
                                                default:
                                                    field += "未填寫";
                                                    break;
                                            }
                                            content.Add(field);
                                            #endregion

                                            semicolon.Add(String.Join("，", content));
                                            content = new List<string>();
                                            field = "";

                                            #region 觀察嬰兒[嬰兒會尋找乳房(是 / 飢餓時會尋找乳房、否 / 不呈現)]
                                            field = "觀察嬰兒";
                                            commaFlag = false;
                                            switch ((r["BRE_RES2"] ?? "").ToString())
                                            {
                                                case "0":
                                                    field += "";
                                                    break;
                                                case "1":
                                                    commaFlag = true;
                                                    field += "飢餓時會尋找乳房";
                                                    break;
                                                default:
                                                    field += "";
                                                    break;
                                            }
                                            #endregion

                                            #region [嬰兒接觸乳房時平靜而清醒(是 / 情緒平穩、否 / 哭泣)]
                                            if (commaFlag)
                                                field += "、";

                                            switch ((r["BRE_RES3"] ?? "").ToString())
                                            {
                                                case "0":
                                                    field += "哭泣";
                                                    break;
                                                case "1":
                                                    field += "情緒平穩";
                                                    break;
                                                default:
                                                    field += "";
                                                    break;
                                            }

                                            content.Add(field);
                                            #endregion

                                            semicolon.Add(String.Join("，", content));
                                            content = new List<string>();
                                            field = "";
                                            commaFlag = false;

                                            #region 哺餵母乳時，產婦身體姿勢[母親放鬆而舒服(是 / 放鬆、否 / 緊繃)]
                                            field = "哺餵母乳時，產婦身體姿勢";
                                            switch ((r["BRE_BP1"] ?? "").ToString())
                                            {
                                                case "0":
                                                    field += "緊繃";
                                                    break;
                                                case "1":
                                                    field += "放鬆";
                                                    break;
                                                default:
                                                    field += "未填寫";
                                                    break;
                                            }
                                            if (IsNoEmpty(field))
                                                content.Add(field);
                                            #endregion

                                            #region [嬰兒身體緊貼母親，臉朝向乳房(是 / 嬰兒身體緊貼母親，臉朝向乳房、否 / 不 呈現)]
                                            field = "";
                                            switch ((r["BRE_BP2"] ?? "").ToString())
                                            {
                                                case "0":
                                                    field += "";
                                                    break;
                                                case "1":
                                                    field += "嬰兒身體緊貼母親，臉朝向乳房";
                                                    break;
                                                default:
                                                    field += "";
                                                    break;
                                            }
                                            if (IsNoEmpty(field))
                                                content.Add(field);
                                            #endregion

                                            #region [嬰兒頭部及身體呈一直線(是 / 頭部及身體一直線、否 / 嬰兒頭部及身體扭轉)]
                                            field = "";
                                            switch ((r["BRE_BP3"] ?? "").ToString())
                                            {
                                                case "0":
                                                    field += "嬰兒頭部及身體扭轉";
                                                    break;
                                                case "1":
                                                    field += "頭部及身體一直線";
                                                    break;
                                                default:
                                                    field += "";
                                                    break;
                                            }
                                            if (IsNoEmpty(field))
                                                content.Add(field);
                                            #endregion

                                            #region [嬰兒下巴貼著乳房(是 / 下巴貼著乳房、否 / 下巴未貼著乳房)]
                                            field = "";
                                            switch ((r["BRE_BP4"] ?? "").ToString())
                                            {
                                                case "0":
                                                    field += "下巴未貼著乳房";
                                                    break;
                                                case "1":
                                                    field += "下巴貼著乳房";
                                                    break;
                                                default:
                                                    field += "";
                                                    break;
                                            }
                                            if (IsNoEmpty(field))
                                                content.Add(field);
                                            #endregion

                                            #region [嬰兒臀部受支撐(是 / 臀部有支撐、否 / 不呈現)]
                                            field = "";
                                            switch ((r["BRE_BP5"] ?? "").ToString())
                                            {
                                                case "0":
                                                    field += "";
                                                    break;
                                                case "1":
                                                    field += "臀部有支撐";
                                                    break;
                                                default:
                                                    field += "";
                                                    break;
                                            }
                                            if (IsNoEmpty(field))
                                                content.Add(field);
                                            #endregion

                                            #region [嬰兒嘴巴張大(是 / 吸吮時嘴巴有張大、否 / 嘴巴僅含著乳頭)] 
                                            field = "";
                                            switch ((r["BRE_SUC1"] ?? "").ToString())
                                            {
                                                case "0":
                                                    field += "嘴巴僅含著乳頭";
                                                    break;
                                                case "1":
                                                    field += "吸吮時嘴巴有張大";
                                                    break;
                                                default:
                                                    field += "";
                                                    break;
                                            }
                                            if (IsNoEmpty(field))
                                                content.Add(field);
                                            #endregion

                                            #region [下唇外翻(是 / 下唇外翻、否 / 下唇未外翻)]
                                            field = "";
                                            switch ((r["BRE_SUC2"] ?? "").ToString())
                                            {
                                                case "0":
                                                    field += "下唇未外翻";
                                                    break;
                                                case "1":
                                                    field += "下唇外翻";
                                                    break;
                                                default:
                                                    field += "";
                                                    break;
                                            }
                                            if (IsNoEmpty(field))
                                                content.Add(field);
                                            #endregion

                                            #region [嬰兒嘴巴上方乳暈較多(是 / 嘴巴上方乳暈較多、否 / 未含上乳暈)]
                                            field = "";
                                            switch ((r["BRE_SUC3"] ?? "").ToString())
                                            {
                                                case "0":
                                                    field += "未含上乳暈";
                                                    break;
                                                case "1":
                                                    field += "嘴巴上方乳暈較多";
                                                    break;
                                                default:
                                                    field += "";
                                                    break;
                                            }
                                            if (IsNoEmpty(field))
                                                content.Add(field);
                                            #endregion

                                            #region [慢慢的深吸奶，一陣子後間隔有休息(是 / 慢而深吸奶，一陣子後間隔有休息、否 / 吸吮急促)]
                                            field = "";
                                            switch ((r["BRE_SUC4"] ?? "").ToString())
                                            {
                                                case "0":
                                                    field += "吸吮急促";
                                                    break;
                                                case "1":
                                                    field += "慢而深吸奶，一陣子後間隔有休息";
                                                    break;
                                                default:
                                                    field += "";
                                                    break;
                                            }
                                            if (IsNoEmpty(field))
                                                content.Add(field);
                                            #endregion

                                            #region [可看到或聽到吞嚥(是 / 可看到或聽到吞嚥、否 / 不呈現)]
                                            field = "";
                                            switch ((r["BRE_SUC5"] ?? "").ToString())
                                            {
                                                case "0":
                                                    field += "";
                                                    break;
                                                case "1":
                                                    field += "可看到或聽到吞嚥";
                                                    break;
                                                default:
                                                    field += "";
                                                    break;
                                            }
                                            if (IsNoEmpty(field))
                                                content.Add(field);
                                            #endregion

                                            #region [嬰兒自己鬆開乳房(是 / 喝飽後嬰兒自己鬆開乳房、否 / 不呈現)]
                                            field = "";
                                            switch ((r["BRE_TSS1"] ?? "").ToString())
                                            {
                                                case "0":
                                                    field += "";
                                                    break;
                                                case "1":
                                                    field += "喝飽後嬰兒自己鬆開乳房";
                                                    break;
                                                default:
                                                    field += "";
                                                    break;
                                            }
                                            if (IsNoEmpty(field))
                                                content.Add(field);
                                            #endregion

                                            field = "";

                                            #region 吸吮[嬰兒吸吮母乳時間] 分鐘。嬰兒自解{ [排便 次數] 次[排便 顏色][排便 軟便]
                                            field = "#嬰兒吸吮母乳時間#";

                                            if (IsNoEmpty(field))
                                                content.Add(field);
                                            #endregion

                                            #region [排尿 次數] 次[排尿 顏色] 尿液
                                            field = $"{(IsNoEmpty(r["URI_FRQ"].ToString()) == true ? r["URI_FRQ"].ToString() : "0")}次";

                                            switch ((r["URI_COL"].ToString() ?? "").ToString())
                                            {
                                                case "0":
                                                    field += "黃色";
                                                    break;
                                                case "1":
                                                    field += "透明";
                                                    break;
                                                case "2":
                                                    field += "褐色";
                                                    break;
                                                case "3":
                                                    field += "結晶尿";
                                                    break;
                                                case "4":
                                                    field += "血尿";
                                                    break;
                                                default:
                                                    field += "";
                                                    break;
                                            }
                                            field += "尿液";

                                            if (IsNoEmpty(r["TREAT"].ToString()))
                                                field += $"。{r["TREAT"].ToString()}";

                                            if (IsNoEmpty(field))
                                                content.Add(field);
                                            #endregion

                                            semicolon.Add(String.Join("，", content));

                                            var result = String.Join("；", semicolon) + "。";
                                            field = "";

                                            if (r["NB_CARE_RECORD"].ToString() == "1")
                                            {
                                                field = $"吸吮{(IsNoEmpty(r["BRE_TSS2"].ToString()) == true ? r["BRE_TSS2"].ToString() : "0")}分鐘，";
                                                field += $"餵食開水量{(IsNoEmpty(r["WATER"].ToString()) == true ? r["WATER"].ToString() : "0")}mL";
                                                field += $"、母乳量{(IsNoEmpty(r["BRE_MLK"].ToString()) == true ? r["BRE_MLK"].ToString() : "0")}mL";
                                                field += $"、配方奶量{(IsNoEmpty(r["MILK"].ToString()) == true ? r["MILK"].ToString() : "0")}mL";
                                                field += $"、D5W量{(IsNoEmpty(r["D5W"].ToString()) == true ? r["D5W"].ToString() : "0")}mL。";

                                                field += $"嬰兒自解{(IsNoEmpty(r["DEF_FRQ"].ToString()) == true ? r["DEF_FRQ"].ToString() : "0")}次";

                                                switch ((r["DEF_COL"].ToString() ?? "").ToString())
                                                {
                                                    case "0":
                                                        field += "墨綠";
                                                        break;
                                                    case "1":
                                                        field += "灰白色";
                                                        break;
                                                    case "2":
                                                        field += "白色";
                                                        break;
                                                    case "3":
                                                        field += "黃綠色";
                                                        break;
                                                    case "4":
                                                        field += "黃色";
                                                        break;
                                                    case "5":
                                                        field += "鮮紅色";
                                                        break;
                                                    case "6":
                                                        field += "暗紅色";
                                                        break;
                                                    default:
                                                        field += "";
                                                        break;
                                                }

                                                switch ((r["DEF_CHA"].ToString() ?? "").ToString())
                                                {
                                                    case "0":
                                                        field += "軟便";
                                                        break;
                                                    case "1":
                                                        field += "硬便";
                                                        break;
                                                    case "2":
                                                        field += "糊便";
                                                        break;
                                                    case "3":
                                                        field += "稀便";
                                                        break;
                                                    case "4":
                                                        field += "水便";
                                                        break;
                                                    case "5":
                                                        field += "粘便";
                                                        break;
                                                    case "6":
                                                        field += "母乳便";
                                                        break;
                                                    case "7":
                                                        field += "過渡便";
                                                        break;
                                                    case "8":
                                                        field += "胎便";
                                                        break;
                                                    case "9":
                                                        field += "綠便";
                                                        break;
                                                    default:
                                                        field += "";
                                                        break;
                                                }

                                                result = (String.Join("；", semicolon) + "。").Replace("#嬰兒吸吮母乳時間#", field);

                                                //小孩給小孩
                                                if (ptinfo?.Age < 1)
                                                {
                                                    result = result.Replace("#from#", "");
                                                    base.Insert_CareRecordTns(Convert.ToDateTime(r["RECORDTIME"].ToString()).ToString("yyyy/MM/dd HH:mm"), r["IID"].ToString(), "母嬰護理及飲食排泄紀錄", result, "", "", "", "", "OBS_PATNB", ref link);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        #endregion
                    }
                }
            }
            catch
            {
                link.DBRollBack();
            }

            if (erow > 0)
            {
                link.DBCommit();

                string return_jsonstr = string.Empty;

                if (form["TYPE"] == "已完成")
                {

                    PdfEmr.ControllerContext = ControllerContext;
                    PdfEmr.userinfo = userinfo;
                    PdfEmr.ptinfo = ptinfo;
                    return_jsonstr = PdfEmr.GetPDF_EMR("Save", feeno, userno, "Insert_NBENTR", PK_ID, mom_feeno);
                    json_result = JsonConvert.DeserializeObject<RESPONSE_MSG>(return_jsonstr);
                }
                else
                {
                    json_result.status = RESPONSE_STATUS.SUCCESS;
                    json_result.message = "";

                }

                //return "1";
                //Response.Write("<script>alert('新增成功');window.location.href='../BabyBorn/NBENTR_List';</script>");
            }
            else
            {
                link.DBRollBack();
                json_result.status = RESPONSE_STATUS.ERROR;
                json_result.message = "新增失敗";

                //return "-1";
                //Response.Write("<script>alert('新增失敗');window.opener.location.href='../BabyBorn/NBENTR_List';</script>");
            }

            return Content(JsonConvert.SerializeObject(json_result), "application/json");
        }
        #endregion

        #region 新生兒入院護理評估編輯
        /// <summary>
        /// 新生兒入院護理評估編輯
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        public ActionResult Upd_NBENTR(FormCollection form)
        {
            RESPONSE_MSG json_result = new RESPONSE_MSG();
            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;
            string save_type = "";
            string mom_feeno = string.Empty, tableid = string.Empty; //簽章用 此處以存檔僅會單筆為基礎  多筆則不適用需改寫法
            string[] id_list = form["IID"].Split(',');
            var dateNow = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

            List<DBItem> insertDataList = new List<DBItem>();
            int erow = 0;
            for (int v = 0; v < id_list.Length; v++)
            {
                tableid = id_list[v];

                insertDataList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDTIME", dateNow, DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("RECORDTIME", dateNow, DBItem.DBDataType.DataTime)); //紀錄時間

                if (form["STATUS"] == "已完成")
                {
                    save_type = "已完成";
                }
                else
                {
                    save_type = form["TYPE"];
                    insertDataList.Add(new DBItem("STATUS", save_type, DBItem.DBDataType.String));
                }

                #region 新生兒出生資料
                insertDataList.Add(new DBItem("BIRTH_PLACE", form["RB_BIRTH_PLACE" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FROM_WHERE", form["RB_FROM_WHERE" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PROCESS", form["TXT_PROCESS" + id_list[v]], DBItem.DBDataType.String));

                if (form["TXT_BIRTH_DAY" + id_list[v]] != "" && form["TXT_BIRTH_DAY" + id_list[v]] != null && form["TXT_BIRTH_TIME" + id_list[v]] != "" && form["TXT_BIRTH_TIME" + id_list[v]] != null)
                    insertDataList.Add(new DBItem("BIRTH", form["TXT_BIRTH_DAY" + id_list[v]] + " " + form["TXT_BIRTH_TIME" + id_list[v]], DBItem.DBDataType.DataTime));

                insertDataList.Add(new DBItem("GENDER", form["RB_GENDER" + id_list[v]], DBItem.DBDataType.String));

                insertDataList.Add(new DBItem("BIRTH_TYPE", form["RB_BIRTH_TYPE" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_BIRTH_TYPE" + id_list[v]] == "0")
                    insertDataList.Add(new DBItem("VACUUM_NSD", form["RB_VACUUM_NSD" + id_list[v]], DBItem.DBDataType.String));
                else if (form["RB_BIRTH_TYPE" + id_list[v]] == "1")
                    insertDataList.Add(new DBItem("VACUUM_CS", form["RB_VACUUM_CS" + id_list[v]], DBItem.DBDataType.String));
                else
                {
                    insertDataList.Add(new DBItem("VACUUM_NSD", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("VACUUM_CS", null, DBItem.DBDataType.String));
                }

                insertDataList.Add(new DBItem("GEST_M", form["TXT_GEST_M" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("GEST_D", form["TXT_GEST_D" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("GEST_REMARK", form["TXT_GEST_REMARK" + id_list[v]], DBItem.DBDataType.String));

                insertDataList.Add(new DBItem("HEIGHT", form["TXT_HEIGHT" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("WEIGHT", form["TXT_WEIGHT" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("HEAD", form["TXT_HEAD" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CHEST", form["TXT_CHEST" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("RB_AS_1_NUM", form["TXT_RB_AS_1_NUM" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("RB_AS_5_NUM", form["TXT_RB_AS_5_NUM" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("RB_AS_10_NUM", form["TXT_RB_AS_10_NUM" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("RB_AS_REMARK", form["TXT_RB_AS_REMARK" + id_list[v]], DBItem.DBDataType.String));

                insertDataList.Add(new DBItem("MAT_NAME", form["TXT_MAT_NAME" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MAT_AGE", form["TXT_MAT_AGE" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MAT_OCU", form["TXT_MAT_OCU" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MAT_EDU", form["RB_MAT_EDU" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MAT_TEL", form["TXT_MAT_TEL" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PAT_NAME", form["TXT_PAT_NAME" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PAT_AGE", form["TXT_PAT_AGE" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PAT_OCU", form["TXT_PAT_OCU" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PAT_EDU", form["RB_PAT_EDU" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PAT_TEL", form["TXT_PAT_TEL" + id_list[v]], DBItem.DBDataType.String));
                #endregion

                #region 母親病史
                insertDataList.Add(new DBItem("MED", form["RB_MED" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_MED" + id_list[v]] == "1")
                {

                }

                #region 疾病史
                insertDataList.Add(new DBItem("DISE", form["RB_DISE" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_DISE" + id_list[v]] == "1")
                {
                    var DISE_ITM = form["CK_DISE_ITM" + id_list[v]];
                    var DISE_ITMV = new List<string>() { "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0" };
                    if (DISE_ITM == null)
                        insertDataList.Add(new DBItem("DISE_ITM", String.Join("", DISE_ITMV), DBItem.DBDataType.String));
                    else
                    {
                        var DISE_ITM_SP = DISE_ITM.Split(',');
                        foreach (var i in DISE_ITM_SP)
                            DISE_ITMV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("DISE_ITM", String.Join("", DISE_ITMV), DBItem.DBDataType.String));
                    }
                    if (DISE_ITMV[14] == "1")
                        insertDataList.Add(new DBItem("DISE_ITM_OTH", form["TXT_DISE_ITM_OTH" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("DISE_ITM_OTH", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("DISE_ITM", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("DISE_ITM_OTH", null, DBItem.DBDataType.String));
                }
                #endregion

                insertDataList.Add(new DBItem("HBSAG", form["DDL_HBSAG" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("HBSAG_INST", form["TXT_HBSAG_INST" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("HBSAG_OTH", form["TXT_HBSAG_OTH" + id_list[v]], DBItem.DBDataType.String));

                if (form["HBSAGTYPE"].ToString() != "")
                {
                }
                else
                {
                    if (save_type == "已完成")
                    {
                        json_result.status = RESPONSE_STATUS.ERROR;
                        json_result.message = "HBsAg(B型肝炎表面抗原)尚未按確定覆核!";
                        return Content(JsonConvert.SerializeObject(json_result), "application/json");
                    }
                }


                insertDataList.Add(new DBItem("HBEAG", form["DDL_HBEAG" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("HBEAG_INST", form["TXT_HBEAG_INST" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("HBEAG_OTH", form["TXT_HBEAG_OTH" + id_list[v]], DBItem.DBDataType.String));

                if (form["HBEAGTYPE"].ToString() != "")
                {
                }
                else
                {
                    if (save_type == "已完成")
                    {
                        json_result.status = RESPONSE_STATUS.ERROR;
                        json_result.message = "HBeAg(e抗原)尚未按確定覆核!";
                        return Content(JsonConvert.SerializeObject(json_result), "application/json");
                    }
                }

                insertDataList.Add(new DBItem("RUBELLA", form["DDL_RUBELLA" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("RUBELLA_INST", form["TXT_RUBELLA_INST" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("RUBELLA_OTH", form["TXT_RUBELLA_OTH" + id_list[v]], DBItem.DBDataType.String));

                insertDataList.Add(new DBItem("VDRL1", form["DDL_VDRL1" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("VDRL1_INST", form["TXT_VDRL1_INST" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("VDRL1_OTH", form["TXT_VDRL1_OTH" + id_list[v]], DBItem.DBDataType.String));

                insertDataList.Add(new DBItem("VDRL2", form["DDL_VDRL2" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("VDRL2_INST", form["TXT_VDRL2_INST" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("VDRL2_OTH", form["TXT_VDRL2_OTH" + id_list[v]], DBItem.DBDataType.String));

                insertDataList.Add(new DBItem("HIV", form["DDL_HIV" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("HIV_INST", form["TXT_HIV_INST" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("HIV_OTH", form["TXT_HIV_OTH" + id_list[v]], DBItem.DBDataType.String));

                insertDataList.Add(new DBItem("HIVPT_R", form["RB_HIVPT_R" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("GBS", form["DDL_GBS" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("GBS_INST", form["TXT_GBS_INST" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("GBS_OTH", form["TXT_GBS_OTH" + id_list[v]], DBItem.DBDataType.String));

                if (form["TXT_GBS_AB_DAY" + id_list[v]] != "" && form["TXT_GBS_AB_DAY" + id_list[v]] != null && form["TXT_GBS_AB_TIME" + id_list[v]] != "" && form["TXT_GBS_AB_TIME" + id_list[v]] != null)
                    insertDataList.Add(new DBItem("GBS_AB", form["TXT_GBS_AB_DAY" + id_list[v]] + " " + form["TXT_GBS_AB_TIME" + id_list[v]], DBItem.DBDataType.DataTime));


                #region 妊娠併發症
                insertDataList.Add(new DBItem("PREG", form["RB_PREG" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_PREG" + id_list[v]] == "1")
                {
                    var PREG_ITM = form["CK_PREG_ITM" + id_list[v]];
                    var PREG_ITMV = new List<string>() { "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0" };
                    if (PREG_ITM == null)
                        insertDataList.Add(new DBItem("PREG_ITM", String.Join("", PREG_ITM), DBItem.DBDataType.String));
                    else
                    {
                        var PREG_ITM_SP = PREG_ITM.Split(',');
                        foreach (var i in PREG_ITM_SP)
                            PREG_ITMV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("PREG_ITM", String.Join("", PREG_ITMV), DBItem.DBDataType.String));
                    }
                    if (PREG_ITMV[12] == "1")
                        insertDataList.Add(new DBItem("PREG_ITM_OTH", form["TXT_PREG_ITM_OTH" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("PREG_ITM_OTH", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("PREG_ITM", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PREG_ITM_OTH", null, DBItem.DBDataType.String));
                }
                #endregion

                #endregion

                #region 護理評估

                #region 1.頭部

                insertDataList.Add(new DBItem("ASS_1_1", form["RB_ASS_1_1" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_1_1" + id_list[v]] == "0")
                {
                    for (var param = 1; param <= 5; param++)
                    {
                        List<DBItem> data = new List<DBItem>();
                        data.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                        data.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
                        data.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                        data.Add(new DBItem("RECORDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));

                        var ASS_1_1F = form[$"CK_ASS_1_1F{param}{id_list[v]}"];
                        var ASS_1_1FV = new List<string>() { "0", "0", "0", "0", "0" };

                        if (ASS_1_1F != null)
                        {
                            var ASS_1_1F_SP = ASS_1_1F.Split(',');
                            foreach (var i in ASS_1_1F_SP)
                                ASS_1_1FV[Convert.ToInt32(i)] = "1";
                        }
                        data.Add(new DBItem("ITM_REMARK", String.Join("", ASS_1_1FV), DBItem.DBDataType.String));

                        if (ASS_1_1FV[0] == "1")
                            data.Add(new DBItem("ITM_RED_NO", form[$"TXT_{param}_RED{id_list[v]}"], DBItem.DBDataType.String));
                        else
                            data.Add(new DBItem("ITM_RED_NO", null, DBItem.DBDataType.String));

                        if (ASS_1_1FV[1] == "1")
                            data.Add(new DBItem("ITM_BLI_NO", form[$"TXT_{param}_BLI{id_list[v]}"], DBItem.DBDataType.String));
                        else
                            data.Add(new DBItem("ITM_BLI_NO", null, DBItem.DBDataType.String));

                        if (ASS_1_1FV[2] == "1")
                            data.Add(new DBItem("ITM_HUR_NO", form[$"TXT_{param}_HUR{id_list[v]}"], DBItem.DBDataType.String));
                        else
                            data.Add(new DBItem("ITM_HUR_NO", null, DBItem.DBDataType.String));

                        if (ASS_1_1FV[3] == "1")
                            data.Add(new DBItem("ITM_CS_NO", form[$"TXT_{param}_CS{id_list[v]}"], DBItem.DBDataType.String));
                        else
                            data.Add(new DBItem("ITM_CS_NO", null, DBItem.DBDataType.String));

                        if (ASS_1_1FV[4] == "1")
                            data.Add(new DBItem("ITM_OTH_NO", form[$"TXT_{param}_OTH{id_list[v]}"], DBItem.DBDataType.String));
                        else
                            data.Add(new DBItem("ITM_OTH_NO", null, DBItem.DBDataType.String));

                        string data_where = " IID = '" + id_list[v] + "' ";
                        data_where += "AND SNO = '" + param + "' ";
                        data = setNullToEmpty(data);
                        if (link.DBExecUpdateTns("OBS_ASS_1_1F", data, data_where) < 0)
                        {
                            link.DBRollBack();
                        }
                    }
                }

                insertDataList.Add(new DBItem("ASS_1_2", form["RB_ASS_1_2" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_1_2" + id_list[v]] == "0")
                {
                    var ASS_1_2F1 = form["CK_ASS_1_2F1" + id_list[v]];
                    var ASS_1_2F1V = new List<string>() { "0", "0", "0", "0", "0", "0", "0", "0" };
                    if (ASS_1_2F1 == null)
                        insertDataList.Add(new DBItem("ASS_1_2F1", String.Join("", ASS_1_2F1V), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_1_2F1V_SP = ASS_1_2F1.Split(',');
                        foreach (var i in ASS_1_2F1V_SP)
                            ASS_1_2F1V[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_1_2F1", String.Join("", ASS_1_2F1V), DBItem.DBDataType.String));
                    }
                    if (ASS_1_2F1V[3] == "1")
                        insertDataList.Add(new DBItem("ASS_1_2F2", form["TXT_ASS_1_2F2" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_1_2F2", null, DBItem.DBDataType.String));
                    if (ASS_1_2F1V[7] == "1")
                        insertDataList.Add(new DBItem("ASS_1_2F3", form["TXT_ASS_1_2F3" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_1_2F3", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_1_2F1", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_1_2F2", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_1_2F3", null, DBItem.DBDataType.String));
                }
                #endregion

                #region 2.顏面
                insertDataList.Add(new DBItem("ASS_2", form["RB_ASS_2" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_2" + id_list[v]] == "0")
                {
                    var ASS_2F = form["CK_ASS_2F" + id_list[v]];
                    var ASS_2FV = new List<string>() { "0", "0", "0", "0", "0", "0" };
                    if (ASS_2F == null)
                        insertDataList.Add(new DBItem("ASS_2F", String.Join("", ASS_2FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_2F_SP = ASS_2F.Split(',');
                        foreach (var i in ASS_2F_SP)
                            ASS_2FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_2F", String.Join("", ASS_2FV), DBItem.DBDataType.String));
                    }
                    if (ASS_2FV[5] == "1")
                        insertDataList.Add(new DBItem("ASS_2FO", form["TXT_ASS_2FO" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_2FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_2F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_2FO", null, DBItem.DBDataType.String));
                }
                #endregion

                #region 3.眼睛
                insertDataList.Add(new DBItem("ASS_3", form["RB_ASS_3" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_3" + id_list[v]] == "0")
                {
                    var ASS_3F = form["CK_ASS_3F" + id_list[v]];
                    var ASS_3FV = new List<string>() { "0", "0", "0", "0", "0", "0" };
                    if (ASS_3F == null)
                        insertDataList.Add(new DBItem("ASS_3F", String.Join("", ASS_3FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_3F_SP = ASS_3F.Split(',');
                        foreach (var i in ASS_3F_SP)
                            ASS_3FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_3F", String.Join("", ASS_3FV), DBItem.DBDataType.String));
                    }
                    if (ASS_3FV[5] == "1")
                        insertDataList.Add(new DBItem("ASS_3FO", form["TXT_ASS_3FO" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_3FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_3F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_3FO", null, DBItem.DBDataType.String));
                }
                #endregion

                #region 4.耳朵
                insertDataList.Add(new DBItem("ASS_4", form["RB_ASS_4" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_4" + id_list[v]] == "0")
                {
                    var ASS_4F = form["CK_ASS_4F" + id_list[v]];
                    var ASS_4FV = new List<string>() { "0", "0", "0", "0", "0", "0", "0" };
                    if (ASS_4F == null)
                        insertDataList.Add(new DBItem("ASS_4F", String.Join("", ASS_4FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_4F_SP = ASS_4F.Split(',');
                        foreach (var i in ASS_4F_SP)
                            ASS_4FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_4F", String.Join("", ASS_4FV), DBItem.DBDataType.String));
                    }
                    if (ASS_4FV[6] == "1")
                        insertDataList.Add(new DBItem("ASS_4FO", form["TXT_ASS_4FO" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_4FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_4F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_4FO", null, DBItem.DBDataType.String));
                }
                #endregion

                #region 5.鼻子
                var ASS_5 = form["CK_ASS_5" + id_list[v]];
                var ASS_5V = new List<string>() { "0", "0", "0" };
                if (ASS_5 == null)
                    insertDataList.Add(new DBItem("ASS_5", String.Join("", ASS_5V), DBItem.DBDataType.String));
                else
                {
                    var ASS_5_SP = ASS_5.Split(',');
                    foreach (var i in ASS_5_SP)
                        ASS_5V[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("ASS_5", String.Join("", ASS_5V), DBItem.DBDataType.String));
                }
                if (ASS_5V[2] == "1")
                    insertDataList.Add(new DBItem("ASS_5O", form["TXT_ASS_5O" + id_list[v]], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_5O", null, DBItem.DBDataType.String));
                #endregion

                #region 6.口腔
                insertDataList.Add(new DBItem("ASS_6", form["RB_ASS_6" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_6" + id_list[v]] == "0")
                {
                    var ASS_6F = form["CK_ASS_6F" + id_list[v]];
                    var ASS_6FV = new List<string>() { "0", "0", "0", "0", "0" };
                    if (ASS_6F == null)
                        insertDataList.Add(new DBItem("ASS_6F", String.Join("", ASS_6FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_6F_SP = ASS_6F.Split(',');
                        foreach (var i in ASS_6F_SP)
                            ASS_6FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_6F", String.Join("", ASS_6FV), DBItem.DBDataType.String));
                    }
                    if (ASS_6FV[4] == "1")
                        insertDataList.Add(new DBItem("ASS_6FO", form["TXT_ASS_6FO" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_6FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_6F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_6FO", null, DBItem.DBDataType.String));
                }
                #endregion

                #region 7.頸部
                insertDataList.Add(new DBItem("ASS_7", form["RB_ASS_7" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_7" + id_list[v]] == "0")
                {
                    var ASS_7F = form["CK_ASS_7F" + id_list[v]];
                    var ASS_7FV = new List<string>() { "0", "0", "0", "0", "0", "0", "0" };
                    if (ASS_7F == null)
                        insertDataList.Add(new DBItem("ASS_7F", String.Join("", ASS_7FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_7F_SP = ASS_7F.Split(',');
                        foreach (var i in ASS_7F_SP)
                            ASS_7FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_7F", String.Join("", ASS_7FV), DBItem.DBDataType.String));
                    }
                    if (ASS_7FV[6] == "1")
                        insertDataList.Add(new DBItem("ASS_7FO", form["TXT_ASS_7FO" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_7FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_7F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_7FO", null, DBItem.DBDataType.String));
                }
                #endregion

                #region 8.胸部
                //外觀
                insertDataList.Add(new DBItem("ASS_8_1", form["RB_ASS_8_1" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_8_1" + id_list[v]] == "0")
                {
                    var ASS_8_1F = form["CK_ASS_8_1F" + id_list[v]];
                    var ASS_8_1FV = new List<string>() { "0", "0", "0", "0", "0" };
                    if (ASS_8_1F == null)
                        insertDataList.Add(new DBItem("ASS_8_1F", String.Join("", ASS_8_1FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_8_1F_SP = ASS_8_1F.Split(',');
                        foreach (var i in ASS_8_1F_SP)
                            ASS_8_1FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_8_1F", String.Join("", ASS_8_1FV), DBItem.DBDataType.String));
                    }
                    if (ASS_8_1FV[4] == "1")
                        insertDataList.Add(new DBItem("ASS_8_1FO", form["TXT_ASS_8_1FO" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_8_1FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_8_1F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_8_1FO", null, DBItem.DBDataType.String));
                }
                //呼吸
                insertDataList.Add(new DBItem("ASS_8_2", form["RB_ASS_8_2" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_8_2" + id_list[v]] == "0")
                {
                    var ASS_8_2F = form["CK_ASS_8_2F" + id_list[v]];
                    var ASS_8_2FV = new List<string>() { "0", "0", "0", "0" };
                    if (ASS_8_2F == null)
                        insertDataList.Add(new DBItem("ASS_8_2F", String.Join("", ASS_8_2FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_8_2F_SP = ASS_8_2F.Split(',');
                        foreach (var i in ASS_8_2F_SP)
                            ASS_8_2FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_8_2F", String.Join("", ASS_8_2FV), DBItem.DBDataType.String));
                    }
                    if (ASS_8_2FV[0] == "1")
                        insertDataList.Add(new DBItem("ASS_8_2FO", form["TXT_ASS_8_2FO" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_8_2FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_8_2F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_8_2FO", null, DBItem.DBDataType.String));
                }
                //呼吸音
                insertDataList.Add(new DBItem("ASS_8_3", form["RB_ASS_8_3" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_8_3" + id_list[v]] == "0")
                {
                    var ASS_8_3F = form["CK_ASS_8_3F" + id_list[v]];
                    var ASS_8_3FV = new List<string>() { "0", "0", "0", "0", "0" };
                    if (ASS_8_3F == null)
                        insertDataList.Add(new DBItem("ASS_8_3F", String.Join("", ASS_8_3FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_8_3F_SP = ASS_8_3F.Split(',');
                        foreach (var i in ASS_8_3F_SP)
                            ASS_8_3FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_8_3F", String.Join("", ASS_8_3FV), DBItem.DBDataType.String));
                    }
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_8_3F", null, DBItem.DBDataType.String));
                }
                //心臟
                insertDataList.Add(new DBItem("ASS_8_4", form["RB_ASS_8_4" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_8_4" + id_list[v]] == "0")
                {
                    var ASS_8_4F = form["CK_ASS_8_4F" + id_list[v]];
                    var ASS_8_4FV = new List<string>() { "0", "0", "0" };
                    if (ASS_8_4F == null)
                        insertDataList.Add(new DBItem("ASS_8_4F", String.Join("", ASS_8_4FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_8_4F_SP = ASS_8_4F.Split(',');
                        foreach (var i in ASS_8_4F_SP)
                            ASS_8_4FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_8_4F", String.Join("", ASS_8_4FV), DBItem.DBDataType.String));
                    }
                    if (ASS_8_4FV[2] == "1")
                        insertDataList.Add(new DBItem("ASS_8_4FO", form["TXT_ASS_8_4FO" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_8_4FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_8_4F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_8_4FO", null, DBItem.DBDataType.String));
                }
                #endregion

                #region 9.腹部
                //疝氣
                insertDataList.Add(new DBItem("ASS_9_1", form["RB_ASS_9_1" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_9_1" + id_list[v]] == "1")
                {
                    var ASS_9_1F = form["CK_ASS_9_1F" + id_list[v]];
                    var ASS_9_1FV = new List<string>() { "0", "0", "0", "0" };
                    if (ASS_9_1F == null)
                        insertDataList.Add(new DBItem("ASS_9_1F", String.Join("", ASS_9_1FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_9_1F_SP = ASS_9_1F.Split(',');
                        foreach (var i in ASS_9_1F_SP)
                            ASS_9_1FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_9_1F", String.Join("", ASS_9_1FV), DBItem.DBDataType.String));
                    }
                    if (ASS_9_1FV[3] == "1")
                        insertDataList.Add(new DBItem("ASS_9_1FO", form["TXT_ASS_9_1FO" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_9_1FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_9_1F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_9_1FO", null, DBItem.DBDataType.String));
                }
                //腹脹
                insertDataList.Add(new DBItem("ASS_9_2", form["RB_ASS_9_2" + id_list[v]], DBItem.DBDataType.String));
                //臍帶滲血
                insertDataList.Add(new DBItem("ASS_9_3", form["RB_ASS_9_3" + id_list[v]], DBItem.DBDataType.String));
                //臍動脈
                insertDataList.Add(new DBItem("ASS_9_4", form["RB_ASS_9_4" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_9_4"] == "2")
                    insertDataList.Add(new DBItem("ASS_9_4O", form["TXT_ASS_9_5O" + id_list[v]], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_9_4O", null, DBItem.DBDataType.String));
                //臍靜脈
                insertDataList.Add(new DBItem("ASS_9_5", form["RB_ASS_9_5" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_9_5"] == "2")
                    insertDataList.Add(new DBItem("ASS_9_5O", form["TXT_ASS_9_5O" + id_list[v]], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_9_5O", null, DBItem.DBDataType.String));
                //其他
                insertDataList.Add(new DBItem("ASS_9_6", form["TXT_ASS_9_6" + id_list[v]], DBItem.DBDataType.String));
                #endregion

                #region 10.臀背部
                insertDataList.Add(new DBItem("ASS_10", form["RB_ASS_10" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_10" + id_list[v]] == "0")
                {
                    var ASS_10F = form["CK_ASS_10F"];
                    var ASS_10FV = new List<string>() { "0", "0", "0", "0" };
                    if (ASS_10F == null)
                        insertDataList.Add(new DBItem("ASS_10F", String.Join("", ASS_10FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_10F_SP = ASS_10F.Split(',');
                        foreach (var i in ASS_10F_SP)
                            ASS_10FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_10F", String.Join("", ASS_10FV), DBItem.DBDataType.String));
                    }
                    if (ASS_10FV[3] == "1")
                        insertDataList.Add(new DBItem("ASS_10FO", form["TXT_ASS_10FO" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_10FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_10F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_10FO", null, DBItem.DBDataType.String));
                }
                #endregion

                #region 11.泌尿生殖器外觀
                //男
                if (form["RB_GENDER" + id_list[v]] == "0")
                {
                    //睪丸
                    insertDataList.Add(new DBItem("ASS_11_1", form["RB_ASS_11_1" + id_list[v]], DBItem.DBDataType.String));
                    if (form["RB_ASS_11_1" + id_list[v]] == "0")
                    {
                        var ASS_11_1F = form["CK_ASS_11_1F" + id_list[v]];
                        var ASS_11_1FV = new List<string>() { "0", "0" };
                        if (ASS_11_1F == null)
                            insertDataList.Add(new DBItem("ASS_11_1F", String.Join("", ASS_11_1FV), DBItem.DBDataType.String));
                        else
                        {
                            var ASS_11_1F_SP = ASS_11_1F.Split(',');
                            foreach (var i in ASS_11_1F_SP)
                                ASS_11_1FV[Convert.ToInt32(i)] = "1";
                            insertDataList.Add(new DBItem("ASS_11_1F", String.Join("", ASS_11_1FV), DBItem.DBDataType.String));
                        }
                    }
                    else
                    {
                        insertDataList.Add(new DBItem("ASS_11_1F", null, DBItem.DBDataType.String));
                    }
                    //睪丸
                    insertDataList.Add(new DBItem("ASS_11_2", form["RB_ASS_11_2" + id_list[v]], DBItem.DBDataType.String));
                    if (form["RB_ASS_11_2" + id_list[v]] == "0")
                    {
                        var ASS_11_2F = form["CK_ASS_11_2F" + id_list[v]];
                        var ASS_11_2FV = new List<string>() { "0", "0", "0" };
                        if (ASS_11_2F == null)
                            insertDataList.Add(new DBItem("ASS_11_2F", String.Join("", ASS_11_2FV), DBItem.DBDataType.String));
                        else
                        {
                            var ASS_11_2F_SP = ASS_11_2F.Split(',');
                            foreach (var i in ASS_11_2F_SP)
                                ASS_11_2FV[Convert.ToInt32(i)] = "1";
                            insertDataList.Add(new DBItem("ASS_11_2F", String.Join("", ASS_11_2FV), DBItem.DBDataType.String));
                        }
                        if (ASS_11_2FV[2] == "1")
                            insertDataList.Add(new DBItem("ASS_11_2FO", form["TXT_ASS_11_2FO" + id_list[v]], DBItem.DBDataType.String));
                        else
                            insertDataList.Add(new DBItem("ASS_11_2FO", null, DBItem.DBDataType.String));
                    }
                    else
                    {
                        insertDataList.Add(new DBItem("ASS_11_2F", null, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("ASS_11_2FO", null, DBItem.DBDataType.String));
                    }
                    //尿道下裂
                    insertDataList.Add(new DBItem("ASS_11_3", form["RB_ASS_11_3" + id_list[v]], DBItem.DBDataType.String));
                    //其他
                    insertDataList.Add(new DBItem("ASS_11_4", form["TXT_ASS_11_4" + id_list[v]], DBItem.DBDataType.String));
                    //清除女的欄位
                    insertDataList.Add(new DBItem("ASS_11_5", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_11_6", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_11_7", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_11_7F", null, DBItem.DBDataType.String));
                }
                //女
                else if (form["RB_GENDER" + id_list[v]] == "1")
                {
                    //清除男的欄位
                    insertDataList.Add(new DBItem("ASS_11_1", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_11_1F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_11_2", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_11_2F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_11_3", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_11_4", null, DBItem.DBDataType.String));
                    //陰道口
                    insertDataList.Add(new DBItem("ASS_11_5", form["RB_ASS_11_5" + id_list[v]], DBItem.DBDataType.String));
                    //陰唇腫
                    insertDataList.Add(new DBItem("ASS_11_6", form["RB_ASS_11_6" + id_list[v]], DBItem.DBDataType.String));
                    //陰道分泌物
                    insertDataList.Add(new DBItem("ASS_11_7", form["RB_ASS_11_7" + id_list[v]], DBItem.DBDataType.String));
                    if (form["RB_ASS_11_7" + id_list[v]] == "1")
                    {
                        var ASS_11_7F = form["CK_ASS_11_7F" + id_list[v]];
                        var ASS_11_7FV = new List<string>() { "0", "0", "0" };
                        if (ASS_11_7F == null)
                            insertDataList.Add(new DBItem("ASS_11_7F", String.Join("", ASS_11_7FV), DBItem.DBDataType.String));
                        else
                        {
                            var ASS_11_7F_SP = ASS_11_7F.Split(',');
                            foreach (var i in ASS_11_7F_SP)
                                ASS_11_7FV[Convert.ToInt32(i)] = "1";
                            insertDataList.Add(new DBItem("ASS_11_7F", String.Join("", ASS_11_7FV), DBItem.DBDataType.String));
                        }
                    }
                    else
                    {
                        insertDataList.Add(new DBItem("ASS_11_7F", null, DBItem.DBDataType.String));
                    }
                }
                //肛門
                insertDataList.Add(new DBItem("ASS_11_8", form["RB_ASS_11_8" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_11_8" + id_list[v]] == "0")
                {
                    var ASS_11_8F = form["CK_ASS_11_8F" + id_list[v]];
                    var ASS_11_8FV = new List<string>() { "0", "0", "0", "0", "0" };
                    if (ASS_11_8F == null)
                        insertDataList.Add(new DBItem("ASS_11_8F", String.Join("", ASS_11_8FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_11_8F_SP = ASS_11_8F.Split(',');
                        foreach (var i in ASS_11_8F_SP)
                            ASS_11_8FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_11_8F", String.Join("", ASS_11_8FV), DBItem.DBDataType.String));
                    }
                    if (ASS_11_8FV[4] == "1")
                        insertDataList.Add(new DBItem("ASS_11_8FO", form["TXT_ASS_11_8FO" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_11_8FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_11_8F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_11_8FO", null, DBItem.DBDataType.String));
                }
                #endregion

                #region 12.骨骼
                //骨折
                insertDataList.Add(new DBItem("ASS_12_1", form["RB_ASS_12_1" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_12_1" + id_list[v]] == "1")
                {
                    var ASS_12_1F = form["CK_ASS_12_1F" + id_list[v]];
                    var ASS_12_1FV = new List<string>() { "0", "0", "0" };
                    if (ASS_12_1F == null)
                        insertDataList.Add(new DBItem("ASS_12_1F", String.Join("", ASS_12_1FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_12_1F_SP = ASS_12_1F.Split(',');
                        foreach (var i in ASS_12_1F_SP)
                            ASS_12_1FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_12_1F", String.Join("", ASS_12_1FV), DBItem.DBDataType.String));
                    }
                    if (ASS_12_1FV[2] == "1")
                        insertDataList.Add(new DBItem("ASS_12_1FO", form["TXT_ASS_12_1FO" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_12_1FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_12_1F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_12_1FO", null, DBItem.DBDataType.String));
                }
                //脫臼
                insertDataList.Add(new DBItem("ASS_12_2", form["RB_ASS_12_2" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_12_2" + id_list[v]] == "1")
                    insertDataList.Add(new DBItem("ASS_12_2F", form["TXT_ASS_12_2F" + id_list[v]], DBItem.DBDataType.String));
                //新生兒先天性髖關節脫臼檢查：巴氏測驗(Barlow test) && 歐氏測驗(Ortolani test)
                insertDataList.Add(new DBItem("ASS_12_3", form["RB_ASS_12_3" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_12_4", form["RB_ASS_12_4" + id_list[v]], DBItem.DBDataType.String));
                #endregion

                #region 13.肌肉緊張度 14.哭聲
                insertDataList.Add(new DBItem("ASS_13", form["RB_ASS_13" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_14", form["RB_ASS_14" + id_list[v]], DBItem.DBDataType.String));

                if (form["RB_ASS_14" + id_list[v]] == "4")
                {
                    insertDataList.Add(new DBItem("ASS_14_OTH", form["TXT_ASS_14_OTH" + id_list[v]], DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_14_OTH", null, DBItem.DBDataType.String));
                }
                #endregion

                #region 15.皮膚
                var ASS_15 = form["CK_ASS_15" + id_list[v]];
                var ASS_15V = new List<string>() { "0", "0", "0", "0", "0", "0", "0" };
                if (ASS_15 == null)
                    insertDataList.Add(new DBItem("ASS_15", String.Join("", ASS_15V), DBItem.DBDataType.String));
                else
                {
                    var ASS_15_SP = ASS_15.Split(',');
                    foreach (var i in ASS_15_SP)
                        ASS_15V[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("ASS_15", String.Join("", ASS_15V), DBItem.DBDataType.String));
                }
                if (ASS_15V[6] == "1")
                    insertDataList.Add(new DBItem("ASS_15O", form["TXT_ASS_15O" + id_list[v]], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_15O", null, DBItem.DBDataType.String));
                //血管瘤
                insertDataList.Add(new DBItem("ASS_15_1", form["RB_ASS_15_1" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_15_1" + id_list[v]] == "1")
                {
                    insertDataList.Add(new DBItem("ASS_15_1P", form["TXT_ASS_15_1P" + id_list[v]], DBItem.DBDataType.String));

                    //var ASS_15_1F = form["CK_ASS_15_1F" + id_list[v]];
                    //var ASS_15_1FV = new List<string>() { "0", "0", "0" };
                    //if (ASS_15_1F == null)
                    //    insertDataList.Add(new DBItem("ASS_15_1F", String.Join("", ASS_15_1FV), DBItem.DBDataType.String));
                    //else
                    //{
                    //    var ASS_15_1F_SP = ASS_15_1F.Split(',');
                    //    foreach (var i in ASS_15_1F_SP)
                    //        ASS_15_1FV[Convert.ToInt32(i)] = "1";
                    //    insertDataList.Add(new DBItem("ASS_15_1F", String.Join("", ASS_15_1FV), DBItem.DBDataType.String));
                    //}
                    //if (ASS_15_1FV[0] == "1")
                    //    insertDataList.Add(new DBItem("ASS_15_1F1", form["TXT_ASS_15_1F1" + id_list[v]], DBItem.DBDataType.String));
                    //else
                    //    insertDataList.Add(new DBItem("ASS_15_1F1", null, DBItem.DBDataType.String));
                    //if (ASS_15_1FV[1] == "1")
                    //    insertDataList.Add(new DBItem("ASS_15_1F2", form["TXT_ASS_15_1F2" + id_list[v]], DBItem.DBDataType.String));
                    //else
                    //    insertDataList.Add(new DBItem("ASS_15_1F2", null, DBItem.DBDataType.String));
                    //if (ASS_15_1FV[2] == "1")
                    //    insertDataList.Add(new DBItem("ASS_15_1F3", form["TXT_ASS_15_1F3" + id_list[v]], DBItem.DBDataType.String));
                    //else
                    //    insertDataList.Add(new DBItem("ASS_15_1F3", null, DBItem.DBDataType.String));
                }
                else
                {
                    //insertDataList.Add(new DBItem("ASS_15_1F", null, DBItem.DBDataType.String));
                    //insertDataList.Add(new DBItem("ASS_15_1F1", null, DBItem.DBDataType.String));
                    //insertDataList.Add(new DBItem("ASS_15_1F2", null, DBItem.DBDataType.String));
                    //insertDataList.Add(new DBItem("ASS_15_1F3", null, DBItem.DBDataType.String));
                }
                //胎記
                insertDataList.Add(new DBItem("ASS_15_2", form["RB_ASS_15_2" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_15_2" + id_list[v]] == "1")
                {
                    var ASS_15_2F = form["CK_ASS_15_2F" + id_list[v]];
                    var ASS_15_2FV = new List<string>() { "0", "0", "0", "0" };
                    if (ASS_15_2F == null)
                        insertDataList.Add(new DBItem("ASS_15_2F", String.Join("", ASS_15_2FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_15_2F_SP = ASS_15_2F.Split(',');
                        foreach (var i in ASS_15_2F_SP)
                            ASS_15_2FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_15_2F", String.Join("", ASS_15_2FV), DBItem.DBDataType.String));
                    }
                    if (ASS_15_2FV[0] == "1")
                        insertDataList.Add(new DBItem("ASS_15_2F1", form["TXT_ASS_15_2F1" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_15_2F1", null, DBItem.DBDataType.String));
                    if (ASS_15_2FV[1] == "1")
                        insertDataList.Add(new DBItem("ASS_15_2F2", form["TXT_ASS_15_2F2" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_15_2F2", null, DBItem.DBDataType.String));
                    if (ASS_15_2FV[2] == "1")
                        insertDataList.Add(new DBItem("ASS_15_2F3", form["TXT_ASS_15_2F3" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_15_2F3", null, DBItem.DBDataType.String));
                    if (ASS_15_2FV[3] == "1")
                        insertDataList.Add(new DBItem("ASS_15_2FO", form["TXT_ASS_15_2FO" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_15_2FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_15_2F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_15_2F1", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_15_2F2", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_15_2F3", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_15_2FO", null, DBItem.DBDataType.String));
                }
                #endregion

                #region 16.四肢
                insertDataList.Add(new DBItem("ASS_16", form["RB_ASS_16" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_16" + id_list[v]] == "0")
                {
                    #region 四肢 - 足內翻
                    insertDataList.Add(new DBItem("ASS_16_1", form["RB_ASS_16_1" + id_list[v]], DBItem.DBDataType.String));
                    if (form["RB_ASS_16_1" + id_list[v]] == "1")
                    {
                        var ASS_16_1F = form["CK_ASS_16_1F" + id_list[v]];
                        var ASS_16_1FV = new List<string>() { "0", "0" };
                        if (ASS_16_1F == null)
                            insertDataList.Add(new DBItem("ASS_16_1F", String.Join("", ASS_16_1FV), DBItem.DBDataType.String));
                        else
                        {
                            var ASS_16_1F_SP = ASS_16_1F.Split(',');
                            foreach (var i in ASS_16_1F_SP)
                                ASS_16_1FV[Convert.ToInt32(i)] = "1";
                            insertDataList.Add(new DBItem("ASS_16_1F", String.Join("", ASS_16_1FV), DBItem.DBDataType.String));
                        }
                    }
                    else
                        insertDataList.Add(new DBItem("ASS_16_1F", null, DBItem.DBDataType.String));
                    #endregion

                    #region 四肢 - 足外翻
                    insertDataList.Add(new DBItem("ASS_16_2", form["RB_ASS_16_2" + id_list[v]], DBItem.DBDataType.String));
                    if (form["RB_ASS_16_2" + id_list[v]] == "1")
                    {
                        var ASS_16_2F = form["CK_ASS_16_2F" + id_list[v]];
                        var ASS_16_2FV = new List<string>() { "0", "0" };
                        if (ASS_16_2F == null)
                            insertDataList.Add(new DBItem("ASS_16_2F", String.Join("", ASS_16_2FV), DBItem.DBDataType.String));
                        else
                        {
                            var ASS_16_2F_SP = ASS_16_2F.Split(',');
                            foreach (var i in ASS_16_2F_SP)
                                ASS_16_2FV[Convert.ToInt32(i)] = "1";
                            insertDataList.Add(new DBItem("ASS_16_2F", String.Join("", ASS_16_2FV), DBItem.DBDataType.String));
                        }
                    }
                    else
                        insertDataList.Add(new DBItem("ASS_16_2F", null, DBItem.DBDataType.String));
                    #endregion

                    #region 四肢 - 單一手紋(斷掌)
                    insertDataList.Add(new DBItem("ASS_16_3", form["RB_ASS_16_3" + id_list[v]], DBItem.DBDataType.String));
                    if (form["RB_ASS_16_3" + id_list[v]] == "1")
                    {
                        var ASS_16_3F = form["CK_ASS_16_3F" + id_list[v]];
                        var ASS_16_3FV = new List<string>() { "0", "0" };
                        if (ASS_16_3F == null)
                            insertDataList.Add(new DBItem("ASS_16_3F", String.Join("", ASS_16_3FV), DBItem.DBDataType.String));
                        else
                        {
                            var ASS_16_3F_SP = ASS_16_3F.Split(',');
                            foreach (var i in ASS_16_3F_SP)
                                ASS_16_3FV[Convert.ToInt32(i)] = "1";
                            insertDataList.Add(new DBItem("ASS_16_3F", String.Join("", ASS_16_3FV), DBItem.DBDataType.String));
                        }
                    }
                    else
                        insertDataList.Add(new DBItem("ASS_16_3F", null, DBItem.DBDataType.String));
                    #endregion
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_16_1", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_1F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_2", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_2F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_3", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_3F", null, DBItem.DBDataType.String));
                }


                #region 四肢 - 活動力
                insertDataList.Add(new DBItem("ASS_16_4", form["RB_ASS_16_4" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_16_4" + id_list[v]] == "0")
                {
                    var ASS_16_4F = form["CK_ASS_16_4F" + id_list[v]];
                    var ASS_16_4FV = new List<string>() { "0", "0", "0", "0" };
                    if (ASS_16_4F == null)
                        insertDataList.Add(new DBItem("ASS_16_4F", String.Join("", ASS_16_4FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_16_4F_SP = ASS_16_4F.Split(',');
                        foreach (var i in ASS_16_4F_SP)
                            ASS_16_4FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_16_4F", String.Join("", ASS_16_4FV), DBItem.DBDataType.String));
                    }
                }
                else
                    insertDataList.Add(new DBItem("ASS_16_4F", null, DBItem.DBDataType.String));
                #endregion

                #region 四肢 - 水腫
                insertDataList.Add(new DBItem("ASS_16_5", form["RB_ASS_16_5" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_16_5" + id_list[v]] == "1")
                {
                    var ASS_16_5F = form["CK_ASS_16_5F" + id_list[v]];
                    var ASS_16_5FV = new List<string>() { "0", "0", "0", "0" };
                    if (ASS_16_5F == null)
                        insertDataList.Add(new DBItem("ASS_16_5F", String.Join("", ASS_16_5FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_16_5F_SP = ASS_16_5F.Split(',');
                        foreach (var i in ASS_16_5F_SP)
                            ASS_16_5FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_16_5F", String.Join("", ASS_16_5FV), DBItem.DBDataType.String));
                    }
                }
                else
                    insertDataList.Add(new DBItem("ASS_16_5F", null, DBItem.DBDataType.String));
                #endregion

                #region 四肢 - 麻痺
                insertDataList.Add(new DBItem("ASS_16_6", form["RB_ASS_16_6" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_16_6" + id_list[v]] == "1")
                {
                    var ASS_16_6F = form["CK_ASS_16_6F" + id_list[v]];
                    var ASS_16_6FV = new List<string>() { "0", "0", "0", "0" };
                    if (ASS_16_6F == null)
                        insertDataList.Add(new DBItem("ASS_16_6F", String.Join("", ASS_16_6FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_16_6F_SP = ASS_16_6F.Split(',');
                        foreach (var i in ASS_16_6F_SP)
                            ASS_16_6FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_16_6F", String.Join("", ASS_16_6FV), DBItem.DBDataType.String));
                    }
                }
                else
                    insertDataList.Add(new DBItem("ASS_16_6F", null, DBItem.DBDataType.String));
                #endregion

                #region 四肢 - 手指
                insertDataList.Add(new DBItem("ASS_16_7", form["RB_ASS_16_7" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_16_7" + id_list[v]] == "0")
                {
                    var ASS_16_7F = form["CK_ASS_16_7F" + id_list[v]];
                    var ASS_16_7FV = new List<string>() { "0", "0", "0", "0", "0", "0", "0", "0" };
                    if (ASS_16_7F == null)
                        insertDataList.Add(new DBItem("ASS_16_7F", String.Join("", ASS_16_7FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_16_7F_SP = ASS_16_7F.Split(',');
                        foreach (var i in ASS_16_7F_SP)
                            ASS_16_7FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_16_7F", String.Join("", ASS_16_7FV), DBItem.DBDataType.String));
                    }
                    if (ASS_16_7FV[0] == "1")
                        insertDataList.Add(new DBItem("ASS_16_7F1", form["TXT_ASS_16_7F1" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_16_7F1", null, DBItem.DBDataType.String));

                    if (ASS_16_7FV[1] == "1")
                        insertDataList.Add(new DBItem("ASS_16_7F2", form["TXT_ASS_16_7F2" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_16_7F2", null, DBItem.DBDataType.String));

                    if (ASS_16_7FV[2] == "1")
                        insertDataList.Add(new DBItem("ASS_16_7F3", form["TXT_ASS_16_7F3" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_16_7F3", null, DBItem.DBDataType.String));

                    if (ASS_16_7FV[3] == "1")
                        insertDataList.Add(new DBItem("ASS_16_7F4", form["TXT_ASS_16_7F4" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_16_7F4", null, DBItem.DBDataType.String));

                    if (ASS_16_7FV[4] == "1")
                        insertDataList.Add(new DBItem("ASS_16_7F5", form["TXT_ASS_16_7F5" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_16_7F5", null, DBItem.DBDataType.String));

                    if (ASS_16_7FV[5] == "1")
                        insertDataList.Add(new DBItem("ASS_16_7F6", form["TXT_ASS_16_7F6" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_16_7F6", null, DBItem.DBDataType.String));

                    if (ASS_16_7FV[6] == "1")
                        insertDataList.Add(new DBItem("ASS_16_7F7", form["TXT_ASS_16_7F7" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_16_7F7", null, DBItem.DBDataType.String));

                    if (ASS_16_7FV[7] == "1")
                        insertDataList.Add(new DBItem("ASS_16_7F8", form["TXT_ASS_16_7F8" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_16_7F8", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_16_7F1", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_7F2", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_7F3", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_7F4", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_7F5", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_7F6", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_7F7", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_7F8", null, DBItem.DBDataType.String));
                }
                #endregion

                #region 四肢 - 腳趾
                insertDataList.Add(new DBItem("ASS_16_8", form["RB_ASS_16_8" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_16_8" + id_list[v]] == "0")
                {
                    var ASS_16_8F = form["CK_ASS_16_8F" + id_list[v]];
                    var ASS_16_8FV = new List<string>() { "0", "0", "0", "0", "0", "0" };
                    if (ASS_16_8F == null)
                        insertDataList.Add(new DBItem("ASS_16_8F", String.Join("", ASS_16_8FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_16_8F_SP = ASS_16_8F.Split(',');
                        foreach (var i in ASS_16_8F_SP)
                            ASS_16_8FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_16_8F", String.Join("", ASS_16_8FV), DBItem.DBDataType.String));
                    }
                    if (ASS_16_8FV[0] == "1")
                        insertDataList.Add(new DBItem("ASS_16_8F1", form["TXT_ASS_16_8F1" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_16_8F1", null, DBItem.DBDataType.String));

                    if (ASS_16_8FV[1] == "1")
                        insertDataList.Add(new DBItem("ASS_16_8F2", form["TXT_ASS_16_8F2" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_16_8F2", null, DBItem.DBDataType.String));

                    if (ASS_16_8FV[2] == "1")
                        insertDataList.Add(new DBItem("ASS_16_8F3", form["TXT_ASS_16_8F3" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_16_8F3", null, DBItem.DBDataType.String));

                    if (ASS_16_8FV[3] == "1")
                        insertDataList.Add(new DBItem("ASS_16_8F4", form["TXT_ASS_16_8F4" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_16_8F4", null, DBItem.DBDataType.String));

                    if (ASS_16_8FV[4] == "1")
                        insertDataList.Add(new DBItem("ASS_16_8F5", form["TXT_ASS_16_8F5" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_16_8F5", null, DBItem.DBDataType.String));

                    if (ASS_16_8FV[5] == "1")
                        insertDataList.Add(new DBItem("ASS_16_8F6", form["TXT_ASS_16_8F6" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_16_8F6", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_16_8F1", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_8F2", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_8F3", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_8F4", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_8F5", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_8F6", null, DBItem.DBDataType.String));
                }
                #endregion
                #endregion

                #region 17.反射
                insertDataList.Add(new DBItem("ASS_17_1", form["RB_ASS_17_1" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_17_2", form["RB_ASS_17_2" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_17_3", form["RB_ASS_17_3" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_17_4", form["RB_ASS_17_4" + id_list[v]], DBItem.DBDataType.String));
                #endregion

                #endregion

                #region 疼痛評估
                var PAIN_AS = form["RB_PAIN_AS" + id_list[v]];
                insertDataList.Add(new DBItem("PAIN_AS", PAIN_AS, DBItem.DBDataType.String));

                //數字量表
                if (PAIN_AS == "0")
                    insertDataList.Add(new DBItem("PAIN_AS1", form["RB_PAIN_AS1" + id_list[v]], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("PAIN_AS1", null, DBItem.DBDataType.String));

                //臉譜量表
                if (PAIN_AS == "1")
                    insertDataList.Add(new DBItem("PAIN_AS2", form["RB_PAIN_AS2" + id_list[v]], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("PAIN_AS2", null, DBItem.DBDataType.String));

                //困難評估(新生兒)
                if (PAIN_AS == "4")
                {
                    insertDataList.Add(new DBItem("PAIN_AS3_1", form["RB_PAIN_AS3_1" + id_list[v]], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS3_2", form["RB_PAIN_AS3_2" + id_list[v]], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS3_3", form["RB_PAIN_AS3_3" + id_list[v]], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS3_4", form["RB_PAIN_AS3_4" + id_list[v]], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS3_5", form["RB_PAIN_AS3_5" + id_list[v]], DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("PAIN_AS3_1", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS3_2", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS3_3", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS3_4", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS3_5", null, DBItem.DBDataType.String));
                }

                //CPOT評估(加護單位)
                if (PAIN_AS == "5")
                {
                    insertDataList.Add(new DBItem("PAIN_AS4_1", form["RB_PAIN_AS4_1" + id_list[v]], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS4_2", form["RB_PAIN_AS4_2" + id_list[v]], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS4_3", form["RB_PAIN_AS4_3" + id_list[v]], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS4_4", form["RB_PAIN_AS4_4" + id_list[v]], DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("PAIN_AS4_1", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS4_2", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS4_3", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS4_4", null, DBItem.DBDataType.String));
                }
                #endregion

                #region 接觸史
                //生產前14天內，產婦或同住家人有無：發燒、腹瀉、咳嗽、流鼻水等疑似感染症狀？
                insertDataList.Add(new DBItem("INF_MOM", form["RB_INF_MOM" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_INF_MOM" + id_list[v]] == "1")
                {
                    var INF_MOM_SYM = form["CK_INF_MOM_SYM" + id_list[v]];
                    var INF_MOM_SYMV = new List<string>() { "0", "0", "0", "0", "0", "0" };
                    if (INF_MOM_SYM == null)
                        insertDataList.Add(new DBItem("INF_MOM_SYM", String.Join("", INF_MOM_SYMV), DBItem.DBDataType.String));
                    else
                    {
                        var INF_MOM_SYM_SP = INF_MOM_SYM.Split(',');
                        foreach (var i in INF_MOM_SYM_SP)
                            INF_MOM_SYMV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("INF_MOM_SYM", String.Join("", INF_MOM_SYMV), DBItem.DBDataType.String));
                    }
                    if (INF_MOM_SYMV[5] == "1")
                        insertDataList.Add(new DBItem("INF_MOM_OTH", form["TXT_INF_MOM_OTH" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("INF_MOM_OTH", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("INF_MOM_SYM", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("INF_MOM_OTH", null, DBItem.DBDataType.String));
                }
                //同住家人
                insertDataList.Add(new DBItem("INF_OTH", form["RB_INF_OTH" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_INF_OTH" + id_list[v]] == "1")
                {
                    insertDataList.Add(new DBItem("INF_OTH_WHO", form["TXT_INF_OTH_WHO" + id_list[v]], DBItem.DBDataType.String));
                    var INF_OTH_SYM = form["CK_INF_OTH_SYM" + id_list[v]];
                    var INF_OTH_SYMV = new List<string>() { "0", "0", "0", "0", "0", "0" };
                    if (INF_OTH_SYM == null)
                        insertDataList.Add(new DBItem("INF_OTH_SYM", String.Join("", INF_OTH_SYMV), DBItem.DBDataType.String));
                    else
                    {
                        var INF_OTH_SYM_SP = INF_OTH_SYM.Split(',');
                        foreach (var i in INF_OTH_SYM_SP)
                            INF_OTH_SYMV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("INF_OTH_SYM", String.Join("", INF_OTH_SYMV), DBItem.DBDataType.String));
                    }
                    if (INF_OTH_SYMV[5] == "1")
                        insertDataList.Add(new DBItem("INF_OTH_OTH", form["TXT_INF_OTH_OTH" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("INF_OTH_OTH", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("INF_OTH_WHO", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("INF_OTH_SYM", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("INF_OTH_OTH", null, DBItem.DBDataType.String));
                }
                //生產前14天內，寶寶的哥哥、姊姊學校有無班上同學因為傳染病請假或班級停課之情形
                insertDataList.Add(new DBItem("BS_CLS", form["RB_BS_CLS" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_BS_CLS" + id_list[v]] == "1")
                {
                    var BS_CLS_RS = form["CK_BS_CLS_RS" + id_list[v]];
                    var BS_CLS_RSV = new List<string>() { "0", "0", "0", "0" };
                    if (BS_CLS_RS == null)
                        insertDataList.Add(new DBItem("BS_CLS_RS", String.Join("", BS_CLS_RSV), DBItem.DBDataType.String));
                    else
                    {
                        var BS_CLS_RS_SP = BS_CLS_RS.Split(',');
                        foreach (var i in BS_CLS_RS_SP)
                            BS_CLS_RSV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("BS_CLS_RS", String.Join("", BS_CLS_RSV), DBItem.DBDataType.String));
                    }
                    if (BS_CLS_RSV[3] == "1")
                        insertDataList.Add(new DBItem("BS_CLS_OTH", form["TXT_BS_CLS_OTH" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("BS_CLS_OTH", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("BS_CLS_RS", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("BS_CLS_OTH", null, DBItem.DBDataType.String));
                }
                //住院期間照顧者(應盡量維持同一人)，目前有無：發燒、腹瀉、咳嗽、流鼻水等疑似感染症狀？
                insertDataList.Add(new DBItem("INF_CARE", form["RB_INF_CARE" + id_list[v]], DBItem.DBDataType.String));
                #endregion

                #region TOCC評估
                //症狀
                var SYM = form["CK_SYM" + id_list[v]];
                var SYMV = new List<string>() { "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0" };
                if (SYM == null)
                    insertDataList.Add(new DBItem("SYM", String.Join("", SYMV), DBItem.DBDataType.String));
                else
                {
                    var SYM_SP = SYM.Split(',');
                    foreach (var i in SYM_SP)
                        SYMV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("SYM", String.Join("", SYMV), DBItem.DBDataType.String));
                }
                if (SYMV[10] == "1")//(其他TXT)
                    insertDataList.Add(new DBItem("SYM_OTH", form["TXT_SYM_OTH" + id_list[v]], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("SYM_OTH", null, DBItem.DBDataType.String));

                //旅遊史
                var TRAV = form["CK_TRAV" + id_list[v]];
                var TRAVV = new List<string>() { "0", "0", "0" };
                if (TRAV == null)
                    insertDataList.Add(new DBItem("TRAV", String.Join("", TRAVV), DBItem.DBDataType.String));
                else
                {
                    var TRAV_SP = TRAV.Split(',');
                    foreach (var i in TRAV_SP)
                        TRAVV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("TRAV", String.Join("", TRAVV), DBItem.DBDataType.String));
                }
                //國內旅遊城市
                insertDataList.Add(new DBItem("TRAV_DOME_CITY", form["TXT_TRAV_DOME_CITY" + id_list[v]], DBItem.DBDataType.String));
                //國內旅遊景點
                insertDataList.Add(new DBItem("TRAV_DOME_VIEW", form["TXT_TRAV_DOME_VIEW" + id_list[v]], DBItem.DBDataType.String));
                //國內旅遊交通
                var TRAV_DOME_TRAF = form["CK_TRAV_DOME_TRAF" + id_list[v]];
                var TRAV_DOME_TRAFV = new List<string>() { "0", "0", "0" };
                if (TRAV_DOME_TRAF == null)
                    insertDataList.Add(new DBItem("TRAV_DOME_TRAF", String.Join("", TRAV_DOME_TRAFV), DBItem.DBDataType.String));
                else
                {
                    var TRAV_DOME_TRAF_SP = TRAV_DOME_TRAF.Split(',');
                    foreach (var i in TRAV_DOME_TRAF_SP)
                        TRAV_DOME_TRAFV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("TRAV_DOME_TRAF", String.Join("", TRAV_DOME_TRAFV), DBItem.DBDataType.String));
                }
                if (TRAV_DOME_TRAFV[2] == "1")//(其他TXT)
                    insertDataList.Add(new DBItem("TRAV_DOME_TRAF_OTH", form["TXT_TRAV_DOME_TRAF_OTH" + id_list[v]], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("TRAV_DOME_TRAF_OTH", null, DBItem.DBDataType.String));
                //國外旅遊國家
                insertDataList.Add(new DBItem("TRAV_ABO_COUN", form["TXT_TRAV_ABO_COUN" + id_list[v]], DBItem.DBDataType.String));
                //國外旅遊目的地
                insertDataList.Add(new DBItem("TRAV_ABO_DEST", form["TXT_TRAV_ABO_DEST" + id_list[v]], DBItem.DBDataType.String));
                //國外旅遊交通方式
                var TRAV_ABO_TRAF = form["CK_TRAV_ABO_TRAF" + id_list[v]];
                var TRAV_ABO_TRAFV = new List<string>() { "0", "0", "0" };
                if (TRAV_ABO_TRAF == null)
                    insertDataList.Add(new DBItem("TRAV_ABO_TRAF", String.Join("", TRAV_ABO_TRAFV), DBItem.DBDataType.String));
                else
                {
                    var TRAV_ABO_TRAF_SP = TRAV_ABO_TRAF.Split(',');
                    foreach (var i in TRAV_ABO_TRAF_SP)
                        TRAV_ABO_TRAFV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("TRAV_ABO_TRAF", String.Join("", TRAV_ABO_TRAFV), DBItem.DBDataType.String));
                }
                if (TRAV_ABO_TRAFV[2] == "1")//(其他TXT)
                    insertDataList.Add(new DBItem("TRAV_ABO_TRAF_OTH", form["TXT_TRAV_ABO_TRAF_OTH" + id_list[v]], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("TRAV_ABO_TRAF_OTH", null, DBItem.DBDataType.String));
                //職業別
                var OCCU = form["CK_OCCU" + id_list[v]];
                var OCCUV = new List<string>() { "0", "0", "0", "0", "0" };
                if (OCCU == null)
                    insertDataList.Add(new DBItem("OCCU", String.Join("", OCCUV), DBItem.DBDataType.String));
                else
                {
                    var OCCU_SP = OCCU.Split(',');
                    foreach (var i in OCCU_SP)
                        OCCUV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("OCCU", String.Join("", OCCUV), DBItem.DBDataType.String));
                }
                if (OCCUV[4] == "1")//(其他TXT)
                    insertDataList.Add(new DBItem("OCCU_OTH", form["TXT_OCCU_OTH" + id_list[v]], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("OCCU_OTH", null, DBItem.DBDataType.String));
                //接觸史
                var CONT = form["CK_CONT" + id_list[v]];
                var CONTV = new List<string>() { "0", "0", "0" };
                if (CONT == null)
                    insertDataList.Add(new DBItem("CONT", String.Join("", CONTV), DBItem.DBDataType.String));
                else
                {
                    var CONT_SP = CONT.Split(',');
                    foreach (var i in CONT_SP)
                        CONTV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("CONT", String.Join("", CONTV), DBItem.DBDataType.String));
                }
                if (CONTV[1] == "1")//(接觸禽鳥類、畜類等)
                {
                    var CONT_BIRD = form["CK_CONT_BIRD" + id_list[v]];
                    var CONT_BIRDV = new List<string>() { "0", "0" };
                    if (CONT_BIRD == null)
                        insertDataList.Add(new DBItem("CONT_BIRD", String.Join("", CONT_BIRDV), DBItem.DBDataType.String));
                    else
                    {
                        var CONT_BIRD_SP = CONT_BIRD.Split(',');
                        foreach (var i in CONT_BIRD_SP)
                            CONT_BIRDV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("CONT_BIRD", String.Join("", CONT_BIRDV), DBItem.DBDataType.String));
                    }
                }
                else
                    insertDataList.Add(new DBItem("CONT_BIRD", null, DBItem.DBDataType.String));

                //嬰幼兒、新生兒接觸史
                //(生產前 14 天內，產婦或同住家人有發燒、腹瀉、咳嗽、流鼻水等疑似感染症狀?)
                insertDataList.Add(new DBItem("CONT_OBS_SYM", form["RB_CONT_OBS_SYM" + id_list[v]], DBItem.DBDataType.String));
                //(生產前 14 天內，寶寶的哥哥、姊姊學校班上同學有因為傳染病請假或班級停課之情形?)
                insertDataList.Add(new DBItem("CONT_OBS_SICKLEAVE", form["RB_CONT_OBS_SICKLEAVE" + id_list[v]], DBItem.DBDataType.String));
                //(住院期間照顧者(應盡量維持同一人)，目前有發燒、腹瀉、咳嗽、流鼻水等疑似感染症狀?)
                insertDataList.Add(new DBItem("CONT_OBS_CARESYM", form["RB_CONT_OBS_CARESYM" + id_list[v]], DBItem.DBDataType.String));

                if (CONTV[2] == "1")//(其他)
                {
                    insertDataList.Add(new DBItem("CONT_OTH", form["TXT_CONT_OTH" + id_list[v]], DBItem.DBDataType.String));
                }
                else
                    insertDataList.Add(new DBItem("CONT_OTH", null, DBItem.DBDataType.String));

                //群聚史
                insertDataList.Add(new DBItem("CLU", form["RB_CLU" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_CLU" + id_list[v]] == "1")//(家人/朋友/同事有發燒或類流感症狀)
                {
                    var CLU_RELA = form["CK_CLU_RELA" + id_list[v]];
                    var CLU_RELAV = new List<string>() { "0", "0", "0", "0" };
                    if (CLU_RELA == null)
                        insertDataList.Add(new DBItem("CLU_RELA", String.Join("", CLU_RELAV), DBItem.DBDataType.String));
                    else
                    {
                        var CLU_RELA_SP = CLU_RELA.Split(',');
                        foreach (var i in CLU_RELA_SP)
                            CLU_RELAV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("CLU_RELA", String.Join("", CLU_RELAV), DBItem.DBDataType.String));
                    }
                    if (CLU_RELAV[3] == "1")
                    {
                        insertDataList.Add(new DBItem("CLU_RELA_OTH", form["TXT_CLU_RELA_OTH" + id_list[v]], DBItem.DBDataType.String));
                    }
                    else
                    {
                        insertDataList.Add(new DBItem("CLU_RELA_OTH", null, DBItem.DBDataType.String));
                    }
                }
                else
                {
                    var CLU_RELAV = new List<string>() { "0", "0", "0", "0" };
                    insertDataList.Add(new DBItem("CLU_RELA", String.Join("", CLU_RELAV), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CLU_RELA_OTH", null, DBItem.DBDataType.String));
                }
                #endregion

                string where = " IID = '" + id_list[v] + "' ";
                insertDataList = setNullToEmpty(insertDataList);
                erow += obs_m.DBExecUpdate("OBS_NBENTR", insertDataList, where);

                #region 護理紀錄
                var exceptionResult = new List<string>();
                var finalResult = "入院經過：";
                finalResult += $"{(form["TXT_PROCESS" + id_list[v]] == "" ? "未填寫" : form["TXT_PROCESS" + id_list[v]])}。";
                finalResult += "護理評估：";

                #region 頭部-外觀
                var headFlag = false;
                if (form["RB_ASS_1_1" + id_list[v]] == "0")
                {
                    var items = new List<string>();
                    var bone = new List<string>() { "1.頭骨", "2.頂骨", "3.左顳骨", "4.右顳骨", "5.枕骨" };
                    #region 五大判斷
                    for (var i = 1; i <= 5; i++)
                    {
                        var CK_ASS_1_1F = form[$"CK_ASS_1_1F{i}" + id_list[v]];
                        if (IsNoEmpty(CK_ASS_1_1F))
                        {
                            var other = new List<string>();

                            var values = CK_ASS_1_1F.Split(',');
                            if (values.Length > 0)
                                foreach (var val in values)
                                {
                                    if (val == "0" && IsNoEmpty(form[$"TXT_{i}_RED" + id_list[v]]))
                                        other.Add($"紅腫{form[$"TXT_{i}_RED" + id_list[v]]}");

                                    if (val == "1" && IsNoEmpty(form[$"TXT_{i}_BLI" + id_list[v]]))
                                        other.Add($"水泡{form[$"TXT_{i}_BLI" + id_list[v]]}");

                                    if (val == "2" && IsNoEmpty(form[$"TXT_{i}_HUR" + id_list[v]]))
                                        other.Add($"破皮{form[$"TXT_{i}_HUR" + id_list[v]]}");

                                    if (val == "3" && IsNoEmpty(form[$"TXT_{i}_CS" + id_list[v]]))
                                        other.Add($"產瘤{form[$"TXT_{i}_CS" + id_list[v]]}");

                                    if (val == "4" && IsNoEmpty(form[$"TXT_{i}_OTH" + id_list[v]]))
                                        other.Add(form[$"TXT_{i}_OTH" + id_list[v]]);
                                }
                            items.Add($"{bone[i - 1]}：{String.Join("、", other)}");
                        }
                    }
                    #endregion
                    if (items.Count > 0)
                        exceptionResult.Add($"頭部外觀：{String.Join("、", items)}");
                }
                #endregion

                #region 頭部-囪門
                if (form["RB_ASS_1_2" + id_list[v]] == "0")
                {
                    var CK_ASS_1_2F1 = form["CK_ASS_1_2F1" + id_list[v]];
                    if (IsNoEmpty(CK_ASS_1_2F1))
                    {
                        var front = new List<string>();
                        var back = new List<string>();
                        var result = "";

                        var values = CK_ASS_1_2F1.Split(',');
                        if (values.Length > 0)
                            result += $"{(headFlag == false ? "頭部" : "")}囪門：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                front.Add("關閉");
                            if (val == "1")
                                front.Add("凹陷");
                            if (val == "2")
                                front.Add("膨出");
                            if (val == "3" && IsNoEmpty(form["TXT_ASS_1_2F2" + id_list[v]]))
                                front.Add(form["TXT_ASS_1_2F2" + id_list[v]]);

                            if (val == "4")
                                back.Add("關閉");
                            if (val == "5")
                                back.Add("凹陷");
                            if (val == "6")
                                back.Add("膨出");
                            if (val == "7" && IsNoEmpty(form["TXT_ASS_1_2F3" + id_list[v]]))
                                back.Add(form["TXT_ASS_1_2F3" + id_list[v]]);
                        }
                        if (front.Count > 0)
                            result += "前：" + String.Join("、", front);
                        if (back.Count > 0)
                            result += $"{(front.Count > 0 ? "，" : "")}後：{String.Join("、", back)}";

                        exceptionResult.Add(result);
                    }
                }
                #endregion

                #region 顏面
                if (form["RB_ASS_2" + id_list[v]] == "0")
                {
                    var CK_ASS_2F = form["CK_ASS_2F" + id_list[v]];
                    if (IsNoEmpty(CK_ASS_2F))
                    {
                        var left = new List<string>();
                        var right = new List<string>();
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_2F.Split(',');
                        result += "顏面：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                result += "不對稱";

                            if (val == "1")
                                left.Add("嘴角下垂");
                            if (val == "2")
                                left.Add("瘜肉");

                            if (val == "3")
                                right.Add("嘴角下垂");
                            if (val == "4")
                                right.Add("瘜肉");

                            if (val == "7" && IsNoEmpty(form["TXT_ASS_2FO" + id_list[v]]))
                                other.Add(form["TXT_ASS_2FO" + id_list[v]].ToString());
                        }
                        if (left.Count > 0)
                            result += $"，左：{String.Join("、", left)}";
                        if (right.Count > 0)
                            result += $"，右：{String.Join("、", right)}";
                        if (other.Count > 0)
                            result += $"，{String.Join("、", other)}";

                        exceptionResult.Add(result);
                    }
                }
                #endregion

                #region 眼睛
                if (form["RB_ASS_3" + id_list[v]] == "0")
                {
                    var CK_ASS_3F = form["CK_ASS_3F" + id_list[v]];
                    if (IsNoEmpty(CK_ASS_3F))
                    {
                        var left = new List<string>();
                        var right = new List<string>();
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_3F.Split(',');
                        result += "眼睛：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                left.Add("結膜出血");
                            if (val == "1")
                                left.Add("眼瞼水腫");

                            if (val == "2")
                                right.Add("結膜出血");
                            if (val == "3")
                                right.Add("眼瞼水腫");
                            if (val == "4")
                                other.Add("雙眼距離過大");
                            if (val == "5" && IsNoEmpty(form["TXT_ASS_3FO" + id_list[v]]))
                                other.Add(form["TXT_ASS_3FO" + id_list[v]].ToString());
                        }
                        if (left.Count > 0)
                            result += $"左：{String.Join("、", left)}";
                        if (right.Count > 0)
                            result += $"{(left.Count > 0 ? "，" : "")}右：{String.Join("、", right)}";
                        if (other.Count > 0)
                            result += $"{(left.Count > 0 || right.Count > 0 ? "，" : "")}{String.Join("、", other)}";

                        exceptionResult.Add(result);
                    }
                }
                #endregion

                #region 耳朵
                if (form["RB_ASS_4" + id_list[v]] == "0")
                {
                    var CK_ASS_4F = form["CK_ASS_4F" + id_list[v]];
                    if (IsNoEmpty(CK_ASS_4F))
                    {
                        var left = new List<string>();
                        var right = new List<string>();
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_4F.Split(',');
                        result += "耳朵：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                left.Add("低位耳");
                            if (val == "1")
                                left.Add("耳殼異常");
                            if (val == "2")
                                left.Add("瘜肉");

                            if (val == "3")
                                right.Add("低位耳");
                            if (val == "4")
                                right.Add("耳殼異常");
                            if (val == "5")
                                right.Add("瘜肉");

                            if (val == "6" && IsNoEmpty(form["TXT_ASS_4FO" + id_list[v]]))
                                other.Add(form["TXT_ASS_4FO" + id_list[v]].ToString());
                        }
                        if (left.Count > 0)
                            result += $"左：{String.Join("、", left)}";
                        if (right.Count > 0)
                            result += $"{(left.Count > 0 ? "，" : "")}右：{String.Join("、", right)}";
                        if (other.Count > 0)
                            result += $"{(left.Count > 0 || right.Count > 0 ? "，" : "")}{String.Join("、", other)}";

                        exceptionResult.Add(result);
                    }
                }
                #endregion

                #region 口腔
                if (form["RB_ASS_6" + id_list[v]] == "0")
                {
                    var CK_ASS_6F = form["CK_ASS_6F" + id_list[v]];
                    if (IsNoEmpty(CK_ASS_6F))
                    {
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_6F.Split(',');
                        result += "口腔：唇裂與顎裂：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                other.Add("單側不完全唇裂");
                            if (val == "1")
                                other.Add("單側完全唇裂與顎裂");
                            if (val == "2")
                                other.Add("雙側完全唇裂與顎裂");
                            if (val == "3")
                                other.Add("顎裂");
                            if (val == "4" && IsNoEmpty(form["TXT_ASS_6FO" + id_list[v]]))
                                other.Add(form["TXT_ASS_6FO" + id_list[v]].ToString());
                        }
                        result += $"{String.Join("、", other)}";

                        exceptionResult.Add(result);
                    }
                }
                #endregion

                #region 頸部
                if (form["RB_ASS_7" + id_list[v]] == "0")
                {
                    var CK_ASS_7F = form["CK_ASS_7F" + id_list[v]];
                    if (IsNoEmpty(CK_ASS_7F))
                    {
                        var left = new List<string>();
                        var right = new List<string>();
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_7F.Split(',');
                        result += "頸部：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                left.Add("腫塊");
                            if (val == "1")
                                left.Add("疑斜頸");
                            if (val == "2")
                                left.Add("僵硬");

                            if (val == "3")
                                right.Add("腫塊");
                            if (val == "4")
                                right.Add("疑斜頸");
                            if (val == "5")
                                right.Add("僵硬");

                            if (val == "6" && IsNoEmpty(form["TXT_ASS_7FO" + id_list[v]]))
                                other.Add(form["TXT_ASS_7FO" + id_list[v]].ToString());
                        }
                        if (left.Count > 0)
                            result += $"左：{String.Join("、", left)}";
                        if (right.Count > 0)
                            result += $"{(left.Count > 0 ? "，" : "")}右：{String.Join("、", right)}";
                        if (other.Count > 0)
                            result += $"{(left.Count > 0 || right.Count > 0 ? "，" : "")}{String.Join("、", other)}";

                        exceptionResult.Add(result);
                    }
                }
                #endregion

                #region 胸部-外觀
                var chestFlag = false;
                if (form["RB_ASS_8_1" + id_list[v]] == "0")
                {
                    var CK_ASS_8_1F = form["CK_ASS_8_1F" + id_list[v]];
                    if (IsNoEmpty(CK_ASS_8_1F))
                    {
                        var left = new List<string>();
                        var right = new List<string>();
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_8_1F.Split(',');
                        result += "胸部外觀：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                left.Add("腫大");
                            if (val == "1")
                                left.Add("魔乳");

                            if (val == "2")
                                right.Add("腫大");
                            if (val == "3")
                                right.Add("魔乳");

                            if (val == "4" && IsNoEmpty(form["TXT_ASS_8_1FO" + id_list[v]]))
                                other.Add(form["TXT_ASS_8_1FO" + id_list[v]].ToString());
                        }
                        if (left.Count > 0)
                            result += $"，左：{String.Join("、", left)}";
                        if (right.Count > 0)
                            result += $"，右：{String.Join("、", right)}";
                        if (other.Count > 0)
                            result += $"，{String.Join("、", other)}";

                        exceptionResult.Add(result);
                    }
                }
                #endregion

                #region 胸部-呼吸
                if (form["RB_ASS_8_2" + id_list[v]] == "0")
                {
                    var CK_ASS_8_2F = form["CK_ASS_8_2F" + id_list[v]];
                    if (IsNoEmpty(CK_ASS_8_2F))
                    {
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_8_2F.Split(',');
                        result += $"{(chestFlag == false ? "胸部" : "")}呼吸：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                other.Add($"{(IsNoEmpty(form["TXT_ASS_8_2FO" + id_list[v]]) ? "呼吸急促" + form["TXT_ASS_8_2FO" + id_list[v]] + "次/分" : "呼吸急促")}");
                            if (val == "1")
                                other.Add("鼻翼搧動");
                            if (val == "2")
                                other.Add("呻吟聲(grunting)");
                            if (val == "3")
                                other.Add("胸骨凹陷");
                        }
                        result += $"{String.Join("、", other)}";

                        exceptionResult.Add(result);
                        chestFlag = true;
                    }
                }
                #endregion

                #region 胸部-呼吸音
                if (form["RB_ASS_8_3" + id_list[v]] == "0")
                {
                    var CK_ASS_8_3F = form["CK_ASS_8_3F" + id_list[v]];
                    if (IsNoEmpty(CK_ASS_8_3F))
                    {
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_8_3F.Split(',');
                        result += $"{(chestFlag == false ? "胸部" : "")}呼吸音：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                other.Add("Rhonchi 乾囉音");
                            if (val == "1")
                                other.Add("wheeze 喘嗚音");
                            if (val == "2")
                                other.Add("Bronchial 支氣管音");
                            if (val == "3")
                                other.Add("Rub 摩擦音");
                            if (val == "4")
                                other.Add("Crackles 囉音");
                        }
                        result += $"{String.Join("、", other)}";

                        exceptionResult.Add(result);
                        chestFlag = true;
                    }
                }
                #endregion

                #region 胸部-心臟
                if (form["RB_ASS_8_4" + id_list[v]] == "0")
                {
                    var CK_ASS_8_4F = form["CK_ASS_8_4F" + id_list[v]];
                    if (IsNoEmpty(CK_ASS_8_4F))
                    {
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_8_4F.Split(',');
                        result += $"{(chestFlag == false ? "胸部" : "")}心臟：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                other.Add("心跳不規則");
                            if (val == "1")
                                other.Add("雜音");
                            if (val == "2" && IsNoEmpty(form["TXT_ASS_8_4FO" + id_list[v]]))
                                other.Add(form["TXT_ASS_8_4FO" + id_list[v]]);
                        }
                        result += $"{String.Join("、", other)}";

                        exceptionResult.Add(result);
                        chestFlag = true;
                    }
                }
                #endregion

                #region 臀背部
                if (form["RB_ASS_10" + id_list[v]] == "0")
                {
                    var CK_ASS_10F = form["CK_ASS_10F" + id_list[v]];
                    if (IsNoEmpty(CK_ASS_10F))
                    {
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_10F.Split(',');
                        result += $"臀背部：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                other.Add("毛性小凹");
                            if (val == "1")
                                other.Add("脊髓膜膨出");
                            if (val == "2")
                                other.Add("脊椎彎曲");
                            if (val == "3" && IsNoEmpty(form["TXT_ASS_10FO" + id_list[v]]))
                                other.Add(form["TXT_ASS_10FO" + id_list[v]]);
                        }
                        result += $"{String.Join("、", other)}";

                        exceptionResult.Add(result);
                    }
                }
                #endregion

                #region 泌尿生殖器外觀-男-陰囊
                var downFlag = false;
                if (form["RB_ASS_11_2" + id_list[v]] == "0")
                {
                    var CK_ASS_11F = form["CK_ASS_11F" + id_list[v]];
                    if (IsNoEmpty(CK_ASS_11F))
                    {
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_11F.Split(',');
                        result += $"泌尿生殖器外觀男陰囊：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                other.Add("水腫");
                            if (val == "1")
                                other.Add("破皮");
                            if (val == "2" && IsNoEmpty(form["TXT_ASS_11_2FO" + id_list[v]]))
                                other.Add(form["TXT_ASS_11_2FO" + id_list[v]]);
                        }
                        result += $"{String.Join("、", other)}";

                        exceptionResult.Add(result);
                        downFlag = true;
                    }
                }
                #endregion

                #region 泌尿生殖器外觀-肛門
                if (form["RB_ASS_11_8" + id_list[v]] == "0")
                {
                    var CK_ASS_11_8F = form["CK_ASS_11_8F" + id_list[v]];
                    if (IsNoEmpty(CK_ASS_11_8F))
                    {
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_11_8F.Split(',');
                        result += $"{(downFlag == false ? "泌尿生殖器外觀" : "")}肛門：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                other.Add("無肛");
                            if (val == "1")
                                other.Add("閉鎖");
                            if (val == "2")
                                other.Add("瘜肉");
                            if (val == "3")
                                other.Add("裂隙");
                            if (val == "4" && IsNoEmpty(form["TXT_ASS_11_8FO" + id_list[v]]))
                                other.Add(form["TXT_ASS_11_8FO" + id_list[v]]);
                        }
                        result += $"{String.Join("、", other)}";

                        exceptionResult.Add(result);
                        downFlag = true;
                    }
                }
                #endregion

                #region 四肢
                var feetFlag = false;
                if (form["RB_ASS_16" + id_list[v]] == "0")
                {
                    #region 足內翻
                    if (form["RB_ASS_16_1" + id_list[v]] == "1")
                    {
                        var CK_ASS_16_1F = form["CK_ASS_16_1F" + id_list[v]];
                        if (IsNoEmpty(CK_ASS_16_1F))
                        {
                            var other = new List<string>();
                            var result = "";

                            var values = CK_ASS_16_1F.Split(',');
                            result += $"{(feetFlag == false ? "四肢：" : "")}足內翻：";
                            foreach (var val in values)
                            {
                                if (val == "0")
                                    other.Add("左腳");
                                if (val == "1")
                                    other.Add("右腳");
                            }

                            result += $"{String.Join("、", other)}";

                            exceptionResult.Add(result);
                            feetFlag = true;
                        }
                    }
                    #endregion

                    #region 足外翻
                    if (form["RB_ASS_16_2" + id_list[v]] == "1")
                    {
                        var CK_ASS_16_2F = form["CK_ASS_16_2F" + id_list[v]];
                        if (IsNoEmpty(CK_ASS_16_2F))
                        {
                            var other = new List<string>();
                            var result = "";

                            var values = CK_ASS_16_2F.Split(',');
                            result += $"{(feetFlag == false ? "四肢：" : "")}足外翻：";
                            foreach (var val in values)
                            {
                                if (val == "0")
                                    other.Add("左腳");
                                if (val == "1")
                                    other.Add("右腳");
                            }

                            result += $"{String.Join("、", other)}";

                            exceptionResult.Add(result);
                            feetFlag = true;
                        }
                    }
                    #endregion

                    #region 單一手紋(斷掌)
                    if (form["RB_ASS_16_3" + id_list[v]] == "1")
                    {
                        var CK_ASS_16_3F = form["CK_ASS_16_3F" + id_list[v]];
                        if (IsNoEmpty(CK_ASS_16_3F))
                        {
                            var other = new List<string>();
                            var result = "";

                            var values = CK_ASS_16_3F.Split(',');
                            result += $"{(feetFlag == false ? "四肢：" : "")}單一手紋(斷掌)：";
                            foreach (var val in values)
                            {
                                if (val == "0")
                                    other.Add("左手");
                                if (val == "1")
                                    other.Add("右手");
                            }

                            result += $"{String.Join("、", other)}";

                            exceptionResult.Add(result);
                            feetFlag = true;
                        }
                    }
                    #endregion
                }
                #endregion

                #region 四肢-手指
                if (form["RB_ASS_16_7" + id_list[v]] == "0")
                {
                    var CK_ASS_16_7F = form["CK_ASS_16_7F" + id_list[v]];
                    if (IsNoEmpty(CK_ASS_16_7F))
                    {
                        var left = new List<string>();
                        var right = new List<string>();
                        var result = "";

                        var values = CK_ASS_16_7F.Split(',');
                        result += $"{(feetFlag == false ? "四肢 : " : "")}手指：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                left.Add($"{(IsNoEmpty(form["TXT_ASS_16_7F1" + id_list[v]]) ? "少指，部位：" + form["TXT_ASS_16_7F1" + id_list[v]] : "少指")}");
                            if (val == "1")
                                left.Add($"{(IsNoEmpty(form["TXT_ASS_16_7F2" + id_list[v]]) ? "多指，部位：" + form["TXT_ASS_16_7F2" + id_list[v]] : "多指")}");
                            if (val == "2")
                                left.Add($"{(IsNoEmpty(form["TXT_ASS_16_7F3" + id_list[v]]) ? "併指，部位：" + form["TXT_ASS_16_7F3" + id_list[v]] : "併指")}");
                            if (val == "3")
                                left.Add($"{(IsNoEmpty(form["TXT_ASS_16_7F4" + id_list[v]]) ? "蹼狀指，部位：" + form["TXT_ASS_16_7F4" + id_list[v]] : "蹼狀指")}");

                            if (val == "4")
                                right.Add($"{(IsNoEmpty(form["TXT_ASS_16_7F5" + id_list[v]]) ? "少指，部位：" + form["TXT_ASS_16_7F5" + id_list[v]] : "少指")}");
                            if (val == "5")
                                left.Add($"{(IsNoEmpty(form["TXT_ASS_16_7F6" + id_list[v]]) ? "多指，部位：" + form["TXT_ASS_16_7F6" + id_list[v]] : "多指")}");
                            if (val == "6")
                                right.Add($"{(IsNoEmpty(form["TXT_ASS_16_7F7" + id_list[v]]) ? "併指，部位：" + form["TXT_ASS_16_7F7" + id_list[v]] : "併指")}");
                            if (val == "7")
                                right.Add($"{(IsNoEmpty(form["TXT_ASS_16_7F8" + id_list[v]]) ? "蹼狀指，部位：" + form["TXT_ASS_16_7F8" + id_list[v]] : "蹼狀指")}");
                        }

                        if (left.Count > 0)
                            result += $"左：{String.Join("、", left)}";
                        if (right.Count > 0)
                            result += $"{(left.Count > 0 ? "。" : "")}右：{String.Join("、", right)}";

                        exceptionResult.Add(result);
                        feetFlag = true;
                    }
                }
                #endregion

                #region 四肢-腳趾
                if (form["RB_ASS_16_8" + id_list[v]] == "0")
                {
                    var CK_ASS_16_8F = form["CK_ASS_16_8F" + id_list[v]];
                    if (IsNoEmpty(CK_ASS_16_8F))
                    {
                        var left = new List<string>();
                        var right = new List<string>();
                        var result = "";

                        var values = CK_ASS_16_8F.Split(',');
                        result += $"{(feetFlag == false ? "四肢 : " : "")}腳趾：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                left.Add($"{(IsNoEmpty(form["TXT_ASS_16_8F1" + id_list[v]]) ? "少趾，部位：" + form["TXT_ASS_16_8F1" + id_list[v]] : "少趾")}");
                            if (val == "1")
                                left.Add($"{(IsNoEmpty(form["TXT_ASS_16_8F2" + id_list[v]]) ? "多趾，部位：" + form["TXT_ASS_16_8F2" + id_list[v]] : "多趾")}");
                            if (val == "2")
                                left.Add($"{(IsNoEmpty(form["TXT_ASS_16_8F3" + id_list[v]]) ? "併趾，部位：" + form["TXT_ASS_16_8F3" + id_list[v]] : "併趾")}");

                            if (val == "3")
                                left.Add($"{(IsNoEmpty(form["TXT_ASS_16_8F4" + id_list[v]]) ? "少趾，部位：" + form["TXT_ASS_16_8F4" + id_list[v]] : "少趾")}");
                            if (val == "4")
                                left.Add($"{(IsNoEmpty(form["TXT_ASS_16_8F5" + id_list[v]]) ? "多趾，部位：" + form["TXT_ASS_16_8F5" + id_list[v]] : "多趾")}");
                            if (val == "5")
                                left.Add($"{(IsNoEmpty(form["TXT_ASS_16_8F6" + id_list[v]]) ? "併趾，部位：" + form["TXT_ASS_16_8F6" + id_list[v]] : "併趾")}");
                        }

                        if (left.Count > 0)
                            result += $"左：{String.Join("、", left)}";
                        if (right.Count > 0)
                            result += $"{(left.Count > 0 ? "。" : "")}右：{String.Join("、", right)}";

                        exceptionResult.Add(result);
                        feetFlag = true;
                    }
                }
                #endregion

                if (exceptionResult.Count > 0)
                    finalResult += String.Join("。", exceptionResult);
                else
                    finalResult += "無異常項目";
                DataTable babylink = obs_m.sel_nbcha("", ptinfo.ChartNo);
                if (babylink != null && babylink.Rows.Count > 0)
                {
                    mom_feeno = babylink.Rows[0]["MOM_FEE_NO"].ToString();
                }

                var CareRecord = new CareRecord();
                DataTable dt = CareRecord.sel_carerecord(id_list[v] + "OBS_BABYENTR");

                if (form["type"].ToString() != "暫存")
                {
                    if (dt == null || dt.Rows.Count == 0)
                    {
                        erow += base.Insert_CareRecordTns(dateNow, id_list[v], "新生兒入院護理評估", finalResult + "。", "", "", "", "", "OBS_BABYENTR", ref link);
                    }
                    else
                    {
                        erow += base.Upd_CareRecord(dateNow, id_list[v], "新生兒入院護理評估", finalResult + "。", "", "", "", "", "OBS_BABYENTR");
                    }
                }
                else if (form["type"].ToString() == "暫存")
                {
                    // 讓下方判斷正確
                    erow += 1;
                }

                #endregion
            }
            if (erow == id_list.Length * 2)
            {
                string return_jsonstr = string.Empty;

                if (save_type == "已完成")
                {

                    PdfEmr.ControllerContext = ControllerContext;
                    PdfEmr.userinfo = userinfo;
                    PdfEmr.ptinfo = ptinfo;
                    return_jsonstr = PdfEmr.GetPDF_EMR("Save", feeno, userno, "Insert_NBENTR", tableid, mom_feeno);
                    json_result = JsonConvert.DeserializeObject<RESPONSE_MSG>(return_jsonstr);
                }
                else
                {
                    json_result.status = RESPONSE_STATUS.SUCCESS;
                    json_result.message = "";

                }


            }
            else
            {
                //Response.Write("<script>alert('更新失敗');window.location.href='../BabyBorn/NBENTR_List';</script>");
                json_result.status = RESPONSE_STATUS.ERROR;
                json_result.message = "更新失敗";
            }

            return Content(JsonConvert.SerializeObject(json_result), "application/json");
        }
        #endregion

        #region 新生兒入院護理評估刪除
        /// <summary>
        /// 新生兒入院護理評估刪除
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        [HttpPost]
        public string Del_NBENTR(string IID)
        {
            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("DELETED", userinfo.EmployeesNo, DBItem.DBDataType.String));
            string where = " IID = '" + IID + "' ";

            insertDataList = setNullToEmpty(insertDataList);
            int erow_ASS_1_1F = obs_m.DBExecUpdate("OBS_ASS_1_1F", insertDataList, where);


            if (erow_ASS_1_1F >= 0)
            {
                insertDataList = setNullToEmpty(insertDataList);
                int erow = obs_m.DBExecUpdate("OBS_NBENTR", insertDataList, where);
                if (erow > 0)
                {
                    del_emr(IID, userinfo.EmployeesNo);
                    return "1";
                    // Response.Write("<script>alert('刪除成功');window.location.href='../BabyBorn/BabyEntr_List';</script>");

                }
                else
                {
                    return "-1";
                    //Response.Write("<script>alert('刪除失敗');window.location.href='../BabyBorn/BabyEntr_List';</script>");

                }
            }
            else
                return "-1";
            //Response.Write("<script>alert('刪除失敗');window.location.href='../BabyBorn/BabyEntr_List';</script>");
        }
        #endregion

        #region 雙人覆核(新增)
        /// <summary>
        /// 雙人覆核(新增)
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        public string InsDoubleChk(FormCollection form)
        {
            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;

            var type = form["CHKTYPE"];

            var chkuser = "";
            var chkupass = "";

            if (type == "HBEAG")
            {
                chkuser = form["TXT_HBEAG_RCA"];
                chkupass = form["TXT_HBEAG_RCP"];
            }
            else if (type == "HBSAG")
            {
                chkuser = form["TXT_HBSAG_RCA"];
                chkupass = form["TXT_HBSAG_RCP"];
            }

            if (chkuser == userno)
                return "執行覆核者不得與建立者同一人!";

            byte[] listByteCode = webService.UserLogin(chkuser, chkupass);
            if (listByteCode == null)
                return "帳號密碼錯誤!";

            return "覆核成功!";
        }
        #endregion

        #region 雙人覆核(編輯)
        /// <summary>
        /// 雙人覆核(編輯)
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        public string Doublechk(FormCollection form)
        {
            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;
            string[] id_list = form["IID"].Split(',');

            string Type = form["CHKTYPE"];

            List<DBItem> insertDataList = new List<DBItem>();
            int erow = 0;
            for (int v = 0; v < id_list.Length; v++)
            {
                if (Type == "HBEAG")
                {
                    var chkuser = form["TXT_HBEAG_RCA" + id_list[v]];
                    var chkupass = form["TXT_HBEAG_RCP" + id_list[v]];

                    if (chkuser == form["CREATNO"])
                        return "執行覆核者不得與建立者同一人!";

                    byte[] listByteCode = webService.UserLogin(chkuser, chkupass);
                    if (listByteCode == null)
                        return "帳號密碼錯誤!";

                    insertDataList.Add(new DBItem("HBEAG", form["DDL_HBEAG" + id_list[v]], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("HBEAG_INST", form["TXT_HBEAG_INST" + id_list[v]], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("HBEAG_RCA", form["TXT_HBEAG_RCA" + id_list[v]], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("HBEAG_RCR", form["TXT_HBEAG_RCR" + id_list[v]], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("HBEAG_RCT", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                }
                if (Type == "HBSAG")
                {
                    var chkuser = form["TXT_HBSAG_RCA" + id_list[v]];
                    var chkupass = form["TXT_HBSAG_RCP" + id_list[v]];

                    if (chkuser == form["CREATNO"])
                        return "執行覆核者不得與建立者同一人!";

                    byte[] listByteCode = webService.UserLogin(chkuser, chkupass);
                    if (listByteCode == null)
                        return "帳號密碼錯誤!";

                    insertDataList.Add(new DBItem("HBSAG", form["DDL_HBSAG" + id_list[v]], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("HBSAG_INST", form["TXT_HBSAG_INST" + id_list[v]], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("HBSAG_RCA", form["TXT_HBSAG_RCA" + id_list[v]], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("HBSAG_RCR", form["TXT_HBSAG_RCR" + id_list[v]], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("HBSAG_RCT", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                }
                string where = " IID = '" + id_list[v] + "' ";
                insertDataList = setNullToEmpty(insertDataList);
                erow += obs_m.DBExecUpdate("OBS_NBENTR", insertDataList, where);
            }
            if (erow > 0)
            {
                return "覆核成功!";
            }
            else
                return "覆核失敗!";
        }
        #endregion

        #region 雙人覆核取消(編輯)
        /// <summary>
        /// 雙人覆核取消(編輯)
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        public string DoublechkCancel(FormCollection form)
        {
            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;
            string[] id_list = form["IID"].Split(',');

            string Type = form["CHKTYPE"];
            List<DBItem> insertDataList = new List<DBItem>();
            int erow = 0;
            for (int v = 0; v < id_list.Length; v++)
            {
                if (Type == "HBEAG")
                {
                    insertDataList.Add(new DBItem("HBEAG_RCA", "", DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("HBEAG_RCR", "", DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("HBEAG_RCT", null, DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("HBEAG_RCCT", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                }
                if (Type == "HBSAG")
                {
                    insertDataList.Add(new DBItem("HBSAG_RCA", "", DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("HBSAG_RCR", "", DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("HBSAG_RCT", null, DBItem.DBDataType.DataTime));
                    insertDataList.Add(new DBItem("HBSAG_RCCT", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));

                }
                string where = " IID = '" + id_list[v] + "' ";
                insertDataList = setNullToEmpty(insertDataList);
                erow += obs_m.DBExecUpdate("OBS_NBENTR", insertDataList, where);
            }
            if (erow > 0)
            {
                return "取消覆核成功!";
            }
            else
                return "取消覆核失敗!";
        }
        #endregion


        #region 嬰幼兒入院護理評估單
        /// <summary>
        /// 嬰幼兒入院護理評估單
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        public ActionResult BabyEntr_List(string feeno, string chartno, bool IsHisView = false)
        {
            ViewBag.IsHisView = IsHisView;

            if (IsHisView)
            {
                his.ControllerContext = ControllerContext;
                his.getSession(feeno);
            }

            if (Session["PatInfo"] != null)
            {
                if (feeno == "" || feeno == null)
                    feeno = ptinfo.FeeNo;
                DataTable dt = obs_m.sel_babyentr(feeno, "", "");
                dt.Columns.Add("UPDNAME");
                dt.Columns.Add("IS_CREATNO");
                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow r in dt.Rows)
                    {
                        if (r["UPDNO"].ToString() != "")
                        {
                            byte[] listByteCode = webService.UserName(r["UPDNO"].ToString());
                            if (listByteCode == null)
                                r["UPDNAME"] = "";
                            else
                            {
                                string listJsonArray = CompressTool.DecompressString(listByteCode);
                                UserInfo user_name = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                                r["UPDNAME"] = user_name.EmployeesName;
                            }
                        }

                        if (!IsHisView)
                        {
                            if (r["CREATNO"].ToString() == userinfo.EmployeesNo)
                                r["IS_CREATNO"] = true;
                            else
                                r["IS_CREATNO"] = false;
                        }
                        else
                        {
                            r["IS_CREATNO"] = false;
                        }
                    }
                }
                ViewBag.dt = set_dt(dt);
                if (!IsHisView)
                {
                    ViewBag.LoginEmployeesName = userinfo.EmployeesName;
                }
                ViewBag.feeno = feeno;
                ViewBag.RootDocument = GetSourceUrl();
                return View();
            }
            Response.Write("<script>alert('請重新選擇病患');</script>");
            return new EmptyResult();
        }
        #endregion

        #region 嬰幼兒入院護理評估單列印
        /// <summary>
        /// 嬰幼兒入院護理評估單列印
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        public ActionResult Insert_BabyEntr_PDF(string feeno, string tableid)
        {
            PatientInfo pinfo = new PatientInfo();
            byte[] ByteCode = webService.GetPatientInfo(feeno);
            string JosnArr = "";
            //病人資訊
            if (ByteCode != null)
            {
                JosnArr = CompressTool.DecompressString(ByteCode);
                pinfo = JsonConvert.DeserializeObject<PatientInfo>(JosnArr);
            }
            ViewData["ptinfo"] = pinfo;

            if (tableid != "" && tableid != null)
            {
                DataTable dt = obs_m.sel_babyentr(feeno, tableid, "");
                ViewBag.dt = set_dt(dt);

                DataTable dt_CT = obs_m.sel_babyentr_ct(feeno, tableid, "");
                ViewBag.dt_CT = set_dt(dt_CT);

                DataTable dt_HO = obs_m.sel_babyentr_ho(feeno, tableid, "");
                ViewBag.dt_HO = set_dt(dt_HO);

                DataTable dt_SG = obs_m.sel_babyentr_sg(feeno, tableid, "");
                ViewBag.dt_SG = set_dt(dt_SG);

                DataTable dt_ASS_1F = obs_m.sel_ass_1_1f(feeno, "BABYENTR", tableid, "");
                ViewBag.dt_ASS_1F = set_dt(dt_ASS_1F);
            }
            return View();
        }
        #endregion

        #region 嬰幼兒入院護理評估單新增畫面
        /// <summary>
        /// 嬰幼兒入院護理評估單畫面
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        public ActionResult Insert_BabyEntr(string IID, bool IsHisView = false)
        {
            ViewBag.IsHisView = IsHisView;

            if (IID != "" && IID != null)
            {
                DataTable dt = obs_m.sel_babyentr(ptinfo?.FeeNo, IID, "");
                foreach (DataRow r in dt.Rows)
                {
                    if (!IsHisView)
                    {
                        if (r["CREATNO"].ToString() == userinfo.EmployeesNo)
                            ViewBag.CREATNO = true;
                        else
                            ViewBag.CREATNO = false;
                    }
                    else
                    {
                        ViewBag.CREATNO = false;
                    }
                }
                ViewBag.dt = set_dt(dt);

                DataTable dt_CT = obs_m.sel_babyentr_ct(ptinfo?.FeeNo, IID, "");
                ViewBag.dt_CT = set_dt(dt_CT);

                DataTable dt_HO = obs_m.sel_babyentr_ho(ptinfo?.FeeNo, IID, "");
                ViewBag.dt_HO = set_dt(dt_HO);

                DataTable dt_SG = obs_m.sel_babyentr_sg(ptinfo?.FeeNo, IID, "");
                ViewBag.dt_SG = set_dt(dt_SG);

                DataTable dt_ASS_1F = obs_m.sel_ass_1_1f(ptinfo?.FeeNo, "BABYENTR", IID, "");
                ViewBag.dt_ASS_1F = set_dt(dt_ASS_1F);

                ViewBag.IsNB = false;
            }
            else
            {
                ViewBag.CREATNO = null;

                //先抓嬰幼兒
                DataTable dt_new = obs_m.sel_babyentr_seq(ptinfo?.FeeNo, "");
                if (dt_new != null && dt_new.Rows.Count > 0)
                {
                    ViewBag.dt_new = set_dt(dt_new);
                    DataTable dt_CT = obs_m.sel_babyentr_ct(ptinfo?.FeeNo, dt_new.Rows[0]["IID"].ToString(), "");
                    ViewBag.dt_CT = set_dt(dt_CT);

                    DataTable dt_HO = obs_m.sel_babyentr_ho(ptinfo?.FeeNo, dt_new.Rows[0]["IID"].ToString(), "");
                    ViewBag.dt_HO = set_dt(dt_HO);

                    DataTable dt_SG = obs_m.sel_babyentr_sg(ptinfo?.FeeNo, dt_new.Rows[0]["IID"].ToString(), "");
                    ViewBag.dt_SG = set_dt(dt_SG);

                    ViewBag.IsNB = false;
                }
                else
                {
                    //沒有再抓新生兒
                    DataTable dt_nb_new = obs_m.sel_nbentr(ptinfo?.FeeNo, "", "");
                    ViewBag.dt_new = set_dt(dt_nb_new);
                    ViewBag.IsNB = true;
                }
            }
            return View();
        }
        #endregion

        #region 嬰幼兒入院護理評估單新增
        /// <summary>
        /// 嬰幼兒入院護理評估單新增
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Insert_BabyEntr(FormCollection form)
        {
            RESPONSE_MSG json_result = new RESPONSE_MSG();
            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;
            string id = base.creatid("OBS_BABYENTR", userno, feeno, "0");

            link.DBOpen();

            var dateNow = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("IID", id, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATTIME", dateNow, DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("RECORDTIME", dateNow, DBItem.DBDataType.DataTime)); //紀錄時間

            insertDataList.Add(new DBItem("DESCRIPTION", "入院護理評估-嬰幼兒", DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("STATUS", form["TYPE"], DBItem.DBDataType.String));

            var lastSeq = 0;
            DataTable dt_babyentr = obs_m.sel_babyentr_seq(feeno);
            if (dt_babyentr != null && dt_babyentr.Rows.Count > 0)
                lastSeq = Convert.ToInt32(dt_babyentr.Rows[0]["VERSION"]);
            //add by chih,20200826
            else
            {
                byte[] listByteCode = webService.GetBabyFeeNo2(ptinfo.ChartNo, ptinfo.FeeNo);
                if (listByteCode == null) { }
                else
                {
                    string listJsonArray = CompressTool.DecompressString(listByteCode);
                    BabyFeeNo2[] baby_feeno2 = JsonConvert.DeserializeObject<BabyFeeNo2[]>(listJsonArray);

                    List<DBItem> insertDataList_baby = new List<DBItem>();
                    insertDataList_baby.Add(new DBItem("BABY_FEE_NO2", ptinfo.FeeNo, DBItem.DBDataType.String));

                    string where = " BABY_FEE_NO = '" + baby_feeno2[0].NB_FEENO
                        + "' AND BABY_CHART_NO = '" + ptinfo.ChartNo + "'";
                    obs_m.DBExecUpdate("OBS_BABYLINK_DATA", insertDataList_baby, where);
                }
            }
            // end add by chih,20200826
            var seq = (Convert.ToInt32(lastSeq) + 1).ToString();

            insertDataList.Add(new DBItem("VERSION", seq, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("BIRTH_PLACE", form["RB_BIRTH_PLACE"], DBItem.DBDataType.String));

            #region 基本資料
            if (form["TXT_ADM_DAY_DAY"] != "" && form["TXT_ADM_DAY_DAY"] != null && form["TXT_ADM_DAY_TIME"] != "" && form["TXT_ADM_DAY_TIME"] != null)
                insertDataList.Add(new DBItem("ADM_DAY", form["TXT_ADM_DAY_DAY"] + " " + form["TXT_ADM_DAY_TIME"], DBItem.DBDataType.DataTime));

            insertDataList.Add(new DBItem("FROM_WHERE", form["RB_FROM_WHERE"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("PROCESS", form["TXT_PROCESS"], DBItem.DBDataType.String));
            if (form["TXT_BIRTH_DAY"] != "" && form["TXT_BIRTH_DAY"] != null && form["TXT_BIRTH_TIME"] != "" && form["TXT_BIRTH_TIME"] != null)
                insertDataList.Add(new DBItem("BIRTH", form["TXT_BIRTH_DAY"] + " " + form["TXT_BIRTH_TIME"], DBItem.DBDataType.DataTime));

            insertDataList.Add(new DBItem("PARITY", form["TXT_PARITY"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("GEST_M", form["TXT_GEST_M"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("GEST_D", form["TXT_GEST_D"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("WEIGHT", form["TXT_WEIGHT"], DBItem.DBDataType.String));

            var BIRTH_TYPE = form["RB_BIRTH_TYPE"];
            insertDataList.Add(new DBItem("BIRTH_TYPE", BIRTH_TYPE, DBItem.DBDataType.String));
            if (BIRTH_TYPE == "2")
                insertDataList.Add(new DBItem("BIRTH_TYPE_OTH", form["TXT_BIRTH_TYPE_OTH"], DBItem.DBDataType.String));
            else
                insertDataList.Add(new DBItem("BIRTH_TYPE_OTH", null, DBItem.DBDataType.String));

            var BIRTH_CON = form["RB_BIRTH_CON"];
            insertDataList.Add(new DBItem("BIRTH_CON", BIRTH_CON, DBItem.DBDataType.String));
            if (BIRTH_CON == "4")
                insertDataList.Add(new DBItem("BIRTH_CON_OTH", form["TXT_BIRTH_CON_OTH"], DBItem.DBDataType.String));
            else
                insertDataList.Add(new DBItem("BIRTH_CON_OTH", null, DBItem.DBDataType.String));

            #region 過敏史藥物
            var DRUG_ALL = form["RB_DRUG_ALL"];
            insertDataList.Add(new DBItem("DRUG_ALL", DRUG_ALL, DBItem.DBDataType.String));
            if (DRUG_ALL == "1")
            {
                var DRUG_ALL_ITM = form["CK_DRUG_ALL_ITM"];
                var DRUG_ALL_ITMV = new List<string>() { "0", "0", "0", "0", "0", "0", "0", "0", "0", "0" };
                if (DRUG_ALL_ITM == null)
                    insertDataList.Add(new DBItem("DRUG_ALL_ITM", String.Join("", DRUG_ALL_ITMV), DBItem.DBDataType.String));
                else
                {
                    var DRUG_ALL_ITM_SP = DRUG_ALL_ITM.Split(',');
                    foreach (var i in DRUG_ALL_ITM_SP)
                        DRUG_ALL_ITMV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("DRUG_ALL_ITM", String.Join("", DRUG_ALL_ITMV), DBItem.DBDataType.String));
                }

                //匹林系藥物(pyrin)
                if (DRUG_ALL_ITMV[1] == "1")
                    insertDataList.Add(new DBItem("DRUG_ALL_ITM1", form["TXT_DRUG_ALL_ITM1"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("DRUG_ALL_ITM1", null, DBItem.DBDataType.String));

                //非類固醇抗炎藥物(NSAID)
                if (DRUG_ALL_ITMV[3] == "1")
                    insertDataList.Add(new DBItem("DRUG_ALL_ITM3", form["TXT_DRUG_ALL_ITM3"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("DRUG_ALL_ITM3", null, DBItem.DBDataType.String));

                //磺氨類
                if (DRUG_ALL_ITMV[5] == "1")
                    insertDataList.Add(new DBItem("DRUG_ALL_ITM5", form["TXT_DRUG_ALL_ITM5"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("DRUG_ALL_ITM5", null, DBItem.DBDataType.String));

                //盤尼西林類
                if (DRUG_ALL_ITMV[6] == "1")
                    insertDataList.Add(new DBItem("DRUG_ALL_ITM6", form["TXT_DRUG_ALL_ITM6"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("DRUG_ALL_ITM6", null, DBItem.DBDataType.String));

                //抗生素類
                if (DRUG_ALL_ITMV[7] == "1")
                    insertDataList.Add(new DBItem("DRUG_ALL_ITM7", form["TXT_DRUG_ALL_ITM7"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("DRUG_ALL_ITM7", null, DBItem.DBDataType.String));

                //麻醉藥
                if (DRUG_ALL_ITMV[8] == "1")
                    insertDataList.Add(new DBItem("DRUG_ALL_ITM8", form["TXT_DRUG_ALL_ITM8"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("DRUG_ALL_ITM8", null, DBItem.DBDataType.String));

                //其他
                if (DRUG_ALL_ITMV[9] == "1")
                    insertDataList.Add(new DBItem("DRUG_ALL_OTH", form["TXT_DRUG_ALL_OTH"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("DRUG_ALL_OTH", null, DBItem.DBDataType.String));
            }
            else
            {
                insertDataList.Add(new DBItem("DRUG_ALL_ITM", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("DRUG_ALL_ITM1", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("DRUG_ALL_ITM3", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("DRUG_ALL_ITM5", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("DRUG_ALL_ITM6", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("DRUG_ALL_ITM7", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("DRUG_ALL_ITM8", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("DRUG_ALL_OTH", null, DBItem.DBDataType.String));
            }

            #endregion

            #region 過敏史食物
            var FOOD_ALL = form["RB_FOOD_ALL"];
            insertDataList.Add(new DBItem("FOOD_ALL", FOOD_ALL, DBItem.DBDataType.String));
            if (FOOD_ALL == "1")
            {
                var FOOD_ALL_ITM = form["CK_FOOD_ALL_ITM"];
                var FOOD_ALL_ITMV = new List<string>() { "0", "0", "0", "0", "0", "0" };
                if (FOOD_ALL_ITM == null)
                    insertDataList.Add(new DBItem("FOOD_ALL_ITM", String.Join("", FOOD_ALL_ITMV), DBItem.DBDataType.String));
                else
                {
                    var FOOD_ALL_ITM_SP = FOOD_ALL_ITM.Split(',');
                    foreach (var i in FOOD_ALL_ITM_SP)
                        FOOD_ALL_ITMV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("FOOD_ALL_ITM", String.Join("", FOOD_ALL_ITMV), DBItem.DBDataType.String));
                }

                //海鮮類
                if (FOOD_ALL_ITMV[1] == "1")
                    insertDataList.Add(new DBItem("FOOD_ALL_ITM1", form["TXT_FOOD_ALL_ITM1"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("FOOD_ALL_ITM1", null, DBItem.DBDataType.String));

                //水果
                if (FOOD_ALL_ITMV[3] == "1")
                    insertDataList.Add(new DBItem("FOOD_ALL_ITM3", form["TXT_FOOD_ALL_ITM3"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("FOOD_ALL_ITM3", null, DBItem.DBDataType.String));

                //其他
                if (FOOD_ALL_ITMV[5] == "1")
                    insertDataList.Add(new DBItem("FOOD_ALL_OTH", form["TXT_FOOD_ALL_OTH"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("FOOD_ALL_OTH", null, DBItem.DBDataType.String));
            }
            else
            {
                insertDataList.Add(new DBItem("FOOD_ALL_ITM", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FOOD_ALL_ITM1", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FOOD_ALL_ITM3", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FOOD_ALL_OTH", null, DBItem.DBDataType.String));
            }
            #endregion

            #region 過敏史其他
            var OTH_ALL = form["RB_OTH_ALL"];
            insertDataList.Add(new DBItem("OTH_ALL", OTH_ALL, DBItem.DBDataType.String));
            if (OTH_ALL == "1")
            {
                var OTH_ALL_ITM = form["CK_OTH_ALL_ITM"];
                var OTH_ALL_ITMV = new List<string>() { "0", "0", "0", "0", "0" };
                if (OTH_ALL_ITM == null)
                    insertDataList.Add(new DBItem("OTH_ALL_ITM", String.Join("", OTH_ALL_ITMV), DBItem.DBDataType.String));
                else
                {
                    var OTH_ALL_ITM_SP = OTH_ALL_ITM.Split(',');
                    foreach (var i in OTH_ALL_ITM_SP)
                        OTH_ALL_ITMV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("OTH_ALL_ITM", String.Join("", OTH_ALL_ITMV), DBItem.DBDataType.String));
                }

                if (OTH_ALL_ITMV[4] == "1")
                    insertDataList.Add(new DBItem("OTH_ALL_OTH", form["TXT_OTH_ALL_OTH"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("OTH_ALL_OTH", null, DBItem.DBDataType.String));
            }
            else
            {
                insertDataList.Add(new DBItem("OTH_ALL_ITM", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("OTH_ALL_OTH", null, DBItem.DBDataType.String));
            }

            #endregion

            insertDataList.Add(new DBItem("MAINCARE_N", form["TXT_MAINCARE_N"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("MAINCARE_T", form["TXT_MAINCARE_T"], DBItem.DBDataType.String));

            //父親資料
            insertDataList.Add(new DBItem("MAT_NAME", form["TXT_MAT_NAME"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("MAT_AGE", form["TXT_MAT_AGE"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("MAT_OCU", form["TXT_MAT_OCU"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("MAT_EDU", form["RB_MAT_EDU"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("MAT_TEL", form["TXT_MAT_TEL"], DBItem.DBDataType.String));

            //母親資料
            insertDataList.Add(new DBItem("PAT_NAME", form["TXT_PAT_NAME"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("PAT_AGE", form["TXT_PAT_AGE"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("PAT_OCU", form["TXT_PAT_OCU"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("PAT_EDU", form["RB_PAT_EDU"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("PAT_TEL", form["TXT_PAT_TEL"], DBItem.DBDataType.String));

            insertDataList.Add(new DBItem("SOURCE", form["RB_SOURCE"], DBItem.DBDataType.String));

            #endregion

            #region 個人病史
            var DISE = form["RB_DISE"];
            insertDataList.Add(new DBItem("DISE", DISE, DBItem.DBDataType.String));
            if (DISE == "1")
            {
                var DISE_ITM = form["CK_DISE_ITM"];
                var DISE_ITMV = new List<string>() { "0", "0", "0", "0", "0", "0", "0", "0" };
                if (DISE_ITM == null)
                    insertDataList.Add(new DBItem("DISE_ITM", String.Join("", DISE_ITMV), DBItem.DBDataType.String));
                else
                {
                    var DISE_ITM_SP = DISE_ITM.Split(',');
                    foreach (var i in DISE_ITM_SP)
                        DISE_ITMV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("DISE_ITM", String.Join("", DISE_ITMV), DBItem.DBDataType.String));
                }

                //感冒
                if (DISE_ITMV[6] == "1")
                    insertDataList.Add(new DBItem("DISE_ITM_CD", form["TXT_DISE_ITM_CD"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("DISE_ITM_CD", null, DBItem.DBDataType.String));

                //其他
                if (DISE_ITMV[7] == "1")
                    insertDataList.Add(new DBItem("DISE_OTH", form["TXT_DISE_OTH"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("DISE_OTH", null, DBItem.DBDataType.String));
            }
            else
            {
                insertDataList.Add(new DBItem("DISE_ITM", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("DISE_ITM_CD", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("DISE_OTH", null, DBItem.DBDataType.String));
            }

            insertDataList.Add(new DBItem("BT", form["RB_BT"], DBItem.DBDataType.String));

            var BT_ALL = form["RB_BT_ALL"];
            insertDataList.Add(new DBItem("BT_ALL", BT_ALL, DBItem.DBDataType.String));
            if (BT_ALL == "1")
                insertDataList.Add(new DBItem("BT_ALL_N", form["TXT_BT_ALL_N"], DBItem.DBDataType.String));
            else
                insertDataList.Add(new DBItem("BT_ALL_N", null, DBItem.DBDataType.String));


            var MED = form["RB_MED"];
            insertDataList.Add(new DBItem("MED", MED, DBItem.DBDataType.String));
            if (MED == "1")
                insertDataList.Add(new DBItem("MED_ITM", form["TXT_MED_ITM"], DBItem.DBDataType.String));
            else
                insertDataList.Add(new DBItem("MED_ITM", null, DBItem.DBDataType.String));

            //先天疾患
            var CON_DIS = form["RB_CON_DIS"];
            insertDataList.Add(new DBItem("CON_DIS", CON_DIS, DBItem.DBDataType.String));
            if (CON_DIS == "1")
            {
                var CON_DIS_ITM = form["RB_CON_DIS_ITM"];
                insertDataList.Add(new DBItem("CON_DIS_ITM", CON_DIS_ITM, DBItem.DBDataType.String));
                if (CON_DIS_ITM == "2")
                    insertDataList.Add(new DBItem("CON_DIS_OTH", form["TXT_CON_DIS_OTH"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("CON_DIS_OTH", null, DBItem.DBDataType.String));
            }
            else
            {
                insertDataList.Add(new DBItem("CON_DIS_ITM", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CON_DIS_OTH", null, DBItem.DBDataType.String));
            }

            //新生兒代謝疾病篩檢
            var NBSCREEN = form["RB_NBSCREEN"];
            insertDataList.Add(new DBItem("NBSCREEN", NBSCREEN, DBItem.DBDataType.String));
            if (NBSCREEN == "0")
                insertDataList.Add(new DBItem("NBSCREEN_N", form["TXT_NBSCREEN_N"], DBItem.DBDataType.String));
            else
                insertDataList.Add(new DBItem("NBSCREEN_N", null, DBItem.DBDataType.String));
            #endregion

            #region 預防接種
            insertDataList.Add(new DBItem("HBIG", form["RB_HBIG"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("HEP_B_1", form["RB_HEP_B_1"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("HEP_B_2", form["RB_HEP_B_2"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FIVE_IN1_1", form["RB_FIVE_IN1_1"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FIVE_IN1_2", form["RB_FIVE_IN1_2"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("SIX_IN1_1", form["RB_SIX_IN1_1"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("SIX_IN1_2", form["RB_SIX_IN1_2"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("PCV_1", form["RB_PCV_1"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("PCV_2", form["RB_PCV_2"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("ROTA_1", form["RB_ROTA_1"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("ROTA_2", form["RB_ROTA_2"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("VAC_OTH", form["TXT_VAC_OTH"], DBItem.DBDataType.String));
            #endregion

            #region 身體評估
            #region 1.頭部
            insertDataList.Add(new DBItem("ASS_1_1", form["RB_ASS_1_1"], DBItem.DBDataType.String));

            var ADD_ITEM = new List<string>() {
                    form["CK_ASS_1_1F1"] != null ? "1" : "0",
                    form["CK_ASS_1_1F2"] != null ? "1" : "0",
                    form["CK_ASS_1_1F3"] != null ? "1" : "0",
                    form["CK_ASS_1_1F4"] != null ? "1" : "0",
                    form["CK_ASS_1_1F5"] != null ? "1" : "0",
                    };

            var param = 0;
            foreach (var a in ADD_ITEM)
            {
                param = ++param;
                List<DBItem> data = new List<DBItem>();
                data.Add(new DBItem("IID", id, DBItem.DBDataType.String));
                data.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                data.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
                data.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                data.Add(new DBItem("RECORDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                data.Add(new DBItem("TYPE", "BABYENTR", DBItem.DBDataType.String));
                data.Add(new DBItem("SNO", param.ToString(), DBItem.DBDataType.String));
                var ITM = "";
                switch (param.ToString())
                {
                    case "1":
                        ITM = "頭骨";
                        break;
                    case "2":
                        ITM = "頂骨";
                        break;
                    case "3":
                        ITM = "左顳骨";
                        break;
                    case "4":
                        ITM = "右顳骨";
                        break;
                    case "5":
                        ITM = "枕骨";
                        break;
                }

                data.Add(new DBItem("ITM", ITM, DBItem.DBDataType.String));

                if (a == "1")
                {
                    var ASS_1_1F = form[$"CK_ASS_1_1F{param}"];
                    var ASS_1_1FV = new List<string>() { "0", "0", "0", "0", "0" };

                    var ASS_1_1F_SP = ASS_1_1F.Split(',');
                    foreach (var i in ASS_1_1F_SP)
                        ASS_1_1FV[Convert.ToInt32(i)] = "1";

                    data.Add(new DBItem("ITM_REMARK", String.Join("", ASS_1_1FV), DBItem.DBDataType.String));

                    if (ASS_1_1FV[0] == "1")
                        data.Add(new DBItem("ITM_RED_NO", form[$"TXT_{param}_RED"], DBItem.DBDataType.String));
                    else
                        data.Add(new DBItem("ITM_RED_NO", null, DBItem.DBDataType.String));

                    if (ASS_1_1FV[1] == "1")
                        data.Add(new DBItem("ITM_BLI_NO", form[$"TXT_{param}_BLI"], DBItem.DBDataType.String));
                    else
                        data.Add(new DBItem("ITM_BLI_NO", null, DBItem.DBDataType.String));

                    if (ASS_1_1FV[2] == "1")
                        data.Add(new DBItem("ITM_HUR_NO", form[$"TXT_{param}_HUR"], DBItem.DBDataType.String));
                    else
                        data.Add(new DBItem("ITM_HUR_NO", null, DBItem.DBDataType.String));

                    if (ASS_1_1FV[3] == "1")
                        data.Add(new DBItem("ITM_CS_NO", form[$"TXT_{param}_CS"], DBItem.DBDataType.String));
                    else
                        data.Add(new DBItem("ITM_CS_NO", null, DBItem.DBDataType.String));

                    if (ASS_1_1FV[4] == "1")
                        data.Add(new DBItem("ITM_OTH_NO", form[$"TXT_{param}_OTH"], DBItem.DBDataType.String));
                    else
                        data.Add(new DBItem("ITM_OTH_NO", null, DBItem.DBDataType.String));
                }
                data = setNullToEmpty(data);
                if (link.DBExecInsertTns("OBS_ASS_1_1F", data) < 0)
                {
                    link.DBRollBack();
                    json_result.status = RESPONSE_STATUS.ERROR;
                    json_result.message = "新增失敗";
                    return Content(JsonConvert.SerializeObject(json_result), "application/json");

                    //return "-5";
                }
            }

            insertDataList.Add(new DBItem("ASS_1_2", form["RB_ASS_1_2"], DBItem.DBDataType.String));
            if (form["RB_ASS_1_2"] == "0")
            {
                var ASS_1_2F1 = form["CK_ASS_1_2F1"];
                var ASS_1_2F1V = new List<string>() { "0", "0", "0", "0", "0", "0", "0", "0" };
                if (ASS_1_2F1 == null)
                    insertDataList.Add(new DBItem("ASS_1_2F1", String.Join("", ASS_1_2F1V), DBItem.DBDataType.String));
                else
                {
                    var ASS_1_2F1V_SP = ASS_1_2F1.Split(',');
                    foreach (var i in ASS_1_2F1V_SP)
                        ASS_1_2F1V[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("ASS_1_2F1", String.Join("", ASS_1_2F1V), DBItem.DBDataType.String));
                }
                if (ASS_1_2F1V[3] == "1")
                    insertDataList.Add(new DBItem("ASS_1_2F2", form["TXT_ASS_1_2F2"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_1_2F2", null, DBItem.DBDataType.String));
                if (ASS_1_2F1V[7] == "1")
                    insertDataList.Add(new DBItem("ASS_1_2F3", form["TXT_ASS_1_2F3"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_1_2F3", null, DBItem.DBDataType.String));
            }
            else
            {
                insertDataList.Add(new DBItem("ASS_1_2F1", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_1_2F2", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_1_2F3", null, DBItem.DBDataType.String));
            }
            #endregion

            #region 2.臉部
            insertDataList.Add(new DBItem("ASS_2", form["RB_ASS_2"], DBItem.DBDataType.String));
            if (form["RB_ASS_2"] == "0")
            {
                var ASS_2F = form["CK_ASS_2F"];
                var ASS_2FV = new List<string>() { "0", "0", "0", "0", "0", "0" };
                if (ASS_2F == null)
                    insertDataList.Add(new DBItem("ASS_2F", String.Join("", ASS_2FV), DBItem.DBDataType.String));
                else
                {
                    var ASS_2F_SP = ASS_2F.Split(',');
                    foreach (var i in ASS_2F_SP)
                        ASS_2FV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("ASS_2F", String.Join("", ASS_2FV), DBItem.DBDataType.String));
                }
                if (ASS_2FV[5] == "1")
                    insertDataList.Add(new DBItem("ASS_2FO", form["TXT_ASS_2FO"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_2FO", null, DBItem.DBDataType.String));
            }
            else
            {
                insertDataList.Add(new DBItem("ASS_2F", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_2FO", null, DBItem.DBDataType.String));
            }

            #endregion

            #region 3.眼睛
            insertDataList.Add(new DBItem("ASS_3", form["RB_ASS_3"], DBItem.DBDataType.String));
            if (form["RB_ASS_3"] == "0")
            {
                var ASS_3F = form["CK_ASS_3F"];
                var ASS_3FV = new List<string>() { "0", "0", "0", "0", "0", "0" };
                if (ASS_3F == null)
                    insertDataList.Add(new DBItem("ASS_3F", String.Join("", ASS_3FV), DBItem.DBDataType.String));
                else
                {
                    var ASS_3F_SP = ASS_3F.Split(',');
                    foreach (var i in ASS_3F_SP)
                        ASS_3FV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("ASS_3F", String.Join("", ASS_3FV), DBItem.DBDataType.String));
                }
                if (ASS_3FV[5] == "1")
                    insertDataList.Add(new DBItem("ASS_3FO", form["TXT_ASS_3FO"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_3FO", null, DBItem.DBDataType.String));
            }
            else
            {
                insertDataList.Add(new DBItem("ASS_3F", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_3FO", null, DBItem.DBDataType.String));
            }

            #endregion

            #region 4.耳朵
            insertDataList.Add(new DBItem("ASS_4", form["RB_ASS_4"], DBItem.DBDataType.String));
            if (form["RB_ASS_4"] == "0")
            {
                var ASS_4F = form["CK_ASS_4F"];
                var ASS_4FV = new List<string>() { "0", "0", "0", "0", "0", "0", "0" };
                if (ASS_4F == null)
                    insertDataList.Add(new DBItem("ASS_4F", String.Join("", ASS_4FV), DBItem.DBDataType.String));
                else
                {
                    var ASS_4F_SP = ASS_4F.Split(',');
                    foreach (var i in ASS_4F_SP)
                        ASS_4FV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("ASS_4F", String.Join("", ASS_4FV), DBItem.DBDataType.String));
                }
                if (ASS_4FV[6] == "1")
                    insertDataList.Add(new DBItem("ASS_4FO", form["TXT_ASS_4FO"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_4FO", null, DBItem.DBDataType.String));
            }
            else
            {
                insertDataList.Add(new DBItem("ASS_4F", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_4FO", null, DBItem.DBDataType.String));
            }
            #endregion

            #region 5.鼻子
            var ASS_5 = form["CK_ASS_5"];
            var ASS_5V = new List<string>() { "0", "0", "0" };
            if (ASS_5 == null)
                insertDataList.Add(new DBItem("ASS_5", String.Join("", ASS_5V), DBItem.DBDataType.String));
            else
            {
                var ASS_5_SP = ASS_5.Split(',');
                foreach (var i in ASS_5_SP)
                    ASS_5V[Convert.ToInt32(i)] = "1";
                insertDataList.Add(new DBItem("ASS_5", String.Join("", ASS_5V), DBItem.DBDataType.String));
            }
            if (ASS_5V[2] == "1")
                insertDataList.Add(new DBItem("ASS_5O", form["TXT_ASS_5O"], DBItem.DBDataType.String));
            else
                insertDataList.Add(new DBItem("ASS_5O", null, DBItem.DBDataType.String));
            #endregion

            #region 6.口腔
            insertDataList.Add(new DBItem("ASS_6", form["RB_ASS_6"], DBItem.DBDataType.String));
            if (form["RB_ASS_6"] == "0")
            {
                var ASS_6F = form["CK_ASS_6F"];
                var ASS_6FV = new List<string>() { "0", "0", "0", "0", "0" };
                if (ASS_6F == null)
                    insertDataList.Add(new DBItem("ASS_6F", String.Join("", ASS_6FV), DBItem.DBDataType.String));
                else
                {
                    var ASS_6F_SP = ASS_6F.Split(',');
                    foreach (var i in ASS_6F_SP)
                        ASS_6FV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("ASS_6F", String.Join("", ASS_6FV), DBItem.DBDataType.String));
                }
                if (ASS_6FV[4] == "1")
                    insertDataList.Add(new DBItem("ASS_6FO", form["TXT_ASS_6FO"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_6FO", null, DBItem.DBDataType.String));
            }
            else
            {
                insertDataList.Add(new DBItem("ASS_6F", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_6FO", null, DBItem.DBDataType.String));
            }
            #endregion

            #region 7.頸部
            insertDataList.Add(new DBItem("ASS_7", form["RB_ASS_7"], DBItem.DBDataType.String));
            if (form["RB_ASS_7"] == "0")
            {
                var ASS_7F = form["CK_ASS_7F"];
                var ASS_7FV = new List<string>() { "0", "0", "0", "0", "0", "0", "0" };
                if (ASS_7F == null)
                    insertDataList.Add(new DBItem("ASS_7F", String.Join("", ASS_7FV), DBItem.DBDataType.String));
                else
                {
                    var ASS_7F_SP = ASS_7F.Split(',');
                    foreach (var i in ASS_7F_SP)
                        ASS_7FV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("ASS_7F", String.Join("", ASS_7FV), DBItem.DBDataType.String));
                }
                if (ASS_7FV[6] == "1")
                    insertDataList.Add(new DBItem("ASS_7FO", form["TXT_ASS_7FO"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_7FO", null, DBItem.DBDataType.String));
            }
            else
            {
                insertDataList.Add(new DBItem("ASS_7F", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_7FO", null, DBItem.DBDataType.String));
            }
            #endregion

            #region 8.胸部

            insertDataList.Add(new DBItem("ASS_8_1", form["RB_ASS_8_1"], DBItem.DBDataType.String));
            if (form["RB_ASS_8_1"] == "0")
            {
                var ASS_8_1F = form["CK_ASS_8_1F"];
                var ASS_8_1FV = new List<string>() { "0", "0" };
                if (ASS_8_1F == null)
                    insertDataList.Add(new DBItem("ASS_8_1F", String.Join("", ASS_8_1FV), DBItem.DBDataType.String));
                else
                {
                    var ASS_8_1F_SP = ASS_8_1F.Split(',');
                    foreach (var i in ASS_8_1F_SP)
                        ASS_8_1FV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("ASS_8_1F", String.Join("", ASS_8_1FV), DBItem.DBDataType.String));
                }
                if (ASS_8_1FV[1] == "1")
                    insertDataList.Add(new DBItem("ASS_8_1FO", form["TXT_ASS_8_1FO"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_8_1FO", null, DBItem.DBDataType.String));
            }
            else
            {
                insertDataList.Add(new DBItem("ASS_8_1F", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_8_1FO", null, DBItem.DBDataType.String));
            }

            insertDataList.Add(new DBItem("ASS_8_2", form["RB_ASS_8_2"], DBItem.DBDataType.String));
            if (form["RB_ASS_8_2"] == "0")
            {
                var ASS_8_2F = form["CK_ASS_8_2F"];
                var ASS_8_2FV = new List<string>() { "0", "0", "0", "0" };
                if (ASS_8_2F == null)
                    insertDataList.Add(new DBItem("ASS_8_2F", String.Join("", ASS_8_2FV), DBItem.DBDataType.String));
                else
                {
                    var ASS_8_2F_SP = ASS_8_2F.Split(',');
                    foreach (var i in ASS_8_2F_SP)
                        ASS_8_2FV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("ASS_8_2F", String.Join("", ASS_8_2FV), DBItem.DBDataType.String));
                }
                if (ASS_8_2FV[0] == "1")
                    insertDataList.Add(new DBItem("ASS_8_2FO", form["TXT_ASS_8_2FO"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_8_2FO", null, DBItem.DBDataType.String));
            }
            else
            {
                insertDataList.Add(new DBItem("ASS_8_2F", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_8_2FO", null, DBItem.DBDataType.String));
            }

            insertDataList.Add(new DBItem("ASS_8_3", form["RB_ASS_8_3"], DBItem.DBDataType.String));
            if (form["RB_ASS_8_3"] == "0")
            {
                var ASS_8_3F = form["CK_ASS_8_3F"];
                var ASS_8_3FV = new List<string>() { "0", "0", "0", "0", "0" };
                if (ASS_8_3F == null)
                    insertDataList.Add(new DBItem("ASS_8_3F", String.Join("", ASS_8_3FV), DBItem.DBDataType.String));
                else
                {
                    var ASS_8_3F_SP = ASS_8_3F.Split(',');
                    foreach (var i in ASS_8_3F_SP)
                        ASS_8_3FV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("ASS_8_3F", String.Join("", ASS_8_3FV), DBItem.DBDataType.String));
                }
            }
            else
                insertDataList.Add(new DBItem("ASS_8_3F", null, DBItem.DBDataType.String));

            insertDataList.Add(new DBItem("ASS_8_4", form["RB_ASS_8_4"], DBItem.DBDataType.String));
            if (form["RB_ASS_8_4"] == "0")
            {
                var ASS_8_4F = form["CK_ASS_8_4F"];
                var ASS_8_4FV = new List<string>() { "0", "0", "0" };
                if (ASS_8_4F == null)
                    insertDataList.Add(new DBItem("ASS_8_4F", String.Join("", ASS_8_4FV), DBItem.DBDataType.String));
                else
                {
                    var ASS_8_4F_SP = ASS_8_4F.Split(',');
                    foreach (var i in ASS_8_4F_SP)
                        ASS_8_4FV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("ASS_8_4F", String.Join("", ASS_8_4FV), DBItem.DBDataType.String));
                }
                if (ASS_8_4FV[2] == "1")
                    insertDataList.Add(new DBItem("ASS_8_4FO", form["TXT_ASS_8_4FO"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_8_4FO", null, DBItem.DBDataType.String));
            }
            else
            {
                insertDataList.Add(new DBItem("ASS_8_4F", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_8_4FO", null, DBItem.DBDataType.String));
            }

            #endregion

            #region 9.腹部
            var ASS_9 = form["CK_ASS_9"];
            var ASS_9V = new List<string>() { "0", "0", "0", "0" };
            if (ASS_9 == null)
                insertDataList.Add(new DBItem("ASS_9", String.Join("", ASS_9V), DBItem.DBDataType.String));
            else
            {
                var ASS_9_SP = ASS_9.Split(',');
                foreach (var i in ASS_9_SP)
                    ASS_9V[Convert.ToInt32(i)] = "1";
                insertDataList.Add(new DBItem("ASS_9", String.Join("", ASS_9V), DBItem.DBDataType.String));
            }

            if (ASS_9V[3] == "1")
                insertDataList.Add(new DBItem("ASS_9O", form["TXT_ASS_9O"], DBItem.DBDataType.String));
            else
                insertDataList.Add(new DBItem("ASS_9O", null, DBItem.DBDataType.String));

            insertDataList.Add(new DBItem("ASS_9_1", form["RB_ASS_9_1"], DBItem.DBDataType.String));
            if (form["RB_ASS_9_1"] == "1")
            {
                var ASS_9_1F = form["CK_ASS_9_1F"];
                var ASS_9_1FV = new List<string>() { "0", "0", "0", "0" };
                if (ASS_9_1F == null)
                    insertDataList.Add(new DBItem("ASS_9_1F", String.Join("", ASS_9_1FV), DBItem.DBDataType.String));
                else
                {
                    var ASS_9_1F_SP = ASS_9_1F.Split(',');
                    foreach (var i in ASS_9_1F_SP)
                        ASS_9_1FV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("ASS_9_1F", String.Join("", ASS_9_1FV), DBItem.DBDataType.String));
                }
                if (ASS_9_1FV[3] == "1")
                    insertDataList.Add(new DBItem("ASS_9_1FO", form["TXT_ASS_9_1FO"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_9_1FO", null, DBItem.DBDataType.String));
            }
            else
            {
                insertDataList.Add(new DBItem("ASS_9_1F", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_9_1FO", null, DBItem.DBDataType.String));
            }
            #endregion

            #region 10.臍帶
            var ASS_10 = form["CK_ASS_10"];
            var ASS_10V = new List<string>() { "0", "0", "0", "0", "0", "0", "0" };
            if (ASS_10 == null)
                insertDataList.Add(new DBItem("ASS_10", String.Join("", ASS_10V), DBItem.DBDataType.String));
            else
            {
                var ASS_10_SP = ASS_10.Split(',');
                foreach (var i in ASS_10_SP)
                    ASS_10V[Convert.ToInt32(i)] = "1";
                insertDataList.Add(new DBItem("ASS_10", String.Join("", ASS_10V), DBItem.DBDataType.String));
            }

            if (ASS_10V[6] == "1")
                insertDataList.Add(new DBItem("ASS_10O", form["TXT_ASS_10O"], DBItem.DBDataType.String));
            else
                insertDataList.Add(new DBItem("ASS_10O", null, DBItem.DBDataType.String));
            #endregion

            #region 11.臀背部
            insertDataList.Add(new DBItem("ASS_11", form["RB_ASS_11"], DBItem.DBDataType.String));
            if (form["RB_ASS_11"] == "0")
            {
                var ASS_11F = form["CK_ASS_11F"];
                var ASS_11FV = new List<string>() { "0", "0", "0", "0" };
                if (ASS_11F == null)
                    insertDataList.Add(new DBItem("ASS_11F", String.Join("", ASS_11FV), DBItem.DBDataType.String));
                else
                {
                    var ASS_11F_SP = ASS_11F.Split(',');
                    foreach (var i in ASS_11F_SP)
                        ASS_11FV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("ASS_11F", String.Join("", ASS_11FV), DBItem.DBDataType.String));
                }
                if (ASS_11FV[3] == "1")
                    insertDataList.Add(new DBItem("ASS_11FO", form["TXT_ASS_11FO"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_11FO", null, DBItem.DBDataType.String));
            }
            else
            {
                insertDataList.Add(new DBItem("ASS_11F", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_11FO", null, DBItem.DBDataType.String));
            }

            #endregion

            #region 12.泌尿生殖器外觀
            if (ptinfo.PatientGender == "男")
                insertDataList.Add(new DBItem("ASS_12", "0", DBItem.DBDataType.String));
            else if (ptinfo.PatientGender == "女")
                insertDataList.Add(new DBItem("ASS_12", "1", DBItem.DBDataType.String));

            if (ptinfo.PatientGender == "男")
            {//男
                insertDataList.Add(new DBItem("ASS_12_1", form["RB_ASS_12_1"], DBItem.DBDataType.String));
                if (form["RB_ASS_12_1"] == "0")
                {
                    var ASS_12_1F = form["CK_ASS_12_1F"];
                    var ASS_12_1FV = new List<string>() { "0", "0" };
                    if (ASS_12_1F == null)
                        insertDataList.Add(new DBItem("ASS_12_1F", String.Join("", ASS_12_1FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_12_1F_SP = ASS_12_1F.Split(',');
                        foreach (var i in ASS_12_1F_SP)
                            ASS_12_1FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_12_1F", String.Join("", ASS_12_1FV), DBItem.DBDataType.String));
                    }
                }
                else
                    insertDataList.Add(new DBItem("ASS_12_1F", null, DBItem.DBDataType.String));

                insertDataList.Add(new DBItem("ASS_12_2", form["RB_ASS_12_2"], DBItem.DBDataType.String));
                if (form["RB_ASS_12_2"] == "0")
                {
                    var ASS_12_2F = form["CK_ASS_12_2F"];
                    var ASS_12_2FV = new List<string>() { "0", "0", "0" };
                    if (ASS_12_2F == null)
                        insertDataList.Add(new DBItem("ASS_12_2F", String.Join("", ASS_12_2FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_12_2F_SP = ASS_12_2F.Split(',');
                        foreach (var i in ASS_12_2F_SP)
                            ASS_12_2FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_12_2F", String.Join("", ASS_12_2FV), DBItem.DBDataType.String));
                    }

                    if (ASS_12_2FV[2] == "1")
                        insertDataList.Add(new DBItem("ASS_12_2FO", form["TXT_ASS_12_2FO"], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_12_2FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_12_2F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_12_2FO", null, DBItem.DBDataType.String));
                }

                insertDataList.Add(new DBItem("ASS_12_3", form["RB_ASS_12_3"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_12_4", form["TXT_ASS_12_4"], DBItem.DBDataType.String));

                insertDataList.Add(new DBItem("ASS_12_5", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_12_6", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_12_7", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_12_7F", null, DBItem.DBDataType.String));
            }
            else if (ptinfo.PatientGender == "女")
            {//女

                insertDataList.Add(new DBItem("ASS_12_5", form["RB_ASS_12_5"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_12_6", form["RB_ASS_12_6"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_12_7", form["RB_ASS_12_7"], DBItem.DBDataType.String));
                if (form["RB_ASS_12_7"] == "1")
                {
                    var ASS_12_7F = form["CK_ASS_12_7F"];
                    var ASS_12_7FV = new List<string>() { "0", "0", "0" };
                    if (ASS_12_7F == null)
                        insertDataList.Add(new DBItem("ASS_12_7F", String.Join("", ASS_12_7FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_12_7F_SP = ASS_12_7F.Split(',');
                        foreach (var i in ASS_12_7F_SP)
                            ASS_12_7FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_12_7F", String.Join("", ASS_12_7FV), DBItem.DBDataType.String));
                    }
                }
                else
                    insertDataList.Add(new DBItem("ASS_12_7F", null, DBItem.DBDataType.String));

                insertDataList.Add(new DBItem("ASS_12_1", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_12_1F", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_12_2", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_12_2F", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_12_2FO", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_12_3", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_12_4", null, DBItem.DBDataType.String));
            }

            #region 肛門
            insertDataList.Add(new DBItem("ASS_12_8", form["RB_ASS_12_8"], DBItem.DBDataType.String));
            if (form["RB_ASS_12_8"] == "0")
            {
                var ASS_12_8F = form["CK_ASS_12_8F"];
                var ASS_12_8FV = new List<string>() { "0", "0", "0", "0", "0" };
                if (ASS_12_8F == null)
                    insertDataList.Add(new DBItem("ASS_12_8F", String.Join("", ASS_12_8FV), DBItem.DBDataType.String));
                else
                {
                    var ASS_12_8F_SP = ASS_12_8F.Split(',');
                    foreach (var i in ASS_12_8F_SP)
                        ASS_12_8FV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("ASS_12_8F", String.Join("", ASS_12_8FV), DBItem.DBDataType.String));
                }

                if (ASS_12_8FV[4] == "1")
                    insertDataList.Add(new DBItem("ASS_12_8FO", form["TXT_ASS_12_8FO"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_12_8FO", null, DBItem.DBDataType.String));
            }
            else
            {
                insertDataList.Add(new DBItem("ASS_12_8F", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_12_8FO", null, DBItem.DBDataType.String));
            }
            #endregion
            #endregion

            #region 13.骨骼
            insertDataList.Add(new DBItem("ASS_13_1", form["RB_ASS_13_1"], DBItem.DBDataType.String));
            if (form["RB_ASS_13_1"] == "1")
            {
                var ASS_13_1F = form["CK_ASS_13_1F"];
                var ASS_13_1FV = new List<string>() { "0", "0", "0" };
                if (ASS_13_1F == null)
                    insertDataList.Add(new DBItem("ASS_13_1F", String.Join("", ASS_13_1FV), DBItem.DBDataType.String));
                else
                {
                    var ASS_13_1F_SP = ASS_13_1F.Split(',');
                    foreach (var i in ASS_13_1F_SP)
                        ASS_13_1FV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("ASS_13_1F", String.Join("", ASS_13_1FV), DBItem.DBDataType.String));
                }
                if (ASS_13_1FV[2] == "1")
                    insertDataList.Add(new DBItem("ASS_13_1FO", form["TXT_ASS_13_1FO"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_13_1FO", null, DBItem.DBDataType.String));
            }
            else
            {
                insertDataList.Add(new DBItem("ASS_13_1F", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_13_1FO", null, DBItem.DBDataType.String));
            }
            #endregion

            #region 14.哭聲
            insertDataList.Add(new DBItem("ASS_14", form["RB_ASS_14"], DBItem.DBDataType.String));
            #endregion

            #region 15.活動力
            var ASS_15 = form["CK_ASS_15"];
            var ASS_15V = new List<string>() { "0", "0", "0", "0", "0", "0" };
            if (ASS_15 == null)
                insertDataList.Add(new DBItem("ASS_15", String.Join("", ASS_15V), DBItem.DBDataType.String));
            else
            {
                var ASS_15_SP = ASS_15.Split(',');
                foreach (var i in ASS_15_SP)
                    ASS_15V[Convert.ToInt32(i)] = "1";
                insertDataList.Add(new DBItem("ASS_15", String.Join("", ASS_15V), DBItem.DBDataType.String));
            }
            if (ASS_15V[5] == "1")
                insertDataList.Add(new DBItem("ASS_15O", form["TXT_ASS_15O"], DBItem.DBDataType.String));
            else
                insertDataList.Add(new DBItem("ASS_15O", null, DBItem.DBDataType.String));
            #endregion

            #region 16.皮膚
            var ASS_16_1 = form["CK_ASS_16_1"];
            var ASS_16_1V = new List<string>() { "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0" };
            if (ASS_16_1 == null)
                insertDataList.Add(new DBItem("ASS_16_1", String.Join("", ASS_16_1V), DBItem.DBDataType.String));
            else
            {
                var ASS_16_1SP = ASS_16_1.Split(',');
                foreach (var i in ASS_16_1SP)
                    ASS_16_1V[Convert.ToInt32(i)] = "1";
                insertDataList.Add(new DBItem("ASS_16_1", String.Join("", ASS_16_1V), DBItem.DBDataType.String));
            }
            if (ASS_16_1V[8] == "1")
                insertDataList.Add(new DBItem("ASS_16_11", form["TXT_ASS_16_11"], DBItem.DBDataType.String));
            else
                insertDataList.Add(new DBItem("ASS_16_11", null, DBItem.DBDataType.String));

            if (ASS_16_1V[9] == "1")
                insertDataList.Add(new DBItem("ASS_16_12", form["TXT_ASS_16_12"], DBItem.DBDataType.String));
            else
                insertDataList.Add(new DBItem("ASS_16_12", null, DBItem.DBDataType.String));

            if (ASS_16_1V[10] == "1")
                insertDataList.Add(new DBItem("ASS_16O", form["TXT_ASS_16O"], DBItem.DBDataType.String));
            else
                insertDataList.Add(new DBItem("ASS_16O", null, DBItem.DBDataType.String));

            #region 血管瘤
            insertDataList.Add(new DBItem("ASS_16_2", form["RB_ASS_16_2"], DBItem.DBDataType.String));
            if (form["RB_ASS_16_2"] == "1")
            {
                insertDataList.Add(new DBItem("ASS_16_2P", form["TXT_ASS_16_2P"], DBItem.DBDataType.String));
                //var ASS_16_2F = form["CK_ASS_16_2F"];
                //var ASS_16_2FV = new List<string>() { "0", "0", "0" };
                //if (ASS_16_2F == null)
                //    insertDataList.Add(new DBItem("ASS_16_2F", String.Join("", ASS_16_2FV), DBItem.DBDataType.String));
                //else
                //{
                //    var ASS_16_2F_SP = ASS_16_2F.Split(',');
                //    foreach (var i in ASS_16_2F_SP)
                //        ASS_16_2FV[Convert.ToInt32(i)] = "1";
                //    insertDataList.Add(new DBItem("ASS_16_2F", String.Join("", ASS_16_2FV), DBItem.DBDataType.String));
                //}

                //if (ASS_16_2FV[0] == "1")
                //    insertDataList.Add(new DBItem("ASS_16_2F1", form["TXT_ASS_16_2F1"], DBItem.DBDataType.String));
                //else
                //    insertDataList.Add(new DBItem("ASS_16_2F1", null, DBItem.DBDataType.String));

                //if (ASS_16_2FV[1] == "1")
                //    insertDataList.Add(new DBItem("ASS_16_2F2", form["TXT_ASS_16_2F2"], DBItem.DBDataType.String));
                //else
                //    insertDataList.Add(new DBItem("ASS_16_2F2", null, DBItem.DBDataType.String));

                //if (ASS_16_2FV[2] == "1")
                //    insertDataList.Add(new DBItem("ASS_16_2F3", form["TXT_ASS_16_2F3"], DBItem.DBDataType.String));
                //else
                //    insertDataList.Add(new DBItem("ASS_16_2F3", null, DBItem.DBDataType.String));
            }
            else
            {
                insertDataList.Add(new DBItem("ASS_16_2P", null, DBItem.DBDataType.String));
                //insertDataList.Add(new DBItem("ASS_16_2F", null, DBItem.DBDataType.String));
                //insertDataList.Add(new DBItem("ASS_16_2F1", null, DBItem.DBDataType.String));
                //insertDataList.Add(new DBItem("ASS_16_2F2", null, DBItem.DBDataType.String));
                //insertDataList.Add(new DBItem("ASS_16_2F3", null, DBItem.DBDataType.String));
            }
            #endregion

            #region 胎記
            insertDataList.Add(new DBItem("ASS_16_3", form["RB_ASS_16_3"], DBItem.DBDataType.String));
            if (form["RB_ASS_16_3"] == "1")
            {
                var ASS_16_3F = form["CK_ASS_16_3F"];
                var ASS_16_3FV = new List<string>() { "0", "0", "0", "0" };
                if (ASS_16_3F == null)
                    insertDataList.Add(new DBItem("ASS_16_3F", String.Join("", ASS_16_3FV), DBItem.DBDataType.String));
                else
                {
                    var ASS_16_3F_SP = ASS_16_3F.Split(',');
                    foreach (var i in ASS_16_3F_SP)
                        ASS_16_3FV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("ASS_16_3F", String.Join("", ASS_16_3FV), DBItem.DBDataType.String));
                }

                if (ASS_16_3FV[0] == "1")
                    insertDataList.Add(new DBItem("ASS_16_3F1", form["TXT_ASS_16_3F1"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_16_3F1", null, DBItem.DBDataType.String));

                if (ASS_16_3FV[1] == "1")
                    insertDataList.Add(new DBItem("ASS_16_3F2", form["TXT_ASS_16_3F2"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_16_3F2", null, DBItem.DBDataType.String));

                if (ASS_16_3FV[2] == "1")
                    insertDataList.Add(new DBItem("ASS_16_3F3", form["TXT_ASS_16_3F3"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_16_3F3", null, DBItem.DBDataType.String));

                if (ASS_16_3FV[3] == "1")
                    insertDataList.Add(new DBItem("ASS_16_3FO", form["TXT_ASS_16_3FO"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_16_3FO", null, DBItem.DBDataType.String));
            }
            else
            {
                insertDataList.Add(new DBItem("ASS_16_3F", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_16_3F1", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_16_3F2", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_16_3F3", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_16_3FO", null, DBItem.DBDataType.String));
            }

            #endregion
            #endregion

            #region 17.四肢
            insertDataList.Add(new DBItem("ASS_17", form["RB_ASS_17"], DBItem.DBDataType.String));
            if (form["RB_ASS_17"] == "0")
            {
                #region 四肢 - 足內翻
                insertDataList.Add(new DBItem("ASS_17_1", form["RB_ASS_17_1"], DBItem.DBDataType.String));
                if (form["RB_ASS_17_1"] == "1")
                {
                    var ASS_17_1F = form["CK_ASS_17_1F"];
                    var ASS_17_1FV = new List<string>() { "0", "0" };
                    if (ASS_17_1F == null)
                        insertDataList.Add(new DBItem("ASS_17_1F", String.Join("", ASS_17_1FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_17_1F_SP = ASS_17_1F.Split(',');
                        foreach (var i in ASS_17_1F_SP)
                            ASS_17_1FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_17_1F", String.Join("", ASS_17_1FV), DBItem.DBDataType.String));
                    }
                }
                else
                    insertDataList.Add(new DBItem("ASS_17_1F", null, DBItem.DBDataType.String));
                #endregion

                #region 四肢 - 足外翻
                insertDataList.Add(new DBItem("ASS_17_2", form["RB_ASS_17_2"], DBItem.DBDataType.String));
                if (form["RB_ASS_17_2"] == "1")
                {
                    var ASS_17_2F = form["CK_ASS_17_2F"];
                    var ASS_17_2FV = new List<string>() { "0", "0" };
                    if (ASS_17_2F == null)
                        insertDataList.Add(new DBItem("ASS_17_2F", String.Join("", ASS_17_2FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_17_2F_SP = ASS_17_2F.Split(',');
                        foreach (var i in ASS_17_2F_SP)
                            ASS_17_2FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_17_2F", String.Join("", ASS_17_2FV), DBItem.DBDataType.String));
                    }
                }
                else
                    insertDataList.Add(new DBItem("ASS_17_2F", null, DBItem.DBDataType.String));
                #endregion

                #region 四肢 - 單一手紋(斷掌)
                insertDataList.Add(new DBItem("ASS_17_3", form["RB_ASS_17_3"], DBItem.DBDataType.String));
                if (form["RB_ASS_17_3"] == "1")
                {
                    var ASS_17_3F = form["CK_ASS_17_3F"];
                    var ASS_17_3FV = new List<string>() { "0", "0" };
                    if (ASS_17_3F == null)
                        insertDataList.Add(new DBItem("ASS_17_3F", String.Join("", ASS_17_3FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_17_3F_SP = ASS_17_3F.Split(',');
                        foreach (var i in ASS_17_3F_SP)
                            ASS_17_3FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_17_3F", String.Join("", ASS_17_3FV), DBItem.DBDataType.String));
                    }
                }
                else
                    insertDataList.Add(new DBItem("ASS_17_3F", null, DBItem.DBDataType.String));
                #endregion
            }
            else
            {
                insertDataList.Add(new DBItem("ASS_17_1", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_17_1F", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_17_2", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_17_2F", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_17_3", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_17_3F", null, DBItem.DBDataType.String));
            }

            #region 四肢 - 活動力
            insertDataList.Add(new DBItem("ASS_17_4", form["RB_ASS_17_4"], DBItem.DBDataType.String));
            if (form["RB_ASS_17_4"] == "0")
            {
                var ASS_17_4F = form["CK_ASS_17_4F"];
                var ASS_17_4FV = new List<string>() { "0", "0", "0", "0" };
                if (ASS_17_4F == null)
                    insertDataList.Add(new DBItem("ASS_17_4F", String.Join("", ASS_17_4FV), DBItem.DBDataType.String));
                else
                {
                    var ASS_17_4F_SP = ASS_17_4F.Split(',');
                    foreach (var i in ASS_17_4F_SP)
                        ASS_17_4FV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("ASS_17_4F", String.Join("", ASS_17_4FV), DBItem.DBDataType.String));
                }
            }
            else
                insertDataList.Add(new DBItem("ASS_17_4F", null, DBItem.DBDataType.String));
            #endregion

            #region 四肢 - 水腫
            insertDataList.Add(new DBItem("ASS_17_5", form["RB_ASS_17_5"], DBItem.DBDataType.String));
            if (form["RB_ASS_17_5"] == "1")
            {
                var ASS_17_5F = form["CK_ASS_17_5F"];
                var ASS_17_5FV = new List<string>() { "0", "0", "0", "0" };
                if (ASS_17_5F == null)
                    insertDataList.Add(new DBItem("ASS_17_5F", String.Join("", ASS_17_5FV), DBItem.DBDataType.String));
                else
                {
                    var ASS_17_5F_SP = ASS_17_5F.Split(',');
                    foreach (var i in ASS_17_5F_SP)
                        ASS_17_5FV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("ASS_17_5F", String.Join("", ASS_17_5FV), DBItem.DBDataType.String));
                }
            }
            else
                insertDataList.Add(new DBItem("ASS_17_5F", null, DBItem.DBDataType.String));
            #endregion

            #region 四肢 - 麻痺
            insertDataList.Add(new DBItem("ASS_17_6", form["RB_ASS_17_6"], DBItem.DBDataType.String));
            if (form["RB_ASS_17_6"] == "1")
            {
                var ASS_17_6F = form["CK_ASS_17_6F"];
                var ASS_17_6FV = new List<string>() { "0", "0", "0", "0" };
                if (ASS_17_6F == null)
                    insertDataList.Add(new DBItem("ASS_17_6F", String.Join("", ASS_17_6FV), DBItem.DBDataType.String));
                else
                {
                    var ASS_17_6F_SP = ASS_17_6F.Split(',');
                    foreach (var i in ASS_17_6F_SP)
                        ASS_17_6FV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("ASS_17_6F", String.Join("", ASS_17_6FV), DBItem.DBDataType.String));
                }
            }
            else
                insertDataList.Add(new DBItem("ASS_17_6F", null, DBItem.DBDataType.String));
            #endregion

            #region 四肢 - 手指
            insertDataList.Add(new DBItem("ASS_17_7", form["RB_ASS_17_7"], DBItem.DBDataType.String));
            if (form["RB_ASS_17_7"] == "0")
            {
                var ASS_17_7F = form["CK_ASS_17_7F"];
                var ASS_17_7FV = new List<string>() { "0", "0", "0", "0", "0", "0", "0", "0" };
                if (ASS_17_7F == null)
                    insertDataList.Add(new DBItem("ASS_17_7F", String.Join("", ASS_17_7FV), DBItem.DBDataType.String));
                else
                {
                    var ASS_17_7F_SP = ASS_17_7F.Split(',');
                    foreach (var i in ASS_17_7F_SP)
                        ASS_17_7FV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("ASS_17_7F", String.Join("", ASS_17_7FV), DBItem.DBDataType.String));
                }
                if (ASS_17_7FV[0] == "1")
                    insertDataList.Add(new DBItem("ASS_17_7F1", form["TXT_ASS_17_7F1"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_17_7F1", null, DBItem.DBDataType.String));

                if (ASS_17_7FV[1] == "1")
                    insertDataList.Add(new DBItem("ASS_17_7F2", form["TXT_ASS_17_7F2"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_17_7F2", null, DBItem.DBDataType.String));

                if (ASS_17_7FV[2] == "1")
                    insertDataList.Add(new DBItem("ASS_17_7F3", form["TXT_ASS_17_7F3"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_17_7F3", null, DBItem.DBDataType.String));

                if (ASS_17_7FV[3] == "1")
                    insertDataList.Add(new DBItem("ASS_17_7F4", form["TXT_ASS_17_7F4"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_17_7F4", null, DBItem.DBDataType.String));

                if (ASS_17_7FV[4] == "1")
                    insertDataList.Add(new DBItem("ASS_17_7F5", form["TXT_ASS_17_7F5"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_17_7F5", null, DBItem.DBDataType.String));

                if (ASS_17_7FV[5] == "1")
                    insertDataList.Add(new DBItem("ASS_17_7F6", form["TXT_ASS_17_7F6"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_17_7F6", null, DBItem.DBDataType.String));

                if (ASS_17_7FV[6] == "1")
                    insertDataList.Add(new DBItem("ASS_17_7F7", form["TXT_ASS_17_7F7"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_17_7F7", null, DBItem.DBDataType.String));

                if (ASS_17_7FV[7] == "1")
                    insertDataList.Add(new DBItem("ASS_17_7F8", form["TXT_ASS_17_7F8"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_17_7F8", null, DBItem.DBDataType.String));
            }
            else
            {
                insertDataList.Add(new DBItem("ASS_17_7F1", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_17_7F2", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_17_7F3", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_17_7F4", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_17_7F5", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_17_7F6", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_17_7F7", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_17_7F8", null, DBItem.DBDataType.String));
            }
            #endregion

            #region 四肢 - 腳趾
            insertDataList.Add(new DBItem("ASS_17_8", form["RB_ASS_17_8"], DBItem.DBDataType.String));
            if (form["RB_ASS_17_8"] == "0")
            {
                var ASS_17_8F = form["CK_ASS_17_8F"];
                var ASS_17_8FV = new List<string>() { "0", "0", "0", "0", "0", "0" };
                if (ASS_17_8F == null)
                    insertDataList.Add(new DBItem("ASS_17_8F", String.Join("", ASS_17_8FV), DBItem.DBDataType.String));
                else
                {
                    var ASS_17_8F_SP = ASS_17_8F.Split(',');
                    foreach (var i in ASS_17_8F_SP)
                        ASS_17_8FV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("ASS_17_8F", String.Join("", ASS_17_8FV), DBItem.DBDataType.String));
                }
                if (ASS_17_8FV[0] == "1")
                    insertDataList.Add(new DBItem("ASS_17_8F1", form["TXT_ASS_17_8F1"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_17_8F1", null, DBItem.DBDataType.String));

                if (ASS_17_8FV[1] == "1")
                    insertDataList.Add(new DBItem("ASS_17_8F2", form["TXT_ASS_17_8F2"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_17_8F2", null, DBItem.DBDataType.String));

                if (ASS_17_8FV[2] == "1")
                    insertDataList.Add(new DBItem("ASS_17_8F3", form["TXT_ASS_17_8F3"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_17_8F3", null, DBItem.DBDataType.String));

                if (ASS_17_8FV[3] == "1")
                    insertDataList.Add(new DBItem("ASS_17_8F4", form["TXT_ASS_17_8F4"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_17_8F4", null, DBItem.DBDataType.String));

                if (ASS_17_8FV[4] == "1")
                    insertDataList.Add(new DBItem("ASS_17_8F5", form["TXT_ASS_17_8F5"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_17_8F5", null, DBItem.DBDataType.String));

                if (ASS_17_8FV[5] == "1")
                    insertDataList.Add(new DBItem("ASS_17_8F6", form["TXT_ASS_17_8F6"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_17_8F6", null, DBItem.DBDataType.String));
            }
            else
            {
                insertDataList.Add(new DBItem("ASS_17_8F1", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_17_8F2", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_17_8F3", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_17_8F4", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_17_8F5", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_17_8F6", null, DBItem.DBDataType.String));
            }
            #endregion

            insertDataList.Add(new DBItem("ASS_17_9", form["RB_ASS_17_9"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("ASS_17_10", form["RB_ASS_17_10"], DBItem.DBDataType.String));
            #endregion

            #region 18.反射
            insertDataList.Add(new DBItem("ASS_18_1", form["RB_ASS_18_1"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("ASS_18_2", form["RB_ASS_18_2"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("ASS_18_3", form["RB_ASS_18_3"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("ASS_18_4", form["RB_ASS_18_4"], DBItem.DBDataType.String));

            #endregion
            #endregion

            #region 進食情形
            insertDataList.Add(new DBItem("FEED", form["RB_FEED"], DBItem.DBDataType.String));
            var FEED_K = form["CK_FEED_K"];
            var FEED_KV = new List<string>() { "0", "0" };
            if (FEED_K == null)
                insertDataList.Add(new DBItem("FEED_K", String.Join("", FEED_KV), DBItem.DBDataType.String));
            else
            {
                var FEED_K_SP = FEED_K.Split(',');
                foreach (var i in FEED_K_SP)
                    FEED_KV[Convert.ToInt32(i)] = "1";
                insertDataList.Add(new DBItem("FEED_K", String.Join("", FEED_KV), DBItem.DBDataType.String));
            }

            if (FEED_KV[1] == "1")
            {
                insertDataList.Add(new DBItem("FEED_MB", form["TXT_FEED_MB"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FEED_MF", form["TXT_FEED_MF"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FEED_MA", form["TXT_FEED_MA"], DBItem.DBDataType.String));
            }
            else
            {
                insertDataList.Add(new DBItem("FEED_MB", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FEED_MF", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FEED_MA", null, DBItem.DBDataType.String));
            }

            var VOM = form["RB_VOM"];
            insertDataList.Add(new DBItem("VOM", VOM, DBItem.DBDataType.String));
            if (VOM == "1")
            {
                insertDataList.Add(new DBItem("VOM_AMT", form["TXT_VOM_AMT"], DBItem.DBDataType.String));

                var VOM_K = form["CK_VOM_K"];
                var VOM_KV = new List<string>() { "0", "0", "0", "0", "0" };
                if (VOM_K == null)
                    insertDataList.Add(new DBItem("VOM_K", String.Join("", VOM_KV), DBItem.DBDataType.String));
                else
                {
                    var VOM_K_SP = VOM_K.Split(',');
                    foreach (var i in VOM_K_SP)
                        VOM_KV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("VOM_K", String.Join("", VOM_KV), DBItem.DBDataType.String));
                }
            }
            else
            {
                insertDataList.Add(new DBItem("VOM_K", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("VOM_AMT", null, DBItem.DBDataType.String));
            }

            #endregion

            #region 排泄情形
            insertDataList.Add(new DBItem("URI_AMT", form["RB_URI_AMT"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("URI_COL", form["RB_URI_COL"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("DEF_COL", form["RB_DEF_COL"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("DEL_CHA", form["RB_DEL_CHA"], DBItem.DBDataType.String));
            #endregion

            #region 疼痛評估
            var PAIN_AS = form["RB_PAIN_AS"];
            insertDataList.Add(new DBItem("PAIN_AS", PAIN_AS, DBItem.DBDataType.String));

            //數字量表
            if (PAIN_AS == "0")
                insertDataList.Add(new DBItem("PAIN_AS1", form["RB_PAIN_AS1"], DBItem.DBDataType.String));
            else
                insertDataList.Add(new DBItem("PAIN_AS1", null, DBItem.DBDataType.String));

            //臉譜量表
            if (PAIN_AS == "1")
                insertDataList.Add(new DBItem("PAIN_AS2", form["RB_PAIN_AS2"], DBItem.DBDataType.String));
            else
                insertDataList.Add(new DBItem("PAIN_AS2", null, DBItem.DBDataType.String));

            //困難評估(新生兒)
            if (PAIN_AS == "4")
            {
                insertDataList.Add(new DBItem("PAIN_AS3_1", form["RB_PAIN_AS3_1"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PAIN_AS3_2", form["RB_PAIN_AS3_2"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PAIN_AS3_3", form["RB_PAIN_AS3_3"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PAIN_AS3_4", form["RB_PAIN_AS3_4"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PAIN_AS3_5", form["RB_PAIN_AS3_5"], DBItem.DBDataType.String));
            }
            else
            {
                insertDataList.Add(new DBItem("PAIN_AS3_1", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PAIN_AS3_2", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PAIN_AS3_3", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PAIN_AS3_4", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PAIN_AS3_5", null, DBItem.DBDataType.String));
            }

            //CPOT評估(加護單位)
            if (PAIN_AS == "5")
            {
                insertDataList.Add(new DBItem("PAIN_AS4_1", form["RB_PAIN_AS4_1"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PAIN_AS4_2", form["RB_PAIN_AS4_2"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PAIN_AS4_3", form["RB_PAIN_AS4_3"], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PAIN_AS4_4", form["RB_PAIN_AS4_4"], DBItem.DBDataType.String));
            }
            else
            {
                insertDataList.Add(new DBItem("PAIN_AS4_1", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PAIN_AS4_2", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PAIN_AS4_3", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PAIN_AS4_4", null, DBItem.DBDataType.String));
            }
            #endregion

            #region 接觸史

            var INF_MOM = form["RB_INF_MOM"];
            insertDataList.Add(new DBItem("INF_MOM", INF_MOM, DBItem.DBDataType.String));
            if (INF_MOM == "1")
            {
                var INF_MOM_SYM = form["CK_INF_MOM_SYM"];
                var INF_MOM_SYMV = new List<string>() { "0", "0", "0", "0", "0", "0" };
                if (INF_MOM_SYM == null)
                    insertDataList.Add(new DBItem("INF_MOM_SYM", String.Join("", INF_MOM_SYMV), DBItem.DBDataType.String));
                else
                {
                    var INF_MOM_SYM_SP = INF_MOM_SYM.Split(',');
                    foreach (var i in INF_MOM_SYM_SP)
                        INF_MOM_SYMV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("INF_MOM_SYM", String.Join("", INF_MOM_SYMV), DBItem.DBDataType.String));
                }

                //其他
                if (INF_MOM_SYMV[5] == "1")
                    insertDataList.Add(new DBItem("INF_MOM_OTH", form["TXT_INF_MOM_OTH"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("INF_MOM_OTH", null, DBItem.DBDataType.String));
            }
            else
            {
                insertDataList.Add(new DBItem("INF_MOM_SYM", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("INF_MOM_OTH", null, DBItem.DBDataType.String));
            }

            var INF_OTH = form["RB_INF_OTH"];
            insertDataList.Add(new DBItem("INF_OTH", INF_OTH, DBItem.DBDataType.String));
            if (INF_OTH == "1")
            {
                insertDataList.Add(new DBItem("INF_OTH_WHO", form["TXT_INF_OTH_WHO"], DBItem.DBDataType.String));

                var INF_OTH_SYM = form["CK_INF_OTH_SYM"];
                var INF_OTH_SYMV = new List<string>() { "0", "0", "0", "0", "0", "0" };
                if (INF_OTH_SYM == null)
                    insertDataList.Add(new DBItem("INF_OTH_SYM", String.Join("", INF_OTH_SYMV), DBItem.DBDataType.String));
                else
                {
                    var INF_OTH_SYM_SP = INF_OTH_SYM.Split(',');
                    foreach (var i in INF_OTH_SYM_SP)
                        INF_OTH_SYMV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("INF_OTH_SYM", String.Join("", INF_OTH_SYMV), DBItem.DBDataType.String));
                }

                //其他
                if (INF_OTH_SYMV[5] == "1")
                    insertDataList.Add(new DBItem("INF_OTH_OTH", form["TXT_INF_OTH_OTH"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("INF_OTH_OTH", null, DBItem.DBDataType.String));
            }
            else
            {
                insertDataList.Add(new DBItem("INF_OTH_SYM", null, DBItem.DBDataType.String));
                //insertDataList.Add(new DBItem("INF_OTH_WHO", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("INF_OTH_OTH", null, DBItem.DBDataType.String));
            }

            var BS_CLS = form["RB_BS_CLS"];
            insertDataList.Add(new DBItem("BS_CLS", BS_CLS, DBItem.DBDataType.String));
            if (BS_CLS == "1")
            {
                var BS_CLS_RS = form["CK_BS_CLS_RS"];
                var BS_CLS_RSV = new List<string>() { "0", "0", "0", "0" };
                if (BS_CLS_RS == null)
                    insertDataList.Add(new DBItem("BS_CLS_RS", String.Join("", BS_CLS_RSV), DBItem.DBDataType.String));
                else
                {
                    var BS_CLS_RS_SP = BS_CLS_RS.Split(',');
                    foreach (var i in BS_CLS_RS_SP)
                        BS_CLS_RSV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("BS_CLS_RS", String.Join("", BS_CLS_RSV), DBItem.DBDataType.String));
                }

                if (BS_CLS_RSV[3] == "1")
                    insertDataList.Add(new DBItem("BS_CLS_OTH", form["TXT_BS_CLS_OTH"], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("BS_CLS_OTH", null, DBItem.DBDataType.String));
            }
            else
            {
                insertDataList.Add(new DBItem("BS_CLS_RS", null, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("BS_CLS_OTH", null, DBItem.DBDataType.String));
            }

            insertDataList.Add(new DBItem("INF_CARE", form["RB_INF_CARE"], DBItem.DBDataType.String));
            #endregion

            #region TOCC評估
            //症狀
            var SYM = form["CK_SYM"];
            var SYMV = new List<string>() { "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0" };
            if (SYM == null)
                insertDataList.Add(new DBItem("SYM", String.Join("", SYMV), DBItem.DBDataType.String));
            else
            {
                var SYM_SP = SYM.Split(',');
                foreach (var i in SYM_SP)
                    SYMV[Convert.ToInt32(i)] = "1";
                insertDataList.Add(new DBItem("SYM", String.Join("", SYMV), DBItem.DBDataType.String));
            }
            if (SYMV[10] == "1")//(其他TXT)
                insertDataList.Add(new DBItem("SYM_OTH", form["TXT_SYM_OTH"], DBItem.DBDataType.String));
            else
                insertDataList.Add(new DBItem("SYM_OTH", null, DBItem.DBDataType.String));

            //旅遊史
            var TRAV = form["CK_TRAV"];
            var TRAVV = new List<string>() { "0", "0", "0" };
            if (TRAV == null)
                insertDataList.Add(new DBItem("TRAV", String.Join("", TRAVV), DBItem.DBDataType.String));
            else
            {
                var TRAV_SP = TRAV.Split(',');
                foreach (var i in TRAV_SP)
                    TRAVV[Convert.ToInt32(i)] = "1";
                insertDataList.Add(new DBItem("TRAV", String.Join("", TRAVV), DBItem.DBDataType.String));
            }
            //國內旅遊城市
            insertDataList.Add(new DBItem("TRAV_DOME_CITY", form["TXT_TRAV_DOME_CITY"], DBItem.DBDataType.String));
            //國內旅遊景點
            insertDataList.Add(new DBItem("TRAV_DOME_VIEW", form["TXT_TRAV_DOME_VIEW"], DBItem.DBDataType.String));
            //國內旅遊交通
            var TRAV_DOME_TRAF = form["CK_TRAV_DOME_TRAF"];
            var TRAV_DOME_TRAFV = new List<string>() { "0", "0", "0" };
            if (TRAV_DOME_TRAF == null)
                insertDataList.Add(new DBItem("TRAV_DOME_TRAF", String.Join("", TRAV_DOME_TRAFV), DBItem.DBDataType.String));
            else
            {
                var TRAV_DOME_TRAF_SP = TRAV_DOME_TRAF.Split(',');
                foreach (var i in TRAV_DOME_TRAF_SP)
                    TRAV_DOME_TRAFV[Convert.ToInt32(i)] = "1";
                insertDataList.Add(new DBItem("TRAV_DOME_TRAF", String.Join("", TRAV_DOME_TRAFV), DBItem.DBDataType.String));
            }
            if (TRAV_DOME_TRAFV[2] == "1")//(其他TXT)
                insertDataList.Add(new DBItem("TRAV_DOME_TRAF_OTH", form["TXT_TRAV_DOME_TRAF_OTH"], DBItem.DBDataType.String));
            else
                insertDataList.Add(new DBItem("TRAV_DOME_TRAF_OTH", null, DBItem.DBDataType.String));
            //國外旅遊國家
            insertDataList.Add(new DBItem("TRAV_ABO_COUN", form["TXT_TRAV_ABO_COUN"], DBItem.DBDataType.String));
            //國外旅遊目的地
            insertDataList.Add(new DBItem("TRAV_ABO_DEST", form["TXT_TRAV_ABO_DEST"], DBItem.DBDataType.String));
            //國外旅遊交通方式
            var TRAV_ABO_TRAF = form["CK_TRAV_ABO_TRAF"];
            var TRAV_ABO_TRAFV = new List<string>() { "0", "0", "0" };
            if (TRAV_ABO_TRAF == null)
                insertDataList.Add(new DBItem("TRAV_ABO_TRAF", String.Join("", TRAV_ABO_TRAFV), DBItem.DBDataType.String));
            else
            {
                var TRAV_ABO_TRAF_SP = TRAV_ABO_TRAF.Split(',');
                foreach (var i in TRAV_ABO_TRAF_SP)
                    TRAV_ABO_TRAFV[Convert.ToInt32(i)] = "1";
                insertDataList.Add(new DBItem("TRAV_ABO_TRAF", String.Join("", TRAV_ABO_TRAFV), DBItem.DBDataType.String));
            }
            if (TRAV_ABO_TRAFV[2] == "1")//(其他TXT)
                insertDataList.Add(new DBItem("TRAV_ABO_TRAF_OTH", form["TXT_TRAV_ABO_TRAF_OTH"], DBItem.DBDataType.String));
            else
                insertDataList.Add(new DBItem("TRAV_ABO_TRAF_OTH", null, DBItem.DBDataType.String));
            //職業別
            var OCCU = form["CK_OCCU"];
            var OCCUV = new List<string>() { "0", "0", "0", "0", "0" };
            if (OCCU == null)
                insertDataList.Add(new DBItem("OCCU", String.Join("", OCCUV), DBItem.DBDataType.String));
            else
            {
                var OCCU_SP = OCCU.Split(',');
                foreach (var i in OCCU_SP)
                    OCCUV[Convert.ToInt32(i)] = "1";
                insertDataList.Add(new DBItem("OCCU", String.Join("", OCCUV), DBItem.DBDataType.String));
            }
            if (OCCUV[4] == "1")//(其他TXT)
                insertDataList.Add(new DBItem("OCCU_OTH", form["TXT_OCCU_OTH"], DBItem.DBDataType.String));
            else
                insertDataList.Add(new DBItem("OCCU_OTH", null, DBItem.DBDataType.String));
            //接觸史
            var CONT = form["CK_CONT"];
            var CONTV = new List<string>() { "0", "0", "0" };
            if (CONT == null)
                insertDataList.Add(new DBItem("CONT", String.Join("", CONTV), DBItem.DBDataType.String));
            else
            {
                var CONT_SP = CONT.Split(',');
                foreach (var i in CONT_SP)
                    CONTV[Convert.ToInt32(i)] = "1";
                insertDataList.Add(new DBItem("CONT", String.Join("", CONTV), DBItem.DBDataType.String));
            }
            if (CONTV[1] == "1")//(接觸禽鳥類、畜類等)
            {
                var CONT_BIRD = form["CK_CONT_BIRD"];
                var CONT_BIRDV = new List<string>() { "0", "0" };
                if (CONT_BIRD == null)
                    insertDataList.Add(new DBItem("CONT_BIRD", String.Join("", CONT_BIRDV), DBItem.DBDataType.String));
                else
                {
                    var CONT_BIRD_SP = CONT_BIRD.Split(',');
                    foreach (var i in CONT_BIRD_SP)
                        CONT_BIRDV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("CONT_BIRD", String.Join("", CONT_BIRDV), DBItem.DBDataType.String));
                }
            }
            else
                insertDataList.Add(new DBItem("CONT_BIRD", null, DBItem.DBDataType.String));

            //嬰幼兒、新生兒接觸史
            //(生產前 14 天內，產婦或同住家人有發燒、腹瀉、咳嗽、流鼻水等疑似感染症狀?)
            insertDataList.Add(new DBItem("CONT_OBS_SYM", form["RB_CONT_OBS_SYM"], DBItem.DBDataType.String));
            //(生產前 14 天內，寶寶的哥哥、姊姊學校班上同學有因為傳染病請假或班級停課之情形?)
            insertDataList.Add(new DBItem("CONT_OBS_SICKLEAVE", form["RB_CONT_OBS_SICKLEAVE"], DBItem.DBDataType.String));
            //(住院期間照顧者(應盡量維持同一人)，目前有發燒、腹瀉、咳嗽、流鼻水等疑似感染症狀?)
            insertDataList.Add(new DBItem("CONT_OBS_CARESYM", form["RB_CONT_OBS_CARESYM"], DBItem.DBDataType.String));

            if (CONTV[2] == "1")//(其他)
            {
                insertDataList.Add(new DBItem("CONT_OTH", form["TXT_CONT_OTH"], DBItem.DBDataType.String));
            }
            else
                insertDataList.Add(new DBItem("CONT_OTH", null, DBItem.DBDataType.String));

            //群聚史
            insertDataList.Add(new DBItem("CLU", form["RB_CLU"], DBItem.DBDataType.String));
            if (form["RB_CLU"] == "1")//(家人/朋友/同事有發燒或類流感症狀)
            {
                var CLU_RELA = form["CK_CLU_RELA"];
                var CLU_RELAV = new List<string>() { "0", "0", "0", "0" };
                if (CLU_RELA == null)
                    insertDataList.Add(new DBItem("CLU_RELA", String.Join("", CLU_RELAV), DBItem.DBDataType.String));
                else
                {
                    var CLU_RELA_SP = CLU_RELA.Split(',');
                    foreach (var i in CLU_RELA_SP)
                        CLU_RELAV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("CLU_RELA", String.Join("", CLU_RELAV), DBItem.DBDataType.String));
                }
                if (CLU_RELAV[3] == "1")
                {
                    insertDataList.Add(new DBItem("CLU_RELA_OTH", form["TXT_CLU_RELA_OTH"], DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("CLU_RELA_OTH", null, DBItem.DBDataType.String));
                }
            }
            else
            {
                var CLU_RELAV = new List<string>() { "0", "0", "0", "0" };
                insertDataList.Add(new DBItem("CLU_RELA", String.Join("", CLU_RELAV), DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("CLU_RELA_OTH", null, DBItem.DBDataType.String));
            }
            #endregion

            var HOSP = form["RB_HOSP"];
            insertDataList.Add(new DBItem("HOSP", HOSP, DBItem.DBDataType.String));
            var SUR = form["RB_SUR"];
            insertDataList.Add(new DBItem("SUR", SUR, DBItem.DBDataType.String));

            insertDataList = setNullToEmpty(insertDataList);
            int erow = link.DBExecInsertTns("OBS_BABYENTR", insertDataList);

            #region 護理紀錄
            var exceptionResult = new List<string>();
            var finalResult = "入院經過：";
            finalResult += $"{(form["TXT_PROCESS"] == "" ? "未填寫" : form["TXT_PROCESS"])}。";
            finalResult += "護理評估：";

            #region 頭部-外觀
            var headFlag = false;
            if (form["RB_ASS_1_1"] == "0")
            {
                var items = new List<string>();
                var bone = new List<string>() { "1.頭骨", "2.頂骨", "3.左顳骨", "4.右顳骨", "5.枕骨" };
                #region 五大判斷
                for (var i = 1; i <= 5; i++)
                {
                    var CK_ASS_1_1F = form[$"CK_ASS_1_1F{i}"];
                    if (IsNoEmpty(CK_ASS_1_1F))
                    {
                        var other = new List<string>();

                        var values = CK_ASS_1_1F.Split(',');
                        if (values.Length > 0)
                            foreach (var val in values)
                            {
                                if (val == "0" && IsNoEmpty(form[$"TXT_{i}_RED"]))
                                    other.Add($"紅腫{form[$"TXT_{i}_RED"]}");

                                if (val == "1" && IsNoEmpty(form[$"TXT_{i}_BLI"]))
                                    other.Add($"水泡{form[$"TXT_{i}_BLI"]}");

                                if (val == "2" && IsNoEmpty(form[$"TXT_{i}_HUR"]))
                                    other.Add($"破皮{form[$"TXT_{i}_HUR"]}");

                                if (val == "3" && IsNoEmpty(form[$"TXT_{i}_CS"]))
                                    other.Add($"產瘤{form[$"TXT_{i}_CS"]}");

                                if (val == "4" && IsNoEmpty(form[$"TXT_{i}_OTH"]))
                                    other.Add(form[$"TXT_{i}_OTH"]);
                            }
                        items.Add($"{bone[i - 1]}：{String.Join("、", other)}");
                    }
                }
                #endregion
                if (items.Count > 0)
                    exceptionResult.Add($"頭部外觀：{String.Join("、", items)}");
            }
            #endregion

            #region 頭部-囪門
            if (form["RB_ASS_1_2"] == "0")
            {
                var CK_ASS_1_2F1 = form["CK_ASS_1_2F1"];
                if (IsNoEmpty(CK_ASS_1_2F1))
                {
                    var front = new List<string>();
                    var back = new List<string>();
                    var result = "";

                    var values = CK_ASS_1_2F1.Split(',');
                    if (values.Length > 0)
                        result += $"{(headFlag == false ? "頭部" : "")}囪門：";
                    foreach (var val in values)
                    {
                        if (val == "0")
                            front.Add("關閉");
                        if (val == "1")
                            front.Add("凹陷");
                        if (val == "2")
                            front.Add("膨出");
                        if (val == "3" && IsNoEmpty(form["TXT_ASS_1_2F2"]))
                            front.Add(form["TXT_ASS_1_2F2"]);

                        if (val == "4")
                            back.Add("關閉");
                        if (val == "5")
                            back.Add("凹陷");
                        if (val == "6")
                            back.Add("膨出");
                        if (val == "7" && IsNoEmpty(form["TXT_ASS_1_2F3"]))
                            back.Add(form["TXT_ASS_1_2F3"]);
                    }
                    if (front.Count > 0)
                        result += "前：" + String.Join("、", front);
                    if (back.Count > 0)
                        result += $"{(front.Count > 0 ? "，" : "")}後：{String.Join("、", back)}";

                    exceptionResult.Add(result);
                }
            }
            #endregion

            #region 臉部
            if (form["RB_ASS_2"] == "0")
            {
                var CK_ASS_2F = form["CK_ASS_2F"];
                if (IsNoEmpty(CK_ASS_2F))
                {
                    var left = new List<string>();
                    var right = new List<string>();
                    var other = new List<string>();
                    var result = "";

                    var values = CK_ASS_2F.Split(',');
                    result += "臉部：";
                    foreach (var val in values)
                    {
                        if (val == "0")
                            result += "不對稱";

                        if (val == "1")
                            left.Add("嘴角下垂");
                        if (val == "2")
                            left.Add("瘜肉");

                        if (val == "3")
                            right.Add("嘴角下垂");
                        if (val == "4")
                            right.Add("瘜肉");

                        if (val == "7" && IsNoEmpty(form["TXT_ASS_2FO"]))
                            other.Add(form["TXT_ASS_2FO"].ToString());
                    }
                    if (left.Count > 0)
                        result += $"，左：{String.Join("、", left)}";
                    if (right.Count > 0)
                        result += $"，右：{String.Join("、", right)}";
                    if (other.Count > 0)
                        result += $"，{String.Join("、", other)}";

                    exceptionResult.Add(result);
                }
            }
            #endregion

            #region 眼睛
            if (form["RB_ASS_3"] == "0")
            {
                var CK_ASS_3F = form["CK_ASS_3F"];
                if (IsNoEmpty(CK_ASS_3F))
                {
                    var left = new List<string>();
                    var right = new List<string>();
                    var other = new List<string>();
                    var result = "";

                    var values = CK_ASS_3F.Split(',');
                    result += "眼睛：";
                    foreach (var val in values)
                    {
                        if (val == "0")
                            left.Add("結膜出血");
                        if (val == "1")
                            left.Add("眼瞼水腫");

                        if (val == "2")
                            right.Add("結膜出血");
                        if (val == "3")
                            right.Add("眼瞼水腫");
                        if (val == "4")
                            other.Add("雙眼距離過大");
                        if (val == "5" && IsNoEmpty(form["TXT_ASS_3FO"]))
                            other.Add(form["TXT_ASS_3FO"].ToString());
                    }
                    if (left.Count > 0)
                        result += $"左：{String.Join("、", left)}";
                    if (right.Count > 0)
                        result += $"{(left.Count > 0 ? "，" : "")}右：{String.Join("、", right)}";
                    if (other.Count > 0)
                        result += $"{(left.Count > 0 || right.Count > 0 ? "，" : "")}{String.Join("、", other)}";

                    exceptionResult.Add(result);
                }
            }
            #endregion

            #region 耳朵
            if (form["RB_ASS_4"] == "0")
            {
                var CK_ASS_4F = form["CK_ASS_4F"];
                if (IsNoEmpty(CK_ASS_4F))
                {
                    var left = new List<string>();
                    var right = new List<string>();
                    var other = new List<string>();
                    var result = "";

                    var values = CK_ASS_4F.Split(',');
                    result += "耳朵：";
                    foreach (var val in values)
                    {
                        if (val == "0")
                            left.Add("低位耳");
                        if (val == "1")
                            left.Add("耳殼異常");
                        if (val == "2")
                            left.Add("瘜肉");

                        if (val == "3")
                            right.Add("低位耳");
                        if (val == "4")
                            right.Add("耳殼異常");
                        if (val == "5")
                            right.Add("瘜肉");

                        if (val == "6" && IsNoEmpty(form["TXT_ASS_4FO"]))
                            other.Add(form["TXT_ASS_4FO"].ToString());
                    }
                    if (left.Count > 0)
                        result += $"左：{String.Join("、", left)}";
                    if (right.Count > 0)
                        result += $"{(left.Count > 0 ? "，" : "")}右：{String.Join("、", right)}";
                    if (other.Count > 0)
                        result += $"{(left.Count > 0 || right.Count > 0 ? "，" : "")}{String.Join("、", other)}";

                    exceptionResult.Add(result);
                }
            }
            #endregion

            #region 口腔
            if (form["RB_ASS_6"] == "0")
            {
                var CK_ASS_6F = form["CK_ASS_6F"];
                if (IsNoEmpty(CK_ASS_6F))
                {
                    var other = new List<string>();
                    var result = "";

                    var values = CK_ASS_6F.Split(',');
                    result += "口腔：唇裂與顎裂：";
                    foreach (var val in values)
                    {
                        if (val == "0")
                            other.Add("單側不完全唇裂");
                        if (val == "1")
                            other.Add("單側完全唇裂與顎裂");
                        if (val == "2")
                            other.Add("雙側完全唇裂與顎裂");
                        if (val == "3")
                            other.Add("顎裂");
                        if (val == "4" && IsNoEmpty(form["TXT_ASS_6FO"]))
                            other.Add(form["TXT_ASS_6FO"].ToString());
                    }
                    result += $"{String.Join("、", other)}";

                    exceptionResult.Add(result);
                }
            }
            #endregion

            #region 頸部
            if (form["RB_ASS_7"] == "0")
            {
                var CK_ASS_7F = form["CK_ASS_7F"];
                if (IsNoEmpty(CK_ASS_7F))
                {
                    var left = new List<string>();
                    var right = new List<string>();
                    var other = new List<string>();
                    var result = "";

                    var values = CK_ASS_7F.Split(',');
                    result += "頸部：";
                    foreach (var val in values)
                    {
                        if (val == "0")
                            left.Add("腫塊");
                        if (val == "1")
                            left.Add("疑斜頸");
                        if (val == "2")
                            left.Add("僵硬");

                        if (val == "3")
                            right.Add("腫塊");
                        if (val == "4")
                            right.Add("疑斜頸");
                        if (val == "5")
                            right.Add("僵硬");

                        if (val == "6" && IsNoEmpty(form["TXT_ASS_7FO"]))
                            other.Add(form["TXT_ASS_7FO"].ToString());
                    }
                    if (left.Count > 0)
                        result += $"左：{String.Join("、", left)}";
                    if (right.Count > 0)
                        result += $"{(left.Count > 0 ? "，" : "")}右：{String.Join("、", right)}";
                    if (other.Count > 0)
                        result += $"{(left.Count > 0 || right.Count > 0 ? "，" : "")}{String.Join("、", other)}";

                    exceptionResult.Add(result);
                }
            }
            #endregion

            #region 胸部-外觀
            var chestFlag = false;
            if (form["RB_ASS_8_1"] == "0")
            {
                var CK_ASS_8_1F = form["CK_ASS_8_1F"];
                if (IsNoEmpty(CK_ASS_8_1F))
                {
                    var other = new List<string>();
                    var result = "";

                    var values = CK_ASS_8_1F.Split(',');
                    result += "胸部外觀：";
                    foreach (var val in values)
                    {
                        if (val == "0")
                            other.Add("胸骨或肋緣凹陷");
                        if (val == "1" && IsNoEmpty(form["TXT_ASS_8_1FO"]))
                            other.Add(form["TXT_ASS_8_1FO"].ToString());
                    }
                    result += $"{String.Join("、", other)}";

                    exceptionResult.Add(result);
                    chestFlag = true;
                }
            }
            #endregion

            #region 胸部-呼吸
            if (form["RB_ASS_8_2"] == "0")
            {
                var CK_ASS_8_2F = form["CK_ASS_8_2F"];
                if (IsNoEmpty(CK_ASS_8_2F))
                {
                    var other = new List<string>();
                    var result = "";

                    var values = CK_ASS_8_2F.Split(',');
                    result += $"{(chestFlag == false ? "胸部" : "")}呼吸：";
                    foreach (var val in values)
                    {
                        if (val == "0")
                            other.Add($"{(IsNoEmpty(form["TXT_ASS_8_2FO"]) ? "呼吸急促" + form["TXT_ASS_8_2FO"] + "次/分" : "呼吸急促")}");
                        if (val == "1")
                            other.Add("鼻翼搧動");
                        if (val == "2")
                            other.Add("呻吟聲(grunting)");
                        if (val == "3")
                            other.Add("胸骨凹陷");
                    }
                    result += $"{String.Join("、", other)}";

                    exceptionResult.Add(result);
                    chestFlag = true;
                }
            }
            #endregion

            #region 胸部-呼吸音
            if (form["RB_ASS_8_3"] == "0")
            {
                var CK_ASS_8_3F = form["CK_ASS_8_3F"];
                if (IsNoEmpty(CK_ASS_8_3F))
                {
                    var other = new List<string>();
                    var result = "";

                    var values = CK_ASS_8_3F.Split(',');
                    result += $"{(chestFlag == false ? "胸部" : "")}呼吸音：";
                    foreach (var val in values)
                    {
                        if (val == "0")
                            other.Add("Rhonchi 乾囉音");
                        if (val == "1")
                            other.Add("wheeze 喘嗚音");
                        if (val == "2")
                            other.Add("Bronchial 支氣管音");
                        if (val == "3")
                            other.Add("Rub 摩擦音");
                        if (val == "4")
                            other.Add("Crackles 囉音");
                    }
                    result += $"{String.Join("、", other)}";

                    exceptionResult.Add(result);
                    chestFlag = true;
                }
            }
            #endregion

            #region 胸部-心臟
            if (form["RB_ASS_8_4"] == "0")
            {
                var CK_ASS_8_4F = form["CK_ASS_8_4F"];
                if (IsNoEmpty(CK_ASS_8_4F))
                {
                    var other = new List<string>();
                    var result = "";

                    var values = CK_ASS_8_4F.Split(',');
                    result += $"{(chestFlag == false ? "胸部" : "")}心臟：";
                    foreach (var val in values)
                    {
                        if (val == "0")
                            other.Add("心跳不規則");
                        if (val == "1")
                            other.Add("雜音");
                        if (val == "2" && IsNoEmpty(form["TXT_ASS_8_4FO"]))
                            other.Add(form["TXT_ASS_8_4FO"]);
                    }
                    result += $"{String.Join("、", other)}";

                    exceptionResult.Add(result);
                    chestFlag = true;
                }
            }
            #endregion

            #region 臀背部
            if (form["RB_ASS_11"] == "0")
            {
                var CK_ASS_11F = form["CK_ASS_11F"];
                if (IsNoEmpty(CK_ASS_11F))
                {
                    var other = new List<string>();
                    var result = "";

                    var values = CK_ASS_11F.Split(',');
                    result += $"臀背部：";
                    foreach (var val in values)
                    {
                        if (val == "0")
                            other.Add("毛性小凹");
                        if (val == "1")
                            other.Add("脊髓膜膨出");
                        if (val == "2")
                            other.Add("脊椎彎曲");
                        if (val == "3" && IsNoEmpty(form["TXT_ASS_11FO"]))
                            other.Add(form["TXT_ASS_11FO"]);
                    }
                    result += $"{String.Join("、", other)}";

                    exceptionResult.Add(result);
                }
            }
            #endregion

            #region 泌尿生殖器外觀-男-陰囊
            var downFlag = false;
            if (form["RB_ASS_12_2"] == "0")
            {
                var CK_ASS_12_2F = form["CK_ASS_12_2F"];
                if (IsNoEmpty(CK_ASS_12_2F))
                {
                    var other = new List<string>();
                    var result = "";

                    var values = CK_ASS_12_2F.Split(',');
                    result += $"泌尿生殖器外觀男陰囊：";
                    foreach (var val in values)
                    {
                        if (val == "0")
                            other.Add("水腫");
                        if (val == "1")
                            other.Add("破皮");
                        if (val == "2" && IsNoEmpty(form["TXT_ASS_12_2FO"]))
                            other.Add(form["TXT_ASS_12_2FO"]);
                    }
                    result += $"{String.Join("、", other)}";

                    exceptionResult.Add(result);
                    downFlag = true;
                }
            }
            #endregion

            #region 泌尿生殖器外觀-肛門
            if (form["RB_ASS_12_8"] == "0")
            {
                var CK_ASS_12_8F = form["CK_ASS_12_8F"];
                if (IsNoEmpty(CK_ASS_12_8F))
                {
                    var other = new List<string>();
                    var result = "";

                    var values = CK_ASS_12_8F.Split(',');
                    result += $"{(downFlag == false ? "泌尿生殖器外觀" : "")}肛門：";
                    foreach (var val in values)
                    {
                        if (val == "0")
                            other.Add("無肛");
                        if (val == "1")
                            other.Add("閉鎖");
                        if (val == "2")
                            other.Add("瘜肉");
                        if (val == "3")
                            other.Add("裂隙");
                        if (val == "4" && IsNoEmpty(form["TXT_ASS_12_8FO"]))
                            other.Add(form["TXT_ASS_12_8FO"]);
                    }
                    result += $"{String.Join("、", other)}";

                    exceptionResult.Add(result);
                    downFlag = true;
                }
            }
            #endregion

            #region 四肢
            var feetFlag = false;
            if (form["RB_ASS_17"] == "0")
            {
                #region 足內翻
                if (form["RB_ASS_17_1"] == "1")
                {
                    var CK_ASS_17_1F = form["CK_ASS_17_1F"];
                    if (IsNoEmpty(CK_ASS_17_1F))
                    {
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_17_1F.Split(',');
                        result += $"{(feetFlag == false ? "四肢：" : "")}足內翻：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                other.Add("左腳");
                            if (val == "1")
                                other.Add("右腳");
                        }

                        result += $"{String.Join("、", other)}";

                        exceptionResult.Add(result);
                        feetFlag = true;
                    }
                }
                #endregion

                #region 足外翻
                if (form["RB_ASS_17_2"] == "1")
                {
                    var CK_ASS_17_2F = form["CK_ASS_17_2F"];
                    if (IsNoEmpty(CK_ASS_17_2F))
                    {
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_17_2F.Split(',');
                        result += $"{(feetFlag == false ? "四肢：" : "")}足外翻：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                other.Add("左腳");
                            if (val == "1")
                                other.Add("右腳");
                        }

                        result += $"{String.Join("、", other)}";

                        exceptionResult.Add(result);
                        feetFlag = true;
                    }
                }
                #endregion

                #region 單一手紋(斷掌)
                if (form["RB_ASS_17_3"] == "1")
                {
                    var CK_ASS_17_3F = form["CK_ASS_17_3F"];
                    if (IsNoEmpty(CK_ASS_17_3F))
                    {
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_17_3F.Split(',');
                        result += $"{(feetFlag == false ? "四肢：" : "")}單一手紋(斷掌)：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                other.Add("左手");
                            if (val == "1")
                                other.Add("右手");
                        }

                        result += $"{String.Join("、", other)}";

                        exceptionResult.Add(result);
                        feetFlag = true;
                    }
                }
                #endregion
            }
            #endregion  

            #region 四肢-手指
            if (form["RB_ASS_17_7"] == "0")
            {
                var CK_ASS_17_7F = form["CK_ASS_17_7F"];
                if (IsNoEmpty(CK_ASS_17_7F))
                {
                    var left = new List<string>();
                    var right = new List<string>();
                    var result = "";

                    var values = CK_ASS_17_7F.Split(',');
                    result += $"{(feetFlag == false ? "四肢 : " : "")}手指：";
                    foreach (var val in values)
                    {
                        if (val == "0")
                            left.Add($"{(IsNoEmpty(form["TXT_ASS_17_7F1"]) ? "少指，部位：" + form["TXT_ASS_17_7F1"] : "少指")}");
                        if (val == "1")
                            left.Add($"{(IsNoEmpty(form["TXT_ASS_17_7F2"]) ? "多指，部位：" + form["TXT_ASS_17_7F2"] : "多指")}");
                        if (val == "2")
                            left.Add($"{(IsNoEmpty(form["TXT_ASS_17_7F3"]) ? "併指，部位：" + form["TXT_ASS_17_7F3"] : "併指")}");
                        if (val == "3")
                            left.Add($"{(IsNoEmpty(form["TXT_ASS_17_7F4"]) ? "蹼狀指，部位：" + form["TXT_ASS_17_7F4"] : "蹼狀指")}");

                        if (val == "4")
                            right.Add($"{(IsNoEmpty(form["TXT_ASS_17_7F5"]) ? "少指，部位：" + form["TXT_ASS_17_7F5"] : "少指")}");
                        if (val == "5")
                            right.Add($"{(IsNoEmpty(form["TXT_ASS_17_7F6"]) ? "多指，部位：" + form["TXT_ASS_17_7F6"] : "多指")}");
                        if (val == "6")
                            right.Add($"{(IsNoEmpty(form["TXT_ASS_17_7F7"]) ? "併指，部位：" + form["TXT_ASS_17_7F7"] : "併指")}");
                        if (val == "7")
                            right.Add($"{(IsNoEmpty(form["TXT_ASS_17_7F8"]) ? "蹼狀指，部位：" + form["TXT_ASS_17_7F8"] : "蹼狀指")}");
                    }

                    if (left.Count > 0)
                        result += $"左：{String.Join("、", left)}";
                    if (right.Count > 0)
                        result += $"{(left.Count > 0 ? "。" : "")}右：{String.Join("、", right)}";

                    exceptionResult.Add(result);
                    feetFlag = true;
                }
            }
            #endregion

            #region 四肢-腳趾
            if (form["RB_ASS_17_8"] == "0")
            {
                var CK_ASS_17_8F = form["CK_ASS_17_8F"];
                if (IsNoEmpty(CK_ASS_17_8F))
                {
                    var left = new List<string>();
                    var right = new List<string>();
                    var result = "";

                    var values = CK_ASS_17_8F.Split(',');
                    result += $"{(feetFlag == false ? "四肢 : " : "")}腳趾：";
                    foreach (var val in values)
                    {
                        if (val == "0")
                            left.Add($"{(IsNoEmpty(form["TXT_ASS_17_8F1"]) ? "少趾，部位：" + form["TXT_ASS_17_8F1"] : "少趾")}");
                        if (val == "1")
                            left.Add($"{(IsNoEmpty(form["TXT_ASS_17_8F2"]) ? "多趾，部位：" + form["TXT_ASS_17_8F2"] : "多趾")}");
                        if (val == "2")
                            left.Add($"{(IsNoEmpty(form["TXT_ASS_17_8F3"]) ? "併趾，部位：" + form["TXT_ASS_17_8F3"] : "併趾")}");

                        if (val == "3")
                            right.Add($"{(IsNoEmpty(form["TXT_ASS_17_8F4"]) ? "少趾，部位：" + form["TXT_ASS_17_8F4"] : "少趾")}");
                        if (val == "4")
                            right.Add($"{(IsNoEmpty(form["TXT_ASS_17_8F5"]) ? "多趾，部位：" + form["TXT_ASS_17_8F5"] : "多趾")}");
                        if (val == "5")
                            right.Add($"{(IsNoEmpty(form["TXT_ASS_17_8F6"]) ? "併趾，部位：" + form["TXT_ASS_17_8F6"] : "併趾")}");
                    }

                    if (left.Count > 0)
                        result += $"左：{String.Join("、", left)}";
                    if (right.Count > 0)
                        result += $"{(left.Count > 0 ? "。" : "")}右：{String.Join("、", right)}";

                    exceptionResult.Add(result);
                    feetFlag = true;
                }
            }
            #endregion

            if (exceptionResult.Count > 0)
                finalResult += String.Join("。", exceptionResult);
            else
                finalResult += "無異常項目";

            if (form["type"].ToString() != "暫存")
            {
                erow += base.Insert_CareRecordTns(dateNow, id, "嬰幼兒入院護理評估", finalResult + "。", "", "", "", "", "OBS_BABYENTR", ref link);
            }

            #endregion

            #region 拋轉TOCC
            string TOCCid = creatid("TOCC_DATA", userno, feeno, "0");

            var TOCCmsg = "";
            //症狀
            TOCCmsg += "病人目前";
            if (form["CK_SYM"] != "" && form["CK_SYM"] != null)
            {
                if (form["CK_SYM"] == "0" || form["CK_SYM"] == "")
                {
                    TOCCmsg += "無症狀。";
                }
                else
                {
                    var symptomArr = form["CK_SYM"].Split(',');
                    var symptomTransArr = new List<string>();

                    foreach (var val in symptomArr)
                    {
                        if (val == "1")
                            symptomTransArr.Add("發燒(≥38℃)");
                        if (val == "2")
                            symptomTransArr.Add("咳嗽");
                        if (val == "3")
                            symptomTransArr.Add("喘");
                        if (val == "4")
                            symptomTransArr.Add("流鼻水");
                        if (val == "5")
                            symptomTransArr.Add("鼻塞");
                        if (val == "6")
                            symptomTransArr.Add("喉嚨痛");
                        if (val == "7")
                            symptomTransArr.Add("肌肉痠痛");
                        if (val == "8")
                            symptomTransArr.Add("頭痛");
                        if (val == "9")
                            symptomTransArr.Add("極度疲倦感");
                    }


                    if (form["TXT_SYM_OTH"].ToString() != "" && form["TXT_SYM_OTH"] != null)
                    {
                        symptomTransArr.Add(form["TXT_SYM_OTH"].ToString());
                    }
                    TOCCmsg += String.Join("、", symptomTransArr);

                    TOCCmsg += "。";
                }
            }

            //旅遊史(Travel)
            TOCCmsg += "旅遊史(Travel)：最近14日內";

            if (form["CK_TRAV"] == "0")
            {
                TOCCmsg += "無國內、外旅遊。";
            }
            else
            {
                if (form["CK_TRAV"] != "" && form["CK_TRAV"] != null)
                {
                    string travel = form["CK_TRAV"].ToString();
                    var travleArr = travel.Split(',');
                    for (int i = 0; i < travleArr.Count(); i++)
                    {
                        switch (travleArr[i])
                        {
                            case "1":
                                TOCCmsg += "國內旅遊";
                                TOCCmsg += "(";
                                if (form["TXT_TRAV_DOME_CITY"] != "")
                                {
                                    TOCCmsg += form["TXT_TRAV_DOME_CITY"];
                                }
                                if (form["TXT_TRAV_DOME_CITY"] != "" && form["TXT_TRAV_DOME_VIEW"] != "")
                                {
                                    TOCCmsg += "，";
                                }
                                if (form["TXT_TRAV_DOME_VIEW"] != "")
                                {
                                    TOCCmsg += form["TXT_TRAV_DOME_VIEW"];
                                }
                                if (form["TXT_TRAV_DOME_CITY"] != "" || form["TXT_TRAV_DOME_VIEW"] != "")
                                {
                                    TOCCmsg += "，";
                                }
                                TOCCmsg += "交通方式：";

                                var traffic = form["CK_TRAV_DOME_TRAF"].Split(',');
                                var trafficTransArr = new List<string>();

                                foreach (var val in traffic)
                                {
                                    if (val == "0")
                                        trafficTransArr.Add("大眾運輸");
                                    if (val == "1")
                                        trafficTransArr.Add("自行駕駛");
                                }
                                if (form["TXT_TRAV_DOME_TRAF_OTH"] != "")
                                {
                                    trafficTransArr.Add(form["TXT_TRAV_DOME_TRAF_OTH"]);
                                }
                                TOCCmsg += String.Join("、", trafficTransArr);

                                if (travleArr.Count() > 1)
                                {
                                    TOCCmsg += "、";
                                }
                                break;
                            case "2":
                                TOCCmsg += "國外旅遊(包含轉機或船舶停靠曾到訪)";
                                TOCCmsg += "(";
                                if (form["TXT_TRAV_ABO_COUN"] != "")
                                {
                                    TOCCmsg += form["TXT_TRAV_ABO_COUN"];
                                }
                                if (form["TXT_TRAV_ABO_COUN"] != "" && form["TXT_TRAV_ABO_DEST"] != "")
                                {
                                    TOCCmsg += "，";
                                }
                                if (form["TXT_TRAV_ABO_DEST"] != "")
                                {
                                    TOCCmsg += form["TXT_TRAV_ABO_DEST"];
                                }
                                if (form["TXT_TRAV_ABO_COUN"] != "" || form["TXT_TRAV_ABO_DEST"] != "")
                                {
                                    TOCCmsg += "，";
                                }
                                TOCCmsg += "交通方式：";

                                var trafficAbo = form["CK_TRAV_ABO_TRAF"].Split(',');
                                var trafficTransAboArr = new List<string>();

                                foreach (var val in trafficAbo)
                                {
                                    if (val == "0")
                                        trafficTransAboArr.Add("大眾運輸");
                                    if (val == "1")
                                        trafficTransAboArr.Add("自行駕駛");
                                }
                                if (form["TXT_TRAV_ABO_TRAF_OTH"] != "" && form["TXT_TRAV_ABO_TRAF_OTH"] != null)
                                {
                                    trafficTransAboArr.Add(form["TXT_TRAV_ABO_TRAF_OTH"]);
                                }
                                TOCCmsg += String.Join("、", trafficTransAboArr);
                                break;
                        }
                    }
                }
                TOCCmsg += "。";
            }
            //職業別
            TOCCmsg += "職業別(Occupation)：";
            if (form["CK_OCCU"] != "" && form["CK_OCCU"] != null)
            {
                var occuArr = form["CK_OCCU"].Split(',');
                var occuTransArr = new List<string>();

                foreach (var val in occuArr)
                {
                    if (val == "0")
                        occuTransArr.Add("無");
                    if (val == "1")
                        occuTransArr.Add("醫事機構工作者");
                    if (val == "2")
                        occuTransArr.Add("禽畜販賣業者");
                    if (val == "3")
                        occuTransArr.Add("航空服務業工作者");

                }
                if (form["TXT_OCCU_OTH"] != "" && form["TXT_OCCU_OTH"] != null)
                {
                    occuTransArr.Add(form["TXT_OCCU_OTH"].ToString());
                }
                TOCCmsg += String.Join("、", occuTransArr);
                TOCCmsg += "。";
            }

            //接觸史
            TOCCmsg += "接觸史(Contact)：";
            if ((form["CK_CONT"] != "" && form["CK_CONT"] != null)|| (form["RB_CONT_OBS_SYM"] !=  null && form["RB_CONT_OBS_SYM"] != ""))
            {
                if (form["CK_CONT"] == "0")
                {
                    TOCCmsg += "無。";
                }
                else
                {
                    if (form["CK_CONT"] != "" && form["CK_CONT"] != null)
                    {
                        string contact = form["CK_CONT"].ToString();

                        var contactArr = contact.Split(',');


                        if (contactArr.Contains("1"))
                        {
                            TOCCmsg += "接觸禽鳥類、畜類等 : ";
                            if (form["CK_CONT_BIRD"] != "" && form["CK_CONT_BIRD"] != null)
                            {
                                var birdArr = form["CK_CONT_BIRD"].Split(',');
                                var tempBird = new List<string>();
                                foreach (var val in birdArr)
                                {
                                    if (val == "0")
                                        tempBird.Add("禽鳥類接觸：如雞、鴨等");
                                    if (val == "1")
                                        tempBird.Add("畜類接觸：如豬、貓、狗等");
                                }
                                TOCCmsg += "(" + String.Join("、", tempBird) + ")";
                            }
                            TOCCmsg += "。";
                        }
                    }
                    TOCCmsg += "嬰幼兒接觸史：";
                    if (form["RB_CONT_OBS_SYM"] != "" && form["RB_CONT_OBS_SYM"] != null)
                    {
                        TOCCmsg += "(1) 住院前 14 天內，同住家人有發燒、腹瀉、咳嗽、流鼻水等疑似感染症狀 : ";
                        if (form["RB_CONT_OBS_SYM"] == "0")
                        {
                            TOCCmsg += "無。";
                        }
                        else
                        {
                            TOCCmsg += "有。";
                        }
                    }
                    if (form["RB_CONT_OBS_SICKLEAVE"] != "" && form["RB_CONT_OBS_SICKLEAVE"] != null)
                    {
                        TOCCmsg += "(2) 住院前 14 天內，同住家人學校班上同學有因為傳染病請假或班級停課之情形 : ";
                        if (form["RB_CONT_OBS_SICKLEAVE"] == "0")
                        {
                            TOCCmsg += "無。";
                        }
                        else
                        {
                            TOCCmsg += "有。";
                        }
                    }
                    if (form["RB_CONT_OBS_CARESYM"] != "" && form["RB_CONT_OBS_CARESYM"] != null)
                    {
                        TOCCmsg += "(3) 住院期間照顧者(應盡量維持同一人)，目前有發燒、腹瀉、咳嗽、流鼻水等疑似感染症狀 : ";
                        if (form["RB_CONT_OBS_CARESYM"] == "0")
                        {
                            TOCCmsg += "無。";
                        }
                        else
                        {
                            TOCCmsg += "有。";
                        }
                    }

                    if (form["CK_CONT"] != "" && form["CK_CONT"] != null)
                    {
                        string contact = form["CK_CONT"].ToString();

                        var contactArr = contact.Split(',');
                        if (contactArr.Contains("2"))
                        {
                            if (form["TXT_CONT_OTH"] != "" && form["TXT_CONT_OTH"] != null)
                            {
                                TOCCmsg += form["TXT_CONT_OTH"] + "。";
                            }
                        }
                    }
                }
            }

            //群聚史(Cluster)
            TOCCmsg += "群聚史(Cluster)：";
            if (form["RB_CLU"] != "" && form["RB_CLU"] != null)
            {
                if (form["RB_CLU"] == "0")
                {
                    TOCCmsg += "無。";
                }
                else
                {
                    TOCCmsg += "家人/朋友/同事有發燒或類流感症狀：";
                    if (form["CK_CLU_RELA"] != "" && form["CK_CLU_RELA"] != null)
                    {
                        var clusterArr = form["CK_CLU_RELA"].Split(',');
                        var clusterTransArr = new List<string>();

                        foreach (var val in clusterArr)
                        {
                            if (val == "0")
                                clusterTransArr.Add("家人");
                            if (val == "1")
                                clusterTransArr.Add("朋友");
                            if (val == "2")
                                clusterTransArr.Add("同事");
                        }
                        if (form["TXT_CLU_RELA_OTH"] != "" && form["TXT_CLU_RELA_OTH"] != null)
                        {
                            clusterTransArr.Add( form["TXT_CLU_RELA_OTH"]);
                        }
                        TOCCmsg += String.Join("、", clusterTransArr);
                    }
                    TOCCmsg += "。";
                }
            }

            if (TOCCmsg != "" && form["type"].ToString() != "暫存")
            {
                erow += base.Insert_CareRecord(dateNow, TOCCid, "TOCC評估", TOCCmsg, "", "", "", "", "TOCC");
            }
            #endregion

            if (erow > 0)
            {
                #region 新增住院經驗
                int erow_HOSP = 0;
                var HOSP_RS = form["TXT_HOSP_RS"].Split(',');
                var HOSP_P = form["TXT_HOSP_P"].Split(',');
                if (HOSP == "1")
                {
                    for (var i = 0; i < HOSP_RS.Length; i++)
                    {
                        List<DBItem> insertDataList_HOSP = new List<DBItem>();
                        insertDataList_HOSP.Add(new DBItem("IID", id, DBItem.DBDataType.String));
                        insertDataList_HOSP.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                        insertDataList_HOSP.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
                        insertDataList_HOSP.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                        insertDataList_HOSP.Add(new DBItem("RECORDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime)); //紀錄時間

                        insertDataList_HOSP.Add(new DBItem("SNO", (i + 1).ToString(), DBItem.DBDataType.String));
                        insertDataList_HOSP.Add(new DBItem("HOSP_RS", HOSP_RS[i], DBItem.DBDataType.String));
                        insertDataList_HOSP.Add(new DBItem("HOSP_P", HOSP_P[i], DBItem.DBDataType.String));

                        insertDataList_HOSP = setNullToEmpty(insertDataList_HOSP);
                        erow_HOSP += link.DBExecInsertTns("OBS_BABYENTR_HO", insertDataList_HOSP);
                    }
                }
                #endregion

                #region 新增手術經驗

                int erow_SUR = 0;
                var SUR_RS = form["TXT_SUR_RS"].Split(',');
                var SUR_P = form["TXT_SUR_P"].Split(',');
                if (SUR == "1")
                {
                    for (var i = 0; i < SUR_RS.Length; i++)
                    {
                        List<DBItem> insertDataList_SUR = new List<DBItem>();
                        insertDataList_SUR.Add(new DBItem("IID", id, DBItem.DBDataType.String));
                        insertDataList_SUR.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                        insertDataList_SUR.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
                        insertDataList_SUR.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                        insertDataList_SUR.Add(new DBItem("RECORDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime)); //紀錄時間

                        insertDataList_SUR.Add(new DBItem("SNO", (i + 1).ToString(), DBItem.DBDataType.String));
                        insertDataList_SUR.Add(new DBItem("SUR_RS", SUR_RS[i], DBItem.DBDataType.String));
                        insertDataList_SUR.Add(new DBItem("SUR_P", SUR_P[i], DBItem.DBDataType.String));

                        insertDataList_SUR = setNullToEmpty(insertDataList_SUR);
                        erow_SUR += link.DBExecInsertTns("OBS_BABYENTR_SG", insertDataList_SUR);
                    }
                }
                #endregion

                #region 新增聯絡人

                var COUNT = Convert.ToInt32(form["CONTACT_COUNT"]);
                int erow_CONTACT = 0;

                for (var i = 0; i < COUNT; i++)
                {
                    List<DBItem> insertDataList_CONTACT = new List<DBItem>();
                    insertDataList_CONTACT.Add(new DBItem("IID", id, DBItem.DBDataType.String));
                    insertDataList_CONTACT.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                    insertDataList_CONTACT.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
                    insertDataList_CONTACT.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                    insertDataList_CONTACT.Add(new DBItem("RECORDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime)); //紀錄時間

                    insertDataList_CONTACT.Add(new DBItem("SNO", (i + 1).ToString(), DBItem.DBDataType.String));
                    insertDataList_CONTACT.Add(new DBItem("CONTACT", form["TXT_CONTACT_" + (i + 1).ToString()], DBItem.DBDataType.String));
                    insertDataList_CONTACT.Add(new DBItem("TITLE", form["RB_TITLE_" + (i + 1).ToString()], DBItem.DBDataType.String));
                    if (form["RB_TITLE_" + i] == "9")
                        insertDataList_CONTACT.Add(new DBItem("TITLE_OTH", form["TXT_TITLE_OTH_" + (i + 1)], DBItem.DBDataType.String));
                    else
                        insertDataList_CONTACT.Add(new DBItem("TITLE_OTH", null, DBItem.DBDataType.String));

                    insertDataList_CONTACT.Add(new DBItem("TEL_C", form["TXT_TEL_C_" + (i + 1).ToString()], DBItem.DBDataType.String));
                    insertDataList_CONTACT.Add(new DBItem("TEL_H", form["TXT_TEL_H_" + (i + 1).ToString()], DBItem.DBDataType.String));
                    insertDataList_CONTACT.Add(new DBItem("TEL_M", form["TXT_TEL_M_" + (i + 1).ToString()], DBItem.DBDataType.String));

                    insertDataList_CONTACT = setNullToEmpty(insertDataList_CONTACT);
                    erow_CONTACT += link.DBExecInsertTns("OBS_BABYENTR_CT", insertDataList_CONTACT);
                }
                #endregion

                if (erow_HOSP != HOSP_RS.Length && HOSP_RS[0] != "")
                {
                    link.DBRollBack();
                    json_result.status = RESPONSE_STATUS.ERROR;
                    json_result.message = "住院經驗 新增失敗";
                    //return "-1";
                }
                //Response.Write("<script>alert('住院經驗新增失敗');window.location.href='../BabyBorn/BabyEntr_List';</script>");
                else if (erow_SUR != SUR_RS.Length && SUR_RS[0] != "")
                {
                    link.DBRollBack();
                    json_result.status = RESPONSE_STATUS.ERROR;
                    json_result.message = "手術經驗 新增失敗";
                    //return "-2";
                }
                //Response.Write("<script>alert('手術經驗新增失敗');window.location.href='../BabyBorn/BabyEntr_List';</script>");
                else if (erow_CONTACT != COUNT)
                {
                    link.DBRollBack();
                    json_result.status = RESPONSE_STATUS.ERROR;
                    json_result.message = "緊急聯絡人 新增失敗";
                    //return "-3";
                }
                //Response.Write("<script>alert('緊急聯絡人新增失敗');window.location.href='../BabyBorn/BabyEntr_List';</script>");
                else
                {
                    link.DBCommit();
                    string return_jsonstr = string.Empty;
                    if (form["TYPE"] == "已完成")
                    {

                        PdfEmr.ControllerContext = ControllerContext;
                        PdfEmr.userinfo = userinfo;
                        PdfEmr.ptinfo = ptinfo;
                        return_jsonstr = PdfEmr.GetPDF_EMR("Save", feeno, userno, "Insert_BabyEntr", id);
                        json_result = JsonConvert.DeserializeObject<RESPONSE_MSG>(return_jsonstr);
                    }
                    else
                    {
                        json_result.status = RESPONSE_STATUS.SUCCESS;
                        json_result.message = "";

                    }
                    //return "1";

                }
                //Response.Write("<script>alert('新增成功');window.location.href='../BabyBorn/BabyEntr_List';</script>");
            }
            else
            {
                link.DBRollBack();
                json_result.status = RESPONSE_STATUS.ERROR;
                json_result.message = " 新增失敗";
                //return "-4";
            }
            //Response.Write("<script>alert('新增失敗');window.location.href='../BabyBorn/BabyEntr_List';</script>");
            return Content(JsonConvert.SerializeObject(json_result), "application/json");

        }
        #endregion

        #region 嬰幼兒入院護理評估單編輯
        /// <summary>
        /// 嬰幼兒入院護理評估單編輯
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Upd_BabyEntr(FormCollection form)
        {
            RESPONSE_MSG json_result = new RESPONSE_MSG();
            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;
            string save_type = string.Empty;
            string[] id_list = form["IID"].Split(',');
            link.DBOpen();
            List<DBItem> insertDataList = new List<DBItem>();
            int erow = 0;
            var dateNow = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            for (int v = 0; v < id_list.Length; v++)
            {
                insertDataList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDTIME", dateNow, DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("RECORDTIME", dateNow, DBItem.DBDataType.DataTime)); //紀錄時間

                insertDataList.Add(new DBItem("DESCRIPTION", "入院護理評估-嬰幼兒", DBItem.DBDataType.String));
                if (form["STATUS"] == "已完成")
                {
                    save_type = "已完成";
                }
                else
                {
                    save_type = form["TYPE"];
                    insertDataList.Add(new DBItem("STATUS", save_type, DBItem.DBDataType.String));
                }
                insertDataList.Add(new DBItem("BIRTH_PLACE", form["RB_BIRTH_PLACE" + id_list[v]], DBItem.DBDataType.String));


                #region 基本資料
                if (form["TXT_ADM_DAY_DAY" + id_list[v]] != "" && form["TXT_ADM_DAY_DAY" + id_list[v]] != null && form["TXT_ADM_DAY_TIME" + id_list[v]] != "" && form["TXT_ADM_DAY_TIME" + id_list[v]] != null)
                    insertDataList.Add(new DBItem("ADM_DAY", form["TXT_ADM_DAY_DAY" + id_list[v]] + " " + form["TXT_ADM_DAY_TIME" + id_list[v]], DBItem.DBDataType.DataTime));

                insertDataList.Add(new DBItem("FROM_WHERE", form["RB_FROM_WHERE" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PROCESS", form["TXT_PROCESS" + id_list[v]], DBItem.DBDataType.String));
                if (form["TXT_BIRTH_DAY" + id_list[v]] != "" && form["TXT_BIRTH_DAY" + id_list[v]] != null && form["TXT_BIRTH_TIME" + id_list[v]] != "" && form["TXT_BIRTH_TIME" + id_list[v]] != null)
                    insertDataList.Add(new DBItem("BIRTH", form["TXT_BIRTH_DAY" + id_list[v]] + " " + form["TXT_BIRTH_TIME" + id_list[v]], DBItem.DBDataType.DataTime));

                insertDataList.Add(new DBItem("PARITY", form["TXT_PARITY" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("GEST_M", form["TXT_GEST_M" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("GEST_D", form["TXT_GEST_D" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("WEIGHT", form["TXT_WEIGHT" + id_list[v]], DBItem.DBDataType.String));

                var BIRTH_TYPE = form["RB_BIRTH_TYPE" + id_list[v]];
                insertDataList.Add(new DBItem("BIRTH_TYPE", BIRTH_TYPE, DBItem.DBDataType.String));
                if (BIRTH_TYPE == "2")
                    insertDataList.Add(new DBItem("BIRTH_TYPE_OTH", form["TXT_BIRTH_TYPE_OTH" + id_list[v]], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("BIRTH_TYPE_OTH", null, DBItem.DBDataType.String));

                var BIRTH_CON = form["RB_BIRTH_CON" + id_list[v]];
                insertDataList.Add(new DBItem("BIRTH_CON", BIRTH_CON, DBItem.DBDataType.String));
                if (BIRTH_CON == "4")
                    insertDataList.Add(new DBItem("BIRTH_CON_OTH", form["TXT_BIRTH_CON_OTH" + id_list[v]], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("BIRTH_CON_OTH", null, DBItem.DBDataType.String));

                #region 過敏史藥物
                var DRUG_ALL = form["RB_DRUG_ALL" + id_list[v]];
                insertDataList.Add(new DBItem("DRUG_ALL", DRUG_ALL, DBItem.DBDataType.String));
                if (DRUG_ALL == "1")
                {
                    var DRUG_ALL_ITM = form["CK_DRUG_ALL_ITM" + id_list[v]];
                    var DRUG_ALL_ITMV = new List<string>() { "0", "0", "0", "0", "0", "0", "0", "0", "0", "0" };
                    if (DRUG_ALL_ITM == null)
                        insertDataList.Add(new DBItem("DRUG_ALL_ITM", String.Join("", DRUG_ALL_ITMV), DBItem.DBDataType.String));
                    else
                    {
                        var DRUG_ALL_ITM_SP = DRUG_ALL_ITM.Split(',');
                        foreach (var i in DRUG_ALL_ITM_SP)
                            DRUG_ALL_ITMV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("DRUG_ALL_ITM", String.Join("", DRUG_ALL_ITMV), DBItem.DBDataType.String));
                    }

                    //匹林系藥物(pyrin)
                    if (DRUG_ALL_ITMV[1] == "1")
                        insertDataList.Add(new DBItem("DRUG_ALL_ITM1", form["TXT_DRUG_ALL_ITM1" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("DRUG_ALL_ITM1", null, DBItem.DBDataType.String));

                    //非類固醇抗炎藥物(NSAID)
                    if (DRUG_ALL_ITMV[3] == "1")
                        insertDataList.Add(new DBItem("DRUG_ALL_ITM3", form["TXT_DRUG_ALL_ITM3" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("DRUG_ALL_ITM3", null, DBItem.DBDataType.String));

                    //磺氨類
                    if (DRUG_ALL_ITMV[5] == "1")
                        insertDataList.Add(new DBItem("DRUG_ALL_ITM5", form["TXT_DRUG_ALL_ITM5" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("DRUG_ALL_ITM5", null, DBItem.DBDataType.String));

                    //盤尼西林類
                    if (DRUG_ALL_ITMV[6] == "1")
                        insertDataList.Add(new DBItem("DRUG_ALL_ITM6", form["TXT_DRUG_ALL_ITM6" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("DRUG_ALL_ITM6", null, DBItem.DBDataType.String));

                    //抗生素類
                    if (DRUG_ALL_ITMV[7] == "1")
                        insertDataList.Add(new DBItem("DRUG_ALL_ITM7", form["TXT_DRUG_ALL_ITM7" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("DRUG_ALL_ITM7", null, DBItem.DBDataType.String));

                    //麻醉藥
                    if (DRUG_ALL_ITMV[8] == "1")
                        insertDataList.Add(new DBItem("DRUG_ALL_ITM8", form["TXT_DRUG_ALL_ITM8" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("DRUG_ALL_ITM8", null, DBItem.DBDataType.String));

                    //其他
                    if (DRUG_ALL_ITMV[9] == "1")
                        insertDataList.Add(new DBItem("DRUG_ALL_OTH", form["TXT_DRUG_ALL_OTH" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("DRUG_ALL_OTH", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("DRUG_ALL_ITM", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("DRUG_ALL_ITM1", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("DRUG_ALL_ITM3", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("DRUG_ALL_ITM5", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("DRUG_ALL_ITM6", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("DRUG_ALL_ITM7", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("DRUG_ALL_ITM8", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("DRUG_ALL_OTH", null, DBItem.DBDataType.String));
                }

                #endregion

                #region 過敏史食物
                var FOOD_ALL = form["RB_FOOD_ALL" + id_list[v]];
                insertDataList.Add(new DBItem("FOOD_ALL", FOOD_ALL, DBItem.DBDataType.String));
                if (FOOD_ALL == "1")
                {
                    var FOOD_ALL_ITM = form["CK_FOOD_ALL_ITM" + id_list[v]];
                    var FOOD_ALL_ITMV = new List<string>() { "0", "0", "0", "0", "0", "0" };
                    if (FOOD_ALL_ITM == null)
                        insertDataList.Add(new DBItem("FOOD_ALL_ITM", String.Join("", FOOD_ALL_ITMV), DBItem.DBDataType.String));
                    else
                    {
                        var FOOD_ALL_ITM_SP = FOOD_ALL_ITM.Split(',');
                        foreach (var i in FOOD_ALL_ITM_SP)
                            FOOD_ALL_ITMV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("FOOD_ALL_ITM", String.Join("", FOOD_ALL_ITMV), DBItem.DBDataType.String));
                    }

                    //海鮮類
                    if (FOOD_ALL_ITMV[1] == "1")
                        insertDataList.Add(new DBItem("FOOD_ALL_ITM1", form["TXT_FOOD_ALL_ITM1" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("FOOD_ALL_ITM1", null, DBItem.DBDataType.String));

                    //水果
                    if (FOOD_ALL_ITMV[3] == "1")
                        insertDataList.Add(new DBItem("FOOD_ALL_ITM3", form["TXT_FOOD_ALL_ITM3" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("FOOD_ALL_ITM3", null, DBItem.DBDataType.String));

                    //其他
                    if (FOOD_ALL_ITMV[5] == "1")
                        insertDataList.Add(new DBItem("FOOD_ALL_OTH", form["TXT_FOOD_ALL_OTH" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("FOOD_ALL_OTH", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("FOOD_ALL_ITM", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("FOOD_ALL_ITM1", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("FOOD_ALL_ITM3", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("FOOD_ALL_OTH", null, DBItem.DBDataType.String));
                }
                #endregion

                #region 過敏史其他
                var OTH_ALL = form["RB_OTH_ALL" + id_list[v]];
                insertDataList.Add(new DBItem("OTH_ALL", OTH_ALL, DBItem.DBDataType.String));
                if (OTH_ALL == "1")
                {
                    var OTH_ALL_ITM = form["CK_OTH_ALL_ITM" + id_list[v]];
                    var OTH_ALL_ITMV = new List<string>() { "0", "0", "0", "0", "0" };
                    if (OTH_ALL_ITM == null)
                        insertDataList.Add(new DBItem("OTH_ALL_ITM", String.Join("", OTH_ALL_ITMV), DBItem.DBDataType.String));
                    else
                    {
                        var OTH_ALL_ITM_SP = OTH_ALL_ITM.Split(',');
                        foreach (var i in OTH_ALL_ITM_SP)
                            OTH_ALL_ITMV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("OTH_ALL_ITM", String.Join("", OTH_ALL_ITMV), DBItem.DBDataType.String));
                    }

                    if (OTH_ALL_ITMV[4] == "1")
                        insertDataList.Add(new DBItem("OTH_ALL_OTH", form["TXT_OTH_ALL_OTH" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("OTH_ALL_OTH", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("OTH_ALL_ITM", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("OTH_ALL_OTH", null, DBItem.DBDataType.String));
                }

                #endregion

                insertDataList.Add(new DBItem("MAINCARE_N", form["TXT_MAINCARE_N" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MAINCARE_T", form["TXT_MAINCARE_T" + id_list[v]], DBItem.DBDataType.String));

                //父親資料
                insertDataList.Add(new DBItem("MAT_NAME", form["TXT_MAT_NAME" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MAT_AGE", form["TXT_MAT_AGE" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MAT_OCU", form["TXT_MAT_OCU" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MAT_EDU", form["RB_MAT_EDU" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MAT_TEL", form["TXT_MAT_TEL" + id_list[v]], DBItem.DBDataType.String));

                //母親資料
                insertDataList.Add(new DBItem("PAT_NAME", form["TXT_PAT_NAME" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PAT_AGE", form["TXT_PAT_AGE" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PAT_OCU", form["TXT_PAT_OCU" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PAT_EDU", form["RB_PAT_EDU" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PAT_TEL", form["TXT_PAT_TEL" + id_list[v]], DBItem.DBDataType.String));

                insertDataList.Add(new DBItem("SOURCE", form["RB_SOURCE" + id_list[v]], DBItem.DBDataType.String));

                #endregion

                #region 個人病史
                var DISE = form["RB_DISE" + id_list[v]];
                insertDataList.Add(new DBItem("DISE", DISE, DBItem.DBDataType.String));
                if (DISE == "1")
                {
                    var DISE_ITM = form["CK_DISE_ITM" + id_list[v]];
                    var DISE_ITMV = new List<string>() { "0", "0", "0", "0", "0", "0", "0", "0" };
                    if (DISE_ITM == null)
                        insertDataList.Add(new DBItem("DISE_ITM", String.Join("", DISE_ITMV), DBItem.DBDataType.String));
                    else
                    {
                        var DISE_ITM_SP = DISE_ITM.Split(',');
                        foreach (var i in DISE_ITM_SP)
                            DISE_ITMV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("DISE_ITM", String.Join("", DISE_ITMV), DBItem.DBDataType.String));
                    }

                    //感冒
                    if (DISE_ITMV[6] == "1")
                        insertDataList.Add(new DBItem("DISE_ITM_CD", form["TXT_DISE_ITM_CD" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("DISE_ITM_CD", null, DBItem.DBDataType.String));

                    //其他
                    if (DISE_ITMV[7] == "1")
                        insertDataList.Add(new DBItem("DISE_OTH", form["TXT_DISE_OTH" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("DISE_OTH", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("DISE_ITM", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("DISE_ITM_CD", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("DISE_OTH", null, DBItem.DBDataType.String));
                }

                insertDataList.Add(new DBItem("BT", form["RB_BT" + id_list[v]], DBItem.DBDataType.String));

                var BT_ALL = form["RB_BT_ALL" + id_list[v]];
                insertDataList.Add(new DBItem("BT_ALL", BT_ALL, DBItem.DBDataType.String));
                if (BT_ALL == "1")
                    insertDataList.Add(new DBItem("BT_ALL_N", form["TXT_BT_ALL_N" + id_list[v]], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("BT_ALL_N", null, DBItem.DBDataType.String));


                var MED = form["RB_MED" + id_list[v]];
                insertDataList.Add(new DBItem("MED", MED, DBItem.DBDataType.String));
                if (MED == "1")
                    insertDataList.Add(new DBItem("MED_ITM", form["TXT_MED_ITM" + id_list[v]], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("MED_ITM", null, DBItem.DBDataType.String));

                //先天疾患
                var CON_DIS = form["RB_CON_DIS" + id_list[v]];
                insertDataList.Add(new DBItem("CON_DIS", CON_DIS, DBItem.DBDataType.String));
                if (CON_DIS == "1")
                {
                    var CON_DIS_ITM = form["RB_CON_DIS_ITM" + id_list[v]];
                    insertDataList.Add(new DBItem("CON_DIS_ITM", CON_DIS_ITM, DBItem.DBDataType.String));
                    if (CON_DIS_ITM == "2")
                        insertDataList.Add(new DBItem("CON_DIS_OTH", form["TXT_CON_DIS_OTH" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("CON_DIS_OTH", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("CON_DIS_ITM", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CON_DIS_OTH", null, DBItem.DBDataType.String));
                }

                //新生兒代謝疾病篩檢
                var NBSCREEN = form["RB_NBSCREEN" + id_list[v]];
                insertDataList.Add(new DBItem("NBSCREEN", NBSCREEN, DBItem.DBDataType.String));
                if (NBSCREEN == "0")
                    insertDataList.Add(new DBItem("NBSCREEN_N", form["TXT_NBSCREEN_N" + id_list[v]], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("NBSCREEN_N", null, DBItem.DBDataType.String));
                #endregion

                #region 預防接種
                insertDataList.Add(new DBItem("HBIG", form["RB_HBIG" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("HEP_B_1", form["RB_HEP_B_1" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("HEP_B_2", form["RB_HEP_B_2" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FIVE_IN1_1", form["RB_FIVE_IN1_1" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FIVE_IN1_2", form["RB_FIVE_IN1_2" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("SIX_IN1_1", form["RB_SIX_IN1_1" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("SIX_IN1_2", form["RB_SIX_IN1_2" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PCV_1", form["RB_PCV_1" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("PCV_2", form["RB_PCV_2" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ROTA_1", form["RB_ROTA_1" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ROTA_2", form["RB_ROTA_2" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("VAC_OTH", form["TXT_VAC_OTH" + id_list[v]], DBItem.DBDataType.String));
                #endregion

                #region 身體評估
                #region 1.頭部

                insertDataList.Add(new DBItem("ASS_1_1", form["RB_ASS_1_1" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_1_1" + id_list[v]] == "0")
                {
                    for (var param = 1; param <= 5; param++)
                    {
                        List<DBItem> data = new List<DBItem>();
                        data.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                        data.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
                        data.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                        data.Add(new DBItem("RECORDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));

                        var ASS_1_1F = form[$"CK_ASS_1_1F{param}{id_list[v]}"];
                        var ASS_1_1FV = new List<string>() { "0", "0", "0", "0", "0" };

                        if (ASS_1_1F != null)
                        {
                            var ASS_1_1F_SP = ASS_1_1F.Split(',');
                            foreach (var i in ASS_1_1F_SP)
                                ASS_1_1FV[Convert.ToInt32(i)] = "1";
                        }
                        data.Add(new DBItem("ITM_REMARK", String.Join("", ASS_1_1FV), DBItem.DBDataType.String));

                        if (ASS_1_1FV[0] == "1")
                            data.Add(new DBItem("ITM_RED_NO", form[$"TXT_{param}_RED{id_list[v]}"], DBItem.DBDataType.String));
                        else
                            data.Add(new DBItem("ITM_RED_NO", null, DBItem.DBDataType.String));

                        if (ASS_1_1FV[1] == "1")
                            data.Add(new DBItem("ITM_BLI_NO", form[$"TXT_{param}_BLI{id_list[v]}"], DBItem.DBDataType.String));
                        else
                            data.Add(new DBItem("ITM_BLI_NO", null, DBItem.DBDataType.String));

                        if (ASS_1_1FV[2] == "1")
                            data.Add(new DBItem("ITM_HUR_NO", form[$"TXT_{param}_HUR{id_list[v]}"], DBItem.DBDataType.String));
                        else
                            data.Add(new DBItem("ITM_HUR_NO", null, DBItem.DBDataType.String));

                        if (ASS_1_1FV[3] == "1")
                            data.Add(new DBItem("ITM_CS_NO", form[$"TXT_{param}_CS{id_list[v]}"], DBItem.DBDataType.String));
                        else
                            data.Add(new DBItem("ITM_CS_NO", null, DBItem.DBDataType.String));

                        if (ASS_1_1FV[4] == "1")
                            data.Add(new DBItem("ITM_OTH_NO", form[$"TXT_{param}_OTH{id_list[v]}"], DBItem.DBDataType.String));
                        else
                            data.Add(new DBItem("ITM_OTH_NO", null, DBItem.DBDataType.String));

                        string data_where = " IID = '" + id_list[v] + "' ";
                        data_where += "AND SNO = '" + param + "' ";

                        data = setNullToEmpty(data);
                        if (link.DBExecUpdateTns("OBS_ASS_1_1F", data, data_where) < 0)
                        {
                            link.DBRollBack();
                            json_result.status = RESPONSE_STATUS.ERROR;
                            json_result.message = "新增失敗";
                            return Content(JsonConvert.SerializeObject(json_result), "application/json");

                            //return "-5";
                        }
                    }
                }

                insertDataList.Add(new DBItem("ASS_1_2", form["RB_ASS_1_2" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_1_2" + id_list[v]] == "0")
                {
                    var ASS_1_2F1 = form["CK_ASS_1_2F1" + id_list[v]];
                    var ASS_1_2F1V = new List<string>() { "0", "0", "0", "0", "0", "0", "0", "0" };
                    if (ASS_1_2F1 == null)
                        insertDataList.Add(new DBItem("ASS_1_2F1", String.Join("", ASS_1_2F1V), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_1_2F1V_SP = ASS_1_2F1.Split(',');
                        foreach (var i in ASS_1_2F1V_SP)
                            ASS_1_2F1V[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_1_2F1", String.Join("", ASS_1_2F1V), DBItem.DBDataType.String));
                    }
                    if (ASS_1_2F1V[3] == "1")
                        insertDataList.Add(new DBItem("ASS_1_2F2", form["TXT_ASS_1_2F2" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_1_2F2", null, DBItem.DBDataType.String));
                    if (ASS_1_2F1V[7] == "1")
                        insertDataList.Add(new DBItem("ASS_1_2F3", form["TXT_ASS_1_2F3" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_1_2F3", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_1_2F1", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_1_2F2", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_1_2F3", null, DBItem.DBDataType.String));
                }
                #endregion

                #region 2.臉部
                insertDataList.Add(new DBItem("ASS_2", form["RB_ASS_2" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_2" + id_list[v]] == "0")
                {
                    var ASS_2F = form["CK_ASS_2F" + id_list[v]];
                    var ASS_2FV = new List<string>() { "0", "0", "0", "0", "0", "0" };
                    if (ASS_2F == null)
                        insertDataList.Add(new DBItem("ASS_2F", String.Join("", ASS_2FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_2F_SP = ASS_2F.Split(',');
                        foreach (var i in ASS_2F_SP)
                            ASS_2FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_2F", String.Join("", ASS_2FV), DBItem.DBDataType.String));
                    }
                    if (ASS_2FV[5] == "1")
                        insertDataList.Add(new DBItem("ASS_2FO", form["TXT_ASS_2FO" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_2FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_2F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_2FO", null, DBItem.DBDataType.String));
                }

                #endregion

                #region 3.眼睛
                insertDataList.Add(new DBItem("ASS_3", form["RB_ASS_3" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_3" + id_list[v]] == "0")
                {
                    var ASS_3F = form["CK_ASS_3F" + id_list[v]];
                    var ASS_3FV = new List<string>() { "0", "0", "0", "0", "0", "0" };
                    if (ASS_3F == null)
                        insertDataList.Add(new DBItem("ASS_3F", String.Join("", ASS_3FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_3F_SP = ASS_3F.Split(',');
                        foreach (var i in ASS_3F_SP)
                            ASS_3FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_3F", String.Join("", ASS_3FV), DBItem.DBDataType.String));
                    }
                    if (ASS_3FV[5] == "1")
                        insertDataList.Add(new DBItem("ASS_3FO", form["TXT_ASS_3FO" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_3FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_3F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_3FO", null, DBItem.DBDataType.String));
                }

                #endregion

                #region 4.耳朵
                insertDataList.Add(new DBItem("ASS_4", form["RB_ASS_4" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_4" + id_list[v]] == "0")
                {
                    var ASS_4F = form["CK_ASS_4F" + id_list[v]];
                    var ASS_4FV = new List<string>() { "0", "0", "0", "0", "0", "0", "0" };
                    if (ASS_4F == null)
                        insertDataList.Add(new DBItem("ASS_4F", String.Join("", ASS_4FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_4F_SP = ASS_4F.Split(',');
                        foreach (var i in ASS_4F_SP)
                            ASS_4FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_4F", String.Join("", ASS_4FV), DBItem.DBDataType.String));
                    }
                    if (ASS_4FV[6] == "1")
                        insertDataList.Add(new DBItem("ASS_4FO", form["TXT_ASS_4FO" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_4FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_4F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_4FO", null, DBItem.DBDataType.String));
                }
                #endregion

                #region 5.鼻子
                var ASS_5 = form["CK_ASS_5" + id_list[v]];
                var ASS_5V = new List<string>() { "0", "0", "0" };
                if (ASS_5 == null)
                    insertDataList.Add(new DBItem("ASS_5", String.Join("", ASS_5V), DBItem.DBDataType.String));
                else
                {
                    var ASS_5_SP = ASS_5.Split(',');
                    foreach (var i in ASS_5_SP)
                        ASS_5V[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("ASS_5", String.Join("", ASS_5V), DBItem.DBDataType.String));
                }
                if (ASS_5V[2] == "1")
                    insertDataList.Add(new DBItem("ASS_5O", form["TXT_ASS_5O" + id_list[v]], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_5O", null, DBItem.DBDataType.String));
                #endregion

                #region 6.口腔
                insertDataList.Add(new DBItem("ASS_6", form["RB_ASS_6" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_6" + id_list[v]] == "0")
                {
                    var ASS_6F = form["CK_ASS_6F" + id_list[v]];
                    var ASS_6FV = new List<string>() { "0", "0", "0", "0", "0" };
                    if (ASS_6F == null)
                        insertDataList.Add(new DBItem("ASS_6F", String.Join("", ASS_6FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_6F_SP = ASS_6F.Split(',');
                        foreach (var i in ASS_6F_SP)
                            ASS_6FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_6F", String.Join("", ASS_6FV), DBItem.DBDataType.String));
                    }
                    if (ASS_6FV[4] == "1")
                        insertDataList.Add(new DBItem("ASS_6FO", form["TXT_ASS_6FO" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_6FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_6F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_6FO", null, DBItem.DBDataType.String));
                }
                #endregion

                #region 7.頸部
                insertDataList.Add(new DBItem("ASS_7", form["RB_ASS_7" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_7" + id_list[v]] == "0")
                {
                    var ASS_7F = form["CK_ASS_7F" + id_list[v]];
                    var ASS_7FV = new List<string>() { "0", "0", "0", "0", "0", "0", "0" };
                    if (ASS_7F == null)
                        insertDataList.Add(new DBItem("ASS_7F", String.Join("", ASS_7FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_7F_SP = ASS_7F.Split(',');
                        foreach (var i in ASS_7F_SP)
                            ASS_7FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_7F", String.Join("", ASS_7FV), DBItem.DBDataType.String));
                    }
                    if (ASS_7FV[6] == "1")
                        insertDataList.Add(new DBItem("ASS_7FO", form["TXT_ASS_7FO" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_7FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_7F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_7FO", null, DBItem.DBDataType.String));
                }
                #endregion

                #region 8.胸部

                insertDataList.Add(new DBItem("ASS_8_1", form["RB_ASS_8_1" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_8_1" + id_list[v]] == "0")
                {
                    var ASS_8_1F = form["CK_ASS_8_1F" + id_list[v]];
                    var ASS_8_1FV = new List<string>() { "0", "0" };
                    if (ASS_8_1F == null)
                        insertDataList.Add(new DBItem("ASS_8_1F", String.Join("", ASS_8_1FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_8_1F_SP = ASS_8_1F.Split(',');
                        foreach (var i in ASS_8_1F_SP)
                            ASS_8_1FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_8_1F", String.Join("", ASS_8_1FV), DBItem.DBDataType.String));
                    }
                    if (ASS_8_1FV[1] == "1")
                        insertDataList.Add(new DBItem("ASS_8_1FO", form["TXT_ASS_8_1FO" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_8_1FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_8_1F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_8_1FO", null, DBItem.DBDataType.String));
                }

                insertDataList.Add(new DBItem("ASS_8_2", form["RB_ASS_8_2" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_8_2" + id_list[v]] == "0")
                {
                    var ASS_8_2F = form["CK_ASS_8_2F" + id_list[v]];
                    var ASS_8_2FV = new List<string>() { "0", "0", "0", "0" };
                    if (ASS_8_2F == null)
                        insertDataList.Add(new DBItem("ASS_8_2F", String.Join("", ASS_8_2FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_8_2F_SP = ASS_8_2F.Split(',');
                        foreach (var i in ASS_8_2F_SP)
                            ASS_8_2FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_8_2F", String.Join("", ASS_8_2FV), DBItem.DBDataType.String));
                    }
                    if (ASS_8_2FV[0] == "1")
                        insertDataList.Add(new DBItem("ASS_8_2FO", form["TXT_ASS_8_2FO" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_8_2FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_8_2F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_8_2FO", null, DBItem.DBDataType.String));
                }

                insertDataList.Add(new DBItem("ASS_8_3", form["RB_ASS_8_3" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_8_3" + id_list[v]] == "0")
                {
                    var ASS_8_3F = form["CK_ASS_8_3F" + id_list[v]];
                    var ASS_8_3FV = new List<string>() { "0", "0", "0", "0", "0" };
                    if (ASS_8_3F == null)
                        insertDataList.Add(new DBItem("ASS_8_3F", String.Join("", ASS_8_3FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_8_3F_SP = ASS_8_3F.Split(',');
                        foreach (var i in ASS_8_3F_SP)
                            ASS_8_3FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_8_3F", String.Join("", ASS_8_3FV), DBItem.DBDataType.String));
                    }
                }
                else
                    insertDataList.Add(new DBItem("ASS_8_3F", null, DBItem.DBDataType.String));

                insertDataList.Add(new DBItem("ASS_8_4", form["RB_ASS_8_4" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_8_4" + id_list[v]] == "0")
                {
                    var ASS_8_4F = form["CK_ASS_8_4F" + id_list[v]];
                    var ASS_8_4FV = new List<string>() { "0", "0", "0" };
                    if (ASS_8_4F == null)
                        insertDataList.Add(new DBItem("ASS_8_4F", String.Join("", ASS_8_4FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_8_4F_SP = ASS_8_4F.Split(',');
                        foreach (var i in ASS_8_4F_SP)
                            ASS_8_4FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_8_4F", String.Join("", ASS_8_4FV), DBItem.DBDataType.String));
                    }
                    if (ASS_8_4FV[2] == "1")
                        insertDataList.Add(new DBItem("ASS_8_4FO", form["TXT_ASS_8_4FO" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_8_4FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_8_4F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_8_4FO", null, DBItem.DBDataType.String));
                }

                #endregion

                #region 9.腹部
                var ASS_9 = form["CK_ASS_9" + id_list[v]];
                var ASS_9V = new List<string>() { "0", "0", "0", "0" };
                if (ASS_9 == null)
                    insertDataList.Add(new DBItem("ASS_9", String.Join("", ASS_9V), DBItem.DBDataType.String));
                else
                {
                    var ASS_9_SP = ASS_9.Split(',');
                    foreach (var i in ASS_9_SP)
                        ASS_9V[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("ASS_9", String.Join("", ASS_9V), DBItem.DBDataType.String));
                }

                if (ASS_9V[3] == "1")
                    insertDataList.Add(new DBItem("ASS_9O", form["TXT_ASS_9O" + id_list[v]], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_9O", null, DBItem.DBDataType.String));

                insertDataList.Add(new DBItem("ASS_9_1", form["RB_ASS_9_1" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_9_1" + id_list[v]] == "1")
                {
                    var ASS_9_1F = form["CK_ASS_9_1F" + id_list[v]];
                    var ASS_9_1FV = new List<string>() { "0", "0", "0", "0" };
                    if (ASS_9_1F == null)
                        insertDataList.Add(new DBItem("ASS_9_1F", String.Join("", ASS_9_1FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_9_1F_SP = ASS_9_1F.Split(',');
                        foreach (var i in ASS_9_1F_SP)
                            ASS_9_1FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_9_1F", String.Join("", ASS_9_1FV), DBItem.DBDataType.String));
                    }
                    if (ASS_9_1FV[3] == "1")
                        insertDataList.Add(new DBItem("ASS_9_1FO", form["TXT_ASS_9_1FO" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_9_1FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_9_1F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_9_1FO", null, DBItem.DBDataType.String));
                }
                #endregion

                #region 10.臍帶
                var ASS_10 = form["CK_ASS_10" + id_list[v]];
                var ASS_10V = new List<string>() { "0", "0", "0", "0", "0", "0", "0" };
                if (ASS_10 == null)
                    insertDataList.Add(new DBItem("ASS_10", String.Join("", ASS_10V), DBItem.DBDataType.String));
                else
                {
                    var ASS_10_SP = ASS_10.Split(',');
                    foreach (var i in ASS_10_SP)
                        ASS_10V[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("ASS_10", String.Join("", ASS_10V), DBItem.DBDataType.String));
                }

                if (ASS_10V[6] == "1")
                    insertDataList.Add(new DBItem("ASS_10O", form["TXT_ASS_10O" + id_list[v]], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_10O", null, DBItem.DBDataType.String));
                #endregion

                #region 11.臀背部
                insertDataList.Add(new DBItem("ASS_11", form["RB_ASS_11" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_11" + id_list[v]] == "0")
                {
                    var ASS_11F = form["CK_ASS_11F" + id_list[v]];
                    var ASS_11FV = new List<string>() { "0", "0", "0", "0" };
                    if (ASS_11F == null)
                        insertDataList.Add(new DBItem("ASS_11F", String.Join("", ASS_11FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_11F_SP = ASS_11F.Split(',');
                        foreach (var i in ASS_11F_SP)
                            ASS_11FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_11F", String.Join("", ASS_11FV), DBItem.DBDataType.String));
                    }
                    if (ASS_11FV[3] == "1")
                        insertDataList.Add(new DBItem("ASS_11FO", form["TXT_ASS_11FO" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_11FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_11F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_11FO", null, DBItem.DBDataType.String));
                }

                #endregion

                #region 12.泌尿生殖器外觀
                if (ptinfo.PatientGender == "男")
                    insertDataList.Add(new DBItem("ASS_12", "0", DBItem.DBDataType.String));
                else if (ptinfo.PatientGender == "女")
                    insertDataList.Add(new DBItem("ASS_12", "1", DBItem.DBDataType.String));

                if (ptinfo.PatientGender == "男")
                {//男
                    insertDataList.Add(new DBItem("ASS_12_1", form["RB_ASS_12_1" + id_list[v]], DBItem.DBDataType.String));
                    if (form["RB_ASS_12_1" + id_list[v]] == "0")
                    {
                        var ASS_12_1F = form["CK_ASS_12_1F" + id_list[v]];
                        var ASS_12_1FV = new List<string>() { "0", "0" };
                        if (ASS_12_1F == null)
                            insertDataList.Add(new DBItem("ASS_12_1F", String.Join("", ASS_12_1FV), DBItem.DBDataType.String));
                        else
                        {
                            var ASS_12_1F_SP = ASS_12_1F.Split(',');
                            foreach (var i in ASS_12_1F_SP)
                                ASS_12_1FV[Convert.ToInt32(i)] = "1";
                            insertDataList.Add(new DBItem("ASS_12_1F", String.Join("", ASS_12_1FV), DBItem.DBDataType.String));
                        }
                    }
                    else
                        insertDataList.Add(new DBItem("ASS_12_1F", null, DBItem.DBDataType.String));

                    insertDataList.Add(new DBItem("ASS_12_2", form["RB_ASS_12_2" + id_list[v]], DBItem.DBDataType.String));
                    if (form["RB_ASS_12_2" + id_list[v]] == "0")
                    {
                        var ASS_12_2F = form["CK_ASS_12_2F" + id_list[v]];
                        var ASS_12_2FV = new List<string>() { "0", "0", "0" };
                        if (ASS_12_2F == null)
                            insertDataList.Add(new DBItem("ASS_12_2F", String.Join("", ASS_12_2FV), DBItem.DBDataType.String));
                        else
                        {
                            var ASS_12_2F_SP = ASS_12_2F.Split(',');
                            foreach (var i in ASS_12_2F_SP)
                                ASS_12_2FV[Convert.ToInt32(i)] = "1";
                            insertDataList.Add(new DBItem("ASS_12_2F", String.Join("", ASS_12_2FV), DBItem.DBDataType.String));
                        }

                        if (ASS_12_2FV[2] == "1")
                            insertDataList.Add(new DBItem("ASS_12_2FO", form["TXT_ASS_12_2FO" + id_list[v]], DBItem.DBDataType.String));
                        else
                            insertDataList.Add(new DBItem("ASS_12_2FO", null, DBItem.DBDataType.String));
                    }
                    else
                    {
                        insertDataList.Add(new DBItem("ASS_12_2F", null, DBItem.DBDataType.String));
                        insertDataList.Add(new DBItem("ASS_12_2FO", null, DBItem.DBDataType.String));
                    }

                    insertDataList.Add(new DBItem("ASS_12_3", form["RB_ASS_12_3" + id_list[v]], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_12_4", form["TXT_ASS_12_4" + id_list[v]], DBItem.DBDataType.String));

                    insertDataList.Add(new DBItem("ASS_12_5", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_12_6", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_12_7", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_12_7F", null, DBItem.DBDataType.String));
                }
                else if (ptinfo.PatientGender == "女")
                {//女

                    insertDataList.Add(new DBItem("ASS_12_5", form["RB_ASS_12_5" + id_list[v]], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_12_6", form["RB_ASS_12_6" + id_list[v]], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_12_7", form["RB_ASS_12_7" + id_list[v]], DBItem.DBDataType.String));
                    if (form["RB_ASS_12_7" + id_list[v]] == "1")
                    {
                        var ASS_12_7F = form["CK_ASS_12_7F" + id_list[v]];
                        var ASS_12_7FV = new List<string>() { "0", "0", "0" };
                        if (ASS_12_7F == null)
                            insertDataList.Add(new DBItem("ASS_12_7F", String.Join("", ASS_12_7FV), DBItem.DBDataType.String));
                        else
                        {
                            var ASS_12_7F_SP = ASS_12_7F.Split(',');
                            foreach (var i in ASS_12_7F_SP)
                                ASS_12_7FV[Convert.ToInt32(i)] = "1";
                            insertDataList.Add(new DBItem("ASS_12_7F", String.Join("", ASS_12_7FV), DBItem.DBDataType.String));
                        }
                    }
                    else
                        insertDataList.Add(new DBItem("ASS_12_7F", null, DBItem.DBDataType.String));

                    insertDataList.Add(new DBItem("ASS_12_1", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_12_1F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_12_2", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_12_2F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_12_2FO", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_12_3", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_12_4", null, DBItem.DBDataType.String));
                }

                #region 肛門
                insertDataList.Add(new DBItem("ASS_12_8", form["RB_ASS_12_8" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_12_8" + id_list[v]] == "0")
                {
                    var ASS_12_8F = form["CK_ASS_12_8F" + id_list[v]];
                    var ASS_12_8FV = new List<string>() { "0", "0", "0", "0", "0" };
                    if (ASS_12_8F == null)
                        insertDataList.Add(new DBItem("ASS_12_8F", String.Join("", ASS_12_8FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_12_8F_SP = ASS_12_8F.Split(',');
                        foreach (var i in ASS_12_8F_SP)
                            ASS_12_8FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_12_8F", String.Join("", ASS_12_8FV), DBItem.DBDataType.String));
                    }

                    if (ASS_12_8FV[4] == "1")
                        insertDataList.Add(new DBItem("ASS_12_8FO", form["TXT_ASS_12_8FO" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_12_8FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_12_8F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_12_8FO", null, DBItem.DBDataType.String));
                }
                #endregion
                #endregion

                #region 13.骨骼
                insertDataList.Add(new DBItem("ASS_13_1", form["RB_ASS_13_1" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_13_1" + id_list[v]] == "1")
                {
                    var ASS_13_1F = form["CK_ASS_13_1F" + id_list[v]];
                    var ASS_13_1FV = new List<string>() { "0", "0", "0" };
                    if (ASS_13_1F == null)
                        insertDataList.Add(new DBItem("ASS_13_1F", String.Join("", ASS_13_1FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_13_1F_SP = ASS_13_1F.Split(',');
                        foreach (var i in ASS_13_1F_SP)
                            ASS_13_1FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_13_1F", String.Join("", ASS_13_1FV), DBItem.DBDataType.String));
                    }
                    if (ASS_13_1FV[2] == "1")
                        insertDataList.Add(new DBItem("ASS_13_1FO", form["TXT_ASS_13_1FO" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_13_1FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_13_1F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_13_1FO", null, DBItem.DBDataType.String));
                }
                #endregion

                #region 14.哭聲
                insertDataList.Add(new DBItem("ASS_14", form["RB_ASS_14" + id_list[v]], DBItem.DBDataType.String));
                #endregion

                #region 15.活動力
                var ASS_15 = form["CK_ASS_15" + id_list[v]];
                var ASS_15V = new List<string>() { "0", "0", "0", "0", "0", "0" };
                if (ASS_15 == null)
                    insertDataList.Add(new DBItem("ASS_15", String.Join("", ASS_15V), DBItem.DBDataType.String));
                else
                {
                    var ASS_15_SP = ASS_15.Split(',');
                    foreach (var i in ASS_15_SP)
                        ASS_15V[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("ASS_15", String.Join("", ASS_15V), DBItem.DBDataType.String));
                }
                if (ASS_15V[5] == "1")
                    insertDataList.Add(new DBItem("ASS_15O", form["TXT_ASS_15O" + id_list[v]], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_15O", null, DBItem.DBDataType.String));
                #endregion

                #region 16.皮膚
                var ASS_16_1 = form["CK_ASS_16_1" + id_list[v]];
                var ASS_16_1V = new List<string>() { "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0" };
                if (ASS_16_1 == null)
                    insertDataList.Add(new DBItem("ASS_16_1", String.Join("", ASS_16_1V), DBItem.DBDataType.String));
                else
                {
                    var ASS_16_1SP = ASS_16_1.Split(',');
                    foreach (var i in ASS_16_1SP)
                        ASS_16_1V[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("ASS_16_1", String.Join("", ASS_16_1V), DBItem.DBDataType.String));
                }
                if (ASS_16_1V[8] == "1")
                    insertDataList.Add(new DBItem("ASS_16_11", form["TXT_ASS_16_11" + id_list[v]], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_16_11", null, DBItem.DBDataType.String));

                if (ASS_16_1V[9] == "1")
                    insertDataList.Add(new DBItem("ASS_16_12", form["TXT_ASS_16_12" + id_list[v]], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_16_12", null, DBItem.DBDataType.String));

                if (ASS_16_1V[10] == "1")
                    insertDataList.Add(new DBItem("ASS_16O", form["TXT_ASS_16O" + id_list[v]], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("ASS_16O", null, DBItem.DBDataType.String));

                #region 血管瘤
                insertDataList.Add(new DBItem("ASS_16_2", form["RB_ASS_16_2" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_16_2" + id_list[v]] == "1")
                {
                    insertDataList.Add(new DBItem("ASS_16_2P", form["TXT_ASS_16_2P" + id_list[v]], DBItem.DBDataType.String));

                    //var ASS_16_2F = form["CK_ASS_16_2F" + id_list[v]];
                    //var ASS_16_2FV = new List<string>() { "0", "0", "0" };
                    //if (ASS_16_2F == null)
                    //    insertDataList.Add(new DBItem("ASS_16_2F", String.Join("", ASS_16_2FV), DBItem.DBDataType.String));
                    //else
                    //{
                    //    var ASS_16_2F_SP = ASS_16_2F.Split(',');
                    //    foreach (var i in ASS_16_2F_SP)
                    //        ASS_16_2FV[Convert.ToInt32(i)] = "1";
                    //    insertDataList.Add(new DBItem("ASS_16_2F", String.Join("", ASS_16_2FV), DBItem.DBDataType.String));
                    //}

                    //if (ASS_16_2FV[0] == "1")
                    //    insertDataList.Add(new DBItem("ASS_16_2F1", form["TXT_ASS_16_2F1" + id_list[v]], DBItem.DBDataType.String));
                    //else
                    //    insertDataList.Add(new DBItem("ASS_16_2F1", null, DBItem.DBDataType.String));

                    //if (ASS_16_2FV[1] == "1")
                    //    insertDataList.Add(new DBItem("ASS_16_2F2", form["TXT_ASS_16_2F2" + id_list[v]], DBItem.DBDataType.String));
                    //else
                    //    insertDataList.Add(new DBItem("ASS_16_2F2", null, DBItem.DBDataType.String));

                    //if (ASS_16_2FV[2] == "1")
                    //    insertDataList.Add(new DBItem("ASS_16_2F3", form["TXT_ASS_16_2F3" + id_list[v]], DBItem.DBDataType.String));
                    //else
                    //    insertDataList.Add(new DBItem("ASS_16_2F3", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_16_2P", null, DBItem.DBDataType.String));
                    //insertDataList.Add(new DBItem("ASS_16_2F", null, DBItem.DBDataType.String));
                    //insertDataList.Add(new DBItem("ASS_16_2F1", null, DBItem.DBDataType.String));
                    //insertDataList.Add(new DBItem("ASS_16_2F2", null, DBItem.DBDataType.String));
                    //insertDataList.Add(new DBItem("ASS_16_2F3", null, DBItem.DBDataType.String));
                }
                #endregion

                #region 胎記
                insertDataList.Add(new DBItem("ASS_16_3", form["RB_ASS_16_3" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_16_3" + id_list[v]] == "1")
                {
                    var ASS_16_3F = form["CK_ASS_16_3F" + id_list[v]];
                    var ASS_16_3FV = new List<string>() { "0", "0", "0", "0" };
                    if (ASS_16_3F == null)
                        insertDataList.Add(new DBItem("ASS_16_3F", String.Join("", ASS_16_3FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_16_3F_SP = ASS_16_3F.Split(',');
                        foreach (var i in ASS_16_3F_SP)
                            ASS_16_3FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_16_3F", String.Join("", ASS_16_3FV), DBItem.DBDataType.String));
                    }

                    if (ASS_16_3FV[0] == "1")
                        insertDataList.Add(new DBItem("ASS_16_3F1", form["TXT_ASS_16_3F1" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_16_3F1", null, DBItem.DBDataType.String));

                    if (ASS_16_3FV[1] == "1")
                        insertDataList.Add(new DBItem("ASS_16_3F2", form["TXT_ASS_16_3F2" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_16_3F2", null, DBItem.DBDataType.String));

                    if (ASS_16_3FV[2] == "1")
                        insertDataList.Add(new DBItem("ASS_16_3F3", form["TXT_ASS_16_3F3" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_16_3F3", null, DBItem.DBDataType.String));

                    if (ASS_16_3FV[3] == "1")
                        insertDataList.Add(new DBItem("ASS_16_3FO", form["TXT_ASS_16_3FO" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_16_3FO", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_16_3F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_3F1", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_3F2", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_3F3", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_16_3FO", null, DBItem.DBDataType.String));
                }

                #endregion
                #endregion

                #region 17.四肢
                insertDataList.Add(new DBItem("ASS_17", form["RB_ASS_17" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_17" + id_list[v]] == "0")
                {
                    #region 四肢 - 足內翻
                    insertDataList.Add(new DBItem("ASS_17_1", form["RB_ASS_17_1" + id_list[v]], DBItem.DBDataType.String));
                    if (form["RB_ASS_17_1" + id_list[v]] == "1")
                    {
                        var ASS_17_1F = form["CK_ASS_17_1F" + id_list[v]];
                        var ASS_17_1FV = new List<string>() { "0", "0" };
                        if (ASS_17_1F == null)
                            insertDataList.Add(new DBItem("ASS_17_1F", String.Join("", ASS_17_1FV), DBItem.DBDataType.String));
                        else
                        {
                            var ASS_17_1F_SP = ASS_17_1F.Split(',');
                            foreach (var i in ASS_17_1F_SP)
                                ASS_17_1FV[Convert.ToInt32(i)] = "1";
                            insertDataList.Add(new DBItem("ASS_17_1F", String.Join("", ASS_17_1FV), DBItem.DBDataType.String));
                        }
                    }
                    else
                        insertDataList.Add(new DBItem("ASS_17_1F", null, DBItem.DBDataType.String));
                    #endregion

                    #region 四肢 - 足外翻
                    insertDataList.Add(new DBItem("ASS_17_2", form["RB_ASS_17_2" + id_list[v]], DBItem.DBDataType.String));
                    if (form["RB_ASS_17_2" + id_list[v]] == "1")
                    {
                        var ASS_17_2F = form["CK_ASS_17_2F" + id_list[v]];
                        var ASS_17_2FV = new List<string>() { "0", "0" };
                        if (ASS_17_2F == null)
                            insertDataList.Add(new DBItem("ASS_17_2F", String.Join("", ASS_17_2FV), DBItem.DBDataType.String));
                        else
                        {
                            var ASS_17_2F_SP = ASS_17_2F.Split(',');
                            foreach (var i in ASS_17_2F_SP)
                                ASS_17_2FV[Convert.ToInt32(i)] = "1";
                            insertDataList.Add(new DBItem("ASS_17_2F", String.Join("", ASS_17_2FV), DBItem.DBDataType.String));
                        }
                    }
                    else
                        insertDataList.Add(new DBItem("ASS_17_2F", null, DBItem.DBDataType.String));
                    #endregion

                    #region 四肢 - 單一手紋(斷掌)
                    insertDataList.Add(new DBItem("ASS_17_3", form["RB_ASS_17_3" + id_list[v]], DBItem.DBDataType.String));
                    if (form["RB_ASS_17_3" + id_list[v]] == "1")
                    {
                        var ASS_17_3F = form["CK_ASS_17_3F" + id_list[v]];
                        var ASS_17_3FV = new List<string>() { "0", "0" };
                        if (ASS_17_3F == null)
                            insertDataList.Add(new DBItem("ASS_17_3F", String.Join("", ASS_17_3FV), DBItem.DBDataType.String));
                        else
                        {
                            var ASS_17_3F_SP = ASS_17_3F.Split(',');
                            foreach (var i in ASS_17_3F_SP)
                                ASS_17_3FV[Convert.ToInt32(i)] = "1";
                            insertDataList.Add(new DBItem("ASS_17_3F", String.Join("", ASS_17_3FV), DBItem.DBDataType.String));
                        }
                    }
                    else
                        insertDataList.Add(new DBItem("ASS_17_3F", null, DBItem.DBDataType.String));
                    #endregion
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_17_1", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_17_1F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_17_2", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_17_2F", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_17_3", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_17_3F", null, DBItem.DBDataType.String));
                }


                #region 四肢 - 活動力
                insertDataList.Add(new DBItem("ASS_17_4", form["RB_ASS_17_4" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_17_4" + id_list[v]] == "0")
                {
                    var ASS_17_4F = form["CK_ASS_17_4F" + id_list[v]];
                    var ASS_17_4FV = new List<string>() { "0", "0", "0", "0" };
                    if (ASS_17_4F == null)
                        insertDataList.Add(new DBItem("ASS_17_4F", String.Join("", ASS_17_4FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_17_4F_SP = ASS_17_4F.Split(',');
                        foreach (var i in ASS_17_4F_SP)
                            ASS_17_4FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_17_4F", String.Join("", ASS_17_4FV), DBItem.DBDataType.String));
                    }
                }
                else
                    insertDataList.Add(new DBItem("ASS_17_4F", null, DBItem.DBDataType.String));
                #endregion

                #region 四肢 - 水腫
                insertDataList.Add(new DBItem("ASS_17_5", form["RB_ASS_17_5" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_17_5" + id_list[v]] == "1")
                {
                    var ASS_17_5F = form["CK_ASS_17_5F" + id_list[v]];
                    var ASS_17_5FV = new List<string>() { "0", "0", "0", "0" };
                    if (ASS_17_5F == null)
                        insertDataList.Add(new DBItem("ASS_17_5F", String.Join("", ASS_17_5FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_17_5F_SP = ASS_17_5F.Split(',');
                        foreach (var i in ASS_17_5F_SP)
                            ASS_17_5FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_17_5F", String.Join("", ASS_17_5FV), DBItem.DBDataType.String));
                    }
                }
                else
                    insertDataList.Add(new DBItem("ASS_17_5F", null, DBItem.DBDataType.String));
                #endregion

                #region 四肢 - 麻痺
                insertDataList.Add(new DBItem("ASS_17_6", form["RB_ASS_17_6" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_17_6" + id_list[v]] == "1")
                {
                    var ASS_17_6F = form["CK_ASS_17_6F" + id_list[v]];
                    var ASS_17_6FV = new List<string>() { "0", "0", "0", "0" };
                    if (ASS_17_6F == null)
                        insertDataList.Add(new DBItem("ASS_17_6F", String.Join("", ASS_17_6FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_17_6F_SP = ASS_17_6F.Split(',');
                        foreach (var i in ASS_17_6F_SP)
                            ASS_17_6FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_17_6F", String.Join("", ASS_17_6FV), DBItem.DBDataType.String));
                    }
                }
                else
                    insertDataList.Add(new DBItem("ASS_17_6F", null, DBItem.DBDataType.String));
                #endregion

                #region 四肢 - 手指
                insertDataList.Add(new DBItem("ASS_17_7", form["RB_ASS_17_7" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_17_7" + id_list[v]] == "0")
                {
                    var ASS_17_7F = form["CK_ASS_17_7F" + id_list[v]];
                    var ASS_17_7FV = new List<string>() { "0", "0", "0", "0", "0", "0", "0", "0" };
                    if (ASS_17_7F == null)
                        insertDataList.Add(new DBItem("ASS_17_7F", String.Join("", ASS_17_7FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_17_7F_SP = ASS_17_7F.Split(',');
                        foreach (var i in ASS_17_7F_SP)
                            ASS_17_7FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_17_7F", String.Join("", ASS_17_7FV), DBItem.DBDataType.String));
                    }
                    if (ASS_17_7FV[0] == "1")
                        insertDataList.Add(new DBItem("ASS_17_7F1", form["TXT_ASS_17_7F1" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_17_7F1", null, DBItem.DBDataType.String));

                    if (ASS_17_7FV[1] == "1")
                        insertDataList.Add(new DBItem("ASS_17_7F2", form["TXT_ASS_17_7F2" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_17_7F2", null, DBItem.DBDataType.String));

                    if (ASS_17_7FV[2] == "1")
                        insertDataList.Add(new DBItem("ASS_17_7F3", form["TXT_ASS_17_7F3" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_17_7F3", null, DBItem.DBDataType.String));

                    if (ASS_17_7FV[3] == "1")
                        insertDataList.Add(new DBItem("ASS_17_7F4", form["TXT_ASS_17_7F4" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_17_7F4", null, DBItem.DBDataType.String));

                    if (ASS_17_7FV[4] == "1")
                        insertDataList.Add(new DBItem("ASS_17_7F5", form["TXT_ASS_17_7F5" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_17_7F5", null, DBItem.DBDataType.String));

                    if (ASS_17_7FV[5] == "1")
                        insertDataList.Add(new DBItem("ASS_17_7F6", form["TXT_ASS_17_7F6" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_17_7F6", null, DBItem.DBDataType.String));

                    if (ASS_17_7FV[6] == "1")
                        insertDataList.Add(new DBItem("ASS_17_7F7", form["TXT_ASS_17_7F7" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_17_7F7", null, DBItem.DBDataType.String));

                    if (ASS_17_7FV[7] == "1")
                        insertDataList.Add(new DBItem("ASS_17_7F8", form["TXT_ASS_17_7F8" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_17_7F8", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_17_7F1", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_17_7F2", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_17_7F3", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_17_7F4", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_17_7F5", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_17_7F6", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_17_7F7", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_17_7F8", null, DBItem.DBDataType.String));
                }
                #endregion

                #region 四肢 - 腳趾
                insertDataList.Add(new DBItem("ASS_17_8", form["RB_ASS_17_8" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_ASS_17_8" + id_list[v]] == "0")
                {
                    var ASS_17_8F = form["CK_ASS_17_8F" + id_list[v]];
                    var ASS_17_8FV = new List<string>() { "0", "0", "0", "0", "0", "0" };
                    if (ASS_17_8F == null)
                        insertDataList.Add(new DBItem("ASS_17_8F", String.Join("", ASS_17_8FV), DBItem.DBDataType.String));
                    else
                    {
                        var ASS_17_8F_SP = ASS_17_8F.Split(',');
                        foreach (var i in ASS_17_8F_SP)
                            ASS_17_8FV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("ASS_17_8F", String.Join("", ASS_17_8FV), DBItem.DBDataType.String));
                    }
                    if (ASS_17_8FV[0] == "1")
                        insertDataList.Add(new DBItem("ASS_17_8F1", form["TXT_ASS_17_8F1" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_17_8F1", null, DBItem.DBDataType.String));

                    if (ASS_17_8FV[1] == "1")
                        insertDataList.Add(new DBItem("ASS_17_8F2", form["TXT_ASS_17_8F2" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_17_8F2", null, DBItem.DBDataType.String));

                    if (ASS_17_8FV[2] == "1")
                        insertDataList.Add(new DBItem("ASS_17_8F3", form["TXT_ASS_17_8F3" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_17_8F3", null, DBItem.DBDataType.String));

                    if (ASS_17_8FV[3] == "1")
                        insertDataList.Add(new DBItem("ASS_17_8F4", form["TXT_ASS_17_8F4" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_17_8F4", null, DBItem.DBDataType.String));

                    if (ASS_17_8FV[4] == "1")
                        insertDataList.Add(new DBItem("ASS_17_8F5", form["TXT_ASS_17_8F5" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_17_8F5", null, DBItem.DBDataType.String));

                    if (ASS_17_8FV[5] == "1")
                        insertDataList.Add(new DBItem("ASS_17_8F6", form["TXT_ASS_17_8F6" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("ASS_17_8F6", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("ASS_17_8F1", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_17_8F2", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_17_8F3", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_17_8F4", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_17_8F5", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("ASS_17_8F6", null, DBItem.DBDataType.String));
                }
                #endregion

                insertDataList.Add(new DBItem("ASS_17_9", form["RB_ASS_17_9" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_17_10", form["RB_ASS_17_10" + id_list[v]], DBItem.DBDataType.String));
                #endregion

                #region 18.反射
                insertDataList.Add(new DBItem("ASS_18_1", form["RB_ASS_18_1" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_18_2", form["RB_ASS_18_2" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_18_3", form["RB_ASS_18_3" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("ASS_18_4", form["RB_ASS_18_4" + id_list[v]], DBItem.DBDataType.String));

                #endregion
                #endregion

                #region 進食情形
                insertDataList.Add(new DBItem("FEED", form["RB_FEED" + id_list[v]], DBItem.DBDataType.String));
                var FEED_K = form["CK_FEED_K" + id_list[v]];
                var FEED_KV = new List<string>() { "0", "0" };
                if (FEED_K == null)
                    insertDataList.Add(new DBItem("FEED_K", String.Join("", FEED_KV), DBItem.DBDataType.String));
                else
                {
                    var FEED_K_SP = FEED_K.Split(',');
                    foreach (var i in FEED_K_SP)
                        FEED_KV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("FEED_K", String.Join("", FEED_KV), DBItem.DBDataType.String));
                }

                if (FEED_KV[1] == "1")
                {
                    insertDataList.Add(new DBItem("FEED_MB", form["TXT_FEED_MB" + id_list[v]], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("FEED_MF", form["TXT_FEED_MF" + id_list[v]], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("FEED_MA", form["TXT_FEED_MA" + id_list[v]], DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("FEED_MB", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("FEED_MF", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("FEED_MA", null, DBItem.DBDataType.String));
                }

                var VOM = form["RB_VOM" + id_list[v]];
                insertDataList.Add(new DBItem("VOM", VOM, DBItem.DBDataType.String));
                if (VOM == "1")
                {
                    insertDataList.Add(new DBItem("VOM_AMT", form["TXT_VOM_AMT" + id_list[v]], DBItem.DBDataType.String));

                    var VOM_K = form["CK_VOM_K" + id_list[v]];
                    var VOM_KV = new List<string>() { "0", "0", "0", "0", "0" };
                    if (VOM_K == null)
                        insertDataList.Add(new DBItem("VOM_K", String.Join("", VOM_KV), DBItem.DBDataType.String));
                    else
                    {
                        var VOM_K_SP = VOM_K.Split(',');
                        foreach (var i in VOM_K_SP)
                            VOM_KV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("VOM_K", String.Join("", VOM_KV), DBItem.DBDataType.String));
                    }
                }
                else
                {
                    insertDataList.Add(new DBItem("VOM_K", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("VOM_AMT", null, DBItem.DBDataType.String));
                }

                #endregion

                #region 排泄情形
                insertDataList.Add(new DBItem("URI_AMT", form["RB_URI_AMT" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("URI_COL", form["RB_URI_COL" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("DEF_COL", form["RB_DEF_COL" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("DEL_CHA", form["RB_DEL_CHA" + id_list[v]], DBItem.DBDataType.String));
                #endregion

                #region 疼痛評估
                var PAIN_AS = form["RB_PAIN_AS" + id_list[v]];
                insertDataList.Add(new DBItem("PAIN_AS", PAIN_AS, DBItem.DBDataType.String));

                //數字量表
                if (PAIN_AS == "0")
                    insertDataList.Add(new DBItem("PAIN_AS1", form["RB_PAIN_AS1" + id_list[v]], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("PAIN_AS1", null, DBItem.DBDataType.String));

                //臉譜量表
                if (PAIN_AS == "1")
                    insertDataList.Add(new DBItem("PAIN_AS2", form["RB_PAIN_AS2" + id_list[v]], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("PAIN_AS2", null, DBItem.DBDataType.String));

                //困難評估(新生兒)
                if (PAIN_AS == "4")
                {
                    insertDataList.Add(new DBItem("PAIN_AS3_1", form["RB_PAIN_AS3_1" + id_list[v]], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS3_2", form["RB_PAIN_AS3_2" + id_list[v]], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS3_3", form["RB_PAIN_AS3_3" + id_list[v]], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS3_4", form["RB_PAIN_AS3_4" + id_list[v]], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS3_5", form["RB_PAIN_AS3_5" + id_list[v]], DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("PAIN_AS3_1", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS3_2", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS3_3", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS3_4", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS3_5", null, DBItem.DBDataType.String));
                }

                //CPOT評估(加護單位)
                if (PAIN_AS == "5")
                {
                    insertDataList.Add(new DBItem("PAIN_AS4_1", form["RB_PAIN_AS4_1" + id_list[v]], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS4_2", form["RB_PAIN_AS4_2" + id_list[v]], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS4_3", form["RB_PAIN_AS4_3" + id_list[v]], DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS4_4", form["RB_PAIN_AS4_4" + id_list[v]], DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("PAIN_AS4_1", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS4_2", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS4_3", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("PAIN_AS4_4", null, DBItem.DBDataType.String));
                }
                #endregion

                #region 接觸史

                var INF_MOM = form["RB_INF_MOM" + id_list[v]];
                insertDataList.Add(new DBItem("INF_MOM", INF_MOM, DBItem.DBDataType.String));
                if (INF_MOM == "1")
                {
                    var INF_MOM_SYM = form["CK_INF_MOM_SYM" + id_list[v]];
                    var INF_MOM_SYMV = new List<string>() { "0", "0", "0", "0", "0", "0" };
                    if (INF_MOM_SYM == null)
                        insertDataList.Add(new DBItem("INF_MOM_SYM", String.Join("", INF_MOM_SYMV), DBItem.DBDataType.String));
                    else
                    {
                        var INF_MOM_SYM_SP = INF_MOM_SYM.Split(',');
                        foreach (var i in INF_MOM_SYM_SP)
                            INF_MOM_SYMV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("INF_MOM_SYM", String.Join("", INF_MOM_SYMV), DBItem.DBDataType.String));
                    }

                    //其他
                    if (INF_MOM_SYMV[5] == "1")
                        insertDataList.Add(new DBItem("INF_MOM_OTH", form["TXT_INF_MOM_OTH" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("INF_MOM_OTH", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("INF_MOM_SYM", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("INF_MOM_OTH", null, DBItem.DBDataType.String));
                }

                var INF_OTH = form["RB_INF_OTH" + id_list[v]];
                insertDataList.Add(new DBItem("INF_OTH", INF_MOM, DBItem.DBDataType.String));
                if (INF_OTH == "1")
                {
                    insertDataList.Add(new DBItem("INF_OTH_WHO", form["TXT_INF_OTH_WHO" + id_list[v]], DBItem.DBDataType.String));

                    var INF_OTH_SYM = form["CK_INF_OTH_SYM" + id_list[v]];
                    var INF_OTH_SYMV = new List<string>() { "0", "0", "0", "0", "0", "0" };
                    if (INF_OTH_SYM == null)
                        insertDataList.Add(new DBItem("INF_OTH_SYM", String.Join("", INF_OTH_SYMV), DBItem.DBDataType.String));
                    else
                    {
                        var INF_OTH_SYM_SP = INF_OTH_SYM.Split(',');
                        foreach (var i in INF_OTH_SYM_SP)
                            INF_OTH_SYMV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("INF_OTH_SYM", String.Join("", INF_OTH_SYMV), DBItem.DBDataType.String));
                    }

                    //其他
                    if (INF_OTH_SYMV[5] == "1")
                        insertDataList.Add(new DBItem("INF_OTH_OTH", form["TXT_INF_OTH_OTH" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("INF_OTH_OTH", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("INF_OTH_SYM", null, DBItem.DBDataType.String));
                    //insertDataList.Add(new DBItem("INF_OTH_WHO", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("INF_OTH_OTH", null, DBItem.DBDataType.String));
                }

                var BS_CLS = form["RB_BS_CLS" + id_list[v]];
                insertDataList.Add(new DBItem("BS_CLS", BS_CLS, DBItem.DBDataType.String));
                if (BS_CLS == "1")
                {
                    var BS_CLS_RS = form["CK_BS_CLS_RS" + id_list[v]];
                    var BS_CLS_RSV = new List<string>() { "0", "0", "0", "0" };
                    if (BS_CLS_RS == null)
                        insertDataList.Add(new DBItem("BS_CLS_RS", String.Join("", BS_CLS_RSV), DBItem.DBDataType.String));
                    else
                    {
                        var BS_CLS_RS_SP = BS_CLS_RS.Split(',');
                        foreach (var i in BS_CLS_RS_SP)
                            BS_CLS_RSV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("BS_CLS_RS", String.Join("", BS_CLS_RSV), DBItem.DBDataType.String));
                    }

                    if (BS_CLS_RSV[3] == "1")
                        insertDataList.Add(new DBItem("BS_CLS_OTH", form["TXT_BS_CLS_OTH" + id_list[v]], DBItem.DBDataType.String));
                    else
                        insertDataList.Add(new DBItem("BS_CLS_OTH", null, DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("BS_CLS_RS", null, DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("BS_CLS_OTH", null, DBItem.DBDataType.String));
                }

                insertDataList.Add(new DBItem("INF_CARE", form["RB_INF_CARE" + id_list[v]], DBItem.DBDataType.String));
                #endregion

                #region TOCC評估
                //症狀
                var SYM = form["CK_SYM" + id_list[v]];
                var SYMV = new List<string>() { "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0" };
                if (SYM == null)
                    insertDataList.Add(new DBItem("SYM", String.Join("", SYMV), DBItem.DBDataType.String));
                else
                {
                    var SYM_SP = SYM.Split(',');
                    foreach (var i in SYM_SP)
                        SYMV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("SYM", String.Join("", SYMV), DBItem.DBDataType.String));
                }
                if (SYMV[10] == "1")//(其他TXT)
                    insertDataList.Add(new DBItem("SYM_OTH", form["TXT_SYM_OTH" + id_list[v]], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("SYM_OTH", null, DBItem.DBDataType.String));

                //旅遊史
                var TRAV = form["CK_TRAV" + id_list[v]];
                var TRAVV = new List<string>() { "0", "0", "0" };
                if (TRAV == null)
                    insertDataList.Add(new DBItem("TRAV", String.Join("", TRAVV), DBItem.DBDataType.String));
                else
                {
                    var TRAV_SP = TRAV.Split(',');
                    foreach (var i in TRAV_SP)
                        TRAVV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("TRAV", String.Join("", TRAVV), DBItem.DBDataType.String));
                }
                //國內旅遊城市
                insertDataList.Add(new DBItem("TRAV_DOME_CITY", form["TXT_TRAV_DOME_CITY" + id_list[v]], DBItem.DBDataType.String));
                //國內旅遊景點
                insertDataList.Add(new DBItem("TRAV_DOME_VIEW", form["TXT_TRAV_DOME_VIEW" + id_list[v]], DBItem.DBDataType.String));
                //國內旅遊交通
                var TRAV_DOME_TRAF = form["CK_TRAV_DOME_TRAF" + id_list[v]];
                var TRAV_DOME_TRAFV = new List<string>() { "0", "0", "0" };
                if (TRAV_DOME_TRAF == null)
                    insertDataList.Add(new DBItem("TRAV_DOME_TRAF", String.Join("", TRAV_DOME_TRAFV), DBItem.DBDataType.String));
                else
                {
                    var TRAV_DOME_TRAF_SP = TRAV_DOME_TRAF.Split(',');
                    foreach (var i in TRAV_DOME_TRAF_SP)
                        TRAV_DOME_TRAFV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("TRAV_DOME_TRAF", String.Join("", TRAV_DOME_TRAFV), DBItem.DBDataType.String));
                }
                if (TRAV_DOME_TRAFV[2] == "1")//(其他TXT)
                    insertDataList.Add(new DBItem("TRAV_DOME_TRAF_OTH", form["TXT_TRAV_DOME_TRAF_OTH" + id_list[v]], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("TRAV_DOME_TRAF_OTH", null, DBItem.DBDataType.String));
                //國外旅遊國家
                insertDataList.Add(new DBItem("TRAV_ABO_COUN", form["TXT_TRAV_ABO_COUN" + id_list[v]], DBItem.DBDataType.String));
                //國外旅遊目的地
                insertDataList.Add(new DBItem("TRAV_ABO_DEST", form["TXT_TRAV_ABO_DEST" + id_list[v]], DBItem.DBDataType.String));
                //國外旅遊交通方式
                var TRAV_ABO_TRAF = form["CK_TRAV_ABO_TRAF" + id_list[v]];
                var TRAV_ABO_TRAFV = new List<string>() { "0", "0", "0" };
                if (TRAV_ABO_TRAF == null)
                    insertDataList.Add(new DBItem("TRAV_ABO_TRAF", String.Join("", TRAV_ABO_TRAFV), DBItem.DBDataType.String));
                else
                {
                    var TRAV_ABO_TRAF_SP = TRAV_ABO_TRAF.Split(',');
                    foreach (var i in TRAV_ABO_TRAF_SP)
                        TRAV_ABO_TRAFV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("TRAV_ABO_TRAF", String.Join("", TRAV_ABO_TRAFV), DBItem.DBDataType.String));
                }
                if (TRAV_ABO_TRAFV[2] == "1")//(其他TXT)
                    insertDataList.Add(new DBItem("TRAV_ABO_TRAF_OTH", form["TXT_TRAV_ABO_TRAF_OTH" + id_list[v]], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("TRAV_ABO_TRAF_OTH", null, DBItem.DBDataType.String));
                //職業別
                var OCCU = form["CK_OCCU" + id_list[v]];
                var OCCUV = new List<string>() { "0", "0", "0", "0", "0" };
                if (OCCU == null)
                    insertDataList.Add(new DBItem("OCCU", String.Join("", OCCUV), DBItem.DBDataType.String));
                else
                {
                    var OCCU_SP = OCCU.Split(',');
                    foreach (var i in OCCU_SP)
                        OCCUV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("OCCU", String.Join("", OCCUV), DBItem.DBDataType.String));
                }
                if (OCCUV[4] == "1")//(其他TXT)
                    insertDataList.Add(new DBItem("OCCU_OTH", form["TXT_OCCU_OTH" + id_list[v]], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("OCCU_OTH", null, DBItem.DBDataType.String));
                //接觸史
                var CONT = form["CK_CONT" + id_list[v]];
                var CONTV = new List<string>() { "0", "0", "0" };
                if (CONT == null)
                    insertDataList.Add(new DBItem("CONT", String.Join("", CONTV), DBItem.DBDataType.String));
                else
                {
                    var CONT_SP = CONT.Split(',');
                    foreach (var i in CONT_SP)
                        CONTV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("CONT", String.Join("", CONTV), DBItem.DBDataType.String));
                }
                if (CONTV[1] == "1")//(接觸禽鳥類、畜類等)
                {
                    var CONT_BIRD = form["CK_CONT_BIRD" + id_list[v]];
                    var CONT_BIRDV = new List<string>() { "0", "0" };
                    if (CONT_BIRD == null)
                        insertDataList.Add(new DBItem("CONT_BIRD", String.Join("", CONT_BIRDV), DBItem.DBDataType.String));
                    else
                    {
                        var CONT_BIRD_SP = CONT_BIRD.Split(',');
                        foreach (var i in CONT_BIRD_SP)
                            CONT_BIRDV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("CONT_BIRD", String.Join("", CONT_BIRDV), DBItem.DBDataType.String));
                    }
                }
                else
                    insertDataList.Add(new DBItem("CONT_BIRD", null, DBItem.DBDataType.String));

                //嬰幼兒、新生兒接觸史
                //(生產前 14 天內，產婦或同住家人有發燒、腹瀉、咳嗽、流鼻水等疑似感染症狀?)
                insertDataList.Add(new DBItem("CONT_OBS_SYM", form["RB_CONT_OBS_SYM" + id_list[v]], DBItem.DBDataType.String));
                //(生產前 14 天內，寶寶的哥哥、姊姊學校班上同學有因為傳染病請假或班級停課之情形?)
                insertDataList.Add(new DBItem("CONT_OBS_SICKLEAVE", form["RB_CONT_OBS_SICKLEAVE" + id_list[v]], DBItem.DBDataType.String));
                //(住院期間照顧者(應盡量維持同一人)，目前有發燒、腹瀉、咳嗽、流鼻水等疑似感染症狀?)
                insertDataList.Add(new DBItem("CONT_OBS_CARESYM", form["RB_CONT_OBS_CARESYM" + id_list[v]], DBItem.DBDataType.String));

                if (CONTV[2] == "1")//(其他)
                {
                    insertDataList.Add(new DBItem("CONT_OTH", form["TXT_CONT_OTH" + id_list[v]], DBItem.DBDataType.String));
                }
                else
                    insertDataList.Add(new DBItem("CONT_OTH", null, DBItem.DBDataType.String));

                //群聚史
                insertDataList.Add(new DBItem("CLU", form["RB_CLU" + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_CLU" + id_list[v]] == "1")//(家人/朋友/同事有發燒或類流感症狀)
                {
                    var CLU_RELA = form["CK_CLU_RELA" + id_list[v]];
                    var CLU_RELAV = new List<string>() { "0", "0", "0", "0" };
                    if (CLU_RELA == null)
                        insertDataList.Add(new DBItem("CLU_RELA", String.Join("", CLU_RELAV), DBItem.DBDataType.String));
                    else
                    {
                        var CLU_RELA_SP = CLU_RELA.Split(',');
                        foreach (var i in CLU_RELA_SP)
                            CLU_RELAV[Convert.ToInt32(i)] = "1";
                        insertDataList.Add(new DBItem("CLU_RELA", String.Join("", CLU_RELAV), DBItem.DBDataType.String));
                    }
                    if (CLU_RELAV[3] == "1")
                    {
                        insertDataList.Add(new DBItem("CLU_RELA_OTH", form["TXT_CLU_RELA_OTH" + id_list[v]], DBItem.DBDataType.String));
                    }
                    else
                    {
                        insertDataList.Add(new DBItem("CLU_RELA_OTH", null, DBItem.DBDataType.String));
                    }
                }
                else
                {
                    var CLU_RELAV = new List<string>() { "0", "0", "0", "0" };
                    insertDataList.Add(new DBItem("CLU_RELA", String.Join("", CLU_RELAV), DBItem.DBDataType.String));
                    insertDataList.Add(new DBItem("CLU_RELA_OTH", null, DBItem.DBDataType.String));
                }
                #endregion

                var HOSP = form["RB_HOSP" + id_list[v]];
                insertDataList.Add(new DBItem("HOSP", HOSP, DBItem.DBDataType.String));
                var SUR = form["RB_SUR" + id_list[v]];
                insertDataList.Add(new DBItem("SUR", SUR, DBItem.DBDataType.String));

                string where = " IID = '" + id_list[v] + "' ";
                insertDataList = setNullToEmpty(insertDataList);
                erow += link.DBExecUpdateTns("OBS_BABYENTR", insertDataList, where);

                #region 護理紀錄
                var exceptionResult = new List<string>();
                var finalResult = "入院經過：";
                finalResult += $"{(form["TXT_PROCESS" + id_list[v]] == "" ? "未填寫" : form["TXT_PROCESS" + id_list[v]])}。";
                finalResult += "護理評估：";

                #region 頭部-外觀
                var headFlag = false;
                if (form["RB_ASS_1_1" + id_list[v]] == "0")
                {
                    var items = new List<string>();
                    var bone = new List<string>() { "1.頭骨", "2.頂骨", "3.左顳骨", "4.右顳骨", "5.枕骨" };
                    #region 五大判斷
                    for (var i = 1; i <= 5; i++)
                    {
                        var CK_ASS_1_1F = form[$"CK_ASS_1_1F{i}" + id_list[v]];
                        if (IsNoEmpty(CK_ASS_1_1F))
                        {
                            var other = new List<string>();

                            var values = CK_ASS_1_1F.Split(',');
                            if (values.Length > 0)
                                foreach (var val in values)
                                {
                                    if (val == "0" && IsNoEmpty(form[$"TXT_{i}_RED" + id_list[v]]))
                                        other.Add($"紅腫{form[$"TXT_{i}_RED" + id_list[v]]}");

                                    if (val == "1" && IsNoEmpty(form[$"TXT_{i}_BLI" + id_list[v]]))
                                        other.Add($"水泡{form[$"TXT_{i}_BLI" + id_list[v]]}");

                                    if (val == "2" && IsNoEmpty(form[$"TXT_{i}_HUR" + id_list[v]]))
                                        other.Add($"破皮{form[$"TXT_{i}_HUR" + id_list[v]]}");

                                    if (val == "3" && IsNoEmpty(form[$"TXT_{i}_CS" + id_list[v]]))
                                        other.Add($"產瘤{form[$"TXT_{i}_CS" + id_list[v]]}");

                                    if (val == "4" && IsNoEmpty(form[$"TXT_{i}_OTH" + id_list[v]]))
                                        other.Add(form[$"TXT_{i}_OTH" + id_list[v]]);
                                }
                            items.Add($"{bone[i - 1]}：{String.Join("、", other)}");
                        }
                    }
                    #endregion
                    if (items.Count > 0)
                        exceptionResult.Add($"頭部外觀：{String.Join("、", items)}");
                }
                #endregion

                #region 頭部-囪門
                if (form["RB_ASS_1_2" + id_list[v]] == "0")
                {
                    var CK_ASS_1_2F1 = form["CK_ASS_1_2F1" + id_list[v]];
                    if (IsNoEmpty(CK_ASS_1_2F1))
                    {
                        var front = new List<string>();
                        var back = new List<string>();
                        var result = "";

                        var values = CK_ASS_1_2F1.Split(',');
                        if (values.Length > 0)
                            result += $"{(headFlag == false ? "頭部" : "")}囪門：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                front.Add("關閉");
                            if (val == "1")
                                front.Add("凹陷");
                            if (val == "2")
                                front.Add("膨出");
                            if (val == "3" && IsNoEmpty(form["TXT_ASS_1_2F2" + id_list[v]]))
                                front.Add(form["TXT_ASS_1_2F2" + id_list[v]]);

                            if (val == "4")
                                back.Add("關閉");
                            if (val == "5")
                                back.Add("凹陷");
                            if (val == "6")
                                back.Add("膨出");
                            if (val == "7" && IsNoEmpty(form["TXT_ASS_1_2F3" + id_list[v]]))
                                back.Add(form["TXT_ASS_1_2F3" + id_list[v]]);
                        }
                        if (front.Count > 0)
                            result += "前：" + String.Join("、", front);
                        if (back.Count > 0)
                            result += $"{(front.Count > 0 ? "，" : "")}後：{String.Join("、", back)}";

                        exceptionResult.Add(result);
                    }
                }
                #endregion

                #region 臉部
                if (form["RB_ASS_2" + id_list[v]] == "0")
                {
                    var CK_ASS_2F = form["CK_ASS_2F" + id_list[v]];
                    if (IsNoEmpty(CK_ASS_2F))
                    {
                        var left = new List<string>();
                        var right = new List<string>();
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_2F.Split(',');
                        result += "臉部：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                result += "不對稱";

                            if (val == "1")
                                left.Add("嘴角下垂");
                            if (val == "2")
                                left.Add("瘜肉");

                            if (val == "3")
                                right.Add("嘴角下垂");
                            if (val == "4")
                                right.Add("瘜肉");

                            if (val == "7" && IsNoEmpty(form["TXT_ASS_2FO" + id_list[v]]))
                                other.Add(form["TXT_ASS_2FO" + id_list[v]].ToString());
                        }
                        if (left.Count > 0)
                            result += $"，左：{String.Join("、", left)}";
                        if (right.Count > 0)
                            result += $"，右：{String.Join("、", right)}";
                        if (other.Count > 0)
                            result += $"，{String.Join("、", other)}";

                        exceptionResult.Add(result);
                    }
                }
                #endregion

                #region 眼睛
                if (form["RB_ASS_3" + id_list[v]] == "0")
                {
                    var CK_ASS_3F = form["CK_ASS_3F" + id_list[v]];
                    if (IsNoEmpty(CK_ASS_3F))
                    {
                        var left = new List<string>();
                        var right = new List<string>();
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_3F.Split(',');
                        result += "眼睛：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                left.Add("結膜出血");
                            if (val == "1")
                                left.Add("眼瞼水腫");

                            if (val == "2")
                                right.Add("結膜出血");
                            if (val == "3")
                                right.Add("眼瞼水腫");
                            if (val == "4")
                                other.Add("雙眼距離過大");
                            if (val == "5" && IsNoEmpty(form["TXT_ASS_3FO" + id_list[v]]))
                                other.Add(form["TXT_ASS_3FO" + id_list[v]].ToString());
                        }
                        if (left.Count > 0)
                            result += $"左：{String.Join("、", left)}";
                        if (right.Count > 0)
                            result += $"{(left.Count > 0 ? "，" : "")}右：{String.Join("、", right)}";
                        if (other.Count > 0)
                            result += $"{(left.Count > 0 || right.Count > 0 ? "，" : "")}{String.Join("、", other)}";

                        exceptionResult.Add(result);
                    }
                }
                #endregion

                #region 耳朵
                if (form["RB_ASS_4" + id_list[v]] == "0")
                {
                    var CK_ASS_4F = form["CK_ASS_4F" + id_list[v]];
                    if (IsNoEmpty(CK_ASS_4F))
                    {
                        var left = new List<string>();
                        var right = new List<string>();
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_4F.Split(',');
                        result += "耳朵：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                left.Add("低位耳");
                            if (val == "1")
                                left.Add("耳殼異常");
                            if (val == "2")
                                left.Add("瘜肉");

                            if (val == "3")
                                right.Add("低位耳");
                            if (val == "4")
                                right.Add("耳殼異常");
                            if (val == "5")
                                right.Add("瘜肉");

                            if (val == "6" && IsNoEmpty(form["TXT_ASS_4FO" + id_list[v]]))
                                other.Add(form["TXT_ASS_4FO" + id_list[v]].ToString());
                        }
                        if (left.Count > 0)
                            result += $"左：{String.Join("、", left)}";
                        if (right.Count > 0)
                            result += $"{(left.Count > 0 ? "，" : "")}右：{String.Join("、", right)}";
                        if (other.Count > 0)
                            result += $"{(left.Count > 0 || right.Count > 0 ? "，" : "")}{String.Join("、", other)}";

                        exceptionResult.Add(result);
                    }
                }
                #endregion

                #region 口腔
                if (form["RB_ASS_6" + id_list[v]] == "0")
                {
                    var CK_ASS_6F = form["CK_ASS_6F" + id_list[v]];
                    if (IsNoEmpty(CK_ASS_6F))
                    {
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_6F.Split(',');
                        result += "口腔：唇裂與顎裂：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                other.Add("單側不完全唇裂");
                            if (val == "1")
                                other.Add("單側完全唇裂與顎裂");
                            if (val == "2")
                                other.Add("雙側完全唇裂與顎裂");
                            if (val == "3")
                                other.Add("顎裂");
                            if (val == "4" && IsNoEmpty(form["TXT_ASS_6FO" + id_list[v]]))
                                other.Add(form["TXT_ASS_6FO" + id_list[v]].ToString());
                        }
                        result += $"{String.Join("、", other)}";

                        exceptionResult.Add(result);
                    }
                }
                #endregion

                #region 頸部
                if (form["RB_ASS_7" + id_list[v]] == "0")
                {
                    var CK_ASS_7F = form["CK_ASS_7F" + id_list[v]];
                    if (IsNoEmpty(CK_ASS_7F))
                    {
                        var left = new List<string>();
                        var right = new List<string>();
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_7F.Split(',');
                        result += "頸部：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                left.Add("腫塊");
                            if (val == "1")
                                left.Add("疑斜頸");
                            if (val == "2")
                                left.Add("僵硬");

                            if (val == "3")
                                right.Add("腫塊");
                            if (val == "4")
                                right.Add("疑斜頸");
                            if (val == "5")
                                right.Add("僵硬");

                            if (val == "6" && IsNoEmpty(form["TXT_ASS_7FO" + id_list[v]]))
                                other.Add(form["TXT_ASS_7FO" + id_list[v]].ToString());
                        }
                        if (left.Count > 0)
                            result += $"左：{String.Join("、", left)}";
                        if (right.Count > 0)
                            result += $"{(left.Count > 0 ? "，" : "")}右：{String.Join("、", right)}";
                        if (other.Count > 0)
                            result += $"{(left.Count > 0 || right.Count > 0 ? "，" : "")}{String.Join("、", other)}";

                        exceptionResult.Add(result);
                    }
                }
                #endregion

                #region 胸部-外觀
                var chestFlag = false;
                if (form["RB_ASS_8_1" + id_list[v]] == "0")
                {
                    var CK_ASS_8_1F = form["CK_ASS_8_1F" + id_list[v]];
                    if (IsNoEmpty(CK_ASS_8_1F))
                    {
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_8_1F.Split(',');
                        result += "胸部外觀：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                other.Add("胸骨或肋緣凹陷");
                            if (val == "1" && IsNoEmpty(form["TXT_ASS_8_1FO" + id_list[v]]))
                                other.Add(form["TXT_ASS_8_1FO" + id_list[v]].ToString());
                        }
                        result += $"{String.Join("、", other)}";

                        exceptionResult.Add(result);
                        chestFlag = true;
                    }
                }
                #endregion

                #region 胸部-呼吸
                if (form["RB_ASS_8_2" + id_list[v]] == "0")
                {
                    var CK_ASS_8_2F = form["CK_ASS_8_2F" + id_list[v]];
                    if (IsNoEmpty(CK_ASS_8_2F))
                    {
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_8_2F.Split(',');
                        result += $"{(chestFlag == false ? "胸部" : "")}呼吸：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                other.Add($"{(IsNoEmpty(form["TXT_ASS_8_2FO" + id_list[v]]) ? "呼吸急促" + form["TXT_ASS_8_2FO" + id_list[v]] + "次/分" : "呼吸急促")}");
                            if (val == "1")
                                other.Add("鼻翼搧動");
                            if (val == "2")
                                other.Add("呻吟聲(grunting)");
                            if (val == "3")
                                other.Add("胸骨凹陷");
                        }
                        result += $"{String.Join("、", other)}";

                        exceptionResult.Add(result);
                        chestFlag = true;
                    }
                }
                #endregion

                #region 胸部-呼吸音
                if (form["RB_ASS_8_3" + id_list[v]] == "0")
                {
                    var CK_ASS_8_3F = form["CK_ASS_8_3F" + id_list[v]];
                    if (IsNoEmpty(CK_ASS_8_3F))
                    {
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_8_3F.Split(',');
                        result += $"{(chestFlag == false ? "胸部" : "")}呼吸音：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                other.Add("Rhonchi 乾囉音");
                            if (val == "1")
                                other.Add("wheeze 喘嗚音");
                            if (val == "2")
                                other.Add("Bronchial 支氣管音");
                            if (val == "3")
                                other.Add("Rub 摩擦音");
                            if (val == "4")
                                other.Add("Crackles 囉音");
                        }
                        result += $"{String.Join("、", other)}";

                        exceptionResult.Add(result);
                        chestFlag = true;
                    }
                }
                #endregion

                #region 胸部-心臟
                if (form["RB_ASS_8_4" + id_list[v]] == "0")
                {
                    var CK_ASS_8_4F = form["CK_ASS_8_4F" + id_list[v]];
                    if (IsNoEmpty(CK_ASS_8_4F))
                    {
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_8_4F.Split(',');
                        result += $"{(chestFlag == false ? "胸部" : "")}心臟：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                other.Add("心跳不規則");
                            if (val == "1")
                                other.Add("雜音");
                            if (val == "2" && IsNoEmpty(form["TXT_ASS_8_4FO" + id_list[v]]))
                                other.Add(form["TXT_ASS_8_4FO" + id_list[v]]);
                        }
                        result += $"{String.Join("、", other)}";

                        exceptionResult.Add(result);
                        chestFlag = true;
                    }
                }
                #endregion

                #region 腹部-疝氣
                if (form["RB_ASS_9_1" + id_list[v]] == "0")
                {
                    var CK_ASS_9_1F = form["CK_ASS_9_1F" + id_list[v]];
                    if (IsNoEmpty(CK_ASS_9_1F))
                    {
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_9_1F.Split(',');
                        result += $"腹部疝氣： 部位：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                other.Add("臍部");
                            if (val == "1")
                                other.Add("左側鼠蹊部");
                            if (val == "2")
                                other.Add("右側鼠蹊部");
                            if (val == "3" && IsNoEmpty(form["TXT_ASS_9_1FO" + id_list[v]]))
                                other.Add(form["TXT_ASS_9_1FO" + id_list[v]]);
                        }
                        result += $"{String.Join("、", other)}";

                        exceptionResult.Add(result);
                    }
                }
                #endregion

                #region 臀背部
                if (form["RB_ASS_11" + id_list[v]] == "0")
                {
                    var CK_ASS_11F = form["CK_ASS_11F" + id_list[v]];
                    if (IsNoEmpty(CK_ASS_11F))
                    {
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_11F.Split(',');
                        result += $"臀背部：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                other.Add("毛性小凹");
                            if (val == "1")
                                other.Add("脊髓膜膨出");
                            if (val == "2")
                                other.Add("脊椎彎曲");
                            if (val == "3" && IsNoEmpty(form["TXT_ASS_11FO" + id_list[v]]))
                                other.Add(form["TXT_ASS_11FO" + id_list[v]]);
                        }
                        result += $"{String.Join("、", other)}";

                        exceptionResult.Add(result);
                    }
                }
                #endregion

                #region 泌尿生殖器外觀-男-陰囊
                var downFlag = false;
                if (form["RB_ASS_12_2" + id_list[v]] == "0")
                {
                    var CK_ASS_12_2F = form["CK_ASS_12_2F" + id_list[v]];
                    if (IsNoEmpty(CK_ASS_12_2F))
                    {
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_12_2F.Split(',');
                        result += $"泌尿生殖器外觀男陰囊：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                other.Add("水腫");
                            if (val == "1")
                                other.Add("破皮");
                            if (val == "2" && IsNoEmpty(form["TXT_ASS_12_2FO" + id_list[v]]))
                                other.Add(form["TXT_ASS_12_2FO" + id_list[v]]);
                        }
                        result += $"{String.Join("、", other)}";

                        exceptionResult.Add(result);
                        downFlag = true;
                    }
                }
                #endregion

                #region 泌尿生殖器外觀-肛門
                if (form["RB_ASS_12_8" + id_list[v]] == "0")
                {
                    var CK_ASS_12_8F = form["CK_ASS_12_8F" + id_list[v]];
                    if (IsNoEmpty(CK_ASS_12_8F))
                    {
                        var other = new List<string>();
                        var result = "";

                        var values = CK_ASS_12_8F.Split(',');
                        result += $"{(downFlag == false ? "泌尿生殖器外觀" : "")}肛門：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                other.Add("無肛");
                            if (val == "1")
                                other.Add("閉鎖");
                            if (val == "2")
                                other.Add("瘜肉");
                            if (val == "3")
                                other.Add("裂隙");
                            if (val == "4" && IsNoEmpty(form["TXT_ASS_12_8FO" + id_list[v]]))
                                other.Add(form["TXT_ASS_12_8FO" + id_list[v]]);
                        }
                        result += $"{String.Join("、", other)}";

                        exceptionResult.Add(result);
                        downFlag = true;
                    }
                }
                #endregion

                #region 四肢
                var feetFlag = false;
                if (form["RB_ASS_17"] == "0")
                {
                    #region 足內翻
                    if (form["RB_ASS_17_1" + id_list[v]] == "1")
                    {
                        var CK_ASS_17_1F = form["CK_ASS_17_1F" + id_list[v]];
                        if (IsNoEmpty(CK_ASS_17_1F))
                        {
                            var other = new List<string>();
                            var result = "";

                            var values = CK_ASS_17_1F.Split(',');
                            result += $"{(feetFlag == false ? "四肢：" : "")}足內翻：";
                            foreach (var val in values)
                            {
                                if (val == "0")
                                    other.Add("左腳");
                                if (val == "1")
                                    other.Add("右腳");
                            }

                            result += $"{String.Join("、", other)}";

                            exceptionResult.Add(result);
                            feetFlag = true;
                        }
                    }
                    #endregion

                    #region 足外翻
                    if (form["RB_ASS_17_2" + id_list[v]] == "1")
                    {
                        var CK_ASS_17_2F = form["CK_ASS_17_2F" + id_list[v]];
                        if (IsNoEmpty(CK_ASS_17_2F))
                        {
                            var other = new List<string>();
                            var result = "";

                            var values = CK_ASS_17_2F.Split(',');
                            result += $"{(feetFlag == false ? "四肢：" : "")}足外翻：";
                            foreach (var val in values)
                            {
                                if (val == "0")
                                    other.Add("左腳");
                                if (val == "1")
                                    other.Add("右腳");
                            }

                            result += $"{String.Join("、", other)}";

                            exceptionResult.Add(result);
                            feetFlag = true;
                        }
                    }
                    #endregion

                    #region 單一手紋(斷掌)
                    if (form["RB_ASS_17_3" + id_list[v]] == "1")
                    {
                        var CK_ASS_17_3F = form["CK_ASS_17_3F" + id_list[v]];
                        if (IsNoEmpty(CK_ASS_17_3F))
                        {
                            var other = new List<string>();
                            var result = "";

                            var values = CK_ASS_17_3F.Split(',');
                            result += $"{(feetFlag == false ? "四肢：" : "")}單一手紋(斷掌)：";
                            foreach (var val in values)
                            {
                                if (val == "0")
                                    other.Add("左手");
                                if (val == "1")
                                    other.Add("右手");
                            }

                            result += $"{String.Join("、", other)}";

                            exceptionResult.Add(result);
                            feetFlag = true;
                        }
                    }
                    #endregion
                }
                #endregion

                #region 四肢-手指
                if (form["RB_ASS_17_7" + id_list[v]] == "0")
                {
                    var CK_ASS_17_7F = form["CK_ASS_17_7F" + id_list[v]];
                    if (IsNoEmpty(CK_ASS_17_7F))
                    {
                        var left = new List<string>();
                        var right = new List<string>();
                        var result = "";

                        var values = CK_ASS_17_7F.Split(',');
                        result += $"{(feetFlag == false ? "四肢 : " : "")}手指：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                left.Add($"{(IsNoEmpty(form["TXT_ASS_17_7F1" + id_list[v]]) ? "少指，部位：" + form["TXT_ASS_17_7F1" + id_list[v]] : "少指")}");
                            if (val == "1")
                                left.Add($"{(IsNoEmpty(form["TXT_ASS_17_7F2" + id_list[v]]) ? "多指，部位：" + form["TXT_ASS_17_7F2" + id_list[v]] : "多指")}");
                            if (val == "2")
                                left.Add($"{(IsNoEmpty(form["TXT_ASS_17_7F3" + id_list[v]]) ? "併指，部位：" + form["TXT_ASS_17_7F3" + id_list[v]] : "併指")}");
                            if (val == "3")
                                left.Add($"{(IsNoEmpty(form["TXT_ASS_17_7F4" + id_list[v]]) ? "蹼狀指，部位：" + form["TXT_ASS_17_7F4" + id_list[v]] : "蹼狀指")}");

                            if (val == "4")
                                left.Add($"{(IsNoEmpty(form["TXT_ASS_17_7F5" + id_list[v]]) ? "少指，部位：" + form["TXT_ASS_17_7F5" + id_list[v]] : "少指")}");
                            if (val == "5")
                                left.Add($"{(IsNoEmpty(form["TXT_ASS_17_7F6" + id_list[v]]) ? "多指，部位：" + form["TXT_ASS_17_7F6" + id_list[v]] : "多指")}");
                            if (val == "6")
                                left.Add($"{(IsNoEmpty(form["TXT_ASS_17_7F7" + id_list[v]]) ? "併指，部位：" + form["TXT_ASS_17_7F7" + id_list[v]] : "併指")}");
                            if (val == "7")
                                left.Add($"{(IsNoEmpty(form["TXT_ASS_17_7F8" + id_list[v]]) ? "蹼狀指，部位：" + form["TXT_ASS_17_7F8" + id_list[v]] : "蹼狀指")}");
                        }

                        if (left.Count > 0)
                            result += $"左：{String.Join("、", left)}";
                        if (right.Count > 0)
                            result += $"{(left.Count > 0 ? "。" : "")}右：{String.Join("、", right)}";

                        exceptionResult.Add(result);
                        feetFlag = true;
                    }
                }
                #endregion

                #region 四肢-腳趾
                if (form["RB_ASS_17_8" + id_list[v]] == "0")
                {
                    var CK_ASS_17_8F = form["CK_ASS_17_8F" + id_list[v]];
                    if (IsNoEmpty(CK_ASS_17_8F))
                    {
                        var left = new List<string>();
                        var right = new List<string>();
                        var result = "";

                        var values = CK_ASS_17_8F.Split(',');
                        result += $"{(feetFlag == false ? "四肢 :" : "")}腳趾：";
                        foreach (var val in values)
                        {
                            if (val == "0")
                                left.Add($"{(IsNoEmpty(form["TXT_ASS_17_8F1" + id_list[v]]) ? "少趾，部位：" + form["TXT_ASS_17_8F1" + id_list[v]] : "少趾")}");
                            if (val == "1")
                                left.Add($"{(IsNoEmpty(form["TXT_ASS_17_8F2" + id_list[v]]) ? "多趾，部位：" + form["TXT_ASS_17_8F2" + id_list[v]] : "多趾")}");
                            if (val == "2")
                                left.Add($"{(IsNoEmpty(form["TXT_ASS_17_8F3" + id_list[v]]) ? "併趾，部位：" + form["TXT_ASS_17_8F3" + id_list[v]] : "併趾")}");

                            if (val == "3")
                                right.Add($"{(IsNoEmpty(form["TXT_ASS_17_8F4" + id_list[v]]) ? "少趾，部位：" + form["TXT_ASS_17_8F4" + id_list[v]] : "少趾")}");
                            if (val == "4")
                                right.Add($"{(IsNoEmpty(form["TXT_ASS_17_8F5" + id_list[v]]) ? "多趾，部位：" + form["TXT_ASS_17_8F5" + id_list[v]] : "多趾")}");
                            if (val == "5")
                                right.Add($"{(IsNoEmpty(form["TXT_ASS_17_8F6" + id_list[v]]) ? "併趾，部位：" + form["TXT_ASS_17_8F6" + id_list[v]] : "併趾")}");
                        }

                        if (left.Count > 0)
                            result += $"左：{String.Join("、", left)}";
                        if (right.Count > 0)
                            result += $"{(left.Count > 0 ? "。" : "")}右：{String.Join("、", right)}";

                        exceptionResult.Add(result);
                        feetFlag = true;
                    }
                }
                #endregion

                if (exceptionResult.Count > 0)
                    finalResult += String.Join("。", exceptionResult);
                else
                    finalResult += "無異常項目";

                var CareRecord = new CareRecord();
                DataTable dt = CareRecord.sel_carerecord(id_list[v] + "OBS_BABYENTR");

                if (form["type"].ToString() != "暫存")
                {
                    if (dt == null || dt.Rows.Count == 0)
                    {
                        erow += base.Insert_CareRecordTns(dateNow, id_list[v], "嬰幼兒入院護理評估", finalResult + "。", "", "", "", "", "OBS_BABYENTR", ref link);
                    }
                    else
                    {
                        erow += base.Upd_CareRecord(dateNow, id_list[v], "嬰幼兒入院護理評估", finalResult + "。", "", "", "", "", "OBS_BABYENTR");
                    }
                }

                #endregion

                if (erow > 0)
                {
                    List<DBItem> deleteDataList = new List<DBItem>();
                    deleteDataList.Add(new DBItem("DELETED", userinfo.EmployeesNo, DBItem.DBDataType.String));
                    string deletewhere = " IID = '" + id_list[v] + "' ";

                    int erow_HO = obs_m.DBExecDelete("OBS_BABYENTR_HO", deletewhere);
                    int erow_SG = obs_m.DBExecDelete("OBS_BABYENTR_SG", deletewhere);
                    int erow_CT = obs_m.DBExecDelete("OBS_BABYENTR_CT", deletewhere);

                    #region 新增住院經驗
                    int erow_HOSP = 0;
                    var HOSP_RS = form["TXT_HOSP_RS" + id_list[v]].Split(',');
                    var HOSP_P = form["TXT_HOSP_P" + id_list[v]].Split(',');
                    if (HOSP == "1")
                    {
                        for (var i = 0; i < HOSP_RS.Length; i++)
                        {
                            List<DBItem> insertDataList_HOSP = new List<DBItem>();
                            insertDataList_HOSP.Add(new DBItem("IID", id_list[v], DBItem.DBDataType.String));
                            insertDataList_HOSP.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                            insertDataList_HOSP.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
                            insertDataList_HOSP.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                            insertDataList_HOSP.Add(new DBItem("RECORDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime)); //紀錄時間

                            insertDataList_HOSP.Add(new DBItem("SNO", (i + 1).ToString(), DBItem.DBDataType.String));
                            insertDataList_HOSP.Add(new DBItem("HOSP_RS", HOSP_RS[i], DBItem.DBDataType.String));
                            insertDataList_HOSP.Add(new DBItem("HOSP_P", HOSP_P[i], DBItem.DBDataType.String));

                            insertDataList_HOSP = setNullToEmpty(insertDataList_HOSP);
                            erow_HOSP += link.DBExecInsertTns("OBS_BABYENTR_HO", insertDataList_HOSP);
                        }
                    }
                    #endregion

                    #region 新增手術經驗

                    int erow_SUR = 0;
                    var SUR_RS = form["TXT_SUR_RS" + id_list[v]].Split(',');
                    var SUR_P = form["TXT_SUR_P" + id_list[v]].Split(',');
                    if (SUR == "1")
                    {
                        for (var i = 0; i < SUR_RS.Length; i++)
                        {
                            List<DBItem> insertDataList_SUR = new List<DBItem>();
                            insertDataList_SUR.Add(new DBItem("IID", id_list[v], DBItem.DBDataType.String));
                            insertDataList_SUR.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                            insertDataList_SUR.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
                            insertDataList_SUR.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                            insertDataList_SUR.Add(new DBItem("RECORDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime)); //紀錄時間

                            insertDataList_SUR.Add(new DBItem("SNO", (i + 1).ToString(), DBItem.DBDataType.String));
                            insertDataList_SUR.Add(new DBItem("SUR_RS", SUR_RS[i], DBItem.DBDataType.String));
                            insertDataList_SUR.Add(new DBItem("SUR_P", SUR_P[i], DBItem.DBDataType.String));

                            insertDataList_SUR = setNullToEmpty(insertDataList_SUR);
                            erow_SUR += link.DBExecInsertTns("OBS_BABYENTR_SG", insertDataList_SUR);
                        }
                    }
                    #endregion

                    #region 新增聯絡人

                    var COUNT = Convert.ToInt32(form["CONTACT_COUNT"]);
                    int erow_CONTACT = 0;

                    for (var i = 0; i < COUNT; i++)
                    {
                        List<DBItem> insertDataList_CONTACT = new List<DBItem>();
                        insertDataList_CONTACT.Add(new DBItem("IID", id_list[v], DBItem.DBDataType.String));
                        insertDataList_CONTACT.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
                        insertDataList_CONTACT.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
                        insertDataList_CONTACT.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                        insertDataList_CONTACT.Add(new DBItem("RECORDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime)); //紀錄時間

                        insertDataList_CONTACT.Add(new DBItem("SNO", (i + 1).ToString(), DBItem.DBDataType.String));
                        insertDataList_CONTACT.Add(new DBItem("CONTACT", form["TXT_CONTACT_" + (i + 1).ToString() + id_list[v]], DBItem.DBDataType.String));
                        insertDataList_CONTACT.Add(new DBItem("TITLE", form["RB_TITLE_" + (i + 1).ToString() + id_list[v]], DBItem.DBDataType.String));
                        if (form["RB_TITLE_" + (i + 1).ToString() + id_list[v]] == "9")
                            insertDataList_CONTACT.Add(new DBItem("TITLE_OTH", form["TXT_TITLE_OTH_" + (i + 1).ToString() + id_list[v]], DBItem.DBDataType.String));
                        else
                            insertDataList_CONTACT.Add(new DBItem("TITLE_OTH", null, DBItem.DBDataType.String));

                        insertDataList_CONTACT.Add(new DBItem("TEL_C", form["TXT_TEL_C_" + (i + 1).ToString() + id_list[v]], DBItem.DBDataType.String));
                        insertDataList_CONTACT.Add(new DBItem("TEL_H", form["TXT_TEL_H_" + (i + 1).ToString() + id_list[v]], DBItem.DBDataType.String));
                        insertDataList_CONTACT.Add(new DBItem("TEL_M", form["TXT_TEL_M_" + (i + 1).ToString() + id_list[v]], DBItem.DBDataType.String));

                        insertDataList_CONTACT = setNullToEmpty(insertDataList_CONTACT);
                        erow_CONTACT += link.DBExecInsertTns("OBS_BABYENTR_CT", insertDataList_CONTACT);
                    }
                    #endregion

                    if (erow_HOSP != HOSP_RS.Length && HOSP_RS[0] != "")
                    {
                        link.DBRollBack();
                        json_result.status = RESPONSE_STATUS.ERROR;
                        json_result.message = "住院經驗 新增失敗";
                        //return "-1";
                    }
                    //Response.Write("<script>alert('住院經驗新增失敗');window.location.href='../BabyBorn/BabyEntr_List';</script>");
                    else if (erow_SUR != SUR_RS.Length && SUR_RS[0] != "")
                    {
                        link.DBRollBack();
                        json_result.status = RESPONSE_STATUS.ERROR;
                        json_result.message = "手術經驗 新增失敗";
                        //return "-2";
                    }
                    //Response.Write("<script>alert('手術經驗新增失敗');window.location.href='../BabyBorn/BabyEntr_List';</script>");
                    else if (erow_CONTACT != COUNT)
                    {
                        link.DBRollBack();
                        json_result.status = RESPONSE_STATUS.ERROR;
                        json_result.message = "緊急聯絡人 新增失敗";
                        //return "-3";
                    }
                    //Response.Write("<script>alert('緊急聯絡人新增失敗');window.location.href='../BabyBorn/BabyEntr_List';</script>");
                    else
                    {
                        link.DBCommit();
                        string return_jsonstr = string.Empty;
                        if (form["TYPE"] == "已完成")
                        {

                            PdfEmr.ControllerContext = ControllerContext;
                            PdfEmr.userinfo = userinfo;
                            PdfEmr.ptinfo = ptinfo;
                            return_jsonstr = PdfEmr.GetPDF_EMR("Save", feeno, userno, "Insert_BabyEntr", id_list[v]);
                            json_result = JsonConvert.DeserializeObject<RESPONSE_MSG>(return_jsonstr);
                        }
                        else
                        {
                            json_result.status = RESPONSE_STATUS.SUCCESS;
                            json_result.message = "";

                        }
                        //return "1";       
                    }
                    //Response.Write("<script>alert('新增成功');window.location.href='../BabyBorn/BabyEntr_List';</script>");
                }
                else
                {
                    link.DBRollBack();
                    json_result.status = RESPONSE_STATUS.ERROR;
                    json_result.message = " 新增失敗";
                    //return "-4";
                }
                //Response.Write("<script>alert('新增失敗');window.location.href='../BabyBorn/BabyEntr_List';</script>");
            }
            return Content(JsonConvert.SerializeObject(json_result), "application/json");
        }
        #endregion

        #region 嬰幼兒入院護理評估單刪除
        /// <summary>
        /// 嬰幼兒入院護理評估單刪除
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        [HttpPost]
        public string Del_BabyEntr(string IID)
        {
            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("DELETED", userinfo.EmployeesNo, DBItem.DBDataType.String));
            string where = " IID = '" + IID + "' ";

            insertDataList = setNullToEmpty(insertDataList);
            int erow_HO = obs_m.DBExecUpdate("OBS_BABYENTR_HO", insertDataList, where);
            int erow_SG = obs_m.DBExecUpdate("OBS_BABYENTR_SG", insertDataList, where);
            int erow_CT = obs_m.DBExecUpdate("OBS_BABYENTR_CT", insertDataList, where);
            int erow_ASS_1_1F = obs_m.DBExecUpdate("OBS_ASS_1_1F", insertDataList, where);

            //base.Del_CareRecordTns(IID, "OBS_BABYENTR", ref link);
            base.Del_CareRecord(IID, "OBS_BABYENTR");
            if (erow_HO >= 0 && erow_SG >= 0 && erow_CT >= 0 && erow_ASS_1_1F >= 0)
            {
                int erow = obs_m.DBExecUpdate("OBS_BABYENTR", insertDataList, where);
                if (erow > 0)
                {
                    del_emr(IID, userinfo.EmployeesNo);
                    return "1";
                    // Response.Write("<script>alert('刪除成功');window.location.href='../BabyBorn/BabyEntr_List';</script>");
                }
                else
                {
                    return "-1";
                    //Response.Write("<script>alert('刪除失敗');window.location.href='../BabyBorn/BabyEntr_List';</script>");
                }

            }
            else
                return "-1";
            //Response.Write("<script>alert('刪除失敗');window.location.href='../BabyBorn/BabyEntr_List';</script>");
        }

        #endregion

        #region 評估單頁首
        public ActionResult ENTR_HEADER(string feeno, string Type)
        {
            PatientInfo pinfo = new PatientInfo();
            byte[] ByteCode = webService.GetPatientInfo(feeno);
            string JosnArr = "";
            //病人資訊
            if (ByteCode != null)
            {
                JosnArr = CompressTool.DecompressString(ByteCode);
                pinfo = JsonConvert.DeserializeObject<PatientInfo>(JosnArr);
            }
            ViewData["ptinfo"] = pinfo;

            switch (Type)
            {
                case "Insert_NBENTR":
                    ViewBag.Title = "新生兒入院護理評估單";
                    break;
                case "Insert_BabyEntr":
                    ViewBag.Title = "嬰幼兒入院護理評估單";
                    break;
                case "Production_Record":
                    ViewBag.Title = "生產紀錄單";
                    break;
                case "Child_Birth":
                    ViewBag.Title = "嬰兒出生紀錄單";
                    break;
                default:
                    ViewBag.Title = "";
                    break;

            }

            return View();
        }
        #endregion


        #region 新生兒出生7小時觀察記錄
        /// <summary>
        /// 新生兒出生7小時觀察記錄
        /// </summary>
        /// <returns></returns>
        public ActionResult NB7HR_List(string feeno, string chartno, bool IsHisView = false)
        {
            ViewBag.IsHisView = IsHisView;

            if (IsHisView)
            {
                his.ControllerContext = ControllerContext;
                his.getSession(feeno);
            }

            if (Session["PatInfo"] != null)
            {
                if (feeno == "" || feeno == null)
                    feeno = ptinfo.FeeNo;
                DataTable dt_1 = obs_m.sel_nb7hr(feeno, "", "", "1");
                DataTable dt_2 = obs_m.sel_nb7hr(feeno, "", "", "2");
                DataTable dt_3 = obs_m.sel_nb7hr(feeno, "", "", "3");
                DataTable dt_5 = obs_m.sel_nb7hr(feeno, "", "", "5");
                DataTable dt_7 = obs_m.sel_nb7hr(feeno, "", "", "7");

                dt_1.Columns.Add("NURSE_NAME");
                dt_2.Columns.Add("NURSE_NAME");
                dt_3.Columns.Add("NURSE_NAME");
                dt_5.Columns.Add("NURSE_NAME");
                dt_7.Columns.Add("NURSE_NAME");

                foreach (DataRow r in dt_1.Rows)
                {
                    if (r["NURSE"].ToString() != "")
                    {
                        byte[] listByteCode = webService.UserName(r["NURSE"].ToString());
                        if (listByteCode == null)
                            r["NURSE_NAME"] = "";
                        else
                        {
                            string listJsonArray = CompressTool.DecompressString(listByteCode);
                            UserInfo user_name = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                            r["NURSE_NAME"] = user_name.EmployeesName;
                        }
                    }
                }

                foreach (DataRow r in dt_2.Rows)
                {
                    if (r["NURSE"].ToString() != "")
                    {
                        byte[] listByteCode = webService.UserName(r["NURSE"].ToString());
                        if (listByteCode == null)
                            r["NURSE_NAME"] = "";
                        else
                        {
                            string listJsonArray = CompressTool.DecompressString(listByteCode);
                            UserInfo user_name = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                            r["NURSE_NAME"] = user_name.EmployeesName;
                        }
                    }
                }

                foreach (DataRow r in dt_3.Rows)
                {
                    if (r["NURSE"].ToString() != "")
                    {
                        byte[] listByteCode = webService.UserName(r["NURSE"].ToString());
                        if (listByteCode == null)
                            r["NURSE_NAME"] = "";
                        else
                        {
                            string listJsonArray = CompressTool.DecompressString(listByteCode);
                            UserInfo user_name = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                            r["NURSE_NAME"] = user_name.EmployeesName;
                        }
                    }
                }

                foreach (DataRow r in dt_5.Rows)
                {
                    if (r["NURSE"].ToString() != "")
                    {
                        byte[] listByteCode = webService.UserName(r["NURSE"].ToString());
                        if (listByteCode == null)
                            r["NURSE_NAME"] = "";
                        else
                        {
                            string listJsonArray = CompressTool.DecompressString(listByteCode);
                            UserInfo user_name = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                            r["NURSE_NAME"] = user_name.EmployeesName;
                        }
                    }
                }

                foreach (DataRow r in dt_7.Rows)
                {
                    if (r["NURSE"].ToString() != "")
                    {
                        byte[] listByteCode = webService.UserName(r["NURSE"].ToString());
                        if (listByteCode == null)
                            r["NURSE_NAME"] = "";
                        else
                        {
                            string listJsonArray = CompressTool.DecompressString(listByteCode);
                            UserInfo user_name = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                            r["NURSE_NAME"] = user_name.EmployeesName;
                        }
                    }
                }

                ViewBag.dt_1 = set_dt(dt_1);
                ViewBag.dt_2 = set_dt(dt_2);
                ViewBag.dt_3 = set_dt(dt_3);
                ViewBag.dt_5 = set_dt(dt_5);
                ViewBag.dt_7 = set_dt(dt_7);

                return View();
            }
            Response.Write("<script>alert('請重新選擇病患');</script>");
            return new EmptyResult();
        }
        #endregion

        #region 生命徵象(新生兒出生7小時觀察記錄專用)
        public ActionResult VitalSign_Index(string starttime, string endtime, string type)
        {
            string start = DateTime.Now.ToString("yyyy/MM/dd 00:00");
            string end = DateTime.Now.AddMinutes(2).ToString("yyyy/MM/dd HH:mm");
            if (starttime != null && endtime != null)
            {
                start = starttime;
                end = endtime;
            }
            ViewBag.start = start;
            ViewBag.end = end;

            //確認是否有病人資料(有選取病人)
            if (Session["PatInfo"] != null)
            {
                //宣告必須要使用到的變數
                List<VitalSignDataList> vsList = new List<VitalSignDataList>();
                List<string[]> vsId = new List<string[]>();
                VitalSignDataList vsdl = null;
                //取得異常查檢表
                DataTable dt_check = Get_Check_Abnormal_dt();

                //取得vs_id
                DataTable dt = new DataTable();
                string sqlstr = " select CREATE_DATE,vs_id from data_vitalsign where fee_no = '" + ptinfo.FeeNo + "' ";
                sqlstr += "and create_date between to_date('" + start + "','yyyy/MM/dd hh24:mi:ss') and to_date('" + end + "','yyyy/MM/dd hh24:mi:ss') ";
                sqlstr += "group by CREATE_DATE,vs_id order by CREATE_DATE";
                dt = link.DBExecSQL(sqlstr);

                if (dt != null && dt.Rows.Count > 0)
                    foreach (DataRow r in dt.Rows)
                        vsId.Add(new string[] { r["vs_id"].ToString().Trim(), r["CREATE_DATE"].ToString() });

                DataTable dt1 = new DataTable();
                // 開始處理資料
                for (int i = 0; i <= vsId.Count - 1; i++)
                {
                    //初始化資料
                    vsdl = new VitalSignDataList();

                    sqlstr = " select vsd.*, to_char(CREATE_DATE,'yyyy/MM/dd hh24:mi:ss') as m_date  from data_vitalsign vsd ";
                    sqlstr += " where fee_no ='" + ptinfo.FeeNo + "' and vs_id = '" + vsId[i][0] + "' ";
                    sqlstr += " and create_date = to_date('" + Convert.ToDateTime(vsId[i][1]).ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/MM/dd hh24:mi:ss')";

                    vsdl.vsid = vsId[i][0];
                    dt1 = link.DBExecSQL(sqlstr);

                    if (dt1 != null && dt1.Rows.Count > 0)
                        foreach (DataRow r in dt1.Rows)
                        {
                            vsdl.DataList.Add(new VitalSignData(


                            r["vs_item"].ToString().Trim(),
                            r["vs_part"].ToString().Trim(),
                            r["vs_record"].ToString().Trim(),
                            r["vs_reason"].ToString().Trim(),
                            r["vs_memo"].ToString().Trim(),
                            r["vs_other_memo"].ToString().Trim(),
                            r["CREATE_USER"].ToString().Trim(),
                            "", "",
                            r["m_date"].ToString().Trim(),
                            r["vs_type"].ToString().Trim()//區分TYPE類型

                            ));
                        }
                    vsList.Add(vsdl);
                }

                ViewBag.ck_type = base.get_check_type(ptinfo);
                ViewBag.dt_check = dt_check;
                ViewBag.age = ptinfo.Age;
                ViewData["VSData"] = vsList;
                ViewBag.userno = userinfo.EmployeesNo;
                ViewBag.type = type;
            }
            return View();
        }

        //VitalSign_取得異常值檢查表
        //private DataTable Get_Check_Abnormal_dt()
        //{
        //    DataTable dt = new DataTable();
        //    link.DBOpen();
        //    link.DBExecSQL("SELECT * FROM NIS_SYS_VITALSIGN_OPTION ", ref dt);
        //    link.DBClose();
        //    return dt;
        //}
        #endregion

        #region 新生兒出生7小時觀察記錄新增

        /// <summary>
        /// 新生兒出生7小時觀察記錄新增
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public string Insert_NB7HR(FormCollection form)
        {
            link.DBOpen();
            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;
            string id = base.creatid("OBS_NB7HR", userno, feeno, "0");

            string type = form["TYPE"];

            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("IID", id, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("RECORDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime)); //紀錄時間

            insertDataList.Add(new DBItem("SNO", type, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("TPR", form["TXT_TPR_" + type], DBItem.DBDataType.String));

            if (form["TXT_ASMT_TIME_DAY_" + type] != "" && form["TXT_ASMT_TIME_DAY_" + type] != null && form["TXT_ASMT_TIME_TIME_" + type] != "" && form["TXT_ASMT_TIME_TIME_" + type] != null)
                insertDataList.Add(new DBItem("ASMT_TIME", form["TXT_ASMT_TIME_DAY_" + type] + " " + form["TXT_ASMT_TIME_TIME_" + type], DBItem.DBDataType.DataTime));

            var BRE_TYP = form["CK_BRE_TYP_" + type];
            var BRE_TYPV = new List<string>() { "0", "0", "0", "0" };
            if (BRE_TYP == null)
                insertDataList.Add(new DBItem("BRE_TYP", String.Join("", BRE_TYPV), DBItem.DBDataType.String));
            else
            {
                var BRE_TYP_SP = BRE_TYP.Split(',');
                foreach (var i in BRE_TYP_SP)
                    BRE_TYPV[Convert.ToInt32(i)] = "1";
                insertDataList.Add(new DBItem("BRE_TYP", String.Join("", BRE_TYPV), DBItem.DBDataType.String));
            }

            insertDataList.Add(new DBItem("CRY", form["RB_CRY_" + type], DBItem.DBDataType.String));
            if (form["RB_CRY_" + type] == "4")
            {
                insertDataList.Add(new DBItem("CRY_NO", form["TXT_CRY_NO_" + type], DBItem.DBDataType.String));
            }
            insertDataList.Add(new DBItem("ACTI", form["RB_ACTI_" + type], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("SKIN", form["RB_SKIN_" + type], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("MUSCLE", form["RB_MUSCLE_" + type], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FS", form["RB_FS_" + type], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("URINE", form["RB_URINE_" + type], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("INCU", form["RB_INCU_" + type], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("TREAT", form["TXT_TREAT_" + type], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("NURSE", userno, DBItem.DBDataType.String));

            insertDataList = setNullToEmpty(insertDataList);
            int erow = link.DBExecInsertTns("OBS_NB7HR", insertDataList);
            //TODO CareRecord Title
            var title = $"出生七小時觀察紀錄-第{type}小時";
            #region 護理紀錄
            List<string> content = new List<string>();
            var field = "";

            #region 生命徵象
            field = "生命徵象：";
            if (form["TXT_TPR_" + type] != "" && form["TXT_TPR_" + type] != null)
            {
                var TPRs = form["TXT_TPR_" + type].ToString().Split('/');
                //體溫
                if (TPRs[0] != "")
                    field += $"{TPRs[0]}℃";
                else
                    field += "無";

                //脈搏
                if (TPRs[1] != "")
                    field += $"，{TPRs[1]}次/分";
                else
                    field += "，無";

                //呼吸
                if (TPRs[2] != "")
                    field += $"，{TPRs[2]}次/分";
                else
                    field += "，無";
            }
            else
                field += "無";
            content.Add(field);
            #endregion
            #region 呼吸狀態
            field = "呼吸狀態：";
            if (form["CK_BRE_TYP_" + type] != "" && form["CK_BRE_TYP_" + type] != null)
            {
                var bre_typ = form["CK_BRE_TYP_" + type].Split(',');
                var commaFlag = false;
                foreach (var r in bre_typ)
                {
                    if (r == "0")
                        field += (commaFlag == false ? "" : "、") + "順暢";
                    else if (r == "1")
                        field += (commaFlag == false ? "" : "、") + "呼吸急促";
                    else if (r == "2")
                        field += (commaFlag == false ? "" : "、") + "胸骨凹陷";
                    else if (r == "3")
                        field += (commaFlag == false ? "" : "、") + "困難";

                    commaFlag = true;
                }
            }
            else
            {
                field += "無";
            }
            content.Add(field);
            #endregion
            #region 哭聲
            field = "哭聲：";
            switch (form["RB_CRY_" + type]?.ToString())
            {
                case "0":
                    field += "宏亮";
                    break;
                case "1":
                    field += "尚可";
                    break;
                case "2":
                    field += "弱小";
                    break;
                case "3":
                    field += "尖銳";
                    break;
                default:
                    field += "無";
                    break;
            }
            content.Add(field);
            #endregion
            #region 活動力
            field = "活動力：";
            switch (form["RB_ACTI_" + type]?.ToString())
            {
                case "0":
                    field += "強";
                    break;
                case "1":
                    field += "中";
                    break;
                case "2":
                    field += "弱";
                    break;
                default:
                    field += "無";
                    break;
            }
            content.Add(field);
            #endregion
            #region 膚色
            field = "膚色：";
            switch (form["RB_SKIN_" + type]?.ToString())
            {
                case "0":
                    field += "粉紅";
                    break;
                case "1":
                    field += "蒼白";
                    break;
                case "2":
                    field += "發紺";
                    break;
                default:
                    field += "無";
                    break;
            }
            content.Add(field);
            #endregion
            #region 肌肉張力
            field = "肌肉張力：";
            switch (form["RB_MUSCLE_" + type]?.ToString())
            {
                case "0":
                    field += "強";
                    break;
                case "1":
                    field += "中";
                    break;
                case "2":
                    field += "弱";
                    break;
                default:
                    field += "無";
                    break;
            }
            content.Add(field);
            #endregion
            #region 胎便
            field = "";
            switch (form["RB_FS_" + type]?.ToString())
            {
                case "0":
                    field += "未解";
                    break;
                case "1":
                    field += "已解";
                    break;
                default:
                    field += "無";
                    break;
            }
            field += "胎便";
            content.Add(field);
            #endregion
            #region 小便
            field = "";
            switch (form["RB_URINE_" + type]?.ToString())
            {
                case "0":
                    field += "未解";
                    break;
                case "1":
                    field += "已解";
                    break;
                default:
                    field += "無";
                    break;
            }
            field += "小便";
            content.Add(field);
            #endregion
            #region 保溫箱
            field = "";
            switch (form["RB_INCU_" + type]?.ToString())
            {
                case "0":
                    field += "無";
                    break;
                case "1":
                    field += "有";
                    break;
                default:
                    field += "無";
                    break;
            }
            field += "使用保溫箱";
            content.Add(field);
            #endregion
            #region 其它處置
            field = "給予處置：" + form["TXT_TREAT_" + type] ?? "無";
            content.Add(field);
            #endregion
            #region 護理人員
            field = "護理人員：" + Get_Employee_Name(userno) ?? "無";
            content.Add(field);
            #endregion

            #endregion

            erow += base.Insert_CareRecordTns(form["TXT_ASMT_TIME_DAY_" + type] + " " + form["TXT_ASMT_TIME_TIME_" + type], id, title, String.Join("，", content) + "。", "", "", "", "", "OBS_NB7HR", ref link);
            if (erow == 2)
                link.DBCommit();
            else
                link.DBRollBack();
            return (erow / 2).ToString();
        }
        #endregion

        #region 新生兒出生7小時觀察記錄編輯
        /// <summary>
        /// 新生兒出生7小時觀察記錄編輯
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        [HttpPost]
        public string Upd_NB7HR(FormCollection form)
        {
            link.DBOpen();
            string userno = userinfo.EmployeesNo;
            string type = form["TYPE"];
            string feeno = ptinfo.FeeNo;
            string[] id_list = form["IID_" + type].Split(',');
            List<DBItem> insertDataList = new List<DBItem>();
            int erow = 0;
            for (int v = 0; v < id_list.Length; v++)
            {
                insertDataList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("RECORDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime)); //紀錄時間

                insertDataList.Add(new DBItem("TPR", form["TXT_TPR_" + type + id_list[v]], DBItem.DBDataType.String));


                if (form["TXT_ASMT_TIME_DAY_" + type + id_list[v]] != "" && form["TXT_ASMT_TIME_DAY_" + type + id_list[v]] != null && form["TXT_ASMT_TIME_TIME_" + type + id_list[v]] != "" && form["TXT_ASMT_TIME_TIME_" + type + id_list[v]] != null)
                    insertDataList.Add(new DBItem("ASMT_TIME", form["TXT_ASMT_TIME_DAY_" + type + id_list[v]] + " " + form["TXT_ASMT_TIME_TIME_" + type + id_list[v]], DBItem.DBDataType.DataTime));

                var BRE_TYP = form["CK_BRE_TYP_" + type + id_list[v]];
                var BRE_TYPV = new List<string>() { "0", "0", "0", "0" };
                if (BRE_TYP == null)
                    insertDataList.Add(new DBItem("BRE_TYP", String.Join("", BRE_TYPV), DBItem.DBDataType.String));
                else
                {
                    var BRE_TYP_SP = BRE_TYP.Split(',');
                    foreach (var i in BRE_TYP_SP)
                        BRE_TYPV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("BRE_TYP", String.Join("", BRE_TYPV), DBItem.DBDataType.String));
                }

                insertDataList.Add(new DBItem("CRY", form["RB_CRY_" + type + id_list[v]], DBItem.DBDataType.String));
                if (form["RB_CRY_" + type + id_list[v]] == "4")
                {
                    insertDataList.Add(new DBItem("CRY_NO", form["TXT_CRY_NO_" + type + id_list[v]], DBItem.DBDataType.String));
                }
                else
                {
                    insertDataList.Add(new DBItem("CRY_NO", null, DBItem.DBDataType.String));
                }
                insertDataList.Add(new DBItem("ACTI", form["RB_ACTI_" + type + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("SKIN", form["RB_SKIN_" + type + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MUSCLE", form["RB_MUSCLE_" + type + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("FS", form["RB_FS_" + type + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("URINE", form["RB_URINE_" + type + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("INCU", form["RB_INCU_" + type + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("TREAT", form["TXT_TREAT_" + type + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("NURSE", userno, DBItem.DBDataType.String));

                string where = " IID = '" + id_list[v] + "' ";
                insertDataList = setNullToEmpty(insertDataList);
                erow += link.DBExecUpdateTns("OBS_NB7HR", insertDataList, where);

                //TODO CareRecord Title
                var title = $"出生七小時觀察紀錄-第{type}小時";
                #region 護理紀錄
                List<string> content = new List<string>();
                var field = "";

                #region 生命徵象
                field = "生命徵象：";
                if (form["TXT_TPR_" + type + id_list[v]] != "" && form["TXT_TPR_" + type + id_list[v]] != null)
                {
                    var TPRs = form["TXT_TPR_" + type + id_list[v]].ToString().Split('/');
                    //體溫
                    if (TPRs[0] != "")
                        field += $"{TPRs[0]}℃";
                    else
                        field += "無";

                    //脈搏
                    if (TPRs[1] != "")
                        field += $"，{TPRs[1]}次/分";
                    else
                        field += "，無";

                    //呼吸
                    if (TPRs[2] != "")
                        field += $"，{TPRs[2]}次/分";
                    else
                        field += "，無";
                }
                else
                    field += "無";
                content.Add(field);
                #endregion
                #region 呼吸狀態
                field = "呼吸狀態：";
                if (form["CK_BRE_TYP_" + type + id_list[v]] != "" && form["CK_BRE_TYP_" + type + id_list[v]] != null)
                {
                    var bre_typ = form["CK_BRE_TYP_" + type + id_list[v]].Split(',');
                    var commaFlag = false;
                    foreach (var r in bre_typ)
                    {
                        if (r == "0")
                            field += (commaFlag == false ? "" : "、") + "順暢";
                        else if (r == "1")
                            field += (commaFlag == false ? "" : "、") + "呼吸急促";
                        else if (r == "2")
                            field += (commaFlag == false ? "" : "、") + "胸骨凹陷";
                        else if (r == "3")
                            field += (commaFlag == false ? "" : "、") + "困難";

                        commaFlag = true;
                    }
                }
                else
                {
                    field += "無";
                }
                content.Add(field);
                #endregion
                #region 哭聲
                field = "哭聲：";
                switch (form["RB_CRY_" + type + id_list[v]]?.ToString())
                {
                    case "0":
                        field += "宏亮";
                        break;
                    case "1":
                        field += "尚可";
                        break;
                    case "2":
                        field += "弱小";
                        break;
                    case "3":
                        field += "尖銳";
                        break;
                    default:
                        field += "無";
                        break;
                }
                content.Add(field);
                #endregion
                #region 活動力
                field = "活動力：";
                switch (form["RB_ACTI_" + type + id_list[v]]?.ToString())
                {
                    case "0":
                        field += "強";
                        break;
                    case "1":
                        field += "中";
                        break;
                    case "2":
                        field += "弱";
                        break;
                    default:
                        field += "無";
                        break;
                }
                content.Add(field);
                #endregion
                #region 膚色
                field = "膚色：";
                switch (form["RB_SKIN_" + type + id_list[v]]?.ToString())
                {
                    case "0":
                        field += "粉紅";
                        break;
                    case "1":
                        field += "蒼白";
                        break;
                    case "2":
                        field += "發紺";
                        break;
                    default:
                        field += "無";
                        break;
                }
                content.Add(field);
                #endregion
                #region 肌肉張力
                field = "肌肉張力：";
                switch (form["RB_MUSCLE_" + type + id_list[v]]?.ToString())
                {
                    case "0":
                        field += "強";
                        break;
                    case "1":
                        field += "中";
                        break;
                    case "2":
                        field += "弱";
                        break;
                    default:
                        field += "無";
                        break;
                }
                content.Add(field);
                #endregion
                #region 胎便
                field = "";
                switch (form["RB_FS_" + type + id_list[v]]?.ToString())
                {
                    case "0":
                        field += "未解";
                        break;
                    case "1":
                        field += "已解";
                        break;
                    default:
                        field += "無";
                        break;
                }
                field += "胎便";
                content.Add(field);
                #endregion
                #region 小便
                field = "";
                switch (form["RB_URINE_" + type + id_list[v]]?.ToString())
                {
                    case "0":
                        field += "未解";
                        break;
                    case "1":
                        field += "已解";
                        break;
                    default:
                        field += "無";
                        break;
                }
                field += "小便";
                content.Add(field);
                #endregion
                #region 保溫箱
                field = "";
                switch (form["RB_INCU_" + type + id_list[v]]?.ToString())
                {
                    case "0":
                        field += "無";
                        break;
                    case "1":
                        field += "有";
                        break;
                    default:
                        field += "無";
                        break;
                }
                field += "使用保溫箱";
                content.Add(field);
                #endregion
                #region 其它處置
                field = "給予處置：" + form["TXT_TREAT_" + type + id_list[v]] ?? "無";
                content.Add(field);
                #endregion
                #region 護理人員
                field = "護理人員：" + Get_Employee_Name(userno) ?? "無";
                content.Add(field);
                #endregion

                #endregion
                erow += base.Upd_CareRecord(form["TXT_ASMT_TIME_DAY_" + type + id_list[v]] + " " + form["TXT_ASMT_TIME_TIME_" + type + id_list[v]], id_list[v], title, String.Join("，", content) + "。", "", "", "", "", "OBS_NB7HR");
                if (erow == 2)
                    link.DBCommit();
                else
                    link.DBRollBack();
            }
            return (erow / 2).ToString();
        }
        #endregion

        #region 新生兒出生7小時觀察記錄刪除
        /// <summary>
        /// 新生兒出生7小時觀察記錄刪除
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        [HttpPost]
        public string Del_NB7HR(FormCollection form)
        {
            string type = form["TYPE"];
            string id_list = form["IID_" + type].ToString();

            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("DELETED", userinfo.EmployeesNo, DBItem.DBDataType.String));
            string where = " IID = '" + id_list + "' ";
            insertDataList = setNullToEmpty(insertDataList);
            int erow = obs_m.DBExecUpdate("OBS_NB7HR", insertDataList, where);
            return erow.ToString();
        }
        #endregion


        #region  嬰兒呼吸暫停記錄
        /// <summary>
        /// 嬰兒呼吸暫停記錄
        /// </summary>
        /// <returns></returns>
        public ActionResult PIBS_List(string feeno, string chartno, bool IsHisView = false)
        {
            ViewBag.IsHisView = IsHisView;

            if (IsHisView)
            {
                his.ControllerContext = ControllerContext;
                his.getSession(feeno);
            }

            if (Session["PatInfo"] != null)
            {
                if (feeno == "" || feeno == null)
                    feeno = ptinfo.FeeNo;
                DataTable dt = obs_m.sel_pibs(feeno, "", "");
                ViewBag.dt = set_dt(dt);
                return View();
            }
            Response.Write("<script>alert('請重新選擇病患');</script>");
            return new EmptyResult();
        }
        #endregion

        #region 嬰兒呼吸暫停記錄新增/編輯畫面
        /// <summary>
        /// 嬰兒呼吸暫停記錄新增/編輯畫面
        /// </summary>
        /// <returns></returns>
        public ActionResult Insert_PIBS(string IID)
        {
            if (IID != "" && IID != null)
            {
                DataTable dt = obs_m.sel_pibs(ptinfo?.FeeNo, "", IID);
                ViewBag.dt = set_dt(dt);
            }
            return View();
        }
        #endregion

        #region 嬰兒呼吸暫停記錄新增
        /// <summary>
        /// 嬰兒呼吸暫停記錄新增
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Insert_PIBS(FormCollection form)
        {
            link.DBOpen();
            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;
            string id = base.creatid("OBS_PIBS", userno, feeno, "0");
            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("IID", id, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("RECORDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime)); //紀錄時間

            if (form["TXT_ASMT_TIME_DAY"] != "" && form["TXT_ASMT_TIME_DAY"] != null && form["TXT_ASMT_TIME_TIME"] != "" && form["TXT_ASMT_TIME_TIME"] != null)
                insertDataList.Add(new DBItem("ASMT_TIME", form["TXT_ASMT_TIME_DAY"] + " " + form["TXT_ASMT_TIME_TIME"], DBItem.DBDataType.DataTime));

            insertDataList.Add(new DBItem("APNEA", form["TXT_APNEA"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("HBS", form["TXT_HBS"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("SPO2", form["TXT_SPO2"], DBItem.DBDataType.String));

            var REASON = form["CK_REASON"];
            var REASONV = new List<string>() { "0", "0", "0" };
            if (REASON == null)
                insertDataList.Add(new DBItem("REASON", String.Join("", REASONV), DBItem.DBDataType.String));
            else
            {
                var REASON_SP = REASON.Split(',');
                foreach (var i in REASON_SP)
                    REASONV[Convert.ToInt32(i)] = "1";
                insertDataList.Add(new DBItem("REASON", String.Join("", REASONV), DBItem.DBDataType.String));
            }
            if (REASONV[2] == "1")
                insertDataList.Add(new DBItem("REASON_OTH", form["TXT_REASON_OTH"], DBItem.DBDataType.String));
            else
                insertDataList.Add(new DBItem("REASON_OTH", null, DBItem.DBDataType.String));

            var RECO = form["CK_RECO"];
            var RECOV = new List<string>() { "0", "0", "0", "0", "0" };
            if (RECO == null)
                insertDataList.Add(new DBItem("RECO", String.Join("", RECOV), DBItem.DBDataType.String));
            else
            {
                var RECO_SP = RECO.Split(',');
                foreach (var i in RECO_SP)
                    RECOV[Convert.ToInt32(i)] = "1";
                insertDataList.Add(new DBItem("RECO", String.Join("", RECOV), DBItem.DBDataType.String));
            }
            if (RECOV[4] == "1")
                insertDataList.Add(new DBItem("RECO_OTH", form["TXT_RECO_OTH"], DBItem.DBDataType.String));
            else
                insertDataList.Add(new DBItem("RECO_OTH", null, DBItem.DBDataType.String));

            insertDataList.Add(new DBItem("CARE_RECORD", form["CK_CARE_RECORD"], DBItem.DBDataType.String));

            insertDataList = setNullToEmpty(insertDataList);
            int erow = link.DBExecInsertTns("OBS_PIBS", insertDataList);
            #region 護理紀錄
            if (form["CK_CARE_RECORD"] != null && form["CK_CARE_RECORD"] != "" && form["CK_CARE_RECORD"] == "1")
            {
                //TODO CareRecord Title
                List<string> content = new List<string>();
                content.Add(form["TXT_CARE_RECORD"]);
                //var field = "";

                //#region 病嬰【原因 + 其他】
                //field = "病嬰";
                //if (form["CK_REASON"] != "" && form["CK_REASON"] != null)
                //{
                //    var reasons = form["CK_REASON"].Split(',');
                //    var commaFlag = false;
                //    foreach (var r in reasons)
                //    {
                //        if (r == "0")
                //            field += (commaFlag == false ? "" : "、") + "哭泣後憋氣";
                //        else if (r == "1")
                //            field += (commaFlag == false ? "" : "、") + "熟睡";
                //        else if (r == "2" && form["TXT_REASON_OTH"] != "" && form["TXT_REASON_OTH"] != null)
                //            field += (commaFlag == false ? "" : "、") + form["TXT_REASON_OTH"];

                //        commaFlag = true;
                //    }
                //}
                //else
                //{
                //    field += "無";
                //}
                //content.Add(field);
                //#endregion
                //#region 呼吸暫停時間【呼吸暫停時間】秒
                //field = "呼吸暫停時間";
                //if (form["TXT_APNEA"] != "" && form["TXT_APNEA"] != null)
                //    field += form["TXT_APNEA"];
                //else
                //    field += "0";

                //field += "秒";
                //content.Add(field);
                //#endregion
                //#region 心跳次數【心跳次數 】次 / 分
                //field = "心跳次數";
                //if (form["TXT_HBS"] != "" && form["TXT_HBS"] != null)
                //    field += form["TXT_HBS"];
                //else
                //    field += "0";

                //field += "次 / 分";
                //content.Add(field);
                //#endregion
                //#region SpO2：【SpO2(%)】(%)
                //field = "SpO2：";
                //if (form["TXT_SPO2"] != "" && form["TXT_SPO2"] != null)
                //    field += form["TXT_SPO2"];
                //else
                //    field += "0";

                //field += "(%)";
                //content.Add(field);
                //#endregion
                //#region 恢復呼吸之方法:【恢復呼吸之方法】
                //field = "恢復呼吸之方法:";
                //if (form["CK_RECO"] != "" && form["CK_RECO"] != null)
                //{
                //    var reasons = form["CK_RECO"].Split(',');
                //    var commaFlag = false;
                //    foreach (var r in reasons)
                //    {
                //        if (r == "0")
                //            field += (commaFlag == false ? "" : "、") + "刺激";
                //        else if (r == "1")
                //            field += (commaFlag == false ? "" : "、") + "氧氣(O2)";
                //        else if (r == "2")
                //            field += (commaFlag == false ? "" : "、") + "正壓換氣(Bag)";
                //        else if (r == "3")
                //            field += (commaFlag == false ? "" : "、") + "自動";
                //        else if (r == "4" && form["TXT_RECO_OTH"] != "" && form["TXT_RECO_OTH"] != null)
                //            field += (commaFlag == false ? "" : "、") + form["TXT_RECO_OTH"];

                //        commaFlag = true;
                //    }
                //}
                //else
                //{
                //    field += "無";
                //}
                //content.Add(field);
                //#endregion
                //#region 現心跳: 次 / 分、呼吸: 次 / 分
                //field = "現心跳:";
                //if (form["TXT_HBS_NOW"] != "" && form["TXT_HBS_NOW"] != null)
                //    field += form["TXT_HBS_NOW"];
                //else
                //    field += "0";
                //field += "次 / 分";

                //field += "、呼吸:";
                //if (form["TXT_BREATH_NOW"] != "" && form["TXT_BREATH_NOW"] != null)
                //    field += form["TXT_BREATH_NOW"];
                //else
                //    field += "0";
                //field += "次 / 分";
                //content.Add(field);
                //#endregion
                //#region SpO2：(%)
                //field = "SpO2：";
                //if (form["TXT_SPO2_NOW"] != "" && form["TXT_SPO2_NOW"] != null)
                //    field += form["TXT_SPO2_NOW"];
                //else
                //    field += "0";
                //field += "(%)";
                //content.Add(field);
                //#endregion
                //#region 精神活力
                //field = "精神活力：";
                //if (form["TXT_ENERGY_NOW"] != "" && form["TXT_ENERGY_NOW"] != null)
                //    field += form["TXT_ENERGY_NOW"];
                //else
                //    field += "無";
                //content.Add(field);
                //#endregion
                //#region 膚色
                //field = "膚色：";
                //if (form["TXT_SKIN_NOW"] != "" && form["TXT_SKIN_NOW"] != null)
                //    field += form["TXT_SKIN_NOW"];
                //else
                //    field += "無";
                //content.Add(field);
                //#endregion

                //content.Add("裝置生理監視器，繼續觀察病嬰的生理變化中");
                erow += base.Insert_CareRecordTns(form["TXT_ASMT_TIME_DAY"] + " " + form["TXT_ASMT_TIME_TIME"], id, "嬰兒呼吸暫停紀錄", String.Join("，", content) + "。", "", "", "", "", "OBS_PIBS", ref link);
                if (erow == 2)
                {
                    link.DBCommit();
                    Response.Write("<script>alert('新增成功');window.opener.location.href='../Obstetrics/Index?active=PIBS_List';window.close();</script>");
                }
                else
                {
                    link.DBRollBack();
                    Response.Write("<script>alert('新增失敗');window.opener.location.href='../Obstetrics/Index?active=PIBS_List';window.close();</script>");
                }
            }
            #endregion
            else
            {
                if (erow > 0)
                {
                    link.DBCommit();
                    Response.Write("<script>alert('新增成功');window.opener.location.href='../Obstetrics/Index?active=PIBS_List';window.close();</script>");
                }
                else
                {
                    link.DBRollBack();
                    Response.Write("<script>alert('新增失敗');window.opener.location.href='../Obstetrics/Index?active=PIBS_List';window.close();</script>");
                }
            }

            return new EmptyResult();
        }
        #endregion

        #region 嬰兒呼吸暫停記錄編輯
        /// <summary>
        /// 嬰兒呼吸暫停記錄編輯
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        public ActionResult Upd_PIBS(FormCollection form)
        {
            link.DBOpen();
            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;
            string[] id_list = form["IID"].Split(',');
            List<DBItem> insertDataList = new List<DBItem>();
            int erow = 0;
            for (int v = 0; v < id_list.Length; v++)
            {
                insertDataList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("RECORDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime)); //紀錄時間

                if (form["TXT_ASMT_TIME_DAY" + id_list[v]] != "" && form["TXT_ASMT_TIME_DAY" + id_list[v]] != null && form["TXT_ASMT_TIME_TIME" + id_list[v]] != "" && form["TXT_ASMT_TIME_TIME" + id_list[v]] != null)
                    insertDataList.Add(new DBItem("ASMT_TIME", form["TXT_ASMT_TIME_DAY" + id_list[v]] + " " + form["TXT_ASMT_TIME_TIME" + id_list[v]], DBItem.DBDataType.DataTime));

                insertDataList.Add(new DBItem("APNEA", form["TXT_APNEA" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("HBS", form["TXT_HBS" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("SPO2", form["TXT_SPO2" + id_list[v]], DBItem.DBDataType.String));

                var REASON = form["CK_REASON" + id_list[v]];
                var REASONV = new List<string>() { "0", "0", "0" };
                if (REASON == null)
                    insertDataList.Add(new DBItem("REASON", String.Join("", REASONV), DBItem.DBDataType.String));
                else
                {
                    var REASON_SP = REASON.Split(',');
                    foreach (var i in REASON_SP)
                        REASONV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("REASON", String.Join("", REASONV), DBItem.DBDataType.String));
                }
                if (REASONV[2] == "1")
                    insertDataList.Add(new DBItem("REASON_OTH", form["TXT_REASON_OTH" + id_list[v]], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("REASON_OTH", null, DBItem.DBDataType.String));

                var RECO = form["CK_RECO" + id_list[v]];
                var RECOV = new List<string>() { "0", "0", "0", "0", "0" };
                if (RECO == null)
                    insertDataList.Add(new DBItem("RECO", String.Join("", RECOV), DBItem.DBDataType.String));
                else
                {
                    var RECO_SP = RECO.Split(',');
                    foreach (var i in RECO_SP)
                        RECOV[Convert.ToInt32(i)] = "1";
                    insertDataList.Add(new DBItem("RECO", String.Join("", RECOV), DBItem.DBDataType.String));
                }
                if (RECOV[4] == "1")
                    insertDataList.Add(new DBItem("RECO_OTH", form["TXT_RECO_OTH" + id_list[v]], DBItem.DBDataType.String));
                else
                    insertDataList.Add(new DBItem("RECO_OTH", null, DBItem.DBDataType.String));

                insertDataList.Add(new DBItem("CARE_RECORD", form["CK_CARE_RECORD" + id_list[v]], DBItem.DBDataType.String));

                string where = " IID = '" + id_list[v] + "' ";
                insertDataList = setNullToEmpty(insertDataList);
                erow += link.DBExecUpdateTns("OBS_PIBS", insertDataList, where);
                #region 護理紀錄
                if (form["CK_CARE_RECORD" + id_list[v]] != null && form["CK_CARE_RECORD" + id_list[v]] != "" && form["CK_CARE_RECORD" + id_list[v]] == "1")
                {
                    //TODO CareRecord Title
                    List<string> content = new List<string>();

                    content.Add(form["TXT_CARE_RECORD" + id_list[v]]);
                    //var field = "";

                    //#region 病嬰【原因 + 其他】
                    //field = "病嬰";
                    //if (form["CK_REASON" + id_list[v]] != "" && form["CK_REASON" + id_list[v]] != null)
                    //{
                    //    var reasons = form["CK_REASON" + id_list[v]].Split(',');
                    //    var commaFlag = false;
                    //    foreach (var r in reasons)
                    //    {
                    //        if (r == "0")
                    //            field += (commaFlag == false ? "" : "、") + "哭泣後憋氣";
                    //        else if (r == "1")
                    //            field += (commaFlag == false ? "" : "、") + "熟睡";
                    //        else if (r == "2" && form["TXT_REASON_OTH" + id_list[v]] != "" && form["TXT_REASON_OTH" + id_list[v]] != null)
                    //            field += (commaFlag == false ? "" : "、") + form["TXT_REASON_OTH" + id_list[v]];

                    //        commaFlag = true;
                    //    }
                    //}
                    //else
                    //{
                    //    field += "無";
                    //}
                    //content.Add(field);
                    //#endregion
                    //#region 呼吸暫停時間【呼吸暫停時間】秒
                    //field = "呼吸暫停時間";
                    //if (form["TXT_APNEA" + id_list[v]] != "" && form["TXT_APNEA" + id_list[v]] != null)
                    //    field += form["TXT_APNEA" + id_list[v]];
                    //else
                    //    field += "0";

                    //field += "秒";
                    //content.Add(field);
                    //#endregion
                    //#region 心跳次數【心跳次數 】次 / 分
                    //field = "心跳次數";
                    //if (form["TXT_HBS" + id_list[v]] != "" && form["TXT_HBS" + id_list[v]] != null)
                    //    field += form["TXT_HBS" + id_list[v]];
                    //else
                    //    field += "0";

                    //field += "次 / 分";
                    //content.Add(field);
                    //#endregion
                    //#region SpO2：【SpO2(%)】(%)
                    //field = "SpO2：";
                    //if (form["TXT_SPO2" + id_list[v]] != "" && form["TXT_SPO2" + id_list[v]] != null)
                    //    field += form["TXT_SPO2" + id_list[v]];
                    //else
                    //    field += "0";

                    //field += "(%)";
                    //content.Add(field);
                    //#endregion
                    //#region 恢復呼吸之方法:【恢復呼吸之方法】
                    //field = "恢復呼吸之方法:";
                    //if (form["CK_RECO" + id_list[v]] != "" && form["CK_RECO" + id_list[v]] != null)
                    //{
                    //    var reasons = form["CK_RECO" + id_list[v]].Split(',');
                    //    var commaFlag = false;
                    //    foreach (var r in reasons)
                    //    {
                    //        if (r == "0")
                    //            field += (commaFlag == false ? "" : "、") + "刺激";
                    //        else if (r == "1")
                    //            field += (commaFlag == false ? "" : "、") + "氧氣(O2)";
                    //        else if (r == "2")
                    //            field += (commaFlag == false ? "" : "、") + "正壓換氣(Bag)";
                    //        else if (r == "3")
                    //            field += (commaFlag == false ? "" : "、") + "自動";
                    //        else if (r == "4" && form["TXT_RECO_OTH" + id_list[v]] != "" && form["TXT_RECO_OTH" + id_list[v]] != null)
                    //            field += (commaFlag == false ? "" : "、") + form["TXT_RECO_OTH" + id_list[v]];

                    //        commaFlag = true;
                    //    }
                    //}
                    //else
                    //{
                    //    field += "無";
                    //}
                    //content.Add(field);
                    //#endregion
                    //#region 現心跳: 次 / 分、呼吸: 次 / 分
                    //field = "現心跳:";
                    //if (form["TXT_HBS_NOW" + id_list[v]] != "" && form["TXT_HBS_NOW" + id_list[v]] != null)
                    //    field += form["TXT_HBS_NOW" + id_list[v]];
                    //else
                    //    field += "0";
                    //field += "次 / 分";

                    //field += "、呼吸:";
                    //if (form["TXT_BREATH_NOW" + id_list[v]] != "" && form["TXT_BREATH_NOW" + id_list[v]] != null)
                    //    field += form["TXT_BREATH_NOW" + id_list[v]];
                    //else
                    //    field += "0";
                    //field += "次 / 分";
                    //content.Add(field);
                    //#endregion
                    //#region SpO2：(%)
                    //field = "SpO2：";
                    //if (form["TXT_SPO2_NOW" + id_list[v]] != "" && form["TXT_SPO2_NOW" + id_list[v]] != null)
                    //    field += form["TXT_SPO2_NOW" + id_list[v]];
                    //else
                    //    field += "0";
                    //field += "(%)";
                    //content.Add(field);
                    //#endregion
                    //#region 精神活力
                    //field = "精神活力：";
                    //if (form["TXT_ENERGY_NOW" + id_list[v]] != "" && form["TXT_ENERGY_NOW" + id_list[v]] != null)
                    //    field += form["TXT_ENERGY_NOW" + id_list[v]];
                    //else
                    //    field += "無";
                    //content.Add(field);
                    //#endregion
                    //#region 膚色
                    //field = "膚色：";
                    //if (form["TXT_SKIN_NOW" + id_list[v]] != "" && form["TXT_SKIN_NOW" + id_list[v]] != null)
                    //    field += form["TXT_SKIN_NOW" + id_list[v]];
                    //else
                    //    field += "無";
                    //content.Add(field);
                    //#endregion

                    //content.Add("裝置生理監視器，繼續觀察病嬰的生理變化中");
                    base.Upd_CareRecord(form["TXT_ASMT_TIME_DAY" + id_list[v]] + " " + form["TXT_ASMT_TIME_TIME" + id_list[v]], id_list[v], "嬰兒呼吸暫停紀錄", String.Join("，", content) + "。", "", "", "", "", "OBS_PIBS");
                }
                #endregion
            }

            if (erow == id_list.Length)
            {
                link.DBCommit();
                Response.Write("<script>alert('更新成功');window.opener.location.href='../Obstetrics/Index?active=PIBS_List';window.close();</script>");
            }
            else
            {
                link.DBRollBack();
                Response.Write("<script>alert('更新失敗');window.opener.location.href='../Obstetrics/Index?active=PIBS_List';window.close();</script>");
            }
            return new EmptyResult();
        }
        #endregion

        #region 嬰兒呼吸暫停記錄刪除
        /// <summary>
        /// 嬰兒呼吸暫停記錄刪除
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        [HttpPost]
        public string Del_PIBS()
        {
            string[] id_list = Request["IID"].ToString().Split(',');
            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("DELETED", userinfo.EmployeesNo, DBItem.DBDataType.String));
            string where = " IID IN ('" + String.Join("','", id_list) + "')";
            insertDataList = setNullToEmpty(insertDataList);
            int erow = obs_m.DBExecUpdate("OBS_PIBS", insertDataList, where);

            foreach (var v in id_list)
                //base.Del_CareRecordTns(v, "OBS_PIBS", ref link);
                base.Del_CareRecord(v, "OBS_PIBS");

            return erow.ToString();
        }
        #endregion


        #region 新生兒戒斷系統評估
        /// <summary>
        /// 新生兒戒斷系統評估
        /// </summary>
        /// <returns></returns>
        public ActionResult NASS_List(string feeno, string chartno, bool IsHisView = false)
        {
            ViewBag.IsHisView = IsHisView;

            if (IsHisView)
            {
                his.ControllerContext = ControllerContext;
                his.getSession(feeno);
            }

            if (Session["PatInfo"] != null)
            {
                if (feeno == "" || feeno == null)
                    feeno = ptinfo.FeeNo;
                DataTable dt = obs_m.sel_nass(feeno, "", "");
                ViewBag.dt = set_dt(dt);
                return View();
            }
            Response.Write("<script>alert('請重新選擇病患');</script>");
            return new EmptyResult();
        }
        #endregion

        #region 新生兒戒斷系統評估新增/編輯畫面
        /// <summary>
        /// 新生兒戒斷系統評估新增/編輯畫面
        /// </summary>
        /// <returns></returns>
        public ActionResult Insert_NASS(string IID)
        {
            if (IID != "" && IID != null)
            {
                DataTable dt = obs_m.sel_nass(ptinfo?.FeeNo, "", IID);
                ViewBag.dt = set_dt(dt);
            }
            return View();
        }
        #endregion

        #region 新生兒戒斷系統評估新增

        /// <summary>
        /// 新生兒戒斷系統評估新增
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Insert_NASS(FormCollection form)
        {
            link.DBOpen();
            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;
            string id = base.creatid("OBS_NASS", userno, feeno, "0");
            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("IID", id, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("RECORDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime)); //紀錄時間

            if (form["TXT_ASMT_TIME_DAY"] != "" && form["TXT_ASMT_TIME_DAY"] != null && form["TXT_ASMT_TIME_TIME"] != "" && form["TXT_ASMT_TIME_TIME"] != null)
                insertDataList.Add(new DBItem("ASMT_TIME", form["TXT_ASMT_TIME_DAY"] + " " + form["TXT_ASMT_TIME_TIME"], DBItem.DBDataType.DataTime));

            insertDataList.Add(new DBItem("EV1", form["RB_EV1"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("EV2", form["RB_EV2"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("EV3", form["RB_EV3"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("EV4", form["RB_EV4"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("EV5", form["RB_EV5"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("EV6", form["RB_EV6"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("EV7", form["RB_EV7"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("EV8", form["RB_EV8"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("EV9", form["RB_EV9"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("EV10", form["RB_EV10"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("EV11", form["RB_EV11"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("EV12", form["RB_EV12"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("EV13", form["RB_EV13"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("EV14", form["RB_EV14"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("EV15", form["RB_EV15"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("EV16", form["RB_EV16"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("EV17", form["RB_EV17"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("EV18", form["RB_EV18"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("EV19", form["RB_EV19"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("EV20", form["RB_EV20"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("TOTAL", form["TXT_TOTAL"], DBItem.DBDataType.String));

            insertDataList = setNullToEmpty(insertDataList);
            int erow = link.DBExecInsertTns("OBS_NASS", insertDataList);
            //TODO CareRecord Title
            erow += base.Insert_CareRecordTns(form["TXT_ASMT_TIME_DAY"] + " " + form["TXT_ASMT_TIME_TIME"], id, "新生兒戒斷系統評估量表", $"新生兒戒斷系統評估，總分:{form["TXT_TOTAL"]}分 。", "", "", "", "", "OBS_NASS", ref link);
            if (erow == 2)
            {
                link.DBCommit();
                Response.Write("<script>alert('新增成功');window.opener.location.href='../Obstetrics/Index?active=NASS_List';window.close();</script>");
            }
            else
            {
                link.DBRollBack();
                Response.Write("<script>alert('新增失敗');window.opener.location.href='../Obstetrics/Index?active=NASS_List';window.close();</script>");
            }
            return new EmptyResult();
        }
        #endregion

        #region 新生兒戒斷系統評估編輯
        /// <summary>
        /// 新生兒戒斷系統評估編輯
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        public ActionResult Upd_NASS(FormCollection form)
        {
            link.DBOpen();
            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;
            string[] id_list = form["IID"].Split(',');
            List<DBItem> insertDataList = new List<DBItem>();
            int erow = 0;
            for (int v = 0; v < id_list.Length; v++)
            {
                insertDataList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("RECORDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime)); //紀錄時間

                if (form["TXT_ASMT_TIME_DAY" + id_list[v]] != "" && form["TXT_ASMT_TIME_DAY" + id_list[v]] != null && form["TXT_ASMT_TIME_TIME" + id_list[v]] != "" && form["TXT_ASMT_TIME_TIME" + id_list[v]] != null)
                    insertDataList.Add(new DBItem("ASMT_TIME", form["TXT_ASMT_TIME_DAY" + id_list[v]] + " " + form["TXT_ASMT_TIME_TIME" + id_list[v]], DBItem.DBDataType.DataTime));

                insertDataList.Add(new DBItem("EV1", form["RB_EV1" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EV2", form["RB_EV2" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EV3", form["RB_EV3" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EV4", form["RB_EV4" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EV5", form["RB_EV5" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EV6", form["RB_EV6" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EV7", form["RB_EV7" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EV8", form["RB_EV8" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EV9", form["RB_EV9" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EV10", form["RB_EV10" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EV11", form["RB_EV11" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EV12", form["RB_EV12" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EV13", form["RB_EV13" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EV14", form["RB_EV14" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EV15", form["RB_EV15" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EV16", form["RB_EV16" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EV17", form["RB_EV17" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EV18", form["RB_EV18" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EV19", form["RB_EV19" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("EV20", form["RB_EV20" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("TOTAL", form["TXT_TOTAL" + id_list[v]], DBItem.DBDataType.String));

                string where = " IID = '" + id_list[v] + "' ";
                insertDataList = setNullToEmpty(insertDataList);
                erow += link.DBExecUpdateTns("OBS_NASS", insertDataList, where);
                //TODO CareRecord Title
                erow += base.Upd_CareRecord(form["TXT_ASMT_TIME_DAY" + id_list[v]] + " " + form["TXT_ASMT_TIME_TIME" + id_list[v]], id_list[v], "新生兒戒斷系統評估量表", $"新生兒戒斷系統評估，總分:{form["TXT_TOTAL" + id_list[v]]}分 。", "", "", "", "", "OBS_NASS");
            }
            if (erow == id_list.Length * 2)
            {
                link.DBCommit();
                Response.Write("<script>alert('更新成功');window.opener.location.href='../Obstetrics/Index?active=NASS_List';window.close();</script>");
            }
            else
            {
                link.DBRollBack();
                Response.Write("<script>alert('更新失敗');window.opener.location.href='../Obstetrics/Index?active=NASS_List';window.close();</script>");
            }
            return new EmptyResult();
        }
        #endregion

        #region 新生兒戒斷系統評估刪除
        /// <summary>
        /// 新生兒戒斷系統評估刪除
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        [HttpPost]
        public string Del_NASS()
        {
            string[] id_list = Request["IID"].ToString().Split(',');
            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("DELETED", userinfo.EmployeesNo, DBItem.DBDataType.String));
            string where = " IID IN ('" + String.Join("','", id_list) + "')";
            insertDataList = setNullToEmpty(insertDataList);
            int erow = obs_m.DBExecUpdate("OBS_NASS", insertDataList, where);
            foreach (var v in id_list)
                //base.Del_CareRecordTns(v, "OBS_NASS", ref link);
                base.Del_CareRecord(v, "OBS_NASS");
            return erow.ToString();
        }
        #endregion


        #region 愛丁堡產後憂鬱症評估
        /// <summary>
        /// 愛丁堡產後憂鬱症評估
        /// </summary>
        /// <returns></returns>
        public ActionResult EPDS_List(string feeno, string chartno, bool IsHisView = false)
        {
            ViewBag.IsHisView = IsHisView;

            if (IsHisView)
            {
                his.ControllerContext = ControllerContext;
                his.getSession(feeno);
            }

            if (Session["PatInfo"] != null)
            {
                if (feeno == "" || feeno == null)
                    feeno = ptinfo.FeeNo;

                DataTable dt = obs_m.sel_epds(feeno, "", "");
                dt.Columns.Add("CREATNO_NAME");
                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow r in dt.Rows)
                    {
                        if (r["CREATNO"].ToString() != "")
                        {
                            byte[] listByteCode = webService.UserName(r["CREATNO"].ToString());
                            if (listByteCode == null)
                                r["CREATNO_NAME"] = "";
                            else
                            {
                                string listJsonArray = CompressTool.DecompressString(listByteCode);
                                UserInfo user_name = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                                r["CREATNO_NAME"] = user_name.EmployeesName;
                            }
                        }
                    }
                }
                ViewBag.dt = set_dt(dt);
                return View();
            }
            Response.Write("<script>alert('請重新選擇病患');</script>");
            return new EmptyResult();
        }
        #endregion

        #region 愛丁堡產後憂鬱症評估新增/編輯畫面
        /// <summary>
        /// 愛丁堡產後憂鬱症評估新增/編輯畫面
        /// </summary>
        /// <returns></returns>
        public ActionResult Insert_EPDS(string IID)
        {
            if (IID != "" && IID != null)
            {
                DataTable dt = obs_m.sel_epds(ptinfo?.FeeNo, "", IID);
                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow r in dt.Rows)
                    {
                        if (r["CREATNO"].ToString().Trim() != userinfo.EmployeesNo)
                        {
                            Response.Write("<script>alert('無權限修改!');window.close();</script>");
                            return new EmptyResult();
                        }
                    }
                }
                ViewBag.dt = set_dt(dt);
            }
            return View();
        }
        #endregion

        #region 愛丁堡產後憂鬱症評估新增
        /// <summary>
        /// 愛丁堡產後憂鬱症評估新增
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Insert_EPDS(FormCollection form)
        {
            link.DBOpen();
            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;
            string id = base.creatid("OBS_EPDS", userno, feeno, "0");
            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("IID", id, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("FEENO", feeno, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATNO", userno, DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("CREATTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
            insertDataList.Add(new DBItem("RECORDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime)); //紀錄時間

            if (form["TXT_ASMT_TIME_DAY"] != "" && form["TXT_ASMT_TIME_DAY"] != null && form["TXT_ASMT_TIME_TIME"] != "" && form["TXT_ASMT_TIME_TIME"] != null)
                insertDataList.Add(new DBItem("ASMT_TIME", form["TXT_ASMT_TIME_DAY"] + " " + form["TXT_ASMT_TIME_TIME"], DBItem.DBDataType.DataTime));

            insertDataList.Add(new DBItem("Q1", form["RB_Q1"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("Q2", form["RB_Q2"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("Q3", form["RB_Q3"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("Q4", form["RB_Q4"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("Q5", form["RB_Q5"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("Q6", form["RB_Q6"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("Q7", form["RB_Q7"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("Q8", form["RB_Q8"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("Q9", form["RB_Q9"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("Q10", form["RB_Q10"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("SCORE", form["TXT_SCORE"], DBItem.DBDataType.String));
            insertDataList.Add(new DBItem("MEMO", form["TXT_MEMO"], DBItem.DBDataType.String));

            insertDataList = setNullToEmpty(insertDataList);

            int erow = link.DBExecInsertTns("OBS_EPDS", insertDataList);

            #region 護理紀錄
            var title = "愛丁堡產後憂鬱症評估";
            var content = "";
            content = $"總分為{form["TXT_SCORE"].ToString()}分，{form["TXT_MEMO"].ToString()}";
            erow += base.Insert_CareRecord_BlackTns(Convert.ToDateTime(form["TXT_ASMT_TIME_DAY"] + " " + form["TXT_ASMT_TIME_TIME"]).ToString("yyyy/MM/dd HH:mm:ss"), id, title, content, "", "", "", "", ref link);

            #endregion
            if (erow == 2)
            {
                link.DBCommit();
                Response.Write("<script>alert('新增成功');window.opener.location.href='../Obstetrics/Index?active=EPDS_List';window.close();</script>");
            }
            else
            {
                link.DBRollBack();
                Response.Write("<script>alert('新增失敗');window.opener.location.href='../Obstetrics/Index?active=EPDS_List';window.close();</script>");
            }

            return new EmptyResult();
        }
        #endregion

        #region 愛丁堡產後憂鬱症評估編輯

        /// <summary>
        /// 愛丁堡產後憂鬱症評估編輯
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        public ActionResult Upd_EPDS(FormCollection form)
        {
            link.DBOpen();

            string userno = userinfo.EmployeesNo;
            string feeno = ptinfo.FeeNo;
            string[] id_list = form["IID"].Split(',');
            List<DBItem> insertDataList = new List<DBItem>();
            int erow = 0;
            for (int v = 0; v < id_list.Length; v++)
            {
                insertDataList.Add(new DBItem("UPDNO", userno, DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("UPDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime));
                insertDataList.Add(new DBItem("RECORDTIME", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), DBItem.DBDataType.DataTime)); //紀錄時間

                if (form["TXT_ASMT_TIME_DAY" + id_list[v]] != "" && form["TXT_ASMT_TIME_DAY" + id_list[v]] != null && form["TXT_ASMT_TIME_TIME" + id_list[v]] != "" && form["TXT_ASMT_TIME_TIME" + id_list[v]] != null)
                    insertDataList.Add(new DBItem("ASMT_TIME", form["TXT_ASMT_TIME_DAY" + id_list[v]] + " " + form["TXT_ASMT_TIME_TIME" + id_list[v]], DBItem.DBDataType.DataTime));

                insertDataList.Add(new DBItem("Q1", form["RB_Q1" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("Q2", form["RB_Q2" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("Q3", form["RB_Q3" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("Q4", form["RB_Q4" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("Q5", form["RB_Q5" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("Q6", form["RB_Q6" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("Q7", form["RB_Q7" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("Q8", form["RB_Q8" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("Q9", form["RB_Q9" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("Q10", form["RB_Q10" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("SCORE", form["TXT_SCORE" + id_list[v]], DBItem.DBDataType.String));
                insertDataList.Add(new DBItem("MEMO", form["TXT_MEMO" + id_list[v]], DBItem.DBDataType.String));


                string where = " IID = '" + id_list[v] + "' ";
                insertDataList = setNullToEmpty(insertDataList);
                erow += link.DBExecUpdateTns("OBS_EPDS", insertDataList, where);

                #region 護理紀錄
                var title = "愛丁堡產後憂鬱症評估";
                var content = "";
                content = $"總分為{form["TXT_SCORE" + id_list[v]].ToString()}分，{form["TXT_MEMO" + id_list[v]]}";
                erow += base.Insert_CareRecord_BlackTns(Convert.ToDateTime(form["TXT_ASMT_TIME_DAY" + id_list[v]] + " " + form["TXT_ASMT_TIME_TIME" + id_list[v]]).ToString("yyyy/MM/dd HH:mm"), id_list[v], title, content, "", "", "", "", ref link);
                #endregion
            }
            if (erow == id_list.Length * 2)
            {
                link.DBCommit();
                Response.Write("<script>alert('更新成功');window.opener.location.href='../Obstetrics/Index?active=EPDS_List';window.close();</script>");
            }
            else
            {
                link.DBRollBack();
                Response.Write("<script>alert('更新失敗');window.opener.location.href='../Obstetrics/Index?active=EPDS_List';window.close();</script>");
            }

            return new EmptyResult();
        }

        #endregion


        #region 值不為空回傳True，否則為False
        public bool IsNoEmpty(string str)
        {
            if (str != null && str != "")
                return true;
            else
                return false;
        }
        #endregion

        #region 資料Null轉為空
        private List<DBItem> setNullToEmpty(List<DBItem> data)
        {
            foreach (DBItem d in data)
                if (d.Value == null && d.DataType == DBItem.DBDataType.String)
                    d.Value = "";

            return data;
        }
        #endregion

        #region set_dt
        private DataTable set_dt(DataTable dt)
        {
            if (dt.Rows.Count > 0)
            {
                dt.Columns.Add("username");

                string userno = "";
                UserInfo user_name = new UserInfo();
                foreach (DataRow r in dt.Rows)
                {
                    if (r["UPDNO"].ToString() != "")
                    {
                        if (userno != r["UPDNO"].ToString())
                        {
                            userno = r["UPDNO"].ToString();
                            byte[] listByteCode = webService.UserName(userno);
                            string listJsonArray = CompressTool.DecompressString(listByteCode);
                            user_name = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                        }
                        r["username"] = user_name.EmployeesName;
                    }
                }
            }
            return dt;
        }
        #endregion

        //退回暫存
        public ActionResult BackAssessment(string tableid, string natype)
        {
            List<DBItem> insertDataList = new List<DBItem>();
            insertDataList.Add(new DBItem("STATUS", "暫存", DBItem.DBDataType.String));
            int erow = 0;
            if (natype == "BabyBorn")
            {
                erow = obs_m.DBExecUpdate("OBS_BABYENTR", insertDataList, "IID = '" + tableid + "' ");

            }
            else
            {
                erow = obs_m.DBExecUpdate("OBS_NBENTR", insertDataList, "IID = '" + tableid + "' ");
            }


            if (erow > 0)
            {
                if (natype == "BabyBorn")
                {
                    Response.Write("<script>alert('退回成功');window.location.href='BabyEntr_List';</script>");
                }
                else
                {
                    Response.Write("<script>alert('退回成功');window.location.href='NBENTR_List';</script>");
                }
            }
            else
            {
                if (natype == "BabyBorn")
                {
                    Response.Write("<script>alert('退回失敗');window.location.href='BabyEntr_List';</script>");
                }
                else
                {
                    Response.Write("<script>alert('退回失敗');window.location.href='NBENTR_List';</script>");
                }
            }
            return new EmptyResult();
        }
    }
}
