using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;
using System.Data;

[assembly: OwinStartup(typeof(WebApplication.WEB_EBM))]

namespace WebApplication
{
    public class WEB_EBM
    {
        public void Configuration(IAppBuilder app)
        {
            // 如需如何設定應用程式的詳細資訊，請參閱  http://go.microsoft.com/fwlink/?LinkID=316888
        }

        public string ReportTextResult(string ExamNo)
        {
            string ReportText;
            string WK_ErrMsg;
            DataTable WK_REPORT = new DataTable();
            string[] VerifyStr= new string[4];

            VerifyStr[0] = DateTime.Now.ToString();
            VerifyStr[1] = "MAYA.NIS";
            VerifyStr[2] = "admin";
            VerifyStr[3] = GetPW1(VerifyStr[2]);
            VerifyStr[4] = GetPW2(VerifyStr[2], VerifyStr[0], "M@Y@NIS");
            
            ReportText = "";
            WK_ErrMsg = "";
            WK_REPORT = GetEBMWsService.GetEBMWsService.UniReportTextResult(VerifyStr, ExamNo,  WK_ErrMsg);

            for (int i = 0 ; i < WK_REPORT.Rows.Count;i ++)
            {
                ReportText = WK_REPORT.Rows[i]["REPORT"].ToString();
            }
            return ReportText;
        }

        private string GetPW1 (string LK_UserAccount)
        {
            String WK_Nonce_str = LK_UserAccount;
            byte[] WK_Nonce_Byte = System.Text.Encoding.Default.GetBytes(WK_Nonce_str);
            String WK_Nonce_Base64 = Convert.ToBase64String(WK_Nonce_Byte);
            return WK_Nonce_Base64;
        }

        private string GetPW2(string LK_UserAccount, string LK_DateTimeNow,string LK_UserPW)
        {
            System.Security.Cryptography.SHA512 WK_SHA512 = new System.Security.Cryptography.SHA512CryptoServiceProvider();
            byte[] Combin_Byte = System.Text.Encoding.Default.GetBytes(LK_UserAccount + "1131090019" + LK_DateTimeNow + "MAYA" + LK_UserPW);
            byte[] WK_SHA512_Cryptp = WK_SHA512.ComputeHash(Combin_Byte);
            String WK_PW_Base64_SHA512 = Convert.ToBase64String(WK_SHA512_Cryptp);
            return WK_PW_Base64_SHA512;
        }

    }
}
