using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using NIS.Controllers;
using System.Net;
using System.Net.Sockets;

namespace NIS
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801
    public class MvcApplication : System.Web.HttpApplication
    {
        /// <summary>json ini 物件</summary>
        public static IniObject iniObj;
        /// <summary>Local IP</summary>
        public static string localIp = GetLocalIPAddress();
        BaseController base_controller = null;
        bool status = true;
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            //設定Json ini
            setServerMode(ServerMode.Auto); //設定ini模式(Auto:自動判斷、Maya:內部設定檔、UAT:恩主公測試機、Production:恩主公正式機)
            base_controller = new BaseController();

            System.Timers.Timer myTimer = new System.Timers.Timer(1000);//60秒
            myTimer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimedEvent);
            myTimer.Interval = 1000;
            myTimer.Enabled = true;           
        }

        /// <summary>
        /// 設定 Json ini
        /// </summary>
        /// <param name="server_mode">伺服器模式(Auto:自動判斷、Maya:內部設定檔、UAT:恩主公測試機、Production:恩主公正式機)</param>
        /// <returns>ServerMode</returns>
        public static ServerMode setServerMode(ServerMode server_mode)
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            using (System.IO.StreamReader r = new System.IO.StreamReader(AppDomain.CurrentDomain.BaseDirectory + "App_Config\\NISConfig.json"))
            {
                string json = r.ReadToEnd();
                iniObj = Newtonsoft.Json.JsonConvert.DeserializeObject<IniObject>(json);
                iniObj.NisSetting = new NisSetting();
                if (string.IsNullOrEmpty(iniObj.NisSystem.SetServerMode))
                {
                    if (server_mode == ServerMode.Auto)
                    {
                        //依取得IP自動判斷
                        NisSetting set_obj = iniObj.NisSettingList.Find(c => c.ServerIP == localIp);
                        if (set_obj != null)
                        {
                            iniObj.NisSetting = set_obj;
                        }
                        else
                        {
                            //IP未被找到，使用Maya設定檔
                            set_obj = iniObj.NisSettingList.Find(c => c.ServerMode == ServerMode.Maya);
                            if (set_obj != null)
                            {
                                iniObj.NisSetting = set_obj;
                            }
                        }
                    }
                    else
                    {
                        //依設定模式選擇設定檔
                        NisSetting set_obj = iniObj.NisSettingList.Find(c => c.ServerMode == server_mode);
                        if (set_obj != null)
                        {
                            iniObj.NisSetting = set_obj;
                        }
                    }
                }
                else
                {
                    //依強制設定模式選擇設定檔
                    NisSetting set_obj = iniObj.NisSettingList.Find(c => c.ServerMode.ToString() == iniObj.NisSystem.SetServerMode);
                    if (set_obj != null)
                    {
                        iniObj.NisSetting = set_obj;
                    }
                }
                r.Close();
                r.Dispose();
            }
            return iniObj.NisSetting.ServerMode;
        }

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        #region Json Ini
        /// <summary>
        /// 連線設定
        /// </summary>
        public class Connection
        {
            /// <summary>DB Service Name</summary>
            public string ServiceName { get; set; }
            /// <summary>DB User ID</summary>
            public string User { get; set; }
            /// <summary>DB Password</summary>
            public string Password { get; set; }
            /// <summary>DB Provider</summary>
            public string Provider { get; set; }
            /// <summary>Web Service Url</summary>
            public string WebServiceUrl { get; set; }
            /// <summary>DataSource</summary>
            public string DataSource { get; set; }
            /// <summary>EKGUrl</summary>
            public string EKGUrl { get; set; }
            /// <summary>AISUrl</summary>
            public string AISUrl { get; set; }
        }

        /// <summary>
        /// 檔案位置
        /// </summary>
        public class FilePath
        {
            /// <summary>執行檔位置</summary>
            public string PDFexe { get; set; }
            /// <summary>Html轉PDF位置</summary>
            public string PDFPath { get; set; }
        }

        /// <summary>
        /// 設定
        /// </summary>
        public class NisSetting
        {
            /// <summary>伺服器模式</summary>
            public ServerMode ServerMode { get; set; }
            /// <summary>Service IP</summary>
            public string ServerIP { get; set; }
            /// <summary>連線設定</summary>
            public Connection Connection { get; set; }
            /// <summary>檔案位置</summary>
            public FilePath FilePath { get; set; }
        }

        /// <summary>
        /// 系統
        /// </summary>
        public class NisSystem
        {
            /// <summary>強制設定ServerMode</summary>
            public string SetServerMode { get; set; }
            /// <summary>語系</summary>
            public string Language { get; set; }
            /// <summary> </summary>
            public string SwitchAssessmentInto { get; set; }
            public int ChrNoLength { get; set; }
            /// <summary>自動登出</summary>
            public bool AutoLogout { get; set; }
            /// <summary>註銷期</summary>
            public int LogoutPeriod { get; set; }
            public string AdminUserNo { get; set; }
            public string AdminCaegory { get; set; }
            /// <summary>鎖定天數</summary>
            public int LockDay { get; set; }            
        }

        /// <summary>
        /// 簽章設定
        /// </summary>
        public class ElectSign
        {
            /// <summary>列印表尾</summary>
            public bool PrintFoot { get; set; }
            /// <summary>輸出檔案</summary>
            public bool OutputFile { get; set; }
            /// <summary>輸出檔案路徑</summary>
            public string OutputPath { get; set; }
            /// <summary>EMR參考</summary>
            public string EMRReference { get; set; }
            /// <summary>醫院ID</summary>
            public string HospitalNHIID { get; set; }
            /// <summary>醫院名稱</summary>
            public string HospitalName { get; set; }
        }

        /// <summary>
        /// Json Ini Class
        /// </summary>
        public class IniObject
        {
            /// <summary>設定清單</summary>
            public List<NisSetting> NisSettingList { get; set; }
            /// <summary>設定</summary>
            public NisSetting NisSetting { get; set; }
            /// <summary>系統</summary>
            public NisSystem NisSystem { get; set; }
            /// <summary>簽章設定</summary>
            public ElectSign ElectSign { get; set; }
        }

        public enum ServerMode
        {
            Auto = 0,
            Maya = 1,
            UAT = 2,
            Production = 3,
            Vpn_local =4,
            Production_new = 5
        }
        #endregion
        private void OnTimedEvent(object source, System.Timers.ElapsedEventArgs e)
        {
            if ("" == iniObj.NisSetting.ServerIP.ToString())
            {
                int? num = null;
                if (status)
                {
                    status = false;
                    num = base_controller.Insert_VIP_CareRecord();
                    if (num != null) // 防計時器塞車
                    {
                        status = true;
                    }
                }
            }
            
        }        
    }
}