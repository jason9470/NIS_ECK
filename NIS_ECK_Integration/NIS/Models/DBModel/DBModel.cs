using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NIS.Models.DBModel
{
    public class DBModel
    {
        public class CARERECORD_DATA
        {
            /// <summary>CARERECORD_ID</summary>
            public string CARERECORD_ID { get; set; }
            /// <summary>CREATNO</summary>
            public string CREATNO { get; set; }
            /// <summary>GUIDE_NO</summary>
            public string GUIDE_NO { get; set; }
            /// <summary>UPDNO</summary>
            public string UPDNO { get; set; }
            /// <summary>CREATNAME</summary>
            public string CREATNAME { get; set; }
            /// <summary>CREATTIME</summary>
            public string CREATTIME { get; set; }
            /// <summary>UPDTIME</summary>
            public string UPDTIME { get; set; }
            /// <summary>RECORDTIME</summary>
            public string RECORDTIME { get; set; }
            /// <summary>FEENO</summary>
            public string FEENO { get; set; }
            /// <summary>TITLE</summary>
            public string TITLE { get; set; }
            /// <summary>C</summary>
            public string C { get; set; }
            /// <summary>C_OTHER</summary>
            public string C_OTHER { get; set; }
            /// <summary>S</summary>
            public string S { get; set; }
            /// <summary>S_OTHER</summary>
            public string S_OTHER { get; set; }
            /// <summary>O</summary>
            public string O { get; set; }
            /// <summary>O_OTHER</summary>
            public string O_OTHER { get; set; }
            /// <summary>I</summary>
            public string I { get; set; }
            /// <summary>I_OTHER</summary>
            public string I_OTHER { get; set; }
            /// <summary>E</summary>
            public string E { get; set; }
            /// <summary>E_OTHER</summary>
            public string E_OTHER { get; set; }
            /// <summary>DELETED</summary>
            public string DELETED { get; set; }
            /// <summary>SELF</summary>
            public string SELF { get; set; }
            /// <summary>SIGN</summary>
            public string SIGN { get; set; }
            /// <summary>VER</summary>
            public string VER { get; set; }
            /// <summary>SIGNTIME</summary>
            public string SIGNTIME { get; set; }
            /// <summary>SIGNSTATUS</summary>
            public string SIGNSTATUS { get; set; }
            /// <summary>EKG</summary>
            public string EKG { get; set; }
            /// <summary>VIP_ID</summary>
            public string VIP_ID { get; set; }
        }
        public class CAREPLAN //護理計畫總表
        {
            /// <summary>批價序號</summary>
            public string FEENO { get; set; }
            /// <summary>健康問題 ID</summary>
            public string TOPICID { get; set; }
            /// <summary>編號</summary>
            public string PNO { get; set; }
            /// <summary>健康問題描述</summary>
            public string TOPICDESC { get; set; }
            /// <summary>計畫起始日</summary>
            public string PLANSTARTDATE { get; set; }
            /// <summary>計畫結束日</summary>
            public string PLANENDDATE { get; set; }
            /// <summary>紀錄者員編</summary>
            public string RECORDER { get; set; }
            /// <summary>紀錄者姓名</summary>
            public string RECORDER_NAME { get; set; }
            /// <summary>紀錄者單位</summary>
            public string RECORDER_UNIT { get; set; }
            /// <summary>最後修改者</summary>
            public string MODIFY_ID { get; set; }
            /// <summary>最後修改者姓名</summary>
            public string MODIFY_NAME { get; set; }
            /// <summary>最後修改者單位</summary>
            public string MODIFY_UNIT { get; set; }
            /// <summary>最後修改時間</summary>
            public string MODIFT_DATE { get; set; }
            /// <summary>是否有修改</summary>
            public bool MODIFT_bool { get; set; }
            /// <summary>最後評值者</summary>
            public string ASSESS_ID { get; set; }
            /// <summary>最後評值者姓名</summary>
            public string ASSESS_NAME { get; set; }
            /// <summary>最後評值者單位</summary>
            public string ASSESS_UNIT { get; set; }
            /// <summary>最後評值時間</summary>
            public string ASSESS_DATE { get; set; }
            /// <summary>是否有評值</summary>
            public bool ASSESS_bool { get; set; }
        }
    }
    public class dataObj
    {
        /// <summary>
        /// 欲查詢的病歷號  (格式: yyyy-MM-dd)
        /// </summary>
        public string chrno { get; set; }
        /// <summary>
        /// 批價序號  (格式: yyyy-MM-dd)
        /// </summary>
        public string feeno { get; set; }
        /// <summary>
        /// 資料日期  (格式: yyyy-MM-dd)
        /// </summary>
        public string date { get; set; }
        /// <summary>
        /// 是否有資料( Y:有, N:無 )
        /// </summary>
        public string hasData { get; set; }
    }
    public class DB_NIS_SYS_PARAMS
    {
        /// <summary>編號</summary>
        public string P_ID { get; set; } = "";
        /// <summary>模組名稱</summary>
        public string P_MODEL { get; set; } = "";
        /// <summary>所屬類別</summary>
        public string P_GROUP { get; set; } = "";
        /// <summary>名稱</summary>
        public string P_NAME { get; set; } = "";
        /// <summary>值</summary>
        public string P_VALUE { get; set; } = "";
        /// <summary>語言</summary>
        public string P_LANG { get; set; } = "";
        /// <summary>排序</summary>
        public int P_SORT { get; set; } = 0;
        /// <summary>註解</summary>
        public string P_MEMO { get; set; } = "";
    }
    public class DB_NIS_IO_DATA
    {
        /// <summary>IO_ROW(PK)</summary>
        public string IO_ROW { get; set; } = "";
        /// <summary>ID</summary>
        public string IO_ID { get; set; } = "";
        /// <summary>住院(批價)序號</summary>
        public string FEENO { get; set; } = "";
        /// <summary>紀錄時間</summary>
        public string CREATTIME { get; set; } = "";
        /// <summary>建立者ID</summary>
        public string CREANO { get; set; } = "";
        /// <summary>種類序號</summary>
        public string TYPEID { get; set; } = "";
        /// <summary>項目序號</summary>
        public string ITEMID { get; set; } = "";
        /// <summary>數量</summary>
        public double AMOUNT { get; set; } = 0;
        /// <summary>卡洛里</summary>
        public int CALORIES { get; set; } = 0;
        /// <summary>單位</summary>
        public string AMOUNT_UNIT { get; set; } = "";
        /// <summary></summary>
        public string POSITION { get; set; } = "";
        /// <summary>原因</summary>
        public string REASON { get; set; } = "";
        /// <summary>刪除註記</summary>
        public string DELETED { get; set; } = "";
        /// <summary>說明細項</summary>
        public string EXPLANATION_ITEM { get; set; } = "";
        /// <summary>建立者姓名</summary>
        public string CREATENAME { get; set; } = "";
        /// <summary>修改者</summary>
        public string UPDNO { get; set; } = "";
        /// <summary>修改者姓名</summary>
        public string UPDNAME { get; set; } = "";
        /// <summary>修改時間</summary>
        public string UPDTIME { get; set; } = "";
        /// <summary>備註</summary>
        public string REMARK { get; set; } = "";
    }
    /// <summary>
    /// IO_DATA 外加 FUNC_IO_DATA
    /// </summary>
    public class DB_NIS_IO_DATA_FUNC_IO_NAME : DB_NIS_IO_DATA
    {
        /// <summary>AMOUNT_ALL</summary>
        public Decimal AMOUNT_ALL { get; set; }
        /// <summary>P_GROUP</summary>
        public string P_GROUP { get; set; } = "";
        /// <summary>P_NAME</summary>
        public string P_NAME { get; set; } = "";
        /// <summary>名稱</summary>
        public string NAME { get; set; } = "";
        /// <summary>攝入輸出顏色ID</summary>
        public string COLORID { get; set; } = "";
        /// <summary>攝入輸出顏色</summary>
        public string COLORNAME { get; set; } = "";
        /// <summary>攝入輸出-顏色(其他)</summary>
        public string COLOROTHER { get; set; } = "";
        /// <summary>攝入輸出性質ID</summary>
        public string NATUREID { get; set; } = "";
        /// <summary>攝入輸出性質</summary>
        public string NATURENAME { get; set; } = "";
        /// <summary>攝入輸出-性質(其他)</summary>
        public string NATUREOTHER { get; set; } = "";
        /// <summary>攝入輸出味道ID</summary>
        public string TASTEID { get; set; } = "";
        /// <summary>攝入輸出味道</summary>
        public string TASTENAME { get; set; } = "";
        /// <summary>攝入輸出-味道(其他)</summary>
        public string TASTEOTHER { get; set; } = "";
    }
    /// <summary>
    /// TPRAantibiotic
    /// </summary>
    public class TPRAantibiotic
    {
        /// <summary>日期</summary>
        public DateTime UseDate { get; set; }
        /// <summary>名稱</summary>
        public string content { get; set; }
    }

    public class nis_usingURL_variable
    {
        /// <summary>住院序號(PK)</summary>
        public string feeno { get; set; } = "";
        /// <summary>查詢日期範圍(開始時間)</summary>
        public DateTime start_date { get; set; }
        /// <summary>查詢日期範圍(結束時間)</summary>
        public DateTime end_date { get; set; }
        /// <summary>建立者ID</summary>
        public string userno { get; set; } = "";
        /// <summary>建立者密碼</summary>
        public string password { get; set; } = "";
        /// <summary>浮動序號</summary>
        public string Token { get; set; } = "";
        /// <summary>目標頁ID</summary>
        public string module_id { get; set; } = "";
    }

    public class ID_List
    {
        /// <summary>定義性特徵id</summary>
        public string SERIAL { get; set; }
        /// <summary>TALBE來源</summary>
        public string SOURCE_TAG { get; set; }

    }

    /// <summary>
    /// Return Json Data
    /// </summary>
    public class ReturnJsonData
    {
        /// <summary>回傳狀態</summary>
        public string Status { get; set; }
        /// <summary>ID(沿用資料)</summary>
        public string ReturnData { get; set; }

    }
    /// <summary>
    /// CarePlan_History
    /// </summary>
    public class CarePlan_History
    {
        public string PK_ID { get; set; }
        public string RECORDID { get; set; }
        public string TARGETSTATUS { get; set; }
        public string REASON { get; set; }
        public string ASSESS_ID { get; set; }
        public string ASSESS_NAME { get; set; }
        public DateTime ASSESS_DATE { get; set; }
        public string CPFEATUREID_OBJ { get; set; }
        public string CPRF_OBJ { get; set; }
        public string CPMEASURE_OBJ { get; set; }
        public string CARERECORDID { get; set; }
        public string TARGETID { get; set; }
        public string DELETED { get; set; }
    }
    #region TALK_TABLE
    /// <summary>
    /// DATAModel
    /// </summary>
    public class TALK_TABLE
    {
        public string PK_ID { get; set; }
        public DateTime CREATE_TIME { get; set; }
        public string Action_Type { get; set; }
        public string Controller { get; set; }
        public string UserId { get; set; }
        public string Fee_no { get; set; }
        public DateTime Start_Datetime { get; set; }
        public DateTime End_Datetime { get; set; }
        public string Cellular_Phone { get; set; }
        public string STATUS { get; set; }
        public string Parameter_CMD { get; set; }
        public string Parameter { get; set; }
        public string VOICE_PATH { get; set; }
    }
    #endregion

    #region TEMPREADING 血糖中繼表
    public class TEMPREADING
    {
        // 資料流水號
        public int ID { get; set; }

        // 操作人員ID
        public string OPERATORID { get; set; }

        // 操作人員單位代碼
        public string OPERATORDEPT { get; set; }

        // 病患ID
        public string PID { get; set; }

        // 試片批號
        public string STRIPLOT { get; set; }

        // 量測時間
        public DateTime DATETIME { get; set; }

        // 型態(1-血糖、2-血酮)
        public string TYPE { get; set; }

        // 讀值(20-600、>600、<20)
        public string READING { get; set; }

        // 1-操作人員未完成教育訓練
        public string NOTECODE { get; set; }

        // 機器序號
        public string SERIALNUMBER { get; set; }

        // 時段(AC-飯前、PC-飯後、ST-即時)
        public string MEDICALORDER { get; set; }

        // 上傳註記(Y-參考表身)
        public string UPSTATUS { get; set; }

        // 上傳失敗備註
        public string UPNOTE { get; set; }

        // 資料建立時間
        public DateTime CDATETIME { get; set; }

        // GKReading資料表id(五鼎用)
        public int GKID { get; set; }
    }
    #endregion

    #region TEMPREADINGUP 血糖中繼表資料拿取紀錄
    public class TEMPREADINGUP
    {
        public int ID { get; set; }
        public string UP_SYSTEM { get; set; }
        public DateTime UP_DATETIME { get; set; }
        public string UP_NOTE { get; set; }
        public string UP_STATUS { get; set; }
    }
    #endregion
}