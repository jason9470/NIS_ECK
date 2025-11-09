using Com.Mayaminer;
using Newtonsoft.Json;
using NIS.Data;
using NIS.Models;
using NIS.UtilTool;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using static NIS.Models.PdfEMR;

namespace NIS.Controllers
{
    public class PrintController : BaseController
    {
        private string DirectUrl;
        private LogTool log;
        private DBConnector link;

        //private PdfEMRController PdfEmr = new PdfEMRController();
        public PrintController()
        {
            this.DirectUrl = System.AppDomain.CurrentDomain.BaseDirectory;
            this.log = new LogTool();
            this.link = new DBConnector();

        }
        

        [HttpGet]
        public ActionResult GetPDF(bool EMR = false, string filename ="", string UrlParameter = "", string obj_json = "")
        {//Edit by wawa 修改版本 wawa20160722
            //** !!重要!! ** 注意：使用 GetPDF ，請將 url 參數擺在最後，因為 url 參數本身帶的是網址，裡面也有帶參數，放在前面會抓取不到 url 網址裡帶的參數值，其它 GetPDF 需帶的參數請往前放  by wawa 2016/7/22
            //** 切記：GetPDF 參數名，請勿與 url 帶的參數名相等，會有抓取錯誤的可能
            //** url 參數第一個加上 1=1 ，則之後參數位置可隨意調換；若無 1=1 ，切勿將列印所需之相關參數擺放於第一位
            //** 中文字在網址傳遞請加上 Server.UrlDecode()
            //System.Web.HttpUtility  取代 Server寫法
            string Parameters = string.Empty;
            string Title = "", Margin = string.Empty, Footer = string.Empty
                , title_url = string.Empty, title_str = string.Empty, margin_top = string.Empty
                , foot_right = string.Empty, foot_left = string.Empty;
            string AdditionalValue = "";  //wkhtmltopdf 需附加的變數值，如頁首、頁尾…等。
            EMR_pdf_json AtrributeObj = JsonConvert.DeserializeObject<EMR_pdf_json>(obj_json);
            if (string.IsNullOrEmpty(UrlParameter))
            {
                Parameters = Request.Url.Query;
            }
            else
            {
                Parameters = UrlParameter;
            }
            string ParametersUrl = Parameters.Substring(1, Parameters.Length - 1);  //去掉第一個 ? 符號

            if (!string.IsNullOrEmpty(obj_json))
            {
                title_url = AtrributeObj.titleUrl;
                margin_top = AtrributeObj.marginTop;
                foot_left = AtrributeObj.footLeft;
                title_str = AtrributeObj.titleStr;



                Margin = " --margin-left \"20mm\" --margin-right \"15mm\" ";
                Margin += "--margin-top \"" + margin_top + "mm\" " + "--margin-bottom \"21mm\" ";
                if (!string.IsNullOrWhiteSpace(title_url))
                {
                    Title += "--header-html \"" + title_url + "\" ";
                }
                else
                {
                    if (!string.IsNullOrEmpty(title_str))
                    {
                        Title += "--header-font-size \"20\" ";
                        Title += "--header-font-name \"標楷體\" ";
                        Title += "--header-center " + title_str + " ";
                    }
                }


                Footer = "--footer-center " + "\"頁 [page]/[topage]\"";
                if (!string.IsNullOrEmpty(foot_right))
                    Footer += " --footer-right \"" + foot_right + "\"";
                if (!string.IsNullOrEmpty(foot_left))
                {
                    Footer += " --footer-left \"" + foot_left + "\" ";
                    Footer += "--footer-font-size \"10\" ";
                    Footer += "--footer-font-name \"TIMES NEW ROMAN\" ";
                    Footer += "--footer-font-name \"標楷體\" ";
                }
                AdditionalValue = Margin + " --header-spacing \"3\" --footer-spacing \"2\" " + Title + Footer;
            }

            //抓取網址後所有帶的參數值，包含 GetODF 及 url 裡的檔案名  by wawa 2016/7/22
            string[] ParametersSplitArray = ParametersUrl.Split('&');  //將所有參數以 & 拆開
            string HeaderValTmp = "", FooterValTmp = "", UrlTmp = "", FileNameTmp = "";
            bool DelFile = true;
            foreach (string ArrayVal in ParametersSplitArray)
            {//將列印所需之各參數值抓取出來
                if (ArrayVal != "")
                {
                    string[] ParamAndVal = ArrayVal.Split('=');
                    if (ParamAndVal.Length > 1)  //確認參數有值，若無值則跳過
                    {
                        switch (ParamAndVal[0].ToString())
                        {
                            case "HeaderVal":  //頁首
                                HeaderValTmp = System.Web.HttpUtility.UrlDecode(ParamAndVal[1].ToString());
                                break;
                            case "FooterVal":  //頁尾
                                FooterValTmp = System.Web.HttpUtility.UrlDecode(ParamAndVal[1].ToString());
                                break;
                            case "FooterVal_local":  //頁尾
                                FooterValTmp = System.Web.HttpUtility.UrlDecode(ParamAndVal[1].ToString());
                                break;
                            case "url":  //欲轉 PDF 之網址。偵測到符合 url 時，重新抓取 ParametersUrl 裡 url= 後所有值
                                string[] UrlArrTmp = ParametersUrl.Split(new string[] { "url=" }, StringSplitOptions.RemoveEmptyEntries);
                                UrlTmp = System.Web.HttpUtility.UrlDecode(UrlArrTmp[UrlArrTmp.Length - 1].ToString());  //最後一個 Array 值才是 url 需抓取的值，所以 url 需放在最後
                                break;
                            case "filename":  //檔案名稱
                                if (ParamAndVal[1].ToString() != "")
                                    FileNameTmp = System.Web.HttpUtility.UrlDecode(ParamAndVal[1].ToString()) + ".pdf";
                                break;
                            case "DelFile":
                                if(ParamAndVal[1].ToString() != "" && ParamAndVal[1].ToString() == "false")
                                DelFile = false;
                                break;
                        }
                    }
                }
            }

            if (HeaderValTmp != "")
                AdditionalValue += "--header-center " + HeaderValTmp + " ";
            if (FooterValTmp != "")
            {
                if (string.IsNullOrWhiteSpace(Request["FooterVal_local"]))
                    {
                        AdditionalValue += "--footer-center " + FooterValTmp + " ";
                    }
                    else
                    {
                        AdditionalValue += "--footer-" + Request["FooterVal_local"] + " " + FooterValTmp + " ";
                    }
            }
                
            if (FileNameTmp != "")
            {
                //確保有檔案名稱可做執行和列印，避免出錯
                string strPath = @"C:\\wkhtmltopdf\\wkhtmltopdf.exe";
                //string tempPath = this.DirectUrl + @"Images\" + FileNameTmp;
                string tempPath = this.DirectUrl;
                if (EMR)
                {
                    tempPath = "C:\\EMR\\";
                }
                else
                {
                    //檢查資料夾是否存在
                    if (Directory.Exists(tempPath + @"pdfFiles"))
                    {
                        //資料夾存在
                    }
                    else
                    {
                        //新增資料夾
                        Directory.CreateDirectory(tempPath + @"pdfFiles");
                    }

                    tempPath += (DelFile) ? (@"Images\") : (@"pdfFiles\");//為是的狀況，就是產生在Images資料夾內；為否的情況，在pdfFiles資料夾產生
                }
                tempPath += FileNameTmp;

                Process p = new Process();
                p.StartInfo.FileName = strPath;
                p.StartInfo.Arguments = AdditionalValue + "\"" +UrlTmp + "\" \"" + tempPath + "\" ";
                p.StartInfo.UseShellExecute = true;

                p.Start();
                p.WaitForExit();
                if (EMR)
                {

                }
                else
                {
                    if (DelFile)
                    {//開啟詢問是否檔案下載的小提醒
                        Response.Write("<script>window.open('Download_Pdf?filename=" + FileNameTmp + "');</script>");
                    }
                    else
                    {//產生檔案在pdfFiles下後//關閉分頁視窗
                        Response.Write("<script>window.opener = null;window.close();</script>");
                    }
                }
                
            }
            else
                Response.Write("<script>alert('列印失敗，請連絡資訊室人員。');</script>");

            return new EmptyResult();
        }
        public ActionResult Download_EMRPdf(string filename, bool DelFile = true)
        {
            string tempPath = "C:\\EMR\\" + filename + ".pdf";
            FileInfo fileInfo = new FileInfo(tempPath);
            System.Web.HttpContext.Current.Response.Clear();
            System.Web.HttpContext.Current.Response.ClearContent();
            System.Web.HttpContext.Current.Response.ClearHeaders();
            System.Web.HttpContext.Current.Response.AddHeader("Content-Disposition", "attachment;filename=" + filename + ".pdf");
            System.Web.HttpContext.Current.Response.AddHeader("Content-Length", fileInfo.Length.ToString());
            System.Web.HttpContext.Current.Response.AddHeader("Content-Transfer-Encoding", "binary");
            System.Web.HttpContext.Current.Response.ContentType = "application/vnd.ms-excel";

            System.Web.HttpContext.Current.Response.ContentEncoding = Encoding.UTF8;
            System.Web.HttpContext.Current.Response.WriteFile(fileInfo.FullName);
            System.Web.HttpContext.Current.Response.Flush();
            System.Web.HttpContext.Current.Response.End();
            if (DelFile)//為是的狀況，就會刪除(是初始預設的狀況，都會刪;除了交班單產生PDF的需求，需產生PDF不刪)
                fileInfo.Delete();

            return new EmptyResult();
        }
        public ActionResult Download_Pdf(string filename,bool DelFile=true,string UrlPath = "",bool EMR = false)
        {
            string tempPath = string.Empty;
            tempPath = this.DirectUrl;
            tempPath += (DelFile) ? (@"Images\") : (@"pdfFiles\");//為是的狀況，就是產生在Images資料夾內；為否的情況，在pdfFiles資料夾產生(交班單功能使用)
            tempPath += filename;
            FileInfo fileInfo = new FileInfo(tempPath);
            Response.Clear();
            Response.ClearContent();
            Response.ClearHeaders();
            Response.AddHeader("Content-Disposition", "attachment;filename=" + filename);
            Response.AddHeader("Content-Length", fileInfo.Length.ToString());
            Response.AddHeader("Content-Transfer-Encoding", "binary");
            Response.ContentType = "application/vnd.ms-excel";

            Response.ContentEncoding = Encoding.UTF8;
            Response.WriteFile(fileInfo.FullName);
            Response.Flush();
            Response.End();
            if(DelFile)//為是的狀況，就會刪除(是初始預設的狀況，都會刪;除了交班單產生PDF的需求，需產生PDF不刪)
            fileInfo.Delete();

            return new EmptyResult();
        }
        public void GetPDF_Print(string features, string url, string filename = "", string pk_id = "")
        {
            EMR_pdf_json obj = new EMR_pdf_json();
            string Parameters = string.Empty, obj_toJson = string.Empty;
            string fee_no = ptinfo.FeeNo;
            obj.marginTop = "35";

            switch (features)
            {
                case "Insert_NBENTR":
                    obj.titleStr = "新生兒入院護理評估單";
                    obj.footLeft = "(2019_Q2)恩主公醫院BAIA080-004-03";
                    obj.titleUrl = GetSourceUrl() + "/BabyBorn/ENTR_HEADER?feeno=" + fee_no + "&Type=" + features;
                    obj_toJson = JsonConvert.SerializeObject(obj);
                    Parameters = Request.Url.Query;
                    Parameters = string.Format("?url={0}&filename={1}&feeno={2}&tableid={3}"
                        , url
                        , filename
                        , fee_no
                        , pk_id
                        );

                    GetPDF(false, filename, Parameters, obj_toJson);

                    break;
                case "Insert_BabyEntr":
                    obj.titleStr = "嬰幼兒入院護理評估單";
                    obj.footLeft = "(2019_Q2)恩主公醫院BAIA051-001-03";
                    obj.titleUrl = GetSourceUrl() + "/BabyBorn/ENTR_HEADER?feeno=" + fee_no + "&Type=" + features;
                    obj_toJson = JsonConvert.SerializeObject(obj);
                    Parameters = Request.Url.Query;
                    Parameters = string.Format("?url={0}&filename={1}&feeno={2}&tableid={3}"
                        , url
                        , filename
                        , fee_no
                        , pk_id
                        );

                    GetPDF(false, filename, Parameters, obj_toJson);
                    break;
                default:
                    break;
            }
        }

    }
}
