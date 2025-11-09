using System;
using System.IO;
using System.Text;
using System.Web;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Com.Mayaminer;

namespace NIS.Models
{
    public class TINI : IDisposable
    {
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        public static extern int GetPrivateProfileString(string lpAppName,
                                                             string lpKeyName,
                                                             string lpDefault,
                                                             IntPtr lpReturnedString,
                                                             uint nSize,
                                                             string lpFileName);
        private bool bDisposed = false;
        private string _FilePath = string.Empty;

        public string FilePath
        {
            get
            {
                if (_FilePath == null)
                    return string.Empty;
                else
                    return _FilePath;
            }
            set
            {
                if (_FilePath != value)
                    _FilePath = value;
            }
        }

        //constructor
        public TINI(String path)
        {
            _FilePath = path;
        }

        //destructor
        ~TINI()
        {
            Dispose(true);
        }

        //release used resources
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);//Requests that the system not call the finalizer for the specified object
        }

        //release used resources (給系統呼叫的)
        protected virtual void Dispose(bool isDisposing)
        {
            if (bDisposed)
            {
                return;
            }

            bDisposed = true;
        }

        private static string[] ConvertNullSeperatedStringToStringArray(IntPtr ptr, int valLength)
        {
            string[] retval;

            if (valLength == 0)
            {
                //Return an empty array.
                retval = new string[0];
            }
            else
            {
                //Convert the buffer into a string.  Decrease the length 
                //by 1 so that we remove the second null off the end.
                string buff = Marshal.PtrToStringAuto(ptr, valLength - 1);

                //Parse the buffer into an array of strings by searching for nulls.
                retval = buff.Split('\0');
            }

            return retval;
        }

        //set key value
        public void SetKeyValue(string iniSection, string iniKey, string iniValue)
        {
            WritePrivateProfileString(iniSection, iniKey, iniValue, this._FilePath);
        }

        //get key value
        public string GetKeyValue(string iniSection, string iniKey)
        {
            StringBuilder strBuilder = new StringBuilder(255);

            int i = GetPrivateProfileString(iniSection, iniKey, "", strBuilder, 255, this._FilePath);

            return strBuilder.ToString();
        }

        public string[] GetKeyNames(string sectionName, string FilePath, int MaxSectionSize)
        {
            int len;
            string[] retval;

            if (sectionName == null)
                throw new ArgumentNullException("sectionName");

            IntPtr ptr = Marshal.AllocCoTaskMem(MaxSectionSize);

            try
            {
                //Get the section names into the buffer.
                len = GetPrivateProfileString(sectionName,
                                                            null,
                                                            null,
                                                            ptr,
                                                            (uint)MaxSectionSize,
                                                            FilePath);

                retval = ConvertNullSeperatedStringToStringArray(ptr, len);
            }
            finally
            {
                //Free the buffer
                Marshal.FreeCoTaskMem(ptr);
            }

            return retval;
        }
    }

    public static class IniFile
    {

        private static string iniPath = AppDomain.CurrentDomain.BaseDirectory + "App_Config\\NISConfig.ini";

        public static string GetConnStr() 
        {
            //string servicename = GetConfig("Connection", "ServiceName").Trim();
            //string user = GetConfig("Connection", "User").Trim();
            //string Provider = GetConfig("Connection", "Provider").Trim();
            //string password = SecurityTool.DecodeDES( GetConfig("Connection","Password"), "aaaaaaaa", "bbbbbbbb").Trim();//從這下手
            string servicename = NIS.MvcApplication.iniObj.NisSetting.Connection.ServiceName;
            string user = NIS.MvcApplication.iniObj.NisSetting.Connection.User;
            string Provider = NIS.MvcApplication.iniObj.NisSetting.Connection.Provider;
            string password = SecurityTool.DecodeDES(NIS.MvcApplication.iniObj.NisSetting.Connection.Password, "aaaaaaaa", "bbbbbbbb").Trim();//從這下手             

            return "Provider=" + Provider + ";Data Source=" + servicename + ";Persist Security Info=True;Password=" + password + ";User ID=" + user;
        }

        public static string GetConnStr2()
        {
            //Data Source=10.168.30.2:1521/mayadevelop;User Id=mdro;Password=mdro;";
            //string ip = GetConfig("Connection", "Ip").Trim();
            //string ServiceName = GetConfig("Connection", "ServiceName").Trim();
            //string user = GetConfig("Connection", "User").Trim(); 
            //string password = SecurityTool.DecodeDES(GetConfig("Connection", "Password"), "aaaaaaaa", "bbbbbbbb").Trim();//從這下手             

            string DataSource = NIS.MvcApplication.iniObj.NisSetting.Connection.DataSource;
            string user = NIS.MvcApplication.iniObj.NisSetting.Connection.User;
            string password = SecurityTool.DecodeDES(NIS.MvcApplication.iniObj.NisSetting.Connection.Password, "aaaaaaaa", "bbbbbbbb").Trim();//從這下手             
            return "Data Source=" + DataSource + ";Password=" + password + ";User ID=" + user;
        }


        /// <summary>
        /// 回傳ini的設定值
        /// </summary>
        public static string GetConfig(String session, String key)
        {
            StreamReader sr = new StreamReader( iniPath, Encoding.Default);
            String line = "", head = "", result = "";
            String[] value;
            char[] spChr = { '=' };

            while ((line = sr.ReadLine()) != null)
            {
                if (line.IndexOf("[") != -1 && line.IndexOf("]") != -1)
                {
                    head = line.Replace("[", "").Replace("]", "");
                    continue;
                }
                else
                {
                    if (head == session)
                    {
                        value = line.Split(spChr);
                        if (value[0] == key)
                        {
                            result = value[1];
                            break;
                        }
                    }
                    else
                    {
                        continue;
                    }
                }
            }
            sr.Close();
            line = null;
            head = null;
            value = null;
            spChr = null;
            sr = null;
            return result;
        }

        #region Write data to the ini file
        /// <summary>
        /// 將TPR的設定寫入ini file
        /// </summary>
        public static void WriteToIni(List<string> SettingValues, string section)
        {
            string FilePath = HttpContext.Current.Server.MapPath("/App_Config"),
                   key = string.Empty,
                   value = string.Empty,
                   tempStr = string.Empty;

            if (Directory.Exists(FilePath))
            {
                TINI iniObj = new TINI(FilePath + "\\NISConfig.ini");
                iniObj.SetKeyValue(section, null, null);//clear the previous settings

                for (int i = 0; i < SettingValues.Count; i++)
                {
                    tempStr = SettingValues[i];
                    key = tempStr.Substring(0, tempStr.IndexOf("="));
                    tempStr = tempStr.Remove(0, tempStr.IndexOf("=") + 1);
                    value = tempStr;

                    iniObj.SetKeyValue(section, key, value);
                }

                iniObj.Dispose();
            }
            else
            {

            }
        }
        
        //public static void WriteToIni(List<string> SettingValues, string section)

        public static string ReadFromIni(string section, string key)
        {
            string result = string.Empty;
            string FilePath = HttpContext.Current.Server.MapPath("/App_Config");

            TINI iniObj = new TINI(FilePath + "\\NISConfig.ini");

            result = iniObj.GetKeyValue(section, key);

            iniObj.Dispose();

            return result;
        }

        //get all keys in a section
        public static void GetKeys(string FilePath, string section, ref string[] keys)
        {

            TINI iniObj = new TINI(FilePath + "\\NISConfig.ini");
            keys = iniObj.GetKeyNames(section, FilePath + "\\NISConfig.ini", 32767);

            iniObj.Dispose();
        }
        #endregion

    }

}