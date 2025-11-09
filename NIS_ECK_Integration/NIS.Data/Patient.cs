using System;
using System.Collections.Generic;
using System.Web;
using Newtonsoft.Json;

namespace NIS.Data
{
    #region 資料

    /// <summary> 患者清單 </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class LabBorn
    {
        /// <summary> 產檢項目 </summary>
        [JsonProperty]
        public string Item { set; get; }

        /// <summary> 檢驗結果 </summary>
        [JsonProperty]
        public string Result { set; get; }

    }

    /// <summary> 患者清單 </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class PatientList
    {
        /// <summary> 病歷號 </summary>
        [JsonProperty]
        public string ChrNo { set; get; }

        /// <summary> 批價序號 </summary>
        [JsonProperty]
        public string FeeNo { set; get; }

        /// <summary> 床號 </summary>
        [JsonProperty]
        public string BedNo { set; get; }

        /// <summary> 患者姓名 </summary>
        [JsonProperty]
        public string PatientName { set; get; }

        /// <summary> 感染註記 </summary>
        [JsonProperty]
        public string Note { set; get; }

        /// <summary> 是否有新醫囑 </summary>
        [JsonProperty]
        public Boolean New_Med_Advice { set; get; }

    }

    /// <summary> 轉床清單 </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class BedTransList
    {
        /// <summary> 轉床結束日期 </summary>
        [JsonProperty]
        public DateTime TransDate { set; get; }

        /// <summary> 床號 </summary>
        [JsonProperty]
        public string BedNo { set; get; }

        /// <summary> 成本中心/護理站代碼 </summary>
        [JsonProperty]
        public string CostCode { set; get; }

        /// <summary> 成本中心/護理站名稱 </summary>
        [JsonProperty]
        public string CostDesc { set; get; }

    }

    /// <summary> 轉床清單_詳細資料 </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class TransListDetail
    {
        /// <summary> 批價序號 </summary>
        [JsonProperty]
        public string FeeNo { set; get; }

        /// <summary> 床號 </summary>
        [JsonProperty]
        public string BedNo { set; get; }

        /// <summary> 轉床結束日期 </summary>
        [JsonProperty]
        public DateTime TransDate { set; get; }

        /// <summary> 成本中心代碼 </summary>
        [JsonProperty]
        public string CostCenterNo { set; get; }

        /// <summary> 主治醫師姓名 </summary>
        [JsonProperty]
        public string DocName { set; get; }

        /// <summary> 科別名稱 </summary>
        [JsonProperty]
        public string DeptName { set; get; }

        /// <summary> 主診斷碼 </summary>
        [JsonProperty]
        public string ICD9_code1 { set; get; }
    }

    /// <summary> 患者資訊 </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class PatientInfo
    {
        /// <summary> 批價序號 </summary>
        [JsonProperty]
        public string FeeNo { set; get; }

        /// <summary> 病歷號 </summary>
        [JsonProperty]
        public string ChartNo { set; get; }

        /// <summary> 身份證字號 </summary>
        [JsonProperty]
        public string PatientID { set; get; }

        /// <summary> 床號 </summary>
        [JsonProperty]
        public string BedNo { set; get; }

        /// <summary> 患者性別 </summary>
        [JsonProperty]
        public string PatientGender { set; get; }

        /// <summary> 患者姓名 </summary>
        [JsonProperty]
        public string PatientName { set; get; }

        /// <summary> 患者出生日期 </summary>
        [JsonProperty]
        public DateTime Birthday { set; get; }

        /// <summary> 患者年齡 </summary>
        [JsonProperty]
        public int Age { set; get; }

        /// <summary> 主治醫師姓名 </summary>
        [JsonProperty]
        public string DocName { set; get; }

        /// <summary> 主治醫師編號 </summary>
        [JsonProperty]
        public string DocNo { set; get; }

        /// <summary> 人院日期 </summary>
        [JsonProperty]
        public DateTime InDate { set; get; }

        /// <summary> 住院天數 </summary>
        [JsonProperty]
        public int InDay { set; get; }

        /// <summary> 出院日期 </summary>
        [JsonProperty]
        public DateTime OutDate { set; get; }

        /// <summary> 預計出院日期 </summary>
        [JsonProperty]
        public DateTime Expected_OutDate { set; get; }

        /// <summary> 轉床日期 </summary>
        [JsonProperty]
        public DateTime TBDate { set; get; }

        /// <summary> 主診斷碼 </summary>
        [JsonProperty]
        public string ICD9_code1 { set; get; }

        /// <summary> 診斷碼 </summary>
        [JsonProperty]
        public string ICD9_code2 { set; get; }

        /// <summary> 診斷碼 </summary>
        [JsonProperty]
        public string ICD9_code3 { set; get; }

        /// <summary> 診斷碼 </summary>
        [JsonProperty]
        public string ICD9_code4 { set; get; }

        /// <summary> 診斷碼 </summary>
        [JsonProperty]
        public string ICD9_code5 { set; get; }

        /// <summary> 癌症代碼 </summary>
        [JsonProperty]
        public string CancerICD9 { set; get; }

        /// <summary> 拒絕急救 </summary>
        [JsonProperty]
        public string DNR { set; get; }

        /// <summary> 安寧病房 </summary>
        [JsonProperty]
        public string Hospice { set; get; }

        /// <summary> 自殺傷 </summary>
        [JsonProperty]
        public string Suicide { set; get; }//**自殺數據**

        /// <summary> 器官捐贈 </summary>
        [JsonProperty]
        public string OrganDonation { set; get; }

        /// <summary> 過敏 </summary>
        [JsonProperty]
        public string Allergy { set; get; }

        /// <summary> 保密 </summary>
        [JsonProperty]
        public string Security { set; get; }

        /// <summary> 血型 </summary>
        [JsonProperty]
        public string Blood_Type { set; get; }

        /// <summary> RH </summary>
        [JsonProperty]
        public string RH_Type { set; get; }

        /// <summary> 備血日期 </summary>
        [JsonProperty]
        public DateTime Prepare_Date { set; get; }

        /// <summary> 備血量 </summary>
        [JsonProperty]
        public int Ttl_Gty { set; get; }

        /// <summary> 血種 </summary>
        [JsonProperty]
        public string Code_Desc { set; get; }

        /// <summary> 餘血量 </summary>
        [JsonProperty]
        public int Unrece_Gty { set; get; }

        /// <summary> 過敏紀錄 </summary>
        [JsonProperty]
        public string AllergyList { set; get; }

        /// <summary> 過敏紀錄說明 </summary>
        [JsonProperty]
        public string AllergyDesc { set; get; }

        /// <summary> 科別代碼 </summary>
        [JsonProperty]
        public string DeptNo { set; get; }

        /// <summary> 科別名稱 </summary>
        [JsonProperty]
        public string DeptName { set; get; }

        /// <summary> 成本中心代碼(舊) </summary>
        [JsonProperty]
        public string CostCenterNo { set; get; }

        /// <summary> 成本中心名稱 </summary>
        [JsonProperty]
        public string CostCenterName { set; get; }

        /// <summary> 成本中心代碼(新) </summary>
        [JsonProperty]
        public string CostCenterCode { set; get; }

        /// <summary> 身份別(健保/自費) </summary>
        [JsonProperty]
        public string PayInfo { set; get; }

        /// <summary> 患者地址 </summary>
        [JsonProperty]
        public string PatientAddress { set; get; }

        /// <summary> 患者家裡電話 </summary>
        [JsonProperty]
        public string PatientHomeNo { set; get; }

        /// <summary> 患者公司電話 </summary>
        [JsonProperty]
        public string PatientWorkNo { set; get; }

        /// <summary> 患者行動電話 </summary>
        [JsonProperty]
        public string PatientMobile { set; get; }

        /// <summary> 患者電子郵件 </summary>
        [JsonProperty]
        public string PatientEmail { set; get; }

        /// <summary> 患者婚姻狀況 </summary>
        [JsonProperty]
        public string PatientMarryStatus { set; get; }

        /// <summary> 患者宗教 </summary>
        [JsonProperty]
        public string PatientReligion { set; get; }

        // <summary> 患者出生地 </summary>
        [JsonProperty]
        public string PatientBirthPlace { set; get; }

        // <summary> 患者配偶姓名 </summary>
        [JsonProperty]
        public string PatientSpouseName { set; get; }

        // <summary> 緊急聯絡人姓名 </summary>
        [JsonProperty]
        public string ContactName { set; get; }

        // <summary> 緊急聯絡人關係 </summary>
        [JsonProperty]
        public string ContactRelationship { set; get; }

        /// <summary> 緊急聯絡人家裡電話 </summary>
        [JsonProperty]
        public string ContactHomeNo { set; get; }

        /// <summary> 緊急聯絡人公司電話 </summary>
        [JsonProperty]
        public string ContactWorkNo { set; get; }

        /// <summary> 緊急聯絡人行動電話 </summary>
        [JsonProperty]
        public string ContactMobile { set; get; }

        /// <summary> 緊急聯絡人電子郵件 </summary>
        [JsonProperty]
        public string Contactemail { set; get; }

        /// <summary> 護理評估代碼 </summary>
        [JsonProperty]
        public string Assessment { set; get; }

        /// <summary> 最後一次門診用藥 </summary>
        [JsonProperty]
        public string OpdMed { set; get; }

        /// <summary> 轉床日期 </summary>
        [JsonProperty]
        public DateTime TransDate { set; get; }

        /// <summary> 最後一次月經後最後一次檢驗 </summary>
        [JsonProperty]
        public string LMPLab { set; get; }

        /// <summary> 一歲以下年齡 </summary>
        [JsonProperty]
        public int Month { set; get; }

        /// <summary> 隔離欄位 </summary>
        [JsonProperty]
        public string IcnDiseaseStr { set; get; }

        /// <summary> 專師 </summary>
        [JsonProperty]
        public string NursePractitioner { set; get; }

        /// <summary> 是否病危 </summary>
        [JsonProperty]
        public string Terminally { set; get; }

        /// <summary> 是否有檢查 </summary>
        [JsonProperty]
        public string Exam { set; get; }

        /// <summary> 是否有手術 </summary>
        [JsonProperty]
        public string Surgery { set; get; }

        /// <summary> 是否有會診 </summary>
        [JsonProperty]
        public string Consultation { set; get; }

        /// <summary> Condition </summary>
        [JsonProperty]
        public string Condition { set; get; }

        /// <summary> 健保卡特殊註記 </summary>
        [JsonProperty]
        public string card_specialinfo { set; get; }

        /// <summary> 醫院特殊註記(對照健保卡特殊註記) </summary>
        [JsonProperty]
        public string hospital_specialinfo { set; get; }

        /// <summary>產科交班單代號</summary>
        [JsonProperty]
        public string duty_code { set; get; }
        //產嬰交班單: BM    , 一般交班單: 空白
    }

    /// <summary> 患者基本資料 </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class PatientBasic
    {
        /// <summary> 入院日期 </summary>
        [JsonProperty]
        public DateTime InDate { set; get; }

        /// <summary> 教育程度 (01:不識字02:小學03:初中04:高中05:大專或大學06:研究所以上) </summary>
        [JsonProperty]
        public string edu { set; get; }

        /// <summary> 職業 (01:無02:公務員03:農04:工05:商06:服務業07:教職08:家營09:退休10:自由業11:其它) </summary>
        [JsonProperty]
        public string Profession { set; get; }

        /// <summary> 患者姓名 </summary>
        [JsonProperty]
        public string PatientName { set; get; }

        /// <summary> 緊急聯絡人姓名 </summary>
        [JsonProperty]
        public string EmergencyContact { set; get; }

        /// <summary> 與患者關係 (01:本人02:父親03:母親04:祖父05:祖母06:外傭07:配偶08:子女09:兄弟姐妹10:其它) </summary>
        [JsonProperty]
        public string Relationship { set; get; }

    }

    /// <summary> 住院歷程 </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class InHistory
    {
        /// <summary> 住院次數 </summary>
        [JsonProperty]
        public int InTime { set; get; }

        /// <summary> 入院日期 </summary>
        [JsonProperty]
        public DateTime indate { set; get; }

        /// <summary> 出院日期 </summary>
        [JsonProperty]
        public DateTime outdate { set; get; }

        /// <summary> 入院原因 主診斷 ICD9 </summary>
        [JsonProperty]
        public string Description { set; get; }

        /// <summary> 批價序號 </summary>
        [JsonProperty]
        public string FeeNo { set; get; }

        /// <summary> 狀態 </summary>
        [JsonProperty]
        public string IpdFlag { set; get; }

        /// <summary> 診別 (急診 / 住院) </summary> 
        [JsonProperty]
        public string HIS_TYPE { set; get; }

        /// <summary> 成本中心 </summary>
        [JsonProperty]
        public string CostCode { set; get; }

        /// <summary> 住院科別名稱 </summary>
        [JsonProperty]
        public string DeptName { set; get; }

        /// <summary> 病歷號 </summary>
        [JsonProperty]
        public string ChrNo { set; get; }

        /// <summary> 姓名 </summary>
        [JsonProperty]
        public string PatName { set; get; }

        /// <summary> 性別 </summary>
        [JsonProperty]
        public string SexType { set; get; }
        [JsonProperty]
        public string DataZone { set; get; }
    }

    /// <summary> 轉床歷程 </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class BED_TRANS
    {
        /// <summary> 批價序號 </summary>
        [JsonProperty]
        public string FEENO { set; get; }

        /// <summary> 床號 </summary>
        [JsonProperty]
        public string BEDNO { set; get; }

        /// <summary> 成本中心代碼 </summary>
        [JsonProperty]
        public string CostCode { set; get; }

        // <summary> 成本中心名稱 </summary>
        [JsonProperty]
        public string CostName { set; get; }

        /// <summary> 開始日期 </summary>
        [JsonProperty]
        public DateTime BeginDate { set; get; }

        /// <summary> 結束日期 </summary>
        [JsonProperty]
        public DateTime EndDate { set; get; }

        /// <summary> 病歷號 </summary>
        [JsonProperty]
        public string ChartNo { set; get; }

        /// <summary> 患者姓名 </summary>
        [JsonProperty]
        public string PatName { set; get; }

        /// <summary> 出生日期 </summary>
        [JsonProperty]
        public DateTime Birthday { set; get; }

        /// <summary> 年齡(歲) </summary>
        [JsonProperty]
        public string AgeY { set; get; }

        /// <summary> 年齡(月) </summary>
        [JsonProperty]
        public string AgeM { set; get; }

        /// <summary> 性別 </summary>
        [JsonProperty]
        public string Gender { set; get; }

        /// <summary> 疾病碼 </summary>
        [JsonProperty]
        public string ICD9Code { set; get; }

        /// <summary> 疾病名稱 </summary>
        [JsonProperty]
        public string ICD9Desc { set; get; }

        /// <summary> 科別代碼 </summary>
        [JsonProperty]
        public string DepCode { set; get; }

        /// <summary> 科別名稱 </summary>
        [JsonProperty]
        public string DepName { set; get; }

        /// <summary> 主治醫師代碼 </summary>
        [JsonProperty]
        public string DocCode { set; get; }

        /// <summary> 主治醫師姓名 </summary>
        [JsonProperty]
        public string DocName { set; get; }
    }

    /// <summary>申請補輸列表</summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class Complement_List
    {
        /// <summary>是否通過</summary>
        [JsonProperty]
        public Boolean Status { set; get; }
    }


    #endregion

    #region 病房床號
    public class BedItem
    {
        /// <summary> 病房成本代碼 </summary>
        public string CostCenterCode { get; set; }
        /// <summary> 病房名稱 </summary>
        public string CostCenterName { get; set; }
        /// <summary> 床號代碼 </summary>
        public string BedNo { get; set; }
    }
    #endregion

    #region 交班
    /// <summary> 排班表 </summary>
    public class ShiftList
    {
        /// <summary> 床號 </summary>
        public string bedno { set; get; }

        /// <summary> 患者姓名 </summary>
        public string PatientName { set; get; }

        /// <summary> 小Leader </summary>
        public string leader_empno { set; get; }

        /// <summary> 檢查結果 </summary>
        public string response_name { set; get; }

        /// <summary> 檢查結果 </summary>
        public string response_empno { set; get; }

        /// <summary> 檢查結果 </summary>
        public string combine_name { set; get; }

        /// <summary> 檢查結果 </summary>
        public string combine_empno { set; get; }
    }

    /// <summary> 資料 </summary>
    public class TaskList
    {
        public PatientInfo ptinfo { set; get; }
        public List<OrderList> orderList { set; get; }
        public List<ExecResultList> execList { set; get; }

        public TaskList(PatientInfo i_patinfo, List<OrderList> i_orderList, List<ExecResultList> i_execList)
        {
            this.ptinfo = i_patinfo;
            this.orderList = i_orderList;
            this.execList = i_execList;
        }
    }

    public class ExecResultList
    {
        public string exec_name;

        public string fee_no { set; get; }

        public string sheet_no { set; get; }

        /// <summary> 醫囑內容 </summary>
        public string order_content { set; get; }

        /// <summary> 執行計畫 </summary>
        public string action { set; get; }

        /// <summary> 執行時段 </summary>
        public string exec_priod { set; get; }

        /// <summary> 執行結果 </summary>
        public DateTime exec_time { set; get; }

        /// <summary> 執行結果 </summary>
        public string exec_result { set; get; }

        /// <summary> 不執行原因 </summary>
        public string exec_reason { set; get; }

    }


    public class OrderList
    {
        public string sheet_no { set; get; }
        public string order_content { set; get; }
        public string[] set_priod { set; get; }
        public string set_action { set; get; }
        public string order_type { set; get; }//囑型
        public DateTime Dc_date { set; get; }
        /// <summary> DC簽收 </summary>
        [JsonProperty]
        public string DELETED { set; get; }
        /// <summary> DC簽收人 </summary>
        [JsonProperty]
        public string del_username { set; get; }
        /// <summary> 取消全部時間 </summary>
        public DateTime? canceltime { set; get; }


    }

    /// <summary> 交班資料 </summary>
    public class TransList
    {
        public PatientInfo ptinfo { set; get; }
        public string[] vital_info { set; get; }
        public string[] wait_info { set; get; }
        public string remark_info { set; get; }
        public string special_info { set; get; }
        public string shift_cate { set; get; }
        public string remarkExtra { set; get; }
        public string FRIDs { set; get; }
        public TransList(PatientInfo i_ptinfo, string[] i_vital_info = null, string[] i_wait_info = null, string i_remark_info = "", string i_special_info = "", string i_shift_cate = "", string remarkExtra = "", string FRIDs = "")
        {
            this.ptinfo = i_ptinfo;
            this.vital_info = i_vital_info;
            this.wait_info = i_wait_info;
            this.remark_info = i_remark_info;
            this.special_info = i_special_info;
            this.shift_cate = i_shift_cate;
            this.remarkExtra = remarkExtra;
            this.FRIDs = FRIDs;
        }
    }

    /// <summary> 備血 </summary>
    public class BloodInfo
    {
        public string Blood_NO { set; get; } //血型KEY
        public string Blood_Type { set; get; } //血型
        public DateTime Prepare_Date { set; get; } //備血日期
        public string Code_Desc { set; get; } //血種
        public int Ttl_Gty { set; get; } //備血量
        public int Unrece_Gty { set; get; } //餘血量
        public string rh_type { set; get; } //RH型
        public DateTime last_use_date { set; get; } //備血日期
        public string Blood_Name { set; get; } //血品名稱
        public string Reason { set; get; } //備血原因
    }

    #endregion

    #region 護理評估

    public enum NursingAssessment_Status
    {
        /// <summary> 臨時護理評估(維護中非正式版本) </summary>
        T = 0,
        /// <summary> 過時護理評估(不可維護，僅為記錄使用) </summary>
        O = 1,
        /// <summary> 正式版護理評估(線上使用中) </summary>
        C = 2
    }

    /// <summary>
    /// 護理評估項目分類清單
    /// </summary>
    public class NursingAssessmentItem
    {
        public string na_cate { set; get; }
        public string na_name { set; get; }
        public string na_type { set; get; }
        public int na_sort { set; get; }
    }

    /// <summary> 護理評估表單資訊 </summary>
    public class NursingAssessmentMain
    {
        /// <summary> 評估單序號 </summary>
        public string na_id { set; get; }

        /// <summary> 名稱 </summary>
        public string na_name { set; get; }

        /// <summary> 病委會序號 </summary>
        public string na_iso { set; get; }

        /// <summary> 評估單狀態 T:暫時 O:過時 C:正式 </summary>
        public NursingAssessment_Status na_status { set; get; }

        /// <summary> 版本 </summary>
        public string na_version { set; get; }

        /// <summary> 類型 </summary>
        public string na_type { set; get; }

        /// <summary> 描述 </summary>
        public string na_desc { set; get; }

    }

    /// <summary> 護理評估標籤 </summary>
    public class NursingAssessmentTag
    {
        /// <summary> 標籤序號 </summary>
        public string tag_id { set; get; }

        /// <summary> 標籤名稱 </summary>
        public string tag_name { set; get; }

        /// <summary> 標籤說明 </summary>
        public string tag_help { set; get; }

        /// <summary> 排序 </summary>
        public int tag_sort { set; get; }
    }

    /// <summary> 護理評估項目 </summary>
    public class NursingAssessmentDtl
    {
        /// <summary> 評估表ID </summary>
        public string na_id { set; get; }

        /// <summary> 標籤ID </summary>
        public string tag_id { set; get; }

        /// <summary> 細項編號 </summary>
        public string dtl_id { set; get; }

        /// <summary> 選單類別 {T:textbox R:radio S:select C:checkbox A:textarea D:datetime picker} </summary>
        public string dtl_type { set; get; }

        /// <summary> 細項抬頭 </summary>
        public string dtl_title { set; get; }

        /// <summary> 細項隱藏選項 </summary>
        public string dtl_child_hide { set; get; }

        /// <summary> 細項符合出現的條件 </summary>
        public string dtl_show_value { set; get; }

        /// <summary> 細項父皆ID </summary>
        public string dtl_parent_id { set; get; }

        /// <summary> 細項排序 </summary>
        public int dtl_sort { set; get; }

        /// <summary> 細項資料 </summary>
        public string dtl_value { set; get; }

        /// <summary> 細項預設值 </summary>
        public string dtl_default_value { set; get; }

        /// <summary> 限制長度  </summary>
        public int dtl_length { set; get; }

        /// <summary> 說明文字  </summary>
        public string dtl_help { set; get; }

        /// <summary> 後置文字  </summary>
        public string dtl_rear_word { set; get; }

        /// <summary> 是否必填  </summary>
        public string dtl_must { set; get; }
    }

    /// <summary> 檢視用評估細項物件 </summary>
    public class NA_View_Obj
    {

        /// <summary> 標籤ID </summary>
        public string tag_id { set; get; }

        /// <summary> 細項物件 </summary>
        public List<NursingAssessmentDtlObj> del_list { set; get; }
    }

    /// <summary> 細項物件 </summary>
    public class NursingAssessmentDtlObj
    {
        /// <summary> 評估表ID </summary>
        public string na_id { set; get; }

        /// <summary> 標籤ID </summary>
        public string tag_id { set; get; }

        /// <summary> 細項編號 </summary>
        public string dtl_id { set; get; }

        /// <summary> 選單類別 {T:textbox R:radio S:select C:checkbox A:textarea D:datetime picker} </summary>
        public string dtl_type { set; get; }

        /// <summary> 細項抬頭 </summary>
        public string dtl_title { set; get; }

        /// <summary> 細項隱藏選項 </summary>
        public string dtl_child_hide { set; get; }

        /// <summary> 細項符合出現的條件 </summary>
        public string dtl_show_value { set; get; }

        /// <summary> 子細項物件 </summary>
        public List<NursingAssessmentDtlObj> child_obj { set; get; }

        /// <summary> 細項父皆ID </summary>
        public string dtl_parent_id { set; get; }

        /// <summary> 細項排序 </summary>
        public int dtl_sort { set; get; }

        /// <summary> 細項資料 </summary>
        public string dtl_value { set; get; }

        /// <summary> 細項預設值 </summary>
        public string dtl_default_value { set; get; }

        /// <summary> 限制長度  </summary>
        public int dtl_length { set; get; }

        /// <summary> 說明文字  </summary>
        public string dtl_help { set; get; }

        /// <summary> 後置文字  </summary>
        public string dtl_rear_word { set; get; }

        /// <summary> 是否必填  </summary>
        public string dtl_must { set; get; }
    }

    /// <summary> 安寧評估物件 </summary>
    public class Na_Piece
    {
        public string fee_no { set; get; }
        public string field_name { set; get; }
        public string value { set; get; }
    }

    #endregion

    #region -- 點滴--
    [JsonObject(MemberSerialization.OptOut)]
    public class IVItem
    {
        /// <summary> 點滴名稱 </summary>
        [JsonProperty]
        public string name { set; get; }
        /// <summary> 點滴排序 </summary>
        [JsonProperty]
        public string sort { set; get; }
    }

    #endregion

    #region 檢驗、檢查、放射
    /// <summary> 檢驗 </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class Lab
    {
        /// <summary> 檢驗單號 </summary>
        [JsonProperty]
        public string LabNo { set; get; }

        /// <summary> 檢驗日期 </summary>
        [JsonProperty]
        public DateTime LabDate { set; get; }

        /// <summary> 開單日期 </summary>
        [JsonProperty]
        public DateTime OrderDate { set; get; }

        /// <summary> 組別 </summary>
        [JsonProperty]
        public string Group { set; get; }

        /// <summary> 檢體 </summary>
        [JsonProperty]
        public string Specimen { set; get; }

        /// <summary> 項目 </summary>
        [JsonProperty]
        public string ItemName { set; get; }

        /// <summary> 狀態 </summary>
        [JsonProperty]
        public string Status { set; get; }

        /// <summary> 備註 </summary>
        [JsonProperty]
        public string Memo { set; get; }

        /// <summary> 檢驗名稱 </summary>
        [JsonProperty]
        public string LabName { set; get; }

        /// <summary> 檢驗值 </summary>
        [JsonProperty]
        public string LabValue { set; get; }

        /// <summary> 檢驗代碼 </summary>
        [JsonProperty]
        public string LabCode { set; get; }

        /// <summary> 檢驗值單位 </summary>
        [JsonProperty]
        public string LabValueUnit { set; get; }

        /// <summary> 檢驗正常值低值 </summary>
        [JsonProperty]
        public string LVL { set; get; }

        /// <summary> 檢驗正常值高值 </summary>
        [JsonProperty]
        public string LVH { set; get; }

        /// <summary> 檢驗異常值註記 </summary>
        [JsonProperty]
        public string LabErrorFlag { set; get; }

        /// <summary> 檢驗報告網址 </summary>
        [JsonProperty]
        public string lab_page { set; get; }
    }

    /// <summary> 檢查 </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class Exam
    {
        /// <summary> 檢查單號 </summary>
        [JsonProperty]
        public String ExamNo { set; get; }

        /// <summary> 開單日期 </summary>
        [JsonProperty]
        public DateTime OrderDate { set; get; }

        /// <summary> 檢查日期 </summary>
        [JsonProperty]
        public DateTime ExamDate { set; get; }

        /// <summary> 檢查名稱 </summary>
        [JsonProperty]
        public string ExamName { set; get; }

        /// <summary> 檢查結果 </summary>
        [JsonProperty]
        public string ExamReport { set; get; }

        /// <summary> 檢查網址 </summary>
        [JsonProperty]
        public string ExamUrl { set; get; }

    }

    /// <summary> 會診 </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class Consultation
    {
        /// <summary> 開單單號 </summary>
        [JsonProperty]
        public string OrderNo { set; get; }

        /// <summary> 開單日期 </summary>
        [JsonProperty]
        public DateTime OrderDate { set; get; }

        /// <summary> 會診科別 </summary>
        [JsonProperty]
        public string ConsDept { set; get; }

        /// <summary> 會診醫師 </summary>
        [JsonProperty]
        public string ConsDoc { set; get; }

        /// <summary> 會診結果 </summary>
        [JsonProperty]
        public string ConsContent { set; get; }

        /// <summary> 會診主鑑 </summary>
        [JsonProperty]
        public string cons_key { set; get; }

        public string Cateory { set; get; }//會診類別
        public string NoteDept { set; get; }//照會科別
        public string NoteDoc { set; get; }//指定照會醫生
        public string Status { set; get; }//執行狀態
        public string EnterName { set; get; }//輸入者姓名
        public string ApplicationDoc { set; get; }//申請醫師
        public string Summery { set; get; }//概述
        public string ReplyTime { set; get; }//回覆時間
        /// <summary> 照會結果 0:會診、1:照會 </summary>
        [JsonProperty]
        public string ConsultFlag { set; get; }
    }
    /// <summary> 住院會診結果 </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class AdmConsultation
    {
        /// <summary> 申請時間 </summary>
        public string ApplicationTime { set; get; }
        /// <summary> 會診類別 </summary>
        public string Cateory { set; get; }
        /// <summary> 照會科別 </summary>
        public string NoteDept { set; get; }
        /// <summary> 指定照會醫生 </summary>
        public string NoteDoc { set; get; }
        /// <summary> 會診醫生 </summary>
        public string ConsultationDoc { set; get; }
        /// <summary> 執行狀態 </summary>
        public string Status { set; get; }
        /// <summary> 輸入者姓名 </summary>
        public string EnterName { set; get; }
        /// <summary> 申請醫師 </summary>
        public string ApplicationDoc { set; get; }
        /// <summary> 申請科別 </summary>
        public string ApplicationDept { set; get; }
        /// <summary> 概述</summary>
        public string Summery { set; get; }
        /// <summary> 回覆結果 </summary>
        public string Result { set; get; }
        /// <summary> 回覆時間 </summary>
        public string ReplyTime { set; get; }
        /// <summary> 照會結果 0:會診、1:照會 </summary>
        public string ConsultFlag { set; get; }
    }

    /// <summary>
    /// 化療各項檢驗
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class ChemotherapyStatus
    {
        /// <summary>
        /// 面積
        /// </summary>
        [JsonProperty]
        public string BSA { set; get; }

        /// <summary>
        /// 身高
        /// </summary>
        [JsonProperty]
        public string Height { set; get; }

        /// <summary>
        /// 體重
        /// </summary>
        [JsonProperty]
        public string Weight { set; get; }

        /// <summary>
        /// 血清肌酐
        /// </summary>
        [JsonProperty]
        public string Scr { set; get; }
        /// <summary>
        /// 肌酐清除率
        /// </summary>
        [JsonProperty]
        public string Ccr { set; get; }
        /// <summary>
        /// 白血球計數
        /// </summary>
        [JsonProperty]
        public string WBC { set; get; }
        /// <summary>
        /// 白血球計數檢驗日期
        /// </summary>
        [JsonProperty]
        public DateTime WBCDate { set; get; }
        /// <summary>
        /// TMN 分期
        /// (腫瘤)
        /// </summary>
        [JsonProperty]
        public string T { set; get; }
        /// <summary>
        /// TMN 分期
        /// (淋巴結)
        /// </summary>
        [JsonProperty]
        public string N { set; get; }
        /// <summary>
        /// TMN 分期
        /// (轉移)
        /// </summary>
        [JsonProperty]
        public string M { set; get; }
        /// <summary>
        /// 癌症期數
        /// </summary>
        [JsonProperty]
        public string Stage { set; get; }
        /// <summary>
        /// 絕對啫中性白血球
        /// </summary>
        [JsonProperty]
        public string ANC { set; get; }
    }
    #endregion

    #region 醫囑

    [JsonObject(MemberSerialization.OptOut)]
    public class IcdList
    {
        /// <summary>
        /// 診斷碼代碼
        /// </summary>
        [JsonProperty]
        public string icd_code { set; get; }

        /// <summary>
        /// 診斷碼中文說明
        /// </summary>
        [JsonProperty]
        public string icd_desc_cht { set; get; }

        /// <summary>
        /// 診斷碼英文說明
        /// </summary>
        [JsonProperty]
        public string icd_desc_eng { set; get; }
    }

    /// <summary> 抗生素列表 </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class AntibioticsList
    {
        /// <summary> 藥囑開立時間 </summary>
        [JsonProperty]
        public DateTime OrderStartDate { set; get; }

        /// <summary> 藥囑有效時間時間 </summary>
        [JsonProperty]
        public DateTime OrderEndDate { set; get; }

        /// <summary> 抗生素名稱 </summary>
        [JsonProperty]
        public string DrugName { set; get; }

        /// <summary> 劑量 </summary>
        [JsonProperty]
        public float Dose { set; get; }

        /// <summary> 劑量單位 </summary>
        [JsonProperty]
        public string DoseUnit { set; get; }

        /// <summary> QTY </summary>
        [JsonProperty]
        public string QTY { set; get; }

        /// <summary> 途徑 </summary>
        [JsonProperty]
        public string Route { set; get; }

        /// <summary> 頻次 </summary>
        [JsonProperty]
        public string Feq { set; get; }
    }

    /// <summary> 處置 </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class Procedure
    {
        /// <summary> 處置開立日期 </summary>
        [JsonProperty]
        public DateTime ProcedureDate { set; get; }

        /// <summary> 處置名稱 </summary>
        [JsonProperty]
        public string ProcedureName { set; get; }

        /// <summary> 處置代碼 </summary>
        [JsonProperty]
        public string ProcedureCode { set; get; }
    }

    /// <summary> 文字醫囑 </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class TextOrder
    {
        /// <summary> 醫囑編號</summary>
        [JsonProperty]
        public string SheetNo { set; get; }

        /// <summary> 醫囑囑型 (常規 R 需要時才給 S) </summary>
        [JsonProperty]
        public string Category { set; get; }

        /// <summary> 醫囑開立時間 </summary>
        [JsonProperty]
        public DateTime OrderStartDate { set; get; }

        /// <summary> 醫囑有效時間時間 </summary>
        [JsonProperty]
        public DateTime OrderEndDate { set; get; }

        /// <summary> 醫囑內容 </summary>
        [JsonProperty]
        public string Content { set; get; }

        /// <summary> DC_狀態 </summary>
        [JsonProperty]
        public string DC_FLAG { set; get; }
    }

    /// <summary> 一般藥囑 </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class UdOrder
    {
        /// <summary> 醫囑編號</summary>
        [JsonProperty]
        public string UD_SEQ { set; get; }

        /// <summary> 醫囑編號2</summary>
        [JsonProperty]
        public string UD_SEQ_OLD { set; get; }

        /// <summary> 醫囑囑型</summary>
        [JsonProperty]
        public string UD_TYPE { set; get; }

        /// <summary> 醫囑狀態</summary>
        [JsonProperty]
        public string UD_STATUS { set; get; }

        /// <summary> 批價序號</summary>
        [JsonProperty]
        public string FEE_NO { set; get; }

        /// <summary> 病歷號</summary>
        [JsonProperty]
        public string CHR_NO { set; get; }

        /// <summary> 藥品代碼</summary>
        [JsonProperty]
        public string MED_CODE { set; get; }

        /// <summary> 成本中心</summary>
        [JsonProperty]
        public string COST_CODE { set; get; }

        /// <summary> 床號</summary>
        [JsonProperty]
        public string BED_NO { set; get; }

        /// <summary> 病人姓名</summary>
        [JsonProperty]
        public string PAT_NAME { set; get; }

        /// <summary> 藥品商品名</summary>
        [JsonProperty]
        public string MED_DESC { set; get; }

        /// <summary> 藥品學名</summary>
        [JsonProperty]
        public string ALISE_DESC { set; get; }

        /// <summary> 醫囑劑量</summary>
        [JsonProperty]
        public string UD_DOSE { set; get; }

        /// <summary> 醫囑單位</summary>
        [JsonProperty]
        public string UD_UNIT { set; get; }

        /// <summary> 醫囑頻次</summary>
        [JsonProperty]
        public string UD_CIR { set; get; }

        /// <summary> 醫囑途徑</summary>
        [JsonProperty]
        public string UD_PATH { set; get; }

        /// <summary> 醫囑極量</summary>
        [JsonProperty]
        public string UD_LIMIT { set; get; }

        /// <summary> 醫囑數量</summary>
        [JsonProperty]
        public string UD_QTY { set; get; }

        /// <summary> 醫囑身份別</summary>
        [JsonProperty]
        public string PAY_FLAG { set; get; }

        /// <summary> 醫囑編號</summary>
        [JsonProperty]
        public string PROG_FLAG { set; get; }

        /// <summary> 醫囑開始日期</summary>
        [JsonProperty]
        public string BEGIN_DATE { set; get; }

        /// <summary> 醫囑開始時間</summary>
        [JsonProperty]
        public string BEGIN_TIME { set; get; }

        /// <summary> 舊-醫囑結束日期</summary>
        [JsonProperty]
        public string DC_DATE { set; get; }

        /// <summary> 舊-醫囑結束時間</summary>
        [JsonProperty]
        public string DC_TIME { set; get; }

        /// <summary> 醫囑結束日期</summary>
        [JsonProperty]
        public string END_DATE { set; get; }

        /// <summary> 醫囑結束時間</summary>
        [JsonProperty]
        public string END_TIME { set; get; }

        /// <summary> 醫囑備註</summary>
        [JsonProperty]
        public string UD_CMD { set; get; }

        /// <summary> 開立醫師</summary>
        [JsonProperty]
        public string DOC_CODE { set; get; }

        /// <summary> 發藥量</summary>
        [JsonProperty]
        public string SEND_AMT { set; get; }

        /// <summary> 退藥量</summary>
        [JsonProperty]
        public string BACK_AMT { set; get; }

        /// <summary> 費用日期</summary>
        [JsonProperty]
        public string FEE_DATE { set; get; }

        /// <summary> 費用時間</summary>
        [JsonProperty]
        public string FEE_TIME { set; get; }

        /// <summary> 發藥總量</summary>
        [JsonProperty]
        public string UD_DOSE_TOTAL { set; get; }

        /// <summary> 開始日期</summary>
        /// 
        [JsonProperty]
        public string BEGIN_DAY { set; get; }

        /// <summary> 結束日期</summary>
        [JsonProperty]
        public string DC_DAY { set; get; }

        /// <summary> 覆核</summary>
        [JsonProperty]
        public string DoubleCheck { set; get; }

        /// <summary> 每日給藥次數</summary>
        [JsonProperty]
        public string DAY_CNT { set; get; }

        /// <summary> 藥品分類</summary>
        [JsonProperty]
        public string DRUG_TYPE { set; get; }

        /// <summary> 流速</summary>
        [JsonProperty]
        public string FLOW_SPEED { set; get; }

        /// <summary> 部位</summary>
        [JsonProperty]
        public string POSITION { set; get; }

        /// <summary> 藥品圖片路徑</summary>
        [JsonProperty]
        public string DrugPicPath { set; get; }

        /// <summary> DC_FLAG</summary>
        [JsonProperty]
        public string DC_FLAG { set; get; }

        /// <summary> 易跌藥物</summary>
        [JsonProperty]
        public Boolean IsFRIDs { set; get; }

        /// <summary> 止痛藥物</summary>
        [JsonProperty]
        public Boolean IsAnalgesics { set; get; }
    }

    /// <summary> 一般藥囑 </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class DrugOrder
    {
        /// <summary> 藥囑編號</summary>
        [JsonProperty]
        public string SheetNo { set; get; }

        /// <summary> 藥囑囑型 (常規 R 需要時才給 S) </summary>
        [JsonProperty]
        public string Category { set; get; }

        /// <summary> 藥囑開立時間 </summary>
        [JsonProperty]
        public DateTime OrderStartDate { set; get; }

        /// <summary> 藥囑有效時間時間 </summary>
        [JsonProperty]
        public DateTime OrderEndDate { set; get; }

        /// <summary> 藥品商品名 </summary>
        [JsonProperty]
        public string DrugName { set; get; }

        /// <summary> 藥品學名 </summary>
        [JsonProperty]
        public string GenericDrugs { set; get; }

        /// <summary> 劑量 </summary>
        [JsonProperty]
        public string Dose { set; get; }

        /// <summary> 劑量單位 </summary>
        [JsonProperty]
        public string DoseUnit { set; get; }

        /// <summary> 途徑 </summary>
        [JsonProperty]
        public string Route { set; get; }

        /// <summary> 最低流速 </summary>
        [JsonProperty]
        public string RateL { set; get; }

        /// <summary> 最高流速 </summary>
        [JsonProperty]
        public string RateH { set; get; }

        /// <summary> 流速備註 </summary>
        [JsonProperty]
        public string RateMemo { set; get; }

        /// <summary> DC Flag </summary>
        [JsonProperty]
        public string DcFlag { set; get; }

        /// <summary> 給藥序號 </summary>
        [JsonProperty]
        public string GiveSerial { set; get; }

        /// <summary> 給藥日期 </summary>
        [JsonProperty]
        public string GiveDate { set; get; }

        /// <summary> 給藥時間 </summary>
        [JsonProperty]
        public string GiveTime { set; get; }

        /// <summary> 施打所需時間 </summary>
        [JsonProperty]
        public string CostTime { set; get; }

        /// <summary> 頻次 </summary>
        [JsonProperty]
        public string Feq { set; get; }

        /// <summary> 雙人複核確認 </summary>
        [JsonProperty]
        public string DoubleCheck { set; get; }

        /// <summary> 備註 </summary>
        [JsonProperty]
        public string Note { set; get; }

        /// <summary> 藥品代碼 </summary>
        [JsonProperty]
        public string Med_code { set; get; }

        /// <summary> 藥品狀態 </summary>
        [JsonProperty]
        public string Ud_status { set; get; }

        /// <summary> 流速 </summary>
        [JsonProperty]
        public string flow_speed { set; get; }

        /// <summary> DC_狀態 </summary>
        [JsonProperty]
        public string DC_FLAG { set; get; }

        /// <summary> 易跌藥物</summary>
        [JsonProperty]
        public Boolean IsFRIDs { set; get; }

        /// <summary> 止痛藥物</summary>
        [JsonProperty]
        public Boolean IsAnalgesics { set; get; }
    }

    /// <summary>
    /// 化療藥囑
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class ChemotherapyDrugOrder
    {
        /// <summary> 化療單號</summary>
        [JsonProperty]
        public string OrderNo { set; get; }

        /// <summary> 藥囑編號</summary>
        [JsonProperty]
        public string SheetNo { set; get; }

        /// <summary>
        /// 給藥順序
        /// </summary>
        [JsonProperty]
        public string Sequence { set; get; }
        /// <summary>
        /// 開立時間
        /// </summary>
        [JsonProperty]
        public DateTime OrderStartDate { set; get; }
        /// <summary>
        /// 有效時間時間
        /// </summary>
        [JsonProperty]
        public DateTime OrderEndDate { set; get; }
        /// <summary>
        /// 藥品商品名
        /// </summary>
        [JsonProperty]
        public string DrugName { set; get; }
        /// <summary>
        /// 藥品學名
        /// </summary>
        [JsonProperty]
        public string GenericDrugs { set; get; }
        /// <summary>
        /// 劑量
        /// </summary>
        [JsonProperty]
        public float Dose { set; get; }
        /// <summary>
        /// 劑量單位
        /// </summary>
        [JsonProperty]
        public string DoseUnit { set; get; }
        /// <summary>
        /// 途徑
        /// </summary>
        [JsonProperty]
        public string Route { set; get; }
        /// <summary>
        /// 頻次
        /// </summary>
        [JsonProperty]
        public string Feq { set; get; }
        /// <summary>
        /// 天數
        /// </summary>
        [JsonProperty]
        public string days { set; get; }
        /// <summary>
        /// 總量
        /// </summary>
        [JsonProperty]
        public string Total { set; get; }
        /// <summary>
        /// 速率
        /// </summary>
        [JsonProperty]
        public string Rate { set; get; }
        /// <summary>
        /// 醫囑備註
        /// </summary>
        [JsonProperty]
        public string Memo { set; get; }

        /// <summary> 藥品代碼 </summary>
        [JsonProperty]
        public string med_code { set; get; }

        /// <summary> 藥品狀態 </summary>
        [JsonProperty]
        public string ud_status { set; get; }

        /// <summary> 持續時間 </summary>
        [JsonProperty]
        public string dur_time { set; get; }

        /// <summary> 泡製溶液 </summary>
        [JsonProperty]
        public string InfusionSolution { set; get; }
    }

    /// <summary> 已簽醫囑 </summary>
    public class SignOrder
    {
        /// <summary> 住院序號 </summary>
        public string fee_no { set; get; }

        /// <summary> 醫囑序號 </summary>
        public string sheet_no { set; get; }

        /// <summary> 單位 </summary>
        public string unit { set; get; }

        /// <summary> 次輛 </summary>
        public string pre_qty { set; get; }

        /// <summary> 囑型 </summary>
        public string order_type { set; get; }

        /// <summary> 途徑 </summary>
        public string path_code { set; get; }

        /// <summary> 頻率 </summary>
        public string cir_code { set; get; }

        /// <summary> 醫囑開始時間 </summary>
        public DateTime start_date { set; get; }

        /// <summary> 醫囑結束時間 </summary>
        public DateTime end_date { set; get; }

        /// <summary> 醫囑內容 </summary>
        public string order_content { set; get; }

        /// <summary> 簽收時間 </summary>
        public DateTime sign_time { set; get; }

        /// <summary> 簽收人員員編 </summary>
        public string sign_user { set; get; }

        /// <summary> 簽收人員姓名 </summary>
        public string sign_user_name { set; get; }

        /// <summary> 紀錄類型{ T:文字 M:藥囑} </summary>
        public string sign_type { set; get; }

        /// <summary> 設定人員 </summary>
        public string set_user { set; get; }

        /// <summary> 設定時段 </summary>
        public string set_priod { set; get; }

        /// <summary> 設定動作 </summary>
        public string set_action { set; get; }

        /// <summary> 備註 </summary>
        public string memo { set; get; }

        /// <summary> 流速 </summary>
        public string rate_l { set; get; }
        public string rate_h { set; get; }

        /// <summary> 流速備註 </summary>
        public string rate_memo { set; get; }

        /// <summary> DC_狀態 </summary>
        public string DC_FLAG { set; get; }

        /// <summary> 記錄姓名 </summary>
        public string record_name { set; get; }

        /// <summary> 記錄者時間 </summary>
        public string record_time { set; get; }

        /// <summary> 易跌藥物</summary>
        [JsonProperty]
        public Boolean IsFRIDs { set; get; }

        /// <summary> 止痛藥物</summary>
        [JsonProperty]
        public Boolean IsAnalgesics { set; get; }

    }

    /// <summary> 手術資訊 </summary>
    public class Surgery
    {
        /// <summary> 手術日期 </summary>
        public DateTime SurgeryDate { set; get; }

        /// <summary> 手術序號 </summary>
        public string SurgeryNo { set; get; }

        /// <summary> 手術內容 </summary>
        public string SurgeryContent { set; get; }
    }

    /// <summary> 預計執行檢查/檢驗 </summary>
    public class GetExpectedItem
    {
        public DateTime UseDate { get; set; }
        public string PkNo { get; set; }
        public string ItemType { get; set; }
        public string ItemName { get; set; }
        public string ComplyDate { get; set; }
        public string Type { get; set; }
        public string LabNo { get; set; }
    }

    /// <summary>
    /// 藥品資訊
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class MedicalInfo
    {
        /// <summary> 藥品代碼 </summary>
        [JsonProperty]
        public string DrugCode { set; get; }

        /// <summary> 藥品商品名 </summary>
        [JsonProperty]
        public string DrugName { set; get; }

        /// <summary> 藥品學名 </summary>
        [JsonProperty]
        public string GenericDrugs { set; get; }

        /// <summary> 藥品途徑 </summary>
        [JsonProperty]
        public string DrugsPath { set; get; }

        /// <summary> 藥品作用 </summary>
        [JsonProperty]
        public string DrugEffects { set; get; }

        /// <summary> 藥品副作用 </summary>
        [JsonProperty]
        public string DrugSideEffects { set; get; }

        /// <summary> 藥品圖片路徑 </summary>
        [JsonProperty]
        public string DrugPicPath { set; get; }

        /// <summary> 藥品網頁路徑 </summary>
        [JsonProperty]
        public string DrugHref { set; get; }
    }

    //
    [JsonObject(MemberSerialization.OptOut)]
    public class MedOrderRenew
    {
        /// <summary> 藥品代碼 </summary>
        [JsonProperty]
        public string MED_CODE { set; get; }

        /// <summary> 更新後給藥序號 </summary>
        [JsonProperty]
        public string UDSEQ { set; get; }

        /// <summary> 更新後藥品劑量 </summary>
        [JsonProperty]
        public string UD_DOSE { set; get; }

        /// <summary> 更新後藥品劑量單位 </summary>
        [JsonProperty]
        public string UD_UNIT { set; get; }

        /// <summary> 更新後藥品頻次 </summary>
        [JsonProperty]
        public string UD_CIR { set; get; }

        /// <summary> 更新後藥品途徑 </summary>
        [JsonProperty]
        public string UD_PATH { set; get; }

        /// <summary> 更新後藥品囑型 </summary>
        [JsonProperty]
        public string UD_TYPE { set; get; }

        /// <summary> 更新後藥品開立日期 </summary>
        [JsonProperty]
        public string ORDERDATE { set; get; }

        /// <summary> 原始藥品序號 </summary>
        [JsonProperty]
        public string UDSEQ_O { set; get; }

        /// <summary> 原始藥品劑量 </summary>
        [JsonProperty]
        public string UD_DOSE_O { set; get; }

        /// <summary> 原始藥品劑量單位 </summary>
        [JsonProperty]
        public string UD_UNIT_O { set; get; }

        /// <summary> 原始藥品頻次 </summary>
        [JsonProperty]
        public string UD_CIR_O { set; get; }

        /// <summary> 原始藥品途徑 </summary>
        [JsonProperty]
        public string UD_PATH_O { set; get; }

        /// <summary> 原始藥品囑型 </summary>
        [JsonProperty]
        public string UD_TYPE_O { set; get; }
    }
    #endregion

    [JsonObject(MemberSerialization.OptOut)]
    public class DRUG_EXECUTE //TODO 找出這個為什麼其他的項目都有值只有INSULIN_SITE接不到DB的值
    {
        public string UD_SEQPK { get; set; } //簽用序號
        public string UD_SEQ { get; set; } //醫囑序號
        public string USE_DOSE { get; set; } //使用劑量
        public string DRUG_DATE { get; set; } //給藥日期時間CON
        public string EXEC_DATE { get; set; } //執行日期時間
        public string EXEC_ID { get; set; } //簽收者ID
        public string EXEC_NAME { get; set; } //簽收者姓名
        public string REASONTYPE { get; set; } //延遲(未執行)原因代碼
        public string REASON { get; set; } //延遲(未執行)原因
        public string STATION { get; set; } //應執行護理站
        public string CHECKER_DATE { get; set; } //覆核時間
        public string CHECKER_ID { get; set; } //覆核者ID
        public string CHECKER { get; set; } //覆核者姓名
        public string CREAT_DATE { get; set; } //建立日期
        public string INVALID_DATE { get; set; } //無效時間
        public string SIGNMODE { get; set; } //簽章方式
        public string RECORD_DATE { get; set; } //簽章時間
        public string RECORD_ID { get; set; } //簽章紀錄
        public string BADREACTION { get; set; } //不良反應
        public string REMARK { get; set; } //註記備註
        public string DESCRIPTION { get; set; } //醫囑備註
        public string MED_CODE { get; set; }
        public string FEE_NO { get; set; }
        public string ORDSTATUS { get; set; }
        public string DAY_CNT { get; set; }
        public string DRUG_TYPE { get; set; }
        public string UPD_TYPE { get; set; }
        public string UD_CIR { get; set; }
        public string UD_PATH { get; set; }
        public string UD_UNIT { get; set; }
        public string MED_DESC { get; set; }
        public string ALISE_DESC { get; set; }
        public string POSITION { get; set; }
        public string SS_DRUGNAME { get; set; }
        public string SS_DOSE { get; set; }
        public string FLOW_SPEED { get; set; }
        public string INSULIN_SITE { get; set; }
        public string DOSE_COUNT { get; set; }

    }

    public class BloodSugarInsulin_List
    {
        public string BLOODSUGAR { get; set; }
        public string INSOPNAME { get; set; }
        public string B_INDATE { get; set; }
        public string I_INDATE { get; set; }
        public string IN_DRUGNAME { get; set; }
        public string IN_DOSE { get; set; }
        public string IN_DOSEUNIT { get; set; }
        public string POSITION { get; set; }
        public string INJECTION { get; set; }
        public string SS_DRUGNAME { get; set; }
        public string SS_DOSE { get; set; }
        public string CHECK_FLAG { get; set; }
        public string MEAL_STATUS { get; set; }

    }


    /// <summary>
    /// 病人清單頁面使用
    /// </summary>
    public class PatientListPage
    {
        public PatientInfo ptinfo { set; get; }

        public List<SpecialNotes> spnote { set; get; }

        public PatientListPage(PatientInfo i_ptinfo, List<SpecialNotes> i_spnote)
        {
            this.ptinfo = i_ptinfo;
            this.spnote = i_spnote;
        }
    }

    /// <summary>
    /// 特殊註記
    /// </summary>
    public class SpecialNotes
    {
        public string DNR { set; get; }//拒
        public string Hospice { set; get; }//寧

        public string OrganDonation { set; get; }//捐

        public string Allergy { set; get; }//敏

        public string Security { set; get; }//密

        public string IcnDiseaseStr { set; get; }//隔

        public string fall_assess { set; get; }//跌

        public string pressure { set; get; }//壓

        public string pain { set; get; }//痛

        public string discharge { set; get; }//出

        public string Suicide { set; get; }//傷"

        public string Constraint { set; get; }//約

        public string Drug { set; get; }//給藥

    }
    /// <summary>
    /// 產檢資料
    /// </summary>
    public class BabyLab
    {
        public string FEE_NO { set; get; }//批價序號
        public string LabCode { set; get; }//檢驗項目代碼
        public string LabNameENG { set; get; }//英文名稱
        public string LabName { set; get; }//中文名稱
        public string LabValue { set; get; }//檢驗結果
        public string INSPTNo { set; get; }//檢驗院所代號
        public string INSPTName { set; get; }//檢驗院所名稱
        public DateTime INSPTDT { get; set; }//檢驗日期時間

    }

    /// <summary>
    /// 執行檢查輸入
    /// </summary>
    public class BaByPatInfo_IID
    {
        public string IID { set; get; }//身分證字號
        public string CHARNO { set; get; }//病歷號碼
        public string PATName { set; get; }//姓名
        public string PATBirthday { set; get; }//患者出生日期
        public string PATAddress { set; get; }//患者聯絡地址
        public string PATHomeNo { set; get; }//患者家裡電話

    }

    /// <summary>
    /// 患者資料（生產史產婦資料）
    /// </summary>
    public class BaByPatInfo_FeeNo
    {
        public string PATID { set; get; }//身份證字號
        public string Blood_Type { set; get; }//血型
        public string PATBirthPlace { set; get; }//患者出生地（聯絡地址區碼）
        public string PATBirthPlaceC { set; get; }//患者出生地（聯絡地址區名                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   ）
        public string PATAddress { set; get; }//患者聯絡地址(哺餵母乳轉介)
        public string PATHomeNo { set; get; }//患者家裡電話
        public string PATEducation { set; get; }//患者教育程度
        public string PATNational { set; get; }//患者國籍別
        public string PATPregnancy { set; get; }//患者高危險妊娠註記

    }

    /// <summary>
    /// 患者資料（生產史產婦配偶資料）
    /// </summary>
    public class BaByMatInfo
    {
        public string PATName { set; get; }//姓名
        public string PATBirthday { set; get; }//出生日期
        public string PATBirthPlace { set; get; }//出生地（聯絡地址區碼）
        public string PATBirthPlaceC { set; get; }//患者出生地（聯絡地址區名）

        public string PATNational { set; get; }//國籍別
        public string Blood_Type { set; get; }//血型
        public string PATEducation { set; get; }//教育程度
        public string PATHomeNo { set; get; }//家裡電話
        public string PATOcc { set; get; }//職業

    }
    /// <summary>
    /// 門診SDM
    /// </summary>
    public class SDMInfo
    {
        public string DATATime { set; get; }//資料時間
        public string SDM1 { set; get; }//親子同室方式
        public string SDM2 { set; get; }//餵食方式
    }
    /// <summary>
    /// 檢驗院所
    /// </summary>
    public class INSPTList
    {
        public string INSPTNo { set; get; }//院所代號
        public string INSPTName { set; get; }//院所名稱
    }
    /// <summary>
    /// 產婦新生兒對應
    /// </summary>
    public class Babylink
    {
        public string NB_FEENO { set; get; }//新生兒FEE_NO
        public string NB_CHARNO { set; get; }//新生兒CHAR_NO
        public string NB_SEQ { set; get; }//新生兒出生序
        public DateTime NB_BTDAY { get; set; }//新生兒出生日期

    }

    /// <summary>
    /// 產傷
    /// </summary>
    public class BirthTrauma
    {
        public int NUMBER_OF_BT { set; get; }//產傷兒數
        public Boolean Steroid { set; get; }//類固醇施打
    }
    /// <summary>
    /// 新生兒第二個Fee_No
    /// </summary>
    public class BabyFeeNo2
    {
        public string NB_FEENO { set; get; }//新生兒FEE_NO
    }
    /// <summary>
    /// 取得新生兒產傷發生率資料
    /// </summary>
    public class TraumaRate
    {
        public int NO_OF_TRAUMA { set; get; }//新生兒產傷人數
        public int NO_OF_LH { set; get; }//新生兒出院人數
    }
}

/// <summary> 頻次表 </summary>
[JsonObject(MemberSerialization.OptOut)]
public class Fre
{
    /// <summary> 頻次代碼 </summary>
    [JsonProperty]
    public string FreCode { set; get; }

    /// <summary> 頻次名稱 </summary>
    [JsonProperty]
    public string FreName { set; get; }

    /// <summary> 頻次時間點 </summary>
    [JsonProperty]
    public string FreTime { set; get; }
}

/// <summary> 一般藥囑 </summary>
[JsonObject(MemberSerialization.OptOut)]
public class IVOeder
{
    /// <summary> 醫囑編號</summary>
    [JsonProperty]
    public string UD_SEQ { set; get; }

    /// <summary> 醫囑編號2</summary>
    [JsonProperty]
    public string UD_SEQ_OLD { set; get; }

    /// <summary> 醫囑囑型</summary>
    [JsonProperty]
    public string UD_TYPE { set; get; }

    /// <summary> 醫囑狀態</summary>
    [JsonProperty]
    public string UD_STATUS { set; get; }

    /// <summary> 批價序號</summary>
    [JsonProperty]
    public string FEE_NO { set; get; }

    /// <summary> 病歷號</summary>
    [JsonProperty]
    public string CHR_NO { set; get; }

    /// <summary> 藥品代碼</summary>
    [JsonProperty]
    public string MED_CODE { set; get; }

    /// <summary> 成本中心</summary>
    [JsonProperty]
    public string COST_CODE { set; get; }

    /// <summary> 床號</summary>
    [JsonProperty]
    public string BED_NO { set; get; }

    /// <summary> 病人姓名</summary>
    [JsonProperty]
    public string PAT_NAME { set; get; }

    /// <summary> 藥品商品名</summary>
    [JsonProperty]
    public string MED_DESC { set; get; }

    /// <summary> 藥品學名</summary>
    [JsonProperty]
    public string ALISE_DESC { set; get; }

    /// <summary> 醫囑劑量</summary>
    [JsonProperty]
    public string UD_DOSE { set; get; }

    /// <summary> 醫囑單位</summary>
    [JsonProperty]
    public string UD_UNIT { set; get; }

    /// <summary> 醫囑頻次</summary>
    [JsonProperty]
    public string UD_CIR { set; get; }

    /// <summary> 醫囑途徑</summary>
    [JsonProperty]
    public string UD_PATH { set; get; }

    /// <summary> 醫囑極量</summary>
    [JsonProperty]
    public string UD_LIMIT { set; get; }

    /// <summary> 醫囑數量</summary>
    [JsonProperty]
    public string UD_QTY { set; get; }

    /// <summary> 醫囑身份別</summary>
    [JsonProperty]
    public string PAY_FLAG { set; get; }

    /// <summary> 醫囑編號</summary>
    [JsonProperty]
    public string PROG_FLAG { set; get; }

    /// <summary> 醫囑開始日期</summary>
    [JsonProperty]
    public string BEGIN_DATE { set; get; }

    /// <summary> 醫囑開始時間</summary>
    [JsonProperty]
    public string BEGIN_TIME { set; get; }

    /// <summary> 醫囑結束日期</summary>
    [JsonProperty]
    public string END_DATE { set; get; }

    /// <summary> 醫囑結束時間</summary>
    [JsonProperty]
    public string END_TIME { set; get; }

    /// <summary> 醫囑備註</summary>
    [JsonProperty]
    public string UD_CMD { set; get; }

    /// <summary> 開立醫師</summary>
    [JsonProperty]
    public string DOC_CODE { set; get; }

    /// <summary> 發藥量</summary>
    [JsonProperty]
    public string SEND_AMT { set; get; }

    /// <summary> 退藥量</summary>
    [JsonProperty]
    public string BACK_AMT { set; get; }

    /// <summary> 費用日期</summary>
    [JsonProperty]
    public string FEE_DATE { set; get; }

    /// <summary> 費用時間</summary>
    [JsonProperty]
    public string FEE_TIME { set; get; }

    /// <summary> 發藥總量</summary>
    [JsonProperty]
    public string UD_DOSE_TOTAL { set; get; }

    /// <summary> 開始日期</summary>
    /// 
    [JsonProperty]
    public string BEGIN_DAY { set; get; }

    /// <summary> 結束日期</summary>
    [JsonProperty]
    public string DC_DAY { set; get; }

    /// <summary> 覆核</summary>
    [JsonProperty]
    public string DoubleCheck { set; get; }

    /// <summary> 每日給藥次數</summary>
    [JsonProperty]
    public string DAY_CNT { set; get; }

    /// <summary> 藥品分類</summary>
    [JsonProperty]
    public string DRUG_TYPE { set; get; }

    /// <summary> 流速</summary>
    [JsonProperty]
    public string FLOW_SPEED { set; get; }

    /// <summary> 部位</summary>
    [JsonProperty]
    public string POSITION { set; get; }

    /// <summary> 藥品圖片路徑</summary>
    [JsonProperty]
    public string DrugPicPath { set; get; }
}

/// <summary>
/// 生命徵象拋轉
/// </summary>
public class VitalSignbyDate
{
    public string INSID { set; get; }//機器名稱
    public string SYSTOLIC { set; get; }//收縮壓
    public string DIATOLIC { set; get; }//舒張壓
    public string MAP { set; get; }//壓差
    public string HR { set; get; }//心跳
    public string TEMP { set; get; }//體溫
    public string SPO2 { set; get; }//血氧
    public string SPO2HR { set; get; }//血氧心跳
    public string HEIGHT { set; get; }//身高
    public string WEIGHT { set; get; }//體重
    public string RESP { set; get; }//呼吸
    public string PAIN { set; get; }//疼痛
    public string PATIENICIANID { set; get; }//病歷號
    public string CLINICIANID { set; get; }//操作員
    public string MDATE { set; get; }//日期時間(yyyy/MM/dd HH:mm:ss)
}

/// <summary>
/// 文字給藥醫囑是否有未簽收判斷
/// </summary>
public class OrderItem
{
    public string R { set; get; }//;長期醫囑
    public string S3 { set; get; }//臨時三天內醫囑
    public string S { set; get; }//全部臨時
    public string DC { set; get; }//刪除的醫囑
    public string D { set; get; }//出院醫囑
    public string ALL { set; get; }//全部醫囑


}

#region 取得TPR抗生素
public class TPRAantibiotic
{
    public DateTime UseDate { set; get; }//日期
    public string content { set; get; }//內容

}
#endregion

#region 取得TPR總膽紅素血球容積%
public class TPRTBilHCT
{
    public DateTime UseDate { set; get; }//日期
    public string T_Bilirubin { set; get; }//總膽紅素
    public string HCT { set; get; }//血球容積比
}

#endregion

#region 取得交班單預計項目數值
public class ExpectedItem
{
    /// <summary> 醫囑日期 </summary>
    public DateTime UseDate { set; get; }
    /// <summary> 醫囑序號(需唯一值pk ) </summary>
    public string PkNo { set; get; }
    /// <summary> 項目類別(檢查【exam】、檢驗【lab】、手術【op】、抽血【blood】) </summary>
    public string ItemType { set; get; }
    /// <summary> 項目名稱(手術名稱) </summary>
    public string ItemName { set; get; }
    /// <summary> 檢查時間(手術時間) </summary>
    public string ComplyDate { set; get; }
    /// <summary> 檢體類別(檢驗檢查使用的)(麻醉類別) </summary>
    public string Type { set; get; }
    /// <summary> 檢查單號 </summary>
    public string LabNo { set; get; }
}
#endregion

#region 急診
public class ERConsultation
{
    public string ApplicationTime { set; get; }//申請時間
    public string Cateory { set; get; }//會診類別
    public string NoteDept { set; get; }//照會科別
    public string NoteDoc { set; get; }//指定照會醫生
    public string ConsultationDoc { set; get; }//會診醫生
    public string Status { set; get; }//執行狀態
    public string EnterName { set; get; }//輸入者姓名
    public string ApplicationDoc { set; get; }//申請醫師
    public string ApplicationDept { set; get; }//申請科別
    public string Summery { set; get; }//概述
    public string Result { set; get; }//回覆結果
    public string ReplyTime { set; get; }//回覆時間
}

public class TextOrderRecord
{
    public string Content { set; get; }//醫囑紀錄客觀資料
}

public class ERList
{
    public DateTime RegTime { set; get; }//檢傷時間
    public string ChartNo { set; get; }//病歷號
    public string PatientName { set; get; }//病人姓名
    public string Doc { set; get; }//主治醫師
    public string Level { set; get; }//檢傷級數
    public string BedNo { set; get; }//床號
    public string DBack { set; get; }//二返
    public string CT { set; get; }//CT
    public string XRay { set; get; }//XRay
    public string Bio { set; get; }//生化
    public string ME { set; get; }//鏡檢
    public string Blood { set; get; }//血液
    public string EKG { set; get; }//EKG
    public string Note { set; get; }//特殊註記
    public string FeeNo { set; get; }//批價序號
    public string DeptNo { set; get; }//科別代碼
    public string Status { set; get; }//是否需變色
}

public class ERDeptList
{
    public string DeptName { set; get; }
    public string DeptNo { set; get; }
}

public class TriageInfo
{
    public string FeeNo { set; get; }//批價序號
    public string Level { set; get; }//檢傷級數
    public string DeptNo { set; get; }//科別代碼
    public string DeptName { set; get; }//科別名稱
    public string ChartNo { set; get; }//病歷號
    public string PT_NAME { set; get; }//患者姓名
    public string ID_NO { set; get; }//身份證字號
    public string SEX { set; get; }//患者性別
    public string BIRTH_DATE { set; get; }//患者生日
    public string TEL_NO { set; get; }//患者電話
    public string ADMIT_DATE { set; get; }//檢傷日期
    public string ADMIT_TIME { set; get; }//檢傷時間
    public string OHCA_FLAG { set; get; }//到院前死亡
    public string DOMESTIC_VIOLENCE_FLAG { set; get; }//家暴
    public string SEXUAL_ABUSE_FLAG { set; get; }//性侵
    public string SUICIDE_FLAG { set; get; }//自殺
    public string DANGER_FLAG { set; get; }//病危
    public string TRAUMA_FLAG { set; get; }//外傷
    public string IN_HOSP_TYPE { set; get; }//到院方式
    public string ESCORTS_PERSON { set; get; }//陪同人員
    public string GCS_E { set; get; }//GCS_E
    public string GCS_V { set; get; }//GCS_V
    public string GCS_M { set; get; }//GCS_M
    public string GCS_Total { set; get; }//GCS 總分
    public string VAS { set; get; }//疼痛評估分數
    public string PUPIL_SIZE_LEFT { set; get; }//Pupil Size Left
    public string PUPIL_SIZE_RIGHT { set; get; }//Pupil Size Right
    public string LIGHT_REACTION_LEFT { set; get; }//瞳孔對光反應(左)
    public string LIGHT_REACTION_RIGHT { set; get; }//瞳孔對光反應(右)
    public string SKIN_SIGN { set; get; }//皮膚徵象
    public string SKIN_UNUSUAL_SPOT { set; get; }//皮膚異常部位
    public string ALLERGY_FLAG { set; get; }//過敏史
    public string ALLERGY_MED_FOOD { set; get; }//過敏藥食物
    public string ALLERGY_OTHER { set; get; }//過敏史其他
    public string BLOOD_TYPE { set; get; }//血型
    public string VITAL_SIGNS_FLAG { set; get; }//量測方式
    public string BP_SYSTOLIC { set; get; }//收縮壓
    public string BP_DIASTOLIC { set; get; }//舒張壓
    public string BP_REMARK { set; get; }//血壓備註
    public string TEMPERATURE { set; get; }//體溫
    public string TEMPERATURE_NOTE { set; get; }//體溫備註
    public string PULSE { set; get; }//脈搏
    public string RESPIRE { set; get; }//呼吸次數
    public string BODY_WEIGHT { set; get; }//體重
    public string VITAL_SIGNS_NOTE { set; get; }//生命徵象備註
    public string HIS_CONTACT_FLAG { set; get; }//接觸史
    public string HIS_TRAVEL_FLAG { set; get; }//旅遊史
    public string HIS_GATHER_FLAG { set; get; }//群聚史
    public string HIS_OCCUPATION_FLAG { set; get; }//職業史
    public string BREATH_CONDITION { set; get; }//呼吸型態
    public string SUBJECT { set; get; }//主訴
    public string ER_WOUND { set; get; }//傷口
    public string HIS_MEDICAL { set; get; }//過去病史
    public string SPO2 { set; get; }//SPO2
    public string MEASURING_TIME { set; get; }//量測時間
    public string INJURY_FLAG { set; get; }//受傷機轉
    public string TRIAGE_NURSE { set; get; }//檢傷護士
    public string CONFRIM_NURSE { set; get; }//檢傷護士
}
#endregion

#region VoiceCommand
/// <summary>
/// viewModel
/// </summary>
public class VoiceCommand
{
    public string PK_ID { get; set; }
    public DateTime CREATE_TIME { get; set; }
    public string Action_Type { get; set; }
    public string Controller { get; set; }
    public string UserId { get; set; }
    public string Fee_no { get; set; }
    public DateTime? Start_Datetime { get; set; }
    public DateTime? End_Datetime { get; set; }
    public string Cellular_Phone { get; set; }
    public string STATUS { get; set; }
    public string Parameter_CMD { get; set; }
    public string Parameter { get; set; }
    public string VOICE_PATH { get; set; }
}
#endregion
#region VoiceCommand
/// <summary>
/// viewModel
/// </summary>
public class VoiceMemoParameter
{
    public string Content { get; set; }
    public string VoiceType { get; set; }
}
#endregion
#region VoiceToTaskList
public class VoiceToTaskList
{
    /// <summary> 完成動作人員</summary>
    [JsonProperty]
    public string UserId { set; get; }
    /// <summary> 完成時間點</summary>
    [JsonProperty]
    public string Exec_Priod { get; set; }
    /// <summary> 動作</summary>
    [JsonProperty]
    public string Set_Action { get; set; }
    /// <summary> 病患批價號</summary>
    [JsonProperty]
    public string Fee_no { get; set; }
    /// <summary> 執行時間</summary>
    [JsonProperty]
    public string CREATE_DATETIME { get; set; }
    /// <summary> 執行結果</summary>
    [JsonProperty]
    public string Exec_Result { get; set; }
    /// <summary> 未執行原因</summary>
    [JsonProperty]
    public string Exec_Reason { get; set; }
}
#endregion
#region SetVoiceToPhrase
public class VoicePhrase
{
    /// <summary> 片語主題</summary>
    [JsonProperty]
    public string Phrase_Name { set; get; }
    /// <summary> 片語C</summary>
    [JsonProperty]
    public string C_str { get; set; }
    /// <summary> 片語S</summary>
    [JsonProperty]
    public string S_str { get; set; }
    /// <summary> 片語O</summary>
    [JsonProperty]
    public string O_str { get; set; }
    /// <summary> 片語I</summary>
    [JsonProperty]
    public string I_str { get; set; }
    /// <summary> 片語E</summary>
    [JsonProperty]
    public string E_str { get; set; }
}
#endregion