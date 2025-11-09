using NIS.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NIS.Models
{
    public class Billing
    {
       
    }
    public class Basic_Data
    {
        public string SERIAL { get; set; }
        public string HO_ID { get; set; }
        public string ITEM_NAME { get; set; }
        public string ITEM_TYPE { get; set; }
        public string ITEM_IDENTITY { get; set; }
        public string ITEM_PRICE { get; set; }
        public string ITEM_PRICE_NH { get; set; }
        public string COUNT { get; set; }
        public string COSTCODE { get; set; }
        public string SET { get; set; }
        public string NH_CODE { get; set; }
        public string SET_TYPE { get; set; }


    }

    public class Bill_Detal
    {
        public string SERIAL { get; set; }
        public string HO_ID { get; set; }
        public string ITEM_NAME { get; set; }
        public string ITEM_TYPE { get; set; }
        public string ITEM_IDENTITY { get; set; }
        public string ITEM_PRICE { get; set; }
        public string ITEM_PRICE_NH { get; set; }
        public string COUNT { get; set; }
        public string COSTCODE { get; set; }
        public string SELF_PRICE { get; set; }
        public string NH_PRICE { get; set; }
        public string COVER { get; set; }
        public string SET { get; set; }
        public string NH_CODE { get; set; }
    }

    public class Bill_RECORD
    {
        public string HO_ID { get; set; }      
        public string COUNT { get; set; }   
        public string IDENTITY { get; set; }
    }

    public class Bill_Accounts_Master
    {
        /// <summary>住院序號</summary>
        public string CHART_NO { get; set; }
        /// <summary>床號</summary>
        public string BED_NO{ get; set; }
        /// <summary>姓名</summary>
        public string NAME { get; set; }
        /// <summary>性別</summary>
        public string Gender { get; set; }
        /// <summary>紀錄時間</summary>
        public string Record_Date { get; set; }

        /// <summary>明細</summary>
        public List<Bill_Summary_Detail> Deatils  { get; set; }

    }


    public class Bill_Accounts_Deatil
    {

    }


}