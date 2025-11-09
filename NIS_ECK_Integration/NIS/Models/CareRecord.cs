using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NIS.Models;
using System.Data;
using NIS.WebService;
using NIS.UtilTool;
using Newtonsoft.Json;
using NIS.Data;
using NIS.Controllers;

namespace NIS.Models
{
    public class CareRecord : DBConnector
    {
        private BaseController baseC = new BaseController();

        public CareRecord()
        {
        }
        /// <summary>
        /// 新增
        /// </summary>
        /// <returns>傳回成功筆數</returns>
        public int insert(string TableName, DataTable dt)
        {
            int eftrow = 0;
            List<DBItem> insertDataList = new List<DBItem>();
            for(int i = 1; i < dt.Rows.Count; i++)
            {
                for(int j = 0; j < dt.Columns.Count; j++)
                {
                    if(dt.Rows[0][j].ToString() == "String")
                        insertDataList.Add(new DBItem(dt.Columns[j].ToString(), dt.Rows[i][j].ToString(), DBItem.DBDataType.String));
                    else if(dt.Rows[0][j].ToString() == "DataTime")
                        insertDataList.Add(new DBItem(dt.Columns[j].ToString(), dt.Rows[i][j].ToString(), DBItem.DBDataType.DataTime));
                    else if(dt.Rows[0][j].ToString() == "Number")
                        insertDataList.Add(new DBItem(dt.Columns[j].ToString(), dt.Rows[i][j].ToString(), DBItem.DBDataType.Number));
                }
                eftrow = eftrow + base.DBExecInsert(TableName, insertDataList);
                insertDataList.Clear();
            }
            return eftrow;
        }

        /// <summary>
        /// 更新
        /// </summary>
        public int upd(string TableName, DataTable dt)
        {
            int eftrow = 0;
            List<DBItem> insertDataList = new List<DBItem>();
            for(int i = 1; i < dt.Rows.Count; i++)
            {
                for(int j = 0; j < dt.Columns.Count - 1; j++)
                {
                    if(dt.Rows[0][j].ToString() == "String")
                        insertDataList.Add(new DBItem(dt.Columns[j].ToString(), dt.Rows[i][j].ToString(), DBItem.DBDataType.String));
                    else if(dt.Rows[0][j].ToString() == "DataTime")
                        insertDataList.Add(new DBItem(dt.Columns[j].ToString(), dt.Rows[i][j].ToString(), DBItem.DBDataType.DataTime));
                    else if(dt.Rows[0][j].ToString() == "Number")
                        insertDataList.Add(new DBItem(dt.Columns[j].ToString(), dt.Rows[i][j].ToString(), DBItem.DBDataType.Number));
                }
                eftrow = eftrow + base.DBExecUpdate(TableName, insertDataList, dt.Rows[i][dt.Columns.Count - 1].ToString());
                insertDataList.Clear();
            }
            return eftrow;
        }

        /// <summary>
        /// 刪除
        /// </summary>
        public int del(string TableName, string where)
        {
            int eftrow = base.DBExecDelete(TableName, where);
            return eftrow;
        }

        /// <summary>
        /// 取得此使用者的所有FEENO
        /// </summary>
        public DataTable sel_pt(string table_name, string fee_col_name, string userno, string user_col_name)
        {
            DataTable dt = new DataTable();

            string sql = "SELECT DISTINCT(" + fee_col_name + ") FROM " + table_name + " WHERE " + user_col_name + " = '" + userno + "'";
            base.DBExecSQL(sql, ref dt);

            return dt;
        }

        /// <summary>
        /// 搜尋護理紀錄(已除去護理計畫))
        /// </summary>
        /// <param name="userno">登入者</param>
        /// <param name="feeno">病例號(必填)</param>
        /// <param name="userno">使用者名稱(必填)</param>
        /// <param name="id">護理紀錄/計畫ID</param>
        /// <param name="starttime">搜尋日期</param>
        public DataTable sel_carerecord(string feeno, string userno, string id, string starttime, string endtime, string sign, string type ="",string fromVIEW =null)
        {
            DataTable dt = new DataTable();
            string sql = "";
            if (!string.IsNullOrWhiteSpace(fromVIEW))
            {
                 sql = "SELECT * FROM H_CARERECORD_DATA WHERE 0 = 0 ";
            }else
            {
                 sql = "SELECT * FROM CARERECORD_DATA WHERE 0 = 0 ";
            }
            if(feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if(userno != "")
                sql += "AND CREATNO = '" + userno + "' ";
            if(id != "")
                sql += "AND CARERECORD_ID || SELF = '" + id + "' ";
            if(starttime != "")
                sql += "AND RECORDTIME BETWEEN to_date('" + starttime + "','yyyy/mm/dd hh24:mi:ss') AND to_date('" + endtime + "','yyyy/mm/dd hh24:mi:ss') ";
            if(sign != "")
                sql += "AND SIGN <> 'Y' OR SIGN IS NULL ";
            //11/19 護理計畫 NEW
            if (type != "NPlan")
            {
                //sql += " AND SELF <> 'CAREPLANMASTER'";
            }
            sql += "AND DELETED is null ORDER BY RECORDTIME,SELF,TITLE,CARERECORD_ID ASC";

            base.DBExecSQL(sql, ref dt);

            return dt;
        }
        public DataTable get_cp_record(string feeno, string rb_mode = "", string s_datetime = "", string e_datetime = "")
        {
            DataTable dt = new DataTable();
            string sql = string.Concat("select TO_NUMBER(replace(REGEXP_SUBSTR(REPLACE (cd.title, '重整 ', ''),",
                "'(#\\d+)'),'#','')) as sort_order, REPLACE (cd.title, '重整 ', '') as ot,cd.* ",
                "from CARERECORD_DATA cd where self = 'CAREPLANMASTER' and feeno ='"+ feeno + "' AND DELETED is null ");
            //11/19 護理紀錄 護理計畫資料 NEW
            if (!string.IsNullOrEmpty(rb_mode))
            {
                sql += " AND CREATTIME BETWEEN TO_DATE( '" + s_datetime + "', 'yyyy/mm/dd hh24:mi:ss' ) ";
                sql += " AND TO_DATE('" + e_datetime + "', 'yyyy/mm/dd hh24:mi:ss' )";
            }
            sql += " order by sort_order,RECORDtime asc";  
            base.DBExecSQL(sql, ref dt);
            return dt;
        }

        /// <summary>
        /// 搜尋護理紀錄(護理計畫)
        /// </summary>
        /// <param name="userno">登入者</param>
        /// <param name="feeno">病例號(必填)</param>
        /// <param name="userno">使用者名稱(必填)</param>
        /// <param name="id">護理計畫ID</param>
        /// <param name="starttime">搜尋日期</param>
        public DataTable sel_carerecord2(string feeno, string userno, string id, string starttime, string endtime, string sign)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM CARERECORD_DATA WHERE 0 = 0 ";
            if(feeno != "")
                sql += "AND FEENO = '" + feeno + "' ";
            if(userno != "")
                sql += "AND CREATNO = '" + userno + "' ";
            if(id != "")
                sql += "AND CARERECORD_ID || SELF = '" + id + "' ";
            if(starttime != "")
                sql += "AND RECORDTIME BETWEEN to_date('" + starttime + "','yyyy/mm/dd hh24:mi:ss') AND to_date('" + endtime + "','yyyy/mm/dd hh24:mi:ss') ";
            if(sign != "")
                sql += "AND SIGN <> 'Y' OR SIGN IS NULL ";

            sql += "AND DELETED is null ORDER BY RECORDTIME,TITLE ASC";

            base.DBExecSQL(sql, ref dt);

            return dt;
        }

        public DataTable sel_carerecord(string id)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM CARERECORD_DATA WHERE ";
            sql += " carerecord_id||self ='" + id + "'";

            base.DBExecSQL(sql, ref dt);

            return dt;
        }

        /// <summary>
        /// 搜尋護理紀錄
        /// </summary>
        public DataTable sel_carerecord(string guid_no, string id, string starttime, string endtime, string sign)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM CARERECORD_DATA WHERE 0 = 0 ";
            if(guid_no != "")
                sql += "AND GUIDE_NO = '" + guid_no + "' ";
            if(id != "")
                sql += "AND CARERECORD_ID || SELF IN " + id + " ";
            if(starttime != "")
                sql += "AND RECORDTIME BETWEEN to_date('" + starttime + "','yyyy/mm/dd hh24:mi:ss') AND to_date('" + endtime + "','yyyy/mm/dd hh24:mi:ss') ";
            if(sign != "" && sign == "N")
                sql += "AND SIGN <> 'Y' ";
            if(sign != "" && sign == "Y")
                sql += "AND SIGN = '" + sign + "' ";

            sql += "AND DELETED is null ORDER BY FEENO,RECORDTIME DESC";

            base.DBExecSQL(sql, ref dt);

            return dt;
        }

        public DataTable sel_elect_med(string userno, string id, string starttime, string endtime, string sign)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT A.MED_DESC,A.UD_PATH,A.UD_UNIT,A.UD_CIR,B.*,";
            sql += "(CASE WHEN A.UD_CIR = 'STAT' THEN 'S' WHEN INSTR(A.UD_CIR,'PRN') > 0 THEN 'P' ELSE 'R' END) UD_TYPE ";
            sql += "FROM UD_ORDER A,DRUG_EXECUTE B ";
            sql += "WHERE A.FEE_NO = B.FEE_NO AND A.UD_SEQ = B.UD_SEQ ";
            sql += "AND B.USE_DOSE <> '0' ";
            if(userno != "")
                sql += "AND B.EXEC_ID = '" + userno + "' ";
            if(id != "")
                sql += "AND UD_SEQPK IN " + id + " ";
            if(starttime != "")
                sql += "AND DRUG_DATE BETWEEN to_date('" + starttime + "','yyyy/mm/dd hh24:mi:ss') AND to_date('" + endtime + "','yyyy/mm/dd hh24:mi:ss') ";
            if(sign != "" && sign == "N")
                sql += "AND (RECORD_ID <> 'Y' OR RECORD_ID IS NULL) ";
            if(sign != "" && sign == "Y")
                sql += "AND RECORD_ID = '" + sign + "' ";

            sql += "AND EXEC_DATE IS NOT NULL AND INVALID_DATE IS NULL ORDER BY B.FEE_NO,B.DRUG_DATE DESC";

            base.DBExecSQL(sql, ref dt);

            return dt;
        }

        /// <summary>
        /// VitalSign列表
        /// </summary>
        public DataTable sel_vital_sign(string feeno, string userno, string starttime, string endtime)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM data_vitalsign WHERE FEE_NO = '" + feeno + "' ";
            if(userno != "")
                sql += "AND CREATE_USER = '" + userno + "' ";
            if(starttime != "")
                sql += "AND CREATTIME BETWEEN to_date('" + starttime + "','yyyy/mm/dd hh24:mi:ss') AND to_date('" + endtime + "','yyyy/mm/dd hh24:mi:ss') ";

            sql += "AND VS_RECORD IS NOT NULL ORDER BY CREATE_DATE DESC ";

            base.DBExecSQL(sql, ref dt);

            return dt;
        }

        /// <summary>
        /// 搜尋節點
        /// </summary>
        public DataTable sel_phrase_node(string userno, string depth, string p_node, string node, string p_type, string srch_val = "")
        {
            DataTable dt = new DataTable();

            string sql = "SELECT * FROM PHRASE_NODE WHERE 0 = 0 ";
            if(userno != "")
                sql += "AND (CREANO = 'sys' OR CREANO = '" + userno + "') ";
            if(depth != "")
                sql += "AND DEPTH = " + depth + " ";
            if(p_node != "")
                sql += "AND PARENT_NODE = " + p_node + " ";
            if(node != "")
                sql += "AND NODEID = " + node + " ";
            if(p_type != "")
            {
                sql += "AND PHRASE_TYPE = '" + p_type + "' ";
                switch(p_type)
                {
                    case "unit":
                        sql += "AND COST_UNIT = '" + srch_val + "' ";
                        break;
                    case "self":
                        sql += "AND CREANO = '" + srch_val + "' ";
                        break;
                }
            }

            sql += "ORDER BY NAME ASC ";

            base.DBExecSQL(sql, ref dt);

            return dt;
        }

        /// <summary>
        /// 搜尋片語內容
        /// </summary>
        public DataTable sel_phrase_data(string node, string data_id, string type, string creano = "")
        {
            DataTable dt = new DataTable();

            string sql = "SELECT * FROM PHRASE_DATA WHERE 0=0 ";
            if(node != "")
                sql += "AND NODE_ID = " + node + " ";
            if(data_id != "")
                sql += "AND DATA_ID = '" + data_id + "' ";
            if(type == "user")
                sql += "AND CREANO = '" + creano + "' ";

            base.DBExecSQL(sql, ref dt);

            return dt;
        }


        public DataTable sel_med(string feeno, string starttime, string endtime)
        {
            DataTable dt = new DataTable();

            string sql = "SELECT UD_SEQ,ROUND(USE_DOSE,1) USE_DOSE,DRUG_DATE,REASONTYPE,REASON,INVALID_DATE,BADREACTION,TO_CHAR(EXEC_DATE,'yyyy/mm/dd hh24:mi:ss') EXEC_DATE,REASON";
            sql += " FROM DRUG_EXECUTE WHERE FEE_NO = '" + feeno + "'";
            sql += " AND EXEC_DATE BETWEEN TO_DATE('" + starttime + "','yyyy/mm/dd hh24:mi:ss') AND to_date('" + endtime + "','yyyy/mm/dd hh24:mi:ss')";
            sql += " AND INVALID_DATE IS NULL ORDER BY EXEC_DATE DESC ";

            base.DBExecSQL(sql, ref dt);

            return dt;

        }

        /// <summary>
        /// 搜尋指導者
        /// </summary>
        /// <param name="feeno">搜尋的feeno</param>
        /// <param name="date">搜尋的日期</param>
        /// <param name="shit_cate">搜尋的班別</param>
        public string sel_guide_userno(string feeno, DateTime date, int cate = 99)
        {
            DataTable dt = new DataTable();
            string name = feeno;
            string shit_cate = "";
            if(cate != 99)
            {
                if(cate < 8)
                    shit_cate = "N";
                else if(cate < 16)
                    shit_cate = "D";
                else if(cate < 24)
                    shit_cate = "E";
            }

            string sql = "SELECT GUIDE_USER FROM DATA_DISPATCHING WHERE 0 = 0 ";

            if(feeno != "")
                sql += "AND RESPONSIBLE_USER = '" + feeno + "' ";
            if(date != null)
                sql += "AND SHIFT_DATE = TO_DATE('" + date.ToString("yyyy/MM/dd") + "','yyyy/MM/dd') ";
            if(shit_cate != "")
                sql += "AND SHIFT_CATE = '" + shit_cate + "' ";

            sql += "AND GUIDE_USER IS NOT NULL";

            base.DBExecSQL(sql, ref dt);

            if(dt.Rows.Count > 0)
                name = dt.Rows[0]["GUIDE_USER"].ToString();

            return name;
        }

        public DataTable get_special_event(string fee_no, string id, List<string> where = null)
        {
            DataTable dt = new DataTable();

            bool mayaDemo = (NIS.MvcApplication.iniObj.NisSetting.ServerMode.ToString() == "Maya");
            string dt_name = string.Empty, history_dt_name = string.Empty;
            if (!mayaDemo)
            {
                dt_name = "CS.";
                history_dt_name = "CS1.";
            }

            string strsql = "SELECT * FROM ( ";
            strsql += " SELECT A.*, C_OTHER,TITLE FROM NIS_SPECIAL_EVENT_DATA A, " + dt_name + "CARERECORD_DATA B ";
            // strsql += "WHERE A.FEENO = B.FEENO AND A.EVENT_ID = B.CARERECORD_ID AND B.SELF = 'SPE_EVENT' AND TYPE_ID <> '5' ";
            //2014/11/03 特殊註記加上出院準備項目給人員自行刪除 mod by yungchen
            strsql += "WHERE A.FEENO = B.FEENO AND A.EVENT_ID = B.CARERECORD_ID  ";
            if(!string.IsNullOrEmpty(fee_no))
            {
                strsql += "AND A.FEENO in ('" + fee_no + "') ";
            }
            if(id != "")
            {
                strsql += "AND A.EVENT_ID = '" + id + "' ";
            }
            if (where != null)
            {
                strsql += " AND ";
                strsql += string.Join(" AND ", where);
            }
            //strsql += "ORDER BY A.CREATTIME ";

            //2019 新增歷中資料判斷
            strsql += " UNION ";  
            strsql += " SELECT A.*, C_OTHER,TITLE FROM NIS_SPECIAL_EVENT_DATA A, " + history_dt_name + "CARERECORD_DATA B ";
            strsql += " WHERE A.FEENO = B.FEENO AND A.EVENT_ID = B.CARERECORD_ID  ";
            if (!string.IsNullOrEmpty(fee_no))
            {
                strsql += "AND A.FEENO in ('" + fee_no + "') ";
            }
            if (id != "")
            {
                strsql += "AND A.EVENT_ID = '" + id + "' ";
            }
            if (where != null)
            {
                strsql += " AND ";
                strsql += string.Join(" AND ", where);
            }
            strsql += " ) EVT ORDER BY EVT.CREATTIME  ";

            base.DBExecSQL(strsql, ref dt);
            return dt;
        }

        /// <summary>
        /// 取得此使用者的所有護理計畫
        /// </summary>
        public DataTable GetCarePlan_Master(string feeno)
        {
            DataTable dt = new DataTable();
            //20140730 修改取得護理計畫方式
            //string sql = "select * from careplanmaster where planenddate is null ";
            //sql += "and FEENO ='" + feeno + "'";
            string sql = "select * from CAREPLANMASTER where FEENO in ('" + feeno + "') and recordid in (select recordid from cptargetdtl where targetenddate is null) ";
            base.DBExecSQL(sql, ref dt);
            return dt;
        }

        /// <summary>
        /// 取得此使用者的所有護理計畫
        /// </summary>
        public DataTable GetCarePlan_Object(string feeno)
        {
            DataTable dt = new DataTable();
            //20140730 修改取得護理計畫方式
            //string sql = "select * from cptargetdtl t where t.recordid in ( select recordid from careplanmaster where planenddate is null ";
            //sql += "and FEENO ='" + feeno + "')";
            string sql = " (select recordid,serial,alltargetcontent from CPTARGETDTL where recordid in (select recordid from careplanmaster where FEENO = '" + feeno + "' ) and TARGETENDDATE is null) ";
            sql += "union (select recordid,serial,itemvalue from cpcustomer where recordid in (select recordid from careplanmaster where FEENO = '" + feeno + "' ) ";
            sql += " and substr(itemname,1,6) = 'target' and enddate is null ) order by recordid ";
            base.DBExecSQL(sql, ref dt);
            return dt;
        }

        /// <summary>
        /// 取得此使用者的所有護理計畫20160509
        /// </summary>
        public DataTable sel_NursePlan(string feeno)
        {
            DataTable dt = new DataTable();
            string sql = "select '#'||pno||topicdesc title from CAREPLANMASTER  where feeno='" + feeno + "' and planenddate is null order by pno";
            base.DBExecSQL(sql, ref dt);
            return dt;
        }

        /// <summary>
        /// 護理記錄存檔,組xml文字
        /// </summary>
        /// <param name="pt_name">姓名</param>
        /// <param name="chart_no">病歷號</param>
        /// <param name="sex">性別 (M-男，F-女)</param>
        /// <param name="age">年齡</param>
        /// <param name="bed_no">床號</param>
        /// <param name="admit_date">入院日期(西元年，EX: 20130502)</param>
        /// <param name="admit_time">入院時間(EX:202551)</param>
        /// <param name="NKDA">過敏( WEB_SERVICE : GetAllergyList 取得)</param>
        /// <param name="record_date">護理日期(西元年，EX:20130502)</param>
        /// <param name="record_time">護理時間(EX:2030)</param>
        /// <param name="duty_nurse">護理人員(EX:陳琬娸)</param>
        /// <param name="treat_focus">護理類別(處置)</param>
        /// <param name="record_content">護理內容</param>
        /// <returns>xml文字</returns>
        public string care_Record_Get_xml(string pt_name, string chart_no, string sex, string age, string bed_no, string admit_date, string admit_time
            , string NKDA, string record_date, string record_time, string duty_nurse, string treat_focus, string record_content, string ekgBase64string = ""
            )
        {
            string xml = "<?xml version='1.0' encoding='utf-8'?>";
            //xml += "<?xml-stylesheet type='text/xsl' href='./NIS_A000040.XSL'?>";
            xml += "<Document>";
            xml += "<TING_PT_NAME>" + pt_name + "</TING_PT_NAME>";
            xml += "<TING_CHART_NO>" + chart_no + "</TING_CHART_NO>";
            xml += "<TING_SEX>" + sex + "</TING_SEX>";
            xml += "<TING_AGE>" + age + "</TING_AGE>";
            xml += "<TING_BED_NO>" + bed_no + "</TING_BED_NO>";
            xml += "<TING_ADMIT_DATE>" + admit_date + "</TING_ADMIT_DATE>";
            xml += "<TING_ADMIT_TIME>" + admit_time + "</TING_ADMIT_TIME>";
            xml += "<TING_ALLERGEN>" + NKDA + "</TING_ALLERGEN>";
            //xml += "<CHART_RECORD>";
            xml += "<TING_NURSING_DATE>" + record_date + "</TING_NURSING_DATE>";
            xml += "<TING_NURSING_TIME>" + record_time + "</TING_NURSING_TIME>";
            xml += "<TING_DUTY_NURSE>" + duty_nurse + "</TING_DUTY_NURSE>";
            xml += "<TING_TREAT_FOCUS>" + treat_focus + "</TING_TREAT_FOCUS>";
            xml += "<TING_EKG>" + ekgBase64string + "</TING_EKG>";
            xml += "<CONTENT>" + record_content + "</CONTENT>";
            //xml += "</CHART_RECORD>";
            xml += "</Document>";
            return xml;
        }

        /// <summary>
        /// 護理記錄存檔,組xml文字批次
        /// </summary>
        /// <param name="pt_name">姓名</param>
        /// <param name="chart_no">病歷號</param>
        /// <param name="sex">性別 (M-男，F-女)</param>
        /// <param name="age">年齡</param>
        /// <param name="bed_no">床號</param>
        /// <param name="admit_date">入院日期(西元年，EX: 20130502)</param>
        /// <param name="admit_time">入院時間(EX:202551)</param>
        /// <param name="NKDA">過敏( WEB_SERVICE : GetAllergyList 取得)</param>
        /// <param name="record_date">護理日期(西元年，EX:20130502)</param>
        /// <param name="record_time">護理時間(EX:2030)</param>
        /// <param name="duty_nurse">護理人員(EX:陳琬娸)</param>
        /// <param name="treat_focus">護理類別(處置)</param>
        /// <param name="record_content">護理內容</param>
        /// <returns>xml文字</returns>
        public string care_Record_Get_xml_new(List<SignListDtl> signlist, string NKDA, PatientInfo pi)
        {
            string xml = "<?xml version='1.0' encoding='utf-8'?>";
            string ekgBase64string = "";
            //xml += "<?xml-stylesheet type='text/xsl' href='./NIS_A000040.XSL'?>";
            xml += "<Document>";
            xml += "<TING_PT_NAME>" + pi.PatientName+ "</TING_PT_NAME>";
            xml += "<TING_CHART_NO>" + pi.ChartNo + "</TING_CHART_NO>";
            xml += "<TING_SEX>" + pi.PatientGender + "</TING_SEX>";
            xml += "<TING_AGE>" + pi.Age + "</TING_AGE>";
            xml += "<TING_BED_NO>" + pi.BedNo + "</TING_BED_NO>";
            xml += "<TING_ADMIT_DATE>" + pi.InDate.ToString("yyyyMMdd") + "</TING_ADMIT_DATE>";
            xml += "<TING_ADMIT_TIME>" + pi.InDate.ToString("HHmmss") + "</TING_ADMIT_TIME>";
            xml += "<TING_ALLERGEN>" + trans_special_code(NKDA) + "</TING_ALLERGEN>";
            for(int i = 0; i < signlist.Count; i++)
            {
                xml += "<CHART_RECORD>";
                string contentTrans = trans_special_code(signlist[i].CONTENT);
                xml += "<Seq>" + signlist[i].RECORD_KEY + "_"+ signlist[i].SELF +"</Seq>";
                xml += "<TING_NURSING_DATE>" + signlist[i].RECORD_DATE + "</TING_NURSING_DATE>";
                xml += "<TING_NURSING_TIME>" + signlist[i].RECORD_TIME + "</TING_NURSING_TIME>";
                xml += "<TING_DUTY_NURSE>" + signlist[i].CREATE_NAME + "</TING_DUTY_NURSE>";
                xml += "<TING_TREAT_FOCUS>" + signlist[i].FOCUS + "</TING_TREAT_FOCUS>";
                xml += "<TING_EKG>" + signlist[i].EKG + "</TING_EKG>";
                xml += "<CONTENT>" + contentTrans + "</CONTENT>";
                xml += "</CHART_RECORD>";
            }
            xml += "</Document>";
            return xml;
        }

        /// <summary>
        /// //判斷來產生列表的員工姓名
        /// </summary>
        /// <param name="dt"></param>
        /// <returns>回傳一個，增加userName欄位的dt，及將每個userName欄位填入判斷後的名字</returns>
        protected internal DataTable getRecorderName(DataTable dt)
        {
            dt.Columns.Add("username");
            if(dt.Rows.Count > 0)
            {
                for(int i = 0; i < dt.Rows.Count; i++)
                {
                    string userno = string.Empty;
                    if(!string.IsNullOrEmpty(dt.Rows[i]["UPDNO"].ToString()))
                    {
                        userno = dt.Rows[i]["UPDNO"].ToString();
                        byte[] listByteCode = baseC. webService.UserName(userno);
                        if(listByteCode != null)
                        {
                            string listJsonArray = CompressTool.DecompressString(listByteCode);
                            UserInfo user_name = JsonConvert.DeserializeObject<UserInfo>(listJsonArray);
                            dt.Rows[i]["username"] = user_name.EmployeesName;
                        }
                        else
                        {
                            dt.Rows[i]["username"] = "";
                        }
                    }
                    else
                    {
                        dt.Rows[i]["username"] = dt.Rows[i]["CREATNAME"];
                    }
                }
            }
            return dt;
        }

        private string trans_special_code(string content)
        {
            if (content != null)
            {
                content = content.Trim();
                content = content.Replace("&", "&amp;");
                content = content.Replace("<", "&lt;");
                content = content.Replace(">", "&gt;");
                content = content.Replace("'", "&apos;");
                content = content.Replace("\"", "&quot;");

                content = content.Replace("\u0001", "");
                content = content.Replace("\u000B", "");
                content = content.Replace("\r\n", "&#xD;&#xA;");
                content = content.Replace("\n", "&#xA;");
                return content;
            }
            else
                return "";
        }
        public DataTable get_PhraseList(string oper_id)
        {
            DataTable dt = new DataTable();

            string sql = "SELECT * from PHRASE_NODE where phrase_type='self' and creano='" + oper_id + "' order by 'depth',parent_node,nodeid";          

            base.DBExecSQL(sql, ref dt);

            return dt;

        }

        public DataTable get_EKG(string chartNO)
        {
            DataTable dt = new DataTable();
            string start = DateTime.Now.AddDays(-365).ToString("yyyy/MM/dd");
            string end = DateTime.Now.ToString("yyyy/MM/dd");
            
            string sql = "SELECT * FROM ZDMS_MID WHERE HISNUM = '"+ chartNO + "' AND imgdatetime BETWEEN TO_DATE('"+ start + "', 'YYYY/MM/DD')  AND TO_DATE('"+ end + "', 'YYYY/MM/DD')";

            base.DBExecSQL(sql, ref dt);

            return dt;

        }

    }
}
