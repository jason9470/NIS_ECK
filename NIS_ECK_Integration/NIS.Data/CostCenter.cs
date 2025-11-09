using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace NIS.Data
{
    /// <summary> 成本中心列表 </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class CostCenterList
    {
        /// <summary> 成本中心代碼 </summary>
        [JsonProperty]
        public string CostCenterCode { set; get; }
        /// <summary> 成本中心描述 </summary>
        [JsonProperty]
        public string CCCDescription { set; get; }
    }

    /// <summary> 科別列表 </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class DeptList
    {
        /// <summary> 科別代碼 </summary>
        [JsonProperty]
        public string DeptCode { set; get; }

        /// <summary> 科別名稱 </summary>
        [JsonProperty]
        public string DeptName { set; get; }

    }
}