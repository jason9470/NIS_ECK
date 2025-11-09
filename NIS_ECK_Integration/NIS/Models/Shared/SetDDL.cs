using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;

namespace NIS.Models
{
    public class SetDDL : DBConnector
    {
        public List<SelectListItem> GetddlItem(string Str, DataTable DT = null, bool IsOther = false, int TxtIndex = 0, int ValIndex = 0)
        {
            if (DT == null)
            {
                DT = new DataTable();
                base.DBExecSQL(Str, ref DT);
            }
            List<SelectListItem> items = new List<SelectListItem>();
            if (DT != null && DT.Rows.Count > 0)
            {
                foreach (DataRow DR in DT.Rows)
                {
                    items.Add(new SelectListItem() { Text = DR[TxtIndex].ToString(), Value = DR[ValIndex].ToString(), Selected = false });
                }
                if (IsOther) items.Add(new SelectListItem() { Text = "其他", Value = "其他", Selected = false });
            }
            return items;
        }
    }

}