using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NIS.Models
{
    public class DailyBodyData
    {
        //建構LIST上的架構
        public string DBAM_ID { set; get; }
        public string DBAM_DTM { set; get; }
        public string[] DBAD_ITEM { set; get; }
        public string CREANO { set; get; }
        public string username { set; get; }
        public string temp_status { set; get; }
        public DailyBodyData(string id, string DTM, string[] item, string CREANO, string username, string Temp_Status)
        {
            this.DBAM_ID = id;
            this.DBAM_DTM = DTM;
            this.DBAD_ITEM = item;
            this.CREANO = CREANO;
            this.username = username;
            this.temp_status = Temp_Status;
        }
    }
}