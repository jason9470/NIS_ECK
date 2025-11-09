using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NIS.Data
{

    /// <summary>
    /// 系統公告
    /// </summary>
    public class AnnouncmentItem {
        public int sa_id { set; get; }
        public string sa_desc { set; get; }
        public DateTime sa_dateline { set; get; }
        public int sa_sort { set; get; }

        public AnnouncmentItem(int i_sa_id, string i_sa_desc, DateTime i_sa_dateline, int i_sa_sort) {
            this.sa_id = i_sa_id;
            this.sa_desc = i_sa_desc;
            this.sa_dateline = i_sa_dateline;
            this.sa_sort = i_sa_sort;
        }
    }

    /// <summary>
    /// 我的最愛清單
    /// </summary>
    public class FavItem {
        public string module_url { set; get; }
        public string module_name { set; get; }
        public string check { set; get; }
        public string hide { set; get; }

        public FavItem(string module_url, string module_name, string check, string hide) {
            this.module_url = module_url;
            this.module_name = module_name;
            this.check = check;
            this.hide = hide;
        }
    }

    /// <summary> 模組項目 </summary>
    public class ModelItem {

        /// <summary> 標籤class名稱 </summary>
        public string classname { set; get; }

        /// <summary> 按鈕名稱 </summary>
        public string button_name { set; get; }

        /// <summary> 模組位置 </summary>
        public string model_url { set; get; }

        /// <summary> 是否要檢查有無病人 </summary>
        public string check_patinfo { set; get; }

        /// <summary> 隱藏病人資訊 </summary>
        public string hide_patinfo { set; get; }

        /// <summary> 開新視窗 </summary>
        public string open_window { set; get; }

        /// <summary> 設為預設視窗 </summary>
        public string set_default_page { set; get; }

        /// <summary> 設為使用者有此功能(僅設定權限使用) </summary>
        public string user_owner { set; get; }

        /// <summary> 模組ID(僅設定權限使用) </summary>
        public string mo_id { set; get; }

        /// <summary> 模組開啟方式 {W:Web A:Application} </summary>
        public string model_type { set; get; }

        /// <summary> 是否有模組權限 </summary>
        public string has_model { set; get; }

    }

    /// <summary> 系統參數項目 </summary>
    public class SysParamItem {
        
        /// <summary> 參數序號 </summary>
        public string p_id { set; get; }

        /// <summary> 模組名稱 </summary>
        public string p_model { set; get; }

        /// <summary> 群組名稱 </summary>
        public string p_group { set; get; }

        /// <summary> 顯示文字 </summary>
        public string p_name { set; get; }

        /// <summary> 值 </summary>
        public string p_value { set; get; }

        /// <summary> 語系 </summary>
        public string p_lang { set; get; }

        /// <summary> 排序 </summary>
        public string p_sort { set; get; }

        /// <summary> 備註 </summary>
        public string p_memo { set; get; }

    }

    public class FallAssessMeasure
    {
        public string Name { set; get; }
        public int Num { set; get; }

        public FallAssessMeasure(string Name, int Num)
        {
            this.Name = Name;
            this.Num = Num;
        }
    }
    /// <summary> 簽章清單 </summary>
    public class SignList
    {
        /// <summary> 日期 </summary>
        public string Date { set; get; }
        /// <summary> 病歷號 </summary>
        public string Feeno { set; get; }
        /// <summary> 病歷號 </summary>
        public string Chartno { set; get; }
        /// <summary> 床號 </summary>
        public string Bedno { set; get; }
        /// <summary> 病人姓名 </summary>
        public string PatientName { set; get; }
        /// <summary> 班別 </summary>
        public string Shift { set; get; }
        /// <summary> 送簽人 </summary>
        public string Signer { set; get; }
        /// <summary> 指導者 </summary>
        public string GuiderId { set; get; }
        /// <summary> 代簽人 </summary>
        public string RepId { set; get; }
        /// <summary> 狀態 </summary>
        public string STATUS { set; get; }

    }
    public class SignListDtl
    {
        /// <summary> 簽章包明細序號 </summary>
        public string PACD_ID { set; get; }
        /// <summary> 簽章包序號 </summary>
        public string PAC_ID { set; get; }
        /// <summary> 紀錄key </summary>
        public string RECORD_KEY { set; get; }
        /// <summary> 紀錄日期 </summary>
        public string RECORD_DATE { set; get; }
        /// <summary> 紀錄時間 </summary>
        public string RECORD_TIME { set; get; }
        /// <summary> FOCUS </summary>
        public string FOCUS { set; get; }
        /// <summary> 內容 </summary>
        public string CONTENT { set; get; }
        /// <summary> 是否有EKG </summary>
        public string HAS_EKG { set; get; }
        /// <summary> 建立人ID </summary>
        public string CREATE_ID { set; get; }
        /// <summary> 建立人姓名 </summary>
        public string CREATE_NAME { set; get; }
        /// <summary> 建立日期 </summary>
        public string CREATE_DATE { set; get; }
        /// <summary> 來源 </summary>
        public string SELF { set; get; }
        /// <summary> 來源 </summary>
        public string EKG { set; get; }

    }

}