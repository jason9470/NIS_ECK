using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace NIS.Data
{
    /// <summary>
    /// 使用者資訊
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class UserInfo
    {
        /// <summary> 員工編號 </summary>
        [JsonProperty]
        public string EmployeesNo { set; get; }

        /// <summary> 員工身份證字號 </summary>
        [JsonProperty]
        public string UserID { set; get; }

        /// <summary> 員工姓名 </summary>
        [JsonProperty]
        public string EmployeesName { set; get; }

        /// <summary> 職業類別(N:護理師,D:醫師) </summary>
        [JsonProperty]
        public string Category { set; get; }

        /// <summary> 所屬成本中心代碼 </summary>
        [JsonProperty]
        public string CostCenterCode { set; get; }

        /// <summary> 職級 </summary>
        [JsonProperty]
        public string JobGrade { set; get; }

        /// <summary> 成本中心代碼 </summary>
        [JsonProperty]
        public string CostCenterNo { set; get; }

        /// <summary> 成本中心名稱 </summary>
        [JsonProperty]
        public string CostCenterName { set; get; }

        /// <summary> 密碼 </summary>
        [JsonProperty]
        public string Pwd { set; get; }

        /// <summary> 指導者 </summary>
        [JsonProperty]
        public string Guider { set; get; }
        /// <summary> 指導者所屬單位 </summary>
        [JsonProperty]
        public string Guider_CCCode { set; get; }
    }

    /// <summary>
    /// 使用者登入資料檔
    /// </summary>
    public class UserData
    {
        public string user_id { set; get; }
        private string user_sec_code { set; get; }
        private string a_key = "80265495", b_key = "maya.com";

        public void SetSecCode(string org_password)
        {
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();
            des.Key = Encoding.ASCII.GetBytes(this.a_key);
            des.IV = Encoding.ASCII.GetBytes(this.b_key);
            byte[] s = Encoding.ASCII.GetBytes(org_password);
            ICryptoTransform desencrypt = des.CreateEncryptor();
            this.user_sec_code = BitConverter.ToString(desencrypt.TransformFinalBlock(s, 0, s.Length)).Replace("-", string.Empty);
        }

        public string GetSecCode()
        {
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();
            des.Key = Encoding.ASCII.GetBytes(this.a_key);
            des.IV = Encoding.ASCII.GetBytes(this.b_key);
            byte[] s = new byte[this.user_sec_code.Length / 2];
            int j = 0;
            for (int i = 0; i < this.user_sec_code.Length / 2; i++)
            {
                s[i] = Byte.Parse(this.user_sec_code[j].ToString() + this.user_sec_code[j + 1].ToString(), System.Globalization.NumberStyles.HexNumber);
                j += 2;
            }
            ICryptoTransform desencrypt = des.CreateDecryptor();
            return Encoding.ASCII.GetString(desencrypt.TransformFinalBlock(s, 0, s.Length));
        }

    }

    /// <summary>
    /// 班表
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    //public class ScheduleList
    public class ShiftData
    {
        /// <summary> 員工編號 </summary>
        [JsonProperty]
        public string employe_no { set; get; }

        /// <summary> 員工姓名 </summary>
        [JsonProperty]
        public string employe_name { set; get; }

        /// <summary> 職級 </summary>
        [JsonProperty]
        public string employe_title { set; get; }

        /// <summary> 班別 D:白班 N:小夜 E:大夜 </summary>
        [JsonProperty]
        public string shift_cate { set; get; }

        /// <summary> 單位名稱 </summary>
        [JsonProperty]
        public string cost_name { set; get; }

    }
}