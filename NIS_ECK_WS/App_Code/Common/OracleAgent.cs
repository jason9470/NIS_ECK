using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Data.OleDb;

namespace NIS
{
    public class OracleAgent
    {
        private string m_connStr = ConfigurationManager.ConnectionStrings["OracleConnectionString"].ConnectionString;
        private OleDbConnection DBConnect;
        private OleDbCommand SQLCmd;

        public OracleAgent()
        {
            //System.Environment.SetEnvironmentVariable("NLS_LANG", "TRADITIONAL CHINESE_TAIWAN.ZHT16MSWIN950");
            System.Environment.SetEnvironmentVariable("NLS_LANG", "TRADITIONAL CHINESE_TAIWAN.ZHT16BIG5");
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

        public int DBInsertData(string sql)
        {
            int cnt = 0;
            try
            {
                cnt = ExecuteNonQuery(sql);
            }
            catch (Exception ex)
            {
                return 0;
            }
            return cnt;
        }
        public int ExecuteNonQuery(string sql)
        {
            int cnt = 0;
            OleDbConnection conn = new OleDbConnection(m_connStr);
            try
            {
                if (conn.State == System.Data.ConnectionState.Closed)
                {
                    conn.Open();
                }

                OleDbCommand cmd = new OleDbCommand();
                cmd.Connection = conn;
                //以Parameter方式新增資料
                cmd.CommandText = sql;

                cnt = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
            }
            catch
            {
                cnt = 0;
            }
            finally
            {
                conn.Close();
            }
            return cnt;
        }

        public int Execute(string sql)
        {
            OleDbConnection conn = new OleDbConnection(m_connStr);

            OpenDB();
            OleDbCommand sqlCmd = new OleDbCommand(sql, conn);
            sqlCmd.CommandTimeout = 30;
            CloseDB();

            return sqlCmd.ExecuteNonQuery();
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
            catch (Exception ex)
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
}
