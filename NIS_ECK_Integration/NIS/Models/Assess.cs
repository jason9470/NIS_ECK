using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NIS.Models;
using System.Data;
using System.Text.RegularExpressions;
using NIS.Controllers;
using Com.Mayaminer;

namespace NIS.Models
{
    public class Assess : DBConnector
    {
        private BaseController baseC = new BaseController();
        /// <summary> 取得高危_DT </summary>
        public DataTable sel_danger_data_dt(string feeno, string id, string starttime, string endtime)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM NIS_DANGER_DATA WHERE 0 = 0 ";
            if(id != "")
                sql += "AND DANGER_ID = '" + id + "' ";
            if(feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if(starttime != "")
                sql += "AND RECORDTIME BETWEEN to_date('" + starttime + "','yyyy/mm/dd hh24:mi:ss') AND to_date('" + endtime + "','yyyy/mm/dd hh24:mi:ss') ";

            sql += "AND DELETED IS NULL ORDER BY RECORDTIME DESC";

            base.DBExecSQL(sql, ref dt);
            return dt;
        }

        /// <summary> 取得高危_Reader </summary>
        public DataTable sel_danger_data_r(string feeno, string id, string starttime, string endtime)
        {
            DataTable dt = null;
            string sql = "SELECT * FROM NIS_DANGER_DATA WHERE 0 = 0 ";
            if(id != "")
                sql += "AND DANGER_ID = '" + id + "' ";
            if(feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if(starttime != "")
                sql += "AND RECORDTIME BETWEEN to_date('" + starttime + "','yyyy/mm/dd hh24:mi:ss') AND to_date('" + endtime + "','yyyy/mm/dd hh24:mi:ss') ";

            sql += "AND DELETED IS NULL ORDER BY RECORDTIME DESC";

            base.DBExecSQL(sql, ref dt);
            return dt;
        }

        public DataTable sel_team_care_data(string feeno)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM NIS_TEAM_CARE_DATA WHERE 0 = 0 ";
            if(feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";

            base.DBExecSQL(sql, ref dt);
            return dt;
        }

        public DataTable sel_fall_assess_data(string feeno, string id, string tablename)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM " + tablename + " WHERE 0 = 0 ";
            if(id != "")
                sql += "AND FALL_ID = '" + id + "' ";
            if(feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";

            sql += "AND DELETED IS NULL ORDER BY RECORDTIME DESC";

            base.DBExecSQL(sql, ref dt);
            return dt;
        }

        //jarvis add 區間查詢----20160823---
        public DataTable sel_fall_assess_data_date(string feeno, string id, string tablename, string startdate, string enddate)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM " + tablename + " WHERE 0 = 0 ";
            if(id != "")
                sql += "AND FALL_ID = '" + id + "' ";
            if(feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            sql += "AND RECORDTIME BETWEEN to_date('" + startdate + "','yyyy/mm/dd hh24:mi:ss') AND to_date('" + enddate + "','yyyy/mm/dd hh24:mi:ss') ";
            sql += "AND DELETED IS NULL ORDER BY RECORDTIME DESC";

            base.DBExecSQL(sql, ref dt);
            return dt;
        }

        //搜尋最後一筆跌倒評估日期
        public string sel_last_fall_assess_time(string feeno, string tablename)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM (SELECT RECORDTIME FROM " + tablename + " WHERE 0 = 0 ";
            if(feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            sql += "AND DELETED IS NULL ORDER BY RECORDTIME DESC) WHERE ROWNUM >= 1 ";

            base.DBExecSQL(sql, ref dt);
            if(dt.Rows.Count > 0)
                return dt.Rows[0]["RECORDTIME"].ToString();
            else
                return "";
        }

        //搜尋最後一筆跌倒評估日期+分數
        public string sel_last_fall_assess_time_And_total(string feeno, string tablename)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM (SELECT RECORDTIME,TOTAL FROM " + tablename + " WHERE 0 = 0 ";
            if(feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            sql += "AND DELETED IS NULL ORDER BY RECORDTIME DESC) WHERE ROWNUM >= 1 ";

            base.DBExecSQL(sql, ref dt);
            if(dt.Rows.Count > 0)
            {
                string temp_data = dt.Rows[0]["RECORDTIME"].ToString() + "|" + dt.Rows[0]["TOTAL"].ToString();
                return temp_data;
            }
            else
                return "";
        }

        public DataTable sel_fall_assess_check(string feeno, string fid, string id, string tablename)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT NIS_FALL_ASSESS_CHECK.*, ";
            sql += "(SELECT NUM FROM " + tablename + " WHERE FALL_ID = NIS_FALL_ASSESS_CHECK.FALL_ID)NUM ";
            sql += "FROM NIS_FALL_ASSESS_CHECK WHERE 0 = 0 ";
            if(fid != "")
                sql += "AND FALL_ID = '" + fid + "' ";
            if(id != "")
                sql += "AND CHECK_ID = '" + id + "' ";
            if(feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";

            sql += "AND DELETED IS NULL ORDER BY RECORDTIME DESC";

            base.DBExecSQL(sql, ref dt);
            return dt;
        }

        public DataTable sel_leave_referral_data(string feeno)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM NIS_LEAVE_REFERRAL_DATA WHERE 0 = 0 ";
            if(feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";

            base.DBExecSQL(sql, ref dt);
            return dt;
        }

        public string sel_health_education_item(string feeno, string into_type)
        {
            DataTable dt = new DataTable();
            string item_id = "";
            string sql = "SELECT ITEM_ID FROM HEALTH_EDUCATION_ITEM_DATA WHERE 0 = 0 ";
            if(into_type != "")
                sql += "AND INTO_TYPE = '" + into_type + "' ";
            base.DBExecSQL(sql, ref dt);

            if(dt.Rows.Count > 0)
            {
                item_id = dt.Rows[0]["ITEM_ID"].ToString();
                sql = "SELECT * FROM HEALTH_EDUCATION_DATA WHERE FEENO = '" + feeno + "' AND ITEMID = '" + item_id + "' ";
                dt.Reset();
                base.DBExecSQL(sql, ref dt);
                if(dt != null && dt.Rows.Count > 0)
                    item_id = "";
            }

            return item_id;
        }

        public DataTable sel_assessment_list(string feeno, string na_type)
        {
            DataTable dt = new DataTable();

            string sql = "SELECT * FROM ASSESSMENTMASTER WHERE 0 = 0 ";
            if(feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if(na_type != "")
                sql += "AND NATYPE = '" + na_type + "' ";
            sql += "ORDER BY CREATETIME DESC";

            base.DBExecSQL(sql, ref dt);
            return dt;
        }

        public DataTable sel_assessment_list_for_elec_sign(string id, string userno, string starttime, string endtime, string sign)
        {
            DataTable dt = new DataTable();

            string sql = "SELECT * FROM ASSESSMENTMASTER WHERE 0 = 0 ";
            if(id != "")
                sql += "AND TABLEID IN " + id + " ";
            if(userno != "")
                sql += "AND MODIFYUSER = '" + userno + "' ";
            if(starttime != "")
                sql += "AND MODIFYTIME BETWEEN to_date('" + starttime + "','yyyy/mm/dd hh24:mi:ss') AND to_date('" + endtime + "','yyyy/mm/dd hh24:mi:ss') ";
            if(sign != "" && sign == "N")
                sql += "AND SIGN <> 'Y' ";
            if(sign != "" && sign == "Y")
                sql += "AND SIGN = '" + sign + "' ";
            sql += "AND STATUS IN('insert','update') ORDER BY FEENO, MODIFYTIME DESC";

            base.DBExecSQL(sql, ref dt);
            return dt;
        }

        public DataTable sel_assessment_contnet(string tableid)
        {
            DataTable dt = new DataTable();

            string sql = "SELECT * FROM ASSESSMENTDETAIL WHERE 0 = 0 ";
            if(tableid != "")
            {
                sql += "AND TABLEID = '" + tableid + "' ";
            }
            sql += " order by SERIAL ";///增加排序
            base.DBExecSQL(sql, ref dt);
            return dt;
        }
        public DataTable sel_assessment_clock(string tableid)
        {
            DataTable dt = new DataTable();

            string sql = "SELECT * FROM ASSESSMENTOBJ WHERE  ";
            if (tableid != "")
            {
                sql += "TABLEID = '" + tableid + "'AND ITEMID = 'Obj_Clock' AND DEL IS NULL";
            }
            base.DBExecSQL(sql, ref dt);
            return dt;
        }

        //有完成的入院評估後，在執行後，再按新增，使用此取得病人完成評估的資料//20160629 by jarvis
        public DataTable sel_assessment_contnet_CarryOutInsert(string tableid, string ntype)
        {
            DataTable dt = new DataTable();

            string sql = "SELECT * FROM ASSESSMENTDETAIL WHERE 0 = 0 ";
            if(tableid != "")
            {
                sql += "AND TABLEID = '" + tableid + "' ";
            }
            if(ntype == "A")
            {
                sql += "AND ITEMID IN ('param_assessment','param_assessment_other','param_tube_date','param_tube_time','param_assessment','param_assessment_turn','param_assessment_other','param_ipd_reason','param_ipd_source','param_ipd_style','param_ipd_style_other',";
                sql += "'param_education','param_religion','param_religion_other','param_job','param_marrage','param_child_f','param_child_m','param_living','param_living_other','param_ph_home','param_ph_office','param_ph_number',";
                sql += "'param_EMGContact','param_ContactRole','param_ContactRole_other','param_EMGContact_1','param_EMGContact_2','param_EMGContact_3',";
                sql += "'param_ContactRole_1','param_ContactRole_2','param_ContactRole_3','param_ContactRole_4','param_ContactRole_5','param_ContactRole_6','param_ContactRole_7','param_ContactRole_8','param_ContactRole_9','param_ContactRole_10',";
                sql += "'param_hasipd','param_ipd_past_reason','param_ipd_past_location','param_surgery','param_ipd_surgery_reason','param_ipd_surgery_location','param_blood','param_blood_reaction','transfusion_blood_dtl_txt','param_transfusion_blood_allergy','param_allergy_med',";
                sql += "'param_allergy_med_other','param_allergy_med_other_2_name','param_allergy_med_other_4_name','param_allergy_med_other_6_name',";
                sql += "'param_allergy_med_other_7_name','param_allergy_med_other_8_name','param_allergy_med_other_9_name','param_allergy_med_other_10_name','param_allergy_food','param_allergy_food_other','param_allergy_food_other_2_name',";
                sql += "'param_allergy_food_other_4_name','param_allergy_food_other_6_name','param_allergy_other','param_allergy_other_other','param_allergy_other_other_1_name','param_allergy_other_other_2_name','param_allergy_other_other_3_name',";
                sql += "'param_allergy_other_other_4_name','param_allergy_other_other_5_name','param_allergy_other_other_6_name','param_im_history','param_im_history_item_other','param_im_history_item_other_txt','param_med','param_med_name'";
                sql += ",'param_language','param_lang','param_lang_other','param_lang_no','param_lang_other_no','param_food_drink','param_food_drink_eschew_text'";
                sql += ",'param_cigarette','param_cigarette_stop_year','param_cigarette_yes_amount','param_cigarette_yes_year','param_cigarette_tutor','param_drink'";
                sql += ",'param_drink_stop_year','param_drink_yes_amount','param_drink_yes_year','param_areca','param_areca_stop_year','param_areca_tutor'";
                sql += ",'param_Sleeping_text','param_Sleeping','param_Sleeping_abnormal','param_sleeping_abnormal_other'";
                sql += ",'param_eating','param_garb','param_eating_self','param_bathe','param_toilet','param_bed_activities'";
                sql += ")";
            }
            else if(ntype == "C")
            {//**
                sql += "AND ITEMID IN ('param_tube_date','param_tube_time','param_ipd_reason','param_ipd_source','param_ipd_style','param_ipd_style_other','param_ipd_depiction','param_allergy_med','param_allergy_med_other'";
                sql += ",'param_allergy_med_other_2_name','param_allergy_med_other_4_name','param_allergy_med_other_6_name','param_allergy_med_other_7_name'";
                sql += ",'param_allergy_med_other_8_name','param_allergy_med_other_9_name','param_allergy_med_other_10_name','param_allergy_food','param_allergy_food_other'";
                sql += ",'param_allergy_food_other_2_name','param_allergy_food_other_4_name','param_allergy_food_other_6_name'";
                sql += ",'param_allergy_other','param_allergy_other_other','param_allergy_other_other_6_name'";
                sql += ",'param_primary_care','param_primary_father_name','param_primary_father_age','param_primary_father_jop','param_father_education'";
                sql += ",'param_primary_mother_name','param_primary_mother_age','param_primary_mother_jop','param_mother_education'";
                sql += ",'param_source','param_EMGContact','param_ContactRole','param_ContactRole_other','param_EMGContact_1','param_EMGContact_2','param_EMGContact_3'";
                sql += ",'param_ContactRole_1','param_ContactRole_2','param_ContactRole_3','param_ContactRole_4','param_ContactRole_5','param_ContactRole_6','param_ContactRole_7','param_ContactRole_8','param_ContactRole_9','param_ContactRole_10'";
                //個人病史
                sql += ",'param_im_history','param_im_history_item_other','param_im_history_item_txt','param_im_history_item_other_txt'";//曾患疾病
                sql += ",'param_hasipd','param_ipd_past_count','param_ipd_past_reason','param_ipd_past_location'";//住院次數
                sql += ",'param_surgery','param_ipd_surgery_count','param_ipd_surgery_reason','param_ipd_surgery_location'";//手術情形
                sql += ",'param_blood','param_blood_reaction','transfusion_blood_dtl_txt','param_take_medicine','param_take_medicine_dtl_txt'";//輸血經驗、服藥
                sql += ",'param_Congenital_disease','Congenital_disease_dtl','Congenital_disease_dtl_other'";//先天疾患
                //預防接種
                sql += ",'param_HepatitisB','param_HepatitisB1','param_HepatitisB2','param_HepatitisB3','param_5in1Vaccine1','param_5in1Vaccine2','param_5in1Vaccine3','param_5in1Vaccine4'"; //B型肝炎、五合一疫苗
                sql += ",'param_MMR1','param_MMR2','param_VaricellaVaccine','param_JP_encephalitis1','param_JP_encephalitis2','param_JP_encephalitis3','param_JP_encephalitis4'";//MMR、水痘疫苗、日本腦炎疫苗
                sql += ",'param_DPT','param_InfluenzaVaccination'";//DPT及小兒麻痺混合疫苗、流感疫苗
                sql += ",'param_Stre_PneumococcalVaccine1','param_Stre_PneumococcalVaccine2','param_Stre_PneumococcalVaccine3','param_Stre_PneumococcalVaccine4'";//肺炎鏈球菌疫苗
                sql += ",'param_Hepatitis_A1','param_SHepatitis_A2','param_Rotavirus1','param_Rotavirus2','VaccineName'";//A型肝炎疫苗、自費項目輪狀病毒疫苗、其他
                //日常生活
                sql += ",'param_Diet','param_Diet_other','param_Diet_type','param_Diet_type_other','param_Sleep_Habits'";//飲食方式種類、睡眠習慣
                sql += ",'param_Voiding_patterns','param_Excretion_patterns','param_Defecation_D','param_Defecation_F'";//解尿型態、排泄型態、排便次數
                sql += ",'param_Color_defecation','param_Color_defecation_other','param_Bowel_defecation','param_Bowel_defecation_other'";//排便顏色、排便性狀
                sql += ",'param_Daily_activities','param_Daily_activities_other'";//日常生活
                sql += ",'param_Behavioral_Development','param_Behavioral_Development_YN','param_Behavioral_Development_txt'";//行為發展
                sql += ")";
            }
            base.DBExecSQL(sql, ref dt);
            return dt;
        }

        public DataTable get_assessment_user(string tableid)
        {
            DataTable dt = new DataTable();

            string sql = "SELECT * FROM assessmentmaster where tableid = '" + tableid + "' ";

            base.DBExecSQL(sql, ref dt);
            return dt;
        }

        public DataTable get_vs(string feeno, string vs)
        {
            DataTable dt = new DataTable();

            string sql = "select * from (select * from DATA_VITALSIGN where fee_no ='" + feeno + "' and vs_item = '" + vs + "' order by modify_date asc) where rownum = 1";

            base.DBExecSQL(sql, ref dt);
            return dt;
        }

        /// <summary>
        /// 取得兒童發展評估
        /// </summary>
        public DataTable sel_child_develope(string feeno)
        {
            DataTable dt = new DataTable();
            if(feeno != "")
            {
                string sql = "SELECT ASSESS_ITEM,SEQ_NO, ";
                sql += "(SELECT * FROM (SELECT ITEM_VALUE FROM NIS_ASSESSMENTCHILD WHERE ";
                sql += "FEE_NO = '" + feeno + "' ORDER BY MODIFY_DATE DESC) WHERE rownum <=1) VAL ";
                sql += "FROM NIS_ASSESSMENTCHILD_DTL DTL WHERE ";
                sql += "DTL.SEQ_NO = ";
                sql += "(SELECT * FROM (SELECT SEQ_NO FROM NIS_ASSESSMENTCHILD WHERE ";
                sql += "FEE_NO = '" + feeno + "' ORDER BY MODIFY_DATE DESC) WHERE rownum <=1)";

                base.DBExecSQL(sql, ref dt);
            }
            return dt;
        }
        public static String SQLString(String ColString, bool IsRegex = false, bool Num = false)
        {
            if(ColString != null)
            {
                if(IsRegex)
                {
                    //   \ to \\
                    ColString = Regex.Replace(ColString, "[^\\w\\~～ 。@!！$＄%％^︿&＆*＊(（）)'＼／’]", "");
                }

                int _find = ColString.IndexOf("'");
                if(_find >= 0)
                {
                    ColString = ColString.Replace("'", "''");

                }
                return "'" + ColString.ToString().Trim() + "'";
            }
            else
            {
                if(Num) return "0";
                return "null";
            }
        }
        /// <summary>
        /// 取得入評中的傷口種類及部位
        /// </summary>
        public DataTable sel_type_list(string PGroup, string PModel)
        {
            DataTable dt = new DataTable();
            string sql = string.Format("SELECT A.P_NAME, A.P_VALUE FROM SYS_PARAMS A WHERE A.P_GROUP={0} AND A.P_MODEL={1} ORDER BY A.P_SORT ", SQLString(PGroup), SQLString(PModel));
            base.DBExecSQL(sql, ref dt);
            return dt;
        }

        /// <summary>
        /// 取得入評中的管路名稱
        /// </summary>
        public DataTable sel_tube_list()
        {
            DataTable dt = new DataTable();
            string sql = string.Format("select kindname,kindid from TUBE_KIND where other='0' ");
            base.DBExecSQL(sql, ref dt);
            return dt;
        }

        /// <summary>
        /// 取得當筆管路資訊
        /// </summary>
        /// <remarks>2016/06/03 Vanda Add</remarks>
        public DataTable GetCurrentTubeInfo(string pTube)
        {
            DataTable dt = new DataTable();
            string sql = string.Format("SELECT * FROM TUBE_KIND WHERE KINDID='{0}'", pTube);
            base.DBExecSQL(sql, ref dt);
            return dt;
        }


        /// <summary>
        /// 丟進性別跟年齡的條件
        /// </summary>
        /// <param name="sex"></param>
        /// <param name="age"></param>
        /// <returns>回傳該條件的BMI範圍值datatable</returns>
        public DataTable GetBMISort(string sex, float age = 0.0F)
        {
            DataTable dt = new DataTable();
            string sqlStr = string.Empty;
            if (!string.IsNullOrEmpty(sex) && age != 0.0F)
            {
                try
                {
                    sqlStr = "SELECT * ";
                    sqlStr += " FROM (SELECT * FROM DATA_CHILD_BMI_BASIC WHERE SEX='" + sex + "' AND AGE<=" + age + " ORDER BY AGE DESC )";
                    sqlStr += " WHERE ROWNUM = 1";
                    base.DBExecSQL(sqlStr, ref dt);
                }
                catch (Exception ex)
                {    
                 //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                    string tmp_controller = baseC.ControllerContext.RouteData.Values["controller"].ToString();
                    string tmp_action = baseC.ControllerContext.RouteData.Values["action"].ToString();
                    write_logMsg(tmp_controller, baseC.userinfo.EmployeesNo, tmp_action, ex.ToString(),ex);
                }
            }
            return dt;
        }
       
        /// <summary>
        /// 兒童入評，依性別及歲數，判斷出，患者所擁有的BMI，是什麼類別評語
        /// </summary>
        /// <param name="dt">取這個歲數跟性別的，BMI條件</param>
        /// <param name="BMI_val">患者經換算後的BMI值</param>
        /// <returns>回傳評語</returns>
        public string Get_BMI_String(DataTable dt, string BMI_val)
        {
            string Str = "";
            if(dt != null && dt.Rows.Count > 0)
            {
                if(Convert.ToDouble(BMI_val) >= Convert.ToDouble(dt.Rows[0]["BMI_FAT"]))
                {
                    Str = "肥胖";
                }
                else if(Convert.ToDouble(BMI_val) >= Convert.ToDouble(dt.Rows[0]["BMI_WEIGHT"]))
                {
                    Str = "過重";
                }
                else if(Convert.ToDouble(BMI_val) <= Convert.ToDouble(dt.Rows[0]["BMI_LIGHT"]))
                {
                    Str = "過輕";
                }
                else
                {
                    Str = "正常";
                }
            }
            return Str;
        }
        
           

    }
}
