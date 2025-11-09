using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.OleDb;
using System.Web.Services;
using System.Xml;
using Newtonsoft.Json;
using Com.Mayaminer;
using System.Diagnostics;
using NIS.UtilTool;
using NIS.Data;
using System.Configuration;

namespace NIS.WebService
{
    public class NISAgent
    {
        private OleDbConnection DBConnect;
        private OleDbCommand SQLCmd;
        private string m_connStr;

        public NISAgent()
        {
            this.m_connStr = System.Configuration.ConfigurationManager.AppSettings["WebServiceConnectionString"].ToString();
            System.Environment.SetEnvironmentVariable("NLS_LANG", "TRADITIONAL CHINESE_TAIWAN.ZHT16MSWIN950");
            this.DBConnect = new OleDbConnection(m_connStr);
            this.SQLCmd = new OleDbCommand();
            this.SQLCmd.Connection = this.DBConnect;
        }

        public void OpenDB()
        {
            if (this.DBConnect.State == System.Data.ConnectionState.Closed)
                this.DBConnect.Open();
        }

        public void CloseDB()
        {
            if (this.DBConnect.State == System.Data.ConnectionState.Open)
                this.DBConnect.Close();
        }

        public DataTable GetDataTable(string sql)
        {
            this.SQLCmd.CommandText = sql;
            OleDbDataAdapter adapter = new OleDbDataAdapter(sql, m_connStr);

            DataSet ds = new DataSet();

            adapter.Fill(ds, "table");
            return ds.Tables["table"];
        }

        public int Execute(string sql)
        {
            //OleDbConnection conn = new OleDbConnection(m_connStr);

            OpenDB();
            OleDbCommand sqlCmd = new OleDbCommand(sql, this.DBConnect);
            sqlCmd.CommandTimeout = 30;
            int effRow = sqlCmd.ExecuteNonQuery();

            CloseDB();

            return effRow;

        }

        public int ExeSqlTransaction(string sqls)
        {
            int ret = 0;
            OleDbConnection conn = new OleDbConnection(m_connStr);
            OleDbCommand command = new OleDbCommand();
            OleDbTransaction transaction;

            conn.Open();
            transaction = conn.BeginTransaction();
            command.Connection = conn;
            command.Transaction = transaction;
            try
            {
                string[] sql = sqls.Split(';');
                foreach (string s in sql)
                {
                    if (s.Trim() == "") continue;

                    command.CommandText = s;
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
                ret = 1;
            }
            catch (Exception)
            {
                transaction.Rollback();
                ret = 0;
            }

            command.Connection.Close();
            return ret;
        }

        public void BeginTransaction()
        {
            OleDbConnection conn = new OleDbConnection(m_connStr);
            SQLCmd = new OleDbCommand();
            OleDbTransaction transaction;

            conn.Open();
            transaction = conn.BeginTransaction();
            SQLCmd.Connection = conn;
            SQLCmd.Transaction = transaction;
        }

        public void ExecSqlTransaction(string sql)
        {
            SQLCmd.CommandText = sql;
            SQLCmd.ExecuteNonQuery();
        }

        public void Commit()
        {
            SQLCmd.Transaction.Commit();
            SQLCmd.Connection.Close();
            SQLCmd.Connection.Dispose();
        }

        public void Rollback()
        {
            SQLCmd.Transaction.Rollback();
            SQLCmd.Connection.Close();
            SQLCmd.Connection.Dispose();
        }
    }


    /// <summary>
    ///Interface 的摘要描述
    /// </summary>
    [WebService(Namespace = "http://NIS/WebService")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // 若要允許使用 ASP.NET AJAX 從指令碼呼叫此 Web 服務，請取消註解下列一行。
    // [System.Web.Script.Services.ScriptService]
    public class Interface : System.Web.Services.WebService
    {
        NISAgent m_dbAgency = new NISAgent();
        private LogTool log = new LogTool();
        private Nis nis;

        private XmlDocument createSuccessXmlDoc(string count)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement element = doc.CreateElement("EMR");
            doc.AppendChild(element);
            xmlAddChild(element, "Count", count);
            return doc;
        }

        private XmlDocument createMergePrintResultSuccessXmlDoc()
        {
            XmlDocument doc = new XmlDocument();
            XmlElement element = doc.CreateElement("EMR");
            doc.AppendChild(element);
            return doc;
        }

        private XmlElement xmlAddChild(XmlElement parent, String name, String value)
        {
            XmlElement element = parent.OwnerDocument.CreateElement(name);
            element.InnerText = value;
            parent.AppendChild(element);
            return element;
        }

        private XmlElement xmlAddChild(XmlElement parent, String name)
        {
            XmlElement element = parent.OwnerDocument.CreateElement(name);
            parent.AppendChild(element);
            return element;
        }

        private static void __outputEMRListXML(XmlElement pXmlNode, DataTable pDT)
        {
            if (pDT != null && pDT.Rows.Count > 0)
            {
                XmlElement _listNode = (XmlElement)pXmlNode.AppendChild(pXmlNode.OwnerDocument.CreateElement("List"));
                XmlElement _itemNode = null;
                foreach (DataRow _r in pDT.Rows)
                {
                    _itemNode = (XmlElement)_listNode.AppendChild(pXmlNode.OwnerDocument.CreateElement("OrderNo"));
                    _itemNode.InnerText = _r["OrderNo"].ToString();
                }
            }
        }

        private static void __outputMergePrintResultXML(XmlElement pXmlNode, DataTable pDT)
        {
            if (pDT != null && pDT.Rows.Count > 0)
            {
                XmlElement _listNode = (XmlElement)pXmlNode.AppendChild(pXmlNode.OwnerDocument.CreateElement("List"));
                XmlElement _itemNode = null;                                             

                foreach (DataRow _r in pDT.Rows)
                {
                    _itemNode = (XmlElement)_listNode.AppendChild(pXmlNode.OwnerDocument.CreateElement("EMPNO"));
                    _itemNode.InnerText = _r["MODIFYUSER"].ToString();
                    _itemNode = (XmlElement)_listNode.AppendChild(pXmlNode.OwnerDocument.CreateElement("OrderNoList"));
                    _itemNode.InnerText = _r["OrderNo"].ToString();
                }
            }
        }

        private string CreateEMRxml(string count, DataTable DT)
        {
            XmlDocument _doc = createSuccessXmlDoc(count);
            //__outputEMRListXML(_doc.DocumentElement, DT);
            return _doc.InnerXml;
        }

        private string CreateMergePrintResultxml(DataTable DT)
        {
            XmlDocument _doc = createMergePrintResultSuccessXmlDoc();
            __outputMergePrintResultXML(_doc.DocumentElement, DT);
            return _doc.InnerXml;
        }

        [WebMethod]
        public string Insert_VitalSign(string empno, string ChartNo, string vstype, string vs_record, string createtime)
        {
            //取最近一次住院序號
            this.nis = new Nis();
            string feeno = string.Empty, type = "a";
            empno = empno.Trim();
            byte[] inHistoryByte = this.nis.GetInHistory(ChartNo);
            if (inHistoryByte != null)
            {
                string inHistoryJson = NIS.UtilTool.CompressTool.DecompressString(inHistoryByte);
                List<NIS.Data.InHistory> inHistoryList = JsonConvert.DeserializeObject<List<NIS.Data.InHistory>>(inHistoryJson);
                inHistoryList.Sort((x, y) => { return -x.indate.CompareTo(y.indate); });
                if (inHistoryList.Count > 0)
                {
                    feeno = inHistoryList[0].FeeNo.ToString().Trim();
                    //取得病人年齡
                    byte[] ptinfoByteCode = this.nis.GetPatientInfo(feeno);
                    if (ptinfoByteCode != null)
                    {
                        string ptinfoJosnArr = NIS.UtilTool.CompressTool.DecompressString(ptinfoByteCode);
                        NIS.Data.PatientInfo pi = JsonConvert.DeserializeObject<NIS.Data.PatientInfo>(ptinfoJosnArr);
                        if (pi.Age < 19)
                            type = "y";
                        if (pi.Age < 12)
                            type = "c";
                        if (pi.Age < 5)
                        {
                            type = "t";
                            DateTime Birthday = pi.Birthday;
                            int totalMonth = DateTime.Now.Year * 12 + DateTime.Now.Month - Birthday.Year * 12 - Birthday.Month;
                            if (totalMonth < 18)
                                type = "b";
                            if (totalMonth < 1)
                                type = "n";
                        }
                    }
                }
            }

            string returnmessage = string.Empty;
            if (feeno != "")
            {
                if (vstype == "bs")
                {
                    string vs_id = feeno + DateTime.Now.AddYears(-1911).Year.ToString() + DateTime.Now.ToString("MMddHHmm");
                    string sql = "INSERT INTO BLOODSUGAR (BSID,FEENO,INDATE,BLOODSUGAR,INSDT,INSOP,INSOPNAME,status) Values (";
                    sql += "'" + vs_id + "', ";
                    sql += "'" + feeno + "', ";
                    sql += "'" + createtime + "', ";
                    sql += "'#" + vs_record + "', ";
                    sql += "'" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + "', ";
                    sql += "'" + empno + "', ";
                    sql += "'系統拋轉',";
                    sql += "'SYSTEM') ";

                    if (m_dbAgency.Execute(sql) > 0)
                        returnmessage = "BLOODSUGAR Insert Success.";
                    else
                        returnmessage = "BLOODSUGAR Insert Fail.";
                }
                else
                {
                    createtime = Convert.ToDateTime(createtime).ToString("yyyy/MM/dd HH:mm:00");
                    string vs_id = feeno + "_" + Convert.ToDateTime(createtime).ToString("yyyyMMddHHmm");
                    string sql = "INSERT INTO DATA_VITALSIGN (fee_no, vs_id, vs_item, vs_record,create_user,create_date,modify_date) Values (";
                    sql += "'" + feeno + "', ";
                    sql += "'" + vs_id + "', ";
                    sql += "'" + vstype + "', ";
                    sql += "'#" + vs_record + "', ";
                    sql += "'" + ((empno == "") ? "SYSTEM" : empno) + "', ";
                    sql += "to_date('" + createtime.Trim() + "','YYYY/MM/DD HH24:MI:ss'),";
                    sql += "to_date('" + createtime.Trim() + "','YYYY/MM/DD HH24:MI:ss'))";

                    if (m_dbAgency.Execute(sql) > 0)
                    {
                        returnmessage = "Vital Sign Insert Success.";
                        if (empno != "")
                        {
                            string content_o = "";
                            if (vstype == "bt" && Check_Num_Abnormal("btl_e", "bth_e", vs_record) == "Y")
                                content_o = "耳溫：" + vs_record + " ℃";
                            else if (vstype == "mp" && Check_Num_Abnormal("mpl_" + type, "mph_" + type, vs_record) == "Y")
                                content_o = "心跳：" + vs_record + " 次/分";
                            else if (vstype == "bf" && Check_Num_Abnormal("bfl_" + type, "bfh_" + type, vs_record) == "Y")
                                content_o = "呼吸：" + vs_record + " 次/分";
                            else if (vstype == "bp")
                            {
                                if (Check_Num_Abnormal("bpls_" + type, "bphs_" + type, vs_record.Split('|').GetValue(0).ToString()) == "Y" || Check_Num_Abnormal("bpld_" + type, "bphd_" + type, vs_record.Split('|').GetValue(1).ToString()) == "Y")
                                    content_o = "血壓：" + vs_record.Replace("|", " / ") + " mmHg";
                            }
                            else if (vstype == "ab")
                            {
                                if (Check_Num_Abnormal("abls", "abhs", vs_record.Split('|').GetValue(0).ToString()) == "Y" || Check_Num_Abnormal("abld", "abhd", vs_record.Split('|').GetValue(1).ToString()) == "Y")
                                    content_o = "動脈血壓：" + vs_record.Replace("|", " / ") + " mmHg";
                            }
                            else if (vstype == "pa")
                            {
                                if (Check_Num_Abnormal("pals", "pahs", vs_record.Split('|').GetValue(0).ToString()) == "Y" || Check_Num_Abnormal("pald", "pahd", vs_record.Split('|').GetValue(1).ToString()) == "Y")
                                    content_o = "肺動脈壓：" + vs_record.Replace("|", " / ") + " mmHg";
                            }
                            else if (vstype == "sp" && Check_Num_Abnormal("spl_" + type, "", vs_record) == "Y")
                                content_o = "血氧：" + vs_record + " %";
                            else if (vstype == "co" && Check_Num_Abnormal("col", "coh", vs_record) == "Y")
                                content_o = "心輸出量：" + vs_record + " L/min";
                            else if (vstype == "ci" && Check_Num_Abnormal("cil", "cih", vs_record) == "Y")
                                content_o = "心輸出量指數：" + vs_record + " L/min/m2";
                            else if (vstype == "sv" && Check_Num_Abnormal("svl", "svh", vs_record) == "Y")
                                content_o = "混合靜脈血氧飽合度：" + vs_record + " %";
                            else if (vstype == "cp" && Check_Num_Abnormal("cpl", "", vs_record) == "Y")
                                content_o = "腦灌流壓：" + vs_record + " mmHg";

                            if (content_o != "")
                                Insert_CareRecord(createtime, vs_id, content_o, vstype, empno, feeno);
                        }
                    }
                    else
                        returnmessage = "Vital Sign Insert Fail.";
                }
            }
            else
                returnmessage = "Vital Sign Insert Fail.";
            return returnmessage;
        }

        [WebMethod]
        public Boolean Upd_Signature_Status(string TemplateId, string OrderNo, string Status, out string ErrMsg)
        {
            Boolean success = false;
            string sql = string.Empty;
            ErrMsg = string.Empty;
            if (TemplateId == "039")
            {
                //sql = "update ASSESSMENTMASTER set signstatus ='" + Status + "' where FEENO = substr('" + OrderNo + "',1,8) and FEENO || MODIFYUSER ='" + OrderNo + "' ";
                sql = "update ASSESSMENTMASTER set signstatus ='" + Status + "' where substr(FEENO,1,8) = substr('" + OrderNo + "',1,8) ";
                //string sql_emrms = "update NIS_EMRMS set STATUS = 'S' whewre ORDER_NO = '" + OrderNo + "'";                
                //int result_emrms = m_dbAgency.Execute(sql_emrms);
                int result = m_dbAgency.Execute(sql);
                if (result > 0 )
                    success = true;
                else
                {
                    ErrMsg = sql;
                    this.log.saveLogMsg(sql, "EMRUpdate");
                }
            }
            else if (TemplateId == "014")
            {
                sql = "update CARERECORD_DATA set signstatus ='" + Status + "' where CARERECORD_ID || SELF = '" + OrderNo + "'";
                //string sql_emrms = "update NIS_EMRMS set STATUS = 'S' whewre ORDER_NO = '" + OrderNo + "'";
                //int result_emrms = m_dbAgency.Execute(sql_emrms);
                int result = m_dbAgency.Execute(sql);
                if (result > 0 )
                    success = true;
                else
                {
                    ErrMsg = sql;
                    this.log.saveLogMsg(sql, "EMRUpdate");
                }
            }
            else if (TemplateId == "067")
            {
                sql = "update DRUG_EXECUTE set signstatus ='" + Status + "' where UD_SEQPK ='" + OrderNo + "'";
                //string sql_emrms = "update NIS_EMRMS set STATUS = 'S' whewre ORDER_NO = '" + OrderNo + "'";
                //int result_emrms = m_dbAgency.Execute(sql_emrms);
                int result = m_dbAgency.Execute(sql);
                if (result > 0 )
                    success = true;
                else
                {
                    ErrMsg = sql;
                    this.log.saveLogMsg(sql, "EMRUpdate");
                }
            }

            return success;
        }

        [WebMethod]
        public string Merge_Print_Result(string StartDate, string EndDate, string chartno, string TemplateId)
        {
            string returnmessage = string.Empty;
            string sql = string.Empty;
            string StartDay = ((DateTime)Convert.ToDateTime(StartDate)).ToString("yyyy/MM/dd");
            string EndDay = ((DateTime)Convert.ToDateTime(EndDate)).ToString("yyyy/MM/dd");
            string feeno = string.Empty;

            //取最近一次住院序號
            this.nis = new Nis();
            byte[] inHistoryByte = this.nis.GetInHistory(chartno);
            if (inHistoryByte != null)
            {
                string inHistoryJson = NIS.UtilTool.CompressTool.DecompressString(inHistoryByte);
                List<NIS.Data.InHistory> inHistoryList = JsonConvert.DeserializeObject<List<NIS.Data.InHistory>>(inHistoryJson);
                inHistoryList.Sort((x, y) => { return -x.indate.CompareTo(y.indate); });
                if (inHistoryList.Count > 0)
                {
                    feeno = inHistoryList[0].FeeNo.ToString();
                }
            }

            if (TemplateId == "039")
            {
                #region Group_CONCAT 但 OLEDB 不支援
                sql = "with data as (";
                sql += "select distinct MODIFYUSER, FEENO || MODIFYUSER OrderNo ,";
                sql += " row_number() over (partition by MODIFYUSER order by FEENO || MODIFYUSER) rn, ";
                sql += " count(*) over (partition by MODIFYUSER) cnt ";
                sql += " from ASSESSMENTMASTER where substr(feeno,0,8) = substr('" + feeno + "',0,8) AND ";
                sql += " (to_date(modifytime) < to_date('" + EndDay + " 23:59:59','YYYY/MM/DD HH24:MI:ss') ";
                sql += " and to_date(modifytime) > to_date('" + StartDay + " 00:00:00','YYYY/MM/DD HH24:MI:ss'))";
                sql += " AND SIGN <> 'Y' and VER is null and SIGNTIME is null";
                sql += ") ";
                sql += " select MODIFYUSER, ltrim(sys_connect_by_path(OrderNo,','),',') ORDERNO";
                sql += " from data where rn = cnt start with rn = 1 ";
                sql += " connect by prior MODIFYUSER = MODIFYUSER ";
                sql += " and prior rn = rn-1 ";
                sql += " order by MODIFYUSER";
                #endregion

                sql = "select distinct MODIFYUSER, FEENO || MODIFYUSER OrderNo ";
                sql += " from ASSESSMENTMASTER where substr(feeno,0,8) = substr('" + feeno + "',0,8) AND ";
                sql += " (to_date(modifytime) < to_date('" + EndDay + " 23:59:59','YYYY/MM/DD HH24:MI:ss') ";
                sql += " and to_date(modifytime) > to_date('" + StartDay + " 00:00:00','YYYY/MM/DD HH24:MI:ss'))";
                sql += " AND SIGN <> 'Y' and VER is null and SIGNTIME is null";
               

                DataTable DT = m_dbAgency.GetDataTable(sql);
                if (DT.Rows.Count > 0)
                {
                    returnmessage = CreateMergePrintResultxml(DT);
                }
                else
                    returnmessage = "No Data Find.";
            }
            else if (TemplateId == "014")
            {
                sql = "select GUIDE_NO as MODIFYUSER, ";
                sql += " RTRIM (XMLAGG (XMLELEMENT (GUIDE_NO, carerecord_id || self || ',')).EXTRACT ('//text()'),',') ORDERNO";
                sql += " from CARERECORD_DATA ";
                sql += " where substr(feeno,0,8) = substr('" + feeno + "',0,8) AND ";
                sql += " (to_date(creattime) < to_date('" + EndDay + " 23:59:59','YYYY/MM/DD HH24:MI:ss') ";
                sql += " and to_date(creattime) > to_date('" + StartDay + " 00:00:00','YYYY/MM/DD HH24:MI:ss'))";
                sql += " AND DELETED is null ";
                sql += " AND SIGN <> 'Y' and VER is null ";
                sql += " GROUP BY GUIDE_NO ";

                DataTable DT = m_dbAgency.GetDataTable(sql);
                if (DT.Rows.Count > 0)
                {
                    returnmessage = CreateMergePrintResultxml(DT);
                }
                else
                    returnmessage = "No Data Find.";
            }
            else if (TemplateId == "067")
            {
                sql = "select exec_id as MODIFYUSER, ";
                sql += " RTRIM (XMLAGG (XMLELEMENT (exec_id, UD_SEQPK || ',')).EXTRACT ('//text()'),',') ORDERNO";
                sql += " from DRUG_EXECUTE where substr(fee_no,0,8) = substr('" + feeno + "',0,8) AND ";
                sql += " (to_date(exec_date) < to_date('" + EndDay + " 23:59:59','YYYY/MM/DD HH24:MI:ss') ";
                sql += " and to_date(exec_date) > to_date('" + StartDay + " 00:00:00','YYYY/MM/DD HH24:MI:ss'))";
                sql += " AND (RECORD_ID <> 'Y' OR RECORD_ID IS NULL) ";
                sql += " AND EXEC_DATE IS NOT NULL AND INVALID_DATE IS NULL ";
                sql += " GROUP BY exec_id ";

                DataTable DT = m_dbAgency.GetDataTable(sql);
                if (DT.Rows.Count > 0)
                {
                    returnmessage = CreateMergePrintResultxml(DT);
                }
                else
                    returnmessage = "No Data Find.";
            }

            return returnmessage;
        }
        
        [WebMethod]
        public string Wait_Signature_EMPResult(string StartDate, string EndDate, string empno, string TemplateId)
        {
            string returnmessage = string.Empty;
            string sql = string.Empty;
            string StartDay = ((DateTime)Convert.ToDateTime(StartDate)).ToString("yyyy/MM/dd");
            string EndDay = ((DateTime)Convert.ToDateTime(EndDate)).ToString("yyyy/MM/dd");

            if (TemplateId == "039")
            {
                sql = "select TABLEID as OrderNo from ASSESSMENTMASTER where substr(MODIFYUSER,0,8) = trim('" + empno + "',0,8) AND ";
                sql += " (to_date(modifytime) < to_date('" + EndDay + " 23:59:59','YYYY/MM/DD HH24:MI:ss') ";
                sql += " and to_date(modifytime) > to_date('" + StartDay + " 00:00:00','YYYY/MM/DD HH24:MI:ss'))";
                sql += " AND SIGN <> 'Y' and VER is null and SIGNTIME is null";

                DataTable DT = m_dbAgency.GetDataTable(sql);
                if (DT.Rows.Count > 0)
                {
                    returnmessage = CreateEMRxml(DT.Rows.Count.ToString(), DT);
                }
                else
                    returnmessage = "No Data Find.";
            }
            else if (TemplateId == "014")
            {
                sql = "select CARERECORD_ID as OrderNo from CARERECORD_DATA where substr(GUIDE_NO,0,8) = substr('" + empno + "',0,8) AND ";
                sql += " (to_date(creattime) < to_date('" + EndDay + " 23:59:59','YYYY/MM/DD HH24:MI:ss') ";
                sql += " and to_date(creattime) > to_date('" + StartDay + " 00:00:00','YYYY/MM/DD HH24:MI:ss'))";
                sql += " AND DELETED is null ";
                sql += " AND SIGN <> 'Y' and VER is null and SIGNTIME is null";

                DataTable DT = m_dbAgency.GetDataTable(sql);
                if (DT.Rows.Count > 0)
                {
                    returnmessage = CreateEMRxml(DT.Rows.Count.ToString(), DT);
                }
                else
                    returnmessage = "No Data Find.";
            }
            else if (TemplateId == "067")
            {
                sql = "select UD_SEQPK as OrderNo from DRUG_EXECUTE where substr(exec_id,0,8) = substr('" + empno + "',0,8) AND ";
                sql += " (to_date(exec_date) < to_date('" + EndDay + " 23:59:59','YYYY/MM/DD HH24:MI:ss') ";
                sql += " and to_date(exec_date) > to_date('" + StartDay + " 00:00:00','YYYY/MM/DD HH24:MI:ss'))";
                sql += " AND (RECORD_ID <> 'Y' OR RECORD_ID IS NULL) ";
                sql += " AND EXEC_DATE IS NOT NULL AND INVALID_DATE IS NULL ";

                DataTable DT = m_dbAgency.GetDataTable(sql);
                if (DT.Rows.Count > 0)
                {
                    returnmessage = CreateEMRxml(DT.Rows.Count.ToString(), DT);
                }
                else
                    returnmessage = "No Data Find.";
            }

            return returnmessage;
        }

        [WebMethod]
        public string Wait_Signature_TemplateResult(string StartDate, string EndDate, string TemplateId)
        {
            string returnmessage = string.Empty;
            string sql = string.Empty;
            string StartDay = ((DateTime)Convert.ToDateTime(StartDate)).ToString("yyyy/MM/dd");
            string EndDay = ((DateTime)Convert.ToDateTime(EndDate)).ToString("yyyy/MM/dd");

            if (TemplateId == "039")
            {
                sql = "select TABLEID as OrderNo from ASSESSMENTMASTER where ";
                sql += " (to_date(modifytime) < to_date('" + EndDay + " 23:59:59','YYYY/MM/DD HH24:MI:ss') ";
                sql += " and to_date(modifytime) > to_date('" + StartDay + " 00:00:00','YYYY/MM/DD HH24:MI:ss'))";
                sql += " AND SIGN <> 'Y' and VER is null and SIGNTIME is null";

                DataTable DT = m_dbAgency.GetDataTable(sql);
                if (DT.Rows.Count > 0)
                {
                    returnmessage = CreateEMRxml(DT.Rows.Count.ToString(), DT);
                }
                else
                    returnmessage = "No Data Find.";
            }
            else if (TemplateId == "014")
            {
                sql = "select CARERECORD_ID as OrderNo from CARERECORD_DATA where";
                sql += " (to_date(creattime) < to_date('" + EndDay + " 23:59:59','YYYY/MM/DD HH24:MI:ss') ";
                sql += " and to_date(creattime) > to_date('" + StartDay + " 00:00:00','YYYY/MM/DD HH24:MI:ss'))";
                sql += " AND DELETED is null ";
                sql += " AND SIGN <> 'Y' and VER is null and SIGNTIME is null";

                DataTable DT = m_dbAgency.GetDataTable(sql);
                if (DT.Rows.Count > 0)
                {
                    returnmessage = CreateEMRxml(DT.Rows.Count.ToString(), DT);
                }
                else
                    returnmessage = "No Data Find.";
            }
            else if (TemplateId == "067")
            {
                sql = "select UD_SEQPK as OrderNo from DRUG_EXECUTE where ";
                sql += " (to_date(exec_date) < to_date('" + EndDay + " 23:59:59','YYYY/MM/DD HH24:MI:ss') ";
                sql += " and to_date(exec_date) > to_date('" + StartDay + " 00:00:00','YYYY/MM/DD HH24:MI:ss'))";
                sql += " AND (RECORD_ID <> 'Y' OR RECORD_ID IS NULL) ";
                sql += " AND EXEC_DATE IS NOT NULL AND INVALID_DATE IS NULL ";

                DataTable DT = m_dbAgency.GetDataTable(sql);
                if (DT.Rows.Count > 0)
                {
                    returnmessage = CreateEMRxml(DT.Rows.Count.ToString(), DT);
                }
                else
                    returnmessage = "No Data Find.";
            }

            return returnmessage;
        }

        //VitalSign_儲存時檢查異常值 
        private string Check_Num_Abnormal(string lfn, string hfn, string cvalue)
        {
            string must = "";
            string sqlstr = string.Empty;
            sqlstr = " SELECT * FROM NIS_SYS_VITALSIGN_OPTION WHERE ";
            sqlstr += " MODEL_ID IN ('" + lfn + "','" + hfn + "') ";

            DataTable dt = m_dbAgency.GetDataTable(sqlstr);

            foreach (DataRow r in dt.Rows)
            {
                if (r["DECIDE"].ToString() == ">")
                {
                    if (double.Parse(cvalue.Replace("#", "")) > double.Parse(r["value_limit"].ToString()))
                        must = "Y";
                }
                if (r["DECIDE"].ToString() == "<")
                {
                    if (double.Parse(cvalue.Replace("#", "")) < double.Parse(r["value_limit"].ToString()))
                        must = "Y";
                }
            }

            return must;
        }

        private int Insert_CareRecord(string time, string id, string O, string self, string userno, string feeno)
        {
            int erow = 0;
            string sign_userno = sel_guide_userno(userno, Convert.ToDateTime(time), Convert.ToDateTime(time).Hour);
            byte[] listByteCode = nis.UserName(userno);
            //20140721 新增輸入人員員編判斷
            if (listByteCode != null)
            {
                string listJsonArray = NIS.UtilTool.CompressTool.DecompressString(listByteCode);
                NIS.Data.UserInfo user_name = JsonConvert.DeserializeObject<NIS.Data.UserInfo>(listJsonArray);

                string sql = "INSERT INTO CARERECORD_DATA(CARERECORD_ID, CREATNO, GUIDE_NO, CREATNAME, CREATTIME, RECORDTIME, FEENO, O_OTHER, SELF, SIGN) Values (";
                sql += "'" + id + "',";
                sql += "'" + userno + "',";
                sql += "'" + sign_userno + "',";
                sql += "'" + ((user_name.EmployeesName == null) ? "" : user_name.EmployeesName) + "',";
                sql += "to_date('" + DateTime.Now.ToString("yyyy/MM/dd HH:mm") + "','YYYY/MM/DD HH24:MI:ss'),";
                sql += "to_date('" + time + "','YYYY/MM/DD HH24:MI:ss'),";
                sql += "'" + feeno + "',";
                sql += "'" + O + "',";
                sql += "'" + self + "',";
                sql += "'N')";

                if (m_dbAgency.Execute(sql) > 0)
                {
                    //將紀錄回寫至 EMR Temp Table
                    try
                    {
                        ////string sqlstr = "begin P_NIS_EMRMS('" + feeno + "','014','護理紀錄單','" + id + self + "','" + time + "','" + sign_userno + "','I');end;";
                       ////m_dbAgency.Execute(sqlstr);
                        #region JAG 簽章
                        // 20150608 EMR
                        string strPath = @"C:\\wkhtmltopdf\\wkhtmltopdf.exe";
                        string filename = @"C:\inetpub\NIS\Images\" + id + self + ".pdf";

                        string port = System.Web.HttpContext.Current.Request.ServerVariables["SERVER_PORT"];
                        if (port == null || port == "80" || port == "443")
                            port = "";
                        else
                            port = ":" + port;

                        string protocol = System.Web.HttpContext.Current.Request.ServerVariables["SERVER_PORT_SECURE"];
                        if (protocol == null || protocol == "0")
                            protocol = "http://";
                        else
                            protocol = "https://";

                        string sOut = protocol + System.Web.HttpContext.Current.Request.ServerVariables["SERVER_NAME"] + port + System.Web.HttpContext.Current.Request.ApplicationPath;

                        if (sOut.EndsWith("/"))
                        {
                            sOut = sOut.Substring(0, sOut.Length - 1);
                        }

                        PatientInfo pinfo = new PatientInfo();
                        byte[] ByteCode = nis.GetPatientInfo(feeno);
                        //病人資訊
                        if (ByteCode != null)
                            pinfo = JsonConvert.DeserializeObject<PatientInfo>(CompressTool.DecompressString(ByteCode));

                        string url = sOut + "/CareRecord/List_PDF?id=" + id + self + "&feeno=" + pinfo.FeeNo;
                        Process p = new Process();
                        p.StartInfo.FileName = strPath;
                        p.StartInfo.Arguments = url + " " + filename;
                        p.StartInfo.UseShellExecute = true;
                        p.Start();
                        p.WaitForExit();                        

                        string emp_id = (user_name.UserID != null) ? user_name.UserID.Trim() : "";
                        string emp_name = (user_name.EmployeesName != null) ? user_name.EmployeesName.Trim() : "";
                        string dep_no = pinfo.DeptNo;
                        string chr_no = pinfo.ChartNo;
                        string pat_name = pinfo.PatientName;
                        string in_date = pinfo.InDate.ToString("yyyyMMdd");
                        string chagre_type = (pinfo.PayInfo == "健保") ? "001" : "000";
                        int result = emr_sign(id + self, pinfo.FeeNo, "014", userno, emp_name, emp_id, dep_no, chr_no, pat_name, in_date, chagre_type, filename);
                        #endregion

                    }
                    catch (Exception)
                    {

                    }
                }
            }
            return erow;
        }

        public int emr_sign(string pkey, string fee_no, string doc_type, string emp_no, string emp_name, string emp_id, string dep_no, string chr_no, string pat_name, string in_date, string charge_type, string filename)
        {
            try
            {
                JagEMRReference.Service1 jag = new JagEMRReference.Service1();
                jag.Url = ConfigurationManager.AppSettings["JagEMRReference"].ToString();

                string orderstr = string.Empty;
                string base64str = string.Empty;

                orderstr = get_xml_str(emp_no, emp_name, emp_id, dep_no, pkey, doc_type, fee_no, chr_no, pat_name, in_date, charge_type);

                byte[] pdfbyte = System.IO.File.ReadAllBytes(@"" + filename);
                base64str = Convert.ToBase64String(pdfbyte);
                int result = jag.UploadEMRFile(orderstr, base64str);
                return result;

            }
            catch (Exception)
            {
                return 4;
            }

        }

        public string get_xml_str(string emp_no, string emp_name, string emp_id, string dep_no, string pkey, string doc_type, string fee_no, string chr_no, string pat_name, string in_date, string charge_type)
        {
            string xml = string.Empty;
            xml += "<RequestDate>" + DateTime.Now.ToString("yyyyMMdd") + "</RequestDate>";
            xml += "<RequestTime>" + DateTime.Now.ToString("HHmmss") + "</RequestTime>";
            xml += "<RequestUser>" + emp_no + "</RequestUser>";
            xml += "<RequestUserName>" + emp_name + "</RequestUserName>";
            xml += "<UserIDNO>" + emp_id + "</UserIDNO>";
            xml += "<RequestDivision>" + dep_no.Trim() + "</RequestDivision>";
            xml += "<FileName>" + pkey + ".pdf</FileName>";
            xml += "<SignSystem>NIS</SignSystem>";
            xml += "<RequestDocType>" + doc_type + "</RequestDocType>";
            xml += "<RequestDocDate>" + DateTime.Now.ToString("yyyyMMdd") + "</RequestDocDate>";
            xml += "<RequestDocTime>" + DateTime.Now.ToString("HHmmss") + "</RequestDocTime>";
            xml += "<RequestDocRoot>" + fee_no + "</RequestDocRoot>";
            xml += "<RequestDocParent>" + fee_no + "</RequestDocParent>";
            xml += "<RequestDocNo>" + pkey + "</RequestDocNo>";
            xml += "<RequestPatientID>" + chr_no + "</RequestPatientID>";
            xml += "<RequestPatinetName>" + pat_name + "</RequestPatinetName>";
            xml += "<VisitDate>" + in_date + "</VisitDate>";
            xml += "<Category>I</Category>";
            xml += "<DocCharge>" + charge_type + "</DocCharge>";
            xml += "<InHospital>1</InHospital>";
            xml += "<DischargeDate>        </DischargeDate>";

            //xml += "<RequestDate>20100303</RequestDate>";
            //xml += "<RequestTime>075614</RequestTime>";
            //xml += "<RequestUser>A1001</RequestUser>";
            //xml += "<RequestUserName>林小明醫師</RequestUserName>";
            //xml += "<UserIDNO>B001790309</UserIDNO>";
            //xml += "<RequestDivision>11</RequestDivision>";
            //xml += "<FileName>IN201003031533001234.pdf</FileName>";
            //xml += "<SignSystem>IPD</SignSystem>";
            //xml += "<RequestDocType>18842-5</RequestDocType>";
            //xml += "<RequestDocDate>20100303</RequestDocDate>";
            //xml += "<RequestDocTime>082838</RequestDocTime>";
            //xml += "<RequestDocRoot>IN1206120034</RequestDocRoot>";
            //xml += "<RequestDocParent>IN1206120034</RequestDocParent>";
            //xml += "<RequestDocNo>IN1206120034L18842501</RequestDocNo>";
            //xml += "<RequestPatientID>0012345</RequestPatientID>";
            //xml += "<RequestPatinetName>王明雄</RequestPatinetName>";
            //xml += "<VisitDate>20100302</VisitDate>";
            //xml += "<Category>I</Category>";
            //xml += "<DocCharge>000</DocCharge>";
            //xml += "<InHospital>1</InHospital>";
            //xml += "<DischargeDate>20100303</DischargeDate>";

            return xml;
        }

        /// <summary>
        /// 搜尋指導者
        /// </summary>
        /// <param name="feeno">搜尋的feeno</param>
        /// <param name="date">搜尋的日期</param>
        /// <param name="shit_cate">搜尋的班別</param>
        public string sel_guide_userno(string feeno, DateTime date, int cate = 99)
        {
            DataTable dt = new DataTable();
            string name = feeno;
            string shit_cate = "";
            if (cate != 99)
            {
                if (cate < 8)
                    shit_cate = "N";
                else if (cate < 16)
                    shit_cate = "D";
                else if (cate < 24)
                    shit_cate = "E";
            }

            string sql = "SELECT GUIDE_USER FROM DATA_DISPATCHING WHERE 0 = 0 ";

            if (feeno != "")
                sql += "AND RESPONSIBLE_USER = '" + feeno + "' ";
            if (date != null)
                sql += "AND SHIFT_DATE = TO_DATE('" + date.ToString("yyyy/MM/dd") + "','yyyy/MM/dd') ";
            if (shit_cate != "")
                sql += "AND SHIFT_CATE = '" + shit_cate + "' ";

            sql += "AND GUIDE_USER IS NOT NULL";

            dt = m_dbAgency.GetDataTable(sql);

            if (dt.Rows.Count > 0)
                name = dt.Rows[0]["GUIDE_USER"].ToString();

            return name;
        }

    }
}
