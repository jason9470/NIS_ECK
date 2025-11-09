using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NIS.Models
{
    public class AIS
    {
        public class GetAdmissionAssessment
        {
            //住院經驗(選項)
            public string HasIpd { get; set; }

            //住院經驗
            public List<Dictionary<string, string>> IpdPast { get;set;}

            //手術經驗(選項)
            public string HasSurgery { get; set; }

            //手術經驗列表
            public List<Dictionary<string, string>> IpdSurgery { get;set;}

        }
    }
}