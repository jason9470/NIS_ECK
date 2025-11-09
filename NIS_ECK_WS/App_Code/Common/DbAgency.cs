using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Data.SqlClient;

namespace DSHealth
{
    public class DbAgency
    {
        private string m_connStr = ConfigurationManager.ConnectionStrings["MSSQLConnectionString"].ConnectionString;
        //private string m_connStr = ConfigurationManager.ConnectionStrings["OracleConnectionString"].ConnectionString;
        private SqlCommand m_command;

        public DbAgency()
        {
           
        }

        public DbAgency(string connStr)
        {
            m_connStr = connStr;
        }

        public DataTable GetDataTable(string sql)
        {
            SqlConnection conn = new SqlConnection(m_connStr);
            SqlDataAdapter adapter = new SqlDataAdapter(sql, conn);
            DataSet ds = new DataSet();

            adapter.Fill(ds, "table");
            return ds.Tables["table"];
        }

        public int Execute(string sql)
        {
            SqlConnection conn = new SqlConnection(m_connStr);

            conn.Open();
            SqlCommand sqlCmd = new SqlCommand(sql, conn);
            sqlCmd.CommandTimeout = 30;

            return sqlCmd.ExecuteNonQuery();
        }

        public int ExeSqlTransaction(string sqls)
        {
            int ret = 0;
            SqlConnection conn = new SqlConnection(m_connStr);
            SqlCommand command = new SqlCommand();
            SqlTransaction transaction;

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
            SqlConnection conn = new SqlConnection(m_connStr);
            m_command = new SqlCommand();
            SqlTransaction transaction;

            conn.Open();
            transaction = conn.BeginTransaction();
            m_command.Connection = conn;
            m_command.Transaction = transaction;
        }

        public void ExecSqlTransaction(string sql)
        {
            m_command.CommandText = sql;
            m_command.ExecuteNonQuery();
        }

        public void Commit()
        {
            m_command.Transaction.Commit();
            m_command.Connection.Close();
            m_command.Connection.Dispose();
        }

        public void Rollback()
        {
            m_command.Transaction.Rollback();
            m_command.Connection.Close();
            m_command.Connection.Dispose();
        }
    }
}
