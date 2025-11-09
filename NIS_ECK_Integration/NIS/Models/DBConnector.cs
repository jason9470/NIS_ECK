using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Web;
using NIS.Models;
using System.Data;
using Com.Mayaminer;
using Oracle.ManagedDataAccess.Client;
using System.Web.Mvc;
using NIS.Controllers;
using Oracle.ManagedDataAccess.Types;

namespace NIS.Models
{


    /// <summary>
    /// 放入DB的資料集
    /// </summary>
    public class DBItem
    {
        public enum DBDataType
        {
            /// <summary>文字</summary>
            String = 1,
            /// <summary>時間 格式：yyyy/MM/dd hh24:mi:ss</summary>
            DataTime = 2,
            /// <summary>數值(包括小數)</summary>
            Number = 3,

            /// <summary>二進制資料</summary>
            BLOB = 4
        }

        public string Field { set; get; }
        public string Value { set; get; }
        public DBDataType DataType { set; get; }

        /// <summary>
        /// 資料集
        /// </summary>
        /// <param name="Field">欄位名稱</param>
        /// <param name="Value">值</param>
        /// <param name="DataType">資料型態</param>
        public DBItem(string Field, string Value, DBDataType DataType)
        {
            this.Field = Field;
            this.Value = Value;
            this.DataType = DataType;
        }
    }

    public class PainItem
    {
        public string Name { set; get; }
        public string Value { set; get; }
        /// <summary>
        /// 資料集
        /// </summary>
        /// <param name="Field">欄位名稱</param>
        /// <param name="Value">值</param>
        /// <param name="DataType">資料型態</param>
        public PainItem(string Name, string Value)
        {
            this.Name = Name;
            this.Value = Value;
        }
    }

    public class DBConnector : DBConnector2
    {

    }
    public class DBConnector1
    {
        private OleDbConnection DBLink;
        public OleDbCommand DBCmd;
        private LogTool log;
        private OleDbTransaction DBTnstion;
        private bool IsTransaction;

        public static string base_string = IniFile.GetConnStr();
        public static string no_pool = base_string + "Pooling=false";
        public static string with_pool = base_string + "Pooling=true";
        public static string with_cache = with_pool + "; Statement Cache Size=1";

        /// <summary>
        /// 建構式
        /// </summary>
        public DBConnector1()
        {
            this.log = new LogTool();
            try
            {
                this.DBLink = new OleDbConnection(IniFile.GetConnStr());
                this.DBCmd = new OleDbCommand();
                this.DBCmd.Connection = this.DBLink;
                this.IsTransaction = false;
            }
            catch (Exception ex)
            {
                this.log.saveLogMsg(ex.Message.ToString(), "DBConnector_V1");
            }
        }

        /// <summary> 開啟DB </summary>
        /// <param name="OpenTnstion">是否啟動 Transaction</param>
        public void DBOpen(bool OpenTnstion = false)
        {
            try
            {
                if (this.DBLink.State == System.Data.ConnectionState.Closed)
                {
                    this.DBLink.Open();
                }

                if (OpenTnstion)
                {
                    this.IsTransaction = true;
                    this.DBTnstion = this.DBLink.BeginTransaction();
                    this.DBCmd.Transaction = this.DBTnstion;
                }
                else if (DBTnstion != null)
                {
                    this.IsTransaction = false;
                    this.DBTnstion.Dispose();
                    this.DBTnstion = null;
                }

            }
            catch (Exception ex)
            {
                this.log.saveLogMsg(ex.Message.ToString(), "DBOpen");
            }
        }

        /// <summary>
        /// 關閉連線
        /// </summary>
        public void DBClose()
        {
            try
            {
                if (this.DBLink.State == System.Data.ConnectionState.Open)
                {
                    this.DBLink.Close();
                    OleDbConnection.ReleaseObjectPool();
                }

            }
            catch (Exception ex)
            {
                this.log.saveLogMsg(ex.Message.ToString(), "DBClosed");
            }
        }

        /// <summary> 確認交易 </summary>
        public void DBCommit()
        {
            try
            {
                DBOpen();
                if (IsTransaction)
                {
                    this.IsTransaction = false;
                    this.DBTnstion.Commit();
                    this.DBTnstion.Dispose();
                    this.DBTnstion = null;
                }
            }
            catch (Exception ex)
            {
                DBTnstion.Rollback();
                DBTnstion.Dispose();
                DBTnstion = null;
                this.log.saveLogMsg(ex.Message.ToString(), "DBCommit");
                throw new DBCommitException("DBCommit Fail");
            }
            finally
            {
                this.DBClose();
            }
        }

        #region 自訂Exception: DBCommitException

        [Serializable]
        class DBCommitException : Exception
        {
            public DBCommitException()
            {
            }

            public DBCommitException(string Msg)
                : base(String.Format("DB Commit Exception: {0}", Msg))
            {
            }
        }

        #endregion

        /// <summary> 取消交易 </summary>
        public void DBRollBack()
        {
            try
            {
                DBOpen();
                if (IsTransaction)
                {
                    this.IsTransaction = false;
                    this.DBTnstion.Rollback();
                    this.DBTnstion.Dispose();
                    this.DBTnstion = null;
                }
            }
            catch (Exception ex)
            {
                this.log.saveLogMsg(ex.Message.ToString(), "DBRollBack");
            }
            finally
            {
                this.DBClose();
            }
        }

        /// <summary>
        /// 執行SQL取得Boolean
        /// </summary>
        /// <param name="SqlStatment">要執行的查詢語法</param>
        /// <param name="DataTable">資料讀取器的位置(call by reference)</param>
        /// <returns>取得Boolean</returns>
        public Boolean DBExecSQL(string SqlStatment, bool saveSqlFile = false)
        {
            try
            {
                DataTable DataTable = new DataTable();
                DBOpen();
                this.DBCmd.CommandText = SqlStatment.Replace("°Ｃ", "℃").Replace("°C", "℃").Replace("°", "℃");
                OleDbDataReader DataReader = null;
                DataReader = this.DBCmd.ExecuteReader();

                DataTable.Load(DataReader);
                DataReader.Close();
                DataReader.Dispose();
                return true;
            }
            catch (Exception Ex)
            {
                this.log.saveLogMsg(SqlStatment, "DBExecSQL");
                this.log.saveLogMsg(Ex.Message.ToString(), "DBExecSQL");
                return false;
            }
            finally
            {
                DBClose();
            }
        }

        /// <summary>
        /// 執行SQL取得DATAREADER
        /// </summary>
        /// <param name="sqlStatment">要執行的查詢語法</param>
        /// <param name="dataReader">資料讀取器的位置(call by reference)</param>
        /// <param name="saveSqlFile">發生錯誤時是否要順便紀錄SQL</param>
        /// <returns>是否取得成功</returns>
        //public bool DBExecSQL(string sqlStatment, ref OleDbDataReader dataReader, bool saveSqlFile = false)
        //{
        //    try
        //    {
        //        if (this.DBLink.State == System.Data.ConnectionState.Open)
        //        {
        //            this.DBCmd.CommandText = sqlStatment;
        //            dataReader = this.DBCmd.ExecuteReader();
        //            return true;
        //        }
        //        else
        //        {
        //            this.DBOpen();
        //            this.DBCmd.CommandText = sqlStatment;
        //            dataReader = this.DBCmd.ExecuteReader();
        //            return true;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        this.log.saveLogMsg(sqlStatment, "DBExecSQL");
        //        this.log.saveLogMsg(ex.Message.ToString(), "DBExecSQL");
        //        return false;
        //    }
        //}

        /// <summary>
        /// 執行SQL取得DATATABLE
        /// </summary>
        /// <param name="sqlStatment">要執行的查詢語法</param>
        /// <param name="dataTable">資料讀取器的位置(call by reference)</param>
        /// <param name="saveSqlFile">發生錯誤時是否要順便紀錄SQL</param>
        /// <returns>是否取得成功</returns>
        public void DBExecSQL(string sqlStatment, ref DataTable dataTable, bool saveSqlFile = false)
        {

            System.Environment.SetEnvironmentVariable("NLS_LANG", "TRADITIONAL CHINESE_TAIWAN.ZHT16BIG5");
            DBOpen();
            try
            {
                this.DBCmd.CommandText = sqlStatment;
                dataTable.Load(this.DBCmd.ExecuteReader());
            }
            catch (StackOverflowException stack_ex)
            {
                this.log.saveLogMsg(sqlStatment, "DBExecSQL");
                this.log.saveLogMsg(stack_ex.Message.ToString(), "DBExecSQL");
            }
            catch (Exception ex)
            {
                this.log.saveLogMsg(sqlStatment, "DBExecSQL");
                this.log.saveLogMsg(ex.Message.ToString(), "DBExecSQL");
            }
            finally
            {
                DBClose();
            }
        }

        /// <summary>
        /// 執行SQL取得DataTable
        /// </summary>
        /// <param name="SqlStatment">要執行的查詢語法</param>
        /// <param name="DataTable">資料讀取器的位置(call by reference)</param>
        /// <returns>取得DataTable</returns>
        public DataTable DBExecSQL(string SqlStatment)
        {
            DataTable DataTable = new DataTable();
            try
            {
                DBOpen();
                this.DBCmd.CommandText = SqlStatment.Replace("°Ｃ", "℃").Replace("°C", "℃").Replace("°", "℃");
                OleDbDataReader DataReader = this.DBCmd.ExecuteReader();
                DataTable.Load(DataReader);
                //DataReader.Close();
                //DataReader.Dispose();
                return DataTable;
            }
            catch (StackOverflowException stack_ex)
            {
                this.log.saveLogMsg(SqlStatment, "DBExecSQL");
                this.log.saveLogMsg(stack_ex.Message.ToString(), "DBExecSQL");
                return DataTable;
            }
            catch (Exception Ex)
            {
                this.log.saveLogMsg(SqlStatment, "DBExecSQL");
                this.log.saveLogMsg(Ex.Message.ToString(), "DBExecSQL");
                return DataTable;
            }
            finally
            {
                DBClose();
            }
        }

        /// <summary>
        /// 執行輸入語法
        /// </summary>
        /// <param name="tableName">表格名稱</param>
        /// <param name="insertData">輸入資料集</param>
        /// <param name="saveSqlFile">錯誤時是否要儲存SQL檔</param>
        /// <returns></returns>
        public int DBExecInsert(string tableName, List<DBItem> insertData)
        {
            int effectRow = 0;
            string sqlStatment = "";

            if (set_insert_sql(tableName, insertData, ref sqlStatment))
            {
                try
                {
                    DBOpen();
                    this.DBCmd.CommandText = sqlStatment.Replace("°Ｃ", "℃").Replace("°C", "℃").Replace("°", "℃");
                    effectRow = this.DBCmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    this.log.saveLogMsg(sqlStatment, "DBExecInsert");
                    this.log.saveLogMsg(ex.Message.ToString(), "DBExecInsert");
                }
                finally
                {
                    DBClose();
                }
            }
            return effectRow;
        }

        public int DBExecInsertTns(string tableName, List<DBItem> insertData)
        {
            int effectRow = 0;
            string sqlStatment = "";

            if (set_insert_sql(tableName, insertData, ref sqlStatment))
            {
                try
                {
                    if (DBLink.State == System.Data.ConnectionState.Closed)
                    {
                        DBOpen();
                    }
                    this.DBCmd.CommandText = sqlStatment.Replace("°Ｃ", "℃").Replace("°C", "℃").Replace("°", "℃");
                    effectRow = this.DBCmd.ExecuteNonQuery();

                }
                catch (Exception ex)
                {
                    this.log.saveLogMsg(sqlStatment, "DBExecInsert");
                    this.log.saveLogMsg(ex.Message.ToString(), "DBExecInsert");
                }
            }
            return effectRow;
        }

        /// <summary> 組成INSERT語法 </summary>
        private bool set_insert_sql(string tableName, List<DBItem> insertData, ref string sql)
        {
            bool success = true;
            sql = "INSERT INTO " + tableName;
            List<string> fieldSet = new List<string>(), valueSet = new List<string>();

            // 先取出資料
            foreach (DBItem item in insertData)
            {
                switch (item.DataType)
                {
                    case DBItem.DBDataType.String:
                        fieldSet.Add(item.Field);
                        valueSet.Add(Qt(item.Value));
                        break;
                    case DBItem.DBDataType.DataTime:
                        fieldSet.Add(item.Field);
                        valueSet.Add("to_date(" + Qt(item.Value) + ",'yyyy/MM/dd hh24:mi:ss')");
                        break;
                    case DBItem.DBDataType.Number:
                        fieldSet.Add(item.Field);
                        if (item.Value == "")
                            item.Value = "null";
                        valueSet.Add(item.Value);
                        break;
                }
            }
            sql += "(" + String.Join(",", fieldSet.ToArray()) + ") values (" + String.Join(",", valueSet.ToArray()) + ")";

            return success;
        }

        /// <summary>
        /// 執行更新語法
        /// </summary>
        /// <param name="tableName">表格名稱</param>
        /// <param name="updData">更新資料集</param>
        /// <param name="saveSqlFile">錯誤時是否要儲存SQL檔</param>
        /// <returns>影響列數</returns>
        public int DBExecUpdate(string tableName, List<DBItem> updData, string whereCondition)
        {
            int effectRow = 0;
            string sqlStatment = "";

            if (set_upd_sql(tableName, whereCondition, updData, ref sqlStatment))
            {
                try
                {
                    DBOpen();
                    this.DBCmd.CommandText = sqlStatment.Replace("°Ｃ", "℃").Replace("°C", "℃").Replace("°", "℃");
                    effectRow = this.DBCmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    this.log.saveLogMsg(sqlStatment, "DBExecUpdate");
                    this.log.saveLogMsg(ex.Message.ToString(), "DBExecUpdate");
                }
                finally
                {
                    DBClose();
                }
            }
            return effectRow;
        }

        public int DBExecUpdateTns(string tableName, List<DBItem> updData, string whereCondition)
        {
            int effectRow = 0;
            string sqlStatment = "";

            if (set_upd_sql(tableName, whereCondition, updData, ref sqlStatment))
            {
                try
                {
                    if (DBLink.State == System.Data.ConnectionState.Closed)
                    {
                        DBOpen();
                    }
                    this.DBCmd.CommandText = sqlStatment.Replace("°Ｃ", "℃").Replace("°C", "℃").Replace("°", "℃");
                    effectRow = this.DBCmd.ExecuteNonQuery();
                    
                }
                catch (Exception ex)
                {
                    this.log.saveLogMsg(sqlStatment, "DBExecUpdate");
                    this.log.saveLogMsg(ex.Message.ToString(), "DBExecUpdate");
                }
            }
            return effectRow;
        }

        /// <summary> 組成Upd語法 </summary>
        private bool set_upd_sql(string tableName, string whereCondition, List<DBItem> updData, ref string sql)
        {
            bool success = true;
            sql = "UPDATE " + tableName + " SET ";
            List<string> updStr = new List<string>();

            foreach (DBItem item in updData)
            {
                switch (item.DataType)
                {
                    case DBItem.DBDataType.String:
                        updStr.Add(item.Field + " = " + Qt(item.Value));
                        break;
                    case DBItem.DBDataType.DataTime:
                        updStr.Add(item.Field + " = to_date(" + Qt(item.Value) + ",'yyyy/MM/dd hh24:mi:ss')");
                        break;
                    case DBItem.DBDataType.Number:
                        if (string.IsNullOrEmpty(item.Value))
                            item.Value = "null";
                        updStr.Add(item.Field + " = " + item.Value);
                        break;

                }
            }
            sql += String.Join(",", updStr.ToArray()) + " WHERE " + whereCondition;

            return success;
        }

        public int DBExecDeleteTns(string tableName, string whereCondition, bool saveSqlFile = false)
        {
            string sqlStatment = " delete " + tableName + " where " + whereCondition;
            this.DBCmd.CommandText = sqlStatment;

            try
            {
                DBOpen();
                return this.DBCmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                //  if (saveSqlFile)
                //      this.log.saveToSQLFile(sqlStatment, "DBExecDelete");
                this.log.saveLogMsg(sqlStatment, "DBExecDelete");
                this.log.saveLogMsg(ex.Message.ToString(), "DBExecDelete");
                throw;
            }
            finally
            {
                DBClose();
            }
        }

        /// <summary>
        /// 刪除資料
        /// </summary>
        /// <param name="tableName">表單名稱</param>
        /// <param name="whereCondition">刪除條件</param>
        /// <param name="saveSqlFile">錯誤時是否要儲存SQL檔</param>
        /// <returns></returns>
        public int DBExecDelete(string tableName, string whereCondition, bool saveSqlFile = false)
        {
            string sqlStatment = " delete " + tableName + " where " + whereCondition;
            this.DBCmd.CommandText = sqlStatment;

            try
            {
                DBOpen();
                return this.DBCmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                //  if (saveSqlFile)
                //      this.log.saveToSQLFile(sqlStatment, "DBExecDelete");
                this.log.saveLogMsg(sqlStatment, "DBExecDelete");
                this.log.saveLogMsg(ex.Message.ToString(), "DBExecDelete");
                return 0;
            }
            finally
            {
                DBClose();
            }
        }


        /// <summary>
        /// 加上單引號 ex : 'inStr'
        /// </summary>
        /// <param name="inStr">帶入的值</param>
        /// <returns>回傳加上單引號的值</returns>
        private string Qt(string inStr)
        {
            string tmpStr = "";
            if (!string.IsNullOrWhiteSpace(inStr))
            {
                try
                {
                    inStr = inStr.Replace("'", "''");
                    tmpStr = "'" + inStr.Trim() + "'";
                }
                catch (Exception ex)
                {
                    log.saveLogMsg("Qt Error : " + inStr + "\t" + ex.ToString(), "DBConnector");
                }
            }
            else
            {
                tmpStr = "'" + tmpStr.Trim() + "'";
            }

            return tmpStr;
        }

        /// <summary>
        /// 執行SQL
        /// </summary>
        /// <param name="sqlStatment">要執行的查詢語法</param>
        /// <param name="dataReader">資料讀取器的位置(call by reference)</param>
        /// <param name="saveSqlFile">發生錯誤時是否要順便紀錄SQL</param>
        /// <returns>是否取得成功</returns>
        public bool DBExec(string sqlStatment)
        {
            try
            {
                if (this.DBLink.State == System.Data.ConnectionState.Open)
                {
                    this.DBCmd.CommandText = sqlStatment;
                    this.DBCmd.ExecuteNonQuery();
                    return true;
                }
                else
                {
                    this.DBOpen();
                    this.DBCmd.CommandText = sqlStatment;
                    this.DBCmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch (Exception ex)
            {
                this.log.saveLogMsg(sqlStatment, "DBExecSQL");
                this.log.saveLogMsg(ex.Message.ToString(), "DBExecSQL");
                return false;
            }
        }
        public int DBExecNonSQL(string sqlStatment, bool saveSqlFile = false)
        {
            try
            {
                if (this.IsTransaction == false)
                    DBOpen();
                this.DBCmd.CommandText = sqlStatment;
                int aaa = this.DBCmd.ExecuteNonQuery();
                return aaa;
            }
            catch (Exception ex)
            {
                DBRollBack();
                this.log.saveLogMsg(sqlStatment, "DBExecNonSQL");
                this.log.saveLogMsg(ex.Message.ToString(), "DBExecNonSQL");
                return 0;
            }
        }
        public int DBExecNonSQL(OleDbCommand cmd, bool saveSqlFile = false)
        {
            try
            {
                cmd.Connection = this.DBLink;
                return cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                DBRollBack();
                this.log.saveLogMsg(cmd.CommandText, "DBExecNonSQL");
                this.log.saveLogMsg(ex.Message.ToString(), "DBExecNonSQL");
                return 0;
            }
        }

        #region 寫ErrorLog (write_logMsg)
        //寫入 write_logMsg
        public static void write_logMsg(string controllerName, string loginID, string actionName, string err_msg, string err_folder = "DBExecSQL", string strSQL = "", Exception ex = null)
        {
            string tmp_msg = "", tmpfolder = "";
            if (err_folder != "")
                tmpfolder = err_folder;
            else
                tmpfolder = "DBExecSQL";
            tmp_msg = "loginID: " + loginID + ",\tcontrollerName: " + controllerName + "\t,ActionName: " + actionName + "\t行號: #" + GetLineNumber(ex).ToString() + "\n";
            LogTool log = new LogTool();
            if (strSQL != "")
            {
                log.saveLogMsg(tmp_msg + ", err_msg= \t" + err_msg, tmpfolder);
                log.saveLogMsg(tmp_msg + ", SQL= \t" + strSQL, tmpfolder);
            }
            else
                log.saveLogMsg(tmp_msg + ", err_msg= \t" + err_msg, tmpfolder);
        }
        public static void write_logMsg(string controllerName, string loginID, string actionName, string err_msg)
        {
            string tmp_msg = "", tmpfolder = "DBExecSQL", strSQL = "", err_folder = "";
            if (err_folder != "")
                tmpfolder = err_folder;
            else
                tmpfolder = "DBExecSQL";
            tmp_msg = "loginID: " + loginID + ",\tcontrollerName: " + controllerName + "\t,ActionName: " + actionName + "\n";
            LogTool log = new LogTool();
            if (strSQL != "")
            {
                log.saveLogMsg(tmp_msg + ", err_msg= \t" + err_msg, tmpfolder);
                log.saveLogMsg(tmp_msg + ", SQL= \t" + strSQL, tmpfolder);
            }
            else
                log.saveLogMsg(tmp_msg + ", err_msg= \t" + err_msg, tmpfolder);
        }

        public static void write_logMsg(string controllerName, string loginID, string actionName, string err_msg, Exception ex = null)
        {
            string tmp_msg = "", tmpfolder = "DBExecSQL", strSQL = "", err_folder = "";
            if (err_folder != "")
                tmpfolder = err_folder;
            else
                tmpfolder = "DBExecSQL";
            tmp_msg = "loginID: " + loginID + ",\tcontrollerName: " + controllerName + "\t,ActionName: " + actionName + "\t行號: #" + GetLineNumber(ex).ToString() + "\n";
            LogTool log = new LogTool();
            if (strSQL != "")
            {
                log.saveLogMsg(tmp_msg + ", err_msg= \t" + err_msg, tmpfolder);
                log.saveLogMsg(tmp_msg + ", SQL= \t" + strSQL, tmpfolder);
            }
            else
                log.saveLogMsg(tmp_msg + ", err_msg= \t" + err_msg, tmpfolder);
        }

        //回傳error行號
        public static int GetLineNumber(Exception ex)
        {
            var lineNumber = 0;
            const string lineSearch = ":line ";
            var index = ex.StackTrace.LastIndexOf(lineSearch);
            if (index != -1)
            {
                var lineNumberText = ex.StackTrace.Substring(index + lineSearch.Length);
                if (int.TryParse(lineNumberText, out lineNumber))
                {
                }
            }
            return lineNumber;
        }
        #endregion

    }


    public class DBConnector2
    {
        string aStr { get { return ":"; } }
        private OracleConnection DBLink;
        public OracleCommand DBCmd;
        private LogTool log = new LogTool();
        private OracleTransaction DBTnstion;
        private bool IsTransaction;

        public static string OracleConnectionStr = IniFile.GetConnStr2();
        public static string no_pool = OracleConnectionStr + ";Pooling=false";
        public static string with_pool = OracleConnectionStr + ";Pooling=true";
        public static string with_cache = with_pool + "; Statement Cache Size=1";

        public DBConnector2()
        {
            try
            {
                //string OracleConnectionStr = IniFile.GetConnStr2();
                //this.DBLink = new OracleConnection(OracleConnectionStr);
                //this.DBLink = new OracleConnection(with_pool);
                //this.DBCmd = new OracleCommand();
                //this.DBCmd.Connection = this.DBLink;


                // the connection object to use for the test
                this.DBLink = new OracleConnection(no_pool);
                this.DBCmd = new OracleCommand();
                this.DBCmd.Connection = this.DBLink;

                //ConnectionPoolTest(10);
            }
            catch (Exception ex)
            {
                this.log.saveLogMsg(ex.Message.ToString(), "DBConnector_V2");
            }
        }



        /// <summary>
        /// 執行SQL
        /// </summary>
        /// <param name="sqlStatment">要執行的查詢語法</param>
        /// <param name="dataReader">資料讀取器的位置(call by reference)</param>
        /// <param name="saveSqlFile">發生錯誤時是否要順便紀錄SQL</param>
        /// <returns>是否取得成功</returns>
        public bool DBExec(string sqlStatment)
        {
            try
            {
                DBOpen();
                this.DBCmd.CommandText = sqlStatment;
                this.DBCmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                this.log.saveLogMsg(sqlStatment, "DBExecSQL");
                this.log.saveLogMsg(ex.Message.ToString(), "DBExecSQL");
                return false;
            }
            finally
            {
                DBClose();
            }
        }



        private int ExecuteNonQuery(string sql, List<DBItem> dbSet, string source = "")
        {
            int cnt = 0;
            try
            {
                if (source != "Tns")
                {
                    DBOpen();
                }

                OracleCommand cmd = new OracleCommand();
                cmd.Connection = this.DBLink;
                cmd.BindByName = true; // 確定 parameters 比對方式
                //以Parameter方式新增資料
                cmd.CommandText = sql;
                foreach (DBItem item in dbSet)
                {
                    string str_trim = item.Value;
                    if (!string.IsNullOrEmpty(item.Value))
                    {
                        str_trim = item.Value.Trim();
                    }
                    switch (item.DataType)
                    {
                        case DBItem.DBDataType.String:
                            cmd.Parameters.Add(item.Field, OracleDbType.NVarchar2).Value = str_trim;
                            break;
                        case DBItem.DBDataType.DataTime:
                            cmd.Parameters.Add(item.Field, OracleDbType.NVarchar2).Value = str_trim;
                            break;
                        case DBItem.DBDataType.Number:
                            cmd.Parameters.Add(item.Field, OracleDbType.NVarchar2).Value = str_trim;
                            break;
                        default:
                            break;
                    }
                }

                cnt = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
            }
            catch (Exception ex)
            {
                this.log.saveLogMsg(sql, "ExecuteNonQuery");
                this.log.saveLogMsg(ex.Message.ToString(), "ExecuteNonQuery");
            }
            finally
            {
                //Tns 不要關連線
                if (source != "Tns")
                {
                    DBClose();
                }
            }

            return cnt;
        }
        #region 方法

        #region 新增

        public int DBExecInsert(string tableName, List<DBItem> dbSet)
        {
            int cnt = 0;
            string sqlInsert = set_insert_sql(tableName, dbSet);
            try
            {
                cnt = ExecuteNonQuery(sqlInsert, dbSet);
            }
            catch (Exception ex)
            {
                this.log.saveLogMsg(sqlInsert, "DBinsert");
                this.log.saveLogMsg(ex.Message.ToString(), "DBinsert");
            }

            return cnt;
        }


        public int DBExecInsertTns(string tableName, List<DBItem> insertData)
        {
            int effectRow = 0;
            string sqlStatment = "";

            sqlStatment = set_insert_sql(tableName, insertData);
            try
            {
                DBOpen();
                effectRow = ExecuteNonQuery(sqlStatment, insertData, "Tns");
            }
            catch (Exception ex)
            {
                this.log.saveLogMsg(sqlStatment, "DBExecInsert");
                this.log.saveLogMsg(ex.Message.ToString(), "DBExecInsert");
            }
            //Tns 不要關連線
            return effectRow;
        }
        /// <summary> 組成INSERT語法 </summary>
        private bool set_insert_sql(string tableName, List<DBItem> insertData, ref string sql)
        {
            bool success = true;
            sql = "INSERT INTO " + tableName;
            List<string> fieldSet = new List<string>(), valueSet = new List<string>();

            // 先取出資料
            foreach (DBItem item in insertData)
            {
                switch (item.DataType)
                {
                    case DBItem.DBDataType.String:
                        fieldSet.Add(item.Field);
                        valueSet.Add(Qt(item.Value));
                        break;
                    case DBItem.DBDataType.DataTime:
                        fieldSet.Add(item.Field);
                        valueSet.Add("to_date(" + Qt(item.Value) + ",'yyyy/MM/dd hh24:mi:ss')");
                        break;
                    case DBItem.DBDataType.Number:
                        fieldSet.Add(item.Field);
                        if (item.Value == "")
                            item.Value = "null";
                        valueSet.Add(item.Value);
                        break;                    
                }
            }
            sql += "(" + String.Join(",", fieldSet.ToArray()) + ") values (" + String.Join(",", valueSet.ToArray()) + ")";

            return success;
        }

        /// <summary> 組成INSERT語法 </summary>
        private string set_insert_sql(string tableName, List<DBItem> insertData)
        {
            string sql = "INSERT INTO " + tableName;
            List<string> fieldSet = new List<string>(), valueSet = new List<string>();
            // 先取出資料
            foreach (DBItem item in insertData)
            {
                switch (item.DataType)
                {
                    case DBItem.DBDataType.String:
                        if (item.Value == null)
                        {
                            fieldSet.Add(item.Field);
                            item.Value = "null";
                            //valueSet.Add(item.Field + " = " + DBNull.Value);
                            valueSet.Add(item.Value);
                        }
                        else
                        {
                            fieldSet.Add(item.Field);
                            valueSet.Add(aStr + item.Field);
                        }                           
                        break;
                    case DBItem.DBDataType.DataTime:
                        fieldSet.Add(item.Field);
                        valueSet.Add("to_date(" + aStr + item.Field + ",'yyyy/MM/dd hh24:mi:ss')");
                        break;
                    case DBItem.DBDataType.Number:
                        fieldSet.Add(item.Field);
                        if (item.Value == "")
                            item.Value = "null";
                        valueSet.Add(item.Value);
                        break;
                }
            }
            sql += "(" + String.Join(",", fieldSet.ToArray()) + ") values (" + String.Join(",", valueSet.ToArray()) + ")";

            return sql;
        }

        #endregion

        #region 更新

        /// <summary>
        /// 執行更新語法
        /// </summary>
        /// <param name="tableName">表格名稱</param>
        /// <param name="updData">更新資料集</param>
        /// <param name="saveSqlFile">錯誤時是否要儲存SQL檔</param>
        /// <returns>影響列數</returns>
        public int DBExecUpdate(string tableName, List<DBItem> updData, string whereCondition)
        {
            int effectRow = 0;
            string sqlStatment = set_upd_sql(tableName, whereCondition, updData);
            try
            {
                DBOpen();
                effectRow = ExecuteNonQuery(sqlStatment, updData);
            }
            catch (Exception ex)
            {
                this.log.saveLogMsg(sqlStatment, "DBExecUpdate");
                this.log.saveLogMsg(ex.Message.ToString(), "DBExecUpdate");
            }
            finally
            {
                DBClose();
            }

            return effectRow;
        }
        public int DBExecUpdateTns(string tableName, List<DBItem> updData, string whereCondition)
        {
            int effectRow = 0;
            string sqlStatment = "";

            sqlStatment = set_upd_sql(tableName, whereCondition, updData);
            try
            {
                DBOpen();
                effectRow = ExecuteNonQuery(sqlStatment, updData, "Tns");
            }
            catch (Exception ex)
            {
                this.log.saveLogMsg(sqlStatment, "DBExecUpdate");
                this.log.saveLogMsg(ex.Message.ToString(), "DBExecUpdate");
            }
            //finally
            //{   
            //    DBClose(); //Tns 不要關連線
            //}

            return effectRow;
        }
        private bool set_upd_sql(string tableName, string whereCondition, List<DBItem> updData, ref string sql)
        {
            bool success = true;
            sql = "UPDATE " + tableName + " SET ";
            List<string> updStr = new List<string>();

            foreach (DBItem item in updData)
            {
                switch (item.DataType)
                {
                    case DBItem.DBDataType.String:
                        updStr.Add(item.Field + " = " + aStr + item.Field);
                        break;
                    case DBItem.DBDataType.DataTime:
                        updStr.Add(item.Field + " = to_date(" + aStr + item.Field + ",'yyyy/MM/dd hh24:mi:ss')");
                        break;
                    case DBItem.DBDataType.Number:
                        updStr.Add(item.Field + " = " + item.Value);
                        break;
                }
            }
            sql += String.Join(",", updStr.ToArray()) + " WHERE " + whereCondition;

            return success;
        }

        /// <summary> 組成Upd語法 </summary>
        private string set_upd_sql(string tableName, string whereCondition, List<DBItem> updData)
        {
            string sql = "UPDATE " + tableName + " SET ";
            List<string> updStr = new List<string>();

            foreach (DBItem item in updData)
            {
                switch (item.DataType)
                {
                    case DBItem.DBDataType.String:
                        if (item.Value == null)
                            updStr.Add(item.Field + " = " + DBNull.Value);
                        else
                            updStr.Add(item.Field + " = " + aStr + item.Field);
                        break;
                    case DBItem.DBDataType.DataTime:
                        updStr.Add(item.Field + " = to_date(" + aStr + item.Field + ",'yyyy/MM/dd hh24:mi:ss')");
                        break;
                    case DBItem.DBDataType.Number:
                        if (item.Value == "")
                            item.Value = "null";
                        updStr.Add(item.Field + " = " + aStr + item.Field);
                        break;

                }
            }
            sql += String.Join(",", updStr.ToArray()) + " WHERE " + whereCondition;

            return sql;
        }
        #endregion

        #region 刪除

        /// <summary>
        /// 刪除資料
        /// </summary>
        /// <param name="tableName">表單名稱</param>
        /// <param name="whereCondition">刪除條件</param>
        /// <param name="saveSqlFile">錯誤時是否要儲存SQL檔</param>
        /// <returns></returns>
        public int DBExecDelete(string tableName, string whereCondition, bool saveSqlFile = false)
        {
            string sqlStatment = " delete " + tableName + " where " + whereCondition;
            this.DBCmd.CommandText = sqlStatment;

            try
            {
                DBOpen();
                return this.DBCmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                this.log.saveLogMsg(sqlStatment, "DBExecDelete");
                this.log.saveLogMsg(ex.Message.ToString(), "DBExecDelete");
                return 0;
            }
            finally
            {
                DBClose();
            }
        }

        public int DBExecDeleteTns(string tableName, string whereCondition, bool saveSqlFile = false)
        {
            string sqlStatment = " delete " + tableName + " where " + whereCondition;
            this.DBCmd.CommandText = sqlStatment;

            try
            {
                DBOpen();
                return this.DBCmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                this.log.saveLogMsg(sqlStatment, "DBExecDelete");
                this.log.saveLogMsg(ex.Message.ToString(), "DBExecDelete");
                throw;

            }
            //finally
            //{
            //    DBClose(); //Tns 不要關連線
            //}
        }
        #endregion

        #region 開啟資料庫

        /// <summary> 開啟DB </summary>
        /// <param name="OpenTnstion">是否啟動 Transaction</param>
        public void DBOpen(bool OpenTnstion = false)
        {
            try
            {
                if (this.DBLink.State == System.Data.ConnectionState.Closed)
                {
                    this.DBLink.Open();
                }
                if (OpenTnstion)
                {
                    this.IsTransaction = true;
                    this.DBTnstion = this.DBLink.BeginTransaction();
                    this.DBCmd.Transaction = this.DBTnstion;
                }
                else if (DBTnstion != null)
                {
                    this.IsTransaction = false;
                    this.DBTnstion.Dispose();
                    this.DBTnstion = null;
                }
                else
                {
                }

            }
            catch (Exception ex)
            {
                this.log.saveLogMsg(ex.Message.ToString(), "DBOpen");
            }
        }

        /// <summary>
        /// 關閉連線
        /// </summary>
        public void DBClose()
        {
            try
            {
                if (this.DBLink.State == System.Data.ConnectionState.Open)
                {
                    //2022/02/21 家雄加 (院方反應記憶體會堆砌)
                    //Dispose large object
                    try
                    {
                        foreach (OracleParameter p in this.DBCmd.Parameters)
                        {
                            if (p.OracleDbType == OracleDbType.Blob || p.OracleDbType == OracleDbType.Clob || p.OracleDbType == OracleDbType.NClob)
                            {
                                if (p.Value is IDisposable)
                                {
                                    ((IDisposable)(p.Value)).Dispose();
                                }
                                p.Value = null;
                            }
                            p.Dispose();
                        }
                    }
                    catch (Exception e)
                    {
                        //Do nothing
                    }

                    this.DBLink.Close();
                    OracleConnection.ClearPool(this.DBLink);
                }

            }
            catch (Exception ex)
            {
                this.log.saveLogMsg(ex.Message.ToString(), "DBClosed");
            }
        }
        /// <summary> 確認交易 </summary>
        public void DBCommit()
        {
            try
            {
                DBOpen();
                OracleTransaction DBTnstion = DBLink.BeginTransaction();

                if (DBTnstion != null)
                {
                    DBTnstion.Commit();
                    DBTnstion.Dispose();
                    DBTnstion = null;
                }
            }
            catch (Exception ex)
            {
                DBTnstion.Rollback();
                DBTnstion.Dispose();
                DBTnstion = null;
                this.log.saveLogMsg(ex.Message.ToString(), "DBCommit");
                throw new DBCommitException("DBCommit Fail");
            }
            finally
            {
                DBClose();
            }
        }

        #region 自訂Exception: DBCommitException

        [Serializable]
        class DBCommitException : Exception
        {
            public DBCommitException()
            {
            }

            public DBCommitException(string Msg)
                : base(String.Format("DB Commit Exception: {0}", Msg))
            {
            }
        }

        #endregion


        /// <summary> 取消交易 </summary>
        public void DBRollBack()
        {
            try
            {
                //if (this.DBLink.State == System.Data.ConnectionState.Closed)
                //{
                //    this.DBLink.Open();
                //}
                DBOpen();
                OracleTransaction DBTnstion = DBLink.BeginTransaction();

                if (DBTnstion != null)
                {
                    //this.IsTransaction = false;
                    DBTnstion.Rollback();
                    DBTnstion.Dispose();
                    DBTnstion = null;
                }
            }
            catch (Exception ex)
            {
                this.log.saveLogMsg(ex.Message.ToString(), "DBRollBack");
            }
            finally
            {
                DBClose();
            }
        }



        /// <summary>
        /// 執行SQL取得DATATABLE
        /// </summary>
        /// <param name="sqlStatment">要執行的查詢語法</param>
        /// <param name="dataTable">資料讀取器的位置(call by reference)</param>
        /// <param name="saveSqlFile">發生錯誤時是否要順便紀錄SQL</param>
        /// <returns>是否取得成功</returns>
        public void DBExecSQL(string sqlStatment, ref DataTable dataTable, bool saveSqlFile = false)
        {
            System.Environment.SetEnvironmentVariable("NLS_LANG", "TRADITIONAL CHINESE_TAIWAN.AL32UTF8");

            try
            {
                DBOpen();
                this.DBCmd.CommandText = sqlStatment;
                dataTable.Load(this.DBCmd.ExecuteReader());
            }
            catch (StackOverflowException stack_ex)
            {
                this.log.saveLogMsg(sqlStatment, "DBExecSQL");
                this.log.saveLogMsg(stack_ex.Message.ToString(), "DBExecSQL");
            }
            catch (Exception ex)
            {
                this.log.saveLogMsg(sqlStatment, "DBExecSQL");
                this.log.saveLogMsg(ex.Message.ToString(), "DBExecSQL");
            }
            finally
            {
                DBClose();
            }
        }



        /// <summary>
        /// 執行SQL取得Boolean
        /// </summary>
        /// <param name="SqlStatment">要執行的查詢語法</param>
        /// <param name="DataTable">資料讀取器的位置(call by reference)</param>
        /// <returns>取得Boolean</returns>
        public Boolean DBExecSQL(string SqlStatment, bool saveSqlFile = false)
        {
            try
            {
                DBOpen();
                DataTable DataTable = new DataTable();
                this.DBCmd.CommandText = SqlStatment.Replace("°Ｃ", "℃").Replace("°C", "℃").Replace("°", "℃");
                IDataReader DataReader = this.DBCmd.ExecuteReader();

                DataTable.Load(DataReader);
                return true;
            }
            catch (Exception Ex)
            {
                this.log.saveLogMsg(SqlStatment, "DBExecSQL");
                this.log.saveLogMsg(Ex.Message.ToString(), "DBExecSQL");
                return false;
            }
            finally
            {
                DBClose();
            }
        }


        /// <summary>
        /// 執行SQL取得DataTable
        /// </summary>
        /// <param name="SqlStatment">要執行的查詢語法</param>
        /// <param name="DataTable">資料讀取器的位置(call by reference)</param>
        /// <returns>取得DataTable</returns>
        public DataTable DBExecSQL(string SqlStatment)
        {
            DataTable DataTable = new DataTable();
            try
            {
                DBOpen();
                this.DBCmd.CommandText = SqlStatment.Replace("°Ｃ", "℃").Replace("°C", "℃").Replace("°", "℃");
                IDataReader DataReader = null;
                DataReader = this.DBCmd.ExecuteReader();
                DataTable.Load(DataReader);

                return DataTable;
            }
            catch (StackOverflowException stack_ex)
            {
                this.log.saveLogMsg(SqlStatment, "DBExecSQL");
                this.log.saveLogMsg(stack_ex.Message.ToString(), "DBExecSQL");
                return DataTable;
            }
            catch (Exception Ex)
            {
                this.log.saveLogMsg(SqlStatment, "DBExecSQL");
                this.log.saveLogMsg(Ex.Message.ToString(), "DBExecSQL");
                return DataTable;
            }
            finally
            {
                DBClose();
            }
        }
        #endregion
        #endregion
        private string Qt(string inStr)
        {
            string tmpStr = "";
            if (!string.IsNullOrWhiteSpace(inStr))
            {
                try
                {
                    inStr = inStr.Replace("'", "''");
                    tmpStr = "'" + inStr.Trim() + "'";
                }
                catch (Exception ex)
                {
                    log.saveLogMsg("Qt Error : " + inStr + "\t" + ex.ToString(), "DBConnector");
                }
            }
            else
            {
                tmpStr = "'" + tmpStr.Trim() + "'";
            }

            return tmpStr;
        }



        #region 寫ErrorLog (write_logMsg)
        //寫入 write_logMsg
        public static void write_logMsg(string controllerName, string loginID, string actionName, string err_msg, string err_folder = "DBExecSQL", string strSQL = "", Exception ex = null)
        {
            string tmp_msg = "", tmpfolder = "";
            if (err_folder != "")
                tmpfolder = err_folder;
            else
                tmpfolder = "DBExecSQL";
            tmp_msg = "loginID: " + loginID + ",\tcontrollerName: " + controllerName + "\t,ActionName: " + actionName + "\t行號: #" + GetLineNumber(ex).ToString() + "\n";
            LogTool log = new LogTool();
            if (strSQL != "")
            {
                log.saveLogMsg(tmp_msg + ", err_msg= \t" + err_msg, tmpfolder);
                log.saveLogMsg(tmp_msg + ", SQL= \t" + strSQL, tmpfolder);
            }
            else
                log.saveLogMsg(tmp_msg + ", err_msg= \t" + err_msg, tmpfolder);
        }
        public static void write_logMsg(string controllerName, string loginID, string actionName, string err_msg)
        {
            string tmp_msg = "", tmpfolder = "DBExecSQL", strSQL = "", err_folder = "";
            if (err_folder != "")
                tmpfolder = err_folder;
            else
                tmpfolder = "DBExecSQL";
            tmp_msg = "loginID: " + loginID + ",\tcontrollerName: " + controllerName + "\t,ActionName: " + actionName + "\n";
            LogTool log = new LogTool();
            if (strSQL != "")
            {
                log.saveLogMsg(tmp_msg + ", err_msg= \t" + err_msg, tmpfolder);
                log.saveLogMsg(tmp_msg + ", SQL= \t" + strSQL, tmpfolder);
            }
            else
                log.saveLogMsg(tmp_msg + ", err_msg= \t" + err_msg, tmpfolder);
        }

        public static void write_logMsg(string controllerName, string loginID, string actionName, string err_msg, Exception ex = null)
        {
            string tmp_msg = "", tmpfolder = "DBExecSQL", strSQL = "", err_folder = "";
            if (err_folder != "")
                tmpfolder = err_folder;
            else
                tmpfolder = "DBExecSQL";
            tmp_msg = "loginID: " + loginID + ",\tcontrollerName: " + controllerName + "\t,ActionName: " + actionName + "\t行號: #" + GetLineNumber(ex).ToString() + "\n";
            LogTool log = new LogTool();
            if (strSQL != "")
            {
                log.saveLogMsg(tmp_msg + ", err_msg= \t" + err_msg, tmpfolder);
                log.saveLogMsg(tmp_msg + ", SQL= \t" + strSQL, tmpfolder);
            }
            else
                log.saveLogMsg(tmp_msg + ", err_msg= \t" + err_msg, tmpfolder);
        }

        //回傳error行號
        public static int GetLineNumber(Exception ex)
        {
            var lineNumber = 0;
            const string lineSearch = ":line ";
            var index = ex.StackTrace.LastIndexOf(lineSearch);
            if (index != -1)
            {
                var lineNumberText = ex.StackTrace.Substring(index + lineSearch.Length);
                if (int.TryParse(lineNumberText, out lineNumber))
                {
                }
            }
            return lineNumber;
        }
        #endregion


    }


}