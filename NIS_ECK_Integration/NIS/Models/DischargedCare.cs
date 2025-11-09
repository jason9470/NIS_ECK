using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NIS.Models;
using System.Data;

namespace NIS.Models
{
    //出院準備服務
    public class DischargedCare : DBConnector
    {      
        public DataTable GetDischargedCareQueryDt(string pFeeNo,string pStart = "",string pEnd = "")
        {
            string DateCondition = pStart != "" && pEnd != "" ? string.Format(" AND ASSESS_DATE >='{0}' AND ASSESS_DATE <= '{1}'", pStart, pEnd) : "";
            string CaseCondition = this.GetCaseState(pFeeNo) != "" ? "'1' AS EDIT_FLAG" : "'0' AS EDIT_FLAG";
            DataTable Dt = new DataTable();
            string Str = @"SELECT CREATE_USER,FEE_NO,DC_ID,ASSESS_DATE,ASSESS_TIME,TOTAL,CREATE_USER_NAME EXER,
                          (CASE FILTER_STATE WHEN '0' THEN '新病人' WHEN '1' THEN '手術' WHEN '2' THEN 'ICU轉入' WHEN '3' THEN '病情變化' WHEN '99' THEN FILTER_OTHER END) FILTER_STATE,{2}
                          FROM DISCHARGED_CARE WHERE FEE_NO = '{0}' {1} ORDER BY ASSESS_DATE,ASSESS_TIME";
            base.DBExecSQL(string.Format(Str, pFeeNo, DateCondition, CaseCondition), ref Dt);
            return Dt;
        }

        public DataTable GetDischargedCareEditDt(string pDcId)
        {
            DataTable Dt = new DataTable();
            base.DBExecSQL(string.Format("SELECT * FROM DISCHARGED_CARE WHERE DC_ID = '{0}'", pDcId), ref Dt);
            if (Dt.Rows.Count == 0)
            {
                DataRow Dr = Dt.NewRow();
                Dr["FEE_NO"] = "";
                Dr["DC_ID"] = "";
                Dt.Rows.Add(Dr);
            }
            return Dt;
        }

        public string GetCaseState(string pFeeNo)
        {
            //撈院內收案狀態WebService
            //2016/06/21 收案狀態暫時先抓case_state，Y 是收案，N 是不收案，沒資料時秀"收案狀態:"
            DataTable Dt = base.DBExecSQL(string.Format("SELECT fee_no, case_state, MAX(assess_date || ' ' || assess_time) assess_date FROM discharged_care assess_date WHERE fee_no='{0}' GROUP BY fee_no, case_state", pFeeNo));
            if (Dt != null && Dt.Rows.Count > 0)
            {
                if (Dt.Rows[0]["case_state"].ToString().Trim() == "Y") return "收案";
                if (Dt.Rows[0]["case_state"].ToString().Trim() == "N") return "不收案";
            }
            return "";
        }
    }
}
