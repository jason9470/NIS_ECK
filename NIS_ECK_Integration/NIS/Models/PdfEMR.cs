using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NIS.Models
{
    public class PdfEMR
    {
        /// <summary>
        //產生簽章所需JSON檔
        //add by shih,20181024
        /// </summary>
        public class EMR_pdf_json
        {
            public string url { get; set; }
            public string keyNo { get; set; }
            public string marginTop { get; set; }
            public string titleUrl { get; set; }
            public string titleStr { get; set; }
            public string footLeft { get; set; }
        }
    }
}