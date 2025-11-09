using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NIS.Models;
using System.Data;
using NIS.Controllers;
using NIS.UtilTool;
using Newtonsoft.Json;
using NIS.Data;

namespace NIS.Models
{
    public class Function : DBConnector
    {
        private BaseController baseC = new BaseController();
        private DBConnector link;
        private DBConnector2 link2;

        //建構子
        public Function()
        {
            this.link = new DBConnector();
            this.link2 = new DBConnector2();
        }
        /// <summary>
        /// 新增
        /// </summary>
        /// <returns>傳回成功筆數</returns>
        public int insert(string TableName, DataTable dt)
        {
            int eftrow = 0;
            try
            {
                List<DBItem> insertDataList = new List<DBItem>();
                for (int i = 1; i < dt.Rows.Count; i++)
                {
                    for (int j = 0; j < dt.Columns.Count; j++)
                    {
                        if (dt.Rows[0][j].ToString() == "String")
                            insertDataList.Add(new DBItem(dt.Columns[j].ToString(), dt.Rows[i][j].ToString(), DBItem.DBDataType.String));
                        else if (dt.Rows[0][j].ToString() == "DataTime")
                            insertDataList.Add(new DBItem(dt.Columns[j].ToString(), dt.Rows[i][j].ToString(), DBItem.DBDataType.DataTime));
                        else if (dt.Rows[0][j].ToString() == "Number")
                            insertDataList.Add(new DBItem(dt.Columns[j].ToString(), dt.Rows[i][j].ToString(), DBItem.DBDataType.Number));
                    }
                    if (TableName == "HEALTH_EDUCATION_ITEM_DATA")
                    {
                        eftrow = eftrow + link2.DBExecInsert(TableName, insertDataList);
                    }
                    else
                    {
                        eftrow = eftrow + link.DBExecInsert(TableName, insertDataList);
                    }
                    insertDataList.Clear();
                }

            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = baseC.ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = baseC.ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, baseC.userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }
            finally
            {
                if (TableName == "HEALTH_EDUCATION_ITEM_DATA")
                {
                    this.link2.DBClose();
                }
                else
                {
                    this.link.DBClose();
                }
            }

            return eftrow;
        }

        /// <summary>
        /// 更新
        /// </summary>
        public int upd(string TableName, DataTable dt)
        {
            int eftrow = 0;
            try
            {            
                List<DBItem> insertDataList = new List<DBItem>();
                for (int i = 1; i < dt.Rows.Count; i++)
                {
                    for (int j = 0; j < dt.Columns.Count - 1; j++)
                    {
                        if (dt.Rows[0][j].ToString() == "String")
                            insertDataList.Add(new DBItem(dt.Columns[j].ToString(), dt.Rows[i][j].ToString(), DBItem.DBDataType.String));
                        else if (dt.Rows[0][j].ToString() == "DataTime")
                            insertDataList.Add(new DBItem(dt.Columns[j].ToString(), dt.Rows[i][j].ToString(), DBItem.DBDataType.DataTime));
                        else if (dt.Rows[0][j].ToString() == "Number")
                            insertDataList.Add(new DBItem(dt.Columns[j].ToString(), dt.Rows[i][j].ToString(), DBItem.DBDataType.Number));
                    }

                    if(TableName == "HEALTH_EDUCATION_ITEM_DATA")
                    {
                        eftrow = eftrow + link2.DBExecUpdate(TableName, insertDataList, dt.Rows[i][dt.Columns.Count - 1].ToString());
                    }
                    else
                    {
                        eftrow = eftrow + base.DBExecUpdate(TableName, insertDataList, dt.Rows[i][dt.Columns.Count - 1].ToString());
                    }
                    insertDataList.Clear();
                }
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = baseC.ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = baseC.ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, baseC.userinfo.EmployeesNo, tmp_action, ex.ToString(),ex);
            }
            finally
            {
                if (TableName == "HEALTH_EDUCATION_ITEM_DATA")
                {
                    this.link2.DBClose();
                }
                else
                {
                    this.link.DBClose();
                }
            }
            return eftrow;
        }

        /// <summary>
        /// 刪除
        /// </summary>
        public int del(string TableName, string where)
        {
            int eftrow = link.DBExecDelete(TableName, where);
            return eftrow;
        }

        
        public DataTable sel_Ais_Inquiry(string userno)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM AIS_INQUIRY_DATA WHERE 0 = 0 ";
            if (userno != "")
                sql += "AND CREANO = '" + userno + "' ";
            sql += " ORDER BY DATA_QUEUE";

            link.DBExecSQL(sql, ref dt);

            return dt;
        }

        public DataTable sel_type(string model, string group)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT SYS_PARAMS.*,(SELECT COUNT(*) FROM HEALTH_EDUCATION_ITEM_DATA WHERE CATEGORY_ID = P_ID)CATEGORY_NUM, ";
            sql += "(SELECT COUNT(*) FROM HEALTH_EDUCATION_ITEM_DATA WHERE SECOND_CATEGORY_ID = P_ID)SECOND_CATEGORY_NUM, ";
            sql += "(SELECT COUNT(*) FROM HEALTH_EDUCATION_ITEM_DATA WHERE EXPLANATION_ID = P_ID)EXPLANATION_NUM ";
            sql += "FROM SYS_PARAMS WHERE P_MODEL = '" + model + "' AND P_GROUP = '" + group + "' ";
            sql += "ORDER BY P_SORT ASC ";

            base.DBExecSQL(sql, ref dt);

            return dt;
        }

        public DataTable sel_health_education_item(string category, string column, string key, string show = "")
        {
            DataTable dt = new DataTable();

            string sql = "SELECT HEALTH_EDUCATION_ITEM_DATA.*, ";
            sql += "(SELECT COUNT(*) FROM HEALTH_EDUCATION_DATA WHERE HEALTH_EDUCATION_ITEM_DATA.ITEM_ID = HEALTH_EDUCATION_DATA.ITEMID)NUM, ";
            sql += " nvl((SELECT P_NAME FROM SYS_PARAMS WHERE HEALTH_EDUCATION_ITEM_DATA.EXPLANATION_ID = P_VALUE),'其他未分類') ";
            sql += " EXPLANATION_NAME FROM HEALTH_EDUCATION_ITEM_DATA  WHERE 0 = 0 AND OTHER = '0' ";
           // mod by 20131216 將未分類項目標題以"其他未分類"顯示

            if (category != "" && key.Split(',').GetValue(0).ToString() == "0")
            {
                sql += "AND ( CATEGORY_ID = '" + key.Split(',').GetValue(0).ToString() + "' OR CATEGORY_ID = '0' ) AND SECOND_CATEGORY_ID = '" + key.Split(',').GetValue(1).ToString() + "' ";
            }
            else if (category != "")
            {
                sql += "AND ( CATEGORY_ID = '" + key.Split(',').GetValue(0).ToString() + "'  ) AND SECOND_CATEGORY_ID = '" + key.Split(',').GetValue(1).ToString() + "' ";
            }
            if (show != "")
                sql += "AND SHOW = '" + show + "' ";
            if (column != "")
            {
                string sql_search_str = difficult_word_convert(key, "LIKE");
                sql += "AND " + column + " LIKE '%'" + sql_search_str + "'%' escape '_' ";
            }

            sql += " ORDER BY EXPLANATION_ID ,NAME, CREATE_DATE DESC, ITEM_ID ";
            
            base.DBExecSQL(sql, ref dt);

            return dt;
        }

        public string difficult_word_convert(string keyword, string type)
        {
            char[] values = keyword.ToCharArray();
            string key_tmep = "";
            List<string> temp = new List<string>();
            string sql_search_str = "";
            List<char> preserveWord = new List<char>() {'%','_' };
            foreach (char letter in values)
            {
                // Get the integral value of the character.

                int value = Convert.ToInt32(letter);
                // Convert the integer value to a hexadecimal value in string form.
                key_tmep = "0000" + $"{value:X}";
                if (preserveWord.Contains(letter)) {
                    temp.Add("'_'");
                }
                temp.Add("UNISTR('\\" +  (key_tmep).Substring(key_tmep.Length - 4) + "')");
            }
            if (temp.Count > 0)
            {
                if (type == "LIKE")
                {
                    sql_search_str = "||" + string.Join("||", temp) + "||";

                }
                else
                {
                    sql_search_str = string.Join("||", temp) ;
                }

            }

            return sql_search_str;
        }
        public DataTable sel_icndiseasestr_data(string feeno) //隔
        {
            DataTable dt = new DataTable();
            string sql = " SELECT TRANS_REMARK FROM DATA_TRANS_MEMO WHERE FEE_NO = '" + feeno + "' AND TRANS_FLAG='M' AND TRANS_REMARK LIKE '%隔離%'";
            base.DBExecSQL(sql, ref dt);
            return dt;
        }

        public DataTable sel_pressure_data(string feeno)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM NIS_PRESSURE_SORE_DATA WHERE FEENO = '" + feeno + "' ";
            sql += " AND TOTAL < 19 AND RECORDTIME = ";
            sql += "(SELECT MAX(RECORDTIME) FROM NIS_PRESSURE_SORE_DATA WHERE FEENO = '" + feeno + "' AND DELETED IS NULL) ";
            sql += "ORDER BY RECORDTIME ASC ";

            base.DBExecSQL(sql, ref dt);
            return dt;
        }

        public DataTable sel_pain_data(string feeno)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM PAINCONTINUED WHERE FEENO = '" + feeno + "' ";
            sql += "AND status <> 'del' AND status <> '結案' ORDER BY STARTDT ASC ";

            base.DBExecSQL(sql, ref dt);
            return dt;
        }
        public DataTable sel_suicide_data(string feeno)//2017/7/26新增 自殺傷害 連結DB的moodAssesment Table
        {
            DataTable dt = new DataTable();
            //string sql = "SELECT * FROM MOOD_ASSESSMENT_DATA WHERE FEE_NO = '" + feeno + "' ";
            //sql += " AND ASSESS_DT = (SELECT MAX(ASSESS_DT) AS MAXDATE FROM MOOD_ASSESSMENT_DATA where DEL_USER is null AND TOTAL_SCORE >= 10)";

            //string sql = "SELECT FEE_NO,TOTAL_SCORE,(Select MAX(ASSESS_DT)from MOOD_ASSESSMENT_DATA)as maxdate FROM MOOD_ASSESSMENT_DATA WHERE  DEL_USER is null AND TOTAL_SCORE >= 10";
            //sql += " AND ASSESS_DT in (select ASSESS_DT from mood_assessment_data where FEE_NO = '" + feeno + "' )";

            string sql = "SELECT total_score FROM MOOD_ASSESSMENT_DATA";
            sql += " where  ASSESS_DT in (SELECT MAX(ASSESS_DT) FROM MOOD_ASSESSMENT_DATA WHERE FEE_NO ='" + feeno + "' and DEL_USER is null )";
            sql += "AND DEL_USER is null AND TOTAL_SCORE >= 10 AND FEE_NO ='" + feeno + "'";

          
            base.DBExecSQL(sql, ref dt);
            return dt;
        }

        public DataTable sel_constraint_data(string feeno)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM BINDTABLE WHERE FEENO = '" + feeno + "' ";
            sql += "and enddt is null AND status <> 'del' ";

            base.DBExecSQL(sql, ref dt);
            return dt;
        }
        public DataTable sel_leave_referral_data(string feeno)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM NIS_LEAVE_REFERRAL_DATA WHERE FEENO = '" + feeno + "' ";

            base.DBExecSQL(sql, ref dt);
            return dt;
        }

        public DataTable sel_health_education(string feeno, string starttime, string endtime)
        {
            DataTable dt = null;

            string sql = "SELECT HEALTH_EDUCATION_DATA.*, ";
            sql += "(SELECT P_NAME FROM SYS_PARAMS WHERE P_VALUE = (SELECT CATEGORY_ID FROM HEALTH_EDUCATION_ITEM_DATA WHERE HEALTH_EDUCATION_ITEM_DATA.ITEM_ID = HEALTH_EDUCATION_DATA.ITEMID))ITEM_NAME, ";
            sql += "(SELECT NAME FROM HEALTH_EDUCATION_ITEM_DATA WHERE HEALTH_EDUCATION_ITEM_DATA.ITEM_ID = HEALTH_EDUCATION_DATA.ITEMID)NAME, ";
            sql += "NVL((select assessdt from (select id,assessdt from paincontinuedsave where status <> 'del' order by assessdt asc) where substr(id,-8,8) = substr(feeno,0,8) and rownum = 1), 0) AS FISRT_DATE, ";
            sql += "NVL((select strength from (select id,strength from paincontinuedsave where status <> 'del' order by assessdt asc) where substr(id,-8,8) = substr(feeno,0,8) and rownum = 1), 0) as FISRT_PAIN, ";
            sql += "NVL((select assessdt from (select id,assessdt from paincontinuedsave where status <> 'del' order by assessdt desc) where substr(id,-8,8) = substr(feeno,0,8) and rownum = 1), 0) AS LAST_DATE, ";
            sql += "NVL((select strength from (select id,strength from paincontinuedsave where status <> 'del' order by assessdt desc) where substr(id,-8,8) = substr(feeno,0,8) and rownum = 1), 0) as LAST_PAIN ";
            sql += "FROM HEALTH_EDUCATION_DATA WHERE 0 = 0 ";
            if (feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if (starttime != "")
                sql += "AND RECORDTIME BETWEEN to_date('" + starttime + "','yyyy/mm/dd hh24:mi:ss') AND to_date('" + endtime + "','yyyy/mm/dd hh24:mi:ss') ";

            sql += "AND DELETED IS NULL ORDER BY RECORDTIME DESC ";

            base.DBExecSQL(sql, ref dt);
            return dt;
        }

        public DataTable sel_health_education_item()
        {
            DataTable dt = null;
            base.DBExecSQL("SELECT ITEM_ID, NAME FROM HEALTH_EDUCATION_ITEM_DATA ", ref dt);
            return dt;
        }

        public Boolean sel_FRIDs(string feeno)
        {
            Boolean result = false;
            try
            {
                byte[] doByteCode = baseC.webService.GetUdOrder(feeno, "");
                if (doByteCode != null)
                {
                    string doJsonArr = CompressTool.DecompressString(doByteCode);
                    List<UdOrder> GetUdOrderList = JsonConvert.DeserializeObject<List<UdOrder>>(doJsonArr);
                    GetUdOrderList.ForEach((item) =>
                    {
                        if (item.IsFRIDs == true)
                        {
                            result = true;
                        }
                    });
                }
            }
            catch (Exception ex)
            {

            }
            return result;
        }
        public DataTable sql_shiftPrint(string costcode, string shift_date, ref DataTable dt)
        {
            string sql = "";
            sql += "SELECT A.BED_NO,白班護理,白班指導,小夜護理,小夜指導,大夜護理,大夜指導 FROM (";
            sql += "SELECT DISTINCT BED_NO FROM DATA_DISPATCHING WHERE COST_CODE = '" + costcode + "' AND ";
            sql += "SHIFT_DATE = TO_DATE('" + shift_date + "','YYYY/MM/DD')) A LEFT JOIN ";
            sql += "(SELECT BED_NO,RESPONSIBLE_NAME||RESPONSIBLE_USER 白班護理,GUIDE_NAME||GUIDE_USER ";
            sql += "白班指導 FROM DATA_DISPATCHING WHERE COST_CODE = '" + costcode + "' AND SHIFT_DATE = TO_DATE ";
            sql += "('" + shift_date + "','YYYY/MM/DD') AND SHIFT_CATE='D') B ON A.BED_NO = B.BED_NO LEFT JOIN ";
            sql += "(SELECT BED_NO,RESPONSIBLE_NAME||RESPONSIBLE_USER 小夜護理,GUIDE_NAME||GUIDE_USER ";
            sql += "小夜指導 FROM DATA_DISPATCHING WHERE COST_CODE = '" + costcode + "' AND SHIFT_DATE = TO_DATE ";
            sql += "('" + shift_date + "','YYYY/MM/DD') AND SHIFT_CATE='E') C ON A.BED_NO = C.BED_NO LEFT JOIN ";
            sql += "(SELECT BED_NO,RESPONSIBLE_NAME||RESPONSIBLE_USER 大夜護理,GUIDE_NAME||GUIDE_USER  ";
            sql += "大夜指導 FROM DATA_DISPATCHING WHERE COST_CODE = '" + costcode + "' AND SHIFT_DATE = TO_DATE ";
            sql += "('" + shift_date + "','YYYY/MM/DD') AND SHIFT_CATE='N') D ON A.BED_NO = D.BED_NO ";
            sql += "ORDER BY A.BED_NO";

            base.DBExecSQL(sql, ref dt);

            return dt;
        }
        public DataTable produce_exl(string sql)
        {
            DataTable dt = null;
            this.DBExecSQL(sql, ref dt);
            return dt;
        }

        /// <summary>
        /// 搜尋傷口_壓傷
        /// </summary>
        /// <param name="type">傷口種類</param>
        /// <param name="position">部位</param>
        /// <param name="reason">原因</param>
        public DataTable sel_wound_pressure(string type, string[] position, string reason, string start_date, string end_date)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT DISTINCT(LOCATION),(SELECT COUNT(*) FROM WOUND_DATA WHERE LOCATION = TEMP.LOCATION ";
            if (type != "")
                sql += "AND TYPE = '" + type + "' ";

            if (start_date != "")
                sql += "AND creattime >= to_date('" + start_date + "','YYYY/MM/DD') ";
            if (end_date != "")
                sql += "AND creattime <= to_date('" + end_date + "','YYYY/MM/DD') ";

            if (position != null)
                sql += "AND POSITION IN ('" + String.Join("','", position) + "')";
            if (reason != "")
                sql += "AND REASON LIKE '%" + reason + "%' ";
            sql += ") NUM FROM WOUND_DATA TEMP WHERE LOCATION IN (SELECT DISTINCT(LOCATION) FROM WOUND_DATA)";

            base.DBExecSQL(sql, ref dt);
            return dt;
        }

        /// <summary>
        /// 搜尋傷口_壓傷 傷口級數計算2015/3/24
        /// </summary>
        /// <param name="type">傷口種類</param>
        /// <param name="position">部位</param>
        /// <param name="classnum">級數</param>
        public DataTable sel_wound_pressure_class(string type, string[] position, string classnum, string start_date, string end_date)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT DISTINCT(LOCATION),(select COUNT(DISTINCT(t.feeno))from WOUND_DATA t, wound_record r where t.LOCATION = TEMP.LOCATION ";
            sql += "AND t.deleted is null ";
            sql += "AND t.wound_id = r.wound_id ";
            if (type != "")
                sql += "AND t.TYPE = '" + type + "' ";

            if (start_date != "")
                sql += "AND t.creattime >= to_date('" + start_date + "','YYYY/MM/DD') ";
            if (end_date != "")
                sql += "AND t.creattime <= to_date('" + end_date + "','YYYY/MM/DD') ";

            if (position != null)
                sql += "AND t.POSITION IN ('" + String.Join("','", position) + "')";
            //if (reason != "")
            //    sql += "AND REASON LIKE '%" + reason + "%' ";
            if (classnum != "")
                sql += "AND r.CLASS = '" + classnum + "' ";

            sql += ") NUM FROM WOUND_DATA TEMP WHERE LOCATION IN (SELECT DISTINCT(LOCATION) FROM WOUND_DATA)";

            base.DBExecSQL(sql, ref dt);
            return dt;
        }
        /// <summary>
        /// 搜尋傷口_壓傷 多數壓傷(兩筆(含)以上人數)2015/3/24
        /// </summary>
        /// <param name="type">傷口種類</param>
        /// <param name="position">部位</param>
        /// <param name="reason">原因</param>
        public DataTable sel_wound_pressure_n(string type, string[] position, string classnum, string start_date, string end_date)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT LOCATION, count(0) num from (select LOCATION from WOUND_DATA WHERE 1=1  ";
            sql += "AND deleted is null ";
            if (type != "")
                sql += "AND TYPE = '" + type + "' ";
            if (start_date != "")
                sql += "AND creattime >= to_date('" + start_date + "','YYYY/MM/DD') ";
            if (end_date != "")
                sql += "AND creattime <= to_date('" + end_date + "','YYYY/MM/DD') ";
            if (position != null)
                sql += "AND POSITION IN ('" + String.Join("','", position) + "')";
            if (classnum != "")
                sql += "";

            sql += " group by LOCATION, feeno having count(feeno) >= 2) num  group by LOCATION";

            base.DBExecSQL(sql, ref dt);
            return dt;
        }
    }
}
