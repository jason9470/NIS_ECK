using System;
using System.Data;

namespace NIS.Models
{
    public class AssessmentPain : DBAction
    {
        /// <summary>
        /// 取得 新增table欄位
        /// </summary>
        /// <returns>回傳table</returns>
        public DataTable get_table(string item, string feeno, string page)
        {
            DataTable dt = new DataTable();
            string DataSheet = item;
            string sql = "";
            switch (item)
            {
                case "PAIN_TABLE":
                    DataSheet = "PAIN_TABLE";
                    sql = "select * from " + DataSheet + " WHERE 1=1 and feeno ='" + feeno + "' ";
                    sql += " and STATUS <> 'del' ";
                    break;
                case "PAIN_INITIAL":
                    DataSheet = "PAIN_INITIAL";
                    sql = "select * from " + DataSheet + " WHERE 1=1 and id ='" + page + "' ";
                    break;
                case "ASSESSMENTBODY":
                    DataSheet = "ASSESSMENTBODY";
                    sql = "select * from " + DataSheet + " WHERE 1=1 and feeno ='" + feeno + "' order by insdt desc";
                    break;
                case "ASSESSMENTBODYup":
                    DataSheet = "ASSESSMENTBODY";
                    sql = "select * from " + DataSheet + " WHERE 1=1 and feeno ='" + feeno + "' ";
                    if (page != null)
                    { sql += "and id ='"+ page.ToString().Trim() +"' "; }
                     sql += "order by insdt desc";
                    break;
                case "ASSESSMENTBODYINQUIRY":
                    DataSheet = "ASSESSMENTBODY";
                 
                    //sql = "select * from ASSESSMENTBODY where id in ";
                    //sql += "(SELECT  max(id) FROM ASSESSMENTBODY where feeno ='" + feeno + "' and (STATUS='D' or STATUS='E'or STATUS='N')  GROUP BY substr(id,0,8))";
                    //sql += " and feeno ='" + feeno + "'";
                    //sql += " order by id ";        

                    sql = "select * from ASSESSMENTBODY where feeno ='" + feeno + "' and  status <> 'del' and  (insdt in ";

                    sql += "(SELECT  max(insdt) FROM ASSESSMENTBODY where feeno ='" + feeno + "' and STATUS='ICU' and to_char(to_date(insdt,'yyyy/MM/dd HH24:mi:ss'),'HH24mi') between '0801' and '1600'  GROUP BY substr(insdt,0,10))";
                    sql += "OR insdt in ";
                    sql += "(SELECT  max(insdt) FROM ASSESSMENTBODY where feeno ='" + feeno + "' and STATUS='ICU' and to_char(to_date(insdt,'yyyy/MM/dd HH24:mi:ss'),'HH24mi') between '1601' and '2359' GROUP BY substr(insdt,0,10))";
                    sql += "OR insdt in ";
                    sql += "(SELECT  max(insdt) FROM ASSESSMENTBODY where feeno ='" + feeno + "' and STATUS='ICU' and to_char(to_date(insdt,'yyyy/MM/dd HH24:mi:ss'),'HH24mi') between '0001' and '0700'  GROUP BY substr(insdt,0,10))";

                    sql += "OR insdt in ";

                    sql += "(SELECT  max(insdt) FROM ASSESSMENTBODY where feeno ='" + feeno + "' and STATUS='D' GROUP BY substr(insdt,0,10))";
                    sql += "OR insdt in ";
                    sql += "(SELECT  max(insdt) FROM ASSESSMENTBODY where feeno ='" + feeno + "' and STATUS='E' GROUP BY substr(insdt,0,10))";
                    sql += "OR insdt in ";
                    sql += "(SELECT  max(insdt) FROM ASSESSMENTBODY where feeno ='" + feeno + "' and STATUS='N' GROUP BY substr(insdt,0,10))";
                   // sql += " and feeno ='" + feeno + "' ";
                    sql += " ) AND  to_date(INSDT,'yyyy/MM/dd hh24:mi:ss')  BETWEEN to_date('" + DateTime.Now.AddDays(-7).ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/MM/dd hh24:mi:ss') AND to_date('" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/mm/dd hh24:mi:ss') ";
                    sql += " order by insdt "; 
                   // sql += ") order by insdt "; 
                    break;
                case "check":
                    DataSheet = "PAINCONTINUED";
                    sql = "select * from " + DataSheet + " WHERE 1=1 and feeno ='" + feeno + "' and STATUS <> 'del'" ;
                    break;
                case "DATA_VITALSIGN":
                    sql = "select VS_RECORD from DATA_VITALSIGN WHERE vs_item='gc' and fee_no ='" + feeno + "'  order by create_date desc";
                    break;
                    

            }           
           
            //select * from CARETABLE t inner join careitem_factor f on t.factor=f.f_id
            return base.gettable(sql);
        }

        /// <summary>
        /// 取得 新增table欄位
        /// </summary>
        /// <returns>回傳table</returns>
        public DataTable get_table2(string page, string feeno, string starttime, string endtime)
        {
            DataTable dt = new DataTable();
            string DataSheet = page;
            string sql = "";
            switch (page)
            {           
                case "ASSESSMENTBODYINQUIRY":
                    DataSheet = "ASSESSMENTBODY";
                    //sql = "select * from ASSESSMENTBODY where id in ";
                    //sql += "(SELECT  max(id) FROM ASSESSMENTBODY where feeno ='" + feeno + "' and (STATUS='D' or STATUS='E'or STATUS='N')  GROUP BY substr(id,0,8))";
                    //sql += " and feeno ='" + feeno + "' ";
                    //if (starttime != "")
                    //sql += "AND  to_date(INSDT,'yyyy/mm/dd hh24:mi:ss')  BETWEEN to_date('" + starttime + "','yyyy/mm/dd hh24:mi:ss') AND to_date('" + endtime + "','yyyy/mm/dd hh24:mi:ss') ";
                    //sql += " order by id "; 

                    sql = "select * from ASSESSMENTBODY where feeno ='" + feeno + "' and  status <> 'del'  and (insdt in ";

                    sql += "(SELECT  max(insdt) FROM ASSESSMENTBODY where feeno ='" + feeno + "' and STATUS='ICU' and to_char(to_date(insdt,'yyyy/MM/dd HH24:mi:ss'),'HH24mi') between '0801' and '1600'  GROUP BY substr(insdt,0,10))";
                    sql += "OR insdt in ";
                    sql += "(SELECT  max(insdt) FROM ASSESSMENTBODY where feeno ='" + feeno + "' and STATUS='ICU' and to_char(to_date(insdt,'yyyy/MM/dd HH24:mi:ss'),'HH24mi') between '1601' and '2359' GROUP BY substr(insdt,0,10))";
                    sql += "OR insdt in ";
                    sql += "(SELECT  max(insdt) FROM ASSESSMENTBODY where feeno ='" + feeno + "' and STATUS='ICU' and to_char(to_date(insdt,'yyyy/MM/dd HH24:mi:ss'),'HH24mi') between '0001' and '0800'  GROUP BY substr(insdt,0,10))";

                    sql += "OR insdt in ";

                    sql += "(SELECT  max(insdt) FROM ASSESSMENTBODY where feeno ='" + feeno + "' and STATUS='D' GROUP BY substr(insdt,0,10))";
                    sql += "OR insdt in ";
                    sql += "(SELECT  max(insdt) FROM ASSESSMENTBODY where feeno ='" + feeno + "' and STATUS='E' GROUP BY substr(insdt,0,10))";
                    sql += "OR insdt in ";
                    sql += "(SELECT  max(insdt) FROM ASSESSMENTBODY where feeno ='" + feeno + "' and STATUS='N' GROUP BY substr(insdt,0,10))";
                    //sql += " and feeno ='" + feeno + "' ";
                    sql += " ) AND  to_date(INSDT,'yyyy/MM/dd hh24:mi:ss')  BETWEEN to_date('" + starttime + "','yyyy/MM/dd hh24:mi:ss') AND to_date('" + endtime + "','yyyy/mm/dd hh24:mi:ss') ";
                    sql += " order by insdt "; 
                    break;              
            }

            //select * from CARETABLE t inner join careitem_factor f on t.factor=f.f_id
            return base.gettable(sql);
        }
    }
}
